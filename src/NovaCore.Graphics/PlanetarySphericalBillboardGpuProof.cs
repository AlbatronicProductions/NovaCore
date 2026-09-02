using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public enum PlanetarySphericalBillboardProofLevel : byte
{
    Orbital = 0,
    IntermediateApproach = 1,
    SurfacePupil = 2,
}

/// <summary>
/// Disk-authored P2S2 topology selected for the isolated P2S3 runtime. This
/// description contains no patch owner, patch LRU, stitch, or promotion state.
/// </summary>
public sealed record PlanetarySphericalBillboardGpuRuntimeDescription(
    PlanetarySphericalBillboardProofLevel Level,
    string ArtifactPath,
    int ArtifactBytes,
    PlanetarySphericalBillboardTopology Topology)
{
    public uint RuntimeTopologyGenerationCount => 0;
    public uint LegacyIdentityDependencyCount => 0;
    public ulong ImmutableGpuBytes => checked((ulong)Topology.Vertices.Count * 16ul +
        (ulong)Topology.Indices.Count * 4ul +
        (ulong)(Topology.NeighborOffsets.Count + Topology.Neighbors.Count) * 4ul);
}

public static class PlanetarySphericalBillboardGpuProofLibrary
{
    public static IReadOnlyList<PlanetarySphericalBillboardGpuRuntimeDescription> Load(string artifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);
        var manifestPath = Path.Combine(artifactDirectory, "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "NovaCoreSphericalBillboardTopology" ||
            root.GetProperty("formatVersion").GetUInt32() != PlanetarySphericalBillboardTopology.FormatVersion ||
            root.GetProperty("generatorVersion").GetUInt32() != PlanetarySphericalBillboardTopology.GeneratorVersion)
            throw new InvalidDataException("The spherical-billboard topology manifest is incompatible.");

        var result = new List<PlanetarySphericalBillboardGpuRuntimeDescription>();
        foreach (var entry in root.GetProperty("levels").EnumerateArray())
        {
            if (!Enum.TryParse<PlanetarySphericalBillboardProofLevel>(entry.GetProperty("level").GetString(), out var level))
                throw new InvalidDataException("The topology manifest contains an unknown level.");
            var fileName = entry.GetProperty("file").GetString() ?? throw new InvalidDataException("A topology file name is missing.");
            if (Path.GetFileName(fileName) != fileName) throw new InvalidDataException("Topology artifacts must be direct manifest siblings.");
            var path = Path.Combine(artifactDirectory, fileName); var bytes = File.ReadAllBytes(path);
            if (bytes.Length != entry.GetProperty("bytes").GetInt32()) throw new InvalidDataException($"Topology byte count mismatch for {level}.");
            var topology = PlanetarySphericalBillboardTopology.Load(bytes);
            var hashText = entry.GetProperty("topologyHash").GetString();
            if (hashText is null || !ulong.TryParse(hashText.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var manifestHash) ||
                topology.TopologyHash != manifestHash || topology.Level != (byte)level)
                throw new InvalidDataException($"Topology identity mismatch for {level}.");
            result.Add(new(level, path, bytes.Length, topology));
        }
        if (result.Count != 3 || result.Select(value => value.Level).Distinct().Count() != 3)
            throw new InvalidDataException("The P2S3 proof requires exactly three distinct topology levels.");
        return result.OrderBy(value => value.Level).ToArray();
    }
}

public readonly record struct PlanetarySphericalBillboardGpuProofResult(
    PlanetarySphericalBillboardProofLevel Level,
    NativeSphericalBillboardProofMetrics Upload,
    NativeSphericalBillboardProofMetrics Frame,
    NativeSphericalBillboardProofMetrics CameraUpdate);

public readonly record struct PlanetarySphericalBillboardGpuScalingResult(
    uint Vertices,
    uint Triangles,
    NativeSphericalBillboardProofMetrics Metrics);

public sealed record PlanetarySphericalBillboardGpuProofReport(
    IReadOnlyList<PlanetarySphericalBillboardGpuProofResult> Levels,
    IReadOnlyList<PlanetarySphericalBillboardGpuScalingResult> Scaling,
    NativeSphericalBillboardProofMetrics FinalMetrics,
    double TopologyLoadMilliseconds);

/// <summary>Explicit, off-screen, P2S3-only Vulkan lifecycle and indirect-draw proof.</summary>
public sealed unsafe class PlanetarySphericalBillboardGpuProofSession : IDisposable
{
    public const uint FrameResourceCount = 3;
    public const uint MaximumVertexWorkItems = 500_000;
    public const uint MaximumTriangleWorkItems = 1_000_000;
    public const uint RenderExtent = 128;
    private bool _disposed;

