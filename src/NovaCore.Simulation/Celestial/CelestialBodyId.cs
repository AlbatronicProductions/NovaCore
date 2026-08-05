namespace NovaCore.Simulation.Celestial;

/// <summary>Stable authoritative identity for one celestial body. It is distinct from frame, event, and render identities.</summary>
internal readonly record struct CelestialBodyId(ulong Value)
{
    public static CelestialBodyId Invalid => new(0);
    public bool IsValid => Value != 0;
    public int CompareTo(CelestialBodyId other) => Value.CompareTo(other.Value);
    public static bool operator <(CelestialBodyId left, CelestialBodyId right) => left.Value < right.Value;
    public static bool operator >(CelestialBodyId left, CelestialBodyId right) => left.Value > right.Value;
}
