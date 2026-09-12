using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;
using static CertifiedContinuationFixture;
using static ContactGenerationFixture;
using Case=FloridaContactProductionTests.Case;

internal static class CertifiedContinuationPublicationTests
{
    private static void Check(bool condition,string contract)=>CertifiedContinuationFixture.Check(condition,contract);
    internal static void Run()
    {
        var query=Acquire();
        var success=new CertifiedContinuationFixture(query);
        var borrow=success.Source.Engine.State;var beforeClock=success.Request.Clock;
        var result=success.Publish();Check(result.Published,"genuine Coast publication: "+result.Status);
        SameEndpoint(success.Staged,result.Observation);
        Check(result.Observation.Translation.Epoch==success.Request.Target,"translation epoch T");
        Check(result.Observation.Rotation.Epoch==success.Request.Target,"rotation epoch T");
        Check(result.Observation.Clock==beforeClock with{Time=success.Request.Target,Debt=new(500000)},"clock/debt exact paired advancement");
        Check(result.Observation.StateRevision.Value==1,"one state revision");
        Check(result.Observation.TimelineRevision==success.Request.TimelineRevision,"timeline unchanged");
        Check(success.Source.Engine.ProcessedContinuationCount==1,"one coupled record");
        Check(success.Source.Engine.TryGetProcessedContinuation(0,out var history),"history readable");
        Check(history.AfterTranslation==result.Observation.Translation&&history.AfterRotation==result.Observation.Rotation,"history exact after state");
        Check(history.BeforeClock==beforeClock&&history.AfterClock==result.Observation.Clock,"history clock pair");
        Check(history.Provenance.Qualification.Kinematics==Kinematics,"replay qualification provenance");
        Check(!borrow.Spacecraft.TryGetTranslation(Craft,out _,out _),"borrowed view expires");
        Check(success.Source.Engine.TryCaptureContinuationObservation(Craft,out var observation)&&Bits(observation)==Bits(result.Observation),"coherent copied observation");
        Refuse(success,"repeat consumed proof");

        // Replay reconstructs all receipts from source inputs; retained history cannot authorize a write.
        string? replayBits=null;
        foreach(var cadence in new[]{0,30,144})
        {
            var replay=new CertifiedContinuationFixture(query);
            for(var i=0;i<cadence;i++)Check(replay.Source.Engine.TryCaptureContinuationObservation(Craft,out _),"presentation-only observation");
            var published=replay.Publish();Check(published.Published,"fresh replay publication");
            replay.Source.Engine.TryGetProcessedContinuation(0,out var replayHistory);
            var bits=Bits(replayHistory);replayBits??=bits;Check(bits==replayBits&&bits==Bits(history),"fresh reconstruction/cadence exact replay history");
        }
        var retained=Bits(observation);
        success.Source.Timeline.Schedule(success.Request.Target,new(new(80),success.Request.Target,0,SimulationEventKind.Marker));
        Check(success.Source.Engine.ExecuteCanonicalPendingEvent().Committed,"later ordinary commit");
        Check(Bits(observation)==retained,"retained observation never tracks live state");

        foreach(var mutate in new Action<CertifiedContinuationFixture>[]
        {
            f=>f.Request=default,
            f=>f.Request=f.Request with{Root=default},
            f=>f.Request=f.Request with{Endpoint=default},
            f=>f.Request=f.Request with{Clearance=default},
            f=>f.Request=f.Request with{Initial=default},
            f=>f.Request=f.Request with{Realization=default},
            f=>f.Request=f.Request with{Pose=new(.25)},
            f=>f.Request=f.Request with{Propagation=new(.001,2e-6,2e-6,4e-6)},
            f=>f.Request=f.Request with{Target=new(999999)},
            f=>f.Request=f.Request with{Target=new(1000001)},
            f=>f.Request=f.Request with{Target=default},
            f=>f.Request=f.Request with{StateRevision=new(9)},
            f=>f.Request=f.Request with{TimelineRevision=new(9)},
            f=>f.Source.Clock.AdvanceByHostDuration(new(1)),
            f=>f.Source.Clock.Pause(),
            f=>f.Source.Clock.TrySetRate(SimulationRate.Two),
        })
        {var f=new CertifiedContinuationFixture(query);mutate(f);Refuse(f,"changed/default input");}
        Refuse(new(query,force:true),"Force unresolved clearance",ContinuationPublicationStatus.InvalidClearanceEvidence);
        Refuse(new(query,capacity:0),"no history capacity",ContinuationPublicationStatus.HistoryCapacityFailure);
        Refuse(new(query,revision:new(ulong.MaxValue)),"state revision overflow",ContinuationPublicationStatus.StateRevisionOverflow);
        Refuse(new(query,debt:999999),"insufficient debt",ContinuationPublicationStatus.InsufficientDebt);
        Refuse(new(query,sourceGap:true),"N before SourceStart",ContinuationPublicationStatus.SourceTimeMismatch);

        var first=new CertifiedContinuationFixture(query);var foreign=new CertifiedContinuationFixture(query);
        Refuse(first,"foreign engine",ContinuationPublicationStatus.ForeignEngine,foreign.Use);
        foreach(var request in new[]{first.Request with{Root=foreign.Request.Root},first.Request with{Endpoint=foreign.Request.Endpoint},
            first.Request with{Clearance=foreign.Request.Clearance},first.Request with{Initial=foreign.Request.Initial},
            first.Request with{Realization=foreign.Request.Realization}})
        {first.Request=request;Refuse(first,"foreign receipt/source");}
        var geometryCase=new CertifiedContinuationFixture(query);
        Check(SpacecraftContactGeometry.TryCreate(Craft,501,1,[new(1,new(1,-2,3.01),ContactFeatureRole.LandingTip)],out var geometry),"same-id changed geometry");
        Refuse(geometryCase,"geometry content changed",null,geometryCase.Use with{Geometry=geometry!});
        Refuse(geometryCase,"foreign model",null,geometryCase.Use with{System=NovaCore.Simulation.Celestial.SolAnalyticalDefinition.CreateForTest()});
        Refuse(geometryCase,"foreign frame",null,geometryCase.Use with{Graph=Graph(19)});
        Refuse(geometryCase,"terrain identity copy",null,geometryCase.Use with{Terrain=new QueryCopy(query)});

        // Chain mismatch and stale clearance are separate; coherent endpoint staleness is tested by ContinuationAcceptanceTests.
        var old=new CertifiedContinuationFixture(query);var oldRequest=old.Request;
        old.Source.Timeline.Schedule(default,new(new(99),new(2000000),0,SimulationEventKind.NoOpMarker));
        old.Source.Timeline.Cancel(new(99));
        var fresh=new CertifiedContinuationFixture(query,existing:old.Source);
        var valid=fresh.Request;
        fresh.Request=valid with{Endpoint=oldRequest.Endpoint};Refuse(fresh,"endpoint/root chain mismatch");
        fresh.Request=valid with{Clearance=oldRequest.Clearance};Refuse(fresh,"stale clearance");

        Events(query);PreservedClockAndHistories(query);Allocations(query);
        Console.WriteLine("PUBLICATION_CONTRACT PASS Coast exact paired bits/time/debt/history; Force refused; default/foreign/stale/boundary/capacity/repeatability/observation/allocation");
    }

