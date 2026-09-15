using System.Numerics;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using BepuUtilities.Memory;

internal sealed class Capture
{
    internal readonly int[] Features=new int[4];
    internal readonly float[] Impulses=new float[7];
    internal readonly Contact[] Contacts=new Contact[4];
    internal float Omega, TwiceDamping, Friction, Recovery;
    internal int Count;
}
internal struct Callbacks(Capture capture):INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation){}
    public bool AllowContactGeneration(int workerIndex,CollidableReference a,CollidableReference b,ref float margin)=>true;
    public bool AllowContactGeneration(int workerIndex,CollidablePair pair,int a,int b)=>true;
    public bool ConfigureContactManifold<T>(int workerIndex,CollidablePair pair,ref T manifold,out PairMaterialProperties material)
        where T:unmanaged,IContactManifold<T>
    {
        material=new(.5f,2,new SpringSettings(30,1));
        if(!manifold.Convex||manifold.Count>4)throw new InvalidOperationException("unsupported geometry");
        capture.Count=manifold.Count;
        for(var i=0;i<manifold.Count;i++)
        { capture.Features[i]=manifold[i].FeatureId; capture.Contacts[i]=manifold[i]; }
        return true;
    }
    public bool ConfigureContactManifold(int workerIndex,CollidablePair pair,int a,int b,ref ConvexContactManifold manifold)=>true;
    public void Dispose(){}
}
internal struct Integrator:IPoseIntegratorCallbacks
{
    public AngularIntegrationMode AngularIntegrationMode=>AngularIntegrationMode.ConserveMomentumWithGyroscopicTorque;
    public bool AllowSubstepsForUnconstrainedBodies=>false;
    public bool IntegrateVelocityForKinematics=>false;
    public void Initialize(Simulation simulation){}
    public void PrepareForIntegration(float dt){}
    public void IntegrateVelocity(Vector<int> indices,Vector3Wide position,QuaternionWide orientation,
        BodyInertiaWide inertia,Vector<int> mask,int worker,Vector<float> dt,ref BodyVelocityWide velocity)
    { velocity.Linear.Y -= new Vector<float>(9.81f)*dt; }
}
internal struct Extractor(Capture capture,bool write):ISolverContactPrestepAndImpulsesExtractor
{
    public void ConvexOneBody<TP,TI>(ref TP prestep,ref TI impulses)
        where TP:struct,IConvexContactPrestep<TP>
        where TI:struct,IConvexContactAccumulatedImpulses<TI>
    {
        if(TP.ContactCount!=4)throw new InvalidOperationException("expected four actual retained rows");
        if(!write)
        {
            ref var n=ref TP.GetNormal(ref prestep);
            ref var material=ref TP.GetMaterialProperties(ref prestep);
            capture.Omega=material.SpringSettings.AngularFrequency[0];
            capture.TwiceDamping=material.SpringSettings.TwiceDampingRatio[0];
            capture.Friction=material.FrictionCoefficient[0];capture.Recovery=material.MaximumRecoveryVelocity[0];
            for(var i=0;i<4;i++)
            {
                ref var contact=ref TP.GetContact(ref prestep,i);
                capture.Contacts[i].Normal=new(n.X[0],n.Y[0],n.Z[0]);
                capture.Contacts[i].Offset=new(contact.OffsetA.X[0],contact.OffsetA.Y[0],contact.OffsetA.Z[0]);
                capture.Contacts[i].Depth=contact.Depth[0];
            }
        }
        for(var i=0;i<4;i++)Exchange(ref TI.GetPenetrationImpulseForContact(ref impulses,i),i);
        ref var tangent=ref TI.GetTangentFriction(ref impulses);
        Exchange(ref tangent.X,4);Exchange(ref tangent.Y,5);Exchange(ref TI.GetTwistFriction(ref impulses),6);
    }
    private void Exchange(ref Vector<float> vector,int index)
    {
        if(write)GatherScatter.GetFirst(ref vector)=capture.Impulses[index];
        else capture.Impulses[index]=vector[0];
    }
    public void ConvexTwoBody<TP,TI>(ref TP p,ref TI i)
        where TP:struct,ITwoBodyConvexContactPrestep<TP>
        where TI:struct,IConvexContactAccumulatedImpulses<TI> =>throw new InvalidOperationException("two dynamic bodies");
    public void NonconvexOneBody<TP,TI>(ref TP p,ref TI i)
        where TP:struct,INonconvexContactPrestep<TP>
        where TI:struct,INonconvexContactAccumulatedImpulses<TI> =>throw new InvalidOperationException("nonconvex");
    public void NonconvexTwoBody<TP,TI>(ref TP p,ref TI i)
        where TP:struct,ITwoBodyNonconvexContactPrestep<TP>
        where TI:struct,INonconvexContactAccumulatedImpulses<TI> =>throw new InvalidOperationException("nonconvex two body");
}
internal static class Program
{
    private static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
    private static void Main(string[] args)
    {
        if(args.Length>0&&args[0]=="kernel") { KernelProbe.Run(args.Length>1?args[1]:null); return; }
        var capture=new Capture();var pool=new BufferPool(16384);
        using(var simulation=Simulation.Create(pool,new Callbacks(capture),new Integrator(),new SolveDescription(8,1)))
        {
            var shape=simulation.Shapes.Add(new Box(2,1,1));
            var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=1f/8,InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
                new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            var floor=simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            simulation.PredictBoundingBoxes(1f/60);simulation.CollisionDetection(1f/60);
            Console.WriteLine(JsonSerializer.Serialize(new { stage="first_collision_refresh", contacts=capture.Count,
                constraints=simulation.Bodies[body].Constraints.Count,awake=simulation.Bodies[body].Awake,
                position=simulation.Bodies[body].Pose.Position.ToString(),orientation=simulation.Bodies[body].Pose.Orientation.ToString() }));
            Check(capture.Count==4&&simulation.Bodies[body].Constraints.Count==1,"one retained convex constraint with four rows");
            var handle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;
            var featureBefore=(int[])capture.Features.Clone();
            var expected=new[]{.25f,.5f,.75f,1f,.03125f,-.0625f,.125f};
            expected.CopyTo(capture.Impulses,0);
            var writer=new Extractor(capture,true);
            Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref writer),"direct cache write");
            Array.Clear(capture.Impulses);
            var reader=new Extractor(capture,false);
            Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"direct cache read");
            Check(capture.Impulses.SequenceEqual(expected),"all seven actual cache values roundtrip");
            simulation.PredictBoundingBoxes(1f/60);simulation.CollisionDetection(1f/60);
            var nextHandle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;
            Check(nextHandle==handle&&simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(nextHandle,ref reader),"reacquire same retained constraint");
            for(var i=0;i<4;i++)
            {
                var previous=Array.IndexOf(featureBefore,capture.Features[i]);
                Check(previous>=0&&capture.Impulses[i]==expected[previous],"normal feature/cache mapping");
            }
            for(var i=4;i<7;i++)Check(capture.Impulses[i]==expected[i],"tangent/twist cache survives refresh");
            var result=new {classification="RETAINED CACHE ACCESS WITNESS ONLY",body=body.Value,slab=floor.Value,
                constraint=handle.Value,featureBefore,featureAfter=capture.Features,cacheAfter=capture.Impulses,
                bodyPosition=simulation.Bodies[body].Pose.Position.ToString(),worldsCreated=1,collisionRefreshes=2,
                solverSteps=0,canonicalMutations=0,poolBytes=pool.GetTotalAllocatedByteCount()};
            var json=JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true});
            if(args.Length==1)File.WriteAllText(args[0],json+Environment.NewLine);
            Console.WriteLine(json);
        }
        pool.Clear();
    }
}
