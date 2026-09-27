using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void PhysicalOracleFixtures()
    {
        var catalog=StarterCatalog();var rows=new List<object>();
        static double[] V(Double3 v)=>[v.X,v.Y,v.Z];static double[] M(Matrix3 m)=>[m.A,m.B,m.C,m.D,m.E,m.F,m.G,m.H,m.I];
        static double[] State(AssemblyMotion m)=>[m.PositionO.X,m.PositionO.Y,m.PositionO.Z,m.VelocityO.X,m.VelocityO.Y,m.VelocityO.Z,m.BodyToWorld.X,m.BodyToWorld.Y,m.BodyToWorld.Z,m.BodyToWorld.W,m.AngularVelocityBody.X,m.AngularVelocityBody.Y,m.AngularVelocityBody.Z];
        foreach(var longer in new[]{false,true}){
            var craft=CraftCompiler.Compile(catalog,StarterCraft(catalog,longer).Data,"assets/vehicles/modular-starter");var services=new ConstructionPhysicalServices(craft);var control=new CompiledCraftControl(craft);
            var quantities=craft.Mass.Stores.Select((s,i)=>s.CapacityKg*(i==0?.125:.375)).ToArray();
            var fuel=ConstructionFuelState.Create(craft.Fuel,quantities.Select(q=>ConstructionFuelNetwork.Decode(q,true)*craft.Fuel.Scale).ToImmutableArray(),1,0);
            var row=control.Resolve(new(true));var evolved=services.Advance(fuel,craft.Power.Initial(),15625,row.Consumers.AsSpan());
            var initial=new AssemblyMotion(default,new(4,5,6),DoubleQuaternion.Identity,new(.2,-.15,.1));var gravity=new Double3(0,-9.81,0);
            var result=AssemblyDynamics.Evaluate(control,initial,evolved,default,row,gravity);
            rows.Add(new{craft=longer?"long":"short",dry=new{mass=craft.Mass.Dry.Mass,com=V(craft.Mass.Dry.Com),inertia=M(craft.Mass.Dry.Inertia)},
                stores=craft.Mass.Stores.Select(s=>new{com=V(s.Datum),inertiaPerKg=M(s.InertiaPerKg)}),before=quantities,after=ConstructionNumerics.Quantities(evolved.Fuel),dt=.015625,
                gravity=V(gravity),force=new[]{30720d,0,0},source=State(initial),result=State(result.Motion)});
        }
        Console.WriteLine(JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void PhysicalMeasurements()
    {
        var catalog=StarterCatalog();var rows=new List<object>();
        object Distribution(IEnumerable<double> values){var a=values.Order().ToArray();double P(double p)=>a[(int)Math.Ceiling(a.Length*p)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=a[^1]};}
        foreach(var longer in new[]{false,true}){
            var craft=CraftCompiler.Compile(catalog,StarterCraft(catalog,longer).Data,"assets/vehicles/modular-starter");var services=new ConstructionPhysicalServices(craft);var control=new CompiledCraftControl(craft);
            var initialFuel=craft.Fuel.Initial();var initialPower=craft.Power.Initial();
            foreach(var mode in new[]{"powered-combined","rcs-coast","power-boundary"}){
                var row=control.Resolve(new(mode!="rcs-coast",new(1,-1,1)));var power=initialPower;
                if(mode=="power-boundary"){var b=craft.Power.Modules.FindIndex(m=>m.Role==ElectricalRole.Battery);power=ConstructionPowerState.Create(craft.Power,power.Charge.SetItem(b,ConstructionFuelNetwork.Decode(.5,true)),1,power.Active,power.Cursor,0);}
                for(var window=0;window<3;window++){
                    var timings=new List<double>();var allocations=new List<double>();var collections=new int[3];var serviceTimes=new List<double>();var dynamicTimes=new List<double>();var retainedBefore=GC.GetTotalMemory(false);
                    for(var sample=-4;sample<64;sample++){
                        var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};var bytes=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
                        var next=services.Advance(initialFuel,power,15625,row.Consumers.AsSpan());var boundary=Stopwatch.GetTimestamp();
                        var motion=AssemblyDynamics.Evaluate(control,new(default,default,DoubleQuaternion.Identity,new(.2,-.15,.1)),next,default,row,new(0,-9.81,0));
                        var end=Stopwatch.GetTimestamp();if(!motion.Motion.Finite)throw new InvalidDataException("Measurement motion refused.");
                        var allocation=GC.GetAllocatedBytesForCurrentThread()-bytes;
                        if(sample>=0){timings.Add(Stopwatch.GetElapsedTime(start,end).TotalMilliseconds);serviceTimes.Add(Stopwatch.GetElapsedTime(start,boundary).TotalMilliseconds);dynamicTimes.Add(Stopwatch.GetElapsedTime(boundary,end).TotalMilliseconds);allocations.Add(allocation);for(var i=0;i<3;i++)collections[i]+=GC.CollectionCount(i)-gc[i];}
                    }
                    var sorted=timings.Order().ToArray();var p95=sorted[(int)Math.Ceiling(.95*sorted.Length)-1];var consecutive=0;var maxConsecutive=0;
                    foreach(var t in timings){consecutive=t>=p95?consecutive+1:0;maxConsecutive=Math.Max(maxConsecutive,consecutive);}
                    rows.Add(new{craft=longer?"long":"short",mode,window,samples=64,milliseconds=Distribution(timings),serviceMilliseconds=Distribution(serviceTimes),dynamicsMilliseconds=Distribution(dynamicTimes),allocatedBytes=Distribution(allocations),gc=collections,
                        retainedDeltaWithoutForcedGc=GC.GetTotalMemory(false)-retainedBefore,maximumConsecutiveAtOrAboveWindowP95=maxConsecutive});
                }
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new{scope="Prepared service and free-flight numerical proposals only; fresh equivalent source each sample, normal GC; canonical publication/contact/render not yet included; no zero-allocation claim",rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
