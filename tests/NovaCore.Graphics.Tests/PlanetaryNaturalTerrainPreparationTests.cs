using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class PlanetaryNaturalTerrainPreparationTests
{
    private const uint Seed = 0x4D12D2B1u;
    private static readonly PlanetaryNaturalTerrainFamilyIdentity Identity =
        new(PlanetaryPhysicalSurface.EarthBodyId, PlanetaryNaturalTerrainFamilies.ProofGeneration, Seed);

    public static void Run()
    {
        var parity = VerifyCpuDirectGlslPreparedGpuParity();
        Console.WriteLine($"Natural terrain CPU/direct-GLSL/prepared-GPU: samples={parity.Samples}; preparedMax={parity.Prepared:E17}; gradientMax={parity.Gradient:E17}");
    }

    private static ParityResult VerifyCpuDirectGlslPreparedGpuParity()
    {
        var paths = ResolveGpuPaths();
        var cases = BuildParityCases();
        var queries = BuildQueries(cases, descriptorGeneration: 17u, out var points);
        var direct = InvokeGpu(queries, paths, paths.DirectShader, out var directMetrics);
        var prepared = InvokeGpu(queries, paths, paths.PreparedShader, out var preparedMetrics);
        double macro = 0d, meso = 0d, preparedHeight = 0d, gradient = 0d, weight = 0d;
        double weightGradient = 0d, orientation = 0d, bound = 0d;
        for (var index = 0; index < cases.Count; index++)
        {
            var current = cases[index]; var directResult = direct[index]; var result = prepared[index];
            Require(directResult.Valid == 1 && result.Valid == 1 && directMetrics.ValidationErrors == 0 &&
                preparedMetrics.ValidationErrors == 0, $"P2C1 Vulkan proof result {index} is valid");
            var descriptor = PlanetaryNaturalTerrainFamilies.EvaluateCellDescriptor(points[index], current.Identity);
            PlanetaryNaturalTerrainFieldSample expectedMacro, expectedMeso, expectedTotal;
            PlanetaryNaturalTerrainFamily first, second; double expectedWeight; Double3 expectedWeightGradient;
            PlanetaryNaturalTerrainFamilyBounds expectedBounds;
            if (current.Mode == 0u)
            {
                var sample = PlanetaryNaturalTerrainFamilies.EvaluateComposed(points[index], current.Identity);
                expectedMacro = sample.Macro; expectedMeso = sample.Meso; expectedTotal = sample.Total;
                first = sample.FirstFamily; second = sample.SecondFamily;
                expectedWeight = sample.SecondWeight; expectedWeightGradient = sample.SecondWeightGradient;
                expectedBounds = PlanetaryNaturalTerrainFamilies.ComposedBounds();
            }
            else
            {
                var family = (PlanetaryNaturalTerrainFamily)current.Family;
                var sample = PlanetaryNaturalTerrainFamilies.EvaluateFamily(points[index], family, current.Identity);
                expectedMacro = sample.Macro; expectedMeso = sample.Meso; expectedTotal = sample.Total;
                first = second = family; expectedWeight = 0d; expectedWeightGradient = default;
                expectedBounds = PlanetaryNaturalTerrainFamilies.Bounds(family);
            }
            var expectedPrepared = Add(expectedMacro, expectedMeso);
            Require(ReadUInt(result.ReconstructedHighX) == (uint)current.Identity.PhysicalFieldGeneration &&
                ReadUInt(result.ReconstructedHighY) == (uint)(current.Identity.PhysicalFieldGeneration >> 32) &&
                ReadUInt(result.ReconstructedHighZ) == 17u && ReadUInt(result.ReconstructedHighPadding) == current.Identity.Seed &&
                ReadUInt(result.ReconstructedLowX) == PlanetaryNaturalTerrainField.HashVersion &&
                ReadUInt(result.ReconstructedLowY) == PlanetaryNaturalTerrainFamilies.CompositionVersion,
                $"P2C1 generation/hash identity {index}");
            Require(ReadSigned(result.GlobalFace, result.GlobalLevel) == descriptor.Cell.X &&
                ReadSigned(result.GlobalX, result.GlobalY) == descriptor.Cell.Y &&
                ReadSigned(result.LocalAvailable, result.LocalLevel) == descriptor.Cell.Z,
                $"P2C1 canonical signed cell identity {index}");
            Require(result.LocalX == (uint)first && result.LocalY == (uint)second &&
                result.SourceHasLocal == descriptor.ControlHashX && result.ResultTerrainVersion == descriptor.ControlHashY &&
                result.Reserved == PlanetaryNaturalTerrainFamilies.CompositionVersion,
                $"P2C1 cell hash/family/composition identity {index}");
            macro = Maximum(macro, Math.Abs(directResult.FaceV - expectedMacro.Height),
                Math.Abs(result.FaceU - expectedMacro.Height), Math.Abs(result.FaceU - directResult.FaceV));
            meso = Maximum(meso, Math.Abs(directResult.PhysicalHeightMetres - expectedMeso.Height),
                Math.Abs(result.FaceV - expectedMeso.Height), Math.Abs(result.FaceV - directResult.PhysicalHeightMetres));
            preparedHeight = Math.Max(preparedHeight, Math.Abs(result.OracleElevationMetres - expectedPrepared.Height));
            gradient = Maximum(gradient,
                Length(new Double3(result.TerrainV5ElevationMetres, result.LocalResidualMetres, result.PhysicalHeightMetres) - expectedPrepared.BodyGradient),
                Length(new Double3(directResult.OracleElevationMetres, directResult.TerrainV5ElevationMetres,
                    directResult.LocalResidualMetres) - expectedTotal.BodyGradient));
            weight = Maximum(weight, Math.Abs(result.BaseHeightMetres - expectedWeight),
                current.Mode == 0u ? Math.Abs(directResult.ModifierHeightMetres - expectedWeight) : 0d);
            weightGradient = Maximum(weightGradient,
                Length(new Double3(result.ReconstructedX, result.ReconstructedY, result.ReconstructedZ) - expectedWeightGradient),
                current.Mode == 0u ? Length(new Double3(directResult.TiledModifierHeightMetres,
                    directResult.ErosionModifierHeightMetres, directResult.EastGradient) - expectedWeightGradient) : 0d);
            orientation = Math.Max(orientation, Length(new Double3(result.ModifierHeightMetres,
                result.TiledModifierHeightMetres, result.ErosionModifierHeightMetres) - descriptor.Orientation));
            bound = Maximum(bound, Math.Abs(result.EastGradient - expectedBounds.TotalHeight),
                Math.Abs(result.NorthGradient - expectedBounds.TotalGradient));
            Require(Math.Abs(result.OracleElevationMetres) <= result.EastGradient &&
                Length(new(result.TerrainV5ElevationMetres, result.LocalResidualMetres, result.PhysicalHeightMetres)) <= result.NorthGradient,
                $"P2C1 analytic prepared bounds enclose direct canonical sample {index}");
        }
        Require(macro <= 2e-10 && meso <= 2e-10 && preparedHeight <= 2e-10 && gradient <= 3e-11 &&
            weight <= 2e-11 && weightGradient <= 2e-11 && orientation <= 2e-13 && bound <= 2e-11,
            $"P2C1 CPU/direct GLSL/prepared GPU parity: {macro:R}/{meso:R}/{preparedHeight:R}/{gradient:R}/{weight:R}/{weightGradient:R}/{orientation:R}/{bound:R}");
        return new(cases.Count, macro, meso, preparedHeight, gradient, weight, weightGradient, orientation, bound);
    }

    private static List<GpuCase> BuildParityCases()
    {
        var radius = PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres;
        var directions = new[]
        {
            new Double3(1, 1, 0).Normalized(), new Double3(1, 1, 1).Normalized(),
            new Double3(-1, 1, 0).Normalized(), new Double3(1e-12, 1, 1e-12).Normalized(),
            new Double3(1e-12, -1, -1e-12).Normalized(),
            new Double3(-1, 1e-10, 1e-10).Normalized(),
            new Double3(-2_135_177.25, 5_991_331.75, -319_775.5).Normalized()
        };
        var points = directions.Select(direction => direction * radius).ToList();
        points.Add(new(-32.000001, 63.999999, 128.5));
        points.Add(new(6_500_000_000.25, -5_800_000_000.75, 4_423_456_789.5));
        var result = new List<GpuCase>();
        foreach (var point in points)
        {
            result.Add(new(point, 0u, 0u, Identity));
            foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
                result.Add(new(point, (uint)family, 1u, Identity));
        }
        return result;
    }

    private static NativePlanetaryHeightQuery[] BuildQueries(IReadOnlyList<GpuCase> cases, uint descriptorGeneration,
        out Double3[] reconstructed)
    {
        var queries = new NativePlanetaryHeightQuery[cases.Count]; reconstructed = new Double3[cases.Count];
        for (var index = 0; index < cases.Count; index++)
        {
            var current = cases[index]; var encoded = EncodedPosition.Encode(current.Point); reconstructed[index] = encoded.Reconstruct();
            queries[index] = new()
            {
                AnchorHighX = encoded.HighX, AnchorHighY = encoded.HighY, AnchorHighZ = encoded.HighZ,
                AnchorLowX = encoded.LowX, AnchorLowY = encoded.LowY, AnchorLowZ = encoded.LowZ,
                BodyIdLow = (uint)current.Identity.BodyId, BodyIdHigh = (uint)(current.Identity.BodyId >> 32),
                TerrainVersion = (uint)current.Identity.PhysicalFieldGeneration,
                AnchoredTier = (uint)(current.Identity.PhysicalFieldGeneration >> 32),
                TopologyVersion = current.Family, SourcePolicy = current.Mode,
                Reserved0 = current.Identity.Seed, Reserved1 = descriptorGeneration
            };
        }
        return queries;
    }

    private static NativePlanetaryHeightResult[] InvokeGpu(NativePlanetaryHeightQuery[] queries, in GpuPaths paths,
        string shaderPath, out NativePlanetaryHeightQueryMetrics metrics)
    {
        var results = new NativePlanetaryHeightResult[queries.Length];
        var oracle = Encoding.UTF8.GetBytes(paths.Oracle + '\0'); var terrain = Encoding.UTF8.GetBytes(paths.Terrain + '\0');
        var local = Encoding.UTF8.GetBytes(paths.Local + '\0'); var shader = Encoding.UTF8.GetBytes(shaderPath + '\0');
        var value = new NativePlanetaryHeightQueryMetrics { Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(), Version = 1 };
        fixed (NativePlanetaryHeightQuery* queryPointer = queries) fixed (NativePlanetaryHeightResult* resultPointer = results)
        fixed (byte* oraclePointer = oracle) fixed (byte* terrainPointer = terrain) fixed (byte* localPointer = local) fixed (byte* shaderPointer = shader)
        {
            var assets = new NativePlanetaryHeightQueryAssets
            {
                Size = (uint)Marshal.SizeOf<NativePlanetaryHeightQueryAssets>(), Version = 1,
                ElevationOraclePathUtf8 = oraclePointer, ProductionTerrainPathUtf8 = terrainPointer,
                LocalTerrainPathUtf8 = localPointer, ComputeShaderPathUtf8 = shaderPointer
            };
            Require(NativeRuntime.QueryPlanetaryPhysicalHeights(queryPointer, (uint)queries.Length, resultPointer, &assets, &value) == NativeResult.Success,
                "P2C1 proof-only Vulkan preparation succeeds");
        }
        metrics = value; return results;
    }

    private static GpuPaths ResolveGpuPaths()
    {
        var root = RepositoryRoot();
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out var terrain, out var terrainError), $"P2C1 terrain-v5 asset: {terrainError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var local, out var localError), $"P2C1 local-v2 asset: {localError}");
        var paths = new GpuPaths(Path.Combine(root, "assets", "earth", "runtime", "earth_elevation_8192x4096.r16"),
            terrain, local, Path.Combine(root, "build", "native-ninja", "shaders", "planetary_natural_terrain_families_query.comp.spv"),
            Path.Combine(root, "build", "native-ninja", "shaders", "planetary_natural_terrain_prepare.comp.spv"));
        Require(File.Exists(paths.Oracle) && File.Exists(paths.DirectShader) && File.Exists(paths.PreparedShader),
            "P2C1 direct and prepared proof assets exist");
        return paths;
    }

    private static PlanetaryNaturalTerrainFieldSample Add(in PlanetaryNaturalTerrainFieldSample a,
        in PlanetaryNaturalTerrainFieldSample b) => new(a.Height + b.Height, a.BodyGradient + b.BodyGradient);
    private static uint ReadUInt(float value) => unchecked((uint)BitConverter.SingleToInt32Bits(value));
    private static long ReadSigned(uint low, uint high) => unchecked((long)((ulong)low | ((ulong)high << 32)));
    private static double Length(in Double3 value) => Math.Sqrt(value.LengthSquared);
    private static double Maximum(double first, params double[] rest) { foreach (var value in rest) first = Math.Max(first, value); return first; }
    private static double Percentile95(double[] values) { var ordered = values.OrderBy(value => value).ToArray(); return ordered[(int)Math.Ceiling(ordered.Length * .95) - 1]; }
    private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private readonly record struct GpuCase(Double3 Point, uint Family, uint Mode, PlanetaryNaturalTerrainFamilyIdentity Identity);
    private readonly record struct GpuPaths(string Oracle, string Terrain, string Local, string DirectShader, string PreparedShader);
    private readonly record struct ParityResult(int Samples, double Macro, double Meso, double Prepared, double Gradient,
        double Weight, double WeightGradient, double Orientation, double Bound);
}
