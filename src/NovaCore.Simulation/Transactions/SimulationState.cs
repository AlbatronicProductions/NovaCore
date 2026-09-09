using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;

namespace NovaCore.Simulation.Transactions;

/// <summary>Minimal internal authoritative state used solely to establish the mutation contract.</summary>
internal sealed class SimulationState
{
    private long _markerValue;
    private StateRevision _revision;
    private readonly CelestialStateStore _celestial;
    private readonly SpacecraftStateStore _spacecraft;

    internal SimulationState(CelestialStateStore? celestial = null, SpacecraftStateStore? spacecraft = null) { _celestial = celestial ?? CelestialStateStore.Empty; _spacecraft = spacecraft ?? SpacecraftStateStore.Empty; }

    public SimulationStateView CreateView() => new(_markerValue, _revision, _celestial.CreateView(), _spacecraft.CreateView());

    /// <summary>Only the transaction engine calls this after complete continuity, revision and capacity validation.</summary>
    internal void CommitSpacecraftTranslation(in SpacecraftTranslationState expected, in SpacecraftTranslationState replacement)
    {
        if (!_spacecraft.TryReplaceTranslation(expected, replacement)) throw new InvalidOperationException("Validated translation replacement failed.");
        _revision = new StateRevision(checked(_revision.Value + 1));
    }

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

    /// <summary>Called only by the transaction engine after all attitude validation and capacity checks succeed.</summary>
    internal bool CommitSpacecraftAttitudeReplacement(SpacecraftId subject, in SpacecraftAttitudeState expected, in SpacecraftAttitudeState replacement, out SpacecraftStateStoreMutationStatus status)
    {
        if (!_spacecraft.TryReplaceAttitude(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1)); return true;
    }

    /// <summary>Called only by the transaction engine after a rigid-body replacement is fully validated.</summary>
    internal bool CommitSpacecraftRigidBodyReplacement(SpacecraftId subject, in SpacecraftRigidBodyRotationState expected, in SpacecraftRigidBodyRotationState replacement, out SpacecraftStateStoreMutationStatus status)
    {
        if (!_spacecraft.TryReplaceRigidBody(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1)); return true;
    }
}
