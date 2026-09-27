using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record ConstructionActuatorInstance(ConstructionPartIdentity Part,string LocalId,Double3 ForcePoint,Double3 Axis,Double3? GimbalPivot);

/// <summary>Immutable construction binding and cold preparation. Live state and contact publication remain owned by the simulation engine.</summary>
internal sealed partial class ConstructionRuntimeBinding
{
    private static long nextGeneration;
    private int registered;
    internal bool TryClaimRegistration()=>Interlocked.CompareExchange(ref registered,1,0)==0;
    internal CompiledConstructionDesign Design {get;}
    internal ConstructionFuelNetwork Fuel {get;}
    internal ConstructionPowerNetwork Power {get;}
    internal SpacecraftDefinition Spacecraft {get;}
    internal ConstructionVehicleIdentity Identity {get;}
    internal ImmutableArray<ConstructionPartIdentity> Parts {get;}
    internal ImmutableArray<ConstructionSubpartIdentity> Subparts {get;}
    internal ImmutableArray<ConstructionActuatorInstance> Actuators {get;}
    internal ConstructionRuntimeState Initial {get;}
    internal Guid FlightId {get;}
    internal bool RestoredFlight {get;}
    internal ConstructionFlightCheckpoint? AdmittedFlightCheckpoint {get;}
    internal AssemblyMass FullLoadedReference {get;}
    internal bool ServiceConsumptionQualified {get;}
    private readonly ImmutableArray<MassRegionData> dryRegions;
    private readonly ImmutableArray<MassRegionData> loadedRegions;
    internal int DryRegionCount=>dryRegions.Length;
    internal int StoreRegionCount=>loadedRegions.Length;
    internal ConstructionRuntimeBinding(CompiledConstructionDesign design,SimulationInstant epoch):this(design,epoch,null){}
    private ConstructionRuntimeBinding(CompiledConstructionDesign design,SimulationInstant epoch,ConstructionPhysicalBinding? physical,ConstructionFlightCheckpoint? checkpoint=null)
    {
        Physical=physical;Design=design;Fuel=physical?.Craft.Fuel??ConstructionFuelNetwork.Compile(design);Power=physical?.Craft.Power??ConstructionPowerNetwork.Compile(Fuel);
        ServiceConsumptionQualified=design.Parts.All(p=>p.Definition.Construction!.UnqualifiedHardware.IsEmpty);
        static MassRegionData PlaceRegion(string id,AssemblyPose pose,MassRegionData r)
        {
            var tensor=pose.Rotation*r.InertiaAtCom*pose.Rotation.Transpose();
            tensor=new(tensor.A,tensor.B,tensor.C,tensor.B,tensor.E,tensor.F,tensor.C,tensor.F,tensor.I);
            return new(id,r.MassKg,pose.Point(r.Com),tensor);
        }
        dryRegions=design.Parts.SelectMany(p=>p.Definition.Construction!.MassRegions.Select(r=>PlaceRegion(p.Instance.Id+"/"+r.Id,p.Instance.Pose,r))).ToImmutableArray();
        loadedRegions=Fuel.Stores.Select(store=>{
            var part=design.Parts[store.Part];var s=part.Definition.Stores.Single(x=>x.Id==store.Key.Store);
            var geometry=part.Definition.Construction!.StoreGeometry.Single(g=>g.Store==s.Id);
            return PlaceRegion(store.Key.Part+"/"+store.Key.Store,part.Instance.Pose,new(s.Id,s.CapacityKg,s.Datum,geometry.InertiaPerKg*s.CapacityKg));
        }).ToImmutableArray();
        FullLoadedReference=physical is null?Aggregate(dryRegions.Concat(loadedRegions)):
            physical.Craft.Mass.Evaluate(physical.Craft.Mass.Stores.Select(s=>s.CapacityKg).ToArray());
        var fuel=Fuel.Initial();Initial=new(fuel,Power.Initial(),epoch,0,ReferenceMass(fuel))
            {Physical=physical is null?null:new(new(default,default,AssemblyContactProfile.Upright,default),default,AssemblyPhysicalConsumer.SupportedContact)};
        if(physical is not null)
        {
            FlightId=checkpoint?.FlightId??Guid.NewGuid();RestoredFlight=checkpoint is not null;AdmittedFlightCheckpoint=checkpoint;
            if(checkpoint is not null)Initial=checkpoint.RestoreState(this);
        }
        // Observational identity only. It is deliberately absent from deterministic save/replay content.
        var generation=Interlocked.Increment(ref nextGeneration);Require(generation>0,"Construction identity exhausted.");
        Spacecraft=new(new((ulong)generation),new(1),new(2),design.Data.Id);Identity=new(Spacecraft.Id,generation);
        Parts=design.Parts.Select(p=>new ConstructionPartIdentity(Identity,p.Instance.Id)).ToImmutableArray();
        Subparts=design.Parts.SelectMany((p,i)=>p.Definition.Construction!.Subparts.Select(s=>new ConstructionSubpartIdentity(Parts[i],s.Id))).ToImmutableArray();
        Actuators=physical is not null?physical.Craft.Actuators.Select(a=>new ConstructionActuatorInstance(Parts[a.Part],a.Key.Consumer,a.Point,a.Axis,a.Gimbal is {} g?a.PartOrigin+a.PartRotation.Apply(g.Pivot):null)).ToImmutableArray():
            design.Parts.SelectMany((p,i)=>p.Definition.Construction!.Consumers.Select(c=>new ConstructionActuatorInstance(Parts[i],c.Id,
            p.Instance.Pose.Point(c.ForcePoint),p.Instance.Pose.Rotation.Apply(c.Axis),c.Gimbal is {} g?p.Instance.Pose.Point(g.Pivot):null))).ToImmutableArray();
    }
    internal AssemblyMass? ReferenceMass(ConstructionFuelState state)
    {
        Require(ReferenceEquals(state.Network,Fuel),"Foreign inventory for physical witness.");
        if(Physical is {} physical)return physical.Craft.Mass.Evaluate(ConstructionNumerics.Quantities(state));
        // A loaded-region tensor supplies only its authored full state and the empty limit.
        // Partial initial fills and later partial depletion have no qualified spatial mass law.
        for(var i=0;i<Fuel.Stores.Length;i++)if(state.Quantities[i]!=0&&state.Quantities[i]!=Fuel.Stores[i].Capacity*Fuel.Scale*state.Denominator)return null;
        return Aggregate(dryRegions.Concat(loadedRegions.Where((_,i)=>state.Quantities[i]!=0)));
    }
    internal static ConstructionRatio TotalResourceMassInQ(ConstructionFuelState state)=>ConstructionRatio.Create(
        state.Quantities.Aggregate(BigInteger.Zero,(sum,n)=>sum+n),state.Network.Scale*state.Denominator);
}

