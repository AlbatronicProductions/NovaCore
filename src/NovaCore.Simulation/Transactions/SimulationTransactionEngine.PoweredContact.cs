using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    internal PoweredFlightStatus BeginPoweredContact(PropellantResourceAuthority resource, PoweredContactPreparation prepared,
        int historyCapacity, out PoweredFlightAuthority? authority, out LocalContactWorld? world)
    {
        authority = null; world = null;
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try
        {
            if (_poweredFlight is not null || !OwnsPropellant(resource) || prepared is null || !prepared.Available ||
                !ReferenceEquals(resource.Definition, prepared.ResourceDefinition) ||
                !ReferenceEquals(resource.Engine.Definition, prepared.EngineDefinition)) return PoweredFlightStatus.InvalidAuthority;
            var commands = resource.Engine.Commands;
            if (commands.LastIndex > 1200 || historyCapacity <= 0 || historyCapacity > 1200 ||
                _enginePreparation!.Active || _propellantPreparation!.Active || _enginePreparation.LastBoundary != -1 ||
                _clock.CurrentTime != commands.Origin || !SupportedCommandClock || _clock.PendingSimulationDebt.Ticks < 0)
                return PoweredFlightStatus.InvalidInput;
            if (LocalContactSource.CapturePoweredPreparation(this, commands.Spacecraft, prepared, commands.End, out var source)
                != LocalContactStatus.Success) return PoweredFlightStatus.StaleSource;
            var view = _state.CreateView();
            if (!SpacecraftPhysicalSource.TryCapture(view.Spacecraft, commands.Spacecraft, out var physical) ||
                !view.Spacecraft.TryGetDefinition(commands.Spacecraft, out var definition) || !PropellantMassMatches(resource, out _))
                return PoweredFlightStatus.StaleSource;
            authority = new(resource, PoweredPhysicalConsumer.RetainedContact);
            var storage = new PoweredFlightStorage(authority, historyCapacity)
            {
                ExpectedPhysical = physical, ExpectedDefinition = definition, ExpectedResource = resource.Copy(),
                ExpectedClock = CaptureContinuationClock(), ExpectedRevision = view.Revision, ExpectedTimeline = _clock.Timeline.Revision,
                ContactConfiguration = prepared.Configuration
            };
            _state.PrepareAppliedEndpointStorage();
            storage.Observation = new(new(commands.Spacecraft, commands.RootFrame, commands.Origin,
                physical.Linear.PositionRoot, physical.Linear.VelocityRoot, physical.Angular.OrientationLocalToParent,
                physical.Angular.AngularVelocityBody, physical.Properties, physical.Inertia, AppliedEndpointValidity.EndpointOnly),
                storage.ExpectedResource, default, storage.ExpectedRevision, storage.ExpectedTimeline, storage.ExpectedClock, 0);
            storage.Episode = new(2, definition, storage.Observation, commands.End, prepared.EngineDefinition.Values,
                PoweredPhysicalConsumer.RetainedContact, (int)prepared.Fixture);
            world = LocalContactWorld.BindPoweredPreparation(source!, prepared, this, authority, physical, definition,
                storage.Observation, out storage.ContactReceipt);
            storage.Contact = world;
            _poweredFlight = storage;
            return PoweredFlightStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }

    internal PoweredFlightResult ObservePoweredContact(PoweredFlightAuthority authority)
        => authority is { Consumer: PoweredPhysicalConsumer.RetainedContact } ? ObservePowered(authority) : new(PoweredFlightStatus.InvalidAuthority);
    internal PoweredFlightResult AdmitPoweredContactHostTime(PoweredFlightAuthority authority, long sequence, SimulationDuration elapsed,
        bool failAcknowledgementForTest = false)
        => authority is { Consumer: PoweredPhysicalConsumer.RetainedContact } ? AdmitPoweredTime(authority, sequence, elapsed, failAcknowledgementForTest) : new(PoweredFlightStatus.InvalidAuthority);
    internal PoweredFlightResult ServicePoweredContactDebt(PoweredFlightAuthority authority)
        => authority is { Consumer: PoweredPhysicalConsumer.RetainedContact } ? ServicePoweredDebt(authority) : new(PoweredFlightStatus.InvalidAuthority);

    internal PoweredFlightStatus PreparePoweredContact(PoweredFlightAuthority authority, PropellantProposal resource,
        out PoweredFlightProposal proposal, bool refuseForTest = false)
    {
        proposal = default;
        if (authority is not { Consumer: PoweredPhysicalConsumer.RetainedContact }) return PoweredFlightStatus.InvalidAuthority;
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try { return PreparePoweredFlightInOwnedPhase(authority, resource, out proposal, refuseForTest); }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal PoweredFlightResult PublishPoweredContact(PoweredFlightAuthority authority, PoweredFlightProposal proposal,
        bool failAcknowledgementForTest = false)
    {
        if (authority is not { Consumer: PoweredPhysicalConsumer.RetainedContact }) return new(PoweredFlightStatus.InvalidAuthority);
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return new(entered);
        try { return PublishPoweredFlightInOwnedPhase(authority, proposal, failAcknowledgementForTest); }
        finally { _clock.PublicationPhase.Exit(); }
    }

    private PoweredFlightStatus PreparePoweredContactPhysical(PoweredFlightStorage p,
        in PropellantSegmentationPreview segmentation, out PoweredKinematics value)
    {
        value = default;
        if (p.Contact is not { } world || p.ContactConfiguration is not { } config) return PoweredFlightStatus.InvalidAuthority;
        if (p.ExpectedRevision.Value == ulong.MaxValue || p.Actual.ActuatorRevision == ulong.MaxValue ||
            (!segmentation.ConsumedUnits.IsZero && p.ExpectedResource.ResourceRevision == ulong.MaxValue)) return PoweredFlightStatus.RevisionOverflow;
        if (p.Count >= p.History.Length) return PoweredFlightStatus.HistoryCapacity;
        var ticks = (Int128)segmentation.Engine.End.Ticks - p.ExpectedClock.Time.Ticks;
        if (ticks <= 0 || ticks > long.MaxValue || p.ExpectedClock.Debt.Ticks < 0) return PoweredFlightStatus.ArithmeticOverflow;
        if (p.ExpectedClock.Debt.Ticks < ticks) return PoweredFlightStatus.AwaitingDebt;
        if (HasContactProofBoundaryThrough(segmentation.Engine.End)) return PoweredFlightStatus.PendingEvent;
        var ready = world.PreparePoweredInput(this, p.Authority, p.ContactReceipt, segmentation, out var input);
        if (ready != PoweredFlightStatus.Ready) return ready;
        try
        {
            world.InstallPoweredInput(input);
            var stepped = world.Step(this, config, p.ContactReceipt, segmentation.Engine.End, out var receipt);
            if (stepped != LocalContactStatus.Success) return InvalidatePoweredContact(p, PoweredFlightStatus.NumericalFailure);
            p.ContactReceipt = receipt;
            if (world.ReadPoweredEndpoint(this, p.Authority, receipt, out var endpoint) != PoweredFlightStatus.Prepared)
                return InvalidatePoweredContact(p, PoweredFlightStatus.InvalidProposal);
            // Explicitly supported-only. No same-interval free-flight fallback or resource debit.
            if (endpoint.ContactPoints == 0 || endpoint.ConstraintCount != 1 || world.PoweredPenetration() > .001)
                return InvalidatePoweredContact(p, PoweredFlightStatus.OutsideModel);
            value = new(endpoint.Motion.PositionRoot, endpoint.Motion.VelocityRoot, endpoint.Motion.BodyToRoot, endpoint.Motion.AngularVelocityBody);
            return PoweredFlightStatus.Prepared;
        }
        catch (Exception) { return InvalidatePoweredContact(p, PoweredFlightStatus.NumericalFailure); }
    }

    private static PoweredFlightStatus InvalidatePoweredContact(PoweredFlightStorage p, PoweredFlightStatus result)
    {
        p.Invalidated = true; p.Contact?.InvalidatePoweredContinuation(); return result;
    }

    internal PoweredFlightStatus RetirePoweredContactProposal(PoweredFlightAuthority authority, PoweredFlightProposal proposal)
    {
        if (authority is not { Consumer: PoweredPhysicalConsumer.RetainedContact }) return PoweredFlightStatus.InvalidAuthority;
        var entered = EnterPoweredPhase(); if (entered != PoweredFlightStatus.Ready) return entered;
        try
        {
            var p = _poweredFlight;
            if (p is null || !ReferenceEquals(p.Authority, authority)) return PoweredFlightStatus.InvalidAuthority;
            if (p.Invalidated) return PoweredFlightStatus.Invalidated;
            if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Generation != p.Generation) return PoweredFlightStatus.InvalidProposal;
            p.Active = false; p.Prepared = default; p.ResourceLease = default;
            _enginePreparation!.Active = false; _enginePreparation.Preview = default;
            _propellantPreparation!.Active = false; _propellantPreparation.Parent = default; _propellantPreparation.Preview = default;
            // A native step cannot be rolled back or reevaluated like the pure free-flight consumer.
            return InvalidatePoweredContact(p, PoweredFlightStatus.Retired);
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
}
