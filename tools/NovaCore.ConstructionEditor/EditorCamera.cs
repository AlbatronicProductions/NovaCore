using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.ConstructionEditor;

// Editor presentation only. X is the vehicle's longitudinal axis, drawn up.
internal sealed class EditorCamera
{
    internal Double3 Target=new(-1,0,0);
    internal double Distance=10,Yaw=.8,Pitch=-.2;
    internal double Aspect=1;
    internal Double3 Eye=>Target+new Double3(Math.Sin(Pitch),Math.Cos(Pitch)*Math.Cos(Yaw),Math.Cos(Pitch)*Math.Sin(Yaw))*Distance;
    internal Double3 Forward=>(Target-Eye).Normalized();
    internal Double3 Right=>Double3.Cross(Forward,Double3.UnitX).Normalized();
    internal Double3 Up=>Double3.Cross(Right,Forward).Normalized();
    internal const double TanHalfFov=.41421356237309503;
    internal Double3 Ray(double x,double y,double width,double height)=>(Forward+Right*((2*x/width-1)*Aspect*TanHalfFov)+Up*((1-2*y/height)*TanHalfFov)).Normalized();
    internal (double X,double Y,double Depth) Project(Double3 point,double width,double height)
    {
        var d=point-Eye;var depth=Double3.Dot(d,Forward);
        return((Double3.Dot(d,Right)/(depth*Aspect*TanHalfFov)+1)*width*.5,(1-Double3.Dot(d,Up)/(depth*TanHalfFov))*height*.5,depth);
    }
    internal NativeCameraData Native()
    {
        var right=Right/(Aspect*TanHalfFov);var up=Up*(-1/TanHalfFov);var f=Forward;
        // Camera-relative vertices, Vulkan Y-down and infinite reversed-Z.
        return new(){ViewProjection=new(){C0R0=(float)right.X,C1R0=(float)right.Y,C2R0=(float)right.Z,
            C0R1=(float)up.X,C1R1=(float)up.Y,C2R1=(float)up.Z,
            C0R3=(float)f.X,C1R3=(float)f.Y,C2R3=(float)f.Z,C3R2=.02f}};
    }
}
