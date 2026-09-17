using System.Globalization;
using NovaCore.Simulation.Spacecraft.Resources;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyStores(PropellantInteger Fuel,PropellantInteger Oxidizer);
internal readonly record struct AssemblyConsumption(AssemblyStores Before,AssemblyStores After,PropellantInteger ExtentRate,
    PropellantInteger ConsumedTotal,PropellantDuration Powered,PropellantDuration Unpowered,PropellantClassification Classification);

/// <summary>Pure exact two-store arithmetic. Does not issue authority or own inventory.</summary>
internal static class AssemblyResources
{
    internal static PropellantInteger Mass(double kg)=>PropellantInteger.TryFromKilograms(kg,out var v)?v:throw new InvalidDataException("Invalid exact mass.");
    internal static PropellantInteger Rate(double k)=>PropellantInteger.TryDecodeFlow(k,out var v)?v:throw new InvalidDataException("Invalid exact flow.");
    internal static PropellantInteger Add(PropellantInteger a,PropellantInteger b)=>PropellantInteger.TryAdd(a,b,out var v)?v:throw new OverflowException("Exact quantity overflow.");
    internal static PropellantInteger Times(PropellantInteger a,ulong n)=>PropellantInteger.TryMultiply(a,n,out var v)?v:throw new OverflowException("Exact product overflow.");
    internal static PropellantInteger Subtract(PropellantInteger a,PropellantInteger b)=>PropellantInteger.TrySubtract(a,b,out var v)?v:throw new OverflowException("Exact subtraction underflow.");
    internal static bool Matched(AssemblyStores s)=>Times(s.Fuel,3)==Times(s.Oxidizer,2);
    internal static void Validate(CompiledAssemblyDesign d,AssemblyStores s)
    {
        if(!Matched(s))throw new InvalidDataException("Unmatched authoritative species.");
        foreach(var store in d.Tank.Definition.Stores)
            if(PropellantInteger.Compare(store.Species=="FUEL"?s.Fuel:s.Oxidizer,Mass(store.CapacityKg))>0)throw new InvalidDataException("Store capacity exceeded.");
    }
    internal static AssemblyConsumption Calculate(CompiledAssemblyDesign d,AssemblyStores s,PropellantInteger k,long ticks)
    {
        Validate(d,s);if(ticks<=0||ticks>15625)throw new InvalidDataException("Unsupported command interval.");
        var one=PropellantInteger.FromUInt64(1);
        if(k.IsZero)return new(s,s,k,default,new(default,one),new(PropellantInteger.FromUInt64((ulong)ticks),one),PropellantClassification.NoDemand);
        var fuelRate=Times(k,2);var required=Times(fuelRate,(ulong)ticks);var comparison=PropellantInteger.Compare(s.Fuel,required);
        var consumedFuel=comparison>=0?required:s.Fuel;
        var consumedOx=comparison>=0?Times(Times(k,3),(ulong)ticks):s.Oxidizer;
        var after=new AssemblyStores(Subtract(s.Fuel,consumedFuel),Subtract(s.Oxidizer,consumedOx));
        var kind=s.Fuel.IsZero?PropellantClassification.NoFeed:comparison>0?PropellantClassification.FullPowered:comparison==0?PropellantClassification.EndpointExhaustion:PropellantClassification.InteriorExhaustion;
        return new(s,after,k,Add(consumedFuel,consumedOx),new(consumedFuel,fuelRate),new(Subtract(required,consumedFuel),fuelRate),kind);
    }
    internal static string Hex(PropellantInteger value)
    {
        return string.Create(544,value,static (text,number)=>
        {
            for(var i=33;i>=0;i--)
                if(!number.Limb(i).TryFormat(text.Slice((33-i)*16,16),out var written,"x16",CultureInfo.InvariantCulture)||written!=16)
                    throw new InvalidOperationException("Exact resource formatting failed.");
        });
    }
    internal static PropellantInteger ParseHex(string text)
    {
        if(text.Length!=544||text.Any(c=>!(c is >= '0' and <= '9' or >= 'a' and <= 'f')))throw new InvalidDataException("Noncanonical exact resource encoding.");
        var value=default(PropellantInteger);
        for(var i=0;i<text.Length;i+=8)value=Add(Times(value,1UL<<32),PropellantInteger.FromUInt64(uint.Parse(text.AsSpan(i,8),NumberStyles.HexNumber,CultureInfo.InvariantCulture)));
        return value;
    }
}
