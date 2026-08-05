using System.Diagnostics;
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
    // The coordinator delegates all event execution; it never mutates state itself.
    private readonly SimulationTransactionEngine _transactionEngine;
    private bool _isOrchestrating;

    public SimulationExecutionOrchestrator(SimulationClock clock, SimulationTransactionEngine transactionEngine)
    {
        _clock = clock;
        _transactionEngine = transactionEngine;
    }

    public SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroup(SimulationInstant target)
    {
        if (_isOrchestrating) return CreateExecutionResult(SimulationExecutionStopReason.ReentrantExecution, target, SimulationAdvanceStopReason.ReentrantAdvance, null, null);
        _isOrchestrating = true;
        try
        {
            var initial = _clock.AdvanceTo(target);
            if (initial.Reason == SimulationAdvanceStopReason.ReachedTarget)
                return CreateExecutionResult(SimulationExecutionStopReason.ReachedTarget, target, initial.Reason, null, null);
            if (initial.Reason == SimulationAdvanceStopReason.TargetBeforeCurrent)
                return CreateExecutionResult(SimulationExecutionStopReason.TargetBeforeCurrent, target, initial.Reason, null, null);
            if (initial.Reason == SimulationAdvanceStopReason.ReentrantAdvance)
                return CreateExecutionResult(SimulationExecutionStopReason.ReentrantAdvance, target, initial.Reason, null, null);

            var group = _transactionEngine.ExecuteCanonicalGroup();
            if (!group.IsComplete)
                return CreateExecutionResult(MapGroupReason(group.Reason), target, initial.Reason, null, group);

            var continuation = _clock.AdvanceTo(target);
            var reason = continuation.Reason == SimulationAdvanceStopReason.ReachedTarget
                ? SimulationExecutionStopReason.Completed
                : continuation.Reason == SimulationAdvanceStopReason.ReachedEventBoundary
                    ? SimulationExecutionStopReason.NextBoundaryReached
                    : continuation.Reason == SimulationAdvanceStopReason.TargetBeforeCurrent
                        ? SimulationExecutionStopReason.TargetBeforeCurrent
                        : SimulationExecutionStopReason.ReentrantAdvance;
            return CreateExecutionResult(reason, target, initial.Reason, continuation.Reason, group);
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
        Debug.Assert(debtBefore.Ticks >= 0);
        if (debtBefore.IsZero) return DebtResult(SimulationDebtServiceStopReason.NoDebt, startTime, startTime, debtBefore, 0, 0, null);
        if (_isOrchestrating) return DebtResult(SimulationDebtServiceStopReason.ReentrantExecution, startTime, startTime, debtBefore, 0, 0, null);
        if (!_clock.TryGetPendingSimulationDebtTarget(out var target))
            return DebtResult(SimulationDebtServiceStopReason.ArithmeticOverflow, startTime, default, debtBefore, 0, 0, null);

        _isOrchestrating = true;
        try
        {
            var processedEvents = 0;
            var executedGroups = 0;
            SimulationCanonicalGroupStopReason? lastGroupReason = null;
            while (!_clock.PendingSimulationDebt.IsZero)
            {
                var timeBefore = _clock.CurrentTime;
                var advance = _clock.AdvanceTo(target);
                if (advance.Reason is SimulationAdvanceStopReason.ReentrantAdvance or SimulationAdvanceStopReason.TargetBeforeCurrent)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);

                var traversedTicks = _clock.CurrentTime.Ticks - timeBefore.Ticks;
                if (traversedTicks > 0)
                    _clock.ConsumePendingSimulationDebt(new SimulationDuration(traversedTicks));
                Debug.Assert(_clock.PendingSimulationDebt.Ticks >= 0);
                Debug.Assert(_clock.CurrentTime <= target);

                if (advance.Reason == SimulationAdvanceStopReason.ReachedTarget)
                    return DebtResult(SimulationDebtServiceStopReason.Completed, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);

                if (advance.Reason != SimulationAdvanceStopReason.ReachedEventBoundary)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);

                var remainingBudget = _clock.MaximumEventsPerAdvance - processedEvents;
                if (remainingBudget == 0)
                    return DebtResult(SimulationDebtServiceStopReason.EventLimitReached, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);

                var group = _transactionEngine.ExecuteCanonicalGroup(remainingBudget);
                processedEvents += group.ProcessedEventCount;
                executedGroups++;
                lastGroupReason = group.Reason;
                Debug.Assert(processedEvents <= _clock.MaximumEventsPerAdvance);
                if (group.Reason == SimulationCanonicalGroupStopReason.EventLimitReached)
                    return DebtResult(SimulationDebtServiceStopReason.EventLimitReached, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);
                if (group.Reason == SimulationCanonicalGroupStopReason.ValidationRejected)
                    return DebtResult(SimulationDebtServiceStopReason.ValidationRejected, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);
                if (!group.IsComplete)
                    return DebtResult(SimulationDebtServiceStopReason.NoProgress, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);
            }

            return DebtResult(SimulationDebtServiceStopReason.Completed, startTime, target, debtBefore, processedEvents, executedGroups, lastGroupReason);
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

    private SimulationExecutionResult CreateExecutionResult(SimulationExecutionStopReason reason, SimulationInstant requested, SimulationAdvanceStopReason initial, SimulationAdvanceStopReason? continuation, SimulationCanonicalGroupResult? group) =>
        new(reason, requested, _clock.CurrentTime, initial, continuation, group);

    private SimulationDebtServiceResult DebtResult(SimulationDebtServiceStopReason reason, SimulationInstant startTime, SimulationInstant targetTime, SimulationDuration debtBefore, int processedEvents, int executedGroups, SimulationCanonicalGroupStopReason? lastGroupReason) =>
        new(reason, startTime, targetTime, _clock.CurrentTime, debtBefore, _clock.PendingSimulationDebt, processedEvents, executedGroups, lastGroupReason);

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
