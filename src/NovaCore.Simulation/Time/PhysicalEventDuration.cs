namespace NovaCore.Simulation.Time;

/// <summary>
/// Exact segment-relative duration for read-only event evaluation, bounded to the signed Int64 tick span.
/// Not public time, persistent authority or an execution interval. Convert only the local difference to FP64.
/// </summary>
internal readonly struct PhysicalEventDuration
{
    internal ulong WholeMagnitudeTicks { get; }
    internal ulong Numerator { get; }
    internal ulong Denominator { get; }
    internal bool IsNegative { get; }

    private PhysicalEventDuration(ulong whole, ulong numerator, ulong denominator, bool negative)
    { WholeMagnitudeTicks = whole; Numerator = numerator; Denominator = denominator; IsNegative = negative; }

    internal static bool TryDifference(PhysicalEventEpoch time, SimulationInstant origin, out PhysicalEventDuration duration)
    {
        duration = default;
        var floor = (Int128)time.FloorTicks - origin.Ticks;
        if (floor < long.MinValue || floor > long.MaxValue || (floor == long.MaxValue && time.Numerator != 0))
            return false;
        var negative = floor < 0;
        var numerator = time.Numerator;
        if (negative && numerator != 0)
        {
            floor += 1;
            numerator = time.Denominator - numerator;
        }
        duration = new((ulong)(negative ? -floor : floor), numerator, time.Denominator, negative);
        return true;
    }

    internal bool ExceedsMagnitude(long ticks) => WholeMagnitudeTicks > (ulong)ticks ||
        (WholeMagnitudeTicks == (ulong)ticks && Numerator != 0);

    internal double Seconds => (IsNegative ? -1d : 1d) *
        ((double)WholeMagnitudeTicks + (double)Numerator / Denominator) / SimulationInstant.TicksPerSecond;
}
