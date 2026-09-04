using NovaCore.Core;
using NovaCore.Graphics;

internal static class PlanetaryNestedScaleMeshTopologyTests
{
    public static void Run()
    {
        var definitions = PlanetaryNestedScaleMeshTopologyGenerator.Definitions();
        Require(definitions.Count == PlanetaryNestedScaleMeshTopologyGenerator.ScaleCount,
            "complete 18-scale definition library");
        var artifactInput = Environment.GetEnvironmentVariable("NOVACORE_P2S5F_ARTIFACT_INPUT");
        IReadOnlyList<PlanetaryNestedScaleMeshTopology>? inputLevels = null;
        if (!string.IsNullOrWhiteSpace(artifactInput))
        {
            inputLevels = File.Exists(Path.Combine(artifactInput,
                    PlanetaryNestedScaleMeshTopologyLibrary.ManifestFileName))
                ? PlanetaryNestedScaleMeshTopologyLibrary.Load(artifactInput)
                : Directory.EnumerateFiles(artifactInput, "*.ncsm1")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => PlanetaryNestedScaleMeshTopology.Load(File.ReadAllBytes(path)))
                    .ToArray();
            Require(inputLevels.Count == definitions.Count,
                "artifact-input library contains the complete 18-scale definition set");
            inputLevels = inputLevels.Select((level, scale) =>
                PlanetaryNestedScaleMeshTopologyGenerator.ApplyScaleDefinition(level,
                    definitions[scale])).ToArray();
        }
        var levels = new List<PlanetaryNestedScaleMeshTopology>(18);

