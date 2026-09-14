using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Resources;

internal enum PropellantPreparationStatus
{
    InvalidInput, Ready, Prepared, Preview, Retired, InvalidDefinition, InvalidAuthority,
    AlreadyBound, LateBinding, WrongOwnerThread, ReentrantOperation, InvalidEngineProposal,
    StaleSource, InvalidInterval, IncompatibleFlow, ArithmeticFailure, OutstandingProposal,
    InvalidProposal, PreparationRefused, GenerationExhausted
}
internal enum PropellantClassification { Invalid, NoDemand, NoFeed, FullPowered, EndpointExhaustion, InteriorExhaustion }
internal enum PropellantPreviewMeaning { Invalid, ProposedIfApplied }
internal enum PropellantMassLaw { Invalid, CentralPointReservoirV1 }

internal readonly record struct PropellantDefinitionValues(ulong ResourceId, uint Version, ulong FeedId,
    ulong EngineId, uint EngineVersion, ulong DryBodyId, uint DryBodyVersion,
    PropellantMassLaw MassLaw, double DryMassKilograms, PrincipalMomentsOfInertia DryInertia);

/// <summary>One declared ideal point reservoir at dry COM. No implicit article wet/dry reinterpretation.</summary>
internal sealed class PropellantDefinition
{
    internal readonly PropellantDefinitionValues Values;
    internal readonly PropellantInteger InitialUnits, DryMassUnits;
    internal readonly double InitialTotalMassKilograms;
    private PropellantDefinition(PropellantDefinitionValues values, in PropellantInteger initial,
        in PropellantInteger dry, double total)
    { Values = values; InitialUnits = initial; DryMassUnits = dry; InitialTotalMassKilograms = total; }

    internal static PropellantPreparationStatus TryCreate(PropellantDefinitionValues values, double initialKilograms,
        out PropellantDefinition? definition)
    {
        definition = null;
        if (values.ResourceId == 0 || values.Version == 0 || values.FeedId == 0 || values.EngineId == 0 ||
            values.EngineVersion == 0 || values.DryBodyId == 0 || values.DryBodyVersion == 0 ||
            values.MassLaw != PropellantMassLaw.CentralPointReservoirV1 ||
            !double.IsFinite(values.DryMassKilograms) || values.DryMassKilograms <= 0 ||
            !values.DryInertia.IsFinite || !values.DryInertia.IsStrictlyPositive ||
            !PropellantInteger.TryFromKilograms(initialKilograms, out var initial) ||
            !PropellantInteger.TryFromKilograms(values.DryMassKilograms, out var dry) ||
            !PropellantInteger.TryAdd(initial, dry, out var sum) || !sum.TryToKilograms(out var total) || total <= 0)
            return PropellantPreparationStatus.InvalidDefinition;
        definition = new(values, initial, dry, total);
        return PropellantPreparationStatus.Ready;
    }
}
/// <summary>Immutable capability. Canonical resource storage and its only writer belong to the transaction engine.</summary>
internal sealed class PropellantResourceAuthority
{
    internal readonly EnginePreparationAuthority Engine;
    internal readonly PropellantDefinition Definition;
    private readonly SimulationTransactionEngine _owner;
    internal PropellantInteger RemainingUnits => Copy().RemainingUnits;
    internal ulong ResourceRevision => Copy().ResourceRevision;
    internal PropellantResourceAuthority(SimulationTransactionEngine owner, EnginePreparationAuthority engine, PropellantDefinition definition)
    { _owner = owner; Engine = engine; Definition = definition; }
    internal PropellantSourceObservation Copy() => _owner.CopyFinitePropellantSource(this);
}
internal readonly record struct PropellantSourceObservation(PropellantDefinitionValues Definition,
    PropellantInteger InitialUnits, PropellantInteger RemainingUnits, ulong ResourceRevision);
internal readonly record struct PropellantPreparationProgress(ulong LeaseGeneration, bool HasProposal);

/// <summary>Private lease; neither a copied observation nor an exhaustion value can forge it.</summary>
internal readonly struct PropellantProposal
{
    private readonly object? _seal;
    internal readonly ulong Generation;
    internal PropellantProposal(ulong generation, object? seal = null) { Generation = generation; _seal = seal; }
    internal bool IsIssuedBy(object seal) => ReferenceEquals(_seal, seal);
}
internal readonly record struct PropellantMassProperties(double TotalMassKilograms, PrincipalMomentsOfInertia Inertia);
internal readonly record struct PropellantSegment(bool Powered, PropellantDuration Duration,
    Double3 EngineForceBodyNewtons, Double3 EngineMomentBodyNewtonMetres);

