using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

/// <summary>Pure deterministic evaluation of the single 6B-3A marker event contract.</summary>
internal static class SimulationEventEvaluator
{
    public static SimulationTransaction Evaluate(
        ScheduledSimulationEvent pending,
        SimulationStateView state,
        SimulationInstant evaluationTime,
        TimelineRevision timelineRevision)
    {
        var changesState = pending.Header.Kind == SimulationEventKind.Marker && state.MarkerValue != long.MaxValue;
        var isNoOp = pending.Header.Kind == SimulationEventKind.NoOpMarker;
        var consistent = changesState || isNoOp;
        var markerValue = changesState ? state.MarkerValue + 1 : state.MarkerValue;
        return new SimulationTransaction(
            pending.Header,
            evaluationTime,
            timelineRevision,
            state.Revision,
            markerValue,
            ChangesAuthoritativeState: changesState,
            IsInternallyConsistent: consistent);
    }
}
