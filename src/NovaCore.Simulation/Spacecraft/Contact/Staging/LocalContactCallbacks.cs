using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed class LocalContactMetrics
{
    internal int Contacts;
    internal float MaximumDepth;
    internal int ArticleChildMask;
    internal CompoundContactCoverage? Coverage;
}

internal struct LocalContactCallbacks(LocalContactMetrics metrics) : INarrowPhaseCallbacks
{
    public void Initialize(BepuPhysics.Simulation simulation) { }
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float margin) =>
        a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childA, int childB) => true;
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties material) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Qualification dry contact: dimensionless mu=.5, critical damping, 30 Hz spring.
        // Recovery 2 m/s bounds penetration correction separately from physical source force.
        material = new PairMaterialProperties(.5f, 2f, new SpringSettings(30, 1));
        if (metrics.Coverage is { } coverage && !coverage.Parent(workerIndex, pair, ref manifold)) return false;
        metrics.Contacts = Math.Max(metrics.Contacts, manifold.Count);
        for (var i = 0; i < manifold.Count; i++) metrics.MaximumDepth = Math.Max(metrics.MaximumDepth, manifold.GetDepth(i));
        return true;
    }
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childA, int childB,
        ref ConvexContactManifold manifold)
    {
        // Bounded diagnostic only. Independent corner/support tests establish physical support;
        // this mask establishes which of the three distinct compound children supplied manifolds.
        var child = pair.A.Mobility == CollidableMobility.Dynamic ? childA : childB;
        if (manifold.Count > 0 && (uint)child < EngineeringContactArticle.ChildCount)
            metrics.ArticleChildMask |= 1 << child;
        return metrics.Coverage?.Child(workerIndex, pair, childA, childB, ref manifold) ?? true;
    }
    public void Dispose() { }
}

internal struct LocalContactIntegrator(Vector3 acceleration) : IPoseIntegratorCallbacks
{
    // Torque-free asymmetric bodies still need gyroscopic angular momentum integration.
    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.ConserveMomentumWithGyroscopicTorque;
    public bool AllowSubstepsForUnconstrainedBodies => false;
    public bool IntegrateVelocityForKinematics => false;
    public void Initialize(BepuPhysics.Simulation simulation) { }
    public void PrepareForIntegration(float dt) { }
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        // No hidden gravity: source force / source mass, transformed once into the fixed local frame.
        velocity.Linear.X += new Vector<float>(acceleration.X) * dt;
        velocity.Linear.Y += new Vector<float>(acceleration.Y) * dt;
        velocity.Linear.Z += new Vector<float>(acceleration.Z) * dt;
    }
}
