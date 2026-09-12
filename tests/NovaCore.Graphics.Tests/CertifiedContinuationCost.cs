using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static CertifiedContinuationFixture;
using static ContactGenerationFixture;

// Test-only direct delegates to the actual private phases. They never forge a receipt or prepared state.
// Setup/reflection is outside measured intervals. Ordinary timing and checked allocation are separate.
internal static class CertifiedContinuationCost
{
    private static void Check(bool condition,string contract)=>CertifiedContinuationFixture.Check(condition,contract);
    private delegate ContinuationPublicationStatus Prepare(in CertifiedContinuationRequest r,in FloridaContactUse u,out PreparedContinuationPublication p);
    private delegate ContinuationPublicationStatus Recheck(in CertifiedContinuationRequest r,in FloridaContactUse u,in PreparedContinuationPublication p);
    private delegate void Commit(in PreparedContinuationPublication p);
    private delegate ProcessedCertifiedContinuation History(int i,in SpacecraftTranslationState a,in SpacecraftRigidBodyRotationState b,
        in SpacecraftTranslationState c,in SpacecraftRigidBodyRotationState d,SpacecraftPhysicalProperties properties,
        ContinuationClockState before,ContinuationClockState after,StateRevision rb,StateRevision ra,TimelineRevision timeline,
        in ContinuationPublicationProvenance provenance);
    private sealed class Bound(SimulationTransactionEngine engine)
    {
        internal readonly Prepare Prepare=Method("PrepareCertifiedContinuation").CreateDelegate<Prepare>(engine);
        internal readonly Recheck Recheck=Method("RecheckCertifiedContinuation").CreateDelegate<Recheck>(engine);
        internal readonly Commit Commit=Method("CommitCertifiedContinuation").CreateDelegate<Commit>(engine);
    }
    private static MethodInfo Method(string name)=>typeof(SimulationTransactionEngine).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!;
    private static readonly History BuildHistory=typeof(SimulationTransactionEngine).GetMethod("PrepareContinuationHistory",BindingFlags.Static|BindingFlags.NonPublic)!.CreateDelegate<History>();
    private readonly record struct Sample(long Preparation,long Recheck,long History,long Commit,long Observation);
    private static ProcessedCertifiedContinuation HistoryCopy(in ProcessedCertifiedContinuation r)=>BuildHistory(r.Index,r.BeforeTranslation,r.BeforeRotation,
        r.AfterTranslation,r.AfterRotation,r.Properties,r.BeforeClock,r.AfterClock,r.BeforeRevision,r.AfterRevision,r.TimelineRevision,r.Provenance);

