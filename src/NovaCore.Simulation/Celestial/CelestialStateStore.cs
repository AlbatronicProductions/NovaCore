using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Fixed-size authoritative celestial records. Caller declaration order is canonical; ID lookup uses a setup-time sorted index.</summary>
internal sealed class CelestialStateStore
{
    private static readonly CelestialBodyDefinition[] EmptyDefinitions = [];
    private static readonly CelestialBodyState[] EmptyStates = [];
    private static readonly ulong[] EmptyLookupIds = [];
    private static readonly int[] EmptyLookupIndices = [];
    private readonly CelestialBodyDefinition[] _definitions;
    private readonly CelestialBodyState[] _states;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;

    private CelestialStateStore(CelestialBodyDefinition[] definitions, CelestialBodyState[] states, ulong[] lookupIds, int[] lookupIndices)
    {
        _definitions = definitions; _states = states; _lookupIds = lookupIds; _lookupIndices = lookupIndices;
    }

    public static CelestialStateStore Empty { get; } = new(EmptyDefinitions, EmptyStates, EmptyLookupIds, EmptyLookupIndices);
    public int Count => _definitions.Length;
    public CelestialStateView CreateView() => new(this);

    public static bool TryCreate(ReadOnlySpan<CelestialBodyDefinition> definitions, ReadOnlySpan<CelestialBodyState> states, out CelestialStateStore? store, out CelestialStateStoreStatus status)
    {
        store = null;
        if (definitions.Length != states.Length) { status = CelestialStateStoreStatus.StateCountMismatch; return false; }
        if (definitions.Length == 0) { store = Empty; status = CelestialStateStoreStatus.Success; return true; }
        if (definitions.Length > int.MaxValue / 2) { status = CelestialStateStoreStatus.CapacityOverflow; return false; }

        for (var index = 0; index < definitions.Length; index++)
        {
            ref readonly var definition = ref definitions[index];
            ref readonly var state = ref states[index];
            if (!definition.Id.IsValid || !state.Id.IsValid) { status = CelestialStateStoreStatus.InvalidBodyId; return false; }
            if (definition.InertialFrame.Value == 0) { status = CelestialStateStoreStatus.InvalidInertialFrame; return false; }
            if (!double.IsFinite(definition.GravitationalParameter) || definition.GravitationalParameter <= 0d) { status = CelestialStateStoreStatus.InvalidGravitationalParameter; return false; }
            if (definition.PrimaryBody == definition.Id) { status = CelestialStateStoreStatus.SelfPrimaryBody; return false; }
            if (state.Id != definition.Id) { status = CelestialStateStoreStatus.StateDefinitionMismatch; return false; }
            for (var prior = 0; prior < index; prior++)
            {
                if (definitions[prior].Id == definition.Id) { status = CelestialStateStoreStatus.DuplicateBodyId; return false; }
                if (definitions[prior].InertialFrame == definition.InertialFrame) { status = CelestialStateStoreStatus.DuplicateInertialFrame; return false; }
            }
        }

        for (var index = 0; index < definitions.Length; index++)
        {
            ref readonly var definition = ref definitions[index];
            ref readonly var state = ref states[index];
            if (definition.PrimaryBody is not { } primary)
            {
                if (state.Trajectory is not null) { status = CelestialStateStoreStatus.RootTrajectoryNotAllowed; return false; }
                continue;
            }
            if (FindDefinitionIndex(definitions, primary) < 0) { status = CelestialStateStoreStatus.MissingPrimaryBody; return false; }
            if (state.Trajectory is not { } trajectory) { status = CelestialStateStoreStatus.ChildTrajectoryRequired; return false; }
            if (!trajectory.CentralBody.IsValid) { status = CelestialStateStoreStatus.InvalidTrajectoryCentralBody; return false; }
            if (trajectory.CentralBody != primary) { status = CelestialStateStoreStatus.TrajectoryPrimaryMismatch; return false; }
            if (trajectory.Model != TwoBodyPropagationModel.CartesianTwoBodyV1) { status = CelestialStateStoreStatus.InvalidTrajectoryModel; return false; }
            if (!trajectory.StateAtEpoch.IsFinite) { status = CelestialStateStoreStatus.NonFiniteCartesianState; return false; }
        }

        for (var index = 0; index < definitions.Length; index++)
        {
            var slow = index; var fast = index;
            while (true)
            {
                slow = NextPrimaryIndex(definitions, slow);
                fast = NextPrimaryIndex(definitions, NextPrimaryIndex(definitions, fast));
                if (slow < 0 || fast < 0) break;
                if (slow == fast) { status = CelestialStateStoreStatus.PrimaryBodyCycle; return false; }
            }
        }

        var definitionCopy = definitions.ToArray(); var stateCopy = states.ToArray();
        var lookupIds = new ulong[definitionCopy.Length]; var lookupIndices = new int[definitionCopy.Length];
        for (var index = 0; index < lookupIds.Length; index++) { lookupIds[index] = definitionCopy[index].Id.Value; lookupIndices[index] = index; }
        Array.Sort(lookupIds, lookupIndices);
        store = new CelestialStateStore(definitionCopy, stateCopy, lookupIds, lookupIndices);
        status = CelestialStateStoreStatus.Success;
        return true;
    }

    internal CelestialBodyDefinition GetDefinitionAt(int index) => _definitions[index];
    internal CelestialBodyState GetStateAt(int index) => _states[index];
    internal bool TryGetIndex(CelestialBodyId id, out int index)
    {
        var found = Array.BinarySearch(_lookupIds, id.Value);
        if (found >= 0) { index = _lookupIndices[found]; return true; }
        index = -1; return false;
    }
    internal bool TryGetDefinition(CelestialBodyId id, out CelestialBodyDefinition definition)
    {
        if (TryGetIndex(id, out var index)) { definition = _definitions[index]; return true; }
        definition = default; return false;
    }
    internal bool TryGetState(CelestialBodyId id, out CelestialBodyState state)
    {
        if (TryGetIndex(id, out var index)) { state = _states[index]; return true; }
        state = default; return false;
    }

    private static int FindDefinitionIndex(ReadOnlySpan<CelestialBodyDefinition> definitions, CelestialBodyId id)
    {
        for (var index = 0; index < definitions.Length; index++) if (definitions[index].Id == id) return index;
        return -1;
    }
    private static int NextPrimaryIndex(ReadOnlySpan<CelestialBodyDefinition> definitions, int index) =>
        index < 0 || definitions[index].PrimaryBody is not { } primary ? -1 : FindDefinitionIndex(definitions, primary);
}
