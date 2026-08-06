using NovaCore.Core.ReferenceFrames;

namespace NovaCore.Simulation.Celestial;

internal enum CelestialSystemEvaluationStatus : byte
{
    Success = 0,
    DestinationTooSmall,
    InvalidHierarchy,
    InvalidConstants,
    UnsupportedTrajectoryModel,
    ParentEvaluationFailed,
    NumericalFailure,
    NonFiniteResult,
}

internal readonly record struct CelestialSystemEvaluationResult(CelestialSystemEvaluationStatus Status)
{
    public bool Succeeded => Status == CelestialSystemEvaluationStatus.Success;
}
