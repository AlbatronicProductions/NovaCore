using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Pure construction and validation of direct trajectory-replacement candidates.</summary>
internal static class CelestialTrajectoryTransactionEvaluator
{
    internal static CelestialTrajectoryTransactionCreationResult TryCreateReplacement(
        ScheduledSimulationEvent canonicalEvent,
        SimulationStateView state,
        TimelineRevision timelineRevision,
        CelestialBodyId subject,
        TwoBodyTrajectory replacement)
    {
        var status = ValidateReplacement(canonicalEvent.Header.Time, state.Celestial, subject, default, replacement, requireExpected: false, out var current);
        if (canonicalEvent.Header.Kind != SimulationEventKind.ReplaceTrajectory)
            status = CelestialTrajectoryTransactionStatus.InvalidEventKind;
        if (status != CelestialTrajectoryTransactionStatus.Success)
            return new(status, null);

        return new(CelestialTrajectoryTransactionStatus.Success, new(
            canonicalEvent,
            canonicalEvent.Header.Time,
            timelineRevision,
            state.Revision,
            subject,
            current,
            replacement));
    }

    internal static CelestialTrajectoryTransactionStatus ValidateReplacement(
        SimulationInstant eventTime,
        CelestialStateView celestial,
        CelestialBodyId subject,
        TwoBodyTrajectory expected,
        TwoBodyTrajectory replacement,
        bool requireExpected,
        out TwoBodyTrajectory current)
    {
        current = default;
        if (!celestial.TryGetDefinition(subject, out var definition) || !celestial.TryGetState(subject, out var state))
            return CelestialTrajectoryTransactionStatus.SubjectNotFound;
        if (definition.PrimaryBody is not { } primary)
            return CelestialTrajectoryTransactionStatus.RootBody;
        if (state.Trajectory is not { } trajectory)
            return CelestialTrajectoryTransactionStatus.NoCurrentTrajectory;
        current = trajectory;
        if (requireExpected && !TwoBodyTrajectoryIdentity.EqualsRaw(trajectory, expected))
            return CelestialTrajectoryTransactionStatus.TrajectoryBasisMismatch;
        if (replacement.Epoch != eventTime)
            return CelestialTrajectoryTransactionStatus.EventTimeMismatch;
        if (TwoBodyTrajectoryIdentity.EqualsRaw(trajectory, replacement))
            return CelestialTrajectoryTransactionStatus.ReplacementNoOp;
        if (replacement.CentralBody != primary)
            return CelestialTrajectoryTransactionStatus.ReplacementCentralMismatch;
        if (!celestial.TryGetDefinition(primary, out var central))
            return CelestialTrajectoryTransactionStatus.CentralBodyNotFound;
        if (!double.IsFinite(central.GravitationalParameter) || central.GravitationalParameter <= 0d)
            return CelestialTrajectoryTransactionStatus.InvalidCentralGravitationalParameter;
        if (replacement.Model != TwoBodyPropagationModel.CartesianTwoBodyV1)
            return CelestialTrajectoryTransactionStatus.UnsupportedModel;
        if (!replacement.StateAtEpoch.IsFinite)
            return CelestialTrajectoryTransactionStatus.InvalidReplacementState;

        var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(
            replacement.StateAtEpoch,
            replacement.Epoch,
            eventTime,
            central.GravitationalParameter);
        return propagation.Succeeded
            ? CelestialTrajectoryTransactionStatus.Success
            : CelestialTrajectoryTransactionStatus.UnsupportedReplacementOrbit;
    }
}
