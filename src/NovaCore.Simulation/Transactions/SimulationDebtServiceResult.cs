using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

internal enum SimulationDebtServiceStopReason : byte
{
    Completed = 0,
    NoDebt,
    EventLimitReached,
    ValidationRejected,
    ReentrantExecution,
    ArithmeticOverflow,
    NoProgress,
}

/// <summary>Allocation-free outcome of servicing retained host-duration debt in one orchestration call.</summary>
internal readonly record struct SimulationDebtServiceResult(
    SimulationDebtServiceStopReason Reason,
    SimulationInstant StartTime,
    SimulationInstant TargetTime,
    SimulationInstant ReachedTime,
    SimulationDuration DebtBefore,
    SimulationDuration DebtAfter,
    int ProcessedEventCount,
    int ExecutedGroupCount,
    SimulationCanonicalGroupStopReason? LastGroupStopReason);
