using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

/// <summary>Minimal internal authoritative state used solely to establish the mutation contract.</summary>
internal sealed class SimulationState
{
    private long _markerValue;
    private StateRevision _revision;

    public SimulationStateView CreateView() => new(_markerValue, _revision);

    internal void CommitMarkerValue(long markerValue)
    {
        _markerValue = markerValue;
        _revision = new StateRevision(checked(_revision.Value + 1));
    }
}
