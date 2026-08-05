using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Versioned immutable Cartesian trajectory seed. It is authoritative state, not an evaluated cache.</summary>
internal readonly record struct TwoBodyTrajectory(
    CelestialBodyId CentralBody,
    SimulationInstant Epoch,
    CartesianState StateAtEpoch,
    TwoBodyPropagationModel Model);

/// <summary>Reserved model identity; Milestone 7A does not implement propagation.</summary>
internal enum TwoBodyPropagationModel : byte
{
    CartesianTwoBodyV1 = 1,
}
