namespace NovaCore.Simulation.Celestial;

using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;

/// <summary>Borrowed read view; captured state revisions never label newer live slots.</summary>
internal readonly struct CelestialStateView
{
    private readonly CelestialStateStore _store;
    private readonly SimulationState? _owner;
    private readonly StateRevision _revision;

    internal CelestialStateView(CelestialStateStore store) => _store = store;
    internal CelestialStateView(CelestialStateStore store, SimulationState owner, StateRevision revision)
    { _store = store; _owner = owner; _revision = revision; }
    private bool Current => _owner is not null ? _owner.IsBorrowCurrent(_revision) : _store.Owner is null;
    private void Verify() { if (!Current) throw new InvalidOperationException("Celestial state borrow expired; acquire a current view or retain copied values."); }

    public int Count { get { Verify(); return _store.Count; } }
    public CelestialBodyDefinition GetDefinition(int index) { Verify(); return _store.GetDefinitionAt(index); }
    public CelestialBodyState GetState(int index) { Verify(); return _store.GetStateAt(index); }
    public bool TryGetIndex(CelestialBodyId id, out int index)
    { if (Current) return _store.TryGetIndex(id, out index); index = -1; return false; }
    public bool TryGetDefinition(CelestialBodyId id, out CelestialBodyDefinition definition)
    { if (Current) return _store.TryGetDefinition(id, out definition); definition = default; return false; }
    public bool TryGetState(CelestialBodyId id, out CelestialBodyState state)
    { if (Current) return _store.TryGetState(id, out state); state = default; return false; }
}
