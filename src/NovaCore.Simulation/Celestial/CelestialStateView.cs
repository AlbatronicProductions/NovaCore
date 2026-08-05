namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable, allocation-free read view over one authoritative celestial store.</summary>
internal readonly struct CelestialStateView
{
    private readonly CelestialStateStore _store;

    internal CelestialStateView(CelestialStateStore store) => _store = store;

    public int Count => _store.Count;
    public CelestialBodyDefinition GetDefinition(int index) => _store.GetDefinitionAt(index);
    public CelestialBodyState GetState(int index) => _store.GetStateAt(index);
    public bool TryGetIndex(CelestialBodyId id, out int index) => _store.TryGetIndex(id, out index);
    public bool TryGetDefinition(CelestialBodyId id, out CelestialBodyDefinition definition) => _store.TryGetDefinition(id, out definition);
    public bool TryGetState(CelestialBodyId id, out CelestialBodyState state) => _store.TryGetState(id, out state);
}
