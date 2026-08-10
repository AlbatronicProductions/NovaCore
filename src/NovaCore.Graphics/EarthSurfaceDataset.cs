using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Immutable metadata for the checked Earth presentation dataset.</summary>
public static class EarthSurfaceDatasetContract
{
    public const string Schema = "NovaCore.EarthVirtualTexture/3";
    public const string IdentitySha256 = "664ff32c3a57043960f246d5d97397214cedc4b976e48e867e9803c414d796b5";
    public const string PayloadSha256 = "d09a9ddf944242a7d322ae3ce58c1b0b31014feb8d6a330fb9d592e438e9d306";
    public const string RuntimePackSha256 = "a16aebd834f01bdd430790de499a095d55f895655ce037fe25b6e13106674dc5";
    public const string ManifestSha256 = "868769b2499bab96b32c3f5c5ea6b444db5c747294dd0e1e497057bf4e85e19b";
    public const string ElevationSha256 = "4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317";
    public const string ElevationPackSectionSha256 = "e16390be4dc29f4e6d9e1f6c05da4defbdc137c4fbde04f7be1c91fd9167d1a0";
    public const int TileSize = 256;
    public const int TileGutter = 2;
    public const int PhysicalTileExtent = 260;
    public const int MaximumLevel = 4;
    public const int TileCount = 682;
    public const int ElevationWidth = 8192;
    public const int ElevationHeight = 4096;
    public const int HeaderBytes = 256;
    public const int ChannelCount = 4;
    public const int AlbedoTileBytes = 67_600;
    public const int ElevationTileBytes = 135_200;
    public const int LandMaskTileBytes = 33_800;
    public const int CloudTileBytes = 33_800;
    public const int CloudMaximumLevel = 2;
    public const int CloudTileCount = 42;
    public const int UploadBudgetChannels = 4;
    public const int StagingBudgetBytes = 1_081_600;
    public const long RuntimePackBytes = 162_781_056;
    public const double MinimumElevationMetres = -11_000d;
    public const double MaximumElevationMetres = 9_000d;
    public const long PhysicalPoolBudgetBytes = 34_611_200;
}

/// <summary>Immutable contract for the optional bounded Mount St. Helens regional proof pack.</summary>
public static class EarthRegionalDatasetContract
{
    public const string Schema="NovaCore.PlanetaryRegionalPack/1";
    public const string FileName="mount_st_helens_v1.ncvreg";
    public const string PackSha256="9f66aa63963ce503fc871eed03d5626b42548dec24c8222221f8c273b6c27b00";
    public const string IdentitySha256="c06d20fc3ece50b518f94e3f9e19c584c709823b4620c8dbff6992f357bfe7d7";
    public const string PayloadSha256="40049ffdc1e969876ea70e7f41e21aa66e52dd2551cf4c23aa6203185f765037";
    public const int MinimumLevel=5,MaximumLevel=12,PageCount=48,HashCapacity=512,PackBytes=11_359_360;
    public const double WestDegrees=-122.3,SouthDegrees=46.1,EastDegrees=-122.1,NorthDegrees=46.3;
}

public enum PlanetaryTextureSemantic:uint { Albedo=1,Elevation=2,LandMask=3,Cloud=4,Normal=5,Roughness=6 }
public enum PlanetaryGpuTextureFormat:uint { Bc1RgbSrgb=1,R16Unorm=2,Bc4Unorm=3,Bc7Srgb=4,Bc5Unorm=5 }
public enum PlanetaryTextureColorSpace:uint { Linear,Srgb }
public readonly record struct PlanetaryTextureChannelPolicy(PlanetaryTextureSemantic Semantic,PlanetaryGpuTextureFormat Format,PlanetaryTextureColorSpace ColorSpace,int MaximumUsefulLevel,bool LosslessAuthorityRequired);

