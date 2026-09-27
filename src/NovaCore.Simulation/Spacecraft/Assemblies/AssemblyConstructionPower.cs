using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold connected components; command/data never grants electricity.</summary>
internal sealed class ConstructionServiceNetwork
{
    internal ImmutableArray<int> Component {get;}
    internal int Count {get;}
    private ConstructionServiceNetwork(ImmutableArray<int> component,int count){Component=component;Count=count;}
    internal static ConstructionServiceNetwork Compile(CompiledConstructionDesign design,ConstructionService service,ImmutableArray<string> unavailable=default,CompiledCraftPorts? ports=null)
    {
        Require(design.Data.Schema==CompiledConstructionDesign.CraftSchema?ports is not null&&ReferenceEquals(ports.Design,design):ports is null,"CraftDocument requires qualified explicit-port service compilation.");
        Require(service is ConstructionService.Electricity or ConstructionService.Data,"Unsupported undirected service.");
        if(unavailable.IsDefault)unavailable=[];Unique(unavailable,x=>x,"unavailable service edges");
        if(ports is not null){Require(unavailable.IsEmpty,"Craft port topology cannot be altered through legacy edge overrides.");var portLabels=ports.Components(service,out var count);return new(portLabels,count);}
        foreach(var id in unavailable)Require(design.ServiceEdges.Any(e=>e.Id==id),"Unknown unavailable edge.");
        var parent=Enumerable.Range(0,design.Parts.Length).ToArray();
        int Root(int p){while(parent[p]!=p)p=parent[p];return p;}
        foreach(var e in design.ServiceEdges)if(e.Services.HasFlag(service)&&!unavailable.Contains(e.Id))
        {var a=Root(e.Supply);var b=Root(e.Receiver);parent[Math.Max(a,b)]=Math.Min(a,b);}
        var labels=new Dictionary<int,int>();var components=ImmutableArray.CreateBuilder<int>(parent.Length);
        for(var p=0;p<parent.Length;p++){var r=Root(p);if(!labels.TryGetValue(r,out var i)){i=labels.Count;labels.Add(r,i);}components.Add(i);}
        return new(components.MoveToImmutable(),labels.Count);
    }
    internal bool Connected(int a,int b)=>(uint)a<(uint)Component.Length&&(uint)b<(uint)Component.Length&&Component[a]>=0&&Component[a]==Component[b];
}

