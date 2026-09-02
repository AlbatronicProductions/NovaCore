using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public enum PlanetaryDynamicSurfaceFailure : byte
{
    None,
    GpuAllocation
}

public enum PlanetaryDynamicGenerationRejectReason : byte
{
    None,
    Capacity,
    InjectedFailure
}

public enum PlanetaryAnchoredPatchCacheState : byte
{
    Absent,
    Requested,
    Preparing,
    Resident,
    Ready,
    Authoritative,
    Cached,
    Retiring,
    Failed
}

public readonly record struct PlanetaryAnchoredPatchKey(
    ulong BodyId,
    uint TerrainVersion,
    uint PhysicalSurfaceGeneration,
    CubeSphereFace Face,
    int Level,
    int X,
    int Y) : IComparable<PlanetaryAnchoredPatchKey>
{
    public PlanetarySurfacePatchId Patch => new(BodyId, TerrainVersion, Face, Level, X, Y);
    public bool IsValid => PhysicalSurfaceGeneration != 0 && Patch.IsValid;
    public int CompareTo(PlanetaryAnchoredPatchKey other)
    {
        var patch = Patch.CompareTo(other.Patch); return patch != 0 ? patch :
            PhysicalSurfaceGeneration.CompareTo(other.PhysicalSurfaceGeneration);
    }
}

public readonly record struct PlanetaryDynamicAnchoredTelemetry(
    int DemandedPatchCount,
    int AuthoritativePatchCount,
    int ResidentPatchCount,
    int MinimumLevel,
    int MaximumLevel,
    int CacheCapacity,
    long CacheHits,
    long CacheMisses,
    long Evictions,
    long UploadBytes,
    uint ActiveGeneration,
    bool CompleteCoverage,
    bool GlobalFallbackActive,
    double MaximumProjectedErrorPixels,
    double LastPreparationMilliseconds,
    double LastBackgroundPreparationMilliseconds,
    double LastSelectionMilliseconds,
    int RetainedDemandedPatchCount,
    int NewDemandedPatchCount,
    int ReleasedDemandedPatchCount,
    double DemandOverlapPercent,
    long FrameCacheHits,
    long FrameCacheMisses,
    long FrameEvictions,
    int FramePreparations,
    long FrameUploadBytes,
    int PendingPatchCount,
    int PreparedPendingPatchCount,
    bool NadirCovered,
    int ConfiguredWorkerCount,
    int LastPreparationWorkerCount,
    double LastWorkerUtilizationPercent,
    int LastPreparationBatchSize,
    int PreparationQueueDepth,
    double LastPublicationLatencyMilliseconds,
    double MainThreadTerrainMilliseconds,
    PlanetaryDynamicGenerationRejectReason RejectedGenerationReason,
    long PersistentTopologyReuseCount,
    long PersistentTopologyTranslationCount,
    int DescriptorDeltaCount,
    double LastSelectionQueueLatencyMilliseconds,
    bool SelectionPending,
    uint PresentationGeneration);

public readonly record struct PlanetaryDynamicAnchoredConfiguration(
    double TargetPatchPixels,
    int MaximumLevel,
    double AcquireAltitudeMetres,
    double ReleaseAltitudeMetres)
{
    public static PlanetaryDynamicAnchoredConfiguration ProductionDefault =>
        new(256d, PlanetaryDynamicAnchoredSurface.MaximumLevel, 700_000d, 800_000d);

    public bool IsValid => double.IsFinite(TargetPatchPixels) && TargetPatchPixels > 0d &&
        MaximumLevel is >= 0 and <= PlanetaryDynamicAnchoredSurface.MaximumLevel &&
        !double.IsNaN(AcquireAltitudeMetres) && AcquireAltitudeMetres >= 0d &&
        !double.IsNaN(ReleaseAltitudeMetres) && ReleaseAltitudeMetres >= AcquireAltitudeMetres;
}

/// <summary>
/// Dynamic body-fixed production refinement. SurfaceAnchor and camera state
/// influence demand only; every cache key remains canonical patch geography.
/// A complete requested set is published as one generation, otherwise the
/// terrain-v5 global surface remains the sole owner.
/// </summary>
public sealed class PlanetaryDynamicAnchoredSurface
{
    public const int GpuBaseGridResolution = 4;
    public const int GpuBaseVerticesPerPatch = 25;
    public const int GpuBaseIndicesPerPatch = 96;
    public const int GpuTargetEdgePixels = 16;
    public const int GpuMaximumTessellationFactor = 16;
    public const double GpuTessellationRangeMetres = 50d;
    public const int GpuPatchDescriptorBytes = 80;
    // The active table holds one complete orientation-independent near-field
    // neighborhood at the native 3440x1440 acceptance density.  Geometry is
    // reusable; this bounded increase is descriptor/cache identity capacity,
    // not another per-patch final-raster mesh allocation.
    public const int DefaultCacheCapacity = 12288;
    public const int DefaultActiveCapacity = 6144;
    public const int TransactionHeadroom = DefaultCacheCapacity - DefaultActiveCapacity;
    public const int MaximumBackgroundPreparationWorkers = 8;
    public const int MaximumLevel = 20;
    public const double TargetPatchPixels = 256d;
    public const double VisibilityAltitudeMetres = 700_000d;
    public const double VisibilityReleaseAltitudeMetres = 800_000d;
    public const double PresentationMorphDurationSeconds = 0.5d;
    public const double MinimumRetainedNeighborhoodRadiusMetres = 32_000d;
    public const double MaximumRetainedNeighborhoodRadiusMetres = 64_000d;
    // The retained neighborhood is persistent presentation topology.  Ordinary
    // reference-frame motion may translate its canonical addresses only after
    // half of the retained radius has been consumed; the old 1/64 threshold
    // converted Earth's normal rotation into a full CPU selection every second.
    public const double MinimumResidencyRecenterMetres = 4_000d;
    public const double MaximumResidencyRecenterMetres = 32_000d;
    public const double ResidencyRecenterRadiusFraction = .5d;
    public const uint SubmissionReady = 1u << 0;
    public const uint SubmissionAuthoritative = 1u << 1;
    public const uint SubmissionGeometryComplete = 1u << 2;
    public const uint SubmissionPhysicalSurfaceComplete = 1u << 3;
    public const uint SubmissionMaterialComplete = 1u << 4;
    public const uint SubmissionSynchronizationComplete = 1u << 5;
    public const uint SubmissionLocalPayloadRequired = 1u << 6;
    public const uint SubmissionMorphFromParent = 1u << 7;
    public const uint SubmissionMorphToParent = 1u << 8;
    public const uint CpuCompleteSubmissionFlags = SubmissionReady |
        SubmissionGeometryComplete | SubmissionPhysicalSurfaceComplete | SubmissionMaterialComplete;
    public const uint RequiredSubmissionFlags = SubmissionReady | SubmissionAuthoritative |
        SubmissionGeometryComplete | SubmissionPhysicalSurfaceComplete |
        SubmissionMaterialComplete | SubmissionSynchronizationComplete;

    private sealed class Entry
    {
        internal PlanetaryAnchoredPatchKey Key;
        private int _state;
        internal PlanetaryAnchoredPatchCacheState State
        {
            get => (PlanetaryAnchoredPatchCacheState)Volatile.Read(ref _state);
            set => Volatile.Write(ref _state, (int)value);
        }
        internal uint Generation;
        internal long LastUse;
    }

    private readonly record struct PreparedTopologyTranslation(
        long RequestId,
        PlanetaryLodSelection Selection,
        Double3 CameraBody,
        Double3 ForwardBody,
        double FieldOfView,
        double Aspect,
        double ViewportHeight,
        double NeighborhoodRadius,
        double SelectionMilliseconds,
        PlanetaryPatchConservativeBounds[] PreparedBounds,
        double MaximumProjectedErrorPixels,
        bool ReusedPersistentTopology,
        long StartedTimestamp);

