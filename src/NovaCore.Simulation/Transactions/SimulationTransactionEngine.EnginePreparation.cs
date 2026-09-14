using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    // Explicit composed-consumer lag budget: one maximal ordinary burst of engine edges.
    // Independently enforced across queue drain/refill; queue capacity is NOT assumed to bound history.
    internal const int EngineTransitionCapacity = 7;
    private sealed class EnginePreparationStorage(EnginePreparationAuthority authority)
    {
        internal readonly EnginePreparationAuthority Authority = authority;
        internal readonly object Seal = new();
        internal readonly CapturedEngineTransition[] Edges = new CapturedEngineTransition[EngineTransitionCapacity];
        internal int Count;
        internal ulong CapturedThrough, ConsumedThrough;
        internal ProposedEngineLatch Latch;
        internal long LastBoundary = -1;
        internal bool Active;
        internal EngineActuationPreview Preview;
        internal ContinuationClockState Clock;
        internal SpacecraftTranslationState Linear;
        internal SpacecraftRigidBodyRotationState Angular;
        internal SpacecraftPhysicalProperties Mass;
        internal SpacecraftDefinition Craft;
        internal EnginePreparationProgress Copy() => new(CapturedThrough, ConsumedThrough, Count, LastBoundary, Active, Latch);
    }
    private EnginePreparationStorage? _enginePreparation;

    private static bool IsEngineEdge(SpacecraftCommandKind kind) =>
        kind is SpacecraftCommandKind.IgniteRequest or SpacecraftCommandKind.ShutdownRequest;

    // Reserve capacity at admission so a full captured batch cannot strand a due edge ahead of Close.
    // Nonengine commands, including the reserved neutralization, never consume bridge storage.
    private bool CanReserveEngineTransition(SpacecraftCommandKind kind)
    {
        if (_enginePreparation is not { } p || !IsEngineEdge(kind)) return true;
        var reserved = p.Count;
        for (var i = 0; i < _commands!.Count; i++) if (IsEngineEdge(_commands.Pending[i].Intent.Kind)) reserved++;
        return reserved < EngineTransitionCapacity;
    }
    private bool CanCaptureEngineTransition(SpacecraftCommandKind kind) =>
        _enginePreparation is not { } p || !IsEngineEdge(kind) || p.Count < EngineTransitionCapacity;
    private void CaptureEngineTransition(in PendingSpacecraftCommand pending, CommandRevision revision)
    {
        if (_enginePreparation is not { } p || !IsEngineEdge(pending.Intent.Kind)) return;
        p.Edges[p.Count++] = new(pending.Sequence, pending.Epoch, pending.Intent.Kind, revision);
        p.CapturedThrough = pending.Sequence;
    }

    private EnginePreparationStatus EnterEnginePreparationPhase()
    {
        if (!_clock.PublicationPhase.IsOwnerThread) return EnginePreparationStatus.WrongOwnerThread;
        return _isExecutingGroup || !_clock.PublicationPhase.TryEnter(this)
            ? EnginePreparationStatus.ReentrantOperation : EnginePreparationStatus.Ready;
    }
    private bool OwnsEnginePreparation(EnginePreparationAuthority? authority) =>
        authority is not null && ReferenceEquals(authority, _enginePreparation?.Authority);

    /// <summary>Cold binding only, before any command consumption/closure. Never reconstructs missed edges from a snapshot.</summary>
    internal EnginePreparationStatus BindSingleEnginePreparation(SpacecraftCommandAuthority commands,
        IdealEngineDefinition? definition, out EnginePreparationAuthority? authority)
    {
        authority = null;
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try
        {
            if (_enginePreparation is not null) return EnginePreparationStatus.AlreadyBound;
            if (definition is null) return EnginePreparationStatus.InvalidDefinition;
            if (ValidateCommandAuthority(commands) != SpacecraftCommandStatus.Accepted) return EnginePreparationStatus.InvalidAuthority;
            if (_commands!.LastConsumedSequence != 0 || _commands.ClosedThrough != -1 || _clock.CurrentTime != commands.Origin)
                return EnginePreparationStatus.LateBinding;
            if (!_state.CreateView().Spacecraft.TryGetRigidBody(commands.Spacecraft, out _) ||
                !_state.CreateView().Spacecraft.TryGetDefinition(commands.Spacecraft, out _)) return EnginePreparationStatus.InvalidSource;
            authority = new(commands, definition);
            _enginePreparation = new(authority);
            return EnginePreparationStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal EnginePreparationStatus ObserveEnginePreparation(EnginePreparationAuthority authority, out EnginePreparationProgress progress)
    {
        progress = default;
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsEnginePreparation(authority)) return EnginePreparationStatus.InvalidAuthority;
            progress = _enginePreparation!.Copy(); return EnginePreparationStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal EnginePreparationStatus ObservePendingEngineTransition(EnginePreparationAuthority authority, int index,
        out CapturedEngineTransition transition)
    {
        transition = default;
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsEnginePreparation(authority)) return EnginePreparationStatus.InvalidAuthority;
            if ((uint)index >= (uint)_enginePreparation!.Count) return EnginePreparationStatus.InvalidIndex;
            transition = _enginePreparation.Edges[index]; return EnginePreparationStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal EnginePreparationStatus PrepareSingleEngineActuation(EnginePreparationAuthority authority,
        SimulationInstant target, out EngineActuationProposal proposal) => PrepareSingleEngineActuationCore(authority, target, false, out proposal);
    internal EnginePreparationStatus RefuseSingleEnginePreparationForTest(EnginePreparationAuthority authority,
        SimulationInstant target, out EngineActuationProposal proposal) => PrepareSingleEngineActuationCore(authority, target, true, out proposal);

    private EnginePreparationStatus PrepareSingleEngineActuationCore(EnginePreparationAuthority authority,
        SimulationInstant target, bool refusePreparedForTest, out EngineActuationProposal proposal)
    {
        proposal = default;
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsEnginePreparation(authority)) return EnginePreparationStatus.InvalidAuthority;
            var p = _enginePreparation!; var commands = authority.Commands;
            if (!SupportedCommandClock) return EnginePreparationStatus.UnsupportedClock;
            if (ValidateCommandAuthority(commands) != SpacecraftCommandStatus.Accepted) return EnginePreparationStatus.StaleSource;
            if (!commands.TryBoundaryAtOrAfter(_clock.CurrentTime, 0, out var index, out var start) || start != _clock.CurrentTime)
                return EnginePreparationStatus.InvalidInterval;
            if (index >= commands.LastIndex) return EnginePreparationStatus.SourceExhausted;
            if (!commands.TryBoundaryAtOrAfter(start, index + 1, out _, out var next) || target != next)
                return EnginePreparationStatus.InvalidInterval;
            if (_commands!.ClosedThrough != index) return EnginePreparationStatus.BoundaryNotClosed;
            if (p.Active) return EnginePreparationStatus.OutstandingProposal;
            if (index <= p.LastBoundary) return EnginePreparationStatus.IntervalConsumed;
            if (index != p.LastBoundary + 1) return EnginePreparationStatus.IntervalGap;
            if (HasContactProofBoundaryThrough(target)) return EnginePreparationStatus.PendingEvent;
            var view = _state.CreateView();
            if (!view.Spacecraft.TryGetTranslation(commands.Spacecraft, out var linear, out var mass) || !mass.IsValid ||
                !view.Spacecraft.TryGetRigidBody(commands.Spacecraft, out var angular) ||
                !view.Spacecraft.TryGetDefinition(commands.Spacecraft, out var craft) || craft.CarrierFrame != commands.RootFrame ||
                linear.Epoch > start || angular.Epoch > start)
                return EnginePreparationStatus.InvalidSource;

            var latch = p.Latch; var cursor = p.ConsumedThrough; var fingerprint = 14695981039346656037UL;
            var ignites = 0; var shutdowns = 0;
            for (var i = 0; i < p.Count; i++)
            {
                var edge = p.Edges[i];
                if (edge.Sequence <= cursor || edge.Epoch > start || !IsEngineEdge(edge.Kind)) return EnginePreparationStatus.StaleSource;
                cursor = edge.Sequence;
                if (edge.Kind == SpacecraftCommandKind.IgniteRequest) { latch = ProposedEngineLatch.Enabled; ignites++; }
                else { latch = ProposedEngineLatch.Off; shutdowns++; }
                fingerprint = MixEngineEdge(MixEngineEdge(MixEngineEdge(MixEngineEdge(fingerprint, edge.Sequence),
                    unchecked((ulong)edge.Epoch.Ticks)), (ulong)edge.Kind), edge.CommandRevision.Value);
            }
            if (cursor != p.CapturedThrough || cursor != _commands.State.EngineRequestSequence) return EnginePreparationStatus.StaleSource;
            var definition = authority.Definition.Values;
            var requested = _commands.State.RequestedThrottlePosition;
            var throttle = definition.HardwareAvailable && latch == ProposedEngineLatch.Enabled ? requested : 0d;
            var thrust = throttle * definition.MaximumThrustNewtons;
            var force = thrust == 0 ? Double3.Zero : definition.ThrustAxisBody * thrust;
            var moment = thrust == 0 ? Double3.Zero : Double3.Cross(definition.MountFromComMetres, force);
            var flow = thrust / definition.EffectiveExhaustMetresPerSecond;
            if (!double.IsFinite(throttle) || throttle < 0 || throttle > 1 || !double.IsFinite(thrust) ||
                !force.IsFinite || !moment.IsFinite || !double.IsFinite(flow)) return EnginePreparationStatus.ArithmeticFailure;
            var activity = !definition.HardwareAvailable ? ProposedEngineActivity.Unavailable :
                thrust > 0 ? ProposedEngineActivity.Firing : ProposedEngineActivity.Inactive;
            var clock = CaptureContinuationClock(); var timeline = _clock.Timeline.Revision;
            var preview = new EngineActuationPreview(EnginePreviewMeaning.ProposedIfApplied, definition, commands.Spacecraft,
                craft.BodyFrame, commands.RootFrame, start, target, index, view.Revision, timeline,
                _commands.Revision, _commands.LastConsumedSequence, p.ConsumedThrough, cursor, p.Count, ignites, shutdowns,
                fingerprint, _commands.State.LastEngineRequest, _commands.State.EngineRequestSequence, latch, activity,
                requested, throttle, thrust, force, moment, flow, EngineDemandFrame.BodyAboutCanonicalCom,
                EngineFeedAssumption.AvailableFeedRequiredFlowOnly);
            if (refusePreparedForTest) return EnginePreparationStatus.PreparationRefused;
            // Final applicability under the same exclusive owner phase, with no callback or outside work.
            if (ValidateCommandAuthority(commands) != SpacecraftCommandStatus.Accepted || _commands.ClosedThrough != index ||
                _commands.Revision != preview.SourceCommandRevision || _commands.LastConsumedSequence != preview.SourceCommandSequence ||
                _state.CreateView().Revision != preview.SourceStateRevision || CaptureContinuationClock() != clock || _clock.Timeline.Revision != timeline)
                return EnginePreparationStatus.StaleSource;
            // Fixed private PREPARATION bookkeeping only. No physical/canonical actuator/resource write exists here.
            p.Preview = preview; p.Clock = clock; p.Linear = linear; p.Angular = angular; p.Mass = mass; p.Craft = craft;
            p.Latch = latch; p.ConsumedThrough = cursor; p.LastBoundary = index; p.Active = true;
            for (var i = 0; i < p.Count; i++) p.Edges[i] = default;
            p.Count = 0;
            proposal = new(index, p.Seal);
            return EnginePreparationStatus.Prepared;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    private static ulong MixEngineEdge(ulong hash, ulong value) => unchecked((hash ^ value) * 1099511628211UL);

    internal EnginePreparationStatus PreviewSingleEngineActuation(EnginePreparationAuthority authority,
        EngineActuationProposal proposal, out EngineActuationPreview preview)
    {
        preview = default;
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try { return ReadSingleEngineActuationInOwnedPhase(authority, proposal, out preview); }
        finally { _clock.PublicationPhase.Exit(); }
    }

    // Shared source reader only. Public preview still acquires the same exclusive owner phase.
    private EnginePreparationStatus ReadSingleEngineActuationInOwnedPhase(EnginePreparationAuthority authority,
        EngineActuationProposal proposal, out EngineActuationPreview preview)
    {
        preview = default;
        if (!_clock.PublicationPhase.IsOwnedBy(this)) return EnginePreparationStatus.ReentrantOperation;
        if (!OwnsEnginePreparation(authority)) return EnginePreparationStatus.InvalidAuthority;
        var p = _enginePreparation!;
        if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Boundary != p.LastBoundary) return EnginePreparationStatus.InvalidProposal;
        var view = _state.CreateView();
        if (ValidateCommandAuthority(authority.Commands) != SpacecraftCommandStatus.Accepted ||
            CaptureContinuationClock() != p.Clock || view.Revision != p.Preview.SourceStateRevision ||
            _clock.Timeline.Revision != p.Preview.SourceTimelineRevision || _commands!.Revision != p.Preview.SourceCommandRevision ||
            _commands.LastConsumedSequence != p.Preview.SourceCommandSequence || _commands.ClosedThrough != p.LastBoundary ||
            !view.Spacecraft.TryGetTranslation(authority.Commands.Spacecraft, out var linear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(authority.Commands.Spacecraft, out var angular) ||
            !view.Spacecraft.TryGetDefinition(authority.Commands.Spacecraft, out var craft) ||
            linear != p.Linear || angular != p.Angular || mass != p.Mass || craft != p.Craft)
            return EnginePreparationStatus.StaleSource;
        preview = p.Preview; return EnginePreparationStatus.Preview;
    }

    /// <summary>Explicit owner retirement, including stale sources. Does not apply, acknowledge, rewind or resurrect edges.</summary>
    internal EnginePreparationStatus DiscardSingleEngineProposal(EnginePreparationAuthority authority, EngineActuationProposal proposal)
    {
        var entered = EnterEnginePreparationPhase(); if (entered != EnginePreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsEnginePreparation(authority)) return EnginePreparationStatus.InvalidAuthority;
            var p = _enginePreparation!;
            if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Boundary != p.LastBoundary) return EnginePreparationStatus.InvalidProposal;
            p.Active = false; p.Preview = default;
            return EnginePreparationStatus.Retired;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
}
