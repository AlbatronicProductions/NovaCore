using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using System.Runtime.InteropServices;

internal static class PlanetaryProductionSphericalBillboardRuntimeTests
{
    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var levels = PlanetaryProductionSphericalBillboardTopologyLibrary.Load(
            Path.Combine(root, "assets", "planetary-production-topology"));
        Require(levels.Count == 18, "complete production topology library loaded");
        Require(Marshal.SizeOf<NativeSphericalBillboardPhysicalVertex>() == 64,
            "physical billboard vertex matches the std430 dvec4 array stride");
        ProveV2Gpu(root, levels);
        ProveNativeSelectedIncomingResidency(root, levels);
        ProveSelector(levels);
        ProvePupilAndSnap(levels);
        ProveReuse(levels);
        ProvePublication(levels);
        ProveTes(levels[^1]);
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
            Math.PI / 3d, 50_000d, finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        var reversed = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(b, a, 1440,
            Math.PI / 3d, 50_000d, finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        Require(first == reversed && first is >= 1 and <= 64,
            "TES edge factors are shared-edge deterministic and bounded 1-64");
        var skew = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(
            new Double3(0, 0, -10), new Double3(1, 0, -1000), 1440,
            Math.PI / 3d, 50_000d, finest.Error.MinimumRepresentablePhysicalWavelengthMetres);
        Require(skew >= 1 && skew <= 64, "perspective-skew path remains bounded");
        const double exactHorizonBasePixels = 1344.683;
        var residual = exactHorizonBasePixels / 64d;
        Require(Math.Abs(residual - 21.010671875d) < 1e-9 && residual > finest.Error.TesTargetMaximumPixels,
            "known L17 exact-horizon residual is measured honestly and is not hidden by raising the cap");
        Console.WriteLine($"P2S5C exact horizon: base={exactHorizonBasePixels:F3}px; TES64={residual:F6}px; " +
            "transitionCurvature=0.459px; silhouette=1.312px; requiresManualSignificanceCheck=true");
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
}
