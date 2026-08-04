namespace NovaCore.Core.ReferenceFrames;

public readonly record struct OrbitalFrameGeometry(double InclinationRadians, double LongitudeOfAscendingNodeRadians, double ArgumentOfPeriapsisRadians)
{
    /// <summary>ORB-to-ECL: Rz(LAN) * Rx(inclination) * Rz(argument of periapsis).</summary>
    public DoubleQuaternion ToEclRotation()
    {
        var z1=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,LongitudeOfAscendingNodeRadians);
        var x=DoubleQuaternion.FromAxisAngle(Double3.UnitX,InclinationRadians);
        var z2=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,ArgumentOfPeriapsisRadians);
        return (z1*x*z2).Normalized();
    }
}
