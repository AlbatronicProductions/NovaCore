using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;

var frame = new ReferenceFrameId(1);
var camera = new RenderOrigin(new UniversePosition(new Double3(4_000_000_000d, 0d, 0d), frame));
var objectPosition = new UniversePosition(new Double3(4_000_000_000.25d, 0d, 0d), frame);
var relative = ReferenceFrame.Resolve(objectPosition, camera);
Console.WriteLine($"Camera render position: (0, 0, 0); triangle relative X: {relative.Value.X:F2}");

NativeRuntime.DiagnosticCallback callback = (message, _) => Console.WriteLine($"[native] {Marshal.PtrToStringUTF8(message)}");
var result = NativeRuntime.RunTriangle(RenderSubmission.ForNative(relative), callback, IntPtr.Zero);
return result == NativeResult.Success ? 0 : (int)result;
