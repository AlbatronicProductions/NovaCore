using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class PlanetaryNaturalTerrainFieldTests
{
    private const double CellSize = 32d;
    private const double Amplitude = 2.75d;
    private static readonly PlanetaryNaturalTerrainFieldIdentity Identity = new(6, 1, 17, 3, 0x12345678u);
    private static readonly Double3 AxisA = new(.7715167498104595d, -.1543033499620919d, .6172133998483676d);
    private static readonly Double3 AxisB = new(-.3244428422615251d, .8111071056538127d, .4866642633922876d);
    private static readonly Double3 AxisC = new(.1690308509457033d, .8451542547285166d, -.50709255283711d);
    private static readonly Double3 WarpA = new(.8728715609439696d, .4364357804719848d, -.2182178902359924d);
    private static readonly Double3 WarpB = new(.3903600291794133d, -.6506000486323555d, .6506000486323555d);

    public static void Run()
    {
        VerifyDomainReduction();
        VerifyHashAndGradients();
        VerifyDeterminismAndAnalyticGradient();
        var continuity = VerifyC2Continuity();
        var planetary = VerifyPlanetaryContinuity();
        var bounds = VerifyConservativeBounds();
        var structure = VerifyNaturalnessStructure();
        var performance = VerifyPerformanceAndAllocation();
        var parity = VerifyGpuGlslParity();
        VerifyIsolation();
        Console.WriteLine($"P2A continuity: valueMax={continuity.Value:E17}; gradientMax={continuity.Gradient:E17}; secondDerivativeResidualMax={continuity.Second:E17}; probes={continuity.Probes}");
        Console.WriteLine($"P2A planetary: valueLipschitzRatioMax={planetary.ValueRatio:E17}; gradientDeltaMax={planetary.GradientDelta:E17}; probes={planetary.Probes}; cube/longitude/poles/antipodes=covered");
        Console.WriteLine($"P2A bounds: sampledValue={bounds.Value:E17}/{PlanetaryNaturalTerrainField.ValueBound(Amplitude):E17}; sampledGradient={bounds.Gradient:E17}/{PlanetaryNaturalTerrainField.GradientBound(CellSize,Amplitude):E17}; analytic=sqrt(3),12.25/cell");
        Console.WriteLine($"P2A structure: directional={structure.P2ADirectional:F6}/{structure.Generation3Directional:F6}; crossGrid={structure.P2ACrossGrid:F6}/{structure.Generation3CrossGrid:F6}; autocorrelation={structure.P2AAutocorrelation:F6}/{structure.Generation3Autocorrelation:F6}; repeated={structure.P2ARepeated:F6}/{structure.Generation3Repeated:F6}; seeds=4; domains=4");
        Console.WriteLine($"P2A CPU: p2a={performance.P2ANanoseconds:F3} ns/query; generation3={performance.Generation3Nanoseconds:F3} ns/query; ratio={performance.Ratio:F4}; allocations={performance.Allocations} bytes/query-path; checksum={performance.Checksum:R}");
        Console.WriteLine($"P2A CPU/GLSL: samples={parity.Samples}; cellMismatch=0; hashMismatch=0; gradientSelectionMax={parity.Selection:E17}; valueMax={parity.Value:E17}; bodyGradientMax={parity.Gradient:E17}; validation=0; gpu={parity.GpuMilliseconds:F6} ms");
    }

    private static void VerifyDomainReduction()
    {
        var negative = PlanetaryNaturalTerrainField.ReduceBodyPoint(new(-32.25d, -64d, 95.999d), 32d);
        Require(negative.Cell == new PlanetaryNaturalTerrainCell(-2, -2, 2) &&
            Near(negative.Fraction.X, .9921875d, 0d) && negative.Fraction.Y == 0d &&
            Near(negative.Fraction.Z, .99996875d, 2e-16d), "negative FP64 floor/fraction contract");
        var remote = PlanetaryNaturalTerrainField.ReduceBodyPoint(
            new(6_500_000_000.625d, -5_800_000_000.25d, 4_423_456_789.875d), 1d);
        Require(remote.Cell == new PlanetaryNaturalTerrainCell(6_500_000_000, -5_800_000_001, 4_423_456_789) &&
            remote.Fraction == new Double3(.625d, .75d, .875d), "large signed coordinates retain full cell identity");
        var direction = new Double3(-1d, 2d, -3d).Normalized();
        var planetary = PlanetaryNaturalTerrainField.ReduceDomain(direction, 137d);
        var point = direction * PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres;
        var direct = PlanetaryNaturalTerrainField.ReduceBodyPoint(point, 137d);
        Require(planetary == direct && planetary.IsValid, "planetary reduction is exactly body-centred p=R*d then q=p/cellSize");
    }

    private static void VerifyHashAndGradients()
    {
        Require(PlanetaryNaturalTerrainField.HashCell(new(6, 1, 17, 0, 0x12345678u), new(0, 0, 0)) == 0xA761D790u,
            "frozen hash vector origin");
        Require(PlanetaryNaturalTerrainField.HashCell(Identity, new(-1, -2, -3)) == 0xE85B3BDDu,
            "frozen hash vector negative cells");
        Require(PlanetaryNaturalTerrainField.HashCell(new(0x1_00000006ul, 0x2_00000001ul, uint.MaxValue, 19, 0xDEADBEEFu),
            new(6_500_000_000, -5_800_000_000, 4_423_456_789)) == 0x5FF59D07u, "frozen hash vector full high words and distant signed cells");
        Require(PlanetaryNaturalTerrainField.HashCell(Identity, new(-1, 2, 3)) !=
            PlanetaryNaturalTerrainField.HashCell(Identity, new(1, 2, 3)), "signed cell bits participate without absolute-value aliasing");
        Require(PlanetaryNaturalTerrainField.HashCell(Identity, new(6_500_000_000, 2, 3)) !=
            PlanetaryNaturalTerrainField.HashCell(Identity, new(2_205_032_704, 2, 3)), "distant coordinates do not alias through 32-bit truncation");
        var gradients = new HashSet<Double3>();
        for (uint index = 0; index < 24; index++)
        {
            var gradient = PlanetaryNaturalTerrainField.SelectGradient(index);
            Require(Math.Abs(gradient.LengthSquared - 1d) <= 3e-16d, $"unit gradient {index}");
            gradients.Add(gradient);
        }
        Require(gradients.Count == 24, "all 24 signed permutations of (2,1,0)/sqrt(5) are selectable");
    }

    private static void VerifyDeterminismAndAnalyticGradient()
    {
        var probes = new[]
        {
            new Double3(17.25d, 29.5d, -83.75d), new Double3(-1_234_567.125d, 765_432.875d, -55.5d),
            new Double3(PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres, 0d, 0d),
            new Double3(-48d, -64d, -96d), new Double3(63.999999d, -32.000001d, 128.5d)
        };
        var maximumDerivativeError = 0d;
        foreach (var point in probes)
        {
            var first = PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, Amplitude, Identity);
            for (var repeat = 0; repeat < 32; repeat++)
                Require(PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, Amplitude, Identity) == first,
                    "value and analytic gradient are bit-stable across repeated execution");
            const double step = .001d;
            var numerical = new Double3(
                (PlanetaryNaturalTerrainField.EvaluateBodyPoint(point + Double3.UnitX * step, CellSize, Amplitude, Identity).Height -
                 PlanetaryNaturalTerrainField.EvaluateBodyPoint(point - Double3.UnitX * step, CellSize, Amplitude, Identity).Height) / (2d * step),
                (PlanetaryNaturalTerrainField.EvaluateBodyPoint(point + Double3.UnitY * step, CellSize, Amplitude, Identity).Height -
                 PlanetaryNaturalTerrainField.EvaluateBodyPoint(point - Double3.UnitY * step, CellSize, Amplitude, Identity).Height) / (2d * step),
                (PlanetaryNaturalTerrainField.EvaluateBodyPoint(point + Double3.UnitZ * step, CellSize, Amplitude, Identity).Height -
                 PlanetaryNaturalTerrainField.EvaluateBodyPoint(point - Double3.UnitZ * step, CellSize, Amplitude, Identity).Height) / (2d * step));
            maximumDerivativeError = Math.Max(maximumDerivativeError, Length(numerical - first.BodyGradient));
        }
        Require(maximumDerivativeError <= 2e-8d, $"analytic derivative includes corner field and interpolation-weight terms: {maximumDerivativeError:R}");
    }

    private static (double Value, double Gradient, double Second, int Probes) VerifyC2Continuity()
    {
        var origins = new[] { -6_500_000_000d, -40d, 0d, 84d, 6_500_000_000d };
        var normals = new[]
        {
            Double3.UnitX, Double3.UnitY, Double3.UnitZ,
            new Double3(1d,1d,0d).Normalized(), new Double3(1d,0d,1d).Normalized(),
            new Double3(0d,1d,1d).Normalized(), new Double3(1d,1d,1d).Normalized()
        };
        var maxValue = 0d; var maxGradient = 0d; var maxSecond = 0d; var probes = 0;
        foreach (var origin in origins) foreach (var normal in normals)
        {
            var boundary = new Double3(
                normal.X == 0d ? origin + .371d : origin,
                normal.Y == 0d ? -origin * .5d + .619d : -origin * .5d,
                normal.Z == 0d ? origin * .25d + .283d : origin * .25d);
            const double epsilon = 1e-4d; const double h = 8e-3d;
            var left = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary - normal * epsilon, 1d, 1d, Identity);
            var leftFar = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary - normal * (2d*epsilon), 1d, 1d, Identity);
            var right = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary + normal * epsilon, 1d, 1d, Identity);
            var rightFar = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary + normal * (2d*epsilon), 1d, 1d, Identity);
            // Linear extrapolation removes the ordinary within-cell slope so the
            // comparison measures the two limits at the shared boundary.
            maxValue = Math.Max(maxValue, Math.Abs((2d*left.Height-leftFar.Height)-(2d*right.Height-rightFar.Height)));
            maxGradient = Math.Max(maxGradient, Length((left.BodyGradient*2d-leftFar.BodyGradient)-(right.BodyGradient*2d-rightFar.BodyGradient)));
            var centerGradient = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary,1d,1d,Identity).BodyGradient;
            var minus1Gradient = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary-normal*h,1d,1d,Identity).BodyGradient;
            var plus1Gradient = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary+normal*h,1d,1d,Identity).BodyGradient;
            var minusHalfGradient = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary-normal*(h*.5d),1d,1d,Identity).BodyGradient;
            var plusHalfGradient = PlanetaryNaturalTerrainField.EvaluateBodyPoint(boundary+normal*(h*.5d),1d,1d,Identity).BodyGradient;
            var centerSlope=Double3.Dot(centerGradient,normal);
            var leftSecondFull=(centerSlope-Double3.Dot(minus1Gradient,normal))/h;
            var rightSecondFull=(Double3.Dot(plus1Gradient,normal)-centerSlope)/h;
            var leftSecondHalf=(centerSlope-Double3.Dot(minusHalfGradient,normal))/(h*.5d);
            var rightSecondHalf=(Double3.Dot(plusHalfGradient,normal)-centerSlope)/(h*.5d);
            var leftSecond=2d*leftSecondHalf-leftSecondFull;
            var rightSecond=2d*rightSecondHalf-rightSecondFull;
            maxSecond = Math.Max(maxSecond, Math.Abs(leftSecond - rightSecond)); probes++;
        }
        Require(maxValue <= 2e-6d && maxGradient <= 2e-5d && maxSecond <= .03d,
            $"C2 face/edge/corner continuity: value={maxValue:R}; gradient={maxGradient:R}; second={maxSecond:R}");
        return (maxValue, maxGradient, maxSecond, probes);
    }

    private static (double ValueRatio, double GradientDelta, int Probes) VerifyPlanetaryContinuity()
    {
        const double epsilon = 1e-10d;
        var points = new List<(Double3 A, Double3 B)>();
        // Cube-face edges and corners are crossed in body-fixed space, not addressed through a cube face.
        foreach (var zeroAxis in new[] { 0, 1, 2 }) foreach (var a in new[] { -1d, 1d }) foreach (var b in new[] { -1d, 1d })
        {
            var values = new double[3]; var cursor = 0;
            for (var axis = 0; axis < 3; axis++) if (axis != zeroAxis) values[axis] = cursor++ == 0 ? a : b;
            var edge = new Double3(values[0], values[1], values[2]).Normalized();
            var perturb = zeroAxis == 0 ? Double3.UnitX : zeroAxis == 1 ? Double3.UnitY : Double3.UnitZ;
            points.Add(((edge - perturb * epsilon).Normalized(), (edge + perturb * epsilon).Normalized()));
        }
        foreach (var x in new[] { -1d, 1d }) foreach (var y in new[] { -1d, 1d }) foreach (var z in new[] { -1d, 1d })
        {
            var corner = new Double3(x, y, z).Normalized();
            points.Add(((corner - new Double3(epsilon, -epsilon, epsilon)).Normalized(),
                (corner + new Double3(epsilon, -epsilon, epsilon)).Normalized()));
        }
        points.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(.31d, Math.PI - epsilon),
            BodyFixedGeography.DirectionFromLatitudeLongitude(.31d, -Math.PI + epsilon)));
        points.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(Math.PI * .5d - epsilon, 0d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(Math.PI * .5d - epsilon, Math.PI)));
        points.Add((BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI * .5d + epsilon, 0d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI * .5d + epsilon, Math.PI)));
        points.Add((new Double3(1d, 2d, 3d).Normalized(), new Double3(1d + epsilon, 2d - epsilon, 3d).Normalized()));
        points.Add((new Double3(-1d, -2d, -3d).Normalized(), new Double3(-1d - epsilon, -2d + epsilon, -3d).Normalized()));

        var maximumRatio = 0d; var maximumGradientDelta = 0d;
        var bound = PlanetaryNaturalTerrainField.GradientBound(CellSize, Amplitude);
        foreach (var (a, b) in points)
        {
            var first = PlanetaryNaturalTerrainField.Evaluate(a, CellSize, Amplitude, Identity);
            var second = PlanetaryNaturalTerrainField.Evaluate(b, CellSize, Amplitude, Identity);
            var distance = Length((a - b) * PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres);
            maximumRatio = Math.Max(maximumRatio, Math.Abs(first.Height - second.Height) / Math.Max(bound * distance, 1e-30d));
            maximumGradientDelta = Math.Max(maximumGradientDelta, Length(first.BodyGradient - second.BodyGradient));
        }
        Require(maximumRatio <= 1.000001d && maximumGradientDelta <= 2e-5d,
            $"body-centred field crosses cube/longitude/pole topology without a seam: ratio={maximumRatio:R}; gradient={maximumGradientDelta:R}");
        return (maximumRatio, maximumGradientDelta, points.Count);
    }

    private static (double Value, double Gradient) VerifyConservativeBounds()
    {
        // Analytic proof: each corner dot is bounded by sqrt(3), and interpolation
        // weights form a convex partition. For the gradient, the weighted unit
        // gradients contribute <=1; max fade' is 1.875 and the two signed weights
        // contribute 3.75 per axis, yielding 1+3*3.75=12.25 in lattice space.
        Require(PlanetaryNaturalTerrainField.MaximumUnitValue == Math.Sqrt(3d) &&
            PlanetaryNaturalTerrainField.MaximumUnitGradient == 12.25d, "frozen analytic envelopes match the derivation");
        var maximumValue = 0d; var maximumGradient = 0d;
        for (var z = 0; z < 23; z++) for (var y = 0; y < 23; y++) for (var x = 0; x < 23; x++)
        {
            var point = new Double3(-815.75d + x * 3.17d, 411.125d + y * 2.73d, -93.625d + z * 4.09d);
            var sample = PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, Amplitude, Identity);
            maximumValue = Math.Max(maximumValue, Math.Abs(sample.Height));
            maximumGradient = Math.Max(maximumGradient, Length(sample.BodyGradient));
        }
        Require(maximumValue <= PlanetaryNaturalTerrainField.ValueBound(Amplitude) &&
            maximumGradient <= PlanetaryNaturalTerrainField.GradientBound(CellSize, Amplitude), "sampled field stays inside analytic envelopes");
        return (maximumValue, maximumGradient);
    }

    private static StructureResult VerifyNaturalnessStructure()
    {
        var origins = new[]
        {
            new Double3(6_371_008.8d, 17_123d, -8_791d), new Double3(-2_135_177d, 5_991_331d, -319_775d),
            new Double3(441_751d, -6_301_173d, 803_119d), new Double3(-3_311_731d, -1_717_173d, 5_211_901d)
        };
        var seeds = new[] { 0x10203040u, 0x89ABCDEFu, 0x31415926u, 0xC001D00Du };
        double p2aDirectional = 0d, p2aGrid = 0d, p2aAuto = 0d, p2aRepeated = 0d;
        double generationDirectional = 0d, generationGrid = 0d, generationAuto = 0d, generationRepeated = 0d;
        for (var caseIndex = 0; caseIndex < seeds.Length; caseIndex++)
        {
            var identity = Identity with { Seed = seeds[caseIndex], OctaveId = (uint)caseIndex };
            var origin = origins[caseIndex];
            var p2a = OrientationMetrics(origin, identity, false); var generation = OrientationMetrics(origin, identity, true);
            p2aDirectional += p2a.Directional; p2aGrid += p2a.CrossGrid;
            generationDirectional += generation.Directional; generationGrid += generation.CrossGrid;
            p2aAuto += Correlation(origin, identity, false, AxisC * 32d);
            generationAuto += Correlation(origin, identity, true, AxisC * 32d);
            p2aRepeated += RepeatedSignature(origin, identity, false);
            generationRepeated += RepeatedSignature(origin, identity, true);
        }
        const double inverse = .25d;
        var result = new StructureResult(p2aDirectional * inverse, generationDirectional * inverse,
            p2aGrid * inverse, generationGrid * inverse, p2aAuto * inverse, generationAuto * inverse,
            p2aRepeated * inverse, generationRepeated * inverse);
        Require(result.P2ADirectional < .32d && result.P2ADirectional < result.Generation3Directional * .72d,
            $"multiple-seed P2A orientation energy has no generation-3 dominant axis: {result}");
        Require(result.P2ACrossGrid < .28d && result.P2ACrossGrid < result.Generation3CrossGrid * .78d,
            $"multiple-seed P2A angular fourth harmonic rejects the generation-3 cross-grid signature: {result}");
        Require(Math.Abs(result.P2AAutocorrelation) < .28d && result.P2AAutocorrelation < result.Generation3Autocorrelation * .6d,
            $"translated P2A cells decorrelate while the 32 m carrier persists: {result}");
        Require(result.P2ARepeated < .34d && result.P2ARepeated < result.Generation3Repeated * .62d,
            $"multiple translations expose no cell-period stamp signature: {result}");
        return result;
    }

    private static (double Directional, double CrossGrid) OrientationMetrics(in Double3 origin,
        in PlanetaryNaturalTerrainFieldIdentity identity, bool generation3)
    {
        double xx = 0d, xy = 0d, yy = 0d, cos4 = 0d, sin4 = 0d, weightSum = 0d;
        for (var y = 0; y < 48; y++) for (var x = 0; x < 48; x++)
        {
            var point = origin + Double3.UnitX * ((x - 23.5d) * 2.31d) + Double3.UnitY * ((y - 23.5d) * 2.17d);
            var gradient = generation3 ? Generation3Carrier(point).BodyGradient :
                PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, 1d, identity).BodyGradient;
            var gx = gradient.X; var gy = gradient.Y; var weight = gx * gx + gy * gy;
            if (weight <= 1e-20d) continue;
            xx += gx * gx; xy += gx * gy; yy += gy * gy; weightSum += weight;
            var angle = Math.Atan2(gy, gx); cos4 += weight * Math.Cos(4d * angle); sin4 += weight * Math.Sin(4d * angle);
        }
        var directional = Math.Sqrt((xx - yy) * (xx - yy) + 4d * xy * xy) / Math.Max(xx + yy, 1e-30d);
        var grid = Math.Sqrt(cos4 * cos4 + sin4 * sin4) / Math.Max(weightSum, 1e-30d);
        return (directional, grid);
    }

    private static double Correlation(in Double3 origin, in PlanetaryNaturalTerrainFieldIdentity identity,
        bool generation3, in Double3 translation)
    {
        const int count = 41 * 37; var first = new double[count]; var second = new double[count]; var cursor = 0;
        for (var y = 0; y < 37; y++) for (var x = 0; x < 41; x++)
        {
            var point = origin + Double3.UnitX * ((x - 20d) * 2.73d) + Double3.UnitY * ((y - 18d) * 2.41d);
            first[cursor] = generation3 ? Generation3Carrier(point).Height :
                PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, 1d, identity).Height;
            var shifted = point + translation;
            second[cursor++] = generation3 ? Generation3Carrier(shifted).Height :
                PlanetaryNaturalTerrainField.EvaluateBodyPoint(shifted, CellSize, 1d, identity).Height;
        }
        return Pearson(first, second);
    }

    private static double RepeatedSignature(in Double3 origin, in PlanetaryNaturalTerrainFieldIdentity identity, bool generation3)
    {
        // Compare each primitive at its own proposed repeat period. Sub-cell
        // translations are intentionally excluded because any smooth field is
        // strongly correlated there and that is not a tiling signature.
        var translations = generation3
            ? new[] { AxisC * 32d, AxisA * 7d, AxisB * 1.4d }
            : new[] { Double3.UnitX * CellSize, Double3.UnitY * CellSize, Double3.UnitZ * CellSize,
                new Double3(1d,1d,1d).Normalized()*CellSize };
        var total=0d;
        foreach(var translation in translations)total+=Math.Abs(Correlation(origin,identity,generation3,translation));
        return total/translations.Length;
    }

    private static double Pearson(double[] first, double[] second)
    {
        var meanA = first.Average(); var meanB = second.Average(); double numerator = 0d, aa = 0d, bb = 0d;
        for (var index = 0; index < first.Length; index++)
        {
            var a = first[index] - meanA; var b = second[index] - meanB;
            numerator += a * b; aa += a * a; bb += b * b;
        }
        return numerator / Math.Sqrt(Math.Max(aa * bb, 1e-60d));
    }

    private static PerformanceResult VerifyPerformanceAndAllocation()
    {
        var point = new Double3(6_371_008.8d, 31_337.25d, -73_119.75d); var checksum = 0d;
        for (var index = 0; index < 30_000; index++)
        {
            var samplePoint = point + new Double3(index * .013d, -index * .017d, index * .019d);
            checksum += PlanetaryNaturalTerrainField.EvaluateBodyPoint(samplePoint, CellSize, 1d, Identity).Height;
            checksum += Generation3Carrier(samplePoint).Height;
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
        {
            var samplePoint = point + new Double3(index * .013d, -index * .017d, index * .019d);
            var sample = PlanetaryNaturalTerrainField.EvaluateBodyPoint(samplePoint, CellSize, 1d, Identity);
            checksum += sample.Height + sample.BodyGradient.X + sample.BodyGradient.Y + sample.BodyGradient.Z;
        }
        var allocations = GC.GetAllocatedBytesForCurrentThread() - before;
        var p2a = double.MaxValue; var generation3 = double.MaxValue;
        for (var repeat = 0; repeat < 4; repeat++)
        {
            p2a = Math.Min(p2a, MeasureP2A(point, ref checksum));
            generation3 = Math.Min(generation3, MeasureGeneration3(point, ref checksum));
        }
        var ratio = p2a / generation3;
        Require(allocations == 0, $"P2A full value+gradient query allocates zero bytes: {allocations}");
        Require(ratio <= 1.25d, $"P2A CPU cost is <=1.25x corresponding generation-3 three-carrier workload: {ratio:R}");
        return new(p2a, generation3, ratio, allocations, checksum);
    }

    private static double MeasureP2A(in Double3 origin, ref double checksum)
    {
        const int iterations = 300_000; var start = Stopwatch.GetTimestamp(); var sum = 0d;
        for (var index = 0; index < iterations; index++)
        {
            var point = origin + new Double3(index * .013d, -index * .017d, index * .019d);
            var sample = PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, CellSize, 1d, Identity);
            sum += sample.Height + sample.BodyGradient.X + sample.BodyGradient.Y + sample.BodyGradient.Z;
        }
        var elapsed = Stopwatch.GetElapsedTime(start).TotalNanoseconds / iterations; checksum += sum; return elapsed;
    }

    private static double MeasureGeneration3(in Double3 origin, ref double checksum)
    {
        const int iterations = 300_000; var start = Stopwatch.GetTimestamp(); var sum = 0d;
        for (var index = 0; index < iterations; index++)
        {
            var point = origin + new Double3(index * .013d, -index * .017d, index * .019d);
            var sample = Generation3Carrier(point); sum += sample.Height + sample.BodyGradient.X + sample.BodyGradient.Y + sample.BodyGradient.Z;
        }
        var elapsed = Stopwatch.GetElapsedTime(start).TotalNanoseconds / iterations; checksum += sum; return elapsed;
    }

    private static PlanetaryNaturalTerrainFieldSample Generation3Carrier(in Double3 point)
    {
        var a = WarpedBand(point, AxisC, 32d, .53d, .62d, WarpA, 210d, .83d, 1.71d, AxisB, 73d, .31d, -.37d);
        var b = WarpedBand(point, AxisA, 7d, -1.13d, .27d, AxisB, 53d, .67d, -.19d, WarpB, 19d, .24d, 2.03d);
        var c = WarpedBand(point, AxisB, 1.4d, 2.31d, .11d, AxisC, 17d, .49d, .61d, AxisA, 5d, .19d, -2.27d);
        return new(a.Height + b.Height + c.Height, a.BodyGradient + b.BodyGradient + c.BodyGradient);
    }

    private static PlanetaryNaturalTerrainFieldSample WarpedBand(in Double3 point, in Double3 carrierAxis,
        double wavelength, double phase, double amplitude, in Double3 warpAxisA, double warpWavelengthA,
        double warpStrengthA, double warpPhaseA, in Double3 warpAxisB, double warpWavelengthB,
        double warpStrengthB, double warpPhaseB)
    {
        var carrier = WrappedPhase(Double3.Dot(point, carrierAxis), wavelength, phase);
        var warpA = WrappedPhase(Double3.Dot(point, warpAxisA), warpWavelengthA, warpPhaseA);
        var warpB = WrappedPhase(Double3.Dot(point, warpAxisB), warpWavelengthB, warpPhaseB);
        var angle = carrier + warpStrengthA * Math.Sin(warpA) + warpStrengthB * Math.Sin(warpB);
        var phaseGradient = carrierAxis * (Math.Tau / wavelength) +
            warpAxisA * (warpStrengthA * Math.Tau / warpWavelengthA * Math.Cos(warpA)) +
            warpAxisB * (warpStrengthB * Math.Tau / warpWavelengthB * Math.Cos(warpB));
        return new(amplitude * Math.Sin(angle), phaseGradient * (amplitude * Math.Cos(angle)));
    }

    private static double WrappedPhase(double coordinate, double wavelength, double phase)
    {
        var cycles = coordinate / wavelength; cycles -= Math.Floor(cycles);
        return cycles * Math.Tau + phase;
    }

    private static GpuParityResult VerifyGpuGlslParity()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out var terrainPath, out var terrainError), $"P2A parity terrain-v5 asset: {terrainError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out var localError), $"P2A parity local-v2 asset: {localError}");
        var oraclePath = Path.Combine(root, "assets", "earth", "runtime", "earth_elevation_8192x4096.r16");
        var shaderPath = Path.Combine(root, "build", "native-ninja", "shaders", "planetary_natural_terrain_field_query.comp.spv");
        Require(File.Exists(oraclePath) && File.Exists(shaderPath), "P2A proof compute shader and executor assets exist");
        var cases = new List<(Double3 Point, double Cell, double Amplitude, PlanetaryNaturalTerrainFieldIdentity Identity)>();
        var points = new[]
        {
            new Double3(17.25d,29.5d,-83.75d), new Double3(-32d,-64d,-96d), new Double3(-32.000001d,63.999999d,128.5d),
            new Double3(6_500_000_000.25d,-5_800_000_000.75d,4_423_456_789.5d),
            new Double3(PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres,0d,0d),
            new Double3(1d,1d,0d).Normalized()*PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres,
            new Double3(1d,1d,1d).Normalized()*PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres,
            Double3.UnitY*PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres,
            -Double3.UnitY*PlanetaryNaturalTerrainField.EarthReferenceRadiusMetres
        };
        var seeds = new[] { 0x12345678u, 0x89ABCDEFu, 0x31415926u, 0xC001D00Du };
        for (var seedIndex = 0; seedIndex < seeds.Length; seedIndex++) foreach (var point in points)
            cases.Add((point, seedIndex % 2 == 0 ? 32d : 137d, 1.25d + seedIndex,
                Identity with { Seed = seeds[seedIndex], OctaveId = (uint)seedIndex }));
        cases.Add((new(6_500_000_000.25d,-5_800_000_000.75d,4_423_456_789.5d),1d,3.5d,
            Identity with { Seed=0xDEADBEEFu,OctaveId=19u }));
        var queries = new NativePlanetaryHeightQuery[cases.Count]; var reconstructed = new Double3[cases.Count];
        for (var index = 0; index < cases.Count; index++)
        {
            var encoded = EncodedPosition.Encode(cases[index].Point); reconstructed[index] = encoded.Reconstruct();
            var identity = cases[index].Identity;
            queries[index] = new NativePlanetaryHeightQuery
            {
                AnchorHighX=encoded.HighX,AnchorHighY=encoded.HighY,AnchorHighZ=encoded.HighZ,
                AnchorLowX=encoded.LowX,AnchorLowY=encoded.LowY,AnchorLowZ=encoded.LowZ,
                OracleU=cases[index].Cell,OracleV=cases[index].Amplitude,
                BodyIdLow=(uint)identity.BodyId,BodyIdHigh=(uint)(identity.BodyId>>32),
                TerrainVersion=(uint)identity.PhysicalFieldGeneration,AnchoredTier=(uint)(identity.PhysicalFieldGeneration>>32),
                TopologyVersion=identity.FamilyId,SourcePolicy=identity.OctaveId,Reserved0=identity.Seed,Reserved1=PlanetaryNaturalTerrainField.HashVersion
            };
        }
        var results = InvokeGpu(queries, oraclePath, terrainPath, localPath, shaderPath, out var metrics);
        var maxValue = 0d; var maxGradient = 0d; var maxSelection = 0d;
        for (var index = 0; index < cases.Count; index++)
        {
            var current = cases[index]; var cpu = PlanetaryNaturalTerrainField.EvaluateBodyPoint(reconstructed[index], current.Cell, current.Amplitude, current.Identity);
            var domain = PlanetaryNaturalTerrainField.ReduceBodyPoint(reconstructed[index], current.Cell);
            var hash = PlanetaryNaturalTerrainField.HashCell(current.Identity, domain.Cell);
            var selected = PlanetaryNaturalTerrainField.SelectGradient(hash); var result = results[index];
            Require(result.Valid == 1 && result.ResultTerrainVersion == PlanetaryNaturalTerrainField.HashVersion,
                $"P2A GPU result valid {index}");
            Require(Signed64(result.GlobalFace,result.GlobalLevel)==domain.Cell.X&&Signed64(result.GlobalX,result.GlobalY)==domain.Cell.Y&&
                Signed64(result.LocalAvailable,result.LocalLevel)==domain.Cell.Z&&(uint)result.FaceV==hash&&result.Reserved==hash,
                $"P2A GPU signed cell/hash parity {index}");
            Require(result.LocalX==current.Identity.FamilyId&&result.LocalY==current.Identity.OctaveId&&
                result.SourceHasLocal==current.Identity.Seed&&result.ResultTerrainVersion==PlanetaryNaturalTerrainField.HashVersion,
                $"P2A GPU identity parity {index}");
            maxValue = Math.Max(maxValue, Math.Abs(result.FaceU - cpu.Height));
            maxGradient = Math.Max(maxGradient, Length(new Double3(result.OracleElevationMetres,
                result.TerrainV5ElevationMetres,result.LocalResidualMetres)-cpu.BodyGradient));
            maxSelection = Math.Max(maxSelection, Length(new Double3(result.BaseHeightMetres,
                result.ModifierHeightMetres,result.TiledModifierHeightMetres)-selected));
        }
        Require(maxValue <= 2e-12d && maxGradient <= 2e-12d && maxSelection <= 2e-15d && metrics.ValidationErrors == 0,
            $"CPU/compiled GLSL parity: value={maxValue:R}; gradient={maxGradient:R}; selection={maxSelection:R}; validation={metrics.ValidationErrors}");
        return new(cases.Count, maxValue, maxGradient, maxSelection, metrics.GpuMilliseconds);
    }

    private static NativePlanetaryHeightResult[] InvokeGpu(NativePlanetaryHeightQuery[] queries, string oraclePath,
        string terrainPath, string localPath, string shaderPath, out NativePlanetaryHeightQueryMetrics metrics)
    {
        var results = new NativePlanetaryHeightResult[queries.Length];
        var oracle = Encoding.UTF8.GetBytes(oraclePath + '\0'); var terrain = Encoding.UTF8.GetBytes(terrainPath + '\0');
        var local = Encoding.UTF8.GetBytes(localPath + '\0'); var shader = Encoding.UTF8.GetBytes(shaderPath + '\0');
        var value = new NativePlanetaryHeightQueryMetrics { Size=(uint)Marshal.SizeOf<NativePlanetaryHeightQueryMetrics>(),Version=1 };
        fixed (NativePlanetaryHeightQuery* queryPointer=queries) fixed (NativePlanetaryHeightResult* resultPointer=results)
        fixed (byte* oraclePointer=oracle) fixed (byte* terrainPointer=terrain) fixed (byte* localPointer=local) fixed (byte* shaderPointer=shader)
        {
            var assets = new NativePlanetaryHeightQueryAssets { Size=(uint)Marshal.SizeOf<NativePlanetaryHeightQueryAssets>(),Version=1,
                ElevationOraclePathUtf8=oraclePointer,ProductionTerrainPathUtf8=terrainPointer,LocalTerrainPathUtf8=localPointer,ComputeShaderPathUtf8=shaderPointer };
            Require(NativeRuntime.QueryPlanetaryPhysicalHeights(queryPointer,(uint)queries.Length,resultPointer,&assets,&value)==NativeResult.Success,
                "P2A proof-only Vulkan query succeeds");
        }
        metrics=value;return results;
    }

    private static void VerifyIsolation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var shaderRoot = Path.Combine(root,"native","NovaCore.Native","shaders");
        var includes = Directory.EnumerateFiles(shaderRoot,"*",SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith("planetary_natural_terrain_field.glsl",StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("planetary_natural_terrain_field_query.comp",StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("planetary_natural_terrain_families.glsl",StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("planetary_natural_terrain_families_query.comp",StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("planetary_natural_terrain_field.glsl",StringComparison.Ordinal)).ToArray();
        Require(includes.Length==0,"only the isolated P2A/P2B proof shaders include the P2A field");
        var physical = File.ReadAllText(Path.Combine(root,"src","NovaCore.Graphics","PlanetaryPhysicalSurface.cs"));
        Require(!physical.Contains("PlanetaryNaturalTerrainField",StringComparison.Ordinal),"generation 3 production CPU authority does not route through P2A");
    }

    private static double Length(in Double3 value) => Math.Sqrt(value.LengthSquared);
    private static long Signed64(uint low,uint high)=>unchecked((long)((ulong)low|((ulong)high<<32)));
    private static bool Near(double actual,double expected,double tolerance) => Math.Abs(actual-expected)<=tolerance;
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    private readonly record struct StructureResult(double P2ADirectional,double Generation3Directional,double P2ACrossGrid,double Generation3CrossGrid,double P2AAutocorrelation,double Generation3Autocorrelation,double P2ARepeated,double Generation3Repeated);
    private readonly record struct PerformanceResult(double P2ANanoseconds,double Generation3Nanoseconds,double Ratio,long Allocations,double Checksum);
    private readonly record struct GpuParityResult(int Samples,double Value,double Gradient,double Selection,double GpuMilliseconds);
}
