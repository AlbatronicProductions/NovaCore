using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

public enum PlanetaryTerrainModifierType : uint
{
    TiledDetail = 1,
    ErosionLike = 2,
    GeographicDecal = 3,
    RollingGrassland = 4,
    RockyMountain = 5,
    DesertDunes = 6,
    CoastalWetland = 7,
    SnowGlacial = 8,
    NearMaterial = 9
}

public enum PlanetaryTerrainModifierScope : uint
{
    Global = 1,
    GeographicFootprint = 2
}

public enum PlanetaryTerrainModifierOrder : uint
{
    BroadShaping = 100,
    TiledPhysicalDetail = 200,
    RollingPhysicalDetail = 210,
    RockyPhysicalDetail = 220,
    DesertPhysicalDetail = 230,
    CoastalPhysicalDetail = 240,
    GlacialPhysicalDetail = 250,
    BoundedGeographicDetail = 300,
    AuthoredCorrection = 400,
    NearPhysicalMaterial = 500
}

/// <summary>
/// Stable physical identity for a modifier. Camera, frame, mesh, owner,
/// residency and allocation state are deliberately absent.
/// </summary>
public readonly record struct PlanetaryTerrainModifierId(
    ulong BodyId,
    uint TerrainVersion,
    uint SchemaVersion,
    PlanetaryTerrainModifierType Type,
    uint Seed,
    ulong SourceAssetIdentity)
{
    public bool IsValid => BodyId != 0 && TerrainVersion != 0 && SchemaVersion != 0 &&
        Type is >= PlanetaryTerrainModifierType.TiledDetail and <= PlanetaryTerrainModifierType.NearMaterial;
}

/// <summary>Topology-neutral body-fixed footprint with a C2 radial falloff.</summary>
public readonly record struct PlanetaryTerrainModifierFootprint(
    Double3 CenterDirection,
    double RadiusMetres)
{
    public bool IsValid => CenterDirection.IsFinite &&
        Math.Abs(CenterDirection.LengthSquared - 1d) <= 1e-12d &&
        double.IsFinite(RadiusMetres) && RadiusMetres > 0d;
}

public readonly record struct PlanetaryTerrainModifierDefinition(
    PlanetaryTerrainModifierId Id,
    PlanetaryTerrainModifierScope Scope,
    PlanetaryTerrainModifierOrder Order,
    double AmplitudeMetres,
    double WavelengthMetres,
    double PhaseRadians,
    PlanetaryTerrainModifierFootprint Footprint)
{
    public bool IsValid => Id.IsValid &&
        Scope is PlanetaryTerrainModifierScope.Global or PlanetaryTerrainModifierScope.GeographicFootprint &&
        double.IsFinite(AmplitudeMetres) && AmplitudeMetres >= 0d &&
        double.IsFinite(WavelengthMetres) && WavelengthMetres > 0d &&
        double.IsFinite(PhaseRadians) &&
        (Scope == PlanetaryTerrainModifierScope.Global || Footprint.IsValid);
}

public readonly record struct PlanetaryTerrainModifierSample(
    double TiledHeightMetres,
    double ErosionHeightMetres,
    double MesoHeightMetres,
    double NearHeightMetres,
    double EastGradient,
    double NorthGradient,
    double GeographicWeight,
    PlanetaryTerrainModifierType DominantType,
    PlanetaryBiomeBlend Biomes)
{
    public double BaseHeightMetres => TiledHeightMetres + ErosionHeightMetres + MesoHeightMetres;
    public double HeightMetres => BaseHeightMetres + NearHeightMetres;
    public double NearEastGradient { get; init; }
    public double NearNorthGradient { get; init; }
    public bool IsFinite => double.IsFinite(TiledHeightMetres) && double.IsFinite(ErosionHeightMetres) &&
        double.IsFinite(MesoHeightMetres) && double.IsFinite(NearHeightMetres) &&
        double.IsFinite(EastGradient) && double.IsFinite(NorthGradient) &&
        double.IsFinite(NearEastGradient) && double.IsFinite(NearNorthGradient) &&
        double.IsFinite(GeographicWeight) && (Biomes.IsFinite ||
            (Biomes.TotalWeight == 0d && TiledHeightMetres == 0d && ErosionHeightMetres == 0d &&
             MesoHeightMetres == 0d && NearHeightMetres == 0d));
}

public readonly record struct PlanetaryPhysicalSurfaceSample(
    double BaseHeightMetres,
    PlanetaryTerrainModifierSample Modifiers,
    double FinalHeightMetres,
    double EastGradient,
    double NorthGradient,
    Double3 PhysicalNormal)
{
    public double ModifierHeightMetres => Modifiers.HeightMetres;
    public bool IsFinite => double.IsFinite(BaseHeightMetres) && Modifiers.IsFinite &&
        double.IsFinite(FinalHeightMetres) && double.IsFinite(EastGradient) &&
        double.IsFinite(NorthGradient) && PhysicalNormal.IsFinite;
}

