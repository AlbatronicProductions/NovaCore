using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>Qualified source identity, not a contact detector or a solver manifold handle.</summary>
internal readonly record struct ContactResponseProvenance(
    ContactFeatureIdentity Feature, PhysicalSurfaceAuthorityIdentity Support, ulong ObservationId)
{
    internal bool IsValid => Feature.Spacecraft.IsValid && Feature.FeatureId != 0 &&
        Feature.Geometry.DefinitionId != 0 && Feature.Geometry.Version != 0 && IsDigest(Feature.Geometry.ContentSha256) &&
        Support.BodyId != 0 && Support.Terrain.SourceId != 0 && Support.Terrain.Version != 0 &&
        Support.PhysicalGeneration != 0 && Support.QueryPolicyVersion != 0 &&
        double.IsFinite(Support.ReferenceRadiusMetres) && Support.ReferenceRadiusMetres > 0 &&
        IsDigest(Support.GlobalSha256) && IsDigest(Support.RegionalSha256) && ObservationId != 0;

    private static bool IsDigest(string? value)
    {
        if (value is null || value.Length != 64) return false;
        foreach (var c in value)
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))) return false;
        return true;
    }
}

/// <summary>
/// Already-qualified instantaneous impulse (kg m/s) and COM offset (m), both in the named inertial root.
/// Admission of physical contact belongs to the producer. The engine binds this intent to its canonical
/// pending event and rejects stale revisions; it does not infer contact, restitution or support readiness.
/// Version 1 rejects zero impulse without consuming an event. No force/torque intent is replaced.
/// </summary>
internal readonly record struct SpacecraftContactImpulseIntent(
    SpacecraftId Spacecraft, StateRevision ExpectedStateRevision, SimulationInstant Time,
    ReferenceFrameId RootFrame, Double3 ImpulseRoot, Double3 OffsetFromComRoot,
    ContactResponseProvenance Provenance, uint Version = 1)
{
    internal bool IsValid => Version == 1 && Spacecraft.IsValid && RootFrame.Value != 0 &&
        ImpulseRoot.IsFinite && ImpulseRoot != Double3.Zero && OffsetFromComRoot.IsFinite &&
        Provenance.IsValid && Provenance.Feature.Spacecraft == Spacecraft;
}

internal enum ContactImpulseStatus : byte
{
    Uninitialized, Success, InvalidIntent, EventMismatch, TimeMismatch, StateRevisionMismatch,
    RootMismatch, MotionUnavailable, NonFiniteResponse, InvalidReplacement, HistoryCapacityFailure,
    StateRevisionOverflow, TimelineRevisionOverflow,
}

internal readonly record struct SpacecraftContactImpulseTransaction(
    SimulationEventHeader Event, TimelineRevision ExpectedTimelineRevision, SpacecraftContactImpulseIntent Intent,
    SpacecraftTranslationState ExpectedTranslation, SpacecraftTranslationState ReplacementTranslation,
    SpacecraftRigidBodyRotationState ExpectedRotation, SpacecraftRigidBodyRotationState ReplacementRotation);

/// <summary>One physical response history. The general processed-event receipt records scheduling only.</summary>
internal readonly record struct ProcessedSpacecraftContactImpulse(
    SimulationEventHeader Event, SpacecraftContactImpulseIntent Intent, StateRevision Before, StateRevision After,
    SpacecraftTranslationState ExpectedTranslation, SpacecraftTranslationState ReplacementTranslation,
    SpacecraftRigidBodyRotationState ExpectedRotation, SpacecraftRigidBodyRotationState ReplacementRotation);

internal static class SpacecraftContactImpulseEvaluator
{
    /// <summary>Pure evaluation in the single-writer phase. Commit independently re-evaluates the pending intent.</summary>
    internal static ContactImpulseStatus TryCreate(SimulationStateView state, SimulationTimeline timeline, ScheduledSimulationEvent pending,
        SimulationInstant time, TimelineRevision timelineRevision, out SpacecraftContactImpulseTransaction transaction)
    {
        transaction = default;
        if (pending.Header.Kind != SimulationEventKind.SpacecraftContactImpulse ||
            !timeline.TryResolveContactImpulse(pending, out var intent)) return ContactImpulseStatus.InvalidIntent;
        if (pending.Header.Time != time || intent.Time != time) return ContactImpulseStatus.TimeMismatch;
        if (intent.ExpectedStateRevision != state.Revision) return ContactImpulseStatus.StateRevisionMismatch;
        if (!state.Spacecraft.TryGetTranslation(intent.Spacecraft, out var linear, out _) ||
            !state.Spacecraft.TryGetRigidBody(intent.Spacecraft, out var angular)) return ContactImpulseStatus.MotionUnavailable;
        if (linear.RootFrame != intent.RootFrame) return ContactImpulseStatus.RootMismatch;
        if (SpacecraftMotionEvaluator.TryEvaluate(state, intent.Spacecraft, time, out var motion) != SpacecraftTranslationStatus.Success)
            return ContactImpulseStatus.MotionUnavailable;

        var deltaVelocity = intent.ImpulseRoot / motion.Properties.MassKilograms;
        var deltaMomentumRoot = Double3.Cross(intent.OffsetFromComRoot, intent.ImpulseRoot);
        var deltaMomentumBody = motion.BodyToRoot.Conjugate().Rotate(deltaMomentumRoot);
        var inertia = motion.Inertia;
        var deltaOmega = new Double3(deltaMomentumBody.X / inertia.X, deltaMomentumBody.Y / inertia.Y, deltaMomentumBody.Z / inertia.Z);
        if (!deltaVelocity.IsFinite || !deltaMomentumRoot.IsFinite || !deltaMomentumBody.IsFinite || !deltaOmega.IsFinite)
            return ContactImpulseStatus.NonFiniteResponse;

        // Keep the exact evaluated pose. Re-running a creation factory could normalize Q a second time.
        var replacementLinear = linear with { Epoch = time, PositionRoot = motion.PositionRoot,
            VelocityRoot = motion.VelocityRoot + deltaVelocity };
        var replacementAngular = angular with { Epoch = time, OrientationLocalToParent = motion.BodyToRoot,
            AngularVelocityBody = motion.AngularVelocityBody + deltaOmega };
        if (!SpacecraftTranslationEvaluator.TryEvaluate(replacementLinear, motion.Properties, time).Succeeded ||
            !SpacecraftRigidBodyRotationEvaluator.TryEvaluate(replacementAngular, time).Succeeded)
            return ContactImpulseStatus.NonFiniteResponse;
        transaction = new(pending.Header, timelineRevision, intent, linear, replacementLinear, angular, replacementAngular);
        return ContactImpulseStatus.Success;
    }
}
