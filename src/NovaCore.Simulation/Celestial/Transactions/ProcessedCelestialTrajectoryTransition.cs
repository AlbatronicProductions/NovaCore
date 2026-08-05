using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Compact immutable audit data for one successful authoritative trajectory replacement.</summary>
internal readonly record struct ProcessedCelestialTrajectoryTransition(
    CelestialBodyId Subject,
    SimulationInstant EventTime,
    SimulationInstant PriorTrajectoryEpoch,
    SimulationInstant ReplacementTrajectoryEpoch,
    StateRevision StateRevisionBefore,
    StateRevision StateRevisionAfter,
    ulong PriorTrajectoryHash,
    ulong ReplacementTrajectoryHash);
