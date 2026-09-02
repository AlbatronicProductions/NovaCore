using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryDynamicAnchoredSurfaceTests
{
    private const ulong EarthBodyId = 6;
    private const double Radius = 6_378_137d;
    private const double Fov = Math.PI / 3d;
    private const double Aspect = 16d / 9d;
    private const double Viewport = 1080d;

    public static void Run()
    {
        GpuPipelineSourceContract();
        QuantizedScreenSpaceRefinement();
        ReusableBaseTopologyAndStitches();
        StableSphericalBillboardFrame();
        TransactionalDescriptorPublication();
        CanonicalPhysicalAndMaterialAuthority();
        MovingFootprintSchedulerAndCapacity();
        MovingReferenceFrameReusesPersistentTopology();
        FrameSynchronizedA1A5Telemetry();
        StationaryRotationRetainsPhysicalNeighborhood();
        SolarScaleDeactivationIsIdle();
        FailureRetainsPreviousOwner();
    }

    private static void GpuPipelineSourceContract()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var vertex = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "anchored_terrain.vert"));
        var physical = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "anchored_physical_surface.glsl"));
        var canonical = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary_physical_authority.glsl"));
        var control = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "anchored_terrain.tesc"));
        var evaluation = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "anchored_terrain.tese"));
        var header = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.h"));
        var native = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.cpp"));
        Assert(vertex.Contains("layout(location=0) in vec2 baseUv", StringComparison.Ordinal) &&
            vertex.Contains("AnchoredFrameOffset=16384u", StringComparison.Ordinal) &&
            vertex.Contains("AnchoredPatchOffset=16393u", StringComparison.Ordinal) &&
            evaluation.Contains("frameOffset=16384u", StringComparison.Ordinal) &&
            native.Contains("AnchoredSurfaceMaximumPatches=6144", StringComparison.Ordinal) &&
            native.Contains("AnchoredSurfaceMaximumCacheSlots=16384", StringComparison.Ordinal) &&
            native.Contains("AnchoredSurfaceCoverageCapacity=16384", StringComparison.Ordinal),
            "production vertex input is one reusable base topology plus patch descriptors");
        Assert(control.Contains("ConservativelyOutside", StringComparison.Ordinal) &&
            control.Contains("QuantizedRefinement", StringComparison.Ordinal) &&
            control.Contains("rangeMetres=50.0", StringComparison.Ordinal) &&
            control.Contains("planeIndex<4u", StringComparison.Ordinal) &&
            control.Contains("gl_TessLevelOuter", StringComparison.Ordinal),
            "tessellation control performs conservative bound rejection and raster-proximate screen refinement");
        Assert(vertex.Contains("AnchoredGeographicHeight", StringComparison.Ordinal) &&
            vertex.Contains("CanonicalBasePhysicalHeight(direction)", StringComparison.Ordinal) &&
            vertex.Contains("AnchoredBasePhysicalNormal", StringComparison.Ordinal) &&
            evaluation.Contains("ProductionProjectD", StringComparison.Ordinal) &&
            evaluation.Contains("layout(triangles,equal_spacing,cw)", StringComparison.Ordinal) &&
            evaluation.Contains("EvaluateNearPhysicalD(direction,EvaluateBiomeBlendD(direction,geographic))", StringComparison.Ordinal) &&
            evaluation.Contains("nearValue.eastGradient", StringComparison.Ordinal),
            "geographic/meso displacement is prepared on the reusable base while TES adds the bounded canonical near field and its physical gradient");
        Assert(physical.Contains("planetary_physical_authority.glsl", StringComparison.Ordinal) &&
            physical.Contains("CanonicalGeographicHeight(direction)", StringComparison.Ordinal) &&
            canonical.Contains("binding=33", StringComparison.Ordinal) &&
            canonical.Contains("const uint width=8192u,height=4096u", StringComparison.Ordinal) &&
            canonical.Contains("atan(-lookupDirection.z,lookupDirection.x)", StringComparison.Ordinal) &&
            !physical.Contains("AnchoredGlobalHeight", StringComparison.Ordinal) &&
            native.Contains("physical elevation oracle path is required", StringComparison.Ordinal) &&
            native.Contains("binds[20].binding=33", StringComparison.Ordinal) &&
            header.Contains("elevationOraclePathUtf8", StringComparison.Ordinal),
            "anchored production geometry consumes the shared canonical 8192x4096 physical oracle through the required runtime ABI, never the terrain-v5 visual payload");
        Assert(!header.Contains("NcAnchoredTerrainVertex", StringComparison.Ordinal) &&
            !header.Contains("anchoredSurfaceVertexPool", StringComparison.Ordinal),
            "the frame ABI has no CPU final-raster vertex pool");
    }

    private static void QuantizedScreenSpaceRefinement()
    {
        var cases = new[]
        {
            (Pixels: 0d, Factor: 1u), (Pixels: 16d, Factor: 1u),
            (Pixels: 16.001d, Factor: 2u), (Pixels: 32.001d, Factor: 4u),
            (Pixels: 128.001d, Factor: 16u), (Pixels: 100_000d, Factor: 16u),
            (Pixels: double.PositiveInfinity, Factor: 16u)
        };
        foreach (var value in cases)
            Assert(PlanetaryScreenSpaceSubdivision.QuantizedGpuFactor(value.Pixels) == value.Factor,
                $"projected edge {value.Pixels:R}px quantizes to factor {value.Factor}");
        foreach (var viewport in new[] { (W: 1280d, H: 720d), (W: 3440d, H: 1440d), (W: 2560d, H: 1080d) })
        foreach (var fov in new[] { Math.PI / 4d, Math.PI / 3d, 1.25d })
        {
            var focal = viewport.H / (2d * Math.Tan(fov * .5d));
            foreach (var metres in new[] { 100d, 1_000d, 10_000d, 100_000d })
            {
                var pixels = metres / 100_000d * focal;
                var factor = PlanetaryScreenSpaceSubdivision.QuantizedGpuFactor(pixels);
                Assert(factor is 1 or 2 or 4 or 8 or 16,
                    "every aspect/FOV/viewport case produces a bounded power-of-two factor");
                Assert(factor == 16 || pixels / factor <= PlanetaryDynamicAnchoredSurface.GpuTargetEdgePixels + 1e-12d,
                    "uncapped refined edge remains within the 16-pixel production target");
            }
        }
    }

    private static void ReusableBaseTopologyAndStitches()
    {
        const int r = PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution;
        Assert(PlanetaryDynamicAnchoredSurface.GpuBaseVerticesPerPatch == (r + 1) * (r + 1) &&
            PlanetaryDynamicAnchoredSurface.GpuBaseIndicesPerPatch == r * r * 6,
            "reusable base topology count is self-consistent");
        for (var mask = 0; mask < 16; mask++)
        {
            var doubleArea = 0;
            foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            {
                var patch = new PlanetaryPatch(face, 2, 1, 1);
                for (var y = 0; y < r; y++)
                for (var x = 0; x < r; x++)
                {
                    var q0 = Remap(x, y, mask); var q1 = Remap(x + 1, y, mask);
                    var q2 = Remap(x, y + 1, mask); var q3 = Remap(x + 1, y + 1, mask);
                    Verify(q0, q1, q2); Verify(q1, q3, q2);
                }

                void Verify((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
                {
                    var area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                    Assert(area >= 0, $"stitch mask {mask} preserves planar winding");
                    doubleArea += area;
                    if (area == 0) return;
                    var p0 = Point(a); var p1 = Point(b); var p2 = Point(c);
                    Assert(Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) > 0d,
                        $"stitch mask {mask} is outward on face {face}");
                }

                Double3 Point((int X, int Y) value)
                {
                    var uv = patch.GridCoordinate(value.X, value.Y, r);
                    return RelaxedCubeSphereProjection.UnitDirection(face, uv.U, uv.V);
                }
            }
            Assert(doubleArea == 6 * r * r * 2, $"stitch mask {mask} covers all six representative patches exactly");
        }

        static (int X, int Y) Remap(int x, int y, int mask)
        {
            if (x == 0 && (mask & 1) != 0 && (y & 1) != 0) y--;
            if (x == r && (mask & 2) != 0 && (y & 1) != 0) y--;
            if (y == 0 && (mask & 4) != 0 && (x & 1) != 0) x--;
            if (y == r && (mask & 8) != 0 && (x & 1) != 0) x--;
            return (x, y);
        }
    }

    private static void StableSphericalBillboardFrame()
    {
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        var hierarchy = Create();
        Publish(hierarchy, direction, 100_000d);
        var first = hierarchy.PresentationFrame;
        Assert(first.IsValid && first.BodyId == EarthBodyId && first.TangentBasis.IsValid,
            "published hierarchy has one valid canonical billboard frame");
        var pointDirection = (direction + first.TangentBasis.East * (100d / Radius)).Normalized();
        var sample = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(pointDirection);
        var point = pointDirection * (Radius + sample.FinalHeightMetres);
        var camera = direction * (Radius + 100_000d);
        var relative = first.CameraRelative(point, camera);
        var roundTrip = camera + relative;
        Assert(Math.Sqrt((roundTrip - point).LengthSquared) <= 1e-8d,
            "small billboard-relative transport round-trips canonical physical position");
    }

    private static void TransactionalDescriptorPublication()
    {
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        var hierarchy = Create();
        Update(hierarchy, direction, 100_000d);
        Assert(hierarchy.ActivePatchCount > 0 && !hierarchy.Visible && hierarchy.HasPendingGeneration,
            "candidate descriptors prepare while the prior/global owner remains authoritative");
        var generation = hierarchy.ActiveGeneration;
        hierarchy.AcknowledgeGpuGeneration(generation + 1u);
        Update(hierarchy, direction, 100_000d);
        Assert(!hierarchy.Visible, "wrong GPU generation cannot publish");
        PublishCurrent(hierarchy, direction, 100_000d);
        var patches = hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount).ToArray();
        Assert(patches.Length > 0 && patches.Length <= PlanetaryDynamicAnchoredSurface.DefaultActiveCapacity &&
            patches.All(value => (value.Flags & PlanetaryDynamicAnchoredSurface.RequiredSubmissionFlags) ==
                PlanetaryDynamicAnchoredSurface.RequiredSubmissionFlags),
            "one complete descriptor generation becomes authoritative atomically");
        Assert(patches.Select(value => (value.Face, value.Level, value.X, value.Y)).Distinct().Count() == patches.Length,
            "published generation has one owner per canonical patch identity");
        Assert(hierarchy.Telemetry.UploadBytes <= patches.Length * PlanetaryDynamicAnchoredSurface.GpuPatchDescriptorBytes * 2L,
            "CPU preparation publishes compact descriptors rather than 17x17 final-raster vertices");
    }

    private static void CanonicalPhysicalAndMaterialAuthority()
    {
        var direction = RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .4999d, .2111d);
        var hierarchy = Create(); Publish(hierarchy, direction, 30_000d);
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        var samples = 0; var edgeGap = 0d; var addresses = 0;
        var positions = new Dictionary<PlanetaryAnchoredMeshVertexId, Double3>();
        foreach (var descriptor in hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount))
        {
            var patch = new PlanetarySurfacePatchId(EarthBodyId, descriptor.TerrainVersion,
                (CubeSphereFace)descriptor.Face, (int)descriptor.Level, (int)descriptor.X, (int)descriptor.Y);
            var bounds = new PlanetaryPatch(patch.Face, patch.Level, patch.X, patch.Y).Bounds;
            var centerDirection = RelaxedCubeSphereProjection.UnitDirection(patch.Face,
                (bounds.MinX + bounds.MaxX) * .5d, (bounds.MinY + bounds.MaxY) * .5d);
            Assert(RelaxedCubeSphereProjection.TryAddress(centerDirection, out var face, out var u, out var v),
                "canonical center is addressable");
            var materialLevel = Math.Min(patch.Level, PlanetaryCubeSurfacePackContract.MaximumLevel);
            var cells = 1 << materialLevel;
            Assert(face == patch.Face && descriptor.MaterialLevel == materialLevel &&
                descriptor.MaterialX == (uint)Math.Min((int)Math.Floor(u * cells), cells - 1) &&
                descriptor.MaterialY == (uint)Math.Min((int)Math.Floor(v * cells), cells - 1),
                "material address derives from the same canonical body-fixed patch");
            Assert(hierarchy.CoversDirection(centerDirection),
                "canonical patch interior is covered by the published owner");
            addresses++;
            for (var y = 0; y <= PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution; y++)
            for (var x = 0; x <= PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution; x++)
            {
                var identity = PlanetaryAnchoredMeshVertexId.FromPatchGrid(patch, x, y,
                    PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution);
                var physical = terrain.SamplePhysicalSurface(identity.BodyFixedDirection);
                Assert(physical.IsFinite, "late displacement source is finite");
                var position = identity.BodyFixedDirection * (Radius + physical.FinalHeightMetres);
                if (positions.TryGetValue(identity, out var previous))
                    edgeGap = Math.Max(edgeGap, Math.Sqrt((position - previous).LengthSquared));
                else positions.Add(identity, position);
                samples++;
            }
        }
        Assert(samples > 0 && addresses == hierarchy.AuthoritativePatchCount && edgeGap <= 1e-9d,
            "geometry, height, and material share exact geography across patch and cube boundaries");
    }

    private static void MovingFootprintSchedulerAndCapacity()
    {
        var hierarchy = Create();
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        Publish(hierarchy, direction, 300_000d);
        var generation = hierarchy.AuthoritativeGeneration;
        var initialMisses = hierarchy.Telemetry.CacheMisses;
        for (var frame = 0; frame < 32; frame++) Update(hierarchy, direction, 300_000d);
        Assert(hierarchy.AuthoritativeGeneration == generation && hierarchy.Telemetry.CacheMisses == initialMisses,
            "stationary observation performs no selection/preparation churn");
        var peak = 0;
        for (var step = 1; step <= 24; step++)
        {
            var east = new Double3(-direction.Z, 0d, direction.X).Normalized();
            var moved = (direction + east * (step * 2_000d / Radius)).Normalized();
            Publish(hierarchy, moved, 100_000d);
            peak = Math.Max(peak, hierarchy.AuthoritativePatchCount);
        }
        Assert(peak <= PlanetaryDynamicAnchoredSurface.DefaultActiveCapacity &&
            hierarchy.Telemetry.ResidentPatchCount <= PlanetaryDynamicAnchoredSurface.DefaultCacheCapacity &&
            hierarchy.Telemetry.RejectedGenerationReason == PlanetaryDynamicGenerationRejectReason.None,
            "batched background preparation remains bounded by active and cache capacity");
    }

    private static void StationaryRotationRetainsPhysicalNeighborhood()
    {
        var hierarchy = Create();
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        Publish(hierarchy, direction, 1_000d);
        var generation = hierarchy.AuthoritativeGeneration;
        var misses = hierarchy.Telemetry.CacheMisses;
        var resident = hierarchy.Telemetry.ResidentPatchCount;
        var demanded = hierarchy.Telemetry.DemandedPatchCount;
        var camera = CameraAt(direction, 1_000d);
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        for (var step = 0; step < 72; step++)
        {
            var angle = step * Math.PI * 2d / 72d;
            var forward = (frame.North * Math.Cos(angle) + frame.East * Math.Sin(angle)).Normalized();
            hierarchy.Update(camera, forward, Fov, Aspect, Viewport);
            hierarchy.AcknowledgeGpuGeneration(hierarchy.ActiveGeneration);
        }
        Assert(hierarchy.AuthoritativeGeneration == generation && !hierarchy.HasPendingGeneration &&
            hierarchy.Telemetry.CacheMisses == misses &&
            hierarchy.Telemetry.ResidentPatchCount == resident &&
            hierarchy.Telemetry.DemandedPatchCount == demanded &&
            hierarchy.Telemetry.FramePreparations == 0 &&
            hierarchy.Telemetry.LastSelectionMilliseconds == 0d &&
            hierarchy.RotationReuseCount == 72 &&
            hierarchy.RetainedNeighborhoodRadiusMetres >=
                PlanetaryDynamicAnchoredSurface.MinimumRetainedNeighborhoodRadiusMetres &&
            hierarchy.CameraToResidencyCenterMetres == 0d,
            "a stationary 360-degree look preserves one retained physical neighborhood without selection, publication, or cache churn");
        Console.WriteLine($"Retained near-field rotation: patches={demanded}; resident={resident}; " +
            $"radius={hierarchy.RetainedNeighborhoodRadiusMetres:R}m; " +
            $"recenter={hierarchy.ResidencyRecenterDistanceMetres:R}m; " +
            $"generations=1; selections=0; misses=0; preparations=0");
    }

    private static void MovingReferenceFrameReusesPersistentTopology()
    {
        var hierarchy = Create();
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        Publish(hierarchy, direction, 10.004d);
        var generation = hierarchy.AuthoritativeGeneration;
        var before = hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount)
            .Select(value => ((int)value.Level, (PlanetaryPatchEdge)value.StitchMask))
            .OrderBy(value => value.Item1).ThenBy(value => value.Item2).ToArray();
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var moved = (direction + frame.East * (18_000d / Radius)).Normalized();
        var bodyMotion = Math.Sqrt((CameraAt(moved,10.004d)-CameraAt(direction,10.004d)).LengthSquared);
        var firstBefore = hierarchy.AuthoritativePatches[0];

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        Update(hierarchy, moved, 10.004d);
        var callbackMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Assert(hierarchy.AuthoritativeGeneration == generation && hierarchy.Visible &&
            hierarchy.Telemetry.SelectionPending && !hierarchy.HasPendingGeneration &&
            hierarchy.Telemetry.MainThreadTerrainMilliseconds == 0d,
            "ordinary moving-reference input retains the immutable owner and queues topology translation off the render callback");

        PublishCurrent(hierarchy, moved, 10.004d);
        var after = hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount)
            .Select(value => ((int)value.Level, (PlanetaryPatchEdge)value.StitchMask))
            .OrderBy(value => value.Item1).ThenBy(value => value.Item2).ToArray();
        var firstAfter = hierarchy.AuthoritativePatches[0];
        Console.WriteLine($"Moving reference candidate: generation={generation}->{hierarchy.AuthoritativeGeneration}; " +
            $"before={before.Length}; after={after.Length}; topologyEqual={before.SequenceEqual(after)}; " +
            $"translations={hierarchy.PersistentTopologyTranslationCount}; delta={hierarchy.Telemetry.DescriptorDeltaCount}; " +
            $"overlap={hierarchy.Telemetry.DemandOverlapPercent:R}%; motion={bodyMotion:R}m; " +
            $"first={firstBefore.Face}/{firstBefore.Level}/{firstBefore.X}/{firstBefore.Y}->" +
            $"{firstAfter.Face}/{firstAfter.Level}/{firstAfter.X}/{firstAfter.Y}");
        Assert(hierarchy.AuthoritativeGeneration != generation && before.SequenceEqual(after) &&
            hierarchy.PersistentTopologyTranslationCount > 0 &&
            hierarchy.Telemetry.DescriptorDeltaCount > 0 &&
            hierarchy.Telemetry.DescriptorDeltaCount < hierarchy.AuthoritativePatchCount * 2 &&
            hierarchy.Telemetry.DemandOverlapPercent > 0d,
            "the replacement remaps one persistent LOD/stitch topology and prepares only entering/leaving canonical descriptor deltas");
        Console.WriteLine($"Moving reference topology: callback={callbackMilliseconds:R}ms; " +
            $"patches={hierarchy.AuthoritativePatchCount}; delta={hierarchy.Telemetry.DescriptorDeltaCount}; " +
            $"overlap={hierarchy.Telemetry.DemandOverlapPercent:R}%; translations={hierarchy.PersistentTopologyTranslationCount}; " +
            $"queueLatency={hierarchy.Telemetry.LastSelectionQueueLatencyMilliseconds:R}ms");
    }

    private static void FailureRetainsPreviousOwner()
    {
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        var hierarchy = Create(); Publish(hierarchy, direction, 100_000d);
        var generation = hierarchy.AuthoritativeGeneration;
        hierarchy.InjectNextPreparationFailure(PlanetaryDynamicSurfaceFailure.GpuAllocation);
        var east = new Double3(-direction.Z, 0d, direction.X).Normalized();
        var moved = (direction + east * .08d).Normalized();
        Update(hierarchy, moved, 100_000d);
        for (var frame = 0; frame < 10_000 && hierarchy.Telemetry.SelectionPending; frame++)
        {
            Thread.Sleep(1);
            Update(hierarchy, moved, 100_000d);
        }
        Assert(hierarchy.AuthoritativeGeneration == generation && hierarchy.Visible &&
            hierarchy.RequiresGlobalFallback && !hierarchy.HasPendingGeneration &&
            hierarchy.Telemetry.RejectedGenerationReason ==
                PlanetaryDynamicGenerationRejectReason.InjectedFailure,
            "incomplete replacement never suppresses the previous/global owner");
    }

    private static void FrameSynchronizedA1A5Telemetry()
    {
        const int frames = 180;
        var start = BodyFixedGeography.DirectionFromLatitudeLongitude(
            28.5721d * Math.PI / 180d, -80.648d * Math.PI / 180d);
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        Assert(SurfaceAnchor.TryCreate(EarthBodyId,
            new TerrainAuthorityVersion(terrain.SourceId, terrain.Version), start, 0d,
            out var anchor) == SurfaceAnchorCreationStatus.Success,
            "A1-A5 telemetry anchor is canonical");

        RunPhase("A1 anchored stationary 1x", 0d, rotateView: false, activeAnchor: anchor);
        RunPhase("A2 anchored time warp", 0d, rotateView: true, activeAnchor: anchor);
        RunPhase("A3 unanchored moving frame", 100d, rotateView: false, null);
        RunPhase("A4 unanchored lateral", 200d, rotateView: false, null);
        RunPhase("A5 unanchored time warp", 400d, rotateView: false, null);

        void RunPhase(string name, double metresPerFrame, bool rotateView, SurfaceAnchor? activeAnchor)
        {
            var hierarchy = Create();
            hierarchy.Update(CameraAt(start, 10.004d), -start, Fov, Aspect, 1440d,
                activeAnchor, .05d);
            for (var settle = 0; settle < 10_000 &&
                 (!hierarchy.Visible || hierarchy.HasPendingGeneration || hierarchy.Telemetry.SelectionPending); settle++)
            {
                hierarchy.AcknowledgeGpuGeneration(hierarchy.ActiveGeneration);
                hierarchy.Update(CameraAt(start, 10.004d), -start, Fov, Aspect, 1440d,
                    activeAnchor, .05d);
                if (!hierarchy.Visible || hierarchy.HasPendingGeneration || hierarchy.Telemetry.SelectionPending)
                    Thread.Sleep(1);
            }
            Assert(hierarchy.Visible && !hierarchy.HasPendingGeneration,
                $"{name} initial immutable generation settles");
            var callbacks = new double[frames];
            var generations = 0; var descriptorDeltas = 0; var selectionFrames = 0;
            var mainThreadMaximum = 0d; var previousGeneration = hierarchy.AuthoritativeGeneration;
            var selectionMaximum = 0d; var preparationMaximum = 0d;
            for (var frameIndex = 0; frameIndex < frames; frameIndex++)
            {
                var surfaceFrame = PlanetarySurfaceFrame.AtDirection(start);
                var distance = metresPerFrame * frameIndex;
                var direction = (start + surfaceFrame.East * (distance / Radius)).Normalized();
                var forward = rotateView
                    ? (-direction * Math.Cos(frameIndex * Math.PI / frames) +
                       surfaceFrame.East * Math.Sin(frameIndex * Math.PI / frames)).Normalized()
                    : -direction;
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                hierarchy.Update(CameraAt(direction, 10.004d), forward, Fov, Aspect, 1440d,
                    activeAnchor, .05d);
                callbacks[frameIndex] = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                hierarchy.AcknowledgeGpuGeneration(hierarchy.ActiveGeneration);
                if (hierarchy.Telemetry.SelectionPending)
                {
                    selectionFrames++;
                    Thread.Sleep(1);
                }
                mainThreadMaximum = Math.Max(mainThreadMaximum,
                    hierarchy.Telemetry.MainThreadTerrainMilliseconds);
                selectionMaximum = Math.Max(selectionMaximum, hierarchy.Telemetry.LastSelectionMilliseconds);
                preparationMaximum = Math.Max(preparationMaximum,
                    hierarchy.Telemetry.LastBackgroundPreparationMilliseconds);
                if (hierarchy.AuthoritativeGeneration != previousGeneration)
                {
                    generations++;
                    previousGeneration = hierarchy.AuthoritativeGeneration;
                    descriptorDeltas = Math.Max(descriptorDeltas,
                        hierarchy.Telemetry.DescriptorDeltaCount);
                }
            }
            var sorted = callbacks.Order().ToArray();
            double Percentile(double value) => sorted[Math.Min(sorted.Length - 1,
                Math.Max(0, (int)Math.Ceiling(value * sorted.Length) - 1))];
            Console.WriteLine($"Phase 1B {name}: callback median={Percentile(.5):R}ms; " +
                $"p95={Percentile(.95):R}ms; p99={Percentile(.99):R}ms; max={callbacks.Max():R}ms; " +
                $"generationChanges={generations}; descriptorDeltaMax={descriptorDeltas}; " +
                $"selectionFrames={selectionFrames}; mainThreadTerrainMax={mainThreadMaximum:R}ms; " +
                $"selectionMax={selectionMaximum:R}ms; preparationMax={preparationMaximum:R}ms; " +
                $"topologyReuse={hierarchy.PersistentTopologyReuseCount}; " +
                $"topologyTranslations={hierarchy.PersistentTopologyTranslationCount}");
            Assert(mainThreadMaximum < 16.67d && callbacks.Max() < 16.67d,
                $"{name} keeps ordinary terrain work off the render callback budget");
        }
    }

    private static void SolarScaleDeactivationIsIdle()
    {
        var direction = new Double3(.31d, .72d, .62d).Normalized();
        var hierarchy = Create(); Publish(hierarchy, direction, 100_000d);
        hierarchy.Deactivate();
        var telemetry = hierarchy.Telemetry;
        Assert(!hierarchy.Visible && !hierarchy.HasPendingGeneration &&
            telemetry.DemandedPatchCount == 0 && telemetry.AuthoritativePatchCount == 0 &&
            telemetry.PendingPatchCount == 0 && telemetry.PreparedPendingPatchCount == 0 &&
            telemetry.FramePreparations == 0 && telemetry.FrameUploadBytes == 0 &&
            telemetry.PreparationQueueDepth == 0 && telemetry.MainThreadTerrainMilliseconds == 0d &&
            telemetry.LastBackgroundPreparationMilliseconds == 0d &&
            telemetry.CompleteCoverage && telemetry.GlobalFallbackActive,
            "Solar-scale deactivation retains cache residency but performs no near-field work");
    }

    private static PlanetaryDynamicAnchoredSurface Create() => new(EarthBodyId, Radius,
        PlanetaryTerrainDefinition.EarthProductionCubeV5,
        (uint)PlanetaryPhysicalSurface.ModifierGenerationId);

    private static void Update(PlanetaryDynamicAnchoredSurface hierarchy, in Double3 direction,
        double altitude)
    {
        var unit = direction.Normalized();
        hierarchy.Update(CameraAt(unit, altitude), -unit, Fov, Aspect, Viewport);
    }

    private static Double3 CameraAt(in Double3 direction, double altitude)
    {
        var unit = direction.Normalized();
        var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(unit);
        return unit * (Radius + physical.FinalHeightMetres + altitude);
    }

    private static void Publish(PlanetaryDynamicAnchoredSurface hierarchy, in Double3 direction,
        double altitude)
    {
        Update(hierarchy, direction, altitude);
        PublishCurrent(hierarchy, direction, altitude);
    }

    private static void PublishCurrent(PlanetaryDynamicAnchoredSurface hierarchy, in Double3 direction,
        double altitude)
    {
        for (var frame = 0; frame < 10_000 &&
             (!hierarchy.Visible || hierarchy.HasPendingGeneration || hierarchy.Telemetry.SelectionPending); frame++)
        {
            hierarchy.AcknowledgeGpuGeneration(hierarchy.ActiveGeneration);
            Update(hierarchy, direction, altitude);
            if (!hierarchy.Visible || hierarchy.HasPendingGeneration || hierarchy.Telemetry.SelectionPending)
                Thread.Sleep(1);
        }
        Assert(hierarchy.Visible && !hierarchy.HasPendingGeneration && !hierarchy.Telemetry.SelectionPending,
            "bounded descriptor generation publishes in the test deadline");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
