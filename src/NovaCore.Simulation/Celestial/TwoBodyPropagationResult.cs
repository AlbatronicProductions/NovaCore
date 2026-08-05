using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable result of pure propagation. State is meaningful only when status is <see cref="TwoBodyPropagationStatus.Success"/>.</summary>
internal readonly record struct TwoBodyPropagationResult(
    TwoBodyPropagationStatus Status,
    SimulationInstant RequestedTime,
    CartesianState State,
    int Iterations)
{
    public bool Succeeded => Status == TwoBodyPropagationStatus.Success;
}
