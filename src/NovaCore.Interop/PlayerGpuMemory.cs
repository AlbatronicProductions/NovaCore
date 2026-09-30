using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace NovaCore.Interop;

public enum PlayerGpuMemoryStatus : uint { Inactive, Ready, Unsupported, Failed, Stopped, Starting, AdmissionDenied }

public sealed record PlayerGpuMemoryReading(PlayerGpuMemoryStatus Status, ulong Sequence, long Timestamp, long Frequency,
    DateTime SampleUtc, ulong QueryNanoseconds, ulong UsageBytes, ulong BudgetBytes, ulong CapacityBytes,
    string Adapter, string Uuid, string Luid, bool LuidValid, uint Vendor, uint Device, uint Driver, uint HeapMask, string Detail)
{
    public double AgeSeconds => Frequency<=0?double.PositiveInfinity:Math.Max(0,(Stopwatch.GetTimestamp()-Timestamp)/(double)Frequency);
    public bool IsLive => Status==PlayerGpuMemoryStatus.Ready&&AgeSeconds<=3;
    public string DisplayText {
        get {
            var name=string.IsNullOrEmpty(Adapter)?"Selected rendering adapter":Adapter;
            if(Status==PlayerGpuMemoryStatus.Ready){
                if(!IsLive)return $"{name}\nGPU memory reading is stale — refreshing…\nCurrent NovaCore usage and budget unavailable";
                return $"{name} · device-local capacity {CapacityBytes/1073741824.0:F1} GiB\nNovaCore usage ≈ {UsageBytes/1073741824.0:F2} GiB / application budget ≈ {BudgetBytes/1073741824.0:F2} GiB\nDriver estimates · updated live";
            }
            return $"{name}\nGPU memory: {(Status==PlayerGpuMemoryStatus.Starting?"reading driver…":"unavailable")}\n{Detail}";
        }
    }
}

// One native owner for the whole player lifetime. Read copies cached data only.
public static unsafe partial class PlayerGpuMemory
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Snapshot {
        public uint Size,Version,Status,HeapCount;
        public ulong Sequence,Qpc,Frequency,UtcFileTime,QueryNs,Usage,Budget,Capacity;
        public uint Vendor,Device,Driver,HeapMask;
        public fixed byte Uuid[16],Luid[8];
        public uint LuidValid,Reserved;
        public fixed byte Adapter[256],Detail[160];
        public fixed ulong HeapUsage[16],HeapBudget[16],HeapCapacity[16];
        public fixed uint HeapFlags[16];
    }
    public static void Begin(nint window){if(BeginNative((ulong)window)!=NativeResult.Success)throw new InvalidOperationException("GPU memory owner initialization refused.");}
    public static void End(){if(EndNative()!=NativeResult.Success)throw new InvalidOperationException("GPU memory owner cannot close before renderer cleanup.");}
    public static void Poll(){if(PollNative()!=NativeResult.Success)throw new InvalidOperationException("GPU memory polling owner refused.");}
    public static PlayerGpuMemoryReading Read(){
        Snapshot s=new(){Size=(uint)sizeof(Snapshot),Version=1};
        if(ReadNative(&s)!=NativeResult.Success)throw new InvalidOperationException("GPU memory snapshot contract failed.");
        return new((PlayerGpuMemoryStatus)s.Status,s.Sequence,(long)s.Qpc,(long)s.Frequency,
            s.UtcFileTime==0?DateTime.MinValue:DateTime.FromFileTimeUtc((long)s.UtcFileTime),s.QueryNs,s.Usage,s.Budget,s.Capacity,
            Text(s.Adapter,256),Convert.ToHexString(new ReadOnlySpan<byte>(s.Uuid,16)),Convert.ToHexString(new ReadOnlySpan<byte>(s.Luid,8)),
            s.LuidValid!=0,s.Vendor,s.Device,s.Driver,s.HeapMask,Text(s.Detail,160));
    }
    private static string Text(byte* p,int limit){var s=new ReadOnlySpan<byte>(p,limit);var n=s.IndexOf((byte)0);return Encoding.UTF8.GetString(s[..(n<0?limit:n)]);}
    [LibraryImport("NovaCore.Native",EntryPoint="nc_player_gpu_memory_begin")]private static partial NativeResult BeginNative(ulong window);
    [LibraryImport("NovaCore.Native",EntryPoint="nc_player_gpu_memory_read")]private static partial NativeResult ReadNative(Snapshot* snapshot);
    [LibraryImport("NovaCore.Native",EntryPoint="nc_player_gpu_memory_end")]private static partial NativeResult EndNative();
    [LibraryImport("NovaCore.Native",EntryPoint="nc_player_gpu_memory_poll")]private static partial NativeResult PollNative();
}
