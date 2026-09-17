using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyCommand(bool MainOn,string? Pair,double GimbalTargetY,double GimbalTargetZ,long Ticks);
internal readonly record struct CompiledAssemblyCommand(AssemblyCommand Request,ushort Jets,PropellantInteger ExtentRate);
internal enum AssemblyFeedState { NoDemand,Available,Exhausted }
internal readonly record struct AssemblyRealization(bool MainOn,ushort Jets,AssemblyFeedState Feed);
internal readonly record struct AssemblyRuntimeState(SimulationInstant Epoch,AssemblyStores Stores,AssemblyMass Mass,
    AssemblyMotion Motion,AssemblyGimbal Gimbal,AssemblyCommand AppliedCommand,AssemblyRealization Actual,
    int Frontier,ulong ResourceRevision,ulong ActuatorRevision);

/// <summary>Immutable recorded launch data. No mutable state or private owner seal.</summary>
internal sealed class AssemblyLaunch
{
    internal CompiledAssemblyDesign Design {get;}
    internal SpacecraftDefinition Spacecraft {get;}
    internal string LaunchId {get;}
    internal ImmutableArray<string> PartKeys {get;}
    internal ImmutableArray<string> CapabilityKeys {get;}
    internal ImmutableArray<string> StoreKeys {get;}
    internal ImmutableArray<CompiledAssemblyCommand> Plan {get;}
    internal AssemblyRuntimeState Initial {get;}
    internal SimulationInstant End {get;}
    internal AssemblyLaunch(CompiledAssemblyDesign design,SpacecraftDefinition spacecraft,string launchId,
        AssemblyMotion initial,AssemblyGimbal gimbal,ReadOnlySpan<AssemblyCommand> commands,SimulationInstant origin=default)
    {
        if(!spacecraft.Id.IsValid||spacecraft.CarrierFrame.Value==0||spacecraft.BodyFrame.Value==0||spacecraft.BodyFrame==spacecraft.CarrierFrame||
            string.IsNullOrWhiteSpace(spacecraft.DiagnosticName)||string.IsNullOrWhiteSpace(launchId)||launchId.Length>96||
            launchId.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c is not '_' and not '-')||commands.Length is <1 or >128)
            throw new InvalidDataException("Invalid assembly launch identity/capacity.");
        if(!AssemblyDynamics.InEnvelope(initial)||initial.PositionO!=Double3.Zero||initial.VelocityO.LengthSquared>.25*.25||initial.AngularVelocityBody.LengthSquared>.05*.05)
            throw new InvalidDataException("Initial motion exceeds the qualified envelope.");
        ValidateGimbal(gimbal);
        Design=design;Spacecraft=spacecraft;LaunchId=launchId;
        PartKeys=design.Parts.Select(p=>RuntimeKey("part",launchId,p.Instance.Id)).ToImmutableArray();
        CapabilityKeys=design.Parts.Where(p=>p.Definition.Propulsion is not null).Select(p=>RuntimeKey("capability",launchId,p.Instance.Id,p.Definition.Propulsion!.Id))
            .Concat(design.Jets.Where(j=>j.Definition.Role==AssemblyRole.RcsBlock).Select(j=>RuntimeKey("capability",launchId,j.Instance.Id,j.Propulsion.Id)))
            .Concat([RuntimeKey("capability",launchId,design.Main.Instance.Id,design.Main.Definition.Gimbal!.Id)]).ToImmutableArray();
        StoreKeys=design.Tank.Definition.Stores.Select(s=>RuntimeKey("store",launchId,design.Tank.Instance.Id,s.Id)).ToImmutableArray();
        var compiled=ImmutableArray.CreateBuilder<CompiledAssemblyCommand>(commands.Length);long total=0;
        foreach(var command in commands)
        {
            if(command.Ticks is <1 or >15625||!double.IsFinite(command.GimbalTargetY)||!double.IsFinite(command.GimbalTargetZ))throw new InvalidDataException("Unsupported scheduled command.");
            total=checked(total+command.Ticks);if(total>2_000_000)throw new InvalidDataException("Episode exceeds qualified horizon.");
            ushort mask=0;var rate=default(PropellantInteger);
            if(command.MainOn)rate=AssemblyResources.Rate(design.Main.Definition.Propulsion!.ExtentRateKgS);
            if(command.Pair is {} name)
            {
                var pair=design.Pair(name);
                for(var i=0;i<design.Jets.Length;i++)
                    if(design.Jets[i].Matches(pair.First,pair.FirstActuator)||design.Jets[i].Matches(pair.Second,pair.SecondActuator))
                    {mask|=(ushort)(1<<i);rate=AssemblyResources.Add(rate,AssemblyResources.Rate(design.Jets[i].Propulsion.ExtentRateKgS));}
            }
            compiled.Add(new(command,mask,rate));
        }
        Plan=compiled.MoveToImmutable();End=new(checked(origin.Ticks+total));
        var stores=new AssemblyStores(AssemblyResources.Mass(design.Data.Design.InitialFuelKg),AssemblyResources.Mass(design.Data.Design.InitialOxidizerKg));
        AssemblyResources.Validate(design,stores);
        Initial=new(origin,stores,ObserveMass(design,stores),initial,gimbal,default,default,0,0,0);
    }
    // Length-prefixed components preserve tuple identity even when authored IDs contain '/'.
    internal static string RuntimeKey(string kind,params string[] components)=>kind+":"+string.Concat(components.Select(x=>x.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+x));
    internal static AssemblyMass ObserveMass(CompiledAssemblyDesign d,AssemblyStores s)
    {
        var total=AssemblyResources.Add(AssemblyResources.Mass(d.DryMass),AssemblyResources.Add(s.Fuel,s.Oxidizer));
        if(!total.TryToKilograms(out var kg))throw new InvalidDataException("Mass projection failed.");
        return d.ObserveMass(kg);
    }
    private static void ValidateGimbal(AssemblyGimbal g)
    {
        if(!double.IsFinite(g.ActualY)||!double.IsFinite(g.ActualZ)||!double.IsFinite(g.TargetY)||!double.IsFinite(g.TargetZ)||
            Math.Abs(g.ActualY)>.05||Math.Abs(g.ActualZ)>.05||Math.Abs(g.TargetY)>.05||Math.Abs(g.TargetZ)>.05)
            throw new InvalidDataException("Invalid initial actual/target gimbal.");
    }
}

