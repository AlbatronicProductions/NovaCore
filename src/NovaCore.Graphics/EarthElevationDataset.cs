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

    internal static double CapElevationUpperBound(in Double3 direction,double angularRadius)
    {
        var values=Volatile.Read(ref _elevation)??throw new InvalidOperationException("Physical height data not loaded.");
        if(!direction.IsFinite||Math.Abs(direction.LengthSquared-1)>1e-10||!double.IsFinite(angularRadius)||angularRadius<0||angularRadius>Math.PI)
            throw new ArgumentOutOfRangeException(nameof(angularRadius));
        var unit=direction.Normalized();var horizontal=Math.Sqrt(unit.X*unit.X+unit.Z*unit.Z);
        var latitude=Math.Atan2(unit.Y,horizontal);
        var a=Math.BitIncrement(angularRadius+64*Math.ScaleB(1d,-52));
        if(Math.Abs(latitude)+a>=Math.PI/2)return MaximumElevationMetres;
        var longitude=BodyFixedGeography.LongitudeRadians(unit);
        var longitudeRadius=Math.Asin(Math.Min(1,Math.Sin(a)/horizontal));
        var u=(longitude/Math.Tau+.5)*Width-.5;
        var lowX=(int)Math.Floor(u-longitudeRadius/Math.Tau*Width)-1;
        var highX=(int)Math.Floor(u+longitudeRadius/Math.Tau*Width)+2;
        var lowY=Math.Clamp((int)Math.Floor((.5-(latitude+a)/Math.PI)*Height-.5)-1,0,Height-1);
        var highY=Math.Clamp((int)Math.Floor((.5-(latitude-a)/Math.PI)*Height-.5)+2,0,Height-1);
        // Every bilinear sample is a convex combination of these texels. The
        // extra cell collar encloses floating address/normalization rounding.
        if((long)(highX-lowX+1)*(highY-lowY+1)>4096)return MaximumElevationMetres;
        ushort maximum=0;
        for(var y=lowY;y<=highY;y++)for(var x=lowX;x<=highX;x++)maximum=Math.Max(maximum,values[y*Width+Mod(x,Width)]);
        return Math.BitIncrement(Decode(maximum)+128*Math.ScaleB(1d,-52)*(MaximumElevationMetres-MinimumElevationMetres));
    }

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
    internal static CollisionJet CollisionBounds(CollisionJet px,CollisionJet py,double finiteYError=0)
    {
        var values=Volatile.Read(ref _elevation)??throw new InvalidDataException("Collision elevation unavailable.");
        var x0=(int)Math.Floor(px.V.Low);var x1=(int)Math.Floor(px.V.High);var y0=(int)Math.Floor(py.V.Low);var y1=(int)Math.Floor(py.V.High);
        // Production clamps the row index before taking the fractional part.
        // At the north polar clamped-row join that can be discontinuous. A
        // gradient union is not a certificate across a discontinuity.
        var finitePy=py.V.Inflate(finiteYError);
        if(finitePy.Low<0&&finitePy.High>=0)throw new InvalidDataException("Collision geographic polar clamp join requires a separate bound.");
        if(x1-x0>4||y1-y0>4)throw new InvalidDataException("Collision geographic cell extent requires refinement.");
        CollisionJet? result=null;
        for(var y=y0;y<=y1;y++)for(var x=x0;x<=x1;x++)
        {
            var iy=Math.Clamp(y,0,Height-1);var jy=Math.Min(iy+1,Height-1);var ix=Mod(x,Width);var jx=Mod(x+1,Width);
            var u=px.WithRange(px.V.Clip(x,x+1))-x;var v=py.WithRange(py.V.Clip(y,y+1))-y;
            // Shared finite decoded texels define one continuous reference
            // field. Coefficient algebra must remain real interval algebra;
            // rounding b-a before promotion would break endpoint agreement.
            CollisionJet a=Decode(values[iy*Width+ix]),b=Decode(values[iy*Width+jx]),c=Decode(values[jy*Width+ix]),d=Decode(values[jy*Width+jx]);
            var sample=a+(b-a)*u+(c-a)*v+(d-b-c+a)*u*v;
            result=result is {} prior?prior.Union(sample):sample;
        }
        return result!.Value;
    }
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
