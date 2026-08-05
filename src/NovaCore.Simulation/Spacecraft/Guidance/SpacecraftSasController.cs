using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Guidance;

internal enum SpacecraftSasMode : byte { Off, HoldAttitude, Prograde, Retrograde, Normal, AntiNormal, RadialOut, RadialIn }
internal enum SpacecraftSasControlStatus : byte { Success = 0, Settled, InvalidConfiguration, InvalidOrientation, InvalidAngularVelocity, InvalidInertia, InvalidTargetBasis, NonFiniteResult }
internal readonly record struct SpacecraftSasControllerConfiguration(Double3 ProportionalGain, Double3 DerivativeGain, Double3 MaximumTorque, double AttitudeDeadbandRadians, double AngularRateDeadband, double SettledAttitudeRadians, double SettledAngularRate)
{
    internal bool IsValid => ProportionalGain.IsFinite && DerivativeGain.IsFinite && MaximumTorque.IsFinite && ProportionalGain.X >= 0d && ProportionalGain.Y >= 0d && ProportionalGain.Z >= 0d && DerivativeGain.X >= 0d && DerivativeGain.Y >= 0d && DerivativeGain.Z >= 0d && MaximumTorque.X > 0d && MaximumTorque.Y > 0d && MaximumTorque.Z > 0d && double.IsFinite(AttitudeDeadbandRadians) && double.IsFinite(AngularRateDeadband) && double.IsFinite(SettledAttitudeRadians) && double.IsFinite(SettledAngularRate) && AttitudeDeadbandRadians >= 0d && AngularRateDeadband >= 0d && SettledAttitudeRadians >= AttitudeDeadbandRadians && SettledAngularRate >= AngularRateDeadband;
}
internal readonly record struct SpacecraftSasControlResult(SpacecraftSasControlStatus Status, Double3 RequestedBodyTorque, Double3 AttitudeErrorBody)
{ internal bool Succeeded => Status is SpacecraftSasControlStatus.Success or SpacecraftSasControlStatus.Settled; }

internal static class SpacecraftSasTargetOrientation
{
    internal const double BasisMinimumSquared = 1e-24d;
    internal static SpacecraftSasControlStatus TryCreate(in Double3 desiredForward, in Double3 preferredUp, out DoubleQuaternion target)
    {
        target = default;
        if (!desiredForward.IsFinite || !preferredUp.IsFinite) return SpacecraftSasControlStatus.InvalidTargetBasis;
        var f2 = desiredForward.LengthSquared; if (f2 <= BasisMinimumSquared || !double.IsFinite(f2)) return SpacecraftSasControlStatus.InvalidTargetBasis;
        var forward = desiredForward / Math.Sqrt(f2); var projectedUp = preferredUp - forward * Double3.Dot(preferredUp, forward);
        if (!projectedUp.IsFinite || projectedUp.LengthSquared <= BasisMinimumSquared)
        {
            var candidate = Double3.UnitZ; var best = Math.Abs(Double3.Dot(candidate, forward));
            var yAlignment = Math.Abs(Double3.Dot(Double3.UnitY, forward)); if (yAlignment < best) { candidate = Double3.UnitY; best = yAlignment; }
            var xAlignment = Math.Abs(Double3.Dot(Double3.UnitX, forward)); if (xAlignment < best) candidate = Double3.UnitX;
            projectedUp = candidate - forward * Double3.Dot(candidate, forward);
        }
        var up = projectedUp.Normalized(); var right = Double3.Cross(forward, up).Normalized(); var down = Double3.Cross(forward, right).Normalized();
        target = FromColumns(forward, right, down);
        return SpacecraftAttitudeEvaluator.TryCanonicalize(target, out target) == SpacecraftAttitudeEvaluationStatus.Success && Double3.Dot(target.Rotate(Double3.UnitX), forward) > 1d - 1e-12d ? SpacecraftSasControlStatus.Success : SpacecraftSasControlStatus.InvalidTargetBasis;
    }
    internal static DoubleQuaternion CaptureHold(in DoubleQuaternion current) => SpacecraftAttitudeEvaluator.TryCanonicalize(current, out var canonical) == SpacecraftAttitudeEvaluationStatus.Success ? canonical : default;
    private static DoubleQuaternion FromColumns(in Double3 x, in Double3 y, in Double3 z)
    {
        var trace = x.X + y.Y + z.Z;
        if (trace > 0d) { var s = Math.Sqrt(trace + 1d) * 2d; return new((y.Z - z.Y) / s, (z.X - x.Z) / s, (x.Y - y.X) / s, .25d * s); }
        if (x.X > y.Y && x.X > z.Z) { var s = Math.Sqrt(1d + x.X - y.Y - z.Z) * 2d; return new(.25d * s, (x.Y + y.X) / s, (z.X + x.Z) / s, (y.Z - z.Y) / s); }
        if (y.Y > z.Z) { var s = Math.Sqrt(1d + y.Y - x.X - z.Z) * 2d; return new((x.Y + y.X) / s, .25d * s, (y.Z + z.Y) / s, (z.X - x.Z) / s); }
        { var s = Math.Sqrt(1d + z.Z - x.X - y.Y) * 2d; return new((z.X + x.Z) / s, (y.Z + z.Y) / s, .25d * s, (x.Y - y.X) / s); }
    }
}

