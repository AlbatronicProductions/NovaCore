using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Clock;

/// <summary>
/// Managed authoritative time cursor over a pending-event timeline. This 6B-2 clock deliberately
/// stops at boundaries; it does not execute, remove, or otherwise mutate pending events.
/// </summary>
public sealed class SimulationClock
{
    private readonly SimulationClockSettings _settings;
    private bool _isAdvancing;
    private bool _isPaused;
    private SimulationInstant _currentTime;
    private SimulationRate _rate;
    private long _rateRemainder;

    public SimulationClock(
        SimulationInstant initialTime,
        SimulationTimeline timeline,
        SimulationRate? rate = null,
        SimulationClockSettings? settings = null)
    {
        Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        _currentTime = initialTime;
        _rate = rate ?? SimulationRate.One;
        var selectedSettings = settings ?? SimulationClockSettings.Default;
        if (selectedSettings.MaximumEventsPerAdvance <= 0) throw new ArgumentOutOfRangeException(nameof(settings));
        _settings = selectedSettings;
    }

    public SimulationInstant CurrentTime => _currentTime;
    public bool IsPaused => _isPaused;
    public SimulationRate Rate => _rate;
    public long RateRemainder => _rateRemainder;
    public SimulationTimeline Timeline { get; }
    public SimulationClockSettings Settings => _settings;
    public int MaximumEventsPerAdvance => _settings.MaximumEventsPerAdvance;

    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;

    /// <summary>Changes the future host-duration rate only; 6B-2 performs no host-duration advancement.</summary>
    public bool TrySetRate(SimulationRate rate)
    {
        if (_rate == rate) return false;
        _rate = rate;
        _rateRemainder = 0;
        return true;
    }

    /// <summary>
    /// Advances forward exactly until the requested time or canonical first pending boundary.
    /// Explicit commands remain valid while paused.
    /// </summary>
    public SimulationAdvanceResult AdvanceTo(SimulationInstant target)
    {
        if (_isAdvancing) return Result(SimulationAdvanceStopReason.ReentrantAdvance, target, null, 0);
        _isAdvancing = true;
        try
        {
            if (target < _currentTime) return Result(SimulationAdvanceStopReason.TargetBeforeCurrent, target, null, 0);
            if (!Timeline.TryPeekPending(out var pending))
            {
                _currentTime = target;
                return Result(SimulationAdvanceStopReason.ReachedTarget, target, null, 0);
            }
            if (pending.Header.Time < _currentTime) throw new InvalidOperationException("Pending event precedes the authoritative clock time.");
            if (pending.Header.Time <= target)
            {
                _currentTime = pending.Header.Time;
                return Result(SimulationAdvanceStopReason.ReachedEventBoundary, target, pending.Header, 1);
            }
            _currentTime = target;
            return Result(SimulationAdvanceStopReason.ReachedTarget, target, null, 1);
        }
        finally { _isAdvancing = false; }
    }

    /// <summary>Moves to the canonical next pending boundary without consuming it. Valid while paused.</summary>
    public SimulationAdvanceResult AdvanceUntilNextEvent()
    {
        var requested = _currentTime;
        if (_isAdvancing) return Result(SimulationAdvanceStopReason.ReentrantAdvance, requested, null, 0);
        _isAdvancing = true;
        try
        {
            if (!Timeline.TryPeekPending(out var pending)) return Result(SimulationAdvanceStopReason.NoPendingEvent, requested, null, 0);
            if (pending.Header.Time < _currentTime) throw new InvalidOperationException("Pending event precedes the authoritative clock time.");
            _currentTime = pending.Header.Time;
            return Result(SimulationAdvanceStopReason.ReachedEventBoundary, requested, pending.Header, 1);
        }
        finally { _isAdvancing = false; }
    }

    // Narrow test seam: no public callback path exists before 6B-3 event execution can reenter the clock.
    internal SimulationAdvanceResult AdvanceToWhileGuardedForTest(SimulationInstant target)
    {
        if (_isAdvancing) throw new InvalidOperationException("Test seam cannot nest its own guard.");
        _isAdvancing = true;
        try { return AdvanceTo(target); }
        finally { _isAdvancing = false; }
    }

    internal SimulationAdvanceResult AdvanceUntilNextEventWhileGuardedForTest()
    {
        if (_isAdvancing) throw new InvalidOperationException("Test seam cannot nest its own guard.");
        _isAdvancing = true;
        try { return AdvanceUntilNextEvent(); }
        finally { _isAdvancing = false; }
    }

    private SimulationAdvanceResult Result(SimulationAdvanceStopReason reason, SimulationInstant requested, SimulationEventHeader? boundary, int examined) =>
        new(reason, requested, _currentTime, boundary, Timeline.Revision, examined);
}
