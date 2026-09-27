using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    // Report-only normal-GC timing. Observation, retained-memory collections,
    // serialization and test assertions remain outside the measured service.
    internal static void SurfacePerformance()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(var longer in new[]{false,true})foreach(var mode in new[]{"gentle","skid","roll"})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            var q=(mode=="roll"?DoubleQuaternion.FromAxisAngle(Double3.UnitZ,Math.PI/2):DoubleQuaternion.Identity)*AssemblyContactProfile.Upright;
            var plane=cold.Binding.Physical!.Contact.SupportPlane;
            var lowest=craft.Collision.SelectMany(h=>h.Vertices).Min(v=>q.Rotate(v).Y);
            var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
            var bytes=AssemblyJson.Write(basis with{Physical=new(new(new(0,plane-lowest+.01,0),new(mode=="skid"?.7:0,-.2,0),q,mode=="roll"?new(.25,0,0):default),default,AssemblyPhysicalConsumer.FreeFlight)});
            var stamp=Stopwatch.GetTimestamp();var s=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
            var prepareMs=Stopwatch.GetElapsedTime(stamp).TotalMilliseconds;
            using(s)
            {
                long host=0;var firstContact=-1;var rows=new List<SurfaceCost>(1536);var worlds=new HashSet<LocalContactWorld>();
                SurfaceCost Step()
                {
                    var begin=Stopwatch.GetTimestamp();var allocated=GC.GetAllocatedBytesForCurrentThread();
                    var credit=s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));
                    var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);
                    var cost=Stopwatch.GetElapsedTime(begin).TotalMilliseconds;allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
                    Need(credit==ConstructionServiceStatus.AcceptedCredit&&status==ConstructionServiceStatus.Published&&count==1,"performance physical successor");
                    var world=s.Engine.ConstructionContactWorldForTest(s.Authority);if(world is not null)worlds.Add(world);
                    var contacts=s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records[^1].Contacts;
                    var constraints=world is null?0:Field<BepuPhysics.Simulation>(world,"simulation").Bodies[Field<BepuPhysics.BodyHandle>(world,"body")].Constraints.Count;
                    return new(cost,allocated,contacts,world?.PoolBytes??0,constraints);
                }
                for(var i=0;i<256;i++){var row=Step();rows.Add(row);if(firstContact<0&&row.LastSliceContacts>0)firstContact=i;}
                Need(firstContact>=0,"measured first actual contact");
                void AssertRest()
                {
                    var state=Observe(s);var motion=state.Physical!.Motion;
                    var comVelocity=motion.VelocityO+motion.BodyToWorld.Rotate(Double3.Cross(motion.AngularVelocityBody,state.ReferenceMass!.Value.Com));
                    Need(state.Physical.Consumer==AssemblyPhysicalConsumer.SurfaceContact&&Norm(comVelocity)<.002&&Norm(motion.AngularVelocityBody)<.002,"measured actual resting contact, COM and angular speed");
                }
                AssertRest();
                var retainedBefore=GC.GetTotalMemory(true);
                var restWindows=new List<object>();
                for(var window=0;window<3;window++)
                {
                    AssertRest();
                    var start=rows.Count;for(var i=0;i<256;i++)rows.Add(Step());
                    AssertRest();
                    restWindows.Add(SurfaceCostReport(rows.Skip(start).ToArray()));
                }
                var retainedAfter=GC.GetTotalMemory(true);
                var resting=Observe(s);Need(Norm(resting.Physical!.Motion.VelocityO)<.002,"measured physical rest");
                object? rcs=null;object? ignition=null;
                if(mode=="gentle")
                {
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(false,new(0,0,1))).Status==AssemblyControlStatus.Admitted,"measured grounded RCS command");
                    var active=new SurfaceCost[4];for(var i=0;i<active.Length;i++)active[i]=Step();rcs=SurfaceCostReport(active);
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,2,new(false)).Status==AssemblyControlStatus.Admitted,"measured release");
                    for(var i=0;i<64;i++)Step();
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,3,new(true)).Status==AssemblyControlStatus.Admitted,"measured grounded ignition");
                    var powered=new List<SurfaceCost>();
                    do{powered.Add(Step());}while(powered.Count<256&&Observe(s).Physical!.Consumer!=AssemblyPhysicalConsumer.FreeFlight);
                    Need(Observe(s).Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight,"measured real relaunch");ignition=SurfaceCostReport(powered.ToArray());
                }
                var baselineNativeMaximum=rows.Max(r=>r.PoolBytes);s.Dispose();
                Need(worlds.All(w=>w.PoolBytes==0),"observed surviving contact worlds release their native pool allocation on disposal");
                Console.WriteLine(JsonSerializer.Serialize(new{kind=longer?"long":"short",mode,prepareMs,firstPublishedContactStep=firstContact,firstPublishedContact=rows[firstContact],transient=SurfaceCostReport(rows.Take(256).ToArray()),restWindows,rcs,ignition,baselineNativeMaximum,retainedManagedBefore=retainedBefore,retainedManagedAfter=retainedAfter,retainedManagedDelta=retainedAfter-retainedBefore,nativeAfterDispose=worlds.Sum(w=>(long)w.PoolBytes)}));
            }
        }
        Console.WriteLine($"SURFACE_PERFORMANCE_PASS checks={checks}; timings report-only; managed retention is process-level with test observations, native pool ownership is exact");
    }
    private readonly record struct SurfaceCost(double Milliseconds,long Allocated,int LastSliceContacts,ulong PoolBytes,int Constraints);
    private static object SurfaceCostReport(SurfaceCost[] rows)
    {
        object Stats(IEnumerable<double> source){var a=source.Order().ToArray();return new{median=a[(a.Length-1)/2],p95=a[(int)Math.Ceiling(a.Length*.95)-1],p99=a[(int)Math.Ceiling(a.Length*.99)-1],maximum=a[^1]};}
        return new{samples=rows.Length,milliseconds=Stats(rows.Select(r=>r.Milliseconds)),allocated=Stats(rows.Select(r=>(double)r.Allocated)),maximumSampledContacts=rows.Max(r=>r.LastSliceContacts),maximumSampledConstraints=rows.Max(r=>r.Constraints),nativePoolMaximum=rows.Max(r=>r.PoolBytes)};
    }
}
