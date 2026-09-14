using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Actuation;

internal enum EnginePreparationStatus
{
    InvalidInput, Ready, Prepared, Preview, Retired, InvalidDefinition, InvalidAuthority,
    AlreadyBound, LateBinding, WrongOwnerThread, ReentrantOperation, UnsupportedClock,
    InvalidSource, StaleSource, InvalidInterval, SourceExhausted, BoundaryNotClosed,
    IntervalGap, IntervalConsumed, OutstandingProposal, InvalidProposal, ArithmeticFailure,
    PendingEvent, PreparationRefused, InvalidIndex
}
internal enum ProposedEngineLatch { Off, Enabled }
internal enum EnginePreviewMeaning { Invalid, ProposedIfApplied }
internal enum ProposedEngineActivity { Inactive, Firing, Unavailable }
internal enum EngineDemandFrame { Invalid, BodyAboutCanonicalCom }
internal enum EngineFeedAssumption { Invalid, AvailableFeedRequiredFlowOnly }

/// <summary>Versioned ideal model: instantaneous [0,1] proportional throttle, no minimum, delay or consumable start.</summary>
internal readonly record struct IdealEngineDefinitionValues(ulong EngineId, uint Version, Double3 MountFromComMetres,
    Double3 ThrustAxisBody, double MaximumThrustNewtons, double EffectiveExhaustMetresPerSecond, bool HardwareAvailable);

internal sealed class IdealEngineDefinition
{
    internal IdealEngineDefinitionValues Values { get; }
    private IdealEngineDefinition(IdealEngineDefinitionValues values) => Values = values;

    internal static EnginePreparationStatus TryCreate(ulong id, uint version, Double3 mountFromComMetres,
        Double3 axisBody, double maximumThrustNewtons, double effectiveExhaustMetresPerSecond,
        bool hardwareAvailable, out IdealEngineDefinition? definition)
    {
        definition = null;
        if (id == 0 || version == 0 || !mountFromComMetres.IsFinite || !axisBody.IsFinite ||
            !double.IsFinite(maximumThrustNewtons) || maximumThrustNewtons <= 0 ||
            !double.IsFinite(effectiveExhaustMetresPerSecond) || effectiveExhaustMetresPerSecond <= 0)
            return EnginePreparationStatus.InvalidDefinition;
        // Scale before length: accept nonzero finite authored axes without square overflow/underflow.
        var scale = Math.Max(Math.Abs(axisBody.X), Math.Max(Math.Abs(axisBody.Y), Math.Abs(axisBody.Z)));
        if (scale == 0) return EnginePreparationStatus.InvalidDefinition;
        var scaled = axisBody / scale;
        var unit = scaled / Math.Sqrt(scaled.LengthSquared);
        definition = new(new(id, version, mountFromComMetres, unit, maximumThrustNewtons,
            effectiveExhaustMetresPerSecond, hardwareAvailable));
        return EnginePreparationStatus.Ready;
    }
}

/// <summary>Immutable issued binding. It exposes no cursor, array or mutable hardware state.</summary>
internal sealed class EnginePreparationAuthority
{
    internal readonly SpacecraftCommandAuthority Commands;
    internal readonly IdealEngineDefinition Definition;
    internal EnginePreparationAuthority(SpacecraftCommandAuthority commands, IdealEngineDefinition definition)
    { Commands = commands; Definition = definition; }
}

internal readonly record struct CapturedEngineTransition(ulong Sequence, SimulationInstant Epoch,
    SpacecraftCommandKind Kind, CommandRevision CommandRevision);

/// <summary>Private evaluation progress only; consumption here is not hardware publication or physical acknowledgement.</summary>
internal readonly record struct EnginePreparationProgress(ulong CapturedThroughSequence, ulong PreparationConsumedSequence,
    int PendingEngineTransitions, long LastPreparedBoundary, bool HasProposal, ProposedEngineLatch PreparationLatch);

/// <summary>Opaque current-preview capability. Constructing/copying public values cannot forge its private issue seal.</summary>
internal readonly struct EngineActuationProposal
{
    private readonly object? seal;
    internal readonly long Boundary;
    internal EngineActuationProposal(long boundary, object? seal = null) { Boundary = boundary; this.seal = seal; }
    internal bool IsIssuedBy(object identity) => ReferenceEquals(seal, identity);
}

/// <summary>
/// Conditional copied BODY-wrench interval model. No root-force, fuel reservation/depletion or actual firing is asserted.
/// A future consumer must rotate the body wrench using its qualified interval motion, not just its source orientation.
/// </summary>
internal readonly record struct EngineActuationPreview(EnginePreviewMeaning Meaning, IdealEngineDefinitionValues Definition,
    SpacecraftId Spacecraft, ReferenceFrameId BodyFrame, ReferenceFrameId RootFrame,
    SimulationInstant Start, SimulationInstant End, long BoundaryIndex, StateRevision SourceStateRevision,
    TimelineRevision SourceTimelineRevision, CommandRevision SourceCommandRevision, ulong SourceCommandSequence,
    ulong PreparationCursorBefore, ulong PreparationCursorAfter, int EngineTransitionCount,
    int IgniteCount, int ShutdownCount, ulong OrderedTransitionFingerprint,
    SpacecraftCommandKind LastRequestedEngineEdge, ulong LastRequestedEngineSequence,
    ProposedEngineLatch ProposedLatch, ProposedEngineActivity ProposedActivity,
    double RequestedThrottle, double ProposedRealizedThrottle, double ProposedThrustNewtons,
    Double3 ProposedForceBodyNewtons, Double3 ProposedMomentBodyNewtonMetres,
    double RequiredMassFlowKilogramsPerSecond, EngineDemandFrame Frame, EngineFeedAssumption FeedAssumption);
