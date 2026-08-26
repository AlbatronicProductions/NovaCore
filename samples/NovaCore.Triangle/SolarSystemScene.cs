using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

internal static class SolarOverlayLayout
{
    internal const double CharacterStrideNdc = .0103d;
    internal const double LabelGlyphWidthNdc = .009d;
    internal const double LabelGlyphHeightNdc = .021d;
    internal const double LabelOffsetXNdc = .009d;
    internal const double LabelOffsetYNdc = .009d;
    internal const double FocusedLabelOffsetYNdc = .013d;
    internal const double CollisionMarginNdc = .0035d;
    internal const double ScreenEdgeMarginNdc = .012d;

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
            minX + (length - 1) * CharacterStrideNdc + LabelGlyphWidthNdc,
            minY + LabelGlyphHeightNdc);
        return bounds.IsFinite && bounds.MinX >= -1d + ScreenEdgeMarginNdc && bounds.MaxX <= 1d - ScreenEdgeMarginNdc &&
            bounds.MinY >= -1d + ScreenEdgeMarginNdc && bounds.MaxY <= 1d - ScreenEdgeMarginNdc;
    }

    internal static bool TryProjectBody(in PlanetRenderProxy body, CameraState camera, out double anchorX, out double anchorY, out double apparentRadiusNdc, out double depth)
    {
        anchorX = anchorY = apparentRadiusNdc = depth = double.NaN;
        if (!CameraRelativeRenderPosition.TryCreate(body.Position.Value, camera.Position.Value, out var relative)) return false;
        var view = camera.Orientation.Conjugate().Normalized().Rotate(relative.Value);
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

internal static class SolarCameraZoomPolicy
{
    internal const double DistanceRatioPerDetent = 1.25d;
    internal const double DomainComparisonEpsilonMetres = .01d;

    internal static double ApplyAltitude(double altitude, double minimumAltitude, double maximumAltitude, int detents)
    {
        if (!double.IsFinite(altitude) || !double.IsFinite(minimumAltitude) || !double.IsFinite(maximumAltitude) ||
            altitude < 0d || minimumAltitude <= 0d || maximumAltitude < minimumAltitude) throw new ArgumentOutOfRangeException(nameof(altitude));
        if (detents == 0) return Math.Clamp(altitude, minimumAltitude, maximumAltitude);
        return Math.Clamp(Math.Max(minimumAltitude, altitude) * Math.Exp(-detents * Math.Log(DistanceRatioPerDetent)), minimumAltitude, maximumAltitude);
    }

    internal static double Apply(double distance, double surfaceRadius, double minimumDistance, double maximumDistance, int detents)
    {
        if (!double.IsFinite(distance) || !double.IsFinite(surfaceRadius) || !double.IsFinite(minimumDistance) ||
            !double.IsFinite(maximumDistance) || surfaceRadius < 0d || minimumDistance <= surfaceRadius || maximumDistance < minimumDistance)
            throw new ArgumentOutOfRangeException(nameof(distance));
        if (detents == 0) return Math.Clamp(distance, minimumDistance, maximumDistance);
        var minimumAltitude = minimumDistance - surfaceRadius;
        var maximumAltitude = maximumDistance - surfaceRadius;
        return surfaceRadius + ApplyAltitude(Math.Max(0d, distance - surfaceRadius), minimumAltitude, maximumAltitude, detents);
    }

    internal static double ApplyTargetRelative(double distance, double minimumDistance, double maximumDistance, int detents)
    {
        if (!double.IsFinite(distance) || !double.IsFinite(minimumDistance) || !double.IsFinite(maximumDistance) ||
            distance < 0d || minimumDistance <= 0d || maximumDistance < minimumDistance) throw new ArgumentOutOfRangeException(nameof(distance));
        return ApplyAltitude(distance, minimumDistance, maximumDistance, detents);
    }

    internal static double OffsetDistanceForSurfaceAltitude(
        in PlanetRenderProxy body,
        in Double3 focusRoot,
        in Double3 cameraOffsetDirectionRoot,
        in PlanetaryTerrainDefinition terrain,
        double desiredAltitude,
        double maximumDistance)
    {
        if (!focusRoot.IsFinite || !cameraOffsetDirectionRoot.IsFinite ||
            cameraOffsetDirectionRoot.LengthSquared <= 0d || !double.IsFinite(desiredAltitude) || desiredAltitude < 0d ||
            !double.IsFinite(maximumDistance) || maximumDistance <= 0d) throw new ArgumentOutOfRangeException();

        var direction = cameraOffsetDirectionRoot.Normalized();
        var maximumAltitude = MaximumSurfaceAltitude(body, focusRoot, direction, terrain, maximumDistance);
        if (!double.IsFinite(maximumAltitude) || maximumAltitude + DomainComparisonEpsilonMetres < desiredAltitude)
            throw new InvalidOperationException("The requested zoom altitude is outside the bounded camera domain.");
        if (desiredAltitude >= maximumAltitude) return maximumDistance;

        var low = 0d;
        var high = maximumDistance;
        for (var iteration = 0; iteration < 56; iteration++)
        {
            var middle = low + (high - low) * .5d;
            var altitude = SurfaceAnchorAcquisition.SurfaceAltitude(body, focusRoot + direction * middle, terrain);
            if (altitude >= desiredAltitude) high = middle; else low = middle;
        }
        return low + (high - low) * .5d;
    }

    internal static double MaximumSurfaceAltitude(
        in PlanetRenderProxy body,
        in Double3 cameraLineOriginRoot,
        in Double3 cameraOffsetDirectionRoot,
        in PlanetaryTerrainDefinition terrain,
        double maximumDistance)
    {
        if (!cameraLineOriginRoot.IsFinite || !cameraOffsetDirectionRoot.IsFinite ||
            cameraOffsetDirectionRoot.LengthSquared <= 0d || !double.IsFinite(maximumDistance) || maximumDistance <= 0d)
            throw new ArgumentOutOfRangeException();
        return SurfaceAnchorAcquisition.SurfaceAltitude(body,
            cameraLineOriginRoot + cameraOffsetDirectionRoot.Normalized() * maximumDistance, terrain);
    }
}

internal static class SurfaceVisualAimHandoffPolicy
{
    internal const double ReleaseStartRadians = .01d;
    internal const double ReleaseCompleteRadians = .0025d;

    internal static double RetainedWeight(double angularSeparationRadians)
    {
        if (!double.IsFinite(angularSeparationRadians) || angularSeparationRadians < 0d)
            throw new ArgumentOutOfRangeException(nameof(angularSeparationRadians));
        var amount = Math.Clamp((angularSeparationRadians - ReleaseCompleteRadians) /
            (ReleaseStartRadians - ReleaseCompleteRadians), 0d, 1d);
        return amount * amount * (3d - 2d * amount);
    }

    internal static double AngularSeparation(
        in PlanetRenderProxy body,
        in Double3 anchorRoot,
        in Double3 cameraOffsetDirectionRoot,
        double surfaceAltitudeMetres)
    {
        if (!anchorRoot.IsFinite || !cameraOffsetDirectionRoot.IsFinite || cameraOffsetDirectionRoot.LengthSquared <= 0d ||
            !double.IsFinite(surfaceAltitudeMetres) || surfaceAltitudeMetres < 0d) throw new ArgumentOutOfRangeException();
        var cameraRoot = body.Position.Value + cameraOffsetDirectionRoot.Normalized() *
            (body.RadiusMetres + surfaceAltitudeMetres);
        var toAnchor = (anchorRoot - cameraRoot).Normalized();
        var toCenter = (body.Position.Value - cameraRoot).Normalized();
        return Math.Acos(Math.Clamp(Double3.Dot(toAnchor, toCenter), -1d, 1d));
    }

    internal static Double3 CameraLineOrigin(
        in Double3 focusRoot,
        in Double3 visualAimRoot,
        in Double3 cameraOffsetDirectionRoot)
    {
        if (!focusRoot.IsFinite || !visualAimRoot.IsFinite || !cameraOffsetDirectionRoot.IsFinite ||
            cameraOffsetDirectionRoot.LengthSquared <= 0d) throw new ArgumentOutOfRangeException();
        // The positional focus remains the 3D-1 algebraic pivot, but a retained
        // visual aim owns the complete inertial camera line. This prevents a
        // rotating body-fixed focus from driving either lateral or radial camera motion.
        return visualAimRoot;
    }
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
    internal const int MoonOrbitPathIndex = 3;
    internal const int OrbitSampleCount = OrbitSegmentCount + 1;
    internal const int OrbitVertexCount = OrbitPathCount * OrbitSegmentCount * 2;
    private const int MoonPeriodicSubdivisionsPerControlSegment = 8;
    private const int MoonPeriodicDenseSampleCount = OrbitSegmentCount * MoonPeriodicSubdivisionsPerControlSegment;
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
    private readonly Double3[] _moonOrbitControlSamples;
    private readonly Double3[] _moonOrbitPeriodicControls;
    private readonly Double3[] _moonOrbitDenseSamples;
    private readonly double[] _moonOrbitCumulativeLengths;
    private readonly int[] _orbitTraversalIndices;
    private readonly int[] _orbitCenterTraversalIndices;
    private readonly double[] _orbitPeriods;
    private readonly SimulationClock _clock;
    private readonly bool[] _visibleLabels = new bool[BodyOrder.Length];
    private readonly bool[] _visibleMarkers = new bool[BodyOrder.Length];
    private readonly byte[] _orbitOpacityBytes = new byte[OrbitPathCount];
    private readonly bool[] _labelBoundsValid = new bool[BodyOrder.Length];
    private readonly SolarScreenRect[] _labelBounds = new SolarScreenRect[BodyOrder.Length];
    private readonly ulong[] _visibleLabelIds = new ulong[BodyOrder.Length];
    private readonly PlanetaryProductionPupilOrientation _productionPupilOrientation = new();
    private readonly PlanetaryProductionEyeballSelection _productionEyeballSelection = new();
    private PlanetaryRepresentationHandoff _handoff = new(EarthPlanetaryScene.HandoffConfiguration);
    private bool _eyeballCoversVisibleSurface = true;
    private PlanetaryRepresentationBlend _blend;
    private int _rateStepIndex = 1;
    private double _speedHudSecondsRemaining;
    private double _orbitDistance;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;
    private DoubleQuaternion? _inertialOrbitOrientationOverride;
    private Double3? _inertialOrbitOffsetDirectionOverride;
    private double _surfaceAnchorBlend;
    private SurfaceAnchorFocus? _retainedVisualAimAnchor;
    private Double3 _retainedVisualAimOffsetRoot;
    private double _retainedVisualAimWeight;
    private double _surfaceAltitudeMetres=double.PositiveInfinity;
    private PlanetaryCameraPresentationMode _surfaceCameraMode;
    private float _eyeballWeight;
    private PlanetaryProductionPupilCell _productionPupilCell;
    private double _moonOrbitEndpointMismatchMetres;
    private int _cameraClearanceCorrectionCount;
    private long _cameraClearanceTerrainQueries;
    private long _cameraClearanceIterationCount;
    private long _cameraClearanceIterationZeroCount;
    private long _cameraClearanceIterationOneCount;
    private long _cameraClearanceIterationTwoCount;
    private long _cameraClearanceIterationThreeCount;
    private int _cameraClearanceMaximumIterations;
    private double _cameraClearanceMaximumCorrectionMetres;
    private Double3 _publishedCameraRoot;
    private double _bodyLocalCameraAltitudeDemandMetres=double.NaN;
    private Double3 _bodyLocalCameraPlacementSeedRoot;
    private bool _bodyLocalCameraPlacementPending;
    private bool _bodyLocalCameraPlacementUseOrbitCandidate;
    private FocusTarget _focusTarget = FocusTarget.BodyCenter(BodyOrder[0]);
    private CameraReferenceAuthority _cameraReferenceAuthority;
    private SurfaceCameraState _surfaceCameraState;
    private Double3 _surfaceCameraPivotRoot;
    private bool _surfaceCameraToggleWasDown;
    private SurfaceCameraTransitionMetrics _surfaceCameraLastTransitionMetrics;
    private FloridaLaunchSite _floridaLaunchSite;

    private SolarSystemScene(
        CelestialSystemDefinition system,
        ReferenceFrameId root,
        int[] traversalIndices,
        Double3[] rootOrbitSamples,
        int[] orbitTraversalIndices,
        int[] orbitCenterTraversalIndices,
        double[] orbitPeriods,
        SimulationInstant initialTime,
        DateTimeOffset? startupUtc)
    {
        _system = system;
        _root = root;
        _traversalIndices = traversalIndices;
        _rootOrbitSamples = rootOrbitSamples;
        _orbitSampleStaging = new Double3[rootOrbitSamples.Length];
        _moonOrbitControlSamples = new Double3[OrbitSegmentCount];
        _moonOrbitPeriodicControls = new Double3[OrbitSegmentCount];
        _moonOrbitDenseSamples = new Double3[MoonPeriodicDenseSampleCount + 1];
        _moonOrbitCumulativeLengths = new double[MoonPeriodicDenseSampleCount + 1];
        BuildPeriodicMoonOrbit(_rootOrbitSamples);
        _orbitTraversalIndices = orbitTraversalIndices;
        _orbitCenterTraversalIndices = orbitCenterTraversalIndices;
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
    internal ReadOnlySpan<Double3> MoonOrbitControlSamples => _moonOrbitControlSamples;
    internal ReadOnlySpan<Double3> MoonOrbitPeriodicControlSamples => _moonOrbitPeriodicControls;
    internal int FocusIndex { get; private set; }
    internal int DistantBodyCount { get; private set; }
    internal PlanetaryRepresentationBlend FocusedBlend => _blend;
    private static readonly PlanetaryProductionSurfaceEligibility ProductionEarthSurface = new(
        SolarSystemBodyIds.Earth.Value, 6_371_008.8d,
        PlanetaryTerrainDefinition.EarthProductionCubeV5.SourceId,
        PlanetaryTerrainDefinition.EarthProductionCubeV5.Version,
        "earth_surface_v5.nccube");
    internal bool ProductionSurfaceEligible => ProductionEarthSurface.Supports(
        FocusedBody.BodyId, FocusedBody.RadiusMetres, PlanetaryTerrainDefinition.EarthProductionCubeV5);
    internal bool ProductionEarthFocused => ProductionSurfaceEligible;
    internal bool DetailedComputeRequested => ProductionSurfaceEligible;
    internal bool EyeballComputeRequested => ProductionSurfaceEligible && _eyeballWeight > 0f && _productionPupilCell.IsValid;
    internal float EyeballWeight => _eyeballWeight;
    internal bool EyeballCoversVisibleSurface => _eyeballCoversVisibleSurface;
    internal int ProductionEyeballTier => _productionEyeballSelection.Tier;
    internal PlanetaryProductionPupilCell ProductionPupilCell => _productionPupilCell;
    internal long ProductionPupilOrientationChanges => _productionPupilOrientation.Changes;
    internal long ProductionEyeballTierChanges => _productionEyeballSelection.TierChanges;
    internal PlanetRenderProxy FocusedBody => Presentation.Bodies[FocusIndex];
    internal FocusTarget CurrentFocusTarget => _focusTarget;
    internal Double3 CurrentFocusRoot => _cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative
        ? _surfaceCameraPivotRoot
        : _focusTarget.Kind==FocusTargetKind.SurfaceAnchor
        ? SurfaceFocusHandoffPolicy.BlendedRoot(FocusedBody.Position.Value,EvaluateSurfaceAnchorRoot(),_surfaceAnchorBlend)
        : FocusedBody.Position.Value;
    internal Double3 CurrentVisualAimRoot => _cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative
        ? _surfaceCameraPivotRoot
        : EvaluateRetainedVisualAimRoot(CurrentFocusRoot);
    internal SimulationInstant CurrentTime => _clock.CurrentTime;
    internal SimulationRate Rate => _clock.Rate;
    internal int SpeedPresetIndex => _rateStepIndex;
    internal string SpeedHudLabel => SimulationSpeedPresets.Get(_rateStepIndex).Label;
    internal bool SpeedHudVisible => _speedHudSecondsRemaining > 0d;
    internal float SpeedHudAlpha => !SpeedHudVisible ? 0f : (float)Math.Clamp(_speedHudSecondsRemaining / SpeedHudFadeSeconds, 0d, 1d);
    internal DateTimeOffset? StartupUtc { get; }
    internal bool IsPaused => _clock.IsPaused;
    internal double OrbitDistance => _orbitDistance;
    internal Double3 CurrentInertialCameraOffset
    {
        get=>_publishedCameraRoot-CurrentFocusRoot;
    }
    internal double OrbitYawRadians => _orbitYawRadians;
    internal double OrbitPitchRadians => _orbitPitchRadians;
    internal double MoonOrbitPeriodSeconds => _orbitPeriods[MoonOrbitPathIndex];
    internal double MoonOrbitEndpointMismatchMetres => _moonOrbitEndpointMismatchMetres;
    internal SolarCameraPresentationMode CameraPresentationMode { get; private set; }
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d,
        FocusIndex>0&&double.IsFinite(_surfaceAltitudeMetres)&&_surfaceAltitudeMetres>=0d?PlanetarySurfaceCameraPolicy.NearClipMetres(_surfaceAltitudeMetres):PlanetarySurfaceCameraPolicy.MaximumNearClipMetres,
        SolAnalyticalDefinition.AstronomicalUnitMetres * MaximumOverviewDistanceAu);
    internal PlanetaryCameraPresentationMode SurfaceCameraMode=>_surfaceCameraMode;
    internal SurfaceAnchorFocus? SurfaceFocus=>_focusTarget.Kind==FocusTargetKind.SurfaceAnchor?_focusTarget.SurfaceAnchor:null;
    internal double SurfaceAnchorBlend=>_surfaceAnchorBlend;
    internal bool HasRetainedVisualAim=>_retainedVisualAimAnchor.HasValue;
    internal double RetainedVisualAimWeight=>_retainedVisualAimWeight;
    internal double SurfaceAltitudeMetres=>_surfaceAltitudeMetres;
    internal int CameraClearanceCorrectionCount=>_cameraClearanceCorrectionCount;
    internal long CameraClearanceTerrainQueries=>_cameraClearanceTerrainQueries;
    internal long CameraClearanceIterationCount=>_cameraClearanceIterationCount;
    internal (long Zero,long One,long Two,long Three) CameraClearanceIterationDistribution=>(_cameraClearanceIterationZeroCount,_cameraClearanceIterationOneCount,_cameraClearanceIterationTwoCount,_cameraClearanceIterationThreeCount);
    internal int CameraClearanceMaximumIterations=>_cameraClearanceMaximumIterations;
    internal double CameraClearanceMaximumCorrectionMetres=>_cameraClearanceMaximumCorrectionMetres;
    internal double BodyLocalCameraAltitudeDemandMetres=>_bodyLocalCameraAltitudeDemandMetres;
    internal CameraReferenceAuthority CurrentCameraReferenceAuthority=>_cameraReferenceAuthority;
    internal SurfaceCameraState CurrentSurfaceCameraState=>_surfaceCameraState;
    internal SurfaceCameraTransitionMetrics SurfaceCameraLastTransitionMetrics=>_surfaceCameraLastTransitionMetrics;
    internal FloridaLaunchSite FloridaLaunchSite=>_floridaLaunchSite;
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
        if (!TryLoadEarthElevationOracle(out error)) return false;
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

        if (!TryBuildRootOrbitSamples(system, out var rootOrbitSamples, out var orbitTraversalIndices, out var orbitCenterTraversalIndices, out var orbitPeriods, out error)) return false;
        var candidate = new SolarSystemScene(system, root, traversalIndices, rootOrbitSamples, orbitTraversalIndices, orbitCenterTraversalIndices, orbitPeriods, initialTime, startupUtc);
        if (!candidate.TryPublishAt(initialTime, out error)) return false;
        if (!candidate.TryInitializeFloridaLaunchSite()) { error = "Florida launch-site physical anchor initialization failed."; return false; }
        scene = candidate;
        error = string.Empty;
        return true;
    }

    internal void Update(CameraState camera)
    {
        var selectedBlend = _handoff.Update(FocusedBody, camera.Position.Value);
        _blend = FocusIndex == 0 || !ProductionSurfaceEligible
            ? new PlanetaryRepresentationBlend(PlanetaryRenderRegime.DistantOnly, selectedBlend.DistanceRadii, 1f, 0f)
            : selectedBlend;
        _eyeballWeight = 0f;
        _eyeballCoversVisibleSurface = true;
        if (ProductionSurfaceEligible)
        {
            var rootToBody = FocusedBody.BodyFixedToRoot.Conjugate().Normalized();
            var relative = rootToBody.Rotate(camera.Position.Value - FocusedBody.Position.Value);
            var distance = Math.Sqrt(relative.LengthSquared);
            var radial = distance > 0d ? relative / distance : Double3.UnitZ;
            var surfaceRadius = PlanetaryTerrainQuery.VisibleSurfaceRadius(FocusedBody.RadiusMetres, radial, EarthPlanetaryScene.Terrain, EarthPlanetaryScene.EarthSeaLevelMetres);
            var altitude = Math.Max(EarthPlanetaryScene.MinimumTerrainClearanceMetres, distance - surfaceRadius);
            var tanHalfFov = Math.Tan(camera.Projection.VerticalFieldOfViewRadians * .5d);
            var projectedError = PlanetaryProductionEyeballSelection.ProjectedTerrainErrorPixels(
                altitude, EarthPlanetaryScene.ProofViewportHeightPixels, tanHalfFov, EarthPlanetaryScene.Terrain.MaximumHeightMetres);
            var tier = _productionEyeballSelection.UpdateTier(projectedError);
            // The persistent spherical billboard is centred on the body-fixed
            // sub-camera point.  View orientation is observation state only:
            // letting the forward-ray intersection select the pupil caused a
            // click-drag to re-triangulate stationary ground and, at the
            // physical floor, could place the coarse centre fan around the
            // camera even though the camera origin was terrain-safe.
            _productionPupilCell = _productionPupilOrientation.Update(radial, tier);
            _eyeballWeight = PlanetaryProductionEyeballSelection.OwnershipWeight(projectedError);
            _eyeballCoversVisibleSurface = PlanetaryProductionEyeballTopology.CoversVisibleSurface(relative, _productionPupilCell.BodyFixedDirection,
                FocusedBody.RadiusMetres, EarthPlanetaryScene.Terrain.MaximumHeightMetres);
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
                // Production terrain-v5 roots are synchronously resident before
                // the first submitted frame. A focused generic Earth sphere is
                // therefore never a production fallback owner.
                DistantAlpha = focused ? ProductionSurfaceEligible ? 0f : _blend.DistantAlpha : 1f,
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
        _cameraReferenceAuthority=CameraReferenceAuthority.Inertial;_surfaceCameraState=default;_surfaceCameraPivotRoot=default;_inertialOrbitOrientationOverride=null;_inertialOrbitOffsetDirectionOverride=null;
        _focusTarget = FocusTarget.BodyCenter(Presentation.Bodies[index].BodyId);
        _handoff = new PlanetaryRepresentationHandoff(EarthPlanetaryScene.HandoffConfiguration);
        _orbitDistance = FocusFramingDistance(FocusedBody);
        _surfaceAnchorBlend=0d;_retainedVisualAimAnchor=null;_retainedVisualAimOffsetRoot=Double3.Zero;_retainedVisualAimWeight=0d;
        _surfaceAltitudeMetres=double.PositiveInfinity;_surfaceCameraMode=PlanetaryCameraPresentationMode.Orbital;_bodyLocalCameraAltitudeDemandMetres=double.NaN;_bodyLocalCameraPlacementPending=false;_bodyLocalCameraPlacementUseOrbitCandidate=false;
        _productionPupilOrientation.Reset();_productionEyeballSelection.Reset();_productionPupilCell=default;
        CameraPresentationMode = SolarCameraPresentationMode.Free3D;
        ApplyOrbitPose(camera,true);
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

        var surfaceToggleDown=input.MoveUp!=0;
        if(surfaceToggleDown&&!_surfaceCameraToggleWasDown)
        {
            if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)DetachSurfaceCamera(camera);
            else TryAttachSurfaceCamera(camera);
        }
        _surfaceCameraToggleWasDown=surfaceToggleDown;
        if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)
        {
            ApplySurfaceCameraInput(camera,input);
            return;
        }

        var cameraChanged = false;
        if (input.MouseWheelDetents != 0)
        {
            var terrain=FocusedTerrain;
            var maximumDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*MaximumOverviewDistanceAu;
            var rootRadial=OrbitOffsetDirection();
            var currentAltitude=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,camera.Position.Value,terrain);
            var desiredFocusRoot=FocusedBody.Position.Value;
            double desiredAltitude;
            if(_focusTarget.Kind==FocusTargetKind.SurfaceAnchor)
            {
                RetainActiveSurfaceAim();
                var activeVisualAimRoot=EvaluateRetainedVisualAimRoot(desiredFocusRoot);
                var maximumAltitude=SolarCameraZoomPolicy.MaximumSurfaceAltitude(FocusedBody,activeVisualAimRoot,rootRadial,terrain,maximumDistance);
                desiredAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,maximumAltitude,input.MouseWheelDetents);
                var desiredBlend=SurfaceFocusHandoffPolicy.SurfaceBlend(desiredAltitude);
                desiredFocusRoot=SurfaceFocusHandoffPolicy.BlendedRoot(FocusedBody.Position.Value,EvaluateSurfaceAnchorRoot(),desiredBlend);
            }
            else
            {
                var activeVisualAimRoot=EvaluateRetainedVisualAimRoot(desiredFocusRoot);
                var activeLineMaximumAltitude=SolarCameraZoomPolicy.MaximumSurfaceAltitude(FocusedBody,activeVisualAimRoot,rootRadial,terrain,maximumDistance);
                desiredAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,activeLineMaximumAltitude,input.MouseWheelDetents);
                UpdateRetainedVisualAim(desiredAltitude,rootRadial);
                var updatedVisualAimRoot=EvaluateRetainedVisualAimRoot(desiredFocusRoot);
                if(updatedVisualAimRoot!=activeVisualAimRoot)
                {
                    var updatedLineMaximumAltitude=SolarCameraZoomPolicy.MaximumSurfaceAltitude(FocusedBody,updatedVisualAimRoot,rootRadial,terrain,maximumDistance);
                    desiredAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,updatedLineMaximumAltitude,input.MouseWheelDetents);
                }
            }
            var desiredVisualAimRoot=EvaluateRetainedVisualAimRoot(desiredFocusRoot);
            var cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(desiredFocusRoot,desiredVisualAimRoot,rootRadial);
            var finalLineMaximumAltitude=SolarCameraZoomPolicy.MaximumSurfaceAltitude(FocusedBody,cameraLineOrigin,rootRadial,terrain,maximumDistance);
            if(desiredAltitude>finalLineMaximumAltitude)
                desiredAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,finalLineMaximumAltitude,input.MouseWheelDetents);
            var bodyToCamera=camera.Position.Value-FocusedBody.Position.Value;
            var cameraLineFacesOutward=bodyToCamera.LengthSquared>0d&&Double3.Dot(bodyToCamera.Normalized(),rootRadial)>0d;
            if(FocusedBodyHasNavigableSolidSurface&&
               _focusTarget.Kind==FocusTargetKind.SurfaceAnchor&&
               _surfaceAnchorBlend>=1d&&
               (!cameraLineFacesOutward||double.IsFinite(_bodyLocalCameraAltitudeDemandMetres))&&
               desiredAltitude<=PlanetarySurfaceCameraPolicy.SurfaceLocalAltitudeMetres)
            {
                _bodyLocalCameraAltitudeDemandMetres=desiredAltitude;
                _bodyLocalCameraPlacementSeedRoot=camera.Position.Value;
                _bodyLocalCameraPlacementPending=true;
            }
            else
            {
                _bodyLocalCameraAltitudeDemandMetres=double.NaN;
                _bodyLocalCameraPlacementPending=false;
                _orbitDistance=SolarCameraZoomPolicy.OffsetDistanceForSurfaceAltitude(FocusedBody,cameraLineOrigin,rootRadial,terrain,desiredAltitude,maximumDistance);
            }
            cameraChanged = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            ApplyInertialLook(input.MouseDeltaX,input.MouseDeltaY);
            _bodyLocalCameraPlacementUseOrbitCandidate=true;
            CameraPresentationMode = SolarCameraPresentationMode.Free3D;
            cameraChanged = true;
        }
        if(_surfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal&&_focusTarget.Kind==FocusTargetKind.SurfaceAnchor&&ProductionSurfaceEligible&&
            (input.MoveForward!=input.MoveBackward||input.MoveRight!=input.MoveLeft))
        {
            var anchor=_focusTarget.SurfaceAnchor;var frame=anchor.LocalTangentBasis;var forwardAxis=(int)input.MoveForward-(int)input.MoveBackward;var rightAxis=(int)input.MoveRight-(int)input.MoveLeft;var length=Math.Sqrt(forwardAxis*forwardAxis+rightAxis*rightAxis);
            var seconds=Math.Clamp((double)input.DeltaSeconds,0d,.1d);var travel=PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(Math.Max(0d,_surfaceAltitudeMetres))*seconds;var tangent=(frame.North*forwardAxis+frame.East*rightAxis)/length;var direction=(anchor.BodyFixedDirection+tangent*(travel/Math.Sqrt(anchor.BodyLocalPosition.LengthSquared))).Normalized();var terrain=FocusedTerrain;var elevation=terrain.IsValid?terrain.SampleHeight(direction,24):0d;_focusTarget=FocusTarget.AtSurface(SurfaceAnchorFocus.AtDirection(FocusedBody.BodyId,direction,FocusedBody.RadiusMetres,elevation));cameraChanged=true;
        }
        if (cameraChanged) ApplyOrbitPose(camera,true);
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
        _cameraReferenceAuthority=CameraReferenceAuthority.Inertial;_surfaceCameraState=default;_surfaceCameraPivotRoot=default;_surfaceCameraToggleWasDown=false;_inertialOrbitOrientationOverride=null;_inertialOrbitOffsetDirectionOverride=null;
        _focusTarget = FocusTarget.BodyCenter(Presentation.Bodies[0].BodyId);
        _handoff = new PlanetaryRepresentationHandoff(EarthPlanetaryScene.HandoffConfiguration);
        _orbitDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * InitialOverviewDistanceAu;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = SolarMapPitchRadians;
        _surfaceAnchorBlend=0d;_retainedVisualAimAnchor=null;_retainedVisualAimOffsetRoot=Double3.Zero;_retainedVisualAimWeight=0d;
        _surfaceAltitudeMetres=double.PositiveInfinity;_surfaceCameraMode=PlanetaryCameraPresentationMode.Orbital;
        _bodyLocalCameraAltitudeDemandMetres=double.NaN;_bodyLocalCameraPlacementPending=false;_bodyLocalCameraPlacementUseOrbitCandidate=false;
        _productionPupilOrientation.Reset();_productionEyeballSelection.Reset();_productionPupilCell=default;
        CameraPresentationMode = SolarCameraPresentationMode.SolarMap;
        ApplyOrbitPose(camera,true);
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
        if(ProductionEarthFocused){native.DistantAlpha=0f;native.DetailedAlpha=1f;native.Regime=NativePlanetaryRenderRegime.DetailedOnly;}
        SolarPlanetMaterials.TryApply(ref native, body.BodyId);
        SolarPlanetMaterials.ApplyBodyOrientation(ref native,body.BodyFixedToRoot);
        return native;
    }

    internal NativePlanetaryGpuConstants GpuConstants(CameraState camera)
    {
        var body = FocusedBody;
        var rootToBody=body.BodyFixedToRoot.Conjugate().Normalized();var relative=rootToBody.Rotate(camera.Position.Value-body.Position.Value);var encoded=EncodedPosition.Encode(relative);var radiusHigh=(float)body.RadiusMetres;var radiusLow=(float)(body.RadiusMetres-radiusHigh);var terrain=ProductionSurfaceEligible?PlanetaryTerrainDefinition.EarthProductionCubeV5:default;var viewForward=rootToBody.Rotate(camera.Orientation.Rotate(new Double3(0,0,-1))).Normalized();var tanY=Math.Tan(camera.Projection.VerticalFieldOfViewRadians*.5d);var halfAngle=Math.Atan(Math.Sqrt(tanY*tanY+tanY*tanY*camera.Projection.AspectRatio*camera.Projection.AspectRatio));
        return new NativePlanetaryGpuConstants
        {
            CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,
            CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,
            RefinementThreshold = (float)EarthPlanetaryScene.RegionalLodConfiguration.MaximumProjectedPatchSpan,
            NearFieldAltitudeRadii = ProductionSurfaceEligible?float.MaxValue:1f,
            SurfaceAltitudeMetres=(float)(terrain.IsValid?Math.Sqrt(relative.LengthSquared)-PlanetaryTerrainQuery.SurfaceRadius(body.RadiusMetres,relative.Normalized(),terrain):Math.Sqrt(relative.LengthSquared)-body.RadiusMetres),MaximumTerrainHeightMetres=(float)(terrain.IsValid?terrain.MaximumHeightMetres:0d),MaximumLevel = ProductionSurfaceEligible?EarthPlanetaryScene.RegionalMaximumLod:0u,
            OutputCapacity = ProductionSurfaceEligible?EarthPlanetaryScene.MaximumPatchCapacity:6u,TerrainVersion=terrain.Version,
            ViewForwardX=(float)viewForward.X,ViewForwardY=(float)viewForward.Y,ViewForwardZ=(float)viewForward.Z,ViewHalfAngleRadians=(float)halfAngle,
            ViewportHeightPixels=(float)EarthPlanetaryScene.ProofViewportHeightPixels,VerticalTanHalfFov=(float)tanY,TargetTexelPixels=PlanetaryProductionSamplingPolicy.TargetTexelPixels,RequestedAlbedoLevel=1f
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
        var tangentAnchor=_productionPupilCell.BodyFixedDirection;
        var distance = Math.Sqrt(cameraBody.LengthSquared);
        var radial = distance > 0d ? cameraBody / distance : Double3.UnitZ;
        var altitude = distance - PlanetaryTerrainQuery.VisibleSurfaceRadius(body.RadiusMetres, radial, EarthPlanetaryScene.Terrain, EarthPlanetaryScene.EarthSeaLevelMetres);
        var tier=PlanetaryProductionEyeballTopology.Tier(_productionEyeballSelection.Tier);
        return new NativePlanetaryEyeball
        {
            CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,
            CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,
            SurfaceAltitudeMetres=(float)altitude,MaximumTerrainHeightMetres=(float)EarthPlanetaryScene.Terrain.MaximumHeightMetres,OceanSeaLevelMetres=(float)EarthPlanetaryScene.EarthSeaLevelMetres,BlendAlpha=_eyeballWeight,
            BodyIdLow=(uint)body.BodyId,BodyIdHigh=(uint)(body.BodyId>>32),TerrainVersion=PlanetaryTerrainDefinition.EarthProductionCubeV5.Version,Enabled=1,
            TangentAnchorX=(float)tangentAnchor.X,TangentAnchorY=(float)tangentAnchor.Y,TangentAnchorZ=(float)tangentAnchor.Z,MaximumAngleRadians=(float)PlanetaryProductionEyeballTopology.MaximumAngleRadians,
            RadialWarpExponent=(float)tier.RadialWarpExponent,DetailFrequency=1f,NormalStepMetres=2f,RegionalAlpha=1f,
            VertexCount=(uint)tier.VertexCount,IndexCount=(uint)tier.IndexCount,RadialRingCount=(uint)tier.RadialRings,AzimuthSegmentCount=(uint)tier.AzimuthSegments,
            Reserved0=(uint)tier.Index,Reserved1=(uint)_productionPupilCell.Face,Reserved2=(uint)_productionPupilCell.X,Reserved3=(uint)_productionPupilCell.Y
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

    private bool TryInitializeFloridaLaunchSite()
    {
        if(!Presentation.TryGetBody(SolarSystemBodyIds.Earth.Value,out var earth))return false;
        return FloridaLaunchSite.TryCreate(earth.BodyId,earth.RadiusMetres,
            PlanetaryTerrainDefinition.EarthProductionCubeV5,out _floridaLaunchSite);
    }

    internal bool TryEvaluateFloridaLaunchSite(out AnchoredSurfaceObjectPose pose)
    {
        pose=default;
        if(!_floridaLaunchSite.IsValid||!Presentation.TryGetBody(_floridaLaunchSite.Object.Anchor.BodyId,out var earth))return false;
        var terrain=new PlanetaryPhysicalTerrainAuthority(earth.BodyId,PlanetaryTerrainDefinition.EarthProductionCubeV5);
        var body=new SurfaceBodyReference(earth.BodyId,earth.RadiusMetres,new ReferenceFrameId(checked((long)earth.BodyId)));
        var bodyToRoot=new FrameTransform(earth.Position.Value,earth.BodyFixedToRoot);
        return AnchoredSurfaceObjectEvaluator.TryEvaluate(_floridaLaunchSite.Object,body,terrain,bodyToRoot,
            Presentation.RootFrame,out pose)==SurfaceAnchorEvaluationStatus.Success;
    }

    internal bool TryGetFloridaLaunchSitePresentation(CameraState camera,out UniversePosition position,out DoubleQuaternion orientation)
    {
        position=default;orientation=default;
        if(!TryEvaluateFloridaLaunchSite(out var pose))return false;
        var relative=pose.RootPosition.Value-camera.Position.Value;
        var distanceSquared=relative.LengthSquared;
        if(!double.IsFinite(distanceSquared)||distanceSquared>FloridaLaunchSite.MaximumRenderDistanceMetres*FloridaLaunchSite.MaximumRenderDistanceMetres)return false;
        var view=camera.Orientation.Conjugate().Normalized().Rotate(relative);
        if(-view.Z<camera.Projection.NearClip)return false;
        var projectedPixels=FloridaLaunchSite.PlatformEastWidthMetres*(-view.Z>0d?1d/-view.Z:0d)*
            1080d/(2d*Math.Tan(camera.Projection.VerticalFieldOfViewRadians*.5d));
        if(!double.IsFinite(projectedPixels)||projectedPixels<.75d)return false;
        position=pose.RootPosition;orientation=pose.RootOrientation;return true;
    }

    internal bool TryStartAtFloridaLaunchSite(CameraState camera)
    {
        var earthIndex=-1;for(var index=0;index<Presentation.Count;index++)if(Presentation.Bodies[index].BodyId==SolarSystemBodyIds.Earth.Value){earthIndex=index;break;}
        if(earthIndex<0||!Focus(camera,earthIndex)||!_floridaLaunchSite.IsValid||
            !SurfaceEnuFrame.TryCreate(_floridaLaunchSite.Object.Anchor,out _))return false;
        var eye=new Double3(115d,-135d,72d);var pivot=new Double3(0d,0d,4d);var view=pivot-eye;
        var horizontal=Math.Sqrt(view.X*view.X+view.Y*view.Y);
        var yaw=Math.Atan2(view.X,view.Y);var pitch=Math.Atan2(view.Z,horizontal);
        if(!SurfaceCameraState.TryCreateFreeLook(_floridaLaunchSite.Object.Anchor,eye,pivot,yaw,pitch,out _surfaceCameraState))return false;
        _cameraReferenceAuthority=CameraReferenceAuthority.SurfaceRelative;
        _inertialOrbitOrientationOverride=null;_inertialOrbitOffsetDirectionOverride=null;
        ApplySurfaceCameraPose(camera);
        return true;
    }

    internal bool TryAttachSurfaceCamera(CameraState camera)
    {
        if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)return true;
        var terrain=FocusedTerrain;
        var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(FocusedBody.BodyId,terrain);
        if(!physicalTerrain.IsValid||!physicalTerrain.SupportsBody(FocusedBody.BodyId))return false;
        var pivotRoot=CurrentVisualAimRoot;
        if(!SurfaceCameraAuthority.TryAttach(FocusedBody,
            new UniversePosition(camera.Position.Value,Presentation.RootFrame),
            new UniversePosition(pivotRoot,Presentation.RootFrame),camera.Orientation,physicalTerrain,
            out var state,out _,out var metrics))return false;
        _surfaceCameraState=state;
        _surfaceCameraLastTransitionMetrics=metrics;
        _cameraReferenceAuthority=CameraReferenceAuthority.SurfaceRelative;
        _inertialOrbitOrientationOverride=null;
        _inertialOrbitOffsetDirectionOverride=null;
        ApplySurfaceCameraPose(camera);
        return true;
    }

    internal bool DetachSurfaceCamera(CameraState camera)
    {
        if(_cameraReferenceAuthority!=CameraReferenceAuthority.SurfaceRelative)return true;
        var terrain=FocusedTerrain;
        var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(FocusedBody.BodyId,terrain);
        if(!SurfaceCameraAuthority.TryEvaluate(FocusedBody,_surfaceCameraState,physicalTerrain,out var pose))return false;
        var offset=pose.RootEye.Value-pose.RootPivot.Value;
        if(!offset.IsFinite||offset.LengthSquared<=0d)return false;
        var rootRadial=offset.Normalized();
        _orbitDistance=Math.Sqrt(offset.LengthSquared);
        _orbitYawRadians=Math.Atan2(rootRadial.X,rootRadial.Z);
        _orbitPitchRadians=-Math.Asin(Math.Clamp(rootRadial.Y,-1d,1d));
        _inertialOrbitOrientationOverride=pose.RootOrientation;
        _inertialOrbitOffsetDirectionOverride=rootRadial;
        var anchorFocus=SurfaceAnchorFocus.AtDirection(FocusedBody.BodyId,
            _surfaceCameraState.Anchor.NormalizedBodyFixedDirection,FocusedBody.RadiusMetres,
            pose.PhysicalTerrainHeightMetres);
        _focusTarget=FocusTarget.AtSurface(anchorFocus);
        _surfaceAnchorBlend=1d;
        _retainedVisualAimAnchor=anchorFocus;
        _retainedVisualAimOffsetRoot=pose.RootPivot.Value-FocusedBody.Position.Value;
        _retainedVisualAimWeight=1d;
        _cameraReferenceAuthority=CameraReferenceAuthority.Inertial;
        _surfaceCameraState=default;
        _surfaceCameraPivotRoot=default;
        ApplyOrbitPose(camera);
        _surfaceCameraLastTransitionMetrics=new(
            Math.Sqrt((camera.Position.Value-pose.RootEye.Value).LengthSquared),0d,
            SurfaceCameraAuthority.QuaternionAngularError(camera.Orientation,pose.RootOrientation));
        return _surfaceCameraLastTransitionMetrics.IsFinite;
    }

    private void ApplySurfaceCameraInput(CameraState camera,in NativeInputState input)
    {
        if(_cameraReferenceAuthority!=CameraReferenceAuthority.SurfaceRelative)return;
        var terrain=FocusedTerrain;
        var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(FocusedBody.BodyId,terrain);
        if(!SurfaceCameraAuthority.TryEvaluate(FocusedBody,_surfaceCameraState,physicalTerrain,out var pose)||
            !SurfaceEnuFrame.TryCreate(_surfaceCameraState.Anchor,out var enu))
            throw new InvalidOperationException("The retained surface camera state cannot be evaluated.");
        var changed=false;
        if(input.LookActive!=0&&(input.MouseDeltaX!=0f||input.MouseDeltaY!=0f))
        {
            var yaw=SurfaceCameraState.NormalizeYaw(_surfaceCameraState.LocalYawRadians-input.MouseDeltaX*OrbitSensitivity);
            var pitch=PlanetarySurfaceCameraPolicy.ApplyPitchDelta(_surfaceCameraState.LocalPitchRadians,
                -input.MouseDeltaY*OrbitSensitivity);
            if(!SurfaceCameraState.TryCreateFreeLook(_surfaceCameraState.Anchor,
                _surfaceCameraState.EyeOffsetEnuMetres,_surfaceCameraState.PivotOffsetEnuMetres,
                yaw,pitch,out _surfaceCameraState))
                throw new InvalidOperationException("Surface camera free-look input produced an invalid state.");
            changed=true;
        }
        if(input.MouseWheelDetents!=0)
        {
            if(!SurfaceCameraAuthority.TryEvaluate(FocusedBody,_surfaceCameraState,physicalTerrain,out pose)||
                !SurfaceEnuFrame.TryCreate(_surfaceCameraState.Anchor,out enu))
                throw new InvalidOperationException("Surface camera zoom cannot evaluate the retained state.");
            var eyeFromPivot=pose.BodyFixedEye-pose.BodyFixedPivot;
            var distance=Math.Sqrt(eyeFromPivot.LengthSquared);
            if(!double.IsFinite(distance)||distance<=0d)throw new InvalidOperationException("Surface camera eye and pivot are degenerate.");
            var maximum=SolAnalyticalDefinition.AstronomicalUnitMetres*MaximumOverviewDistanceAu;
            var nextDistance=Math.Clamp(distance*Math.Exp(-Math.Clamp(input.MouseWheelDetents,-100,100)*.12d),1d,maximum);
            var nextEye=pose.BodyFixedPivot+eyeFromPivot*(nextDistance/distance);
            var anchorBody=pose.BodyFixedEye-SurfaceCameraAuthority.FromEnu(_surfaceCameraState.EyeOffsetEnuMetres,enu);
            if(!SurfaceCameraState.TryCreate(_surfaceCameraState.Anchor,
                SurfaceCameraAuthority.ToEnu(nextEye-anchorBody,enu),_surfaceCameraState.PivotOffsetEnuMetres,
                _surfaceCameraState.BodyFixedOrientation,out _surfaceCameraState))
                throw new InvalidOperationException("Surface camera zoom input produced an invalid state.");
            changed=true;
        }
        if((input.MoveForward!=input.MoveBackward||input.MoveRight!=input.MoveLeft)&&
            physicalTerrain.SupportsBody(FocusedBody.BodyId))
        {
            var forwardAxis=(int)input.MoveForward-(int)input.MoveBackward;
            var rightAxis=(int)input.MoveRight-(int)input.MoveLeft;
            var axisLength=Math.Sqrt(forwardAxis*forwardAxis+rightAxis*rightAxis);
            var seconds=Math.Clamp((double)input.DeltaSeconds,0d,.1d);
            var travel=PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(
                Math.Max(0d,_surfaceAltitudeMetres),input.FastModifier!=0,input.SlowModifier!=0)*seconds;
            var yaw=_surfaceCameraState.LocalYawRadians;
            var forwardHorizontal=enu.North*Math.Cos(yaw)+enu.East*Math.Sin(yaw);
            var rightHorizontal=enu.East*Math.Cos(yaw)-enu.North*Math.Sin(yaw);
            var tangent=(forwardHorizontal*forwardAxis+rightHorizontal*rightAxis)/axisLength;
            var currentDirection=_surfaceCameraState.Anchor.NormalizedBodyFixedDirection;
            var rotationAxis=Double3.Cross(currentDirection,tangent).Normalized();
            var direction=DoubleQuaternion.FromAxisAngle(rotationAxis,travel/FocusedBody.RadiusMetres)
                .Rotate(currentDirection).Normalized();
            if(SurfaceAnchor.TryCreate(FocusedBody.BodyId,physicalTerrain.AuthorityVersion,direction,0d,out var movedAnchor)!=SurfaceAnchorCreationStatus.Success||
                !SurfaceCameraState.TryCreateFreeLook(movedAnchor,_surfaceCameraState.EyeOffsetEnuMetres,
                    _surfaceCameraState.PivotOffsetEnuMetres,_surfaceCameraState.LocalYawRadians,
                    _surfaceCameraState.LocalPitchRadians,out _surfaceCameraState))
                throw new InvalidOperationException("Surface camera translation produced an invalid anchor.");
            changed=true;
        }
        if(changed)ApplySurfaceCameraPose(camera);
    }

    private void ApplySurfaceCameraPose(CameraState camera)
    {
        var terrain=FocusedTerrain;
        var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(FocusedBody.BodyId,terrain);
        if(!SurfaceCameraAuthority.TryEvaluate(FocusedBody,_surfaceCameraState,physicalTerrain,out var pose))
            throw new InvalidOperationException("The retained surface camera authority cannot be evaluated.");
        var candidateCamera=pose.RootEye.Value;
        if(FocusedBodyHasNavigableSolidSurface)
        {
            var retainedState=_surfaceCameraState;
            if(!SurfaceCameraAuthority.TryConstrainBodyFixedEye(FocusedBody,retainedState,physicalTerrain,
                SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres,
                out _surfaceCameraState,out pose,out var constrained))
                ThrowFinalCameraExteriorFailure(FocusedBody.BodyId,candidateCamera);
            RecordCameraExteriorConstraint(constrained);
            candidateCamera=pose.RootEye.Value;
            _surfaceAltitudeMetres=constrained.SurfaceAltitudeMetres;
        }
        else _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);
        var anchorFocus=SurfaceAnchorFocus.AtDirection(FocusedBody.BodyId,
            _surfaceCameraState.Anchor.NormalizedBodyFixedDirection,FocusedBody.RadiusMetres,pose.PhysicalTerrainHeightMetres);
        _focusTarget=FocusTarget.AtSurface(anchorFocus);
        _surfaceAnchorBlend=1d;
        _surfaceCameraPivotRoot=pose.RootPivot.Value;
        _orbitDistance=Math.Sqrt((candidateCamera-_surfaceCameraPivotRoot).LengthSquared);
        _surfaceCameraMode=PlanetaryCameraPresentationMode.SurfaceLocal;
        CameraPresentationMode=SolarCameraPresentationMode.SurfaceLocal;
        camera.Orientation=pose.RootOrientation;
        camera.Projection=Projection;
        camera.Position=camera.Position with{Value=candidateCamera};
        _publishedCameraRoot=candidateCamera;
        camera.Validate();
        Update(camera);
    }

    private void ApplyInertialLook(float mouseDeltaX,float mouseDeltaY)
    {
        if(_inertialOrbitOrientationOverride is not { } current)
        {
            _orbitYawRadians-=mouseDeltaX*OrbitSensitivity;
            _orbitPitchRadians=Math.Clamp(_orbitPitchRadians-mouseDeltaY*OrbitSensitivity,-1.45d,1.45d);
            return;
        }
        var yaw=DoubleQuaternion.FromAxisAngle(Double3.UnitY,-mouseDeltaX*OrbitSensitivity);
        var yawed=(yaw*current).Normalized();
        var radialBefore=OrbitOffsetDirection();
        var yawedRadial=yaw.Rotate(radialBefore).Normalized();
        var right=yawed.Rotate(Double3.UnitX).Normalized();
        var pitch=DoubleQuaternion.FromAxisAngle(right,-mouseDeltaY*OrbitSensitivity);
        var candidate=(pitch*yawed).Normalized();
        var radial=pitch.Rotate(yawedRadial).Normalized();
        if(Math.Abs(Math.Asin(Math.Clamp(radial.Y,-1d,1d)))>1.45d){candidate=yawed;radial=yawedRadial;}
        _inertialOrbitOrientationOverride=candidate;
        _inertialOrbitOffsetDirectionOverride=radial;
        _orbitYawRadians=Math.Atan2(radial.X,radial.Z);
        _orbitPitchRadians=-Math.Asin(Math.Clamp(radial.Y,-1d,1d));
    }

    private void ApplyOrbitPose(CameraState camera,bool allowFocusTransition=false)
    {
        if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)
        {
            ApplySurfaceCameraPose(camera);
            return;
        }
        var orientation=OrbitOrientation();
        var rootRadial=OrbitOffsetDirection();
        var terrain=FocusedTerrain;
        var acquiredNow=false;
        var targetRoot=CurrentFocusRoot;
        var visualAimRoot=EvaluateRetainedVisualAimRoot(targetRoot);
        var cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(targetRoot,visualAimRoot,rootRadial);
        var candidateCamera=cameraLineOrigin+rootRadial*_orbitDistance;
        _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);

        if(allowFocusTransition&&_focusTarget.Kind==FocusTargetKind.BodyCenter&&FocusedBody.BodyId!=SolarSystemBodyIds.Sun.Value&&
            SurfaceFocusHandoffPolicy.ShouldAcquire(_surfaceAltitudeMetres)&&
            SurfaceAnchorAcquisition.TryAcquire(FocusedBody,new UniversePosition(candidateCamera,Presentation.RootFrame),-rootRadial,terrain,out var acquired))
        {
            _focusTarget=FocusTarget.AtSurface(acquired.Anchor);
            // Identity changes at zero positional weight; subsequent wheel input
            // advances the deterministic altitude blend without a target snap.
            _surfaceAnchorBlend=0d;
            _retainedVisualAimAnchor=acquired.Anchor;
            _retainedVisualAimOffsetRoot=acquired.RootPositionAtAcquisition.Value-FocusedBody.Position.Value;
            _retainedVisualAimWeight=1d;
            acquiredNow=true;
            targetRoot=SurfaceFocusHandoffPolicy.BlendedRoot(FocusedBody.Position.Value,acquired.RootPositionAtAcquisition.Value,_surfaceAnchorBlend);
            visualAimRoot=EvaluateRetainedVisualAimRoot(targetRoot);
            cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(targetRoot,visualAimRoot,rootRadial);
            _orbitDistance=Double3.Dot(candidateCamera-cameraLineOrigin,rootRadial);
            candidateCamera=cameraLineOrigin+rootRadial*_orbitDistance;
            _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);
        }

        if(_focusTarget.Kind==FocusTargetKind.SurfaceAnchor)
        {
            RetainActiveSurfaceAim();
            var anchorRoot=EvaluateSurfaceAnchorRoot();
            if(allowFocusTransition&&!acquiredNow)
            {
                _surfaceAnchorBlend=SurfaceFocusHandoffPolicy.SurfaceBlend(_surfaceAltitudeMetres);
            }
            targetRoot=SurfaceFocusHandoffPolicy.BlendedRoot(FocusedBody.Position.Value,anchorRoot,_surfaceAnchorBlend);
            visualAimRoot=EvaluateRetainedVisualAimRoot(targetRoot);
            cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(targetRoot,visualAimRoot,rootRadial);
            candidateCamera=cameraLineOrigin+rootRadial*_orbitDistance;
            _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);
            if(allowFocusTransition&&!acquiredNow&&_surfaceAnchorBlend==0d&&SurfaceFocusHandoffPolicy.ShouldRelease(_surfaceAltitudeMetres))
            {
                _focusTarget=FocusTarget.BodyCenter(FocusedBody.BodyId);targetRoot=FocusedBody.Position.Value;
                visualAimRoot=EvaluateRetainedVisualAimRoot(targetRoot);
                cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(targetRoot,visualAimRoot,rootRadial);
                candidateCamera=cameraLineOrigin+rootRadial*_orbitDistance;
                _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);
            }
        }
        else
        {
            targetRoot=FocusedBody.Position.Value;
            visualAimRoot=EvaluateRetainedVisualAimRoot(targetRoot);
            cameraLineOrigin=SurfaceVisualAimHandoffPolicy.CameraLineOrigin(targetRoot,visualAimRoot,rootRadial);
            candidateCamera=cameraLineOrigin+rootRadial*_orbitDistance;
            _surfaceAltitudeMetres=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,candidateCamera,terrain);
        }

        // Surface exclusion is a final camera-position invariant, not a
        // SurfaceAnchor privilege. Body-center, handoff, retained-aim, and
        // released-anchor frames all resolve against the same body-fixed
        // physical surface before the camera is published.
        if(FocusedBodyHasNavigableSolidSurface)
        {
            if(double.IsFinite(_bodyLocalCameraAltitudeDemandMetres))
            {
                if(!_bodyLocalCameraPlacementUseOrbitCandidate)
                    candidateCamera=_bodyLocalCameraPlacementPending?_bodyLocalCameraPlacementSeedRoot:_publishedCameraRoot;
                var rootToBody=FocusedBody.BodyFixedToRoot.Conjugate().Normalized();
                var candidateBody=rootToBody.Rotate(candidateCamera-FocusedBody.Position.Value);
                var publishedBody=rootToBody.Rotate(_publishedCameraRoot-FocusedBody.Position.Value);
                if(candidateBody.LengthSquared>0d&&publishedBody.LengthSquared>0d&&
                   Double3.Dot(candidateBody.Normalized(),publishedBody.Normalized())<=0d)
                {
                    // A close visual-aim ray may point through the body. Entering
                    // body-local altitude authority must retain the last accepted
                    // hemisphere rather than accepting that opposite-side exit.
                    candidateCamera=_publishedCameraRoot;
                }
                if(!SurfaceAnchorAcquisition.TryPlaceCameraOriginAtSurfaceAltitude(
                    FocusedBody,candidateCamera,terrain,_bodyLocalCameraAltitudeDemandMetres,
                    out candidateCamera,out var placementQueries))
                    ThrowFinalCameraExteriorFailure(FocusedBody.BodyId,candidateCamera);
                _cameraClearanceTerrainQueries+=placementQueries;
                _bodyLocalCameraPlacementPending=false;
                _bodyLocalCameraPlacementUseOrbitCandidate=false;
            }
            if(!SurfaceAnchorAcquisition.TryConstrainCameraOrigin(
                FocusedBody,candidateCamera,terrain,
                SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres,
                out var constrained))
                ThrowFinalCameraExteriorFailure(FocusedBody.BodyId,candidateCamera);
            RecordCameraExteriorConstraint(constrained);
            candidateCamera=constrained.RootPosition;
            _surfaceAltitudeMetres=constrained.SurfaceAltitudeMetres;
        }

        if(!double.IsFinite(_bodyLocalCameraAltitudeDemandMetres))_bodyLocalCameraPlacementUseOrbitCandidate=false;
        _surfaceCameraMode=_focusTarget.Kind!=FocusTargetKind.SurfaceAnchor?PlanetaryCameraPresentationMode.Orbital:
            _surfaceAnchorBlend<1d?PlanetaryCameraPresentationMode.Transition:PlanetaryCameraPresentationMode.SurfaceLocal;
        if(_surfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal)CameraPresentationMode=SolarCameraPresentationMode.SurfaceLocal;
        else if(CameraPresentationMode!=SolarCameraPresentationMode.SolarMap)CameraPresentationMode=SolarCameraPresentationMode.Free3D;
        camera.Orientation=orientation;
        camera.Projection=Projection;
        camera.Position=camera.Position with{Value=candidateCamera};
        _publishedCameraRoot=candidateCamera;
        camera.Validate();
        Update(camera);
    }

    private PlanetaryTerrainDefinition FocusedTerrain=>FocusedBody.BodyId==SolarSystemBodyIds.Earth.Value?EarthPlanetaryScene.Terrain:default;
    private bool FocusedBodyHasNavigableSolidSurface=>SolarPlanetMaterials.Catalog.TryGet(FocusedBody.BodyId,out var material)&&
        material.Kind is PlanetMaterialKind.Rocky or PlanetMaterialKind.Terrestrial;

    internal double EnforceFinalCameraInvariant(CameraState camera)
    {
        var terrain=FocusedTerrain;
        if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)
        {
            var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(FocusedBody.BodyId,terrain);
            var retainedState=_surfaceCameraState;
            if(!SurfaceCameraAuthority.TryConstrainBodyFixedEye(FocusedBody,retainedState,physicalTerrain,
                SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres,
                out _surfaceCameraState,out var pose,out var surfaceConstraint))
                ThrowFinalCameraExteriorFailure(FocusedBody.BodyId,camera.Position.Value);
            RecordCameraExteriorConstraint(surfaceConstraint);
            camera.Position=camera.Position with{Value=pose.RootEye.Value};
            camera.Orientation=pose.RootOrientation;
            _surfaceCameraPivotRoot=pose.RootPivot.Value;
            _publishedCameraRoot=pose.RootEye.Value;
            _surfaceAltitudeMetres=surfaceConstraint.SurfaceAltitudeMetres;
            camera.Validate();
            return _surfaceAltitudeMetres;
        }
        if(FocusedBodyHasNavigableSolidSurface)
        {
            if(!SurfaceAnchorAcquisition.TryConstrainCameraOrigin(
                FocusedBody,camera.Position.Value,terrain,
                SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres,
                out var constrained))
                ThrowFinalCameraExteriorFailure(FocusedBody.BodyId,camera.Position.Value);
            RecordCameraExteriorConstraint(constrained);
            camera.Position=camera.Position with{Value=constrained.RootPosition};
            _publishedCameraRoot=constrained.RootPosition;
            _surfaceAltitudeMetres=constrained.SurfaceAltitudeMetres;
        }
        var altitude=SurfaceAnchorAcquisition.SurfaceAltitude(FocusedBody,camera.Position.Value,terrain);
        if(!double.IsFinite(altitude)||(FocusedBodyHasNavigableSolidSurface&&!SurfaceFocusHandoffPolicy.SatisfiesMinimumTerrainClearance(altitude)))
            ThrowFinalCameraClearanceFailure(FocusedBody.BodyId,altitude);
        _surfaceAltitudeMetres=altitude;
        return altitude;
    }

    private void RecordCameraExteriorConstraint(in CameraExteriorConstraintResult value)
    {
        _cameraClearanceTerrainQueries+=value.TerrainQueries;
        _cameraClearanceIterationCount+=value.Iterations;
        switch(value.Iterations)
        {
            case 0:_cameraClearanceIterationZeroCount++;break;
            case 1:_cameraClearanceIterationOneCount++;break;
            case 2:_cameraClearanceIterationTwoCount++;break;
            case 3:_cameraClearanceIterationThreeCount++;break;
        }
        _cameraClearanceMaximumIterations=Math.Max(_cameraClearanceMaximumIterations,value.Iterations);
        _cameraClearanceMaximumCorrectionMetres=Math.Max(_cameraClearanceMaximumCorrectionMetres,value.CorrectionMetres);
        if(value.CorrectionMetres>0d)_cameraClearanceCorrectionCount++;
    }

    internal static void ValidateFinalCameraClearance(ulong bodyId,double altitudeMetres)
    {
        if(!SurfaceFocusHandoffPolicy.SatisfiesMinimumTerrainClearance(altitudeMetres))
            ThrowFinalCameraClearanceFailure(bodyId,altitudeMetres);
    }

    private static void ThrowFinalCameraClearanceFailure(ulong bodyId,double altitudeMetres) =>
        throw new InvalidOperationException($"Final camera clearance invariant failed for body {bodyId}: altitude={altitudeMetres:R} m; physicalMinimum={SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres:R} m; tolerance={SurfaceFocusHandoffPolicy.TerrainClearanceInvariantToleranceMetres:R} m.");
    private static void ThrowFinalCameraExteriorFailure(ulong bodyId,in Double3 proposedCameraRoot) =>
        throw new InvalidOperationException($"Final body-local camera exterior solver failed for body {bodyId}: proposedRoot={proposedCameraRoot}.");

    private static bool TryLoadEarthElevationOracle(out string error)
    {
        if(EarthElevationDataset.IsLoaded){error=string.Empty;return true;}
        if(EarthElevationDataset.TryLoad(Path.Combine(AppContext.BaseDirectory,"earth-data"),out error))return true;
        if(TerrainAssetRepository.TryFindRoot(out var repositoryRoot)&&
            EarthElevationDataset.TryLoad(Path.Combine(repositoryRoot,"assets","earth","runtime"),out error))return true;
        error=$"Solar terrain-v5 camera authority requires the Earth elevation oracle. {error}";
        return false;
    }
    private Double3 EvaluateFocusRoot()=>_focusTarget.TryEvaluate(FocusedBody,out var root)?root.Value:throw new InvalidOperationException("The active focus target cannot be evaluated from the current presentation snapshot.");
    private Double3 EvaluateSurfaceAnchorRoot()=>_focusTarget.Kind==FocusTargetKind.SurfaceAnchor?EvaluateFocusRoot():throw new InvalidOperationException("A surface anchor is not active.");
    private Double3 EvaluateRetainedVisualAimRoot(in Double3 fallbackRoot)=>_retainedVisualAimAnchor.HasValue
        ?SurfaceFocusHandoffPolicy.BlendedRoot(FocusedBody.Position.Value,FocusedBody.Position.Value+_retainedVisualAimOffsetRoot,_retainedVisualAimWeight)
        :fallbackRoot;
    private void RetainActiveSurfaceAim()
    {
        if(_focusTarget.Kind!=FocusTargetKind.SurfaceAnchor)return;
        var activeAnchor=_focusTarget.SurfaceAnchor;
        if(_retainedVisualAimAnchor is not { } retainedAnchor||retainedAnchor!=activeAnchor)
        {
            _retainedVisualAimAnchor=activeAnchor;
            _retainedVisualAimOffsetRoot=EvaluateSurfaceAnchorRoot()-FocusedBody.Position.Value;
        }
        _retainedVisualAimWeight=1d;
    }
    private void UpdateRetainedVisualAim(double surfaceAltitudeMetres,in Double3 rootRadial)
    {
        if(!_retainedVisualAimAnchor.HasValue)return;
        var separation=SurfaceVisualAimHandoffPolicy.AngularSeparation(FocusedBody,FocusedBody.Position.Value+_retainedVisualAimOffsetRoot,rootRadial,surfaceAltitudeMetres);
        _retainedVisualAimWeight=SurfaceVisualAimHandoffPolicy.RetainedWeight(separation);
        if(_retainedVisualAimWeight==0d){_retainedVisualAimAnchor=null;_retainedVisualAimOffsetRoot=Double3.Zero;}
    }
    private DoubleQuaternion OrbitOrientation()=>_inertialOrbitOrientationOverride??
        (DoubleQuaternion.FromAxisAngle(Double3.UnitY,_orbitYawRadians)*DoubleQuaternion.FromAxisAngle(Double3.UnitX,_orbitPitchRadians)).Normalized();
    private Double3 OrbitOffsetDirection()=>_inertialOrbitOffsetDirectionOverride??-OrbitOrientation().Rotate(new Double3(0d,0d,-1d));
    private Double3 CurrentSurfaceDirection(){if(_cameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative)return _surfaceCameraState.Anchor.NormalizedBodyFixedDirection;if(_focusTarget.Kind==FocusTargetKind.SurfaceAnchor)return _focusTarget.SurfaceAnchor.BodyFixedDirection;var rootRadial=OrbitOffsetDirection();return FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(rootRadial);}

    private void UpdateOrbitVertices(CameraState camera)
    {
        var output = 0;
        for (var path = 0; path < OrbitPathCount; path++)
        for (var segment = 0; segment < OrbitSegmentCount; segment++)
        {
            var first = CameraRelativeRenderPosition.Create(_rootOrbitSamples[path * OrbitSampleCount + segment], camera.Position.Value).Value;
            var secondSample = path == MoonOrbitPathIndex && segment == OrbitSegmentCount - 1 ? 0 : segment + 1;
            var second = CameraRelativeRenderPosition.Create(_rootOrbitSamples[path * OrbitSampleCount + secondSample], camera.Position.Value).Value;
            OrbitVertices[output++] = new NativeOrbitLineVertex { X = (float)first.X, Y = (float)first.Y, Z = (float)first.Z };
            OrbitVertices[output++] = new NativeOrbitLineVertex { X = (float)second.X, Y = (float)second.Y, Z = (float)second.Z };
        }
    }

    private static bool TryBuildRootOrbitSamples(CelestialSystemDefinition system, out Double3[] samples, out int[] traversal, out int[] centers, out double[] periods, out string error)
    {
        samples = new Double3[OrbitPathCount * OrbitSampleCount];
        traversal = new int[OrbitPathCount];
        centers = new int[OrbitPathCount];
        periods = new double[OrbitPathCount];
        var sunTraversal = FindTraversalIndex(system, SolarSystemBodyIds.Sun);
        if (sunTraversal < 0) { error = "Solar orbit center is missing from the hierarchy."; return false; }
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
                !system.TryGetAnalyticalCorrection(node.Ephemeris.PayloadIndex, out var correction) ||
                !correction.IsValid ||
                !TryGetPeriodSeconds(trajectory, central.PhysicalProperties.GravitationalParameter, out periods[path]))
            {
                error = "Solar orbit trajectory authority is unavailable.";
                return false;
            }
            periods[path] /= correction.TimeScale;
            traversal[path] = nodeIndex;
            if (!system.TryGetBody(bodyId, out var catalog)) { error = "Solar orbit catalog body is missing."; return false; }
            var centerId = catalog.Identity.Classification == CelestialBodyClassification.Moon
                ? catalog.Identity.ParentBody
                : SolarSystemBodyIds.Sun;
            if (centerId is not { } resolvedCenter || (centers[path] = FindTraversalIndex(system, resolvedCenter)) < 0)
            {
                error = "Solar orbit presentation center is unavailable.";
                return false;
            }
        }

        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var initial = CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.Zero, evaluations, roots, staging, stagingRoots);
        if (!initial.Succeeded) { error = $"Solar orbit center evaluation failed: {initial.Status}"; return false; }
        Span<Double3> currentCenters = stackalloc Double3[OrbitPathCount];
        for (var path = 0; path < OrbitPathCount; path++) currentCenters[path] = roots[centers[path]].Translation;
        for (var sample = 0; sample < OrbitSampleCount; sample++)
        for (var path = 0; path < OrbitPathCount; path++)
        {
            SimulationInstant time;
            try { time = SimulationInstant.FromSecondsRounded(periods[path] * sample / OrbitSegmentCount); }
            catch (OverflowException) { error = "Solar orbit sample time overflow."; return false; }
            var result = CelestialSystemEvaluator.TryEvaluateSystem(system, time, evaluations, roots, staging, stagingRoots);
            if (!result.Succeeded) { error = $"Solar orbit evaluation failed: {result.Status}"; return false; }
            samples[path * OrbitSampleCount + sample] = sample == 0
                ? roots[traversal[path]].Translation
                : currentCenters[path] + roots[traversal[path]].Translation - roots[centers[path]].Translation;
        }
        error = string.Empty;
        return true;
    }

    private bool TrySampleRootOrbits(SimulationInstant start, Span<Double3> destination, out string error)
    {
        if (destination.Length < OrbitPathCount * OrbitSampleCount) { error = "Solar orbit sample destination is too small."; return false; }
        Span<Double3> currentCenters = stackalloc Double3[OrbitPathCount];
        for (var path = 0; path < OrbitPathCount; path++) currentCenters[path] = _roots[_orbitCenterTraversalIndices[path]].Translation;
        for (var sample = 0; sample < OrbitSampleCount; sample++)
        for (var path = 0; path < OrbitPathCount; path++)
        {
            SimulationInstant time;
            try { time = start + SimulationDuration.FromSecondsRounded(_orbitPeriods[path] * sample / OrbitSegmentCount); }
            catch (OverflowException) { error = "Solar orbit sample time overflow."; return false; }
            var result = CelestialSystemEvaluator.TryEvaluateSystem(_system, time, _evaluations, _roots, _staging, _stagingRoots);
            if (!result.Succeeded) { error = $"Solar orbit evaluation failed: {result.Status}"; return false; }
            destination[path * OrbitSampleCount + sample] = sample == 0
                ? _roots[_orbitTraversalIndices[path]].Translation
                : currentCenters[path] + _roots[_orbitTraversalIndices[path]].Translation - _roots[_orbitCenterTraversalIndices[path]].Translation;
        }
        BuildPeriodicMoonOrbit(destination);
        error = string.Empty;
        return true;
    }

    private void BuildPeriodicMoonOrbit(Span<Double3> destination)
    {
        var pathOffset = MoonOrbitPathIndex * OrbitSampleCount;
        for (var sample = 0; sample < OrbitSegmentCount; sample++)
            _moonOrbitControlSamples[sample] = destination[pathOffset + sample];

        var endpoint = destination[pathOffset + OrbitSegmentCount];
        var startDerivative =
            (_moonOrbitControlSamples[0] * -3d + _moonOrbitControlSamples[1] * 4d - _moonOrbitControlSamples[2]) *
            (OrbitSegmentCount * .5d);
        var endDerivative =
            (endpoint * 3d - _moonOrbitControlSamples[OrbitSegmentCount - 1] * 4d +
             _moonOrbitControlSamples[OrbitSegmentCount - 2]) * (OrbitSegmentCount * .5d);
        var positionClosure = endpoint - _moonOrbitControlSamples[0];
        var derivativeClosure = endDerivative - startDerivative;
        _moonOrbitEndpointMismatchMetres = Math.Sqrt(positionClosure.LengthSquared);
        var oppositeSample = OrbitSegmentCount / 2;
        for (var sample = 0; sample < OrbitSegmentCount; sample++)
        {
            if (sample <= oppositeSample)
            {
                _moonOrbitPeriodicControls[sample] = _moonOrbitControlSamples[sample];
                continue;
            }
            var phase = (sample - oppositeSample) / (double)oppositeSample;
            var squared = phase * phase;
            var cubed = squared * phase;
            var positionWeight = -2d * cubed + 3d * squared;
            var derivativeWeight = cubed - squared;
            _moonOrbitPeriodicControls[sample] = _moonOrbitControlSamples[sample] -
                positionClosure * positionWeight - derivativeClosure * (.5d * derivativeWeight);
        }
        _moonOrbitPeriodicControls[0] = _moonOrbitControlSamples[0];

        var denseIndex = 0;
        for (var segment = 0; segment < OrbitSegmentCount; segment++)
        {
            var previous = _moonOrbitPeriodicControls[(segment + OrbitSegmentCount - 1) % OrbitSegmentCount];
            var first = _moonOrbitPeriodicControls[segment];
            var second = _moonOrbitPeriodicControls[(segment + 1) % OrbitSegmentCount];
            var following = _moonOrbitPeriodicControls[(segment + 2) % OrbitSegmentCount];
            var firstTangent = (second - previous) * .5d;
            var secondTangent = (following - first) * .5d;
            for (var subdivision = 0; subdivision < MoonPeriodicSubdivisionsPerControlSegment; subdivision++)
            {
                var amount = subdivision / (double)MoonPeriodicSubdivisionsPerControlSegment;
                _moonOrbitDenseSamples[denseIndex++] = CubicHermite(first, firstTangent, second, secondTangent, amount);
            }
        }
        _moonOrbitDenseSamples[MoonPeriodicDenseSampleCount] = _moonOrbitDenseSamples[0];
        _moonOrbitCumulativeLengths[0] = 0d;
        for (var sample = 1; sample <= MoonPeriodicDenseSampleCount; sample++)
            _moonOrbitCumulativeLengths[sample] = _moonOrbitCumulativeLengths[sample - 1] +
                Math.Sqrt((_moonOrbitDenseSamples[sample] - _moonOrbitDenseSamples[sample - 1]).LengthSquared);

        var totalLength = _moonOrbitCumulativeLengths[MoonPeriodicDenseSampleCount];
        if (!double.IsFinite(totalLength) || totalLength <= 0d)
        {
            destination[pathOffset + OrbitSegmentCount] = destination[pathOffset];
            return;
        }

        var denseCursor = 0;
        var oppositeDenseSample = MoonPeriodicDenseSampleCount / 2;
        var oppositeLength = _moonOrbitCumulativeLengths[oppositeDenseSample];
        for (var sample = 0; sample < OrbitSegmentCount; sample++)
        {
            var targetLength = sample < OrbitSegmentCount / 2
                ? oppositeLength * sample / (OrbitSegmentCount / 2)
                : oppositeLength + (totalLength - oppositeLength) *
                    (sample - OrbitSegmentCount / 2) / (OrbitSegmentCount / 2);
            while (denseCursor + 1 < MoonPeriodicDenseSampleCount &&
                   _moonOrbitCumulativeLengths[denseCursor + 1] < targetLength)
                denseCursor++;
            var intervalLength = _moonOrbitCumulativeLengths[denseCursor + 1] - _moonOrbitCumulativeLengths[denseCursor];
            var amount = intervalLength > 0d
                ? (targetLength - _moonOrbitCumulativeLengths[denseCursor]) / intervalLength
                : 0d;
            destination[pathOffset + sample] = _moonOrbitDenseSamples[denseCursor] +
                (_moonOrbitDenseSamples[denseCursor + 1] - _moonOrbitDenseSamples[denseCursor]) * amount;
        }
        destination[pathOffset + OrbitSegmentCount] = destination[pathOffset];
    }

    private static Double3 CubicHermite(in Double3 first, in Double3 firstTangent, in Double3 second, in Double3 secondTangent, double amount)
    {
        var squared = amount * amount;
        var cubed = squared * amount;
        return first * (2d * cubed - 3d * squared + 1d) +
               firstTangent * (cubed - 2d * squared + amount) +
               second * (-2d * cubed + 3d * squared) +
               secondTangent * (cubed - squared);
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
