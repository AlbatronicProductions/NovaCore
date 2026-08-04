using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Platform;

if (!LogOptions.TryParse(args, out var logOptions, out var logError))
{
    Console.Error.WriteLine(logError);
    return 2;
}

return Run(logOptions);

static unsafe int Run(LogOptions logOptions)
{
    var frame = new ReferenceFrameId(1);
    var diagnosticCamera = new RenderOrigin(new UniversePosition(new Double3(4_000_000_000_000d, -3_000_000_000_000d, 7_000_000_000_000d), frame));
    var diagnosticObject = new UniversePosition(new Double3(4_000_000_000_000d + 0.25d, -3_000_000_000_000d - 0.125d, 7_000_000_000_000d + 0.5d), frame);
    var visualObject = new UniversePosition(diagnosticCamera.CameraPosition.Value, frame);
    var state = new CameraState(diagnosticCamera, visualObject, logOptions);
    var renderObject = RenderSubmission.CreateObject(visualObject, state.Origin, MeshHandle.Triangle);
    NativeRenderObject nativeObject = ToNativeObject(renderObject);
    NativeFrameSubmission submission = new() { Camera = ToNativePosition(RenderSubmission.EncodeCamera(state.Origin)), Objects = &nativeObject, ObjectCount = 1 };

    if (logOptions.IsEnabled(LogCategory.Precision)) PrintPrecisionReport(diagnosticCamera, diagnosticObject);
    if (logOptions.IsEnabled(LogCategory.Startup)) PrintVisualMode(state, renderObject.Position, submission.Camera);
    var handle = GCHandle.Alloc(state);
    try
    {
        var result = NativeRuntime.RunRenderer(&submission, HostCallback, GCHandle.ToIntPtr(handle));
        return result == NativeResult.Success ? 0 : (int)result;
    }
    finally { handle.Free(); }
}

static unsafe void HostCallback(NativeHostEvent* hostEvent, IntPtr userData)
{
    var state = (CameraState)GCHandle.FromIntPtr(userData).Target!;
    if (hostEvent->Type == NativeHostEventType.Diagnostic)
    {
        var category = (LogCategory)hostEvent->LogCategory;
        if (category is LogCategory.None or LogCategory.Validation || state.LogOptions.IsEnabled(category))
            Console.WriteLine($"[native] {Marshal.PtrToStringUTF8((IntPtr)hostEvent->Utf8Message)}");
        return;
    }

    var input = hostEvent->Input;
    var frameDeltaSeconds = Math.Clamp((double)input.DeltaSeconds, 0d, 0.1d);
    var direction = new Double3((input.MoveRight != 0 ? 1d : 0d) - (input.MoveLeft != 0 ? 1d : 0d), (input.MoveForward != 0 ? 1d : 0d) - (input.MoveBackward != 0 ? 1d : 0d), 0d);
    var cameraBefore = state.Origin.CameraPosition;
    var reset = NativeKeyboard.IsDown('R');
    var movement = reset ? new Double3(0d, 0d, 0d) : direction * (CameraState.MovementSpeed * frameDeltaSeconds);
    if (reset) state.Origin = state.InitialOrigin;
    else if (direction.X != 0d || direction.Y != 0d)
        state.Origin = new RenderOrigin(new UniversePosition(cameraBefore.Value + movement, cameraBefore.Frame));
    hostEvent->Submission->Camera = ToNativePosition(RenderSubmission.EncodeCamera(state.Origin));
    if (state.LogOptions.IsEnabled(LogCategory.Input) && (reset || direction.X != 0d || direction.Y != 0d))
        LogMovement(reset ? "R" : DirectionName(direction), input.DeltaSeconds, movement, cameraBefore, state.Origin.CameraPosition, state.ObjectPosition);
    state.ReportElapsed += input.DeltaSeconds;
    if (state.LogOptions.IsEnabled(LogCategory.Input) && state.ReportElapsed >= 1f)
    {
        state.ReportElapsed = 0f;
        PrintCurrentVisualState(state);
    }
}

static NativeRenderObject ToNativeObject(RenderObject value) => new() { Position = ToNativePosition(value.Position), Mesh = value.Mesh.Value };
static NativeEncodedPosition ToNativePosition(EncodedPosition value) => new() { HighX = value.HighX, HighY = value.HighY, HighZ = value.HighZ, LowX = value.LowX, LowY = value.LowY, LowZ = value.LowZ };

