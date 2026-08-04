using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>
/// Authoritative pending-event topology. It owns neither simulation time nor event execution;
/// callers explicitly provide their current authoritative time when scheduling or replacing.
/// </summary>
public sealed class SimulationTimeline
{
    private readonly SimulationEventHeap _pending;
    private readonly HashSet<SimulationEventId> _usedIds;
    private readonly List<ScheduledSimulationEvent> _cancelled;
    private ulong _nextSequenceValue = 1;
    private TimelineRevision _revision;

    public SimulationTimeline(int initialCapacity = 0) : this(initialCapacity, 1, TimelineRevision.Zero) { }
    internal SimulationTimeline(int initialCapacity, ulong nextSequenceValue, TimelineRevision revision)
    {
        if (initialCapacity < 0 || nextSequenceValue == 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _pending = new SimulationEventHeap(initialCapacity);
        _usedIds = new HashSet<SimulationEventId>(initialCapacity);
        _cancelled = new List<ScheduledSimulationEvent>(initialCapacity);
        _nextSequenceValue = nextSequenceValue;
        _revision = revision;
    }

    public TimelineRevision Revision => _revision;
    public int PendingCount => _pending.Count;
    public int CancelledCount => _cancelled.Count;
    public bool IsIdReserved(SimulationEventId id) => id.IsValid && _usedIds.Contains(id);
    public bool TryPeekPending(out ScheduledSimulationEvent value) => _pending.TryPeek(out value);
    public bool TryGetPending(SimulationEventId id, out ScheduledSimulationEvent value) => _pending.TryGet(id, out value);
    public int CopyPending(Span<ScheduledSimulationEvent> destination) => _pending.CopyTo(destination);

    public SimulationScheduleResult Schedule(SimulationInstant currentTime, SimulationEventRequest request)
    {
        var status = ValidateRequest(currentTime, request);
        if (status is not null) return SimulationScheduleResult.Failure(status.Value);
        // Reserve ulong.MaxValue as the overflow sentinel so a successful operation can always
        // leave a valid next sequence without partially committing checked arithmetic.
        if (_nextSequenceValue == 0 || _nextSequenceValue == ulong.MaxValue) return SimulationScheduleResult.Failure(SimulationScheduleStatus.SequenceOverflow);
        if (!CanAdvanceRevision()) return SimulationScheduleResult.Failure(SimulationScheduleStatus.RevisionOverflow);

        var scheduled = CreateScheduled(request, _nextSequenceValue);
        // All validation precedes these three commit operations; only these mutate timeline topology.
        _pending.Add(scheduled);
        _usedIds.Add(request.Id);
        _nextSequenceValue = checked(_nextSequenceValue + 1);
        _revision = _revision.Next();
        return new SimulationScheduleResult(SimulationScheduleStatus.Scheduled, scheduled);
    }

    public SimulationCancelResult Cancel(SimulationEventId id)
    {
        if (!id.IsValid) return SimulationCancelResult.Failure(SimulationCancelStatus.InvalidId);
        if (!_pending.TryGet(id, out _)) return SimulationCancelResult.Failure(SimulationCancelStatus.NotPending);
        if (!CanAdvanceRevision()) return SimulationCancelResult.Failure(SimulationCancelStatus.RevisionOverflow);
        _pending.TryRemove(id, out var removed);
        _cancelled.Add(removed);
        _revision = _revision.Next();
        return new SimulationCancelResult(SimulationCancelStatus.Cancelled, removed);
    }

    public SimulationScheduleResult Replace(SimulationInstant currentTime, SimulationEventId oldId, SimulationEventRequest replacement)
    {
        if (!oldId.IsValid || !_pending.TryGet(oldId, out _)) return SimulationScheduleResult.Failure(SimulationScheduleStatus.ReplacementTargetNotPending);
        var status = ValidateRequest(currentTime, replacement);
        if (status is not null) return SimulationScheduleResult.Failure(status.Value);
        if (!CanAdvanceRevision()) return SimulationScheduleResult.Failure(SimulationScheduleStatus.RevisionOverflow);
        if (_nextSequenceValue == ulong.MaxValue) return SimulationScheduleResult.Failure(SimulationScheduleStatus.SequenceOverflow);

        var scheduled = CreateScheduled(replacement, _nextSequenceValue);
        // The new node is committed before the old one is removed, so a failed validation never tears topology.
        _pending.Add(scheduled);
        _usedIds.Add(replacement.Id);
        _pending.TryRemove(oldId, out var oldEvent);
        _cancelled.Add(oldEvent);
        _nextSequenceValue = checked(_nextSequenceValue + 1);
        _revision = _revision.Next();
        return new SimulationScheduleResult(SimulationScheduleStatus.Scheduled, scheduled);
    }

    public bool ValidateInvariants() => _pending.ValidateInvariants();

    private SimulationScheduleStatus? ValidateRequest(SimulationInstant currentTime, SimulationEventRequest request)
    {
        if (!request.Id.IsValid) return SimulationScheduleStatus.InvalidId;
        if (request.Kind is not (SimulationEventKind.Marker or SimulationEventKind.ReplaceTrajectory)) return SimulationScheduleStatus.InvalidKind;
        if (_usedIds.Contains(request.Id)) return SimulationScheduleStatus.DuplicateId;
        if (request.Time < currentTime) return SimulationScheduleStatus.PastTime;
        return null;
    }
    private static ScheduledSimulationEvent CreateScheduled(SimulationEventRequest request, ulong sequence) =>
        new(new SimulationEventHeader(request.Id, request.Time, request.Priority, new SimulationEventSequence(sequence), request.Kind));
    private bool CanAdvanceRevision() => _revision.Value != ulong.MaxValue;
}
