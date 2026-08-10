using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public enum PlanetMaterialKind : uint
{
    Unspecified = 0,
    Rocky = 1,
    Terrestrial = 2,
    GasGiant = 3,
    IceGiant = 4
}

public enum PlanetAlbedoSource : uint
{
    Tint = 0,
    MercuryProcedural = 1,
    VenusProcedural = 2,
    EarthProcedural = 3,
    MoonProcedural = 4,
    MarsProcedural = 5,
    JupiterProcedural = 6,
    SaturnProcedural = 7,
    UranusProcedural = 8,
    NeptuneProcedural = 9,
    EarthAuthoritative = 10
}

public enum PlanetTextureProjection : uint
{
    BodyLocalDirection = 0
}

public readonly record struct PlanetRingPresentation(
    ulong ParentBodyId,
    DoubleQuaternion PlaneOrientation,
    double InnerRadiusMetres,
    double OuterRadiusMetres,
    Float3 Color,
    float Opacity,
    float BandFrequency,
    uint SourceIdentity = 0)
{
    public bool IsValid => ParentBodyId != 0 && PlaneOrientation.IsFinite && PlaneOrientation.LengthSquared > 0d &&
        double.IsFinite(InnerRadiusMetres) && double.IsFinite(OuterRadiusMetres) && InnerRadiusMetres > 0d &&
        OuterRadiusMetres > InnerRadiusMetres && Color.IsFinite && float.IsFinite(Opacity) && Opacity is > 0f and <= 1f &&
        float.IsFinite(BandFrequency) && BandFrequency > 0f;
}

public readonly record struct PlanetMaterialPresentation(
    ulong BodyId,
    PlanetMaterialKind Kind,
    PlanetAlbedoSource AlbedoSource,
    Float3 Tint,
    float Roughness,
    float Specular,
    float Emissive,
    float PresentationRotationRadians,
    PlanetTextureProjection Projection,
    uint AtmosphereHook,
    uint CloudHook,
    float LocalDetailScaleMeters = 64f,
    float LocalDetailMicroScaleMeters = 3f,
    float LocalDetailFadeStartMetres = 1200f,
    float LocalDetailFadeEndMetres = 18000f,
    PlanetRingPresentation? Ring = null)
{
    public bool IsValid => BodyId != 0 && Kind != PlanetMaterialKind.Unspecified && AlbedoSource != PlanetAlbedoSource.Tint &&
        Tint.IsFinite && float.IsFinite(Roughness) && Roughness is >= 0f and <= 1f &&
        float.IsFinite(Specular) && Specular is >= 0f and <= 1f && float.IsFinite(Emissive) && Emissive >= 0f &&
        float.IsFinite(PresentationRotationRadians) &&
        float.IsFinite(LocalDetailScaleMeters) && LocalDetailScaleMeters > 0f &&
        float.IsFinite(LocalDetailMicroScaleMeters) && LocalDetailMicroScaleMeters > 0f &&
        float.IsFinite(LocalDetailFadeStartMetres) && float.IsFinite(LocalDetailFadeEndMetres) &&
        LocalDetailFadeStartMetres >= 0f && LocalDetailFadeEndMetres > LocalDetailFadeStartMetres &&
        (!Ring.HasValue || Ring.Value.IsValid && Ring.Value.ParentBodyId == BodyId);
}

public sealed class PlanetMaterialCatalog
{
    private readonly PlanetMaterialPresentation[] _materials;

    public PlanetMaterialCatalog(IEnumerable<PlanetMaterialPresentation> materials)
    {
        ArgumentNullException.ThrowIfNull(materials);
        _materials = materials.OrderBy(material => material.BodyId).ToArray();
        if (_materials.Length == 0 || _materials.Any(material => !material.IsValid)) throw new ArgumentException("Every planet material must be valid.", nameof(materials));
        for (var index = 1; index < _materials.Length; index++)
            if (_materials[index - 1].BodyId == _materials[index].BodyId) throw new ArgumentException("Planet material body IDs must be unique.", nameof(materials));
    }

    public ReadOnlySpan<PlanetMaterialPresentation> Materials => _materials;

    public bool TryGet(ulong bodyId, out PlanetMaterialPresentation material)
    {
        var low = 0;
        var high = _materials.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var candidate = _materials[middle];
            if (candidate.BodyId == bodyId) { material = candidate; return true; }
            if (candidate.BodyId < bodyId) low = middle + 1; else high = middle - 1;
        }
        material = default;
        return false;
    }
}

    public static class PlanetMaterialNativeEncoder
    {
        public const float DefaultLocalDetailScaleMeters = 64f;
        public const float DefaultLocalDetailMicroScaleMeters = 3f;
        public const float DefaultLocalDetailFadeStartMetres = 1200f;
        public const float DefaultLocalDetailFadeEndMetres = 18000f;

        public static void Apply(ref NativePlanetaryPresentation native, in PlanetMaterialPresentation material)
        {
            if (!material.IsValid) throw new ArgumentException("Planet material is invalid.", nameof(material));
            native.BodyIdLow = (uint)material.BodyId;
            native.BodyIdHigh = (uint)(material.BodyId >> 32);
            native.MaterialKind = (uint)material.Kind;
            native.AlbedoSource = (uint)material.AlbedoSource;
            native.Roughness = material.Roughness;
            native.Specular = material.Specular;
            native.Emissive = material.Emissive;
            native.PresentationRotationRadians = material.PresentationRotationRadians;
            native.LocalDetailScaleMeters = material.LocalDetailScaleMeters;
            native.LocalDetailMicroScaleMeters = material.LocalDetailMicroScaleMeters;
            native.LocalDetailFadeStartMetres = material.LocalDetailFadeStartMetres;
            native.LocalDetailFadeEndMetres = material.LocalDetailFadeEndMetres;
            native.BodyOrientationX = 0f;
            native.BodyOrientationY = 0f;
            native.BodyOrientationZ = 0f;
            native.BodyOrientationW = 1f;
            native.ProjectionKind = (uint)material.Projection;
        native.AtmosphereHook = material.AtmosphereHook;
        native.CloudHook = material.CloudHook;
        native.RingOrientationW = 1f;
        if (!material.Ring.HasValue) return;
        var ring = material.Ring.Value;
        native.RingAssociation = ring.SourceIdentity == 0 ? 1u : ring.SourceIdentity;
        native.RingInnerRadiusRatio = (float)(ring.InnerRadiusMetres / native.Radius);
        native.RingOuterRadiusRatio = (float)(ring.OuterRadiusMetres / native.Radius);
        native.RingOpacity = ring.Opacity;
        native.RingBandFrequency = ring.BandFrequency;
        var orientation = ring.PlaneOrientation.Normalized();
        native.RingOrientationX = (float)orientation.X;
        native.RingOrientationY = (float)orientation.Y;
        native.RingOrientationZ = (float)orientation.Z;
        native.RingOrientationW = (float)orientation.W;
        native.RingColorR = ring.Color.X;
        native.RingColorG = ring.Color.Y;
        native.RingColorB = ring.Color.Z;
        native.RingColorA = ring.Opacity;
        }

        public static void ApplyLocalDefaults(ref NativePlanetaryPresentation native)
        {
            native.LocalDetailScaleMeters = DefaultLocalDetailScaleMeters;
            native.LocalDetailMicroScaleMeters = DefaultLocalDetailMicroScaleMeters;
            native.LocalDetailFadeStartMetres = DefaultLocalDetailFadeStartMetres;
            native.LocalDetailFadeEndMetres = DefaultLocalDetailFadeEndMetres;
        }
}
