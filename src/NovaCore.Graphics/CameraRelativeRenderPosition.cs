using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// GPU-facing translation produced only after subtracting an FP64 root-space camera position
/// from an FP64 root-space object position. This value never owns simulation position authority.
/// </summary>
public readonly record struct CameraRelativeRenderPosition
{
    private CameraRelativeRenderPosition(in Double3 value) => Value = value;

    public Double3 Value { get; }
    public bool IsFinite => Value.IsFinite;

    public static bool TryCreate(
        in UniversePosition objectRoot,
        in UniversePosition cameraRoot,
        out CameraRelativeRenderPosition relative)
    {
        relative = default;
        if (objectRoot.Frame != cameraRoot.Frame || !objectRoot.Value.IsFinite || !cameraRoot.Value.IsFinite) return false;
        return TryCreate(objectRoot.Value, cameraRoot.Value, out relative);
    }

    public static bool TryCreate(
        in Double3 objectRoot,
        in Double3 cameraRoot,
        out CameraRelativeRenderPosition relative)
    {
        relative = default;
        if (!objectRoot.IsFinite || !cameraRoot.IsFinite) return false;
        var value = objectRoot - cameraRoot; // Mandatory FP64 subtraction boundary.
        if (!value.IsFinite) return false;
        relative = new(value);
        return true;
    }

    public static CameraRelativeRenderPosition Create(
        in UniversePosition objectRoot,
        in UniversePosition cameraRoot)
    {
        if (!TryCreate(objectRoot, cameraRoot, out var relative))
            throw new ArgumentException("Object and camera must be finite positions in the same root frame.");
        return relative;
    }

    public static CameraRelativeRenderPosition Create(in Double3 objectRoot, in Double3 cameraRoot)
    {
        if (!TryCreate(objectRoot, cameraRoot, out var relative))
            throw new ArgumentException("Object and camera root positions must be finite.");
        return relative;
    }

    /// <summary>Splits the already-relative value; absolute astronomical coordinates never reach this encoder.</summary>
    public EncodedPosition Encode() => EncodedPosition.Encode(Value);

    public bool TryNarrow(out Float3 value)
    {
        value = new((float)Value.X, (float)Value.Y, (float)Value.Z);
        return IsFinite && value.IsFinite;
    }
}
