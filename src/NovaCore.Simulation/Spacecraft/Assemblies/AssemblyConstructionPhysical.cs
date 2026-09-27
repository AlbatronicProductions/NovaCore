using System.Collections.Immutable;
using System.Numerics;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record ConstructionPowerPhase(ConstructionRatio Ticks,ImmutableArray<bool> DeliveredLoads);
internal sealed record ConstructionContinuousPower(ConstructionPowerState Before,ImmutableArray<BigInteger> Charge,
    ImmutableArray<bool> Active,ImmutableArray<ConstructionRatio> Delivered,ImmutableArray<ConstructionPowerPhase> Phases)
{
    internal ConstructionPowerState Finish(int fuelEvents)=>ConstructionPowerState.Create(Before.Network,Charge,1,Active,Before.Cursor,fuelEvents);
}

/// <summary>Cold bounded physical-service facts. Reuses the construction ledgers;
/// this object is neither mutable inventory nor publication authority.</summary>
internal sealed class ConstructionPhysicalServices
{
    internal CompiledCraft Craft {get;}
    internal ImmutableArray<BigInteger> BusRates {get;}
    internal int ScratchMagnitudeBits {get;}
    internal int NumericalScratchBits {get;}
    internal int ScratchSignedBits=>checked(ScratchMagnitudeBits+1);
    internal ConstructionPhysicalServices(CompiledCraft craft)
    {
        Craft=craft;var net=craft.Power;Require(craft.Function,"Physical services require FUNCTION.");
        Require(net.Modules.All(m=>m.Role is ElectricalRole.Battery or ElectricalRole.Load),"Physical profile does not qualify generators.");
        Require(net.Batteries.All(b=>b.Length<=1),"Physical profile qualifies one battery per electrical bus.");
        var rates=new BigInteger[net.Power.Count];
        foreach(var m in net.Modules)if(m.Role==ElectricalRole.Load&&m.Enabled)rates[m.Bus]+=m.Rate;
        BusRates=rates.ToImmutableArray();
        foreach(var rate in rates)Require(rate==0||craft.Fuel.TimeScale%rate==0,"Physical power rate is outside the prepared time lattice.");
        var rateBits=checked((int)BigInteger.Max(1,rates.DefaultIfEmpty().Max()).GetBitLength());
        var energyBits=checked((int)BigInteger.Max(1,net.Modules.Select(m=>m.Capacity).DefaultIfEmpty().Max()).GetBitLength());
        // E/R cuts: compare E1*R2, difference E1*R2-E2*R1,
        // denominator R1*R2, then loadRate*difference. Integer host cut
        // numerator <= Int64.MaxValue; use its 63 bits in the same derivation.
        ScratchMagnitudeBits=checked(Math.Max(net.ScratchBits,Math.Max(energyBits,63)+2*rateBits+2));
        Require(ScratchMagnitudeBits<=ConstructionFuelNetwork.MaximumArithmeticBits,"Physical service arithmetic admission width exceeded.");
        NumericalScratchBits=ConstructionNumerics.RequiredScratchBits(craft.Fuel);
    }
    internal ConstructionPhysicalEvolution Advance(ConstructionFuelState fuel,ConstructionPowerState power,long ticks,ReadOnlySpan<bool> requested)
    {
        Require(ReferenceEquals(fuel.Network,Craft.Fuel)&&ReferenceEquals(power.Network,Craft.Power)&&fuel.Events==power.FuelEvents&&
            ticks>=0&&requested.Length==Craft.Fuel.Consumers.Length,"Physical service source/interval mismatch.");
        var electrical=ConstructionPowerSolver.AdvanceContinuous(this,power,ticks);
        var state=fuel;var phases=ImmutableArray.CreateBuilder<ConstructionFuelPhase>();
        var allowed=new bool[requested.Length];
        foreach(var phase in electrical.Phases){
            Array.Clear(allowed);
            foreach(var a in Craft.Actuators){
                var delivered=a.DataReachable&&requested[a.Consumer];
                foreach(var load in a.RequiredLoads)delivered&=phase.DeliveredLoads[load];
                allowed[a.Consumer]=delivered;
            }
            var next=ConstructionFuelSolver.AdvanceExact(state,phase.Ticks,allowed);
            phases.AddRange(next.Phases.Select(p=>p with {DeliveredLoads=phase.DeliveredLoads}));state=next.State;
        }
        return new(state,electrical.Finish(state.Events),phases.ToImmutable(),electrical.Delivered);
    }
    internal bool Powered(CraftActuator a,ConstructionPowerState power)
    {
        Require(ReferenceEquals(power.Network,Craft.Power),"Foreign actuator power source.");
        if(!a.DataReachable)return false;
        foreach(var load in a.RequiredLoads)
        {
            if(!power.Active[load])return false;var supplied=false;
            foreach(var battery in Craft.Power.Batteries[Craft.Power.Modules[load].Bus])supplied|=power.Active[battery]&&power.Charge[battery]>0;
            if(!supplied)return false;
        }
        return true;
    }
    internal bool Available(CraftActuator a,ConstructionFuelState fuel,ConstructionPowerState power)
    {
        Require(ReferenceEquals(fuel.Network,Craft.Fuel),"Foreign actuator availability source.");
        if(!Powered(a,power))return false;
        foreach(var term in Craft.Fuel.Consumers[a.Consumer].Terms)
        {
            var supplied=false;foreach(var store in term.Stores)supplied|=Craft.Fuel.Stores[store].Enabled&&fuel.Quantities[store]>0;
            if(!supplied)return false;
        }
        return true;
    }
}
internal sealed record ConstructionPhysicalEvolution(ConstructionFuelState Fuel,ConstructionPowerState Power,
    ImmutableArray<ConstructionFuelPhase> Phases,ImmutableArray<ConstructionRatio> Delivered);

