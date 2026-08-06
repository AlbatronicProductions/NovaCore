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
using NovaCore.Simulation.Spacecraft.Guidance;
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
    internal static readonly Double3 FixtureInitialAngularVelocity = Double3.Zero;
    internal const double PlayerTorqueMagnitude = 4d;
    // Presentation-only lengths in authoritative metres. They do not participate in SAS evaluation.
    internal const double BodyForwardIndicatorLengthMetres = 35_000_000d;
    internal const double SasTargetIndicatorLengthMetres = 70_000_000d;
    internal const long SasControlIntervalTicks = 50_000;
    internal const int MaximumSasControlBoundariesPerHostUpdate = 2_048;
    internal const double SasTorqueQuantizationIncrement = .01d;
    internal static readonly SimulationRate MaximumSupportedSasRate = SimulationRate.Ten;
    private static readonly SpacecraftSasControllerConfiguration SasControllerConfiguration = new(
        new Double3(7.5d, 7.5d, 7.5d), new Double3(63d, 63d, 63d), new Double3(8d, 8d, 8d),
        .002d, .002d, .01d, .01d);
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
    private SpacecraftSasMode _sasMode;
    private DoubleQuaternion _holdTarget;
    private bool _hasHoldTarget;
    private SimulationInstant _nextSasControlBoundary = new(SasControlIntervalTicks);
    private Double3 _lastSasTorque;
    private Double3 _lastRawSasTorque;
    private SpacecraftSasControlStatus _lastSasControlStatus;
    private bool _hasSasTorqueRequest;
    private int _sasCrossedBoundaryCount;
    private bool _sasBoundaryRecoveryReported;
    private bool _sasControlSuspended;
    private bool _sasBoundaryCapReported;
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
    internal SpacecraftSasMode SasMode => _sasMode;
    internal bool HasHoldTarget => _hasHoldTarget;
    internal DoubleQuaternion HoldTarget => _holdTarget;
    internal int TorqueTransitionCount => _transactions.ProcessedRigidBodyTorqueCount;
    internal int SasCrossedBoundaryCount => _sasCrossedBoundaryCount;
    internal bool HasSasTorqueRequest => _hasSasTorqueRequest;
    internal Double3 LastSasTorque => _lastSasTorque;
    internal Double3 LastRawSasTorque => _lastRawSasTorque;
    internal SpacecraftSasControlStatus LastSasControlStatus => _lastSasControlStatus;
    internal SimulationInstant NextSasControlBoundary => _nextSasControlBoundary;
    internal bool SasControlSuspended => _sasControlSuspended;
    public double OrbitDistance => _orbitDistance;
    public static FixtureCameraConfiguration Camera => new(
        new Double3(0d, 0d, 24d),
        new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1_000d),
        .1d);

    public static bool TryCreate(out CelestialAnalyticalScene? scene, out string error) => TryCreateCore(DoubleQuaternion.Identity, FixtureInitialAngularVelocity, out scene, out error);

    internal static bool TryCreateForTest(in DoubleQuaternion initialOrientation, in Double3 initialAngularVelocity, out CelestialAnalyticalScene? scene, out string error) => TryCreateCore(initialOrientation, initialAngularVelocity, out scene, out error);

    private static bool TryCreateCore(in DoubleQuaternion initialOrientation, in Double3 initialAngularVelocity, out CelestialAnalyticalScene? scene, out string error)
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
            if (SpacecraftRigidBodyRotationState.TryCreate(spacecraftId, SimulationInstant.Zero, initialOrientation, initialAngularVelocity, FixtureInertia, Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1, out var rotation) != SpacecraftRigidBodyRotationEvaluationStatus.Success)
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

        // A manual request wins for this update, so no SAS boundary may run before it disengages.
        if (CreateTorqueCommand(input).RequestedBodyTorque != Double3.Zero && _sasMode != SpacecraftSasMode.Off)
        {
            _sasMode = SpacecraftSasMode.Off;
            _hasHoldTarget = false;
            _hasSasTorqueRequest = false;
        }

        if (!TryServiceHostDurationWithSasCadence(out error)) return false;
        if (!TryApplySasMode(input, out error) || !TryApplyTorqueControl(input, out error)) return false;
        return TryPublishCandidate(false, out error);
    }

    private bool TryServiceHostDurationWithSasCadence(out string error)
    {
        _sasCrossedBoundaryCount = 0;
        if (_sasMode == SpacecraftSasMode.Off)
        {
            var service = _transactions.ServicePendingHostDurationDebt();
            if (service.Reason is SimulationDebtServiceStopReason.Completed or SimulationDebtServiceStopReason.NoDebt) { error = string.Empty; return true; }
            error = $"Celestial clock execution failed: {service.Reason}";
            return false;
        }
        if (_sasControlSuspended)
        {
            var service = _transactions.ServicePendingHostDurationDebt();
            if (service.Reason is SimulationDebtServiceStopReason.Completed or SimulationDebtServiceStopReason.NoDebt) { error = string.Empty; return true; }
            error = $"Celestial clock execution failed: {service.Reason}";
            return false;
        }
        if (!TryEnsureFutureSasControlBoundary(true, out error)) return false;
        if (!_clock.TryGetPendingSimulationDebtTarget(out var target)) { error = "Celestial SAS cadence target overflow."; return false; }
        var processedEvents = 0;
        while (_clock.PendingSimulationDebt.Ticks > 0)
        {
            if (_nextSasControlBoundary <= target && _sasCrossedBoundaryCount == MaximumSasControlBoundariesPerHostUpdate)
            {
                if (!TryCommitSasTorque(Double3.Zero, _clock.CurrentTime, out error)) return false;
                if (!TryGetFirstSasControlBoundaryAfter(_clock.CurrentTime, out _nextSasControlBoundary)) { error = "Celestial SAS control boundary overflow."; return false; }
                if (!_sasBoundaryCapReported) { Console.Error.WriteLine("Celestial SAS cadence cap reached; control will resume at the next future boundary."); _sasBoundaryCapReported = true; }
                error = string.Empty;
                return true;
            }
            var boundary = _nextSasControlBoundary <= target ? _nextSasControlBoundary : target;
            var before = _clock.CurrentTime;
            var advance = _clock.AdvanceTo(boundary);
            var traversed = _clock.CurrentTime.Ticks - before.Ticks;
            if (traversed > 0) _clock.ConsumePendingSimulationDebt(new SimulationDuration(traversed));
            if (advance.Reason == SimulationAdvanceStopReason.ReachedEventBoundary)
            {
                var remainingEvents = _clock.MaximumEventsPerAdvance - processedEvents;
                if (remainingEvents <= 0) { error = "Celestial canonical event budget reached."; return false; }
                var group = _transactions.ExecuteCanonicalGroup(remainingEvents);
                processedEvents += group.ProcessedEventCount;
                if (!group.IsComplete) { error = $"Celestial clock execution failed: {group.Reason}"; return false; }
                continue;
            }
            if (advance.Reason != SimulationAdvanceStopReason.ReachedTarget) { error = $"Celestial cadence advance failed: {advance.Reason}"; return false; }
            if (_clock.CurrentTime == _nextSasControlBoundary)
            {
                _sasCrossedBoundaryCount++;
                if (!TryApplySasControlAtBoundary(_clock.CurrentTime, out error)) return false;
                try { _nextSasControlBoundary = new SimulationInstant(checked(_nextSasControlBoundary.Ticks + SasControlIntervalTicks)); }
                catch (OverflowException) { error = "Celestial SAS control boundary overflow."; return false; }
            }
            if (traversed == 0 && _clock.CurrentTime != _nextSasControlBoundary) { error = "Celestial cadence made no progress."; return false; }
        }
        error = string.Empty;
        return true;
    }

    private bool TryEnsureFutureSasControlBoundary(bool reportRecovery, out string error)
    {
        if (_nextSasControlBoundary > _clock.CurrentTime) { error = string.Empty; return true; }
        if (!TryGetFirstSasControlBoundaryAfter(_clock.CurrentTime, out _nextSasControlBoundary)) { error = "Celestial SAS control boundary overflow."; return false; }
        if (reportRecovery && !_sasBoundaryRecoveryReported)
        {
            Console.Error.WriteLine($"Celestial SAS cadence recovered stale boundary at simulation tick {_clock.CurrentTime.Ticks}.");
            _sasBoundaryRecoveryReported = true;
        }
        error = string.Empty;
        return true;
    }

    internal static bool TryGetFirstSasControlBoundaryAfter(SimulationInstant current, out SimulationInstant next)
    {
        var remainder = current.Ticks % SasControlIntervalTicks;
        var increment = remainder >= 0 ? SasControlIntervalTicks - remainder : -remainder;
        try { next = new SimulationInstant(checked(current.Ticks + increment)); return true; }
        catch (OverflowException) { next = default; return false; }
    }

    private bool TryUpdateSasRateSupport(out string error)
    {
        if (_sasMode == SpacecraftSasMode.Off) { _sasControlSuspended = false; error = string.Empty; return true; }
        var supported = (Int128)_clock.Rate.Numerator <= (Int128)MaximumSupportedSasRate.Numerator * _clock.Rate.Denominator;
        if (!supported)
        {
            if (!_sasControlSuspended)
            {
                if (!TryCommitSasTorque(Double3.Zero, _clock.CurrentTime, out error)) return false;
                _sasControlSuspended = true;
                Console.WriteLine($"Celestial SAS suspended: rate {_clock.Rate.Numerator}:{_clock.Rate.Denominator} exceeds 10:1.");
            }
            error = string.Empty;
            return true;
        }
        if (_sasControlSuspended)
        {
            _sasControlSuspended = false;
            _hasSasTorqueRequest = false;
            if (!TryGetFirstSasControlBoundaryAfter(_clock.CurrentTime, out _nextSasControlBoundary)) { error = "Celestial SAS control boundary overflow."; return false; }
            Console.WriteLine("Celestial SAS resumed at supported rate.");
        }
        error = string.Empty;
        return true;
    }

    private bool TryApplySasControlAtBoundary(SimulationInstant boundary, out string error)
    {
        if (_sasMode == SpacecraftSasMode.Off) { error = string.Empty; return true; }
        if (!_transactions.State.Spacecraft.TryGetRigidBody(_spacecraftId, out var rotation)) { error = "SAS spacecraft state unavailable."; return false; }
        var current = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, boundary);
        if (!current.Succeeded) { error = $"SAS rigid-body evaluation failed: {current.Status}"; return false; }

        DoubleQuaternion target;
        if (_sasMode == SpacecraftSasMode.HoldAttitude)
        {
            if (!_hasHoldTarget) return TryCommitSasTorque(Double3.Zero, boundary, out error);
            target = _holdTarget;
        }
        else
        {
            if (!TryEvaluateSasTarget(boundary, out target)) return TryCommitSasTorque(Double3.Zero, boundary, out error);
        }
        var result = SpacecraftSasController.TryEvaluate(current.OrientationLocalToParent, current.AngularVelocityBody, target, rotation.PrincipalInertia, SasControllerConfiguration);
        _lastSasControlStatus = result.Status;
        _lastRawSasTorque = result.Succeeded ? result.RequestedBodyTorque : Double3.Zero;
        return result.Succeeded ? TryCommitSasTorque(QuantizeSasTorque(result.RequestedBodyTorque), boundary, out error) : TryCommitSasTorque(Double3.Zero, boundary, out error);
    }

    private bool TryEvaluateSasTarget(SimulationInstant boundary, out DoubleQuaternion target)
    {
        target = default;
        var propagated = CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(2), _transactions.State.Celestial, boundary);
        if (!propagated.Succeeded || !TryFlightReferenceMode(_sasMode, out var mode)) return false;
        var reference = FlightReferenceEvaluator.TryEvaluate(propagated.State.Position, propagated.State.Velocity, DoubleQuaternion.Identity, mode);
        return reference.Succeeded && SpacecraftSasTargetOrientation.TryCreate(reference.DirectionCarrierParent, Double3.UnitZ, out target) == SpacecraftSasControlStatus.Success;
    }

    private bool TryCommitSasTorque(in Double3 torque, SimulationInstant time, out string error)
    {
        if (!torque.IsFinite) { error = "SAS torque was non-finite."; return false; }
        if (_hasSasTorqueRequest && torque == _lastSasTorque) { error = string.Empty; return true; }
        if (!TryCommitTorqueRequest(torque, time, out error)) return false;
        _lastSasTorque = torque;
        _hasSasTorqueRequest = true;
        return true;
    }

    private bool TryCommitTorqueRequest(in Double3 torque, SimulationInstant time, out string error)
    {
        if (torque == _requestedTorque) { error = string.Empty; return true; }
        var command = new SpacecraftTorqueCommand(_spacecraftId, torque, time);
        var candidate = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(_transactions.State, command);
        if (candidate.Status == RigidBodyTorqueTransactionStatus.ReplacementNoOp) { _requestedTorque = torque; error = string.Empty; return true; }
        if (!candidate.Succeeded || candidate.Transaction is null) { error = $"Spacecraft torque candidate failed: {candidate.Status}"; return false; }
        var committed = _transactions.ValidateAndCommit(candidate.Transaction.Value);
        if (!committed.Committed) { error = $"Spacecraft torque commit failed: {committed.Status}"; return false; }
        _requestedTorque = torque; error = string.Empty; return true;
    }

    internal static Double3 QuantizeSasTorque(in Double3 torque) => new(QuantizeSasTorqueComponent(torque.X), QuantizeSasTorqueComponent(torque.Y), QuantizeSasTorqueComponent(torque.Z));
    private static double QuantizeSasTorqueComponent(double value) => !double.IsFinite(value) ? double.NaN : Math.Round(value / SasTorqueQuantizationIncrement, MidpointRounding.AwayFromZero) * SasTorqueQuantizationIncrement;
    private static bool TryFlightReferenceMode(SpacecraftSasMode mode, out FlightReferenceMode value)
    {
        value = mode switch { SpacecraftSasMode.Prograde => FlightReferenceMode.Prograde, SpacecraftSasMode.Retrograde => FlightReferenceMode.Retrograde, SpacecraftSasMode.Normal => FlightReferenceMode.Normal, SpacecraftSasMode.AntiNormal => FlightReferenceMode.AntiNormal, SpacecraftSasMode.RadialOut => FlightReferenceMode.RadialOut, SpacecraftSasMode.RadialIn => FlightReferenceMode.RadialIn, _ => default };
        return mode is not (SpacecraftSasMode.Off or SpacecraftSasMode.HoldAttitude);
    }

    private bool TryApplyTorqueControl(in NativeInputState input, out string error)
    {
        var command = CreateTorqueCommand(input);
        if (command.RequestedBodyTorque != Double3.Zero && _sasMode != SpacecraftSasMode.Off) { _sasMode = SpacecraftSasMode.Off; _hasHoldTarget = false; _hasSasTorqueRequest = false; _sasControlSuspended = false; }
        // A released manual key is not a zero-torque request while SAS owns the active command.
        if (command.RequestedBodyTorque == Double3.Zero && _sasMode != SpacecraftSasMode.Off) { error = string.Empty; return true; }
        // An unchanged request is already authoritative. Do not pollute history or rebase its epoch.
        if (command.RequestedBodyTorque == _requestedTorque) { error = string.Empty; return true; }
        return TryCommitTorqueRequest(command.RequestedBodyTorque, command.Time, out error);
    }

    private bool TryApplySasMode(in NativeInputState input, out string error)
    {
        error = string.Empty;
        if (input.SasModeKey == 0) return true;
        if (input.SasModeKey is > 8) { error = "Invalid SAS mode key."; return false; }
        if (input.SasModeKey == 1) { _sasMode = SpacecraftSasMode.Off; _hasHoldTarget = false; _sasControlSuspended = false; var committed = TryCommitSasTorque(Double3.Zero, _clock.CurrentTime, out error); _hasSasTorqueRequest = false; return committed; }
        if (input.SasModeKey == 2)
        {
            if (!_transactions.State.Spacecraft.TryGetRigidBody(_spacecraftId, out var rotation)) { error = "SAS hold spacecraft state unavailable."; return false; }
            var evaluated = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, _clock.CurrentTime);
            if (!evaluated.Succeeded || SpacecraftAttitudeEvaluator.TryCanonicalize(evaluated.OrientationLocalToParent, out _holdTarget) != SpacecraftAttitudeEvaluationStatus.Success) { error = "SAS hold capture failed."; return false; }
            _sasMode = SpacecraftSasMode.HoldAttitude; _hasHoldTarget = true; _hasSasTorqueRequest = false; return TryUpdateSasRateSupport(out error) && (!_sasControlSuspended ? TryEnsureFutureSasControlBoundary(false, out error) : true);
        }
        _sasMode = (SpacecraftSasMode)(input.SasModeKey - 1); _hasHoldTarget = false; _hasSasTorqueRequest = false; return TryUpdateSasRateSupport(out error) && (!_sasControlSuspended ? TryEnsureFutureSasControlBoundary(false, out error) : true);
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
        if (rateChanged && !TryUpdateSasRateSupport(out var supportError)) Console.Error.WriteLine(supportError);
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
            var spacecraftRootPosition = spacecraft.ConvertPosition(Double3.Zero);
            _objects[1] = Marker(2, spacecraftRootPosition, new Double3(12d, 4d, 1d), forceInvalidMesh ? MeshHandle.Invalid : MeshHandle.Triangle, spacecraft.SourceToTarget.Rotation);
            _objects[2] = Marker(3, _burnPointMetres, _burnVisible ? new Double3(2.5d, 2.5d, 1d) : Double3.Zero, MeshHandle.Triangle);
            var bodyForward = spacecraft.SourceToTarget.Rotation.Rotate(Double3.UnitX);
            var bodyForwardIndicator = DirectionIndicator(spacecraftRootPosition, bodyForward, BodyForwardIndicatorLengthMetres);
            ResolvedDirectionIndicator? targetDirectionIndicator = null;
            if (_sasMode != SpacecraftSasMode.Off && !_sasControlSuspended && TryGetPresentationSasTarget(_clock.CurrentTime, out var sasTarget))
                targetDirectionIndicator = DirectionIndicator(spacecraftRootPosition, sasTarget.Rotate(Double3.UnitX), SasTargetIndicatorLengthMetres);
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
            if (!ResolvedRenderSnapshot.TryCreate(_objects, candidateCurve, candidatePreviousCurve, bodyForwardIndicator, targetDirectionIndicator, out snapshot, out var snapshotStatus) || snapshot is null)
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

    private ResolvedDirectionIndicator DirectionIndicator(in Double3 startMetres, in Double3 direction, double lengthMetres) =>
        new(new UniversePosition(startMetres / MetresPerDisplayUnit, _rootFrame), new UniversePosition((startMetres + direction * lengthMetres) / MetresPerDisplayUnit, _rootFrame));

    private bool TryGetPresentationSasTarget(SimulationInstant time, out DoubleQuaternion target)
    {
        if (_sasMode == SpacecraftSasMode.HoldAttitude)
        {
            target = _holdTarget;
            return _hasHoldTarget;
        }
        return TryEvaluateSasTarget(time, out target);
    }

    internal bool TryGetPresentationSasTargetForTest(SimulationInstant time, out DoubleQuaternion target) => TryGetPresentationSasTarget(time, out target);
    internal bool TryGetCurrentAngularVelocityForTest(out Double3 angularVelocity)
    {
        angularVelocity = default;
        if (!_transactions.State.Spacecraft.TryGetRigidBody(_spacecraftId, out var rotation)) return false;
        var evaluated = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(rotation, _clock.CurrentTime);
        if (!evaluated.Succeeded) return false;
        angularVelocity = evaluated.AngularVelocityBody;
        return true;
    }

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
