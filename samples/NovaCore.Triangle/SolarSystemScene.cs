using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

internal static class SolarOverlayLayout
{
    internal const double CharacterStrideNdc = .008d;
    internal const double CellWidthNdc = .0019d;
    internal const double CellHeightNdc = .0024d;
    internal const double LabelOffsetXNdc = .008d;
    internal const double LabelOffsetYNdc = .008d;
    internal const double FocusedLabelOffsetYNdc = .0105d;
    internal const double CollisionMarginNdc = .0035d;
    internal const double ScreenEdgeMarginNdc = .012d;

    // Vulkan's positive-height viewport maps increasing NDC Y down the framebuffer.
    internal static (double X, double Y) LabelCellOffset(int characterIndex, int column, int row) =>
        (characterIndex * CharacterStrideNdc + column * CellWidthNdc, row * CellHeightNdc);

    internal static int GlyphColumnBit(int column) => 1 << (2 - column);

    internal static bool TryProjectLabel(
        in PlanetRenderProxy body,
        CameraState camera,
        bool focused,
        out SolarScreenRect bounds,
        out double depth)
    {
        bounds = default;
        depth = double.NaN;
        if (!TryProjectBody(body, camera, out var anchorX, out var anchorY, out _, out depth)) return false;
        var length = body.Label?.Length ?? 0;
        if (length == 0) return false;
        var minX = anchorX + LabelOffsetXNdc;
        var minY = anchorY + (focused ? FocusedLabelOffsetYNdc : LabelOffsetYNdc);
        bounds = new SolarScreenRect(
            minX,
            minY,
            minX + (length - 1) * CharacterStrideNdc + 3d * CellWidthNdc,
            minY + 5d * CellHeightNdc);
        return bounds.IsFinite && bounds.MinX >= -1d + ScreenEdgeMarginNdc && bounds.MaxX <= 1d - ScreenEdgeMarginNdc &&
            bounds.MinY >= -1d + ScreenEdgeMarginNdc && bounds.MaxY <= 1d - ScreenEdgeMarginNdc;
    }

    internal static bool TryProjectBody(in PlanetRenderProxy body, CameraState camera, out double anchorX, out double anchorY, out double apparentRadiusNdc, out double depth)
    {
        anchorX = anchorY = apparentRadiusNdc = depth = double.NaN;
        var view = camera.Orientation.Conjugate().Normalized().Rotate(body.Position.Value - camera.Position.Value);
        var forward = -view.Z;
        if (!view.IsFinite || !double.IsFinite(forward) || forward < camera.Projection.NearClip) return false;
        var scale = 1d / Math.Tan(camera.Projection.VerticalFieldOfViewRadians * .5d);
        anchorX = scale / camera.Projection.AspectRatio * view.X / forward;
        anchorY = -scale * view.Y / forward;
        apparentRadiusNdc = scale * body.RadiusMetres / forward;
        depth = 1d - camera.Projection.NearClip / forward;
        return double.IsFinite(anchorX) && double.IsFinite(anchorY) && double.IsFinite(apparentRadiusNdc) && apparentRadiusNdc >= 0d &&
            double.IsFinite(depth) && depth is >= 0d and < 1d && anchorX + apparentRadiusNdc >= -1d && anchorX - apparentRadiusNdc <= 1d &&
            anchorY + apparentRadiusNdc >= -1d && anchorY - apparentRadiusNdc <= 1d;
    }

    internal static bool Overlaps(in SolarScreenRect left, in SolarScreenRect right) =>
        left.MinX < right.MaxX + CollisionMarginNdc && left.MaxX + CollisionMarginNdc > right.MinX &&
        left.MinY < right.MaxY + CollisionMarginNdc && left.MaxY + CollisionMarginNdc > right.MinY;
}

internal enum SolarCameraPresentationMode
{
    Free3D,
    SolarMap,
    SurfaceLocal
}

internal readonly record struct SolarScreenRect(double MinX, double MinY, double MaxX, double MaxY)
{
    internal bool IsFinite => double.IsFinite(MinX) && double.IsFinite(MinY) && double.IsFinite(MaxX) && double.IsFinite(MaxY);
}

internal sealed class SolarSystemScene
{
    internal static readonly ulong[] BodyOrder = [2, 3, 4, 6, 7, 8, 9, 10, 11, 12];
    internal const int OrbitPathCount = 9;
    internal const int OrbitSegmentCount = 128;
    internal const int OrbitSampleCount = OrbitSegmentCount + 1;
    internal const int OrbitVertexCount = OrbitPathCount * OrbitSegmentCount * 2;
    internal const double InitialOverviewDistanceAu = 58d;
    internal const double MaximumOverviewDistanceAu = 100d;
    internal const uint LabelVisibleBit = 0x4000_0000u;
    internal const uint StellarPresentationBit = 0x2000_0000u;
    internal const uint MarkerVisibleBit = 0x1000_0000u;
    internal const int OrbitOpacityShift = 8;
    internal const uint OrbitOpacityMask = 0x0000_FF00u;
    internal const double SolarMapPitchRadians = 23.439291111d * Math.PI / 180d;
    internal const double FocusTargetRadiusNdc = .15d;

