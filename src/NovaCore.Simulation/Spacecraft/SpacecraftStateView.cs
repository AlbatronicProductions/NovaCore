namespace NovaCore.Simulation.Spacecraft;

using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;

/// <summary>Borrowed read view. State-owned views refuse newer slots after their captured revision expires.</summary>
internal readonly struct SpacecraftStateView
{
    private readonly SpacecraftStateStore _store;
    private readonly SimulationState? _owner;
    private readonly StateRevision _revision;
    internal SpacecraftStateView(SpacecraftStateStore store) => _store = store;
    internal SpacecraftStateView(SpacecraftStateStore store, SimulationState owner, StateRevision revision)
    { _store = store; _owner = owner; _revision = revision; }
    private bool Current => _owner is not null ? _owner.IsBorrowCurrent(_revision) : _store.Owner is null;
    private void Verify() { if (!Current) throw new InvalidOperationException("Spacecraft state borrow expired; acquire a current view or retain copied values."); }
    public int Count { get { Verify(); return _store.Count; } }
    internal bool TryGetTranslation(SpacecraftId id, out SpacecraftTranslationState translation, out SpacecraftPhysicalProperties properties)
    { if (Current) return _store.TryGetTranslation(id, out translation, out properties); translation = default; properties = default; return false; }
    internal SpacecraftDefinition GetDefinition(int index) { Verify(); return _store.GetDefinitionAt(index); }
    internal SpacecraftAttitudeState GetAttitude(int index) { Verify(); return _store.GetAttitudeAt(index); }
    internal bool TryGetIndex(SpacecraftId id, out int index)
    { if (Current) return _store.TryGetIndex(id, out index); index = -1; return false; }
    internal bool TryGetDefinition(SpacecraftId id, out SpacecraftDefinition definition)
    { if (Current) return _store.TryGetDefinition(id, out definition); definition = default; return false; }
    internal bool TryGetAttitude(SpacecraftId id, out SpacecraftAttitudeState attitude)
    { if (Current) return _store.TryGetAttitude(id, out attitude); attitude = default; return false; }
    /// <summary>Rigid-body state is the authoritative rotation source when present.</summary>
    internal bool TryGetRigidBody(SpacecraftId id, out SpacecraftRigidBodyRotationState rotation)
    { if (Current) return _store.TryGetRigidBody(id, out rotation); rotation = default; return false; }
}
