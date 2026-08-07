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
    internal const double CharacterStrideNdc = .009d;
    internal const double CellWidthNdc = .0022d;
    internal const double CellHeightNdc = .0028d;
    internal const double LabelOffsetXNdc = .009d;
    internal const double LabelOffsetYNdc = .009d;
    internal const double FocusedLabelOffsetYNdc = .012d;
    internal const double CollisionMarginNdc = .004d;

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
        var view = camera.Orientation.Conjugate().Normalized().Rotate(body.Position.Value - camera.Position.Value);
        var forward = -view.Z;
        if (!view.IsFinite || !double.IsFinite(forward) || forward < camera.Projection.NearClip) return false;
        var scale = 1d / Math.Tan(camera.Projection.VerticalFieldOfViewRadians * .5d);
        var anchorX = scale / camera.Projection.AspectRatio * view.X / forward;
        var anchorY = -scale * view.Y / forward;
        depth = 1d - camera.Projection.NearClip / forward;
        var length = body.Label?.Length ?? 0;
        if (length == 0 || !double.IsFinite(anchorX) || !double.IsFinite(anchorY) || !double.IsFinite(depth) || depth is < 0d or >= 1d) return false;
        var minX = anchorX + LabelOffsetXNdc;
        var minY = anchorY + (focused ? FocusedLabelOffsetYNdc : LabelOffsetYNdc);
        bounds = new SolarScreenRect(
            minX,
            minY,
            minX + (length - 1) * CharacterStrideNdc + 3d * CellWidthNdc,
            minY + 5d * CellHeightNdc);
        return bounds.IsFinite && bounds.MaxX >= -1d && bounds.MinX <= 1d && bounds.MaxY >= -1d && bounds.MinY <= 1d;
    }

    internal static bool Overlaps(in SolarScreenRect left, in SolarScreenRect right) =>
        left.MinX < right.MaxX + CollisionMarginNdc && left.MaxX + CollisionMarginNdc > right.MinX &&
        left.MinY < right.MaxY + CollisionMarginNdc && left.MaxY + CollisionMarginNdc > right.MinY;
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
    internal const double InitialOverviewDistanceAu = 45d;
    internal const double MaximumOverviewDistanceAu = 100d;
    internal const uint LabelVisibleBit = 0x4000_0000u;

    private static readonly Float3[] Colors =
    [
        new(1f, .82f, .35f), new(.48f, .48f, .48f), new(.88f, .72f, .42f), new(.08f, .32f, .72f), new(.62f, .62f, .62f),
        new(.72f, .25f, .14f), new(.72f, .53f, .32f), new(.82f, .68f, .38f), new(.38f, .78f, .86f), new(.12f, .32f, .86f)
    ];
    private static readonly SimulationRate[] RateSteps =
    [
        SimulationRate.One, new(10, 1), new(100, 1), new(1_000, 1), new(5_000, 1), new(10_000, 1), new(50_000, 1)
    ];

    private const uint FocusedOverlayBit = 0x8000_0000u;
    private const double OrbitSensitivity = .002d;
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
    private readonly bool[] _labelBoundsValid = new bool[BodyOrder.Length];
    private readonly SolarScreenRect[] _labelBounds = new SolarScreenRect[BodyOrder.Length];
    private readonly ulong[] _visibleLabelIds = new ulong[BodyOrder.Length];
    private PlanetaryRepresentationHandoff _handoff = new(EarthPlanetaryScene.HandoffConfiguration);
    private PlanetaryRepresentationBlend _blend;
    private int _rateStepIndex;
    private double _orbitDistance;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;

    private SolarSystemScene(
        CelestialSystemDefinition system,
        ReferenceFrameId root,
        int[] traversalIndices,
        Double3[] rootOrbitSamples,
        int[] orbitTraversalIndices,
        double[] orbitPeriods)
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
        _clock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.One);
        DistantBodies = new NativePlanetaryPresentation[BodyOrder.Length];
        OrbitVertices = new NativeOrbitLineVertex[OrbitVertexCount];
        _orbitDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * InitialOverviewDistanceAu;
    }

    internal PlanetaryPresentationSnapshot Presentation { get; private set; } = null!;
    internal NativePlanetaryPresentation[] DistantBodies { get; }
    internal NativeOrbitLineVertex[] OrbitVertices { get; }
    internal ReadOnlySpan<Double3> OrbitRootSamples => _rootOrbitSamples;
    internal int FocusIndex { get; private set; }
    internal int DistantBodyCount { get; private set; }
    internal PlanetaryRepresentationBlend FocusedBlend => _blend;
    internal bool DetailedComputeRequested => _blend.DrawDetailed;
    internal PlanetRenderProxy FocusedBody => Presentation.Bodies[FocusIndex];
    internal SimulationInstant CurrentTime => _clock.CurrentTime;
    internal SimulationRate Rate => _clock.Rate;
    internal bool IsPaused => _clock.IsPaused;
    internal double OrbitDistance => _orbitDistance;
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d, 1e6d, SolAnalyticalDefinition.AstronomicalUnitMetres * MaximumOverviewDistanceAu);
    internal ReadOnlySpan<ulong> VisibleLabelIds => _visibleLabelIds.AsSpan(0, VisibleLabelCount);
    internal int VisibleLabelCount { get; private set; }

    internal static bool TryCreate(ReferenceFrameId root, out SolarSystemScene? scene, out string error)
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
        var candidate = new SolarSystemScene(system, root, traversalIndices, rootOrbitSamples, orbitTraversalIndices, orbitPeriods);
        if (!candidate.TryPublishAt(SimulationInstant.Zero, out error)) return false;
        scene = candidate;
        error = string.Empty;
        return true;
    }

    internal void Update(CameraState camera)
    {
        _blend = _handoff.Update(FocusedBody, camera.Position.Value);
        SelectVisibleLabels(camera);
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
                Enabled = (uint)(index + 1) | (focused ? FocusedOverlayBit : 0u) | (_visibleLabels[index] ? LabelVisibleBit : 0u)
            };
        }
        DistantBodyCount = Presentation.Count;
        UpdateOrbitVertices(camera);
    }

    internal bool Focus(CameraState camera, int index)
    {
        if ((uint)index >= Presentation.Count) return false;
        FocusIndex = index;
        _handoff = new PlanetaryRepresentationHandoff(EarthPlanetaryScene.HandoffConfiguration);
        _orbitDistance = FocusedBody.RadiusMetres * 10d;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = 0d;
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
        rateChanged = false;
        pauseChanged = false;
        if (input.RateDecrease != 0 && _rateStepIndex > 0)
        {
            _rateStepIndex--;
            _clock.TrySetRate(RateSteps[_rateStepIndex]);
            rateChanged = true;
        }
        if (input.RateIncrease != 0 && _rateStepIndex + 1 < RateSteps.Length)
        {
            _rateStepIndex++;
            _clock.TrySetRate(RateSteps[_rateStepIndex]);
            rateChanged = true;
        }
        if (input.PauseToggle != 0)
        {
            if (_clock.IsPaused) _clock.Resume(); else _clock.Pause();
            pauseChanged = true;
        }

        var cameraChanged = false;
        if (input.MouseWheelDetents != 0)
        {
            _orbitDistance = Math.Clamp(
                _orbitDistance * Math.Pow(1.1d, -input.MouseWheelDetents),
                FocusedBody.RadiusMetres * 1.05d,
                SolAnalyticalDefinition.AstronomicalUnitMetres * MaximumOverviewDistanceAu);
            cameraChanged = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            _orbitYawRadians -= input.MouseDeltaX * OrbitSensitivity;
            _orbitPitchRadians = Math.Clamp(_orbitPitchRadians - input.MouseDeltaY * OrbitSensitivity, -1.45d, 1.45d);
            cameraChanged = true;
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
        _orbitDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * InitialOverviewDistanceAu;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = 0d;
        ApplyOrbitPose(camera);
    }

    internal NativePlanetaryPresentation FocusedPresentation(CameraState camera)
    {
        var body = FocusedBody;
        var center = CubeSphereProjection.CameraRelativeCenter(body, new UniversePosition(camera.Position.Value, Presentation.RootFrame));
        return new NativePlanetaryPresentation
        {
            CenterX = (float)center.X, CenterY = (float)center.Y, CenterZ = (float)center.Z, Radius = (float)body.RadiusMetres,
            ColorR = body.Color.X, ColorG = body.Color.Y, ColorB = body.Color.Z,
            DistantAlpha = _blend.DistantAlpha, DetailedAlpha = _blend.DetailedAlpha, DistanceRadii = (float)_blend.DistanceRadii,
            Regime = (NativePlanetaryRenderRegime)_blend.Regime, Enabled = 1
        };
    }

    internal NativePlanetaryGpuConstants GpuConstants(CameraState camera)
    {
        var body = FocusedBody;
        var relative = camera.Position.Value - body.Position.Value;
        return new NativePlanetaryGpuConstants
        {
            CameraBodyX = (float)relative.X, CameraBodyY = (float)relative.Y, CameraBodyZ = (float)relative.Z,
            Radius = (float)body.RadiusMetres,
            RefinementThreshold = (float)EarthPlanetaryScene.LodConfiguration.MaximumProjectedPatchSpan,
            NearFieldAltitudeRadii = (float)EarthPlanetaryScene.LodConfiguration.NearFieldAltitudeRadii,
            DetailedAlpha = _blend.DetailedAlpha, MaximumLevel = EarthPlanetaryScene.MaximumLod,
            OutputCapacity = EarthPlanetaryScene.MaximumPatchCapacity
        };
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

    private void SelectVisibleLabels(CameraState camera)
    {
        Array.Clear(_visibleLabels);
        Array.Clear(_labelBoundsValid);
        VisibleLabelCount = 0;
        Span<int> priority = stackalloc int[BodyOrder.Length];
        var priorityCount = 0;
        AddPriority(priority, ref priorityCount, FocusIndex);
        AddPriority(priority, ref priorityCount, 0); // Sun
        AddPriority(priority, ref priorityCount, 3); // Earth
        AddPriority(priority, ref priorityCount, 4); // Moon
        for (var index = 0; index < BodyOrder.Length; index++) AddPriority(priority, ref priorityCount, index); // Stable body-ID order.

        for (var priorityIndex = 0; priorityIndex < priorityCount; priorityIndex++)
        {
            var index = priority[priorityIndex];
            var focused = index == FocusIndex;
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
            _bodyStaging[index] = new EvaluatedPlanetaryBody(
                id.Value, new UniversePosition(_roots[_traversalIndices[index]].Translation, _root),
                catalog.PhysicalProperties.MeanRadius, Colors[index], catalog.Identity.DisplayName, true);
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
        var forward = orientation.Rotate(new Double3(0d, 0d, -1d));
        camera.Orientation = orientation;
        camera.Position = camera.Position with { Value = FocusedBody.Position.Value - forward * _orbitDistance };
        camera.Validate();
        Update(camera);
    }

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
