using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Resources;

namespace NovaCore.Simulation.Spacecraft.Actuation;

/// <summary>Copied numerical transport only. It grants no resource, interval or native-step authority.</summary>
internal readonly record struct OrdinaryContactInput(Vector3 LinearAcceleration, Vector3 AngularAcceleration,
    float InverseMass, float Duration);

/// <summary>Cold schedule-invariant numerical facts. Not a source, proposal or step capability.</summary>
internal readonly struct OrdinaryContactProjectionSchedule
{
    private readonly PoweredBinaryScale shortDuration, longDuration, zeroDuty;
    private OrdinaryContactProjectionSchedule(PoweredBinaryScale shortDuration, PoweredBinaryScale longDuration,
        PoweredBinaryScale zeroDuty)
    { this.shortDuration = shortDuration; this.longDuration = longDuration; this.zeroDuty = zeroDuty; }

    internal static bool TryPrepare(out OrdinaryContactProjectionSchedule result)
    {
        result = default;
        var one = PropellantInteger.FromUInt64(1);
        if (!PoweredFlightNumerics.TryRatio(PropellantInteger.FromUInt64(16666), one, PropellantInteger.TicksPerSecond, out var shorter) ||
            !PoweredFlightNumerics.TryRatio(PropellantInteger.FromUInt64(16667), one, PropellantInteger.TicksPerSecond, out var longer) ||
            !PoweredFlightNumerics.TryRatio(default, one, 1, out var zero)) return false;
        result = new(shorter, longer, zero); return true;
    }

    internal bool TryMap(in PropellantDuration powered, uint ticks, Double3 forceLocal, Double3 momentLocal,
        double sourceMass, double sphericalDryInertia, Double3 gravityLocal, out OrdinaryContactInput input)
    {
        input = default;
        if (ticks is not (16666 or 16667) ||
            !OrdinaryContactInputProjection.Validate(powered, ticks, 1, forceLocal, momentLocal, sourceMass,
                sphericalDryInertia, gravityLocal, out var numerator)) return false;
        var duty = zeroDuty;
        if (!numerator.IsZero && !PoweredFlightNumerics.TryRatio(numerator, powered.Denominator, ticks, out duty)) return false;
        return OrdinaryContactInputProjection.Finish(duty, ticks == 16666 ? shortDuration : longDuration,
            forceLocal, momentLocal, sourceMass, sphericalDryInertia, gravityLocal, out input);
    }
}

/// <summary>Exact powered fraction, fixed-workspace ratio, then complete weighted products before FP32 transport.</summary>
internal static class OrdinaryContactInputProjection
{
    // H is an ordinary duration in rational canonical ticks. Production passes its admitted integer
    // interval/1; the literal-1/60 diagnostic replay uses 1,000,000/60. This is not a step scheduler.
    internal static bool TryMap(in PropellantDuration powered, uint ordinaryTickNumerator,
        uint ordinaryTickDenominator, Double3 forceLocal, Double3 momentLocal, double sourceMass,
        double sphericalDryInertia, Double3 gravityLocal, out OrdinaryContactInput input)
    {
        input = default;
        if (!Validate(powered, ordinaryTickNumerator, ordinaryTickDenominator, forceLocal, momentLocal,
            sourceMass, sphericalDryInertia, gravityLocal, out var numerator) ||
            !PoweredFlightNumerics.TryRatio(numerator, powered.Denominator, ordinaryTickNumerator, out var duty)) return false;
        var hn = PropellantInteger.FromUInt64(ordinaryTickNumerator);
        var hd = PropellantInteger.FromUInt64(ordinaryTickDenominator);
        if (!PoweredFlightNumerics.TryRatio(hn, hd, PropellantInteger.TicksPerSecond, out var duration)) return false;
        return Finish(duty, duration, forceLocal, momentLocal, sourceMass, sphericalDryInertia, gravityLocal, out input);
    }

    internal static bool Validate(in PropellantDuration powered, uint ordinaryTickNumerator, uint ordinaryTickDenominator,
        Double3 forceLocal, Double3 momentLocal, double sourceMass, double sphericalDryInertia, Double3 gravityLocal,
        out PropellantInteger numerator)
    {
        numerator = default;
        return !(ordinaryTickNumerator == 0 || ordinaryTickDenominator == 0 || powered.Denominator.IsZero ||
            !forceLocal.IsFinite || !momentLocal.IsFinite || !gravityLocal.IsFinite ||
            !double.IsFinite(sourceMass) || sourceMass <= 0 ||
            !double.IsFinite(sphericalDryInertia) || sphericalDryInertia <= 0 ||
            !PropellantInteger.TryMultiply(powered.Numerator, ordinaryTickDenominator, out numerator) ||
            !PropellantInteger.TryMultiply(powered.Denominator, ordinaryTickNumerator, out var denominator) ||
            PropellantInteger.Compare(numerator, denominator) > 0);
    }

    internal static bool Finish(PoweredBinaryScale duty, PoweredBinaryScale duration, Double3 forceLocal,
        Double3 momentLocal, double sourceMass, double sphericalDryInertia, Double3 gravityLocal, out OrdinaryContactInput input)
    {
        input = default;
        if (duty.Exponent > 0 || (duty.Exponent == 0 && duty.Significand > 1)) return false;
        var linear = new Vector3(
            (float)(duty.MultiplyDivide(forceLocal.X, sourceMass) + gravityLocal.X),
            (float)(duty.MultiplyDivide(forceLocal.Y, sourceMass) + gravityLocal.Y),
            (float)(duty.MultiplyDivide(forceLocal.Z, sourceMass) + gravityLocal.Z));
        var angular = new Vector3(
            (float)duty.MultiplyDivide(momentLocal.X, sphericalDryInertia),
            (float)duty.MultiplyDivide(momentLocal.Y, sphericalDryInertia),
            (float)duty.MultiplyDivide(momentLocal.Z, sphericalDryInertia));
        var inverseMass = (float)(1 / sourceMass);
        var dt = (float)duration.Value;
        if (!Finite(linear) || !Finite(angular) || !float.IsFinite(inverseMass) || inverseMass <= 0 ||
            !float.IsFinite(dt) || dt <= 0) return false;
        input = new(linear, angular, inverseMass, dt);
        return true;
    }

    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
