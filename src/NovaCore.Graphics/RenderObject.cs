namespace NovaCore.Graphics;

/// <summary>Fixed-width GPU mesh identifier. Zero is never a valid mesh.</summary>
public readonly record struct MeshHandle(uint Value)
{
    public static MeshHandle Invalid { get; } = new(0);
    public static MeshHandle Triangle { get; } = new(1);
    /// <summary>Unit cubes for the opt-in contact witness; no spacecraft/facility physical identity.</summary>
    public static MeshHandle ContactQualificationBody { get; } = new(5);
    public static MeshHandle ContactQualificationSupport { get; } = new(6);
    /// <summary>Reusable unit sphere presentation mesh. Body size is supplied only by a render transform.</summary>
    public static MeshHandle Sphere { get; } = new(2);
    /// <summary>Persistent original NovaCore Florida launchpad proof geometry.</summary>
    public static MeshHandle FloridaLaunchPad { get; } = new(3);
    /// <summary>Unit Florida footing: X/Y=-0.5..0.5, Z=-1..0; scaled from the canonical footprint survey.</summary>
    public static MeshHandle FloridaLaunchFoundation { get; } = new(4);
    /// <summary>Centred unit box; the explicit Florida slab support owner supplies its dimensions and pose.</summary>
    public static MeshHandle FloridaSupportSlab { get; } = new(7);
    public bool IsValid => Value != 0;
}

/// <summary>GPU transport data only; simulation state must remain in managed doubles.</summary>
public readonly record struct RenderObject(EncodedPosition Position, RenderTransform Transform, MeshHandle Mesh);

/// <summary>Derived, stable-order transport batch. Sample and simulation code never construct this.</summary>
public readonly record struct RenderBatch(MeshHandle Mesh, uint FirstObject, uint ObjectCount);
