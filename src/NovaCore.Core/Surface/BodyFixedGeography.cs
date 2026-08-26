namespace NovaCore.Core.Surface;

/// <summary>
/// Canonical NovaCore body-fixed geography. The frame is right-handed with +Y at the north pole;
/// longitude zero is +X and positive (east) longitude advances toward -Z. This makes the local
/// surface basis +X East, +Y North, +Z Up satisfy East x North = Up and agrees with the existing
/// +90-degree NovaCore-surface-to-IAU basis conversion.
/// </summary>
public static class BodyFixedGeography
{
    public static Double3 DirectionFromLatitudeLongitude(double latitudeRadians, double longitudeRadians)
    {
        if (!double.IsFinite(latitudeRadians) || !double.IsFinite(longitudeRadians) ||
            latitudeRadians < -Math.PI * .5d || latitudeRadians > Math.PI * .5d)
            throw new ArgumentOutOfRangeException();
        var cosine = Math.Cos(latitudeRadians);
        return new(cosine * Math.Cos(longitudeRadians), Math.Sin(latitudeRadians),
            -cosine * Math.Sin(longitudeRadians));
    }

    public static double LatitudeRadians(in Double3 bodyFixedDirection)
    {
        if (!bodyFixedDirection.IsFinite || bodyFixedDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException(nameof(bodyFixedDirection));
        return Math.Asin(Math.Clamp(bodyFixedDirection.Normalized().Y, -1d, 1d));
    }

    public static double LongitudeRadians(in Double3 bodyFixedDirection)
    {
        if (!bodyFixedDirection.IsFinite || bodyFixedDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException(nameof(bodyFixedDirection));
        var direction = bodyFixedDirection.Normalized();
        return Math.Atan2(-direction.Z, direction.X);
    }
}
