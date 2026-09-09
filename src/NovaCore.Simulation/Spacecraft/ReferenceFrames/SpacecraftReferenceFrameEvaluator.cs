using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;

namespace NovaCore.Simulation.Spacecraft.ReferenceFrames;

/// <summary>Pure body-frame extraction layered onto already staged celestial carrier frames.</summary>
internal static class SpacecraftReferenceFrameEvaluator
{
    internal static SpacecraftReferenceFrameEvaluationStatus TryEvaluate(
        in SpacecraftStateView spacecraft, ReferenceFrameGraph graph, SimulationInstant requestedTime,
        Span<ReferenceFrameEvaluation> destination)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (destination.Length < graph.Count) return SpacecraftReferenceFrameEvaluationStatus.DestinationTooSmall;
        for (var index = 0; index < spacecraft.Count; index++)
        {
            var definition = spacecraft.GetDefinition(index);
            if (!graph.TryGetIndex(definition.CarrierFrame, out var carrierIndex) || !graph.TryGetIndex(definition.BodyFrame, out var bodyIndex)) return SpacecraftReferenceFrameEvaluationStatus.FrameMissing;
            if (graph.GetParentIndexAt(bodyIndex) != carrierIndex) return SpacecraftReferenceFrameEvaluationStatus.CarrierOwnershipMismatch;
            DoubleQuaternion orientation;
            Double3 angularVelocity;
            if (spacecraft.TryGetRigidBody(definition.Id, out var rigid))
            {
                if (spacecraft.TryGetTranslation(definition.Id, out _, out _) && requestedTime < rigid.Epoch)
                    return SpacecraftReferenceFrameEvaluationStatus.AttitudeEvaluationFailed;
                var evaluatedRigid = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rigid, requestedTime);
                if (!evaluatedRigid.Succeeded) return SpacecraftReferenceFrameEvaluationStatus.AttitudeEvaluationFailed;
                orientation = evaluatedRigid.OrientationLocalToParent;
                angularVelocity = evaluatedRigid.AngularVelocityBody;
            }
            else
            {
                var attitude = spacecraft.GetAttitude(index);
                var evaluated = SpacecraftAttitudeEvaluator.TryEvaluate(attitude, requestedTime);
                if (!evaluated.Succeeded) return SpacecraftReferenceFrameEvaluationStatus.AttitudeEvaluationFailed;
                orientation = evaluated.OrientationLocalToParent;
                angularVelocity = evaluated.AngularVelocityBody;
            }
            var parentAngularVelocity = orientation.Rotate(angularVelocity);
            if (!parentAngularVelocity.IsFinite) return SpacecraftReferenceFrameEvaluationStatus.NonFiniteResult;
            var position = Double3.Zero; var velocity = Double3.Zero;
            if (spacecraft.TryGetTranslation(definition.Id, out var translation, out var properties))
            {
                var root = graph.GetNodeAt(carrierIndex);
                if (graph.RootCount != 1 || root.ParentId is not null || root.Kind != ReferenceFrameKind.Ecl || translation.RootFrame != root.Id)
                    return SpacecraftReferenceFrameEvaluationStatus.CarrierOwnershipMismatch;
                var linear = SpacecraftTranslationEvaluator.TryEvaluate(translation, properties, requestedTime);
                if (!linear.Succeeded) return SpacecraftReferenceFrameEvaluationStatus.TranslationEvaluationFailed;
                position = linear.PositionRoot; velocity = linear.VelocityRoot;
            }
            destination[bodyIndex] = new ReferenceFrameEvaluation(definition.BodyFrame, new EvaluatedReferenceFrame(new FrameTransform(position, orientation), velocity, parentAngularVelocity, false));
        }
        return SpacecraftReferenceFrameEvaluationStatus.Success;
    }
}

internal enum SpacecraftReferenceFrameEvaluationStatus : byte
{
    Success = 0,
    DestinationTooSmall,
    FrameMissing,
    CarrierOwnershipMismatch,
    AttitudeEvaluationFailed,
    NonFiniteResult,
    TranslationEvaluationFailed,
}
