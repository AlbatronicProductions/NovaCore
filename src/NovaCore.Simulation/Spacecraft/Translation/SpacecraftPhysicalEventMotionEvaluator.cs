using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Read-only event query. Use a fresh view within the existing simulation single-writer phase.</summary>
internal static class SpacecraftPhysicalEventMotionEvaluator
{
    internal static SpacecraftTranslationStatus TryEvaluate(SimulationStateView state, SpacecraftId subject,
        PhysicalEventEpoch time, out SpacecraftPhysicalEventMotion motion)
    {
        motion = default;
        if (time.TryGetCanonicalInstant(out var canonical))
        {
            var status = SpacecraftMotionEvaluator.TryEvaluate(state, subject, canonical, out var value);
            if (status == SpacecraftTranslationStatus.Success)
                motion = new(value.Spacecraft, time, value.Revision, value.RootFrame, value.PositionRoot, value.VelocityRoot,
                    value.BodyToRoot, value.AngularVelocityBody, value.Properties, value.Inertia);
            return status;
        }
        // Copy immutable segments before numerical work. This view is neither a deep snapshot nor a lock.
        if (!state.Spacecraft.TryGetTranslation(subject, out var linear, out var properties) ||
            !state.Spacecraft.TryGetRigidBody(subject, out var angular)) return SpacecraftTranslationStatus.SubjectNotFound;
        var revision = state.Revision;
        if (time.CompareTo(PhysicalEventEpoch.FromCanonical(angular.Epoch)) < 0) return SpacecraftTranslationStatus.TimeBeforeEpoch;
        var translation = SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(linear, properties, time, out var p, out var v);
        if (translation != SpacecraftTranslationStatus.Success) return translation;
        var rotation = SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(angular, time, out var q, out var w, out _);
        if (rotation != SpacecraftRigidBodyRotationEvaluationStatus.Success) return SpacecraftTranslationStatus.RotationEvaluationFailed;
        motion = new(subject, time, revision, linear.RootFrame, p, v, q, w, properties, angular.PrincipalInertia);
        return SpacecraftTranslationStatus.Success;
    }
}
