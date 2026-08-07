using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

internal sealed class EarthPlanetaryScene
{
    internal static readonly Float3 EarthColor = new(.08f, .32f, .72f);
    internal const double InitialOrbitDistanceRadii = 20d;
    internal const int MaximumLod = 5;
    internal static readonly PlanetaryLodConfiguration LodConfiguration = new(19d, MaximumLod, .11d);
    private const double OrbitSensitivity = .002d;
    private readonly PlanetRenderProxy _earth;
    private PlanetaryPatch[] _activeLeaves = [];
    private double _orbitDistance;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;
    private int _activePatchCount;
    private int _minimumActiveLod;
    private int _maximumActiveLod;
    private PlanetaryRepresentation _representation;
    private double _altitudeRadii;
    private int _refinementCount;
    private int _balancedRefinementCount;
    private int _culledPatchCount;

    private EarthPlanetaryScene(PlanetaryPresentationSnapshot presentation, in PlanetRenderProxy earth, NativePlanetaryPatch[] patches)
    {
        Presentation = presentation;
        _earth = earth;
        Patches = patches;
        _orbitDistance = earth.RadiusMetres * InitialOrbitDistanceRadii;
    }

    internal PlanetaryPresentationSnapshot Presentation { get; }
    internal NativePlanetaryPatch[] Patches { get; }
    internal PlanetRenderProxy Earth => _earth;
    internal double OrbitDistance => _orbitDistance;
    internal int ActivePatchCount => _activePatchCount;
    internal int MinimumActiveLod => _minimumActiveLod;
    internal int MaximumActiveLod => _maximumActiveLod;
    internal PlanetaryRepresentation Representation => _representation;
    internal double AltitudeRadii => _altitudeRadii;
    internal int RefinementCount => _refinementCount;
    internal int BalancedRefinementCount => _balancedRefinementCount;
    internal int CulledPatchCount => _culledPatchCount;
    internal ReadOnlySpan<PlanetaryPatch> ActiveLeaves => _activeLeaves.AsSpan();
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d, Math.Max(1d, _earth.RadiusMetres / 10_000d), _earth.RadiusMetres * 100d);

    internal static bool TryCreate(ReferenceFrameId presentationRoot, out EarthPlanetaryScene? scene, out string error)
    {
        scene = null;
        var system = SolAnalyticalDefinition.Instance;
        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var evaluation = CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.Zero, evaluations, roots, staging, stagingRoots);
        if (!evaluation.Succeeded) { error = $"SolAnalytical evaluation failed: {evaluation.Status}"; return false; }

        var earthIndex = -1;
        for (var index = 0; index < system.Count; index++) if (system.GetNodeInTraversalOrder(index).Id == SolarSystemBodyIds.Earth) { earthIndex = index; break; }
        if (earthIndex < 0 || !system.TryGetBody(SolarSystemBodyIds.Earth, out var catalogEarth)) { error = "SolAnalytical Earth body is missing."; return false; }

        var evaluatedEarth = new EvaluatedPlanetaryBody(
            catalogEarth.Id.Value,
            new UniversePosition(roots[earthIndex].Translation, presentationRoot),
            catalogEarth.PhysicalProperties.MeanRadius,
            EarthColor,
            catalogEarth.Identity.DisplayName,
            true);
        if (!PlanetaryBodyPresentationProvider.TryCreateSnapshot([evaluatedEarth], out var presentation) || presentation is null || !presentation.TryGetBody(catalogEarth.Id.Value, out var earth))
        { error = "Earth planetary presentation publication failed."; return false; }

        var initialCamera = earth.Position.Value + new Double3(0d, 0d, earth.RadiusMetres * InitialOrbitDistanceRadii);
        var selection = PlanetaryRepresentationSelector.SelectPatches(earth, initialCamera, LodConfiguration);
        if (selection.Representation != PlanetaryRepresentation.NearFieldSurface || selection.Patches.Length != 6)
        { error = "Earth root-patch selection failed."; return false; }

        var patches = new NativePlanetaryPatch[6 * (1 << (2 * MaximumLod))];
        scene = new EarthPlanetaryScene(presentation, earth, patches);
        scene.UpdatePatchRecords(selection, initialCamera);
        error = string.Empty;
        return true;
    }

    internal bool TryFocus(CameraState camera)
    {
        if (!PlanetaryCameraFocus.TryFocus(camera, Presentation, _earth.BodyId, _orbitDistance)) return false;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = 0d;
        camera.Orientation = DoubleQuaternion.Identity;
        camera.Validate();
        UpdatePatches(camera);
        return true;
    }

    internal void ApplyPresentationInput(CameraState camera, in NativeInputState input)
    {
        var changed = false;
        if (input.MouseWheelDetents != 0)
        {
            _orbitDistance = Math.Clamp(
                _orbitDistance * Math.Pow(1.1d, -input.MouseWheelDetents),
                _earth.RadiusMetres * 1.05d,
                _earth.RadiusMetres * (LodConfiguration.NearFieldAltitudeRadii + 1d));
            changed = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            _orbitYawRadians -= input.MouseDeltaX * OrbitSensitivity;
            _orbitPitchRadians = Math.Clamp(_orbitPitchRadians - input.MouseDeltaY * OrbitSensitivity, -1.45d, 1.45d);
            changed = true;
        }
        if (changed) ApplyOrbitPose(camera);
    }

    internal void ResetPresentationCamera(CameraState camera)
    {
        _orbitDistance = _earth.RadiusMetres * InitialOrbitDistanceRadii;
        _orbitYawRadians = 0d;
        _orbitPitchRadians = 0d;
        if (!TryFocus(camera)) throw new InvalidOperationException("Earth focus reset failed.");
    }

    internal void UpdatePatches(CameraState camera)
    {
        UpdatePatchRecords(PlanetaryRepresentationSelector.SelectPatches(_earth, camera.Position.Value, LodConfiguration), camera.Position.Value);
    }

    private void UpdatePatchRecords(in PlanetaryLodSelection selection, in Double3 cameraRootPosition)
    {
        _representation = selection.Representation;
        _altitudeRadii = (Math.Sqrt((cameraRootPosition - _earth.Position.Value).LengthSquared) - _earth.RadiusMetres) / _earth.RadiusMetres;
        _activeLeaves = selection.Patches;
        _activePatchCount = selection.Patches.Length;
        _minimumActiveLod = _activePatchCount == 0 ? 0 : selection.Patches.Min(patch => patch.Level);
        _maximumActiveLod = _activePatchCount == 0 ? 0 : selection.Patches.Max(patch => patch.Level);
        _refinementCount = selection.RefinementCount;
        _balancedRefinementCount = selection.BalancedRefinementCount;
        _culledPatchCount = selection.CulledPatchCount;
        if (_activePatchCount > Patches.Length) throw new InvalidOperationException("Earth patch capacity exceeded.");
        var center = CubeSphereProjection.CameraRelativeCenter(_earth, new UniversePosition(cameraRootPosition, Presentation.RootFrame));
        for (var index = 0; index < _activePatchCount; index++)
        {
            var leaf = selection.Patches[index];
            ref var patch = ref Patches[index];
            var color = DebugColor(leaf);
            patch.Face = (uint)leaf.Face;
            patch.Level = (uint)leaf.Level;
            patch.X = (uint)leaf.X;
            patch.Y = (uint)leaf.Y;
            patch.CenterX = (float)center.X;
            patch.CenterY = (float)center.Y;
            patch.CenterZ = (float)center.Z;
            patch.Radius = (float)_earth.RadiusMetres;
            patch.ColorR = color.X;
            patch.ColorG = color.Y;
            patch.ColorB = color.Z;
            patch.ColorA = 1f;
            patch.StitchMask = (uint)selection.StitchMasks[index];
            patch.Reserved0 = 0;
            patch.Reserved1 = 0;
            patch.Reserved2 = 0;
        }
    }

    private static Float3 DebugColor(in PlanetaryPatch patch)
    {
        var baseColor = patch.Face switch
        {
            CubeSphereFace.PositiveX => new Float3(.16f, .42f, .92f),
            CubeSphereFace.NegativeX => new Float3(.10f, .68f, .88f),
            CubeSphereFace.PositiveY => new Float3(.20f, .82f, .45f),
            CubeSphereFace.NegativeY => new Float3(.08f, .48f, .28f),
            CubeSphereFace.PositiveZ => new Float3(.22f, .34f, .78f),
            _ => new Float3(.12f, .24f, .56f),
        };
        var brightness = .62f + .06f * patch.Level + (((patch.X ^ patch.Y) & 1) == 0 ? .10f : -.04f);
        return new Float3(baseColor.X * brightness, baseColor.Y * brightness, baseColor.Z * brightness);
    }

    private void ApplyOrbitPose(CameraState camera)
    {
        var yaw = DoubleQuaternion.FromAxisAngle(Double3.UnitY, _orbitYawRadians);
        var pitch = DoubleQuaternion.FromAxisAngle(Double3.UnitX, _orbitPitchRadians);
        var orientation = (yaw * pitch).Normalized();
        var forward = orientation.Rotate(new Double3(0d, 0d, -1d));
        camera.Orientation = orientation;
        camera.Position = camera.Position with { Value = _earth.Position.Value - forward * _orbitDistance };
        camera.Validate();
        UpdatePatches(camera);
    }
}
