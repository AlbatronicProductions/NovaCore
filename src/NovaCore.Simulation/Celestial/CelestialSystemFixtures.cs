using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Small immutable authored fixtures. They are topology/evaluation examples, not simulation state or render content.</summary>
internal static class CelestialSystemFixtures
{
    private static readonly CelestialSystemTimeMapping IdentityMapping = CelestialSystemTimeMapping.Identity(new(1));
    private static readonly CelestialEphemerisMetadata FixtureMetadata = Metadata(1);

    internal static CelestialSystemDefinition SolMini { get; } = Create(new(1),
    [
        Node(1, null, 1, 1.32712440018e20, CelestialTrajectoryModel.FixedBody, 1, 0),
        Node(2, 1, 2, 3.986004418e14, CelestialTrajectoryModel.AnalyticalKepler, 2, 0),
        Node(3, 2, 3, 4.9048695e12, CelestialTrajectoryModel.AnalyticalKepler, 2, 1),
    ], Sources(1, true, false), [FixedBodyEphemerisPayload.Identity], [], [Kepler(1, 149_597_870_700d, 1.32712440018e20), Kepler(2, 384_400_000d, 3.986004418e14)]);

    internal static CelestialSystemDefinition GeocentricDemo { get; } = Create(new(2),
    [
        Node(10, null, 10, 3.986004418e14, CelestialTrajectoryModel.FixedBody, 1, 0),
        Node(20, 10, 20, 4.9048695e12, CelestialTrajectoryModel.CircularOrbit, 3, 0),
    ], Sources(1, false, true), [FixedBodyEphemerisPayload.Identity], [Circular(384_400_000d, 3.986004418e14)], []);

    internal static CelestialSystemDefinition BinaryDemo { get; } = Create(new(3),
    [
        Node(100, null, 100, 8e19, CelestialTrajectoryModel.FixedBody, 1, 0),
        Node(200, 100, 200, 6e19, CelestialTrajectoryModel.CircularOrbit, 3, 0),
    ], Sources(1, false, true), [FixedBodyEphemerisPayload.Identity], [Circular(1_000_000_000d, 8e19)], []);

    private static CelestialSystemDefinition Create(CelestialSystemId id, CelestialHierarchyNode[] nodes, CelestialEphemerisSource[] sources, FixedBodyEphemerisPayload[] fixedBodies, CircularOrbitEphemerisPayload[] circular, TwoBodyTrajectory[] kepler)
    {
        if (CelestialSystemDefinition.TryCreate(id, nodes, IdentityMapping, FixtureMetadata, sources, fixedBodies, circular, kepler, out var definition, out _)) return definition!;
        throw new InvalidOperationException("Built-in celestial-system fixture is invalid.");
    }
    private static CelestialEphemerisSource[] Sources(ulong fixedId, bool kepler, bool circular)
    {
        var values = new CelestialEphemerisSource[1 + (kepler ? 1 : 0) + (circular ? 1 : 0)]; values[0] = new(new(fixedId), CelestialTrajectoryModel.FixedBody, Metadata(fixedId)); var index = 1;
        if (kepler) values[index++] = new(new(2), CelestialTrajectoryModel.AnalyticalKepler, Metadata(2));
        if (circular) values[index] = new(new(3), CelestialTrajectoryModel.CircularOrbit, Metadata(3)); return values;
    }
    private static CelestialEphemerisMetadata Metadata(ulong source) => new(new(source), new(1), new(1), long.MinValue, long.MaxValue, new(1), new(1), new(0, 0), new(0, 0));
    private static CelestialHierarchyNode Node(ulong id, ulong? parent, long frame, double mu, CelestialTrajectoryModel model, ulong source, int index) => new(new(new(id), parent is { } value ? new CelestialBodyId(value) : null, new ReferenceFrameId(frame), mu), new CelestialEphemerisBinding(model, new(source), index));
    private static TwoBodyTrajectory Kepler(ulong central, double radius, double mu) => new(new(central), SimulationInstant.Zero, new(new Double3(radius, 0d, 0d), new Double3(0d, Math.Sqrt(mu / radius), 0d)), TwoBodyPropagationModel.CartesianTwoBodyV1);
    private static CircularOrbitEphemerisPayload Circular(double radius, double mu) => new(0, radius, 0d, DoubleQuaternion.Identity, mu);
}
