using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Rotation;

/// <summary>Pure bounded RK4 evaluation of diagonal-inertia body rotation under constant body-space torque.</summary>
internal static class SpacecraftRigidBodyRotationEvaluator
{
    internal const long FullSubstepTicks = 10_000;
    internal const int MaximumSubstepCount = 1_000_000;
    internal const long MaximumEvaluationTicks = UniversalVariableTwoBodyPropagator.MaximumEvaluationTicks;

    internal static SpacecraftRigidBodyRotationEvaluationResult TryEvaluate(in SpacecraftRigidBodyRotationState state, SimulationInstant requestedTime)
    {
        var validation = Validate(state, out var orientation);
        if (validation != SpacecraftRigidBodyRotationEvaluationStatus.Success) return Failure(validation, requestedTime);
        long ticks;
        try { ticks = (requestedTime - state.Epoch).Ticks; }
        catch (OverflowException) { return Failure(SpacecraftRigidBodyRotationEvaluationStatus.DurationOverflow, requestedTime); }
        if (ticks < -MaximumEvaluationTicks || ticks > MaximumEvaluationTicks) return Failure(SpacecraftRigidBodyRotationEvaluationStatus.DurationBoundExceeded, requestedTime);
        var absoluteTicks = ticks < 0 ? -ticks : ticks;
        var fullSteps = absoluteTicks / FullSubstepTicks;
        var remainder = absoluteTicks % FullSubstepTicks;
        var count = fullSteps + (remainder == 0 ? 0 : 1);
        if (count > MaximumSubstepCount) return Failure(SpacecraftRigidBodyRotationEvaluationStatus.ExcessiveStepCount, requestedTime);
        if (ticks == 0) return new(SpacecraftRigidBodyRotationEvaluationStatus.Success, requestedTime, orientation, state.AngularVelocityBody, 0);

        var signedFullSeconds = (ticks < 0 ? -1d : 1d) * FullSubstepTicks / (double)SimulationInstant.TicksPerSecond;
        var currentOrientation = orientation; var currentAngularVelocity = state.AngularVelocityBody;
        for (var step = 0L; step < fullSteps; step++)
            if (!TryRk4Step(ref currentOrientation, ref currentAngularVelocity, state.PrincipalInertia, state.ConstantBodyTorque, signedFullSeconds, out var status)) return Failure(status, requestedTime);
        if (remainder != 0)
        {
            var remainderSeconds = (ticks < 0 ? -1d : 1d) * remainder / (double)SimulationInstant.TicksPerSecond;
            if (!TryRk4Step(ref currentOrientation, ref currentAngularVelocity, state.PrincipalInertia, state.ConstantBodyTorque, remainderSeconds, out var status)) return Failure(status, requestedTime);
        }
        return new(SpacecraftRigidBodyRotationEvaluationStatus.Success, requestedTime, currentOrientation, currentAngularVelocity, checked((int)count));
    }

    internal static Double3 AngularMomentum(in PrincipalMomentsOfInertia inertia, in Double3 angularVelocity) => inertia.Multiply(angularVelocity);
    internal static double RotationalEnergy(in PrincipalMomentsOfInertia inertia, in Double3 angularVelocity) => .5d * Double3.Dot(angularVelocity, inertia.Multiply(angularVelocity));

