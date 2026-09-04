using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public sealed record PlanetaryProductionBillboardPreparedGeneration(
    PlanetaryProductionSphericalBillboardTopology Topology,
    PlanetaryProductionBillboardPupil Pupil,
    PlanetaryProductionBillboardPhysicalPreparation Physical,
    NativeProductionBillboardLatticeVertex[] Lattice,
    uint[] Indices,
    uint PupilFrameIdentity,
    ulong PublicationGeneration,
    PlanetaryProductionBillboardReuse Reuse,
    double PreparationMilliseconds,
    bool TopologyUploadRequired,
    PlanetaryProductionCullContract CullContract = default,
    bool NativeGpuPhysicalPreparation = false);

public readonly record struct PlanetaryProductionBillboardMovingTelemetry(
    ulong CompletedFrames,
    int CurrentLevel,
    int IncomingLevel,
    uint PupilGeneration,
    uint PupilFrameIdentity,
    double PupilAngularErrorRadians,
    int ActiveSamples,
    int ReusedSamples,
    int NewSamples,
    ulong TopologyUploads,
    ulong Publications,
    ulong ZeroOwnerFrames,
    ulong OverlapOwnerFrames,
    ulong StaleGenerationDraws,
    ulong ResidentGpuBytes,
    ulong PeakResidentGpuBytes,
    double LastSelectorMilliseconds,
    double LastPupilMilliseconds,
    double LastSchedulingMilliseconds,
    double LastPreparationMilliseconds,
    double LastPublicationMilliseconds);

public readonly record struct PlanetaryProductionBillboardTiming(double AverageMilliseconds,
    double P50Milliseconds, double P95Milliseconds, double P99Milliseconds,
    double MaximumMilliseconds, int Samples);

public readonly record struct PlanetaryProductionBillboardTimingSummary(
    PlanetaryProductionBillboardTiming Callback,
    PlanetaryProductionBillboardTiming Selector,
    PlanetaryProductionBillboardTiming PupilAndSnap,
    PlanetaryProductionBillboardTiming DemandScheduling,
    PlanetaryProductionBillboardTiming PhysicalPreparation,
    PlanetaryProductionBillboardTiming GpuPreparation,
    PlanetaryProductionBillboardTiming Publication);

/// <summary>
/// P2S5C2 camera-driven managed coordinator. Preparation is bounded to one
/// background generation; renderer publication is acknowledged only after the
/// native C1 readiness fence has completed.
/// </summary>
public sealed class PlanetaryProductionSphericalBillboardMovingRuntime
{
    private readonly IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> _levels;
    private readonly PlanetaryProductionSphericalBillboardSelector _selector;
    private readonly PlanetaryProductionBillboardPhysicalCache _cache = new();
    private readonly Func<PlanetaryProductionSphericalBillboardTopology,
        PlanetaryProductionBillboardPupil, PlanetaryProductionBillboardPhysicalCache,
        PlanetaryProductionBillboardPhysicalPreparation> _prepare;
    private readonly Func<PlanetaryProductionSphericalBillboardTopology,
        PlanetaryProductionCullContract> _cullContract;
    private readonly bool _nativeGpuPhysicalPreparation;
    private readonly HashSet<ulong> _submittedTopologyPayloads = new();
    private Task<PlanetaryProductionBillboardPreparedGeneration>? _preparing;
    private PlanetaryProductionBillboardPreparedGeneration? _ready;
    private PlanetaryProductionBillboardPreparedGeneration? _submitted;
    private PlanetaryProductionBillboardPreparedGeneration? _current;
    private ulong _nextGeneration;
    private uint _nextPupilFrameIdentity;
    private ulong _topologyUploads;
    private ulong _publications;
    private ulong _staleGenerationDraws;
    private ulong _peakResidentGpuBytes;
    private double _selectorMs, _pupilMs, _schedulingMs, _preparationMs, _publicationMs;
    private readonly TimingWindow _callbackTiming = new();
    private readonly TimingWindow _selectorTiming = new();
    private readonly TimingWindow _pupilTiming = new();
    private readonly TimingWindow _schedulingTiming = new();
    private readonly TimingWindow _preparationTiming = new();
    private readonly TimingWindow _gpuPreparationTiming = new();
    private readonly TimingWindow _publicationTiming = new();

    public PlanetaryProductionSphericalBillboardMovingRuntime(string repositoryRoot,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
        : this(levels, (topology, pupil, cache) =>
            PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
                repositoryRoot, topology, pupil, cache, maximumParitySamples: 64), null) { }