    private readonly ulong _bodyId;
    private readonly double _bodyRadius;
    private readonly PlanetaryTerrainDefinition _terrain;
    private readonly uint _physicalSurfaceGeneration;
    private readonly PlanetaryDynamicAnchoredConfiguration _configuration;
    private readonly Entry[] _entries;
    private readonly NativeAnchoredSurfacePatch[] _submission;
    private readonly NativeAnchoredSurfacePatch[] _authoritativeSubmission;
    private readonly HashSet<PlanetaryAnchoredPatchKey> _requested;
    private readonly HashSet<PlanetaryPatch> _previousLeafSet;
    private readonly HashSet<PlanetaryPatch> _morphToParentSet;
    private readonly Dictionary<PlanetaryAnchoredPatchKey, int> _slotByKey;
    private readonly Stack<int> _freeSlots;
    private readonly PriorityQueue<int, long> _cachedLru;
    private readonly List<(int Slot, long Priority)> _deferredLru;
    private readonly int[] _slots;
    private readonly int[] _preparationIndices;
    private readonly int[] _preparationWorkerThreadIds;
    private PlanetaryPatch[] _previousLeaves = [];
    private PlanetaryLodSelection _lastSelection;
    private PlanetaryLodSelection _pendingSelection;
    private Double3 _lastSelectionCamera;
    private Double3 _lastSelectionForward;
    private double _lastSelectionNeighborhoodRadius;
    private double _lastSelectionFieldOfView;
    private double _lastSelectionAspect;
    private double _lastSelectionViewportHeight;
    private bool _hasLastSelection;
    private bool _pending;
    private bool _coarseningActive;
    private long _coarseningStartedTimestamp;
    private PlanetaryLodSelection _coarseningTargetSelection;
    private int _authoritativePatchCount;
    private uint _authoritativeGeneration;
    private int _preparedPendingPatchCount;
    private int _pendingPreparationCount;
    private double _pendingMaximumProjectedError;
    private int _retainedDemandedPatchCount, _newDemandedPatchCount, _releasedDemandedPatchCount;
    private double _demandOverlapPercent, _lastSelectionMilliseconds;
    private double _lastBackgroundPreparationMilliseconds;
    private long _frameHitsStart, _frameMissesStart, _frameEvictionsStart, _frameUploadBytesStart;
    private int _framePreparations;
    private PlanetaryDynamicGenerationRejectReason _rejectedGenerationReason;
    private long _serial, _hits, _misses, _evictions, _uploadBytes;
    private uint _activeGeneration;
    private uint _gpuReadyGeneration;
    private int _activePatchCount;
    private bool _visible;
    private bool _authoritativeVisible;
    private PlanetaryDynamicAnchoredTelemetry _telemetry;
    private PlanetaryDynamicSurfaceFailure _injectedFailure;
    private Thread? _preparationWorker;
    private Thread? _selectionWorker;
    private readonly object _selectionGate = new();
    private PreparedTopologyTranslation? _preparedTopologyTranslation;
    private long _selectionRequestSerial;
    private uint _backgroundCompleteGeneration;
    private int _lastReportedPreparedPendingPatchCount;
    private int _lastPreparationWorkerCount;
    private int _lastPreparationBatchSize;
    private long _pendingStartedTimestamp;
    private double _lastPublicationLatencyMilliseconds;
    private PlanetarySphericalBillboardFrame _presentationFrame;
    private double _cameraToPresentationOriginMetres;
    private double _retainedNeighborhoodRadiusMetres;
    private double _cameraToResidencyCenterMetres;
    private long _rotationReuseCount;
    private long _persistentTopologyReuseCount;
    private long _persistentTopologyTranslationCount;
    private int _lastDescriptorDeltaCount;
    private bool _pendingReusesPersistentTopology;
    private double _lastSelectionQueueLatencyMilliseconds;
    private bool _lastSelectionAsynchronous;
    private uint _transportGeneration;
    private int _transportPatchCount;

    public PlanetaryDynamicAnchoredSurface(ulong bodyId, double bodyRadius,
        in PlanetaryTerrainDefinition terrain, uint physicalSurfaceGeneration,
        int cacheCapacity = DefaultCacheCapacity, int activeCapacity = DefaultActiveCapacity,
        PlanetaryDynamicAnchoredConfiguration? configuration = null)
    {
        var resolvedConfiguration = configuration ?? PlanetaryDynamicAnchoredConfiguration.ProductionDefault;
        if (bodyId == 0 || !double.IsFinite(bodyRadius) || bodyRadius <= 0d ||
            !terrain.IsValid || physicalSurfaceGeneration == 0 || cacheCapacity is < 32 or > DefaultCacheCapacity ||
            activeCapacity < 16 || activeCapacity > cacheCapacity || !resolvedConfiguration.IsValid)
            throw new ArgumentOutOfRangeException();
        _bodyId = bodyId; _bodyRadius = bodyRadius; _terrain = terrain;
        _physicalSurfaceGeneration = physicalSurfaceGeneration; _configuration = resolvedConfiguration;
        _entries = Enumerable.Range(0, cacheCapacity).Select(_ => new Entry()).ToArray();
        _submission = new NativeAnchoredSurfacePatch[activeCapacity];
        _authoritativeSubmission = new NativeAnchoredSurfacePatch[activeCapacity];
        _requested = new HashSet<PlanetaryAnchoredPatchKey>(activeCapacity);
        _previousLeafSet = new HashSet<PlanetaryPatch>(activeCapacity);
        _morphToParentSet = new HashSet<PlanetaryPatch>(activeCapacity);
        _slotByKey = new Dictionary<PlanetaryAnchoredPatchKey, int>(cacheCapacity);
        _freeSlots = new Stack<int>(cacheCapacity);
        for (var slot = cacheCapacity - 1; slot >= 0; slot--) _freeSlots.Push(slot);
        _cachedLru = new PriorityQueue<int, long>(cacheCapacity);
        _deferredLru = new List<(int Slot, long Priority)>(cacheCapacity);
        _slots = new int[activeCapacity];
        _preparationIndices = new int[activeCapacity];
        _preparationWorkerThreadIds = new int[ConfiguredWorkerCount];
        _telemetry = new(0, 0, 0, 0, 0, cacheCapacity, 0, 0, 0, 0, 0, true, true,
            0d, 0d, 0d, 0d, 0, 0, 0, 100d, 0, 0, 0, 0, 0, 0, 0, false,
            ConfiguredWorkerCount, 0, 0d, 0, 0, 0d, 0d,
            PlanetaryDynamicGenerationRejectReason.None, 0, 0, 0, 0d, false, 0u);
    }

    public NativeAnchoredSurfacePatch[] SubmissionPatches => _submission;
    public NativeAnchoredSurfacePatch[] AuthoritativePatches => _authoritativeSubmission;
    public int ActivePatchCount => _activePatchCount;
    public int AuthoritativePatchCount => _authoritativePatchCount;
    public int CacheCapacity => _entries.Length;
    public uint ActiveGeneration => _activeGeneration;
    public uint AuthoritativeGeneration => _authoritativeGeneration;
    public bool HasPendingGeneration => _pending;
    public bool Visible => _authoritativeVisible && _authoritativePatchCount > 0;
    // A published dynamic generation is complete for its requested visible
    // footprint, not for the entire planetary sphere. The terrain-v5 global
    // hierarchy therefore remains the exact fill owner outside the published
    // patch table even after a transaction is authoritative.
    public bool RequiresGlobalFallback => true;
    public PlanetaryDynamicAnchoredTelemetry Telemetry => _telemetry;
    public PlanetarySphericalBillboardFrame PresentationFrame => _presentationFrame;
    public double CameraToPresentationOriginMetres => _cameraToPresentationOriginMetres;
    public double RetainedNeighborhoodRadiusMetres => _retainedNeighborhoodRadiusMetres;
    public double CameraToResidencyCenterMetres => _cameraToResidencyCenterMetres;
    public double ResidencyRecenterDistanceMetres => Math.Clamp(
        _lastSelectionNeighborhoodRadius * ResidencyRecenterRadiusFraction,
        MinimumResidencyRecenterMetres, MaximumResidencyRecenterMetres);
    public long RotationReuseCount => _rotationReuseCount;
    public long PersistentTopologyReuseCount => _persistentTopologyReuseCount;
    public long PersistentTopologyTranslationCount => _persistentTopologyTranslationCount;
    public static int ConfiguredWorkerCount => Math.Clamp(Environment.ProcessorCount - 2,
        1, MaximumBackgroundPreparationWorkers);
    public NativeAnchoredSurfacePresentation NativePresentation =>
        _presentationFrame.IsValid ? _presentationFrame.Encode() : default;