/// <summary>One bounded physical-displacement frequency in the canonical M12 surface.</summary>
public readonly record struct PlanetaryPhysicalFrequencyBand(
    PlanetaryTerrainModifierType Type,
    double WavelengthMetres,
    double MaximumAmplitudeMetres);

/// <summary>
/// Resolution authority supplied by a concrete geometry representation.  The
/// underlying body-fixed modifier field remains canonical; this context only
/// decides which frequencies that geometry can sample without aliasing.
/// TransitionDistanceMetres is measured inward from a shared coarse/fine edge.
/// </summary>
public readonly record struct PlanetaryPhysicalFrequencyContext(
    double SamplingSpacingMetres,
    double BoundarySamplingSpacingMetres,
    double TransitionDistanceMetres)
{
    public static PlanetaryPhysicalFrequencyContext FullResolution => new(0d, 0d, double.PositiveInfinity);
    public bool IsValid => double.IsFinite(SamplingSpacingMetres) && SamplingSpacingMetres >= 0d &&
        double.IsFinite(BoundarySamplingSpacingMetres) && BoundarySamplingSpacingMetres >= 0d &&
        !double.IsNaN(TransitionDistanceMetres) && TransitionDistanceMetres >= 0d;

    public double Weight(double wavelengthMetres)
    {
        if (!IsValid || !double.IsFinite(wavelengthMetres) || wavelengthMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        if (SamplingSpacingMetres == 0d) return 1d;
        var fine = Representability(wavelengthMetres, SamplingSpacingMetres);
        var coarse = Representability(wavelengthMetres, BoundarySamplingSpacingMetres);
        if (TransitionDistanceMetres == double.PositiveInfinity || Math.Abs(fine - coarse) <= 1e-15d)
            return fine;
        var guardWidth = Math.Max(wavelengthMetres, 4d * SamplingSpacingMetres);
        var guard = SmoothStep(0d, guardWidth, TransitionDistanceMetres);
        return coarse + (fine - coarse) * guard;
    }

    public static double Representability(double wavelengthMetres, double samplingSpacingMetres)
    {
        if (samplingSpacingMetres <= 0d) return 1d;
        return SmoothStep(4d * samplingSpacingMetres, 8d * samplingSpacingMetres, wavelengthMetres);
    }

    public static double PatchExtentMetres(int level, double bodyRadiusMetres)
    {
        if (level is < 0 or > 24 || !double.IsFinite(bodyRadiusMetres) || bodyRadiusMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        return bodyRadiusMetres * (Math.PI * .5d) / (1L << level);
    }

    public static double PatchSpacingMetres(int level, int edgeSegments, double bodyRadiusMetres)
    {
        if (edgeSegments <= 0) throw new ArgumentOutOfRangeException(nameof(edgeSegments));
        return PatchExtentMetres(level, bodyRadiusMetres) / edgeSegments;
    }

    private static double SmoothStep(double start, double end, double value)
    {
        var t = Math.Clamp((value - start) / (end - start), 0d, 1d);
        return t * t * (3d - 2d * t);
    }
}

public enum PlanetaryTerrainModifierGenerationState : byte
{
    Preparing,
    Complete,
    Authoritative,
    Incompatible
}

/// <summary>
/// One complete immutable modifier generation. Publication swaps the complete
/// ordered generation; individual modifiers never become visible separately.
/// </summary>
public sealed class PlanetaryTerrainModifierGeneration
{
    private readonly PlanetaryTerrainModifierDefinition[] _modifiers;

    public PlanetaryTerrainModifierGeneration(ulong generationId, ulong bodyId,
        uint terrainVersion, uint schemaVersion,
        ReadOnlySpan<PlanetaryTerrainModifierDefinition> modifiers)
    {
        GenerationId = generationId; BodyId = bodyId; TerrainVersion = terrainVersion;
        SchemaVersion = schemaVersion; _modifiers = modifiers.ToArray();
        State = Validate() ? PlanetaryTerrainModifierGenerationState.Complete :
            PlanetaryTerrainModifierGenerationState.Incompatible;
        DeterministicHash = ComputeHash();
    }

    public ulong GenerationId { get; }
    public ulong BodyId { get; }
    public uint TerrainVersion { get; }
    public uint SchemaVersion { get; }
    public PlanetaryTerrainModifierGenerationState State { get; internal set; }
    public ulong DeterministicHash { get; }
    public ReadOnlySpan<PlanetaryTerrainModifierDefinition> Modifiers => _modifiers;
    public bool IsComplete => State is PlanetaryTerrainModifierGenerationState.Complete or
        PlanetaryTerrainModifierGenerationState.Authoritative;

    public void BeginPreparation()
    {
        if (State != PlanetaryTerrainModifierGenerationState.Complete)
            throw new InvalidOperationException("Only a complete compatible generation can be delayed.");
        State = PlanetaryTerrainModifierGenerationState.Preparing;
    }

    public bool TryCompletePreparation()
    {
        if (State != PlanetaryTerrainModifierGenerationState.Preparing || !Validate()) return false;
        State = PlanetaryTerrainModifierGenerationState.Complete; return true;
    }

    private bool Validate()
    {
        if (GenerationId == 0 || BodyId == 0 || TerrainVersion == 0 || SchemaVersion == 0 || _modifiers.Length == 0)
            return false;
        var previous = 0u;
        foreach (var modifier in _modifiers)
        {
            if (!modifier.IsValid || modifier.Id.BodyId != BodyId || modifier.Id.TerrainVersion != TerrainVersion ||
                modifier.Id.SchemaVersion != SchemaVersion || (uint)modifier.Order <= previous) return false;
            previous = (uint)modifier.Order;
        }
        return true;
    }

    private ulong ComputeHash()
    {
        var hash = 1469598103934665603ul;
        static ulong Add(ulong value, ulong next) { value ^= next; return value * 1099511628211ul; }
        hash = Add(hash, GenerationId); hash = Add(hash, BodyId); hash = Add(hash, TerrainVersion);
        hash = Add(hash, SchemaVersion);
        foreach (var value in _modifiers)
        {
            hash = Add(hash, (ulong)value.Id.Type); hash = Add(hash, value.Id.Seed);
            hash = Add(hash, (ulong)value.Order); hash = Add(hash, (ulong)value.Scope);
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.AmplitudeMetres));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.WavelengthMetres));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.PhaseRadians));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Footprint.CenterDirection.X));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Footprint.CenterDirection.Y));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Footprint.CenterDirection.Z));
            hash = Add(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Footprint.RadiusMetres));
            hash = Add(hash, value.Id.SourceAssetIdentity);
        }
        return hash;
    }
}

