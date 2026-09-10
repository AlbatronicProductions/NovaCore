namespace NovaCore.Simulation.Timeline;

/// <summary>Canonical ascending order: time, priority, sequence, then stable event ID.</summary>
public static class SimulationEventHeaderComparer
{
    public static int Compare(SimulationEventHeader left, SimulationEventHeader right)
    {
        var compare = left.Time.CompareTo(right.Time);
        if (compare != 0) return compare;
        return CompareEqualTime(left.Priority, left.Sequence, left.Id, right.Priority, right.Sequence, right.Id);
    }

    // Shared only after exact time equality. The live timeline still compares integral SimulationInstant first.
    internal static int CompareEqualTime(int leftPriority, SimulationEventSequence leftSequence, SimulationEventId leftId,
        int rightPriority, SimulationEventSequence rightSequence, SimulationEventId rightId)
    {
        var compare = leftPriority.CompareTo(rightPriority);
        if (compare != 0) return compare;
        compare = leftSequence.CompareTo(rightSequence);
        return compare != 0 ? compare : leftId.CompareTo(rightId);
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
