namespace NovaCore.Simulation.Timeline;

/// <summary>Immutable pending-event record. Event payloads are deliberately deferred beyond Milestone 6B-1.</summary>
public readonly record struct ScheduledSimulationEvent(SimulationEventHeader Header);
