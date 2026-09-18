using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

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

internal sealed class LocalContactStepInput
{
    internal Vector3 Linear, Angular;
    internal AssemblyFloridaSite? Site;
    internal AssemblySiteFrame SiteFrame;
    internal Matrix3 SiteInertia;
    internal Double3 SiteOrigin;
}

internal struct LocalContactIntegrator(Vector3 acceleration, LocalContactStepInput? prepared = null) : IPoseIntegratorCallbacks
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
        if(prepared?.Site is {} site)
        {
            // Only the bounded engine-off site profile. All frame/model values were
            // prepared at the exact source epoch by the transaction owner.
            for(var i=0;i<Vector<float>.Count;i++)
            {
                if(integrationMask[i]==0)continue;
                var p=new Double3(position.X[i],position.Y[i],position.Z[i])+prepared.SiteOrigin;
                var v=new Double3(velocity.Linear.X[i],velocity.Linear.Y[i],velocity.Linear.Z[i]);
                var w=new Double3(velocity.Angular.X[i],velocity.Angular.Y[i],velocity.Angular.Z[i]);
                var q=(new DoubleQuaternion(orientation.X[i],orientation.Y[i],orientation.Z[i],orientation.W[i])*AssemblyContactProfile.Upright).Normalized();
                var a=site.LinearAcceleration(prepared.SiteFrame,p,v);
                // BEPU retains its relative-rate gyro solve. This is the difference
                // needed for physical absolute angular momentum in rotating axes.
                var alpha=AssemblyFloridaSite.AngularCorrection(prepared.SiteFrame,q,w,prepared.SiteInertia);
                velocity.Linear.X=Vector.WithElement(velocity.Linear.X,i,velocity.Linear.X[i]+(float)a.X*dt[i]);
                velocity.Linear.Y=Vector.WithElement(velocity.Linear.Y,i,velocity.Linear.Y[i]+(float)a.Y*dt[i]);
                velocity.Linear.Z=Vector.WithElement(velocity.Linear.Z,i,velocity.Linear.Z[i]+(float)a.Z*dt[i]);
                velocity.Angular.X=Vector.WithElement(velocity.Angular.X,i,velocity.Angular.X[i]+(float)alpha.X*dt[i]);
                velocity.Angular.Y=Vector.WithElement(velocity.Angular.Y,i,velocity.Angular.Y[i]+(float)alpha.Y*dt[i]);
                velocity.Angular.Z=Vector.WithElement(velocity.Angular.Z,i,velocity.Angular.Z[i]+(float)alpha.Z*dt[i]);
            }
            return;
        }
        // No hidden gravity: source force / source mass, transformed once into the fixed local frame.
        var linear = prepared is null ? acceleration : prepared.Linear;
        velocity.Linear.X += new Vector<float>(linear.X) * dt;
        velocity.Linear.Y += new Vector<float>(linear.Y) * dt;
        velocity.Linear.Z += new Vector<float>(linear.Z) * dt;
        if (prepared is not null)
        {
            velocity.Angular.X += new Vector<float>(prepared.Angular.X) * dt;
            velocity.Angular.Y += new Vector<float>(prepared.Angular.Y) * dt;
            velocity.Angular.Z += new Vector<float>(prepared.Angular.Z) * dt;
        }
    }
}
