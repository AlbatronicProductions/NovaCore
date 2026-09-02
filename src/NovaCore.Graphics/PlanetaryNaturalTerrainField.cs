using NovaCore.Core;
using System.Runtime.CompilerServices;

namespace NovaCore.Graphics;

/// <summary>
/// Stable identity for the isolated M12D-P2A natural-terrain field proof.
/// Camera, patch, owner, LOD, residency, frame, and worker state are absent by contract.
/// </summary>
public readonly record struct PlanetaryNaturalTerrainFieldIdentity(
    ulong BodyId,
    ulong PhysicalFieldGeneration,
    uint FamilyId,
    uint OctaveId,
    uint Seed)
{
    public bool IsValid => BodyId != 0 && PhysicalFieldGeneration != 0;
}

/// <summary>Signed, non-truncated P2A lattice cell identity.</summary>
public readonly record struct PlanetaryNaturalTerrainCell(long X, long Y, long Z);

/// <summary>FP64 body-centred domain reduction for one P2A sample.</summary>
public readonly record struct PlanetaryNaturalTerrainDomain(
    PlanetaryNaturalTerrainCell Cell,
    Double3 Fraction)
{
    public bool IsValid => Fraction.IsFinite &&
        Fraction.X is >= 0d and < 1d && Fraction.Y is >= 0d and < 1d && Fraction.Z is >= 0d and < 1d;
}

/// <summary>One P2A scalar displacement and its analytic body-fixed gradient.</summary>
public readonly record struct PlanetaryNaturalTerrainFieldSample(double Height, Double3 BodyGradient)
{
    public bool IsFinite => double.IsFinite(Height) && BodyGradient.IsFinite;
}

/// <summary>
/// Non-production M12D-P2A proof primitive: body-centred, deterministic eight-corner
/// gradient noise with quintic interpolation and an analytic spatial derivative.
/// </summary>
public static class PlanetaryNaturalTerrainField
{
    public const uint HashVersion = 1;
    public const ulong ProofGeneration = 1;
    public const double EarthReferenceRadiusMetres = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
    public const double MaximumUnitValue = 1.7320508075688772935274463415059d; // sqrt(3)
    public const double MaximumUnitGradient = 12.25d;

    // First little-endian words of SHA-256("NovaCore M12D P2A hash ...").  These
    // NovaCore-owned constants and the operation order below define hash version 1.
    private const uint HashInitial = 0x9B0425F0u;
    private const uint HashLaneMultiplier = 0xE46DA8FBu;
    private const uint HashLaneIncrement = 0x8C83052Fu;
    private const uint HashFinalMultiplier = 0x78232465u;
    private const double InverseSqrtFive = 0.447213595499957939281834733746d;

