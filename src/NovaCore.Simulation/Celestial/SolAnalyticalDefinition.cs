using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Approximate fixed-epoch J2000-equivalent Solar-System authoring data; runtime evaluation remains generic two-body propagation.</summary>
internal static class SolAnalyticalDefinition
{
    internal static readonly CelestialSystemId Id = new(10_001);
    internal const double AstronomicalUnitMetres = 149_597_870_700d;
    internal const double JulianYearSeconds = 31_557_600d;
    private const long CoverageSeconds = 15_778_800_000L; // ±500 Julian years.
    private static readonly CelestialTimeDomainId Domain = new(10_001);
    private static readonly CelestialEphemerisSourceId SystemSource = new(10_001);
    private static readonly CelestialEphemerisSourceId FixedSource = new(10_002);
    private static readonly CelestialEphemerisSourceId KeplerSource = new(10_003);
    private static readonly CelestialEphemerisSourceId PhysicalSource = new(10_004);
    private static readonly CelestialConstantsVersionId ConstantsVersion = new(1);

    internal static CelestialSystemDefinition Instance => Holder.Value;
    internal static CelestialSystemDefinition CreateForTest() => Create();
    internal static int ElementCount => Elements.Length;
    internal static SolAnalyticalOrbitalElements GetElement(int index) => Elements[index];

    // Fixed J2000-equivalent values: JPL Table 1 for planets (rates intentionally omitted), and a fixed Earth-relative lunar approximation.
    private static readonly SolAnalyticalOrbitalElements[] Elements =
    [
        new(SolarSystemBodyIds.Mercury, SolarSystemBodyIds.Sun, .38709927d, .20563593d, 7.00497902d, 48.33076593d, 77.45779628d - 48.33076593d, 252.25032350d - 77.45779628d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Venus, SolarSystemBodyIds.Sun, .72333566d, .00677672d, 3.39467605d, 76.67984255d, 131.60246718d - 76.67984255d, 181.97909950d - 131.60246718d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Earth, SolarSystemBodyIds.Sun, 1.00000261d, .01671123d, -.00001531d, 0d, 102.93768193d, 100.46457166d - 102.93768193d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Moon, SolarSystemBodyIds.Earth, 384_400_000d / AstronomicalUnitMetres, .0549d, 5.145d, 125.08d, 318.15d, 115.3654d, 3.986004418e14d),
        new(SolarSystemBodyIds.Mars, SolarSystemBodyIds.Sun, 1.52371034d, .09339410d, 1.84969142d, 49.55953891d, -23.94362959d - 49.55953891d, -4.55343205d - -23.94362959d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Jupiter, SolarSystemBodyIds.Sun, 5.20288700d, .04838624d, 1.30439695d, 100.47390909d, 14.72847983d - 100.47390909d, 34.39644051d - 14.72847983d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Saturn, SolarSystemBodyIds.Sun, 9.53667594d, .05386179d, 2.48599187d, 113.66242448d, 92.59887831d - 113.66242448d, 49.95424423d - 92.59887831d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Uranus, SolarSystemBodyIds.Sun, 19.18916464d, .04725744d, .77263783d, 74.01692503d, 170.95427630d - 74.01692503d, 313.23810451d - 170.95427630d, 1.32712440018e20d),
        new(SolarSystemBodyIds.Neptune, SolarSystemBodyIds.Sun, 30.06992276d, .00859048d, 1.77004347d, 131.78422574d, 44.96476227d - 131.78422574d, -55.12002969d - 44.96476227d, 1.32712440018e20d),
    ];

    private static class Holder { internal static readonly CelestialSystemDefinition Value = Create(); }

