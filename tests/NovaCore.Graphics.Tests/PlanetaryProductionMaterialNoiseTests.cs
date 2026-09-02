using System.Numerics;
using NovaCore.Graphics;

internal static class PlanetaryProductionMaterialNoiseTests
{
    private const double RadiusMetres = 6_371_008.8d;
    private static readonly (double Scale, Vector3 Offset)[] Bands =
    [
        (5.5d, new(613f, 89f, 347f)),
        (96d, new(137f, 271f, 419f)),
        (410d, new(43f, 719f, 181f))
    ];

    public static void Run()
    {
        VerifyShaderContract();

        var directions = new[]
        {
            Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ,
            Vector3.Normalize(new Vector3(1f, 1f, 0f)),
            Vector3.Normalize(new Vector3(1f, -1f, 1f)),
            Vector3.Normalize(new Vector3(-1f, 1f, -1f)),
            Vector3.Normalize(new Vector3(.271f, .913f, -.304f))
        };
        var explicitNoisePoints = new[]
        {
            (0d, 0d), (.125d, .875d), (-.125d, -.875d), (-1.0000001d, 1.9999999d),
            (1_158_365.2363636363d, -932_817.4181818182d),
            (-66_364.675d, 65_218.225d), (15_540.265365853659d, -15_538.731707317073d)
        };

        double maximumScalarDelta = 0d, maximumBiplanarDelta = 0d;
        double maximumMaterialDelta = 0d, maximumNormalDelta = 0d;
        var scalarSamples = 0;
        foreach (var point in explicitNoisePoints)
        {
            CompareScalar(point, ref maximumScalarDelta);
            scalarSamples++;
        }

        var footprints = new[] { .00001f, .5f, 2f, 5.5f, 24f, 96f, 410f, 900f };
        var biplanarSamples = 0;
        foreach (var direction in directions)
        {
            var body = ToDouble(Vector3.Normalize(direction), RadiusMetres + 173.25d);
            var normal = Vector3.Normalize(direction + new Vector3(.017f, -.011f, .009f));
            var weights = PlanetaryTerrainMaterialSynthesis.BiplanarWeights(normal);
            foreach (var band in Bands)
            {
                var point = Add(body, band.Offset);
                var scaled = Divide(point, band.Scale);
                CompareScalar((scaled.Y, scaled.Z), ref maximumScalarDelta);
                CompareScalar((scaled.Z + 19.19d, scaled.X + 7.73d), ref maximumScalarDelta);
                CompareScalar((scaled.X + 41.17d, scaled.Y + 3.11d), ref maximumScalarDelta);
                scalarSamples += 3;

                foreach (var axisWeights in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, weights })
                {
                    var oldValue = BiplanarNoiseOld(body, band.Scale, band.Offset, axisWeights);
                    var optimizedValue = BiplanarNoiseOptimized(body, band.Scale, band.Offset, axisWeights);
                    maximumBiplanarDelta = Math.Max(maximumBiplanarDelta, Math.Abs(oldValue - optimizedValue));
                    Require(BitConverter.SingleToInt32Bits(oldValue) == BitConverter.SingleToInt32Bits(optimizedValue),
                        "optimized biplanar value is bit-identical to the accepted scalar-corner reference");
                    biplanarSamples++;
                }
            }

            foreach (var footprint in footprints)
            {
                var oldMaterial = MaterialSample(body, normal, footprint, optimized: false);
                var optimizedMaterial = MaterialSample(body, normal, footprint, optimized: true);
                maximumMaterialDelta = Math.Max(maximumMaterialDelta, MaximumComponentDelta(oldMaterial, optimizedMaterial));
                Require(oldMaterial == optimizedMaterial,
                    "band activation, material frequency identity, and resulting material response remain bit-identical");

                var oldVisualNormal = VisualNormal(body, normal, footprint, optimized: false);
                var optimizedVisualNormal = VisualNormal(body, normal, footprint, optimized: true);
                maximumNormalDelta = Math.Max(maximumNormalDelta, (oldVisualNormal - optimizedVisualNormal).Length());
                Require(oldVisualNormal == optimizedVisualNormal,
                    "height-derived visual normal remains bit-identical across derivative footprints");
            }
        }

