using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable validated authored hierarchy and its typed ephemeris catalogs.</summary>
internal sealed class CelestialSystemDefinition
{
    private readonly CelestialHierarchyNode[] _nodes;
    private readonly CelestialBodyCatalog _bodyCatalog;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;
    private readonly int[] _traversalIndices;
    private readonly CelestialEphemerisSource[] _sources;
    private readonly ulong[] _sourceLookupIds;
    private readonly int[] _sourceLookupIndices;
    private readonly FixedBodyEphemerisPayload[] _fixedBodies;
    private readonly CircularOrbitEphemerisPayload[] _circularOrbits;
    private readonly TwoBodyTrajectory[] _analyticalKepler;
    private readonly AnalyticalKeplerSecularCorrection[] _analyticalCorrections;
    private readonly AnalyticalKeplerPeriodicCorrection[] _analyticalPeriodicCorrections;
    private readonly SampledEphemerisPayload[] _sampledEphemerides;
    private readonly CelestialEphemerisSample[] _samples;

    private CelestialSystemDefinition(CelestialSystemId id, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, CelestialBodyCatalog bodyCatalog, CelestialHierarchyNode[] nodes, ulong[] lookupIds, int[] lookupIndices, int[] traversal, CelestialBodyId root, CelestialEphemerisSource[] sources, ulong[] sourceIds, int[] sourceIndices, FixedBodyEphemerisPayload[] fixedBodies, CircularOrbitEphemerisPayload[] circularOrbits, TwoBodyTrajectory[] analyticalKepler, AnalyticalKeplerSecularCorrection[] analyticalCorrections, AnalyticalKeplerPeriodicCorrection[] analyticalPeriodicCorrections, SampledEphemerisPayload[] sampledEphemerides, CelestialEphemerisSample[] samples)
    { Id = id; TimeMapping = mapping; EphemerisMetadata = metadata; _bodyCatalog = bodyCatalog; _nodes = nodes; _lookupIds = lookupIds; _lookupIndices = lookupIndices; _traversalIndices = traversal; RootBody = root; _sources = sources; _sourceLookupIds = sourceIds; _sourceLookupIndices = sourceIndices; _fixedBodies = fixedBodies; _circularOrbits = circularOrbits; _analyticalKepler = analyticalKepler; _analyticalCorrections = analyticalCorrections; _analyticalPeriodicCorrections = analyticalPeriodicCorrections; _sampledEphemerides = sampledEphemerides; _samples = samples; }

    public CelestialSystemId Id { get; }
    public CelestialSystemTimeMapping TimeMapping { get; }
    public CelestialEphemerisMetadata EphemerisMetadata { get; }
    public CelestialBodyId RootBody { get; }
    public int Count => _nodes.Length;
    public int BodyCount => _bodyCatalog.Count;

    public static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialBodyCatalogEntry> bodies, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
        => TryCreateCore(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, [], [], [], [], out definition, out validation);

    internal static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialBodyCatalogEntry> bodies, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<AnalyticalKeplerSecularCorrection> analyticalCorrections, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
        => TryCreateCore(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, analyticalCorrections, [], [], [], out definition, out validation);

    internal static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialBodyCatalogEntry> bodies, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<AnalyticalKeplerSecularCorrection> analyticalCorrections, ReadOnlySpan<AnalyticalKeplerPeriodicCorrection> analyticalPeriodicCorrections, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
        => TryCreateCore(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, analyticalCorrections, analyticalPeriodicCorrections, [], [], out definition, out validation);

