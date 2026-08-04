using NovaCore.Core.ReferenceFrames;
namespace NovaCore.Core.Camera;
/// <summary>Authoritative camera pose. Orientation maps camera-local vectors into Position.Frame.</summary>
public sealed class CameraState(FramePosition position,DoubleQuaternion orientation,CameraProjection projection,CameraMode mode)
{
    public FramePosition Position=position; public DoubleQuaternion Orientation=orientation.Normalized(); public CameraProjection Projection=projection; public CameraMode Mode=mode;
    public void Validate(){if(!Position.Value.IsFinite||!Orientation.IsFinite)throw new ArgumentException("Camera state must be finite.");Projection.Validate();if(Math.Abs(Orientation.LengthSquared-1d)>1e-10d)Orientation=Orientation.Normalized();}
}
