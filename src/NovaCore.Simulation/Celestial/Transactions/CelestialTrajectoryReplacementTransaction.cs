using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Immutable candidate to replace one existing child trajectory at one canonical event boundary.</summary>
internal readonly record struct CelestialTrajectoryReplacementTransaction(
    ScheduledSimulationEvent Event,
    SimulationInstant EvaluationTime,
    TimelineRevision ExpectedTimelineRevision,
    StateRevision ExpectedStateRevision,
    CelestialBodyId Subject,
    TwoBodyTrajectory ExpectedTrajectory,
    TwoBodyTrajectory ReplacementTrajectory);

internal readonly record struct CelestialTrajectoryTransactionCreationResult(
    CelestialTrajectoryTransactionStatus Status,
    CelestialTrajectoryReplacementTransaction? Transaction)
{
    public bool Succeeded => Status == CelestialTrajectoryTransactionStatus.Success;
}

internal readonly record struct CelestialTrajectoryTransactionResult(
    CelestialTrajectoryTransactionStatus Status,
    NovaCore.Simulation.Transactions.ProcessedSimulationEvent? ProcessedEvent)
{
    public bool Committed => Status == CelestialTrajectoryTransactionStatus.Success;
}
