using NovaCore.Core;

namespace NovaCore.Graphics;

public static class RenderSubmission
{
    public static RenderObject CreateObject(in CameraRelativeRenderPosition position, in DoubleQuaternion rotation, in Double3 scale, MeshHandle mesh)
    {
        if (!mesh.IsValid) throw new ArgumentOutOfRangeException(nameof(mesh), "Mesh handle zero is invalid.");
        var transform = RenderTransform.FromAuthoritative(rotation, scale);
        if (!transform.IsFinite) throw new ArgumentException("Render transform must be finite.");
        if (!position.IsFinite) throw new ArgumentException("Camera-relative position must be finite.");
        return new RenderObject(position.Encode(), transform, mesh);
    }

}