    private static Sample Phases(CertifiedContinuationFixture f,Bound bound)
    {
        var use=f.Use;var phase=f.Source.Timeline.PublicationPhase;Check(phase.TryEnter(f.Source.Engine),"timed phase entry");
        PreparedContinuationPublication prepared;
        long start,a,b,c,d,e,g;ContinuationPublicationStatus p,r;ProcessedCertifiedContinuation record;
        try
        {
            start=Stopwatch.GetTimestamp();p=bound.Prepare(f.Request,use,out prepared);a=Stopwatch.GetTimestamp();
            Check(p==ContinuationPublicationStatus.Published,"timed preparation");
            b=Stopwatch.GetTimestamp();r=bound.Recheck(f.Request,use,prepared);c=Stopwatch.GetTimestamp();
            Check(r==ContinuationPublicationStatus.Published,"timed recheck");
            d=Stopwatch.GetTimestamp();record=HistoryCopy(prepared.Record);e=Stopwatch.GetTimestamp();
            Check(record==prepared.Record,"actual history constructor equivalence");
            var commitStart=Stopwatch.GetTimestamp();bound.Commit(prepared);g=Stopwatch.GetTimestamp()-commitStart;
        }
        finally {phase.Exit();}
        var observationStart=Stopwatch.GetTimestamp();var observed=f.Source.Engine.TryCaptureContinuationObservation(Craft,out var observation);
        var observationTicks=Stopwatch.GetTimestamp()-observationStart;
        Check(observed&&observation==prepared.Observation,"private phases equal published observation");SameEndpoint(f.Staged,observation);
        return new(a-start,c-b,e-d,g,observationTicks);
    }
    internal static void Run()
    {
        const int count=128;var query=Acquire();
        for(var i=0;i<8;i++)
        {var f=new CertifiedContinuationFixture(query);_=Phases(f,new(f.Source.Engine));Check(new CertifiedContinuationFixture(query).Publish().Published,"total warmup");}
        var samples=new Sample[count];var total=new long[count];var timer=new long[count];
        for(var i=0;i<count;i++)
        {
            var f=new CertifiedContinuationFixture(query);var bound=new Bound(f.Source.Engine);samples[i]=Phases(f,bound);
            var full=new CertifiedContinuationFixture(query);var a=Stopwatch.GetTimestamp();var result=full.Publish();total[i]=Stopwatch.GetTimestamp()-a;
            Check(result.Published,"fresh total publication");SameEndpoint(full.Staged,result.Observation);
            a=Stopwatch.GetTimestamp();timer[i]=Stopwatch.GetTimestamp()-a;
        }
        Report("preflight-including-record",samples.Select(x=>x.Preparation));
        Report("final-applicability-recheck",samples.Select(x=>x.Recheck));
        Report("history-record-preparation-subset",samples.Select(x=>x.History));
        Report("write-only-commit",samples.Select(x=>x.Commit));Report("copied-observation",samples.Select(x=>x.Observation));
        Report("total-success",total);Report("empty-timestamp-pair",timer);
        var noCapacity=new CertifiedContinuationFixture(query,capacity:0);
        var insufficient=new CertifiedContinuationFixture(query,debt:999999);
        var stale=new CertifiedContinuationFixture(query);stale.Source.Clock.AdvanceByHostDuration(new(1));
        var force=new CertifiedContinuationFixture(query,force:true);
        foreach(var pair in new[]{("history-capacity-refusal",noCapacity),("insufficient-debt-refusal",insufficient),("clock-conflict-refusal",stale),("Force-clearance-refusal",force)})
        {
            Check(!pair.Item2.Publish().Published,"refusal warmup");var values=new long[count];
            for(var i=0;i<count;i++){var a=Stopwatch.GetTimestamp();var refusal=pair.Item2.Publish();values[i]=Stopwatch.GetTimestamp()-a;Check(!refusal.Published,"timed refusal");}
            Report(pair.Item1,values);
        }
        for(var i=0;i<2;i++)Check(new CertifiedContinuationFixture(query).Publish().Published,"reconstruction warmup");
        var reconstruction=new long[16];
        for(var i=0;i<16;i++){var a=Stopwatch.GetTimestamp();var f=new CertifiedContinuationFixture(query);var result=f.Publish();reconstruction[i]=Stopwatch.GetTimestamp()-a;Check(result.Published,"fresh replay reconstruction");}
        Report("source-proof-replay-reconstruction-and-publication",reconstruction);
        // Consume independent source states once; zero is required for the complete private phase path too.
        var fixtures=Enumerable.Range(0,8).Select(_=>new CertifiedContinuationFixture(query)).ToArray();
        var bindings=fixtures.Select(f=>new Bound(f.Source.Engine)).ToArray();
        var okay=true;
        using(var measurement=new OrdinaryAllocationMeasurement("publication-private-phases"))
        {
            for(var i=0;i<fixtures.Length;i++)
            {
                var f=fixtures[i];var use=f.Use;var phase=f.Source.Timeline.PublicationPhase;okay&=phase.TryEnter(f.Source.Engine);
                try
                {
                    okay&=bindings[i].Prepare(f.Request,use,out var p)==ContinuationPublicationStatus.Published;
                    okay&=bindings[i].Recheck(f.Request,use,p)==ContinuationPublicationStatus.Published;
                    var history=HistoryCopy(p.Record);okay&=history.Index==p.Record.Index;
                    bindings[i].Commit(p);
                }
                finally {phase.Exit();}
            }
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"private publication phases");
        }
        Check(okay,"all allocating-measurement fixtures consumed successfully");
        Console.WriteLine(JsonSerializer.Serialize(new{kind="publication-storage",historyRecordBytes=Unsafe.SizeOf<ProcessedCertifiedContinuation>(),
            observationBytes=Unsafe.SizeOf<ContinuationPublicationObservation>(),preparedBytes=Unsafe.SizeOf<PreparedContinuationPublication>(),
            growthPerPublication=0,preallocatedSlotsPerFixture=1,recordsPerSuccess=1,productionGpuWork=0,samples=count,
            note="history contains immutable string references; array allocated during engine setup; phase timing includes delegate/timestamp overhead; history subset overlaps preflight"}));
    }
    private static void Report(string name,IEnumerable<long> source)
    {
        var ticks=source.Order().ToArray();double Us(long value)=>value*1e6/Stopwatch.Frequency;
        Console.WriteLine(JsonSerializer.Serialize(new{kind="publication-cost",name,count=ticks.Length,unit="us",
            median=Us(ticks[(ticks.Length-1)/2]),p95=Us(ticks[(int)Math.Ceiling(ticks.Length*.95)-1]),
            p99=Us(ticks[(int)Math.Ceiling(ticks.Length*.99)-1]),max=Us(ticks[^1])}));
    }
}
