using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Pure domain adapter resolving central μ from immutable celestial definitions before mathematical evaluation.</summary>
internal static class CelestialTrajectoryEvaluator
{
    internal static TwoBodyPropagationResult TryEvaluate(CelestialBodyId subject, in CelestialStateView celestial, SimulationInstant requestedTime)
    {
        if (!celestial.TryGetDefinition(subject, out var definition) || !celestial.TryGetState(subject, out var bodyState)) return Failure(TwoBodyPropagationStatus.BodyNotFound, requestedTime);
        if (bodyState.Trajectory is not { } trajectory) return Failure(TwoBodyPropagationStatus.NoTrajectory, requestedTime);
        if (definition.PrimaryBody is not { } primary) return Failure(TwoBodyPropagationStatus.InvalidTrajectory, requestedTime);
        if (trajectory.CentralBody != primary) return Failure(TwoBodyPropagationStatus.PrimaryCentralMismatch, requestedTime);
        if (!celestial.TryGetDefinition(primary, out var central)) return Failure(TwoBodyPropagationStatus.CentralBodyNotFound, requestedTime);
        if (trajectory.Model != TwoBodyPropagationModel.CartesianTwoBodyV1) return Failure(TwoBodyPropagationStatus.UnsupportedModel, requestedTime);
        return UniversalVariableTwoBodyPropagator.TryEvaluate(trajectory.StateAtEpoch, trajectory.Epoch, requestedTime, central.GravitationalParameter);
    }

    private static TwoBodyPropagationResult Failure(TwoBodyPropagationStatus status, SimulationInstant requestedTime) => new(status, requestedTime, default, 0);
}
