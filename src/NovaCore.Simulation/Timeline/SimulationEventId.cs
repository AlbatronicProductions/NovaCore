namespace NovaCore.Simulation.Timeline;

public readonly record struct SimulationEventId(ulong Value)
{
    public static SimulationEventId Invalid => new(0);
    public bool IsValid => Value != 0;
    public int CompareTo(SimulationEventId other) => Value.CompareTo(other.Value);
    public static bool operator <(SimulationEventId left, SimulationEventId right) => left.Value < right.Value;
    public static bool operator >(SimulationEventId left, SimulationEventId right) => left.Value > right.Value;
}
