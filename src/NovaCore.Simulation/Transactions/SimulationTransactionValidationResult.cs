namespace NovaCore.Simulation.Transactions;

internal enum SimulationTransactionValidationStatus : byte
{
    Valid = 0,
    NoPendingEvent,
    EventNotCanonical,
    EvaluationTimeMismatch,
    TimelineRevisionMismatch,
    StateRevisionMismatch,
    InvalidTransaction,
    StateRevisionOverflow,
    TimelineRevisionOverflow,
}

internal readonly record struct SimulationTransactionValidationResult(SimulationTransactionValidationStatus Status)
{
    public bool IsValid => Status == SimulationTransactionValidationStatus.Valid;
    public static SimulationTransactionValidationResult Valid => new(SimulationTransactionValidationStatus.Valid);
}