    public static double PresentationMorphFactor(double elapsedSeconds) =>
        double.IsFinite(elapsedSeconds)
            ? Math.Clamp(elapsedSeconds / PresentationMorphDurationSeconds, 0d, 1d)
            : throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

    public bool CoversDirection(in Double3 bodyDirection)
    {
        if (!RelaxedCubeSphereProjection.TryAddress(bodyDirection, out var face, out var u, out var v)) return false;
        for (var index = 0; index < _authoritativePatchCount; index++)
        {
            var patch = _authoritativeSubmission[index]; if (patch.Face != (uint)face || patch.Level > 24u) continue;
            var cells = 1u << (int)patch.Level;
            var x = Math.Min((uint)Math.Floor(u * cells), cells - 1u);
            var y = Math.Min((uint)Math.Floor(v * cells), cells - 1u);
            if (patch.X == x && patch.Y == y) return true;
        }
        return false;
    }

    public void AcknowledgeGpuGeneration(uint generation)
    {
        if (generation == _activeGeneration) _gpuReadyGeneration = generation;
    }

    public void Deactivate()
    {
        RetireAll();
        PublishInactiveTelemetry();
    }

    public void InjectNextPreparationFailure(PlanetaryDynamicSurfaceFailure failure) =>
        _injectedFailure = failure == PlanetaryDynamicSurfaceFailure.None
            ? throw new ArgumentOutOfRangeException(nameof(failure)) : failure;

    public void Update(in Double3 cameraBody, in Double3 viewForwardBody,
        double verticalFieldOfViewRadians, double aspectRatio, double viewportHeightPixels,
        SurfaceAnchor? activeSurfaceAnchor = null, double nearClipMetres = .05d)
    {
        if (!cameraBody.IsFinite || cameraBody.LengthSquared <= 0d || !viewForwardBody.IsFinite ||
            viewForwardBody.LengthSquared <= 0d || !double.IsFinite(verticalFieldOfViewRadians) ||
            verticalFieldOfViewRadians <= 0d || verticalFieldOfViewRadians >= Math.PI ||
            !double.IsFinite(aspectRatio) || aspectRatio <= 0d ||
            !double.IsFinite(viewportHeightPixels) || viewportHeightPixels <= 0d ||
            !double.IsFinite(nearClipMetres) || nearClipMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        if (activeSurfaceAnchor is { } anchor && (anchor.BodyId != _bodyId || !anchor.IsValid))
            throw new ArgumentOutOfRangeException(nameof(activeSurfaceAnchor));

        BeginFrameTelemetry();
        var radial = cameraBody.Normalized();
        var surface = _terrain.SamplePhysicalSurface(radial);
        var altitude = Math.Sqrt(cameraBody.LengthSquared) - (_bodyRadius + surface.FinalHeightMetres);
        var horizonDistance=Math.Sqrt(Math.Max(0d,altitude*(2d*_bodyRadius+altitude)));
        var retainedRadius=Math.Clamp(16_000d+horizonDistance,
            MinimumRetainedNeighborhoodRadiusMetres,MaximumRetainedNeighborhoodRadiusMetres);
        _retainedNeighborhoodRadiusMetres=retainedRadius;
        _cameraToResidencyCenterMetres=_hasLastSelection
            ?Math.Sqrt((cameraBody-_lastSelectionCamera).LengthSquared):0d;
        if (altitude > _configuration.ReleaseAltitudeMetres ||
            (!_visible && altitude > _configuration.AcquireAltitudeMetres))
        {
            RetireAll();
            PublishInactiveTelemetry();
            return;
        }
        _visible = true;
        var referenceDirection = activeSurfaceAnchor is { } surfaceAnchor
            ? surfaceAnchor.NormalizedBodyFixedDirection
            : radial;
        _presentationFrame = PlanetarySphericalBillboardFrame.Resolve(_presentationFrame,
            _bodyId, _bodyRadius, _terrain, referenceDirection);
        _cameraToPresentationOriginMetres = Math.Sqrt(
            (cameraBody-_presentationFrame.SphericalOriginBodyFixed).LengthSquared);
        var originEnvelope = PlanetarySphericalBillboardFrame.MaximumOriginDistanceMetres(
            _bodyRadius,_terrain.MaximumHeightMetres,_configuration.ReleaseAltitudeMetres);
        if(_cameraToPresentationOriginMetres>originEnvelope+1e-6d)
        {
            // SurfaceAnchor remains the geographic/navigation authority, but it
            // is not a lifetime lease on the GPU presentation origin. During an
            // outward or tangential camera move the retained anchor can be far
            // outside the bounded billboard frame while the camera is still
            // below the hierarchy release altitude. Recenter presentation on
            // the current camera radial before any selection/publication work;
            // canonical patch identities and body-fixed vertices are unchanged.
            _presentationFrame = PlanetarySphericalBillboardFrame.Resolve(_presentationFrame,
                _bodyId, _bodyRadius, _terrain, radial);
            _cameraToPresentationOriginMetres = Math.Sqrt(
                (cameraBody-_presentationFrame.SphericalOriginBodyFixed).LengthSquared);
        }
        if(_cameraToPresentationOriginMetres>originEnvelope+1e-6d)
        {
            // Ordinary camera input must never terminate the process. Keep the
            // complete terrain-v5 fallback authoritative if a future numerical
            // or configuration edge escapes the bounded recenter operation.
            // Focused tests retain the strict envelope assertion.
            RetireAll();
            PublishInactiveTelemetry();
            return;
        }

        if (_pending)
        {
            ContinuePending(cameraBody, radial);
            return;
        }
        if (_coarseningActive)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(_coarseningStartedTimestamp).TotalSeconds;
            if (elapsed < PresentationMorphDurationSeconds)
            {
                PublishAuthoritativeTransport();
                _telemetry = BuildTelemetry(_lastSelection, true, 0d, _pendingMaximumProjectedError);
                return;
            }
            _coarseningActive = false;
            CaptureObservation(_coarseningTargetSelection, cameraBody, viewForwardBody.Normalized(),
                verticalFieldOfViewRadians, aspectRatio, viewportHeightPixels, retainedRadius);
            if (StartPending(_coarseningTargetSelection, cameraBody, radial, viewportHeightPixels,
                    verticalFieldOfViewRadians))
            {
                StartBackgroundPreparation();
                ContinuePending(cameraBody, radial);
                return;
            }
        }
        if (_preparationWorker is { IsAlive: true })
        {
            PublishAuthoritativeTransport();
            _telemetry = _telemetry with { DemandedPatchCount = 0,
                AuthoritativePatchCount = _authoritativePatchCount,
                GlobalFallbackActive = true, CompleteCoverage = _authoritativeVisible,
                LastPreparationMilliseconds = 0d };
            return;
        }

        var normalizedForward = viewForwardBody.Normalized();
        if (TryTakePreparedTopologyTranslation(out var prepared))
        {
            _lastSelectionMilliseconds = prepared.SelectionMilliseconds;
            _lastSelectionAsynchronous = true;
            _lastSelectionQueueLatencyMilliseconds =
                System.Diagnostics.Stopwatch.GetElapsedTime(prepared.StartedTimestamp).TotalMilliseconds;
            if (prepared.ReusedPersistentTopology) _persistentTopologyTranslationCount++;
            ApplySelection(prepared.Selection, prepared.CameraBody, prepared.ForwardBody,
                prepared.FieldOfView, prepared.Aspect, prepared.ViewportHeight,
                prepared.NeighborhoodRadius, nearClipMetres,
                allowCoarseningTransition: false,
                reusePersistentTopology: prepared.ReusedPersistentTopology,
                preparedBounds: prepared.PreparedBounds,
                preparedMaximumProjectedError: prepared.MaximumProjectedErrorPixels);
            return;
        }
        if (_selectionWorker is { IsAlive: true })
        {
            _persistentTopologyReuseCount++;
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(_lastSelection, _authoritativeVisible, 0d,
                _pendingMaximumProjectedError);
            return;
        }
        if (_authoritativeVisible && _hasLastSelection &&
            SameResidencyObservation(_lastSelectionCamera, cameraBody,
                _lastSelectionNeighborhoodRadius, retainedRadius,
                _lastSelectionFieldOfView, verticalFieldOfViewRadians,
                _lastSelectionAspect, aspectRatio, _lastSelectionViewportHeight, viewportHeightPixels))
        {
            if(Double3.Dot(_lastSelectionForward,normalizedForward)<1d-5e-15d)_rotationReuseCount++;
            _persistentTopologyReuseCount++;
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(_lastSelection, true, 0d, _pendingMaximumProjectedError);
            return;
        }

        if (_authoritativeVisible && _hasLastSelection &&
            SameTopologyScale(_lastSelectionNeighborhoodRadius, retainedRadius,
                _lastSelectionFieldOfView, verticalFieldOfViewRadians,
                _lastSelectionAspect, aspectRatio, _lastSelectionViewportHeight, viewportHeightPixels))
        {
            StartTopologyTranslation(cameraBody, normalizedForward, verticalFieldOfViewRadians,
                aspectRatio, viewportHeightPixels, retainedRadius, Math.Max(0d, altitude),
                nearClipMetres);
            _persistentTopologyReuseCount++;
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(_lastSelection, true, 0d, _pendingMaximumProjectedError);
            return;
        }

        var body = new PlanetRenderProxy(_bodyId,
            new UniversePosition(Double3.Zero, new ReferenceFrameId(1)), _bodyRadius,
            new Float3(1f, 1f, 1f), "Anchored", true, DoubleQuaternion.Identity);
        var configuration = PlanetaryLodConfiguration.ForViewport(19d, _configuration.MaximumLevel,
            _configuration.TargetPatchPixels, viewportHeightPixels, verticalFieldOfViewRadians,
            _terrain.MaximumHeightMetres, true);
        var selectionStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        // Physical availability is retained around the viewer and cannot be a
        // function of instantaneous look direction. The current KSA production
        // evidence uses one nested billboard mesh around its snapped pivot and
        // defers final visibility to GPU triangle culling. Supplying no CPU
        // frustum here gives NovaCore the same responsibility boundary: a turn
        // cannot discover or retire nearby terrain, while the production TCS
        // still rejects conservatively off-screen primitives before refinement.
        var selection = PlanetaryRepresentationSelector.SelectRetainedNeighborhood(body, cameraBody,
            configuration, Math.Max(0d, altitude), nearClipMetres, retainedRadius, _previousLeaves);
        _lastSelectionMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(selectionStarted).TotalMilliseconds;
        _lastSelectionAsynchronous = false;
        ApplySelection(selection, cameraBody, normalizedForward, verticalFieldOfViewRadians,
            aspectRatio, viewportHeightPixels, retainedRadius, nearClipMetres);
    }

