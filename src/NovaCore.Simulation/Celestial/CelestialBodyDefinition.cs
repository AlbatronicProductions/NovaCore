using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable mechanical identity data. Gravitational parameter uses the same consistent simulation units as state vectors.</summary>
internal readonly record struct CelestialBodyDefinition(
    CelestialBodyId Id,
    CelestialBodyId? PrimaryBody,
    ReferenceFrameId InertialFrame,
    double GravitationalParameter);
