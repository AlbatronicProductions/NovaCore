using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Reusable GPU snapshot storage. This type contains no authoritative world state.</summary>
public sealed class RenderFrameSubmission
{
    private readonly RenderObject[] _objects;
    private readonly RenderBatch[] _batches;
    private readonly OrbitLineVertex[] _orbitVertices;
    private readonly OrbitLineVertex[] _previousOrbitVertices;
    private readonly OrbitLineVertex[] _bodyForwardVertices = new OrbitLineVertex[2];
    private readonly OrbitLineVertex[] _targetDirectionVertices = new OrbitLineVertex[2];
    private UniversePosition _cameraRootPosition;

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
    public ReadOnlySpan<OrbitLineVertex> BodyForwardVertices => _bodyForwardVertices.AsSpan(0, BodyForwardVertexCount);
    public int BodyForwardVertexCount { get; private set; }
    public ReadOnlySpan<OrbitLineVertex> TargetDirectionVertices => _targetDirectionVertices.AsSpan(0, TargetDirectionVertexCount);
    public int TargetDirectionVertexCount { get; private set; }

    public void Begin(in GpuCameraData camera, in UniversePosition cameraRootPosition)
    {
        if (!cameraRootPosition.Value.IsFinite) throw new ArgumentException("Camera root position must be finite.", nameof(cameraRootPosition));
        Camera = camera;
        _cameraRootPosition = cameraRootPosition;
        ObjectCount = 0;
        BatchCount = 0;
        OrbitVertexCount = 0;
        PreviousOrbitVertexCount = 0;
        BodyForwardVertexCount = 0;
        TargetDirectionVertexCount = 0;
    }

    internal bool TrySetOrbitVertices(ResolvedOrbitCurve curve, in UniversePosition camera, bool previous = false)
    {
        var destination = previous ? _previousOrbitVertices : _orbitVertices;
        if (curve.Count > destination.Length || curve.RootFrame != camera.Frame) return false;
        for (var index = 0; index < curve.Count; index++)
        {
            if (!CameraRelativeRenderPosition.TryCreate(curve.Positions[index], camera, out var relative)) return false;
            var encoded=relative.Encode();
            destination[index] = new OrbitLineVertex { X=encoded.HighX,Y=encoded.HighY,Z=encoded.HighZ,LowX=encoded.LowX,LowY=encoded.LowY,LowZ=encoded.LowZ };
        }
        if (previous) PreviousOrbitVertexCount = curve.Count; else OrbitVertexCount = curve.Count;
        return true;
    }

    internal bool TrySetDirectionIndicator(in ResolvedDirectionIndicator indicator, in UniversePosition camera, bool target)
    {
        if (!indicator.IsValid(camera.Frame)) return false;
        if (!CameraRelativeRenderPosition.TryCreate(indicator.Start, camera, out var start) ||
            !CameraRelativeRenderPosition.TryCreate(indicator.End, camera, out var end)) return false;
        var encodedStart=start.Encode();var encodedEnd=end.Encode();
        var destination = target ? _targetDirectionVertices : _bodyForwardVertices;
        destination[0] = new OrbitLineVertex { X=encodedStart.HighX,Y=encodedStart.HighY,Z=encodedStart.HighZ,LowX=encodedStart.LowX,LowY=encodedStart.LowY,LowZ=encodedStart.LowZ };
        destination[1] = new OrbitLineVertex { X=encodedEnd.HighX,Y=encodedEnd.HighY,Z=encodedEnd.HighZ,LowX=encodedEnd.LowX,LowY=encodedEnd.LowY,LowZ=encodedEnd.LowZ };
        if (target) TargetDirectionVertexCount = 2; else BodyForwardVertexCount = 2;
        return true;
    }


    public void Add(in UniversePosition position, in DoubleQuaternion rotation, in Double3 scale, MeshHandle mesh)
    {
        if (ObjectCount == _objects.Length) throw new InvalidOperationException("Render submission capacity exceeded.");
        var relative = CameraRelativeRenderPosition.Create(position, _cameraRootPosition);
        _objects[ObjectCount++] = RenderSubmission.CreateObject(relative, rotation, scale, mesh);
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
