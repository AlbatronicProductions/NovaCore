using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

internal readonly record struct DynamicFixtureDiagnostics(
    FixtureSceneDiagnostics InitialFixture,
    ulong ScriptedSequenceHash);

internal readonly record struct DynamicFixtureKinematics(
    Double3 MoonLocalPosition,
    Double3 MoonLocalVelocity,
    Double3 VesselLocalPosition,
    Double3 VesselLocalVelocity);

/// <summary>Sample-only prescribed transform motion published through complete immutable render snapshots.</summary>
internal sealed class DynamicReferenceFrameFixtureScene
{
    internal const double MoonRadius = 10d;
    internal const double VesselRadius = 3d;
    internal const double MoonInitialPhase = Math.PI / 2d;
    internal const double VesselInitialPhase = 0d;
    internal const double MoonAngularRate = .20d;
    internal const double VesselAngularRate = .85d;

    private readonly ReferenceFrameId _star = new(1), _planet = new(2), _moon = new(3), _vessel = new(4);
    private readonly ReferenceFrameGraph _graph;
    private readonly ReferenceFrameEvaluation[] _evaluations = new ReferenceFrameEvaluation[4];
    private readonly ResolvedRenderObject[] _objects = new ResolvedRenderObject[4];
    private readonly ReferenceFrameId[] _sourcePath = new ReferenceFrameId[4], _targetPath = new ReferenceFrameId[4], _traversalPath = new ReferenceFrameId[7];
    private readonly SimulationClock _clock;

    private DynamicReferenceFrameFixtureScene()
    {
        var builder = new ReferenceFrameGraphBuilder();
        builder.Add(new ReferenceFrameNode(_star, null, ReferenceFrameKind.Ecl, "fixture-ecl"));
        builder.Add(new ReferenceFrameNode(_planet, _star, ReferenceFrameKind.Cce, "fixture-cce"));
        builder.Add(new ReferenceFrameNode(_moon, _planet, ReferenceFrameKind.Cci, "fixture-cci"));
        builder.Add(new ReferenceFrameNode(_vessel, _moon, ReferenceFrameKind.Ccf, "fixture-ccf"));
        _graph = builder.Build();
        _clock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    }

    public ResolvedRenderSnapshot CurrentSnapshot { get; private set; } = null!;
    public SimulationInstant CurrentTime => _clock.CurrentTime;
    public int GraphConstructionCount => 1;
    public static FixtureCameraConfiguration Camera => StaticReferenceFrameFixtureSceneFactory.Camera;

    public static bool TryCreate(out DynamicReferenceFrameFixtureScene? scene, out DynamicFixtureDiagnostics diagnostics, out string error)
    {
        scene = new DynamicReferenceFrameFixtureScene(); diagnostics = default;
        if (!scene.TryPublishCandidate(SimulationInstant.Zero, false, out error)) { scene = null; return false; }
        var initial = scene.CurrentSnapshot.Objects;
        var fixture = new FixtureSceneDiagnostics(scene._star, initial[0].RootPosition, initial[1].RootPosition, initial[2].RootPosition, initial[3].RootPosition, StaticReferenceFrameFixtureSceneFactory.ComputeSetupHash(initial));
        diagnostics = new DynamicFixtureDiagnostics(fixture, scene.ComputeScriptedSequenceHash()); error = string.Empty; return true;
    }

    public bool TryAdvanceByHostDuration(SimulationDuration hostDuration, out string error)
    {
        if (hostDuration.Ticks < 0) { error = "Dynamic fixture host duration must be nonnegative."; return false; }
        SimulationInstant target;
        try { target = _clock.CurrentTime + hostDuration; }
        catch (OverflowException) { error = "Dynamic fixture time overflow."; return false; }
        var advance = _clock.AdvanceTo(target);
        if (advance.Reason != SimulationAdvanceStopReason.ReachedTarget) { error = $"Dynamic fixture clock advance failed: {advance.Reason}"; return false; }
        return TryPublishCandidate(_clock.CurrentTime, false, out error);
    }

    internal bool TryPublishCandidateForTest(SimulationInstant time, bool forceInvalidMesh, out string error) => TryPublishCandidate(time, forceInvalidMesh, out error);
    internal bool TryBuildCandidateForTest(SimulationInstant time, out ResolvedRenderSnapshot? snapshot, out string error) => TryBuildCandidate(time, false, out snapshot, out error);

    internal static DynamicFixtureKinematics EvaluateKinematics(SimulationInstant time)
    {
        var seconds = time.SecondsSinceEpoch; var moonPhase = MoonInitialPhase + MoonAngularRate * seconds; var vesselPhase = VesselInitialPhase + VesselAngularRate * seconds;
        var moon = CircularPosition(MoonRadius, moonPhase); var vessel = CircularPosition(VesselRadius, vesselPhase);
        return new(moon, CircularVelocity(MoonAngularRate, moon), vessel, CircularVelocity(VesselAngularRate, vessel));
    }

