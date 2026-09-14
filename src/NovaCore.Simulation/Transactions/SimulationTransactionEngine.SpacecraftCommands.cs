using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    // Mutable authority is private to the transaction engine. Issued capabilities never expose these arrays/fields.
    private sealed class CommandStorage(SpacecraftCommandAuthority authority)
    {
        internal readonly SpacecraftCommandAuthority Authority = authority;
        internal readonly PendingSpacecraftCommand[] Pending = new PendingSpacecraftCommand[SpacecraftCommandAuthority.OrdinaryCapacity + 1];
        internal int Count;
        internal ulong LastAcceptedSequence, LastConsumedSequence;
        internal long ClosedThrough = -1;
        internal bool Revoked;
        internal SpacecraftCommandState State;
        internal CommandRevision Revision;
        internal SimulationInstant LastTransitionEpoch = authority.Origin;
        internal SimulationInstant LastAdmittedHorizon = authority.Origin;
        internal SpacecraftCommandObservation Copy() => new(Authority.Spacecraft, Authority.Lease, State, Revision,
            LastTransitionEpoch, LastConsumedSequence, Revoked, Count, LastAcceptedSequence, ClosedThrough,
            Count == 0 ? default : Pending[0].Epoch);
    }
    private CommandStorage? _commands;

    private SpacecraftCommandStatus EnterCommandPhase()
    {
        if (!_clock.PublicationPhase.IsOwnerThread) return SpacecraftCommandStatus.WrongOwnerThread;
        return _isExecutingGroup || !_clock.PublicationPhase.TryEnter(this)
            ? SpacecraftCommandStatus.ReentrantOperation : SpacecraftCommandStatus.Accepted;
    }

    private bool SupportedCommandClock => !_clock.IsPaused && _clock.Rate == SimulationRate.One && _clock.RateRemainder == 0;

    /// <summary>Cold preparation; one owner lifetime, no reset/rebind. Uses an explicit finite 60 Hz schedule in either physical regime.</summary>
    internal SpacecraftCommandStatus PrepareSpacecraftCommands(SpacecraftId spacecraft, long intervals,
        out SpacecraftCommandAuthority? authority)
    {
        authority = null;
        var entered = EnterCommandPhase(); if (entered != SpacecraftCommandStatus.Accepted) return entered;
        try
        {
            if (_commands is not null) return SpacecraftCommandStatus.AlreadyPrepared;
            if (!SupportedCommandClock) return SpacecraftCommandStatus.UnsupportedClock;
            if (intervals <= 0) return SpacecraftCommandStatus.InvalidInput;
            var end = (Int128)_clock.CurrentTime.Ticks + (Int128)intervals * SimulationDuration.TicksPerSecond / 60;
            if (end > long.MaxValue) return SpacecraftCommandStatus.ArithmeticOverflow;
            if (!_state.CreateView().Spacecraft.TryGetTranslation(spacecraft, out var linear, out _))
                return SpacecraftCommandStatus.SubjectUnavailable;
            authority = new(spacecraft, linear.RootFrame, _clock.CurrentTime, new((long)end), intervals);
            _commands = new(authority);
            return SpacecraftCommandStatus.Accepted;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    private SpacecraftCommandStatus ValidateCommandAuthority(SpacecraftCommandAuthority? authority)
    {
        if (authority is null || !ReferenceEquals(authority, _commands?.Authority)) return SpacecraftCommandStatus.InvalidAuthority;
        if (!SupportedCommandClock) return SpacecraftCommandStatus.UnsupportedClock;
        if (_clock.CurrentTime < authority.Origin || _clock.CurrentTime > authority.End) return SpacecraftCommandStatus.SourceExhausted;
        if (!_state.CreateView().Spacecraft.TryGetTranslation(authority.Spacecraft, out var linear, out _) || linear.RootFrame != authority.RootFrame)
            return SpacecraftCommandStatus.SubjectUnavailable;
        if (_commands!.Count > 0 && _commands!.Pending[0].Epoch < _clock.CurrentTime) return SpacecraftCommandStatus.MissedBoundary;
        if (!_clock.TryGetPendingSimulationDebtTarget(out var horizon)) return SpacecraftCommandStatus.ArithmeticOverflow;
        if (horizon < _commands!.LastAdmittedHorizon) return SpacecraftCommandStatus.FundedHorizonRegressed;
        return SpacecraftCommandStatus.Accepted;
    }

    private static bool UnitIntent(Double3 value) => value.IsFinite && Math.Abs(value.X) <= 1 && Math.Abs(value.Y) <= 1 && Math.Abs(value.Z) <= 1;
    private static bool ValidCommandTarget(SpacecraftCommandTarget target, ReferenceFrameId root)
    {
        if (target.Kind == CommandTargetKind.None) return target == default;
        if (target.Frame != root) return false; // No implicit camera/body/other-frame conversion.
        return target.Kind switch
        {
            CommandTargetKind.CaptureAttitude => target.Attitude == default && target.AngularRate == default,
            CommandTargetKind.Attitude => target.AngularRate == default &&
                SpacecraftAttitudeEvaluator.TryCanonicalize(target.Attitude, out _) == SpacecraftAttitudeEvaluationStatus.Success,
            CommandTargetKind.AngularRate => target.Attitude == default && target.AngularRate.IsFinite,
            _ => false
        };
    }

    private static bool ValidCommand(SpacecraftCommandIntent intent, ReferenceFrameId root) => intent.Kind switch
    {
        SpacecraftCommandKind.IgniteRequest or SpacecraftCommandKind.ShutdownRequest or SpacecraftCommandKind.RcsEnabled => true,
        SpacecraftCommandKind.HeldAxes => UnitIntent(intent.Rotation) && UnitIntent(intent.Translation),
        SpacecraftCommandKind.ThrottlePosition => double.IsFinite(intent.Throttle) && intent.Throttle >= 0 && intent.Throttle <= 1,
        SpacecraftCommandKind.Mode => intent.Mode is RequestedControlMode.Manual or RequestedControlMode.RateAssist or RequestedControlMode.AttitudeAssist,
        SpacecraftCommandKind.Target => ValidCommandTarget(intent.Target, root),
        _ => false
    };

    internal SpacecraftCommandAdmission AdmitSpacecraftCommand(SpacecraftCommandAuthority authority, ulong lease,
        ulong sequence, in SpacecraftCommandIntent intent) => AdmitSpacecraftCommandCore(authority, lease, sequence, intent, false);

    /// <summary>Immediately seals ingress; neutralization itself remains ordered at the funded horizon. No implicit reacquisition.</summary>
    internal SpacecraftCommandAdmission RevokeSpacecraftControl(SpacecraftCommandAuthority authority, ulong lease, ulong sequence) =>
        AdmitSpacecraftCommandCore(authority, lease, sequence, SpacecraftCommandIntent.Neutralize(), true);

    private SpacecraftCommandAdmission AdmitSpacecraftCommandCore(SpacecraftCommandAuthority authority, ulong lease,
        ulong sequence, in SpacecraftCommandIntent intent, bool revoke)
    {
        var entered = EnterCommandPhase(); if (entered != SpacecraftCommandStatus.Accepted) return new(entered);
        try
        {
            var status = ValidateCommandAuthority(authority); if (status != SpacecraftCommandStatus.Accepted) return new(status);
            if (lease != authority.Lease) return new(SpacecraftCommandStatus.InvalidAuthority);
            if (sequence == _commands!.LastAcceptedSequence) return new(SpacecraftCommandStatus.DuplicateOrStaleSequence);
            if (_commands!.LastAcceptedSequence == ulong.MaxValue) return new(SpacecraftCommandStatus.SequenceOverflow);
            if (sequence < _commands!.LastAcceptedSequence) return new(SpacecraftCommandStatus.DuplicateOrStaleSequence);
            if (sequence != _commands!.LastAcceptedSequence + 1) return new(SpacecraftCommandStatus.SequenceGap);
            if (_commands!.Revoked) return new(SpacecraftCommandStatus.IngressRevoked);
            if (!revoke && !ValidCommand(intent, authority.RootFrame)) return new(SpacecraftCommandStatus.InvalidInput);
            if (_commands!.Count >= (revoke ? _commands!.Pending.Length : SpacecraftCommandAuthority.OrdinaryCapacity)) return new(SpacecraftCommandStatus.Capacity);
            if (!CanReserveEngineTransition(intent.Kind)) return new(SpacecraftCommandStatus.Capacity);
            if (!_clock.TryGetPendingSimulationDebtTarget(out var horizon)) return new(SpacecraftCommandStatus.ArithmeticOverflow);
            if (!authority.TryBoundaryAtOrAfter(horizon, _commands!.ClosedThrough + 1, out _, out var epoch)) return new(SpacecraftCommandStatus.SourceExhausted);
            if (HasContactProofBoundaryThrough(epoch)) return new(SpacecraftCommandStatus.PendingEvent);
            // Final applicability is still under the same exclusive phase; no callbacks or external work occurred.
            status = ValidateCommandAuthority(authority); if (status != SpacecraftCommandStatus.Accepted) return new(status);
            var slot = _commands!.Count;
            while (slot > 0 && _commands!.Pending[slot - 1].Epoch > epoch)
            { _commands!.Pending[slot] = _commands!.Pending[slot - 1]; slot--; }
            _commands!.Pending[slot] = new(sequence, epoch, intent);
            _commands!.Count++; _commands!.LastAcceptedSequence = sequence;
            _commands!.LastAdmittedHorizon = horizon;
            if (revoke) _commands!.Revoked = true;
            return new(SpacecraftCommandStatus.Accepted, sequence, epoch);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    /// <summary>One independently reported transition, at its exact epoch; never advances or services physical time.</summary>
    internal SpacecraftCommandCommit CommitNextSpacecraftCommand(SpacecraftCommandAuthority authority) => CommitNextSpacecraftCommandCore(authority, false);
    internal SpacecraftCommandCommit RefusePreparedSpacecraftCommandForTest(SpacecraftCommandAuthority authority) => CommitNextSpacecraftCommandCore(authority, true);

    private SpacecraftCommandCommit CommitNextSpacecraftCommandCore(SpacecraftCommandAuthority authority, bool refusePreparedForTest)
    {
        var entered = EnterCommandPhase(); if (entered != SpacecraftCommandStatus.Accepted) return new(entered);
        try
        {
            var status = ValidateCommandAuthority(authority); if (status != SpacecraftCommandStatus.Accepted) return new(status);
            if (!authority.TryBoundaryAtOrAfter(_clock.CurrentTime, 0, out var index, out var current) || current != _clock.CurrentTime)
                return new(SpacecraftCommandStatus.NotAtBoundary);
            if (index <= _commands!.ClosedThrough) return new(SpacecraftCommandStatus.BoundaryClosed);
            if (_commands!.Count == 0) return new(SpacecraftCommandStatus.NoCommand, Observation: _commands!.Copy());
            var pending = _commands!.Pending[0];
            if (pending.Epoch > current) return new(SpacecraftCommandStatus.Pending, pending.Sequence, pending.Epoch, _commands!.Copy());
            if (HasContactProofBoundaryThrough(current)) return new(SpacecraftCommandStatus.PendingEvent);
            var before = _commands!.State; var after = before; var intent = pending.Intent;
            switch (intent.Kind)
            {
                case SpacecraftCommandKind.IgniteRequest:
                case SpacecraftCommandKind.ShutdownRequest:
                    after = after with { LastEngineRequest = intent.Kind, EngineRequestSequence = pending.Sequence }; break;
                case SpacecraftCommandKind.HeldAxes:
                    after = after with { RotationIntent = intent.Rotation, TranslationIntent = intent.Translation }; break;
                case SpacecraftCommandKind.ThrottlePosition: after = after with { RequestedThrottlePosition = intent.Throttle }; break;
                case SpacecraftCommandKind.RcsEnabled: after = after with { RequestedRcsEnabled = intent.Enabled }; break;
                case SpacecraftCommandKind.Mode: after = after with { RequestedMode = intent.Mode }; break;
                case SpacecraftCommandKind.Target:
                    var target = intent.Target;
                    if (target.Kind == CommandTargetKind.CaptureAttitude)
                    {
                        if (SpacecraftMotionEvaluator.TryEvaluate(_state.CreateView(), authority.Spacecraft, current, out var motion) != SpacecraftTranslationStatus.Success)
                            return new(SpacecraftCommandStatus.InvalidTarget);
                        target = new(CommandTargetKind.Attitude, motion.RootFrame, motion.BodyToRoot, default);
                    }
                    else if (target.Kind == CommandTargetKind.Attitude && target != before.Target)
                    {
                        // Prepare at E against its deterministic predecessor, never against admission-time state.
                        // Re-submitting the copied committed target preserves its exact bits without renormalization drift.
                        if (SpacecraftAttitudeEvaluator.TryCanonicalize(target.Attitude, out var canonical) != SpacecraftAttitudeEvaluationStatus.Success)
                            return new(SpacecraftCommandStatus.InvalidTarget);
                        target = target with { Attitude = canonical };
                    }
                    after = after with { Target = target }; break;
                case SpacecraftCommandKind.Neutralize:
                    // Held intent/assist are released. Latched throttle/RCS and engine requests are not silently rewritten.
                    after = after with { RotationIntent = default, TranslationIntent = default, RequestedMode = RequestedControlMode.Manual, Target = default }; break;
                default: return new(SpacecraftCommandStatus.InvalidInput);
            }
            var changed = after != before;
            if (changed && _commands!.Revision.Value == ulong.MaxValue) return new(SpacecraftCommandStatus.RevisionOverflow);
            var revision = changed ? new CommandRevision(_commands!.Revision.Value + 1) : _commands!.Revision;
            if (!CanCaptureEngineTransition(intent.Kind)) return new(SpacecraftCommandStatus.Capacity);
            if (refusePreparedForTest) return new(SpacecraftCommandStatus.PreparationRefused);
            status = ValidateCommandAuthority(authority); if (status != SpacecraftCommandStatus.Accepted) return new(status);
            // Fixed bounded commit: all expected refusals precede the first write. No physics/clock/revision/history writes.
            if (changed) _commands!.State = after; // A no-op retains original bits, including signed zero.
            _commands!.Revision = revision; _commands!.LastConsumedSequence = pending.Sequence;
            if (changed) _commands!.LastTransitionEpoch = current;
            CaptureEngineTransition(pending, revision); // Optional bounded owner-private handoff; preflighted above.
            for (var i = 1; i < _commands!.Count; i++) _commands!.Pending[i - 1] = _commands!.Pending[i];
            _commands!.Pending[--_commands!.Count] = default;
            return new(changed ? SpacecraftCommandStatus.Committed : SpacecraftCommandStatus.NoChange, pending.Sequence, current, _commands!.Copy());
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    /// <summary>Freeze this boundary immediately before evaluating the following interval; copied demand never applies physics.</summary>
    internal SpacecraftCommandStatus CloseSpacecraftCommandBoundary(SpacecraftCommandAuthority authority, out RequestedControlDemand demand)
    {
        demand = default;
        var entered = EnterCommandPhase(); if (entered != SpacecraftCommandStatus.Accepted) return entered;
        try
        {
            var status = ValidateCommandAuthority(authority); if (status != SpacecraftCommandStatus.Accepted) return status;
            if (!authority.TryBoundaryAtOrAfter(_clock.CurrentTime, 0, out var index, out var epoch) || epoch != _clock.CurrentTime)
                return SpacecraftCommandStatus.NotAtBoundary;
            if (index <= _commands!.ClosedThrough) return SpacecraftCommandStatus.BoundaryClosed;
            if (_commands!.Count > 0 && _commands!.Pending[0].Epoch == epoch) return SpacecraftCommandStatus.Pending;
            if (HasContactProofBoundaryThrough(epoch)) return SpacecraftCommandStatus.PendingEvent;
            demand = _commands!.Copy().ConsumeWithoutActuation(); _commands!.ClosedThrough = index;
            return SpacecraftCommandStatus.BoundaryReady;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal SpacecraftCommandStatus ObserveSpacecraftCommands(SpacecraftCommandAuthority authority, out SpacecraftCommandObservation observation)
    {
        observation = default;
        if (!_clock.PublicationPhase.IsOwnerThread) return SpacecraftCommandStatus.WrongOwnerThread;
        if (authority is null || !ReferenceEquals(authority, _commands?.Authority)) return SpacecraftCommandStatus.InvalidAuthority;
        observation = _commands!.Copy(); return SpacecraftCommandStatus.Accepted;
    }

    // Narrow boundary-value test seam, no general setter/callback or scene-visible mutable command store.
    internal void SaturateCommandRevisionForTest(SpacecraftCommandAuthority authority)
    {
        var entered = EnterCommandPhase();
        if (entered != SpacecraftCommandStatus.Accepted) throw new InvalidOperationException("Command revision test seam requires owner phase.");
        try
        {
            if (ValidateCommandAuthority(authority) != SpacecraftCommandStatus.Accepted || _commands!.Count != 0)
                throw new InvalidOperationException("Command revision test seam requires an empty current authority.");
            _commands!.Revision = new(ulong.MaxValue);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
}
