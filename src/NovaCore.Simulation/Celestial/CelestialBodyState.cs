namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable current state record. Roots explicitly have no trajectory; children have exactly one trajectory.</summary>
internal readonly record struct CelestialBodyState(CelestialBodyId Id, TwoBodyTrajectory? Trajectory)
{
    public static CelestialBodyState Root(CelestialBodyId id) => new(id, null);
    public static CelestialBodyState Orbiting(CelestialBodyId id, TwoBodyTrajectory trajectory) => new(id, trajectory);
}