internal sealed record ConstructionRuntimeState(ConstructionFuelState Fuel,ConstructionPowerState Power,SimulationInstant Epoch,long Sequence,AssemblyMass? ReferenceMass)
{
    internal ConstructionPhysicalState? Physical {get;init;}
}
internal sealed class ConstructionServiceAuthority
{
    internal ConstructionRuntimeBinding Binding {get;}
    internal ConstructionServiceAuthority(ConstructionRuntimeBinding binding)=>Binding=binding;
}
internal readonly record struct ConstructionLoadSetting(int Module,bool Enabled);
internal sealed record ConstructionServiceCommand(long HostTicks,ImmutableArray<bool> Demand,ImmutableArray<ConstructionLoadSetting> Loads);
internal enum ConstructionServiceStatus {Ready,Advanced,Retired,InvalidAuthority,StaleSource,InvalidInput,NoDataPath,UnqualifiedHardware,HistoryCapacity,Overflow,WrongOwnerThread,Reentrant,PendingEvent,PreparationRefused,AcceptedCredit,AwaitingDebt,Published,Duplicate,InvalidSequence,Invalidated,ClearanceRefused}
internal sealed record ConstructionRuntimeSave(string Schema,ConstructionDesignData Design,long InitialTicks,ulong InitialRevision,int Capacity,
    ImmutableArray<ConstructionServiceCommand> Commands,long FinalTicks,ulong FinalRevision,byte[] Fuel,byte[] Power);

