namespace NovaCore.Graphics;

/// <summary>Stable, caller-owned render transport identity. Zero is invalid.</summary>
public readonly record struct RenderObjectId(uint Value)
{
    public static RenderObjectId Invalid => new(0);
    public bool IsValid => Value != 0;
}
