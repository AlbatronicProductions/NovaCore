namespace NovaCore.Simulation.Spacecraft;

/// <summary>Allocation-free read-only traversal over the authoritative fixed spacecraft store.</summary>
internal readonly struct SpacecraftStateView
{
    private readonly SpacecraftStateStore _store;
    internal SpacecraftStateView(SpacecraftStateStore store) => _store = store;
    public int Count => _store.Count;
    internal SpacecraftDefinition GetDefinition(int index) => _store.GetDefinitionAt(index);
    internal SpacecraftAttitudeState GetAttitude(int index) => _store.GetAttitudeAt(index);
    internal bool TryGetIndex(SpacecraftId id, out int index) => _store.TryGetIndex(id, out index);
    internal bool TryGetDefinition(SpacecraftId id, out SpacecraftDefinition definition) => _store.TryGetDefinition(id, out definition);
    internal bool TryGetAttitude(SpacecraftId id, out SpacecraftAttitudeState attitude) => _store.TryGetAttitude(id, out attitude);
}