internal static partial class ConstructionPowerSolver
{
    internal static ConstructionContinuousPower AdvanceContinuous(ConstructionPhysicalServices profile,ConstructionPowerState source,long ticks)
    {
        var net=source.Network;
        Require(ReferenceEquals(net,profile.Craft.Power)&&ticks>=0&&source.Denominator.IsOne,"Physical power requires its integer host-boundary snapshot.");
        var charge=source.Charge.ToArray();var active=source.Active.ToArray();var cutoff=new ConstructionRatio[net.Power.Count];
        var cuts=new List<ConstructionRatio>{new(0,1),new(ticks,1)};
        var delivered=Enumerable.Repeat(new ConstructionRatio(0,1),net.Modules.Length).ToArray();
        for(var bus=0;bus<net.Power.Count;bus++){
            var rate=profile.BusRates[bus];var battery=net.Batteries[bus].IsEmpty?-1:net.Batteries[bus][0];
            var energy=battery>=0&&active[battery]?charge[battery]:BigInteger.Zero;
            bool? observed=null;
            foreach(var m in net.Modules.Select((m,i)=>(m,i)).Where(x=>x.m.Bus==bus&&x.m.Role==ElectricalRole.Load)){
                Require(m.m.Enabled||!active[m.i],"Physical load enablement differs from prepared configuration.");
                if(m.m.Enabled){
                    observed??=active[m.i];Require(observed==active[m.i],"Partial physical bus load shutdown is not qualified.");
                    Require(energy.IsZero||active[m.i],"Energized physical bus load configuration changed.");
                }
            }
            cutoff[bus]=rate==0?new(ticks,1):ConstructionRatio.Create(energy,rate);
            if(Compare(cutoff[bus],new(ticks,1))>0)cutoff[bus]=new(ticks,1);
            cuts.Add(cutoff[bus]);
            foreach(var m in net.Modules.Select((m,i)=>(m,i)).Where(x=>x.m.Bus==bus&&x.m.Role==ElectricalRole.Load&&x.m.Enabled)){
                delivered[m.i]=ConstructionRatio.Create(m.m.Rate*cutoff[bus].Numerator,cutoff[bus].Denominator);
                if(rate>0&&energy<=rate*ticks)active[m.i]=false;
            }
            if(battery>=0&&active[battery])charge[battery]=BigInteger.Max(0,energy-rate*ticks);
        }
        if(ticks==0)return new(source,source.Charge,source.Active,net.ZeroDelivery,[]);
        cuts.Sort(Compare);var distinct=new List<ConstructionRatio>();foreach(var cut in cuts)if(distinct.Count==0||Compare(distinct[^1],cut)!=0)distinct.Add(cut);
        var phases=ImmutableArray.CreateBuilder<ConstructionPowerPhase>();
        for(var k=1;k<distinct.Count;k++){
            var start=distinct[k-1];var end=distinct[k];
            var duration=ConstructionRatio.Create(end.Numerator*start.Denominator-start.Numerator*end.Denominator,end.Denominator*start.Denominator);
            Require(profile.Craft.Fuel.TimeScale%duration.Denominator==0,"Physical power phase left prepared time lattice.");
            var enabled=new bool[net.Modules.Length];
            for(var i=0;i<enabled.Length;i++)if(net.Modules[i].Role==ElectricalRole.Load){
                var m=net.Modules[i];enabled[i]=source.Active[i]&&Compare(start,cutoff[m.Bus])<0;
            }
            phases.Add(new(duration,enabled.ToImmutableArray()));
        }
        return new(source,charge.ToImmutableArray(),active.ToImmutableArray(),delivered.ToImmutableArray(),phases.ToImmutable());
    }
    private static int Compare(ConstructionRatio a,ConstructionRatio b)=>(a.Numerator*b.Denominator).CompareTo(b.Numerator*a.Denominator);
}
