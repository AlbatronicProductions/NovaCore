namespace NovaCore.Simulation.Celestial;

/// <summary>Internal store-only outcomes; revision and timeline ownership remain outside the store.</summary>
internal enum CelestialStateStoreMutationStatus : byte
{
    Success = 0,
    SubjectNotFound,
    RootBody,
    NoCurrentTrajectory,
    ExpectedTrajectoryMismatch,
}
