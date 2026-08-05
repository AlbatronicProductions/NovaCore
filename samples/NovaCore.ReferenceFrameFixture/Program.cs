using System.Globalization;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;

return Run();

static int Run()
{
    try
    {
        var fixture = CreateFixture();
        var graph = fixture.Transforms.Graph;
        var sourcePath = new ReferenceFrameId[graph.Count];
        var targetPath = new ReferenceFrameId[graph.Count];
        var traversalPath = new ReferenceFrameId[graph.Count * 2 - 1];

        Console.WriteLine("NovaCore Reference-Frame Fixture");
        Console.WriteLine();
        Console.WriteLine("Topology");
        Console.WriteLine("  ECL Star");
        Console.WriteLine("  └── CCE Planet");
        Console.WriteLine("      └── CCI Moon");
        Console.WriteLine("          └── CCF TestVessel");
        Console.WriteLine();
        Console.WriteLine("Scope");
        Console.WriteLine("  Static evaluated transforms.");
        Console.WriteLine("  No propagation, authoritative time binding, orbital mechanics, or SimulationSnapshot.");
        Console.WriteLine();
        Console.WriteLine("Resolved roots");
        PrintRoot("Star", fixture.Star, Double3.Zero, fixture, sourcePath, targetPath, traversalPath);
        PrintRoot("Planet", fixture.Planet, new Double3(100, 20, 0), fixture, sourcePath, targetPath, traversalPath);
        PrintRoot("Moon", fixture.Moon, new Double3(0, 10, 0), fixture, sourcePath, targetPath, traversalPath);
        PrintRoot("Vessel", fixture.Vessel, new Double3(2, 0, 0), fixture, sourcePath, targetPath, traversalPath);

        var vesselToRoot = Resolve(fixture, fixture.Vessel, fixture.Star, sourcePath, targetPath, traversalPath);
        var vesselToPlanet = Resolve(fixture, fixture.Vessel, fixture.Planet, sourcePath, targetPath, traversalPath);
        var planetToVessel = Resolve(fixture, fixture.Planet, fixture.Vessel, sourcePath, targetPath, traversalPath);
        var moonToRoot = Resolve(fixture, fixture.Moon, fixture.Star, sourcePath, targetPath, traversalPath);
        var vesselRootPosition = vesselToRoot.ConvertPosition(Double3.Zero);
        var vesselRootVelocity = vesselToRoot.ConvertVelocity(Double3.Zero, Double3.Zero);
        var vesselPlanetPosition = vesselToPlanet.ConvertPosition(Double3.Zero);
        var planetVesselPosition = planetToVessel.ConvertPosition(Double3.Zero);
        var roundTripPoint = vesselToRoot.ConvertPosition(planetToVessel.ConvertPosition(new Double3(3, 4, 0)));
        var moonDirection = moonToRoot.ConvertDirection(Double3.UnitX);
        var rotatingPointVelocity = moonToRoot.ConvertVelocity(new Double3(2, 0, 0), Double3.Zero);
        var rotatingContribution = rotatingPointVelocity - moonToRoot.SourceOriginVelocityInTarget;

        CheckNear(vesselRootPosition, new Double3(100, 32, 0), "Vessel root position");
        CheckNear(vesselPlanetPosition, new Double3(0, 12, 0), "Vessel-to-Planet position");
        CheckNear(planetVesselPosition, new Double3(-12, 0, 0), "Planet-to-Vessel position");
        CheckNear(roundTripPoint, new Double3(103, 24, 0), "Planet/Vessel round trip");
        CheckNear(moonDirection, Double3.UnitY, "Moon +X direction");
        CheckNear(vesselRootVelocity, new Double3(0, 2, 0), "Vessel root velocity");
        CheckNear(rotatingContribution, new Double3(-1, 0, 0), "Rotating velocity contribution");

        Console.WriteLine();
        Console.WriteLine("Queries");
        Console.WriteLine($"  Vessel -> ECL: position={Format(vesselRootPosition)} orientation={FormatQuaternion(vesselToRoot.ConvertOrientation(DoubleQuaternion.Identity))} velocity={Format(vesselRootVelocity)} angularVelocity={Format(vesselToRoot.SourceAngularVelocityInTarget)}");
        Console.WriteLine($"  Vessel -> CCE: position={Format(vesselPlanetPosition)}");
        Console.WriteLine($"  Planet -> CCF: position={Format(planetVesselPosition)}");
        Console.WriteLine($"  Moon +X -> ECL: direction={Format(moonDirection)}");
        Console.WriteLine($"  Rotating velocity contribution: {Format(rotatingContribution)}");

        _ = ComputeHash(fixture, sourcePath, targetPath, traversalPath);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = 14695981039346656037UL;
        for (var index = 0; index < 100_000; index++)
        {
            var resolved = Resolve(fixture, fixture.Vessel, fixture.Star, sourcePath, targetPath, traversalPath);
            checksum = Mix(checksum, (ulong)BitConverter.DoubleToInt64Bits(resolved.ConvertPosition(new Double3(index * .001d, 0, 0)).X));
        }
        var allocations = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocations == 0, "Warm resolution allocations");
        var hash = ComputeHash(fixture, sourcePath, targetPath, traversalPath);
        Check(hash == ComputeHash(fixture, sourcePath, targetPath, traversalPath), "Fixture hash repeatability");

        Console.WriteLine();
        Console.WriteLine("Verification: PASS");
        Console.WriteLine($"Deterministic fixture hash: 0x{hash:X16}");
        Console.WriteLine($"Warm resolution allocations: {allocations} bytes");
        Console.WriteLine($"Warm resolution checksum: 0x{checksum:X16}");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Verification: FAIL - {exception.Message}");
        return 1;
    }
}

