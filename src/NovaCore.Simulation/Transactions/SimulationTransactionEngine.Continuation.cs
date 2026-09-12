using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Spacecraft.Translation;
using Proof = NovaCore.Simulation.Spacecraft.Contact.FloridaContactProvider.Proof;

namespace NovaCore.Simulation.Transactions;

internal sealed partial class SimulationTransactionEngine
{
    private readonly ProcessedCertifiedContinuation[] _continuationHistory;
    private int _continuationCount;
    internal int ProcessedContinuationCount { get { _clock.PublicationPhase.VerifyRead(); return _continuationCount; } }
    internal int ContinuationHistoryCapacity => _continuationHistory.Length;

    internal bool TryGetProcessedContinuation(int index, out ProcessedCertifiedContinuation record)
    {
        _clock.PublicationPhase.VerifyRead();
        if ((uint)index < (uint)_continuationCount) { record = _continuationHistory[index]; return true; }
        record = default;
        return false;
    }

    internal ContinuationClockState CaptureContinuationClock() =>
        new(_clock.CurrentTime, _clock.PendingSimulationDebt, _clock.Rate, _clock.RateRemainder, _clock.IsPaused);

    /// <summary>
    /// The sole continuation publication entry. Receipt checks, immutable preparation and final live
    /// admission share one exclusive phase. The private commit accepts only this prepared result.
    /// No arbitrary endpoint, replay record or constructible snapshot is accepted as authority.
    /// </summary>
    internal ContinuationPublicationResult PublishCertifiedContinuation(in CertifiedContinuationRequest request,
        in FloridaContactUse current)
    {
        var phase = _clock.PublicationPhase;
        if (!phase.IsOwnerThread) return new(ContinuationPublicationStatus.WrongOwnerThread);
        if (_isExecutingGroup || !phase.TryEnter(this)) return new(ContinuationPublicationStatus.ReentrantPublication);
        try
        {
            var status = PrepareCertifiedContinuation(request, current, out var prepared);
            if (status != ContinuationPublicationStatus.Published) return new(status);
            status = RecheckCertifiedContinuation(request, current, prepared);
            if (status != ContinuationPublicationStatus.Published) return new(status);
            CommitCertifiedContinuation(prepared);
            return new(ContinuationPublicationStatus.Published, prepared.Observation);
        }
        finally { phase.Exit(); }
    }

    internal bool TryCaptureContinuationObservation(SpacecraftId craft, out ContinuationPublicationObservation observation)
    {
        observation = default;
        var phase = _clock.PublicationPhase;
        if (!phase.TryEnter(this)) return false;
        try
        {
            var state = _state.CreateView();
            if (!state.Spacecraft.TryGetTranslation(craft, out var linear, out var properties) ||
                !state.Spacecraft.TryGetRigidBody(craft, out var angular)) return false;
            observation = new(craft, linear, angular, properties, CaptureContinuationClock(), state.Revision,
                _clock.Timeline.Revision, _continuationCount, _continuationCount - 1);
            return true;
        }
        finally { phase.Exit(); }
    }

