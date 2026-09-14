using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Commands;

/// <summary>
/// Immutable capability and schedule description. Only its issuing transaction engine accepts it.
/// Seven ordinary slots hold one complete supported action burst (two edges, axes, throttle, RCS, mode, target).
/// The eighth slot is reserved for prospective control-loss neutralization. No runtime history is retained.
/// </summary>
internal sealed class SpacecraftCommandAuthority
{
    internal const int OrdinaryCapacity = 7;
    internal readonly SpacecraftId Spacecraft;
    internal readonly ReferenceFrameId RootFrame;
    internal readonly SimulationInstant Origin, End;
    internal readonly long LastIndex;
    internal readonly ulong Lease = 1; // One nonrenewable possession lifetime in this bounded slice.

    internal SpacecraftCommandAuthority(SpacecraftId spacecraft, ReferenceFrameId root,
        SimulationInstant origin, SimulationInstant end, long lastIndex)
    { Spacecraft = spacecraft; RootFrame = root; Origin = origin; End = end; LastIndex = lastIndex; }

    // Original 60 Hz integer lattice. Int128 covers the entire Int64 timestamp domain without a float inverse.
    internal bool TryBoundaryAtOrAfter(SimulationInstant horizon, long minimumIndex, out long index, out SimulationInstant epoch)
    {
        var delta = (Int128)horizon.Ticks - Origin.Ticks;
        var n = delta <= 0 ? 0 : (delta * 60 + SimulationDuration.TicksPerSecond - 1) / SimulationDuration.TicksPerSecond;
        n = Int128.Max(n, minimumIndex);
        var ticks = (Int128)Origin.Ticks + n * SimulationDuration.TicksPerSecond / 60;
        if (n > LastIndex || ticks > End.Ticks || ticks > long.MaxValue)
        { index = 0; epoch = default; return false; }
        index = (long)n; epoch = new((long)ticks); return true;
    }
}