internal static class SpacecraftSasController
{
    internal static SpacecraftSasControlResult TryEvaluate(in DoubleQuaternion current, in Double3 angularVelocityBody, in DoubleQuaternion target, in PrincipalMomentsOfInertia inertia, in SpacecraftSasControllerConfiguration config)
    {
        if (!config.IsValid) return Failure(SpacecraftSasControlStatus.InvalidConfiguration);
        if (!angularVelocityBody.IsFinite) return Failure(SpacecraftSasControlStatus.InvalidAngularVelocity);
        if (!inertia.IsFinite || !inertia.IsStrictlyPositive) return Failure(SpacecraftSasControlStatus.InvalidInertia);
        if (SpacecraftAttitudeEvaluator.TryCanonicalize(current, out var q) != SpacecraftAttitudeEvaluationStatus.Success || SpacecraftAttitudeEvaluator.TryCanonicalize(target, out var t) != SpacecraftAttitudeEvaluationStatus.Success) return Failure(SpacecraftSasControlStatus.InvalidOrientation);
        var e = q.Conjugate() * t; if (e.W < 0d) e = new(-e.X, -e.Y, -e.Z, -e.W);
        var v = new Double3(e.X, e.Y, e.Z); var magnitude = Math.Sqrt(v.LengthSquared); var angle = 2d * Math.Atan2(magnitude, e.W);
        var error = magnitude < 1e-12d ? v * 2d : v * (angle / magnitude);
        if (error.LengthSquared <= config.AttitudeDeadbandRadians * config.AttitudeDeadbandRadians) error = Double3.Zero;
        var rate = angularVelocityBody.LengthSquared <= config.AngularRateDeadband * config.AngularRateDeadband ? Double3.Zero : angularVelocityBody;
        var torque = new Double3(config.ProportionalGain.X * error.X - config.DerivativeGain.X * rate.X, config.ProportionalGain.Y * error.Y - config.DerivativeGain.Y * rate.Y, config.ProportionalGain.Z * error.Z - config.DerivativeGain.Z * rate.Z);
        torque = new Double3(Math.Clamp(torque.X, -config.MaximumTorque.X, config.MaximumTorque.X), Math.Clamp(torque.Y, -config.MaximumTorque.Y, config.MaximumTorque.Y), Math.Clamp(torque.Z, -config.MaximumTorque.Z, config.MaximumTorque.Z));
        if (!torque.IsFinite) return Failure(SpacecraftSasControlStatus.NonFiniteResult);
        var settled = error.LengthSquared <= config.SettledAttitudeRadians * config.SettledAttitudeRadians && angularVelocityBody.LengthSquared <= config.SettledAngularRate * config.SettledAngularRate;
        return new(settled ? SpacecraftSasControlStatus.Settled : SpacecraftSasControlStatus.Success, torque, error);
    }
    private static SpacecraftSasControlResult Failure(SpacecraftSasControlStatus status) => new(status, default, default);
}
