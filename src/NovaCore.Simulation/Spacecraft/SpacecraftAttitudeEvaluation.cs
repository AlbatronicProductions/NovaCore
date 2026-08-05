using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft;

internal enum SpacecraftAttitudeEvaluationStatus : byte
{
    Success = 0,
    InvalidSpacecraftId,
    UnsupportedModel,
    NonFiniteOrientation,
    NearZeroOrientation,
    NonFiniteAngularVelocity,
    DurationOverflow,
    EvaluationSpanExceeded,
    NonFiniteResult,
}

internal readonly record struct SpacecraftAttitudeEvaluationResult(
    SpacecraftAttitudeEvaluationStatus Status,
    SimulationInstant RequestedTime,
    DoubleQuaternion OrientationLocalToParent,
    Double3 AngularVelocityBody)
{
    internal bool Succeeded => Status == SpacecraftAttitudeEvaluationStatus.Success;
}
