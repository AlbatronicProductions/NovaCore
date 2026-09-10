using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum EarthRelativeObservationFailure : byte
{
    None, InvalidGeometry, InvalidFeature, SpacecraftMissing, UnsupportedBody, UnsupportedCelestialModel,
    UnsupportedTimeMapping, EpochOutsideRange, DurationCapacity, EarthEvaluation, SpacecraftEvaluation,
    TerrainUnavailable, NormalUnqualified, StaleRevision, AuthorityMismatch, RootMismatch, InvalidBuffers, NonFinite,
}

/// <summary>Status retains canonical M14.3 semantics; Failure supplies the bounded exact-event refusal detail.</summary>
internal readonly record struct EarthRelativeObservationResult(ContactGenerationStatus Status,
    EarthRelativeObservationFailure Failure = default, PhysicalSurfaceQueryStatus SurfaceStatus = default)
{ internal bool Succeeded => Status == ContactGenerationStatus.Ready; }

/// <summary>
/// Immutable numerical evidence, not contact, a root, admissibility or permission to mutate.
/// Exact Epoch identifies FP64 evaluated values; it does not certify mathematical physical state.
/// Contains no live state view. Valid only against its recorded revision, geometry and acquired terrain authority.
/// </summary>
internal readonly record struct SpacecraftPhysicalEventContactObservation(
    bool IsReady, ContactFeatureIdentity Feature, PhysicalEventEpoch Epoch, StateRevision SourceRevision,
    CelestialSystemId System, CelestialBodyId Body, ReferenceFrameId Root,
    PhysicalSurfaceAuthorityIdentity TerrainAuthority, Double3 FeaturePositionRoot,
    Double3 TerrainDirectionBodyFixed, Double3 TerrainPositionRoot, Double3 PhysicalNormalRoot,
    double RadialSignedGapMetres, Double3 FeatureVelocityRoot, Double3 TerrainVelocityRoot, Double3 RelativeVelocityRoot);

