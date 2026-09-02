using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryNaturalTerrainRendererIntegrationTests
{
    private const ulong EarthBodyId = PlanetaryPhysicalSurface.EarthBodyId;
    private const uint CandidateSeed = PlanetaryPhysicalSurface.NaturalTerrainCandidateSeed;
    private static readonly PlanetaryTerrainDefinition Terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
    private static readonly PlanetaryNaturalTerrainFamilyIdentity Identity = new(EarthBodyId,
        PlanetaryNaturalTerrainFamilies.ProofGeneration, CandidateSeed);

    public static void Run()
    {
        LoadTerrainAuthorities();
        VerifyOptInRouting();
        var (samples, heightMaximum, normalMaximum) = VerifyCanonicalEquality();
        var boundaryMaximum = VerifySharedBoundaryEquality();
        VerifyShaderConsumers();
        VerifyTransactionalPreparationRouting();
        Console.WriteLine($"M12D candidate renderer integration: samples={samples}; " +
            $"heightMax={heightMaximum:E17}m; normalMax={normalMaximum:E17}rad; " +
            $"sharedBoundaryMax={boundaryMaximum:E17}m; generation=4; default=3");
    }

    private static void LoadTerrainAuthorities()
    {
        var root = RepositoryRoot();
        Require(EarthElevationDataset.TryLoad(Path.Combine(root, "assets", "earth", "runtime"), out var oracleError),
            $"candidate CPU elevation oracle: {oracleError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out var localError), $"candidate local-v2 asset: {localError}");
        Require(EarthLocalTerrainElevationDataset.TryLoad(localPath, out var localLoadError),
            $"candidate CPU local-v2 oracle: {localLoadError}");
    }

    private static void VerifyOptInRouting()
    {
        var previous = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.Generation3);
            Require(PlanetaryPhysicalSurface.RuntimeGeneration == PlanetaryPhysicalSurfaceGeneration.Generation3,
                "generation 3 remains the explicit default authority");
            var direction = Direction(28.6084d, -80.6042d);
            var stable = Terrain.SamplePhysicalSurface(direction);
            var candidate = Terrain.SamplePhysicalSurface(direction,
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            Require(BitConverter.DoubleToInt64Bits(stable.FinalHeightMetres) !=
                BitConverter.DoubleToInt64Bits(candidate.FinalHeightMetres),
                "candidate selection is explicit rather than a hidden alias of generation 3");
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            Require(Terrain.SamplePhysicalSurface(direction) == candidate,
                "all default CPU collision/clearance calls consume the selected process authority");
        }
        finally
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous);
        }
    }

    private static (int Samples, double HeightMaximum, double NormalMaximum) VerifyCanonicalEquality()
    {
        var directions = new List<(string Name, Double3 Direction)>
        {
            ("Florida", Direction(28.6084d, -80.6042d)),
            ("Everest", Direction(27.9881d, 86.925d)),
            ("Alpine", Direction(30d, 82d)),
            ("Sahara", Direction(24d, 13d)),
            ("Grassland", Direction(46d, -101d)),
            ("Glacial", Direction(72d, -40d)),
            ("Coastline", Direction(25.45d, -80.75d)),
            ("Remote Pacific", Direction(0d, -140d)),
            ("North pole", new Double3(0d, 1d, 0d)),
            ("South pole", new Double3(0d, -1d, 0d))
        };
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            directions.Add(($"{face} interior", RelaxedCubeSphereProjection.UnitDirection(face, .37d, .61d)));
        foreach (var x in new[] { -1d, 1d }) foreach (var y in new[] { -1d, 1d })
            foreach (var z in new[] { -1d, 1d }) directions.Add(($"corner {x}/{y}/{z}", new Double3(x, y, z).Normalized()));

        var maximumHeight = 0d; var maximumNormal = 0d; var maximumNormalName = string.Empty;
        foreach (var (name, rawDirection) in directions)
        {
            var direction = rawDirection.Normalized();
            var candidate = Terrain.SamplePhysicalSurface(direction,
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            var natural = PlanetaryNaturalTerrainFamilies.EvaluateComposed(
                direction * PlanetaryPhysicalSurface.EarthReferenceRadiusMetres, Identity);
            var geographic = Terrain.SampleBaseHeight(direction);
            var preparedBase = Math.Max(0d, geographic + natural.Macro.Height + natural.Meso.Height);
            var composed = Math.Max(0d, preparedBase + natural.Near.Height);
            maximumHeight = Math.Max(maximumHeight, Math.Abs(candidate.FinalHeightMetres - composed));
            Require(candidate.PhysicalNormal.IsFinite &&
                Double3.Dot(candidate.PhysicalNormal, direction) > 0d, $"candidate outward finite normal {name}");
            if (Math.Abs(direction.Y) < .999999d)
            {
                var numerical = NumericalNormal(direction);
                var normalError = Math.Acos(Math.Clamp(Double3.Dot(candidate.PhysicalNormal, numerical), -1d, 1d));
                if (normalError > maximumNormal) { maximumNormal = normalError; maximumNormalName = name; }
            }
            Require(natural.IsFinite && natural.FirstFamily is >= PlanetaryNaturalTerrainFamily.Grassland and
                <= PlanetaryNaturalTerrainFamily.GenericRemote && natural.SecondFamily is >=
                PlanetaryNaturalTerrainFamily.Grassland and <= PlanetaryNaturalTerrainFamily.GenericRemote,
                $"candidate family/biome identity is canonical {name}");
        }
        Require(maximumHeight <= 1e-10d, $"candidate canonical clamp/order equality: {maximumHeight:R}");
        Require(maximumNormal <= 2e-4d, $"candidate value/gradient physical-normal equality: {maximumNormal:R} rad at {maximumNormalName}");
        return (directions.Count, maximumHeight, maximumNormal);
    }

    private static double VerifySharedBoundaryEquality()
    {
        var maximum = 0d;
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        {
            var parent = new PlanetarySurfacePatchId(EarthBodyId, Terrain.Version, face, 3, 3, 2);
            var child = parent.Child(3);
            for (var edge = 0; edge <= 4; edge++)
            {
                // Parent's upper-right quadrant and child 3 share these exact rational cube directions.
                var parentDirection = RelaxedCubeSphereProjection.PatchPoint(parent, 8 + edge * 2, 8, 16);
                var childDirection = RelaxedCubeSphereProjection.PatchPoint(child, edge * 4, 0, 16);
                Require(parentDirection == childDirection, $"canonical parent/child direction identity {face}/{edge}");
                var global = Terrain.SamplePhysicalSurface(parentDirection,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                var anchored = Terrain.SamplePhysicalSurface(childDirection,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                maximum = Math.Max(maximum, Math.Abs(global.FinalHeightMetres - anchored.FinalHeightMetres));
                Require(global.PhysicalNormal == anchored.PhysicalNormal,
                    $"candidate shared-boundary normal/generation equality {face}/{edge}");
                var first = PlanetaryNaturalTerrainFamilies.EvaluateComposed(parentDirection *
                    PlanetaryPhysicalSurface.EarthReferenceRadiusMetres, Identity);
                var second = PlanetaryNaturalTerrainFamilies.EvaluateComposed(childDirection *
                    PlanetaryPhysicalSurface.EarthReferenceRadiusMetres, Identity);
                Require(first == second, $"candidate family/biome/frequency identity equality {face}/{edge}");
            }
        }
        Require(maximum == 0d, $"candidate global/anchored boundary height equality: {maximum:R}");
        return maximum;
    }

    private static void VerifyShaderConsumers()
    {
        var shader = Path.Combine(RepositoryRoot(), "native", "NovaCore.Native", "shaders");
        var global = File.ReadAllText(Path.Combine(shader, "planetary.vert"));
        var anchored = File.ReadAllText(Path.Combine(shader, "anchored_terrain.vert"));
        var tes = File.ReadAllText(Path.Combine(shader, "anchored_terrain.tese"));
        var query = File.ReadAllText(Path.Combine(shader, "planetary_height_query.comp"));
        var mesh = File.ReadAllText(Path.Combine(shader, "planetary_mesh_displace.comp"));
        Require(global.Contains("naturalGlobal.naturalValues", StringComparison.Ordinal) &&
            global.Contains("EvaluateNaturalCandidateNearD", StringComparison.Ordinal),
            "global candidate consumes prepared macro/meso plus canonical near detail");
        Require(anchored.Contains("naturalAnchored.naturalAnchoredValues", StringComparison.Ordinal) &&
            anchored.Contains("NaturalPreparedBaseNormal", StringComparison.Ordinal),
            "anchored candidate consumes prepared macro/meso value+gradient");
        Require(tes.Contains("EvaluateNaturalCandidateNearD", StringComparison.Ordinal) &&
            !tes.Contains("EvaluateNaturalCandidatePreparedD", StringComparison.Ordinal),
            "TES evaluates only bounded near-family detail and never macro/meso");
        Require(query.Contains("naturalCandidate", StringComparison.Ordinal) &&
            mesh.Contains("NOVACORE_PHYSICAL_GENERATION_M12D", StringComparison.Ordinal),
            "GPU query and mesh displacement share the explicit candidate authority");
    }

    private static void VerifyTransactionalPreparationRouting()
    {
        var root = RepositoryRoot();
        var native = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.cpp"));
        var preparation = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders",
            "planetary_natural_terrain_anchored_prepare.comp"));
        var submitted = native.IndexOf("naturalAnchoredSubmittedGeneration==", StringComparison.Ordinal);
        var acknowledged = native.IndexOf("anchoredSurfaceGpuReadyGeneration=a.naturalAnchoredPreparedGeneration",
            StringComparison.Ordinal);
        var published = native.IndexOf("BindDynamicAnchoredResource(a,resourceIndex)", acknowledged,
            StringComparison.Ordinal);
        Require(submitted >= 0 && acknowledged > submitted && published > acknowledged,
            "candidate publication is ordered GPU submitted/fenced -> acknowledged -> atomic owner bind");
        Require(preparation.Contains("binding=37", StringComparison.Ordinal) &&
            native.Contains("BindNaturalAnchoredPreparationResource", StringComparison.Ordinal),
            "incoming candidate preparation uses a retired descriptor resource, not the live owner table");
        Require(native.Contains("naturalAnchoredPreparedGeneration==a.anchoredSurfaceActiveGeneration",
                StringComparison.Ordinal),
            "candidate publication diagnostics prove the matching prepared generation");
    }

    private static Double3 NumericalNormal(in Double3 direction)
    {
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var angle = PlanetaryPhysicalSurface.PhysicalNormalSampleRadiusMetres /
            PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
        var leftDirection = (direction - frame.East * angle).Normalized();
        var rightDirection = (direction + frame.East * angle).Normalized();
        var downDirection = (direction - frame.North * angle).Normalized();
        var upDirection = (direction + frame.North * angle).Normalized();
        var leftHeight = BaseHeight(leftDirection); var rightHeight = BaseHeight(rightDirection);
        var downHeight = BaseHeight(downDirection); var upHeight = BaseHeight(upDirection);
        var left = leftDirection * (PlanetaryPhysicalSurface.EarthReferenceRadiusMetres + leftHeight);
        var right = rightDirection * (PlanetaryPhysicalSurface.EarthReferenceRadiusMetres + rightHeight);
        var down = downDirection * (PlanetaryPhysicalSurface.EarthReferenceRadiusMetres + downHeight);
        var up = upDirection * (PlanetaryPhysicalSurface.EarthReferenceRadiusMetres + upHeight);
        var baseNormal = Double3.Cross(right - left, up - down).Normalized();
        if (Double3.Dot(baseNormal, direction) < 0d) baseNormal = -baseNormal;
        var radial = Math.Max(Double3.Dot(baseNormal, direction), 1e-9d);
        var eastGradient = -Double3.Dot(baseNormal, frame.East) / radial;
        var northGradient = -Double3.Dot(baseNormal, frame.North) / radial;
        var near = PlanetaryNaturalTerrainFamilies.EvaluateComposed(direction *
            PlanetaryPhysicalSurface.EarthReferenceRadiusMetres, Identity).Near.BodyGradient;
        eastGradient += Double3.Dot(near, frame.East);
        northGradient += Double3.Dot(near, frame.North);
        return (direction - frame.East * eastGradient - frame.North * northGradient).Normalized();

        static double BaseHeight(in Double3 sampleDirection) => PlanetaryPhysicalSurface.EvaluateBaseHeightNoGradient(
            Terrain, sampleDirection, PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
    }

    private static Double3 Direction(double latitudeDegrees, double longitudeDegrees) =>
        BodyFixedGeography.DirectionFromLatitudeLongitude(latitudeDegrees * Math.PI / 180d,
            longitudeDegrees * Math.PI / 180d);

    private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
