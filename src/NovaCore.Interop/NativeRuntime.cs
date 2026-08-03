using System.Runtime.InteropServices;

namespace NovaCore.Interop;

public enum NativeResult : int { Success = 0, Failure = 1, InvalidArgument = 2 }

[StructLayout(LayoutKind.Sequential)]
public struct NativeRelativePosition { public double X, Y, Z; }

public static partial class NativeRuntime
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DiagnosticCallback(IntPtr utf8Message, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_triangle")]
    public static partial NativeResult RunTriangle(NativeRelativePosition position, DiagnosticCallback callback, IntPtr userData);
}
