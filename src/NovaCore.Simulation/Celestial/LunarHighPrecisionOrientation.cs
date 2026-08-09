using System.Buffers.Binary;
using System.Reflection;
using NovaCore.Core;

namespace NovaCore.Simulation.Celestial;

/// <summary>Kernel-free daily residual pack extracted offline from the official DE440 lunar PCK/frame chain.</summary>
internal static class LunarHighPrecisionOrientation
{
    internal const string FrameName="MOON_ME_DE440_ME421";
    internal const string Authority="NAIF/JPL DE440 moon_pa_de440_200625.bpc + moon_de440_250416.tf";
    internal const int HeaderBytes=128,RecordBytes=12;
    internal const uint Version=1;
    private static ReadOnlySpan<byte> Magic=>"NCLODE44"u8;
    private static ReadOnlySpan<byte> ExpectedPckHash=>[0x60,0xCD,0x55,0xAA,0x40,0x1E,0xA2,0xEA,0x97,0x36,0x06,0x36,0xF5,0x67,0x55,0x4B,0xFE,0x4E,0x37,0xBB,0x82,0x9F,0x90,0x1B,0x44,0x60,0xA4,0x55,0xDF,0xAF,0x78,0x3F];
    private static ReadOnlySpan<byte> ExpectedFrameHash=>[0xA4,0x7C,0x71,0xE9,0xC9,0xF3,0x37,0x96,0xBD,0xAF,0xB2,0xC9,0xD6,0x9A,0x7E,0xE4,0x47,0xB6,0x01,0x6E,0xCA,0xD8,0x0F,0x71,0xCD,0x6F,0x3E,0x47,0x9F,0x9C,0xF7,0x68];
    private static readonly byte[] Data=Load();
    private static readonly PackHeader Header=Parse(Data);

    internal static bool IsAvailable=>Header.IsValid;
    internal static long CoverageStartTicks=>Header.StartTicks;
    internal static long CoverageEndTicks=>Header.EndTicks;
    internal static ulong DeterministicHash=>Header.ContentHash;

    internal static bool TryEvaluate(double secondsSinceJ2000,in DoubleQuaternion fallback,out DoubleQuaternion value)
    {
        value=default;
        if(!Header.IsValid||!double.IsFinite(secondsSinceJ2000)||!fallback.IsFinite)return false;
        var position=(secondsSinceJ2000-Header.StartTicks/1_000_000d)/(Header.StepTicks/1_000_000d);
        if(position<0d||position>Header.Count-1d)return false;
        var index=(int)Math.Floor(position);var fraction=position-index;
        if(index>=Header.Count-1){index=Header.Count-2;fraction=1d;}
        var p0=Read(Math.Max(0,index-1));var p1=Read(index);var p2=Read(index+1);var p3=Read(Math.Min(Header.Count-1,index+2));
        var t2=fraction*fraction;var t3=t2*fraction;
        var residual=(p1*2d+(p2-p0)*fraction+(p0*2d-p1*5d+p2*4d-p3)*t2+(-p0+p1*3d-p2*3d+p3)*t3)*.5d;
        var angle=Math.Sqrt(residual.LengthSquared);
        var correction=angle<=1e-18d?DoubleQuaternion.Identity:DoubleQuaternion.FromAxisAngle(residual/angle,angle);
        value=(fallback*correction).Normalized();
        if(value.W<0d)value=new(-value.X,-value.Y,-value.Z,-value.W);
        return value.IsFinite;
    }

    internal static bool Validate(ReadOnlySpan<byte> data)=>Parse(data).IsValid;

    private static Double3 Read(int index)
    {
        var offset=HeaderBytes+index*RecordBytes;
        return new(BinaryPrimitives.ReadInt32LittleEndian(Data.AsSpan(offset,4))*Header.Scale,BinaryPrimitives.ReadInt32LittleEndian(Data.AsSpan(offset+4,4))*Header.Scale,BinaryPrimitives.ReadInt32LittleEndian(Data.AsSpan(offset+8,4))*Header.Scale);
    }

    private static byte[] Load()
    {
        try
        {
            using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("NovaCore.Simulation.Celestial.Data.moon_me_de440_1900_2100.nclo");
            if(stream is null||stream.Length>int.MaxValue)return [];
            var data=new byte[(int)stream.Length];stream.ReadExactly(data);return data;
        }
        catch{return [];}
    }

    private static PackHeader Parse(ReadOnlySpan<byte> data)
    {
        if(data.Length<HeaderBytes||!data[..8].SequenceEqual(Magic))return default;
        var version=BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);var headerBytes=BinaryPrimitives.ReadInt32LittleEndian(data[12..]);var start=BinaryPrimitives.ReadInt64LittleEndian(data[16..]);var step=BinaryPrimitives.ReadInt64LittleEndian(data[24..]);var count=BinaryPrimitives.ReadInt32LittleEndian(data[32..]);var recordBytes=BinaryPrimitives.ReadInt32LittleEndian(data[36..]);var scale=BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data[40..]));var end=BinaryPrimitives.ReadInt64LittleEndian(data[48..]);var hash=BinaryPrimitives.ReadUInt64LittleEndian(data[56..]);
        if(version!=Version||headerBytes!=HeaderBytes||recordBytes!=RecordBytes||step<=0||count<4||!double.IsFinite(scale)||scale<=0d||end!=start+(count-1L)*step||data.Length!=HeaderBytes+(long)count*RecordBytes||!data.Slice(64,32).SequenceEqual(ExpectedPckHash)||!data.Slice(96,32).SequenceEqual(ExpectedFrameHash))return default;
        ulong actual=14695981039346656037UL;for(var index=HeaderBytes;index<data.Length;index++){actual^=data[index];actual*=1099511628211UL;}if(actual!=hash)return default;
        return new(true,start,end,step,count,scale,hash);
    }

    private readonly record struct PackHeader(bool IsValid,long StartTicks,long EndTicks,long StepTicks,int Count,double Scale,ulong ContentHash);
}