    private static readonly Float3[] Colors =
    [
        new(1f, .82f, .35f), new(.48f, .48f, .48f), new(.88f, .72f, .42f), new(.08f, .32f, .72f), new(.62f, .62f, .62f),
        new(.72f, .25f, .14f), new(.72f, .53f, .32f), new(.82f, .68f, .38f), new(.38f, .78f, .86f), new(.12f, .32f, .86f)
    ];
    private const uint FocusedOverlayBit = 0x8000_0000u;
    private const double OrbitSensitivity = .002d;
    internal const double SpeedHudDisplaySeconds = 2d;
    internal const double SpeedHudFadeSeconds = .75d;
    private readonly CelestialSystemDefinition _system;
    private readonly ReferenceFrameId _root;
    private readonly int[] _traversalIndices;
    private readonly ReferenceFrameEvaluation[] _evaluations;
    private readonly FrameTransform[] _roots;
    private readonly ReferenceFrameEvaluation[] _staging;
    private readonly FrameTransform[] _stagingRoots;
    private readonly EvaluatedPlanetaryBody[] _bodyStaging;
    private readonly Double3[] _rootOrbitSamples;
    private readonly Double3[] _orbitSampleStaging;
    private readonly int[] _orbitTraversalIndices;
    private readonly double[] _orbitPeriods;
    private readonly SimulationClock _clock;
    private readonly bool[] _visibleLabels = new bool[BodyOrder.Length];
    private readonly bool[] _visibleMarkers = new bool[BodyOrder.Length];
    private readonly byte[] _orbitOpacityBytes = new byte[OrbitPathCount];
    private readonly bool[] _labelBoundsValid = new bool[BodyOrder.Length];
    private readonly SolarScreenRect[] _labelBounds = new SolarScreenRect[BodyOrder.Length];
    private readonly ulong[] _visibleLabelIds = new ulong[BodyOrder.Length];
    private PlanetaryRepresentationHandoff _handoff = new(EarthPlanetaryScene.HandoffConfiguration);
    private PlanetaryRepresentationBlend _blend;
    private int _rateStepIndex = 1;
    private double _speedHudSecondsRemaining;
    private double _orbitDistance;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;
    private PlanetarySurfaceFocus? _surfaceFocus;
    private double _surfaceYawRadians;
    private double _surfacePitchRadians=-Math.PI/12d;
    private PlanetaryCameraPresentationMode _surfaceCameraMode;
    private float _eyeballWeight;

    private SolarSystemScene(
        CelestialSystemDefinition system,
        ReferenceFrameId root,
        int[] traversalIndices,
        Double3[] rootOrbitSamples,
        int[] orbitTraversalIndices,
        double[] orbitPeriods,
        SimulationInstant initialTime,
        DateTimeOffset? startupUtc)
    {
        _system = system;
        _root = root;
        _traversalIndices = traversalIndices;
        _rootOrbitSamples = rootOrbitSamples;
        _orbitSampleStaging = new Double3[rootOrbitSamples.Length];
        _orbitTraversalIndices = orbitTraversalIndices;
        _orbitPeriods = orbitPeriods;
        _evaluations = new ReferenceFrameEvaluation[system.Count];
        _roots = new FrameTransform[system.Count];
        _staging = new ReferenceFrameEvaluation[system.Count];
        _stagingRoots = new FrameTransform[system.Count];
        _bodyStaging = new EvaluatedPlanetaryBody[BodyOrder.Length];
        _clock = new SimulationClock(initialTime, new SimulationTimeline(), SimulationSpeedPresets.Get(_rateStepIndex).Rate);
        DistantBodies = new NativePlanetaryPresentation[BodyOrder.Length];
        OrbitVertices = new NativeOrbitLineVertex[OrbitVertexCount];
        _orbitDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * InitialOverviewDistanceAu;
        _orbitPitchRadians = SolarMapPitchRadians;
        CameraPresentationMode = SolarCameraPresentationMode.SolarMap;
        StartupUtc = startupUtc;
    }

