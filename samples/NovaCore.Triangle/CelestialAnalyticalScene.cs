using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Celestial.ReferenceFrames;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using NovaCore.Interop;

/// <summary>Sample-only analytical celestial presentation. All celestial values remain SI until this class derives marker data.</summary>
internal sealed class CelestialAnalyticalScene
{
    internal const double MetresPerDisplayUnit = 10_000_000d;
    internal const long SampleRate = 10_000;
    internal const double RootMu = 3.986004418e14d;
    internal const double OrbitRadiusMetres = 100_000_000d;
    internal const double ImpulseMetresPerSecond = 200d;
    internal static readonly PrincipalMomentsOfInertia FixtureInertia = new(120d, 120d, 120d);
    // Presentation-only: deliberately calm at 1× while retaining visible multi-axis response.
    internal static readonly Double3 FixtureInitialAngularVelocity = new(.01d, .015d, .03d);
    // Over 5,000 s and 120 kg·m² this adds (.02, -.01, .006̅) rad/s.
    internal static readonly Double3 FixtureTorque = new(.00048d, -.00024d, .00016d);
    internal const double PlayerTorqueMagnitude = .00048d;
    private static readonly SimulationInstant ImpulseTime = SimulationInstant.FromWholeSeconds(100_000);
    private static readonly SimulationRate[] RateSteps = [new(1, 1), new(10, 1), new(100, 1), new(1_000, 1), new(5_000, 1), new(SampleRate, 1), new(50_000, 1)];

    private readonly ReferenceFrameId _rootFrame = new(1);
    private readonly ReferenceFrameId _satelliteFrame = new(2);
    private readonly ReferenceFrameGraph _graph;
    private readonly ReferenceFrameId _spacecraftFrame = new(3);
    private readonly ReferenceFrameEvaluation[] _evaluations = new ReferenceFrameEvaluation[3];
    private readonly ReferenceFrameId[] _sourcePath = new ReferenceFrameId[3];
    private readonly ReferenceFrameId[] _targetPath = new ReferenceFrameId[3];
    private readonly ReferenceFrameId[] _traversalPath = new ReferenceFrameId[5];
    private readonly ResolvedRenderObject[] _objects = new ResolvedRenderObject[3];
    private readonly Double3[] _orbitSamples = new Double3[AnalyticalOrbitSampler.VertexCount];
    private readonly UniversePosition[] _orbitPositions = new UniversePosition[AnalyticalOrbitSampler.VertexCount];
    private readonly SimulationClock _clock;
    private readonly SimulationTransactionEngine _transactions;
    private readonly SpacecraftId _spacecraftId = new(1);
    private Double3 _requestedTorque;
    private double _orbitDistance = 24d;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;
    private int _rateStepIndex = 5;
    private ResolvedOrbitCurve? _orbitCurve;
    private ResolvedOrbitCurve? _previousOrbitCurve;
    private ulong _orbitCurveKey;
    private Double3 _burnPointMetres;
    private bool _burnVisible;
    internal int OrbitCurveBuildCount { get; private set; }

    private CelestialAnalyticalScene(ReferenceFrameGraph graph, SimulationClock clock, SimulationTransactionEngine transactions)
    {
        _graph = graph;
        _clock = clock;
        _transactions = transactions;
    }

