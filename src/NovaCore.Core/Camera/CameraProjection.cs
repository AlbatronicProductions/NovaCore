namespace NovaCore.Core.Camera;
public readonly record struct CameraProjection(double VerticalFieldOfViewRadians,double AspectRatio,double NearClip,double FarClip)
{
    public void Validate(){if(!double.IsFinite(VerticalFieldOfViewRadians)||VerticalFieldOfViewRadians<=0||VerticalFieldOfViewRadians>=Math.PI||!double.IsFinite(AspectRatio)||AspectRatio<=0||!double.IsFinite(NearClip)||!double.IsFinite(FarClip)||NearClip<=0||FarClip<=NearClip)throw new ArgumentOutOfRangeException(nameof(CameraProjection));}
    public CameraProjection WithAspect(double aspect){var result=this with{AspectRatio=aspect};result.Validate();return result;}
}
