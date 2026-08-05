using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Celestial.Transactions;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Transactions;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using System.Diagnostics;

namespace NovaCore.Simulation.Transactions;

/// <summary>The only component allowed to commit authoritative state and consume pending events.</summary>
internal sealed class SimulationTransactionEngine
{
    private readonly SimulationClock _clock;
    private readonly SimulationState _state;
    private readonly List<ProcessedSimulationEvent> _history;
    private readonly List<ProcessedSpacecraftAttitudeTransition> _spacecraftAttitudeHistory;
    private readonly List<ProcessedRigidBodyTorqueTransition> _rigidBodyTorqueHistory;
    private bool _isExecutingGroup;
    private readonly SimulationExecutionOrchestrator _orchestrator;

    public SimulationTransactionEngine(SimulationClock clock, SimulationState state, int initialHistoryCapacity = 0)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (initialHistoryCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialHistoryCapacity));
        _history = new List<ProcessedSimulationEvent>(initialHistoryCapacity);
        _spacecraftAttitudeHistory = new List<ProcessedSpacecraftAttitudeTransition>(initialHistoryCapacity);
        _rigidBodyTorqueHistory = new List<ProcessedRigidBodyTorqueTransition>(initialHistoryCapacity);
        _orchestrator = new SimulationExecutionOrchestrator(_clock, this);
    }

    public SimulationStateView State => _state.CreateView();
    public int ProcessedCount => _history.Count;
    internal int ProcessedSpacecraftAttitudeCount => _spacecraftAttitudeHistory.Count;
    internal int ProcessedRigidBodyTorqueCount => _rigidBodyTorqueHistory.Count;
    internal bool TryGetProcessedSpacecraftAttitude(int index, out ProcessedSpacecraftAttitudeTransition value)
    { if ((uint)index < (uint)_spacecraftAttitudeHistory.Count) { value = _spacecraftAttitudeHistory[index]; return true; } value = default; return false; }
    internal bool TryGetProcessedRigidBodyTorque(int index, out ProcessedRigidBodyTorqueTransition value)
    { if ((uint)index < (uint)_rigidBodyTorqueHistory.Count) { value = _rigidBodyTorqueHistory[index]; return true; } value = default; return false; }
    public bool TryGetProcessed(int index, out ProcessedSimulationEvent value)
    {
        if ((uint)index < (uint)_history.Count) { value = _history[index]; return true; }
        value = default; return false;
    }

    public SimulationTransaction EvaluateNext()
    {
        if (!_clock.Timeline.TryPeekPending(out var pending)) return default;
        return SimulationEventEvaluator.Evaluate(pending, _state.CreateView(), _clock.CurrentTime, _clock.Timeline.Revision);
    }

    /// <summary>
    /// Executes exactly the canonical pending event at the authoritative clock boundary. It does
    /// not advance to a future boundary; <see cref="SimulationClock"/> remains responsible for that.
    /// </summary>
    public SimulationTransactionResult ExecuteCanonicalPendingEvent() => ValidateAndCommit(EvaluateNext());

    /// <summary>
    /// Orchestrates the canonical pending events at exactly the current clock instant. Each event
    /// retains independent evaluation, validation, and atomic commit semantics.
    /// </summary>
    public SimulationCanonicalGroupResult ExecuteCanonicalGroup() => ExecuteCanonicalGroup(_clock.MaximumEventsPerAdvance);

    /// <summary>Executes one same-time group without exceeding the supplied call-wide event budget.</summary>
    internal SimulationCanonicalGroupResult ExecuteCanonicalGroup(int maximumEventCount)
    {
        if (maximumEventCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumEventCount));
        Debug.Assert(maximumEventCount <= _clock.MaximumEventsPerAdvance);
        var groupTime = _clock.CurrentTime;
        if (_isExecutingGroup) return GroupResult(SimulationCanonicalGroupStopReason.ReentrantExecution, groupTime, 0, false, null);
        _isExecutingGroup = true;
        try
        {
            if (!_clock.Timeline.TryPeekPending(out var pending)) return GroupResult(SimulationCanonicalGroupStopReason.NoPendingEvent, groupTime, 0, true, null);
            if (pending.Header.Time != groupTime) return GroupResult(SimulationCanonicalGroupStopReason.NotAtBoundary, groupTime, 0, false, pending.Header);

            var processed = 0;
            while (true)
            {
                if (!_clock.Timeline.TryPeekPending(out pending))
                    return GroupResult(SimulationCanonicalGroupStopReason.Completed, groupTime, processed, true, null);
                if (pending.Header.Time != groupTime)
                    return GroupResult(SimulationCanonicalGroupStopReason.Completed, groupTime, processed, true, pending.Header);
                if (processed == maximumEventCount)
                    return GroupResult(SimulationCanonicalGroupStopReason.EventLimitReached, groupTime, processed, false, pending.Header);

                var result = ExecuteCanonicalPendingEvent();
                if (!result.Committed)
                    return GroupResult(SimulationCanonicalGroupStopReason.ValidationRejected, groupTime, processed, false, pending.Header);
                processed++;
            }
        }
        finally { _isExecutingGroup = false; }
    }

    /// <summary>
    /// Coasts to one boundary, executes at most that one canonical group, then resumes coasting
    /// toward the requested time without executing a second boundary group.
    /// </summary>
    public SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroup(SimulationInstant target) =>
        _orchestrator.AdvanceAndExecuteOneCanonicalGroup(target);

    /// <summary>Internal clock-duration composition entry point; public duration advancement remains deferred.</summary>
    internal SimulationDebtServiceResult ServicePendingHostDurationDebt() =>
        _orchestrator.ServicePendingHostDurationDebt();

    public SimulationTransactionResult ValidateAndCommit(SimulationTransaction transaction)
    {
        if (transaction.CelestialReplacement is { } celestialReplacement)
        {
            var celestial = ValidateAndCommit(celestialReplacement);
            return new(
                celestial.Committed ? SimulationTransactionStatus.Committed : SimulationTransactionStatus.ValidationFailed,
                celestial.Committed ? SimulationTransactionValidationResult.Valid : new(SimulationTransactionValidationStatus.InvalidTransaction),
                celestial.ProcessedEvent,
                transaction.CelestialImpulseStatus,
                celestial.Status);
        }
        if (transaction.RigidBodyTorqueReplacement is { } rigidBodyReplacement)
        {
            var rigid = ValidateAndCommit(rigidBodyReplacement, transaction.Event);
            return new(rigid.Committed ? SimulationTransactionStatus.Committed : SimulationTransactionStatus.ValidationFailed,
                rigid.Committed ? SimulationTransactionValidationResult.Valid : new(SimulationTransactionValidationStatus.InvalidTransaction), rigid.Committed ? GetLastProcessed() : null);
        }
        var validation = Validate(transaction);
        if (!validation.IsValid) return new(SimulationTransactionStatus.ValidationFailed, validation, null, transaction.CelestialImpulseStatus);

        // Capacity is reserved before any authoritative mutation. All remaining operations are validated,
        // allocation-free state/list/heap updates with no normal failure path.
        if (_history.Count == _history.Capacity) _history.EnsureCapacity(checked(_history.Count + 1));
        var timelineBefore = _clock.Timeline.Revision;
        var stateBefore = _state.CreateView().Revision;
        if (transaction.ChangesAuthoritativeState) _state.CommitMarkerValue(transaction.ProposedMarkerValue);
        if (!_clock.Timeline.TryConsumeCanonical(transaction.Event, out _)) throw new InvalidOperationException("Validated canonical event could not be consumed.");
        _clock.AdvanceAfterSuccessfulTransaction(transaction.EvaluationTime);
        var processed = new ProcessedSimulationEvent(transaction.Event, transaction.EvaluationTime, timelineBefore, _clock.Timeline.Revision, stateBefore, _state.CreateView().Revision);
        _history.Add(processed);
        return new(SimulationTransactionStatus.Committed, SimulationTransactionValidationResult.Valid, processed);
    }

    /// <summary>Commits one already-evaluated celestial replacement without introducing a second state mutation path.</summary>
    internal CelestialTrajectoryTransactionResult ValidateAndCommit(CelestialTrajectoryReplacementTransaction transaction)
    {
        var validation = Validate(transaction);
        if (validation != CelestialTrajectoryTransactionStatus.Success) return new(validation, null);

        // Celestial commits require preallocated history capacity so no fallible operation remains after mutation.
        var timelineBefore = _clock.Timeline.Revision;
        var stateBefore = _state.CreateView().Revision;
        if (!_state.CommitCelestialTrajectoryReplacement(transaction.Subject, transaction.ExpectedTrajectory, transaction.ReplacementTrajectory, out var storeStatus))
            throw new InvalidOperationException($"Validated celestial trajectory replacement failed in store: {storeStatus}.");
        if (!_clock.Timeline.TryConsumeCanonical(transaction.Event.Header, out _))
            throw new InvalidOperationException("Validated canonical celestial event could not be consumed.");
        _clock.AdvanceAfterSuccessfulTransaction(transaction.EvaluationTime);
        var stateAfter = _state.CreateView().Revision;
        var transition = new ProcessedCelestialTrajectoryTransition(
            transaction.Subject,
            transaction.EvaluationTime,
            transaction.ExpectedTrajectory.Epoch,
            transaction.ReplacementTrajectory.Epoch,
            stateBefore,
            stateAfter,
            TwoBodyTrajectoryIdentity.ComputeHash(transaction.ExpectedTrajectory),
            TwoBodyTrajectoryIdentity.ComputeHash(transaction.ReplacementTrajectory),
            transaction.ImpulseAudit);
        var processed = new ProcessedSimulationEvent(
            transaction.Event.Header,
            transaction.EvaluationTime,
            timelineBefore,
            _clock.Timeline.Revision,
            stateBefore,
            stateAfter,
            transition);
        _history.Add(processed);
        return new(CelestialTrajectoryTransactionStatus.Success, processed);
    }

    /// <summary>Commits one pure direct attitude candidate. It neither consumes a timeline event nor advances time.</summary>
    internal SpacecraftAttitudeTransactionResult ValidateAndCommit(SpacecraftAttitudeReplacementTransaction transaction)
    {
        var validation = Validate(transaction);
        if (validation != SpacecraftAttitudeTransactionStatus.Success) return new(validation, null);
        if (_spacecraftAttitudeHistory.Count == _spacecraftAttitudeHistory.Capacity)
            _spacecraftAttitudeHistory.EnsureCapacity(checked(_spacecraftAttitudeHistory.Count + 1));
        var before = _state.CreateView().Revision;
        if (!_state.CommitSpacecraftAttitudeReplacement(transaction.Subject, transaction.ExpectedAttitude, transaction.ReplacementAttitude, out var storeStatus))
            throw new InvalidOperationException($"Validated spacecraft attitude replacement failed in store: {storeStatus}.");
        var after = _state.CreateView().Revision;
        var processed = new ProcessedSpacecraftAttitudeTransition(transaction.Subject, transaction.EvaluationTime, before, after, SpacecraftAttitudeTransactionEvaluator.ComputeHash(transaction.ExpectedAttitude), SpacecraftAttitudeTransactionEvaluator.ComputeHash(transaction.ReplacementAttitude));
        _spacecraftAttitudeHistory.Add(processed);
        return new(SpacecraftAttitudeTransactionStatus.Success, processed);
    }

    private RigidBodyTorqueTransactionResult ValidateAndCommit(RigidBodyTorqueReplacementTransaction transaction, SimulationEventHeader eventHeader)
    {
        var validation = Validate(transaction, eventHeader);
        if (validation != RigidBodyTorqueTransactionStatus.Success) return new(validation, null);
        var timelineBefore = _clock.Timeline.Revision; var stateBefore = _state.CreateView().Revision;
        if (!_state.CommitSpacecraftRigidBodyReplacement(transaction.Subject, transaction.ExpectedRotation, transaction.ReplacementRotation, out var storeStatus))
            throw new InvalidOperationException($"Validated rigid-body replacement failed in store: {storeStatus}.");
        if (!_clock.Timeline.TryConsumeCanonical(eventHeader, out _)) throw new InvalidOperationException("Validated rigid-body event could not be consumed.");
        _clock.AdvanceAfterSuccessfulTransaction(transaction.EvaluationTime);
        var after = _state.CreateView().Revision;
        var transition = new ProcessedRigidBodyTorqueTransition(transaction.Subject, transaction.EvaluationTime, stateBefore, after, RigidBodyTorqueTransactionEvaluator.ComputeHash(transaction.ExpectedRotation), RigidBodyTorqueTransactionEvaluator.ComputeHash(transaction.ReplacementRotation));
        _rigidBodyTorqueHistory.Add(transition);
        _history.Add(new ProcessedSimulationEvent(eventHeader, transaction.EvaluationTime, timelineBefore, _clock.Timeline.Revision, stateBefore, after));
        return new(RigidBodyTorqueTransactionStatus.Success, transition);
    }

    private SimulationTransactionValidationResult Validate(SimulationTransaction transaction)
    {
        if (!_clock.Timeline.TryPeekPending(out var pending)) return new(SimulationTransactionValidationStatus.NoPendingEvent);
        if (pending.Header != transaction.Event) return new(SimulationTransactionValidationStatus.EventNotCanonical);
        if (transaction.EvaluationTime != _clock.CurrentTime || transaction.Event.Time != _clock.CurrentTime) return new(SimulationTransactionValidationStatus.EvaluationTimeMismatch);
        if (transaction.ExpectedTimelineRevision != _clock.Timeline.Revision) return new(SimulationTransactionValidationStatus.TimelineRevisionMismatch);
        var state = _state.CreateView();
        if (transaction.ExpectedStateRevision != state.Revision) return new(SimulationTransactionValidationStatus.StateRevisionMismatch);
        if (!transaction.IsInternallyConsistent) return new(SimulationTransactionValidationStatus.InvalidTransaction);
        if (transaction.ChangesAuthoritativeState && transaction.ProposedMarkerValue != state.MarkerValue + 1) return new(SimulationTransactionValidationStatus.InvalidTransaction);
        if (!transaction.ChangesAuthoritativeState && transaction.ProposedMarkerValue != state.MarkerValue) return new(SimulationTransactionValidationStatus.InvalidTransaction);
        if (transaction.ChangesAuthoritativeState && state.Revision.Value == ulong.MaxValue) return new(SimulationTransactionValidationStatus.StateRevisionOverflow);
        if (!_clock.Timeline.CanConsumeCanonical(transaction.Event)) return new(SimulationTransactionValidationStatus.TimelineRevisionOverflow);
        return SimulationTransactionValidationResult.Valid;
    }

    private CelestialTrajectoryTransactionStatus Validate(CelestialTrajectoryReplacementTransaction transaction)
    {
        if (transaction.Event.Header.Kind is not (SimulationEventKind.ReplaceTrajectory or SimulationEventKind.CelestialImpulse))
            return CelestialTrajectoryTransactionStatus.InvalidEventKind;
        if (!_clock.Timeline.TryPeekPending(out var pending) || pending.Header != transaction.Event.Header)
            return CelestialTrajectoryTransactionStatus.EventNotCanonical;
        if (transaction.Event.Header.Time != transaction.EvaluationTime)
            return CelestialTrajectoryTransactionStatus.EventTimeMismatch;
        if (_clock.CurrentTime != transaction.EvaluationTime)
            return CelestialTrajectoryTransactionStatus.ClockMismatch;
        if (_clock.Timeline.Revision != transaction.ExpectedTimelineRevision)
            return CelestialTrajectoryTransactionStatus.TimelineRevisionMismatch;
        var state = _state.CreateView();
        if (state.Revision != transaction.ExpectedStateRevision)
            return CelestialTrajectoryTransactionStatus.StateRevisionMismatch;
        if (_history.Count == _history.Capacity)
            return CelestialTrajectoryTransactionStatus.HistoryCapacityFailure;
        if (state.Revision.Value == ulong.MaxValue)
            return CelestialTrajectoryTransactionStatus.StateRevisionOverflow;
        if (!_clock.Timeline.CanConsumeCanonical(transaction.Event.Header))
            return CelestialTrajectoryTransactionStatus.TimelineRevisionOverflow;
        return CelestialTrajectoryTransactionEvaluator.ValidateReplacement(
            transaction.EvaluationTime,
            state.Celestial,
            transaction.Subject,
            transaction.ExpectedTrajectory,
            transaction.ReplacementTrajectory,
            requireExpected: true,
            out _);
    }

    private SpacecraftAttitudeTransactionStatus Validate(SpacecraftAttitudeReplacementTransaction transaction)
    {
        if (transaction.EvaluationTime != _clock.CurrentTime) return SpacecraftAttitudeTransactionStatus.TimeMismatch;
        var state = _state.CreateView();
        if (transaction.ExpectedStateRevision != state.Revision) return SpacecraftAttitudeTransactionStatus.StateRevisionMismatch;
        if (_spacecraftAttitudeHistory.Count == _spacecraftAttitudeHistory.Capacity) return SpacecraftAttitudeTransactionStatus.HistoryCapacityFailure;
        if (state.Revision.Value == ulong.MaxValue) return SpacecraftAttitudeTransactionStatus.StateRevisionOverflow;
        return SpacecraftAttitudeTransactionEvaluator.Validate(state, transaction.EvaluationTime, transaction.Subject, transaction.ExpectedAttitude, transaction.ReplacementAttitude, true);
    }

    private RigidBodyTorqueTransactionStatus Validate(RigidBodyTorqueReplacementTransaction transaction, SimulationEventHeader eventHeader)
    {
        if (!_clock.Timeline.TryPeekPending(out var pending) || pending.Header != eventHeader || eventHeader.Kind != SimulationEventKind.RigidBodyTorque) return RigidBodyTorqueTransactionStatus.RotationBasisMismatch;
        if (transaction.EvaluationTime != _clock.CurrentTime || eventHeader.Time != _clock.CurrentTime) return RigidBodyTorqueTransactionStatus.TimeMismatch;
        var state = _state.CreateView();
        if (transaction.ExpectedStateRevision != state.Revision) return RigidBodyTorqueTransactionStatus.StateRevisionMismatch;
        if (_history.Count == _history.Capacity || _rigidBodyTorqueHistory.Count == _rigidBodyTorqueHistory.Capacity) return RigidBodyTorqueTransactionStatus.HistoryCapacityFailure;
        if (state.Revision.Value == ulong.MaxValue) return RigidBodyTorqueTransactionStatus.StateRevisionOverflow;
        return RigidBodyTorqueTransactionEvaluator.Validate(state, transaction.EvaluationTime, transaction.Subject, transaction.ExpectedRotation, transaction.ReplacementRotation, true);
    }

    private ProcessedSimulationEvent? GetLastProcessed() => _history.Count == 0 ? null : _history[^1];

    internal SimulationCanonicalGroupResult ExecuteCanonicalGroupWhileGuardedForTest()
    {
        if (_isExecutingGroup) throw new InvalidOperationException("Test seam cannot nest its own group guard.");
        _isExecutingGroup = true;
        try { return ExecuteCanonicalGroup(); }
        finally { _isExecutingGroup = false; }
    }

    internal SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(SimulationInstant target) =>
        _orchestrator.AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(target);

    private SimulationCanonicalGroupResult GroupResult(SimulationCanonicalGroupStopReason reason, SimulationInstant groupTime, int processed, bool complete, SimulationEventHeader? pending) =>
        new(reason, groupTime, processed, complete, pending, _clock.Timeline.Revision, _state.CreateView().Revision);

}
