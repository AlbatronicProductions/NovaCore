using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

internal readonly record struct CelestialTimeDomainId(ulong Value)
{
    internal static CelestialTimeDomainId Invalid => new(0);
    internal bool IsValid => Value != 0;
}

internal readonly record struct CelestialEpoch(CelestialTimeDomainId Domain, long DomainTicks, long DomainTicksPerSecond);

internal readonly record struct CelestialTimeArgument(CelestialTimeDomainId Domain, long WholeDomainTicks, Int128 RemainderNumerator, Int128 RemainderDenominator)
{
    internal bool IsExact => RemainderNumerator == 0;

    /// <summary>Converts one exact domain argument into the existing solver's microtick coordinate without consulting host time.</summary>
    internal bool TryToSimulationInstant(long domainTicksPerSecond, out SimulationInstant instant)
    {
        instant = default;
        if (!Domain.IsValid || domainTicksPerSecond <= 0 || RemainderDenominator <= 0 || RemainderNumerator < 0 || RemainderNumerator >= RemainderDenominator) return false;
        var seconds = WholeDomainTicks / (double)domainTicksPerSecond + (double)RemainderNumerator / (double)RemainderDenominator / domainTicksPerSecond;
        if (!double.IsFinite(seconds)) return false;
        try { instant = SimulationInstant.FromSecondsRounded(seconds); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
        catch (OverflowException) { return false; }
    }
}

internal readonly record struct CelestialSystemTimeMapping(SimulationInstant SimulationAnchor, CelestialEpoch DomainAnchor, long ScaleNumerator, long ScaleDenominator)
{
    internal static CelestialSystemTimeMapping Identity(CelestialTimeDomainId domain) => new(SimulationInstant.Zero, new(domain, 0, SimulationInstant.TicksPerSecond), 1, 1);

    internal CelestialSystemTimeMappingStatus TryMap(SimulationInstant requested, out CelestialTimeArgument argument)
    {
        argument = default;
        if (!DomainAnchor.Domain.IsValid) return CelestialSystemTimeMappingStatus.InvalidTimeDomain;
        if (DomainAnchor.DomainTicksPerSecond <= 0) return CelestialSystemTimeMappingStatus.InvalidDomainTickRate;
        if (ScaleNumerator <= 0) return CelestialSystemTimeMappingStatus.InvalidScaleNumerator;
        if (ScaleDenominator <= 0) return CelestialSystemTimeMappingStatus.InvalidScaleDenominator;
        try
        {
            var delta = checked(requested.Ticks - SimulationAnchor.Ticks);
            var denominator = checked((Int128)ScaleDenominator * SimulationInstant.TicksPerSecond);
            var numerator = checked(checked((Int128)delta * ScaleNumerator) * DomainAnchor.DomainTicksPerSecond);
            var quotient = numerator / denominator; var remainder = numerator % denominator;
            // Euclidean canonicalization: denominator is positive and remainder is always [0, denominator).
            if (remainder < 0) { quotient = checked(quotient - 1); remainder += denominator; }
            var whole = checked((Int128)DomainAnchor.DomainTicks + quotient);
            if (whole < long.MinValue || whole > long.MaxValue) return CelestialSystemTimeMappingStatus.MappedTimeOverflow;
            argument = new(DomainAnchor.Domain, (long)whole, remainder, denominator);
            return CelestialSystemTimeMappingStatus.Success;
        }
        catch (OverflowException) { return CelestialSystemTimeMappingStatus.ArithmeticOverflow; }
    }
}

internal enum CelestialSystemTimeMappingStatus : byte
{
    Success = 0,
    InvalidTimeDomain,
    InvalidDomainTickRate,
    InvalidScaleNumerator,
    InvalidScaleDenominator,
    ArithmeticOverflow,
    MappedTimeOverflow,
    OutsideSupportedInterval,
}
