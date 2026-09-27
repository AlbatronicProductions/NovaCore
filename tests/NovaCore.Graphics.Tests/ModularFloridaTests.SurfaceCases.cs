using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceCases(string? selectedKind=null,string? selectedMode=null)
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var catalogBefore=catalog.Save();var report=new List<object>();
        foreach(var kind in new[]{"short","long","asymmetric"})
        {
            if(selectedKind is not null&&kind!=selectedKind)continue;
            var data=Craft(catalog,kind=="long").Data;
            if(kind=="asymmetric")
            {
                // Three opposed pairs at unequal clock spacing retain the
                // accepted axial COM/gimbal profile while producing a truly
                // asymmetric transverse inertia and physical collision hull.
                var retained=data.Instances.Take(4).Concat(new[]{0,1,2,4,5,6}.Select(i=>data.Instances[4+i])).Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);
                var parts=data.Instances.Where(p=>retained.Contains(p.Id)).Select((p,i)=>p with{Order=i}).ToImmutableArray();
                data=data with{Instances=parts,Connections=data.Connections.Where(e=>retained.Contains(e.Child)).ToImmutableArray(),Configuration=data.Configuration.Where(c=>retained.Contains(c.Part)).ToImmutableArray(),Symmetry=[],DependencyDigest=catalog.DependencyDigest(parts)};
            }
            var craft=CraftCompiler.Compile(catalog,data,Assets);
            Need(craft.Function&&craft.AdmissionDiagnostics.IsEmpty,$"{kind} fixture admission: {string.Join(";",craft.Diagnostics.Concat(craft.AdmissionDiagnostics))}");
            if(kind=="asymmetric")Need(craft.InitialMass.Inertia.E!=craft.InitialMass.Inertia.I,"asymmetric inertia is physical");
            using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
            foreach(var mode in new[]{"gentle","harder","skid","tilt","side","roll","rock","tip","envelope"})
            {
                if(selectedMode is not null&&mode!=selectedMode)continue;
                var angle=mode is "tilt" or "rock"?.12:mode=="tip"?.6:mode is "side" or "roll"?Math.PI/2:0;
                var q=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,angle)*AssemblyContactProfile.Upright;
                var lowest=craft.Collision.SelectMany(h=>h.Vertices).Min(v=>q.Rotate(v).Y);
                var supportPlane=cold.Binding.Physical!.Contact.SupportPlane;
                var origin=new Double3(0,supportPlane-lowest+.01,0);
                var velocity=new Double3(mode=="skid"?.7:0,mode=="envelope"?-7.5:mode=="harder"?-1.5:-.2,0);
                var angular=mode=="roll"?new Double3(.25,0,0):mode=="rock"?new Double3(0,0,1.5):Double3.Zero;
                if(mode=="rock")velocity=new Double3(0,-.05,0)-q.Rotate(Double3.Cross(angular,craft.InitialMass.Com));
                var saved=basis with{Physical=new(new(origin,velocity,q,angular),default,AssemblyPhysicalConsumer.FreeFlight)};
                var preparation=Stopwatch.GetTimestamp();
                using var s=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(saved),Assets,terrain.Query,terrain.Slab);
                var prepareMs=Stopwatch.GetElapsedTime(preparation).TotalMilliseconds;
                var energy=new SurfaceEnergy(s);
                long host=0;var contacts=0;var maximumDepth=0d;var departed=false;var recontacts=0;var priorContact=false;
                var geometricTouches=0;var geometricRelease=false;var touched=false;var airborneAgain=false;var maxReleasedGap=0d;var maxUpwardSpeed=0d;
                var footTouches=new int[craft.Support.Length];var footWasTouching=new bool[craft.Support.Length];
                var countTicks=mode=="tip"||kind=="long"&&mode is "tilt" or "rock"?1024:256;
                var costs=new double[countTicks];var allocations=new double[countTicks];
                for(var i=0;i<costs.Length;i++)
                {
                    var start=Stopwatch.GetTimestamp();var bytes=GC.GetAllocatedBytesForCurrentThread();
                    Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625))==ConstructionServiceStatus.AcceptedCredit,"surface case host credit");
                    var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);
                    Need(status==ConstructionServiceStatus.Published&&count==1,$"{kind}/{mode} step {i}: {status}; {s.Engine.ConstructionSupportFailure(s.Authority)}");
                    allocations[i]=GC.GetAllocatedBytesForCurrentThread()-bytes;costs[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    var state=Observe(s);energy.Sample(state,i);var actual=s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records[^1].Contacts;
                    contacts+=actual;var has=actual>0;if(!has&&priorContact)departed=true;if(has&&!priorContact)recontacts++;priorContact=has;
                    var motion=state.Physical!.Motion;
                    foreach(var v in craft.Collision.SelectMany(h=>h.Vertices))maximumDepth=Math.Max(maximumDepth,supportPlane-(motion.PositionO+motion.BodyToWorld.Rotate(v)).Y);
                    var gap=craft.Collision.SelectMany(h=>h.Vertices).Min(v=>(motion.PositionO+motion.BodyToWorld.Rotate(v)).Y)-supportPlane;
                    if(gap<=.002&&!touched){geometricTouches++;touched=true;airborneAgain=false;}
                    if(touched&&gap>.002){geometricRelease=true;airborneAgain=true;touched=false;}
                    if(airborneAgain)maxReleasedGap=Math.Max(maxReleasedGap,gap);
                    maxUpwardSpeed=Math.Max(maxUpwardSpeed,(motion.VelocityO+motion.BodyToWorld.Rotate(Double3.Cross(motion.AngularVelocityBody,state.ReferenceMass!.Value.Com))).Y);
                    for(var f=0;f<craft.Support.Length;f++)
                    {
                        var foot=craft.Support[f];var footGap=double.PositiveInfinity;
                        foreach(var y in new[]{-foot.Foot.HalfWidthY,foot.Foot.HalfWidthY})foreach(var z in new[]{-foot.Foot.HalfWidthZ,foot.Foot.HalfWidthZ})
                            footGap=Math.Min(footGap,(motion.PositionO+motion.BodyToWorld.Rotate(foot.MaterialFrame.Point(new(0,y,z)))).Y-supportPlane);
                        var footTouching=footGap<=.002;if(footTouching&&!footWasTouching[f])footTouches[f]++;footWasTouching[f]=footTouching;
                    }
                    Need(motion.Finite,"finite rigid-body response");
                }
                Need(contacts>0,"real surface contact");
                var final=Observe(s);var checkpoint=s.Save();
                Need(maximumDepth<NovaCore.Simulation.Spacecraft.Contact.Staging.CraftSurfaceImpact.MinimumRecoverySaturationDepth,"independent pad hull penetration stays below the earliest saturation transition");
                Need(Norm(final.Physical!.Motion.VelocityO)<.002&&Norm(final.Physical.Motion.AngularVelocityBody)<.002,"unpowered motion dissipates to physical rest");
                if(mode=="rock"&&kind!="long")Need(footTouches.Max()>=3,"independent authored foot geometry proves repeated separation and recontact");
                if(mode=="tip")Need(geometricRelease&&geometricTouches>=2&&maxReleasedGap>.002,"whole compound geometric separation and recontact after physical tipping");
                using(var restored=ConstructionApplicationSession.RestoreFlight(catalog,checkpoint,Assets,terrain.Query,terrain.Slab))
                {
                    Need(restored.Save().SequenceEqual(checkpoint)&&Observe(restored).Physical==final.Physical,"grounded physical checkpoint exact roundtrip");
                    Need(restored.Engine.AdmitConstructionHostTime(restored.Authority,host+1,new(15625))==ConstructionServiceStatus.AcceptedCredit&&restored.Engine.ServiceConstructionDebt(restored.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"grounded reload continues actual physics");
                }
                if(mode=="gentle")
                {
                    var initialFuel=final.Fuel.Save();
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(false,new(0,0,1))).Status==AssemblyControlStatus.Admitted,"grounded RCS command");
                    for(var i=0;i<4;i++){s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"grounded RCS solve");}
                    Need(!Observe(s).Fuel.Save().SequenceEqual(initialFuel),"grounded RCS consumes exact propellant");
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,2,new(false)).Status==AssemblyControlStatus.Admitted,"grounded release");
                    s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"off frontier advances");
                    Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,3,new(true)).Status==AssemblyControlStatus.Admitted,"grounded ignition");
                    for(var i=0;i<128;i++){s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"physical relaunch solve");}
                    Need(Observe(s).Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight&&Observe(s).Physical!.Motion.PositionO.Y>final.Physical!.Motion.PositionO.Y+.1,"actual thrust relaunch returns to free flight");
                }
                object Stats(double[] values){var ordered=values.Order().ToArray();return new{median=ordered[(ordered.Length-1)/2],p95=ordered[(int)Math.Ceiling(ordered.Length*.95)-1],p99=ordered[(int)Math.Ceiling(ordered.Length*.99)-1],maximum=ordered[^1]};}
                report.Add(new{kind,mode,prepareMs,contacts,maximumDepth,energy.MaximumGain,energy.MaximumAt,energy.Allowance,departed,recontacts,geometricTouches,geometricRelease,maxReleasedGap,maxUpwardSpeed,footTouches,finalSpeed=Norm(final.Physical!.Motion.VelocityO),finalAngular=Norm(final.Physical.Motion.AngularVelocityBody),milliseconds=Stats(costs),allocated=Stats(allocations)});
                Console.WriteLine(JsonSerializer.Serialize(report[^1]));
            }
            foreach(var consumer in new[]{AssemblyPhysicalConsumer.FreeFlight,AssemblyPhysicalConsumer.SurfaceContact})
            {
                var buried=basis with{Physical=new(new(new(80,-100,0),default,AssemblyContactProfile.Upright,default),default,consumer)};
                var before=cold.Save();
                Refuse(()=>{using var invalid=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(buried),Assets,terrain.Query,terrain.Slab);},"wholly buried physical checkpoint");
                Need(before.SequenceEqual(cold.Save()),"burial refusal preserves current source");
            }
        }
        Need(catalogBefore.SequenceEqual(catalog.Save()),"surface qualification preserves catalog");
        Console.WriteLine($"SURFACE_CASES_PASS checks={checks}");
    }
}
