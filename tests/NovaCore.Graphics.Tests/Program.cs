using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Guidance;
using NovaCore.Simulation.Time;

var tests = new (string, Action)[]
{
    ("MeshHandle", MeshHandleTest),
    ("Transport layout", LayoutTest),
    ("Transform conversion", TransformTest),
    ("Camera relative", RelativeTest),
    ("Batches and capacity", BatchTest),
    ("Resolved render transport", ResolvedTransportTest),
    ("Orbit curve transport", OrbitCurveTransportTest),
    ("Static reference-frame fixture transport", StaticReferenceFrameFixtureTransportTest),
    ("Dynamic reference-frame fixture publication", DynamicReferenceFrameFixturePublicationTest),
    ("Celestial analytical fixture publication", CelestialAnalyticalFixturePublicationTest),
    ("Celestial player torque controls", CelestialPlayerTorqueControlsTest),
    ("Celestial SAS mode selection", CelestialSasModeSelectionTest),
    ("Celestial SAS control cadence", CelestialSasControlCadenceTest),
    ("Celestial SAS diagnostic indicators", CelestialSasDiagnosticIndicatorsTest),
    ("Camera snapshot allocation", CameraSnapshotAllocationTest),
};
foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }

static void CelestialPlayerTorqueControlsTest()
{
    static DoubleQuaternion Advance(NativeInputState input)
    {
        Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"player torque scene: {error}");
        Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), input, out error), $"player torque input: {error}");
        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out error), $"player torque release: {error}");
        return scene.CurrentSnapshot.Objects[1].RootOrientation;
    }
    var w = Advance(new NativeInputState { MoveForward = 1 }); var s = Advance(new NativeInputState { MoveBackward = 1 });
    var a = Advance(new NativeInputState { MoveLeft = 1 }); var d = Advance(new NativeInputState { MoveRight = 1 });
    var q = Advance(new NativeInputState { MoveDown = 1 }); var e = Advance(new NativeInputState { MoveUp = 1 });
    var neutral = Advance(default); var cancelled = Advance(new NativeInputState { MoveForward = 1, MoveBackward = 1 });
    Check(w != s && a != d && q != e, "opposed pitch/yaw/roll inputs produce opposite authoritative torque states");
    Check(cancelled == neutral, "opposing inputs cancel");

    Check(CelestialAnalyticalScene.TryCreate(out var held, out var heldError) && held is not null, $"held control scene: {heldError}");
    var press = new NativeInputState { MoveForward = 1 };
    Check(held!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), press, out heldError) && held.TorqueTransitionCount == 1, "one torque-on edge commit");
    for (var index = 0; index < 100; index++) Check(held.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), press, out heldError), $"held torque update: {heldError}");
    Check(held.TorqueTransitionCount == 1, "held torque creates no history entries");
    Check(held.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out heldError) && held.TorqueTransitionCount == 2, "one torque-off edge commit");
}

static void CelestialSasModeSelectionTest()
{
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"SAS selection scene: {error}");
    var hold = new NativeInputState { SasModeKey = 2 };
    Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), hold, out error), $"hold selection: {error}");
    Check(scene.SasMode == SpacecraftSasMode.HoldAttitude && scene.HasHoldTarget && scene.HoldTarget == scene.CurrentSnapshot.Objects[1].RootOrientation, "hold captures the current authoritative orientation");
    Check(scene.TorqueTransitionCount == 0, "mode selection does not create a torque transaction");

    var cancelled = new NativeInputState { MoveForward = 1, MoveBackward = 1 };
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), cancelled, out error), $"cancelled manual input: {error}");
    Check(scene.SasMode == SpacecraftSasMode.HoldAttitude && scene.HasHoldTarget && scene.TorqueTransitionCount == 0, "opposed manual input preserves SAS state");

    var manual = new NativeInputState { MoveForward = 1 };
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), manual, out error), $"manual disengage: {error}");
    Check(scene.SasMode == SpacecraftSasMode.Off && !scene.HasHoldTarget && scene.TorqueTransitionCount == 1, "manual torque disengages SAS before its authoritative commit");

    for (uint key = 3; key <= 8; key++)
    {
        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = key }, out error), $"SAS mode {key}: {error}");
        Check(scene.SasMode == (SpacecraftSasMode)(key - 1) && !scene.HasHoldTarget, $"SAS key {key} maps deterministically");
    }
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out error), $"SAS off: {error}");
    Check(scene.SasMode == SpacecraftSasMode.Off && !scene.HasHoldTarget, "SAS off clears hold state");
}

