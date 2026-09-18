using NovaCore.Simulation.Spacecraft.Assemblies;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

internal static partial class AssemblyContactAdmissionTests
{
    internal static void DepartureStorage()
    {
        using(var warm=DepartureSession()){Check(DepartureOperation(warm,16666),"storage warmup");}
        var before=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        using var s=DepartureSession();
        var coldMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var managed=GC.GetAllocatedBytesForCurrentThread()-before;
        var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var nativeCold=world.PoolBytes;
        Check(DepartureOperation(s,16666)&&DepartureOperation(s,15625),"storage complete path");
        var nativeAfter=world.PoolBytes;
        var profileBefore=GC.GetAllocatedBytesForCurrentThread();
        var profile=new AssemblyDepartureProfile(s.Launch.ContactProfile!,default);
        var profileBytes=GC.GetAllocatedBytesForCurrentThread()-profileBefore;GC.KeepAlive(profile);
        var historyPayload=4L*Unsafe.SizeOf<AssemblyFlightRecord>();var creditPayload=4801L*Unsafe.SizeOf<AssemblyHostCredit>();
        Console.WriteLine("DEPARTURE_STORAGE "+JsonSerializer.Serialize(new{managedConstructionUpperBound=managed,nativeCold,nativeAfter,combinedUpperBound=managed+(long)nativeAfter,profileBytes,historyPayload,creditPayload,coldMs,limit=8388608}));
        Check(managed+(long)nativeAfter<=8388608,"departure retained storage <=8MiB");
    }

    internal static void DeparturePerformance()
    {
        const int warm=128,count=1024;
        var handoff=new double[count];var flight=new double[count];
        var handoffGc=new int[3];var flightGc=new int[3];
        var coldMs=0d;var firstMs=0d;var process=Process.GetCurrentProcess();
        var cpuStart=process.TotalProcessorTime;
        for(var i=-warm;i<count;i++)
        {
            // Each transfer occurs once per world. Cold preparation/disposal is
            // outside timing; no simulated step or handoff is hidden in setup.
            var cold=Stopwatch.GetTimestamp();using var s=DepartureSession();
            if(i==-warm)coldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
            var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
            var begin=Stopwatch.GetTimestamp();var ok=DepartureOperation(s,16666);
            var h=Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
            if(i==-warm)firstMs=h;
            if(i>=0){handoff[i]=h;handoffGc[0]+=GC.CollectionCount(0)-g0;handoffGc[1]+=GC.CollectionCount(1)-g1;handoffGc[2]+=GC.CollectionCount(2)-g2;}
            Check(ok,"timed handoff");
            g0=GC.CollectionCount(0);g1=GC.CollectionCount(1);g2=GC.CollectionCount(2);
            begin=Stopwatch.GetTimestamp();ok=DepartureOperation(s,15625);
            var f=Stopwatch.GetElapsedTime(begin).TotalMilliseconds;
            if(i>=0){flight[i]=f;flightGc[0]+=GC.CollectionCount(0)-g0;flightGc[1]+=GC.CollectionCount(1)-g1;flightGc[2]+=GC.CollectionCount(2)-g2;}
            Check(ok,"timed first successor");
        }
        var wholeHarnessCpuMs=(process.TotalProcessorTime-cpuStart).TotalMilliseconds;
        void Report(string population,double[] order,int[] gc)
        {
            var sorted=(double[])order.Clone();Array.Sort(sorted);
            Console.WriteLine("DEPARTURE_PERFORMANCE "+JsonSerializer.Serialize(new{population,warm,count,median=(sorted[511]+sorted[512])/2,p95=sorted[972],p99=sorted[1013],maximum=sorted[^1],gc,coldMs,firstMs,wholeHarnessCpuMs,tails=order.Select((ms,index)=>new{index,ms}).OrderByDescending(x=>x.ms).Take(8)}));
        }
        Report("complete-contact-handoff",handoff,handoffGc);
        Report("complete-first-free-flight",flight,flightGc);
        Console.WriteLine("Current integrated headroom/tail review applies; historical first-ticket micro-gates are not acceptance thresholds.");
    }
}
