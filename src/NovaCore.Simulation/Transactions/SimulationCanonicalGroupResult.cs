using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal enum SimulationCanonicalGroupStopReason : byte
{
    Completed = 0,
    NoPendingEvent,
    NotAtBoundary,
    EventLimitReached,
    ValidationRejected,
    ReentrantExecution,
}

/// <summary>Outcome of orchestrating one canonical simulation-time group.</summary>
internal readonly record struct SimulationCanonicalGroupResult(
    SimulationCanonicalGroupStopReason Reason,
    SimulationInstant GroupTime,
    int ProcessedEventCount,
    bool IsComplete,
    SimulationEventHeader? PendingEvent,
    TimelineRevision TimelineRevision,
    StateRevision StateRevision);