    // Compatibility overload for pre-catalog focused tests. New authored systems must supply one immutable body catalog.
    internal static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
    {
        var bodies = BuildCompatibilityCatalog(nodes, out validation); if (bodies is null) { definition = null; return false; }
        Span<CelestialEphemerisSource> sources = stackalloc CelestialEphemerisSource[5]; var sourceCount = 0; var fixedNeeded = false; var circularNeeded = false; var keplerNeeded = false; var reservedNeeded = false;
        for (var i = 0; i < nodes.Length; i++) { switch (nodes[i].TrajectoryModel) { case CelestialTrajectoryModel.FixedBody: fixedNeeded = true; break; case CelestialTrajectoryModel.CircularOrbit: circularNeeded = true; break; case CelestialTrajectoryModel.AnalyticalKepler: keplerNeeded = true; break; case CelestialTrajectoryModel.ReservedNumericalNBody: reservedNeeded = true; break; } }
        if (fixedNeeded) sources[sourceCount++] = new(new(2), CelestialTrajectoryModel.FixedBody, metadata with { Source = new(2) }); if (circularNeeded) sources[sourceCount++] = new(new(3), CelestialTrajectoryModel.CircularOrbit, metadata with { Source = new(3) }); if (keplerNeeded) sources[sourceCount++] = new(new(1), CelestialTrajectoryModel.AnalyticalKepler, metadata with { Source = new(1) }); if (reservedNeeded) sources[sourceCount++] = new(new(5), CelestialTrajectoryModel.ReservedNumericalNBody, metadata with { Source = new(5) });
        var firstParent = nodes.Length > 1 && nodes[1].ParentId is { } parent ? parent : new CelestialBodyId(1); var mu = 1d; for (var i = 0; i < bodies.Length; i++) if (bodies[i].Id == firstParent) { mu = bodies[i].PhysicalProperties.GravitationalParameter; break; } if (!double.IsFinite(mu) || mu <= 0d) mu = 1d;
        Span<FixedBodyEphemerisPayload> fixedBodies = fixedNeeded ? stackalloc FixedBodyEphemerisPayload[1] : []; if (fixedNeeded) fixedBodies[0] = FixedBodyEphemerisPayload.Identity;
        Span<CircularOrbitEphemerisPayload> circular = circularNeeded ? stackalloc CircularOrbitEphemerisPayload[1] : []; if (circularNeeded) circular[0] = new(0, 1d, 0d, DoubleQuaternion.Identity, mu);
        Span<TwoBodyTrajectory> kepler = keplerNeeded ? stackalloc TwoBodyTrajectory[1] : []; if (keplerNeeded) kepler[0] = new(firstParent, Simulation.Time.SimulationInstant.Zero, new CartesianState(new Double3(1d, 0d, 0d), new Double3(0d, Math.Sqrt(mu), 0d)), TwoBodyPropagationModel.CartesianTwoBodyV1);
        return TryCreate(id, bodies, nodes, mapping, metadata, sources[..sourceCount], fixedBodies, circular, kepler, out definition, out validation);
    }

    internal static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
    { var bodies = BuildCompatibilityCatalog(nodes, out validation); if (bodies is null) { definition = null; return false; } return TryCreate(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, out definition, out validation); }

    internal static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<SampledEphemerisPayload> sampledEphemerides, ReadOnlySpan<CelestialEphemerisSample> samples, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
    { var bodies = BuildCompatibilityCatalog(nodes, out validation); if (bodies is null) { definition = null; return false; } return TryCreate(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, sampledEphemerides, samples, out definition, out validation); }

