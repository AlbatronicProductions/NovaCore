using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryPhysicalFrequencyContinuityTests
{
    public static void Run()
    {
        VerifyShaderEarlyRejectionContract();
        var radius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
        var globalSpacing = PlanetaryPhysicalFrequencyContext.PatchSpacingMetres(2, 16, radius);
        var anchoredSpacing = PlanetaryPhysicalFrequencyContext.PatchSpacingMetres(17, 4, radius);
        var global = new PlanetaryPhysicalFrequencyContext(globalSpacing, globalSpacing, double.PositiveInfinity);
        var edge = new PlanetaryPhysicalFrequencyContext(anchoredSpacing, globalSpacing, 0d);
        var fullyAnchored = new PlanetaryPhysicalFrequencyContext(anchoredSpacing, globalSpacing,
            PlanetaryPhysicalSurface.FrequencyBands.ToArray().Max(
                band => Math.Max(band.WavelengthMetres, 4d * anchoredSpacing)));

        var suppressedBands = 0; var activatedBands = 0; var maximumC2EdgeDerivative = 0d;
        foreach (var band in PlanetaryPhysicalSurface.FrequencyBands)
        {
            var globalWeight = global.Weight(band.WavelengthMetres);
            Require(Math.Abs(edge.Weight(band.WavelengthMetres) - globalWeight) <= 1e-15d,
                "anchored outer edge uses the exact global-representable frequency weight");
            var fineWeight = PlanetaryPhysicalFrequencyContext.Representability(
                band.WavelengthMetres, anchoredSpacing);
            Require(Math.Abs(fullyAnchored.Weight(band.WavelengthMetres) - fineWeight) <= 1e-15d,
                "the representability contract reaches the fine sampling weight after its C2 transition");
            if (globalWeight < 1d - 1e-12d) suppressedBands++;
            if (fineWeight > globalWeight + 1e-12d) activatedBands++;
            var epsilon = Math.Min(.01d, Math.Max(band.WavelengthMetres, 4d * anchoredSpacing) * 1e-6d);
            var nearEdge = new PlanetaryPhysicalFrequencyContext(anchoredSpacing, globalSpacing, epsilon);
            maximumC2EdgeDerivative = Math.Max(maximumC2EdgeDerivative,
                Math.Abs(nearEdge.Weight(band.WavelengthMetres) - globalWeight) / epsilon);
            var previous = globalWeight;
            var guardWidth = Math.Max(band.WavelengthMetres, 4d * anchoredSpacing);
            for (var step = 1; step <= 32; step++)
            {
                var value = new PlanetaryPhysicalFrequencyContext(anchoredSpacing, globalSpacing,
                    guardWidth * step / 32d).Weight(band.WavelengthMetres);
                Require(value + 1e-15d >= previous, "physical frequency activation is monotonic through its C2 transition");
                previous = value;
            }
        }
        Require(suppressedBands > 0 && activatedBands > 0 && maximumC2EdgeDerivative < 1e-7d,
            "the global contract suppresses unresolved bands and the C2 edge has zero first derivative");

        var directions = new[]
        {
            BodyFixedGeography.DirectionFromLatitudeLongitude(28.5721d*Math.PI/180d,-80.648d*Math.PI/180d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(35d*Math.PI/180d,-112d*Math.PI/180d),
            BodyFixedGeography.DirectionFromLatitudeLongitude(-18d*Math.PI/180d,126d*Math.PI/180d)
        };
        var maximumHeightDelta = 0d; var maximumNormalDelta = 0d; var maximumEdgePositionDelta = 0d;
        foreach (var direction in directions)
        {
            var baseHeight = PlanetaryTerrainDefinition.EarthProductionCubeV5.SampleBaseHeight(direction);
            var globalValue = PlanetaryPhysicalSurface.EvaluateModifiers(direction, baseHeight, global);
            var anchoredEdge = PlanetaryPhysicalSurface.EvaluateModifiers(direction, baseHeight, edge);
            maximumHeightDelta = Math.Max(maximumHeightDelta,
                Math.Abs(globalValue.HeightMetres - anchoredEdge.HeightMetres));
            var frame = PlanetarySurfaceFrame.AtDirection(direction);
            var globalNormal = (direction - frame.East * globalValue.EastGradient -
                frame.North * globalValue.NorthGradient).Normalized();
            var anchoredNormal = (direction - frame.East * anchoredEdge.EastGradient -
                frame.North * anchoredEdge.NorthGradient).Normalized();
            maximumNormalDelta = Math.Max(maximumNormalDelta, Math.Acos(Math.Clamp(
                Double3.Dot(globalNormal, anchoredNormal), -1d, 1d)));
            var globalPoint = direction * (radius + baseHeight + globalValue.HeightMetres);
            var anchoredPoint = direction * (radius + baseHeight + anchoredEdge.HeightMetres);
            maximumEdgePositionDelta = Math.Max(maximumEdgePositionDelta,
                Math.Sqrt((globalPoint - anchoredPoint).LengthSquared));
        }
        Require(maximumHeightDelta <= 1e-12d && maximumNormalDelta <= 3e-8d &&
                maximumEdgePositionDelta <= 1e-9d,
            "global and anchored outer-edge evaluations share height, gradient, and body-fixed position");

        Console.WriteLine($"Physical frequency continuity: bands={PlanetaryPhysicalSurface.FrequencyBands.Length}; " +
            $"globalSpacing={globalSpacing:R}m; anchoredSpacing={anchoredSpacing:R}m; " +
            $"suppressed={suppressedBands}; activated={activatedBands}; heightDelta={maximumHeightDelta:E9}m; " +
            $"normalDelta={maximumNormalDelta:E9}rad; edgePositionDelta={maximumEdgePositionDelta:E9}m; " +
            $"c2Derivative={maximumC2EdgeDerivative:E9}");
    }

    private static void VerifyShaderEarlyRejectionContract()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var shader=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","physical_surface.glsl"));
        Require(shader.Contains("double weight=PhysicalFrequencyWeightD(wavelength,frequency);\n  if(weight==0.0){gradient=dvec3(0.0);return 0.0;}\n  double value=PhysicalBandD",StringComparison.Ordinal) &&
            shader.Contains("double weight=PhysicalFrequencyWeightD(wavelength,frequency);\n  if(weight==0.0){gradient=dvec3(0.0);return 0.0;}\n  double value=PhysicalWarpedBandD",StringComparison.Ordinal),
            "inactive contextual bands reject before phase, trigonometry, warp, and gradient evaluation");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
