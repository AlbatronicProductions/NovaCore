using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;
using NovaCore.Interop;

internal static partial class ModularFloridaTests
{
    internal static void FlightMeasurements()
    {
        var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));var rows=new List<object>();var storage=new List<object>();
        object Distribution(double[] source){var values=source.Order().ToArray();double P(double q)=>values[(int)Math.Ceiling(q*values.Length)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=values[^1]};}
        foreach(var longer in new[]{false,true})foreach(var boundary in new[]{"supported","powered-flight","coast"})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            for(var window=0;window<3;window++)
            {
                using var session=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);long host=0;
                void Step()
                {
                    if(session.Engine.AdmitConstructionHostTime(session.Authority,++host,new(15625))!=ConstructionServiceStatus.AcceptedCredit||
                        session.Engine.ServiceConstructionDebt(session.Authority,out var count)!=ConstructionServiceStatus.Published||count!=1)throw new InvalidDataException("Measurement physical service refused.");
                }
                if(boundary!="supported"){session.Engine.AdmitAssemblyControl(session.Control!,session.Control!.Identity,1,new(true));for(var i=0;i<640;i++)Step();}
                if(boundary=="coast")session.Engine.AdmitAssemblyControl(session.Control!,session.Control!.Identity,2,new(false));
                for(var i=0;i<32;i++)Step();
                var timing=new double[256];var allocated=new double[256];var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
                for(var i=0;i<timing.Length;i++)
                {
                    var before=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();Step();
                    timing[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;allocated[i]=GC.GetAllocatedBytesForCurrentThread()-before;
                }
                for(var i=0;i<3;i++)gc[i]=GC.CollectionCount(i)-gc[i];
                var cutoff=timing.Order().ElementAt((int)Math.Ceiling(timing.Length*.95)-1);var run=0;var longest=0;
                foreach(var value in timing){run=value>=cutoff?run+1:0;longest=Math.Max(longest,run);}
                rows.Add(new{longer,boundary,window,samples=timing.Length,milliseconds=Distribution(timing),allocatedBytes=Distribution(allocated),collections=gc,maximumConsecutiveP95=longest});
            }
        }
        var zeroCraft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using(var s=ConstructionApplicationSession.CreateSupported(zeroCraft,terrain.Query,terrain.Slab))
        {
            var input=new PlayerFlightControlInput(s);var native=new NativeInputState{ControlInputActive=1,PilotKeys=NativePilotKeys.Q};input.Apply(native);
            void Work(){for(var i=0;i<1000;i++){input.Apply(native);if(s.Engine.ObserveConstructionActuation(s.Authority,out _ )!=ConstructionServiceStatus.Ready)throw new InvalidDataException();}}
            for(var i=0;i<8;i++)Work();var before=GC.GetAllocatedBytesForCurrentThread();Work();var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Need(bytes==0,"warm held-input and realized-actuation observations allocate zero");storage.Add(new{heldInputAndActuationObservationBytes=bytes,iterations=1000});
        }
        // Separate retained storage runs, after a full collection; never mixed
        // into normal-GC latency samples. Shared compiled content is excluded.
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var before=GC.GetTotalMemory(true);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(true));
            for(var i=1;i<=767;i++){s.Engine.AdmitConstructionHostTime(s.Authority,i,new(15625));Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"retained flight");}
            var bytes=GC.GetTotalMemory(true)-before;var records=s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records.Length;
            storage.Add(new{longer,managedBytes=bytes,retainedRecords=records,nativePoolBytes=s.Engine.ConstructionContactWorldForTest(s.Authority)?.PoolBytes??0});GC.KeepAlive(s);
        }
        Console.WriteLine(JsonSerializer.Serialize(new{schema="novacore.modular-flight-measure/1",boundary="One host credit + one canonical 1/64s interval including exact ledgers, native contact or flight preparation, clearance, history and publication. Normal GC timings; setup/compiler/assets/render/camera excluded. Retained storage measured separately.",rows,storage}));
    }
}
