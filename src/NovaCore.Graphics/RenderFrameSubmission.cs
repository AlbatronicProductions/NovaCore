using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Reusable GPU snapshot storage. This type contains no authoritative world state.</summary>
public sealed class RenderFrameSubmission
{
    private readonly RenderObject[] _objects;
    private readonly RenderBatch[] _batches;
    private readonly OrbitLineVertex[] _orbitVertices;
    private readonly OrbitLineVertex[] _previousOrbitVertices;

    public RenderFrameSubmission(int capacity, int orbitVertexCapacity = 0)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _objects = new RenderObject[capacity];
        _batches = new RenderBatch[capacity];
        _orbitVertices = new OrbitLineVertex[orbitVertexCapacity];
        _previousOrbitVertices = new OrbitLineVertex[orbitVertexCapacity];
    }

    public GpuCameraData Camera { get; private set; }
    public ReadOnlySpan<RenderObject> Objects => _objects.AsSpan(0, ObjectCount);
    public ReadOnlySpan<RenderBatch> Batches => _batches.AsSpan(0, BatchCount);
    public int Capacity => _objects.Length;
    public int ObjectCount { get; private set; }
    public int BatchCount { get; private set; }
    public ReadOnlySpan<OrbitLineVertex> OrbitVertices => _orbitVertices.AsSpan(0, OrbitVertexCount);
    public int OrbitVertexCount { get; private set; }
    public ReadOnlySpan<OrbitLineVertex> PreviousOrbitVertices => _previousOrbitVertices.AsSpan(0, PreviousOrbitVertexCount);
    public int PreviousOrbitVertexCount { get; private set; }

    public void Begin(in GpuCameraData camera)
    {
        Camera = camera;
        ObjectCount = 0;
        BatchCount = 0;
        OrbitVertexCount = 0;
        PreviousOrbitVertexCount = 0;
    }

    internal bool TrySetOrbitVertices(ResolvedOrbitCurve curve, in UniversePosition camera, bool previous = false)
    {
        var destination = previous ? _previousOrbitVertices : _orbitVertices;
        if (curve.Count > destination.Length || curve.RootFrame != camera.Frame) return false;
        for (var index = 0; index < curve.Count; index++)
        {
            var relative = curve.Positions[index].Value - camera.Value;
            if (!relative.IsFinite || !float.IsFinite((float)relative.X) || !float.IsFinite((float)relative.Y) || !float.IsFinite((float)relative.Z)) return false;
            destination[index] = new OrbitLineVertex { X = (float)relative.X, Y = (float)relative.Y, Z = (float)relative.Z };
        }
        if (previous) PreviousOrbitVertexCount = curve.Count; else OrbitVertexCount = curve.Count;
        return true;
    }


    public void Add(in UniversePosition position, in DoubleQuaternion rotation, in Double3 scale, MeshHandle mesh)
    {
        if (ObjectCount == _objects.Length) throw new InvalidOperationException("Render submission capacity exceeded.");
        _objects[ObjectCount++] = RenderSubmission.CreateObject(position, rotation, scale, mesh);
    }

    /// <summary>Creates stable contiguous mesh batches. Objects retain their caller-provided order.</summary>
    public void Complete()
    {
        BatchCount = 0;
        var start = 0;
        while (start < ObjectCount)
        {
            var mesh = _objects[start].Mesh;
            var end = start + 1;
            while (end < ObjectCount && _objects[end].Mesh == mesh) end++;
            _batches[BatchCount++] = new RenderBatch(mesh, (uint)start, (uint)(end - start));
            start = end;
        }
    }
}
