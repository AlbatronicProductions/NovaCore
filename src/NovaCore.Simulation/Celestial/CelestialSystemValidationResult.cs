namespace NovaCore.Simulation.Celestial;

/// <summary>Controlled result of validating one authored celestial-system definition.</summary>
internal readonly record struct CelestialSystemValidationResult(CelestialSystemValidationStatus Status, int RootIndex = -1)
{
    public bool Succeeded => Status == CelestialSystemValidationStatus.Success;
}

internal enum CelestialSystemValidationStatus : byte
{
    Success = 0,
    InvalidSystemId,
    EmptySystem,
    InvalidBodyId,
    DuplicateBodyId,
    InvalidInertialFrame,
    InvalidGravitationalParameter,
    InvalidTrajectoryModel,
    RootModelInvalid,
    MissingParent,
    SelfParent,
    MultipleRoots,
    ParentCycle,
    CapacityOverflow,
    InvalidTimeDomain,
    InvalidDomainTickRate,
    InvalidMappingScaleNumerator,
    InvalidMappingScaleDenominator,
    InvalidEphemerisSource,
    InvalidEphemerisVersion,
    InvalidCoordinateFrame,
    InvalidConstantsVersion,
    InvalidSupportedInterval,
    MappingMetadataDomainMismatch,
    InvalidEphemerisBinding,
    MissingEphemerisSource,
    DuplicateEphemerisSourceId,
    NegativePayloadIndex,
    PayloadIndexOutOfRange,
    ModelCatalogMismatch,
    SourceModelIncompatible,
    SourceSystemTimeDomainMismatch,
    InvalidFixedBodyPayload,
    InvalidCircularOrbitPayload,
    InvalidAnalyticalKeplerPayload,
    UnusedEphemerisPayload,
    UnsupportedReservedTrajectoryModel,
}