/// <summary>Explicit GPU format and color-space policy; it is presentation metadata, never geographic authority.</summary>
public static class PlanetaryTextureFormatPolicy
{
    private static readonly PlanetaryTextureChannelPolicy[] EarthChannels =
    [
        new(PlanetaryTextureSemantic.Albedo,PlanetaryGpuTextureFormat.Bc7Srgb,PlanetaryTextureColorSpace.Srgb,4,false),
        new(PlanetaryTextureSemantic.Elevation,PlanetaryGpuTextureFormat.R16Unorm,PlanetaryTextureColorSpace.Linear,4,true),
        new(PlanetaryTextureSemantic.LandMask,PlanetaryGpuTextureFormat.Bc4Unorm,PlanetaryTextureColorSpace.Linear,4,false),
        new(PlanetaryTextureSemantic.Cloud,PlanetaryGpuTextureFormat.Bc4Unorm,PlanetaryTextureColorSpace.Linear,2,false)
    ];
    public static ReadOnlySpan<PlanetaryTextureChannelPolicy> Earth => EarthChannels;
    public static PlanetaryTextureChannelPolicy FutureNormal => new(PlanetaryTextureSemantic.Normal,PlanetaryGpuTextureFormat.Bc5Unorm,PlanetaryTextureColorSpace.Linear,4,false);
}

/// <summary>Bounded view-demand policy shared conceptually with the native SVT streamer.</summary>
public static class EarthSurfaceDemandPolicy
{
    public const int FinestNeighborhoodRadiusTiles=2;
    public static int RequestedLevel(double surfaceAltitudeMetres)
    {
        if(!double.IsFinite(surfaceAltitudeMetres)||surfaceAltitudeMetres<0d)throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));
        return surfaceAltitudeMetres>1_000_000d?1:surfaceAltitudeMetres>100_000d?2:surfaceAltitudeMetres>10_000d?3:EarthSurfaceDatasetContract.MaximumLevel;
    }
    public static int RequestedLevel(PlanetaryTextureSemantic semantic,double surfaceAltitudeMetres)
    {
        var terrain=RequestedLevel(surfaceAltitudeMetres);
        var maximum=semantic==PlanetaryTextureSemantic.Cloud?EarthSurfaceDatasetContract.CloudMaximumLevel:EarthSurfaceDatasetContract.MaximumLevel;
        return Math.Min(terrain,maximum);
    }
    public static double EquatorialMetresPerTexel(double bodyRadiusMetres,int level)
    {
        if(!double.IsFinite(bodyRadiusMetres)||bodyRadiusMetres<=0d||level is <0 or >EarthSurfaceDatasetContract.MaximumLevel)throw new ArgumentOutOfRangeException();
        return Math.Tau*bodyRadiusMetres/(EarthSurfaceDatasetContract.TileSize*(1<<(level+1)));
    }
}

/// <summary>Deterministic logical page hierarchy shared with the native Earth page table.</summary>
public static class EarthVirtualTexturePageContract
{
    public static int BodyFixedPageIdentity(in Double3 bodyFixedDirection,int level)
    {
        level=Math.Clamp(level,0,EarthSurfaceDatasetContract.MaximumLevel);var (x,y)=BodyFixedPageCoordinates(bodyFixedDirection,level,EarthSurfaceDatasetContract.MaximumLevel);return TileIndex(level,x,y);
    }

    public static (int X,int Y) BodyFixedPageCoordinates(in Double3 bodyFixedDirection,int level,int maximumLevel)
    {
        if(!bodyFixedDirection.IsFinite||bodyFixedDirection.LengthSquared<=0d||maximumLevel is <0 or >24)throw new ArgumentOutOfRangeException();
        var direction=bodyFixedDirection.Normalized();var u=Math.IEEERemainder(Math.Atan2(direction.Z,direction.X)/Math.Tau+.5d,1d);if(u<0d)u+=1d;var v=Math.Acos(Math.Clamp(direction.Y,-1d,1d))/Math.PI;
        level=Math.Clamp(level,0,maximumLevel);var tilesX=1<<(level+1);var tilesY=1<<level;return(Math.Min((int)Math.Floor(u*tilesX),tilesX-1),Math.Min((int)Math.Floor(v*tilesY),tilesY-1));
    }

