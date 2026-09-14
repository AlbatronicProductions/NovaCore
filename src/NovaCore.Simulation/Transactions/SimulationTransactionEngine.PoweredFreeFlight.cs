using NovaCore.Core;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private sealed class PoweredFlightStorage(PoweredFlightAuthority authority, int capacity)
    {
        internal readonly PoweredFlightAuthority Authority = authority;
        internal readonly object Seal = new();
        internal readonly PoweredFlightRecord[] History = new PoweredFlightRecord[capacity];
        internal SpacecraftPhysicalSource ExpectedPhysical;
        internal SpacecraftDefinition ExpectedDefinition;
        internal PropellantSourceObservation ExpectedResource;
        internal ContinuationClockState ExpectedClock;
        internal StateRevision ExpectedRevision;
        internal TimelineRevision ExpectedTimeline;
        internal ActualEngineState Actual;
        internal long Frontier, Generation, HostSequence;
        internal int Count;
        internal bool Active, Invalidated;
        internal PropellantProposal ResourceLease;
        internal PoweredFlightRecord Prepared;
        internal PoweredFlightObservation Observation;
        internal PoweredFlightEpisode Episode;
    }
    private PoweredFlightStorage? _poweredFlight;

    private PoweredFlightStatus EnterPoweredPhase()
    {
        if (!_clock.PublicationPhase.IsOwnerThread) return PoweredFlightStatus.WrongOwnerThread;
        return _isExecutingGroup || !_clock.PublicationPhase.TryEnter(this) ? PoweredFlightStatus.Reentrant : PoweredFlightStatus.Ready;
    }
    private PoweredFlightStatus CheckPoweredSource(PoweredFlightAuthority? authority)
    {
        var p = _poweredFlight;
        if (authority is null || p is null || !ReferenceEquals(authority, p.Authority)) return PoweredFlightStatus.InvalidAuthority;
        if (p.Invalidated) return PoweredFlightStatus.Invalidated;
        var view = _state.CreateView();
        if (view.Revision != p.ExpectedRevision || _clock.Timeline.Revision != p.ExpectedTimeline ||
            CaptureContinuationClock() != p.ExpectedClock || !SupportedCommandClock ||
            !SpacecraftPhysicalSource.TryCapture(view.Spacecraft, authority.Commands.Spacecraft, out var physical) ||
            !physical.Same(p.ExpectedPhysical) ||
            !view.Spacecraft.TryGetDefinition(authority.Commands.Spacecraft, out var definition) || definition != p.ExpectedDefinition ||
            authority.Resource.Copy() != p.ExpectedResource || authority.Resource.Definition.Values != p.ExpectedResource.Definition)
            return PoweredFlightStatus.StaleSource;
        return PoweredFlightStatus.Ready;
    }

    internal PoweredFlightStatus BeginPoweredFreeFlight(PropellantResourceAuthority resource, int historyCapacity,
        out PoweredFlightAuthority? authority)
    {
        authority = null;
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try
        {
            if (_poweredFlight is not null || !OwnsPropellant(resource)) return PoweredFlightStatus.InvalidAuthority;
            var commands = resource.Engine.Commands;
            if (commands.LastIndex > 1200 || historyCapacity <= 0 || historyCapacity > 1200 ||
                _enginePreparation!.Active || _propellantPreparation!.Active || _enginePreparation.LastBoundary != -1 ||
                _clock.CurrentTime != commands.Origin || !SupportedCommandClock || _clock.PendingSimulationDebt.Ticks < 0)
                return PoweredFlightStatus.InvalidInput;
            var view = _state.CreateView();
            if (!SpacecraftPhysicalSource.TryCapture(view.Spacecraft, commands.Spacecraft, out var physical) || physical.IsEndpoint ||
                !view.Spacecraft.TryGetDefinition(commands.Spacecraft, out var definition) ||
                physical.Linear.ConstantForceRoot != Double3.Zero || physical.Angular.ConstantBodyTorque != Double3.Zero ||
                physical.Linear.Epoch != commands.Origin || physical.Angular.Epoch != commands.Origin ||
                !PropellantMassMatches(resource, out _) || resource.Definition.Values.DryMassKilograms != 8 ||
                resource.Definition.Values.DryInertia != new Spacecraft.Rotation.PrincipalMomentsOfInertia(2,2,2) ||
                !resource.Definition.InitialUnits.TryToKilograms(out var fuel) || fuel > 1d/128 ||
                !PoweredFlightEvaluator.InModel(new(physical.Linear.PositionRoot, physical.Linear.VelocityRoot,
                    physical.Angular.OrientationLocalToParent, physical.Angular.AngularVelocityBody))) return PoweredFlightStatus.OutsideModel;
            var engine = resource.Engine.Definition.Values;
            if (engine.MaximumThrustNewtons > 9000 || !PoweredFlightEvaluator.MagnitudeFits(
                Double3.Cross(engine.MountFromComMetres, engine.ThrustAxisBody * engine.MaximumThrustNewtons), 2))
                return PoweredFlightStatus.OutsideModel;
            authority = new(resource);
            var storage = new PoweredFlightStorage(authority, historyCapacity)
            {
                ExpectedPhysical = physical, ExpectedDefinition = definition, ExpectedResource = resource.Copy(),
                ExpectedClock = CaptureContinuationClock(), ExpectedRevision = view.Revision, ExpectedTimeline = _clock.Timeline.Revision
            };
            _state.PrepareAppliedEndpointStorage();
            storage.Observation = new(new(commands.Spacecraft, commands.RootFrame, commands.Origin,
                physical.Linear.PositionRoot, physical.Linear.VelocityRoot, physical.Angular.OrientationLocalToParent,
                physical.Angular.AngularVelocityBody, physical.Properties, physical.Inertia, AppliedEndpointValidity.EndpointOnly),
                storage.ExpectedResource, storage.Actual, storage.ExpectedRevision, storage.ExpectedTimeline, storage.ExpectedClock, 0);
            storage.Episode = new(1, definition, storage.Observation, commands.End, engine);
            _poweredFlight = storage;
            return PoweredFlightStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal PoweredFlightResult ObservePoweredFreeFlight(PoweredFlightAuthority authority)
    {
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return new(entered);
        try
        {
            if (_poweredFlight is not { } p || !ReferenceEquals(p.Authority, authority)) return new(PoweredFlightStatus.InvalidAuthority);
            // Copied canonical values remain observable after terminal private failure.
            return new(p.Invalidated ? PoweredFlightStatus.Invalidated : PoweredFlightStatus.Ready, p.Observation);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal bool TryGetPoweredFlightRecord(int index, out PoweredFlightRecord record)
    {
        _clock.PublicationPhase.VerifyRead(); record = default;
        if (_poweredFlight is not { } p || (uint)index >= (uint)p.Count) return false;
        record = p.History[index]; return true;
    }
    internal PoweredFlightEpisode CopyPoweredFlightEpisode()
    { _clock.PublicationPhase.VerifyRead(); return _poweredFlight?.Episode ?? default; }

    /// <summary>Explicit private cancellation, including a stale resource lease. Never rewinds the parent engine cursor.</summary>
    internal PoweredFlightStatus RetirePoweredFlightProposal(PoweredFlightAuthority authority, PoweredFlightProposal proposal)
    {
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try
        {
            if (_poweredFlight is not { } p || !ReferenceEquals(authority, p.Authority)) return PoweredFlightStatus.InvalidAuthority;
            if (p.Invalidated) return PoweredFlightStatus.Invalidated;
            if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Generation != p.Generation) return PoweredFlightStatus.InvalidProposal;
            p.Active = false; p.Prepared = default; p.ResourceLease = default;
            // M14.23 intentionally cannot rewind an explicitly retired interval cursor.
            if (!_enginePreparation!.Active) { p.Invalidated = true; return PoweredFlightStatus.Invalidated; }
            return PoweredFlightStatus.Retired;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal PoweredFlightResult AdmitPoweredHostTime(PoweredFlightAuthority authority, long sequence, SimulationDuration elapsed,
        bool failAcknowledgementForTest = false)
    {
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return new(entered);
        try
        {
            var status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return new(status);
            var p = _poweredFlight!;
            if (p.Frontier == authority.Commands.LastIndex) return new(PoweredFlightStatus.Completed, p.Observation);
            if (p.Active || _enginePreparation!.Active || _propellantPreparation!.Active) return new(PoweredFlightStatus.OutstandingProposal);
            if (p.HostSequence == long.MaxValue || sequence != p.HostSequence + 1) return new(PoweredFlightStatus.InvalidSequence);
            if (elapsed.Ticks < 0) return new(PoweredFlightStatus.InvalidInput);
            if (elapsed.Ticks == 0) return new(PoweredFlightStatus.NoWork, p.Observation);
            var prepared = _clock.PrepareHostAdvance(elapsed);
            if (prepared.Reason != SimulationHostAdvanceStopReason.Accepted) return new(PoweredFlightStatus.ArithmeticOverflow);
            var after = p.ExpectedClock with { Debt = prepared.DebtAfter, RateRemainder = prepared.RateRemainderAfter };
            // M14.22 requires a representable funded horizon as well as representable debt.
            if ((Int128)after.Time.Ticks + after.Debt.Ticks > long.MaxValue) return new(PoweredFlightStatus.ArithmeticOverflow);
            status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return new(status);
            _clock.InstallHostAdvance(prepared);
            p.Observation = p.Observation with { Clock = after };
            if (failAcknowledgementForTest)
            { p.Invalidated = true; return new(PoweredFlightStatus.CanonicalCommittedPrivateInvalidated, p.Observation); }
            p.ExpectedClock = after; p.HostSequence = sequence;
            p.Observation = p.Observation with { Clock = after };
            return new(PoweredFlightStatus.AcceptedCredit, p.Observation);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal PoweredFlightStatus PreparePoweredFlight(PoweredFlightAuthority authority, PropellantProposal resource,
        out PoweredFlightProposal proposal, bool refuseForTest = false)
    {
        proposal = default;
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try { return PreparePoweredFlightInOwnedPhase(authority, resource, out proposal, refuseForTest); }
        finally { _clock.PublicationPhase.Exit(); }
    }
    private PoweredFlightStatus PreparePoweredFlightInOwnedPhase(PoweredFlightAuthority authority, PropellantProposal resource,
        out PoweredFlightProposal proposal, bool refuseForTest)
    {
        proposal = default;
        var status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return status;
        var p = _poweredFlight!;
        if (p.Active) return PoweredFlightStatus.OutstandingProposal;
        if (p.Generation == long.MaxValue) return PoweredFlightStatus.ArithmeticOverflow;
        if (ReadFinitePropellantInOwnedPhase(authority.Resource, resource, out var segmentation) != PropellantPreparationStatus.Preview)
            return PoweredFlightStatus.InvalidProposal;
        if (segmentation.Engine.BoundaryIndex != p.Frontier || segmentation.Engine.Start != p.ExpectedClock.Time)
            return PoweredFlightStatus.StaleSource;
        var source = p.ExpectedPhysical;
        var kinematics = source.IsEndpoint ? new PoweredKinematics(source.Endpoint.PositionRoot, source.Endpoint.VelocityRoot,
            source.Endpoint.BodyToRoot, source.Endpoint.AngularVelocityBody) : new(source.Linear.PositionRoot, source.Linear.VelocityRoot,
            source.Angular.OrientationLocalToParent, source.Angular.AngularVelocityBody);
        var evaluated = PoweredFlightEvaluator.Evaluate(kinematics, segmentation, out var value, out _);
        if (evaluated != PoweredEvaluationStatus.Success)
            return evaluated == PoweredEvaluationStatus.OutsideModel ? PoweredFlightStatus.OutsideModel : PoweredFlightStatus.NumericalFailure;
        if (p.ExpectedRevision.Value == ulong.MaxValue || p.Actual.ActuatorRevision == ulong.MaxValue ||
            (!segmentation.ConsumedUnits.IsZero && p.ExpectedResource.ResourceRevision == ulong.MaxValue)) return PoweredFlightStatus.RevisionOverflow;
        var endpoint = new SpacecraftAppliedEndpoint(authority.Commands.Spacecraft, authority.Commands.RootFrame, segmentation.Engine.End,
            value.Position, value.Velocity, value.Orientation, value.AngularVelocity, new(segmentation.ProposedSuccessorMass.TotalMassKilograms),
            segmentation.ProposedSuccessorMass.Inertia, AppliedEndpointValidity.EndpointOnly);
        var latch = segmentation.Engine.ProposedLatch;
        var activity = latch == ProposedEngineLatch.Off ? ActualEngineActivity.Off : segmentation.SuccessorUnits.IsZero ?
            ActualEngineActivity.EnabledNoFeed : segmentation.Engine.ProposedThrustNewtons > 0 ? ActualEngineActivity.ProducingOutput : ActualEngineActivity.EnabledIdle;
        var actuator = new ActualEngineState(activity, latch, activity == ActualEngineActivity.ProducingOutput ? segmentation.Engine.ProposedRealizedThrottle : 0,
            segmentation.PoweredDuration, p.Frontier + 1, p.Actual.ActuatorRevision + 1);
        var record = new PoweredFlightRecord(1, p.Frontier, segmentation, endpoint, actuator, p.ExpectedRevision,
            new(p.ExpectedRevision.Value + 1), p.ExpectedResource.ResourceRevision + (segmentation.ConsumedUnits.IsZero ? 0UL : 1UL), p.ExpectedTimeline);
        if (refuseForTest) return PoweredFlightStatus.PreparationRefused;
        status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return status;
        if (ReadFinitePropellantInOwnedPhase(authority.Resource, resource, out _) != PropellantPreparationStatus.Preview)
            return PoweredFlightStatus.InvalidProposal;
        p.Prepared = record; p.ResourceLease = resource; p.Generation++; p.Active = true;
        proposal = new(p.Generation, p.Seal);
        return PoweredFlightStatus.Prepared;
    }

    internal PoweredFlightResult PublishPoweredFlight(PoweredFlightAuthority authority, PoweredFlightProposal proposal,
        bool failAcknowledgementForTest = false)
    {
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return new(entered);
        try { return PublishPoweredFlightInOwnedPhase(authority, proposal, failAcknowledgementForTest); }
        finally { _clock.PublicationPhase.Exit(); }
    }
    private PoweredFlightResult PublishPoweredFlightInOwnedPhase(PoweredFlightAuthority authority, PoweredFlightProposal proposal, bool failAcknowledgementForTest)
    {
        var status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return new(status);
        var p = _poweredFlight!;
        if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Generation != p.Generation) return new(PoweredFlightStatus.InvalidProposal);
        if (ReadFinitePropellantInOwnedPhase(authority.Resource, p.ResourceLease, out var resource) != PropellantPreparationStatus.Preview)
            return new(PoweredFlightStatus.InvalidProposal);
        var record = p.Prepared; var endpoint = record.Endpoint;
        if (resource != record.Segmentation || endpoint.Validity != AppliedEndpointValidity.EndpointOnly) return new(PoweredFlightStatus.StaleSource);
        if (HasContactProofBoundaryThrough(endpoint.Epoch)) return new(PoweredFlightStatus.PendingEvent);
        var ticks = (Int128)endpoint.Epoch.Ticks - p.ExpectedClock.Time.Ticks;
        if (ticks <= 0 || ticks > long.MaxValue || p.ExpectedClock.Debt.Ticks < 0) return new(PoweredFlightStatus.ArithmeticOverflow);
        if (p.ExpectedClock.Debt.Ticks < ticks) return new(PoweredFlightStatus.AwaitingDebt);
        if (p.Count >= p.History.Length) return new(PoweredFlightStatus.HistoryCapacity);
        if (!_state.TryPrepareAppliedSlot(authority.Commands.Spacecraft, p.ExpectedPhysical, out var slot)) return new(PoweredFlightStatus.StaleSource);
        var clock = p.ExpectedClock with { Time = endpoint.Epoch, Debt = new(p.ExpectedClock.Debt.Ticks - (long)ticks) };
        record = record with { DebtBefore = p.ExpectedClock.Debt, DebtAfter = clock.Debt };
        var successorResource = p.ExpectedResource with { RemainingUnits = resource.SuccessorUnits, ResourceRevision = record.ResourceRevision };
        var successorPhysical = new SpacecraftPhysicalSource(true, endpoint, default, default, endpoint.Properties);
        var observation = new PoweredFlightObservation(endpoint, successorResource, record.Actuator, record.StateRevision, record.TimelineRevision, clock, p.Count + 1);
        // Final recheck; no callback or release of the transaction owner before fixed writes.
        status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return new(status);
        if (ReadFinitePropellantInOwnedPhase(authority.Resource, p.ResourceLease, out _) != PropellantPreparationStatus.Preview ||
            !_state.TryPrepareAppliedSlot(authority.Commands.Spacecraft, p.ExpectedPhysical, out var again) || again != slot)
            return new(PoweredFlightStatus.StaleSource);

        // ONE CANONICAL TRANSACTION: fixed prevalidated slots, no numerical work, allocation or normal refusal.
        _state.InstallAppliedEndpoint(slot, endpoint, record.StateRevision);
        _propellantPreparation!.Canonical = successorResource;
        p.Actual = record.Actuator;
        _clock.InstallCertifiedContinuation(clock.Time, clock.Debt);
        p.History[p.Count] = record; p.Count++;
        p.Observation = observation;
        // Canonical success consumes every execution lease, including the terminal acknowledgement path.
        _enginePreparation!.Active = false; _enginePreparation.Preview = default;
        _propellantPreparation.Active = false; _propellantPreparation.Parent = default; _propellantPreparation.Preview = default;
        p.Active = false; p.ResourceLease = default; p.Prepared = default;
        try
        {
            if (failAcknowledgementForTest) { p.Invalidated = true; return new(PoweredFlightStatus.CanonicalCommittedPrivateInvalidated, observation, 1); }
            p.ExpectedPhysical = successorPhysical; p.ExpectedResource = successorResource;
            p.ExpectedClock = clock; p.ExpectedRevision = record.StateRevision; p.Frontier = record.Actuator.Frontier;
        }
        catch (Exception) { p.Invalidated = true; return new(PoweredFlightStatus.CanonicalCommittedPrivateInvalidated, observation, 1); }
        return new(PoweredFlightStatus.Published, observation, 1);
    }

    /// <summary>Bounded drain. Each interval uses the just-committed state; host credit is a separate once-only operation.</summary>
    internal PoweredFlightResult ServicePoweredFlightDebt(PoweredFlightAuthority authority)
    {
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return new(entered);
        try
        {
            var count = 0;
            while (true)
            {
                var status = CheckPoweredSource(authority); if (status != PoweredFlightStatus.Ready) return new(status, PublishedCount: count);
                var p = _poweredFlight!;
                if (p.Frontier == authority.Commands.LastIndex) return new(PoweredFlightStatus.Completed, p.Observation, count);
                if (p.Active || _enginePreparation!.Active || _propellantPreparation!.Active) return new(PoweredFlightStatus.OutstandingProposal, p.Observation, count);
                if (!authority.Commands.TryBoundaryAtOrAfter(p.ExpectedClock.Time, p.Frontier + 1, out _, out var target))
                    return new(PoweredFlightStatus.Completed, p.Observation, count);
                if (HasContactProofBoundaryThrough(target)) return new(PoweredFlightStatus.PendingEvent, p.Observation, count);
                if (p.Count >= p.History.Length) return new(PoweredFlightStatus.HistoryCapacity, p.Observation, count);
                if (p.ExpectedClock.Debt.Ticks < (Int128)target.Ticks - p.ExpectedClock.Time.Ticks)
                    return new(PoweredFlightStatus.AwaitingDebt, p.Observation, count);
                if (count == 4) return new(PoweredFlightStatus.BudgetExhausted, p.Observation, count);
                if (_commands!.ClosedThrough < p.Frontier)
                {
                    for (var i = 0; i <= SpacecraftCommandAuthority.OrdinaryCapacity + 1; i++)
                    {
                        var command = CommitNextSpacecraftCommandInOwnedPhase(authority.Commands, false);
                        if (command.Status is SpacecraftCommandStatus.NoCommand or SpacecraftCommandStatus.Pending) break;
                        if (command.Status is not (SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange))
                            return new(PoweredFlightStatus.CommandBlocked, p.Observation, count);
                    }
                    if (CloseSpacecraftCommandBoundaryInOwnedPhase(authority.Commands, out _) != SpacecraftCommandStatus.BoundaryReady)
                        return new(PoweredFlightStatus.CommandBlocked, p.Observation, count);
                }
                if (PrepareSingleEngineActuationInOwnedPhase(authority.Resource.Engine, target, false, out var engine) != EnginePreparationStatus.Prepared)
                    return new(PoweredFlightStatus.EngineBlocked, p.Observation, count);
                if (PrepareFinitePropellantInOwnedPhase(authority.Resource, engine, target, false, out var resource) != PropellantPreparationStatus.Prepared)
                    return new(PoweredFlightStatus.ResourceBlocked, p.Observation, count);
                status = PreparePoweredFlightInOwnedPhase(authority, resource, out var proposal, false);
                if (status != PoweredFlightStatus.Prepared) return new(status, p.Observation, count);
                var published = PublishPoweredFlightInOwnedPhase(authority, proposal, false);
                count += published.PublishedCount;
                if (published.Status != PoweredFlightStatus.Published) return published with { PublishedCount = count };
            }
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
}
