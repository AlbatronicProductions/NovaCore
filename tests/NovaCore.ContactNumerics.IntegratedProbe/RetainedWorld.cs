using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using S=PieceKernel.State;
using V=PieceKernel.V;

// One private BEPU world. This harness never constructs a canonical engine/resource/actuator.
internal sealed class RetainedWorld:IDisposable
{
    private readonly BufferPool pool=new(16384);
    private readonly Capture capture=new();
    private readonly Simulation simulation;
    private readonly BodyHandle body;
    private readonly ConstraintHandle constraint;
    private RigidPose expectedPose;
    internal readonly RetainedOperator Operator;
    internal bool Unsafe {get;private set;}
    internal int AcceptedPieces {get;private set;}
    internal ContactGeometry CurrentGeometry {get;private set;}
    internal ulong NativePoolBytes=>pool.GetTotalAllocatedByteCount();
    internal RigidPose Pose=>simulation.Bodies[body].Pose;
    internal S Velocity {get{var v=simulation.Bodies[body].Velocity;return new(V.From(v.Linear),V.From(v.Angular));}}
    internal RetainedWorld(double sourceMass=8+1d/128)
    {
        simulation=Simulation.Create(pool,new Callbacks(capture),new Integrator(),new SolveDescription(8,1));
        var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
        var inertia=new BodyInertia{InverseMass=(float)(1/sourceMass),InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
        body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
            new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
        simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
        for(int i=0;i<120;i++)simulation.Timestep(1f/60);
        Qualification.Require(capture.Count==4&&simulation.Bodies[body].Constraints.Count==1,"genuine prepared retained convex world");
        constraint=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;
        var reader=new Extractor(capture,false);
        Qualification.Require(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(constraint,ref reader),"last solved native prestep/cache captured before refresh");
        CurrentGeometry=ContactGeometry.From(capture);expectedPose=Pose;
        var cache=CacheVector.From(capture.Impulses.Select(x=>(double)x).ToArray());
        Operator=new(new(new(1,body.Value,1,constraint.Value,1),1,0,PieceKind.Preparation,CurrentGeometry,
            Qualification.Binary((double)(1f/60)),cache,Qualification.Gravity,Velocity));
    }
    // Read the actual current tiny-piece contact equations before choosing an admissible reference.
    // Zero FP32 projection here affects only the bounded local collision-envelope query: no timestep,
    // cache scaling, time advance or physical solve occurs. Exact duration remains a separate input.
    internal ContactGeometry ObserveTinyGeometry(CacheDuration exact,double mass)
    {
        Qualification.Require(exact.Valid&&exact.Origin==DurationOrigin.ExactEvent&&!exact.Exact.IsZero,"positive exact event authority");
        Qualification.Require(!Unsafe&&!Operator.Invalidated,"safe source before tiny geometry refresh");Unsafe=true;
        simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/mass);
        simulation.PredictBoundingBoxes((float)exact.Numerical.Value);simulation.CollisionDetection((float)exact.Numerical.Value);
        Qualification.Require(capture.Count==4&&simulation.Bodies[body].Constraints.Count==1&&
            simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle==constraint,"same actual tiny constraint");
        var reader=new Extractor(capture,false);
        Qualification.Require(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(constraint,ref reader),"actual current tiny prestep");
        CurrentGeometry=ContactGeometry.From(capture);return CurrentGeometry;
    }
    internal OperatorStatus Prepare(double mass,Load load,CacheDuration duration,out RetainedOperator.Proposal proposed,out RetainedOperator.Diagnostics proof)
    {
        proposed=default;proof=default;
        if(Unsafe||Operator.Invalidated)return OperatorStatus.Invalidated;
        if(Pose.Position!=expectedPose.Position||Pose.Orientation!=expectedPose.Orientation||Velocity!=Operator.Snapshot.Endpoint)
            return OperatorStatus.StaleLineage;
        Unsafe=true;
        simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/mass);
        // Collision geometry is an FP32 bounded local consumer, not exact duration authority.
        var nativeH=(float)duration.Numerical.Value;
        simulation.PredictBoundingBoxes(nativeH);simulation.CollisionDetection(nativeH);
        if(capture.Count!=4||simulation.Bodies[body].Constraints.Count!=1||simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle!=constraint)
        {Operator.Invalidate();return OperatorStatus.StaleLineage;}
        var reader=new Extractor(capture,false);
        if(!simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(constraint,ref reader))
        {Operator.Invalidate();return OperatorStatus.StaleLineage;}
        CurrentGeometry=ContactGeometry.From(capture);
        Span<double> native=stackalloc double[7];for(int i=0;i<7;i++)native[i]=capture.Impulses[i];
        var before=Operator.Snapshot;
        var status=Operator.Prepare(before.Identity,before.Generation,before.Piece+1,
            load.Force==default&&load.Torque==default?PieceKind.Coast:PieceKind.Powered,CurrentGeometry,duration,
            Qualification.Body(mass),load,Velocity,native,out proposed,out proof);
        if(status!=OperatorStatus.Ready)Operator.Invalidate();
        return status;
    }
    internal void Install(in RetainedOperator.Proposal proposed)
    {
        var target=proposed.Target;var v=target.Endpoint;var h=(float)target.ProducingDuration.Numerical.Value;
        var linear=new Vector3((float)v.Linear.X,(float)v.Linear.Y,(float)v.Linear.Z);
        var angular=new Vector3((float)v.Angular.X,(float)v.Angular.Y,(float)v.Angular.Z);
        var pose=Pose;pose.Position+=linear*h;
        var dq=Quaternion.Multiply(new Quaternion(angular,0),pose.Orientation);
        pose.Orientation=Quaternion.Normalize(new(pose.Orientation.X+.5f*h*dq.X,pose.Orientation.Y+.5f*h*dq.Y,
            pose.Orientation.Z+.5f*h*dq.Z,pose.Orientation.W+.5f*h*dq.W));
        Qualification.Require(float.IsFinite(pose.Position.Length())&&float.IsFinite(pose.Orientation.Length()),"prepared finite pose projection");
        for(int i=0;i<7;i++)capture.Impulses[i]=(float)target.Cache.At(i).Value;
        var endpoint=new S(V.From(linear),V.From(angular));
        Qualification.Require(Operator.BeginInstall(proposed,endpoint)==OperatorStatus.Ready,"exact proposal enters private installation");
        try
        {
            var writer=new Extractor(capture,true);
            Qualification.Require(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(constraint,ref writer),"native cache installation");
            simulation.Bodies[body].Velocity.Linear=linear;simulation.Bodies[body].Velocity.Angular=angular;simulation.Bodies[body].Pose=pose;
            var reader=new Extractor(capture,false);Qualification.Require(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(constraint,ref reader),"native installed verification");
            Span<double> native=stackalloc double[7];for(int i=0;i<7;i++)native[i]=capture.Impulses[i];
            Qualification.Require(target.Cache.MatchesTransport(native)&&Velocity==endpoint,"same installed cache and physical endpoint");
            Qualification.Require(Operator.CompleteInstall(proposed)==OperatorStatus.Ready,"promote exact cache geometry duration and piece together");
            expectedPose=pose;Unsafe=false;AcceptedPieces++;
        }
        catch{Operator.Invalidate();throw;}
    }
    public void Dispose(){Operator.Invalidate();simulation.Dispose();pool.Clear();}
}

