using System.Numerics;
using System.Runtime.CompilerServices;

namespace NovaCore.Simulation.Spacecraft.Resources;

/// <summary>
/// Bounded unsigned integer for the constant-flow resource model. 34*64=2176 bits.
/// Decoded binary64 needs <=2098 bits; mass units <=2118; flow*positive Int64 ticks <=2161.
/// No growing precision or floating arithmetic participates in availability.
/// </summary>
internal readonly struct PropellantInteger : IEquatable<PropellantInteger>
{
    internal const int LimbCount = 34;
    internal const uint TicksPerSecond = 1_000_000;
    [InlineArray(LimbCount)]
    private struct Limbs { private ulong _first; }
    private readonly Limbs _limbs;
    private PropellantInteger(in Limbs limbs) => _limbs = limbs;
    internal ulong Limb(int index) => _limbs[index];
    internal bool IsZero
    {
        get { for (var i = 0; i < LimbCount; i++) if (_limbs[i] != 0) return false; return true; }
    }
    internal int BitLength
    {
        get
        {
            for (var i = LimbCount - 1; i >= 0; i--)
                if (_limbs[i] != 0) return i * 64 + 64 - BitOperations.LeadingZeroCount(_limbs[i]);
            return 0;
        }
    }
    internal static PropellantInteger FromUInt64(ulong value)
    { Limbs limbs = default; limbs[0] = value; return new(limbs); }

    // x * 2^1074, decoded exactly. Signed zero is arithmetic zero.
    internal static bool TryDecodeFlow(double value, out PropellantInteger result)
    {
        result = default;
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var exponent = (int)((bits >> 52) & 2047);
        if (exponent == 2047 || ((bits >> 63) != 0 && (bits & 0x7FFF_FFFF_FFFF_FFFFUL) != 0)) return false;
        var significand = bits & 0x000F_FFFF_FFFF_FFFFUL;
        if (exponent != 0) significand |= 1UL << 52;
        var shift = exponent == 0 ? 0 : exponent - 1;
        var word = shift / 64; var offset = shift % 64;
        Limbs limbs = default;
        limbs[word] = significand << offset;
        if (offset != 0 && word + 1 < LimbCount) limbs[word + 1] = significand >> (64 - offset);
        result = new(limbs);
        return true;
    }
    // Q=1/(1,000,000*2^1074) kg. A mass and a flow use different integer scales.
    internal static bool TryFromKilograms(double value, out PropellantInteger result)
    {
        result = default;
        return TryDecodeFlow(value, out var decoded) && TryMultiply(decoded, TicksPerSecond, out result);
    }
    internal static int Compare(in PropellantInteger a, in PropellantInteger b)
    {
        for (var i = LimbCount - 1; i >= 0; i--)
            if (a._limbs[i] != b._limbs[i]) return a._limbs[i] < b._limbs[i] ? -1 : 1;
        return 0;
    }
    internal static bool TryMultiply(in PropellantInteger value, ulong factor, out PropellantInteger result)
    {
        Limbs limbs = default; UInt128 carry = 0;
        for (var i = 0; i < LimbCount; i++)
        {
            var product = (UInt128)value._limbs[i] * factor + carry;
            limbs[i] = (ulong)product; carry = product >> 64;
        }
        result = carry == 0 ? new(limbs) : default;
        return carry == 0;
    }
    internal static bool TryAdd(in PropellantInteger a, in PropellantInteger b, out PropellantInteger result)
    {
        Limbs limbs = default; UInt128 carry = 0;
        for (var i = 0; i < LimbCount; i++)
        {
            var sum = (UInt128)a._limbs[i] + b._limbs[i] + carry;
            limbs[i] = (ulong)sum; carry = sum >> 64;
        }
        result = carry == 0 ? new(limbs) : default;
        return carry == 0;
    }
    internal static bool TrySubtract(in PropellantInteger a, in PropellantInteger b, out PropellantInteger result)
    {
        Limbs limbs = default; ulong borrow = 0;
        for (var i = 0; i < LimbCount; i++)
        {
            var subtrahend = (UInt128)b._limbs[i] + borrow;
            limbs[i] = unchecked((ulong)((UInt128)a._limbs[i] - subtrahend));
            borrow = (UInt128)a._limbs[i] < subtrahend ? 1UL : 0;
        }
        result = borrow == 0 ? new(limbs) : default;
        return borrow == 0;
    }
    private PropellantInteger Divide(uint divisor, out uint remainder)
    {
        Limbs limbs = default; UInt128 carry = 0;
        for (var i = LimbCount - 1; i >= 0; i--)
        {
            var dividend = (carry << 64) | _limbs[i];
            limbs[i] = (ulong)(dividend / divisor); carry = dividend % divisor;
        }
        remainder = (uint)carry; return new(limbs);
    }
    private ulong ShiftedLow64(int shift)
    {
        var word = shift / 64; var offset = shift % 64;
        var result = _limbs[word] >> offset;
        if (offset != 0 && word + 1 < LimbCount) result |= _limbs[word + 1] << (64 - offset);
        return result;
    }
    private bool AnyBitsBelow(int count)
    {
        var words = count / 64; var bits = count % 64;
        for (var i = 0; i < words; i++) if (_limbs[i] != 0) return true;
        return bits != 0 && (_limbs[words] & ((1UL << bits) - 1)) != 0;
    }
    /// <summary>Correctly rounded ties-to-even observation of exact units*Q. Never resource authority.</summary>
    internal bool TryToKilograms(out double kilograms)
    {
        kilograms = 0;
        var whole = Divide(TicksPerSecond, out var remainder);
        var shift = Math.Max(0, whole.BitLength - 53);
        var significand = whole.ShiftedLow64(shift);
        bool roundUp;
        if (shift == 0)
            roundUp = remainder > TicksPerSecond / 2 ||
                (remainder == TicksPerSecond / 2 && (significand & 1) != 0);
        else
        {
            var halfway = (whole.ShiftedLow64(shift - 1) & 1) != 0;
            roundUp = halfway && (whole.AnyBitsBelow(shift - 1) || remainder != 0 || (significand & 1) != 0);
        }
        if (roundUp) significand++;
        if (significand == 1UL << 53) { significand >>= 1; shift++; }
        if (significand < 1UL << 52)
        { kilograms = BitConverter.UInt64BitsToDouble(significand); return true; }
        var exponent = shift + 1;
        if (exponent >= 2047) return false;
        kilograms = BitConverter.UInt64BitsToDouble(((ulong)exponent << 52) | (significand & 0x000F_FFFF_FFFF_FFFFUL));
        return true;
    }
    public bool Equals(PropellantInteger other) => Compare(this, other) == 0;
    public override bool Equals(object? obj) => obj is PropellantInteger other && Equals(other);
    public override int GetHashCode()
    {
        var hash = 2166136261U;
        for (var i = 0; i < LimbCount; i++) hash = unchecked((hash ^ (uint)_limbs[i] ^ (uint)(_limbs[i] >> 32)) * 16777619U);
        return unchecked((int)hash);
    }
    public static bool operator ==(PropellantInteger a, PropellantInteger b) => a.Equals(b);
    public static bool operator !=(PropellantInteger a, PropellantInteger b) => !a.Equals(b);
}

/// <summary>Interval-local ratio in ticks, never a public clock or a spending capability.</summary>
internal readonly record struct PropellantDuration(PropellantInteger Numerator, PropellantInteger Denominator)
{
    internal bool IsValid => !Denominator.IsZero;
    internal bool IsZero => IsValid && Numerator.IsZero;
    internal bool IsWithin(long ticks) => ticks >= 0 && IsValid &&
        PropellantInteger.TryMultiply(Denominator, (ulong)ticks, out var end) &&
        PropellantInteger.Compare(Numerator, end) <= 0;
}
