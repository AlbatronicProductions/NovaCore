using System.Diagnostics;
using System.Text.Json;
using System.Runtime.CompilerServices;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularFloridaTests
{
    internal static void Measurements()
    {
        var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));var rows=new List<object>();var storage=new List<object>();
        object Distribution(IEnumerable<double> source){var values=source.Order().ToArray();double P(double p)=>values[(int)Math.Ceiling(p*values.Length)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=values[^1]};}
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            for(var window=0;window<3;window++)
            {
                var milliseconds=new List<double>();var allocations=new List<double>();var collections=new int[3];ulong pool=0;
                for(var sample=-3;sample<24;sample++)
                {
                    var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};var before=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
                    using var session=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
                    var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var allocation=GC.GetAllocatedBytesForCurrentThread()-before;
                    pool=session.Engine.ConstructionContactWorldForTest(session.Authority)!.PoolBytes;
                    if(sample>=0){milliseconds.Add(elapsed);allocations.Add(allocation);for(var g=0;g<3;g++)collections[g]+=GC.CollectionCount(g)-gc[g];}
                }
                var threshold=milliseconds.Order().ElementAt((int)Math.Ceiling(milliseconds.Count*.95)-1);var consecutive=0;var longest=0;
                foreach(var sample in milliseconds){consecutive=sample>=threshold?consecutive+1:0;longest=Math.Max(longest,consecutive);}
                rows.Add(new{longer,craft=craft.Digest,window,samples=milliseconds.Count,milliseconds=Distribution(milliseconds),allocatedBytes=Distribution(allocations),collections,nativePoolBytes=pool,maximumConsecutiveP95=longest});
            }
            for(var sample=0;sample<3;sample++)storage.Add(new{longer,sample,retained=Retained(craft,terrain.Query,terrain.Slab)});
        }
        Console.WriteLine(JsonSerializer.Serialize(new{schema="novacore.modular-florida-measure/1",boundary="CompiledCraft already prepared; immutable physical preparation, authenticated site checks, canonical registration, 69-convex native world and cold controls; excludes shared dataset acquisition, compilation, camera, render and stepping; timings use normal GC; retained storage measured separately after full collection",rows,storage}));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Retained(CompiledCraft craft,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab)
    {
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var before=GC.GetTotalMemory(true);
        using var session=ConstructionApplicationSession.CreateSupported(craft,query,slab);
        var managed=GC.GetTotalMemory(true)-before;var native=session.Engine.ConstructionContactWorldForTest(session.Authority)!.PoolBytes;
        GC.KeepAlive(session);return new{managedBytes=managed,nativePoolBytes=native};
    }
}
