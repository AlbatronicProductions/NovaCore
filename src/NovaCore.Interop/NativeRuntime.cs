using System.Runtime.InteropServices;

namespace NovaCore.Interop;

public enum NativeResult : int { Success = 0, Failure = 1, InvalidArgument = 2 }

[StructLayout(LayoutKind.Sequential)]
public struct NativeEncodedPosition
{
    public float HighX, HighY, HighZ, HighPadding;
    public float LowX, LowY, LowZ, LowPadding;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeRenderObject { public NativeEncodedPosition Position; public uint Mesh; public uint Padding0, Padding1, Padding2; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFrameSubmission { public NativeEncodedPosition Camera; public NativeRenderObject* Objects; public uint ObjectCount; public uint Padding; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeInputState { public float DeltaSeconds; public uint MoveLeft, MoveRight, MoveForward, MoveBackward; }

public enum NativeHostEventType : uint { Diagnostic = 1, UpdateFrame = 2 }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeHostEvent { public NativeHostEventType Type; public uint LogCategory; public byte* Utf8Message; public NativeInputState Input; public NativeFrameSubmission* Submission; }

public static partial class NativeRuntime
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void HostCallback(NativeHostEvent* hostEvent, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_renderer")]
    public static unsafe partial NativeResult RunRenderer(NativeFrameSubmission* submission, HostCallback callback, IntPtr userData);
}
