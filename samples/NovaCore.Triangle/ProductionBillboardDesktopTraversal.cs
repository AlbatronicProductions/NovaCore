using System.Diagnostics;
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
        UnanchoredWarpUp,
        UnanchoredWarpHold,
        UnanchoredWarpDown,
        Retreat,
        Reapproach,
        FinalSettle,
        Complete
    }

    private const int HorizonFrames = 180;
    private const int HorizonDiagnosticFrames = 600;
    private const int CoverageAbFrames = 40;
    private const int RequiredSnaps = 10;
    private const int WarpHoldFrames = 20;
    private const int FinalSettleFrames = 120;
    private readonly bool _horizonDiagnosticOnly;
    private readonly string _coverageDiagnostic;
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
    private long _frames;

    internal ProductionBillboardDesktopTraversal(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        _levels = levels;
        _representativeAltitude = ResolveRepresentativeAltitudes(levels);
        _descentTargets = ResolveDescentTargets(levels, _representativeAltitude);
        _horizonDiagnosticOnly = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5C3_HORIZON_ONLY") == "1";
        _coverageDiagnostic = Environment.GetEnvironmentVariable(
            "NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC") ?? string.Empty;
        if (_coverageDiagnostic is not ("" or "force-tes1" or "bypass-screen-reject"))
            throw new InvalidOperationException(
                "NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC must be force-tes1 or bypass-screen-reject.");
        if (_horizonDiagnosticOnly) _phase = Phase.HorizonRotation;
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
        (_horizonDiagnosticOnly ? 128u : 0u);
    internal string FinalReport { get; private set; } = string.Empty;

    internal NativeInputState PrepareInput(in NativeInputState real, SolarSystemScene scene)
    {
        var input = new NativeInputState
        {
            DeltaSeconds = Math.Clamp(real.DeltaSeconds, 1f / 240f, 1f / 30f),
            ViewportWidthPixels = real.ViewportWidthPixels,
            ViewportHeightPixels = real.ViewportHeightPixels
        };
        if ((_phase is Phase.AnchoredWarpUp or Phase.UnanchoredWarpUp) &&
            scene.SpeedPresetIndex < SimulationSpeedPresets.Count - 1)
            input.RateIncrease = 1;
        if ((_phase is Phase.AnchoredWarpDown or Phase.UnanchoredWarpDown) &&
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
        var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
        var cameraBody = direction * (body.RadiusMetres + physical.FinalHeightMetres + altitude);
        camera.Position = camera.Position with
        {
            Value = body.Position.Value + body.BodyFixedToRoot.Rotate(cameraBody)
        };
        var yaw = _phase == Phase.HorizonRotation
            ? Math.Tau * _phaseFrames /
              (_horizonDiagnosticOnly ? HorizonDiagnosticFrames : HorizonFrames)
            : .18d;
        var bodyOrientation = altitude <= 100_000d
            ? PlanetarySurfaceFrame.AtDirection(direction).LookOrientation(yaw, -.035d)
            : LookTowardCenter(direction);
        camera.Orientation = (body.BodyFixedToRoot * bodyOrientation).Normalized();
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

        if (_horizonDiagnosticOnly && (_phaseFrames == 1 || _phaseFrames % 10 == 0))
        {
            Console.WriteLine($"P2S5C3 coverage pose: diagnostic={(_coverageDiagnostic.Length == 0 ? "baseline" : _coverageDiagnostic)}; " +
                $"phaseFrame={_phaseFrames}; yaw={Math.Tau * _phaseFrames / HorizonDiagnosticFrames:F9}rad; " +
                $"level=L{telemetry.CurrentLevel}; publication={telemetry.Publications}; " +
                $"cameraBody=({_lastCameraBody.X:R},{_lastCameraBody.Y:R},{_lastCameraBody.Z:R}); " +
                $"orientationBody=({_lastBodyOrientation.X:R},{_lastBodyOrientation.Y:R},{_lastBodyOrientation.Z:R},{_lastBodyOrientation.W:R})");
        }

        switch (_phase)
        {
            case Phase.DeepOrbit when SettledAt(runtime, 0):
                Next(Phase.Descent, "deep-orbit");
                break;
            case Phase.Descent when AdvanceDescent(runtime, Phase.HorizonRotation):
                break;
            case Phase.HorizonRotation when _phaseFrames >=
                                                   (_horizonDiagnosticOnly
                                                       ? (_coverageDiagnostic.Length == 0
                                                           ? HorizonDiagnosticFrames
                                                           : CoverageAbFrames)
                                                       : HorizonFrames):
                if (_horizonDiagnosticOnly)
                {
                    FinalReport = $"P2S5C3 horizon diagnostic PASS: mode={(_coverageDiagnostic.Length == 0 ? "baseline" : _coverageDiagnostic)}; " +
                        $"frames={_frames}; level=L{telemetry.CurrentLevel}; generation={telemetry.Publications}; " +
                        $"pupilErrorMax={_maximumPupilError:E9}rad; heightParityMax={_maximumHeightParity:E9}m; " +
                        $"normalParityMax={_maximumNormalParity:E9}rad; zeroOwner={_zeroOwnerFrames}; " +
                        $"overlap={_overlapOwnerFrames}; stale={_staleGenerationDraws}";
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
                        $"reuse={snapped.Reuse.ReusePercent:F3}%; prepareMs={snapped.PreparationMilliseconds:F6}");
                    if (_snapCount >= RequiredSnaps) Next(Phase.ScaleOut, "sustained-L17-snaps");
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
            case Phase.Reapproach when AdvanceDescent(runtime, Phase.FinalSettle):
                break;
            case Phase.FinalSettle when _phaseFrames >= FinalSettleFrames &&
                                             !runtime.ReplacementInFlight:
                CompleteRun(runtime);
                return true;
        }
        return false;
    }

    private double TargetAltitude() => _phase switch
    {
        Phase.DeepOrbit => _representativeAltitude[0],
        Phase.Retreat => _currentLevel <= 1
            ? _representativeAltitude[0] * 2d
            : _representativeAltitude[_currentLevel - 2],
        Phase.ScaleOut => _representativeAltitude[15],
        Phase.Descent or Phase.Reapproach => _descentTargets[_descentTargetIndex].Metres,
        Phase.ScaleIn => _representativeAltitude[17],
        _ => 10.004d
    };

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
