using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using NovaCore.Simulation.Spacecraft.Resources;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct ConstructionStoreKey(string Part,string Store);
internal readonly record struct ConstructionConsumerKey(string Part,string Consumer);
internal sealed record ConstructionFuelStore(ConstructionStoreKey Key,string Resource,BigInteger Capacity,BigInteger Initial,bool Enabled,int Part);
internal sealed record ConstructionFuelTerm(string Resource,uint Weight,ImmutableArray<int> Stores,ImmutableArray<int> Levels,ImmutableArray<BigInteger> ShareFactors,BigInteger ShareNumerator=default)
{
    // The prepared lattice is divisible by every reachable supplier count.
    // Modular terms retain one exact numerator rather than duplicating S+1
    // large integers for every mixture term. Legacy tables keep their identity.
    internal BigInteger ShareFactor(int suppliers)
    {
        Require(suppliers>0&&suppliers<=Stores.Length,"Invalid selected supplier count.");
        return ShareFactors.IsDefault?ShareNumerator/suppliers:ShareFactors[suppliers];
    }
}
internal sealed record ConstructionFuelConsumer(ConstructionConsumerKey Key,BigInteger Rate,ConstructionFlowRule Rule,ImmutableArray<ConstructionFuelTerm> Terms);
internal readonly record struct ConstructionRatio(BigInteger Numerator,BigInteger Denominator)
{
    internal static ConstructionRatio Create(BigInteger n,BigInteger d)
    {Require(n>=0&&d>0,"Invalid unsigned rational.");var g=BigInteger.GreatestCommonDivisor(n,d);return new(n/g,d/g);}
}

