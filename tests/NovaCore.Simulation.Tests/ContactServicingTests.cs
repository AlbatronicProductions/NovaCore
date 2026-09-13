using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class ContactServicingTests
{
    private static readonly SpacecraftId Craft = new(901);
    private static readonly ReferenceFrameId Root = new(1), Body = new(902);
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException("Contact servicing: " + name); }
    private sealed class Fixture : IDisposable
    {
        internal readonly SimulationClock Clock;
        internal readonly SimulationState State;
        internal readonly SpacecraftStateStore Store;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly LocalContactConfiguration Configuration;
        internal readonly LocalContactSource Source;
        internal readonly LocalContactWorld World;
        internal LocalContactWorld.Receipt Receipt;
        internal long Sequence;
        internal Fixture(bool tilted = false, bool moving = false, long debt = 0, int capacity = 1200,
            StateRevision revision = default, bool unsafeForce = false, long startTicks = 1234567)
        {
            var origin = moving ? new Double3(1e9, -2e9, 3e9) : Double3.Zero;
            var velocity = moving ? new Double3(11, -7, 3) : Double3.Zero;
            var frame = moving ? DoubleQuaternion.FromAxisAngle(new(1, 2, -1), .7) : DoubleQuaternion.Identity;
            var start = new SimulationInstant(startTicks);
            Check(LocalContactConfiguration.TryCreate(1, Root, origin, velocity, frame, new(2,1,1), 64, out var c) == LocalContactStatus.Success, "config");
            Configuration = c!;
            var linear = new SpacecraftTranslationState(Craft, Root, start, origin + frame.Rotate(new(0,2,0)), velocity,
                frame.Rotate(new Double3(0, unsafeForce ? -1e12 : -9810, 0)));
            var angular = new SpacecraftRigidBodyRotationState(Craft, start,
                frame * (tilted ? DoubleQuaternion.FromAxisAngle(Double3.UnitZ, .25) : DoubleQuaternion.Identity), Double3.Zero,
                c!.BoxInertia(1000), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
            var graph = new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "root"));
            graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "qualification body"));
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft,Root,Body,"Qualification-only box")],
                [angular], [new(1000)], [linear], graph.Build(), out var store, out _), "state");
            Store = store!; State = new SimulationState(spacecraft: store, initialRevision: revision);
            Clock = new(start, new SimulationTimeline(4));
            if (debt > 0) Clock.AdvanceByHostDuration(new(debt));
            if (debt < 0) Set(Clock,"_pendingSimulationDebt",new SimulationDuration(debt));
            Engine = new(Clock, State, 4, persistentContactHistoryCapacity: capacity);
            Check(LocalContactSource.Capture(Engine,Craft,c,start+new SimulationDuration(20_000_000),out var source)==LocalContactStatus.Success,"source");
            Source = source!;
            Check(LocalContactWorld.TryCreate(Engine,Source,c,out var world,out Receipt)==LocalContactStatus.Success,"world");
            World = world!;
            Check(Engine.BeginPersistentContact(World,c,Receipt)==LocalContactStatus.Success,"binding");
        }
        internal ContactHostCreditResult Credit(long ticks)
        {
            var result = Engine.AdmitContactHostTime(World,Configuration,Receipt,new(Sequence+1,new(ticks)));
            if (result.CanonicalCommitted) Sequence++;
            return result;
        }
        internal ContactServiceResult Service()
        {
            var result = Engine.ServiceContactDebt(World,Configuration,Receipt);
            Receipt = result.Receipt; return result;
        }
        public void Dispose() => World.Dispose();
    }

    private readonly record struct Snapshot(SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular,
        SpacecraftPhysicalProperties Properties, StateRevision Revision, ContinuationClockState Clock,
        TimelineRevision Timeline, int Pending, int Events, int Publications, ProcessedPersistentContact Last);
    private static Snapshot Capture(Fixture f)
    {
        f.Engine.State.Spacecraft.TryGetTranslation(Craft,out var linear,out var properties);
        f.Engine.State.Spacecraft.TryGetRigidBody(Craft,out var angular);
        f.Engine.TryGetProcessedPersistentContact(f.Engine.ProcessedPersistentContactCount-1,out var last);
        return new(linear,angular,properties,f.Engine.State.Revision,f.Engine.CaptureContinuationClock(),f.Clock.Timeline.Revision,
            f.Clock.Timeline.PendingCount,f.Engine.ProcessedCount,f.Engine.ProcessedPersistentContactCount,last);
    }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(target,value);

    internal static void Cheap()
    {
        using var f = new Fixture(); var before = Capture(f);
        Check(f.Service().Status==ContactServiceStatus.AwaitingDebt,"zero debt waits");
        Check(Capture(f)==before,"waiting canonical nonmutation");
        Check(f.Credit(16665).Status==ContactHostCreditStatus.Accepted,"first host credit");
        Check(Capture(f) with { Clock=before.Clock } == before,"credit only clock");
        Check(f.Service().Published==0 && f.Receipt.Step==0,"insufficient credit never steps");
        Check(f.Credit(1).CanonicalCommitted,"boundary credit");
        var a=f.Service(); Check(a.Published==1 && a.Observation.Clock.Time.Ticks==f.Source.Motion.Time.Ticks+16666,"first interval");
        Check(f.Credit(16667).CanonicalCommitted,"second credit");
        var b=f.Service(); Check(b.Published==1 && b.Observation.Clock.Time.Ticks==f.Source.Motion.Time.Ticks+33333,"second interval");
        using var reference=new Fixture(debt:33333);
        var r=reference.Service(); Check(r.Published==2 && Capture(f)==Capture(reference),"prefunded and live physical/canonical identity");
        f.Clock.AdvanceByHostDuration(new(1)); var external=Capture(f);
        Check(f.Credit(1).Status==ContactHostCreditStatus.Refused && f.Service().Status==ContactServiceStatus.AuthorityRefused,"external debt remains stale");
        Check(Capture(f)==external,"external mutation refusal nonmutation");
        Sequence(true,true,150);
        Console.WriteLine("PASS contact servicing cheap: exact credit, no unfunded step, unchanged external-debt refusal, original moving epoch/lattice");
    }

    private static bool Bits(double a,double b)=>BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a,Double3 b)=>Bits(a.X,b.X)&&Bits(a.Y,b.Y)&&Bits(a.Z,b.Z);
    private static bool Bits(DoubleQuaternion a,DoubleQuaternion b)=>Bits(a.X,b.X)&&Bits(a.Y,b.Y)&&Bits(a.Z,b.Z)&&Bits(a.W,b.W);
    private static void Sequence(bool tilted,bool moving,int fps)
    {
        using var f=new Fixture(tilted,moving); using var control=new Fixture(tilted,moving,debt:20_000_000);
        var reference=new ProcessedPersistentContact[1200];
        while(control.Engine.ProcessedPersistentContactCount<1200)
            Check(control.Service().Published>0,"reference service");
        for(var i=0;i<1200;i++) Check(control.Engine.TryGetProcessedPersistentContact(i,out reference[i]),"reference record");
        var accepted=0L;var frames=0;var support=0;var geometricPeak=0d;var minHeight=double.MaxValue;var maxHeight=double.MinValue;
        var maxOmega=0d;var maxDepth=0d;var generation=f.World.Generation;var previous=0;
        while(f.Engine.ProcessedPersistentContactCount<1200)
        {
            if(accepted<20_000_000)
            {
                frames++;
                var cumulative=fps==0 ? Math.Min(20_000_000,frames*500_000L) : Math.Min(20_000_000,(long)((Int128)frames*1_000_000/fps));
                Check(f.Credit(cumulative-accepted).Status==ContactHostCreditStatus.Accepted,"partition credit"); accepted=cumulative;
            }
            var result=f.Service();
            Check(result.Status is ContactServiceStatus.AwaitingDebt or ContactServiceStatus.BudgetExhausted or ContactServiceStatus.Completed,"service status");
            Check(result.Published<=4,"bounded catch-up");
            var count=f.Engine.ProcessedPersistentContactCount;
            Check(f.Clock.CurrentTime.Ticks-f.Source.Motion.Time.Ticks+f.Clock.PendingSimulationDebt.Ticks==accepted,"credit conservation");
            for(var i=previous;i<count;i++)
            {
                Check(f.Engine.TryGetProcessedPersistentContact(i,out var record),"record");var expected=reference[i];
                Check(Bits(record.AfterTranslation.PositionRoot,expected.AfterTranslation.PositionRoot)&&
                    Bits(record.AfterTranslation.VelocityRoot,expected.AfterTranslation.VelocityRoot)&&
                    Bits(record.AfterRotation.OrientationLocalToParent,expected.AfterRotation.OrientationLocalToParent)&&
                    Bits(record.AfterRotation.AngularVelocityBody,expected.AfterRotation.AngularVelocityBody),"host partition physical bit identity");
                var target=f.Source.Motion.Time.Ticks+(long)((Int128)(i+1)*1_000_000/60);
                Check(record.AfterClock.Time.Ticks==target && record.AfterRevision.Value==(ulong)i+1 && record.TimelineRevision==default,"time/revision/timeline");
                Check(record.Episode==expected.Episode && record.Frontier==i+1 && record.AfterClock.Debt.Ticks==record.BeforeClock.Debt.Ticks-(i%3==0?16666:16667),"immutable episode/exact interval debt");
                var origin=f.Configuration.OriginRoot+f.Configuration.OriginVelocityRoot*((target-f.Source.Motion.Time.Ticks)/1_000_000d);
                var center=f.Configuration.LocalToRoot.Conjugate().Rotate(record.AfterTranslation.PositionRoot-origin);
                var minimum=double.MaxValue;
                for(var x=-1;x<=1;x+=2)for(var y=-1;y<=1;y+=2)for(var z=-1;z<=1;z+=2)
                    minimum=Math.Min(minimum,(center+f.Configuration.LocalToRoot.Conjugate().Rotate(record.AfterRotation.OrientationLocalToParent.Rotate(new(x,y*.5,z*.5)))).Y);
                geometricPeak=Math.Max(geometricPeak,-minimum);
                maxOmega=Math.Max(maxOmega,Math.Sqrt(record.AfterRotation.AngularVelocityBody.LengthSquared));
                if(i>=600)
                {
                    minHeight=Math.Min(minHeight,center.Y);maxHeight=Math.Max(maxHeight,center.Y);
                    if(Math.Abs(minimum)<=.002 && Math.Abs(center.Y-.5)<=.002 && Math.Sqrt((record.AfterTranslation.VelocityRoot-f.Configuration.OriginVelocityRoot).LengthSquared)<=.06) support++;
                }
            }
            if(result.Published>0)
            {
                Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var endpoint)==LocalContactStatus.Success,"read endpoint");
                maxDepth=Math.Max(maxDepth,endpoint.MaximumDepth);
                if(count>600) Check(endpoint.ContactPoints>=2 && endpoint.ConstraintCount>0,"supported solver continuation");
                Check(result.Observation.Translation==Capture(f).Linear,"copied canonical observation");
            }
            previous=count;
        }
        Check(support==600 && geometricPeak<=.020 && maxDepth<=.020 && maxHeight-minHeight<=.001,"600/600 independent support/penetration/drift");
        Check(!tilted||maxOmega>.1,"angular response");
        Check(f.World.Generation==generation && f.Clock.PendingSimulationDebt.Ticks==0 && f.Receipt.Step==1200,"same world/final debt/frontier");
        var final=Capture(f); Check(f.Credit(1).Status==ContactHostCreditStatus.Completed && f.Service().Status==ContactServiceStatus.Completed && Capture(f)==final,"completion nonmutation");
        Console.WriteLine($"CONTACT_SERVICE_SEQUENCE tilted={tilted} moving={moving} fps={fps} steps=1200 support={support}/600 shorts=400 longs=800 debt=0 trajectory=EXACT geometric_peak_m={geometricPeak:R} drift_m={maxHeight-minHeight:R}");
    }

    private static T[] Values<T>(object target,string field)=>(T[])target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(target)!;

    private static void Refusals()
    {
        void Mutation(string name,Action<Fixture> mutate)
        {
            using var f=new Fixture();mutate(f);var snapshot=Capture(f);var receipt=f.Receipt;
            Check(f.Credit(1).Status==ContactHostCreditStatus.Refused,name+" credit refusal");
            Check(f.Service().Status==ContactServiceStatus.AuthorityRefused,name+" service refusal");
            Check(Capture(f)==snapshot && f.Receipt.Step==receipt.Step,name+" nonmutation");
            Console.WriteLine("CONTACT_SERVICE_REFUSAL "+name+" nonmutation=PASS");
        }
        Mutation("StateRevision/numerically identical mutation",f=>f.State.CommitMarkerValue(0));
        Mutation("TimelineRevision",f=>f.Clock.Timeline.Schedule(f.Clock.CurrentTime,new(new(1),new(40_000_000),0,SimulationEventKind.Marker)));
        Mutation("position",f=>Values<SpacecraftTranslationState>(f.Store,"_translations")[0]=Values<SpacecraftTranslationState>(f.Store,"_translations")[0] with {PositionRoot=new(1,2,3)});
        Mutation("mass",f=>Values<SpacecraftPhysicalProperties>(f.Store,"_properties")[0]=new(1001));
        Mutation("inertia",f=>Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0]=Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with {PrincipalInertia=new(1,2,3)});
        Mutation("force",f=>Values<SpacecraftTranslationState>(f.Store,"_translations")[0]=Values<SpacecraftTranslationState>(f.Store,"_translations")[0] with {ConstantForceRoot=Double3.Zero});
        Mutation("torque",f=>Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0]=Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with {ConstantBodyTorque=Double3.UnitX});
        Mutation("model",f=>Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0]=Values<SpacecraftRigidBodyRotationState>(f.Store,"_rigidBodies")[0] with {Model=(RigidBodyRotationModel)2});
        Mutation("definition",f=>Values<SpacecraftDefinition>(f.Store,"_definitions")[0]=new(Craft,Root,Body,"changed"));
        Mutation("rate",f=>f.Clock.TrySetRate(new(2,1)));
        Mutation("pause",f=>f.Clock.Pause());
        Mutation("clock",f=>f.Clock.AdvanceTo(f.Clock.CurrentTime+new SimulationDuration(1)));
        Mutation("debt",f=>f.Clock.AdvanceByHostDuration(new(1)));
        foreach(var tick in new[]{1L,16666L})
            Mutation("event <= target "+tick,f=>f.Clock.Timeline.Schedule(f.Clock.CurrentTime,new(new(1),f.Clock.CurrentTime+new SimulationDuration(tick),0,SimulationEventKind.Marker)));

        using var a=new Fixture();using var other=new Fixture();var before=Capture(a);var otherBefore=Capture(other);
        void ReceiptRefusal(LocalContactWorld.Receipt receipt,string name)
        {
            Check(a.Engine.AdmitContactHostTime(a.World,a.Configuration,receipt,new(1,new(1))).Status==ContactHostCreditStatus.Refused,name+" host");
            Check(a.Engine.ServiceContactDebt(a.World,a.Configuration,receipt).Status==ContactServiceStatus.AuthorityRefused,name+" service");
            Check(Capture(a)==before,name+" nonmutation");
        }
        ReceiptRefusal(default,"default receipt");ReceiptRefusal(new(a.World,0),"fabricated receipt");ReceiptRefusal(other.Receipt,"foreign world");
        object wrong=a.Receipt;Set(wrong,"<Generation>k__BackingField",a.Receipt.Generation+1);ReceiptRefusal((LocalContactWorld.Receipt)wrong,"generation");
        wrong=a.Receipt;Set(wrong,"Step",1L);ReceiptRefusal((LocalContactWorld.Receipt)wrong,"frontier");
        Check(other.Engine.AdmitContactHostTime(a.World,a.Configuration,a.Receipt,new(1,new(1))).AuthorityStatus==LocalContactStatus.ForeignEngine,"foreign engine");
        Check(a.Engine.AdmitContactHostTime(a.World,other.Configuration,a.Receipt,new(1,new(1))).AuthorityStatus==LocalContactStatus.ConfigurationMismatch,"configuration");
        Check(Capture(a)==before && Capture(other)==otherBefore,"foreign authority nonmutation");
        var threadCredit=default(ContactHostCreditResult);var threadService=default(ContactServiceResult);
        var thread=new Thread(()=>{threadCredit=a.Credit(1);threadService=a.Engine.ServiceContactDebt(a.World,a.Configuration,a.Receipt);});thread.Start();thread.Join();
        Check(threadCredit.Status==ContactHostCreditStatus.WrongOwnerThread && threadService.Status==ContactServiceStatus.WrongOwnerThread,"owner thread");
        Check(a.Clock.PublicationPhase.TryEnter(a.Engine),"phase setup");
        try{Check(a.Credit(1).Status==ContactHostCreditStatus.Reentrant && a.Service().Status==ContactServiceStatus.Reentrant,"reentrant");}
        finally{a.Clock.PublicationPhase.Exit();}
        Check(Capture(a)==before,"thread/reentrant nonmutation");
        Check(a.Credit(-1).Status==ContactHostCreditStatus.InvalidDuration && a.Credit(0).Status==ContactHostCreditStatus.NoWork && Capture(a)==before,"negative/zero nonmutation");
        Check(a.Engine.AdmitContactHostTime(a.World,a.Configuration,a.Receipt,new(2,new(1))).Status==ContactHostCreditStatus.InvalidSequence,"future input sequence");
        Check(a.Credit(16666).CanonicalCommitted,"credit before duplicate");before=Capture(a);
        Check(a.Engine.AdmitContactHostTime(a.World,a.Configuration,a.Receipt,new(1,new(16666))).Status==ContactHostCreditStatus.InvalidSequence && Capture(a)==before,"duplicate credit");
        var initial=a.Receipt;a.Source.TryEndpoint(1,out var target);
        Check(a.World.Step(a.Engine,a.Configuration,a.Receipt,target,out a.Receipt)==LocalContactStatus.Success,"pending setup");
        before=Capture(a);
        Check(a.Credit(1).AuthorityStatus==LocalContactStatus.PublicationPending && a.Service().AuthorityStatus==LocalContactStatus.PublicationPending && Capture(a)==before,"pending endpoint never discarded or credited");
        var refused=a.Engine.PublishPersistentContact(a.World,a.Configuration,new(a.Receipt,default));
        Check(refused.Status==PersistentContactPublicationStatus.ClockConflict && Capture(a)==before,"sound precommit refusal");
        Check(a.Engine.PublishPersistentContact(a.World,a.Configuration,new(a.Receipt,a.Engine.CaptureContinuationClock())).CanonicalCommitted,"sound receipt retries");
        before=Capture(a);ReceiptRefusal(initial,"older receipt");
        Check(a.Engine.PublishPersistentContact(a.World,a.Configuration,new(a.Receipt,a.Engine.CaptureContinuationClock())).Status==PersistentContactPublicationStatus.FrontierConflict && Capture(a)==before,"duplicate consumed publication");

        using(var huge=new Fixture(debt:long.MaxValue))
        {var snapshot=Capture(huge);Check(huge.Credit(1).Status==ContactHostCreditStatus.ArithmeticOverflow && Capture(huge)==snapshot,"credit overflow");}
        using(var negative=new Fixture(debt:-1))
        {var snapshot=Capture(negative);Check(negative.Credit(1).Status==ContactHostCreditStatus.ArithmeticOverflow && negative.Service().Status==ContactServiceStatus.ArithmeticOverflow && Capture(negative)==snapshot,"invalid negative debt");}
        foreach(var overflow in new[]{false,true})
        {
            using var f=new Fixture(capacity:overflow?1200:0,revision:overflow?new(ulong.MaxValue):default);
            Check(f.Credit(16666).CanonicalCommitted,"credit commits independently of service blocker");var snapshot=Capture(f);
            var result=f.Service();Check(result.Status==(overflow?ContactServiceStatus.StateRevisionOverflow:ContactServiceStatus.HistoryCapacity),"pre-step capacity/revision");
            Check(Capture(f)==snapshot && f.Receipt.Step==0,"service refusal preserves previously credited debt and private frontier");
        }
        using(var f=new Fixture())
        {f.World.Dispose();var snapshot=Capture(f);Check(f.Credit(1).AuthorityStatus==LocalContactStatus.Disposed && f.Service().AuthorityStatus==LocalContactStatus.Disposed && Capture(f)==snapshot,"disposed world");}
        using(var f=new Fixture(unsafeForce:true))
        {
            Check(f.Credit(16666).CanonicalCommitted,"unsafe fixture credit");var snapshot=Capture(f);
            Check(f.Service().Status==ContactServiceStatus.StepFailed && Capture(f)==snapshot,"post-step export failure preserves credited canonical state");
            Check(f.Credit(1).AuthorityStatus==LocalContactStatus.Invalidated && f.Service().AuthorityStatus==LocalContactStatus.Invalidated,"unsafe world terminal");
        }
        Console.WriteLine("PASS contact servicing refusal matrix");
    }

    private static void TerminalFailures()
    {
        using var a=new Fixture();var before=Capture(a);
        var credit=a.Engine.AdmitContactHostTimeWithFailedAcknowledgementForTest(a.World,a.Configuration,a.Receipt,new(1,new(16666)));
        var after=Capture(a);
        Check(credit.Status==ContactHostCreditStatus.CanonicalCommittedPrivateInvalidated && credit.CanonicalCommitted &&
            after.Clock.Debt.Ticks==16666 && after with {Clock=before.Clock}==before,"credit committed private invalidated");
        Check(a.Credit(1).AuthorityStatus==LocalContactStatus.Invalidated && a.Service().AuthorityStatus==LocalContactStatus.Invalidated && Capture(a)==after,"credit terminal retry nonmutation");
        using var b=new Fixture();Check(b.Credit(16666).CanonicalCommitted,"terminal physical credit");
        var result=b.Engine.ServiceContactDebtWithFailedAcknowledgementForTest(b.World,b.Configuration,b.Receipt);b.Receipt=result.Receipt;
        after=Capture(b);
        Check(result.Status==ContactServiceStatus.CanonicalCommittedPrivateInvalidated && result.Published==1 && after.Revision.Value==1 && after.Publications==1 &&
            after.Clock.Debt.Ticks==0 && after.Linear==result.Observation.Translation && after.Angular==result.Observation.Rotation,"physical canonical commit survives ack failure");
        Check(b.Service().AuthorityStatus==LocalContactStatus.Invalidated && Capture(b)==after,"physical terminal retry refused");
        Console.WriteLine("CONTACT_SERVICE_TERMINALS credit=COMMITTED/private-invalidated physical=COMMITTED/private-invalidated retries=REFUSED");
    }

    private static bool Operation(Fixture f)
    {
        var step=f.Receipt.Step+1;var ticks=step%3==1?16666:16667;
        var credit=f.Credit(ticks);var service=f.Service();
        return credit.Status==ContactHostCreditStatus.Accepted && service.Published==1 && service.Status==ContactServiceStatus.AwaitingDebt;
    }
    private static void Allocation()
    {
        var before=GC.GetAllocatedBytesForCurrentThread();using var f=new Fixture();
        for(var i=0;i<128;i++)Check(Operation(f),"warm operation");
        var managed=GC.GetAllocatedBytesForCurrentThread()-before;var native=f.World.PoolBytes;
        Check(managed+(long)native<=8*1024*1024,"retained storage upper bound");var success=true;
        using(var measurement=new OrdinaryAllocationMeasurement("contact-servicing-complete"))
        {for(var i=0;i<1024;i++)success&=Operation(f);OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"complete servicing");}
        Check(success && f.Engine.ProcessedPersistentContactCount==1152 && f.World.PoolBytes==native,"measured operations complete/storage stable");
        using(var measurement=new OrdinaryAllocationMeasurement("contact-servicing-no-work"))
        {for(var i=0;i<1024;i++)success&=f.Credit(0).Status==ContactHostCreditStatus.NoWork && f.Service().Status==ContactServiceStatus.AwaitingDebt;OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"no work");}
        using var backlog=new Fixture(debt:20_000_000);
        for(var i=0;i<128;i++)Check(backlog.Service().Published==4,"backlog warmup");
        using(var measurement=new OrdinaryAllocationMeasurement("contact-servicing-backlog"))
        {for(var i=0;i<128;i++)success&=backlog.Service().Published==4;OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"backlog");}
        Check(success,"all allocation workloads");OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"CONTACT_SERVICE_STORAGE managed_setup_upper_bound={managed} native_pool={native} combined_upper_bound={managed+(long)native} history_payload_included={Unsafe.SizeOf<ProcessedPersistentContact>()*(long)f.Engine.PersistentContactHistoryCapacity} limit=8388608");
    }
    internal static void Performance()
    {
        var samples=new double[1024];var order=new double[1024];var cold=Stopwatch.GetTimestamp();using var f=new Fixture();
        var coldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        for(var i=0;i<128;i++)Check(Operation(f),"performance warmup");
        for(var i=0;i<1024;i++)
        {var start=Stopwatch.GetTimestamp();var success=Operation(f);var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;Check(success,"measured complete operation");samples[i]=order[i]=ms;}
        Array.Sort(samples);var median=(samples[511]+samples[512])/2;var p95=samples[972];var p99=samples[1013];var max=samples[^1];
        Console.WriteLine("CONTACT_SERVICE_PERFORMANCE "+JsonSerializer.Serialize(new{median_ms=median,p95_ms=p95,p99_ms=p99,max_ms=max,cold_total_ms=coldMs,warm=128,measured=1024,
            tails=order.Select((ms,index)=>new{index,ms}).OrderByDescending(x=>x.ms).Take(8).ToArray()}));
        Check(median<=.05 && p95<=.10 && p99<=.25 && max<=.50,"complete operation performance ceiling");
    }
    internal static void Run()
    {
        Cheap();Refusals();TerminalFailures();
        foreach(var tilted in new[]{false,true})foreach(var fps in new[]{30,60,150,240,0})Sequence(tilted,false,fps);
        Allocation();Console.WriteLine("PASS host-paced contact servicing");
    }
}
