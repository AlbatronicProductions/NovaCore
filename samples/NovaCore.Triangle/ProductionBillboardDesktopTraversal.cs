using System.Diagnostics;
using System.Globalization;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

/// <summary>
/// P2S5C3-only Desktop acceptance driver.  It changes camera demand, never
/// renderer authority: the production moving-billboard coordinator still owns
/// selection, preparation, current+incoming residency, and publication.
/// </summary>
internal sealed class ProductionBillboardDesktopTraversal
{
    private readonly record struct AltitudeTarget(double Metres, int Level, string Label);

    private enum Phase
    {
        DeepOrbit,
        Descent,
        HorizonRotation,
        FinestSnaps,
        ScaleOut,
        ScaleIn,
        AnchoredWarpUp,
        AnchoredWarpHold,
        AnchoredWarpDown,
        WarpDiagnosticOne,
        WarpDiagnosticModerateUp,
        WarpDiagnosticModerate,
        WarpDiagnosticHighUp,
        WarpDiagnosticHigh,
        WarpDiagnosticDown,
        UnanchoredWarpUp,
        UnanchoredWarpHold,
        UnanchoredWarpDown,
        ZeroVisibleNearSurface,
        ZeroVisibleRetreat,
        ZeroVisibleOffEarthHold,
        ZeroVisibleReentry,
        DirectionalVisibility,
        Retreat,
        Reapproach,
        FinalSettle,
        Complete
    }

    private const int HorizonFrames = 180;
    private const int HorizonDiagnosticFrames = 600;
    private const int CoverageAbFrames = 40;
    private const int RasterDiagnosticFrames = 120;
    private const int RequiredSnaps = 10;
    private const int WarpHoldFrames = 20;
    private const int WarpDiagnosticHoldFrames = 240;
    private const int ZeroVisibleOffEarthHoldFrames = 60;
    private const int ZeroVisibleReentryFrames = 180;
    private const int DirectionalSweepFrames = 360;
    private const int DirectionalGridYawSteps = 90;
    private const int DirectionalGridPitchSteps = 9;
    private const int DirectionalHoldFrames = 180;
    private static readonly int WarpDiagnosticModerateIndex =
        SimulationSpeedPresets.IndexOf(new SimulationRate(600, 1));
    private const int FinalSettleFrames = 120;
    private readonly bool _horizonDiagnosticOnly;
    private readonly bool _warpDiagnosticOnly;
    private readonly bool _zeroVisibleDiagnosticOnly;
    private readonly bool _residencyDiagnosticOnly;
    private readonly bool _directionalDiagnosticOnly;
    private readonly bool _directionalSphereOnly;
    private readonly bool _directionalGrid;
    private readonly bool _fixedDiagnosticTime;
    private readonly int _directionalLevel;
    private readonly double? _directionalYawRadians;
    private readonly double _directionalPitchRadians;
    private readonly string _coverageDiagnostic;
    private readonly string _rasterDiagnostic;
    private readonly int? _frozenHorizonFrame;
    private readonly IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> _levels;
    private readonly double[] _representativeAltitude;
    private readonly AltitudeTarget[] _descentTargets;
    private readonly bool[] _visited = new bool[18];
    private readonly long _started = Stopwatch.GetTimestamp();
    private Phase _phase;
    private int _phaseFrames;
    private int _settledFrames;
    private int _snapCount;
    private int _scaleReversals;
    private int _descentTargetIndex;
    private int _currentLevel = 4;
    private uint _lastPupilGeneration;
    private ulong _observedPublications;
    private bool _publicationFrame;
    private Double3 _direction = RelaxedCubeSphereProjection.UnitDirection(
        CubeSphereFace.PositiveZ, 2047.5d / 4096d, 864.5d / 4096d);
    private Double3 _inertialRadialRoot;
    private Double3 _lastCameraBody;
    private DoubleQuaternion _lastBodyOrientation = DoubleQuaternion.Identity;
    private bool _inertialRadialCaptured;
    private double _maximumPupilError;
    private double _maximumHeightParity;
    private double _maximumNormalParity;
    private ulong _zeroOwnerFrames;
    private ulong _overlapOwnerFrames;
    private ulong _staleGenerationDraws;
    private ulong _maximumResidentBytes;
    private ulong _maximumPeakResidentBytes;
    private ulong _warpPhasePublications;
    private uint _warpPhasePupil;
    private uint _warpPhaseFrameIdentity;
    private ulong _zeroVisiblePublishedGeneration;
    private ulong _zeroVisiblePublicationCount;
    private bool _zeroVisibleSurfaceAttached;
    private long _frames;

