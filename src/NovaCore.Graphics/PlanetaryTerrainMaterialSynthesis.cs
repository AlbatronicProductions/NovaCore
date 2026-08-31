using System.Numerics;

namespace NovaCore.Graphics;

/// <summary>Reusable presentation material identities; they never enter terrain or simulation authority.</summary>
public enum PlanetaryTerrainMaterialKind : byte
{
    VegetatedSoil,
    WetGround,
    BeachSand,
    RockCliff,
    AlpineRock,
    DesertSand,
    SnowIce
}

/// <summary>Normalized, deterministic Earth presentation weights derived from body-fixed geographic inputs.</summary>
public readonly record struct PlanetaryTerrainMaterialWeights(
    float VegetatedSoil,
    float WetGround,
    float BeachSand,
    float RockCliff,
    float AlpineRock,
    float DesertSand,
    float SnowIce)
{
    public float Total => VegetatedSoil + WetGround + BeachSand + RockCliff + AlpineRock + DesertSand + SnowIce;
    public bool IsFinite => float.IsFinite(VegetatedSoil) && float.IsFinite(WetGround) && float.IsFinite(BeachSand) &&
        float.IsFinite(RockCliff) && float.IsFinite(AlpineRock) && float.IsFinite(DesertSand) && float.IsFinite(SnowIce);
    public float this[PlanetaryTerrainMaterialKind kind] => kind switch
    {
        PlanetaryTerrainMaterialKind.VegetatedSoil => VegetatedSoil,
        PlanetaryTerrainMaterialKind.WetGround => WetGround,
        PlanetaryTerrainMaterialKind.BeachSand => BeachSand,
        PlanetaryTerrainMaterialKind.RockCliff => RockCliff,
        PlanetaryTerrainMaterialKind.AlpineRock => AlpineRock,
        PlanetaryTerrainMaterialKind.DesertSand => DesertSand,
        PlanetaryTerrainMaterialKind.SnowIce => SnowIce,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

public readonly record struct PlanetaryTerrainPbrMaterial(
    Vector3 BaseColor,
    float Roughness,
    float Metallic,
    float AmbientOcclusion,
    float VisualDisplacementMetres)
{
    public bool IsFinite => float.IsFinite(BaseColor.X) && float.IsFinite(BaseColor.Y) && float.IsFinite(BaseColor.Z) &&
        float.IsFinite(Roughness) && float.IsFinite(Metallic) && float.IsFinite(AmbientOcclusion) && float.IsFinite(VisualDisplacementMetres);
}

/// <summary>
/// CPU oracle for the shader's compact Earth material synthesis. Geographic albedo/elevation remain authoritative;
/// these values add bounded presentation frequency and are deliberately absent at orbital distance.
/// </summary>
public static class PlanetaryTerrainMaterialSynthesis
{
    public const float FullDetailAltitudeMetres = 1_200f;
    public const float ZeroDetailAltitudeMetres = 18_000f;
    public const float MaximumVisualDisplacementMetres = .45f;
    public const float Bc4MaximumPhysicalHeightErrorMetres = 2.008f;
    public const float Bc4RmsPhysicalHeightErrorMetres = 1.460f;
    public const float MaximumMaterialNormalAngleDegrees = 8f;

    private static readonly PlanetaryTerrainPbrMaterial[] Library =
    [
        new(new(.105f, .205f, .070f), .88f, 0f, .94f, .08f),
        new(new(.245f, .165f, .090f), .91f, 0f, .91f, .12f),
        new(new(.54f, .43f, .245f), .82f, 0f, .96f, .06f),
        new(new(.235f, .225f, .205f), .78f, 0f, .82f, .45f),
        new(new(.355f, .350f, .335f), .74f, 0f, .86f, .32f),
        new(new(.42f, .245f, .115f), .84f, 0f, .90f, .22f),
        new(new(.72f, .78f, .82f), .62f, 0f, .98f, .04f)
    ];

    public static ReadOnlySpan<PlanetaryTerrainPbrMaterial> Materials => Library;

    public static PlanetaryTerrainMaterialWeights Classify(
        float landMask,
        float elevationMetres,
        float slope,
        float absoluteLatitude,
        float moisture,
        float temperature)
    {
        if (!float.IsFinite(landMask) || !float.IsFinite(elevationMetres) || !float.IsFinite(slope) ||
            !float.IsFinite(absoluteLatitude) || !float.IsFinite(moisture) || !float.IsFinite(temperature))
            throw new ArgumentOutOfRangeException();

        landMask = Math.Clamp(landMask, 0f, 1f);
        slope = Math.Clamp(slope, 0f, 1f);
        absoluteLatitude = Math.Clamp(absoluteLatitude, 0f, 1f);
        moisture = Math.Clamp(moisture, 0f, 1f);
        temperature = Math.Clamp(temperature, 0f, 1f);

        var flat = 1f - SmoothStep(.32f, .78f, slope);
        var cliff = SmoothStep(.38f, .82f, slope);
        var beach = flat * (1f - SmoothStep(30f, 360f, MathF.Abs(elevationMetres))) * (1f - SmoothStep(.84f, .94f, absoluteLatitude));
        var snowClimate = Math.Clamp(absoluteLatitude + Math.Clamp(elevationMetres / 8_000f, 0f, 1f) * .42f + (1f - temperature) * .22f, 0f, 1f);
        var snow = SmoothStep(.72f, .94f, snowClimate) * (1f - .42f * cliff);
        var alpine = SmoothStep(1_100f, 3_600f, elevationMetres) * (1f - snow) * (.38f + .62f * cliff);
        var desert = SmoothStep(.42f, .82f, (1f - moisture) * (.55f + .45f * temperature)) * (1f - snow) * (1f - .35f * alpine);
        var vegetation = flat * moisture * temperature * (1f - beach) * (1f - snow) * (1f - desert);
        var rock = cliff * (1f - .5f * snow) * (1f - .35f * alpine);
        var soil = flat * (1f - beach) * (1f - snow) * (1f - .6f * vegetation) * (1f - .55f * desert);

        // The material system is land-only. Keeping a normalized result for low mask values makes the oracle
        // total and deterministic while the shader retains the shared ocean branch unchanged.
        var land = SmoothStep(.45f, .55f, landMask);
        vegetation *= land; soil *= land; beach *= land; rock *= land; alpine *= land; desert *= land; snow *= land;
        var total = vegetation + soil + beach + rock + alpine + desert + snow;
        if (!(total > 1e-7f)) return new(0f, 1f, 0f, 0f, 0f, 0f, 0f);
        var inverse = 1f / total;
        return new(vegetation * inverse, soil * inverse, beach * inverse, rock * inverse, alpine * inverse, desert * inverse, snow * inverse);
    }

    public static PlanetaryTerrainMaterialWeights Classify(in PlanetaryBiomeBlend biomes, float landMask)
    {
        if (!biomes.IsFinite || !float.IsFinite(landMask)) throw new ArgumentOutOfRangeException();
        var land = SmoothStep(.45f, .55f, Math.Clamp(landMask, 0f, 1f));
        var grass = (float)biomes.Weight(PlanetarySurfaceBiome.GrassRolling);
        var scrub = (float)biomes.Weight(PlanetarySurfaceBiome.ScrubDry);
        var wet = (float)biomes.Weight(PlanetarySurfaceBiome.Wetland);
        var developed = (float)biomes.Weight(PlanetarySurfaceBiome.DevelopedReserved);
        var vegetation = (grass + .28f * scrub) * land;
        var wetGround = (wet + .55f * scrub + .65f * developed) * land;
        var beach = (float)biomes.Weight(PlanetarySurfaceBiome.BeachSand) * land;
        var rock = ((float)biomes.Weight(PlanetarySurfaceBiome.RockyMountain) + .35f * developed) * land;
        var alpine = (float)biomes.Weight(PlanetarySurfaceBiome.Alpine) * land;
        var desert = (float)biomes.Weight(PlanetarySurfaceBiome.Desert) * land;
        var snow = (float)biomes.Weight(PlanetarySurfaceBiome.SnowGlacial) * land;
        var total = vegetation + wetGround + beach + rock + alpine + desert + snow;
        if (!(total > 1e-7f)) return new(0f, 1f, 0f, 0f, 0f, 0f, 0f);
        var inverse = 1f / total;
        return new(vegetation * inverse, wetGround * inverse, beach * inverse, rock * inverse,
            alpine * inverse, desert * inverse, snow * inverse);
    }

    public static PlanetaryTerrainPbrMaterial Blend(in PlanetaryTerrainMaterialWeights weights)
    {
        if (!weights.IsFinite || MathF.Abs(weights.Total - 1f) > 2e-5f) throw new ArgumentOutOfRangeException(nameof(weights));
        var color = Vector3.Zero; var roughness = 0f; var metallic = 0f; var ao = 0f; var displacement = 0f;
        for (var index = 0; index < Library.Length; index++)
        {
            var weight = weights[(PlanetaryTerrainMaterialKind)index]; var material = Library[index];
            color += material.BaseColor * weight; roughness += material.Roughness * weight; metallic += material.Metallic * weight;
            ao += material.AmbientOcclusion * weight; displacement += material.VisualDisplacementMetres * weight;
        }
        return new(color, roughness, metallic, ao, Math.Clamp(displacement, -MaximumVisualDisplacementMetres, MaximumVisualDisplacementMetres));
    }

    public static float DetailWeight(float surfaceAltitudeMetres)
    {
        if (!float.IsFinite(surfaceAltitudeMetres)) throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));
        return 1f - SmoothStep(FullDetailAltitudeMetres, ZeroDetailAltitudeMetres, Math.Max(surfaceAltitudeMetres, 0f));
    }

    /// <summary>Band-limit used by the shader for non-mipped procedural frequency.</summary>
    public static float FrequencyAttenuation(float metresPerPixel, float scaleMetres)
    {
        if (!float.IsFinite(metresPerPixel) || metresPerPixel < 0f || !float.IsFinite(scaleMetres) || scaleMetres <= 0f)
            throw new ArgumentOutOfRangeException();
        return 1f - SmoothStep(.22f, .62f, metresPerPixel / scaleMetres);
    }

    public static float NormalFrequencyAttenuation(float metresPerPixel, float scaleMetres)
    {
        if (!float.IsFinite(metresPerPixel) || metresPerPixel < 0f || !float.IsFinite(scaleMetres) || scaleMetres <= 0f)
            throw new ArgumentOutOfRangeException();
        return 1f - SmoothStep(.12f, .38f, metresPerPixel / scaleMetres);
    }

    /// <summary>
    /// Adaptive biplanar weights. The smallest axis is removed only when its identity is unambiguous;
    /// a narrow three-weight bridge makes the pair change continuous at equal-axis boundaries.
    /// </summary>
    public static Vector3 BiplanarWeights(Vector3 surfaceNormal)
    {
        if (!float.IsFinite(surfaceNormal.X) || !float.IsFinite(surfaceNormal.Y) || !float.IsFinite(surfaceNormal.Z) || surfaceNormal.LengthSquared() <= 0f)
            throw new ArgumentOutOfRangeException(nameof(surfaceNormal));
        var normal = Vector3.Normalize(surfaceNormal);
        var value = new Vector3(MathF.Pow(Math.Max(MathF.Abs(normal.X), 1e-5f), 3f), MathF.Pow(Math.Max(MathF.Abs(normal.Y), 1e-5f), 3f), MathF.Pow(Math.Max(MathF.Abs(normal.Z), 1e-5f), 3f));
        value /= value.X + value.Y + value.Z;
        var selected = value; float smallest; float second;
        if (value.X <= value.Y && value.X <= value.Z) { smallest = value.X; second = Math.Min(value.Y, value.Z); selected.X = 0f; }
        else if (value.Y <= value.Z) { smallest = value.Y; second = Math.Min(value.X, value.Z); selected.Y = 0f; }
        else { smallest = value.Z; second = Math.Min(value.X, value.Y); selected.Z = 0f; }
        selected /= selected.X + selected.Y + selected.Z;
        var confidence = SmoothStep(.012f, .075f, second - smallest);
        var weights = Vector3.Lerp(value, selected, confidence);
        return weights / (weights.X + weights.Y + weights.Z);
    }

    public static float LimitNormalAngleDegrees(float candidateAngleDegrees)
    {
        if (!float.IsFinite(candidateAngleDegrees) || candidateAngleDegrees < 0f) throw new ArgumentOutOfRangeException(nameof(candidateAngleDegrees));
        return Math.Min(candidateAngleDegrees, MaximumMaterialNormalAngleDegrees);
    }

    /// <summary>Deterministic CPU diagnostic for the shader's bounded meso/micro height-normal field.</summary>
    public static float ProceduralNormalAngleDegrees(Vector3 bodyMetres, Vector3 surfaceNormal, float materialDisplacementMetres, float metresPerPixel)
    {
        if (!float.IsFinite(materialDisplacementMetres) || materialDisplacementMetres < 0f || materialDisplacementMetres > MaximumVisualDisplacementMetres ||
            !float.IsFinite(metresPerPixel) || metresPerPixel < 0f) throw new ArgumentOutOfRangeException();
        var normal = Vector3.Normalize(surfaceNormal);
        var reference = MathF.Abs(normal.Y) < .9f ? Vector3.UnitY : Vector3.UnitX;
        var east = Vector3.Normalize(Vector3.Cross(reference, normal));
        var north = Vector3.Normalize(Vector3.Cross(normal, east));
        var step = Math.Max(.02f, metresPerPixel * .5f);
        var eastSlope = (Height(bodyMetres + east * step) - Height(bodyMetres - east * step)) / (2f * step);
        var northSlope = (Height(bodyMetres + north * step) - Height(bodyMetres - north * step)) / (2f * step);
        var raw = MathF.Atan(MathF.Sqrt(eastSlope * eastSlope + northSlope * northSlope)) * (180f / MathF.PI);
        return LimitNormalAngleDegrees(raw);

        float Height(Vector3 point)
        {
            var meso = BiplanarNoise(point, normal, 96f, new(137f, 271f, 419f), NormalFrequencyAttenuation(metresPerPixel,96f));
            var micro = BiplanarNoise(point, normal, 5.5f, new(613f, 89f, 347f), NormalFrequencyAttenuation(metresPerPixel,5.5f));
            return materialDisplacementMetres * ((meso - .5f) * .72f + (micro - .5f) * .28f);
        }
    }

    private static float BiplanarNoise(Vector3 point, Vector3 normal, float scale, Vector3 offset, float attenuation)
    {
        var value = (point + offset) / scale; var weights = BiplanarWeights(normal);
        return weights.X * FilteredNoise(new(value.Y, value.Z), attenuation) +
               weights.Y * FilteredNoise(new(value.Z + 19.19f, value.X + 7.73f), attenuation) +
               weights.Z * FilteredNoise(new(value.X + 41.17f, value.Y + 3.11f), attenuation);
    }

    private static float FilteredNoise(Vector2 point, float attenuation) => .5f + (Noise(point) - .5f) * attenuation;
    private static float Noise(Vector2 point)
    {
        var cell = new Vector2(MathF.Floor(point.X), MathF.Floor(point.Y)); var fraction = point - cell;
        fraction *= fraction * (new Vector2(3f) - 2f * fraction);
        var a = Hash(cell); var b = Hash(cell + Vector2.UnitX); var c = Hash(cell + Vector2.UnitY); var d = Hash(cell + Vector2.One);
        return Lerp(Lerp(a, b, fraction.X), Lerp(c, d, fraction.X), fraction.Y);
    }

    private static float Hash(Vector2 point)
    {
        var value = Fract(new Vector3(point.X, point.Y, point.X) * new Vector3(.1031f, .1030f, .0973f));
        value += new Vector3(Vector3.Dot(value, new Vector3(value.Y, value.Z, value.X) + new Vector3(33.33f)));
        return Fract((value.X + value.Y) * value.Z);
    }

    private static Vector3 Fract(Vector3 value) => value - new Vector3(MathF.Floor(value.X), MathF.Floor(value.Y), MathF.Floor(value.Z));
    private static float Fract(float value) => value - MathF.Floor(value);
    private static float Lerp(float first, float second, float amount) => first + (second - first) * amount;

    private static float SmoothStep(float start, float end, float value)
    {
        var t = Math.Clamp((value - start) / (end - start), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}

/// <summary>Evidence model for the optional T3 tessellation gate. No runtime tessellation is enabled by this type.</summary>
public static class PlanetaryTerrainTessellationStudy
{
    public const int MaximumFactor = 8;
    public const int T3TriangleCount = 261_632;
    public const int MaximumAmplifiedTriangleCount = T3TriangleCount * MaximumFactor * MaximumFactor;
    public const bool AcceptedForProduction = false;

    public static int EdgeFactor(float projectedEdgePixels, float targetEdgePixels, float rangeWeight)
    {
        if (!float.IsFinite(projectedEdgePixels) || projectedEdgePixels < 0f || !float.IsFinite(targetEdgePixels) || targetEdgePixels <= 0f ||
            !float.IsFinite(rangeWeight)) throw new ArgumentOutOfRangeException();
        var raw = projectedEdgePixels / targetEdgePixels * Math.Clamp(rangeWeight, 0f, 1f);
        var factor = 1;
        while (factor < MaximumFactor && factor < raw) factor <<= 1;
        return Math.Min(factor, MaximumFactor);
    }

    public static long AmplifiedTriangleCount(int factor)
    {
        if (factor is < 1 or > MaximumFactor || (factor & (factor - 1)) != 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return (long)T3TriangleCount * factor * factor;
    }
}
