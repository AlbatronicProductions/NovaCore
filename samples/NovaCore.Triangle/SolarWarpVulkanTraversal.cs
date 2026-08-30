using NovaCore.Interop;
using NovaCore.Graphics;
using NovaCore.Simulation.Time;

internal sealed class SolarWarpVulkanTraversal
{
    private enum Phase
    {
        Settle,
        WarpUp,
        MaximumWarp,
        WarpDown,
        DownwarpSettle,
        PausePulse,
        Paused,
        ResumePulse,
        Resumed,
        FocusEarth,
        EarthApproach,
        EarthSurfaceSettle,
        EarthRetreat,
        EarthFarSettle,
        FocusMoon,
        MoonSettle,
        FocusSun,
        FinalSettle,
        Complete
    }

    private Phase _phase;
    private int _phaseFrames;
    private int _frames;
    private long _authorityMismatches;
    private long _nonFiniteFrames;
    private long _cacheRebuildFrames;
    private double _maximumGpuAttachmentErrorMetres;
    private double _maximumIndependentRootErrorMetres;
    private double _maximumIndependentBodyCameraErrorMetres;
    private double _maximumIndependentOrbitCameraErrorMetres;
    private double _maximumIndependentBodyScreenErrorNdc;
    private double _maximumIndependentOrbitScreenErrorNdc;
    private long _independentReferenceFailures;
    private bool _sawOne;
    private bool _sawThirty;
    private bool _sawSixHundred;
    private bool _sawFourteenFourHundred;
    private bool _sawMaximum;
    private SimulationInstant _pausedInstant;
    private bool _pausedInstantCaptured;
    private bool _pausedAdvanced;
    private bool _resumedAdvanced;
    private bool _sawEarthSurface;
    private bool _sawEarthRetreat;

    internal bool Failed { get; private set; }
    internal string FinalReport { get; private set; } = string.Empty;

    internal NativeInputState PrepareInput(in NativeInputState real, SolarSystemScene scene)
    {
        var input = new NativeInputState
        {
            DeltaSeconds = Math.Clamp(real.DeltaSeconds, 1f / 240f, 1f / 30f),
            ViewportWidthPixels = real.ViewportWidthPixels,
            ViewportHeightPixels = real.ViewportHeightPixels
        };
        switch (_phase)
        {
            case Phase.WarpUp when scene.SpeedPresetIndex < SimulationSpeedPresets.Count - 1:
                input.RateIncrease = 1;
                break;
            case Phase.WarpDown when scene.SpeedPresetIndex > SimulationSpeedPresets.IndexOf(SimulationRate.One):
                input.RateDecrease = 1;
                break;
            case Phase.PausePulse:
            case Phase.ResumePulse:
                input.PauseToggle = 1;
                break;
            case Phase.FocusEarth:
                input.PresentationFocus = NativePresentationFocus.Earth;
                break;
            case Phase.EarthApproach:
                input.MouseWheelDetents = 8;
                break;
            case Phase.EarthSurfaceSettle:
                input.LookActive = 1;
                input.MouseDeltaX = 3;
                input.MouseDeltaY = -1;
                break;
            case Phase.EarthRetreat:
                input.MouseWheelDetents = -100;
                break;
            case Phase.FocusMoon:
                input.PresentationFocus = NativePresentationFocus.Moon;
                break;
            case Phase.FocusSun:
                input.PresentationFocus = NativePresentationFocus.Sun;
                break;
        }
        return input;
    }

