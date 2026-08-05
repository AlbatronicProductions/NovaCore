using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Spacecraft.Transactions;

/// <summary>Immutable direct candidate. It is evaluated outside and committed only by the transaction engine.</summary>
internal readonly record struct SpacecraftAttitudeReplacementTransaction(
    SimulationInstant EvaluationTime,
    StateRevision ExpectedStateRevision,
    SpacecraftId Subject,
    SpacecraftAttitudeState ExpectedAttitude,
    SpacecraftAttitudeState ReplacementAttitude);

internal enum SpacecraftAttitudeTransactionStatus : byte
{
    Success = 0,
    SubjectNotFound,
    TimeMismatch,
    StateRevisionMismatch,
    AttitudeBasisMismatch,
    ReplacementInvalid,
    ReplacementNoOp,
    StateRevisionOverflow,
    HistoryCapacityFailure,
}

internal readonly record struct SpacecraftAttitudeTransactionCreationResult(SpacecraftAttitudeTransactionStatus Status, SpacecraftAttitudeReplacementTransaction? Transaction)
{ internal bool Succeeded => Status == SpacecraftAttitudeTransactionStatus.Success; }

internal readonly record struct ProcessedSpacecraftAttitudeTransition(SpacecraftId Subject, SimulationInstant ExecutionTime, StateRevision StateRevisionBefore, StateRevision StateRevisionAfter, ulong ExpectedHash, ulong ReplacementHash);
internal readonly record struct SpacecraftAttitudeTransactionResult(SpacecraftAttitudeTransactionStatus Status, ProcessedSpacecraftAttitudeTransition? ProcessedTransition)
{ internal bool Committed => Status == SpacecraftAttitudeTransactionStatus.Success; }
