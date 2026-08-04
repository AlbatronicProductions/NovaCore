namespace NovaCore.Simulation.Clock;

/// <summary>Immutable clock limits. The cap is retained for future event execution.</summary>
public readonly record struct SimulationClockSettings
{
    public const int DefaultMaximumEventsPerAdvance = 10_000;
    public static SimulationClockSettings Default => new(DefaultMaximumEventsPerAdvance);
    public int MaximumEventsPerAdvance { get; }

    public SimulationClockSettings(int maximumEventsPerAdvance = DefaultMaximumEventsPerAdvance)
    {
        if (maximumEventsPerAdvance <= 0) throw new ArgumentOutOfRangeException(nameof(maximumEventsPerAdvance));
        MaximumEventsPerAdvance = maximumEventsPerAdvance;
    }
}
