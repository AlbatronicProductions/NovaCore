using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyContactAdmissionTests
{
    private static bool CompleteOperation(AssemblyApplicationSession s,ref AssemblyFlightObservation observation)
    {
        var credit=s.Engine.AdmitAssemblyHostTime(s.Authority,observation.HostSequence+1,new(s.Launch.Plan[observation.State.Frontier].Request.Ticks));
        var service=s.Engine.ServiceAssemblyContactDebt(s.Authority);
        var copied=s.Engine.ObserveAssemblyFlight(s.Authority,out observation);
        return credit.Status==AssemblyFlightStatus.AcceptedCredit&&service.PublishedCount==1&&
            service.Status==AssemblyFlightStatus.AwaitingDebt&&copied==AssemblyFlightStatus.Ready;
    }

    private static bool Bits(double a,double b)=>BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a,Double3 b)=>Bits(a.X,b.X)&&Bits(a.Y,b.Y)&&Bits(a.Z,b.Z);
    private static bool Bits(DoubleQuaternion a,DoubleQuaternion b)=>Bits(a.X,b.X)&&Bits(a.Y,b.Y)&&Bits(a.Z,b.Z)&&Bits(a.W,b.W);
    private static bool MotionBits(AssemblyMotion a,AssemblyMotion b)=>Bits(a.PositionO,b.PositionO)&&Bits(a.VelocityO,b.VelocityO)&&Bits(a.BodyToWorld,b.BodyToWorld)&&Bits(a.AngularVelocityBody,b.AngularVelocityBody);

    internal static void Schedules()
    {
        // Independent native-only control: no engine preparation/publication or acknowledgement.
        // Test-only access keeps canonical state fixed while the same native solver advances.
        using(var raw=Session())
        using(var published=Session())
        {
            var original=Observe(raw);var rw=raw.Engine.AssemblyContactWorldForTest(raw.Authority)!;
            var pw=published.Engine.AssemblyContactWorldForTest(published.Authority)!;
            var rs=NativeField<BepuPhysics.Simulation>(rw,"simulation");var ps=NativeField<BepuPhysics.Simulation>(pw,"simulation");
            var rb=NativeField<BepuPhysics.BodyHandle>(rw,"body");var pb=NativeField<BepuPhysics.BodyHandle>(pw,"body");
            var metrics=NativeField<NovaCore.Simulation.Spacecraft.Contact.Staging.LocalContactMetrics>(rw,"metrics");
            for(var i=0;i<1200;i++)
            {
                var dt=(float)(raw.Launch.Plan[i].Request.Ticks/1_000_000d);
                metrics.Contacts=0;metrics.MaximumDepth=0;metrics.ArticleChildMask=0;metrics.Coverage?.Begin(dt);
                rs.Timestep(dt);Next(published);
                var a=rs.Bodies[rb];var b=ps.Bodies[pb];
                static bool V(System.Numerics.Vector3 x,System.Numerics.Vector3 y)=>Bits(x.X,y.X)&&Bits(x.Y,y.Y)&&Bits(x.Z,y.Z);
                static bool Q(System.Numerics.Quaternion x,System.Numerics.Quaternion y)=>Bits(x.X,y.X)&&Bits(x.Y,y.Y)&&Bits(x.Z,y.Z)&&Bits(x.W,y.W);
                Check(V(a.Pose.Position,b.Pose.Position)&&Q(a.Pose.Orientation,b.Pose.Orientation)&&V(a.Velocity.Linear,b.Velocity.Linear)&&V(a.Velocity.Angular,b.Velocity.Angular),"native-only vs publication: every pose/velocity component bit");
                Check(Observe(raw)==original,"native-only control does not publish canonical state");
            }
            Console.WriteLine("ASSEMBLY_CONTACT_PRIVATE_NATIVE PASS 1200 intervals all native pose/velocity bits identical; canonical control unchanged");
        }
        var reference=new AssemblyFlightRecord[1200];
        using(var s=Session())
        {
            Credit(s,20_000_000);
            for(var i=0;i<300;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4,"prefunded reference four-interval bound");
            for(var i=0;i<1200;i++)Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out reference[i]),"reference history");
        }
        foreach(var fps in new[]{30,60,150,240,0})
        {
            using var s=Session();var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
            long elapsed=0;var frames=fps==0?40:20*fps;var published=0;var budgetExhausted=0;
            for(var frame=1;frame<=frames;frame++)
            {
                var target=(long)frame*20_000_000/frames;Credit(s,target-elapsed);elapsed=target;
                var service=s.Engine.ServiceAssemblyContactDebt(s.Authority);published+=service.PublishedCount;
                Check(service.PublishedCount<=4,"per-display service budget");
                if(service.Status==AssemblyFlightStatus.BudgetExhausted)budgetExhausted++;
                Check(service.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.BudgetExhausted or AssemblyFlightStatus.Completed,"schedule service result");
                Check(Observe(s).Clock.Debt.Ticks==elapsed-Observe(s).State.Epoch.Ticks,"host accounting conservation");
            }
            while(published<1200)
            {
                var service=s.Engine.ServiceAssemblyContactDebt(s.Authority);Check(service.PublishedCount is >0 and <=4,"drain existing debt without resubmission");published+=service.PublishedCount;
            }
            var final=Observe(s);
            for(var i=0;i<1200;i++)
            {
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out var actual),"partition history");
                Check(actual==reference[i]&&MotionBits(actual.Successor.Motion,reference[i].Successor.Motion),"all frontier bits and value history match independent prefunded schedule");
            }
            Check(final.State.Epoch.Ticks==20_000_000&&final.Clock.Debt.Ticks==0&&final.StateRevision.Value==1200&&final.HistoryCount==1200&&final.TimelineRevision.Value==0,"final clock/revision/history");
            Check(final.State.Stores==s.Launch.Initial.Stores&&final.State.Mass==s.Launch.Initial.Mass&&final.State.ResourceRevision==0,"exact stores/mass");
            var last=world.AssemblyIdentityForTest;Check(last.Generation==identity.Generation&&last.Body==identity.Body&&last.Shape==identity.Shape&&!last.Pending,"retained identity");
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,final.HostSequence+1,new(1)).Status==AssemblyFlightStatus.Completed&&s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.Completed&&Observe(s)==final,"source end holds with no additional credit or step");
            Console.WriteLine($"ASSEMBLY_CONTACT_SCHEDULE fps={fps} inputs={frames} budgetExhausted={budgetExhausted} ticks=20000000 publications=1200 debt=0 allFrontierBits=EXACT");
        }
        using(var s=Session(new(.125,.0625,-.03125)))
        {
            for(var i=0;i<1200;i++)
            {
                var v=Next(s);var expected=reference[i].Successor.Motion;var elapsed=(i+1)*1_000_000L/60/1e6;var frame=s.Launch.Initial.Motion.VelocityO;
                Check((v.State.Motion.PositionO-(expected.PositionO+frame*elapsed)).LengthSquared<1e-27,"original moving-origin epoch");
                Check((v.State.Motion.VelocityO-(expected.VelocityO+frame)).LengthSquared<1e-28&&Bits(v.State.Motion.BodyToWorld,expected.BodyToWorld)&&Bits(v.State.Motion.AngularVelocityBody,expected.AngularVelocityBody),"moving frame preserves native trajectory");
            }
            Console.WriteLine("ASSEMBLY_CONTACT_MOVING PASS 1200 intervals; original frame epoch and native trajectory preserved");
        }
        using(var s=Session())
        {
            Credit(s,20_000_017);for(var i=0;i<300;i++)s.Engine.ServiceAssemblyContactDebt(s.Authority);
            var final=Observe(s);Check(final.Clock.Debt.Ticks==17&&final.State.Epoch.Ticks==20_000_000,"completion overshoot retained");
            Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.Completed&&Observe(s)==final,"no evolution beyond finite source");
        }
    }

    private static bool NoWork(AssemblyApplicationSession s,ref AssemblyFlightObservation v)=>
        s.Engine.AdmitAssemblyHostTime(s.Authority,v.HostSequence+1,new(0)).Status==AssemblyFlightStatus.NoWork&&
        s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.AwaitingDebt&&
        s.Engine.ObserveAssemblyFlight(s.Authority,out v)==AssemblyFlightStatus.Ready;
    private static bool RetryOperation(AssemblyApplicationSession s,ref AssemblyFlightObservation v)
    {
        var e=s.Engine;var a=s.Authority;
        if(e.AdmitAssemblyHostTime(a,v.HostSequence+1,new(s.Launch.Plan[v.State.Frontier].Request.Ticks)).Status!=AssemblyFlightStatus.AcceptedCredit||
            e.PrepareAssemblyContact(a,out var proposal)!=AssemblyFlightStatus.Prepared)return false;
        return e.PublishAssemblyContact(a,proposal,refuseForTest:true).Status==AssemblyFlightStatus.PreparationRefused&&
            e.PublishAssemblyContact(a,proposal).Status==AssemblyFlightStatus.Published&&e.ObserveAssemblyFlight(a,out v)==AssemblyFlightStatus.Ready;
    }

    internal static void Allocation(bool powered=false)
    {
        if(powered)Console.WriteLine("POWERED_SUPPORTED_ASSEMBLY_ALLOCATION");
        AssemblyApplicationSession Session()=>powered?PoweredSession():AssemblyContactAdmissionTests.Session();
        using(var s=Session())
        {
            var v=Observe(s);for(var i=0;i<128;i++)Check(CompleteOperation(s,ref v),"allocation warmup");
            using var measure=new OrdinaryAllocationMeasurement("assembly-contact-complete");
            var ok=true;for(var i=0;i<1024;i++)ok&=CompleteOperation(s,ref v);
            var bytes=measure.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"assembly-contact-complete");Check(ok&&v.State.Frontier==1152,"allocation workload completed");
        }
        using(var s=Session())
        {
            var v=Observe(s);for(var i=0;i<128;i++)Check(NoWork(s,ref v),"no-work warmup");var original=v;
            using var measure=new OrdinaryAllocationMeasurement("assembly-contact-no-work");
            var ok=true;for(var i=0;i<1024;i++)ok&=NoWork(s,ref v);
            var bytes=measure.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"assembly-contact-no-work");Check(ok&&v==original,"no-work canonical nonmutation");
        }
        using(var s=Session())
        {
            Credit(s,20_000_000);for(var i=0;i<32;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4,"backlog warmup");
            using var measure=new OrdinaryAllocationMeasurement("assembly-contact-backlog");
            var ok=true;for(var i=0;i<256;i++)ok&=s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4;
            var bytes=measure.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"assembly-contact-backlog");Check(ok&&Observe(s).State.Frontier==1152,"backlog exact workload");
        }
        using(var s=Session())
        {
            var v=Observe(s);for(var i=0;i<128;i++)Check(RetryOperation(s,ref v),"retry warmup");
            using var measure=new OrdinaryAllocationMeasurement("assembly-contact-refusal-retry");
            var ok=true;for(var i=0;i<1024;i++)ok&=RetryOperation(s,ref v);
            var bytes=measure.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"assembly-contact-refusal-retry");Check(ok&&v.State.Frontier==1152,"retry without repeated solve");
        }
        OrdinaryAllocationMeasurement.PositiveControl();
    }

    internal static void Storage(bool powered=false)
    {
        if(powered)Console.WriteLine("POWERED_SUPPORTED_ASSEMBLY_STORAGE");
        AssemblyApplicationSession Session()=>powered?PoweredSession():AssemblyContactAdmissionTests.Session();
        using(var warm=Session()){var v=Observe(warm);CompleteOperation(warm,ref v);}
        var before=GC.GetAllocatedBytesForCurrentThread();var cold=Stopwatch.GetTimestamp();using var s=Session();
        var coldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;var initialManaged=GC.GetAllocatedBytesForCurrentThread()-before;
        var v2=Observe(s);for(var i=0;i<128;i++)Check(CompleteOperation(s,ref v2),"storage warmup");
        var managed=GC.GetAllocatedBytesForCurrentThread()-before;var native=s.Engine.AssemblyContactWorldForTest(s.Authority)!.PoolBytes;
        var history=(long)Unsafe.SizeOf<AssemblyFlightRecord>()*1200;var credits=(long)Unsafe.SizeOf<AssemblyHostCredit>()*4801;
        Console.WriteLine($"ASSEMBLY_CONTACT_STORAGE managedCold={initialManaged} managedConstructionWarmUpperBound={managed} nativePool={native} combinedUpperBound={managed+(long)native} historyPayloadIncluded={history} creditPayloadIncluded={credits} limit=8388608 coldPreparedMs={coldMs:R}");
        Check(managed+(long)native<=8L*1024*1024,"retained upper bound <=8MiB");
    }

    internal static void Performance(bool powered=false)
    {
        if(powered)Console.WriteLine("POWERED_SUPPORTED_ASSEMBLY_PERFORMANCE");
        AssemblyApplicationSession Session()=>powered?PoweredSession():AssemblyContactAdmissionTests.Session();
        var samples=new double[1024];var order=new double[1024];var cold=Stopwatch.GetTimestamp();using var s=Session();var coldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        var v=Observe(s);var first=Stopwatch.GetTimestamp();Check(CompleteOperation(s,ref v),"first physical use");var firstMs=Stopwatch.GetElapsedTime(first).TotalMilliseconds;
        for(var i=1;i<128;i++)Check(CompleteOperation(s,ref v),"performance warmup");
        var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
        for(var i=0;i<1024;i++)
        {var start=Stopwatch.GetTimestamp();var ok=CompleteOperation(s,ref v);var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;samples[i]=order[i]=ms;Check(ok,"timed complete operation");}
        Array.Sort(samples);var median=(samples[511]+samples[512])/2;var p95=samples[972];var p99=samples[1013];var maximum=samples[^1];
        Console.WriteLine("ASSEMBLY_CONTACT_PERFORMANCE "+JsonSerializer.Serialize(new{median,p95,p99,maximum,coldMs,firstMs,warm=128,measured=1024,gc=new[]{GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2},tails=order.Select((ms,index)=>new{index,ms}).OrderByDescending(x=>x.ms).Take(8)}));
        if(!powered)Check(median<=.05&&p95<=.10&&p99<=.25&&maximum<=.50,"unchanged Stage 1 reproduction ceilings");
        else Console.WriteLine("Historical micro-gates reported only; Stage 2 acceptance requires integrated headroom/tail review.");
    }
}
