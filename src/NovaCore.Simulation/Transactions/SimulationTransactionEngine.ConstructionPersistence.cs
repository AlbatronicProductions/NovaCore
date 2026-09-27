using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal byte[] SaveConstructionFlight(ConstructionServiceAuthority authority)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Physical save outside owner phase.");
        try
        {
            if(CheckConstruction(authority)!=ConstructionServiceStatus.Ready||authority.Binding.Physical is not {} physical)
                throw new InvalidDataException("Retired or stale physical save.");
            var p=_construction!;var c=p.Control!;var state=p.Expected;
            var checkpoint=new ConstructionFlightCheckpoint(ConstructionFlightCheckpoint.SchemaId,authority.Binding.FlightId,authority.Binding.Design.Data,
                physical.Craft.Digest,physical.Site.Authority,physical.Site.Digest,physical.Site.Start.Ticks,state.Epoch.Ticks,state.Sequence,p.Revision.Value,
                state.Fuel.Save(),state.Power.Save(),state.ReferenceMass!.Value,state.Physical!,c.BaseSequence+c.Count,c.Requested,c.ArbitrationFrontier,c.OffAtFrontier,
                p.HostSequence,p.LastHostTicks,p.Clock.Debt.Ticks);
            checkpoint.Validate();var bytes=AssemblyJson.Write(checkpoint);
            if(bytes.Length>ConstructionSaveMaximumBytes)throw new InvalidDataException("Physical save capacity exceeded.");return bytes;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal void RestoreConstructionFlightFrontier(ConstructionServiceAuthority authority,ConstructionFlightCheckpoint saved)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Physical restore outside owner phase.");
        try
        {
            saved.Validate();
            if(CheckConstruction(authority)!=ConstructionServiceStatus.Ready||!ReferenceEquals(authority.Binding.AdmittedFlightCheckpoint,saved)||_construction!.FlightFrontierRestored||_construction.HostSequence!=0||
                _construction.Control!.Count!=0||_construction.Expected.Epoch.Ticks!=saved.EpochTicks)
                throw new InvalidDataException("Physical restore frontier requires a fresh private session.");
            var p=_construction;var c=p.Control;p.FlightFrontierRestored=true;
            _clock.InstallCertifiedContinuation(new(saved.EpochTicks),new(saved.DebtTicks));
            p.Clock=CaptureContinuationClock();p.HostSequence=saved.HostSequence;p.LastHostTicks=saved.LastHostTicks;
            c.BaseSequence=saved.ControlSequence;c.Requested=saved.Requested;c.ArbitrationFrontier=saved.ArbitrationFrontier;c.OffAtFrontier=saved.OffAtFrontier;
            p.PhysicalCheckpoint=new(p.Expected,p.Revision,p.Clock,c.BaseSequence,c.Requested,c.ArbitrationFrontier,c.OffAtFrontier);
            p.ContactWorld?.AcknowledgeCraftCredit(p.Clock);
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal long ObserveConstructionHostSequence(ConstructionServiceAuthority authority)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Host sequence outside owner phase.");
        try{if(CheckConstruction(authority)!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Stale physical host sequence.");return _construction!.HostSequence;}
        finally{_clock.PublicationPhase.Exit();}
    }
}
