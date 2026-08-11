using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Guidance;
using NovaCore.Simulation.Time;

var tests = new (string, Action)[]
{
    ("MeshHandle", MeshHandleTest),
    ("Transport layout", LayoutTest),
    ("Transform conversion", TransformTest),
    ("Camera relative", RelativeTest),
    ("Batches and capacity", BatchTest),
    ("Resolved render transport", ResolvedTransportTest),
    ("Orbit curve transport", OrbitCurveTransportTest),
    ("Static reference-frame fixture transport", StaticReferenceFrameFixtureTransportTest),
    ("Dynamic reference-frame fixture publication", DynamicReferenceFrameFixturePublicationTest),
    ("Celestial analytical fixture publication", CelestialAnalyticalFixturePublicationTest),
    ("Celestial player torque controls", CelestialPlayerTorqueControlsTest),
    ("Celestial SAS mode selection", CelestialSasModeSelectionTest),
    ("Celestial SAS control cadence", CelestialSasControlCadenceTest),
    ("Celestial SAS convergence", CelestialSasConvergenceTest),
    ("Celestial SAS diagnostic indicators", CelestialSasDiagnosticIndicatorsTest),
    ("Camera snapshot allocation", CameraSnapshotAllocationTest),
    ("Planetary presentation pipeline", PlanetaryPresentationPipelineTest),
    ("Planetary presentation SPIR-V stride", PlanetaryPresentationSpirvStrideTest),
    ("Focus target authority", FocusTargetAuthorityTest),
    ("Planet material presentation", PlanetMaterialPresentationTest),
    ("Planet micro-normal foundation", PlanetMicroNormalFoundationTest),
    ("Planet surface scatter placement", PlanetarySurfaceScatterPlacementTest),
    ("Planetary environment presentation", PlanetaryEnvironmentPresentationTest),
    ("Earth authoritative presentation dataset", EarthAuthoritativeDatasetTest),
    ("SurfaceAnchor acquisition, ENU, and handoff", SurfaceAnchorPhaseBTest),
    ("Camera focus-position continuity", CameraFocusPositionContinuityTest),
    ("Cube-sphere planetary surface", CubeSpherePlanetarySurfaceTest),
    ("Planetary terrain residency and surface frame", PlanetaryTerrainResidencyAndSurfaceFrameTest),
    ("Planetary patch topology and ABI", PlanetaryPatchTopologyAndAbiTest),
    ("Planetary Renderer V2 eyeball topology and ABI", PlanetaryEyeballTopologyAndAbiTest),
    ("Spatial terrain continuity and demand", SpatialTerrainContinuityAndDemandTest),
    ("Planetary representation handoff", PlanetaryRepresentationHandoffTest),
    ("Distant quaternion transform parity", DistantQuaternionTransformParityTest),
    ("Distant visible hemisphere winding", DistantVisibleHemisphereWindingTest),
    ("Sol system presentation and focus", SolarSystemSceneTest),
    ("SolAnalytical Earth planetary scene", EarthPlanetarySceneTest),
};
var testFilter=args.FirstOrDefault(argument=>argument.StartsWith("--test=",StringComparison.OrdinalIgnoreCase))?[7..];
foreach (var (name, test) in tests) if(testFilter is null||name.Contains(testFilter,StringComparison.OrdinalIgnoreCase)){test();Console.WriteLine($"PASS {name}");}

static void SpatialTerrainContinuityAndDemandTest()
{
    var root=new ReferenceFrameId(1);const double radius=6_371_008.8d;var body=new PlanetRenderProxy(SolarSystemBodyIds.Earth.Value,new UniversePosition(Double3.Zero,root),radius,new Float3(.1f,.4f,.8f),"Earth",true,DoubleQuaternion.Identity);
    var transitionAltitudes=new[]{2_000_000d,1_750_000d,1_500_000d,1_250_000d,1_000_001d,1_000_000d,999_999d,100_000d,10_000d,SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres};
    foreach(var altitude in transitionAltitudes){var near=PlanetarySurfaceCameraPolicy.NearClipMetres(altitude);Check(near>=PlanetarySurfaceCameraPolicy.MinimumNearClipMetres&&near<=PlanetarySurfaceCameraPolicy.MaximumNearClipMetres&&near<altitude,"continuous near plane remains in front of the camera and behind the aimed surface");}
    Check(Math.Abs(PlanetarySurfaceCameraPolicy.NearClipMetres(1_000_001d)-PlanetarySurfaceCameraPolicy.NearClipMetres(999_999d))<1d,"regional/eyeball boundary has no near-plane discontinuity");
    var cameraBody=new Double3(0d,0d,radius+1_500_000d);var aimedDirection=new Double3(.18d,.08d,1d).Normalized();var aimedSurface=aimedDirection*radius;var forward=(aimedSurface-cameraBody).Normalized();Check(PlanetaryEyeballTopology.TryViewPupil(cameraBody,forward,radius,out var pupil)&&Double3.Dot(pupil,aimedDirection)>.999999999d,"eyeball footprint follows the view-ray surface hit instead of the sub-camera point");var cap=PlanetaryEyeballTopology.MaximumAngleRadians(cameraBody,radius,.62d);Check(cap>.62d&&cap<=1.45d,"eyeball cap conservatively includes the visible viewport and horizon margin");
    var blends=new[]{new PlanetaryRepresentationBlend(PlanetaryRenderRegime.DistantOnly,20,1,0),new PlanetaryRepresentationBlend(PlanetaryRenderRegime.Transition,15,.5f,.5f),new PlanetaryRepresentationBlend(PlanetaryRenderRegime.DetailedOnly,10,0,1)};foreach(var blend in blends)foreach(var eye in new[]{0f,.5f,1f})Check(PlanetarySurfaceCoverage.HasVisibleOwner(blend,eye),"distant/regional/eyeball coverage always has an owner");
    var config=PlanetaryLodConfiguration.ForViewport(19d,8,128d,1440d,Math.PI/3d,9_000d);var camera=new Double3(0,0,radius+200_000d);var localBody=body with{Position=new UniversePosition(Double3.Zero,root)};var first=PlanetaryRepresentationSelector.SelectPatches(localBody,camera,config,new Double3(0,0,-1),Math.PI/3d,16d/9d,200_000d);var stable=PlanetaryRepresentationSelector.SelectPatches(localBody,camera,config,new Double3(0,0,-1),Math.PI/3d,16d/9d,200_000d,first.Patches);Check(first.Patches.SequenceEqual(stable.Patches)&&stable.SplitPatchCount==0&&stable.MergedPatchCount==0,"identical camera state is deterministic and does not thrash spatial LOD");var hysteresisLeaves=stable.Patches;for(var crossing=0;crossing<32;crossing++){var jitterAltitude=(crossing&1)==0?199_000d:201_000d;var jitter=PlanetaryRepresentationSelector.SelectPatches(localBody,new Double3(0,0,radius+jitterAltitude),config,new Double3(0,0,-1),Math.PI/3d,16d/9d,jitterAltitude,hysteresisLeaves);Check(hysteresisLeaves.SequenceEqual(jitter.Patches)&&jitter.SplitPatchCount==0&&jitter.MergedPatchCount==0,"split/merge hysteresis absorbs repeated one-percent boundary motion");hysteresisLeaves=jitter.Patches;}var previousChildren=Enum.GetValues<CubeSphereFace>().SelectMany(face=>Enumerable.Range(0,4).Select(child=>new PlanetaryPatch(face,0,0,0).Child(child))).ToArray();var mergeConfig=new PlanetaryLodConfiguration(19d,8,1d);var farther=PlanetaryRepresentationSelector.SelectPatches(localBody,new Double3(0,0,radius*10d),mergeConfig,Double3.Zero,0,0,radius*9d,previousChildren);Check(farther.Patches.Length==6&&farther.MergedPatchCount==6&&farther.FrustumCulledPatchCount+farther.HorizonCulledPatchCount==farther.CulledPatchCount,"receding merges deterministic parent coverage and reports conservative culling separately");
    for(var level=0;level<=EarthSurfaceDatasetContract.MaximumLevel;level++){var expected=Math.Tau*radius/(EarthSurfaceDatasetContract.TileSize*(1<<(level+1)));Check(Math.Abs(EarthSurfaceDemandPolicy.EquatorialMetresPerTexel(radius,level)-expected)<1e-9,"Earth level metres-per-texel contract");}Check(EarthSurfaceDemandPolicy.RequestedLevel(2_000_000d)==1&&EarthSurfaceDemandPolicy.RequestedLevel(500_000d)==2&&EarthSurfaceDemandPolicy.RequestedLevel(50_000d)==3&&EarthSurfaceDemandPolicy.RequestedLevel(5_000d)==4,"SVT demand level follows visible surface importance");
}

static void PlanetaryPresentationPipelineTest()
{
    var root=new ReferenceFrameId(1);
    var evaluated=new[]
    {
        new EvaluatedPlanetaryBody(10,new UniversePosition(new Double3(0,0,0),root),695_700_000,new Float3(1,.8f,.3f),"Sun",true,DoubleQuaternion.Identity),
        new EvaluatedPlanetaryBody(399,new UniversePosition(new Double3(1.5e11,0,0),root),6_371_008.8,new Float3(.2f,.5f,1),"Earth",true,DoubleQuaternion.FromAxisAngle(Double3.UnitY,.25)),
        new EvaluatedPlanetaryBody(301,new UniversePosition(new Double3(1.503844e11,0,0),root),1_737_400,new Float3(.7f,.7f,.7f),"Moon",true,DoubleQuaternion.Identity),
    };
    Check(PlanetaryBodyPresentationProvider.TryCreateSnapshot(evaluated,out var snapshot)&&snapshot is not null,"planetary snapshot creation");
    var published=snapshot!;var original=published.Bodies[1];evaluated[1]=evaluated[1] with{RadiusMetres=1};
    Check(published.Bodies[1]==original,"presentation snapshot copies evaluated input");
    Span<ResolvedRenderObject> objects=stackalloc ResolvedRenderObject[3];
    Check(FarFieldPlanetaryRenderProxyProvider.TryBuild(published,1d,1d,objects,out var count)&&count==3,"planet renderer consumes snapshot");
    Check(objects[..count].ToArray().All(value=>value.Mesh==MeshHandle.Sphere)&&objects[1].RootOrientation==published.Bodies[1].BodyFixedToRoot,"one reusable sphere mesh carries immutable body orientation");
    var camera=new CameraState(new FramePosition(root,new Double3(0,0,100)),DoubleQuaternion.Identity,new CameraProjection(Math.PI/3,16d/9,.01,1000),CameraMode.Free);
    var before=published.Bodies.ToArray();Check(PlanetaryCameraFocus.TryFocus(camera,published,399,10_000_000),"Earth focus");Check(PlanetaryCameraFocus.TryFocus(camera,published,301,10_000_000),"Moon focus");Check(PlanetaryCameraFocus.TryFocus(camera,published,10,10_000_000),"Sun focus");
    Check(before.SequenceEqual(published.Bodies.ToArray()),"focus does not modify celestial presentation evaluation");
}

static void FocusTargetAuthorityTest()
{
    var root=new ReferenceFrameId(1);var bodyToRoot=DoubleQuaternion.FromAxisAngle(new Double3(.2,.8,.4).Normalized(),.71d);var body=new PlanetRenderProxy(399,new UniversePosition(new Double3(4e12,-3e12,7e12),root),6_371_008.8d,new Float3(.1f,.4f,.8f),"Earth",true,bodyToRoot);
    var center=FocusTarget.BodyCenter(body.BodyId);Check(center.IsValid&&center.Kind==FocusTargetKind.BodyCenter&&center.TryEvaluate(body,out var centerRoot)&&centerRoot==body.Position,"body-center focus evaluates only current authoritative translation");
    var direction=new Double3(.31d,.42d,.851d).Normalized();var anchor=SurfaceAnchorFocus.AtDirection(body.BodyId,direction,body.RadiusMetres,125d);var surface=FocusTarget.AtSurface(anchor);var expected=body.Position.Value+body.BodyFixedToRoot.Rotate(anchor.BodyLocalPosition);var surfaceEvaluated=surface.TryEvaluate(body,out var surfaceRoot);Check(anchor.IsValid&&surface.IsValid&&surface.Kind==FocusTargetKind.SurfaceAnchor&&surfaceEvaluated&&surfaceRoot.Frame==root&&surfaceRoot.Value==expected,"surface-anchor focus evaluates body-local position through current body orientation");
    var changedBody=body with{Position=new UniversePosition(body.Position.Value+new Double3(1e8,-2e8,3e8),root),BodyFixedToRoot=DoubleQuaternion.FromAxisAngle(Double3.UnitY,1.2d)};Check(surface.TryEvaluate(changedBody,out var changedRoot)&&changedRoot.Value==changedBody.Position.Value+changedBody.BodyFixedToRoot.Rotate(anchor.BodyLocalPosition)&&changedRoot!=surfaceRoot,"surface anchor follows current translation/orientation without owning either authority");
    var sceneObject=FocusTarget.SceneObject(77);var objectRoot=new UniversePosition(new Double3(-8e12,2e12,1e12),root);Check(sceneObject.TryEvaluateSceneObject(77,objectRoot,out var resolvedObject)&&resolvedObject==objectRoot&&!sceneObject.TryEvaluateSceneObject(78,objectRoot,out _),"future scene-object focus seam accepts only matching current authority");
    _=surface.TryEvaluate(body,out _);var allocatedBefore=GC.GetAllocatedBytesForCurrentThread();var started=Stopwatch.GetTimestamp();var checksum=0d;for(var index=0;index<1_000_000;index++){Check(surface.TryEvaluate(body,out var evaluated),"warm focus evaluation");checksum+=evaluated.Value.X;}var elapsed=Stopwatch.GetElapsedTime(started);var allocated=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;Check(allocated==0&&double.IsFinite(checksum),"focus target evaluation is bounded and allocation-free");Console.WriteLine($"focus target evaluation: {elapsed.TotalNanoseconds/1_000_000d:F2} ns/update; allocations={allocated} bytes");
}

static void SurfaceAnchorPhaseBTest()
{
    var root = new ReferenceFrameId(1);
    var bodyOrientation = DoubleQuaternion.FromAxisAngle(new Double3(.2d, .9d, -.3d).Normalized(), .83d);
    var earth = new PlanetRenderProxy(
        SolarSystemBodyIds.Earth.Value,
        new UniversePosition(new Double3(4e12d, -3e12d, 7e12d), root),
        6_371_008.8d,
        new Float3(.1f, .4f, .8f), "Earth", true, bodyOrientation);

    var latitudes = new[] { 0d, 45d, 80d, 89.999d, 89.999999999d, -89.999999999d };
    var maximumRoundTripError = 0d;
    foreach (var latitude in latitudes)
    {
        var radians = latitude * Math.PI / 180d;
        var direction = new Double3(Math.Cos(radians) * Math.Cos(.71d), Math.Sin(radians), Math.Cos(radians) * Math.Sin(.71d));
        var anchor = SurfaceAnchorFocus.AtDirection(earth.BodyId, direction, earth.RadiusMetres, 321.25d);
        var basis = anchor.LocalTangentBasis;
        Check(anchor.IsValid && basis.IsValid && Double3.Dot(Double3.Cross(basis.East, basis.North), basis.Up) > 1d - 1e-12d,
            $"right-handed pole-safe ENU at {latitude:R} degrees");
        foreach (var local in new[] { new Double3(1d, -2d, 3d), new Double3(.01d, -.02d, .03d), new Double3(.001d, -.001d, .002d) })
        {
            var bodyPoint = basis.ToBodyFixed(local, anchor.BodyLocalPosition);
            var recovered = basis.ToLocal(bodyPoint, anchor.BodyLocalPosition);
            var error = Math.Sqrt((recovered - local).LengthSquared);
            maximumRoundTripError = Math.Max(maximumRoundTripError, error);
            Check(error <= .000000002d && recovered.IsFinite, $"BodyFixed/ENU round trip at {latitude:R} degrees and {local.LengthSquared:R} scale");
        }
    }
    var northPole = SurfaceAnchorFocus.AtDirection(earth.BodyId, Double3.UnitY, earth.RadiusMetres, 0d);
    var southPole = SurfaceAnchorFocus.AtDirection(earth.BodyId, -Double3.UnitY, earth.RadiusMetres, 0d);
    Check(northPole.IsValid && southPole.IsValid && northPole.LocalTangentBasis.East.IsFinite && southPole.LocalTangentBasis.East.IsFinite,
        "exact poles use deterministic finite fallback axes");

    var aimedDirection = new Double3(.61d, .42d, -.671d).Normalized();
    var elevation = EarthSurfaceDataset.SampleHeight(aimedDirection);
    var aimedRoot = earth.Position.Value + earth.BodyFixedToRoot.Rotate(aimedDirection * (earth.RadiusMetres + elevation));
    var cameraRoot = aimedRoot + earth.BodyFixedToRoot.Rotate(aimedDirection) * 3_000_000d;
    var cameraForward = (aimedRoot - cameraRoot).Normalized();
    Check(!SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root),
        Double3.Cross(cameraForward, Double3.UnitY).Normalized(), PlanetaryTerrainDefinition.EarthAuthoritativeV3, out _),
        "a view ray that misses Earth does not fabricate a SurfaceAnchor");
    Check(SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root), cameraForward,
        PlanetaryTerrainDefinition.EarthAuthoritativeV3, out var acquisition), "Earth camera ray acquires authoritative surface");
    var acquisitionPositionError = Math.Sqrt((acquisition.RootPositionAtAcquisition.Value - aimedRoot).LengthSquared);
    Check(acquisition.Anchor.IsValid && acquisition.SurfaceRefinementCount == SurfaceAnchorAcquisition.TerrainRefinementCount &&
        Math.Abs(acquisition.Anchor.AuthoritativeElevationMetres - EarthSurfaceDataset.SampleHeight(acquisition.Anchor.BodyFixedDirection)) < 1e-9d &&
        acquisitionPositionError < .01d, "Earth acquisition refines against the loaded elevation oracle");
    var retainedOnMiss = FocusTarget.AtSurface(acquisition.Anchor);
    if (SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root),
        Double3.Cross(cameraForward, Double3.UnitY).Normalized(), PlanetaryTerrainDefinition.EarthAuthoritativeV3, out var replacement))
        retainedOnMiss = FocusTarget.AtSurface(replacement.Anchor);
    Check(retainedOnMiss == FocusTarget.AtSurface(acquisition.Anchor), "a missed reacquisition retains the previous valid focus state");

    var anchorRoot = acquisition.RootPositionAtAcquisition.Value;
    var maximumCameraRelativePackingError = 0d;
    foreach (var localOffset in new[] { new Double3(1d, -.5d, .25d), new Double3(.01d, -.02d, .03d), new Double3(.001d, -.001d, .002d) })
    {
        var bodyPoint = acquisition.Anchor.LocalTangentBasis.ToBodyFixed(localOffset, acquisition.Anchor.BodyLocalPosition);
        var pointRoot = earth.Position.Value + earth.BodyFixedToRoot.Rotate(bodyPoint);
        var expectedRelative = pointRoot - anchorRoot;
        var encodedRelative = CameraRelativeRenderPosition.Create(pointRoot, anchorRoot).Encode().Reconstruct();
        var packingError = Math.Max(Math.Abs(encodedRelative.X - expectedRelative.X),
            Math.Max(Math.Abs(encodedRelative.Y - expectedRelative.Y), Math.Abs(encodedRelative.Z - expectedRelative.Z)));
        maximumCameraRelativePackingError = Math.Max(maximumCameraRelativePackingError, packingError);
        Check(packingError <= 2e-9d, "surface-anchor camera-relative transport adds no meaningful meter/cm/mm loss");
    }

    Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var firstScene, out var sceneError) && firstScene is not null,
        $"SurfaceAnchor handoff scene: {sceneError}");
    var scene = firstScene!;
    var camera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, scene.Projection, CameraMode.Free);
    Check(scene.Focus(camera, NativePresentationFocus.Earth), "Earth focus for SurfaceAnchor handoff");
    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    var immutableBodies = scene.Presentation.Bodies.ToArray();
    var maximumAcquisitionCameraError = 0d;
    var maximumAcquisitionCameraPositionError = 0d;
    var maximumAcquisitionTargetDistanceError = 0d;
    var acquired = false;
    for (var step = 0; step < 128 && !acquired; step++)
    {
        var beforePosition = camera.Position.Value;
        var beforeCenter = scene.FocusedBody.Position.Value;
        var beforeRadial = (beforePosition - beforeCenter).Normalized();
        var beforeDistance = Math.Sqrt((beforePosition - beforeCenter).LengthSquared);
        var beforeBodyDirection = scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(beforeRadial);
        var surfaceRadius = scene.FocusedBody.RadiusMetres + EarthPlanetaryScene.Terrain.SampleHeight(beforeBodyDirection, 24);
        var expectedDistance = SolarCameraZoomPolicy.Apply(beforeDistance, surfaceRadius,
            surfaceRadius + SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
            SolAnalyticalDefinition.AstronomicalUnitMetres * SolarSystemScene.MaximumOverviewDistanceAu, 1);
        var expectedPosition = beforeCenter + beforeRadial * expectedDistance;
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        acquired = scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor;
        if (acquired)
        {
            var cameraLineError = Math.Sqrt(Double3.Cross(camera.Position.Value - beforePosition, beforeRadial).LengthSquared);
            maximumAcquisitionCameraError = Math.Max(maximumAcquisitionCameraError, cameraLineError);
            maximumAcquisitionCameraPositionError = Math.Sqrt((camera.Position.Value - expectedPosition).LengthSquared);
            maximumAcquisitionTargetDistanceError = Math.Abs(scene.OrbitDistance -
                Math.Sqrt((camera.Position.Value - scene.CurrentFocusRoot).LengthSquared));
            Check(scene.SurfaceAnchorBlend == 0d && scene.CurrentFocusRoot == scene.FocusedBody.Position.Value,
                "SurfaceAnchor identity begins at zero positional weight");
        }
    }
    Check(acquired && maximumAcquisitionCameraError < .01d && maximumAcquisitionCameraPositionError < .01d &&
        maximumAcquisitionTargetDistanceError < 2e-6d, "BodyCenter acquisition has no camera or target-distance snap");
    var acquiredAnchor = scene.CurrentFocusTarget.SurfaceAnchor;
    var previousBlend = scene.SurfaceAnchorBlend;
    var maximumOrientationStep = 0d;
    var maximumInertialOffsetZoomRatioError = 0d;
    for (var step = 0; step < 64 && scene.SurfaceAnchorBlend < 1d; step++)
    {
        var beforeOrientation = camera.Orientation;Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out _),"active anchor evaluates before zoom");var beforeOffsetDistance=Math.Sqrt(scene.CurrentInertialCameraOffset.LengthSquared);
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out _),"active anchor evaluates after zoom");var afterOffsetDistance=Math.Sqrt(scene.CurrentInertialCameraOffset.LengthSquared);
        maximumInertialOffsetZoomRatioError=Math.Max(maximumInertialOffsetZoomRatioError,Math.Abs(beforeOffsetDistance/afterOffsetDistance-SolarCameraZoomPolicy.DistanceRatioPerDetent));
        maximumOrientationStep = Math.Max(maximumOrientationStep, QuaternionAngle(beforeOrientation, camera.Orientation));
        Check(scene.SurfaceAnchorBlend >= previousBlend && scene.CurrentFocusTarget.SurfaceAnchor == acquiredAnchor,
            "handoff blend is monotonic and does not hop anchors");
        previousBlend = scene.SurfaceAnchorBlend;
    }
    Check(scene.SurfaceAnchorBlend == 1d && scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.SurfaceLocal,
        "descent reaches full SurfaceAnchor focus");
    Check(maximumInertialOffsetZoomRatioError<1e-9d,"post-acquisition wheel cadence logarithmically scales the inertial camera offset");
    for (var step = 0; step < 128 && scene.SurfaceAltitudeMetres > 10d; step++)
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
    Check(scene.SurfaceAltitudeMetres >= SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres-1e-5d && scene.SurfaceAltitudeMetres < 20d,
        "target-relative wheel reaches near-ground scale without terrain penetration");

    var beforeDragTarget = scene.CurrentFocusRoot;
    var beforeDragDistance = scene.OrbitDistance;
    var beforeDragBodies = scene.Presentation.Bodies.ToArray();
    scene.ApplyPresentationInput(camera, new NativeInputState { LookActive = 1, MouseDeltaX = 21, MouseDeltaY = -9 }, out _, out _);
    Check(scene.CurrentFocusRoot == beforeDragTarget && scene.OrbitDistance+1e-6d>=beforeDragDistance && scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres &&
        !immutableBodies.Except(beforeDragBodies).Any() && beforeDragBodies.SequenceEqual(scene.Presentation.Bodies),
        "click-drag orbits the camera around the anchor without changing body truth and may only increase distance for terrain clearance");

    var retainedAnchor = scene.CurrentFocusTarget.SurfaceAnchor;
    for (var step = 0; step < 128 && scene.SurfaceAltitudeMetres < 1_500_000d; step++)
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _);
    Check(scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && scene.CurrentFocusTarget.SurfaceAnchor == retainedAnchor,
        "zoom-out hysteresis retains the same anchor above the acquisition threshold");
    for (var step = 0; step < 32 && scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor; step++)
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _);
    Check(scene.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter && scene.SurfaceAnchorBlend == 0d,
        "zoom-out beyond release threshold returns deterministically to BodyCenter");

    var firstReplay = HandoffReplay();
    var secondReplay = HandoffReplay();
    Check(firstReplay == secondReplay && firstReplay.AcquisitionTargetError < 1e-6d && firstReplay.ReleaseTargetError < 1e-6d &&
        firstReplay.CameraPositionError < .01d && firstReplay.TargetDistanceError < 2e-6d &&
        firstReplay.AcquisitionOrientationError < 1e-12d && firstReplay.ReleaseOrientationError < 1e-12d,
        "repeated BodyCenter/SurfaceAnchor crossings are deterministic and continuous");

    var warpRates = new[]
    {
        SimulationRate.One,
        new SimulationRate(30, 1),
        new SimulationRate(600, 1),
        new SimulationRate(14_400, 1),
        new SimulationRate(7_776_000, 1),
    };
    foreach (var rate in warpRates)
    {
        Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var warpSceneCandidate, out var warpError) && warpSceneCandidate is not null,
            $"SurfaceAnchor warp scene {rate.Numerator}x: {warpError}");
        var warpScene = warpSceneCandidate!;
        var warpCamera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, warpScene.Projection, CameraMode.Free);
        Check(warpScene.Focus(warpCamera, NativePresentationFocus.Earth), $"SurfaceAnchor Earth focus {rate.Numerator}x");
        for (var step = 0; step < 128 && (warpScene.CurrentFocusTarget.Kind != FocusTargetKind.SurfaceAnchor || warpScene.SurfaceAnchorBlend == 0d); step++)
            warpScene.ApplyPresentationInput(warpCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        Check(warpScene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && warpScene.SurfaceAnchorBlend > 0d,
            $"SurfaceAnchor active before {rate.Numerator}x advance");
        var requestedIndex = SimulationSpeedPresets.IndexOf(rate);
        while (warpScene.SpeedPresetIndex < requestedIndex) warpScene.ApplyPresentationInput(warpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
        while (warpScene.SpeedPresetIndex > requestedIndex) warpScene.ApplyPresentationInput(warpCamera, new NativeInputState { RateDecrease = 1 }, out _, out _);
        var stableAnchor = warpScene.CurrentFocusTarget.SurfaceAnchor;
        var beforeTarget = warpScene.CurrentFocusRoot;
        var beforeOffset = warpCamera.Position.Value - beforeTarget;
        var beforeView = warpCamera.Orientation;
        Check(warpScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), warpCamera, out warpError),
            $"SurfaceAnchor {rate.Numerator}x advance: {warpError}");
        var afterTarget = warpScene.CurrentFocusRoot;
        var afterOffset = warpCamera.Position.Value - afterTarget;
        var afterBody = warpScene.FocusedBody;
        var expectedAnchorRoot = afterBody.Position.Value + afterBody.BodyFixedToRoot.Rotate(stableAnchor.BodyLocalPosition);
        var expectedFocusRoot = SurfaceFocusHandoffPolicy.BlendedRoot(afterBody.Position.Value, expectedAnchorRoot, warpScene.SurfaceAnchorBlend);
        var offsetError = Math.Sqrt((afterOffset - beforeOffset).LengthSquared);
        Check(warpScene.CurrentFocusTarget.SurfaceAnchor == stableAnchor && afterTarget == expectedFocusRoot && afterTarget != beforeTarget &&
            warpCamera.Orientation == beforeView && offsetError <= Math.Max(.01d, Math.Sqrt(beforeOffset.LengthSquared) * 1e-12d) &&
            warpCamera.Position.Value.IsFinite && double.IsFinite(warpScene.SurfaceAltitudeMetres),
            $"SurfaceAnchor remains geographic and camera orientation remains inertial at {rate.Numerator}x");
        Console.WriteLine($"SurfaceAnchor warp {rate.Numerator}x: targetMotion={Math.Sqrt((afterTarget-beforeTarget).LengthSquared):R} m; offsetError={offsetError:E3} m; orientationFixed={warpCamera.Orientation==beforeView}");
    }

    var mock = new BodyFixedSceneObject(9001, earth.BodyId, acquisition.Anchor.BodyLocalPosition + acquisition.Anchor.LocalTangentBasis.Up * 30d);
    var mockFocus = FocusTarget.SceneObject(mock.ObjectId);
    Check(mock.TryEvaluate(earth, out var mockRoot), "mock surface rocket evaluates from its parent body");
    Check(mockFocus.TryEvaluateSceneObject(mock.ObjectId, mockRoot, out var focusedMock),
        "mock surface rocket resolves through the SceneObject authority seam");
    foreach (var distance in new[] { 5d, 100_000d, 2_000_000d, 20_000_000d, 5d })
    {
        var mockCamera = focusedMock.Value + Double3.UnitZ * distance;
        Check(Math.Abs(Math.Sqrt((mockCamera - focusedMock.Value).LengthSquared) - distance) < 1e-9d,
            "mock rocket remains focusable from ground through far planetary scale");
    }

    var perfAnchor = acquisition.Anchor;
    var perfTarget = FocusTarget.AtSurface(perfAnchor);
    _ = perfTarget.TryEvaluate(earth, out _);
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var checksum = 0d;
    var started = Stopwatch.GetTimestamp();
    for (var index = 0; index < 100_000; index++)
    { Check(perfTarget.TryEvaluate(earth, out var evaluated), "anchor performance evaluation"); checksum += evaluated.Value.X; }
    var anchorElapsed = Stopwatch.GetElapsedTime(started);
    started = Stopwatch.GetTimestamp();
    for (var index = 0; index < 100_000; index++) checksum += perfAnchor.LocalTangentBasis.ToLocal(perfAnchor.BodyLocalPosition + perfAnchor.LocalTangentBasis.East, perfAnchor.BodyLocalPosition).X;
    var enuElapsed = Stopwatch.GetElapsedTime(started);
    started = Stopwatch.GetTimestamp();
    for (var index = 0; index < 100_000; index++) checksum += SurfaceFocusHandoffPolicy.SurfaceBlend(1_500_000d);
    var handoffElapsed = Stopwatch.GetElapsedTime(started);
    started = Stopwatch.GetTimestamp();
    var zoomDistance = 100_000d;
    for (var index = 0; index < 100_000; index++) zoomDistance = SolarCameraZoomPolicy.ApplyTargetRelative(zoomDistance, 2d, 1e12d, (index & 1) == 0 ? 1 : -1);
    var zoomElapsed = Stopwatch.GetElapsedTime(started);checksum += zoomDistance;
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    Check(allocated == 0 && double.IsFinite(checksum), "anchor/ENU/handoff update is allocation-free");
    Console.WriteLine($"SurfaceAnchor: acquisitionError={acquisitionPositionError:E3} m; ENU maxError={maximumRoundTripError:E3} m; cameraRelativePackError={maximumCameraRelativePackingError:E3} m; acquisitionCameraPositionError={maximumAcquisitionCameraPositionError:E3} m; acquisitionTargetDistanceError={maximumAcquisitionTargetDistanceError:E3} m; zoomRatioError={maximumInertialOffsetZoomRatioError:E3}; maxOrientationStep={maximumOrientationStep:E3} rad; handoffTarget={firstReplay.AcquisitionTargetError:E3}/{firstReplay.ReleaseTargetError:E3} m; handoffCamera={firstReplay.CameraPositionError:E3} m; handoffDistance={firstReplay.TargetDistanceError:E3} m; handoffOrientation={firstReplay.AcquisitionOrientationError:E3}/{firstReplay.ReleaseOrientationError:E3} rad; anchor={anchorElapsed.TotalNanoseconds / 100_000d:F2} ns; ENU={enuElapsed.TotalNanoseconds / 100_000d:F2} ns; handoff={handoffElapsed.TotalNanoseconds / 100_000d:F2} ns; zoom={zoomElapsed.TotalNanoseconds / 100_000d:F2} ns; allocations={allocated}");

    (int AcquisitionSteps,int ReleaseSteps,double AcquisitionTargetError,double ReleaseTargetError,double CameraPositionError,double TargetDistanceError,double AcquisitionOrientationError,double ReleaseOrientationError,Double3 CameraRoot,DoubleQuaternion CameraOrientation) HandoffReplay()
    {
        Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var replayCandidate, out var replayError) && replayCandidate is not null,
            $"handoff replay scene: {replayError}");
        var replay = replayCandidate!;
        var replayCamera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, replay.Projection, CameraMode.Free);
        Check(replay.Focus(replayCamera, NativePresentationFocus.Earth), "handoff replay Earth focus");
        var acquisitionSteps = 0;
        var acquisitionTargetError = double.NaN;
        var cameraPositionError = double.NaN;
        var targetDistanceError = double.NaN;
        var acquisitionOrientationError = double.NaN;
        while (replay.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter && acquisitionSteps++ < 128)
        {
            var beforeTarget = replay.CurrentFocusRoot;var beforeView = replayCamera.Orientation;
            var beforeRelative = replayCamera.Position.Value - beforeTarget;
            var beforeDistance = Math.Sqrt(beforeRelative.LengthSquared);
            var beforeRadial = beforeRelative / beforeDistance;
            var beforeDirection = replay.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(beforeRadial);
            var surfaceRadius = replay.FocusedBody.RadiusMetres + EarthPlanetaryScene.Terrain.SampleHeight(beforeDirection, 24);
            var expectedDistance = SolarCameraZoomPolicy.Apply(beforeDistance, surfaceRadius,
                surfaceRadius + SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                SolAnalyticalDefinition.AstronomicalUnitMetres * SolarSystemScene.MaximumOverviewDistanceAu, 1);
            var expectedPosition = beforeTarget + beforeRadial * expectedDistance;
            replay.ApplyPresentationInput(replayCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
            if (replay.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor)
            {
                acquisitionTargetError = Math.Sqrt((replay.CurrentFocusRoot - beforeTarget).LengthSquared);
                cameraPositionError = Math.Sqrt((replayCamera.Position.Value - expectedPosition).LengthSquared);
                targetDistanceError = Math.Abs(replay.OrbitDistance - Math.Sqrt((replayCamera.Position.Value - replay.CurrentFocusRoot).LengthSquared));
                acquisitionOrientationError = QuaternionAngle(beforeView, replayCamera.Orientation);
            }
        }
        while (replay.SurfaceAnchorBlend < 1d) replay.ApplyPresentationInput(replayCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        var releaseSteps = 0;
        var releaseTargetError = double.NaN;
        var releaseOrientationError = double.NaN;
        while (replay.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && releaseSteps++ < 128)
        {
            var beforeView = replayCamera.Orientation;
            replay.ApplyPresentationInput(replayCamera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _);
            if (replay.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter)
            {
                releaseTargetError = Math.Sqrt((replay.CurrentFocusRoot - replay.FocusedBody.Position.Value).LengthSquared);
                cameraPositionError = Math.Max(cameraPositionError, Math.Sqrt((replayCamera.Position.Value -
                    (replay.CurrentFocusRoot + replay.CurrentInertialCameraOffset)).LengthSquared));
                targetDistanceError = Math.Max(targetDistanceError, Math.Abs(replay.OrbitDistance - Math.Sqrt((replayCamera.Position.Value - replay.CurrentFocusRoot).LengthSquared)));
                releaseOrientationError = QuaternionAngle(beforeView, replayCamera.Orientation);
            }
        }
        return (acquisitionSteps, releaseSteps, acquisitionTargetError, releaseTargetError, cameraPositionError, targetDistanceError, acquisitionOrientationError, releaseOrientationError,
            replayCamera.Position.Value, replayCamera.Orientation);
    }
}

