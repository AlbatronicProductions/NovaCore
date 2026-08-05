namespace NovaCore.Simulation.Timeline;

public enum SimulationScheduleStatus : byte
{
    Scheduled = 0,
    InvalidId,
    InvalidKind,
    InvalidPayload,
    DuplicateId,
    PastTime,
    ReplacementTargetNotPending,
    SequenceOverflow,
    RevisionOverflow,
}

public readonly record struct SimulationScheduleResult(
    SimulationScheduleStatus Status,
    ScheduledSimulationEvent ScheduledEvent)
{
    public bool Succeeded => Status == SimulationScheduleStatus.Scheduled;
    internal static SimulationScheduleResult Failure(SimulationScheduleStatus status) => new(status, default);
}
