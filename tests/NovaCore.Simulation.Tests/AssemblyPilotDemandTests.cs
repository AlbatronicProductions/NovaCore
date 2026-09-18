using System.Runtime.CompilerServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static class AssemblyPilotDemandTests
{
    private sealed record LegacyRequest(bool MainOn);
    private sealed record LegacyAdmission(AssemblyControlIdentity Identity,long Sequence,int Frontier,long HostSequence,LegacyRequest Requested,LegacyRequest Effective);
    private static int checks;
    private static readonly AssemblyStockCatalog Catalog=AssemblyStockCatalog.LoadDefault();
    private static void Check(bool pass,string message){if(!pass)throw new InvalidOperationException("PILOT DEMAND: "+message);checks++;}
    private static AssemblyApplicationSession Create()
    {
        var s=AssemblyApplicationSession.Create(new(Catalog.Resolve("novacore.stock.SRV01.FourHorn"),new(new(201),new(1),new(2),"Pilot"),"pilot-proof",new(default,default,DoubleQuaternion.Identity,default),default,Enumerable.Repeat(new AssemblyCommand(false,null,0,0,15625),128).ToArray()));
        s.EnableLiveControl();return s;
    }
    private static AssemblyControlObservation Control(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyControl(s.Control!,out var c)==AssemblyControlStatus.Ready,"observe control");return c;}
    private static AssemblyFlightObservation Physical(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var c)==AssemblyFlightStatus.Ready,"observe physical");return c;}
    private static AssemblyControlResult Request(AssemblyApplicationSession s,AssemblyControlRequest request)=>s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,Control(s).AdmissionCount+1L,request);
    private static void Next(AssemblyApplicationSession s,bool fragmented=false)
    {
        foreach(var ticks in fragmented?new long[]{1,624,7000,8000}:new long[]{15625})
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,Physical(s).HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"service");
    }
    private static void Reject(Action action)
    {try{action();}catch(InvalidDataException){checks++;return;}throw new InvalidOperationException("Pilot snapshot unexpectedly accepted");}
    internal static void Run()
    {
        Check(Encoding.UTF8.GetString(AssemblyJson.Write(new AssemblyControlRequest(true)))=="{\"mainOn\":true}","v3 ON payload exact bytes");
        Check(Encoding.UTF8.GetString(AssemblyJson.Write(new AssemblyControlRequest(false)))=="{\"mainOn\":false}","v3 OFF payload exact bytes");
        var oldRecord=new LegacyAdmission(new(new(201),1),1,0,0,new(true),new(true));
        var newRecord=new AssemblyControlAdmission(new(new(201),1),1,0,0,new(true),new(true));
        Check(AssemblyJson.Write(new[]{oldRecord}).AsSpan().SequenceEqual(AssemblyJson.Write(new[]{newRecord})),"complete v3 journal byte compatibility, hence unchanged digest");
        // Independent enumeration of all 64 held-intent combinations. An opposite
        // release restores the surviving sign; it is not an unconditional zero.
        for(var mask=0;mask<64;mask++)
        {
            bool Held(int bit)=>(mask&(1<<bit))!=0;
            var demand=AssemblyPilotDemand.FromOpposingRequests(Held(0),Held(1),Held(2),Held(3),Held(4),Held(5));
            sbyte Axis(int bit)=>Held(bit)==Held(bit+1)?(sbyte)0:Held(bit)?(sbyte)1:(sbyte)-1;
            Check(demand==new AssemblyPilotDemand(Axis(0),Axis(2),Axis(4))&&demand.IsValid,"independent ternary opposing/release rule");
        }
        using(var s=Create())
        {
            var initial=Physical(s);var p=new AssemblyPilotDemand(1,-1,1);
            Check(Request(s,new(false,p,true)).Status==AssemblyControlStatus.Admitted&&Physical(s)==initial&&!Control(s).Requested.MainOn,"READY pilot intent has no physics or ignition");
            var ready=s.Save();Check(AssemblyJson.Read<AssemblySaveData>(ready,1_048_576).Schema=="novacore.assembly-runtime/4","demand-only execution version");
            using var restored=AssemblyApplicationSession.Restore(Catalog,ready);
            Check(restored.Save().AsSpan().SequenceEqual(ready)&&Control(restored)==Control(s),"READY demand durable byte roundtrip");
            Check(Request(s,new(true,p)).Admission.Effective.MainOn,"pilot-only OFF latch creates no cutoff veto");
            Check(Request(s,new(false,new(-1,1,-1),true)).Admission.Effective.MainOn,"pilot update preserves ON latch");
            Request(s,new(false,p));Request(s,new(false,default,true));
            var veto=Request(s,new(true,p));Check(!veto.Admission.Effective.MainOn&&veto.Admission.Effective.Pilot==p,"explicit OFF dominates with intervening pilot update");
            Next(s);Check(Request(s,new(true,p)).Admission.Effective.MainOn,"old arbitration expires");
            var physical=Physical(s);var control=Control(s);
            foreach(var invalid in new[]{new AssemblyControlRequest(false,new(2,0,0),true),new(false,new(0,-2,0),true),new(false,new(0,0,127),true),new(true,p,true)})
                Check(Request(s,invalid).Status==AssemblyControlStatus.InvalidInput&&Physical(s)==physical&&Control(s)==control,"invalid pilot payload atomic refusal");
            var last=control.AdmissionCount;
            Check(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,last,new(true,p)).Status==AssemblyControlStatus.Duplicate,"exact pilot retry");
            Check(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,last,new(true,default)).Status==AssemblyControlStatus.InvalidSequence,"changed pilot retry refused");
            var data=AssemblyJson.Read<AssemblySaveData>(s.Save(),1_048_576);
            Reject(()=>AssemblyApplicationSession.Restore(Catalog,AssemblyJson.Write(data with {Schema="novacore.assembly-runtime/3"})));
            var changed=data.Live!.Admissions.ToArray();changed[0]=changed[0] with {Requested=new(false,new(-1,-1,1),true)};
            Reject(()=>AssemblyApplicationSession.Restore(Catalog,AssemblyJson.Write(data with {Live=data.Live with {Admissions=changed}})));
            Request(s,new(false,default,true));Next(s);
            Check(Physical(s).State.Motion.AngularVelocityBody==default&&Physical(s).State.Gimbal==default&&Physical(s).State.Actual.Jets==0,"Stage3 records demand, does not allocate ideal torque");
        }
        using(var a=Create())using(var b=Create())
        {
            for(var i=0;i<128;i++)
            {
                var demand=new AssemblyPilotDemand((sbyte)(i%3-1),(sbyte)((i/3)%3-1),(sbyte)((i/9)%3-1));
                var request=new AssemblyControlRequest(i<32||i>=64&&i<96,demand);
                Request(a,request);Request(b,request);Next(a);Next(b,true);
                var x=Physical(a);var y=Physical(b);
                Check(x.State==y.State&&x.StateRevision==y.StateRevision&&x.Clock.Time==y.Clock.Time&&x.Clock.Debt==y.Clock.Debt,"all27 simultaneous demands host fragmentation equivalence");
                Check(a.Engine.TryGetAssemblyHistory(a.Authority,i,out var ah)&&b.Engine.TryGetAssemblyHistory(b.Authority,i,out var bh)&&ah==bh,"canonical physical history equality");
                Check(Control(a).Requested.Pilot==demand&&x.State.Actual.Jets==0&&x.State.Gimbal==default,"independent axes retained, no unqualified allocator");
                if(i==47){using var r=AssemblyApplicationSession.Restore(Catalog,a.Save());Check(r.Save().AsSpan().SequenceEqual(a.Save()),"midflight demand replay exact bytes");}
            }
            using var replay=AssemblyApplicationSession.Restore(Catalog,b.Save());
            Check(replay.Save().AsSpan().SequenceEqual(b.Save()),"terminal demand replay exact bytes");
        }
        Measure();Console.WriteLine($"PILOT_DEMAND PASS checks={checks}");
    }
    private static void Measure()
    {
        // Mutable admission fixtures are fresh and prepared outside measurement.
        using(var warm=Create())for(var i=0;i<256;i++)Request(warm,new(false,new((sbyte)(i%3-1),1,-1),true));
        var timed=Enumerable.Range(0,8).Select(_=>Create()).ToArray();
        var sessions=Enumerable.Range(0,8).Select(_=>Create()).ToArray();
        var samples=new double[2048];var index=0;
        var gc0=GC.CollectionCount(0);var gc1=GC.CollectionCount(1);var gc2=GC.CollectionCount(2);
        var rawBefore=GC.GetAllocatedBytesForCurrentThread();
        foreach(var s in timed)for(var i=0;i<256;i++)
        {
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            var result=s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,i+1,new(false,new((sbyte)(i%3-1),1,-1),true));
            samples[index++]=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if(result.Status!=AssemblyControlStatus.Admitted)throw new InvalidOperationException("measured pilot admission");
        }
        var rawBytes=GC.GetAllocatedBytesForCurrentThread()-rawBefore;
        var collections=$"{GC.CollectionCount(0)-gc0}/{GC.CollectionCount(1)-gc1}/{GC.CollectionCount(2)-gc2}";
        using var region=new OrdinaryAllocationMeasurement("pilot-admission");
        foreach(var s in sessions)for(var i=0;i<256;i++)
        {
            var result=s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,i+1,new(false,new((sbyte)(i%3-1),1,-1),true));
            if(result.Status!=AssemblyControlStatus.Admitted)throw new InvalidOperationException("allocation pilot admission");
        }
        var bytes=region.Complete();
        Check(bytes==0,"2048 pilot admissions allocate zero");
        Array.Sort(samples);
        Console.WriteLine($"PILOT_PERFORMANCE samples=2048 median_ms={samples[1023]:R} p95_ms={samples[1945]:R} p99_ms={samples[2027]:R} max_ms={samples[^1]:R} raw_counter_bytes={rawBytes} collections={collections} normal_runtime=true");
        var saved=sessions[0].Save();Check(saved.Length<1_048_576,"full256 pilot journal save bounded");
        Console.WriteLine($"PILOT_STORAGE admission_bytes={Unsafe.SizeOf<AssemblyControlAdmission>()} capacity=256 payload_bytes={Unsafe.SizeOf<AssemblyControlAdmission>()*256} full_journal_save_bytes={saved.Length} admission_allocation_bytes={bytes}");
        foreach(var s in sessions)s.Dispose();
        foreach(var s in timed)s.Dispose();
        OrdinaryAllocationMeasurement.PositiveControl();
    }
}