static void PrintPrecisionReport(RenderOrigin camera, UniversePosition objectPosition)
{
    var objectEncoded = EncodedPosition.Encode(objectPosition.Value);
    var cameraEncoded = EncodedPosition.Encode(camera.CameraPosition.Value);
    var expected = ReferenceFrame.Resolve(objectPosition, camera);
    var reconstructed = EncodedPosition.Resolve(objectEncoded, cameraEncoded);
    var plainFloatRelative = (float)objectPosition.Value.X - (float)camera.CameraPosition.Value.X;
    Console.WriteLine("Precision diagnostic coordinates (not the interactive visual object):");
    Console.WriteLine($"Object UniversePosition: {objectPosition.Value}");
    Console.WriteLine($"Camera UniversePosition: {camera.CameraPosition.Value}");
    Console.WriteLine($"Expected relative position: {expected.Value}");
    Console.WriteLine($"Object encoded high/low: H=({objectEncoded.HighX:R}, {objectEncoded.HighY:R}, {objectEncoded.HighZ:R}) L=({objectEncoded.LowX:R}, {objectEncoded.LowY:R}, {objectEncoded.LowZ:R})");
    Console.WriteLine($"Camera encoded high/low: H=({cameraEncoded.HighX:R}, {cameraEncoded.HighY:R}, {cameraEncoded.HighZ:R}) L=({cameraEncoded.LowX:R}, {cameraEncoded.LowY:R}, {cameraEncoded.LowZ:R})");
    Console.WriteLine($"Reconstructed relative result: {reconstructed.Value}");
    Console.WriteLine($"Single-float world X result: {plainFloatRelative:R}; expected X: {expected.Value.X:R}");
}

static void PrintVisualMode(CameraState state, EncodedPosition objectEncoded, NativeEncodedPosition cameraNative)
{
    var cameraEncoded = new EncodedPosition(cameraNative.HighX, cameraNative.HighY, cameraNative.HighZ, cameraNative.LowX, cameraNative.LowY, cameraNative.LowZ);
    Console.WriteLine();
    Console.WriteLine("Interactive visual demonstration: triangle begins at render-space origin.");
    Console.WriteLine($"Visual object encoded position: {objectEncoded}");
    Console.WriteLine($"Visual camera encoded position: {cameraEncoded}");
    Console.WriteLine($"Movement speed: {CameraState.MovementSpeed:F2} relative units/second. A/D: X, W/S: Y, R: reset. Use --verbose-input for per-movement tracing.");
    PrintCurrentVisualState(state);
}

static void PrintCurrentVisualState(CameraState state) =>
    Console.WriteLine($"Visual camera UniversePosition: {state.Origin.CameraPosition.Value}; expected relative: {ReferenceFrame.Resolve(state.ObjectPosition, state.Origin).Value}; speed: {CameraState.MovementSpeed:F2} units/s");

static void LogMovement(string direction, float rawFrameDeltaSeconds, Double3 movement, UniversePosition cameraBefore, UniversePosition cameraAfter, UniversePosition objectPosition)
{
    var expected = ReferenceFrame.Resolve(objectPosition, new RenderOrigin(cameraAfter));
    var cameraEncoded = EncodedPosition.Encode(cameraAfter.Value);
    var objectEncoded = EncodedPosition.Encode(objectPosition.Value);
    var reconstructed = EncodedPosition.Resolve(objectEncoded, cameraEncoded);
    Console.WriteLine($"Movement {direction}: frameDelta={rawFrameDeltaSeconds:F6}s signedDelta={movement}; cameraBefore={cameraBefore.Value}; cameraAfter={cameraAfter.Value}; expectedRelative={expected.Value}; cameraHigh=({cameraEncoded.HighX:R}, {cameraEncoded.HighY:R}, {cameraEncoded.HighZ:R}) cameraLow=({cameraEncoded.LowX:R}, {cameraEncoded.LowY:R}, {cameraEncoded.LowZ:R}); reconstructedRelative={reconstructed.Value}");
}

static string DirectionName(Double3 direction) => direction switch
{
    { X: < 0d, Y: 0d } => "A",
    { X: > 0d, Y: 0d } => "D",
    { X: 0d, Y: > 0d } => "W",
    { X: 0d, Y: < 0d } => "S",
    _ => "combined",
};

file sealed class CameraState(RenderOrigin initialOrigin, UniversePosition objectPosition, LogOptions logOptions)
{
    public const double MovementSpeed = 0.1d;
    public RenderOrigin InitialOrigin { get; } = initialOrigin;
    public RenderOrigin Origin = initialOrigin;
    public UniversePosition ObjectPosition { get; } = objectPosition;
    public LogOptions LogOptions { get; } = logOptions;
    public float ReportElapsed;
}

internal static partial class NativeKeyboard
{
    [LibraryImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
    private static partial short GetAsyncKeyState(int virtualKey);
    public static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
