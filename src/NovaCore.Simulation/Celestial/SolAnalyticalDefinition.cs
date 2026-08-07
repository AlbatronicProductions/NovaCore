using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Compact DE440-validated J2000 Solar-System authoring data; runtime evaluation remains generic two-body propagation.</summary>
internal static class SolAnalyticalDefinition
{
    internal const string VersionName = "SolCompact-DE440Validated-v3";
    internal static readonly CelestialSystemId Id = new(10_004);
    internal const double AstronomicalUnitMetres = 149_597_870_700d;
    internal const double JulianYearSeconds = 31_557_600d;
    private const long CoverageSeconds = 15_778_800_000L; // ±500 Julian years.
    private static readonly CelestialTimeDomainId Domain = new(10_001);
    private static readonly CelestialEphemerisSourceId SystemSource = new(10_001);
    private static readonly CelestialEphemerisSourceId FixedSource = new(10_002);
    private static readonly CelestialEphemerisSourceId KeplerSource = new(10_003);
    private static readonly CelestialEphemerisSourceId PhysicalSource = new(10_004);
    private static readonly CelestialConstantsVersionId ConstantsVersion = new(2);

    internal static CelestialSystemDefinition Instance => Holder.Value;
    internal static CelestialSystemDefinition CreateForTest() => Create(true);
    internal static CelestialSystemDefinition CreateV2ForTest() => Create(false);
    internal static int ElementCount => Elements.Length;
    internal static SolAnalyticalOrbitalElements GetElement(int index) => Elements[index];
    internal static AnalyticalKeplerSecularCorrection LunarCorrection { get; } = CreateLunarCorrection();
    internal static AnalyticalKeplerPeriodicCorrection LunarPeriodicCorrection { get; } = CreateLunarPeriodicCorrection();

    // ET 0 osculating elements derived offline from pinned DE440 geometric J2000 states. Planet states are Sun-relative;
    // the Moon state is Earth-relative. Runtime remains source-independent generic Cartesian two-body propagation.
    private static readonly SolAnalyticalOrbitalElements[] Elements =
    [
        new(SolarSystemBodyIds.Mercury, SolarSystemBodyIds.Sun, 57_909_074_636.49355d / AstronomicalUnitMetres, .2056301618061039d, 28.552255986680105d, 10.987947922877025d, 67.56295883671017d, 174.79588000228333d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Venus, SolarSystemBodyIds.Sun, 108_208_435_338.18922d / AstronomicalUnitMetres, .0067573530708024045d, 24.433051716792335d, 8.007372079614651d, 124.55944869284438d, 50.098707475193336d, 1.327124400412794e20d),
        // Earth intentionally uses the DE440 EMB heliocentric seed because this ten-body hierarchy has no EMB node.
        new(SolarSystemBodyIds.Earth, SolarSystemBodyIds.Sun, 149_597_806_502.43307d / AstronomicalUnitMetres, .016705450456221425d, 23.43921151626908d, .00016617231131312412d, 102.91731805764385d, 357.5456657494844d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Moon, SolarSystemBodyIds.Earth, 386_138_428.9987714d / AstronomicalUnitMetres, .05357474370672006d, 20.94230395200119d, 12.236438458717638d, 68.05335118129264d, 140.14966404984276d, 3.986004355070226e14d),
        new(SolarSystemBodyIds.Mars, SolarSystemBodyIds.Sun, 227_939_220_446.44705d / AstronomicalUnitMetres, .09331542799780028d, 24.677090036011343d, 3.373683582426468d, 333.01852068935466d, 19.35640471827126d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Jupiter, SolarSystemBodyIds.Sun, 779_362_936_402.3601d / AstronomicalUnitMetres, .04971556713126671d, 23.23516872990558d, 3.253163771800034d, 12.959998743658176d, 18.42852288642702d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Saturn, SolarSystemBodyIds.Sun, 1_433_894_841_645.5752d / AstronomicalUnitMetres, .05594548839657417d, 22.551324390567498d, 5.945123431529416d, 83.9797544634598d, 320.55135593341265d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Uranus, SolarSystemBodyIds.Sun, 2_876_796_628_880.9165d / AstronomicalUnitMetres, .04437132042000233d, 23.663373727853898d, 1.8504645855031836d, 168.86635062947198d, 142.9241008876921d, 1.327124400412794e20d),
        new(SolarSystemBodyIds.Neptune, SolarSystemBodyIds.Sun, 4_503_672_132_547.987d / AstronomicalUnitMetres, .011212677874712446d, 22.297808829138386d, 3.4756056094738432d, 33.977349056484876d, 268.0284779165326d, 1.327124400412794e20d),
    ];

    private static class Holder { internal static readonly CelestialSystemDefinition Value = Create(true); }

