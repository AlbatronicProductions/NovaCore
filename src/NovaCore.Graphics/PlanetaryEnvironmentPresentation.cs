using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

[Flags]
public enum PlanetaryEnvironmentLayers : uint
{
    None = 0,
    Atmosphere = 1 << 0,
    Clouds = 1 << 1,
    Ocean = 1 << 2
}

/// <summary>Immutable, presentation-only atmosphere/cloud/ocean parameters associated with one body.</summary>
public readonly record struct PlanetaryEnvironmentPresentation(
    ulong BodyId,
    uint SourceVersion,
    PlanetaryEnvironmentLayers Layers,
    double AtmosphereHeightMetres,
    double RayleighScaleHeightMetres,
    double MieScaleHeightMetres,
    float MieAnisotropy,
    Float3 RayleighScattering,
    float MieScattering,
    double CloudBaseHeightMetres,
    double CloudTopHeightMetres,
    float CloudCoverage,
    float CloudDensity,
    float CloudGlobalScale,
    float CloudDetailScale,
    float CloudShadowStrength,
    double OceanSeaLevelMetres,
    float OceanRoughness,
    float OceanWaveScale,
    float OceanWaveStrength,
    Float3 OceanColor,
    float ExposureAdjustment,
    double MaximumTerrainHeightMetres)
{
    public static PlanetaryEnvironmentPresentation EarthProceduralV1 => new(
        6, 1,
        PlanetaryEnvironmentLayers.Atmosphere | PlanetaryEnvironmentLayers.Clouds | PlanetaryEnvironmentLayers.Ocean,
        100_000d, 8_000d, 1_200d, .76f,
        new Float3(5.8e-3f, 13.5e-3f, 33.1e-3f), 4.0e-3f,
        2_000d, 11_000d, .82f, .55f, 5.5f, 38f, .14f,
        180d, .34f, 48_000f, .18f, new Float3(.008f, .055f, .16f),
        1f, PlanetaryTerrainDefinition.EarthProceduralV1.MaximumHeightMetres);

    public static PlanetaryEnvironmentPresentation EarthDataV2 => new(
        6, 2,
        PlanetaryEnvironmentLayers.Atmosphere | PlanetaryEnvironmentLayers.Clouds | PlanetaryEnvironmentLayers.Ocean,
        100_000d, 8_000d, 1_200d, .76f,
        new Float3(5.8e-3f, 13.5e-3f, 33.1e-3f), 4.0e-3f,
        2_000d, 12_000d, .42f, .82f, 1f, 8f, .22f,
        .5d, .16f, 2_400f, .12f, new Float3(.006f, .035f, .11f),
        .94f, PlanetaryTerrainDefinition.EarthAuthoritativeV3.MaximumHeightMetres);

    public bool IsValid => BodyId != 0 && SourceVersion != 0 && Layers != PlanetaryEnvironmentLayers.None &&
        double.IsFinite(AtmosphereHeightMetres) && AtmosphereHeightMetres > 0d &&
        double.IsFinite(RayleighScaleHeightMetres) && RayleighScaleHeightMetres > 0d && RayleighScaleHeightMetres < AtmosphereHeightMetres &&
        double.IsFinite(MieScaleHeightMetres) && MieScaleHeightMetres > 0d && MieScaleHeightMetres < AtmosphereHeightMetres &&
        float.IsFinite(MieAnisotropy) && MieAnisotropy is >= 0f and < 1f && RayleighScattering.IsFinite &&
        RayleighScattering.X > 0f && RayleighScattering.Y > 0f && RayleighScattering.Z > 0f &&
        float.IsFinite(MieScattering) && MieScattering > 0f &&
        double.IsFinite(CloudBaseHeightMetres) && double.IsFinite(CloudTopHeightMetres) &&
        CloudBaseHeightMetres >= 0d && CloudTopHeightMetres > CloudBaseHeightMetres && CloudTopHeightMetres < AtmosphereHeightMetres &&
        float.IsFinite(CloudCoverage) && CloudCoverage is > 0f and < 1f && float.IsFinite(CloudDensity) && CloudDensity > 0f &&
        float.IsFinite(CloudGlobalScale) && CloudGlobalScale > 0f && float.IsFinite(CloudDetailScale) && CloudDetailScale > CloudGlobalScale &&
        float.IsFinite(CloudShadowStrength) && CloudShadowStrength is >= 0f and <= 1f &&
        double.IsFinite(OceanSeaLevelMetres) && OceanSeaLevelMetres >= 0d &&
        float.IsFinite(OceanRoughness) && OceanRoughness is > 0f and <= 1f && float.IsFinite(OceanWaveScale) && OceanWaveScale > 0f &&
        float.IsFinite(OceanWaveStrength) && OceanWaveStrength is >= 0f and <= 1f && OceanColor.IsFinite &&
        float.IsFinite(ExposureAdjustment) && ExposureAdjustment is > 0f and <= 2f &&
        double.IsFinite(MaximumTerrainHeightMetres) && MaximumTerrainHeightMetres > OceanSeaLevelMetres;

    public double VisibleSurfaceHeight(in Double3 bodyDirection, in PlanetaryTerrainDefinition terrain) =>
        Math.Max(terrain.SampleHeight(bodyDirection, 24), Layers.HasFlag(PlanetaryEnvironmentLayers.Ocean) ? OceanSeaLevelMetres : 0d);

    public NativePlanetaryEnvironment Encode(in PlanetRenderProxy body, in UniversePosition cameraRoot)
    {
        if (!IsValid || body.BodyId != BodyId || body.Position.Frame != cameraRoot.Frame) throw new ArgumentException("Planetary environment authority mismatch.");
        var center = CameraRelativeRenderPosition.Create(body.Position, cameraRoot);
        if (!center.TryNarrow(out var narrowed)) throw new ArgumentOutOfRangeException(nameof(cameraRoot));
        return new NativePlanetaryEnvironment
        {
            CenterX = narrowed.X, CenterY = narrowed.Y, CenterZ = narrowed.Z, Radius = (float)body.RadiusMetres,
            BodyIdLow = (uint)BodyId, BodyIdHigh = (uint)(BodyId >> 32), EnabledLayers = (uint)Layers, SourceVersion = SourceVersion,
            AtmosphereHeightMetres = (float)AtmosphereHeightMetres, RayleighScaleHeightMetres = (float)RayleighScaleHeightMetres,
            MieScaleHeightMetres = (float)MieScaleHeightMetres, MieAnisotropy = MieAnisotropy,
            RayleighR = RayleighScattering.X, RayleighG = RayleighScattering.Y, RayleighB = RayleighScattering.Z, MieScattering = MieScattering,
            CloudBaseHeightMetres = (float)CloudBaseHeightMetres, CloudTopHeightMetres = (float)CloudTopHeightMetres,
            CloudCoverage = CloudCoverage, CloudDensity = CloudDensity, CloudGlobalScale = CloudGlobalScale, CloudDetailScale = CloudDetailScale,
            CloudShadowStrength = CloudShadowStrength, MaximumTerrainHeightMetres = (float)MaximumTerrainHeightMetres,
            OceanSeaLevelMetres = (float)OceanSeaLevelMetres, OceanRoughness = OceanRoughness, OceanWaveScale = OceanWaveScale,
            OceanWaveStrength = OceanWaveStrength, OceanColorR = OceanColor.X, OceanColorG = OceanColor.Y, OceanColorB = OceanColor.Z,
            ExposureAdjustment = ExposureAdjustment
        };
    }
}

