using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ScalableLaunchSupportTests
{
    private static int checks;
    private static void Need(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private static void Refuse(Action action,string reason){try{action();}catch(InvalidDataException e){Need(e.Message.Contains(reason,StringComparison.OrdinalIgnoreCase),$"Expected {reason}, received {e.Message}");return;}throw new Exception("Accepted "+reason);}
    internal static void Run()
    {
        checks=0;FrictionMapping();var catalog=RcsScalabilityFixture.Catalog();var saved=catalog.Save();var rows=new List<object>();CompiledCraft? template=null;
        foreach(var tanks in new[]{new[]{"short"},new[]{"long"},new[]{"short","short"},new[]{"long","long"},new[]{"long","long","short"}}){
            var d=ScalableLaunchSupportFixture.Document(catalog,tanks.Select(t=>"nc.tank."+t+"-2").ToArray());var before=d.Save();
            var c=CraftCompiler.Compile(catalog,d.Data,ScalableLaunchSupportFixture.Assets);template??=c;
            var profile=new CompiledCraftContact(c);var proof=CraftSupportPreparation.Prepare(profile,c.InitialMass,new(0,-9.81,0));
            CheckImport(profile,c.InitialMass,2);
            CheckImport(profile,c.InitialMass,8.335569277405739);
            var partialData=d.Data with {Configuration=d.Data.Configuration.Select(v=>v with {
                Stores=v.Stores.Select((s,i)=>s with {QuantityKg=s.QuantityKg*(i==0?.125:.375)}).ToImmutableArray()
            }).ToImmutableArray()};
            var partial=CraftCompiler.Compile(catalog,partialData,ScalableLaunchSupportFixture.Assets);
            CheckImport(new(partial),partial.InitialMass,8.335569277405739);
            var reloaded=CraftCompiler.Compile(catalog,CompiledConstructionDesign.LoadCraft(catalog,before).Data,ScalableLaunchSupportFixture.Assets);
            var again=CraftSupportPreparation.Prepare(new(reloaded),reloaded.InitialMass,new(0,-9.81,0));
            Need(proof.Digest==again.Digest,"deterministic repeated native preparation and save/reload identity");
            Need(before.SequenceEqual(d.Save())&&catalog.Save().SequenceEqual(saved),"preparation preserves source and catalog");
            CheckProof(profile,c.InitialMass,new(-9.81,0,0),proof);
            var times=new List<double>();var allocations=new List<long>();
            for(var i=0;i<12;i++){var allocated=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.GetTimestamp();_=CraftSupportPreparation.Prepare(profile,c.InitialMass,new(0,-9.81,0));times.Add(Stopwatch.GetElapsedTime(watch).TotalMilliseconds);allocations.Add(GC.GetAllocatedBytesForCurrentThread()-allocated);}
            times.Sort();rows.Add(new{tanks,mass=c.InitialMass.Mass,proof.MaximumObservedLoad,proof.MaximumDisplacement,proof.ForceResidual,proof.MomentResidual,proof.Digest,admission=new{median=times[6],p95=times[11],p99=times[11],worst=times[^1],allocatedMedian=allocations.Order().ElementAt(6)}});
        }
        // Analytic isolated rigid-contact fixtures, not new catalog parts or
        // player craft. Aggregate mass/tensor is explicit numerical test data.
        foreach(var n in new[]{3,5,6,9}){
            var c=Numerical(template!,n,n*1150,new(1,.04,-.02));var p=new CompiledCraftContact(c);
            var proof=CraftSupportPreparation.Prepare(p,c.InitialMass,new(.04,-9.79,.07));
            CheckProof(p,c.InitialMass,AssemblyContactProfile.Upright.Conjugate().Rotate(new(.04,-9.79,.07)),proof);
            Need(proof.Reactions.Length==n,"every authored support carries a real solved load");
            if(n>=6)Need(c.InitialMass.Mass*9.79>4*15000,"witness cannot fit a global four-foot load solution");
            Console.WriteLine($"SUPPORT_POPULATION n={n} mass={c.InitialMass.Mass:R} loaded={proof.Reactions.Length} peak={proof.MaximumObservedLoad:R}");
        }
        var six=Numerical(template!,6,6900,new(1,.04,-.02));
        CheckImport(new(six),six.InitialMass,8.335569277405739);
        // Ordinary short convex supports expose independently rounded world,
        // compound-query and child-relative narrow-phase face constructions.
        foreach(var (height,com) in new[]{(.15,6.229096681565805),(.1,4.490313444178518)}){
            var precision=Numerical(template!,6,6900,new(com,0,0));
            precision=Copy(precision,precision.Collision.Select(c=>c with{Vertices=c.Vertices.Select(v=>v with{X=v.X*height/2}).ToImmutableArray()}).ToImmutableArray(),precision.Support,precision.InitialMass);
            CheckImport(new(precision),precision.InitialMass,8.335569277405739);
        }
        var lowCom=Numerical(template!,6,6900,new(5e-7,0,0));
        lowCom=Copy(lowCom,lowCom.Collision.Select(c=>c with{Vertices=c.Vertices.Select(v=>v with{X=v.X*32}).ToImmutableArray()}).ToImmutableArray(),lowCom.Support,lowCom.InitialMass);
        var bounded=Stopwatch.GetTimestamp();CheckImport(new(lowCom),lowCom.InitialMass,8,recenter:false);
        Need(Stopwatch.GetElapsedTime(bounded).TotalSeconds<2,"small-COM import has bounded work rather than millions of adjacent-float trials");
        // Scalar precision boundary independent of gravity or stability: a
        // distant numerical COM tests import representation, not launchability.
        var boundary=32768d+.002/16;
        var lower=Math.BitDecrement(boundary);var upper=Math.BitIncrement(boundary);
        var boundaryCraft=Numerical(template!,6,6900,new(lower,0,0));
        CheckImport(new(boundaryCraft),boundaryCraft.InitialMass,1,recenter:false);
        var beyond=Numerical(template!,6,6900,new(upper,0,0));
        Refuse(()=>CheckImport(new(beyond),beyond.InitialMass,1,recenter:false),"precision envelope");
        Refuse(()=>CraftSupportPreparation.Prepare(new(six),six.InitialMass with{Mass=10000},new(0,-9.81,0)),"total load capacity");
        Refuse(()=>CraftSupportPreparation.Prepare(new(six),six.InitialMass with{Com=new(1,5,0)},new(0,-9.81,0)),"polygon");
        var duplicate=Copy(six,six.Collision,six.Support.Add(six.Support[0] with{Foot=six.Support[0].Foot with{Id="duplicate"}}),six.InitialMass);
        Refuse(()=>new CompiledCraftContact(duplicate),"uniquely authored");
        var penetrated=Copy(six,six.Collision.Add(new("unsupported","below",six.Collision[0].Vertices.Select(v=>v-new Double3(.01,0,0)).ToImmutableArray())),six.Support,six.InitialMass);
        Refuse(()=>new CompiledCraftContact(penetrated),"below authored");
        var weak=Copy(six,six.Collision,six.Support.SetItem(0,six.Support[0] with{Foot=six.Support[0].Foot with{MaximumLoadN=100}}),six.InitialMass);
        Refuse(()=>CraftSupportPreparation.Prepare(new(weak),weak.InitialMass,new(0,-9.81,0)),"load exceeded");
        Console.WriteLine(JsonSerializer.Serialize(new{qualification="SUPPORT_GEOMETRY_LOAD",checks,rows}));
        Console.WriteLine($"SCALABLE_SUPPORT_PASS checks={checks}");
    }
    private static void CheckImport(CompiledCraftContact profile,AssemblyMass mass,double thickness,bool recenter=true)
    {
        var pool=new BufferPool();var metrics=new LocalContactMetrics();
        var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-9.81f,0)),new SolveDescription(8,1));
        BepuPhysics.Collidables.TypedIndex shape=default;CraftContactManifolds? contacts=null;
        try{
            shape=LocalContactWorld.CreateCraftShape(sim.Shapes,pool,profile,mass,out var centers);
            var half=(float)(thickness/2);var slab=sim.Shapes.Add(new Box(64,2*half,48));var surface=sim.Statics.Add(new StaticDescription(new Vector3(0,-half,0),slab));
            var exact=AssemblyContactProfile.Upright.Rotate(mass.Com)-new Double3(0,profile.SupportPlane,0);
            var encoded=CraftSupportImport.Encode(sim.Shapes,shape,profile,exact,half);
            Need(encoded.Y<=(float)exact.Y,"contact encoding never raises the native initial pose");
            var residue=exact-new Double3(encoded.X,encoded.Y,encoded.Z);
            Need(Math.Sqrt(residue.LengthSquared)<=profile.ContactTolerance/16,"total import error is bounded");
            // The selected representation is maximal: if changed, its immediate
            // predecessor in the upward direction must fail the overlap test.
            var above=MathF.BitIncrement(encoded.Y);var aboveFits=true;
            ref var compound=ref sim.Shapes.GetShape<Compound>(shape.Index);
            for(var i=0;i<compound.Children.Length;i++){
                if(!profile.Craft.Collision[i].Vertices.Any(p=>Math.Abs(p.X-profile.SupportPlane)<=1e-12))continue;
                ref var child=ref compound.Children[i];sim.Shapes.GetShape<ConvexHull>(child.ShapeIndex.Index).ComputeBounds(Quaternion.Identity,out var min,out _);
                var bottom=min.Y+child.LocalPosition.Y;
                aboveFits&=bottom+above<=0&&bottom<=half-(above+half)&&min.Y<=half-(child.LocalPosition.Y+(above+half));
            }
            Need(encoded.Y==(float)exact.Y||!aboveFits,"greatest representation satisfying world, query and child face overlaps");
            var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new(encoded,Quaternion.Identity),new BodyVelocity(),LocalContactWorld.CraftInertia(mass),new CollidableDescription(shape,.1f),new(-1)));
            contacts=new(sim,body,surface,shape,profile);metrics.Craft=contacts;
            // Very distant COM scalar fixture is not a contact-geometry probe.
            if(!recenter)return;
            contacts.VerifyInitialCoverage(mass);var native=sim.Bodies[body];
            Need(native.Pose.Position==encoded&&native.Velocity.Linear==Vector3.Zero&&native.Velocity.Angular==Vector3.Zero&&native.Constraints.Count==0,"zero-time import performs no solve or motion");
            Need(new Double3(encoded.X,encoded.Y,encoded.Z)+residue==exact,"exact canonical COM retained");
            var successor=mass with{Com=mass.Com+new Double3(.000037,0,0)};
            var residues=LocalContactWorld.RecenterCraftBody(sim,native,shape,centers,mass,successor,residue,default,profile.ContactTolerance);
            var nextExact=new Double3(native.Pose.Position.X,native.Pose.Position.Y,native.Pose.Position.Z)+residues.Position;
            native.Pose.Position=CraftSupportImport.Encode(sim.Shapes,shape,profile,nextExact,half);
            contacts.VerifyInitialCoverage(successor);
            Need(native.Velocity.Linear==Vector3.Zero&&native.Velocity.Angular==Vector3.Zero&&native.Constraints.Count==0,"immediate mass recenter has no artificial impulse");
            Console.WriteLine($"SUPPORT_IMPORT mass={mass.Mass:R} slab={thickness:R} dy={residue.Y:R} PASS");
        }finally{metrics.Craft=null;contacts?.Dispose();if(shape.Exists)sim.Shapes.RecursivelyRemoveAndDispose(shape,pool);sim.Dispose();pool.Clear();}
    }
    private static void CheckProof(CompiledCraftContact p,AssemblyMass mass,Double3 gravity,CraftSupportSolution proof)
    {
        var force=Double3.Zero;var moment=Double3.Zero;
        foreach(var r in proof.Reactions){var s=p.Craft.Support.Single(s=>s.Part==r.Part&&s.Foot.Id==r.Foot);var local=s.MaterialFrame.Rotation.Transpose().Apply(r.MaterialPoint-s.MaterialFrame.Position);
            Need(Math.Abs(local.X)<1e-12&&Math.Abs(local.Y)<=s.Foot.HalfWidthY&&Math.Abs(local.Z)<=s.Foot.HalfWidthZ,"certificate uses exact finite authored patches");
            Need(r.MaterialForce.X>=0&&r.MaterialForce.X<=s.Foot.MaximumLoadN,"certificate finite compressive rating");
            force+=r.MaterialForce;moment+=Double3.Cross(r.MaterialPoint-mass.Com,r.MaterialForce);
        }
        Need(Math.Sqrt((force+gravity*mass.Mass).LengthSquared)<1e-7&&Math.Sqrt(moment.LengthSquared)<1e-7,"independent force and moment reconstruction");
        Need(proof.MaximumDisplacement<=p.ContactTolerance,"original-pose native probe remains in admitted precision");
    }
    internal static CompiledCraft Numerical(CompiledCraft template,int count,double mass,Double3 com)
    {
        var collision=ImmutableArray.CreateBuilder<CraftCollision>();var support=ImmutableArray.CreateBuilder<CraftSupport>();
        for(var i=0;i<count;i++){
            var angle=2*Math.PI*i/count;var center=new Double3(0,2*Math.Cos(angle),2*Math.Sin(angle));var points=ImmutableArray.CreateBuilder<Double3>();
            foreach(var x in new[]{0d,2d})foreach(var y in new[]{-.15,.15})foreach(var z in new[]{-.15,.15})points.Add(center+new Double3(x,y,z));
            var id="support-"+i;collision.Add(new(id,"column",points.ToImmutable()));var pose=new AssemblyPose(center,Matrix3.Identity);
            support.Add(new(id,new(id,pose,.15,.15,15000,"column","Analytic contact qualification fixture"),pose));
        }
        return Copy(template,collision.ToImmutable(),support.ToImmutable(),new(mass,com,Matrix3.Diagonal(new(mass*3,mass*2,mass*2))));
    }
    private static CompiledCraft Copy(CompiledCraft c,ImmutableArray<CraftCollision> collision,ImmutableArray<CraftSupport> support,AssemblyMass mass)=>
        new(c.Design,c.Dependencies,c.Render,c.Ports,c.Fuel,c.Power,new(mass,[]),mass,collision,[],support,c.Actuators,[]);
}


