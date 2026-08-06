using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Small immutable validation fixtures. They are authored topology examples, not simulation state or render content.</summary>
internal static class CelestialSystemFixtures
{
    internal static CelestialSystemDefinition SolMini { get; } = Create(new(1),
    [
        Node(1, null, 1, 1.32712440018e20, CelestialTrajectoryModel.FixedBody),
        Node(2, 1, 2, 3.986004418e14, CelestialTrajectoryModel.AnalyticalKepler),
        Node(3, 2, 3, 4.9048695e12, CelestialTrajectoryModel.AnalyticalKepler),
    ]);
    internal static CelestialSystemDefinition GeocentricDemo { get; } = Create(new(2),
    [
        Node(10, null, 10, 3.986004418e14, CelestialTrajectoryModel.FixedBody),
        Node(20, 10, 20, 4.9048695e12, CelestialTrajectoryModel.CircularOrbit),
    ]);
    internal static CelestialSystemDefinition BinaryDemo { get; } = Create(new(3),
    [
        Node(100, null, 100, 8.0e19, CelestialTrajectoryModel.FixedBody),
        Node(200, 100, 200, 6.0e19, CelestialTrajectoryModel.ReservedNumericalNBody),
    ]);

    private static CelestialSystemDefinition Create(CelestialSystemId id, CelestialHierarchyNode[] nodes)
    {
        if (CelestialSystemDefinition.TryCreate(id, nodes, out var definition, out _)) return definition!;
        throw new InvalidOperationException("Built-in celestial-system fixture is invalid.");
    }
    private static CelestialHierarchyNode Node(ulong id, ulong? parent, long frame, double mu, CelestialTrajectoryModel model) =>
        new(new CelestialBodyDefinition(new(id), parent is { } value ? new CelestialBodyId(value) : null, new ReferenceFrameId(frame), mu), model);
}