static Fixture CreateFixture()
{
    var star = new ReferenceFrameId(1);
    var planet = new ReferenceFrameId(2);
    var moon = new ReferenceFrameId(3);
    var vessel = new ReferenceFrameId(4);
    var builder = new ReferenceFrameGraphBuilder();
    builder.Add(new ReferenceFrameNode(star, null, ReferenceFrameKind.Ecl, "fixture-ecl"));
    builder.Add(new ReferenceFrameNode(planet, star, ReferenceFrameKind.Cce, "fixture-cce"));
    builder.Add(new ReferenceFrameNode(moon, planet, ReferenceFrameKind.Cci, "fixture-cci"));
    builder.Add(new ReferenceFrameNode(vessel, moon, ReferenceFrameKind.Ccf, "fixture-ccf"));
    var graph = builder.Build();
    var transforms = new ReferenceFrameTransformSet(graph,
    [
        new ReferenceFrameEvaluation(star, new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true)),
        new ReferenceFrameEvaluation(planet, new EvaluatedReferenceFrame(new FrameTransform(new Double3(100, 20, 0), DoubleQuaternion.Identity), new Double3(1, 0, 0), Double3.Zero, true)),
        new ReferenceFrameEvaluation(moon, new EvaluatedReferenceFrame(new FrameTransform(new Double3(0, 10, 0), DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d)), new Double3(0, 2, 0), new Double3(0, 0, .5d), false)),
        new ReferenceFrameEvaluation(vessel, new EvaluatedReferenceFrame(new FrameTransform(new Double3(2, 0, 0), DoubleQuaternion.Identity), Double3.Zero, Double3.Zero, false)),
    ]);
    return new Fixture(transforms, star, planet, moon, vessel);
}

static void PrintRoot(string label, ReferenceFrameId frame, in Double3 local, in Fixture fixture, Span<ReferenceFrameId> sourcePath, Span<ReferenceFrameId> targetPath, Span<ReferenceFrameId> traversalPath) =>
    Console.WriteLine($"  {label,-7} local={Format(local),-14} root={Format(Resolve(fixture, frame, fixture.Star, sourcePath, targetPath, traversalPath).ConvertPosition(Double3.Zero))}");

