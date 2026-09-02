using NovaCore.Core;
using System.Runtime.CompilerServices;

namespace NovaCore.Graphics;

/// <summary>Stable proof identities for the M12D-P2B natural terrain families.</summary>
public enum PlanetaryNaturalTerrainFamily : uint
{
    Grassland = 1,
    ScrubDry = 2,
    RockyMountain = 3,
    Alpine = 4,
    DesertDunes = 5,
    CoastWetland = 6,
    SnowGlacial = 7,
    GenericRemote = 8
}

public readonly record struct PlanetaryNaturalTerrainFamilyIdentity(
    ulong BodyId,
    ulong PhysicalFieldGeneration,
    uint Seed)
{
    public bool IsValid => BodyId != 0 && PhysicalFieldGeneration != 0;
}

public readonly record struct PlanetaryNaturalTerrainFamilySample(
    PlanetaryNaturalTerrainFamily Family,
    PlanetaryNaturalTerrainFieldSample Macro,
    PlanetaryNaturalTerrainFieldSample Meso,
    PlanetaryNaturalTerrainFieldSample Near,
    PlanetaryNaturalTerrainFieldSample Total,
    Double3 Orientation)
{
    public bool IsFinite => Macro.IsFinite && Meso.IsFinite && Near.IsFinite && Total.IsFinite && Orientation.IsFinite;
}

public readonly record struct PlanetaryNaturalTerrainCompositionSample(
    PlanetaryNaturalTerrainFamily FirstFamily,
    PlanetaryNaturalTerrainFamily SecondFamily,
    double SecondWeight,
    Double3 SecondWeightGradient,
    PlanetaryNaturalTerrainFieldSample Macro,
    PlanetaryNaturalTerrainFieldSample Meso,
    PlanetaryNaturalTerrainFieldSample Near,
    PlanetaryNaturalTerrainFieldSample Total)
{
    public bool IsFinite => double.IsFinite(SecondWeight) && SecondWeight is >= 0d and <= 1d &&
        SecondWeightGradient.IsFinite && Macro.IsFinite && Meso.IsFinite && Near.IsFinite && Total.IsFinite;
}

public readonly record struct PlanetaryNaturalTerrainFamilyBounds(
    double MacroHeight,
    double MesoHeight,
    double NearHeight,
    double TotalHeight,
    double TotalGradient)
{
    public bool IsFinite => double.IsFinite(MacroHeight) && double.IsFinite(MesoHeight) &&
        double.IsFinite(NearHeight) && double.IsFinite(TotalHeight) && double.IsFinite(TotalGradient);
}

public readonly record struct PlanetaryNaturalTerrainCellDescriptorSample(
    PlanetaryNaturalTerrainCell Cell,
    uint ControlHashX,
    uint ControlHashY,
    PlanetaryNaturalTerrainFamily FirstFamily,
    PlanetaryNaturalTerrainFamily SecondFamily,
    double SecondWeight,
    Double3 SecondWeightGradient,
    Double3 Orientation,
    PlanetaryNaturalTerrainFamilyBounds Bounds)
{
    public bool IsFinite => double.IsFinite(SecondWeight) && SecondWeightGradient.IsFinite &&
        Orientation.IsFinite && Bounds.IsFinite;
}

/// <summary>
/// Canonical M12D-P2B composition. It consumes the frozen P2A primitive and is
/// routed only by the explicit M12D candidate generation; generation 3 is unchanged.
/// </summary>
public static class PlanetaryNaturalTerrainFamilies
{
    public const ulong ProofGeneration = 2;
    public const uint CompositionVersion = 1;
    public const double WarpMagnitudeFraction = .10d;
    public const double MaximumAnisotropicWarpFraction = .08d;
    public const double OrientationRegularizer = .5d;
    public const double DomainWarpControlCellSizeMetres = 240_000d;

    private const uint WarpControlFamilyId = 0x50423210u;
    private const uint SeedFamilyMultiplier = 0x9E3779B9u;
    private const uint SeedOctaveMultiplier = 0x85EBCA6Bu;
    private const uint SeedVersion = 0xB2F10A4Du;

    public static PlanetaryNaturalTerrainFamilySample EvaluateFamily(
        in Double3 bodyFixedPoint,
        PlanetaryNaturalTerrainFamily family,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        Validate(bodyFixedPoint, family, identity);
        var configuration = Configuration(family);
        var controls = EvaluateWarpControls(bodyFixedPoint, identity);
        var orientation = RegularizedOrientation(controls);
        return EvaluateFamily(bodyFixedPoint, family, configuration, controls, orientation, identity);
    }

