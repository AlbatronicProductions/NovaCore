using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using System.Collections.Immutable;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private static AssemblyControlStatus ConstructionControlStatus(ConstructionServiceStatus status)=>status switch
    {
        ConstructionServiceStatus.Ready=>AssemblyControlStatus.Ready,
        ConstructionServiceStatus.Retired=>AssemblyControlStatus.Retired,
        ConstructionServiceStatus.WrongOwnerThread=>AssemblyControlStatus.WrongOwnerThread,
        ConstructionServiceStatus.Reentrant=>AssemblyControlStatus.Reentrant,
        ConstructionServiceStatus.InvalidAuthority=>AssemblyControlStatus.InvalidAuthority,
        _=>AssemblyControlStatus.StaleSource
    };
    private AssemblyControlResult AdmitConstructionControlInOwnedPhase(AssemblyControlAuthority control,AssemblyControlIdentity identity,long sequence,AssemblyControlRequest request)
    {
        var p=_construction!;var c=p.Control!;
        if(c.Retired)return new(AssemblyControlStatus.Retired);
        if(identity!=control.Identity)return new(AssemblyControlStatus.InvalidIdentity);
        var source=CheckConstruction(p.Authority);if(source!=ConstructionServiceStatus.Ready)return new(ConstructionControlStatus(source));
        if(!request.Pilot.IsValid||request.PilotOnly&&request.MainOn)return new(AssemblyControlStatus.InvalidInput);
        var total=c.BaseSequence+c.Count;
        if(sequence<=c.BaseSequence)return new(AssemblyControlStatus.InvalidSequence);
        if(sequence<=total){var prior=c.Journal[(int)(sequence-c.BaseSequence)-1];return prior.Requested==request?new(AssemblyControlStatus.Duplicate,prior):new(AssemblyControlStatus.InvalidSequence);}
        if(total==long.MaxValue)return new(AssemblyControlStatus.Capacity);
        if(sequence!=total+1)return new(AssemblyControlStatus.InvalidSequence);
        ConstructionControlCheckpoint? checkpoint=null;
        if(c.Count==c.Journal.Length)checkpoint=new(p.Expected,p.Revision,p.Clock,total,c.Requested,c.ArbitrationFrontier,c.OffAtFrontier);
        var frontier=p.Expected.Sequence;
        var off=(c.ArbitrationFrontier==frontier&&c.OffAtFrontier)||(!request.PilotOnly&&!request.MainOn);
        var effective=new AssemblyControlRequest(request.PilotOnly?c.Requested.MainOn:request.MainOn&&!off,request.Pilot);
        var record=new AssemblyControlAdmission(identity,sequence,frontier,p.HostSequence,request,effective);
        // Bounded admission checkpoint; no physical restore or private solver
        // cache serialization is implied. Expired receipts can never re-admit.
        if(checkpoint is not null){p.ControlCheckpoint=checkpoint;c.BaseSequence=total;c.Count=0;Array.Clear(c.Journal);}
        c.Journal[c.Count++]=record;c.ArbitrationFrontier=frontier;c.OffAtFrontier=off;c.Requested=effective;
        return new(AssemblyControlStatus.Admitted,record);
    }
    internal ConstructionServiceStatus BeginConstructionPhysical(ConstructionRuntimeBinding binding,out ConstructionServiceAuthority? authority,out AssemblyControlAuthority? control)
    {
        control=null;var status=BeginConstruction(binding,1,true,out authority);
        if(status==ConstructionServiceStatus.Ready)control=_construction!.Control!.Authority;
        return status;
    }
    internal ConstructionServiceStatus CheckConstructionContactSource(ConstructionServiceAuthority authority)=>
        authority.Binding.Physical is not null&&_construction?.Expected.Physical is not null?
            CheckConstruction(authority):ConstructionServiceStatus.InvalidAuthority;
    internal ConstructionRuntimeState CaptureConstructionContactState(ConstructionServiceAuthority authority)
    {
        if(!OwnsPersistentPublicationPhase||CheckConstructionContactSource(authority)!=ConstructionServiceStatus.Ready)
            throw new InvalidDataException("Contact capture requires the current construction publication owner.");
        return _construction!.Expected;
    }
    internal LocalContactWorld? ConstructionContactWorldForTest(ConstructionServiceAuthority authority)=>
        ReferenceEquals(_construction?.Authority,authority)?_construction?.ContactWorld:null;
    internal string? ConstructionSupportFailure(ConstructionServiceAuthority authority)=>
        _clock.PublicationPhase.IsOwnerThread&&ReferenceEquals(_construction?.Authority,authority)?_construction?.ContactWorld?.CraftSupportFailure:null;
    internal ConstructionServiceStatus AdmitConstructionHostTime(ConstructionServiceAuthority authority,long sequence,SimulationDuration duration)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            var status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
            var p=_construction!;var physical=authority.Binding.Physical;
            if(physical is null||duration.Ticks<=0)return ConstructionServiceStatus.InvalidInput;
            if(sequence==p.HostSequence&&sequence>0)return duration.Ticks==p.LastHostTicks?ConstructionServiceStatus.Duplicate:ConstructionServiceStatus.InvalidSequence;
            if(p.HostSequence==long.MaxValue)return ConstructionServiceStatus.Overflow;
            if(sequence!=p.HostSequence+1)return ConstructionServiceStatus.InvalidSequence;
            var credit=_clock.PrepareHostAdvance(duration);
            if(credit.Reason!=SimulationHostAdvanceStopReason.Accepted||(Int128)_clock.CurrentTime.Ticks+credit.DebtAfter.Ticks>physical.Site.End.Ticks)
                return ConstructionServiceStatus.Overflow;
            var target=new SimulationInstant(_clock.CurrentTime.Ticks+credit.DebtAfter.Ticks);
            if(HasContactProofBoundaryThrough(target))return ConstructionServiceStatus.PendingEvent;
            _clock.InstallHostAdvance(credit);p.HostSequence=sequence;p.LastHostTicks=duration.Ticks;p.Clock=CaptureContinuationClock();
            p.ContactWorld?.AcknowledgeCraftCredit(p.Clock);
            return ConstructionServiceStatus.AcceptedCredit;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus ServiceConstructionDebt(ConstructionServiceAuthority authority,out int published,bool refusePreparationForTest=false,bool refusePublicationForTest=false)
    {
        published=0;var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            for(var i=0;i<4;i++)
            {
                var status=StepConstructionInOwnedPhase(authority,refusePreparationForTest,refusePublicationForTest);
                if(status!=ConstructionServiceStatus.Published)return status==ConstructionServiceStatus.AwaitingDebt&&published>0?ConstructionServiceStatus.Published:status;
                published++;
            }
            return ConstructionServiceStatus.Published;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    private ConstructionServiceStatus StepConstructionInOwnedPhase(ConstructionServiceAuthority authority,bool refusePreparation,bool refusePublication)
    {
        const long ticks=15625; // 64 Hz authority cadence; independent of host-frame partitions.
        var status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
        var p=_construction!;var profile=authority.Binding.Physical;
        if(profile is null||p.Control is null||p.Expected.Physical is not {} physical)return ConstructionServiceStatus.InvalidInput;
        if(_clock.PendingSimulationDebt.Ticks<ticks)return ConstructionServiceStatus.AwaitingDebt;
        if(p.Revision.Value==ulong.MaxValue||p.Expected.Sequence==long.MaxValue||p.Expected.Epoch.Ticks>profile.Site.End.Ticks-ticks)return ConstructionServiceStatus.Overflow;
        var target=new SimulationInstant(p.Expected.Epoch.Ticks+ticks);
        if(HasContactProofBoundaryThrough(target))return ConstructionServiceStatus.PendingEvent;
        if(!_state.TryPrepareConstructionSlot(authority.Binding,p.Expected,out var slot))return ConstructionServiceStatus.StaleSource;
        var request=p.Control.Requested;var row=profile.Control.Resolve(request);
        ConstructionPhysicalEvolution services;AssemblyMass mass;AssemblyMotion motion=physical.Motion;AssemblyGimbal gimbal=physical.Gimbal;
        var consumer=physical.Consumer;var clearance=double.NegativeInfinity;var contacts=0;var importedContact=false;
        ConstructionControlCheckpoint? checkpoint=null;
        try
        {
            using(ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.Resources)){
            services=profile.Services.Advance(p.Expected.Fuel,p.Expected.Power,ticks,row.Consumers.AsSpan());
            mass=profile.Craft.Mass.Evaluate(ConstructionNumerics.Quantities(services.Fuel));}
            if(p.PhysicalCount==p.PhysicalHistory!.Length)
                checkpoint=new(p.Expected,p.Revision,p.Clock,p.Control.BaseSequence+p.Control.Count,request,p.Control.ArbitrationFrontier,p.Control.OffAtFrontier);
            if(refusePreparation)return ConstructionServiceStatus.PreparationRefused;
            if(consumer==AssemblyPhysicalConsumer.FreeFlight)
            {
                if(profile.Clearance.Clears(motion,p.Expected.ReferenceMass!.Value,p.Expected.Epoch,ticks,out clearance))
                {
                    using var freeFlightTiming=ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.FreeFlight);
                    var next=AssemblyDynamics.Evaluate(profile.Control,motion,services,gimbal,row,default,site:profile.Site,epoch:p.Expected.Epoch);
                    motion=next.Motion;gimbal=next.Gimbal;
                    // A restored endpoint may have prepared a private world
                    // during admission. A clear free-flight successor needs no
                    // native continuation, and must not retain its old frontier.
                    p.ContactWorld?.Dispose();p.ContactWorld=null;p.ContactReceipt=default;
                }
                else
                {
                    // Failure of a clearance proof is a request for collision
                    // detection, not a landing event. Import the current exact
                    // endpoint before advancing the uncertified interval.
                    if(p.ContactWorld is null)
                    {
                        var config=LocalContactConfiguration.CreateCraft(authority.Binding,true,motion,mass:p.Expected.ReferenceMass);
                        var source=LocalContactSource.CaptureCraft(this,authority,config);
                        using var worldTiming=ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.World);
                        if(ConstructionWorkProbe.Current is {} creationProbe)creationProbe.Creates++;
                        var imported=LocalContactWorld.TryCreate(this,source,config,out var world,out var initial);
                        if(imported!=LocalContactStatus.Success)return ConstructionServiceStatus.ClearanceRefused;
                        p.ContactWorld=world;p.ContactConfiguration=config;p.ContactReceipt=initial;
                        importedContact=true;
                    }
                    consumer=AssemblyPhysicalConsumer.SurfaceContact;
                }
            }
        }
        catch(Exception e) when(e is InvalidDataException or OverflowException){return ConstructionServiceStatus.PreparationRefused;}
        // Exact resources and history checkpoint are prepared before private
        // contact mutation. Remaining proposal allocation is guarded below;
        // a failed private solve or post-solve preparation is never reused.
        var receipt=p.ContactReceipt;
        ConstructionRuntimeState successor;ConstructionPhysicalRecord record;
        var revision=new StateRevision(p.Revision.Value+1);
        try
        {
        if(consumer is AssemblyPhysicalConsumer.SupportedContact or AssemblyPhysicalConsumer.SurfaceContact)
        {
            var world=p.ContactWorld!;
            var step=world.StepCraft(this,receipt,target,services,gimbal,row,out receipt,out motion,out gimbal);
            while(step!=LocalContactStatus.Success&&consumer==AssemblyPhysicalConsumer.SurfaceContact&&world.CraftMotionRefinementRequested&&
                p.ContactConfiguration!.CraftRefinement<CraftSurfaceImpact.MaximumRefinement)
            {
                // A contact can redistribute linear/angular energy. Refine
                // from the untouched canonical endpoint, never from a failed
                // native slice; exact resources are the same prepared proposal.
                var config=LocalContactConfiguration.CreateCraft(authority.Binding,true,physical.Motion,p.ContactConfiguration.CraftRefinement+1,p.Expected.ReferenceMass);
                var source=LocalContactSource.CaptureCraft(this,authority,config);
                if(ConstructionWorkProbe.Current is {} disposeProbe)disposeProbe.Disposes++;
                world.Dispose();
                LocalContactStatus imported;LocalContactWorld? refined;LocalContactWorld.Receipt initial;
                using(ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.World)){
                    if(ConstructionWorkProbe.Current is {} creationProbe)creationProbe.Creates++;
                    imported=LocalContactWorld.TryCreate(this,source,config,out refined,out initial);
                }
                if(imported!=LocalContactStatus.Success){p.Invalidated=true;return ConstructionServiceStatus.Invalidated;}
                world=refined!;p.ContactWorld=world;p.ContactConfiguration=config;p.ContactReceipt=initial;
                step=world.StepCraft(this,initial,target,services,physical.Gimbal,row,out receipt,out motion,out gimbal);
            }
            if(step!=LocalContactStatus.Success)
            {
                p.Invalidated=world.CraftInvalidated;
                // A pre-solve refusal has no canonical successor. Retire a new
                // import now so a retry cannot leak or reuse that private world.
                if(importedContact&&!p.Invalidated){if(ConstructionWorkProbe.Current is {} disposeProbe)disposeProbe.Disposes++;
                world.Dispose();p.ContactWorld=null;p.ContactReceipt=default;}
                return p.Invalidated?ConstructionServiceStatus.Invalidated:ConstructionServiceStatus.PreparationRefused;
            }
            if(world.CheckCraftPublication(this,receipt,mass)!=LocalContactStatus.Success){p.Invalidated=true;world.InvalidateCraft();return ConstructionServiceStatus.Invalidated;}
            contacts=world.CraftContactCount;
            if(contacts==0&&profile.Clearance.Clears(motion,mass,target,ticks,out clearance))consumer=AssemblyPhysicalConsumer.FreeFlight;
        }
        if(refusePublication)throw new InvalidDataException("Qualification post-solve preparation refusal.");
        successor=new ConstructionRuntimeState(services.Fuel,services.Power,target,p.Expected.Sequence+1,mass){Physical=new(motion,gimbal,consumer)};
        record=new ConstructionPhysicalRecord(p.Expected,successor,request,revision,p.HostSequence,contacts,clearance);
        status=CheckConstruction(authority);
        if(status!=ConstructionServiceStatus.Ready){p.Invalidated=true;p.ContactWorld?.InvalidateCraft();return status;}
        }
        catch(Exception)
        {
            if(p.ContactWorld is {} world){p.Invalidated=true;world.InvalidateCraft();return ConstructionServiceStatus.Invalidated;}
            return ConstructionServiceStatus.PreparationRefused;
        }
        // Fixed publication under the existing engine/clock phase. Consumer
        // transfers preserve the exact material-origin state in both directions.
        using var publicationTiming=ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.Publication);
        if(ConstructionWorkProbe.Current is {} publicationProbe)publicationProbe.Intervals++;
        _state.InstallConstruction(slot,successor,revision);
        _clock.InstallCertifiedContinuation(target,new(_clock.PendingSimulationDebt.Ticks-ticks));
        p.Expected=successor;p.Revision=revision;p.Clock=CaptureContinuationClock();p.ContactReceipt=receipt;
        if(checkpoint is not null){p.PhysicalCheckpoint=checkpoint;Array.Clear(p.PhysicalHistory!);p.PhysicalCount=0;}
        p.PhysicalHistory![p.PhysicalCount++]=record;
        if(p.ContactWorld is {} contact)
        {
            contact.AcknowledgeCraft(revision,p.Clock);
            if(consumer==AssemblyPhysicalConsumer.FreeFlight){if(ConstructionWorkProbe.Current is {} finalDisposeProbe)finalDisposeProbe.Disposes++;contact.Dispose();p.ContactWorld=null;}
        }
        return ConstructionServiceStatus.Published;
    }
    internal ConstructionPhysicalHistory ObserveConstructionPhysicalHistory(ConstructionServiceAuthority authority)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Physical history outside owner phase.");
        try
        {
            if(CheckConstruction(authority)!=ConstructionServiceStatus.Ready||_construction!.PhysicalHistory is null)throw new InvalidDataException("Unavailable physical history.");
            var p=_construction!;return new(p.PhysicalCheckpoint!,p.PhysicalHistory.Take(p.PhysicalCount).Select(r=>r!).ToImmutableArray());
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus ObserveConstructionActuation(ConstructionServiceAuthority authority,out ConstructionActuationObservation observation)
    {
        observation=default;var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            var status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
            var p=_construction!;var profile=authority.Binding.Physical;
            if(profile is null||p.Control is null)return ConstructionServiceStatus.InvalidInput;
            var row=profile.Control.Resolve(p.Control.Requested);var main=profile.Control.Main;var services=profile.Services;
            var fuel=p.Expected.Fuel;var power=p.Expected.Power;
            var jets=new ConstructionJetObservation(services,row,fuel,power);
            observation=new(row.Consumers[main.Consumer]&&services.Available(main,fuel,power),jets,services.Powered(main,power));return ConstructionServiceStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus ObserveConstructionContactPoints(ConstructionServiceAuthority authority,out int points)
    {
        points=0;var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            var status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
            var p=_construction!;if(p.PhysicalHistory is null)return ConstructionServiceStatus.InvalidInput;
            // This observes the last published interval, not the private solver
            // workspace or its choice of integration owner. A fresh restored
            // endpoint has no solved contact observation until it advances.
            if(p.PhysicalCount>0)points=p.PhysicalHistory[p.PhysicalCount-1]!.Contacts;
            return ConstructionServiceStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
}
