using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private readonly List<ProcessedSpacecraftContactImpulse> _contactImpulseHistory;
    internal int ProcessedContactImpulseCount => _contactImpulseHistory.Count;
    internal bool TryGetProcessedContactImpulse(int index, out ProcessedSpacecraftContactImpulse value)
    {
        if ((uint)index < (uint)_contactImpulseHistory.Count) { value = _contactImpulseHistory[index]; return true; }
        value = default;
        return false;
    }

    private SimulationTransactionResult ValidateAndCommitContactEnvelope(SimulationTransaction transaction)
    {
        if (transaction.ContactImpulseReplacement is not { } response ||
            transaction.Event != response.Event || transaction.EvaluationTime != response.Intent.Time ||
            transaction.ExpectedTimelineRevision != response.ExpectedTimelineRevision ||
            transaction.ExpectedStateRevision != response.Intent.ExpectedStateRevision ||
            !transaction.IsInternallyConsistent || !transaction.ChangesAuthoritativeState ||
            transaction.ProposedMarkerValue != _state.CreateView().MarkerValue ||
            transaction.CelestialImpulseStatus is not null || transaction.CelestialReplacement is not null ||
            transaction.RigidBodyTorqueReplacement is not null || transaction.SpacecraftForceReplacement is not null)
            return new(SimulationTransactionStatus.ValidationFailed, new(SimulationTransactionValidationStatus.InvalidTransaction), null);
        var status = ValidateAndCommit(response);
        return new(status == ContactImpulseStatus.Success ? SimulationTransactionStatus.Committed : SimulationTransactionStatus.ValidationFailed,
            status == ContactImpulseStatus.Success ? SimulationTransactionValidationResult.Valid : new(SimulationTransactionValidationStatus.InvalidTransaction),
            status == ContactImpulseStatus.Success ? GetLastProcessed() : null);
    }

    /// <summary>
    /// Recompute from the canonical intent, not caller-supplied replacements. No callbacks, external queries,
    /// allocations or normal rejection paths remain after complete preflight in this exclusive single-writer phase.
    /// </summary>
    internal ContactImpulseStatus ValidateAndCommit(SpacecraftContactImpulseTransaction transaction)
    {
        if (!_clock.Timeline.TryPeekPending(out var pending) || pending.Header != transaction.Event ||
            pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse) return ContactImpulseStatus.EventMismatch;
        if (transaction.Event.Time != _clock.CurrentTime || transaction.Intent.Time != _clock.CurrentTime)
            return ContactImpulseStatus.TimeMismatch;
        if (transaction.ExpectedTimelineRevision != _clock.Timeline.Revision) return ContactImpulseStatus.EventMismatch;
        var state = _state.CreateView();
        if (transaction.Intent.ExpectedStateRevision != state.Revision) return ContactImpulseStatus.StateRevisionMismatch;
        var status = SpacecraftContactImpulseEvaluator.TryCreate(state, _clock.Timeline, pending, _clock.CurrentTime, _clock.Timeline.Revision, out var canonical);
        if (status != ContactImpulseStatus.Success) return status;
        if (transaction != canonical) return ContactImpulseStatus.InvalidReplacement;
        if (_history.Count == _history.Capacity || _contactImpulseHistory.Count == _contactImpulseHistory.Capacity)
            return ContactImpulseStatus.HistoryCapacityFailure;
        if (state.Revision.Value == ulong.MaxValue) return ContactImpulseStatus.StateRevisionOverflow;
        if (!_clock.Timeline.CanConsumeCanonical(transaction.Event)) return ContactImpulseStatus.TimelineRevisionOverflow;

        // Construct all records and checked successor revisions before mutation. General history is only
        // the event receipt; the contact list contains the sole coupled physical before/after record.
        var after = new StateRevision(checked(state.Revision.Value + 1));
        var timelineBefore = _clock.Timeline.Revision;
        var timelineAfter = new TimelineRevision(checked(timelineBefore.Value + 1));
        var transition = new ProcessedSpacecraftContactImpulse(transaction.Event, transaction.Intent, state.Revision, after,
            transaction.ExpectedTranslation, transaction.ReplacementTranslation, transaction.ExpectedRotation, transaction.ReplacementRotation);
        var receipt = new ProcessedSimulationEvent(transaction.Event, _clock.CurrentTime, timelineBefore, timelineAfter, state.Revision, after);

        _state.CommitContactResponse(transaction.ExpectedTranslation, transaction.ReplacementTranslation,
            transaction.ExpectedRotation, transaction.ReplacementRotation);
        // Cannot reject after CanConsumeCanonical above without forbidden concurrent mutation or internal corruption.
        if (!_clock.Timeline.TryConsumeCanonical(transaction.Event, out _))
            throw new InvalidOperationException("Prevalidated contact event consumption invariant failed.");
        _contactImpulseHistory.Add(transition);
        _history.Add(receipt);
        // Clock already equals the event instant; an instantaneous response does not advance it.
        return ContactImpulseStatus.Success;
    }
}
