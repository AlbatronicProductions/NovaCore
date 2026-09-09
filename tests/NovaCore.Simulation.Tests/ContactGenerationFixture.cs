using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

/// <summary>Shared permanent physical fixture for Simulation oracles and the real CPU-terrain integration test.</summary>
internal static class ContactGenerationFixture
{
    internal static readonly SpacecraftId Craft = new(71);
    internal static ReferenceFrameGraph Graph(long rootId = 1)
    {
        var b = new ReferenceFrameGraphBuilder();
        b.Add(new ReferenceFrameNode(new(rootId), null, ReferenceFrameKind.Ecl, "Inertial root"));
        b.Add(new ReferenceFrameNode(new(72), new(rootId), ReferenceFrameKind.Ccf, "Lander COM"));
        return b.Build();
    }
    internal static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("Contact: " + message); }
    internal static SpacecraftContactGeometry Geometry(int count, uint version = 1)
    {
        // Authored lower support hardpoints: paired fore/aft and lateral contacts in +X forward,+Y right,+Z down.
        // These physical locations remain meaningful independently of future hull/leg collision or suspension.
        var features = new SpacecraftContactFeature[count];
        for (var i = 0; i < count; i++) features[i] = new((ulong)i + 1,
            new((i % 2 == 0 ? -1 : 1) * (1 + i / 4), i % 4 < 2 ? -1 : 1, 1), ContactFeatureRole.SupportHardpoint);
        Check(SpacecraftContactGeometry.TryCreate(Craft, 101, version, features, out var geometry), "admit authored lander hardpoints");
        return geometry!;
    }
    internal static SimulationState State(Double3 position, Double3 velocity, DoubleQuaternion orientation,
        Double3 omegaBody, SimulationInstant time = default, long root = 1)
    {
        var rotation = new SpacecraftRigidBodyRotationState(Craft, time, orientation, omegaBody,
            new(2, 3, 4), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, new(root), new(72), "Four-hardpoint lander")],
            [rotation], [new(8)], [new(Craft, new(root), time, position, velocity, Double3.Zero)], Graph(root), out var store, out _), "admit physical craft");
        return new(spacecraft: store);
    }
    internal static CelestialSystemDefinition StaticSystem(Double3 center, Double3 velocity, bool ancestor = false)
    {
        var metadata = new CelestialEphemerisMetadata(new(1), new(1), new(1), long.MinValue, long.MaxValue, new(1), new(1), new(0, 0), new(0, 0));
        var earth = new CelestialBodyCatalogEntry(new(new(6), "Analytical Earth", CelestialBodyClassification.Other,
            ancestor ? new CelestialBodyId(1) : null, default, default, default), new(1, 16, 16, 16, 0, default, default, default));
        CelestialBodyCatalogEntry[] bodies = ancestor
            ? [new(new(new(1), "Moving ancestor", CelestialBodyClassification.Other, null, default, default, default), new(1, 1, 1, 1, 0, default, default, default)), earth]
            : [earth];
        CelestialHierarchyNode[] nodes = ancestor
            ? [new(new(1), new(CelestialTrajectoryModel.FixedBody, new(1), 0)), new(new(6), new(CelestialTrajectoryModel.FixedBody, new(1), 1))]
            : [new(new(6), new(CelestialTrajectoryModel.FixedBody, new(1), 0))];
        FixedBodyEphemerisPayload[] fixedBodies = ancestor
            ? [new(center, velocity, DoubleQuaternion.Identity, Double3.Zero), new(new(10, 20, 30), new(4, 5, 6), DoubleQuaternion.Identity, Double3.Zero)]
            : [new(center, velocity, DoubleQuaternion.Identity, Double3.Zero)];
        Check(CelestialSystemDefinition.TryCreate(new(700), bodies, nodes, CelestialSystemTimeMapping.Identity(new(1)), metadata,
            [new(new(1), CelestialTrajectoryModel.FixedBody, metadata)], fixedBodies, [], [], out var system, out _), "admit analytical celestial system");
        return system!;
    }
    internal static ContactBodyMotion Body(CelestialSystemDefinition system, SimulationInstant time = default, long root = 1)
    {
        Check(ContactBodyMotion.TryEvaluate(system, Graph(root), new(6), time,
            new ReferenceFrameEvaluation[system.Count], new FrameTransform[system.Count],
            new ReferenceFrameEvaluation[system.Count], new FrameTransform[system.Count], out var body) == ContactGenerationStatus.Ready, "fresh body evaluation");
        return body;
    }
    internal sealed class AnalyticalSurface(double slope = 0, bool plane = false) : IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority { get; } = new(6, new(2, 5), 4, 16, "analytical-global", "analytical-regional", 1, 1, 1);
        internal int Calls;
        internal int FailAt = 0;
        internal PhysicalSurfaceQueryStatus Failure = PhysicalSurfaceQueryStatus.NormalUnqualified;
        internal bool WrongAuthority = false;
        public PhysicalSurfacePointResult Query(ulong bodyId, in Double3 direction)
        {
            Calls++;
            if (Calls == FailAt) return new(Failure, Authority, default, default, default, default, default);
            var d = direction / Math.Sqrt(direction.LengthSquared);
            // Exact sphere, or explicit radial graph of plane z=16+slope*x (including a flat plane).
            var radius = !plane ? 16 : 16 / (d.Z - slope * d.X);
            var n = !plane ? d : new Double3(-slope, 0, 1) / Math.Sqrt(1 + slope * slope);
            return new(PhysicalSurfaceQueryStatus.Ready, WrongAuthority ? Authority with { CompositionIdentity = 2 } : Authority,
                d, d * radius, radius - 16, n, 0);
        }
    }
}
