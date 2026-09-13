using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private readonly ProcessedPersistentContact[] _persistentContactHistory;
    private int _persistentContactCount;
    private long _persistentContactEpisode;
    internal bool OwnsPersistentPublicationPhase => _clock.PublicationPhase.IsOwnedBy(this);
    internal int ProcessedPersistentContactCount { get { _clock.PublicationPhase.VerifyRead(); return _persistentContactCount; } }
    internal int PersistentContactHistoryCapacity => _persistentContactHistory.Length;

    internal bool TryGetProcessedPersistentContact(int index, out ProcessedPersistentContact record)
    {
        _clock.PublicationPhase.VerifyRead();
        if ((uint)index < (uint)_persistentContactCount) { record = _persistentContactHistory[index]; return true; }
        record = default; return false;
    }

    internal LocalContactStatus BeginPersistentContact(LocalContactWorld world, LocalContactConfiguration configuration,
        LocalContactWorld.Receipt initial)
    {
        var phase = _clock.PublicationPhase;
        if (_isExecutingGroup || !phase.TryEnter(this)) return LocalContactStatus.WrongThread;
        try
        {
            if (_persistentContactEpisode == long.MaxValue) return LocalContactStatus.InvalidInterval;
            var next = _persistentContactEpisode + 1;
            var status = world.BindPublication(this, configuration, initial, next);
            if (status == LocalContactStatus.Success) _persistentContactEpisode = next;
            return status;
        }
        finally { phase.Exit(); }
    }

    internal PersistentContactPublicationResult PublishPersistentContact(LocalContactWorld world,
        LocalContactConfiguration configuration, in PersistentContactPublicationRequest request) =>
        PublishPersistentContactCore(world, configuration, request, failAcknowledgementForTest: false);

    // Narrow internal terminal-path seam. No callback, solver injection or configurable runtime policy.
    internal PersistentContactPublicationResult PublishPersistentContactWithFailedAcknowledgementForTest(
        LocalContactWorld world, LocalContactConfiguration configuration, in PersistentContactPublicationRequest request) =>
        PublishPersistentContactCore(world, configuration, request, failAcknowledgementForTest: true);

    private PersistentContactPublicationResult PublishPersistentContactCore(LocalContactWorld world,
        LocalContactConfiguration configuration, in PersistentContactPublicationRequest request, bool failAcknowledgementForTest)
    {
        var phase = _clock.PublicationPhase;
        if (!phase.IsOwnerThread) return new(PersistentContactPublicationStatus.WrongOwnerThread);
        if (_isExecutingGroup || !phase.TryEnter(this)) return new(PersistentContactPublicationStatus.ReentrantPublication);
        try
        {
            if (world is null || configuration is null) return new(PersistentContactPublicationStatus.InvalidReceipt);
            if (CaptureContinuationClock() != request.Clock) return new(PersistentContactPublicationStatus.ClockConflict);
            var status = world.PreparePublication(this, configuration, request.Receipt,
                out var endpoint, out var episode, out var linear, out var angular);
            if (status != PersistentContactPublicationStatus.Published) return new(status);
            var target = endpoint.Motion.Time;
            if (HasContactProofBoundaryThrough(target)) return new(PersistentContactPublicationStatus.PendingEvent);
            var delta = (Int128)target.Ticks - request.Clock.Time.Ticks;
            if (delta <= 0 || delta > long.MaxValue || request.Clock.Debt.Ticks < 0)
                return new(PersistentContactPublicationStatus.ArithmeticOverflow);
            if (request.Clock.Debt.Ticks < delta) return new(PersistentContactPublicationStatus.InsufficientDebt);
            var view = _state.CreateView();
            if (view.Revision.Value == ulong.MaxValue) return new(PersistentContactPublicationStatus.StateRevisionOverflow);
            if (_persistentContactCount >= _persistentContactHistory.Length)
                return new(PersistentContactPublicationStatus.HistoryCapacityFailure);
            if (!_state.TryPrepareContinuationSlot(linear, angular, out var slot))
                return new(PersistentContactPublicationStatus.StateConflict);
            var motion = endpoint.Motion;
            if (!motion.PositionRoot.IsFinite || !motion.VelocityRoot.IsFinite || !motion.AngularVelocityBody.IsFinite ||
                !Finite(motion.BodyToRoot) || motion.BodyToRoot == default ||
                motion.Revision != view.Revision || motion.Spacecraft != linear.Spacecraft || motion.RootFrame != linear.RootFrame)
                return new(PersistentContactPublicationStatus.InvalidEndpoint);

            // Install represented bits. No evaluator, renormalization or solver replay at this boundary.
            var afterLinear = linear with { Epoch = target, PositionRoot = motion.PositionRoot, VelocityRoot = motion.VelocityRoot };
            var afterAngular = angular with { Epoch = target, OrientationLocalToParent = motion.BodyToRoot,
                AngularVelocityBody = motion.AngularVelocityBody };
            var revision = new StateRevision(view.Revision.Value + 1);
            var clock = request.Clock with { Time = target, Debt = new(request.Clock.Debt.Ticks - (long)delta) };
            var count = _persistentContactCount + 1;
            var record = new ProcessedPersistentContact(1, _persistentContactCount, episode, endpoint.Frontier,
                linear, angular, afterLinear, afterAngular, motion.Properties, request.Clock, clock,
                view.Revision, revision, _clock.Timeline.Revision);
            var observation = new PersistentContactObservation(afterLinear, afterAngular, motion.Properties, clock,
                revision, record.TimelineRevision, episode.Sequence, endpoint.Frontier, record.Index);
            var success = new PersistentContactPublicationResult(PersistentContactPublicationStatus.Published, observation);
            var terminal = new PersistentContactPublicationResult(PersistentContactPublicationStatus.CanonicalCommittedPrivateInvalidated, observation);
            if (!LocalContactWorld.Acknowledgement.TryPrepare(world, this, observation, out var acknowledgement))
                return new(PersistentContactPublicationStatus.StateConflict);

            // Final applicability and indexed destination checks while the SAME owner phase is held.
            status = world.PreparePublication(this, configuration, request.Receipt, out var rechecked,
                out _, out _, out _);
            if (status != PersistentContactPublicationStatus.Published) return new(status);
            if (rechecked != endpoint || CaptureContinuationClock() != request.Clock ||
                _state.CreateView().Revision != record.BeforeRevision ||
                !_state.TryPrepareContinuationSlot(linear, angular, out var recheckedSlot) || recheckedSlot != slot)
                return new(PersistentContactPublicationStatus.StateConflict);

            // FIXED CANONICAL COMMIT. Reuse the same paired-slot and clock primitives as M14.17.
            _state.InstallCertifiedContinuation(slot, afterLinear, afterAngular, revision);
            _clock.InstallCertifiedContinuation(clock.Time, clock.Debt);
            _persistentContactHistory[record.Index] = record;
            _persistentContactCount = count;
            // Canonical commit is irrevocable. Private acknowledgement cannot return an ordinary refusal.
            try
            {
                if (failAcknowledgementForTest) { acknowledgement.Invalidate(); return terminal; }
                acknowledgement.Commit();
            }
            catch (Exception) { acknowledgement.Invalidate(); return terminal; }
            return success;
        }
        finally { phase.Exit(); }
    }
}