static void CameraFocusPositionContinuityTest()
{
    var root = new ReferenceFrameId(1);
    Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var candidate, out var error) && candidate is not null,
        $"camera continuity scene: {error}");
    var scene = candidate!;
    var camera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, scene.Projection, CameraMode.Free);
    Check(scene.Focus(camera, NativePresentationFocus.Earth), "camera continuity Earth focus");

    var referenceOrientation = camera.Orientation;
    var previousFocus = scene.CurrentFocusRoot;
    var previousCamera = camera.Position.Value;
    var previousOffset = scene.CurrentInertialCameraOffset;
    var previousKind = scene.CurrentFocusTarget.Kind;
    var maximumFocusError = 0d;
    var maximumCameraError = 0d;
    var maximumOffsetError = 0d;
    var maximumOrientationError = 0d;
    var sawStart = false;
    var sawMidpoint = false;
    var sawCompletion = false;
    var sawRelease = false;

    for (var crossing = 0; crossing < 2; crossing++)
    {
        var inwardWeight = 0d;
        for (var step = 0; step < 160 && (scene.CurrentFocusTarget.Kind != FocusTargetKind.SurfaceAnchor || scene.SurfaceAnchorBlend < 1d); step++)
        {
            scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
            Measure($"inward {crossing}/{step}");
            Check(scene.SurfaceAnchorBlend + 1e-12d >= inwardWeight, "inward handoff weight is monotonic");
            inwardWeight = scene.SurfaceAnchorBlend;
        }
        Check(scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && scene.SurfaceAnchorBlend == 1d,
            "inward traversal completes SurfaceAnchor handoff");

        var outwardWeight = scene.SurfaceAnchorBlend;
        for (var step = 0; step < 160 && scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor; step++)
        {
            scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _);
            Measure($"outward {crossing}/{step}");
            Check(scene.SurfaceAnchorBlend <= outwardWeight + 1e-12d, "outward handoff weight is monotonic");
            outwardWeight = scene.SurfaceAnchorBlend;
        }
        Check(scene.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter && scene.SurfaceAnchorBlend == 0d,
            "outward traversal releases to BodyCenter at zero positional weight");
    }

    for (var step = 0; step < 160 && (scene.CurrentFocusTarget.Kind != FocusTargetKind.SurfaceAnchor || !(scene.SurfaceAnchorBlend > 0d && scene.SurfaceAnchorBlend < 1d)); step++)
    {
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        Measure("warp acquisition");
    }
    Check(scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && scene.SurfaceAnchorBlend > 0d && scene.SurfaceAnchorBlend < 1d,
        "maximum-warp proof begins during the positional handoff");
    var stableAnchor = scene.CurrentFocusTarget.SurfaceAnchor;
    Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody, out var anchorBeforeWarp), "anchor evaluates before maximum warp");
    var offsetBeforeWarp = scene.CurrentInertialCameraOffset;
    var orientationBeforeWarp = camera.Orientation;
    while (scene.SpeedPresetIndex < SimulationSpeedPresets.Count - 1)
        scene.ApplyPresentationInput(camera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), camera, out error), $"maximum-warp continuity advance: {error}");
    Measure("maximum warp");
    Check(scene.CurrentFocusTarget.SurfaceAnchor == stableAnchor, "maximum warp retains body-fixed anchor identity");
    Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody, out var anchorAfterWarp), "anchor evaluates after maximum warp");
    var expectedAnchorAfterWarp = scene.FocusedBody.Position.Value + scene.FocusedBody.BodyFixedToRoot.Rotate(stableAnchor.BodyLocalPosition);
    Check(anchorAfterWarp.Value == expectedAnchorAfterWarp && anchorAfterWarp != anchorBeforeWarp,
        "rotating Earth evaluates the fixed geographic anchor into current root space");
    maximumOffsetError = Math.Max(maximumOffsetError, Math.Sqrt((scene.CurrentInertialCameraOffset - offsetBeforeWarp).LengthSquared));
    maximumOrientationError = Math.Max(maximumOrientationError, QuaternionAngle(orientationBeforeWarp, camera.Orientation));

    Check(sawStart && sawMidpoint && sawCompletion && sawRelease, "handoff start, midpoint, completion, and release were sampled");
    Check(maximumFocusError < .01d && maximumCameraError < .01d && maximumOffsetError < .01d && maximumOrientationError < 1e-12d,
        "camera focus-position continuity errors remain below deterministic root-space tolerances");
    Console.WriteLine($"Camera focus continuity: focus={maximumFocusError:E3} m; camera={maximumCameraError:E3} m; offset={maximumOffsetError:E3} m; orientation={maximumOrientationError:E3} rad");

    void Measure(string sample)
    {
        var focus = scene.CurrentFocusRoot;
        var inertialOffset = scene.CurrentInertialCameraOffset;
        var cameraRoot = camera.Position.Value;
        var expectedFocus = scene.FocusedBody.Position.Value;
        if (scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor)
        {
            Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody, out var anchorRoot), $"{sample}: SurfaceAnchor evaluates");
            expectedFocus = SurfaceFocusHandoffPolicy.BlendedRoot(scene.FocusedBody.Position.Value, anchorRoot.Value, scene.SurfaceAnchorBlend);
            sawStart |= scene.SurfaceAnchorBlend == 0d;
            sawMidpoint |= scene.SurfaceAnchorBlend > 0d && scene.SurfaceAnchorBlend < 1d;
            sawCompletion |= scene.SurfaceAnchorBlend == 1d;
        }
        sawRelease |= previousKind == FocusTargetKind.SurfaceAnchor && scene.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter;

        var actualOffset = cameraRoot - focus;
        maximumFocusError = Math.Max(maximumFocusError, Math.Sqrt((focus - expectedFocus).LengthSquared));
        maximumCameraError = Math.Max(maximumCameraError, Math.Sqrt((cameraRoot - (focus + inertialOffset)).LengthSquared));
        maximumCameraError = Math.Max(maximumCameraError, Math.Sqrt(((cameraRoot - previousCamera) -
            ((focus - previousFocus) + (inertialOffset - previousOffset))).LengthSquared));
        maximumOffsetError = Math.Max(maximumOffsetError, Math.Sqrt((actualOffset - inertialOffset).LengthSquared));
        maximumOrientationError = Math.Max(maximumOrientationError, QuaternionAngle(referenceOrientation, camera.Orientation));
        Check(scene.SurfaceAnchorBlend is >= 0d and <= 1d && double.IsFinite(scene.SurfaceAnchorBlend) && focus.IsFinite &&
            inertialOffset.IsFinite && cameraRoot.IsFinite && camera.Orientation.IsFinite, $"{sample}: all continuity values are finite and bounded");
        Check(previousOffset.LengthSquared == 0d || Double3.Dot(previousOffset, inertialOffset) > 0d,
            $"{sample}: inertial camera offset never inverts");
        previousFocus = focus;
        previousCamera = cameraRoot;
        previousOffset = inertialOffset;
        previousKind = scene.CurrentFocusTarget.Kind;
    }
}

static double QuaternionAngle(in DoubleQuaternion first, in DoubleQuaternion second)
{
    var dot = Math.Abs(first.X * second.X + first.Y * second.Y + first.Z * second.Z + first.W * second.W);
    return 2d * Math.Acos(Math.Clamp(dot, -1d, 1d));
}

static void PlanetMaterialPresentationTest()
{
    Check(SolarPlanetMaterials.Catalog.Materials.Length==9,"nine non-stellar Solar materials");
    var materials=SolarPlanetMaterials.Catalog.Materials.ToArray();
    Check(materials.Select(material=>material.BodyId).SequenceEqual(new ulong[]{3,4,6,7,8,9,10,11,12})&&materials.All(material=>material.IsValid),"material table uses stable body IDs and valid generic contracts");
    Check(materials.Select(material=>material.AlbedoSource).Distinct().Count()==9,"each validated body has an explicit albedo identity");
    Check(SolarPlanetMaterials.Catalog.TryGet(10,out var saturn)&&saturn.Ring.HasValue,"generic Saturn ring lookup");var ring=saturn.Ring!.Value;Check(ring.IsValid&&ring.InnerRadiusMetres>58_000_000d&&ring.OuterRadiusMetres>ring.InnerRadiusMetres,"generic Saturn ring configuration");
    var native=new NativePlanetaryPresentation{Radius=58_232_000f};PlanetMaterialNativeEncoder.Apply(ref native,saturn);
    Check(native.BodyIdLow==10&&native.BodyIdHigh==0&&native.MaterialKind==(uint)PlanetMaterialKind.GasGiant&&native.AlbedoSource==(uint)PlanetAlbedoSource.SaturnProcedural,"material identity encodes with fixed-width values");
    Check(native.RingAssociation==1&&native.RingInnerRadiusRatio>1&&native.RingOuterRadiusRatio>native.RingInnerRadiusRatio&&native.RingOpacity==ring.Opacity,"ring radii and presentation profile encode independently of body radius authority");
    Check(Math.Abs(native.RingOrientationX*native.RingOrientationX+native.RingOrientationY*native.RingOrientationY+native.RingOrientationZ*native.RingOrientationZ+native.RingOrientationW*native.RingOrientationW-1f)<1e-6f,"ring orientation transport normalized");
    Check(native.LocalDetailScaleMeters==PlanetMaterialNativeEncoder.DefaultLocalDetailScaleMeters&&native.LocalDetailMicroScaleMeters==PlanetMaterialNativeEncoder.DefaultLocalDetailMicroScaleMeters&&native.LocalDetailFadeStartMetres==PlanetMaterialNativeEncoder.DefaultLocalDetailFadeStartMetres&&native.LocalDetailFadeEndMetres==PlanetMaterialNativeEncoder.DefaultLocalDetailFadeEndMetres,"material defaults include local detail");
    Check(Marshal.SizeOf<NativePlanetaryPresentation>()==176&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.BodyIdLow)).ToInt32()==48&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.Roughness)).ToInt32()==64&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.ProjectionKind)).ToInt32()==80&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingInnerRadiusRatio)).ToInt32()==96&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingOrientationX)).ToInt32()==112&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingColorR)).ToInt32()==128&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.BodyOrientationX)).ToInt32()==144&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.LocalDetailScaleMeters)).ToInt32()==160,"material, ring, body-orientation, and local-detail ABI layout");
}

static void PlanetaryPresentationSpirvStrideTest()
{
    var shaderSourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "native", "NovaCore.Native", "shaders"));
    var shaderBinaryDirectory = Path.GetFullPath(Path.Combine(shaderSourceDirectory, "..", "..", "..", "build", "native-ninja", "shaders"));
    string[] expectedConsumers =
    [
        "distant_planet.vert",
        "planetary.vert",
        "planetary_environment.frag",
        "planetary_eyeball.vert",
        "planetary_ring.vert",
        "solar_label.vert",
        "solar_marker.vert",
        "solar_orbit.vert",
        "stellar_glow.vert",
        "stellar_sun.vert"
    ];

    var actualConsumers = Directory.GetFiles(shaderSourceDirectory)
        .Where(path =>
        {
            var source = File.ReadAllText(path);
            return source.Contains("binding=6", StringComparison.Ordinal) && source.Contains("Presentation values[]", StringComparison.Ordinal);
        })
        .Select(Path.GetFileName)
        .Order(StringComparer.Ordinal)
        .ToArray();
    Array.Sort(expectedConsumers, StringComparer.Ordinal);
    Check(actualConsumers.SequenceEqual(expectedConsumers), "binding 6 Presentation runtime-array consumer set");

    foreach (var shader in actualConsumers)
    {
        var sourcePath = Path.Combine(shaderSourceDirectory, shader!);
        Check(File.ReadAllText(sourcePath).Contains("vec4 localDetail;", StringComparison.Ordinal), $"{shader} includes localDetail in Presentation ABI");
        var binaryPath = Path.Combine(shaderBinaryDirectory, shader + ".spv");
        Check(File.Exists(binaryPath), $"compiled SPIR-V exists for {shader}");
        var presentation = ReadSpirvStructLayout(binaryPath, "Presentation");
        var expectedOffsets = new Dictionary<string, uint>
        {
            ["centerRadius"] = 0, ["colorDistant"] = 16, ["blendMetricState"] = 32, ["identity"] = 48,
            ["surface"] = 64, ["hooks"] = 80, ["ringGeometry"] = 96, ["ringOrientation"] = 112,
            ["ringColor"] = 128, ["bodyOrientation"] = 144, ["localDetail"] = 160
        };
        Check(presentation.ArrayStride == 176u, $"{shader} Presentation ArrayStride is 176, actual {presentation.ArrayStride}");
        Check(presentation.MemberOffsets.Count == expectedOffsets.Count && expectedOffsets.All(expected =>
            presentation.MemberOffsets.TryGetValue(expected.Key, out var actual) && actual == expected.Value),
            $"{shader} complete Presentation member offsets");
        Console.WriteLine($"Presentation ABI {shader}.spv: stride={presentation.ArrayStride}; members={presentation.MemberOffsets.Count}");
    }

    var planetaryFragment = Path.Combine(shaderBinaryDirectory, "planetary.frag.spv");
    Check(File.Exists(planetaryFragment), "compiled SPIR-V exists for planetary.frag");
    var eyeball = ReadSpirvStructLayout(planetaryFragment, "EyeballDebugInput");
    var expectedEyeballOffsets = new Dictionary<string, uint>
    {
        ["cameraHighRadiusHigh"] = 0, ["cameraLowRadiusLow"] = 16, ["surface"] = 32, ["identity"] = 48,
        ["viewForwardMargin"] = 64, ["mapping"] = 80, ["topology"] = 96, ["reserved"] = 112
    };
    Check(eyeball.Bindings.SequenceEqual(new uint[] { 12 }), "planetary.frag EyeballDebugInput is reflected at binding 12");
    Check(eyeball.MemberOffsets.Count == expectedEyeballOffsets.Count && expectedEyeballOffsets.All(expected =>
        eyeball.MemberOffsets.TryGetValue(expected.Key, out var actual) && actual == expected.Value),
        "planetary.frag complete EyeballDebugInput member offsets");
    Check(eyeball.MemberOffsets["topology"] == 96u && eyeball.MemberOffsets["reserved"] == 112u &&
        eyeball.MemberOffsets["reserved"] + 16u == 128u, "binding 12 topology/reserved offsets and 128-byte record size");
    Console.WriteLine($"Eyeball binding 12 ABI planetary.frag.spv: topology={eyeball.MemberOffsets["topology"]}; reserved={eyeball.MemberOffsets["reserved"]}; size={eyeball.MemberOffsets["reserved"] + 16u}");
}

static (uint? ArrayStride, Dictionary<string, uint> MemberOffsets, uint[] Bindings) ReadSpirvStructLayout(string path, string structName)
{
    var bytes = File.ReadAllBytes(path);
    Check(bytes.Length >= 20 && bytes.Length % sizeof(uint) == 0, $"valid SPIR-V byte length: {path}");
    var words = new uint[bytes.Length / sizeof(uint)];
    for (var index = 0; index < words.Length; index++) words[index] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint), sizeof(uint)));
    Check(words[0] == 0x07230203u, $"SPIR-V magic: {path}");

    var names = new Dictionary<uint, string>();
    var memberNames = new Dictionary<(uint Type, uint Member), string>();
    var memberOffsets = new Dictionary<(uint Type, uint Member), uint>();
    var runtimeArrayElementTypes = new Dictionary<uint, uint>();
    var arrayStrides = new Dictionary<uint, uint>();
    var pointerPointeeTypes = new Dictionary<uint, uint>();
    var variablePointerTypes = new Dictionary<uint, uint>();
    var bindings = new Dictionary<uint, uint>();
    var descriptorSets = new Dictionary<uint, uint>();
    for (var index = 5; index < words.Length;)
    {
        var instruction = words[index];
        var wordCount = (int)(instruction >> 16);
        var opcode = (ushort)instruction;
        Check(wordCount > 0 && index + wordCount <= words.Length, $"valid SPIR-V instruction: {path}");
        if (opcode == 5 && wordCount >= 3) names[words[index + 1]] = ReadSpirvString(words, index + 2, wordCount - 2);
        else if (opcode == 6 && wordCount >= 4) memberNames[(words[index + 1], words[index + 2])] = ReadSpirvString(words, index + 3, wordCount - 3);
        else if (opcode == 29 && wordCount == 3) runtimeArrayElementTypes[words[index + 1]] = words[index + 2];
        else if (opcode == 32 && wordCount == 4) pointerPointeeTypes[words[index + 1]] = words[index + 3];
        else if (opcode == 59 && wordCount >= 4) variablePointerTypes[words[index + 2]] = words[index + 1];
        else if (opcode == 71 && wordCount >= 4)
        {
            if (words[index + 2] == 6u) arrayStrides[words[index + 1]] = words[index + 3];
            else if (words[index + 2] == 33u) bindings[words[index + 1]] = words[index + 3];
            else if (words[index + 2] == 34u) descriptorSets[words[index + 1]] = words[index + 3];
        }
        else if (opcode == 72 && wordCount >= 5 && words[index + 3] == 35u)
            memberOffsets[(words[index + 1], words[index + 2])] = words[index + 4];
        index += wordCount;
    }

    var namedStructTypes = names.Where(entry => entry.Value == structName).Select(entry => entry.Key).ToArray();
    var runtimeArrayStructTypes = runtimeArrayElementTypes
        .Where(entry => namedStructTypes.Contains(entry.Value) && arrayStrides.ContainsKey(entry.Key))
        .Select(entry => entry.Value)
        .Distinct()
        .ToArray();
    var boundStructTypes = variablePointerTypes
        .Where(entry => pointerPointeeTypes.TryGetValue(entry.Value, out var pointee) && namedStructTypes.Contains(pointee) && bindings.ContainsKey(entry.Key))
        .Select(entry => pointerPointeeTypes[entry.Value])
        .Distinct()
        .ToArray();
    var structType = runtimeArrayStructTypes.Length != 0 ? runtimeArrayStructTypes.Single() : boundStructTypes.Single();
    var reflectedOffsets = memberNames
        .Where(entry => entry.Key.Type == structType)
        .ToDictionary(entry => entry.Value, entry => memberOffsets[entry.Key], StringComparer.Ordinal);
    uint? arrayStride = runtimeArrayElementTypes
        .Where(entry => entry.Value == structType && arrayStrides.ContainsKey(entry.Key))
        .Select(entry => (uint?)arrayStrides[entry.Key])
        .SingleOrDefault();
    var reflectedBindings = variablePointerTypes
        .Where(entry => pointerPointeeTypes.TryGetValue(entry.Value, out var pointee) && pointee == structType &&
            bindings.ContainsKey(entry.Key) && descriptorSets.GetValueOrDefault(entry.Key) == 0u)
        .Select(entry => bindings[entry.Key])
        .Order()
        .ToArray();
    return (arrayStride, reflectedOffsets, reflectedBindings);
}

static string ReadSpirvString(uint[] words, int start, int wordCount)
{
    var bytes = new byte[wordCount * sizeof(uint)];
    for (var index = 0; index < wordCount; index++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint), sizeof(uint)), words[start + index]);
    var length = Array.IndexOf(bytes, (byte)0);
    if (length < 0) length = bytes.Length;
    return System.Text.Encoding.UTF8.GetString(bytes, 0, length);
}

static void PlanetMicroNormalFoundationTest()
{
    var macro = Vector3.Normalize(new Vector3(0.33f, 0.82f, 0.47f));
    var microXyFlat = new Vector2(0.5f, 0.5f);
    var flat = ComposeMicroNormal(macro, microXyFlat, 0.73f, 0.55f);
    Check(Vector3.Distance(flat, macro) < 1e-5f, "flat micro normal leaves macro normal unchanged");

    var east = PlanetMicroBasisEast(macro);
    var north = PlanetMicroBasisNorth(macro);
    var plusX = ComposeMicroNormal(macro, new Vector2(0.96f, 0.5f), 1f, 0.95f);
    var plusY = ComposeMicroNormal(macro, new Vector2(0.5f, 0.96f), 1f, 0.95f);
    Check(Vector3.Dot(Vector3.Normalize(plusX - macro), east) > 0.12f, "known +X BC5 perturbation maps toward NovaCore east");
    Check(Vector3.Dot(Vector3.Normalize(plusY - macro), north) > 0.12f, "known +Y BC5 perturbation maps toward NovaCore north");

    var reconstructed = PlanetDecodeBc5Normal(new Vector2(0.42f, 0.84f));
    Check(float.IsFinite(reconstructed.Z) && reconstructed.Z >= 0f, "reconstructed BC5 Z is finite and nonnegative");
    var reconstructedProjected = new Vector2(reconstructed.X, reconstructed.Y);
    Check(MathF.Abs(reconstructed.Z * reconstructed.Z - MathF.Max(0f, 1f - Vector2.Dot(reconstructedProjected, reconstructedProjected))) < 1e-5f, "reconstructed BC5 Z matches x/y length");

    Check(MathF.Abs(Vector3.Dot(plusX, plusX) - 1f) < 3e-5f, "composed +X result is normalized");
    Check(MathF.Abs(Vector3.Dot(plusY, plusY) - 1f) < 3e-5f, "composed +Y result is normalized");

    var muted = ComposeMicroNormal(macro, new Vector2(0.3f, 0.7f), 0f, 0.95f);
    Check(Vector3.Distance(muted, macro) < 1e-5f, "zero local contribution preserves macro normal");

    for (var y = 0f; y <= 1f; y += 0.125f)
    {
        for (var x = 0f; x <= 1f; x += 0.125f)
        {
            var encoded = new Vector2(x, y);
            var decoded = PlanetDecodeBc5Normal(encoded);
            var composed = ComposeMicroNormal(macro, encoded, 0.4f, 0.7f);
            Check(!float.IsNaN(decoded.X) && !float.IsNaN(decoded.Y) && !float.IsNaN(decoded.Z) && float.IsFinite(decoded.Z) && decoded.Z >= 0f, "bounded BC5 XY inputs decode to finite nonnegative Z");
            Check(MathF.Abs(Vector3.Dot(composed, composed) - 1f) < 5e-5f, "bounded BC5 composition remains normalized");
        }
    }
}

static void PlanetarySurfaceScatterPlacementTest()
{
    var root = new ReferenceFrameId(1);
    var body = new PlanetRenderProxy(
        SolarSystemBodyIds.Earth.Value,
        new UniversePosition(new Double3(7.2e11d, -4.8e11d, 3.1e11d), root),
        6_371_008.8d,
        new Float3(.1f, .4f, .8f),
        "Earth",
        true,
        DoubleQuaternion.Identity);

    const float minimumScale = 0.35f;
    const float maximumScale = 1.4f;
    var terrain = PlanetaryTerrainDefinition.EarthProceduralV1;
    var anchorDirection = new Double3(.61d, .42d, -.671d).Normalized();
    var anchor = SurfaceAnchorFocus.AtDirection(
        body.BodyId,
        anchorDirection,
        body.RadiusMetres,
        terrain.SampleHeight(anchorDirection, 24));

    var config = new PlanetarySurfaceScatterConfiguration(
        ScatterRadiusMetres: 2_500d,
        CellSizeMetres: 64d,
        MaximumCandidateCells: 196,
        MaximumInstances: 64,
        MinimumScaleMetres: minimumScale,
        MaximumScaleMetres: maximumScale,
        Seed: 0xC0FFEE01u);

    var first = PlanetarySurfaceScatterPlacement.Generate(body, anchor, terrain, config);
    var repeat = PlanetarySurfaceScatterPlacement.Generate(body, anchor, terrain, config);
    Check(first.Length > 0 && first.Length <= config.MaximumInstances && first.Length <= config.MaximumCandidateCells, "deterministic bounded scatter produces candidates");
    Check(first.SequenceEqual(repeat), "same body/cell/seed produces identical surface scatter instances");

    var anchorRoot = body.Position.Value + body.BodyFixedToRoot.Rotate(anchor.BodyLocalPosition);
    var cameraA = new UniversePosition(anchorRoot + body.BodyFixedToRoot.Rotate(new Double3(180d, 1_200d, 2_300d)), root);
    var cameraB = new UniversePosition(anchorRoot + body.BodyFixedToRoot.Rotate(new Double3(-700d, 1_000d, 1_950d)), root);
    var cameraAInstances = PlanetarySurfaceScatterPlacement.Generate(body, anchor, terrain, config, cameraA, null, false);
    var cameraBInstances = PlanetarySurfacePlacementWithCameraOptional(body, anchor, terrain, config, cameraB);
    Check(cameraAInstances.Select(value => value.IdentityHash).SequenceEqual(cameraBInstances.Select(value => value.IdentityHash)),
        "moving camera does not alter body-fixed scatter identity");

    var secondCellDirection = (anchor.BodyLocalPosition + anchor.LocalTangentBasis.East * (config.CellSizeMetres * 3d)).Normalized();
    var secondCell = SurfaceAnchorFocus.AtDirection(
        body.BodyId,
        secondCellDirection,
        body.RadiusMetres,
        terrain.SampleHeight(secondCellDirection, 24));
    var differentCell = PlanetarySurfaceScatterPlacement.Generate(body, secondCell, terrain, config);
    Check(!first.Select(value => value.BodyLocalPosition).SequenceEqual(differentCell.Select(value => value.BodyLocalPosition)),
        "different cells produce deterministic different scatter patterns");

    Check(first.All(value => value.IsFinite), "all scatter instances are finite");
    Check(first.All(value => value.ScaleMetres >= minimumScale - 1e-9f && value.ScaleMetres <= maximumScale + 1e-9f),
        "all scales are configured-range bounded");

    foreach (var value in first)
    {
        var bodyDirection = value.BodyLocalPosition.Normalized();
        Check(bodyDirection.IsFinite, "surface scatter instance body-fixed direction is finite");
        var expectedRadius = body.RadiusMetres + terrain.SampleHeight(bodyDirection, 24);
        var bodyRadius = Math.Sqrt(value.BodyLocalPosition.LengthSquared);
        Check(bodyRadius + 1e-6 >= expectedRadius, "surface scatter instance body-local radius is at or above authoritative terrain");
        Check(bodyRadius - expectedRadius <= 1e-6d, "surface scatter instance height resolves at authoritative terrain radius");
    }

    var rotatedBody = body with
    {
        Position = new UniversePosition(new Double3(1.25e11d, 1.75e11d, -8.4e11d), root),
        BodyFixedToRoot = DoubleQuaternion.FromAxisAngle(new Double3(.2d, .8d, .4d).Normalized(), 0.73d),
    };
    var rotated = PlanetarySurfaceScatterPlacement.Generate(rotatedBody, anchor, terrain, config);
    Check(first.Length == rotated.Length && first.Select(value => value.IdentityHash).SequenceEqual(rotated.Select(value => value.IdentityHash)),
        "high-warp/body rotation keeps stable scatter identity");
    for (var index = 0; index < first.Length; index++)
    {
        var beforeRoot = body.Position.Value + body.BodyFixedToRoot.Rotate(first[index].BodyLocalPosition);
        var afterRoot = rotatedBody.Position.Value + rotatedBody.BodyFixedToRoot.Rotate(rotated[index].BodyLocalPosition);
        Check(!beforeRoot.Equals(afterRoot), "body rotation/body translation changes world instance placement while body-fixed identity remains stable");
    }

    var cullCameraForward = (anchorRoot - cameraA.Value).Normalized();
    var culled = PlanetarySurfaceScatterPlacement.Generate(body, anchor, terrain, config, cameraA, cullCameraForward, true);
    Check(culled.Length <= first.Length, "camera relevance culling never increases scatter candidate count");

    static PlanetarySurfaceScatterInstance[] PlanetarySurfacePlacementWithCameraOptional(
        in PlanetRenderProxy bodyProxy,
        in SurfaceAnchorFocus surfaceAnchor,
        in PlanetaryTerrainDefinition queryTerrain,
        in PlanetarySurfaceScatterConfiguration scatterConfiguration,
        in UniversePosition optionalCamera)
        => PlanetarySurfaceScatterPlacement.Generate(bodyProxy, surfaceAnchor, queryTerrain, scatterConfiguration, optionalCamera);
}

static void PlanetaryEnvironmentPresentationTest()
{
    var environment=PlanetaryEnvironmentPresentation.EarthDataV2;Check(environment.IsValid&&environment.Layers==(PlanetaryEnvironmentLayers.Atmosphere|PlanetaryEnvironmentLayers.Clouds|PlanetaryEnvironmentLayers.Ocean),"Earth environment uses generic bounded layers");
    Check(SolarPlanetMaterials.Environments.TryGet(6,out var catalogEnvironment)&&catalogEnvironment==environment&&!SolarPlanetMaterials.Environments.TryGet(8,out _),"environment catalog is body-associated rather than renderer hard-coded");
    var root=new ReferenceFrameId(1);var earth=new PlanetRenderProxy(6,new UniversePosition(new Double3(12,34,56),root),6_371_008.8,new Float3(.1f,.4f,.8f),"Earth",true,DoubleQuaternion.Identity);var cameraRoot=new UniversePosition(new Double3(2,4,6),root);var native=environment.Encode(earth,cameraRoot);
    Check(Marshal.SizeOf<NativePlanetaryEnvironment>()==128&&Marshal.OffsetOf<NativePlanetaryEnvironment>(nameof(NativePlanetaryEnvironment.BodyIdLow)).ToInt32()==16&&Marshal.OffsetOf<NativePlanetaryEnvironment>(nameof(NativePlanetaryEnvironment.AtmosphereHeightMetres)).ToInt32()==32&&Marshal.OffsetOf<NativePlanetaryEnvironment>(nameof(NativePlanetaryEnvironment.CloudBaseHeightMetres)).ToInt32()==64&&Marshal.OffsetOf<NativePlanetaryEnvironment>(nameof(NativePlanetaryEnvironment.OceanSeaLevelMetres)).ToInt32()==96,"planetary environment ABI layout");
    Check(native.CenterX==10&&native.CenterY==30&&native.CenterZ==50&&native.Radius==(float)earth.RadiusMetres&&native.BodyIdLow==6&&native.EnabledLayers==7&&native.SourceVersion==2,"environment camera-relative transport preserves body authority");
    Check(PlanetarySurfaceCameraPolicy.Mode(1_000_000)==PlanetaryCameraPresentationMode.Orbital&&PlanetarySurfaceCameraPolicy.Mode(500_000)==PlanetaryCameraPresentationMode.Transition&&PlanetarySurfaceCameraPolicy.Mode(100_000)==PlanetaryCameraPresentationMode.SurfaceLocal,"camera mode altitude boundaries");
    Check(PlanetarySurfaceCameraPolicy.SurfaceBlend(1_000_000)==0&&PlanetarySurfaceCameraPolicy.SurfaceBlend(100_000)==1&&PlanetarySurfaceCameraPolicy.ZoomFactor(1_000)<PlanetarySurfaceCameraPolicy.ZoomFactor(1_000_000),"camera transition and fine zoom are deterministic");
    Check(PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(2)==12.04d&&PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(100_000)==2_000d,"SurfaceLocal translation speed is bounded and altitude-aware");
    var frame=PlanetarySurfaceFrame.AtDirection(Double3.UnitZ);var a=frame.LookOrientation(.25,-.2);var b=frame.LookOrientation(.25,-.2);Check(a==b&&Math.Abs(a.LengthSquared-1)<1e-12,"local tangent camera orientation deterministic");
}