    internal static void Refuse(CertifiedContinuationFixture f,string contract,ContinuationPublicationStatus? expected=null,FloridaContactUse? use=null)
    {
        var before=f.Snapshot();var result=f.Source.Engine.PublishCertifiedContinuation(f.Request,use??f.Use);
        Check(!result.Published,contract+": must refuse");
        if(expected.HasValue)Check(result.Status==expected,contract+": "+result.Status);
        Check(f.Snapshot()==before,contract+": complete authority/history/pending state unchanged");
        Console.WriteLine($"PUBLICATION_REFUSAL contract={contract} status={result.Status} unchanged=PASS");
    }
    private static void Events(PlanetaryPhysicalSurfacePointQuery query)
    {
        foreach(var kind in Enum.GetValues<SimulationEventKind>())
        foreach(var tick in new[]{0L,500000L,1000000L})
        {
            var c=new Case(query,.5,-1,attitude:new(0,0,.6,.8),publicationCapacity:1,contactCapacity:1);
            c.Clock.AdvanceByHostDuration(new(1500000));var f=new CertifiedContinuationFixture(query,existing:c);
            Schedule(c,kind,new(tick));f.RefreshExpectations();
            Refuse(f,$"pending {kind} at {tick}",ContinuationPublicationStatus.PendingEvent);
        }
        var changed=new CertifiedContinuationFixture(query);
        Check(SimulationEventRequest.TryCreateSpacecraftForce(new(7),0,new(Craft,default,new Double3(1,0,0)),out var force),"force command");
        changed.Source.Timeline.Schedule(default,force);Check(changed.Source.Engine.ExecuteCanonicalPendingEvent().Committed,"force source changed");
        changed.RefreshExpectations();Refuse(changed,"changed source state");
    }
    private static void Schedule(Case c,SimulationEventKind kind,SimulationInstant time)
    {
        var request=new SimulationEventRequest(new(7),time,0,kind);
        if(kind==SimulationEventKind.CelestialImpulse)Check(SimulationEventRequest.TryCreateCelestialImpulse(new(7),time,0,new(6),Double3.UnitX,out request),"celestial event");
        if(kind==SimulationEventKind.RigidBodyTorque)Check(SimulationEventRequest.TryCreateRigidBodyTorque(new(7),time,0,Craft,out request),"torque event");
        if(kind==SimulationEventKind.SpacecraftForce)Check(SimulationEventRequest.TryCreateSpacecraftForce(new(7),0,new(Craft,time,Double3.UnitX),out request),"force event");
        if(kind==SimulationEventKind.SpacecraftContactImpulse)
        {
            var intent=new SpacecraftContactImpulseIntent(Craft,c.Engine.State.Revision,time,new(1),Double3.UnitX,Double3.Zero,
                new(new(Craft,c.Geometry.Identity,1),c.Query.Authority,401));
            Check(c.Timeline.ScheduleContactImpulse(c.Clock.CurrentTime,new(7),0,intent).Succeeded,"contact event");
        }
        else Check(c.Timeline.Schedule(c.Clock.CurrentTime,request).Succeeded,"scheduled "+kind);
    }
    private static void PreservedClockAndHistories(PlanetaryPhysicalSurfacePointQuery query)
    {
        var c=new Case(query,.5,-1,attitude:new(0,0,.6,.8),publicationCapacity:1,contactCapacity:1);
        c.Timeline.Schedule(default,new(new(1),default,0,SimulationEventKind.Marker));Check(c.Engine.ExecuteCanonicalPendingEvent().Committed,"prior history");
        Schedule(c,SimulationEventKind.SpacecraftContactImpulse,new(1000001));
        c.Clock.TrySetRate(new(3,2));c.Clock.AdvanceByHostDuration(new(1000001));c.Clock.Pause();
        var f=new CertifiedContinuationFixture(query,existing:c);var before=c.Engine.CaptureContinuationClock();
        var pending=new ScheduledSimulationEvent[1];c.Timeline.CopyPending(pending);
        c.Timeline.TryResolveContactImpulse(pending[0],out var intent);
        Check(c.Engine.TryGetProcessed(0,out var priorRecord),"prior ordinary history record");
        var timelineBefore=Bits(TimelineSnapshot(c.Timeline));
        var count=c.Engine.ProcessedCount;var result=f.Publish();Check(result.Published,"publication with prior history and future typed payload");
        Check(result.Observation.Clock==before with{Time=c.End,Debt=new(before.Debt.Ticks-1000000)},"rate/remainder/pause/debt preserved");
        Check(c.Engine.ProcessedCount==count,"unrelated history count unchanged");
        Check(c.Engine.TryGetProcessed(0,out var retainedRecord)&&Bits(priorRecord)==Bits(retainedRecord),"prior ordinary history contents unchanged");
        Check(Bits(TimelineSnapshot(c.Timeline))==timelineBefore,"complete timeline unchanged on success");
        Check(c.Timeline.TryPeekPending(out var after)&&after==pending[0]&&c.Timeline.TryResolveContactImpulse(after,out var afterIntent)&&afterIntent==intent,"future typed payload exact");
        Check(result.Observation.StateRevision.Value==2,"success after nonzero revision");
    }
    private static void Allocations(PlanetaryPhysicalSurfacePointQuery query)
    {
        for(var i=0;i<8;i++)Check(new CertifiedContinuationFixture(query).Publish().Published,"fresh allocation warmup");
        var items=Enumerable.Range(0,8).Select(_=>new CertifiedContinuationFixture(query)).ToArray();
        var outcomes=new ContinuationPublicationResult[8];
        using(var measurement=new OrdinaryAllocationMeasurement("certified-publication-success"))
        {for(var i=0;i<items.Length;i++)outcomes[i]=items[i].Publish();OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"publication success");}
        foreach(var outcome in outcomes)Check(outcome.Published,"consuming allocation success");
        var f=items[0];f.Source.Engine.TryCaptureContinuationObservation(Craft,out _);_ = f.Publish();
        using(var measurement=new OrdinaryAllocationMeasurement("certified-publication-observation-refusal"))
        {for(var i=0;i<32;i++){f.Source.Engine.TryCaptureContinuationObservation(Craft,out _);_=f.Publish();}OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"observation/refusal");}
        OrdinaryAllocationMeasurement.PositiveControl();
    }
    private sealed class QueryCopy(IPhysicalSurfacePointQuery query):IPhysicalSurfacePointQuery
    {public PhysicalSurfaceAuthorityIdentity Authority=>query.Authority;public PhysicalSurfacePointResult Query(ulong body,in Double3 direction)=>query.Query(body,direction);}
}
