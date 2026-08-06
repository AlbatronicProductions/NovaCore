using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Pure, caller-buffered authored-system evaluation. It publishes no partial result on failure.</summary>
internal static class CelestialSystemEvaluator
{
    internal static CelestialSystemEvaluationResult TryEvaluateSystem(CelestialSystemDefinition system, SimulationInstant instant, Span<ReferenceFrameEvaluation> destination, Span<FrameTransform> rootTransforms, Span<ReferenceFrameEvaluation> staging, Span<FrameTransform> stagingRoots)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (destination.Length < system.Count || rootTransforms.Length < system.Count || staging.Length < system.Count || stagingRoots.Length < system.Count) return new(CelestialSystemEvaluationStatus.DestinationTooSmall);
        var validation = ValidateSystem(system);
        if (!validation.Succeeded) return validation;
        for (var index = 0; index < system.Count; index++)
        {
            var node = system.GetNodeInTraversalOrder(index); var body = node.Body;
            FrameTransform local; Double3 velocity;
            if (node.ParentId is null)
            {
                local = FrameTransform.Identity; velocity = Double3.Zero;
            }
            else
            {
                var parentIndex = FindParentTraversalIndex(system, index, node.ParentId.Value);
                if (parentIndex < 0) return new(CelestialSystemEvaluationStatus.ParentEvaluationFailed);
                var parent = system.GetNodeInTraversalOrder(parentIndex);
                if (node.TrajectoryModel == CelestialTrajectoryModel.ReservedNumericalNBody) return new(CelestialSystemEvaluationStatus.UnsupportedTrajectoryModel);
                if (node.Trajectory is not { } trajectory || trajectory.CentralBody != node.ParentId.Value) return new(CelestialSystemEvaluationStatus.ParentEvaluationFailed);
                if (node.TrajectoryModel is not (CelestialTrajectoryModel.AnalyticalKepler or CelestialTrajectoryModel.CircularOrbit)) return new(CelestialSystemEvaluationStatus.UnsupportedTrajectoryModel);
                if (!double.IsFinite(parent.Body.GravitationalParameter) || parent.Body.GravitationalParameter <= 0d) return new(CelestialSystemEvaluationStatus.InvalidConstants);
                var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(trajectory.StateAtEpoch, trajectory.Epoch, instant, parent.Body.GravitationalParameter);
                if (!propagation.Succeeded) return new(CelestialSystemEvaluationStatus.NumericalFailure);
                if (!propagation.State.IsFinite) return new(CelestialSystemEvaluationStatus.NonFiniteResult);
                local = new FrameTransform(propagation.State.Position, DoubleQuaternion.Identity); velocity = propagation.State.Velocity;
            }
            var root = index == 0 ? FrameTransform.Identity : FrameTransform.Compose(stagingRoots[FindParentTraversalIndex(system, index, node.ParentId!.Value)], local);
            if (!local.IsFinite || !root.IsFinite || !velocity.IsFinite) return new(CelestialSystemEvaluationStatus.NonFiniteResult);
            staging[index] = new(body.InertialFrame, new EvaluatedReferenceFrame(local, velocity, Double3.Zero, true));
            stagingRoots[index] = root;
        }
        staging[..system.Count].CopyTo(destination); stagingRoots[..system.Count].CopyTo(rootTransforms);
        return new(CelestialSystemEvaluationStatus.Success);
    }

    private static CelestialSystemEvaluationResult ValidateSystem(CelestialSystemDefinition system)
    {
        var rootSeen = false;
        for (var index = 0; index < system.Count; index++)
        {
            var node = system.GetNodeInTraversalOrder(index); var body = node.Body;
            if (!body.Id.IsValid || body.InertialFrame.Value == 0 || !double.IsFinite(body.GravitationalParameter) || body.GravitationalParameter <= 0d) return new(CelestialSystemEvaluationStatus.InvalidConstants);
            if (node.ParentId is null) { if (index != 0 || rootSeen || node.TrajectoryModel != CelestialTrajectoryModel.FixedBody) return new(CelestialSystemEvaluationStatus.InvalidHierarchy); rootSeen = true; }
            else if (!rootSeen || FindParentTraversalIndex(system, index, node.ParentId.Value) < 0) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
        }
        return rootSeen ? new(CelestialSystemEvaluationStatus.Success) : new(CelestialSystemEvaluationStatus.InvalidHierarchy);
    }

    private static int FindParentTraversalIndex(CelestialSystemDefinition system, int beforeIndex, CelestialBodyId parent)
    {
        for (var index = 0; index < beforeIndex; index++) if (system.GetNodeInTraversalOrder(index).Id == parent) return index;
        return -1;
    }
}
