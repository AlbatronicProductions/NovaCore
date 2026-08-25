using NovaCore.Core.Surface;

namespace NovaCore.Core.Camera;

/// <summary>Explicit authority for the player camera reference state.</summary>
public enum CameraReferenceAuthority : byte
{
    Inertial = 0,
    SurfaceRelative = 1,
}

/// <summary>
/// Retained, body-fixed camera authority. The canonical surface identity and all local camera
/// state are FP64 and independent of render topology, GPU residency, and simulation warp.
/// </summary>
public readonly record struct SurfaceCameraState(
    SurfaceAnchor Anchor,
    Double3 EyeOffsetEnuMetres,
    Double3 PivotOffsetEnuMetres,
    DoubleQuaternion BodyFixedOrientation,
    double LocalYawRadians,
    double LocalPitchRadians)
{
    public bool IsValid => Anchor.IsValid && EyeOffsetEnuMetres.IsFinite && PivotOffsetEnuMetres.IsFinite &&
        BodyFixedOrientation.IsFinite && Math.Abs(BodyFixedOrientation.LengthSquared - 1d) <= 1e-10d &&
        double.IsFinite(LocalYawRadians) && double.IsFinite(LocalPitchRadians) &&
        LocalPitchRadians >= -Math.PI * .5d && LocalPitchRadians <= Math.PI * .5d;

    public static bool TryCreate(
        in SurfaceAnchor anchor,
        in Double3 eyeOffsetEnuMetres,
        in Double3 pivotOffsetEnuMetres,
        in DoubleQuaternion bodyFixedOrientation,
        out SurfaceCameraState state)
    {
        state = default;
        if (!anchor.IsValid || !eyeOffsetEnuMetres.IsFinite || !pivotOffsetEnuMetres.IsFinite ||
            !bodyFixedOrientation.IsFinite || bodyFixedOrientation.LengthSquared <= 0d ||
            !SurfaceEnuFrame.TryCreate(anchor, out var enu) ||
            !TryExtractLocalLook(bodyFixedOrientation, enu, out var yawRadians, out var pitchRadians)) return false;
        state = new(anchor, eyeOffsetEnuMetres, pivotOffsetEnuMetres, bodyFixedOrientation.Normalized(),
            NormalizeYaw(yawRadians), pitchRadians);
        return state.IsValid;
    }

    public static bool TryCreateFreeLook(
        in SurfaceAnchor anchor,
        in Double3 eyeOffsetEnuMetres,
        in Double3 pivotOffsetEnuMetres,
        double localYawRadians,
        double localPitchRadians,
        out SurfaceCameraState state)
    {
        state = default;
        if (!anchor.IsValid || !eyeOffsetEnuMetres.IsFinite || !pivotOffsetEnuMetres.IsFinite ||
            !double.IsFinite(localYawRadians) || !double.IsFinite(localPitchRadians) ||
            localPitchRadians < -Math.PI * .5d || localPitchRadians > Math.PI * .5d ||
            !SurfaceEnuFrame.TryCreate(anchor, out var enu)) return false;
        var yaw = NormalizeYaw(localYawRadians);
        state = new(anchor, eyeOffsetEnuMetres, pivotOffsetEnuMetres,
            LookOrientation(enu, yaw, localPitchRadians), yaw, localPitchRadians);
        return state.IsValid;
    }

    public static DoubleQuaternion LookOrientation(in SurfaceEnuFrame enu, double yawRadians, double pitchRadians)
    {
        if (!enu.IsValid || !double.IsFinite(yawRadians) || !double.IsFinite(pitchRadians) ||
            pitchRadians < -Math.PI * .5d || pitchRadians > Math.PI * .5d) throw new ArgumentOutOfRangeException();
        var horizontal = (enu.North * Math.Cos(yawRadians) + enu.East * Math.Sin(yawRadians)).Normalized();
        var forward = (horizontal * Math.Cos(pitchRadians) + enu.Up * Math.Sin(pitchRadians)).Normalized();
        var rightCandidate = Double3.Cross(forward, enu.Up);
        var right = rightCandidate.LengthSquared > 1e-24d
            ? rightCandidate.Normalized()
            : (enu.East * Math.Cos(yawRadians) - enu.North * Math.Sin(yawRadians)).Normalized();
        var cameraUp = Double3.Cross(right, forward).Normalized();
        return QuaternionFromBasis(right, cameraUp, -forward);
    }

    public static bool TryExtractLocalLook(
        in DoubleQuaternion bodyFixedOrientation,
        in SurfaceEnuFrame enu,
        out double yawRadians,
        out double pitchRadians)
    {
        yawRadians = double.NaN;
        pitchRadians = double.NaN;
        if (!enu.IsValid || !bodyFixedOrientation.IsFinite || bodyFixedOrientation.LengthSquared <= 0d) return false;
        var orientation = bodyFixedOrientation.Normalized();
        var forward = orientation.Rotate(new Double3(0d, 0d, -1d)).Normalized();
        var upComponent = Math.Clamp(Double3.Dot(forward, enu.Up), -1d, 1d);
        pitchRadians = Math.Asin(upComponent);
        var horizontal = forward - enu.Up * upComponent;
        if (horizontal.LengthSquared <= 1e-24d)
        {
            var cameraUp = orientation.Rotate(Double3.UnitY).Normalized();
            horizontal = pitchRadians >= 0d ? -cameraUp : cameraUp;
            horizontal -= enu.Up * Double3.Dot(horizontal, enu.Up);
        }
        if (horizontal.LengthSquared <= 1e-24d) return false;
        horizontal = horizontal.Normalized();
        yawRadians = NormalizeYaw(Math.Atan2(Double3.Dot(horizontal, enu.East), Double3.Dot(horizontal, enu.North)));
        return double.IsFinite(yawRadians) && double.IsFinite(pitchRadians);
    }

    public static double NormalizeYaw(double yawRadians)
    {
        if (!double.IsFinite(yawRadians)) return double.NaN;
        var wrapped = Math.IEEERemainder(yawRadians, Math.Tau);
        return wrapped <= -Math.PI ? wrapped + Math.Tau : wrapped;
    }

    private static DoubleQuaternion QuaternionFromBasis(in Double3 x, in Double3 y, in Double3 z)
    {
        var trace = x.X + y.Y + z.Z;
        double qx, qy, qz, qw;
        if (trace > 0d) { var s = Math.Sqrt(trace + 1d) * 2d; qw = .25d * s; qx = (y.Z - z.Y) / s; qy = (z.X - x.Z) / s; qz = (x.Y - y.X) / s; }
        else if (x.X > y.Y && x.X > z.Z) { var s = Math.Sqrt(1d + x.X - y.Y - z.Z) * 2d; qw = (y.Z - z.Y) / s; qx = .25d * s; qy = (y.X + x.Y) / s; qz = (z.X + x.Z) / s; }
        else if (y.Y > z.Z) { var s = Math.Sqrt(1d + y.Y - x.X - z.Z) * 2d; qw = (z.X - x.Z) / s; qx = (y.X + x.Y) / s; qy = .25d * s; qz = (z.Y + y.Z) / s; }
        else { var s = Math.Sqrt(1d + z.Z - x.X - y.Y) * 2d; qw = (x.Y - y.X) / s; qx = (z.X + x.Z) / s; qy = (z.Y + y.Z) / s; qz = .25d * s; }
        return new DoubleQuaternion(qx, qy, qz, qw).Normalized();
    }
}