    internal bool Observe(SolarSystemScene scene)
    {
        _frames++;
        ObserveRate(scene.Rate);
        if (scene.OrbitAuthorityTime != scene.CurrentTime) _authorityMismatches++;
        if (scene.OrbitCurveBuildCount != 1) _cacheRebuildFrames++;

        var finite = true;
        for (var path = 0; path < SolarSystemScene.OrbitPathCount; path++)
        {
            var vertex = scene.OrbitVertices[path * SolarSystemScene.OrbitSegmentCount * 2];
            var bodyOrdinal = (uint)(path + 2);
            var bodyId = SolarSystemScene.BodyOrder[path + 1];
            var body = default(NativePlanetaryPresentation);
            for (var bodyIndex = 0; bodyIndex < scene.DistantBodyCount; bodyIndex++)
                if ((scene.DistantBodies[bodyIndex].Enabled & 255u) == bodyOrdinal)
                {
                    body = scene.DistantBodies[bodyIndex];
                    break;
                }
            finite &= float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z) &&
                      float.IsFinite(body.CenterX) && float.IsFinite(body.CenterY) && float.IsFinite(body.CenterZ);
            var dx = ((double)vertex.X+vertex.LowX) - ((double)body.CenterX+body.CenterLowX);
            var dy = ((double)vertex.Y+vertex.LowY) - ((double)body.CenterY+body.CenterLowY);
            var dz = ((double)vertex.Z+vertex.LowZ) - ((double)body.CenterZ+body.CenterLowZ);
            _maximumGpuAttachmentErrorMetres = Math.Max(_maximumGpuAttachmentErrorMetres,
                Math.Sqrt(dx * dx + dy * dy + dz * dz));
            if (!scene.TryEvaluateIndependentBodyRoot(bodyId, scene.CurrentTime, out var referenceRoot) ||
                !scene.Presentation.TryGetBody(bodyId, out var productionBody))
            {
                _independentReferenceFailures++;
                continue;
            }
            var referenceRelative = referenceRoot - scene.LastOrbitCameraRoot;
            var productionRootError = Math.Sqrt((productionBody.Position.Value - referenceRoot).LengthSquared);
            var bodyRelative = new NovaCore.Core.Double3((double)body.CenterX+body.CenterLowX,
                (double)body.CenterY+body.CenterLowY,(double)body.CenterZ+body.CenterLowZ);
            var orbitRelative = new NovaCore.Core.Double3((double)vertex.X+vertex.LowX,
                (double)vertex.Y+vertex.LowY,(double)vertex.Z+vertex.LowZ);
            var bodyCameraError = Math.Sqrt((bodyRelative - referenceRelative).LengthSquared);
            var orbitCameraError = Math.Sqrt((orbitRelative - referenceRelative).LengthSquared);
            _maximumIndependentRootErrorMetres = Math.Max(_maximumIndependentRootErrorMetres, productionRootError);
            _maximumIndependentBodyCameraErrorMetres = Math.Max(_maximumIndependentBodyCameraErrorMetres, bodyCameraError);
            _maximumIndependentOrbitCameraErrorMetres = Math.Max(_maximumIndependentOrbitCameraErrorMetres, orbitCameraError);
            if (TryProject(referenceRelative, scene, out var referenceX, out var referenceY) &&
                TryProject(bodyRelative, scene, out var bodyX, out var bodyY))
                _maximumIndependentBodyScreenErrorNdc = Math.Max(_maximumIndependentBodyScreenErrorNdc,
                    Math.Sqrt((bodyX - referenceX) * (bodyX - referenceX) + (bodyY - referenceY) * (bodyY - referenceY)));
            if (TryProject(referenceRelative, scene, out referenceX, out referenceY) &&
                TryProject(orbitRelative, scene, out var orbitX, out var orbitY))
                _maximumIndependentOrbitScreenErrorNdc = Math.Max(_maximumIndependentOrbitScreenErrorNdc,
                    Math.Sqrt((orbitX - referenceX) * (orbitX - referenceX) + (orbitY - referenceY) * (orbitY - referenceY)));
        }
        if (!finite) _nonFiniteFrames++;

        if (_phase == Phase.Paused)
        {
            if (!_pausedInstantCaptured)
            {
                _pausedInstant = scene.CurrentTime;
                _pausedInstantCaptured = true;
            }
            else if (scene.CurrentTime != _pausedInstant) _pausedAdvanced = true;
        }
        if (_phase == Phase.Resumed && _pausedInstantCaptured && scene.CurrentTime != _pausedInstant)
            _resumedAdvanced = true;
        _sawEarthSurface |= scene.FocusedBody.Label == "Earth" &&
                            scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.SurfaceLocal;
        _sawEarthRetreat |= _sawEarthSurface && scene.FocusedBody.Label == "Earth" &&
                            scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.Orbital &&
                            scene.FocusedBlend.Regime == PlanetaryRenderRegime.DistantOnly;

        AdvancePhase(scene);
        if (_phase != Phase.Complete) return false;

