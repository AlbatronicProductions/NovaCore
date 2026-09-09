using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum ContactGenerationStatus : byte
{
    Uninitialized, Ready, InvalidGeometry, UnsupportedBody, BodyMismatch, RootMismatch, TimeMismatch,
    InvalidFrame, BodyEvaluationFailed, MotionUnavailable, AuthorityMismatch, SurfaceUnavailable,
    InsufficientCapacity, NonFiniteResult, StaleState,
}

internal readonly record struct ContactFeatureIdentity(SpacecraftId Spacecraft, ContactGeometryIdentity Geometry, ulong FeatureId);

/// <summary>Point-only observation. RadialSignedGapMetres is neither closest-point distance nor finite-volume penetration depth.</summary>
internal readonly record struct SpacecraftContactObservation(
    bool IsReady, ContactFeatureIdentity Feature, PhysicalSurfaceAuthorityIdentity TerrainAuthority,
    StateRevision SpacecraftRevision, SimulationInstant Time, ReferenceFrameId Root,
    Double3 FeaturePositionRoot, Double3 TerrainPositionRoot, Double3 TerrainDirectionBodyFixed,
    Double3 PhysicalNormalRoot, double RadialSignedGapMetres,
    Double3 FeatureVelocityRoot, Double3 TerrainVelocityRoot, Double3 RelativeVelocityRoot);

internal readonly record struct ContactGenerationResult(ContactGenerationStatus Status, int Written,
    ulong FailedFeatureId = 0, PhysicalSurfaceQueryStatus SurfaceStatus = default)
{ internal bool Succeeded => Status == ContactGenerationStatus.Ready; }

internal static class SpacecraftContactGenerator
{
    /// <summary>
    /// Call in the single-writer simulation phase after servicing canonical events. Supply the current
    /// authoritative revision independently of any retained view: views contain live-backed spacecraft storage.
    /// Body motion must come from the application's current immutable celestial definition.
    /// Only [0, Written) is published; failure clears the attempted output prefix so old observations cannot masquerade as new ones.
    /// No contact state, force, torque or response is stored or changed.
    /// </summary>
    internal static ContactGenerationResult Generate(in SimulationStateView state, StateRevision expectedRevision, SimulationInstant time,
        SpacecraftContactGeometry? geometry, in ContactBodyMotion body, CelestialSystemDefinition expectedSystem,
        IPhysicalSurfacePointQuery? query, in PhysicalSurfaceAuthorityIdentity expectedAuthority,
        Span<SpacecraftContactObservation> destination)
        => GenerateWithMotion(state, expectedRevision, time, geometry, body, expectedSystem, query, expectedAuthority, destination, out _);

    // The response receipt retains this same evaluated motion; no second frame evaluation or terrain query.
    internal static ContactGenerationResult GenerateWithMotion(in SimulationStateView state, StateRevision expectedRevision, SimulationInstant time,
        SpacecraftContactGeometry? geometry, in ContactBodyMotion body, CelestialSystemDefinition expectedSystem,
        IPhysicalSurfacePointQuery? query, in PhysicalSurfaceAuthorityIdentity expectedAuthority,
        Span<SpacecraftContactObservation> destination, out SpacecraftMotion motion)
    {
        motion = default;
        if (geometry is null) { destination.Clear(); return new(ContactGenerationStatus.InvalidGeometry, 0); }
        var output = destination[..Math.Min(destination.Length, geometry.Count)];
        output.Clear();
        if (destination.Length < geometry.Count) return new(ContactGenerationStatus.InsufficientCapacity, 0);
        if (state.Revision != expectedRevision) return new(ContactGenerationStatus.StaleState, 0);
        if (!body.IsReady || !ReferenceEquals(body.System, expectedSystem)) return new(ContactGenerationStatus.InvalidFrame, 0);
        if (body.Time != time) return new(ContactGenerationStatus.TimeMismatch, 0);
        if (body.Body.Value != expectedAuthority.BodyId) return new(ContactGenerationStatus.BodyMismatch, 0);
        if (query is null) return new(ContactGenerationStatus.SurfaceUnavailable, 0, 0, PhysicalSurfaceQueryStatus.RequiredDataNotReady);
        if (query.Authority != expectedAuthority) return new(ContactGenerationStatus.AuthorityMismatch, 0);
        if (!double.IsFinite(expectedAuthority.ReferenceRadiusMetres) || expectedAuthority.ReferenceRadiusMetres <= 0)
            return new(ContactGenerationStatus.AuthorityMismatch, 0);
        var status = SpacecraftMotionEvaluator.TryEvaluate(state, geometry.Spacecraft, time, out motion);
        if (status != SpacecraftTranslationStatus.Success) return new(ContactGenerationStatus.MotionUnavailable, 0);
        if (motion.RootFrame != body.Root) return new(ContactGenerationStatus.RootMismatch, 0);
        var craftPose = new FrameTransform(motion.PositionRoot, motion.BodyToRoot);
        var craftOmega = motion.BodyToRoot.Rotate(motion.AngularVelocityBody);
        for (var i = 0; i < geometry.Count; i++)
        {
            var feature = geometry.GetFeature(i);
            var qRoot = craftPose.LocalToParent(feature.OffsetFromComMetres);
            var qBody = body.BodyFixedToRoot.ParentToLocal(qRoot);
            var radius = Math.Sqrt(qBody.LengthSquared);
            if (!qRoot.IsFinite || !qBody.IsFinite || !double.IsFinite(radius) || radius <= 0)
            { output.Clear(); return new(ContactGenerationStatus.NonFiniteResult, 0, feature.Id); }
            var direction = qBody / radius;
            var surface = query.Query(body.Body.Value, direction);
            if (!surface.IsReady)
            { output.Clear(); return new(ContactGenerationStatus.SurfaceUnavailable, 0, feature.Id, surface.Status); }
            if (surface.Authority != expectedAuthority)
            { output.Clear(); return new(ContactGenerationStatus.AuthorityMismatch, 0, feature.Id); }
            var sRoot = body.BodyFixedToRoot.LocalToParent(surface.BodyFixedPositionMetres);
            var normal = body.BodyFixedToRoot.LocalDirectionToParent(surface.PhysicalNormal);
            var vFeature = ReferenceFrameMath.ResolveVelocityToRoot(craftPose, motion.VelocityRoot, craftOmega, feature.OffsetFromComMetres, Double3.Zero);
            var vSurface = ReferenceFrameMath.ResolveVelocityToRoot(body.BodyFixedToRoot, body.VelocityRoot, body.AngularVelocityRoot, surface.BodyFixedPositionMetres, Double3.Zero);
            var relative = vFeature - vSurface;
            var gap = (radius - expectedAuthority.ReferenceRadiusMetres) - surface.HeightMetres;
            if (!sRoot.IsFinite || !normal.IsFinite || normal.LengthSquared <= 0 || !vFeature.IsFinite || !vSurface.IsFinite || !relative.IsFinite || !double.IsFinite(gap))
            { output.Clear(); return new(ContactGenerationStatus.NonFiniteResult, 0, feature.Id); }
            output[i] = new(true, new(geometry.Spacecraft, geometry.Identity, feature.Id), expectedAuthority,
                motion.Revision, time, motion.RootFrame, qRoot, sRoot, surface.BodyFixedDirection,
                normal, gap, vFeature, vSurface, relative);
        }
        return new(ContactGenerationStatus.Ready, geometry.Count);
    }
}