    public static int LevelOffset(int level)
    {
        if (level is < 0 or > EarthSurfaceDatasetContract.MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
        var offset=0;var count=2;for(var current=0;current<level;current++){offset+=count;count*=4;}return offset;
    }

    public static int TileIndex(int level,int x,int y)
    {
        if(level is < 0 or > EarthSurfaceDatasetContract.MaximumLevel)throw new ArgumentOutOfRangeException(nameof(level));
        var tilesX=1<<(level+1);var tilesY=1<<level;
        if(x<0||x>=tilesX||y<0||y>=tilesY)throw new ArgumentOutOfRangeException();
        return LevelOffset(level)+y*tilesX+x;
    }

    public static int ParentIndex(int level,int x,int y) => level==0?TileIndex(0,x,y):TileIndex(level-1,x/2,y/2);

    public static int ResolveResidentPage(double u,double v,int requestedLevel,ReadOnlySpan<bool> resident,out int residentLevel)
    {
        if(!double.IsFinite(u)||!double.IsFinite(v)||requestedLevel<0)throw new ArgumentOutOfRangeException();
        u-=Math.Floor(u);v=Math.Clamp(v,0d,1d);requestedLevel=Math.Min(requestedLevel,EarthSurfaceDatasetContract.MaximumLevel);
        for(var level=requestedLevel;level>=0;level--){var tilesX=1<<(level+1);var tilesY=1<<level;var x=Math.Min((int)Math.Floor(u*tilesX),tilesX-1);var y=Math.Min((int)Math.Floor(v*tilesY),tilesY-1);var page=TileIndex(level,x,y);if(page<resident.Length&&resident[page]){residentLevel=level;return page;}}
        residentLevel=0;return TileIndex(0,u>=.5d?1:0,0);
    }

    public static double PromotionBlend(uint readyFrame,uint currentFrame)
    {
        var t=Math.Clamp((currentFrame>readyFrame?currentFrame-readyFrame:0u)/30d,0d,1d);
        return t*t*(3d-2d*t);
    }
}

public enum EarthVirtualTextureDebugMode : uint
{
    None, MipLevel, PhysicalPage, Residency, FallbackDepth, RequestedVsResident,
    ParentChildBlend, ImageryOnly, ElevationOnly, OceanMask, CloudLayer, AtmosphereContribution
}

public static class EarthVirtualTextureDebug
{
    public static EarthVirtualTextureDebugMode FromEnvironment()
    {
        var value = Environment.GetEnvironmentVariable("NOVACORE_EARTH_DEBUG");
        return Enum.TryParse<EarthVirtualTextureDebugMode>(value, true, out var mode) ? mode : EarthVirtualTextureDebugMode.None;
    }
}

/// <summary>
/// Renderer-facing, presentation-only CPU height oracle. The native renderer
/// streams the matching tiled data; this compact copy keeps the SurfaceLocal
/// floor and validation queries registered to the same ETOPO source.
/// </summary>
public static class EarthSurfaceDataset
{
    private static readonly object Gate = new();
    private static ushort[]? _elevation;
    private static EarthRegionalElevation? _regional;

    public static bool IsLoaded => Volatile.Read(ref _elevation) is not null;
    public static bool IsRegionalLoaded => Volatile.Read(ref _regional) is not null;