/// <summary>Read-only catalog. Registration is cold and digest checked; no vehicle-name behavior.</summary>
internal sealed class AssemblyStockCatalog
{
    private readonly ImmutableArray<CompiledAssemblyDesign> _designs;
    internal AssemblyStockCatalog(ReadOnlySpan<(byte[] Json,string Digest)> entries)
    {
        var builder=ImmutableArray.CreateBuilder<CompiledAssemblyDesign>(entries.Length);
        foreach(var entry in entries)
        {
            var d=CompiledAssemblyDesign.Compile(entry.Json);
            if(d.Digest!=entry.Digest||builder.Any(x=>x.Data.Design.Id==d.Data.Design.Id))throw new InvalidDataException("Duplicate or changed stock registration.");
            builder.Add(d);
        }
        _designs=builder.MoveToImmutable();
    }
    internal CompiledAssemblyDesign Resolve(string id)=>_designs.Single(d=>d.Data.Design.Id==id);
    internal static AssemblyStockCatalog LoadDefault()
    {
        static byte[] Read(string resource)
        {
            using var stream=typeof(AssemblyStockCatalog).Assembly.GetManifestResourceStream(resource)??throw new InvalidDataException("Missing stock assembly.");
            using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();
        }
        return new([(Read("NovaCore.Stock.SRV01.G0B"),"a06153ad518bc7fc7fd365e17cd1f3ed84c09052674401a84049f9ad61212340"),
            (Read("NovaCore.Stock.SRV01.FourHorn"),"ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf")]);
    }
}