    internal ProductionBillboardDesktopTraversal(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        _levels = levels;
        _representativeAltitude = ResolveRepresentativeAltitudes(levels);
        _descentTargets = ResolveDescentTargets(levels, _representativeAltitude);
        _horizonDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5C3_HORIZON_ONLY") == "1";
        _warpDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5E_WARP_ONLY") == "1";
        _zeroVisibleDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_ZERO_VISIBLE_ONLY") == "1";
        _residencyDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_RESIDENCY_ONLY") == "1";
        _directionalDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_ONLY") == "1";
        _directionalGrid = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_GRID") == "1";
        var directionalLevel = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_LEVEL");
        _directionalLevel = directionalLevel is null ? 16 :
            int.TryParse(directionalLevel, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var parsedDirectionalLevel) && parsedDirectionalLevel is >= 0 and < 18
                ? parsedDirectionalLevel
                : throw new InvalidOperationException(
                    "NOVACORE_P2S5F_DIRECTIONAL_LEVEL must be an integer in [0, 17].");
        var directionalVisibility = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_VISIBILITY") ?? "narrow";
        if (directionalVisibility is not ("sphere" or "narrow"))
            throw new InvalidOperationException(
                "NOVACORE_P2S5F_DIRECTIONAL_VISIBILITY must be sphere or narrow.");
        _directionalSphereOnly = directionalVisibility == "sphere";
        var directionalYaw = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS");
        if (directionalYaw is not null)
        {
            if (!double.TryParse(directionalYaw, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var parsedYaw) || !double.IsFinite(parsedYaw))
                throw new InvalidOperationException(
                    "NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS must be finite.");
            _directionalYawRadians = parsedYaw;
        }
        var directionalPitch = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS");
        _directionalPitchRadians = directionalPitch is null
            ? -.035d
            : double.TryParse(directionalPitch, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var parsedPitch) &&
              double.IsFinite(parsedPitch) && Math.Abs(parsedPitch) < Math.PI/2d
                ? parsedPitch
                : throw new InvalidOperationException(
                    "NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS must be finite and inside (-pi/2, pi/2).");
        _coverageDiagnostic = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC") ?? string.Empty;
        _rasterDiagnostic = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5F_RASTER_DIAGNOSTIC") ?? string.Empty;
        var frozenHorizonText = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5E_HORIZON_FRAME");
        if (frozenHorizonText is not null)
        {
            if (!_horizonDiagnosticOnly || !int.TryParse(frozenHorizonText,
                    out var frozenHorizonFrame) ||
                frozenHorizonFrame < 0 || frozenHorizonFrame >= HorizonDiagnosticFrames)
                throw new InvalidOperationException(
                    "NOVACORE_P2S5E_HORIZON_FRAME requires horizon-only mode and an integer in [0, 599].");
            _frozenHorizonFrame = frozenHorizonFrame;
        }
        if (_coverageDiagnostic is not ("" or "normal" or "force-tes1" or
            "bypass-horizon-reject" or "bypass-screen-reject" or "bypass-culling"))
            throw new InvalidOperationException(
                "NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC must be normal, force-tes1, " +
                "bypass-horizon-reject, bypass-screen-reject, or bypass-culling.");
        if (_rasterDiagnostic is not ("" or "owner" or "no-face-cull" or
            "opposite-face" or "no-depth"))
            throw new InvalidOperationException(
                "NOVACORE_P2S5F_RASTER_DIAGNOSTIC must be owner, no-face-cull, " +
                "opposite-face, or no-depth.");
        if (_rasterDiagnostic.Length != 0 && !_horizonDiagnosticOnly)
            throw new InvalidOperationException(
                "NOVACORE_P2S5F_RASTER_DIAGNOSTIC requires horizon-only mode.");
        if ((_horizonDiagnosticOnly ? 1 : 0)+(_warpDiagnosticOnly ? 1 : 0)+
            (_zeroVisibleDiagnosticOnly ? 1 : 0)+(_residencyDiagnosticOnly ? 1 : 0)+
            (_directionalDiagnosticOnly ? 1 : 0) > 1)
            throw new InvalidOperationException(
                "Horizon, warp, zero-visible, residency, and directional diagnostics are mutually exclusive.");
        if (_horizonDiagnosticOnly) _phase = Phase.HorizonRotation;
        if (_warpDiagnosticOnly) _phase = Phase.WarpDiagnosticOne;
        if (_zeroVisibleDiagnosticOnly) _phase = Phase.ZeroVisibleNearSurface;
        if (_directionalDiagnosticOnly) _phase = Phase.DirectionalVisibility;
        _fixedDiagnosticTime = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME") == "1";
        if (_fixedDiagnosticTime && !(_directionalDiagnosticOnly || _horizonDiagnosticOnly))
            throw new InvalidOperationException(
                "NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME requires a directional or horizon diagnostic.");
        if (_fixedDiagnosticTime)
            Console.WriteLine("P2S5G fixed diagnostic time: pause before first simulation advance; physical traversal unchanged.");
    }

    internal bool Failed { get; private set; }
    internal bool Complete => _phase == Phase.Complete;
    internal uint NativeFlags => 1u | 2u |
        (_phase == Phase.FinestSnaps ? 4u : 0u) |
        (_phase is Phase.DeepOrbit or Phase.Descent or Phase.ScaleOut or Phase.ScaleIn or
            Phase.Retreat or Phase.Reapproach ? 8u : 0u) |
        (_publicationFrame ? 16u : 0u) |
        (_coverageDiagnostic == "force-tes1" ? 32u : 0u) |
        (_coverageDiagnostic == "bypass-screen-reject" ? 64u : 0u) |
        (_horizonDiagnosticOnly || _zeroVisibleDiagnosticOnly ? 128u : 0u) |
        (_coverageDiagnostic == "bypass-horizon-reject" ? 256u : 0u) |
        (_coverageDiagnostic == "bypass-culling" ? 512u : 0u) |
        (_rasterDiagnostic == "no-face-cull" ? 1024u : 0u) |
        (_rasterDiagnostic == "opposite-face" ? 2048u : 0u) |
        (_rasterDiagnostic == "no-depth" ? 4096u : 0u) |
        (_rasterDiagnostic.Length != 0 ? 8192u : 0u) |
        (_directionalDiagnosticOnly && _directionalSphereOnly ? 16384u : 0u) |
        (_directionalDiagnosticOnly && !_directionalSphereOnly ? 32768u : 0u);
    internal string FinalReport { get; private set; } = string.Empty;