/// <summary>Cold immutable plumbing facts. No per-frame graph traversal or mesh dependency.</summary>
internal sealed class ConstructionFuelNetwork
{
    internal const int MaximumStores=256,MaximumConsumers=256,MaximumMixtureTerms=1024,MaximumArithmeticBits=1_000_000;
    internal CompiledConstructionDesign Design {get;}
    internal CompiledCraftPorts? Ports {get;}
    internal string Digest {get;}
    internal ImmutableArray<ConstructionFuelStore> Stores {get;}
    internal ImmutableArray<ConstructionFuelConsumer> Consumers {get;}
    internal BigInteger Scale {get;}
    // Physical continuous-power event denominators divide this immutable lattice.
    // Legacy static networks retain one and their original arithmetic.
    internal BigInteger TimeScale {get;}
    internal int TimeNumeratorBits {get;}
    internal int RateBits {get;}
    internal int QuantityBits {get;}
    internal int ScratchBits {get;}
    internal int ScratchSignedBits=>checked(ScratchBits+1);
    internal int MaximumSnapshotBytes {get;}
    internal static int SnapshotCapacity(int stores,int quantityBits,bool modular)
    {
        Require(stores>=0&&quantityBits>=0,"Invalid fuel snapshot dimensions.");
        var bytes=modular?checked(256L+((long)stores+1)*(quantityBits/4L+5)):
            checked(1024L+((long)stores+1)*(quantityBits/4L+2));
        Require(bytes<=Array.MaxLength,"Fuel snapshot exceeds the CLR byte-array representation.");return (int)bytes;
    }
    internal int InitiallyPositive {get;}
    internal ImmutableArray<ConstructionRatio> ZeroActivity {get;}
    private ConstructionFuelNetwork(CompiledConstructionDesign design,string digest,ImmutableArray<ConstructionFuelStore> stores,
        ImmutableArray<ConstructionFuelConsumer> consumers,BigInteger scale,CompiledCraftPorts? ports=null,BigInteger timeScale=default)
    {
        Design=design;Digest=digest;Stores=stores;Consumers=consumers;Scale=scale;Ports=ports;TimeScale=timeScale.IsZero?BigInteger.One:timeScale;
        TimeNumeratorBits=ports is null?63:checked(63+(int)TimeScale.GetBitLength());
        ZeroActivity=Enumerable.Repeat(new ConstructionRatio(BigInteger.Zero,BigInteger.One),consumers.Length).ToImmutableArray();
        InitiallyPositive=stores.Count(s=>s.Initial>0);
        var maximumRate=BigInteger.Max(BigInteger.One,scale*consumers.Aggregate(BigInteger.Zero,(s,c)=>s+c.Rate));
        RateBits=checked((int)maximumRate.GetBitLength());
        var denominatorBits=checked(1+InitiallyPositive*RateBits);
        QuantityBits=checked((int)(stores.Length==0?0:stores.Max(s=>s.Capacity.GetBitLength()))+(int)scale.GetBitLength()+denominatorBits);
        ScratchBits=checked(Math.Max(QuantityBits+RateBits+1,TimeNumeratorBits+1+denominatorBits+RateBits));
        // Load constructs a signed hexadecimal value before canonical/sign
        // refusal. Incomplete craft networks can have tiny rates, making this
        // parser envelope larger than every solver expression.
        if(ports is not null)ScratchBits=Math.Max(ScratchBits,checked(4*(QuantityBits/4+2)));
        Require(ScratchBits<=MaximumArithmeticBits,"Fuel arithmetic admission width exceeded.");
        MaximumSnapshotBytes=SnapshotCapacity(stores.Length,QuantityBits,ports is not null);
    }
    internal static BigInteger Decode(double value,bool kilograms)
    {
        PropellantInteger exact;
        Require(kilograms?PropellantInteger.TryFromKilograms(value,out exact):PropellantInteger.TryDecodeFlow(value,out exact),"Invalid exact quantity/rate.");
        Span<byte> bytes=stackalloc byte[PropellantInteger.LimbCount*8];
        for(var i=0;i<PropellantInteger.LimbCount;i++)System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes[(i*8)..],exact.Limb(i));
        return new(bytes,isUnsigned:true,isBigEndian:false);
    }
    private static BigInteger Lcm(BigInteger a,BigInteger b)=>a/BigInteger.GreatestCommonDivisor(a,b)*b;
    // unavailableEdges is a cold graph-proof input only. It does not detach parts or publish successors.
    internal static ConstructionFuelNetwork Compile(CompiledConstructionDesign design,ImmutableArray<string> unavailableEdges=default,CompiledCraftPorts? ports=null)
    {
        Require(design.Data.Schema==CompiledConstructionDesign.CraftSchema?ports is not null&&ReferenceEquals(ports.Design,design):ports is null,"CraftDocument requires qualified explicit-port service compilation.");
        if(unavailableEdges.IsDefault)unavailableEdges=[];
        Require(ports is null||unavailableEdges.IsEmpty,"Craft port topology cannot be altered through legacy edge overrides.");
        Unique(unavailableEdges,x=>x,"unavailable service edges");
        foreach(var id in unavailableEdges)Require(design.ServiceEdges.Any(e=>e.Id==id),"Unknown unavailable edge.");
        Require(ports is not null||design.Parts.Sum(p=>p.Definition.Stores.Length)<=MaximumStores&&
            design.Parts.Sum(p=>p.Definition.Construction!.Consumers.Length)<=MaximumConsumers&&
            design.Parts.Sum(p=>p.Definition.Construction!.Consumers.Sum(c=>c.Mixture.Length))<=MaximumMixtureTerms,"Fuel network admission capacity exceeded.");
        var stores=ImmutableArray.CreateBuilder<ConstructionFuelStore>();
        var definitions=new List<(int Part,ConsumerDefinition Definition)>();
        for(var p=0;p<design.Parts.Length;p++)
        {
            var part=design.Parts[p];var configuration=design.Data.Configuration.Single(x=>x.Part==part.Instance.Id);
            foreach(var store in part.Definition.Stores)
            {var initial=configuration.Stores.Single(s=>s.Store==store.Id);stores.Add(new(new(part.Instance.Id,store.Id),store.ResourceIdentity,Decode(store.CapacityKg,true),Decode(initial.QuantityKg,true),initial.Enabled,p));}
            foreach(var consumer in part.Definition.Construction!.Consumers)definitions.Add((p,consumer));
        }
        Require(ports is not null||stores.Count<=MaximumStores&&definitions.Count<=MaximumConsumers,"Fuel network admission capacity exceeded.");
        var scale=BigInteger.One;
        foreach(var (_,c) in definitions){var sum=c.Mixture.Aggregate(BigInteger.Zero,(v,t)=>v+t.Weight);
            if(ports is not null)Require(scale.GetBitLength()+sum.GetBitLength()<=MaximumArithmeticBits,"Physical mixture lattice admission width exceeded.");
            scale=Lcm(scale,sum);}
        var sharing=BigInteger.One;for(var n=1;n<=stores.Count;n++){
            if(ports is not null)Require(sharing.GetBitLength()+new BigInteger(n).GetBitLength()<=MaximumArithmeticBits,"Physical sharing lattice admission width exceeded.");
            sharing=Lcm(sharing,n);}
        if(ports is not null)Require(scale.GetBitLength()+sharing.GetBitLength()<=MaximumArithmeticBits,"Physical sharing scale admission width exceeded.");
        scale*=sharing;
        var timeScale=BigInteger.One;
        if(ports is not null){
            var power=ConstructionServiceNetwork.Compile(design,ConstructionService.Electricity,ports:ports);
            var rates=new BigInteger[power.Count];
            for(var p=0;p<design.Parts.Length;p++){
                var part=design.Parts[p];var configuration=design.Data.Configuration.Single(c=>c.Part==part.Instance.Id);
                foreach(var load in part.Definition.Construction!.Electrical.Where(e=>e.Role==ElectricalRole.Load))
                    if(configuration.Electrical.Single(c=>c.Module==load.Id).Enabled)
                        rates[ports.ElectricalBus(power.Component,p,load.Id)]+=Decode(load.Watts,false);
            }
            foreach(var rate in rates)if(rate>0){
                // Bound each LCM input/product before the next composition.
                Require(timeScale.GetBitLength()+rate.GetBitLength()<=MaximumArithmeticBits,"Physical time lattice admission width exceeded.");
                timeScale=Lcm(timeScale,rate);
            }
            Require(scale.GetBitLength()+timeScale.GetBitLength()<=MaximumArithmeticBits,"Physical fuel scale construction width exceeded.");
            scale*=timeScale;
        }
        // Prove temporary width before allocating the per-mixture sharing tables.
        var totalRate=definitions.Aggregate(BigInteger.Zero,(s,c)=>s+Decode(c.Definition.TotalFlowKgS,false));
        if(ports is not null)Require(scale.GetBitLength()+totalRate.GetBitLength()<=MaximumArithmeticBits,"Physical fuel rate construction width exceeded.");
        var rateBits=BigInteger.Max(BigInteger.One,scale*totalRate).GetBitLength();
        var denominatorBits=1+stores.Count(s=>s.Initial>0)*rateBits;
        var quantityBits=(stores.Count==0?0:stores.Max(s=>s.Capacity.GetBitLength()))+scale.GetBitLength()+denominatorBits;
        var timeBits=ports is null?63:63+timeScale.GetBitLength();
        var scratch=Math.Max(quantityBits+rateBits+1,timeBits+1+denominatorBits+rateBits);
        if(ports is not null)scratch=Math.Max(scratch,4*(quantityBits/4+2));
        Require(scratch<=MaximumArithmeticBits,"Fuel arithmetic admission width exceeded.");
        // Reverse adjacency: traverse from consumer towards a permitted supplier.
        var incoming=Enumerable.Range(0,design.Parts.Length).Select(_=>new List<(int From,string Interface)>()).ToArray();
        foreach(var edge in design.ServiceEdges)
        {
            if(!edge.Services.HasFlag(ConstructionService.Propellant)||unavailableEdges.Contains(edge.Id))continue;
            incoming[edge.Receiver].Add((edge.Supply,edge.ReceiverInterface));
            if(edge.Bidirectional)incoming[edge.Supply].Add((edge.Receiver,edge.SupplyInterface));
        }
        var compiled=ImmutableArray.CreateBuilder<ConstructionFuelConsumer>();
        foreach(var (owner,c) in definitions)
        {
            var distance=Enumerable.Repeat(-1,design.Parts.Length).ToArray();distance[owner]=0;var queue=new Queue<int>();
            foreach(var edge in incoming[owner])if(c.FeedInterfaces.Contains(edge.Interface)&&distance[edge.From]<0){distance[edge.From]=1;queue.Enqueue(edge.From);}
            while(queue.TryDequeue(out var current))foreach(var edge in incoming[current])if(distance[edge.From]<0){distance[edge.From]=distance[current]+1;queue.Enqueue(edge.From);}
            var terms=ImmutableArray.CreateBuilder<ConstructionFuelTerm>();var sum=c.Mixture.Aggregate(BigInteger.Zero,(v,t)=>v+t.Weight);
            foreach(var term in c.Mixture)
            {
                var explicitDistance=ports?.SupplierDistances(owner,p=>p.Consumer==c.Id&&p.Service==ConstructionService.Propellant&&p.Resource==term.Resource);
                int Level(int i)=>ports is null?distance[stores[i].Part]:ports.StoreDistance(explicitDistance!.Value,stores[i].Part,stores[i].Key.Store);
                var eligible=Enumerable.Range(0,stores.Count).Where(i=>stores[i].Resource==term.Resource&&
                    (ports is not null?Level(i)!=int.MaxValue:stores[i].Part==owner?c.FeedStores.Contains(stores[i].Key.Store):distance[stores[i].Part]>=0)).ToImmutableArray();
                if(ports is not null)Require(scale.GetBitLength()+32<=MaximumArithmeticBits,"Physical mixture construction width exceeded.");
                if(ports is not null)terms.Add(new(term.Resource,term.Weight,eligible,eligible.Select(Level).ToImmutableArray(),default,scale*term.Weight/sum));
                else{
                    var factors=ImmutableArray.CreateBuilder<BigInteger>(stores.Count+1);factors.Add(0);
                    for(var n=1;n<=stores.Count;n++)factors.Add(scale*term.Weight/(sum*n));
                    terms.Add(new(term.Resource,term.Weight,eligible,eligible.Select(Level).ToImmutableArray(),factors.MoveToImmutable()));
                }
            }
            compiled.Add(new(new(design.Parts[owner].Instance.Id,c.Id),Decode(c.TotalFlowKgS,false),c.FlowRule,terms.ToImmutable()));
        }
        var digest=ports is null?AssemblyJson.Digest(new {Design=design.Digest,Unavailable=unavailableEdges.Order(StringComparer.Ordinal).ToArray()}):
            AssemblyJson.Digest(new {Design=design.Digest,Policy="physical-time-lattice/1",Scale=scale.ToString("x",CultureInfo.InvariantCulture),TimeScale=timeScale.ToString("x",CultureInfo.InvariantCulture)});
        return new(design,digest,stores.ToImmutable(),compiled.ToImmutable(),scale,ports,timeScale);
    }
    internal ConstructionFuelState Initial()=>ConstructionFuelState.Create(this,Stores.Select(s=>s.Initial*Scale).ToImmutableArray(),BigInteger.One,0);
}

