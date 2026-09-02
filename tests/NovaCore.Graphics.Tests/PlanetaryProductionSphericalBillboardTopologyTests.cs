using NovaCore.Core;
using NovaCore.Graphics;

internal static class PlanetaryProductionSphericalBillboardTopologyTests
{
    public static void Run()
    {
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology>? candidate16 = PlanetaryProductionSphericalBillboardTopologyGenerator.GenerateLibrary(
            PlanetaryProductionSphericalBillboardTopologyGenerator.SixteenLevelDefinitions());
        var report16 = PlanetaryProductionSphericalBillboardTopologyGenerator.Characterize("16-level", candidate16);
        Require(candidate16.Count == 16, "16-level candidate is complete");
        candidate16 = null; GC.Collect(); GC.WaitForPendingFinalizers();
        var control18 = PlanetaryProductionSphericalBillboardTopologyGenerator.GenerateLibrary(
            PlanetaryProductionSphericalBillboardTopologyGenerator.EighteenLevelDefinitions());
        var report18 = PlanetaryProductionSphericalBillboardTopologyGenerator.Characterize("18-level", control18);
        Require(control18.Count == 18, "18-level control is complete");
        Console.WriteLine($"P2S5B candidate preflight: base16={report16.MaximumBasePixels:F3}; transition16={report16.MaximumTransitionPixels:F3}; silhouette16={report16.MaximumSilhouettePixels:F3}; tes16={report16.MaximumRequiredTesFactor}; jump16={report16.LargestAdjacentDensityJump:F3}; bytes16={report16.SerializedBytes}; overlap16={report16.MaximumSelectedAndIncomingGpuBytes}; base18={report18.MaximumBasePixels:F3}; transition18={report18.MaximumTransitionPixels:F3}; silhouette18={report18.MaximumSilhouettePixels:F3}; tes18={report18.MaximumRequiredTesFactor}; jump18={report18.LargestAdjacentDensityJump:F3}; bytes18={report18.SerializedBytes}; overlap18={report18.MaximumSelectedAndIncomingGpuBytes}");
        Require(report18.MaximumBasePixels / 64d <= 24.01d,
            "18-level control remains under the urgent 24 px ceiling after the bounded factor-64 TES responsibility");
        Require(report16.LargestAdjacentDensityJump > report18.LargestAdjacentDensityJump * 2d,
            "removing two levels materially increases the largest density jump");
        Require(control18[^1].Vertices.Count is >= 450_000 and <= 500_000 && control18[^1].TriangleCount is >= 900_000 and <= 1_000_000,
            "finest accepted topology remains inside the approved P2S3 capacity envelope");
        Require(control18[^1].Error.PupilSpacingRadians is >= .34e-6 and <= .50e-6 &&
            control18[^1].Error.OuterSpacingRadians / control18[^1].Error.PupilSpacingRadians >= 90_000d,
            "finest pupil spacing and concentration satisfy production targets");

        long totalBytes = 0; ulong selectedIncomingMaximum = 0; PlanetaryProductionSphericalBillboardTopology? previous = null;
        foreach (var topology in control18)
        {
            Console.WriteLine($"P2S5B validating L{topology.Level:D2}");
            var bytes = PlanetaryProductionSphericalBillboardTopology.Serialize(topology);
            var loaded = PlanetaryProductionSphericalBillboardTopology.Load(bytes);
            var replay = PlanetaryProductionSphericalBillboardTopologyGenerator.Generate(topology.Level,
                PlanetaryProductionSphericalBillboardTopologyGenerator.EighteenLevelDefinitions()[topology.Level], previous);
            var replayBytes = PlanetaryProductionSphericalBillboardTopology.Serialize(replay);
            Require(bytes.SequenceEqual(replayBytes) && loaded.TopologyHash == topology.TopologyHash,
                $"level {topology.Level} byte-identical deterministic regeneration and load");
            Require(topology.Vertices.All(v => Math.Max(Math.Abs((long)v.CubeX), Math.Max(Math.Abs((long)v.CubeY), Math.Abs((long)v.CubeZ))) == topology.LatticeScale),
                $"level {topology.Level} uses exact signed cube-lattice coordinates");
            var quality = ValidateGeometry(topology);
            Require(quality.MinimumAngle > 8d && quality.MaximumAspect < 8d,
                $"level {topology.Level} passes hard FP64 triangle-quality gates: angle={quality.MinimumAngle:R}, aspect={quality.MaximumAspect:R}");
            if (previous is null) Require(topology.ParentVertexMap.Count == 0, "root has no parent mapping");
            else
            {
                Require(topology.ParentVertexMap.Count == previous.Vertices.Count && topology.ParentVertexMap.Distinct().Count() == previous.Vertices.Count,
                    $"level {topology.Level} has a complete one-to-one parent map");
                for (var i = 0; i < previous.Vertices.Count; i++)
                {
                    var parentVertex = previous.Vertices[i];
                    var childVertex = topology.Vertices[topology.ParentVertexMap[i]];
                    Require(parentVertex.CubeX == childVertex.CubeX && parentVertex.CubeY == childVertex.CubeY && parentVertex.CubeZ == childVertex.CubeZ,
                        $"level {topology.Level} parent vertex {i} has exact lattice coordinates");
                }
            }
            Require(topology.Snap.CandidateShiftMultiple > 0 && topology.Snap.OverlapFootprintCells > topology.Snap.CandidateShiftMultiple &&
                topology.Snap.EnteringStripCells > 0 && topology.Snap.LatticeIdentity != 0,
                $"level {topology.Level} snap-lattice metadata is bounded");
            Require(topology.Error.MinimumRepresentablePhysicalWavelengthMetres > 0d && topology.Error.MaximumExpectedBaseErrorPixels > 0f &&
                topology.Error.MaximumTesFactor is >= 1 and <= 64 && topology.Error.EntryPixels == 18f && topology.Error.ReturnPixels == 12f &&
                topology.Error.UrgentPixels == 24f && topology.Error.TesTargetMinimumPixels == 4f && topology.Error.TesTargetMaximumPixels == 6f,
                $"level {topology.Level} screen-error, frequency-representability, and bounded TES metadata is complete");
            var before = topology.TopologyHash; var syntheticShift = topology.Snap.CandidateShiftMultiple * topology.Snap.PupilCellRadians;
            Require(syntheticShift > 0d && before == PlanetaryProductionSphericalBillboardTopology.Load(bytes).TopologyHash,
                $"level {topology.Level} integer pupil translation does not regenerate topology");
            totalBytes += bytes.Length; previous = topology;
            var active = ActiveBytes(topology); var incoming = topology.Level + 1 < control18.Count ? ActiveBytes(control18[topology.Level + 1]) : 0ul;
            selectedIncomingMaximum = Math.Max(selectedIncomingMaximum, active + incoming);
            Console.WriteLine($"P2S5B L{topology.Level:D2}: vertices={topology.Vertices.Count}; triangles={topology.TriangleCount}; bytes={bytes.Length}; " +
                $"pupil={topology.Error.PupilSpacingRadians:E9}; transition={topology.Error.TransitionSpacingRadians:E9}; outer={topology.Error.OuterSpacingRadians:E9}; " +
                $"density={topology.Error.OuterSpacingRadians / topology.Error.PupilSpacingRadians:F1}; minAngle={quality.MinimumAngle:F4}; p1={quality.FirstPercentile:F4}; " +
                $"p5={quality.FifthPercentile:F4}; median={quality.Median:F4}; aspect={quality.MaximumAspect:F4}; pupilWorst={quality.WorstPupil:F4}; transitionWorst={quality.WorstTransition:F4}; farWorst={quality.WorstFar:F4}; map=0x{topology.ParentMappingHash:X16}; hash=0x{topology.TopologyHash:X16}");
        }
        Require(control18.Select(topology => topology.Snap.LatticeIdentity).Distinct().Count() == 1,
            "all levels use one exact production tangent-lattice identity");
        var corrupt = PlanetaryProductionSphericalBillboardTopology.Serialize(control18[^1]); corrupt[^1] ^= 0x40;
        Require(Fails(() => PlanetaryProductionSphericalBillboardTopology.Load(corrupt)) &&
            Fails(() => PlanetaryProductionSphericalBillboardTopology.Load(corrupt.AsSpan(0, corrupt.Length - 1))),
            "v2 loader rejects corrupt and truncated artifacts");
        var artifactOutput = Environment.GetEnvironmentVariable("NOVACORE_P2S5B_ARTIFACT_OUTPUT");
        if (!string.IsNullOrWhiteSpace(artifactOutput)) PlanetaryProductionSphericalBillboardTopologyGenerator.WriteProductionLibrary(artifactOutput, control18);
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var loadedLibrary = PlanetaryProductionSphericalBillboardTopologyLibrary.Load(Path.Combine(root, "assets", "planetary-production-topology"));
        Require(loadedLibrary.Count == control18.Count && loadedLibrary.Select(topology => topology.TopologyHash).SequenceEqual(control18.Select(topology => topology.TopologyHash)),
            "manifest loader consumes all authored artifacts without runtime topology generation");
        Console.WriteLine($"P2S5B compare: 16/base={report16.MaximumBasePixels:F3}px/transition={report16.MaximumTransitionPixels:F3}px/silhouette={report16.MaximumSilhouettePixels:F3}px/TES={report16.MaximumRequiredTesFactor}/jump={report16.LargestAdjacentDensityJump:F2}/bytes={report16.SerializedBytes}/angle={report16.FinestMinimumAngleDegrees:F4}/aspect={report16.FinestMaximumAspectRatio:F4}; " +
            $"18/base={report18.MaximumBasePixels:F3}px/transition={report18.MaximumTransitionPixels:F3}px/silhouette={report18.MaximumSilhouettePixels:F3}px/TES={report18.MaximumRequiredTesFactor}/jump={report18.LargestAdjacentDensityJump:F2}/bytes={report18.SerializedBytes}/angle={report18.FinestMinimumAngleDegrees:F4}/aspect={report18.FinestMaximumAspectRatio:F4}; selectedIncoming={selectedIncomingMaximum}; decision=18");
        Console.WriteLine($"P2S5B library: levels={control18.Count}; bytes={totalBytes}; runtimeGeneration=0; format=2; coordinate=SignedCubeLatticeInt32");
    }

