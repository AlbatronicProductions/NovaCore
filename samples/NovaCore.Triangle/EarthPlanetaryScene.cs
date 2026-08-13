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
    internal const int RegionalMaximumLod = PlanetaryEyeballHandoff.RegionalMaximumLod;
    internal const int MaximumPatchCapacity = 8_192;
    internal const double TargetPatchPixels = 128d;
    internal const double ProofViewportHeightPixels = 1_440d;
    internal const double MinimumTerrainClearanceMetres = SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres;
    internal static readonly PlanetaryTerrainDefinition Terrain = PlanetaryTerrainDefinition.EarthAuthoritativeV3;
    internal static readonly PlanetaryEnvironmentPresentation EnvironmentDefinition = PlanetaryEnvironmentPresentation.EarthDataV2;
    internal static readonly PlanetaryLodConfiguration LodConfiguration = PlanetaryLodConfiguration.ForViewport(19d,MaximumLod,TargetPatchPixels,ProofViewportHeightPixels,Math.PI/3d,Terrain.MaximumHeightMetres);
    internal static readonly PlanetaryLodConfiguration RegionalLodConfiguration = PlanetaryLodConfiguration.ForViewport(19d,RegionalMaximumLod,TargetPatchPixels,ProofViewportHeightPixels,Math.PI/3d,Terrain.MaximumHeightMetres);
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
    private int _frustumCulledPatchCount;
    private int _horizonCulledPatchCount;
    private int _splitPatchCount;
    private int _mergedPatchCount;
    private double _altitudeMetres;
    private double _surfaceFrameBlend;
    private PlanetarySurfaceFocus? _surfaceFocus;
    private double _localYawRadians;
    private double _localPitchRadians=-Math.PI/12d;
    private PlanetaryCameraPresentationMode _cameraPresentationMode;
    private float _eyeballWeight;
    private Double3? _eyeballTangentAnchor;

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
    internal PlanetaryCameraPresentationMode CameraPresentationMode => _cameraPresentationMode;
    internal float EyeballWeight => _eyeballWeight;
    internal PlanetarySurfaceFocus? SurfaceFocus => _surfaceFocus;
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
    internal int FrustumCulledPatchCount => _frustumCulledPatchCount;
    internal int HorizonCulledPatchCount => _horizonCulledPatchCount;
    internal int SplitPatchCount => _splitPatchCount;
    internal int MergedPatchCount => _mergedPatchCount;
    internal ReadOnlySpan<PlanetaryPatch> ActiveLeaves => _activeLeaves.AsSpan();
    internal NativePlanetaryMode Mode => _mode;
    internal PlanetaryRepresentationBlend RepresentationBlend => _blend;
    internal bool DetailedComputeRequested => _mode is not NativePlanetaryMode.CpuReference && _blend.DrawDetailed && _eyeballWeight < 1f;
    internal bool EyeballComputeRequested => _blend.DrawDetailed && _eyeballWeight > 0f;
    internal int DistantDrawCount => _blend.DrawDistant ? 1 : 0;
    internal CameraProjection Projection => new(Math.PI / 3d, 16d / 9d, .05d, _earth.RadiusMetres * 100d);

    internal static bool TryCreate(ReferenceFrameId presentationRoot, out EarthPlanetaryScene? scene, out string error)=>TryCreate(presentationRoot,NativePlanetaryMode.CpuReference,MaximumPatchCapacity,out scene,out error);

    internal static bool TryCreate(ReferenceFrameId presentationRoot,NativePlanetaryMode mode,uint gpuOutputCapacity,out EarthPlanetaryScene? scene,out string error)
    {
        scene = null;
        EarthSurfaceDataset.TryLoad(Path.Combine(AppContext.BaseDirectory,"earth-data"),out _);
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
            true,
            CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,SimulationInstant.Zero,out var earthOrientation)?earthOrientation.BodyFixedToInertial:throw new InvalidOperationException("Earth orientation evaluation failed."));
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
        SetDaySideOrbit();
        _surfaceFocus=null;_localYawRadians=0d;_localPitchRadians=-Math.PI/12d;_cameraPresentationMode=PlanetaryCameraPresentationMode.Orbital;
        ApplyOrbitPose(camera);
        return true;
    }

    internal void ApplyPresentationInput(CameraState camera, in NativeInputState input)
    {
        var changed = false;
        if (input.MouseWheelDetents != 0)
        {
            var radial=CurrentSurfaceDirection();var surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,radial,Terrain,EnvironmentDefinition);var altitude=Math.Max(MinimumTerrainClearanceMetres,_orbitDistance-surfaceRadius);
            altitude=Math.Clamp(altitude*Math.Pow(PlanetarySurfaceCameraPolicy.ZoomFactor(altitude),-input.MouseWheelDetents),MinimumTerrainClearanceMetres,_earth.RadiusMetres*LodConfiguration.NearFieldAltitudeRadii);
            _orbitDistance=surfaceRadius+altitude;
            changed = true;
        }
        if (input.LookActive != 0 && (input.MouseDeltaX != 0f || input.MouseDeltaY != 0f))
        {
            if(_cameraPresentationMode==PlanetaryCameraPresentationMode.Orbital){_orbitYawRadians-=input.MouseDeltaX*OrbitSensitivity;_orbitPitchRadians=Math.Clamp(_orbitPitchRadians-input.MouseDeltaY*OrbitSensitivity,-1.45d,1.45d);}
            else{_localYawRadians-=input.MouseDeltaX*OrbitSensitivity;_localPitchRadians=Math.Clamp(_localPitchRadians-input.MouseDeltaY*OrbitSensitivity,PlanetarySurfaceCameraPolicy.MinimumPitchRadians,PlanetarySurfaceCameraPolicy.MaximumPitchRadians);}
            changed = true;
        }
        if(_cameraPresentationMode==PlanetaryCameraPresentationMode.SurfaceLocal&&_surfaceFocus is { } focus&&
            (input.MoveForward!=input.MoveBackward||input.MoveRight!=input.MoveLeft))
        {
            var forwardAxis=(int)input.MoveForward-(int)input.MoveBackward;var rightAxis=(int)input.MoveRight-(int)input.MoveLeft;var length=Math.Sqrt(forwardAxis*forwardAxis+rightAxis*rightAxis);var frame=focus.TangentFrame;
            var forward=(frame.North*Math.Cos(_localYawRadians)+frame.East*Math.Sin(_localYawRadians)).Normalized();var right=(frame.East*Math.Cos(_localYawRadians)-frame.North*Math.Sin(_localYawRadians)).Normalized();
            var currentSurfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,frame.Direction,Terrain,EnvironmentDefinition);var altitude=Math.Max(MinimumTerrainClearanceMetres,_orbitDistance-currentSurfaceRadius);var seconds=Math.Clamp((double)input.DeltaSeconds,0d,.1d);var travel=PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(altitude)*seconds;
            var tangent=(forward*forwardAxis+right*rightAxis)/length;var direction=(frame.Direction+tangent*(travel/currentSurfaceRadius)).Normalized();var nextSurfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,direction,Terrain,EnvironmentDefinition);_surfaceFocus=PlanetarySurfaceFocus.AtDirection(_earth.BodyId,direction,nextSurfaceRadius,altitude);_orbitDistance=nextSurfaceRadius+altitude;changed=true;
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

    internal void SetValidationAltitude(CameraState camera,double altitudeMetres,string surfaceSite="land")
    {
        if(!double.IsFinite(altitudeMetres)||altitudeMetres<MinimumTerrainClearanceMetres)throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        var direction=surfaceSite switch{"land"=>AtLatitudeLongitude(-45d,-70d),"ocean"=>AtLatitudeLongitude(-30d,-90d),"slope"=>AtLatitudeLongitude(-25d,-70d),"mount-st-helens"=>AtLatitudeLongitude(46.1912d,-122.1944d),_=>throw new ArgumentOutOfRangeException(nameof(surfaceSite))};_orbitYawRadians=Math.Atan2(direction.X,direction.Z);_orbitPitchRadians=-Math.Asin(direction.Y);_surfaceFocus=null;_localYawRadians=0d;_localPitchRadians=-Math.PI/12d;_cameraPresentationMode=PlanetaryCameraPresentationMode.Orbital;var surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,direction,Terrain,EnvironmentDefinition);_orbitDistance=surfaceRadius+altitudeMetres;ApplyOrbitPose(camera);
        static Double3 AtLatitudeLongitude(double latitudeDegrees,double longitudeDegrees){var latitude=latitudeDegrees*Math.PI/180d;var longitude=longitudeDegrees*Math.PI/180d;var cosLatitude=Math.Cos(latitude);return new Double3(cosLatitude*Math.Cos(longitude),Math.Sin(latitude),cosLatitude*Math.Sin(longitude));}
    }

    internal void UpdatePatches(CameraState camera)
    {
        var rootToBody=_earth.BodyFixedToRoot.Conjugate().Normalized();var bodyOffset=rootToBody.Rotate(camera.Position.Value-_earth.Position.Value);var distance=Math.Sqrt(bodyOffset.LengthSquared);var radial=distance>0?bodyOffset/distance:Double3.UnitZ;var surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,radial,Terrain,EnvironmentDefinition);_altitudeMetres=distance-surfaceRadius;_altitudeRadii=(distance-_earth.RadiusMetres)/_earth.RadiusMetres;_eyeballWeight=PlanetaryEyeballHandoff.EyeballWeight(_altitudeMetres);if(_eyeballWeight>0f)_eyeballTangentAnchor??=_surfaceFocus?.TangentFrame.Direction??radial;else _eyeballTangentAnchor=null;
        _blend = _handoff.Update(_earth, camera.Position.Value);
        _representation = _blend.DrawDetailed ? PlanetaryRepresentation.NearFieldSurface : PlanetaryRepresentation.FarFieldBody;
        if(!_blend.DrawDetailed||_eyeballWeight>=1f||_mode==NativePlanetaryMode.GpuProduction){_activeLeaves=[];_activePatchCount=0;_minimumActiveLod=0;_maximumActiveLod=0;_refinementCount=0;_balancedRefinementCount=0;_culledPatchCount=0;_frustumCulledPatchCount=0;_horizonCulledPatchCount=0;_splitPatchCount=0;_mergedPatchCount=0;return;}
        var previousLeaves=_activeLeaves;var viewForward=rootToBody.Rotate(camera.Orientation.Rotate(new Double3(0,0,-1)));var localBody=_earth with{Position=new UniversePosition(Double3.Zero,Presentation.RootFrame),BodyFixedToRoot=DoubleQuaternion.Identity};UpdatePatchRecords(PlanetaryRepresentationSelector.SelectPatches(localBody,bodyOffset,RegionalLodConfiguration,viewForward,camera.Projection.VerticalFieldOfViewRadians,camera.Projection.AspectRatio,Math.Max(MinimumTerrainClearanceMetres,_altitudeMetres),previousLeaves),camera.Position.Value);
    }

    internal NativePlanetaryGpuConstants GpuConstants(CameraState camera)
    {
        var rootToBody=_earth.BodyFixedToRoot.Conjugate().Normalized();var cameraBody=rootToBody.Rotate(camera.Position.Value-_earth.Position.Value);var encoded=EncodedPosition.Encode(cameraBody);var radiusHigh=(float)_earth.RadiusMetres;var radiusLow=(float)(_earth.RadiusMetres-radiusHigh);
        var viewForward=rootToBody.Rotate(camera.Orientation.Rotate(new Double3(0,0,-1))).Normalized();var tanY=Math.Tan(camera.Projection.VerticalFieldOfViewRadians*.5d);var halfAngle=Math.Atan(Math.Sqrt(tanY*tanY+tanY*tanY*camera.Projection.AspectRatio*camera.Projection.AspectRatio));
        var radial=cameraBody.Normalized();var surfaceAltitude=Math.Sqrt(cameraBody.LengthSquared)-PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,radial,Terrain,EnvironmentDefinition);
        return new(){CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,RefinementThreshold=(float)RegionalLodConfiguration.MaximumProjectedPatchSpan,NearFieldAltitudeRadii=(float)RegionalLodConfiguration.NearFieldAltitudeRadii,SurfaceAltitudeMetres=(float)Math.Max(MinimumTerrainClearanceMetres,surfaceAltitude),MaximumTerrainHeightMetres=(float)Terrain.MaximumHeightMetres,MaximumLevel=RegionalMaximumLod,OutputCapacity=_gpuOutputCapacity,TerrainVersion=Terrain.Version,ViewForwardX=(float)viewForward.X,ViewForwardY=(float)viewForward.Y,ViewForwardZ=(float)viewForward.Z,ViewHalfAngleRadians=(float)halfAngle,ViewportHeightPixels=(float)ProofViewportHeightPixels,VerticalTanHalfFov=(float)tanY,TargetTexelPixels=(float)EarthSurfaceDemandPolicy.TargetTexelPixels,RequestedAlbedoLevel=1f};
    }

    internal NativePlanetaryEyeball EyeballConstants(CameraState camera)
    {
        if (!EyeballComputeRequested) return default;
        var rootToBody=_earth.BodyFixedToRoot.Conjugate().Normalized();var cameraBody=rootToBody.Rotate(camera.Position.Value-_earth.Position.Value);var encoded=EncodedPosition.Encode(cameraBody);var radiusHigh=(float)_earth.RadiusMetres;var radiusLow=(float)(_earth.RadiusMetres-radiusHigh);var tangentAnchor=_eyeballTangentAnchor??throw new InvalidOperationException("Eyeball rendering requires a fixed body-frame tangent anchor.");
        return new NativePlanetaryEyeball{CameraBodyHighX=encoded.HighX,CameraBodyHighY=encoded.HighY,CameraBodyHighZ=encoded.HighZ,RadiusHigh=radiusHigh,CameraBodyLowX=encoded.LowX,CameraBodyLowY=encoded.LowY,CameraBodyLowZ=encoded.LowZ,RadiusLow=radiusLow,SurfaceAltitudeMetres=(float)Math.Max(MinimumTerrainClearanceMetres,_altitudeMetres),MaximumTerrainHeightMetres=(float)Terrain.MaximumHeightMetres,OceanSeaLevelMetres=(float)EnvironmentDefinition.OceanSeaLevelMetres,BlendAlpha=_eyeballWeight,BodyIdLow=(uint)_earth.BodyId,BodyIdHigh=(uint)(_earth.BodyId>>32),TerrainVersion=Terrain.Version,Enabled=1,TangentAnchorX=(float)tangentAnchor.X,TangentAnchorY=(float)tangentAnchor.Y,TangentAnchorZ=(float)tangentAnchor.Z,MaximumAngleRadians=(float)PlanetaryEyeballTopology.FixedMaximumAngleRadians,RadialWarpExponent=(float)PlanetaryEyeballTopology.RadialWarpExponent,DetailFrequency=1f,NormalStepMetres=2f,RegionalAlpha=1f-_eyeballWeight,VertexCount=PlanetaryEyeballTopology.VertexCount,IndexCount=PlanetaryEyeballTopology.IndexCount,RadialRingCount=PlanetaryEyeballTopology.RadialRingCount,AzimuthSegmentCount=PlanetaryEyeballTopology.AzimuthSegmentCount};
    }

    internal NativePlanetaryPresentation NativePresentation(CameraState camera)
    {
        var center=CubeSphereProjection.CameraRelativeCenter(_earth,new UniversePosition(camera.Position.Value,Presentation.RootFrame));
        var native = new NativePlanetaryPresentation{CenterX=(float)center.X,CenterY=(float)center.Y,CenterZ=(float)center.Z,Radius=(float)_earth.RadiusMetres,ColorR=_earth.Color.X,ColorG=_earth.Color.Y,ColorB=_earth.Color.Z,DistantAlpha=_blend.DistantAlpha,DetailedAlpha=_blend.DetailedAlpha,DistanceRadii=(float)_blend.DistanceRadii,Regime=(NativePlanetaryRenderRegime)_blend.Regime,Enabled=1};
        SolarPlanetMaterials.TryApply(ref native, _earth.BodyId);
        SolarPlanetMaterials.ApplyBodyOrientation(ref native,_earth.BodyFixedToRoot);
        return native;
    }

    internal NativeSolarLighting SolarLighting(CameraState camera)
    {
        if (!_solarLighting.TryEncode(new UniversePosition(camera.Position.Value, Presentation.RootFrame), out var native))
            throw new InvalidOperationException("Earth Solar-lighting transport failed.");
        return native;
    }

    internal NativePlanetaryEnvironment PlanetaryEnvironment(CameraState camera) =>
        EnvironmentDefinition.Encode(_earth,new UniversePosition(camera.Position.Value,Presentation.RootFrame));

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
        _frustumCulledPatchCount = selection.FrustumCulledPatchCount;
        _horizonCulledPatchCount = selection.HorizonCulledPatchCount;
        _splitPatchCount = selection.SplitPatchCount;
        _mergedPatchCount = selection.MergedPatchCount;
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
        var radial=_surfaceFocus?.TangentFrame.Direction??-orbitalOrientation.Rotate(new Double3(0d,0d,-1d));var surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,radial,Terrain,EnvironmentDefinition);_orbitDistance=Math.Max(_orbitDistance,surfaceRadius+MinimumTerrainClearanceMetres);var altitude=_orbitDistance-surfaceRadius;
        _cameraPresentationMode=PlanetarySurfaceCameraPolicy.Mode(altitude);if(_cameraPresentationMode!=PlanetaryCameraPresentationMode.Orbital&&_surfaceFocus is null)_surfaceFocus=PlanetarySurfaceFocus.AtDirection(_earth.BodyId,radial,surfaceRadius,altitude);if(_cameraPresentationMode==PlanetaryCameraPresentationMode.Orbital)_surfaceFocus=null;
        radial=_surfaceFocus?.TangentFrame.Direction??radial;surfaceRadius=PlanetaryTerrainQuery.VisibleSurfaceRadius(_earth.RadiusMetres,radial,Terrain,EnvironmentDefinition);_orbitDistance=surfaceRadius+altitude;
        _surfaceFrameBlend=PlanetarySurfaceCameraPolicy.SurfaceBlend(altitude);var surfaceOrientation=PlanetarySurfaceFrame.AtDirection(radial).LookOrientation(_localYawRadians,_localPitchRadians);camera.Orientation=(_earth.BodyFixedToRoot*Nlerp(orbitalOrientation,surfaceOrientation,_surfaceFrameBlend)).Normalized();
        camera.Position=camera.Position with{Value=_earth.Position.Value+_earth.BodyFixedToRoot.Rotate(radial*_orbitDistance)};
        camera.Validate();
        UpdatePatches(camera);
    }

    private Double3 CurrentSurfaceDirection(){if(_surfaceFocus is { } focus)return focus.TangentFrame.Direction;var yaw=DoubleQuaternion.FromAxisAngle(Double3.UnitY,_orbitYawRadians);var pitch=DoubleQuaternion.FromAxisAngle(Double3.UnitX,_orbitPitchRadians);return -(yaw*pitch).Normalized().Rotate(new Double3(0d,0d,-1d));}
    private void SetDaySideOrbit(){var sun=_earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(_solarLighting.SourceCenter.Value-_earth.Position.Value).Normalized();var radial=new Double3(sun.X,sun.Y*.35d,sun.Z).Normalized();_orbitYawRadians=Math.Atan2(radial.X,radial.Z);_orbitPitchRadians=-Math.Asin(Math.Clamp(radial.Y,-1d,1d));}
    private static DoubleQuaternion Nlerp(in DoubleQuaternion from,in DoubleQuaternion to,double amount){var target=from.X*to.X+from.Y*to.Y+from.Z*to.Z+from.W*to.W<0?new DoubleQuaternion(-to.X,-to.Y,-to.Z,-to.W):to;return new DoubleQuaternion(from.X+(target.X-from.X)*amount,from.Y+(target.Y-from.Y)*amount,from.Z+(target.Z-from.Z)*amount,from.W+(target.W-from.W)*amount).Normalized();}
}
