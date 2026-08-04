namespace NovaCore.Simulation.Clock;

public enum SimulationAdvanceStopReason : byte
{
    ReachedTarget = 0,
    ReachedEventBoundary,
    NoPendingEvent,
    Paused,
    TargetBeforeCurrent,
    ReentrantAdvance,
    ArithmeticOverflow,
}