    internal NativeInputState PrepareInput(in NativeInputState real, SolarSystemScene scene)
    {
        var input = new NativeInputState
        {
            DeltaSeconds = Math.Clamp(real.DeltaSeconds, 1f / 240f, 1f / 30f),
            ViewportWidthPixels = real.ViewportWidthPixels,
            ViewportHeightPixels = real.ViewportHeightPixels
        };
        if (_fixedDiagnosticTime && !scene.IsPaused) input.PauseToggle = 1;
        if ((_phase is Phase.AnchoredWarpUp or Phase.UnanchoredWarpUp or
                Phase.WarpDiagnosticHighUp) &&
            scene.SpeedPresetIndex < SimulationSpeedPresets.Count - 1)
            input.RateIncrease = 1;
        if (_phase == Phase.WarpDiagnosticModerateUp &&
            scene.SpeedPresetIndex < WarpDiagnosticModerateIndex)
            input.RateIncrease = 1;
        if ((_phase is Phase.AnchoredWarpDown or Phase.UnanchoredWarpDown or
                Phase.WarpDiagnosticDown) &&
            scene.SpeedPresetIndex > SimulationSpeedPresets.IndexOf(SimulationRate.One))
            input.RateDecrease = 1;
        return input;
    }

    internal void ApplyPose(CameraState camera, SolarSystemScene scene)
    {
        if (Complete) return;
        var body = scene.FocusedBody;
        var altitude = TargetAltitude();
        var direction = _direction;
        var unanchored = _phase is Phase.UnanchoredWarpUp or Phase.UnanchoredWarpHold or
            Phase.UnanchoredWarpDown;
        if (unanchored)
        {
            if (!_inertialRadialCaptured)
            {
                _inertialRadialRoot = body.BodyFixedToRoot.Rotate(direction).Normalized();
                _inertialRadialCaptured = true;
            }
            direction = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(_inertialRadialRoot).Normalized();
        }
        var physicalHeight = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
            PlanetaryTerrainDefinition.EarthProductionCubeV5, direction);
        var cameraBody = direction * (body.RadiusMetres + physicalHeight + altitude);
        camera.Position = camera.Position with
        {
            Value = body.Position.Value + body.BodyFixedToRoot.Rotate(cameraBody)
        };
        var yaw = _phase == Phase.DirectionalVisibility
            ? DirectionalYaw()
            : _phase == Phase.HorizonRotation && _rasterDiagnostic.Length == 0
            ? Math.Tau * (_frozenHorizonFrame ?? _phaseFrames) /
              (_horizonDiagnosticOnly ? HorizonDiagnosticFrames : HorizonFrames)
            : .18d;
        var bodyOrientation = _phase is Phase.ZeroVisibleRetreat or
                                        Phase.ZeroVisibleOffEarthHold
            ? PlanetarySurfaceFrame.AtDirection(direction).LookOrientation(.18d, .55d)
            : altitude <= 100_000d
            ? PlanetarySurfaceFrame.AtDirection(direction).LookOrientation(yaw,
                _phase == Phase.DirectionalVisibility ? DirectionalPitch() : -.035d)
            : LookTowardCenter(direction);
        camera.Orientation = (body.BodyFixedToRoot * bodyOrientation).Normalized();
        if (_zeroVisibleDiagnosticOnly && !_zeroVisibleSurfaceAttached)
            _zeroVisibleSurfaceAttached = scene.TryAttachSurfaceCamera(camera);
        _lastCameraBody = cameraBody;
        _lastBodyOrientation = bodyOrientation;
    }

    internal bool Observe(PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        in PlanetaryProductionBillboardMovingTelemetry telemetry, SolarSystemScene scene)
    {
        _frames++;
        _phaseFrames++;
        _publicationFrame = telemetry.Publications != _observedPublications;
        _observedPublications = telemetry.Publications;
        if (telemetry.CurrentLevel >= 0)
        {
            _currentLevel = telemetry.CurrentLevel;
            _visited[telemetry.CurrentLevel] = true;
        }
        _maximumPupilError = Math.Max(_maximumPupilError, telemetry.PupilAngularErrorRadians);
        _zeroOwnerFrames = Math.Max(_zeroOwnerFrames, telemetry.ZeroOwnerFrames);
        _overlapOwnerFrames = Math.Max(_overlapOwnerFrames, telemetry.OverlapOwnerFrames);
        _staleGenerationDraws = Math.Max(_staleGenerationDraws, telemetry.StaleGenerationDraws);
        _maximumResidentBytes = Math.Max(_maximumResidentBytes, telemetry.ResidentGpuBytes);
        _maximumPeakResidentBytes = Math.Max(_maximumPeakResidentBytes, telemetry.PeakResidentGpuBytes);

        if (runtime.Current is { } current)
        {
            _maximumHeightParity = Math.Max(_maximumHeightParity,
                current.Physical.MaximumCpuHeightErrorMetres);
            _maximumNormalParity = Math.Max(_maximumNormalParity,
                current.Physical.MaximumCpuNormalErrorRadians);
            if (_lastPupilGeneration == 0) _lastPupilGeneration = current.Pupil.Generation;
        }
        if (_zeroOwnerFrames != 0 || _overlapOwnerFrames != 0 || _staleGenerationDraws != 0)
            return Fail("owner/generation invariant failed");

        if (_warpDiagnosticOnly) return ObserveWarpDiagnostic(runtime, telemetry, scene);
        if (_zeroVisibleDiagnosticOnly)
            return ObserveZeroVisibleDiagnostic(runtime, telemetry, scene);
        if (_directionalDiagnosticOnly)
            return ObserveDirectionalDiagnostic(runtime, telemetry);

        if (_rasterDiagnostic.Length != 0 &&
            (runtime.Current?.Topology.Level != 8 || runtime.ReplacementInFlight))
        {
            _phaseFrames = 0;
            return false;
        }

        if (_horizonDiagnosticOnly && (_phaseFrames == 1 || _phaseFrames % 10 == 0))
        {
            Console.WriteLine($"P2S5C3 coverage pose: diagnostic={(_coverageDiagnostic.Length == 0 ? "baseline" : _coverageDiagnostic)}; " +
                $"phaseFrame={_phaseFrames}; frozenHorizonFrame={_frozenHorizonFrame?.ToString() ?? "none"}; " +
                $"yaw={(_rasterDiagnostic.Length == 0 ? Math.Tau * (_frozenHorizonFrame ?? _phaseFrames) / HorizonDiagnosticFrames : .18d):F9}rad; " +
                $"level=L{telemetry.CurrentLevel}; publication={telemetry.Publications}; " +
                $"cameraBody=({_lastCameraBody.X:R},{_lastCameraBody.Y:R},{_lastCameraBody.Z:R}); " +
                $"orientationBody=({_lastBodyOrientation.X:R},{_lastBodyOrientation.Y:R},{_lastBodyOrientation.Z:R},{_lastBodyOrientation.W:R})");
        }

        switch (_phase)
        {
            case Phase.DeepOrbit when SettledAt(runtime, 0):
                Next(Phase.Descent, "deep-orbit");
                break;
            case Phase.Descent when AdvanceDescent(runtime,
                _residencyDiagnosticOnly ? Phase.Retreat : Phase.HorizonRotation):
                break;
            case Phase.HorizonRotation when _phaseFrames >=
                                                   (_horizonDiagnosticOnly
                                                       ? (_rasterDiagnostic.Length != 0
                                                           ? RasterDiagnosticFrames
                                                           : _coverageDiagnostic.Length == 0
                                                           ? (_frozenHorizonFrame.HasValue
                                                               ? WarpDiagnosticHoldFrames
                                                               : HorizonDiagnosticFrames)
                                                           : (_frozenHorizonFrame.HasValue
                                                               ? WarpDiagnosticHoldFrames
                                                               : CoverageAbFrames))
                                                       : HorizonFrames):
                if (_horizonDiagnosticOnly)
                {
                    FinalReport = $"P2S5C3 horizon diagnostic PASS: mode={(_coverageDiagnostic.Length == 0 ? "baseline" : _coverageDiagnostic)}; " +
                        $"frames={_frames}; level=L{telemetry.CurrentLevel}; generation={telemetry.Publications}; " +
                        $"pupilErrorMax={_maximumPupilError:E9}rad; heightParityMax={_maximumHeightParity:E9}m; " +
                        $"normalParityMax={_maximumNormalParity:E9}rad; zeroOwner={_zeroOwnerFrames}; " +
                        $"overlap={_overlapOwnerFrames}; stale={_staleGenerationDraws}; " +
                        $"rasterDiagnostic={(_rasterDiagnostic.Length == 0 ? "none" : _rasterDiagnostic)}";
                    _phase = Phase.Complete;
                    return true;
                }
                BeginSnap(runtime);
                Next(Phase.FinestSnaps, "10m-horizon-rotation");
                break;
            case Phase.FinestSnaps:
                if (runtime.Current is { } snapped && !runtime.ReplacementInFlight &&
                    snapped.Pupil.Generation != _lastPupilGeneration)
                {
                    _lastPupilGeneration = snapped.Pupil.Generation;
                    _snapCount++;
                    Console.WriteLine($"P2S5C3 finest snap: count={_snapCount}; pupil={snapped.Pupil.Generation}; " +
                        $"reused={snapped.Reuse.ReusedSamples}; new={snapped.Reuse.NewSamples}; " +
                        $"reuse={snapped.Reuse.ReusePercent:F3}%; prepareMs={snapped.PreparationMilliseconds:F6}; " +
                        $"pupilMs={telemetry.LastPupilMilliseconds:F6}; topologyUploads={telemetry.TopologyUploads}");
                    if (_residencyDiagnosticOnly)
                        Next(Phase.FinalSettle, "persistent-resource-same-level-snap");
                    else if (_snapCount >= RequiredSnaps) Next(Phase.ScaleOut, "sustained-L17-snaps");
                    else BeginSnap(runtime);
                }
                break;
            case Phase.ScaleOut when SettledAt(runtime, 16):
                _scaleReversals++;
                Next(Phase.ScaleIn, $"scale-reversal-{_scaleReversals}-out");
                break;
            case Phase.ScaleIn when SettledAt(runtime, 17):
                if (_scaleReversals < 3) Next(Phase.ScaleOut, $"scale-reversal-{_scaleReversals}-in");
                else Next(Phase.AnchoredWarpUp, "scale-reversals-complete");
                break;
            case Phase.AnchoredWarpUp when scene.SpeedPresetIndex == SimulationSpeedPresets.Count - 1:
                Next(Phase.AnchoredWarpHold, "anchored-warp-maximum");
                break;
            case Phase.AnchoredWarpHold when _phaseFrames >= WarpHoldFrames:
                Next(Phase.AnchoredWarpDown, "anchored-warp-hold");
                break;
            case Phase.AnchoredWarpDown when scene.SpeedPresetIndex == SimulationSpeedPresets.IndexOf(SimulationRate.One):
                _inertialRadialCaptured = false;
                Next(Phase.UnanchoredWarpUp, "anchored-warp-complete");
                break;
            case Phase.UnanchoredWarpUp when scene.SpeedPresetIndex == SimulationSpeedPresets.Count - 1:
                Next(Phase.UnanchoredWarpHold, "unanchored-warp-maximum");
                break;
            case Phase.UnanchoredWarpHold when _phaseFrames >= WarpHoldFrames:
                Next(Phase.UnanchoredWarpDown, "unanchored-warp-hold");
                break;
            case Phase.UnanchoredWarpDown when scene.SpeedPresetIndex == SimulationSpeedPresets.IndexOf(SimulationRate.One):
                Next(Phase.Retreat, "unanchored-warp-complete");
                break;
            case Phase.Retreat when SettledAt(runtime, 0):
                _descentTargetIndex = 0;
                Next(Phase.Reapproach, "retreat-to-orbit");
                break;
            case Phase.Reapproach when AdvanceDescent(runtime,
                _residencyDiagnosticOnly ? Phase.FinestSnaps : Phase.FinalSettle):
                if(_residencyDiagnosticOnly)BeginSnap(runtime);
                break;
            case Phase.FinalSettle when _phaseFrames >= FinalSettleFrames &&
                                             !runtime.ReplacementInFlight:
                if(_residencyDiagnosticOnly)CompleteResidencyRun(runtime,telemetry);
                else CompleteRun(runtime);
                return true;
        }
        return false;
    }

    private bool ObserveZeroVisibleDiagnostic(
        PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        in PlanetaryProductionBillboardMovingTelemetry telemetry,
        SolarSystemScene scene)
    {
        switch (_phase)
        {
            case Phase.ZeroVisibleNearSurface when SettledAt(runtime, 17):
                if (!_zeroVisibleSurfaceAttached ||
                    scene.CurrentCameraReferenceAuthority != CameraReferenceAuthority.SurfaceRelative)
                    return Fail("near-surface camera did not enter SurfaceRelative authority");
                Console.WriteLine($"P2S5F zero-visible phase: near-surface anchored; " +
                    $"level=L17; generation={runtime.Current!.PublicationGeneration}; " +
                    $"cameraAuthority={scene.CurrentCameraReferenceAuthority}");
                Next(Phase.ZeroVisibleRetreat, "anchored-near-surface");
                break;
            case Phase.ZeroVisibleRetreat when SettledAt(runtime, 14):
                _zeroVisiblePublishedGeneration = runtime.Current!.PublicationGeneration;
                _zeroVisiblePublicationCount = telemetry.Publications;
                Console.WriteLine($"P2S5F zero-visible phase: off-Earth cross-level publication; " +
                    $"level=L14; generation={_zeroVisiblePublishedGeneration}; " +
                    $"publications={_zeroVisiblePublicationCount}; owners=1; zeroOwner={telemetry.ZeroOwnerFrames}; " +
                    $"overlap={telemetry.OverlapOwnerFrames}; stale={telemetry.StaleGenerationDraws}");
                Next(Phase.ZeroVisibleOffEarthHold, "zero-visible-L14-publication");
                break;
            case Phase.ZeroVisibleOffEarthHold when _phaseFrames >= ZeroVisibleOffEarthHoldFrames:
                if (runtime.Current?.PublicationGeneration != _zeroVisiblePublishedGeneration ||
                    telemetry.Publications != _zeroVisiblePublicationCount)
                    return Fail("off-Earth hold changed the published generation");
                Next(Phase.ZeroVisibleReentry, "off-Earth-hold");
                break;
            case Phase.ZeroVisibleReentry when _phaseFrames >= ZeroVisibleReentryFrames &&
                                                    !runtime.ReplacementInFlight:
                if (runtime.Current?.PublicationGeneration != _zeroVisiblePublishedGeneration ||
                    telemetry.Publications != _zeroVisiblePublicationCount)
                    return Fail("Earth re-entry required a topology republication");
                FinalReport = $"P2S5F zero-visible publication replay PASS: " +
                    $"anchored=true; offEarthLevel=L14; generation={_zeroVisiblePublishedGeneration}; " +
                    $"publicationCount={_zeroVisiblePublicationCount}; reentrySameGeneration=true; " +
                    $"zeroOwner={telemetry.ZeroOwnerFrames}; overlap={telemetry.OverlapOwnerFrames}; " +
                    $"stale={telemetry.StaleGenerationDraws}; topologyUploads={telemetry.TopologyUploads}; " +
                    $"crossScalePreparationPreserved=true";
                _phase = Phase.Complete;
                return true;
        }
        return false;
    }

    private bool ObserveWarpDiagnostic(PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        in PlanetaryProductionBillboardMovingTelemetry telemetry, SolarSystemScene scene)
    {
        if (runtime.Current?.Topology.Level != 17 || runtime.ReplacementInFlight)
        {
            _phaseFrames = 0;
            return false;
        }
        if (_phaseFrames == 1)
        {
            _warpPhasePublications = telemetry.Publications;
            _warpPhasePupil = telemetry.PupilGeneration;
            _warpPhaseFrameIdentity = telemetry.PupilFrameIdentity;
            Console.WriteLine($"P2S5E anchored warp phase begin: phase={_phase}; rate={scene.Rate.Numerator}:{scene.Rate.Denominator}; " +
                $"level=L{telemetry.CurrentLevel}; pupil={telemetry.PupilGeneration}; frameIdentity={telemetry.PupilFrameIdentity}; " +
                $"publications={telemetry.Publications}; cameraBody=({_lastCameraBody.X:R},{_lastCameraBody.Y:R},{_lastCameraBody.Z:R})");
        }

        switch (_phase)
        {
            case Phase.WarpDiagnosticOne when _phaseFrames >= WarpDiagnosticHoldFrames:
                CompleteWarpPlateau(telemetry, scene);
                Next(Phase.WarpDiagnosticModerateUp, "P2S5E-1x-hold");
                break;
            case Phase.WarpDiagnosticModerateUp when scene.SpeedPresetIndex >= WarpDiagnosticModerateIndex:
                Next(Phase.WarpDiagnosticModerate, "P2S5E-moderate-selected");
                break;
            case Phase.WarpDiagnosticModerate when _phaseFrames >= WarpDiagnosticHoldFrames:
                CompleteWarpPlateau(telemetry, scene);
                Next(Phase.WarpDiagnosticHighUp, "P2S5E-moderate-hold");
                break;
            case Phase.WarpDiagnosticHighUp when scene.SpeedPresetIndex == SimulationSpeedPresets.Count - 1:
                Next(Phase.WarpDiagnosticHigh, "P2S5E-high-selected");
                break;
            case Phase.WarpDiagnosticHigh when _phaseFrames >= WarpDiagnosticHoldFrames:
                CompleteWarpPlateau(telemetry, scene);
                Next(Phase.WarpDiagnosticDown, "P2S5E-high-hold");
                break;
            case Phase.WarpDiagnosticDown when scene.SpeedPresetIndex ==
                                                   SimulationSpeedPresets.IndexOf(SimulationRate.One):
                var timing = runtime.TimingSummary;
                FinalReport = $"P2S5E anchored warp diagnostic PASS: frames={_frames}; altitude=10.004m; " +
                    $"pupil={telemetry.PupilGeneration}; frameIdentity={telemetry.PupilFrameIdentity}; " +
                    $"publications={telemetry.Publications}; topologyUploads={telemetry.TopologyUploads}; " +
                    $"zeroOwner={telemetry.ZeroOwnerFrames}; overlap={telemetry.OverlapOwnerFrames}; " +
                    $"stale={telemetry.StaleGenerationDraws}; callback={Format(timing.Callback)}; " +
                    $"selector={Format(timing.Selector)}; pupilSnap={Format(timing.PupilAndSnap)}; " +
                    $"scheduling={Format(timing.DemandScheduling)}; physical={Format(timing.PhysicalPreparation)}; " +
                    $"gpuPrepare={Format(timing.GpuPreparation)}; publication={Format(timing.Publication)}";
                _phase = Phase.Complete;
                return true;
        }
        return false;
    }

    private void CompleteWarpPlateau(in PlanetaryProductionBillboardMovingTelemetry telemetry,
        SolarSystemScene scene)
    {
        Console.WriteLine($"P2S5E anchored warp phase end: phase={_phase}; rate={scene.Rate.Numerator}:{scene.Rate.Denominator}; " +
            $"frames={_phaseFrames}; pupil={_warpPhasePupil}->{telemetry.PupilGeneration}; " +
            $"frameIdentity={_warpPhaseFrameIdentity}->{telemetry.PupilFrameIdentity}; " +
            $"publicationDelta={telemetry.Publications-_warpPhasePublications}; topologyUploads={telemetry.TopologyUploads}; " +
            $"selectorLast={telemetry.LastSelectorMilliseconds:F6}ms; pupilLast={telemetry.LastPupilMilliseconds:F6}ms; " +
            $"scheduleLast={telemetry.LastSchedulingMilliseconds:F6}ms; prepareLast={telemetry.LastPreparationMilliseconds:F6}ms; " +
            $"publicationLast={telemetry.LastPublicationMilliseconds:F6}ms");
    }

    private double TargetAltitude() => _phase switch
    {
        Phase.HorizonRotation when _rasterDiagnostic.Length != 0 => 700_000d,
        Phase.ZeroVisibleRetreat => _currentLevel > 14
            ? _representativeAltitude[_currentLevel - 1]
            : _representativeAltitude[14],
        Phase.ZeroVisibleOffEarthHold or Phase.ZeroVisibleReentry =>
            _representativeAltitude[14],
        Phase.DirectionalVisibility => _representativeAltitude[_directionalLevel],
        Phase.DeepOrbit => _representativeAltitude[0],
        Phase.Retreat => _currentLevel <= 1
            ? _representativeAltitude[0] * 2d
            : _representativeAltitude[_currentLevel - 2],
        // This phase waits for settled L16; requesting L15 can pass through L16
        // before its settle counter completes and leave the driver waiting.
        Phase.ScaleOut => _representativeAltitude[16],
        Phase.Descent or Phase.Reapproach => _descentTargets[_descentTargetIndex].Metres,
        Phase.ScaleIn => _representativeAltitude[17],
        _ => 10.004d
    };

    private bool ObserveDirectionalDiagnostic(
        PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        in PlanetaryProductionBillboardMovingTelemetry telemetry)
    {
        if (runtime.Current?.Topology.Level != _directionalLevel || runtime.ReplacementInFlight)
        {
            _phaseFrames = 0;
            return false;
        }
        var yaw = DirectionalYaw();
        var pitch = DirectionalPitch();
        if (_phaseFrames == 1 || _phaseFrames % 10 == 0)
            Console.WriteLine($"P2S5F directional pose: mode={(_directionalSphereOnly ? "sphere-only" : "broad+narrow")}; " +
                $"phaseFrame={_phaseFrames}; yaw={yaw:R}; pitch={pitch:R}; level=L{telemetry.CurrentLevel}; " +
                $"generation={runtime.Current.PublicationGeneration}; " +
                $"cameraBody=({_lastCameraBody.X:R},{_lastCameraBody.Y:R},{_lastCameraBody.Z:R}); " +
                $"orientationBody=({_lastBodyOrientation.X:R},{_lastBodyOrientation.Y:R}," +
                $"{_lastBodyOrientation.Z:R},{_lastBodyOrientation.W:R})");
        var required = _directionalGrid
            ? DirectionalGridYawSteps * DirectionalGridPitchSteps
            : _directionalYawRadians.HasValue ? DirectionalHoldFrames : DirectionalSweepFrames;
        if (_phaseFrames < required) return false;
        FinalReport = $"P2S5F directional visibility PASS: " +
            $"mode={(_directionalSphereOnly ? "sphere-only" : "broad+narrow")}; " +
            $"frames={_frames}; measuredFrames={_phaseFrames}; level=L{telemetry.CurrentLevel}; " +
            $"yaw={(_directionalYawRadians?.ToString("R", CultureInfo.InvariantCulture) ?? "sweep")}; " +
            $"pitch={(_directionalGrid ? "grid" : _directionalPitchRadians.ToString("R", CultureInfo.InvariantCulture))}; " +
            $"zeroOwner={_zeroOwnerFrames}; overlap={_overlapOwnerFrames}; stale={_staleGenerationDraws}; " +
            $"heightParityMax={_maximumHeightParity:E9}m; normalParityMax={_maximumNormalParity:E9}rad";
        _phase = Phase.Complete;
        return true;
    }

    private double DirectionalYaw()
    {
        if (!_directionalGrid)
            return _directionalYawRadians ?? Math.Tau * _phaseFrames / DirectionalSweepFrames;
        return Math.Tau * (_phaseFrames % DirectionalGridYawSteps) / DirectionalGridYawSteps;
    }

    private double DirectionalPitch()
    {
        if (!_directionalGrid) return _directionalPitchRadians;
        var row = Math.Min(_phaseFrames / DirectionalGridYawSteps,
            DirectionalGridPitchSteps - 1);
        return -1.4d + row * (2.8d / (DirectionalGridPitchSteps - 1));
    }

    private bool AdvanceDescent(PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        Phase completedPhase)
    {
        var target = _descentTargets[_descentTargetIndex];
        if (runtime.Current is null || runtime.ReplacementInFlight)
        {
            _settledFrames = 0;
            return false;
        }
        if (++_settledFrames < 3) return false;
        var actualLevel = runtime.Current.Topology.Level;
        Console.WriteLine($"P2S5C3 altitude checkpoint: label={target.Label}; " +
            $"altitude={target.Metres:F6}m; selected=L{actualLevel}; expected=L{target.Level}");
        _descentTargetIndex++;
        _settledFrames = 0;
        if (_descentTargetIndex < _descentTargets.Length) return false;
        _descentTargetIndex = _descentTargets.Length - 1;
        Next(completedPhase, completedPhase == Phase.HorizonRotation
            ? "all-level continuous descent" : "continuous reapproach");
        return true;
    }

    private bool SettledAt(PlanetaryProductionSphericalBillboardMovingRuntime runtime, int level)
    {
        if (runtime.Current?.Topology.Level != level || runtime.ReplacementInFlight)
        {
            _settledFrames = 0;
            return false;
        }
        return ++_settledFrames >= 3;
    }

    private void BeginSnap(PlanetaryProductionSphericalBillboardMovingRuntime runtime)
    {
        if (runtime.Current is not { } current) return;
        var pupil = current.Pupil;
        _lastPupilGeneration = pupil.Generation;
        var angle = _levels[17].Snap.PupilCellRadians *
            (_levels[17].Snap.CandidateShiftMultiple + 1);
        var sign = (_snapCount & 1) == 0 ? 1d : -1d;
        _direction = (pupil.PivotDirection + pupil.Tangent.East * (sign * angle) +
            pupil.Tangent.North * (angle * .25d)).Normalized();
    }

    private void CompleteRun(PlanetaryProductionSphericalBillboardMovingRuntime runtime)
    {
        var missing = string.Join(',', _visited.Select((seen, level) => (seen, level))
            .Where(value => !value.seen).Select(value => $"L{value.level}"));
        Failed = _visited.Any(value => !value) || _snapCount < RequiredSnaps ||
            _scaleReversals < 3 || _zeroOwnerFrames != 0 || _overlapOwnerFrames != 0 ||
            _staleGenerationDraws != 0 || _maximumHeightParity >= 1e-5 ||
            _maximumNormalParity >= 5e-3;
        var timing = runtime.TimingSummary;
        FinalReport = $"P2S5C3 Desktop traversal {(Failed ? "FAIL" : "PASS")}: " +
            $"frames={_frames}; elapsedMs={Stopwatch.GetElapsedTime(_started).TotalMilliseconds:R}; " +
            $"levels={string.Join(',', _visited.Select((seen, level) => seen ? $"L{level}" : $"!L{level}"))}; " +
            $"missing={missing}; snaps={_snapCount}; reversals={_scaleReversals}; " +
            $"publications={runtime.Current?.PublicationGeneration ?? 0}; " +
            $"pupilErrorMax={_maximumPupilError:E9}rad; heightParityMax={_maximumHeightParity:E9}m; " +
            $"normalParityMax={_maximumNormalParity:E9}rad; zeroOwner={_zeroOwnerFrames}; " +
            $"overlap={_overlapOwnerFrames}; stale={_staleGenerationDraws}; " +
            $"residentMax={_maximumResidentBytes}; peakResident={_maximumPeakResidentBytes}; " +
            $"callback={Format(timing.Callback)}; selector={Format(timing.Selector)}; " +
            $"pupilSnap={Format(timing.PupilAndSnap)}; scheduling={Format(timing.DemandScheduling)}; " +
            $"physical={Format(timing.PhysicalPreparation)}; gpuPrepare={Format(timing.GpuPreparation)}; " +
            $"publication={Format(timing.Publication)}";
        _phase = Phase.Complete;
    }

    private void CompleteResidencyRun(PlanetaryProductionSphericalBillboardMovingRuntime runtime,
        in PlanetaryProductionBillboardMovingTelemetry telemetry)
    {
        var missing=string.Join(',',_visited.Select((seen,level)=>(seen,level))
            .Where(value=>!value.seen).Select(value=>$"L{value.level}"));
        Failed=_visited.Any(value=>!value)||_zeroOwnerFrames!=0||_overlapOwnerFrames!=0||
            _staleGenerationDraws!=0||telemetry.TopologyUploads!=18;
        var timing=runtime.TimingSummary;
        using var process=Process.GetCurrentProcess();
        FinalReport=$"P2S5F persistent scale residency {(Failed?"FAIL":"PASS")}: " +
            $"levels={string.Join(',',_visited.Select((seen,level)=>seen?$"L{level}":$"!L{level}"))}; " +
            $"missing={missing}; topologyUploads={telemetry.TopologyUploads}; expectedUniqueUploads=18; " +
            $"revisitUploads=0; publications={telemetry.Publications}; zeroOwner={_zeroOwnerFrames}; " +
            $"overlap={_overlapOwnerFrames}; stale={_staleGenerationDraws}; " +
            $"workingSet={process.WorkingSet64}; peakWorkingSet={process.PeakWorkingSet64}; " +
            $"managedPrepare={Format(timing.PhysicalPreparation)}; callback={Format(timing.Callback)}; " +
            $"selector={Format(timing.Selector)}; scheduling={Format(timing.DemandScheduling)}; " +
            $"publication={Format(timing.Publication)}";
        _phase=Phase.Complete;
    }

    private bool Fail(string message)
    {
        Failed = true;
        FinalReport = $"P2S5C3 Desktop traversal FAIL: {message}; frames={_frames}";
        _phase = Phase.Complete;
        return true;
    }

    private void Next(Phase next, string completed)
    {
        Console.WriteLine($"P2S5C3 phase: completed={completed}; next={next}");
        _phase = next;
        _phaseFrames = 0;
        _settledFrames = 0;
    }

    private static string Format(PlanetaryProductionBillboardTiming value) =>
        $"{value.AverageMilliseconds:F6}/{value.P50Milliseconds:F6}/{value.P95Milliseconds:F6}/" +
        $"{value.P99Milliseconds:F6}/{value.MaximumMilliseconds:F6}ms({value.Samples})";

    private static double[] ResolveRepresentativeAltitudes(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var result = new double[levels.Count];
        var found = new bool[levels.Count];
        var minimum = Enumerable.Repeat(double.PositiveInfinity, levels.Count).ToArray();
        var maximum = new double[levels.Count];
        var logarithmicRange = Math.Log(80_000_000d / 10.004d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            var level = selector.Evaluate(new(altitude, Double3.UnitZ, 3440, 1440,
                Math.PI / 3d, (ulong)sample), false).Level;
            found[level] = true;
            minimum[level] = Math.Min(minimum[level], altitude);
            maximum[level] = Math.Max(maximum[level], altitude);
        }
        if (found.Any(value => !value))
            throw new InvalidOperationException("P2S5C3 could not resolve all 18 representative altitudes.");
        for (var level = 0; level < result.Length; level++)
            result[level] = Math.Sqrt(minimum[level] * maximum[level]);
        return result;
    }

    private static AltitudeTarget[] ResolveDescentTargets(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,
        IReadOnlyList<double> representative)
    {
        var checkpoints = new (double Metres, string Label)[]
        {
            (100_000d, "100-km"), (10_000d, "10-km"), (1_000d, "1-km"),
            (250d, "250-m"), (100d, "100-m"), (10.004d, "10.004-m")
        };
        var candidates = representative.Select((metres, level) =>
                (Metres: metres, Label: $"L{level}-representative"))
            .Concat(checkpoints)
            .OrderByDescending(value => value.Metres)
            .ToArray();
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var result = new List<AltitudeTarget>(candidates.Length);
        foreach (var candidate in candidates)
        {
            if (result.Count != 0 &&
                Math.Abs(result[^1].Metres - candidate.Metres) <= candidate.Metres * 1e-9)
                continue;
            selector.CancelInitialSelectionForTests();
            var level = selector.Evaluate(new(candidate.Metres, Double3.UnitZ, 3440, 1440,
                Math.PI / 3d, (ulong)result.Count), false).Level;
            result.Add(new(candidate.Metres, level, candidate.Label));
        }
        return result.ToArray();
    }

    private static DoubleQuaternion LookTowardCenter(in Double3 radial)
    {
        var forward = -radial.Normalized();
        var reference = Math.Abs(Double3.Dot(forward, Double3.UnitY)) > .99d
            ? Double3.UnitZ : Double3.UnitY;
        var right = Double3.Cross(forward, reference).Normalized();
        var up = Double3.Cross(right, forward).Normalized();
        return QuaternionFromBasis(right, up, -forward);
    }

    private static DoubleQuaternion QuaternionFromBasis(in Double3 x, in Double3 y, in Double3 z)
    {
        var trace = x.X + y.Y + z.Z; double qx, qy, qz, qw;
        if (trace > 0d) { var s = Math.Sqrt(trace + 1d) * 2d; qw = .25d * s; qx = (y.Z - z.Y) / s; qy = (z.X - x.Z) / s; qz = (x.Y - y.X) / s; }
        else if (x.X > y.Y && x.X > z.Z) { var s = Math.Sqrt(1d + x.X - y.Y - z.Z) * 2d; qw = (y.Z - z.Y) / s; qx = .25d * s; qy = (y.X + x.Y) / s; qz = (z.X + x.Z) / s; }
        else if (y.Y > z.Z) { var s = Math.Sqrt(1d + y.Y - x.X - z.Z) * 2d; qw = (z.X - x.Z) / s; qx = (y.X + x.Y) / s; qy = .25d * s; qz = (z.Y + y.Z) / s; }
        else { var s = Math.Sqrt(1d + z.Z - x.X - y.Y) * 2d; qw = (x.Y - y.X) / s; qx = (z.X + x.Z) / s; qy = (z.Y + y.Z) / s; qz = .25d * s; }
        return new DoubleQuaternion(qx, qy, qz, qw).Normalized();
    }
}
