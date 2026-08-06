namespace NovaCore.Simulation.Celestial;

/// <summary>Stable authored identity for one immutable celestial-system definition.</summary>
internal readonly record struct CelestialSystemId(ulong Value)
{
    public static CelestialSystemId Invalid => new(0);
    public bool IsValid => Value != 0;
}
