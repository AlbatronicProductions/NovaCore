using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public sealed record PlanetarySphericalBillboardNaturalTerrainLevelResult(
    PlanetarySphericalBillboardProofLevel Level,
    NativePlanetaryHeightQueryMetrics PhysicalPreparation,
    NativeSphericalBillboardProofMetrics Publication,
    NativeSphericalBillboardProofMetrics Frame,
    NativeSphericalBillboardProofMetrics CameraUpdate,
    double MaximumCpuHeightErrorMetres,
    double MaximumCpuNormalErrorRadians,
    int PreparedCanonicalSamples,
    int ReusedCanonicalSamples);

public sealed record PlanetarySphericalBillboardNaturalTerrainReport(
    IReadOnlyList<PlanetarySphericalBillboardNaturalTerrainLevelResult> Levels,
    int UniqueCanonicalSamples, int ReusedCanonicalSamples,
    double MaximumSharedLevelHeightDeltaMetres,
    double MaximumSharedLevelNormalDeltaRadians,
    double MaximumPhysicalHeightMetres);

public sealed record PlanetaryProductionBillboardPhysicalPreparation(
    NativeSphericalBillboardPhysicalVertex[] Vertices,
    NativePlanetaryHeightQueryMetrics Metrics,
    double MaximumCpuHeightErrorMetres,
    double MaximumCpuNormalErrorRadians,
    int PreparedSamples,
    int ReusedSamples,
    IReadOnlyList<PlanetaryCanonicalPhysicalSampleIdentity> Identities);

public sealed class PlanetaryProductionBillboardPhysicalCache
{
    private readonly Dictionary<PlanetaryCanonicalPhysicalSampleIdentity,
        NativeSphericalBillboardPhysicalVertex> _values = new();

    internal bool TryGet(PlanetaryCanonicalPhysicalSampleIdentity identity,
        out NativeSphericalBillboardPhysicalVertex value) => _values.TryGetValue(identity, out value);

    internal void Store(PlanetaryCanonicalPhysicalSampleIdentity identity,
        in NativeSphericalBillboardPhysicalVertex value) => _values[identity] = value;

    public int Count => _values.Count;

    public void RetainOnly(IEnumerable<PlanetaryCanonicalPhysicalSampleIdentity> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);
        var retained = identities.ToHashSet();
        foreach (var identity in _values.Keys.Where(identity => !retained.Contains(identity)).ToArray())
            _values.Remove(identity);
    }
}

/// <summary>
/// Isolated P2S4 binding. The shared topology-neutral GPU physical query owns
/// H(direction) and its analytic physical normal. Billboard levels consume an
/// immutable canonical publication and never become physical identity.
/// </summary>
public static unsafe class PlanetarySphericalBillboardNaturalTerrainProof
{
    public const uint PhysicalGeneration = PlanetaryPhysicalSurface.NaturalTerrainCandidateGenerationId;
    public const uint TerrainDataGeneration = 5;
    public const double EarthRadiusMetres = 6_378_137d;
    private readonly record struct PreparedSample(double Height, Double3 Normal);

    /// <summary>Prepares the immutable production-v2 directions with the same
    /// topology-neutral GPU H(direction) authority used by P2S4.</summary>
    public static PlanetaryProductionBillboardPhysicalPreparation PrepareProduction(
        string repositoryRoot,
        PlanetaryProductionSphericalBillboardTopology topology,
        in PlanetaryProductionBillboardPupil pupil)
        => PrepareProductionIncremental(repositoryRoot, topology, pupil,
            new PlanetaryProductionBillboardPhysicalCache());

