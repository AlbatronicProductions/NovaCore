namespace NovaCore.Core.Surface;

/// <summary>Trusted physical-query capability: conservative maximum radial
/// height over every direction in a closed angular cap of the current immutable
/// composition. A point normal or rendered mesh cannot supply this proof.</summary>
internal interface IPhysicalSurfaceHeightBounds : IPhysicalSurfacePointQuery
{
    double HeightUpperBound(in Double3 direction,double angularRadius);
}
