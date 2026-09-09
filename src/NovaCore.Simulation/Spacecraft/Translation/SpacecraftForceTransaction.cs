using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Translation;

internal readonly record struct SpacecraftForceTransaction(
    SimulationEventHeader Event, TimelineRevision ExpectedTimelineRevision, StateRevision ExpectedStateRevision,
    SpacecraftTranslationState Expected, SpacecraftTranslationState Replacement);

internal readonly record struct ProcessedSpacecraftForceTransition(
    SimulationEventHeader Event, StateRevision Before, StateRevision After,
    SpacecraftTranslationState Expected, SpacecraftTranslationState Replacement);

internal static class SpacecraftForceTransactionEvaluator
{
    internal static SpacecraftTranslationStatus TryCreate(SimulationStateView state, ScheduledSimulationEvent pending,
        SimulationInstant time, TimelineRevision timelineRevision, out SpacecraftForceTransaction transaction)
    {
        transaction = default;
        if (pending.Header.Time != time || pending.Header.Kind != SimulationEventKind.SpacecraftForce ||
            !pending.Payload.IsCompatibleWith(pending.Header.Kind)) return SpacecraftTranslationStatus.EventMismatch;
        if (!state.Spacecraft.TryGetTranslation(pending.Payload.SpacecraftSubject, out var current, out var properties))
            return SpacecraftTranslationStatus.SubjectNotFound;
        var evaluated = SpacecraftTranslationEvaluator.TryEvaluate(current, properties, time);
        if (!evaluated.Succeeded) return evaluated.Status;
        // Identical intent consumes its event but does not alter the segment or revision.
        var replacement = current.ConstantForceRoot == pending.Payload.ForceRoot ? current :
            current with { Epoch = time, PositionRoot = evaluated.PositionRoot, VelocityRoot = evaluated.VelocityRoot,
                ConstantForceRoot = pending.Payload.ForceRoot };
        if (!SpacecraftTranslationEvaluator.TryEvaluate(replacement, properties, time).Succeeded)
            return SpacecraftTranslationStatus.InvalidReplacement;
        transaction = new(pending.Header, timelineRevision, state.Revision, current, replacement);
        return SpacecraftTranslationStatus.Success;
    }
}
