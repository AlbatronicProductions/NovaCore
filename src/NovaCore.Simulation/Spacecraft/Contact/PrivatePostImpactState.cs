using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum PrivatePostImpactStateStatus : byte { Unresolved, Ready, Unsupported, Stale }
internal enum PrivatePostImpactStateFailure : byte
{
    None, InvalidWitness, InvalidRequest, ChangedAuthority, InvalidSource,
    UnsupportedArithmetic, NonFinitePose, PoseResolution,
}

/// <summary>Maximum full COM-position enclosure width per component, not a contact tolerance.</summary>
internal readonly record struct PrivatePostImpactPoseRequest(double PositionComponentMetres)
{
    internal bool IsValid => double.IsFinite(PositionComponentMetres) && PositionComponentMetres > 0;
}

/// <summary>
/// One private initial state at Root's alpha. Frozen source epochs describe the pre-impact model,
/// not the new initial instant. PositionRoot encloses the source expression at that same alpha;
/// its coordinates cannot be independently selected. FrozenSourceRotation's stored quaternion
/// denotes its exact normalization. Initial velocities are final certified bits, not increments.
/// Constructible snapshot data grants no execution or publication authority; use the checked receipt.
/// </summary>
internal readonly record struct PrivatePostImpactStateValues(
    FloridaContactProvider.Proof Root, FloridaBound PoseRootEnclosure,
    SpacecraftTranslationState FrozenSourceTranslation, SpacecraftRigidBodyRotationState FrozenSourceRotation,
    SpacecraftPhysicalProperties Properties, StateRevision SourceRevision, TimelineRevision SourceTimelineRevision,
    SimulationInstant SourceStart, SimulationInstant SourceEnd, PhysicalSurfaceAuthorityIdentity TerrainAuthority,
    FloridaVector PositionRoot, Double3 InitialLinearVelocityRoot, Double3 InitialAngularVelocityBody);

internal readonly record struct PrivatePostImpactStateResult(PrivatePostImpactStateStatus Status,
    PrivatePostImpactStateFailure Failure, FloridaContactProvider.Proof.PrivatePostImpactState State = default);