public sealed class PlanetaryEnvironmentCatalog
{
    private readonly PlanetaryEnvironmentPresentation[] _values;
    public PlanetaryEnvironmentCatalog(IEnumerable<PlanetaryEnvironmentPresentation> values)
    {
        ArgumentNullException.ThrowIfNull(values);_values=values.OrderBy(value=>value.BodyId).ToArray();
        if(_values.Length==0||_values.Any(value=>!value.IsValid))throw new ArgumentException("Every planetary environment must be valid.",nameof(values));
        for(var index=1;index<_values.Length;index++)if(_values[index-1].BodyId==_values[index].BodyId)throw new ArgumentException("Planetary environment body IDs must be unique.",nameof(values));
    }
    public bool TryGet(ulong bodyId,out PlanetaryEnvironmentPresentation environment)
    {
        var low=0;var high=_values.Length-1;while(low<=high){var middle=low+((high-low)>>1);var candidate=_values[middle];if(candidate.BodyId==bodyId){environment=candidate;return true;}if(candidate.BodyId<bodyId)low=middle+1;else high=middle-1;}environment=default;return false;
    }
}

public enum PlanetaryCameraPresentationMode
{
    Orbital,
    Transition,
    SurfaceLocal
}

/// <summary>Reusable presentation focus for a future body-fixed surface object or vehicle.</summary>
public readonly record struct PlanetarySurfaceFocus(
    ulong BodyId,
    Double3 BodyLocalAnchor,
    PlanetarySurfaceFrame TangentFrame,
    Double3 LocalCameraOffset)
{
    public bool IsValid => BodyId != 0 && BodyLocalAnchor.IsFinite && TangentFrame.Direction.IsFinite && LocalCameraOffset.IsFinite;
    public static PlanetarySurfaceFocus AtDirection(ulong bodyId,in Double3 direction,double surfaceRadius,double altitudeMetres)
    {
        if(bodyId==0||!double.IsFinite(surfaceRadius)||surfaceRadius<=0d||!double.IsFinite(altitudeMetres)||altitudeMetres<0d)throw new ArgumentOutOfRangeException();
        var frame=PlanetarySurfaceFrame.AtDirection(direction);return new(bodyId,frame.Direction*surfaceRadius,frame,frame.Up*altitudeMetres);
    }
}

