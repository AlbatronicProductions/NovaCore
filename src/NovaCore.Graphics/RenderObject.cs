namespace NovaCore.Graphics;

public readonly record struct MeshHandle(uint Value)
{
    public static MeshHandle Triangle { get; } = new(1);
}

/// <summary>Renderer submission data; not available to gameplay or simulation assemblies.</summary>
public readonly record struct RenderObject(EncodedPosition Position, MeshHandle Mesh);
