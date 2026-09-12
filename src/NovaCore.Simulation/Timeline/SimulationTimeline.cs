using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>
/// Authoritative pending-event topology. It owns neither simulation time nor event execution;
/// callers explicitly provide their current authoritative time when scheduling or replacing.
/// </summary>
public sealed partial class SimulationTimeline
{
    internal ContinuationPublicationPhase PublicationPhase { get; } = new();
    private readonly SimulationEventHeap _pending;
    private readonly HashSet<SimulationEventId> _usedIds;
    private readonly List<ScheduledSimulationEvent> _cancelled;
    private ulong _nextSequenceValue = 1;
    private TimelineRevision _revision;

    public SimulationTimeline(int initialCapacity = 0) : this(initialCapacity, 1, TimelineRevision.Zero) { }
    internal SimulationTimeline(int initialCapacity, int contactPayloadCapacity) : this(initialCapacity, 1, TimelineRevision.Zero, contactPayloadCapacity) { }
    internal SimulationTimeline(int initialCapacity, ulong nextSequenceValue, TimelineRevision revision, int contactPayloadCapacity = 0)
    {
        if (initialCapacity < 0 || nextSequenceValue == 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        _pending = new SimulationEventHeap(initialCapacity);
        _usedIds = new HashSet<SimulationEventId>(initialCapacity);
        _cancelled = new List<ScheduledSimulationEvent>(initialCapacity);
        _nextSequenceValue = nextSequenceValue;
        _revision = revision;
        InitializeContactPayloads(contactPayloadCapacity);
    }

    public TimelineRevision Revision { get { PublicationPhase.VerifyRead(); return _revision; } }
    public int PendingCount { get { PublicationPhase.VerifyRead(); return _pending.Count; } }
    public int CancelledCount { get { PublicationPhase.VerifyRead(); return _cancelled.Count; } }
    public bool IsIdReserved(SimulationEventId id) { PublicationPhase.VerifyRead(); return id.IsValid && _usedIds.Contains(id); }
    public bool TryPeekPending(out ScheduledSimulationEvent value) { PublicationPhase.VerifyRead(); return _pending.TryPeek(out value); }
    public bool TryGetPending(SimulationEventId id, out ScheduledSimulationEvent value) { PublicationPhase.VerifyRead(); return _pending.TryGet(id, out value); }
    public int CopyPending(Span<ScheduledSimulationEvent> destination) { PublicationPhase.VerifyRead(); return _pending.CopyTo(destination); }

    internal bool CanConsumeCanonical(SimulationEventHeader expected) =>
        CanAdvanceRevision() && _pending.TryPeek(out var pending) && pending.Header == expected && CanRetireContactPayload(pending);

    internal bool TryConsumeCanonical(SimulationEventHeader expected, out ScheduledSimulationEvent consumed)
    {
        PublicationPhase.VerifyOrdinaryMutation();
        if (!CanConsumeCanonical(expected)) { consumed = default; return false; }
        if (!_pending.TryRemove(expected.Id, out consumed)) throw new InvalidOperationException("Canonical event lookup became inconsistent.");
        RetireContactPayload(consumed);
        _revision = _revision.Next();
        return true;
    }

    public SimulationScheduleResult Schedule(SimulationInstant currentTime, SimulationEventRequest request)
    {
        PublicationPhase.VerifyOrdinaryMutation();
        var status = ValidateSchedule(currentTime, request);
        if (status is not null) return SimulationScheduleResult.Failure(status.Value);
        return CommitSchedule(request);
    }

    private SimulationScheduleStatus? ValidateSchedule(SimulationInstant currentTime, SimulationEventRequest request, bool contactAdmission = false)
    {
        var status = ValidateRequest(currentTime, request, contactAdmission);
        if (status is not null) return status;
        // Reserve ulong.MaxValue as the overflow sentinel so a successful operation can always
        // leave a valid next sequence without partially committing checked arithmetic.
        if (_nextSequenceValue == 0 || _nextSequenceValue == ulong.MaxValue) return SimulationScheduleStatus.SequenceOverflow;
        if (!CanAdvanceRevision()) return SimulationScheduleStatus.RevisionOverflow;
        return null;
    }

    private SimulationScheduleResult CommitSchedule(SimulationEventRequest request)
    {
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
        PublicationPhase.VerifyOrdinaryMutation();
        if (!id.IsValid) return SimulationCancelResult.Failure(SimulationCancelStatus.InvalidId);
        if (!_pending.TryGet(id, out var pending)) return SimulationCancelResult.Failure(SimulationCancelStatus.NotPending);
        if (!CanRetireContactPayload(pending)) throw new InvalidOperationException("Pending contact payload ownership is inconsistent.");
        if (!CanAdvanceRevision()) return SimulationCancelResult.Failure(SimulationCancelStatus.RevisionOverflow);
        _pending.TryRemove(id, out var removed);
        _cancelled.Add(removed);
        RetireContactPayload(removed);
        _revision = _revision.Next();
        return new SimulationCancelResult(SimulationCancelStatus.Cancelled, removed);
    }

    public SimulationScheduleResult Replace(SimulationInstant currentTime, SimulationEventId oldId, SimulationEventRequest replacement)
    {
        PublicationPhase.VerifyOrdinaryMutation();
        var status = ValidateReplacement(currentTime, oldId, replacement);
        if (status is not null) return SimulationScheduleResult.Failure(status.Value);
        return CommitReplacement(oldId, replacement);
    }

    private SimulationScheduleStatus? ValidateReplacement(SimulationInstant currentTime, SimulationEventId oldId, SimulationEventRequest replacement, bool contactAdmission = false)
    {
        if (!oldId.IsValid || !_pending.TryGet(oldId, out var oldEvent)) return SimulationScheduleStatus.ReplacementTargetNotPending;
        if (!CanRetireContactPayload(oldEvent)) return SimulationScheduleStatus.InvalidPayload;
        var status = ValidateRequest(currentTime, replacement, contactAdmission);
        if (status is not null) return status;
        if (!CanAdvanceRevision()) return SimulationScheduleStatus.RevisionOverflow;
        if (_nextSequenceValue == ulong.MaxValue) return SimulationScheduleStatus.SequenceOverflow;
        return null;
    }

    private SimulationScheduleResult CommitReplacement(SimulationEventId oldId, SimulationEventRequest replacement)
    {
        var scheduled = CreateScheduled(replacement, _nextSequenceValue);
        // The new node is committed before the old one is removed, so a failed validation never tears topology.
        _pending.Add(scheduled);
        _usedIds.Add(replacement.Id);
        _pending.TryRemove(oldId, out var oldEvent);
        _cancelled.Add(oldEvent);
        RetireContactPayload(oldEvent);
        _nextSequenceValue = checked(_nextSequenceValue + 1);
        _revision = _revision.Next();
        return new SimulationScheduleResult(SimulationScheduleStatus.Scheduled, scheduled);
    }

    public bool ValidateInvariants() { PublicationPhase.VerifyRead(); return _pending.ValidateInvariants(); }

    private SimulationScheduleStatus? ValidateRequest(SimulationInstant currentTime, SimulationEventRequest request, bool contactAdmission = false)
    {
        if (!request.Id.IsValid) return SimulationScheduleStatus.InvalidId;
        if (request.Kind is not (SimulationEventKind.Marker or SimulationEventKind.ReplaceTrajectory or SimulationEventKind.NoOpMarker or SimulationEventKind.CelestialImpulse or SimulationEventKind.RigidBodyTorque or SimulationEventKind.SpacecraftForce or SimulationEventKind.SpacecraftContactImpulse)) return SimulationScheduleStatus.InvalidKind;
        if (!request.Payload.IsCompatibleWith(request.Kind)) return SimulationScheduleStatus.InvalidPayload;
        if (request.Kind == SimulationEventKind.SpacecraftContactImpulse && !contactAdmission)
            return SimulationScheduleStatus.InvalidPayload;
        if (_usedIds.Contains(request.Id)) return SimulationScheduleStatus.DuplicateId;
        if (request.Time < currentTime) return SimulationScheduleStatus.PastTime;
        return null;
    }
    private static ScheduledSimulationEvent CreateScheduled(SimulationEventRequest request, ulong sequence) =>
        new(new SimulationEventHeader(request.Id, request.Time, request.Priority, new SimulationEventSequence(sequence), request.Kind), request.Payload);
    private bool CanAdvanceRevision() => _revision.Value != ulong.MaxValue;
}
