using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial.ReferenceFrames;

/// <summary>
/// Pure extraction from authoritative celestial Cartesian trajectories into evaluated
/// local-to-parent frame values. It owns neither topology, time, state mutation, nor publication.
/// </summary>
internal static class CelestialReferenceFrameEvaluator
{
    internal static CelestialReferenceFrameEvaluationStatus TryEvaluate(
        in CelestialStateView celestial,
        ReferenceFrameGraph graph,
        SimulationInstant requestedTime,
        Span<ReferenceFrameEvaluation> destination)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (destination.Length < graph.Count) return CelestialReferenceFrameEvaluationStatus.DestinationTooSmall;
        if (celestial.Count == 0) return CelestialReferenceFrameEvaluationStatus.EmptySystem;
        if (graph.RootCount != 1) return CelestialReferenceFrameEvaluationStatus.MultipleGraphRoots;
        if (celestial.Count != graph.Count) return CelestialReferenceFrameEvaluationStatus.GraphCelestialCountMismatch;

        // Graph insertion order is canonical and parent-before-child. Require the celestial
        // declaration to match it rather than creating a second hierarchy or runtime index.
        for (var index = 0; index < graph.Count; index++)
        {
            var node = graph.GetNodeAt(index);
            var definition = celestial.GetDefinition(index);
            var state = celestial.GetState(index);
            if (definition.InertialFrame != node.Id) return CelestialReferenceFrameEvaluationStatus.FrameMappingMismatch;

            var parentIndex = graph.GetParentIndexAt(index);
            if (parentIndex < 0)
            {
                if (index != 0 || definition.PrimaryBody is not null || state.Trajectory is not null)
                    return CelestialReferenceFrameEvaluationStatus.RootMappingMismatch;
                continue;
            }

            var parentDefinition = celestial.GetDefinition(parentIndex);
            if (definition.PrimaryBody != parentDefinition.Id) return CelestialReferenceFrameEvaluationStatus.PrimaryParentMismatch;
            if (state.Trajectory is not { } trajectory) return CelestialReferenceFrameEvaluationStatus.ChildTrajectoryMissing;
            if (trajectory.CentralBody != parentDefinition.Id) return CelestialReferenceFrameEvaluationStatus.TrajectoryPrimaryMismatch;
        }

        for (var index = 0; index < graph.Count; index++)
        {
            var definition = celestial.GetDefinition(index);
            if (graph.GetParentIndexAt(index) < 0)
            {
                destination[index] = new ReferenceFrameEvaluation(
                    definition.InertialFrame,
                    new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true));
                continue;
            }

            var propagated = CelestialTrajectoryEvaluator.TryEvaluate(definition.Id, celestial, requestedTime);
            if (!propagated.Succeeded) return CelestialReferenceFrameEvaluationStatus.TrajectoryEvaluationFailed;
            if (!propagated.State.IsFinite) return CelestialReferenceFrameEvaluationStatus.NonFiniteEvaluatedState;
            destination[index] = new ReferenceFrameEvaluation(
                definition.InertialFrame,
                new EvaluatedReferenceFrame(
                    new FrameTransform(propagated.State.Position, DoubleQuaternion.Identity),
                    propagated.State.Velocity,
                    Double3.Zero,
                    true));
        }

        return CelestialReferenceFrameEvaluationStatus.Success;
    }
}
