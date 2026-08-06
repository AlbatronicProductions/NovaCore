namespace NovaCore.Simulation.Celestial;

/// <summary>One immutable trajectory binding. Body identity, parentage, frames, and constants live only in the body catalog.</summary>
internal readonly record struct CelestialHierarchyNode(CelestialBodyId BodyId, CelestialEphemerisBinding Ephemeris)
{
    private readonly CelestialBodyDefinition? _legacyDefinition;
    public CelestialBodyId Id => BodyId;
    public CelestialTrajectoryModel TrajectoryModel => Ephemeris.Model;

    // Compatibility authoring input for retained focused tests. Published definitions extract this data into CelestialBodyCatalog.
    internal CelestialHierarchyNode(CelestialBodyDefinition body, CelestialTrajectoryModel model) : this(body.Id, new CelestialEphemerisBinding(model, new CelestialEphemerisSourceId((ulong)model + 1UL), 0)) { _legacyDefinition = body; }
    internal CelestialHierarchyNode(CelestialBodyDefinition body, CelestialEphemerisBinding ephemeris) : this(body.Id, ephemeris) { _legacyDefinition = body; }
    internal CelestialBodyDefinition? LegacyDefinition => _legacyDefinition;
    internal CelestialBodyId? ParentId => _legacyDefinition?.PrimaryBody;
}
