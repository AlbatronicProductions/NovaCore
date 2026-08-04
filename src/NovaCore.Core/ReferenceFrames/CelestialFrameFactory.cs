namespace NovaCore.Core.ReferenceFrames;

public static class CelestialFrameFactory
{
    public static EvaluatedReferenceFrame RootEcl() => new(FrameTransform.Identity,Double3.Zero,Double3.Zero,true);
    /// <summary>Origin and velocity are explicitly supplied by future simulation; no propagation occurs here.</summary>
    public static EvaluatedReferenceFrame Orb(in Double3 originInEcl, in Double3 velocityInEcl, in OrbitalFrameGeometry geometry) => new(new FrameTransform(originInEcl,geometry.ToEclRotation()),velocityInEcl,Double3.Zero,true);
    public static EvaluatedReferenceFrame Cce(in Double3 originInEcl, in Double3 velocityInEcl) => new(new FrameTransform(originInEcl,DoubleQuaternion.Identity),velocityInEcl,Double3.Zero,true);
    public static EvaluatedReferenceFrame Cci(in Double3 originInCce, in Double3 velocityInCce, in Double3 northPoleInCce, in Double3 inertialReferenceInCce)
    {
        var z=northPoleInCce.Normalized(); var projected=inertialReferenceInCce-z*Double3.Dot(inertialReferenceInCce,z);
        if (projected.LengthSquared <= 1e-24d) throw new ArgumentException("Inertial reference direction is parallel to the north pole.");
        var x=projected.Normalized(); var y=Double3.Cross(z,x).Normalized();
        return new(new FrameTransform(originInCce,QuaternionFromBasis(x,y,z)),velocityInCce,Double3.Zero,true);
    }
    public static EvaluatedReferenceFrame Ccf(double rotationAngleRadians, double angularRateRadiansPerSecond) => new(new FrameTransform(Double3.Zero,DoubleQuaternion.FromAxisAngle(Double3.UnitZ,rotationAngleRadians)),Double3.Zero,new Double3(0d,0d,angularRateRadiansPerSecond),angularRateRadiansPerSecond==0d);
    private static DoubleQuaternion QuaternionFromBasis(Double3 x,Double3 y,Double3 z)
    {
        var trace=x.X+y.Y+z.Z; double qx,qy,qz,qw;
        if(trace>0d){var s=Math.Sqrt(trace+1d)*2d;qw=.25d*s;qx=(y.Z-z.Y)/s;qy=(z.X-x.Z)/s;qz=(x.Y-y.X)/s;}
        else if(x.X>y.Y&&x.X>z.Z){var s=Math.Sqrt(1d+x.X-y.Y-z.Z)*2d;qw=(y.Z-z.Y)/s;qx=.25d*s;qy=(y.X+x.Y)/s;qz=(z.X+x.Z)/s;}
        else if(y.Y>z.Z){var s=Math.Sqrt(1d+y.Y-x.X-z.Z)*2d;qw=(z.X-x.Z)/s;qx=(y.X+x.Y)/s;qy=.25d*s;qz=(z.Y+y.Z)/s;}
        else {var s=Math.Sqrt(1d+z.Z-x.X-y.Y)*2d;qw=(x.Y-y.X)/s;qx=(z.X+x.Z)/s;qy=(z.Y+y.Z)/s;qz=.25d*s;}
        return new DoubleQuaternion(qx,qy,qz,qw).Normalized();
    }
}
