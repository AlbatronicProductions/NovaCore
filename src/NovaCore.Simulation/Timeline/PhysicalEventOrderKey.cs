using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>
/// Ordering identity only: no payload, scheduling, ID allocation, execution or publication.
/// Producers supply authoritative sequence/ID; completion order does not assign them here.
/// </summary>
internal readonly struct PhysicalEventOrderKey : IComparable<PhysicalEventOrderKey>
{
    internal PhysicalEventEpoch Epoch { get; }
    internal int Priority { get; }
    internal SimulationEventSequence Sequence { get; }
    internal SimulationEventId Id { get; }

    internal PhysicalEventOrderKey(PhysicalEventEpoch epoch, int priority, SimulationEventSequence sequence, SimulationEventId id)
    {
        if (!sequence.IsAssigned) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (!id.IsValid) throw new ArgumentOutOfRangeException(nameof(id));
        Epoch = epoch; Priority = priority; Sequence = sequence; Id = id;
    }

    internal static PhysicalEventOrderKey FromCanonical(SimulationEventHeader header) =>
        new(PhysicalEventEpoch.FromCanonical(header.Time), header.Priority, header.Sequence, header.Id);

    public int CompareTo(PhysicalEventOrderKey other)
    {
        var epoch = Epoch.CompareTo(other.Epoch);
        return epoch != 0 ? epoch : SimulationEventHeaderComparer.CompareEqualTime(
            Priority, Sequence, Id, other.Priority, other.Sequence, other.Id);
    }
}
