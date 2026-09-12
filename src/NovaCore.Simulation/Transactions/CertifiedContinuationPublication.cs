using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using Proof = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof;
using Coverage = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof.PostImpactCoverageProvider.CoverageEvidence;

namespace NovaCore.Simulation.Transactions;

internal enum ContinuationPublicationStatus : byte
{
    InvalidInput, Published, WrongOwnerThread, ReentrantPublication, ForeignEngine,
    StaleEndpoint, InvalidEndpointEvidence, StaleClearance, InvalidClearanceEvidence,
    SourceTimeMismatch, ClockConflict, StateConflict, TimelineConflict, PendingEvent,
    InvalidTarget, InsufficientDebt, ArithmeticOverflow, StateSlotMismatch,
    InvalidEndpoint, StateRevisionOverflow, HistoryCapacityFailure, FinalApplicabilityFailure,
}

/// <summary>Copied exact clock expectation; changing debt/rate does not change physical receipt applicability.</summary>
internal readonly record struct ContinuationClockState(SimulationInstant Time, SimulationDuration Debt,
    SimulationRate Rate, long RateRemainder, bool Paused);

/// <summary>Input bundle of genuine checked receipts and explicit expectations; constructibility grants no authority.</summary>
internal readonly record struct CertifiedContinuationRequest(Proof Root, Proof.PrivatePostImpactState Initial,
    Proof.PostImpactVelocity Realization, PrivatePostImpactPoseRequest Pose, SimulationInstant Target,
    PrivatePropagationRequest Propagation, Proof.PrivateCanonicalState Endpoint, Coverage Clearance,
    StateRevision StateRevision, TimelineRevision TimelineRevision, ContinuationClockState Clock);

/// <summary>
/// Stable value proposition for the unique source-cell root, not a selected alpha or provider pointer.
/// Original requests are retained so reconstruction need not guess tolerances or refinement budgets.
/// </summary>
internal readonly record struct ContinuationPublicationProvenance(
    SimulationInstant SourceStart, SimulationInstant SourceEnd, FloridaBound RootEnclosure,
    FloridaBound PoseRootEnclosure, int RootRefinements, SpacecraftDefinition Spacecraft,
    ReferenceFrameNode RootFrame, ReferenceFrameNode BodyFrame,
    ContactGeometryIdentity Geometry, SpacecraftContactFeature Feature, PhysicalSurfaceAuthorityIdentity Terrain,
    CelestialSystemId System, string SystemVersion, CelestialEphemerisMetadata Ephemeris,
    CelestialSystemTimeMapping TimeMapping, ContinuationQualificationInputs Qualification,
    PrivatePropagationRequest Propagation, uint ProviderVersion, uint RelationVersion,
    uint RealizationPolicyVersion, uint RealizationNumericalVersion, uint RealizationRecipeVersion,
    uint PrivateStateVersion, int PropagationVersion, uint ClearanceVersion);

/// <summary>One fixed-capacity coupled value record; no receipt, provider, proof tree or capability is retained.</summary>
internal readonly record struct ProcessedCertifiedContinuation(uint Version, int Index, SpacecraftId Spacecraft,
    SpacecraftTranslationState BeforeTranslation, SpacecraftRigidBodyRotationState BeforeRotation,
    SpacecraftTranslationState AfterTranslation, SpacecraftRigidBodyRotationState AfterRotation,
    SpacecraftPhysicalProperties Properties, ContinuationClockState BeforeClock, ContinuationClockState AfterClock,
    StateRevision BeforeRevision, StateRevision AfterRevision, TimelineRevision TimelineRevision,
    ContinuationPublicationProvenance Provenance, ContinuationPublicationStatus Classification);

/// <summary>Retainable copied authority. Journal cursor is the global publication frontier, not this craft's latest mutation.</summary>
internal readonly record struct ContinuationPublicationObservation(SpacecraftId Spacecraft,
    SpacecraftTranslationState Translation, SpacecraftRigidBodyRotationState Rotation,
    SpacecraftPhysicalProperties Properties, ContinuationClockState Clock, StateRevision StateRevision,
    TimelineRevision TimelineRevision, int PublicationCount, int PublicationJournalCursor);

internal readonly record struct ContinuationPublicationResult(ContinuationPublicationStatus Status,
    ContinuationPublicationObservation Observation = default)
{
    internal bool Published => Status == ContinuationPublicationStatus.Published;
}

// Private engine preparation uses plain values. There is no public readiness capability or commit API.
// Separate methods make the actual preflight/history/write responsibilities independently measurable.
internal readonly record struct PreparedContinuationPublication(int Slot, ProcessedCertifiedContinuation Record,
    ContinuationPublicationObservation Observation);
