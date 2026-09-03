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
    ulong PublicationGeneration,
    PlanetaryProductionBillboardReuse Reuse,
    double PreparationMilliseconds,
    bool TopologyUploadRequired);

public readonly record struct PlanetaryProductionBillboardMovingTelemetry(
    ulong CompletedFrames,
    int CurrentLevel,
    int IncomingLevel,
    uint PupilGeneration,
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
    private Task<PlanetaryProductionBillboardPreparedGeneration>? _preparing;
    private PlanetaryProductionBillboardPreparedGeneration? _ready;
    private PlanetaryProductionBillboardPreparedGeneration? _submitted;
    private PlanetaryProductionBillboardPreparedGeneration? _current;
    private ulong _nextGeneration;
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
                repositoryRoot, topology, pupil, cache, maximumParitySamples: 64)) { }

    public PlanetaryProductionSphericalBillboardMovingRuntime(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,
        Func<PlanetaryProductionSphericalBillboardTopology,
            PlanetaryProductionBillboardPupil, PlanetaryProductionBillboardPhysicalCache,
            PlanetaryProductionBillboardPhysicalPreparation> prepare)
    {
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(prepare);
        _levels = levels;
        _selector = new(levels);
        _prepare = prepare;
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
        var basis = _current?.Pupil ?? default;
        var desiredPupil = PlanetaryProductionBillboardPupil.Resolve(basis,
            view.CameraBodyDirection, _levels[selection.Level]);
        _pupilMs = Stopwatch.GetElapsedTime(pupilStart).TotalMilliseconds;
        _pupilTiming.Add(_pupilMs);

        if (!ReplacementInFlight && (_current is null ||
            _current.Topology.Level != selection.Level ||
            _current.Pupil.Generation != desiredPupil.Generation))
            Schedule(_levels[selection.Level], desiredPupil);

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
            desiredPupil.Generation, angular, active?.Reuse.ActiveSamples ?? 0,
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
        var previous = _current;
        var topologyUpload = previous is null || previous.Topology.TopologyHash != topology.TopologyHash;
        var scheduled = Stopwatch.GetTimestamp();
        _preparing = Task.Run(() =>
        {
            var begin = Stopwatch.GetTimestamp();
            if (previous is not null) _cache.RetainOnly(previous.Physical.Identities);
            var physical = _prepare(topology, pupil, _cache);
            var lattice = topology.Vertices.Select(vertex => new NativeProductionBillboardLatticeVertex
            {
                CubeX = vertex.CubeX, CubeY = vertex.CubeY, CubeZ = vertex.CubeZ,
                Metadata = (uint)vertex.DensityRegion | ((uint)vertex.RefinementDepth << 8)
            }).ToArray();
            var reuse = new PlanetaryProductionBillboardReuse(topology.Vertices.Count,
                physical.ReusedSamples, physical.PreparedSamples);
            return new PlanetaryProductionBillboardPreparedGeneration(topology, pupil, physical,
                lattice, topology.Indices.ToArray(), generation, reuse,
                Stopwatch.GetElapsedTime(begin).TotalMilliseconds, topologyUpload);
        });
        _schedulingMs = Stopwatch.GetElapsedTime(scheduled).TotalMilliseconds;
        _schedulingTiming.Add(_schedulingMs);
    }

    private void CompleteBackgroundPreparation()
    {
        if (_preparing is null || !_preparing.IsCompleted) return;
        _ready = _preparing.GetAwaiter().GetResult();
        _preparing = null;
        _preparationMs = _ready.PreparationMilliseconds;
        _preparationTiming.Add(_preparationMs);
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
