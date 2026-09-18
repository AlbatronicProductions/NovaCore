namespace NovaCore.Core.Surface;

/// <summary>Immutable authored Florida base/footing union, distinct from terrain grading authority.</summary>
internal sealed class FloridaSlabSupport
{
    internal const string Identity = "novacore.florida.base-support/1";
    internal const double EastWidth = 64, NorthLength = 48, AuthoredTop = 1.5;
    internal PhysicalSurfaceAuthorityIdentity Authority { get; }
    internal double RootRadius { get; }
    internal double FoundationDepth { get; }
    internal double TopRadius => RootRadius + AuthoredTop;
    // Native/site coordinates are East, Up, -North. Both consumers use this exact declaration.
    internal Double3 Dimensions => new(EastWidth, FoundationDepth + AuthoredTop, NorthLength);
    internal Double3 TopBodyFixed => FloridaFacilitySupport.Region.Up * TopRadius;

    internal FloridaSlabSupport(PhysicalSurfaceAuthorityIdentity authority, double rootRadius, double foundationDepth)
    {
        var region = FloridaFacilitySupport.Region;
        if (authority.BodyId != region.BodyId || authority.FacilitySupportIdentity != FloridaFacilitySupport.DefinitionIdentity ||
            authority.ReferenceRadiusMetres != region.RadiusMetres || !double.IsFinite(rootRadius) ||
            !double.IsFinite(foundationDepth) || foundationDepth <= 0 || foundationDepth >= 32 ||
            Math.Abs(rootRadius - foundationDepth - region.RadiusMetres - region.PlaneAltitudeMetres) > 1e-6)
            throw new ArgumentException("Florida authored base must meet the authenticated graded site.");
        Authority = authority; RootRadius = rootRadius; FoundationDepth = foundationDepth;
    }
}
