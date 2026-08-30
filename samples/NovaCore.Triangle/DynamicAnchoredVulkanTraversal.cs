using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;

internal sealed class DynamicAnchoredVulkanTraversal
{
    private readonly record struct Pose(string Name, Double3 Direction, double AltitudeMetres, bool Horizon);
    private const int TransitionFrames = 24;
    private const int SlowRotationFrames = 240;
    private const int RapidRotationFrames = 72;
    private const int StationaryRotationFrames = SlowRotationFrames + RapidRotationFrames;

    private static readonly Double3 LocalCenter = RelaxedCubeSphereProjection.UnitDirection(
        CubeSphereFace.PositiveZ, 2047.5d / 4096d, 864.5d / 4096d);
    private static readonly Pose[] Poses =
    [
        new("same-face-a", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .42d, .36d), 300_000d, false),
        new("same-face-b", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .46d, .39d), 100_000d, false),
        new("patch-boundary-left-1", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .4998d, .417d), 30_000d, false),
        new("patch-boundary-right-1", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .5002d, .417d), 30_000d, false),
        new("patch-boundary-left-2", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .4998d, .417d), 30_000d, false),
        new("patch-boundary-right-2", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .5002d, .417d), 30_000d, false),
        new("face-z-edge", RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(.995d, .13d, 1d)), 10_000d, false),
        new("face-x-edge", RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, .13d, .995d)), 10_000d, false),
        new("corner-z", RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(.995d, .992d, 1d)), 10_000d, false),
        new("corner-x", RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, .992d, .995d)), 10_000d, false),
        new("corner-y", RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(.995d, 1d, .992d)), 10_000d, false),
        new("local-horizon", LocalCenter, 10.001d, true),
        new("sub-metre-east", (LocalCenter + PlanetarySurfaceFrame.AtDirection(LocalCenter).East * (.25d / 6_378_137d)).Normalized(), 10.001d, true),
        new("kilometres-east", (LocalCenter + PlanetarySurfaceFrame.AtDirection(LocalCenter).East * (5_000d / 6_378_137d)).Normalized(), 1_000d, true),
        new("snap-before", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, 2048.11d / 4096d, 864.5d / 4096d), 1_000d, true),
        new("snap-after", RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, 2048.14d / 4096d, 864.5d / 4096d), 1_000d, true),
        new("retreat", LocalCenter, 500_000d, false),
        new("reapproach", LocalCenter, 1_000d, true),
    ];

    private readonly Dictionary<PlanetaryAnchoredPatchKey, Double3> _stablePatchPositions = new();
    private readonly Dictionary<PlanetaryAnchoredMeshVertexId, Double3> _edgePositions = new();
    private readonly long _started = Stopwatch.GetTimestamp();
    private long _lastFrame = Stopwatch.GetTimestamp();
    private double _frameMilliseconds, _frameMillisecondsSquared, _maximumFrameMilliseconds;
    private double _maximumPreparationMilliseconds, _maximumGeographicDriftMetres, _maximumEdgeGapMetres;
    private double _maximumBackgroundPreparationMilliseconds, _maximumMainThreadTerrainMilliseconds;
    private double _maximumPublicationLatencyMilliseconds, _maximumWorkerUtilizationPercent;
    private double _maximumSnapCorrespondenceMetres, _maximumTangentFrameDeltaRadians;
    private double _maximumCameraToBillboardOriginMetres;
    private long _observations, _promotions, _retirements, _preparationFrames, _uploadBytesStart = -1;
    private long _cacheHitsStart = -1, _cacheMissesStart = -1;
    private int _poseIndex, _poseFrames, _settledFrames, _peakDemanded, _peakAuthoritative, _peakResident;
    private int _peakPreparationWorkers, _peakPreparationBatchSize, _peakPreparationQueueDepth;
    private int _stationaryFrames;
    private readonly double[] _stationaryFrameMilliseconds = new double[StationaryRotationFrames];
    private long _stationaryHitsStart, _stationaryMissesStart, _stationaryEvictionsStart, _stationaryUploadsStart;
    private long _stationaryRotationReuseStart;
    private uint _stationaryGeneration;
    private int _stationaryDemand;
    private double _stationarySelectionMillisecondsMaximum;
    private uint _lastPublishedGeneration;
    private PlanetarySphericalBillboardFrame _lastPresentationFrame;
    private bool _failed, _complete, _hasFrameTimestamp, _stationaryPrimed;
    private string _failure = string.Empty;

    public bool Failed => _failed;
    public string FinalReport { get; private set; } = string.Empty;

    public void ApplyPose(CameraState camera, in PlanetRenderProxy body)
    {
        if (_complete) return;
        var pose = Poses[Math.Min(_poseIndex, Poses.Length - 1)];
        var direction = pose.Direction.Normalized();
        var altitude = pose.AltitudeMetres;
        if (_poseIndex > 0 && _poseFrames < TransitionFrames)
        {
            var previous = Poses[_poseIndex - 1];
            var amount = Math.Clamp(_poseFrames / (double)TransitionFrames, 0d, 1d);
            amount = amount * amount * (3d - 2d * amount);
            direction = (previous.Direction * (1d - amount) + pose.Direction * amount).Normalized();
            altitude = Math.Exp(Math.Log(previous.AltitudeMetres) * (1d - amount) +
                                Math.Log(pose.AltitudeMetres) * amount);
        }
        var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
        var cameraBody = direction * (body.RadiusMetres + physical.FinalHeightMetres + altitude);
        camera.Position = camera.Position with { Value = body.Position.Value + body.BodyFixedToRoot.Rotate(cameraBody) };
        var rotationYaw = _poseIndex < Poses.Length ? .18d : StationaryRotationYaw(_stationaryFrames);
        var bodyOrientation = pose.Horizon
            ? PlanetarySurfaceFrame.AtDirection(direction).LookOrientation(rotationYaw, -.035d)
            : LookTowardCenter(direction);
        camera.Orientation = (body.BodyFixedToRoot * bodyOrientation).Normalized();
    }

    public bool Observe(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        if (_complete) return true;
        var now = Stopwatch.GetTimestamp(); var frameMilliseconds = _hasFrameTimestamp
            ? Stopwatch.GetElapsedTime(_lastFrame, now).TotalMilliseconds : 0d;
        _hasFrameTimestamp = true;
        _lastFrame = now; _observations++; _frameMilliseconds += frameMilliseconds;
        _frameMillisecondsSquared += frameMilliseconds * frameMilliseconds;
        _maximumFrameMilliseconds = Math.Max(_maximumFrameMilliseconds, frameMilliseconds);
        _poseFrames++;
        var telemetry = hierarchy.Telemetry;
        MeasurePresentationSnap(hierarchy);
        _maximumCameraToBillboardOriginMetres=Math.Max(_maximumCameraToBillboardOriginMetres,
            hierarchy.CameraToPresentationOriginMetres);
        if (_uploadBytesStart < 0) { _uploadBytesStart = telemetry.UploadBytes; _cacheHitsStart = telemetry.CacheHits; _cacheMissesStart = telemetry.CacheMisses; }
        _peakDemanded = Math.Max(_peakDemanded, telemetry.DemandedPatchCount);
        _peakAuthoritative = Math.Max(_peakAuthoritative, telemetry.AuthoritativePatchCount);
        _peakResident = Math.Max(_peakResident, telemetry.ResidentPatchCount);
        _maximumPreparationMilliseconds = Math.Max(_maximumPreparationMilliseconds, telemetry.LastPreparationMilliseconds);
        _maximumBackgroundPreparationMilliseconds = Math.Max(_maximumBackgroundPreparationMilliseconds,
            telemetry.LastBackgroundPreparationMilliseconds);
        _maximumMainThreadTerrainMilliseconds = Math.Max(_maximumMainThreadTerrainMilliseconds,
            telemetry.MainThreadTerrainMilliseconds);
        _maximumPublicationLatencyMilliseconds = Math.Max(_maximumPublicationLatencyMilliseconds,
            telemetry.LastPublicationLatencyMilliseconds);
        _maximumWorkerUtilizationPercent = Math.Max(_maximumWorkerUtilizationPercent,
            telemetry.LastWorkerUtilizationPercent);
        _peakPreparationWorkers = Math.Max(_peakPreparationWorkers, telemetry.LastPreparationWorkerCount);
        _peakPreparationBatchSize = Math.Max(_peakPreparationBatchSize, telemetry.LastPreparationBatchSize);
        _peakPreparationQueueDepth = Math.Max(_peakPreparationQueueDepth, telemetry.PreparationQueueDepth);
        if (telemetry.LastPreparationMilliseconds > 0d) _preparationFrames++;

        if (telemetry.DemandedPatchCount > PlanetaryDynamicAnchoredSurface.DefaultActiveCapacity ||
            telemetry.ResidentPatchCount > hierarchy.CacheCapacity)
            return Fail($"unbounded residency at {Poses[_poseIndex].Name}: demand={telemetry.DemandedPatchCount}; resident={telemetry.ResidentPatchCount}");
        if (!hierarchy.Visible)
        {
            _settledFrames = 0;
            if (!telemetry.GlobalFallbackActive || _poseFrames > 240)
                return Fail($"transaction did not preserve complete fallback at {Poses[Math.Min(_poseIndex, Poses.Length - 1)].Name}");
            return false;
        }
        if (hierarchy.HasPendingGeneration)
        {
            if (telemetry.AuthoritativePatchCount == 0 || !telemetry.GlobalFallbackActive ||
                HasAncestorOverlap(hierarchy))
                return Fail($"incoming transaction displaced valid authority at {Poses[Math.Min(_poseIndex, Poses.Length - 1)].Name}");
            MeasureStableGeography(hierarchy);
            MeasureSharedEdges(hierarchy);
            _settledFrames = 0;
            return false;
        }
        var stationaryPhase = _poseIndex >= Poses.Length;
        if (!telemetry.CompleteCoverage || telemetry.AuthoritativePatchCount != telemetry.DemandedPatchCount ||
            hierarchy.AuthoritativePatchCount != telemetry.DemandedPatchCount)
            return Fail($"incomplete or overlapping authoritative transaction at {Poses[Math.Min(_poseIndex, Poses.Length - 1)].Name}");
        if ((!stationaryPhase || !_stationaryPrimed) &&
            (HasAncestorOverlap(hierarchy) ||
             hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount).Any(patch =>
                 (patch.Flags & PlanetaryDynamicAnchoredSurface.RequiredSubmissionFlags) !=
                 PlanetaryDynamicAnchoredSurface.RequiredSubmissionFlags)))
            return Fail($"incomplete or overlapping authoritative transaction at {Poses[Math.Min(_poseIndex, Poses.Length - 1)].Name}");

        if (_lastPublishedGeneration != hierarchy.ActiveGeneration)
        {
            if (_lastPublishedGeneration != 0u) _retirements++;
            _lastPublishedGeneration = hierarchy.ActiveGeneration; _promotions++;
        }
        if (stationaryPhase)
        {
            if (!_stationaryPrimed)
            {
                MeasureStableGeography(hierarchy);
                MeasureSharedEdges(hierarchy);
                _stationaryHitsStart = telemetry.CacheHits; _stationaryMissesStart = telemetry.CacheMisses;
                _stationaryEvictionsStart = telemetry.Evictions; _stationaryUploadsStart = telemetry.UploadBytes;
                _stationaryRotationReuseStart = hierarchy.RotationReuseCount;
                _stationaryGeneration = hierarchy.ActiveGeneration;
                _stationaryDemand = telemetry.DemandedPatchCount;
                _stationaryPrimed = true;
                _lastFrame = Stopwatch.GetTimestamp();
                return false;
            }
            _stationarySelectionMillisecondsMaximum = Math.Max(_stationarySelectionMillisecondsMaximum,
                telemetry.LastSelectionMilliseconds);
            if (telemetry.FrameCacheMisses != 0 || telemetry.FrameEvictions != 0 ||
                telemetry.FramePreparations != 0 || telemetry.FrameUploadBytes != 0 ||
                telemetry.LastSelectionMilliseconds != 0d ||
                hierarchy.ActiveGeneration != _stationaryGeneration ||
                telemetry.DemandedPatchCount != _stationaryDemand ||
                !telemetry.GlobalFallbackActive)
                return Fail($"stationary footprint remained active: misses={telemetry.FrameCacheMisses}; " +
                    $"evictions={telemetry.FrameEvictions}; preparations={telemetry.FramePreparations}; " +
                    $"uploadBytes={telemetry.FrameUploadBytes}; selectionMs={telemetry.LastSelectionMilliseconds:R}; " +
                    $"generation={hierarchy.ActiveGeneration}/{_stationaryGeneration}; " +
                    $"demand={telemetry.DemandedPatchCount}/{_stationaryDemand}; fallback={telemetry.GlobalFallbackActive}");
            _stationaryFrameMilliseconds[_stationaryFrames++] = frameMilliseconds;
            if (_stationaryFrames < _stationaryFrameMilliseconds.Length) return false;
            Complete(hierarchy);
            return true;
        }
        MeasureStableGeography(hierarchy);
        MeasureSharedEdges(hierarchy);
        if (_poseIndex > 0 && _poseFrames < TransitionFrames) return false;
        _settledFrames++;
        if (_settledFrames < 3) return false;

        Console.WriteLine($"Dynamic Vulkan traversal pose: index={_poseIndex}; name={Poses[_poseIndex].Name}; " +
            $"demanded={telemetry.DemandedPatchCount}; authoritative={telemetry.AuthoritativePatchCount}; " +
            $"resident={telemetry.ResidentPatchCount}/{hierarchy.CacheCapacity}; generation={hierarchy.ActiveGeneration}; " +
            $"hits={telemetry.CacheHits}; misses={telemetry.CacheMisses}; evictions={telemetry.Evictions}; " +
            $"prepareMs={telemetry.LastPreparationMilliseconds:R}; " +
            $"cameraToBillboardOrigin={hierarchy.CameraToPresentationOriginMetres:R}m; coverage=complete");
        _poseIndex++; _poseFrames = 0; _settledFrames = 0;
        if (_poseIndex < Poses.Length) return false;
        return false;
    }

    private void Complete(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        var orderedStationary = _stationaryFrameMilliseconds.Order().ToArray();
        var stationaryAverage = _stationaryFrameMilliseconds.Average();
        var stationaryP95 = orderedStationary[(int)Math.Ceiling(.95d * orderedStationary.Length) - 1];
        var stationaryMaximum = orderedStationary[^1];

        var elapsed = Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
        var average = _frameMilliseconds / Math.Max(1, _observations);
        var deviation = Math.Sqrt(Math.Max(0d, _frameMillisecondsSquared / Math.Max(1, _observations) - average * average));
        var final = hierarchy.Telemetry;
        FinalReport = $"Dynamic anchored Vulkan traversal PASS: poses={Poses.Length}; frames={_observations}; elapsedMs={elapsed:R}; " +
            $"demandedPeak={_peakDemanded}; authoritativePeak={_peakAuthoritative}; residentPeak={_peakResident}/{hierarchy.CacheCapacity}; " +
            $"occupancy={100d * _peakResident / hierarchy.CacheCapacity:F2}%; promotions={_promotions}; retirements={_retirements}; " +
            $"preparationFrames={_preparationFrames}; uploadBytes={final.UploadBytes - _uploadBytesStart}; " +
            $"selectionReuseHits={final.CacheHits - _cacheHitsStart}; selectionMisses={final.CacheMisses - _cacheMissesStart}; " +
            $"cpuFrameMsAvg={average:R}; cpuFrameMsStdDev={deviation:R}; cpuFrameMsMax={_maximumFrameMilliseconds:R}; " +
            $"mainThreadTerrainMsMax={_maximumMainThreadTerrainMilliseconds:R}; " +
            $"preparePollMsMax={_maximumPreparationMilliseconds:R}; backgroundPrepareMsMax={_maximumBackgroundPreparationMilliseconds:R}; " +
            $"publicationLatencyMsMax={_maximumPublicationLatencyMilliseconds:R}; " +
            $"workers={_peakPreparationWorkers}/{final.ConfiguredWorkerCount}; workerUtilizationMax={_maximumWorkerUtilizationPercent:R}%; " +
            $"preparationBatchPeak={_peakPreparationBatchSize}; preparationQueueDepthPeak={_peakPreparationQueueDepth}; " +
            $"geographicDriftMax={_maximumGeographicDriftMetres:E6}m; " +
            $"sharedEdgeGapMax={_maximumEdgeGapMetres:E6}m; billboardSnaps={Math.Max(0, hierarchy.PresentationFrame.PresentationGeneration-1)}; " +
            $"snapCorrespondenceMax={_maximumSnapCorrespondenceMetres:E6}m; tangentFrameDeltaMax={_maximumTangentFrameDeltaRadians:E6}rad; " +
            $"cameraToBillboardOriginMax={_maximumCameraToBillboardOriginMetres:R}m; settledFrames={_stationaryFrames}; " +
            $"settledCpuFrameMsAvg={stationaryAverage:R}; settledCpuFrameMsP95={stationaryP95:R}; " +
            $"settledCpuFrameMsMax={stationaryMaximum:R}; settledHits={final.CacheHits-_stationaryHitsStart}; " +
            $"settledMisses={final.CacheMisses-_stationaryMissesStart}; settledEvictions={final.Evictions-_stationaryEvictionsStart}; " +
            $"settledUploadBytes={final.UploadBytes-_stationaryUploadsStart}; " +
            $"stationaryRotation=slow360+rapid360; rotationFrames={StationaryRotationFrames}; " +
            $"rotationReuse={hierarchy.RotationReuseCount-_stationaryRotationReuseStart}; " +
            $"rotationSelectionMsMax={_stationarySelectionMillisecondsMaximum:R}; " +
            $"rotationGeneration={_stationaryGeneration}; rotationDemand={_stationaryDemand}; " +
            $"coverage=complete; stalePatches=0; zeroOwner=0; overlap=0";
        _complete = true;
    }

    private static double StationaryRotationYaw(int frame)
    {
        if (frame < SlowRotationFrames)
            return .18d + Math.Tau * frame / SlowRotationFrames;
        return .18d + Math.Tau + Math.Tau * (frame - SlowRotationFrames) / RapidRotationFrames;
    }

    private bool Fail(string message)
    {
        _failed = true; _complete = true; _failure = message;
        FinalReport = $"Dynamic anchored Vulkan traversal FAIL: {_failure}";
        return true;
    }

    private void MeasureStableGeography(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        for (var index = 0; index < hierarchy.AuthoritativePatchCount; index++)
        {
            var patch = hierarchy.AuthoritativePatches[index];
            var key = new PlanetaryAnchoredPatchKey(((ulong)patch.BodyIdHigh << 32) | patch.BodyIdLow,
                patch.TerrainVersion, patch.PhysicalSurfaceGeneration, (CubeSphereFace)patch.Face,
                (int)patch.Level, (int)patch.X, (int)patch.Y);
            var cells = 1u << (int)patch.Level;
            var direction = RelaxedCubeSphereProjection.UnitDirection((CubeSphereFace)patch.Face,
                (patch.X + .5d) / cells, (patch.Y + .5d) / cells);
            var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
            var position = direction * (6_378_137d + physical.FinalHeightMetres);
            if (_stablePatchPositions.TryGetValue(key, out var previous))
                _maximumGeographicDriftMetres = Math.Max(_maximumGeographicDriftMetres,
                    Math.Sqrt((position - previous).LengthSquared));
            else _stablePatchPositions.Add(key, position);
        }
    }

    private void MeasureSharedEdges(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        _edgePositions.Clear();
        for (var index = 0; index < hierarchy.AuthoritativePatchCount; index++)
        {
            var patch = hierarchy.AuthoritativePatches[index];
            var id = new PlanetarySurfacePatchId(((ulong)patch.BodyIdHigh << 32) | patch.BodyIdLow,
                patch.TerrainVersion, (CubeSphereFace)patch.Face, (int)patch.Level, (int)patch.X, (int)patch.Y);
            for (var sample = 0; sample <= PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution; sample++)
            {
                Compare(id, 0, sample);
                Compare(id, PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution, sample);
                Compare(id, sample, 0);
                Compare(id, sample, PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution);
            }
        }

        void Compare(in PlanetarySurfacePatchId patch, int gridX, int gridY)
        {
            var identity = PlanetaryAnchoredMeshVertexId.FromPatchGrid(patch, gridX, gridY,
                PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution);
            var direction = identity.BodyFixedDirection;
            var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
            var position = direction * (6_378_137d + physical.FinalHeightMetres);
            if (_edgePositions.TryGetValue(identity, out var other))
                _maximumEdgeGapMetres = Math.Max(_maximumEdgeGapMetres, Math.Sqrt((position - other).LengthSquared));
            else _edgePositions.Add(identity, position);
        }
    }

    private void MeasurePresentationSnap(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        var current = hierarchy.PresentationFrame;
        if (!current.IsValid) return;
        if (_lastPresentationFrame.IsValid && current.PresentationGeneration != _lastPresentationFrame.PresentationGeneration)
        {
            var direction = current.CanonicalReferenceDirection;
            var physical = PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(direction);
            var point = direction * (6_378_137d + physical.FinalHeightMetres);
            var camera = direction * (6_378_137d + physical.FinalHeightMetres + 1_000d);
            _maximumSnapCorrespondenceMetres = Math.Max(_maximumSnapCorrespondenceMetres,
                Math.Sqrt((current.CameraRelative(point, camera) -
                           _lastPresentationFrame.CameraRelative(point, camera)).LengthSquared));
            var tangentDelta = Math.Max(
                Math.Acos(Math.Clamp(Double3.Dot(current.TangentBasis.East, _lastPresentationFrame.TangentBasis.East), -1d, 1d)),
                Math.Acos(Math.Clamp(Double3.Dot(current.TangentBasis.North, _lastPresentationFrame.TangentBasis.North), -1d, 1d)));
            _maximumTangentFrameDeltaRadians = Math.Max(_maximumTangentFrameDeltaRadians, tangentDelta);
        }
        _lastPresentationFrame = current;
    }

    private static bool HasAncestorOverlap(PlanetaryDynamicAnchoredSurface hierarchy)
    {
        var patches = hierarchy.AuthoritativePatches.Take(hierarchy.AuthoritativePatchCount)
            .Select(value => new PlanetaryPatch((CubeSphereFace)value.Face, (int)value.Level,
                (int)value.X, (int)value.Y)).ToHashSet();
        foreach (var patch in patches)
        {
            var parent = patch.Parent;
            while (parent is { } value)
            {
                if (patches.Contains(value)) return true;
                parent = value.Parent;
            }
        }
        return false;
    }

    private static DoubleQuaternion LookTowardCenter(in Double3 radial)
    {
        var forward = -radial.Normalized();
        var reference = Math.Abs(Double3.Dot(forward, Double3.UnitY)) > .99d ? Double3.UnitZ : Double3.UnitY;
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
