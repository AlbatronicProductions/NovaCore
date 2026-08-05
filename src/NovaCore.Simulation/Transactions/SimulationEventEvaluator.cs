using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial.Transactions;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;

namespace NovaCore.Simulation.Transactions;

/// <summary>Pure closed dispatch for marker and celestial impulse event intents.</summary>
internal static class SimulationEventEvaluator
{
    public static SimulationTransaction Evaluate(
        ScheduledSimulationEvent pending,
        SimulationStateView state,
        SimulationInstant evaluationTime,
        TimelineRevision timelineRevision)
    {
        if (pending.Header.Kind == SimulationEventKind.CelestialImpulse)
        {
            var impulse = CelestialImpulseEvaluator.TryEvaluate(pending, state, evaluationTime, timelineRevision);
            if (impulse.Succeeded)
            {
                var replacement = impulse.Transaction!.Value;
                return new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue, true, true, replacement, impulse.Status);
            }
            return new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue, false, false, null, impulse.Status);
        }
        if (pending.Header.Kind == SimulationEventKind.RigidBodyTorque)
        {
            if (pending.Payload.Kind != SimulationEventPayloadKind.RigidBodyTorque || pending.Header.Time != evaluationTime)
                return new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue, false, false);
            var result = RigidBodyTorqueTransactionEvaluator.TryCreateReplacement(state, evaluationTime, pending.Payload.SpacecraftSubject);
            return result.Succeeded
                ? new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue, true, true, null, null, result.Transaction)
                : new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue, false, false);
        }
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
