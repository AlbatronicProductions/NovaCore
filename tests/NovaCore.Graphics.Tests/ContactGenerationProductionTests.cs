using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using static ContactGenerationFixture;

/// <summary>Headless real-data integration and bounded, query-inclusive CPU cost. No renderer or native query path.</summary>
internal static class ContactGenerationProductionTests
{
    private static void Check(bool value, string message) => ContactGenerationFixture.Check(value, message);
    internal static void Run()
    {
        var root = GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root, "assets", "earth", "runtime"), out var error), error);
        Check(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null, out _, out var path, out error), error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path, out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, root, out var acquired) == PhysicalSurfaceQueryStatus.Ready, "production authority acquisition");
        var query = acquired!; var system = SolAnalyticalDefinition.Instance; var graph = Graph();
        var eval = new ReferenceFrameEvaluation[system.Count]; var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count]; var stagingRoots = new FrameTransform[system.Count];
        Check(ContactBodyMotion.TryEvaluate(system, graph, new(6), default, eval, roots, staging, stagingRoots, out var body) == ContactGenerationStatus.Ready, "production Sol body motion");
        Console.WriteLine("Contact production authority: " + JsonSerializer.Serialize(query.Authority));
        Console.WriteLine("Contact cost protocol: 64 warmup batches; 201 individual samples per path; alternating complete/query-only order; complete includes fresh body evaluation; setup/acquisition excluded; zero-byte allocation window separate; milliseconds CPU, not GPU.");
        foreach (var site in new[] { ("Florida", FloridaFacilitySupport.Region.Up), ("regional", Geo(28.5, -80.5)), ("inland", Geo(39.1, -106.8)) })
        foreach (var count in new[] { 1, 4, 8 })
        {
            var center = query.Query(6, site.Item2); Check(center.IsReady, "center physical readiness");
            // Map the authored lander's +Z-down axis onto local down; no camera or rendering frame enters this fixture.
            var down = -site.Item2; var axis = Double3.Cross(Double3.UnitZ, down).Normalized();
            var localOrientation = DoubleQuaternion.FromAxisAngle(axis, Math.Acos(Math.Clamp(Double3.Dot(Double3.UnitZ, down), -1, 1)));
            var orientation = (body.BodyFixedToRoot.Rotation * localOrientation).Normalized();
            var centerBody = site.Item2 * (query.Authority.ReferenceRadiusMetres + center.HeightMetres + 2);
            var position = body.BodyFixedToRoot.LocalToParent(centerBody);
            var velocity = body.VelocityRoot + Double3.Cross(body.AngularVelocityRoot, body.BodyFixedToRoot.LocalDirectionToParent(centerBody));
            var state = State(position, velocity, orientation, orientation.Conjugate().Rotate(body.AngularVelocityRoot));
            var view = state.CreateView(); var geometry = Geometry(count); var output = new SpacecraftContactObservation[count];
            bool Complete()
            {
                return ContactBodyMotion.TryEvaluate(system, graph, new(6), default, eval, roots, staging, stagingRoots, out var fresh) == ContactGenerationStatus.Ready &&
                    SpacecraftContactGenerator.Generate(view, state.CreateView().Revision, default, geometry, fresh, system, query, query.Authority, output).Succeeded;
            }
            Check(Complete(), "complete real terrain observation");
            // Repeat the generator's exact pre-query direction calculation for paired query-only attribution.
            var directions = output.Select(o => { var local = body.BodyFixedToRoot.ParentToLocal(o.FeaturePositionRoot); return local / Math.Sqrt(local.LengthSquared); }).ToArray();
            bool Queries()
            {
                var ready = true;
                for (var i = 0; i < directions.Length; i++) ready &= query.Query(6, directions[i]).IsReady;
                return ready;
            }
            for (var i = 0; i < 64; i++) Check(Complete() && Queries(), "warm real queries");
            var total = new long[201]; var terrain = new long[201]; var successes = 0;
            for (var i = 0; i < total.Length; i++)
            {
                if ((i & 1) == 0) { total[i] = Timed(Complete, ref successes); terrain[i] = Timed(Queries, ref successes); }
                else { terrain[i] = Timed(Queries, ref successes); total[i] = Timed(Complete, ref successes); }
            }
            Check(successes == 402, "all timed operations complete");
            // Banked Simulation measurement boundary: prevent GC allocation-context retirement from
            // being charged as workload allocation. This is outside timings, never production policy.
            Check(GC.TryStartNoGCRegion(1L << 20, disallowFullBlockingGC: true), "enter allocation measurement boundary");
            long bytes; var allocationSuccesses = 0;
            try
            {
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 32; i++) if (Complete()) allocationSuccesses++;
                bytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            }
            finally { GC.EndNoGCRegion(); }
            Check(allocationSuccesses == 32 && bytes == 0, $"query-inclusive allocation batches={allocationSuccesses} bytes={bytes}");
            var rootScale = Math.Max(Math.Abs(position.X), Math.Max(Math.Abs(position.Y), Math.Abs(position.Z)));
            var positionBar = 16 * (Math.BitIncrement(rootScale) - rootScale) + 128 * Math.Pow(2, -53) * query.Authority.ReferenceRadiusMetres;
            double maxWitness = 0, maxNormal = 0, maxGap = 0;
            foreach (var observation in output)
            {
                var canonical = query.Query(6, observation.TerrainDirectionBodyFixed); Check(canonical.IsReady, "direct canonical witness");
                maxWitness = Math.Max(maxWitness, Math.Sqrt((body.BodyFixedToRoot.LocalToParent(canonical.BodyFixedPositionMetres) - observation.TerrainPositionRoot).LengthSquared));
                maxNormal = Math.Max(maxNormal, Math.Sqrt((body.BodyFixedToRoot.LocalDirectionToParent(canonical.PhysicalNormal) - observation.PhysicalNormalRoot).LengthSquared));
                var qBody = body.BodyFixedToRoot.ParentToLocal(observation.FeaturePositionRoot);
                var gap = Math.Sqrt(qBody.LengthSquared) - query.Authority.ReferenceRadiusMetres - canonical.HeightMetres;
                maxGap = Math.Max(maxGap, Math.Abs(gap - observation.RadialSignedGapMetres));
                Check(observation.TerrainAuthority == query.Authority && observation.IsReady, "sole canonical physical owner");
            }
            // This compares repeated canonical queries and rotation, not independent physical-normal accuracy.
            Check(maxWitness <= positionBar && maxGap <= positionBar && maxNormal <= 128 * Math.Pow(2, -53), "canonical integration transport bounds");
            Check(state.CreateView().Revision == view.Revision, "no transaction mutation");
            Console.WriteLine(JsonSerializer.Serialize(new { site = site.Item1, features = count, samples = total.Length,
                completeMs = Summary(total), queryOnlyMs = Summary(terrain), allocatedBytes = bytes,
                meanAdditionalMs = (total.Average() - terrain.Average()) * 1000 / Stopwatch.Frequency,
                maxWitnessErrorM = maxWitness, maxNormalError = maxNormal, maxGapErrorM = maxGap, positionBarM = positionBar }));
        }
    }
    private static Double3 Geo(double lat, double lon) => BodyFixedGeography.DirectionFromLatitudeLongitude(lat * Math.PI / 180, lon * Math.PI / 180);
    private static long Timed(Func<bool> operation, ref int successes)
    { var start = Stopwatch.GetTimestamp(); if (operation()) successes++; return Stopwatch.GetTimestamp() - start; }
    private static object Summary(long[] ticks)
    {
        Array.Sort(ticks); double Ms(long value) => value * 1000d / Stopwatch.Frequency;
        return new { median = Ms(ticks[100]), p95 = Ms(ticks[190]), p99 = Ms(ticks[198]) };
    }
}
