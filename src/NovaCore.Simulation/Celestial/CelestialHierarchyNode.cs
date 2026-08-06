namespace NovaCore.Simulation.Celestial;

/// <summary>One immutable authored body node. Parentage and physical constants remain in <see cref="CelestialBodyDefinition"/>.</summary>
internal readonly record struct CelestialHierarchyNode(CelestialBodyDefinition Body, CelestialTrajectoryModel TrajectoryModel, TwoBodyTrajectory? Trajectory = null)
{
    public CelestialBodyId Id => Body.Id;
    public CelestialBodyId? ParentId => Body.PrimaryBody;
}
