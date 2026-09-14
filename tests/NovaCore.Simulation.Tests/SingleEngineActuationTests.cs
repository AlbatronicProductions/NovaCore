using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class SingleEngineActuationTests
{
    private static void Check(bool condition, string contract)
    { if (!condition) throw new InvalidOperationException("Engine preparation: " + contract); }
    private static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a, Double3 b) => Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z);
    private static IdealEngineDefinition Definition(Double3? mount = null, Double3? axis = null,
        double thrust = 1200, double exhaust = 3000, bool available = true, ulong id = 1, uint version = 1)
    {
        Check(IdealEngineDefinition.TryCreate(id, version, mount ?? new(0,2,0), axis ?? Double3.UnitX,
            thrust, exhaust, available, out var d) == EnginePreparationStatus.Ready, "valid authored definition");
        return d!;
    }
    private readonly record struct PhysicalSnapshot(ContinuationClockState Clock, StateRevision Revision,
        TimelineRevision Timeline, SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular,
        SpacecraftPhysicalProperties Properties, int Pending, int ContactHistory);
    private sealed class Fixture
    {
        internal static readonly SpacecraftId Craft = new(101);
        internal static readonly ReferenceFrameId Root = new(1), Body = new(2);
        internal readonly SimulationClock Clock;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftCommandAuthority Commands;
        internal readonly EnginePreparationAuthority Authority;
        internal readonly long Origin;
        internal long Index;
        internal Fixture(IdealEngineDefinition? definition = null, long intervals = 5000, long origin = 0, bool bind = true, long segmentOffset = 0)
        {
            Origin = origin; var t = new SimulationInstant(origin);
            var graph = new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "root"));
            graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "body COM"));
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft,Root,Body,"engine test")],
                [new(Craft,new(t.Ticks+segmentOffset),DoubleQuaternion.Identity,new(0,0,.1),new(1,1,1),default,RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(10)], [new(Craft,Root,new(t.Ticks+segmentOffset),default,default,default)], graph.Build(), out var store,out _), "fixture");
            Clock = new(t,new SimulationTimeline(4)); Engine = new(Clock,new SimulationState(spacecraft:store),4);
            Check(Engine.PrepareSpacecraftCommands(Craft,intervals,out var command) == SpacecraftCommandStatus.Accepted,"command preparation");
            Commands=command!;
            if (bind)
            {
                Check(Engine.BindSingleEnginePreparation(Commands,definition ?? Definition(),out var authority)==EnginePreparationStatus.Ready,"engine binding");
                Authority=authority!;
            }
            else Authority=null!;
        }
        internal SimulationInstant Target => new(Origin+(Index+1)*1_000_000/60);
        internal SpacecraftCommandObservation Requested()
        { Check(Engine.ObserveSpacecraftCommands(Commands,out var v)==SpacecraftCommandStatus.Accepted,"command observation"); return v; }
        internal EnginePreparationProgress Progress()
        { Check(Engine.ObserveEnginePreparation(Authority,out var v)==EnginePreparationStatus.Ready,"preparation observation"); return v; }
        internal SpacecraftCommandAdmission Admit(SpacecraftCommandIntent intent) => Engine.AdmitSpacecraftCommand(Commands,1,Requested().LastAcceptedSequence+1,intent);
        internal SpacecraftCommandCommit Send(SpacecraftCommandIntent intent)
        {
            Check(Admit(intent).Status==SpacecraftCommandStatus.Accepted,"input admitted");
            var c=Engine.CommitNextSpacecraftCommand(Commands);
            Check(c.Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange,"requested commit"); return c;
        }
        internal void Close() => Check(Engine.CloseSpacecraftCommandBoundary(Commands,out _)==SpacecraftCommandStatus.BoundaryReady,"close");
        internal EngineActuationPreview Prepare(out EngineActuationProposal token)
        {
            var physical=Physical();var requested=Requested();
            Check(Engine.PrepareSingleEngineActuation(Authority,Target,out token)==EnginePreparationStatus.Prepared,"prepare");
            Check(Engine.PreviewSingleEngineActuation(Authority,token,out var preview)==EnginePreparationStatus.Preview,"conditional preview");
            Check(Physical()==physical && Requested()==requested,"successful preparation changes no canonical state, command, clock/debt or history");
            return preview;
        }
        internal PhysicalSnapshot Physical()
        {
            Engine.State.Spacecraft.TryGetTranslation(Craft,out var l,out var p);
            Engine.State.Spacecraft.TryGetRigidBody(Craft,out var a);
            return new(Engine.CaptureContinuationClock(),Engine.State.Revision,Clock.Timeline.Revision,l,a,p,
                Clock.Timeline.PendingCount,Engine.ProcessedPersistentContactCount);
        }
        internal void Advance()
        {
            var target=Target;var delta=target.Ticks-Clock.CurrentTime.Ticks;
            Clock.AdvanceByHostDuration(new(delta));
            Clock.AdvanceTo(target);Clock.ConsumePendingSimulationDebt(new(delta));Index++;
        }
    }

    internal static void Cheap()
    {
        Definitions(); Mathematics(); Capture(); Authority(); Determinism();
        Check(!RuntimeHelpers.IsReferenceOrContainsReferences<EngineActuationPreview>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<CapturedEngineTransition>(),"copied output has no live references");
        Check(typeof(EngineActuationPreview).GetProperties().All(p=>!p.Name.Contains("ForceRoot",StringComparison.Ordinal) &&
            !p.Name.Contains("ConsumedFuel",StringComparison.Ordinal)),"no root-force or consumed-fuel output");
        Console.WriteLine("ENGINE_CHEAP PASS definition/math/ordered-capture/refusal/seal/replay/body-frame/conditional-preview/determinism");
    }
    private static void Definitions()
    {
        foreach(var bad in new[]{0d,-1,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            Check(IdealEngineDefinition.TryCreate(1,1,default,Double3.UnitX,bad,3000,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid thrust");
            Check(IdealEngineDefinition.TryCreate(1,1,default,Double3.UnitX,1200,bad,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid exhaust");
        }
        foreach(var axis in new[]{Double3.Zero,new Double3(double.NaN,0,0),new Double3(0,double.PositiveInfinity,0)})
            Check(IdealEngineDefinition.TryCreate(1,1,default,axis,1200,3000,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid axis");
        Check(IdealEngineDefinition.TryCreate(1,1,new(0,double.NaN,0),Double3.UnitX,1200,3000,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid mount");
        Check(IdealEngineDefinition.TryCreate(0,1,default,Double3.UnitX,1200,3000,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid engine id");
        Check(IdealEngineDefinition.TryCreate(1,0,default,Double3.UnitX,1200,3000,true,out _)==EnginePreparationStatus.InvalidDefinition,"invalid version");
        Check(Definition().Values!=Definition(version:2).Values && Definition().Values!=Definition(id:2).Values,"version/identity distinction");
        var unit=Definition(axis:new(3,4,0)).Values.ThrustAxisBody;
        Check(unit==new Double3(.6,.8,0),"authored axis normalization");
        Check(Definition(axis:new(double.MaxValue,0,0)).Values.ThrustAxisBody==Double3.UnitX &&
            Definition(axis:new(double.Epsilon,0,0)).Values.ThrustAxisBody==Double3.UnitX,"scaled finite axis domain");
        var f=new Fixture();
        Check(f.Engine.BindSingleEnginePreparation(f.Commands,Definition(),out _)==EnginePreparationStatus.AlreadyBound,"duplicate identity/binding cannot overwrite");
        var late=new Fixture(bind:false);late.Send(SpacecraftCommandIntent.Ignite());
        Check(late.Engine.BindSingleEnginePreparation(late.Commands,Definition(),out _)==EnginePreparationStatus.LateBinding,"cannot reconstruct missing history");
        var cold=new Fixture(bind:false);
        Check(cold.Engine.BindSingleEnginePreparation(cold.Commands,null,out _)==EnginePreparationStatus.InvalidDefinition,"null definition");
        Check(cold.Engine.BindSingleEnginePreparation(f.Commands,Definition(),out _)==EnginePreparationStatus.InvalidAuthority,"foreign command authority");
    }
    private static void Mathematics()
    {
        foreach(var t in new[]{0d,1d,.25,1e-250})
        {
            var f=new Fixture();f.Send(SpacecraftCommandIntent.ThrottlePosition(t));f.Send(SpacecraftCommandIntent.Ignite());f.Close();
            var v=f.Prepare(out _);
            Check(v.ProposedLatch==ProposedEngineLatch.Enabled && Bits(v.ProposedRealizedThrottle,t),"enabled ideal throttle");
            Check(Bits(v.ProposedThrustNewtons,t*1200) && Bits(v.ProposedForceBodyNewtons.X,t*1200),"thrust analytical scalar");
            Check(v.ProposedForceBodyNewtons.Y==0 && v.ProposedForceBodyNewtons.Z==0 && v.ProposedMomentBodyNewtonMetres.X==0 &&
                v.ProposedMomentBodyNewtonMetres.Y==0 && v.ProposedMomentBodyNewtonMetres.Z== -2*(t*1200),"independent lever arm sign");
            Check(Bits(v.RequiredMassFlowKilogramsPerSecond,(t*1200)/3000),"effective exhaust flow equation");
            Check(v.Meaning==EnginePreviewMeaning.ProposedIfApplied && v.FeedAssumption==EngineFeedAssumption.AvailableFeedRequiredFlowOnly &&
                v.Frame==EngineDemandFrame.BodyAboutCanonicalCom,"conditional/no-fuel/body semantics");
        }
        var off=new Fixture();off.Send(SpacecraftCommandIntent.ThrottlePosition(.7));off.Close();var ov=off.Prepare(out var ot);
        Check(ov.RequestedThrottle==.7 && ov.ProposedLatch==ProposedEngineLatch.Off && ov.ProposedRealizedThrottle==0 &&
            ov.ProposedForceBodyNewtons==default && ov.ProposedMomentBodyNewtonMetres==default && ov.RequiredMassFlowKilogramsPerSecond==0,"Off does not consume latched throttle");
        off.Engine.DiscardSingleEngineProposal(off.Authority,ot);off.Advance();off.Send(SpacecraftCommandIntent.Ignite());off.Close();
        var on=off.Prepare(out var onToken);Check(on.ProposedRealizedThrottle==.7,"ignite uses latched throttle");
        Check(ov.ProposedLatch==ProposedEngineLatch.Off && ov.ProposedThrustNewtons==0,"preview copy isolation");
        off.Engine.DiscardSingleEngineProposal(off.Authority,onToken);off.Advance();off.Send(SpacecraftCommandIntent.Shutdown());off.Close();var shut=off.Prepare(out var st);
        Check(shut.ProposedLatch==ProposedEngineLatch.Off && shut.ProposedThrustNewtons==0,"shutdown");
        off.Engine.DiscardSingleEngineProposal(off.Authority,st);off.Advance();off.Send(SpacecraftCommandIntent.Ignite());off.Close();Check(off.Prepare(out _).ProposedRealizedThrottle==.7,"restart");
        var unavailable=new Fixture(Definition(available:false));unavailable.Send(SpacecraftCommandIntent.Ignite());unavailable.Send(SpacecraftCommandIntent.ThrottlePosition(1));unavailable.Close();
        var uv=unavailable.Prepare(out _);
        Check(uv.ProposedLatch==ProposedEngineLatch.Enabled && !uv.Definition.HardwareAvailable && uv.ProposedActivity==ProposedEngineActivity.Unavailable &&
            uv.ProposedRealizedThrottle==0 && uv.ProposedForceBodyNewtons==default && uv.ProposedMomentBodyNewtonMetres==default && uv.RequiredMassFlowKilogramsPerSecond==0 && uv.IgniteCount==1,"unavailable preserves edge/latch but produces zero demand");
        foreach(var (mount,axis,expected) in new[]{(Double3.Zero,Double3.UnitX,Double3.Zero),(new Double3(0,-2,0),Double3.UnitX,new Double3(0,0,2400)),
            (new Double3(0,2,0),-Double3.UnitX,new Double3(0,0,2400))})
        {
            var f=new Fixture(Definition(mount,axis));f.Send(SpacecraftCommandIntent.Ignite());f.Send(SpacecraftCommandIntent.ThrottlePosition(1));f.Close();
            Check(f.Prepare(out _).ProposedMomentBodyNewtonMetres==expected,"COM/mirrored mount and axis");
        }
        var max=new Fixture(Definition(mount:Double3.Zero,thrust:double.MaxValue,exhaust:double.MaxValue));
        max.Send(SpacecraftCommandIntent.Ignite());max.Send(SpacecraftCommandIntent.ThrottlePosition(1));max.Close();
        var mx=max.Prepare(out _);Check(mx.ProposedThrustNewtons==double.MaxValue && mx.RequiredMassFlowKilogramsPerSecond==1,"maximum finite admitted result");
        foreach(var d in new[]{Definition(mount:new(0,double.MaxValue,0)),Definition(exhaust:double.Epsilon)})
        {
            var f=new Fixture(d);f.Send(SpacecraftCommandIntent.Ignite());f.Send(SpacecraftCommandIntent.ThrottlePosition(1));f.Close();
            var before=f.Progress();var physical=f.Physical();var requested=f.Requested();
            Check(f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)==EnginePreparationStatus.ArithmeticFailure &&
                f.Progress()==before && f.Physical()==physical && f.Requested()==requested,"overflow refusal leaves edge pending and every authority unchanged");
        }
    }
    private static void Capture()
    {
        foreach(var edges in new[]{new[]{SpacecraftCommandIntent.Ignite(),SpacecraftCommandIntent.Shutdown()},
            new[]{SpacecraftCommandIntent.Shutdown(),SpacecraftCommandIntent.Ignite()},
            new[]{SpacecraftCommandIntent.Ignite(),SpacecraftCommandIntent.Shutdown(),SpacecraftCommandIntent.Ignite()},
            new[]{SpacecraftCommandIntent.Ignite(),SpacecraftCommandIntent.Ignite(),SpacecraftCommandIntent.Shutdown()}})
        {
            var f=new Fixture();f.Send(SpacecraftCommandIntent.ThrottlePosition(.25));
            for(var i=0;i<edges.Length;i++)
            {
                var commit=f.Send(edges[i]);
                Check(f.Engine.ObservePendingEngineTransition(f.Authority,i,out var e)==EnginePreparationStatus.Ready &&
                    e.Sequence==commit.Sequence && e.Kind==edges[i].Kind && e.Epoch==commit.EffectiveEpoch && e.CommandRevision==commit.Observation.Revision,"complete fixed ordered capture");
            }
            var before=f.Progress();Check(before.PreparationConsumedSequence==0 && before.CapturedThroughSequence==f.Requested().LastConsumedSequence,"command cursor independent before seal");
            Check(f.Requested().State.LastEngineRequest==edges[^1].Kind && before.PendingEngineTransitions==edges.Length,"final snapshot insufficient; bridge retains all edges");
            f.Close();var preview=f.Prepare(out _);
            Check(preview.EngineTransitionCount==edges.Length && preview.IgniteCount+preview.ShutdownCount==edges.Length &&
                preview.ProposedLatch==(edges[^1].Kind==SpacecraftCommandKind.IgniteRequest?ProposedEngineLatch.Enabled:ProposedEngineLatch.Off),"order not command-class precedence");
            Check(f.Progress().PreparationConsumedSequence==before.CapturedThroughSequence && f.Progress().PendingEngineTransitions==0,"private consumption seals once");
        }
        var full=new Fixture();
        for(var i=0;i<SimulationTransactionEngine.EngineTransitionCapacity;i++) full.Send(SpacecraftCommandIntent.Ignite());
        var snapshot=full.Requested();var captured=full.Progress();
        Check(full.Admit(SpacecraftCommandIntent.Shutdown()).Status==SpacecraftCommandStatus.Capacity && full.Requested()==snapshot && full.Progress()==captured,"drain/refill saturation with empty command queue preserves history");
        full.Send(SpacecraftCommandIntent.ThrottlePosition(.5));
        var seq=full.Requested().LastAcceptedSequence+1;
        Check(full.Engine.RevokeSpacecraftControl(full.Commands,1,seq).Status==SpacecraftCommandStatus.Accepted &&
            full.Engine.CommitNextSpacecraftCommand(full.Commands).Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange,"neutralization independent of edge capacity");
        full.Close();Check(full.Prepare(out _).EngineTransitionCount==SimulationTransactionEngine.EngineTransitionCapacity,"full bridge can close and consume without deadlock");
        var reserve=new Fixture();
        for(var i=0;i<6;i++)reserve.Send(SpacecraftCommandIntent.Ignite());
        Check(reserve.Admit(SpacecraftCommandIntent.Shutdown()).Status==SpacecraftCommandStatus.Accepted,"last reserved edge");
        Check(reserve.Admit(SpacecraftCommandIntent.Ignite()).Status==SpacecraftCommandStatus.Capacity,"pending plus captured admission reserve");
        Check(reserve.Engine.CommitNextSpacecraftCommand(reserve.Commands).Status==SpacecraftCommandStatus.Committed,"previously accepted edge retained");
        reserve.Close();Check(reserve.Prepare(out _).ProposedLatch==ProposedEngineLatch.Off,"accepted shutdown survives saturation");
        var empty=new Fixture();empty.Close();var ev=empty.Prepare(out _);
        Check(ev.EngineTransitionCount==0 && ev.PreparationCursorBefore==0 && ev.PreparationCursorAfter==0 && ev.ProposedLatch==ProposedEngineLatch.Off,"empty range valid without fabricated consumption");
        var refused=new Fixture();refused.Admit(SpacecraftCommandIntent.Ignite());
        var refusedCommand=refused.Requested();var refusedBridge=refused.Progress();var refusedPhysical=refused.Physical();
        Check(refused.Engine.RefusePreparedSpacecraftCommandForTest(refused.Commands).Status==SpacecraftCommandStatus.PreparationRefused &&
            refused.Requested()==refusedCommand && refused.Progress()==refusedBridge && refused.Physical()==refusedPhysical,"command refusal captures no phantom edge or partial authority");
        Check(refused.Engine.CommitNextSpacecraftCommand(refused.Commands).Status==SpacecraftCommandStatus.Committed && refused.Progress().PendingEngineTransitions==1,"refused command still captures exactly once on valid commit");
    }
    private static void Authority()
    {
        var f=new Fixture(intervals:3);f.Send(SpacecraftCommandIntent.Ignite());
        var before=f.Progress();
        Check(f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)==EnginePreparationStatus.BoundaryNotClosed && f.Progress()==before,"open boundary refused");
        f.Close();var requested=f.Requested();var physical=f.Physical();
        Check(f.Engine.RefuseSingleEnginePreparationForTest(f.Authority,f.Target,out var none)==EnginePreparationStatus.PreparationRefused &&
            f.Progress()==before && f.Requested()==requested && f.Physical()==physical,"final refusal loses no edge/state");
        Check(f.Engine.PreviewSingleEngineActuation(f.Authority,none,out _)==EnginePreparationStatus.InvalidProposal,"failed preparation issues no capability");
        Check(f.Engine.PrepareSingleEngineActuation(f.Authority,new(f.Target.Ticks+1),out _)==EnginePreparationStatus.InvalidInterval,"exact target only");
        var first=f.Prepare(out var token);var sealedProgress=f.Progress();
        Check(f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)==EnginePreparationStatus.OutstandingProposal && f.Progress()==sealedProgress,"duplicate prepare is refused no replay");
        Check(f.Engine.PreviewSingleEngineActuation(f.Authority,new(0),out _)==EnginePreparationStatus.InvalidProposal &&
            f.Engine.PreviewSingleEngineActuation(f.Authority,new(0,new object()),out _)==EnginePreparationStatus.InvalidProposal,"default/fabricated seals refused");
        var foreign=new Fixture(Definition(id:2));foreign.Close();foreign.Prepare(out var foreignToken);
        Check(f.Engine.PreviewSingleEngineActuation(f.Authority,foreignToken,out _)==EnginePreparationStatus.InvalidProposal &&
            f.Engine.PrepareSingleEngineActuation(foreign.Authority,f.Target,out _)==EnginePreparationStatus.InvalidAuthority &&
            foreign.Engine.PreviewSingleEngineActuation(f.Authority,token,out _)==EnginePreparationStatus.InvalidAuthority,"foreign engine/definition/binding/token");
        Check(Task.Run(()=>f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)).GetAwaiter().GetResult()==EnginePreparationStatus.WrongOwnerThread,"wrong owner");
        Check(f.Clock.PublicationPhase.TryEnter(f),"hold phase");
        try
        {
            Check(f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)==EnginePreparationStatus.ReentrantOperation &&
                f.Engine.PreviewSingleEngineActuation(f.Authority,token,out _)==EnginePreparationStatus.ReentrantOperation &&
                f.Engine.DiscardSingleEngineProposal(f.Authority,token)==EnginePreparationStatus.ReentrantOperation,"reentrant operations refuse");
        }
        finally{f.Clock.PublicationPhase.Exit();}
        var prospective=f.Admit(SpacecraftCommandIntent.ThrottlePosition(.9));
        Check(prospective.EffectiveEpoch==f.Target && f.Engine.PreviewSingleEngineActuation(f.Authority,token,out var afterAdmission)==EnginePreparationStatus.Preview && afterAdmission==first,"future ingress alone does not stale closed prefix");
        f.Clock.AdvanceByHostDuration(new(1));
        Check(f.Engine.PreviewSingleEngineActuation(f.Authority,token,out _)==EnginePreparationStatus.StaleSource,"changed debt stales source");
        Check(f.Engine.DiscardSingleEngineProposal(f.Authority,token)==EnginePreparationStatus.Retired,"explicit stale-slot cleanup");
        Check(f.Engine.PreviewSingleEngineActuation(f.Authority,token,out _)==EnginePreparationStatus.InvalidProposal &&
            f.Engine.DiscardSingleEngineProposal(f.Authority,token)==EnginePreparationStatus.InvalidProposal,"retired token cannot replay");
        Check(f.Engine.PrepareSingleEngineActuation(f.Authority,f.Target,out _)==EnginePreparationStatus.IntervalConsumed &&
            f.Progress().PreparationConsumedSequence==first.PreparationCursorAfter,"retirement does not resurrect consumed range");
        // Fresh continuation fixture: private preparation continuity, not canonical hardware publication.
        var sequence=new Fixture(intervals:2);sequence.Send(SpacecraftCommandIntent.Ignite());sequence.Close();var initial=sequence.Prepare(out var old);
        sequence.Engine.DiscardSingleEngineProposal(sequence.Authority,old);sequence.Advance();sequence.Close();var next=sequence.Prepare(out var current);
        Check(next.EngineTransitionCount==0 && next.PreparationCursorAfter==initial.PreparationCursorAfter && next.ProposedLatch==ProposedEngineLatch.Enabled &&
            next.Start.Ticks==16666 && next.End.Ticks==33333,"no-edge private preparation continuation preserves lattice");
        Check(sequence.Engine.PreviewSingleEngineActuation(sequence.Authority,old,out _)==EnginePreparationStatus.InvalidProposal,"older proposal cannot alias new seal frontier");
        sequence.Engine.DiscardSingleEngineProposal(sequence.Authority,current);sequence.Advance();sequence.Close();
        Check(sequence.Engine.PrepareSingleEngineActuation(sequence.Authority,sequence.Target,out _)==EnginePreparationStatus.SourceExhausted,"finite source end");
        var futureSource=new Fixture(segmentOffset:1);futureSource.Send(SpacecraftCommandIntent.Ignite());futureSource.Close();
        var futureProgress=futureSource.Progress();var futurePhysical=futureSource.Physical();
        Check(futureSource.Engine.PrepareSingleEngineActuation(futureSource.Authority,futureSource.Target,out _)==EnginePreparationStatus.InvalidSource &&
            futureSource.Progress()==futureProgress && futureSource.Physical()==futurePhysical,"future physical segment cannot qualify earlier interval; pending edge retained");
        var skipped=new Fixture();skipped.Close();skipped.Advance();skipped.Close();
        Check(skipped.Engine.PrepareSingleEngineActuation(skipped.Authority,skipped.Target,out _)==EnginePreparationStatus.IntervalGap,"no invented unprepared interval");
        foreach(var tick in new[]{1L,16666L})
        {
            var events=new Fixture();events.Send(SpacecraftCommandIntent.Ignite());events.Close();
            events.Clock.Timeline.Schedule(events.Clock.CurrentTime,new(new(1),new(tick),0,SimulationEventKind.Marker));
            var eventProgress=events.Progress();var eventPhysical=events.Physical();
            Check(events.Engine.PrepareSingleEngineActuation(events.Authority,events.Target,out _)==EnginePreparationStatus.PendingEvent &&
                events.Progress()==eventProgress && events.Physical()==eventPhysical,"event at/before target refuses without consuming bridge");
        }
        var changedTimeline=new Fixture();changedTimeline.Close();changedTimeline.Prepare(out var timelineToken);
        changedTimeline.Clock.Timeline.Schedule(changedTimeline.Clock.CurrentTime,new(new(1),new(50000),0,SimulationEventKind.Marker));
        Check(changedTimeline.Engine.PreviewSingleEngineActuation(changedTimeline.Authority,timelineToken,out _)==EnginePreparationStatus.StaleSource,"changed TimelineRevision stales sealed preview even beyond its interval");
        var revision=new Fixture();revision.Close();revision.Prepare(out var revisionToken);revision.Engine.SaturateCommandRevisionForTest(revision.Commands);
        Check(revision.Engine.PreviewSingleEngineActuation(revision.Authority,revisionToken,out _)==EnginePreparationStatus.StaleSource,"stale command revision");
        var physics=new Fixture();physics.Close();physics.Prepare(out var physicsToken);
        var replacement=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(physics.Engine.State,new(Fixture.Craft,new(1,0,0),physics.Clock.CurrentTime));
        Check(replacement.Succeeded && physics.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed,"external physical mutation witness");
        Check(physics.Engine.PreviewSingleEngineActuation(physics.Authority,physicsToken,out _)==EnginePreparationStatus.StaleSource,"physical authority invalidates preview");
        foreach(var pause in new[]{false,true})
        {
            var clock=new Fixture();clock.Close();if(pause)clock.Clock.Pause();else clock.Clock.TrySetRate(SimulationRate.Two);
            var progress=clock.Progress();Check(clock.Engine.PrepareSingleEngineActuation(clock.Authority,clock.Target,out _)==EnginePreparationStatus.UnsupportedClock && clock.Progress()==progress,"unsupported pause/rate no consumption");
        }
    }
    private static void Determinism()
    {
        EngineActuationPreview Run(bool interleaved)
        {
            var f=new Fixture(origin:1234567);
            var stream=new[]{SpacecraftCommandIntent.Ignite(),SpacecraftCommandIntent.ThrottlePosition(.25),SpacecraftCommandIntent.Shutdown(),SpacecraftCommandIntent.Ignite()};
            foreach(var input in stream)
            {
                Check(f.Admit(input).Status==SpacecraftCommandStatus.Accepted,"same admitted stream");
                if(interleaved){ f.Engine.CommitNextSpacecraftCommand(f.Commands); for(var i=0;i<17;i++)f.Requested(); }
            }
            if(!interleaved)for(var i=0;i<stream.Length;i++)f.Engine.CommitNextSpacecraftCommand(f.Commands);
            f.Close();return f.Prepare(out _);
        }
        var a=Run(false);var b=Run(true);
        Check(a==b && Bits(a.ProposedForceBodyNewtons,b.ProposedForceBodyNewtons) && Bits(a.ProposedMomentBodyNewtonMetres,b.ProposedMomentBodyNewtonMetres) &&
            Bits(a.RequiredMassFlowKilogramsPerSecond,b.RequiredMassFlowKilogramsPerSecond),"exact values/provenance independent of admission/poll/display partition");
        var future=new Fixture();future.Clock.AdvanceByHostDuration(new(16666));
        future.Admit(SpacecraftCommandIntent.Ignite());future.Admit(SpacecraftCommandIntent.ThrottlePosition(.25));future.Close();future.Prepare(out var zero);
        future.Engine.DiscardSingleEngineProposal(future.Authority,zero);future.Clock.AdvanceTo(new(16666));future.Clock.ConsumePendingSimulationDebt(new(16666));future.Index++;
        future.Engine.CommitNextSpacecraftCommand(future.Commands);future.Engine.CommitNextSpacecraftCommand(future.Commands);future.Close();var late=future.Prepare(out _);
        var immediate=new Fixture();immediate.Close();immediate.Prepare(out var z);immediate.Engine.DiscardSingleEngineProposal(immediate.Authority,z);immediate.Advance();
        immediate.Send(SpacecraftCommandIntent.Ignite());immediate.Send(SpacecraftCommandIntent.ThrottlePosition(.25));immediate.Close();
        Check(late==immediate.Prepare(out _),"same committed epochs independent of admission time");
        var rotate=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,Math.PI/2);
        var futureRoot=rotate.Rotate(a.ProposedForceBodyNewtons);
        Check(a.Frame==EngineDemandFrame.BodyAboutCanonicalCom && a.BodyFrame==Fixture.Body &&
            Math.Abs(futureRoot.X)<1e-10 && Math.Abs(futureRoot.Y-300)<1e-10 && a.ProposedForceBodyNewtons==new Double3(300,0,0),"rotating body demand must rotate in future dynamics, not freeze at source root orientation");
    }
}
