using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal const int AssemblyHostCreditCapacity=4096;
    private sealed class AssemblyFlightStorage(AssemblyFlightAuthority authority,int capacity,StateRevision initialRevision)
    {
        internal readonly AssemblyFlightAuthority Authority=authority;
        internal readonly object Seal=new();
        internal readonly AssemblyFlightRecord[] History=new AssemblyFlightRecord[capacity];
        internal readonly StateRevision InitialRevision=initialRevision;
        internal readonly AssemblyHostCredit[] Credits=new AssemblyHostCredit[AssemblyHostCreditCapacity];
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
        var view=_state.CreateView();
        if(view.Revision!=p.ExpectedRevision||_clock.Timeline.Revision!=p.ExpectedTimeline||CaptureContinuationClock()!=p.ExpectedClock||
            !view.Spacecraft.TryGetAssembly(authority.Launch.Spacecraft.Id,out var launch,out var state)||
            !ReferenceEquals(launch,authority.Launch)||state!=p.ExpectedState)return AssemblyFlightStatus.StaleSource;
        return AssemblyFlightStatus.Ready;
    }
    internal AssemblyFlightStatus BeginAssemblyFlight(AssemblyLaunch launch,int historyCapacity,out AssemblyFlightAuthority? authority)
    {
        authority=null;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try
        {
            // This first production profile owns one bounded spacecraft episode
            // in the common state/clock. No competing physical consumer binds it.
            var view=_state.CreateView();
            if(_assemblyFlight is not null||_poweredFlight is not null||_commands is not null||view.Spacecraft.Count!=1||
                historyCapacity is <1 or >128||_clock.CurrentTime!=launch.Initial.Epoch||_clock.PendingSimulationDebt.Ticks!=0||
                _clock.IsPaused||_clock.Timeline.PendingCount!=0||_clock.Timeline.Revision.Value!=0||
                !view.Spacecraft.TryGetAssembly(launch.Spacecraft.Id,out var registered,out var state)||
                !ReferenceEquals(registered,launch)||state!=launch.Initial)return AssemblyFlightStatus.InvalidInput;
            authority=new(launch);
            _assemblyFlight=new(authority,historyCapacity,view.Revision){ExpectedState=state,ExpectedRevision=view.Revision,
                ExpectedTimeline=_clock.Timeline.Revision,ExpectedClock=CaptureContinuationClock()};
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
            observation=new(state,view.Revision,_clock.Timeline.Revision,CaptureContinuationClock(),p.CanonicalHostSequence,p.Count,p.Invalidated);
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
            if(failAcknowledgementForTest){p.Invalidated=true;return new(AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated);}
            return new(AssemblyFlightStatus.AcceptedCredit);
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightStatus PrepareAssemblyFlight(AssemblyFlightAuthority authority,out AssemblyFlightProposal proposal,bool refuseForTest=false)
    {
        proposal=default;var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try{return PrepareAssemblyInOwnedPhase(authority,out proposal,refuseForTest);}
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
        var record=AssemblyFlightPreparation.Evaluate(authority.Launch,p.ExpectedState,p.ExpectedRevision,p.ExpectedTimeline);
        if(refuseForTest)return AssemblyFlightStatus.PreparationRefused;
        p.Prepared=record;p.Generation++;p.Active=true;proposal=new(p.Generation,p.Seal);return AssemblyFlightStatus.Prepared;
    }
    internal AssemblyFlightStatus AbortAssemblyFlight(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return entered;
        try
        {
            var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return status;var p=_assemblyFlight!;
            if(!p.Active||!proposal.IssuedBy(p.Seal)||proposal.Generation!=p.Generation)return AssemblyFlightStatus.InvalidProposal;
            p.Active=false;p.Prepared=default;return AssemblyFlightStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal AssemblyFlightResult PublishAssemblyFlight(AssemblyFlightAuthority authority,AssemblyFlightProposal proposal,bool failAcknowledgementForTest=false)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try{return PublishAssemblyInOwnedPhase(authority,proposal,failAcknowledgementForTest);}
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
        // Common production owner phase: only prevalidated fixed writes remain.
        // Expire the lease before installing resource/motion/actuator/frontier,
        // global revision, clock and history as one unobservable write sequence.
        p.Active=false;p.Prepared=default;
        _state.InstallAssembly(slot,record.Successor,record.StateRevision);
        _clock.InstallCertifiedContinuation(record.Successor.Epoch,debt);
        p.History[p.Count]=record;p.Count++;
        p.ExpectedState=record.Successor;p.ExpectedRevision=record.StateRevision;p.ExpectedClock=CaptureContinuationClock();
        if(failAcknowledgementForTest){p.Invalidated=true;return new(AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,1);}
        return new(AssemblyFlightStatus.Published,1);
    }
    internal AssemblyFlightResult ServiceAssemblyFlightDebt(AssemblyFlightAuthority authority)
    {
        var entered=EnterAssemblyPhase();if(entered!=AssemblyFlightStatus.Ready)return new(entered);
        try
        {
            var count=0;
            while(true)
            {
                var status=CheckAssemblySource(authority);if(status!=AssemblyFlightStatus.Ready)return new(status,count);var p=_assemblyFlight!;
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
