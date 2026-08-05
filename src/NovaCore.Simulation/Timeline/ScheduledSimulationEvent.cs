namespace NovaCore.Simulation.Timeline;

/// <summary>Immutable pending record with a closed internal payload paired to its public header kind.</summary>
public readonly record struct ScheduledSimulationEvent
{
    public SimulationEventHeader Header { get; }
    internal SimulationEventPayload Payload { get; }
    internal ScheduledSimulationEvent(SimulationEventHeader header, SimulationEventPayload payload) { Header = header; Payload = payload; }
}