    public PlanetarySphericalBillboardGpuProofSession(string shaderDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderDirectory);
        var names = new[]
        {
            "spherical_billboard_proof_reset.comp.spv", "spherical_billboard_proof_prepare.comp.spv",
            "spherical_billboard_proof_normals.comp.spv", "spherical_billboard_proof_cull.comp.spv",
            "spherical_billboard_proof_compact.comp.spv", "spherical_billboard_proof.vert.spv",
            "spherical_billboard_proof.frag.spv",
        };
        var pointers = names.Select(name => Marshal.StringToCoTaskMemUTF8(Path.Combine(shaderDirectory, name))).ToArray();
        try
        {
            var assets = new NativeSphericalBillboardProofAssets
            {
                Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofAssets>(), Version = 1,
                ResetShaderPathUtf8 = (byte*)pointers[0], PrepareShaderPathUtf8 = (byte*)pointers[1],
                NormalShaderPathUtf8 = (byte*)pointers[2], CullShaderPathUtf8 = (byte*)pointers[3],
                CompactShaderPathUtf8 = (byte*)pointers[4], VertexShaderPathUtf8 = (byte*)pointers[5],
                FragmentShaderPathUtf8 = (byte*)pointers[6], MaximumVertexWorkItems = MaximumVertexWorkItems,
                MaximumTriangleWorkItems = MaximumTriangleWorkItems, FrameResourceCount = FrameResourceCount,
                RenderExtent = RenderExtent,
            };
            var metrics = EmptyMetrics();
            if (NativeRuntime.InitializeSphericalBillboardGpuProof(&assets, &metrics) != NativeResult.Success)
                throw new InvalidOperationException("The isolated P2S3 Vulkan runtime could not initialize.");
            InitializationMetrics = metrics;
        }
        finally { foreach (var pointer in pointers) Marshal.FreeCoTaskMem(pointer); }
    }

    public NativeSphericalBillboardProofMetrics InitializationMetrics { get; }

    public NativeSphericalBillboardProofMetrics Upload(PlanetarySphericalBillboardGpuRuntimeDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var topology = description.Topology;
        var vertices = topology.Vertices.Select(value => new NativeSphericalBillboardProofVertex
        { X = value.Position.X, Y = value.Position.Y, Z = value.Position.Z, W = value.AverageAngularSpacingRadians }).ToArray();
        var indices = topology.Indices.ToArray();
        var offsets = topology.NeighborOffsets.Select(value => checked((uint)value)).ToArray();
        var neighbors = topology.Neighbors.Select(value => checked((uint)value)).ToArray();
        fixed (NativeSphericalBillboardProofVertex* vertexPointer = vertices)
        fixed (uint* indexPointer = indices)
        fixed (uint* offsetPointer = offsets)
        fixed (uint* neighborPointer = neighbors)
        {
            var native = new NativeSphericalBillboardProofTopology
            {
                Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofTopology>(), Version = 1,
                FormatVersion = PlanetarySphericalBillboardTopology.FormatVersion,
                GeneratorVersion = PlanetarySphericalBillboardTopology.GeneratorVersion,
                Level = (uint)description.Level, VertexCount = (uint)vertices.Length,
                IndexCount = (uint)indices.Length, NeighborOffsetCount = (uint)offsets.Length,
                NeighborCount = (uint)neighbors.Length, TopologyHash = topology.TopologyHash,
                Vertices = vertexPointer, Indices = indexPointer, NeighborOffsets = offsetPointer, Neighbors = neighborPointer,
            };
            var metrics = EmptyMetrics();
            if (NativeRuntime.UploadSphericalBillboardGpuProofTopology(&native, &metrics) != NativeResult.Success)
                throw new InvalidOperationException($"P2S3 topology upload failed for {description.Level}.");
            return metrics;
        }
    }

    public NativeSphericalBillboardProofMetrics RunFrame(PlanetarySphericalBillboardGpuRuntimeDescription description,
        uint frameIndex, uint? workVertices = null, uint? workTriangles = null, bool render = true, double cameraDistanceRadii = 2.5)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        const double radius = 6_371_008.8;
        var frame = new NativeSphericalBillboardProofFrame
        {
            Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofFrame>(), Version = 1, FrameIndex = frameIndex,
            RenderEnabled = render ? 1u : 0u,
            WorkVertexCount = workVertices ?? (uint)description.Topology.Vertices.Count,
            WorkTriangleCount = workTriangles ?? (uint)(description.Topology.Indices.Count / 3),
            ExpectedTopologyHash = description.Topology.TopologyHash, BodyRadiusMetres = radius,
            CameraDistanceMetres = radius * cameraDistanceRadii, VerticalTanHalfFov = (float)Math.Tan(Math.PI / 6d),
            AspectRatio = 1f,
        };
        var metrics = EmptyMetrics();
        if (NativeRuntime.RunSphericalBillboardGpuProofFrame(&frame, &metrics) != NativeResult.Success)
            throw new InvalidOperationException($"P2S3 frame failed for {description.Level}: validation={metrics.ValidationErrors}; invalid={metrics.InvalidCommands}; overflow={metrics.OverflowCount}; visible={metrics.VisibleTriangles}.");
        return metrics;
    }

    public NativeResult TryRunWithoutTopology()
    {
        var frame = new NativeSphericalBillboardProofFrame
        {
            Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofFrame>(), Version = 1,
            WorkVertexCount = 1, WorkTriangleCount = 1, BodyRadiusMetres = 1, CameraDistanceMetres = 2.5,
            VerticalTanHalfFov = 0.5f, AspectRatio = 1,
        };
        var metrics = EmptyMetrics(); return NativeRuntime.RunSphericalBillboardGpuProofFrame(&frame, &metrics);
    }

    public NativeResult TryRunWithStaleTopologyIdentity(PlanetarySphericalBillboardGpuRuntimeDescription description)
    {
        var frame = new NativeSphericalBillboardProofFrame
        {
            Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofFrame>(), Version = 1,
            WorkVertexCount = (uint)description.Topology.Vertices.Count,
            WorkTriangleCount = (uint)(description.Topology.Indices.Count / 3),
            ExpectedTopologyHash = description.Topology.TopologyHash ^ 1ul,
            BodyRadiusMetres = 6_371_008.8, CameraDistanceMetres = 15_927_522,
            VerticalTanHalfFov = (float)Math.Tan(Math.PI / 6d), AspectRatio = 1,
        };
        var metrics = EmptyMetrics(); return NativeRuntime.RunSphericalBillboardGpuProofFrame(&frame, &metrics);
    }

    public void Dispose()
    {
        if (_disposed) return;
        var result = NativeRuntime.ShutdownSphericalBillboardGpuProof(); _disposed = true;
        if (result != NativeResult.Success) throw new InvalidOperationException("P2S3 Vulkan shutdown failed.");
    }

    private static NativeSphericalBillboardProofMetrics EmptyMetrics() => new()
    { Size = (uint)Marshal.SizeOf<NativeSphericalBillboardProofMetrics>(), Version = 1 };
}

