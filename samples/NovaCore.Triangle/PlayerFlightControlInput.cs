using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

/// <summary>Input adapter only. It owns no motion, stores, actuator or simulation clock.</summary>
internal sealed class PlayerFlightControlInput
{
    private readonly AssemblyApplicationSession? session;
    private readonly SimulationTransactionEngine engine;
    private readonly AssemblyControlAuthority control;
    internal bool Started { get; private set; }
    internal AssemblyControlIdentity Identity => control.Identity;
    internal AssemblyControlObservation Observation { get; private set; }
    internal AssemblyControlResult LastResult { get; private set; }

    internal PlayerFlightControlInput(AssemblyApplicationSession session)
    {
        this.session = session;
        if (session.Control is null || session.Engine.ObserveAssemblyControl(session.Control, out var observation) != AssemblyControlStatus.Ready)
            throw new InvalidDataException("Player control capability unavailable.");
        engine=session.Engine;control=session.Control;
        Observation = observation;
        for(var i=0;i<observation.AdmissionCount;i++)
        {
            if(!session.Engine.TryGetAssemblyControlAdmission(session.Control,i,out var admission))
                throw new InvalidDataException("Player admission unavailable.");
            if(!admission.Effective.MainOn)continue;
            if(admission.Frontier!=0)throw new InvalidDataException("Player episode must begin with admitted ignition.");
            Started=true;break;
        }
        if(!Started&&(observation.Frontier!=0||session.Engine.ObserveAssemblyFlight(session.Authority,out var physical)!=AssemblyFlightStatus.Ready||physical.HostSequence!=0))
            throw new InvalidDataException("READY player episode has already consumed host time.");
    }

    internal PlayerFlightControlInput(ConstructionApplicationSession session)
    {
        if(session.Binding.Physical is null||session.Control is null||session.Engine.ObserveAssemblyControl(session.Control,out var observation)!=AssemblyControlStatus.Ready)
            throw new InvalidDataException("Physical craft control capability unavailable.");
        engine=session.Engine;control=session.Control;Observation=observation;
        // A live supported craft already advances cold. No ignition gate or
        // finite prerecorded episode applies to this shared input adapter.
        Started=true;
    }

    internal AssemblyControlResult Apply(in NativeInputState input)
    {
        var status = engine.ObserveAssemblyControl(control, out var observed);
        Observation = observed;
        if (status != AssemblyControlStatus.Ready) return LastResult = new(status);
        if (session is not null&&observed.Frontier == session.Launch.Plan.Length) return LastResult = new(AssemblyControlStatus.Terminal);
        if ((input.EngineActions & ~(NativeEngineActions.On | NativeEngineActions.Off)) != 0 ||
            ((uint)input.PilotKeys & ~63u) != 0)
            return LastResult = new(AssemblyControlStatus.InvalidInput);
        var keys = input.ControlInputActive != 0 ? input.PilotKeys : NativePilotKeys.None;
        var pilot = AssemblyPilotDemand.FromOpposingRequests(
            (keys & NativePilotKeys.W) != 0, (keys & NativePilotKeys.S) != 0,
            (keys & NativePilotKeys.A) != 0, (keys & NativePilotKeys.D) != 0,
            (keys & NativePilotKeys.Q) != 0, (keys & NativePilotKeys.E) != 0);
        var actions = input.ControlInputActive != 0 ? input.EngineActions : NativeEngineActions.None;
        var on = (actions & NativeEngineActions.Off) == 0;
        // READY-only X is a no-op, not an OFF arbitration record at interval 0.
        // Repeated identical intent also cannot renew a session or grow its log.
        var engineChanged = actions != 0 && (Started || on) && observed.Requested.MainOn != on;
        if (!engineChanged && observed.Requested.Pilot == pilot)
            return LastResult = new(AssemblyControlStatus.Ready);
        var request = engineChanged ? new AssemblyControlRequest(on, pilot) : new(false, pilot, true);
        if(observed.AdmissionCount==long.MaxValue)return LastResult=new(AssemblyControlStatus.Capacity);
        var result = engine.AdmitAssemblyControl(control, Identity, observed.AdmissionCount + 1L, request);
        if (result.Status == AssemblyControlStatus.Admitted)
        {
            if (result.Admission.Effective.MainOn) Started = true;
            engine.ObserveAssemblyControl(control, out observed);
            Observation = observed;
        }
        return LastResult = result;
    }

    internal static NativeInputState CameraInput(in NativeInputState input) => input with
    { MoveLeft=0, MoveRight=0, MoveForward=0, MoveBackward=0, MoveDown=0, MoveUp=0 };
}
