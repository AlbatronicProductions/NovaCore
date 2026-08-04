using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Platform;

var cases = new[]
{
    new PrecisionCase("positive large", new Double3(4_000_000_000_000.25d, 3d, -4d), new Double3(4_000_000_000_000d, 0d, 0d), 0.001d),
    new PrecisionCase("negative large", new Double3(-4_000_000_000_000.125d, -2d, 3d), new Double3(-4_000_000_000_000d, 0d, 0d), 0.001d),
    new PrecisionCase("extreme large", new Double3(1_000_000_000_000_000d + 0.125d, -1_000_000_000_000_000d - 0.25d, 5d), new Double3(1_000_000_000_000_000d, -1_000_000_000_000_000d, 0d), 0.5d),
    new PrecisionCase("small delta", new Double3(4_000_000_000_000.0078125d, 0d, 0d), new Double3(4_000_000_000_000d, 0d, 0d), 0.001d),
};

Console.WriteLine("Precision Test");
Console.WriteLine();
Console.WriteLine($"{"Magnitude",-12} {"Error",-14} {"Maximum",-14} Result");

foreach (var test in cases)
{
    var objectEncoded = EncodedPosition.Encode(test.Object);
    var cameraEncoded = EncodedPosition.Encode(test.Camera);
    var reconstructedWorld = objectEncoded.Reconstruct();
    var relative = EncodedPosition.Resolve(objectEncoded, cameraEncoded).Value;
    var expected = test.Object - test.Camera;
    var error = MaxAbs(relative - expected);
    var deterministic = objectEncoded == EncodedPosition.Encode(test.Object) && cameraEncoded == EncodedPosition.Encode(test.Camera);
    var withinTolerance = error <= test.MaximumError;
    Console.WriteLine($"{test.Magnitude,-12} {error,-14:R} {test.MaximumError,-14:R} {(withinTolerance ? "PASS" : "FAIL")}");
    Assert(withinTolerance, $"{test.Name}: relative error {error:R} exceeds {test.MaximumError:R}");
    Assert(deterministic, $"{test.Name}: encoding was not deterministic");
    Assert(MaxAbs(reconstructedWorld - test.Object) <= test.MaximumError, $"{test.Name}: world reconstruction out of tolerance");
}

var plainFloat = (float)cases[0].Object.X - (float)cases[0].Camera.X;
Assert(plainFloat == 0f, "single-float control case should lose the 0.25-unit delta");
VerifyCameraMovementSymmetry();
VerifyLogOptions();
Console.WriteLine();
Console.WriteLine("PASS");

static double MaxAbs(Double3 value) => Math.Max(Math.Abs(value.X), Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

static void VerifyCameraMovementSymmetry()
{
    var basePosition = new Double3(4_000_000_000_000d, -3_000_000_000_000d, 7_000_000_000_000d);
    var step = 0.1d;
    var objectEncoded = EncodedPosition.Encode(basePosition);
    var plusX = EncodedPosition.Resolve(objectEncoded, EncodedPosition.Encode(basePosition + new Double3(step, 0d, 0d))).Value;
    var minusX = EncodedPosition.Resolve(objectEncoded, EncodedPosition.Encode(basePosition + new Double3(-step, 0d, 0d))).Value;
    var plusY = EncodedPosition.Resolve(objectEncoded, EncodedPosition.Encode(basePosition + new Double3(0d, step, 0d))).Value;
    var minusY = EncodedPosition.Resolve(objectEncoded, EncodedPosition.Encode(basePosition + new Double3(0d, -step, 0d))).Value;
    Console.WriteLine($"movement symmetry: +X={plusX.X:R}, -X={minusX.X:R}, +Y={plusY.Y:R}, -Y={minusY.Y:R}");
    const double tolerance = 0.01d;
    Assert(Math.Abs(plusX.X + minusX.X) <= tolerance && Math.Abs(Math.Abs(plusX.X) - Math.Abs(minusX.X)) <= tolerance, "+X/-X camera movement is not symmetric");
    Assert(Math.Abs(plusY.Y + minusY.Y) <= tolerance && Math.Abs(Math.Abs(plusY.Y) - Math.Abs(minusY.Y)) <= tolerance, "+Y/-Y camera movement is not symmetric");
    Assert(plusX.X < 0d && minusX.X > 0d, "X camera-relative subtraction has the wrong sign");
    Assert(plusY.Y < 0d && minusY.Y > 0d, "Y camera-relative subtraction has the wrong sign");
}

static void VerifyLogOptions()
{
    Assert(LogOptions.TryParse(["--log=input,precision", "--log=vulkan"], out var options, out _), "valid log options were rejected");
    Assert(options.IsEnabled(LogCategory.Input) && options.IsEnabled(LogCategory.Precision) && options.IsEnabled(LogCategory.Vulkan), "log categories were not combined");
    Assert(LogOptions.TryParse(["--verbose-input"], out var compatibility, out _) && compatibility.IsEnabled(LogCategory.Input), "verbose input alias failed");
    Assert(!LogOptions.TryParse(["--log=unknown"], out _, out _), "invalid log category was accepted");
    Console.WriteLine("log options: parsing and verbose-input compatibility passed");
}
file readonly record struct PrecisionCase(string Name, Double3 Object, Double3 Camera, double MaximumError)
{
    public string Magnitude => Name switch
    {
        "extreme large" => "1e15",
        _ => "4e12",
    };
}
