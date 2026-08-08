using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
namespace NovaCore.Graphics;
public static class CameraRenderSnapshotBuilder
{
    public static bool TryBuild(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
        => TryBuild(state,resolver,root,false,false,out data,out rootPosition,out rootOrientation);

    /// <summary>Builds a finite Vulkan projection with near=1 and far=0 for reversed-Z depth testing.</summary>
    public static bool TryBuildReversedZ(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
        => TryBuild(state,resolver,root,false,true,out data,out rootPosition,out rootOrientation);

    /// <summary>Builds a standard Vulkan 0..1 depth projection whose far limit is asymptotic.</summary>
    public static bool TryBuildInfiniteFar(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
        => TryBuild(state,resolver,root,true,false,out data,out rootPosition,out rootOrientation);

    /// <summary>Builds an asymptotic infinite-far Vulkan projection with near=1 and infinity=0.</summary>
    public static bool TryBuildReversedInfiniteFar(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
        => TryBuild(state,resolver,root,true,true,out data,out rootPosition,out rootOrientation);

    private static bool TryBuild(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,bool infiniteFar,bool reversedZ,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
    {
        state.Validate();if(!resolver.TryResolvePosition(state.Position,out rootPosition)||!resolver.TryConvertOrientation(state.Position.Frame,root,state.Orientation,out rootOrientation)){data=default;rootPosition=default;rootOrientation=default;return false;}
        var q=rootOrientation.Conjugate().Normalized();var p=state.Projection;var f=1d/Math.Tan(p.VerticalFieldOfViewRadians*.5d);var a=f/p.AspectRatio;var za=reversedZ?(infiniteFar?0d:p.NearClip/(p.FarClip-p.NearClip)):(infiniteFar?-1d:p.FarClip/(p.NearClip-p.FarClip));var zb=reversedZ?(infiniteFar?p.NearClip:p.NearClip*p.FarClip/(p.FarClip-p.NearClip)):(infiniteFar?-p.NearClip:p.NearClip*p.FarClip/(p.NearClip-p.FarClip));
        // P * inverse(camera orientation), column-major; camera translation is deliberately absent.
        var r=Rotation(q); data=new GpuCameraData{Position=EncodedPosition.Encode(rootPosition.Value),ViewProjection=Multiply(new Float4x4{C0R0=(float)a,C1R1=(float)-f,C2R2=(float)za,C2R3=-1,C3R2=(float)zb},r)};return true;
    }
    private static Float4x4 Rotation(DoubleQuaternion q){var x=q.X;var y=q.Y;var z=q.Z;var w=q.W;return new(){C0R0=(float)(1-2*y*y-2*z*z),C0R1=(float)(2*x*y+2*z*w),C0R2=(float)(2*x*z-2*y*w),C1R0=(float)(2*x*y-2*z*w),C1R1=(float)(1-2*x*x-2*z*z),C1R2=(float)(2*y*z+2*x*w),C2R0=(float)(2*x*z+2*y*w),C2R1=(float)(2*y*z-2*x*w),C2R2=(float)(1-2*x*x-2*y*y),C3R3=1};}
    private static Float4x4 Multiply(Float4x4 a,Float4x4 b)=>new(){
        C0R0=a.C0R0*b.C0R0+a.C1R0*b.C0R1+a.C2R0*b.C0R2+a.C3R0*b.C0R3,C0R1=a.C0R1*b.C0R0+a.C1R1*b.C0R1+a.C2R1*b.C0R2+a.C3R1*b.C0R3,C0R2=a.C0R2*b.C0R0+a.C1R2*b.C0R1+a.C2R2*b.C0R2+a.C3R2*b.C0R3,C0R3=a.C0R3*b.C0R0+a.C1R3*b.C0R1+a.C2R3*b.C0R2+a.C3R3*b.C0R3,
        C1R0=a.C0R0*b.C1R0+a.C1R0*b.C1R1+a.C2R0*b.C1R2+a.C3R0*b.C1R3,C1R1=a.C0R1*b.C1R0+a.C1R1*b.C1R1+a.C2R1*b.C1R2+a.C3R1*b.C1R3,C1R2=a.C0R2*b.C1R0+a.C1R2*b.C1R1+a.C2R2*b.C1R2+a.C3R2*b.C1R3,C1R3=a.C0R3*b.C1R0+a.C1R3*b.C1R1+a.C2R3*b.C1R2+a.C3R3*b.C1R3,
        C2R0=a.C0R0*b.C2R0+a.C1R0*b.C2R1+a.C2R0*b.C2R2+a.C3R0*b.C2R3,C2R1=a.C0R1*b.C2R0+a.C1R1*b.C2R1+a.C2R1*b.C2R2+a.C3R1*b.C2R3,C2R2=a.C0R2*b.C2R0+a.C1R2*b.C2R1+a.C2R2*b.C2R2+a.C3R2*b.C2R3,C2R3=a.C0R3*b.C2R0+a.C1R3*b.C2R1+a.C2R3*b.C2R2+a.C3R3*b.C2R3,
        C3R0=a.C0R0*b.C3R0+a.C1R0*b.C3R1+a.C2R0*b.C3R2+a.C3R0*b.C3R3,C3R1=a.C0R1*b.C3R0+a.C1R1*b.C3R1+a.C2R1*b.C3R2+a.C3R1*b.C3R3,C3R2=a.C0R2*b.C3R0+a.C1R2*b.C3R1+a.C2R2*b.C3R2+a.C3R2*b.C3R3,C3R3=a.C0R3*b.C3R0+a.C1R3*b.C3R1+a.C2R3*b.C3R2+a.C3R3*b.C3R3};
}