internal sealed record ConstructionFuelSave(string Network,string Denominator,int Events,ImmutableArray<string> Quantities);
/// <summary>Immutable proposal/snapshot. Canonical installation belongs to SimulationTransactionEngine.</summary>
internal sealed class ConstructionFuelState
{
    internal ConstructionFuelNetwork Network {get;}
    internal ImmutableArray<BigInteger> Quantities {get;}
    internal BigInteger Denominator {get;}
    internal int Events {get;}
    private ConstructionFuelState(ConstructionFuelNetwork network,ImmutableArray<BigInteger> quantities,BigInteger denominator,int events)
    {Network=network;Quantities=quantities;Denominator=denominator;Events=events;}
    internal static ConstructionFuelState Create(ConstructionFuelNetwork network,ImmutableArray<BigInteger> quantities,BigInteger denominator,int events)
    {
        Require(!quantities.IsDefault&&quantities.Length==network.Stores.Length&&events>=0&&events<=network.InitiallyPositive,"Invalid fuel snapshot dimensions/events.");
        Require(denominator>0&&denominator.GetBitLength()<=1+events*network.RateBits,"Fuel denominator bound exceeded.");
        var positive=0;var gcd=denominator;
        for(var i=0;i<quantities.Length;i++)
        {
            var n=quantities[i];Require(n>=0&&n.GetBitLength()<=network.QuantityBits&&n<=network.Stores[i].Initial*network.Scale*denominator,"Invalid or credited store quantity.");
            if(n>0)positive++;gcd=BigInteger.GreatestCommonDivisor(gcd,n);
        }
        Require(events<=network.InitiallyPositive-positive,"Depletion event count inconsistent with inventory.");
        if(gcd>1){denominator/=gcd;quantities=quantities.Select(n=>n/gcd).ToImmutableArray();}
        return new(network,quantities,denominator,events);
    }
    internal ConstructionRatio InQ(int store)=>ConstructionRatio.Create(Quantities[store],Network.Scale*Denominator);
    private static string Hex(BigInteger n)=>n.ToString("x",CultureInfo.InvariantCulture);
    internal byte[] Save()=>AssemblyJson.Write(new ConstructionFuelSave(Network.Digest,Hex(Denominator),Events,Quantities.Select(Hex).ToImmutableArray()));
    internal static ConstructionFuelState Load(ConstructionFuelNetwork network,ReadOnlySpan<byte> bytes)
    {
        // Generic snapshots derive punctuation/hex space from every store;
        // retain the legacy static format's original admission envelope.
        var data=AssemblyJson.Read<ConstructionFuelSave>(bytes,network.MaximumSnapshotBytes);
        Require(data.Network==network.Digest&&!data.Quantities.IsDefault&&data.Quantities.Length==network.Stores.Length,"Wrong fuel network snapshot.");
        BigInteger Parse(string s,int bits)
        {
            Require(s is {Length:>0}&&s.Length<=bits/4+2&&s.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f'),"Invalid bounded integer encoding.");
            var n=BigInteger.Parse(s,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);Require(n>=0&&Hex(n)==s&&n.GetBitLength()<=bits,"Noncanonical integer.");return n;
        }
        return Create(network,data.Quantities.Select(s=>Parse(s,network.QuantityBits)).ToImmutableArray(),Parse(data.Denominator,network.QuantityBits),data.Events);
    }
}

internal sealed record ConstructionFuelPhase(ConstructionFuelState Before,ConstructionFuelState After,ConstructionRatio Ticks,ImmutableArray<bool> Active,
    ImmutableArray<bool> DeliveredLoads=default);
internal readonly record struct ConstructionFuelEvolution(ConstructionFuelState State,ImmutableArray<ConstructionRatio> ActiveTicks,ImmutableArray<ConstructionFuelPhase> Phases=default);
internal static class ConstructionFuelSolver
{
    // Pure finite consumption proposal. No refill, transfer credit, topology mutation, thrust or clock publication.
    internal static ConstructionFuelEvolution Advance(ConstructionFuelState source,long ticks,ReadOnlySpan<bool> requested)
        =>AdvanceCore(source,ticks,BigInteger.One,requested,false);
    internal static ConstructionFuelEvolution AdvanceExact(ConstructionFuelState source,ConstructionRatio ticks,ReadOnlySpan<bool> requested)
    {
        var network=source.Network;
        Require(network.Ports is not null&&ticks.Numerator>=0&&ticks.Denominator>0&&
            ticks.Denominator.GetBitLength()<=network.TimeScale.GetBitLength()&&ticks.Numerator.GetBitLength()<=network.TimeNumeratorBits,
            "Invalid physical fuel time representation.");
        Require(network.TimeScale%ticks.Denominator==0&&ticks.Numerator<=long.MaxValue*ticks.Denominator,"Physical fuel time outside prepared lattice.");
        return AdvanceCore(source,ticks.Numerator,ticks.Denominator,requested,true);
    }
    private static ConstructionFuelEvolution AdvanceCore(ConstructionFuelState source,BigInteger ticks,BigInteger timeDivisor,ReadOnlySpan<bool> requested,bool capture)
    {
        var network=source.Network;Require(ticks>=0&&requested.Length==network.Consumers.Length,"Invalid fuel interval/demand dimensions.");
        var phases=capture?ImmutableArray.CreateBuilder<ConstructionFuelPhase>():null;
        var any=false;foreach(var on in requested)any|=on;
        if(!any||ticks==0){
            if(capture&&ticks>0)phases!.Add(new(source,source,ConstructionRatio.Create(ticks,timeDivisor),Enumerable.Repeat(false,requested.Length).ToImmutableArray()));
            return new(source,network.ZeroActivity,phases?.ToImmutable()??default);
        }
        var n=source.Quantities.ToArray();var d=source.Denominator;var p=BigInteger.Zero;var events=source.Events;
        var rates=new BigInteger[n.Length];var work=new BigInteger[requested.Length];var active=new bool[requested.Length];
        var chosen=new List<int>[network.Consumers.Length][];
        for(var j=0;j<chosen.Length;j++){chosen[j]=new List<int>[network.Consumers[j].Terms.Length];for(var k=0;k<chosen[j].Length;k++)chosen[j][k]=[];}
        while(p<ticks*d)
        {
            var before=capture?ConstructionFuelState.Create(network,n.ToImmutableArray(),d,events):null;
            Array.Clear(rates);Array.Clear(active);
            for(var j=0;j<network.Consumers.Length;j++)
            {
                if(!requested[j])continue;var consumer=network.Consumers[j];var complete=true;
                for(var k=0;k<consumer.Terms.Length;k++)
                {
                    var term=consumer.Terms[k];var selected=chosen[j][k];selected.Clear();var level=consumer.Rule==ConstructionFlowRule.FarthestFirst?int.MinValue:int.MaxValue;
                    for(var v=0;v<term.Stores.Length;v++)
                    {
                        var i=term.Stores[v];if(!network.Stores[i].Enabled||n[i]==0)continue;var candidate=term.Levels[v];
                        var better=consumer.Rule==ConstructionFlowRule.FarthestFirst?candidate>level:candidate<level;
                        if(better){level=candidate;selected.Clear();}if(candidate==level)selected.Add(i);
                    }
                    if(selected.Count==0)complete=false;
                }
                if(!complete)continue;active[j]=true;
                for(var k=0;k<consumer.Terms.Length;k++){
                    var rate=consumer.Rate*(consumer.Terms[k].ShareFactor(chosen[j][k].Count)/timeDivisor);
                    foreach(var i in chosen[j][k])rates[i]+=rate;
                }
            }
            var earliest=-1;
            for(var i=0;i<n.Length;i++)if(rates[i]>0&&(earliest<0||n[i]*rates[earliest]<n[earliest]*rates[i]))earliest=i;
            if(earliest<0){
                if(capture)phases!.Add(new(before!,before!,ConstructionRatio.Create(ticks*d-p,d*timeDivisor),active.ToImmutableArray()));
                break;
            }
            var remaining=ticks*d-p;
            ConstructionRatio phaseTicks;
            if(n[earliest]<=rates[earliest]*remaining)
            {
                var numerator=n[earliest];var divisor=rates[earliest];
                phaseTicks=capture?ConstructionRatio.Create(numerator,d*divisor*timeDivisor):default;
                for(var i=0;i<n.Length;i++)n[i]=n[i]*divisor-rates[i]*numerator;
                for(var j=0;j<work.Length;j++)work[j]=work[j]*divisor+(active[j]?numerator:0);
                p=p*divisor+numerator;d*=divisor;events++;
                var gcd=BigInteger.GreatestCommonDivisor(d,p);
                foreach(var value in n)gcd=BigInteger.GreatestCommonDivisor(gcd,value);
                foreach(var value in work)gcd=BigInteger.GreatestCommonDivisor(gcd,value);
                if(gcd>1){d/=gcd;p/=gcd;for(var i=0;i<n.Length;i++)n[i]/=gcd;for(var j=0;j<work.Length;j++)work[j]/=gcd;}
                Require(events<=network.InitiallyPositive&&d.GetBitLength()<=1+events*network.RateBits,"Fuel event bound exceeded.");
            }
            else
            {
                phaseTicks=capture?ConstructionRatio.Create(remaining,d*timeDivisor):default;
                for(var i=0;i<n.Length;i++)n[i]-=rates[i]*remaining;
                for(var j=0;j<work.Length;j++)if(active[j])work[j]+=remaining;
                p=ticks*d;
            }
            if(capture)phases!.Add(new(before!,ConstructionFuelState.Create(network,n.ToImmutableArray(),d,events),phaseTicks,active.ToImmutableArray()));
        }
        var duration=ImmutableArray.CreateBuilder<ConstructionRatio>(work.Length);
        for(var j=0;j<work.Length;j++)duration.Add(ConstructionRatio.Create(work[j],d*timeDivisor));
        return new(ConstructionFuelState.Create(network,n.ToImmutableArray(),d,events),duration.MoveToImmutable(),phases?.ToImmutable()??default);
    }
}
