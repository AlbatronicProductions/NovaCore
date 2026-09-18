using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold application composition. All mutable physical authority stays in the existing engine/state/clock.</summary>
internal sealed class AssemblyApplicationSession : IDisposable
{
    internal AssemblyLaunch Launch {get;}
    internal ReferenceFrameGraph Frames {get;}
    internal SimulationClock Clock {get;}
    internal SimulationTransactionEngine Engine {get;}
    internal AssemblyFlightAuthority Authority {get;}
    private AssemblyApplicationSession(AssemblyLaunch launch,int capacity,SimulationRate rate,StateRevision revision,bool contact=false)
    {
        Launch=launch;
        var builder=new ReferenceFrameGraphBuilder();
        if(launch.Site is {} site)
        {
            builder.Add(new ReferenceFrameNode(site.EarthFrame,null,ReferenceFrameKind.Ecl,"Earth-centred nonrotating carrier"));
            builder.Add(new ReferenceFrameNode(launch.Spacecraft.CarrierFrame,site.EarthFrame,ReferenceFrameKind.Ccf,"Florida physical site"));
        }
        else builder.Add(new ReferenceFrameNode(launch.Spacecraft.CarrierFrame,null,ReferenceFrameKind.Ecl,"Assembly inertial root"));
        builder.Add(new ReferenceFrameNode(launch.Spacecraft.BodyFrame,launch.Spacecraft.CarrierFrame,ReferenceFrameKind.Ccf,"Assembly material origin O"));
        Frames=builder.Build();
        Clock=new(launch.Initial.Epoch,new SimulationTimeline(4),rate);
        Engine=new(Clock,new SimulationState(spacecraft:SpacecraftStateStore.CreateAssemblies([launch],Frames),initialRevision:revision));
        AssemblyFlightAuthority? authority;
        var admitted=contact?Engine.BeginAssemblyContact(launch,capacity,out authority):Engine.BeginAssemblyFlight(launch,capacity,out authority);
        if(admitted!=AssemblyFlightStatus.Ready)throw new InvalidDataException("Assembly binding refused.");
        Authority=authority!;
    }
    internal static AssemblyApplicationSession Create(AssemblyLaunch launch,int historyCapacity=128,SimulationRate rate=default,StateRevision initialRevision=default)=>
        new(launch,historyCapacity,rate==default?SimulationRate.One:rate,initialRevision);
    internal static AssemblyApplicationSession CreateSupported(AssemblyLaunch launch,int historyCapacity=1200,StateRevision initialRevision=default)=>
        new(launch,historyCapacity,SimulationRate.One,initialRevision,true);
    public void Dispose(){if(Launch.Consumer==AssemblyPhysicalConsumer.SupportedContact)Engine.DisposeAssemblyContact(Authority);}
    internal byte[] Save()=>Engine.SaveAssemblyFlight(Authority);
    internal static AssemblyApplicationSession Restore(AssemblyStockCatalog catalog,ReadOnlySpan<byte> bytes)=>SimulationTransactionEngine.RestoreAssemblyFlight(catalog,bytes);
}