static void EarthAuthoritativeDatasetTest()
{
    var runtime=Path.Combine(Directory.GetCurrentDirectory(),"assets","earth","runtime");
    Check(EarthSurfaceDataset.TryLoad(runtime,out var error),$"authoritative Earth elevation load: {error}");
    Check(EarthSurfaceDataset.IsLoaded&&EarthSurfaceDatasetContract.TileSize==256&&EarthSurfaceDatasetContract.TileGutter==2&&EarthSurfaceDatasetContract.PhysicalTileExtent==260&&EarthSurfaceDatasetContract.MaximumLevel==4&&EarthSurfaceDatasetContract.TileCount==682,"fixed production Earth virtual-texture contract");
    var regionalPath=Path.Combine(runtime,"regions",EarthRegionalDatasetContract.FileName);var regionalValid=EarthSurfaceDataset.TryValidateRegionalPack(regionalPath,out var regionalError);Check(EarthSurfaceDataset.IsRegionalLoaded&&regionalValid,$"bounded regional pack load/validation: {regionalError}");
    Check(EarthRegionalDatasetContract.MinimumLevel==5&&EarthRegionalDatasetContract.MaximumLevel==12&&EarthRegionalDatasetContract.PageCount==48&&EarthRegionalDatasetContract.PackBytes==11_359_360,"bounded sparse regional contract");
    using(var regionalStream=File.OpenRead(regionalPath))Check(Convert.ToHexStringLower(SHA256.HashData(regionalStream))==EarthRegionalDatasetContract.PackSha256,"regional pack regression hash");
    var mountStHelens=Direction(46.1912,-122.1944);var spiritLake=Direction(46.263,-122.137);var regionalSummit=EarthSurfaceDataset.SampleElevation(mountStHelens);var regionalLake=EarthSurfaceDataset.SampleElevation(spiritLake);Check(regionalSummit is >2_200 and <2_700&&regionalLake is >900 and <1_300,"regional 3DEP elevation probes retain plausible NAVD88 relief");Check(EarthSurfaceDataset.SampleElevation(mountStHelens)==regionalSummit,"regional body-fixed sample deterministic");
    var regionalRecords=new HashSet<(int Level,int X,int Y)>();using(var regionalReader=new BinaryReader(File.OpenRead(regionalPath))){regionalReader.BaseStream.Position=256;for(var record=0;record<EarthRegionalDatasetContract.PageCount;record++){var level=regionalReader.ReadInt32();var x=regionalReader.ReadInt32();var y=regionalReader.ReadInt32();regionalReader.ReadInt32();regionalRecords.Add((level,x,y));}}for(var level=EarthRegionalDatasetContract.MinimumLevel;level<=EarthRegionalDatasetContract.MaximumLevel;level++){var page=EarthVirtualTexturePageContract.BodyFixedPageCoordinates(mountStHelens,level,EarthRegionalDatasetContract.MaximumLevel);Check(regionalRecords.Contains((level,page.X,page.Y)),$"regional L{level} page and global SVT use one body-fixed geographic address");}
    var corruptPath=Path.Combine(Path.GetTempPath(),"novacore-regional-corrupt.ncvreg");var corrupt=File.ReadAllBytes(regionalPath);corrupt[^1]^=0x5a;File.WriteAllBytes(corruptPath,corrupt);Check(!EarthSurfaceDataset.TryValidateRegionalPack(corruptPath,out _),"corrupt regional pack rejected without affecting global authority");File.Delete(corruptPath);Check(!EarthSurfaceDataset.TryValidateRegionalPack(Path.Combine(Path.GetTempPath(),"novacore-regional-missing.ncvreg"),out _),"missing regional pack cleanly rejected");
    Check(EarthSurfaceDatasetContract.IdentitySha256=="664ff32c3a57043960f246d5d97397214cedc4b976e48e867e9803c414d796b5"&&EarthSurfaceDatasetContract.PayloadSha256=="d09a9ddf944242a7d322ae3ce58c1b0b31014feb8d6a330fb9d592e438e9d306","Earth dataset v3 identity and payload regression hashes");
    var manifestPath=Path.Combine(runtime,"earth_surface_v3.manifest.json");var packPath=Path.Combine(runtime,"earth_surface_v3.ncvtex");
    var manifest=File.ReadAllText(manifestPath);
    Check(manifest.Contains(EarthSurfaceDatasetContract.IdentitySha256,StringComparison.Ordinal)&&manifest.Contains(EarthSurfaceDatasetContract.PayloadSha256,StringComparison.Ordinal),"manifest identifies the exact deterministic payload");
    using(var manifestStream=File.OpenRead(manifestPath))Check(Convert.ToHexStringLower(SHA256.HashData(manifestStream))==EarthSurfaceDatasetContract.ManifestSha256,"manifest file regression hash");
    using(var packHashStream=File.OpenRead(packPath))Check(Convert.ToHexStringLower(SHA256.HashData(packHashStream))==EarthSurfaceDatasetContract.RuntimePackSha256,"runtime pack regression hash");
    using(var pack=File.OpenRead(packPath))
    {
        Span<byte> header=stackalloc byte[256];pack.ReadExactly(header);
        Check(header[..8].SequenceEqual("NCVTEAR2"u8)&&BinaryPrimitives.ReadUInt32LittleEndian(header[8..])==3&&BinaryPrimitives.ReadUInt32LittleEndian(header[12..])==256,"production pack magic/version/header");
        Check(BinaryPrimitives.ReadUInt32LittleEndian(header[16..])==256&&BinaryPrimitives.ReadUInt32LittleEndian(header[20..])==2&&BinaryPrimitives.ReadUInt32LittleEndian(header[24..])==4&&BinaryPrimitives.ReadUInt32LittleEndian(header[28..])==682,"production pack tile metadata");
        Check(BinaryPrimitives.ReadUInt32LittleEndian(header[32..])==260&&BinaryPrimitives.ReadUInt32LittleEndian(header[36..])==4&&EarthSurfaceDatasetContract.PhysicalTileExtent%4==0,"production pack channel count and BC block alignment");
        (uint Semantic,uint Format,uint Color,uint Level,uint Count,uint Bytes,long Offset)[] expected=[(1u,4u,1u,4u,682u,67_600u,256L),(2u,2u,0u,4u,682u,135_200u,46_103_456L),(3u,3u,0u,4u,682u,33_800u,138_309_856L),(4u,3u,0u,2u,42u,33_800u,161_361_456L)];
        for(var channel=0;channel<expected.Length;channel++){var descriptor=header[(112+channel*32)..];var value=expected[channel];Check(BinaryPrimitives.ReadUInt32LittleEndian(descriptor)==value.Semantic&&BinaryPrimitives.ReadUInt32LittleEndian(descriptor[4..])==value.Format&&BinaryPrimitives.ReadUInt32LittleEndian(descriptor[8..])==value.Color&&BinaryPrimitives.ReadUInt32LittleEndian(descriptor[12..])==value.Level&&BinaryPrimitives.ReadUInt32LittleEndian(descriptor[16..])==value.Count&&BinaryPrimitives.ReadUInt32LittleEndian(descriptor[20..])==value.Bytes&&BinaryPrimitives.ReadUInt64LittleEndian(descriptor[24..])==(ulong)value.Offset,$"channel {channel} explicit semantic/format/color/LOD/bytes/offset");}
        Check(pack.Length==EarthSurfaceDatasetContract.RuntimePackBytes,"production pack byte size");
        var firstBc7=new byte[16];pack.Position=256;pack.ReadExactly(firstBc7);Check((firstBc7[0]&0x7f)==0x40,"BC7 albedo begins with deterministic mode-6 block");
        pack.Position=46_103_456;using var elevationHash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var elevationBuffer=new byte[1_048_576];long elevationRemaining=92_206_400;while(elevationRemaining>0){var count=pack.Read(elevationBuffer,0,(int)Math.Min(elevationBuffer.Length,elevationRemaining));Check(count>0,"complete R16 elevation section");elevationHash.AppendData(elevationBuffer,0,count);elevationRemaining-=count;}Check(Convert.ToHexStringLower(elevationHash.GetHashAndReset())==EarthSurfaceDatasetContract.ElevationPackSectionSha256,"R16 elevation tile section regression hash");
    }
    using(var document=JsonDocument.Parse(manifest)){var quality=document.RootElement.GetProperty("quality");Check(quality.GetProperty("bc7").GetProperty("global").GetProperty("psnrDb").GetDouble()>45&&quality.GetProperty("bc1").GetProperty("coastline").GetProperty("psnrDb").GetDouble()<30&&quality.GetProperty("bc7").GetProperty("coastline").GetProperty("psnrDb").GetDouble()>32,"measured BC7 quality selection over BC1");Check(quality.GetProperty("coastlineMaskBc4").GetProperty("psnrDb").GetDouble()>50&&quality.GetProperty("cloudBc4").GetProperty("psnrDb").GetDouble()>39,"BC4 mask/cloud quality thresholds");}
    var policies=PlanetaryTextureFormatPolicy.Earth.ToArray();Check(policies.Length==4&&policies[0].Format==PlanetaryGpuTextureFormat.Bc7Srgb&&policies[0].ColorSpace==PlanetaryTextureColorSpace.Srgb&&policies.Skip(1).All(policy=>policy.ColorSpace==PlanetaryTextureColorSpace.Linear)&&policies.Single(policy=>policy.Semantic==PlanetaryTextureSemantic.Elevation).LosslessAuthorityRequired,"explicit SRGB/linear format policy prevents gamma misuse");Check(PlanetaryTextureFormatPolicy.FutureNormal.Format==PlanetaryGpuTextureFormat.Bc5Unorm&&PlanetaryTextureFormatPolicy.FutureNormal.ColorSpace==PlanetaryTextureColorSpace.Linear,"future two-component normal contract is BC5 linear");
    Check(EarthSurfaceDemandPolicy.RequestedLevel(PlanetaryTextureSemantic.Albedo,5_000)==4&&EarthSurfaceDemandPolicy.RequestedLevel(PlanetaryTextureSemantic.Elevation,5_000)==4&&EarthSurfaceDemandPolicy.RequestedLevel(PlanetaryTextureSemantic.LandMask,5_000)==4&&EarthSurfaceDemandPolicy.RequestedLevel(PlanetaryTextureSemantic.Cloud,5_000)==2,"independent per-channel maximum useful LOD policy");
    Check(Enumerable.Range(0,5).Select(EarthVirtualTexturePageContract.LevelOffset).SequenceEqual(new[]{0,2,10,42,170}),"deterministic level offsets");
    Check(EarthVirtualTexturePageContract.ParentIndex(4,31,15)==EarthVirtualTexturePageContract.TileIndex(3,15,7),"deterministic parent mapping");
    var resident=new bool[EarthSurfaceDatasetContract.TileCount];resident[0]=resident[1]=true;
    var requested=EarthVirtualTexturePageContract.TileIndex(4,3,6);resident[requested]=true;
    Check(EarthVirtualTexturePageContract.ResolveResidentPage(.1,.4,4,resident,out var exactLevel)==requested&&exactLevel==4,"resident requested page selection");
    resident[requested]=false;Check(EarthVirtualTexturePageContract.ResolveResidentPage(.1,.4,4,resident,out var fallbackLevel)==0&&fallbackLevel==0,"resident ancestor fallback");
    Check(EarthVirtualTexturePageContract.ResolveResidentPage(-.1,2,4,resident,out _)==1,"longitude wrap and polar clamp page selection");
    Check(EarthVirtualTexturePageContract.PromotionBlend(100,100)==0&&EarthVirtualTexturePageContract.PromotionBlend(100,115)==.5&&EarthVirtualTexturePageContract.PromotionBlend(100,130)==1,"fixed 30-frame smooth promotion");
    var everest=Direction(27.9881,86.925);var pacific=Direction(0,-140);var sahara=Direction(23,13);
    var everestElevation=EarthSurfaceDataset.SampleElevation(everest);var pacificElevation=EarthSurfaceDataset.SampleElevation(pacific);var saharaElevation=EarthSurfaceDataset.SampleElevation(sahara);
    Check(everestElevation is >5_000 and <8_000&&pacificElevation is <-3_000 and >-7_000&&saharaElevation is >200 and <2_000,"known ETOPO land/ocean elevation probes");
    Check(EarthSurfaceDataset.SampleHeight(pacific)==0&&EarthSurfaceDataset.SampleHeight(everest)==everestElevation,"sea-level floor is separate from signed elevation authority");
    Check(EarthSurfaceDataset.SampleElevation(everest)==everestElevation&&EarthSurfaceDataset.SampleElevation(pacific)==pacificElevation,"repeated body-fixed samples deterministic");
    Check(PlanetaryTerrainDefinition.EarthAuthoritativeV3.SourceId==2&&PlanetaryTerrainDefinition.EarthAuthoritativeV3.Version==3&&PlanetaryTerrainDefinition.EarthAuthoritativeV3.SampleHeight(everest,24)==everestElevation,"terrain query uses the registered Earth source independent of topology LOD");
    Check(Enum.GetValues<EarthVirtualTextureDebugMode>().Length==12&&EarthSurfaceDatasetContract.PhysicalPoolBudgetBytes==34_611_200&&EarthSurfaceDatasetContract.StagingBudgetBytes==1_081_600&&EarthSurfaceDatasetContract.UploadBudgetChannels==4,"bounded compressed pool/staging budgets and complete debug-view contract");
    Console.WriteLine($"Earth dataset v3: identity={EarthSurfaceDatasetContract.IdentitySha256}; payload={EarthSurfaceDatasetContract.PayloadSha256}; pack={EarthSurfaceDatasetContract.RuntimePackSha256}; manifest={EarthSurfaceDatasetContract.ManifestSha256}; regional={EarthRegionalDatasetContract.PackSha256}; MountStHelens={regionalSummit:F1} m; SpiritLake={regionalLake:F1} m; Everest={everestElevation:F1} m; Pacific={pacificElevation:F1} m");
    static Double3 Direction(double latitudeDegrees,double longitudeDegrees){var latitude=latitudeDegrees*Math.PI/180d;var longitude=longitudeDegrees*Math.PI/180d;var cosLatitude=Math.Cos(latitude);return new Double3(cosLatitude*Math.Cos(longitude),Math.Sin(latitude),cosLatitude*Math.Sin(longitude));}
}

static void CubeSpherePlanetarySurfaceTest()
{
    var faces=Enum.GetValues<CubeSphereFace>();Check(faces.Length==6&&faces.Distinct().Count()==6,"six deterministic cube faces");var root=new PlanetaryPatch(CubeSphereFace.PositiveX,0,0,0);var children=Enumerable.Range(0,4).Select(root.Child).ToArray();Check(children.Distinct().Count()==4&&children.All(child=>child.Parent==root),"deterministic children");Check(children.Select(child=>child.Bounds).OrderBy(bounds=>bounds.MinY).ThenBy(bounds=>bounds.MinX).Count()==4,"child bounds partition root");foreach(var face in faces)foreach(var u in new[]{0d,.5d,1d})foreach(var v in new[]{0d,.5d,1d})Check(Math.Abs(Math.Sqrt(CubeSphereProjection.Project(face,u,v,10).LengthSquared)-10)<1e-10,"cube sphere radius");
    var body=new PlanetRenderProxy(399,new UniversePosition(Double3.Zero,new ReferenceFrameId(1)),10,new Float3(0,0,1),"",true,DoubleQuaternion.Identity);var config=new PlanetaryLodConfiguration(8,3);var far=PlanetaryRepresentationSelector.SelectPatches(body,new Double3(1000,0,0),config);var near=PlanetaryRepresentationSelector.SelectPatches(body,new Double3(20,0,0),config);var closer=PlanetaryRepresentationSelector.SelectPatches(body,new Double3(11,0,0),config);Check(far.Representation==PlanetaryRepresentation.FarFieldBody&&near.MaximumLevel>=0&&closer.MaximumLevel>=near.MaximumLevel,"deterministic far near lod");var camera=new UniversePosition(new Double3(1e12,0,0),new ReferenceFrameId(1));var relative=CubeSphereProjection.CameraRelativeCenter(body,camera);Check(relative.X==-1e12&&body.Position.Value==Double3.Zero,"camera relative does not mutate body");
    var edges=new[]{PlanetaryPatchEdge.NegativeU,PlanetaryPatchEdge.PositiveU,PlanetaryPatchEdge.NegativeV,PlanetaryPatchEdge.PositiveV};
    var expected=new[]{
        T(CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeU,CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveU,false),T(CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveU,CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeU,false),T(CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeV,CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveU,true),T(CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveV,CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveU,false),
        T(CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeU,CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveU,false),T(CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveU,CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeU,false),T(CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeV,CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeU,false),T(CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveV,CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeU,true),
        T(CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeU,CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveV,true),T(CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveU,CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveV,false),T(CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeV,CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveV,false),T(CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveV,CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveV,true),
        T(CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeU,CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeV,false),T(CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveU,CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeV,true),T(CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeV,CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeV,true),T(CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveV,CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeV,false),
        T(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeU,CubeSphereFace.NegativeX,PlanetaryPatchEdge.PositiveU,false),T(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveU,CubeSphereFace.PositiveX,PlanetaryPatchEdge.NegativeU,false),T(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.NegativeV,CubeSphereFace.NegativeY,PlanetaryPatchEdge.PositiveV,false),T(CubeSphereFace.PositiveZ,PlanetaryPatchEdge.PositiveV,CubeSphereFace.PositiveY,PlanetaryPatchEdge.NegativeV,false),
        T(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeU,CubeSphereFace.PositiveX,PlanetaryPatchEdge.PositiveU,false),T(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveU,CubeSphereFace.NegativeX,PlanetaryPatchEdge.NegativeU,false),T(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.NegativeV,CubeSphereFace.NegativeY,PlanetaryPatchEdge.NegativeV,true),T(CubeSphereFace.NegativeZ,PlanetaryPatchEdge.PositiveV,CubeSphereFace.PositiveY,PlanetaryPatchEdge.PositiveV,true)};
    Check(expected.All(item=>CubeSphereAdjacency.GetTransition(item.Face,item.Edge)==item.Transition),"complete cross-face transition table");
    foreach(var face in faces)foreach(var edge in edges)for(var along=0;along<8;along++){var patch=edge is PlanetaryPatchEdge.NegativeU or PlanetaryPatchEdge.PositiveU?new PlanetaryPatch(face,3,edge==PlanetaryPatchEdge.NegativeU?0:7,along):new PlanetaryPatch(face,3,along,edge==PlanetaryPatchEdge.NegativeV?0:7);var transition=CubeSphereAdjacency.GetTransition(face,edge);var neighbor=CubeSphereAdjacency.NeighborAtSameLevel(patch,edge);Check(CubeSphereAdjacency.NeighborAtSameLevel(neighbor,transition.NeighborEdge)==patch,"cross-face adjacency reciprocal");foreach(var t in new[]{0d,.25d,.5d,.75d,1d}){var source=EdgePoint(face,edge,t);var target=EdgePoint(transition.NeighborFace,transition.NeighborEdge,transition.Reversed?1d-t:t);Check(Math.Sqrt((source-target).LengthSquared)<1e-12,"cross-face transition geometry");}}
    var unbalanced=new HashSet<PlanetaryPatch>(faces.Select(face=>new PlanetaryPatch(face,0,0,0)));var adaptiveRoot=new PlanetaryPatch(CubeSphereFace.PositiveZ,0,0,0);unbalanced.Remove(adaptiveRoot);foreach(var child in Enumerable.Range(0,4).Select(adaptiveRoot.Child))unbalanced.Add(child);var levelOne=adaptiveRoot.Child(0);unbalanced.Remove(levelOne);foreach(var child in Enumerable.Range(0,4).Select(levelOne.Child))unbalanced.Add(child);var levelTwo=levelOne.Child(0);unbalanced.Remove(levelTwo);foreach(var child in Enumerable.Range(0,4).Select(levelTwo.Child))unbalanced.Add(child);var balancedA=PlanetaryRepresentationSelector.BalancePatches(unbalanced,3,out var balanceCountA);var balancedB=PlanetaryRepresentationSelector.BalancePatches(unbalanced.Reverse(),3,out var balanceCountB);Check(balanceCountA>0&&balanceCountA==balanceCountB&&balancedA.SequenceEqual(balancedB),"balancing deterministic and exercised");var balancedSet=balancedA.ToHashSet();Check(balancedA.All(patch=>edges.All(edge=>PlanetaryRepresentationSelector.FindCoveringNeighbor(patch,edge,balancedSet) is not { } neighbor||patch.Level-neighbor.Level<=1)),"balanced hierarchy neighbor constraint");
    static (CubeSphereFace Face,PlanetaryPatchEdge Edge,CubeSphereEdgeTransition Transition) T(CubeSphereFace face,PlanetaryPatchEdge edge,CubeSphereFace neighbor,PlanetaryPatchEdge neighborEdge,bool reversed)=>(face,edge,new(neighbor,neighborEdge,reversed));
    static Double3 EdgePoint(CubeSphereFace face,PlanetaryPatchEdge edge,double along){var u=edge switch{PlanetaryPatchEdge.NegativeU=>0d,PlanetaryPatchEdge.PositiveU=>1d,_=>along};var v=edge switch{PlanetaryPatchEdge.NegativeV=>0d,PlanetaryPatchEdge.PositiveV=>1d,_=>along};return CubeSphereProjection.Project(face,u,v,1d);}
}

static unsafe void PlanetaryPatchTopologyAndAbiTest()
{
    var topology=PlanetaryPatchTopology.Shared;var repeated=PlanetaryPatchTopology.Shared;
    Check(topology.Vertices.Length==289&&topology.Indices.Length==1536,"patch grid counts");Check(topology.DeterministicHash==0x654A3EA13F0C9C2DUL&&topology.DeterministicHash==repeated.DeterministicHash,"patch topology regression hash");
    Check(topology.Indices.All(index=>index<topology.Vertices.Length),"patch indices in range");Check(topology.Vertices.All(vertex=>vertex.U is >=0 and <=1&&vertex.V is >=0 and <=1),"patch coordinates bounded");Check(topology.Vertices[0]==new PlanetaryPatchTopology.Vertex(0,0)&&topology.Vertices[^1]==new PlanetaryPatchTopology.Vertex(1,1),"patch corners");
    Check(Marshal.SizeOf<NativePlanetaryPatch>()==64,"planetary patch ABI size");Check(Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.Face)).ToInt32()==0&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.CenterX)).ToInt32()==16&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.ColorR)).ToInt32()==32&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.StitchMask)).ToInt32()==48,"planetary patch ABI offsets");
    var patch=new NativePlanetaryPatch{Face=5,Level=3,X=7,Y=6,CenterX=BitConverter.Int32BitsToSingle(unchecked((int)0x3F800001)),CenterY=2,CenterZ=3,Radius=BitConverter.Int32BitsToSingle(unchecked((int)0x41200001)),ColorA=1,StitchMask=15};Check(patch.Face==5&&patch.Level==3&&patch.X==7&&patch.Y==6&&patch.StitchMask==15&&BitConverter.SingleToInt32Bits(patch.CenterX)==unchecked((int)0x3F800001)&&BitConverter.SingleToInt32Bits(patch.Radius)==unchecked((int)0x41200001),"planetary patch ABI bit preservation");
    Check(NativeRuntime.ValidatePlanetaryPatches(null,0)==NativeResult.Success,"native zero patch batch");Check(NativeRuntime.ValidatePlanetaryPatches(null,1)==NativeResult.InvalidArgument,"native null nonzero rejected");var pointer=&patch;Check(NativeRuntime.ValidatePlanetaryPatches(pointer,1)==NativeResult.Success,"native valid patch");var batch=stackalloc NativePlanetaryPatch[6];for(uint face=0;face<6;face++){batch[face]=patch;batch[face].Face=face;}Check(NativeRuntime.ValidatePlanetaryPatches(batch,6)==NativeResult.Success,"native six face batch");batch[0].Face=6;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native face rejected");batch[0]=patch;batch[0].Radius=0;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native zero radius rejected");batch[0]=patch;batch[0].Radius=float.NaN;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native nan radius rejected");batch[0]=patch;batch[0].CenterX=float.PositiveInfinity;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native infinite center rejected");batch[0]=patch;batch[0].Level=2;batch[0].X=4;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native level coordinates rejected");batch[0]=patch;batch[0].StitchMask=16;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native stitch mask rejected");batch[0]=patch;batch[0].Reserved0=1;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native reserved metadata rejected");
    Console.WriteLine($"Planetary patch topology hash: 0x{topology.DeterministicHash:X16}");
}

static void PlanetaryEyeballTopologyAndAbiTest()
{
    var topology=PlanetaryEyeballTopology.Shared;
    Check(PlanetaryEyeballTopology.RadialRingCount==128&&PlanetaryEyeballTopology.AzimuthSegmentCount==256&&PlanetaryEyeballTopology.VertexCount==32_769&&PlanetaryEyeballTopology.IndexCount==195_840,"fixed V2 workload dimensions");
    Check(topology.Indices.Length==PlanetaryEyeballTopology.IndexCount&&topology.Indices.ToArray().All(index=>index<PlanetaryEyeballTopology.VertexCount),"eyeball topology indices in range");
    Check(topology.DeterministicHash==0x4A46E29A7D6E90A7UL&&topology.DeterministicHash==PlanetaryEyeballTopology.Shared.DeterministicHash,"eyeball topology regression hash");
    Check(topology.Indices[..6].SequenceEqual(new uint[]{0,1,2,0,2,3})&&topology.Indices[^6..].SequenceEqual(new uint[]{32_512,32_768,32_257,32_257,32_768,32_513}),"center fan and final annulus ordering");
    var previous=0d;for(var ring=1;ring<=PlanetaryEyeballTopology.RadialRingCount;ring++){var radius=PlanetaryEyeballTopology.WarpedRadius(ring);Check(radius>previous&&radius is >0 and <=1,"monotonic squared radial warp");previous=radius;}
    var pupil=new Double3(.3,.4,.5).Normalized();var first=PlanetaryEyeballTopology.DirectionAt(pupil,1,0,.25);var repeated=PlanetaryEyeballTopology.DirectionAt(pupil,1,0,.25);var rim=PlanetaryEyeballTopology.DirectionAt(pupil,PlanetaryEyeballTopology.RadialRingCount,255,.25);Check(first==repeated&&Math.Abs(first.LengthSquared-1)<1e-12&&Math.Abs(rim.LengthSquared-1)<1e-12,"body-fixed pupil mapping deterministic and spherical");
    Check(Marshal.SizeOf<NativePlanetaryEyeball>()==128&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.CameraBodyHighX)).ToInt32()==0&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.CameraBodyLowX)).ToInt32()==16&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.SurfaceAltitudeMetres)).ToInt32()==32&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.BodyIdLow)).ToInt32()==48&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.ViewForwardX)).ToInt32()==64&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.RadialWarpExponent)).ToInt32()==80&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.VertexCount)).ToInt32()==96&&Marshal.OffsetOf<NativePlanetaryEyeball>(nameof(NativePlanetaryEyeball.Reserved0)).ToInt32()==112,"eyeball fixed-width ABI layout");
    Check(PlanetaryEyeballHandoff.EyeballWeight(2_000_000)==0f&&PlanetaryEyeballHandoff.EyeballWeight(1_500_000)==.5f&&PlanetaryEyeballHandoff.EyeballWeight(1_000_000)==1f&&PlanetaryEyeballHandoff.EyeballWeight(2)==1f,"bounded deterministic regional/eyeball handoff");
    var direction=new Double3(.31,-.74,.59).Normalized();var terrain=PlanetaryTerrainDefinition.EarthEyeballV2;var h0=terrain.SampleHeight(direction,0);Check(h0==terrain.SampleHeight(direction,12)&&h0==terrain.SampleHeight(direction,24),"terrain truth independent of regional topology level");
    Console.WriteLine($"Planetary eyeball topology hash: 0x{topology.DeterministicHash:X16}; vertices={PlanetaryEyeballTopology.VertexCount}; indices={PlanetaryEyeballTopology.IndexCount}");
}

static void PlanetaryTerrainResidencyAndSurfaceFrameTest()
{
    var terrain=PlanetaryTerrainDefinition.EarthProceduralV1;Check(terrain.IsValid&&terrain.MaximumHeightMetres==7_600d&&PlanetaryTerrainDefinition.GridVertexCount==289,"versioned bounded Earth terrain definition");
    var directions=new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ,new Double3(1,2,3).Normalized()};var first=directions.Select(direction=>terrain.SampleHeight(direction,22)).ToArray();var repeated=directions.Select(direction=>terrain.SampleHeight(direction,22)).ToArray();Check(first.SequenceEqual(repeated)&&first.All(height=>height>=0&&height<=terrain.MaximumHeightMetres),"terrain evaluation deterministic and bounded");
    foreach(var t in new[]{0d,.25d,.5d,.75d,1d}){var a=CubeSphereProjection.Project(CubeSphereFace.PositiveX,0,t,1);var b=CubeSphereProjection.Project(CubeSphereFace.PositiveZ,1,t,1);Check(Math.Abs(terrain.SampleHeight(a,22)-terrain.SampleHeight(b,22))<1e-9,"direction-space terrain is continuous across cube faces");}
    var cache=new PlanetaryTerrainResidencyCache(2);var keys=new[]{new PlanetaryTerrainPatchKey(6,CubeSphereFace.PositiveZ,8,127,127,terrain.Version,terrain.SourceId),new PlanetaryTerrainPatchKey(6,CubeSphereFace.PositiveZ,8,128,127,terrain.Version,terrain.SourceId),new PlanetaryTerrainPatchKey(6,CubeSphereFace.PositiveZ,8,128,128,terrain.Version,terrain.SourceId)};var tile=cache.Acquire(keys[0],terrain);var tileRepeat=cache.Acquire(keys[0],terrain);cache.Acquire(keys[1],terrain);cache.Acquire(keys[2],terrain);var statistics=cache.Statistics;Check(ReferenceEquals(tile,tileRepeat)&&statistics.Hits==1&&statistics.Misses==3&&statistics.Generated==3&&statistics.Evictions==1&&statistics.ResidentCount==2&&statistics.Capacity==2&&statistics.ResidentBytes==2L*289*sizeof(float),"bounded deterministic terrain LRU accounting");
    var frame=PlanetarySurfaceFrame.AtDirection(new Double3(1,2,3));var orientation=frame.HorizonViewOrientation();Check(Math.Abs(Double3.Dot(frame.East,frame.North))<1e-12&&Math.Abs(Double3.Dot(frame.East,frame.Up))<1e-12&&Math.Abs(Double3.Dot(frame.North,frame.Up))<1e-12&&Math.Abs(frame.East.LengthSquared-1)<1e-12&&Math.Abs(orientation.LengthSquared-1)<1e-12,"stable orthonormal local tangent frame");
}

static void PlanetaryRepresentationHandoffTest()
{
    var root=new ReferenceFrameId(1);var body=new PlanetRenderProxy(42,new UniversePosition(Double3.Zero,root),10,new Float3(.1f,.2f,.3f),"Generic",true,DoubleQuaternion.Identity);var before=body;var config=new PlanetaryRepresentationHandoffConfiguration(12,18,.25);
    Check(config.IsValid,"handoff configuration");var controller=new PlanetaryRepresentationHandoff(config);
    var far=controller.Update(body,new Double3(0,0,200));Check(far.Regime==PlanetaryRenderRegime.DistantOnly&&far.DistantAlpha==1&&far.DetailedAlpha==0&&far.DistanceRadii==20&&far.DrawDistant&&!far.DrawDetailed,"distant-only selection");
    Check(controller.Update(body,new Double3(0,0,179)).Regime==PlanetaryRenderRegime.DistantOnly,"distant boundary hysteresis hold");var transition=controller.Update(body,new Double3(0,0,177));Check(transition.Regime==PlanetaryRenderRegime.Transition&&transition.DistantAlpha>0&&transition.DetailedAlpha>0&&Math.Abs(transition.DistantAlpha+transition.DetailedAlpha-1)<1e-6,"transition selection and normalized weights");
    var transitionRepeat=controller.Update(body,new Double3(0,0,177));Check(transitionRepeat==transition,"identical state produces identical handoff");var middle=controller.Update(body,new Double3(0,0,150));var inner=controller.Update(body,new Double3(0,0,130));Check(middle.Regime==PlanetaryRenderRegime.Transition&&inner.Regime==PlanetaryRenderRegime.Transition&&transition.DetailedAlpha<middle.DetailedAlpha&&middle.DetailedAlpha<inner.DetailedAlpha,"transition weights monotonic");
    Check(controller.Update(body,new Double3(0,0,121)).Regime==PlanetaryRenderRegime.Transition,"detailed boundary hysteresis hold");var detailed=controller.Update(body,new Double3(0,0,117));Check(detailed.Regime==PlanetaryRenderRegime.DetailedOnly&&detailed.DistantAlpha==0&&detailed.DetailedAlpha==1&&!detailed.DrawDistant&&detailed.DrawDetailed,"detailed-only selection");Check(controller.Update(body,new Double3(0,0,121)).Regime==PlanetaryRenderRegime.DetailedOnly,"detailed hysteresis prevents chatter");Check(controller.Update(body,new Double3(0,0,123)).Regime==PlanetaryRenderRegime.Transition,"detailed hysteresis release");
    var freshTransition=new PlanetaryRepresentationHandoff(config).Update(body,new Double3(0,0,150));Check(freshTransition.Regime==PlanetaryRenderRegime.Transition,"stateless initial transition");Check(body==before,"handoff does not mutate celestial presentation proxy");
}

