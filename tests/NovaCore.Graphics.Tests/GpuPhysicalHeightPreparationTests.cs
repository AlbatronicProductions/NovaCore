using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class GpuPhysicalHeightPreparationTests
{
    private const ulong EarthBodyId = 6;
    private const double EarthRadiusMetres = 6_378_137d;

    public static void Run()
    {
        VerifyAbi();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out var terrainPath, out var terrainError), $"terrain-v5 asset: {terrainError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out var localError), $"local-v2 asset: {localError}");
        var oracleDirectory = Path.Combine(root, "assets", "earth", "runtime");
        Require(EarthElevationDataset.TryLoad(oracleDirectory, out var oracleError), $"CPU elevation oracle: {oracleError}");
        Require(EarthLocalTerrainElevationDataset.TryLoad(localPath, out var localLoadError), $"CPU local-v2 oracle: {localLoadError}");

        var samples = BuildSamples(localPath);
        var queries = new NativePlanetaryHeightQuery[samples.Count];
        var reconstructed = new Double3[samples.Count];
        for (var index = 0; index < samples.Count; index++)
            Require(PlanetaryGpuPhysicalHeightQuery.TryCreate(EarthBodyId, 5, samples[index].Tier,
                samples[index].Anchor, samples[index].Delta, out queries[index], out reconstructed[index]),
                $"query creation {samples[index].Name}");

        var shaderPath = Path.Combine(root, "build", "native-ninja", "shaders", "planetary_height_query.comp.spv");
        var oraclePath = Path.Combine(oracleDirectory, "earth_elevation_8192x4096.r16");
        Require(File.Exists(shaderPath) && File.Exists(oraclePath), "height-query shader and CPU oracle are available");
        var first = Invoke(queries, oraclePath, terrainPath, localPath, shaderPath, out var firstMetrics);
        var second = Invoke(queries, oraclePath, terrainPath, localPath, shaderPath, out var secondMetrics);
        var reversedQueries = queries.Reverse().ToArray();
        var reversed = Invoke(reversedQueries, oraclePath, terrainPath, localPath, shaderPath, out _);

        var terrain = new PlanetaryPhysicalTerrainAuthority(EarthBodyId, PlanetaryTerrainDefinition.EarthProductionCubeV5);
        var maximumTransportError = 0d; var maximumCoordinateError = 0d; var maximumUvError = 0d; var maximumHeightError = 0d;
        var localSamples = 0; var globalOnlySamples = 0;
        for (var index = 0; index < samples.Count; index++)
        {
            var result = first[index]; var repeat = second[index]; var reordered = reversed[^(index + 1)];
            Require(result.Valid == 1 && result.ResultTerrainVersion == 5, $"valid GPU result {samples[index].Name}");
            var gpuPoint = new Double3(result.ReconstructedX, result.ReconstructedY, result.ReconstructedZ);
            maximumTransportError = Math.Max(maximumTransportError,
                Math.Sqrt((reconstructed[index] - (samples[index].Anchor + samples[index].Delta)).LengthSquared));
            maximumCoordinateError = Math.Max(maximumCoordinateError, Math.Sqrt((gpuPoint - reconstructed[index]).LengthSquared));
            var direction = reconstructed[index].Normalized();
            Require(RelaxedCubeSphereProjection.TryAddress(direction, out var face, out var u, out var v), $"CPU address {samples[index].Name}");
            Require(result.GlobalFace == (uint)face, $"canonical face parity {samples[index].Name}: CPU={face}; GPU={result.GlobalFace}");
            maximumUvError = Math.Max(maximumUvError, Math.Max(Math.Abs(result.FaceU - u), Math.Abs(result.FaceV - v)));
            Require(terrain.TrySampleHeight(EarthBodyId, direction, out var cpuHeight), $"CPU physical height {samples[index].Name}");
            maximumHeightError = Math.Max(maximumHeightError, Math.Abs(result.PhysicalHeightMetres - cpuHeight));
            Require(result.LocalAvailable == result.SourceHasLocal, $"local source identity {samples[index].Name}");
            if (result.LocalAvailable == 1) localSamples++; else globalOnlySamples++;
            Require(ResultBitsEqual(result, repeat) && ResultBitsEqual(result, reordered), $"bit-stable repeat/order independence {samples[index].Name}");
        }

        Require(maximumTransportError <= 1e-3d, $"authority-to-split transport remains sub-millimetre: {maximumTransportError:R}");
        Require(maximumCoordinateError == 0d, $"split high/low GPU reconstruction is exact after explicit FP32 local delta: {maximumCoordinateError:R}");
        Require(maximumUvError <= 2e-12d, $"CPU/GPU relaxed-cube UV agreement: {maximumUvError:R}");
        Require(maximumHeightError <= 2e-6d, $"CPU/GPU physical-height agreement: {maximumHeightError:R} m");
        Require(localSamples > 0 && globalOnlySamples > 0, "local-v2 present and absent fallback policies are both exercised");
        Require(firstMetrics.QueryCount == samples.Count && firstMetrics.DispatchGroups == (samples.Count + 63) / 64 &&
            firstMetrics.GlobalRecordCount == 96 && firstMetrics.LocalRecordCount > 0 && firstMetrics.ValidationErrors == 0,
            "bounded dispatch and zero Vulkan validation errors");
        Require(secondMetrics.ValidationErrors == 0, "repeat dispatch remains validation-clean");

        var invalid = queries[0]; invalid.TerrainVersion = 4;
        Require(Invoke([invalid], oraclePath, terrainPath, localPath, shaderPath, out _)[0].Valid == 0,
            "terrain-version mismatch is rejected in shader-visible authority");
        invalid = queries[0]; invalid.SourcePolicy = 2;
        Require(Invoke([invalid], oraclePath, terrainPath, localPath, shaderPath, out _)[0].Valid == 0,
            "physical-source policy mismatch is rejected");
        var collapsed = queries[0]; collapsed.AnchorLowX = collapsed.AnchorLowY = collapsed.AnchorLowZ = 0f;
        var collapsedResult = Invoke([collapsed], oraclePath, terrainPath, localPath, shaderPath, out _)[0];
        var collapsedError = Math.Sqrt((new Double3(collapsedResult.ReconstructedX, collapsedResult.ReconstructedY,
            collapsedResult.ReconstructedZ) - reconstructed[0]).LengthSquared);
        Require(collapsedError > 1e-4d, "single-FP32 Earth-scale coordinate collapse is detectable and outside the split contract");

        Require(PlanetaryPatchTopology.Shared.DeterministicHash == 0x98792D7EBC45FF6Dul &&
            PlanetaryProductionPatchTopology.Shared.DeterministicHash == 0x61C28B0A3B4F21FFul,
            "live 11B-6C topology hashes remain unchanged");
        Console.WriteLine($"GPU physical height: samples={samples.Count}; local={localSamples}; globalOnly={globalOnlySamples}; " +
            $"transportMax={maximumTransportError:E17}m; gpuReconstructionMax={maximumCoordinateError:E17}m; " +
            $"uvMax={maximumUvError:E17}; heightMax={maximumHeightError:E17}m; " +
            $"groups={firstMetrics.DispatchGroups}; gpu={firstMetrics.GpuMilliseconds:F6}ms; boundedCpu={firstMetrics.CpuMilliseconds:F3}ms; validation=0");
    }

    private static List<Sample> BuildSamples(string localPath)
    {
        var samples = new List<Sample>();
        var floridaDirection = BodyFixedGeography.DirectionFromLatitudeLongitude(
            FloridaLaunchSite.Latitude * Math.PI / 180d, FloridaLaunchSite.Longitude * Math.PI / 180d);
        Require(SurfaceAnchor.TryCreate(EarthBodyId, new(2, 5), floridaDirection, 0d, out var florida) == SurfaceAnchorCreationStatus.Success,
            "Florida canonical SurfaceAnchor");
        Require(SurfaceEnuFrame.TryCreate(florida, out var enu), "Florida canonical ENU");
        var floridaPoint = floridaDirection * EarthRadiusMetres;
        samples.Add(new("Florida", floridaPoint, Double3.Zero, 3));
        foreach (var metres in new[] { 1d, 10d, 100d, 1_000d })
        {
            samples.Add(new($"Florida E+{metres}", floridaPoint, enu.East * metres, 3));
            samples.Add(new($"Florida E-{metres}", floridaPoint, enu.East * -metres, 3));
            samples.Add(new($"Florida N+{metres}", floridaPoint, enu.North * metres, 3));
            samples.Add(new($"Florida N-{metres}", floridaPoint, enu.North * -metres, 3));
        }
        samples.Add(Direction("Everest", BodyFixedGeography.DirectionFromLatitudeLongitude(27.9881d * Math.PI / 180d, 86.925d * Math.PI / 180d)));
        samples.Add(Direction("Pacific ocean", BodyFixedGeography.DirectionFromLatitudeLongitude(0d, -140d * Math.PI / 180d)));
        var localPackage = File.ReadAllBytes(localPath);
        Require(PlanetaryLocalTerrainPackContract.TryReadHeader(localPackage, out _), "local-v2 sample header");
        Require(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(
                localPackage.AsSpan(PlanetaryLocalTerrainPackContract.HeaderBytes, PlanetaryLocalTerrainPackContract.RecordHeaderBytes),
                out var localRecord), "local-v2 sample identity");
        var localCells = 1 << localRecord.Sector.Level;
        samples.Add(Direction("local-v2 sector center", RelaxedCubeSphereProjection.UnitDirection(localRecord.Sector.Face,
            (localRecord.Sector.X + .5d) / localCells, (localRecord.Sector.Y + .5d) / localCells)));
        samples.Add(Direction("local-v2 sector boundary inside", RelaxedCubeSphereProjection.UnitDirection(localRecord.Sector.Face,
            (localRecord.Sector.X + 1d - 1e-8d) / localCells, (localRecord.Sector.Y + .5d) / localCells)));
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>()) samples.Add(Direction($"{face} interior", RelaxedCubeSphereProjection.UnitDirection(face, .37d, .61d)));

        const double epsilon = 1e-10d;
        for (var zeroAxis = 0; zeroAxis < 3; zeroAxis++)
            foreach (var firstSign in new[] { -1d, 1d }) foreach (var secondSign in new[] { -1d, 1d })
            {
                var values = new double[3]; var cursor = 0;
                for (var axis = 0; axis < 3; axis++) if (axis != zeroAxis) values[axis] = cursor++ == 0 ? firstSign : secondSign;
                var edge = new Double3(values[0], values[1], values[2]).Normalized();
                samples.Add(Direction($"edge {zeroAxis}/{firstSign}/{secondSign}", edge));
                var perturb = zeroAxis == 0 ? new Double3(epsilon, 0d, 0d) : zeroAxis == 1 ? new Double3(0d, epsilon, 0d) : new Double3(0d, 0d, epsilon);
                samples.Add(Direction($"edge {zeroAxis}/{firstSign}/{secondSign} +eps", (edge + perturb).Normalized()));
                samples.Add(Direction($"edge {zeroAxis}/{firstSign}/{secondSign} -eps", (edge - perturb).Normalized()));
            }
        foreach (var x in new[] { -1d, 1d }) foreach (var y in new[] { -1d, 1d }) foreach (var z in new[] { -1d, 1d })
        {
            var corner = new Double3(x, y, z).Normalized();
            samples.Add(Direction($"corner {x}/{y}/{z}", corner));
            samples.Add(Direction($"corner {x}/{y}/{z} +eps", (corner + new Double3(epsilon, -epsilon, epsilon)).Normalized()));
            samples.Add(Direction($"corner {x}/{y}/{z} -eps", (corner + new Double3(-epsilon, epsilon, -epsilon)).Normalized()));
        }
        return samples;
    }

    private static Sample Direction(string name, in Double3 direction) => new(name, direction * EarthRadiusMetres, Double3.Zero, 2);

    private static NativePlanetaryHeightResult[] Invoke(NativePlanetaryHeightQuery[] queries, string oraclePath,
        string terrainPath, string localPath, string shaderPath, out NativePlanetaryHeightQueryMetrics metrics)
    {
        var results = new NativePlanetaryHeightResult[queries.Length];
        var oracle = Utf8(oraclePath); var terrain = Utf8(terrainPath); var local = Utf8(localPath); var shader = Utf8(shaderPath);
        var metricValue = new NativePlanetaryHeightQueryMetrics { Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(), Version = 1 };
        fixed (NativePlanetaryHeightQuery* queryPointer = queries)
        fixed (NativePlanetaryHeightResult* resultPointer = results)
        fixed (byte* oraclePointer = oracle)
        fixed (byte* terrainPointer = terrain)
        fixed (byte* localPointer = local)
        fixed (byte* shaderPointer = shader)
        {
            var assets = new NativePlanetaryHeightQueryAssets
            {
                Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryAssets>(), Version = 1,
                ElevationOraclePathUtf8 = oraclePointer, ProductionTerrainPathUtf8 = terrainPointer,
                LocalTerrainPathUtf8 = localPointer, ComputeShaderPathUtf8 = shaderPointer,
            };
            Require(NativeRuntime.QueryPlanetaryPhysicalHeights(queryPointer, (uint)queries.Length, resultPointer, &assets, &metricValue) == NativeResult.Success,
                "native Vulkan height query succeeds");
        }
        metrics = metricValue;
        return results;
    }

    private static bool ResultBitsEqual(in NativePlanetaryHeightResult left, in NativePlanetaryHeightResult right)
    {
        var a = left; var b = right;
        return new ReadOnlySpan<byte>(&a, sizeof(NativePlanetaryHeightResult)).SequenceEqual(new ReadOnlySpan<byte>(&b, sizeof(NativePlanetaryHeightResult)));
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + '\0');

    private static void VerifyAbi()
    {
        Require(Marshal.SizeOf<NativePlanetaryHeightQuery>() == 96, "height query ABI size 96");
        Require(Marshal.OffsetOf<NativePlanetaryHeightQuery>(nameof(NativePlanetaryHeightQuery.OracleU)).ToInt32() == 48 &&
            Marshal.OffsetOf<NativePlanetaryHeightQuery>(nameof(NativePlanetaryHeightQuery.BodyIdLow)).ToInt32() == 64 &&
            Marshal.OffsetOf<NativePlanetaryHeightQuery>(nameof(NativePlanetaryHeightQuery.TopologyVersion)).ToInt32() == 80,
            "height query ABI offsets 48/64/80");
        Require(Marshal.SizeOf<NativePlanetaryHeightResult>() == 224 &&
            Marshal.OffsetOf<NativePlanetaryHeightResult>(nameof(NativePlanetaryHeightResult.FaceU)).ToInt32() == 32 &&
            Marshal.OffsetOf<NativePlanetaryHeightResult>(nameof(NativePlanetaryHeightResult.GlobalFace)).ToInt32() == 176 &&
            Marshal.OffsetOf<NativePlanetaryHeightResult>(nameof(NativePlanetaryHeightResult.Valid)).ToInt32() == 208,
            "height result ABI size/offsets 224/32/176/208");
        Require(Marshal.SizeOf<NativePlanetaryHeightQueryAssets>() == 40 && Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>() == 48,
            "height query assets/metrics ABI sizes 40/48");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private readonly record struct Sample(string Name, Double3 Anchor, Double3 Delta, uint Tier);
}
