using System.Reflection;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class PoweredFreeFlightTests
{
    private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static object Field(object source,string name)=>source.GetType().GetField(name,Fields)!.GetValue(source)!;
    private static void Set(object source,string name,object value)=>source.GetType().GetField(name,Fields)!.SetValue(source,value);
    private readonly record struct AuthoritySnapshot(SpacecraftPhysicalSource Physical, PropellantSourceObservation Resource,
        ActualEngineState Actual, StateRevision Revision, TimelineRevision Timeline, ContinuationClockState Clock,
        int Count, bool PhysicalActive, long Frontier, long Sequence, EnginePreparationProgress Engine, PropellantPreparationProgress Fuel);
    private static AuthoritySnapshot Snapshot(Flight f)
    {
        Check(SpacecraftPhysicalSource.TryCapture(f.Engine.State.Spacecraft,Flight.Craft,out var physical),"canonical source copy");
        var p=Field(f.Engine,"_poweredFlight");var observation=f.Engine.ObservePoweredFreeFlight(f.Power).Observation;
        f.Engine.ObserveEnginePreparation(f.Actuation,out var engine);f.Engine.ObserveFinitePropellant(f.Resource,out var source,out var fuel);
        return new(physical,source,observation.Actuator,f.Engine.State.Revision,f.Clock.Timeline.Revision,f.Engine.CaptureContinuationClock(),
            observation.HistoryCount,(bool)Field(p,"Active"),(long)Field(p,"Frontier"),(long)Field(p,"HostSequence"),engine,fuel);
    }
    private static void Unchanged(Flight f,AuthoritySnapshot before,string why)=>Check(Snapshot(f)==before,"nonmutation "+why);
    internal static void Authority()
    {
        var f=new Flight();var original=Snapshot(f);
        Check(f.Engine.AdmitPoweredHostTime(f.Power,1,new(-1)).Status==PoweredFlightStatus.InvalidInput,"negative host");Unchanged(f,original,"negative");
        Check(f.Engine.AdmitPoweredHostTime(f.Power,1,default).Status==PoweredFlightStatus.NoWork,"zero host");Unchanged(f,original,"zero");
        Check(f.Engine.ServicePoweredFlightDebt(f.Power).Status==PoweredFlightStatus.AwaitingDebt,"initial no-work");Unchanged(f,original,"no work");
        f.Credit(16665);var insufficient=Snapshot(f);
        Check(f.Engine.ServicePoweredFlightDebt(f.Power).Status==PoweredFlightStatus.AwaitingDebt,"one tick insufficient");Unchanged(f,insufficient,"insufficient");
        Check(f.Engine.AdmitPoweredHostTime(f.Power,1,new(1)).Status==PoweredFlightStatus.InvalidSequence,"duplicate credit");Unchanged(f,insufficient,"duplicate credit");
        Check(f.Engine.AdmitPoweredHostTime(f.Power,3,new(1)).Status==PoweredFlightStatus.InvalidSequence,"gap credit");Unchanged(f,insufficient,"gap credit");
        Check(f.Engine.AdmitPoweredHostTime(f.Power,2,new(long.MaxValue)).Status==PoweredFlightStatus.ArithmeticOverflow,"debt overflow");Unchanged(f,insufficient,"overflow");
        f.Credit(1);var published=f.Engine.ServicePoweredFlightDebt(f.Power);
        Check(published.PublishedCount==1&&published.Status==PoweredFlightStatus.AwaitingDebt&&published.Observation.Clock.Time.Ticks==16666&&
            published.Observation.Clock.Debt.Ticks==0&&published.Observation.StateRevision.Value==1&&published.Observation.Actuator.ActuatorRevision==1&&
            published.Observation.Resource.ResourceRevision==1,"exact funded interval and synchronized revisions");
        Check(published.Observation.TimelineRevision==original.Timeline,"publication keeps timeline");
        EndpointFence(f);

        var refused=new Flight();refused.Seal();var before=Snapshot(refused);
        Check(refused.Engine.PreparePoweredFlight(refused.Power,refused.FuelLease,out _,true)==PoweredFlightStatus.PreparationRefused,"prepared refusal");Unchanged(refused,before,"prepared refusal");
        Check(refused.Engine.PreparePoweredFlight(refused.Power,default,out _)==PoweredFlightStatus.InvalidProposal,"default resource");Unchanged(refused,before,"default resource");
        Check(refused.Engine.PreparePoweredFlight(refused.Power,refused.FuelLease,out refused.PhysicalLease)==PoweredFlightStatus.Prepared,"seal physical");
        var pending=Snapshot(refused);
        Check(refused.Engine.AdmitPoweredHostTime(refused.Power,2,new(1)).Status==PoweredFlightStatus.OutstandingProposal,"no credit with pending source");Unchanged(refused,pending,"outstanding credit");
        Check(refused.Engine.PublishPoweredFlight(refused.Power,default).Status==PoweredFlightStatus.InvalidProposal,"default physical receipt");Unchanged(refused,pending,"default physical");
        Check(refused.Engine.PublishPoweredFlight(refused.Power,new(refused.PhysicalLease.Generation,new object())).Status==PoweredFlightStatus.InvalidProposal,"fabricated physical receipt");Unchanged(refused,pending,"fabricated physical");
        var foreign=new Flight();
        Check(refused.Engine.PublishPoweredFlight(foreign.Power,refused.PhysicalLease).Status==PoweredFlightStatus.InvalidAuthority,"foreign authority");Unchanged(refused,pending,"foreign");
        Check(Task.Run(()=>refused.Engine.PublishPoweredFlight(refused.Power,refused.PhysicalLease).Status).GetAwaiter().GetResult()==PoweredFlightStatus.WrongOwnerThread,"wrong thread");Unchanged(refused,pending,"wrong thread");
        Check(refused.Clock.PublicationPhase.TryEnter(refused),"test phase");
        try
        {
            Check(refused.Engine.PublishPoweredFlight(refused.Power,refused.PhysicalLease).Status==PoweredFlightStatus.Reentrant&&
                refused.Engine.AdmitPoweredHostTime(refused.Power,2,new(1)).Status==PoweredFlightStatus.Reentrant&&
                refused.Engine.ServicePoweredFlightDebt(refused.Power).Status==PoweredFlightStatus.Reentrant,"reentrant paths");
        }
        finally{refused.Clock.PublicationPhase.Exit();}
        Unchanged(refused,pending,"reentrancy");
        var committed=refused.Engine.PublishPoweredFlight(refused.Power,refused.PhysicalLease);
        Check(committed.Status==PoweredFlightStatus.Published,"retry genuine retained proposal");var after=Snapshot(refused);
        Check(refused.Engine.PublishPoweredFlight(refused.Power,refused.PhysicalLease).Status==PoweredFlightStatus.InvalidProposal,"duplicate consumed physical");Unchanged(refused,after,"duplicate physical");
        Check(refused.Engine.PreviewSingleEngineActuation(refused.Actuation,refused.EngineLease,out _)==EnginePreparationStatus.InvalidProposal&&
            refused.Engine.PreviewFinitePropellant(refused.Resource,refused.FuelLease,out _)==PropellantPreparationStatus.InvalidProposal,"source leases consumed exactly once");
        refused.Seal();refused.Apply();Check(refused.Engine.PublishPoweredFlight(refused.Power,new(1)).Status==PoweredFlightStatus.InvalidProposal,"older receipt");

        foreach(var mutation in new[]{"debt","pause","rate","state","timeline","signed-zero","mass","validity"})
        {
            var stale=new Flight();stale.Seal();stale.Apply();
            switch(mutation)
            {
                case "debt":stale.Clock.AdvanceByHostDuration(new(1));break;
                case "pause":stale.Clock.Pause();break;
                case "rate":stale.Clock.TrySetRate(SimulationRate.Two);break;
                case "state":Set(Field(stale.Engine,"_state"),"_revision",new StateRevision(2));break;
                case "timeline":stale.Clock.Timeline.Schedule(stale.Clock.CurrentTime,new(new(1),new(50000),0,SimulationEventKind.Marker));break;
                default:
                    var endpoints=(SpacecraftAppliedEndpoint[])Field(stale.Store,"_appliedEndpoints");var ep=endpoints[0];
                    endpoints[0]=mutation=="signed-zero"?ep with{PositionRoot=ep.PositionRoot with{Z=-0d}}:
                        mutation=="mass"?ep with{Properties=new(9)}:ep with{Validity=(AppliedEndpointValidity)99};break;
            }
            var changed=Snapshot(stale);
            Check(stale.Engine.AdmitPoweredHostTime(stale.Power,2,new(1)).Status==PoweredFlightStatus.StaleSource&&
                stale.Engine.ServicePoweredFlightDebt(stale.Power).Status==PoweredFlightStatus.StaleSource,"external authority " + mutation);
            Unchanged(stale,changed,"external "+mutation);
        }
        foreach(var at in new[]{16665L,16666L})
        {
            var blocked=new Flight(eventAt:at);blocked.Credit(16666);var blockedBefore=Snapshot(blocked);
            Check(blocked.Engine.ServicePoweredFlightDebt(blocked.Power).Status==PoweredFlightStatus.PendingEvent,"event at/before target");Unchanged(blocked,blockedBefore,"event");
        }
        var overflow=new Flight(revision:ulong.MaxValue);overflow.Seal();var overflowBefore=Snapshot(overflow);
        Check(overflow.Engine.PreparePoweredFlight(overflow.Power,overflow.FuelLease,out _)==PoweredFlightStatus.RevisionOverflow,"state revision overflow");Unchanged(overflow,overflowBefore,"revision overflow");
        var capacity=new Flight(capacity:1);capacity.Seal();capacity.Apply();capacity.Credit(16667);var capacityBefore=Snapshot(capacity);
        Check(capacity.Engine.ServicePoweredFlightDebt(capacity.Power).Status==PoweredFlightStatus.HistoryCapacity,"history capacity before work");Unchanged(capacity,capacityBefore,"capacity");

        var terminal=new Flight();terminal.Seal();Check(terminal.Engine.PreparePoweredFlight(terminal.Power,terminal.FuelLease,out var token)==PoweredFlightStatus.Prepared,"terminal prepare");
        var ack=terminal.Engine.PublishPoweredFlight(terminal.Power,token,true);
        Check(ack.Status==PoweredFlightStatus.CanonicalCommittedPrivateInvalidated&&ack.CanonicalCommitted&&ack.Observation.HistoryCount==1&&
            terminal.Engine.State.Revision.Value==1&&terminal.Resource.ResourceRevision==1&&terminal.Clock.CurrentTime.Ticks==16666&&
            terminal.Clock.PendingSimulationDebt.Ticks==0&&ack.Observation.Actuator.ActuatorRevision==1,"terminal preserves entire canonical bundle");
        Check(terminal.Engine.PublishPoweredFlight(terminal.Power,token).Status==PoweredFlightStatus.Invalidated&&
            terminal.Engine.PreviewSingleEngineActuation(terminal.Actuation,terminal.EngineLease,out _)==EnginePreparationStatus.InvalidProposal&&
            terminal.Engine.PreviewFinitePropellant(terminal.Resource,terminal.FuelLease,out _)==PropellantPreparationStatus.InvalidProposal,"terminal cannot replay any lease");
        var creditTerminal=new Flight();var terminalCredit=creditTerminal.Engine.AdmitPoweredHostTime(creditTerminal.Power,1,new(16666),true);
        Check(terminalCredit.Status==PoweredFlightStatus.CanonicalCommittedPrivateInvalidated&&creditTerminal.Clock.PendingSimulationDebt.Ticks==16666&&
            creditTerminal.Engine.State.Revision.Value==0&&creditTerminal.Engine.ServicePoweredFlightDebt(creditTerminal.Power).Status==PoweredFlightStatus.Invalidated,"credit terminal retains debt without motion");
        var cancel=new Flight();cancel.Seal();Check(cancel.Engine.PreparePoweredFlight(cancel.Power,cancel.FuelLease,out var abandoned)==PoweredFlightStatus.Prepared,"cancel prepare");
        Check(cancel.Engine.RetireFinitePropellant(cancel.Resource,cancel.FuelLease)==PropellantPreparationStatus.Retired,"resource canceled explicitly");
        Check(cancel.Engine.PublishPoweredFlight(cancel.Power,abandoned).Status==PoweredFlightStatus.InvalidProposal,"canceled resource blocks dependent endpoint");
        Check(cancel.Engine.RetirePoweredFlightProposal(cancel.Power,abandoned)==PoweredFlightStatus.Retired,"owner retires stale joint lease");
        Check(cancel.Engine.PrepareFinitePropellant(cancel.Resource,cancel.EngineLease,new(16666),out cancel.FuelLease)==PropellantPreparationStatus.Prepared,"same genuine parent recomputes resource");
        cancel.Apply();Check(cancel.Engine.PublishPoweredFlight(cancel.Power,abandoned).Status==PoweredFlightStatus.InvalidProposal,"abandoned joint generation stays dead");
        var parentCancel=new Flight();parentCancel.Seal();Check(parentCancel.Engine.PreparePoweredFlight(parentCancel.Power,parentCancel.FuelLease,out var discarded)==PoweredFlightStatus.Prepared,"parent cancel prepare");
        parentCancel.Engine.DiscardSingleEngineProposal(parentCancel.Actuation,parentCancel.EngineLease);
        Check(parentCancel.Engine.RetirePoweredFlightProposal(parentCancel.Power,discarded)==PoweredFlightStatus.Invalidated&&
            parentCancel.Engine.ServicePoweredFlightDebt(parentCancel.Power).Status==PoweredFlightStatus.Invalidated,"parent cursor retirement is explicit terminal continuation");
        var episode=cancel.Engine.CopyPoweredFlightEpisode();
        Check(episode.Initial.StateRevision.Value==0&&episode.Initial.Endpoint.Epoch.Ticks==0&&episode.Initial.Resource.ResourceRevision==0&&
            episode.Initial.Actuator.ActuatorRevision==0&&episode.NumericalPolicyVersion==1,"immutable initial history provenance");
        Check(cancel.Engine.TryGetPoweredFlightRecord(0,out var first)&&first.BeforeRevision==episode.Initial.StateRevision&&
            first.DebtBefore.Ticks-first.DebtAfter.Ticks==16666,"first transition reconstructed with exact accounting");
        Console.WriteLine("POWERED_AUTHORITY PASS owner/nonmutation/atomic/revisions/exhaustion/leases/endpoint-only/terminal-ack/event/debt/history");
    }
    private static void EndpointFence(Flight f)
    {
        var view=f.Engine.State;Check(view.Spacecraft.TryGetAppliedEndpoint(Flight.Craft,out var endpoint),"canonical typed endpoint");
        Check(!view.Spacecraft.TryGetTranslation(Flight.Craft,out _,out _)&&!view.Spacecraft.TryGetRigidBody(Flight.Craft,out _)&&
            !view.Spacecraft.TryGetAttitude(Flight.Craft,out _),"no endpoint-to-legacy readers");
        bool shadowRefused=false;try{_=view.Spacecraft.GetAttitude(0);}catch(InvalidOperationException){shadowRefused=true;}Check(shadowRefused,"no stale indexed attitude shadow");
        foreach(var delta in new[]{-1L,1L})
            Check(SpacecraftMotionEvaluator.TryEvaluate(view,Flight.Craft,new(endpoint.Epoch.Ticks+delta),out _)==SpacecraftTranslationStatus.OutsideQualifiedEndpoint,"no endpoint extrapolation");
        PhysicalEventEpoch.TryCreate(endpoint.Epoch.Ticks,1,2,out var fractional);
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(view,Flight.Craft,fractional,out _)==SpacecraftTranslationStatus.OutsideQualifiedEndpoint,"fractional endpoint refusal");
        Check(SpacecraftMotionEvaluator.TryEvaluate(view,Flight.Craft,endpoint.Epoch,out var motion)==SpacecraftTranslationStatus.Success&&
            motion.PositionRoot==endpoint.PositionRoot&&motion.BodyToRoot==endpoint.BodyToRoot,"exact copied endpoint observation");
        var frames=new ReferenceFrameEvaluation[f.Graph.Count];
        Check(SpacecraftReferenceFrameEvaluator.TryEvaluate(view.Spacecraft,f.Graph,endpoint.Epoch,frames)==SpacecraftReferenceFrameEvaluationStatus.Success&&
            SpacecraftReferenceFrameEvaluator.TryEvaluate(view.Spacecraft,f.Graph,new(endpoint.Epoch.Ticks+1),frames)==SpacecraftReferenceFrameEvaluationStatus.TranslationEvaluationFailed,"frame endpoint policy");
        var oldLinear=(SpacecraftTranslationState[])Field(f.Store,"_translations");
        var oldAngular=(NovaCore.Simulation.Spacecraft.Rotation.SpacecraftRigidBodyRotationState[])Field(f.Store,"_rigidBodies");
        var oldAttitude=(SpacecraftAttitudeState[])Field(f.Store,"_attitudes");
        var before=Snapshot(f);
        Check(!f.Store.TryReplaceTranslation(oldLinear[0],oldLinear[0] with{Epoch=endpoint.Epoch})&&
            !f.Store.TryReplaceRigidBody(Flight.Craft,oldAngular[0],oldAngular[0] with{Epoch=endpoint.Epoch},out _)&&
            !f.Store.TryReplaceAttitude(Flight.Craft,oldAttitude[0],oldAttitude[0] with{Epoch=endpoint.Epoch},out _)&&
            !f.Store.TryReplaceContactResponse(oldLinear[0],oldLinear[0],oldAngular[0],oldAngular[0])&&
            !f.Store.TryPrepareContinuationSlot(oldLinear[0],oldAngular[0],out _),"all same-epoch legacy replacements refuse");
        Unchanged(f,before,"sticky endpoint replacement policy");
    }
}
