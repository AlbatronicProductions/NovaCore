using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static class RcsScalabilityTests
{
    private static int checks;
    private static void Need(bool value,string why){checks++;if(!value)throw new InvalidDataException("RCS scalability: "+why);}
    private static void Refuse(Action action,string why){try{action();}catch(InvalidDataException){checks++;return;}throw new Exception("Accepted "+why);}
    private static double Component(Double3 v,int a)=>a==0?v.X:a==1?v.Y:v.Z;
    private static double Norm(Double3 v){var s=Math.Max(Math.Abs(v.X),Math.Max(Math.Abs(v.Y),Math.Abs(v.Z)));return s==0?0:s*Math.Sqrt((v/s).LengthSquared);}
    // Deliberately exhaustive independent oracle. Production must not retain
    // this quadratic pair list. Also supports the former stock tie order.
    private static ImmutableArray<int>[] Oracle(ImmutableArray<CraftActuator> jets,CompiledCraftAllocation allocation,bool old=false)
    {
        var result=new ImmutableArray<int>[6];
        for(var axis=0;axis<3;axis++)for(var sign=-1;sign<=1;sign+=2){
            var rank=Enumerable.Range(0,jets.Length).OrderByDescending(j=>Component(Double3.Cross(jets[j].Point,jets[j].Axis*jets[j].Thrust),axis)*sign).ThenBy(j=>j).Select((j,r)=>(j,r)).ToDictionary(x=>x.j,x=>x.r);
            var candidates=new List<(double Strength,int I,int J)>();
            for(var i=0;i<jets.Length;i++)for(var j=i+1;j<jets.Length;j++){
                var f=jets[i].Axis*jets[i].Thrust+jets[j].Axis*jets[j].Thrust;
                var m=Double3.Cross(jets[i].Point,jets[i].Axis*jets[i].Thrust)+Double3.Cross(jets[j].Point,jets[j].Axis*jets[j].Thrust);
                if(Norm(f)>allocation.ForceTolerance||Component(m,axis)*sign<=allocation.TorqueTolerance||Enumerable.Range(0,3).Any(a=>a!=axis&&Math.Abs(Component(m,a))>allocation.TorqueTolerance))continue;
                candidates.Add((Component(m,axis)*sign,i,j));
            }
            var selected=new HashSet<int>();foreach(var p in candidates.OrderByDescending(p=>p.Strength).ThenBy(p=>p.I).ThenBy(p=>old?p.J:rank[p.J]))
                if(!selected.Contains(p.I)&&!selected.Contains(p.J)){selected.Add(p.I);selected.Add(p.J);}
            result[axis*2+(sign>0?1:0)]=selected.Order().ToImmutableArray();
        }
        return result;
    }
    private static void CompareOracle(ImmutableArray<CraftActuator> jets,CraftMassLaw mass,bool old=false)
    {
        var a=new CompiledCraftAllocation(jets,mass);var expected=Oracle(jets,a,old);
        for(var i=0;i<6;i++)Need(a.AxisJets[i].SequenceEqual(expected[i]),"complete streamed allocation equals exhaustive pair oracle");
    }
    internal static void Run(string? output=null)
    {
        checks=0;var catalog=RcsScalabilityFixture.Catalog();var half=RcsScalabilityFixture.Catalog(true);var before=catalog.Save();var rows=new List<object>();
        foreach(var members in new[]{1,2,4,8}){
            var d=RcsScalabilityFixture.Document(catalog,1,members);var c=RcsScalabilityFixture.Compile(catalog,d);
            Need(c.Allocation.JetActuators.Length==members*4&&c.Actuators.Select(a=>a.Key).Distinct().Count()==members*4+1,"every symmetry member expands independently");
            Need(CompiledConstructionDesign.LoadCraft(catalog,d.Save()).Save().SequenceEqual(d.Save()),"all symmetry save/reload identities");
        }
        foreach(var count in new[]{8,32,64,96,288}){
            var library=count==96?half:catalog;var d=RcsScalabilityFixture.Document(library,Math.Max(1,count/32),count==8?2:8,count==96?RcsScalabilityFixture.HalfTank:"nc.tank.short-2");
            var c=RcsScalabilityFixture.Compile(library,d);var fuel=c.Fuel.Initial();var power=c.Power.Initial();
            Need(c.Allocation.JetActuators.Length==count&&c.Fuel.Consumers.Length==count+1,"no first-N truncation including former service ceiling");
            Need(c.Actuators.Select(a=>a.Key).Distinct().Count()==count+1,"unique complete actuator identity");
            var reload=RcsScalabilityFixture.Compile(library,CompiledConstructionDesign.LoadCraft(library,d.Save()));Need(reload.Digest==c.Digest,"recompile identity");
            var reverse=d.Data with {Instances=d.Data.Instances.Reverse().ToImmutableArray(),Connections=d.Data.Connections.Reverse().ToImmutableArray(),Configuration=d.Data.Configuration.Reverse().ToImmutableArray()};
            Need(CraftCompiler.Compile(library,reverse,RcsScalabilityFixture.Assets).Digest==c.Digest,"enumeration cannot replace authored order");
            Need(ConstructionFuelState.Load(c.Fuel,fuel.Save()).Save().SequenceEqual(fuel.Save())&&ConstructionPowerState.Load(c.Power,power.Save()).Save().SequenceEqual(power.Save()),"exact service save/reload");
            foreach(var term in c.Fuel.Consumers.SelectMany(x=>x.Terms))for(var n=1;n<=term.Stores.Length;n++){
                Need(term.ShareFactors.IsDefault&&term.ShareNumerator%n==0&&term.ShareFactor(n)*n==term.ShareNumerator,"compact exact sharing lattice, no table or division residue");
            }
            var jets=c.Allocation.JetActuators.Select(i=>c.Actuators[i]).ToImmutableArray();CompareOracle(jets,c.Mass);if(count==32)CompareOracle(jets,c.Mass,true);
            if(count==8){Need(!c.Function&&c.Diagnostics.Any(x=>x.Code=="ATTITUDE_AUTHORITY"),"eight stock jets retain real missing-axis refusal");
                var request=Enumerable.Repeat(true,count+1).ToArray();var flow=ConstructionFuelSolver.Advance(fuel,15625,request);Need(flow.ActiveTicks.Length==count+1&&flow.ActiveTicks.All(t=>t.Numerator>0),"all eight jets retain service delivery");
            }else{
                Need(c.Function,"balanced complete craft FUNCTION");var control=new CompiledCraftControl(c);var services=new ConstructionPhysicalServices(c);
                if(count<=96){var contact=new CompiledCraftContact(c);Need(CraftSupportPreparation.Prepare(contact,c.InitialMass,new(0,-9.81,0)).MaximumObservedLoad<=15000,"unchanged support law accepts explicit vertical gravity fixture");}
                for(var main=0;main<2;main++)for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++){
                    var row=control.Resolve(new(main!=0,new(pitch,yaw,roll)));var g=new AssemblyGimbal(row.TargetY,row.TargetZ,row.TargetY,row.TargetZ);
                    var w=AssemblyActuation.Resolve(c,row.Consumers.AsSpan(),g);var force=Double3.Zero;var moment=Double3.Zero;
                    foreach(var a in c.Actuators)if(row.Consumers[a.Consumer]){var aw=a.Gimbal is null?new AssemblyWrench(a.Axis*a.Thrust,Double3.Cross(a.Point,a.Axis*a.Thrust)):AssemblyActuation.Main(a,g.ActualY,g.ActualZ);force+=aw.Force;moment+=aw.MomentAtOrigin;}
                    Need(w==new AssemblyWrench(force,moment),"all54 rows independently accumulate every realized actuator exactly once");
                    var view=new ConstructionJetObservation(services,row,fuel,power);Need(view.Count==jets.Count(a=>row.Consumers[a.Consumer])&&Enumerable.Range(0,count).Count(view.Contains)==view.Count,"complete availability view");
                }
                var command=control.Resolve(new(false,new(1,1,1)));var observation=new ConstructionJetObservation(services,command,fuel,power);
                Need(observation.Contains(count-1)||Enumerable.Range(32,Math.Max(0,count-32)).Any(observation.Contains)||count==32,"high indices are physically available without aliasing");
                Refuse(()=>new ConstructionJetObservation(services,new CompiledCraftControl(reload).Resolve(default),fuel,power),"foreign row from equal-size rebuild");
                var evolution=services.Advance(fuel,power,15625,command.Consumers.AsSpan());Need(evolution.Phases.Any(p=>p.Active.Any(x=>x)),"real service output");
                foreach(var resource in c.Fuel.Stores.Select(s=>s.Resource).Distinct()){
                    BigInteger dn=0,dd=1;for(var s=0;s<c.Fuel.Stores.Length;s++)if(c.Fuel.Stores[s].Resource==resource){var a=fuel.InQ(s);var b=evolution.Fuel.InQ(s);dn=dn*a.Denominator*b.Denominator+(a.Numerator*b.Denominator-b.Numerator*a.Denominator)*dd;dd*=a.Denominator*b.Denominator;var gcd=BigInteger.GreatestCommonDivisor(dn,dd);dn/=gcd;dd/=gcd;}
                    BigInteger rn=0,rd=1;for(var i=0;i<c.Fuel.Consumers.Length;i++)if(command.Consumers[i]){var consumer=c.Fuel.Consumers[i];var sum=consumer.Terms.Aggregate(BigInteger.Zero,(n,t)=>n+t.Weight);foreach(var term in consumer.Terms.Where(t=>t.Resource==resource)){rn=rn*sum+consumer.Rate*term.Weight*15625*rd;rd*=sum;var gcd=BigInteger.GreatestCommonDivisor(rn,rd);rn/=gcd;rd/=gcd;}}
                    Need(dn*rd==rn*dd,"independent exact mixture and resource conservation");
                }
                var motion=new AssemblyMotion(default,default,DoubleQuaternion.Identity,default);var response=AssemblyDynamics.Evaluate(control,motion,evolution,default,command);Need(response.Motion.AngularVelocityBody.LengthSquared>0,"actual physical attitude response");
                var empty=ConstructionFuelState.Create(c.Fuel,Enumerable.Repeat(BigInteger.Zero,c.Fuel.Stores.Length).ToImmutableArray(),1,c.Fuel.InitiallyPositive);
                Need(new ConstructionJetObservation(services,command,empty,power).IsEmpty&&!observation.IsEmpty,"starvation and retained old observation distinct");
                var battery=c.Power.Modules.FindIndex(m=>m.Role==ElectricalRole.Battery);var partial=ConstructionPowerState.Create(c.Power,power.Charge.SetItem(battery,ConstructionFuelNetwork.Decode(.001,true)),1,power.Active,power.Cursor,0);
                var outage=services.Advance(fuel,partial,15625,command.Consumers.AsSpan());Need(outage.Phases.Any(p=>p.Active.Any(x=>x))&&outage.Phases.Any(p=>p.Active.All(x=>!x)),"exact partial power interval includes coast tail");
                Need(new ConstructionJetObservation(services,command,outage.Fuel,outage.Power).IsEmpty,"unavailable power produces no endpoint output");
                var release=control.Resolve(default);Need(new ConstructionJetObservation(services,release,fuel,power).IsEmpty&&!observation.IsEmpty,"release/rebuild cannot mutate captured view");
                for(var warm=0;warm<1024;warm++){_=new ConstructionJetObservation(services,command,fuel,power);_=AssemblyActuation.Resolve(c,command.Consumers.AsSpan(),default);}
                var start=GC.GetAllocatedBytesForCurrentThread();for(var repeat=0;repeat<1024;repeat++){_=new ConstructionJetObservation(services,command,fuel,power);_=AssemblyActuation.Resolve(c,command.Consumers.AsSpan(),default);}Need(GC.GetAllocatedBytesForCurrentThread()==start,"warmed complete observation and wrench exactly0B");
            }
            rows.Add(Measure(library,d,c,count));Console.WriteLine($"RCS jets={count} function={c.Function} consumers={c.Fuel.Consumers.Length} massMaximum={c.Mass.MaximumMass} PASS");
        }
        foreach(var (tanks,tank) in new[]{(2,"nc.tank.long-2"),(3,"nc.tank.short-2")}){var c=RcsScalabilityFixture.Compile(catalog,RcsScalabilityFixture.Document(catalog,tanks,tank:tank));Need(c.Function,"larger stock actuator system remains functional");Need(CraftSupportPreparation.Prepare(new(c),c.InitialMass,new(0,-9.81,0)).MaximumObservedLoad<15000,"native vertical fixture fits unchanged foot ratings");}
        ServiceCapacity(catalog);
        var baseline=RcsScalabilityFixture.Compile(catalog,RcsScalabilityFixture.Document(catalog,1));var example=baseline.Actuators.First(a=>a.Model=="nc.actuator.attitude/1");
        foreach(var n in new[]{31,32,33,63,64,65,257}){
            var jets=Enumerable.Range(0,n).Select(i=>example with {Point=new(0,(i%2==0?1:-1)*(1+(i%7)*Math.ScaleB(1d,-52)),0),Axis=i%2==0?Double3.UnitZ:-Double3.UnitZ,Thrust=1,Key=new("synthetic",i.ToString())}).ToImmutableArray();CompareOracle(jets,baseline.Mass);
        }
        Need(catalog.Save().SequenceEqual(before),"stock six-part catalog unchanged");
        var report=new{judgment="PASS",checks,scope="CPU scalable compilation/services/physical wrench;8 stock jets correctly refuse missing-axis authority;96 uses explicitly distinct qualification half-capacity tank;288 compile/services only",rows};
        if(output is not null){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);File.WriteAllText(output,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"qualification-catalog.json"),half.Save());}
        Console.WriteLine($"RCS_SCALABILITY PASS checks={checks}");
    }
    private static void ServiceCapacity(AssemblyDefinitionCatalog catalog)
    {
        var limit=(Array.MaxLength-256)/10-1;
        Need(ConstructionFuelNetwork.SnapshotCapacity(limit,20,true)<=Array.MaxLength,"derived fuel serialization boundary");
        Refuse(()=>ConstructionFuelNetwork.SnapshotCapacity(limit+1,20,true),"first unrepresentable fuel snapshot");
        limit=(Array.MaxLength-256-8)/14;
        Need(ConstructionPowerNetwork.SnapshotCapacity(limit,5,0,true)<=Array.MaxLength,"derived power serialization boundary");
        Refuse(()=>ConstructionPowerNetwork.SnapshotCapacity(limit+1,5,0,true),"first unrepresentable power snapshot");
        // Cross all old static fuel counts together. No contact/flight claim.
        var many=RcsScalabilityFixture.Compile(catalog,RcsScalabilityFixture.Document(catalog,129,1));
        Need(many.Fuel.Stores.Length==258&&many.Fuel.Consumers.Length==517&&many.Fuel.Consumers.Sum(c=>c.Terms.Length)==1034,"generic fuel has no replacement256/1024 cap");
        var saved=many.Fuel.Initial().Save();Need(ConstructionFuelState.Load(many.Fuel,saved).Save().SequenceEqual(saved),"more-than256 stores roundtrip under derived byte admission");
        var core=catalog.Data.Definitions.Single(d=>d.Id=="nc.core.command-2");
        var added=Enumerable.Range(0,1100).Select(i=>new ElectricalDefinition("test-battery-"+i,ElectricalRole.Battery,1,0,null)).ToImmutableArray();
        var changed=core with {Construction=core.Construction! with {Electrical=core.Construction.Electrical.AddRange(added)},Standard=core.Standard! with {
            Ports=core.Standard.Ports.AddRange(added.Select(m=>new PartServicePort(m.Id+".power",ConstructionService.Electricity,null,null,null,null,m.Id,false))),
            Routes=core.Standard.Routes.AddRange(added.Select(m=>new PartInternalRoute(m.Id+".route",m.Id+".power","battery.power",true)))}};
        var library=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d.Id==core.Id?changed:d).ToImmutableArray()});
        var craft=RcsScalabilityFixture.Compile(library,RcsScalabilityFixture.Document(library,1));var power=craft.Power.Initial();
        var cursor=power.Cursor.SetItem(craft.Power.Modules.First(m=>m.Role==ElectricalRole.Battery).Bus,1100);
        power=ConstructionPowerState.Create(craft.Power,power.Charge,1,power.Active,cursor,0);saved=power.Save();
        Need(craft.Power.Modules.Length>512&&saved.Length<=craft.Power.MaximumSnapshotBytes&&ConstructionPowerState.Load(craft.Power,saved).Save().SequenceEqual(saved),"generic modules beyond512 and four-digit battery cursor roundtrip");
        Refuse(()=>new ConstructionPhysicalServices(craft),"generic multi-battery topology still obeys separate physical profile");
    }
    private static object Distribution(List<double> values){values.Sort();double P(double p)=>values[(int)Math.Ceiling(p*values.Count)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=values[^1]};}
    private static object Measure(AssemblyDefinitionCatalog catalog,CompiledConstructionDesign source,CompiledCraft craft,int jets)
    {
        var cold=new List<double>();var bytes=new List<double>();for(var i=0;i<18;i++){var b=GC.GetAllocatedBytesForCurrentThread();var t=Stopwatch.GetTimestamp();var c=RcsScalabilityFixture.Compile(catalog,source);var ms=Stopwatch.GetElapsedTime(t).TotalMilliseconds;var allocated=GC.GetAllocatedBytesForCurrentThread()-b;Need(c.Digest==craft.Digest,"measured rebuild identity");if(i>=2){cold.Add(ms);bytes.Add(allocated);}}
        var service=new List<double>();var serviceBytes=new List<double>();var observer=new List<double>();
        if(craft.Function){var controls=new CompiledCraftControl(craft);var profile=new ConstructionPhysicalServices(craft);var row=controls.Resolve(new(false,new(1,1,1)));var fuel=craft.Fuel.Initial();var power=craft.Power.Initial();
            for(var i=0;i<260;i++){var b=GC.GetAllocatedBytesForCurrentThread();var t=Stopwatch.GetTimestamp();_=profile.Advance(fuel,power,15625,row.Consumers.AsSpan());var ms=Stopwatch.GetElapsedTime(t).TotalMilliseconds;var allocation=GC.GetAllocatedBytesForCurrentThread()-b;if(i>=4){service.Add(ms);serviceBytes.Add(allocation);}}
            for(var window=0;window<32;window++){var t=Stopwatch.GetTimestamp();for(var i=0;i<128;i++)_=new ConstructionJetObservation(profile,row,fuel,power);observer.Add(Stopwatch.GetElapsedTime(t).TotalMilliseconds/128);}}
        else{var fuel=craft.Fuel.Initial();var request=craft.Actuators.Select(a=>a.Model=="nc.actuator.attitude/1").ToArray();
            for(var i=0;i<260;i++){var b=GC.GetAllocatedBytesForCurrentThread();var t=Stopwatch.GetTimestamp();_=ConstructionFuelSolver.Advance(fuel,15625,request);_=AssemblyActuation.Resolve(craft,request,default);var ms=Stopwatch.GetElapsedTime(t).TotalMilliseconds;var allocation=GC.GetAllocatedBytesForCurrentThread()-b;if(i>=4){service.Add(ms);serviceBytes.Add(allocation);}}}
        var retainedFactors=craft.Fuel.Consumers.SelectMany(c=>c.Terms).Sum(t=>(t.ShareNumerator.GetBitLength()+7)/8);
        var retained=new List<double>();for(var repeat=0;repeat<3;repeat++)retained.Add(Retained(catalog,source));
        return new{jets,coldMs=Distribution(cold),coldAllocatedBytes=Distribution(bytes),serviceMs=Distribution(service),serviceAllocatedBytes=Distribution(serviceBytes),observerMs=observer.Count==0?null:Distribution(observer),retainedHeapBytesPerGraph=retained,selectionPayloadBytes=craft.Allocation.AxisJets.Sum(a=>a.Length)*sizeof(int),exactSharingMagnitudeBytes=retainedFactors,preparedConsumerRowPayloadBytes=craft.Function?54L*craft.Fuel.Consumers.Length:0,scope="normal GC CPU timing;8 measures exact fuel plus wrench, others physical service; separate forced-GC retained graph deltas; no GPU timing; cold report-only"};
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static double Retained(AssemblyDefinitionCatalog catalog,CompiledConstructionDesign source)
    {
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var before=GC.GetTotalMemory(true);var retained=new object[3];
        for(var i=0;i<retained.Length;i++){var craft=RcsScalabilityFixture.Compile(catalog,source);retained[i]=new object?[]{craft,craft.Function?new CompiledCraftControl(craft):null,craft.Function?new ConstructionPhysicalServices(craft):null,craft.Fuel.Initial(),craft.Power.Initial()};}
        var delta=GC.GetTotalMemory(true)-before;GC.KeepAlive(retained);return delta/3d;
    }
}
