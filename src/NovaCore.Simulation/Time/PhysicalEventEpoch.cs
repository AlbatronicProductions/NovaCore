using System.Buffers.Binary;

namespace NovaCore.Simulation.Time;

internal enum PhysicalEventEpochStatus : byte
{
    Success, InvalidDenominator, DenominatorCapacityExceeded, TickOverflow,
    InvalidEncoding, UnsupportedVersion, UnsupportedKind,
}

/// <summary>
/// Internal rational physical-event identity, not a fractional public clock or an execution epoch.
/// Canonical floor is Int64; the reduced fraction uses UInt64. Default is canonical zero.
/// No floating constructor, rounded public conversion, state evaluation or root certification exists here.
/// </summary>
internal readonly struct PhysicalEventEpoch : IEquatable<PhysicalEventEpoch>, IComparable<PhysicalEventEpoch>
{
    internal const int SerializedSize = 32;
    private const uint Magic = 0x50454850; // PHEP, little endian.
    private const byte Version = 1;
    private readonly ulong _denominatorMinusOne;

    internal long FloorTicks { get; }
    internal ulong Numerator { get; }
    internal ulong Denominator => checked(_denominatorMinusOne + 1);
    internal bool IsCanonical => Numerator == 0;

    private PhysicalEventEpoch(long floorTicks, ulong numerator, ulong denominator)
    {
        FloorTicks = floorTicks;
        Numerator = numerator;
        _denominatorMinusOne = checked(denominator - 1);
    }

    internal static PhysicalEventEpoch FromCanonical(SimulationInstant time) => new(time.Ticks, 0, 1);

    /// <summary>
    /// Normalize ticks + numerator/denominator using Euclidean floor division, before adding ticks.
    /// Int128 inputs allow improper fractions and signed borrow without negating Int64.MinValue.
    /// Denominator capacity is checked before normalization. Failure returns canonical zero, not a usable result.
    /// </summary>
    internal static PhysicalEventEpochStatus TryCreate(long ticks, Int128 numerator, Int128 denominator,
        out PhysicalEventEpoch epoch)
    {
        epoch = default;
        if (denominator <= 0) return PhysicalEventEpochStatus.InvalidDenominator;
        if (denominator > ulong.MaxValue) return PhysicalEventEpochStatus.DenominatorCapacityExceeded;
        var whole = numerator / denominator;
        var remainder = numerator % denominator;
        try
        {
            if (remainder < 0)
            {
                whole = checked(whole - 1);
                remainder = checked(remainder + denominator);
            }
            var floor = checked((Int128)ticks + whole);
            if (floor < long.MinValue || floor > long.MaxValue) return PhysicalEventEpochStatus.TickOverflow;
            if (remainder == 0)
            {
                epoch = FromCanonical(new((long)floor));
                return PhysicalEventEpochStatus.Success;
            }
            var n = checked((ulong)remainder);
            var d = checked((ulong)denominator);
            var divisor = GreatestCommonDivisor(n, d);
            epoch = new((long)floor, n / divisor, d / divisor);
            return PhysicalEventEpochStatus.Success;
        }
        catch (OverflowException) { return PhysicalEventEpochStatus.TickOverflow; }
    }

    internal bool TryGetCanonicalInstant(out SimulationInstant time)
    {
        time = IsCanonical ? new(FloorTicks) : default;
        return IsCanonical;
    }

    public int CompareTo(PhysicalEventEpoch other)
    {
        var floor = FloorTicks.CompareTo(other.FloorTicks);
        if (floor != 0) return floor;
        // Both factors are at most UInt64.MaxValue. Their product is strictly below 2^128.
        var left = checked((UInt128)Numerator * other.Denominator);
        var right = checked((UInt128)other.Numerator * Denominator);
        return left.CompareTo(right);
    }

    public bool Equals(PhysicalEventEpoch other) => FloorTicks == other.FloorTicks &&
        Numerator == other.Numerator && _denominatorMinusOne == other._denominatorMinusOne;
    public override bool Equals(object? obj) => obj is PhysicalEventEpoch other && Equals(other);
    // Stable value hash; serialized bytes, not this lossy hash, carry persistent identity.
    public override int GetHashCode() => unchecked((int)FloorTicks ^ (int)(FloorTicks >> 32) ^
        (int)Numerator ^ (int)(Numerator >> 32) ^ (int)_denominatorMinusOne ^ (int)(_denominatorMinusOne >> 32));
    public static bool operator ==(PhysicalEventEpoch left, PhysicalEventEpoch right) => left.Equals(right);
    public static bool operator !=(PhysicalEventEpoch left, PhysicalEventEpoch right) => !left.Equals(right);

    /// <summary>Writes exactly the first 32 bytes. A short destination is unchanged.</summary>
    internal bool TryWrite(Span<byte> destination)
    {
        if (destination.Length < SerializedSize) return false;
        BinaryPrimitives.WriteUInt32LittleEndian(destination, Magic);
        destination[4] = Version;
        destination[5] = IsCanonical ? (byte)0 : (byte)1;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[6..], 0);
        BinaryPrimitives.WriteInt64LittleEndian(destination[8..], FloorTicks);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], Numerator);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], Denominator);
        return true;
    }

    /// <summary>
    /// Reads one exact-size canonical encoding. Non-reduced encodings are rejected, not normalized.
    /// Kinds other than canonical(0)/rational(1), including future certified roots, are unsupported.
    /// A future certificate needs its own typed payload/comparison contract; these fractions cannot carry it.
    /// </summary>
    internal static PhysicalEventEpochStatus TryRead(ReadOnlySpan<byte> source, out PhysicalEventEpoch epoch)
    {
        epoch = default;
        if (source.Length != SerializedSize || BinaryPrimitives.ReadUInt32LittleEndian(source) != Magic)
            return PhysicalEventEpochStatus.InvalidEncoding;
        if (source[4] != Version) return PhysicalEventEpochStatus.UnsupportedVersion;
        var kind = source[5];
        if (kind > 1) return PhysicalEventEpochStatus.UnsupportedKind;
        if (BinaryPrimitives.ReadUInt16LittleEndian(source[6..]) != 0) return PhysicalEventEpochStatus.InvalidEncoding;
        var floor = BinaryPrimitives.ReadInt64LittleEndian(source[8..]);
        var numerator = BinaryPrimitives.ReadUInt64LittleEndian(source[16..]);
        var denominator = BinaryPrimitives.ReadUInt64LittleEndian(source[24..]);
        if (denominator == 0 || numerator >= denominator ||
            (kind == 0 ? numerator != 0 || denominator != 1 : numerator == 0) ||
            GreatestCommonDivisor(numerator, denominator) != 1)
            return PhysicalEventEpochStatus.InvalidEncoding;
        epoch = new(floor, numerator, denominator);
        return PhysicalEventEpochStatus.Success;
    }

    private static ulong GreatestCommonDivisor(ulong left, ulong right)
    {
        while (right != 0) { var remainder = left % right; left = right; right = remainder; }
        return left;
    }
}