    private static PlanetaryNaturalTerrainFamilySample EvaluateFamily(in Double3 bodyFixedPoint,
        PlanetaryNaturalTerrainFamily family, in FamilyConfiguration configuration,
        in VectorField controls, in VectorField orientation,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        var macro = EvaluateScale(bodyFixedPoint, family, 0u, configuration.MacroCell,
            configuration.MacroAmplitude, configuration, controls, orientation, identity);
        var meso = Shape(EvaluateScale(bodyFixedPoint, family, 1u, configuration.MesoCell,
            configuration.MesoAmplitude, configuration, controls, orientation, identity),
            configuration.ShapeLinear, configuration.ShapeRidge);
        var near = EvaluateScale(bodyFixedPoint, family, 2u, configuration.NearCell,
            configuration.NearAmplitude, configuration, controls, orientation, identity);
        var total = Add(Add(macro, meso), near);
        return new(family, macro, meso, near, total, orientation.Value);
    }

    public static PlanetaryNaturalTerrainFamilySample EvaluateFamily(
        in Double3 normalizedBodyFixedDirection,
        double radiusMetres,
        PlanetaryNaturalTerrainFamily family,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        if (!normalizedBodyFixedDirection.IsFinite || normalizedBodyFixedDirection.LengthSquared <= 0d ||
            !double.IsFinite(radiusMetres) || radiusMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        return EvaluateFamily(normalizedBodyFixedDirection.Normalized() * radiusMetres, family, identity);
    }

    public static PlanetaryNaturalTerrainCompositionSample EvaluateComposed(
        in Double3 bodyFixedPoint,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        if (!bodyFixedPoint.IsFinite || !identity.IsValid) throw new ArgumentOutOfRangeException();
        var controls = EvaluateWarpControls(bodyFixedPoint, identity);
        var blend = EvaluateBiomeBlend(controls.X);
        var orientation = RegularizedOrientation(controls);
        var first = EvaluateFamily(bodyFixedPoint, blend.First, Configuration(blend.First), controls, orientation, identity);
        if (blend.First == blend.Second)
            return new(blend.First, blend.Second, 0d, default, first.Macro, first.Meso, first.Near, first.Total);
        var second = EvaluateFamily(bodyFixedPoint, blend.Second, Configuration(blend.Second), controls, orientation, identity);
        return new(blend.First, blend.Second, blend.Weight, blend.Gradient,
            Blend(first.Macro, second.Macro, blend.Weight, blend.Gradient),
            Blend(first.Meso, second.Meso, blend.Weight, blend.Gradient),
            Blend(first.Near, second.Near, blend.Weight, blend.Gradient),
            Blend(first.Total, second.Total, blend.Weight, blend.Gradient));
    }

    public static PlanetaryNaturalTerrainCellDescriptorSample EvaluateCellDescriptor(
        in Double3 bodyFixedPoint,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        if (!bodyFixedPoint.IsFinite || !identity.IsValid) throw new ArgumentOutOfRangeException();
        var controls = EvaluateWarpControls(bodyFixedPoint, identity);
        var orientation = RegularizedOrientation(controls);
        var blend = EvaluateBiomeBlend(controls.X);
        var domain = PlanetaryNaturalTerrainField.ReduceBodyPoint(bodyFixedPoint, DomainWarpControlCellSizeMetres);
        var xIdentity = new PlanetaryNaturalTerrainFieldIdentity(identity.BodyId, identity.PhysicalFieldGeneration,
            WarpControlFamilyId, 0x100u, DeriveSeed(identity.Seed, WarpControlFamilyId, 0x100u));
        var yIdentity = new PlanetaryNaturalTerrainFieldIdentity(identity.BodyId, identity.PhysicalFieldGeneration,
            WarpControlFamilyId, 0x101u, DeriveSeed(identity.Seed, WarpControlFamilyId, 0x101u));
        return new(domain.Cell, PlanetaryNaturalTerrainField.HashCell(xIdentity, domain.Cell),
            PlanetaryNaturalTerrainField.HashCell(yIdentity, domain.Cell), blend.First, blend.Second,
            blend.Weight, blend.Gradient, orientation.Value, ComposedBounds());
    }

    public static PlanetaryNaturalTerrainCompositionSample EvaluateComposed(
        in Double3 normalizedBodyFixedDirection,
        double radiusMetres,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        if (!normalizedBodyFixedDirection.IsFinite || normalizedBodyFixedDirection.LengthSquared <= 0d ||
            !double.IsFinite(radiusMetres) || radiusMetres <= 0d)
            throw new ArgumentOutOfRangeException();
        return EvaluateComposed(normalizedBodyFixedDirection.Normalized() * radiusMetres, identity);
    }

    public static PlanetaryNaturalTerrainFamilyBounds Bounds(PlanetaryNaturalTerrainFamily family)
    {
        var configuration = Configuration(family);
        var macro = ScaleValueBound(configuration.MacroAmplitude, configuration.ShapeLinear, 0d);
        var meso = ScaleValueBound(configuration.MesoAmplitude, configuration.ShapeLinear, configuration.ShapeRidge);
        var near = ScaleValueBound(configuration.NearAmplitude, configuration.ShapeLinear, 0d);
        var transform = WarpGradientTransformBound(configuration);
        var gradient = transform * (
            PlanetaryNaturalTerrainField.GradientBound(configuration.MacroCell, configuration.MacroAmplitude) +
            (Math.Abs(configuration.ShapeLinear) + Math.Abs(configuration.ShapeRidge)) *
                PlanetaryNaturalTerrainField.GradientBound(configuration.MesoCell, configuration.MesoAmplitude) +
            PlanetaryNaturalTerrainField.GradientBound(configuration.NearCell, configuration.NearAmplitude));
        return new(macro, meso, near, macro + meso + near, gradient);
    }

    public static PlanetaryNaturalTerrainFamilyBounds ComposedBounds()
    {
        var maximumMacro = 0d; var maximumMeso = 0d; var maximumNear = 0d;
        var maximumHeight = 0d; var maximumGradient = 0d;
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var bounds = Bounds(family);
            maximumMacro = Math.Max(maximumMacro, bounds.MacroHeight);
            maximumMeso = Math.Max(maximumMeso, bounds.MesoHeight);
            maximumNear = Math.Max(maximumNear, bounds.NearHeight);
            maximumHeight = Math.Max(maximumHeight, bounds.TotalHeight);
            maximumGradient = Math.Max(maximumGradient, bounds.TotalGradient);
        }
        var controlGradient = PlanetaryNaturalTerrainField.GradientBound(DomainWarpControlCellSizeMetres, 1d);
        var normalizedGradient = controlGradient / PlanetaryNaturalTerrainField.MaximumUnitValue;
        var outerWeightGradient = 1.875d / .84d * normalizedGradient;
        var blendWeightGradient = 1.875d * 6d * outerWeightGradient;
        maximumGradient += 2d * maximumHeight * blendWeightGradient;
        return new(maximumMacro, maximumMeso, maximumNear, maximumHeight, maximumGradient);
    }

