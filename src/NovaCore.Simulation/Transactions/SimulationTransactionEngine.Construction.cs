using System.Collections.Immutable;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal const int ConstructionHistoryMaximum=4096,ConstructionSaveMaximumBytes=268_435_456;
    internal const string ConstructionSaveSchema="novacore.construction-runtime/1";
    private sealed class ConstructionStorage(ConstructionServiceAuthority authority,int capacity,StateRevision revision,TimelineRevision timeline,ContinuationClockState clock)
    {
        internal readonly ConstructionServiceAuthority Authority=authority;
        internal readonly ConstructionServiceCommand[] History=new ConstructionServiceCommand[capacity];
        internal readonly StateRevision InitialRevision=revision;
        internal ConstructionRuntimeState Expected=authority.Binding.Initial;
        internal StateRevision Revision=revision;
        internal readonly TimelineRevision Timeline=timeline;
        internal ContinuationClockState Clock=clock;
        internal int Count;
        internal bool Retired;
        internal AssemblyControlStorage? Control;
        internal LocalContactWorld? ContactWorld;
        internal LocalContactConfiguration? ContactConfiguration;
        internal LocalContactWorld.Receipt ContactReceipt;
        internal bool Invalidated=false;
        internal long HostSequence;
        internal long LastHostTicks;
        internal ConstructionControlCheckpoint? ControlCheckpoint;
        internal ConstructionPhysicalRecord?[]? PhysicalHistory;
        internal ConstructionControlCheckpoint? PhysicalCheckpoint;
        internal int PhysicalCount;
        internal bool FlightFrontierRestored;
    }
    private ConstructionStorage? _construction;
    private ConstructionServiceStatus EnterConstruction()
    {
        if(!_clock.PublicationPhase.IsOwnerThread)return ConstructionServiceStatus.WrongOwnerThread;
        return _isExecutingGroup||!_clock.PublicationPhase.TryEnter(this)?ConstructionServiceStatus.Reentrant:ConstructionServiceStatus.Ready;
    }
    private ConstructionServiceStatus CheckConstruction(ConstructionServiceAuthority? authority)
    {
        var p=_construction;
        if(authority is null||p is null||!ReferenceEquals(authority,p.Authority))return ConstructionServiceStatus.InvalidAuthority;
        if(p.Retired)return ConstructionServiceStatus.Retired;
        if(p.Invalidated)return ConstructionServiceStatus.Invalidated;
        if(authority.Binding.Physical is {} physical&&!physical.Site.Applicable)return ConstructionServiceStatus.StaleSource;
        var view=_state.CreateView();
        if(view.Revision!=p.Revision||_clock.Timeline.Revision!=p.Timeline||CaptureContinuationClock()!=p.Clock||
            !view.Spacecraft.TryGetConstruction(authority.Binding.Spacecraft.Id,out var binding,out var current)||
            !ReferenceEquals(binding,authority.Binding)||!ReferenceEquals(current,p.Expected))return ConstructionServiceStatus.StaleSource;
        return ConstructionServiceStatus.Ready;
    }
    internal ConstructionServiceStatus BeginConstructionServices(ConstructionRuntimeBinding binding,int capacity,out ConstructionServiceAuthority? authority)
        =>BeginConstruction(binding,capacity,false,out authority);
    private ConstructionServiceStatus BeginConstruction(ConstructionRuntimeBinding binding,int capacity,bool physical,out ConstructionServiceAuthority? authority)
    {
        authority=null;var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            var view=_state.CreateView();
            if((binding.Physical is not null)!=physical||_construction is not null||_assemblyFlight is not null||_poweredFlight is not null||_commands is not null||view.Spacecraft.Count!=1||
                capacity is <1 or >ConstructionHistoryMaximum||_clock.CurrentTime!=binding.Initial.Epoch||_clock.PendingSimulationDebt.Ticks!=0||
                _clock.Rate!=SimulationRate.One||_clock.IsPaused||_clock.Timeline.PendingCount!=0||_clock.Timeline.Revision.Value!=0||
                !view.Spacecraft.TryGetConstruction(binding.Spacecraft.Id,out var registered,out var state)||!ReferenceEquals(binding,registered)||!ReferenceEquals(state,binding.Initial))return ConstructionServiceStatus.InvalidInput;
            authority=new(binding);_construction=new(authority,capacity,view.Revision,_clock.Timeline.Revision,CaptureContinuationClock());
            if(physical)
            {
                try
                {
                    if(state.Physical!.Consumer!=AssemblyPhysicalConsumer.FreeFlight||!binding.Physical!.Clearance.Clears(
                        state.Physical.Motion,state.ReferenceMass!.Value,state.Epoch,15625,out _))
                    {
                        var config=LocalContactConfiguration.CreateCraft(binding,state.Physical.Consumer!=AssemblyPhysicalConsumer.SupportedContact);
                        var source=LocalContactSource.CaptureCraft(this,authority,config);
                        if(LocalContactWorld.TryCreate(this,source,config,out var world,out var receipt)!=LocalContactStatus.Success)
                            throw new InvalidDataException("Craft contact reduction refused.");
                        // A near-surface free-flight checkpoint must pass native
                        // geometry admission before presentation. Keep that same
                        // private preparation until the first owned step; its
                        // existence does not change the canonical consumer.
                        _construction.ContactWorld=world;_construction.ContactConfiguration=config;_construction.ContactReceipt=receipt;
                    }
                    _construction.Control=new(new(new(binding.Spacecraft.Id,binding.Identity.Generation)),AssemblyControlCapacity,null);
                    _construction.PhysicalHistory=new ConstructionPhysicalRecord[256];
                    _construction.PhysicalCheckpoint=new(state,view.Revision,_construction.Clock,0,default,-1,false);
                }
                catch
                {
                    _construction.ContactWorld?.Dispose();_construction=null;authority=null;throw;
                }
            }
            return ConstructionServiceStatus.Ready;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus ObserveConstructionServices(ConstructionServiceAuthority authority,out ConstructionRuntimeState? snapshot)
    {
        snapshot=null;var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try{var status=CheckConstruction(authority);if(status==ConstructionServiceStatus.Ready)snapshot=_construction!.Expected;return status;}
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus AdvanceConstructionServices(ConstructionServiceAuthority authority,int sequence,ConstructionServiceCommand command,bool refusePowerForTest=false)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            var status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
            var p=_construction!;var binding=authority.Binding;
            if(binding.Physical is not null)return ConstructionServiceStatus.InvalidInput;
            if(command is null||command.HostTicks<=0||sequence!=p.Count||command.Demand.IsDefault||command.Demand.Length!=binding.Fuel.Consumers.Length||
                command.Loads.IsDefault||command.Loads.Length>binding.Power.Modules.Length)return ConstructionServiceStatus.InvalidInput;
            if(p.Count==p.History.Length)return ConstructionServiceStatus.HistoryCapacity;
            if(p.Revision.Value==ulong.MaxValue)return ConstructionServiceStatus.Overflow;
            for(var i=0;i<command.Demand.Length;i++)if(command.Demand[i])
            {
                if(!binding.ServiceConsumptionQualified)return ConstructionServiceStatus.UnqualifiedHardware;
                if(!binding.Power.CanCommand(binding.Design.Index(binding.Fuel.Consumers[i].Key.Part)))return ConstructionServiceStatus.NoDataPath;
            }
            var power=p.Expected.Power;
            for(var i=0;i<command.Loads.Length;i++)
            {
                var setting=command.Loads[i];
                if((uint)setting.Module>=(uint)binding.Power.Modules.Length||binding.Power.Modules[setting.Module].Role!=ElectricalRole.Load)return ConstructionServiceStatus.InvalidInput;
                for(var j=0;j<i;j++)if(command.Loads[j].Module==setting.Module)return ConstructionServiceStatus.InvalidInput;
                if(!binding.Power.CanCommand(binding.Power.Modules[setting.Module].Part))return ConstructionServiceStatus.NoDataPath;
                power=power.SetLoadActive(setting.Module,setting.Enabled);
            }
            var credit=_clock.PrepareHostAdvance(new(command.HostTicks));
            if(credit.Reason!=SimulationHostAdvanceStopReason.Accepted||credit.DebtAfter.Ticks!=command.HostTicks||
                (Int128)p.Expected.Epoch.Ticks+command.HostTicks>long.MaxValue)return ConstructionServiceStatus.Overflow;
            var target=new SimulationInstant(p.Expected.Epoch.Ticks+command.HostTicks);
            if(_clock.Timeline.TryPeekPending(out var pending)&&pending.Header.Time<=target)return ConstructionServiceStatus.PendingEvent;
            ConstructionRuntimeState successor;
            try
            {
                var fuel=ConstructionFuelSolver.Advance(p.Expected.Fuel,command.HostTicks,command.Demand.AsSpan());
                // Permanent atomic-refusal seam: exact fuel preparation has completed, nothing is installed.
                if(refusePowerForTest)return ConstructionServiceStatus.PreparationRefused;
                var energy=ConstructionPowerSolver.Advance(power,command.HostTicks,fuel);
                var mass=ReferenceEquals(fuel.State,p.Expected.Fuel)?p.Expected.ReferenceMass:binding.ReferenceMass(fuel.State);
                successor=new(fuel.State,energy.State,target,p.Count+1,mass);
            }
            catch(Exception e) when(e is InvalidDataException or OverflowException){return ConstructionServiceStatus.PreparationRefused;}
            status=CheckConstruction(authority);if(status!=ConstructionServiceStatus.Ready)return status;
            if(!_clock.PublicationPhase.IsOwnedBy(this)||!_state.TryPrepareConstructionSlot(binding,p.Expected,out var slot))return ConstructionServiceStatus.StaleSource;
            var revision=new StateRevision(p.Revision.Value+1);
            // One existing-owner phase; only fixed validated writes remain. No external callbacks.
            _state.InstallConstruction(slot,successor,revision);
            _clock.InstallHostAdvance(credit);_clock.InstallCertifiedContinuation(target,SimulationDuration.Zero);
            p.History[p.Count]=command;p.Count++;p.Expected=successor;p.Revision=revision;p.Clock=CaptureContinuationClock();
            return ConstructionServiceStatus.Advanced;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal byte[] SaveConstructionServices(ConstructionServiceAuthority authority)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Construction save outside owner phase.");
        try
        {
            if(CheckConstruction(authority)!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Retired or stale construction save.");
            var p=_construction!;var state=p.Expected;
            if(authority.Binding.Physical is not null)throw new InvalidDataException("Static service save cannot represent a physical craft.");
            var bytes=AssemblyJson.Write(new ConstructionRuntimeSave(ConstructionSaveSchema,authority.Binding.Design.Data,authority.Binding.Initial.Epoch.Ticks,
                p.InitialRevision.Value,p.History.Length,p.History.Take(p.Count).ToImmutableArray(),state.Epoch.Ticks,p.Revision.Value,state.Fuel.Save(),state.Power.Save()));
            if(bytes.Length>ConstructionSaveMaximumBytes)throw new InvalidDataException("Construction save capacity exceeded.");return bytes;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
    internal ConstructionServiceStatus RetireConstructionServices(ConstructionServiceAuthority authority)
    {
        var entered=EnterConstruction();if(entered!=ConstructionServiceStatus.Ready)return entered;
        try
        {
            if(_construction is not {} p||!ReferenceEquals(p.Authority,authority))return ConstructionServiceStatus.InvalidAuthority;
            p.Retired=true;if(p.Control is {} c)c.Retired=true;p.ContactWorld?.Dispose();return ConstructionServiceStatus.Retired;
        }
        finally{_clock.PublicationPhase.Exit();}
    }
}