static void DistantQuaternionTransformParityTest()
{
    static Double3 ShaderRotate(in Double3 point,in DoubleQuaternion quaternion)
    {
        var vector=new Double3(quaternion.X,quaternion.Y,quaternion.Z);
        return point+Double3.Cross(vector,Double3.Cross(vector,point)+point*quaternion.W)*2d;
    }
    static DoubleQuaternion ShaderQuaternion(in DoubleQuaternion value)=>new((float)value.X,(float)value.Y,(float)value.Z,(float)value.W);
    static Double3 DetailedDirection(in Double3 bodyLocal,in DoubleQuaternion encodedBodyFixedToRoot)=>ShaderRotate(bodyLocal,encodedBodyFixedToRoot);
    static Double3 DistantDirection(in Double3 bodyLocal,in DoubleQuaternion encodedBodyFixedToRoot)=>ShaderRotate(bodyLocal,encodedBodyFixedToRoot);
    static Double3 BodyFixedSun(in Double3 bodyToSunRoot,in DoubleQuaternion encodedBodyFixedToRoot)=>ShaderRotate(bodyToSunRoot,new(-encodedBodyFixedToRoot.X,-encodedBodyFixedToRoot.Y,-encodedBodyFixedToRoot.Z,encodedBodyFixedToRoot.W)).Normalized();
    static void VectorNear(in Double3 actual,in Double3 expected,double tolerance,string message)=>Check((actual-expected).LengthSquared<=tolerance*tolerance,message);

    var known=new[]
    {
        ("identity",DoubleQuaternion.Identity,Double3.UnitX,Double3.UnitX),
        ("+90 X",DoubleQuaternion.FromAxisAngle(Double3.UnitX,Math.PI/2d),Double3.UnitY,Double3.UnitZ),
        ("+90 Y",DoubleQuaternion.FromAxisAngle(Double3.UnitY,Math.PI/2d),Double3.UnitZ,Double3.UnitX),
        ("+90 Z",DoubleQuaternion.FromAxisAngle(Double3.UnitZ,Math.PI/2d),Double3.UnitX,Double3.UnitY),
        ("-90 Z",DoubleQuaternion.FromAxisAngle(Double3.UnitZ,-Math.PI/2d),Double3.UnitX,-Double3.UnitY)
    };
    foreach(var (label,quaternion,input,expected) in known)VectorNear(ShaderRotate(input,ShaderQuaternion(quaternion)),expected,2e-7d,$"GLSL quaternion helper {label}");

    var t0=SimulationInstant.Zero;var t1=SimulationInstant.FromWholeSeconds(3_600);var reference=Double3.UnitX;
    var bodies=new[]{SolarSystemBodyIds.Earth,SolarSystemBodyIds.Moon,SolarSystemBodyIds.Mars,SolarSystemBodyIds.Jupiter,SolarSystemBodyIds.Saturn};
    foreach(var bodyId in bodies)
    {
        Check(CelestialBodyOrientationEvaluator.TryEvaluate(bodyId,t0,out var orientation0),$"T0 orientation sample for body {bodyId.Value}");Check(CelestialBodyOrientationEvaluator.TryEvaluate(bodyId,t1,out var orientation1),$"T1 orientation sample for body {bodyId.Value}");
        var encoded0=ShaderQuaternion(orientation0.BodyFixedToInertial);var encoded1=ShaderQuaternion(orientation1.BodyFixedToInertial);
        var cpu0=orientation0.BodyFixedToInertial.Rotate(reference);var cpu1=orientation1.BodyFixedToInertial.Rotate(reference);
        var detailed0=DetailedDirection(reference,encoded0);var detailed1=DetailedDirection(reference,encoded1);var distant0=DistantDirection(reference,encoded0);var distant1=DistantDirection(reference,encoded1);
        VectorNear(detailed0,cpu0,2e-7d,$"body {bodyId.Value} T0 CPU/detailed direction");VectorNear(distant0,detailed0,0d,$"body {bodyId.Value} T0 detailed/distant direction");
        VectorNear(detailed1,cpu1,2e-7d,$"body {bodyId.Value} T1 CPU/detailed direction");VectorNear(distant1,detailed1,0d,$"body {bodyId.Value} T1 detailed/distant direction");
        var delta=(orientation1.BodyFixedToInertial*orientation0.BodyFixedToInertial.Conjugate()).Normalized();var axis=new Double3(delta.X,delta.Y,delta.Z);if(axis.LengthSquared>1e-20d){var cpuSign=Math.Sign(Double3.Dot(axis,Double3.Cross(cpu0,cpu1)));var detailedSign=Math.Sign(Double3.Dot(axis,Double3.Cross(detailed0,detailed1)));var distantSign=Math.Sign(Double3.Dot(axis,Double3.Cross(distant0,distant1)));Check(cpuSign!=0&&cpuSign==detailedSign&&detailedSign==distantSign,$"body {bodyId.Value} T0/T1 signed rotation direction parity");}
    }

    var root=new ReferenceFrameId(1);Check(SolarSystemScene.TryCreateAt(root,t0,out var scene,out var error)&&scene is not null,$"distant parity Solar scene: {error}");var sol=scene!;var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);
    foreach(var focus in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Moon,NativePresentationFocus.Mars,NativePresentationFocus.Jupiter,NativePresentationFocus.Saturn})
    {
        Check(sol.Focus(camera,focus),$"distant parity focus {focus}");var body=sol.FocusedBody;camera.Position=camera.Position with{Value=body.Position.Value+Double3.UnitZ*body.RadiusMetres*30d};camera.Orientation=DoubleQuaternion.Identity;sol.Update(camera);Check(sol.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly,$"{focus} distant-only parity state");
        sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);var beforeBody=sol.FocusedBody;var beforeNative=sol.DistantBodies[0];var beforeQuaternion=ShaderQuaternion(beforeBody.BodyFixedToRoot);var beforeRootDirection=DetailedDirection(reference,beforeQuaternion);var beforeSunRoot=sol.Presentation.Bodies[0].Position.Value-beforeBody.Position.Value;var beforeSun=BodyFixedSun(beforeSunRoot,beforeQuaternion);
        sol.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=73,MouseDeltaY=-29},out _,out _);var afterBody=sol.FocusedBody;var afterNative=sol.DistantBodies[0];var afterQuaternion=ShaderQuaternion(afterBody.BodyFixedToRoot);var afterRootDirection=DistantDirection(reference,afterQuaternion);var afterSunRoot=sol.Presentation.Bodies[0].Position.Value-afterBody.Position.Value;var afterSun=BodyFixedSun(afterSunRoot,afterQuaternion);
        Check(sol.IsPaused&&beforeBody.BodyFixedToRoot==afterBody.BodyFixedToRoot&&beforeNative.BodyOrientationX==afterNative.BodyOrientationX&&beforeNative.BodyOrientationY==afterNative.BodyOrientationY&&beforeNative.BodyOrientationZ==afterNative.BodyOrientationZ&&beforeNative.BodyOrientationW==afterNative.BodyOrientationW,$"{focus} paused camera does not alter distant body quaternion");
        VectorNear(afterRootDirection,beforeRootDirection,0d,$"{focus} paused camera preserves distant root reference direction");VectorNear(afterSun,beforeSun,1e-12d,$"{focus} paused camera preserves body-fixed Sun direction");
        var encodedBodyToSun=BodyFixedSun(new Double3(sol.SolarLighting(camera).SourceCenterX-afterNative.CenterX,sol.SolarLighting(camera).SourceCenterY-afterNative.CenterY,sol.SolarLighting(camera).SourceCenterZ-afterNative.CenterZ),afterQuaternion);VectorNear(encodedBodyToSun,afterSun,3e-7d,$"{focus} camera-relative distant/detailed Sun direction parity");
        sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
    }
    Console.WriteLine("Distant transform parity: helper=identity/+90X/+90Y/+90Z/-90Z; bodies=Earth/Moon/Mars/Jupiter/Saturn; T0=0s; T1=3600s; paused-camera and Sun vectors invariant");
}

static void DistantVisibleHemisphereWindingTest()
{
    static Double3 Cross(in Double3 a,in Double3 b)=>Double3.Cross(a,b);
    static double SignedWinding(in Double3 a,in Double3 b,in Double3 c)
    {
        var centroid=(a+b+c)/3d;
        return Double3.Dot(Cross(b-a,c-a),centroid);
    }
    static Double3 SphereVertex(int latitude,int longitude)
    {
        const int latitudeSegments=12,longitudeSegments=24;
        if(latitude==0)return Double3.UnitY;
        var phi=Math.PI*latitude/latitudeSegments;var theta=Math.Tau*longitude/longitudeSegments;
        return new(Math.Sin(phi)*Math.Cos(theta),Math.Cos(phi),Math.Sin(phi)*Math.Sin(theta));
    }
    static Double3 CubeFace0(double u,double v)
    {
        var a=2d*u-1d;var b=2d*v-1d;
        return new Double3(1d,b,-a).Normalized();
    }

    var distantWinding=SignedWinding(SphereVertex(0,0),SphereVertex(1,1),SphereVertex(1,0));
    var detailedWinding=SignedWinding(CubeFace0(0,0),CubeFace0(0,1),CubeFace0(1,0));
    Check(distantWinding>0d&&detailedWinding<0d,"native distant sphere and detailed cube-sphere use opposite authored winding");

    var nativePath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","NovaCoreNative.cpp"));
    var nativeSource=File.ReadAllText(nativePath);
    Check(nativeSource.Contains("planetaryRaster.frontFace=VK_FRONT_FACE_CLOCKWISE",StringComparison.Ordinal)&&nativeSource.Contains("distantRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE",StringComparison.Ordinal),"oppositely wound meshes select the same outward visible hemisphere");
    Check(!nativeSource.Contains("distantRaster.frontFace=VK_FRONT_FACE_CLOCKWISE",StringComparison.Ordinal),"distant sphere cannot regress to rendering its back hemisphere");
    Console.WriteLine($"Visible-hemisphere winding: detailed={detailedWinding:R}; distant={distantWinding:R}; dedicated front-face conventions preserve outward surface visibility");
}

