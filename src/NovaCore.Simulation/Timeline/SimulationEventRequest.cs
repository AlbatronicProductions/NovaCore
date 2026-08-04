using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>Payload-free request submitted to a timeline. The timeline assigns its canonical sequence.</summary>
public readonly record struct SimulationEventRequest(
    SimulationEventId Id,
    SimulationInstant Time,
    int Priority,
    SimulationEventKind Kind);