    internal static SpacecraftRigidBodyRotationEvaluationStatus TryCanonicalize(in DoubleQuaternion value, out DoubleQuaternion canonical)
    {
        canonical = default;
        if (!value.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteOrientation;
        var lengthSquared = value.LengthSquared;
        if (!double.IsFinite(lengthSquared)) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteOrientation;
        if (lengthSquared <= SpacecraftAttitudeEvaluator.MinimumOrientationLengthSquared) return SpacecraftRigidBodyRotationEvaluationStatus.InvalidOrientation;
        try { canonical = Canonicalize(value.Normalized()); return SpacecraftRigidBodyRotationEvaluationStatus.Success; }
        catch (ArgumentOutOfRangeException) { return SpacecraftRigidBodyRotationEvaluationStatus.InvalidOrientation; }
    }

    private static SpacecraftRigidBodyRotationEvaluationStatus Validate(in SpacecraftRigidBodyRotationState state, out DoubleQuaternion orientation)
    {
        orientation = default;
        if (!state.Spacecraft.IsValid) return SpacecraftRigidBodyRotationEvaluationStatus.InvalidSpacecraftId;
        if (state.Model != RigidBodyRotationModel.ConstantBodyTorqueV1) return SpacecraftRigidBodyRotationEvaluationStatus.UnsupportedModel;
        var status = TryCanonicalize(state.OrientationLocalToParent, out orientation); if (status != SpacecraftRigidBodyRotationEvaluationStatus.Success) return status;
        if (!state.AngularVelocityBody.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteAngularVelocity;
        if (!state.ConstantBodyTorque.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteTorque;
        if (!state.PrincipalInertia.IsFinite) return SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteInertia;
        return state.PrincipalInertia.IsStrictlyPositive ? SpacecraftRigidBodyRotationEvaluationStatus.Success : SpacecraftRigidBodyRotationEvaluationStatus.NonPositiveInertia;
    }

    private static bool TryRk4Step(ref DoubleQuaternion orientation, ref Double3 angularVelocity, in PrincipalMomentsOfInertia inertia, in Double3 torque, double seconds, out SpacecraftRigidBodyRotationEvaluationStatus status)
    {
        if (!double.IsFinite(seconds)) { status = SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteIntermediate; return false; }
        Derivative(orientation, angularVelocity, inertia, torque, out var k1q, out var k1w);
        Derivative(AddScaled(orientation, k1q, seconds * .5d), angularVelocity + k1w * (seconds * .5d), inertia, torque, out var k2q, out var k2w);
        Derivative(AddScaled(orientation, k2q, seconds * .5d), angularVelocity + k2w * (seconds * .5d), inertia, torque, out var k3q, out var k3w);
        Derivative(AddScaled(orientation, k3q, seconds), angularVelocity + k3w * seconds, inertia, torque, out var k4q, out var k4w);
        var nextOrientation = AddScaled(orientation, Combine(k1q, k2q, k3q, k4q), seconds / 6d);
        var nextAngularVelocity = angularVelocity + Combine(k1w, k2w, k3w, k4w) * (seconds / 6d);
        if (!nextOrientation.IsFinite || !nextAngularVelocity.IsFinite) { status = SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteIntermediate; return false; }
        var canonical = TryCanonicalize(nextOrientation, out orientation);
        if (canonical != SpacecraftRigidBodyRotationEvaluationStatus.Success) { status = SpacecraftRigidBodyRotationEvaluationStatus.QuaternionNormalizationFailure; return false; }
        angularVelocity = nextAngularVelocity; status = SpacecraftRigidBodyRotationEvaluationStatus.Success; return true;
    }

    private static void Derivative(in DoubleQuaternion orientation, in Double3 angularVelocity, in PrincipalMomentsOfInertia inertia, in Double3 torque, out DoubleQuaternion orientationDerivative, out Double3 angularVelocityDerivative)
    {
        angularVelocityDerivative = new(
            (torque.X + (inertia.Y - inertia.Z) * angularVelocity.Y * angularVelocity.Z) / inertia.X,
            (torque.Y + (inertia.Z - inertia.X) * angularVelocity.Z * angularVelocity.X) / inertia.Y,
            (torque.Z + (inertia.X - inertia.Y) * angularVelocity.X * angularVelocity.Y) / inertia.Z);
        orientationDerivative = orientation * new DoubleQuaternion(angularVelocity.X, angularVelocity.Y, angularVelocity.Z, 0d);
        orientationDerivative = new(orientationDerivative.X * .5d, orientationDerivative.Y * .5d, orientationDerivative.Z * .5d, orientationDerivative.W * .5d);
    }

    private static DoubleQuaternion AddScaled(in DoubleQuaternion left, in DoubleQuaternion right, double scale) => new(left.X + right.X * scale, left.Y + right.Y * scale, left.Z + right.Z * scale, left.W + right.W * scale);
    private static DoubleQuaternion Combine(in DoubleQuaternion one, in DoubleQuaternion two, in DoubleQuaternion three, in DoubleQuaternion four) => new(one.X + 2d * (two.X + three.X) + four.X, one.Y + 2d * (two.Y + three.Y) + four.Y, one.Z + 2d * (two.Z + three.Z) + four.Z, one.W + 2d * (two.W + three.W) + four.W);
    private static Double3 Combine(in Double3 one, in Double3 two, in Double3 three, in Double3 four) => one + (two + three) * 2d + four;
    private static DoubleQuaternion Canonicalize(in DoubleQuaternion value)
    {
        var q = new DoubleQuaternion(Zero(value.X), Zero(value.Y), Zero(value.Z), Zero(value.W));
        return q.W < 0d || (q.W == 0d && (q.X < 0d || (q.X == 0d && (q.Y < 0d || (q.Y == 0d && q.Z < 0d))))) ? new(-q.X, -q.Y, -q.Z, -q.W) : q;
    }
    private static double Zero(double value) => value == 0d ? 0d : value;
    private static SpacecraftRigidBodyRotationEvaluationResult Failure(SpacecraftRigidBodyRotationEvaluationStatus status, SimulationInstant requestedTime) => new(status, requestedTime, default, default, 0);
}
