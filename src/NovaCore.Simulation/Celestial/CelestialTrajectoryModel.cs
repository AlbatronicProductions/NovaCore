namespace NovaCore.Simulation.Celestial;

/// <summary>Authored trajectory-model selection. This contract does not evaluate or propagate a model.</summary>
internal enum CelestialTrajectoryModel : byte
{
    AnalyticalKepler = 0,
    FixedBody = 1,
    CircularOrbit = 2,
    SampledEphemeris = 3,
    ReservedNumericalNBody = 4,
}
