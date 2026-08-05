using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Spacecraft.Rotation.Transactions;

/// <summary>Immutable direct proposal replacing one rigid-body state at an exact end instant.</summary>
internal readonly record struct RigidBodyTorqueReplacementTransaction(
    SimulationInstant EvaluationTime,
    StateRevision ExpectedStateRevision,
    SpacecraftId Subject,
    SpacecraftRigidBodyRotationState ExpectedRotation,
    SpacecraftRigidBodyRotationState ReplacementRotation);

internal enum RigidBodyTorqueTransactionStatus : byte
{
    Success = 0, SubjectNotFound, TimeMismatch, StateRevisionMismatch, RotationBasisMismatch,
    ReplacementInvalid, ReplacementNoOp, EvaluationFailed, StateRevisionOverflow, HistoryCapacityFailure,
}

internal readonly record struct RigidBodyTorqueTransactionCreationResult(RigidBodyTorqueTransactionStatus Status, RigidBodyTorqueReplacementTransaction? Transaction)
{ internal bool Succeeded => Status == RigidBodyTorqueTransactionStatus.Success; }

internal readonly record struct ProcessedRigidBodyTorqueTransition(SpacecraftId Subject, SimulationInstant ExecutionTime, StateRevision StateRevisionBefore, StateRevision StateRevisionAfter, ulong ExpectedHash, ulong ReplacementHash);
internal readonly record struct RigidBodyTorqueTransactionResult(RigidBodyTorqueTransactionStatus Status, ProcessedRigidBodyTorqueTransition? ProcessedTransition)
{ internal bool Committed => Status == RigidBodyTorqueTransactionStatus.Success; }
