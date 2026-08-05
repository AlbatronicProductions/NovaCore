using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft;

/// <summary>Fixed-size authoritative spacecraft records. Declaration order is canonical; lookup indices allocate only at setup.</summary>
internal sealed class SpacecraftStateStore
{
    private readonly SpacecraftDefinition[] _definitions;
    private readonly SpacecraftAttitudeState[] _attitudes;
    private readonly SpacecraftRigidBodyRotationState[] _rigidBodies;
    private readonly bool[] _hasRigidBody;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;

    private SpacecraftStateStore(SpacecraftDefinition[] definitions, SpacecraftAttitudeState[] attitudes, SpacecraftRigidBodyRotationState[] rigidBodies, bool[] hasRigidBody, ulong[] lookupIds, int[] lookupIndices)
    { _definitions = definitions; _attitudes = attitudes; _rigidBodies = rigidBodies; _hasRigidBody = hasRigidBody; _lookupIds = lookupIds; _lookupIndices = lookupIndices; }

    internal static SpacecraftStateStore Empty { get; } = new([], [], [], [], [], []);
    internal int Count => _definitions.Length;
    internal SpacecraftStateView CreateView() => new(this);

    internal static bool TryCreate(ReadOnlySpan<SpacecraftDefinition> definitions, ReadOnlySpan<SpacecraftAttitudeState> attitudes, out SpacecraftStateStore? store, out SpacecraftStateStoreStatus status)
    {
        store = null;
        if (definitions.Length != attitudes.Length) { status = SpacecraftStateStoreStatus.StateCountMismatch; return false; }
        if (definitions.Length == 0) { store = Empty; status = SpacecraftStateStoreStatus.Success; return true; }
        if (definitions.Length > int.MaxValue / 2) { status = SpacecraftStateStoreStatus.CapacityOverflow; return false; }
        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index]; var attitude = attitudes[index];
            if (!definition.Id.IsValid || !attitude.Spacecraft.IsValid || definition.Id != attitude.Spacecraft) { status = SpacecraftStateStoreStatus.InvalidSpacecraftId; return false; }
            if (definition.CarrierFrame.Value == 0) { status = SpacecraftStateStoreStatus.InvalidCarrierFrame; return false; }
            if (definition.BodyFrame.Value == 0) { status = SpacecraftStateStoreStatus.InvalidBodyFrame; return false; }
            if (definition.CarrierFrame == definition.BodyFrame) { status = SpacecraftStateStoreStatus.CarrierEqualsBodyFrame; return false; }
            if (string.IsNullOrWhiteSpace(definition.DiagnosticName)) { status = SpacecraftStateStoreStatus.InvalidDiagnosticName; return false; }
            if (SpacecraftAttitudeEvaluator.TryCanonicalize(attitude.OrientationLocalToParent, out var canonical) != SpacecraftAttitudeEvaluationStatus.Success || canonical != attitude.OrientationLocalToParent || !attitude.AngularVelocityBody.IsFinite || attitude.Model != SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1) { status = SpacecraftStateStoreStatus.InvalidAttitudeState; return false; }
            for (var prior = 0; prior < index; prior++)
            { if (definitions[prior].Id == definition.Id) { status = SpacecraftStateStoreStatus.DuplicateSpacecraftId; return false; } if (definitions[prior].BodyFrame == definition.BodyFrame) { status = SpacecraftStateStoreStatus.DuplicateBodyFrame; return false; } }
        }
        var definitionCopy = definitions.ToArray(); var attitudeCopy = attitudes.ToArray(); var ids = new ulong[definitions.Length]; var indices = new int[definitions.Length];
        for (var index = 0; index < ids.Length; index++) { ids[index] = definitionCopy[index].Id.Value; indices[index] = index; }
        Array.Sort(ids, indices); store = new(definitionCopy, attitudeCopy, new SpacecraftRigidBodyRotationState[definitionCopy.Length], new bool[definitionCopy.Length], ids, indices); status = SpacecraftStateStoreStatus.Success; return true;
    }

    internal static bool TryCreateRigidBody(ReadOnlySpan<SpacecraftDefinition> definitions, ReadOnlySpan<SpacecraftRigidBodyRotationState> rotations, out SpacecraftStateStore? store, out SpacecraftStateStoreStatus status)
    {
        var attitudes = new SpacecraftAttitudeState[rotations.Length];
        for (var index = 0; index < rotations.Length; index++)
        {
            var rotation = rotations[index];
            if (SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, rotation.Epoch).Status != SpacecraftRigidBodyRotationEvaluationStatus.Success) { store = null; status = SpacecraftStateStoreStatus.InvalidAttitudeState; return false; }
            if (SpacecraftAttitudeState.TryCreate(rotation.Spacecraft, rotation.Epoch, rotation.OrientationLocalToParent, rotation.AngularVelocityBody, SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var attitude) != SpacecraftAttitudeEvaluationStatus.Success) { store = null; status = SpacecraftStateStoreStatus.InvalidAttitudeState; return false; }
            attitudes[index] = attitude;
        }
        if (!TryCreate(definitions, attitudes, out store, out status) || store is null) return false;
        for (var index = 0; index < rotations.Length; index++) { store._rigidBodies[index] = rotations[index]; store._hasRigidBody[index] = true; }
        return true;
    }

    internal SpacecraftDefinition GetDefinitionAt(int index) => _definitions[index];
    internal SpacecraftAttitudeState GetAttitudeAt(int index) => _attitudes[index];
    internal bool TryGetIndex(SpacecraftId id, out int index) { var found = Array.BinarySearch(_lookupIds, id.Value); if (found >= 0) { index = _lookupIndices[found]; return true; } index = -1; return false; }
    internal bool TryGetDefinition(SpacecraftId id, out SpacecraftDefinition value) { if (TryGetIndex(id, out var index)) { value = _definitions[index]; return true; } value = default; return false; }
    internal bool TryGetAttitude(SpacecraftId id, out SpacecraftAttitudeState value) { if (TryGetIndex(id, out var index)) { value = _attitudes[index]; return true; } value = default; return false; }
    internal bool TryGetRigidBody(SpacecraftId id, out SpacecraftRigidBodyRotationState value) { if (TryGetIndex(id, out var index) && _hasRigidBody[index]) { value = _rigidBodies[index]; return true; } value = default; return false; }
    internal bool TryReplaceAttitude(SpacecraftId subject, in SpacecraftAttitudeState expected, in SpacecraftAttitudeState replacement, out SpacecraftStateStoreMutationStatus status)
    { if (!TryGetIndex(subject, out var index)) { status = SpacecraftStateStoreMutationStatus.SubjectNotFound; return false; } if (_attitudes[index] != expected) { status = SpacecraftStateStoreMutationStatus.ExpectedAttitudeMismatch; return false; } _attitudes[index] = replacement; status = SpacecraftStateStoreMutationStatus.Success; return true; }
    internal bool TryReplaceRigidBody(SpacecraftId subject, in SpacecraftRigidBodyRotationState expected, in SpacecraftRigidBodyRotationState replacement, out SpacecraftStateStoreMutationStatus status)
    { if (!TryGetIndex(subject, out var index)) { status = SpacecraftStateStoreMutationStatus.SubjectNotFound; return false; } if (!_hasRigidBody[index] || _rigidBodies[index] != expected) { status = SpacecraftStateStoreMutationStatus.ExpectedAttitudeMismatch; return false; } _rigidBodies[index] = replacement; status = SpacecraftStateStoreMutationStatus.Success; return true; }
}
