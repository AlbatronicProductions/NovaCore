using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // The original source, frame and solver remain immutable. Only the engine's fixed acknowledgement
    // changes these expected authority tokens. Explicit owner host credit can acknowledge clock accounting;
    // ordinary external changes are never auto-rebound.
    private sealed class PublicationBinding
    {
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftDefinition Definition;
        internal readonly SpacecraftPhysicalProperties Properties;
        internal readonly TimelineRevision Timeline;
        internal readonly PersistentContactEpisode Episode;
        internal SpacecraftTranslationState Linear;
        internal SpacecraftRigidBodyRotationState Angular;
        internal StateRevision Revision;
        internal ContinuationClockState Clock;
        internal long AcknowledgedFrontier;
        internal bool Pending;
        internal long HostInputSequence;

        internal PublicationBinding(SimulationTransactionEngine engine, SpacecraftDefinition definition,
            SpacecraftPhysicalProperties properties, TimelineRevision timeline, PersistentContactEpisode episode,
            SpacecraftTranslationState linear, SpacecraftRigidBodyRotationState angular,
            StateRevision revision, ContinuationClockState clock)
        {
            Engine = engine; Definition = definition; Properties = properties; Timeline = timeline; Episode = episode;
            Linear = linear; Angular = angular; Revision = revision; Clock = clock;
        }
    }
    private PublicationBinding? publication;

    // Cold opt-in, called only while the engine owns the common publication phase.
    internal LocalContactStatus BindPublication(SimulationTransactionEngine engine, LocalContactConfiguration config,
        Receipt initial, long episode)
    {
        if (!engine.OwnsPersistentPublicationPhase) return LocalContactStatus.WrongThread;
        var status = Validate(engine, config, initial, source.Motion.Time);
        if (status != LocalContactStatus.Success) return status;
        if (frontier != 0 || publication is not null || !initial.IsIssuedBy(receiptIdentity))
            return LocalContactStatus.FrontierMismatch;
        var view = engine.State;
        if (!view.Spacecraft.TryGetTranslation(source.Motion.Spacecraft, out var linear, out var properties) ||
            !view.Spacecraft.TryGetRigidBody(source.Motion.Spacecraft, out var angular) ||
            !view.Spacecraft.TryGetDefinition(source.Motion.Spacecraft, out var definition)) return LocalContactStatus.InvalidSource;
        var identity = new PersistentContactEpisode(episode, source.Motion.Time, source.End, config.Revision,
            config.RootFrame, config.OriginRoot, config.OriginVelocityRoot, config.LocalToRoot,
            config.BoxDimensions, config.PlaneHalfExtent);
        publication = new(engine, definition, properties, source.TimelineRevision, identity,
            linear, angular, view.Revision, engine.CaptureContinuationClock());
        return LocalContactStatus.Success;
    }

    private LocalContactStatus ValidateAuthority(SimulationTransactionEngine engine, LocalContactConfiguration config,
        SimulationInstant target)
    {
        if (publication is not { } p) return source.Validate(engine, config, target);
        if (!ReferenceEquals(engine, p.Engine)) return LocalContactStatus.ForeignEngine;
        if (!engine.IsContactProofOwnerThread) return LocalContactStatus.WrongThread;
        if (!ReferenceEquals(config, configuration)) return LocalContactStatus.ConfigurationMismatch;
        if (engine.HasContactProofBoundaryThrough(target)) return LocalContactStatus.PendingEvent;
        if (engine.ContactProofTimelineRevision != p.Timeline) return LocalContactStatus.TimelineConflict;
        var view = engine.State;
        if (view.Revision != p.Revision || engine.CaptureContinuationClock() != p.Clock ||
            !view.Spacecraft.TryGetTranslation(source.Motion.Spacecraft, out var linear, out var properties) ||
            !view.Spacecraft.TryGetRigidBody(source.Motion.Spacecraft, out var angular) ||
            !view.Spacecraft.TryGetDefinition(source.Motion.Spacecraft, out var definition) ||
            linear != p.Linear || angular != p.Angular || properties != p.Properties || definition != p.Definition)
            return LocalContactStatus.ChangedAuthority;
        if (target < p.Clock.Time || target > source.End) return LocalContactStatus.InvalidInterval;
        return engine.HasContactProofBoundaryThrough(target) ? LocalContactStatus.PendingEvent : LocalContactStatus.Success;
    }

    internal PersistentContactPublicationStatus PreparePublication(SimulationTransactionEngine engine,
        LocalContactConfiguration config, Receipt receipt, out Export endpoint, out PersistentContactEpisode episode,
        out SpacecraftTranslationState linear, out SpacecraftRigidBodyRotationState angular)
    {
        endpoint = default; episode = default; linear = default; angular = default;
        if (Environment.CurrentManagedThreadId != ownerThread) return PersistentContactPublicationStatus.WrongOwnerThread;
        if (!ReferenceEquals(receipt.Owner, this) || !receipt.IsIssuedBy(receiptIdentity) || receipt.Generation != Generation)
            return PersistentContactPublicationStatus.InvalidReceipt;
        if (disposed || invalidated) return PersistentContactPublicationStatus.WorldUnavailable;
        if (publication is not { } p) return PersistentContactPublicationStatus.NotPublishing;
        if (!p.Pending || receipt.Step != frontier || frontier != p.AcknowledgedFrontier + 1)
            return PersistentContactPublicationStatus.FrontierConflict;
        var status = ValidateAuthority(engine, config, export.Motion.Time);
        if (status != LocalContactStatus.Success) return status switch
        {
            LocalContactStatus.ForeignEngine => PersistentContactPublicationStatus.ForeignEngine,
            LocalContactStatus.ConfigurationMismatch => PersistentContactPublicationStatus.ConfigurationMismatch,
            LocalContactStatus.TimelineConflict => PersistentContactPublicationStatus.TimelineConflict,
            LocalContactStatus.PendingEvent => PersistentContactPublicationStatus.PendingEvent,
            _ => PersistentContactPublicationStatus.StateConflict,
        };
        if (!source.TryEndpoint(frontier, out var target) || target != export.Motion.Time ||
            !source.TryEndpoint(p.AcknowledgedFrontier, out var start) || start != p.Clock.Time)
            return PersistentContactPublicationStatus.InvalidEndpoint;
        endpoint = export; episode = p.Episode; linear = p.Linear; angular = p.Angular;
        return PersistentContactPublicationStatus.Published;
    }

    /// <summary>Prepared fixed targets. Never exposed by a publication result or canonical history.</summary>
    internal readonly struct Acknowledgement
    {
        private readonly LocalContactWorld world;
        private readonly PublicationBinding binding;
        private readonly PersistentContactObservation committed;
        private Acknowledgement(LocalContactWorld world, PublicationBinding binding, PersistentContactObservation committed)
        { this.world = world; this.binding = binding; this.committed = committed; }

        internal static bool TryPrepare(LocalContactWorld world, SimulationTransactionEngine engine,
            in PersistentContactObservation committed, out Acknowledgement result)
        {
            result = default;
            if (!engine.OwnsPersistentPublicationPhase || world.publication is not { } p ||
                !ReferenceEquals(p.Engine, engine) || !p.Pending || world.disposed || world.invalidated ||
                committed.Frontier != world.frontier || committed.Episode != p.Episode.Sequence) return false;
            result = new(world, p, committed); return true;
        }

        // WRITE-ONLY ACK: resolved fields, no owner lookup, reimport, validation or solver calls.
        internal void Commit()
        {
            binding.Linear = committed.Translation; binding.Angular = committed.Rotation;
            binding.Revision = committed.StateRevision; binding.Clock = committed.Clock;
            binding.AcknowledgedFrontier = committed.Frontier; binding.Pending = false;
        }
        internal void Invalidate() => world.invalidated = true;
    }
}
