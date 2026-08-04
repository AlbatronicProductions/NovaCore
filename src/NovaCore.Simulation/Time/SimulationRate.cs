namespace NovaCore.Simulation.Time;

/// <summary>Normalized positive rational simulation-rate multiplier. Pause is a future clock state, never a zero rate.</summary>
public readonly record struct SimulationRate
{
    public long Numerator { get; }
    public long Denominator { get; }
    public static SimulationRate Quarter => new(1, 4);
    public static SimulationRate Half => new(1, 2);
    public static SimulationRate One => new(1, 1);
    public static SimulationRate Two => new(2, 1);
    public static SimulationRate Five => new(5, 1);
    public static SimulationRate Ten => new(10, 1);
    public static SimulationRate Hundred => new(100, 1);

    public SimulationRate(long numerator, long denominator)
    {
        if (numerator <= 0) throw new ArgumentOutOfRangeException(nameof(numerator));
        if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
        var gcd = GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    /// <summary>
    /// Scales nonnegative host microticks. Remainder is numerator units modulo this rate's denominator and must be reset when the rate changes.
    /// </summary>
    public bool TryScale(long hostTicks, ref long remainder, out long simulationTicks)
    {
        simulationTicks = 0;
        if (hostTicks < 0 || remainder < 0 || remainder >= Denominator) return false;
        var scaled = (Int128)hostTicks * Numerator + remainder;
        var quotient = scaled / Denominator;
        if (quotient > long.MaxValue) return false;
        simulationTicks = (long)quotient;
        remainder = (long)(scaled % Denominator);
        return true;
    }

    public void ResetRemainder(ref long remainder) => remainder = 0;

    private static long GreatestCommonDivisor(long left, long right)
    {
        while (right != 0) { var next = left % right; left = right; right = next; }
        return left;
    }
}