    private ContinuationPublicationStatus PrepareCertifiedContinuation(in CertifiedContinuationRequest request,
        in FloridaContactUse current, out PreparedContinuationPublication prepared)
    {
        prepared = default;
        if (!_clock.PublicationPhase.IsOwnedBy(this)) return ContinuationPublicationStatus.ReentrantPublication;
        if (!ReferenceEquals(current.Engine, this)) return ContinuationPublicationStatus.ForeignEngine;
        if (current.Geometry is null || current.System is null || current.Graph is null || current.Terrain is null ||
            !request.Root.IsRoot) return ContinuationPublicationStatus.InvalidInput;
        if (CaptureContinuationClock() != request.Clock) return ContinuationPublicationStatus.ClockConflict;
        var state = _state.CreateView();
        if (state.Revision != request.StateRevision) return ContinuationPublicationStatus.StateConflict;
        if (_clock.Timeline.Revision != request.TimelineRevision) return ContinuationPublicationStatus.TimelineConflict;
        if (HasContactProofBoundaryThrough(request.Target)) return ContinuationPublicationStatus.PendingEvent;

        var endpointStatus = request.Endpoint.Read(request.Root, request.Initial, request.Realization, request.Pose,
            request.Target, request.Propagation, current, out var staged);
        if (endpointStatus != PrivatePropagationStatus.Ready)
            return endpointStatus == PrivatePropagationStatus.Stale ? ContinuationPublicationStatus.StaleEndpoint :
                ContinuationPublicationStatus.InvalidEndpointEvidence;
        var clearanceStatus = request.Clearance.Read(request.Endpoint, current, out var clearance);
        if (clearanceStatus != CoverageStatus.EventFreeThroughTarget)
            return clearanceStatus == CoverageStatus.Stale ? ContinuationPublicationStatus.StaleClearance :
                ContinuationPublicationStatus.InvalidClearanceEvidence;

        var source = staged.Initial;
        if (request.Clock.Time != source.SourceStart) return ContinuationPublicationStatus.SourceTimeMismatch;
        if (request.Target != source.SourceEnd || request.Target <= source.SourceStart ||
            staged.Target != request.Target || clearance.Target != request.Target)
            return ContinuationPublicationStatus.InvalidTarget;
        // The applicable M14.15 receipt already certifies strict SourceStart < alpha < SourceEnd.
        // Do not replace that exact ordering with a rounded-double canonical-time comparison.
        if (source.SourceRevision != state.Revision || source.SourceTimelineRevision != request.TimelineRevision)
            return ContinuationPublicationStatus.StateConflict;
        if (current.Geometry.Count != 1 || clearance.Feature.Spacecraft != current.Geometry.Spacecraft ||
            clearance.Terrain != source.TerrainAuthority || clearance.Version != PostImpactCoverageSearch.Version ||
            staged.ArithmeticVersion != PrivatePropagationMath.Version) return ContinuationPublicationStatus.InvalidInput;

        var delta = (Int128)request.Target.Ticks - request.Clock.Time.Ticks;
        if (delta <= 0 || delta > long.MaxValue) return ContinuationPublicationStatus.ArithmeticOverflow;
        if (request.Clock.Debt.Ticks < delta) return ContinuationPublicationStatus.InsufficientDebt;
        if (!_state.TryPrepareContinuationSlot(source.FrozenSourceTranslation, source.FrozenSourceRotation, out var slot) ||
            !state.Spacecraft.TryGetTranslation(current.Geometry.Spacecraft, out var linear, out var properties) ||
            !state.Spacecraft.TryGetRigidBody(current.Geometry.Spacecraft, out var angular) ||
            !state.Spacecraft.TryGetDefinition(current.Geometry.Spacecraft, out var definition) ||
            linear != source.FrozenSourceTranslation || angular != source.FrozenSourceRotation || properties != source.Properties)
            return ContinuationPublicationStatus.StateSlotMismatch;
        var endpoint = staged.Endpoint;
        // Inspect represented values only. Never normalize, evaluate rotation, or recompute physics.
        if (!endpoint.PositionRoot.IsFinite || !endpoint.VelocityRoot.IsFinite || !endpoint.AngularVelocityBody.IsFinite ||
            !Finite(endpoint.OrientationBodyToRoot) || !properties.IsValid || !angular.PrincipalInertia.IsFinite ||
            endpoint.OrientationBodyToRoot == default || staged.EventCoverage != PrivatePropagationCoverage.Unknown)
            return ContinuationPublicationStatus.InvalidEndpoint;
        if (state.Revision.Value == ulong.MaxValue) return ContinuationPublicationStatus.StateRevisionOverflow;
        if (_continuationCount == _continuationHistory.Length) return ContinuationPublicationStatus.HistoryCapacityFailure;
        if (!current.Graph.TryGetNode(linear.RootFrame, out var rootFrame) ||
            !current.Graph.TryGetNode(definition.BodyFrame, out var bodyFrame)) return ContinuationPublicationStatus.InvalidInput;

        var replacementLinear = linear with { Epoch = request.Target, PositionRoot = endpoint.PositionRoot, VelocityRoot = endpoint.VelocityRoot };
        var replacementAngular = angular with { Epoch = request.Target, OrientationLocalToParent = endpoint.OrientationBodyToRoot,
            AngularVelocityBody = endpoint.AngularVelocityBody };
        var nextRevision = new StateRevision(state.Revision.Value + 1);
        var nextClock = request.Clock with { Time = request.Target, Debt = new(request.Clock.Debt.Ticks - (long)delta) };
        var provenance = new ContinuationPublicationProvenance(source.SourceStart, source.SourceEnd, request.Root.RootEnclosure,
            source.PoseRootEnclosure, request.Root.RefinementCount, definition, rootFrame, bodyFrame,
            current.Geometry.Identity, current.Geometry.GetFeature(0), source.TerrainAuthority, current.System.Id,
            SolAnalyticalDefinition.VersionName, current.System.EphemerisMetadata, current.System.TimeMapping,
            request.Initial.PublicationInputs, staged.Request, FloridaContactProvider.ProviderVersion, FloridaContactProvider.RelationVersion,
            Proof.PostImpactVelocity.PolicyVersion, Proof.PostImpactVelocity.NumericalVersion, Proof.PostImpactVelocity.RecipeVersion,
            Proof.PrivatePostImpactState.Version, PrivatePropagationMath.Version, PostImpactCoverageSearch.Version);
        var record = PrepareContinuationHistory(_continuationCount, linear, angular, replacementLinear, replacementAngular,
            properties, request.Clock, nextClock, state.Revision, nextRevision, request.TimelineRevision, provenance);
        var observation = new ContinuationPublicationObservation(linear.Spacecraft, replacementLinear, replacementAngular, properties,
            nextClock, nextRevision, request.TimelineRevision, _continuationCount + 1, _continuationCount);
        prepared = new(slot, record, observation);
        return ContinuationPublicationStatus.Published;
    }