    private void ApplySelection(in PlanetaryLodSelection selection, in Double3 cameraBody,
        in Double3 normalizedForward, double verticalFieldOfViewRadians, double aspectRatio,
        double viewportHeightPixels, double retainedRadius, double nearClipMetres,
        bool allowCoarseningTransition = true,
        bool reusePersistentTopology = false,
        PlanetaryPatchConservativeBounds[]? preparedBounds = null,
        double? preparedMaximumProjectedError = null)
    {
        _ = nearClipMetres;
        var radial = cameraBody.Normalized();
        CaptureObservation(selection, cameraBody, normalizedForward, verticalFieldOfViewRadians,
            aspectRatio, viewportHeightPixels, retainedRadius);
        if (selection.Patches.Length == 0 || selection.Patches.Length > _submission.Length)
        {
            PublishAuthoritativeTransport();
            _telemetry = _telemetry with { DemandedPatchCount = selection.Patches.Length,
                AuthoritativePatchCount = _authoritativePatchCount, CompleteCoverage = false,
                GlobalFallbackActive = true, LastSelectionMilliseconds = _lastSelectionMilliseconds,
                RejectedGenerationReason = PlanetaryDynamicGenerationRejectReason.Capacity };
            return;
        }
        if (_authoritativeVisible && SamePatches(selection.Patches, _previousLeaves))
        {
            _lastSelection = selection;
            _pendingMaximumProjectedError = preparedMaximumProjectedError ??
                MaximumProjectedError(selection, cameraBody, viewportHeightPixels,
                    verticalFieldOfViewRadians);
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(selection, true, 0d, _pendingMaximumProjectedError);
            return;
        }
        if (allowCoarseningTransition &&
            TryCreateCoarseningTransition(selection, out var transitionSelection, out var morphingChildren))
        {
            if (StartPending(transitionSelection, cameraBody, radial, viewportHeightPixels,
                    verticalFieldOfViewRadians, morphingChildren))
            {
                _coarseningTargetSelection = selection;
                StartBackgroundPreparation();
                ContinuePending(cameraBody, radial);
            }
            else
            {
                PublishAuthoritativeTransport();
                _telemetry = BuildTelemetry(_lastSelection, true, 0d, _pendingMaximumProjectedError);
            }
            return;
        }
        if (!StartPending(selection, cameraBody, radial, viewportHeightPixels,
                verticalFieldOfViewRadians, reusePersistentTopology: reusePersistentTopology,
                preparedBounds: preparedBounds,
                preparedMaximumProjectedError: preparedMaximumProjectedError))
        {
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(selection, false, 0d,
                MaximumProjectedError(selection, cameraBody, viewportHeightPixels,
                    verticalFieldOfViewRadians));
            return;
        }
        if (_injectedFailure != PlanetaryDynamicSurfaceFailure.None)
        {
            _injectedFailure = PlanetaryDynamicSurfaceFailure.None;
            for (var index = 0; index < _pendingSelection.Patches.Length; index++)
            {
                var entry = _entries[_slots[index]];
                if (entry.State != PlanetaryAnchoredPatchCacheState.Requested) continue;
                entry.State = PlanetaryAnchoredPatchCacheState.Failed;
                break;
            }
            _pending = false;
            _rejectedGenerationReason = PlanetaryDynamicGenerationRejectReason.InjectedFailure;
            PublishAuthoritativeTransport();
            _telemetry = BuildTelemetry(selection, false, 0d, _pendingMaximumProjectedError);
            return;
        }
        StartBackgroundPreparation();
        ContinuePending(cameraBody, radial);
    }

    private void BeginFrameTelemetry()
    {
        _frameHitsStart = _hits; _frameMissesStart = _misses;
        _frameEvictionsStart = _evictions;
        _framePreparations = 0; _lastSelectionMilliseconds = 0d;
        _lastSelectionAsynchronous = false;
        _rejectedGenerationReason = PlanetaryDynamicGenerationRejectReason.None;
    }

    private void CaptureObservation(in PlanetaryLodSelection selection, in Double3 cameraBody,
        in Double3 normalizedForward, double fieldOfView, double aspect, double viewportHeight,
        double neighborhoodRadius)
    {
        _lastSelection = selection; _lastSelectionCamera = cameraBody;
        _lastSelectionForward = normalizedForward; _lastSelectionFieldOfView = fieldOfView;
        _lastSelectionAspect = aspect; _lastSelectionViewportHeight = viewportHeight;
        _lastSelectionNeighborhoodRadius=neighborhoodRadius;
        _cameraToResidencyCenterMetres=0d;
        _hasLastSelection = true;
    }