internal static class SpacecraftPhysicalEventObservationEvaluator
{
    /// <summary>
    /// Single-writer read phase only. Supply the current authoritative revision independently of the view,
    /// the application's current immutable Sol definition and acquired terrain authority. All buffers are caller-owned.
    /// The complete admitted geometry must contain exactly one zero-radius point; no subset selection is performed.
    /// </summary>
    internal static EarthRelativeObservationResult Evaluate(in SimulationStateView state, StateRevision expectedRevision,
        PhysicalEventEpoch epoch, SpacecraftContactGeometry? geometry, ulong featureId,
        CelestialSystemDefinition system, ReferenceFrameGraph graph, IPhysicalSurfacePointQuery? query,
        in PhysicalSurfaceAuthorityIdentity expectedAuthority, Span<ReferenceFrameEvaluation> evaluations,
        Span<FrameTransform> roots, Span<ReferenceFrameEvaluation> staging, Span<FrameTransform> stagingRoots,
        out SpacecraftPhysicalEventContactObservation result)
    {
        result = default;
        if (geometry is null || geometry.Count != 1) return new(ContactGenerationStatus.InvalidGeometry, EarthRelativeObservationFailure.InvalidGeometry);
        if (geometry.GetFeature(0).Id != featureId) return new(ContactGenerationStatus.InvalidGeometry, EarthRelativeObservationFailure.InvalidFeature);
        if (state.Revision != expectedRevision) return new(ContactGenerationStatus.StaleState, EarthRelativeObservationFailure.StaleRevision);
        if (expectedAuthority.BodyId != SolarSystemBodyIds.Earth.Value) return new(ContactGenerationStatus.BodyMismatch, EarthRelativeObservationFailure.UnsupportedBody);
        var earthStatus = EarthPhysicalEventEvaluator.TryEvaluate(system, graph, epoch, evaluations, roots, staging, stagingRoots, out var earth);
        if (earthStatus != EarthPhysicalEventStatus.Ready) return EarthFailure(earthStatus);
        if (epoch.TryGetCanonicalInstant(out var canonical))
        {
            // Managed-reference-containing value on the stack: one caller-independent span, no heap buffer.
            SpacecraftContactObservation value = default;
            var output = MemoryMarshal.CreateSpan(ref value, 1);
            var canonicalResult = SpacecraftContactGenerator.Generate(state, expectedRevision, canonical, geometry,
                earth.Canonical, system, query, expectedAuthority, output);
            if (!canonicalResult.Succeeded) return CanonicalFailure(canonicalResult, state, geometry);
            result = new(true, value.Feature, epoch, value.SpacecraftRevision, system.Id, earth.Body, value.Root,
                value.TerrainAuthority, value.FeaturePositionRoot, value.TerrainDirectionBodyFixed, value.TerrainPositionRoot,
                value.PhysicalNormalRoot, value.RadialSignedGapMetres, value.FeatureVelocityRoot,
                value.TerrainVelocityRoot, value.RelativeVelocityRoot);
            return new(ContactGenerationStatus.Ready);
        }
        if (query is null) return new(ContactGenerationStatus.SurfaceUnavailable, EarthRelativeObservationFailure.TerrainUnavailable, PhysicalSurfaceQueryStatus.RequiredDataNotReady);
        if (query.Authority != expectedAuthority || !double.IsFinite(expectedAuthority.ReferenceRadiusMetres) || expectedAuthority.ReferenceRadiusMetres <= 0)
            return new(ContactGenerationStatus.AuthorityMismatch, EarthRelativeObservationFailure.AuthorityMismatch);
        var motionStatus = SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(state, geometry.Spacecraft, epoch, out var motion);
        if (motionStatus != SpacecraftTranslationStatus.Success)
            return new(ContactGenerationStatus.MotionUnavailable, motionStatus switch
            {
                SpacecraftTranslationStatus.SubjectNotFound => EarthRelativeObservationFailure.SpacecraftMissing,
                SpacecraftTranslationStatus.DurationOverflow => EarthRelativeObservationFailure.DurationCapacity,
                SpacecraftTranslationStatus.NonFiniteResult => EarthRelativeObservationFailure.NonFinite,
                _ => EarthRelativeObservationFailure.SpacecraftEvaluation,
            });
        if (motion.RootFrame != earth.Root) return new(ContactGenerationStatus.RootMismatch, EarthRelativeObservationFailure.RootMismatch);
        return Compose(motion, earth, geometry, query, expectedAuthority, out result);
    }

    // Only this evaluator combines freshly evaluated inputs; no public/internal bypass accepting fabricated samples.
    private static EarthRelativeObservationResult Compose(in SpacecraftPhysicalEventMotion motion, in EarthPhysicalEventMotion earth,
        SpacecraftContactGeometry geometry, IPhysicalSurfacePointQuery query, in PhysicalSurfaceAuthorityIdentity authority,
        out SpacecraftPhysicalEventContactObservation result)
    {
        result = default;
        if (motion.Epoch != earth.Epoch || motion.RootFrame != earth.Root || motion.Spacecraft != geometry.Spacecraft || geometry.Count != 1)
            return new(ContactGenerationStatus.TimeMismatch, EarthRelativeObservationFailure.AuthorityMismatch);
        var feature = geometry.GetFeature(0);
        var craftPose = new FrameTransform(motion.PositionRoot, motion.BodyToRoot);
        var qRoot = craftPose.LocalToParent(feature.OffsetFromComMetres);
        var qBody = earth.BodyFixedToRoot.ParentToLocal(qRoot);
        var radius = Math.Sqrt(qBody.LengthSquared);
        if (!qRoot.IsFinite || !qBody.IsFinite || !double.IsFinite(radius) || radius <= 0)
            return new(ContactGenerationStatus.NonFiniteResult, EarthRelativeObservationFailure.NonFinite);
        var surface = query.Query(earth.Body.Value, qBody / radius);
        if (!surface.IsReady) return new(ContactGenerationStatus.SurfaceUnavailable,
            surface.Status == PhysicalSurfaceQueryStatus.NormalUnqualified ? EarthRelativeObservationFailure.NormalUnqualified : EarthRelativeObservationFailure.TerrainUnavailable, surface.Status);
        if (surface.Authority != authority) return new(ContactGenerationStatus.AuthorityMismatch, EarthRelativeObservationFailure.AuthorityMismatch);
        var witness = earth.BodyFixedToRoot.LocalToParent(surface.BodyFixedPositionMetres);
        var normal = earth.BodyFixedToRoot.LocalDirectionToParent(surface.PhysicalNormal);
        var vFeature = ReferenceFrameMath.ResolveVelocityToRoot(craftPose, motion.VelocityRoot,
            motion.BodyToRoot.Rotate(motion.AngularVelocityBody), feature.OffsetFromComMetres, Double3.Zero);
        var vTerrain = ReferenceFrameMath.ResolveVelocityToRoot(earth.BodyFixedToRoot, earth.VelocityRoot,
            earth.AngularVelocityRoot, surface.BodyFixedPositionMetres, Double3.Zero);
        var relative = vFeature - vTerrain;
        var gap = (radius - authority.ReferenceRadiusMetres) - surface.HeightMetres;
        if (!witness.IsFinite || !normal.IsFinite || normal.LengthSquared <= 0 || !vFeature.IsFinite ||
            !vTerrain.IsFinite || !relative.IsFinite || !double.IsFinite(gap))
            return new(ContactGenerationStatus.NonFiniteResult, EarthRelativeObservationFailure.NonFinite);
        result = new(true, new(geometry.Spacecraft, geometry.Identity, feature.Id), motion.Epoch, motion.Revision,
            earth.System, earth.Body, earth.Root, authority, qRoot, surface.BodyFixedDirection, witness, normal,
            gap, vFeature, vTerrain, relative);
        return new(ContactGenerationStatus.Ready);
    }