    public PlanetaryProductionSphericalBillboardMovingRuntime(string repositoryRoot,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,
        IReadOnlyDictionary<ulong, PlanetaryProductionCullContract> cullContracts)
        : this(levels, (topology, pupil, cache) =>
            PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
                repositoryRoot, topology, pupil, cache, maximumParitySamples: 64),
            topology => cullContracts.TryGetValue(topology.TopologyHash, out var contract)
                ? contract
                : throw new InvalidOperationException("Missing topology-specific culling contract."), true) { }

    public PlanetaryProductionSphericalBillboardMovingRuntime(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,
        Func<PlanetaryProductionSphericalBillboardTopology,
            PlanetaryProductionBillboardPupil, PlanetaryProductionBillboardPhysicalCache,
            PlanetaryProductionBillboardPhysicalPreparation> prepare,
        Func<PlanetaryProductionSphericalBillboardTopology,
            PlanetaryProductionCullContract>? cullContract = null,
        bool nativeGpuPhysicalPreparation = false)
    {
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(prepare);
        _levels = levels;
        _selector = new(levels);
        _prepare = prepare;
        _cullContract = cullContract ?? (_ => PlanetaryProductionCullContract.Radial);
        _nativeGpuPhysicalPreparation = nativeGpuPhysicalPreparation;
    }

    public PlanetaryProductionBillboardPreparedGeneration? Current => _current;
    public bool ReplacementInFlight => _preparing is not null || _ready is not null || _submitted is not null;
    public PlanetaryProductionBillboardTimingSummary TimingSummary => new(
        _callbackTiming.Snapshot(), _selectorTiming.Snapshot(), _pupilTiming.Snapshot(),
        _schedulingTiming.Snapshot(), _preparationTiming.Snapshot(),
        _gpuPreparationTiming.Snapshot(), _publicationTiming.Snapshot());

    public PlanetaryProductionBillboardMovingTelemetry Update(
        in PlanetaryProductionBillboardView view, uint gpuReadyGeneration)
    {
        if (!view.IsValid) throw new ArgumentOutOfRangeException(nameof(view));
        var start = Stopwatch.GetTimestamp();
        _schedulingMs = 0d;
        CompleteNativePublication(gpuReadyGeneration);
        CompleteBackgroundPreparation();

        var selectorStart = Stopwatch.GetTimestamp();
        var selection = _selector.Evaluate(view, ReplacementInFlight);
        _selectorMs = Stopwatch.GetElapsedTime(selectorStart).TotalMilliseconds;
        _selectorTiming.Add(_selectorMs);

        var pupilStart = Stopwatch.GetTimestamp();
        PlanetaryProductionBillboardPupil desiredPupil;
        if (_current is not null)
        {
            desiredPupil = PlanetaryProductionBillboardPupil.Resolve(_current.Pupil,
                view.CameraBodyDirection, _current.Topology);
            if (desiredPupil.Generation != _current.Pupil.Generation)
                _current = _current with
                {
                    Pupil = desiredPupil,
                    PupilFrameIdentity = NextPupilFrameIdentity()
                };
        }
        else
        {
            desiredPupil = PlanetaryProductionBillboardPupil.Resolve(default,
                view.CameraBodyDirection, _levels[selection.Level]);
        }
        _pupilMs = Stopwatch.GetElapsedTime(pupilStart).TotalMilliseconds;
        _pupilTiming.Add(_pupilMs);

        if (!ReplacementInFlight && (_current is null ||
            _current.Topology.Level != selection.Level))
        {
            var incomingPupil = _current is null
                ? desiredPupil
                : PlanetaryProductionBillboardPupil.Resolve(_current.Pupil,
                    view.CameraBodyDirection, _levels[selection.Level]);
            Schedule(_levels[selection.Level], incomingPupil);
        }

        // Until the first candidate publication, terrain-v5 remains the sole
        // owner. Thereafter the current candidate remains the sole owner while
        // one incoming generation is prepared invisibly.
        if (_current is not null && _current.PublicationGeneration == 0) _staleGenerationDraws++;
        var target = view.CameraBodyDirection.Normalized();
        var angular = desiredPupil.IsValid ? Math.Acos(Math.Clamp(
            Double3.Dot(desiredPupil.PivotDirection, target), -1d, 1d)) : 0d;
        var residentBytes = ResidentGpuBytes(_current) + ResidentGpuBytes(_submitted);
        if (_current is not null && _submitted is not null &&
            _current.Topology.TopologyHash == _submitted.Topology.TopologyHash)
            residentBytes -= checked((ulong)_submitted.Lattice.Length * 16ul +
                (ulong)_submitted.Indices.Length * 4ul);
        _peakResidentGpuBytes = Math.Max(_peakResidentGpuBytes, residentBytes);
        var callbackMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _callbackTiming.Add(callbackMs);
        var active = _submitted ?? _ready ?? (_preparing is null ? _current : null);
        return new(view.CompletedFrame, _current?.Topology.Level ?? -1,
            _submitted?.Topology.Level ?? _ready?.Topology.Level ?? -1,
            desiredPupil.Generation, _current?.PupilFrameIdentity ?? 0u,
            angular, active?.Reuse.ActiveSamples ?? 0,
            active?.Reuse.ReusedSamples ?? 0, active?.Reuse.NewSamples ?? 0,
            _topologyUploads, _publications, 0, 0,
            _staleGenerationDraws, residentBytes, _peakResidentGpuBytes,
            _selectorMs, _pupilMs, _schedulingMs,
            _preparationMs, _publicationMs);
    }

    public bool TrySubmitPrepared(out PlanetaryProductionBillboardPreparedGeneration generation)
    {
        CompleteBackgroundPreparation();
        if (_ready is null || _submitted is not null)
        {
            generation = null!;
            return false;
        }
        generation = _ready;
        _submitted = _ready;
        _ready = null;
        return true;
    }

    private void Schedule(PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardPupil pupil)
    {
        var generation = ++_nextGeneration;
        var pupilFrameIdentity = NextPupilFrameIdentity();
        var previous = _current;
        var scheduled = Stopwatch.GetTimestamp();
        _preparing = Task.Run(() =>
        {
            var begin = Stopwatch.GetTimestamp();
            PlanetaryProductionBillboardPhysicalPreparation physical;
            if (_nativeGpuPhysicalPreparation)
            {
                physical = new(Array.Empty<NativeSphericalBillboardPhysicalVertex>(), default,
                    0d, 0d, 0, 0,
                    Array.Empty<PlanetaryCanonicalPhysicalSampleIdentity>());
            }
            else
            {
                if (previous is not null) _cache.RetainOnly(previous.Physical.Identities);
                physical = _prepare(topology, pupil, _cache);
            }
            bool topologyUpload;
            lock (_submittedTopologyPayloads)
                topologyUpload = _submittedTopologyPayloads.Add(topology.TopologyHash);
            var reuse = new PlanetaryProductionBillboardReuse(topology.Vertices.Count,
                physical.ReusedSamples,
                _nativeGpuPhysicalPreparation ? topology.Vertices.Count : physical.PreparedSamples);
            return new PlanetaryProductionBillboardPreparedGeneration(topology, pupil, physical,
                topology.NativeLattice, topology.NativeIndices, pupilFrameIdentity, generation, reuse,
                Stopwatch.GetElapsedTime(begin).TotalMilliseconds, topologyUpload,
                _cullContract(topology), _nativeGpuPhysicalPreparation);
        });
        _schedulingMs = Stopwatch.GetElapsedTime(scheduled).TotalMilliseconds;
        _schedulingTiming.Add(_schedulingMs);
    }

    private uint NextPupilFrameIdentity()
    {
        _nextPupilFrameIdentity = _nextPupilFrameIdentity == uint.MaxValue
            ? 1u
            : _nextPupilFrameIdentity + 1u;
        return _nextPupilFrameIdentity;
    }

    private void CompleteBackgroundPreparation()
    {
        if (_preparing is null || !_preparing.IsCompleted) return;
        _ready = _preparing.GetAwaiter().GetResult();
        _preparing = null;
        _preparationMs = _ready.PreparationMilliseconds;
        _preparationTiming.Add(_preparationMs);
        if (!_ready.NativeGpuPhysicalPreparation)
            _gpuPreparationTiming.Add(_ready.Physical.Metrics.GpuMilliseconds);
        if (_ready.TopologyUploadRequired) _topologyUploads++;
    }

    private void CompleteNativePublication(uint gpuReadyGeneration)
    {
        if (_submitted is null || gpuReadyGeneration != unchecked((uint)_submitted.PublicationGeneration)) return;
        var start = Stopwatch.GetTimestamp();
        _current = _submitted;
        _submitted = null;
        _selector.CommitPublication(_current.Topology.Level);
        _publications++;
        _publicationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _publicationTiming.Add(_publicationMs);
    }

    private static ulong ResidentGpuBytes(PlanetaryProductionBillboardPreparedGeneration? generation)
    {
        if (generation is null) return 0;
        var vertices = (ulong)generation.Lattice.Length;
        var indices = (ulong)generation.Indices.Length;
        var triangles = indices / 3ul;
        return checked(vertices * 16ul + indices * 4ul + vertices * 64ul +
            triangles * 4ul + indices * 4ul + 20ul + 32ul);
    }

    private sealed class TimingWindow
    {
        private const int Capacity = 2048;
        private readonly double[] _values = new double[Capacity];
        private int _count, _next;

        public void Add(double value)
        {
            if (!double.IsFinite(value) || value < 0d) return;
            _values[_next] = value;
            _next = (_next + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        public PlanetaryProductionBillboardTiming Snapshot()
        {
            if (_count == 0) return default;
            var values = new double[_count];
            Array.Copy(_values, values, _count);
            Array.Sort(values);
            double Percentile(double percentile) =>
                values[Math.Min(values.Length - 1,
                    Math.Max(0, (int)Math.Ceiling(values.Length * percentile) - 1))];
            return new(values.Average(), Percentile(.50d), Percentile(.95d),
                Percentile(.99d), values[^1], values.Length);
        }
    }
}