    public static bool TryLoad(string runtimeDirectory, out string error)
    {
        if (string.IsNullOrWhiteSpace(runtimeDirectory)) { error = "Earth runtime directory is empty."; return false; }
        if (IsLoaded) { error = string.Empty; return true; }
        var path = Path.Combine(runtimeDirectory, "earth_elevation_8192x4096.r16");
        if (!File.Exists(path)) { error = $"Earth elevation fallback: '{path}' is unavailable."; return false; }
        try
        {
            var bytes = File.ReadAllBytes(path);
            var expected = EarthSurfaceDatasetContract.ElevationWidth * EarthSurfaceDatasetContract.ElevationHeight * sizeof(ushort);
            if (bytes.Length != expected) { error = $"Earth elevation has {bytes.Length} bytes; expected {expected}."; return false; }
            var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(actual, EarthSurfaceDatasetContract.ElevationSha256, StringComparison.Ordinal))
            { error = $"Earth elevation checksum mismatch: {actual}."; return false; }
            var values = new ushort[expected / sizeof(ushort)];
            for (var index = 0; index < values.Length; index++)
                values[index] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(index * 2, 2));
            EarthRegionalElevation.TryLoad(Path.Combine(runtimeDirectory,"regions"),out var regional);
            lock (Gate) { _elevation ??= values; _regional ??= regional; }
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { error = $"Earth elevation fallback: {exception.Message}"; return false; }
    }

    public static double SampleHeight(in Double3 bodyDirection)
        => Math.Max(0d, SampleElevation(bodyDirection));

    /// <summary>Samples signed ETOPO surface elevation for validation and coastline classification.</summary>
    public static double SampleElevation(in Double3 bodyDirection)
    {
        if (!bodyDirection.IsFinite || bodyDirection.LengthSquared <= 0d) throw new ArgumentOutOfRangeException(nameof(bodyDirection));
        var values = Volatile.Read(ref _elevation);
        if (values is null) return SampleFallback(bodyDirection);
        var direction = bodyDirection.Normalized();
        var u = Math.Atan2(direction.Z, direction.X) / (Math.PI * 2d) + .5d;
        u -= Math.Floor(u);
        var v = Math.Acos(Math.Clamp(direction.Y, -1d, 1d)) / Math.PI;
        var regional=Volatile.Read(ref _regional);
        if(regional is not null&&regional.TrySample(u,v,out var regionalElevation))return regionalElevation;
        var px = u * EarthSurfaceDatasetContract.ElevationWidth - .5d;
        var py = v * EarthSurfaceDatasetContract.ElevationHeight - .5d;
        var x0 = (int)Math.Floor(px); var y0 = Math.Clamp((int)Math.Floor(py), 0, EarthSurfaceDatasetContract.ElevationHeight - 1);
        var x1 = Mod(x0 + 1, EarthSurfaceDatasetContract.ElevationWidth); x0 = Mod(x0, EarthSurfaceDatasetContract.ElevationWidth);
        var y1 = Math.Min(y0 + 1, EarthSurfaceDatasetContract.ElevationHeight - 1);
        var tx = px - Math.Floor(px); var ty = py - Math.Floor(py);
        var a = Decode(values[y0 * EarthSurfaceDatasetContract.ElevationWidth + x0]);
        var b = Decode(values[y0 * EarthSurfaceDatasetContract.ElevationWidth + x1]);
        var c = Decode(values[y1 * EarthSurfaceDatasetContract.ElevationWidth + x0]);
        var d = Decode(values[y1 * EarthSurfaceDatasetContract.ElevationWidth + x1]);
        return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
    }

    public static bool TryValidateRegionalPack(string path,out string error)
        => EarthRegionalElevation.TryRead(path,EarthRegionalDatasetContract.PackSha256,out _,out error);

    private static double Decode(ushort value) => EarthSurfaceDatasetContract.MinimumElevationMetres +
        value / 65535d * (EarthSurfaceDatasetContract.MaximumElevationMetres - EarthSurfaceDatasetContract.MinimumElevationMetres);
    private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double SampleFallback(in Double3 bodyDirection)
    {
        var direction = bodyDirection.Normalized();
        var continental=.46d*Math.Sin(Double3.Dot(direction,new(.8017837257372732,.2672612419124244,.5345224838248488))*3.1d+.7d)
            +.31d*Math.Sin(Double3.Dot(direction,new(-.4082482904638631,.8164965809277261,.4082482904638631))*5.3d-1.2d)
            +.23d*Math.Sin(Double3.Dot(direction,new(.1825741858350554,-.3651483716701107,.9128709291752769))*8.7d+.35d);
        return Math.Clamp(Math.Pow(Math.Max(0d,continental-.02d),2d)*5_200d,0d,EarthSurfaceDatasetContract.MaximumElevationMetres);
    }
}

