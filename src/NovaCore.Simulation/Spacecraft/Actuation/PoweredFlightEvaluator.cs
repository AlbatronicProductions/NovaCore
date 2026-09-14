using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Actuation;

internal readonly record struct PoweredKinematics(Double3 Position, Double3 Velocity, DoubleQuaternion Orientation, Double3 AngularVelocity);
internal enum PoweredEvaluationStatus { Invalid, Success, OutsideModel, InvalidSegmentation, NumericalFailure }

/// <summary>
/// Bounded dry-8 kg spherical-inertia fixture, no environment. Coupled RK4 in exact-segment local time.
/// Consumes copied conditional values only; the transaction owner separately qualifies genuine leases.
/// </summary>
internal static class PoweredFlightEvaluator
{
    internal const int RegularSteps = 128;
    internal const double PositionError = 1e-6, VelocityError = 1e-6, OrientationError = 1e-9, AngularVelocityError = 1e-9;
    internal static bool InModel(in PoweredKinematics value) => value.Position.IsFinite && value.Velocity.IsFinite &&
        value.AngularVelocity.IsFinite && value.Orientation.IsFinite &&
        Math.Max(Math.Abs(value.Position.X), Math.Max(Math.Abs(value.Position.Y), Math.Abs(value.Position.Z))) <= 1000 &&
        MagnitudeFits(value.Velocity, 100) && MagnitudeFits(value.AngularVelocity, 1.1) &&
        Math.Abs(value.Orientation.LengthSquared - 1) <= 1e-12;

    internal static PoweredEvaluationStatus Evaluate(in PoweredKinematics source, in PropellantSegmentationPreview proposal,
        out PoweredKinematics endpoint, out int steps)
    {
        endpoint = default; steps = 0;
        var definition = proposal.Resource.Definition;
        var ticks = (Int128)proposal.Engine.End.Ticks - proposal.Engine.Start.Ticks;
        if (!InModel(source) || definition.DryMassKilograms != 8 || definition.DryInertia != new PrincipalMomentsOfInertia(2, 2, 2) ||
            !proposal.Resource.InitialUnits.TryToKilograms(out var initialFuel) || initialFuel > 1d / 128 ||
            ticks <= 0 || ticks > 16667 || !proposal.Engine.ProposedForceBodyNewtons.IsFinite ||
            !proposal.Engine.ProposedMomentBodyNewtonMetres.IsFinite || !MagnitudeFits(proposal.Engine.ProposedForceBodyNewtons, 9000) ||
            !MagnitudeFits(proposal.Engine.ProposedMomentBodyNewtonMetres, 2))
            return PoweredEvaluationStatus.OutsideModel;
        if (proposal.SegmentCount is < 1 or > 2 || !PropellantInteger.TryFromKilograms(8, out var dry))
            return PoweredEvaluationStatus.InvalidSegmentation;
        // An upper bound through the entire interval, rounded away from the admission edge.
        // The positive sum/norm/product path has fewer than sixteen roundings. Inflation plus
        // next-up covers them, including zero/subnormal norms (which cannot approach this limit).
        var omegaBound = Math.BitIncrement((Math.Sqrt(source.AngularVelocity.LengthSquared) +
            (double)ticks / 1_000_000 * Math.Sqrt(proposal.Engine.ProposedMomentBodyNewtonMetres.LengthSquared) / 2) * (1 + 32 * Math.ScaleB(1d, -53)));
        if (omegaBound > 1.1) return PoweredEvaluationStatus.OutsideModel;
        var state = source;
        for (var segmentIndex = 0; segmentIndex < proposal.SegmentCount; segmentIndex++)
        {
            if (!proposal.TryGetSegment(segmentIndex, out var segment) || segment.Duration.IsZero ||
                !PoweredFlightNumerics.TrySeconds(segment.Duration, out var h)) return PoweredEvaluationStatus.InvalidSegmentation;
            var count = h.Exponent < -40 || (h.Exponent == -40 && h.Significand == 1) ? 1 : RegularSteps;
            var stepScale = h.DividePowerOfTwo(count == 1 ? 0 : 7);
            var initial = state;
            var sumP = Double3.Zero; var sumV = Double3.Zero; var sumW = Double3.Zero;
            for (var j = 0; j < count; j++)
            {
                double m1, m2, m4;
                if (segment.Powered)
                {
                    if (!PoweredFlightNumerics.TryStageMass(dry, proposal.SuccessorUnits, proposal.ConsumedUnits, (uint)(2*j), (uint)(2*count), out m1) ||
                        !PoweredFlightNumerics.TryStageMass(dry, proposal.SuccessorUnits, proposal.ConsumedUnits, (uint)(2*j+1), (uint)(2*count), out m2) ||
                        !PoweredFlightNumerics.TryStageMass(dry, proposal.SuccessorUnits, proposal.ConsumedUnits, (uint)(2*j+2), (uint)(2*count), out m4))
                        return PoweredEvaluationStatus.NumericalFailure;
                }
                else m1 = m2 = m4 = proposal.ProposedSuccessorMass.TotalMassKilograms;
                if (!Derivative(state, segment, m1, out var k1) ||
                    !Derivative(Add(state, k1, stepScale, 2), segment, m2, out var k2) ||
                    !Derivative(Add(state, k2, stepScale, 2), segment, m2, out var k3) ||
                    !Derivative(Add(state, k3, stepScale, 1), segment, m4, out var k4)) return PoweredEvaluationStatus.NumericalFailure;
                // Sum unweighted, bounded derivatives before applying H/(6N). Tiny component impulses
                // are not individually rounded away N times; the endpoint is projected from the source.
                sumP += Combine(k1.Position, k2.Position, k3.Position, k4.Position);
                sumV += Combine(k1.Velocity, k2.Velocity, k3.Velocity, k4.Velocity);
                sumW += Combine(k1.AngularVelocity, k2.AngularVelocity, k3.AngularVelocity, k4.AngularVelocity);
                var rawQ = Add(state.Orientation, Combine(k1.Orientation, k2.Orientation, k3.Orientation, k4.Orientation), stepScale, 6);
                if (SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(rawQ, out var q) != SpacecraftRigidBodyRotationEvaluationStatus.Success)
                    return PoweredEvaluationStatus.NumericalFailure;
                state = new(initial.Position + Scale(sumP, stepScale, 6), initial.Velocity + Scale(sumV, stepScale, 6),
                    q, initial.AngularVelocity + Scale(sumW, stepScale, 6));
                if (!state.Position.IsFinite || !state.Velocity.IsFinite || !state.AngularVelocity.IsFinite ||
                    state.Velocity.LengthSquared > 120 * 120) return PoweredEvaluationStatus.NumericalFailure;
            }
            steps += count;
        }
        if (!InModel(state)) return PoweredEvaluationStatus.OutsideModel;
        endpoint = state; return PoweredEvaluationStatus.Success;
    }

