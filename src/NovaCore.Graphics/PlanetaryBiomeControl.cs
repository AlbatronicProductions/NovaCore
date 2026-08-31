using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Planet-wide Earth surface families. Values are stable GPU/diagnostic identities.</summary>
public enum PlanetarySurfaceBiome : byte
{
    OceanCoastal,
    BeachSand,
    Wetland,
    GrassRolling,
    ScrubDry,
    Desert,
    RockyMountain,
    Alpine,
    SnowGlacial,
    DevelopedReserved
}

public readonly record struct PlanetaryBiomeContribution(PlanetarySurfaceBiome Biome, double Weight)
{
    public bool IsFinite => Enum.IsDefined(Biome) && double.IsFinite(Weight) && Weight >= 0d && Weight <= 1d;
}

/// <summary>
/// Four strongest normalized contributors plus physical/material eligibility.
/// No patch, camera, residency, or time state participates in this value.
/// </summary>
public readonly record struct PlanetaryBiomeBlend(
    PlanetaryBiomeContribution First,
    PlanetaryBiomeContribution Second,
    PlanetaryBiomeContribution Third,
    PlanetaryBiomeContribution Fourth,
    double RollingEligibility,
    double RockyEligibility,
    double DesertEligibility,
    double CoastalEligibility,
    double GlacialEligibility,
    double MaterialEligibility)
{
    public double TotalWeight => First.Weight + Second.Weight + Third.Weight + Fourth.Weight;
    public PlanetarySurfaceBiome DominantBiome => First.Biome;
    public bool IsFinite => First.IsFinite && Second.IsFinite && Third.IsFinite && Fourth.IsFinite &&
        Math.Abs(TotalWeight - 1d) <= 1e-12d &&
        IsUnit(RollingEligibility) && IsUnit(RockyEligibility) && IsUnit(DesertEligibility) &&
        IsUnit(CoastalEligibility) && IsUnit(GlacialEligibility) && IsUnit(MaterialEligibility);

    public double Weight(PlanetarySurfaceBiome biome)
    {
        var value = 0d;
        if (First.Biome == biome) value += First.Weight;
        if (Second.Biome == biome) value += Second.Weight;
        if (Third.Biome == biome) value += Third.Weight;
        if (Fourth.Biome == biome) value += Fourth.Weight;
        return value;
    }

    private static bool IsUnit(double value) => double.IsFinite(value) && value >= 0d && value <= 1d;
}

/// <summary>
/// NovaCore-owned procedural Earth control oracle. It is intentionally compact
/// enough to mirror exactly in FP64 GLSL and can later be replaced by a streamed
/// control map without changing its four-contributor consumer contract.
/// </summary>
public static class PlanetaryBiomeControlAuthority
{
    public const uint SchemaVersion = 1;
    public const uint EarthSeed = 0xB10C0A11u;
    public const int MaximumContributors = 4;

    private static readonly Double3 ClimateAxisA = new(.7427813527082074d, .5570860145311556d, -.3713906763541037d);
    private static readonly Double3 ClimateAxisB = new(-.4364357804719848d, .2182178902359924d, .8728715609439696d);
    private static readonly Double3 ClimateAxisC = new(.2672612419124244d, -.8017837257372732d, .5345224838248488d);