    public static PlanetaryNaturalTerrainDomain ReduceDomain(in Double3 normalizedBodyFixedDirection,
        double cellSizeMetres)
    {
        if (!normalizedBodyFixedDirection.IsFinite || normalizedBodyFixedDirection.LengthSquared <= 0d ||
            !double.IsFinite(cellSizeMetres) || cellSizeMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        var direction = normalizedBodyFixedDirection.Normalized();
        return ReduceBodyPoint(direction * EarthReferenceRadiusMetres, cellSizeMetres);
    }

    public static PlanetaryNaturalTerrainDomain ReduceBodyPoint(in Double3 bodyFixedPoint, double cellSizeMetres)
    {
        if (!bodyFixedPoint.IsFinite || !double.IsFinite(cellSizeMetres) || cellSizeMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        var q = bodyFixedPoint / cellSizeMetres;
        var x = SignedFloor(q.X); var y = SignedFloor(q.Y); var z = SignedFloor(q.Z);
        return new(new(x, y, z), new(q.X - x, q.Y - y, q.Z - z));
    }

    public static PlanetaryNaturalTerrainFieldSample Evaluate(in Double3 normalizedBodyFixedDirection,
        double cellSizeMetres, double amplitudeMetres, in PlanetaryNaturalTerrainFieldIdentity identity)
    {
        if (!normalizedBodyFixedDirection.IsFinite || normalizedBodyFixedDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException(nameof(normalizedBodyFixedDirection));
        var direction = normalizedBodyFixedDirection.Normalized();
        return EvaluateBodyPoint(direction * EarthReferenceRadiusMetres, cellSizeMetres, amplitudeMetres, identity);
    }

    public static PlanetaryNaturalTerrainFieldSample EvaluateBodyPoint(in Double3 bodyFixedPoint,
        double cellSizeMetres, double amplitudeMetres, in PlanetaryNaturalTerrainFieldIdentity identity)
    {
        if (!identity.IsValid || !bodyFixedPoint.IsFinite || !double.IsFinite(cellSizeMetres) || cellSizeMetres <= 0d ||
            !double.IsFinite(amplitudeMetres) || amplitudeMetres < 0d)
            throw new ArgumentOutOfRangeException();

        var qx=bodyFixedPoint.X/cellSizeMetres;var qy=bodyFixedPoint.Y/cellSizeMetres;var qz=bodyFixedPoint.Z/cellSizeMetres;
        var cellX=SignedFloor(qx);var cellY=SignedFloor(qy);var cellZ=SignedFloor(qz);
        var fx=qx-cellX;var fy=qy-cellY;var fz=qz-cellZ;
        var ux=Fade(fx);var uy=Fade(fy);var uz=Fade(fz);
        var dux=FadeDerivative(fx);var duy=FadeDerivative(fy);var duz=FadeDerivative(fz);
        var wx0=1d-ux;var wy0=1d-uy;var wz0=1d-uz;var identityHash=HashIdentity(identity);
        var x0=HashCoordinate(cellX,0u);var x1=HashCoordinate(cellX+1,0u);
        var y0=HashCoordinate(cellY,1u);var y1=HashCoordinate(cellY+1,1u);
        var z0=HashCoordinate(cellZ,2u);var z1=HashCoordinate(cellZ+1,2u);
        var value=0d;var gradientX=0d;var gradientY=0d;var gradientZ=0d;
        Accumulate(FinalizeHash(identityHash^x0^y0^z0),fx,fy,fz,wx0,wy0,wz0,-dux,-duy,-duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x1^y0^z0),fx-1d,fy,fz,ux,wy0,wz0,dux,-duy,-duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x0^y1^z0),fx,fy-1d,fz,wx0,uy,wz0,-dux,duy,-duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x1^y1^z0),fx-1d,fy-1d,fz,ux,uy,wz0,dux,duy,-duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x0^y0^z1),fx,fy,fz-1d,wx0,wy0,uz,-dux,-duy,duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x1^y0^z1),fx-1d,fy,fz-1d,ux,wy0,uz,dux,-duy,duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x0^y1^z1),fx,fy-1d,fz-1d,wx0,uy,uz,-dux,duy,duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        Accumulate(FinalizeHash(identityHash^x1^y1^z1),fx-1d,fy-1d,fz-1d,ux,uy,uz,dux,duy,duz,ref value,ref gradientX,ref gradientY,ref gradientZ);
        var scale=amplitudeMetres/cellSizeMetres;
        return new(value*amplitudeMetres,new(gradientX*scale,gradientY*scale,gradientZ*scale));
    }

    public static uint HashCell(in PlanetaryNaturalTerrainFieldIdentity identity,
        in PlanetaryNaturalTerrainCell cell)
    {
        if (!identity.IsValid) throw new ArgumentOutOfRangeException(nameof(identity));
        return HashCell(HashIdentity(identity), cell);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashIdentity(in PlanetaryNaturalTerrainFieldIdentity identity)
    {
        var hash = HashInitial;
        hash = MixLane(hash, HashVersion);
        hash = MixLane(hash, (uint)identity.BodyId);
        hash = MixLane(hash, (uint)(identity.BodyId >> 32));
        hash = MixLane(hash, (uint)identity.PhysicalFieldGeneration);
        hash = MixLane(hash, (uint)(identity.PhysicalFieldGeneration >> 32));
        hash = MixLane(hash, identity.FamilyId);
        hash = MixLane(hash, identity.OctaveId);
        return MixLane(hash, identity.Seed);
    }

    private static uint HashCell(uint identityHash, in PlanetaryNaturalTerrainCell cell)
    {
        return FinalizeHash(identityHash^HashCoordinate(cell.X,0u)^HashCoordinate(cell.Y,1u)^HashCoordinate(cell.Z,2u));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashCoordinate(long value,uint axis)
    {
        var bits=unchecked((ulong)value);
        return HashCoordinateWord((uint)bits,axis*2u)^HashCoordinateWord((uint)(bits>>32),axis*2u+1u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashCoordinateWord(uint value,uint lane)
    {
        var hash=value^unchecked(HashLaneIncrement+lane*HashFinalMultiplier);
        hash^=hash>>16;hash=unchecked(hash*HashLaneMultiplier);hash^=hash>>15;
        return RotateLeft(hash,lane*5u+4u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FinalizeHash(uint hash)
    {
        hash ^= hash >> 16;
        hash = unchecked(hash * HashLaneIncrement);
        hash ^= hash >> 15;
        hash = unchecked(hash * HashFinalMultiplier);
        return hash ^ (hash >> 16);
    }

    /// <summary>
    /// Selects one of 24 symmetric unit directions: every signed permutation of
    /// (2,1,0)/sqrt(5).  Selection is an exact function of the low hash modulo 24.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 SelectGradient(uint hash)
    {
        var index = hash % 24u;
        var zeroAxis = (int)(index / 8u);
        var lane = index % 8u;
        var swap = (lane & 4u) != 0u;
        var first = (swap ? 1d : 2d) * InverseSqrtFive;
        var second = (swap ? 2d : 1d) * InverseSqrtFive;
        if ((lane & 1u) != 0u) first = -first;
        if ((lane & 2u) != 0u) second = -second;
        return zeroAxis switch
        {
            0 => new(0d, first, second),
            1 => new(first, 0d, second),
            _ => new(first, second, 0d)
        };
    }

    public static double ValueBound(double amplitudeMetres) =>
        ValidateAmplitude(amplitudeMetres) * MaximumUnitValue;

    public static double GradientBound(double cellSizeMetres, double amplitudeMetres)
    {
        if (!double.IsFinite(cellSizeMetres) || cellSizeMetres <= 0d) throw new ArgumentOutOfRangeException(nameof(cellSizeMetres));
        return ValidateAmplitude(amplitudeMetres) * MaximumUnitGradient / cellSizeMetres;
    }

    private static long SignedFloor(double value)
    {
        var floor = Math.Floor(value);
        // FP64 integers are exact through 2^53. One positive corner is evaluated,
        // so the positive endpoint is deliberately excluded.
        const double maximumExactInteger=9_007_199_254_740_992d;
        if (floor<=-maximumExactInteger||floor>=maximumExactInteger-1d)throw new ArgumentOutOfRangeException(nameof(value));
        return (long)floor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixLane(uint hash, uint value)
    {
        hash = RotateLeft(hash ^ value, 13);
        hash = unchecked(hash * HashLaneMultiplier + HashLaneIncrement);
        return hash ^ (hash >> 15);
    }

    private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));
    private static uint RotateLeft(uint value, uint count) => (value << (int)count) | (value >> (int)(32u - count));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Accumulate(uint hash,double ox,double oy,double oz,double wx,double wy,double wz,
        double dwx,double dwy,double dwz,ref double value,ref double gradientX,ref double gradientY,ref double gradientZ)
    {
        var gradient=SelectGradient(hash);var cornerValue=gradient.X*ox+gradient.Y*oy+gradient.Z*oz;
        var weight=wx*wy*wz;value+=weight*cornerValue;
        gradientX+=weight*gradient.X+dwx*wy*wz*cornerValue;
        gradientY+=weight*gradient.Y+wx*dwy*wz*cornerValue;
        gradientZ+=weight*gradient.Z+wx*wy*dwz*cornerValue;
    }
    private static double Fade(double value) => value * value * value * (value * (value * 6d - 15d) + 10d);
    private static double FadeDerivative(double value) => 30d * value * value * (value - 1d) * (value - 1d);
    private static double ValidateAmplitude(double value)
    {
        if (!double.IsFinite(value) || value < 0d) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
}
