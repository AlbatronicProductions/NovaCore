using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal const int AssemblyHostCreditCapacity=4096;
    internal const int AssemblyContactHostCreditCapacity=4801;
    private sealed class AssemblyFlightStorage(AssemblyFlightAuthority authority,int capacity,StateRevision initialRevision)
    {
        internal readonly AssemblyFlightAuthority Authority=authority;
        internal readonly object Seal=new();
        internal readonly AssemblyFlightRecord[] History=new AssemblyFlightRecord[capacity];
        internal readonly StateRevision InitialRevision=initialRevision;
        internal AssemblyPhysicalConsumer Consumer=authority.Launch.Consumer;
        internal readonly AssemblyHostCredit[] Credits=new AssemblyHostCredit[authority.Launch.Consumer==AssemblyPhysicalConsumer.FreeFlight?AssemblyHostCreditCapacity:AssemblyContactHostCreditCapacity];
        internal LocalContactWorld? ContactWorld;
        internal LocalContactConfiguration? ContactConfiguration;
        internal LocalContactWorld.Receipt ContactReceipt;
        internal int CreditCount;
        internal int Count;
        internal long Generation;
        // Canonical host-credit identity installs alongside debt before private
        // acknowledgement. A saved committed credit cannot be credited twice.
        internal long CanonicalHostSequence=>CreditCount;
        internal bool Active,Invalidated;
        internal AssemblyFlightRecord Prepared;
        internal AssemblyRuntimeState ExpectedState;
        internal StateRevision ExpectedRevision;
        internal TimelineRevision ExpectedTimeline;
        internal ContinuationClockState ExpectedClock;
    }
    private AssemblyFlightStorage? _assemblyFlight;
    private bool OwnsAssemblyConsumer(AssemblyFlightAuthority? authority,AssemblyPhysicalConsumer consumer)=>
        authority is not null&&_assemblyFlight is {} p&&ReferenceEquals(p.Authority,authority)&&p.Consumer==consumer;
    private AssemblyFlightStatus EnterAssemblyPhase()
    {
        if(!_clock.PublicationPhase.IsOwnerThread)return AssemblyFlightStatus.WrongOwnerThread;
        return _isExecutingGroup||!_clock.PublicationPhase.TryEnter(this)?AssemblyFlightStatus.Reentrant:AssemblyFlightStatus.Ready;
    }
    private AssemblyFlightStatus CheckAssemblySource(AssemblyFlightAuthority? authority)
    {
        var p=_assemblyFlight;
        if(authority is null||p is null||!ReferenceEquals(p.Authority,authority))return AssemblyFlightStatus.InvalidAuthority;
        if(p.Invalidated)return AssemblyFlightStatus.Invalidated;
        if(authority.Launch.Site is {} site&&!site.Applicable)return AssemblyFlightStatus.StaleSource;
        var view=_state.CreateView();
        if(view.Revision!=p.ExpectedRevision||_clock.Timeline.Revision!=p.ExpectedTimeline||CaptureContinuationClock()!=p.ExpectedClock||
            !view.Spacecraft.TryGetAssembly(authority.Launch.Spacecraft.Id,out var launch,out var state)||
            !ReferenceEquals(launch,authority.Launch)||state!=p.ExpectedState||
            p.Consumer!=(p.Count==0?authority.Launch.Consumer:p.History[p.Count-1].NextConsumer??authority.Launch.Consumer))return AssemblyFlightStatus.StaleSource;
        return AssemblyFlightStatus.Ready;
    }
    internal AssemblyFlightStatus BeginAssemblyFlight(AssemblyLaunch launch,int historyCapacity,out AssemblyFlightAuthority? authority)
        =>BeginAssembly(launch,historyCapacity,AssemblyPhysicalConsumer.FreeFlight,out authority);
    internal AssemblyFlightStatus BeginAssemblyContact(AssemblyLaunch launch,int historyCapacity,out AssemblyFlightAuthority? authority)
        =>BeginAssembly(launch,historyCapacity,AssemblyPhysicalConsumer.SupportedContact,out authority);
    private AssemblyFlightStatus BeginAssembly(AssemblyLaunch launch,int historyCapacity,AssemblyPhysicalConsumer consumer,out AssemblyFlightAuthority? authority)
    {
        authority=null;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try
        {
            // This first production profile owns one bounded spacecraft episode
            // in the common state/clock. No competing physical consumer binds it.
            var view=_state.CreateView();
            if(_assemblyFlight is not null||_poweredFlight is not null||_commands is not null||view.Spacecraft.Count!=1||
                launch.Consumer!=consumer||historyCapacity<1||historyCapacity>(consumer==AssemblyPhysicalConsumer.FreeFlight?128:1200)||
                (consumer==AssemblyPhysicalConsumer.SupportedContact&&_clock.Rate!=SimulationRate.One)||
                _clock.CurrentTime!=launch.Initial.Epoch||_clock.PendingSimulationDebt.Ticks!=0||
                _clock.IsPaused||_clock.Timeline.PendingCount!=0||_clock.Timeline.Revision.Value!=0||
                !view.Spacecraft.TryGetAssembly(launch.Spacecraft.Id,out var registered,out var state)||
                !ReferenceEquals(registered,launch)||state!=launch.Initial)return AssemblyFlightStatus.InvalidInput;
            authority=new(launch);
            _assemblyFlight=new(authority,historyCapacity,view.Revision){ExpectedState=state,ExpectedRevision=view.Revision,
                ExpectedTimeline=_clock.Timeline.Revision,ExpectedClock=CaptureContinuationClock()};
            if(consumer==AssemblyPhysicalConsumer.SupportedContact)
            {
                try
                {
                    if(LocalContactConfiguration.TryCreateAssembly(launch,out var config)!=LocalContactStatus.Success)
                        throw new InvalidDataException("Assembly contact configuration refused.");
                    var source=LocalContactSource.CaptureAssembly(this,authority,config!);
                    if(LocalContactWorld.TryCreate(this,source,config!,out var world,out var receipt)!=LocalContactStatus.Success)
                        throw new InvalidDataException("Assembly contact reduction refused.");
                    _assemblyFlight.ContactWorld=world;_assemblyFlight.ContactConfiguration=config;_assemblyFlight.ContactReceipt=receipt;
                }
                catch
                { _assemblyFlight.ContactWorld?.Dispose();_assemblyFlight=null;authority=null;throw; }
            }
            return AssemblyFlightStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightStatus ObserveAssemblyFlight(AssemblyFlightAuthority authority,out AssemblyFlightObservation observation)
    {
        observation=default;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try
        {
            var p=_assemblyFlight;
            if(p is null||!ReferenceEquals(p.Authority,authority))return AssemblyFlightStatus.InvalidAuthority;
            var view=_state.CreateView();
            if(!view.Spacecraft.TryGetAssembly(authority.Launch.Spacecraft.Id,out _,out var state))return AssemblyFlightStatus.StaleSource;
            observation=new(state,view.Revision,_clock.Timeline.Revision,CaptureContinuationClock(),p.CanonicalHostSequence,p.Count,p.Invalidated,p.Consumer);
            return p.Invalidated?AssemblyFlightStatus.Invalidated:AssemblyFlightStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal bool TryGetAssemblyHistory(AssemblyFlightAuthority authority,int index,out AssemblyFlightRecord record)
    {
        record=default;_clock.PublicationPhase.VerifyRead();var p=_assemblyFlight;
        if(p is null||!ReferenceEquals(p.Authority,authority)||(uint)index>=(uint)p.Count)return false;
        record=p.History[index];return true;
    }
    internal AssemblyFlightResult AdmitAssemblyHostTime(AssemblyFlightAuthority authority,long sequence,SimulationDuration elapsed,bool failAcknowledgementForTest=false)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try
        {
            var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return new(status);
            var p=_assemblyFlight!;
            if(p.Consumer==AssemblyPhysicalConsumer.SupportedContact&&p.ContactWorld is {} contact&&contact.CheckAssemblyReady(this)!=LocalContactStatus.Success)return new(AssemblyFlightStatus.Invalidated);
            if(p.ExpectedState.Frontier==authority.Launch.Plan.Length)return new(AssemblyFlightStatus.Completed);
            if(p.Active)return new(AssemblyFlightStatus.OutstandingProposal);
            if(p.CanonicalHostSequence==long.MaxValue||sequence!=p.CanonicalHostSequence+1)return new(AssemblyFlightStatus.InvalidSequence);
            if(elapsed.Ticks<0)return new(AssemblyFlightStatus.InvalidInput);
            if(elapsed.Ticks==0)return new(AssemblyFlightStatus.NoWork);
            if(p.CreditCount==p.Credits.Length)return new(AssemblyFlightStatus.HistoryCapacity);
            var credit=_clock.PrepareHostAdvance(elapsed);
            if(credit.Reason!=Clock.SimulationHostAdvanceStopReason.Accepted||(Int128)p.ExpectedClock.Time.Ticks+credit.DebtAfter.Ticks>long.MaxValue)
                return new(AssemblyFlightStatus.Overflow);
            // Both canonical accounting fields commit before any failure point.
            _clock.InstallHostAdvance(credit);
            p.Credits[p.CreditCount]=new(elapsed.Ticks,p.ExpectedState.Frontier);p.CreditCount++;
            p.ExpectedClock=CaptureContinuationClock();
            if(failAcknowledgementForTest){p.Invalidated=true;p.ContactWorld?.InvalidateAssembly();return new(AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated);}
            if(p.Consumer==AssemblyPhysicalConsumer.SupportedContact)p.ContactWorld?.AcknowledgeAssemblyCredit(p.ExpectedClock);
            return new(AssemblyFlightStatus.AcceptedCredit);
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightStatus PrepareAssemblyFlight(AssemblyFlightAuthority authority,out AssemblyFlightProposal proposal,bool refuseForTest=false)
    {
        proposal=default;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try{return !OwnsAssemblyConsumer(authority,AssemblyPhysicalConsumer.FreeFlight)?AssemblyFlightStatus.InvalidAuthority:PrepareAssemblyInOwnedPhase(authority,out proposal,refuseForTest);}
        finally{_clock.PublicationPhase.Exit();}
    }
    private AssemblyFlightStatus PrepareAssemblyInOwnedPhase(AssemblyFlightAuthority authority,out AssemblyFlightProposal proposal,bool refuseForTest)
    {
        proposal=default;var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return status;
        var p=_assemblyFlight!;
        if(p.Active)return AssemblyFlightStatus.OutstandingProposal;
        if(p.ExpectedState.Frontier==authority.Launch.Plan.Length)return AssemblyFlightStatus.Completed;
        if(p.Count>=p.History.Length)return AssemblyFlightStatus.HistoryCapacity;
        if(p.Generation==long.MaxValue||p.ExpectedRevision.Value==ulong.MaxValue||p.ExpectedState.ActuatorRevision==ulong.MaxValue||p.ExpectedState.ResourceRevision==ulong.MaxValue)
            return AssemblyFlightStatus.Overflow;
        var ticks=authority.Launch.Plan[p.ExpectedState.Frontier].Request.Ticks;
        if(p.ExpectedClock.Debt.Ticks<ticks)return AssemblyFlightStatus.AwaitingDebt;
        var target=new SimulationInstant(p.ExpectedState.Epoch.Ticks+ticks);
        if(_clock.Timeline.TryPeekPending(out var pending)&&pending.Header.Time<=target)return AssemblyFlightStatus.PendingEvent;
        if(refuseForTest)return AssemblyFlightStatus.PreparationRefused;
        AssemblyFlightRecord record;
        if(p.Consumer==AssemblyPhysicalConsumer.SupportedContact&&p.ContactWorld is {} world)
        {
            var s=p.ExpectedState;var command=authority.Launch.Plan[s.Frontier];
            var successorMass=s.Mass;var stores=s.Stores;var wrench=default(AssemblyWrench);
            var consumption=default(AssemblyConsumption);
            if(authority.Launch.PoweredSupport)
            {
                consumption=AssemblyResources.CalculateContact(authority.Launch.Design,s.Stores,command.ExtentRate,ticks);
                // The stock 20-second main-only profile has ample feed. Do not silently
                // generalize this admission into an unqualified contact-exhaustion path.
                if(consumption.Classification!=Spacecraft.Resources.PropellantClassification.FullPowered||!command.Request.MainOn||command.Jets!=0||
                    s.Gimbal.ActualY!=0||s.Gimbal.ActualZ!=0)return AssemblyFlightStatus.PreparationRefused;
                stores=consumption.After;successorMass=AssemblyLaunch.ObserveMass(authority.Launch.Design,stores);
                wrench=AssemblyActuation.Main(authority.Launch.Design,0,0);
                if(world.PrepareAssemblyPower(this,s.Mass,wrench)!=LocalContactStatus.Success)return AssemblyFlightStatus.PreparationRefused;
            }
            if(authority.Launch.Site is {} && world.PrepareAssemblySite(this,s.Mass,s.Epoch)!=LocalContactStatus.Success)return AssemblyFlightStatus.PreparationRefused;
            try
            {
            var step=world.Step(this,p.ContactConfiguration!,p.ContactReceipt,target,out var receipt);
            if(step!=LocalContactStatus.Success){p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.Invalidated;}
            p.ContactReceipt=receipt;
            if(world.Read(this,p.ContactConfiguration!,receipt,out var endpoint)!=LocalContactStatus.Success)
            {p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.Invalidated;}
            var m=endpoint.Motion;
            var motion=AssemblyContactProfile.ToOrigin(m.PositionRoot,m.VelocityRoot,m.BodyToRoot,m.AngularVelocityBody,s.Mass.Com);
            var displacement=p.ContactConfiguration!.OriginVelocityRoot*((target.Ticks-authority.Launch.Initial.Epoch.Ticks)/1e6);
            if(authority.Launch.Departure is null&&(endpoint.ContactPoints==0||endpoint.ConstraintCount==0||!authority.Launch.ContactProfile!.AdmitsSupportedEndpoint(motion,displacement,p.ContactConfiguration.ContactTolerance,successorMass.Com)))
            {p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.OutsideContactDomain;}
            if(authority.Launch.PoweredSupport&&world.RefreshAssemblyMass(this,s.Mass,successorMass)!=LocalContactStatus.Success)
            {p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.Invalidated;}
            var successor=s with {Epoch=target,Motion=motion,Stores=stores,Mass=successorMass,AppliedCommand=command.Request,
                Actual=new(authority.Launch.PoweredSupport,0,authority.Launch.PoweredSupport?AssemblyFeedState.Available:AssemblyFeedState.NoDemand),
                ResourceRevision=s.ResourceRevision+(consumption.ConsumedTotal.IsZero?0UL:1UL),Frontier=s.Frontier+1,ActuatorRevision=s.ActuatorRevision+1};
            if(authority.Launch.Departure is {} departure&&
                (successor.Frontier>=authority.Launch.Plan.Length||!departure.Clears(authority.Launch,successor,
                    authority.Launch.Plan[successor.Frontier].Request.Ticks,p.ContactConfiguration.ContactTolerance,true,out _)))
            {p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.OutsideContactDomain;}
            record=new(s.Frontier,successor.AppliedCommand,s.Gimbal,wrench,consumption.Powered,
                authority.Launch.PoweredSupport?consumption.Classification:Spacecraft.Resources.PropellantClassification.NoDemand,
                successor,p.ExpectedRevision,new(p.ExpectedRevision.Value+1),p.ExpectedTimeline,authority.Launch.ContactProfile!.Digest,
                authority.Launch.Departure is null?null:AssemblyPhysicalConsumer.FreeFlight);
            }
            catch(Exception){p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.Invalidated;}
        }
        else if(authority.Launch.Departure is {} departure)
        {
            if(!departure.Clears(authority.Launch,p.ExpectedState,ticks,p.ContactConfiguration!.ContactTolerance,false,out _))
                return AssemblyFlightStatus.ClearanceExpired;
            record=AssemblyFlightPreparation.Evaluate(authority.Launch,p.ExpectedState,p.ExpectedRevision,p.ExpectedTimeline,AssemblyDepartureProfile.Gravity)
                with {ContactProfile=authority.Launch.ContactProfile!.Digest,NextConsumer=AssemblyPhysicalConsumer.FreeFlight};
            if(!departure.EndpointClear(authority.Launch,record.Successor,p.ContactConfiguration.ContactTolerance))
                return AssemblyFlightStatus.ClearanceExpired;
        }
        else record=AssemblyFlightPreparation.Evaluate(authority.Launch,p.ExpectedState,p.ExpectedRevision,p.ExpectedTimeline,authority.Launch.GravityRoot);
        p.Prepared=record;p.Generation++;p.Active=true;proposal=new(p.Generation,p.Seal);return AssemblyFlightStatus.Prepared;
    }
    internal AssemblyFlightStatus AbortAssemblyFlight(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal)
        =>AbortAssembly(authority,proposal,AssemblyPhysicalConsumer.FreeFlight);
    internal AssemblyFlightStatus AbortAssemblyContact(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal)
        =>AbortAssembly(authority,proposal,AssemblyPhysicalConsumer.SupportedContact);
    private AssemblyFlightStatus AbortAssembly(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal,AssemblyPhysicalConsumer consumer)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try
        {
            var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return status;var p=_assemblyFlight!;
            if(p.Consumer!=consumer)return AssemblyFlightStatus.InvalidAuthority;
            if(!p.Active||!proposal.IssuedBy(p.Seal)||proposal.Generation!=p.Generation)return AssemblyFlightStatus.InvalidProposal;
            p.Active=false;p.Prepared=default;
            if(p.Consumer==AssemblyPhysicalConsumer.SupportedContact&&p.ContactWorld is {} world){p.Invalidated=true;world.InvalidateAssembly();return AssemblyFlightStatus.Invalidated;}
            return AssemblyFlightStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightResult PublishAssemblyFlight(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal,bool failAcknowledgementForTest=false)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try{return !OwnsAssemblyConsumer(authority,AssemblyPhysicalConsumer.FreeFlight)?new(AssemblyFlightStatus.InvalidAuthority):PublishAssemblyInOwnedPhase(authority,proposal,failAcknowledgementForTest);}
        finally{_clock.PublicationPhase.Exit();}
    }
    private AssemblyFlightResult PublishAssemblyInOwnedPhase(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal,bool failAcknowledgementForTest)
    {
        var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return new(status);var p=_assemblyFlight!;
        if(!p.Active||!proposal.IssuedBy(p.Seal)||proposal.Generation!=p.Generation)return new(AssemblyFlightStatus.InvalidProposal);
        var record=p.Prepared;
        if(p.Count>=p.History.Length)return new(AssemblyFlightStatus.HistoryCapacity);
        if(!_state.TryPrepareAssemblySlot(authority.Launch,p.ExpectedState,out var slot))return new(AssemblyFlightStatus.StaleSource);
        if(_clock.Timeline.TryPeekPending(out var pending)&&pending.Header.Time<=record.Successor.Epoch)return new(AssemblyFlightStatus.PendingEvent);
        var debt=new SimulationDuration(p.ExpectedClock.Debt.Ticks-record.Command.Ticks);
        if(debt.Ticks<0)return new(AssemblyFlightStatus.AwaitingDebt);
        var contact=p.Consumer==AssemblyPhysicalConsumer.SupportedContact?p.ContactWorld:null;
        if(contact is not null&&contact.CheckAssemblyPublication(this,p.ContactReceipt,record.Successor.Mass)!=LocalContactStatus.Success)
            return new(AssemblyFlightStatus.InvalidProposal);
        // Common production owner phase: only prevalidated fixed writes remain.
        // Expire the lease before installing resource/motion/actuator/frontier,
        // global revision, clock and history as one unobservable write sequence.
        p.Active=false;p.Prepared=default;
        _state.InstallAssembly(slot,record.Successor,record.StateRevision);
        _clock.InstallCertifiedContinuation(record.Successor.Epoch,debt);
        p.History[p.Count]=record;p.Count++;
        p.ExpectedState=record.Successor;p.ExpectedRevision=record.StateRevision;p.ExpectedClock=CaptureContinuationClock();
        p.Consumer=record.NextConsumer??authority.Launch.Consumer;
        if(failAcknowledgementForTest){p.Invalidated=true;p.ContactWorld?.InvalidateAssembly();return new(AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,1);}
        contact?.AcknowledgeAssembly(record.StateRevision,p.ExpectedClock);
        // Retain native storage/body identity but revoke all old motion/receipt permission.
        if(contact is not null&&p.Consumer==AssemblyPhysicalConsumer.FreeFlight)contact.InvalidateAssembly();
        return new(AssemblyFlightStatus.Published,1);
    }
    internal AssemblyFlightResult ServiceAssemblyFlightDebt(AssemblyFlightAuthority authority)
        =>ServiceAssemblyDebt(authority,AssemblyPhysicalConsumer.FreeFlight);
    internal AssemblyFlightResult ServiceAssemblyContactDebt(AssemblyFlightAuthority authority)
        =>ServiceAssemblyDebt(authority,AssemblyPhysicalConsumer.SupportedContact);
    internal AssemblyFlightResult ServiceAssemblyDepartureDebt(AssemblyFlightAuthority authority)
        =>authority?.Launch.Departure is null?new(AssemblyFlightStatus.InvalidAuthority):ServiceAssemblyDebt(authority,authority.Launch.Consumer,true);
    private AssemblyFlightResult ServiceAssemblyDebt(AssemblyFlightAuthority authority,AssemblyPhysicalConsumer consumer,bool permitDeparture=false)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try
        {
            if(authority is null||(!permitDeparture&&!OwnsAssemblyConsumer(authority,consumer)))return new(AssemblyFlightStatus.InvalidAuthority);
            var count=0;
            while(true)
            {
                var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return new(status,count);var p=_assemblyFlight!;
                if(!permitDeparture&&p.Consumer!=consumer)return new(AssemblyFlightStatus.Published,count);
                if(p.ExpectedState.Frontier==authority.Launch.Plan.Length)return new(AssemblyFlightStatus.Completed,count);
                if(p.Active)return new(AssemblyFlightStatus.OutstandingProposal);
                if(p.ExpectedClock.Debt.Ticks<authority.Launch.Plan[p.ExpectedState.Frontier].Request.Ticks)return new(AssemblyFlightStatus.AwaitingDebt,count);
                if(count==4)return new(AssemblyFlightStatus.BudgetExhausted,count);
                status=PrepareAssemblyInOwnedPhase(authority,out var proposal,false);if(status!=AssemblyFlightStatus.Prepared)return new(status,count);
                var published=PublishAssemblyInOwnedPhase(authority,proposal,false);count+=published.PublishedCount;
                if(published.Status!=AssemblyFlightStatus.Published)return new(published.Status,count);
            }
        }
        finally{_clock.PublicationPhase.Exit();}
    }
}