static void SolarSystemSceneTest()
{
    var root=new ReferenceFrameId(1);Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var scene,out var error)&&scene is not null,$"Solar deterministic scene: {error}");var sol=scene!;Check(sol.Presentation.Count==10&&sol.CurrentTime==SimulationInstant.Zero&&sol.StartupUtc is null,"ten rendered Solar bodies at explicit deterministic ET0");var bodies=sol.Presentation.Bodies.ToArray();Check(bodies.Select(body=>body.BodyId).Distinct().Count()==10&&bodies.Select(body=>body.BodyId).SequenceEqual(SolarSystemScene.BodyOrder),"stable unique body IDs");var frozen=bodies.ToArray();
    var suppliedUtc=new DateTimeOffset(2024,1,1,0,0,0,TimeSpan.Zero);var suppliedTicks=checked(suppliedUtc.UtcDateTime.Ticks-DateTime.UnixEpoch.Ticks);Check(SolarUtcTime.TryToSimulationInstant(new UtcInstant(suppliedTicks),out var suppliedInstant),"injected UTC conversion");var provider=new FixedUtcTimeProvider(suppliedUtc);Check(SolarSystemScene.TryCreate(root,provider,out var currentScene,out var currentError)&&currentScene is not null,$"injected current-epoch scene: {currentError}");var current=currentScene!;Check(provider.QueryCount==1&&current.StartupUtc==suppliedUtc&&current.CurrentTime==suppliedInstant,"fresh Solar startup queries supplied UTC exactly once and publishes its converted instant");var repeatedProvider=new FixedUtcTimeProvider(suppliedUtc);Check(SolarSystemScene.TryCreate(root,repeatedProvider,out var repeatedCurrent,out currentError)&&repeatedCurrent is not null&&repeatedCurrent.CurrentTime==current.CurrentTime&&repeatedCurrent.Presentation.Bodies.SequenceEqual(current.Presentation.Bodies),"same injected UTC produces identical initial celestial state");
    Check(Enum.GetUnderlyingType(typeof(NativePresentationFocus))==typeof(uint)&&Enumerable.Range(0,11).Select(value=>(uint)(NativePresentationFocus)value).SequenceEqual(Enumerable.Range(0,11).Select(value=>(uint)value)),"fixed-width deterministic focus enum");

    var system=SolAnalyticalDefinition.Instance;var evaluations=new ReferenceFrameEvaluation[system.Count];var roots=new FrameTransform[system.Count];var staging=new ReferenceFrameEvaluation[system.Count];var stagingRoots=new FrameTransform[system.Count];var result=CelestialSystemEvaluator.TryEvaluateSystem(system,SimulationInstant.Zero,evaluations,roots,staging,stagingRoots);Check(result.Succeeded,"independent Sol evaluation");
    Double3 EvaluatedCenter(ulong bodyId){for(var i=0;i<system.Count;i++)if(system.GetNodeInTraversalOrder(i).Id.Value==bodyId)return roots[i].Translation;throw new InvalidOperationException("Body absent from independently evaluated Sol system.");}
    foreach(var body in bodies)Check(body.Position.Value==EvaluatedCenter(body.BodyId),$"body {body.BodyId} uses evaluated root center");

    var camera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);sol.ResetPresentationCamera(camera);sol.Update(camera);Check(sol.CameraPresentationMode==SolarCameraPresentationMode.SolarMap&&sol.FocusIndex==0&&sol.CurrentFocusTarget==FocusTarget.BodyCenter(sol.FocusedBody.BodyId)&&sol.OrbitDistance==SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu&&sol.OrbitYawRadians==0d&&sol.OrbitPitchRadians==SolarSystemScene.SolarMapPitchRadians,"explicit deterministic Solar Map home state and orientation-free body-center focus identity");Check(sol.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly&&!sol.DetailedComputeRequested,"focused far body remains distant-only");Check(frozen.SequenceEqual(sol.Presentation.Bodies.ToArray()),"Solar Map camera does not mutate celestial presentation");
    Check(bodies.Select(body=>body.Label).Distinct().Count()==10,"presentation labels map uniquely to body identities");Check(sol.OrbitRootSamples.Length==SolarSystemScene.OrbitPathCount*SolarSystemScene.OrbitSampleCount&&sol.OrbitVertices.Length==SolarSystemScene.OrbitVertexCount,"bounded nine-path solar trajectory transport");var frozenOrbit=sol.OrbitRootSamples.ToArray();var frozenOrbitVertices=sol.OrbitVertices.ToArray();for(var path=0;path<SolarSystemScene.OrbitPathCount;path++)Check(sol.OrbitRootSamples[path*SolarSystemScene.OrbitSampleCount]==bodies[path+1].Position.Value,$"orbit path {path} begins at its evaluated authoritative center");var earthPath=2*SolarSystemScene.OrbitSampleCount;var moonPath=SolarSystemScene.MoonOrbitPathIndex*SolarSystemScene.OrbitSampleCount;Check(Math.Abs(Math.Sqrt((sol.OrbitRootSamples[earthPath]-sol.OrbitRootSamples[moonPath]).LengthSquared)-Math.Sqrt((bodies[3].Position.Value-bodies[4].Position.Value).LengthSquared))<1e-6,"Moon path preserves Earth-relative hierarchy after root resolution");var moonTraversal=Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),index=>system.GetNodeInTraversalOrder(index).Id==SolarSystemBodyIds.Moon);var moonBinding=system.GetNodeInTraversalOrder(moonTraversal).Ephemeris;Check(system.TryGetAnalyticalKepler(moonBinding.PayloadIndex,out var moonTrajectory),"corrected Moon orbit trajectory available");Check(system.TryGetAnalyticalCorrection(moonBinding.PayloadIndex,out var moonCorrection)&&moonCorrection.IsValid,"corrected Moon orbit timing available");Check(system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth,out var earthProperties),"corrected Moon parent constants available");var moonRadius=Math.Sqrt(moonTrajectory.StateAtEpoch.Position.LengthSquared);var moonAlpha=2d/moonRadius-moonTrajectory.StateAtEpoch.Velocity.LengthSquared/earthProperties.GravitationalParameter;var moonOsculatingPeriod=2d*Math.PI/(Math.Sqrt(earthProperties.GravitationalParameter)*moonAlpha*Math.Sqrt(moonAlpha));var moonPeriod=moonOsculatingPeriod/moonCorrection.TimeScale;Check(sol.MoonOrbitPeriodSeconds==moonPeriod,"Moon presentation period follows corrected runtime mean motion");var moonAuthoritySample=64;var moonAuthorityTime=SimulationInstant.FromSecondsRounded(moonPeriod*moonAuthoritySample/SolarSystemScene.OrbitSegmentCount);var earthTraversal=Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),index=>system.GetNodeInTraversalOrder(index).Id==SolarSystemBodyIds.Earth);Check(CelestialSystemEvaluator.TryEvaluateSystem(system,moonAuthorityTime,evaluations,roots,staging,stagingRoots).Succeeded,"future corrected Moon orbit evaluation");var expectedMoonPresentationPoint=bodies[3].Position.Value+roots[moonTraversal].Translation-roots[earthTraversal].Translation;Check(sol.OrbitRootSamples[moonPath+moonAuthoritySample]==expectedMoonPresentationPoint,"Moon orbit line samples the corrected Earth-relative runtime trajectory around the current evaluated Earth center");var maximumMoonPresentationRadius=Enumerable.Range(0,SolarSystemScene.OrbitSegmentCount).Max(sample=>Math.Sqrt((sol.OrbitRootSamples[moonPath+sample]-bodies[3].Position.Value).LengthSquared));Check(maximumMoonPresentationRadius<500_000_000d,"Moon presentation orbit remains Earth-local rather than heliocentric");Check(sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount]==sol.OrbitRootSamples[moonPath],"Moon orbit stores an exact closure sentinel rather than relying on future-state coincidence");var moonFirstRelative=sol.OrbitRootSamples[moonPath]-camera.Position.Value;var moonClosingVertex=sol.OrbitVertices[(SolarSystemScene.MoonOrbitPathIndex*SolarSystemScene.OrbitSegmentCount+SolarSystemScene.OrbitSegmentCount-1)*2+1];Check(moonClosingVertex.X==(float)moonFirstRelative.X&&moonClosingVertex.Y==(float)moonFirstRelative.Y&&moonClosingVertex.Z==(float)moonFirstRelative.Z,"Moon orbit renderer explicitly connects the last unique point to sample zero");Console.WriteLine($"Moon presentation orbit: old={moonOsculatingPeriod:R}s ({moonOsculatingPeriod/86_400d:R}d); corrected={moonPeriod:R}s ({moonPeriod/86_400d:R}d); uniqueSamples={SolarSystemScene.OrbitSegmentCount}; closed=true");Check(CelestialSystemEvaluator.TryEvaluateSystem(system,SimulationInstant.Zero,evaluations,roots,staging,stagingRoots).Succeeded,"restore independent Sol epoch evaluation");for(var index=0;index<10;index++){var body=bodies[index];var record=sol.DistantBodies[index];var relative=body.Position.Value-camera.Position.Value;Check((record.Enabled&255u)==(uint)(index+1)&&record.CenterX==(float)relative.X&&record.CenterY==(float)relative.Y&&record.CenterZ==(float)relative.Z,$"label and marker anchor {body.Label} uses evaluated center");}Check((sol.DistantBodies[0].Enabled&0x80000000u)!=0,"focused label/marker metadata");

    var moonControls=sol.MoonOrbitControlSamples;Check(moonControls[moonAuthoritySample]==expectedMoonPresentationPoint,"Moon periodic curve retains the corrected Earth-relative runtime samples as its interpolation controls");
    var moonPeriodicControls=sol.MoonOrbitPeriodicControlSamples;var moonMaximumFitDeviation=0d;for(var sample=0;sample<SolarSystemScene.OrbitSegmentCount;sample++)moonMaximumFitDeviation=Math.Max(moonMaximumFitDeviation,Math.Sqrt((moonPeriodicControls[sample]-moonControls[sample]).LengthSquared));Console.WriteLine($"Moon periodic fit: endpointMismatch={sol.MoonOrbitEndpointMismatchMetres:R}m; maximumControlCorrection={moonMaximumFitDeviation:R}m");Check(moonMaximumFitDeviation<=sol.MoonOrbitEndpointMismatchMetres*1.01d,"Moon periodic presentation correction remains bounded by the measured endpoint mismatch");
    var moonClosurePositionError=Math.Sqrt((sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount]-sol.OrbitRootSamples[moonPath]).LengthSquared);
    var moonPreviousLength=Math.Sqrt((sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]-sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-2]).LengthSquared);var moonClosingLength=Math.Sqrt((sol.OrbitRootSamples[moonPath]-sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).LengthSquared);var moonNextLength=Math.Sqrt((sol.OrbitRootSamples[moonPath+1]-sol.OrbitRootSamples[moonPath]).LengthSquared);var moonSeamMeanLength=(moonPreviousLength+moonClosingLength+moonNextLength)/3d;var moonMaximumSegmentDiscontinuity=Math.Max(Math.Abs(moonClosingLength-moonPreviousLength),Math.Abs(moonNextLength-moonClosingLength))/moonSeamMeanLength;
    var moonIncoming=(sol.OrbitRootSamples[moonPath]-sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).Normalized();var moonOutgoing=(sol.OrbitRootSamples[moonPath+1]-sol.OrbitRootSamples[moonPath]).Normalized();var moonRenderedTurnDegrees=Math.Acos(Math.Clamp(Double3.Dot(moonIncoming,moonOutgoing),-1d,1d))*180d/Math.PI;var moonAnalyticIncoming=(moonPeriodicControls[1]-moonPeriodicControls[SolarSystemScene.OrbitSegmentCount-1])*.5d;var moonAnalyticOutgoing=(moonPeriodicControls[1]-moonPeriodicControls[SolarSystemScene.OrbitSegmentCount-1])*.5d;var moonTangentAngleDegrees=Math.Acos(Math.Clamp(Double3.Dot(moonAnalyticIncoming.Normalized(),moonAnalyticOutgoing.Normalized()),-1d,1d))*180d/Math.PI;
    if(moonAnalyticIncoming==moonAnalyticOutgoing)moonTangentAngleDegrees=0d;
    var moonPreviousDirection=(sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]-sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-2]).Normalized();var moonNextDirection=(sol.OrbitRootSamples[moonPath+2]-sol.OrbitRootSamples[moonPath+1]).Normalized();Check(Double3.Dot(moonPreviousDirection,moonIncoming)>.99d&&Double3.Dot(moonIncoming,moonOutgoing)>.99d&&Double3.Dot(moonOutgoing,moonNextDirection)>.99d,"Moon seam remains monotonic without a local loop or reversal");Check(moonClosurePositionError==0d,"Moon periodic curve has exact positional closure");Check(moonTangentAngleDegrees<1e-9d,"wrapped periodic cubic has C1 tangent continuity at the seam");Check(moonRenderedTurnDegrees<4d,"Moon display polyline has no visible seam kink");Check(moonMaximumSegmentDiscontinuity<.05d,"Moon seam segment lengths remain comparable to both neighbors");Console.WriteLine($"Moon periodic seam: positionError={moonClosurePositionError:R}m; tangentDiscontinuity={moonTangentAngleDegrees:R}deg; renderedTurn={moonRenderedTurnDegrees:R}deg; previousSegment={moonPreviousLength:R}m; seamSegment={moonClosingLength:R}m; nextSegment={moonNextLength:R}m; maximumRelativeSegmentDiscontinuity={moonMaximumSegmentDiscontinuity:R}");
    Check(sol.DistantBodies.Skip(1).All(body=>body.BodyIdLow!=0&&body.MaterialKind is >=1 and <=4&&body.AlbedoSource is >=1 and <=10),"all nine planets share the generic native material contract");Check(sol.DistantBodies.Skip(1).Select(body=>body.AlbedoSource).Distinct().Count()==9,"Solar material sources preserve body identities");var saturnRecord=sol.DistantBodies.Single(body=>body.BodyIdLow==10);Check(saturnRecord.RingAssociation!=0&&saturnRecord.RingInnerRadiusRatio>1&&saturnRecord.RingOuterRadiusRatio>saturnRecord.RingInnerRadiusRatio,"Saturn alone publishes the generic ring association");
    var overviewLabels=sol.VisibleLabelIds.ToArray();Check(overviewLabels.Length is >0 and <=10&&overviewLabels[0]==SolarSystemBodyIds.Sun.Value,"Solar Map overview preserves focused-Sun label priority");Check(NonOverlapping(overviewLabels),"accepted overview labels satisfy deterministic clearance margin");foreach(var id in overviewLabels){Check(sol.TryGetLabelBounds(id,out var bounds)&&bounds.IsFinite&&bounds.MinX>=-1d+SolarOverlayLayout.ScreenEdgeMarginNdc&&bounds.MaxX<=1d-SolarOverlayLayout.ScreenEdgeMarginNdc&&bounds.MinY>=-1d+SolarOverlayLayout.ScreenEdgeMarginNdc&&bounds.MaxY<=1d-SolarOverlayLayout.ScreenEdgeMarginNdc,"accepted overview label remains fully on screen");}for(var index=0;index<10;index++)Check(((sol.DistantBodies[index].Enabled&SolarSystemScene.LabelVisibleBit)!=0)==overviewLabels.Contains(SolarSystemScene.BodyOrder[index]),"native label visibility metadata matches managed selection");var mapOrbitOpacity=sol.OrbitOpacityBytes.ToArray();Check(sol.VisibleOrbitCount==SolarSystemScene.OrbitPathCount&&mapOrbitOpacity[3]<mapOrbitOpacity[0]&&mapOrbitOpacity.Where((_,index)=>index!=3).Distinct().Count()==1,"Solar Map shows all major paths with a subordinate lunar path");Check(sol.VisibleMarkerCount==10&&sol.DistantBodies.All(body=>(body.Enabled&SolarSystemScene.MarkerVisibleBit)!=0),"Solar Map markers remain available for sub-pixel bodies");Check(sol.DistantBodies.Count(body=>(body.Enabled&SolarSystemScene.StellarPresentationBit)!=0)==1&&(sol.DistantBodies.Single(body=>(body.Enabled&SolarSystemScene.StellarPresentationBit)!=0).Enabled&255u)==1,"exactly the authoritative Sun enters the stellar pipeline");var overviewLighting=sol.SolarLighting(camera);Check(overviewLighting.Enabled==1&&overviewLighting.Exposure>0&&overviewLighting.SourceRadiance>1&&overviewLighting.AmbientFloor is >0 and <.1f,"Solar scene publishes bounded HDR lighting");var overviewBatch=sol.DistantBodies.ToArray();var overviewOrbitOpacity=sol.OrbitOpacityBytes.ToArray();sol.Update(camera);Check(overviewLabels.SequenceEqual(sol.VisibleLabelIds.ToArray())&&overviewBatch.SequenceEqual(sol.DistantBodies)&&overviewOrbitOpacity.SequenceEqual(sol.OrbitOpacityBytes.ToArray()),"identical overview camera produces identical label, marker, and orbit batch");Check(frozenOrbitVertices.SequenceEqual(sol.OrbitVertices),"camera-relative solar orbit conversion deterministic");
    var labelSnapshot=sol.Presentation;Check(sol.Focus(camera,3),"Earth focus for label priority");var labelEarth=sol.FocusedBody;camera.Position=camera.Position with{Value=labelEarth.Position.Value+Double3.UnitZ*SolAnalyticalDefinition.AstronomicalUnitMetres*45d};camera.Orientation=DoubleQuaternion.Identity;sol.Update(camera);var distantEarthMoonLabels=sol.VisibleLabelIds.ToArray();Check(distantEarthMoonLabels[0]==SolarSystemBodyIds.Earth.Value&&distantEarthMoonLabels.Contains(SolarSystemBodyIds.Earth.Value)&&!distantEarthMoonLabels.Contains(SolarSystemBodyIds.Moon.Value),"focused Earth wins distant Earth-Moon collision");camera.Position=camera.Position with{Value=labelEarth.Position.Value+Double3.UnitZ*690_280_069.1073977d};sol.Update(camera);var nearEarthMoonLabels=sol.VisibleLabelIds.ToArray();Check(nearEarthMoonLabels.Contains(SolarSystemBodyIds.Earth.Value)&&nearEarthMoonLabels.Contains(SolarSystemBodyIds.Moon.Value),"Earth and Moon labels reappear after screen-space separation");sol.Update(camera);Check(nearEarthMoonLabels.SequenceEqual(sol.VisibleLabelIds.ToArray()),"Earth-Moon label selection deterministic");Check(ReferenceEquals(labelSnapshot,sol.Presentation)&&frozen.SequenceEqual(sol.Presentation.Bodies.ToArray()),"label collision decisions do not mutate celestial presentation");Check(sol.Focus(camera,4),"Moon focus for label priority");camera.Position=camera.Position with{Value=sol.FocusedBody.Position.Value+Double3.UnitZ*SolAnalyticalDefinition.AstronomicalUnitMetres*45d};camera.Orientation=DoubleQuaternion.Identity;sol.Update(camera);Check(sol.VisibleLabelIds[0]==SolarSystemBodyIds.Moon.Value&&sol.VisibleLabelIds.Contains(SolarSystemBodyIds.Moon.Value)&&!sol.VisibleLabelIds.Contains(SolarSystemBodyIds.Earth.Value),"focused Moon wins overlapping Earth label");

    bool NonOverlapping(ulong[] ids){for(var left=0;left<ids.Length;left++){Check(sol.TryGetLabelBounds(ids[left],out var leftBounds),"accepted label has bounds");for(var right=left+1;right<ids.Length;right++){Check(sol.TryGetLabelBounds(ids[right],out var rightBounds),"accepted label has comparison bounds");if(SolarOverlayLayout.Overlaps(leftBounds,rightBounds))return false;}}return true;}
    Check(!sol.Focus(camera,NativePresentationFocus.None),"none focus does not select a body");sol.ResetPresentationCamera(camera);var preservedFocusOrientation=camera.Orientation;for(var index=0;index<10;index++){var focus=(NativePresentationFocus)(index+1);Check(sol.Focus(camera,focus),$"focus target {focus}");var firstPosition=camera.Position.Value;var expectedDistance=sol.FocusFramingDistance(sol.FocusedBody);Check(sol.Focus(camera,focus)&&camera.Position.Value==firstPosition,"deterministic extent-aware focus distance");var expectedRegime=index is 0 or 7?PlanetaryRenderRegime.DistantOnly:PlanetaryRenderRegime.DetailedOnly;var expectedCameraOrientation=preservedFocusOrientation;Check(sol.CameraPresentationMode==SolarCameraPresentationMode.Free3D&&camera.Orientation==expectedCameraOrientation&&sol.FocusIndex==index&&sol.FocusedBody.BodyId==SolarSystemScene.BodyOrder[index]&&sol.FocusedBlend.Regime==expectedRegime&&sol.DetailedComputeRequested==(index is not 0 and not 7)&&sol.DistantBodyCount==10,$"focus mapping, inertial camera orientation, and dedicated stellar/ring framing {focus}");var actualDistance=Math.Sqrt((camera.Position.Value-sol.FocusedBody.Position.Value).LengthSquared);Check(Math.Abs(actualDistance-expectedDistance)<=Math.Max(1e-4d,expectedDistance*1e-8d)&&actualDistance>sol.FocusedBody.RadiusMetres*4d,$"positive extent-aware focus distance {focus}");Check(SolarOverlayLayout.TryProjectBody(sol.FocusedBody,camera,out _,out _,out var focusedRadius,out _)&&focusedRadius is >=.06d and <=.151d,"focused body has useful deterministic apparent size");var focusedMaterial=sol.FocusedPresentation(camera);Check(focusedMaterial.BodyIdLow==(uint)sol.FocusedBody.BodyId&&focusedMaterial.AlbedoSource==sol.DistantBodies[0].AlbedoSource&&focusedMaterial.MaterialKind==sol.DistantBodies[0].MaterialKind,$"distant/detail material identity agrees for {focus}");}

    Check(sol.Focus(camera,NativePresentationFocus.Earth),"Earth focus for overlay hierarchy");var earthOrbitOpacity=sol.OrbitOpacityBytes.ToArray();Check(sol.VisibleOrbitCount==2&&earthOrbitOpacity[2]>0&&earthOrbitOpacity[3]>earthOrbitOpacity[2]&&earthOrbitOpacity.Where((_,index)=>index is not 2 and not 3).All(value=>value==0),"Earth-local view retains only focused and child hierarchy orbits");Check((sol.DistantBodies[0].Enabled&SolarSystemScene.MarkerVisibleBit)==0,"rendered focused Earth suppresses redundant marker");Check(sol.Focus(camera,NativePresentationFocus.Jupiter),"Jupiter focus for overlay hierarchy");Check(sol.VisibleOrbitCount==1&&sol.OrbitOpacityBytes[5]>0,"Jupiter-local view retains only the focused hierarchy orbit");var beforeMapReset=sol.Presentation.Bodies.ToArray();var timeBeforeMapReset=sol.CurrentTime;sol.ResetPresentationCamera(camera);sol.Update(camera);Check(sol.CameraPresentationMode==SolarCameraPresentationMode.SolarMap&&sol.FocusIndex==0&&sol.VisibleOrbitCount==SolarSystemScene.OrbitPathCount&&sol.CurrentTime==timeBeforeMapReset&&beforeMapReset.SequenceEqual(sol.Presentation.Bodies),"Solar Map reset restores overview without changing time or celestial evaluation");

    Check(sol.Focus(camera,3),"Earth focus for promotion");var earth=sol.FocusedBody;camera.Position=camera.Position with{Value=earth.Position.Value+Double3.UnitZ*earth.RadiusMetres*15d};sol.Update(camera);Check(sol.FocusedBlend.Regime==PlanetaryRenderRegime.Transition&&sol.FocusedBlend.DrawDetailed&&sol.FocusedBlend.DrawDistant,"focused transition uses both representations");Check(sol.DistantBodies[0].DetailedAlpha>0&&sol.DistantBodies[0].DistantAlpha>0&&sol.DistantBodies.Skip(1).All(body=>body.Regime==NativePlanetaryRenderRegime.DistantOnly&&body.DetailedAlpha==0&&body.DistantAlpha==1),"only focused body has detailed eligibility");
    Check(sol.Focus(camera,9),"move detail eligibility to Neptune");Check(sol.FocusedBody.BodyId==SolarSystemBodyIds.Neptune.Value&&sol.DistantBodies.Skip(1).All(body=>body.Regime==NativePlanetaryRenderRegime.DistantOnly&&body.DetailedAlpha==0),"old focus returns to distant batch");Check(sol.Focus(camera,3)&&sol.FocusedBody.Position==earth.Position,"Earth Neptune Earth focus identity");

    var moon=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Moon.Value);var neptune=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Neptune.Value);var sun=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Sun.Value);double Distance(PlanetRenderProxy left,PlanetRenderProxy right)=>Math.Sqrt((left.Position.Value-right.Position.Value).LengthSquared);var earthMoon=Distance(earth,moon);var evaluatedEarthMoon=Math.Sqrt((EvaluatedCenter(earth.BodyId)-EvaluatedCenter(moon.BodyId)).LengthSquared);var tolerance=Math.Max(1e-6,evaluatedEarthMoon*1e-15);Check(Math.Abs(earthMoon-evaluatedEarthMoon)<=tolerance,"Earth-Moon snapshot/evaluation separation agreement");var sunEarth=Distance(sun,earth);var earthNeptune=Distance(earth,neptune);Check(sunEarth==Math.Sqrt((EvaluatedCenter(sun.BodyId)-EvaluatedCenter(earth.BodyId)).LengthSquared)&&earthNeptune==Math.Sqrt((EvaluatedCenter(earth.BodyId)-EvaluatedCenter(neptune.BodyId)).LengthSquared),"Sun-Earth and Earth-Neptune evaluated relationships");
    Check(sol.Focus(camera,0),"Sun focus for physical-radius check");sol.Update(camera);var mercury=bodies[1];Check(sol.DistantBodies[1].Radius==(float)mercury.RadiusMetres&&sol.Presentation.Bodies[1].RadiusMetres==mercury.RadiusMetres,"screen-space marker does not inflate physical sphere radius");Check(frozen.SequenceEqual(sol.Presentation.Bodies.ToArray())&&frozenOrbit.SequenceEqual(sol.OrbitRootSamples.ToArray()),"focus and presentation aids do not mutate snapshot or trajectories");Check(sol.DistantBodies.Take(sol.DistantBodyCount).All(body=>body.Radius>0&&float.IsFinite(body.CenterX)&&float.IsFinite(body.CenterY)&&float.IsFinite(body.CenterZ)),"physical body records finite");

    Check(SolarOverlayLayout.LabelGlyphWidthNdc>0d&&SolarOverlayLayout.LabelGlyphHeightNdc>SolarOverlayLayout.LabelGlyphWidthNdc&&SolarOverlayLayout.CharacterStrideNdc>=SolarOverlayLayout.LabelGlyphWidthNdc,"professional label metrics use restrained proportional sans-serif cells");var labelDistanceA=earth.RadiusMetres*12d;var labelDistanceB=SolAnalyticalDefinition.AstronomicalUnitMetres*12d;var labelCameraA=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*labelDistanceA),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var labelCameraB=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*labelDistanceB),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);Check(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out var labelBoundsA,out _)&&SolarOverlayLayout.TryProjectLabel(earth,labelCameraB,true,out var labelBoundsB,out _)&&Math.Abs((labelBoundsA.MaxX-labelBoundsA.MinX)-(labelBoundsB.MaxX-labelBoundsB.MinX))<1e-15d&&Math.Abs((labelBoundsA.MaxY-labelBoundsA.MinY)-(labelBoundsB.MaxY-labelBoundsB.MinY))<1e-15d,"celestial labels retain stable apparent screen size across camera distance");
    var solarFrames=new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root,null,ReferenceFrameKind.Ecl,"solar-test-root"),CelestialFrameFactory.RootEcl())]);var solarResolver=new ReferenceFrameResolver(solarFrames);var zoomCases=new[]{("near Earth",earth,earth.RadiusMetres*10d),("Earth-Moon",earth,690_280_069.1073977d),("inner system",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*5d),("full system",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*45d),("beyond overview",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu)};foreach(var zoomCase in zoomCases){var zoomCamera=new CameraState(new FramePosition(root,zoomCase.Item2.Position.Value+Double3.UnitZ*zoomCase.Item3),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);Check(CameraRenderSnapshotBuilder.TryBuildInfiniteFar(zoomCamera,solarResolver,root,out var gpuCamera,out _,out _),$"{zoomCase.Item1} infinite-far camera build");Check(MatrixFinite(gpuCamera.ViewProjection),$"{zoomCase.Item1} view/projection finite");Check(SolarOverlayLayout.TryProjectLabel(zoomCase.Item2,zoomCamera,true,out var projectedBounds,out var projectedDepth)&&projectedBounds.IsFinite&&projectedDepth is >=0d and <1d,$"{zoomCase.Item1} focused marker/label projectable");var relative=zoomCase.Item2.Position.Value-zoomCamera.Position.Value;Check(((float)relative.X is var rx&&float.IsFinite(rx))&&((float)relative.Y is var ry&&float.IsFinite(ry))&&((float)relative.Z is var rz&&float.IsFinite(rz)),$"{zoomCase.Item1} camera-relative center finite");}
    var lastValidDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*45d*Math.Pow(1.1d,7d);var firstInvalidDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*45d*Math.Pow(1.1d,8d);var finiteLast=new CameraState(new FramePosition(root,sun.Position.Value+Double3.UnitZ*lastValidDistance),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var finiteFirst=new CameraState(new FramePosition(root,sun.Position.Value+Double3.UnitZ*firstInvalidDistance),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var finiteLastBuilt=CameraRenderSnapshotBuilder.TryBuild(finiteLast,solarResolver,root,out var finiteLastGpu,out _,out _);var finiteFirstBuilt=CameraRenderSnapshotBuilder.TryBuild(finiteFirst,solarResolver,root,out var finiteFirstGpu,out _,out _);Check(finiteLastBuilt&&finiteFirstBuilt,"finite-far boundary camera builds");var finiteLastDepth=ProjectedDepth(finiteLastGpu.ViewProjection,-lastValidDistance);var finiteFirstDepth=ProjectedDepth(finiteFirstGpu.ViewProjection,-firstInvalidDistance);Check(finiteLastDepth<=1d&&finiteFirstDepth>1d,"finite FP32 projection reproduces last-visible/first-clipped boundary");var infiniteFirstBuilt=CameraRenderSnapshotBuilder.TryBuildInfiniteFar(finiteFirst,solarResolver,root,out var infiniteFirstGpu,out _,out _);Check(infiniteFirstBuilt&&ProjectedDepth(infiniteFirstGpu.ViewProjection,-firstInvalidDistance)<1d,"infinite-far projection keeps former failing distance inside Vulkan depth");var reversedFiniteBuilt=CameraRenderSnapshotBuilder.TryBuildReversedZ(finiteLast,solarResolver,root,out var reversedFiniteGpu,out _,out _);var reversedInfiniteBuilt=CameraRenderSnapshotBuilder.TryBuildReversedInfiniteFar(finiteFirst,solarResolver,root,out var reversedInfiniteGpu,out _,out _);var reversedNearDepth=ProjectedDepth(reversedFiniteGpu.ViewProjection,-sol.Projection.NearClip);var reversedFarDepth=ProjectedDepth(reversedFiniteGpu.ViewProjection,-sol.Projection.FarClip);var reversedDistantDepth=ProjectedDepth(reversedInfiniteGpu.ViewProjection,-firstInvalidDistance);Check(reversedFiniteBuilt&&Math.Abs(reversedNearDepth-1d)<1e-6d&&Math.Abs(reversedFarDepth)<1e-6d,"finite reversed-Z projection maps near to one and far to zero without clip-space subtraction");Check(reversedInfiniteBuilt&&reversedDistantDepth>0d&&reversedDistantDepth<1d,"infinite reversed-Z projection retains positive depth at Solar scale");
    Check(sol.Focus(camera,0),"Sun focus for zoom round trip");sol.ResetPresentationCamera(camera);var roundTripDistance=sol.OrbitDistance;sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=5},out _,out _);sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-5},out _,out _);Check(Math.Abs(sol.OrbitDistance-roundTripDistance)<=roundTripDistance*1e-15d,"Solar zoom in/out round trip stable");sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-100},out _,out _);Check(sol.OrbitDistance==SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu&&sol.VisibleLabelCount>0&&sol.VisibleLabelIds[0]==SolarSystemBodyIds.Sun.Value,"maximum Solar overview retains focused presentation label");Console.WriteLine($"Solar finite-depth boundary: last={lastValidDistance/SolAnalyticalDefinition.AstronomicalUnitMetres:R} AU depth={finiteLastDepth:R}; first={firstInvalidDistance/SolAnalyticalDefinition.AstronomicalUnitMetres:R} AU depth={finiteFirstDepth:R}; infiniteDepth={ProjectedDepth(infiniteFirstGpu.ViewProjection,-firstInvalidDistance):R}");

    static bool MatrixFinite(in Float4x4 matrix)=>new[]{matrix.C0R0,matrix.C0R1,matrix.C0R2,matrix.C0R3,matrix.C1R0,matrix.C1R1,matrix.C1R2,matrix.C1R3,matrix.C2R0,matrix.C2R1,matrix.C2R2,matrix.C2R3,matrix.C3R0,matrix.C3R1,matrix.C3R2,matrix.C3R3}.All(float.IsFinite);
    static double ProjectedDepth(in Float4x4 matrix,double cameraZ){var z=(float)cameraZ;var clipZ=matrix.C2R2*z+matrix.C3R2;var clipW=matrix.C2R3*z+matrix.C3R3;return clipZ/clipW;}
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var separation,out var separationError)&&separation is not null,$"camera/body separation scene: {separationError}");var separationScene=separation!;var separationCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,separationScene.Projection,CameraMode.Free);Check(separationScene.Focus(separationCamera,3),"camera/body separation Earth focus");separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{PauseToggle=1},out _,out _);var fixedTime=separationScene.CurrentTime;var fixedSnapshot=separationScene.Presentation.Bodies.ToArray();var fixedEarth=separationScene.FocusedBody;var anchorDirection=new Double3(.31,.42,.851).Normalized();var anchorPage=EarthVirtualTexturePageContract.BodyFixedPageIdentity(anchorDirection,EarthSurfaceDatasetContract.MaximumLevel);Check(CelestialBodyFixedFrameEvaluator.TryTransformAnchor(new CelestialSurfaceAnchor(SolarSystemBodyIds.Earth,.25d,-1.1d,125d),fixedTime,fixedEarth.RadiusMetres,fixedEarth.Position.Value,out var anchorRoot),"Earth body-fixed anchor transform");for(var step=0;step<12;step++){separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{LookActive=1,MouseDeltaX=31-step,MouseDeltaY=step-7,MouseWheelDetents=step%2==0?1:-1},out _,out _);Check(separationScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),separationCamera,out separationError),$"paused camera manipulation {step}: {separationError}");}var afterEarth=separationScene.FocusedBody;Check(separationScene.IsPaused&&separationScene.CurrentTime==fixedTime&&fixedEarth.BodyFixedToRoot==afterEarth.BodyFixedToRoot&&fixedSnapshot.SequenceEqual(separationScene.Presentation.Bodies.ToArray())&&anchorPage==EarthVirtualTexturePageContract.BodyFixedPageIdentity(anchorDirection,EarthSurfaceDatasetContract.MaximumLevel)&&CelestialBodyFixedFrameEvaluator.TryTransformAnchor(new CelestialSurfaceAnchor(SolarSystemBodyIds.Earth,.25d,-1.1d,125d),fixedTime,fixedEarth.RadiusMetres,fixedEarth.Position.Value,out var anchorRootAfter)&&anchorRootAfter==anchorRoot,"paused drag/zoom preserves Earth orientation, anchor, SVT identity, time, and celestial state");Check(separationScene.Focus(separationCamera,5),"camera/body separation Mars focus");var fixedMars=separationScene.FocusedBody;for(var step=0;step<8;step++)separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{LookActive=1,MouseDeltaX=-22,MouseDeltaY=9,MouseWheelDetents=step%2==0?1:-1},out _,out _);Check(separationScene.CurrentTime==fixedTime&&separationScene.FocusedBody.BodyFixedToRoot==fixedMars.BodyFixedToRoot&&fixedSnapshot.SequenceEqual(separationScene.Presentation.Bodies.ToArray()),"paused Mars drag/zoom preserves physical orientation and celestial state");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var orientationProof,out var orientationError)&&orientationProof is not null,$"Earth handoff orientation proof scene: {orientationError}");var orientationScene=orientationProof!;var orientationCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,orientationScene.Projection,CameraMode.Free);Check(orientationScene.Focus(orientationCamera,NativePresentationFocus.Earth),"Earth handoff orientation focus");orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{PauseToggle=1},out _,out _);var orientationTime=orientationScene.CurrentTime;var orientationSnapshot=orientationScene.Presentation.Bodies.ToArray();var orientationEarth=orientationScene.FocusedBody;var geographicDirection=new Double3(.371d,.482d,.793d).Normalized();var geographicPage=EarthVirtualTexturePageContract.BodyFixedPageIdentity(geographicDirection,EarthSurfaceDatasetContract.MaximumLevel);var geographicRootDirection=orientationEarth.BodyFixedToRoot.Rotate(geographicDirection);var distantRecoveredDirection=orientationEarth.BodyFixedToRoot.Conjugate().Normalized().Rotate(geographicRootDirection);Check(Math.Sqrt((distantRecoveredDirection-geographicDirection).LengthSquared)<1e-12d&&orientationEarth.BodyFixedToRoot.Rotate(geographicDirection)==geographicRootDirection,"distant, regional, and eyeball paths share body-local direction semantics");
    AssertPausedEarthPath("near-field",PlanetaryRenderRegime.DetailedOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);AssertPausedEarthPath("handoff",PlanetaryRenderRegime.Transition);var outwardNotches=0;while(orientationScene.FocusedBlend.Regime!=PlanetaryRenderRegime.DistantOnly&&outwardNotches++<16)orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);Check(orientationScene.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly,"one-notch stepping reaches first distant-only state");AssertPausedEarthPath("just-outside-handoff",PlanetaryRenderRegime.DistantOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-4},out _,out _);AssertPausedEarthPath("far-distant",PlanetaryRenderRegime.DistantOnly);for(var crossing=0;crossing<4;crossing++){Check(orientationScene.Focus(orientationCamera,NativePresentationFocus.Earth),$"handoff return to near {crossing}");AssertPausedEarthPath($"repeat-near-{crossing}",PlanetaryRenderRegime.DetailedOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-8},out _,out _);Check(orientationScene.FocusedBlend.Regime is PlanetaryRenderRegime.Transition or PlanetaryRenderRegime.DistantOnly,$"handoff outward crossing {crossing}");AssertPausedEarthPath($"repeat-far-{crossing}",orientationScene.FocusedBlend.Regime);}
    void AssertPausedEarthPath(string path,PlanetaryRenderRegime expectedRegime){orientationScene.Update(orientationCamera);var beforeBody=orientationScene.FocusedBody;var beforeNative=orientationScene.FocusedPresentation(orientationCamera);var beforeDistant=orientationScene.DistantBodies[0];var beforePosition=orientationCamera.Position.Value;var beforeView=orientationCamera.Orientation;var beforeDistance=Math.Sqrt((beforePosition-beforeBody.Position.Value).LengthSquared);var beforeRootPoint=RootSurfacePoint(beforeBody,geographicDirection);var beforeDetailed=DetailedCameraRelativePoint(beforeBody,beforePosition,geographicDirection);var beforeFar=DistantCameraRelativePoint(beforeBody,beforePosition,geographicDirection);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{LookActive=1,MouseDeltaX=37,MouseDeltaY=-19},out _,out _);Check(orientationScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),orientationCamera,out orientationError),$"{path} paused host advance: {orientationError}");var afterBody=orientationScene.FocusedBody;var afterNative=orientationScene.FocusedPresentation(orientationCamera);var afterDistant=orientationScene.DistantBodies[0];var afterDistance=Math.Sqrt((orientationCamera.Position.Value-afterBody.Position.Value).LengthSquared);var afterRootPoint=RootSurfacePoint(afterBody,geographicDirection);var afterDetailed=DetailedCameraRelativePoint(afterBody,orientationCamera.Position.Value,geographicDirection);var afterFar=DistantCameraRelativePoint(afterBody,orientationCamera.Position.Value,geographicDirection);var lookDirection=(afterBody.Position.Value-orientationCamera.Position.Value).Normalized();var cameraForward=orientationCamera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();Check(orientationScene.FocusedBlend.Regime==expectedRegime&&orientationCamera.Position.Value!=beforePosition&&orientationCamera.Orientation!=beforeView&&Math.Abs(afterDistance-beforeDistance)<=Math.Max(1e-6d,beforeDistance*1e-12d)&&Double3.Dot(lookDirection,cameraForward)>.999999999999d,$"{path} drag orbits camera at fixed distance and retains body focus");Check(orientationScene.IsPaused&&orientationScene.CurrentTime==orientationTime&&beforeBody.Position==afterBody.Position&&beforeBody.BodyFixedToRoot==afterBody.BodyFixedToRoot&&orientationSnapshot.SequenceEqual(orientationScene.Presentation.Bodies.ToArray()),$"{path} body position, quaternion, time, and celestial snapshot invariant");Check(beforeRootPoint==afterRootPoint&&RootPointBits(beforeRootPoint)==RootPointBits(afterRootPoint),$"{path} representative physical root-space surface point is bit-identical across paused drag");Check(Math.Sqrt((beforeDetailed-beforeFar).LengthSquared)<1e-5d&&Math.Sqrt((afterDetailed-afterFar).LengthSquared)<1e-5d,$"{path} detailed and distant transform chains resolve one physical root-space orientation");Check(NativeOrientation(beforeNative)==NativeOrientation(afterNative)&&NativeOrientation(beforeDistant)==NativeOrientation(afterDistant)&&NativeOrientation(afterNative)==NativeOrientation(afterDistant),$"{path} focused and distant native paths consume one immutable quaternion");Check(geographicPage==EarthVirtualTexturePageContract.BodyFixedPageIdentity(geographicDirection,EarthSurfaceDatasetContract.MaximumLevel)&&orientationEarth.BodyFixedToRoot.Rotate(geographicDirection)==geographicRootDirection,$"{path} geographic anchor and SVT page identity invariant");}
    static (int X,int Y,int Z,int W) NativeOrientation(in NativePlanetaryPresentation value)=>(BitConverter.SingleToInt32Bits(value.BodyOrientationX),BitConverter.SingleToInt32Bits(value.BodyOrientationY),BitConverter.SingleToInt32Bits(value.BodyOrientationZ),BitConverter.SingleToInt32Bits(value.BodyOrientationW));
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var allBodyProof,out var allBodyError)&&allBodyProof is not null,$"all-body live-path orientation scene: {allBodyError}");var allBodyScene=allBodyProof!;var allBodyCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,allBodyScene.Projection,CameraMode.Free);allBodyScene.ApplyPresentationInput(allBodyCamera,new NativeInputState{PauseToggle=1},out _,out _);var allBodySnapshot=allBodyScene.Presentation.Bodies.ToArray();var allBodyTime=allBodyScene.CurrentTime;var regimeProofs=new[]{PlanetaryRenderRegime.DetailedOnly,PlanetaryRenderRegime.Transition,PlanetaryRenderRegime.DistantOnly};
    for(var focusValue=2;focusValue<=10;focusValue++)
    {
        var focus=(NativePresentationFocus)focusValue;
        foreach(var expectedRegime in regimeProofs)
        {
            Check(allBodyScene.Focus(allBodyCamera,focus),$"all-body focus {focus} {expectedRegime}");for(var step=0;allBodyScene.FocusedBlend.Regime!=expectedRegime&&step<64;step++){var wheel=expectedRegime==PlanetaryRenderRegime.DetailedOnly?1:expectedRegime==PlanetaryRenderRegime.DistantOnly?-1:allBodyScene.FocusedBlend.Regime==PlanetaryRenderRegime.DetailedOnly?-1:1;allBodyScene.ApplyPresentationInput(allBodyCamera,new NativeInputState{MouseWheelDetents=wheel},out _,out _);}var body=allBodyScene.FocusedBody;Check(allBodyScene.FocusedBlend.Regime==expectedRegime,$"{body.Label} exact live representation state {expectedRegime}");
            var beforePosition=allBodyCamera.Position.Value;var beforeView=allBodyCamera.Orientation;var beforeBody=allBodyScene.FocusedBody;var beforeDistance=Math.Sqrt((beforePosition-beforeBody.Position.Value).LengthSquared);var beforeFocused=allBodyScene.FocusedPresentation(allBodyCamera);var beforeDistant=allBodyScene.DistantBodies[0];var beforeLight=BodyLocalSolarDirection(beforeBody,beforeFocused,allBodyScene.SolarLighting(allBodyCamera));var proofDirection=new Double3(.231d,.707d,.668d).Normalized();var beforeRootPoint=RootSurfacePoint(beforeBody,proofDirection);var beforeDetailedPoint=DetailedCameraRelativePoint(beforeBody,beforePosition,proofDirection);var beforeDistantPoint=DistantCameraRelativePoint(beforeBody,beforePosition,proofDirection);
            allBodyScene.ApplyPresentationInput(allBodyCamera,new NativeInputState{LookActive=1,MouseDeltaX=37,MouseDeltaY=-19},out _,out _);Check(allBodyScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),allBodyCamera,out allBodyError),$"{body.Label} paused exact live-path advance: {allBodyError}");var afterBody=allBodyScene.FocusedBody;var afterFocused=allBodyScene.FocusedPresentation(allBodyCamera);var afterDistant=allBodyScene.DistantBodies[0];var afterLight=BodyLocalSolarDirection(afterBody,afterFocused,allBodyScene.SolarLighting(allBodyCamera));
            var orbitDistance=Math.Sqrt((allBodyCamera.Position.Value-afterBody.Position.Value).LengthSquared);var look=(afterBody.Position.Value-allBodyCamera.Position.Value).Normalized();var forward=allBodyCamera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();var afterRootPoint=RootSurfacePoint(afterBody,proofDirection);var afterDetailedPoint=DetailedCameraRelativePoint(afterBody,allBodyCamera.Position.Value,proofDirection);var afterDistantPoint=DistantCameraRelativePoint(afterBody,allBodyCamera.Position.Value,proofDirection);Check(allBodyCamera.Position.Value!=beforePosition&&allBodyCamera.Orientation!=beforeView&&Math.Abs(orbitDistance-beforeDistance)<=Math.Max(1e-6d,beforeDistance*1e-12d)&&Double3.Dot(look,forward)>.999999999999d&&allBodyScene.FocusedBlend.Regime==expectedRegime,$"{body.Label} {expectedRegime} drag orbits camera at fixed distance");Check(allBodyScene.CurrentTime==allBodyTime&&beforeBody.Position==afterBody.Position&&beforeBody.BodyFixedToRoot==afterBody.BodyFixedToRoot&&allBodySnapshot.SequenceEqual(allBodyScene.Presentation.Bodies.ToArray()),$"{body.Label} {expectedRegime} celestial orientation authority invariant");Check(beforeRootPoint==afterRootPoint&&RootPointBits(beforeRootPoint)==RootPointBits(afterRootPoint),$"{body.Label} {expectedRegime} representative root-space surface point bit identity");Check(Math.Sqrt((beforeDetailedPoint-beforeDistantPoint).LengthSquared)<1e-5d&&Math.Sqrt((afterDetailedPoint-afterDistantPoint).LengthSquared)<1e-5d,$"{body.Label} {expectedRegime} detailed/transition/distant physical surface orientation agreement");Check(NativeOrientation(beforeFocused)==NativeOrientation(afterFocused)&&NativeOrientation(beforeDistant)==NativeOrientation(afterDistant)&&NativeOrientation(afterFocused)==NativeOrientation(afterDistant),$"{body.Label} {expectedRegime} distant/detail quaternion identity");Check(Double3.Dot(beforeLight,afterLight)>.9999999d,$"{body.Label} {expectedRegime} body-local evaluated-Sun direction invariant within FP32 camera-relative transport");
        }
    }
    static Double3 BodyLocalSolarDirection(in PlanetRenderProxy body,in NativePlanetaryPresentation presentation,in NativeSolarLighting lighting)=>body.BodyFixedToRoot.Conjugate().Normalized().Rotate(new Double3(lighting.SourceCenterX-presentation.CenterX,lighting.SourceCenterY-presentation.CenterY,lighting.SourceCenterZ-presentation.CenterZ).Normalized()).Normalized();
    static Double3 RootSurfacePoint(in PlanetRenderProxy body,in Double3 bodyDirection)=>body.Position.Value+body.BodyFixedToRoot.Rotate(bodyDirection*body.RadiusMetres);
    static Double3 DistantCameraRelativePoint(in PlanetRenderProxy body,in Double3 cameraRoot,in Double3 bodyDirection)=>body.Position.Value-cameraRoot+body.BodyFixedToRoot.Rotate(bodyDirection*body.RadiusMetres);
    static Double3 DetailedCameraRelativePoint(in PlanetRenderProxy body,in Double3 cameraRoot,in Double3 bodyDirection)=>body.BodyFixedToRoot.Rotate(bodyDirection*body.RadiusMetres-body.BodyFixedToRoot.Conjugate().Normalized().Rotate(cameraRoot-body.Position.Value));
    static (long X,long Y,long Z) RootPointBits(in Double3 point)=>(BitConverter.DoubleToInt64Bits(point.X),BitConverter.DoubleToInt64Bits(point.Y),BitConverter.DoubleToInt64Bits(point.Z));
    var planetaryVertexPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","shaders","planetary.vert"));var planetaryVertexSource=File.ReadAllText(planetaryVertexPath);var distantVertexSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"distant_planet.vert"));var nativeRendererSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"..","NovaCoreNative.cpp"));Check(planetaryVertexSource.Contains("lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz",StringComparison.Ordinal)&&!planetaryVertexSource.Contains("lighting.sourceCenterExposure.xyz-p.centerRadius.xyz",StringComparison.Ordinal),"detailed shader derives body-local Sun direction from root-camera-relative presentation center only");Check(planetaryVertexSource.Contains("RotateQuaternion(localPosition,presentation.bodyOrientation)",StringComparison.Ordinal)&&distantVertexSource.Contains("presentation.centerRadius.xyz+RotateQuaternion(bodyLocalPosition,presentation.bodyOrientation)",StringComparison.Ordinal),"detailed and distant vertices apply only the immutable body quaternion before shared view/projection");Check(!planetaryVertexSource.Contains("rootOrbit",StringComparison.OrdinalIgnoreCase)&&!distantVertexSource.Contains("rootOrbit",StringComparison.OrdinalIgnoreCase)&&!planetaryVertexSource.Contains("cameraOrientation",StringComparison.OrdinalIgnoreCase)&&!distantVertexSource.Contains("cameraOrientation",StringComparison.OrdinalIgnoreCase),"planet model shaders do not consume camera-orbit orientation state");Check(nativeRendererSource.Contains("handoffDepth.depthWriteEnable=VK_FALSE",StringComparison.Ordinal)&&nativeRendererSource.Contains("const uint32_t firstUnfocused=handoff?1u:0u",StringComparison.Ordinal),"focused handoff sphere cannot invisibly write depth or fight detailed geometry");
    var labelVertexSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"solar_label.vert"));var labelFragmentSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"solar_label.frag"));var hudFragmentSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"solar_speed_hud.frag"));var sharedSansSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"solar_sans_sdf.glsl"));Check(labelVertexSource.Contains("gl_VertexIndex/6",StringComparison.Ordinal)&&!labelVertexSource.Contains("glyphMask",StringComparison.Ordinal)&&nativeRendererSource.Contains("vkCmdDraw(c,42,10",StringComparison.Ordinal),"professional labels draw one analytic SDF quad per character instead of pixel-art cell quads");Check(labelFragmentSource.Contains("solar_sans_sdf.glsl",StringComparison.Ordinal)&&hudFragmentSource.Contains("solar_sans_sdf.glsl",StringComparison.Ordinal),"celestial labels and simulation-speed HUD share the same renderer-owned sans-serif visual language");Check(sharedSansSource.Contains("vec2(glyphUv.x,1.0-glyphUv.y)",StringComparison.Ordinal),"shared sans renderer accounts for the positive Vulkan viewport and keeps labels/HUD upright");
    var warpFocuses=new[]{NativePresentationFocus.Earth,NativePresentationFocus.Mars,NativePresentationFocus.Jupiter,NativePresentationFocus.Saturn,NativePresentationFocus.Moon};var warpRates=new[]{new SimulationRate(1,1),new SimulationRate(30,1),new SimulationRate(120,1),new SimulationRate(600,1),new SimulationRate(14_400,1),new SimulationRate(7_776_000,1)};
    foreach(var warpFocus in warpFocuses)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var inertialProof,out var inertialError)&&inertialProof is not null,$"{warpFocus} inertial focus scene: {inertialError}");var inertialScene=inertialProof!;var inertialCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,inertialScene.Projection,CameraMode.Free);Check(inertialScene.Focus(inertialCamera,warpFocus),$"{warpFocus} inertial focus");
        foreach(var targetRate in warpRates)
        {
            var targetRateIndex=SimulationSpeedPresets.IndexOf(targetRate);while(inertialScene.SpeedPresetIndex<targetRateIndex)inertialScene.ApplyPresentationInput(inertialCamera,new NativeInputState{RateIncrease=1},out _,out _);Check(inertialScene.Rate==targetRate,$"{warpFocus} selects {targetRate.Numerator}x");var beforeBody=inertialScene.FocusedBody;var activeTarget=inertialScene.CurrentFocusTarget;var beforeTargetEvaluated=activeTarget.TryEvaluate(beforeBody,out var beforeTarget);Check(activeTarget.Kind==FocusTargetKind.BodyCenter&&beforeTargetEvaluated&&beforeTarget==beforeBody.Position,$"{warpFocus} uses explicit body-center target at {targetRate.Numerator}x");var beforeSun=inertialScene.Presentation.Bodies[0];var beforeOffset=inertialCamera.Position.Value-beforeTarget.Value;var beforeView=inertialCamera.Orientation;var beforeBodyFixedCamera=beforeBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(beforeOffset.Normalized());var beforeBodyFixedSun=beforeBody.BodyFixedToRoot.Conjugate().Normalized().Rotate((beforeSun.Position.Value-beforeBody.Position.Value).Normalized());Check(inertialScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),inertialCamera,out inertialError),$"{warpFocus} {targetRate.Numerator}x advance: {inertialError}");var afterBody=inertialScene.FocusedBody;var afterTargetEvaluated=activeTarget.TryEvaluate(afterBody,out var afterTarget);Check(afterTargetEvaluated&&afterTarget==afterBody.Position,$"{warpFocus} reevaluates target from current snapshot at {targetRate.Numerator}x");var afterSun=inertialScene.Presentation.Bodies[0];var afterOffset=inertialCamera.Position.Value-afterTarget.Value;var afterBodyFixedCamera=afterBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(afterOffset.Normalized());var afterBodyFixedSun=afterBody.BodyFixedToRoot.Conjugate().Normalized().Rotate((afterSun.Position.Value-afterBody.Position.Value).Normalized());var offsetError=Math.Sqrt((afterOffset-beforeOffset).LengthSquared);var offsetTolerance=Math.Max(.01d,Math.Sqrt(beforeOffset.LengthSquared)*1e-12d);var look=(afterTarget.Value-inertialCamera.Position.Value).Normalized();var forward=inertialCamera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();var longitudeMotion=Math.Sqrt((afterBodyFixedCamera-beforeBodyFixedCamera).LengthSquared);var lightMotion=Math.Sqrt((afterBodyFixedSun-beforeBodyFixedSun).LengthSquared);Check(afterBody.Position.Value!=beforeBody.Position.Value&&afterBody.BodyFixedToRoot!=beforeBody.BodyFixedToRoot&&offsetError<=offsetTolerance&&inertialCamera.Orientation==beforeView&&Double3.Dot(look,forward)>.999999999999d&&longitudeMotion>1e-12d&&lightMotion>1e-12d,$"{warpFocus} {targetRate.Numerator}x follows target translation with inertially fixed camera while body longitude and Sun-facing frame evolve");if(warpFocus==NativePresentationFocus.Earth&&(targetRate.Numerator==30||targetRate.Numerator==600||targetRate.Numerator==14_400||targetRate.Numerator==7_776_000))Console.WriteLine($"Earth focus authority {targetRate.Numerator}x: offsetError={offsetError:E3} m; orientationFixed={inertialCamera.Orientation==beforeView}; bodyLongitudeMotion={longitudeMotion:E3}; bodySunMotion={lightMotion:E3}");
        }
    }
    var surfaceWarpRates=new[]{new SimulationRate(1,1),new SimulationRate(30,1),new SimulationRate(600,1),new SimulationRate(14_400,1),new SimulationRate(7_776_000,1)};var fixedSurfaceDirection=new Double3(.31d,.42d,.851d).Normalized();var inertialMockOrientation=DoubleQuaternion.FromAxisAngle(new Double3(.2d,.8d,.4d).Normalized(),.41d);var mockOffset=new Double3(10_000d,-20_000d,30_000d);
    foreach(var surfaceWarpRate in surfaceWarpRates)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var surfaceWarpSceneValue,out var surfaceWarpError)&&surfaceWarpSceneValue is not null,$"surface-anchor {surfaceWarpRate.Numerator}x scene: {surfaceWarpError}");var surfaceWarpScene=surfaceWarpSceneValue!;var driverCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,surfaceWarpScene.Projection,CameraMode.Free);Check(surfaceWarpScene.Focus(driverCamera,NativePresentationFocus.Earth),$"surface-anchor Earth focus at {surfaceWarpRate.Numerator}x");var surfaceRateIndex=SimulationSpeedPresets.IndexOf(surfaceWarpRate);while(surfaceWarpScene.SpeedPresetIndex<surfaceRateIndex)surfaceWarpScene.ApplyPresentationInput(driverCamera,new NativeInputState{RateIncrease=1},out _,out _);var surfaceBodyBefore=surfaceWarpScene.FocusedBody;var surfaceAnchor=SurfaceAnchorFocus.AtDirection(surfaceBodyBefore.BodyId,fixedSurfaceDirection,surfaceBodyBefore.RadiusMetres,125d);var surfaceTarget=FocusTarget.AtSurface(surfaceAnchor);Check(surfaceTarget.TryEvaluate(surfaceBodyBefore,out var surfaceRootBefore),$"surface-anchor root before {surfaceWarpRate.Numerator}x");var mockCameraBefore=surfaceRootBefore.Value+mockOffset;var mockOrientationBefore=inertialMockOrientation;Check(surfaceWarpScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),driverCamera,out surfaceWarpError),$"surface-anchor advance {surfaceWarpRate.Numerator}x: {surfaceWarpError}");var surfaceBodyAfter=surfaceWarpScene.FocusedBody;Check(surfaceTarget.TryEvaluate(surfaceBodyAfter,out var surfaceRootAfter),$"surface-anchor root after {surfaceWarpRate.Numerator}x");var mockCameraAfter=surfaceRootAfter.Value+mockOffset;var distanceBefore=Math.Sqrt((mockCameraBefore-surfaceRootBefore.Value).LengthSquared);var distanceAfter=Math.Sqrt((mockCameraAfter-surfaceRootAfter.Value).LengthSquared);var distanceError=Math.Abs(distanceAfter-distanceBefore);Check(surfaceRootBefore.Value!=surfaceRootAfter.Value&&surfaceBodyBefore.BodyFixedToRoot!=surfaceBodyAfter.BodyFixedToRoot&&surfaceRootAfter.Value.IsFinite&&mockCameraAfter.IsFinite&&distanceError<1e-4d&&mockOrientationBefore==inertialMockOrientation&&mockOrientationBefore!=surfaceBodyAfter.BodyFixedToRoot,$"surface anchor evolves physically at {surfaceWarpRate.Numerator}x while mock camera follows position at stable distance without inheriting BodyFixedToRoot");Console.WriteLine($"surface-anchor focus {surfaceWarpRate.Numerator}x: targetMotion={Math.Sqrt((surfaceRootAfter.Value-surfaceRootBefore.Value).LengthSquared):R} m; distanceError={distanceError:E3} m; orientationInherited=false");
    }
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var orbitHierarchyProof,out var orbitHierarchyError)&&orbitHierarchyProof is not null,$"Moon orbit hierarchy proof scene: {orbitHierarchyError}");
    var hierarchyScene=orbitHierarchyProof!;
    var hierarchyCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,hierarchyScene.Projection,CameraMode.Free);
    var hierarchyRates=new[]{new SimulationRate(1,1),new SimulationRate(30,1),new SimulationRate(600,1),new SimulationRate(14_400,1),new SimulationRate(7_776_000,1)};
    foreach(var hierarchyRate in hierarchyRates)
    {
        var hierarchyRateIndex=SimulationSpeedPresets.IndexOf(hierarchyRate);
        while(hierarchyScene.SpeedPresetIndex<hierarchyRateIndex)hierarchyScene.ApplyPresentationInput(hierarchyCamera,new NativeInputState{RateIncrease=1},out _,out _);
        Check(hierarchyScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),hierarchyCamera,out orbitHierarchyError),$"Moon hierarchy {hierarchyRate.Numerator}x advance: {orbitHierarchyError}");
        var hierarchyEarth=hierarchyScene.Presentation.Bodies[3].Position.Value;
        var hierarchyMoon=hierarchyScene.Presentation.Bodies[4].Position.Value;
        var hierarchyMaximumRadius=Enumerable.Range(0,SolarSystemScene.OrbitSegmentCount).Max(sample=>Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+sample]-hierarchyEarth).LengthSquared));
        Check(hierarchyScene.OrbitRootSamples[moonPath]==hierarchyMoon&&hierarchyMaximumRadius<500_000_000d,$"Moon orbit stays Earth-relative and translates with evaluated Earth at {hierarchyRate.Numerator}x");
        Check(hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount]==hierarchyScene.OrbitRootSamples[moonPath],$"Moon orbit remains exactly closed at {hierarchyRate.Numerator}x");
        var hierarchyIncoming=(hierarchyScene.OrbitRootSamples[moonPath]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).Normalized();var hierarchyOutgoing=(hierarchyScene.OrbitRootSamples[moonPath+1]-hierarchyScene.OrbitRootSamples[moonPath]).Normalized();var hierarchySeamTurn=Math.Acos(Math.Clamp(Double3.Dot(hierarchyIncoming,hierarchyOutgoing),-1d,1d))*180d/Math.PI;var hierarchyPreviousLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-2]).LengthSquared);var hierarchySeamLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).LengthSquared);var hierarchyNextLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+1]-hierarchyScene.OrbitRootSamples[moonPath]).LengthSquared);var hierarchyMeanLength=(hierarchyPreviousLength+hierarchySeamLength+hierarchyNextLength)/3d;var hierarchySegmentDiscontinuity=Math.Max(Math.Abs(hierarchySeamLength-hierarchyPreviousLength),Math.Abs(hierarchyNextLength-hierarchySeamLength))/hierarchyMeanLength;Check(hierarchySeamTurn<4d&&hierarchySegmentDiscontinuity<.06d,$"Moon periodic seam remains visually continuous at {hierarchyRate.Numerator}x");Console.WriteLine($"Moon seam {hierarchyRate.Numerator}x: turn={hierarchySeamTurn:R}deg; relativeSegmentDiscontinuity={hierarchySegmentDiscontinuity:R}");
    }
    hierarchyScene.Update(hierarchyCamera);
    var moonOrbitUpdateAllocationBefore=GC.GetAllocatedBytesForCurrentThread();
    for(var update=0;update<10_000;update++)hierarchyScene.Update(hierarchyCamera);
    var moonOrbitUpdateAllocated=GC.GetAllocatedBytesForCurrentThread()-moonOrbitUpdateAllocationBefore;
    Check(moonOrbitUpdateAllocated==0,"warmed closed Moon orbit camera-relative updates allocate zero managed bytes");
    var zoomMinimum=earth.RadiusMetres*1.05d;var zoomMaximum=SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu;var farZoom=SolarCameraZoomPolicy.Apply(SolAnalyticalDefinition.AstronomicalUnitMetres,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var nearStart=earth.RadiusMetres*2d;var nearZoom=SolarCameraZoomPolicy.Apply(nearStart,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var reverseNearZoom=SolarCameraZoomPolicy.Apply(nearZoom,earth.RadiusMetres,zoomMinimum,zoomMaximum,-1);Check(Math.Abs((SolAnalyticalDefinition.AstronomicalUnitMetres-earth.RadiusMetres)/(farZoom-earth.RadiusMetres)-SolarCameraZoomPolicy.DistanceRatioPerDetent)<1e-12d&&Math.Abs((nearStart-earth.RadiusMetres)/(nearZoom-earth.RadiusMetres)-SolarCameraZoomPolicy.DistanceRatioPerDetent)<1e-12d,"wheel zoom applies one continuous logarithmic altitude ratio at astronomical and local distance");Check(SolAnalyticalDefinition.AstronomicalUnitMetres-farZoom>nearStart-nearZoom&&Math.Abs(reverseNearZoom-nearStart)<1e-7d,"wheel zoom supplies large astronomical travel, fine near-body control, and reversible detents");Check(sol.Focus(camera,3),"Earth focus for interaction");var interactionSnapshot=sol.Presentation;var interactionOrbit=sol.OrbitRootSamples.ToArray();var initialOrbitDistance=sol.OrbitDistance;var initialCamera=camera.Position.Value;var initialView=camera.Orientation;sol.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-30},out var rateChanged,out var pauseChanged);var orbitDistanceTolerance=Math.Max(1e-6d,initialOrbitDistance*1e-12d);var interactionLook=(sol.FocusedBody.Position.Value-camera.Position.Value).Normalized();var interactionForward=camera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();Check(!rateChanged&&!pauseChanged&&camera.Position.Value!=initialCamera&&camera.Orientation!=initialView&&Math.Abs(Math.Sqrt((camera.Position.Value-sol.FocusedBody.Position.Value).LengthSquared)-initialOrbitDistance)<orbitDistanceTolerance&&Double3.Dot(interactionLook,interactionForward)>.999999999999d,"Solar mouse drag orbits current focus without changing distance");var draggedPosition=camera.Position.Value;var draggedView=camera.Orientation;sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);Check(sol.OrbitDistance<initialOrbitDistance&&camera.Position.Value!=draggedPosition&&camera.Orientation==draggedView,"Solar wheel changes distance only and preserves root-inertial orbital direction");
    sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(1,10)&&sol.SpeedHudVisible&&sol.SpeedHudLabel=="Simulation Speed: 0.1x (Slow Motion)"&&sol.SolarLighting(camera).SpeedHud!=0,"Solar reaches 0.1x and immediately publishes its HUD");sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out rateChanged,out _);Check(!rateChanged&&sol.Rate==new SimulationRate(1,10),"Solar 0.1x lower clamp");for(var step=0;step<4;step++)sol.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(10,1)&&sol.SpeedHudLabel=="Simulation Speed: 10x","Solar ordered rate steps reach 10x");for(var step=0;step<15;step++)sol.ApplyPresentationInput(camera,new NativeInputState{DeltaSeconds=.1f},out _,out _);Check(sol.SpeedHudVisible&&sol.SpeedHudAlpha is >0f and <1f,"Solar speed HUD uses a readable wall-time hold and bounded fade");for(var step=0;step<5;step++)sol.ApplyPresentationInput(camera,new NativeInputState{DeltaSeconds=.1f},out _,out _);Check(!sol.SpeedHudVisible&&sol.SpeedHudAlpha==0f&&sol.SolarLighting(camera).SpeedHud==0,"Solar speed HUD disappears after two wall-time seconds");sol.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(30,1)&&sol.SpeedHudVisible,"changing speed resets the HUD timer");sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out _,out _);Check(sol.Rate==new SimulationRate(10,1),"Solar restores 10x for deterministic replay");sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out pauseChanged);var pausedTime=sol.CurrentTime;var pausedAdvance=sol.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out var pausedError);Check(pauseChanged&&sol.IsPaused&&pausedAdvance&&sol.CurrentTime==pausedTime&&ReferenceEquals(interactionSnapshot,sol.Presentation),$"Solar pause freezes authoritative evaluation: {pausedError}");sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);Check(!sol.IsPaused,"Solar resume");
    var oldFocusedCenter=sol.FocusedBody.Position.Value;var oldOrientation=sol.FocusedBody.BodyFixedToRoot;var oldCameraOffset=camera.Position.Value-oldFocusedCenter;var oldCameraOrientation=camera.Orientation;var oldBodyFixedCameraDirection=oldOrientation.Conjugate().Normalized().Rotate(oldCameraOffset.Normalized());Check(sol.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out var advanceError),$"Solar dynamic publication: {advanceError}");Check(sol.CurrentTime==SimulationInstant.FromWholeSeconds(10)&&!ReferenceEquals(interactionSnapshot,sol.Presentation)&&sol.FocusedBody.Position.Value!=oldFocusedCenter&&sol.FocusedBody.BodyFixedToRoot!=oldOrientation,"Solar time advancement republishes evaluated positions and exact-epoch body orientations");var newCameraOffset=camera.Position.Value-sol.FocusedBody.Position.Value;var newBodyFixedCameraDirection=sol.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(newCameraOffset.Normalized());Check(Math.Abs(Math.Sqrt(newCameraOffset.LengthSquared)-sol.OrbitDistance)<1e-4&&Math.Sqrt((newCameraOffset-oldCameraOffset).LengthSquared)<1e-4&&camera.Orientation==oldCameraOrientation&&newBodyFixedCameraDirection!=oldBodyFixedCameraDirection,"focused camera follows translation with inertially stable offset while body longitude rotates underneath");Check(interactionSnapshot.Bodies[3].Position.Value==oldFocusedCenter,"prior immutable presentation remains unchanged");Check(!interactionOrbit.SequenceEqual(sol.OrbitRootSamples.ToArray())&&Enumerable.Range(0,SolarSystemScene.OrbitPathCount).All(path=>sol.OrbitRootSamples[path*SolarSystemScene.OrbitSampleCount]==sol.Presentation.Bodies[path+1].Position.Value),"time controls resample every orbit path from current authoritative state");
    var dynamicResult=CelestialSystemEvaluator.TryEvaluateSystem(system,sol.CurrentTime,evaluations,roots,staging,stagingRoots);Check(dynamicResult.Succeeded,"independent dynamic Sol evaluation");for(var index=0;index<sol.Presentation.Count;index++)Check(sol.Presentation.Bodies[index].Position.Value==roots[Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),candidate=>system.GetNodeInTraversalOrder(candidate).Id.Value==sol.Presentation.Bodies[index].BodyId)].Translation,$"dynamic body {index} matches authoritative evaluator");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var replay,out var replayError)&&replay is not null,$"Solar replay creation: {replayError}");var replayCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,camera.Projection,CameraMode.Free);Check(replay!.Focus(replayCamera,3),"Solar replay Earth focus");replay.ApplyPresentationInput(replayCamera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-30},out _,out _);replay.ApplyPresentationInput(replayCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);for(var step=0;step<3;step++)replay.ApplyPresentationInput(replayCamera,new NativeInputState{RateIncrease=1},out _,out _);Check(replay.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),replayCamera,out replayError),$"Solar replay advance: {replayError}");Check(replay.CurrentTime==sol.CurrentTime&&replay.Presentation.Bodies.SequenceEqual(sol.Presentation.Bodies)&&replayCamera.Position.Value==camera.Position.Value&&replay.DistantBodies.SequenceEqual(sol.DistantBodies)&&replay.OrbitRootSamples.SequenceEqual(sol.OrbitRootSamples),"identical Solar controls produce identical time, snapshot, camera, orbit paths, and presentation batch");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var warp,out var warpError)&&warp is not null,$"Solar high-warp creation: {warpError}");var warpCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,warp!.Projection,CameraMode.Free);Check(warp.Focus(warpCamera,4)&&warp.FocusedBody.BodyId==SolarSystemBodyIds.Moon.Value,"Solar high-warp Moon focus");var warpOrbit=warp.OrbitRootSamples.ToArray();for(var step=warp.SpeedPresetIndex;step<SimulationSpeedPresets.Count-1;step++)warp.ApplyPresentationInput(warpCamera,new NativeInputState{RateIncrease=1},out _,out _);Check(warp.Rate==new SimulationRate(7_776_000,1),"Solar reaches 7,776,000x maximum preset");for(var step=0;step<64;step++)Check(warp.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),warpCamera,out warpError),$"Solar repeated 7,776,000x advancement {step}: {warpError}");Check(warp.CurrentTime==SimulationInstant.FromWholeSeconds(497_664_000)&&warp.Presentation.Bodies.ToArray().All(body=>body.Position.Value.IsFinite)&&warp.DistantBodies.Take(warp.DistantBodyCount).All(body=>float.IsFinite(body.CenterX)&&float.IsFinite(body.CenterY)&&float.IsFinite(body.CenterZ)),"Solar sustained 7,776,000x states and transport finite");var epochStaticMoonMismatch=Math.Sqrt((warpOrbit[moonPath]-warp.FocusedBody.Position.Value).LengthSquared);Check(Math.Abs(Math.Sqrt((warpCamera.Position.Value-warp.FocusedBody.Position.Value).LengthSquared)-warp.OrbitDistance)<1e-3&&!warpOrbit.SequenceEqual(warp.OrbitRootSamples.ToArray())&&warp.OrbitRootSamples[moonPath]==warp.FocusedBody.Position.Value&&epochStaticMoonMismatch>1e9d&&warp.OrbitVertices.All(vertex=>float.IsFinite(vertex.X)&&float.IsFinite(vertex.Y)&&float.IsFinite(vertex.Z)),"Solar maximum-warp Moon focus follows corrected body and orbit authority refreshes from current time");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var presetScene,out var presetError)&&presetScene is not null,$"Solar preset-input scene: {presetError}");var presetCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,presetScene!.Projection,CameraMode.Free);presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateDecrease=1},out _,out _);for(var index=0;index<SimulationSpeedPresets.Count;index++){var expected=SimulationSpeedPresets.Get(index);Check(presetScene.SpeedPresetIndex==index&&presetScene.Rate==expected.Rate&&presetScene.SpeedHudLabel==expected.Label,$"Solar exact input preset {index}");if(index<SimulationSpeedPresets.Count-1)presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1},out _,out _);}presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(!rateChanged&&presetScene.SpeedPresetIndex==SimulationSpeedPresets.Count-1,"Solar maximum preset upper clamp");presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1,RateDecrease=1},out rateChanged,out _);Check(!rateChanged&&presetScene.SpeedPresetIndex==SimulationSpeedPresets.Count-1,"simultaneous rate inputs do not skip presets");presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{DeltaSeconds=.001f},out _,out _);var hudAllocationBefore=GC.GetAllocatedBytesForCurrentThread();for(var frame=0;frame<10_000;frame++)presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{DeltaSeconds=.001f},out _,out _);var hudAllocated=GC.GetAllocatedBytesForCurrentThread()-hudAllocationBefore;Check(hudAllocated==0,"warmed simulation-speed HUD timer allocates zero bytes");
    Check(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out _,out _),"warm professional label layout");var labelAllocationBefore=GC.GetAllocatedBytesForCurrentThread();var labelStart=System.Diagnostics.Stopwatch.GetTimestamp();var labelProjectionCount=0;for(var frame=0;frame<100_000;frame++)if(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out _,out _))labelProjectionCount++;var labelTicks=System.Diagnostics.Stopwatch.GetTimestamp()-labelStart;var labelAllocated=GC.GetAllocatedBytesForCurrentThread()-labelAllocationBefore;var labelNanoseconds=labelTicks*1_000_000_000d/System.Diagnostics.Stopwatch.Frequency/100_000d;var zoomAccumulator=nearStart;SolarCameraZoomPolicy.Apply(zoomAccumulator,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var zoomAllocationBefore=GC.GetAllocatedBytesForCurrentThread();var zoomStart=System.Diagnostics.Stopwatch.GetTimestamp();for(var frame=0;frame<100_000;frame++)zoomAccumulator=SolarCameraZoomPolicy.Apply(zoomAccumulator,earth.RadiusMetres,zoomMinimum,zoomMaximum,(frame&1)==0?1:-1);var zoomTicks=System.Diagnostics.Stopwatch.GetTimestamp()-zoomStart;var zoomAllocated=GC.GetAllocatedBytesForCurrentThread()-zoomAllocationBefore;var zoomNanoseconds=zoomTicks*1_000_000_000d/System.Diagnostics.Stopwatch.Frequency/100_000d;Check(labelProjectionCount==100_000&&labelAllocated==0&&zoomAllocated==0&&double.IsFinite(zoomAccumulator),"professional label layout and logarithmic camera policy allocate zero managed bytes after warmup");
    var warpEarth=warp.Presentation.Bodies[3];var warpSurfaceAnchor=new CelestialSurfaceAnchor(SolarSystemBodyIds.Earth,.25d,-1.1d,125d);var warpBodyLocal=warpSurfaceAnchor.BodyFixedPosition(warpEarth.RadiusMetres);var warpPageBefore=EarthVirtualTexturePageContract.BodyFixedPageIdentity(warpBodyLocal.Normalized(),EarthSurfaceDatasetContract.MaximumLevel);Check(CelestialBodyFixedFrameEvaluator.TryTransformAnchor(warpSurfaceAnchor,warp.CurrentTime,warpEarth.RadiusMetres,warpEarth.Position.Value,out var warpAnchorRoot)&&warpAnchorRoot==warpEarth.Position.Value+warpEarth.BodyFixedToRoot.Rotate(warpBodyLocal)&&warpPageBefore==EarthVirtualTexturePageContract.BodyFixedPageIdentity(warpBodyLocal.Normalized(),EarthSurfaceDatasetContract.MaximumLevel),"maximum warp preserves Earth body-fixed anchor and SVT geographic identity");
    Console.WriteLine($"Earth-Moon evaluated separation: {earthMoon:R} m");Console.WriteLine($"Sun-Earth evaluated separation: {sunEarth:R} m; Earth-Neptune: {earthNeptune:R} m");Console.WriteLine($"Solar interaction proof: time={sol.CurrentTime.Ticks}; rate={sol.Rate.Numerator}:{sol.Rate.Denominator}; focus={sol.FocusedBody.Label}; distance={sol.OrbitDistance:R} m; corrected_epoch_static_mismatch={epochStaticMoonMismatch:R} m; HUD allocation={hudAllocated} bytes");Console.WriteLine($"11B label/camera cost: label={labelNanoseconds:F2} ns/update, zoom={zoomNanoseconds:F2} ns/update, allocations={labelAllocated+zoomAllocated} bytes");
}

