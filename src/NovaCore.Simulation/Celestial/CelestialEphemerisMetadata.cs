namespace NovaCore.Simulation.Celestial;

internal readonly record struct CelestialEphemerisSourceId(ulong Value) { internal bool IsValid => Value != 0; }
internal readonly record struct CelestialEphemerisVersionId(ulong Value) { internal bool IsValid => Value != 0; }
internal readonly record struct CelestialCoordinateFrameId(ulong Value) { internal bool IsValid => Value != 0; }
internal readonly record struct CelestialConstantsVersionId(ulong Value) { internal bool IsValid => Value != 0; }
internal readonly record struct CelestialContentHash(ulong High, ulong Low);

/// <summary>Opaque provenance and inclusive exact domain-tick coverage for one authored system.</summary>
internal readonly record struct CelestialEphemerisMetadata(
    CelestialEphemerisSourceId Source,
    CelestialEphemerisVersionId Version,
    CelestialTimeDomainId Domain,
    long SupportedStartDomainTicks,
    long SupportedEndDomainTicks,
    CelestialCoordinateFrameId CoordinateFrame,
    CelestialConstantsVersionId ConstantsVersion,
    CelestialContentHash ContentHash,
    CelestialContentHash AuthoredModificationHash)
{
    internal bool Contains(long domainTicks) => domainTicks >= SupportedStartDomainTicks && domainTicks <= SupportedEndDomainTicks;
}