/// <summary>Bounded qualification only: no root selection, refinement, state advancement or pose rounding.</summary>
internal static class PrivatePostImpactPose
{
    internal static PrivatePostImpactStateFailure Evaluate(in SpacecraftTranslationState source,
        in SpacecraftPhysicalProperties properties, FloridaBound root, in PrivatePostImpactPoseRequest request,
        out FloridaVector position)
    {
        position = default;
        if (!request.IsValid) return PrivatePostImpactStateFailure.InvalidRequest;
        if (!properties.IsValid || !source.Spacecraft.IsValid || source.RootFrame.Value == 0 || !root.IsFinite ||
            !source.PositionRoot.IsFinite || !source.VelocityRoot.IsFinite || !source.ConstantForceRoot.IsFinite)
            return PrivatePostImpactStateFailure.InvalidSource;
        if (!CertifiedPostImpactVelocityMath.ArithmeticSupported()) return PrivatePostImpactStateFailure.UnsupportedArithmetic;
        // Exact source-double interpretation, including division by mass and epoch ticks / 10^6.
        // Interval arithmetic encloses one correlated quadratic expression; it never chooses alpha.
        var elapsed = root - FloridaBound.Integer(source.Epoch.Ticks) / SimulationInstant.TicksPerSecond;
        var candidate = FloridaVector.From(source.PositionRoot) + FloridaVector.From(source.VelocityRoot) * elapsed +
            (FloridaVector.From(source.ConstantForceRoot) / properties.MassKilograms) * (elapsed.Square() / 2);
        if (!candidate.IsFinite) return PrivatePostImpactStateFailure.NonFinitePose;
        if (!FloridaKinematicsRequest.Fits(candidate, request.PositionComponentMetres))
            return PrivatePostImpactStateFailure.PoseResolution;
        position = candidate;
        return PrivatePostImpactStateFailure.None;
    }
}

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal PrivatePostImpactStateResult PreparePrivatePostImpactState(in Kinematics witness,
            in ResponseProposal response, in CertifiedResponseRequest responseRequest,
            in PreImpactVelocity preimpact, in CertifiedPreImpactVelocityRequest preimpactRequest,
            in PostImpactVelocity realization, in PrivatePostImpactPoseRequest poseRequest, in FloridaContactUse current) =>
            PrivatePostImpactState.Prepare(this, witness, response, responseRequest, preimpact, preimpactRequest,
                realization, poseRequest, current);

        /// <summary>
        /// Immutable provider-owned preparation. Every read rechecks the issuing tuple and current source.
        /// Single-writer applicability is not a concurrency lock or a consumed publication token.
        /// No mutable refinement generation: an applicable older receipt survives same-owner refinement.
        /// </summary>
        internal readonly partial struct PrivatePostImpactState
        {
            internal const uint Version = 1;
            private readonly Proof root;
            private readonly Kinematics witness;
            private readonly ResponseProposal response;
            private readonly CertifiedResponseRequest responseRequest;
            private readonly PreImpactVelocity preimpact;
            private readonly CertifiedPreImpactVelocityRequest preimpactRequest;
            private readonly PostImpactVelocity realization;
            private readonly PrivatePostImpactPoseRequest poseRequest;
            private readonly PrivatePostImpactStateValues values;

            private PrivatePostImpactState(in Proof root, in Kinematics witness, in ResponseProposal response,
                in CertifiedResponseRequest responseRequest, in PreImpactVelocity preimpact,
                in CertifiedPreImpactVelocityRequest preimpactRequest, in PostImpactVelocity realization,
                in PrivatePostImpactPoseRequest poseRequest, in PrivatePostImpactStateValues values)
            {
                this.root = root; this.witness = witness; this.response = response; this.responseRequest = responseRequest;
                this.preimpact = preimpact; this.preimpactRequest = preimpactRequest; this.realization = realization;
                this.poseRequest = poseRequest; this.values = values;
            }

            internal PrivatePostImpactStateStatus Read(in Proof expectedRoot, in PostImpactVelocity expectedRealization,
                in PrivatePostImpactPoseRequest expectedPoseRequest, in FloridaContactUse current,
                out PrivatePostImpactStateValues result)
            {
                result = default;
                if (!root.IsRoot || !root.SameResponseInput(expectedRoot) || poseRequest != expectedPoseRequest)
                    return PrivatePostImpactStateStatus.Unsupported;
                var status = realization.Read(root, witness, response, responseRequest, preimpact, preimpactRequest,
                    current, out var held);
                if (status != CertifiedPostImpactVelocityStatus.Qualified) return Map(status);
                status = expectedRealization.Read(expectedRoot, witness, response, responseRequest, preimpact,
                    preimpactRequest, current, out var supplied);
                if (status != CertifiedPostImpactVelocityStatus.Qualified) return Map(status);
                // Compare the complete certified realization, not just equal velocity components.
                if (held != supplied) return PrivatePostImpactStateStatus.Unsupported;
                result = values;
                return PrivatePostImpactStateStatus.Ready;
            }

            internal static PrivatePostImpactStateResult Prepare(in Proof root, in Kinematics witness,
                in ResponseProposal response, in CertifiedResponseRequest responseRequest,
                in PreImpactVelocity preimpact, in CertifiedPreImpactVelocityRequest preimpactRequest,
                in PostImpactVelocity realization, in PrivatePostImpactPoseRequest poseRequest, in FloridaContactUse current)
            {
                // Read the accepted final bits. Never requalify or reapply the response here.
                var status = realization.Read(root, witness, response, responseRequest, preimpact, preimpactRequest,
                    current, out var final);
                if (status != CertifiedPostImpactVelocityStatus.Qualified)
                    return new(Map(status), status == CertifiedPostImpactVelocityStatus.Stale
                        ? PrivatePostImpactStateFailure.ChangedAuthority : status == CertifiedPostImpactVelocityStatus.Unresolved
                        ? PrivatePostImpactStateFailure.UnsupportedArithmetic : PrivatePostImpactStateFailure.InvalidWitness);
                var k = witness.Read(root, current, out var input);
                if (k != FloridaKinematicsStatus.Qualified)
                    return new(k == FloridaKinematicsStatus.Stale ? PrivatePostImpactStateStatus.Stale : PrivatePostImpactStateStatus.Unsupported,
                        k == FloridaKinematicsStatus.Stale ? PrivatePostImpactStateFailure.ChangedAuthority : PrivatePostImpactStateFailure.InvalidWitness);
                var owner = root.owner!; // The complete checked issuing chain above established this owner.
                var failure = PrivatePostImpactPose.Evaluate(owner.linear, owner.properties, input.RootEnclosure, poseRequest, out var pose);
                if (failure != PrivatePostImpactStateFailure.None)
                    return new(failure is PrivatePostImpactStateFailure.InvalidRequest or PrivatePostImpactStateFailure.InvalidSource
                        ? PrivatePostImpactStateStatus.Unsupported : PrivatePostImpactStateStatus.Unresolved, failure);
                var captured = new PrivatePostImpactStateValues(root, input.RootEnclosure, owner.linear, owner.angular,
                    owner.properties, owner.stateRevision, owner.timelineRevision, owner.start, owner.end, owner.authority,
                    pose, final.LinearVelocityRoot, final.AngularVelocityBody);
                return new(PrivatePostImpactStateStatus.Ready, PrivatePostImpactStateFailure.None,
                    new(root, witness, response, responseRequest, preimpact, preimpactRequest, realization, poseRequest, captured));
            }

            private static PrivatePostImpactStateStatus Map(CertifiedPostImpactVelocityStatus status) => status switch
            {
                CertifiedPostImpactVelocityStatus.Qualified => PrivatePostImpactStateStatus.Ready,
                CertifiedPostImpactVelocityStatus.Stale => PrivatePostImpactStateStatus.Stale,
                CertifiedPostImpactVelocityStatus.Unresolved => PrivatePostImpactStateStatus.Unresolved,
                _ => PrivatePostImpactStateStatus.Unsupported,
            };
        }
    }
}