static void CelestialSasControlCadenceTest()
{
    Check(CelestialAnalyticalScene.QuantizeSasTorque(new Double3(.005d, -.005d, 4.004d)) == new Double3(.01d, -.01d, 4d), "SAS torque quantizes midpoint away from zero after controller clamp");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(SimulationInstant.Zero, out var zeroBoundary) && zeroBoundary == new SimulationInstant(50_000), "zero-time SAS engagement schedules the first boundary");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(new SimulationInstant(50_000), out var exactBoundary) && exactBoundary == new SimulationInstant(100_000), "exact cadence time schedules the strictly next boundary");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(new SimulationInstant(73_000), out var betweenBoundary) && betweenBoundary == new SimulationInstant(100_000), "between-boundary engagement schedules the next boundary");
    static void SetSupportedSasRate(CelestialAnalyticalScene scene)
    {
        var fixtureCamera = CelestialAnalyticalScene.Camera;
        var camera = new CameraState(new FramePosition(new ReferenceFrameId(1), fixtureCamera.Position), DoubleQuaternion.Identity, fixtureCamera.Projection, CameraMode.Free);
        for (var index = 0; index < 4; index++) scene.ApplyPresentationInput(camera, new NativeInputState { RateDecrease = 1 }, out _, out _);
        Check(scene.Rate == SimulationRate.Ten, "10x is the highest supported SAS rate");
    }
    static CelestialAnalyticalScene CreateSelected()
    {
        Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"SAS cadence scene: {error}");
        SetSupportedSasRate(scene!);
        Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error), $"SAS prograde selection: {error}");
        return scene;
    }
    var scene = CreateSelected();
    Check(scene.CurrentTime == new SimulationInstant(10_000) && scene.TorqueTransitionCount == 0, "mode selection advances no SAS boundary");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(3_000), out var error), $"pre-boundary coast: {error}");
    Check(scene.SasCrossedBoundaryCount == 0 && scene.TorqueTransitionCount == 0, "no SAS boundary before 50000 microticks");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out error), $"first SAS boundary: {error}");
    Check(scene.CurrentTime == new SimulationInstant(50_000) && scene.SasCrossedBoundaryCount == 1 && scene.HasSasTorqueRequest && scene.LastSasTorque != Double3.Zero && scene.TorqueTransitionCount == 1, $"first exact SAS boundary commits one quantized torque: time={scene.CurrentTime.Ticks}, rate={scene.Rate.Numerator}:{scene.Rate.Denominator}, mode={scene.SasMode}, suspended={scene.SasControlSuspended}, next={scene.NextSasControlBoundary.Ticks}, boundaries={scene.SasCrossedBoundaryCount}, torque={scene.LastSasTorque}, transitions={scene.TorqueTransitionCount}");
    var transitions = scene.TorqueTransitionCount;
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out error), $"between-boundary coast: {error}");
    Check(scene.SasCrossedBoundaryCount == 0 && scene.TorqueTransitionCount == transitions, "between boundaries creates no transaction");

    static (DoubleQuaternion Orientation, Double3 Torque, int Transitions) AdvancePartition(ReadOnlySpan<long> partitions)
    {
        var candidate = CreateSelected();
        foreach (var ticks in partitions) Check(candidate.TryAdvanceByHostDuration(SimulationDuration.FromTicks(ticks), out var error), $"partitioned SAS advance: {error}");
        return (candidate.CurrentSnapshot.Objects[1].RootOrientation, candidate.LastSasTorque, candidate.TorqueTransitionCount);
    }
    var whole = AdvancePartition([10_000]);
    var partitioned = AdvancePartition([3_000, 4_000, 3_000]);
    Check(whole == partitioned, "SAS control boundaries are partition-independent");
    var cadenceHash = MixQuaternion(MixDouble3(14695981039346656037UL, whole.Torque), whole.Orientation);
    Check(cadenceHash == MixQuaternion(MixDouble3(14695981039346656037UL, partitioned.Torque), partitioned.Orientation), "SAS scripted sequence hash is deterministic");

    static (Double3 Torque, int Boundaries) EvaluateMode(uint key)
    {
        Check(CelestialAnalyticalScene.TryCreate(out var selected, out var selectionError) && selected is not null, $"SAS mode fixture: {selectionError}");
        SetSupportedSasRate(selected!);
        Check(selected!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = key }, out selectionError), $"SAS mode select: {selectionError}");
        Check(selected.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out selectionError), $"SAS mode boundary: {selectionError}");
        return (selected.LastSasTorque, selected.SasCrossedBoundaryCount);
    }
    Check(EvaluateMode(2) is { Torque: var holdTorque, Boundaries: 1 } && holdTorque == Double3.Zero, "hold-attitude evaluates its captured target at the boundary");
    Check(EvaluateMode(3).Torque != Double3.Zero, "prograde target evaluates at the boundary");
    Check(EvaluateMode(5).Torque != Double3.Zero, "normal target evaluates at the boundary");
    Check(EvaluateMode(7).Boundaries == 1, "radial-out target evaluates at the boundary");

    var multiple = CreateSelected();
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error), $"multiple SAS boundaries: {error}");
    Check(multiple.SasCrossedBoundaryCount == 2, "crossed SAS boundaries process chronologically");
    var beforeOff = multiple.TorqueTransitionCount;
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 1 }, out error), $"SAS off: {error}");
    Check(multiple.SasMode == SpacecraftSasMode.Off && multiple.LastSasTorque == Double3.Zero && multiple.TorqueTransitionCount == beforeOff + 1, "SAS off commits zero torque once");

    var switched = CreateSelected(); var nextBeforeSwitch = switched.NextSasControlBoundary;
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 5 }, out error), $"pre-boundary mode switch: {error}");
    Check(switched.SasMode == SpacecraftSasMode.Normal && switched.NextSasControlBoundary == nextBeforeSwitch && switched.NextSasControlBoundary > switched.CurrentTime, "active mode switch preserves a future cadence boundary");
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error), $"post-boundary mode switch coast: {error}");
    var nextAfterBoundaries = switched.NextSasControlBoundary;
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 7 }, out error), $"post-boundary mode switch: {error}");
    Check(switched.SasMode == SpacecraftSasMode.RadialOut && switched.NextSasControlBoundary == nextAfterBoundaries && switched.NextSasControlBoundary > switched.CurrentTime, "post-boundary switch retains no stale target");

    Check(CelestialAnalyticalScene.TryCreate(out var late, out var lateError) && late is not null, $"late SAS fixture: {lateError}"); SetSupportedSasRate(late!);
    Check(late!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(100_000), out lateError), $"large off-mode advance: {lateError}");
    Check(late.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out lateError), $"late SAS engagement: {lateError}");
    Check(late.NextSasControlBoundary > late.CurrentTime, "late SAS engagement reinitializes a strictly future boundary");
    Check(late.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out lateError), $"late SAS cadence advance: {lateError}");
    Check(late.CurrentTime > new SimulationInstant(1_000_000) && late.SasCrossedBoundaryCount == 1, "late SAS engagement continues authoritative time without TargetBeforeCurrent");

    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error), $"off-to-active reengagement: {error}");
    Check(multiple.NextSasControlBoundary > multiple.CurrentTime, "off-to-active transition schedules a future boundary");
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { MoveForward = 1 }, out error), $"manual SAS disengagement: {error}");
    Check(multiple.SasMode == SpacecraftSasMode.Off, "manual torque disengages SAS");
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error) && multiple.NextSasControlBoundary > multiple.CurrentTime, $"manual reengagement: {error}");

    var paused = CreateSelected(); var camera = new CameraState(new FramePosition(new ReferenceFrameId(1), CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    paused.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    var pausedBoundary = paused.NextSasControlBoundary; var pausedTime = paused.CurrentTime;
    Check(paused.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error) && paused.SasCrossedBoundaryCount == 0 && paused.TorqueTransitionCount == 0 && paused.CurrentTime == pausedTime && paused.NextSasControlBoundary == pausedBoundary, $"pause suppresses SAS cadence: {error}");
    paused.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(paused.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out error) && paused.SasCrossedBoundaryCount == 1, $"resume preserves the next future cadence boundary: {error}");

    Check(CelestialAnalyticalScene.TryCreate(out var highWarp, out var highWarpError) && highWarp is not null, $"high-warp SAS fixture: {highWarpError}");
    Check(highWarp!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out highWarpError), $"high-warp SAS selection: {highWarpError}");
    Check(highWarp.SasControlSuspended && highWarp.SasCrossedBoundaryCount == 0 && highWarp.SasMode == SpacecraftSasMode.Prograde, "SAS selection at 10000x suspends without cadence work");
    var highWarpTime = highWarp.CurrentTime; var highWarpTransitions = highWarp.TorqueTransitionCount;
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out highWarpError), $"high-warp suspended coast: {highWarpError}");
    Check(highWarp.CurrentTime > highWarpTime && highWarp.SasControlSuspended && highWarp.SasCrossedBoundaryCount == 0 && highWarp.TorqueTransitionCount == highWarpTransitions, "suspended SAS leaves clock advancing without cap or transaction growth");
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 5 }, out highWarpError), $"high-warp mode switch: {highWarpError}");
    Check(highWarp.SasControlSuspended && highWarp.SasMode == SpacecraftSasMode.Normal && highWarp.SasCrossedBoundaryCount == 0, "mode switch while suspended retains the newest mode");
    var highWarpCamera = new CameraState(new FramePosition(new ReferenceFrameId(1), CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    for (var index = 0; index < 4; index++) highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateDecrease = 1 }, out _, out _);
    Check(highWarp.Rate == SimulationRate.Ten && !highWarp.SasControlSuspended && highWarp.NextSasControlBoundary > highWarp.CurrentTime, "supported-rate transition resumes at a strictly future boundary");
    var ticksToResume = (highWarp.NextSasControlBoundary.Ticks - highWarp.CurrentTime.Ticks) / SimulationRate.Ten.Numerator;
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(ticksToResume), out highWarpError) && highWarp.SasCrossedBoundaryCount == 1, $"supported-rate resume runs only the future boundary: {highWarpError}");
    var activeTransitions = highWarp.TorqueTransitionCount;
    highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    Check(highWarp.Rate == SimulationRate.Hundred && highWarp.SasControlSuspended && highWarp.LastSasTorque == Double3.Zero && highWarp.TorqueTransitionCount == activeTransitions + 1, "unsupported-rate transition commits zero torque once");
    highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    Check(highWarp.TorqueTransitionCount == activeTransitions + 1, "repeated unsupported rate changes do not duplicate zero torque");
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out highWarpError) && highWarp.SasMode == SpacecraftSasMode.Off && !highWarp.SasControlSuspended && highWarp.TorqueTransitionCount == activeTransitions + 1, $"off while suspended clears without duplicate zero torque: {highWarpError}");
    _ = CelestialAnalyticalScene.QuantizeSasTorque(Double3.UnitX); var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) _ = CelestialAnalyticalScene.QuantizeSasTorque(new Double3(index * .001d, -index * .001d, 0d)); Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore, "warmed SAS torque quantization allocates zero bytes");
    Console.WriteLine($"Deterministic SAS cadence hash: 0x{cadenceHash:X16}; quantization allocation=0 bytes");
}