static void EarthPlanetarySceneTest()
{
    var root=new ReferenceFrameId(1);Check(EarthPlanetaryScene.TryCreate(root,out var scene,out var error)&&scene is not null,$"Earth planetary scene: {error}");var earthScene=scene!;
    PlanetRenderProxy publishedEarth=default;var hasEarth=earthScene.Presentation.TryGetBody(SolarSystemBodyIds.Earth.Value,out publishedEarth);Check(earthScene.Presentation.Count==1&&hasEarth,"Earth snapshot publication");var earth=publishedEarth;Check(earth==earthScene.Earth,"Earth proxy identity");
    Check(SolAnalyticalDefinition.Instance.TryGetBody(SolarSystemBodyIds.Earth,out var catalogEarth)&&earth.RadiusMetres==catalogEarth.PhysicalProperties.MeanRadius&&earth.RadiusMetres==6_371_008.8d,"Earth catalog radius");
    var distanceFromSun=Math.Sqrt(earth.Position.Value.LengthSquared);Check(distanceFromSun>.9d*SolAnalyticalDefinition.AstronomicalUnitMetres&&distanceFromSun<1.1d*SolAnalyticalDefinition.AstronomicalUnitMetres,"evaluated SolAnalytical Earth position");
    Check(earth.Color==EarthPlanetaryScene.EarthColor&&earth.Label=="Earth"&&earth.Visible,"Earth presentation properties");var materialCamera=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*earth.RadiusMetres*20d),DoubleQuaternion.Identity,earthScene.Projection,CameraMode.Free);var earthMaterial=earthScene.NativePresentation(materialCamera);Check(earthMaterial.BodyIdLow==6&&earthMaterial.AlbedoSource==(uint)PlanetAlbedoSource.EarthAuthoritative&&earthMaterial.RingAssociation==0,"Earth scene uses the shared generic material transport");var earthLighting=earthScene.SolarLighting(materialCamera);Check(earthLighting.Enabled==1&&earthLighting.SourceCenterZ<0&&earthLighting.AmbientFloor is >0 and <.1f,"Earth lighting derives from evaluated Sun and camera-relative transport");
    Check(earthScene.Patches.Length==EarthPlanetaryScene.MaximumPatchCapacity&&EarthPlanetaryScene.RegionalMaximumLod==12&&PlanetaryEyeballTopology.VertexCount==32_769,"Earth retains bounded regional storage while V2 owns a fixed near-field workload");
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,earthScene.Projection,CameraMode.Free);var before=earthScene.Presentation.Bodies.ToArray();Check(earthScene.TryFocus(camera),"Earth focus");var focused=camera.Position.Value;var focusedDistance=Math.Sqrt((focused-earth.Position.Value).LengthSquared);Check(Math.Abs(focusedDistance-earthScene.OrbitDistance)<=earth.RadiusMetres*1e-8d,"Earth focus distance");
    Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DistantOnly&&earthScene.ActivePatchCount==0&&earthScene.Representation==PlanetaryRepresentation.FarFieldBody&&earthScene.DistantDrawCount==1&&earthScene.RepresentationBlend is{DistantAlpha:1,DetailedAlpha:0},"far Earth uses only the distant body renderer");
    SetDistance(15d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.Transition&&earthScene.ActivePatchCount>0&&earthScene.DistantDrawCount==1&&earthScene.RepresentationBlend.DistantAlpha>0&&earthScene.RepresentationBlend.DetailedAlpha>0,"transition renders distant and detailed representations");
    SetDistance(10d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DetailedOnly&&earthScene.ActivePatchCount>6&&earthScene.MaximumActiveLod>0&&earthScene.DistantDrawCount==0,"closer Earth uses detailed patches only");
    SetDistance(2d);Console.WriteLine($"Earth adaptive LOD: patches={earthScene.ActivePatchCount}; min={earthScene.MinimumActiveLod}; max={earthScene.MaximumActiveLod}; refined={earthScene.RefinementCount}; balanced={earthScene.BalancedRefinementCount}; culled={earthScene.CulledPatchCount}");Check(earthScene.ActivePatchCount>6&&earthScene.MaximumActiveLod>0,"near Earth refines the visible frustum");var closeLeaves=earthScene.ActiveLeaves.ToArray();var closePatches=earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount).ToArray();var closeHash=Hash(closePatches);Check(closeLeaves.Distinct().Count()==closeLeaves.Length,"active patch IDs unique");Check(!HasAncestorOverlap(closeLeaves),"no parent child overlap");Check(IsTraversalOrdered(closeLeaves),"deterministic child traversal order");var axialIndices=closeLeaves.Select((patch,index)=>(patch,index)).Where(item=>item.patch.Face==CubeSphereFace.PositiveZ&&Contains(item.patch,.5d,.5d)).ToArray();Check(axialIndices.Length>0&&axialIndices.Any(item=>closePatches[item.index].StitchMask==0),"near selection retains unstitched camera-axis surface coverage");
    var closest=closeLeaves.MinBy(patch=>PatchDistance(patch));var farthest=closeLeaves.MaxBy(patch=>PatchDistance(patch));Check(closest.Level>=farthest.Level,"nearby patch is never coarser than the farthest visible patch");var active=closeLeaves.ToHashSet();var edges=new[]{PlanetaryPatchEdge.NegativeU,PlanetaryPatchEdge.PositiveU,PlanetaryPatchEdge.NegativeV,PlanetaryPatchEdge.PositiveV};Check(closeLeaves.All(patch=>edges.All(edge=>PlanetaryRepresentationSelector.FindCoveringNeighbor(patch,edge,active) is not { } neighbor||patch.Level-neighbor.Level<=1)),"edge neighbor level difference at most one");Check(earthScene.MinimumActiveLod<earthScene.MaximumActiveLod?closePatches.Any(patch=>patch.StitchMask!=0):closePatches.All(patch=>patch.StitchMask==0),"stitch metadata matches mixed or uniform visible LOD");for(var index=0;index<closeLeaves.Length;index++){uint expectedMask=0;foreach(var edge in edges)if(PlanetaryRepresentationSelector.FindCoveringNeighbor(closeLeaves[index],edge,active) is { } neighbor&&neighbor.Level+1==closeLeaves[index].Level)expectedMask|=(uint)edge;Check(closePatches[index].StitchMask==expectedMask,"deterministic stitch metadata");}
    var closeRefined=earthScene.RefinementCount;var closeBalanced=earthScene.BalancedRefinementCount;var closeCulled=earthScene.CulledPatchCount;earthScene.UpdatePatches(camera);Check(closeLeaves.SequenceEqual(earthScene.ActiveLeaves.ToArray())&&closeHash==Hash(earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount))&&closeRefined==earthScene.RefinementCount&&closeBalanced==earthScene.BalancedRefinementCount&&closeCulled==earthScene.CulledPatchCount,"repeated camera state has deterministic leaves, balancing, metadata, and batch");
    SetAltitude(3_000_000d);var regionalLevel=earthScene.MaximumActiveLod;var regionalCount=earthScene.ActivePatchCount;Check(regionalCount>0&&regionalLevel<=EarthPlanetaryScene.RegionalMaximumLod&&!earthScene.EyeballComputeRequested,"3000 km remains on bounded regional coverage");
    SetAltitude(1_500_000d);var transitionEye=earthScene.EyeballConstants(camera);Check(Math.Abs(earthScene.EyeballWeight-.5f)<1e-5f&&earthScene.ActivePatchCount>0&&transitionEye.Enabled==1&&Math.Abs(transitionEye.RegionalAlpha-.5f)<1e-5f,"2000-to-1000 km handoff overlaps regional and eyeball paths deterministically");
    var proofAltitudes=new[]{1_000_000d,100_000d,10_000d,1_000d,100d,10d,EarthPlanetaryScene.MinimumTerrainClearanceMetres};var proofInputs=new NativePlanetaryEyeball[proofAltitudes.Length];for(var index=0;index<proofAltitudes.Length;index++){SetAltitude(proofAltitudes[index]);proofInputs[index]=earthScene.EyeballConstants(camera);Check(earthScene.ActivePatchCount==0&&!earthScene.DetailedComputeRequested&&earthScene.EyeballComputeRequested&&earthScene.EyeballWeight==1f,"near field retires regional selection");}
    Check(proofInputs.All(eye=>eye.Enabled==1&&eye.VertexCount==PlanetaryEyeballTopology.VertexCount&&eye.IndexCount==PlanetaryEyeballTopology.IndexCount&&eye.RadialRingCount==PlanetaryEyeballTopology.RadialRingCount&&eye.AzimuthSegmentCount==PlanetaryEyeballTopology.AzimuthSegmentCount&&eye.RegionalAlpha==0f),"1000 km through 10 m uses one fixed topology and one indirect workload");
    Console.WriteLine($"Earth V2 descent proof: regional 3000km={regionalCount}/L{regionalLevel}; fixed 1000km..10m={PlanetaryEyeballTopology.VertexCount} vertices/{PlanetaryEyeballTopology.IndexCount} indices/1 draw");
    SetDistance(20d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DistantOnly&&earthScene.ActivePatchCount==0&&earthScene.DistantDrawCount==1,"receding restores distant-only rendering");
    earthScene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=100,MouseDeltaY=-50,MouseWheelDetents=1});Check(camera.Position.Value!=focused&&Math.Abs(Math.Sqrt((camera.Position.Value-earth.Position.Value).LengthSquared)-earthScene.OrbitDistance)<=earth.RadiusMetres*1e-8d,"Earth camera orbit");
    Check(before.SequenceEqual(earthScene.Presentation.Bodies.ToArray()),"Earth focus and orbit do not mutate presentation snapshot");var relative=CubeSphereProjection.CameraRelativeCenter(earth,new UniversePosition(camera.Position.Value,root));var nativePresentation=earthScene.NativePresentation(camera);Check(nativePresentation.CenterX==(float)relative.X&&nativePresentation.CenterY==(float)relative.Y&&nativePresentation.CenterZ==(float)relative.Z&&nativePresentation.Radius==(float)earth.RadiusMetres,"distant and detailed paths share camera-relative center and authoritative radius");Check(earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount).ToArray().All(patch=>patch.CenterX==(float)relative.X&&patch.CenterY==(float)relative.Y&&patch.CenterZ==(float)relative.Z&&patch.Radius==(float)earth.RadiusMetres),"Earth camera-relative patch batch");
    earthScene.ResetPresentationCamera(camera);var resetRadial=(camera.Position.Value-earth.Position.Value).Normalized();var resetBodyRadial=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(resetRadial);var resetLighting=earthScene.SolarLighting(camera);var evaluatedSunDirection=new Double3(resetLighting.SourceCenterX,resetLighting.SourceCenterY,resetLighting.SourceCenterZ).Normalized();Check(Double3.Dot(resetRadial,evaluatedSunDirection)>.70d&&Math.Abs(resetBodyRadial.Y)<.75d&&Math.Abs(Math.Sqrt((camera.Position.Value-earth.Position.Value).LengthSquared)-earth.RadiusMetres*EarthPlanetaryScene.InitialOrbitDistanceRadii)<=earth.RadiusMetres*1e-8d,"Earth focus reset uses evaluated-Sun temperate body-fixed day-side presentation without changing celestial truth");
    Check(EarthPlanetaryScene.TryCreate(root,NativePlanetaryMode.GpuProduction,128,out var gpuScene,out var gpuError)&&gpuScene is not null,$"GPU Earth scene: {gpuError}");
    var gpuEarth=gpuScene!;var gpuCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,gpuEarth.Projection,CameraMode.Free);Check(gpuEarth.TryFocus(gpuCamera),"GPU Earth focus");gpuEarth.UpdatePatches(gpuCamera);
    var gpuConstants=gpuEarth.GpuConstants(gpuCamera);var gpuCameraBody=new Double3((double)gpuConstants.CameraBodyHighX+gpuConstants.CameraBodyLowX,(double)gpuConstants.CameraBodyHighY+gpuConstants.CameraBodyLowY,(double)gpuConstants.CameraBodyHighZ+gpuConstants.CameraBodyLowZ);var gpuRadius=(double)gpuConstants.RadiusHigh+gpuConstants.RadiusLow;
    Check(gpuEarth.Mode==NativePlanetaryMode.GpuProduction&&gpuEarth.ActivePatchCount==0&&!gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==1,"distant GPU production suppresses compute and emits one whole-body draw");
    var gpuLight=gpuEarth.SolarLighting(gpuCamera);var gpuSunDirection=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(new Double3(gpuLight.SourceCenterX,gpuLight.SourceCenterY,gpuLight.SourceCenterZ)).Normalized();Check(Math.Abs(Math.Sqrt(gpuCameraBody.LengthSquared)-earth.RadiusMetres*EarthPlanetaryScene.InitialOrbitDistanceRadii)<1e-5&&Double3.Dot(gpuCameraBody.Normalized(),gpuSunDirection)>.70d&&Math.Abs(gpuCameraBody.Normalized().Y)<.75d&&Math.Abs(gpuRadius-earth.RadiusMetres)<1e-6&&gpuConstants.RefinementThreshold==(float)EarthPlanetaryScene.RegionalLodConfiguration.MaximumProjectedPatchSpan&&gpuConstants.MaximumLevel==EarthPlanetaryScene.RegionalMaximumLod&&gpuConstants.OutputCapacity==128&&gpuConstants.TerrainVersion==EarthPlanetaryScene.Terrain.Version,"GPU production high/low precision, evaluated-Sun temperate focus, and bounded regional constants");
    SetGpuDistance(15d);Check(gpuEarth.RepresentationBlend.Regime==PlanetaryRenderRegime.Transition&&gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==1,"GPU transition enables compute and both renderers");
    var gpuPresentation=gpuEarth.NativePresentation(gpuCamera);gpuConstants=gpuEarth.GpuConstants(gpuCamera);gpuCameraBody=new((double)gpuConstants.CameraBodyHighX+gpuConstants.CameraBodyLowX,(double)gpuConstants.CameraBodyHighY+gpuConstants.CameraBodyLowY,(double)gpuConstants.CameraBodyHighZ+gpuConstants.CameraBodyLowZ);gpuRadius=(double)gpuConstants.RadiusHigh+gpuConstants.RadiusLow;var expectedRootCenter=earth.BodyFixedToRoot.Rotate(-gpuCameraBody);var transportedCenter=new Double3(gpuPresentation.CenterX,gpuPresentation.CenterY,gpuPresentation.CenterZ);var transportedOrientation=new DoubleQuaternion(gpuPresentation.BodyOrientationX,gpuPresentation.BodyOrientationY,gpuPresentation.BodyOrientationZ,gpuPresentation.BodyOrientationW);var orientationDot=transportedOrientation.X*earth.BodyFixedToRoot.X+transportedOrientation.Y*earth.BodyFixedToRoot.Y+transportedOrientation.Z*earth.BodyFixedToRoot.Z+transportedOrientation.W*earth.BodyFixedToRoot.W;Check(Math.Sqrt((transportedCenter-expectedRootCenter).LengthSquared)<=32d&&gpuPresentation.Radius==(float)gpuRadius&&gpuPresentation.DetailedAlpha==gpuEarth.RepresentationBlend.DetailedAlpha&&Math.Abs(orientationDot)>.999999d,"native distant/detail inputs share body-fixed orientation, root-relative center, radius, and blend");
    SetGpuDistance(10d);Check(gpuEarth.RepresentationBlend.Regime==PlanetaryRenderRegime.DetailedOnly&&gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==0,"GPU detailed-only suppresses distant draw");var roundTripTruth=gpuEarth.Presentation.Bodies.ToArray();for(var step=0;step<128&&gpuEarth.CameraPresentationMode!=PlanetaryCameraPresentationMode.SurfaceLocal;step++)gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MouseWheelDetents=1});Check(gpuEarth.CameraPresentationMode==PlanetaryCameraPresentationMode.SurfaceLocal&&gpuEarth.SurfaceFocus is { IsValid:true },"orbital descent enters reusable SurfaceLocal camera mode");var surfaceAnchor=gpuEarth.SurfaceFocus!.Value.TangentFrame.Direction;var surfaceOrientation=gpuCamera.Orientation;gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-40});var cameraBodyFixed=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(gpuCamera.Position.Value-earth.Position.Value).Normalized();Check(Double3.Dot(cameraBodyFixed,surfaceAnchor)>.999999d&&gpuCamera.Orientation!=surfaceOrientation,"SurfaceLocal look changes orientation while preserving body-fixed anchor");var truthBeforeTranslation=gpuEarth.Presentation.Bodies.ToArray();var translatedFrom=gpuEarth.SurfaceFocus!.Value;gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MoveForward=1,MoveRight=1,DeltaSeconds=.1f});var translatedTo=gpuEarth.SurfaceFocus!.Value;Check(translatedTo.TangentFrame.Direction!=translatedFrom.TangentFrame.Direction&&translatedTo.BodyId==translatedFrom.BodyId&&Math.Abs(Math.Sqrt((gpuCamera.Position.Value-earth.Position.Value).LengthSquared)-gpuEarth.OrbitDistance)<1e-5&&truthBeforeTranslation.SequenceEqual(gpuEarth.Presentation.Bodies.ToArray()),"SurfaceLocal tangent translation moves the body-fixed anchor without moving celestial Earth");gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MoveBackward=1,MoveLeft=1,DeltaSeconds=.1f});Check(Double3.Dot(gpuEarth.SurfaceFocus!.Value.TangentFrame.Direction,surfaceAnchor)>.999999999999d,"opposed SurfaceLocal translation is stable and reversible");for(var step=0;step<128&&gpuEarth.CameraPresentationMode!=PlanetaryCameraPresentationMode.Orbital;step++)gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MouseWheelDetents=-1});Check(gpuEarth.CameraPresentationMode==PlanetaryCameraPresentationMode.Orbital&&gpuEarth.SurfaceFocus is null&&roundTripTruth.SequenceEqual(gpuEarth.Presentation.Bodies.ToArray()),"SurfaceLocal-to-orbital round trip releases anchor without changing celestial truth");Check(EarthPlanetaryScene.TryCreate(root,NativePlanetaryMode.CpuGpuValidation,EarthPlanetaryScene.MaximumPatchCapacity,out var validationScene,out var validationError)&&validationScene is not null&&validationScene.ActivePatchCount==0&&validationScene.Mode==NativePlanetaryMode.CpuGpuValidation&&!validationScene.DetailedComputeRequested,$"CPU/GPU oracle distant scene: {validationError}");

    void SetDistance(double radii){camera.Position=camera.Position with{Value=earth.Position.Value+earth.BodyFixedToRoot.Rotate(new Double3(0,0,earth.RadiusMetres*radii))};camera.Orientation=earth.BodyFixedToRoot;earthScene.UpdatePatches(camera);}
    void SetAltitude(double metres){Console.WriteLine($"Selecting Earth terrain at {metres:R} m");var direction=Double3.UnitZ;var surface=PlanetaryTerrainQuery.SurfaceRadius(earth.RadiusMetres,direction,EarthPlanetaryScene.Terrain);camera.Position=camera.Position with{Value=earth.Position.Value+earth.BodyFixedToRoot.Rotate(direction*(surface+metres))};camera.Orientation=earth.BodyFixedToRoot;earthScene.UpdatePatches(camera);}
    void SetGpuDistance(double radii){gpuCamera.Position=gpuCamera.Position with{Value=earth.Position.Value+earth.BodyFixedToRoot.Rotate(new Double3(0,0,earth.RadiusMetres*radii))};gpuCamera.Orientation=earth.BodyFixedToRoot;gpuEarth.UpdatePatches(gpuCamera);}
    static bool HasAncestorOverlap(PlanetaryPatch[] leaves){var active=leaves.ToHashSet();foreach(var leaf in leaves){var parent=leaf.Parent;while(parent is { } candidate){if(active.Contains(candidate))return true;parent=candidate.Parent;}}return false;}
    static bool IsTraversalOrdered(PlanetaryPatch[] leaves){for(var index=1;index<leaves.Length;index++)if(TraversalCompare(leaves[index-1],leaves[index])>0)return false;return true;}
    static int TraversalCompare(PlanetaryPatch left,PlanetaryPatch right){var face=((int)left.Face).CompareTo((int)right.Face);if(face!=0)return face;var shared=Math.Min(left.Level,right.Level);for(var depth=0;depth<shared;depth++){var leftBit=left.Level-1-depth;var rightBit=right.Level-1-depth;var leftChild=(((left.Y>>leftBit)&1)<<1)|((left.X>>leftBit)&1);var rightChild=(((right.Y>>rightBit)&1)<<1)|((right.X>>rightBit)&1);if(leftChild!=rightChild)return leftChild.CompareTo(rightChild);}return left.Level.CompareTo(right.Level);}
    double PatchDistance(PlanetaryPatch patch){var bounds=patch.Bounds;var center=earth.Position.Value+earth.BodyFixedToRoot.Rotate(CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5,(bounds.MinY+bounds.MaxY)*.5,earth.RadiusMetres));return Math.Sqrt((camera.Position.Value-center).LengthSquared);}
    static bool Contains(PlanetaryPatch patch,double u,double v){var bounds=patch.Bounds;return u>=bounds.MinX&&u<=bounds.MaxX&&v>=bounds.MinY&&v<=bounds.MaxY;}
    static ulong Hash(ReadOnlySpan<NativePlanetaryPatch> patches){ulong hash=14695981039346656037UL;foreach(ref readonly var patch in patches){Mix(patch.Face);Mix(patch.Level);Mix(patch.X);Mix(patch.Y);Mix((uint)BitConverter.SingleToInt32Bits(patch.CenterX));Mix((uint)BitConverter.SingleToInt32Bits(patch.CenterY));Mix((uint)BitConverter.SingleToInt32Bits(patch.CenterZ));Mix((uint)BitConverter.SingleToInt32Bits(patch.Radius));Mix((uint)BitConverter.SingleToInt32Bits(patch.ColorR));Mix((uint)BitConverter.SingleToInt32Bits(patch.ColorG));Mix((uint)BitConverter.SingleToInt32Bits(patch.ColorB));Mix(patch.StitchMask);void Mix(uint value)=>hash=(hash^value)*1099511628211UL;}Console.WriteLine($"Earth patch batch hash: 0x{hash:X16}");return hash;}
}