public sealed class PlanetaryTerrainModifierPublication
{
    public PlanetaryTerrainModifierPublication(PlanetaryTerrainModifierGeneration initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (!initial.IsComplete) throw new ArgumentException("The initial modifier generation is incomplete.", nameof(initial));
        initial.State = PlanetaryTerrainModifierGenerationState.Authoritative;
        Authoritative = initial;
    }

    public PlanetaryTerrainModifierGeneration Authoritative { get; private set; }

    public bool TryPublish(PlanetaryTerrainModifierGeneration candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!candidate.IsComplete || candidate.BodyId != Authoritative.BodyId ||
            candidate.TerrainVersion != Authoritative.TerrainVersion ||
            candidate.SchemaVersion != Authoritative.SchemaVersion ||
            candidate.GenerationId <= Authoritative.GenerationId) return false;
        Authoritative.State = PlanetaryTerrainModifierGenerationState.Complete;
        candidate.State = PlanetaryTerrainModifierGenerationState.Authoritative;
        Authoritative = candidate; return true;
    }
}

/// <summary>Canonical NovaCore M12B planet-wide physical-surface evaluator.</summary>
public static class PlanetaryPhysicalSurface
{
    public const ulong EarthBodyId = 6;
    public const uint ModifierSchemaVersion = 2;
    public const ulong ModifierGenerationId = 3;
    public const double EarthReferenceRadiusMetres = 6_371_008.8d;
    public const double PhysicalNormalSampleRadiusMetres = 9_774.0d;
    public const double TiledAmplitudeMetres = 8d;
    public const double TiledWavelengthMetres = 2_500_000d;
    public const uint TiledSeed = 0x7A11D3u;
    public const double RollingMaximumAmplitudeMetres = 18d;
    public const double RockyMaximumAmplitudeMetres = 65d;
    public const double DesertMaximumAmplitudeMetres = 7d;
    public const double CoastalMaximumAmplitudeMetres = 2d;
    public const double GlacialMaximumAmplitudeMetres = 14d;
    public const double NearMaximumAmplitudeMetres = .9d;
    public const double ErosionAmplitudeMetres = 1.5d;
    public const double ErosionWavelengthMetres = 1_200d;
    public const double ErosionFootprintRadiusMetres = 24_000d;
    public const double LaunchReservationRadiusMetres = 275d;
    public const double LaunchReservationTransitionMetres = 125d;
    public const uint ErosionSeed = 0xE20510u;