internal static partial class Qualification
{
    private static void Sequence()
    {
        Gate="genuine-retained-seed";using var world=new RetainedWorld();
        var seed=world.Operator.Snapshot;var seedPose=world.Pose;
        Save("sequence-seed.json",new{status="CAPTURED",seed,position=seedPose.Position,orientation=seedPose.Orientation,worlds=1,
            geometryCapturedBeforePoweredRefresh=true,preparationSteps=120,nativePoolBytes=world.NativePoolBytes});
        var powered=Exact(1d/128,1);
        var coast=ExactCoast();
        try
        {
            foreach(var item in new[]{(Name:"powered",Duration:powered,Mass:MidMass,Load:Powered),(Name:"coast",Duration:coast,Mass:8d,Load:Gravity)})
            {
                Gate="retained-"+item.Name;var before=world.Operator.Snapshot;var source=world.Velocity;var previousPose=world.Pose;
                var status=world.Prepare(item.Mass,item.Load,item.Duration,out var proposal,out var proof);
                if(status!=OperatorStatus.Ready)
                {Save("sequence-"+item.Name+".json",new{status,proof,before,current=world.CurrentGeometry,acceptedPieces=world.AcceptedPieces,world.Unsafe});Require(false,"actual retained admission "+status);}
                var comparison=Compare(world.CurrentGeometry,item.Mass,item.Load,source,item.Duration,proposal);
                Save("sequence-"+item.Name+".json",new{status=comparison.Error.Pass?"PASS":"FAIL",before,current=world.CurrentGeometry,
                    item.Duration,item.Load,item.Mass,source,proof,target=proposal.Target,comparison.Error,comparison.Reference,
                    acceptedUnchanged=world.Operator.Snapshot==before,position=previousPose.Position,orientation=previousPose.Orientation});
                Require(comparison.Error.Pass,"actual retained piece reference accuracy");
                Require(world.Operator.Snapshot==before,"proposal cannot mutate accepted provenance");
                world.Install(proposal);var after=world.Operator.Snapshot;
                Save("installed-"+item.Name+".json",new{status="PASS",after,position=world.Pose.Position,orientation=world.Pose.Orientation,
                    acceptedPieces=world.AcceptedPieces,world.Unsafe,identityUnchanged=after.Identity==seed.Identity,
                    producingGeometryExact=after.ProducingGeometry==proposal.Target.ProducingGeometry});
                Require(after.Generation==before.Generation+1&&after.Piece==before.Piece+1&&after.ProducingDuration==item.Duration&&
                    after.ProducingGeometry==proposal.Target.ProducingGeometry&&after.Cache==proposal.Target.Cache,"atomic accepted tuple and producing provenance");
            }
            var pose=world.Pose;double deepest=0;
            foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})
                deepest=Math.Max(deepest,-(pose.Position+Vector3.Transform(new Vector3(x,y,z),pose.Orientation)).Y);
            Require(deepest<=.020&&Math.Abs(world.Operator.Snapshot.Endpoint.Linear.Y)<=.0005/(16667d/1e6),"independent retained support bounds");
            Save("powered-coast.json",new{status="PASS",acceptedPieces=world.AcceptedPieces,worldsCreated=1,
                poweredSeconds=powered.Numerical.Value,coastSeconds=coast.Numerical.Value,totalTicks=16666,deepest,
                normalVelocity=world.Operator.Snapshot.Endpoint.Linear.Y,canonicalCapabilities=0,canonicalMutations=0,reconstruction=false});
        }
        catch
        {
            world.Operator.Invalidate();Save("sequence-linearization.json",new{status="STOP",gate=Gate,world.AcceptedPieces,
                world.Unsafe,invalidated=world.Operator.Invalidated,lastAccepted=world.Operator.Snapshot,canonicalMutations=0});throw;
        }
    }
}
