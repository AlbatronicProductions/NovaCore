namespace NovaCore.Simulation.Time;

/// <summary>UTC represented without a host calendar: 100-nanosecond ticks since 1970-01-01T00:00:00Z.</summary>
public readonly record struct UtcInstant(long TicksSinceUnixEpoch)
{
    public const long TicksPerSecond = 10_000_000;
}

/// <summary>
/// Pure UTC-to-J2000-ET conversion derived from the pinned NAIF0012 leap-second constants.
/// It never queries a host clock or loads a kernel; callers supply the UTC instant explicitly.
/// </summary>
public static class SolarUtcTime
{
    private const long J2000UtcNoonUnixSeconds = 946_728_000;
    private const double DeltaTaSeconds = 32.184d;
    private const double TdbK = 1.657e-3d;
    private const double TdbEb = 1.671e-2d;
    private const double MeanAnomalyAtJ2000 = 6.239996d;
    private const double MeanAnomalyRate = 1.99096871e-7d;

    // Effective UTC Unix seconds paired with TAI-UTC. Values are the complete
    // DELTET/DELTA_AT table in the pinned NAIF0012 LSK, ordered ascending.
    private static readonly (long EffectiveUnixSeconds, int TaiMinusUtcSeconds)[] LeapSeconds =
    [
        (63_072_000,10),(78_796_800,11),(94_694_400,12),(126_230_400,13),(157_766_400,14),(189_302_400,15),
        (220_924_800,16),(252_460_800,17),(283_996_800,18),(315_532_800,19),(362_793_600,20),(394_329_600,21),
        (425_865_600,22),(489_024_000,23),(567_993_600,24),(631_152_000,25),(662_688_000,26),(709_948_800,27),
        (741_484_800,28),(773_020_800,29),(820_454_400,30),(867_715_200,31),(915_148_800,32),(1_136_073_600,33),
        (1_230_768_000,34),(1_341_100_800,35),(1_435_708_800,36),(1_483_228_800,37)
    ];

    public static bool TryToSimulationInstant(UtcInstant utc, out SimulationInstant instant)
    {
        instant = default;
        var wholeSeconds = Math.DivRem(utc.TicksSinceUnixEpoch, UtcInstant.TicksPerSecond, out var subsecondTicks);
        if (subsecondTicks < 0) { wholeSeconds--; subsecondTicks += UtcInstant.TicksPerSecond; }
        if (!TryTaiMinusUtc(wholeSeconds, out var taiMinusUtc)) return false;

        var utcSecondsFromJ2000Noon = checked(wholeSeconds - J2000UtcNoonUnixSeconds) + subsecondTicks / (double)UtcInstant.TicksPerSecond;
        var et = utcSecondsFromJ2000Noon + taiMinusUtc + DeltaTaSeconds;
        // The LSK periodic term depends on ET. Three fixed iterations converge far
        // below NovaCore's one-microsecond SimulationInstant resolution.
        for (var iteration = 0; iteration < 3; iteration++)
        {
            var meanAnomaly = MeanAnomalyAtJ2000 + MeanAnomalyRate * et;
            var eccentricAnomaly = meanAnomaly + TdbEb * Math.Sin(meanAnomaly);
            et = utcSecondsFromJ2000Noon + taiMinusUtc + DeltaTaSeconds + TdbK * Math.Sin(eccentricAnomaly);
        }
        try { instant = SimulationInstant.FromSecondsRounded(et); return true; }
        catch (OverflowException) { return false; }
    }

    private static bool TryTaiMinusUtc(long unixSeconds, out int value)
    {
        value = 0;
        for (var index = LeapSeconds.Length - 1; index >= 0; index--)
        {
            if (unixSeconds < LeapSeconds[index].EffectiveUnixSeconds) continue;
            value = LeapSeconds[index].TaiMinusUtcSeconds;
            return true;
        }
        return false;
    }
}