    internal PlanetaryPresentationSnapshot Presentation { get; private set; } = null!;
    internal NativePlanetaryPresentation[] DistantBodies { get; }
    internal NativeOrbitLineVertex[] OrbitVertices { get; }
    internal ReadOnlySpan<Double3> OrbitRootSamples => _rootOrbitSamples;
    internal int FocusIndex { get; private set; }
    internal int DistantBodyCount { get; private set; }
    internal PlanetaryRepresentationBlend FocusedBlend => _blend;
    internal bool DetailedComputeRequested => _blend.DrawDetailed && _eyeballWeight < 1f;
    internal bool EyeballComputeRequested => _blend.DrawDetailed && _eyeballWeight > 0f;
    internal float EyeballWeight => _eyeballWeight;
    internal PlanetRenderProxy FocusedBody => Presentation.Bodies[FocusIndex];
    internal SimulationInstant CurrentTime => _clock.CurrentTime;
    internal SimulationRate Rate => _clock.Rate;
    internal int SpeedPresetIndex => _rateStepIndex;
    internal string SpeedHudLabel => SimulationSpeedPresets.Get(_rateStepIndex).Label;
    internal bool SpeedHudVisible => _speedHudSecondsRemaining > 0d;
    internal float SpeedHudAlpha => !SpeedHudVisible ? 0f : (float)Math.Clamp(_speedHudSecondsRemaining / SpeedHudFadeSeconds, 0d, 1d);
    internal DateTimeOffset? StartupUtc { get; }
    internal bool IsPaused => _clock.IsPaused;
    internal double OrbitDistance => _orbitDistance;
    internal double OrbitYawRadians => _orbitYawRadians;
    internal double OrbitPitchRadians => _orbitPitchRadians;
    internal SolarCameraPresentationMode CameraPresentationMode { get; private set; }
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d, _surfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal?.05d:1e6d, SolAnalyticalDefinition.AstronomicalUnitMetres * MaximumOverviewDistanceAu);
    internal PlanetaryCameraPresentationMode SurfaceCameraMode=>_surfaceCameraMode;
    internal PlanetarySurfaceFocus? SurfaceFocus=>_surfaceFocus;
    internal ReadOnlySpan<ulong> VisibleLabelIds => _visibleLabelIds.AsSpan(0, VisibleLabelCount);
    internal ReadOnlySpan<byte> OrbitOpacityBytes => _orbitOpacityBytes;
    internal int VisibleLabelCount { get; private set; }
    internal int VisibleMarkerCount { get; private set; }
    internal int VisibleOrbitCount { get; private set; }

    internal static bool TryCreate(ReferenceFrameId root, out SolarSystemScene? scene, out string error)
        => TryCreate(root, TimeProvider.System, out scene, out error);

    internal static bool TryCreate(ReferenceFrameId root, TimeProvider utcTimeProvider, out SolarSystemScene? scene, out string error)
    {
        scene = null;
        if (utcTimeProvider is null) { error = "UTC time provider is required."; return false; }
        var utc = utcTimeProvider.GetUtcNow(); // The production fresh-start path samples the host clock exactly once.
        long unixTicks;
        try { unixTicks = checked(utc.UtcDateTime.Ticks - DateTime.UnixEpoch.Ticks); }
        catch (OverflowException) { error = "Current UTC is outside the supported timestamp range."; return false; }
        if (!SolarUtcTime.TryToSimulationInstant(new UtcInstant(unixTicks), out var initialTime))
        {
            error = "Current UTC could not be converted through the pinned Solar UTC time authority.";
            return false;
        }
        return TryCreateAt(root, initialTime, utc, out scene, out error);
    }

    internal static bool TryCreateAt(ReferenceFrameId root, SimulationInstant initialTime, out SolarSystemScene? scene, out string error)
        => TryCreateAt(root, initialTime, null, out scene, out error);

    private static bool TryCreateAt(ReferenceFrameId root, SimulationInstant initialTime, DateTimeOffset? startupUtc, out SolarSystemScene? scene, out string error)
    {
        scene = null;
        var system = SolAnalyticalDefinition.Instance;
        var traversalIndices = new int[BodyOrder.Length];
        for (var bodyIndex = 0; bodyIndex < BodyOrder.Length; bodyIndex++)
        {
            var id = new CelestialBodyId(BodyOrder[bodyIndex]);
            traversalIndices[bodyIndex] = FindTraversalIndex(system, id);
            if (traversalIndices[bodyIndex] < 0 || !system.TryGetBody(id, out _))
            {
                error = "SolAnalytical catalog or hierarchy body is missing.";
                return false;
            }
        }

        if (!TryBuildRootOrbitSamples(system, out var rootOrbitSamples, out var orbitTraversalIndices, out var orbitPeriods, out error)) return false;
        var candidate = new SolarSystemScene(system, root, traversalIndices, rootOrbitSamples, orbitTraversalIndices, orbitPeriods, initialTime, startupUtc);
        if (!candidate.TryPublishAt(initialTime, out error)) return false;
        scene = candidate;
        error = string.Empty;
        return true;
    }

    internal void Update(CameraState camera)
    {
        var selectedBlend = _handoff.Update(FocusedBody, camera.Position.Value);
        _blend = FocusIndex == 0
            ? new PlanetaryRepresentationBlend(PlanetaryRenderRegime.DistantOnly, selectedBlend.DistanceRadii, 1f, 0f)
            : selectedBlend;
        _eyeballWeight = 0f;
        if (FocusedBody.BodyId == SolarSystemBodyIds.Earth.Value && _blend.DrawDetailed)
        {
            var relative = FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value - FocusedBody.Position.Value);
            var distance = Math.Sqrt(relative.LengthSquared);
            var radial = distance > 0d ? relative / distance : Double3.UnitZ;
            var surfaceRadius = PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres, radial, EarthPlanetaryScene.Terrain, EarthPlanetaryScene.EnvironmentDefinition);
            _eyeballWeight = PlanetaryEyeballHandoff.EyeballWeight(distance - surfaceRadius);
        }
        SelectOverlayPresentation(camera);
        for (var output = 0; output < Presentation.Count; output++)
        {
            var index = output == 0 ? FocusIndex : output <= FocusIndex ? output - 1 : output;
            var body = Presentation.Bodies[index];
            var center = CubeSphereProjection.CameraRelativeCenter(body, new UniversePosition(camera.Position.Value, Presentation.RootFrame));
            var focused = index == FocusIndex;
            DistantBodies[output] = new NativePlanetaryPresentation
            {
                CenterX = (float)center.X, CenterY = (float)center.Y, CenterZ = (float)center.Z, Radius = (float)body.RadiusMetres,
                ColorR = body.Color.X, ColorG = body.Color.Y, ColorB = body.Color.Z,
                DistantAlpha = focused ? _blend.DistantAlpha : 1f,
                DetailedAlpha = focused ? _blend.DetailedAlpha : 0f,
                DistanceRadii = (float)(Math.Sqrt((camera.Position.Value - body.Position.Value).LengthSquared) / body.RadiusMetres),
                Regime = focused ? (NativePlanetaryRenderRegime)_blend.Regime : NativePlanetaryRenderRegime.DistantOnly,
                Enabled = (uint)(index + 1) | (index == 0 ? StellarPresentationBit : 0u) | (focused ? FocusedOverlayBit : 0u) |
                    (_visibleLabels[index] ? LabelVisibleBit : 0u) | (_visibleMarkers[index] ? MarkerVisibleBit : 0u) |
                    (index == 0 ? 0u : (uint)_orbitOpacityBytes[index - 1] << OrbitOpacityShift)
            };
            SolarPlanetMaterials.TryApply(ref DistantBodies[output], body.BodyId);
            SolarPlanetMaterials.ApplyBodyOrientation(ref DistantBodies[output],body.BodyFixedToRoot);
        }
        DistantBodyCount = Presentation.Count;
        UpdateOrbitVertices(camera);
    }

    internal bool Focus(CameraState camera, int index)
    {
        if ((uint)index >= Presentation.Count) return false;
        FocusIndex = index;
        _handoff = new PlanetaryRepresentationHandoff(EarthPlanetaryScene.HandoffConfiguration);
        _orbitDistance = FocusFramingDistance(FocusedBody);
        _surfaceFocus=null;_surfaceYawRadians=0d;_surfacePitchRadians=-Math.PI/12d;_surfaceCameraMode=PlanetaryCameraPresentationMode.Orbital;
        CameraPresentationMode = SolarCameraPresentationMode.Free3D;
        ApplyOrbitPose(camera);
        return true;
    }

    internal bool Focus(CameraState camera, NativePresentationFocus focus)
    {
        var value = (uint)focus;
        return value is >= 1 and <= 10 && Focus(camera, (int)value - 1);
    }

    internal void ApplyPresentationInput(CameraState camera, in NativeInputState input, out bool rateChanged, out bool pauseChanged)
    {
        AdvanceSpeedHud(input.DeltaSeconds);
        rateChanged = false;
        pauseChanged = false;
        var rateStep = (input.RateDecrease != 0) == (input.RateIncrease != 0) ? 0 : input.RateIncrease != 0 ? 1 : -1;
        var nextRateIndex = Math.Clamp(_rateStepIndex + rateStep, 0, SimulationSpeedPresets.Count - 1);
        if (rateStep != 0 && nextRateIndex != _rateStepIndex)
        {
            _rateStepIndex = nextRateIndex;
            rateChanged = _clock.TrySetRate(SimulationSpeedPresets.Get(_rateStepIndex).Rate);
            if (rateChanged) _speedHudSecondsRemaining = SpeedHudDisplaySeconds;
        }
        if (input.PauseToggle != 0)
        {
            if (_clock.IsPaused) _clock.Resume(); else _clock.Pause();
            pauseChanged = true;
        }

        var cameraChanged = false;
        if (input.MouseWheelDetents != 0)
        {
            var earthEnvironment=TryFocusedEnvironment(out var environment);var radial=CurrentSurfaceDirection();var surfaceRadius=earthEnvironment?PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres,radial,EarthPlanetaryScene.Terrain,environment):FocusedBody.RadiusMetres;var altitude=Math.Max(EarthPlanetaryScene.MinimumTerrainClearanceMetres,_orbitDistance-surfaceRadius);var factor=earthEnvironment?PlanetarySurfaceCameraPolicy.ZoomFactor(altitude):1.1d;
            _orbitDistance = Math.Clamp(
                surfaceRadius+altitude*Math.Pow(factor, -input.MouseWheelDetents),
                earthEnvironment?surfaceRadius+EarthPlanetaryScene.MinimumTerrainClearanceMetres:FocusedBody.RadiusMetres*1.05d,
                SolAnalyticalDefinition.AstronomicalUnitMetres * MaximumOverviewDistanceAu);
            cameraChanged = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            if(_surfaceCameraMode==PlanetaryCameraPresentationMode.Orbital){_orbitYawRadians-=input.MouseDeltaX*OrbitSensitivity;_orbitPitchRadians=Math.Clamp(_orbitPitchRadians-input.MouseDeltaY*OrbitSensitivity,-1.45d,1.45d);}
            else{_surfaceYawRadians-=input.MouseDeltaX*OrbitSensitivity;_surfacePitchRadians=Math.Clamp(_surfacePitchRadians-input.MouseDeltaY*OrbitSensitivity,PlanetarySurfaceCameraPolicy.MinimumPitchRadians,PlanetarySurfaceCameraPolicy.MaximumPitchRadians);}
            CameraPresentationMode = SolarCameraPresentationMode.Free3D;
            cameraChanged = true;
        }
        if(_surfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal&&_surfaceFocus is { } focus&&TryFocusedEnvironment(out var localEnvironment)&&
            (input.MoveForward!=input.MoveBackward||input.MoveRight!=input.MoveLeft))
        {
            var forwardAxis=(int)input.MoveForward-(int)input.MoveBackward;var rightAxis=(int)input.MoveRight-(int)input.MoveLeft;var length=Math.Sqrt(forwardAxis*forwardAxis+rightAxis*rightAxis);var frame=focus.TangentFrame;
            var forward=(frame.North*Math.Cos(_surfaceYawRadians)+frame.East*Math.Sin(_surfaceYawRadians)).Normalized();var right=(frame.East*Math.Cos(_surfaceYawRadians)-frame.North*Math.Sin(_surfaceYawRadians)).Normalized();var currentSurfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres,frame.Direction,EarthPlanetaryScene.Terrain,localEnvironment);var altitude=Math.Max(EarthPlanetaryScene.MinimumTerrainClearanceMetres,_orbitDistance-currentSurfaceRadius);var seconds=Math.Clamp((double)input.DeltaSeconds,0d,.1d);var travel=PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(altitude)*seconds;var tangent=(forward*forwardAxis+right*rightAxis)/length;var direction=(frame.Direction+tangent*(travel/currentSurfaceRadius)).Normalized();var nextSurfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres,direction,EarthPlanetaryScene.Terrain,localEnvironment);_surfaceFocus=PlanetarySurfaceFocus.AtDirection(FocusedBody.BodyId,direction,nextSurfaceRadius,altitude);_orbitDistance=nextSurfaceRadius+altitude;cameraChanged=true;
        }
        if (cameraChanged) ApplyOrbitPose(camera);
    }

    internal bool TryAdvanceByHostDuration(SimulationDuration hostDuration, CameraState camera, out string error)
    {
        var host = _clock.AdvanceByHostDuration(hostDuration);
        if (host.Reason is SimulationHostAdvanceStopReason.Paused or SimulationHostAdvanceStopReason.NoWork)
        {
            error = string.Empty;
            return true;
        }
        if (host.Reason != SimulationHostAdvanceStopReason.Accepted || !_clock.TryGetPendingSimulationDebtTarget(out var target))
        {
            error = $"Solar host-duration conversion failed: {host.Reason}.";
            return false;
        }
        var before = _clock.CurrentTime;
        var advance = _clock.AdvanceTo(target);
        if (advance.Reason != SimulationAdvanceStopReason.ReachedTarget)
        {
            error = $"Solar clock advance failed: {advance.Reason}.";
            return false;
        }
        _clock.ConsumePendingSimulationDebt(new SimulationDuration(_clock.CurrentTime.Ticks - before.Ticks));
        if (!TryPublishAt(_clock.CurrentTime, out error)) return false;
        ApplyOrbitPose(camera);
        return true;
    }

    internal void ResetPresentationCamera(CameraState camera)
    {
        FocusIndex = 0;
        _handoff = new PlanetaryRepresentationHandoff(EarthPlanetaryScene.HandoffConfiguration);
        _orbitDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * InitialOverviewDistanceAu;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = SolarMapPitchRadians;
        _surfaceFocus=null;_surfaceCameraMode=PlanetaryCameraPresentationMode.Orbital;
        CameraPresentationMode = SolarCameraPresentationMode.SolarMap;
        ApplyOrbitPose(camera);
    }

    internal double FocusFramingDistance(in PlanetRenderProxy body)
    {
        var visualRadius = body.RadiusMetres;
        if (body.BodyId == SolarSystemBodyIds.Sun.Value) visualRadius *= 1.6d;
        else if (SolarPlanetMaterials.Catalog.TryGet(body.BodyId, out var material) && material.Ring is { } ring) visualRadius = Math.Max(visualRadius, ring.OuterRadiusMetres);
        var projectionScale = 1d / Math.Tan(Projection.VerticalFieldOfViewRadians * .5d);
        return Math.Max(body.RadiusMetres * 4d, projectionScale * visualRadius / FocusTargetRadiusNdc);
    }

    internal NativePlanetaryPresentation FocusedPresentation(CameraState camera)
    {
        var body = FocusedBody;
        var center = CubeSphereProjection.CameraRelativeCenter(body, new UniversePosition(camera.Position.Value, Presentation.RootFrame));
        var native = new NativePlanetaryPresentation
        {
            CenterX = (float)center.X, CenterY = (float)center.Y, CenterZ = (float)center.Z, Radius = (float)body.RadiusMetres,
            ColorR = body.Color.X, ColorG = body.Color.Y, ColorB = body.Color.Z,
            DistantAlpha = _blend.DistantAlpha, DetailedAlpha = _blend.DetailedAlpha, DistanceRadii = (float)_blend.DistanceRadii,
            Regime = (NativePlanetaryRenderRegime)_blend.Regime, Enabled = 1
        };
        SolarPlanetMaterials.TryApply(ref native, body.BodyId);
        SolarPlanetMaterials.ApplyBodyOrientation(ref native,body.BodyFixedToRoot);
        return native;
    }

    internal NativePlanetaryGpuConstants GpuConstants(CameraState camera)
    {
        var body = FocusedBody;
        var rootToBody=body.BodyFixedToRoot.Conjugate().Normalized();var relative=rootToBody.Rotate(camera.Position.Value-body.Position.Value);var encoded=EncodedPosition.Encode(relative);var radiusHigh=(float)body.RadiusMetres;var radiusLow=(float)(body.RadiusMetres-radiusHigh);var terrain=body.BodyId==SolarSystemBodyIds.Earth.Value?EarthPlanetaryScene.Terrain:default;var viewForward=rootToBody.Rotate(camera.Orientation.Rotate(new Double3(0,0,-1))).Normalized();var tanY=Math.Tan(camera.Projection.VerticalFieldOfViewRadians*.5d);var halfAngle=Math.Atan(Math.Sqrt(tanY*tanY+tanY*tanY*camera.Projection.AspectRatio*camera.Projection.AspectRatio));
        return new NativePlanetaryGpuConstants
        {
            CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,
            CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,
            RefinementThreshold = (float)EarthPlanetaryScene.RegionalLodConfiguration.MaximumProjectedPatchSpan,
            NearFieldAltitudeRadii = (float)EarthPlanetaryScene.RegionalLodConfiguration.NearFieldAltitudeRadii,
            SurfaceAltitudeMetres=(float)(terrain.IsValid?Math.Max(EarthPlanetaryScene.MinimumTerrainClearanceMetres,Math.Sqrt(relative.LengthSquared)-PlanetaryTerrainQuery.SurfaceRadius(body.RadiusMetres,relative.Normalized(),terrain)):Math.Max(0d,Math.Sqrt(relative.LengthSquared)-body.RadiusMetres)),MaximumTerrainHeightMetres=(float)(terrain.IsValid?terrain.MaximumHeightMetres:0d),MaximumLevel = EarthPlanetaryScene.RegionalMaximumLod,
            OutputCapacity = EarthPlanetaryScene.MaximumPatchCapacity,TerrainVersion=terrain.Version,
            ViewForwardX=(float)viewForward.X,ViewForwardY=(float)viewForward.Y,ViewForwardZ=(float)viewForward.Z,ViewHalfAngleRadians=(float)halfAngle
        };
    }

    internal NativePlanetaryEyeball EyeballConstants(CameraState camera)
    {
        if (!EyeballComputeRequested || FocusedBody.BodyId != SolarSystemBodyIds.Earth.Value) return default;
        var body = FocusedBody;
        var rootToBody=body.BodyFixedToRoot.Conjugate().Normalized();var cameraBody=rootToBody.Rotate(camera.Position.Value-body.Position.Value);
        var encoded = EncodedPosition.Encode(cameraBody);
        var radiusHigh = (float)body.RadiusMetres;
        var radiusLow = (float)(body.RadiusMetres - radiusHigh);
        var viewForward=rootToBody.Rotate(camera.Orientation.Rotate(new Double3(0d,0d,-1d))).Normalized();
        var distance = Math.Sqrt(cameraBody.LengthSquared);
        var radial = distance > 0d ? cameraBody / distance : Double3.UnitZ;
        var altitude = distance - PlanetaryTerrainQuery.VisibleSurfaceRadius(body.RadiusMetres, radial, EarthPlanetaryScene.Terrain, EarthPlanetaryScene.EnvironmentDefinition);
        return new NativePlanetaryEyeball
        {
            CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,
            CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,
            SurfaceAltitudeMetres=(float)Math.Max(EarthPlanetaryScene.MinimumTerrainClearanceMetres,altitude),MaximumTerrainHeightMetres=(float)EarthPlanetaryScene.Terrain.MaximumHeightMetres,OceanSeaLevelMetres=(float)EarthPlanetaryScene.EnvironmentDefinition.OceanSeaLevelMetres,BlendAlpha=_eyeballWeight,
            BodyIdLow=(uint)body.BodyId,BodyIdHigh=(uint)(body.BodyId>>32),TerrainVersion=EarthPlanetaryScene.Terrain.Version,Enabled=1,
            ViewForwardX=(float)viewForward.X,ViewForwardY=(float)viewForward.Y,ViewForwardZ=(float)viewForward.Z,HorizonMarginRadians=(float)PlanetaryEyeballTopology.HorizonMarginRadians,
            RadialWarpExponent=(float)PlanetaryEyeballTopology.RadialWarpExponent,DetailFrequency=1f,NormalStepMetres=2f,RegionalAlpha=1f-_eyeballWeight,
            VertexCount=PlanetaryEyeballTopology.VertexCount,IndexCount=PlanetaryEyeballTopology.IndexCount,RadialRingCount=PlanetaryEyeballTopology.RadialRingCount,AzimuthSegmentCount=PlanetaryEyeballTopology.AzimuthSegmentCount
        };
    }

    internal NativeSolarLighting SolarLighting(CameraState camera)
    {
        var lighting = SolarLightingPresentation.CreateDefault(Presentation.Bodies[0].Position);
        if (!lighting.TryEncode(new UniversePosition(camera.Position.Value, Presentation.RootFrame), out var native))
            throw new InvalidOperationException("Solar lighting transport failed.");
        native.SpeedHud = SpeedHudPacked();
        return native;
    }

    internal NativePlanetaryEnvironment PlanetaryEnvironment(CameraState camera)
    {
        if(!TryFocusedEnvironment(out var environment))return default;
        return environment.Encode(FocusedBody,new UniversePosition(camera.Position.Value,Presentation.RootFrame));
    }

    internal bool TryGetLabelBounds(ulong bodyId, out SolarScreenRect bounds)
    {
        for (var index = 0; index < BodyOrder.Length; index++)
        {
            if (BodyOrder[index] != bodyId) continue;
            bounds = _labelBounds[index];
            return _labelBoundsValid[index];
        }
        bounds = default;
        return false;
    }

    private void SelectOverlayPresentation(CameraState camera)
    {
        Array.Clear(_visibleLabels);
        Array.Clear(_visibleMarkers);
        Array.Clear(_labelBoundsValid);
        VisibleLabelCount = 0;
        VisibleMarkerCount = 0;
        Span<double> apparentRadii = stackalloc double[BodyOrder.Length];
        Span<bool> projected = stackalloc bool[BodyOrder.Length];
        for (var index = 0; index < BodyOrder.Length; index++)
        {
            projected[index] = SolarOverlayLayout.TryProjectBody(Presentation.Bodies[index], camera, out var x, out var y, out apparentRadii[index], out _);
            if (!projected[index] || Math.Abs(x) > 1d - .015d || Math.Abs(y) > 1d - .015d) continue;
            var focused = index == FocusIndex;
            var related = IsDirectlyRelated(index, FocusIndex);
            var closeLocal = FocusIndex != 0 && _orbitDistance < SolAnalyticalDefinition.AstronomicalUnitMetres * .05d;
            var maximumRadius = index == 0 ? .0035d : focused ? .016d : .011d;
            if (apparentRadii[index] < maximumRadius && (!closeLocal || focused || related))
            {
                _visibleMarkers[index] = true;
                VisibleMarkerCount++;
            }
        }
        SelectOrbitOpacities();
        Span<int> priority = stackalloc int[BodyOrder.Length];
        var priorityCount = 0;
        AddPriority(priority, ref priorityCount, FocusIndex);
        AddRelatedPriorities(priority, ref priorityCount, FocusIndex);
        AddPriority(priority, ref priorityCount, 0); // Sun
        for (var index = 0; index < BodyOrder.Length; index++)
            if (_system.TryGetBody(new CelestialBodyId(BodyOrder[index]), out var body) && body.Identity.Classification == CelestialBodyClassification.Planet)
                AddPriority(priority, ref priorityCount, index);
        for (var index = 0; index < BodyOrder.Length; index++) AddPriority(priority, ref priorityCount, index); // Stable body-ID order.

        for (var priorityIndex = 0; priorityIndex < priorityCount; priorityIndex++)
        {
            var index = priority[priorityIndex];
            var focused = index == FocusIndex;
            var related = IsDirectlyRelated(index, FocusIndex);
            var closeLocal = FocusIndex != 0 && _orbitDistance < SolAnalyticalDefinition.AstronomicalUnitMetres * .05d;
            if (!projected[index] || apparentRadii[index] >= (focused ? .22d : .08d) || closeLocal && !focused && !related) continue;
            if (!SolarOverlayLayout.TryProjectLabel(Presentation.Bodies[index], camera, focused, out var bounds, out _)) continue;
            _labelBounds[index] = bounds;
            _labelBoundsValid[index] = true;
            var overlaps = false;
            for (var accepted = 0; accepted < VisibleLabelCount; accepted++)
            {
                var acceptedIndex = IndexOfBody(_visibleLabelIds[accepted]);
                if (acceptedIndex >= 0 && SolarOverlayLayout.Overlaps(bounds, _labelBounds[acceptedIndex])) { overlaps = true; break; }
            }
            if (overlaps) continue;
            _visibleLabels[index] = true;
            _visibleLabelIds[VisibleLabelCount++] = BodyOrder[index];
        }

    }

    private void SelectOrbitOpacities()
    {
        VisibleOrbitCount = 0;
        for (var path = 0; path < OrbitPathCount; path++)
        {
            var bodyIndex = path + 1;
            var isMoon = _system.TryGetBody(new CelestialBodyId(BodyOrder[bodyIndex]), out var orbitBody) &&
                orbitBody.Identity.Classification == CelestialBodyClassification.Moon;
            double opacity;
            if (CameraPresentationMode == SolarCameraPresentationMode.SolarMap)
            {
                opacity = isMoon ? .10d : .24d;
            }
            else if (FocusIndex == 0)
            {
                var bodyDistance = Math.Sqrt((Presentation.Bodies[bodyIndex].Position.Value - Presentation.Bodies[0].Position.Value).LengthSquared);
                var ratio = bodyDistance / Math.Max(_orbitDistance, 1d);
                var range = 1d - SmoothStep(.65d, 1.35d, ratio);
                opacity = (isMoon ? .035d : .04d) + range * (isMoon ? .08d : .22d);
                if (_orbitDistance < SolAnalyticalDefinition.AstronomicalUnitMetres * .2d) opacity *= SmoothStep(.03d, .2d, _orbitDistance / SolAnalyticalDefinition.AstronomicalUnitMetres);
            }
            else if (bodyIndex == FocusIndex)
            {
                opacity = .22d;
            }
            else if (IsDirectlyRelated(bodyIndex, FocusIndex))
            {
                opacity = .48d;
            }
            else
            {
                opacity = .035d * SmoothStep(.05d, 2d, _orbitDistance / SolAnalyticalDefinition.AstronomicalUnitMetres);
            }
            var encoded = (byte)Math.Clamp((int)Math.Round(opacity * byte.MaxValue, MidpointRounding.AwayFromZero), 0, byte.MaxValue);
            _orbitOpacityBytes[path] = encoded;
            if (encoded != 0) VisibleOrbitCount++;
        }
    }

    private void AddRelatedPriorities(Span<int> priority, ref int count, int focusIndex)
    {
        for (var index = 0; index < BodyOrder.Length; index++) if (IsDirectlyRelated(index, focusIndex)) AddPriority(priority, ref count, index);
    }

    private bool IsDirectlyRelated(int leftIndex, int rightIndex)
    {
        if ((uint)leftIndex >= BodyOrder.Length || (uint)rightIndex >= BodyOrder.Length || leftIndex == rightIndex) return false;
        if (!_system.TryGetBody(new CelestialBodyId(BodyOrder[leftIndex]), out var left) || !_system.TryGetBody(new CelestialBodyId(BodyOrder[rightIndex]), out var right)) return false;
        return left.Identity.ParentBody == right.Id || right.Identity.ParentBody == left.Id;
    }

    private static double SmoothStep(double minimum, double maximum, double value)
    {
        var t = Math.Clamp((value - minimum) / (maximum - minimum), 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    private void AdvanceSpeedHud(float wallSeconds)
    {
        if (_speedHudSecondsRemaining <= 0d || !float.IsFinite(wallSeconds) || wallSeconds <= 0f) return;
        _speedHudSecondsRemaining = Math.Max(0d, _speedHudSecondsRemaining - Math.Min((double)wallSeconds, 1d));
    }

    private uint SpeedHudPacked()
    {
        if (!SpeedHudVisible) return 0u;
        var alpha = (uint)Math.Clamp((int)Math.Round(SpeedHudAlpha * byte.MaxValue, MidpointRounding.AwayFromZero), 1, byte.MaxValue);
        return (uint)(_rateStepIndex + 1) | alpha << 8;
    }

    private static void AddPriority(Span<int> priority, ref int count, int index)
    {
        for (var existing = 0; existing < count; existing++) if (priority[existing] == index) return;
        priority[count++] = index;
    }

    private static int IndexOfBody(ulong bodyId)
    {
        for (var index = 0; index < BodyOrder.Length; index++) if (BodyOrder[index] == bodyId) return index;
        return -1;
    }

    private bool TryPublishAt(SimulationInstant time, out string error)
    {
        var result = CelestialSystemEvaluator.TryEvaluateSystem(_system, time, _evaluations, _roots, _staging, _stagingRoots);
        if (!result.Succeeded)
        {
            error = $"SolAnalytical evaluation failed at {time.Ticks}: {result.Status}.";
            return false;
        }
        for (var index = 0; index < BodyOrder.Length; index++)
        {
            var id = new CelestialBodyId(BodyOrder[index]);
            if (!_system.TryGetBody(id, out var catalog))
            {
                error = "SolAnalytical catalog body disappeared.";
                return false;
            }
            var orientation=DoubleQuaternion.Identity;
            if(id!=SolarSystemBodyIds.Sun){if(!CelestialBodyOrientationEvaluator.TryEvaluate(id,time,out var bodyOrientation)){error=$"Body orientation evaluation failed for {id.Value}.";return false;}orientation=bodyOrientation.BodyFixedToInertial;}
            _bodyStaging[index] = new EvaluatedPlanetaryBody(
                id.Value, new UniversePosition(_roots[_traversalIndices[index]].Translation, _root),
                catalog.PhysicalProperties.MeanRadius, Colors[index], catalog.Identity.DisplayName, true,orientation);
        }
        if (!TrySampleRootOrbits(time, _orbitSampleStaging, out error)) return false;
        if (!PlanetaryBodyPresentationProvider.TryCreateSnapshot(_bodyStaging, out var candidate) || candidate is null)
        {
            error = "Solar presentation publication failed.";
            return false;
        }
        _orbitSampleStaging.CopyTo(_rootOrbitSamples, 0);
        Presentation = candidate;
        error = string.Empty;
        return true;
    }

    private void ApplyOrbitPose(CameraState camera)
    {
        var yaw = DoubleQuaternion.FromAxisAngle(Double3.UnitY, _orbitYawRadians);
        var pitch = DoubleQuaternion.FromAxisAngle(Double3.UnitX, _orbitPitchRadians);
        var orientation = (yaw * pitch).Normalized();
        var rootRadial=-orientation.Rotate(new Double3(0d,0d,-1d));
        if(TryFocusedEnvironment(out var environment)){var bodyToRoot=FocusedBody.BodyFixedToRoot;var bodyRadial=bodyToRoot.Conjugate().Normalized().Rotate(rootRadial);var surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres,bodyRadial,EarthPlanetaryScene.Terrain,environment);_orbitDistance=Math.Max(_orbitDistance,surfaceRadius+EarthPlanetaryScene.MinimumTerrainClearanceMetres);var altitude=_orbitDistance-surfaceRadius;_surfaceCameraMode=PlanetarySurfaceCameraPolicy.Mode(altitude);if(_surfaceCameraMode!=PlanetaryCameraPresentationMode.Orbital&&_surfaceFocus is null)_surfaceFocus=PlanetarySurfaceFocus.AtDirection(FocusedBody.BodyId,bodyRadial,surfaceRadius,altitude);if(_surfaceCameraMode==PlanetaryCameraPresentationMode.Orbital)_surfaceFocus=null;bodyRadial=_surfaceFocus?.TangentFrame.Direction??bodyRadial;if(_surfaceCameraMode!=PlanetaryCameraPresentationMode.Orbital)rootRadial=bodyToRoot.Rotate(bodyRadial);surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres,bodyRadial,EarthPlanetaryScene.Terrain,environment);_orbitDistance=surfaceRadius+altitude;var localRoot=(bodyToRoot*PlanetarySurfaceFrame.AtDirection(bodyRadial).LookOrientation(_surfaceYawRadians,_surfacePitchRadians)).Normalized();camera.Orientation=Nlerp(orientation,localRoot,PlanetarySurfaceCameraPolicy.SurfaceBlend(altitude));CameraPresentationMode=_surfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal?SolarCameraPresentationMode.SurfaceLocal:SolarCameraPresentationMode.Free3D;}
        else{_surfaceFocus=null;_surfaceCameraMode=PlanetaryCameraPresentationMode.Orbital;camera.Orientation=orientation;}
        camera.Projection=Projection;
        camera.Position=camera.Position with{Value=FocusedBody.Position.Value+rootRadial*_orbitDistance};
        camera.Validate();
        Update(camera);
    }

    private bool TryFocusedEnvironment(out PlanetaryEnvironmentPresentation environment)=>SolarPlanetMaterials.Environments.TryGet(FocusedBody.BodyId,out environment);
    private Double3 CurrentSurfaceDirection(){if(_surfaceFocus is { } focus)return focus.TangentFrame.Direction;var yaw=DoubleQuaternion.FromAxisAngle(Double3.UnitY,_orbitYawRadians);var pitch=DoubleQuaternion.FromAxisAngle(Double3.UnitX,_orbitPitchRadians);var rootRadial=-(yaw*pitch).Normalized().Rotate(new Double3(0d,0d,-1d));return FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(rootRadial);}
    private static DoubleQuaternion Nlerp(in DoubleQuaternion from,in DoubleQuaternion to,double amount){var target=from.X*to.X+from.Y*to.Y+from.Z*to.Z+from.W*to.W<0?new DoubleQuaternion(-to.X,-to.Y,-to.Z,-to.W):to;return new DoubleQuaternion(from.X+(target.X-from.X)*amount,from.Y+(target.Y-from.Y)*amount,from.Z+(target.Z-from.Z)*amount,from.W+(target.W-from.W)*amount).Normalized();}

    private void UpdateOrbitVertices(CameraState camera)
    {
        var output = 0;
        for (var path = 0; path < OrbitPathCount; path++)
        for (var segment = 0; segment < OrbitSegmentCount; segment++)
        {
            var first = _rootOrbitSamples[path * OrbitSampleCount + segment] - camera.Position.Value;
            var second = _rootOrbitSamples[path * OrbitSampleCount + segment + 1] - camera.Position.Value;
            OrbitVertices[output++] = new NativeOrbitLineVertex { X = (float)first.X, Y = (float)first.Y, Z = (float)first.Z };
            OrbitVertices[output++] = new NativeOrbitLineVertex { X = (float)second.X, Y = (float)second.Y, Z = (float)second.Z };
        }
    }

    private static bool TryBuildRootOrbitSamples(CelestialSystemDefinition system, out Double3[] samples, out int[] traversal, out double[] periods, out string error)
    {
        samples = new Double3[OrbitPathCount * OrbitSampleCount];
        traversal = new int[OrbitPathCount];
        periods = new double[OrbitPathCount];
        for (var path = 0; path < OrbitPathCount; path++)
        {
            var bodyId = new CelestialBodyId(BodyOrder[path + 1]);
            var nodeIndex = FindTraversalIndex(system, bodyId);
            if (nodeIndex < 0)
            {
                error = "Solar orbit body missing from hierarchy.";
                return false;
            }
            var node = system.GetNodeInTraversalOrder(nodeIndex);
            if (node.TrajectoryModel != CelestialTrajectoryModel.AnalyticalKepler ||
                !system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var trajectory) ||
                !system.TryGetBody(trajectory.CentralBody, out var central) ||
                !TryGetPeriodSeconds(trajectory, central.PhysicalProperties.GravitationalParameter, out periods[path]))
            {
                error = "Solar orbit trajectory authority is unavailable.";
                return false;
            }
            traversal[path] = nodeIndex;
        }

        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        for (var sample = 0; sample < OrbitSampleCount; sample++)
        for (var path = 0; path < OrbitPathCount; path++)
        {
            SimulationInstant time;
            try { time = SimulationInstant.FromSecondsRounded(periods[path] * sample / OrbitSegmentCount); }
            catch (OverflowException) { error = "Solar orbit sample time overflow."; return false; }
            var result = CelestialSystemEvaluator.TryEvaluateSystem(system, time, evaluations, roots, staging, stagingRoots);
            if (!result.Succeeded) { error = $"Solar orbit evaluation failed: {result.Status}"; return false; }
            samples[path * OrbitSampleCount + sample] = roots[traversal[path]].Translation;
        }
        error = string.Empty;
        return true;
    }

    private bool TrySampleRootOrbits(SimulationInstant start, Span<Double3> destination, out string error)
    {
        if (destination.Length < OrbitPathCount * OrbitSampleCount) { error = "Solar orbit sample destination is too small."; return false; }
        for (var sample = 0; sample < OrbitSampleCount; sample++)
        for (var path = 0; path < OrbitPathCount; path++)
        {
            SimulationInstant time;
            try { time = start + SimulationDuration.FromSecondsRounded(_orbitPeriods[path] * sample / OrbitSegmentCount); }
            catch (OverflowException) { error = "Solar orbit sample time overflow."; return false; }
            var result = CelestialSystemEvaluator.TryEvaluateSystem(_system, time, _evaluations, _roots, _staging, _stagingRoots);
            if (!result.Succeeded) { error = $"Solar orbit evaluation failed: {result.Status}"; return false; }
            destination[path * OrbitSampleCount + sample] = _roots[_orbitTraversalIndices[path]].Translation;
        }
        error = string.Empty;
        return true;
    }

    private static int FindTraversalIndex(CelestialSystemDefinition system, CelestialBodyId id)
    {
        for (var index = 0; index < system.Count; index++) if (system.GetNodeInTraversalOrder(index).Id == id) return index;
        return -1;
    }

    private static bool TryGetPeriodSeconds(in TwoBodyTrajectory trajectory, double centralMu, out double period)
    {
        var state = trajectory.StateAtEpoch;
        var radius = Math.Sqrt(state.Position.LengthSquared);
        var alpha = 2d / radius - state.Velocity.LengthSquared / centralMu;
        var meanMotion = Math.Sqrt(centralMu) * alpha * Math.Sqrt(alpha);
        period = 2d * Math.PI / meanMotion;
        return state.IsFinite && double.IsFinite(centralMu) && centralMu > 0d && double.IsFinite(radius) && radius > 0d &&
               double.IsFinite(alpha) && alpha > 0d && double.IsFinite(period) && period > 0d;
    }
}
