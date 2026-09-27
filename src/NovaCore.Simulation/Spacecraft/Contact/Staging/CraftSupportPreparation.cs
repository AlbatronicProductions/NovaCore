using System.Collections.Immutable;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed record CraftSupportReaction(string Part,string Foot,Double3 MaterialPoint,Double3 MaterialForce);
internal sealed record CraftSupportSolution(string Digest,ImmutableArray<CraftSupportReaction> Reactions,
    double MaximumObservedLoad,double MaximumDisplacement,int NativeSteps,double ForceResidual,double MomentResidual);

/// <summary>Disposable initial-pose proof. Neither the settled pose, impulses nor
/// native caches are exported to the player world. A bounded probe is not a proof
/// of perpetual stability: live contacts retain per-substep load enforcement.</summary>
internal static class CraftSupportPreparation
{
    // Explicit preparation work budget (2 s at the production maximum slice),
    // not a craft mass/class/count limit or a claim about universal settling time.
    internal const int PreparationSteps=2048;
    internal const float Step=1f/1024;
    internal static CraftSupportSolution Prepare(CompiledCraftContact profile,AssemblyMass mass,Double3 siteAcceleration,
        AssemblyFloridaSite? site=null,SimulationInstant epoch=default,Double3? supportDimensions=null)
    {
        var gravity=AssemblyContactProfile.Upright.Conjugate().Rotate(siteAcceleration);
        var support=profile.Craft.Support;
        if(!gravity.IsFinite||gravity.X>=0)throw new InvalidDataException("Support requires finite inward gravity.");
        var normalLoad=-mass.Mass*gravity.X;
        if(normalLoad>support.Sum(s=>s.Foot.MaximumLoadN))throw new InvalidDataException("Authored support total load capacity exceeded.");
        // CraftContactDescription maps the physical coefficient to the pinned
        // convex functions' arity convention; physical cone is mu*normal load.
        if(Math.Sqrt(gravity.Y*gravity.Y+gravity.Z*gravity.Z)>-gravity.X*CraftContactManifolds.Material.FrictionCoefficient)
            throw new InvalidDataException("Gravity exceeds the physical support friction cone.");
        var projection=mass.Com+gravity*((profile.SupportPlane-mass.Com.X)/gravity.X);
        CheckPolygon(profile,projection);
        var pool=new BufferPool();var metrics=new LocalContactMetrics();
        var acceleration=new Vector3((float)siteAcceleration.X,(float)siteAcceleration.Y,(float)siteAcceleration.Z);
        var input=site is null?null:new LocalContactStepInput{Site=site,SiteOrigin=new(0,profile.SupportPlane,0),CraftMass=mass,SiteInertia=mass.Inertia};
        var simulation=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(acceleration,input),new SolveDescription(8,1));
        TypedIndex shape=default;CraftContactManifolds? contacts=null;
        try{
            shape=LocalContactWorld.CreateCraftShape(simulation.Shapes,pool,profile,mass,out _);
            var vertices=profile.Craft.Collision.SelectMany(c=>c.Vertices).ToArray();
            var halfX=vertices.Max(v=>Math.Abs(v.Y))+1;var halfZ=vertices.Max(v=>Math.Abs(v.Z))+1;
            var dimensions=site?.Slab?.Dimensions??supportDimensions??new Double3(2*halfX,2,2*halfZ);
            var slab=simulation.Shapes.Add(new Box((float)dimensions.X,(float)dimensions.Y,(float)dimensions.Z));
            var surface=simulation.Statics.Add(new StaticDescription(new Vector3(0,(float)(-dimensions.Y/2),0),slab));
            var nativeCom=AssemblyContactProfile.Upright.Rotate(mass.Com)-new Double3(0,profile.SupportPlane,0);
            var initial=CraftSupportImport.Encode(simulation.Shapes,shape,profile,nativeCom,(float)(dimensions.Y/2));
            var feature=profile.Craft.Support.Min(s=>2*Math.Min(s.Foot.HalfWidthY,s.Foot.HalfWidthZ));
            var margin=(float)(feature*.5+20*profile.ContactTolerance);
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(initial,Quaternion.Identity),new BodyVelocity(),LocalContactWorld.CraftInertia(mass),new CollidableDescription(shape,margin),new BodyActivityDescription(-1)));
            contacts=new(simulation,body,surface,shape,profile);metrics.Craft=contacts;
            contacts.VerifyInitialCoverage(mass);
            var maximum=0d;var travel=0d;
            for(var step=0;step<PreparationSteps;step++){
                if(input is not null){var ticks=(step+.5)*Step*1_000_000;var whole=(long)ticks;input.SiteFrame=site!.At(new(checked(epoch.Ticks+whole)),(ticks-whole)/1_000_000);}
                contacts.Begin(mass);simulation.Timestep(Step);
                if(!contacts.Observe(Step))throw new InvalidDataException($"{contacts.Failure} Initial proof step {step}; gravity={siteAcceleration}.");
                maximum=Math.Max(maximum,contacts.MaximumLoad);
                var state=simulation.Bodies[body];travel=Math.Max(travel,(state.Pose.Position-initial).Length());
                if(travel>profile.ContactTolerance||contacts.MaximumDepth>profile.ContactTolerance)
                    throw new InvalidDataException("Initial support motion leaves the contact precision envelope.");
            }
            var native=simulation.Bodies[body];
            if(native.Velocity.Linear.Length()*Step>profile.ContactTolerance/16||native.Velocity.Angular.Length()*Step*vertices.Max(v=>Math.Sqrt((v-mass.Com).LengthSquared))>profile.ContactTolerance/16)
                throw new InvalidDataException("Initial support preparation has not converged within its work budget.");
            return Certificate(profile,mass,gravity,projection,contacts,maximum,travel);
        }finally{
            metrics.Craft=null;contacts?.Dispose();
            if(shape.Exists)simulation.Shapes.RecursivelyRemoveAndDispose(shape,pool);
            simulation.Dispose();pool.Clear();
        }
    }
    private static CraftSupportSolution Certificate(CompiledCraftContact profile,AssemblyMass mass,Double3 gravity,Double3 projection,
        CraftContactManifolds native,double peak,double travel)
    {
        var total=0d;foreach(var impulse in native.FootImpulses)total+=impulse;
        if(!double.IsFinite(total)||total<=0)throw new InvalidDataException("No load-bearing native support solution.");
        var points=new Double3[profile.Craft.Support.Length];var center=Double3.Zero;
        for(var i=0;i<points.Length;i++){
            var s=profile.Craft.Support[i];var weight=native.FootImpulses[i]/total;
            if(weight==0){points[i]=s.MaterialFrame.Position;continue;}
            var p=s.MaterialFrame.Rotation.Transpose().Apply(native.FootFirstMoments[i]/native.FootImpulses[i]-s.MaterialFrame.Position);
            // Remove only already-checked contact precision error. The certificate
            // itself uses exact authored finite patches, never expanded patches.
            points[i]=s.MaterialFrame.Point(new(0,Math.Clamp(p.Y,-s.Foot.HalfWidthY,s.Foot.HalfWidthY),Math.Clamp(p.Z,-s.Foot.HalfWidthZ,s.Foot.HalfWidthZ)));
            center+=points[i]*weight;
        }
        var correction=new Double3(0,projection.Y-center.Y,projection.Z-center.Z);
        var reactions=ImmutableArray.CreateBuilder<CraftSupportReaction>();var sum=Double3.Zero;var moment=Double3.Zero;
        var forceScale=mass.Mass*Math.Sqrt(gravity.LengthSquared);var lever=1d;
        for(var i=0;i<points.Length;i++){
            var weight=native.FootImpulses[i]/total;if(weight==0)continue;
            var s=profile.Craft.Support[i];var point=points[i]+correction;
            var local=s.MaterialFrame.Rotation.Transpose().Apply(point-s.MaterialFrame.Position);
            if(Math.Abs(local.Y)>s.Foot.HalfWidthY||Math.Abs(local.Z)>s.Foot.HalfWidthZ)
                throw new InvalidDataException("Initial support equilibrium certificate leaves an authored patch.");
            var force=-gravity*(mass.Mass*weight);
            if(force.X>s.Foot.MaximumLoadN)throw new InvalidDataException($"Authored support equilibrium load exceeded: {s.Part}/{s.Foot.Id}.");
            sum+=force;moment+=Double3.Cross(point-mass.Com,force);lever=Math.Max(lever,Math.Sqrt((point-mass.Com).LengthSquared));
            reactions.Add(new(s.Part,s.Foot.Id,point,force));
        }
        var forceResidual=Math.Sqrt((sum+gravity*mass.Mass).LengthSquared);var momentResidual=Math.Sqrt(moment.LengthSquared);
        // Binary64 summation/reconstruction allowance scales with actual work;
        // no physical force/moment slack or extra capacity is granted.
        var rounding=128d*Math.ScaleB(1d,-52)*(points.Length+1);
        if(forceResidual>rounding*forceScale||momentResidual>rounding*forceScale*lever)
            throw new InvalidDataException("Initial support force/moment reconstruction failed.");
        var result=reactions.ToImmutable();var digest=AssemblyJson.Digest(new{profile.Craft.Digest,mass,gravity,profile.SupportPlane,Reactions=result});
        return new(digest,result,peak,travel,PreparationSteps,forceResidual,momentResidual);
    }
    private static void CheckPolygon(CompiledCraftContact profile,Double3 point)
    {
        var points=profile.Craft.Support.SelectMany(s=>new[]{s.MaterialFrame.Point(new(0,-s.Foot.HalfWidthY,-s.Foot.HalfWidthZ)),s.MaterialFrame.Point(new(0,-s.Foot.HalfWidthY,s.Foot.HalfWidthZ)),s.MaterialFrame.Point(new(0,s.Foot.HalfWidthY,-s.Foot.HalfWidthZ)),s.MaterialFrame.Point(new(0,s.Foot.HalfWidthY,s.Foot.HalfWidthZ))}).Distinct().OrderBy(p=>p.Y).ThenBy(p=>p.Z).ToArray();
        static double Cross(Double3 a,Double3 b,Double3 c)=>(b.Y-a.Y)*(c.Z-a.Z)-(b.Z-a.Z)*(c.Y-a.Y);
        var hull=new List<Double3>();foreach(var p in points){while(hull.Count>=2&&Cross(hull[^2],hull[^1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        var lower=hull.Count;for(var i=points.Length-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[^2],hull[^1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        if(hull.Count<4)throw new InvalidDataException("Degenerate support polygon.");
        for(var i=1;i<hull.Count;i++)if(Cross(hull[i-1],hull[i],point)<=0)throw new InvalidDataException("Gravity line leaves the interior support polygon.");
    }
}
