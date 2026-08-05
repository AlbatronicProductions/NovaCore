using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using System.Diagnostics;

namespace NovaCore.Simulation.Transactions;

/// <summary>The only component allowed to commit authoritative state and consume pending events.</summary>
internal sealed class SimulationTransactionEngine
{
    private readonly SimulationClock _clock;
    private readonly SimulationState _state;
    private readonly List<ProcessedSimulationEvent> _history;
    private bool _isExecutingGroup;
    private readonly SimulationExecutionOrchestrator _orchestrator;

    public SimulationTransactionEngine(SimulationClock clock, SimulationState state, int initialHistoryCapacity = 0)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (initialHistoryCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialHistoryCapacity));
        _history = new List<ProcessedSimulationEvent>(initialHistoryCapacity);
        _orchestrator = new SimulationExecutionOrchestrator(_clock, this);
    }

    public SimulationStateView State => _state.CreateView();
    public int ProcessedCount => _history.Count;
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
        var validation = Validate(transaction);
        if (!validation.IsValid) return new(SimulationTransactionStatus.ValidationFailed, validation, null);

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
