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
    internal const int MaximumLod = 22;
    internal const int MaximumPatchCapacity = 8_192;
    internal const double TargetPatchPixels = 128d;
    internal const double ProofViewportHeightPixels = 1_440d;
    internal const double MinimumTerrainClearanceMetres = 2d;
    internal static readonly PlanetaryTerrainDefinition Terrain = PlanetaryTerrainDefinition.EarthProceduralV1;
    internal static readonly PlanetaryLodConfiguration LodConfiguration = PlanetaryLodConfiguration.ForViewport(19d,MaximumLod,TargetPatchPixels,ProofViewportHeightPixels,Math.PI/3d,Terrain.MaximumHeightMetres);
    internal static readonly PlanetaryRepresentationHandoffConfiguration HandoffConfiguration = new(12d, 18d, .25d);
    private const double OrbitSensitivity = .002d;
    private readonly PlanetRenderProxy _earth;
    private readonly SolarLightingPresentation _solarLighting;
    private readonly NativePlanetaryMode _mode;
    private readonly uint _gpuOutputCapacity;
    private readonly PlanetaryRepresentationHandoff _handoff = new(HandoffConfiguration);
    private PlanetaryRepresentationBlend _blend;
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
    private double _altitudeMetres;
    private double _surfaceFrameBlend;

    private EarthPlanetaryScene(PlanetaryPresentationSnapshot presentation, in PlanetRenderProxy earth, in SolarLightingPresentation solarLighting, NativePlanetaryPatch[] patches,NativePlanetaryMode mode,uint gpuOutputCapacity)
    {
        Presentation = presentation;
        _earth = earth;
        _solarLighting = solarLighting;
        Patches = patches;
        _mode = mode;
        _gpuOutputCapacity = gpuOutputCapacity;
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
    internal double AltitudeMetres => _altitudeMetres;
    internal double SurfaceFrameBlend => _surfaceFrameBlend;
    internal int RefinementCount => _refinementCount;
    internal int BalancedRefinementCount => _balancedRefinementCount;
    internal int CulledPatchCount => _culledPatchCount;
    internal ReadOnlySpan<PlanetaryPatch> ActiveLeaves => _activeLeaves.AsSpan();
    internal NativePlanetaryMode Mode => _mode;
    internal PlanetaryRepresentationBlend RepresentationBlend => _blend;
    internal bool DetailedComputeRequested => _mode is not NativePlanetaryMode.CpuReference && _blend.DrawDetailed;
    internal int DistantDrawCount => _blend.DrawDistant ? 1 : 0;
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d, .05d, _earth.RadiusMetres * 100d);

    internal static bool TryCreate(ReferenceFrameId presentationRoot, out EarthPlanetaryScene? scene, out string error)=>TryCreate(presentationRoot,NativePlanetaryMode.CpuReference,MaximumPatchCapacity,out scene,out error);

    internal static bool TryCreate(ReferenceFrameId presentationRoot,NativePlanetaryMode mode,uint gpuOutputCapacity,out EarthPlanetaryScene? scene,out string error)
    {
        scene = null;
        if(mode>NativePlanetaryMode.CpuGpuValidation||gpuOutputCapacity is 0 or >MaximumPatchCapacity){error="Invalid planetary renderer mode or GPU capacity.";return false;}
        var system = SolAnalyticalDefinition.Instance;
        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var evaluation = CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.Zero, evaluations, roots, staging, stagingRoots);
        if (!evaluation.Succeeded) { error = $"SolAnalytical evaluation failed: {evaluation.Status}"; return false; }

        var earthIndex = -1;
        var sunIndex = -1;
        for (var index = 0; index < system.Count; index++)
        {
            var id = system.GetNodeInTraversalOrder(index).Id;
            if (id == SolarSystemBodyIds.Earth) earthIndex = index;
            else if (id == SolarSystemBodyIds.Sun) sunIndex = index;
        }
        if (earthIndex < 0 || sunIndex < 0 || !system.TryGetBody(SolarSystemBodyIds.Earth, out var catalogEarth)) { error = "SolAnalytical Sun or Earth body is missing."; return false; }

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
        var patches = new NativePlanetaryPatch[MaximumPatchCapacity];
        var solarLighting = SolarLightingPresentation.CreateDefault(new UniversePosition(roots[sunIndex].Translation, presentationRoot));
        scene = new EarthPlanetaryScene(presentation, earth, solarLighting, patches,mode,gpuOutputCapacity);
        scene.UpdatePatches(new CameraState(new FramePosition(presentationRoot,initialCamera),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free));
        if(scene.RepresentationBlend.Regime!=PlanetaryRenderRegime.DistantOnly||scene.ActivePatchCount!=0){scene=null;error="Earth distant-representation selection failed.";return false;}
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
            var radial=CurrentSurfaceDirection();var surfaceRadius=PlanetaryTerrainQuery.SurfaceRadius(_earth.RadiusMetres,radial,Terrain);var altitude=Math.Max(MinimumTerrainClearanceMetres,_orbitDistance-surfaceRadius);
            altitude=Math.Clamp(altitude*Math.Pow(2d,-input.MouseWheelDetents),MinimumTerrainClearanceMetres,_earth.RadiusMetres*LodConfiguration.NearFieldAltitudeRadii);
            _orbitDistance=surfaceRadius+altitude;
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
        var bodyOffset=camera.Position.Value-_earth.Position.Value;var distance=Math.Sqrt(bodyOffset.LengthSquared);var radial=distance>0?bodyOffset/distance:Double3.UnitZ;var surfaceRadius=PlanetaryTerrainQuery.SurfaceRadius(_earth.RadiusMetres,radial,Terrain);_altitudeMetres=distance-surfaceRadius;_altitudeRadii=(distance-_earth.RadiusMetres)/_earth.RadiusMetres;
        _blend = _handoff.Update(_earth, camera.Position.Value);
        _representation = _blend.DrawDetailed ? PlanetaryRepresentation.NearFieldSurface : PlanetaryRepresentation.FarFieldBody;
        if(!_blend.DrawDetailed||_mode==NativePlanetaryMode.GpuProduction){_activeLeaves=[];_activePatchCount=0;_minimumActiveLod=0;_maximumActiveLod=0;_refinementCount=0;_balancedRefinementCount=0;_culledPatchCount=0;return;}
        var viewForward=camera.Orientation.Rotate(new Double3(0,0,-1));UpdatePatchRecords(PlanetaryRepresentationSelector.SelectPatches(_earth,camera.Position.Value,LodConfiguration,viewForward,camera.Projection.VerticalFieldOfViewRadians,camera.Projection.AspectRatio,Math.Max(MinimumTerrainClearanceMetres,_altitudeMetres)),camera.Position.Value);
    }

    internal NativePlanetaryGpuConstants GpuConstants(CameraState camera)
    {
        var cameraBody=camera.Position.Value-_earth.Position.Value;var encoded=EncodedPosition.Encode(cameraBody);var radiusHigh=(float)_earth.RadiusMetres;var radiusLow=(float)(_earth.RadiusMetres-radiusHigh);
        var viewForward=camera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();var tanY=Math.Tan(camera.Projection.VerticalFieldOfViewRadians*.5d);var halfAngle=Math.Atan(Math.Sqrt(tanY*tanY+tanY*tanY*camera.Projection.AspectRatio*camera.Projection.AspectRatio));
        var radial=cameraBody.Normalized();var surfaceAltitude=Math.Sqrt(cameraBody.LengthSquared)-PlanetaryTerrainQuery.SurfaceRadius(_earth.RadiusMetres,radial,Terrain);
        return new(){CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,RefinementThreshold=(float)LodConfiguration.MaximumProjectedPatchSpan,NearFieldAltitudeRadii=(float)LodConfiguration.NearFieldAltitudeRadii,SurfaceAltitudeMetres=(float)Math.Max(MinimumTerrainClearanceMetres,surfaceAltitude),MaximumTerrainHeightMetres=(float)Terrain.MaximumHeightMetres,MaximumLevel=MaximumLod,OutputCapacity=_gpuOutputCapacity,TerrainVersion=Terrain.Version,ViewForwardX=(float)viewForward.X,ViewForwardY=(float)viewForward.Y,ViewForwardZ=(float)viewForward.Z,ViewHalfAngleRadians=(float)halfAngle};
    }

    internal NativePlanetaryPresentation NativePresentation(CameraState camera)
    {
        var center=CubeSphereProjection.CameraRelativeCenter(_earth,new UniversePosition(camera.Position.Value,Presentation.RootFrame));
        var native = new NativePlanetaryPresentation{CenterX=(float)center.X,CenterY=(float)center.Y,CenterZ=(float)center.Z,Radius=(float)_earth.RadiusMetres,ColorR=_earth.Color.X,ColorG=_earth.Color.Y,ColorB=_earth.Color.Z,DistantAlpha=_blend.DistantAlpha,DetailedAlpha=_blend.DetailedAlpha,DistanceRadii=(float)_blend.DistanceRadii,Regime=(NativePlanetaryRenderRegime)_blend.Regime,Enabled=1};
        SolarPlanetMaterials.TryApply(ref native, _earth.BodyId);
        return native;
    }

    internal NativeSolarLighting SolarLighting(CameraState camera)
    {
        if (!_solarLighting.TryEncode(new UniversePosition(camera.Position.Value, Presentation.RootFrame), out var native))
            throw new InvalidOperationException("Earth Solar-lighting transport failed.");
        return native;
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
            var color = EarthColor;
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
            patch.ColorA = _blend.DetailedAlpha;
            patch.StitchMask = (uint)selection.StitchMasks[index];
            patch.Reserved0 = 0;
            patch.Reserved1 = 0;
            patch.Reserved2 = 0;
        }
    }

    private void ApplyOrbitPose(CameraState camera)
    {
        var yaw = DoubleQuaternion.FromAxisAngle(Double3.UnitY, _orbitYawRadians);
        var pitch = DoubleQuaternion.FromAxisAngle(Double3.UnitX, _orbitPitchRadians);
        var orbitalOrientation = (yaw * pitch).Normalized();
        var radial=-orbitalOrientation.Rotate(new Double3(0d,0d,-1d));var surfaceRadius=PlanetaryTerrainQuery.SurfaceRadius(_earth.RadiusMetres,radial,Terrain);_orbitDistance=Math.Max(_orbitDistance,surfaceRadius+MinimumTerrainClearanceMetres);var altitude=_orbitDistance-surfaceRadius;
        _surfaceFrameBlend=SmoothStep(1_000_000d,100_000d,altitude);var surfaceOrientation=PlanetarySurfaceFrame.AtDirection(radial).HorizonViewOrientation();camera.Orientation=Nlerp(orbitalOrientation,surfaceOrientation,_surfaceFrameBlend);
        camera.Position = camera.Position with { Value = _earth.Position.Value + radial * _orbitDistance };
        camera.Validate();
        UpdatePatches(camera);
    }

    private Double3 CurrentSurfaceDirection(){var yaw=DoubleQuaternion.FromAxisAngle(Double3.UnitY,_orbitYawRadians);var pitch=DoubleQuaternion.FromAxisAngle(Double3.UnitX,_orbitPitchRadians);return -(yaw*pitch).Normalized().Rotate(new Double3(0d,0d,-1d));}
    private static double SmoothStep(double high,double low,double value){var t=Math.Clamp((high-value)/(high-low),0d,1d);return t*t*(3d-2d*t);}
    private static DoubleQuaternion Nlerp(in DoubleQuaternion from,in DoubleQuaternion to,double amount){var target=from.X*to.X+from.Y*to.Y+from.Z*to.Z+from.W*to.W<0?new DoubleQuaternion(-to.X,-to.Y,-to.Z,-to.W):to;return new DoubleQuaternion(from.X+(target.X-from.X)*amount,from.Y+(target.Y-from.Y)*amount,from.Z+(target.Z-from.Z)*amount,from.W+(target.W-from.W)*amount).Normalized();}
}