static void CelestialSasDiagnosticIndicatorsTest()
{
    static void SetSupportedRate(CelestialAnalyticalScene scene, CameraState camera)
    {
        for (var index = 0; index < 4; index++) scene.ApplyPresentationInput(camera, new NativeInputState { RateDecrease = 1 }, out _, out _);
    }
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var createError) && scene is not null, $"SAS indicator fixture: {createError}");
    var root = new ReferenceFrameId(1); var camera = new CameraState(new FramePosition(root, CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    var initial = scene!.CurrentSnapshot;
    Check(initial.BodyForwardIndicator is { } initialForward && initial.TargetDirectionIndicator is null, "body-forward is visible while SAS-off target is hidden");
    initialForward = initial.BodyForwardIndicator.GetValueOrDefault();
    var expectedForward = initial.Objects[1].RootOrientation.Rotate(Double3.UnitX);
    CheckNear((initialForward.End.Value - initialForward.Start.Value).Normalized(), expectedForward, "body-forward endpoint matches q.Rotate(+X)");

    SetSupportedRate(scene, camera);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out var advanceError), $"select prograde for indicators: {advanceError}");
    var prograde = scene.CurrentSnapshot;
    Check(prograde.BodyForwardIndicator is not null && prograde.TargetDirectionIndicator is { } target, "active valid SAS publishes both indicators");
    target = prograde.TargetDirectionIndicator.GetValueOrDefault();
    Check(target.Start == prograde.BodyForwardIndicator!.Value.Start && (target.End.Value - target.Start.Value).LengthSquared > 0d, "target indicator begins at spacecraft and has direction");
    Check(scene.TryGetPresentationSasTargetForTest(scene.CurrentTime, out var targetOrientation), "pure SAS target available");
    CheckNear((target.End.Value - target.Start.Value).Normalized(), targetOrientation.Rotate(Double3.UnitX), "target endpoint matches selected reference direction");

    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 2 }, out advanceError), $"select hold for indicators: {advanceError}");
    var hold = scene.CurrentSnapshot; Check(hold.TargetDirectionIndicator is { } holdTarget && scene.HasHoldTarget, "hold target indicator visible");
    holdTarget = hold.TargetDirectionIndicator.GetValueOrDefault();
    CheckNear((holdTarget.End.Value - holdTarget.Start.Value).Normalized(), scene.HoldTarget.Rotate(Double3.UnitX), "hold indicator follows captured target +X");

    scene.ApplyPresentationInput(camera, new NativeInputState { LookActive = 1, MouseDeltaX = 10f }, out _, out _);
    Check(scene.TryBuildCandidateForTest(out var afterCamera, out var candidateError) && afterCamera is not null, $"camera-independent indicator candidate: {candidateError}");
    Check(IndicatorHash(hold.BodyForwardIndicator!.Value) == IndicatorHash(afterCamera!.BodyForwardIndicator!.Value) && IndicatorHash(hold.TargetDirectionIndicator!.Value) == IndicatorHash(afterCamera.TargetDirectionIndicator!.Value), "camera movement does not alter world-space indicators");

    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out advanceError), $"paused indicator publication: {advanceError}");
    Check(IndicatorHash(hold.BodyForwardIndicator!.Value) == IndicatorHash(scene.CurrentSnapshot.BodyForwardIndicator!.Value), "pause freezes authoritative body-forward orientation");
    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out advanceError), $"disable SAS indicators: {advanceError}");
    Check(scene.CurrentSnapshot.TargetDirectionIndicator is null, "SAS-off hides target indicator");

    Check(CelestialAnalyticalScene.TryCreate(out var replay, out var replayError) && replay is not null, $"SAS indicator replay: {replayError}"); var replayScene = replay!; var replayCamera = new CameraState(new FramePosition(root, CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free); SetSupportedRate(replayScene, replayCamera); Check(replayScene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out replayError), $"SAS indicator replay advance: {replayError}");
    var forwardHash = IndicatorHash(prograde.BodyForwardIndicator!.Value); var targetHash = IndicatorHash(prograde.TargetDirectionIndicator!.Value); Check(forwardHash == IndicatorHash(replayScene.CurrentSnapshot.BodyForwardIndicator!.Value) && targetHash == IndicatorHash(replayScene.CurrentSnapshot.TargetDirectionIndicator!.Value), "indicator hashes are deterministic");
    var submission = new RenderFrameSubmission(3, 257); var cameraRoot = new UniversePosition(CelestialAnalyticalScene.Camera.Position, root); var gpuCamera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.BodyForwardVertexCount == 2 && submission.TargetDirectionVertexCount == 2, "indicator transport uses fixed two-vertex streams");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm indicator transport"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm indicator transport allocation");
    Console.WriteLine($"Deterministic SAS indicator hashes: forward=0x{forwardHash:X16}; target=0x{targetHash:X16}; transport allocation=0 bytes");
}

