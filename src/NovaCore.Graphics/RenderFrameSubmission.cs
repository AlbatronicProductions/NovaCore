using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Reusable GPU snapshot storage. This type contains no authoritative world state.</summary>
public sealed class RenderFrameSubmission
{
    private readonly RenderObject[] _objects;
    private readonly RenderBatch[] _batches;

    public RenderFrameSubmission(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _objects = new RenderObject[capacity];
        _batches = new RenderBatch[capacity];
    }

    public EncodedPosition Camera { get; private set; }
    public ReadOnlySpan<RenderObject> Objects => _objects.AsSpan(0, ObjectCount);
    public ReadOnlySpan<RenderBatch> Batches => _batches.AsSpan(0, BatchCount);
    public int Capacity => _objects.Length;
    public int ObjectCount { get; private set; }
    public int BatchCount { get; private set; }

    public void Begin(in RenderOrigin camera)
    {
        Camera = RenderSubmission.EncodeCamera(camera);
        ObjectCount = 0;
        BatchCount = 0;
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