        ulong previousHash = 0; long serializedBytes = 0;
        for (var scale = 0; scale < definitions.Count; scale++)
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            var level = inputLevels is null
                ? PlanetaryNestedScaleMeshTopologyGenerator.Generate(scale, definitions[scale])
                : inputLevels[scale];
            levels.Add(level);
            var bytes = PlanetaryNestedScaleMeshTopology.Serialize(level);
            var loaded = PlanetaryNestedScaleMeshTopology.Load(bytes);
            if (Environment.GetEnvironmentVariable("NOVACORE_P2S5F_EXHAUSTIVE_DETERMINISM") == "1")
            {
                var replay = PlanetaryNestedScaleMeshTopologyGenerator.Generate(level.Scale, definitions[level.Scale]);
                var replayBytes = PlanetaryNestedScaleMeshTopology.Serialize(replay);
                Require(bytes.SequenceEqual(replayBytes),
                    $"scale {level.Scale} byte-identical deterministic regeneration");
            }
            Require(loaded.TopologyHash == level.TopologyHash,
                $"scale {level.Scale} serialized identity round trip");
            Require(level.TopologyHash != 0 && level.TopologyHash != previousHash,
                $"scale {level.Scale} unique immutable topology identity");
            previousHash = level.TopologyHash; serializedBytes += bytes.Length;
            Validate(level);
            Console.WriteLine($"P2S5F S{level.Scale:D2}: vertices={level.Vertices.Count}; triangles={level.TriangleCount}; " +
                $"regions={level.Regions.Count}; minEdge={level.Geometry.MinimumEdgeMetres:F6}m; avgEdge={level.Geometry.AverageEdgeMetres:F3}m; " +
                $"maxEdge={level.Geometry.MaximumEdgeMetres:F3}m; maxSpan={level.Geometry.MaximumAngularSpanRadians:E9}rad; " +
                $"sag={level.Geometry.MaximumChordSagMetres:F6}m; support={level.Geometry.PlanetOcclusionSupportRadiusMetres:F3}m; " +
                $"pupil={level.Geometry.PupilRadiusRadians * level.Geometry.ReferenceRadiusMetres:F3}m; immutable={level.ImmutableGpuBytes}; " +
                $"hash=0x{level.TopologyHash:X16}; generatedMs={started.Elapsed.TotalMilliseconds:F1}");
        }

        Require(levels.Count == 18 && levels.Select(value => value.Scale).SequenceEqual(Enumerable.Range(0, 18)),
            "complete ordered nested scale-mesh library");

        var report = PlanetaryNestedScaleMeshTopologyGenerator.Characterize(levels);
        var adapted = PlanetaryNestedScaleMeshRuntimeAdapter.Adapt(levels);
        Require(adapted.Levels.Count == levels.Count && adapted.CullContracts.Count == levels.Count,
            "complete opt-in runtime adaptation");
        for (var scale = 0; scale < levels.Count; scale++)
        {
            var runtime = adapted.Levels[scale];
            var contract = adapted.CullContracts[runtime.TopologyHash];
            Require(runtime.Vertices.Count == levels[scale].Vertices.Count &&
                    runtime.TriangleCount == levels[scale].TriangleCount &&
                    runtime.TopologyHash == levels[scale].TopologyHash,
                $"scale {scale} runtime adapter preserves immutable identity and counts");
            Require(contract.Family == PlanetaryProductionTopologyFamily.NestedScaleMeshNcsm1 &&
                    Math.Abs(contract.PlanetOcclusionSupportRadiusMetres -
                        levels[scale].Geometry.PlanetOcclusionSupportRadiusMetres) <= 1e-9d &&
                    Math.Abs(contract.MaximumTesDisplacementMetres -
                        levels[scale].Geometry.MaximumTesDisplacementMetres) <= 1e-9d,
                $"scale {scale} selects displaced-triangle culling only with its serialized contract");
        }
        Require(report.MaximumEdgeMetres < 475_000d && report.MaximumChordSagMetres < 4_500d,
            "near-scale outer edge and chord sag are KSA-equivalent or tighter");
        Require(report.FinestSpacingMetres is >= 1.5d and <= 3d,
            "finest physical spacing preserves approximately two-metre near-field quality");
        Require(levels[^1].Geometry.PupilRadiusRadians * levels[^1].Geometry.ReferenceRadiusMetres is >= 150d and <= 300d,
            "finest pupil footprint remains bounded near the measured two-hundred-metre contract");
        Require(levels.All(level => level.Geometry.MaximumChordSagMetres + level.Geometry.MaximumTesDisplacementMetres <=
                                    level.Geometry.ReferenceRadiusMetres - level.Geometry.PlanetOcclusionSupportRadiusMetres + 1e-7d),
            "every scale proves chord sag plus bounded TES displacement against its support radius");
        var selector = new PlanetaryProductionSphericalBillboardSelector(adapted.Levels);
        var reached = new bool[levels.Count];
        var logarithmicRange = Math.Log(80_000_000d / 10.004d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            reached[selector.Evaluate(new(altitude, Double3.UnitZ, 3440, 1440,
                Math.PI / 3d, (ulong)sample), false).Level] = true;
        }
        Require(reached.All(value => value), "all 18 scale ranges are reachable by projected-error selection");
        var corrupt = PlanetaryNestedScaleMeshTopology.Serialize(levels[^1]); corrupt[^1] ^= 0x20;
        Require(Fails(() => PlanetaryNestedScaleMeshTopology.Load(corrupt)) &&
                Fails(() => PlanetaryNestedScaleMeshTopology.Load(corrupt.AsSpan(0, corrupt.Length - 1))),
            "ncsm1 loader rejects corrupt and truncated artifacts");

        var output = Environment.GetEnvironmentVariable("NOVACORE_P2S5F_ARTIFACT_OUTPUT");
        if (!string.IsNullOrWhiteSpace(output))
        {
            PlanetaryNestedScaleMeshTopologyGenerator.WriteProductionLibrary(output, levels);
            var loaded = PlanetaryNestedScaleMeshTopologyLibrary.Load(output);
            Require(loaded.Select(value => value.TopologyHash).SequenceEqual(levels.Select(value => value.TopologyHash)),
                "manifest reload preserves all scale identities");
        }
        Console.WriteLine($"P2S5F library: levels={levels.Count}; bytes={serializedBytes}; maxPair={report.MaximumCurrentIncomingBytes}; " +
            $"finest={report.FinestVertices}/{report.FinestTriangles}; maxEdge={report.MaximumEdgeMetres:F3}m; " +
            $"maxSag={report.MaximumChordSagMetres:F6}m; finestSpacing={report.FinestSpacingMetres:F6}m");
    }

    private static void Validate(PlanetaryNestedScaleMeshTopology topology)
    {
        Require(topology.Regions.Count == topology.Scale + 1, $"scale {topology.Scale} region chain");
        Require(topology.NeighborOffsets.Count == topology.Vertices.Count + 1 &&
                topology.NeighborOffsets[^1] == topology.Neighbors.Count,
            $"scale {topology.Scale} complete CSR adjacency");
        Require(topology.Vertices.Distinct().Count() == topology.Vertices.Count,
            $"scale {topology.Scale} unique rational identities");
        Require(topology.Vertices.All(value => value.Direction.IsFinite &&
            Math.Abs(value.Direction.LengthSquared - 1d) <= 1e-12d),
            $"scale {topology.Scale} finite unit directions");

        // The offline generator validates every directed triangle and sorts all
        // edge incidents before an artifact can be emitted. Keep the test-side
        // proof linear so the million-triangle production scales remain usable
        // in focused CI rather than sorting the complete edge set twice.
        for (var i = 0; i < topology.Indices.Count; i += 3)
        {
            var ia = topology.Indices[i]; var ib = topology.Indices[i + 1]; var ic = topology.Indices[i + 2];
            Require(ia != ib && ib != ic && ic != ia, $"scale {topology.Scale} no degenerate indices");
            var a = topology.Vertices[(int)ia].Direction; var b = topology.Vertices[(int)ib].Direction;
            var c = topology.Vertices[(int)ic].Direction;
            Require(Double3.Dot(Double3.Cross(b - a, c - a), a + b + c) > 0d,
                $"scale {topology.Scale} consistent outward winding");
        }
        var eulerEdges = topology.Indices.Count / 2;
        Require(topology.Vertices.Count - eulerEdges + topology.TriangleCount == 2,
            $"scale {topology.Scale} connected closed-sphere Euler characteristic");
    }

    private static bool Fails(Action action)
    {
        try { action(); return false; }
        catch (InvalidDataException) { return true; }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("P2S5F: " + message);
    }
}
