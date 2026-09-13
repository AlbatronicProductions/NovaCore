using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Transactions;

internal enum PersistentContactPublicationStatus : byte
{
    InvalidReceipt, Published, CanonicalCommittedPrivateInvalidated, WrongOwnerThread,
    ReentrantPublication, ForeignEngine, ConfigurationMismatch, NotPublishing, WorldUnavailable,
    FrontierConflict, StateConflict, TimelineConflict, ClockConflict, PendingEvent,
    InvalidEndpoint, ArithmeticOverflow, InsufficientDebt, StateRevisionOverflow, HistoryCapacityFailure,
}

/// <summary>Explicit copied clock expectation; a forged request cannot manufacture an issued endpoint.</summary>
internal readonly record struct PersistentContactPublicationRequest(LocalContactWorld.Receipt Receipt,
    ContinuationClockState Clock);

/// <summary>Value provenance for a qualification episode. No solver capability or global generation order.</summary>
internal readonly record struct PersistentContactEpisode(long Sequence, SimulationInstant Start, SimulationInstant End,
    long ConfigurationRevision, ReferenceFrameId Root, Double3 Origin, Double3 OriginVelocity,
    DoubleQuaternion LocalToRoot, Double3 BoxDimensions, double PlaneHalfExtent,
    EngineeringArticleIdentity Article = default);

/// <summary>One canonical paired endpoint publication; contains only deterministic values.</summary>
internal readonly record struct ProcessedPersistentContact(uint Version, int Index, PersistentContactEpisode Episode,
    long Frontier, SpacecraftTranslationState BeforeTranslation, SpacecraftRigidBodyRotationState BeforeRotation,
    SpacecraftTranslationState AfterTranslation, SpacecraftRigidBodyRotationState AfterRotation,
    SpacecraftPhysicalProperties Properties, ContinuationClockState BeforeClock, ContinuationClockState AfterClock,
    StateRevision BeforeRevision, StateRevision AfterRevision, TimelineRevision TimelineRevision);

internal readonly record struct PersistentContactObservation(SpacecraftTranslationState Translation,
    SpacecraftRigidBodyRotationState Rotation, SpacecraftPhysicalProperties Properties, ContinuationClockState Clock,
    StateRevision StateRevision, TimelineRevision TimelineRevision, long Episode, long Frontier, int HistoryIndex);

internal readonly record struct PersistentContactPublicationResult(PersistentContactPublicationStatus Status,
    PersistentContactObservation Observation = default)
{
    internal bool CanonicalCommitted => Status is PersistentContactPublicationStatus.Published or
        PersistentContactPublicationStatus.CanonicalCommittedPrivateInvalidated;
}
