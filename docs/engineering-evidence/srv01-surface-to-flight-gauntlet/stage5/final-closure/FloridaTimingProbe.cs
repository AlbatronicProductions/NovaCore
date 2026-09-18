using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

// Disposable compile-overlay observer. Never included in the canonical project files.
internal sealed class FloridaTimingProbe
{
    private readonly AssemblyApplicationSession session;
    private readonly LocalContactWorld world;
    private readonly BepuPhysics.Simulation simulation;
    private readonly BepuPhysics.BodyHandle body;
    private readonly object site;
    private readonly Row[] rows=new Row[40000];
    private int count;
    internal readonly record struct Stamp(int Index,long Time,int Frontier,int Constraints,ulong Pool,long Bytes,int G0,int G1,int G2,long Cpu,long Generation,int Body,int Shape);
    private readonly record struct Row(int Index,long Time,int Before,int After,int ConstraintsBefore,int ConstraintsAfter,ulong PoolBefore,ulong PoolAfter,long Allocated,int G0,int G1,int G2,double ThreadCpuMs,double DisplayMs,double ServiceMs,bool SameWorld,bool SameSite,bool Pending,bool Invalidated);
    internal FloridaTimingProbe(AssemblyApplicationSession s)
    {
        session=s;world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;site=s.Launch.Site!;
        simulation=(BepuPhysics.Simulation)world.GetType().GetField("simulation",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(world)!;
        body=new(world.AssemblyIdentityForTest.Body);
        s.Engine.ObserveAssemblyFlight(s.Authority,out var v);
        for(var i=0;i<16;i++){var before=Begin(-1,0,v);End(before,v,0,0);}
    }
    [DllImport("kernel32.dll",SetLastError=true)]
    private static extern bool GetThreadTimes(IntPtr thread,out long creation,out long exit,out long kernel,out long user);
    private static long Cpu()
    {if(!GetThreadTimes(new IntPtr(-2),out _,out _,out var kernel,out var user))throw new InvalidOperationException("GetThreadTimes failed");return kernel+user;}
    internal Stamp Begin(int index,long time,AssemblyFlightObservation v)
    {
        var id=world.AssemblyIdentityForTest;
        return new(index,time,v.State.Frontier,simulation.Bodies[body].Constraints.Count,world.PoolBytes,
            GC.GetAllocatedBytesForCurrentThread(),GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),Cpu(),id.Generation,id.Body,id.Shape);
    }
    internal void End(Stamp stamp,AssemblyFlightObservation v,double display,double service)
    {
        var cpu=Cpu();var bytes=GC.GetAllocatedBytesForCurrentThread();var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
        var id=world.AssemblyIdentityForTest;
        var row=new Row(stamp.Index,stamp.Time,stamp.Frontier,v.State.Frontier,stamp.Constraints,simulation.Bodies[body].Constraints.Count,
            stamp.Pool,world.PoolBytes,bytes-stamp.Bytes,g0-stamp.G0,g1-stamp.G1,g2-stamp.G2,(cpu-stamp.Cpu)/10000d,
            display,service,ReferenceEquals(world,session.Engine.AssemblyContactWorldForTest(session.Authority))&&id.Generation==stamp.Generation&&id.Body==stamp.Body&&id.Shape==stamp.Shape,
            ReferenceEquals(site,session.Launch.Site),id.Pending,id.Invalidated);
        if(stamp.Index>=0)rows[count++]=row;
    }
    internal void Report(AssemblyFlightObservation v)
    {
        var identity=world.AssemblyIdentityForTest;
        var repeated=session.Engine.ServiceAssemblyContactDebt(session.Authority);
        session.Engine.ObserveAssemblyFlight(session.Authority,out var after);
        if(repeated.Status!=AssemblyFlightStatus.Completed||repeated.PublishedCount!=0||after!=v||world.AssemblyIdentityForTest!=identity)
            throw new InvalidOperationException("Terminal observer nonmutation check failed");
        ulong hash=14695981039346656037;
        void Mix(double value){hash^=(ulong)BitConverter.DoubleToInt64Bits(value);hash*=1099511628211;}
        for(var i=0;i<1200;i++)
        {
            if(!session.Engine.TryGetAssemblyHistory(session.Authority,i,out var h))throw new InvalidOperationException("Missing final history");
            var m=h.Successor.Motion;
            Mix(m.PositionO.X);Mix(m.PositionO.Y);Mix(m.PositionO.Z);Mix(m.VelocityO.X);Mix(m.VelocityO.Y);Mix(m.VelocityO.Z);
            Mix(m.BodyToWorld.X);Mix(m.BodyToWorld.Y);Mix(m.BodyToWorld.Z);Mix(m.BodyToWorld.W);Mix(m.AngularVelocityBody.X);Mix(m.AngularVelocityBody.Y);Mix(m.AngularVelocityBody.Z);
        }
        Console.WriteLine("FINAL_TIMING_META "+JsonSerializer.Serialize(new{count,frequency=Stopwatch.Frequency,frontier=v.State.Frontier,history=v.HistoryCount,tick=v.Clock.Time.Ticks,debt=v.Clock.Debt.Ticks,
            consumer=v.Consumer.ToString(),pending=identity.Pending,invalidated=identity.Invalidated,terminalService=repeated.Status.ToString(),terminalPublished=repeated.PublishedCount,trajectoryHash=hash.ToString("X16")}));
        for(var i=0;i<count;i++)Console.WriteLine("FINAL_TIMING_ROW "+JsonSerializer.Serialize(rows[i]));
    }
}
