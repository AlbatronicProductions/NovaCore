using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static class SolarPlanetMaterials
{
    internal static readonly PlanetaryEnvironmentCatalog Environments = new([PlanetaryEnvironmentPresentation.EarthProceduralV1]);
    internal static readonly PlanetMaterialCatalog Catalog = new(
    [
        Material(3, PlanetMaterialKind.Rocky, PlanetAlbedoSource.MercuryProcedural, new(.52f, .50f, .47f), .92f, .03f, .35f),
        Material(4, PlanetMaterialKind.Terrestrial, PlanetAlbedoSource.VenusProcedural, new(.92f, .72f, .38f), .96f, .02f, 1.15f),
        Material(6, PlanetMaterialKind.Terrestrial, PlanetAlbedoSource.EarthProcedural, new(.10f, .42f, .88f), .58f, .16f, -.35f, 1, 1),
        Material(7, PlanetMaterialKind.Rocky, PlanetAlbedoSource.MoonProcedural, new(.61f, .60f, .57f), .95f, .02f, .2f),
        Material(8, PlanetMaterialKind.Rocky, PlanetAlbedoSource.MarsProcedural, new(.78f, .28f, .12f), .88f, .04f, .7f),
        Material(9, PlanetMaterialKind.GasGiant, PlanetAlbedoSource.JupiterProcedural, new(.84f, .64f, .42f), .84f, .04f, -.2f),
        new PlanetMaterialPresentation(10, PlanetMaterialKind.GasGiant, PlanetAlbedoSource.SaturnProcedural, new(.88f, .73f, .46f), .88f, .03f, 0f, .45f, PlanetTextureProjection.BodyLocalDirection, 0, 0,
            new PlanetRingPresentation(10, DoubleQuaternion.FromAxisAngle(Double3.UnitX, 26.73d * Math.PI / 180d), 74_658_000d, 136_775_000d, new(.78f, .69f, .52f), .78f, 17f, 1)),
        Material(11, PlanetMaterialKind.IceGiant, PlanetAlbedoSource.UranusProcedural, new(.45f, .83f, .86f), .72f, .06f, .1f),
        Material(12, PlanetMaterialKind.IceGiant, PlanetAlbedoSource.NeptuneProcedural, new(.12f, .31f, .88f), .68f, .08f, -.55f)
    ]);

    internal static bool TryApply(ref NativePlanetaryPresentation native, ulong bodyId)
    {
        if (bodyId == 2)
        {
            native.BodyIdLow = 2;
            native.RingOrientationW = 1f;
            return true;
        }
        if (!Catalog.TryGet(bodyId, out var material)) return false;
        PlanetMaterialNativeEncoder.Apply(ref native, material);
        return true;
    }

    private static PlanetMaterialPresentation Material(ulong id, PlanetMaterialKind kind, PlanetAlbedoSource source, Float3 tint, float roughness, float specular, float rotation,uint atmosphereHook=0,uint cloudHook=0) =>
        new(id, kind, source, tint, roughness, specular, 0f, rotation, PlanetTextureProjection.BodyLocalDirection, atmosphereHook, cloudHook, null);
}
