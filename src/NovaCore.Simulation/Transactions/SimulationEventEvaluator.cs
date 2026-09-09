using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial.Transactions;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Spacecraft.Contact;

namespace NovaCore.Simulation.Transactions;

/// <summary>Pure closed dispatch for marker and celestial impulse event intents.</summary>
internal static class SimulationEventEvaluator
{
    public static SimulationTransaction Evaluate(
        ScheduledSimulationEvent pending,
        SimulationStateView state,
        SimulationInstant evaluationTime,
        TimelineRevision timelineRevision,
        SimulationTimeline timeline)
    {
        if (pending.Header.Kind == SimulationEventKind.SpacecraftContactImpulse)
        {
            var status = SpacecraftContactImpulseEvaluator.TryCreate(state, timeline, pending, evaluationTime, timelineRevision, out var response);
            return new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue,
                status == ContactImpulseStatus.Success, status == ContactImpulseStatus.Success,
                ContactImpulseReplacement: status == ContactImpulseStatus.Success ? response : null);
        }
        if (pending.Header.Kind == SimulationEventKind.SpacecraftForce)
        {
            var status = SpacecraftForceTransactionEvaluator.TryCreate(state, pending, evaluationTime, timelineRevision, out var force);
            return new(pending.Header, evaluationTime, timelineRevision, state.Revision, state.MarkerValue,
                status == SpacecraftTranslationStatus.Success && force.Expected != force.Replacement,
                status == SpacecraftTranslationStatus.Success, SpacecraftForceReplacement: status == SpacecraftTranslationStatus.Success ? force : null);
        }
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
