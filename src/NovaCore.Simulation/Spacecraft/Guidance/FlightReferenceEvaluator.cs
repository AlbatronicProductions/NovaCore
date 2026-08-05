using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Guidance;

internal enum FlightReferenceMode : byte { Prograde, Retrograde, Normal, AntiNormal, RadialOut, RadialIn }
internal enum FlightReferenceEvaluationStatus : byte { Success = 0, UnsupportedMode, NonFiniteState, NearZeroRadius, NearZeroVelocity, UndefinedNormal, InvalidCarrierOrientation, NonFiniteResult }
internal readonly record struct FlightReferenceEvaluationResult(FlightReferenceEvaluationStatus Status, Double3 DirectionCarrierParent)
{ internal bool Succeeded => Status == FlightReferenceEvaluationStatus.Success; }

/// <summary>Pure orbital-direction evaluation. Cartesian inputs are in trajectory frame; carrier orientation maps that frame into carrier parent.</summary>
internal static class FlightReferenceEvaluator
{
    internal const double MinimumMagnitudeSquared = 1e-24d;
    internal static FlightReferenceEvaluationResult TryEvaluate(in Double3 position, in Double3 velocity, in DoubleQuaternion trajectoryToCarrierParent, FlightReferenceMode mode)
    {
        if (!position.IsFinite || !velocity.IsFinite) return Failure(FlightReferenceEvaluationStatus.NonFiniteState);
        if (SpacecraftAttitudeEvaluator.TryCanonicalize(trajectoryToCarrierParent, out var carrier) != SpacecraftAttitudeEvaluationStatus.Success) return Failure(FlightReferenceEvaluationStatus.InvalidCarrierOrientation);
        var radius2 = position.LengthSquared; var velocity2 = velocity.LengthSquared;
        if (!double.IsFinite(radius2) || !double.IsFinite(velocity2)) return Failure(FlightReferenceEvaluationStatus.NonFiniteState);
        Double3 trajectoryDirection;
        switch (mode)
        {
            case FlightReferenceMode.Prograde or FlightReferenceMode.Retrograde:
                if (velocity2 <= MinimumMagnitudeSquared) return Failure(FlightReferenceEvaluationStatus.NearZeroVelocity);
                trajectoryDirection = velocity / Math.Sqrt(velocity2); if (mode == FlightReferenceMode.Retrograde) trajectoryDirection = -trajectoryDirection; break;
            case FlightReferenceMode.RadialOut or FlightReferenceMode.RadialIn:
                if (radius2 <= MinimumMagnitudeSquared) return Failure(FlightReferenceEvaluationStatus.NearZeroRadius);
                trajectoryDirection = position / Math.Sqrt(radius2); if (mode == FlightReferenceMode.RadialIn) trajectoryDirection = -trajectoryDirection; break;
            case FlightReferenceMode.Normal or FlightReferenceMode.AntiNormal:
                if (radius2 <= MinimumMagnitudeSquared) return Failure(FlightReferenceEvaluationStatus.NearZeroRadius);
                if (velocity2 <= MinimumMagnitudeSquared) return Failure(FlightReferenceEvaluationStatus.NearZeroVelocity);
                var normal = Double3.Cross(position, velocity); var normal2 = normal.LengthSquared;
                if (!double.IsFinite(normal2) || normal2 <= MinimumMagnitudeSquared) return Failure(FlightReferenceEvaluationStatus.UndefinedNormal);
                trajectoryDirection = normal / Math.Sqrt(normal2); if (mode == FlightReferenceMode.AntiNormal) trajectoryDirection = -trajectoryDirection; break;
            default: return Failure(FlightReferenceEvaluationStatus.UnsupportedMode);
        }
        var result = carrier.Rotate(trajectoryDirection);
        return result.IsFinite && result.LengthSquared > MinimumMagnitudeSquared ? new(FlightReferenceEvaluationStatus.Success, result) : Failure(FlightReferenceEvaluationStatus.NonFiniteResult);
    }
    private static FlightReferenceEvaluationResult Failure(FlightReferenceEvaluationStatus status) => new(status, default);
}
