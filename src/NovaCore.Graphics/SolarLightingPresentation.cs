using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Interop;

namespace NovaCore.Graphics;

/// <summary>Immutable presentation-only stellar lighting derived from an evaluated celestial source.</summary>
public readonly record struct SolarLightingPresentation(
    UniversePosition SourceCenter,
    Float3 PhotosphereColor,
    float Exposure,
    float AmbientFloor,
    float SourceRadiance,
    float GlowStrength)
{
    public static SolarLightingPresentation CreateDefault(in UniversePosition sourceCenter) =>
        new(sourceCenter, new Float3(1f, .82f, .48f), 1f, .025f, 2.25f, 1f);

    public bool IsValid =>
        SourceCenter.Value.IsFinite && PhotosphereColor.IsFinite &&
        PhotosphereColor.X >= 0f && PhotosphereColor.Y >= 0f && PhotosphereColor.Z >= 0f &&
        float.IsFinite(Exposure) && Exposure > 0f &&
        float.IsFinite(AmbientFloor) && AmbientFloor is >= 0f and <= 1f &&
        float.IsFinite(SourceRadiance) && SourceRadiance > 1f &&
        float.IsFinite(GlowStrength) && GlowStrength is >= 0f and <= 4f;

    public bool TryEncode(in UniversePosition cameraRoot, out NativeSolarLighting native)
    {
        native = default;
        if (!IsValid || cameraRoot.Frame != SourceCenter.Frame || !cameraRoot.Value.IsFinite) return false;
        if (!CameraRelativeRenderPosition.TryCreate(SourceCenter, cameraRoot, out var relative) || !relative.TryNarrow(out var narrowed)) return false;
        native = new NativeSolarLighting
        {
            SourceCenterX = narrowed.X,
            SourceCenterY = narrowed.Y,
            SourceCenterZ = narrowed.Z,
            Exposure = Exposure,
            PhotosphereR = PhotosphereColor.X,
            PhotosphereG = PhotosphereColor.Y,
            PhotosphereB = PhotosphereColor.Z,
            AmbientFloor = AmbientFloor,
            SourceRadiance = SourceRadiance,
            GlowStrength = GlowStrength,
            Enabled = 1
        };
        return true;
    }
}
