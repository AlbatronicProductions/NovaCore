namespace NovaCore.Simulation.Spacecraft;

/// <summary>Stable spacecraft identity; zero is never valid and is distinct from all other NovaCore IDs.</summary>
internal readonly record struct SpacecraftId(ulong Value)
{
    internal static SpacecraftId Invalid => new(0);
    internal bool IsValid => Value != 0;
}
