using System.Diagnostics;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PhysicalSurfacePointQueryTests
{
    private const double Radius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
    // Independent contract ceiling; changing the implementation must not silently relax this test.
    private const double AngleBar = 5e-5;
    private static string Root => GraphicsTestHarness.RepositoryPath();
    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static double Height(Double3 d) => PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
        PlanetaryTerrainDefinition.EarthProductionCubeV5, d, PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
    private static Double3 Geo(double lat, double lon) => BodyFixedGeography.DirectionFromLatitudeLongitude(lat * Math.PI / 180, lon * Math.PI / 180);
    private static double Angle(Double3 a, Double3 b) => Math.Atan2(Math.Sqrt(Double3.Cross(a, b).LengthSquared), Double3.Dot(a, b));
    private static void LoadGlobal() => Check(EarthElevationDataset.TryLoad(Path.Combine(Root, "assets", "earth", "runtime"), out var error), error);
    private static string RegionalPath()
    {
        Check(TerrainAssetCache.TryResolveRequired(Root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var path, out var error), error);
        return path;
    }

    // Each harness case runs in a fresh process: there is deliberately no mutable dataset reset.
    internal static void RejectStaleSnapshot()
    {
        LoadGlobal();
        var fixture = GraphicsTestHarness.RepositoryPath("tests", "fixtures", "terrain", "local-payload2.nccube");
        Check(EarthLocalTerrainElevationDataset.TryLoad(fixture, out var error), error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(RegionalPath(), out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out var query) == PhysicalSurfaceQueryStatus.AuthorityMismatch && query is null,
            "later successful load must not relabel the once-published fixture as production");
    }

    internal static void Run()
    {
        Check(!default(PhysicalSurfacePointResult).IsReady, "default result cannot authorize contact");
        Check(PlanetaryPhysicalSurfacePointQuery.NormalAngularToleranceRadians <= AngleBar, "physical normal contract ceiling");
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, "invalid\0root", out _) == PhysicalSurfaceQueryStatus.InvalidInput, "malformed root");
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(0, Root, out _) == PhysicalSurfaceQueryStatus.InvalidInput, "invalid body");
        foreach (var body in new ulong[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, ulong.MaxValue })
            Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(body, Root, out _) == PhysicalSurfaceQueryStatus.UnsupportedBody, "no invented non-Earth terrain");
        var before = PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out var absent);
        Check(before == PhysicalSurfaceQueryStatus.RequiredDataNotReady && absent is null, "no incomplete global fallback");
        LoadGlobal();
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out _) == PhysicalSurfaceQueryStatus.RequiredDataNotReady,
            "global-only data is not complete physical authority, even for outside-coverage queries");
        var malformed = Path.Combine(Path.GetTempPath(), "novacore-surface-query-" + Guid.NewGuid().ToString("N") + ".nccube");
        try
        {
            File.WriteAllBytes(malformed, new byte[17]);
            Check(!EarthLocalTerrainElevationDataset.TryLoad(malformed, out _), "malformed data cannot publish");
            Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out _) == PhysicalSurfaceQueryStatus.RequiredDataNotReady, "malformed data cannot become ready");
        }
        finally { File.Delete(malformed); }
        Check(EarthLocalTerrainElevationDataset.TryLoad(RegionalPath(), out var error), error);
        var acquisition = Stopwatch.StartNew();
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out var acquired) == PhysicalSurfaceQueryStatus.Ready && acquired is not null, "acquire verified immutable data");
        acquisition.Stop();
        var query = acquired!;
        var firstQueryStart = Stopwatch.GetTimestamp();
        Check(query.Query(6, FloridaFacilitySupport.Region.Up).IsReady, "first query after acquisition");
        var firstQueryUs = (Stopwatch.GetTimestamp() - firstQueryStart) * 1e6 / Stopwatch.Frequency;
        CheckManifestFailures();
        Check(before == PhysicalSurfaceQueryStatus.RequiredDataNotReady && absent is null, "previous unavailable acquisition remains immutable");
        Check(query.Authority.BodyId == 6 && query.Authority.Terrain == new TerrainAuthorityVersion(2, 5) && query.Authority.PhysicalGeneration == 4 &&
            query.Authority.FacilitySupportIdentity == FloridaFacilitySupport.DefinitionIdentity && query.Authority.RegionalSha256.Length == 64,
            "full canonical physical identity");
        foreach (var d in new[] { default(Double3), new Double3(double.NaN, 0, 0), new Double3(double.PositiveInfinity, 0, 0), Double3.UnitX * 2 })
            Check(query.Query(6, d).Status == PhysicalSurfaceQueryStatus.InvalidInput, "invalid directions explicitly reject");
        Check(query.Query(7, Double3.UnitX).Status == PhysicalSurfaceQueryStatus.UnsupportedBody, "query body mismatch");

        var points = Points();
        var results = new PhysicalSurfacePointResult[points.Count];
        double maxHeight = 0, maxPosition = 0, maxNormal = 0, maxUnit = 0, maxPlane = 0;
        int ready = 0, unqualified = 0, creases = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var (name, d) = points[i];
            var result = results[i] = query.Query(6, d);
            if (!result.IsReady)
            {
                Check(result.Status == PhysicalSurfaceQueryStatus.NormalUnqualified, "only qualified or explicitly unresolved normal after readiness");
                Check(result.PhysicalNormal == default && result.BodyFixedPositionMetres == default, "failure cannot leak a substitute contact point");
                if (name == "clamp") creases++;
                unqualified++; continue;
            }
            Check(name != "clamp", "a measured clamp crease must not silently average into a contact normal");
            ready++;
            var canonical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(d,
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate).FinalHeightMetres;
            maxHeight = Math.Max(maxHeight, Math.Abs(result.HeightMetres - canonical));
            maxPosition = Math.Max(maxPosition, Math.Sqrt((result.BodyFixedPositionMetres - d.Normalized() * (Radius + canonical)).LengthSquared));
            maxUnit = Math.Max(maxUnit, Math.Abs(result.PhysicalNormal.LengthSquared - 1));
            Check(Double3.Dot(result.PhysicalNormal, d) > 0, "normal outward");
            // Independent basis (22.5 degrees), step (3/5 of returned fine distance), order (fourth).
            maxNormal = Math.Max(maxNormal, Angle(result.PhysicalNormal, Oracle(result.BodyFixedDirection, result.NormalSampleDistanceMetres * 3 / 5)));
            if (FloridaFacilitySupport.Region.Sample(d).Weight == 1)
                maxPlane = Math.Max(maxPlane, Angle(result.PhysicalNormal, FloridaFacilitySupport.Region.Up));
        }
        Check(ready > 500 && unqualified > 0 && creases == 12, "stress coverage retained");
        var fingerprint = Fingerprint(points, results);
        // Captured from the accepted eager-evaluation candidate before performance changes.
        Check(fingerprint == "3e08f86757588127099cbb100d3829b73d5e5f29b1de8d0f22ec83de28534912",
            "all 564 input/result bits and physical authority must remain identical");
        Console.WriteLine("Surface-point output fingerprint: " + fingerprint);
        Check(maxHeight <= 2e-6 && maxPosition <= 1e-3 && maxNormal <= AngleBar && maxPlane <= AngleBar && maxUnit <= 16 * 2.2204460492503131e-16,
            $"physical parity: height={maxHeight:R} position={maxPosition:R} normal={maxNormal:R} plane={maxPlane:R} unit={maxUnit:R}");
        // The canonical inner grading surface is an analytic plane. Nearby queries must remain
        // continuous there, independent of the ENU frames used to evaluate their derivatives.
        var support = FloridaFacilitySupport.Region;
        var nearLeft = query.Query(6, (support.Up * Radius + support.East * (32 - .001)).Normalized());
        var nearRight = query.Query(6, (support.Up * Radius + support.East * (32 + .001)).Normalized());
        Check(nearLeft.IsReady && nearRight.IsReady && Angle(nearLeft.PhysicalNormal, nearRight.PhysicalNormal) <= 2 * AngleBar,
            "qualified normal continuity on the analytic physical support plane");
        var previousGeneration = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.Generation3);
            for (int i = points.Count - 1; i >= 0; i--)
                Check(SameBits(results[i], query.Query(6, points[i].Direction)), "repeat/reorder/ambient generation cannot change immutable physical query");
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previousGeneration); }
        Check(EarthLocalTerrainElevationDataset.TryLoad("not-a-replacement.nccube", out error), error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, Root, out var again) == PhysicalSurfaceQueryStatus.Ready && again!.Authority == query.Authority,
            "later paths cannot mutate data or provenance");
        Console.WriteLine(JsonSerializer.Serialize(new { samples = points.Count, ready, unqualified, creases, maxHeight, maxPosition, maxNormal, maxPlane, maxUnit,
            acquisitionMs = acquisition.Elapsed.TotalMilliseconds, firstQueryUs, authority = query.Authority }));
        Measure(query, "Florida", FloridaFacilitySupport.Region.Up);
        Measure(query, "regional", Geo(28.5, -80.5));
        Measure(query, "inland", Geo(39.1, -106.8));
        Measure(query, "unqualified-pole", -Double3.UnitY);
    }

    private static bool SameBits(PhysicalSurfacePointResult a, PhysicalSurfacePointResult b)
    {
        static bool D(double x, double y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y);
        static bool V(Double3 x, Double3 y) => D(x.X, y.X) && D(x.Y, y.Y) && D(x.Z, y.Z);
        return a.Status == b.Status && a.Authority == b.Authority && V(a.BodyFixedDirection, b.BodyFixedDirection) &&
            V(a.BodyFixedPositionMetres, b.BodyFixedPositionMetres) && V(a.PhysicalNormal, b.PhysicalNormal) &&
            D(a.HeightMetres, b.HeightMetres) && D(a.NormalSampleDistanceMetres, b.NormalSampleDistanceMetres);
    }

    private static string Fingerprint(List<(string Name, Double3 Direction)> points, PhysicalSurfacePointResult[] results)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        // Fixed-order FP64 little-endian query inputs and outputs, plus the full immutable authority.
        // This compact fixture guards exact output preservation independently of the gradient oracle.
        digest.AppendData(JsonSerializer.SerializeToUtf8Bytes(results[0].Authority));
        for (int i = 0; i < results.Length; i++)
        {
            var result = results[i];
            Number((byte)result.Status); Vector(points[i].Direction); Vector(result.BodyFixedDirection);
            Vector(result.BodyFixedPositionMetres); Number(result.HeightMetres);
            Vector(result.PhysicalNormal); Number(result.NormalSampleDistanceMetres);
        }
        return Convert.ToHexStringLower(digest.GetHashAndReset());

        void Vector(Double3 value) { Number(value.X); Number(value.Y); Number(value.Z); }
        void Number(double value)
        {
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(bytes, BitConverter.DoubleToInt64Bits(value));
            digest.AppendData(bytes);
        }
    }

    private static void CheckManifestFailures()
    {
        var root = Path.Combine(Path.GetTempPath(), "novacore-query-manifest-" + Guid.NewGuid().ToString("N"));
        var path = TerrainAssetRepository.ManifestPath(root, TerrainAssetCache.ProductionEarthLocalAssetId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            Check(TerrainAssetManifestFile.TryLoad(TerrainAssetRepository.ManifestPath(Root, TerrainAssetCache.ProductionEarthLocalAssetId), out var production, out var error), error);
            foreach (var changed in new[] { production with { BodyId = 7 }, production with { TerrainVersion = 6 },
                production with { AssetId = "unrelated-fixture" }, production with { Sha256 = new string('0', 64) },
                production with { Hierarchy = production.Hierarchy with { RecordCount = production.Hierarchy.RecordCount + 1 } } })
            {
                File.WriteAllText(path, JsonSerializer.Serialize(changed));
                Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, root, out _) == PhysicalSurfaceQueryStatus.AuthorityMismatch,
                    "wrong body/version/content/coverage cannot acquire published authority");
            }
            foreach (var changed in new[] { production with { Sha256 = null! }, production with { Hierarchy = null! }, production with { Format = null! } })
            {
                File.WriteAllText(path, JsonSerializer.Serialize(changed));
                Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, root, out _) == PhysicalSurfaceQueryStatus.AuthorityUnavailable, "null JSON member is explicit unavailable authority");
            }
            File.WriteAllText(path, "{malformed");
            Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, root, out _) == PhysicalSurfaceQueryStatus.AuthorityUnavailable, "malformed JSON");
            File.Delete(path);
            Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6, root, out _) == PhysicalSurfaceQueryStatus.AuthorityUnavailable, "missing manifest");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void Measure(PlanetaryPhysicalSurfacePointQuery query, string name, Double3 direction)
    {
        var cold = Stopwatch.GetTimestamp(); var first = query.Query(6, direction); var coldTicks = Stopwatch.GetTimestamp() - cold;
        for (var i = 0; i < 32; i++) query.Query(6, direction);
        var ticks = new long[1024]; var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < ticks.Length; i++) { var start = Stopwatch.GetTimestamp(); query.Query(6, direction); ticks[i] = Stopwatch.GetTimestamp() - start; }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "warmed point query allocates");
        Array.Sort(ticks);
        var batchStart = Stopwatch.GetTimestamp();
        for (var batch = 0; batch < 64; batch++) for (var point = 0; point < 16; point++) query.Query(6, direction);
        var batchTicks = Stopwatch.GetTimestamp() - batchStart;
        double Us(long value) => value * 1e6 / Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new { name, status = first.Status.ToString(), firstProbeUs = Us(coldTicks), medianUs = Us(ticks[512]),
            p95Us = Us(ticks[972]), p99Us = Us(ticks[1013]), allocatedBytes = allocated, batch16MeanUs = Us(batchTicks) / 64 }));
    }

    private static Double3 Oracle(Double3 d, double h)
    {
        var frame = PlanetarySurfaceFrame.AtDirection(d);
        var co = Math.Cos(Math.PI / 8); var si = Math.Sin(Math.PI / 8);
        var x = frame.East * co + frame.North * si; var y = frame.North * co - frame.East * si;
        double H(Double3 a, double scale) => Height((d + a * (scale / Radius)).Normalized());
        double D(Double3 a) => (H(a, -2 * h) - 8 * H(a, -h) + 8 * H(a, h) - H(a, 2 * h)) / (12 * h);
        return (d - (x * D(x) + y * D(y)) * (Radius / (Radius + Height(d)))).Normalized();
    }

    private static List<(string Name, Double3 Direction)> Points()
    {
        var points = new List<(string, Double3)>();
        void Add(string name, double lat, double lon) => points.Add((name, Geo(lat, lon)));
        foreach (var (lat, lon) in new[] { (27.9881, 86.925), (0d, -140d), (25.45, -80.75), (46d, -101d), (-31d, 134d), (24d, 13d), (39.1, -106.8), (30d, 82d), (72d, -40d) }) Add("land", lat, lon);
        foreach (var d in new[] { Double3.UnitX, Double3.UnitY, Double3.UnitZ, -Double3.UnitX, -Double3.UnitY, -Double3.UnitZ, new Double3(1, 1, 1).Normalized(), new Double3(1, 1, 0).Normalized() }) points.Add(("axis", d));
        var region = FloridaFacilitySupport.Region;
        foreach (var e in new[] { 0d, 32d, 64d - 1e-3, 64d, 64d + 1e-3, 128d, 192d - 1e-3, 192d, 192d + 1e-3, 500d, 2000d, 10000d })
            foreach (var n in new[] { 0d, 56d, 130d }) points.Add(("support", (region.Up * Radius + region.East * e + region.North * n).Normalized()));
        foreach (var lat in new[] { 27.999444443706977, 28d, 28.5, 29d, 29.000555555895062 })
            foreach (var lon in new[] { -81.00055555609339, -81d, -80.5, -80d, -79.9994444439053 }) Add("boundary", lat, lon);
        var random = new Random(719);
        for (int i = 0; i < 100; i++) Add("global", -80 + random.NextDouble() * 160, -180 + random.NextDouble() * 360);
        for (int i = 0; i < 300; i++) Add("regional", 28 + random.NextDouble(), -81 + random.NextDouble());
        for (int i = 0; i < 50; i++) { int row = random.Next(1, 4095), col = random.Next(8192); Add("texel-knot", 90 - (row + .5) / 4096 * 180, (col + .5) / 8192 * 360 - 180); }
        foreach (double distance in new[] { 1e-3, 1d, 100d }) foreach (double az in new[] { 0d, 45d, 90d, 135d })
        { double lat = 90 - distance / Radius * 180 / Math.PI; Add("north", lat, az); Add("south", -lat, az); }
        int roots = 0;
        for (int i = 0; i < points.Count - 1 && roots < 12; i++)
        {
            var a = points[i].Item2; var b = points[i + 1].Item2;
            if ((Height(a) == 0) == (Height(b) == 0) || Angle(a, b) > .1) continue;
            if (Height(a) != 0) (a, b) = (b, a);
            for (int k = 0; k < 50; k++) { var mid = (a + b).Normalized(); if (Height(mid) == 0) a = mid; else b = mid; }
            points.Add(("clamp", (a + b).Normalized())); roots++;
        }
        return points;
    }
}
