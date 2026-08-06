using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Small immutable validation fixtures. They are authored topology examples, not simulation state or render content.</summary>
internal static class CelestialSystemFixtures
{
    internal static CelestialSystemDefinition SolMini { get; } = Create(new(1),
    [
        Node(1, null, 1, 1.32712440018e20, CelestialTrajectoryModel.FixedBody),
        Node(2, 1, 2, 3.986004418e14, CelestialTrajectoryModel.AnalyticalKepler, 149_597_870_700d),
        Node(3, 2, 3, 4.9048695e12, CelestialTrajectoryModel.AnalyticalKepler, 384_400_000d),
    ]);
    internal static CelestialSystemDefinition GeocentricDemo { get; } = Create(new(2),
    [
        Node(10, null, 10, 3.986004418e14, CelestialTrajectoryModel.FixedBody),
        Node(20, 10, 20, 4.9048695e12, CelestialTrajectoryModel.CircularOrbit, 384_400_000d),
    ]);
    internal static CelestialSystemDefinition BinaryDemo { get; } = Create(new(3),
    [
        Node(100, null, 100, 8.0e19, CelestialTrajectoryModel.FixedBody),
        Node(200, 100, 200, 6.0e19, CelestialTrajectoryModel.CircularOrbit, 1_000_000_000d),
    ]);

    private static CelestialSystemDefinition Create(CelestialSystemId id, CelestialHierarchyNode[] nodes)
    {
        if (CelestialSystemDefinition.TryCreate(id, nodes, out var definition, out _)) return definition!;
        throw new InvalidOperationException("Built-in celestial-system fixture is invalid.");
    }
    private static CelestialHierarchyNode Node(ulong id, ulong? parent, long frame, double mu, CelestialTrajectoryModel model, double radius = 0d)
    {
        var body = new CelestialBodyDefinition(new(id), parent is { } value ? new CelestialBodyId(value) : null, new ReferenceFrameId(frame), mu);
        if (parent is not { } central) return new(body, model);
        var parentMu = central switch { 1 => 1.32712440018e20, 100 => 8.0e19, _ => 3.986004418e14 };
        var state = new CartesianState(new Double3(radius, 0d, 0d), new Double3(0d, Math.Sqrt(parentMu / radius), 0d));
        return new(body, model, new TwoBodyTrajectory(new CelestialBodyId(central), SimulationInstant.Zero, state, TwoBodyPropagationModel.CartesianTwoBodyV1));
    }
}
