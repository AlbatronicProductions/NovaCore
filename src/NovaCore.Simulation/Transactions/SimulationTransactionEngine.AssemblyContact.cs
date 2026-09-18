using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal AssemblyFlightStatus CheckAssemblyContactSource(AssemblyFlightAuthority authority) =>
        OwnsAssemblyConsumer(authority,AssemblyPhysicalConsumer.SupportedContact)?CheckAssemblySource(authority):AssemblyFlightStatus.InvalidAuthority;

    internal AssemblyFlightStatus PrepareAssemblyContact(AssemblyFlightAuthority authority,out AssemblyFlightProposal proposal,bool refuseForTest=false)
    {
        proposal=default;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try{return !OwnsAssemblyConsumer(authority,AssemblyPhysicalConsumer.SupportedContact)?AssemblyFlightStatus.InvalidAuthority:PrepareAssemblyInOwnedPhase(authority,out proposal,refuseForTest);}
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightResult PublishAssemblyContact(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal,bool failAcknowledgementForTest=false,bool refuseForTest=false)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try
        {
            if(!OwnsAssemblyConsumer(authority,AssemblyPhysicalConsumer.SupportedContact))return new(AssemblyFlightStatus.InvalidAuthority);
            if(refuseForTest)return new(AssemblyFlightStatus.PreparationRefused);
            return PublishAssemblyInOwnedPhase(authority,proposal,failAcknowledgementForTest);
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal LocalContactWorld? AssemblyContactWorldForTest(AssemblyFlightAuthority authority) =>
        ReferenceEquals(_assemblyFlight?.Authority,authority)?_assemblyFlight?.ContactWorld:null;
    internal void DisposeAssemblyContact(AssemblyFlightAuthority authority)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)throw new InvalidOperationException("Contact disposal outside owner phase.");
        try
        {
            if(_assemblyFlight is not {} p||!ReferenceEquals(p.Authority,authority)||p.ContactWorld is null)return;
            p.Invalidated=true;p.ContactWorld.Dispose();
        }
        finally{_clock.PublicationPhase.Exit();}
    }
}
