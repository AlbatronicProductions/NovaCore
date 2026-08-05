namespace NovaCore.Simulation.Spacecraft;

internal enum SpacecraftStateStoreStatus : byte
{
    Success = 0,
    StateCountMismatch,
    InvalidSpacecraftId,
    DuplicateSpacecraftId,
    InvalidCarrierFrame,
    InvalidBodyFrame,
    DuplicateBodyFrame,
    CarrierEqualsBodyFrame,
    InvalidDiagnosticName,
    InvalidAttitudeState,
    CapacityOverflow,
}

internal enum SpacecraftStateStoreMutationStatus : byte
{
    Success = 0,
    SubjectNotFound,
    ExpectedAttitudeMismatch,
}
