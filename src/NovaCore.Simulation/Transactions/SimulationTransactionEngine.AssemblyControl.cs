using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal const int AssemblyControlCapacity = 256;
    private sealed class AssemblyControlStorage(AssemblyControlAuthority authority, int capacity,AssemblyPilotAllocation? allocation)
    {
        internal readonly AssemblyControlAuthority Authority = authority;
        internal readonly AssemblyControlAdmission[] Journal = new AssemblyControlAdmission[capacity];
        internal readonly AssemblyPilotAllocation? Allocation=allocation;
        internal int Count;
        internal AssemblyControlRequest Requested;
        internal int ArbitrationFrontier = -1;
        internal bool OffAtFrontier;
        internal bool Retired;
    }

    private static AssemblyControlStatus ControlStatus(AssemblyFlightStatus status) => status switch
    {
        AssemblyFlightStatus.Ready => AssemblyControlStatus.Ready,
        AssemblyFlightStatus.WrongOwnerThread => AssemblyControlStatus.WrongOwnerThread,
        AssemblyFlightStatus.Reentrant => AssemblyControlStatus.Reentrant,
        AssemblyFlightStatus.InvalidAuthority => AssemblyControlStatus.InvalidAuthority,
        _ => AssemblyControlStatus.StaleSource
    };

    internal AssemblyControlStatus BeginAssemblyControl(AssemblyFlightAuthority authority,
        int capacity, out AssemblyControlAuthority? control,AssemblyControlExecution execution=AssemblyControlExecution.DemandOnly)
    {
        control = null;
        var entered = EnterAssemblyPhase();
        if (entered != AssemblyFlightStatus.Ready) return ControlStatus(entered);
        try
        {
            var status = CheckAssemblySource(authority);
            if (status != AssemblyFlightStatus.Ready) return ControlStatus(status);
            var p = _assemblyFlight!;
            var launch = authority.Launch;
            if (p.Control is not null || p.Active || p.Count != 0 || p.CreditCount != 0 ||
                p.Consumer != AssemblyPhysicalConsumer.FreeFlight || launch.Departure is not null ||
                launch.Design.Development is not null || launch.GravityRoot != NovaCore.Core.Double3.Zero ||
                launch.Initial.Gimbal != default || _clock.Rate != SimulationRate.One || capacity is < 1 or > AssemblyControlCapacity||!Enum.IsDefined(execution))
                return AssemblyControlStatus.InvalidInput;
            foreach (var command in launch.Plan)
                if (command.Request.MainOn || command.Jets != 0 || command.Request.Pair is not null ||
                    command.Request.GimbalTargetY != 0 || command.Request.GimbalTargetZ != 0)
                    return AssemblyControlStatus.InvalidInput;
            AssemblyPilotAllocation? allocation=null;
            if(execution==AssemblyControlExecution.PhysicalActuators)
            {try{allocation=new(launch.Design);}catch(InvalidDataException){return AssemblyControlStatus.InvalidInput;}}
            control = new(new(launch.Spacecraft.Id, 1));
            p.Control = new(control, capacity,allocation);
            return AssemblyControlStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal AssemblyControlResult AdmitAssemblyControl(AssemblyControlAuthority control,
        AssemblyControlIdentity identity, long sequence, AssemblyControlRequest request)
    {
        var entered = EnterAssemblyPhase();
        if (entered != AssemblyFlightStatus.Ready) return new(ControlStatus(entered));
        try
        {
            var p = _assemblyFlight;
            if (p?.Control is not { } c || !ReferenceEquals(c.Authority, control))
                return new(AssemblyControlStatus.InvalidAuthority);
            if (c.Retired) return new(AssemblyControlStatus.Retired);
            if (identity != c.Authority.Identity) return new(AssemblyControlStatus.InvalidIdentity);
            var source = CheckAssemblySource(p.Authority);
            if (source != AssemblyFlightStatus.Ready) return new(ControlStatus(source));
            if (!request.Pilot.IsValid || request.PilotOnly && request.MainOn)
                return new(AssemblyControlStatus.InvalidInput);
            // Retrying a committed admission returns that historical receipt.
            // It never restores its old latch or creates another physical step.
            if (sequence > 0 && sequence <= c.Count)
            {
                var prior = c.Journal[(int)sequence - 1];
                return prior.Requested == request ? new(AssemblyControlStatus.Duplicate, prior)
                    : new(AssemblyControlStatus.InvalidSequence);
            }
            if (sequence != c.Count + 1L) return new(AssemblyControlStatus.InvalidSequence);
            if (p.ExpectedState.Frontier == p.Authority.Launch.Plan.Length || p.Count == p.History.Length)
                return new(AssemblyControlStatus.Terminal);
            if (p.Active) return new(AssemblyControlStatus.OutstandingProposal);
            if (c.Count == c.Journal.Length) return new(AssemblyControlStatus.Capacity);
            var frontier = p.ExpectedState.Frontier;
            var off = (c.ArbitrationFrontier == frontier && c.OffAtFrontier) || (!request.PilotOnly && !request.MainOn);
            var effective = new AssemblyControlRequest(request.PilotOnly?c.Requested.MainOn:request.MainOn && !off, request.Pilot);
            var record = new AssemblyControlAdmission(identity, sequence, frontier,
                p.CanonicalHostSequence, request, effective);
            c.Journal[c.Count++] = record;
            c.ArbitrationFrontier = frontier;
            c.OffAtFrontier = off;
            c.Requested = effective;
            return new(AssemblyControlStatus.Admitted, record);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal AssemblyControlStatus ObserveAssemblyControl(AssemblyControlAuthority control,
        out AssemblyControlObservation observation)
    {
        observation = default;
        var entered = EnterAssemblyPhase();
        if (entered != AssemblyFlightStatus.Ready) return ControlStatus(entered);
        try
        {
            var p = _assemblyFlight;
            if (p?.Control is not { } c || !ReferenceEquals(c.Authority, control))
                return AssemblyControlStatus.InvalidAuthority;
            observation = new(c.Authority.Identity, c.Requested, c.Count, c.Journal.Length,
                p.ExpectedState.Frontier, c.Retired);
            if (c.Retired) return AssemblyControlStatus.Retired;
            return ControlStatus(CheckAssemblySource(p.Authority));
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal bool TryGetAssemblyControlAdmission(AssemblyControlAuthority control, int index,
        out AssemblyControlAdmission admission)
    {
        admission = default;
        _clock.PublicationPhase.VerifyRead();
        if (_assemblyFlight?.Control is not { } c || !ReferenceEquals(c.Authority, control) ||
            (uint)index >= (uint)c.Count) return false;
        admission = c.Journal[index];
        return true;
    }

    internal AssemblyControlStatus RetireAssemblyControl(AssemblyControlAuthority control)
    {
        var entered = EnterAssemblyPhase();
        if (entered != AssemblyFlightStatus.Ready) return ControlStatus(entered);
        try
        {
            var p = _assemblyFlight;
            if (p?.Control is not { } c || !ReferenceEquals(c.Authority, control))
                return AssemblyControlStatus.InvalidAuthority;
            RetireAssemblyInOwnedPhase(p);
            return AssemblyControlStatus.Retired;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal AssemblyFlightStatus RetireAssemblyFlight(AssemblyFlightAuthority authority)
    {
        var entered = EnterAssemblyPhase();
        if (entered != AssemblyFlightStatus.Ready) return entered;
        try
        {
            var p = _assemblyFlight;
            if (p is null || !ReferenceEquals(p.Authority, authority) ||
                authority.Launch.Consumer != AssemblyPhysicalConsumer.FreeFlight)
                return AssemblyFlightStatus.InvalidAuthority;
            RetireAssemblyInOwnedPhase(p);
            return AssemblyFlightStatus.Invalidated;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    private static void RetireAssemblyInOwnedPhase(AssemblyFlightStorage p)
    {
        if (p.Control is { } c) c.Retired = true;
        p.Invalidated = true;
        p.Active = false;
        p.Prepared = default;
    }

    private static CompiledAssemblyCommand ResolveAssemblyCommand(AssemblyFlightStorage p)
    {
        var recorded = p.Authority.Launch.Plan[p.ExpectedState.Frontier];
        if (p.Control is not { } c) return recorded;
        if(c.Allocation is {} allocation)return allocation.Resolve(c.Requested,recorded.Request.Ticks);
        return AssemblyLaunch.CompileCommand(p.Authority.Launch.Design,
            new(c.Requested.MainOn, null, 0, 0, recorded.Request.Ticks));
    }
}
