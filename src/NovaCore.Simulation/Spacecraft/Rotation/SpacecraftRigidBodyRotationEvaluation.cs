using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Rotation;

internal enum SpacecraftRigidBodyRotationEvaluationStatus : byte
{
    Success = 0,
    InvalidSpacecraftId,
    UnsupportedModel,
    NonFiniteOrientation,
    InvalidOrientation,
    NonFiniteAngularVelocity,
    NonFiniteTorque,
    NonFiniteInertia,
    NonPositiveInertia,
    DurationOverflow,
    DurationBoundExceeded,
    ExcessiveStepCount,
    QuaternionNormalizationFailure,
    NonFiniteIntermediate,
    NonFiniteResult,
}

internal readonly record struct SpacecraftRigidBodyRotationEvaluationResult(
    SpacecraftRigidBodyRotationEvaluationStatus Status,
    SimulationInstant RequestedTime,
    DoubleQuaternion OrientationLocalToParent,
    Double3 AngularVelocityBody,
    int SubstepCount)
{
    internal bool Succeeded => Status == SpacecraftRigidBodyRotationEvaluationStatus.Success;
}
