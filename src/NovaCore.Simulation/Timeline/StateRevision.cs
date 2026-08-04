namespace NovaCore.Simulation.Timeline;

/// <summary>Reserved immutable revision for future authoritative simulation state snapshots.</summary>
public readonly record struct StateRevision(ulong Value)
{
    public static StateRevision Zero => new(0);
}
