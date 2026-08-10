using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Immutable metadata for the checked Earth presentation dataset.</summary>
public static class EarthSurfaceDatasetContract
{
    public const string Schema = "NovaCore.EarthVirtualTexture/2";
    public const string IdentitySha256 = "53c8cea5328e20e610b1ef4ddc714a5d01f79ba2a5cbb03afb599038705e5426";
    public const string PayloadSha256 = "377ef730ce530bf503075d5a9f5ce0fe41b803599f6fbc5b57b8c32019e65513";
    public const string RuntimePackSha256 = "dbcc006dc6a29d88b64b1dd4bca2ef63e7ac32879a30da6685d9db8b1860ae73";
    public const string ManifestSha256 = "f684e8ed1662c919fc1f20015640bc3ecdd3f20504e48defb4c9c9884fa10f1f";
    public const string ElevationSha256 = "4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317";
    public const int TileSize = 256;
    public const int TileGutter = 2;
    public const int PhysicalTileExtent = 260;
    public const int MaximumLevel = 4;
    public const int TileCount = 682;
    public const int ElevationWidth = 8192;
    public const int ElevationHeight = 4096;
    public const int AlbedoTileBytes = 270_400;
    public const int ElevationTileBytes = 135_200;
    public const int CloudTileBytes = 67_600;
    public const int TileRecordBytes = 473_200;
    public const int UploadBudgetTiles = 2;
    public const int StagingBudgetBytes = 946_400;
    public const long RuntimePackBytes = 322_722_528;
    public const double MinimumElevationMetres = -11_000d;
    public const double MaximumElevationMetres = 9_000d;
    public const long PhysicalPoolBudgetBytes = 60_569_600;
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
        if(!bodyFixedDirection.IsFinite||bodyFixedDirection.LengthSquared<=0d)throw new ArgumentOutOfRangeException(nameof(bodyFixedDirection));
        var direction=bodyFixedDirection.Normalized();var u=Math.IEEERemainder(Math.Atan2(direction.Z,direction.X)/Math.Tau+.5d,1d);if(u<0d)u+=1d;var v=Math.Acos(Math.Clamp(direction.Y,-1d,1d))/Math.PI;
        level=Math.Clamp(level,0,EarthSurfaceDatasetContract.MaximumLevel);var tilesX=1<<(level+1);var tilesY=1<<level;var x=Math.Min((int)Math.Floor(u*tilesX),tilesX-1);var y=Math.Min((int)Math.Floor(v*tilesY),tilesY-1);return TileIndex(level,x,y);
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

    public static bool IsLoaded => Volatile.Read(ref _elevation) is not null;

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
            lock (Gate) _elevation ??= values;
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
