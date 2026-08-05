namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Controlled outcomes for direct authoritative celestial trajectory replacement.</summary>
internal enum CelestialTrajectoryTransactionStatus : byte
{
    Success = 0,
    InvalidEventKind,
    SubjectNotFound,
    RootBody,
    NoCurrentTrajectory,
    EventNotCanonical,
    EventTimeMismatch,
    ClockMismatch,
    TimelineRevisionMismatch,
    StateRevisionMismatch,
    TrajectoryBasisMismatch,
    ReplacementNoOp,
    ReplacementCentralMismatch,
    CentralBodyNotFound,
    InvalidCentralGravitationalParameter,
    UnsupportedModel,
    InvalidReplacementState,
    UnsupportedReplacementOrbit,
    CelestialStoreInconsistency,
    HistoryCapacityFailure,
    StateRevisionOverflow,
    TimelineRevisionOverflow,
}
