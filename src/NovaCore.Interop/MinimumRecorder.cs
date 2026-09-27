using System.Runtime.InteropServices;
namespace NovaCore.Interop;
public static class MinimumRecorder
{
    [StructLayout(LayoutKind.Sequential)] public struct CostSample {public ulong Loop,Ticks,Cycles,Events,AllocationCalls,AllocationBytes;}
    [StructLayout(LayoutKind.Sequential)] public struct CostHeader {public ulong Size,Version,Count,Frequency,RetainedBytes,OwnedCalls,OwnedBytes,Overflow;}
    public static void StartMeasurement(){if(MeasureNative(1)!=0)throw new InvalidOperationException("Minimum cost measurement unavailable.");}
    public static void StopMeasurement(){if(MeasureNative(0)!=0)throw new InvalidOperationException("Minimum cost measurement unavailable.");}
    public static void RouteMarker(ulong body,double distance,double radius,ulong epoch){if(RouteNative(body,distance,radius,epoch)!=0)throw new InvalidOperationException("Minimum route marker refused.");}
    public static unsafe (CostHeader Header,CostSample[] Samples) ReadMeasurement()
    {
        var samples=new CostSample[32768];CostHeader h;
        fixed(CostSample* p=samples)if(ReadNative(&h,p,(uint)samples.Length)!=0)throw new InvalidOperationException("Minimum cost read refused.");
        if(h.Size!=64||h.Version!=1||h.Count==0||h.Count>(ulong)samples.Length||h.Frequency==0||h.Overflow!=0)throw new InvalidDataException("Minimum cost evidence incomplete or overflowed.");
        Array.Resize(ref samples,(int)h.Count);return(h,samples);
    }
    public static void Configure(string name,Guid session)
    {
        byte[] b=session.ToByteArray();
        if(ConfigureNative(name,BitConverter.ToUInt64(b,0),BitConverter.ToUInt64(b,8))!=0)
            throw new InvalidOperationException("Mandatory minimum native recorder initialization failed. Unrecorded session refused before GPU initialization.");
    }
    public static void Close()=>CloseNative();
    [DllImport("NovaCore.Native",EntryPoint="nc_configure_minimum_recorder",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]
    private static extern int ConfigureNative(string mapping,ulong sessionLow,ulong sessionHigh);
    [DllImport("NovaCore.Native",EntryPoint="nc_close_minimum_recorder",CallingConvention=CallingConvention.Cdecl)]
    private static extern void CloseNative();
    [DllImport("NovaCore.Native",EntryPoint="nc_minimum_measurement",CallingConvention=CallingConvention.Cdecl)] private static extern int MeasureNative(int action);
    [DllImport("NovaCore.Native",EntryPoint="nc_minimum_route_marker",CallingConvention=CallingConvention.Cdecl)] private static extern int RouteNative(ulong body,double distance,double radius,ulong epoch);
    [DllImport("NovaCore.Native",EntryPoint="nc_read_minimum_measurement",CallingConvention=CallingConvention.Cdecl)] private static extern unsafe int ReadNative(CostHeader* header,CostSample* samples,uint capacity);
}