internal sealed class EarthRegionalElevation
{
    private const int HeaderBytes=256,RecordBytes=16,Extent=260,TileSize=256,Gutter=2,ElevationTileBytes=135_200;
    private readonly RegionalPage[] _pages;
    private EarthRegionalElevation(RegionalPage[] pages)=>_pages=pages;
    private readonly record struct RegionalPage(int Level,int X,int Y,ushort[] Values);

    public static bool TryLoad(string directory,out EarthRegionalElevation? dataset)
    {
        dataset=null;
        try
        {
            var indexPath=Path.Combine(directory,"earth_regions.index");
            if(!File.Exists(indexPath))return false;
            var fields=File.ReadAllText(indexPath).Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(fields.Length!=2||fields[0]!=EarthRegionalDatasetContract.FileName||fields[1]!=EarthRegionalDatasetContract.PackSha256)return false;
            return TryRead(Path.Combine(directory,fields[0]),fields[1],out dataset,out _);
        }
        catch(IOException){return false;}
        catch(UnauthorizedAccessException){return false;}
    }

    public static bool TryRead(string path,string? expectedSha256,out EarthRegionalElevation? dataset,out string error)
    {
        dataset=null;
        try
        {
            if(!File.Exists(path)){error="regional pack missing";return false;}
            var bytes=File.ReadAllBytes(path);
            if(bytes.Length!=EarthRegionalDatasetContract.PackBytes){error="regional pack byte count";return false;}
            var actual=Convert.ToHexStringLower(SHA256.HashData(bytes));
            if(expectedSha256 is not null&&!string.Equals(actual,expectedSha256,StringComparison.Ordinal)){error="regional pack checksum";return false;}
            if(!bytes.AsSpan(0,8).SequenceEqual("NCREGN1\0"u8)||Read32(bytes,8)!=1||Read32(bytes,12)!=HeaderBytes||Read32(bytes,16)!=TileSize||Read32(bytes,20)!=Gutter||Read32(bytes,24)!=Extent||Read32(bytes,28)!=EarthRegionalDatasetContract.MinimumLevel||Read32(bytes,32)!=EarthRegionalDatasetContract.MaximumLevel||Read32(bytes,36)!=EarthRegionalDatasetContract.PageCount||Read32(bytes,40)!=3||Read32(bytes,44)!=EarthRegionalDatasetContract.HashCapacity||Read32(bytes,48)!=6||Read32(bytes,52)!=0){error="regional pack header";return false;}
            if(!bytes.AsSpan(96,32).SequenceEqual(Convert.FromHexString(EarthRegionalDatasetContract.IdentitySha256))||!bytes.AsSpan(128,32).SequenceEqual(Convert.FromHexString(EarthRegionalDatasetContract.PayloadSha256))){error="regional identity/payload";return false;}
            const int descriptor=192;
            var maximumLevel=(int)Read32(bytes,descriptor+12);var count=(int)Read32(bytes,descriptor+16);var tileBytes=(int)Read32(bytes,descriptor+20);var offset=(int)Read64(bytes,descriptor+24);
            if(Read32(bytes,descriptor)!=2||Read32(bytes,descriptor+4)!=2||maximumLevel<EarthRegionalDatasetContract.MinimumLevel||maximumLevel>EarthRegionalDatasetContract.MaximumLevel||count<1||count>EarthRegionalDatasetContract.PageCount||tileBytes!=ElevationTileBytes){error="regional elevation descriptor";return false;}
            var payloadOffset=offset+count*RecordBytes;var pages=new RegionalPage[count];
            for(var index=0;index<count;index++)
            {
                var record=offset+index*RecordBytes;var level=(int)Read32(bytes,record);var x=(int)Read32(bytes,record+4);var y=(int)Read32(bytes,record+8);
                var countX=level>=0&&level<31?1<<(level+1):0;var countY=level>=0&&level<31?1<<level:0;var ordered=index==0||level>pages[index-1].Level||(level==pages[index-1].Level&&(y>pages[index-1].Y||(y==pages[index-1].Y&&x>pages[index-1].X)));
                if(Read32(bytes,record+12)!=0||level<EarthRegionalDatasetContract.MinimumLevel||level>maximumLevel||x<0||x>=countX||y<0||y>=countY||!ordered||payloadOffset+(index+1L)*ElevationTileBytes>bytes.Length){error="regional elevation record";return false;}
                var values=new ushort[Extent*Extent];var source=payloadOffset+index*ElevationTileBytes;
                for(var pixel=0;pixel<values.Length;pixel++)values[pixel]=BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(source+pixel*2,2));
                pages[index]=new(level,x,y,values);
            }
            dataset=new(pages);error=string.Empty;return true;
        }
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException or OverflowException){error=exception.Message;return false;}
    }

    public bool TrySample(double u,double v,out double elevation)
    {
        var longitude=u*360d-180d;var latitude=90d-v*180d;
        if(longitude<EarthRegionalDatasetContract.WestDegrees||longitude>EarthRegionalDatasetContract.EastDegrees||latitude<EarthRegionalDatasetContract.SouthDegrees||latitude>EarthRegionalDatasetContract.NorthDegrees){elevation=0;return false;}
        for(var level=EarthRegionalDatasetContract.MaximumLevel;level>=EarthRegionalDatasetContract.MinimumLevel;level--)
        {
            var countX=1<<(level+1);var countY=1<<level;var scaledX=u*countX;var scaledY=v*countY;var x=Math.Min((int)Math.Floor(scaledX),countX-1);var y=Math.Min((int)Math.Floor(scaledY),countY-1);
            var index=Find(level,x,y);if(index<0)continue;var localX=scaledX-Math.Floor(scaledX);var localY=scaledY-Math.Floor(scaledY);var px=localX*TileSize+Gutter-.5d;var py=localY*TileSize+Gutter-.5d;
            var x0=Math.Clamp((int)Math.Floor(px),0,Extent-1);var y0=Math.Clamp((int)Math.Floor(py),0,Extent-1);var x1=Math.Min(x0+1,Extent-1);var y1=Math.Min(y0+1,Extent-1);var tx=px-Math.Floor(px);var ty=py-Math.Floor(py);var values=_pages[index].Values;
            var a=Decode(values[y0*Extent+x0]);var b=Decode(values[y0*Extent+x1]);var c=Decode(values[y1*Extent+x0]);var d=Decode(values[y1*Extent+x1]);elevation=Lerp(Lerp(a,b,tx),Lerp(c,d,tx),ty);return true;
        }
        elevation=0;return false;
    }

    private int Find(int level,int x,int y)
    {
        var low=0;var high=_pages.Length-1;
        while(low<=high){var middle=(low+high)>>1;var page=_pages[middle];var comparison=page.Level!=level?page.Level.CompareTo(level):page.Y!=y?page.Y.CompareTo(y):page.X.CompareTo(x);if(comparison==0)return middle;if(comparison<0)low=middle+1;else high=middle-1;}return -1;
    }
    private static uint Read32(byte[] bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
    private static ulong Read64(byte[] bytes,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset,8));
    private static double Decode(ushort value)=>EarthSurfaceDatasetContract.MinimumElevationMetres+value/65535d*(EarthSurfaceDatasetContract.MaximumElevationMetres-EarthSurfaceDatasetContract.MinimumElevationMetres);
    private static double Lerp(double a,double b,double t)=>a+(b-a)*t;
}