    private void StartTopologyTranslation(in Double3 cameraBody, in Double3 normalizedForward,
        double fieldOfView, double aspect, double viewportHeight, double neighborhoodRadius,
        double surfaceAltitude, double nearClipMetres)
    {
        if (_selectionWorker is { IsAlive: true }) return;
        var requestId = Interlocked.Increment(ref _selectionRequestSerial);
        var previousSelection = _lastSelection with
        {
            Patches = _lastSelection.Patches.ToArray(),
            StitchMasks = _lastSelection.StitchMasks.ToArray()
        };
        var previousCamera = _lastSelectionCamera;
        var requestedCamera = cameraBody;
        var requestedForward = normalizedForward;
        var previousLeaves = _previousLeaves.ToArray();
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        lock (_selectionGate) _preparedTopologyTranslation = null;
        _selectionWorker = new Thread(() =>
        {
            var selectionStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            var reused = TryTranslatePersistentTopology(previousSelection,
                previousCamera.Normalized(), requestedCamera.Normalized(), out var selection);
            if (!reused)
            {
                var body = new PlanetRenderProxy(_bodyId,
                    new UniversePosition(Double3.Zero, new ReferenceFrameId(1)), _bodyRadius,
                    new Float3(1f, 1f, 1f), "Anchored", true, DoubleQuaternion.Identity);
                var configuration = PlanetaryLodConfiguration.ForViewport(19d,
                    _configuration.MaximumLevel, _configuration.TargetPatchPixels,
                    viewportHeight, fieldOfView, _terrain.MaximumHeightMetres, true);
                selection = PlanetaryRepresentationSelector.SelectRetainedNeighborhood(body,
                    requestedCamera, configuration, surfaceAltitude, nearClipMetres,
                    neighborhoodRadius, previousLeaves);
            }
            var preparedBounds = new PlanetaryPatchConservativeBounds[selection.Patches.Length];
            for (var index = 0; index < selection.Patches.Length; index++)
                preparedBounds[index] = PlanetaryRepresentationSelector.ConservativeBounds(
                    selection.Patches[index], _bodyRadius, _terrain.MaximumHeightMetres, true);
            var maximumProjectedError = MaximumProjectedError(selection, requestedCamera,
                viewportHeight, fieldOfView);
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(selectionStarted).TotalMilliseconds;
            var result = new PreparedTopologyTranslation(requestId, selection, requestedCamera,
                requestedForward, fieldOfView, aspect, viewportHeight, neighborhoodRadius,
                elapsed, preparedBounds, maximumProjectedError, reused, started);
            lock (_selectionGate)
            {
                if (requestId == Volatile.Read(ref _selectionRequestSerial))
                    _preparedTopologyTranslation = result;
            }
        })
        {
            IsBackground = true,
            Name = "NovaCore persistent topology translation",
            Priority = ThreadPriority.BelowNormal
        };
        _selectionWorker.Start();
    }

    private bool TryTakePreparedTopologyTranslation(out PreparedTopologyTranslation result)
    {
        lock (_selectionGate)
        {
            if (_preparedTopologyTranslation is not { } prepared)
            {
                result = default;
                return false;
            }
            _preparedTopologyTranslation = null;
            _selectionWorker = null;
            result = prepared;
            return true;
        }
    }

    private static bool TryTranslatePersistentTopology(in PlanetaryLodSelection source,
        in Double3 previousDirection, in Double3 requestedDirection,
        out PlanetaryLodSelection translated)
    {
        translated = default;
        if (source.Patches.Length == 0 ||
            !RelaxedCubeSphereProjection.TryAddress(previousDirection, out var previousFace,
                out var previousU, out var previousV) ||
            !RelaxedCubeSphereProjection.TryAddress(requestedDirection, out var requestedFace,
                out var requestedU, out var requestedV) || previousFace != requestedFace)
            return false;

        var referencePatches = source.Patches.Where(patch => patch.Face == previousFace).ToArray();
        if (referencePatches.Length == 0) return false;
        var minimumLevel = referencePatches.Min(patch => patch.Level);
        if (minimumLevel is < 0 or > MaximumLevel) return false;
        var referenceCells = 1 << minimumLevel;
        var previousReferenceX = Math.Min((int)Math.Floor(previousU * referenceCells), referenceCells - 1);
        var previousReferenceY = Math.Min((int)Math.Floor(previousV * referenceCells), referenceCells - 1);
        var requestedReferenceX = Math.Min((int)Math.Floor(requestedU * referenceCells), referenceCells - 1);
        var requestedReferenceY = Math.Min((int)Math.Floor(requestedV * referenceCells), referenceCells - 1);
        var referenceDeltaX = requestedReferenceX - previousReferenceX;
        var referenceDeltaY = requestedReferenceY - previousReferenceY;
        if (referenceDeltaX == 0 && referenceDeltaY == 0)
        {
            translated = source;
            return true;
        }

        var patches = new PlanetaryPatch[source.Patches.Length];
        var active = new HashSet<PlanetaryPatch>(source.Patches.Length);
        for (var index = 0; index < source.Patches.Length; index++)
        {
            var patch = source.Patches[index];
            if (patch.Level is < 0 or > MaximumLevel) return false;
            // The retained selection also contains coarse complete-coverage
            // patches on non-reference faces. They are persistent global
            // topology and do not move when the local billboard pivot shifts.
            if (patch.Face != previousFace)
            {
                if (!active.Add(patch)) return false;
                patches[index] = patch;
                continue;
            }
            var cells = 1 << patch.Level;
            var scale = 1 << (patch.Level - minimumLevel);
            var x = patch.X + referenceDeltaX * scale;
            var y = patch.Y + referenceDeltaY * scale;
            if (x < 0 || y < 0 || x >= cells || y >= cells) return false;
            if ((patch.X == 0 || patch.Y == 0 || patch.X == cells - 1 || patch.Y == cells - 1 ||
                 x == 0 || y == 0 || x == cells - 1 || y == cells - 1) &&
                (referenceDeltaX != 0 || referenceDeltaY != 0)) return false;
            var value = new PlanetaryPatch(requestedFace, patch.Level, x, y);
            if (!active.Add(value)) return false;
            patches[index] = value;
        }

        translated = source with
        {
            Patches = patches,
            // One integer translation in the coarsest active lattice is scaled
            // exactly through every finer level. Parent/child alignment and
            // therefore the complete mixed-LOD stitch topology are invariant.
            StitchMasks = source.StitchMasks.ToArray(),
            SplitPatchCount = 0,
            MergedPatchCount = 0,
            ParentFallbackCount = 0,
            PendingChildCount = 0
        };
        return true;
    }

    private bool StartPending(in PlanetaryLodSelection selection, in Double3 cameraBody,
        in Double3 nadir, double viewportHeight, double fieldOfView,
        HashSet<PlanetaryPatch>? morphToParent = null,
        bool reusePersistentTopology = false,
        PlanetaryPatchConservativeBounds[]? preparedBounds = null,
        double? preparedMaximumProjectedError = null)
    {
        _requested.Clear();
        for (var index = 0; index < selection.Patches.Length; index++)
        {
            var patch = selection.Patches[index];
            _requested.Add(new PlanetaryAnchoredPatchKey(_bodyId, _terrain.Version,
                _physicalSurfaceGeneration, patch.Face, patch.Level, patch.X, patch.Y));
        }
        var available = _entries.Count(entry => entry.State == PlanetaryAnchoredPatchCacheState.Absent ||
            entry.State != PlanetaryAnchoredPatchCacheState.Authoritative && !_requested.Contains(entry.Key));
        var missing = _requested.Count(key => !_slotByKey.TryGetValue(key, out var slot) ||
            _entries[slot].State == PlanetaryAnchoredPatchCacheState.Absent);
        if (missing > available)
        {
            _rejectedGenerationReason = PlanetaryDynamicGenerationRejectReason.Capacity;
            // Transaction capacity failure can delay a geographic replacement;
            // it can never suppress the previous complete owner or force a
            // synchronous bootstrap on the following render callback.
            return false;
        }

        _retainedDemandedPatchCount = 0;
        _pendingPreparationCount = 0;
        for (var index = 0; index < selection.Patches.Length; index++)
        {
            var patch = selection.Patches[index];
            var key = new PlanetaryAnchoredPatchKey(_bodyId, _terrain.Version,
                _physicalSurfaceGeneration, patch.Face, patch.Level, patch.X, patch.Y);
            if (_slotByKey.TryGetValue(key, out var retained) &&
                _entries[retained].State != PlanetaryAnchoredPatchCacheState.Absent)
                _retainedDemandedPatchCount++;
            _slots[index] = Acquire(key, _requested);
            if (_entries[_slots[index]].State == PlanetaryAnchoredPatchCacheState.Requested)
                _preparationIndices[_pendingPreparationCount++] = index;
        }
        _newDemandedPatchCount = selection.Patches.Length - _retainedDemandedPatchCount;
        _releasedDemandedPatchCount = Math.Max(0, _previousLeaves.Length - _retainedDemandedPatchCount);
        _lastDescriptorDeltaCount = _newDemandedPatchCount + _releasedDemandedPatchCount;
        _demandOverlapPercent = selection.Patches.Length == 0 ? 100d :
            100d * _retainedDemandedPatchCount / selection.Patches.Length;
        PrioritizeNadir(selection.Patches, nadir);
        _pendingSelection = selection; _pending = true; _preparedPendingPatchCount = 0;
        _pendingStartedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        _lastReportedPreparedPendingPatchCount = 0;
        _activeGeneration = _activeGeneration == uint.MaxValue ? 1u : _activeGeneration + 1u;
        _gpuReadyGeneration = 0u;
        _pendingMaximumProjectedError = preparedMaximumProjectedError ??
            MaximumProjectedError(selection, cameraBody, viewportHeight, fieldOfView);
        _morphToParentSet.Clear();
        if (morphToParent is not null) _morphToParentSet.UnionWith(morphToParent);
        _pendingReusesPersistentTopology = reusePersistentTopology;
        InitializePendingSubmission(preparedBounds);
        _morphToParentSet.Clear();
        return true;
    }

