namespace NovaCore.Simulation.Spacecraft;

/// <summary>Fixed-size authoritative spacecraft records. Declaration order is canonical; lookup indices allocate only at setup.</summary>
internal sealed class SpacecraftStateStore
{
    private readonly SpacecraftDefinition[] _definitions;
    private readonly SpacecraftAttitudeState[] _attitudes;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;

    private SpacecraftStateStore(SpacecraftDefinition[] definitions, SpacecraftAttitudeState[] attitudes, ulong[] lookupIds, int[] lookupIndices)
    { _definitions = definitions; _attitudes = attitudes; _lookupIds = lookupIds; _lookupIndices = lookupIndices; }

    internal static SpacecraftStateStore Empty { get; } = new([], [], [], []);
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
        Array.Sort(ids, indices); store = new(definitionCopy, attitudeCopy, ids, indices); status = SpacecraftStateStoreStatus.Success; return true;
    }

    internal SpacecraftDefinition GetDefinitionAt(int index) => _definitions[index];
    internal SpacecraftAttitudeState GetAttitudeAt(int index) => _attitudes[index];
    internal bool TryGetIndex(SpacecraftId id, out int index) { var found = Array.BinarySearch(_lookupIds, id.Value); if (found >= 0) { index = _lookupIndices[found]; return true; } index = -1; return false; }
    internal bool TryGetDefinition(SpacecraftId id, out SpacecraftDefinition value) { if (TryGetIndex(id, out var index)) { value = _definitions[index]; return true; } value = default; return false; }
    internal bool TryGetAttitude(SpacecraftId id, out SpacecraftAttitudeState value) { if (TryGetIndex(id, out var index)) { value = _attitudes[index]; return true; } value = default; return false; }
    internal bool TryReplaceAttitude(SpacecraftId subject, in SpacecraftAttitudeState expected, in SpacecraftAttitudeState replacement, out SpacecraftStateStoreMutationStatus status)
    { if (!TryGetIndex(subject, out var index)) { status = SpacecraftStateStoreMutationStatus.SubjectNotFound; return false; } if (_attitudes[index] != expected) { status = SpacecraftStateStoreMutationStatus.ExpectedAttitudeMismatch; return false; } _attitudes[index] = replacement; status = SpacecraftStateStoreMutationStatus.Success; return true; }
}
