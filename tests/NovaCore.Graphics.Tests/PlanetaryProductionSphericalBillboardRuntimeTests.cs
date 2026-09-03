using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class PlanetaryProductionSphericalBillboardRuntimeTests
{
    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var levels = PlanetaryProductionSphericalBillboardTopologyLibrary.Load(
            Path.Combine(root, "assets", "planetary-production-topology"));
        Require(levels.Count == 18, "complete production topology library loaded");
        Require(PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres ==
                PlanetaryPhysicalSurface.EarthReferenceRadiusMetres,
            "production billboard preparation uses the canonical physical Earth radius");
        Require(Marshal.SizeOf<NativeSphericalBillboardPhysicalVertex>() == 64,
            "physical billboard vertex matches the std430 dvec4 array stride");
        ProveV2Gpu(root, levels);
        ProveNativeSelectedIncomingResidency(root, levels);
        ProveSelector(levels);
        ProvePupilAndSnap(levels);
        ProveReuse(levels);
        ProveActualIncrementalReuse(levels);
        ProveActualPhysicalSnapPreparation(root, levels);
        ProveMovingCoordinator(levels);
        ProveMovementCampaign(levels);
        ProvePublication(levels);
        ProveTes(levels[^1]);
        ProveConservativeNearSurfaceHorizonCoverage(levels[14]);
        DiagnoseBodyFixedLateralContinuity(levels);
        ProveIsolation(root);
    }

    private static void ProveNativeSelectedIncomingResidency(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        using var session = new PlanetarySphericalBillboardGpuProofSession(
            Path.Combine(root, "build", "native-ninja", "shaders"));
        var current = levels[16];
        var incoming = levels[17];
        var currentUpload = session.UploadProduction(current);
        Require(currentUpload.ActiveLevel == 16 && currentUpload.IncomingTopologyBytes == 0,
            "L16 begins as the sole native GPU owner");
        var currentFrame = session.RunProductionFrame(current, 0);
        Require(currentFrame.TopologyHash == current.TopologyHash &&
            currentFrame.ZeroOwnerFrames == 0 && currentFrame.OverlapOwnerFrames == 0,
            "current generation remains drawable before replacement");

        var staged = session.UploadProduction(incoming, stageAsIncoming: true);
        Require(staged.ActiveLevel == 16 && staged.TopologyHash == current.TopologyHash &&
            staged.IncomingLevel == 17 && staged.IncomingTopologyHash == incoming.TopologyHash &&
            staged.IncomingReadiness == 1 &&
            staged.SelectedIncomingBytes == current.ImmutableGpuBytes + incoming.ImmutableGpuBytes,
            "native runtime retains current plus at most one staged incoming topology");
        var retained = session.RunProductionFrame(current, 1);
        Require(retained.ActiveLevel == 16 && retained.IncomingLevel == 17 &&
            retained.TopologyHash == current.TopologyHash && retained.PublicationCount == 0,
            "staging never changes the authoritative draw generation");

        var published = session.RunProductionFrame(incoming, 2, publishIncoming: true);
        Require(published.ActiveLevel == 17 && published.TopologyHash == incoming.TopologyHash &&
            published.IncomingTopologyBytes == 0 && published.PublicationCount == 1 &&
            published.DeferredRetirementCount == 1 && published.ZeroOwnerFrames == 0 &&
            published.OverlapOwnerFrames == 0 && published.StaleGenerationDraws == 0 &&
            published.ValidationErrors == 0 && published.InvalidCommands == 0,
            "fence-boundary publication switches atomically to the incoming topology");
        Console.WriteLine($"P2S5C native residency: current=L16; incoming=L17; " +
            $"overlapBytes={staged.SelectedIncomingBytes}; publications={published.PublicationCount}; " +
            $"deferredRetirements={published.DeferredRetirementCount}; owners=1");
    }

    private static void ProveV2Gpu(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        using var session = new PlanetarySphericalBillboardGpuProofSession(
            Path.Combine(root, "build", "native-ninja", "shaders"));
        uint frame = 0;
        foreach (var topology in new[] { levels[0], levels[16], levels[17] })
        {
            var upload = session.UploadProduction(topology);
            Require(upload.TopologyHash == topology.TopologyHash &&
                upload.ActiveTopologyBytes == topology.ImmutableGpuBytes,
                $"L{topology.Level:D2} uploads exact v2 identity");
            var rendered = session.RunProductionFrame(topology, frame++);
            Require(rendered.RuntimeTopologyGenerationCount == 0 &&
                rendered.ValidationErrors == 0 && rendered.InvalidCommands == 0 &&
                rendered.OverflowCount == 0 && rendered.VisibleTriangles > 0,
                $"L{topology.Level:D2} v2 GPU path is validation-clean");
            Require(rendered.DirectionDecodeMaximumErrorRadians <= 2e-6,
                $"L{topology.Level:D2} GPU signed-lattice decode matches FP64 direction authority");
            Console.WriteLine($"P2S5C v2 GPU L{topology.Level:D2}: hash=0x{rendered.TopologyHash:X16}; " +
                $"vertices={rendered.BaseVertexCount}; triangles={rendered.BaseTriangleCount}; " +
                $"visible={rendered.VisibleTriangles}; directionMax={rendered.DirectionDecodeMaximumErrorRadians:E9}rad; " +
                $"uploadMs={upload.TopologyUploadMilliseconds:F6}; gpuMs={rendered.GpuTotalMilliseconds:F6}");
        }
    }

    private static void ProveSelector(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var altitudes = new[] { 80_000_000d, 35_786_000d, 3_000_000d, 700_000d, 100_000d,
            10_000d, 1_000d, 250d, 100d, 10d };
        var selected = new List<int>();
        foreach (var altitude in altitudes)
        {
            var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
            selected.Add(selector.Evaluate(View(altitude, 0), false).Level);
        }
        Console.WriteLine($"P2S5C selector probe: {string.Join(';', altitudes.Zip(selected).Select(pair => $"{pair.First:F0}m=L{pair.Second}"))}");
        Require(selected.Zip(selected.Skip(1)).All(pair => pair.Second >= pair.First),
            "descent selects monotonically finer levels");

        var stateful = new PlanetaryProductionSphericalBillboardSelector(levels);
        var first = stateful.Evaluate(View(3_000_000d, 0), false);
        stateful.CommitPublication(first.Level);
        var targetAltitude = altitudes.First(altitude =>
        {
            var probe = new PlanetaryProductionSphericalBillboardSelector(levels);
            return probe.Evaluate(View(altitude, 0), false).Level > first.Level;
        });
        var one = stateful.Evaluate(View(targetAltitude, 1), false);
        if (!one.Urgent) Require(one.Level == first.Level, "non-urgent transition observes first dwell frame");
        var two = stateful.Evaluate(View(targetAltitude, 2), false);
        Require(two.Level >= first.Level, "second completed frame permits inward selection");
        if (stateful.InFlightLevel >= 0)
        {
            var locked = stateful.Evaluate(View(80_000_000d, 3), true);
            Require(locked.Level == stateful.InFlightLevel, "in-flight publication cannot reverse from one noisy sample");
            stateful.CommitPublication(stateful.InFlightLevel);
        }
        Console.WriteLine($"P2S5C selector: altitudes=[{string.Join(',', altitudes.Select(a => a.ToString("F0")))}]; " +
            $"levels=[{string.Join(',', selected)}]; monotonic=true");
    }

    private static void ProvePupilAndSnap(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var topology = levels[^1];
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
        Require(pupil.IsValid && Double3.Dot(pupil.PivotDirection, Double3.UnitZ) > 1d - 1e-15,
            "+Z pupil initially faces the camera/surface direction");
        var small = (Double3.UnitZ + pupil.Tangent.East *
            (topology.Snap.PupilCellRadians * (topology.Snap.CandidateShiftMultiple - 1))).Normalized();
        var retained = PlanetaryProductionBillboardPupil.Resolve(pupil, small, topology);
        Require(retained == pupil, "sub-threshold motion retains the exact tangent frame");
        var moved = (Double3.UnitZ + pupil.Tangent.East *
            (topology.Snap.PupilCellRadians * (topology.Snap.CandidateShiftMultiple + 1))).Normalized();
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, moved, topology);
        Require(snapped.Generation == pupil.Generation + 1 &&
            snapped.SnapEastCells % topology.Snap.CandidateShiftMultiple == 0 &&
            snapped.SnapNorthCells % topology.Snap.CandidateShiftMultiple == 0,
            "recenter shifts by exact authored integer lattice cells");
        var identity = new Double3(.2, -.3, .9327379053088815).Normalized();
        var body = snapped.RotateCanonical(identity);
        Require(Math.Abs(body.LengthSquared - 1d) < 1e-12d,
            "pupil representation preserves canonical unit direction");
        var pole = PlanetaryProductionBillboardPupil.Resolve(snapped,
            new Double3(1e-9, 1d, -1e-9), topology);
        Require(pole.IsValid && Double3.Dot(pole.Tangent.East, snapped.Tangent.East) > -0.999999,
            "parallel transport remains finite and orientation-continuous at the pole");
    }

    private static void ProveReuse(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var cross = PlanetaryProductionSphericalBillboardReuse.CrossLevel(levels[16], levels[17]);
        Require(cross.ActiveSamples == 450_778 && cross.NewSamples == 34_636 &&
            cross.ReusedSamples == levels[16].Vertices.Count,
            "L16 to L17 prepares only 34,636 entering topology samples");
        var unchanged = PlanetaryProductionSphericalBillboardReuse.Snapped(levels[17], 0, 0);
        Require(unchanged.NewSamples == 0 && unchanged.ReusePercent == 100d,
            "stationary pupil reuses all canonical samples");
        var shifted = PlanetaryProductionSphericalBillboardReuse.Snapped(levels[17],
            levels[17].Snap.CandidateShiftMultiple, 0);
        Require(shifted.NewSamples > 0 && shifted.NewSamples < shifted.ActiveSamples,
            "integer recenter prepares an entering strip rather than the full topology");
        Console.WriteLine($"P2S5C reuse: L16-L17 active={cross.ActiveSamples}; reused={cross.ReusedSamples}; " +
            $"new={cross.NewSamples}; reuse={cross.ReusePercent:F3}%; snapNew={shifted.NewSamples}");
        foreach (var topology in levels)
        {
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
            ProveSnappedGeometry(topology, pupil);
        }
    }

    private static void ProveActualIncrementalReuse(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var cache = new PlanetaryProductionBillboardPhysicalCache();
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, levels[16]);
        var parent = PrepareSynthetic(levels[16], pupil, cache);
        var child = PrepareSynthetic(levels[17], pupil, cache);
        Require(child.Vertices.Length == 450_778 && child.ReusedSamples == 416_142 &&
            child.PreparedSamples == 34_636,
            "actual canonical cache realizes the authored L16 to L17 reuse mapping");
        cache.RetainOnly(child.Identities);
        var target = (pupil.PivotDirection + pupil.Tangent.East *
            (levels[17].Snap.PupilCellRadians * (levels[17].Snap.CandidateShiftMultiple + 1))).Normalized();
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, target, levels[17]);
        var moved = PrepareSynthetic(levels[17], snapped, cache);
        Require(snapped.Generation == pupil.Generation + 1 && moved.ReusedSamples > 0 &&
            moved.PreparedSamples > 0 && moved.PreparedSamples < moved.Vertices.Length,
            "actual snapped generation preserves canonical identities and prepares only entering samples");
        var inner = levels[17].Snap.OverlapFootprintCells *
            levels[17].Snap.PupilCellRadians * Math.Sqrt(2d);
        var outer = inner + 4d * levels[17].Snap.CandidateShiftMultiple *
            levels[17].Snap.PupilCellRadians;
        for (var i = 0; i < levels[17].Vertices.Count; i++)
        {
            var vertex = levels[17].Vertices[i];
            var radius = vertex.CubeZ == levels[17].LatticeScale
                ? Math.Sqrt((double)vertex.CubeX * vertex.CubeX +
                    (double)vertex.CubeY * vertex.CubeY) / levels[17].LatticeScale
                : double.PositiveInfinity;
            if (radius >= outer)
                Require(child.Identities[i] == moved.Identities[i],
                    "snap demand is confined to the pupil and its conforming transition annulus");
        }
        Require(moved.PreparedSamples < moved.Vertices.Length / 20,
            "ordinary finest-level snap reuses more than 95% of actual canonical samples");
        ProveSnappedGeometry(levels[17], snapped);
        var retained = child.Identities.Intersect(moved.Identities).First();
        var oldVertex = child.Vertices[child.Identities.ToList().IndexOf(retained)];
        var newVertex = moved.Vertices[moved.Identities.ToList().IndexOf(retained)];
        Require(oldVertex.BodyX == newVertex.BodyX && oldVertex.BodyY == newVertex.BodyY &&
            oldVertex.BodyZ == newVertex.BodyZ && oldVertex.PhysicalHeightMetres == newVertex.PhysicalHeightMetres &&
            oldVertex.NormalX == newVertex.NormalX && oldVertex.NormalY == newVertex.NormalY &&
            oldVertex.NormalZ == newVertex.NormalZ,
            "retained H(bodyDirection) samples remain bit-identical across integer snap");
        Console.WriteLine($"P2S5C2 actual reuse: L16-L17={child.ReusedSamples}/{child.Vertices.Length} " +
            $"({100d * child.ReusedSamples / child.Vertices.Length:F3}%); snap={moved.ReusedSamples}/{moved.Vertices.Length} " +
            $"({100d * moved.ReusedSamples / moved.Vertices.Length:F3}%); entering={moved.PreparedSamples}");
        GC.KeepAlive(parent);
    }

    private static void ProveSnappedGeometry(
        PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardPupil pupil)
    {
        var directions = topology.Vertices.Select(vertex =>
            pupil.ResolveCanonicalDirection(vertex, topology)).ToArray();
        var minimumOutward = double.PositiveInfinity;
        var minimumEdge = double.PositiveInfinity;
        var nonOutward = 0;
        for (var index = 0; index < topology.Indices.Count; index += 3)
        {
            var a = directions[topology.Indices[index]];
            var b = directions[topology.Indices[index + 1]];
            var c = directions[topology.Indices[index + 2]];
            var normal = Double3.Cross(b - a, c - a);
            var outward = Double3.Dot(normal, (a + b + c).Normalized());
            if (outward <= 0d) nonOutward++;
            minimumOutward = Math.Min(minimumOutward, outward);
            minimumEdge = Math.Min(minimumEdge, Math.Min((b - a).LengthSquared,
                Math.Min((c - b).LengthSquared, (a - c).LengthSquared)));
        }
        Console.WriteLine($"P2S5C2 snapped geometry: minOutward={minimumOutward:E9}; " +
            $"minEdgeSquared={minimumEdge:E9}; nonOutward={nonOutward}; triangles={topology.TriangleCount}");
        Require(minimumOutward > 0d && minimumEdge > 0d,
            "snapped pupil keeps every production triangle finite, nondegenerate, and outward");
    }

    private static void ProveActualPhysicalSnapPreparation(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var previous = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            ProveActualPhysicalSnapPreparationCore(root, levels);
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous); }
    }

    private static void ProveActualPhysicalSnapPreparationCore(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var topology = levels[8];
        var cache = new PlanetaryProductionBillboardPhysicalCache();
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
        var initialStart = Stopwatch.GetTimestamp();
        var initial = PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
            root, topology, pupil, cache, maximumParitySamples: 64);
        var initialCpu = Stopwatch.GetElapsedTime(initialStart).TotalMilliseconds;
        cache.RetainOnly(initial.Identities);
        var snapAngle = topology.Snap.PupilCellRadians *
            (topology.Snap.CandidateShiftMultiple + 1);
        var target = pupil.PivotDirection * Math.Cos(snapAngle) +
            pupil.Tangent.East * Math.Sin(snapAngle);
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, target, topology);
        var snapStart = Stopwatch.GetTimestamp();
        var moved = PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
            root, topology, snapped, cache, maximumParitySamples: 64);
        var snapCpu = Stopwatch.GetElapsedTime(snapStart).TotalMilliseconds;
        Console.WriteLine($"P2S5C2 physical snap L8: active={moved.Vertices.Length}; " +
            $"reused={moved.ReusedSamples}; new={moved.PreparedSamples}; " +
            $"reuse={100d * moved.ReusedSamples / moved.Vertices.Length:F3}%; " +
            $"initialCpu={initialCpu:F3}ms; initialGpu={initial.Metrics.GpuMilliseconds:F6}ms; " +
            $"snapCpu={snapCpu:F3}ms; snapGpu={moved.Metrics.GpuMilliseconds:F6}ms; " +
            $"heightParity={moved.MaximumCpuHeightErrorMetres:E9}m; " +
            $"normalParity={moved.MaximumCpuNormalErrorRadians:E9}rad; topologyUploads=0");
        Require(initial.Metrics.ValidationErrors == 0 && moved.Metrics.ValidationErrors == 0 &&
            moved.ReusedSamples > 0 && moved.PreparedSamples > 0 &&
            moved.PreparedSamples < moved.Vertices.Length &&
            moved.MaximumCpuHeightErrorMetres < 1e-5 &&
            moved.MaximumCpuNormalErrorRadians < 5e-3,
            "real physical preparation reuses the snapped sample set with CPU/GPU parity");
    }

    private static void ProveMovingCoordinator(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var runtime = new PlanetaryProductionSphericalBillboardMovingRuntime(levels, PrepareSynthetic);
        ulong frame = 0;
        var target = Double3.UnitZ;
        var altitude = FindRepresentativeAltitude(levels, 8);
        var telemetry = runtime.Update(View(altitude, frame++), 0);
        var initial = AwaitPrepared(runtime);
        telemetry = runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++),
            unchecked((uint)initial.PublicationGeneration));
        Require(runtime.Current?.Topology.Level == 8 && telemetry.Publications == 1 &&
            telemetry.ZeroOwnerFrames == 0 && telemetry.OverlapOwnerFrames == 0,
            "initial candidate atomically replaces the complete global fallback with one owner");

        var stable = runtime.Current!.Pupil;
        var small = (stable.PivotDirection + stable.Tangent.East *
            (levels[8].Snap.PupilCellRadians * (levels[8].Snap.CandidateShiftMultiple - 1))).Normalized();
        telemetry = runtime.Update(new(altitude, small, 3440, 1440, Math.PI / 3d, frame++), 0);
        Require(!runtime.ReplacementInFlight && telemetry.TopologyUploads == 1,
            "sub-threshold movement updates live camera demand without topology or physical replacement");

        var snapAngle = levels[8].Snap.PupilCellRadians * (levels[8].Snap.CandidateShiftMultiple + 1);
        target = stable.PivotDirection * Math.Cos(snapAngle) + stable.Tangent.East * Math.Sin(snapAngle);
        runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++), 0);
        var snapped = AwaitPrepared(runtime);
        Require(!snapped.TopologyUploadRequired && snapped.Physical.PreparedSamples > 0 &&
            snapped.Physical.ReusedSamples > 0,
            "snap retains immutable topology and incrementally prepares the entering physical set");
        var stagedTelemetry = runtime.Update(
            new(altitude, target, 3440, 1440, Math.PI / 3d, frame++), 0);
        telemetry = runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++),
            unchecked((uint)snapped.PublicationGeneration));
        Require(runtime.Current?.Pupil.Generation == snapped.Pupil.Generation &&
            telemetry.TopologyUploads == 1 && telemetry.Publications == 2 &&
            telemetry.ZeroOwnerFrames == 0 && telemetry.OverlapOwnerFrames == 0 &&
            telemetry.StaleGenerationDraws == 0 &&
            stagedTelemetry.ResidentGpuBytes > telemetry.ResidentGpuBytes &&
            telemetry.PeakResidentGpuBytes == stagedTelemetry.ResidentGpuBytes,
            "snap publication changes the actual current generation with zero topology upload or ownership fault");
        var timing = runtime.TimingSummary;
        Require(timing.Callback.Samples > 0 && timing.Selector.Samples > 0 &&
            timing.PupilAndSnap.Samples > 0 && timing.PhysicalPreparation.Samples == 2 &&
            timing.Publication.Samples == 2,
            "bounded runtime records callback, selector, pupil/snap, preparation, and publication timing");
        Console.WriteLine($"P2S5C2 moving runtime: publications={telemetry.Publications}; topologyUploads={telemetry.TopologyUploads}; " +
            $"current=L{telemetry.CurrentLevel}; pupilError={telemetry.PupilAngularErrorRadians:E9}rad; " +
            $"resident={telemetry.ResidentGpuBytes}; peak={telemetry.PeakResidentGpuBytes}; " +
            $"callback={timing.Callback.AverageMilliseconds:F6}/{timing.Callback.P95Milliseconds:F6}/{timing.Callback.MaximumMilliseconds:F6}ms");
        Console.WriteLine($"P2S5C2 moving timings avg/p95/max ms: " +
            $"selector={Format(timing.Selector)}; pupilSnap={Format(timing.PupilAndSnap)}; " +
            $"scheduling={Format(timing.DemandScheduling)}; physical={Format(timing.PhysicalPreparation)}; " +
            $"gpuPrepare={Format(timing.GpuPreparation)}; publication={Format(timing.Publication)}");
    }

    private static void ProveMovementCampaign(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var representative = new double[levels.Count];
        var found = new bool[levels.Count];
        var logarithmicRange = Math.Log(80_000_000d / 10d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            var level = selector.Evaluate(View(altitude, (ulong)sample), false).Level;
            if (!found[level]) { found[level] = true; representative[level] = altitude; }
        }
        Require(found.All(value => value), "the deterministic orbit-to-10m campaign exercises all 18 authored levels");

        selector.CancelInitialSelectionForTests();
        var first = selector.Evaluate(View(representative[0], 0), false);
        selector.CommitPublication(first.Level);
        ulong frame = 1;
        for (var level = 1; level < levels.Count; level++)
        {
            var firstDwell = selector.Evaluate(View(representative[level], frame++), false);
            var secondDwell = selector.Evaluate(View(representative[level], frame++), false);
            var selected = firstDwell.Urgent ? firstDwell : secondDwell;
            Require(selected.Level == level && selector.InFlightLevel == level,
                $"inward scale transition enters adjacent L{level:D2} after urgent or two-frame dwell");
            var noisyReverse = selector.Evaluate(View(representative[0], frame++), true);
            Require(noisyReverse.Level == level,
                "one noisy reverse sample cannot reverse an in-flight generation");
            selector.CommitPublication(level);
        }
        for (var level = levels.Count - 2; level >= 0; level--)
        {
            var outwardAltitude = representative[Math.Max(0, level - 1)];
            _ = selector.Evaluate(View(outwardAltitude, frame++), false);
            var selected = selector.Evaluate(View(outwardAltitude, frame++), false);
            Require(selected.Level == level && selector.InFlightLevel == level,
                $"outward scale transition enters adjacent L{level:D2} after two-frame dwell");
            selector.CommitPublication(level);
        }

        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, levels[8]);
        var directions = new[]
        {
            new Double3(1,0,0), new Double3(-1,0,0), new Double3(0,0,1), new Double3(0,0,-1),
            new Double3(1,1,1).Normalized(), new Double3(-1,1,-1).Normalized(),
            new Double3(1e-10,1,1e-10).Normalized(), new Double3(-1e-10,-1,-1e-10).Normalized()
        };
        var maximumAngularError = 0d;
        foreach (var direction in directions)
        {
            for (var step = 0; step < 8; step++)
            {
                var amount = (step + 1d) / 8d;
                var next = (pupil.PivotDirection * (1d - amount) + direction * amount).Normalized();
                pupil = PlanetaryProductionBillboardPupil.Resolve(pupil, next, levels[8]);
                Require(pupil.IsValid && pupil.Tangent.IsValid,
                    "cardinal, diagonal, wrap, and polar transport remains finite");
                maximumAngularError = Math.Max(maximumAngularError, Math.Acos(Math.Clamp(
                    Double3.Dot(pupil.PivotDirection, next), -1d, 1d)));
            }
        }
        Console.WriteLine($"P2S5C2 pupil campaign error: max={maximumAngularError:E9}rad; " +
            $"authoredAxisThreshold={levels[8].Snap.PupilCellRadians * levels[8].Snap.CandidateShiftMultiple:E9}rad");
        Require(maximumAngularError <= levels[8].Snap.PupilCellRadians *
            levels[8].Snap.CandidateShiftMultiple * 3d,
            "retained pupil error stays inside the authored snap neighborhood");
        Console.WriteLine($"P2S5C2 campaign: levels=18; descent=18; ascent=18; frames={frame}; " +
            $"cardinalDiagonalWrapPoles={directions.Length}; maxPupilError={maximumAngularError:E9}rad; " +
            "timeWarp=body-fixed input invariant; unanchored=body-fixed input invariant");
    }

    private static PlanetaryProductionBillboardPreparedGeneration AwaitPrepared(
        PlanetaryProductionSphericalBillboardMovingRuntime runtime)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (runtime.TrySubmitPrepared(out var generation)) return generation;
            Thread.Yield();
        }
        throw new InvalidOperationException("P2S5C: bounded background preparation did not complete.");
    }

    private static double FindRepresentativeAltitude(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels, int requestedLevel)
    {
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var logarithmicRange = Math.Log(80_000_000d / 10d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            if (selector.Evaluate(View(altitude, (ulong)sample), false).Level == requestedLevel)
                return altitude;
        }
        throw new InvalidOperationException($"P2S5C: no representative altitude selected L{requestedLevel:D2}.");
    }

    private static PlanetaryProductionBillboardPhysicalPreparation PrepareSynthetic(
        PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardPupil pupil,
        PlanetaryProductionBillboardPhysicalCache cache)
    {
        var vertices = new NativeSphericalBillboardPhysicalVertex[topology.Vertices.Count];
        var identities = new PlanetaryCanonicalPhysicalSampleIdentity[topology.Vertices.Count];
        var prepared = 0;
        for (var i = 0; i < topology.Vertices.Count; i++)
        {
            var direction = pupil.ResolveCanonicalDirection(topology.Vertices[i], topology);
            var identity = PlanetaryCanonicalPhysicalSampleIdentity.Create(direction,
                PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration,
                PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration);
            identities[i] = identity;
            if (!cache.TryGet(identity, out var value))
            {
                var height = direction.X * 17d + direction.Y * 11d + direction.Z * 5d;
                var body = direction * (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + height);
                value = new NativeSphericalBillboardPhysicalVertex
                {
                    BodyX = body.X, BodyY = body.Y, BodyZ = body.Z,
                    PhysicalHeightMetres = height,
                    NormalX = (float)direction.X, NormalY = (float)direction.Y,
                    NormalZ = (float)direction.Z, NormalValidity = 1f
                };
                cache.Store(identity, value);
                prepared++;
            }
            vertices[i] = value;
        }
        return new(vertices, default, 0d, 0d, prepared, vertices.Length - prepared,
            Array.AsReadOnly(identities));
    }

    private static void ProvePublication(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var publication = new PlanetaryProductionSphericalBillboardPublication();
        publication.Bootstrap(levels[0]);
        publication.RecordFrame(publication.Current!.PublicationGeneration);
        for (var level = 1; level < levels.Count; level++)
        {
            var reuse = PlanetaryProductionSphericalBillboardReuse.CrossLevel(levels[level - 1], levels[level]);
            var incoming = publication.BeginIncoming(levels[level], reuse);
            publication.RecordFrame(publication.Current!.PublicationGeneration);
            foreach (var state in new[]
            {
                PlanetaryProductionBillboardResidencyState.PhysicalReady,
                PlanetaryProductionBillboardResidencyState.NormalReady,
                PlanetaryProductionBillboardResidencyState.CullCompactReady,
                PlanetaryProductionBillboardResidencyState.DrawReady,
                PlanetaryProductionBillboardResidencyState.FenceComplete
            })
            {
                publication.AdvanceIncoming(state);
                publication.RecordFrame(publication.Current!.PublicationGeneration);
            }
            var authoritative = publication.PublishAtFrameBoundary();
            Require(authoritative.PublicationGeneration == incoming.PublicationGeneration,
                "atomic boundary publishes the completely prepared generation");
            publication.RecordFrame(authoritative.PublicationGeneration);
        }
        Require(publication.ZeroOwnerFrames == 0 && publication.OverlapOwnerFrames == 0 &&
            publication.StaleGenerationDraws == 0 && publication.Incoming is null &&
            publication.Current!.Topology.Level == 17,
            "all adjacent transitions retain exactly one current owner and reject stale generations");
    }

    private static void ProveTes(PlanetaryProductionSphericalBillboardTopology finest)
    {
        var a = new Double3(-20, 0, -100);
        var b = new Double3(20, 0, -101);
        var first = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(a, b, 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres,
            finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        var reversed = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(b, a, 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres,
            finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        Require(first == reversed && first is >= 1 and <= 64,
            "TES edge factors are shared-edge deterministic and bounded 1-64");
        var skew = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(
            new Double3(0, 0, -10), new Double3(1, 0, -1000), 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres,
            finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        Require(skew >= 1 && skew <= 64, "perspective-skew path remains bounded");
        const double exactHorizonBasePixels = 1344.683;
        var residual = exactHorizonBasePixels / 64d;
        Require(Math.Abs(residual - 21.010671875d) < 1e-9 && residual > finest.Error.TesTargetMaximumPixels,
            "known L17 exact-horizon residual is measured honestly and is not hidden by raising the cap");
        Console.WriteLine($"P2S5C exact horizon: base={exactHorizonBasePixels:F3}px; TES64={residual:F6}px; " +
            "transitionCurvature=0.459px; silhouette=1.312px; requiresManualSignificanceCheck=true");
    }

    private static void ProveConservativeNearSurfaceHorizonCoverage(
        PlanetaryProductionSphericalBillboardTopology topology)
    {
        var radius = PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
        var maximumTerrain = PlanetaryTerrainDefinition.EarthProductionCubeV5.MaximumHeightMetres;
        // Captured C3 native 3440x1440 failure pose. The initial generation's
        // pupil is deliberately stale here: current remains authoritative while
        // a moved pupil prepares, so its base must still cover the planet.
        var cameraDirection = new Double3(-1036.8296487848155, -2603347.813620877,
            5814848.209969225).Normalized();
        var camera = cameraDirection * (radius + 10.004d);
        var stalePupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
        var directions = topology.Vertices.Select(vertex =>
            stalePupil.ResolveCanonicalDirection(vertex, topology)).ToArray();
        var bodies = directions.Select(direction => direction * radius).ToArray();

        var reference = Math.Abs(cameraDirection.Y) < .9d ? Double3.UnitY : Double3.UnitX;
        var east = Double3.Cross(reference, cameraDirection).Normalized();
        var north = Double3.Cross(cameraDirection, east).Normalized();
        var sampleDistances = new[] { 100d, 1_000d, 5_000d, 10_000d };
        var retainedSamples = 0;
        for (var bearing = 0; bearing < 8; bearing++)
        {
            var azimuth = bearing * Math.Tau / 8d;
            var tangent = east * Math.Cos(azimuth) + north * Math.Sin(azimuth);
            foreach (var distance in sampleDistances)
            {
                Require(distance > PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres,
                    "coverage sample lies outside the 50 m TES refinement range");
                var angle = distance / radius;
                var target = (cameraDirection * Math.Cos(angle) + tangent * Math.Sin(angle)).Normalized();
                var triangle = FindContainingSphericalTriangle(topology, directions, target);
                Require(triangle >= 0, $"closed base contains the {distance:F0} m horizon sample");
                var first = bodies[topology.Indices[triangle * 3]];
                var second = bodies[topology.Indices[triangle * 3 + 1]];
                var third = bodies[topology.Indices[triangle * 3 + 2]];
                var bound = PlanetaryProductionSphericalBillboardCulling.EncloseCurvedPatch(
                    first, second, third, radius, maximumTerrain);
                Require(!PlanetaryProductionSphericalBillboardCulling.IsOccludedByPlanet(
                        camera, bound, radius - .02d),
                    $"curved base retains depth ownership at {distance:F0} m beyond TES range");
                retainedSamples++;
            }
        }
        Console.WriteLine($"P2S5C3 near-surface coverage: level=L{topology.Level}; " +
            $"altitude=10.004m; samples={string.Join(',', sampleDistances.Select(v => $"{v:F0}m"))}; " +
            $"bearings=8; outsideTesRange={retainedSamples}; retained=all");
    }

    private static int FindContainingSphericalTriangle(
        PlanetaryProductionSphericalBillboardTopology topology,
        IReadOnlyList<Double3> directions, in Double3 target)
    {
        for (var triangle = 0; triangle < topology.TriangleCount; triangle++)
        {
            var first = directions[(int)topology.Indices[triangle * 3]];
            var second = directions[(int)topology.Indices[triangle * 3 + 1]];
            var third = directions[(int)topology.Indices[triangle * 3 + 2]];
            if (SameSphericalSide(first, second, third, target) &&
                SameSphericalSide(second, third, first, target) &&
                SameSphericalSide(third, first, second, target)) return triangle;
        }
        return -1;
    }

    private static void DiagnoseBodyFixedLateralContinuity(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var previousGeneration = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            var level14 = levels[14];
            var level15 = levels[15];
            var cameraDirection = new Double3(-1036.8296487848155, -2603347.813620877,
                5814848.209969225).Normalized();
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default, cameraDirection, level14);
            var frame = pupil.Tangent;
            var subThreshold = level14.Snap.PupilCellRadians *
                (level14.Snap.CandidateShiftMultiple - 1d);
            var snapAngle = level14.Snap.PupilCellRadians *
                (level14.Snap.CandidateShiftMultiple + 1d);
            var forward = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(subThreshold) + frame.North * Math.Sin(subThreshold)).Normalized(),
                level14);
            var backward = PlanetaryProductionBillboardPupil.Resolve(forward, cameraDirection, level14);
            var lateral = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(subThreshold) + frame.East * Math.Sin(subThreshold)).Normalized(),
                level14);
            var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(snapAngle) + frame.East * Math.Sin(snapAngle)).Normalized(),
                level14);
            var rebased = PlanetaryProductionBillboardPupil.Resolve(snapped,
                (cameraDirection * Math.Cos(.01d) + frame.East * Math.Sin(.01d)).Normalized(), level14);
            var adjacent = PlanetaryProductionBillboardPupil.Resolve(snapped,
                snapped.PivotDirection, level15);
            ProveSnappedGeometry(level14, rebased);
            var states = new[]
            {
                new TrackedPupilState("stationary", level14, pupil),
                new TrackedPupilState("forward", level14, forward),
                new TrackedPupilState("backward", level14, backward),
                new TrackedPupilState("lateral", level14, lateral),
                new TrackedPupilState("same-level-snap", level14, snapped),
                new TrackedPupilState("same-level-rebase", level14, rebased),
                new TrackedPupilState("adjacent-L15", level15, adjacent)
            };
            var bearings = new[] { 0d, Math.PI / 4d, Math.PI / 2d };
            var distances = new[] { 1_000d, 5_000d, 10_000d, 25_000d };
            var tracked = bearings.SelectMany(bearing => distances.Select(distance =>
            {
                var tangent = frame.North * Math.Cos(bearing) + frame.East * Math.Sin(bearing);
                var angle = distance / PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
                return (label: $"{distance:F0}m/{bearing * 180d / Math.PI:F0}deg",
                    distance,
                    direction: (cameraDirection * Math.Cos(angle) + tangent * Math.Sin(angle)).Normalized());
            })).ToArray();
            var canonical = tracked.Select(sample =>
            {
                var height = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                    PlanetaryTerrainDefinition.EarthProductionCubeV5, sample.direction,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                return (sample.label, sample.direction, height,
                    position: sample.direction *
                        (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + height));
            }).ToArray();
            var baseline = EvaluateRenderedBase(states[0], canonical);
            var baseIdentities = ResolveIdentities(states[0]).ToHashSet();
            var maximumCanonicalHeightDelta = 0d;
            var maximumCanonicalPositionDelta = 0d;
            foreach (var state in states)
            {
                var rendered = EvaluateRenderedBase(state, canonical);
                var maximumRenderedHeightDelta = 0d;
                var maximumOuterCoverageHeightDelta = 0d;
                for (var i = 0; i < canonical.Length; i++)
                {
                    var repeatedHeight = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                        PlanetaryTerrainDefinition.EarthProductionCubeV5, canonical[i].direction,
                        PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                    maximumCanonicalHeightDelta = Math.Max(maximumCanonicalHeightDelta,
                        Math.Abs(repeatedHeight - canonical[i].height));
                    var repeatedPosition = canonical[i].direction *
                        (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + repeatedHeight);
                    maximumCanonicalPositionDelta = Math.Max(maximumCanonicalPositionDelta,
                        Math.Sqrt((repeatedPosition - canonical[i].position).LengthSquared));
                    maximumRenderedHeightDelta = Math.Max(maximumRenderedHeightDelta,
                        Math.Abs(rendered[i] - baseline[i]));
                    if (tracked[i].distance >= 5_000d)
                        maximumOuterCoverageHeightDelta = Math.Max(maximumOuterCoverageHeightDelta,
                            Math.Abs(rendered[i] - baseline[i]));
                }
                var identities = ResolveIdentities(state);
                var reused = identities.Count(identity => baseIdentities.Contains(identity));
                Console.WriteLine($"P2S5C3 lateral continuity: state={state.Name}; level=L{state.Topology.Level}; " +
                    $"pupil={state.Pupil.Generation}; rebase={state.Pupil.Rebased}; " +
                    $"reuse={reused}/{identities.Length}; canonicalHeightDelta={maximumCanonicalHeightDelta:E9}m; " +
                    $"canonicalPositionDelta={maximumCanonicalPositionDelta:E9}m; " +
                    $"renderedBaseHeightDelta={maximumRenderedHeightDelta:E9}m; " +
                    $"outerCoverageHeightDelta={maximumOuterCoverageHeightDelta:E9}m");
            }
            Require(forward == pupil && backward == pupil && lateral == pupil,
                "stationary and sub-threshold forward/back/lateral motion retain the exact pupil generation");
            Require(maximumCanonicalHeightDelta == 0d && maximumCanonicalPositionDelta == 0d,
                "tracked body-fixed H(direction) and FP64 positions are invariant across camera/pupil states");
        }
        finally
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previousGeneration);
        }
    }

    private static double[] EvaluateRenderedBase(TrackedPupilState state,
        IReadOnlyList<(string label, Double3 direction, double height, Double3 position)> samples)
    {
        var directions = state.Topology.Vertices.Select(vertex =>
            state.Pupil.ResolveCanonicalDirection(vertex, state.Topology)).ToArray();
        var triangles = FindContainingSphericalTriangles(state.Topology, directions,
            samples.Select(sample => sample.direction).ToArray());
        var values = new double[samples.Count];
        var radius = PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
        for (var sample = 0; sample < samples.Count; sample++)
        {
            var triangle = triangles[sample];
            Require(triangle >= 0, $"{state.Name} contains tracked sample {samples[sample].label}");
            var positions = new Double3[3];
            for (var corner = 0; corner < 3; corner++)
            {
                var direction = directions[state.Topology.Indices[triangle * 3 + corner]];
                var height = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                    PlanetaryTerrainDefinition.EarthProductionCubeV5, direction,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                positions[corner] = direction * (radius + height);
            }
            var normal = Double3.Cross(positions[1] - positions[0], positions[2] - positions[0]);
            var denominator = Double3.Dot(normal, samples[sample].direction);
            Require(Math.Abs(denominator) > 1e-12d, $"{state.Name} tracked triangle intersects its body ray");
            values[sample] = Double3.Dot(normal, positions[0]) / denominator - radius;
        }
        return values;
    }

    private static int[] FindContainingSphericalTriangles(
        PlanetaryProductionSphericalBillboardTopology topology,
        IReadOnlyList<Double3> directions,
        IReadOnlyList<Double3> targets)
    {
        var nearestVertices = new int[targets.Count];
        var nearestDots = Enumerable.Repeat(double.NegativeInfinity, targets.Count).ToArray();
        for (var vertex = 0; vertex < directions.Count; vertex++)
        {
            for (var target = 0; target < targets.Count; target++)
            {
                var dot = Double3.Dot(directions[vertex], targets[target]);
                if (dot <= nearestDots[target]) continue;
                nearestDots[target] = dot;
                nearestVertices[target] = vertex;
            }
        }

        var results = Enumerable.Repeat(-1, targets.Count).ToArray();
        for (var triangle = 0; triangle < topology.TriangleCount; triangle++)
        {
            var firstIndex = (int)topology.Indices[triangle * 3];
            var secondIndex = (int)topology.Indices[triangle * 3 + 1];
            var thirdIndex = (int)topology.Indices[triangle * 3 + 2];
            for (var target = 0; target < targets.Count; target++)
            {
                if (results[target] >= 0) continue;
                var nearest = nearestVertices[target];
                if (firstIndex != nearest && secondIndex != nearest && thirdIndex != nearest) continue;
                var first = directions[firstIndex];
                var second = directions[secondIndex];
                var third = directions[thirdIndex];
                if (SameSphericalSide(first, second, third, targets[target]) &&
                    SameSphericalSide(second, third, first, targets[target]) &&
                    SameSphericalSide(third, first, second, targets[target]))
                    results[target] = triangle;
            }
        }

        // The generated topology is locally regular, so the containing triangle
        // normally owns the nearest vertex. Retain an exhaustive diagnostic fallback
        // rather than making that acceleration assumption part of production code.
        for (var target = 0; target < targets.Count; target++)
            if (results[target] < 0)
                results[target] = FindContainingSphericalTriangle(topology, directions, targets[target]);
        return results;
    }

    private static PlanetaryCanonicalPhysicalSampleIdentity[] ResolveIdentities(TrackedPupilState state) =>
        state.Topology.Vertices.Select(vertex => PlanetaryCanonicalPhysicalSampleIdentity.Create(
            state.Pupil.ResolveCanonicalDirection(vertex, state.Topology),
            PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration,
            PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration)).ToArray();

    private readonly record struct TrackedPupilState(string Name,
        PlanetaryProductionSphericalBillboardTopology Topology,
        PlanetaryProductionBillboardPupil Pupil);

    private static bool SameSphericalSide(in Double3 first, in Double3 second,
        in Double3 interior, in Double3 target)
    {
        var edge = Double3.Cross(first, second);
        return Double3.Dot(edge, interior) * Double3.Dot(edge, target) >= -1e-15d;
    }

    private static void ProveIsolation(string root)
    {
        var production = File.ReadAllText(Path.Combine(root, "samples", "NovaCore.Triangle", "Program.cs"));
        Require(!production.Contains("ProductionSphericalBillboardTopologyGenerator.Generate", StringComparison.Ordinal),
            "candidate runtime never generates production topology");
        var nativeProduction = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.cpp"));
        Require(!nativeProduction.Contains("planetary-production-topology", StringComparison.Ordinal),
            "existing production renderer remains isolated from the opt-in candidate library");
    }

    private static PlanetaryProductionBillboardView View(double altitude, ulong frame) =>
        new(altitude, Double3.UnitZ, 3440, 1440, Math.PI / 3d, frame);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("P2S5C: " + message);
    }

    private static string Format(PlanetaryProductionBillboardTiming value) =>
        $"{value.AverageMilliseconds:F6}/{value.P95Milliseconds:F6}/{value.MaximumMilliseconds:F6}";
}