    private bool TryCreateCoarseningTransition(in PlanetaryLodSelection target,
        out PlanetaryLodSelection transition, out HashSet<PlanetaryPatch> morphingChildren)
    {
        morphingChildren = new HashSet<PlanetaryPatch>();
        transition = default;
        if (!_authoritativeVisible || _previousLeaves.Length == 0) return false;
        foreach (var parent in target.Patches)
        {
            var quartet = true;
            for (var child = 0; child < 4; child++)
                quartet &= _previousLeafSet.Contains(parent.Child(child));
            if (!quartet) continue;
            for (var child = 0; child < 4; child++) morphingChildren.Add(parent.Child(child));
        }
        if (morphingChildren.Count == 0) return false;
        var previous = _previousLeaves.ToArray();
        transition = target with
        {
            Patches = previous,
            StitchMasks = PlanetaryRepresentationSelector.ComputeStitchMasks(previous, _previousLeafSet),
            MaximumLevel = previous.Max(patch => patch.Level),
            MergedPatchCount = 0
        };
        return true;
    }

    private void PrioritizeNadir(PlanetaryPatch[] patches, in Double3 nadir)
    {
        if (!RelaxedCubeSphereProjection.TryAddress(nadir, out var face, out var u, out var v)) return;
        for (var order = 0; order < patches.Length; order++)
        {
            var patch = patches[order]; if (patch.Face != face) continue;
            var bounds = patch.Bounds;
            if (u < bounds.MinX || u > bounds.MaxX || v < bounds.MinY || v > bounds.MaxY) continue;
            (_preparationIndices[0], _preparationIndices[order]) =
                (_preparationIndices[order], _preparationIndices[0]);
            return;
        }
    }