    /// <summary>Stable manifest of the proof generation's ordered family/octave configuration.</summary>
    public static ulong ComputeManifestHash(uint seed)
    {
        var hash = 1469598103934665603ul;
        hash = MixManifest(hash, ProofGeneration); hash = MixManifest(hash, CompositionVersion);
        hash = MixManifest(hash, PlanetaryNaturalTerrainField.HashVersion); hash = MixManifest(hash, seed);
        hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(WarpMagnitudeFraction));
        hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(MaximumAnisotropicWarpFraction));
        hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(OrientationRegularizer));
        hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(DomainWarpControlCellSizeMetres));
        foreach (var family in Enum.GetValues<PlanetaryNaturalTerrainFamily>())
        {
            var value = Configuration(family); hash = MixManifest(hash, (uint)family);
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.MacroCell));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.MacroAmplitude));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.MesoCell));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.MesoAmplitude));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.NearCell));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.NearAmplitude));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.ShapeLinear));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.ShapeRidge));
            hash = MixManifest(hash, BitConverter.DoubleToUInt64Bits(value.Anisotropy));
        }
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MixManifest(ulong hash, ulong value) => unchecked((hash ^ value) * 1099511628211ul);

    private static FamilyBlend EvaluateBiomeBlend(in PlanetaryNaturalTerrainFieldSample control)
    {
        var normalized = control.Height / PlanetaryNaturalTerrainField.MaximumUnitValue;
        var outerInput = (normalized + .42d) / .84d;
        var outer = SmootherStepClamped(outerInput, out var outerDerivative);
        var outerGradient = control.BodyGradient * (outerDerivative /
            (.84d * PlanetaryNaturalTerrainField.MaximumUnitValue));
        var coordinate = outer * 6d;
        var index = Math.Min((int)Math.Floor(coordinate), 5);
        var fraction = coordinate - index;
        var weight = SmootherStepClamped(fraction, out var blendDerivative);
        var weightGradient = outerGradient * (6d * blendDerivative);
        var first = (PlanetaryNaturalTerrainFamily)(index + 1);
        var second = (PlanetaryNaturalTerrainFamily)(index + 2);
        return new(first, second, weight, weightGradient);
    }

    private static PlanetaryNaturalTerrainFieldSample EvaluateScale(in Double3 point,
        PlanetaryNaturalTerrainFamily family, uint octave, double cellSize, double amplitude,
        in FamilyConfiguration configuration, in VectorField controls, in VectorField orientation,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        var baseScale = WarpMagnitudeFraction * cellSize / 3d;
        var warped = point + controls.Value * baseScale;
        var anisotropicScale = configuration.Anisotropy * cellSize /
            PlanetaryNaturalTerrainField.MaximumUnitValue;
        if (anisotropicScale != 0d)
            warped += orientation.Value * (controls.X.Height * anisotropicScale);
        var fieldIdentity = new PlanetaryNaturalTerrainFieldIdentity(identity.BodyId,
            identity.PhysicalFieldGeneration, (uint)family, octave,
            DeriveSeed(identity.Seed, (uint)family, octave));
        var sample = PlanetaryNaturalTerrainField.EvaluateBodyPoint(warped, cellSize, amplitude, fieldIdentity);
        var gradient = sample.BodyGradient +
            (controls.X.BodyGradient * sample.BodyGradient.X +
             controls.Y.BodyGradient * sample.BodyGradient.Y +
             controls.Z.BodyGradient * sample.BodyGradient.Z) * baseScale;
        if (anisotropicScale != 0d)
        {
            var projected = Double3.Dot(sample.BodyGradient, orientation.Value);
            gradient += (controls.X.BodyGradient * projected +
                (orientation.X.BodyGradient * sample.BodyGradient.X +
                 orientation.Y.BodyGradient * sample.BodyGradient.Y +
                 orientation.Z.BodyGradient * sample.BodyGradient.Z) * controls.X.Height) * anisotropicScale;
        }
        return new(sample.Height, gradient);
    }

    private static VectorField EvaluateWarpControls(in Double3 point,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        var x = EvaluateControl(point, DomainWarpControlCellSizeMetres, WarpControlFamilyId, 0x100u, identity);
        var y = EvaluateControl(point, DomainWarpControlCellSizeMetres, WarpControlFamilyId, 0x101u, identity);
        var inverseBound = 1d / PlanetaryNaturalTerrainField.MaximumUnitValue;
        var z = new PlanetaryNaturalTerrainFieldSample(x.Height * y.Height * inverseBound,
            (x.BodyGradient * y.Height + y.BodyGradient * x.Height) * inverseBound);
        return new(x, y, z);
    }

    private static PlanetaryNaturalTerrainFieldSample EvaluateControl(in Double3 point, double cell,
        uint family, uint octave, in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        var fieldIdentity = new PlanetaryNaturalTerrainFieldIdentity(identity.BodyId,
            identity.PhysicalFieldGeneration, family, octave,
            DeriveSeed(identity.Seed, family, octave));
        return PlanetaryNaturalTerrainField.EvaluateBodyPoint(point, cell, 1d, fieldIdentity);
    }

    private static VectorField RegularizedOrientation(in VectorField value)
    {
        var length = Math.Sqrt(value.Value.LengthSquared + OrientationRegularizer * OrientationRegularizer);
        var inverse = 1d / length;
        var lengthGradient = (value.X.BodyGradient * value.X.Height + value.Y.BodyGradient * value.Y.Height +
            value.Z.BodyGradient * value.Z.Height) * inverse;
        var inverseSquared = inverse * inverse;
        return new(
            new(value.X.Height * inverse, value.X.BodyGradient * inverse - lengthGradient * (value.X.Height * inverseSquared)),
            new(value.Y.Height * inverse, value.Y.BodyGradient * inverse - lengthGradient * (value.Y.Height * inverseSquared)),
            new(value.Z.Height * inverse, value.Z.BodyGradient * inverse - lengthGradient * (value.Z.Height * inverseSquared)));
    }

    private static PlanetaryNaturalTerrainFieldSample Shape(in PlanetaryNaturalTerrainFieldSample sample,
        double linear, double ridge)
    {
        if (ridge == 0d && linear == 1d) return sample;
        var epsilon = Math.Max(Math.Abs(sample.Height) * 0d + .01d, .01d);
        var root = Math.Sqrt(sample.Height * sample.Height + epsilon * epsilon);
        var height = linear * sample.Height + ridge * (root - epsilon);
        var derivative = linear + ridge * sample.Height / root;
        return new(height, sample.BodyGradient * derivative);
    }

    private static PlanetaryNaturalTerrainFieldSample Blend(in PlanetaryNaturalTerrainFieldSample first,
        in PlanetaryNaturalTerrainFieldSample second, double weight, in Double3 weightGradient)
    {
        var inverse = 1d - weight;
        return new(first.Height * inverse + second.Height * weight,
            first.BodyGradient * inverse + second.BodyGradient * weight +
            weightGradient * (second.Height - first.Height));
    }

    private static PlanetaryNaturalTerrainFieldSample Add(in PlanetaryNaturalTerrainFieldSample first,
        in PlanetaryNaturalTerrainFieldSample second) =>
        new(first.Height + second.Height, first.BodyGradient + second.BodyGradient);

    private static double ScaleValueBound(double amplitude, double linear, double ridge) =>
        PlanetaryNaturalTerrainField.ValueBound(amplitude) * (Math.Abs(linear) + Math.Abs(ridge));

    private static double WarpGradientTransformBound(in FamilyConfiguration configuration)
    {
        var controlGradient = PlanetaryNaturalTerrainField.GradientBound(DomainWarpControlCellSizeMetres, 1d);
        var baseContribution = WarpMagnitudeFraction * configuration.MacroCell / 3d * Math.Sqrt(6d) * controlGradient;
        var orientationGradient = 2d * Math.Sqrt(6d) * controlGradient;
        var anisotropicContribution = configuration.Anisotropy * configuration.MacroCell /
            PlanetaryNaturalTerrainField.MaximumUnitValue *
            (controlGradient + PlanetaryNaturalTerrainField.MaximumUnitValue * Math.Sqrt(3d) * orientationGradient);
        return 1d + baseContribution + anisotropicContribution;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DeriveSeed(uint seed, uint family, uint octave) =>
        unchecked(seed ^ SeedVersion ^ family * SeedFamilyMultiplier ^ octave * SeedOctaveMultiplier);

    private static double SmootherStepClamped(double value, out double derivative)
    {
        if (value <= 0d) { derivative = 0d; return 0d; }
        if (value >= 1d) { derivative = 0d; return 1d; }
        var square = value * value;
        derivative = 30d * square * (value - 1d) * (value - 1d);
        return square * value * (value * (value * 6d - 15d) + 10d);
    }

    private static FamilyConfiguration Configuration(PlanetaryNaturalTerrainFamily family) => family switch
    {
        PlanetaryNaturalTerrainFamily.Grassland => new(18_000d, 12d, 2_700d, 5d, 180d, 1.5d, 1d, 0d, 0d),
        PlanetaryNaturalTerrainFamily.ScrubDry => new(24_000d, 11d, 3_400d, 6d, 240d, 1.8d, .92d, .08d, 0d),
        PlanetaryNaturalTerrainFamily.RockyMountain => new(48_000d, 34d, 6_500d, 21d, 520d, 11d, .62d, .38d, .035d),
        PlanetaryNaturalTerrainFamily.Alpine => new(36_000d, 24d, 4_200d, 14d, 320d, 6d, .70d, .30d, .04d),
        PlanetaryNaturalTerrainFamily.DesertDunes => new(14_000d, 5d, 1_600d, 2.2d, 120d, .8d, .82d, .18d, .08d),
        PlanetaryNaturalTerrainFamily.CoastWetland => new(24_000d, 1.5d, 2_800d, .8d, 180d, .25d, .76d, -.24d, 0d),
        PlanetaryNaturalTerrainFamily.SnowGlacial => new(30_000d, 14d, 3_600d, 7d, 260d, 2.5d, .68d, .32d, .06d),
        PlanetaryNaturalTerrainFamily.GenericRemote => new(22_000d, 10d, 2_800d, 4d, 220d, 1.2d, 1d, 0d, 0d),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private static void Validate(in Double3 point, PlanetaryNaturalTerrainFamily family,
        in PlanetaryNaturalTerrainFamilyIdentity identity)
    {
        if (!point.IsFinite || !identity.IsValid || !Enum.IsDefined(family)) throw new ArgumentOutOfRangeException();
    }

    private readonly record struct FamilyConfiguration(double MacroCell, double MacroAmplitude,
        double MesoCell, double MesoAmplitude, double NearCell, double NearAmplitude,
        double ShapeLinear, double ShapeRidge, double Anisotropy);
    private readonly record struct VectorField(PlanetaryNaturalTerrainFieldSample X,
        PlanetaryNaturalTerrainFieldSample Y, PlanetaryNaturalTerrainFieldSample Z)
    {
        public Double3 Value => new(X.Height, Y.Height, Z.Height);
    }
    private readonly record struct FamilyBlend(PlanetaryNaturalTerrainFamily First,
        PlanetaryNaturalTerrainFamily Second, double Weight, Double3 Gradient);
}
