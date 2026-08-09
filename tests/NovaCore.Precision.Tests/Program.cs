using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Platform;

const double rootMagnitude = 4_000_000_000_000d;
var camera = new Double3(rootMagnitude, -rootMagnitude, rootMagnitude);
var cases = new[]
{
    new PrecisionCase("kilometre", camera + new Double3(1_000d, -1_000d, 500d), camera, 2e-4d),
    new PrecisionCase("metre", camera + new Double3(1d, -1d, .5d), camera, 2e-7d),
    new PrecisionCase("centimetre", camera + new Double3(.01d, -.01d, .02d), camera, 2e-9d),
    new PrecisionCase("millimetre", camera + new Double3(.001d, -.001d, .001d), camera, 2e-10d),
    new PrecisionCase("negative root", new Double3(-rootMagnitude - .25d, rootMagnitude + .125d, -rootMagnitude - .5d), new Double3(-rootMagnitude, rootMagnitude, -rootMagnitude), 1e-8d),
    new PrecisionCase("mixed signs", new Double3(-rootMagnitude + .01d, -rootMagnitude - .02d, rootMagnitude + .03d), new Double3(-rootMagnitude, -rootMagnitude, rootMagnitude), 4e-9d),
    new PrecisionCase("near zero", new Double3(.001d, -.01d, 1_000d), Double3.Zero, 2e-10d),
};

Console.WriteLine("Camera-Centric Precision Test");
Console.WriteLine();
Console.WriteLine($"{"Separation",-16} {"Represented",-14} {"GPU error",-14} Result");

foreach (var test in cases)
{
    var expected = test.ObjectRoot - test.CameraRoot;
    var relative = CameraRelativeRenderPosition.Create(test.ObjectRoot, test.CameraRoot);
    var encoded = relative.Encode();
    var reconstructed = encoded.Reconstruct();
    var error = MaxAbs(reconstructed - expected);
    var deterministic = relative == CameraRelativeRenderPosition.Create(test.ObjectRoot, test.CameraRoot) &&
        encoded == CameraRelativeRenderPosition.Create(test.ObjectRoot, test.CameraRoot).Encode();
    var withinTolerance = error <= test.MaximumGpuError;
    Console.WriteLine($"{test.Name,-16} {MaxAbs(expected),-14:R} {error,-14:R} {(withinTolerance ? "PASS" : "FAIL")}");
    Assert(relative.IsFinite && reconstructed.IsFinite, $"{test.Name}: non-finite camera-relative transport");
    Assert(withinTolerance, $"{test.Name}: post-subtraction GPU encoding error {error:R} exceeds {test.MaximumGpuError:R}");
    Assert(deterministic, $"{test.Name}: subtraction/encoding was not deterministic");
}

var representedMillimetre = (camera.X + .001d) - camera.X;
Assert(representedMillimetre > 0d && representedMillimetre < .002d, "FP64 large-root millimetre separation was not retained");
Assert((float)(camera.X + .001d) - (float)camera.X == 0f, "single-float control should lose the millimetre separation");
Assert(!CameraRelativeRenderPosition.TryCreate(new Double3(double.NaN, 0d, 0d), camera, out _), "NaN object accepted");
Assert(!CameraRelativeRenderPosition.TryCreate(camera, new Double3(0d, double.PositiveInfinity, 0d), out _), "infinite camera accepted");

VerifyCameraMovementSymmetry();
VerifyHotPath(camera);
VerifyLogOptions();
Console.WriteLine();
Console.WriteLine("PASS");

static double MaxAbs(Double3 value) => Math.Max(Math.Abs(value.X), Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

static void VerifyCameraMovementSymmetry()
{
    var basePosition = new Double3(4_000_000_000_000d, -3_000_000_000_000d, 7_000_000_000_000d);
    var step = 0.1d;
    var plusX = CameraRelativeRenderPosition.Create(basePosition, basePosition + new Double3(step, 0d, 0d)).Value;
    var minusX = CameraRelativeRenderPosition.Create(basePosition, basePosition + new Double3(-step, 0d, 0d)).Value;
    var plusY = CameraRelativeRenderPosition.Create(basePosition, basePosition + new Double3(0d, step, 0d)).Value;
    var minusY = CameraRelativeRenderPosition.Create(basePosition, basePosition + new Double3(0d, -step, 0d)).Value;
    Console.WriteLine($"movement symmetry: +X={plusX.X:R}, -X={minusX.X:R}, +Y={plusY.Y:R}, -Y={minusY.Y:R}");
    const double tolerance = 0.001d;
    Assert(Math.Abs(plusX.X + minusX.X) <= tolerance && Math.Abs(Math.Abs(plusX.X) - Math.Abs(minusX.X)) <= tolerance, "+X/-X camera movement is not symmetric");
    Assert(Math.Abs(plusY.Y + minusY.Y) <= tolerance && Math.Abs(Math.Abs(plusY.Y) - Math.Abs(minusY.Y)) <= tolerance, "+Y/-Y camera movement is not symmetric");
    Assert(plusX.X < 0d && minusX.X > 0d && plusY.Y < 0d && minusY.Y > 0d, "camera-relative subtraction sign failure");
}

static void VerifyHotPath(in Double3 camera)
{
    const int iterations = 1_000_000;
    var objectRoot = camera + new Double3(.001d, -.01d, 1_000d);
    _ = CameraRelativeRenderPosition.Create(objectRoot, camera).Encode();
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    var checksum = 0d;
    for (var index = 0; index < iterations; index++)
        checksum += CameraRelativeRenderPosition.Create(objectRoot, camera).Encode().LowX;
    var elapsed = Stopwatch.GetElapsedTime(started);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    Assert(allocated == 0 && double.IsFinite(checksum), "camera-relative subtraction/packing hot path allocated or became non-finite");
    Console.WriteLine($"relative subtraction/pack: {elapsed.TotalNanoseconds / iterations:F2} ns/update; allocations={allocated} bytes");
}

static void VerifyLogOptions()
{
    Assert(LogOptions.TryParse(["--log=input,precision", "--log=vulkan"], out var options, out _), "valid log options were rejected");
    Assert(options.IsEnabled(LogCategory.Input) && options.IsEnabled(LogCategory.Precision) && options.IsEnabled(LogCategory.Vulkan), "log categories were not combined");
    Assert(LogOptions.TryParse(["--verbose-input"], out var compatibility, out _) && compatibility.IsEnabled(LogCategory.Input), "verbose input alias failed");
    Assert(!LogOptions.TryParse(["--log=unknown"], out _, out _), "invalid log category was accepted");
    Console.WriteLine("log options: parsing and verbose-input compatibility passed");
}

file readonly record struct PrecisionCase(
    string Name,
    Double3 ObjectRoot,
    Double3 CameraRoot,
    double MaximumGpuError);
