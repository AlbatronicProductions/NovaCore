using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Timeline;

/// <summary>Immutable scheduled-event ordering metadata. It deliberately contains no payload.</summary>
public readonly record struct SimulationEventHeader
{
    public SimulationEventId Id { get; }
    public SimulationInstant Time { get; }
    public int Priority { get; }
    public SimulationEventSequence Sequence { get; }
    public SimulationEventKind Kind { get; }

    public SimulationEventHeader(SimulationEventId id, SimulationInstant time, int priority, SimulationEventSequence sequence, SimulationEventKind kind)
    {
        if (!id.IsValid) throw new ArgumentOutOfRangeException(nameof(id));
        if (!sequence.IsAssigned) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (kind is not (SimulationEventKind.Marker or SimulationEventKind.ReplaceTrajectory)) throw new ArgumentOutOfRangeException(nameof(kind));
        Id = id; Time = time; Priority = priority; Sequence = sequence; Kind = kind;
    }
}