    private static EarthRelativeObservationResult EarthFailure(EarthPhysicalEventStatus status) => status switch
    {
        EarthPhysicalEventStatus.UnsupportedModel => new(ContactGenerationStatus.UnsupportedBody, EarthRelativeObservationFailure.UnsupportedCelestialModel),
        EarthPhysicalEventStatus.UnsupportedTimeMapping => new(ContactGenerationStatus.UnsupportedBody, EarthRelativeObservationFailure.UnsupportedTimeMapping),
        EarthPhysicalEventStatus.OutsideCoverage => new(ContactGenerationStatus.BodyEvaluationFailed, EarthRelativeObservationFailure.EpochOutsideRange),
        EarthPhysicalEventStatus.DurationCapacity => new(ContactGenerationStatus.BodyEvaluationFailed, EarthRelativeObservationFailure.DurationCapacity),
        EarthPhysicalEventStatus.RootMismatch => new(ContactGenerationStatus.RootMismatch, EarthRelativeObservationFailure.RootMismatch),
        EarthPhysicalEventStatus.InvalidBuffers => new(ContactGenerationStatus.InvalidFrame, EarthRelativeObservationFailure.InvalidBuffers),
        _ => new(ContactGenerationStatus.BodyEvaluationFailed, EarthRelativeObservationFailure.EarthEvaluation),
    };

    private static EarthRelativeObservationResult CanonicalFailure(ContactGenerationResult value,
        in SimulationStateView state, SpacecraftContactGeometry geometry) => new(value.Status, value.Status switch
    {
        ContactGenerationStatus.MotionUnavailable => state.Spacecraft.TryGetTranslation(geometry.Spacecraft, out _, out _)
            ? EarthRelativeObservationFailure.SpacecraftEvaluation : EarthRelativeObservationFailure.SpacecraftMissing,
        ContactGenerationStatus.SurfaceUnavailable => value.SurfaceStatus == PhysicalSurfaceQueryStatus.NormalUnqualified
            ? EarthRelativeObservationFailure.NormalUnqualified : EarthRelativeObservationFailure.TerrainUnavailable,
        ContactGenerationStatus.AuthorityMismatch => EarthRelativeObservationFailure.AuthorityMismatch,
        ContactGenerationStatus.RootMismatch => EarthRelativeObservationFailure.RootMismatch,
        ContactGenerationStatus.StaleState => EarthRelativeObservationFailure.StaleRevision,
        _ => EarthRelativeObservationFailure.NonFinite,
    }, value.SurfaceStatus);
}
