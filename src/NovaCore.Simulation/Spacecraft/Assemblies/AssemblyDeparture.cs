using NovaCore.Core;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>
/// Bounded one-way continuation from the authored local slab. Initial separating
/// velocity is qualification data, not an impulse, engine upgrade or liftoff claim.
/// The original frame transport is independent of vehicle-relative motion.
/// </summary>
internal sealed class AssemblyDepartureProfile
{
    internal const string Identity = "srv01-local-slab-separation/1";
    internal static Double3 Gravity => new(0, -9.81, 0);
    internal Double3 FrameVelocity { get; }
    private readonly double radiusO, comBound, inverseBound, inertiaBound, forceBound;

    internal AssemblyDepartureProfile(AssemblyContactProfile profile, Double3 frameVelocity)
    {
        FrameVelocity = frameVelocity;
        var d = profile.Design;
        var dry = d.ObserveMass(d.DryMass);
        var wet = d.ObserveMass(d.DryMass + 100);
        // I(M)=IO-Parallel(S)/M increases in PSD order with M. These full-matrix
        // Frobenius bounds cover the entire admitted mass interval, not only a stage.
        inverseBound = Norm(dry.Inertia.Inverse());
        inertiaBound = Norm(wet.Inertia);
        comBound = Math.Sqrt(dry.Com.LengthSquared);
        var wrench = AssemblyActuation.Main(d, 0, 0);
        if (wrench.MomentAtOrigin != Double3.Zero || Double3.Cross(d.FirstMoment, wrench.Force) != Double3.Zero)
            throw new InvalidDataException("Departure profile requires coaxial fixed main and COM.");
        forceBound = Math.Sqrt(wrench.Force.LengthSquared) / d.DryMass;
        foreach (var child in profile.Children)
            for (var i = 0; i < 8; i++)
                radiusO = Math.Max(radiusO, Math.Sqrt(AssemblyContactProfile.CornerAtOrigin(child, i).LengthSquared));
    }

    private static double Norm(Matrix3 m) => Math.Sqrt(m.A*m.A+m.B*m.B+m.C*m.C+m.D*m.D+m.E*m.E+m.F*m.F+m.G*m.G+m.H*m.H+m.I*m.I);

    // Conservative narrow-profile horizon, not M14.17 EventFreeThroughTarget evidence.
    // Rotation is bounded by |w'| <= ||I^-1|| ||I|| |w|^2 (zero applied torque).
    // The 2*tolerance allowance retains the existing local representation envelope;
    // endpoint and independent numerical-oracle tests additionally qualify RK4 here.
    internal bool Clears(AssemblyLaunch launch, in AssemblyRuntimeState source, long ticks,
        double tolerance, bool requireOutward, out double lowerBound)
    {
        lowerBound = double.NegativeInfinity;
        if (ticks <= 0 || ticks > 15625 || source.Gimbal != default ||
            !AssemblyDynamics.InEnvelope(source.Motion) || !double.IsFinite(tolerance) || tolerance <= 0)
            return false;
        var h = ticks / 1_000_000d;
        var omega0 = Math.Sqrt(source.Motion.AngularVelocityBody.LengthSquared);
        // Strictly narrow small-rotation profile; no claim for general tumbling bodies.
        if (omega0 > .0001) return false;
        var omega = 2 * omega0 + 1e-12;
        var alpha = inverseBound * inertiaBound * omega * omega;
        if (omega0 + h * alpha >= omega) return false;
        var acceleration = 9.81 + forceBound + (alpha + omega * omega) * comBound;
        var motion = source.Motion;
        var velocity = motion.VelocityO - FrameVelocity;
        var origin = motion.PositionO - FrameVelocity * ((source.Epoch.Ticks - launch.Initial.Epoch.Ticks) / 1_000_000d);
        var rotationalSpeed = radiusO * omega;
        if (requireOutward && velocity.Y <= rotationalSpeed + tolerance / h) return false;
        var minimum = double.MaxValue;
        foreach (var child in launch.ContactProfile!.Children)
        for (var corner = 0; corner < 8; corner++)
        {
            var p = origin + motion.BodyToWorld.Rotate(AssemblyContactProfile.CornerAtOrigin(child, corner));
            minimum = Math.Min(minimum, p.Y - AssemblyContactProfile.SupportPlaneAtOrigin);
            var travelX = (Math.Abs(velocity.X) + rotationalSpeed) * h + .5 * acceleration * h * h;
            var travelZ = (Math.Abs(velocity.Z) + rotationalSpeed) * h + .5 * acceleration * h * h;
            if (!p.IsFinite || Math.Abs(p.X) + travelX + 2 * tolerance >= 8 || Math.Abs(p.Z) + travelZ + 2 * tolerance >= 8)
                return false;
        }
        // Concavity places the minimum of this lower envelope at an endpoint.
        lowerBound = minimum + Math.Min(0, (velocity.Y - rotationalSpeed) * h - .5 * acceleration * h * h) - 2 * tolerance;
        return double.IsFinite(lowerBound) && lowerBound > 0;
    }

    internal bool EndpointClear(AssemblyLaunch launch, in AssemblyRuntimeState state, double tolerance)
    {
        var origin = state.Motion.PositionO - FrameVelocity * ((state.Epoch.Ticks - launch.Initial.Epoch.Ticks) / 1_000_000d);
        foreach (var child in launch.ContactProfile!.Children)
        for (var i = 0; i < 8; i++)
        {
            var p = origin + state.Motion.BodyToWorld.Rotate(AssemblyContactProfile.CornerAtOrigin(child, i));
            if (!p.IsFinite || p.Y - AssemblyContactProfile.SupportPlaneAtOrigin <= 2 * tolerance || Math.Abs(p.X) >= 8 || Math.Abs(p.Z) >= 8)
                return false;
        }
        return true;
    }
}
