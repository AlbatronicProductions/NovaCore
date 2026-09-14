using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private sealed class PropellantPreparationStorage(PropellantResourceAuthority authority)
    {
        internal readonly PropellantResourceAuthority Authority = authority;
        internal readonly object Seal = new();
        internal ulong Generation;
        internal bool Active;
        internal EngineActuationProposal Parent;
        internal PropellantSegmentationPreview Preview;
        internal PropellantSourceObservation Canonical = new(authority.Definition.Values, authority.Definition.InitialUnits, authority.Definition.InitialUnits, 0);
    }
    private PropellantPreparationStorage? _propellantPreparation;
    internal PropellantSourceObservation CopyFinitePropellantSource(PropellantResourceAuthority authority)
    {
        _clock.PublicationPhase.VerifyRead();
        if (!OwnsPropellant(authority)) throw new InvalidOperationException("Resource capability does not belong to this canonical owner.");
        return _propellantPreparation!.Canonical;
    }

    private PropellantPreparationStatus EnterPropellantPhase()
    {
        if (!_clock.PublicationPhase.IsOwnerThread) return PropellantPreparationStatus.WrongOwnerThread;
        return _isExecutingGroup || !_clock.PublicationPhase.TryEnter(this)
            ? PropellantPreparationStatus.ReentrantOperation : PropellantPreparationStatus.Ready;
    }
    private bool OwnsPropellant(PropellantResourceAuthority? authority) =>
        authority is not null && ReferenceEquals(authority, _propellantPreparation?.Authority);
    private bool PropellantMassMatches(PropellantResourceAuthority authority, out double total)
    {
        total = 0;
        var definition = authority.Definition;
        if (definition.Values != _propellantPreparation!.Canonical.Definition) return false;
        var view = _state.CreateView();
        return PropellantInteger.TryAdd(definition.DryMassUnits, authority.RemainingUnits, out var sum) &&
            sum.TryToKilograms(out total) &&
            SpacecraftPhysicalSource.TryCapture(view.Spacecraft, authority.Engine.Commands.Spacecraft, out var physical) &&
            BitConverter.DoubleToInt64Bits(physical.Properties.MassKilograms) == BitConverter.DoubleToInt64Bits(total) &&
            physical.Inertia == definition.Values.DryInertia;
    }
    /// <summary>Cold source construction only, before command consumption/closure. Does not alter physical mass.</summary>
    internal PropellantPreparationStatus BindFinitePropellant(EnginePreparationAuthority engine,
        PropellantDefinition? definition, out PropellantResourceAuthority? authority)
    {
        authority = null;
        var entered = EnterPropellantPhase(); if (entered != PropellantPreparationStatus.Ready) return entered;
        try
        {
            if (_propellantPreparation is not null) return PropellantPreparationStatus.AlreadyBound;
            if (!OwnsEnginePreparation(engine)) return PropellantPreparationStatus.InvalidAuthority;
            if (definition is null || definition.Values.EngineId != engine.Definition.Values.EngineId ||
                definition.Values.EngineVersion != engine.Definition.Values.Version) return PropellantPreparationStatus.InvalidDefinition;
            if (_commands!.LastConsumedSequence != 0 || _commands.ClosedThrough != -1 ||
                _clock.CurrentTime != engine.Commands.Origin) return PropellantPreparationStatus.LateBinding;
            if (ValidateCommandAuthority(engine.Commands) != SpacecraftCommandStatus.Accepted)
                return PropellantPreparationStatus.StaleSource;
            // All refusal checks precede retained source/slot installation.
            var view = _state.CreateView();
            if (!view.Spacecraft.TryGetTranslation(engine.Commands.Spacecraft, out _, out var mass) ||
                BitConverter.DoubleToInt64Bits(mass.MassKilograms) != BitConverter.DoubleToInt64Bits(definition.InitialTotalMassKilograms) ||
                !view.Spacecraft.TryGetRigidBody(engine.Commands.Spacecraft, out var angular) ||
                angular.PrincipalInertia != definition.Values.DryInertia) return PropellantPreparationStatus.InvalidDefinition;
            authority = new(this, engine, definition);
            _propellantPreparation = new(authority);
            return PropellantPreparationStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal PropellantPreparationStatus ObserveFinitePropellant(PropellantResourceAuthority authority,
        out PropellantSourceObservation source, out PropellantPreparationProgress progress)
    {
        source = default; progress = default;
        var entered = EnterPropellantPhase(); if (entered != PropellantPreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsPropellant(authority)) return PropellantPreparationStatus.InvalidAuthority;
            source = authority.Copy();
            progress = new(_propellantPreparation!.Generation, _propellantPreparation.Active);
            return PropellantPreparationStatus.Ready;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
    internal PropellantPreparationStatus PrepareFinitePropellant(PropellantResourceAuthority authority,
        EngineActuationProposal parent, SimulationInstant target, out PropellantProposal proposal) =>
        PrepareFinitePropellantCore(authority, parent, target, false, out proposal);
    internal PropellantPreparationStatus RefuseFinitePropellantForTest(PropellantResourceAuthority authority,
        EngineActuationProposal parent, SimulationInstant target, out PropellantProposal proposal) =>
        PrepareFinitePropellantCore(authority, parent, target, true, out proposal);

    private PropellantPreparationStatus PrepareFinitePropellantCore(PropellantResourceAuthority authority,
        EngineActuationProposal parent, SimulationInstant target, bool refusePreparedForTest, out PropellantProposal proposal)
    {
        proposal = default;
        var entered = EnterPropellantPhase(); if (entered != PropellantPreparationStatus.Ready) return entered;
        try { return PrepareFinitePropellantInOwnedPhase(authority, parent, target, refusePreparedForTest, out proposal); }
        finally { _clock.PublicationPhase.Exit(); }
    }

    private PropellantPreparationStatus PrepareFinitePropellantInOwnedPhase(PropellantResourceAuthority authority, EngineActuationProposal parent, SimulationInstant target, bool refusePreparedForTest, out PropellantProposal proposal)
    {
        proposal = default;
        if (!_clock.PublicationPhase.IsOwnedBy(this)) { proposal = default; return PropellantPreparationStatus.ReentrantOperation; }
        if (!OwnsPropellant(authority)) return PropellantPreparationStatus.InvalidAuthority;
        var p = _propellantPreparation!;
        if (p.Active) return PropellantPreparationStatus.OutstandingProposal;
        var parentStatus = ReadSingleEngineActuationInOwnedPhase(authority.Engine, parent, out var engine);
        if (parentStatus != EnginePreparationStatus.Preview)
            return parentStatus == EnginePreparationStatus.StaleSource ?
                PropellantPreparationStatus.StaleSource : PropellantPreparationStatus.InvalidEngineProposal;
        if (target != engine.End || target.Ticks <= engine.Start.Ticks)
            return PropellantPreparationStatus.InvalidInterval;
        // Engine intervals are ordered nonnegative offsets on its checked canonical schedule.
        var ticks128 = (Int128)target.Ticks - engine.Start.Ticks;
        if (ticks128 > long.MaxValue) return PropellantPreparationStatus.InvalidInterval;
        if (p.Generation == ulong.MaxValue) return PropellantPreparationStatus.GenerationExhausted;
        if (!PropellantMassMatches(authority, out var sourceMass)) return PropellantPreparationStatus.StaleSource;
        var source = authority.Copy();
        var physicalDemand = engine.ProposedThrustNewtons != 0 || engine.ProposedForceBodyNewtons != default ||
            engine.ProposedMomentBodyNewtonMetres != default;
        var calculated = PropellantSegmentation.Calculate(source.RemainingUnits, engine.RequiredMassFlowKilogramsPerSecond,
            physicalDemand, (long)ticks128, out var classification, out var flow, out var required,
            out var consumed, out var successor, out var powered, out var unpowered);
        if (calculated != PropellantPreparationStatus.Ready) return calculated;
        if (!PropellantInteger.TryAdd(authority.Definition.DryMassUnits, successor, out var successorTotal) ||
            !successorTotal.TryToKilograms(out var successorMass) || successorMass <= 0 ||
            !powered.IsWithin((long)ticks128) || !unpowered.IsWithin((long)ticks128))
            return PropellantPreparationStatus.ArithmeticFailure;
        var preview = new PropellantSegmentationPreview(PropellantPreviewMeaning.ProposedIfApplied, source, engine,
            flow, required, consumed, successor, classification, powered, unpowered,
            new(sourceMass, source.Definition.DryInertia), new(successorMass, source.Definition.DryInertia));
        if (refusePreparedForTest) return PropellantPreparationStatus.PreparationRefused;
        if (ReadSingleEngineActuationInOwnedPhase(authority.Engine, parent, out _) != EnginePreparationStatus.Preview ||
            authority.Copy() != source || !PropellantMassMatches(authority, out var recheckedMass) ||
            recheckedMass != sourceMass) return PropellantPreparationStatus.StaleSource;
        // Fixed private seal writes only. Canonical resource, mass, revisions and engine cursor never change.
        p.Generation++; p.Parent = parent; p.Preview = preview; p.Active = true;
        proposal = new(p.Generation, p.Seal);
        return PropellantPreparationStatus.Prepared;
    }

    internal PropellantPreparationStatus PreviewFinitePropellant(PropellantResourceAuthority authority,
        PropellantProposal proposal, out PropellantSegmentationPreview preview)
    {
        preview = default;
        var entered = EnterPropellantPhase(); if (entered != PropellantPreparationStatus.Ready) return entered;
        try { return ReadFinitePropellantInOwnedPhase(authority, proposal, out preview); }
        finally { _clock.PublicationPhase.Exit(); }
    }
    private PropellantPreparationStatus ReadFinitePropellantInOwnedPhase(PropellantResourceAuthority authority,
        PropellantProposal proposal, out PropellantSegmentationPreview preview)
    {
        preview = default;
        if (!_clock.PublicationPhase.IsOwnedBy(this)) return PropellantPreparationStatus.ReentrantOperation;
        if (!OwnsPropellant(authority)) return PropellantPreparationStatus.InvalidAuthority;
        var p = _propellantPreparation!;
        if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Generation != p.Generation)
            return PropellantPreparationStatus.InvalidProposal;
        if (ReadSingleEngineActuationInOwnedPhase(authority.Engine, p.Parent, out _) != EnginePreparationStatus.Preview ||
            authority.Copy() != p.Preview.Resource || !PropellantMassMatches(authority, out _))
            return PropellantPreparationStatus.StaleSource;
        preview = p.Preview; return PropellantPreparationStatus.Preview;
    }
    /// <summary>Releases only the resource lease, including stale parents. Reissue is a new preview, never spending/replay.</summary>
    internal PropellantPreparationStatus RetireFinitePropellant(PropellantResourceAuthority authority, PropellantProposal proposal)
    {
        var entered = EnterPropellantPhase(); if (entered != PropellantPreparationStatus.Ready) return entered;
        try
        {
            if (!OwnsPropellant(authority)) return PropellantPreparationStatus.InvalidAuthority;
            var p = _propellantPreparation!;
            if (!p.Active || !proposal.IsIssuedBy(p.Seal) || proposal.Generation != p.Generation)
                return PropellantPreparationStatus.InvalidProposal;
            p.Active = false; p.Parent = default; p.Preview = default;
            return PropellantPreparationStatus.Retired;
        }
        finally { _clock.PublicationPhase.Exit(); }
    }
}
