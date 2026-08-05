using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;

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
            var attitude = spacecraft.GetAttitude(index);
            if (!graph.TryGetIndex(definition.CarrierFrame, out var carrierIndex) || !graph.TryGetIndex(definition.BodyFrame, out var bodyIndex)) return SpacecraftReferenceFrameEvaluationStatus.FrameMissing;
            if (graph.GetParentIndexAt(bodyIndex) != carrierIndex) return SpacecraftReferenceFrameEvaluationStatus.CarrierOwnershipMismatch;
            var evaluated = SpacecraftAttitudeEvaluator.TryEvaluate(attitude, requestedTime);
            if (!evaluated.Succeeded) return SpacecraftReferenceFrameEvaluationStatus.AttitudeEvaluationFailed;
            var parentAngularVelocity = evaluated.OrientationLocalToParent.Rotate(evaluated.AngularVelocityBody);
            if (!parentAngularVelocity.IsFinite) return SpacecraftReferenceFrameEvaluationStatus.NonFiniteResult;
            destination[bodyIndex] = new ReferenceFrameEvaluation(definition.BodyFrame, new EvaluatedReferenceFrame(new FrameTransform(Double3.Zero, evaluated.OrientationLocalToParent), Double3.Zero, parentAngularVelocity, false));
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
