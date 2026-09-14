using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Commands;

internal enum SpacecraftCommandKind { Invalid, IgniteRequest, ShutdownRequest, HeldAxes, ThrottlePosition, RcsEnabled, Mode, Target, Neutralize }
internal enum RequestedControlMode { Manual, RateAssist, AttitudeAssist }
internal enum CommandTargetKind { None, Attitude, AngularRate, CaptureAttitude }

/// <summary>Requested target only. Attitude is body-to-root; rate is expressed in the named root frame.</summary>
internal readonly record struct SpacecraftCommandTarget(CommandTargetKind Kind, ReferenceFrameId Frame,
    DoubleQuaternion Attitude, Double3 AngularRate)
{
    internal static SpacecraftCommandTarget Hold(ReferenceFrameId frame) => new(CommandTargetKind.CaptureAttitude, frame, default, default);
}

/// <summary>Device-free value input. Axes are normalized body-frame intent, not force or torque.</summary>
internal readonly record struct SpacecraftCommandIntent
{
    internal SpacecraftCommandKind Kind { get; }
    internal Double3 Rotation { get; }
    internal Double3 Translation { get; }
    internal double Throttle { get; }
    internal bool Enabled { get; }
    internal RequestedControlMode Mode { get; }
    internal SpacecraftCommandTarget Target { get; }
    private SpacecraftCommandIntent(SpacecraftCommandKind kind, Double3 rotation = default, Double3 translation = default,
        double throttle = 0, bool enabled = false, RequestedControlMode mode = default, SpacecraftCommandTarget target = default)
    { Kind = kind; Rotation = rotation; Translation = translation; Throttle = throttle; Enabled = enabled; Mode = mode; Target = target; }
    internal static SpacecraftCommandIntent Ignite() => new(SpacecraftCommandKind.IgniteRequest);
    internal static SpacecraftCommandIntent Shutdown() => new(SpacecraftCommandKind.ShutdownRequest);
    internal static SpacecraftCommandIntent Axes(Double3 rotation, Double3 translation) => new(SpacecraftCommandKind.HeldAxes, rotation, translation);
    internal static SpacecraftCommandIntent ThrottlePosition(double value) => new(SpacecraftCommandKind.ThrottlePosition, throttle: value);
    internal static SpacecraftCommandIntent Rcs(bool enabled) => new(SpacecraftCommandKind.RcsEnabled, enabled: enabled);
    internal static SpacecraftCommandIntent ControlMode(RequestedControlMode mode) => new(SpacecraftCommandKind.Mode, mode: mode);
    internal static SpacecraftCommandIntent DesiredTarget(SpacecraftCommandTarget target) => new(SpacecraftCommandKind.Target, target: target);
    internal static SpacecraftCommandIntent Neutralize() => new(SpacecraftCommandKind.Neutralize);
}

internal readonly record struct CommandRevision(ulong Value);

/// <summary>Canonical requested state. The last engine request identity is an edge, not engine-running evidence.</summary>
internal readonly record struct SpacecraftCommandState(Double3 RotationIntent, Double3 TranslationIntent,
    double RequestedThrottlePosition, bool RequestedRcsEnabled, RequestedControlMode RequestedMode,
    SpacecraftCommandTarget Target, SpacecraftCommandKind LastEngineRequest, ulong EngineRequestSequence);

/// <summary>Copied non-actuating consumer output. Deliberately contains no realized wrench or hardware state.</summary>
internal readonly record struct RequestedControlDemand(SpacecraftId Spacecraft, SpacecraftCommandState Requested, CommandRevision Revision);

internal readonly record struct SpacecraftCommandObservation(SpacecraftId Spacecraft, ulong Lease,
    SpacecraftCommandState State, CommandRevision Revision, SimulationInstant LastTransitionEpoch,
    ulong LastConsumedSequence, bool IngressRevoked, int PendingCount, ulong LastAcceptedSequence,
    long ClosedThroughBoundaryIndex, SimulationInstant NextPendingEpoch)
{
    internal RequestedControlDemand ConsumeWithoutActuation() => new(Spacecraft, State, Revision);
}

internal enum SpacecraftCommandStatus
{
    Accepted, Committed, NoChange, NoCommand, Pending, BoundaryReady, BoundaryClosed,
    WrongOwnerThread, ReentrantOperation, InvalidAuthority, AlreadyPrepared, UnsupportedClock,
    InvalidInput, InvalidTarget, DuplicateOrStaleSequence, SequenceGap, SequenceOverflow,
    IngressRevoked, Capacity, ArithmeticOverflow, SourceExhausted, PendingEvent,
    MissedBoundary, NotAtBoundary, SubjectUnavailable, RevisionOverflow, PreparationRefused, FundedHorizonRegressed
}

internal readonly record struct SpacecraftCommandAdmission(SpacecraftCommandStatus Status, ulong Sequence = 0,
    SimulationInstant EffectiveEpoch = default);
internal readonly record struct SpacecraftCommandCommit(SpacecraftCommandStatus Status, ulong Sequence = 0,
    SimulationInstant EffectiveEpoch = default, SpacecraftCommandObservation Observation = default);

internal readonly record struct PendingSpacecraftCommand(ulong Sequence, SimulationInstant Epoch, SpacecraftCommandIntent Intent);