    public static PlanetaryBiomeBlend Sample(in Double3 bodyFixedDirection, double geographicHeightMetres)
    {
        if (!bodyFixedDirection.IsFinite || bodyFixedDirection.LengthSquared <= 0d || !double.IsFinite(geographicHeightMetres))
            throw new ArgumentOutOfRangeException();
        var direction = bodyFixedDirection.Normalized();
        var point = direction * PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
        var latitude = Math.Abs(direction.Y);
        var temperature = Saturate(1d - latitude * .82d - Math.Max(geographicHeightMetres, 0d) / 8_500d);
        var climateA = .5d + .5d * PlanetaryProceduralMath.WrappedSin(point, ClimateAxisA, 1_850_000d, .37d);
        var climateB = .5d + .5d * PlanetaryProceduralMath.WrappedSin(point, ClimateAxisB, 620_000d, 2.11d);
        var climateC = .5d + .5d * PlanetaryProceduralMath.WrappedSin(point, ClimateAxisC, 210_000d, -1.43d);
        var moisture = Saturate(.18d + .46d * climateA + .24d * climateB + .12d * climateC - .18d * temperature);
        var aridity = Saturate((1d - moisture) * (.55d + .45d * temperature));
        var coast = 1d - SmoothStep(18d, 420d, Math.Abs(geographicHeightMetres));
        var land = SmoothStep(-2d, 8d, geographicHeightMetres);
        var highland = SmoothStep(420d, 2_400d, geographicHeightMetres);
        var alpineGate = SmoothStep(1_400d, 3_600d, geographicHeightMetres);
        var cold = Saturate(latitude * .9d + Math.Max(geographicHeightMetres, 0d) / 7_500d + (1d - temperature) * .25d);
        var snowGate = SmoothStep(.72d, .94d, cold);
        var wet = SmoothStep(.58d, .86d, moisture) * (1d - SmoothStep(130d, 900d, geographicHeightMetres));
        var developed = land * (1d - highland) * SmoothStep(.78d, .94d,
            .5d + .5d * PlanetaryProceduralMath.WrappedSin(point, ClimateAxisB, 145_000d, .91d)) * .18d;

        Span<double> raw = stackalloc double[10];
        raw[(int)PlanetarySurfaceBiome.OceanCoastal] = 1d - land + land * coast * .18d;
        raw[(int)PlanetarySurfaceBiome.BeachSand] = land * coast * (1d - .55d * wet) * (1d - snowGate);
        raw[(int)PlanetarySurfaceBiome.Wetland] = land * wet * (1d - .6d * highland);
        raw[(int)PlanetarySurfaceBiome.GrassRolling] = land * moisture * temperature * (1d - coast) * (1d - highland) * (1d - snowGate);
        raw[(int)PlanetarySurfaceBiome.ScrubDry] = land * (1d - Math.Abs(moisture - .38d) * 1.8d) * temperature * (1d - .7d * highland) * (1d - coast);
        raw[(int)PlanetarySurfaceBiome.Desert] = land * SmoothStep(.48d, .82d, aridity) * (1d - highland) * (1d - coast) * (1d - snowGate);
        raw[(int)PlanetarySurfaceBiome.RockyMountain] = land * highland * (1d - .55d * snowGate);
        raw[(int)PlanetarySurfaceBiome.Alpine] = land * alpineGate * (1d - snowGate);
        raw[(int)PlanetarySurfaceBiome.SnowGlacial] = land * snowGate * (.35d + .65d * highland);
        raw[(int)PlanetarySurfaceBiome.DevelopedReserved] = developed;
        for (var index = 0; index < raw.Length; index++) raw[index] = Math.Max(raw[index], 0d);

        Span<int> selected = stackalloc int[MaximumContributors];
        selected.Fill(-1);
        for (var slot = 0; slot < MaximumContributors; slot++)
        {
            var best = -1; var bestWeight = double.NegativeInfinity;
            for (var index = 0; index < raw.Length; index++)
            {
                var alreadySelected = false;
                for (var previous = 0; previous < slot; previous++) alreadySelected |= selected[previous] == index;
                if (!alreadySelected && raw[index] > bestWeight) { best = index; bestWeight = raw[index]; }
            }
            selected[slot] = best;
        }
        var total = raw[selected[0]] + raw[selected[1]] + raw[selected[2]] + raw[selected[3]];
        if (!(total > 1e-15d)) { selected[0] = (int)PlanetarySurfaceBiome.ScrubDry; total = 1d; raw[selected[0]] = 1d; }
        var inverse = 1d / total;
        var first = new PlanetaryBiomeContribution((PlanetarySurfaceBiome)selected[0], raw[selected[0]] * inverse);
        var second = new PlanetaryBiomeContribution((PlanetarySurfaceBiome)selected[1], raw[selected[1]] * inverse);
        var third = new PlanetaryBiomeContribution((PlanetarySurfaceBiome)selected[2], raw[selected[2]] * inverse);
        var fourth = new PlanetaryBiomeContribution((PlanetarySurfaceBiome)selected[3], raw[selected[3]] * inverse);
        var rolling = Weight(first, second, third, fourth, PlanetarySurfaceBiome.GrassRolling) +
            .45d * Weight(first, second, third, fourth, PlanetarySurfaceBiome.ScrubDry);
        var rocky = (Weight(first, second, third, fourth, PlanetarySurfaceBiome.RockyMountain) +
            .65d * Weight(first, second, third, fourth, PlanetarySurfaceBiome.Alpine)) * SmoothStep(220d, 1_800d, geographicHeightMetres);
        var desert = Weight(first, second, third, fourth, PlanetarySurfaceBiome.Desert);
        var coastal = Weight(first, second, third, fourth, PlanetarySurfaceBiome.BeachSand) +
            .7d * Weight(first, second, third, fourth, PlanetarySurfaceBiome.Wetland);
        var glacial = Weight(first, second, third, fourth, PlanetarySurfaceBiome.SnowGlacial) +
            .35d * Weight(first, second, third, fourth, PlanetarySurfaceBiome.Alpine);
        return new(first, second, third, fourth, Saturate(rolling), Saturate(rocky), Saturate(desert),
            Saturate(coastal), Saturate(glacial), land);
    }