        Failed = _authorityMismatches != 0 || _nonFiniteFrames != 0 || _cacheRebuildFrames != 0 ||
                 _maximumGpuAttachmentErrorMetres != 0d || !_sawOne || !_sawThirty || !_sawSixHundred ||
                 !_sawFourteenFourHundred || !_sawMaximum || _pausedAdvanced || !_resumedAdvanced ||
                 scene.OrbitCurveBuildCount != 1 || _independentReferenceFailures != 0 ||
                 _maximumIndependentRootErrorMetres > .01d ||
                 _maximumIndependentBodyScreenErrorNdc > 1e-5d ||
                 _maximumIndependentOrbitScreenErrorNdc > 1e-5d || !_sawEarthSurface || !_sawEarthRetreat;
        FinalReport = $"Solar warp Vulkan traversal {(Failed ? "FAIL" : "PASS")}: frames={_frames}; " +
                      $"authorityMismatches={_authorityMismatches}; gpuAttachmentMax={_maximumGpuAttachmentErrorMetres:E6}m; " +
                      $"independentRootMax={_maximumIndependentRootErrorMetres:E6}m; " +
                      $"independentBodyCameraMax={_maximumIndependentBodyCameraErrorMetres:E6}m; " +
                      $"independentOrbitCameraMax={_maximumIndependentOrbitCameraErrorMetres:E6}m; " +
                      $"independentScreenBodyMax={_maximumIndependentBodyScreenErrorNdc:E6}ndc; " +
                      $"independentScreenOrbitMax={_maximumIndependentOrbitScreenErrorNdc:E6}ndc; " +
                      $"independentFailures={_independentReferenceFailures}; " +
                      $"nonFiniteFrames={_nonFiniteFrames}; cacheBuilds={scene.OrbitCurveBuildCount}; " +
                      $"cacheReuses={scene.OrbitCurveReuseCount}; cacheRebuildFrames={_cacheRebuildFrames}; " +
                      $"rates=1x:{_sawOne},30x:{_sawThirty},600x:{_sawSixHundred},14400x:{_sawFourteenFourHundred},max:{_sawMaximum}; " +
                      $"pausedStable={!_pausedAdvanced}; resumedAdvanced={_resumedAdvanced}; " +
                      $"earthSurface={_sawEarthSurface}; earthRetreat={_sawEarthRetreat}; " +
                      $"finalFocus={scene.FocusedBody.Label}; finalInstant={scene.CurrentTime.Ticks}";
        return true;
    }

    private static bool TryProject(in NovaCore.Core.Double3 cameraRelative,
        SolarSystemScene scene, out double x, out double y)
    {
        x = y = double.NaN;
        var view = scene.LastOrbitCameraOrientation.Conjugate().Normalized().Rotate(cameraRelative);
        var forward = -view.Z;
        var projection = scene.LastOrbitCameraProjection;
        if (!view.IsFinite || !double.IsFinite(forward) || forward <= projection.NearClip) return false;
        var scale = 1d / Math.Tan(projection.VerticalFieldOfViewRadians * .5d);
        x = scale / projection.AspectRatio * view.X / forward;
        y = -scale * view.Y / forward;
        return double.IsFinite(x) && double.IsFinite(y);
    }

    private void AdvancePhase(SolarSystemScene scene)
    {
        _phaseFrames++;
        switch (_phase)
        {
            case Phase.Settle when _phaseFrames >= 20:
                Next(Phase.WarpUp);
                break;
            case Phase.WarpUp when scene.SpeedPresetIndex == SimulationSpeedPresets.Count - 1:
                Next(Phase.MaximumWarp);
                break;
            case Phase.MaximumWarp when _phaseFrames >= 30:
                Next(Phase.WarpDown);
                break;
            case Phase.WarpDown when scene.SpeedPresetIndex == SimulationSpeedPresets.IndexOf(SimulationRate.One):
                Next(Phase.DownwarpSettle);
                break;
            case Phase.DownwarpSettle when _phaseFrames >= 15:
                Next(Phase.PausePulse);
                break;
            case Phase.PausePulse:
                Next(Phase.Paused);
                break;
            case Phase.Paused when _phaseFrames >= 20:
                Next(Phase.ResumePulse);
                break;
            case Phase.ResumePulse:
                Next(Phase.Resumed);
                break;
            case Phase.Resumed when _phaseFrames >= 20:
                Next(Phase.FocusEarth);
                break;
            case Phase.FocusEarth:
                Next(Phase.EarthApproach);
                break;
            case Phase.EarthApproach when scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.SurfaceLocal:
                Next(Phase.EarthSurfaceSettle);
                break;
            case Phase.EarthApproach when _phaseFrames >= 120:
                Next(Phase.EarthSurfaceSettle);
                break;
            case Phase.EarthSurfaceSettle when _phaseFrames >= 20:
                Next(Phase.EarthRetreat);
                break;
            case Phase.EarthRetreat when scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.Orbital &&
                                                 scene.FocusedBlend.Regime == PlanetaryRenderRegime.DistantOnly:
                Next(Phase.EarthFarSettle);
                break;
            case Phase.EarthRetreat when _phaseFrames >= 120:
                Next(Phase.EarthFarSettle);
                break;
            case Phase.EarthFarSettle when _phaseFrames >= 20:
                Next(Phase.FocusMoon);
                break;
            case Phase.FocusMoon:
                Next(Phase.MoonSettle);
                break;
            case Phase.MoonSettle when _phaseFrames >= 15:
                Next(Phase.FocusSun);
                break;
            case Phase.FocusSun:
                Next(Phase.FinalSettle);
                break;
            case Phase.FinalSettle when _phaseFrames >= 20:
                Next(Phase.Complete);
                break;
        }
    }

    private void ObserveRate(SimulationRate rate)
    {
        _sawOne |= rate == SimulationRate.One;
        _sawThirty |= rate == new SimulationRate(30, 1);
        _sawSixHundred |= rate == new SimulationRate(600, 1);
        _sawFourteenFourHundred |= rate == new SimulationRate(14_400, 1);
        _sawMaximum |= rate == new SimulationRate(7_776_000, 1);
    }

    private void Next(Phase phase)
    {
        _phase = phase;
        _phaseFrames = 0;
    }
}