static void CelestialPlayerTorqueControlsTest()
{
    static DoubleQuaternion Advance(NativeInputState input)
    {
        Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"player torque scene: {error}");
        Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), input, out error), $"player torque input: {error}");
        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out error), $"player torque release: {error}");
        return scene.CurrentSnapshot.Objects[1].RootOrientation;
    }
    var w = Advance(new NativeInputState { MoveForward = 1 }); var s = Advance(new NativeInputState { MoveBackward = 1 });
    var a = Advance(new NativeInputState { MoveLeft = 1 }); var d = Advance(new NativeInputState { MoveRight = 1 });
    var q = Advance(new NativeInputState { MoveDown = 1 }); var e = Advance(new NativeInputState { MoveUp = 1 });
    var neutral = Advance(default); var cancelled = Advance(new NativeInputState { MoveForward = 1, MoveBackward = 1 });
    Check(w != s && a != d && q != e, "opposed pitch/yaw/roll inputs produce opposite authoritative torque states");
    Check(cancelled == neutral, "opposing inputs cancel");

    Check(CelestialAnalyticalScene.TryCreate(out var held, out var heldError) && held is not null, $"held control scene: {heldError}");
    var press = new NativeInputState { MoveForward = 1 };
    Check(held!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), press, out heldError) && held.TorqueTransitionCount == 1, "one torque-on edge commit");
    for (var index = 0; index < 100; index++) Check(held.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), press, out heldError), $"held torque update: {heldError}");
    Check(held.TorqueTransitionCount == 1, "held torque creates no history entries");
    Check(held.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out heldError) && held.TorqueTransitionCount == 2, "one torque-off edge commit");
}

static void CelestialSasModeSelectionTest()
{
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"SAS selection scene: {error}");
    var hold = new NativeInputState { SasModeKey = 2 };
    Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), hold, out error), $"hold selection: {error}");
    Check(scene.SasMode == SpacecraftSasMode.HoldAttitude && scene.HasHoldTarget && scene.HoldTarget == scene.CurrentSnapshot.Objects[1].RootOrientation, "hold captures the current authoritative orientation");
    Check(scene.TorqueTransitionCount == 0, "mode selection does not create a torque transaction");

    var cancelled = new NativeInputState { MoveForward = 1, MoveBackward = 1 };
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), cancelled, out error), $"cancelled manual input: {error}");
    Check(scene.SasMode == SpacecraftSasMode.HoldAttitude && scene.HasHoldTarget && scene.TorqueTransitionCount == 0, "opposed manual input preserves SAS state");

    var manual = new NativeInputState { MoveForward = 1 };
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), manual, out error), $"manual disengage: {error}");
    Check(scene.SasMode == SpacecraftSasMode.Off && !scene.HasHoldTarget && scene.TorqueTransitionCount == 1, "manual torque disengages SAS before its authoritative commit");

    for (uint key = 3; key <= 8; key++)
    {
        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = key }, out error), $"SAS mode {key}: {error}");
        Check(scene.SasMode == (SpacecraftSasMode)(key - 1) && !scene.HasHoldTarget, $"SAS key {key} maps deterministically");
    }
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out error), $"SAS off: {error}");
    Check(scene.SasMode == SpacecraftSasMode.Off && !scene.HasHoldTarget, "SAS off clears hold state");
}

static void CelestialSasControlCadenceTest()
{
    Check(CelestialAnalyticalScene.QuantizeSasTorque(new Double3(.005d, -.005d, 4.004d)) == new Double3(.01d, -.01d, 4d), "SAS torque quantizes midpoint away from zero after controller clamp");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(SimulationInstant.Zero, out var zeroBoundary) && zeroBoundary == new SimulationInstant(50_000), "zero-time SAS engagement schedules the first boundary");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(new SimulationInstant(50_000), out var exactBoundary) && exactBoundary == new SimulationInstant(100_000), "exact cadence time schedules the strictly next boundary");
    Check(CelestialAnalyticalScene.TryGetFirstSasControlBoundaryAfter(new SimulationInstant(73_000), out var betweenBoundary) && betweenBoundary == new SimulationInstant(100_000), "between-boundary engagement schedules the next boundary");
    static void SetSupportedSasRate(CelestialAnalyticalScene scene)
    {
        var fixtureCamera = CelestialAnalyticalScene.Camera;
        var camera = new CameraState(new FramePosition(new ReferenceFrameId(1), fixtureCamera.Position), DoubleQuaternion.Identity, fixtureCamera.Projection, CameraMode.Free);
        for (var index = 0; index < 4; index++) scene.ApplyPresentationInput(camera, new NativeInputState { RateDecrease = 1 }, out _, out _);
        Check(scene.Rate == SimulationRate.Ten, "10x is the highest supported SAS rate");
    }
    static CelestialAnalyticalScene CreateSelected()
    {
        Check(CelestialAnalyticalScene.TryCreate(out var scene, out var error) && scene is not null, $"SAS cadence scene: {error}");
        SetSupportedSasRate(scene!);
        Check(scene!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error), $"SAS prograde selection: {error}");
        return scene;
    }
    var scene = CreateSelected();
    Check(scene.CurrentTime == new SimulationInstant(10_000) && scene.TorqueTransitionCount == 0, "mode selection advances no SAS boundary");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(3_000), out var error), $"pre-boundary coast: {error}");
    Check(scene.SasCrossedBoundaryCount == 0 && scene.TorqueTransitionCount == 0, "no SAS boundary before 50000 microticks");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out error), $"first SAS boundary: {error}");
    Check(scene.CurrentTime == new SimulationInstant(50_000) && scene.SasCrossedBoundaryCount == 1 && scene.HasSasTorqueRequest && scene.LastSasTorque != Double3.Zero && scene.TorqueTransitionCount == 1, $"first exact SAS boundary commits one quantized torque: time={scene.CurrentTime.Ticks}, rate={scene.Rate.Numerator}:{scene.Rate.Denominator}, mode={scene.SasMode}, suspended={scene.SasControlSuspended}, next={scene.NextSasControlBoundary.Ticks}, boundaries={scene.SasCrossedBoundaryCount}, torque={scene.LastSasTorque}, transitions={scene.TorqueTransitionCount}");
    var transitions = scene.TorqueTransitionCount;
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out error), $"between-boundary coast: {error}");
    Check(scene.SasCrossedBoundaryCount == 0 && scene.TorqueTransitionCount == transitions, "between boundaries creates no transaction");

    static (DoubleQuaternion Orientation, Double3 Torque, int Transitions) AdvancePartition(ReadOnlySpan<long> partitions)
    {
        var candidate = CreateSelected();
        foreach (var ticks in partitions) Check(candidate.TryAdvanceByHostDuration(SimulationDuration.FromTicks(ticks), out var error), $"partitioned SAS advance: {error}");
        return (candidate.CurrentSnapshot.Objects[1].RootOrientation, candidate.LastSasTorque, candidate.TorqueTransitionCount);
    }
    var whole = AdvancePartition([10_000]);
    var partitioned = AdvancePartition([3_000, 4_000, 3_000]);
    Check(whole == partitioned, "SAS control boundaries are partition-independent");
    var cadenceHash = MixQuaternion(MixDouble3(14695981039346656037UL, whole.Torque), whole.Orientation);
    Check(cadenceHash == MixQuaternion(MixDouble3(14695981039346656037UL, partitioned.Torque), partitioned.Orientation), "SAS scripted sequence hash is deterministic");

    static (Double3 Torque, int Boundaries) EvaluateMode(uint key)
    {
        Check(CelestialAnalyticalScene.TryCreate(out var selected, out var selectionError) && selected is not null, $"SAS mode fixture: {selectionError}");
        SetSupportedSasRate(selected!);
        Check(selected!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = key }, out selectionError), $"SAS mode select: {selectionError}");
        Check(selected.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out selectionError), $"SAS mode boundary: {selectionError}");
        return (selected.LastSasTorque, selected.SasCrossedBoundaryCount);
    }
    Check(EvaluateMode(2) is { Torque: var holdTorque, Boundaries: 1 } && holdTorque == Double3.Zero, "hold-attitude evaluates its captured target at the boundary");
    Check(EvaluateMode(3).Torque != Double3.Zero, "prograde target evaluates at the boundary");
    Check(EvaluateMode(5).Torque != Double3.Zero, "normal target evaluates at the boundary");
    Check(EvaluateMode(7).Boundaries == 1, "radial-out target evaluates at the boundary");

    var multiple = CreateSelected();
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error), $"multiple SAS boundaries: {error}");
    Check(multiple.SasCrossedBoundaryCount == 2, "crossed SAS boundaries process chronologically");
    var beforeOff = multiple.TorqueTransitionCount;
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 1 }, out error), $"SAS off: {error}");
    Check(multiple.SasMode == SpacecraftSasMode.Off && multiple.LastSasTorque == Double3.Zero && multiple.TorqueTransitionCount == beforeOff + 1, "SAS off commits zero torque once");

    var switched = CreateSelected(); var nextBeforeSwitch = switched.NextSasControlBoundary;
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 5 }, out error), $"pre-boundary mode switch: {error}");
    Check(switched.SasMode == SpacecraftSasMode.Normal && switched.NextSasControlBoundary == nextBeforeSwitch && switched.NextSasControlBoundary > switched.CurrentTime, "active mode switch preserves a future cadence boundary");
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error), $"post-boundary mode switch coast: {error}");
    var nextAfterBoundaries = switched.NextSasControlBoundary;
    Check(switched.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 7 }, out error), $"post-boundary mode switch: {error}");
    Check(switched.SasMode == SpacecraftSasMode.RadialOut && switched.NextSasControlBoundary == nextAfterBoundaries && switched.NextSasControlBoundary > switched.CurrentTime, "post-boundary switch retains no stale target");

    Check(CelestialAnalyticalScene.TryCreate(out var late, out var lateError) && late is not null, $"late SAS fixture: {lateError}"); SetSupportedSasRate(late!);
    Check(late!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(100_000), out lateError), $"large off-mode advance: {lateError}");
    Check(late.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out lateError), $"late SAS engagement: {lateError}");
    Check(late.NextSasControlBoundary > late.CurrentTime, "late SAS engagement reinitializes a strictly future boundary");
    Check(late.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out lateError), $"late SAS cadence advance: {lateError}");
    Check(late.CurrentTime > new SimulationInstant(1_000_000) && late.SasCrossedBoundaryCount == 1, "late SAS engagement continues authoritative time without TargetBeforeCurrent");

    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error), $"off-to-active reengagement: {error}");
    Check(multiple.NextSasControlBoundary > multiple.CurrentTime, "off-to-active transition schedules a future boundary");
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { MoveForward = 1 }, out error), $"manual SAS disengagement: {error}");
    Check(multiple.SasMode == SpacecraftSasMode.Off, "manual torque disengages SAS");
    Check(multiple.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), new NativeInputState { SasModeKey = 3 }, out error) && multiple.NextSasControlBoundary > multiple.CurrentTime, $"manual reengagement: {error}");

    var paused = CreateSelected(); var camera = new CameraState(new FramePosition(new ReferenceFrameId(1), CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    paused.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    var pausedBoundary = paused.NextSasControlBoundary; var pausedTime = paused.CurrentTime;
    Check(paused.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out error) && paused.SasCrossedBoundaryCount == 0 && paused.TorqueTransitionCount == 0 && paused.CurrentTime == pausedTime && paused.NextSasControlBoundary == pausedBoundary, $"pause suppresses SAS cadence: {error}");
    paused.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(paused.TryAdvanceByHostDuration(SimulationDuration.FromTicks(4_000), out error) && paused.SasCrossedBoundaryCount == 1, $"resume preserves the next future cadence boundary: {error}");

    Check(CelestialAnalyticalScene.TryCreate(out var highWarp, out var highWarpError) && highWarp is not null, $"high-warp SAS fixture: {highWarpError}");
    Check(highWarp!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out highWarpError), $"high-warp SAS selection: {highWarpError}");
    Check(highWarp.SasControlSuspended && highWarp.SasCrossedBoundaryCount == 0 && highWarp.SasMode == SpacecraftSasMode.Prograde, "SAS selection at 10000x suspends without cadence work");
    var highWarpTime = highWarp.CurrentTime; var highWarpTransitions = highWarp.TorqueTransitionCount;
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out highWarpError), $"high-warp suspended coast: {highWarpError}");
    Check(highWarp.CurrentTime > highWarpTime && highWarp.SasControlSuspended && highWarp.SasCrossedBoundaryCount == 0 && highWarp.TorqueTransitionCount == highWarpTransitions, "suspended SAS leaves clock advancing without cap or transaction growth");
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 5 }, out highWarpError), $"high-warp mode switch: {highWarpError}");
    Check(highWarp.SasControlSuspended && highWarp.SasMode == SpacecraftSasMode.Normal && highWarp.SasCrossedBoundaryCount == 0, "mode switch while suspended retains the newest mode");
    var highWarpCamera = new CameraState(new FramePosition(new ReferenceFrameId(1), CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    for (var index = 0; index < 4; index++) highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateDecrease = 1 }, out _, out _);
    Check(highWarp.Rate == SimulationRate.Ten && !highWarp.SasControlSuspended && highWarp.NextSasControlBoundary > highWarp.CurrentTime, "supported-rate transition resumes at a strictly future boundary");
    var ticksToResume = (highWarp.NextSasControlBoundary.Ticks - highWarp.CurrentTime.Ticks) / SimulationRate.Ten.Numerator;
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(ticksToResume), out highWarpError) && highWarp.SasCrossedBoundaryCount == 1, $"supported-rate resume runs only the future boundary: {highWarpError}");
    var activeTransitions = highWarp.TorqueTransitionCount;
    highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    Check(highWarp.Rate == SimulationRate.Hundred && highWarp.SasControlSuspended && highWarp.LastSasTorque == Double3.Zero && highWarp.TorqueTransitionCount == activeTransitions + 1, "unsupported-rate transition commits zero torque once");
    highWarp.ApplyPresentationInput(highWarpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    Check(highWarp.TorqueTransitionCount == activeTransitions + 1, "repeated unsupported rate changes do not duplicate zero torque");
    Check(highWarp.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out highWarpError) && highWarp.SasMode == SpacecraftSasMode.Off && !highWarp.SasControlSuspended && highWarp.TorqueTransitionCount == activeTransitions + 1, $"off while suspended clears without duplicate zero torque: {highWarpError}");
    _ = CelestialAnalyticalScene.QuantizeSasTorque(Double3.UnitX); var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) _ = CelestialAnalyticalScene.QuantizeSasTorque(new Double3(index * .001d, -index * .001d, 0d)); Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore, "warmed SAS torque quantization allocates zero bytes");
    Console.WriteLine($"Deterministic SAS cadence hash: 0x{cadenceHash:X16}; quantization allocation=0 bytes");
}

static void CelestialSasConvergenceTest()
{
    var progradeTarget = Target(FlightReferenceMode.Prograde); var normalTarget = Target(FlightReferenceMode.Normal); var radialTarget = Target(FlightReferenceMode.RadialOut); var retrogradeTarget = Target(FlightReferenceMode.Retrograde);
    var progradeResult = Run(CreateOneXScene(progradeTarget * DoubleQuaternion.FromAxisAngle(Double3.UnitZ, -Math.PI / 2d), Double3.Zero), 3, 35d);
    var normalResult = Run(CreateOneXScene(normalTarget * DoubleQuaternion.FromAxisAngle(Double3.UnitY, -Math.PI / 2d), Double3.Zero), 5, 35d);
    var radialResult = Run(CreateOneXScene(radialTarget * DoubleQuaternion.FromAxisAngle(Double3.UnitZ, -Math.PI / 2d), Double3.Zero), 7, 35d);
    var retrogradeResult = Run(CreateOneXScene(retrogradeTarget * DoubleQuaternion.FromAxisAngle(Double3.UnitZ, -Math.PI), Double3.Zero), 4, 55d);
    Assert90(progradeResult, "Prograde"); Assert90(normalResult, "Normal"); Assert90(radialResult, "Radial Out");
    Check(retrogradeResult.FinalError <= .01d && retrogradeResult.FinalRate <= .01d && retrogradeResult.SettledSeconds is > 0d and <= 55d && retrogradeResult.Crossings <= 1 && retrogradeResult.PeakOvershoot <= 8d * Math.PI / 180d, $"180-degree Retrograde converges through deterministic shortest path: {retrogradeResult}");
    var hold = CreateOneXScene(DoubleQuaternion.Identity, new Double3(.05d, 0d, 0d)); Select(hold, 2); var holdResult = RunToSettled(hold, 20d); Check(holdResult.FinalRate <= .01d && holdResult.FinalError <= .01d, "hold damping settles without target drift");
    var switches = CreateOneXScene(progradeTarget, Double3.Zero); Select(switches, 3); _ = RunToSettled(switches, 2d); Select(switches, 5); var switchOne = RunToSettled(switches, 55d); Select(switches, 7); var switchTwo = RunToSettled(switches, 35d); Check(switchOne.FinalError <= .01d && switchTwo.FinalError <= .01d, $"Prograde->Normal and Normal->Radial Out settle: normal={switchOne}; radial={switchTwo}");
    var hash = Mix(Mix(Mix(Mix(Mix(Mix(14695981039346656037UL, MetricsHash(progradeResult)), MetricsHash(retrogradeResult)), MetricsHash(normalResult)), MetricsHash(radialResult)), MetricsHash(holdResult)), MetricsHash(switchTwo));
    Console.WriteLine($"Deterministic SAS convergence hash: 0x{hash:X16}; prograde={progradeResult}; retrograde={retrogradeResult}; normal={normalResult}; radial={radialResult}; hold={holdResult}; switch-normal={switchOne}; switch-radial={switchTwo}");

    static void Assert90(in SasConvergenceMetrics value, string name) => Check(value.InitialError > 1.5d && value.FinalError <= .01d && value.FinalRate <= .01d && value.SettledSeconds is > 0d and <= 35d && value.Crossings <= 1 && value.PeakOvershoot <= 5d * Math.PI / 180d, $"{name} 90-degree acquisition converges through its moving reference target: {value}");
    static DoubleQuaternion Target(FlightReferenceMode mode) { var result = FlightReferenceEvaluator.TryEvaluate(new Double3(CelestialAnalyticalScene.OrbitRadiusMetres, 0d, 0d), new Double3(0d, Math.Sqrt(CelestialAnalyticalScene.RootMu / CelestialAnalyticalScene.OrbitRadiusMetres), 0d), DoubleQuaternion.Identity, mode); var status = SpacecraftSasTargetOrientation.TryCreate(result.DirectionCarrierParent, Double3.UnitZ, out var target); Check(result.Succeeded && status == SpacecraftSasControlStatus.Success, $"{mode} convergence target"); return target; }
    static SasConvergenceMetrics Run(CelestialAnalyticalScene scene, uint key, double seconds) { Select(scene, key); return RunToSettled(scene, seconds); }
    static CelestialAnalyticalScene CreateOneXScene(in DoubleQuaternion initialOrientation, in Double3 initialAngularVelocity)
    {
        Check(CelestialAnalyticalScene.TryCreateForTest(initialOrientation, initialAngularVelocity, out var scene, out var error) && scene is not null, $"convergence scene: {error}"); var root = new ReferenceFrameId(1); var camera = new CameraState(new FramePosition(root, CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free); for (var index = 0; index < 5; index++) scene!.ApplyPresentationInput(camera, new NativeInputState { RateDecrease = 1 }, out _, out _); Check(scene!.Rate == SimulationRate.One, "convergence fixture runs at 1x"); return scene;
    }
    static void Select(CelestialAnalyticalScene scene, uint key) => Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = key }, out var error), $"select SAS mode {key}: {error}");
    static SasConvergenceMetrics RunToSettled(CelestialAnalyticalScene scene, double maximumSeconds)
    {
        var initial = Error(scene); var previous = initial; var peakOvershoot = 0d; var crossings = 0; var transactionStart = scene.TorqueTransitionCount; var settledAt = -1d; var settledTransactionCount = -1; var postSettle = 0;
        for (var boundary = 1; boundary <= (int)(maximumSeconds / .05d); boundary++)
        {
            Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(50_000), default, out var error), $"convergence boundary {boundary}: {error}"); var current = Error(scene); if (current > previous + .001d && previous < .01d) { crossings++; peakOvershoot = Math.Max(peakOvershoot, current); } previous = current;
            if (settledAt < 0d && scene.LastSasControlStatus == SpacecraftSasControlStatus.Settled) { settledAt = boundary * .05d; settledTransactionCount = scene.TorqueTransitionCount; }
            else if (settledTransactionCount >= 0 && scene.TorqueTransitionCount > settledTransactionCount) { postSettle += scene.TorqueTransitionCount - settledTransactionCount; settledTransactionCount = scene.TorqueTransitionCount; }
        }
        var rate = CurrentAngularRate(scene); return new(initial, previous, rate, peakOvershoot, crossings, settledAt, scene.TorqueTransitionCount - transactionStart, postSettle, scene.LastRawSasTorque, scene.LastSasTorque);
    }
    static double Error(CelestialAnalyticalScene scene) { Check(scene.TryGetPresentationSasTargetForTest(scene.CurrentTime, out var target), "convergence target"); var current = scene.CurrentSnapshot.Objects[1].RootOrientation; var error = current.Conjugate() * target; if (error.W < 0d) error = new(-error.X, -error.Y, -error.Z, -error.W); return 2d * Math.Atan2(Math.Sqrt(error.X * error.X + error.Y * error.Y + error.Z * error.Z), error.W); }
    static double CurrentAngularRate(CelestialAnalyticalScene scene) { Check(scene.TryGetCurrentAngularVelocityForTest(out var angularVelocity), "convergence angular velocity"); return Math.Sqrt(angularVelocity.LengthSquared); }
}
static ulong MetricsHash(in SasConvergenceMetrics value) { var hash = Mix(14695981039346656037UL, (ulong)BitConverter.DoubleToInt64Bits(value.InitialError)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.FinalError)); return Mix(hash, (ulong)value.TransactionCount); }

static void CelestialSasDiagnosticIndicatorsTest()
{
    static void SetSupportedRate(CelestialAnalyticalScene scene, CameraState camera)
    {
        for (var index = 0; index < 4; index++) scene.ApplyPresentationInput(camera, new NativeInputState { RateDecrease = 1 }, out _, out _);
    }
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var createError) && scene is not null, $"SAS indicator fixture: {createError}");
    var root = new ReferenceFrameId(1); var camera = new CameraState(new FramePosition(root, CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free);
    var initial = scene!.CurrentSnapshot;
    Check(initial.BodyForwardIndicator is { } initialForward && initial.TargetDirectionIndicator is null, "body-forward is visible while SAS-off target is hidden");
    initialForward = initial.BodyForwardIndicator.GetValueOrDefault();
    var expectedForward = initial.Objects[1].RootOrientation.Rotate(Double3.UnitX);
    CheckNear((initialForward.End.Value - initialForward.Start.Value).Normalized(), expectedForward, "body-forward endpoint matches q.Rotate(+X)");

    SetSupportedRate(scene, camera);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out var advanceError), $"select prograde for indicators: {advanceError}");
    var prograde = scene.CurrentSnapshot;
    Check(prograde.BodyForwardIndicator is not null && prograde.TargetDirectionIndicator is { } target, "active valid SAS publishes both indicators");
    target = prograde.TargetDirectionIndicator.GetValueOrDefault();
    Check(target.Start == prograde.BodyForwardIndicator!.Value.Start && (target.End.Value - target.Start.Value).LengthSquared > 0d, "target indicator begins at spacecraft and has direction");
    Check(scene.TryGetPresentationSasTargetForTest(scene.CurrentTime, out var targetOrientation), "pure SAS target available");
    CheckNear((target.End.Value - target.Start.Value).Normalized(), targetOrientation.Rotate(Double3.UnitX), "target endpoint matches selected reference direction");

    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 2 }, out advanceError), $"select hold for indicators: {advanceError}");
    var hold = scene.CurrentSnapshot; Check(hold.TargetDirectionIndicator is { } holdTarget && scene.HasHoldTarget, "hold target indicator visible");
    holdTarget = hold.TargetDirectionIndicator.GetValueOrDefault();
    CheckNear((holdTarget.End.Value - holdTarget.Start.Value).Normalized(), scene.HoldTarget.Rotate(Double3.UnitX), "hold indicator follows captured target +X");

    scene.ApplyPresentationInput(camera, new NativeInputState { LookActive = 1, MouseDeltaX = 10f }, out _, out _);
    Check(scene.TryBuildCandidateForTest(out var afterCamera, out var candidateError) && afterCamera is not null, $"camera-independent indicator candidate: {candidateError}");
    Check(IndicatorHash(hold.BodyForwardIndicator!.Value) == IndicatorHash(afterCamera!.BodyForwardIndicator!.Value) && IndicatorHash(hold.TargetDirectionIndicator!.Value) == IndicatorHash(afterCamera.TargetDirectionIndicator!.Value), "camera movement does not alter world-space indicators");

    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), default, out advanceError), $"paused indicator publication: {advanceError}");
    Check(IndicatorHash(hold.BodyForwardIndicator!.Value) == IndicatorHash(scene.CurrentSnapshot.BodyForwardIndicator!.Value), "pause freezes authoritative body-forward orientation");
    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 1 }, out advanceError), $"disable SAS indicators: {advanceError}");
    Check(scene.CurrentSnapshot.TargetDirectionIndicator is null, "SAS-off hides target indicator");

    Check(CelestialAnalyticalScene.TryCreate(out var replay, out var replayError) && replay is not null, $"SAS indicator replay: {replayError}"); var replayScene = replay!; var replayCamera = new CameraState(new FramePosition(root, CelestialAnalyticalScene.Camera.Position), DoubleQuaternion.Identity, CelestialAnalyticalScene.Camera.Projection, CameraMode.Free); SetSupportedRate(replayScene, replayCamera); Check(replayScene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), new NativeInputState { SasModeKey = 3 }, out replayError), $"SAS indicator replay advance: {replayError}");
    var forwardHash = IndicatorHash(prograde.BodyForwardIndicator!.Value); var targetHash = IndicatorHash(prograde.TargetDirectionIndicator!.Value); Check(forwardHash == IndicatorHash(replayScene.CurrentSnapshot.BodyForwardIndicator!.Value) && targetHash == IndicatorHash(replayScene.CurrentSnapshot.TargetDirectionIndicator!.Value), "indicator hashes are deterministic");
    var submission = new RenderFrameSubmission(3, 257); var cameraRoot = new UniversePosition(CelestialAnalyticalScene.Camera.Position, root); var gpuCamera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.BodyForwardVertexCount == 2 && submission.TargetDirectionVertexCount == 2, "indicator transport uses fixed two-vertex streams");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(prograde, gpuCamera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm indicator transport"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm indicator transport allocation");
    Console.WriteLine($"Deterministic SAS indicator hashes: forward=0x{forwardHash:X16}; target=0x{targetHash:X16}; transport allocation=0 bytes");
}