/// <summary>Cold application composition over the existing state/clock/transaction engine.</summary>
internal sealed partial class ConstructionApplicationSession : IDisposable
{
    internal ConstructionRuntimeBinding Binding {get;}
    internal ReferenceFrameGraph Frames {get;}
    internal SimulationClock Clock {get;}
    internal SimulationTransactionEngine Engine {get;}
    internal ConstructionServiceAuthority Authority {get;}
    internal AssemblyControlAuthority? Control {get;private set;}
    private bool disposed;
    private ConstructionApplicationSession(CompiledConstructionDesign design,SimulationInstant epoch,int capacity,StateRevision revision)
    {
        Binding=new(design,epoch);var frames=new ReferenceFrameGraphBuilder();
        frames.Add(new ReferenceFrameNode(Binding.Spacecraft.CarrierFrame,null,ReferenceFrameKind.Ecl,"Construction static reference"));
        frames.Add(new ReferenceFrameNode(Binding.Spacecraft.BodyFrame,Binding.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"Construction material origin"));Frames=frames.Build();
        Clock=new(epoch,new SimulationTimeline(4),SimulationRate.One);
        Engine=new(Clock,new SimulationState(spacecraft:SpacecraftStateStore.CreateConstruction(Binding,Frames),initialRevision:revision));
        Require(Engine.BeginConstructionServices(Binding,capacity,out var authority)==ConstructionServiceStatus.Ready,"Construction service binding refused.");Authority=authority!;
    }
    internal static ConstructionApplicationSession Create(CompiledConstructionDesign design,SimulationInstant epoch=default,int capacity=128,StateRevision revision=default)=>new(design,epoch,capacity,revision);
    internal byte[] Save()=>Binding.Physical is null?Engine.SaveConstructionServices(Authority):Engine.SaveConstructionFlight(Authority);
    internal static ConstructionApplicationSession Restore(AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var saved=AssemblyJson.Read<ConstructionRuntimeSave>(bytes,SimulationTransactionEngine.ConstructionSaveMaximumBytes);
        Require(saved.Schema==SimulationTransactionEngine.ConstructionSaveSchema&&!saved.Commands.IsDefault&&saved.Commands.Length<=saved.Capacity&&saved.Fuel is not null&&saved.Power is not null,"Invalid construction runtime save.");
        var design=CompiledConstructionDesign.Compile(catalog,saved.Design);var session=Create(design,new(saved.InitialTicks),saved.Capacity,new(saved.InitialRevision));
        try
        {
            for(var i=0;i<saved.Commands.Length;i++)Require(session.Engine.AdvanceConstructionServices(session.Authority,i,saved.Commands[i])==ConstructionServiceStatus.Advanced,"Construction replay refused.");
            Require(session.Engine.ObserveConstructionServices(session.Authority,out var current)==ConstructionServiceStatus.Ready&&current is not null&&
                current.Epoch.Ticks==saved.FinalTicks&&session.Engine.State.Revision.Value==saved.FinalRevision&&
                current.Fuel.Save().SequenceEqual(saved.Fuel)&&current.Power.Save().SequenceEqual(saved.Power),"Construction replay endpoint mismatch.");
            return session;
        }
        catch{session.Dispose();throw;}
    }
    public void Dispose()
    {
        if(disposed)return;
        if(Engine.RetireConstructionServices(Authority)!=ConstructionServiceStatus.Retired)throw new InvalidOperationException("Construction retirement outside owner phase.");
        disposed=true;
    }
}
