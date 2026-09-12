using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;

namespace NovaCore.Simulation.Transactions;

/// <summary>Minimal internal authoritative state used solely to establish the mutation contract.</summary>
internal sealed class SimulationState
{
    private SimulationClock? _publicationClock;
    private SimulationTransactionEngine? _engine;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private long _markerValue;
    private StateRevision _revision;
    private readonly CelestialStateStore _celestial;
    private readonly SpacecraftStateStore _spacecraft;

    internal SimulationState(CelestialStateStore? celestial = null, SpacecraftStateStore? spacecraft = null, StateRevision initialRevision = default)
    {
        _celestial = celestial ?? CelestialStateStore.Empty; _spacecraft = spacecraft ?? SpacecraftStateStore.Empty; _revision = initialRevision;
        _celestial.BindOwner(this); _spacecraft.BindOwner(this);
    }

    internal void BindPublicationClock(SimulationClock clock, SimulationTransactionEngine engine)
    {
        clock.PublicationPhase.VerifyOrdinaryMutation();
        if (_ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Canonical state cannot change owner thread.");
        if (_publicationClock is not null && !ReferenceEquals(_publicationClock, clock))
            throw new InvalidOperationException("One canonical state cannot have two clock owners.");
        if (_engine is not null && !ReferenceEquals(_engine, engine))
            throw new InvalidOperationException("One canonical state cannot have two transaction/history owners.");
        _publicationClock = clock;
        _engine = engine;
    }

    internal bool IsBorrowCurrent(StateRevision revision) =>
        _ownerThread == Environment.CurrentManagedThreadId && _revision == revision;

    internal StateRevision BorrowRevision { get { VerifyStoreRead(); return _revision; } }
    internal void VerifyStoreRead()
    {
        if (_ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Canonical state belongs to its owner thread.");
    }
    internal void VerifyStoreMutation()
    {
        VerifyStoreRead();
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
    }

    public SimulationStateView CreateView()
    {
        _publicationClock?.PublicationPhase.VerifyRead();
        return new(_markerValue, _revision, new(_celestial, this, _revision), new(_spacecraft, this, _revision));
    }

    internal bool TryPrepareContinuationSlot(in SpacecraftTranslationState linear, in SpacecraftRigidBodyRotationState angular,
        out int index) => _spacecraft.TryPrepareContinuationSlot(linear, angular, out index);

    // The specialized publisher has checked both slots, ownership and successor arithmetic.
    internal void InstallCertifiedContinuation(int index, in SpacecraftTranslationState linear,
        in SpacecraftRigidBodyRotationState angular, StateRevision revision)
    {
        _spacecraft.InstallCertifiedContinuation(index, linear, angular);
        _revision = revision;
    }

    /// <summary>Single-writer paired commit: arithmetic and both store checks precede any assignment.</summary>
    internal void CommitContactResponse(in SpacecraftTranslationState expectedLinear, in SpacecraftTranslationState replacementLinear,
        in SpacecraftRigidBodyRotationState expectedAngular, in SpacecraftRigidBodyRotationState replacementAngular)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        var next = new StateRevision(checked(_revision.Value + 1));
        if (!_spacecraft.TryReplaceContactResponse(expectedLinear, replacementLinear, expectedAngular, replacementAngular))
            throw new InvalidOperationException("Validated paired spacecraft replacement failed before mutation.");
        _revision = next;
    }

    /// <summary>Only the transaction engine calls this after complete continuity, revision and capacity validation.</summary>
    internal void CommitSpacecraftTranslation(in SpacecraftTranslationState expected, in SpacecraftTranslationState replacement)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        if (!_spacecraft.TryReplaceTranslation(expected, replacement)) throw new InvalidOperationException("Validated translation replacement failed.");
        _revision = new StateRevision(checked(_revision.Value + 1));
    }

    internal void CommitMarkerValue(long markerValue)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        _markerValue = markerValue;
        _revision = new StateRevision(checked(_revision.Value + 1));
    }

    /// <summary>Called only by the transaction engine after all celestial validation and capacity checks succeed.</summary>
    internal bool CommitCelestialTrajectoryReplacement(CelestialBodyId subject, in TwoBodyTrajectory expected, in TwoBodyTrajectory replacement, out CelestialStateStoreMutationStatus status)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        if (!_celestial.TryReplaceTrajectory(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1));
        return true;
    }

    /// <summary>Called only by the transaction engine after all attitude validation and capacity checks succeed.</summary>
    internal bool CommitSpacecraftAttitudeReplacement(SpacecraftId subject, in SpacecraftAttitudeState expected, in SpacecraftAttitudeState replacement, out SpacecraftStateStoreMutationStatus status)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        if (!_spacecraft.TryReplaceAttitude(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1)); return true;
    }

    /// <summary>Called only by the transaction engine after a rigid-body replacement is fully validated.</summary>
    internal bool CommitSpacecraftRigidBodyReplacement(SpacecraftId subject, in SpacecraftRigidBodyRotationState expected, in SpacecraftRigidBodyRotationState replacement, out SpacecraftStateStoreMutationStatus status)
    {
        _publicationClock?.PublicationPhase.VerifyOrdinaryMutation();
        if (!_spacecraft.TryReplaceRigidBody(subject, expected, replacement, out status)) return false;
        _revision = new StateRevision(checked(_revision.Value + 1)); return true;
    }
}