static void MeshHandleTest() { Check(!MeshHandle.Invalid.IsValid, "zero invalid"); Check(MeshHandle.Triangle.IsValid, "triangle valid"); }
static void LayoutTest()
{
    Check(Marshal.SizeOf<NativeEncodedPosition>() == 32, "encoded size"); Check(Marshal.SizeOf<NativeCameraData>() == 96, "native camera size"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.Position)).ToInt32() == 0, "native camera position offset"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.ViewProjection)).ToInt32() == 32, "native camera matrix offset"); Check(Marshal.SizeOf<GpuCameraData>() == 96, "GPU camera size"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.Position)).ToInt32() == 0, "GPU camera position offset"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.ViewProjection)).ToInt32() == 32, "GPU camera matrix offset"); Check(Marshal.SizeOf<NativeRenderTransform>() == 32, "transform size"); Check(Marshal.SizeOf<NativeRenderObject>() == 80, "object stride"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Position)).ToInt32() == 0, "position offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Transform)).ToInt32() == 32, "transform offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Mesh)).ToInt32() == 64, "mesh offset"); Check(Marshal.SizeOf<NativeDrawBatch>() == 16, "batch stride"); Check(Marshal.SizeOf<NativePlanetaryGpuConstants>() == 80 && Marshal.OffsetOf<NativePlanetaryGpuConstants>(nameof(NativePlanetaryGpuConstants.CameraBodyLowX)).ToInt32()==16 && Marshal.OffsetOf<NativePlanetaryGpuConstants>(nameof(NativePlanetaryGpuConstants.RefinementThreshold)).ToInt32()==32 && Marshal.OffsetOf<NativePlanetaryGpuConstants>(nameof(NativePlanetaryGpuConstants.MaximumLevel)).ToInt32()==48 && Marshal.OffsetOf<NativePlanetaryGpuConstants>(nameof(NativePlanetaryGpuConstants.ViewForwardX)).ToInt32()==64 && Marshal.SizeOf<NativePlanetaryPresentation>() == 176 && Marshal.SizeOf<NativeSolarLighting>() == 48 && Marshal.OffsetOf<NativeSolarLighting>(nameof(NativeSolarLighting.SpeedHud)).ToInt32()==44 && Marshal.SizeOf<NativePlanetaryEnvironment>()==128 && Marshal.SizeOf<NativePlanetaryEyeball>()==128 && Marshal.SizeOf<NativeFrameSubmission>() == 800, "planetary terrain, environment, stellar, orientation, speed-HUD, and eyeball handoff frame ABI sizes"); Check(Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryGpu)).ToInt32() == 208 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryMode)).ToInt32() == 288 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryPresentation)).ToInt32() == 304 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.DistantBodies)).ToInt32() == 480 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.DistantBodyCount)).ToInt32() == 488 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.DistantBodyPadding)).ToInt32() == 492 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.SolarLighting)).ToInt32() == 496 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryEnvironment)).ToInt32()==544 && Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryEyeball)).ToInt32()==672, "planetary environment, stellar, orientation, and eyeball handoff frame ABI offsets"); Check(Marshal.SizeOf<NativeInputState>() == 68 && Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.SasModeKey)).ToInt32() == 60 && Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.PresentationFocus)).ToInt32() == 64 && Enum.GetUnderlyingType(typeof(NativePresentationFocus))==typeof(uint), "focus input layout"); Check(NativeRuntime.GetAbiLayout(out var abi) == NativeResult.Success && abi.InputStateSize == 68 && abi.InputSasModeKeyOffset == 60 && abi.InputPresentationFocusOffset == 64 && abi.FrameSubmissionSize == 800 && abi.FramePlanetaryGpuOffset == 208 && abi.FramePlanetaryModeOffset == 288 && abi.FramePlanetaryPresentationOffset == 304 && abi.FrameSolarLightingOffset == 496&&abi.FramePlanetaryEnvironmentOffset==544&&abi.FramePlanetaryEyeballOffset==672, "native frame ABI layout");
}
static void TransformTest() { var t = RenderTransform.FromAuthoritative(new DoubleQuaternion(0, 0, Math.Sqrt(.5), Math.Sqrt(.5)), new Double3(-1, 2, 3)); Check(t.Rotation.W > .7f && t.Scale.X == -1, "conversion/negative scale policy"); Check(FloatQuaternion.Identity == new FloatQuaternion(0, 0, 0, 1), "identity"); }
static void OrbitCurveTransportTest()
{
    var root = new ReferenceFrameId(1); var cameraRoot = new UniversePosition(new Double3(1e12, 0, 0), root); var positions = new[] { new UniversePosition(cameraRoot.Value + new Double3(1, 2, -3), root), new UniversePosition(cameraRoot.Value + new Double3(2, 3, -4), root), new UniversePosition(cameraRoot.Value + new Double3(1, 2, -3), root) };
    Check(ResolvedOrbitCurve.TryCreate(positions, out var curve) && curve is not null, "immutable orbit curve"); var objects = new[] { Object(1, cameraRoot, MeshHandle.Triangle) }; Check(ResolvedRenderSnapshot.TryCreate(objects, curve, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null, "curve snapshot");
    var submission = new RenderFrameSubmission(1, 3); var camera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.OrbitVertexCount == 3, "curve transport"); Check(submission.OrbitVertices[0].X == 1f && submission.OrbitVertices[0].Y == 2f && submission.OrbitVertices[0].Z == -3f, "double camera-relative line conversion"); _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm curve transport"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm curve transport allocation");
}
static void RelativeTest() { var camera = new Double3(4e12, -3e12, 7e12); var positive = CameraRelativeRenderPosition.Create(new Double3(4e12 + .25, -3e12, 7e12),camera); var negative = CameraRelativeRenderPosition.Create(new Double3(4e12 - .25, -3e12, 7e12),camera); Check(positive.Value.X > 0 && negative.Value.X < 0, "relative signs"); }
static void BatchTest()
{
    var frame = new ReferenceFrameId(1); var position = new UniversePosition(new Double3(4e12, 0, 0), frame); var camera = Camera(position);
    var submission = new RenderFrameSubmission(1000); submission.Begin(camera,position); for (var i = 0; i < 1000; i++) submission.Add(new UniversePosition(new Double3(4e12 + i, 0, 0), frame), DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); submission.Complete(); Check(submission.ObjectCount == 1000 && submission.BatchCount == 1 && submission.Batches[0].ObjectCount == 1000, "automatic stable batch");
    var small = new RenderFrameSubmission(1); small.Begin(camera,position); small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); Throws<InvalidOperationException>(() => small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle));
    var invalid = new RenderFrameSubmission(2); invalid.Begin(camera,position); Throws<ArgumentOutOfRangeException>(() => invalid.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Invalid));
}
static void ResolvedTransportTest()
{
    var root = new ReferenceFrameId(1); var other = new ReferenceFrameId(2); var cameraRoot = new UniversePosition(new Double3(4e12, -3e12, 7e12), root); var camera = Camera(cameraRoot);
    var source = new[] { Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value + new Double3(.25, 0, 0), root), new MeshHandle(2)), Object(3, new UniversePosition(cameraRoot.Value + new Double3(.5, 0, 0), root), MeshHandle.Triangle) };
    Check(ResolvedRenderSnapshot.TryCreate(source, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null, "valid snapshot");
    var frozenFirst = snapshot!.Objects[0]; source[0] = Object(9, new UniversePosition(Double3.Zero, root), MeshHandle.Invalid); Check(snapshot.Objects[0] == frozenFirst && snapshot.Count == 3, "snapshot copied caller data");
    Check(snapshot.Objects[0].Id.Value == 1 && snapshot.Objects[1].Id.Value == 2 && snapshot.Objects[2].Id.Value == 3, "caller order retained");
    Check(!ResolvedRenderSnapshot.TryCreate([], out _, out status) && status == ResolvedRenderSnapshotStatus.Empty, "empty rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(0, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidObjectId, "zero ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(1, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.DuplicateObjectId, "duplicate ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, new UniversePosition(new Double3(double.NaN, 0, 0), root), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFinitePosition, "non-finite position rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, default, new Double3(1, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidOrientation, "invalid orientation rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, DoubleQuaternion.Identity, new Double3(double.NaN, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFiniteScale, "non-finite scale rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Invalid)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidMeshHandle, "invalid mesh rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value, other), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.MixedRootFrame, "mixed roots rejected");

    var destination = new RenderFrameSubmission(3); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "transport build"); Check(destination.ObjectCount == 3 && destination.BatchCount == 3 && destination.Batches[0].FirstObject == 0 && destination.Batches[1].FirstObject == 1 && destination.Batches[2].FirstObject == 2, "stable contiguous batches"); Check(destination.Objects[1].Position == CameraRelativeRenderPosition.Create(cameraRoot.Value + new Double3(.25, 0, 0),cameraRoot.Value).Encode(), "sole post-subtraction encoder output"); Check(destination.Objects[1].Position.Reconstruct().X > 0, "large-root relative separation");
    var retainedObject = destination.Objects[0]; var retainedCount = destination.ObjectCount; var retainedBatches = destination.BatchCount;
    Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "camera root mismatch"); Check(destination.ObjectCount == retainedCount && destination.BatchCount == retainedBatches && destination.Objects[0] == retainedObject, "mismatch atomicity");
    var small = new RenderFrameSubmission(2); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, small) == ResolvedRenderSubmissionBuildStatus.DestinationCapacityExceeded && small.ObjectCount == 0 && small.BatchCount == 0, "object and batch capacity protected");
    var badCamera = camera; badCamera.ViewProjection.C0R0 = float.NaN; Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, badCamera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.InvalidCameraData && destination.ObjectCount == retainedCount, "invalid camera atomicity");
    var hash = TransportHash(destination); Check(TransportHash(destination) == hash, "transport hash repeatability"); Console.WriteLine($"Deterministic render-transport hash: 0x{hash:X16}");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037;
    for (var i = 0; i < 100_000; i++) { Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "warm build"); checksum = Mix(checksum, (ulong)BitConverter.SingleToInt32Bits(destination.Objects[1].Position.HighX)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && checksum != 0, "warm successful builds allocate zero bytes");
    before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "warm mismatch"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm mismatch builds allocate zero bytes");
}
static void CameraSnapshotAllocationTest()
{
    var root = new ReferenceFrameId(1); var snapshot = new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root, null, ReferenceFrameKind.Ecl, "root"), CelestialFrameFactory.RootEcl())]); var resolver = new ReferenceFrameResolver(snapshot); var state = new CameraState(new FramePosition(root, new Double3(4e12, -3e12, 7e12)), DoubleQuaternion.Identity, new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1000d), CameraMode.Free);
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var first, out var resolvedRoot, out _), "camera snapshot setup");Check(resolvedRoot.Value==state.Position.Value&&first.Position==default,"camera root remains managed FP64 authority while legacy GPU translation field stays zero"); var hash = CameraHash(first); Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var second, out _, out _) && CameraHash(second) == hash, "camera snapshot deterministic result");
    var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out _, out _, out _), "warm camera snapshot"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm camera snapshots allocate zero bytes"); Console.WriteLine($"Deterministic camera snapshot hash: 0x{hash:X16}");
}
static void StaticReferenceFrameFixtureTransportTest()
{
    var root = new ReferenceFrameId(1); var planet = new ReferenceFrameId(2); var moon = new ReferenceFrameId(3); var vessel = new ReferenceFrameId(4);
    var builder = new ReferenceFrameGraphBuilder();
    builder.Add(new ReferenceFrameNode(root, null, ReferenceFrameKind.Ecl, "fixture-ecl"));
    builder.Add(new ReferenceFrameNode(planet, root, ReferenceFrameKind.Cce, "fixture-cce"));
    builder.Add(new ReferenceFrameNode(moon, planet, ReferenceFrameKind.Cci, "fixture-cci"));
    builder.Add(new ReferenceFrameNode(vessel, moon, ReferenceFrameKind.Ccf, "fixture-ccf"));
    var graph = builder.Build();
    var transforms = new ReferenceFrameTransformSet(graph,
    [
        new ReferenceFrameEvaluation(root, new EvaluatedReferenceFrame(FrameTransform.Identity, Double3.Zero, Double3.Zero, true)),
        new ReferenceFrameEvaluation(planet, new EvaluatedReferenceFrame(new FrameTransform(new Double3(100, 20, 0), DoubleQuaternion.Identity), new Double3(1, 0, 0), Double3.Zero, true)),
        new ReferenceFrameEvaluation(moon, new EvaluatedReferenceFrame(new FrameTransform(new Double3(0, 10, 0), DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d)), new Double3(0, 2, 0), new Double3(0, 0, .5d), false)),
        new ReferenceFrameEvaluation(vessel, new EvaluatedReferenceFrame(new FrameTransform(new Double3(2, 0, 0), DoubleQuaternion.Identity), Double3.Zero, Double3.Zero, false)),
    ]);
    Span<ReferenceFrameId> sourcePath = stackalloc ReferenceFrameId[4]; Span<ReferenceFrameId> targetPath = stackalloc ReferenceFrameId[4]; Span<ReferenceFrameId> traversalPath = stackalloc ReferenceFrameId[7];
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, root, root, sourcePath, targetPath, traversalPath, out var starTransform) == ReferenceFrameTransformResolutionStatus.Success, "star resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, planet, root, sourcePath, targetPath, traversalPath, out var planetTransform) == ReferenceFrameTransformResolutionStatus.Success, "planet resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, moon, root, sourcePath, targetPath, traversalPath, out var moonTransform) == ReferenceFrameTransformResolutionStatus.Success, "moon resolution");
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, vessel, root, sourcePath, targetPath, traversalPath, out var vesselTransform) == ReferenceFrameTransformResolutionStatus.Success, "vessel resolution");
    var objects = new[]
    {
        new ResolvedRenderObject(new RenderObjectId(1), new UniversePosition(starTransform.ConvertPosition(Double3.Zero), root), starTransform.ConvertOrientation(DoubleQuaternion.Identity), new Double3(200,200,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(2), new UniversePosition(planetTransform.ConvertPosition(Double3.Zero), root), (planetTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.35d)).Normalized(), new Double3(125,125,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(3), new UniversePosition(moonTransform.ConvertPosition(Double3.Zero), root), (moonTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.20d)).Normalized(), new Double3(22,22,1), MeshHandle.Triangle),
        new ResolvedRenderObject(new RenderObjectId(4), new UniversePosition(vesselTransform.ConvertPosition(Double3.Zero), root), (vesselTransform.ConvertOrientation(DoubleQuaternion.Identity) * DoubleQuaternion.FromAxisAngle(Double3.UnitZ,-.35d)).Normalized(), new Double3(16,16,1), MeshHandle.Triangle),
    };
    Check(objects[0].RootPosition.Value == Double3.Zero && objects[1].RootPosition.Value == new Double3(100,20,0) && objects[2].RootPosition.Value == new Double3(100,30,0) && objects[3].RootPosition.Value == new Double3(100,32,0), "approved root positions");
    Check(objects[0].Id.Value == 1 && objects[1].Id.Value == 2 && objects[2].Id.Value == 3 && objects[3].Id.Value == 4, "stable object ordering");
    Check(objects[0].Scale == new Double3(200,200,1) && objects[1].Scale == new Double3(125,125,1) && objects[2].Scale == new Double3(22,22,1) && objects[3].Scale == new Double3(16,16,1), "refined presentation scales");
    Check(ResolvedRenderSnapshot.TryCreate(objects, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null && snapshot.RootFrame == root, "fixture snapshot");
    var cameraRoot = new UniversePosition(new Double3(50,16,70), root); var camera = Camera(cameraRoot); var submission = new RenderFrameSubmission(4);
    Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "fixture submission");
    Check(submission.ObjectCount == 4 && submission.BatchCount == 1 && submission.Batches[0].Mesh == MeshHandle.Triangle && submission.Batches[0].FirstObject == 0 && submission.Batches[0].ObjectCount == 4, "fixture batch");
    VerifyFixtureViewport(objects, root, cameraRoot, 16d / 9d, 2560, 1440, "16:9");
    VerifyFixtureViewport(objects, root, cameraRoot, 3440d / 1440d, 3440, 1440, "3440x1440");
    var hash = FixtureSetupHash(objects); Check(hash == FixtureSetupHash(objects), "fixture setup hash repeatability"); Console.WriteLine($"Deterministic fixture render setup hash: 0x{hash:X16}");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037UL;
    for (var i = 0; i < 100_000; i++) { Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot!, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm fixture build"); checksum = Mix(checksum, (ulong)BitConverter.SingleToInt32Bits(submission.Objects[3].Position.HighX)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && checksum != 0, "warm fixture frame assembly allocates zero bytes");
}
static void DynamicReferenceFrameFixturePublicationTest()
{
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var scene, out var diagnostics, out var createError) && scene is not null, $"dynamic fixture creation: {createError}");
    Check(scene!.GraphConstructionCount == 1 && scene.CurrentTime == SimulationInstant.Zero, "dynamic topology constructed once");
    var zero = DynamicReferenceFrameFixtureScene.EvaluateKinematics(SimulationInstant.Zero);
    CheckNear(zero.MoonLocalPosition, new Double3(0, 10, 0), "moon zero position"); CheckNear(zero.VesselLocalPosition, new Double3(3, 0, 0), "vessel zero position"); CheckNear(zero.MoonLocalVelocity, new Double3(-2, 0, 0), "moon zero velocity"); CheckNear(zero.VesselLocalVelocity, new Double3(0, 2.55, 0), "vessel zero velocity");
    var oneSecond = SimulationInstant.FromWholeSeconds(1); var one = DynamicReferenceFrameFixtureScene.EvaluateKinematics(oneSecond);
    CheckNear(one.MoonLocalPosition, new Double3(10 * Math.Cos(Math.PI / 2d + .20d), 10 * Math.Sin(Math.PI / 2d + .20d), 0), "moon one-second position"); CheckNear(one.VesselLocalPosition, new Double3(3 * Math.Cos(.85d), 3 * Math.Sin(.85d), 0), "vessel one-second position");
    Check(scene.TryBuildCandidateForTest(SimulationInstant.FromWholeSeconds(5), out var firstCandidate, out var firstError) && firstCandidate is not null, $"first candidate: {firstError}"); Check(scene.TryBuildCandidateForTest(SimulationInstant.FromWholeSeconds(5), out var secondCandidate, out var secondError) && secondCandidate is not null, $"second candidate: {secondError}");
    Check(DynamicSnapshotHash(SimulationInstant.FromWholeSeconds(5), firstCandidate!) == DynamicSnapshotHash(SimulationInstant.FromWholeSeconds(5), secondCandidate!), "same time candidate repeatability");
    var retained = scene.CurrentSnapshot; var retainedHash = DynamicSnapshotHash(scene.CurrentTime, retained); Check(!scene.TryPublishCandidateForTest(SimulationInstant.FromWholeSeconds(5), true, out _), "controlled candidate rejection"); Check(ReferenceEquals(scene.CurrentSnapshot, retained) && DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot) == retainedHash, "rejection retains prior immutable snapshot");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), out var advanceError), $"whole advance: {advanceError}"); var wholeHash = DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot);
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var partitioned, out var partitionDiagnostics, out var partitionError) && partitioned is not null, $"partition fixture creation: {partitionError}"); for (var index = 0; index < 10; index++) Check(partitioned!.TryAdvanceByHostDuration(SimulationDuration.FromTicks(100_000), out var partitionAdvanceError), $"partition advance: {partitionAdvanceError}");
    Check(scene.CurrentTime == partitioned!.CurrentTime && wholeHash == DynamicSnapshotHash(partitioned.CurrentTime, partitioned.CurrentSnapshot), "frame partition independence"); Check(diagnostics.ScriptedSequenceHash == partitionDiagnostics.ScriptedSequenceHash, "restart scripted sequence repeatability");
    var root = new ReferenceFrameId(1); var initialCameraRoot = new UniversePosition(new Double3(50, 16, 70), root); VerifyFixtureViewport(scene.CurrentSnapshot.Objects, root, initialCameraRoot, 16d / 9d, 2560, 1440, "dynamic 16:9");
    ulong scriptedHash = 14695981039346656037UL;
    foreach (var seconds in new long[] { 0, 1, 5, 10, 100 })
    {
        var time = SimulationInstant.FromWholeSeconds(seconds);
        Check(scene.TryBuildCandidateForTest(time, out var scriptedCandidate, out var scriptedError) && scriptedCandidate is not null, $"scripted candidate {seconds}: {scriptedError}");
        var snapshotHash = DynamicSnapshotHash(time, scriptedCandidate!);
        scriptedHash = Mix(Mix(scriptedHash, (ulong)time.Ticks), FixtureSetupHash(scriptedCandidate!.Objects));
        Console.WriteLine($"Dynamic snapshot hash t={seconds}s: 0x{snapshotHash:X16}");
    }
    Check(scriptedHash == diagnostics.ScriptedSequenceHash, "scripted snapshot sequence hash");
    Check(DynamicReferenceFrameFixtureScene.TryCreate(out var sequencePublication, out _, out var sequencePublicationError) && sequencePublication is not null, $"sequence publication fixture: {sequencePublicationError}");
    var beforeSequencePublication = GC.GetAllocatedBytesForCurrentThread();
    foreach (var duration in new long[] { 1, 4, 5, 90 }) Check(sequencePublication!.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(duration), out var sequenceAdvanceError), $"sequence publication advance: {sequenceAdvanceError}");
    var sequencePublicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforeSequencePublication;
    Check(sequencePublicationBytes > 0 && sequencePublication!.CurrentTime == SimulationInstant.FromWholeSeconds(100), "scripted immutable publication allocations measured");
    Console.WriteLine($"Dynamic scripted publication allocations: {sequencePublicationBytes} bytes/4 updates");
    var publication = partitioned; _ = publication.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out _); var beforePublication = GC.GetAllocatedBytesForCurrentThread(); const int publicationIterations = 100; for (var index = 0; index < publicationIterations; index++) Check(publication.TryAdvanceByHostDuration(SimulationDuration.FromTicks(10_000), out var publicationError), $"publication: {publicationError}"); var publicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforePublication; Check(publicationBytes > 0 && publication.GraphConstructionCount == 1, "immutable publication allocations measured without topology rebuild"); Console.WriteLine($"Dynamic publication allocations: {publicationBytes / publicationIterations} bytes/update ({publicationBytes} bytes/{publicationIterations} updates)");
    var cameraRoot = new UniversePosition(new Double3(50, 16, 70), new ReferenceFrameId(1)); var frame = new RenderFrameSubmission(4); var camera = Camera(cameraRoot); Check(ResolvedRenderSubmissionBuilder.TryBuild(publication.CurrentSnapshot, camera, cameraRoot, frame) == ResolvedRenderSubmissionBuildStatus.Success, "dynamic frame setup"); beforePublication = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(publication.CurrentSnapshot, camera, cameraRoot, frame) == ResolvedRenderSubmissionBuildStatus.Success, "warm dynamic assembly"); Check(GC.GetAllocatedBytesForCurrentThread() == beforePublication, "warm dynamic frame assembly allocates zero bytes");
    Console.WriteLine($"Dynamic scripted-sequence hash: 0x{diagnostics.ScriptedSequenceHash:X16}");
}
static void CelestialAnalyticalFixturePublicationTest()
{
    Check(CelestialAnalyticalScene.TryCreate(out var scene, out var createError) && scene is not null, $"celestial scene creation: {createError}");
    Check(scene!.CurrentTime == SimulationInstant.Zero && scene.CurrentSnapshot.Count == 3 && scene.CurrentSnapshot.OrbitCurve?.Count == 257 && scene.CurrentSnapshot.PreviousOrbitCurve is null && scene.CurrentSnapshot.Objects[2].Scale == Double3.Zero && scene.OrbitCurveBuildCount == 1, "celestial initial snapshot and curve");
    var initialAttitude = scene.CurrentSnapshot.Objects[1].RootOrientation;
    Check(scene.CurrentSnapshot.Objects[0].RootPosition.Value == Double3.Zero, "celestial root marker identity");
    Check(Math.Abs(scene.CurrentSnapshot.Objects[1].RootPosition.Value.X - 10d) < 1e-12d && scene.CurrentSnapshot.Objects[1].RootPosition.Value.Y == 0d, "SI presentation scaling");
    var root = new ReferenceFrameId(1); var celestialCamera = CelestialAnalyticalScene.Camera; var presentationCamera = new CameraState(new FramePosition(root, celestialCamera.Position), DoubleQuaternion.Identity, celestialCamera.Projection, CameraMode.Free); var initialDistance = scene.OrbitDistance;
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = 1 }, out var rateChanged, out var pauseChanged); Check(!rateChanged && !pauseChanged && scene.OrbitDistance < initialDistance, "positive wheel zooms nearer");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _); Check(Math.Abs(scene.OrbitDistance - initialDistance) < 1e-12d, "negative wheel zooms farther");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = 100 }, out _, out _); Check(scene.OrbitDistance == 2d, "minimum zoom clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { MouseWheelDetents = -200 }, out _, out _); Check(scene.OrbitDistance == 500d, "maximum zoom clamp"); scene.ResetPresentationCamera(presentationCamera);
    scene.ResetPresentationCamera(presentationCamera); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaX = 10 }, out _, out _); Check(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).X > 0d, "right drag orbits right");
    scene.ResetPresentationCamera(presentationCamera); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaY = -10 }, out _, out _); Check(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).Y > 0d, "up drag orbits up");
    scene.ApplyPresentationInput(presentationCamera, new NativeInputState { LookActive = 1, MouseDeltaY = -1_000_000 }, out _, out _); Check(Math.Abs(presentationCamera.Orientation.Rotate(new Double3(0, 0, -1)).Y) < 1d, "orbit pitch clamp");
    var immutableBeforeControls = scene.CurrentSnapshot; var curveBuildsBeforeControls = scene.OrbitCurveBuildCount; scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out rateChanged, out pauseChanged); Check(rateChanged && !pauseChanged && scene.Rate == new SimulationRate(5_000, 1), "rate decrease step"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { PauseToggle = 1 }, out rateChanged, out pauseChanged); Check(!rateChanged && pauseChanged && scene.IsPaused && scene.CurrentTime == SimulationInstant.Zero && ReferenceEquals(immutableBeforeControls, scene.CurrentSnapshot), "pause is presentation input only"); Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1), out var pausedError) && scene.CurrentTime == SimulationInstant.Zero && scene.CurrentSnapshot.Objects[1].RootOrientation == initialAttitude, $"pause freezes attitude: {pausedError}"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { PauseToggle = 1 }, out _, out _); Check(!scene.IsPaused, "resume toggle"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out rateChanged, out _); Check(rateChanged && scene.Rate == new SimulationRate(10_000, 1), "rate increase step"); for (var index = 0; index < 6; index++) scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out _, out _); Check(scene.Rate == SimulationRate.One, "1x lower clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out rateChanged, out _); Check(!rateChanged && scene.Rate == SimulationRate.One, "1x remains clamped"); for (var index = 0; index < 6; index++) scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out _, out _); Check(scene.Rate == new SimulationRate(50_000, 1), "50000x upper clamp"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateIncrease = 1 }, out rateChanged, out _); Check(!rateChanged && scene.Rate == new SimulationRate(50_000, 1), "50000x remains clamped"); scene.ApplyPresentationInput(presentationCamera, new NativeInputState { RateDecrease = 1 }, out _, out _); Check(scene.Rate == new SimulationRate(10_000, 1) && scene.OrbitCurveBuildCount == curveBuildsBeforeControls && scene.CurrentSnapshot.Objects[1].RootOrientation == initialAttitude, "camera/rate input does not alter attitude without time advancement");
    var retained = scene.CurrentSnapshot; var retainedHash = DynamicSnapshotHash(scene.CurrentTime, retained);
    Check(!scene.TryPublishCandidateForTest(true, out _), "celestial controlled candidate rejection"); Check(ReferenceEquals(retained, scene.CurrentSnapshot) && DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot) == retainedHash, "celestial rejection retains prior snapshot");
    Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(9.9999d), out var preImpulseError), $"celestial pre-impulse advance: {preImpulseError}"); var beforeImpulse = scene.CurrentSnapshot; Check(beforeImpulse.Objects[1].RootOrientation == initialAttitude, "stationary fixture remains stationary without player torque");
    var initialCurve = beforeImpulse.OrbitCurve; var beforeImpulseAllocation = GC.GetAllocatedBytesForCurrentThread(); Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(.0001d), out var impulseError), $"celestial impulse advance: {impulseError}"); var impulseCurveBytes = GC.GetAllocatedBytesForCurrentThread() - beforeImpulseAllocation; Check(scene.CurrentTime == SimulationInstant.FromWholeSeconds(100_000) && !ReferenceEquals(beforeImpulse, scene.CurrentSnapshot) && scene.OrbitCurveBuildCount == 2 && !ReferenceEquals(initialCurve, scene.CurrentSnapshot.OrbitCurve) && ReferenceEquals(initialCurve, scene.CurrentSnapshot.PreviousOrbitCurve) && scene.CurrentSnapshot.Objects[2].Scale.X > 0d && impulseCurveBytes > 0, "canonical impulse publication includes one ghost and burn marker");
    var hash = DynamicSnapshotHash(scene.CurrentTime, scene.CurrentSnapshot); var activeOrbitHash = OrbitHash(scene.CurrentSnapshot.OrbitCurve!); var ghostOrbitHash = OrbitHash(scene.CurrentSnapshot.PreviousOrbitCurve!); var burnHash = MixDouble3(14695981039346656037UL, scene.CurrentSnapshot.Objects[2].RootPosition.Value); Check(activeOrbitHash != ghostOrbitHash, "active and ghost curves differ"); Check(CelestialAnalyticalScene.TryCreate(out var replay, out var replayError) && replay is not null, $"celestial replay creation: {replayError}"); Check(replay!.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(10), out var replayAdvanceError), $"celestial replay advance: {replayAdvanceError}"); Check(hash == DynamicSnapshotHash(replay.CurrentTime, replay.CurrentSnapshot), "celestial exact-time replay");
    var cameraRoot = new UniversePosition(new Double3(0, 0, 24), root); var camera = Camera(cameraRoot); var submission = new RenderFrameSubmission(3, 257); Check(ResolvedRenderSubmissionBuilder.TryBuild(scene.CurrentSnapshot, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success && submission.PreviousOrbitVertexCount == 257, "celestial submission");
    var beforeSubmission = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(ResolvedRenderSubmissionBuilder.TryBuild(scene.CurrentSnapshot, camera, cameraRoot, submission) == ResolvedRenderSubmissionBuildStatus.Success, "warm celestial submission"); Check(GC.GetAllocatedBytesForCurrentThread() == beforeSubmission, "warm celestial submission allocation");
    _ = scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1), out _); var beforePublication = GC.GetAllocatedBytesForCurrentThread(); const int publicationIterations = 20; for (var index = 0; index < publicationIterations; index++) Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromTicks(1_000), out var publicationError), $"celestial publication: {publicationError}"); var publicationBytes = GC.GetAllocatedBytesForCurrentThread() - beforePublication; Check(publicationBytes > 0, "celestial immutable publication allocation measured"); Console.WriteLine($"Celestial fixture snapshot hash: 0x{hash:X16}; active=0x{activeOrbitHash:X16}; ghost=0x{ghostOrbitHash:X16}; burn=0x{burnHash:X16}; curve replacement/publication allocations: {impulseCurveBytes} bytes; unchanged publication allocations: {publicationBytes / publicationIterations} bytes/update ({publicationBytes} bytes/{publicationIterations} updates)");
}
static void VerifyFixtureViewport(ReadOnlySpan<ResolvedRenderObject> objects, ReferenceFrameId root, UniversePosition cameraRoot, double aspect, int width, int height, string label)
{
    var frames = new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root, null, ReferenceFrameKind.Ecl, "root"), CelestialFrameFactory.RootEcl())]); var resolver = new ReferenceFrameResolver(frames);
    var projection = new CameraProjection(Math.PI / 3d, aspect, .01d, 1000d); projection.Validate();
    var state = new CameraState(new FramePosition(root, cameraRoot.Value), DoubleQuaternion.Identity, projection, CameraMode.Free);
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var camera, out var resolvedCamera, out _), $"{label} fixture camera"); Check(resolvedCamera == cameraRoot, $"{label} camera root");
    Span<ProjectedBounds> projected = stackalloc ProjectedBounds[4];
    for (var index = 0; index < objects.Length; index++)
    {
        projected[index] = ProjectBounds(objects[index], camera, cameraRoot.Value);
        Check(projected[index].CenterX is > -.9d and < .9d && projected[index].CenterY is > -.9d and < .9d, $"{label} marker center inside viewport");
        Check(projected[index].MinX > -1d && projected[index].MaxX < 1d && projected[index].MinY > -1d && projected[index].MaxY < 1d, $"{label} marker bounds inside viewport");
        var pixelHeight = (projected[index].MaxY - projected[index].MinY) * height * .5d;
        Check(pixelHeight >= 18d, $"{label} marker visibility threshold"); Check(pixelHeight <= height * .25d, $"{label} marker maximum size");
    }
    var dx = (projected[2].CenterX - projected[3].CenterX) * width * .5d; var dy = (projected[2].CenterY - projected[3].CenterY) * height * .5d; var separation = Math.Sqrt(dx * dx + dy * dy);
    Check(separation >= 30d, $"{label} Moon/Vessel separation"); var minHeight=Math.Min(Math.Min(projected[0].PixelHeight(height), projected[1].PixelHeight(height)), Math.Min(projected[2].PixelHeight(height), projected[3].PixelHeight(height))); var maxHeight=Math.Max(Math.Max(projected[0].PixelHeight(height), projected[1].PixelHeight(height)), Math.Max(projected[2].PixelHeight(height), projected[3].PixelHeight(height))); Console.WriteLine($"Fixture {label}: minHeight={minHeight:F1}px, maxHeight={maxHeight:F1}px, Moon/Vessel={separation:F1}px");
}
static ProjectedBounds ProjectBounds(in ResolvedRenderObject value, in GpuCameraData camera, in Double3 cameraRoot)
{
    ReadOnlySpan<Double3> vertices = [new(0,-.04,0), new(.04,.04,0), new(-.04,.04,0)]; var relative = CameraRelativeRenderPosition.Create(value.RootPosition.Value,cameraRoot).Value;
    var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity; var minY = double.PositiveInfinity; var maxY = double.NegativeInfinity;
    foreach (ref readonly var vertex in vertices)
    {
        var local = value.RootOrientation.Rotate(new Double3(vertex.X * value.Scale.X, vertex.Y * value.Scale.Y, vertex.Z * value.Scale.Z)); var point = local + relative;
        var x = camera.ViewProjection.C0R0 * point.X + camera.ViewProjection.C1R0 * point.Y + camera.ViewProjection.C2R0 * point.Z + camera.ViewProjection.C3R0;
        var y = camera.ViewProjection.C0R1 * point.X + camera.ViewProjection.C1R1 * point.Y + camera.ViewProjection.C2R1 * point.Z + camera.ViewProjection.C3R1;
        var w = camera.ViewProjection.C0R3 * point.X + camera.ViewProjection.C1R3 * point.Y + camera.ViewProjection.C2R3 * point.Z + camera.ViewProjection.C3R3;
        var ndcX = x / w; var ndcY = y / w; minX = Math.Min(minX, ndcX); maxX = Math.Max(maxX, ndcX); minY = Math.Min(minY, ndcY); maxY = Math.Max(maxY, ndcY);
    }
    return new ProjectedBounds(minX, maxX, minY, maxY);
}
static ResolvedRenderObject Object(uint id, UniversePosition position, MeshHandle mesh) => new(new RenderObjectId(id), position, DoubleQuaternion.Identity, new Double3(1, 1, 1), mesh);
static GpuCameraData Camera(in UniversePosition position) => new() { Position = EncodedPosition.Encode(position.Value), ViewProjection = new Float4x4 { C0R0 = 1, C1R1 = 1, C2R2 = 1, C3R3 = 1 } };
static ulong TransportHash(RenderFrameSubmission submission) { ulong hash = 14695981039346656037; foreach (ref readonly var value in submission.Objects) { hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.LowX)); hash = Mix(hash, value.Mesh.Value); } foreach (ref readonly var batch in submission.Batches) { hash = Mix(hash, batch.Mesh.Value); hash = Mix(hash, batch.FirstObject); hash = Mix(hash, batch.ObjectCount); } return hash; }
static ulong CameraHash(in GpuCameraData camera) { ulong hash = 14695981039346656037; hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C0R0)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C1R1)); return Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C2R2)); }
static ulong FixtureSetupHash(ReadOnlySpan<ResolvedRenderObject> objects) { ulong hash = 14695981039346656037UL; foreach (ref readonly var value in objects) { hash = Mix(hash, value.Id.Value); hash = Mix(hash, (ulong)value.RootPosition.Frame.Value); hash = MixDouble3(hash, value.RootPosition.Value); hash = MixQuaternion(hash, value.RootOrientation); hash = MixDouble3(hash, value.Scale); hash = Mix(hash, value.Mesh.Value); } return hash; }
static ulong DynamicSnapshotHash(SimulationInstant time, ResolvedRenderSnapshot snapshot) { ulong hash = Mix(14695981039346656037UL, (ulong)time.Ticks); return Mix(hash, FixtureSetupHash(snapshot.Objects)); }
static ulong OrbitHash(ResolvedOrbitCurve curve) { ulong hash = 14695981039346656037UL; foreach (ref readonly var position in curve.Positions) hash = MixDouble3(hash, position.Value); return hash; }
static ulong IndicatorHash(in ResolvedDirectionIndicator indicator) => MixDouble3(MixDouble3(14695981039346656037UL, indicator.Start.Value), indicator.End.Value);
static ulong MixDouble3(ulong hash, in Double3 value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); }
static ulong MixQuaternion(ulong hash, in DoubleQuaternion value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.W)); }
static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
static void Throws<T>(Action action) where T : Exception { try { action(); throw new Exception($"Expected {typeof(T).Name}"); } catch (T) { } }
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void CheckNear(in Double3 actual, in Double3 expected, string message) { if ((actual - expected).LengthSquared > 1e-18) throw new Exception(message); }
static Vector3 PlanetDecodeBc5Normal(Vector2 encodedXY)
{
    var xy = encodedXY * 2f - Vector2.One;
    var z = MathF.Sqrt(MathF.Max(0f, 1f - Vector2.Dot(xy, xy)));
    return Vector3.Normalize(new Vector3(xy, z));
}
static Vector3 PlanetMicroBasisEast(Vector3 up)
{
    var basisUp = Vector3.Normalize(up);
    var reference = MathF.Abs(basisUp.Y) < 0.9f ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
    return Vector3.Normalize(Vector3.Cross(reference, basisUp));
}
static Vector3 PlanetMicroBasisNorth(Vector3 up)
{
    var east = PlanetMicroBasisEast(up);
    return Vector3.Normalize(Vector3.Cross(Vector3.Normalize(up), east));
}
static Vector3 ComposeMicroNormal(Vector3 macroNormal, Vector2 encodedMicroXY, float localContribution, float detailStrength)
{
    var up = Vector3.Normalize(macroNormal);
    var micro = PlanetDecodeBc5Normal(encodedMicroXY);
    var east = PlanetMicroBasisEast(up);
    var north = Vector3.Normalize(Vector3.Cross(up, east));
    var microWorld = Vector3.Normalize(east * micro.X + north * micro.Y + up * micro.Z);
    var blend = MathF.Min(MathF.Max(localContribution * detailStrength, 0f), 1f);
    return Vector3.Normalize(Vector3.Lerp(up, microWorld, blend));
}
readonly record struct ProjectedBounds(double MinX, double MaxX, double MinY, double MaxY)
{
    public double CenterX => (MinX + MaxX) * .5d;
    public double CenterY => (MinY + MaxY) * .5d;
    public double PixelHeight(int height) => (MaxY - MinY) * height * .5d;
}
readonly record struct SasConvergenceMetrics(double InitialError, double FinalError, double FinalRate, double PeakOvershoot, int Crossings, double SettledSeconds, int TransactionCount, int PostSettleChanges, Double3 RawTorque, Double3 QuantizedTorque);
file sealed class FixedUtcTimeProvider(DateTimeOffset utc) : TimeProvider
{
    public int QueryCount { get; private set; }
    public override DateTimeOffset GetUtcNow() { QueryCount++; return utc; }
}
