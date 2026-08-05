using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Transactions;

/// <summary>
/// Internal composition boundary for future clock-duration advancement. It coordinates clock
/// movement with group execution but never owns time or mutates simulation state itself.
/// </summary>
internal sealed class SimulationExecutionOrchestrator
{
    private readonly SimulationClock _clock;
    private readonly SimulationTransactionEngine _transactions;
    private bool _isOrchestrating;

    public SimulationExecutionOrchestrator(SimulationClock clock, SimulationTransactionEngine transactions)
    {
        _clock = clock;
        _transactions = transactions;
    }

    public SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroup(SimulationInstant target)
    {
        if (_isOrchestrating) return Result(SimulationExecutionStopReason.ReentrantExecution, target, SimulationAdvanceStopReason.ReentrantAdvance, null, null);
        _isOrchestrating = true;
        try
        {
            var initial = _clock.AdvanceTo(target);
            if (initial.Reason == SimulationAdvanceStopReason.ReachedTarget)
                return Result(SimulationExecutionStopReason.ReachedTarget, target, initial.Reason, null, null);
            if (initial.Reason == SimulationAdvanceStopReason.TargetBeforeCurrent)
                return Result(SimulationExecutionStopReason.TargetBeforeCurrent, target, initial.Reason, null, null);
            if (initial.Reason == SimulationAdvanceStopReason.ReentrantAdvance)
                return Result(SimulationExecutionStopReason.ReentrantAdvance, target, initial.Reason, null, null);

            var group = _transactions.ExecuteCanonicalGroup();
            if (!group.IsComplete)
                return Result(MapGroupReason(group.Reason), target, initial.Reason, null, group);

            var continuation = _clock.AdvanceTo(target);
            var reason = continuation.Reason == SimulationAdvanceStopReason.ReachedTarget
                ? SimulationExecutionStopReason.Completed
                : continuation.Reason == SimulationAdvanceStopReason.ReachedEventBoundary
                    ? SimulationExecutionStopReason.NextBoundaryReached
                    : continuation.Reason == SimulationAdvanceStopReason.TargetBeforeCurrent
                        ? SimulationExecutionStopReason.TargetBeforeCurrent
                        : SimulationExecutionStopReason.ReentrantAdvance;
            return Result(reason, target, initial.Reason, continuation.Reason, group);
        }
        finally { _isOrchestrating = false; }
    }

    /// <summary>
    /// Services retained debt by coasting and executing canonical groups under one call-wide budget.
    /// The clock alone reduces debt, and only after its time cursor has advanced successfully.
    /// </summary>
    internal SimulationDebtServiceResult ServicePendingHostDurationDebt()
    {
        var startTime = _clock.CurrentTime;
        var debtBefore = _clock.PendingSimulationDebt;
        if (debtBefore.IsZero) return DebtResult(SimulationDebtServiceStopReason.NoDebt, startTime, debtBefore, 0);
        if (_isOrchestrating) return DebtResult(SimulationDebtServiceStopReason.ReentrantExecution, startTime, debtBefore, 0);

        _isOrchestrating = true;
        try
        {
            var processedEvents = 0;
            while (!_clock.PendingSimulationDebt.IsZero)
            {
                if (!_clock.TryGetPendingSimulationDebtTarget(out var target))
                    return DebtResult(SimulationDebtServiceStopReason.ArithmeticOverflow, startTime, debtBefore, processedEvents);

                var timeBefore = _clock.CurrentTime;
                var advance = _clock.AdvanceTo(target);
                if (advance.Reason is SimulationAdvanceStopReason.ReentrantAdvance or SimulationAdvanceStopReason.TargetBeforeCurrent)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, debtBefore, processedEvents);

                var traversedTicks = _clock.CurrentTime.Ticks - timeBefore.Ticks;
                if (traversedTicks > 0)
                    _clock.ConsumePendingSimulationDebt(new SimulationDuration(traversedTicks));

                if (advance.Reason == SimulationAdvanceStopReason.ReachedTarget)
                    return DebtResult(SimulationDebtServiceStopReason.Completed, startTime, debtBefore, processedEvents);

                if (advance.Reason != SimulationAdvanceStopReason.ReachedEventBoundary)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, debtBefore, processedEvents);

                var remainingBudget = _clock.MaximumEventsPerAdvance - processedEvents;
                if (remainingBudget == 0)
                    return DebtResult(SimulationDebtServiceStopReason.EventLimitReached, startTime, debtBefore, processedEvents);

                var group = _transactions.ExecuteCanonicalGroup(remainingBudget);
                processedEvents += group.ProcessedEventCount;
                if (group.Reason == SimulationCanonicalGroupStopReason.EventLimitReached)
                    return DebtResult(SimulationDebtServiceStopReason.EventLimitReached, startTime, debtBefore, processedEvents);
                if (group.Reason == SimulationCanonicalGroupStopReason.ValidationRejected)
                    return DebtResult(SimulationDebtServiceStopReason.ValidationRejected, startTime, debtBefore, processedEvents);
                if (!group.IsComplete)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, debtBefore, processedEvents);
            }

            return DebtResult(SimulationDebtServiceStopReason.Completed, startTime, debtBefore, processedEvents);
        }
        finally { _isOrchestrating = false; }
    }

    internal SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(SimulationInstant target)
    {
        if (_isOrchestrating) throw new InvalidOperationException("Test seam cannot nest its own execution guard.");
        _isOrchestrating = true;
        try { return AdvanceAndExecuteOneCanonicalGroup(target); }
        finally { _isOrchestrating = false; }
    }

    private SimulationExecutionResult Result(SimulationExecutionStopReason reason, SimulationInstant requested, SimulationAdvanceStopReason initial, SimulationAdvanceStopReason? continuation, SimulationCanonicalGroupResult? group) =>
        new(reason, requested, _clock.CurrentTime, initial, continuation, group);

    private SimulationDebtServiceResult DebtResult(SimulationDebtServiceStopReason reason, SimulationInstant startTime, SimulationDuration debtBefore, int processedEvents) =>
        new(reason, startTime, _clock.CurrentTime, debtBefore, _clock.PendingSimulationDebt, processedEvents);

    private static SimulationExecutionStopReason MapGroupReason(SimulationCanonicalGroupStopReason reason) => reason switch
    {
        SimulationCanonicalGroupStopReason.NoPendingEvent => SimulationExecutionStopReason.NoPendingEvent,
        SimulationCanonicalGroupStopReason.NotAtBoundary => SimulationExecutionStopReason.NotAtBoundary,
        SimulationCanonicalGroupStopReason.EventLimitReached => SimulationExecutionStopReason.EventLimitReached,
        SimulationCanonicalGroupStopReason.ValidationRejected => SimulationExecutionStopReason.ValidationRejected,
        SimulationCanonicalGroupStopReason.ReentrantExecution => SimulationExecutionStopReason.ReentrantExecution,
        _ => SimulationExecutionStopReason.Completed,
    };
}
