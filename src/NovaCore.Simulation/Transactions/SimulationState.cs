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
}
