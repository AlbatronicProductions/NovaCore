namespace NovaCore.Simulation.Timeline;

/// <summary>Payload-free architectural categories; payloads and transitions are deferred beyond Milestone 6A.</summary>
public enum SimulationEventKind : byte { Marker = 1, ReplaceTrajectory = 2, NoOpMarker = 3 }
