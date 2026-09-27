namespace NovaCore.Core.Surface;

internal readonly record struct PhysicalPatchCoordinate(double X,double Y);
internal readonly record struct PhysicalCollisionFrame(Double3 Radial,Double3 East,Double3 North,double Radius)
{
    internal const double MaximumCoordinate=100_000;
    internal bool Valid=>Radial.IsFinite&&East.IsFinite&&North.IsFinite&&double.IsFinite(Radius)&&Radius>0&&
        Math.Abs(Radial.LengthSquared-1)<1e-12&&Math.Abs(East.LengthSquared-1)<1e-12&&Math.Abs(North.LengthSquared-1)<1e-12&&
        Math.Abs(Double3.Dot(Radial,East))+Math.Abs(Double3.Dot(Radial,North))+Math.Abs(Double3.Dot(East,North))<1e-12;
    internal Double3 Ray(PhysicalPatchCoordinate p)=>Radial*Radius+East*p.X+North*p.Y;
}

/// <summary>Finite collision geometry is prepared by the acquired H owner, never
/// by a render mesh or point-normal stencil. Error bounds cover radial height
/// disagreement between admitted H evaluations and the plane through the three
/// returned samples, over all rays in the parameter triangle. Mathematical
/// geometry is bounded over the entire domain; finite numerical evaluation
/// remains fallible. An evaluation outside the independently verified numerical
/// envelope refuses preparation/continuation and never substitutes a height.
/// Preparing a patch does not promise every future numerical query will succeed.
/// These are not same-parameter
/// Cartesian interpolation errors. Native vertex conversion is a separate bound.</summary>
internal interface IPhysicalSurfaceCollisionSource:IPhysicalSurfacePointQuery
{
    Double3 CollisionPoint(PhysicalCollisionFrame frame,PhysicalPatchCoordinate point);
    bool TryPreparePatch(PhysicalCollisionFrame frame,PhysicalPatchCoordinate minimum,PhysicalPatchCoordinate maximum,out IPhysicalCollisionPatch? patch);
    bool TryTriangleError(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres);
}
internal interface IPhysicalCollisionPatch
{
    // Full-domain projection onto Florida Up, minus its reference radius.
    // Includes the source's finite evaluation envelope; not a sampled maximum.
    bool TryFloridaHeightRange(out double minimum,out double maximum);
    bool TryTriangleError(PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres,double maximumError=double.PositiveInfinity);
    bool TrySubpatch(PhysicalPatchCoordinate minimum,PhysicalPatchCoordinate maximum,out IPhysicalCollisionPatch? patch);
    Double3 CollisionPoint(PhysicalPatchCoordinate point);
}