    public static PlanetaryProductionBillboardPhysicalPreparation PrepareProductionIncremental(
        string repositoryRoot,
        PlanetaryProductionSphericalBillboardTopology topology,
        in PlanetaryProductionBillboardPupil pupil,
        PlanetaryProductionBillboardPhysicalCache cache,
        int maximumParitySamples = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(cache);
        if (!pupil.IsValid) throw new ArgumentOutOfRangeException(nameof(pupil));
        ResolveAssets(repositoryRoot, out var oracle, out var terrain, out var local);
        var queryShader = Path.Combine(repositoryRoot, "build", "native-ninja", "shaders",
            "planetary_height_query.comp.spv");
        var resolvedPupil = pupil;
        var directions = topology.Vertices.Select(vertex =>
            resolvedPupil.ResolveCanonicalDirection(vertex, topology)).ToArray();
        var identities = directions.Select(direction => PlanetaryCanonicalPhysicalSampleIdentity.Create(
            direction, PhysicalGeneration, TerrainDataGeneration)).ToArray();
        var missing = identities.Select((identity, index) => (identity, index))
            .Where(value => !cache.TryGet(value.identity, out _)).ToArray();
        var queries = new NativePlanetaryHeightQuery[missing.Length];
        // The shared physical-query ABI has three anchored density tiers plus its
        // global tier. Production topology levels are selection identities, not
        // new physical authorities, so they consume the finest existing physical
        // preparation tier once level 3 is reached.
        var physicalPreparationTier = Math.Min((uint)topology.Level, 3u);
        for (var i = 0; i < missing.Length; i++)
            if (!PlanetaryGpuPhysicalHeightQuery.TryCreate(6, TerrainDataGeneration,
                physicalPreparationTier, directions[missing[i].index] * EarthRadiusMetres, Double3.Zero,
                out queries[i], out _))
                throw new InvalidOperationException("Production billboard canonical query encoding failed.");
        var metrics = Query(queries, oracle, terrain, local, queryShader, out var queried);
        for (var i = 0; i < missing.Length; i++)
        {
            var value = queried[i];
            if (value.Valid != 1 || value.ResultTerrainVersion != TerrainDataGeneration)
                throw new InvalidOperationException("Production billboard GPU physical sample was invalid.");
            var normal = new Double3(value.PhysicalNormalX, value.PhysicalNormalY,
                value.PhysicalNormalZ).Normalized();
            var direction = directions[missing[i].index];
            var body = direction * (EarthRadiusMetres + value.PhysicalHeightMetres);
            var prepared = new NativeSphericalBillboardPhysicalVertex
            {
                BodyX = body.X, BodyY = body.Y, BodyZ = body.Z,
                PhysicalHeightMetres = value.PhysicalHeightMetres,
                NormalX = (float)normal.X, NormalY = (float)normal.Y,
                NormalZ = (float)normal.Z, NormalValidity = 1f
            };
            cache.Store(missing[i].identity, prepared);
        }
        if (maximumParitySamples < 0) throw new ArgumentOutOfRangeException(nameof(maximumParitySamples));
        var vertices = new NativeSphericalBillboardPhysicalVertex[directions.Length];
        for (var i = 0; i < directions.Length; i++)
        {
            if (!cache.TryGet(identities[i], out var prepared))
                throw new InvalidOperationException("Production billboard physical cache publication was incomplete.");
            vertices[i] = prepared;
        }
        var maximumHeightError = 0d; var maximumNormalError = 0d;
        var paritySamples = Math.Min(directions.Length, maximumParitySamples);
        for (var sample = 0; sample < paritySamples; sample++)
        {
            var i = paritySamples == 1 ? 0 :
                (int)((long)sample * (directions.Length - 1) / (paritySamples - 1));
            var prepared = vertices[i];
            var normal = new Double3(prepared.NormalX, prepared.NormalY, prepared.NormalZ).Normalized();
            var cpu = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(
                directions[i], PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            maximumHeightError = Math.Max(maximumHeightError,
                Math.Abs(prepared.PhysicalHeightMetres - cpu.FinalHeightMetres));
            maximumNormalError = Math.Max(maximumNormalError, Math.Acos(Math.Clamp(
                Double3.Dot(normal, cpu.PhysicalNormal), -1d, 1d)));
        }
        return new(vertices, metrics, maximumHeightError, maximumNormalError,
            missing.Length, directions.Length - missing.Length, Array.AsReadOnly(identities));
    }

    public static PlanetarySphericalBillboardNaturalTerrainReport Run(string repositoryRoot)
    {
        var descriptions = PlanetarySphericalBillboardGpuProofLibrary.Load(
            Path.Combine(repositoryRoot, "assets", "planetary-topology"));
        ResolveAssets(repositoryRoot, out var oracle, out var terrain, out var local);
        var shaderDirectory = Path.Combine(repositoryRoot, "build", "native-ninja", "shaders");
        var queryShader = Path.Combine(shaderDirectory, "planetary_height_query.comp.spv");
        var previous = PlanetaryPhysicalSurface.RuntimeGeneration;
        var prepared = new Dictionary<PlanetaryCanonicalPhysicalSampleIdentity, PreparedSample>();
        var reused = 0; var sharedHeightDelta = 0d; var sharedNormalDelta = 0d;
        var maximumPhysicalHeight = 0d;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            using var session = new PlanetarySphericalBillboardGpuProofSession(shaderDirectory);
            var levels = new List<PlanetarySphericalBillboardNaturalTerrainLevelResult>();
            uint frame = 0;
            foreach (var description in descriptions)
            {
                session.Upload(description);
                var directions = description.Topology.Vertices.Select(vertex =>
                    new Double3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z).Normalized()).ToArray();
                var keys = directions.Select(direction => PlanetaryCanonicalPhysicalSampleIdentity.Create(direction,
                    PhysicalGeneration, TerrainDataGeneration)).ToArray();
                var missing = keys.Select((key, index) => (key, index))
                    .Where(value => !prepared.ContainsKey(value.key)).ToArray();
                var queries = new NativePlanetaryHeightQuery[missing.Length];
                for (var i = 0; i < missing.Length; i++)
                    if (!PlanetaryGpuPhysicalHeightQuery.TryCreate(6, TerrainDataGeneration,
                        (uint)description.Level, directions[missing[i].index] * EarthRadiusMetres,
                        Double3.Zero, out queries[i], out _))
                        throw new InvalidOperationException("Canonical billboard query encoding failed.");
                var queryMetrics = Query(queries, oracle, terrain, local, queryShader, out var queried);
                for (var i = 0; i < missing.Length; i++)
                {
                    var value = queried[i];
                    if (value.Valid != 1 || value.ResultTerrainVersion != TerrainDataGeneration)
                        throw new InvalidOperationException("Canonical GPU physical sample was invalid.");
                    prepared.Add(missing[i].key, new(value.PhysicalHeightMetres,
                        new Double3(value.PhysicalNormalX, value.PhysicalNormalY,
                            value.PhysicalNormalZ).Normalized()));
                }
                var levelReuse = directions.Length - missing.Length; reused += levelReuse;
                var publicationVertices = new NativeSphericalBillboardPhysicalVertex[directions.Length];
                var heightError = 0d; var normalError = 0d;
                for (var i = 0; i < directions.Length; i++)
                {
                    var value = prepared[keys[i]];
                    maximumPhysicalHeight = Math.Max(maximumPhysicalHeight, value.Height);
                    var cpu = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(directions[i],
                        PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                    heightError = Math.Max(heightError, Math.Abs(value.Height - cpu.FinalHeightMetres));
                    normalError = Math.Max(normalError, Math.Acos(Math.Clamp(
                        Double3.Dot(value.Normal, cpu.PhysicalNormal), -1d, 1d)));
                    var body = directions[i] * (EarthRadiusMetres + value.Height);
                    publicationVertices[i] = new() { BodyX = body.X, BodyY = body.Y, BodyZ = body.Z,
                        PhysicalHeightMetres = value.Height, NormalX = (float)value.Normal.X,
                        NormalY = (float)value.Normal.Y, NormalZ = (float)value.Normal.Z,
                        NormalValidity = 1f };
                }
                if (description.Level != PlanetarySphericalBillboardProofLevel.Orbital)
                    foreach (var previousVertex in descriptions[(int)description.Level - 1].Topology.Vertices)
                    {
                        var direction = new Double3(previousVertex.Position.X, previousVertex.Position.Y,
                            previousVertex.Position.Z).Normalized();
                        var key = PlanetaryCanonicalPhysicalSampleIdentity.Create(direction,
                            PhysicalGeneration, TerrainDataGeneration);
                        var a = prepared[key]; var b = prepared[key];
                        sharedHeightDelta = Math.Max(sharedHeightDelta, Math.Abs(a.Height - b.Height));
                        sharedNormalDelta = Math.Max(sharedNormalDelta, Math.Acos(Math.Clamp(
                            Double3.Dot(a.Normal, b.Normal), -1d, 1d)));
                    }
                var publication = session.PublishPhysicalSurface(description, publicationVertices,
                    PhysicalGeneration, TerrainDataGeneration);
                var repeated = session.PublishPhysicalSurface(description, publicationVertices,
                    PhysicalGeneration, TerrainDataGeneration);
                if (repeated.PhysicalPreparationDispatchCount != publication.PhysicalPreparationDispatchCount ||
                    repeated.PhysicalReuseCount < publication.PhysicalReuseCount + publicationVertices.Length)
                    throw new InvalidOperationException("Unchanged canonical physical publication was not reused.");
                if (session.TryRunWithStalePhysicalGeneration(description, PhysicalGeneration,
                    TerrainDataGeneration) != NativeResult.InvalidArgument)
                    throw new InvalidOperationException("Stale physical generation was not rejected.");
                var first = session.RunFrame(description, frame++, physicalGeneration: PhysicalGeneration,
                    terrainDataGeneration: TerrainDataGeneration, bodyRadiusMetres: EarthRadiusMetres);
                var camera = session.RunFrame(description, frame++, cameraDistanceRadii: 2.6,
                    physicalGeneration: PhysicalGeneration, terrainDataGeneration: TerrainDataGeneration,
                    bodyRadiusMetres: EarthRadiusMetres);
                levels.Add(new(description.Level, queryMetrics, publication, first, camera,
                    heightError, normalError, missing.Length, levelReuse));
            }
            return new(levels, prepared.Count, reused, sharedHeightDelta, sharedNormalDelta,
                maximumPhysicalHeight);
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous); }
    }

    private static NativePlanetaryHeightQueryMetrics Query(NativePlanetaryHeightQuery[] queries,
        string oracle, string terrain, string local, string shader,
        out NativePlanetaryHeightResult[] results)
    {
        results = new NativePlanetaryHeightResult[queries.Length];
        if (queries.Length == 0)
            return new() { Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(), Version = 1 };
        var pointers = new[] { oracle, terrain, local, shader }
            .Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        try
        {
            var metrics = new NativePlanetaryHeightQueryMetrics
                { Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(), Version = 1 };
            fixed (NativePlanetaryHeightQuery* query = queries)
            fixed (NativePlanetaryHeightResult* result = results)
            {
                var assets = new NativePlanetaryHeightQueryAssets
                {
                    Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryAssets>(), Version = 1,
                    ElevationOraclePathUtf8 = (byte*)pointers[0],
                    ProductionTerrainPathUtf8 = (byte*)pointers[1],
                    LocalTerrainPathUtf8 = (byte*)pointers[2],
                    ComputeShaderPathUtf8 = (byte*)pointers[3],
                };
                if (NativeRuntime.QueryPlanetaryPhysicalHeights(query, (uint)queries.Length,
                    result, &assets, &metrics) != NativeResult.Success)
                    throw new InvalidOperationException("P2S4 topology-neutral GPU physical preparation failed.");
            }
            return metrics;
        }
        finally { foreach (var pointer in pointers) Marshal.FreeCoTaskMem(pointer); }
    }

    private static void ResolveAssets(string root, out string oracle, out string terrain, out string local)
    {
        if (!TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out terrain, out var terrainError)) throw new InvalidOperationException(terrainError);
        if (!TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out local, out var localError)) throw new InvalidOperationException(localError);
        oracle = Path.Combine(root, "assets", "earth", "runtime", "earth_elevation_8192x4096.r16");
        if (!EarthElevationDataset.TryLoad(Path.GetDirectoryName(oracle)!, out var oracleError))
            throw new InvalidOperationException(oracleError);
        if (!EarthLocalTerrainElevationDataset.TryLoad(local, out var localLoadError))
            throw new InvalidOperationException(localLoadError);
    }
}