    private void ContinuePending(in Double3 cameraBody, in Double3 nadir)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        _preparedPendingPatchCount = 0;
        var complete = true;
        for (var index = 0; index < _pendingSelection.Patches.Length; index++)
        {
            var entry = _entries[_slots[index]];
            var ready = entry.State is PlanetaryAnchoredPatchCacheState.Ready or
                PlanetaryAnchoredPatchCacheState.Cached or PlanetaryAnchoredPatchCacheState.Authoritative;
            if (ready) _preparedPendingPatchCount++; else complete = false;
            var persistent = _submission[index].Flags &
                (SubmissionLocalPayloadRequired | SubmissionMorphFromParent | SubmissionMorphToParent);
            _submission[index].Flags = (ready ? CpuCompleteSubmissionFlags : 0u) | persistent;
        }
        _framePreparations = Math.Max(0,
            _preparedPendingPatchCount - _lastReportedPreparedPendingPatchCount);
        _lastReportedPreparedPendingPatchCount = _preparedPendingPatchCount;
        _activePatchCount = _pendingSelection.Patches.Length;
        if (complete && Volatile.Read(ref _backgroundCompleteGeneration) == _activeGeneration &&
            _gpuReadyGeneration == _activeGeneration && _activeGeneration != 0u)
            PromotePending();
        _telemetry = BuildTelemetry(_pending ? _pendingSelection : _lastSelection,
            !_pending && _authoritativeVisible, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            _pendingMaximumProjectedError);
    }

    private void StartBackgroundPreparation()
    {
        if (_preparationWorker is { IsAlive: true })
            throw new InvalidOperationException("A bounded dynamic-surface preparation worker is already active.");
        var generation = _activeGeneration;
        var workCount = _pendingPreparationCount;
        _lastPreparationBatchSize = workCount;
        Volatile.Write(ref _lastPreparationWorkerCount, 0);
        Array.Clear(_preparationWorkerThreadIds);
        _backgroundCompleteGeneration = 0u;
        _lastBackgroundPreparationMilliseconds = 0d;
        _preparationWorker = new Thread(() =>
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            void PrepareOrder(int order)
            {
                RegisterPreparationWorker();
                var index = _preparationIndices[order];
                var slot = _slots[index];
                var entry = _entries[slot];
                if (entry.State != PlanetaryAnchoredPatchCacheState.Requested) return;
                Prepare(slot, _pendingSelection.Patches[index], entry.Key, entry.Generation);
            }
            if (workCount > 0)
            {
                var workerCount = Math.Min(ConfiguredWorkerCount, workCount);
                Parallel.For(0, workerCount, new ParallelOptions
                {
                    MaxDegreeOfParallelism = workerCount
                }, workerIndex =>
                {
                    // Coarse deterministic partitions avoid creating one tiny
                    // scheduler item per geographic descriptor.
                    for (var order = workerIndex; order < workCount; order += workerCount)
                        PrepareOrder(order);
                });
            }
            var workers = 0;
            for (var index = 0; index < _preparationWorkerThreadIds.Length; index++)
                if (Volatile.Read(ref _preparationWorkerThreadIds[index]) != 0) workers++;
            Volatile.Write(ref _lastPreparationWorkerCount, workers);
            Volatile.Write(ref _lastBackgroundPreparationMilliseconds,
                System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            Volatile.Write(ref _backgroundCompleteGeneration, generation);
        })
        {
            IsBackground = true,
            Name = "NovaCore bounded anchored-surface preparation",
            Priority = ThreadPriority.BelowNormal
        };
        _preparationWorker.Start();
    }

    private void RegisterPreparationWorker()
    {
        var threadId = Environment.CurrentManagedThreadId;
        for (var index = 0; index < _preparationWorkerThreadIds.Length; index++)
        {
            var existing = Volatile.Read(ref _preparationWorkerThreadIds[index]);
            if (existing == threadId) return;
            if (existing == 0 && Interlocked.CompareExchange(
                    ref _preparationWorkerThreadIds[index], threadId, 0) == 0) return;
        }
    }

    private void InitializePendingSubmission(PlanetaryPatchConservativeBounds[]? preparedBounds = null)
    {
        _transportGeneration = 0u;
        _transportPatchCount = 0;
        for (var index = 0; index < _pendingSelection.Patches.Length; index++)
        {
            var patch = _pendingSelection.Patches[index]; var slot = _slots[index];
            var entry = _entries[slot];
            var bounds = preparedBounds is not null && index < preparedBounds.Length
                ? (preparedBounds[index].Center, preparedBounds[index].Radius)
                : Bounds(patch);
            var localRequired = RequiresLocalPayload(patch);
            var morphFromParent = patch.Parent is { } parent && _previousLeafSet.Contains(parent);
            var morphToParent = _morphToParentSet.Contains(patch);
            _submission[index] = new NativeAnchoredSurfacePatch
            {
                BodyIdLow = (uint)_bodyId, BodyIdHigh = (uint)(_bodyId >> 32),
                TerrainVersion = _terrain.Version, PhysicalSurfaceGeneration = _physicalSurfaceGeneration,
                Face = (uint)patch.Face, Level = (uint)patch.Level, X = (uint)patch.X, Y = (uint)patch.Y,
                CacheSlot = (uint)slot, CacheGeneration = entry.Generation,
                StitchMask = (uint)_pendingSelection.StitchMasks[index],
                Flags = (localRequired ? SubmissionLocalPayloadRequired : 0u) |
                    (morphFromParent ? SubmissionMorphFromParent : 0u) |
                    (morphToParent ? SubmissionMorphToParent : 0u),
                MaterialLevel = (uint)Math.Min(patch.Level, PlanetaryCubeSurfacePackContract.MaximumLevel),
                MaterialX = (uint)(patch.X >> Math.Max(0, patch.Level - PlanetaryCubeSurfacePackContract.MaximumLevel)),
                MaterialY = (uint)(patch.Y >> Math.Max(0, patch.Level - PlanetaryCubeSurfacePackContract.MaximumLevel)),
                MaterialGeneration = _physicalSurfaceGeneration,
                BoundsX = (float)bounds.Center.X, BoundsY = (float)bounds.Center.Y,
                BoundsZ = (float)bounds.Center.Z, BoundsRadius = (float)bounds.Radius
            };
        }
    }

    private void PromotePending()
    {
        for (var index = 0; index < _entries.Length; index++)
            if (_entries[index].State == PlanetaryAnchoredPatchCacheState.Authoritative)
                _entries[index].State = PlanetaryAnchoredPatchCacheState.Cached;
        for (var index = 0; index < _pendingSelection.Patches.Length; index++)
        {
            var entry = _entries[_slots[index]]; entry.State = PlanetaryAnchoredPatchCacheState.Authoritative;
            _submission[index].Flags |= SubmissionAuthoritative | SubmissionSynchronizationComplete;
            _authoritativeSubmission[index] = _submission[index];
        }
        _authoritativePatchCount = _pendingSelection.Patches.Length;
        _authoritativeGeneration = _activeGeneration;
        _previousLeaves = _pendingSelection.Patches;
        _previousLeafSet.Clear();
        foreach (var patch in _previousLeaves) _previousLeafSet.Add(patch);
        _lastSelection = _pendingSelection;
        _authoritativeVisible = true; _pending = false;
        _transportGeneration = _authoritativeGeneration;
        _transportPatchCount = _authoritativePatchCount;
        _lastPublicationLatencyMilliseconds = _pendingStartedTimestamp == 0 ? 0d :
            System.Diagnostics.Stopwatch.GetElapsedTime(_pendingStartedTimestamp).TotalMilliseconds;
        if (_authoritativeSubmission.Take(_authoritativePatchCount)
            .Any(patch => (patch.Flags & SubmissionMorphToParent) != 0u))
        {
            _coarseningActive = true;
            _coarseningStartedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }

    private void PublishAuthoritativeTransport()
    {
        if (!_authoritativeVisible || _authoritativePatchCount == 0)
        {
            _activePatchCount = 0;
            return;
        }
        if (_transportGeneration != _authoritativeGeneration ||
            _transportPatchCount != _authoritativePatchCount)
        {
            Array.Copy(_authoritativeSubmission, _submission, _authoritativePatchCount);
            _transportGeneration = _authoritativeGeneration;
            _transportPatchCount = _authoritativePatchCount;
        }
        _activePatchCount = _authoritativePatchCount;
        _activeGeneration = _authoritativeGeneration;
    }

    private static bool SamePatches(PlanetaryPatch[] left, PlanetaryPatch[] right) =>
        left.AsSpan().SequenceEqual(right);

    private static bool SameResidencyObservation(in Double3 previousCamera, in Double3 camera,
        double previousNeighborhoodRadius,double neighborhoodRadius,double previousFov, double fov,
        double previousAspect, double aspect, double previousViewport, double viewport)
    {
        var recenterDistance=Math.Clamp(previousNeighborhoodRadius*ResidencyRecenterRadiusFraction,
            MinimumResidencyRecenterMetres,MaximumResidencyRecenterMetres);
        return (camera - previousCamera).LengthSquared <= recenterDistance*recenterDistance &&
            Math.Abs(previousNeighborhoodRadius-neighborhoodRadius)<=recenterDistance &&
            Math.Abs(previousFov - fov) <= 1e-12d && Math.Abs(previousAspect - aspect) <= 1e-12d &&
            Math.Abs(previousViewport - viewport) <= .25d;
    }

    private static bool SameTopologyScale(double previousNeighborhoodRadius,double neighborhoodRadius,
        double previousFov,double fov,double previousAspect,double aspect,
        double previousViewport,double viewport)
    {
        var retainedTolerance=Math.Clamp(previousNeighborhoodRadius*ResidencyRecenterRadiusFraction,
            MinimumResidencyRecenterMetres,MaximumResidencyRecenterMetres);
        return Math.Abs(previousNeighborhoodRadius-neighborhoodRadius)<=retainedTolerance&&
            Math.Abs(previousFov-fov)<=1e-12d&&Math.Abs(previousAspect-aspect)<=1e-12d&&
            Math.Abs(previousViewport-viewport)<=.25d;
    }

    private static bool Same(double left, double right) =>
        BitConverter.DoubleToInt64Bits(left) == BitConverter.DoubleToInt64Bits(right);

    private static bool Same(in Double3 left, in Double3 right) =>
        Same(left.X, right.X) && Same(left.Y, right.Y) && Same(left.Z, right.Z);

    private int Acquire(in PlanetaryAnchoredPatchKey key, HashSet<PlanetaryAnchoredPatchKey> requested)
    {
        _serial++;
        if (_slotByKey.TryGetValue(key, out var cached) &&
            _entries[cached].State != PlanetaryAnchoredPatchCacheState.Absent)
        {
            _entries[cached].LastUse = _serial; _hits++;
            _cachedLru.Enqueue(cached, _serial);
            if (_entries[cached].State == PlanetaryAnchoredPatchCacheState.Failed)
                _entries[cached].State = PlanetaryAnchoredPatchCacheState.Requested;
            return cached;
        }
        _misses++;
        var selected = _freeSlots.Count == 0 ? -1 : _freeSlots.Pop();
        if (selected < 0)
        {
            _deferredLru.Clear();
            while (_cachedLru.TryDequeue(out var candidate, out var priority))
            {
                var entry = _entries[candidate];
                if (entry.State == PlanetaryAnchoredPatchCacheState.Absent || entry.LastUse != priority)
                    continue;
                if (requested.Contains(entry.Key) ||
                    entry.State == PlanetaryAnchoredPatchCacheState.Authoritative)
                {
                    _deferredLru.Add((candidate, priority));
                    continue;
                }
                selected = candidate;
                break;
            }
            foreach (var deferred in _deferredLru)
                _cachedLru.Enqueue(deferred.Slot, deferred.Priority);
        }
        if (selected < 0) return 0;
        if (_entries[selected].State != PlanetaryAnchoredPatchCacheState.Absent)
        {
            _slotByKey.Remove(_entries[selected].Key);
            _evictions++;
        }
        var generation = _entries[selected].Generation == uint.MaxValue ? 1u : _entries[selected].Generation + 1u;
        _entries[selected].Key = key; _entries[selected].Generation = generation;
        _entries[selected].LastUse = _serial; _entries[selected].State = PlanetaryAnchoredPatchCacheState.Requested;
        _slotByKey.Add(key, selected);
        _cachedLru.Enqueue(selected, _serial);
        return selected;
    }

    private void Prepare(int slot, in PlanetaryPatch patch,
        in PlanetaryAnchoredPatchKey expectedKey, uint expectedGeneration)
    {
        var entry = _entries[slot]; entry.State = PlanetaryAnchoredPatchCacheState.Preparing;
        // The elevation oracle, modifier generation, and NCCUBE2 lookup are
        // persistent physical authorities validated independently at runtime.
        // An entering patch prepares only immutable geographic descriptor
        // identity; it must not re-evaluate H at five CPU points merely because
        // the camera-relative footprint moved.
        var cells = patch.Level is >= 0 and <= MaximumLevel ? 1 << patch.Level : 0;
        if (cells == 0 || patch.X < 0 || patch.Y < 0 || patch.X >= cells || patch.Y >= cells ||
            expectedKey.Face != patch.Face || expectedKey.Level != patch.Level ||
            expectedKey.X != patch.X || expectedKey.Y != patch.Y)
        {
            entry.State = PlanetaryAnchoredPatchCacheState.Failed;
            return;
        }
        if (entry.Generation == expectedGeneration && entry.Key == expectedKey &&
            entry.State == PlanetaryAnchoredPatchCacheState.Preparing)
        {
            entry.State = PlanetaryAnchoredPatchCacheState.Resident;
            entry.State = PlanetaryAnchoredPatchCacheState.Ready;
            Interlocked.Add(ref _uploadBytes, GpuPatchDescriptorBytes);
        }
    }

    private void RetireAll()
    {
        Interlocked.Increment(ref _selectionRequestSerial);
        lock (_selectionGate) _preparedTopologyTranslation = null;
        for (var index = 0; index < _entries.Length; index++)
            if (_entries[index].State == PlanetaryAnchoredPatchCacheState.Authoritative)
                _entries[index].State = PlanetaryAnchoredPatchCacheState.Cached;
        _activePatchCount = 0; _authoritativePatchCount = 0;
        _visible = false; _authoritativeVisible = false; _pending = false; _coarseningActive = false;
        _authoritativeGeneration = 0u; _hasLastSelection = false; _previousLeaves = [];
        _transportGeneration = 0u; _transportPatchCount = 0;
        _previousLeafSet.Clear();
    }

    private void PublishInactiveTelemetry()
    {
        // Cached residency is intentionally retained for a later scale-appropriate
        // approach. All per-frame near-field work, pending-publication state, and
        // demand metrics must nevertheless read as inactive in Solar overview.
        _telemetry = _telemetry with
        {
            DemandedPatchCount = 0,
            AuthoritativePatchCount = 0,
            MinimumLevel = 0,
            MaximumLevel = 0,
            CompleteCoverage = true,
            GlobalFallbackActive = true,
            MaximumProjectedErrorPixels = 0d,
            LastPreparationMilliseconds = 0d,
            LastBackgroundPreparationMilliseconds = 0d,
            LastSelectionMilliseconds = 0d,
            RetainedDemandedPatchCount = 0,
            NewDemandedPatchCount = 0,
            ReleasedDemandedPatchCount = 0,
            DemandOverlapPercent = 100d,
            FrameCacheHits = 0,
            FrameCacheMisses = 0,
            FrameEvictions = 0,
            FramePreparations = 0,
            FrameUploadBytes = 0,
            PendingPatchCount = 0,
            PreparedPendingPatchCount = 0,
            NadirCovered = false,
            PreparationQueueDepth = 0,
            MainThreadTerrainMilliseconds = 0d,
            RejectedGenerationReason = PlanetaryDynamicGenerationRejectReason.None,
            DescriptorDeltaCount = 0,
            LastSelectionQueueLatencyMilliseconds = 0d,
            SelectionPending = false,
            PresentationGeneration = _presentationFrame.PresentationGeneration
        };
    }

    private PlanetaryDynamicAnchoredTelemetry BuildTelemetry(in PlanetaryLodSelection selection,
        bool complete, double preparationMilliseconds, double maximumProjectedError)
    {
        var resident = 0;
        foreach (var entry in _entries) if (entry.State is PlanetaryAnchoredPatchCacheState.Ready or
            PlanetaryAnchoredPatchCacheState.Authoritative or PlanetaryAnchoredPatchCacheState.Cached) resident++;
        var minimum = selection.Patches.Length == 0 ? 0 : selection.Patches.Min(p => p.Level);
        var maximum = selection.Patches.Length == 0 ? 0 : selection.Patches.Max(p => p.Level);
        var uploadBytes = Interlocked.Read(ref _uploadBytes);
        var frameUploadBytes = uploadBytes - _frameUploadBytesStart;
        _frameUploadBytesStart = uploadBytes;
        var selectionPending = SelectionWorkPending();
        return new(selection.Patches.Length, _authoritativeVisible ? _authoritativePatchCount : 0, resident,
            minimum, maximum, _entries.Length, _hits, _misses, _evictions, uploadBytes,
            _authoritativeGeneration, complete, true, maximumProjectedError,
            preparationMilliseconds, Volatile.Read(ref _lastBackgroundPreparationMilliseconds),
            _lastSelectionMilliseconds,
            _retainedDemandedPatchCount, _newDemandedPatchCount, _releasedDemandedPatchCount,
            _demandOverlapPercent, _hits - _frameHitsStart, _misses - _frameMissesStart,
            _evictions - _frameEvictionsStart, _framePreparations,
            frameUploadBytes, _pending ? _pendingSelection.Patches.Length : 0,
            _pending ? _preparedPendingPatchCount : 0, CoversDirection(_lastSelectionCamera),
            ConfiguredWorkerCount, Volatile.Read(ref _lastPreparationWorkerCount),
            100d * Volatile.Read(ref _lastPreparationWorkerCount) / ConfiguredWorkerCount,
            _lastPreparationBatchSize, (_pending ? 1 : 0) + (selectionPending ? 1 : 0),
            _lastPublicationLatencyMilliseconds,
            preparationMilliseconds + (_lastSelectionAsynchronous ? 0d : _lastSelectionMilliseconds),
            _rejectedGenerationReason,
            _persistentTopologyReuseCount,
            _persistentTopologyTranslationCount,
            _lastDescriptorDeltaCount,
            _lastSelectionQueueLatencyMilliseconds,
            selectionPending,
            _presentationFrame.PresentationGeneration);
    }

    private bool SelectionWorkPending()
    {
        if (_selectionWorker is { IsAlive: true }) return true;
        lock (_selectionGate) return _preparedTopologyTranslation.HasValue;
    }

    private double MaximumProjectedError(in PlanetaryLodSelection selection,
        in Double3 cameraBody, double viewportHeight, double fieldOfView)
    {
        var maximum = 0d; var tanHalfFov = Math.Tan(fieldOfView * .5d);
        foreach (var patch in selection.Patches)
            maximum = Math.Max(maximum, ProjectedErrorPixels(patch, cameraBody,
                viewportHeight, tanHalfFov));
        return maximum;
    }

    private (Double3 Center, double Radius) Bounds(in PlanetaryPatch patch)
    {
        var bounds = PlanetaryRepresentationSelector.ConservativeBounds(patch, _bodyRadius,
            _terrain.MaximumHeightMetres, true);
        return (bounds.Center, bounds.Radius);
    }

    private bool RequiresLocalPayload(in PlanetaryPatch patch)
    {
        if (!EarthLocalTerrainElevationDataset.IsLoaded) return false;
        return EarthLocalTerrainElevationDataset.Intersects(patch.Face, patch.Level, patch.X, patch.Y);
    }

    private double ProjectedErrorPixels(in PlanetaryPatch patch, in Double3 cameraBody,
        double viewportHeight, double tanHalfFov)
    {
        var b = patch.Bounds;
        var center = RelaxedCubeSphereProjection.UnitDirection(patch.Face,
            (b.MinX + b.MaxX) * .5d, (b.MinY + b.MaxY) * .5d) * _bodyRadius;
        var corner = RelaxedCubeSphereProjection.UnitDirection(patch.Face, b.MinX, b.MinY) * _bodyRadius;
        var span = 2d * Math.Sqrt((corner - center).LengthSquared);
        var distance = Math.Max(1d, Math.Sqrt((cameraBody - center).LengthSquared));
        return span * viewportHeight / (2d * tanHalfFov * distance);
    }

}
