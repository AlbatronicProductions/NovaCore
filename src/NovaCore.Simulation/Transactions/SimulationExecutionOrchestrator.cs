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

    internal SimulationExecutionResult AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(SimulationInstant target)
    {
        if (_isOrchestrating) throw new InvalidOperationException("Test seam cannot nest its own execution guard.");
        _isOrchestrating = true;
        try { return AdvanceAndExecuteOneCanonicalGroup(target); }
        finally { _isOrchestrating = false; }
    }

    private SimulationExecutionResult Result(SimulationExecutionStopReason reason, SimulationInstant requested, SimulationAdvanceStopReason initial, SimulationAdvanceStopReason? continuation, SimulationCanonicalGroupResult? group) =>
        new(reason, requested, _clock.CurrentTime, initial, continuation, group);

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
