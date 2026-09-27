using System.Collections.Immutable;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ModularCraftTests
{
    private static void PhysicalContactChecks(CompiledCraft craft)
    {
        var profile=new CompiledCraftContact(craft);var pool=new BufferPool();var metrics=new LocalContactMetrics();
        var simulation=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-9.81f,0)),new SolveDescription(8,1));
        var shape=LocalContactWorld.CreateCraftShape(simulation.Shapes,pool,profile,craft.InitialMass,out var centers);
        var surfaceShape=simulation.Shapes.Add(new Box(32,2,32));var surface=simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),surfaceShape));
        static Vector3 Float(Double3 p)=>new((float)p.X,(float)p.Y,(float)p.Z);
        static Double3 Native(Double3 p)=>new(-p.Y,p.X,p.Z);
        static Double3 Double(Vector3 p)=>new(p.X,p.Y,p.Z);
        var initial=new Vector3(0,(float)(craft.InitialMass.Com.X-profile.SupportPlane),0);
        var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(initial,Quaternion.Identity),new BodyVelocity(default,default),
            LocalContactWorld.CraftInertia(craft.InitialMass),new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
        CraftContactManifolds? contact=null;
        try{
            var solution=CraftSupportPreparation.Prepare(profile,craft.InitialMass,new(0,-9.81,0));
            Check(solution.MaximumObservedLoad<15000&&solution.ForceResidual<1e-8&&solution.MomentResidual<1e-8,"native load witness and independently reconstructed equilibrium fit unchanged ratings");
            var compound=simulation.Shapes.GetShape<Compound>(shape.Index);
            Check(compound.Children.Length==69&&centers.Length==69,"native compound includes every authored convex region");
            for(var i=0;i<compound.Children.Length;i++){
                var child=compound.Children[i];ref var hull=ref simulation.Shapes.GetShape<ConvexHull>(child.ShapeIndex.Index);
                hull.ComputeBounds(Quaternion.Identity,out var low,out var high);low+=child.LocalPosition;high+=child.LocalPosition;
                var points=craft.Collision[i].Vertices.Select(v=>Native(v-craft.InitialMass.Com)).ToArray();
                var expectedLow=new Double3(points.Min(v=>v.X),points.Min(v=>v.Y),points.Min(v=>v.Z));var expectedHigh=new Double3(points.Max(v=>v.X),points.Max(v=>v.Y),points.Max(v=>v.Z));
                Check((Double(low)-expectedLow).LengthSquared<1e-11&&(Double(high)-expectedHigh).LengthSquared<1e-11,"native hull bounds independently reproduce authored geometry");
            }
            contact=new(simulation,body,surface,shape,profile);metrics.Craft=contact;
            var maximumDepth=0f;var maximumTravel=0d;
            for(var i=0;i<600;i++){
                contact.Begin(craft.InitialMass);simulation.Timestep(1f/120);Check(contact.Observe(1f/120),"native child manifolds remain coherent and load-safe");
                maximumDepth=Math.Max(maximumDepth,(float)contact.MaximumDepth);maximumTravel=Math.Max(maximumTravel,(simulation.Bodies[body].Pose.Position-initial).Length());
            }
            Check(maximumTravel<.02&&maximumDepth<.02&&simulation.Bodies[body].Velocity.Linear.Length()<.02,"actual BEPU four-foot support remains bounded for five seconds");
            metrics.Craft=null;contact.Dispose();contact=null;
            var native=simulation.Bodies[body];native.Pose.Orientation=Quaternion.Identity;native.Velocity.Angular=new(.3f,-.2f,.1f);
            native.Pose.Position=Float(new Double3(1,2,3)+Native(craft.InitialMass.Com));native.Velocity.Linear=Float(new Double3(4,5,6)+Double3.Cross(Double(native.Velocity.Angular),Native(craft.InitialMass.Com)));
            var startOrigin=Double(native.Pose.Position)-Native(craft.InitialMass.Com);var startVelocity=Double(native.Velocity.Linear)-Double3.Cross(Double(native.Velocity.Angular),Native(craft.InitialMass.Com));
            var previous=craft.InitialMass;var dp=Double3.Zero;var dv=Double3.Zero;var maxP=0d;var maxV=0d;
            for(var i=0;i<=1000;i++){
                var successor=craft.Mass.Evaluate(craft.Mass.Stores.Select((s,j)=>s.CapacityKg*(j==0?1-i/1000d:1-i/2000d)).ToArray());
                (dp,dv)=LocalContactWorld.RecenterCraftBody(simulation,native,shape,centers,previous,successor,dp,dv,profile.ContactTolerance);
                var p=Double(native.Pose.Position)+dp-Native(successor.Com);var v=Double(native.Velocity.Linear)+dv-Double3.Cross(Double(native.Velocity.Angular),Native(successor.Com));
                maxP=Math.Max(maxP,Math.Sqrt((p-startOrigin).LengthSquared));maxV=Math.Max(maxV,Math.Sqrt((v-startVelocity).LengthSquared));previous=successor;
            }
            Check(maxP<1e-10&&maxV<1e-10,"1001 native unequal-fill recenterings retain material origin without artificial impulse");
            var pose=native.Pose;var velocity=native.Velocity;var inertia=native.LocalInertia;var children=Enumerable.Range(0,compound.Children.Length).Select(i=>compound.Children[i].LocalPosition).ToArray();
            Reject(()=>LocalContactWorld.RecenterCraftBody(simulation,native,shape,centers,previous,previous with{Com=new(double.NaN,0,0)},dp,dv,profile.ContactTolerance),"native recenter malformed proposal refuses before writes");
            Check(native.Pose.Position==pose.Position&&native.Pose.Orientation==pose.Orientation&&native.Velocity.Linear==velocity.Linear&&native.Velocity.Angular==velocity.Angular&&native.LocalInertia.InverseMass==inertia.InverseMass&&Enumerable.Range(0,children.Length).All(i=>compound.Children[i].LocalPosition==children[i]),"native refusal preserves body, inertia and every child");
            Console.WriteLine($"contact {craft.InitialMass.Mass}kg: footLoadMax={solution.MaximumObservedLoad:R}N support={solution.Digest} nativeTravel={maximumTravel:R}m depth={maximumDepth:R}m recenterP={maxP:R}m recenterV={maxV:R}m/s");
        }finally{contact?.Dispose();simulation.Shapes.RecursivelyRemoveAndDispose(shape,pool);simulation.Dispose();pool.Clear();}
    }
}
