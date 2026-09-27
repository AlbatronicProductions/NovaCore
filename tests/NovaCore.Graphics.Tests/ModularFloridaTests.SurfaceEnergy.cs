using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ModularFloridaTests
{
    // Falsification test, not a theorem of solver passivity. Compare canonical
    // rotating-frame mechanical energy with the unpowered initial endpoint.
    // The allowance is declared from two existing 2mm spatial uncertainties
    // and bounded Earth-frame acceleration, never fitted to observed motion.
    private sealed class SurfaceEnergy
    {
        private readonly AssemblyFloridaSite site;
        private readonly ConstructionRuntimeState initial;
        private readonly AssemblyMass mass;
        private readonly Double3 r0,v0,w0,o0,c0;
        private readonly double radius0,spatial,framePower;
        internal double MaximumGain {get;private set;}=double.NegativeInfinity;
        internal int MaximumAt {get;private set;}
        internal double Allowance {get;private set;}
        internal SurfaceEnergy(ConstructionApplicationSession session)
        {
            site=session.Binding.Physical!.Site;initial=Observe(session);mass=initial.ReferenceMass!.Value;
            var motion=initial.Physical!.Motion;var frame=site.At(initial.Epoch);
            r0=site.LocalToBodyFixed.Conjugate().Rotate(site.OriginBodyFixed)+motion.PositionO+motion.BodyToWorld.Rotate(mass.Com);
            v0=motion.VelocityO+motion.BodyToWorld.Rotate(Double3.Cross(motion.AngularVelocityBody,mass.Com));
            w0=motion.AngularVelocityBody;o0=motion.BodyToWorld.Conjugate().Rotate(frame.Omega);c0=Double3.Cross(frame.Omega,r0);radius0=Norm(r0);
            var config=LocalContactConfiguration.CreateCraft(session.Binding,true,refinement:CraftSurfaceImpact.MaximumRefinement);
            var reach=Norm(config.OriginRoot)+Math.Sqrt(3)*config.MaximumCoordinate;var root=Norm(site.OriginBodyFixed);
            var rmin=root-reach;var rmax=root+reach;var omega=site.AngularSpeedBound;
            Need(rmin>0,"energy domain stays outside gravity singularity");
            spatial=mass.Mass*(site.Mu/(rmin*rmin)+omega*omega*rmax)*2*config.ContactTolerance;
            // Positive inertia: trace bounds its operator norm.
            var inertia=mass.Inertia.A+mass.Inertia.E+mass.Inertia.I;
            framePower=site.AngularAccelerationBound*(mass.Mass*rmax*(config.MaximumSpeed+omega*rmax)+inertia*(config.MaximumAngularSpeed+omega));
        }
        internal void Sample(ConstructionRuntimeState state,int step)
        {
            Need(state.ReferenceMass==mass,"passive energy case retains physical mass/inertia");
            var motion=state.Physical!.Motion;var frame=site.At(state.Epoch);var start=initial.Physical!.Motion;
            var dr=motion.PositionO-start.PositionO+motion.BodyToWorld.Rotate(mass.Com)-start.BodyToWorld.Rotate(mass.Com);
            var radius=Norm(r0+dr);var v=motion.VelocityO+motion.BodyToWorld.Rotate(Double3.Cross(motion.AngularVelocityBody,mass.Com));
            var w=motion.AngularVelocityBody;var o=motion.BodyToWorld.Conjugate().Rotate(frame.Omega);
            var dc=Double3.Cross(frame.Omega,dr)+Double3.Cross(frame.Omega-site.At(initial.Epoch).Omega,r0);
            static double SquareDifference(Double3 before,Double3 delta)=>2*Double3.Dot(before,delta)+delta.LengthSquared;
            double TensorDifference(Double3 before,Double3 delta)=>2*Double3.Dot(before,mass.Inertia.Apply(delta))+Double3.Dot(delta,mass.Inertia.Apply(delta));
            var kinetic=.5*mass.Mass*SquareDifference(v0,v-v0)+.5*TensorDifference(w0,w-w0);
            var gravity=mass.Mass*site.Mu*(2*Double3.Dot(r0,dr)+dr.LengthSquared)/((radius+radius0)*radius*radius0);
            var centrifugal=-.5*mass.Mass*SquareDifference(c0,dc)-.5*TensorDifference(o0,o-o0);
            var change=kinetic+gravity+centrifugal;
            Allowance=spatial+framePower*((state.Epoch.Ticks-initial.Epoch.Ticks)/1e6);
            if(change>MaximumGain){MaximumGain=change;MaximumAt=step;}
            Need(double.IsFinite(change)&&change<=Allowance,$"unpowered compound energy gain {change:R} J exceeds spatial/frame envelope {Allowance:R} J at {step}");
        }
    }
}
