using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Platform;

if (!SampleOptions.TryParse(args, out var options, out var optionError))
{
    Console.Error.WriteLine(optionError);
    return 2;
}
if (!LogOptions.TryParse(options.LogArguments, out var logOptions, out var logError)) { Console.Error.WriteLine(logError); return 2; }
return Run(options, logOptions);

static unsafe int Run(SampleOptions options, LogOptions logOptions)
{
    var frame = new ReferenceFrameId(1);
    var initial = new RenderOrigin(new UniversePosition(new Double3(4_000_000_000_000d, -3_000_000_000_000d, 7_000_000_000_000d), frame));
    var source = SampleSceneFactory.Create(options.ObjectCount, initial.CameraPosition);
    var snapshot = new RenderFrameSubmission(source.Length);
    Assemble(snapshot, source, initial);
    var state = new CameraState(initial, source, snapshot, logOptions);
    var nativeObjects = new NativeRenderObject[source.Length];
    var nativeBatches = new NativeDrawBatch[source.Length];
    UpdateNative(snapshot, nativeObjects, nativeBatches, out var batchCount);
    var handle = GCHandle.Alloc(state);
    fixed (NativeRenderObject* objects = nativeObjects)
    fixed (NativeDrawBatch* batches = nativeBatches)
    {
        NativeFrameSubmission native = new() { Camera = ToNative(snapshot.Camera), Objects = objects, ObjectCount = (uint)nativeObjects.Length, Batches = batches, BatchCount = (uint)batchCount };
        if (logOptions.IsEnabled(LogCategory.Startup)) Console.WriteLine($"Generic mesh sample: {source.Length} objects, {batchCount} mesh batch, one indexed instanced draw. A/D/W/S move; R resets; close window to exit.");
        try { return NativeRuntime.RunRenderer(&native, HostCallback, GCHandle.ToIntPtr(handle)) == NativeResult.Success ? 0 : 1; }
        finally { handle.Free(); }
    }
}

static void Assemble(RenderFrameSubmission snapshot, SampleRenderableState[] source, RenderOrigin camera)
{
    snapshot.Begin(camera);
    foreach (ref readonly var item in source.AsSpan()) snapshot.Add(item.Position, item.Rotation, item.Scale, item.Mesh);
    snapshot.Complete();
}

static unsafe void HostCallback(NativeHostEvent* e, IntPtr data)
{
    var state = (CameraState)GCHandle.FromIntPtr(data).Target!;
    if (e->Type == NativeHostEventType.Diagnostic)
    {
        var category = (LogCategory)e->LogCategory;
        if (category is LogCategory.None or LogCategory.Validation || state.Log.IsEnabled(category)) Console.WriteLine($"[native] {Marshal.PtrToStringUTF8((IntPtr)e->Utf8Message)}");
        return;
    }
    var input = e->Input; var dt = Math.Clamp((double)input.DeltaSeconds, 0d, .1d);
    var direction = new Double3((input.MoveRight != 0 ? 1d : 0d) - (input.MoveLeft != 0 ? 1d : 0d), (input.MoveForward != 0 ? 1d : 0d) - (input.MoveBackward != 0 ? 1d : 0d), 0);
    if (NativeKeyboard.IsDown('R')) state.Camera = state.Initial;
    else if (direction.X != 0 || direction.Y != 0) state.Camera = new RenderOrigin(new UniversePosition(state.Camera.CameraPosition.Value + direction * (.1d * dt), state.Camera.CameraPosition.Frame));
    state.Snapshot.Begin(state.Camera); foreach (ref readonly var item in state.Source.AsSpan()) state.Snapshot.Add(item.Position,item.Rotation,item.Scale,item.Mesh); state.Snapshot.Complete();
    e->Submission->Camera = ToNative(state.Snapshot.Camera);
    if (state.Log.IsEnabled(LogCategory.Input) && (direction.X != 0 || direction.Y != 0)) Console.WriteLine($"Input dt={dt:F4}s camera={state.Camera.CameraPosition.Value}; relative={ReferenceFrame.Resolve(state.Source[0].Position,state.Camera).Value}");
}

static void UpdateNative(RenderFrameSubmission snapshot, NativeRenderObject[] objects, NativeDrawBatch[] batches, out int batchCount)
{
    for (var i=0;i<objects.Length;i++) { var o=snapshot.Objects[i]; objects[i]=new NativeRenderObject { Position=ToNative(o.Position), Transform=new NativeRenderTransform { RotationX=o.Transform.Rotation.X,RotationY=o.Transform.Rotation.Y,RotationZ=o.Transform.Rotation.Z,RotationW=o.Transform.Rotation.W,ScaleX=o.Transform.Scale.X,ScaleY=o.Transform.Scale.Y,ScaleZ=o.Transform.Scale.Z }, Mesh=new NativeMeshHandle { Value=o.Mesh.Value } }; }
    batchCount=snapshot.BatchCount; for(var i=0;i<batchCount;i++){var b=snapshot.Batches[i];batches[i]=new NativeDrawBatch{Mesh=new NativeMeshHandle{Value=b.Mesh.Value},FirstObject=b.FirstObject,ObjectCount=b.ObjectCount};}
}
static NativeEncodedPosition ToNative(EncodedPosition p)=>new(){HighX=p.HighX,HighY=p.HighY,HighZ=p.HighZ,LowX=p.LowX,LowY=p.LowY,LowZ=p.LowZ};
file sealed class CameraState(RenderOrigin initial, SampleRenderableState[] source, RenderFrameSubmission snapshot, LogOptions log){public RenderOrigin Initial{get;}=initial;public RenderOrigin Camera=initial;public SampleRenderableState[] Source{get;}=source;public RenderFrameSubmission Snapshot{get;}=snapshot;public LogOptions Log{get;}=log;}
file readonly record struct SampleOptions(int ObjectCount,string[] LogArguments){public static bool TryParse(string[] args,out SampleOptions value,out string? error){var count=100;var logs=new List<string>();foreach(var arg in args){if(arg.StartsWith("--objects=")){if(!int.TryParse(arg[10..],out count)||count is not(1 or 100 or 1000 or 10000)){value=default;error="Usage: --objects=1|100|1000|10000";return false;}}else logs.Add(arg);}value=new(count,logs.ToArray());error=null;return true;}}
internal static partial class NativeKeyboard { [LibraryImport("user32.dll",EntryPoint="GetAsyncKeyState")] private static partial short State(int key); public static bool IsDown(int key)=>(State(key)&0x8000)!=0; }
