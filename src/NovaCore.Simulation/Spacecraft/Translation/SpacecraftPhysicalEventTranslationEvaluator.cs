using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Read-only exact-event specialization of the banked constant-root-force model.</summary>
internal static class SpacecraftPhysicalEventTranslationEvaluator
{
    internal static SpacecraftTranslationStatus TryEvaluate(in SpacecraftTranslationState state,
        in SpacecraftPhysicalProperties properties, PhysicalEventEpoch time, out Double3 position, out Double3 velocity)
    {
        position = default;
        velocity = default;
        if (time.TryGetCanonicalInstant(out var canonical))
        {
            var value = SpacecraftTranslationEvaluator.TryEvaluate(state, properties, canonical);
            if (value.Succeeded) { position = value.PositionRoot; velocity = value.VelocityRoot; }
            return value.Status;
        }
        if (!properties.IsValid) return SpacecraftTranslationStatus.InvalidMass;
        if (!state.Spacecraft.IsValid || state.RootFrame.Value == 0 || !state.PositionRoot.IsFinite ||
            !state.VelocityRoot.IsFinite || !state.ConstantForceRoot.IsFinite) return SpacecraftTranslationStatus.InvalidState;
        if (time.CompareTo(PhysicalEventEpoch.FromCanonical(state.Epoch)) < 0) return SpacecraftTranslationStatus.TimeBeforeEpoch;
        if (!PhysicalEventDuration.TryDifference(time, state.Epoch, out var duration)) return SpacecraftTranslationStatus.DurationOverflow;

        // One analytical evaluation from the stored segment avoids rounding an intermediate floor position.
        var seconds = duration.Seconds;
        var acceleration = new Double3(state.ConstantForceRoot.X / properties.MassKilograms,
            state.ConstantForceRoot.Y / properties.MassKilograms, state.ConstantForceRoot.Z / properties.MassKilograms);
        if (!acceleration.IsFinite) return SpacecraftTranslationStatus.NonFiniteResult;
        var deltaVelocity = acceleration * seconds;
        var nextVelocity = state.VelocityRoot + deltaVelocity;
        var nextPosition = state.PositionRoot + state.VelocityRoot * seconds + deltaVelocity * (0.5d * seconds);
        if (!nextPosition.IsFinite || !nextVelocity.IsFinite) return SpacecraftTranslationStatus.NonFiniteResult;
        position = nextPosition;
        velocity = nextVelocity;
        return SpacecraftTranslationStatus.Success;
    }
}
