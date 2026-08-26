using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

/// <summary>
/// Topology-neutral CPU elevation oracle used by camera clearance and parity
/// checks. Runtime terrain-v5 GPU ownership remains in the NCCUBE hierarchy.
/// </summary>
public static class EarthElevationDataset
{
    public const int Width = 8192;
    public const int Height = 4096;
    public const double MinimumElevationMetres = -11_000d;
    public const double MaximumElevationMetres = 9_000d;
    public const string Sha256 = "4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317";

    private static readonly object Gate = new();
    private static ushort[]? _elevation;

    public static bool IsLoaded => Volatile.Read(ref _elevation) is not null;

    public static bool TryLoad(string runtimeDirectory, out string error)
    {
        if (string.IsNullOrWhiteSpace(runtimeDirectory)) { error = "Earth runtime directory is empty."; return false; }
        if (IsLoaded) { error = string.Empty; return true; }
        var path = Path.Combine(runtimeDirectory, "earth_elevation_8192x4096.r16");
        if (!File.Exists(path)) { error = $"Earth elevation oracle: '{path}' is unavailable."; return false; }
        try
        {
            var bytes = File.ReadAllBytes(path);
            var expected = Width * Height * sizeof(ushort);
            if (bytes.Length != expected) { error = $"Earth elevation has {bytes.Length} bytes; expected {expected}."; return false; }
            var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(actual, Sha256, StringComparison.Ordinal))
            { error = $"Earth elevation checksum mismatch: {actual}."; return false; }
            var values = new ushort[expected / sizeof(ushort)];
            for (var index = 0; index < values.Length; index++)
                values[index] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(index * 2, 2));
            lock (Gate) _elevation ??= values;
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { error = $"Earth elevation oracle: {exception.Message}"; return false; }
    }

    public static double SampleHeight(in Double3 bodyDirection) => Math.Max(0d, SampleElevation(bodyDirection));

    public static double SampleElevation(in Double3 bodyDirection)
    {
        if (!bodyDirection.IsFinite || bodyDirection.LengthSquared <= 0d) throw new ArgumentOutOfRangeException(nameof(bodyDirection));
        var values = Volatile.Read(ref _elevation);
        if (values is null) return SampleFallback(bodyDirection);
        var direction = bodyDirection.Normalized();
        var u = BodyFixedGeography.LongitudeRadians(direction) / Math.Tau + .5d;
        u -= Math.Floor(u);
        var v = Math.Acos(Math.Clamp(direction.Y, -1d, 1d)) / Math.PI;
        var px = u * Width - .5d; var py = v * Height - .5d;
        var x0 = (int)Math.Floor(px); var y0 = Math.Clamp((int)Math.Floor(py), 0, Height - 1);
        var x1 = Mod(x0 + 1, Width); x0 = Mod(x0, Width); var y1 = Math.Min(y0 + 1, Height - 1);
        var tx = px - Math.Floor(px); var ty = py - Math.Floor(py);
        var a = Decode(values[y0 * Width + x0]); var b = Decode(values[y0 * Width + x1]);
        var c = Decode(values[y1 * Width + x0]); var d = Decode(values[y1 * Width + x1]);
        return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
    }

    private static double Decode(ushort value) => MinimumElevationMetres + value / 65535d * (MaximumElevationMetres - MinimumElevationMetres);
    private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    private static double SampleFallback(in Double3 bodyDirection)
    {
        var direction = bodyDirection.Normalized();
        var continental=.46d*Math.Sin(Double3.Dot(direction,new(.8017837257372732,.2672612419124244,.5345224838248488))*3.1d+.7d)
            +.31d*Math.Sin(Double3.Dot(direction,new(-.4082482904638631,.8164965809277261,.4082482904638631))*5.3d-1.2d)
            +.23d*Math.Sin(Double3.Dot(direction,new(.1825741858350554,-.3651483716701107,.9128709291752769))*8.7d+.35d);
        return Math.Clamp(Math.Pow(Math.Max(0d,continental-.02d),2d)*5_200d,0d,MaximumElevationMetres);
    }
}