    private static ProcessedCertifiedContinuation PrepareContinuationHistory(int index,
        in Spacecraft.Translation.SpacecraftTranslationState beforeLinear, in Spacecraft.Rotation.SpacecraftRigidBodyRotationState beforeAngular,
        in Spacecraft.Translation.SpacecraftTranslationState afterLinear, in Spacecraft.Rotation.SpacecraftRigidBodyRotationState afterAngular,
        SpacecraftPhysicalProperties properties, ContinuationClockState beforeClock, ContinuationClockState afterClock,
        StateRevision beforeRevision, StateRevision afterRevision, Timeline.TimelineRevision timeline,
        in ContinuationPublicationProvenance provenance) =>
        new(1, index, beforeLinear.Spacecraft, beforeLinear, beforeAngular, afterLinear, afterAngular, properties,
            beforeClock, afterClock, beforeRevision, afterRevision, timeline, provenance, ContinuationPublicationStatus.Published);

    private ContinuationPublicationStatus RecheckCertifiedContinuation(in CertifiedContinuationRequest request,
        in FloridaContactUse current, in PreparedContinuationPublication prepared)
    {
        if (!_clock.PublicationPhase.IsOwnedBy(this)) return ContinuationPublicationStatus.ReentrantPublication;
        if (CaptureContinuationClock() != request.Clock) return ContinuationPublicationStatus.ClockConflict;
        if (_state.CreateView().Revision != request.StateRevision) return ContinuationPublicationStatus.StateConflict;
        if (_clock.Timeline.Revision != request.TimelineRevision || HasContactProofBoundaryThrough(request.Target))
            return ContinuationPublicationStatus.TimelineConflict;
        if (_continuationCount != prepared.Record.Index || _continuationCount >= _continuationHistory.Length)
            return ContinuationPublicationStatus.HistoryCapacityFailure;
        if (!_state.TryPrepareContinuationSlot(prepared.Record.BeforeTranslation, prepared.Record.BeforeRotation, out var slot) ||
            slot != prepared.Slot) return ContinuationPublicationStatus.StateSlotMismatch;
        // Final checked coverage transitively rechecks both staged/source chains against this exact current engine.
        if (request.Clearance.Read(request.Endpoint, current, out var coverage) != CoverageStatus.EventFreeThroughTarget ||
            coverage.Target != request.Target) return ContinuationPublicationStatus.FinalApplicabilityFailure;
        return ContinuationPublicationStatus.Published;
    }

    // WRITE-ONLY COMMIT: fixed validated slots, prepared values and preallocated record.
    // No ordinary failure, lookup, provider query, allocation, arithmetic or callback remains.
    private void CommitCertifiedContinuation(in PreparedContinuationPublication prepared)
    {
        _state.InstallCertifiedContinuation(prepared.Slot, prepared.Record.AfterTranslation,
            prepared.Record.AfterRotation, prepared.Record.AfterRevision);
        _clock.InstallCertifiedContinuation(prepared.Record.AfterClock.Time, prepared.Record.AfterClock.Debt);
        _continuationHistory[prepared.Record.Index] = prepared.Record;
        _continuationCount = prepared.Observation.PublicationCount;
    }

    private static bool Finite(DoubleQuaternion q) => double.IsFinite(q.X) && double.IsFinite(q.Y) && double.IsFinite(q.Z) && double.IsFinite(q.W);
}