internal readonly record struct ConstructionElectricalKey(string Part,string Module);
internal sealed record ConstructionPowerModule(ConstructionElectricalKey Key,ElectricalRole Role,int Part,int Bus,BigInteger Capacity,BigInteger Rate,int Driver,BigInteger Initial,bool Enabled);
/// <summary>Expression-value bounds, not CLR working-buffer sizes. See stage4-width-derivation.md.</summary>
internal readonly record struct ConstructionPowerWidths(int IntegerBits,int HexDigits,int ScratchMagnitudeBits,int ScratchSignedBits)
{
    internal static ConstructionPowerWidths Derive(int denominatorBits)
    {
        Require(denominatorBits>0&&denominatorBits<=ConstructionFuelNetwork.MaximumArithmeticBits,"Power denominator admission width exceeded.");
        // Preserve saved-integer admission. Hex parsing precedes sign/canonical rejection.
        var integers=checked(denominatorBits+2199);var hex=checked(integers/4+2);
        var parsed=checked(4*hex);var pair=checked(2*denominatorBits);
        var magnitude=Math.Max(pair,parsed);
        Require(magnitude<=ConstructionFuelNetwork.MaximumArithmeticBits,"Power arithmetic admission width exceeded.");
        // Positive pair products need a sign bit; the most negative parsed value already includes it.
        return new(integers,hex,magnitude,Math.Max(checked(pair+1),parsed));
    }
}
internal sealed class ConstructionPowerNetwork
{
    internal ConstructionFuelNetwork Fuel {get;}
    internal ConstructionServiceNetwork Power {get;}
    internal ConstructionServiceNetwork Data {get;}
    internal string Digest {get;}
    internal ImmutableArray<ConstructionPowerModule> Modules {get;}
    internal ImmutableArray<ImmutableArray<int>> Batteries {get;}
    internal ImmutableArray<ConstructionRatio> ZeroDelivery {get;}
    internal ConstructionPowerWidths Widths {get;}
    internal int IntegerBits=>Widths.IntegerBits;
    internal int ScratchBits=>Widths.ScratchMagnitudeBits;
    internal int MaximumSnapshotBytes {get;}
    internal static int SnapshotCapacity(int modules,int hexDigits,long cursorBytes,bool modular)
    {
        Require(modules>=0&&hexDigits>0&&cursorBytes>=0,"Invalid power snapshot dimensions.");
        // Canonical ASCII: hex strings include quotes/commas; each bool uses
        // <=6 bytes; cursor digits derive from this network's battery count.
        // Fixed names, identities,
        // event count and punctuation fit the independently counted256 bytes.
        var bytes=modular?checked(256L+((long)modules+1)*(hexDigits+3L)+modules*6L+cursorBytes):
            checked(16384L+((long)modules+1)*hexDigits);
        Require(bytes<=Array.MaxLength,"Power snapshot exceeds the CLR byte-array representation.");return (int)bytes;
    }
    private ConstructionPowerNetwork(ConstructionFuelNetwork fuel,ConstructionServiceNetwork power,ConstructionServiceNetwork data,string digest,
        ImmutableArray<ConstructionPowerModule> modules,ImmutableArray<ImmutableArray<int>> batteries)
    {
        Fuel=fuel;Power=power;Data=data;Digest=digest;Modules=modules;Batteries=batteries;
        ZeroDelivery=Enumerable.Repeat(new ConstructionRatio(0,1),modules.Length).ToImmutableArray();
        Widths=ConstructionPowerWidths.Derive(checked(1+fuel.InitiallyPositive*fuel.RateBits));
        MaximumSnapshotBytes=SnapshotCapacity(modules.Length,Widths.HexDigits,
            batteries.Sum(b=>(long)Math.Max(0,b.Length-1).ToString(CultureInfo.InvariantCulture).Length+1),fuel.Ports is not null);
    }
    internal static ConstructionPowerNetwork Compile(ConstructionFuelNetwork fuel,ImmutableArray<string> unavailable=default)
    {
        var design=fuel.Design;if(unavailable.IsDefault)unavailable=[];
        Require(fuel.Ports is not null||design.Parts.Sum(p=>p.Definition.Construction!.Electrical.Length)<=512,"Power module capacity exceeded.");
        var power=ConstructionServiceNetwork.Compile(design,ConstructionService.Electricity,unavailable,fuel.Ports);
        var data=ConstructionServiceNetwork.Compile(design,ConstructionService.Data,unavailable,fuel.Ports);
        var modules=ImmutableArray.CreateBuilder<ConstructionPowerModule>();
        for(var p=0;p<design.Parts.Length;p++)
        {
            var part=design.Parts[p];var configuration=design.Data.Configuration.Single(c=>c.Part==part.Instance.Id);
            foreach(var e in part.Definition.Construction!.Electrical)
            {
                var initial=configuration.Electrical.Single(c=>c.Module==e.Id);
                var driver=e.Driver is null?-1:fuel.Consumers.FindIndex(c=>c.Key==new ConstructionConsumerKey(part.Instance.Id,e.Driver));
                Require(e.Driver is null||driver>=0,"Unresolved power generator driver.");
                var bus=fuel.Ports is null?power.Component[p]:fuel.Ports.ElectricalBus(power.Component,p,e.Id);
                modules.Add(new(new(part.Instance.Id,e.Id),e.Role,p,bus,ConstructionFuelNetwork.Decode(e.CapacityJ,true),
                    ConstructionFuelNetwork.Decode(e.Watts,false),driver,ConstructionFuelNetwork.Decode(initial.ChargeJ,true),initial.Enabled));
            }
        }
        var ordered=modules.ToImmutable();var batteries=Enumerable.Range(0,power.Count).Select(bus=>Enumerable.Range(0,ordered.Length)
            .Where(i=>ordered[i].Bus==bus&&ordered[i].Role==ElectricalRole.Battery).ToImmutableArray()).ToImmutableArray();
        return new(fuel,power,data,AssemblyJson.Digest(new {Fuel=fuel.Digest,Unavailable=unavailable.Order(StringComparer.Ordinal).ToArray()}),ordered,batteries);
    }
    internal ConstructionPowerState Initial()=>ConstructionPowerState.Create(this,Modules.Select(m=>m.Initial).ToImmutableArray(),1,
        Modules.Select(m=>m.Enabled).ToImmutableArray(),Enumerable.Repeat(0,Power.Count).ToImmutableArray(),0);
    internal bool CanCommand(int part)=>(uint)part<(uint)Fuel.Design.Parts.Length&&(Fuel.Ports is null?Data.Connected(Fuel.Design.ControlIndex,part):
        Fuel.Design.Parts[part].Definition.Construction!.Consumers.Any(c=>Fuel.Ports.CanCommand(part,c.Id))||Fuel.Ports.CanCommand(part,null));
    internal bool CanCommand(ConstructionConsumerKey key)=>Fuel.Ports is null?CanCommand(Fuel.Design.Index(key.Part)):Fuel.Ports.CanCommand(Fuel.Design.Index(key.Part),key.Consumer);
}

