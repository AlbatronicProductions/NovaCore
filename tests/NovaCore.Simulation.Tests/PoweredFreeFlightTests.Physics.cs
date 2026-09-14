using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class PoweredFreeFlightTests
{
    private sealed class Flight
    {
        internal static readonly SpacecraftId Craft = new(101);
        internal readonly SimulationClock Clock;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftCommandAuthority Commands;
        internal readonly EnginePreparationAuthority Actuation;
        internal readonly PropellantResourceAuthority Resource;
        internal readonly PoweredFlightAuthority Power;
        internal EngineActuationProposal EngineLease;
        internal PropellantProposal FuelLease;
        internal PoweredFlightProposal PhysicalLease;
        internal readonly ReferenceFrameGraph Graph;
        internal readonly SpacecraftStateStore Store;
        internal long HostSequence;
        internal Flight(double fuel=1d/128, double thrust=8, double exhaust=5120, double mountY=0,
            double omegaZ=0, int intervals=1200, int capacity=1200, bool ignite=true, long? eventAt=null,
            ulong revision=0)
        {
            var root=new ReferenceFrameId(1);var body=new ReferenceFrameId(2);var graph=new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(root,null,ReferenceFrameKind.Ecl,"isolated inertial root"));graph.Add(new ReferenceFrameNode(body,root,ReferenceFrameKind.Ccf,"authored dry COM"));Graph=graph.Build();
            Check(PropellantDefinition.TryCreate(new(1,1,1,1,1,1,1,PropellantMassLaw.CentralPointReservoirV1,8,new(2,2,2)),fuel,out var definition)==PropellantPreparationStatus.Ready,"resource fixture");
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft,root,body,"dry-8 spherical-inertia propulsion article")],
                [new(Craft,default,DoubleQuaternion.Identity,new(0,0,omegaZ),new(2,2,2),default,RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(definition!.InitialTotalMassKilograms)],[new(Craft,root,default,default,default,default)],Graph,out var store,out _),"physical fixture");Store=store!;
            Clock=new(default,new SimulationTimeline(8));Engine=new(Clock,new SimulationState(spacecraft:Store,initialRevision:new(revision)),8);
            if(eventAt is { } eventTicks) Clock.Timeline.Schedule(default,new(new(1),new(eventTicks),0,SimulationEventKind.Marker));
            Check(Engine.PrepareSpacecraftCommands(Craft,intervals,out var commands)==SpacecraftCommandStatus.Accepted,"command bind");Commands=commands!;
            Check(IdealEngineDefinition.TryCreate(1,1,new(0,mountY,0),Double3.UnitX,thrust,exhaust,true,out var engine)==EnginePreparationStatus.Ready,"engine definition");
            Check(Engine.BindSingleEnginePreparation(Commands,engine,out var actuation)==EnginePreparationStatus.Ready,"engine bind");Actuation=actuation!;
            Check(Engine.BindFinitePropellant(Actuation,definition,out var resource)==PropellantPreparationStatus.Ready,"resource bind");Resource=resource!;
            Check(Engine.BeginPoweredFreeFlight(Resource,capacity,out var power)==PoweredFlightStatus.Ready,"powered bind");Power=power!;
            if(ignite)Send(SpacecraftCommandIntent.Ignite());
            Send(SpacecraftCommandIntent.ThrottlePosition(1));
        }
        internal void Send(SpacecraftCommandIntent intent)
        {
            Engine.ObserveSpacecraftCommands(Commands,out var current);
            Check(Engine.AdmitSpacecraftCommand(Commands,1,current.LastAcceptedSequence+1,intent).Status==SpacecraftCommandStatus.Accepted,"command admission");
        }
        internal void Credit(long ticks)
        {
            var result=Engine.AdmitPoweredHostTime(Power,HostSequence+1,new(ticks));
            Check(result.Status==PoweredFlightStatus.AcceptedCredit,"owner host credit "+result.Status);HostSequence++;
        }
        internal PropellantSegmentationPreview Seal(bool fund=true)
        {
            Check(Commands.TryBoundaryAtOrAfter(Clock.CurrentTime,Engine.ObservePoweredFreeFlight(Power).Observation.Actuator.Frontier+1,out _,out var target),"next target");
            if(fund)Credit(target.Ticks-Clock.CurrentTime.Ticks);
            for(var i=0;i<8;i++)
            {
                var next=Engine.CommitNextSpacecraftCommand(Commands).Status;
                if(next is SpacecraftCommandStatus.NoCommand or SpacecraftCommandStatus.Pending)break;
                Check(next is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange,"command consumption "+next);
            }
            Check(Engine.CloseSpacecraftCommandBoundary(Commands,out _)==SpacecraftCommandStatus.BoundaryReady,"boundary closed");
            Check(Engine.PrepareSingleEngineActuation(Actuation,target,out EngineLease)==EnginePreparationStatus.Prepared,"genuine engine lease");
            Check(Engine.PrepareFinitePropellant(Resource,EngineLease,target,out FuelLease)==PropellantPreparationStatus.Prepared,"genuine resource lease");
            Check(Engine.PreviewFinitePropellant(Resource,FuelLease,out var preview)==PropellantPreparationStatus.Preview,"copied resource preview");
            return preview;
        }
        internal PoweredFlightResult Apply()
        {
            Check(Engine.PreparePoweredFlight(Power,FuelLease,out PhysicalLease)==PoweredFlightStatus.Prepared,"physical preparation");
            var result=Engine.PublishPoweredFlight(Power,PhysicalLease);
            Check(result.Status==PoweredFlightStatus.Published,"joint publication "+result.Status);return result;
        }
    }
    private static double LogOnePlus(double x)
    {
        // Independent convergent Taylor oracle on x<=1/1024; does not call production dynamics.
        double sum=0,power=x;
        for(var k=1;k<=16;k++){sum+=(k%2==1?power:-power)/k;power*=x;}
        return sum;
    }
    internal static void Physics()
    {
        Arithmetic();
        var straight=new Flight(thrust:9000,exhaust:3000);var preview=straight.Seal();
        var result=straight.Apply();var h=preview.Engine.End.Ticks/1_000_000d;var hp=(1d/128)/3;
        var log=LogOnePlus((1d/128)/8);var expectedV=3000*log;
        var expectedX=3000*(hp-8d/3*log)+expectedV*(h-hp);
        var ev=Math.Abs(result.Observation.Endpoint.VelocityRoot.X-expectedV);
        var ex=Math.Abs(result.Observation.Endpoint.PositionRoot.X-expectedX);
        Check(ev<=PoweredFlightEvaluator.VelocityError&&ex<=PoweredFlightEvaluator.PositionError,"analytical changing-mass endpoint");
        Check(Math.Abs(expectedV-9000*hp/(8+1d/128))>1e-3,"constant-source-mass mechanism fails oracle");
        Check(result.Observation.Resource.RemainingUnits.IsZero&&result.Observation.Endpoint.Properties.MassKilograms==8&&
            result.Observation.Actuator.Activity==ActualEngineActivity.EnabledNoFeed,"joint interior exhaustion");

        var rotating=new Flight(mountY:.25,omegaZ:.6);var rotationPreview=rotating.Seal();var rotation=rotating.Apply().Observation.Endpoint;
        var duration=16666/1_000_000d;const int n=16384;double vx=0,vy=0,x=0,y=0;
        var m0=8+1d/128;var flow=8d/5120;
        // Independent analytic scalar orientation + composite Simpson integration, no quaternion ODE/RK code reuse.
        for(var j=0;j<=n;j++)
        {
            var t=duration*j/n;var angle=.6*t-.5*t*t;var m=m0-flow*t;
            var ax=8*Math.Cos(angle)/m;var ay=8*Math.Sin(angle)/m;var weight=j==0||j==n?1:j%2==0?2:4;
            vx+=weight*ax;vy+=weight*ay;x+=weight*(duration-t)*ax;y+=weight*(duration-t)*ay;
        }
        vx*=duration/(3*n);vy*=duration/(3*n);x*=duration/(3*n);y*=duration/(3*n);
        var angleEnd=.6*duration-.5*duration*duration;
        var q=new DoubleQuaternion(0,0,Math.Sin(angleEnd/2),Math.Cos(angleEnd/2));
        var qError=2*Math.Sqrt(Math.Pow(rotation.BodyToRoot.Z-q.Z,2)+Math.Pow(rotation.BodyToRoot.W-q.W,2));
        Check(Math.Abs(rotation.VelocityRoot.X-vx)<=1e-6&&Math.Abs(rotation.VelocityRoot.Y-vy)<=1e-6&&
            Math.Abs(rotation.PositionRoot.X-x)<=1e-6&&Math.Abs(rotation.PositionRoot.Y-y)<=1e-6&&qError<=1e-9&&
            Math.Abs(rotation.AngularVelocityBody.Z-(.6-duration))<=1e-9,"independent rotating reference");
        Check(vy>1e-5,"frozen root thrust fails rotating witness");
        var admission=new Flight(thrust:8192,exhaust:65536);var admissionPreview=admission.Seal();
        var boundaryPreview=admissionPreview with{Engine=admissionPreview.Engine with{ProposedForceBodyNewtons=new(0,0,8192),
            ProposedMomentBodyNewtonMetres=new(-1.037493482083632,-1.1487563618817642,0),End=new(16667)}};
        Check(PoweredFlightEvaluator.Evaluate(new(default,default,DoubleQuaternion.Identity,new(-.7286325703105209,-.8067725870792514,0)),
            boundaryPreview,out _,out _)==PoweredEvaluationStatus.OutsideModel,"outward angular admission rejects red-team rounding counterexample");

        foreach(var c in new[]{(double.Epsilon,16d,8d),(1d/128,Math.ScaleB(1d,-64),Math.ScaleB(1d,-1064)),
            (1d/128,Math.ScaleB(1d,-1065),8d)})
        {
            var tiny=new Flight(c.Item1,c.Item2,c.Item3);var segmentation=tiny.Seal();var value=tiny.Apply().Observation;
            Check(value.Endpoint.VelocityRoot.X==double.Epsilon,"tiny complete impulse rounds to exactly epsilon");
            Check(!segmentation.PoweredDuration.IsZero&&!segmentation.ConsumedUnits.IsZero,"tiny resource event remains positive");
        }
        // q=1e6/2^24, duration=16666/1e6 => exact fuel=16666/2^24.
        var endpointCase=new Flight(fuel:16666d/(1<<24),thrust:1_000_000d/(1<<21),exhaust:8);
        var endpointPreview=endpointCase.Seal();var endpointValue=endpointCase.Apply().Observation;
        Check(endpointPreview.Classification==PropellantClassification.EndpointExhaustion&&endpointValue.Resource.RemainingUnits.IsZero&&
            endpointValue.Actuator.Activity==ActualEngineActivity.EnabledNoFeed,"exact endpoint exhaustion and actual terminal state");
        var off=new Flight(ignite:false);off.Seal();var noThrust=off.Apply().Observation;
        Check(noThrust.Endpoint.PositionRoot==default&&noThrust.Endpoint.VelocityRoot==default&&noThrust.Resource.ResourceRevision==0&&
            noThrust.Actuator.Activity==ActualEngineActivity.Off&&noThrust.Actuator.ActuatorRevision==1,"off interval advances actual frontier only");
        var empty=new Flight(fuel:0);empty.Seal();var starved=empty.Apply().Observation;
        Check(starved.Actuator.Activity==ActualEngineActivity.EnabledNoFeed&&starved.Resource.ResourceRevision==0,"enabled no-feed");
        Console.WriteLine($"POWERED_PHYSICS PASS analytic_x_error={ex:R} analytic_v_error={ev:R} rotating_x_error={Math.Abs(rotation.PositionRoot.X-x):R} rotating_v_error={Math.Abs(rotation.VelocityRoot.Y-vy):R} rotating_q_error={qError:R} frozen_root_missing_dvy={vy:R} tiny_cases=3");
    }
}
