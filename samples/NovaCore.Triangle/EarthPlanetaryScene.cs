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
    internal const double InitialOrbitDistanceRadii = 3d;
    private const double OrbitSensitivity = .002d;
    private readonly PlanetRenderProxy _earth;
    private double _orbitDistance;
    private double _orbitYawRadians;
    private double _orbitPitchRadians;

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
        var selection = PlanetaryRepresentationSelector.SelectPatches(earth, initialCamera, new PlanetaryLodConfiguration(4d, 0));
        if (selection.Representation != PlanetaryRepresentation.NearFieldSurface || selection.Patches.Length != 6)
        { error = "Earth root-patch selection failed."; return false; }

        var patches = new NativePlanetaryPatch[selection.Patches.Length];
        for (var index = 0; index < patches.Length; index++)
        {
            var patch = selection.Patches[index];
            patches[index] = new NativePlanetaryPatch
            {
                Face = (uint)patch.Face,
                Level = (uint)patch.Level,
                X = (uint)patch.X,
                Y = (uint)patch.Y,
                Radius = (float)earth.RadiusMetres,
                ColorR = earth.Color.X,
                ColorG = earth.Color.Y,
                ColorB = earth.Color.Z,
                ColorA = 1f,
            };
        }

        scene = new EarthPlanetaryScene(presentation, earth, patches);
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
                _earth.RadiusMetres * 100d);
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
        var center = CubeSphereProjection.CameraRelativeCenter(_earth, new UniversePosition(camera.Position.Value, Presentation.RootFrame));
        foreach (ref var patch in Patches.AsSpan())
        {
            patch.CenterX = (float)center.X;
            patch.CenterY = (float)center.Y;
            patch.CenterZ = (float)center.Z;
        }
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
