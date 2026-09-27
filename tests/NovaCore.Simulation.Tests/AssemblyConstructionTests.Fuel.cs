using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private static PartDefinitionData ProofPart(string id,ImmutableArray<StoreData> stores,ImmutableArray<ConsumerDefinition> consumers,
        ImmutableArray<AttachmentData> attachments=default,ImmutableArray<InterfaceCapability> interfaces=default)
    {
        var seed=Development().Data.Definitions[0];
        return seed with {Id=id,DryMassKg=1,LocalCom=Double3.Zero,LocalInertia=Matrix3.Identity,Stores=stores,Attachments=attachments.IsDefault?[]:attachments,
            Construction=seed.Construction! with {Name=id,MassRegions=[new("mass",1,Double3.Zero,Matrix3.Identity)],
                StoreGeometry=stores.Select(s=>new StoreGeometryData(s.Id,s.CapacityKg/1000,Matrix3.Identity)).ToImmutableArray(),
                Consumers=consumers,Interfaces=interfaces.IsDefault?[]:interfaces,Subparts=[],Electrical=[],UnqualifiedHardware=[],Command=true}};
    }
    private static ConsumerDefinition Consumer(string id,double rate,ImmutableArray<MixtureTerm> mixture,ImmutableArray<string> stores,ImmutableArray<string> ports=default,
        ConstructionFlowRule rule=ConstructionFlowRule.NearestFirst)=>new(id,null,"proof",rate,mixture,stores,ports.IsDefault?[]:ports,rule,Double3.Zero,new(1,0,0),1,null);
    private static ConstructionFuelNetwork LocalFuel((string Id,string Species,double Amount,bool Enabled)[] tanks,ConsumerDefinition[] consumers)
    {
        var part=ProofPart("proof.local",tanks.Select(t=>new StoreData(t.Id,t.Species,t.Species,Double3.Zero,Math.Max(1,t.Amount))).ToImmutableArray(),consumers.ToImmutableArray());
        var species=tanks.Select(t=>t.Species).Concat(consumers.SelectMany(c=>c.Mixture.Select(m=>m.Resource))).Distinct().Order(StringComparer.Ordinal).Select(s=>new ResourceTypeData(s,1,s)).ToImmutableArray();
        var catalog=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.Schema,species,[part]));
        var instance=new PartInstanceData("root",0,catalog.Reference(catalog.Data.Definitions[0]),new(Double3.Zero,Matrix3.Identity));
        var design=new ConstructionDesignData(CompiledConstructionDesign.Schema,"proof.local",1,catalog.DependencyDigest([instance]),"root","root",[instance],[],[],
            [new("root",tanks.Select(t=>new ConstructionStoreInitial(t.Id,t.Amount,t.Enabled)).ToImmutableArray(),[])],[],[]);
        return ConstructionFuelNetwork.Compile(CompiledConstructionDesign.Compile(catalog,design));
    }
    private static void Quantity(ConstructionFuelState state,string id,int numerator,int denominator=1)
    {
        var i=state.Network.Stores.FindIndex(s=>s.Key.Store==id);var q=state.InQ(i);
        Check(q.Numerator*denominator==ConstructionFuelNetwork.Decode(1,true)*numerator*q.Denominator,"exact quantity "+id);
    }
    internal static void Fuel()
    {
        checks=0;
        var network=LocalFuel([("fuel","F",1,true),("oxidizer","O",1,true)],
            [Consumer("mixture",5,[new("F",2),new("O",3)],["fuel","oxidizer"])]);
        var initial=network.Initial();var result=ConstructionFuelSolver.Advance(initial,1_000_000,[true]);
        Quantity(result.State,"fuel",1,3);Quantity(result.State,"oxidizer",0);
        Check(result.ActiveTicks[0]==ConstructionRatio.Create(1_000_000,3),"mixture stops exactly at first missing component");
        var saved=result.State.Save();var loaded=ConstructionFuelState.Load(network,saved);Check(saved.SequenceEqual(loaded.Save()),"rational state canonical reload");
        Check(ConstructionFuelSolver.Advance(loaded,1_000_000,[true]).State.Save().SequenceEqual(saved),"starved mixture preserves remaining component");
        var divided=ConstructionFuelSolver.Advance(ConstructionFuelSolver.Advance(initial,125_000,[true]).State,875_000,[true]);
        Check(divided.State.Save().SequenceEqual(saved),"interval partition including event gives identical whole snapshot");
        var idle=ConstructionFuelSolver.Advance(initial,1_000_000,[false]);Check(ReferenceEquals(idle.State,initial)&&idle.ActiveTicks.Length==1&&idle.ActiveTicks[0]==ConstructionRatio.Create(0,1),"no-demand no-copy with indexed zero activity");
        Check(ConstructionFuelSolver.Advance(initial,0,[true]).ActiveTicks==network.ZeroActivity,"zero-tick shared activity vector");
        Reject(()=>ConstructionFuelSolver.Advance(initial,-1,[false]),"negative idle ticks");
        Reject(()=>ConstructionFuelSolver.Advance(initial,1,[]),"wrong demand dimension");
        Reject(()=>ConstructionFuelState.Create(network,[network.Scale*ConstructionFuelNetwork.Decode(2,true),BigInteger.Zero],1,0),"inventory credit refused");
        var balanced=LocalFuel([("f0","F",1,true),("f1","F",1,true),("f2","F",1,true),("o","O",1,true)],
            [Consumer("mixture",2,[new("F",1),new("O",1)],["f0","f1","f2","o"])]);
        var b=ConstructionFuelSolver.Advance(balanced.Initial(),2_000_000,[true]).State;
        Quantity(b,"f0",2,3);Quantity(b,"f1",2,3);Quantity(b,"f2",2,3);Quantity(b,"o",0);
        var concurrent=LocalFuel([("a","A",1,true),("b","B",1,true),("c","C",1,true)],
            [Consumer("ab",2,[new("A",1),new("B",1)],["a","b"]),Consumer("ac",2,[new("A",1),new("C",1)],["a","c"]),Consumer("a",1,[new("A",1)],["a"])]);
        var cstate=ConstructionFuelSolver.Advance(concurrent.Initial(),1_000_000,[true,true,true]).State;
        Quantity(cstate,"a",0);Quantity(cstate,"b",2,3);Quantity(cstate,"c",2,3);
        var independent=LocalFuel([("f","F",1,true),("o","O",0,true),("x","X",2,true),("disabled","X",1,false)],
            [Consumer("blocked",2,[new("F",1),new("O",1)],["f","o"]),Consumer("other",1,[new("X",1)],["x","disabled"])]);
        var istate=ConstructionFuelSolver.Advance(independent.Initial(),1_000_000,[true,true]).State;
        Quantity(istate,"f",1);Quantity(istate,"x",1);Quantity(istate,"disabled",1);
        var endpoint=LocalFuel([("a","F",1,true),("b","F",2,true)],[Consumer("one",2,[new("F",1)],["a","b"])]);
        var whole=ConstructionFuelSolver.Advance(endpoint.Initial(),1_500_000,[true]).State;
        var split=ConstructionFuelSolver.Advance(ConstructionFuelSolver.Advance(endpoint.Initial(),1_000_000,[true]).State,500_000,[true]).State;
        Check(whole.Save().SequenceEqual(split.Save())&&whole.Events==2,"endpoint event and changed equal-share denominator");
        var small=ConstructionFuelSolver.Advance(endpoint.Initial(),1,[true]).State;
        Check(small.Denominator==1&&small.Events==0,"non-event command does not grow denominator");
        FuelGraphProof();FuelBoundaryProof();FuelOracleProof();
        var dlv=ConstructionFuelNetwork.Compile(StockDevelopment());
        Check(dlv.Stores.Length==36&&dlv.Consumers.Length==38,"DLV generic static resource binding");
        foreach(var consumer in dlv.Consumers)
        {
            var owner=consumer.Key.Part;var allowed=owner.StartsWith("Booster",StringComparison.Ordinal)?"Booster":owner.StartsWith("Upper",StringComparison.Ordinal)?"Upper":"Capsule";
            Check(consumer.Terms.SelectMany(t=>t.Stores).All(i=>dlv.Stores[i].Key.Part==allowed),"DLV boundaries prohibit cross-stage fuel");
        }
        Console.WriteLine($"Construction Stage 3 PASS: {checks} checks; exact oracle 400 cases; DLV graph only, no ignition");
    }
    private static ConstructionFuelNetwork Plumbing(bool blocked=false,bool hose=false,bool reverse=false,ConstructionFlowRule rule=ConstructionFlowRule.FarthestFirst,bool wrongInlet=false)
    {
        var service=ConstructionService.Propellant;
        var root=ProofPart("proof.engine",[new("hidden","F","F",Double3.Zero,7),new("local","F","F",Double3.Zero,1)],
            [Consumer("engine",1,[new("F",1)],["local"],["IN"],rule)],
            [new("IN","mate",new(Double3.Zero,Matrix3.Identity)),new("OTHER","mate",new(Double3.Zero,Matrix3.Identity))],[new("IN",service,true),new("OTHER",service,false)]);
        var tank=ProofPart("proof.tank",[new("tank","F","F",Double3.Zero,1)],[],
            [new("OUT","mate",new(Double3.Zero,Matrix3.Mate)),new("NEXT","mate",new(Double3.Zero,Matrix3.Identity))],[new("OUT",service,true),new("NEXT",service,true)]);
        var catalog=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.Schema,[new("F",1,"Fuel")],[root,tank]));
        var instances=new[]{("root",root), ("near",tank),("far",tank)}.Select((p,i)=>new PartInstanceData(p.Item1,i,catalog.Reference(catalog.Data.Definitions.Single(d=>d.Id==p.Item2.Id)),new(Double3.Zero,Matrix3.Identity))).ToImmutableArray();
        var data=new ConstructionDesignData(CompiledConstructionDesign.Schema,"proof.plumbing",1,catalog.DependencyDigest(instances),"root","root",instances,
            [new("root","IN","near","OUT",new("inlet",true,service)),new("near","NEXT","far","OUT",new("branch",true,service))],
            hose?[new("hose",reverse?"root":"far",reverse?"IN":"NEXT",reverse?"far":"root",reverse?"NEXT":wrongInlet?"OTHER":"IN",service,false)]:[],
            [new("root",[new("hidden",7,true),new("local",1,true)],[]),new("near",[new("tank",1,true)],[]),new("far",[new("tank",1,true)],[])],[],[]);
        return ConstructionFuelNetwork.Compile(CompiledConstructionDesign.Compile(catalog,data),blocked?["inlet"]:[]);
    }
    private static void FuelGraphProof()
    {
        var full=Plumbing();var result=ConstructionFuelSolver.Advance(full.Initial(),1_000_000,[true]).State;
        var far=full.Stores.FindIndex(s=>s.Key.Part=="far");var near=full.Stores.FindIndex(s=>s.Key.Part=="near");
        Check(result.Quantities[far]==0&&result.Quantities[near]>0,"farthest explicit graph level first");Quantity(result,"hidden",7);Quantity(result,"local",1);
        var promoted=ConstructionFuelSolver.Advance(result,1_000_000,[true]).State;Check(promoted.Quantities[near]==0,"depletion promotes next graph level");
        var nearest=Plumbing(rule:ConstructionFlowRule.NearestFirst);Quantity(ConstructionFuelSolver.Advance(nearest.Initial(),1_000_000,[true]).State,"local",0);
        var isolated=Plumbing(blocked:true);Check(isolated.Consumers[0].Terms[0].Stores.Length==1,"unavailable connection partitions eligible stores");
        var restored=Plumbing();Check(full.Digest==restored.Digest&&full.Initial().Save().SequenceEqual(restored.Initial().Save()),"recompiled restored topology exact identity");
        var bridged=Plumbing(blocked:true,hose:true);Check(bridged.Consumers[0].Terms[0].Stores.Length==3,"explicit hose bridges logically missing structural service");
        var wrongWay=Plumbing(blocked:true,hose:true,reverse:true);Check(wrongWay.Consumers[0].Terms[0].Stores.Length==1,"directed source-to-consumer flow, not reversed");
        var wrongPort=Plumbing(blocked:true,hose:true,wrongInlet:true);Check(wrongPort.Consumers[0].Terms[0].Stores.Length==1,"correct direction still refuses undeclared first inlet");
        var cycle=Plumbing(hose:true);Check(cycle.Consumers[0].Terms[0].Stores.Length==3,"cycle does not expose hidden local store or double-count repeated definition");
        var term=cycle.Consumers[0].Terms[0];Check(term.Levels.Count(l=>l==1)==2,"BFS minimizes hop distance before priority selection");
        Reject(()=>ConstructionFuelState.Load(isolated,result.Save()),"foreign topology state even when quantities would fit");
    }
    private readonly record struct OracleRational
    {
        internal BigInteger N {get;}internal BigInteger D {get;}
        internal OracleRational(BigInteger n,BigInteger d){var g=BigInteger.GreatestCommonDivisor(n,d);N=n/g;D=d/g;}
        internal static OracleRational From(long n)=>new(n,1);
        public static OracleRational operator +(OracleRational a,OracleRational b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
        public static OracleRational operator -(OracleRational a,OracleRational b)=>new(a.N*b.D-b.N*a.D,a.D*b.D);
        public static OracleRational operator *(OracleRational a,OracleRational b)=>new(a.N*b.N,a.D*b.D);
        public static OracleRational operator /(OracleRational a,OracleRational b)=>new(a.N*b.D,a.D*b.N);
        internal bool Less(OracleRational b)=>N*b.D<b.N*D;
    }
    private static void FuelBoundaryProof()
    {
        var partial=LocalFuel([("f","F",2,true),("o","O",1,true),("x","X",2,true)],
            [Consumer("fo",4,[new("F",1),new("O",3)],["f","o"]),Consumer("fx",2,[new("F",1),new("X",1)],["f","x"])]);
        var answer=ConstructionFuelSolver.Advance(partial.Initial(),1_000_000,[true,true]);
        Quantity(answer.State,"f",2,3);Quantity(answer.State,"o",0);Quantity(answer.State,"x",1);
        Check(answer.ActiveTicks[0]==ConstructionRatio.Create(1_000_000,3)&&answer.ActiveTicks[1]==ConstructionRatio.Create(1_000_000,1),"one consumer starves while another continues");
        var extreme=LocalFuel([("f","F",double.Epsilon,true),("o","O",double.MaxValue,true)],
            [Consumer("a",double.MaxValue,[new("F",uint.MaxValue),new("O",uint.MaxValue)],["f","o"])]);
        var e=ConstructionFuelSolver.Advance(extreme.Initial(),long.MaxValue,[true]).State;
        Check(e.Quantities[0]==0&&e.Quantities[1]>0&&e.Denominator.GetBitLength()<=1+e.Events*extreme.RateBits,"maximum binary64 rate and mismatched inventory width");
        Check(ConstructionFuelState.Load(extreme,e.Save()).Save().SequenceEqual(e.Save()),"extreme rational reload");
        var empty=LocalFuel([],[]);Check(ReferenceEquals(ConstructionFuelSolver.Advance(empty.Initial(),1,[]).State.Network,empty),"no-store no-consumer network");
        var missing=LocalFuel([("f","F",1,true)],[Consumer("noOx",2,[new("F",1),new("O",1)],["f"])]);
        Quantity(ConstructionFuelSolver.Advance(missing.Initial(),1_000_000,[true]).State,"f",1);
        var closed=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.G0B");
        foreach(var fuel in new[]{0d,1d/1024,2d})foreach(var ticks in new[]{1L,15625L})
        {
            var old=AssemblyResources.Calculate(closed,new(AssemblyResources.Mass(fuel),AssemblyResources.Mass(fuel*1.5)),AssemblyResources.Rate(1),ticks);
            var generic=LocalFuel([("f","F",fuel,true),("o","O",fuel*1.5,true)],[Consumer("srv-mixture",5,[new("F",2),new("O",3)],["f","o"])]);
            var evolved=ConstructionFuelSolver.Advance(generic.Initial(),ticks,[true]);
            static BigInteger FromOld(NovaCore.Simulation.Spacecraft.Resources.PropellantInteger value)=>BigInteger.Parse("0"+AssemblyResources.Hex(value),System.Globalization.NumberStyles.AllowHexSpecifier);
            Check(evolved.State.InQ(0)==ConstructionRatio.Create(FromOld(old.After.Fuel),1)&&evolved.State.InQ(1)==ConstructionRatio.Create(FromOld(old.After.Oxidizer),1),"matched SRV exact quantity projection");
            Check(evolved.ActiveTicks[0]==ConstructionRatio.Create(FromOld(old.Powered.Numerator),FromOld(old.Powered.Denominator)),"matched SRV exact powered duration");
        }
    }
    private static void FuelOracleProof()
    {
        // Independent per-store reduced-fraction solver, in kilograms/seconds; no Q/L/d/event recurrence.
        var random=new Random(73019);
        for(var test=0;test<400;test++)
        {
            var count=random.Next(2,7);var tanks=Enumerable.Range(0,count).Select(i=>("s"+i,i%2==0?"F":"O",(double)random.Next(0,6),true)).ToArray();
            var a=(uint)random.Next(1,8);var b=(uint)random.Next(1,8);var rate=random.Next(1,12);var duration=random.Next(1,9);
            var net=LocalFuel(tanks,[Consumer("mix",rate,[new("F",a),new("O",b)],tanks.Select(t=>t.Item1).ToImmutableArray())]);
            var amounts=tanks.Select(t=>OracleRational.From((long)t.Item3)).ToArray();var left=OracleRational.From(duration);
            while(left.N>0)
            {
                var f=Enumerable.Range(0,count).Where(i=>i%2==0&&amounts[i].N>0).ToArray();var o=Enumerable.Range(0,count).Where(i=>i%2!=0&&amounts[i].N>0).ToArray();
                if(f.Length==0||o.Length==0)break;
                var rates=Enumerable.Range(0,count).Select(i=>new OracleRational(rate*(i%2==0?a:b),(a+b)*(i%2==0?f.Length:o.Length))).ToArray();
                var positive=f.Concat(o).ToArray();var dt=left;
                foreach(var i in positive){var t=amounts[i]/rates[i];if(t.Less(dt))dt=t;}
                foreach(var i in positive)amounts[i]-=rates[i]*dt;left-=dt;
            }
            var answer=ConstructionFuelSolver.Advance(net.Initial(),duration*1_000_000L,[true]).State;
            var split=ConstructionFuelSolver.Advance(ConstructionFuelSolver.Advance(net.Initial(),333_333,[true]).State,duration*1_000_000L-333_333,[true]).State;
            Check(answer.Save().SequenceEqual(split.Save()),"oracle interval partition "+test);
            for(var i=0;i<count;i++)
            {var q=answer.InQ(i);Check(q.Numerator*amounts[i].D==ConstructionFuelNetwork.Decode(1,true)*amounts[i].N*q.Denominator,"fraction oracle "+test+"/"+i);}
        }
    }
}
