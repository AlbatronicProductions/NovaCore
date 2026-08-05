using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft;

/// <summary>Pure exact-time constant body-angular-velocity attitude evaluation. It never mutates simulation state.</summary>
internal static class SpacecraftAttitudeEvaluator
{
    // Matches the existing bounded pure two-body evaluator policy: +/- 2^31 seconds in microticks.
    internal const long MaximumEvaluationTicks = UniversalVariableTwoBodyPropagator.MaximumEvaluationTicks;
    // Squared norm threshold: orientations with norm <= 1e-12 are rejected before normalization.
    internal const double MinimumOrientationLengthSquared = 1e-24d;
    private const double SmallAngleRadians = 1e-8d;

    internal static SpacecraftAttitudeEvaluationResult TryEvaluate(in SpacecraftAttitudeState state, SimulationInstant requestedTime)
    {
        if (!state.Spacecraft.IsValid) return Failure(SpacecraftAttitudeEvaluationStatus.InvalidSpacecraftId, requestedTime);
        if (state.Model != SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1) return Failure(SpacecraftAttitudeEvaluationStatus.UnsupportedModel, requestedTime);
        var canonicalStatus = TryCanonicalize(state.OrientationLocalToParent, out var initial);
        if (canonicalStatus != SpacecraftAttitudeEvaluationStatus.Success) return Failure(canonicalStatus, requestedTime);
        if (!state.AngularVelocityBody.IsFinite) return Failure(SpacecraftAttitudeEvaluationStatus.NonFiniteAngularVelocity, requestedTime);
        long ticks;
        try { ticks = (requestedTime - state.Epoch).Ticks; }
        catch (OverflowException) { return Failure(SpacecraftAttitudeEvaluationStatus.DurationOverflow, requestedTime); }
        if (ticks < -MaximumEvaluationTicks || ticks > MaximumEvaluationTicks) return Failure(SpacecraftAttitudeEvaluationStatus.EvaluationSpanExceeded, requestedTime);
        if (ticks == 0 || state.AngularVelocityBody.LengthSquared == 0d)
            return new(SpacecraftAttitudeEvaluationStatus.Success, requestedTime, initial, state.AngularVelocityBody);

        var seconds = ticks / (double)SimulationInstant.TicksPerSecond;
        var speed = Math.Sqrt(state.AngularVelocityBody.LengthSquared);
        if (!double.IsFinite(seconds) || !double.IsFinite(speed) || speed == 0d) return Failure(SpacecraftAttitudeEvaluationStatus.NonFiniteResult, requestedTime);
        var halfAngle = speed * seconds * .5d;
        if (!double.IsFinite(halfAngle)) return Failure(SpacecraftAttitudeEvaluationStatus.NonFiniteResult, requestedTime);
        var scale = Math.Abs(halfAngle) < SmallAngleRadians
            ? seconds * .5d * (1d - halfAngle * halfAngle / 6d)
            : Math.Sin(halfAngle) / speed;
        var delta = new DoubleQuaternion(state.AngularVelocityBody.X * scale, state.AngularVelocityBody.Y * scale, state.AngularVelocityBody.Z * scale, Math.Cos(halfAngle));
        var resultStatus = TryCanonicalize(initial * delta, out var orientation);
        return resultStatus == SpacecraftAttitudeEvaluationStatus.Success
            ? new(resultStatus, requestedTime, orientation, state.AngularVelocityBody)
            : Failure(resultStatus, requestedTime);
    }

    internal static SpacecraftAttitudeEvaluationStatus TryCanonicalize(DoubleQuaternion value, out DoubleQuaternion canonical)
    {
        canonical = default;
        if (!value.IsFinite) return SpacecraftAttitudeEvaluationStatus.NonFiniteOrientation;
        var lengthSquared = value.LengthSquared;
        if (!double.IsFinite(lengthSquared)) return SpacecraftAttitudeEvaluationStatus.NonFiniteOrientation;
        if (lengthSquared <= MinimumOrientationLengthSquared) return SpacecraftAttitudeEvaluationStatus.NearZeroOrientation;
        var normalized = value.Normalized();
        normalized = new DoubleQuaternion(Zero(normalized.X), Zero(normalized.Y), Zero(normalized.Z), Zero(normalized.W));
        if (ShouldNegate(normalized)) normalized = new DoubleQuaternion(-normalized.X, -normalized.Y, -normalized.Z, -normalized.W);
        canonical = normalized;
        return SpacecraftAttitudeEvaluationStatus.Success;
    }

    internal static Double3 Forward(in DoubleQuaternion orientation) => orientation.Rotate(Double3.UnitX);
    internal static Double3 Right(in DoubleQuaternion orientation) => orientation.Rotate(Double3.UnitY);
    internal static Double3 Down(in DoubleQuaternion orientation) => orientation.Rotate(Double3.UnitZ);
    internal static Double3 Up(in DoubleQuaternion orientation) => orientation.Rotate(-Double3.UnitZ);

    private static bool ShouldNegate(in DoubleQuaternion q) => q.W < 0d || (q.W == 0d && (q.X < 0d || (q.X == 0d && (q.Y < 0d || (q.Y == 0d && q.Z < 0d)))));
    private static double Zero(double value) => value == 0d ? 0d : value;
    private static SpacecraftAttitudeEvaluationResult Failure(SpacecraftAttitudeEvaluationStatus status, SimulationInstant requestedTime) => new(status, requestedTime, default, default);
}
