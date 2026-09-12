using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Timeline;
using static CertifiedContinuationFixture;
using static ContactGenerationFixture;

internal static class ContinuationAllocationTests
{
    private static void Check(bool condition,string message)=>CertifiedContinuationFixture.Check(condition,message);
    private const int ConsumingCount=8,ReusableCount=32;
    internal static void Run()
    {
        var query=Acquire();
        // Same bounded warmup as the retained campaign: eight independent, genuinely consumed states.
        for(var i=0;i<8;i++)
        {
            var f=new CertifiedContinuationFixture(query);var phase=f.Source.Timeline.PublicationPhase;var b=new ContinuationTestPhases(f.Source.Engine);var use=f.Use;
            Check(phase.TryEnter(f.Source.Engine),"warm phase");
            try{Check(b.Prepare(f.Request,use,out var p)==ContinuationPublicationStatus.Published,"warm prepare");Check(b.Recheck(f.Request,use,p)==ContinuationPublicationStatus.Published,"warm recheck");b.Commit(p);}
            finally{phase.Exit();}
            Check(new CertifiedContinuationFixture(query).Publish().Published,"warm ordinary publisher");
        }
        foreach(var name in new[]{"prepare-history","final-recheck","write-only-commit"})
        {
            long sum=0;
            for(var i=0;i<ConsumingCount;i++)
            {
                var f=new CertifiedContinuationFixture(query);var b=new ContinuationTestPhases(f.Source.Engine);var phase=f.Source.Timeline.PublicationPhase;var use=f.Use;
                Check(phase.TryEnter(f.Source.Engine),"measured phase");
                try
                {
                    PreparedContinuationPublication p=default;var status=ContinuationPublicationStatus.Published;
                    if(name!="prepare-history")Check(b.Prepare(f.Request,use,out p)==ContinuationPublicationStatus.Published,"prepare outside measured stage");
                    if(name=="write-only-commit")Check(b.Recheck(f.Request,use,p)==ContinuationPublicationStatus.Published,"recheck outside commit measurement");
                    using var measurement=new OrdinaryAllocationMeasurement(name);
                    if(name=="prepare-history")status=b.Prepare(f.Request,use,out p);
                    else if(name=="final-recheck")status=b.Recheck(f.Request,use,p);
                    else b.Commit(p);
                    var bytes=measurement.Complete();sum+=bytes;OrdinaryAllocationMeasurement.RequireZero(bytes,name);
                    Check(status==ContinuationPublicationStatus.Published,"measured stage result");
                }
                finally{phase.Exit();}
            }
            Console.WriteLine($"PUBLICATION_ALLOCATION path={name} calls={ConsumingCount} bytes={sum}");
        }
        var successes=Enumerable.Range(0,ConsumingCount).Select(_=>new CertifiedContinuationFixture(query)).ToArray();
        var results=new ContinuationPublicationResult[ConsumingCount];
        using(var measurement=new OrdinaryAllocationMeasurement("full-success"))
        {for(var i=0;i<results.Length;i++)results[i]=successes[i].Publish();OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"full-success");}
        Check(results.All(x=>x.Published),"all fresh success fixtures consumed");
        var observation=successes[0];observation.Source.Engine.TryCaptureContinuationObservation(Craft,out _);
        var reads=true;
        using(var measurement=new OrdinaryAllocationMeasurement("copied-observation"))
        {for(var i=0;i<ReusableCount;i++)reads&=observation.Source.Engine.TryCaptureContinuationObservation(Craft,out _);OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"copied-observation");}
        Check(reads,"all copied observations complete");
        var stale=ContinuationAcceptanceTests.StaleFixture(query);
        Refusal(stale,"stale-endpoint",ContinuationPublicationStatus.StaleEndpoint);
        var old=new CertifiedContinuationFixture(query);var oldClearance=old.Request.Clearance;
        old.Source.Timeline.Schedule(default,new(new(91),new(2000000),0,SimulationEventKind.NoOpMarker));old.Source.Timeline.Cancel(new(91));
        var clearance=new CertifiedContinuationFixture(query,existing:old.Source);clearance.Request=clearance.Request with{Clearance=oldClearance};
        Refusal(clearance,"stale-clearance",ContinuationPublicationStatus.StaleClearance);
        var conflict=new CertifiedContinuationFixture(query);conflict.Source.Timeline.Schedule(default,new(new(92),new(2000000),0,SimulationEventKind.NoOpMarker));
        Refusal(conflict,"timeline-conflict",ContinuationPublicationStatus.TimelineConflict);
        Refusal(new(query,debt:999999),"insufficient-debt",ContinuationPublicationStatus.InsufficientDebt);
        Refusal(new(query,capacity:0),"history-capacity",ContinuationPublicationStatus.HistoryCapacityFailure);
        var reentrant=new CertifiedContinuationFixture(query);Check(reentrant.Source.Timeline.PublicationPhase.TryEnter(reentrant.Source.Engine),"reentrancy setup");
        try{Refusal(reentrant,"reentrant",ContinuationPublicationStatus.ReentrantPublication);}finally{reentrant.Source.Timeline.PublicationPhase.Exit();}
        var foreign=new CertifiedContinuationFixture(query);var before=foreign.Snapshot();Exception? error=null;
        var thread=new Thread(()=>{try{Refusal(foreign,"wrong-thread",ContinuationPublicationStatus.WrongOwnerThread,false);}catch(Exception ex){error=ex;}});
        thread.Start();thread.Join();if(error is not null)throw error;Check(foreign.Snapshot()==before,"foreign-thread allocation run nonmutation");
        SetupAndReplay(query);OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine("PUBLICATION_ALLOCATION_MATRIX PASS setup/preparation/success/recheck/commit/observation/stale/conflict/debt/capacity/ownership/replay");
    }
    private static void Refusal(CertifiedContinuationFixture f,string name,ContinuationPublicationStatus expected,bool snapshot=true)
    {
        var before=snapshot?f.Snapshot():null;Check(f.Publish().Status==expected,"refusal warmup "+name);var correct=true;
        using(var measurement=new OrdinaryAllocationMeasurement(name))
        {for(var i=0;i<ReusableCount;i++)correct&=f.Publish().Status==expected;OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),name);}
        Check(correct,"independent refusal result "+name);if(snapshot)Check(f.Snapshot()==before,"refusal allocation nonmutation "+name);
        Console.WriteLine($"PUBLICATION_ALLOCATION path={name} calls={ReusableCount} bytes=0");
    }
    private static void SetupAndReplay(PlanetaryPhysicalSurfacePointQuery query)
    {
        var original=new CertifiedContinuationFixture(query);Check(original.Publish().Published,"replay source record");original.Source.Engine.TryGetProcessedContinuation(0,out var record);
        // Legitimate construction is reported, not constrained to zero. One reconstruction per
        // 1 MiB region keeps the qualified reservation independent of aggregate sample count.
        for(var i=0;i<2;i++)_ = ContinuationAcceptanceTests.Reconstruct(query,record,[1500000]);
        long setupBytes=0,replayBytes=0,comparisonBytes=0;
        for(var i=0;i<ConsumingCount;i++)
        {
            var state=ContactGenerationFixture.State(record.BeforeTranslation.PositionRoot,record.BeforeTranslation.VelocityRoot,
                record.BeforeRotation.OrientationLocalToParent,record.BeforeRotation.AngularVelocityBody);
            var clock=new SimulationClock(default,new(8));SimulationTransactionEngine engine;
            using(var measurement=new OrdinaryAllocationMeasurement("engine-history-setup"))
            {engine=new(clock,state,8,continuationHistoryCapacity:1);setupBytes+=measurement.Complete();}
            Check(engine.ContinuationHistoryCapacity==1,"reserved history slot");
            CertifiedContinuationFixture replay;
            using(var measurement=new OrdinaryAllocationMeasurement("replay-source-proof-reconstruction"))
            {replay=ContinuationAcceptanceTests.Reconstruct(query,record,[1500000]);replayBytes+=measurement.Complete();}
            bool matches;
            using(var measurement=new OrdinaryAllocationMeasurement("replay-record-comparison"))
            {matches=ContinuationAcceptanceTests.MatchesBeforePublication(replay,record);comparisonBytes+=measurement.Complete();}
            Check(matches,"replay compare before measured installation");
            ContinuationPublicationResult result;
            using(var measurement=new OrdinaryAllocationMeasurement("replay-publication-installation"))
            {result=replay.Publish();OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"replay installation");}
            Check(result.Published,"fresh replay consumed");
        }
        Console.WriteLine($"PUBLICATION_SETUP_ALLOCATION calls={ConsumingCount} engineHistoryBytes={setupBytes} replaySourceProofBytes={replayBytes} replayComparisonBytes={comparisonBytes} installationBytes=0");
    }
}
