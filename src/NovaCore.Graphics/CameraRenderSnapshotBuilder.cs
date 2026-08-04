using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
namespace NovaCore.Graphics;
public static class CameraRenderSnapshotBuilder
{
    public static bool TryBuild(CameraState state,ReferenceFrameResolver resolver,ReferenceFrameId root,out GpuCameraData data,out UniversePosition rootPosition,out DoubleQuaternion rootOrientation)
    {
        state.Validate();if(!resolver.TryResolvePosition(state.Position,out rootPosition)||!resolver.TryConvertOrientation(state.Position.Frame,root,state.Orientation,out rootOrientation)){data=default;rootPosition=default;rootOrientation=default;return false;}
        var q=rootOrientation.Conjugate().Normalized();var p=state.Projection;var f=1d/Math.Tan(p.VerticalFieldOfViewRadians*.5d);var a=f/p.AspectRatio;var za=p.FarClip/(p.NearClip-p.FarClip);var zb=p.NearClip*p.FarClip/(p.NearClip-p.FarClip);
        // P * inverse(camera orientation), column-major; camera translation is deliberately absent.
        var r=Rotation(q); data=new GpuCameraData{Position=EncodedPosition.Encode(rootPosition.Value),ViewProjection=Multiply(new Float4x4{C0R0=(float)a,C1R1=(float)-f,C2R2=(float)za,C2R3=-1,C3R2=(float)zb},r)};return true;
    }
    private static Float4x4 Rotation(DoubleQuaternion q){var x=q.X;var y=q.Y;var z=q.Z;var w=q.W;return new(){C0R0=(float)(1-2*y*y-2*z*z),C0R1=(float)(2*x*y+2*z*w),C0R2=(float)(2*x*z-2*y*w),C1R0=(float)(2*x*y-2*z*w),C1R1=(float)(1-2*x*x-2*z*z),C1R2=(float)(2*y*z+2*x*w),C2R0=(float)(2*x*z+2*y*w),C2R1=(float)(2*y*z-2*x*w),C2R2=(float)(1-2*x*x-2*y*y),C3R3=1};}
    private static Float4x4 Multiply(Float4x4 a,Float4x4 b){float[] x={a.C0R0,a.C0R1,a.C0R2,a.C0R3,a.C1R0,a.C1R1,a.C1R2,a.C1R3,a.C2R0,a.C2R1,a.C2R2,a.C2R3,a.C3R0,a.C3R1,a.C3R2,a.C3R3};float[] y={b.C0R0,b.C0R1,b.C0R2,b.C0R3,b.C1R0,b.C1R1,b.C1R2,b.C1R3,b.C2R0,b.C2R1,b.C2R2,b.C2R3,b.C3R0,b.C3R1,b.C3R2,b.C3R3};float[] o=new float[16];for(var c=0;c<4;c++)for(var r=0;r<4;r++)for(var k=0;k<4;k++)o[c*4+r]+=x[k*4+r]*y[c*4+k];return new(){C0R0=o[0],C0R1=o[1],C0R2=o[2],C0R3=o[3],C1R0=o[4],C1R1=o[5],C1R2=o[6],C1R3=o[7],C2R0=o[8],C2R1=o[9],C2R2=o[10],C2R3=o[11],C3R0=o[12],C3R1=o[13],C3R2=o[14],C3R3=o[15]};}
}
