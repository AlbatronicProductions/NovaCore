using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;

namespace NovaCore.Graphics;

/// <summary>
/// Canonical address of the stable CCF presentation origin used by every
/// visible resolution around one surface anchor.  It is presentation state;
/// canonical patch geography remains body fixed.
/// </summary>
public readonly record struct PlanetaryBillboardSnapAddress(
    CubeSphereFace Face,
    int Level,
    int X,
    int Y)
{
    public bool IsValid => Level is >= 0 and <= PlanetaryDynamicAnchoredSurface.MaximumLevel &&
        X >= 0 && Y >= 0 && X < 1 << Level && Y < 1 << Level;
}

/// <summary>
/// One shared, snapped spherical-billboard presentation frame.  Origin and
/// basis are FP64 CCF authority; GPU encoding happens only after positions and
/// the camera have been expressed relative to this frame.
/// </summary>
public readonly record struct PlanetarySphericalBillboardFrame(
    ulong BodyId,
    PlanetaryBillboardSnapAddress Snap,
    Double3 CanonicalReferenceDirection,
    Double3 SphericalOriginBodyFixed,
    SurfaceEnuFrame TangentBasis,
    uint PresentationGeneration)
{
    public const int ProductionSnapLevel = 12;
    public const double RetentionMarginCells = .125d;
    // A retained origin may never become an unbounded near-field origin.  The
    // relaxed-cube chart is deliberately given a generous angular envelope;
    // UV hysteresis remains the tighter normal-case snap policy.
    public const double MaximumReferenceAngularDistanceCells = 4d;

    public static double MaximumReferenceAngularDistanceRadians =>
        MaximumReferenceAngularDistanceCells / (1 << ProductionSnapLevel);

    public static double MaximumOriginDistanceMetres(double bodyRadius,double maximumTerrainHeightMetres,
        double maximumCameraAltitudeMetres=PlanetaryDynamicAnchoredSurface.VisibilityReleaseAltitudeMetres)
    {
        if(!double.IsFinite(bodyRadius)||bodyRadius<=0d||!double.IsFinite(maximumTerrainHeightMetres)||maximumTerrainHeightMetres<0d||
            !double.IsFinite(maximumCameraAltitudeMetres)||maximumCameraAltitudeMetres<0d)
            throw new ArgumentOutOfRangeException();
        var originRadius=bodyRadius+maximumTerrainHeightMetres;
        var cameraRadius=originRadius+maximumCameraAltitudeMetres;
        return Math.Sqrt(originRadius*originRadius+cameraRadius*cameraRadius-
            2d*originRadius*cameraRadius*Math.Cos(MaximumReferenceAngularDistanceRadians));
    }

    public bool IsValid => BodyId != 0 && Snap.IsValid && CanonicalReferenceDirection.IsFinite &&
        Math.Abs(CanonicalReferenceDirection.LengthSquared - 1d) <= 1e-12d &&
        SphericalOriginBodyFixed.IsFinite && SphericalOriginBodyFixed.LengthSquared > 0d &&
        TangentBasis.IsValid && PresentationGeneration != 0u;

    public Double3 ToTangent(in Double3 bodyFixedPosition)
    {
        var offset = bodyFixedPosition - SphericalOriginBodyFixed;
        return new(Double3.Dot(offset, TangentBasis.East),
            Double3.Dot(offset, TangentBasis.North), Double3.Dot(offset, TangentBasis.Up));
    }

    public Double3 FromTangent(in Double3 tangentPosition) => SphericalOriginBodyFixed +
        TangentBasis.East * tangentPosition.X + TangentBasis.North * tangentPosition.Y +
        TangentBasis.Up * tangentPosition.Z;

    public Double3 CameraRelative(in Double3 bodyFixedPosition, in Double3 cameraBodyFixed)
    {
        var body = ToTangent(bodyFixedPosition);
        var camera = ToTangent(cameraBodyFixed);
        var relative = body - camera;
        return TangentBasis.East * relative.X + TangentBasis.North * relative.Y +
            TangentBasis.Up * relative.Z;
    }

    public NativeAnchoredSurfacePresentation Encode()
    {
        if (!IsValid) throw new InvalidOperationException("Cannot encode an invalid spherical-billboard frame.");
        return new()
        {
            Origin = Encode(SphericalOriginBodyFixed),
            East = Encode(TangentBasis.East),
            North = Encode(TangentBasis.North),
            Up = Encode(TangentBasis.Up),
            BodyIdLow = (uint)BodyId,
            BodyIdHigh = (uint)(BodyId >> 32),
            SnapIdentity = (uint)Snap.Face | (uint)Snap.Level << 3,
            PresentationGeneration = PresentationGeneration
        };
    }

    public static PlanetarySphericalBillboardFrame Resolve(
        in PlanetarySphericalBillboardFrame previous,
        ulong bodyId,
        double bodyRadius,
        in PlanetaryTerrainDefinition terrain,
        in Double3 requestedReferenceDirection)
    {
        if (bodyId == 0 || !double.IsFinite(bodyRadius) || bodyRadius <= 0d || !terrain.IsValid ||
            !requestedReferenceDirection.IsFinite || requestedReferenceDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException();
        var direction = requestedReferenceDirection.Normalized();
        if (!RelaxedCubeSphereProjection.TryAddress(direction, out var face, out var u, out var v))
            throw new InvalidOperationException("The billboard reference has no canonical cube-sphere address.");

        var level = ProductionSnapLevel;
        if (previous.IsValid && previous.BodyId == bodyId && previous.Snap.Level == level &&
            previous.Snap.Face == face && Retains(previous.Snap, u, v) &&
            AngularDistance(previous.CanonicalReferenceDirection,direction) <=
                MaximumReferenceAngularDistanceRadians)
            return previous;

        var cells = 1 << level;
        var x = Math.Min((int)Math.Floor(u * cells), cells - 1);
        var y = Math.Min((int)Math.Floor(v * cells), cells - 1);
        var snappedU = (x + .5d) / cells;
        var snappedV = (y + .5d) / cells;
        var snappedDirection = RelaxedCubeSphereProjection.UnitDirection(face, snappedU, snappedV);
        var physical = terrain.SamplePhysicalSurface(snappedDirection);
        var origin = snappedDirection * (bodyRadius + physical.FinalHeightMetres);
        var eastCandidate = Double3.Cross(Double3.UnitY, snappedDirection);
        var east = eastCandidate.LengthSquared > 1e-24d
            ? eastCandidate.Normalized()
            : Double3.Cross(Double3.UnitZ, snappedDirection).Normalized();
        var north = Double3.Cross(snappedDirection, east).Normalized();
        var tangent = new SurfaceEnuFrame(east, north, snappedDirection);
        if (!tangent.IsValid) throw new InvalidOperationException("The snapped billboard tangent frame is invalid.");
        var generation = previous.PresentationGeneration == uint.MaxValue
            ? 1u
            : previous.PresentationGeneration + 1u;
        if (generation == 0u) generation = 1u;
        return new(bodyId, new(face, level, x, y), snappedDirection, origin, tangent, generation);
    }

    private static bool Retains(in PlanetaryBillboardSnapAddress snap, double u, double v)
    {
        var cells = 1 << snap.Level;
        var margin = RetentionMarginCells / cells;
        var minimumU = snap.X / (double)cells - margin;
        var maximumU = (snap.X + 1d) / cells + margin;
        var minimumV = snap.Y / (double)cells - margin;
        var maximumV = (snap.Y + 1d) / cells + margin;
        return u >= minimumU && u <= maximumU && v >= minimumV && v <= maximumV;
    }

    private static double AngularDistance(in Double3 first,in Double3 second) =>
        Math.Acos(Math.Clamp(Double3.Dot(first,second),-1d,1d));

    private static NativeEncodedPosition Encode(in Double3 value)
    {
        var encoded = EncodedPosition.Encode(value);
        return new()
        {
            HighX = encoded.HighX, HighY = encoded.HighY, HighZ = encoded.HighZ,
            LowX = encoded.LowX, LowY = encoded.LowY, LowZ = encoded.LowZ
        };
    }
}
