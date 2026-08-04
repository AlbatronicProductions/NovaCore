namespace NovaCore.Simulation.Timeline;

/// <summary>Monotonic revision of pending-event topology. Zero is the initial empty timeline.</summary>
public readonly record struct TimelineRevision(ulong Value)
{
    public static TimelineRevision Zero => new(0);
    internal TimelineRevision Next() => new(checked(Value + 1));
}
