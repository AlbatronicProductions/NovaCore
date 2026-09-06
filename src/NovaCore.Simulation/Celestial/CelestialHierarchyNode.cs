namespace NovaCore.Simulation.Celestial;

/// <summary>One immutable trajectory binding. Body identity, parentage, frames, and constants live only in the body catalog.</summary>
internal readonly record struct CelestialHierarchyNode(CelestialBodyId BodyId, CelestialEphemerisBinding Ephemeris)
{
    public CelestialBodyId Id => BodyId;
    public CelestialTrajectoryModel TrajectoryModel => Ephemeris.Model;
}
