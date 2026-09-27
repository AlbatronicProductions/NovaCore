using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Immutable preparation; the existing construction state and engine own the live instance.</summary>
internal sealed class ConstructionPhysicalBinding
{
    internal CompiledCraft Craft {get;}
    internal CompiledCraftContact Contact {get;}
    internal CompiledCraftControl Control {get;}
    internal ConstructionPhysicalServices Services {get;}
    internal AssemblyFloridaSite Site {get;}
    internal CraftClearance Clearance {get;}
    internal CraftSupportSolution Support {get;}
    internal ConstructionPhysicalBinding(CompiledCraft craft,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab,SimulationInstant epoch)
    {
        Require(craft.Function&&craft.AdmissionDiagnostics.IsEmpty,"Craft physical admission requires FUNCTION and support.");
        Craft=craft;Contact=new(craft);Control=new(craft);Services=new(craft);
        Site=AssemblyFloridaSite.CreateCraftSlab(query,epoch,new(3),slab,Contact);
        Clearance=new(craft,Control,Contact,Site);
        var mass=craft.InitialMass;
        var position=AssemblyContactProfile.Upright.Rotate(mass.Com);
        var acceleration=Site.LinearAcceleration(Site.At(epoch),position,default);
        Require(acceleration.IsFinite&&acceleration.Y<0&&Math.Sqrt(acceleration.LengthSquared)<=9.81&&
            Math.Sqrt(acceleration.X*acceleration.X+acceleration.Z*acceleration.Z)<=-acceleration.Y*Math.Tan(Math.PI/90),
            "Florida gravity leaves the qualified support load/tilt envelope.");
        // The compiled collision is independent of render geometry; every solid
        // must fit the authenticated finite slab at the cold upright pose.
        foreach(var shape in craft.Collision)foreach(var v in shape.Vertices)
            Require(Math.Abs(v.Y)+Contact.ContactTolerance<slab.Dimensions.X*.5&&Math.Abs(v.Z)+Contact.ContactTolerance<slab.Dimensions.Z*.5,
                "Craft physical geometry leaves the Florida support slab.");
        Support=CraftSupportPreparation.Prepare(Contact,mass,acceleration,Site,epoch);
    }
}

internal sealed record ConstructionPhysicalState(AssemblyMotion Motion,AssemblyGimbal Gimbal,AssemblyPhysicalConsumer Consumer);
internal sealed record ConstructionControlCheckpoint(ConstructionRuntimeState State,StateRevision Revision,ContinuationClockState Clock,
    long LastSequence,AssemblyControlRequest Requested,long ArbitrationFrontier,bool OffAtFrontier);
internal sealed record ConstructionPhysicalRecord(ConstructionRuntimeState Before,ConstructionRuntimeState After,
    AssemblyControlRequest Request,StateRevision Revision,long HostSequence,int Contacts,double Clearance);
internal sealed record ConstructionPhysicalHistory(ConstructionControlCheckpoint Checkpoint,
    System.Collections.Immutable.ImmutableArray<ConstructionPhysicalRecord> Records);
// An immutable endpoint-availability view, not the preceding interval's integrated
// output. Captured command/resources belong to this compiled craft and remain
// valid after subsequent publication. No per-observation collection allocation.
internal readonly struct ConstructionJetObservation
{
    private readonly ConstructionPhysicalServices? services;
    private readonly CraftControlRow? row;
    private readonly ConstructionFuelState? fuel;
    private readonly ConstructionPowerState? power;
    internal int Length=>services?.Craft.Allocation.JetActuators.Length??0;
    internal int Count {get;}
    internal bool IsEmpty=>Count==0;
    internal bool Contains(int index)
    {
        if((uint)index>=(uint)Length)return false;
        var a=services!.Craft.Actuators[services.Craft.Allocation.JetActuators[index]];
        return row!.Consumers[a.Consumer]&&services.Available(a,fuel!,power!);
    }
    internal ConstructionJetObservation(ConstructionPhysicalServices services,CraftControlRow row,ConstructionFuelState fuel,ConstructionPowerState power)
    {
        AssemblyConstructionFacts.Require(ReferenceEquals(row.Craft,services.Craft)&&ReferenceEquals(fuel.Network,services.Craft.Fuel)&&ReferenceEquals(power.Network,services.Craft.Power)&&row.Consumers.Length==services.Craft.Fuel.Consumers.Length,"Foreign jet observation source.");
        this.services=services;this.row=row;this.fuel=fuel;this.power=power;Count=0;
        for(var i=0;i<Length;i++)if(Contains(i))Count++;
    }
}
internal readonly record struct ConstructionActuationObservation(bool Main,ConstructionJetObservation Jets,bool GimbalPowered);

internal sealed partial class ConstructionRuntimeBinding
{
    internal ConstructionPhysicalBinding? Physical {get;}
    internal ConstructionRuntimeBinding(CompiledCraft craft,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab,SimulationInstant epoch)
        :this(craft.Design,epoch,new(craft,query,slab,epoch)){}
    internal ConstructionRuntimeBinding(CompiledCraft craft,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab,ConstructionFlightCheckpoint checkpoint)
        :this(craft.Design,new(checkpoint.EpochTicks),new(craft,query,slab,new(checkpoint.SiteStartTicks)),checkpoint){}
}

internal sealed partial class ConstructionApplicationSession
{
    private ConstructionApplicationSession(ConstructionRuntimeBinding binding,StateRevision revision,ConstructionFlightCheckpoint? checkpoint=null)
    {
        Binding=binding;var physical=binding.Physical??throw new InvalidDataException("Missing physical binding.");
        var frames=new ReferenceFrameGraphBuilder();
        frames.Add(new ReferenceFrameNode(physical.Site.EarthFrame,null,ReferenceFrameKind.Ecl,"Earth-centred nonrotating carrier"));
        frames.Add(new ReferenceFrameNode(binding.Spacecraft.CarrierFrame,physical.Site.EarthFrame,ReferenceFrameKind.Ccf,"Florida physical site"));
        frames.Add(new ReferenceFrameNode(binding.Spacecraft.BodyFrame,binding.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"Craft material origin"));Frames=frames.Build();
        Clock=new(binding.Initial.Epoch,new SimulationTimeline(4),SimulationRate.One);
        Engine=new(Clock,new SimulationState(spacecraft:SpacecraftStateStore.CreateConstruction(binding,Frames),initialRevision:revision));
        Require(Engine.BeginConstructionPhysical(binding,out var authority,out var control)==ConstructionServiceStatus.Ready,"Craft Florida binding refused.");
        Authority=authority!;Control=control;
        if(checkpoint is not null)
        {
            try{Engine.RestoreConstructionFlightFrontier(Authority,checkpoint);}
            catch{Engine.RetireConstructionServices(Authority);throw;}
        }
    }
    internal static ConstructionApplicationSession CreateSupported(CompiledCraft craft,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab,
        SimulationInstant epoch=default,StateRevision revision=default)=>new(new ConstructionRuntimeBinding(craft,query,slab,epoch),revision);
    internal static ConstructionApplicationSession RestoreFlight(AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes,string assetRoot,IPhysicalSurfacePointQuery query,FloridaSlabSupport slab)
    {
        var checkpoint=AssemblyJson.Read<ConstructionFlightCheckpoint>(bytes,SimulationTransactionEngine.ConstructionSaveMaximumBytes);
        checkpoint.Validate();
        var craft=CraftCompiler.Compile(catalog,checkpoint.Design,assetRoot);
        return new(new ConstructionRuntimeBinding(craft,query,slab,checkpoint),new(checkpoint.Revision),checkpoint);
    }
}
