using System.Runtime.InteropServices;

namespace NovaCore.NaifEphemerisAdapter;

internal enum CspiceSessionStatus : byte { Success, NativeLibraryLoadFailure, MissingNativeExport, ToolkitIdentityFailure, KernelLoadFailure, QueryFailure, KernelClearFailure, NativeErrorStateFailure, InvalidArgument, AlreadyDisposed, NotReady }
internal readonly record struct CspiceDiagnostic(CspiceSessionStatus Status,string Operation,string ShortMessage,string LongMessage);
internal readonly record struct CspiceSourceState(double X,double Y,double Z,double Vx,double Vy,double Vz);
internal sealed class CspiceSession : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int LoadDelegate(IntPtr path);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ClearDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int QueryDelegate(int target,double et,out CspiceStateKm state);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int TextDelegate(IntPtr buffer,int capacity);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ErrorTextDelegate(int longMessage,IntPtr buffer,int capacity);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int FailedDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ResetDelegate();
    IntPtr _library; readonly LoadDelegate _load; readonly ClearDelegate _clear; readonly QueryDelegate _query; readonly TextDelegate _version; readonly ErrorTextDelegate _error; readonly FailedDelegate _failed; readonly ResetDelegate _reset; bool _ready,_disposed;
    CspiceSession(IntPtr library){_library=library;_load=Bind<LoadDelegate>("NcspLoadKernel");_clear=Bind<ClearDelegate>("NcspClearKernels");_query=Bind<QueryDelegate>("NcspQueryGeometricState");_version=Bind<TextDelegate>("NcspGetToolkitVersion");_error=Bind<ErrorTextDelegate>("NcspGetError");_failed=Bind<FailedDelegate>("NcspHasFailure");_reset=Bind<ResetDelegate>("NcspResetError");}
    T Bind<T>(string name) where T:Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library,name));
    internal static bool TryCreate(string shimPath,out CspiceSession? session,out CspiceDiagnostic diagnostic){session=null;if(string.IsNullOrWhiteSpace(shimPath)||!Path.IsPathFullyQualified(shimPath)||!File.Exists(shimPath)){diagnostic=new(CspiceSessionStatus.NativeLibraryLoadFailure,"load","","explicit shim path missing");return false;}try{session=new(NativeLibrary.Load(shimPath));diagnostic=default;return true;}catch(Exception e){diagnostic=new(CspiceSessionStatus.NativeLibraryLoadFailure,"load","",e.Message);return false;}}
    internal bool TryGetVersion(out string value){value="";if(_disposed)return false;var p=Marshal.AllocHGlobal(256);try{if(_version(p,256)==0)return false;value=Marshal.PtrToStringUTF8(p)??"";return value.Length>0;}finally{Marshal.FreeHGlobal(p);}}
    internal bool TryLoadKernels(IReadOnlyList<string> paths){if(_disposed||paths.Count==0)return false;foreach(var path in paths){if(!File.Exists(path))return false;var p=Marshal.StringToCoTaskMemUTF8(path);try{if(_load(p)==0){_clear();return false;}}finally{Marshal.FreeCoTaskMem(p);}}return _ready=true;}
    internal bool TryQuery(int target,double et,out CspiceSourceState state,out CspiceDiagnostic diagnostic){state=default;diagnostic=default;if(_disposed||!_ready||!double.IsFinite(et)){diagnostic=new(_disposed?CspiceSessionStatus.AlreadyDisposed:CspiceSessionStatus.NotReady,"query","","not ready or invalid ET");return false;}if(_query(target,et,out var s)==0||_failed()!=0){diagnostic=CaptureFailure("query");return false;}state=new(s.X,s.Y,s.Z,s.Vx,s.Vy,s.Vz);return true;}
    internal bool TryQuery(int target,double et,out CspiceSourceState state)=>TryQuery(target,et,out state,out _);
    CspiceDiagnostic CaptureFailure(string operation){string Get(int kind){var p=Marshal.AllocHGlobal(2048);try{if(_error(kind,p,2048)==0)return "";return Marshal.PtrToStringUTF8(p)??"";}finally{Marshal.FreeHGlobal(p);}} var shortText=Get(0);var longText=Get(1);_reset();return new(CspiceSessionStatus.QueryFailure,operation,shortText,longText);}
    internal bool Clear(){if(_disposed)return false;_ready=false;return _clear()!=0;}
    public void Dispose(){if(_disposed)return;Clear();NativeLibrary.Free(_library);_library=IntPtr.Zero;_disposed=true;GC.SuppressFinalize(this);}
}
