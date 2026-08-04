using NovaCore.Core;

namespace NovaCore.Graphics;

public static class RenderSubmission
{
    public static RenderObject CreateObject(in UniversePosition position, in DoubleQuaternion rotation, in Double3 scale, MeshHandle mesh)
    {
        if (!mesh.IsValid) throw new ArgumentOutOfRangeException(nameof(mesh), "Mesh handle zero is invalid.");
        var transform = RenderTransform.FromAuthoritative(rotation, scale);
        if (!transform.IsFinite) throw new ArgumentException("Render transform must be finite.");
        return new RenderObject(EncodedPosition.Encode(position.Value), transform, mesh);
    }

}
