using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Immutable simulation mass in kilograms; never inferred from visual scale or changed by force intent.</summary>
internal readonly record struct SpacecraftPhysicalProperties(double MassKilograms)
{
    internal bool IsValid => double.IsFinite(MassKilograms) && MassKilograms > 0d;
}

/// <summary>
/// One constant-net-force segment, in metres, metres/second and newtons in the existing inertial ECL root.
/// Position is the spacecraft COM; body-frame origin is the COM. Observation never rebases this epoch.
/// </summary>
internal readonly record struct SpacecraftTranslationState(
    SpacecraftId Spacecraft, ReferenceFrameId RootFrame, SimulationInstant Epoch,
    Double3 PositionRoot, Double3 VelocityRoot, Double3 ConstantForceRoot);

internal enum SpacecraftTranslationStatus : byte
{
    Success, InvalidState, InvalidMass, TimeBeforeEpoch, DurationOverflow, NonFiniteResult,
    SubjectNotFound, FrameMismatch, RotationEvaluationFailed, StateRevisionMismatch,
    TimeMismatch, ExpectedStateMismatch, InvalidReplacement, HistoryCapacityFailure,
    StateRevisionOverflow, EventMismatch,
}

internal readonly record struct SpacecraftTranslationEvaluation(
    SpacecraftTranslationStatus Status, SimulationInstant Time, Double3 PositionRoot, Double3 VelocityRoot)
{
    internal bool Succeeded => Status == SpacecraftTranslationStatus.Success;
}

/// <summary>Net force intent in inertial root axes, not body-following thrust. Application uses the canonical timeline.</summary>
internal readonly record struct SpacecraftForceCommand(SpacecraftId Spacecraft, SimulationInstant Time, Double3 ForceRoot);
