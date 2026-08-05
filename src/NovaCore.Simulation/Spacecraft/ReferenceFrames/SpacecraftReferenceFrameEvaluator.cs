using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Spacecraft.Rotation;

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
            destination[bodyIndex] = new ReferenceFrameEvaluation(definition.BodyFrame, new EvaluatedReferenceFrame(new FrameTransform(Double3.Zero, orientation), Double3.Zero, parentAngularVelocity, false));
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
}
