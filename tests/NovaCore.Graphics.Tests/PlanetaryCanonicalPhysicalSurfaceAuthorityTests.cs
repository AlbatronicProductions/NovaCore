using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryCanonicalPhysicalSurfaceAuthorityTests
{
    private const double Radius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;

    public static void Run()
    {
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        var directions = RepresentativeDirections();
        var maximumRepeatedHeightDelta = 0d;
        var maximumRepeatedPositionDelta = 0d;
        var maximumRepeatedNormalDelta = 0d;
        var maximumSplitHeightDelta = 0d;
        var maximumSharedBoundaryHeightDelta = 0d;
        var maximumSharedBoundaryNormalDelta = 0d;

        var coarseSpacing = PlanetaryPhysicalFrequencyContext.PatchSpacingMetres(2, 16, Radius);
        var fineSpacing = PlanetaryPhysicalFrequencyContext.PatchSpacingMetres(17, 4, Radius);
        var coarse = new PlanetaryPhysicalFrequencyContext(coarseSpacing, coarseSpacing, double.PositiveInfinity);
        var fineBoundary = new PlanetaryPhysicalFrequencyContext(fineSpacing, coarseSpacing, 0d);

        foreach (var direction in directions)
        {
            var geographic = terrain.SampleCanonicalGeographicHeight(direction);
            Require(geographic == terrain.SampleBaseHeight(direction),
                "legacy base-height API delegates exactly to canonical geographic authority");

            var global = terrain.SamplePhysicalSurface(direction);
            var anchored = terrain.SamplePhysicalSurface(direction);
            var modifiers = PlanetaryPhysicalSurface.EvaluateModifiers(direction, geographic,
                PlanetaryPhysicalFrequencyContext.FullResolution);
            var anchoredSplit = Math.Max(0d,
                Math.Max(0d, geographic + modifiers.BaseHeightMetres) + modifiers.NearHeightMetres);
            maximumSplitHeightDelta = Math.Max(maximumSplitHeightDelta,
                Math.Abs(global.FinalHeightMetres - anchoredSplit));
            maximumRepeatedHeightDelta = Math.Max(maximumRepeatedHeightDelta,
                Math.Abs(global.FinalHeightMetres-anchored.FinalHeightMetres));
            maximumRepeatedPositionDelta = Math.Max(maximumRepeatedPositionDelta,
                Math.Sqrt((direction.Normalized()*(Radius+global.FinalHeightMetres)-
                    direction.Normalized()*(Radius+anchored.FinalHeightMetres)).LengthSquared));
            maximumRepeatedNormalDelta = Math.Max(maximumRepeatedNormalDelta,
                Angle(global.PhysicalNormal,anchored.PhysicalNormal));

            var coarseModifiers = PlanetaryPhysicalSurface.EvaluateModifiers(direction,geographic,coarse);
            var boundaryModifiers = PlanetaryPhysicalSurface.EvaluateModifiers(direction,geographic,fineBoundary);
            maximumSharedBoundaryHeightDelta = Math.Max(maximumSharedBoundaryHeightDelta,
                Math.Abs(coarseModifiers.HeightMetres-boundaryModifiers.HeightMetres));
            var frame = PlanetarySurfaceFrame.AtDirection(direction);
            var coarseNormal = (direction.Normalized()-frame.East*coarseModifiers.EastGradient-
                frame.North*coarseModifiers.NorthGradient).Normalized();
            var boundaryNormal = (direction.Normalized()-frame.East*boundaryModifiers.EastGradient-
                frame.North*boundaryModifiers.NorthGradient).Normalized();
            maximumSharedBoundaryNormalDelta = Math.Max(maximumSharedBoundaryNormalDelta,
                Angle(coarseNormal,boundaryNormal));
        }

        Require(maximumRepeatedHeightDelta==0d&&maximumSplitHeightDelta<=1e-12d&&maximumRepeatedPositionDelta==0d&&
            maximumRepeatedNormalDelta<=3e-8d,
            "global and refined consumers repeat one canonical height, position, and normal result");
        Require(maximumSharedBoundaryHeightDelta<=1e-12d&&maximumSharedBoundaryNormalDelta<=3e-8d,
            "different representation densities converge exactly at the retained coarse/fine frequency boundary");

        VerifyParentChildSharedCoordinates(terrain);
        VerifyShaderAndPublicationContract();
        Console.WriteLine($"Canonical physical authority: directions={directions.Length}; " +
            $"heightDelta={maximumRepeatedHeightDelta:E9}m; splitHeightDelta={maximumSplitHeightDelta:E9}m; positionDelta={maximumRepeatedPositionDelta:E9}m; " +
            $"normalDelta={maximumRepeatedNormalDelta:E9}rad; boundaryHeight={maximumSharedBoundaryHeightDelta:E9}m; " +
            $"boundaryNormal={maximumSharedBoundaryNormalDelta:E9}rad");
    }

    private static Double3[] RepresentativeDirections() =>
    [
        Double3.UnitX, -Double3.UnitX, Double3.UnitY, -Double3.UnitY, Double3.UnitZ, -Double3.UnitZ,
        new Double3(1d,1d,0d).Normalized(), new Double3(1d,1d,1d).Normalized(),
        new Double3(-1d,1d,-1d).Normalized(),
        BodyFixedGeography.DirectionFromLatitudeLongitude(28.5721d*Math.PI/180d,-80.648d*Math.PI/180d)
    ];

    private static void VerifyParentChildSharedCoordinates(in PlanetaryTerrainDefinition terrain)
    {
        var maximumDirectionDelta = 0d;
        var maximumHeightDelta = 0d;
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        for (var childIndex = 0; childIndex < 4; childIndex++)
        for (var y = 0; y <= 4; y++)
        for (var x = 0; x <= 4; x++)
        {
            var childX=childIndex&1;var childY=childIndex>>1;
            var childU=(childX+x/4d)*.5d;var childV=(childY+y/4d)*.5d;
            var childDirection=RelaxedCubeSphereProjection.UnitDirection(face,childU,childV);
            var parentDirection=RelaxedCubeSphereProjection.UnitDirection(face,childU,childV);
            maximumDirectionDelta=Math.Max(maximumDirectionDelta,
                Math.Sqrt((childDirection-parentDirection).LengthSquared));
            maximumHeightDelta=Math.Max(maximumHeightDelta,Math.Abs(
                terrain.SampleCanonicalGeographicHeight(childDirection)-
                terrain.SampleCanonicalGeographicHeight(parentDirection)));
        }
        Require(maximumDirectionDelta==0d&&maximumHeightDelta==0d,
            "parent and child shared coordinates address the exact same canonical geography and height");
    }

    private static void VerifyShaderAndPublicationContract()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var shaders=Path.Combine(root,"native","NovaCore.Native","shaders");
        var authority=File.ReadAllText(Path.Combine(shaders,"planetary_physical_authority.glsl"));
        var global=File.ReadAllText(Path.Combine(shaders,"planetary.vert"));
        var anchoredVertex=File.ReadAllText(Path.Combine(shaders,"anchored_terrain.vert"));
        var anchoredEvaluation=File.ReadAllText(Path.Combine(shaders,"anchored_terrain.tese"));
        var anchored=File.ReadAllText(Path.Combine(shaders,"anchored_physical_surface.glsl"));
        var fragment=File.ReadAllText(Path.Combine(shaders,"planetary_production.frag"));
        var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","NovaCoreNative.cpp"));

        Require(authority.Contains("binding=33",StringComparison.Ordinal)&&
            authority.Contains("CanonicalElevationOracleMetres",StringComparison.Ordinal)&&
            authority.Contains("LocalTerrainElevationResidual",StringComparison.Ordinal)&&
            global.Contains("planetary_physical_authority.glsl",StringComparison.Ordinal)&&
            anchored.Contains("planetary_physical_authority.glsl",StringComparison.Ordinal),
            "global and anchored geometry include one oracle-plus-regional canonical authority");
        Require(global.Contains("return CanonicalPhysicalHeight(direction);",StringComparison.Ordinal)&&
            anchoredVertex.Contains("CanonicalBasePhysicalHeight(direction)",StringComparison.Ordinal)&&
            anchoredEvaluation.Contains("EvaluateNearPhysicalD(direction,EvaluateBiomeBlendD(direction,geographic))",StringComparison.Ordinal)&&
            !global.Contains("TerrainModifierHeightD(direction,geographicHeight,frequency)",StringComparison.Ordinal),
            "global vertices and anchored base/TES vertices sample one representation-independent H(bodyDirection)");
        Require(!global.Contains("productionElevation",StringComparison.Ordinal)&&
            !fragment.Contains("ProductionFixedPhysicalNormal",StringComparison.Ordinal)&&
            fragment.Contains("vec3 physical=normalize(normal)",StringComparison.Ordinal)&&
            !fragment.Contains("materialBenchmark",StringComparison.Ordinal),
            "terrain-v5 cannot re-enter global physical geometry or reconstruct a second lighting normal");
        Require(fragment.Contains("EvaluatePresentationBiomeWeightsF",StringComparison.Ordinal)&&
            fragment.Contains("Geometry/TES has already produced physical height",StringComparison.Ordinal),
            "Candidate D remains presentation-only and does not replace physical height authority");
        Require(native.Contains("managedAcknowledged=a.submission->anchoredSurfaceGpuReadyGeneration==a.submission->anchoredSurfaceActiveGeneration",StringComparison.Ordinal)&&
            native.Contains("coverage[2]=a.submission->anchoredSurfaceActiveGeneration",StringComparison.Ordinal)&&
            native.Contains("AnchoredSurfaceFrameResourceCount=3",StringComparison.Ordinal)&&
            native.Contains("BindDynamicAnchoredResource(a,resourceIndex)",StringComparison.Ordinal)&&
            native.Contains("a.anchoredSurfaceActiveGeneration=a.submission->anchoredSurfaceActiveGeneration",StringComparison.Ordinal),
            "refined ownership becomes draw-visible only as one GPU-ready acknowledged frame-indexed coverage generation");
    }

    private static double Angle(in Double3 a,in Double3 b) =>
        Math.Acos(Math.Clamp(Double3.Dot(a.Normalized(),b.Normalized()),-1d,1d));

    private static void Require(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }
}