    private static double Weight(in PlanetaryBiomeContribution a, in PlanetaryBiomeContribution b,
        in PlanetaryBiomeContribution c, in PlanetaryBiomeContribution d, PlanetarySurfaceBiome biome) =>
        (a.Biome == biome ? a.Weight : 0d) + (b.Biome == biome ? b.Weight : 0d) +
        (c.Biome == biome ? c.Weight : 0d) + (d.Biome == biome ? d.Weight : 0d);
    private static double Saturate(double value) => Math.Clamp(value, 0d, 1d);
    private static double SmoothStep(double start, double end, double value)
    { var t = Saturate((value - start) / (end - start)); return t * t * (3d - 2d * t); }
}

internal static class PlanetaryProceduralMath
{
    internal static double WrappedPhase(double coordinateMetres, double wavelengthMetres, double phaseRadians)
    {
        var cell = Math.Floor(coordinateMetres / wavelengthMetres);
        var local = coordinateMetres - cell * wavelengthMetres;
        return local * (Math.Tau / wavelengthMetres) + phaseRadians;
    }

    internal static double WrappedSin(in Double3 point, in Double3 axis, double wavelengthMetres, double phaseRadians) =>
        Sin(WrappedPhase(Double3.Dot(point, axis), wavelengthMetres, phaseRadians));
    internal static double WrappedCos(in Double3 point, in Double3 axis, double wavelengthMetres, double phaseRadians) =>
        Cos(WrappedPhase(Double3.Dot(point, axis), wavelengthMetres, phaseRadians));

    internal static double Sin(double value)
    {
        value -= Math.Floor((value + Math.PI) / Math.Tau) * Math.Tau;
        var square = value * value;
        return value * (1d + square * (-.16666666666666666666666666666667d +
            square * (.00833333333333333333333333333333d +
            square * (-.00019841269841269841269841269841d +
            square * (.00000275573192239858906525573192d +
            square * (-.00000002505210838544171877505211d +
            square * (.00000000016059043836821614599392d +
            square * (-.00000000000076471637318198164759d))))))));
    }

    internal static double Cos(double value) => Sin(value + Math.PI * .5d);
}
