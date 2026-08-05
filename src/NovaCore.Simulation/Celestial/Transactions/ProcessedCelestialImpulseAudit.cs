using NovaCore.Core;

namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Immutable non-derivable scheduled intent data attached to one processed celestial transition.</summary>
internal readonly record struct ProcessedCelestialImpulseAudit(Double3 DeltaVelocity);
