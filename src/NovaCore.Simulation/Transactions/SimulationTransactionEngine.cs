using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

/// <summary>The only component allowed to commit authoritative state and consume pending events.</summary>
internal sealed class SimulationTransactionEngine
{
    private readonly SimulationClock _clock;
    private readonly SimulationState _state;
    private readonly List<ProcessedSimulationEvent> _history;

    public SimulationTransactionEngine(SimulationClock clock, SimulationState state, int initialHistoryCapacity = 0)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (initialHistoryCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialHistoryCapacity));
        _history = new List<ProcessedSimulationEvent>(initialHistoryCapacity);
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

    public SimulationTransactionResult ValidateAndCommit(SimulationTransaction transaction)
    {
        var validation = Validate(transaction);
        if (!validation.IsValid) return new(SimulationTransactionStatus.ValidationFailed, validation, null);

        // Capacity is reserved before any authoritative mutation. All remaining operations are validated,
        // allocation-free state/list/heap updates with no normal failure path.
        if (_history.Count == _history.Capacity) _history.EnsureCapacity(checked(_history.Count + 1));
        var timelineBefore = _clock.Timeline.Revision;
        var stateBefore = _state.CreateView().Revision;
        _state.CommitMarkerValue(transaction.ProposedMarkerValue);
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
        if (!transaction.IsInternallyConsistent || !transaction.ChangesAuthoritativeState || transaction.ProposedMarkerValue != state.MarkerValue + 1) return new(SimulationTransactionValidationStatus.InvalidTransaction);
        if (state.Revision.Value == ulong.MaxValue) return new(SimulationTransactionValidationStatus.StateRevisionOverflow);
        if (!_clock.Timeline.CanConsumeCanonical(transaction.Event)) return new(SimulationTransactionValidationStatus.TimelineRevisionOverflow);
        return SimulationTransactionValidationResult.Valid;
    }
}
