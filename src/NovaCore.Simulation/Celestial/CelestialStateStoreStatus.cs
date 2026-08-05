namespace NovaCore.Simulation.Celestial;

/// <summary>Controlled construction outcomes for a complete celestial catalog and initial state.</summary>
internal enum CelestialStateStoreStatus : byte
{
    Success = 0,
    StateCountMismatch,
    InvalidBodyId,
    DuplicateBodyId,
    InvalidInertialFrame,
    DuplicateInertialFrame,
    InvalidGravitationalParameter,
    SelfPrimaryBody,
    MissingPrimaryBody,
    PrimaryBodyCycle,
    StateDefinitionMismatch,
    RootTrajectoryNotAllowed,
    ChildTrajectoryRequired,
    InvalidTrajectoryCentralBody,
    TrajectoryPrimaryMismatch,
    InvalidTrajectoryModel,
    NonFiniteCartesianState,
    CapacityOverflow,
}
