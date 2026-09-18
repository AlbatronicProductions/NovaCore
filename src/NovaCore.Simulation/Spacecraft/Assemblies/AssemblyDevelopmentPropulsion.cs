using System.Collections.Immutable;
using System.Security.Cryptography;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record AssemblyDevelopmentPropulsionData(string Schema,string Id,string Status,string Vehicle,string BaseDigest,
    string EngineConfiguration,string FuelIdentity,string OxidizerIdentity,double FuelCapacityKg,double OxidizerCapacityKg,
    double InitialFuelKg,double InitialOxidizerKg,double ThrustN,double ExtentRateKgS,double ExhaustSpeed,int FuelShare,int OxidizerShare,
    string PerformanceDomain,long MaximumQualificationTicks);

/// <summary>One explicitly provisional cold configuration, not a mutable resource owner or stock balance.</summary>
internal sealed class AssemblyDevelopmentPropulsion
{
    internal AssemblyDevelopmentPropulsionData Data {get;}
    internal string Digest {get;}
    private AssemblyDevelopmentPropulsion(AssemblyDevelopmentPropulsionData data)
    {Data=data;Digest=AssemblyJson.Digest(data);}

    internal static AssemblyDevelopmentPropulsion LoadDefault()
    {
        using var stream=typeof(AssemblyDevelopmentPropulsion).Assembly.GetManifestResourceStream("NovaCore.Development.SRV01.Propulsion.V1")
            ??throw new InvalidDataException("Missing development propulsion profile.");
        using var bytes=new MemoryStream();stream.CopyTo(bytes);
        // Normalize only the authored text transport, never dependency binaries or runtime quantities.
        var text=System.Text.Encoding.UTF8.GetString(bytes.ToArray()).Replace("\r\n","\n",StringComparison.Ordinal);
        var utf8=System.Text.Encoding.UTF8.GetBytes(text);
        if(Convert.ToHexString(SHA256.HashData(utf8))!="3A03909DA748624F22B5001AC67B1B5F1DD9689CAE20E0DA694231CF376C5779")
            throw new InvalidDataException("Unqualified development profile bytes.");
        var data=AssemblyJson.Read<AssemblyDevelopmentPropulsionData>(utf8);
        if(data.Schema!="novacore.development-propulsion/1"||data.FuelShare!=2||data.OxidizerShare!=3||
            AssemblyResources.Times(AssemblyResources.Mass(data.FuelCapacityKg),3)!=AssemblyResources.Times(AssemblyResources.Mass(data.OxidizerCapacityKg),2)||
            data.ExhaustSpeed!=3072||AssemblyResources.Rate(data.ThrustN)!=AssemblyResources.Times(AssemblyResources.Rate(data.ExtentRateKgS),15360))
            throw new InvalidDataException("Inconsistent development propulsion.");
        return new(data);
    }

    internal CompiledAssemblyDesign Apply(CompiledAssemblyDesign stock)=>Apply(stock,Data.InitialFuelKg,Data.InitialOxidizerKg);
    // Explicit cold initial-fill variant. This cannot address, refill or rebind an existing engine/session.
    internal CompiledAssemblyDesign Apply(CompiledAssemblyDesign stock,double initialFuelKg,double initialOxidizerKg)
        =>CompiledAssemblyDesign.ApplyDevelopment(stock,this,initialFuelKg,initialOxidizerKg);

