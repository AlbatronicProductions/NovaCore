using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class PlanetaryNaturalTerrainFamiliesTests
{
    private static readonly PlanetaryNaturalTerrainFamilyIdentity Identity =
        new(PlanetaryPhysicalSurface.EarthBodyId, PlanetaryNaturalTerrainFamilies.ProofGeneration, 0x4D12D2B1u);
    private static readonly Double3[] Origins =
    [
        new Double3(6_371_008.8d, 17_123d, -8_791d),
        new Double3(-2_135_177d, 5_991_331d, -319_775d),
        new Double3(441_751d, -6_301_173d, 803_119d),
        new Double3(-3_311_731d, -1_717_173d, 5_211_901d),
        new Double3(5_107_311d, -3_213_717d, 2_417_903d),
        new Double3(-4_719_331d, -4_103_119d, 1_917_553d),
        new Double3(2_611_771d, 4_913_117d, 3_101_337d),
        new Double3(-1_211_513d, 2_817_719d, -5_611_331d)
    ];
    private static readonly uint[] Seeds = [0x10203040u, 0x89ABCDEFu, 0x31415926u, 0xC001D00Du,
        0xA511E9B3u, 0x7F4A7C15u, 0xD1B54A32u, 0x94D049BBu];

    public static void Run()
    {
        VerifyDefinitionsAndDeterminism();
        var derivative = VerifyAnalyticDerivatives();
        var continuity = VerifyCellWarpAndBiomeContinuity();
        var planetary = VerifyPlanetaryContinuity();
        var bounds = VerifyAggregateBounds();
        var structure = VerifyStructuralNaturalness();
        var differentiation = VerifyFamilyDifferentiation();
        var parity = VerifyGpuGlslParity();
        var performance = VerifyPerformanceAndAllocations();
        VerifyIsolation();
        Console.WriteLine($"P2B derivative: familyMax={derivative.Family:E17}; composedMax={derivative.Composed:E17}; domainWarpChain=analytic; biomeWeightDerivative=analytic");
        Console.WriteLine($"P2B continuity: valueMax={continuity.Value:E17}; gradientMax={continuity.Gradient:E17}; secondDerivativeResidualMax={continuity.Second:E17}; probes={continuity.Probes}; cell/family/biome/warp=covered");
        Console.WriteLine($"P2B planetary: valueMax={planetary.Value:E17}; gradientMax={planetary.Gradient:E17}; probes={planetary.Probes}; cube/longitude/poles/remote=covered");
        Console.WriteLine($"P2B bounds: macro={bounds.Bounds.MacroHeight:F6}; meso={bounds.Bounds.MesoHeight:F6}; near={bounds.Bounds.NearHeight:F6}; total={bounds.Bounds.TotalHeight:F6}; gradient={bounds.Bounds.TotalGradient:F9}; sampledHeight={bounds.SampledHeight:F6}; sampledGradient={bounds.SampledGradient:F9}");
        foreach (var item in structure)
            Console.WriteLine($"P2B structure {item.Family}: directional={item.Directional:F6}; axisPersistence={item.AxisPersistence:F6}; crossGrid4={item.CrossGrid:F6}; autocorrelation={item.Autocorrelation:F6}; repeatedStamp={item.Repeated:F6}; gen3={item.Generation3Directional:F6}/{item.Generation3AxisPersistence:F6}/{item.Generation3CrossGrid:F6}/{item.Generation3Autocorrelation:F6}/{item.Generation3Repeated:F6}");
        foreach (var item in differentiation)
            Console.WriteLine($"P2B family {item.Family}: rmsHeight={item.RmsHeight:F6}; rmsGradient={item.RmsGradient:F9}; ridgeDensity={item.RidgeDensity:F6}; anisotropy={item.Anisotropy:F6}; correlationLength={item.CorrelationLength:F3}m; slopeP90={item.SlopeP90:F9}");
        Console.WriteLine($"P2B CPU/GLSL: samples={parity.Samples}; familySelectionMismatch=0; octaveIdentity=frozen; valueMax={parity.Value:E17}; gradientMax={parity.Gradient:E17}; weightMax={parity.Weight:E17}; orientationMax={parity.Orientation:E17}; validation=0; gpu={parity.GpuMilliseconds:F6} ms");
        Console.WriteLine($"P2B CPU ({performance.Configuration}): composed={performance.ComposedNanoseconds:F3} ns/query; generation3Full={performance.Generation3Nanoseconds:F3} ns/query; ratio={performance.Ratio:F4}; allocations={performance.Allocations} bytes; perFamily={performance.PerFamilyText}; checksum={performance.Checksum:R}");
    }

    private static void VerifyDefinitionsAndDeterminism()
    {
        Require(PlanetaryNaturalTerrainFamilies.ProofGeneration == 2 &&
            PlanetaryNaturalTerrainField.ProofGeneration == 1 && PlanetaryNaturalTerrainField.HashVersion == 1,
            "P2B version is distinct while frozen P2A generation/hash remain unchanged");
        var point = new Double3(-2_135_177.25d, 5_991_331.75d, -319_775.5d);
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var first = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity);
            Require(first.IsFinite && first.Family == family, $"{family} evaluates a finite explicit macro/meso/near family");
            for (var repeat = 0; repeat < 16; repeat++)
                Require(PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity) == first,
                    $"{family} is body-fixed and bit-deterministic");
            var changedSeed = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity with { Seed = Identity.Seed + 1u });
            Require(changedSeed.Total != first.Total, $"{family} versioned seed participates in field identity");
        }
        var remote = PlanetaryNaturalTerrainFamilies.EvaluateFamily(
            new Double3(6_500_000_000.25d, -5_800_000_000.75d, 4_423_456_789.5d),
            PlanetaryNaturalTerrainFamily.GenericRemote, Identity);
        Require(remote.IsFinite, "remote/no-NCCUBE2 generic family supports large signed body-centred coordinates");
    }

    private static (double Family, double Composed) VerifyAnalyticDerivatives()
    {
        var probes = new[]
        {
            Origins[0], Origins[1], Origins[2], Origins[3],
            new Double3(-6_500_000_000.25d, 5_800_000_000.75d, -4_423_456_789.5d)
        };
        var maximumFamily = 0d; var maximumComposed = 0d;
        foreach (var point in probes)
        {
            foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
            {
                var sample = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity).Total;
                var numerical = NumericalGradient(point, p => PlanetaryNaturalTerrainFamilies.EvaluateFamily(p, family, Identity).Total.Height, .01d);
                maximumFamily = Math.Max(maximumFamily, Length(numerical - sample.BodyGradient));
            }
            var composed = PlanetaryNaturalTerrainFamilies.EvaluateComposed(point, Identity).Total;
            var composedNumerical = NumericalGradient(point, p => PlanetaryNaturalTerrainFamilies.EvaluateComposed(p, Identity).Total.Height, .01d);
            maximumComposed = Math.Max(maximumComposed, Length(composedNumerical - composed.BodyGradient));
        }
        Require(maximumFamily <= 3e-6d && maximumComposed <= 5e-6d,
            $"analytic gradients include domain-warp, anisotropic orientation, shaping, and biome-weight chain rules: {maximumFamily:R}/{maximumComposed:R}");
        return (maximumFamily, maximumComposed);
    }

    private static ContinuityResult VerifyCellWarpAndBiomeContinuity()
    {
        var boundaries = new List<Double3>();
        var sizes = new[] { 120d, 180d, 220d, 260d, 320d, 520d, 1_600d, 2_700d, 3_600d,
            6_500d, 14_000d, 18_000d, 30_000d, 48_000d, 56_000d, 72_000d, 144_000d, 192_000d, 900_000d };
        foreach (var size in sizes)
        {
            boundaries.Add(new(size * 7d, -size * 3d + .371d, size * 2d + .619d));
            boundaries.Add(new(-size * 5d + .283d, size * 4d, -size * 2d + .719d));
            boundaries.Add(new(size * 3d, -size * 2d, size * 5d));
        }
        var maximumValue = 0d; var maximumGradient = 0d; var maximumSecond = 0d; var probes = 0;
        foreach (var point in boundaries)
        {
            foreach (var normal in new[] { Double3.UnitX, Double3.UnitY, Double3.UnitZ,
                new Double3(1d, 1d, 1d).Normalized() })
            {
                MeasureContinuity(point, normal, p => PlanetaryNaturalTerrainFamilies.EvaluateComposed(p, Identity).Total,
                    ref maximumValue, ref maximumGradient, ref maximumSecond); probes++;
            }
        }
        var transitionOrigin = new Double3(-7_200_000d, 1_337_111d, -931_771d);
        var previous = PlanetaryNaturalTerrainFamilies.EvaluateComposed(transitionOrigin, Identity);
        for (var step = 1; step <= 240; step++)
        {
            var point = transitionOrigin + Double3.UnitX * (step * 60_000d);
            var current = PlanetaryNaturalTerrainFamilies.EvaluateComposed(point, Identity);
            if (current.FirstFamily != previous.FirstFamily)
            {
                var left = point - Double3.UnitX * 60_000d; var right = point;
                var leftFamily = previous.FirstFamily;
                for (var iteration = 0; iteration < 60; iteration++)
                {
                    var middle = (left + right) * .5d;
                    if (PlanetaryNaturalTerrainFamilies.EvaluateComposed(middle, Identity).FirstFamily == leftFamily) left = middle;
                    else right = middle;
                }
                MeasureContinuity((left + right) * .5d, Double3.UnitX,
                    p => PlanetaryNaturalTerrainFamilies.EvaluateComposed(p, Identity).Total,
                    ref maximumValue, ref maximumGradient, ref maximumSecond); probes++;
            }
            previous = current;
        }
        Require(maximumValue <= 2e-4d && maximumGradient <= 2e-4d && maximumSecond <= .025d,
            $"P2B cell/warp/family/biome boundaries are C1 with bounded C2 residual: {maximumValue:R}/{maximumGradient:R}/{maximumSecond:R}");
        return new(maximumValue, maximumGradient, maximumSecond, probes);
    }

    private static PlanetaryContinuityResult VerifyPlanetaryContinuity()
    {
        const double epsilon = 1e-10d; var pairs = new List<(Double3, Double3)>();
        foreach (var zeroAxis in new[] { 0, 1, 2 }) foreach (var a in new[] { -1d, 1d }) foreach (var b in new[] { -1d, 1d })
        {
            var values = new double[3]; var cursor = 0;
            for (var axis = 0; axis < 3; axis++) if (axis != zeroAxis) values[axis] = cursor++ == 0 ? a : b;
            var edge = new Double3(values[0], values[1], values[2]).Normalized();
            var perturb = zeroAxis == 0 ? Double3.UnitX : zeroAxis == 1 ? Double3.UnitY : Double3.UnitZ;
            pairs.Add(((edge - perturb * epsilon).Normalized(), (edge + perturb * epsilon).Normalized()));
        }
        foreach (var x in new[] { -1d, 1d }) foreach (var y in new[] { -1d, 1d }) foreach (var z in new[] { -1d, 1d })
        {
            var corner = new Double3(x, y, z).Normalized();
            pairs.Add(((corner - new Double3(epsilon, -epsilon, epsilon)).Normalized(),
                (corner + new Double3(epsilon, -epsilon, epsilon)).Normalized()));
        }
        pairs.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(.31d, Math.PI - epsilon),
            BodyFixedGeography.DirectionFromLatitudeLongitude(.31d, -Math.PI + epsilon)));
        pairs.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(Math.PI * .5d - epsilon, 0d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(Math.PI * .5d - epsilon, Math.PI)));
        pairs.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI * .5d + epsilon, 0d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI * .5d + epsilon, Math.PI)));
        var maxValue = 0d; var maxGradient = 0d;
        foreach (var (a, b) in pairs)
        {
            var first = PlanetaryNaturalTerrainFamilies.EvaluateComposed(a,
                PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres, Identity).Total;
            var second = PlanetaryNaturalTerrainFamilies.EvaluateComposed(b,
                PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres, Identity).Total;
            maxValue = Math.Max(maxValue, Math.Abs(first.Height - second.Height));
            maxGradient = Math.Max(maxGradient, Length(first.BodyGradient - second.BodyGradient));
        }
        Require(maxValue <= 2e-3d && maxGradient <= 3e-4d,
            $"body-centred P2B composition crosses cube edges/corners, longitude wrap, and poles without seams: {maxValue:R}/{maxGradient:R}");
        return new(maxValue, maxGradient, pairs.Count);
    }

    private static BoundsResult VerifyAggregateBounds()
    {
        var composedBounds = PlanetaryNaturalTerrainFamilies.ComposedBounds();
        Require(composedBounds.IsFinite && composedBounds.MacroHeight > 0d && composedBounds.MesoHeight > 0d &&
            composedBounds.NearHeight > 0d && composedBounds.TotalHeight ==
            Enum.GetValues<PlanetaryNaturalTerrainFamily>().Max(f => PlanetaryNaturalTerrainFamilies.Bounds(f).TotalHeight),
            "macro/meso/near and aggregate conservative bounds are explicitly composable");
        var maxHeight = 0d; var maxGradient = 0d;
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var bounds = PlanetaryNaturalTerrainFamilies.Bounds(family);
            for (var z = 0; z < 9; z++) for (var y = 0; y < 9; y++) for (var x = 0; x < 9; x++)
            {
                var point = Origins[(x + y + z) & 3] + new Double3((x - 4d) * 193.7d, (y - 4d) * 173.3d, (z - 4d) * 211.1d);
                var sample = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity);
                Require(Math.Abs(sample.Macro.Height) <= bounds.MacroHeight && Math.Abs(sample.Meso.Height) <= bounds.MesoHeight &&
                    Math.Abs(sample.Near.Height) <= bounds.NearHeight && Math.Abs(sample.Total.Height) <= bounds.TotalHeight &&
                    Length(sample.Total.BodyGradient) <= bounds.TotalGradient,
                    $"{family} sampled macro/meso/near/total remain within analytic aggregate bounds");
                maxHeight = Math.Max(maxHeight, Math.Abs(sample.Total.Height));
                maxGradient = Math.Max(maxGradient, Length(sample.Total.BodyGradient));
            }
        }
        Require(maxHeight <= composedBounds.TotalHeight && maxGradient <= composedBounds.TotalGradient,
            "sampled family set remains within composed culling/clearance envelope");
        return new(composedBounds, maxHeight, maxGradient);
    }

    private static StructureResult[] VerifyStructuralNaturalness()
    {
        var generation = AggregateStructure(null);
        var results = new List<StructureResult>();
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var measured = AggregateStructure(family);
            var result = new StructureResult(family, measured.Directional, measured.AxisPersistence, measured.CrossGrid,
                measured.Autocorrelation, measured.Repeated, generation.Directional, generation.AxisPersistence,
                generation.CrossGrid, generation.Autocorrelation, generation.Repeated);
            Require(result.Directional < .55d && result.AxisPersistence < .65d && result.CrossGrid < .25d && result.Repeated < .62d &&
                result.Directional < result.Generation3Directional * .90d &&
                result.Repeated < result.Generation3Repeated * .88d,
                $"{family} rejects generation-3-like global axis/grid/repeated-stamp structure: {result}");
            results.Add(result);
        }
        return results.ToArray();
    }

    private static StructureMetrics AggregateStructure(PlanetaryNaturalTerrainFamily? family)
    {
        double directional = 0d, autocorrelation = 0d, repeated = 0d;
        double axisCos = 0d, axisSin = 0d, gridCos = 0d, gridSin = 0d;
        for (var caseIndex = 0; caseIndex < Seeds.Length; caseIndex++)
        {
            var identity = Identity with { Seed = Seeds[caseIndex] };
            var originDirection = Origins[caseIndex].Normalized(); var frame = PlanetarySurfaceFrame.AtDirection(originDirection);
            var origin = originDirection * PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres;
            var tensor = OrientationTensor(origin, frame, identity, family);
            directional += tensor.Directional;
            axisCos += Math.Cos(2d * tensor.AxisAngle); axisSin += Math.Sin(2d * tensor.AxisAngle);
            gridCos += tensor.CrossGrid * Math.Cos(4d * tensor.GridAngle);
            gridSin += tensor.CrossGrid * Math.Sin(4d * tensor.GridAngle);
            var period = family is null ? 32d : NearCell(family.Value);
            autocorrelation += Math.Abs(Correlation(origin, frame, identity, family, frame.East * (period * 1.73d)));
            repeated += (Math.Abs(Correlation(origin, frame, identity, family, frame.East * period)) +
                Math.Abs(Correlation(origin, frame, identity, family, frame.North * period)) +
                Math.Abs(Correlation(origin, frame, identity, family, (frame.East + frame.North).Normalized() * period))) / 3d;
        }
        var inverse = 1d / Seeds.Length;
        return new(directional * inverse, Math.Sqrt(axisCos * axisCos + axisSin * axisSin) * inverse,
            Math.Sqrt(gridCos * gridCos + gridSin * gridSin) * inverse, autocorrelation * inverse, repeated * inverse);
    }

    private static (double Directional, double CrossGrid, double AxisAngle, double GridAngle) OrientationTensor(in Double3 origin,
        in PlanetarySurfaceFrame frame, in PlanetaryNaturalTerrainFamilyIdentity identity,
        PlanetaryNaturalTerrainFamily? family)
    {
        double xx = 0d, xy = 0d, yy = 0d, cos4 = 0d, sin4 = 0d, weightSum = 0d;
        for (var y = 0; y < 28; y++) for (var x = 0; x < 28; x++)
        {
            var point = origin + frame.East * ((x - 13.5d) * 19.3d) + frame.North * ((y - 13.5d) * 17.9d);
            var gradient = Sample(point, identity, family).BodyGradient;
            var gx = Double3.Dot(gradient, frame.East); var gy = Double3.Dot(gradient, frame.North);
            var weight = gx * gx + gy * gy;if (weight <= 1e-24d) continue;
            xx += gx * gx;xy += gx * gy;yy += gy * gy;weightSum += weight;
            var angle = Math.Atan2(gy, gx);cos4 += weight * Math.Cos(4d * angle);sin4 += weight * Math.Sin(4d * angle);
        }
        return (Math.Sqrt((xx - yy) * (xx - yy) + 4d * xy * xy) / Math.Max(xx + yy, 1e-30d),
            Math.Sqrt(cos4 * cos4 + sin4 * sin4) / Math.Max(weightSum, 1e-30d),
            .5d * Math.Atan2(2d * xy, xx - yy), .25d * Math.Atan2(sin4, cos4));
    }

    private static double Correlation(in Double3 origin, in PlanetarySurfaceFrame frame,
        in PlanetaryNaturalTerrainFamilyIdentity identity, PlanetaryNaturalTerrainFamily? family, in Double3 translation)
    {
        const int count = 27 * 25;var first = new double[count];var second = new double[count];var cursor = 0;
        for (var y = 0; y < 25; y++) for (var x = 0; x < 27; x++)
        {
            var point = origin + frame.East * ((x - 13d) * 23.7d) + frame.North * ((y - 12d) * 21.1d);
            first[cursor] = Sample(point, identity, family).Height;
            second[cursor++] = Sample(point + translation, identity, family).Height;
        }
        return Pearson(first, second);
    }

    private static PlanetaryNaturalTerrainFieldSample Sample(in Double3 point,
        in PlanetaryNaturalTerrainFamilyIdentity identity, PlanetaryNaturalTerrainFamily? family)
    {
        if (family.HasValue) return PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family.Value, identity).Total;
        var direction = point.Normalized();var generation = PlanetaryPhysicalSurface.EvaluateModifiers(direction, 1_200d);
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        return new(generation.HeightMetres, frame.East * generation.EastGradient + frame.North * generation.NorthGradient);
    }

    private static FamilyStatistics[] VerifyFamilyDifferentiation()
    {
        var results = new List<FamilyStatistics>();
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var values = new List<double>();var slopes = new List<double>();var ridgeCount = 0;var samples = 0;
            double sumHeight2 = 0d, sumGradient2 = 0d, xx = 0d, xy = 0d, yy = 0d;
            var direction = Origins[(int)family % Origins.Length].Normalized();var frame = PlanetarySurfaceFrame.AtDirection(direction);
            var origin = direction * PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres;
            var spacing = NearCell(family) * .37d;
            for (var y = 0; y < 30; y++) for (var x = 0; x < 30; x++)
            {
                var point = origin + frame.East * ((x - 14.5d) * spacing) + frame.North * ((y - 14.5d) * spacing);
                var value = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point, family, Identity).Total;
                var gx = Double3.Dot(value.BodyGradient, frame.East);var gy = Double3.Dot(value.BodyGradient, frame.North);
                values.Add(value.Height);slopes.Add(Math.Sqrt(gx * gx + gy * gy));sumHeight2 += value.Height * value.Height;
                sumGradient2 += gx * gx + gy * gy;xx += gx * gx;xy += gx * gy;yy += gy * gy;samples++;
                if (x > 0 && x < 29)
                {
                    var left = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point - frame.East * spacing, family, Identity).Total.Height;
                    var right = PlanetaryNaturalTerrainFamilies.EvaluateFamily(point + frame.East * spacing, family, Identity).Total.Height;
                    if (value.Height > left && value.Height > right) ridgeCount++;
                }
            }
            slopes.Sort();var anisotropy = Math.Sqrt((xx - yy) * (xx - yy) + 4d * xy * xy) / Math.Max(xx + yy, 1e-30d);
            var correlationLength = spacing * 8d;
            foreach (var multiplier in new[] { .5d, 1d, 2d, 4d, 8d })
            {
                var distance = spacing * multiplier;
                if (Math.Abs(Correlation(origin, frame, Identity, family, frame.East * distance)) < .5d)
                { correlationLength = distance; break; }
            }
            results.Add(new(family, Math.Sqrt(sumHeight2 / samples), Math.Sqrt(sumGradient2 / samples),
                (double)ridgeCount / (30d * 28d), anisotropy, correlationLength, slopes[(int)(slopes.Count * .9d)]));
        }
        var signatures = results.Select(value => $"{Math.Round(value.RmsHeight, 2)}:{Math.Round(value.RmsGradient, 4)}:{Math.Round(value.RidgeDensity, 2)}:{Math.Round(value.Anisotropy, 2)}:{Math.Round(value.CorrelationLength)}").Distinct().Count();
        Require(signatures == results.Count && results.Max(value => value.RmsHeight) > results.Min(value => value.RmsHeight) * 5d &&
            results.Max(value => value.RmsGradient) > results.Min(value => value.RmsGradient) * 3d,
            "all proof families have distinguishable amplitude/slope/ridge/anisotropy/correlation signatures");
        return results.ToArray();
    }

    private static GpuParityResult VerifyGpuGlslParity()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out var terrainPath, out var terrainError), $"P2B parity terrain-v5 asset: {terrainError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out var localError), $"P2B parity local-v2 asset: {localError}");
        var oraclePath = Path.Combine(root, "assets", "earth", "runtime", "earth_elevation_8192x4096.r16");
        var shaderPath = Path.Combine(root, "build", "native-ninja", "shaders", "planetary_natural_terrain_families_query.comp.spv");
        Require(File.Exists(oraclePath) && File.Exists(shaderPath), "P2B proof compute shader and executor assets exist");
        var points = new[] { Origins[0], Origins[1], Origins[2], Origins[3],
            new Double3(-32.000001d, 63.999999d, 128.5d),
            new Double3(6_500_000_000.25d, -5_800_000_000.75d, 4_423_456_789.5d) };
        var cases = new List<GpuCase>();
        foreach (var point in points)
        {
            cases.Add(new(point, 0u, 0u, Identity));
            foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>()) cases.Add(new(point, (uint)family, 1u, Identity));
        }
        var queries = new NativePlanetaryHeightQuery[cases.Count];var reconstructed = new Double3[cases.Count];
        for (var index = 0; index < cases.Count; index++)
        {
            var current = cases[index];var encoded = EncodedPosition.Encode(current.Point);reconstructed[index] = encoded.Reconstruct();
            queries[index] = new NativePlanetaryHeightQuery
            {
                AnchorHighX=encoded.HighX,AnchorHighY=encoded.HighY,AnchorHighZ=encoded.HighZ,
                AnchorLowX=encoded.LowX,AnchorLowY=encoded.LowY,AnchorLowZ=encoded.LowZ,
                BodyIdLow=(uint)current.Identity.BodyId,BodyIdHigh=(uint)(current.Identity.BodyId>>32),
                TerrainVersion=(uint)current.Identity.PhysicalFieldGeneration,AnchoredTier=(uint)(current.Identity.PhysicalFieldGeneration>>32),
                TopologyVersion=current.Family,SourcePolicy=current.Mode,Reserved0=current.Identity.Seed
            };
        }
        var results = InvokeGpu(queries, oraclePath, terrainPath, localPath, shaderPath, out var metrics);
        var maxValue = 0d;var maxGradient = 0d;var maxWeight = 0d;var maxOrientation = 0d;
        for (var index = 0; index < cases.Count; index++)
        {
            var current = cases[index];var result = results[index];
            Require(result.Valid == 1 && result.ResultTerrainVersion == PlanetaryNaturalTerrainFamilies.CompositionVersion &&
                result.Reserved == current.Mode, $"P2B Vulkan result metadata {index}");
            if (current.Mode == 0u)
            {
                var cpu = PlanetaryNaturalTerrainFamilies.EvaluateComposed(reconstructed[index], current.Identity);
                Require(result.GlobalFace == (uint)cpu.FirstFamily && result.GlobalLevel == (uint)cpu.SecondFamily,
                    $"P2B CPU/GLSL family selection {index}");
                maxValue = Maximum(maxValue, Math.Abs(result.FaceU-cpu.Total.Height), Math.Abs(result.FaceV-cpu.Macro.Height),
                    Math.Abs(result.PhysicalHeightMetres-cpu.Meso.Height), Math.Abs(result.BaseHeightMetres-cpu.Near.Height));
                maxGradient = Math.Max(maxGradient, Length(new Double3(result.OracleElevationMetres,
                    result.TerrainV5ElevationMetres,result.LocalResidualMetres)-cpu.Total.BodyGradient));
                maxWeight = Maximum(maxWeight, Math.Abs(result.ModifierHeightMetres-cpu.SecondWeight),
                    Length(new Double3(result.TiledModifierHeightMetres,result.ErosionModifierHeightMetres,result.EastGradient)-cpu.SecondWeightGradient));
            }
            else
            {
                var cpu = PlanetaryNaturalTerrainFamilies.EvaluateFamily(reconstructed[index],
                    (PlanetaryNaturalTerrainFamily)current.Family, current.Identity);
                Require(result.GlobalFace == current.Family && result.GlobalLevel == current.Family,
                    $"P2B CPU/GLSL explicit family identity {index}");
                maxValue = Maximum(maxValue, Math.Abs(result.FaceU-cpu.Total.Height), Math.Abs(result.FaceV-cpu.Macro.Height),
                    Math.Abs(result.PhysicalHeightMetres-cpu.Meso.Height), Math.Abs(result.BaseHeightMetres-cpu.Near.Height));
                maxGradient = Math.Max(maxGradient, Length(new Double3(result.OracleElevationMetres,
                    result.TerrainV5ElevationMetres,result.LocalResidualMetres)-cpu.Total.BodyGradient));
                maxOrientation = Math.Max(maxOrientation, Length(new Double3(result.ModifierHeightMetres,
                    result.TiledModifierHeightMetres,result.ErosionModifierHeightMetres)-cpu.Orientation));
            }
        }
        Require(maxValue <= 2e-10d && maxGradient <= 2e-11d && maxWeight <= 2e-11d &&
            maxOrientation <= 2e-13d && metrics.ValidationErrors == 0,
            $"P2B CPU/compiled GLSL parity: value={maxValue:R}; gradient={maxGradient:R}; weight={maxWeight:R}; orientation={maxOrientation:R}; validation={metrics.ValidationErrors}");
        return new(cases.Count,maxValue,maxGradient,maxWeight,maxOrientation,metrics.GpuMilliseconds);
    }

    private static PerformanceResult VerifyPerformanceAndAllocations()
    {
        var point = Origins[0];var checksum = 0d;
        for (var index = 0; index < 12_000; index++)
        {
            var samplePoint = point + new Double3(index * .013d,-index * .017d,index * .019d);
            checksum += PlanetaryNaturalTerrainFamilies.EvaluateComposed(samplePoint,Identity).Total.Height;
            checksum += PlanetaryPhysicalSurface.EvaluateModifiers(samplePoint.Normalized(),1_200d).HeightMetres;
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 60_000; index++)
        {
            var samplePoint = point + new Double3(index * .013d,-index * .017d,index * .019d);
            var sample = PlanetaryNaturalTerrainFamilies.EvaluateComposed(samplePoint,Identity);
            checksum += sample.Total.Height+sample.Total.BodyGradient.X+sample.Total.BodyGradient.Y+sample.Total.BodyGradient.Z;
        }
        var allocations = GC.GetAllocatedBytesForCurrentThread()-before;
        var p2b = double.MaxValue;var generation3 = double.MaxValue;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            p2b=Math.Min(p2b,MeasureComposed(point,ref checksum));
            generation3=Math.Min(generation3,MeasureGeneration3(point,ref checksum));
        }
        var perFamily = new List<string>();
        foreach(var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
            perFamily.Add($"{family}={MeasureFamily(point,family,ref checksum):F2}ns");
        var ratio=p2b/generation3;
        Require(allocations==0,$"P2B full composition value+analytic-gradient path allocates zero bytes: {allocations}");
        var benchmarkBlend=PlanetaryNaturalTerrainFamilies.EvaluateComposed(point,Identity);
#if DEBUG
        const double acceptanceRatio=1.75d;const string configuration="Debug diagnostic";
#else
        const double acceptanceRatio=1.50d;const string configuration="Release acceptance";
#endif
        Require(ratio<=acceptanceRatio,$"P2B full height+gradient+biome/family composition CPU cost is within the {configuration} limit {acceptanceRatio:F2}x: {ratio:R} ({p2b:R}/{generation3:R} ns; blend={benchmarkBlend.FirstFamily}/{benchmarkBlend.SecondFamily}/{benchmarkBlend.SecondWeight:R})");
        return new(p2b,generation3,ratio,allocations,string.Join(",",perFamily),configuration,checksum);
    }

    private static double MeasureComposed(in Double3 origin,ref double checksum)
    {
        const int iterations=100_000;var start=Stopwatch.GetTimestamp();var sum=0d;
        for(var index=0;index<iterations;index++)
        {
            var point=origin+new Double3(index*.013d,-index*.017d,index*.019d);
            var sample=PlanetaryNaturalTerrainFamilies.EvaluateComposed(point,Identity).Total;
            sum+=sample.Height+sample.BodyGradient.X+sample.BodyGradient.Y+sample.BodyGradient.Z;
        }
        var elapsed=Stopwatch.GetElapsedTime(start).TotalNanoseconds/iterations;checksum+=sum;return elapsed;
    }
    private static double MeasureGeneration3(in Double3 origin,ref double checksum)
    {
        const int iterations=100_000;var start=Stopwatch.GetTimestamp();var sum=0d;
        for(var index=0;index<iterations;index++)
        {
            var point=origin+new Double3(index*.013d,-index*.017d,index*.019d);
            var sample=PlanetaryPhysicalSurface.EvaluateModifiers(point.Normalized(),1_200d);
            sum+=sample.HeightMetres+sample.EastGradient+sample.NorthGradient;
        }
        var elapsed=Stopwatch.GetElapsedTime(start).TotalNanoseconds/iterations;checksum+=sum;return elapsed;
    }
    private static double MeasureFamily(in Double3 origin,PlanetaryNaturalTerrainFamily family,ref double checksum)
    {
        const int iterations=50_000;var start=Stopwatch.GetTimestamp();var sum=0d;
        for(var index=0;index<iterations;index++)
        {
            var point=origin+new Double3(index*.017d,-index*.011d,index*.023d);
            var sample=PlanetaryNaturalTerrainFamilies.EvaluateFamily(point,family,Identity).Total;
            sum+=sample.Height+sample.BodyGradient.X+sample.BodyGradient.Y+sample.BodyGradient.Z;
        }
        var elapsed=Stopwatch.GetElapsedTime(start).TotalNanoseconds/iterations;checksum+=sum;return elapsed;
    }

    private static NativePlanetaryHeightResult[] InvokeGpu(NativePlanetaryHeightQuery[] queries,string oraclePath,
        string terrainPath,string localPath,string shaderPath,out NativePlanetaryHeightQueryMetrics metrics)
    {
        var results=new NativePlanetaryHeightResult[queries.Length];var oracle=Encoding.UTF8.GetBytes(oraclePath+'\0');
        var terrain=Encoding.UTF8.GetBytes(terrainPath+'\0');var local=Encoding.UTF8.GetBytes(localPath+'\0');
        var shader=Encoding.UTF8.GetBytes(shaderPath+'\0');var value=new NativePlanetaryHeightQueryMetrics
            {Size=(uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(),Version=1};
        fixed(NativePlanetaryHeightQuery* queryPointer=queries)fixed(NativePlanetaryHeightResult* resultPointer=results)
        fixed(byte* oraclePointer=oracle)fixed(byte* terrainPointer=terrain)fixed(byte* localPointer=local)fixed(byte* shaderPointer=shader)
        {
            var assets=new NativePlanetaryHeightQueryAssets{Size=(uint)Marshal.SizeOf<NativePlanetaryHeightQueryAssets>(),Version=1,
                ElevationOraclePathUtf8=oraclePointer,ProductionTerrainPathUtf8=terrainPointer,LocalTerrainPathUtf8=localPointer,ComputeShaderPathUtf8=shaderPointer};
            Require(NativeRuntime.QueryPlanetaryPhysicalHeights(queryPointer,(uint)queries.Length,resultPointer,&assets,&value)==NativeResult.Success,
                "P2B proof-only Vulkan query succeeds");
        }
        metrics=value;return results;
    }

    private static void VerifyIsolation()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var shaderRoot=Path.Combine(root,"native","NovaCore.Native","shaders");
        var authorizedCandidateConsumers=new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "planetary_natural_terrain_families_query.comp", "planetary_natural_terrain_prepare.comp",
            "planetary_natural_terrain_surface.glsl", "planetary_natural_terrain_global_prepare.comp",
            "planetary_natural_terrain_anchored_prepare.comp"
        };
        var includes=Directory.EnumerateFiles(shaderRoot,"*",SearchOption.TopDirectoryOnly)
            .Where(path=>!path.EndsWith("planetary_natural_terrain_families.glsl",StringComparison.OrdinalIgnoreCase)&&
                !authorizedCandidateConsumers.Contains(Path.GetFileName(path)))
            .Where(path=>File.ReadAllText(path).Contains("planetary_natural_terrain_families.glsl",StringComparison.Ordinal)).ToArray();
        Require(includes.Length==0,"P2B family authority is consumed only by the explicit M12D candidate adapters");
        var physical=File.ReadAllText(Path.Combine(root,"src","NovaCore.Graphics","PlanetaryPhysicalSurface.cs"));
        Require(physical.Contains("M12DNaturalTerrainCandidate",StringComparison.Ordinal)&&
            physical.Contains("Generation3 = 3",StringComparison.Ordinal)&&
            physical.Contains("_runtimeGeneration = (uint)PlanetaryPhysicalSurfaceGeneration.Generation3",StringComparison.Ordinal),
            "P2B routing is explicit candidate-only and generation 3 remains the default authority");
    }

    private static void MeasureContinuity(in Double3 boundary,in Double3 normal,
        Func<Double3,PlanetaryNaturalTerrainFieldSample> evaluator,ref double maxValue,ref double maxGradient,ref double maxSecond)
    {
        const double epsilon=1e-3d;const double h=.08d;
        var left=evaluator(boundary-normal*epsilon);var leftFar=evaluator(boundary-normal*(2d*epsilon));
        var right=evaluator(boundary+normal*epsilon);var rightFar=evaluator(boundary+normal*(2d*epsilon));
        maxValue=Math.Max(maxValue,Math.Abs((2d*left.Height-leftFar.Height)-(2d*right.Height-rightFar.Height)));
        maxGradient=Math.Max(maxGradient,Length((left.BodyGradient*2d-leftFar.BodyGradient)-(right.BodyGradient*2d-rightFar.BodyGradient)));
        var center=evaluator(boundary).BodyGradient;var minus=evaluator(boundary-normal*h).BodyGradient;
        var plus=evaluator(boundary+normal*h).BodyGradient;var minusHalf=evaluator(boundary-normal*(h*.5d)).BodyGradient;
        var plusHalf=evaluator(boundary+normal*(h*.5d)).BodyGradient;var centerSlope=Double3.Dot(center,normal);
        var leftSecond=2d*(centerSlope-Double3.Dot(minusHalf,normal))/(h*.5d)-(centerSlope-Double3.Dot(minus,normal))/h;
        var rightSecond=2d*(Double3.Dot(plusHalf,normal)-centerSlope)/(h*.5d)-(Double3.Dot(plus,normal)-centerSlope)/h;
        maxSecond=Math.Max(maxSecond,Math.Abs(leftSecond-rightSecond));
    }
    private static Double3 NumericalGradient(in Double3 point,Func<Double3,double> evaluator,double step)=>new(
        (evaluator(point+Double3.UnitX*step)-evaluator(point-Double3.UnitX*step))/(2d*step),
        (evaluator(point+Double3.UnitY*step)-evaluator(point-Double3.UnitY*step))/(2d*step),
        (evaluator(point+Double3.UnitZ*step)-evaluator(point-Double3.UnitZ*step))/(2d*step));
    private static double Pearson(double[] first,double[] second)
    {
        var meanA=first.Average();var meanB=second.Average();double numerator=0d,aa=0d,bb=0d;
        for(var index=0;index<first.Length;index++){var a=first[index]-meanA;var b=second[index]-meanB;numerator+=a*b;aa+=a*a;bb+=b*b;}
        return numerator/Math.Sqrt(Math.Max(aa*bb,1e-60d));
    }
    private static double NearCell(PlanetaryNaturalTerrainFamily family)=>family switch
    {
        PlanetaryNaturalTerrainFamily.Grassland=>180d,PlanetaryNaturalTerrainFamily.ScrubDry=>240d,
        PlanetaryNaturalTerrainFamily.RockyMountain=>520d,PlanetaryNaturalTerrainFamily.Alpine=>320d,
        PlanetaryNaturalTerrainFamily.DesertDunes=>120d,PlanetaryNaturalTerrainFamily.CoastWetland=>180d,
        PlanetaryNaturalTerrainFamily.SnowGlacial=>260d,_=>220d
    };
    private static double Maximum(double first,params double[] rest){foreach(var value in rest)first=Math.Max(first,value);return first;}
    private static double Length(in Double3 value)=>Math.Sqrt(value.LengthSquared);
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}

    private readonly record struct ContinuityResult(double Value,double Gradient,double Second,int Probes);
    private readonly record struct PlanetaryContinuityResult(double Value,double Gradient,int Probes);
    private readonly record struct BoundsResult(PlanetaryNaturalTerrainFamilyBounds Bounds,double SampledHeight,double SampledGradient);
    private readonly record struct StructureMetrics(double Directional,double AxisPersistence,double CrossGrid,double Autocorrelation,double Repeated);
    private readonly record struct StructureResult(PlanetaryNaturalTerrainFamily Family,double Directional,double AxisPersistence,double CrossGrid,double Autocorrelation,double Repeated,double Generation3Directional,double Generation3AxisPersistence,double Generation3CrossGrid,double Generation3Autocorrelation,double Generation3Repeated);
    private readonly record struct FamilyStatistics(PlanetaryNaturalTerrainFamily Family,double RmsHeight,double RmsGradient,double RidgeDensity,double Anisotropy,double CorrelationLength,double SlopeP90);
    private readonly record struct GpuCase(Double3 Point,uint Family,uint Mode,PlanetaryNaturalTerrainFamilyIdentity Identity);
    private readonly record struct GpuParityResult(int Samples,double Value,double Gradient,double Weight,double Orientation,double GpuMilliseconds);
    private readonly record struct PerformanceResult(double ComposedNanoseconds,double Generation3Nanoseconds,double Ratio,long Allocations,string PerFamilyText,string Configuration,double Checksum);
}
