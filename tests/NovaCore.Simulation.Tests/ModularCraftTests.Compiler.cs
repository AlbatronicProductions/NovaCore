using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void CraftCompilerMeasurements()
    {
        var catalog=StarterCatalog();var rows=new List<object>();
        object Distribution(IEnumerable<double> source){var v=source.Order().ToArray();double P(double p)=>v[(int)Math.Ceiling(p*v.Length)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=v[^1]};}
        foreach(var longer in new[]{false,true}){
            var document=StarterCraft(catalog,longer);string? digest=null;
            for(var window=0;window<3;window++){
                var elapsed=new List<double>();var allocation=new List<double>();var collections=new int[3];
                for(var sample=-2;sample<16;sample++){
                    var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};var bytes=GC.GetAllocatedBytesForCurrentThread();var start=System.Diagnostics.Stopwatch.GetTimestamp();
                    var craft=CraftCompiler.Compile(catalog,document.Data,"assets/vehicles/modular-starter");
                    var time=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;var allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
                    digest??=craft.Digest;if(digest!=craft.Digest||!craft.Function)throw new InvalidDataException("Compiler measurement identity/function changed.");
                    if(sample>=0){elapsed.Add(time);allocation.Add(allocated);for(var g=0;g<3;g++)collections[g]+=GC.CollectionCount(g)-gc[g];}
                }
                rows.Add(new{craft=longer?"long":"short",window,samples=16,milliseconds=Distribution(elapsed),allocatedBytes=Distribution(allocation),gc=collections,source=document.Digest,compiled=digest});
            }
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{scope="Cold full compiler; fresh preparation and asset verification each sample; normal GC; warm OS file cache possible; report only",rows},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void CraftCompilerGate()
    {
        checks=0;var catalog=StarterCatalog();const string assets="assets/vehicles/modular-starter";
        var catalogBytes=catalog.Save();var generation=typeof(ConstructionRuntimeBinding).GetField("nextGeneration",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        var beforeGeneration=generation.GetValue(null);
        foreach(var longer in new[]{false,true}){
            var source=StarterCraft(catalog,longer);var bytes=source.Save();var craft=CraftCompiler.Compile(catalog,source.Data,assets);
            Check(craft.Function&&craft.Diagnostics.IsEmpty&&!craft.PhysicalAdmissionQualified,"complete craft FUNCTION without live admission claim");
            Check(craft.Design.Digest==source.Digest&&source.Save().SequenceEqual(bytes),"compiler seals exact source without mutation");
            Check(craft.Dependencies.Length==5&&craft.Render.Length==12&&craft.Ports.Ports.Length==197,"used content and stable render/port coverage");
            Check(craft.Actuators.Length==33&&craft.AllocationGeometry.Length==33&&craft.Actuators.Count(a=>a.Gimbal is not null)==1,"one main plus 32 independently bound jets");
            Check(craft.Fuel.Stores.Length==2&&craft.Fuel.Consumers.Length==33&&craft.Power.Modules.Length==11,"exact stores, consumers and finite modules");
            Check(craft.Fuel.Consumers.All(c=>c.Terms.Length==2&&c.Terms.All(t=>t.Stores.Length==1)),"each mixture resolves exact resource supplier");
            Check(craft.Actuators.All(a=>a.DataReachable&&a.RequiredLoads.Length==2),"every actuator resolves own and core power prerequisites");
            var nonData=craft.Ports.Ports.Select((p,i)=>(p,i)).Where(x=>x.p.Definition.Service==ConstructionService.Electricity).Take(2).Select(x=>x.i).ToArray();
            Check(!craft.Power.Data.Connected(nonData[0],nonData[1])&&!craft.Power.CanCommand(-1),"non-data/invalid indices cannot grant command connectivity");
            Check(craft.Allocation.Complete&&craft.Allocation.JetActuators.Length==32&&craft.Allocation.AxisJets.Length==6&&craft.Allocation.AxisJets.Any(m=>m.Contains(31)),"compiled population-sized allocation includes the final jet and every axis sign");
            var example=craft.Actuators.First(a=>a.Model=="nc.actuator.attitude/1");
            var tinyA=example with {Point=new(0,1,0),Axis=Double3.UnitZ,Thrust=1e-200};
            var tinyB=example with {Point=new(0,-1,0),Axis=-Double3.UnitZ,Thrust=1e-200*(1-1e-6)};
            Check(new CompiledCraftAllocation([tinyA,tinyB],craft.Mass).AxisJets[1].IsEmpty,"underflowing squared residual cannot admit unbalanced pure couple");
            Reject(()=>new CompiledCraftAllocation([tinyA with {Point=new(0,1e153,0),Thrust=1e155},tinyB with {Point=new(0,-1e153,0),Thrust=1e155}],craft.Mass),"finite columns with overflowing pair moment refuse");
            Check(craft.Collision.Length==69&&craft.Support.Length==4&&craft.Clearance.Length==2,"complete hardware profile independent of render; RCS plumes are not placement obstacles");
            Check(craft.InitialMass.Mass==(longer?2164:1304)&&craft.Mass.Dry.Mass==(longer?564:504),"compiled exact dry and wet physical masses");
            foreach(var a in new[]{0d,.125,.5,1})foreach(var b in new[]{0d,.375,1}){
                var quantities=new[]{craft.Mass.Stores[0].CapacityKg*a,craft.Mass.Stores[1].CapacityKg*b};var observed=craft.Mass.Evaluate(quantities);
                var independent=AssemblyConstructionFacts.Aggregate(new[]{new MassRegionData("dry",craft.Mass.Dry.Mass,craft.Mass.Dry.Com,craft.Mass.Dry.Inertia)}.Concat(craft.Mass.Stores.Select((s,i)=>new MassRegionData(s.Key.Store,quantities[i],s.Datum,s.InertiaPerKg*quantities[i])).Where(r=>r.MassKg>0)));
                Check(Math.Abs(observed.Mass-independent.Mass)<1e-10&&(observed.Com-independent.Com).LengthSquared<1e-24&&(observed.Inertia-independent.Inertia).Maximum<1e-9,"independent aggregate at unequal partial fills");
            }
            Reject(()=>craft.Mass.Evaluate(new[]{-1d,0}),"negative physical quantity");Reject(()=>craft.Mass.Evaluate(new[]{double.NaN,0}),"nonfinite physical quantity");
            var reversed=source.Data with {Instances=source.Data.Instances.Reverse().ToImmutableArray(),Connections=source.Data.Connections.Reverse().ToImmutableArray(),Configuration=source.Data.Configuration.Reverse().ToImmutableArray()};
            Check(CraftCompiler.Compile(catalog,reversed,assets).Digest==craft.Digest,"enumeration independent source/compiled identity");
            var shifted=source.Data with {Instances=source.Data.Instances.Select(p=>p with {Pose=p.Pose with {Position=p.Pose.Position+new Double3(100,200,300)}}).ToImmutableArray()};
            var rebased=CraftCompiler.Compile(catalog,shifted,assets);
            Check((rebased.InitialMass.Com-craft.InitialMass.Com).LengthSquared<1e-24&&(rebased.InitialMass.Inertia-craft.InitialMass.Inertia).Maximum<1e-9,"material frame independent of common design translation");
            Check(craft.Fuel.Consumers.Single(c=>c.Key.Part=="engine").Terms.All(t=>t.Levels.Single()==2),"two explicit part crossings to main engine");
            Check(craft.Fuel.Consumers.Where(c=>c.Key.Part.StartsWith("block-",StringComparison.Ordinal)).All(c=>c.Terms.All(t=>t.Levels.Single()==1)),"one crossing to every radial consumer");
            var broken=source.Data with {Connections=source.Data.Connections.Select(e=>e.Child=="adapter"?e with {Construction=e.Construction! with {Services=ConstructionService.None}}:e).ToImmutableArray()};
            var isolated=CraftCompiler.Compile(catalog,broken,assets);
            Check(!isolated.Function&&isolated.Diagnostics.Any(d=>d.Code=="FUEL_PATH"&&d.Part=="engine")&&isolated.Diagnostics.Any(d=>d.Code=="DATA_PATH"&&d.Part=="engine"),"structural joint cannot invent fuel/data");
            Check(isolated.Diagnostics.Any(d=>d.Code=="POWER_PATH"&&d.Part=="engine")&&isolated.Actuators.Where(a=>a.Key.Part.StartsWith("block-",StringComparison.Ordinal)).All(a=>a.DataReachable),"isolated downstream power; sibling data unaffected");
            var empty=source.Data with {Configuration=source.Data.Configuration.Select(c=>c with {Stores=c.Stores.Select(s=>s with {QuantityKg=0}).ToImmutableArray(),Electrical=c.Electrical.Select(e=>e with {ChargeJ=0}).ToImmutableArray()}).ToImmutableArray()};
            var noEnergy=CraftCompiler.Compile(catalog,empty,assets);Check(!noEnergy.Function&&noEnergy.Diagnostics.Any(d=>d.Code=="FUEL_PATH")&&noEnergy.Diagnostics.Any(d=>d.Code=="POWER_PATH"),"empty energy remains FIT, separately lacks FUNCTION");
            Check(craft.Function&&craft.InitialMass.Mass==(longer?2164:1304),"subsequent compilation cannot mutate old artifact");
        }
        foreach(var service in new[]{ConstructionService.Propellant,ConstructionService.Electricity,ConstructionService.Data}){
            var edited=catalog.Data.Definitions.Select(d=>d.Id=="nc.tank.short-2"?d with {Standard=d.Standard! with {Routes=d.Standard.Routes.Where(r=>d.Standard.Ports.Single(p=>p.Id==r.From).Service!=service).ToImmutableArray()}}:d).ToImmutableArray();
            var broken=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=edited});var result=CraftCompiler.Compile(broken,StarterCraft(broken,false).Data,assets);
            var code=service==ConstructionService.Propellant?"FUEL_PATH":service==ConstructionService.Electricity?"POWER_PATH":"DATA_PATH";
            Check(!result.Function&&result.Diagnostics.Any(d=>d.Code==code),"missing explicit internal route cannot be replaced by part connectivity "+service);
        }
        Check(catalog.Save().SequenceEqual(catalogBytes)&&Equals(beforeGeneration,generation.GetValue(null)),"compiler neither mutates catalog nor creates runtime identity");
        using(var one=new ConstructionEditorSession(catalog)){
            one.Load(0,StarterCraft(catalog,false).Save());one.Remove(one.Revision,one.Current!.Design.Data.Symmetry.Single().BasePart);
            var block=catalog.Data.Definitions.Single(d=>d.Id=="nc.rcs.block-r1");one.PreviewPlacement(one.Revision,new(catalog.Reference(block),"tank","radial-0","mount",0,1,"solo"));one.AcceptPreview(one.Revision);
            var insufficient=CraftCompiler.Compile(catalog,one.Current!.Design.Data,assets);
            Check(!insufficient.Function&&insufficient.Diagnostics.Any(d=>d.Code=="ATTITUDE_AUTHORITY"),"powered/fed single block cannot claim independent three-axis authority");
        }
        var unsupported=catalog.Data.Definitions.Select(d=>d.Id=="nc.mount.single-2to1"?d with {Standard=d.Standard! with {Support=[]}}:d).ToImmutableArray();
        var noFeet=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=unsupported});var functions=CraftCompiler.Compile(noFeet,StarterCraft(noFeet,false).Data,assets);
        Check(functions.Function&&functions.AdmissionDiagnostics.Any(d=>d.Code=="SUPPORT_MISSING")&&!functions.PhysicalAdmissionQualified,"site support refusal remains separate from FUNCTION");
        var core=catalog.Data.Definitions.Single(d=>d.Id=="nc.core.command-2");
        var coreLaw=new CraftMassLaw(new(core.DryMassKg,core.LocalCom,core.LocalInertia),[]);
        var quantum=System.Numerics.BigInteger.One<<PartStandardExact.QuantumExponent;
        var exact=PartStandardExact.Encode(core.LocalInertia.E)*quantum*quantum+PartStandardExact.Encode(core.DryMassKg)*(PartStandardExact.Encode(core.LocalCom.X)*PartStandardExact.Encode(core.LocalCom.X)+PartStandardExact.Encode(core.LocalCom.Z)*PartStandardExact.Encode(core.LocalCom.Z));
        Check(PartStandardExact.Encode(coreLaw.OriginInertiaMinimum.E)*quantum*quantum<=exact&&PartStandardExact.Encode(coreLaw.OriginInertiaMaximum.E)*quantum*quantum>=exact,"exact core-only parallel-axis product is enclosed before final rounding");
        Reject(()=>CraftCompiler.Compile(catalog,StarterCraft(catalog,false).Data with {DependencyDigest=new string('0',64)},assets),"compiler rechecks dependency pin");
        Console.WriteLine($"Modular Gate 7 compiler PASS: {checks} immutable compilation/port/physical checks; runtime not qualified");
    }
}
