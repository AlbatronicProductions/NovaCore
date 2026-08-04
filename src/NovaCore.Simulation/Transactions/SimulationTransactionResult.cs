using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal enum SimulationTransactionStatus : byte { Committed = 0, ValidationFailed }

internal readonly record struct SimulationTransactionResult(
    SimulationTransactionStatus Status,
    SimulationTransactionValidationResult Validation,
    ProcessedSimulationEvent? ProcessedEvent)
{
    public bool Committed => Status == SimulationTransactionStatus.Committed;
}
