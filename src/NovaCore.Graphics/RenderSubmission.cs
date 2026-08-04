using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public static class RenderSubmission
{
    public static RenderObject CreateObject(in UniversePosition position, in RenderOrigin origin, MeshHandle mesh)
    {
        _ = ReferenceFrame.Resolve(position, origin);
        return new RenderObject(EncodedPosition.Encode(position.Value), mesh);
    }

    public static EncodedPosition EncodeCamera(in RenderOrigin origin) => EncodedPosition.Encode(origin.CameraPosition.Value);
}