    public static bool TryCreate(CelestialSystemId id, ReadOnlySpan<CelestialBodyCatalogEntry> bodies, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<SampledEphemerisPayload> sampledEphemerides, ReadOnlySpan<CelestialEphemerisSample> samples, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
        => TryCreateCore(id, bodies, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, [], [], sampledEphemerides, samples, out definition, out validation);

    private static bool TryCreateCore(CelestialSystemId id, ReadOnlySpan<CelestialBodyCatalogEntry> bodies, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<AnalyticalKeplerSecularCorrection> analyticalCorrections, ReadOnlySpan<AnalyticalKeplerPeriodicCorrection> analyticalPeriodicCorrections, ReadOnlySpan<SampledEphemerisPayload> sampledEphemerides, ReadOnlySpan<CelestialEphemerisSample> samples, out CelestialSystemDefinition? definition, out CelestialSystemValidationResult validation)
    {
        definition = null;
        if (!CelestialBodyCatalog.TryCreate(bodies, out var catalog, out validation)) return false;
        validation = CelestialSystemValidator.Validate(id, catalog!, nodes, mapping, metadata, sources, fixedBodies, circularOrbits, analyticalKepler, analyticalCorrections, analyticalPeriodicCorrections, sampledEphemerides, samples);
        if (!validation.Succeeded) return false;
        if (nodes.Length > int.MaxValue / 2 || sources.Length > int.MaxValue / 2) { validation = new(CelestialSystemValidationStatus.CapacityOverflow); return false; }
        var copy = nodes.ToArray(); var sourceCopy = sources.ToArray();
        var lookupIds = new ulong[copy.Length]; var lookupIndices = new int[copy.Length];
        var sourceIds = new ulong[sourceCopy.Length]; var sourceIndices = new int[sourceCopy.Length];
        for (var i = 0; i < copy.Length; i++) { lookupIds[i] = copy[i].Id.Value; lookupIndices[i] = i; }
        for (var i = 0; i < sourceCopy.Length; i++) { sourceIds[i] = sourceCopy[i].Id.Value; sourceIndices[i] = i; }
        Array.Sort(lookupIds, lookupIndices); Array.Sort(sourceIds, sourceIndices);
        var correctionCopy = analyticalCorrections.IsEmpty ? new AnalyticalKeplerSecularCorrection[analyticalKepler.Length] : analyticalCorrections.ToArray();
        var periodicCopy = new AnalyticalKeplerPeriodicCorrection[analyticalKepler.Length];
        for (var index = 0; index < periodicCopy.Length; index++) periodicCopy[index] = analyticalPeriodicCorrections.IsEmpty ? AnalyticalKeplerPeriodicCorrection.Identity : analyticalPeriodicCorrections[index];
        definition = new(id, mapping, metadata, catalog!, copy, lookupIds, lookupIndices, BuildTraversal(catalog!, copy, validation.RootIndex), copy[validation.RootIndex].Id, sourceCopy, sourceIds, sourceIndices, fixedBodies.ToArray(), circularOrbits.ToArray(), analyticalKepler.ToArray(), correctionCopy, periodicCopy, sampledEphemerides.ToArray(), samples.ToArray());
        return true;
    }


    public CelestialHierarchyNode GetNode(int index) => _nodes[index];
    public CelestialHierarchyNode GetNodeInTraversalOrder(int index) => _nodes[_traversalIndices[index]];
    public bool TryGetNode(CelestialBodyId id, out CelestialHierarchyNode node) { var i = Array.BinarySearch(_lookupIds, id.Value); if (i >= 0) { node = _nodes[_lookupIndices[i]]; return true; } node = default; return false; }
    internal bool TryGetBody(CelestialBodyId id, out CelestialBodyCatalogEntry body) => _bodyCatalog.TryGet(id, out body);
    internal bool TryGetPhysicalProperties(CelestialBodyId id, out CelestialPhysicalProperties properties) => _bodyCatalog.TryGetPhysicalProperties(id, out properties);
    internal CelestialBodyCatalogEntry GetBody(int index) => _bodyCatalog.GetEntry(index);
    internal bool TryGetSource(CelestialEphemerisSourceId id, out CelestialEphemerisSource source) { var i = Array.BinarySearch(_sourceLookupIds, id.Value); if (i >= 0) { source = _sources[_sourceLookupIndices[i]]; return true; } source = default; return false; }
    internal bool TryGetFixedBody(int index, out FixedBodyEphemerisPayload payload) => TryGet(_fixedBodies, index, out payload);
    internal bool TryGetCircularOrbit(int index, out CircularOrbitEphemerisPayload payload) => TryGet(_circularOrbits, index, out payload);
    internal bool TryGetAnalyticalKepler(int index, out TwoBodyTrajectory payload) => TryGet(_analyticalKepler, index, out payload);
    internal bool TryGetAnalyticalCorrection(int index, out AnalyticalKeplerSecularCorrection correction) => TryGet(_analyticalCorrections, index, out correction);
    internal bool TryGetAnalyticalPeriodicCorrection(int index, out AnalyticalKeplerPeriodicCorrection correction) { if ((uint)index < (uint)_analyticalPeriodicCorrections.Length) { correction = _analyticalPeriodicCorrections[index]; return true; } correction = AnalyticalKeplerPeriodicCorrection.Identity; return false; }
    internal bool TryGetSampledEphemeris(int index, out SampledEphemerisPayload payload) => TryGet(_sampledEphemerides, index, out payload);
    internal ReadOnlySpan<CelestialEphemerisSample> Samples => _samples;
    internal int SourceCount => _sources.Length;
    internal int FixedBodyCount => _fixedBodies.Length;
    internal int CircularOrbitCount => _circularOrbits.Length;
    internal int AnalyticalKeplerCount => _analyticalKepler.Length;
    internal int SampledEphemerisCount => _sampledEphemerides.Length;
    internal int SampleCount => _samples.Length;
    internal CelestialEphemerisSource GetSource(int index) => _sources[index];
    internal FixedBodyEphemerisPayload GetFixedBody(int index) => _fixedBodies[index];
    internal CircularOrbitEphemerisPayload GetCircularOrbit(int index) => _circularOrbits[index];
    internal TwoBodyTrajectory GetAnalyticalKepler(int index) => _analyticalKepler[index];
    internal AnalyticalKeplerSecularCorrection GetAnalyticalCorrection(int index) => _analyticalCorrections[index];
    internal AnalyticalKeplerPeriodicCorrection GetAnalyticalPeriodicCorrection(int index) => _analyticalPeriodicCorrections[index];
    internal SampledEphemerisPayload GetSampledEphemeris(int index) => _sampledEphemerides[index];
    internal CelestialEphemerisSample GetSample(int index) => _samples[index];
    public CelestialSystemTimeMappingStatus TryMapTime(Simulation.Time.SimulationInstant instant, out CelestialTimeArgument argument) { var status = TimeMapping.TryMap(instant, out argument); return status != CelestialSystemTimeMappingStatus.Success ? status : EphemerisMetadata.Contains(argument.WholeDomainTicks) ? CelestialSystemTimeMappingStatus.Success : CelestialSystemTimeMappingStatus.OutsideSupportedInterval; }

    private static bool TryGet<T>(T[] catalog, int index, out T payload) where T : struct { if ((uint)index < (uint)catalog.Length) { payload = catalog[index]; return true; } payload = default; return false; }
    private static CelestialBodyCatalogEntry[]? BuildCompatibilityCatalog(ReadOnlySpan<CelestialHierarchyNode> nodes, out CelestialSystemValidationResult validation) { var bodies = new CelestialBodyCatalogEntry[nodes.Length]; for (var i = 0; i < nodes.Length; i++) { if (nodes[i].LegacyDefinition is not { } legacy) { validation = new(CelestialSystemValidationStatus.MissingCatalogBody); return null; } bodies[i] = new(new(legacy.Id, $"Legacy-{legacy.Id.Value}", CelestialBodyClassification.Other, legacy.PrimaryBody, default, default, default), new(legacy.GravitationalParameter, 0d, 0d, 0d, 0d, default, default, default)); } validation = new(CelestialSystemValidationStatus.Success); return bodies; }
    private static int[] BuildTraversal(CelestialBodyCatalog catalog, ReadOnlySpan<CelestialHierarchyNode> nodes, int root) { var result = new int[nodes.Length]; result[0] = root; var written = 1; while (written < result.Length) { var selected = -1; for (var candidate = 0; candidate < nodes.Length; candidate++) { if (Contains(result, written, candidate) || !catalog.TryGet(nodes[candidate].Id, out var body) || body.Identity.ParentBody is not { } parent || !ContainsBody(nodes, result, written, parent)) continue; if (selected < 0 || nodes[candidate].Id.Value < nodes[selected].Id.Value) selected = candidate; } result[written++] = selected; } return result; }
    private static bool Contains(ReadOnlySpan<int> values, int count, int value) { for (var i = 0; i < count; i++) if (values[i] == value) return true; return false; }
    private static bool ContainsBody(ReadOnlySpan<CelestialHierarchyNode> nodes, ReadOnlySpan<int> values, int count, CelestialBodyId id) { for (var i = 0; i < count; i++) if (nodes[values[i]].Id == id) return true; return false; }
}

/// <summary>Pure catalog and topology validator. It retains no caller data.</summary>
internal static class CelestialSystemValidator
{
    internal static CelestialSystemValidationResult Validate(CelestialSystemId id, CelestialBodyCatalog catalog, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata, ReadOnlySpan<CelestialEphemerisSource> sources, ReadOnlySpan<FixedBodyEphemerisPayload> fixedBodies, ReadOnlySpan<CircularOrbitEphemerisPayload> circularOrbits, ReadOnlySpan<TwoBodyTrajectory> analyticalKepler, ReadOnlySpan<AnalyticalKeplerSecularCorrection> analyticalCorrections, ReadOnlySpan<AnalyticalKeplerPeriodicCorrection> analyticalPeriodicCorrections, ReadOnlySpan<SampledEphemerisPayload> sampledEphemerides, ReadOnlySpan<CelestialEphemerisSample> samples)
    {
        var common = ValidateCommon(id, catalog, nodes, mapping, metadata); if (!common.Succeeded) return common;
        if (sources.Length == 0) return new(CelestialSystemValidationStatus.MissingEphemerisSource);
        for (var i = 0; i < sources.Length; i++) { ref readonly var source = ref sources[i]; if (!source.Id.IsValid || source.Metadata.Source != source.Id) return new(CelestialSystemValidationStatus.InvalidEphemerisSource); if (!Enum.IsDefined(source.Model)) return new(CelestialSystemValidationStatus.InvalidTrajectoryModel); if (source.Metadata.Domain != mapping.DomainAnchor.Domain) return new(CelestialSystemValidationStatus.SourceSystemTimeDomainMismatch); if (!ValidMetadata(source.Metadata)) return new(CelestialSystemValidationStatus.InvalidEphemerisSource); for (var prior = 0; prior < i; prior++) if (sources[prior].Id == source.Id) return new(CelestialSystemValidationStatus.DuplicateEphemerisSourceId); }
        for (var i = 0; i < fixedBodies.Length; i++) if (!fixedBodies[i].IsCanonical) return new(CelestialSystemValidationStatus.InvalidFixedBodyPayload);
        for (var i = 0; i < circularOrbits.Length; i++) if (!circularOrbits[i].IsValid) return new(CelestialSystemValidationStatus.InvalidCircularOrbitPayload);
        for (var i = 0; i < analyticalKepler.Length; i++) if (!ValidKepler(analyticalKepler[i])) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload);
        if (!analyticalCorrections.IsEmpty && analyticalCorrections.Length != analyticalKepler.Length) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload);
        for (var i = 0; i < analyticalCorrections.Length; i++) if (!analyticalCorrections[i].IsValid) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload);
        if (!analyticalPeriodicCorrections.IsEmpty && analyticalPeriodicCorrections.Length != analyticalKepler.Length) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload);
        for (var i = 0; i < analyticalPeriodicCorrections.Length; i++) if (analyticalPeriodicCorrections[i] is null || !analyticalPeriodicCorrections[i].IsValid) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload);
        for (var i = 0; i < sampledEphemerides.Length; i++)
        {
            ref readonly var payload = ref sampledEphemerides[i];
            if (!payload.Domain.IsValid) return new(CelestialSystemValidationStatus.InvalidSampledTimeDomain);
            if (payload.Domain != mapping.DomainAnchor.Domain) return new(CelestialSystemValidationStatus.SampledPayloadTimeDomainMismatch);
            if (payload.FirstSampleIndex < 0) return new(CelestialSystemValidationStatus.NegativeSampleFirstIndex);
            if (payload.SampleCount < 2) return new(CelestialSystemValidationStatus.InvalidSampleCount);
            if (payload.InterpolationModel != SampledEphemerisInterpolationModel.CubicHermitePositionVelocityV1) return new(CelestialSystemValidationStatus.UnsupportedSampledInterpolationModel);
            int end; try { end = checked(payload.FirstSampleIndex + payload.SampleCount); } catch (OverflowException) { return new(CelestialSystemValidationStatus.SampleRangeOverflow); }
            if (end > samples.Length) return new(CelestialSystemValidationStatus.SampleRangeOutsideStorage);
            if (payload.SupportedStartDomainTick != samples[payload.FirstSampleIndex].DomainTick || payload.SupportedEndDomainTick != samples[end - 1].DomainTick || payload.SupportedStartDomainTick > payload.SupportedEndDomainTick) return new(CelestialSystemValidationStatus.SampleCoverageMismatch);
            for (var sample = payload.FirstSampleIndex; sample < end; sample++) { if (!samples[sample].IsFinite) return new(CelestialSystemValidationStatus.NonFiniteEphemerisSample); if (sample > payload.FirstSampleIndex && samples[sample - 1].DomainTick >= samples[sample].DomainTick) return new(samples[sample - 1].DomainTick == samples[sample].DomainTick ? CelestialSystemValidationStatus.DuplicateSampleTimestamp : CelestialSystemValidationStatus.NonMonotonicSampleTimestamp); }
            for (var prior = 0; prior < i; prior++) { var priorPayload = sampledEphemerides[prior]; if (payload.FirstSampleIndex < priorPayload.FirstSampleIndex + priorPayload.SampleCount && priorPayload.FirstSampleIndex < end) return new(CelestialSystemValidationStatus.OverlappingSampleRanges); }
        }
        for (var sample = 0; sample < samples.Length; sample++) { var used = false; for (var payload = 0; payload < sampledEphemerides.Length; payload++) if (sample >= sampledEphemerides[payload].FirstSampleIndex && sample < sampledEphemerides[payload].FirstSampleIndex + sampledEphemerides[payload].SampleCount) { used = true; break; } if (!used) return new(CelestialSystemValidationStatus.UnusedEphemerisPayload); }
        for (var i = 0; i < nodes.Length; i++)
        {
            ref readonly var node = ref nodes[i]; var binding = node.Ephemeris;
            if (!catalog.TryGet(node.Id, out var catalogBody)) return new(CelestialSystemValidationStatus.MissingCatalogBody);
            if (binding.IsDefault) return new(CelestialSystemValidationStatus.InvalidEphemerisBinding);
            if (binding.PayloadIndex < 0) return new(CelestialSystemValidationStatus.NegativePayloadIndex);
            if (!TryFindSource(sources, binding.SourceId, out var source)) return new(CelestialSystemValidationStatus.MissingEphemerisSource);
            if (source.Model != binding.Model) return new(CelestialSystemValidationStatus.SourceModelIncompatible);
            if (binding.Model == CelestialTrajectoryModel.ReservedNumericalNBody) return new(CelestialSystemValidationStatus.UnsupportedReservedTrajectoryModel);
            if (catalogBody.Identity.ParentBody is null && binding.Model != CelestialTrajectoryModel.FixedBody) return new(CelestialSystemValidationStatus.RootModelInvalid);
            switch (binding.Model)
            {
                case CelestialTrajectoryModel.FixedBody: if ((uint)binding.PayloadIndex >= (uint)fixedBodies.Length) return new(CelestialSystemValidationStatus.PayloadIndexOutOfRange); break;
                case CelestialTrajectoryModel.CircularOrbit: if ((uint)binding.PayloadIndex >= (uint)circularOrbits.Length) return new(CelestialSystemValidationStatus.PayloadIndexOutOfRange); if (catalogBody.Identity.ParentBody is { } parent && (!catalog.TryGetPhysicalProperties(parent, out var parentProperties) || !SameMu(circularOrbits[binding.PayloadIndex].CentralGravitationalParameter, parentProperties.GravitationalParameter))) return new(CelestialSystemValidationStatus.InvalidCircularOrbitPayload); break;
                case CelestialTrajectoryModel.AnalyticalKepler: if ((uint)binding.PayloadIndex >= (uint)analyticalKepler.Length) return new(CelestialSystemValidationStatus.PayloadIndexOutOfRange); if (catalogBody.Identity.ParentBody is { } central && analyticalKepler[binding.PayloadIndex].CentralBody != central) return new(CelestialSystemValidationStatus.InvalidAnalyticalKeplerPayload); break;
                case CelestialTrajectoryModel.SampledEphemeris: if ((uint)binding.PayloadIndex >= (uint)sampledEphemerides.Length) return new(CelestialSystemValidationStatus.PayloadIndexOutOfRange); if (source.Metadata.Domain != sampledEphemerides[binding.PayloadIndex].Domain) return new(CelestialSystemValidationStatus.SampledPayloadTimeDomainMismatch); break;
                default: return new(CelestialSystemValidationStatus.ModelCatalogMismatch);
            }
        }
        if (HasUnused(nodes, CelestialTrajectoryModel.FixedBody, fixedBodies.Length) || HasUnused(nodes, CelestialTrajectoryModel.CircularOrbit, circularOrbits.Length) || HasUnused(nodes, CelestialTrajectoryModel.AnalyticalKepler, analyticalKepler.Length) || HasUnused(nodes, CelestialTrajectoryModel.SampledEphemeris, sampledEphemerides.Length)) return new(CelestialSystemValidationStatus.UnusedEphemerisPayload);
        return common;
    }

    private static CelestialSystemValidationResult ValidateCommon(CelestialSystemId id, CelestialBodyCatalog catalog, ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialSystemTimeMapping mapping, CelestialEphemerisMetadata metadata)
    {
        if (!id.IsValid) return new(CelestialSystemValidationStatus.InvalidSystemId); if (!mapping.DomainAnchor.Domain.IsValid || !metadata.Domain.IsValid) return new(CelestialSystemValidationStatus.InvalidTimeDomain); if (mapping.DomainAnchor.DomainTicksPerSecond <= 0) return new(CelestialSystemValidationStatus.InvalidDomainTickRate); if (mapping.ScaleNumerator <= 0) return new(CelestialSystemValidationStatus.InvalidMappingScaleNumerator); if (mapping.ScaleDenominator <= 0) return new(CelestialSystemValidationStatus.InvalidMappingScaleDenominator); if (!metadata.Source.IsValid) return new(CelestialSystemValidationStatus.InvalidEphemerisSource); if (!metadata.Version.IsValid) return new(CelestialSystemValidationStatus.InvalidEphemerisVersion); if (!metadata.CoordinateFrame.IsValid) return new(CelestialSystemValidationStatus.InvalidCoordinateFrame); if (!metadata.ConstantsVersion.IsValid) return new(CelestialSystemValidationStatus.InvalidConstantsVersion); if (metadata.SupportedStartDomainTicks > metadata.SupportedEndDomainTicks) return new(CelestialSystemValidationStatus.InvalidSupportedInterval); if (mapping.DomainAnchor.Domain != metadata.Domain) return new(CelestialSystemValidationStatus.MappingMetadataDomainMismatch); if (nodes.Length == 0) return new(CelestialSystemValidationStatus.EmptySystem);
        var root = -1;
        if (catalog.Count != nodes.Length) return new(CelestialSystemValidationStatus.MissingCatalogBody);
        for (var i = 0; i < nodes.Length; i++) { if (!catalog.TryGet(nodes[i].Id, out var body)) return new(CelestialSystemValidationStatus.MissingCatalogBody); if (!body.Id.IsValid) return new(CelestialSystemValidationStatus.InvalidBodyId); if (!Enum.IsDefined(nodes[i].TrajectoryModel)) return new(CelestialSystemValidationStatus.InvalidTrajectoryModel); for (var p = 0; p < i; p++) if (nodes[p].Id == body.Id) return new(CelestialSystemValidationStatus.DuplicateBodyId); if (body.Identity.ParentBody is null) { if (root >= 0) return new(CelestialSystemValidationStatus.MultipleRoots); root = i; } }
        if (root < 0) return new(CelestialSystemValidationStatus.MultipleRoots);
        for (var i = 0; i < nodes.Length; i++) { catalog.TryGet(nodes[i].Id, out var current); if (current.Identity.ParentBody is { } parent && FindIndex(nodes, parent) < 0) return new(CelestialSystemValidationStatus.MissingParent); var slow = i; var fast = i; while (true) { slow = NextParentIndex(catalog, nodes, slow); fast = NextParentIndex(catalog, nodes, NextParentIndex(catalog, nodes, fast)); if (slow < 0 || fast < 0) break; if (slow == fast) return new(CelestialSystemValidationStatus.ParentCycle); } }
        return new(CelestialSystemValidationStatus.Success, root);
    }
    private static bool ValidMetadata(CelestialEphemerisMetadata value) => value.Source.IsValid && value.Version.IsValid && value.Domain.IsValid && value.CoordinateFrame.IsValid && value.ConstantsVersion.IsValid && value.SupportedStartDomainTicks <= value.SupportedEndDomainTicks;
    private static bool ValidKepler(TwoBodyTrajectory value) => value.CentralBody.IsValid && value.StateAtEpoch.IsFinite && value.Model == TwoBodyPropagationModel.CartesianTwoBodyV1;
    private static bool TryFindSource(ReadOnlySpan<CelestialEphemerisSource> sources, CelestialEphemerisSourceId id, out CelestialEphemerisSource source) { for (var i = 0; i < sources.Length; i++) if (sources[i].Id == id) { source = sources[i]; return true; } source = default; return false; }
    private static bool SameMu(double left, double right) => Math.Abs(left - right) <= Math.Max(Math.Abs(left), Math.Abs(right)) * 1e-14d;
    private static bool HasUnused(ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialTrajectoryModel model, int count) { for (var payload = 0; payload < count; payload++) { var found = false; for (var node = 0; node < nodes.Length; node++) if (nodes[node].Ephemeris.Model == model && nodes[node].Ephemeris.PayloadIndex == payload) { found = true; break; } if (!found) return true; } return false; }
    private static int FindIndex(ReadOnlySpan<CelestialHierarchyNode> nodes, CelestialBodyId id) { for (var i = 0; i < nodes.Length; i++) if (nodes[i].Id == id) return i; return -1; }
    private static int NextParentIndex(CelestialBodyCatalog catalog, ReadOnlySpan<CelestialHierarchyNode> nodes, int index) { if (index < 0 || !catalog.TryGet(nodes[index].Id, out var body) || body.Identity.ParentBody is not { } parent) return -1; return FindIndex(nodes, parent); }
}