public static class PlanetarySurfaceCameraPolicy
{
    public const double OrbitalTransitionAltitudeMetres=1_000_000d;
    public const double SurfaceLocalAltitudeMetres=100_000d;
    public const double MinimumPitchRadians=-1.45d;
    public const double MaximumPitchRadians=1.35d;
    public const double MinimumNearClipMetres=.05d;
    public const double MaximumNearClipMetres=1_000_000d;
    public const double NearClipAltitudeFraction=.02d;

    public static double SurfaceBlend(double altitudeMetres)
    {
        if(!double.IsFinite(altitudeMetres))throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        var t=Math.Clamp((OrbitalTransitionAltitudeMetres-altitudeMetres)/(OrbitalTransitionAltitudeMetres-SurfaceLocalAltitudeMetres),0d,1d);return t*t*(3d-2d*t);
    }
    public static PlanetaryCameraPresentationMode Mode(double altitudeMetres)
    {
        if(altitudeMetres<=SurfaceLocalAltitudeMetres)return PlanetaryCameraPresentationMode.SurfaceLocal;
        if(altitudeMetres<OrbitalTransitionAltitudeMetres)return PlanetaryCameraPresentationMode.Transition;
        return PlanetaryCameraPresentationMode.Orbital;
    }
    public static double ZoomFactor(double altitudeMetres)=>altitudeMetres switch{>1_000_000d=>1.6d,>100_000d=>1.35d,>10_000d=>1.22d,_=>1.12d};
    /// <summary>
    /// Continuous reversed-Z near plane. Two percent of surface altitude keeps the
    /// visible surface comfortably beyond the clip plane while preserving local depth precision.
    /// </summary>
    public static double NearClipMetres(double surfaceAltitudeMetres)
    {
        if(!double.IsFinite(surfaceAltitudeMetres))throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));
        return Math.Clamp(Math.Max(0d,surfaceAltitudeMetres)*NearClipAltitudeFraction,MinimumNearClipMetres,MaximumNearClipMetres);
    }
    public static double TranslationSpeedMetresPerSecond(double altitudeMetres)
    {
        if(!double.IsFinite(altitudeMetres)||altitudeMetres<0d)throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        return Math.Clamp(12d+altitudeMetres*.02d,12d,2_000d);
    }
}