        Require(maximumScalarDelta == 0d && maximumBiplanarDelta == 0d && maximumMaterialDelta == 0d && maximumNormalDelta == 0d,
            "all old-versus-optimized material-noise comparisons are bit-identical");
        Console.WriteLine($"Production material noise optimization: scalarSamples={scalarSamples}; biplanarSamples={biplanarSamples}; " +
            $"scalarMax={maximumScalarDelta:E3}; biplanarMax={maximumBiplanarDelta:E3}; materialMax={maximumMaterialDelta:E3}; visualNormalMax={maximumNormalDelta:E3}; bitIdentity=true");
    }

    private static void VerifyShaderContract()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var shader = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "production_terrain_material.glsl"));
        Require(shader.Contains("vec4 TerrainHash21Corners(dvec2 cell)", StringComparison.Ordinal) &&
            shader.Contains("dvec2 hashX=fract(x*.1031),hashY=fract(y*.1030),hashZ=fract(x*.0973)", StringComparison.Ordinal) &&
            shader.Contains("vec4 corners=TerrainHash21Corners(cell)", StringComparison.Ordinal),
            "production TerrainNoise2 shares the two-by-two corner hash prefix");
        Require(!shader.Contains("TerrainHash21(cell+dvec2", StringComparison.Ordinal) &&
            Count(shader, "vec3 biplanarWeights=TerrainBiplanarWeights(geometricNormal)") == 1 &&
            Count(shader, "TerrainBiplanarNoiseRaw(bodyMetres") == 3,
            "the immediate material path keeps one shared projection weight and one sample per active frequency");
    }

    private static void CompareScalar((double X, double Y) point, ref double maximumDelta)
    {
        var oldValue = NoiseOld(point);
        var optimizedValue = NoiseOptimized(point);
        maximumDelta = Math.Max(maximumDelta, Math.Abs(oldValue - optimizedValue));
        Require(BitConverter.SingleToInt32Bits(oldValue) == BitConverter.SingleToInt32Bits(optimizedValue),
            $"optimized scalar noise is bit-identical at ({point.X:R}, {point.Y:R})");
    }

    private static float NoiseOld((double X, double Y) point)
    {
        var cell = (Math.Floor(point.X), Math.Floor(point.Y));
        var x = (float)Fract(point.X); var y = (float)Fract(point.Y);
        x = x * x * (3f - 2f * x); y = y * y * (3f - 2f * y);
        var a = HashOld(cell); var b = HashOld((cell.Item1 + 1d, cell.Item2));
        var c = HashOld((cell.Item1, cell.Item2 + 1d)); var d = HashOld((cell.Item1 + 1d, cell.Item2 + 1d));
        return Lerp(Lerp(a, b, x), Lerp(c, d, x), y);
    }

    private static float NoiseOptimized((double X, double Y) point)
    {
        var cellX = Math.Floor(point.X); var cellY = Math.Floor(point.Y);
        var x = (float)Fract(point.X); var y = (float)Fract(point.Y);
        x = x * x * (3f - 2f * x); y = y * y * (3f - 2f * y);
        var hashX0 = Fract(cellX * .1031d); var hashX1 = Fract((cellX + 1d) * .1031d);
        var hashY0 = Fract(cellY * .1030d); var hashY1 = Fract((cellY + 1d) * .1030d);
        var hashZ0 = Fract(cellX * .0973d); var hashZ1 = Fract((cellX + 1d) * .0973d);
        var a = HashPrepared(hashX0, hashY0, hashZ0); var b = HashPrepared(hashX1, hashY0, hashZ1);
        var c = HashPrepared(hashX0, hashY1, hashZ0); var d = HashPrepared(hashX1, hashY1, hashZ1);
        return Lerp(Lerp(a, b, x), Lerp(c, d, x), y);
    }

    private static float HashOld((double X, double Y) point) =>
        HashPrepared(Fract(point.X * .1031d), Fract(point.Y * .1030d), Fract(point.X * .0973d));

    private static float HashPrepared(double x, double y, double z)
    {
        var dot = x * (y + 33.33d) + y * (z + 33.33d) + z * (x + 33.33d);
        x += dot; y += dot; z += dot;
        return (float)Fract((x + y) * z);
    }

    private static float BiplanarNoiseOld((double X, double Y, double Z) body, double scale, Vector3 offset, Vector3 weights) =>
        Biplanar(body, scale, offset, weights, NoiseOld);

    private static float BiplanarNoiseOptimized((double X, double Y, double Z) body, double scale, Vector3 offset, Vector3 weights) =>
        Biplanar(body, scale, offset, weights, NoiseOptimized);

    private static float Biplanar((double X, double Y, double Z) body, double scale, Vector3 offset, Vector3 weights,
        Func<(double X, double Y), float> noise)
    {
        var point = Divide(Add(body, offset), scale); var value = 0f;
        if (weights.X > 1e-4f) value += weights.X * noise((point.Y, point.Z));
        if (weights.Y > 1e-4f) value += weights.Y * noise((point.Z + 19.19d, point.X + 7.73d));
        if (weights.Z > 1e-4f) value += weights.Z * noise((point.X + 41.17d, point.Y + 3.11d));
        return value;
    }

    private static MaterialResult MaterialSample((double X, double Y, double Z) body, Vector3 normal, float footprint, bool optimized)
    {
        var weights = PlanetaryTerrainMaterialSynthesis.BiplanarWeights(normal);
        float Sample(double scale, Vector3 offset) => optimized
            ? BiplanarNoiseOptimized(body, scale, offset, weights)
            : BiplanarNoiseOld(body, scale, offset, weights);
        var mesoAttenuation = PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(footprint, 96f);
        var microAttenuation = PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(footprint, 5.5f);
        var broadAttenuation = PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(footprint, 410f);
        var normalMesoAttenuation = PlanetaryTerrainMaterialSynthesis.NormalFrequencyAttenuation(footprint, 96f);
        var normalMicroAttenuation = PlanetaryTerrainMaterialSynthesis.NormalFrequencyAttenuation(footprint, 5.5f);
        var mesoRaw = mesoAttenuation > 0f || normalMesoAttenuation > 0f ? Sample(96d, new(137f, 271f, 419f)) : .5f;
        var microRaw = microAttenuation > 0f || normalMicroAttenuation > 0f ? Sample(5.5d, new(613f, 89f, 347f)) : .5f;
        var broadRaw = broadAttenuation > 0f ? Sample(410d, new(43f, 719f, 181f)) : .5f;
        var meso = Lerp(.5f, mesoRaw, mesoAttenuation); var micro = Lerp(.5f, microRaw, microAttenuation);
        var broad = Lerp(.5f, broadRaw, broadAttenuation);
        var normalMeso = Lerp(.5f, mesoRaw, normalMesoAttenuation); var normalMicro = Lerp(.5f, microRaw, normalMicroAttenuation);
        var variation = (meso - .5f) * .18f + (micro - .5f) * .055f + (broad - .5f) * .12f;
        return new(variation, .83f + (micro - .5f) * .08f, .93f - (meso - .5f) * .08f,
            .32f * ((normalMeso - .5f) * .72f + (normalMicro - .5f) * .28f));
    }

    private static Vector3 VisualNormal((double X, double Y, double Z) body, Vector3 normal, float footprint, bool optimized)
    {
        var reference = MathF.Abs(normal.Y) < .9f ? Vector3.UnitY : Vector3.UnitX;
        var east = Vector3.Normalize(Vector3.Cross(reference, normal)); var north = Vector3.Normalize(Vector3.Cross(normal, east));
        var step = Math.Max(.02f, footprint * .5f);
        float Height(Vector3 tangent, float sign)
        {
            var point = Add(body, tangent, step * sign); var weights = PlanetaryTerrainMaterialSynthesis.BiplanarWeights(normal);
            float Sample(double scale, Vector3 offset) => optimized
                ? BiplanarNoiseOptimized(point, scale, offset, weights)
                : BiplanarNoiseOld(point, scale, offset, weights);
            var meso = Lerp(.5f, Sample(96d, new(137f, 271f, 419f)), PlanetaryTerrainMaterialSynthesis.NormalFrequencyAttenuation(footprint, 96f));
            var micro = Lerp(.5f, Sample(5.5d, new(613f, 89f, 347f)), PlanetaryTerrainMaterialSynthesis.NormalFrequencyAttenuation(footprint, 5.5f));
            return .32f * ((meso - .5f) * .72f + (micro - .5f) * .28f);
        }
        var eastSlope = (Height(east, 1f) - Height(east, -1f)) / (2f * step);
        var northSlope = (Height(north, 1f) - Height(north, -1f)) / (2f * step);
        return Vector3.Normalize(normal - east * eastSlope - north * northSlope);
    }

    private static double MaximumComponentDelta(MaterialResult left, MaterialResult right) => Math.Max(
        Math.Max(Math.Abs(left.Variation - right.Variation), Math.Abs(left.Roughness - right.Roughness)),
        Math.Max(Math.Abs(left.AmbientOcclusion - right.AmbientOcclusion), Math.Abs(left.Displacement - right.Displacement)));
    private static (double X, double Y, double Z) ToDouble(Vector3 value, double scale) => (value.X * scale, value.Y * scale, value.Z * scale);
    private static (double X, double Y, double Z) Add((double X, double Y, double Z) value, Vector3 add) => (value.X + add.X, value.Y + add.Y, value.Z + add.Z);
    private static (double X, double Y, double Z) Add((double X, double Y, double Z) value, Vector3 direction, float scale) => (value.X + direction.X * scale, value.Y + direction.Y * scale, value.Z + direction.Z * scale);
    private static (double X, double Y, double Z) Divide((double X, double Y, double Z) value, double scale) => (value.X / scale, value.Y / scale, value.Z / scale);
    private static float Lerp(float first, float second, float amount) => first + (second - first) * amount;
    private static double Fract(double value) => value - Math.Floor(value);
    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private readonly record struct MaterialResult(float Variation, float Roughness, float AmbientOcclusion, float Displacement);
}
