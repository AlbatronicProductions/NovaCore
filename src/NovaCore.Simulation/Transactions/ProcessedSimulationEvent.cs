using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial.Transactions;

namespace NovaCore.Simulation.Transactions;

/// <summary>Immutable append-only record of one successfully consumed pending event.</summary>
internal readonly record struct ProcessedSimulationEvent(
    SimulationEventHeader Event,
    SimulationInstant ExecutionTime,
    TimelineRevision TimelineRevisionBefore,
    TimelineRevision TimelineRevisionAfter,
    StateRevision StateRevisionBefore,
    StateRevision StateRevisionAfter,
    ProcessedCelestialTrajectoryTransition? CelestialTransition = null);
