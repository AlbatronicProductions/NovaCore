namespace NovaCore.Simulation.Timeline;

/// <summary>Canonical ascending order: time, priority, sequence, then stable event ID.</summary>
public static class SimulationEventHeaderComparer
{
    public static int Compare(SimulationEventHeader left, SimulationEventHeader right)
    {
        var compare = left.Time.CompareTo(right.Time);
        if (compare != 0) return compare;
        compare = left.Priority.CompareTo(right.Priority);
        if (compare != 0) return compare;
        compare = left.Sequence.CompareTo(right.Sequence);
        return compare != 0 ? compare : left.Id.CompareTo(right.Id);
    }

    public static void ValidateStrictlyOrdered(ReadOnlySpan<SimulationEventHeader> headers)
    {
        for (var index = 1; index < headers.Length; index++)
        {
            var compare = Compare(headers[index - 1], headers[index]);
            if (compare == 0) throw new ArgumentException("Duplicate simulation event ordering key.", nameof(headers));
            if (compare > 0) throw new ArgumentException("Simulation event headers are not in canonical order.", nameof(headers));
        }
    }
}
