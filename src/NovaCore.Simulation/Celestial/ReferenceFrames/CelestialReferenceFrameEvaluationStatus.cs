namespace NovaCore.Simulation.Celestial.ReferenceFrames;

/// <summary>Controlled result of extracting immutable celestial authority into frame-local kinematics.</summary>
internal enum CelestialReferenceFrameEvaluationStatus : byte
{
    Success = 0,
    DestinationTooSmall,
    EmptySystem,
    MultipleGraphRoots,
    GraphCelestialCountMismatch,
    FrameMappingMismatch,
    RootMappingMismatch,
    PrimaryParentMismatch,
    RootTrajectoryPresent,
    ChildTrajectoryMissing,
    TrajectoryPrimaryMismatch,
    TrajectoryEvaluationFailed,
    NonFiniteEvaluatedState,
}