internal enum AssemblyFlightStatus
{
    Invalid,Ready,Prepared,Published,AcceptedCredit,NoWork,AwaitingDebt,BudgetExhausted,Completed,
    InvalidAuthority,WrongOwnerThread,Reentrant,StaleSource,InvalidInput,InvalidSequence,OutstandingProposal,
    InvalidProposal,PendingEvent,HistoryCapacity,Overflow,Invalidated,PreparationRefused,CanonicalCommittedPrivateInvalidated
}
internal sealed class AssemblyFlightAuthority
{
    internal AssemblyLaunch Launch {get;}
    internal AssemblyFlightAuthority(AssemblyLaunch launch)=>Launch=launch;
}
internal readonly struct AssemblyFlightProposal(long generation,object? seal=null)
{
    private readonly object? _seal=seal;
    internal readonly long Generation=generation;
    internal bool IssuedBy(object seal)=>ReferenceEquals(_seal,seal);
}
internal readonly record struct AssemblyFlightRecord(int Index,AssemblyCommand Command,AssemblyGimbal HeldGimbal,
    AssemblyWrench Wrench,PropellantDuration Powered,PropellantClassification Classification,AssemblyRuntimeState Successor,
    StateRevision BeforeRevision,StateRevision StateRevision,TimelineRevision TimelineRevision);
internal readonly record struct AssemblyFlightObservation(AssemblyRuntimeState State,StateRevision StateRevision,
    TimelineRevision TimelineRevision,ContinuationClockState Clock,long HostSequence,int HistoryCount,bool PrivateInvalidated);
internal readonly record struct AssemblyFlightResult(AssemblyFlightStatus Status,int PublishedCount=0);
internal readonly record struct AssemblyHostCredit(long HostTicks,int Frontier);

/// <summary>Pure preparation. No canonical storage, clock, history or spending lease.</summary>
internal static class AssemblyFlightPreparation
{
    internal static AssemblyFlightRecord Evaluate(AssemblyLaunch launch,in AssemblyRuntimeState source,StateRevision revision,TimelineRevision timeline)
    {
        var command=launch.Plan[source.Frontier];var d=launch.Design;var request=command.Request;
        var nextGimbal=AssemblyActuation.Next(d,source.Gimbal,request.GimbalTargetY,request.GimbalTargetZ,request.Ticks);
        var f=Double3.Zero;var t=Double3.Zero;
        if(request.MainOn){var w=AssemblyActuation.Main(d,source.Gimbal.ActualY,source.Gimbal.ActualZ);f=w.Force;t=w.MomentAtOrigin;}
        for(var i=0;i<d.Jets.Length;i++)if((command.Jets&(1<<i))!=0)
        {var w=AssemblyActuation.Jet(d.Jets[i]);f+=w.Force;t+=w.MomentAtOrigin;}
        var used=AssemblyResources.Calculate(d,source.Stores,command.ExtentRate,request.Ticks);var wrench=new AssemblyWrench(f,t);
        var motion=AssemblyDynamics.Evaluate(d,source.Motion,used,wrench);var available=!used.After.Fuel.IsZero;
        var actual=new AssemblyRealization(available&&request.MainOn,available?command.Jets:(ushort)0,
            command.ExtentRate.IsZero?AssemblyFeedState.NoDemand:available?AssemblyFeedState.Available:AssemblyFeedState.Exhausted);
        var successor=new AssemblyRuntimeState(new(checked(source.Epoch.Ticks+request.Ticks)),used.After,AssemblyLaunch.ObserveMass(d,used.After),
            motion,nextGimbal,request,actual,source.Frontier+1,checked(source.ResourceRevision+(used.ConsumedTotal.IsZero?0UL:1UL)),checked(source.ActuatorRevision+1));
        return new(source.Frontier,request,source.Gimbal,used.Powered.IsZero?default:wrench,used.Powered,used.Classification,successor,revision,new(checked(revision.Value+1)),timeline);
    }
}