    private bool TryPublishCandidate(SimulationInstant time, bool forceInvalidMesh, out string error)
    {
        if (!TryBuildCandidate(time, forceInvalidMesh, out var candidate, out error) || candidate is null) return false;
        CurrentSnapshot = candidate; error = string.Empty; return true;
    }

    private bool TryBuildCandidate(SimulationInstant time, bool forceInvalidMesh, out ResolvedRenderSnapshot? snapshot, out string error)
    {
        try
        {
            var kinematics = EvaluateKinematics(time); var seconds = time.SecondsSinceEpoch;
            _evaluations[0] = new(_star, new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true));
            _evaluations[1] = new(_planet, new EvaluatedReferenceFrame(new FrameTransform(new Double3(100, 20, 0), DoubleQuaternion.Identity), Double3.Zero, Double3.Zero, true));
            _evaluations[2] = new(_moon, new EvaluatedReferenceFrame(new FrameTransform(kinematics.MoonLocalPosition, DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d + MoonAngularRate * seconds)), kinematics.MoonLocalVelocity, new Double3(0, 0, MoonAngularRate), false));
            _evaluations[3] = new(_vessel, new EvaluatedReferenceFrame(new FrameTransform(kinematics.VesselLocalPosition, DoubleQuaternion.FromAxisAngle(Double3.UnitZ, VesselAngularRate * seconds)), kinematics.VesselLocalVelocity, new Double3(0, 0, VesselAngularRate), false));
            var transforms = new ReferenceFrameTransformSet(_graph, _evaluations);
            if (!TryResolve(transforms, _star, out var star, out error) || !TryResolve(transforms, _planet, out var planet, out error) || !TryResolve(transforms, _moon, out var moon, out error) || !TryResolve(transforms, _vessel, out var vessel, out error)) { snapshot = null; return false; }
            _objects[0] = new(new RenderObjectId(1), new UniversePosition(star.ConvertPosition(Double3.Zero), _star), star.ConvertOrientation(DoubleQuaternion.Identity), new Double3(200, 200, 1), MeshHandle.Triangle);
            _objects[1] = new(new RenderObjectId(2), new UniversePosition(planet.ConvertPosition(Double3.Zero), _star), (planet.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(.35d)).Normalized(), new Double3(125, 125, 1), MeshHandle.Triangle);
            _objects[2] = new(new RenderObjectId(3), new UniversePosition(moon.ConvertPosition(Double3.Zero), _star), (moon.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(.20d)).Normalized(), new Double3(22, 22, 1), MeshHandle.Triangle);
            _objects[3] = new(new RenderObjectId(4), new UniversePosition(vessel.ConvertPosition(Double3.Zero), _star), (vessel.ConvertOrientation(DoubleQuaternion.Identity) * ZRotation(-.35d)).Normalized(), new Double3(16, 16, 1), forceInvalidMesh ? MeshHandle.Invalid : MeshHandle.Triangle);
            if (!ResolvedRenderSnapshot.TryCreate(_objects, out snapshot, out var status) || snapshot is null) { error = $"Dynamic fixture snapshot failed: {status}"; return false; }
            error = string.Empty; return true;
        }
        catch (ArgumentException exception) { snapshot = null; error = $"Dynamic fixture construction failed: {exception.Message}"; return false; }
    }

    private bool TryResolve(ReferenceFrameTransformSet transforms, ReferenceFrameId source, out ResolvedReferenceFrameTransform resolved, out string error)
    {
        var status = ReferenceFrameTransformResolver.TryResolveTransform(transforms, source, _star, _sourcePath, _targetPath, _traversalPath, out resolved);
        error = status == ReferenceFrameTransformResolutionStatus.Success ? string.Empty : $"Dynamic fixture resolution failed: {source.Value}->{_star.Value}: {status}"; return status == ReferenceFrameTransformResolutionStatus.Success;
    }

    private ulong ComputeScriptedSequenceHash()
    {
        ulong hash = 14695981039346656037UL;
        foreach (var seconds in new long[] { 0, 1, 5, 10, 100 })
        {
            var time = SimulationInstant.FromWholeSeconds(seconds);
            if (!TryBuildCandidate(time, false, out var snapshot, out _ ) || snapshot is null) throw new InvalidOperationException("Dynamic fixture sequence candidate failed.");
            hash = Mix(hash, (ulong)time.Ticks); hash = Mix(hash, StaticReferenceFrameFixtureSceneFactory.ComputeSetupHash(snapshot.Objects));
        }
        return hash;
    }

    private static Double3 CircularPosition(double radius, double phase) => new(radius * Math.Cos(phase), radius * Math.Sin(phase), 0d);
    private static Double3 CircularVelocity(double angularRate, in Double3 position) => new(-angularRate * position.Y, angularRate * position.X, 0d);
    private static DoubleQuaternion ZRotation(double radians) => DoubleQuaternion.FromAxisAngle(Double3.UnitZ, radians);
    private static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
}