    internal static bool MagnitudeFits(Double3 value, double limit)
    {
        if (!value.IsFinite) return false;
        // Preserve exact authored axis-aligned boundary values. Otherwise use directed upper
        // squares/sums and a lower bound on limit^2; no rounded-down norm can admit an overrun.
        if (value.Y == 0 && value.Z == 0) return Math.Abs(value.X) <= limit;
        if (value.X == 0 && value.Z == 0) return Math.Abs(value.Y) <= limit;
        if (value.X == 0 && value.Y == 0) return Math.Abs(value.Z) <= limit;
        var x = Math.BitIncrement(value.X * value.X); var y = Math.BitIncrement(value.Y * value.Y); var z = Math.BitIncrement(value.Z * value.Z);
        var square = Math.BitIncrement(Math.BitIncrement(x + y) + z);
        return square <= Math.BitDecrement(limit * limit);
    }

    private static bool Derivative(in PoweredKinematics state, in PropellantSegment segment, double mass, out PoweredKinematics slope)
    {
        slope = default;
        var norm = state.Orientation.LengthSquared;
        if (!double.IsFinite(norm) || norm < .99 * .99 || norm > 1.01 * 1.01 || mass < 8 || mass > 8 + 1d/128)
            return false;
        // Identical scalar-last Hamilton/body-rate convention to the banked rotation evaluator.
        var qd = state.Orientation * new DoubleQuaternion(state.AngularVelocity.X, state.AngularVelocity.Y, state.AngularVelocity.Z, 0);
        qd = new(qd.X * .5, qd.Y * .5, qd.Z * .5, qd.W * .5);
        // Spherical I=(2,2,2): omega cross I*omega is identically zero. No extra exhaust impulse.
        slope = new(state.Velocity, state.Orientation.Rotate(segment.EngineForceBodyNewtons) / mass, qd,
            segment.EngineMomentBodyNewtonMetres / 2);
        return slope.Position.IsFinite && slope.Velocity.IsFinite && slope.Orientation.IsFinite && slope.AngularVelocity.IsFinite;
    }
    private static Double3 Combine(Double3 a, Double3 b, Double3 c, Double3 d) => a + b*2 + c*2 + d;
    private static DoubleQuaternion Combine(DoubleQuaternion a, DoubleQuaternion b, DoubleQuaternion c, DoubleQuaternion d) =>
        new(a.X+2*b.X+2*c.X+d.X, a.Y+2*b.Y+2*c.Y+d.Y, a.Z+2*b.Z+2*c.Z+d.Z, a.W+2*b.W+2*c.W+d.W);
    private static Double3 Scale(Double3 value, PoweredBinaryScale h, double divisor) =>
        new(h.MultiplyDivide(value.X, divisor), h.MultiplyDivide(value.Y, divisor), h.MultiplyDivide(value.Z, divisor));
    private static DoubleQuaternion Add(DoubleQuaternion q, DoubleQuaternion k, PoweredBinaryScale h, double divisor) =>
        new(q.X+h.MultiplyDivide(k.X, divisor), q.Y+h.MultiplyDivide(k.Y, divisor), q.Z+h.MultiplyDivide(k.Z, divisor), q.W+h.MultiplyDivide(k.W, divisor));
    private static PoweredKinematics Add(in PoweredKinematics state, in PoweredKinematics slope, PoweredBinaryScale h, double divisor) =>
        new(state.Position+Scale(slope.Position,h,divisor), state.Velocity+Scale(slope.Velocity,h,divisor),
            Add(state.Orientation,slope.Orientation,h,divisor), state.AngularVelocity+Scale(slope.AngularVelocity,h,divisor));
}
