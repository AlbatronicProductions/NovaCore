namespace NovaCore.Simulation.Timeline;

public enum SimulationCancelStatus : byte
{
    Cancelled = 0,
    InvalidId,
    NotPending,
    RevisionOverflow,
}

public readonly record struct SimulationCancelResult(
    SimulationCancelStatus Status,
    ScheduledSimulationEvent CancelledEvent)
{
    public bool Succeeded => Status == SimulationCancelStatus.Cancelled;
    internal static SimulationCancelResult Failure(SimulationCancelStatus status) => new(status, default);
}
