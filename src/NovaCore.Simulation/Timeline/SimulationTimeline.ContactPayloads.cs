using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

public sealed partial class SimulationTimeline
{
    // Only explicitly contact-capable timelines allocate this array. No per-event objects/map.
    private ContactPayloadSlot[] _contactPayloads = Array.Empty<ContactPayloadSlot>();
    private int _contactFreeHead = -1;
    internal int ContactPayloadCount { get; private set; }
    internal int ContactPayloadCapacity => _contactPayloads.Length;

    internal struct ContactPayloadSlot
    {
        internal SpacecraftContactImpulseIntent Intent;
        internal SimulationEventHeader Event;
        internal int NextFree;
    }

    private void InitializeContactPayloads(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (capacity == 0) return;
        _contactPayloads = new ContactPayloadSlot[capacity];
        for (var i = 0; i < capacity; i++) _contactPayloads[i].NextFree = i + 1 < capacity ? i + 1 : -1;
        _contactFreeHead = 0;
    }

    internal SimulationScheduleResult ScheduleContactImpulse(SimulationInstant currentTime, SimulationEventId id,
        int priority, SpacecraftContactImpulseIntent intent) => AdmitContactImpulse(currentTime, default, id, priority, intent, false);

    internal SimulationScheduleResult ReplaceContactImpulse(SimulationInstant currentTime, SimulationEventId oldId,
        SimulationEventId id, int priority, SpacecraftContactImpulseIntent intent) => AdmitContactImpulse(currentTime, oldId, id, priority, intent, true);

    private SimulationScheduleResult AdmitContactImpulse(SimulationInstant currentTime, SimulationEventId oldId,
        SimulationEventId id, int priority, SpacecraftContactImpulseIntent intent, bool replace)
    {
        if (!intent.IsValid) return SimulationScheduleResult.Failure(SimulationScheduleStatus.InvalidPayload);
        if (_contactFreeHead < 0) return SimulationScheduleResult.Failure(SimulationScheduleStatus.ContactPayloadCapacityExceeded);
        var index = _contactFreeHead;
        var request = new SimulationEventRequest(id, intent.Time, priority, SimulationEventKind.SpacecraftContactImpulse,
            SimulationEventPayload.ContactHandle(index + 1));
        var status = replace ? ValidateReplacement(currentTime, oldId, request, true) : ValidateSchedule(currentTime, request, true);
        if (status is not null) return SimulationScheduleResult.Failure(status.Value);

        // All ordinary rejection paths precede publication. Exclusive single-writer contract:
        // no callback/concurrent reader may observe the slot before the pending event is installed.
        ref var slot = ref _contactPayloads[index];
        _contactFreeHead = slot.NextFree;
        slot = new ContactPayloadSlot { Intent = intent, Event = CreateScheduled(request, _nextSequenceValue).Header, NextFree = -1 };
        ContactPayloadCount++;
        return replace ? CommitReplacement(oldId, request) : CommitSchedule(request);
    }

    internal bool TryResolveContactImpulse(ScheduledSimulationEvent pending, out SpacecraftContactImpulseIntent intent)
    {
        intent = default;
        if (pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse ||
            !_pending.TryGet(pending.Header.Id, out var canonical) || canonical != pending || !HasContactPayload(pending)) return false;
        intent = _contactPayloads[pending.Payload.ContactSlot - 1].Intent;
        return true;
    }

    private bool HasContactPayload(ScheduledSimulationEvent pending)
    {
        var index = pending.Payload.ContactSlot - 1;
        // Full header includes the never-reused sequence: it is the slot occupancy generation.
        return pending.Payload.IsCompatibleWith(pending.Header.Kind) && (uint)index < (uint)_contactPayloads.Length &&
            _contactPayloads[index].Event == pending.Header;
    }

    private bool CanRetireContactPayload(ScheduledSimulationEvent pending) =>
        pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse || HasContactPayload(pending);

    private void RetireContactPayload(ScheduledSimulationEvent pending)
    {
        if (pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse) return;
        // Caller preflighted ownership before any mutation. Clear all provenance references.
        var index = pending.Payload.ContactSlot - 1;
        _contactPayloads[index] = new ContactPayloadSlot { NextFree = _contactFreeHead };
        _contactFreeHead = index;
        ContactPayloadCount--;
    }

    internal bool ValidateContactPayloadInvariants()
    {
        var active = 0;
        for (var i = 0; i < _contactPayloads.Length; i++)
        {
            ref var slot = ref _contactPayloads[i];
            if (!slot.Event.Id.IsValid) continue;
            if (!_pending.TryGet(slot.Event.Id, out var pending) || pending.Header != slot.Event ||
                pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse || pending.Payload.ContactSlot != i + 1 ||
                !slot.Intent.IsValid || slot.Intent.Time != slot.Event.Time) return false;
            active++;
        }
        var free = 0;
        for (var index = _contactFreeHead; index != -1; index = _contactPayloads[index].NextFree)
        {
            if ((uint)index >= (uint)_contactPayloads.Length || _contactPayloads[index].Event.Id.IsValid ||
                _contactPayloads[index].Intent != default || ++free > _contactPayloads.Length) return false;
        }
        return active == ContactPayloadCount && active + free == _contactPayloads.Length;
    }
}
