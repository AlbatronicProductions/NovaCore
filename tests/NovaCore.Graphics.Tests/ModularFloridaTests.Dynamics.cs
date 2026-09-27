using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

internal static partial class ModularFloridaTests
{
    internal static void RotatingDynamics()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(var longer in new[]{false,true})foreach(var ticks in new[]{999000L,-1000500L,21_999000L})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var session=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab,new(ticks));var p=session.Binding.Physical!;
            var local=new AssemblyMotion(new(2,100,3),new(4,5,6),(AssemblyContactProfile.Upright*DoubleQuaternion.FromAxisAngle(new Double3(1,2,3),.1)).Normalized(),new(.2,-.15,.1));
            var row=p.Control.Resolve(new(true,new(1,-1,1)));var fuel=craft.Fuel.Initial();var power=craft.Power.Initial();
            var services=p.Services.Advance(fuel,power,15625,row.Consumers.AsSpan());Need(services.Phases.Length==1,"single-phase oracle fixture");
            var at=new SimulationInstant(ticks);var target=new SimulationInstant(ticks+15625);
            var result=AssemblyDynamics.Evaluate(p.Control,local,services,default,row,site:p.Site,epoch:at);
            var actual=p.Site.ToEarth(result.Motion,target);var inertial=p.Site.ToEarth(local,at);
            var initial=ConstructionNumerics.Quantities(fuel);var final=ConstructionNumerics.Quantities(services.Fuel);
            const double duration=.015625;const int steps=128;var dt=duration/steps;
            // Independent inertial integration: the already-qualified inertial
            // material-origin equation receives central gravity at the current
            // COM. No rotating-site derivative or pseudo force is used.
            AssemblyMotion D(AssemblyMotion s,double time)
            {
                var values=initial.Select((v,i)=>v+(final[i]-v)*(time/duration)).ToArray();var mass=craft.Mass.EvaluateDynamic(values);
                var com=s.PositionO+s.BodyToWorld.Rotate(mass.Com);var radius=Norm(com);var gravity=com*(-p.Site.Mu/(radius*radius*radius));
                var gimbal=AssemblyActuation.Next(p.Control.Main,default,row.TargetY,row.TargetZ,time,true);
                var wrench=AssemblyActuation.Resolve(craft,services.Phases[0].Active.AsSpan(),gimbal);
                return AssemblyDynamics.CraftDerivative(s,mass,wrench,gravity);
            }
            static AssemblyMotion Add(AssemblyMotion a,AssemblyMotion b,double h)=>new(a.PositionO+b.PositionO*h,a.VelocityO+b.VelocityO*h,
                new(a.BodyToWorld.X+b.BodyToWorld.X*h,a.BodyToWorld.Y+b.BodyToWorld.Y*h,a.BodyToWorld.Z+b.BodyToWorld.Z*h,a.BodyToWorld.W+b.BodyToWorld.W*h),a.AngularVelocityBody+b.AngularVelocityBody*h);
            for(var i=0;i<steps;i++)
            {
                var t=i*dt;var k1=D(inertial,t);var k2=D(Add(inertial,k1,dt*.5),t+dt*.5);var k3=D(Add(inertial,k2,dt*.5),t+dt*.5);var k4=D(Add(inertial,k3,dt),t+dt);
                inertial=Add(Add(Add(Add(inertial,k1,dt/6),k2,dt/3),k3,dt/3),k4,dt/6);inertial=inertial with {BodyToWorld=inertial.BodyToWorld.Normalized()};
            }
            var position=Norm(actual.PositionO-inertial.PositionO);var velocity=Norm(actual.VelocityO-inertial.VelocityO);var omega=Norm(actual.AngularVelocityBody-inertial.AngularVelocityBody);
            var orientation=new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ}.Max(v=>Norm(actual.BodyToWorld.Rotate(v)-inertial.BodyToWorld.Rotate(v)));
            Need(position<5e-8&&velocity<1e-9&&omega<1e-11&&orientation<1e-11,"rotating proposal agrees with independent inertial gravity integration across second boundaries");
            Need(p.Site.At(at,.015625).Omega.LengthSquared<=p.Site.AngularSpeedBound*p.Site.AngularSpeedBound&&p.Site.At(at,.015625).Alpha.LengthSquared<=p.Site.AngularAccelerationBound*p.Site.AngularAccelerationBound,"analytic frame derivative bounds");
            Console.WriteLine($"MODULAR_ROTATING_DYNAMICS mass={craft.InitialMass.Mass:R} epoch={ticks} position={position:R} velocity={velocity:R} omega={omega:R} orientation={orientation:R}");
        }
        Console.WriteLine($"MODULAR_GATE10_DYNAMICS PASS checks={checks}");
    }
}
