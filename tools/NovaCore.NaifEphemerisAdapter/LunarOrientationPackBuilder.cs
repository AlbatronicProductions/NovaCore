using System.Buffers.Binary;
using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Simulation.Celestial;

namespace NovaCore.NaifEphemerisAdapter;

internal static class LunarOrientationPackBuilder
{
    private const long DayTicks=86_400_000_000L,StepTicks=21_600_000_000L,StartTicks=-36_525L*DayTicks,EndTicks=36_525L*DayTicks;
    private const double Scale=1e-11d;
    private const string Frame="MOON_ME_DE440_ME421";
    private static readonly byte[] PckHash=Convert.FromHexString("60CD55AA401EA2EA97360636F567554BFE4E37BB829F901B4460A455DFAF783F");
    private static readonly byte[] FrameHash=Convert.FromHexString("A47C71E9C9F33796BDAFB2C9D69A7EE447B6016ECAD80F71CD6F3E479F9CF768");

    internal static bool TryBuild(string root,string destination,out string report,out string error)
    {
        report="";error="";
        if(!OfficialNaifBundle.VerifyRepositoryRoot(root)){error="official NAIF bundle verification failed";return false;}
        var shim=Path.Combine(root,"external","naif","build","cspice-shim","NovaCore.CSpiceShim.dll");
        var kernels=new[]{"pck00010.tpc","moon_pa_de440_200625.bpc","moon_de440_250416.tf","naif0012.tls"}.Select(name=>Path.Combine(root,"external","naif","kernels",name)).ToArray();
        if(!CspiceSession.TryCreate(shim,out var session,out var diagnostic)||session is null){error=$"shim load failed: {diagnostic.LongMessage}";return false;}
        using(session)
        {
            if(!session.TryLoadKernels(kernels)){error="lunar kernel load failed";return false;}
            var count=checked((int)((EndTicks-StartTicks)/StepTicks+1));var bytes=new byte[LunarHighPrecisionOrientation.HeaderBytes+count*LunarHighPrecisionOrientation.RecordBytes];
            "NCLODE44"u8.CopyTo(bytes);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),LunarHighPrecisionOrientation.Version);BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12),LunarHighPrecisionOrientation.HeaderBytes);BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16),StartTicks);BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(24),StepTicks);BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32),count);BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(36),LunarHighPrecisionOrientation.RecordBytes);BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(40),BitConverter.DoubleToInt64Bits(Scale));BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(48),EndTicks);PckHash.CopyTo(bytes,64);FrameHash.CopyTo(bytes,96);
            var maximumResidual=0d;var watch=Stopwatch.StartNew();
            for(var index=0;index<count;index++)
            {
                var et=(StartTicks+index*StepTicks)/1_000_000d;
                if(!session.TryQueryFrame("J2000",Frame,et,out var matrix,out diagnostic)){error=$"{Frame} query failed at ET {et:R}: {diagnostic.ShortMessage}";return false;}
                var high=ToNovaQuaternion(matrix);var fallback=CelestialBodyOrientationEvaluator.EvaluateMoonFallbackForTest(et);var residual=(fallback.Conjugate().Normalized()*high).Normalized();if(residual.W<0d)residual=new(-residual.X,-residual.Y,-residual.Z,-residual.W);var vector=Log(residual);maximumResidual=Math.Max(maximumResidual,Math.Sqrt(vector.LengthSquared));
                var offset=LunarHighPrecisionOrientation.HeaderBytes+index*LunarHighPrecisionOrientation.RecordBytes;BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset),checked((int)Math.Round(vector.X/Scale,MidpointRounding.ToEven)));BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset+4),checked((int)Math.Round(vector.Y/Scale,MidpointRounding.ToEven)));BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset+8),checked((int)Math.Round(vector.Z/Scale,MidpointRounding.ToEven)));
            }
            ulong hash=14695981039346656037UL;for(var index=LunarHighPrecisionOrientation.HeaderBytes;index<bytes.Length;index++){hash^=bytes[index];hash*=1099511628211UL;}BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56),hash);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)??root);File.WriteAllBytes(destination,bytes);watch.Stop();
            report=$"lunar-orientation-pack,frame={Frame},start_ticks={StartTicks},end_ticks={EndTicks},cadence_seconds={StepTicks/1_000_000},samples={count},bytes={bytes.Length},scale_rad={Scale:R},maximum_residual_rad={maximumResidual:R},hash=0x{hash:X16},build_ms={watch.Elapsed.TotalMilliseconds:R}";return true;
        }
    }

    internal static DoubleQuaternion ToNovaQuaternion(in CspiceFrameTransform m)
    {
        var r00=m.M00;var r01=m.M20;var r02=-m.M10;var r10=m.M01;var r11=m.M21;var r12=-m.M11;var r20=m.M02;var r21=m.M22;var r22=-m.M12;DoubleQuaternion q;
        var trace=r00+r11+r22;
        if(trace>0d){var s=Math.Sqrt(trace+1d)*2d;q=new((r21-r12)/s,(r02-r20)/s,(r10-r01)/s,.25d*s);}
        else if(r00>r11&&r00>r22){var s=Math.Sqrt(1d+r00-r11-r22)*2d;q=new(.25d*s,(r01+r10)/s,(r02+r20)/s,(r21-r12)/s);}
        else if(r11>r22){var s=Math.Sqrt(1d+r11-r00-r22)*2d;q=new((r01+r10)/s,.25d*s,(r12+r21)/s,(r02-r20)/s);}
        else{var s=Math.Sqrt(1d+r22-r00-r11)*2d;q=new((r02+r20)/s,(r12+r21)/s,.25d*s,(r10-r01)/s);}
        q=q.Normalized();return q.W<0d?new(-q.X,-q.Y,-q.Z,-q.W):q;
    }

    private static Double3 Log(in DoubleQuaternion value){var length=Math.Sqrt(value.X*value.X+value.Y*value.Y+value.Z*value.Z);if(length<=1e-18d)return Double3.Zero;var angle=2d*Math.Atan2(length,value.W);return new Double3(value.X,value.Y,value.Z)*(angle/length);}
}
