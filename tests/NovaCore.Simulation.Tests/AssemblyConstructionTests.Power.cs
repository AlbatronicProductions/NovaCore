using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private static ConstructionPowerNetwork LocalPower(ImmutableArray<ElectricalDefinition> modules,ImmutableArray<ConstructionElectricalInitial> initial,bool engine=false)
    {
        var fuel=engine?LocalFuel([("f","F",1,true),("o","O",1,true)],[Consumer("engine",5,[new("F",2),new("O",3)],["f","o"])]):LocalFuel([],[]);
        return WithPower(fuel,modules,initial);
    }
    private static ConstructionPowerNetwork WithPower(ConstructionFuelNetwork fuel,ImmutableArray<ElectricalDefinition> modules,ImmutableArray<ConstructionElectricalInitial> initial)
    {
        var d=fuel.Design;var part=d.Parts[0].Definition;part=part with {Construction=part.Construction! with {Electrical=modules}};
        var catalog=AssemblyDefinitionCatalog.Compile(d.Catalog.Data with {Definitions=[part]});
        var instance=d.Data.Instances[0] with {Definition=catalog.Reference(catalog.Data.Definitions[0])};
        var design=CompiledConstructionDesign.Compile(catalog,d.Data with {Instances=[instance],DependencyDigest=catalog.DependencyDigest([instance]),
            Configuration=[d.Data.Configuration[0] with {Electrical=initial}]});
        return ConstructionPowerNetwork.Compile(ConstructionFuelNetwork.Compile(design));
    }
    private static int Module(ConstructionPowerNetwork net,string id)=>net.Modules.FindIndex(m=>m.Key.Module==id);
    private static void Energy(ConstructionPowerState state,string id,int n,int d=1)
    {var value=state.InQ(Module(state.Network,id));Check(value.Numerator*d==ConstructionFuelNetwork.Decode(1,true)*n*value.Denominator,"exact joules "+id);}
    private static ConstructionPowerEvolution PowerStep(ConstructionPowerState power,long ticks,bool engine=false)
    {
        var fuel=power.Network.Fuel;return ConstructionPowerSolver.Advance(power,ticks,ConstructionFuelSolver.Advance(fuel.Initial(),ticks,Enumerable.Repeat(engine,fuel.Consumers.Length).ToArray()));
    }
    internal static void Power()
    {
        checks=0;
        var battery=new ElectricalDefinition("battery",ElectricalRole.Battery,10,0,null);var load=new ElectricalDefinition("load",ElectricalRole.Load,0,3,null);
        var solar=new ElectricalDefinition("solar",ElectricalRole.SolarGenerator,0,4,null);
        var network=LocalPower([battery,load],[new("battery",10,true),new("load",0,true)]);
        var powered=PowerStep(network.Initial(),2_000_000);Energy(powered.State,"battery",4);
        Check(powered.Delivered[Module(network,"load")]==ConstructionRatio.Create(ConstructionFuelNetwork.Decode(6,true),1),"battery supplies exact load energy");
        var noBattery=LocalPower([solar,load],[new("solar",0,true),new("load",0,true)]);var nb=PowerStep(noBattery.Initial(),1_000_000);
        Check(!nb.State.Active[Module(noBattery,"load")]&&nb.Delivered.All(r=>r.Numerator==0),"generator cannot bypass absent battery");
        var buffered=LocalPower([battery,load,solar],[new("battery",0,true),new("load",0,true),new("solar",0,true)]);
        Energy(PowerStep(buffered.Initial(),2_000_000).State,"battery",2);
        var capacity=LocalPower([battery,solar],[new("battery",9,true),new("solar",0,true)]);var capped=PowerStep(capacity.Initial(),1_000_000);
        Energy(capped.State,"battery",10);Check(capped.Delivered[Module(capacity,"solar")]==ConstructionRatio.Create(ConstructionFuelNetwork.Decode(1,true),1),"overflow generation spills instead of exceeding capacity");
        var shortfall=LocalPower([battery,load],[new("battery",1,true),new("load",0,true)]);var partial=PowerStep(shortfall.Initial(),1_000_000);
        Energy(partial.State,"battery",0);Check(partial.State.Active[Module(shortfall,"load")],"partial supply remains active current interval");
        var empty=PowerStep(partial.State,1_000_000);Check(!empty.State.Active[Module(shortfall,"load")],"zero supply at start disables ordinary load");
        var disabled=LocalPower([battery,load],[new("battery",10,false),new("load",0,true)]);var dc=PowerStep(disabled.Initial(),1_000_000);
        Energy(dc.State,"battery",10);Check(!dc.State.Active[Module(disabled,"load")],"disabled battery contributes no supply");
        var rotor=LocalPower([battery with {Id="b0"},battery with {Id="b1"},load],[new("b0",10,true),new("b1",10,true),new("load",0,true)]);
        var r0=PowerStep(rotor.Initial(),1_000_000);Energy(r0.State,"b0",7);Energy(r0.State,"b1",10);Check(r0.State.Cursor[0]==1,"cursor advances past finishing nonempty battery");
        var r1=PowerStep(r0.State,1_000_000);Energy(r1.State,"b0",7);Energy(r1.State,"b1",7);Check(r1.State.Cursor[0]==0,"next update uses next battery");
        var competition=LocalPower([battery,load with {Id="a",Watts=7},load with {Id="b",Watts=7}],
            [new("battery",10,true),new("a",0,true),new("b",0,true)]);
        var comp=PowerStep(competition.Initial(),1_000_000);
        Check(comp.Delivered[Module(competition,"a")]==ConstructionRatio.Create(ConstructionFuelNetwork.Decode(7,true),1)&&
            comp.Delivered[Module(competition,"b")]==ConstructionRatio.Create(ConstructionFuelNetwork.Decode(3,true),1)&&comp.State.Cursor[0]==0,"stable module order and shortfall cursor reset");
        var generator=new ElectricalDefinition("generator",ElectricalRole.EngineGenerator,0,2,"engine");
        var phaseOrder=LocalPower([battery with {Id="b0",CapacityJ=5},battery with {Id="b1",CapacityJ=5},generator with {Id="zEngine",Watts=6},solar with {Id="aSolar"},load],
            [new("b0",0,true),new("b1",0,true),new("zEngine",0,true),new("aSolar",0,true),new("load",0,true)],true);
        var phases=PowerStep(phaseOrder.Initial(),1_000_000,true);Energy(phases.State,"b0",0);Energy(phases.State,"b1",3);
        Check(phases.State.Cursor[0]==0,"engine generator phase precedes lexically earlier solar and then load");
        var driven=LocalPower([battery,generator],[new("battery",0,true),new("generator",0,true)],true);
        var drivenFuel=ConstructionFuelSolver.Advance(driven.Fuel.Initial(),1_000_000,[true]);var generated=ConstructionPowerSolver.Advance(driven.Initial(),1_000_000,drivenFuel);
        Energy(generated.State,"battery",2,3);Check(generated.State.Denominator>1,"partial fuel availability drives rational generator energy");
        var saved=generated.State.Save();var reload=ConstructionPowerState.Load(driven,saved);Check(saved.SequenceEqual(reload.Save()),"exact power save/reload");
        var after=ConstructionPowerSolver.Advance(reload,1_000_000,ConstructionFuelSolver.Advance(drivenFuel.State,1_000_000,[true]));Energy(after.State,"battery",2,3);
        Reject(()=>ConstructionPowerState.Load(rotor,saved),"foreign power network snapshot");
        Reject(()=>ConstructionPowerSolver.Advance(driven.Initial(),1_000_000,ConstructionFuelSolver.Advance(network.Fuel.Initial(),1_000_000,[])),"foreign fuel activity");
        Reject(()=>ConstructionPowerSolver.Advance(driven.Initial(),1_000_000,drivenFuel with {ActiveTicks=[new(1,0)]}),"zero duration denominator");
        Reject(()=>ConstructionPowerSolver.Advance(driven.Initial(),1_000_000,drivenFuel with {ActiveTicks=[new(1_000_001,1)]}),"generator duration exceeds interval");
        Reject(()=>ConstructionPowerSolver.Advance(driven.Initial(),-1,drivenFuel),"negative power interval");
        var validSave=AssemblyJson.Read<ConstructionPowerSave>(saved);
        Reject(()=>ConstructionPowerState.Load(driven,AssemblyJson.Write(validSave with {Denominator="-1"})),"negative encoded charge denominator");
        Reject(()=>ConstructionPowerState.Load(driven,AssemblyJson.Write(validSave with {Cursor=[999]})),"battery cursor bounds");
        Reject(()=>ConstructionPowerState.Load(driven,AssemblyJson.Write(validSave with {Active=validSave.Active.SetItem(Module(driven,"battery"),false)})),"snapshot cannot alter immutable battery enablement");
        var boundaryBits=driven.Fuel.RateBits*3/5;var d1=(BigInteger.One<<boundaryBits)-1;var d2=(BigInteger.One<<boundaryBits)+1;
        var unrelatedFuel=ConstructionFuelState.Create(driven.Fuel,[BigInteger.Zero,BigInteger.One],d1,1);
        var charges=driven.Initial().Charge.SetItem(Module(driven,"battery"),BigInteger.One);
        var unrelatedEnergy=ConstructionPowerState.Create(driven,charges,d2,driven.Initial().Active,driven.Initial().Cursor,1);
        var unchanged=unrelatedEnergy.Save();
        Reject(()=>ConstructionPowerSolver.Advance(unrelatedEnergy,1,new(unrelatedFuel,driven.Fuel.ZeroActivity)),"individually bounded denominators exceed joint lifetime bound");
        Check(unchanged.SequenceEqual(unrelatedEnergy.Save()),"joint bound refusal leaves source unchanged");
        var retry=LocalPower([battery,generator,load],[new("battery",0,true),new("generator",0,true),new("load",0,true)],true);
        var off=PowerStep(retry.Initial(),1_000_000);Check(!off.State.Active[Module(retry,"load")],"load disables on initially empty engine-off bus");
        var recovered=PowerStep(off.State,1_000_000,true);Energy(recovered.State,"battery",2,3);Check(!recovered.State.Active[Module(retry,"load")],"new generation does not secretly reactivate ordinary load");
        var reactivated=recovered.State.SetLoadActive(Module(retry,"load"),true);
        var actual=ConstructionPowerSolver.Advance(reactivated,1_000_000,ConstructionFuelSolver.Advance(ConstructionFuelSolver.Advance(retry.Fuel.Initial(),1_000_000,[true]).State,1_000_000,[false]));
        Energy(actual.State,"battery",0);Check(actual.State.Active[Module(retry,"load")],"explicit reactivation receives remaining partial energy");
        PowerGraphProof();PowerWidthProof();
        var dlv=ConstructionPowerNetwork.Compile(ConstructionFuelNetwork.Compile(StockDevelopment()));
        Check(dlv.Modules.Length==0&&dlv.Power.Count==9&&dlv.Data.Count==1&&dlv.Fuel.Design.Parts.All(p=>dlv.CanCommand(dlv.Fuel.Design.Index(p.Instance.Id))),"DLV unparameterized power remains absent; data spans attached article");
        Console.WriteLine($"Construction Stage 4 PASS: {checks} checks; power/data independent; no DLV electrical admission");
    }
    private static ConstructionPowerNetwork PowerPair(bool crossPower,bool blocked=false,bool crossData=true)
    {
        var caps=ConstructionService.Electricity|ConstructionService.Data;
        var a=ProofPart("proof.source",[],[],[new("a","mate",new(Double3.Zero,Matrix3.Identity))],[new("a",caps,true)]);
        a=a with {Construction=a.Construction! with {Electrical=[new("battery",ElectricalRole.Battery,10,0,null)]}};
        var b=ProofPart("proof.load",[],[],[new("b","mate",new(Double3.Zero,Matrix3.Mate))],[new("b",caps,true)]);
        b=b with {Construction=b.Construction! with {Command=false,Electrical=[new("load",ElectricalRole.Load,0,2,null)]}};
        var catalog=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.Schema,[],[a,b]));
        var instances=new[]{a,b}.Select((p,i)=>new PartInstanceData(i==0?"source":"load",i,catalog.Reference(catalog.Data.Definitions.Single(d=>d.Id==p.Id)),new(Double3.Zero,Matrix3.Identity))).ToImmutableArray();
        var services=(crossPower?ConstructionService.Electricity:ConstructionService.None)|(crossData?ConstructionService.Data:ConstructionService.None);
        var data=new ConstructionDesignData(CompiledConstructionDesign.Schema,"proof.bus",1,catalog.DependencyDigest(instances),"source","source",instances,
            [new("source","a","load","b",new("wire",true,services))],[],[new("source",[],[new("battery",10,true)]),new("load",[],[new("load",0,true)])],[],[]);
        var design=CompiledConstructionDesign.Compile(catalog,data);return ConstructionPowerNetwork.Compile(ConstructionFuelNetwork.Compile(design),blocked?["wire"]:[]);
    }
    private static void PowerGraphProof()
    {
        var yes=PowerPair(true);var powered=PowerStep(yes.Initial(),1_000_000);Energy(powered.State,"battery",8);Check(yes.CanCommand(1),"explicit power and data opt in");
        var no=PowerPair(false);var isolated=PowerStep(no.Initial(),1_000_000);Energy(isolated.State,"battery",10);Check(no.CanCommand(1)&&!isolated.State.Active[Module(no,"load")],"data does not imply power");
        var dataOff=PowerPair(true,crossData:false);Check(!dataOff.CanCommand(1)&&PowerStep(dataOff.Initial(),1_000_000).State.Active[Module(dataOff,"load")],"power does not imply command data");
        var blocked=PowerPair(true,blocked:true);Energy(PowerStep(blocked.Initial(),1_000_000).State,"battery",10);Check(!blocked.CanCommand(1),"logical service partition has no physical separation");
    }
}