    internal void ValidateLaunch(CompiledAssemblyDesign design,ReadOnlySpan<AssemblyCommand> commands)
    {
        long ticks=0;double speedBound=0;
        var stores=new AssemblyStores(AssemblyResources.Mass(design.Data.Design.InitialFuelKg),AssemblyResources.Mass(design.Data.Design.InitialOxidizerKg));
        foreach(var c in commands)
        {
            if(c.Ticks is <1 or >15625||c.Pair is not null||c.GimbalTargetY!=0||c.GimbalTargetZ!=0)
                throw new InvalidDataException("Development qualification admits fixed-axis main/off commands only.");
            ticks=checked(ticks+c.Ticks);
            if(ticks>Data.MaximumQualificationTicks)throw new InvalidDataException("Development qualification horizon exceeded.");
            var used=AssemblyResources.Calculate(design,stores,c.MainOn?AssemblyResources.Rate(Data.ExtentRateKgS):default,c.Ticks);
            var minimumMass=AssemblyLaunch.ObserveMass(design,used.After).Mass;
            // Conservative speed bound: no credit for gravity/thrust cancellation. All applied
            // torques are exactly zero in the admitted axial material frame, so angular state is fixed.
            speedBound+=(used.Powered.IsZero?0:Data.ThrustN/minimumMass)*c.Ticks/1_000_000d+9.81*c.Ticks/1_000_000d;
            if(!double.IsFinite(speedBound)||speedBound>4.65)throw new InvalidDataException("Development command exceeds retained motion envelope.");
            stores=used.After;
        }
    }
}

internal sealed partial class CompiledAssemblyDesign
{
    internal static CompiledAssemblyDesign ApplyDevelopment(CompiledAssemblyDesign stock,AssemblyDevelopmentPropulsion profile,double fuel,double oxidizer)
    {
        var p=profile.Data;
        if(stock.Development is not null||stock.Data.Design.Id!=p.Vehicle||stock.Digest!=p.BaseDigest)
            throw new InvalidDataException("Development profile requires its exact stock definition.");
        if(!double.IsFinite(fuel)||!double.IsFinite(oxidizer)||fuel<0||oxidizer<0||fuel>p.FuelCapacityKg||oxidizer>p.OxidizerCapacityKg)
            throw new InvalidDataException("Invalid finite development fill.");
        var defs=stock.Data.Definitions.Select(d=>d.Role switch {
            AssemblyRole.Tank=>d with {Stores=d.Stores.Select(s=>s with {CapacityKg=s.Species=="FUEL"?p.FuelCapacityKg:p.OxidizerCapacityKg}).ToImmutableArray()},
            AssemblyRole.MainEngine=>d with {Propulsion=d.Propulsion! with {FullThrustN=p.ThrustN,ExtentRateKgS=p.ExtentRateKgS,ExhaustSpeed=p.ExhaustSpeed}},
            _=>d}).ToImmutableArray();
        var instances=stock.Data.Design.Instances.Select(i=>i with {Definition=i.Definition with {Digest=AssemblyJson.Digest(defs.Single(d=>d.Id==i.Definition.Id))}}).ToImmutableArray();
        var data=stock.Data with {Definitions=defs,Design=stock.Data.Design with {Instances=instances,InitialFuelKg=fuel,InitialOxidizerKg=oxidizer}};
        var parts=stock.Parts.Select((part,i)=>part with {Instance=instances[i],Definition=defs.Single(d=>d.Id==part.Definition.Id)}).ToImmutableArray();
        var result=new CompiledAssemblyDesign(data,parts,stock.DryMass,stock.FirstMoment,stock.OriginInertia,profile);
        AssemblyResources.Validate(result,new(AssemblyResources.Mass(fuel),AssemblyResources.Mass(oxidizer)));
        var dry=result.ObserveMass(result.DryMass);var wet=result.ObserveMass(result.MaximumMass);
        // Same point stores at O: S and I_O remain fixed. I_COM=I_O-parallel(S)/M.
        // Diagonal eigenvalues and COM are monotone in 1/M; endpoint proofs cover the whole finite range.
        if(!dry.Inertia.PhysicalInertia||!wet.Inertia.PhysicalInertia||dry.Com.Y!=0||dry.Com.Z!=0||
            Math.Min(dry.Inertia.A,Math.Min(dry.Inertia.E,dry.Inertia.I))<190||wet.Com.X<0||dry.Com.X>1.17)
            throw new InvalidDataException("Development physical-property interval invalid.");
        return result;
    }
}