    public ResolvedRenderSnapshot CurrentSnapshot { get; private set; } = null!;
    public SimulationInstant CurrentTime => _clock.CurrentTime;
    public SimulationRate Rate => _clock.Rate;
    public bool IsPaused => _clock.IsPaused;
    public double OrbitDistance => _orbitDistance;
    public static FixtureCameraConfiguration Camera => new(
        new Double3(0d, 0d, 24d),
        new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1_000d),
        .1d);

    public static bool TryCreate(out CelestialAnalyticalScene? scene, out string error)
    {
        scene = null;
        try
        {
            var rootBody = new CelestialBodyId(1);
            var satelliteBody = new CelestialBodyId(2);
            var spacecraftId = new SpacecraftId(1);
            var rootFrame = new ReferenceFrameId(1);
            var satelliteFrame = new ReferenceFrameId(2);
            var speed = Math.Sqrt(RootMu / OrbitRadiusMetres);
            var definitions = new[]
            {
                new CelestialBodyDefinition(rootBody, null, rootFrame, RootMu),
                new CelestialBodyDefinition(satelliteBody, rootBody, satelliteFrame, 1d),
            };
            var states = new[]
            {
                CelestialBodyState.Root(rootBody),
                CelestialBodyState.Orbiting(satelliteBody, new TwoBodyTrajectory(rootBody, SimulationInstant.Zero,
                    new CartesianState(new Double3(OrbitRadiusMetres, 0d, 0d), new Double3(0d, speed, 0d)), TwoBodyPropagationModel.CartesianTwoBodyV1)),
            };
            if (!CelestialStateStore.TryCreate(definitions, states, out var celestial, out var celestialStatus) || celestial is null)
            {
                error = $"Celestial scene state failed: {celestialStatus}";
                return false;
            }
            if (SpacecraftRigidBodyRotationState.TryCreate(spacecraftId, SimulationInstant.Zero, DoubleQuaternion.Identity, FixtureInitialAngularVelocity, FixtureInertia, Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1, out var rotation) != SpacecraftRigidBodyRotationEvaluationStatus.Success)
            {
                error = "Celestial spacecraft rigid-body state failed.";
                return false;
            }
            if (!SpacecraftStateStore.TryCreateRigidBody([new SpacecraftDefinition(spacecraftId, satelliteFrame, new ReferenceFrameId(3), "celestial-spacecraft")], [rotation], out var spacecraft, out var spacecraftStatus) || spacecraft is null)
            { error = $"Celestial spacecraft state failed: {spacecraftStatus}"; return false; }

            var graphBuilder = new ReferenceFrameGraphBuilder();
            graphBuilder.Add(new ReferenceFrameNode(rootFrame, null, ReferenceFrameKind.Ecl, "celestial-root-inertial"));
            graphBuilder.Add(new ReferenceFrameNode(satelliteFrame, rootFrame, ReferenceFrameKind.Cce, "celestial-satellite-inertial"));
            graphBuilder.Add(new ReferenceFrameNode(new ReferenceFrameId(3), satelliteFrame, ReferenceFrameKind.Ccf, "celestial-spacecraft-body"));
            var graph = graphBuilder.Build();
            var timeline = new SimulationTimeline(initialCapacity: 1);
            if (!SimulationEventRequest.TryCreateCelestialImpulse(new SimulationEventId(1), ImpulseTime, 0, satelliteBody, new Double3(0d, ImpulseMetresPerSecond, 0d), out var impulse) ||
                timeline.Schedule(SimulationInstant.Zero, impulse).Status != SimulationScheduleStatus.Scheduled)
            {
                error = "Celestial scene impulse scheduling failed.";
                return false;
            }

            var clock = new SimulationClock(SimulationInstant.Zero, timeline, new SimulationRate(SampleRate, 1));
            var transactions = new SimulationTransactionEngine(clock, new SimulationState(celestial, spacecraft), initialHistoryCapacity: 32_768);
            var candidate = new CelestialAnalyticalScene(graph, clock, transactions);
            if (!candidate.TryPublishCandidate(false, out error)) return false;
            scene = candidate;
            error = string.Empty;
            return true;
        }
        catch (ArgumentException exception)
        {
            error = $"Celestial scene construction failed: {exception.Message}";
            return false;
        }
    }

    public bool TryAdvanceByHostDuration(SimulationDuration hostDuration, out string error)
        => TryAdvanceByHostDuration(hostDuration, default, out error);

    public bool TryAdvanceByHostDuration(SimulationDuration hostDuration, in NativeInputState input, out string error)
    {
        var host = _clock.AdvanceByHostDuration(hostDuration);
        if (host.Reason is SimulationHostAdvanceStopReason.Paused or SimulationHostAdvanceStopReason.NoWork)
        {
            error = string.Empty;
            return true;
        }
        if (host.Reason != SimulationHostAdvanceStopReason.Accepted)
        {
            error = $"Celestial host-duration conversion failed: {host.Reason}";
            return false;
        }

        var service = _transactions.ServicePendingHostDurationDebt();
        if (service.Reason is not (SimulationDebtServiceStopReason.Completed or SimulationDebtServiceStopReason.NoDebt))
        {
            error = $"Celestial clock execution failed: {service.Reason}";
            return false;
        }
        if (!TryApplyTorqueControl(input, out error)) return false;
        return TryPublishCandidate(false, out error);
    }

    private bool TryApplyTorqueControl(in NativeInputState input, out string error)
    {
        var command = CreateTorqueCommand(input);
        // Holding torque rebases at each simulation update; releasing commits zero exactly once.
        if (command.RequestedBodyTorque == _requestedTorque && command.RequestedBodyTorque == Double3.Zero) { error = string.Empty; return true; }
        var candidate = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(_transactions.State, command);
        if (candidate.Status == RigidBodyTorqueTransactionStatus.ReplacementNoOp) { _requestedTorque = command.RequestedBodyTorque; error = string.Empty; return true; }
        if (!candidate.Succeeded || candidate.Transaction is null) { error = $"Spacecraft torque candidate failed: {candidate.Status}"; return false; }
        var committed = _transactions.ValidateAndCommit(candidate.Transaction.Value);
        if (!committed.Committed) { error = $"Spacecraft torque commit failed: {committed.Status}"; return false; }
        _requestedTorque = command.RequestedBodyTorque; error = string.Empty; return true;
    }

    private SpacecraftTorqueCommand CreateTorqueCommand(in NativeInputState input) => new(
        _spacecraftId,
        new Double3(
            ((input.MoveForward != 0 ? 1d : 0d) - (input.MoveBackward != 0 ? 1d : 0d)) * PlayerTorqueMagnitude,
            ((input.MoveLeft != 0 ? 1d : 0d) - (input.MoveRight != 0 ? 1d : 0d)) * PlayerTorqueMagnitude,
            ((input.MoveDown != 0 ? 1d : 0d) - (input.MoveUp != 0 ? 1d : 0d)) * PlayerTorqueMagnitude),
        _clock.CurrentTime);

    internal void ApplyPresentationInput(CameraState camera, in NativeInputState input, out bool rateChanged, out bool pauseChanged)
    {
        rateChanged = false;
        pauseChanged = false;
        if (input.RateDecrease != 0 && _rateStepIndex > 0) { _rateStepIndex--; _clock.TrySetRate(RateSteps[_rateStepIndex]); rateChanged = true; }
        if (input.RateIncrease != 0 && _rateStepIndex + 1 < RateSteps.Length) { _rateStepIndex++; _clock.TrySetRate(RateSteps[_rateStepIndex]); rateChanged = true; }
        if (input.PauseToggle != 0) { if (_clock.IsPaused) _clock.Resume(); else _clock.Pause(); pauseChanged = true; }

        var changed = false;
        if (input.MouseWheelDetents != 0)
        {
            _orbitDistance = Math.Clamp(_orbitDistance * Math.Pow(1.1d, -input.MouseWheelDetents), 2d, 500d);
            changed = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            _orbitYawRadians -= input.MouseDeltaX * .002d;
            _orbitPitchRadians = Math.Clamp(_orbitPitchRadians - input.MouseDeltaY * .002d, -1.45d, 1.45d);
            changed = true;
        }
        if (changed) ApplyOrbitPose(camera);
    }

    internal void ResetPresentationCamera(CameraState camera)
    {
        _orbitDistance = 24d;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = 0d;
        ApplyOrbitPose(camera);
    }

    internal bool TryBuildCandidateForTest(out ResolvedRenderSnapshot? snapshot, out string error) => TryBuildCandidate(false, out snapshot, out error);
    internal bool TryPublishCandidateForTest(bool forceInvalidMesh, out string error) => TryPublishCandidate(forceInvalidMesh, out error);

    private bool TryPublishCandidate(bool forceInvalidMesh, out string error)
    {
        if (!TryBuildCandidate(forceInvalidMesh, out var candidate, out error) || candidate is null) return false;
        CurrentSnapshot = candidate;
        return true;
    }

    private bool TryBuildCandidate(bool forceInvalidMesh, out ResolvedRenderSnapshot? snapshot, out string error)
    {
        var state = _transactions.State;
        var view = state.Celestial;
        var extraction = CelestialReferenceFrameEvaluator.TryEvaluate(view, _graph, _clock.CurrentTime, _evaluations);
        if (extraction != CelestialReferenceFrameEvaluationStatus.Success)
        {
            snapshot = null;
            error = $"Celestial frame extraction failed: {extraction}";
            return false;
        }
        var spacecraftExtraction = SpacecraftReferenceFrameEvaluator.TryEvaluate(state.Spacecraft, _graph, _clock.CurrentTime, _evaluations);
        if (spacecraftExtraction != SpacecraftReferenceFrameEvaluationStatus.Success)
        {
            snapshot = null;
            error = $"Spacecraft frame extraction failed: {spacecraftExtraction}";
            return false;
        }

        try
        {
            var transforms = new ReferenceFrameTransformSet(_graph, _evaluations);
            if (!TryResolve(transforms, _rootFrame, out var root, out error) || !TryResolve(transforms, _satelliteFrame, out var satellite, out error) || !TryResolve(transforms, _spacecraftFrame, out var spacecraft, out error))
            {
                snapshot = null;
                return false;
            }

            _objects[0] = Marker(1, root.ConvertPosition(Double3.Zero), new Double3(20d, 20d, 1d), MeshHandle.Triangle);
            _objects[1] = Marker(2, spacecraft.ConvertPosition(Double3.Zero), new Double3(12d, 4d, 1d), forceInvalidMesh ? MeshHandle.Invalid : MeshHandle.Triangle, spacecraft.SourceToTarget.Rotation);
            _objects[2] = Marker(3, _burnPointMetres, _burnVisible ? new Double3(2.5d, 2.5d, 1d) : Double3.Zero, MeshHandle.Triangle);
            if (!view.TryGetState(new CelestialBodyId(2), out var satelliteState) || satelliteState.Trajectory is not { } trajectory || !view.TryGetDefinition(trajectory.CentralBody, out var central))
            {
                snapshot = null;
                error = "Celestial orbit curve source is unavailable.";
                return false;
            }
            var curveKey = AnalyticalOrbitSampler.ComputeIdentityKey(trajectory, central.GravitationalParameter);
            var candidateCurve = _orbitCurve;
            var candidatePreviousCurve = _previousOrbitCurve;
            var rebuildCurve = candidateCurve is null || curveKey != _orbitCurveKey;
            if (rebuildCurve)
            {
                var sampled = AnalyticalOrbitSampler.TrySample(trajectory, view, _orbitSamples);
                if (sampled.Status != AnalyticalOrbitSamplingStatus.Success)
                {
                    snapshot = null;
                    error = $"Celestial orbit sampling failed: {sampled.Status}";
                    return false;
                }
                for (var index = 0; index < sampled.VertexCount; index++) _orbitPositions[index] = new UniversePosition(_orbitSamples[index] / MetresPerDisplayUnit, _rootFrame);
                if (!ResolvedOrbitCurve.TryCreate(_orbitPositions, out candidateCurve) || candidateCurve is null)
                {
                    snapshot = null;
                    error = "Celestial orbit curve construction failed.";
                    return false;
                }
                if (_orbitCurve is not null) candidatePreviousCurve = _orbitCurve;
            }
            if (rebuildCurve && _orbitCurve is not null && trajectory.Epoch == ImpulseTime) { _burnPointMetres = trajectory.StateAtEpoch.Position; _burnVisible = true; _objects[2] = Marker(3, _burnPointMetres, new Double3(2.5d, 2.5d, 1d), MeshHandle.Triangle); }
            if (!ResolvedRenderSnapshot.TryCreate(_objects, candidateCurve, candidatePreviousCurve, out snapshot, out var snapshotStatus) || snapshot is null)
            {
                error = $"Celestial render snapshot failed: {snapshotStatus}";
                return false;
            }
            if (rebuildCurve) { _previousOrbitCurve = candidatePreviousCurve; _orbitCurve = candidateCurve; _orbitCurveKey = curveKey; OrbitCurveBuildCount++; }
            error = string.Empty;
            return true;
        }
        catch (ArgumentException exception)
        {
            snapshot = null;
            error = $"Celestial transform candidate failed: {exception.Message}";
            return false;
        }
    }

    private ResolvedRenderObject Marker(uint id, in Double3 resolvedMetres, in Double3 scale, MeshHandle mesh, DoubleQuaternion? rotation = null) =>
        new(new RenderObjectId(id), new UniversePosition(resolvedMetres / MetresPerDisplayUnit, _rootFrame), rotation ?? DoubleQuaternion.Identity, scale, mesh);

    private bool TryResolve(ReferenceFrameTransformSet transforms, ReferenceFrameId source, out ResolvedReferenceFrameTransform resolved, out string error)
    {
        var status = ReferenceFrameTransformResolver.TryResolveTransform(transforms, source, _rootFrame, _sourcePath, _targetPath, _traversalPath, out resolved);
        error = status == ReferenceFrameTransformResolutionStatus.Success ? string.Empty : $"Celestial frame resolution failed: {source.Value}->{_rootFrame.Value}: {status}";
        return status == ReferenceFrameTransformResolutionStatus.Success;
    }

    private void ApplyOrbitPose(CameraState camera)
    {
        var yaw = DoubleQuaternion.FromAxisAngle(Double3.UnitY, _orbitYawRadians);
        var pitch = DoubleQuaternion.FromAxisAngle(Double3.UnitX, _orbitPitchRadians);
        var orientation = (yaw * pitch).Normalized();
        var forward = orientation.Rotate(new Double3(0d, 0d, -1d));
        camera.Orientation = orientation;
        camera.Position = camera.Position with { Value = -forward * _orbitDistance };
        camera.Validate();
    }
}
