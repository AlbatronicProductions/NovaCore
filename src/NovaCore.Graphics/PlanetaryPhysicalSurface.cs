using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

public enum PlanetaryTerrainModifierType : uint
{
    TiledDetail = 1,
    ErosionLike = 2,
    GeographicDecal = 3
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
    BoundedGeographicDetail = 300,
    AuthoredCorrection = 400
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
        Type is >= PlanetaryTerrainModifierType.TiledDetail and <= PlanetaryTerrainModifierType.GeographicDecal;
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
    double EastGradient,
    double NorthGradient,
    double GeographicWeight,
    PlanetaryTerrainModifierType DominantType)
{
    public double HeightMetres => TiledHeightMetres + ErosionHeightMetres;
    public bool IsFinite => double.IsFinite(TiledHeightMetres) && double.IsFinite(ErosionHeightMetres) &&
        double.IsFinite(EastGradient) && double.IsFinite(NorthGradient) &&
        double.IsFinite(GeographicWeight);
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

/// <summary>Canonical NovaCore 11B-7H physical-surface evaluator.</summary>
public static class PlanetaryPhysicalSurface
{
    public const ulong EarthBodyId = 6;
    public const uint ModifierSchemaVersion = 1;
    public const ulong ModifierGenerationId = 1;
    public const double EarthReferenceRadiusMetres = 6_371_008.8d;
    public const double PhysicalNormalSampleRadiusMetres = 9_774.0d;
    public const double TiledAmplitudeMetres = 8d;
    public const double TiledWavelengthMetres = 2_500_000d;
    public const uint TiledSeed = 0x7A11D3u;
    public const double ErosionAmplitudeMetres = 10d;
    public const double ErosionWavelengthMetres = 40_000d;
    public const double ErosionFootprintRadiusMetres = 24_000d;
    public const uint ErosionSeed = 0xE20510u;

    private static readonly Double3 TiledAxisA = new(.8728715609439696d, .4364357804719848d, -.2182178902359924d);
    private static readonly Double3 TiledAxisB = new(-.1690308509457033d, .50709255283711d, .8451542547285166d);
    private static readonly Double3 TiledAxisC = new(.3903600291794133d, -.6506000486323555d, .6506000486323555d);
    private static readonly Double3 FloridaDirection = BodyFixedGeography.DirectionFromLatitudeLongitude(
        FloridaLaunchSite.Latitude * Math.PI / 180d, FloridaLaunchSite.Longitude * Math.PI / 180d);
    private static readonly PlanetarySurfaceFrame FloridaFrame = PlanetarySurfaceFrame.AtDirection(FloridaDirection);
    private static readonly PlanetaryTerrainModifierDefinition[] EarthDefinitions =
    [
        new(new(EarthBodyId, 5, ModifierSchemaVersion, PlanetaryTerrainModifierType.TiledDetail,
                TiledSeed, 0), PlanetaryTerrainModifierScope.Global,
            PlanetaryTerrainModifierOrder.TiledPhysicalDetail, TiledAmplitudeMetres,
            TiledWavelengthMetres, .713d, default),
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

        var modifiers = EvaluateModifiers(direction);
        var finalHeight = Math.Max(0d, baseHeight + modifiers.HeightMetres);
        var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var angle = PhysicalNormalSampleRadiusMetres / EarthReferenceRadiusMetres;
        var leftDirection = (direction - frame.East * angle).Normalized();
        var rightDirection = (direction + frame.East * angle).Normalized();
        var downDirection = (direction - frame.North * angle).Normalized();
        var upDirection = (direction + frame.North * angle).Normalized();
        var leftHeight = EvaluateFinalHeightNoGradient(terrain, leftDirection);
        var rightHeight = EvaluateFinalHeightNoGradient(terrain, rightDirection);
        var downHeight = EvaluateFinalHeightNoGradient(terrain, downDirection);
        var upHeight = EvaluateFinalHeightNoGradient(terrain, upDirection);
        var eastGradient = (rightHeight - leftHeight) / (2d * PhysicalNormalSampleRadiusMetres);
        var northGradient = (upHeight - downHeight) / (2d * PhysicalNormalSampleRadiusMetres);
        var left = leftDirection * (EarthReferenceRadiusMetres + leftHeight);
        var right = rightDirection * (EarthReferenceRadiusMetres + rightHeight);
        var down = downDirection * (EarthReferenceRadiusMetres + downHeight);
        var up = upDirection * (EarthReferenceRadiusMetres + upHeight);
        var normal = Double3.Cross(right - left, up - down).Normalized();
        if (Double3.Dot(normal, direction) < 0d) normal = -normal;
        return new(baseHeight, modifiers, finalHeight, eastGradient, northGradient, normal);
    }

    public static PlanetaryTerrainModifierSample EvaluateModifiers(in Double3 bodyFixedDirection)
    {
        var direction = bodyFixedDirection.Normalized(); var frame = PlanetarySurfaceFrame.AtDirection(direction);
        var point = direction * EarthReferenceRadiusMetres;
        var wave = Math.Tau / TiledWavelengthMetres;
        var a = wave * Double3.Dot(point, TiledAxisA) + .713d;
        var b = wave * Double3.Dot(point, TiledAxisB) + 2.113d;
        var c = wave * Double3.Dot(point, TiledAxisC) - 1.271d;
        var tiledHeight = TiledAmplitudeMetres *
            (.5d * DeterministicSin(a) + .3d * DeterministicSin(b) + .2d * DeterministicSin(c));
        var tiledGradient = TiledAxisA * (TiledAmplitudeMetres * .5d * wave * DeterministicCos(a)) +
            TiledAxisB * (TiledAmplitudeMetres * .3d * wave * DeterministicCos(b)) +
            TiledAxisC * (TiledAmplitudeMetres * .2d * wave * DeterministicCos(c));

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
        var erosionWave = Math.Tau / ErosionWavelengthMetres;
        var phase1 = erosionWave * (.78d * east + .6257795138864807d * north) + 1.137d;
        var phase2 = erosionWave * (-.35d * east + .9367496997597597d * north) - .443d;
        var carrier = .65d * DeterministicSin(phase1) + .35d * DeterministicSin(phase2) * DeterministicSin(phase1 * .5d);
        var dCarrierEast = .65d * DeterministicCos(phase1) * erosionWave * .78d +
            .35d * (DeterministicCos(phase2) * erosionWave * -.35d * DeterministicSin(phase1 * .5d) +
                DeterministicSin(phase2) * DeterministicCos(phase1 * .5d) * erosionWave * .39d);
        var dCarrierNorth = .65d * DeterministicCos(phase1) * erosionWave * .6257795138864807d +
            .35d * (DeterministicCos(phase2) * erosionWave * .9367496997597597d * DeterministicSin(phase1 * .5d) +
                DeterministicSin(phase2) * DeterministicCos(phase1 * .5d) * erosionWave * .31288975694324035d);
        var radialEast = radius > 1e-9d ? east / radius : 0d;
        var radialNorth = radius > 1e-9d ? north / radius : 0d;
        var erosionHeight = ErosionAmplitudeMetres * weight * carrier;
        var erosionEast = ErosionAmplitudeMetres * (weight * dCarrierEast + carrier * dWeightDr * radialEast);
        var erosionNorth = ErosionAmplitudeMetres * (weight * dCarrierNorth + carrier * dWeightDr * radialNorth);
        // The bounded modifier is defined in the fixed Florida ENU frame.  Its
        // analytic derivatives must be projected into the sample's local ENU
        // frame before they are combined with the global contribution.
        var erosionGradient = FloridaFrame.East * erosionEast + FloridaFrame.North * erosionNorth;
        var modifierEast = Double3.Dot(tiledGradient + erosionGradient, frame.East);
        var modifierNorth = Double3.Dot(tiledGradient + erosionGradient, frame.North);
        var dominant = Math.Abs(erosionHeight) > Math.Abs(tiledHeight)
            ? PlanetaryTerrainModifierType.ErosionLike : PlanetaryTerrainModifierType.TiledDetail;
        return new(tiledHeight, erosionHeight, modifierEast, modifierNorth, weight, dominant);
    }

    public static double EvaluateFinalHeightNoGradient(in PlanetaryTerrainDefinition terrain,
        in Double3 bodyFixedDirection) => Math.Max(0d,
            terrain.SampleBaseHeight(bodyFixedDirection) + EvaluateModifiers(bodyFixedDirection).HeightMetres);

    private static double DeterministicSin(double value)
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

    private static double DeterministicCos(double value) => DeterministicSin(value + Math.PI * .5d);
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
