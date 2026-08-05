using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>Public header request with an internal closed payload used only by approved simulation-domain event kinds.</summary>
public readonly record struct SimulationEventRequest
{
    public SimulationEventId Id { get; }
    public SimulationInstant Time { get; }
    public int Priority { get; }
    public SimulationEventKind Kind { get; }
    internal SimulationEventPayload Payload { get; }

    public SimulationEventRequest(SimulationEventId id, SimulationInstant time, int priority, SimulationEventKind kind) : this(id, time, priority, kind, SimulationEventPayload.None) { }
    internal SimulationEventRequest(SimulationEventId id, SimulationInstant time, int priority, SimulationEventKind kind, SimulationEventPayload payload) { Id = id; Time = time; Priority = priority; Kind = kind; Payload = payload; }

    internal static bool TryCreateCelestialImpulse(SimulationEventId id, SimulationInstant time, int priority, Celestial.CelestialBodyId subject, Core.Double3 deltaVelocity, out SimulationEventRequest request)
    {
        if (!SimulationEventPayload.TryCreateCelestialImpulse(subject, deltaVelocity, out var payload)) { request = default; return false; }
        request = new(id, time, priority, SimulationEventKind.CelestialImpulse, payload);
        return true;
    }
    internal static bool TryCreateRigidBodyTorque(SimulationEventId id, SimulationInstant time, int priority, Spacecraft.SpacecraftId subject, out SimulationEventRequest request)
    { if (!SimulationEventPayload.TryCreateRigidBodyTorque(subject, out var payload)) { request = default; return false; } request = new(id, time, priority, SimulationEventKind.RigidBodyTorque, payload); return true; }
}