    public static ReadOnlySpan<PlanetaryPhysicalFrequencyBand> FrequencyBands => PhysicalFrequencyBands;
    private static readonly PlanetaryPhysicalFrequencyBand[] PhysicalFrequencyBands =
    [
        new(PlanetaryTerrainModifierType.TiledDetail, 2_500_000d, 4d),
        new(PlanetaryTerrainModifierType.TiledDetail, 1_825_000d, 2.4d),
        new(PlanetaryTerrainModifierType.TiledDetail, 1_025_000d, 1.6d),
        new(PlanetaryTerrainModifierType.RollingGrassland, 18_000d, 10.44d),
        new(PlanetaryTerrainModifierType.RollingGrassland, 2_700d, 5.22d),
        new(PlanetaryTerrainModifierType.RollingGrassland, 360d, 2.34d),
        new(PlanetaryTerrainModifierType.RockyMountain, 12_000d, 33.8d),
        new(PlanetaryTerrainModifierType.RockyMountain, 1_850d, 20.15d),
        new(PlanetaryTerrainModifierType.RockyMountain, 190d, 11.05d),
        new(PlanetaryTerrainModifierType.DesertDunes, 1_400d, 4.34d),
        new(PlanetaryTerrainModifierType.DesertDunes, 310d, 1.82d),
        new(PlanetaryTerrainModifierType.DesertDunes, 64d, .84d),
        new(PlanetaryTerrainModifierType.CoastalWetland, 2_800d, 1.36d),
        new(PlanetaryTerrainModifierType.CoastalWetland, 260d, .64d),
        new(PlanetaryTerrainModifierType.SnowGlacial, 7_000d, 8.96d),
        new(PlanetaryTerrainModifierType.SnowGlacial, 840d, 3.5d),
        new(PlanetaryTerrainModifierType.SnowGlacial, 120d, 1.54d),
        new(PlanetaryTerrainModifierType.ErosionLike, 1_200d, 1.5d),
        new(PlanetaryTerrainModifierType.NearMaterial, 32d, .558d),
        new(PlanetaryTerrainModifierType.NearMaterial, 7d, .243d),
        new(PlanetaryTerrainModifierType.NearMaterial, 1.4d, .099d)
    ];

