namespace NovaCore.Simulation.Celestial;

/// <summary>One immutable authored body node. Model data is owned by its system catalog, never embedded here.</summary>
internal readonly record struct CelestialHierarchyNode(CelestialBodyDefinition Body, CelestialEphemerisBinding Ephemeris)
{
    public CelestialBodyId Id => Body.Id;
    public CelestialBodyId? ParentId => Body.PrimaryBody;
    public CelestialTrajectoryModel TrajectoryModel => Ephemeris.Model;

    // Compatibility construction helper for focused topology tests. Published definitions always retain only Ephemeris.
    internal CelestialHierarchyNode(CelestialBodyDefinition body, CelestialTrajectoryModel model) : this(body, new CelestialEphemerisBinding(model, new CelestialEphemerisSourceId((ulong)model + 1UL), 0)) { }
}
