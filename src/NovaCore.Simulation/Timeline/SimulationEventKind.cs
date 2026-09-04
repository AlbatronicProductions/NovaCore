namespace NovaCore.Simulation.Timeline;

/// <summary>Public event categories; scheduled records pair them with internal payloads, and transactional evaluators implement supported state transitions.</summary>
public enum SimulationEventKind : byte { Marker = 1, ReplaceTrajectory = 2, NoOpMarker = 3, CelestialImpulse = 4, RigidBodyTorque = 5 }
