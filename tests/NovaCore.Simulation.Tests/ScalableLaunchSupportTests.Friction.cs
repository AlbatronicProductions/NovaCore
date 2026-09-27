using System.Numerics;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities.Memory;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ScalableLaunchSupportTests
{
    private static void FrictionMapping()
    {
        Probe<Contact1OneBodyPrestepData,Contact1AccumulatedImpulses,Contact1OneBodyFunctions>();
        Probe<Contact2OneBodyPrestepData,Contact2AccumulatedImpulses,Contact2OneBodyFunctions>();
        Probe<Contact3OneBodyPrestepData,Contact3AccumulatedImpulses,Contact3OneBodyFunctions>();
        Probe<Contact4OneBodyPrestepData,Contact4AccumulatedImpulses,Contact4OneBodyFunctions>();
        static void Probe<TP,TI,TF>() where TP:unmanaged,IConvexContactPrestep<TP>
            where TI:unmanaged,IConvexContactAccumulatedImpulses<TI> where TF:unmanaged,IOneBodyConstraintFunctions<TP,TI>
        {
            var pool=new BufferPool();var metrics=new LocalContactMetrics();
            var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-9.81f,0)),new SolveDescription(8,1));
            try{
                sim.Solver.Register<CraftContactDescription<TP,TI,TF>>();
                // Fixed orientation isolates translation and the physical cone.
                var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Vector3.Zero),new BodyVelocity(new(100,0,0)),new BodyInertia{InverseMass=1},default,new(-1)));
                var manifold=new ConvexContactManifold{Count=TP.ContactCount,Normal=Vector3.UnitY};
                for(var i=0;i<TP.ContactCount;i++)manifold[i]=new(){Normal=Vector3.UnitY,Offset=new(i%2,0,i/2),FeatureId=i};
                var handle=sim.Solver.Add(body,new CraftContactDescription<TP,TI,TF>{Manifold=manifold,Material=CraftContactManifolds.Material});
                sim.Timestep(1f/1024);
                Span<float> normal=stackalloc float[4];CraftContactDescription<TP,TI,TF>.ReadImpulses(sim.Solver,handle,normal,out var tangent,out _);
                var sum=0f;for(var i=0;i<TP.ContactCount;i++)sum+=normal[i];
                Need(sum>0&&Math.Abs(tangent.Length()/sum-.5)<1e-5,$"physical Coulomb coefficient independent of native arity {TP.ContactCount}");
                sim.Solver.GetDescription(handle,out CraftContactDescription<TP,TI,TF> roundtrip);
                Need(roundtrip.Material.FrictionCoefficient==.5f,"native material adapter roundtrip returns physical coefficient");
                sim.Solver.Remove(handle);
            }finally{sim.Dispose();pool.Clear();}
        }
    }
}
