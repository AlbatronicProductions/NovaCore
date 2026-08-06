using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable validated authored hierarchy. It owns no dynamic state, propagation, frames, or render data.</summary>
internal sealed class CelestialSystemDefinition
{
    private readonly CelestialHierarchyNode[] _nodes;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;
    private readonly int[] _traversalIndices;

    private CelestialSystemDefinition(CelestialSystemId id, CelestialSystemTimeMapping timeMapping, CelestialEphemerisMetadata ephemerisMetadata, CelestialHierarchyNode[] nodes, ulong[] lookupIds, int[] lookupIndices, int[] traversalIndices, CelestialBodyId rootBody)
    {
        Id = id; TimeMapping = timeMapping; EphemerisMetadata = ephemerisMetadata; _nodes = nodes; _lookupIds = lookupIds; _lookupIndices = lookupIndices; _traversalIndices = traversalIndices; RootBody = rootBody;
    }

    public CelestialSystemId Id { get; }
    public CelestialSystemTimeMapping TimeMapping { get; }
    public CelestialEphemerisMetadata EphemerisMetadata { get; }
    public CelestialBodyId RootBody { get; }
    public int Count => _nodes.Length;

    public static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping timeMapping, CelestialEphemerisMetadata ephemerisMetadata, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
    {
        definition = null;
        validation = CelestialSystemValidator.Validate(id, nodes, timeMapping, ephemerisMetadata);
        if (!validation.Succeeded) return false;
        if (nodes.Length > int.MaxValue / 2) { validation = new(CelestialSystemValidationStatus.CapacityOverflow); return false; }

        var copy = nodes.ToArray();
        var lookupIds = new ulong[copy.Length]; var lookupIndices = new int[copy.Length];
        for (var index = 0; index < copy.Length; index++) { lookupIds[index] = copy[index].Id.Value; lookupIndices[index] = index; }
        Array.Sort(lookupIds, lookupIndices);
        var traversal = BuildTraversal(copy, validation.RootIndex);
        definition = new(id, timeMapping, ephemerisMetadata, copy, lookupIds, lookupIndices, traversal, copy[validation.RootIndex].Id);
        return true;
    }

    public CelestialHierarchyNode GetNode(int index) => _nodes[index];
    public CelestialHierarchyNode GetNodeInTraversalOrder(int traversalIndex) => _nodes[_traversalIndices[traversalIndex]];
    public bool TryGetNode(CelestialBodyId id, out CelestialHierarchyNode node)
    {
        var index = Array.BinarySearch(_lookupIds, id.Value);
        if (index >= 0) { node = _nodes[_lookupIndices[index]]; return true; }
        node = default; return false;
    }
    public CelestialSystemTimeMappingStatus TryMapTime(Simulation.Time.SimulationInstant instant, out CelestialTimeArgument argument)
    {
        var status = TimeMapping.TryMap(instant, out argument);
        return status != CelestialSystemTimeMappingStatus.Success ? status : EphemerisMetadata.Contains(argument.WholeDomainTicks) ? CelestialSystemTimeMappingStatus.Success : CelestialSystemTimeMappingStatus.OutsideSupportedInterval;
    }

    private static int[] BuildTraversal(ReadOnlySpan<CelestialHierarchyNode> nodes, int rootIndex)
    {
        var result = new int[nodes.Length]; result[0] = rootIndex; var written = 1;
        while (written < result.Length)
        {
            var selected = -1;
            for (var candidate = 0; candidate < nodes.Length; candidate++)
            {
                if (Contains(result, written, candidate) || nodes[candidate].ParentId is not { } parent || !ContainsBody(nodes, result, written, parent)) continue;
                if (selected < 0 || nodes[candidate].Id.Value < nodes[selected].Id.Value) selected = candidate;
            }
            result[written++] = selected;
        }
        return result;
    }

    private static bool Contains(ReadOnlySpan<int> values, int count, int value)
    {
        for (var index = 0; index < count; index++) if (values[index] == value) return true;
        return false;
    }
    private static bool ContainsBody(ReadOnlySpan<CelestialHierarchyNode> nodes, ReadOnlySpan<int> values, int count, CelestialBodyId id)
    {
        for (var index = 0; index < count; index++) if (nodes[values[index]].Id == id) return true;
        return false;
    }
}

