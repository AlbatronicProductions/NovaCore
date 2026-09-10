using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Rotation;

/// <summary>
/// Banked canonical rotation to the requested floor, then dynamics over only the positive fractional tick.
/// No canonical integrator rerouting, interpolation, persistent rebasing or continuation authority.
/// </summary>
internal static class SpacecraftPhysicalEventRotationEvaluator
{
    internal static SpacecraftRigidBodyRotationEvaluationStatus TryEvaluate(in SpacecraftRigidBodyRotationState state,
        PhysicalEventEpoch time, out DoubleQuaternion orientation, out Double3 angularVelocity, out int substepCount)
    {
        orientation = default; angularVelocity = default; substepCount = 0;
        if (time.TryGetCanonicalInstant(out var canonical))
        {
            var value = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(state, canonical);
            if (value.Succeeded)
            { orientation = value.OrientationLocalToParent; angularVelocity = value.AngularVelocityBody; substepCount = value.SubstepCount; }
            return value.Status;
        }
        if (!PhysicalEventDuration.TryDifference(time, state.Epoch, out var duration))
            return SpacecraftRigidBodyRotationEvaluationStatus.DurationOverflow;
        if (duration.ExceedsMagnitude(SpacecraftRigidBodyRotationEvaluator.MaximumEvaluationTicks))
            return SpacecraftRigidBodyRotationEvaluationStatus.DurationBoundExceeded;

        var spherical = state.ConstantBodyTorque == Double3.Zero &&
            state.PrincipalInertia.X == state.PrincipalInertia.Y && state.PrincipalInertia.Y == state.PrincipalInertia.Z;
        var floorTicks = (Int128)time.FloorTicks - state.Epoch.Ticks;
        var absoluteFloor = floorTicks < 0 ? -floorTicks : floorTicks;
        var floorSteps = (absoluteFloor + SpacecraftRigidBodyRotationEvaluator.FullSubstepTicks - 1) /
            SpacecraftRigidBodyRotationEvaluator.FullSubstepTicks;
        if (!spherical && floorSteps >= SpacecraftRigidBodyRotationEvaluator.MaximumSubstepCount)
            return SpacecraftRigidBodyRotationEvaluationStatus.ExcessiveStepCount;
        var floor = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(state, new SimulationInstant(time.FloorTicks));
        if (!floor.Succeeded) return floor.Status;

        // Exact identity/range checks precede this final bounded FP64 conversion; 0 < interval <= one microtick.
        var seconds = ((double)time.Numerator / time.Denominator) / SimulationInstant.TicksPerSecond;
        var q = floor.OrientationLocalToParent;
        var w = floor.AngularVelocityBody;
        if (spherical)
        {
            // Preserve the banked model's constant-rate specialization; only this terminal interval is new.
            var speedSquared = w.LengthSquared;
            if (speedSquared != 0d)
            {
                var speed = Math.Sqrt(speedSquared);
                var halfAngle = speed * seconds * .5d;
                if (!double.IsFinite(speed) || !double.IsFinite(halfAngle)) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteIntermediate;
                var scale = Math.Abs(halfAngle) < 1e-8d ? seconds * .5d * (1d - halfAngle * halfAngle / 6d) : Math.Sin(halfAngle) / speed;
                var delta = new DoubleQuaternion(w.X * scale, w.Y * scale, w.Z * scale, Math.Cos(halfAngle));
                if (SpacecraftAttitudeEvaluator.TryCanonicalize(q * delta, out q) != SpacecraftAttitudeEvaluationStatus.Success)
                    return SpacecraftRigidBodyRotationEvaluationStatus.QuaternionNormalizationFailure;
            }
            orientation = q; angularVelocity = w;
            return SpacecraftRigidBodyRotationEvaluationStatus.Success;
        }

        var status = TryFractionalStep(q, w, state.PrincipalInertia, state.ConstantBodyTorque, seconds, out var nextQ, out var nextW);
        if (status != SpacecraftRigidBodyRotationEvaluationStatus.Success) return status;
        orientation = nextQ; angularVelocity = nextW; substepCount = floor.SubstepCount + 1;
        return SpacecraftRigidBodyRotationEvaluationStatus.Success;
    }

    // Deliberate event-only specialization of the banked RK4 equations. Canonical code never calls this helper.
    private static SpacecraftRigidBodyRotationEvaluationStatus TryFractionalStep(in DoubleQuaternion q, in Double3 w, in PrincipalMomentsOfInertia inertia,
        in Double3 torque, double h, out DoubleQuaternion orientation, out Double3 angularVelocity)
    {
        orientation = default; angularVelocity = default;
        Derivative(q, w, inertia, torque, out var k1q, out var k1w);
        Derivative(Add(q, k1q, h * .5d), w + k1w * (h * .5d), inertia, torque, out var k2q, out var k2w);
        Derivative(Add(q, k2q, h * .5d), w + k2w * (h * .5d), inertia, torque, out var k3q, out var k3w);
        Derivative(Add(q, k3q, h), w + k3w * h, inertia, torque, out var k4q, out var k4w);
        var nextQ = Add(q, Combine(k1q, k2q, k3q, k4q), h / 6d);
        var nextW = w + (k1w + (k2w + k3w) * 2d + k4w) * (h / 6d);
        if (!nextQ.IsFinite || !nextW.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteIntermediate;
        var status = SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(nextQ, out orientation);
        if (status != SpacecraftRigidBodyRotationEvaluationStatus.Success) return SpacecraftRigidBodyRotationEvaluationStatus.QuaternionNormalizationFailure;
        angularVelocity = nextW;
        return SpacecraftRigidBodyRotationEvaluationStatus.Success;
    }

    private static void Derivative(in DoubleQuaternion q, in Double3 w, in PrincipalMomentsOfInertia inertia,
        in Double3 torque, out DoubleQuaternion dq, out Double3 dw)
    {
        dw = new((torque.X + (inertia.Y - inertia.Z) * w.Y * w.Z) / inertia.X,
            (torque.Y + (inertia.Z - inertia.X) * w.Z * w.X) / inertia.Y,
            (torque.Z + (inertia.X - inertia.Y) * w.X * w.Y) / inertia.Z);
        var product = q * new DoubleQuaternion(w.X, w.Y, w.Z, 0d);
        dq = new(product.X * .5d, product.Y * .5d, product.Z * .5d, product.W * .5d);
    }

    private static DoubleQuaternion Add(in DoubleQuaternion a, in DoubleQuaternion b, double h) =>
        new(a.X + b.X * h, a.Y + b.Y * h, a.Z + b.Z * h, a.W + b.W * h);
    private static DoubleQuaternion Combine(in DoubleQuaternion a, in DoubleQuaternion b, in DoubleQuaternion c, in DoubleQuaternion d) =>
        new(a.X + 2d * (b.X + c.X) + d.X, a.Y + 2d * (b.Y + c.Y) + d.Y,
            a.Z + 2d * (b.Z + c.Z) + d.Z, a.W + 2d * (b.W + c.W) + d.W);
}