    private static CelestialSystemDefinition Create()
    {
        var metadata = Metadata(SystemSource);
        var bodies = new[]
        {
            Body(SolarSystemBodyIds.Sun, "Sun", CelestialBodyClassification.Star, null, 1.32712440018e20d, 695_700_000d, 696_340_000d, 695_700_000d),
            Body(SolarSystemBodyIds.Mercury, "Mercury", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 2.2032e13d, 2_439_700d, 2_439_700d, 2_439_700d),
            Body(SolarSystemBodyIds.Venus, "Venus", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 3.24859e14d, 6_051_800d, 6_051_800d, 6_051_800d),
            Body(SolarSystemBodyIds.Earth, "Earth", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 3.986004418e14d, 6_371_008.8d, 6_378_137d, 6_356_752.314245d),
            Body(SolarSystemBodyIds.Moon, "Moon", CelestialBodyClassification.Moon, SolarSystemBodyIds.Earth, 4.9048695e12d, 1_737_400d, 1_738_100d, 1_736_000d),
            Body(SolarSystemBodyIds.Mars, "Mars", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 4.282837e13d, 3_389_500d, 3_396_190d, 3_376_200d),
            Body(SolarSystemBodyIds.Jupiter, "Jupiter", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 1.26686534e17d, 69_911_000d, 71_492_000d, 66_854_000d),
            Body(SolarSystemBodyIds.Saturn, "Saturn", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 3.7931187e16d, 58_232_000d, 60_268_000d, 54_364_000d),
            Body(SolarSystemBodyIds.Uranus, "Uranus", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 5.793939e15d, 25_362_000d, 25_559_000d, 24_973_000d),
            Body(SolarSystemBodyIds.Neptune, "Neptune", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 6.836529e15d, 24_622_000d, 24_764_000d, 24_341_000d),
        };
        var nodes = new[]
        {
            new CelestialHierarchyNode(SolarSystemBodyIds.Sun, new(CelestialTrajectoryModel.FixedBody, FixedSource, 0)),
            Node(SolarSystemBodyIds.Mercury, 0), Node(SolarSystemBodyIds.Venus, 1), Node(SolarSystemBodyIds.Earth, 2), Node(SolarSystemBodyIds.Moon, 3),
            Node(SolarSystemBodyIds.Mars, 4), Node(SolarSystemBodyIds.Jupiter, 5), Node(SolarSystemBodyIds.Saturn, 6), Node(SolarSystemBodyIds.Uranus, 7), Node(SolarSystemBodyIds.Neptune, 8),
        };
        var trajectories = new TwoBodyTrajectory[Elements.Length]; for (var index = 0; index < Elements.Length; index++) trajectories[index] = Orbit(Elements[index]);
        var sources = new[] { new CelestialEphemerisSource(FixedSource, CelestialTrajectoryModel.FixedBody, Metadata(FixedSource)), new CelestialEphemerisSource(KeplerSource, CelestialTrajectoryModel.AnalyticalKepler, Metadata(KeplerSource)) };
        if (CelestialSystemDefinition.TryCreate(Id, bodies, nodes, CelestialSystemTimeMapping.Identity(Domain), metadata, sources, [FixedBodyEphemerisPayload.Identity], [], trajectories, out var definition, out _)) return definition!;
        throw new InvalidOperationException("SolAnalytical authored data failed validation.");
    }

    private static CelestialHierarchyNode Node(CelestialBodyId id, int payload) => new(id, new(CelestialTrajectoryModel.AnalyticalKepler, KeplerSource, payload));
    private static CelestialBodyCatalogEntry Body(CelestialBodyId id, string name, CelestialBodyClassification kind, CelestialBodyId? parent, double mu, double mean, double equatorial, double polar) => new(new(id, name, kind, parent, default, default, default), new(mu, mean, equatorial, polar, equatorial == 0d ? 0d : (equatorial - polar) / equatorial, default, default, default, PhysicalSource, ConstantsVersion));
    private static CelestialEphemerisMetadata Metadata(CelestialEphemerisSourceId source) => new(source, new(1), Domain, -CoverageSeconds * SimulationInstant.TicksPerSecond, CoverageSeconds * SimulationInstant.TicksPerSecond, new(10_001), ConstantsVersion, new(0x4A504C5353443031UL, 0x3230303046495845UL), new(0, 0));

    private static TwoBodyTrajectory Orbit(in SolAnalyticalOrbitalElements element)
    {
        var a = element.SemiMajorAxisAu * AstronomicalUnitMetres; var i = Degrees(element.InclinationDegrees); var node = Degrees(element.LongitudeOfAscendingNodeDegrees); var periapsis = Degrees(element.ArgumentOfPeriapsisDegrees); var mean = Degrees(element.MeanAnomalyDegrees); var anomaly = mean;
        for (var iteration = 0; iteration < 16; iteration++) anomaly -= (anomaly - element.Eccentricity * Math.Sin(anomaly) - mean) / (1d - element.Eccentricity * Math.Cos(anomaly));
        var cosine = Math.Cos(anomaly); var sine = Math.Sin(anomaly); var radius = a * (1d - element.Eccentricity * cosine); var x = a * (cosine - element.Eccentricity); var y = a * Math.Sqrt(1d - element.Eccentricity * element.Eccentricity) * sine; var factor = Math.Sqrt(element.CentralGravitationalParameter * a) / radius;
        var vx = -factor * sine; var vy = factor * Math.Sqrt(1d - element.Eccentricity * element.Eccentricity) * cosine;
        return new(element.Parent, SimulationInstant.Zero, new(Rotate(x, y, i, node, periapsis), Rotate(vx, vy, i, node, periapsis)), TwoBodyPropagationModel.CartesianTwoBodyV1);
    }

    private static Double3 Rotate(double x, double y, double inclination, double node, double periapsis)
    {
        var co = Math.Cos(node); var so = Math.Sin(node); var ci = Math.Cos(inclination); var si = Math.Sin(inclination); var cw = Math.Cos(periapsis); var sw = Math.Sin(periapsis);
        return new((cw * co - sw * so * ci) * x + (-sw * co - cw * so * ci) * y, (cw * so + sw * co * ci) * x + (-sw * so + cw * co * ci) * y, sw * si * x + cw * si * y);
    }
    private static double Degrees(double value) => value * Math.PI / 180d;
}

internal readonly record struct SolAnalyticalOrbitalElements(CelestialBodyId Body, CelestialBodyId Parent, double SemiMajorAxisAu, double Eccentricity, double InclinationDegrees, double LongitudeOfAscendingNodeDegrees, double ArgumentOfPeriapsisDegrees, double MeanAnomalyDegrees, double CentralGravitationalParameter);
