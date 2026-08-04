using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Clock;

/// <summary>Allocation-free outcome of one exact-time clock command.</summary>
public readonly record struct SimulationAdvanceResult(
    SimulationAdvanceStopReason Reason,
    SimulationInstant RequestedTime,
    SimulationInstant ReachedTime,
    SimulationEventHeader? BoundaryEvent,
    TimelineRevision TimelineRevision,
    int ExaminedEventCount)
{
    public bool ReachedBoundary => Reason == SimulationAdvanceStopReason.ReachedEventBoundary;
}
