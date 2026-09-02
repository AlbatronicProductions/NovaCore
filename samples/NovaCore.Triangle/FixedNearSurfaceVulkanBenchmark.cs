using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Graphics;

// Permanent benchmark camera authority for a repeatable, settled near-surface measurement.
// It intentionally sits after interactive presentation input in the host callback so a
// physical mouse wheel or key state cannot turn a fixed-pose benchmark into a traversal.
internal sealed class FixedNearSurfaceVulkanBenchmark
{
    private const double LateralExcursionMetres = 128d;
    private readonly Double3 _direction;
    private readonly DoubleQuaternion _orientation;
    private readonly double _altitudeMetres;
    private readonly bool _motionReplay;
    private int _frame;
    private double _minimumAltitudeMetres = double.PositiveInfinity;
    private double _maximumAltitudeMetres = double.NegativeInfinity;

    public FixedNearSurfaceVulkanBenchmark(CameraState camera, in PlanetRenderProxy body,
        double altitudeMetres, bool motionReplay)
    {
        if (!double.IsFinite(altitudeMetres) || altitudeMetres < EarthPlanetaryScene.MinimumTerrainClearanceMetres)
            throw new ArgumentOutOfRangeException(nameof(altitudeMetres));

        var rootToBody = body.BodyFixedToRoot.Conjugate().Normalized();
        _direction = rootToBody.Rotate(camera.Position.Value - body.Position.Value).Normalized();
        _orientation = camera.Orientation;
        _altitudeMetres = altitudeMetres;
        _motionReplay = motionReplay;
    }

    public string FinalReport =>
        $"M12C fixed pose: frames={_frame}; requestedAltitude={_altitudeMetres:R}m; " +
        $"heldAltitude=[{_minimumAltitudeMetres:R}, {_maximumAltitudeMetres:R}]m; " +
        $"range={_maximumAltitudeMetres - _minimumAltitudeMetres:R}m; motionReplay={_motionReplay}";

    public void ApplyPose(CameraState camera, in PlanetRenderProxy body)
    {
        var direction = _direction;
        var orientation = _orientation;
        if (_motionReplay)
            ApplyMotion(ref direction, ref orientation);

        var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
        var cameraBody = direction * (body.RadiusMetres + physical.FinalHeightMetres + _altitudeMetres);
        camera.Position = camera.Position with { Value = body.Position.Value + body.BodyFixedToRoot.Rotate(cameraBody) };
        camera.Orientation = orientation;

        var heldAltitude = Math.Sqrt(cameraBody.LengthSquared) - (body.RadiusMetres + physical.FinalHeightMetres);
        _minimumAltitudeMetres = Math.Min(_minimumAltitudeMetres, heldAltitude);
        _maximumAltitudeMetres = Math.Max(_maximumAltitudeMetres, heldAltitude);
        _frame++;
    }

    private void ApplyMotion(ref Double3 direction, ref DoubleQuaternion orientation)
    {
        // The performance pass is always static.  This optional separate replay only
        // exercises body-fixed yaw and a bounded tangent excursion for visual inspection.
        const int staticFrames = 60, yawFrames = 180, lateralFrames = 90, returnFrames = 90;
        var tangent = PlanetarySurfaceFrame.AtDirection(_direction);
        if (_frame < staticFrames)
            return;

        if (_frame < staticFrames + yawFrames)
        {
            var yaw = 2d * Math.PI * ((_frame - staticFrames) / (double)yawFrames);
            orientation = tangent.LookOrientation(yaw, -.035d);
            return;
        }

        var lateralStart = staticFrames + yawFrames;
        var lateralAmount = _frame < lateralStart + lateralFrames
            ? (_frame - lateralStart) / (double)lateralFrames
            : 1d - Math.Clamp((_frame - lateralStart - lateralFrames) / (double)returnFrames, 0d, 1d);
        direction = (_direction + tangent.East * (LateralExcursionMetres * lateralAmount / 6_378_137d)).Normalized();
        orientation = PlanetarySurfaceFrame.AtDirection(direction).LookOrientation(0d, -.035d);
    }
}
