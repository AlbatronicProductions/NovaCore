using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

internal enum SampledEphemerisInterpolationModel : byte { CubicHermitePositionVelocityV1 = 1 }
internal readonly record struct CelestialEphemerisSample(long DomainTick, Double3 Position, Double3 Velocity)
{ internal bool IsFinite => Position.IsFinite && Velocity.IsFinite; }
internal readonly record struct SampledEphemerisPayload(CelestialTimeDomainId Domain, int FirstSampleIndex, int SampleCount, SampledEphemerisInterpolationModel InterpolationModel, long SupportedStartDomainTick, long SupportedEndDomainTick);

internal enum SampledEphemerisEvaluationStatus : byte { Success = 0, BeforeCoverage, AfterCoverage, ExactComparisonOverflow, IntervalLookupFailure, InterpolationArithmeticFailure, NonFiniteResult }
internal readonly record struct SampledEphemerisEvaluationResult(SampledEphemerisEvaluationStatus Status, CartesianState State)
{ internal bool Succeeded => Status == SampledEphemerisEvaluationStatus.Success; }

/// <summary>Pure exact-time bracketing and cubic Hermite evaluation over one flat sample range.</summary>
internal static class SampledEphemerisEvaluator
{
    internal static SampledEphemerisEvaluationResult TryEvaluate(ReadOnlySpan<CelestialEphemerisSample> samples, in SampledEphemerisPayload payload, in CelestialTimeArgument requested, long domainTicksPerSecond)
    {
        if (requested.Domain != payload.Domain) return new(SampledEphemerisEvaluationStatus.IntervalLookupFailure, default);
        if (Compare(requested, payload.SupportedStartDomainTick) < 0) return new(SampledEphemerisEvaluationStatus.BeforeCoverage, default);
        if (Compare(requested, payload.SupportedEndDomainTick) > 0) return new(SampledEphemerisEvaluationStatus.AfterCoverage, default);
        var first = payload.FirstSampleIndex; var count = payload.SampleCount;
        if (first < 0 || count < 2 || first > samples.Length - count) return new(SampledEphemerisEvaluationStatus.IntervalLookupFailure, default);
        var low = 0; var high = count - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) >> 1); var comparison = Compare(requested, samples[first + mid].DomainTick);
            if (comparison == 0) return new(SampledEphemerisEvaluationStatus.Success, new(samples[first + mid].Position, samples[first + mid].Velocity));
            if (comparison < 0) high = mid - 1; else low = mid + 1;
        }
        if (high < 0) return new(SampledEphemerisEvaluationStatus.BeforeCoverage, default);
        if (low >= count) return new(SampledEphemerisEvaluationStatus.AfterCoverage, default);
        ref readonly var left = ref samples[first + high]; ref readonly var right = ref samples[first + low];
        if (!TryParameter(requested, left.DomainTick, right.DomainTick, domainTicksPerSecond, out var u, out var seconds)) return new(SampledEphemerisEvaluationStatus.InterpolationArithmeticFailure, default);
        return TryHermite(left, right, u, seconds, out var state) ? new(SampledEphemerisEvaluationStatus.Success, state) : new(SampledEphemerisEvaluationStatus.NonFiniteResult, default);
    }

    private static int Compare(in CelestialTimeArgument value, long tick) => value.WholeDomainTicks < tick ? -1 : value.WholeDomainTicks > tick ? 1 : value.RemainderNumerator == 0 ? 0 : 1;
    private static bool TryParameter(in CelestialTimeArgument value, long start, long end, long ticksPerSecond, out double u, out double intervalSeconds)
    {
        u = intervalSeconds = double.NaN;
        try { var denominator = checked((Int128)end - start); if (denominator <= 0 || ticksPerSecond <= 0) return false; var whole = checked((Int128)value.WholeDomainTicks - start); var numerator = checked(whole * value.RemainderDenominator + value.RemainderNumerator); var full = checked(denominator * value.RemainderDenominator); u = (double)numerator / (double)full; intervalSeconds = (double)denominator / ticksPerSecond; return double.IsFinite(u) && double.IsFinite(intervalSeconds) && u > 0d && u < 1d; }
        catch (OverflowException) { return false; }
    }
    private static bool TryHermite(in CelestialEphemerisSample a, in CelestialEphemerisSample b, double u, double intervalTicks, out CartesianState state)
    {
        state = default; var u2 = u * u; var u3 = u2 * u; var h00 = 2d * u3 - 3d * u2 + 1d; var h10 = u3 - 2d * u2 + u; var h01 = -2d * u3 + 3d * u2; var h11 = u3 - u2;
        var position = a.Position * h00 + a.Velocity * (h10 * intervalTicks) + b.Position * h01 + b.Velocity * (h11 * intervalTicks);
        var dh00 = 6d * u2 - 6d * u; var dh10 = 3d * u2 - 4d * u + 1d; var dh01 = -6d * u2 + 6d * u; var dh11 = 3d * u2 - 2d * u;
        var velocity = (a.Position * dh00 + a.Velocity * (dh10 * intervalTicks) + b.Position * dh01 + b.Velocity * (dh11 * intervalTicks)) / intervalTicks;
        if (!position.IsFinite || !velocity.IsFinite) return false; state = new(position, velocity); return true;
    }
}