static void MeshHandleTest() { Check(!MeshHandle.Invalid.IsValid, "zero invalid"); Check(MeshHandle.Triangle.IsValid, "triangle valid"); }
static void LayoutTest()
{
    Check(Marshal.SizeOf<NativeEncodedPosition>() == 32, "encoded size"); Check(Marshal.SizeOf<NativeCameraData>() == 96, "native camera size"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.Position)).ToInt32() == 0, "native camera position offset"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.ViewProjection)).ToInt32() == 32, "native camera matrix offset"); Check(Marshal.SizeOf<GpuCameraData>() == 96, "GPU camera size"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.Position)).ToInt32() == 0, "GPU camera position offset"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.ViewProjection)).ToInt32() == 32, "GPU camera matrix offset"); Check(Marshal.SizeOf<NativeRenderTransform>() == 32, "transform size"); Check(Marshal.SizeOf<NativeRenderObject>() == 80, "object stride"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Position)).ToInt32() == 0, "position offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Transform)).ToInt32() == 32, "transform offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Mesh)).ToInt32() == 64, "mesh offset"); Check(Marshal.SizeOf<NativeDrawBatch>() == 16, "batch stride"); Check(Marshal.SizeOf<NativeInputState>() == 64 && Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.SasModeKey)).ToInt32() == 60, "SAS input layout"); Check(NativeRuntime.GetAbiLayout(out var abi) == NativeResult.Success && abi.InputStateSize == 64 && abi.InputSasModeKeyOffset == 60, "native SAS ABI layout");
}
static void TransformTest() { var t = RenderTransform.FromAuthoritative(new DoubleQuaternion(0, 0, Math.Sqrt(.5), Math.Sqrt(.5)), new Double3(-1, 2, 3)); Check(t.Rotation.W > .7f && t.Scale.X == -1, "conversion/negative scale policy"); Check(FloatQuaternion.Identity == new FloatQuaternion(0, 0, 0, 1), "identity"); }
static void OrbitCurveTransportTest()
{
    var root = new ReferenceFrameId(1); var cameraRoot = new UniversePosition(new Double3(1e12, 0, 0), root); var positions = new[] { new UniversePosition(cameraRoot.Value + new Double3(1, 2, -3), root), new UniversePosition(cameraRoot.Value + new Double3(2, 3, -4), root), new UniversePosition(cameraRoot.Value + new Double3(1, 2, -3), root) };
    Check(ResolvedOrbitCurve.TryCreate(positions, out var curve) && curve is not null, "immutable orbit curve"); var objects = new[] { Object(1, cameraRoot, MeshHandle.Triangle) }; Check(ResolvedRenderSnapshot.TryCreate(objects, curve, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null, "curve snapshot");
    var submission = new RenderFrameSubmission(1, 3); var camera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.OrbitVertexCount == 3, "curve transport"); Check(submission.OrbitVertices[0].X == 1f && submission.OrbitVertices[0].Y == 2f && submission.OrbitVertices[0].Z == -3f, "double camera-relative line conversion"); _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm curve transport"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm curve transport allocation");
}
static void RelativeTest() { var camera = EncodedPosition.Encode(new Double3(4e12, -3e12, 7e12)); var positive = EncodedPosition.Resolve(EncodedPosition.Encode(new Double3(4e12 + .25, -3e12, 7e12)), camera); var negative = EncodedPosition.Resolve(EncodedPosition.Encode(new Double3(4e12 - .25, -3e12, 7e12)), camera); Check(positive.Value.X > 0 && negative.Value.X < 0, "relative signs"); }
static void BatchTest()
{
    var frame = new ReferenceFrameId(1); var position = new UniversePosition(new Double3(4e12, 0, 0), frame); var camera = Camera(position);
    var submission = new RenderFrameSubmission(1000); submission.Begin(camera); for (var i = 0; i < 1000; i++) submission.Add(new UniversePosition(new Double3(4e12 + i, 0, 0), frame), DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); submission.Complete(); Check(submission.ObjectCount == 1000 && submission.BatchCount == 1 && submission.Batches[0].ObjectCount == 1000, "automatic stable batch");
    var small = new RenderFrameSubmission(1); small.Begin(camera); small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); Throws<InvalidOperationException>(() => small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle));
    var invalid = new RenderFrameSubmission(2); invalid.Begin(camera); Throws<ArgumentOutOfRangeException>(() => invalid.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Invalid));
}
static void ResolvedTransportTest()
{
    var root = new ReferenceFrameId(1); var other = new ReferenceFrameId(2); var cameraRoot = new UniversePosition(new Double3(4e12, -3e12, 7e12), root); var camera = Camera(cameraRoot);
    var source = new[] { Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value + new Double3(.25, 0, 0), root), new MeshHandle(2)), Object(3, new UniversePosition(cameraRoot.Value + new Double3(.5, 0, 0), root), MeshHandle.Triangle) };
    Check(ResolvedRenderSnapshot.TryCreate(source, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null, "valid snapshot");
    var frozenFirst = snapshot!.Objects[0]; source[0] = Object(9, new UniversePosition(Double3.Zero, root), MeshHandle.Invalid); Check(snapshot.Objects[0] == frozenFirst && snapshot.Count == 3, "snapshot copied caller data");
    Check(snapshot.Objects[0].Id.Value == 1 && snapshot.Objects[1].Id.Value == 2 && snapshot.Objects[2].Id.Value == 3, "caller order retained");
    Check(!ResolvedRenderSnapshot.TryCreate([], out _, out status) && status == ResolvedRenderSnapshotStatus.Empty, "empty rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(0, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidObjectId, "zero ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(1, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.DuplicateObjectId, "duplicate ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, new UniversePosition(new Double3(double.NaN, 0, 0), root), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFinitePosition, "non-finite position rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, default, new Double3(1, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidOrientation, "invalid orientation rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, DoubleQuaternion.Identity, new Double3(double.NaN, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFiniteScale, "non-finite scale rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Invalid)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidMeshHandle, "invalid mesh rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value, other), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.MixedRootFrame, "mixed roots rejected");

    var destination = new RenderFrameSubmission(3); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "transport build"); Check(destination.ObjectCount == 3 && destination.BatchCount == 3 && destination.Batches[0].FirstObject == 0 && destination.Batches[1].FirstObject == 1 && destination.Batches[2].FirstObject == 2, "stable contiguous batches"); Check(destination.Objects[1].Position == EncodedPosition.Encode(cameraRoot.Value + new Double3(.25, 0, 0)), "sole encoder output"); Check(EncodedPosition.Resolve(destination.Objects[1].Position, camera.Position).Value.X > 0, "large-root relative separation");
    var retainedObject = destination.Objects[0]; var retainedCount = destination.ObjectCount; var retainedBatches = destination.BatchCount;
    Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "camera root mismatch"); Check(destination.ObjectCount == retainedCount && destination.BatchCount == retainedBatches && destination.Objects[0] == retainedObject, "mismatch atomicity");
    var small = new RenderFrameSubmission(2); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, small) == ResolvedRenderSubmissionBuildStatus.DestinationCapacityExceeded && small.ObjectCount == 0 && small.BatchCount == 0, "object and batch capacity protected");
    var badCamera = camera; badCamera.ViewProjection.C0R0 = float.NaN; Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, badCamera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.InvalidCameraData && destination.ObjectCount == retainedCount, "invalid camera atomicity");
    var hash = TransportHash(destination); Check(TransportHash(destination) == hash, "transport hash repeatability"); Console.WriteLine($"Deterministic render-transport hash: 0x{hash:X16}");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037;
    for (var i = 0; i < 100_000; i++) { Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "warm build"); checksum = Mix(checksum, (ulong)BitConverter.SingleToInt32Bits(destination.Objects[1].Position.HighX)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && checksum != 0, "warm successful builds allocate zero bytes");
    before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "warm mismatch"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm mismatch builds allocate zero bytes");
}
static void CameraSnapshotAllocationTest()
{
    var root = new ReferenceFrameId(1); var snapshot = new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root, null, ReferenceFrameKind.Ecl, "root"), CelestialFrameFactory.RootEcl())]); var resolver = new ReferenceFrameResolver(snapshot); var state = new CameraState(new FramePosition(root, new Double3(4e12, -3e12, 7e12)), DoubleQuaternion.Identity, new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1000d), CameraMode.Free);
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var first, out _, out _), "camera snapshot setup"); var hash = CameraHash(first); Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var second, out _, out _) && CameraHash(second) == hash, "camera snapshot deterministic result");
    var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out _, out _, out _), "warm camera snapshot"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm camera snapshots allocate zero bytes"); Console.WriteLine($"Deterministic camera snapshot hash: 0x{hash:X16}");
}
static void StaticReferenceFrameFixtureTransportTest()
{
    var root = new ReferenceFrameId(1); var planet = new ReferenceFrameId(2); var moon = new ReferenceFrameId(3); var vessel = new ReferenceFrameId(4);
    var builder = new ReferenceFrameGraphBuilder();
    builder.Add(new ReferenceFrameNode(root, null, ReferenceFrameKind.Ecl, "fixture-ecl"));
    builder.Add(new ReferenceFrameNode(planet, root, ReferenceFrameKind.Cce, "fixture-cce"));
    builder.Add(new ReferenceFrameNode(moon, planet, ReferenceFrameKind.Cci, "fixture-cci"));
    builder.Add(new ReferenceFrameNode(vessel, moon, ReferenceFrameKind.Ccf, "fixture-ccf"));
    var graph = builder.Build();
    var transforms = new ReferenceFrameTransformSet(graph,
    [
        new ReferenceFrameEvaluation(root, new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true)),
        new ReferenceFrameEvaluation(planet, new EvaluatedReferenceFrame(new FrameTransform(new Double3(100, 20, 0), DoubleQuaternion.Identity), new Double3(1, 0, 0), Double3.Zero, true)),
        new ReferenceFrameEvaluation(moon, new EvaluatedReferenceFrame(new FrameTransform(new Double3(0, 10, 0), DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d)), new Double3(0, 2, 0), new Double3(0, 0, .5d), false)),
        new ReferenceFrameEvaluation(vessel, new EvaluatedReferenceFrame(new FrameTransform(new Double3(2, 0, 0), DoubleQuaternion.Identity), Double3.Zero, Double3.Zero, false)),
    ]);
    Span<ReferenceFrameId> sourcePath = stackalloc ReferenceFrameId[4]; Span<ReferenceFrameId> targetPath = stackalloc ReferenceFrameId[4]; Span<ReferenceFrameId> traversalPath = stackalloc ReferenceFrameId[7];
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, root, root, sourcePath, targetPath, traversalPath, out var starTransform) == ReferenceFrameTransformResolutionStatus.Success, "star resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, planet, root, sourcePath, targetPath, traversalPath, out var planetTransform) == ReferenceFrameTransformResolutionStatus.Success, "planet resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, moon, root, sourcePath, targetPath, traversalPath, out var moonTransform) == ReferenceFrameTransformResolutionStatus.Success, "moon resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, vessel, root, sourcePath, targetPath, traversalPath, out var vesselTransform) == ReferenceFrameTransformResolutionStatus.Success, "vessel resolution");
    var objects = new[]
    {
        new ResolvedRenderObject(new RenderObjectId(1), new UniversePosition(starTransform.ConvertPosition(Double3.Zero), root), starTransform.ConvertOrientation(DoubleQuaternion.Identity), new Double3(200,200,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(2), new UniversePosition(planetTransform.ConvertPosition(Double3.Zero), root), (planetTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.35d)).Normalized(), new Double3(125,125,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(3), new UniversePosition(moonTransform.ConvertPosition(Double3.Zero), root), (moonTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.20d)).Normalized(), new Double3(22,22,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(4), new UniversePosition(vesselTransform.ConvertPosition(Double3.Zero), root), (vesselTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,-.35d)).Normalized(), new Double3(16,16,1), MeshHandle.Triangle),
    };
    Check(objects[0].RootPosition.Value == Double3.Zero && objects[1].RootPosition.Value == new Double3(100,20,0) && objects[2].RootPosition.Value == new Double3(100,30,0) && objects[3].RootPosition.Value == new Double3(100,32,0), "approved root positions");
    Check(objects[0].Id.Value == 1 && objects[1].Id.Value == 2 && objects[2].Id.Value == 3 && objects[3].Id.Value == 4, "stable object ordering");
    Check(objects[0].Scale == new Double3(200,200,1) && objects[1].Scale == new Double3(125,125,1) && objects[2].Scale == new Double3(22,22,1) && objects[3].Scale == new Double3(16,16,1), "refined presentation scales");
    Check(ResolvedRenderSnapshot.TryCreate(objects, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null && snapshot.RootFrame == root, "fixture snapshot");
    var cameraRoot = new UniversePosition(new Double3(50,16,70), root); var camera = Camera(cameraRoot); var submission = new RenderFrameSubmission(4);
    Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "fixture submission");
    Check(submission.ObjectCount == 4 && submission.BatchCount == 1 && submission.Batches[0].Mesh == MeshHandle.Triangle && submission.Batches[0].FirstObject == 0 && submission.Batches[0].ObjectCount == 4, "fixture batch");
    VerifyFixtureViewport(objects, root, cameraRoot, 16d / 9d, 2560, 1440, "16:9");
    VerifyFixtureViewport(objects, root, cameraRoot, 3440d / 1440d, 3440, 1440, "3440x1440");
    var hash = FixtureSetupHash(objects); Check(hash == FixtureSetupHash(objects), "fixture setup hash repeatability"); Console.WriteLine($"Deterministic fixture render setup hash: 0x{hash:X16}");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037UL;
    for (var i = 0; i < 100_000; i++) { Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm fixture build"); checksum = Mix(checksum, (ulong)BitConverter.SingleToInt32Bits(submission.Objects[3].Position.HighX)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && checksum != 0, "warm fixture frame assembly allocates zero bytes");
}
static void DynamicReferenceFrameFixturePublicationTest()
{
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var scene, out var diagnostics, out var createError) && scene is not null, $"dynamic fixture creation: {createError}");
    Check(scene!.GraphConstructionCount == 1 && scene.CurrentTime == SimulationInstant.Zero, "dynamic topology constructed once");
    var zero = DynamicReferenceFrameFixtureScene.EvaluateKinematics(SimulationInstant.Zero);
    CheckNear(zero.MoonLocalPosition, new Double3(0, 10, 0), "moon zero position"); CheckNear(zero.VesselLocalPosition, new Double3(3, 0, 0), "vessel zero position"); CheckNear(zero.MoonLocalVelocity, new Double3(-2, 0, 0), "moon zero velocity"); CheckNear(zero.VesselLocalVelocity, new Double3(0, 2.55, 0), "vessel zero velocity");
    var oneSecond = SimulationInstant.FromWholeSeconds(1); var one = DynamicReferenceFrameFixtureScene.EvaluateKinematics(oneSecond);
    CheckNear(one.MoonLocalPosition, new Double3(10 * Math.Cos(Math.PI / 2d + .20d), 10 * Math.Sin(Math.PI / 2d + .20d), 0), "moon one-second position"); CheckNear(one.VesselLocalPosition, new Double3(3 * Math.Cos(.85d), 3 * Math.Sin(.85d), 0), "vessel one-second position");
    Check(scene.TryBuildCandidateForTest(SimulationInstant.FromWholeSeconds(5), out var firstCandidate, out var firstError) && firstCandidate is not null, $"first candidate: {firstError}"); Check(scene.TryBuildCandidateForTest(SimulationInstant.FromWholeSeconds(5), out var secondCandidate, out var secondError) && secondCandidate is not null, $"second candidate: {secondError}");
    Check(DynamicSnapshotHash(SimulationInstant.FromWholeSeconds(5), firstCandidate!) == DynamicSnapshotHash(SimulationInstant.FromWholeSeconds(5), secondCandidate!), "same time candidate repeatability");
    var retained = scene.CurrentSnapshot; var retainedHash = DynamicSnapshotHash(scene.CurrentTime, retained); Check(!scene.TryPublishCandidateForTest(SimulationInstant.FromWholeSeconds(5), true, out _), "controlled candidate rejection"); Check(ReferenceEquals(scene.CurrentSnapshot, retained) && DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot) == retainedHash, "rejection retains prior immutable snapshot");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), out var advanceError), $"whole advance: {advanceError}"); var wholeHash = DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot);
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var partitioned, out var partitionDiagnostics, out var partitionError) && partitioned is not null, $"partition fixture creation: {partitionError}"); for (var index = 0; index < 10; index++) Check(partitioned!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(100_000), out var partitionAdvanceError), $"partition advance: {partitionAdvanceError}");
    Check(scene.CurrentTime == partitioned!.CurrentTime && wholeHash == DynamicSnapshotHash(partitioned.CurrentTime, partitioned.CurrentSnapshot), "frame partition independence"); Check(diagnostics.ScriptedSequenceHash == partitionDiagnostics.ScriptedSequenceHash, "restart scripted sequence repeatability");
    var root = new ReferenceFrameId(1); var initialCameraRoot = new UniversePosition(new Double3(50, 16, 70), root); VerifyFixtureViewport(scene.CurrentSnapshot.Objects, root, initialCameraRoot, 16d / 9d, 2560, 1440, "dynamic 16:9");
    ulong scriptedHash = 14695981039346656037UL;
    foreach (var seconds in new long[] { 0, 1, 5, 10, 100 })
    {
        var time = SimulationInstant.FromWholeSeconds(seconds);
        Check(scene.TryBuildCandidateForTest(time, out var scriptedCandidate, out var scriptedError) && scriptedCandidate is not null, $"scripted candidate {seconds}: {scriptedError}");
        var snapshotHash = DynamicSnapshotHash(time, scriptedCandidate!);
        scriptedHash = Mix(Mix(scriptedHash, (ulong)time.Ticks), FixtureSetupHash(scriptedCandidate!.Objects));
        Console.WriteLine($"Dynamic snapshot hash t={seconds}s: 0x{snapshotHash:X16}");
    }
    Check(scriptedHash == diagnostics.ScriptedSequenceHash, "scripted snapshot sequence hash");
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var sequencePublication, out _, out var sequencePublicationError) && sequencePublication is not null, $"sequence publication fixture: {sequencePublicationError}");
    var beforeSequencePublication = GC.GetAllocatedBytesForCurrentThread();
    foreach (var duration in new long[] { 1, 4, 5, 90 }) Check(sequencePublication!.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(duration), out var sequenceAdvanceError), $"sequence publication advance: {sequenceAdvanceError}");
    var sequencePublicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforeSequencePublication;
    Check(sequencePublicationBytes > 0 && sequencePublication!.CurrentTime == SimulationInstant.FromWholeSeconds(100), "scripted immutable publication allocations measured");
    Console.WriteLine($"Dynamic scripted publication allocations: {sequencePublicationBytes} bytes/4 updates");
    var publication = partitioned; _ = publication.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out _); var beforePublication = GC.GetAllocatedBytesForCurrentThread(); const int publicationIterations = 100; for (var index = 0; index < publicationIterations; index++) Check(publication.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out var publicationError), $"publication: {publicationError}"); var publicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforePublication; Check(publicationBytes > 0 && publication.GraphConstructionCount == 1, "immutable publication allocations measured without topology rebuild"); Console.WriteLine($"Dynamic publication allocations: {publicationBytes / publicationIterations} bytes/update ({publicationBytes} bytes/{publicationIterations} updates)");
    var cameraRoot = new UniversePosition(new Double3(50, 16, 70), new ReferenceFrameId(1)); var frame = new RenderFrameSubmission(4); var camera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(publication.CurrentSnapshot, camera, cameraRoot, frame) == ResolvedRenderSubmissionBuildStatus.Success, "dynamic frame setup"); beforePublication = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(publication.CurrentSnapshot, camera, cameraRoot, frame) == ResolvedRenderSubmissionBuildStatus.Success, "warm dynamic assembly"); Check(GC.GetAllocatedBytesForCurrentThread() == beforePublication, "warm dynamic frame assembly allocates zero bytes");
    Console.WriteLine($"Dynamic scripted-sequence hash: 0x{diagnostics.ScriptedSequenceHash:X16}");
}
static void CelestialAnalyticalFixturePublicationTest()
{
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var createError) && scene is not null, $"celestial scene creation: {createError}");
    Check(scene!.CurrentTime == SimulationInstant.Zero && scene.CurrentSnapshot.Count == 3 && scene.CurrentSnapshot.OrbitCurve?.Count == 257 && scene.CurrentSnapshot.PreviousOrbitCurve is null && scene.CurrentSnapshot.Objects[2].Scale == Double3.Zero && scene.OrbitCurveBuildCount == 1, "celestial initial snapshot and curve");
    var initialAttitude = scene.CurrentSnapshot.Objects[1].RootOrientation;
    Check(scene.CurrentSnapshot.Objects[0].RootPosition.Value == Double3.Zero, "celestial root marker identity");
    Check(Math.Abs(scene.CurrentSnapshot.Objects[1].RootPosition.Value.X - 10d) < 1e-12d && scene.CurrentSnapshot.Objects[1].RootPosition.Value.Y == 0d, "SI presentation scaling");
    var root = new ReferenceFrameId(1); var celestialCamera = CelestialAnalyticalScene.Camera; var presentationCamera = new CameraState(new FramePosition(root, celestialCamera.Position), DoubleQuaternion.Identity, celestialCamera.Projection, CameraMode.Free); var initialDistance = scene.OrbitDistance;
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = 1 }, out var rateChanged, out var pauseChanged); Check(!rateChanged && !pauseChanged && scene.OrbitDistance < initialDistance, "positive wheel zooms nearer");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _); Check(Math.Abs(scene.OrbitDistance - initialDistance) < 1e-12d, "negative wheel zooms farther");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = 100 }, out _, out _); Check(scene.OrbitDistance == 2d, "minimum zoom clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = -200 }, out _, out _); Check(scene.OrbitDistance == 500d, "maximum zoom clamp"); scene.ResetPresentationCamera(presentationCamera);
    scene.ResetPresentationCamera(presentationCamera); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaX = 10 }, out _, out _); Check(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).X > 0d, "right drag orbits right");
    scene.ResetPresentationCamera(presentationCamera); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaY = -10 }, out _, out _); Check(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).Y > 0d, "up drag orbits up");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaY = -1_000_000 }, out _, out _); Check(Math.Abs(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).Y) < 1d, "orbit pitch clamp");
    var immutableBeforeControls = scene.CurrentSnapshot; var curveBuildsBeforeControls = scene.OrbitCurveBuildCount; scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out rateChanged, out pauseChanged); Check(rateChanged && !pauseChanged && scene.Rate == new SimulationRate(5_000, 1), "rate decrease step"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { PauseToggle = 1 }, out rateChanged, out pauseChanged); Check(!rateChanged && pauseChanged && scene.IsPaused && scene.CurrentTime == SimulationInstant.Zero && ReferenceEquals(immutableBeforeControls, scene.CurrentSnapshot), "pause is presentation input only"); Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), out var pausedError) && scene.CurrentTime == SimulationInstant.Zero && scene.CurrentSnapshot.Objects[1].RootOrientation == initialAttitude, $"pause freezes attitude: {pausedError}"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { PauseToggle = 1 }, out _, out _); Check(!scene.IsPaused, "resume toggle"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out rateChanged, out _); Check(rateChanged && scene.Rate == new SimulationRate(10_000, 1), "rate increase step"); for (var index = 0; index < 6; index++) scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out _, out _); Check(scene.Rate == SimulationRate.One, "1x lower clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out rateChanged, out _); Check(!rateChanged && scene.Rate == SimulationRate.One, "1x remains clamped"); for (var index = 0; index < 6; index++) scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out _, out _); Check(scene.Rate == new SimulationRate(50_000, 1), "50000x upper clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out rateChanged, out _); Check(!rateChanged && scene.Rate == new SimulationRate(50_000, 1), "50000x remains clamped"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out _, out _); Check(scene.Rate == new SimulationRate(10_000, 1) && scene.OrbitCurveBuildCount == curveBuildsBeforeControls && scene.CurrentSnapshot.Objects[1].RootOrientation == initialAttitude, "camera/rate input does not alter attitude without time advancement");
    var retained = scene.CurrentSnapshot; var retainedHash = DynamicSnapshotHash(scene.CurrentTime, retained);
    Check(!scene.TryPublishCandidateForTest(true, out _), "celestial controlled candidate rejection"); Check(ReferenceEquals(retained, scene.CurrentSnapshot) && DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot) == retainedHash, "celestial rejection retains prior snapshot");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(9.9999d), out var preImpulseError), $"celestial pre-impulse advance: {preImpulseError}"); var beforeImpulse = scene.CurrentSnapshot; Check(beforeImpulse.Objects[1].RootOrientation == initialAttitude, "stationary fixture remains stationary without player torque");
    var initialCurve = beforeImpulse.OrbitCurve; var beforeImpulseAllocation = GC.GetAllocatedBytesForCurrentThread(); Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(.0001d), out var impulseError), $"celestial impulse advance: {impulseError}"); var impulseCurveBytes = GC.GetAllocatedBytesForCurrentThread() - beforeImpulseAllocation; Check(scene.CurrentTime == SimulationInstant.FromWholeSeconds(100_000) && !ReferenceEquals(beforeImpulse, scene.CurrentSnapshot) && scene.OrbitCurveBuildCount == 2 && !ReferenceEquals(initialCurve, scene.CurrentSnapshot.OrbitCurve) && ReferenceEquals(initialCurve, scene.CurrentSnapshot.PreviousOrbitCurve) && scene.CurrentSnapshot.Objects[2].Scale.X > 0d && impulseCurveBytes > 0, "canonical impulse publication includes one ghost and burn marker");
    var hash = DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot); var activeOrbitHash = OrbitHash(scene.CurrentSnapshot.OrbitCurve!); var ghostOrbitHash = OrbitHash(scene.CurrentSnapshot.PreviousOrbitCurve!); var burnHash = MixDouble3(14695981039346656037UL, scene.CurrentSnapshot.Objects[2].RootPosition.Value); Check(activeOrbitHash != ghostOrbitHash, "active and ghost curves differ"); Check(CelestialAnalyticalScene.TryCreate(out var replay, out var replayError) && replay is not null, $"celestial replay creation: {replayError}"); Check(replay!.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(10), out var replayAdvanceError), $"celestial replay advance: {replayAdvanceError}"); Check(hash == DynamicSnapshotHash(replay.CurrentTime, replay.CurrentSnapshot), "celestial exact-time replay");
    var cameraRoot = new UniversePosition(new Double3(0, 0, 24), root); var camera = Camera(cameraRoot); var submission = new RenderFrameSubmission(3, 257); Check(ResolvedRenderSubmissionBuilder.TryBuild(scene.CurrentSnapshot, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.PreviousOrbitVertexCount == 257, "celestial submission");
    var beforeSubmission = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(scene.CurrentSnapshot, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm celestial submission"); Check(GC.GetAllocatedBytesForCurrentThread() == beforeSubmission, "warm celestial submission allocation");
    _ = scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out _); var beforePublication = GC.GetAllocatedBytesForCurrentThread(); const int publicationIterations = 20; for (var index = 0; index < publicationIterations; index++) Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out var publicationError), $"celestial publication: {publicationError}"); var publicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforePublication; Check(publicationBytes > 0, "celestial immutable publication allocation measured"); Console.WriteLine($"Celestial fixture snapshot hash: 0x{hash:X16}; active=0x{activeOrbitHash:X16}; ghost=0x{ghostOrbitHash:X16}; burn=0x{burnHash:X16}; curve replacement/publication allocations: {impulseCurveBytes} bytes; unchanged publication allocations: {publicationBytes / publicationIterations} bytes/update ({publicationBytes} bytes/{publicationIterations} updates)");
}
static void VerifyFixtureViewport(ReadOnlySpan<ResolvedRenderObject> objects, ReferenceFrameId root, UniversePosition cameraRoot, double aspect, int width, int height, string label)
{
    var frames = new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root, null, ReferenceFrameKind.Ecl, "root"), CelestialFrameFactory.RootEcl())]); var resolver = new ReferenceFrameResolver(frames);
    var projection = new CameraProjection(Math.PI / 3d, aspect, .01d, 1000d); projection.Validate();
    var state = new CameraState(new FramePosition(root, cameraRoot.Value), DoubleQuaternion.Identity, projection, CameraMode.Free);
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var camera, out var resolvedCamera, out _), $"{label} fixture camera"); Check(resolvedCamera == cameraRoot, $"{label} camera root");
    Span<ProjectedBounds> projected = stackalloc ProjectedBounds[4];
    for (var index = 0; index < objects.Length; index++)
    {
        projected[index] = ProjectBounds(objects[index], camera, cameraRoot.Value);
        Check(projected[index].CenterX is > -.9d and < .9d && projected[index].CenterY is > -.9d and < .9d, $"{label} marker center inside viewport");
        Check(projected[index].MinX > -1d && projected[index].MaxX < 1d && projected[index].MinY > -1d && projected[index].MaxY < 1d, $"{label} marker bounds inside viewport");
        var pixelHeight = (projected[index].MaxY - projected[index].MinY) * height * .5d;
        Check(pixelHeight >= 18d, $"{label} marker visibility threshold"); Check(pixelHeight <= height * .25d, $"{label} marker maximum size");
    }
    var dx = (projected[2].CenterX - projected[3].CenterX) * width * .5d; var dy = (projected[2].CenterY - projected[3].CenterY) * height * .5d; var separation = Math.Sqrt(dx * dx + dy * dy);
    Check(separation >= 30d, $"{label} Moon/Vessel separation"); var minHeight=Math.Min(Math.Min(projected[0].PixelHeight(height), projected[1].PixelHeight(height)), Math.Min(projected[2].PixelHeight(height), projected[3].PixelHeight(height))); var maxHeight=Math.Max(Math.Max(projected[0].PixelHeight(height), projected[1].PixelHeight(height)), Math.Max(projected[2].PixelHeight(height), projected[3].PixelHeight(height))); Console.WriteLine($"Fixture {label}: minHeight={minHeight:F1}px, maxHeight={maxHeight:F1}px, Moon/Vessel={separation:F1}px");
}
static ProjectedBounds ProjectBounds(in ResolvedRenderObject value, in GpuCameraData camera, in Double3 cameraRoot)
{
    ReadOnlySpan<Double3> vertices = [new(0,-.04,0), new(.04,.04,0), new(-.04,.04,0)]; var relative = EncodedPosition.Resolve(EncodedPosition.Encode(value.RootPosition.Value), camera.Position).Value;
    var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity; var minY = double.PositiveInfinity; var maxY = double.NegativeInfinity;
    foreach (ref readonly var vertex in vertices)
    {
        var local = value.RootOrientation.Rotate(new Double3(vertex.X * value.Scale.X, vertex.Y * value.Scale.Y, vertex.Z * value.Scale.Z)); var point = local + relative;
        var x = camera.ViewProjection.C0R0 * point.X + camera.ViewProjection.C1R0 * point.Y + camera.ViewProjection.C2R0 * point.Z + camera.ViewProjection.C3R0;
        var y = camera.ViewProjection.C0R1 * point.X + camera.ViewProjection.C1R1 * point.Y + camera.ViewProjection.C2R1 * point.Z + camera.ViewProjection.C3R1;
        var w = camera.ViewProjection.C0R3 * point.X + camera.ViewProjection.C1R3 * point.Y + camera.ViewProjection.C2R3 * point.Z + camera.ViewProjection.C3R3;
        var ndcX = x / w; var ndcY = y / w; minX = Math.Min(minX, ndcX); maxX = Math.Max(maxX, ndcX); minY = Math.Min(minY, ndcY); maxY = Math.Max(maxY, ndcY);
    }
    return new ProjectedBounds(minX, maxX, minY, maxY);
}
static ResolvedRenderObject Object(uint id, UniversePosition position, MeshHandle mesh) => new(new RenderObjectId(id), position, DoubleQuaternion.Identity, new Double3(1, 1, 1), mesh);
static GpuCameraData Camera(in UniversePosition position) => new() { Position = EncodedPosition.Encode(position.Value), ViewProjection = new Float4x4 { C0R0 = 1, C1R1 = 1, C2R2 = 1, C3R3 = 1 } };
static ulong TransportHash(RenderFrameSubmission submission) { ulong hash = 14695981039346656037; foreach (ref readonly var value in submission.Objects) { hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.LowX)); hash = Mix(hash, value.Mesh.Value); } foreach (ref readonly var batch in submission.Batches) { hash = Mix(hash, batch.Mesh.Value); hash = Mix(hash, batch.FirstObject); hash = Mix(hash, batch.ObjectCount); } return hash; }
static ulong CameraHash(in GpuCameraData camera) { ulong hash = 14695981039346656037; hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C0R0)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C1R1)); return Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C2R2)); }
static ulong FixtureSetupHash(ReadOnlySpan<ResolvedRenderObject> objects) { ulong hash = 14695981039346656037UL; foreach (ref readonly var value in objects) { hash = Mix(hash, value.Id.Value); hash = Mix(hash, (ulong)value.RootPosition.Frame.Value); hash = MixDouble3(hash, value.RootPosition.Value); hash = MixQuaternion(hash, value.RootOrientation); hash = MixDouble3(hash, value.Scale); hash = Mix(hash, value.Mesh.Value); } return hash; }
static ulong DynamicSnapshotHash(SimulationInstant time, ResolvedRenderSnapshot snapshot) { ulong hash = Mix(14695981039346656037UL, (ulong)time.Ticks); return Mix(hash, FixtureSetupHash(snapshot.Objects)); }
static ulong OrbitHash(ResolvedOrbitCurve curve) { ulong hash = 14695981039346656037UL; foreach (ref readonly var position in curve.Positions) hash = MixDouble3(hash, position.Value); return hash; }
static ulong IndicatorHash(in ResolvedDirectionIndicator indicator) => MixDouble3(MixDouble3(14695981039346656037UL, indicator.Start.Value), indicator.End.Value);
static ulong MixDouble3(ulong hash, in Double3 value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); }
static ulong MixQuaternion(ulong hash, in DoubleQuaternion value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.W)); }
static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
static void Throws<T>(Action action) where T : Exception { try { action(); throw new Exception($"Expected {typeof(T).Name}"); } catch (T) { } }
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void CheckNear(in Double3 actual, in Double3 expected, string message) { if ((actual - expected).LengthSquared > 1e-18) throw new Exception(message); }
readonly record struct ProjectedBounds(double MinX, double MaxX, double MinY, double MaxY)
{
    public double CenterX => (MinX + MaxX) * .5d;
    public double CenterY => (MinY + MaxY) * .5d;
    public double PixelHeight(int height) => (MaxY - MinY) * height * .5d;
}
