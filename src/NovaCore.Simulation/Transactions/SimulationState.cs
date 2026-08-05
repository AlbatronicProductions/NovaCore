using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial;

namespace NovaCore.Simulation.Transactions;

/// <summary>Minimal internal authoritative state used solely to establish the mutation contract.</summary>
internal sealed class SimulationState
{
    private long _markerValue;
    private StateRevision _revision;
    private readonly CelestialStateStore _celestial;

    internal SimulationState(CelestialStateStore? celestial = null) => _celestial = celestial ?? CelestialStateStore.Empty;

    public SimulationStateView CreateView() => new(_markerValue, _revision, _celestial.CreateView());

    internal void CommitMarkerValue(long markerValue)
    {
        _markerValue = markerValue;
        _revision = new StateRevision(checked(_revision.Value + 1));
    }

    /// <summary>Called only by the transaction engine after all celestial validation and capacity checks succeed.</summary>
    internal bool CommitCelestialTrajectoryReplacement(CelestialBodyId subject, in TwoBodyTrajectory expected, in TwoBodyTrajectory replacement, out CelestialStateStoreMutationStatus status)
    {
        if (!_celestial.TryReplaceTrajectory(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1));
        return true;
    }
}
