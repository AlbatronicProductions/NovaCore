using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Interop;
using System.Diagnostics;

var frame = new ReferenceFrameId(1);
var projection = new CameraProjection(1, 1, .1, 100);
var initial = new FramePosition(frame, new Double3(4e12, -3e12, 7e12));

CameraState NewState() => new(initial, DoubleQuaternion.Identity, projection, CameraMode.Free);
CameraCommand Speed(int detents) => new(CameraCommandKind.AdjustSpeed, new Double3(detents, 0, 0), default);

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 1e-12, message);

var state = NewState();
var controller = new FreeCameraController(1, .01, 1.55);
controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(0, 0, -1), default)], 1);
var forward = state.Position;
Check(forward.Value.Z < initial.Value.Z && forward.Frame == frame, "forward mapping/frame");
state.Position = initial;
controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(0, 0, 1), default)], 1);
var backward = state.Position;
Check(backward.Value.Z > initial.Value.Z && Math.Abs((forward.Value - initial.Value).Z + (backward.Value - initial.Value).Z) < 1e-12, "forward/back symmetry");

state.Position = initial; controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(1, 0, 0), default)], 1); var right = state.Position;
state.Position = initial; controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(-1, 0, 0), default)], 1); var left = state.Position;
Check(right.Value.X > initial.Value.X && left.Value.X < initial.Value.X, "strafe signs");
state.Position = initial; controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(0, 1, 0), default)], 1); var up = state.Position;
state.Position = initial; controller.Update(state, [new(CameraCommandKind.MoveLocal, new Double3(0, -1, 0), default)], 1); var down = state.Position;
Check(up.Value.Y > initial.Value.Y && down.Value.Y < initial.Value.Y, "vertical signs");
var look = NewState(); controller.Update(look, [new(CameraCommandKind.Look, default, new Double2(1, 0))], 0); Check(look.Orientation.Rotate(new Double3(0, 0, -1)).X > 0d, "right drag looks right");
look = NewState(); controller.Update(look, [new(CameraCommandKind.Look, default, new Double2(0, -1))], 0); Check(look.Orientation.Rotate(new Double3(0, 0, -1)).Y > 0d, "up drag looks up");

var local = new CameraState(new FramePosition(frame, Double3.Zero), DoubleQuaternion.Identity, projection, CameraMode.Free);
var before = local.Position;
controller.Update(local, [new(CameraCommandKind.MoveLocal, new Double3(1, 1, 0), default)], 1);
Near((local.Position.Value - before.Value).LengthSquared, .01, "diagonal normalization");

var speedController = new FreeCameraController(1, .01, 1.55);
speedController.Update(NewState(), [Speed(1)], 0);
var increased = speedController.MovementSpeed;
Check(increased > 1, "positive wheel increases speed");
speedController.Update(NewState(), [Speed(-1)], 0);
Near(speedController.MovementSpeed, 1, "positive/negative reciprocal");
speedController.Update(NewState(), [Speed(100)], 0);
Near(speedController.MovementSpeed, FreeCameraController.MaximumSpeed, "maximum clamp");
speedController.Update(NewState(), [Speed(-1000)], 0);
Near(speedController.MovementSpeed, FreeCameraController.MinimumSpeed, "minimum clamp");

var movement = NewState();
var distanceBefore = movement.Position.Value;
var movementController = new FreeCameraController(1, .01, 1.55);
movementController.Update(movement, [new(CameraCommandKind.MoveLocal, new Double3(0, 0, -1), default)], .05);
var baseline = Math.Sqrt((movement.Position.Value - distanceBefore).LengthSquared);
movement.Position = initial;
movementController.Update(movement, [Speed(4)], 0);
movementController.Update(movement, [new(CameraCommandKind.MoveLocal, new Double3(0, 0, -1), default)], .05);
Near(Math.Sqrt((movement.Position.Value - initial.Value).LengthSquared), baseline * 2, "movement uses adjusted persistent speed");
movementController.Reset();
Near(movementController.MovementSpeed, 1, "reset restores default speed");

Span<CameraCommand> commands = stackalloc CameraCommand[4];
var input = new NativeInputState { MoveForward = 1, LookActive = 1, MouseDeltaX = 1, MouseWheelDetents = -1, Reset = 1 };
var count = DebugCameraInput.Map(input, commands);
Check(count == 4 && commands[2].Kind == CameraCommandKind.AdjustSpeed && commands[2].LocalDirection.X == -1, "wheel maps once with full bounded command buffer");
input.MouseWheelDetents = 0;
Check(DebugCameraInput.Map(input, commands) == 3, "zero wheel produces no speed command and consumed snapshot does not repeat");
Check(new NativeInputState { MouseWheelDetents = 1 }.MouseWheelDetents > 0 && new NativeInputState { MouseWheelDetents = -1 }.MouseWheelDetents < 0, "signed native wheel representation");

var allocationState = NewState();
var allocationController = new FreeCameraController(1, .01, 1.55);
var allocationCommands = new[] { new CameraCommand(CameraCommandKind.MoveLocal, new Double3(1, 0, 0), default) };
var allocationBuffer = new CameraCommand[4];
allocationController.Update(allocationState, allocationCommands, .01);
DebugCameraInput.Map(default, allocationBuffer);
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < 1_000; i++) { allocationController.Update(allocationState, allocationCommands, .01); DebugCameraInput.Map(default, allocationBuffer); }
Check(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore, "steady-state input mapping/controller allocations");

var timedState=NewState();var timedController=new FreeCameraController(1,.01,1.55);timedController.Update(timedState,allocationCommands,.01);var timedAllocatedBefore=GC.GetAllocatedBytesForCurrentThread();var timedStarted=Stopwatch.GetTimestamp();for(var index=0;index<1_000_000;index++)timedController.Update(timedState,allocationCommands,.000001);var timedElapsed=Stopwatch.GetElapsedTime(timedStarted);var timedAllocated=GC.GetAllocatedBytesForCurrentThread()-timedAllocatedBefore;Check(timedAllocated==0&&timedState.Position.Value.IsFinite,"camera update benchmark remains finite and allocation-free");

Console.WriteLine($"PASS camera controller: W delta={(forward.Value - initial.Value).Z:R}, S delta={(backward.Value - initial.Value).Z:R}, wheel 1={increased:R}; update={timedElapsed.TotalNanoseconds/1_000_000d:F2} ns, allocations={timedAllocated}");