static ResolvedReferenceFrameTransform Resolve(in Fixture fixture, ReferenceFrameId source, ReferenceFrameId target, Span<ReferenceFrameId> sourcePath, Span<ReferenceFrameId> targetPath, Span<ReferenceFrameId> traversalPath)
{
    var status = ReferenceFrameTransformResolver.TryResolveTransform(fixture.Transforms, source, target, sourcePath, targetPath, traversalPath, out var result);
    if (status != ReferenceFrameTransformResolutionStatus.Success) throw new InvalidOperationException($"Resolution failed: {status}.");
    return result;
}

static ulong ComputeHash(in Fixture fixture, Span<ReferenceFrameId> sourcePath, Span<ReferenceFrameId> targetPath, Span<ReferenceFrameId> traversalPath)
{
    var hash = 14695981039346656037UL;
    hash = HashResolution(hash, fixture, fixture.Star, fixture.Star, sourcePath, targetPath, traversalPath, Double3.Zero);
    hash = HashResolution(hash, fixture, fixture.Planet, fixture.Star, sourcePath, targetPath, traversalPath, Double3.Zero);
    hash = HashResolution(hash, fixture, fixture.Moon, fixture.Star, sourcePath, targetPath, traversalPath, Double3.UnitX);
    return HashResolution(hash, fixture, fixture.Vessel, fixture.Planet, sourcePath, targetPath, traversalPath, Double3.Zero);
}

static ulong HashResolution(ulong hash, in Fixture fixture, ReferenceFrameId source, ReferenceFrameId target, Span<ReferenceFrameId> sourcePath, Span<ReferenceFrameId> targetPath, Span<ReferenceFrameId> traversalPath, in Double3 position)
{
    var status = ReferenceFrameTransformResolver.TryResolveTransform(fixture.Transforms, source, target, sourcePath, targetPath, traversalPath, out var result);
    if (status != ReferenceFrameTransformResolutionStatus.Success) throw new InvalidOperationException($"Resolution failed: {status}.");
    var converted = result.ConvertPosition(position);
    var velocity = result.ConvertVelocity(position, Double3.Zero);
    var orientation = result.ConvertOrientation(DoubleQuaternion.Identity);
    hash = Mix(hash, (ulong)status); hash = Mix(hash, (ulong)source.Value); hash = Mix(hash, (ulong)target.Value);
    hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(converted.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(converted.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(converted.Z));
    hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(velocity.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(velocity.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(velocity.Z));
    hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.W));
    hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.SourceAngularVelocityInTarget.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.SourceAngularVelocityInTarget.Y)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.SourceAngularVelocityInTarget.Z));
}

static string Format(in Double3 value) => $"({value.X.ToString("G17", CultureInfo.InvariantCulture)},{value.Y.ToString("G17", CultureInfo.InvariantCulture)},{value.Z.ToString("G17", CultureInfo.InvariantCulture)})";
static string FormatQuaternion(in DoubleQuaternion value) => $"({value.X.ToString("G17", CultureInfo.InvariantCulture)},{value.Y.ToString("G17", CultureInfo.InvariantCulture)},{value.Z.ToString("G17", CultureInfo.InvariantCulture)},{value.W.ToString("G17", CultureInfo.InvariantCulture)})";
static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
static void Check(bool condition, string invariant) { if (!condition) throw new InvalidOperationException(invariant); }
static void CheckNear(in Double3 actual, in Double3 expected, string invariant) => Check((actual - expected).LengthSquared <= 1e-18, invariant);

file readonly record struct Fixture(ReferenceFrameTransformSet Transforms, ReferenceFrameId Star, ReferenceFrameId Planet, ReferenceFrameId Moon, ReferenceFrameId Vessel);