    private static CelestialSystemDefinition Create(bool includePeriodicCorrection)
    {
        var metadata = Metadata(SystemSource, includePeriodicCorrection);
        var bodies = new[]
        {
            Body(SolarSystemBodyIds.Sun, "Sun", CelestialBodyClassification.Star, null, 1.327124400412794e20d, 695_700_000d, 696_340_000d, 695_700_000d),
            Body(SolarSystemBodyIds.Mercury, "Mercury", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 2.2032e13d, 2_439_700d, 2_439_700d, 2_439_700d),
            Body(SolarSystemBodyIds.Venus, "Venus", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 3.24859e14d, 6_051_800d, 6_051_800d, 6_051_800d),
            Body(SolarSystemBodyIds.Earth, "Earth", CelestialBodyClassification.Planet, SolarSystemBodyIds.Sun, 3.986004355070226e14d, 6_371_008.8d, 6_378_137d, 6_356_752.314245d),
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
        var corrections = new AnalyticalKeplerSecularCorrection[Elements.Length]; corrections[3] = LunarCorrection;
        var periodicCorrections = new AnalyticalKeplerPeriodicCorrection[Elements.Length]; for (var index = 0; index < periodicCorrections.Length; index++) periodicCorrections[index] = AnalyticalKeplerPeriodicCorrection.Identity; if (includePeriodicCorrection) periodicCorrections[3] = LunarPeriodicCorrection;
        var sources = new[] { new CelestialEphemerisSource(FixedSource, CelestialTrajectoryModel.FixedBody, Metadata(FixedSource, includePeriodicCorrection)), new CelestialEphemerisSource(KeplerSource, CelestialTrajectoryModel.AnalyticalKepler, Metadata(KeplerSource, includePeriodicCorrection)) };
        var id = includePeriodicCorrection ? Id : new CelestialSystemId(10_003);
        if (CelestialSystemDefinition.TryCreate(id, bodies, nodes, CelestialSystemTimeMapping.Identity(Domain), metadata, sources, [FixedBodyEphemerisPayload.Identity], [], trajectories, corrections, periodicCorrections, out var definition, out _)) return definition!;
        throw new InvalidOperationException("SolAnalytical authored data failed validation.");
    }

    private static CelestialHierarchyNode Node(CelestialBodyId id, int payload) => new(id, new(CelestialTrajectoryModel.AnalyticalKepler, KeplerSource, payload));
    private static CelestialBodyCatalogEntry Body(CelestialBodyId id, string name, CelestialBodyClassification kind, CelestialBodyId? parent, double mu, double mean, double equatorial, double polar) => new(new(id, name, kind, parent, default, default, default), new(mu, mean, equatorial, polar, equatorial == 0d ? 0d : (equatorial - polar) / equatorial, default, default, default, PhysicalSource, ConstantsVersion));
    private static CelestialEphemerisMetadata Metadata(CelestialEphemerisSourceId source, bool periodic) => new(source, new(periodic ? 4UL : 3UL), Domain, -CoverageSeconds * SimulationInstant.TicksPerSecond, CoverageSeconds * SimulationInstant.TicksPerSecond, new(10_001), ConstantsVersion, new(0x444534343056414CUL, periodic ? 0x4944415445445633UL : 0x4944415445445632UL), new(0, 0));

    private static AnalyticalKeplerSecularCorrection CreateLunarCorrection()
    {
        const double timeScaleDelta = .0070739315409438d;
        const double nodeDegreesPerJulianYear = -19.165429687499998d;
        const double periapsisDegreesPerJulianYear = 40.70390243530275d;
        const double obliquityRadians = 23.439291111d * Math.PI / 180d;
        var nodeRate = nodeDegreesPerJulianYear * Math.PI / 180d / JulianYearSeconds;
        var planeAngularVelocity = new Double3(0d, -Math.Sin(obliquityRadians) * nodeRate, Math.Cos(obliquityRadians) * nodeRate);
        return new(timeScaleDelta, planeAngularVelocity, periapsisDegreesPerJulianYear * Math.PI / 180d / JulianYearSeconds);
    }

    private static AnalyticalKeplerPeriodicCorrection CreateLunarPeriodicCorrection() => new(
    [
        new(2.6377142567586474e-6d, 13_907_036.829921206d, 13_872_024.118660487d, 0d, 0d),
        new(2.6508409809764152e-6d, -12_141_588.938906228d, -14_669_570.81972078d, 0d, 0d),
        new(4.925298388708774e-6d, -2_434_249.1222010353d, 1_732_004.2527811143d, 0d, 0d),
        new(2.6374618197544597e-6d, 0d, 0d, -0.07902480520654191d, 0.07950143129855441d),
        new(2.651093417980603e-6d, 0d, 0d, 0.07511196305917033d, -0.06282794773814399d),
        new(2.2860695099249988e-6d, 3_608_223.0155419977d, 732_977.9825758068d, -0.004075551465891044d, 0.020721697912036464d),
        new(2.6316557686581393e-6d, 0d, 0d, 0.013295345778247099d, -0.015460614439123546d),
    ]);

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
