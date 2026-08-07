using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Pure deterministic secular rotation and mean-anomaly-rate correction around a universal-variable state.</summary>
internal static class AnalyticalKeplerSecularCorrectionEvaluator
{
    internal static bool TryScaleTime(SimulationInstant epoch, SimulationInstant requested, in AnalyticalKeplerSecularCorrection correction, out SimulationInstant scaled)
    {
        scaled = default;
        if (!correction.IsValid) return false;
        long elapsedTicks;
        try { elapsedTicks = (requested - epoch).Ticks; } catch (OverflowException) { return false; }
        var scaledTicks = elapsedTicks * correction.TimeScale;
        if (!double.IsFinite(scaledTicks) || scaledTicks < long.MinValue || scaledTicks > long.MaxValue) return false;
        try { scaled = epoch + new SimulationDuration(checked((long)Math.Round(scaledTicks, MidpointRounding.ToEven))); }
        catch (OverflowException) { return false; }
        return true;
    }

    internal static bool TryApply(in CartesianState propagated, in CartesianState seed, SimulationInstant epoch, SimulationInstant requested, in AnalyticalKeplerSecularCorrection correction, out CartesianState corrected)
        => TryApply(propagated, seed, epoch, requested, correction, AnalyticalKeplerPeriodicCorrection.Identity, out corrected);

    internal static bool TryApply(in CartesianState propagated, in CartesianState seed, SimulationInstant epoch, SimulationInstant requested, in AnalyticalKeplerSecularCorrection correction, AnalyticalKeplerPeriodicCorrection periodic, out CartesianState corrected)
    {
        corrected = default;
        if (!propagated.IsFinite || !seed.IsFinite || !correction.IsValid || periodic is null || !periodic.IsValid) return false;
        if (correction.IsIdentity && periodic.IsIdentity) { corrected = propagated; return true; }
        double seconds;
        try { seconds = (requested - epoch).Ticks / (double)SimulationInstant.TicksPerSecond; } catch (OverflowException) { return false; }
        var normal = Double3.Cross(seed.Position, seed.Velocity);
        var normalLength = Math.Sqrt(normal.LengthSquared);
        if (!double.IsFinite(normalLength) || normalLength <= 0d) return false;
        normal /= normalLength;

        double radialOffset = 0d, radialRate = 0d, phaseOffset = 0d, phaseRate = 0d;
        for (var index = 0; index < periodic.Count; index++)
        {
            var term = periodic.GetTerm(index);
            var angle = term.AngularFrequencyRadiansPerSecond * seconds;
            var sine = Math.Sin(angle);
            var cosine = Math.Cos(angle);
            radialOffset += term.RadialSineAmplitudeMetres * sine + term.RadialCosineAmplitudeMetres * (cosine - 1d);
            radialRate += term.AngularFrequencyRadiansPerSecond * (term.RadialSineAmplitudeMetres * cosine - term.RadialCosineAmplitudeMetres * sine);
            phaseOffset += term.PhaseSineAmplitudeRadians * sine + term.PhaseCosineAmplitudeRadians * (cosine - 1d);
            phaseRate += term.AngularFrequencyRadiansPerSecond * (term.PhaseSineAmplitudeRadians * cosine - term.PhaseCosineAmplitudeRadians * sine);
        }

        var periapsisAngle = correction.PeriapsisRateRadiansPerSecond * seconds + phaseOffset;
        var positionAfterPeriapsis = Rotate(propagated.Position, normal, periapsisAngle);
        var velocityAfterPeriapsis = Rotate(propagated.Velocity * correction.TimeScale, normal, periapsisAngle) +
            Double3.Cross(normal * (correction.PeriapsisRateRadiansPerSecond + phaseRate), positionAfterPeriapsis);

        var planeRate = Math.Sqrt(correction.ReferencePlaneAngularVelocity.LengthSquared);
        var position = positionAfterPeriapsis;
        var velocity = velocityAfterPeriapsis;
        if (planeRate > 0d)
        {
            var planeAxis = correction.ReferencePlaneAngularVelocity / planeRate;
            var planeAngle = planeRate * seconds;
            position = Rotate(positionAfterPeriapsis, planeAxis, planeAngle);
            velocity = Rotate(velocityAfterPeriapsis, planeAxis, planeAngle) + Double3.Cross(correction.ReferencePlaneAngularVelocity, position);
        }
        if (radialOffset != 0d || radialRate != 0d)
        {
            var radius = Math.Sqrt(position.LengthSquared);
            if (!double.IsFinite(radius) || radius <= 0d || !double.IsFinite(radius + radialOffset) || radius + radialOffset <= 0d) return false;
            var radial = position / radius;
            var radialVelocity = Double3.Dot(radial, velocity);
            var radialDerivative = (velocity - radial * radialVelocity) / radius;
            position += radial * radialOffset;
            velocity += radial * radialRate + radialDerivative * radialOffset;
        }
        corrected = new(position, velocity);
        return corrected.IsFinite;
    }

    private static Double3 Rotate(Double3 value, Double3 axis, double angle)
    {
        var sine = Math.Sin(angle); var cosine = Math.Cos(angle);
        return value * cosine + Double3.Cross(axis, value) * sine + axis * (Double3.Dot(axis, value) * (1d - cosine));
    }
}
