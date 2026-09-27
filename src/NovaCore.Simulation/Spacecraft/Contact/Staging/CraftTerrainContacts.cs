using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Observes the standard BEPU terrain constraint after mesh smoothing.
/// Native contact reduction/impulses remain solver owned.</summary>
internal sealed class CraftTerrainContacts(BepuPhysics.Simulation simulation,BodyHandle body,int craftChildren)
{
    private StaticHandle surface;
    private int surfaceChildren;
    private long generation;
    private Vector3 terrainUp;
    // Current KSA terrain-patch acceptance cone. This is a terrain-only
    // recovery-direction test, not a spring/depth modification. Our mesh
    // preparation independently refuses faces outside this same cone.
    internal const float MinimumTerrainAlignment=.1f;
    internal static bool AcceptsRecovery(Vector3 normal,Vector3 up)=>Vector3.Dot(normal,up)>=MinimumTerrainAlignment;
    private readonly BepuPhysics.CollisionDetection.Contact[] rows=new BepuPhysics.CollisionDetection.Contact[4];
    private int count;
    private bool seen;
    private CollidablePair pair;
    internal int ContactCount {get;private set;}
    internal double MaximumDepth {get;private set;}
    internal Double3 LinearImpulse {get;private set;}
    internal readonly record struct Witness(int Child,int Triangle,Vector3 Offset,Vector3 Normal,float Depth,RigidPose Pose,BodyVelocity Velocity);
    internal Witness Deepest {get;private set;}
    private List<Witness>? rawDiagnostics;
    internal void CaptureForTest()=>rawDiagnostics=new(8192);
    internal IReadOnlyList<Witness>? RawForTest=>rawDiagnostics;
    internal void Register(StaticHandle handle,int children,Vector3 up)
    {
        if(children<=0||!float.IsFinite(up.LengthSquared())||Math.Abs(up.LengthSquared()-1)>1e-5)throw new InvalidDataException("Terrain surface child identity.");
        surface=handle;surfaceChildren=children;terrainUp=up;generation=checked(generation+1);Begin();
    }
    internal bool Owns(CollidablePair p)=>surfaceChildren>0&&p.A.Mobility==CollidableMobility.Dynamic&&p.A.BodyHandle==body&&
        p.B.Mobility==CollidableMobility.Static&&p.B.StaticHandle==surface;
    internal void Begin(){count=0;seen=false;ContactCount=0;MaximumDepth=0;LinearImpulse=default;rawDiagnostics?.Clear();}
    internal bool Child(int worker,CollidablePair p,int a,int b,ref ConvexContactManifold manifold)
    {
        if(worker!=0||!Owns(p)||(uint)a>=craftChildren||(uint)b>=surfaceChildren||manifold.Count is <0 or >4||
            (manifold.Count>0&&(!float.IsFinite(manifold.Normal.LengthSquared())||Math.Abs(manifold.Normal.LengthSquared()-1)>1e-4)))
            throw new InvalidDataException("Terrain child ownership mismatch.");
        // A is the dynamic craft and B the terrain static (Owns above), so the
        // native normal already points along the craft's recovery direction.
        // Reject tangential/backface terrain rows before native mesh smoothing
        // can rotate an edge normal while retaining its lateral overlap depth.
        // Pad and spacecraft/object side contacts never enter this owner.
        if(!AcceptsRecovery(manifold.Normal,terrainUp)){manifold.Count=0;return false;}
        for(var i=0;i<manifold.Count;i++)
        {
            if(!float.IsFinite(manifold[i].Depth)||!float.IsFinite(manifold[i].Offset.LengthSquared()))
                throw new InvalidDataException("Nonfinite terrain child contact.");
            var witness=new Witness(a,b,manifold[i].Offset,manifold.Normal,manifold[i].Depth,simulation.Bodies[body].Pose,simulation.Bodies[body].Velocity);
            if(witness.Depth>MaximumDepth){MaximumDepth=witness.Depth;Deepest=witness;}
            if(rawDiagnostics is {} capture){if(capture.Count==8192)throw new InvalidDataException("Qualification raw contact capacity.");capture.Add(witness);}
        }
        return true; // Mesh normal filtering/reduction has not happened yet.
    }
    internal bool Parent<T>(int worker,CollidablePair p,ref T manifold)where T:unmanaged,IContactManifold<T>
    {
        if(worker!=0||!Owns(p)||seen||manifold.Count is <0 or >4)throw new InvalidDataException("Terrain parent ownership mismatch.");
        seen=true;pair=p;count=manifold.Count;var velocity=simulation.Bodies[body].Velocity;
        for(var i=0;i<count;i++)
        {
            manifold.GetContact(i,out rows[i]);var row=rows[i];
            if(!float.IsFinite(row.Depth)||!float.IsFinite(row.Offset.LengthSquared())||!float.IsFinite(row.Normal.LengthSquared())||
                Math.Abs(row.Normal.LengthSquared()-1)>1e-4||
                !CraftSurfaceImpact.FiniteContactSpeed(-Vector3.Dot(velocity.Linear+Vector3.Cross(velocity.Angular,row.Offset),row.Normal)))
                throw new InvalidDataException("Unqualified terrain impact/contact row.");
        }
        return true; // Keep the ordinary native constraint and its feature cache.
    }
    internal void VerifyGeometry()
    {
        if(!double.IsFinite(MaximumDepth))throw new InvalidDataException("Nonfinite terrain contact depth.");
    }
    internal void Observe()
    {
        VerifyGeometry();if(count==0)return;
        var index=simulation.NarrowPhase.PairCache.IndexOf(pair);
        if(index<0)throw new InvalidDataException("Missing solved terrain pair.");
        ref var cache=ref simulation.NarrowPhase.PairCache.GetCache(index);
        for(var i=0;i<count;i++)
        {
            var id=i switch{0=>cache.FeatureId0,1=>cache.FeatureId1,2=>cache.FeatureId2,_=>cache.FeatureId3};
            if(id!=rows[i].FeatureId)throw new InvalidDataException("Terrain contact feature frontier mismatch.");
        }
        var extract=new Extractor(this,generation);
        if(!simulation.NarrowPhase.TryExtractSolverContactData(cache.ConstraintHandle,ref extract)||extract.Read!=count)
            throw new InvalidDataException("Terrain constraint extraction mismatch.");
        ContactCount=count;
    }
    private void Accept(long expected,int i,BodyHandle owner,Vector3 offset,Vector3 normal,float depth,float impulse,Vector2 tangent)
    {
        if(expected!=generation||owner!=body||i>=count||!float.IsFinite(impulse)||impulse<0||
            !float.IsFinite(tangent.LengthSquared())||offset!=rows[i].Offset||normal!=rows[i].Normal||depth!=rows[i].Depth)
            throw new InvalidDataException("Terrain solved row identity or impulse mismatch.");
        var wide=new Vector3Wide();Vector3Wide.WriteFirst(normal,ref wide);Helpers.BuildOrthonormalBasis(wide,out var t1,out var t2);
        LinearImpulse+=new Double3(normal.X*(double)impulse+t1.X[0]*(double)tangent.X+t2.X[0]*(double)tangent.Y,
            normal.Y*(double)impulse+t1.Y[0]*(double)tangent.X+t2.Y[0]*(double)tangent.Y,
            normal.Z*(double)impulse+t1.Z[0]*(double)tangent.X+t2.Z[0]*(double)tangent.Y);
    }
    private struct Extractor(CraftTerrainContacts target,long expected):ISolverContactDataExtractor
    {
        internal int Read;
        public void ConvexOneBody<TP,TI>(BodyHandle b,ref TP p,ref TI impulse)
            where TP:struct,IConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI>
        {
            Vector3Wide.ReadFirst(TP.GetNormal(ref p),out var normal);Vector2Wide.ReadFirst(TI.GetTangentFriction(ref impulse),out var tangent);
            for(var i=0;i<TP.ContactCount;i++)
            {
                ref var row=ref TP.GetContact(ref p,i);Vector3Wide.ReadFirst(row.OffsetA,out var offset);
                target.Accept(expected,i,b,offset,normal,row.Depth[0],TI.GetPenetrationImpulseForContact(ref impulse,i)[0],i==0?tangent:default);Read++;
            }
        }
        public void NonconvexOneBody<TP,TI>(BodyHandle b,ref TP p,ref TI impulse)
            where TP:struct,INonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI>
        {
            for(var i=0;i<TP.ContactCount;i++)
            {
                ref var row=ref TP.GetContact(ref p,i);Vector3Wide.ReadFirst(row.Offset,out var offset);Vector3Wide.ReadFirst(row.Normal,out var normal);
                ref var solved=ref TI.GetImpulsesForContact(ref impulse,i);Vector2Wide.ReadFirst(solved.Tangent,out var tangent);
                target.Accept(expected,i,b,offset,normal,row.Depth[0],solved.Penetration[0],tangent);Read++;
            }
        }
        public void ConvexTwoBody<TP,TI>(BodyHandle a,BodyHandle b,ref TP p,ref TI impulse)
            where TP:struct,ITwoBodyConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI>
            =>throw new InvalidDataException("Terrain cannot own a two-body contact.");
        public void NonconvexTwoBody<TP,TI>(BodyHandle a,BodyHandle b,ref TP p,ref TI impulse)
            where TP:struct,ITwoBodyNonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI>
            =>throw new InvalidDataException("Terrain cannot own a two-body contact.");
    }
}