    private static readonly Double3 TiledAxisA = new(.8728715609439696d, .4364357804719848d, -.2182178902359924d);
    private static readonly Double3 TiledAxisB = new(-.1690308509457033d, .50709255283711d, .8451542547285166d);
    private static readonly Double3 TiledAxisC = new(.3903600291794133d, -.6506000486323555d, .6506000486323555d);
    private static readonly Double3 DetailAxisA = new(.7715167498104595d, -.1543033499620919d, .6172133998483676d);
    private static readonly Double3 DetailAxisB = new(-.3244428422615251d, .8111071056538127d, .4866642633922876d);
    private static readonly Double3 DetailAxisC = new(.1690308509457033d, .8451542547285166d, -.50709255283711d);
    private static readonly Double3 FloridaDirection = BodyFixedGeography.DirectionFromLatitudeLongitude(
        FloridaLaunchSite.Latitude * Math.PI / 180d, FloridaLaunchSite.Longitude * Math.PI / 180d);
    private static readonly PlanetarySurfaceFrame FloridaFrame = PlanetarySurfaceFrame.AtDirection(FloridaDirection);
    private static readonly PlanetaryTerrainModifierDefinition[] EarthDefinitions =
    [
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.TiledDetail,
                TiledSeed, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.TiledPhysicalDetail, TiledAmplitudeMetres,
            TiledWavelengthMetres, .713d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.RollingGrassland,
                0xA11011u, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.RollingPhysicalDetail, RollingMaximumAmplitudeMetres, 18_000d, .31d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.RockyMountain,
                0xA11012u, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.RockyPhysicalDetail, RockyMaximumAmplitudeMetres, 12_000d, 1.17d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.DesertDunes,
                0xA11013u, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.DesertPhysicalDetail, DesertMaximumAmplitudeMetres, 1_400d, 2.41d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.CoastalWetland,
                0xA11014u, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.CoastalPhysicalDetail, CoastalMaximumAmplitudeMetres, 2_800d, -.83d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.SnowGlacial,
                0xA11015u, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.GlacialPhysicalDetail, GlacialMaximumAmplitudeMetres, 7_000d, .67d, default),
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.ErosionLike,
                ErosionSeed, 0), PlanetaryTerrainModifierScope.GeographicFootprint,
            PlanetaryTerrainModifierOrder.BoundedGeographicDetail, ErosionAmplitudeMetres,
            ErosionWavelengthMetres, 1.137d,
            new(FloridaDirection, ErosionFootprintRadiusMetres))
    ];

    public static PlanetaryTerrainModifierGeneration EarthGeneration { get; } =
        new(ModifierGenerationId, EarthBodyId, 5, ModifierSchemaVersion, EarthDefinitions);

    public static PlanetaryPhysicalSurfaceSample Evaluate(in PlanetaryTerrainDefinition terrain,
        in Double3 bodyFixedDirection)
    {
        if (!terrain.IsValid || !bodyFixedDirection.IsFinite || bodyFixedDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException();
        var direction = bodyFixedDirection.Normalized();
        var baseHeight = terrain.SampleBaseHeight(direction);
        if (terrain.SourceId != PlanetaryTerrainDefinition.EarthProductionCubeV5.SourceId ||
            terrain.Version != PlanetaryTerrainDefinition.EarthProductionCubeV5.Version)
            return new(baseHeight, default, baseHeight, 0d, 0d, direction);

        var modifiers = EvaluateModifiers(direction, baseHeight);
        var finalHeight = Math.Max(0d,
            Math.Max(0d, baseHeight + modifiers.BaseHeightMetres) + modifiers.NearHeightMetres);
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var angle = PhysicalNormalSampleRadiusMetres / EarthReferenceRadiusMetres;
        var leftDirection = (direction - frame.East * angle).Normalized();
        var rightDirection = (direction + frame.East * angle).Normalized();
        var downDirection = (direction - frame.North * angle).Normalized();
        var upDirection = (direction + frame.North * angle).Normalized();
        var leftHeight = EvaluateBaseHeightNoGradient(terrain, leftDirection);
        var rightHeight = EvaluateBaseHeightNoGradient(terrain, rightDirection);
        var downHeight = EvaluateBaseHeightNoGradient(terrain, downDirection);
        var upHeight = EvaluateBaseHeightNoGradient(terrain, upDirection);
        var eastGradient = (rightHeight - leftHeight) / (2d * PhysicalNormalSampleRadiusMetres) + modifiers.NearEastGradient;
        var northGradient = (upHeight - downHeight) / (2d * PhysicalNormalSampleRadiusMetres) + modifiers.NearNorthGradient;
        var left = leftDirection * (EarthReferenceRadiusMetres + leftHeight);
        var right = rightDirection * (EarthReferenceRadiusMetres + rightHeight);
        var down = downDirection * (EarthReferenceRadiusMetres + downHeight);
        var up = upDirection * (EarthReferenceRadiusMetres + upHeight);
        var baseNormal = Double3.Cross(right - left, up - down).Normalized();
        if (Double3.Dot(baseNormal, direction) < 0d) baseNormal = -baseNormal;
        var radialComponent = Math.Max(Double3.Dot(baseNormal, direction), 1e-9d);
        var baseEastSlope = -Double3.Dot(baseNormal, frame.East) / radialComponent;
        var baseNorthSlope = -Double3.Dot(baseNormal, frame.North) / radialComponent;
        var normal = (direction - frame.East * (baseEastSlope + modifiers.NearEastGradient) -
            frame.North * (baseNorthSlope + modifiers.NearNorthGradient)).Normalized();
        return new(baseHeight, modifiers, finalHeight, eastGradient, northGradient, normal);
    }

    public static PlanetaryTerrainModifierSample EvaluateModifiers(in Double3 bodyFixedDirection)
    {
        var direction = bodyFixedDirection.Normalized();
        return EvaluateModifiers(direction, PlanetaryTerrainDefinition.EarthProductionCubeV5.SampleBaseHeight(direction));
    }

    public static PlanetaryTerrainModifierSample EvaluateModifiers(in Double3 bodyFixedDirection, double geographicHeightMetres)
        => EvaluateModifiers(bodyFixedDirection, geographicHeightMetres,
            PlanetaryPhysicalFrequencyContext.FullResolution);

    public static PlanetaryTerrainModifierSample EvaluateModifiers(in Double3 bodyFixedDirection,
        double geographicHeightMetres, in PlanetaryPhysicalFrequencyContext frequency)
    {
        if (!frequency.IsValid) throw new ArgumentOutOfRangeException(nameof(frequency));
        var direction = bodyFixedDirection.Normalized(); var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var point = direction * EarthReferenceRadiusMetres;
        var biomes = PlanetaryBiomeControlAuthority.Sample(direction, geographicHeightMetres);
        var tiledHeight = LimitedBand(point, TiledAxisA, TiledWavelengthMetres, .713d, TiledAmplitudeMetres * .5d, frequency, out var tiledGradient) +
            LimitedBand(point, TiledAxisB, TiledWavelengthMetres * .73d, 2.113d, TiledAmplitudeMetres * .3d, frequency, out var gradient) +
            LimitedBand(point, TiledAxisC, TiledWavelengthMetres * .41d, -1.271d, TiledAmplitudeMetres * .2d, frequency, out var nextGradient);
        tiledGradient += gradient + nextGradient;

        var rolling = biomes.RollingEligibility * (
            LimitedBand(point, DetailAxisA, 18_000d, .31d, RollingMaximumAmplitudeMetres * .58d, frequency, out var rollingGradient) +
            LimitedBand(point, DetailAxisB, 2_700d, 1.73d, RollingMaximumAmplitudeMetres * .29d, frequency, out gradient) +
            LimitedBand(point, DetailAxisC, 360d, -.61d, RollingMaximumAmplitudeMetres * .13d, frequency, out nextGradient));
        rollingGradient = (rollingGradient + gradient + nextGradient) * biomes.RollingEligibility;
        var rocky = biomes.RockyEligibility * (
            LimitedBand(point, DetailAxisC, 12_000d, 1.17d, RockyMaximumAmplitudeMetres * .52d, frequency, out var rockyGradient) +
            LimitedBand(point, TiledAxisA, 1_850d, -2.03d, RockyMaximumAmplitudeMetres * .31d, frequency, out gradient) +
            LimitedBand(point, DetailAxisB, 190d, .44d, RockyMaximumAmplitudeMetres * .17d, frequency, out nextGradient));
        rockyGradient = (rockyGradient + gradient + nextGradient) * biomes.RockyEligibility;
        var desert = biomes.DesertEligibility * (
            LimitedBand(point, DetailAxisB, 1_400d, 2.41d, DesertMaximumAmplitudeMetres * .62d, frequency, out var desertGradient) +
            LimitedBand(point, DetailAxisA, 310d, -.37d, DesertMaximumAmplitudeMetres * .26d, frequency, out gradient) +
            LimitedBand(point, TiledAxisC, 64d, 1.61d, DesertMaximumAmplitudeMetres * .12d, frequency, out nextGradient));
        desertGradient = (desertGradient + gradient + nextGradient) * biomes.DesertEligibility;
        var coastal = biomes.CoastalEligibility * (
            LimitedBand(point, TiledAxisB, 2_800d, -.83d, CoastalMaximumAmplitudeMetres * .68d, frequency, out var coastalGradient) +
            LimitedBand(point, DetailAxisC, 260d, 2.63d, CoastalMaximumAmplitudeMetres * .32d, frequency, out gradient));
        coastalGradient = (coastalGradient + gradient) * biomes.CoastalEligibility;
        var glacial = biomes.GlacialEligibility * (
            LimitedBand(point, DetailAxisA, 7_000d, .67d, GlacialMaximumAmplitudeMetres * .64d, frequency, out var glacialGradient) +
            LimitedBand(point, TiledAxisC, 840d, -1.91d, GlacialMaximumAmplitudeMetres * .25d, frequency, out gradient) +
            LimitedBand(point, DetailAxisB, 120d, 2.87d, GlacialMaximumAmplitudeMetres * .11d, frequency, out nextGradient));
        glacialGradient = (glacialGradient + gradient + nextGradient) * biomes.GlacialEligibility;
        var mesoHeight = rolling + rocky + desert + coastal + glacial;
        var mesoGradient = rollingGradient + rockyGradient + desertGradient + coastalGradient + glacialGradient;

        var nearAmplitude = NearMaximumAmplitudeMetres * biomes.MaterialEligibility * Math.Clamp(
            .17d * biomes.Weight(PlanetarySurfaceBiome.GrassRolling) +
            .10d * biomes.Weight(PlanetarySurfaceBiome.Wetland) +
            .12d * biomes.Weight(PlanetarySurfaceBiome.BeachSand) +
            .62d * biomes.Weight(PlanetarySurfaceBiome.Desert) +
            .92d * biomes.Weight(PlanetarySurfaceBiome.RockyMountain) +
            .74d * biomes.Weight(PlanetarySurfaceBiome.Alpine) +
            .28d * biomes.Weight(PlanetarySurfaceBiome.SnowGlacial) +
            .14d * biomes.Weight(PlanetarySurfaceBiome.ScrubDry), 0d, 1d);
        // These formerly used globally coherent one-dimensional sine bands.  Their
        // fixed 32 m carrier direction produced the visible close-range ribs, with
        // the 7 m carrier making a crossing grid.  Keep the same bounded amplitude
        // budget, but warp each carrier in body-fixed metres using two slower,
        // decorrelated directions so neither material nor physical relief acquires
        // a planet-spanning straight ridge.
        var nearHeight = LimitedWarpedBand(point, DetailAxisC, 32d, .53d, nearAmplitude * .62d, frequency,
            TiledAxisA, 210d, .83d, 1.71d, DetailAxisB, 73d, .31d, -.37d, out var nearGradient) +
            LimitedWarpedBand(point, DetailAxisA, 7d, -1.13d, nearAmplitude * .27d, frequency,
                DetailAxisB, 53d, .67d, -.19d, TiledAxisC, 19d, .24d, 2.03d, out gradient) +
            LimitedWarpedBand(point, DetailAxisB, 1.4d, 2.31d, nearAmplitude * .11d, frequency,
                DetailAxisC, 17d, .49d, .61d, DetailAxisA, 5d, .19d, -2.27d, out nextGradient);
        nearGradient += gradient + nextGradient;

        var delta = point - FloridaDirection * EarthReferenceRadiusMetres;
        var east = Double3.Dot(delta, FloridaFrame.East); var north = Double3.Dot(delta, FloridaFrame.North);
        var radius = Math.Sqrt(east * east + north * north); var weight = 0d; var dWeightDr = 0d;
        if (radius < ErosionFootprintRadiusMetres)
        {
            var q = 1d - radius / ErosionFootprintRadiusMetres;
            weight = q * q * q * (q * (q * 6d - 15d) + 10d);
            var derivativeQ = 30d * q * q * (q - 1d) * (q - 1d);
            dWeightDr = -derivativeQ / ErosionFootprintRadiusMetres;
        }
        var reservation = 1d; var dReservationDr = 0d;
        if (radius <= LaunchReservationRadiusMetres) reservation = 0d;
        else if (radius < LaunchReservationRadiusMetres + LaunchReservationTransitionMetres)
        {
            var t = (radius - LaunchReservationRadiusMetres) / LaunchReservationTransitionMetres;
            reservation = t * t * (3d - 2d * t);
            dReservationDr = 6d * t * (1d - t) / LaunchReservationTransitionMetres;
        }
        var erosionWave = Math.Tau / ErosionWavelengthMetres;
        var phase1 = erosionWave * (.78d * east + .6257795138864807d * north) + 1.137d;
        var phase2 = erosionWave * (-.35d * east + .9367496997597597d * north) - .443d;
        var carrier = .65d * PlanetaryProceduralMath.Sin(phase1) + .35d * PlanetaryProceduralMath.Sin(phase2) * PlanetaryProceduralMath.Sin(phase1 * .5d);
        var dCarrierEast = .65d * PlanetaryProceduralMath.Cos(phase1) * erosionWave * .78d +
            .35d * (PlanetaryProceduralMath.Cos(phase2) * erosionWave * -.35d * PlanetaryProceduralMath.Sin(phase1 * .5d) +
                PlanetaryProceduralMath.Sin(phase2) * PlanetaryProceduralMath.Cos(phase1 * .5d) * erosionWave * .39d);
        var dCarrierNorth = .65d * PlanetaryProceduralMath.Cos(phase1) * erosionWave * .6257795138864807d +
            .35d * (PlanetaryProceduralMath.Cos(phase2) * erosionWave * .9367496997597597d * PlanetaryProceduralMath.Sin(phase1 * .5d) +
                PlanetaryProceduralMath.Sin(phase2) * PlanetaryProceduralMath.Cos(phase1 * .5d) * erosionWave * .31288975694324035d);
        var radialEast = radius > 1e-9d ? east / radius : 0d;
        var radialNorth = radius > 1e-9d ? north / radius : 0d;
        var erosionFrequencyWeight = frequency.Weight(ErosionWavelengthMetres);
        var erosionHeight = ErosionAmplitudeMetres * weight * reservation * carrier * erosionFrequencyWeight;
        var radialDerivative = dWeightDr * reservation + weight * dReservationDr;
        var erosionEast = ErosionAmplitudeMetres * (weight * reservation * dCarrierEast + carrier * radialDerivative * radialEast) * erosionFrequencyWeight;
        var erosionNorth = ErosionAmplitudeMetres * (weight * reservation * dCarrierNorth + carrier * radialDerivative * radialNorth) * erosionFrequencyWeight;
        // The bounded modifier is defined in the fixed Florida ENU frame.  Its
        // analytic derivatives must be projected into the sample's local ENU
        // frame before they are combined with the global contribution.
        var erosionGradient = FloridaFrame.East * erosionEast + FloridaFrame.North * erosionNorth;
        var fullGradient = tiledGradient + mesoGradient + erosionGradient + nearGradient;
        var dominant = PlanetaryTerrainModifierType.TiledDetail; var dominantMagnitude = Math.Abs(tiledHeight);
        SelectDominant(PlanetaryTerrainModifierType.RollingGrassland, rolling, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.RockyMountain, rocky, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.DesertDunes, desert, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.CoastalWetland, coastal, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.SnowGlacial, glacial, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.ErosionLike, erosionHeight, ref dominant, ref dominantMagnitude);
        SelectDominant(PlanetaryTerrainModifierType.NearMaterial, nearHeight, ref dominant, ref dominantMagnitude);
        return new(tiledHeight, erosionHeight, mesoHeight, nearHeight,
            Double3.Dot(fullGradient, frame.East), Double3.Dot(fullGradient, frame.North), weight, dominant, biomes)
        {
            NearEastGradient = Double3.Dot(nearGradient, frame.East),
            NearNorthGradient = Double3.Dot(nearGradient, frame.North)
        };
    }

    public static double EvaluateFinalHeightNoGradient(in PlanetaryTerrainDefinition terrain,
        in Double3 bodyFixedDirection)
    {
        var baseHeight = terrain.SampleBaseHeight(bodyFixedDirection);
        var modifiers = EvaluateModifiers(bodyFixedDirection, baseHeight);
        return Math.Max(0d,
            Math.Max(0d, baseHeight + modifiers.BaseHeightMetres) + modifiers.NearHeightMetres);
    }

    public static double EvaluateBaseHeightNoGradient(in PlanetaryTerrainDefinition terrain,
        in Double3 bodyFixedDirection)
    {
        var baseHeight = terrain.SampleBaseHeight(bodyFixedDirection);
        return Math.Max(0d, baseHeight + EvaluateModifiers(bodyFixedDirection, baseHeight).BaseHeightMetres);
    }

    private static double Band(in Double3 point, in Double3 axis, double wavelength, double phase,
        double amplitude, out Double3 gradient)
    {
        var angle = PlanetaryProceduralMath.WrappedPhase(Double3.Dot(point, axis), wavelength, phase);
        gradient = axis * (amplitude * (Math.Tau / wavelength) * PlanetaryProceduralMath.Cos(angle));
        return amplitude * PlanetaryProceduralMath.Sin(angle);
    }

    private static double LimitedBand(in Double3 point, in Double3 axis, double wavelength, double phase,
        double amplitude, in PlanetaryPhysicalFrequencyContext frequency, out Double3 gradient)
    {
        var value = Band(point, axis, wavelength, phase, amplitude, out gradient);
        var weight = frequency.Weight(wavelength); gradient *= weight; return value * weight;
    }

    private static double WarpedBand(in Double3 point, in Double3 carrierAxis, double wavelength, double phase,
        double amplitude, in Double3 warpAxisA, double warpWavelengthA, double warpStrengthA, double warpPhaseA,
        in Double3 warpAxisB, double warpWavelengthB, double warpStrengthB, double warpPhaseB, out Double3 gradient)
    {
        var carrier = PlanetaryProceduralMath.WrappedPhase(Double3.Dot(point, carrierAxis), wavelength, phase);
        var warpA = PlanetaryProceduralMath.WrappedPhase(Double3.Dot(point, warpAxisA), warpWavelengthA, warpPhaseA);
        var warpB = PlanetaryProceduralMath.WrappedPhase(Double3.Dot(point, warpAxisB), warpWavelengthB, warpPhaseB);
        var angle = carrier + warpStrengthA * PlanetaryProceduralMath.Sin(warpA) +
            warpStrengthB * PlanetaryProceduralMath.Sin(warpB);
        var phaseGradient = carrierAxis * (Math.Tau / wavelength) +
            warpAxisA * (warpStrengthA * (Math.Tau / warpWavelengthA) * PlanetaryProceduralMath.Cos(warpA)) +
            warpAxisB * (warpStrengthB * (Math.Tau / warpWavelengthB) * PlanetaryProceduralMath.Cos(warpB));
        gradient = phaseGradient * (amplitude * PlanetaryProceduralMath.Cos(angle));
        return amplitude * PlanetaryProceduralMath.Sin(angle);
    }

    private static double LimitedWarpedBand(in Double3 point, in Double3 carrierAxis, double wavelength, double phase,
        double amplitude, in PlanetaryPhysicalFrequencyContext frequency,
        in Double3 warpAxisA, double warpWavelengthA, double warpStrengthA, double warpPhaseA,
        in Double3 warpAxisB, double warpWavelengthB, double warpStrengthB, double warpPhaseB, out Double3 gradient)
    {
        var value = WarpedBand(point, carrierAxis, wavelength, phase, amplitude,
            warpAxisA, warpWavelengthA, warpStrengthA, warpPhaseA,
            warpAxisB, warpWavelengthB, warpStrengthB, warpPhaseB, out gradient);
        var weight = frequency.Weight(wavelength); gradient *= weight; return value * weight;
    }

    private static void SelectDominant(PlanetaryTerrainModifierType type, double height,
        ref PlanetaryTerrainModifierType dominant, ref double dominantMagnitude)
    { var magnitude = Math.Abs(height); if (magnitude > dominantMagnitude) { dominant = type; dominantMagnitude = magnitude; } }
}

/// <summary>
/// Future geographic decal descriptor. The contract is body/geography based;
/// no mesh vertex or cache slot participates in identity.
/// </summary>
public readonly record struct PlanetaryTerrainDecalDescriptor(
    ulong BodyId,
    uint TerrainVersion,
    uint ModifierSchemaVersion,
    ulong DecalId,
    Double3 CenterDirection,
    double RadiusMetres,
    double FalloffMetres,
    int Priority,
    ulong HeightSourceIdentity);
