namespace NovaCore.Graphics;

/// <summary>Fixed-width GPU mesh identifier. Zero is never a valid mesh.</summary>
public readonly record struct MeshHandle(uint Value)
{
    public static MeshHandle Invalid { get; } = new(0);
    public static MeshHandle Triangle { get; } = new(1);
    /// <summary>Reusable unit sphere presentation mesh. Body size is supplied only by a render transform.</summary>
    public static MeshHandle Sphere { get; } = new(2);
    public bool IsValid => Value != 0;
}

/// <summary>GPU transport data only; simulation state must remain in managed doubles.</summary>
public readonly record struct RenderObject(EncodedPosition Position, RenderTransform Transform, MeshHandle Mesh);

/// <summary>Derived, stable-order transport batch. Sample and simulation code never construct this.</summary>
public readonly record struct RenderBatch(MeshHandle Mesh, uint FirstObject, uint ObjectCount);
