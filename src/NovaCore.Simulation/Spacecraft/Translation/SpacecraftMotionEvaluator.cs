using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Translation;

/// <summary>Complete derived motion at one instant/revision. Stored linear and angular segment epochs may differ.</summary>
internal readonly record struct SpacecraftMotion(
    SpacecraftId Spacecraft, SimulationInstant Time, StateRevision Revision, ReferenceFrameId RootFrame,
    Double3 PositionRoot, Double3 VelocityRoot, DoubleQuaternion BodyToRoot, Double3 AngularVelocityBody,
    SpacecraftPhysicalProperties Properties, PrincipalMomentsOfInertia Inertia);

internal static class SpacecraftMotionEvaluator
{
    /// <summary>Call within the simulation single-writer phase using a fresh state view. Output is complete or default.</summary>
    internal static SpacecraftTranslationStatus TryEvaluate(SimulationStateView state, SpacecraftId subject,
        SimulationInstant time, out SpacecraftMotion motion)
    {
        motion = default;
        if (!state.Spacecraft.TryGetTranslation(subject, out var translation, out var properties) ||
            !state.Spacecraft.TryGetRigidBody(subject, out var rotation)) return SpacecraftTranslationStatus.SubjectNotFound;
        if (time < rotation.Epoch) return SpacecraftTranslationStatus.TimeBeforeEpoch;
        var linear = SpacecraftTranslationEvaluator.TryEvaluate(translation, properties, time);
        if (!linear.Succeeded) return linear.Status;
        var angular = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, time);
        if (!angular.Succeeded) return SpacecraftTranslationStatus.RotationEvaluationFailed;
        motion = new(subject, time, state.Revision, translation.RootFrame, linear.PositionRoot, linear.VelocityRoot,
            angular.OrientationLocalToParent, angular.AngularVelocityBody, properties, rotation.PrincipalInertia);
        return SpacecraftTranslationStatus.Success;
    }
}
