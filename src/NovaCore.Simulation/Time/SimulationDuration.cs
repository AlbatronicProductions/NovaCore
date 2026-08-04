namespace NovaCore.Simulation.Time;

/// <summary>Signed exact duration in one-millionth-of-a-second simulation ticks.</summary>
public readonly record struct SimulationDuration(long Ticks)
{
    public const long TicksPerSecond = 1_000_000;
    public static SimulationDuration Zero => new(0);
    public bool IsNegative => Ticks < 0;
    public bool IsZero => Ticks == 0;
    public double TotalSeconds => Ticks / (double)TicksPerSecond;

    public static SimulationDuration FromWholeSeconds(long seconds) => new(checked(seconds * TicksPerSecond));
    public static SimulationDuration FromTicks(long ticks) => new(ticks);
    public static SimulationDuration FromSecondsRounded(double seconds)
    {
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        var ticks = seconds * TicksPerSecond;
        if (ticks < long.MinValue || ticks > long.MaxValue) throw new OverflowException();
        return new(checked((long)Math.Round(ticks, MidpointRounding.ToEven)));
    }
    public SimulationDuration Abs()
    {
        if (Ticks == long.MinValue) throw new OverflowException("The absolute value is not representable.");
        return new(Math.Abs(Ticks));
    }
    public int CompareTo(SimulationDuration other) => Ticks.CompareTo(other.Ticks);
    public static SimulationDuration operator +(SimulationDuration left, SimulationDuration right) => new(checked(left.Ticks + right.Ticks));
    public static SimulationDuration operator -(SimulationDuration left, SimulationDuration right) => new(checked(left.Ticks - right.Ticks));
    public static SimulationDuration operator -(SimulationDuration value) => new(checked(-value.Ticks));
    public static bool operator <(SimulationDuration left, SimulationDuration right) => left.Ticks < right.Ticks;
    public static bool operator >(SimulationDuration left, SimulationDuration right) => left.Ticks > right.Ticks;
    public static bool operator <=(SimulationDuration left, SimulationDuration right) => left.Ticks <= right.Ticks;
    public static bool operator >=(SimulationDuration left, SimulationDuration right) => left.Ticks >= right.Ticks;
}
