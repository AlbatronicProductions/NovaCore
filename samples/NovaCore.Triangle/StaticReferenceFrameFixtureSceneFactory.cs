using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;

internal readonly record struct FixtureSceneDiagnostics(
    ReferenceFrameId RootFrame,
    UniversePosition Star,
    UniversePosition Planet,
    UniversePosition Moon,
    UniversePosition Vessel,
    ulong SetupHash);

internal readonly record struct FixtureCameraConfiguration(
    Double3 Position,
    CameraProjection Projection,
    double MovementSpeed);

internal readonly record struct StaticReferenceFrameFixtureScene(
    ResolvedRenderSnapshot Snapshot,
    FixtureSceneDiagnostics Diagnostics);

/// <summary>Sample-only static presentation of the 6D-2 reference-frame fixture.</summary>
internal static class StaticReferenceFrameFixtureSceneFactory
{
    // Identity looks along local -Z in the existing camera convention. Values frame all four static markers at 16:9 and 3440x1440.
    public static FixtureCameraConfiguration Camera => new(new Double3(50, 16, 70), new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1000d), .1d);

    public static bool TryCreate(out StaticReferenceFrameFixtureScene scene, out string error)
    {
        try
        {
            var star = new ReferenceFrameId(1); var planet = new ReferenceFrameId(2); var moon = new ReferenceFrameId(3); var vessel = new ReferenceFrameId(4);
            var graphBuilder = new ReferenceFrameGraphBuilder();
            graphBuilder.Add(new ReferenceFrameNode(star, null, ReferenceFrameKind.Ecl, "fixture-ecl"));
            graphBuilder.Add(new ReferenceFrameNode(planet, star, ReferenceFrameKind.Cce, "fixture-cce"));
            graphBuilder.Add(new ReferenceFrameNode(moon, planet, ReferenceFrameKind.Cci, "fixture-cci"));
            graphBuilder.Add(new ReferenceFrameNode(vessel, moon, ReferenceFrameKind.Ccf, "fixture-ccf"));
            var graph = graphBuilder.Build();
            var transforms = new ReferenceFrameTransformSet(graph,
            [
                new ReferenceFrameEvaluation(star, new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true)),
                new ReferenceFrameEvaluation(planet, new EvaluatedReferenceFrame(new FrameTransform(new Double3(100, 20, 0), DoubleQuaternion.Identity), new Double3(1, 0, 0), Double3.Zero, true)),
                new ReferenceFrameEvaluation(moon, new EvaluatedReferenceFrame(new FrameTransform(new Double3(0, 10, 0), DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d)), new Double3(0, 2, 0), new Double3(0, 0, .5d), false)),
                new ReferenceFrameEvaluation(vessel, new EvaluatedReferenceFrame(new FrameTransform(new Double3(2, 0, 0), DoubleQuaternion.Identity), Double3.Zero, Double3.Zero, false)),
            ]);
            Span<ReferenceFrameId> sourcePath = stackalloc ReferenceFrameId[graph.Count]; Span<ReferenceFrameId> targetPath = stackalloc ReferenceFrameId[graph.Count]; Span<ReferenceFrameId> traversalPath = stackalloc ReferenceFrameId[graph.Count * 2 - 1];
            if (!TryResolve(transforms, star, star, sourcePath, targetPath, traversalPath, out var starTransform, out error) ||
                !TryResolve(transforms, planet, star, sourcePath, targetPath, traversalPath, out var planetTransform, out error) ||
                !TryResolve(transforms, moon, star, sourcePath, targetPath, traversalPath, out var moonTransform, out error) ||
                !TryResolve(transforms, vessel, star, sourcePath, targetPath, traversalPath, out var vesselTransform, out error)) { scene = default; return false; }
            var starPosition = new UniversePosition(starTransform.ConvertPosition(Double3.Zero), star); var planetPosition = new UniversePosition(planetTransform.ConvertPosition(Double3.Zero), star); var moonPosition = new UniversePosition(moonTransform.ConvertPosition(Double3.Zero), star); var vesselPosition = new UniversePosition(vesselTransform.ConvertPosition(Double3.Zero), star);
            var objects = new ResolvedRenderObject[]
            {
                new(new RenderObjectId(1), starPosition, starTransform.ConvertOrientation(DoubleQuaternion.Identity), new Double3(200,200,1), MeshHandle.Triangle),
                new(new RenderObjectId(2), planetPosition, (planetTransform.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(.35d)).Normalized(), new Double3(125,125,1), MeshHandle.Triangle),
                new(new RenderObjectId(3), moonPosition, (moonTransform.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(.20d)).Normalized(), new Double3(22,22,1), MeshHandle.Triangle),
                new(new RenderObjectId(4), vesselPosition, (vesselTransform.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(-.35d)).Normalized(), new Double3(16,16,1), MeshHandle.Triangle),
            };
            if (!ResolvedRenderSnapshot.TryCreate(objects, out var snapshot, out var snapshotStatus) || snapshot is null) { scene = default; error = $"Fixture snapshot failed: {snapshotStatus}"; return false; }
            var diagnostics = new FixtureSceneDiagnostics(star, starPosition, planetPosition, moonPosition, vesselPosition, ComputeSetupHash(objects)); scene = new StaticReferenceFrameFixtureScene(snapshot, diagnostics); error = string.Empty; return true;
        }
        catch (ArgumentException exception) { scene = default; error = $"Fixture construction failed: {exception.Message}"; return false; }
    }

    private static bool TryResolve(ReferenceFrameTransformSet transforms, ReferenceFrameId source, ReferenceFrameId target, Span<ReferenceFrameId> sourcePath, Span<ReferenceFrameId> targetPath, Span<ReferenceFrameId> traversalPath, out ResolvedReferenceFrameTransform resolved, out string error)
    {
        var status = ReferenceFrameTransformResolver.TryResolveTransform(transforms, source, target, sourcePath, targetPath, traversalPath, out resolved); error = status == ReferenceFrameTransformResolutionStatus.Success ? string.Empty : $"Fixture resolution failed: {source.Value}->{target.Value}: {status}"; return status == ReferenceFrameTransformResolutionStatus.Success;
    }

    internal static ulong ComputeSetupHash(ReadOnlySpan<ResolvedRenderObject> objects)
    {
        ulong hash = 14695981039346656037UL;
        foreach (ref readonly var value in objects)
        {
            hash = Mix(hash, value.Id.Value); hash = Mix(hash, (ulong)value.RootPosition.Frame.Value); hash = MixDouble3(hash, value.RootPosition.Value); hash = MixQuaternion(hash, value.RootOrientation); hash = MixDouble3(hash, value.Scale); hash = Mix(hash, value.Mesh.Value);
        }
        return hash;
    }

    private static DoubleQuaternion ZRotation(double radians) => DoubleQuaternion.FromAxisAngle(Double3.UnitZ, radians);
    private static ulong MixDouble3(ulong hash, in Double3 value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); }
    private static ulong MixQuaternion(ulong hash, in DoubleQuaternion value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.W)); }
    private static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
}