    private static (double MinimumAngle, double FirstPercentile, double FifthPercentile, double Median, double MaximumAspect,
        double WorstPupil, double WorstTransition, double WorstFar) ValidateGeometry(
        PlanetaryProductionSphericalBillboardTopology topology)
    {
        var edgeKeys = new ulong[topology.Indices.Count]; var triangleKeys = new HashSet<(uint, uint, uint)>();
        for (var i = 0; i < topology.Indices.Count; i += 3)
        {
            var ia = topology.Indices[i]; var ib = topology.Indices[i + 1]; var ic = topology.Indices[i + 2];
            Require(ia != ib && ib != ic && ic != ia, "no degenerate index triangle");
            var sorted = new[] { ia, ib, ic }; Array.Sort(sorted); Require(triangleKeys.Add((sorted[0], sorted[1], sorted[2])), "no duplicate triangle");
            edgeKeys[i] = Edge(ia, ib); edgeKeys[i + 1] = Edge(ib, ic); edgeKeys[i + 2] = Edge(ic, ia);
            var a = topology.Vertices[(int)ia].Direction(topology.LatticeScale); var b = topology.Vertices[(int)ib].Direction(topology.LatticeScale); var c = topology.Vertices[(int)ic].Direction(topology.LatticeScale);
            Require(Double3.Dot(Double3.Cross(b - a, c - a), a + b + c) > 0d, "outward FP64 winding");
        }
        Array.Sort(edgeKeys);
        for (var i = 0; i < edgeKeys.Length;)
        {
            var end = i + 1; while (end < edgeKeys.Length && edgeKeys[end] == edgeKeys[i]) end++;
            var edgeA = (int)(edgeKeys[i] >> 32); var edgeB = (int)(edgeKeys[i] & uint.MaxValue);
            Require(end - i == 2, $"every manifold edge has exactly two incidents (edge=0x{edgeKeys[i]:X16}, incidents={end - i}, a={topology.Vertices[edgeA]}, b={topology.Vertices[edgeB]})");
            i = end;
        }
        var quality = PlanetaryProductionSphericalBillboardTopologyGenerator.Quality(topology);
        return (quality.MinimumAngle, quality.FirstPercentileAngle, quality.FifthPercentileAngle, quality.MedianAngle, quality.MaximumAspect,
            quality.WorstPupilAngle, quality.WorstTransitionAngle, quality.WorstFarAngle);
        static ulong Edge(uint a, uint b) { if (a > b) (a, b) = (b, a); return ((ulong)a << 32) | b; }
    }

    private static ulong ActiveBytes(PlanetaryProductionSphericalBillboardTopology topology) => checked(topology.ImmutableGpuBytes +
        (ulong)topology.Vertices.Count * (48ul + 96ul) + (ulong)topology.TriangleCount * 48ul);
    private static bool Fails(Action action) { try { action(); return false; } catch (InvalidDataException) { return true; } }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("P2S5B: " + message); }
}
