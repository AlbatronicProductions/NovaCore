using System.Reflection;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static CertifiedContinuationFixture;
using static ContactGenerationFixture;
using Case=FloridaContactProductionTests.Case;

// Test-only access to actual private preparation for pre-install replay comparison and phase allocation.
internal sealed class ContinuationTestPhases(SimulationTransactionEngine engine)
{
    internal delegate ContinuationPublicationStatus PrepareCall(in CertifiedContinuationRequest r,in FloridaContactUse u,out PreparedContinuationPublication p);
    internal delegate ContinuationPublicationStatus CheckCall(in CertifiedContinuationRequest r,in FloridaContactUse u,in PreparedContinuationPublication p);
    internal delegate void CommitCall(in PreparedContinuationPublication p);
    internal readonly PrepareCall Prepare=Bind<PrepareCall>("PrepareCertifiedContinuation",engine);
    internal readonly CheckCall Recheck=Bind<CheckCall>("RecheckCertifiedContinuation",engine);
    internal readonly CommitCall Commit=Bind<CommitCall>("CommitCertifiedContinuation",engine);
    private static T Bind<T>(string name,object engine) where T:Delegate=>
        typeof(SimulationTransactionEngine).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.CreateDelegate<T>(engine);
}

internal static class ContinuationAcceptanceTests
{
    private static void Check(bool condition,string message)=>CertifiedContinuationFixture.Check(condition,message);
    internal static void Run()
    {
        var query=Acquire();Replay(query);StaleEndpoint(query);DirectMutations(query);Ownership(query);Capacity(query);SnapshotCoverage();
        Console.WriteLine("PUBLICATION_ACCEPTANCE_GAPS PASS history-driven replay, stale endpoint, complete payload snapshot, direct mutations, ownership, occupied capacity");
    }
    internal static CertifiedContinuationFixture Reconstruct(PlanetaryPhysicalSurfacePointQuery query,
        in ProcessedCertifiedContinuation record,ReadOnlySpan<long> deliveries,int observations=0)
    {
        var p=record.Provenance;var graphBuilder=new ReferenceFrameGraphBuilder();graphBuilder.Add(p.RootFrame);graphBuilder.Add(p.BodyFrame);
        var graph=graphBuilder.Build();
        Check(SpacecraftStateStore.TryCreateTranslating([p.Spacecraft],[record.BeforeRotation],[record.Properties],[record.BeforeTranslation],graph,out var store,out _),"durable source reconstruction");
        var state=new SimulationState(spacecraft:store,initialRevision:record.BeforeRevision);
        var timeline=new SimulationTimeline(8,1,record.TimelineRevision);
        var clock=new SimulationClock(record.BeforeClock.Time,timeline,record.BeforeClock.Rate);
        var engine=new SimulationTransactionEngine(clock,state,8,continuationHistoryCapacity:1);
        Check(SpacecraftContactGeometry.TryCreate(record.Spacecraft,p.Geometry.DefinitionId,p.Geometry.Version,[p.Feature],out var geometry),"durable singleton geometry");
        foreach(var delta in deliveries)
        {
            clock.AdvanceByHostDuration(new(delta));
            for(var i=0;i<observations;i++)Check(engine.TryCaptureContinuationObservation(record.Spacecraft,out _),"derived render observation");
        }
        if(record.BeforeClock.Paused)clock.Pause();
        Check(engine.CaptureContinuationClock()==record.BeforeClock,"different delivery histories reach exact admission state");
        var c=new Case(query,state,clock,engine,graph,geometry!,p.SourceStart,p.SourceEnd);
        var f=new CertifiedContinuationFixture(query,existing:c,qualification:p.Qualification,propagation:p.Propagation);
        Check(Bits(f.Staged.Initial.FrozenSourceTranslation)==Bits(record.BeforeTranslation)&&Bits(f.Staged.Initial.FrozenSourceRotation)==Bits(record.BeforeRotation),"reconstructed authoritative source bits");
        return f;
    }
    internal static bool MatchesBeforePublication(CertifiedContinuationFixture f,in ProcessedCertifiedContinuation record)
    {
        // The comparison consumes fresh proofs; stored final bits are expectations, never a capability.
        var bindings=new ContinuationTestPhases(f.Source.Engine);var phase=f.Source.Timeline.PublicationPhase;
        Check(phase.TryEnter(f.Source.Engine),"replay comparison phase");
        try
        {
            var use=f.Use;
            if(bindings.Prepare(f.Request,use,out var prepared)!=ContinuationPublicationStatus.Published)return false;
            return Bits(prepared.Record)==Bits(record);
        }
        finally{phase.Exit();}
    }
    private static void Replay(PlanetaryPhysicalSurfacePointQuery query)
    {
        var original=new CertifiedContinuationFixture(query);var published=original.Publish();Check(published.Published,"record original publication");
        Check(original.Source.Engine.TryGetProcessedContinuation(0,out var record),"durable coupled record");
        var encoded=Bits(record);
        record=JsonSerializer.Deserialize<ProcessedCertifiedContinuation>(encoded,Json);
        Check(Bits(record)==encoded,"record-only serialization roundtrip retains exact source/final/provenance bits: "+Differences(encoded,Bits(record)));
        foreach(var delivery in new[]{new long[]{1500000},new long[]{500000,1000000},new long[]{1,499999,250000,750000}})
        foreach(var cadence in new[]{0,30,144})
        {
            var f=Reconstruct(query,record,delivery,cadence);var before=f.Snapshot();
            Check(MatchesBeforePublication(f,record),"reconstructed record matches BEFORE installation");
            Check(f.Snapshot()==before,"replay comparison performs no mutation");
            var result=f.Publish();Check(result.Published,"same specialized replay publisher");
            f.Source.Engine.TryGetProcessedContinuation(0,out var replayed);
            Check(Bits(replayed)==encoded,"replayed coupled history exact");
            Check(Bits(result.Observation)==Bits(published.Observation),"replay copied authority exact");
            Check(f.Source.Engine.TryCaptureContinuationObservation(Craft,out var observed)&&Bits(observed)==Bits(published.Observation),"live replay authority exact");
        }
        var altered=new[]{record with{AfterTranslation=record.AfterTranslation with{PositionRoot=record.AfterTranslation.PositionRoot+Double3.UnitX}},
            record with{AfterTranslation=record.AfterTranslation with{VelocityRoot=record.AfterTranslation.VelocityRoot+Double3.UnitY}},
            record with{AfterRotation=record.AfterRotation with{OrientationLocalToParent=DoubleQuaternion.Identity}},
            record with{AfterRotation=record.AfterRotation with{AngularVelocityBody=Double3.UnitZ}},
            record with{Provenance=record.Provenance with{ClearanceVersion=0}},record with{Provenance=record.Provenance with{Terrain=record.Provenance.Terrain with{PhysicalGeneration=999}}},
            record with{AfterClock=record.AfterClock with{Debt=new(3)}}};
        foreach(var incompatible in altered)
        {var f=Reconstruct(query,record,[1500000]);var before=f.Snapshot();Check(!MatchesBeforePublication(f,incompatible),"incompatible stored record refused before publisher");Check(f.Snapshot()==before,"bad record cannot mutate authority");}
        Console.WriteLine("PUBLICATION_REPLAY variants=9 incompatibleRecords=7 source=serialized-history preinstall-bit-provenance-comparison=PASS authority=exact");
    }
    internal static CertifiedContinuationFixture StaleFixture(PlanetaryPhysicalSurfacePointQuery query)
    {
        var f=new CertifiedContinuationFixture(query);
        f.Source.Timeline.Schedule(default,new(new(99),new(2000000),0,SimulationEventKind.NoOpMarker));f.Source.Timeline.Cancel(new(99));
        f.RefreshExpectations();return f;
    }
    private static void StaleEndpoint(PlanetaryPhysicalSurfacePointQuery query)
    {
        var f=StaleFixture(query);var r=f.Request;
        Check(r.Endpoint.Read(r.Root,r.Initial,r.Realization,r.Pose,r.Target,r.Propagation,f.Use,out _)==PrivatePropagationStatus.Stale,"coherent old endpoint is stale, not mismatched");
        CertifiedContinuationPublicationTests.Refuse(f,"independent coherent endpoint staleness",ContinuationPublicationStatus.StaleEndpoint);
        var normal=new CertifiedContinuationFixture(query);r=normal.Request;Check(normal.Publish().Published,"old receipt invalidation source");
        Check(r.Endpoint.Read(r.Root,r.Initial,r.Realization,r.Pose,r.Target,r.Propagation,normal.Use,out _)==PrivatePropagationStatus.Stale,"consumed endpoint naturally stale");
        Check(r.Clearance.Read(r.Endpoint,normal.Use,out _)==CoverageStatus.Stale,"consumed clearance naturally stale");
        var advanced=new CertifiedContinuationFixture(query);advanced.Source.Clock.AdvanceTo(new(1));advanced.RefreshExpectations();
        CertifiedContinuationPublicationTests.Refuse(advanced,"N greater than SourceStart");
    }
    private static void DirectMutations(PlanetaryPhysicalSurfacePointQuery query)
    {
        foreach(var kind in new[]{"attitude","torque","force","translation","rotation"})
        {
            var f=new CertifiedContinuationFixture(query);var state=f.Source.State;var view=state.CreateView();var timeline=f.Source.Timeline.Revision;
            view.Spacecraft.TryGetTranslation(Craft,out var l,out _);view.Spacecraft.TryGetRigidBody(Craft,out var r);view.Spacecraft.TryGetAttitude(Craft,out var a);
            switch(kind)
            {
                case "attitude":Check(state.CommitSpacecraftAttitudeReplacement(Craft,a,a with{OrientationLocalToParent=DoubleQuaternion.Identity},out _),"direct attitude commit");break;
                case "torque":Check(state.CommitSpacecraftRigidBodyReplacement(Craft,r,r with{ConstantBodyTorque=Double3.UnitX},out _),"direct torque commit");break;
                case "force":state.CommitSpacecraftTranslation(l,l with{ConstantForceRoot=Double3.UnitY});break;
                case "translation":state.CommitSpacecraftTranslation(l,l with{PositionRoot=l.PositionRoot+Double3.UnitZ});break;
                case "rotation":Check(state.CommitSpacecraftRigidBodyReplacement(Craft,r,r with{AngularVelocityBody=Double3.UnitX},out _),"direct rotation commit");break;
            }
            Check(f.Source.Timeline.Revision==timeline,"direct mutation has no timeline change");f.RefreshExpectations();
            CertifiedContinuationPublicationTests.Refuse(f,"direct "+kind,ContinuationPublicationStatus.StaleEndpoint);
        }
        var source=new CertifiedContinuationFixture(query);var result=source.Publish();source.Source.Engine.TryGetProcessedContinuation(0,out var record);
        var different=Reconstruct(query,record with{Properties=new(9)},[1500000]);
        // Immutable mass has no supported in-place setter; a different authority is not the old owner.
        CertifiedContinuationPublicationTests.Refuse(different,"different physical-property authority",null,source.Use);
        Check(result.Published,"physical property control source");
        Console.WriteLine("PUBLICATION_MUTATION immutable-properties=no-in-place-API; different-mass authority refused; fixed slots require genuine upstream paired admission");
    }
    private static void Ownership(PlanetaryPhysicalSurfacePointQuery query)
    {
        var f=new CertifiedContinuationFixture(query);var before=f.Snapshot();var phase=f.Source.Timeline.PublicationPhase;
        // An early checked read is not a reservation. A later ordinary change must still refuse.
        var r=f.Request;Check(r.Endpoint.Read(r.Root,r.Initial,r.Realization,r.Pose,r.Target,r.Propagation,f.Use,out _)==PrivatePropagationStatus.Ready,"early read");
        Check(phase.TryEnter(f.Source.Engine),"held owner phase");
        try
        {
            foreach(var mutation in new Action[]{()=>f.Source.Clock.AdvanceByHostDuration(new(1)),()=>f.Source.Timeline.Schedule(default,new(new(3),default,0,SimulationEventKind.Marker)),
                ()=>f.Source.State.CommitMarkerValue(4),()=>f.Source.Engine.ExecuteCanonicalPendingEvent()})
            {var refused=false;try{mutation();}catch(InvalidOperationException){refused=true;}Check(refused,"no ordinary mutation during phase");Check(f.Snapshot()==before,"guard refusal complete snapshot");}
            Check(f.Publish().Status==ContinuationPublicationStatus.ReentrantPublication,"reentrant publication");Check(f.Snapshot()==before,"reentrant nonmutation");
        }
        finally{phase.Exit();}
        ContinuationPublicationResult foreign=default;Exception? error=null;
        var thread=new Thread(()=>{try{foreign=f.Publish();}catch(Exception e){error=e;}});thread.Start();thread.Join();
        Check(error is null&&foreign.Status==ContinuationPublicationStatus.WrongOwnerThread,"wrong-thread publisher refusal");Check(f.Snapshot()==before,"wrong-thread nonmutation");
        Check(!phase.IsActive&&f.Publish().Published,"ownership restored and genuine publication succeeds");
        var late=new CertifiedContinuationFixture(query);r=late.Request;Check(r.Endpoint.Read(r.Root,r.Initial,r.Realization,r.Pose,r.Target,r.Propagation,late.Use,out _)==PrivatePropagationStatus.Ready,"early applicability");
        late.Source.Clock.AdvanceByHostDuration(new(1));CertifiedContinuationPublicationTests.Refuse(late,"change after early read",ContinuationPublicationStatus.ClockConflict);
    }
    private static void Capacity(PlanetaryPhysicalSurfacePointQuery query)
    {
        var a=new Case(query,.5,-1,attitude:new(0,0,.6,.8));var b=new Case(query,.5,-1,start:1,attitude:new(0,0,.6,.8));
        var av=a.Engine.State.Spacecraft;var bv=b.Engine.State.Spacecraft;
        av.TryGetTranslation(Craft,out var al,out var ap);av.TryGetRigidBody(Craft,out var ar);av.TryGetDefinition(Craft,out var ad);
        bv.TryGetTranslation(Craft,out var bl,out var bp);bv.TryGetRigidBody(Craft,out var br);bv.TryGetDefinition(Craft,out var bd);
        var other=new SpacecraftId(81);bl=bl with{Spacecraft=other};br=br with{Spacecraft=other};bd=bd with{Id=other,BodyFrame=new(82)};
        var builder=new ReferenceFrameGraphBuilder();builder.Add(new ReferenceFrameNode(new(1),null,ReferenceFrameKind.Ecl,"root"));builder.Add(new ReferenceFrameNode(new(72),new(1),ReferenceFrameKind.Ccf,"A"));builder.Add(new ReferenceFrameNode(new(82),new(1),ReferenceFrameKind.Ccf,"B"));var graph=builder.Build();
        Check(SpacecraftStateStore.TryCreateTranslating([ad,bd],[ar,br],[ap,bp],[al,bl],graph,out var store,out _),"fixed two-craft authority");
        var state=new SimulationState(spacecraft:store);var timeline=new SimulationTimeline(8);var clock=new SimulationClock(default,timeline);clock.AdvanceByHostDuration(new(2500000));
        var engine=new SimulationTransactionEngine(clock,state,8,continuationHistoryCapacity:1);
        var ca=new Case(query,state,clock,engine,graph,a.Geometry,a.Start,a.End);var first=new CertifiedContinuationFixture(query,existing:ca);
        Check(first.Publish().Published,"fill single reserved record");engine.TryGetProcessedContinuation(0,out var retained);
        Check(SpacecraftContactGeometry.TryCreate(other,501,1,[b.Geometry.GetFeature(0)],out var geometry),"B singleton geometry");
        var cb=new Case(query,state,clock,engine,graph,geometry!,b.Start,b.End);var second=new CertifiedContinuationFixture(query,existing:cb);
        CertifiedContinuationPublicationTests.Refuse(second,"occupied fixed history",ContinuationPublicationStatus.HistoryCapacityFailure);
        engine.TryGetProcessedContinuation(0,out var after);Check(Bits(retained)==Bits(after),"occupied history record preserved exactly");
        var huge=new CertifiedContinuationFixture(query,debt:long.MaxValue);var result=huge.Publish();Check(result.Published&&result.Observation.Clock.Debt.Ticks==long.MaxValue-1000000,"large exact debt subtraction");
        var empty=new CertifiedContinuationFixture(query,debt:0);CertifiedContinuationPublicationTests.Refuse(empty,"zero debt",ContinuationPublicationStatus.InsufficientDebt);
    }
    private static void SnapshotCoverage()
    {
        SimulationEventRequest.TryCreateSpacecraftForce(new(1),0,new(Craft,default,Double3.UnitX),out var x);
        SimulationEventRequest.TryCreateSpacecraftForce(new(1),0,new(Craft,default,Double3.UnitY),out var y);
        var t1=new SimulationTimeline(1);var t2=new SimulationTimeline(1);t1.Schedule(default,x);t2.Schedule(default,y);
        Check(Bits(TimelineSnapshot(t1))!=Bits(TimelineSnapshot(t2)),"force payload difference detected despite same header");
        t1.Cancel(new(1));t2.Cancel(new(1));Check(Bits(TimelineSnapshot(t1))!=Bits(TimelineSnapshot(t2)),"cancelled payload content captured");
    }
    private static string Differences(string expected,string actual)
    {
        using var e=JsonDocument.Parse(expected);using var a=JsonDocument.Parse(actual);var result=new List<string>();
        void Visit(JsonElement x,JsonElement y,string path)
        {
            if(x.ValueKind==JsonValueKind.Object&&y.ValueKind==JsonValueKind.Object)
            {foreach(var p in x.EnumerateObject())Visit(p.Value,y.GetProperty(p.Name),path+"."+p.Name);}
            else if(x.GetRawText()!=y.GetRawText())result.Add(path+": "+x.GetRawText()+" != "+y.GetRawText());
        }
        Visit(e.RootElement,a.RootElement,"record");return string.Join("; ",result.Take(12));
    }
}
