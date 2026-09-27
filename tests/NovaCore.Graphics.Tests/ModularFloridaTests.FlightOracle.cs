using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularFloridaTests
{
    private static AssemblyMotion FlightOracle(ConstructionApplicationSession session,ConstructionRuntimeState initial,ConstructionRuntimeState final)
    {
        var profile=session.Binding.Physical!;var site=profile.Site;var craft=profile.Craft;
        var state=site.ToEarth(initial.Physical!.Motion,initial.Epoch);
        var before=ConstructionNumerics.Quantities(initial.Fuel);var after=ConstructionNumerics.Quantities(final.Fuel);
        var duration=(final.Epoch.Ticks-initial.Epoch.Ticks)/1e6;var count=(int)Math.Ceiling(duration*256);var dt=duration/count;
        // Independent inertial integration over the complete post-handoff arc.
        // No rotating derivative, site pseudo-force, or production RK routine.
        AssemblyMotion D(AssemblyMotion s,double t)
        {
            var mass=craft.Mass.EvaluateDynamic(before.Select((x,i)=>x+(after[i]-x)*(t/duration)).ToArray());
            var q=s.BodyToWorld;var w=s.AngularVelocityBody;var f=profile.Control.Main.Axis*profile.Control.Main.Thrust;
            var tau=Double3.Cross(profile.Control.Main.Point-mass.Com,f);
            var alpha=mass.Inertia.Inverse().Apply(tau-Double3.Cross(w,mass.Inertia.Apply(w)));
            var com=s.PositionO+q.Rotate(mass.Com);var r=Norm(com);var gravity=com*(-site.Mu/(r*r*r));
            var a=q.Rotate(f/mass.Mass-Double3.Cross(alpha,mass.Com)-Double3.Cross(w,Double3.Cross(w,mass.Com)))+gravity;
            var dq=q*new DoubleQuaternion(w.X,w.Y,w.Z,0);
            return new(s.VelocityO,a,new(dq.X*.5,dq.Y*.5,dq.Z*.5,dq.W*.5),alpha);
        }
        static AssemblyMotion Add(AssemblyMotion s,AssemblyMotion d,double h)=>new(s.PositionO+d.PositionO*h,s.VelocityO+d.VelocityO*h,
            new(s.BodyToWorld.X+d.BodyToWorld.X*h,s.BodyToWorld.Y+d.BodyToWorld.Y*h,s.BodyToWorld.Z+d.BodyToWorld.Z*h,s.BodyToWorld.W+d.BodyToWorld.W*h),s.AngularVelocityBody+d.AngularVelocityBody*h);
        for(var i=0;i<count;i++)
        {
            var t=i*dt;var k1=D(state,t);var k2=D(Add(state,k1,dt/2),t+dt/2);var k3=D(Add(state,k2,dt/2),t+dt/2);var k4=D(Add(state,k3,dt),t+dt);
            state=Add(Add(Add(Add(state,k1,dt/6),k2,dt/3),k3,dt/3),k4,dt/6);state=state with {BodyToWorld=state.BodyToWorld.Normalized()};
        }
        return state;
    }
}