/// <summary>Pure structural validator. It performs no allocation and does not retain caller data.</summary>
internal static class CelestialSystemValidator
{
    internal static CelestialSystemValidationResult Validate(CelestialSystemId id, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping timeMapping, CelestialEphemerisMetadata ephemerisMetadata)
    {
        if (!id.IsValid) return new(CelestialSystemValidationStatus.InvalidSystemId);
        if (!timeMapping.DomainAnchor.Domain.IsValid || !ephemerisMetadata.Domain.IsValid) return new(CelestialSystemValidationStatus.InvalidTimeDomain);
        if (timeMapping.DomainAnchor.DomainTicksPerSecond <= 0) return new(CelestialSystemValidationStatus.InvalidDomainTickRate);
        if (timeMapping.ScaleNumerator <= 0) return new(CelestialSystemValidationStatus.InvalidMappingScaleNumerator);
        if (timeMapping.ScaleDenominator <= 0) return new(CelestialSystemValidationStatus.InvalidMappingScaleDenominator);
        if (!ephemerisMetadata.Source.IsValid) return new(CelestialSystemValidationStatus.InvalidEphemerisSource);
        if (!ephemerisMetadata.Version.IsValid) return new(CelestialSystemValidationStatus.InvalidEphemerisVersion);
        if (!ephemerisMetadata.CoordinateFrame.IsValid) return new(CelestialSystemValidationStatus.InvalidCoordinateFrame);
        if (!ephemerisMetadata.ConstantsVersion.IsValid) return new(CelestialSystemValidationStatus.InvalidConstantsVersion);
        if (ephemerisMetadata.SupportedStartDomainTicks > ephemerisMetadata.SupportedEndDomainTicks) return new(CelestialSystemValidationStatus.InvalidSupportedInterval);
        if (timeMapping.DomainAnchor.Domain != ephemerisMetadata.Domain) return new(CelestialSystemValidationStatus.MappingMetadataDomainMismatch);
        if (nodes.Length == 0) return new(CelestialSystemValidationStatus.EmptySystem);
        var rootIndex = -1;
        for (var index = 0; index < nodes.Length; index++)
        {
            ref readonly var node = ref nodes[index]; var body = node.Body;
            if (!body.Id.IsValid) return new(CelestialSystemValidationStatus.InvalidBodyId);
            if (body.InertialFrame.Value == 0) return new(CelestialSystemValidationStatus.InvalidInertialFrame);
            if (!double.IsFinite(body.GravitationalParameter) || body.GravitationalParameter <= 0d) return new(CelestialSystemValidationStatus.InvalidGravitationalParameter);
            if (!Enum.IsDefined(node.TrajectoryModel)) return new(CelestialSystemValidationStatus.InvalidTrajectoryModel);
            if (body.PrimaryBody == body.Id) return new(CelestialSystemValidationStatus.SelfParent);
            for (var prior = 0; prior < index; prior++) if (nodes[prior].Id == body.Id) return new(CelestialSystemValidationStatus.DuplicateBodyId);
            if (body.PrimaryBody is null)
            {
                if (rootIndex >= 0) return new(CelestialSystemValidationStatus.MultipleRoots);
                if (node.TrajectoryModel != CelestialTrajectoryModel.FixedBody) return new(CelestialSystemValidationStatus.RootModelInvalid);
                rootIndex = index;
            }
        }
        if (rootIndex < 0) return new(CelestialSystemValidationStatus.MultipleRoots);
        for (var index = 0; index < nodes.Length; index++)
        {
            if (nodes[index].ParentId is { } parent && FindIndex(nodes, parent) < 0) return new(CelestialSystemValidationStatus.MissingParent);
            var slow = index; var fast = index;
            while (true)
            {
                slow = NextParentIndex(nodes, slow); fast = NextParentIndex(nodes, NextParentIndex(nodes, fast));
                if (slow < 0 || fast < 0) break;
                if (slow == fast) return new(CelestialSystemValidationStatus.ParentCycle);
            }
        }
        return new(CelestialSystemValidationStatus.Success, rootIndex);
    }

    private static int FindIndex(ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialBodyId id)
    {
        for (var index = 0; index < nodes.Length; index++) if (nodes[index].Id == id) return index;
        return -1;
    }
    private static int NextParentIndex(ReadOnlySpan<CelestialHierarchyNode> nodes, int index) => index < 0 || nodes[index].ParentId is not { } parent ? -1 : FindIndex(nodes, parent);
}