internal sealed record ConstructionPowerSave(string Network,string Denominator,int FuelEvents,ImmutableArray<string> Charge,ImmutableArray<bool> Active,ImmutableArray<int> Cursor);
internal sealed class ConstructionPowerState
{
    internal ConstructionPowerNetwork Network {get;}
    internal ImmutableArray<BigInteger> Charge {get;}
    internal BigInteger Denominator {get;}
    internal ImmutableArray<bool> Active {get;}
    internal ImmutableArray<int> Cursor {get;}
    internal int FuelEvents {get;}
    private ConstructionPowerState(ConstructionPowerNetwork network,ImmutableArray<BigInteger> charge,BigInteger denominator,ImmutableArray<bool> active,ImmutableArray<int> cursor,int events)
    {Network=network;Charge=charge;Denominator=denominator;Active=active;Cursor=cursor;FuelEvents=events;}
    internal static ConstructionPowerState Create(ConstructionPowerNetwork network,ImmutableArray<BigInteger> charge,BigInteger denominator,ImmutableArray<bool> active,ImmutableArray<int> cursor,int events)
    {
        Require(!charge.IsDefault&&!active.IsDefault&&!cursor.IsDefault&&charge.Length==network.Modules.Length&&active.Length==charge.Length&&cursor.Length==network.Power.Count,"Power snapshot dimensions.");
        Require(events>=0&&events<=network.Fuel.InitiallyPositive&&denominator>0&&denominator.GetBitLength()<=1+events*network.Fuel.RateBits,"Power denominator bound.");
        var gcd=denominator;
        for(var i=0;i<charge.Length;i++)
        {Require(charge[i]>=0&&charge[i].GetBitLength()<=network.IntegerBits&&charge[i]<=network.Modules[i].Capacity*denominator,"Battery energy outside capacity.");
            Require(network.Modules[i].Role==ElectricalRole.Load||active[i]==network.Modules[i].Enabled,"Non-load enablement is immutable configuration.");gcd=BigInteger.GreatestCommonDivisor(gcd,charge[i]);}
        for(var bus=0;bus<cursor.Length;bus++)Require(cursor[bus]>=0&&cursor[bus]<Math.Max(1,network.Batteries[bus].Length),"Invalid battery cursor.");
        if(gcd>1){denominator/=gcd;charge=charge.Select(v=>v/gcd).ToImmutableArray();}
        return new(network,charge,denominator,active,cursor,events);
    }
    internal ConstructionRatio InQ(int module)=>ConstructionRatio.Create(Charge[module],Denominator);
    // Explicit proposal for a user/service command, not a hidden automatic restart.
    internal ConstructionPowerState SetLoadActive(int module,bool enabled)
    {
        Require((uint)module<(uint)Network.Modules.Length&&Network.Modules[module].Role==ElectricalRole.Load,"Command target is not a load.");
        return new(Network,Charge,Denominator,Active.SetItem(module,enabled),Cursor,FuelEvents);
    }
    private static string Hex(BigInteger n)=>n.ToString("x",CultureInfo.InvariantCulture);
    internal byte[] Save()=>AssemblyJson.Write(new ConstructionPowerSave(Network.Digest,Hex(Denominator),FuelEvents,Charge.Select(Hex).ToImmutableArray(),Active,Cursor));
    internal static ConstructionPowerState Load(ConstructionPowerNetwork network,ReadOnlySpan<byte> bytes)
    {
        var data=AssemblyJson.Read<ConstructionPowerSave>(bytes,network.MaximumSnapshotBytes);
        Require(data.Network==network.Digest&&!data.Charge.IsDefault&&data.Charge.Length==network.Modules.Length,"Wrong power network snapshot.");
        BigInteger Parse(string s)
        {
            Require(s is {Length:>0}&&s.Length<=network.Widths.HexDigits&&s.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f'),"Invalid bounded energy integer.");
            var n=BigInteger.Parse(s,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);Require(n>=0&&Hex(n)==s&&n.GetBitLength()<=network.IntegerBits,"Noncanonical energy integer.");return n;
        }
        return Create(network,data.Charge.Select(Parse).ToImmutableArray(),Parse(data.Denominator),data.Active,data.Cursor,data.FuelEvents);
    }
}

internal readonly record struct ConstructionPowerEvolution(ConstructionPowerState State,ImmutableArray<ConstructionRatio> Delivered);
internal static partial class ConstructionPowerSolver
{
    internal static ConstructionPowerEvolution Advance(ConstructionPowerState source,long ticks,ConstructionFuelEvolution fuel)
    {
        var net=source.Network;
        Require(ticks>=0&&ReferenceEquals(fuel.State.Network,net.Fuel)&&fuel.State.Events>=source.FuelEvents&&
            !fuel.ActiveTicks.IsDefault&&fuel.ActiveTicks.Length==net.Fuel.Consumers.Length,"Wrong fuel interval/network for electrical proposal.");
        var denominator=source.Denominator/BigInteger.GreatestCommonDivisor(source.Denominator,fuel.State.Denominator)*fuel.State.Denominator;
        foreach(var time in fuel.ActiveTicks)
        {
            Require(time.Numerator>=0&&time.Denominator>0&&time.Numerator.GetBitLength()<=net.IntegerBits&&time.Denominator.GetBitLength()<=1+fuel.State.Events*net.Fuel.RateBits&&
                time.Numerator<=ticks*time.Denominator,"Invalid generator active duration.");
            // Validate the accumulated pair before a third denominator can enter the product.
            // Keep this after activity validation to preserve refusal diagnostic precedence.
            Require(denominator.GetBitLength()<=1+fuel.State.Events*net.Fuel.RateBits,"Electrical lifetime arithmetic bound exceeded.");
            denominator=denominator/BigInteger.GreatestCommonDivisor(denominator,time.Denominator)*time.Denominator;
            Require(denominator.GetBitLength()<=1+fuel.State.Events*net.Fuel.RateBits,"Electrical lifetime arithmetic bound exceeded.");
        }
        Require(denominator.GetBitLength()<=1+fuel.State.Events*net.Fuel.RateBits,"Electrical lifetime arithmetic bound exceeded.");
        var work=false;for(var i=0;i<net.Modules.Length;i++)if(source.Active[i]&&net.Modules[i].Role!=ElectricalRole.Battery){work=true;break;}
        if(ticks==0||!work)return new(source,net.ZeroDelivery);
        var charge=source.Charge.ToArray();for(var i=0;i<charge.Length;i++)charge[i]*=denominator/source.Denominator;
        var active=source.Active.ToArray();var cursor=source.Cursor.ToArray();var delivered=new BigInteger[net.Modules.Length];
        // Installed KSA phases: all engine generators, all solar generators, then ordinary loads.
        for(var phase=0;phase<2;phase++)for(var i=0;i<net.Modules.Length;i++)
        {
            var m=net.Modules[i];if(!active[i]||m.Role!=(phase==0?ElectricalRole.EngineGenerator:ElectricalRole.SolarGenerator))continue;
            var duration=m.Role==ElectricalRole.SolarGenerator?new ConstructionRatio(ticks,1):fuel.ActiveTicks[m.Driver];
            var wanted=m.Rate*duration.Numerator*(denominator/duration.Denominator);
            delivered[i]=Transfer(net,m.Bus,wanted,true,denominator,charge,active,cursor);
        }
        for(var i=0;i<net.Modules.Length;i++)
        {
            var m=net.Modules[i];if(m.Role!=ElectricalRole.Load||!active[i])continue;
            var available=false;foreach(var battery in net.Batteries[m.Bus])if(active[battery]&&charge[battery]>0){available=true;break;}
            if(!available){active[i]=false;continue;}
            delivered[i]=Transfer(net,m.Bus,m.Rate*ticks*denominator,false,denominator,charge,active,cursor);
            // Partial supply remains active this turn; a later zero-supply turn disables it.
        }
        var ratios=ImmutableArray.CreateBuilder<ConstructionRatio>(delivered.Length);foreach(var n in delivered)ratios.Add(ConstructionRatio.Create(n,denominator));
        return new(ConstructionPowerState.Create(net,charge.ToImmutableArray(),denominator,active.ToImmutableArray(),cursor.ToImmutableArray(),fuel.State.Events),ratios.MoveToImmutable());
    }
    private static BigInteger Transfer(ConstructionPowerNetwork net,int bus,BigInteger requested,bool produce,BigInteger denominator,BigInteger[] charge,bool[] active,int[] cursor)
    {
        if(requested==0)return BigInteger.Zero;var batteries=net.Batteries[bus];var remaining=requested;
        for(var offset=0;offset<batteries.Length;offset++)
        {
            var position=(cursor[bus]+offset)%batteries.Length;var i=batteries[position];if(!active[i])continue;
            var room=produce?net.Modules[i].Capacity*denominator-charge[i]:charge[i];var amount=BigInteger.Min(room,remaining);
            charge[i]+=produce?amount:-amount;remaining-=amount;
            if(remaining==0){cursor[bus]=(position+1)%batteries.Length;return requested;}
        }
        cursor[bus]=0;return requested-remaining;
    }
}
