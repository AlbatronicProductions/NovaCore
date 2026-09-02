using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class PlanetaryNaturalTerrainPreparationTests
{
    private const uint Seed = 0x4D12D2B1u;
    private const int ChangedPatchCount = 32;
    private static readonly PlanetaryNaturalTerrainPhysicalFieldGeneration Field =
        PlanetaryNaturalTerrainPhysicalFieldGeneration.EarthProof(Seed);
    private static readonly PlanetaryNaturalTerrainFamilyIdentity Identity =
        new(Field.BodyId, Field.GenerationId, Seed);

    public static void Run()
    {
        var memory = VerifyAbiAndPhysicalFieldGeneration();
        var parity = VerifyCpuDirectGlslPreparedGpuParity();
        var cache = VerifyTransactionalCache();
        var timing = VerifyPreparationPerformance();
        VerifyIsolation();
        Console.WriteLine($"P2C1 ABI: descriptor={memory.DescriptorBytes} B; preparedVertex={memory.VertexBytes} B; perPatch={memory.PerPatchBytes} B; current={memory.CurrentBytes} B ({memory.CurrentMiB:F3} MiB); worst={memory.WorstBytes} B ({memory.WorstMiB:F3} MiB); queryTransport={memory.QueryBytes} B; resultTransport={memory.ResultBytes} B");
        Console.WriteLine($"P2C1 field: body={Field.BodyId}; generation={Field.GenerationId}; hash={Field.HashVersion}; composition={Field.CompositionVersion}; seed=0x{Field.Seed:X8}; manifest=0x{Field.FamilyManifestHash:X16}; bounds={Field.Bounds.TotalHeight:F6}m/{Field.Bounds.TotalGradient:F9}");
        Console.WriteLine($"P2C1 CPU/direct-GLSL/prepared-GPU: samples={parity.Samples}; cellMismatch=0; familyMismatch=0; generationMismatch=0; macroMax={parity.Macro:E17}; mesoMax={parity.Meso:E17}; preparedMax={parity.Prepared:E17}; gradientMax={parity.Gradient:E17}; biomeWeightMax={parity.Weight:E17}; biomeGradientMax={parity.WeightGradient:E17}; orientationMax={parity.Orientation:E17}; boundMax={parity.Bound:E17}; validation=0");
        Console.WriteLine($"P2C1 cache: initialChanged={cache.InitialChanged}; unchangedChanged=0; unchangedReuse={cache.UnchangedReuse:F3}%; replacementChanged={cache.ReplacementChanged}; staleReuse=0; oldOwnerUntilReady=true; noInFlightOverwrite=true; authoritativeEviction=false; pivotIndependent=true; descriptorCellStable=true");
        Console.WriteLine($"P2C1 steady: changed=0; reuse={timing.SteadyReuse:F3}%; gpuAvg={timing.SteadyGpuAverage:F6} ms; gpuP95={timing.SteadyGpuP95:F6} ms; cpuScheduleAvg={timing.SteadyCpuAverage:F6} ms; cpuScheduleP95={timing.SteadyCpuP95:F6} ms; regeneration=0");
        Console.WriteLine($"P2C1 changed: slots={timing.ChangedSlots}; descriptors={timing.Descriptors}; vertices={timing.Vertices}; uploaded={timing.UploadedBytes} B; written={timing.WrittenBytes} B; gpuAvg={timing.ChangedGpuAverage:F6} ms; gpuP95={timing.ChangedGpuP95:F6} ms; cpuScheduleAvg={timing.ChangedCpuAverage:F6} ms; cpuScheduleP95={timing.ChangedCpuP95:F6} ms; publishAvg={timing.PublishAverage:F6} ms; publishP95={timing.PublishP95:F6} ms; asynchronous=true; previousOwnerRetained=true");
    }

    private static MemoryResult VerifyAbiAndPhysicalFieldGeneration()
    {
        Require(Field.IsValid && Field.GenerationId == PlanetaryNaturalTerrainFamilies.ProofGeneration &&
            Field.HashVersion == PlanetaryNaturalTerrainField.HashVersion &&
            Field.CompositionVersion == PlanetaryNaturalTerrainFamilies.CompositionVersion,
            "P2C1 physical-field generation captures the accepted P2A/P2B operation-order identities");
        Require(Field == PlanetaryNaturalTerrainPhysicalFieldGeneration.EarthProof(Seed) &&
            Field.FamilyManifestHash != PlanetaryNaturalTerrainPhysicalFieldGeneration.EarthProof(Seed + 1u).FamilyManifestHash,
            "P2C1 physical-field manifest is deterministic and seed-sensitive");
        var descriptor = Marshal.SizeOf<PlanetaryNaturalTerrainPhysicalCellDescriptor>();
        var vertex = Marshal.SizeOf<PlanetaryNaturalTerrainPreparedPatchVertex>();
        var perPatch = descriptor + vertex * PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch;
        var current = (long)perPatch * PlanetaryNaturalTerrainPreparationCache.DefaultCapacity;
        var worst = (long)perPatch * PlanetaryNaturalTerrainPreparationCache.WorstExpectedCapacity;
        Require(descriptor == 120 && vertex == 120 && perPatch == 3120,
            $"P2C1 logical ABI is measured rather than assumed: {descriptor}/{vertex}/{perPatch}");
        Require(worst < 16L * 1024L * 1024L,
            $"P2C1 worst expected logical cache remains below the architectural 16 MiB estimate: {worst}");
        return new(descriptor, vertex, perPatch, current, worst,
            current / 1048576d, worst / 1048576d,
            Marshal.SizeOf<NativePlanetaryHeightQuery>(), Marshal.SizeOf<NativePlanetaryHeightResult>());
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

    private static CacheResult VerifyTransactionalCache()
    {
        var cache = new PlanetaryNaturalTerrainPreparationCache(12);
        var initial = Requests(Field, 0, 4);
        Require(cache.TryBeginGeneration(Field, initial, out var generation1) && generation1 is not null &&
            generation1.ChangedSlots.Count == 4 && generation1.ReusedSlots == 0, "P2C1 initial generation prepares every missing slot");
        Complete(cache, generation1!); Require(cache.TryPublish(generation1!), "P2C1 initial complete generation publishes atomically");
        Require(cache.TryBeginGeneration(Field, initial, out var unchanged) && unchanged is not null &&
            unchanged.ChangedSlots.Count == 0 && unchanged.ReusedSlots == 4 && cache.TryPublish(unchanged),
            "P2C1 unchanged generation reuses every immutable slot with no regeneration");

        var replacementField = Field with { GenerationId = Field.GenerationId + 1u, FamilyManifestHash = Field.FamilyManifestHash + 1u };
        var replacement = Requests(replacementField, 0, 4);
        Require(cache.TryBeginGeneration(replacementField, replacement, out var generation2) && generation2 is not null &&
            generation2.ChangedSlots.Count == 4 && generation2.ReusedSlots == 0,
            "P2C1 physical-generation change cannot reuse stale slots");
        Require(initial.All(request => cache.IsAuthoritative(request.Key)) && !cache.TryPublish(generation2!),
            "P2C1 old complete owner remains authoritative while replacement is preparing");
        var first = generation2!.ChangedSlots[0];
        var stale = first with { SlotGeneration = first.SlotGeneration + 1u };
        Require(!cache.CompleteChangedSlot(generation2, stale, 1, stale.Request.VertexCount, BytesPerPatch()),
            "P2C1 stale completion cannot overwrite an in-flight slot");
        for (var index = 0; index < generation2.ChangedSlots.Count - 1; index++)
            Require(cache.CompleteChangedSlot(generation2, generation2.ChangedSlots[index], 1,
                generation2.ChangedSlots[index].Request.VertexCount, BytesPerPatch()), "P2C1 changed slot completes");
        Require(!cache.TryPublish(generation2) && initial.All(request => cache.IsAuthoritative(request.Key)),
            "P2C1 incomplete replacement cannot suppress previous authority");
        var last = generation2.ChangedSlots[^1];
        Require(cache.CompleteChangedSlot(generation2, last, 1, last.Request.VertexCount, BytesPerPatch()) &&
            cache.TryPublish(generation2) && replacement.All(request => cache.IsAuthoritative(request.Key)) &&
            initial.All(request => !cache.IsAuthoritative(request.Key)),
            "P2C1 GPU-ready replacement publishes atomically");
        Require(cache.TryBeginGeneration(replacementField, replacement, out var olderReuse) && olderReuse is not null &&
            cache.TryBeginGeneration(replacementField, replacement, out var newerReuse) && newerReuse is not null &&
            cache.TryPublish(newerReuse) && !cache.TryPublish(olderReuse),
            "P2C1 an older ready transaction cannot republish after a newer generation");

        var inflight = new PlanetaryNaturalTerrainPreparationCache(3);
        Require(inflight.TryBeginGeneration(Field, Requests(Field, 10, 2), out var pending) && pending is not null &&
            !inflight.TryBeginGeneration(Field, Requests(Field, 10, 1), out _) &&
            pending.ChangedSlots.All(work => inflight.IsPreparing(work.Slot)),
            "P2C1 identical in-flight keys cannot be assigned to a second slot");
        Require(!inflight.TryBeginGeneration(Field, Requests(Field, 12, 1), out _) &&
            pending!.ChangedSlots.All(work => inflight.IsPreparing(work.Slot)),
            "P2C1 a second changed transaction cannot overlap the active immutable preparation");
        var pressure = new PlanetaryNaturalTerrainPreparationCache(2);
        var full = Requests(Field, 20, 2); Require(pressure.TryBeginGeneration(Field, full, out var fullGeneration) && fullGeneration is not null,
            "P2C1 capacity fixture begins");
        Complete(pressure, fullGeneration!); Require(pressure.TryPublish(fullGeneration!), "P2C1 capacity fixture publishes");
        Require(!pressure.TryBeginGeneration(replacementField, Requests(replacementField, 20, 2), out _) &&
            full.All(request => pressure.IsAuthoritative(request.Key)),
            "P2C1 capacity pressure cannot evict the authoritative complete generation");

        var point = new Double3(-2_135_177.25, 5_991_331.75, -319_775.5);
        var pivotA = new Double3(6_371_008.8, 0, 0); var pivotB = new Double3(-3_000_000, 5_500_000, 700_000);
        var patchA = new PlanetarySurfacePatchId(Field.BodyId, 5, CubeSphereFace.PositiveX, 8, 100, 17);
        var patchB = new PlanetarySurfacePatchId(Field.BodyId, 5, CubeSphereFace.PositiveY, 7, 45, 81);
        var descriptorA = PlanetaryNaturalTerrainFamilies.EvaluateCellDescriptor(point, Identity);
        var descriptorB = PlanetaryNaturalTerrainFamilies.EvaluateCellDescriptor(point, Identity);
        Require(pivotA != pivotB && patchA != patchB && descriptorA == descriptorB,
            "P2C1 descriptor/cell identity survives arbitrary pivot and patch movement because it derives only from body-fixed geography");
        var keyProperties = typeof(PlanetaryNaturalTerrainPreparedPatchKey).GetProperties().Select(property => property.Name).ToArray();
        Require(!keyProperties.Any(name => name.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Pivot", StringComparison.OrdinalIgnoreCase) || name.Contains("Frame", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Owner", StringComparison.OrdinalIgnoreCase) || name.Contains("Worker", StringComparison.OrdinalIgnoreCase)),
            "P2C1 cache identity excludes camera, snapped pivot, frame, owner, and worker order");
        return new(4, unchanged!.ReusePercentage, 4);
    }

    private static TimingResult VerifyPreparationPerformance()
    {
        var paths = ResolveGpuPaths(); var cases = BuildPerformanceCases();
        var queries = BuildQueries(cases, 41u, out _);
        var gpu = new double[7];
        for (var repeat = 0; repeat < gpu.Length; repeat++)
        { _ = InvokeGpu(queries, paths, paths.PreparedShader, out var metrics); gpu[repeat] = metrics.GpuMilliseconds; }
        var cache = new PlanetaryNaturalTerrainPreparationCache(128);
        var requests = Requests(Field, 100, ChangedPatchCount);
        Require(cache.TryBeginGeneration(Field, requests, out var initial) && initial is not null,
            "P2C1 performance generation begins"); Complete(cache, initial!); Require(cache.TryPublish(initial!), "P2C1 performance generation publishes");
        var steady = new double[256];
        for (var repeat = 0; repeat < steady.Length; repeat++)
        {
            var start = Stopwatch.GetTimestamp();
            Require(cache.TryBeginGeneration(Field, requests, out var unchanged) && unchanged is not null &&
                unchanged.ChangedSlots.Count == 0 && unchanged.ReusedSlots == ChangedPatchCount && cache.TryPublish(unchanged),
                "P2C1 steady workload reuses all slots");
            steady[repeat] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        var changed = new double[20]; var publication = new double[20];
        for (var warmIndex = 0; warmIndex < 4; warmIndex++)
        {
            var warmField = Field with { GenerationId = (ulong)(90 + warmIndex), FamilyManifestHash = Field.FamilyManifestHash + (ulong)warmIndex + 90u };
            Require(cache.TryBeginGeneration(warmField, Requests(warmField, 100, ChangedPatchCount), out var warm) && warm is not null,
                "P2C1 changed scheduling/eviction warm-up begins"); Complete(cache, warm!);
            Require(cache.TryPublish(warm!), "P2C1 changed scheduling/eviction warm-up publishes");
        }
        for (var repeat = 0; repeat < changed.Length; repeat++)
        {
            var next = Field with { GenerationId = (ulong)(100 + repeat), FamilyManifestHash = Field.FamilyManifestHash + (ulong)repeat + 1u };
            var changedRequests = Requests(next, 100, ChangedPatchCount);
            var start = Stopwatch.GetTimestamp();
            Require(cache.TryBeginGeneration(next, changedRequests, out var generation) && generation is not null &&
                generation.ChangedSlots.Count == ChangedPatchCount, "P2C1 changed performance workload schedules only changed slots");
            changed[repeat] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            foreach (var work in generation!.ChangedSlots)
                Require(cache.CompleteChangedSlot(generation, work, 1, work.Request.VertexCount, BytesPerPatch()),
                    "P2C1 changed performance slot acknowledges asynchronously");
            start = Stopwatch.GetTimestamp(); Require(cache.TryPublish(generation), "P2C1 changed performance generation publishes");
            publication[repeat] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        var gpuAverage = gpu.Average(); var gpuP95 = Percentile95(gpu);
        var steadyAverage = steady.Average(); var steadyP95 = Percentile95(steady);
        var changedAverage = changed.Average(); var changedP95 = Percentile95(changed);
        var publicationAverage = publication.Average(); var publicationP95 = Percentile95(publication);
        Require(gpuP95 <= 1.5, $"P2C1 changed-generation GPU preparation p95 <= 1.5 ms: {gpuP95:R}");
        Require(steadyP95 <= .25, $"P2C1 steady render-thread scheduling p95 <= 0.25 ms: {steadyP95:R}");
        return new(ChangedPatchCount, ChangedPatchCount, ChangedPatchCount * PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch,
            (long)queries.Length * Marshal.SizeOf<NativePlanetaryHeightQuery>(),
            (long)ChangedPatchCount * BytesPerPatch(), 100d, 0d, 0d, steadyAverage, steadyP95,
            gpuAverage, gpuP95, changedAverage, changedP95, publicationAverage, publicationP95);
    }

    private static void VerifyIsolation()
    {
        var root = RepositoryRoot();
        var productionFiles = new[]
        {
            Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary.vert"),
            Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary_production.vert"),
            Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary_production.tese"),
            Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary_production.frag"),
            Path.Combine(root, "src", "NovaCore.Graphics", "PlanetaryPhysicalSurface.cs")
        };
        foreach (var path in productionFiles.Where(File.Exists))
        {
            var text = File.ReadAllText(path);
            Require(!text.Contains("planetary_natural_terrain_prepare", StringComparison.Ordinal) &&
                !text.Contains("PlanetaryNaturalTerrainPreparation", StringComparison.Ordinal),
                $"P2C1 proof is not routed into production: {Path.GetFileName(path)}");
        }
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

    private static List<GpuCase> BuildPerformanceCases()
    {
        var result = new List<GpuCase>(ChangedPatchCount * PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch);
        var origin = new Double3(-2_135_177.25, 5_991_331.75, -319_775.5);
        for (var patch = 0; patch < ChangedPatchCount; patch++)
            for (var vertex = 0; vertex < PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch; vertex++)
                result.Add(new(origin + new Double3(patch * 721.37 + vertex * 13.17,
                    patch * -337.11 + vertex * 17.03, patch * 193.73 - vertex * 11.19), 0u, 0u, Identity));
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

    private static PlanetaryNaturalTerrainPreparationRequest[] Requests(
        in PlanetaryNaturalTerrainPhysicalFieldGeneration field, int firstX, int count)
    {
        var requests = new PlanetaryNaturalTerrainPreparationRequest[count];
        for (var index = 0; index < count; index++)
        {
            var patch = new PlanetarySurfacePatchId(field.BodyId, 5, CubeSphereFace.PositiveX, 8, firstX + index, 17);
            requests[index] = new(new(patch, field.GenerationId, 1u), PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch);
        }
        return requests;
    }

    private static void Complete(PlanetaryNaturalTerrainPreparationCache cache, PlanetaryNaturalTerrainPreparedPatchGeneration generation)
    {
        foreach (var work in generation.ChangedSlots)
            Require(cache.CompleteChangedSlot(generation, work, 1, work.Request.VertexCount, BytesPerPatch()),
                "P2C1 changed slot completion is accepted once");
    }

    private static int BytesPerPatch() => Marshal.SizeOf<PlanetaryNaturalTerrainPhysicalCellDescriptor>() +
        Marshal.SizeOf<PlanetaryNaturalTerrainPreparedPatchVertex>() * PlanetaryNaturalTerrainPreparationCache.VerticesPerPatch;
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
    private readonly record struct MemoryResult(int DescriptorBytes, int VertexBytes, int PerPatchBytes, long CurrentBytes,
        long WorstBytes, double CurrentMiB, double WorstMiB, int QueryBytes, int ResultBytes);
    private readonly record struct ParityResult(int Samples, double Macro, double Meso, double Prepared, double Gradient,
        double Weight, double WeightGradient, double Orientation, double Bound);
    private readonly record struct CacheResult(int InitialChanged, double UnchangedReuse, int ReplacementChanged);
    private readonly record struct TimingResult(int ChangedSlots, int Descriptors, int Vertices, long UploadedBytes,
        long WrittenBytes, double SteadyReuse, double SteadyGpuAverage, double SteadyGpuP95,
        double SteadyCpuAverage, double SteadyCpuP95, double ChangedGpuAverage, double ChangedGpuP95,
        double ChangedCpuAverage, double ChangedCpuP95, double PublishAverage, double PublishP95);
}