public static class PlanetarySphericalBillboardGpuProof
{
    public static PlanetarySphericalBillboardGpuProofReport Run(string repositoryRoot, bool includeScaling)
    {
        var loadTimer = Stopwatch.StartNew();
        var descriptions = PlanetarySphericalBillboardGpuProofLibrary.Load(Path.Combine(repositoryRoot, "assets", "planetary-topology"));
        loadTimer.Stop();
        using var session = new PlanetarySphericalBillboardGpuProofSession(Path.Combine(repositoryRoot, "build", "native-ninja", "shaders"));
        var results = new List<PlanetarySphericalBillboardGpuProofResult>(); uint frame = 0;
        foreach (var description in descriptions)
        {
            var upload = session.Upload(description);
            var first = session.RunFrame(description, frame++);
            var cameraUpdate = session.RunFrame(description, frame++, cameraDistanceRadii: 2.6);
            var repeatedUpload = session.Upload(description);
            if (repeatedUpload.TopologyUploadCount != upload.TopologyUploadCount)
                throw new InvalidOperationException("Camera/frame reuse caused an immutable topology re-upload.");
            results.Add(new(description.Level, upload, first, cameraUpdate));
        }
        var scaling = new List<PlanetarySphericalBillboardGpuScalingResult>();
        var surface = descriptions[^1]; session.Upload(surface);
        if (includeScaling)
        {
            foreach (var item in new[] { (100_000u, 200_000u), (250_000u, 500_000u), (500_000u, 1_000_000u) })
                scaling.Add(new(item.Item1, item.Item2, session.RunFrame(surface, frame++, item.Item1, item.Item2, render: false)));
        }
        var final = scaling.Count == 0 ? results[^1].CameraUpdate : scaling[^1].Metrics;
        return new(results, scaling, final, loadTimer.Elapsed.TotalMilliseconds);
    }

    public static string FindRepositoryRoot(string start)
    {
        var current = new DirectoryInfo(Path.GetFullPath(start));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NovaCore.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("The NovaCore repository root could not be located.");
    }
}
