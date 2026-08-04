using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal enum SimulationExecutionStopReason : byte
{
    ReachedTarget = 0,
    Completed,
    NoPendingEvent,
    NotAtBoundary,
    EventLimitReached,
    ValidationRejected,
    ReentrantExecution,
    TargetBeforeCurrent,
    ReentrantAdvance,
    NextBoundaryReached,
}

/// <summary>Deterministic diagnostics for coasting to, executing, and coasting after one group.</summary>
internal readonly record struct SimulationExecutionResult(
    SimulationExecutionStopReason Reason,
    SimulationInstant RequestedTime,
    SimulationInstant ReachedTime,
    SimulationAdvanceStopReason InitialAdvanceReason,
    SimulationAdvanceStopReason? ContinuationAdvanceReason,
    SimulationCanonicalGroupResult? Group);
