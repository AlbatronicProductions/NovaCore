using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using C1=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftContactDescription<BepuPhysics.Constraints.Contact.Contact1OneBodyPrestepData,BepuPhysics.Constraints.Contact.Contact1AccumulatedImpulses,BepuPhysics.Constraints.Contact.Contact1OneBodyFunctions>;
using C2=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftContactDescription<BepuPhysics.Constraints.Contact.Contact2OneBodyPrestepData,BepuPhysics.Constraints.Contact.Contact2AccumulatedImpulses,BepuPhysics.Constraints.Contact.Contact2OneBodyFunctions>;
using C3=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftContactDescription<BepuPhysics.Constraints.Contact.Contact3OneBodyPrestepData,BepuPhysics.Constraints.Contact.Contact3AccumulatedImpulses,BepuPhysics.Constraints.Contact.Contact3OneBodyFunctions>;
using C4=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftContactDescription<BepuPhysics.Constraints.Contact.Contact4OneBodyPrestepData,BepuPhysics.Constraints.Contact.Contact4AccumulatedImpulses,BepuPhysics.Constraints.Contact.Contact4OneBodyFunctions>;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>One owned standard native constraint per actual convex child manifold.
/// The single rigid craft is unchanged. No aggregate
/// four-row reduction, synthetic contacts, load redistribution or release force.</summary>
internal sealed class CraftContactManifolds:IDisposable
{
    internal static PairMaterialProperties Material=>new(.5f,2f,new(30,1));
    private PairMaterialProperties ResponseMaterial=>launchSupport?Material:CraftSurfaceImpact.Material;
    private readonly BepuPhysics.Simulation simulation;
    private readonly BodyHandle body;
    private readonly StaticHandle slab;
    private readonly TypedIndex shape;
    private readonly CompiledCraftContact profile;
    private readonly bool launchSupport;
    private readonly record struct ContactKey(int Surface,int Child,int SurfaceChild):IComparable<ContactKey>
    {
        public int CompareTo(ContactKey b){var c=Surface.CompareTo(b.Surface);if(c==0)c=Child.CompareTo(b.Child);return c==0?SurfaceChild.CompareTo(b.SurfaceChild):c;}
    }
    private sealed class Entry {internal ConvexContactManifold Next,Previous;internal bool Seen;internal ConstraintHandle Handle;}
    private readonly SortedDictionary<ContactKey,Entry> entries=new();
    private readonly Dictionary<int,int> surfaces=new();
    private readonly double[] footImpulse;
    private readonly Double3[] footMoment;
    private AssemblyMass mass;
    private DoubleQuaternion materialToSite;
    internal bool Failed {get;private set;}
    internal string Failure {get;private set;}="";
    internal int ContactCount {get;private set;}
    internal double MaximumDepth {get;private set;}
    internal double MaximumLoad {get;private set;}
    internal Double3 LinearImpulse {get;private set;}
    internal ReadOnlySpan<double> FootImpulses=>footImpulse;
    internal ReadOnlySpan<Double3> FootFirstMoments=>footMoment;
    internal int ConstraintCount=>entries.Values.Count(e=>e.Previous.Count>0);
    internal CraftContactManifolds(BepuPhysics.Simulation simulation,BodyHandle body,StaticHandle slab,TypedIndex shape,CompiledCraftContact profile,bool launchSupport=true)
    {
        this.simulation=simulation;this.body=body;this.slab=slab;this.shape=shape;this.profile=profile;this.launchSupport=launchSupport;
        surfaces.Add(slab.Value,1);
        for(var child=0;child<profile.Craft.Collision.Length;child++)entries.Add(new(slab.Value,child,0),new());
        footImpulse=new double[profile.Craft.Support.Length];footMoment=new Double3[footImpulse.Length];
        foreach(var id in new[]{C1.ConstraintTypeId,C2.ConstraintTypeId,C3.ConstraintTypeId,C4.ConstraintTypeId})
            if(NarrowPhase.IsContactConstraintType(id)||(id<simulation.Solver.TypeProcessors.Length&&simulation.Solver.TypeProcessors[id] is not null))
                throw new InvalidDataException("Craft contact type identity is already owned.");
        simulation.Solver.Register<C1>();simulation.Solver.Register<C2>();simulation.Solver.Register<C3>();simulation.Solver.Register<C4>();
        if(simulation.Timestepper is not DefaultTimestepper stepper)throw new InvalidDataException("Craft contact requires the qualified native timestep lifecycle.");
        stepper.CollisionsDetected+=Reconcile;
    }
    internal void Begin(AssemblyMass current)
    {
        mass=current;var q=simulation.Bodies[body].Pose.Orientation;
        materialToSite=(new DoubleQuaternion(q.X,q.Y,q.Z,q.W)*AssemblyContactProfile.Upright).Normalized();
        foreach(var entry in entries.Values){entry.Next=default;entry.Seen=false;}
        Array.Clear(footImpulse);Array.Clear(footMoment);
        Failed=false;Failure="";ContactCount=0;MaximumDepth=0;MaximumLoad=0;LinearImpulse=default;
    }
    internal void VerifyInitialCoverage(AssemblyMass current)
    {
        Begin(current);
        simulation.Bodies[body].UpdateBounds();
        // Collision detection alone performs no integration, solve, or custom
        // reconciliation. No prepared impulses or settled pose are imported.
        simulation.CollisionDetection(0);
        if(Failed)throw new InvalidDataException(Failure);
        var covered=new bool[footImpulse.Length];
        foreach(var (key,entry) in entries)for(var j=0;j<entry.Next.Count;j++){
            var offset=entry.Next[j].Offset;
            var point=current.Com+materialToSite.Conjugate().Rotate(new(offset.X,offset.Y,offset.Z));
            var owner=profile.SupportAt(key.Child,point);
            if(owner<0)throw new InvalidDataException("Initial native contact is outside authored support.");
            covered[owner]=true;
        }
        if(covered.Any(c=>!c))throw new InvalidDataException("Initial native contact does not cover every authored support patch.");
    }
    internal void VerifyRestoredGeometry(AssemblyMass current)
    {
        Begin(current);simulation.Bodies[body].UpdateBounds();simulation.CollisionDetection(0);
        if(Failed)throw new InvalidDataException(Failure);
        foreach(var entry in entries.Values)for(var j=0;j<entry.Next.Count;j++)
            if(!float.IsFinite(entry.Next[j].Depth)||(launchSupport&&entry.Next[j].Depth>profile.ContactTolerance))
                throw new InvalidDataException("Restored contact penetration exceeds the physical envelope.");
    }
    internal void AddSurface(StaticHandle surface,int children)
    {
        if(launchSupport||children<=0||!surfaces.TryAdd(surface.Value,children))throw new InvalidDataException("Invalid terrain contact ownership.");
    }
    private bool Pair(int worker,CollidablePair pair)=>worker==0&&pair.A.Mobility==CollidableMobility.Dynamic&&pair.A.BodyHandle==body&&pair.B.Mobility==CollidableMobility.Static&&surfaces.ContainsKey(pair.B.StaticHandle.Value);
    private void Refuse(string reason){Failed=true;Failure=reason;}
    internal bool Child(int worker,CollidablePair pair,int childA,int childB,ref ConvexContactManifold manifold)
    {
        if(Failed||!Pair(worker,pair)||(uint)childA>=profile.Craft.Collision.Length||childB<0||childB>=surfaces[pair.B.StaticHandle.Value]||manifold.Count is <0 or >4){Refuse("Invalid native child contact ownership.");return false;}
        if(manifold.Count>0&&(!float.IsFinite(manifold.Normal.LengthSquared())||Math.Abs(manifold.Normal.LengthSquared()-1)>1e-4))
        {Refuse("Invalid native child contact normal.");return false;}
        for(var i=0;i<manifold.Count;i++)if(!float.IsFinite(manifold[i].Depth)||!float.IsFinite(manifold[i].Offset.LengthSquared()))
        {Refuse("Nonfinite native child contact row.");return false;}
        var key=new ContactKey(pair.B.StaticHandle.Value,childA,childB);
        if(!entries.TryGetValue(key,out var entry)){entry=new();entries.Add(key,entry);}
        if(entry.Seen){Refuse("Duplicate native child contact ownership.");return false;}
        entry.Seen=true;var copy=manifold;
        ref var child=ref simulation.Shapes.GetShape<Compound>(shape.Index).Children[childA];
        Compound.GetRotatedChildPose(child.LocalPosition,child.LocalOrientation,simulation.Bodies[body].Pose.Orientation,out var pose);
        for(var j=0;j<copy.Count;j++){
            var row=copy[j];row.Offset+=pose.Position;copy[j]=row;
            if(!launchSupport)
            {
                var velocity=simulation.Bodies[body].Velocity;
                var contactVelocity=velocity.Linear+Vector3.Cross(velocity.Angular,row.Offset);
                if(!CraftSurfaceImpact.FiniteContactSpeed(-Vector3.Dot(contactVelocity,copy.Normal)))
                {Refuse("Non-finite physical contact-point velocity.");return false;}
            }
            for(var k=0;k<j;k++)if(copy[k].FeatureId==row.FeatureId){Refuse("Ambiguous native child feature.");return false;}
        }
        // Stable raw-feature order, local to the owning child; no XOR/global IDs.
        for(var j=1;j<copy.Count;j++)for(var k=j;k>0&&copy[k].FeatureId<copy[k-1].FeatureId;k--){var a=copy[k];copy[k]=copy[k-1];copy[k-1]=a;}
        entry.Next=copy;return true;
    }
    internal bool Parent(int worker,CollidablePair pair)
    {
        if(!Pair(worker,pair))Refuse("Foreign aggregate contact pair.");
        return false; // Native child contacts are reconciled after collision detection.
    }
    private void Reconcile(float dt,IThreadDispatcher? dispatcher)
    {
        if(Failed)throw new InvalidDataException(Failure);
        if(dispatcher is not null)throw new InvalidDataException("Craft contact owner is single-threaded.");
        Span<float> old=stackalloc float[4];Span<float> mapped=stackalloc float[4];
        foreach(var entry in entries.Values){
            var before=entry.Previous;var after=entry.Next;var tangent=Vector2.Zero;var twist=0f;old.Clear();mapped.Clear();
            if(before.Count>0)Read(before.Count,entry.Handle,old,out tangent,out twist);
            var same=before.Count==after.Count&&before.Normal==after.Normal;
            for(var j=0;j<after.Count;j++){
                var found=false;
                if(before.Normal==after.Normal)for(var k=0;k<before.Count;k++)if(before[k].FeatureId==after[j].FeatureId){mapped[j]=old[k];found=true;break;}
                same&=found;
            }
            if(!same){tangent=default;twist=0;}
            if(before.Count>0&&before.Count!=after.Count)simulation.Solver.Remove(entry.Handle);
            if(after.Count>0){
                var add=before.Count!=after.Count;
                switch(after.Count){
                    case 1:Update(new C1{Manifold=after,Material=ResponseMaterial});break;
                    case 2:Update(new C2{Manifold=after,Material=ResponseMaterial});break;
                    case 3:Update(new C3{Manifold=after,Material=ResponseMaterial});break;
                    case 4:Update(new C4{Manifold=after,Material=ResponseMaterial});break;
                }
                Write(after.Count,entry.Handle,mapped,tangent,twist);
                void Update<T>(T description)where T:unmanaged,BepuPhysics.Constraints.IOneBodyConstraintDescription<T>{
                    if(add)entry.Handle=simulation.Solver.Add(body,description);else simulation.Solver.ApplyDescription(entry.Handle,description);
                }
            }
            entry.Previous=after;
        }
    }
    internal bool Observe(float seconds)
    {
        if(Failed||!float.IsNormal(seconds)||seconds<=0)return false;
        Span<float> impulses=stackalloc float[4];
        foreach(var (key,entry) in entries){
            var manifold=entry.Previous;if(manifold.Count==0)continue;
            Read(manifold.Count,entry.Handle,impulses,out var tangent,out _);
            if(!float.IsFinite(tangent.LengthSquared())){Refuse("Nonfinite solved contact friction.");return false;}
            var n=manifold.Normal;var nw=new Vector3Wide();Vector3Wide.WriteFirst(n,ref nw);
            Helpers.BuildOrthonormalBasis(nw,out var t1,out var t2);
            var total=0d;
            for(var j=0;j<manifold.Count;j++){
                var impulse=impulses[j];var row=manifold[j];
                if(!float.IsFinite(impulse)||impulse<0){Refuse("Invalid solved support impulse.");return false;}
                ContactCount++;MaximumDepth=Math.Max(MaximumDepth,row.Depth);total+=impulse;
                if(impulse==0)continue;
                var point=mass.Com+materialToSite.Conjugate().Rotate(new(row.Offset.X,row.Offset.Y,row.Offset.Z));
                var owner=profile.SupportAt(key.Child,point);
                if(owner<0){if(launchSupport){Refuse("Solved contact is outside uniquely authored support.");return false;}}
                else{footImpulse[owner]+=impulse;footMoment[owner]+=point*impulse;}
            }
            LinearImpulse+=new Double3(n.X*total+(double)t1.X[0]*tangent.X+(double)t2.X[0]*tangent.Y,
                n.Y*total+(double)t1.Y[0]*tangent.X+(double)t2.Y[0]*tangent.Y,n.Z*total+(double)t1.Z[0]*tangent.X+(double)t2.Z[0]*tangent.Y);
        }
        for(var foot=0;foot<footImpulse.Length;foot++){
            var load=footImpulse[foot]/seconds;MaximumLoad=Math.Max(MaximumLoad,load);
            if(!double.IsFinite(load)||launchSupport&&load>profile.Craft.Support[foot].Foot.MaximumLoadN){Refuse($"Authored support load exceeded: {profile.Craft.Support[foot].Part}/{profile.Craft.Support[foot].Foot.Id}, {load:R} N.");return false;}
        }
        return true;
    }
    private void Read(int count,ConstraintHandle h,Span<float> n,out Vector2 t,out float w){
        switch(count){case 1:C1.ReadImpulses(simulation.Solver,h,n,out t,out w);break;case 2:C2.ReadImpulses(simulation.Solver,h,n,out t,out w);break;case 3:C3.ReadImpulses(simulation.Solver,h,n,out t,out w);break;case 4:C4.ReadImpulses(simulation.Solver,h,n,out t,out w);break;default:throw new InvalidDataException("Contact arity.");}
    }
    private void Write(int count,ConstraintHandle h,ReadOnlySpan<float> n,Vector2 t,float w){
        switch(count){case 1:C1.WriteImpulses(simulation.Solver,h,n,t,w);break;case 2:C2.WriteImpulses(simulation.Solver,h,n,t,w);break;case 3:C3.WriteImpulses(simulation.Solver,h,n,t,w);break;case 4:C4.WriteImpulses(simulation.Solver,h,n,t,w);break;default:throw new InvalidDataException("Contact arity.");}
    }
    public void Dispose(){
        ((DefaultTimestepper)simulation.Timestepper).CollisionsDetected-=Reconcile;
        foreach(var entry in entries.Values)if(entry.Previous.Count>0){simulation.Solver.Remove(entry.Handle);entry.Previous=default;}
    }
}