/// <summary>Deterministic WOULD-event. No live seal or process-global sequence; not an execution right.</summary>
internal readonly record struct PropellantExhaustionWitness(PropellantSourceObservation Resource,
    EngineActuationPreview Engine, PropellantDuration Offset);

/// <summary>Copied conditional values. Exact amount/offset fields are authority observations; mass doubles are rounded views.</summary>
internal readonly record struct PropellantSegmentationPreview(PropellantPreviewMeaning Meaning,
    PropellantSourceObservation Resource, EngineActuationPreview Engine, PropellantInteger FlowUnits,
    PropellantInteger RequiredUnits, PropellantInteger ConsumedUnits, PropellantInteger SuccessorUnits,
    PropellantClassification Classification, PropellantDuration PoweredDuration, PropellantDuration UnpoweredDuration,
    PropellantMassProperties SourceMass, PropellantMassProperties ProposedSuccessorMass)
{
    internal int SegmentCount => Meaning != PropellantPreviewMeaning.ProposedIfApplied ? 0 :
        Classification == PropellantClassification.InteriorExhaustion ? 2 : 1;
    internal bool WouldEndWithoutFeed => SuccessorUnits.IsZero;
    internal bool TryGetSegment(int index, out PropellantSegment segment)
    {
        segment = default;
        if ((uint)index >= (uint)SegmentCount) return false;
        var powered = !PoweredDuration.IsZero && index == 0;
        segment = new(powered, powered ? PoweredDuration : UnpoweredDuration,
            powered ? Engine.ProposedForceBodyNewtons : Double3.Zero,
            powered ? Engine.ProposedMomentBodyNewtonMetres : Double3.Zero);
        return true;
    }
    internal bool TryGetInteriorExhaustion(out PropellantExhaustionWitness witness)
    {
        witness = default;
        if (Meaning != PropellantPreviewMeaning.ProposedIfApplied || Classification != PropellantClassification.InteriorExhaustion)
            return false;
        witness = new(Resource, Engine, PoweredDuration); return true;
    }
}

/// <summary>Pure exact arithmetic. A calculated value alone is never a sealed resource proposal.</summary>
internal static class PropellantSegmentation
{
    internal static PropellantPreparationStatus Calculate(in PropellantInteger available, double flow,
        bool hasPhysicalDemand, long ticks, out PropellantClassification classification,
        out PropellantInteger flowUnits, out PropellantInteger required, out PropellantInteger consumed,
        out PropellantInteger successor, out PropellantDuration powered, out PropellantDuration unpowered)
    {
        classification = default; flowUnits = required = consumed = successor = default;
        powered = unpowered = default;
        if (ticks <= 0) return PropellantPreparationStatus.InvalidInterval;
        if (!PropellantInteger.TryDecodeFlow(flow, out flowUnits) ||
            (flowUnits.IsZero && hasPhysicalDemand) || (!flowUnits.IsZero && !hasPhysicalDemand))
            return PropellantPreparationStatus.IncompatibleFlow;
        if (!PropellantInteger.TryMultiply(flowUnits, (ulong)ticks, out required))
            return PropellantPreparationStatus.ArithmeticFailure;
        var denominator = flowUnits.IsZero ? PropellantInteger.FromUInt64(1) : flowUnits;
        var full = flowUnits.IsZero ? PropellantInteger.FromUInt64((ulong)ticks) : required;
        powered = new(default, denominator); unpowered = new(full, denominator);
        if (flowUnits.IsZero)
        { classification = PropellantClassification.NoDemand; successor = available; return PropellantPreparationStatus.Ready; }
        if (available.IsZero)
        { classification = PropellantClassification.NoFeed; return PropellantPreparationStatus.Ready; }
        var compare = PropellantInteger.Compare(available, required);
        if (compare >= 0)
        {
            if (!PropellantInteger.TrySubtract(available, required, out successor))
                return PropellantPreparationStatus.ArithmeticFailure;
            classification = compare == 0 ? PropellantClassification.EndpointExhaustion : PropellantClassification.FullPowered;
            consumed = required; powered = new(full, denominator); unpowered = new(default, denominator);
        }
        else
        {
            if (!PropellantInteger.TrySubtract(required, available, out var remainder))
                return PropellantPreparationStatus.ArithmeticFailure;
            classification = PropellantClassification.InteriorExhaustion;
            consumed = available; powered = new(available, denominator); unpowered = new(remainder, denominator);
        }
        return PropellantPreparationStatus.Ready;
    }
}
