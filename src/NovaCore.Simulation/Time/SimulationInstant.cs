namespace NovaCore.Simulation.Time;

/// <summary>Exact authoritative simulation timestamp relative to the project-defined zero epoch.</summary>
public readonly record struct SimulationInstant(long Ticks)
{
    public const long TicksPerSecond = SimulationDuration.TicksPerSecond;
    public static SimulationInstant Zero => new(0);
    public double SecondsSinceEpoch => Ticks / (double)TicksPerSecond;

    public static SimulationInstant FromWholeSeconds(long seconds) => new(checked(seconds * TicksPerSecond));
    public static SimulationInstant FromSecondsRounded(double seconds)
    {
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        var ticks = seconds * TicksPerSecond;
        if (ticks < long.MinValue || ticks > long.MaxValue) throw new OverflowException();
        return new(checked((long)Math.Round(ticks, MidpointRounding.ToEven)));
    }
    public int CompareTo(SimulationInstant other) => Ticks.CompareTo(other.Ticks);
    public static SimulationInstant operator +(SimulationInstant instant, SimulationDuration duration) => new(checked(instant.Ticks + duration.Ticks));
    public static SimulationInstant operator -(SimulationInstant instant, SimulationDuration duration) => new(checked(instant.Ticks - duration.Ticks));
    public static SimulationDuration operator -(SimulationInstant left, SimulationInstant right) => new(checked(left.Ticks - right.Ticks));
    public static bool operator <(SimulationInstant left, SimulationInstant right) => left.Ticks < right.Ticks;
    public static bool operator >(SimulationInstant left, SimulationInstant right) => left.Ticks > right.Ticks;
    public static bool operator <=(SimulationInstant left, SimulationInstant right) => left.Ticks <= right.Ticks;
    public static bool operator >=(SimulationInstant left, SimulationInstant right) => left.Ticks >= right.Ticks;
}
