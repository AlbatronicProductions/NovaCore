using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

internal enum AnalyticalOrbitSamplingStatus : byte
{
    Success = 0,
    DestinationTooSmall,
    UnsupportedModel,
    CentralBodyNotFound,
    InvalidGravitationalParameter,
    UnsupportedOrbit,
    PropagationFailed,
    NonFiniteSample,
    PeriodOutOfRange,
}

internal readonly record struct AnalyticalOrbitSamplingResult(AnalyticalOrbitSamplingStatus Status, int VertexCount, ulong IdentityKey);

/// <summary>Pure fixed-count elliptic sampling. It derives geometry only; it never changes a trajectory or clock.</summary>
internal static class AnalyticalOrbitSampler
{
    internal const int SegmentCount = 256;
    internal const int VertexCount = SegmentCount + 1;

    internal static AnalyticalOrbitSamplingResult TrySample(in TwoBodyTrajectory trajectory, in CelestialStateView celestial, Span<Double3> destination)
    {
        if (destination.Length < VertexCount) return new(AnalyticalOrbitSamplingStatus.DestinationTooSmall, 0, 0);
        if (trajectory.Model != TwoBodyPropagationModel.CartesianTwoBodyV1) return new(AnalyticalOrbitSamplingStatus.UnsupportedModel, 0, 0);
        if (!celestial.TryGetDefinition(trajectory.CentralBody, out var central)) return new(AnalyticalOrbitSamplingStatus.CentralBodyNotFound, 0, 0);
        var mu = central.GravitationalParameter;
        if (!double.IsFinite(mu) || mu <= 0d) return new(AnalyticalOrbitSamplingStatus.InvalidGravitationalParameter, 0, 0);
        var key = ComputeIdentityKey(trajectory, mu);
        var state = trajectory.StateAtEpoch;
        var radius = Math.Sqrt(state.Position.LengthSquared);
        var alpha = 2d / radius - state.Velocity.LengthSquared / mu;
        var meanMotion = Math.Sqrt(mu) * alpha * Math.Sqrt(alpha);
        var period = 2d * Math.PI / meanMotion;
        if (!state.IsFinite || !double.IsFinite(radius) || radius <= 0d || !double.IsFinite(alpha) || alpha <= 0d || !double.IsFinite(period) || period <= 0d)
            return new(AnalyticalOrbitSamplingStatus.UnsupportedOrbit, 0, key);
        for (var index = 0; index < SegmentCount; index++)
        {
            var seconds = period * index / SegmentCount;
            SimulationInstant time;
            try { time = trajectory.Epoch + new SimulationDuration(SimulationInstant.FromSecondsRounded(seconds).Ticks); }
            catch (OverflowException) { return new(AnalyticalOrbitSamplingStatus.PeriodOutOfRange, 0, key); }
            var evaluation = UniversalVariableTwoBodyPropagator.TryEvaluate(state, trajectory.Epoch, time, mu);
            if (!evaluation.Succeeded) return new(AnalyticalOrbitSamplingStatus.PropagationFailed, index, key);
            if (!evaluation.State.Position.IsFinite) return new(AnalyticalOrbitSamplingStatus.NonFiniteSample, index, key);
            destination[index] = evaluation.State.Position;
        }
        // The exact copied closure is intentional; the final analytical period evaluation is not needed for geometry.
        destination[SegmentCount] = destination[0];
        return new(AnalyticalOrbitSamplingStatus.Success, VertexCount, key);
    }

    internal static ulong ComputeIdentityKey(in TwoBodyTrajectory trajectory, double centralMu)
    {
        var hash = TwoBodyTrajectoryIdentity.ComputeHash(trajectory);
        return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(centralMu));
    }

    private static ulong Mix(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; }
        return hash;
    }
}
