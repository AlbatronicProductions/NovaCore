using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Pure exact-time propagation and inertial delta-v evaluation. It never mutates authoritative state.</summary>
internal static class CelestialImpulseEvaluator
{
    internal static CelestialImpulseEvaluationResult TryEvaluate(ScheduledSimulationEvent pending, SimulationStateView state, SimulationInstant evaluationTime, TimelineRevision timelineRevision)
    {
        if (pending.Header.Kind != SimulationEventKind.CelestialImpulse) return Failure(CelestialImpulseEvaluationStatus.WrongEventKind);
        if (pending.Payload.Kind != SimulationEventPayloadKind.CelestialImpulse) return Failure(CelestialImpulseEvaluationStatus.PayloadMismatch);
        if (pending.Header.Time != evaluationTime) return Failure(CelestialImpulseEvaluationStatus.EventTimeMismatch);
        var subject = pending.Payload.Subject; var deltaVelocity = pending.Payload.DeltaVelocity;
        if (!subject.IsValid) return Failure(CelestialImpulseEvaluationStatus.InvalidSubject);
        if (!deltaVelocity.IsFinite) return Failure(CelestialImpulseEvaluationStatus.InvalidDeltaVelocity);
        if (deltaVelocity.LengthSquared == 0d) return Failure(CelestialImpulseEvaluationStatus.ZeroDeltaVelocity);
        if (!state.Celestial.TryGetDefinition(subject, out var definition) || !state.Celestial.TryGetState(subject, out var bodyState)) return Failure(CelestialImpulseEvaluationStatus.SubjectNotFound);
        if (definition.PrimaryBody is null) return Failure(CelestialImpulseEvaluationStatus.RootBody);
        if (bodyState.Trajectory is not { } currentTrajectory) return Failure(CelestialImpulseEvaluationStatus.NoCurrentTrajectory);

        var propagated = CelestialTrajectoryEvaluator.TryEvaluate(subject, state.Celestial, evaluationTime);
        if (!propagated.Succeeded) return new(CelestialImpulseEvaluationStatus.PropagationFailed, null, propagated.Status, default);
        var replacementState = new CartesianState(propagated.State.Position, propagated.State.Velocity + deltaVelocity);
        if (!replacementState.IsFinite) return Failure(CelestialImpulseEvaluationStatus.NonFiniteResult);
        var replacement = new TwoBodyTrajectory(currentTrajectory.CentralBody, evaluationTime, replacementState, currentTrajectory.Model);
        var creation = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(pending, state, timelineRevision, subject, replacement);
        if (!creation.Succeeded)
        {
            var status = creation.Status == CelestialTrajectoryTransactionStatus.UnsupportedReplacementOrbit ? CelestialImpulseEvaluationStatus.UnsupportedResultingOrbit : CelestialImpulseEvaluationStatus.ReplacementCandidateRejected;
            return new(status, null, default, creation.Status);
        }
        var transaction = creation.Transaction!.Value with { ImpulseAudit = new ProcessedCelestialImpulseAudit(deltaVelocity) };
        return new(CelestialImpulseEvaluationStatus.Success, transaction, default, CelestialTrajectoryTransactionStatus.Success);
    }

    private static CelestialImpulseEvaluationResult Failure(CelestialImpulseEvaluationStatus status) => new(status, null, default, default);
}

internal readonly record struct CelestialImpulseEvaluationResult(CelestialImpulseEvaluationStatus Status, CelestialTrajectoryReplacementTransaction? Transaction, TwoBodyPropagationStatus PropagationStatus, CelestialTrajectoryTransactionStatus ReplacementStatus)
{
    internal bool Succeeded => Status == CelestialImpulseEvaluationStatus.Success;
}
