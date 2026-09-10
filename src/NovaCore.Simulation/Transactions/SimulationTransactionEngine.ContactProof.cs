using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    // Read access to this engine's OWN clock/timeline prevents an unrelated empty timeline
    // being supplied as proof that the engine's future motion segment is unchanged.
    internal SimulationInstant ContactProofCurrentTime => _clock.CurrentTime;
    internal TimelineRevision ContactProofTimelineRevision => _clock.Timeline.Revision;
    internal bool HasContactProofBoundaryThrough(SimulationInstant end) =>
        _clock.Timeline.TryPeekPending(out var pending) && pending.Header.Time <= end;
}
