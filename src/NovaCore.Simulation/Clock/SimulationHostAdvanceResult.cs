using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Clock;

internal enum SimulationHostAdvanceStopReason : byte
{
    Accepted = 0,
    NoWork,
    Paused,
    InvalidHostDuration,
    ArithmeticOverflow,
}

/// <summary>Allocation-free diagnostics for exact non-authoritative host-duration conversion.</summary>
internal readonly record struct SimulationHostAdvanceResult(
    SimulationHostAdvanceStopReason Reason,
    SimulationDuration RequestedHostDuration,
    SimulationDuration DerivedSimulationDuration,
    SimulationDuration DebtBefore,
    SimulationDuration DebtAfter,
    long RateRemainderBefore,
    long RateRemainderAfter,
    SimulationInstant CurrentTime);
