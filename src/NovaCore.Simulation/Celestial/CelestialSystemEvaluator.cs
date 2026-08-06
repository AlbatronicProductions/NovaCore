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
        if (system.TryMapTime(instant, out var requestedArgument) != CelestialSystemTimeMappingStatus.Success) return new(CelestialSystemEvaluationStatus.TimeMappingFailure);
        var domainTicksPerSecond = system.TimeMapping.DomainAnchor.DomainTicksPerSecond;
        if (!requestedArgument.TryToSimulationInstant(domainTicksPerSecond, out var requestedSolverTime)) return new(CelestialSystemEvaluationStatus.TimeMappingFailure);
        for (var index = 0; index < system.Count; index++)
        {
            var node = system.GetNodeInTraversalOrder(index);
            if (!system.TryGetBody(node.Id, out var body)) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
            var parentId = body.Identity.ParentBody;
            FrameTransform local; Double3 velocity;
            if (node.TrajectoryModel == CelestialTrajectoryModel.FixedBody)
            {
                if (!system.TryGetFixedBody(node.Ephemeris.PayloadIndex, out var fixedBody)) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
                local = new FrameTransform(fixedBody.Position, fixedBody.Orientation); velocity = fixedBody.Velocity;
            }
            else
            {
                if (parentId is null) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
                var parentIndex = FindParentTraversalIndex(system, index, parentId.Value);
                if (parentIndex < 0) return new(CelestialSystemEvaluationStatus.ParentEvaluationFailed);
                var parent = system.GetNodeInTraversalOrder(parentIndex);
                if (node.TrajectoryModel == CelestialTrajectoryModel.ReservedNumericalNBody) return new(CelestialSystemEvaluationStatus.UnsupportedTrajectoryModel);
                if (node.TrajectoryModel == CelestialTrajectoryModel.SampledEphemeris && system.TryGetSampledEphemeris(node.Ephemeris.PayloadIndex, out var sampled))
                {
                    var sampledResult = SampledEphemerisEvaluator.TryEvaluate(system.Samples, sampled, requestedArgument, domainTicksPerSecond);
                    if (!sampledResult.Succeeded) return new(sampledResult.Status is SampledEphemerisEvaluationStatus.BeforeCoverage or SampledEphemerisEvaluationStatus.AfterCoverage ? CelestialSystemEvaluationStatus.TimeMappingFailure : CelestialSystemEvaluationStatus.NumericalFailure);
                    local = new FrameTransform(sampledResult.State.Position, DoubleQuaternion.Identity); velocity = sampledResult.State.Velocity;
                    goto LocalResolved;
                }
                CartesianState epochState; SimulationInstant epochSolverTime;
                if (node.TrajectoryModel == CelestialTrajectoryModel.AnalyticalKepler && system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var trajectory))
                {
                    if (system.TryMapTime(trajectory.Epoch, out var epochArgument) != CelestialSystemTimeMappingStatus.Success || !epochArgument.TryToSimulationInstant(domainTicksPerSecond, out epochSolverTime)) return new(CelestialSystemEvaluationStatus.TimeMappingFailure);
                    epochState = trajectory.StateAtEpoch;
                }
                else if (node.TrajectoryModel == CelestialTrajectoryModel.CircularOrbit && system.TryGetCircularOrbit(node.Ephemeris.PayloadIndex, out var circular))
                {
                    var epochArgument = new CelestialTimeArgument(system.TimeMapping.DomainAnchor.Domain, circular.EpochDomainTicks, 0, 1);
                    if (!epochArgument.TryToSimulationInstant(domainTicksPerSecond, out epochSolverTime)) return new(CelestialSystemEvaluationStatus.TimeMappingFailure);
                    epochState = circular.ToCartesianState();
                }
                else return new(CelestialSystemEvaluationStatus.UnsupportedTrajectoryModel);
                if (!system.TryGetPhysicalProperties(parent.Id, out var parentProperties) || !double.IsFinite(parentProperties.GravitationalParameter) || parentProperties.GravitationalParameter <= 0d) return new(CelestialSystemEvaluationStatus.InvalidConstants);
                var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(epochState, epochSolverTime, requestedSolverTime, parentProperties.GravitationalParameter);
                if (!propagation.Succeeded) return new(CelestialSystemEvaluationStatus.NumericalFailure);
                if (!propagation.State.IsFinite) return new(CelestialSystemEvaluationStatus.NonFiniteResult);
                local = new FrameTransform(propagation.State.Position, DoubleQuaternion.Identity); velocity = propagation.State.Velocity;
            }
        LocalResolved:
            var root = parentId is null ? local : FrameTransform.Compose(stagingRoots[FindParentTraversalIndex(system, index, parentId.Value)], local);
            if (!local.IsFinite || !root.IsFinite || !velocity.IsFinite) return new(CelestialSystemEvaluationStatus.NonFiniteResult);
            staging[index] = new(new ReferenceFrameId((long)body.Id.Value), new EvaluatedReferenceFrame(local, velocity, Double3.Zero, true));
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
            var node = system.GetNodeInTraversalOrder(index);
            if (!system.TryGetBody(node.Id, out var body)) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
            if (!body.Id.IsValid || !body.PhysicalProperties.IsValid) return new(CelestialSystemEvaluationStatus.InvalidConstants);
            if (body.Identity.ParentBody is null) { if (index != 0 || rootSeen || node.TrajectoryModel != CelestialTrajectoryModel.FixedBody) return new(CelestialSystemEvaluationStatus.InvalidHierarchy); rootSeen = true; }
            else if (!rootSeen || FindParentTraversalIndex(system, index, body.Identity.ParentBody.Value) < 0) return new(CelestialSystemEvaluationStatus.InvalidHierarchy);
        }
        return rootSeen ? new(CelestialSystemEvaluationStatus.Success) : new(CelestialSystemEvaluationStatus.InvalidHierarchy);
    }

    private static int FindParentTraversalIndex(CelestialSystemDefinition system, int beforeIndex, CelestialBodyId parent)
    {
        for (var index = 0; index < beforeIndex; index++) if (system.GetNodeInTraversalOrder(index).Id == parent) return index;
        return -1;
    }
}
