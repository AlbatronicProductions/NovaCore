using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Closed-form integration of a constant root force. Constant work, no render delta, subdivision or mutable cache.</summary>
internal static class SpacecraftTranslationEvaluator
{
    internal static SpacecraftTranslationEvaluation TryEvaluate(
        in SpacecraftTranslationState state, in SpacecraftPhysicalProperties properties, SimulationInstant time)
    {
        if (!properties.IsValid) return Failure(SpacecraftTranslationStatus.InvalidMass, time);
        if (!state.Spacecraft.IsValid || state.RootFrame.Value == 0 || !state.PositionRoot.IsFinite ||
            !state.VelocityRoot.IsFinite || !state.ConstantForceRoot.IsFinite)
            return Failure(SpacecraftTranslationStatus.InvalidState, time);
        if (time < state.Epoch) return Failure(SpacecraftTranslationStatus.TimeBeforeEpoch, time);
        long ticks;
        try { ticks = checked(time.Ticks - state.Epoch.Ticks); }
        catch (OverflowException) { return Failure(SpacecraftTranslationStatus.DurationOverflow, time); }
        var acceleration = new Double3(state.ConstantForceRoot.X / properties.MassKilograms,
            state.ConstantForceRoot.Y / properties.MassKilograms, state.ConstantForceRoot.Z / properties.MassKilograms);
        if (!acceleration.IsFinite) return Failure(SpacecraftTranslationStatus.NonFiniteResult, time);
        if (ticks == 0) return new(SpacecraftTranslationStatus.Success, time, state.PositionRoot, state.VelocityRoot);
        // Difference in exact ticks first preserves short intervals at late absolute epochs.
        var seconds = ticks / (double)SimulationInstant.TicksPerSecond;
        var deltaVelocity = acceleration * seconds;
        var velocity = state.VelocityRoot + deltaVelocity;
        var position = state.PositionRoot + state.VelocityRoot * seconds + deltaVelocity * (0.5d * seconds);
        return position.IsFinite && velocity.IsFinite
            ? new(SpacecraftTranslationStatus.Success, time, position, velocity)
            : Failure(SpacecraftTranslationStatus.NonFiniteResult, time);
    }

    private static SpacecraftTranslationEvaluation Failure(SpacecraftTranslationStatus status, SimulationInstant time) => new(status, time, default, default);
}
