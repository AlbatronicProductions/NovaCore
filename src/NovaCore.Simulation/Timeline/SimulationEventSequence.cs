namespace NovaCore.Simulation.Timeline;

/// <summary>Canonical sequence assigned only by a future authoritative timeline transaction.</summary>
public readonly record struct SimulationEventSequence(ulong Value)
{
    public static SimulationEventSequence Unassigned => new(0);
    public bool IsAssigned => Value != 0;
    public int CompareTo(SimulationEventSequence other) => Value.CompareTo(other.Value);
    internal SimulationEventSequence Next() => new(checked(Value + 1));
    public static bool operator <(SimulationEventSequence left, SimulationEventSequence right) => left.Value < right.Value;
    public static bool operator >(SimulationEventSequence left, SimulationEventSequence right) => left.Value > right.Value;
}
