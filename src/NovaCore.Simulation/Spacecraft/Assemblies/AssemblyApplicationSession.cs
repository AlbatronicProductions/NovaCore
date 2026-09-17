using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold application composition. All mutable physical authority stays in the existing engine/state/clock.</summary>
internal sealed class AssemblyApplicationSession
{
    internal AssemblyLaunch Launch {get;}
    internal ReferenceFrameGraph Frames {get;}
    internal SimulationClock Clock {get;}
    internal SimulationTransactionEngine Engine {get;}
    internal AssemblyFlightAuthority Authority {get;}
    private AssemblyApplicationSession(AssemblyLaunch launch,int capacity,SimulationRate rate,StateRevision revision)
    {
        Launch=launch;
        var builder=new ReferenceFrameGraphBuilder();
        builder.Add(new ReferenceFrameNode(launch.Spacecraft.CarrierFrame,null,ReferenceFrameKind.Ecl,"Assembly inertial root"));
        builder.Add(new ReferenceFrameNode(launch.Spacecraft.BodyFrame,launch.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"Assembly material origin O"));
        Frames=builder.Build();
        Clock=new(launch.Initial.Epoch,new SimulationTimeline(4),rate);
        Engine=new(Clock,new SimulationState(spacecraft:SpacecraftStateStore.CreateAssemblies([launch],Frames),initialRevision:revision));
        if(Engine.BeginAssemblyFlight(launch,capacity,out var authority)!=AssemblyFlightStatus.Ready)throw new InvalidDataException("Assembly binding refused.");
        Authority=authority!;
    }
    internal static AssemblyApplicationSession Create(AssemblyLaunch launch,int historyCapacity=128,SimulationRate rate=default,StateRevision initialRevision=default)=>
        new(launch,historyCapacity,rate==default?SimulationRate.One:rate,initialRevision);
    internal byte[] Save()=>Engine.SaveAssemblyFlight(Authority);
    internal static AssemblyApplicationSession Restore(AssemblyStockCatalog catalog,ReadOnlySpan<byte> bytes)=>SimulationTransactionEngine.RestoreAssemblyFlight(catalog,bytes);
}
