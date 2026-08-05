using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial.Transactions;

namespace NovaCore.Simulation.Transactions;

internal enum SimulationTransactionStatus : byte { Committed = 0, ValidationFailed }

internal readonly record struct SimulationTransactionResult(
    SimulationTransactionStatus Status,
    SimulationTransactionValidationResult Validation,
    ProcessedSimulationEvent? ProcessedEvent,
    CelestialImpulseEvaluationStatus? CelestialImpulseStatus = null,
    CelestialTrajectoryTransactionStatus? CelestialTransactionStatus = null)
{
    public bool Committed => Status == SimulationTransactionStatus.Committed;
}
