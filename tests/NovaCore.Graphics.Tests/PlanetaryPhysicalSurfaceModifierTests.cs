using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryPhysicalSurfaceModifierTests
{
    private const double EarthRadius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;

    public static void Run()
    {
        VerifyContractAndDeterminism();
        VerifyGradientAndFootprint();
        VerifyBoundaryAndOwnerParity();
        VerifyTransactionalPublication();
        VerifyFrequencyAndCost();
    }

    private static void VerifyContractAndDeterminism()
    {
        var generation = PlanetaryPhysicalSurface.EarthGeneration;
        Require(generation.IsComplete && generation.GenerationId == 1 &&
            generation.SchemaVersion == 1 && generation.BodyId == 6 && generation.TerrainVersion == 5,
            "7H Earth modifier generation identity");
        Require(generation.Modifiers.Length == 2 &&
            generation.Modifiers[0].Id.Type == PlanetaryTerrainModifierType.TiledDetail &&
            generation.Modifiers[1].Id.Type == PlanetaryTerrainModifierType.ErosionLike &&
            generation.Modifiers[0].Order < generation.Modifiers[1].Order,
            "explicit stable modifier ordering");

        var probes = new[]
        {
            BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
                FloridaLaunchSite.Longitude*Math.PI/180d),
            RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveX,.37d,.61d),
            RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.NegativeY,.22d,.73d),
            new Double3(1d,1d,1d).Normalized()
        };
        foreach (var direction in probes)
        {
            var first = PlanetaryPhysicalSurface.EvaluateModifiers(direction);
            for (var repeat = 0; repeat < 32; repeat++)
                Require(PlanetaryPhysicalSurface.EvaluateModifiers(direction) == first,
                    "modifier evaluation is bit-stable across frames/order");
            Require(first.IsFinite && Math.Abs(first.TiledHeightMetres) <= PlanetaryPhysicalSurface.TiledAmplitudeMetres + 1e-12d &&
                Math.Abs(first.ErosionHeightMetres) <= PlanetaryPhysicalSurface.ErosionAmplitudeMetres + 1e-12d,
                "modifier amplitudes are finite and bounded");
        }

        PlanetaryPhysicalSurface.EvaluateModifiers(probes[0]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            PlanetaryPhysicalSurface.EvaluateModifiers(probes[index & 3]);
        var allocations = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocations == 0, $"modifier evaluation allocates no per-sample memory: {allocations}");
    }

    private static void VerifyGradientAndFootprint()
    {
        var florida = BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
            FloridaLaunchSite.Longitude*Math.PI/180d);
        var probes = new[]
        {
            florida,
            (florida*EarthRadius+PlanetarySurfaceFrame.AtDirection(florida).East*4_000d).Normalized(),
            (florida*EarthRadius+PlanetarySurfaceFrame.AtDirection(florida).North*12_000d).Normalized()
        };
        var maximumEastError = 0d; var maximumNorthError = 0d;
        foreach (var direction in probes)
        {
            var frame = PlanetarySurfaceFrame.AtDirection(direction); const double step = .25d;
            var left = (direction*EarthRadius-frame.East*step).Normalized();
            var right = (direction*EarthRadius+frame.East*step).Normalized();
            var down = (direction*EarthRadius-frame.North*step).Normalized();
            var up = (direction*EarthRadius+frame.North*step).Normalized();
            var value = PlanetaryPhysicalSurface.EvaluateModifiers(direction);
            var east = (PlanetaryPhysicalSurface.EvaluateModifiers(right).HeightMetres-
                PlanetaryPhysicalSurface.EvaluateModifiers(left).HeightMetres)/(2d*step);
            var north = (PlanetaryPhysicalSurface.EvaluateModifiers(up).HeightMetres-
                PlanetaryPhysicalSurface.EvaluateModifiers(down).HeightMetres)/(2d*step);
            maximumEastError = Math.Max(maximumEastError, Math.Abs(value.EastGradient-east));
            maximumNorthError = Math.Max(maximumNorthError, Math.Abs(value.NorthGradient-north));
        }
        Require(maximumEastError < 2e-8d && maximumNorthError < 2e-8d,
            $"analytic modifier gradient parity: east={maximumEastError:R}; north={maximumNorthError:R}");

        var floridaFrame = PlanetarySurfaceFrame.AtDirection(florida);
        var boundary = (florida*EarthRadius+floridaFrame.East*PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres).Normalized();
        var outside = (florida*EarthRadius+floridaFrame.East*(PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres+10d)).Normalized();
        var boundaryValue = PlanetaryPhysicalSurface.EvaluateModifiers(boundary);
        var outsideValue = PlanetaryPhysicalSurface.EvaluateModifiers(outside);
        Require(Math.Abs(boundaryValue.ErosionHeightMetres) < 1e-10d &&
            Math.Abs(outsideValue.ErosionHeightMetres) < 1e-12d && outsideValue.GeographicWeight == 0d,
            "bounded erosion contribution and weight close continuously at the physical footprint");
    }

    private static void VerifyBoundaryAndOwnerParity()
    {
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        var shared = RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, 1d, .25d));
        var sameFace = RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .5d, .5d);
        foreach (var direction in new[] { shared, sameFace })
        {
            var global = terrain.SamplePhysicalSurface(direction);
            var anchored = terrain.SamplePhysicalSurface(direction);
            Require(global == anchored,
                "global and dynamic hierarchy consume one body-fixed physical result");
            Require(global.IsFinite && Math.Abs(global.PhysicalNormal.LengthSquared-1d) < 1e-12d,
                "shared physical normal is finite and unit length");
        }

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var globalShader = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary.vert"));
        var fragment = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary_production.frag"));
        var hierarchy = File.ReadAllText(Path.Combine(root,"src","NovaCore.Graphics","PlanetaryDynamicAnchoredSurface.cs"));
        Require(globalShader.Contains("physical_surface.glsl",StringComparison.Ordinal) &&
            fragment.Contains("physical_surface.glsl",StringComparison.Ordinal) &&
            hierarchy.Contains("SamplePhysicalSurface(direction)",StringComparison.Ordinal),
            "global GPU geometry, dynamic CPU geometry, and shared fragment lighting route through the canonical physical-surface contract");
    }

    private static void VerifyTransactionalPublication()
    {
        var initial = new PlanetaryTerrainModifierGeneration(1, 6, 5, 1,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        var publication = new PlanetaryTerrainModifierPublication(initial);
        var retainedHash = publication.Authoritative.DeterministicHash;

        var delayed = new PlanetaryTerrainModifierGeneration(2, 6, 5, 1,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        delayed.BeginPreparation();
        Require(!publication.TryPublish(delayed) && publication.Authoritative.DeterministicHash == retainedHash,
            "delayed generation cannot replace complete authority");
        Require(delayed.TryCompletePreparation() && publication.TryPublish(delayed) &&
            publication.Authoritative.GenerationId == 2,
            "one complete compatible generation publishes atomically");

        var unavailable = new PlanetaryTerrainModifierGeneration(3, 6, 5, 1,
            ReadOnlySpan<PlanetaryTerrainModifierDefinition>.Empty);
        Require(!publication.TryPublish(unavailable) && publication.Authoritative.GenerationId == 2,
            "unavailable modifier configuration retains previous complete generation");
        var incompatible = new PlanetaryTerrainModifierGeneration(4, 6, 6, 1,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        Require(!publication.TryPublish(incompatible) && publication.Authoritative.GenerationId == 2,
            "incompatible terrain generation retains previous complete generation");
    }

    private static void VerifyFrequencyAndCost()
    {
        var globalL0Spacing = EarthRadius*Math.PI*.5d/(PlanetaryTerrainDefinition.GridResolution);
        var globalL2Spacing = globalL0Spacing/4d;
        var anchoredT0Spacing = 69_047.179d /
            (PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution *
             PlanetaryDynamicAnchoredSurface.GpuMaximumTessellationFactor);
        Require(PlanetaryPhysicalSurface.TiledWavelengthMetres/globalL0Spacing >= 3.9d &&
            PlanetaryPhysicalSurface.TiledWavelengthMetres/globalL2Spacing >= 15d &&
            PlanetaryPhysicalSurface.ErosionWavelengthMetres/anchoredT0Spacing >= 4d,
            "7H wavelengths are representable by current L0-L2 and GPU-refined anchored T0 topology");

        var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
            FloridaLaunchSite.Longitude*Math.PI/180d);
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < 100_000; index++) PlanetaryPhysicalSurface.EvaluateModifiers(direction);
        stopwatch.Stop();
        Require(stopwatch.Elapsed.TotalMilliseconds < 500d,
            $"bounded modifier cost: {stopwatch.Elapsed.TotalMilliseconds:F3} ms/100k");
        Console.WriteLine($"7H modifier foundation: generation=0x{PlanetaryPhysicalSurface.EarthGeneration.DeterministicHash:X16}; " +
            $"tiled={PlanetaryPhysicalSurface.TiledAmplitudeMetres:R}m/{PlanetaryPhysicalSurface.TiledWavelengthMetres:R}m; " +
            $"erosion={PlanetaryPhysicalSurface.ErosionAmplitudeMetres:R}m/{PlanetaryPhysicalSurface.ErosionWavelengthMetres:R}m/" +
            $"R{PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres:R}m; gradientError={maximumGradientPlaceholder:F0}; " +
            $"cost100k={stopwatch.Elapsed.TotalMilliseconds:F3}ms; allocations=0");
    }

    private const double maximumGradientPlaceholder = 0d;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
