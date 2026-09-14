using NovaCore.Simulation.Spacecraft.Resources;

namespace NovaCore.Simulation.Spacecraft.Actuation;

/// <summary>Positive rounded binary scale with an independent exponent; seconds need not fit a double.</summary>
internal readonly record struct PoweredBinaryScale(double Significand, int Exponent)
{
    internal bool IsZero => Significand == 0;
    internal double Value => Math.ScaleB(Significand, Exponent);
    internal PoweredBinaryScale DividePowerOfTwo(int shift) => new(Significand, Exponent - shift);

    // Form the complete weighted product/quotient before returning to binary64 range.
    internal double MultiplyDivide(double value, double divisor = 1)
    {
        if (value == 0 || IsZero) return 0;
        if (!double.IsFinite(value) || !double.IsFinite(divisor) || divisor <= 0) return double.NaN;
        var a = Math.ILogB(Math.Abs(value)); var b = Math.ILogB(divisor);
        return Math.ScaleB(Significand * Math.ScaleB(value, -a) / Math.ScaleB(divisor, -b), Exponent + a - b);
    }
}

/// <summary>Fixed workspace exact ratio rounding and exact dyadic mass-stage construction. No resource writes.</summary>
internal static class PoweredFlightNumerics
{
    // D*1e6 needs <=2196 bits; normalized remainder doubling needs <=2197.
    // N/D normalization aligns operands to at most that width. 35*64=2240 bits.
    private const int Words = 35;
    internal static bool TrySeconds(in PropellantDuration duration, out PoweredBinaryScale seconds)
        => TryRatio(duration.Numerator, duration.Denominator, PropellantInteger.TicksPerSecond, out seconds);

    internal static bool TryRatio(in PropellantInteger numerator, in PropellantInteger denominator,
        uint denominatorFactor, out PoweredBinaryScale result)
    {
        result = default;
        if (denominator.IsZero || denominatorFactor == 0) return false;
        if (numerator.IsZero) return true;
        Span<ulong> n = stackalloc ulong[Words]; Span<ulong> d = stackalloc ulong[Words];
        n.Clear(); d.Clear(); UInt128 carry = 0;
        for (var i = 0; i < PropellantInteger.LimbCount; i++)
        {
            n[i] = numerator.Limb(i);
            var product = (UInt128)denominator.Limb(i) * denominatorFactor + carry;
            d[i] = (ulong)product; carry = product >> 64;
        }
        d[Words - 1] = (ulong)carry;
        var exponent = Length(n) - Length(d);
        if (exponent >= 0) ShiftLeft(d, exponent); else ShiftLeft(n, -exponent);
        if (Compare(n, d) < 0) { ShiftLeft(n, 1); exponent--; }
        ulong bits = 0;
        for (var i = 0; i < 54; i++)
        {
            bits <<= 1;
            if (Compare(n, d) >= 0) { Subtract(n, d); bits |= 1; }
            if (i != 53) ShiftLeft(n, 1);
        }
        var significand = bits >> 1;
        if ((bits & 1) != 0 && (Length(n) != 0 || (significand & 1) != 0)) significand++;
        if (significand == 1UL << 53) { significand >>= 1; exponent++; }
        result = new(significand / 4503599627370496d, exponent);
        return true;
    }

    internal static bool TryStageMass(in PropellantInteger dry, in PropellantInteger successor,
        in PropellantInteger consumed, uint numerator, uint denominator, out double mass)
    {
        mass = 0;
        if (denominator == 0 || (denominator & (denominator - 1)) != 0 || numerator > denominator ||
            !PropellantInteger.TryAdd(dry, successor, out var baseMass) ||
            !PropellantInteger.TryMultiply(baseMass, denominator, out var baseNumerator) ||
            !PropellantInteger.TryMultiply(consumed, denominator - numerator, out var remainder) ||
            !PropellantInteger.TryAdd(baseNumerator, remainder, out var total) ||
            !total.TryToKilograms(out var weightedMass))
            return false;
        // The admitted dry-8 kg fixture and D<=256 keep both numbers normal.
        // Correctly rounded banked projection followed by exact power-of-two scaling.
        mass = Math.ScaleB(weightedMass, -System.Numerics.BitOperations.Log2(denominator));
        return double.IsFinite(mass) && mass > 0;
    }

    private static int Length(ReadOnlySpan<ulong> value)
    {
        for (var i = Words - 1; i >= 0; i--)
            if (value[i] != 0) return i * 64 + 64 - System.Numerics.BitOperations.LeadingZeroCount(value[i]);
        return 0;
    }
    private static int Compare(ReadOnlySpan<ulong> left, ReadOnlySpan<ulong> right)
    {
        for (var i = Words - 1; i >= 0; i--)
            if (left[i] != right[i]) return left[i] < right[i] ? -1 : 1;
        return 0;
    }
    private static void Subtract(Span<ulong> left, ReadOnlySpan<ulong> right)
    {
        ulong borrow = 0;
        for (var i = 0; i < Words; i++)
        {
            var sub = (UInt128)right[i] + borrow;
            var before = left[i]; left[i] = unchecked((ulong)((UInt128)before - sub));
            borrow = (UInt128)before < sub ? 1UL : 0;
        }
    }
    private static void ShiftLeft(Span<ulong> value, int shift)
    {
        var word = shift / 64; var bit = shift % 64;
        for (var i = Words - 1; i >= 0; i--)
        {
            var source = i - word;
            var v = source >= 0 ? value[source] << bit : 0;
            if (bit != 0 && source > 0) v |= value[source - 1] >> (64 - bit);
            value[i] = v;
        }
    }
}
