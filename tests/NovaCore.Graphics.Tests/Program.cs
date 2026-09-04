using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
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
    ("Planetary surface camera presentation", PlanetarySurfaceCameraPresentationTest),
    ("Earth CPU elevation oracle", EarthElevationOracleTest),
    ("Canonical body-fixed geographic handedness", CanonicalBodyFixedGeographicHandednessTest),
    ("Canonical SurfaceAnchor physical terrain authority", CanonicalSurfaceAnchorPhysicalTerrainAuthorityTest),
    ("Anchored Florida launch site", AnchoredFloridaLaunchSiteTest),
    ("Surface-relative camera authority", SurfaceRelativeCameraAuthorityTest),
    ("Near-surface inertial free-look", NearSurfaceInertialFreeLookRegressionTest),
    ("SurfaceAnchor acquisition, ENU, and handoff", SurfaceAnchorPhaseBTest),
    ("Camera focus-position continuity", CameraFocusPositionContinuityTest),
    ("Camera SurfaceAnchor handoff monotonicity", CameraSurfaceAnchorHandoffMonotonicityTest),
    ("Solar preset camera-path convergence", SolarPresetCameraPathConvergenceTest),
    ("Zoom motion-profile continuity", ZoomMotionProfileContinuityTest),
    ("Solar camera bounded-domain crash regression", SolarCameraBoundedDomainCrashRegressionTest),
    ("Surface visual-aim continuity", SurfaceVisualAimContinuityTest),
    ("Inertial visual-aim authority", InertialVisualAimAuthorityTest),
    ("Cube-sphere planetary surface", CubeSpherePlanetarySurfaceTest),
    ("Production relaxed cube-sphere patch hierarchy", ProductionRelaxedCubeSpherePatchHierarchyTest),
    ("Anchored spherical mesh-tier contract", AnchoredSphericalMeshTierContractTest),
    ("M12D-P2S2 spherical billboard topology proof", PlanetarySphericalBillboardTopologyTests.Run),
    ("M12D-P2S3 spherical billboard GPU runtime proof", PlanetarySphericalBillboardGpuRuntimeTests.Run),
    ("M12D-P2S4 canonical natural terrain billboard binding", PlanetarySphericalBillboardNaturalTerrainTests.Run),
    ("M12D-P2S5C production spherical billboard runtime", PlanetaryProductionSphericalBillboardRuntimeTests.Run),
    ("M12D-P2S5B production spherical billboard topology library", PlanetaryProductionSphericalBillboardTopologyTests.Run),
    ("M12D-P2S5F nested production scale-mesh topology", PlanetaryNestedScaleMeshTopologyTests.Run),
    ("GPU physical-height preparation", GpuPhysicalHeightPreparationTests.Run),
    ("Multiscale physical terrain modifier foundation", PlanetaryPhysicalSurfaceModifierTests.Run),
    ("Single canonical physical surface authority", PlanetaryCanonicalPhysicalSurfaceAuthorityTests.Run),
    ("Global/anchored physical frequency continuity", PlanetaryPhysicalFrequencyContinuityTests.Run),
    ("M12D-P2A canonical hashed cell field proof", PlanetaryNaturalTerrainFieldTests.Run),
    ("M12D-P2B multiscale natural terrain family proof", PlanetaryNaturalTerrainFamiliesTests.Run),
    ("M12D-P2C1 prepared natural terrain", PlanetaryNaturalTerrainPreparationTests.Run),
    ("M12D-P2C2 opt-in candidate renderer", PlanetaryNaturalTerrainRendererIntegrationTests.Run),
    ("Production material noise value preservation", PlanetaryProductionMaterialNoiseTests.Run),
    ("Dynamic anchored production hierarchy", PlanetaryDynamicAnchoredSurfaceTests.Run),
    ("Displaced mesh and physical normals", GpuDisplacedMeshPreparationTests.Run),
    ("Screen-space subdivision", PlanetaryScreenSpaceSubdivisionTests.Run),
    ("Terrain-v5 seams, mixed-LOD authority, and Florida classification", TerrainV5PayloadSeamAndFloridaClassificationTest),
    ("Terrain asset distribution boundary", TerrainAssetDistributionBoundaryTest),
    ("Local terrain streaming and GPU compression", LocalTerrainStreamingAndGpuCompressionTest),
    ("M12 Florida regional physical surface", M12FloridaRegionalPhysicalSurfaceTest),
    ("Production cube-sphere GPU residency integration", ProductionCubeSphereGpuResidencyIntegrationTest),
    ("Production physical-normal tangent continuity", ProductionPhysicalNormalTangentContinuityTest),
    ("Production surface body eligibility and transition ownership", ProductionSurfaceBodyEligibilityAndTransitionOwnershipTest),
    ("Production Earth material-state continuity", ProductionEarthMaterialStateContinuityTest),
    ("Planetary camera terrain exclusion", PlanetaryCameraTerrainExclusionTest),
    ("Close-ground reference-frame diagnostic", CloseGroundReferenceFrameDiagnosticTest),
    ("Production terrain material synthesis and tessellation study", ProductionTerrainMaterialSynthesisAndTessellationStudyTest),
    ("Planetary terrain residency and surface frame", PlanetaryTerrainResidencyAndSurfaceFrameTest),
    ("Planetary patch topology and ABI", PlanetaryPatchTopologyAndAbiTest),
    ("Parent-child LOD geographic correspondence", ParentChildLodGeographicCorrespondenceTest),
    ("Opaque distant-detailed handoff", OpaqueDistantDetailedHandoffTest),
    ("Planetary representation handoff", PlanetaryRepresentationHandoffTest),
    ("Distant quaternion transform parity", DistantQuaternionTransformParityTest),
    ("Distant visible hemisphere winding", DistantVisibleHemisphereWindingTest),
    ("Continuous Earth distance visibility", ContinuousEarthDistanceVisibilityTest),
    ("Camera drag isolation", CameraDragIsolationTest),
    ("Sol system presentation and focus", SolarSystemSceneTest),
    ("SolAnalytical Earth planetary scene", EarthPlanetarySceneTest),
};
var testFilter=args.FirstOrDefault(argument=>argument.StartsWith("--test=",StringComparison.OrdinalIgnoreCase))?[7..];
foreach (var (name, test) in tests) if(testFilter is null||name.Contains(testFilter,StringComparison.OrdinalIgnoreCase)){test();Console.WriteLine($"PASS {name}");}

static void AnchoredSphericalMeshTierContractTest()
{
    const ulong earthBodyId=6;
    var terrainDefinition=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    var terrainVersion=new TerrainAuthorityVersion(terrainDefinition.SourceId,terrainDefinition.Version);
    var root=new ReferenceFrameId(0x7A01);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var sceneValue,out var sceneError)&&sceneValue is not null,$"anchored mesh scene: {sceneError}");
    var scene=sceneValue!;var site=scene.FloridaLaunchSite;var florida=site.Object.Anchor;
    Check(site.IsValid&&florida.IsValid&&florida.TerrainAuthorityVersion==terrainVersion,"Florida supplies the existing canonical terrain-v5 SurfaceAnchor");

    Check(PlanetaryAnchoredMeshTierId.TryCreate(florida,2,8,out var tierA),"first deterministic Florida tier identity");
    Check(PlanetaryAnchoredMeshTierId.TryCreate(florida,2,8,out var tierB)&&tierA==tierB&&
        tierA.DeterministicHash==tierB.DeterministicHash,"same body/terrain/tier/anchor produces one deterministic identity");
    var anchorRoundTrip=RelaxedCubeSphereProjection.UnitDirection(tierA.Anchor.Face,tierA.Anchor.U,tierA.Anchor.V);
    var anchorRoundTripError=Math.Sqrt((anchorRoundTrip-florida.NormalizedBodyFixedDirection).LengthSquared);
    Check(anchorRoundTripError<=PlanetaryAnchoredMeshAnchor.MaximumRoundTripDirectionError,"canonical body-fixed anchor round trip stays within its declared numerical contract");

    var topologyA=PlanetaryAnchoredMeshReferenceTopology.Create(tierA);
    var topologyB=PlanetaryAnchoredMeshReferenceTopology.Create(tierB);
    Check(topologyA.Vertices.SequenceEqual(topologyB.Vertices)&&topologyA.Indices.SequenceEqual(topologyB.Indices)&&
        topologyA.DeterministicHash==topologyB.DeterministicHash&&topologyA.Vertices.Length==25&&topologyA.Indices.Length==96,
        "bounded reference topology has deterministic vertex/index identity and intentionally non-production 4x4 density");
    Check(tierA.DeterministicHash==0x11D71BDA21DAECC1ul&&topologyA.DeterministicHash==0x620D695AB3D18F5Dul,
        $"anchored tier and bounded reference topology regression hashes bind M12 physical height without changing topology: tier=0x{tierA.DeterministicHash:X16}; topology=0x{topologyA.DeterministicHash:X16}");

    var splitSamples=new[]{
        Double3.Zero,
        new Double3(6_378_137d,0d,0d),
        florida.NormalizedBodyFixedDirection*site.LocalPhysicalSurfaceRadiusMetres,
        RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveX,0d,.5d)*6_378_137d,
        florida.NormalizedBodyFixedDirection*(site.LocalPhysicalSurfaceRadiusMetres+10.001d),
        new Double3(149_597_870_700.12345d,-28_456_789_012.875d,7_500_000.03125d)};
    var maximumSplitError=0d;
    foreach(var sample in splitSamples)
    {
        var reconstructed=EncodedPosition.Encode(sample).Reconstruct();
        maximumSplitError=Math.Max(maximumSplitError,Math.Max(Math.Abs(sample.X-reconstructed.X),Math.Max(Math.Abs(sample.Y-reconstructed.Y),Math.Abs(sample.Z-reconstructed.Z))));
    }
    var splitAnchor=PlanetaryAnchoredMeshSplitAnchor.Encode(tierA.Anchor,site.LocalPhysicalSurfaceRadiusMetres);
    maximumSplitError=Math.Max(maximumSplitError,Math.Sqrt((splitAnchor.BodyFixedPosition.Reconstruct()-florida.NormalizedBodyFixedDirection*site.LocalPhysicalSurfaceRadiusMetres).LengthSquared));
    Check(maximumSplitError<=1e-4d&&splitAnchor.BodyFixedDirection.Reconstruct().IsFinite,"split FP32 high+low transport stays within the declared 0.1 mm absolute envelope through Solar-root scale");

    PlanetaryAnchoredMeshTierId TierForCell(CubeSphereFace face,int level,int x,int y)
    {
        var size=1<<level;var direction=RelaxedCubeSphereProjection.UnitDirection(face,(x+.5d)/size,(y+.5d)/size);
        Check(SurfaceAnchor.TryCreate(earthBodyId,terrainVersion,direction,0d,out var anchor)==SurfaceAnchorCreationStatus.Success,$"anchor for {face}/{level}/{x}/{y}");
        Check(PlanetaryAnchoredMeshTierId.TryCreate(anchor,level,level,out var identity)&&identity.AnchorCell.Face==face&&identity.AnchorCell.Level==level&&identity.AnchorCell.X==x&&identity.AnchorCell.Y==y,$"tier cell for {face}/{level}/{x}/{y}");
        return identity;
    }

    var sameFaceLeft=PlanetaryAnchoredMeshReferenceTopology.Create(TierForCell(CubeSphereFace.PositiveZ,2,1,1));
    var sameFaceRight=PlanetaryAnchoredMeshReferenceTopology.Create(TierForCell(CubeSphereFace.PositiveZ,2,2,1));
    var maximumSharedDirectionError=0d;
    for(var sample=0;sample<=PlanetaryAnchoredMeshReferenceTopology.ProofQuadsPerSide;sample++)
    {
        var left=sameFaceLeft.EdgeVertex(PlanetaryPatchEdge.PositiveU,sample);var right=sameFaceRight.EdgeVertex(PlanetaryPatchEdge.NegativeU,sample);
        Check(left==right,"same-face neighbors derive bit-identical canonical edge samples");
        maximumSharedDirectionError=Math.Max(maximumSharedDirectionError,Math.Sqrt((left.BodyFixedDirection-right.BodyFixedDirection).LengthSquared));
    }
    Check(sameFaceLeft.EdgeIdentity(PlanetaryPatchEdge.PositiveU)==sameFaceRight.EdgeIdentity(PlanetaryPatchEdge.NegativeU),"same-face neighbors derive one canonical ordered edge identity");

    var rootTopologies=new List<PlanetaryAnchoredMeshReferenceTopology>(6);
    foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>()) rootTopologies.Add(PlanetaryAnchoredMeshReferenceTopology.Create(TierForCell(face,0,0,0)));
    var edgeGroups=new Dictionary<PlanetaryAnchoredMeshEdgeId,List<PlanetaryAnchoredMeshVertexId[]>>();
    foreach(var topology in rootTopologies)
        foreach(PlanetaryPatchEdge edge in Enum.GetValues<PlanetaryPatchEdge>().Where(edge=>edge!=PlanetaryPatchEdge.None))
        {
            var id=topology.EdgeIdentity(edge);var samples=new PlanetaryAnchoredMeshVertexId[PlanetaryAnchoredMeshReferenceTopology.ProofQuadsPerSide+1];
            for(var sample=0;sample<samples.Length;sample++)samples[sample]=topology.EdgeVertex(edge,sample);
            if(samples[0]!=id.First)Array.Reverse(samples);
            if(!edgeGroups.TryGetValue(id,out var incidences)){incidences=[];edgeGroups.Add(id,incidences);}incidences.Add(samples);
        }
    Check(edgeGroups.Count==12&&edgeGroups.Values.All(incidences=>incidences.Count==2&&incidences[0].SequenceEqual(incidences[1])),
        "all twelve physical cube edges have exactly two faces with bit-identical canonically ordered sampling coordinates");
    foreach(var incidences in edgeGroups.Values)for(var sample=0;sample<incidences[0].Length;sample++)
        maximumSharedDirectionError=Math.Max(maximumSharedDirectionError,Math.Sqrt((incidences[0][sample].BodyFixedDirection-incidences[1][sample].BodyFixedDirection).LengthSquared));

    var cornerGroups=new Dictionary<PlanetaryAnchoredMeshVertexId,int>();
    foreach(var topology in rootTopologies)
    {
        var resolution=PlanetaryAnchoredMeshReferenceTopology.ProofQuadsPerSide;
        foreach(var index in new[]{0,resolution,resolution*(resolution+1),resolution*(resolution+1)+resolution})
        {
            var id=topology.Vertices[index].Identity;cornerGroups.TryGetValue(id,out var count);cornerGroups[id]=count+1;
            Check(id.Denominator==1&&Math.Abs(id.X)==1&&Math.Abs(id.Y)==1&&Math.Abs(id.Z)==1,"cube corner reduces to one exact rational canonical coordinate");
        }
    }
    Check(cornerGroups.Count==8&&cornerGroups.Values.All(count=>count==3),"all eight cube corners are independently shared by exactly three faces");
    foreach(var topology in rootTopologies)foreach(ref readonly var vertex in topology.Vertices)
        Check(vertex.Identity.TryCanonicalAddress(out _,out var canonicalU,out var canonicalV)&&canonicalU is >=0d and <=1d&&canonicalV is >=0d and <=1d&&
            vertex.Identity.BodyFixedDirection==RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(vertex.Identity.CanonicalCubePoint),
            "each reference vertex has one canonical cube address and face-independent relaxed-cube direction");

    var parent=PlanetaryAnchoredMeshReferenceTopology.Create(TierForCell(CubeSphereFace.NegativeY,0,0,0));
    var child=PlanetaryAnchoredMeshReferenceTopology.Create(TierForCell(CubeSphereFace.NegativeY,1,0,0));
    for(var y=0;y<=2;y++)for(var x=0;x<=2;x++)
        Check(parent.Vertices[y*5+x].Identity==child.Vertices[(y*2)*5+x*2].Identity,"parent/child tier-local shared samples retain exact rational identity");

    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    var geographicHash=tierA.DeterministicHash;var topologyHash=topologyA.DeterministicHash;var anchorBefore=tierA.Anchor;
    foreach(var focus in new[]{NativePresentationFocus.Sun,NativePresentationFocus.Venus,NativePresentationFocus.Mars,NativePresentationFocus.Moon,NativePresentationFocus.Earth})
    {
        Check(scene.Focus(camera,focus),$"camera-independence focus {focus}");
        var orbitScale=1d+(int)focus*.125d;
        camera.Position=new FramePosition(root,camera.Position.Value+new Double3(123_456.75d*orbitScale,-87_654.5d,12_345.25d/orbitScale));
        camera.Orientation=(DoubleQuaternion.FromAxisAngle(Double3.UnitY,.173d*(int)focus)*
            DoubleQuaternion.FromAxisAngle(Double3.UnitX,-.097d*(int)focus)).Normalized();
        camera.Validate();
        Check(PlanetaryAnchoredMeshTierId.TryCreate(florida,2,8,out var replay)&&replay.DeterministicHash==geographicHash&&
            PlanetaryAnchoredMeshReferenceTopology.Create(replay).DeterministicHash==topologyHash&&replay.Anchor==anchorBefore,
            $"camera orbit/distance/free-look/focus state is absent from anchored geography for {focus}");
    }

    Check(SurfaceEnuFrame.TryCreate(florida,out var enuBefore)&&SurfaceEnuFrame.TryCreate(tierA.Anchor.SurfaceAnchor,out var enuAfter)&&enuBefore==enuAfter,"new mesh anchor preserves the exact existing Florida ENU authority");
    var physicalTerrain=new PlanetaryPhysicalTerrainAuthority(earthBodyId,terrainDefinition);
    Check(physicalTerrain.TrySampleHeight(earthBodyId,florida.NormalizedBodyFixedDirection,out var heightBefore)&&
        physicalTerrain.TrySampleHeight(earthBodyId,tierA.Anchor.BodyFixedDirection,out var heightAfter)&&
        BitConverter.DoubleToInt64Bits(heightBefore)==BitConverter.DoubleToInt64Bits(heightAfter),"new tier correspondence leaves Florida physical terrain query authority bit-identical");
    var floridaMetres=anchorRoundTripError*site.LocalPhysicalSurfaceRadiusMetres;
    Check(floridaMetres<2e-5d,"Florida SurfaceAnchor and mesh-anchor geography agree within the measured relaxed-cube inverse tolerance");

    var sharedEdge=sameFaceLeft.EdgeIdentity(PlanetaryPatchEdge.PositiveU);
    var subdivisionA=PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(sharedEdge,73.125d,12d,64);
    var subdivisionB=PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(sameFaceRight.EdgeIdentity(PlanetaryPatchEdge.NegativeU),73.125d,12d,64);
    Check(subdivisionA==subdivisionB&&subdivisionA.BoundedFactor==7,"backend-neutral quantized projected demand resolves one bounded shared-edge factor");

    Check(PlanetaryPatchTopology.Shared.DeterministicHash==0x98792D7EBC45FF6Dul&&
        PlanetaryProductionPatchTopology.Shared.DeterministicHash==0x61C28B0A3B4F21FFul,
        "11B-6C live grid and production relaxed-cube topology hashes remain unchanged");
    Check(topologyA.DeterministicHash==topologyB.DeterministicHash,"anchored proof topology hash is repeatable");
    Console.WriteLine($"Anchored mesh tier: identity=0x{tierA.DeterministicHash:X16}; topology=0x{topologyA.DeterministicHash:X16}; anchorRoundTrip={anchorRoundTripError:E17}; florida={floridaMetres:E17} m; splitMax={maximumSplitError:E17} m; sharedDirection={maximumSharedDirectionError:E17}; cubeEdges={edgeGroups.Count}; cubeCorners={cornerGroups.Count}; subdivision={subdivisionA.BoundedFactor}; nativeAbi=unchanged; liveRenderer=unchanged");
}

static void TerrainAssetDistributionBoundaryTest()
{
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var productionManifestPath=TerrainAssetRepository.ManifestPath(repositoryRoot,TerrainAssetCache.ProductionEarthAssetId);
    Check(TerrainAssetManifestFile.TryLoad(productionManifestPath,out var production,out var productionError),$"production terrain manifest: {productionError}");
    Check(production.AssetId=="earth-surface-v5"&&production.BodyId==6&&production.TerrainVersion==5&&production.ByteSize==61_484_224&&production.Sha256=="38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae"&&production.Hierarchy.MinimumPayloadLevel==0&&production.Hierarchy.MaximumPayloadLevel==2,"production distribution manifest preserves canonical terrain-v5 identity and global coverage");

    var fixtureRoot=Path.Combine(repositoryRoot,"tests","fixtures","terrain");
    Check(TerrainAssetManifestFile.TryLoad(Path.Combine(fixtureRoot,"tiny-global.json"),out var fixture,out var fixtureError),$"fixture terrain manifest: {fixtureError}");
    var fixtureSource=Path.Combine(fixtureRoot,"tiny-global.nccube");
    var testRoot=Path.Combine(Path.GetTempPath(),"NovaCore-TerrainAssetTests",Guid.NewGuid().ToString("N"));
    var cacheRoot=Path.Combine(testRoot,"cache");
    try
    {
        var missingPath=TerrainAssetCache.ContentPath(cacheRoot,fixture);var missing=TerrainAssetCache.Verify(fixture,missingPath);
        Check(missing.Status==TerrainAssetVerificationStatus.Missing,"empty cache reports fixture missing");
        Directory.CreateDirectory(Path.GetDirectoryName(missingPath)!);File.WriteAllBytes(missingPath+".incomplete-interrupted",[1,2,3,4]);File.WriteAllBytes(Path.Combine(cacheRoot,"abandoned-download.nccube.incomplete"),[1,2,3,4]);
        Check(TerrainAssetCache.Verify(fixture,missingPath).Status==TerrainAssetVerificationStatus.Missing,"incomplete acquisition is never exposed as a valid cache entry");
        Check(TerrainAssetCache.RemoveStaleIncompleteFiles(cacheRoot,TimeSpan.Zero)==2,"explicit stale-incomplete cleanup removes interrupted publication and acquisition files safely");
        var published=TerrainAssetCache.PublishFromFile(fixture,fixtureSource,cacheRoot);
        Check(published.IsValid&&published.ActualBytes==5_032&&published.ActualSha256==fixture.Sha256&&published.MaximumBufferBytes==TerrainAssetCache.VerificationBufferBytes,"atomic fixture publication verifies size and streaming SHA-256");
        Check(!Directory.EnumerateFiles(cacheRoot,"*.incomplete-*",SearchOption.AllDirectories).Any(),"successful atomic publication leaves no incomplete files");
        Check(NativeRuntime.ValidateTerrainAsset(published.Path,fixture.BodyId,fixture.TerrainVersion,(uint)fixture.Hierarchy.RecordCount)==NativeResult.Success,"native runtime opens and digest-validates a payload through the same resolved fixture path");

        using(var stream=new FileStream(published.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){stream.Position=stream.Length-1;var value=stream.ReadByte();stream.Position=stream.Length-1;stream.WriteByte((byte)(value^1));}
        Check(!TerrainAssetCache.Verify(fixture,published.Path).IsValid,"invalid cache occupant is never trusted by its filename");
        Check(TerrainAssetCache.PublishFromFile(fixture,fixtureSource,cacheRoot).IsValid,"atomic publication repairs an invalid content-address occupant with verified identical bytes");

        var corrupt=Path.Combine(testRoot,"corrupt.nccube");File.Copy(fixtureSource,corrupt);using(var stream=new FileStream(corrupt,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){stream.Position=stream.Length-1;var value=stream.ReadByte();stream.Position=stream.Length-1;stream.WriteByte((byte)(value^1));}
        Check(TerrainAssetCache.Verify(fixture,corrupt).Status==TerrainAssetVerificationStatus.HashMismatch,"byte-corrupt fixture is rejected");
        var truncated=Path.Combine(testRoot,"truncated.nccube");using(var source=File.OpenRead(fixtureSource))using(var destination=File.Create(truncated)){source.CopyTo(destination);destination.SetLength(destination.Length-1);}
        Check(TerrainAssetCache.Verify(fixture,truncated).Status==TerrainAssetVerificationStatus.SizeMismatch,"truncated fixture is rejected before publication");

        var productionCorruptRoot=Path.Combine(testRoot,"production-corrupt");var productionCorrupt=TerrainAssetCache.ContentPath(productionCorruptRoot,production);Directory.CreateDirectory(Path.GetDirectoryName(productionCorrupt)!);using(var stream=File.Create(productionCorrupt)){stream.SetLength(production.ByteSize);}
        Check(!TerrainAssetCache.Verify(production,productionCorrupt).IsValid,"corrupt production-sized asset is rejected without becoming resident");

        Directory.Delete(cacheRoot,true);Check(TerrainAssetCache.Verify(fixture,missingPath).Status==TerrainAssetVerificationStatus.Missing,"cache deletion leaves source manifest valid and reports missing");
        var recovered=TerrainAssetCache.PublishFromFile(fixture,fixtureSource,cacheRoot);Check(recovered.IsValid,"disposable cache recovers through explicit atomic acquisition");
        Check(TerrainAssetCache.ContentPath(cacheRoot,fixture).Contains(Path.Combine("sha256",fixture.Sha256[..2],fixture.Sha256+".nccube"),StringComparison.Ordinal),"content address permits identical manifests to share immutable bytes");
    }
    finally
    {
        if(Directory.Exists(testRoot))Directory.Delete(testRoot,true);
    }

    var configuredCache=TerrainAssetRepository.CacheRoot(repositoryRoot);
    var productionPath=TerrainAssetCache.ContentPath(configuredCache,production);var productionVerification=TerrainAssetCache.Verify(production,productionPath);
    if(productionVerification.IsValid)Check(productionVerification.MaximumBufferBytes==1_048_576&&productionVerification.ActualBytes==61_484_224,"production verification is canonical and bounded to one MiB independently of asset size");
    Check(File.ReadAllText(Path.Combine(repositoryRoot,"samples","NovaCore.Triangle","NovaCore.Triangle.csproj")).Contains("earth_surface_v5.nccube",StringComparison.Ordinal)==false,"normal managed builds do not copy production NCCUBE payloads");
    Check(File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp")).Contains("ModuleDirectory()+\"earth-data\\\\earth_surface_v5.nccube\"",StringComparison.Ordinal)==false,"native runtime consumes the explicitly resolved verified path rather than a module-relative copy");
    Console.WriteLine($"Terrain asset boundary: fixture={fixture.ByteSize}B/{fixture.Sha256}; production={productionVerification.Status}; buffer={TerrainAssetCache.VerificationBufferBytes}B; cache={configuredCache}");
}

static void LocalTerrainStreamingAndGpuCompressionTest()
{
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var fixtureRoot=Path.Combine(repositoryRoot,"tests","fixtures","terrain");
    var fixturePath=Path.Combine(fixtureRoot,"tiny-local.nccube");
    Check(TerrainAssetManifestFile.TryLoad(Path.Combine(fixtureRoot,"tiny-local.json"),out var manifest,out var manifestError),$"local fixture manifest: {manifestError}");
    var verification=TerrainAssetCache.Verify(manifest,fixturePath);
    Check(verification.IsValid&&verification.ActualBytes==697_539&&verification.ActualSha256=="6fea3a62833e8aa6beffcbd845697baaca2a8db281eab2ed900d53b2657cf053","NCCUBE2 fixture manifest, size, and content identity");

    var package=File.ReadAllBytes(fixturePath);
    Check(PlanetaryLocalTerrainPackContract.TryReadHeader(package,out var header)&&header.RecordCount==4&&header.MinimumSectorLevel==4&&header.MaximumSectorLevel==4,"NCCUBE2 sparse hierarchy header");
    var sectors=new PlanetaryLocalTerrainSectorId[header.RecordCount];
    var records=new PlanetaryLocalTerrainRecordHeader[header.RecordCount];
    var offset=PlanetaryLocalTerrainPackContract.HeaderBytes;
    for(var index=0;index<records.Length;index++)
    {
        Check(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(package.AsSpan(offset,PlanetaryLocalTerrainPackContract.RecordHeaderBytes),out records[index]),$"NCCUBE2 record {index}");
        sectors[index]=records[index].Sector;
        Check(records[index].PayloadOffset==(ulong)(offset+PlanetaryLocalTerrainPackContract.RecordHeaderBytes),$"NCCUBE2 sequential payload {index}");
        Check(records[index].GpuAlbedoBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.Bc7Srgb,PlanetaryLocalTerrainPackContract.StoredExtent)&&
              records[index].GpuElevationBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.Bc4Unorm,PlanetaryLocalTerrainPackContract.StoredExtent)&&
              records[index].GpuNormalBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.Bc5Unorm,PlanetaryLocalTerrainPackContract.StoredExtent),$"record {index} uses BC7/BC4/BC5 GPU bytes");
        offset=checked((int)records[index].PayloadOffset+(int)records[index].StoredAlbedoBytes+(int)records[index].StoredElevationBytes+(int)records[index].StoredNormalBytes);
    }
    Check(offset==package.Length&&sectors.Distinct().Count()==sectors.Length&&sectors.SequenceEqual(sectors.Order()),"NCCUBE2 records exactly cover file in deterministic identity order");

    static byte[] DecodeChannel(byte[] package,in PlanetaryLocalTerrainRecordHeader record,int channel)
    {
        var storedOffset=checked((int)record.PayloadOffset+(channel==0?0:checked((int)record.StoredAlbedoBytes+(channel==1?0:(int)record.StoredElevationBytes))));
        var storedBytes=channel switch{0=>(int)record.StoredAlbedoBytes,1=>(int)record.StoredElevationBytes,_=>(int)record.StoredNormalBytes};
        var gpuBytes=channel switch{0=>(int)record.GpuAlbedoBytes,1=>(int)record.GpuElevationBytes,_=>(int)record.GpuNormalBytes};
        var codec=channel switch{0=>record.AlbedoCodec,1=>record.ElevationCodec,_=>record.NormalCodec};
        var result=new byte[gpuBytes];
        if(codec==PlanetaryLocalTerrainStorageCodec.RawGpuBlocks)package.AsSpan(storedOffset,storedBytes).CopyTo(result);
        else Check(PlanetaryLocalTerrainTranscode.TryDecodePackBits(package.AsSpan(storedOffset,storedBytes),result,out var written)&&written==gpuBytes,$"PackBits channel {channel} transcode");
        return result;
    }
    var asynchronous=Task.Run(()=>(DecodeChannel(package,records[0],0),DecodeChannel(package,records[0],1),DecodeChannel(package,records[0],2))).GetAwaiter().GetResult();
    Check(asynchronous.Item1.Length==records[0].GpuAlbedoBytes&&asynchronous.Item2.Length==records[0].GpuElevationBytes&&asynchronous.Item3.Length==records[0].GpuNormalBytes,"bounded asynchronous fixture read produces GPU-native BC blocks");
    Check(NativeRuntime.ValidateTerrainAsset(fixturePath,manifest.BodyId,manifest.TerrainVersion,(uint)manifest.Hierarchy.RecordCount)==NativeResult.Success,"native NCCUBE2 parser, transcode, and digest path validates fixture");
    var corruptPath=Path.Combine(Path.GetTempPath(),$"novacore-local-corrupt-{Guid.NewGuid():N}.nccube");
    try
    {
        var corrupt=(byte[])package.Clone();corrupt[checked((int)records[0].PayloadOffset+17)]^=0x5a;File.WriteAllBytes(corruptPath,corrupt);
        Check(NativeRuntime.ValidateTerrainAsset(corruptPath,manifest.BodyId,manifest.TerrainVersion,(uint)manifest.Hierarchy.RecordCount)==NativeResult.Failure,"native local payload digest rejects corrupt sector data");
    }
    finally{if(File.Exists(corruptPath))File.Delete(corruptPath);}
    var missingSector=new PlanetaryLocalTerrainSectorId(6,4,CubeSphereFace.PositiveZ,4,0,0,1,1);
    Check(!sectors.Contains(missingSector),"sparse local package reports geographically absent sectors without inventing unrelated fallback");

    var pupil=RelaxedCubeSphereProjection.UnitDirection(sectors[0].Face,(sectors[0].X+.5)/(1<<sectors[0].Level),(sectors[0].Y+.5)/(1<<sectors[0].Level));
    var orbital=new PlanetaryLocalTerrainDemandInput(6,4,pupil,pupil,pupil,700_000,6_371_008.8,1080,Math.Tan(Math.PI/6));
    Span<PlanetaryLocalTerrainDemand> demand=stackalloc PlanetaryLocalTerrainDemand[64];
    Check(PlanetaryLocalTerrainDemandPlanner.Plan(orbital,sectors,demand)==0,"maximum local detail is not requested at orbital altitude");
    var near=orbital with{SurfaceAltitudeMetres=10_000,PreviousPupilDirection=(pupil+new Double3(.0002,0,0)).Normalized()};
    var demandCount=PlanetaryLocalTerrainDemandPlanner.Plan(near,sectors,demand);
    Check(demandCount>0&&demand[..demandCount].ToArray().All(value=>value.Sector.BodyId==6&&value.Sector.TerrainVersion==4),"near-field demand follows visible and predicted body-fixed footprint");
    var repeated=new PlanetaryLocalTerrainDemand[demand.Length];var repeatedCount=PlanetaryLocalTerrainDemandPlanner.Plan(near,sectors,repeated);
    Check(repeatedCount==demandCount&&repeated.AsSpan(0,repeatedCount).SequenceEqual(demand[..demandCount]),"local demand ordering is deterministic");
    Check(PlanetaryLocalTerrainDemandPlanner.Plan(near with{BodyId=5},sectors,demand)==0,"Earth local package remains isolated from unsupported bodies");
    var opposite=RelaxedCubeSphereProjection.UnitDirection(sectors[^1].Face,(sectors[^1].X+.5)/(1<<sectors[^1].Level),(sectors[^1].Y+.5)/(1<<sectors[^1].Level));
    var reversed=near with{PupilDirection=opposite,PreviousPupilDirection=pupil,ViewDirection=opposite};
    Check(PlanetaryLocalTerrainDemandPlanner.Plan(reversed,sectors,demand)>0,"rapid motion and direction reversal immediately demand the new visible/predicted footprint rather than traversed sectors");
    _=PlanetaryLocalTerrainDemandPlanner.Plan(near,sectors,demand);var allocationStart=GC.GetAllocatedBytesForCurrentThread();
    for(var iteration=0;iteration<1_000;iteration++)_=PlanetaryLocalTerrainDemandPlanner.Plan(near,sectors,demand);
    Check(GC.GetAllocatedBytesForCurrentThread()==allocationStart,"local demand planning is allocation-free on the render path");

    var firstCache=new PlanetaryLocalTerrainCache(128);var secondCache=new PlanetaryLocalTerrainCache(128);
    var firstTokens=new PlanetaryLocalTerrainSlot[128];
    for(var index=0;index<128;index++)
    {
        var id=new PlanetaryLocalTerrainSectorId(6,4,CubeSphereFace.PositiveX,12,index,0,1,1);
        firstTokens[index]=firstCache.Request(id,false);var mirror=secondCache.Request(id,false);
        Check(firstCache.TryBeginRead(firstTokens[index])&&secondCache.TryBeginRead(mirror),$"cache read begins {index}");
        Check(firstCache.TryCompleteRead(firstTokens[index],100,174_240)&&secondCache.TryCompleteRead(mirror,100,174_240),$"cache read completes {index}");
        Check(firstCache.TryPublish(firstTokens[index],174_240)&&secondCache.TryPublish(mirror,174_240),$"cache publishes {index}");
    }
    firstCache.BeginFrame();secondCache.BeginFrame();
    var replacementId=new PlanetaryLocalTerrainSectorId(6,4,CubeSphereFace.PositiveX,12,128,0,1,1);
    var replacement=firstCache.Request(replacementId,true);var replacementMirror=secondCache.Request(replacementId,true);
    Check(replacement.Slot==replacementMirror.Slot&&replacement.Generation==replacementMirror.Generation&&replacement.Slot==0,"deterministic LRU selects the same oldest GPU-safe slot");
    Check(!firstCache.Owns(firstTokens[0])&&!firstCache.TryPublish(firstTokens[0],1),"generation token prevents stale slot publication");
    Check(firstCache.Statistics.Capacity==128&&firstCache.Statistics.Evictions==1&&firstCache.Statistics.Resident==127,"fixed-capacity cache remains bounded through eviction");
    var cancellationCache=new PlanetaryLocalTerrainCache(128);var cancellation=cancellationCache.Request(sectors[0],false);
    Check(cancellationCache.TryBeginRead(cancellation)&&cancellationCache.Cancel(cancellation)&&!cancellationCache.TryCompleteRead(cancellation,1,1)&&cancellationCache.Statistics.Canceled==1,"stale predictive request cancellation invalidates its generation before publication");

    using var content=JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtureRoot,"tiny-local.content.json")));
    var contentRoot=content.RootElement;
    Check(contentRoot.GetProperty("rawBytes").GetInt64()==1_951_488&&contentRoot.GetProperty("gpuBytes").GetInt64()==696_960&&
          contentRoot.GetProperty("bc7Bytes").GetInt64()==278_784&&contentRoot.GetProperty("bc4Bytes").GetInt64()==139_392&&contentRoot.GetProperty("bc5Bytes").GetInt64()==278_784,"fixture reports naive and GPU-native dataset sizes");
    Check(contentRoot.GetProperty("maximumVerticalErrorMetres").GetDouble()<6.1&&contentRoot.GetProperty("rmsVerticalErrorMetres").GetDouble()<2.2,"BC4 physical residual error is explicit and bounded");
    Check(contentRoot.GetProperty("maximumSlopeError").GetDouble()<.0023&&contentRoot.GetProperty("rmsSlopeError").GetDouble()<.00031&&
          contentRoot.GetProperty("maximumNormalErrorDegrees").GetDouble()<.33&&contentRoot.GetProperty("rmsNormalErrorDegrees").GetDouble()<.23&&
          contentRoot.GetProperty("worstVerticalSample").GetProperty("verticalErrorMetres").GetDouble()==contentRoot.GetProperty("maximumVerticalErrorMetres").GetDouble(),
          "BC4 slope/normal error and worst geographic sample are explicit and bounded");

    var nativeSource=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    var localSource=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","LocalTerrainPack.cpp"));
    var shaderSource=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","local_terrain.glsl"));
    Check(nativeSource.Contains("VK_FORMAT_BC7_SRGB_BLOCK",StringComparison.Ordinal)&&nativeSource.Contains("VK_FORMAT_R16_UNORM",StringComparison.Ordinal)&&nativeSource.Contains("VK_FORMAT_BC5_UNORM_BLOCK",StringComparison.Ordinal)&&nativeSource.Contains("VK_FORMAT_R8_UNORM",StringComparison.Ordinal),"native residency uses BC7/R16/BC5/R8 regional channels");
    Check(nativeSource.Contains("LocalPayloadSlots=256",StringComparison.Ordinal)&&nativeSource.Contains("LocalUploadBudget=2",StringComparison.Ordinal)&&nativeSource.Contains("std::thread(LocalIoWorker",StringComparison.Ordinal),"runtime cache, uploads, and asynchronous I/O are fixed and bounded");
    Check(nativeSource.Contains("TryPromoteLocalVisibleTransaction",StringComparison.Ordinal)&&nativeSource.Contains("localLayerPublished",StringComparison.Ordinal)&&
          nativeSource.Contains("a.localLayerPublished[layer]=0",StringComparison.Ordinal),"visible local sectors remain behind the coherent terrain-v5 base until the complete footprint transaction is resident");
    Check(localSource.Contains("local terrain payload digest mismatch",StringComparison.Ordinal)&&shaderSource.Contains("binding=28",StringComparison.Ordinal)&&shaderSource.Contains("binding=31",StringComparison.Ordinal),"fixture exercises the production parser while the shared local-terrain shader declares fixed BC arrays and remap metadata");
    Check(shaderSource.Contains("textureGrad(localTerrainAlbedo",StringComparison.Ordinal)&&shaderSource.Contains("LocalTerrainStoredExtent=264.0",StringComparison.Ordinal)&&
          shaderSource.Contains("ProductionDirectionAddress",StringComparison.Ordinal),"local sector sampling preserves body-direction addressing, explicit gradients, and four-texel filtering gutters");
    var productionFragment=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","planetary_production.frag"));
    Check(productionFragment.Contains("LocalTerrainElevationResidual",StringComparison.Ordinal)&&productionFragment.Contains("SampleLocalTerrainMaterial",StringComparison.Ordinal)&&
          !productionFragment.Contains("surfaceNormal=normalize(mix(surfaceNormal,ApplyLocalTerrainNormal",StringComparison.Ordinal),
          "the dynamic anchored production path consumes regional height/albedo/control while final displaced geometry remains normal authority");
    Console.WriteLine($"Local terrain fixture: sectors={header.RecordCount}; disk={manifest.ByteSize}B; GPU={contentRoot.GetProperty("gpuBytes").GetInt64()}B; cache=128/256/512 bounded candidates; production=256 slots");
}

static void M12FloridaRegionalPhysicalSurfaceTest()
{
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    Check(TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthLocalAssetId,null,
        out var manifest,out var path,out var error),$"M12 regional asset: {error}");
    Check(manifest.AssetId=="earth-florida-m12"&&manifest.FormatVersion==3&&manifest.Hierarchy.MinimumPayloadLevel==8&&
          manifest.Hierarchy.MaximumPayloadLevel==11&&manifest.Hierarchy.RecordCount==859,"M12 manifest identifies one contiguous L8-L11 Florida hierarchy");
    var package=File.ReadAllBytes(path);
    Check(PlanetaryLocalTerrainPackContract.TryReadHeader(package,out var header)&&header.Version==3&&header.RecordCount==859,
        "NCCUBE2-v3 production header");
    Check(NativeRuntime.ValidateTerrainAsset(path,manifest.BodyId,manifest.TerrainVersion,(uint)manifest.Hierarchy.RecordCount)==NativeResult.Success,
        "native NCCUBE2-v3 parser, R16/control transcode, and record digests");
    var offset=PlanetaryLocalTerrainPackContract.HeaderBytes;var levels=new HashSet<int>();
    var minimumRange=double.PositiveInfinity;var maximumRange=double.NegativeInfinity;
    for(var index=0;index<header.RecordCount;index++)
    {
        Check(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(package.AsSpan(offset,PlanetaryLocalTerrainPackContract.RecordHeaderBytes),out var record),$"M12 record {index}");
        Check(record.Sector.PayloadVersion==3&&record.HasControl&&record.HasPerRecordResidualRange&&
              record.GpuElevationBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.R16Unorm,PlanetaryLocalTerrainPackContract.StoredExtent)&&
              record.GpuControlBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.R8Unorm,PlanetaryLocalTerrainPackContract.StoredExtent),
              $"M12 R16/control ABI {index}");
        levels.Add(record.Sector.Level);minimumRange=Math.Min(minimumRange,record.ResidualMinimumMetres);maximumRange=Math.Max(maximumRange,record.ResidualMaximumMetres);
        offset=checked((int)record.PayloadOffset+(int)record.StoredAlbedoBytes+(int)record.StoredElevationBytes+(int)record.StoredNormalBytes+(int)record.StoredControlBytes);
    }
    Check(offset==package.Length&&levels.SetEquals([8,9,10,11])&&minimumRange==-128d&&maximumRange==127.99609375d,
        "M12 records are ordered, exhaustive, and use the versioned physical residual range");
    Check(EarthLocalTerrainElevationDataset.TryLoad(path,out var loadError),$"M12 CPU oracle: {loadError}");
    var launch=BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,FloridaLaunchSite.Longitude*Math.PI/180d);
    var launchResidual=EarthLocalTerrainElevationDataset.SampleResidual(launch);
    Check(launchResidual is >10d and <30d&&EarthLocalTerrainElevationDataset.SampleControl(launch)==PlanetarySurfaceControlClass.LaunchSiteReservation,
        $"M12 launch-site physical residual/control: {launchResidual:R} m");
    var launchModifier=PlanetaryPhysicalSurface.EvaluateModifiers(launch);
    Check(launchModifier.ErosionHeightMetres==0d&&PlanetaryPhysicalSurface.LaunchReservationRadiusMetres==275d,
        "the authored launch-site control reservation excludes the bounded sub-source physical modifier");
    var westBoundary=BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,-81.00055555609339d*Math.PI/180d);
    var outside=BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,-81.01d*Math.PI/180d);
    var boundaryResidual=EarthLocalTerrainElevationDataset.SampleResidual(westBoundary);var outsideResidual=EarthLocalTerrainElevationDataset.SampleResidual(outside);
    Check(Math.Abs(boundaryResidual)<.01d&&outsideResidual==0d,
        $"M12 residual converges to canonical global height at and beyond the regional boundary: boundary={boundaryResidual:R}; outside={outsideResidual:R}");
    using var content=JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot,manifest.ContentManifest.Replace('/',Path.DirectorySeparatorChar))));
    var root=content.RootElement;var classes=root.GetProperty("controlClassTexels");
    Check(root.GetProperty("maximumVerticalErrorMetres").GetDouble()<.0021d&&root.GetProperty("rmsVerticalErrorMetres").GetDouble()<.0018d&&
          classes.GetProperty("ocean").GetInt64()>0&&classes.GetProperty("wetland").GetInt64()>0&&classes.GetProperty("developed").GetInt64()>0&&classes.GetProperty("launchSiteReservation").GetInt64()>0,
          "M12 source quantization is millimetre-class and every foundational Florida control family is represented");
    var nativeSource=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    var fragment=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","planetary_production.frag"));
    foreach(var diagnostic in new[]{"global-height","regional-height","residual","final-height","physical-modifier","biome-id","biome-blend","modifier-family","near-physical","regional-control","material-id","regional-mip","regional-residency","regional-boundary"})
        Check(nativeSource.Contains($"\"{diagnostic}\"",StringComparison.Ordinal),$"M12 diagnostic {diagnostic}");
    Check(fragment.Contains("stored regional BC5 field is a payload/diagnostic channel",StringComparison.Ordinal),
        "production normal is generated from final composed physical geometry rather than reapplied regional BC5");
    Console.WriteLine($"M12 Florida: records={header.RecordCount}; levels={string.Join('/',levels.Order())}; launchResidual={launchResidual:F3}m; maxQuantization={root.GetProperty("maximumVerticalErrorMetres").GetDouble():F6}m; bytes={package.Length}");
}

static void ProductionEarthMaterialStateContinuityTest()
{
    var root=new ReferenceFrameId(994);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error)&&value is not null,$"Earth material-state scene: {error}");
    var scene=value!;var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);

    Check(scene.Focus(camera,NativePresentationFocus.Earth),"initial Earth material-state focus");
    var focusBaseline=FocusFingerprint(scene,camera);
    foreach(var intermediate in new[]{NativePresentationFocus.Sun,NativePresentationFocus.Venus,NativePresentationFocus.Mars,NativePresentationFocus.Moon})
    {
        Check(scene.Focus(camera,intermediate),$"focus {intermediate} before immediate Earth state proof");
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"immediate Earth return from {intermediate}");
        Check(FocusFingerprint(scene,camera)==focusBaseline,$"Earth apparent radius, framing, regime, and production identity are correct on the first authoritative frame after {intermediate}");
        Check(scene.Focus(camera,intermediate),$"focus {intermediate} before warp-control equivalence");
        scene.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out _,out _);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"Earth return from {intermediate} after warp control");
        Check(FocusFingerprint(scene,camera)==focusBaseline,$"touching time-warp control cannot change Earth focus presentation after {intermediate}");
        scene.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out _,out _);
    }
    var earth=scene.FocusedBody;
    var fixedCameraRoot=earth.Position.Value+earth.BodyFixedToRoot.Rotate(new Double3(.31d,.27d,.911d).Normalized()*earth.RadiusMetres*4d);
    var fixedOrientation=earth.BodyFixedToRoot;
    camera.Position=camera.Position with{Value=fixedCameraRoot};camera.Orientation=fixedOrientation;scene.Update(camera);
    var expected=Fingerprint(scene,camera);

    foreach(var intermediate in new[]{NativePresentationFocus.Venus,NativePresentationFocus.Mars,NativePresentationFocus.Moon})
    {
        Check(scene.Focus(camera,intermediate),$"focus {intermediate} between Earth material samples");scene.Update(camera);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"return from {intermediate} to Earth");
        camera.Position=camera.Position with{Value=fixedCameraRoot};camera.Orientation=fixedOrientation;scene.Update(camera);
        Check(Fingerprint(scene,camera)==expected,$"Earth material/lighting transport is bit-identical after {intermediate}");
    }
    scene.ResetPresentationCamera(camera);scene.Update(camera);
    Check(scene.Focus(camera,NativePresentationFocus.Earth),"return from Solar overview to Earth");
    camera.Position=camera.Position with{Value=fixedCameraRoot};camera.Orientation=fixedOrientation;scene.Update(camera);
    Check(Fingerprint(scene,camera)==expected,"Earth material/lighting transport is bit-identical after Solar overview");

    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var shaderRoot=Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders");
    var shared=File.ReadAllText(Path.Combine(shaderRoot,"production_earth_material.glsl"));
    var global=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production.frag"));
    var distant=File.ReadAllText(Path.Combine(shaderRoot,"distant_planet.frag"));
    var native=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    Check(shared.Contains("ProductionEarthSurfaceMaterial",StringComparison.Ordinal)&&shared.Contains("roughness",StringComparison.Ordinal)&&shared.Contains("specular",StringComparison.Ordinal),"terrain-v5 owns one explicit Earth base-material function");
    Check(global.Contains("#include \"production_earth_material.glsl\"",StringComparison.Ordinal)&&distant.Contains("#include \"production_earth_material.glsl\"",StringComparison.Ordinal)&&global.Contains("PlanetLighting(earth.albedo",StringComparison.Ordinal)&&distant.Contains("PlanetLighting(albedo",StringComparison.Ordinal),"global, dynamic anchored, and unsupported-body distant paths share the appropriate material/BRDF authority");
    Check(distant.Contains("binding=24",StringComparison.Ordinal)&&distant.Contains("binding=25",StringComparison.Ordinal)&&distant.Contains("binding=26",StringComparison.Ordinal)&&global.Contains("binding=24",StringComparison.Ordinal)&&global.Contains("binding=25",StringComparison.Ordinal)&&global.Contains("binding=26",StringComparison.Ordinal),"all Earth owners consume the same albedo/elevation/land payload tuple");
    Check(native.Contains("complete bounded terrain-v5 L0-L2 hierarchy is renderer-lifetime",StringComparison.Ordinal)&&native.Contains("BootstrapProductionHierarchy(a)",StringComparison.Ordinal)&&native.Contains("Earth terrain-v5 complete L0-L2 hierarchy synchronously resident before first submitted presentation frame",StringComparison.Ordinal)&&native.Contains("Earth terrain-v5 material roots ready: mask=0x3F",StringComparison.Ordinal)&&native.Contains("sharedAlbedoElevationLand=true",StringComparison.Ordinal),"renderer synchronously publishes the complete immutable L0-L2 hierarchy before the first submitted focus frame");
    var destroySubmissionSource=native[native.IndexOf("void DestroySubmission",StringComparison.Ordinal)..native.IndexOf("void Submission",StringComparison.Ordinal)];var createSubmissionSource=native[native.IndexOf("void CreateSubmission",StringComparison.Ordinal)..native.IndexOf("void EnsurePatchCapacity",StringComparison.Ordinal)];var createProductionSource=native[native.IndexOf("void CreateProductionCubeSurface",StringComparison.Ordinal)..native.IndexOf("void DestroyProductionCubeSurface",StringComparison.Ordinal)];var destroyProductionSource=native[native.IndexOf("void DestroyProductionCubeSurface",StringComparison.Ordinal)..native.IndexOf("float ProductionElevationMetres",StringComparison.Ordinal)];
    Check(!destroySubmissionSource.Contains("productionLayerLookupBuffer",StringComparison.Ordinal)&&!createSubmissionSource.Contains("CreateHostBuffer(a,sizeof(uint32_t)*ProductionLookupCapacity",StringComparison.Ordinal)&&createProductionSource.Contains("CreateHostBuffer(a,sizeof(uint32_t)*ProductionLookupCapacity",StringComparison.Ordinal)&&destroyProductionSource.Contains("DestroyHostBuffer(a,a.productionLayerLookupBuffer",StringComparison.Ordinal),"terrain-v5 root lookup and payload ownership survive ordinary swapchain/submission recreation as renderer-lifetime state");
    Check(native.Contains("id.level==0u&&a.productionLayerLookupMapped&&a.terrainSampleMapped",StringComparison.Ordinal)&&native.Contains("a.productionLayerPatch[layer]==id&&!a.productionElevationCpu[layer].empty()",StringComparison.Ordinal),"GPU terrain-key regeneration reattaches immutable resident roots without duplicate disk loads or material-owner replacement");
    Check(native.Contains("ProductionHierarchyPayloadsReady",StringComparison.Ordinal)&&native.Contains("production context selected before the complete immutable L0-L2 hierarchy was resident",StringComparison.Ordinal)&&
          !native.Contains("productionFallbackOwner",StringComparison.Ordinal)&&!native.Contains("productionFallback",StringComparison.Ordinal),"production focus requires the complete immutable hierarchy and has no generic distant-Earth fallback owner");
    Check(native.Contains("earthTransitionTraceRemaining=contextBody==nc::production::EarthBodyId?180u:0u",StringComparison.Ordinal)&&native.Contains("Earth focus frame: frame=%llu",StringComparison.Ordinal)&&native.Contains("material=%s",StringComparison.Ordinal)&&native.Contains("candidateOwner?\"production-billboard\":\"terrain-v5-root\"",StringComparison.Ordinal)&&
          native.Contains("Earth Vulkan submission: terrainFrame=%llu",StringComparison.Ordinal)&&native.Contains("serializedFence=true; globalDraw=%u; dynamicHierarchyDraw=%u; candidateIndirectDraw=%u; visibleEarthOwners=1",StringComparison.Ordinal),"the first 180 Earth focus submissions expose bounded frame/owner/radius/material and serialized swapchain submission telemetry for manual correlation");
    var focused=scene.FocusedPresentation(camera);var focusedDistant=scene.DistantBodies[0];var focusedGpu=scene.GpuConstants(camera);var transportedRadius=(double)focusedGpu.RadiusHigh+focusedGpu.RadiusLow;
    Check(BitConverter.DoubleToInt64Bits(scene.FocusedBody.RadiusMetres)==BitConverter.DoubleToInt64Bits(earth.RadiusMetres)&&focused.Radius==(float)earth.RadiusMetres&&focusedDistant.Radius==(float)earth.RadiusMetres&&Math.Abs(transportedRadius-earth.RadiusMetres)<1e-6,
        "distant, production-global, and GPU terrain paths share the authoritative Earth base radius exactly before terrain elevation");
    Console.WriteLine($"Earth material-state fingerprint: 0x{expected:X16}; focus=0x{focusBaseline:X16}; switches=Sun/Venus/Mars/Moon; no-warp=warp-control-identical; terrain-v5 shared payload=true");

    static ulong FocusFingerprint(SolarSystemScene scene,CameraState camera)
    {
        Check(SolarOverlayLayout.TryProjectBody(scene.FocusedBody,camera,out _,out _,out var apparentRadius,out _),"focused Earth projects for apparent-radius proof");
        var body=scene.FocusedBody;var distant=scene.DistantBodies[0];var gpu=scene.GpuConstants(camera);var actualDistance=Math.Sqrt((camera.Position.Value-body.Position.Value).LengthSquared);var hash=14695981039346656037ul;
        Mix((ulong)BitConverter.DoubleToInt64Bits(body.RadiusMetres));Mix((ulong)BitConverter.DoubleToInt64Bits(scene.OrbitDistance));Mix((ulong)BitConverter.DoubleToInt64Bits(actualDistance));Mix((ulong)BitConverter.DoubleToInt64Bits(apparentRadius));
        Mix((uint)BitConverter.SingleToInt32Bits(distant.Radius));Mix((uint)BitConverter.SingleToInt32Bits(distant.DistanceRadii));Mix((uint)distant.Regime);Mix((uint)BitConverter.SingleToInt32Bits(gpu.RadiusHigh));Mix((uint)BitConverter.SingleToInt32Bits(gpu.RadiusLow));Mix(scene.ProductionSurfaceEligible?1u:0u);
        return hash;
        void Mix(ulong bits)=>hash=(hash^bits)*1099511628211ul;
    }

    static ulong Fingerprint(SolarSystemScene scene,CameraState camera)
    {
        var presentation=scene.FocusedPresentation(camera);var distant=scene.DistantBodies[0];var light=scene.SolarLighting(camera);var hash=14695981039346656037ul;
        MixBits(presentation.BodyIdLow);MixBits(presentation.BodyIdHigh);MixBits(presentation.MaterialKind);MixBits(presentation.AlbedoSource);
        MixFloat(presentation.Roughness);MixFloat(presentation.Specular);MixFloat(presentation.Emissive);MixFloat(presentation.BodyOrientationX);MixFloat(presentation.BodyOrientationY);MixFloat(presentation.BodyOrientationZ);MixFloat(presentation.BodyOrientationW);
        MixBits(distant.BodyIdLow);MixBits(distant.BodyIdHigh);MixBits(distant.MaterialKind);MixBits(distant.AlbedoSource);MixFloat(distant.Roughness);MixFloat(distant.Specular);MixFloat(distant.Emissive);
        MixFloat(light.SourceCenterX);MixFloat(light.SourceCenterY);MixFloat(light.SourceCenterZ);MixFloat(light.AmbientFloor);MixFloat(light.SourceRadiance);
        return hash;
        void MixBits(uint bits)=>hash=(hash^bits)*1099511628211ul;
        void MixFloat(float number)=>MixBits((uint)BitConverter.SingleToInt32Bits(number));
    }
}

static void PlanetaryCameraTerrainExclusionTest()
{
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    Check(TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var localTerrainPath,out var localTerrainError),$"production local terrain clearance asset: {localTerrainError}");
    Check(EarthLocalTerrainElevationDataset.TryLoad(localTerrainPath,out var localElevationError),$"production local terrain clearance oracle: {localElevationError}");
    var root=new ReferenceFrameId(993);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error)&&value is not null,$"camera exclusion scene: {error}");
    var scene=value!;var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(scene.Focus(camera,NativePresentationFocus.Earth),"focus Earth for terrain exclusion");
    var earth=scene.FocusedBody;var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    Check(EarthElevationDataset.IsLoaded,"fresh Solar scene requires the checked Earth elevation oracle before camera publication");

    var candidates=new List<(Double3 Direction,double Height)>();
    foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())for(var y=0;y<=12;y++)for(var x=0;x<=12;x++)
    {
        var direction=RelaxedCubeSphereProjection.UnitDirection(face,x/12d,y/12d);
        candidates.Add((direction,terrain.SampleHeight(direction,24)));
    }
    candidates.Sort((left,right)=>left.Height.CompareTo(right.Height));
    var sites=new[]{candidates[0],candidates[candidates.Count/2],candidates[^1]};
    var physicalMinimum=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres;
    var correctionTarget=SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres;
    var localPackage=File.ReadAllBytes(localTerrainPath);var localOffset=PlanetaryLocalTerrainPackContract.HeaderBytes;
    var maximumLocalResidual=double.NegativeInfinity;var maximumLocalDirection=Double3.UnitY;
    Check(PlanetaryLocalTerrainPackContract.TryReadHeader(localPackage,out var localHeader),"production local terrain header is readable by the clearance oracle test");
    for(var recordIndex=0;recordIndex<localHeader.RecordCount;recordIndex++)
    {
        Check(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(localPackage.AsSpan(localOffset,PlanetaryLocalTerrainPackContract.RecordHeaderBytes),out var record)&&
            record.Sector.TerrainVersion==5&&record.Sector.PayloadVersion==3&&record.HasControl&&record.HasPerRecordResidualRange&&
            record.GpuElevationBytes==PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.R16Unorm,PlanetaryLocalTerrainPackContract.StoredExtent),
            $"production Florida NCCUBE2-v3 R16/control record {recordIndex}");
        var cells=1<<record.Sector.Level;
        for(var sy=1;sy<=7;sy+=2)for(var sx=1;sx<=7;sx+=2)
        {
            var direction=RelaxedCubeSphereProjection.UnitDirection(record.Sector.Face,(record.Sector.X+sx/8d)/cells,(record.Sector.Y+sy/8d)/cells);
            Check(RelaxedCubeSphereProjection.TryAddress(direction,out var recoveredFace,out var recoveredU,out var recoveredV)&&recoveredFace==record.Sector.Face&&
                  Math.Abs(recoveredU-(record.Sector.X+sx/8d)/cells)<2e-9d&&Math.Abs(recoveredV-(record.Sector.Y+sy/8d)/cells)<2e-9d,
                  "CPU local-terrain address round-trips the accepted relaxed-cube projection");
            var residual=EarthLocalTerrainElevationDataset.SampleResidual(direction);
            if(residual>maximumLocalResidual){maximumLocalResidual=residual;maximumLocalDirection=direction;}
        }
        localOffset=checked((int)record.PayloadOffset+(int)record.StoredAlbedoBytes+(int)record.StoredElevationBytes+(int)record.StoredNormalBytes+(int)record.StoredControlBytes);
    }
    Check(maximumLocalResidual>physicalMinimum,"production NCCUBE2 includes a positive physical residual capable of penetrating the former global-only camera floor");
    var globalOnlyHeight=EarthElevationDataset.SampleHeight(maximumLocalDirection);
    var baseHeight=terrain.SampleBaseHeight(maximumLocalDirection);
    var renderedHeight=terrain.SampleHeight(maximumLocalDirection,24);
    var formerClearance=globalOnlyHeight+correctionTarget-renderedHeight;
    Check(Math.Abs((baseHeight-globalOnlyHeight)-maximumLocalResidual)<1e-9d&&formerClearance<0d,
          "the exact decoded NCCUBE2 residual participates in the canonical physical base and the complete physical surface reproduces the former penetration");
    var maximumExteriorCorrection=0d;var maximumExteriorIterations=0;var exteriorTerrainQueries=0;
    foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())for(var edgeSample=0;edgeSample<=32;edgeSample++)
    {
        var t=edgeSample/32d;
        foreach(var direction in new[]{RelaxedCubeSphereProjection.UnitDirection(face,t,0d),RelaxedCubeSphereProjection.UnitDirection(face,t,1d),RelaxedCubeSphereProjection.UnitDirection(face,0d,t),RelaxedCubeSphereProjection.UnitDirection(face,1d,t)})
        {
            var height=terrain.SampleHeight(direction,24);
            var proposedBody=direction*(earth.RadiusMetres+height-100d);
            var proposedRoot=earth.Position.Value+earth.BodyFixedToRoot.Rotate(proposedBody);
            Check(SurfaceAnchorAcquisition.TryConstrainCameraOrigin(earth,proposedRoot,terrain,physicalMinimum,correctionTarget,out var constrained),
                "body-local final exterior solver converges across relaxed-cube face edges and corners");
            var constrainedDirection=constrained.BodyLocalPosition.Normalized();
            maximumExteriorCorrection=Math.Max(maximumExteriorCorrection,constrained.CorrectionMetres);
            maximumExteriorIterations=Math.Max(maximumExteriorIterations,constrained.Iterations);exteriorTerrainQueries+=constrained.TerrainQueries;
            Check(constrained.SurfaceAltitudeMetres>=physicalMinimum&&constrained.Iterations is >=1 and <=SurfaceAnchorAcquisition.CameraExteriorMaximumIterations&&
                  Double3.Dot(direction,constrainedDirection)>1d-1e-14d,
                "exterior correction preserves the proposed body-fixed side/direction while restoring terrain clearance");
        }
    }
    var failureAnchorDirection=maximumLocalDirection.Normalized();
    var failureAnchorRoot=earth.Position.Value+earth.BodyFixedToRoot.Rotate(failureAnchorDirection*(earth.RadiusMetres+terrain.SampleHeight(failureAnchorDirection,24)));
    var inwardRootDirection=-earth.BodyFixedToRoot.Rotate(failureAnchorDirection).Normalized();
    var oppositeExitDistance=SurfaceAnchorAcquisition.EnforceClearanceDistance(earth,failureAnchorRoot,inwardRootDirection,100d,terrain,correctionTarget);
    var oppositeExitRoot=failureAnchorRoot+inwardRootDirection*oppositeExitDistance;
    var oppositeExitBody=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(oppositeExitRoot-earth.Position.Value).Normalized();
    Check(Double3.Dot(failureAnchorDirection,oppositeExitBody)<0d,
        "an aim-line surface correction can select a wrong-side opposite-surface exit");
    var insideNearSide=failureAnchorRoot+inwardRootDirection*100d;
    Check(SurfaceAnchorAcquisition.TryConstrainCameraOrigin(earth,insideNearSide,terrain,physicalMinimum,correctionTarget,out var nearSideConstraint)&&
          Double3.Dot(failureAnchorDirection,nearSideConstraint.BodyLocalPosition.Normalized())>0d,
        "the production body-local correction retains the intended near-side hemisphere instead of traversing Earth");
    var maximumClearanceError=0d;var maximumRootRecompositionLoss=0d;var maximumHighLowLoss=0d;var maximumFloatTransportLoss=0d;
    foreach(var (direction,height) in sites)
    {
        var rootDirection=earth.BodyFixedToRoot.Rotate(direction).Normalized();
        var idealRadius=earth.RadiusMetres+height+correctionTarget;
        var idealRoot=earth.Position.Value+rootDirection*idealRadius;
        var rootAltitude=SurfaceAnchorAcquisition.SurfaceAltitude(earth,idealRoot,terrain);
        maximumRootRecompositionLoss=Math.Max(maximumRootRecompositionLoss,correctionTarget-rootAltitude);
        var distance=SurfaceAnchorAcquisition.EnforceClearanceDistance(earth,earth.Position.Value,rootDirection,0d,terrain,correctionTarget);
        var correctedRoot=earth.Position.Value+rootDirection*distance;
        var altitude=SurfaceAnchorAcquisition.SurfaceAltitude(earth,correctedRoot,terrain);
        var cameraBody=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(correctedRoot-earth.Position.Value);
        var encoded=EncodedPosition.Encode(cameraBody);
        var reconstructed=new Double3((double)encoded.HighX+encoded.LowX,(double)encoded.HighY+encoded.LowY,(double)encoded.HighZ+encoded.LowZ);
        var reconstructedRadius=Math.Sqrt(reconstructed.LengthSquared);var reconstructedDirection=reconstructed/reconstructedRadius;
        var reconstructedAltitude=reconstructedRadius-PlanetaryTerrainQuery.SurfaceRadius(earth.RadiusMetres,reconstructedDirection,terrain);
        maximumHighLowLoss=Math.Max(maximumHighLowLoss,altitude-reconstructedAltitude);
        maximumFloatTransportLoss=Math.Max(maximumFloatTransportLoss,altitude-(float)altitude);
        maximumClearanceError=Math.Max(maximumClearanceError,Math.Abs(altitude-correctionTarget));
        Check(distance>=earth.RadiusMetres+height&&altitude>=physicalMinimum&&double.IsFinite(distance),$"guarded body-fixed terrain floor holds at sampled elevation {height:R} m");
    }

    // One update attempts to cross the entire remaining altitude. The final
    // published camera position, rather than the previous-frame position,
    // owns the terrain exclusion decision.
    scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=10_000},out _,out _);
    var minimumObserved=SurfaceAnchorAcquisition.SurfaceAltitude(scene.FocusedBody,camera.Position.Value,terrain);
    Check(minimumObserved>=physicalMinimum,"single-step zoom cannot tunnel through Earth terrain");
    var closeDirectionBefore=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value-scene.FocusedBody.Position.Value).Normalized();
    scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=420,MouseDeltaY=-180},out _,out _);
    var closeOrientationAfterDrag=camera.Orientation;
    var closeDirectionAfterDrag=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value-scene.FocusedBody.Position.Value).Normalized();
    scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
    var closeDirectionAfterWheel=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value-scene.FocusedBody.Position.Value).Normalized();
    Check(Double3.Dot(closeDirectionBefore,closeDirectionAfterDrag)>0d&&Double3.Dot(closeDirectionAfterDrag,closeDirectionAfterWheel)>0d&&
          camera.Orientation==closeOrientationAfterDrag&&scene.SurfaceAltitudeMetres>=physicalMinimum,
        "extreme close drag followed by wheel input preserves the near-side hemisphere, inertial orientation, and terrain exterior");
    var correctionCountBeforeStress=scene.CameraClearanceCorrectionCount;var maximumGuardConsumption=0d;
    var maximumStressPositionDelta=0d;var maximumStressOrientationDelta=0d;var maximumStressTargetDistance=0d;
    for(var cycle=0;cycle<1000;cycle++)
    {
        var positionBefore=camera.Position.Value;var orientationBefore=camera.Orientation;
        scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=(cycle%7)-3,MouseDeltaY=(cycle%5)-2,MouseWheelDetents=cycle%11==0?-1:1000},out _,out _);
        if(cycle%200==199)Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out var advanceError),$"camera-floor rotation/time advance {cycle}: {advanceError}");
        var finalAltitude=scene.EnforceFinalCameraInvariant(camera);
        var altitude=SurfaceAnchorAcquisition.SurfaceAltitude(scene.FocusedBody,camera.Position.Value,terrain);minimumObserved=Math.Min(minimumObserved,altitude);
        maximumGuardConsumption=Math.Max(maximumGuardConsumption,Math.Max(0d,correctionTarget-finalAltitude));
        maximumStressPositionDelta=Math.Max(maximumStressPositionDelta,Math.Sqrt((camera.Position.Value-positionBefore).LengthSquared));
        maximumStressOrientationDelta=Math.Max(maximumStressOrientationDelta,QuaternionAngle(orientationBefore,camera.Orientation));
        maximumStressTargetDistance=Math.Max(maximumStressTargetDistance,Math.Sqrt((camera.Position.Value-scene.CurrentVisualAimRoot).LengthSquared));
        Check(altitude>=physicalMinimum&&camera.Position.Value.IsFinite&&scene.CurrentInertialCameraOffset.IsFinite,"aggressive drag/zoom publishes only finite above-terrain camera positions");
    }
    var stressCorrectionCount=scene.CameraClearanceCorrectionCount-correctionCountBeforeStress;

    const int timingSamples=4096;var timingTicks=new long[timingSamples];
    Check(SurfaceAnchorAcquisition.TryConstrainCameraOrigin(earth,failureAnchorRoot,terrain,physicalMinimum,correctionTarget,out _),"warm camera exterior solver");
    var timingAllocationBefore=GC.GetAllocatedBytesForCurrentThread();var timingTotalTicks=0L;var timingIterations=0L;
    for(var sample=0;sample<timingSamples;sample++)
    {
        var site=candidates[(sample*37)%candidates.Count];var signedOffset=(sample&1)==0?100d:-1d;
        var proposedRoot=earth.Position.Value+earth.BodyFixedToRoot.Rotate(site.Direction*(earth.RadiusMetres+site.Height+signedOffset));
        var start=Stopwatch.GetTimestamp();
        Check(SurfaceAnchorAcquisition.TryConstrainCameraOrigin(earth,proposedRoot,terrain,physicalMinimum,correctionTarget,out var timed),"timed camera exterior solver");
        var elapsed=Stopwatch.GetTimestamp()-start;timingTicks[sample]=elapsed;timingTotalTicks+=elapsed;timingIterations+=timed.Iterations;
    }
    var timingAllocations=GC.GetAllocatedBytesForCurrentThread()-timingAllocationBefore;
    Array.Sort(timingTicks);var nanosecondsPerTick=1_000_000_000d/Stopwatch.Frequency;
    var invariantAverageNanoseconds=timingTotalTicks*nanosecondsPerTick/timingSamples;
    var invariantP95Nanoseconds=timingTicks[(int)(timingSamples*.95d)]*nanosecondsPerTick;
    var invariantP99Nanoseconds=timingTicks[(int)(timingSamples*.99d)]*nanosecondsPerTick;
    Check(timingAllocations==0&&timingIterations<=timingSamples&&invariantAverageNanoseconds>0d,"body-local camera exterior solver is bounded and allocation-free");

    var highSite=sites[^1];var highRootDirection=scene.FocusedBody.BodyFixedToRoot.Rotate(highSite.Direction).Normalized();
    double preflightAltitude=double.NaN;
    foreach(var penetration in new[]{.01d,1d,100d})
    {
        camera.Position=camera.Position with{Value=scene.FocusedBody.Position.Value+highRootDirection*(scene.FocusedBody.RadiusMetres+highSite.Height-penetration)};
        preflightAltitude=scene.EnforceFinalCameraInvariant(camera);
        Check(preflightAltitude>=physicalMinimum,$"runtime preflight recovers a deliberate {penetration:R} m camera penetration without terminating player input");
    }
    var gpu=scene.GpuConstants(camera);
    Check(SurfaceAnchorAcquisition.TryMeasureCameraTransport(scene.FocusedBody,camera.Position.Value,terrain,out var transportIdentity),
        "measure final body-local camera transport");
    var transportedGpuBody=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
    Console.WriteLine($"Final terrain preflight: altitude={preflightAltitude:R}; gpu={gpu.SurfaceAltitudeMetres:R}");
    Check(preflightAltitude>=physicalMinimum&&Math.Abs(gpu.SurfaceAltitudeMetres-preflightAltitude)<.0001d,
        "the final pre-submit invariant repairs an injected penetration and transports the same physical terrain altitude to GPU presentation");
    Check(transportIdentity.ValidatedClearanceMetres>=physicalMinimum&&transportIdentity.GpuClearanceMetres>=physicalMinimum&&
          transportIdentity.GpuBodyLocalPosition==transportedGpuBody&&transportIdentity.PositionErrorMetres<.001d,
        "the validated body-local origin and production shader path consume one split-FP32 camera identity");

    // With a fixed camera origin, observation orientation must not select a
    // different production terrain basis. The body-fixed physical direction
    // must remain unchanged by orientation-only input.
    scene.Update(camera);var stationaryRoot=camera.Position.Value;
    var stationaryBody=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(stationaryRoot-scene.FocusedBody.Position.Value);
    var stationaryDirection=stationaryBody.Normalized();var maximumStationaryCameraDrift=0d;var maximumStationaryDirectionError=0d;
    var stationaryCorrectionCount=scene.CameraClearanceCorrectionCount;
    for(var frame=0;frame<300;frame++)
    {
        camera.Orientation=(DoubleQuaternion.FromAxisAngle(Double3.UnitY,frame*.0007d)*DoubleQuaternion.FromAxisAngle(Double3.UnitX,-.4d+frame*.0002d)).Normalized();
        scene.Update(camera);scene.EnforceFinalCameraInvariant(camera);
        maximumStationaryCameraDrift=Math.Max(maximumStationaryCameraDrift,Math.Sqrt((camera.Position.Value-stationaryRoot).LengthSquared));
        var currentBody=scene.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value-scene.FocusedBody.Position.Value);
        maximumStationaryDirectionError=Math.Max(maximumStationaryDirectionError,
            Math.Acos(Math.Clamp(Double3.Dot(currentBody.Normalized(),stationaryDirection),-1d,1d))*scene.FocusedBody.RadiusMetres);
    }
    Console.WriteLine($"Stationary camera proof: drift={maximumStationaryCameraDrift:E9} m; corrections={scene.CameraClearanceCorrectionCount-stationaryCorrectionCount}; directionError={maximumStationaryDirectionError:E9} m; radial={stationaryDirection}");
    Check(maximumStationaryCameraDrift==0d&&scene.CameraClearanceCorrectionCount==stationaryCorrectionCount&&
          maximumStationaryDirectionError<1e-6d,
        "300 frozen-time frames preserve camera origin, settle correction, and retain body-fixed geographic direction");
    foreach(var penetration in new[]{.01d,1d,100d})
    {
        var rejected=false;try{SolarSystemScene.ValidateFinalCameraClearance(earth.BodyId,physicalMinimum-penetration);}catch(InvalidOperationException){rejected=true;}
        Check(rejected,$"final invariant rejects a deliberate {penetration:R} m penetration beyond tolerance");
    }
    Check(SurfaceFocusHandoffPolicy.SatisfiesMinimumTerrainClearance(physicalMinimum-SurfaceFocusHandoffPolicy.TerrainClearanceInvariantToleranceMetres)&&
          !SurfaceFocusHandoffPolicy.SatisfiesMinimumTerrainClearance(physicalMinimum-SurfaceFocusHandoffPolicy.TerrainClearanceInvariantToleranceMetres*1.01d),
          "invariant tolerance accepts only its explicit 0.1 mm numerical boundary");

    var source=File.ReadAllText(Path.Combine(repositoryRoot,"samples","NovaCore.Triangle","SolarSystemScene.cs"));
    var host=File.ReadAllText(Path.Combine(repositoryRoot,"samples","NovaCore.Triangle","Program.cs"));
    var native=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    var finalConstraint=source.IndexOf("if(FocusedBodyHasNavigableSolidSurface)",StringComparison.Ordinal);
    var bodyCenterBranch=finalConstraint>0?source.LastIndexOf("targetRoot=FocusedBody.Position.Value;",finalConstraint,StringComparison.Ordinal):-1;
    Check(finalConstraint>bodyCenterBranch&&source.Contains("material.Kind is PlanetMaterialKind.Rocky or PlanetMaterialKind.Terrestrial",StringComparison.Ordinal),"solid-body exclusion is applied after BodyCenter/SurfaceAnchor/release recomposition and is driven by the material catalog rather than an Earth-only branch");
    Check(source.Contains("TryLoadEarthElevationOracle",StringComparison.Ordinal)&&source.Contains("Final camera clearance invariant failed",StringComparison.Ordinal)&&
          source.Contains("TerrainClearanceCorrectionTargetMetres",StringComparison.Ordinal)&&
          !source.Contains("SurfaceAltitudeMetres=(float)(terrain.IsValid?Math.Max",StringComparison.Ordinal)&&host.Contains("s.Sol?.EnforceFinalCameraInvariant(s.Camera)",StringComparison.Ordinal)&&
          native.Contains("physicalMinimumClearance=10.0f,invariantTolerance=0.0001f",StringComparison.Ordinal),"Solar startup, guarded final managed submission, and native GPU validation enforce one unmasked terrain-v5 clearance authority");
    var iterationDistribution=scene.CameraClearanceIterationDistribution;
    Console.WriteLine($"Camera terrain exclusion: sites={sites[0].Height:R}/{sites[1].Height:R}/{sites[2].Height:R} m; localResidualMax={maximumLocalResidual:R} m; formerClearance={formerClearance:R} m; physical={physicalMinimum:R} m; guard={SurfaceFocusHandoffPolicy.TerrainClearanceNumericalGuardBandMetres:R} m; tolerance={SurfaceFocusHandoffPolicy.TerrainClearanceInvariantToleranceMetres:R} m; rootLoss={maximumRootRecompositionLoss:E9} m; highLowLoss={maximumHighLowLoss:E9} m; floatLoss={maximumFloatTransportLoss:E9} m; maximumError={maximumClearanceError:E9}; minimumStress={minimumObserved:R} m; corrections={stressCorrectionCount}; guardConsumed={maximumGuardConsumption:E9} m; exteriorCorrection={maximumExteriorCorrection:R} m; exteriorIterations={maximumExteriorIterations}; exteriorQueries={exteriorTerrainQueries}; runtimeIterations={scene.CameraClearanceIterationCount}; runtimeDistribution={iterationDistribution.Zero}/{iterationDistribution.One}/{iterationDistribution.Two}/{iterationDistribution.Three}; runtimeMaxIterations={scene.CameraClearanceMaximumIterations}; runtimeMaxCorrection={scene.CameraClearanceMaximumCorrectionMetres:R} m; runtimeQueries={scene.CameraClearanceTerrainQueries}; maxPositionDelta={maximumStressPositionDelta:R} m; maxOrientationDelta={maximumStressOrientationDelta:R} rad; maxTargetDistance={maximumStressTargetDistance:R} m; transportError={transportIdentity.PositionErrorMetres:E9} m; gpuClearance={transportIdentity.GpuClearanceMetres:R} m; stationaryDrift={maximumStationaryCameraDrift:E9} m; directionError={maximumStationaryDirectionError:E9} m; invariantAvg/P95/P99={invariantAverageNanoseconds:F1}/{invariantP95Nanoseconds:F1}/{invariantP99Nanoseconds:F1} ns; invariantAllocations={timingAllocations}");
}

static void CloseGroundReferenceFrameDiagnosticTest()
{
    const double hostIntervalSeconds=.1d;
    var root=new ReferenceFrameId(995);
    var measurements=new List<(string Warp,double SimulationSeconds,double OrientationRadians,double ExpectedSurfaceSpeed,double CameraBodyDrift,double TerrainRelativeDrift,double CameraBodySpeed,double TerrainRelativeSpeed,double InertialOffsetDrift,double AnchorBodyDrift,double Latitude,double Longitude,double LocalRadius,Double3 CameraRoot,Double3 CameraRelativeInertial,Double3 CameraBody,Double3 AnchorBody,Double3 CameraToAnchor)>();
    foreach(var (label,rate,paused) in new[]{("Pause / 0x",SimulationRate.One,true),("1x",SimulationRate.One,false),("2x",SimulationRate.Two,false),("10x",SimulationRate.Ten,false),("30x",new SimulationRate(30,1),false)})
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error)&&value is not null,$"reference-frame diagnostic {label} scene: {error}");
        var scene=value!;var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"reference-frame diagnostic {label} Earth focus");
        for(var step=0;step<192&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||scene.SurfaceAnchorBlend<1d||scene.SurfaceAltitudeMetres>SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres+.001d);step++)
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=10_000},out _,out _);
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d&&scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres&&scene.SurfaceAltitudeMetres<=SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres+.001d,$"reference-frame diagnostic {label} reaches the production 10 m floor");
        var requestedIndex=SimulationSpeedPresets.IndexOf(rate);while(scene.SpeedPresetIndex<requestedIndex)scene.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out _,out _);while(scene.SpeedPresetIndex>requestedIndex)scene.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out _,out _);
        Check(scene.Rate==rate,$"reference-frame diagnostic {label} selects exact rate");if(paused)scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);

        var beforeTime=scene.CurrentTime;var beforeBody=scene.FocusedBody;var beforeCameraRoot=camera.Position.Value;var inverseBefore=beforeBody.BodyFixedToRoot.Conjugate().Normalized();var beforeCameraRelativeInertial=beforeCameraRoot-beforeBody.Position.Value;var beforeCameraBody=inverseBefore.Rotate(beforeCameraRelativeInertial);var direction=beforeCameraBody.Normalized();var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;var terrainHeight=terrain.SampleHeight(direction,24);var anchorBody=direction*(beforeBody.RadiusMetres+terrainHeight);var beforeAnchorRoot=beforeBody.Position.Value+beforeBody.BodyFixedToRoot.Rotate(anchorBody);var beforeCameraToAnchor=beforeAnchorRoot-beforeCameraRoot;var fixedAnchor=scene.CurrentFocusTarget.SurfaceAnchor;
        Check(CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,beforeTime,out var beforeOrientation),$"reference-frame diagnostic {label} orientation before");
        var rootSurfaceRadius=beforeBody.BodyFixedToRoot.Rotate(anchorBody);var expectedSurfaceSpeed=Math.Sqrt(Double3.Cross(beforeOrientation.AngularVelocityInInertial,rootSurfaceRadius).LengthSquared);var latitude=BodyFixedGeography.LatitudeRadians(direction);var longitude=BodyFixedGeography.LongitudeRadians(direction);

        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(hostIntervalSeconds),camera,out error),$"reference-frame diagnostic {label} advance: {error}");scene.Update(camera);
        var afterTime=scene.CurrentTime;var afterBody=scene.FocusedBody;var afterCameraRoot=camera.Position.Value;var inverseAfter=afterBody.BodyFixedToRoot.Conjugate().Normalized();var afterCameraRelativeInertial=afterCameraRoot-afterBody.Position.Value;var afterCameraBody=inverseAfter.Rotate(afterCameraRelativeInertial);var afterAnchorRoot=afterBody.Position.Value+afterBody.BodyFixedToRoot.Rotate(anchorBody);var afterCameraToAnchor=afterAnchorRoot-afterCameraRoot;
        Check(CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,afterTime,out var afterOrientation),$"reference-frame diagnostic {label} orientation after");
        var simulationSeconds=afterTime.SecondsSinceEpoch-beforeTime.SecondsSinceEpoch;var orientationRadians=QuaternionAngle(beforeOrientation.BodyFixedToInertial,afterOrientation.BodyFixedToInertial);var cameraBodyDrift=Math.Sqrt((afterCameraBody-beforeCameraBody).LengthSquared);var terrainRelativeDrift=Math.Sqrt((afterCameraToAnchor-beforeCameraToAnchor).LengthSquared);var inertialOffsetDrift=Math.Sqrt((afterCameraRelativeInertial-beforeCameraRelativeInertial).LengthSquared);var anchorBodyDrift=Math.Sqrt((anchorBody-anchorBody).LengthSquared);var inverseSeconds=simulationSeconds>0d?1d/simulationSeconds:0d;
        measurements.Add((label,simulationSeconds,orientationRadians,expectedSurfaceSpeed,cameraBodyDrift,terrainRelativeDrift,cameraBodyDrift*inverseSeconds,terrainRelativeDrift*inverseSeconds,inertialOffsetDrift,anchorBodyDrift,latitude,longitude,Math.Sqrt(anchorBody.LengthSquared),afterCameraRoot,afterCameraRelativeInertial,afterCameraBody,anchorBody,afterCameraToAnchor));
        Check(scene.CurrentFocusTarget.SurfaceAnchor==fixedAnchor,"time advancement retains the exact body-fixed SurfaceAnchor identity");
        Check(scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres&&camera.Position.Value.IsFinite&&afterCameraBody.IsFinite&&afterCameraToAnchor.IsFinite,"reference-frame diagnostic retains the final terrain-clearance invariant");
        if(paused)Check(simulationSeconds==0d&&orientationRadians==0d&&cameraBodyDrift==0d&&terrainRelativeDrift==0d&&inertialOffsetDrift==0d,"pause freezes camera, terrain, and orientation with zero input");
        else Check(simulationSeconds==hostIntervalSeconds*rate.Numerator/rate.Denominator&&orientationRadians>0d&&expectedSurfaceSpeed>0d&&cameraBodyDrift>0d&&terrainRelativeDrift>0d,"active warp produces measurable body-fixed relative motion");
        Console.WriteLine($"Reference frame {label}: sim={simulationSeconds:R} s; orientation={orientationRadians:R} rad; omega={Math.Sqrt(beforeOrientation.AngularVelocityInInertial.LengthSquared):R} rad/s; lat={latitude:R}; lon={longitude:R}; radius={Math.Sqrt(anchorBody.LengthSquared):R} m; expected={expectedSurfaceSpeed:R} m/s; cameraBodyDrift={cameraBodyDrift:R} m ({cameraBodyDrift*inverseSeconds:R} m/s); terrainRelativeDrift={terrainRelativeDrift:R} m ({terrainRelativeDrift*inverseSeconds:R} m/s); inertialOffsetDrift={inertialOffsetDrift:R} m; anchorBodyDrift={anchorBodyDrift:R} m; cameraRoot={afterCameraRoot}; earthRelativeInertial={afterCameraRelativeInertial}; earthBodyFixed={afterCameraBody}; anchorBody={anchorBody}; cameraToAnchor={afterCameraToAnchor}");
    }
    var pause=measurements[0];var one=measurements[1];
    Check(pause.CameraBodyDrift==0d&&pause.TerrainRelativeDrift==0d&&pause.AnchorBodyDrift==0d,"pause is the zero-motion control");
    foreach(var measurement in measurements.Skip(1))
    {
        var speedError=Math.Abs(measurement.CameraBodySpeed-measurement.ExpectedSurfaceSpeed)/measurement.ExpectedSurfaceSpeed;var terrainSpeedError=Math.Abs(measurement.TerrainRelativeSpeed-measurement.ExpectedSurfaceSpeed)/measurement.ExpectedSurfaceSpeed;
        Check(speedError<.002d&&terrainSpeedError<.002d&&measurement.InertialOffsetDrift<.001d,"close-ground camera is Earth-relative inertial while the body-fixed terrain rotates at the authoritative local rigid-body speed");
    }
    Console.WriteLine($"Reference frame warp ratios: 2x={measurements[2].CameraBodyDrift/one.CameraBodyDrift:R}; 10x={measurements[3].CameraBodyDrift/one.CameraBodyDrift:R}; 30x={measurements[4].CameraBodyDrift/one.CameraBodyDrift:R}; terrain=({measurements[2].TerrainRelativeDrift/one.TerrainRelativeDrift:R},{measurements[3].TerrainRelativeDrift/one.TerrainRelativeDrift:R},{measurements[4].TerrainRelativeDrift/one.TerrainRelativeDrift:R})");
    Check(Math.Abs(measurements[2].CameraBodyDrift/one.CameraBodyDrift-2d)<.001d&&Math.Abs(measurements[3].CameraBodyDrift/one.CameraBodyDrift-10d)<.01d&&Math.Abs(measurements[4].CameraBodyDrift/one.CameraBodyDrift-30d)<.05d,"body-fixed camera drift scales linearly with simulation warp");
}

static void ProductionTerrainMaterialSynthesisAndTessellationStudyTest()
{
    var library=PlanetaryTerrainMaterialSynthesis.Materials;
    Check(library.Length==7&&library.ToArray().All(material=>material.IsFinite&&material.Roughness is >=0f and <=1f&&material.Metallic==0f&&
          material.AmbientOcclusion is >0f and <=1f&&MathF.Abs(material.VisualDisplacementMetres)<=PlanetaryTerrainMaterialSynthesis.MaximumVisualDisplacementMetres),
          "compact reusable terrain library has seven bounded dielectric PBR materials");

    var vegetation=PlanetaryTerrainMaterialSynthesis.Classify(1f,220f,.04f,.18f,.9f,.9f);
    var beach=PlanetaryTerrainMaterialSynthesis.Classify(1f,8f,.02f,.16f,.28f,.88f);
    var cliff=PlanetaryTerrainMaterialSynthesis.Classify(1f,700f,.94f,.25f,.42f,.7f);
    var alpine=PlanetaryTerrainMaterialSynthesis.Classify(1f,4_400f,.70f,.35f,.45f,.30f);
    var desert=PlanetaryTerrainMaterialSynthesis.Classify(1f,600f,.08f,.22f,.02f,.96f);
    var snow=PlanetaryTerrainMaterialSynthesis.Classify(1f,2_600f,.14f,.94f,.45f,.08f);
    var proofs=new[]{vegetation,beach,cliff,alpine,desert,snow};
    Check(proofs.All(weights=>weights.IsFinite&&MathF.Abs(weights.Total-1f)<2e-5f&&Enumerable.Range(0,7).All(index=>weights[(PlanetaryTerrainMaterialKind)index]>=0f)),
          "classification weights are finite, nonnegative, and normalized");
    Check(Top(vegetation)==PlanetaryTerrainMaterialKind.VegetatedSoil&&Top(beach)==PlanetaryTerrainMaterialKind.BeachSand&&
          Top(cliff)==PlanetaryTerrainMaterialKind.RockCliff&&Top(alpine)==PlanetaryTerrainMaterialKind.AlpineRock&&
          Top(desert)==PlanetaryTerrainMaterialKind.DesertSand&&Top(snow)==PlanetaryTerrainMaterialKind.SnowIce,
          "body-fixed elevation/slope/latitude/climate inputs select the intended material families");

    var maximumBoundaryDelta=0f;
    for(var sample=0;sample<512;sample++)
    {
        var elevation=-100f+sample*12f;var first=PlanetaryTerrainMaterialSynthesis.Classify(1f,elevation,.38f,.46f,.52f,.55f);
        var second=PlanetaryTerrainMaterialSynthesis.Classify(1f,elevation+.01f,.38001f,.46001f,.52001f,.55001f);
        for(var index=0;index<7;index++)maximumBoundaryDelta=Math.Max(maximumBoundaryDelta,MathF.Abs(first[(PlanetaryTerrainMaterialKind)index]-second[(PlanetaryTerrainMaterialKind)index]));
        var blended=PlanetaryTerrainMaterialSynthesis.Blend(first);
        Check(blended.IsFinite&&blended.Roughness is >=0f and <=1f&&blended.Metallic==0f&&MathF.Abs(blended.VisualDisplacementMetres)<=.45f,
              "PBR material blend remains finite and bounded");
    }
    Check(maximumBoundaryDelta<.002f,"classification and material transitions remain continuous across nearby geographic inputs");
    Check(PlanetaryTerrainMaterialSynthesis.DetailWeight(0)==1f&&PlanetaryTerrainMaterialSynthesis.DetailWeight(1_200)==1f&&
          PlanetaryTerrainMaterialSynthesis.DetailWeight(18_000)==0f&&PlanetaryTerrainMaterialSynthesis.DetailWeight(100_000)==0f&&
          MathF.Abs(PlanetaryTerrainMaterialSynthesis.DetailWeight(9_600)-.5f)<1e-6f,"near-ground synthesis fades continuously without changing orbital material authority");
    Check(PlanetaryTerrainMaterialSynthesis.Bc4MaximumPhysicalHeightErrorMetres==2.008f&&PlanetaryTerrainMaterialSynthesis.Bc4RmsPhysicalHeightErrorMetres==1.460f,
          "BC4 physical-height error stays explicit and separate from bounded visual material displacement");

    var bandScales=new[]{5.5f,96f,410f};
    foreach(var scale in bandScales)
    {
        var previous=1f;
        for(var sample=0;sample<=128;sample++)
        {
            var footprint=scale*sample/64f;var attenuation=PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(footprint,scale);
            var normalAttenuation=PlanetaryTerrainMaterialSynthesis.NormalFrequencyAttenuation(footprint,scale);
            Check(float.IsFinite(attenuation)&&attenuation is >=0f and <=1f&&attenuation<=previous+1e-7f&&
                  float.IsFinite(normalAttenuation)&&normalAttenuation is >=0f and <=1f&&normalAttenuation<=attenuation+1e-7f,
                  "procedural color and earlier-fading normal attenuation are finite, bounded, and monotonic");previous=attenuation;
        }
    }
    Check(PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(0f,5.5f)==1f&&PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(5.5f,5.5f)==0f&&
          PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(5.5f,96f)==1f&&PlanetaryTerrainMaterialSynthesis.FrequencyAttenuation(96f,410f)>0.9f,
          "unrepresentable micro frequency fades while stable meso/broad frequency remains");
    var maximumWeightTransition=0f;
    Span<float> components=stackalloc float[3];
    foreach(var axes in new[]{(0,1,2),(0,2,1),(1,2,0)})
    {
        Vector3? prior=null;
        for(var sample=-128;sample<=128;sample++)
        {
            var delta=sample*1e-5f;components.Clear();components[axes.Item1]=.39f+delta;components[axes.Item2]=.39f-delta;components[axes.Item3]=.835f;
            var weights=PlanetaryTerrainMaterialSynthesis.BiplanarWeights(new Vector3(components[0],components[1],components[2]));
            Check(float.IsFinite(weights.X)&&float.IsFinite(weights.Y)&&float.IsFinite(weights.Z)&&weights.X>=0f&&weights.Y>=0f&&weights.Z>=0f&&MathF.Abs(weights.X+weights.Y+weights.Z-1f)<2e-6f,
                  "adaptive biplanar projection weights remain finite, nonnegative, and normalized");
            if(prior is { } before)maximumWeightTransition=Math.Max(maximumWeightTransition,(weights-before).Length());prior=weights;
        }
    }
    Check(maximumWeightTransition<.001f,"biplanar axis-pair changes use a smooth ambiguity bridge without a band or weight jump");

    var altitudeNormalStatistics=new List<(double Altitude,float Maximum,float P95)>();
    foreach(var altitude in new[]{1_000d,100d,10d})
    {
        var metresPerPixel=(float)(2d*altitude*Math.Tan(Math.PI/6d)/1440d);var angles=new float[2048];
        for(var sample=0;sample<angles.Length;sample++)
        {
            var normal=Vector3.Normalize(new Vector3(.31f+(sample%17)*.001f,.47f+(sample%23)*.001f,.82f));
            var point=new Vector3((sample%64)*17.13f,(sample/64)*19.71f,(sample%29)*11.37f);
            angles[sample]=PlanetaryTerrainMaterialSynthesis.ProceduralNormalAngleDegrees(point,normal,.45f,metresPerPixel*(sample%5==0?16f:1f));
        }
        Array.Sort(angles);var maximum=angles[^1];var p95=angles[(int)(angles.Length*.95f)];
        Check(maximum<=PlanetaryTerrainMaterialSynthesis.MaximumMaterialNormalAngleDegrees&&p95<=maximum&&angles.All(float.IsFinite),$"{altitude:R} m procedural normal distribution is finite and physically bounded at nadir and grazing footprints");
        altitudeNormalStatistics.Add((altitude,maximum,p95));
    }

    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var shaderRoot=Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders");
    var synthesis=File.ReadAllText(Path.Combine(shaderRoot,"production_terrain_material.glsl"));
    var fragment=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production.frag"));
    var native=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    Check(fragment.Contains("#include \"production_terrain_material.glsl\"",StringComparison.Ordinal)&&fragment.Contains("SynthesizeProductionTerrainMaterial",StringComparison.Ordinal)&&
          fragment.Contains("terrainMaterial.ambientOcclusion",StringComparison.Ordinal),"global and dynamic anchored terrain share one GPU material-synthesis implementation");
    Check(fragment.Contains("ProductionRaySphereDirection(unitDirection,0.0,bodyRadius)",StringComparison.Ordinal)&&
          fragment.Contains("visible.elevation+LocalTerrainElevationResidual(samplingDirection)",StringComparison.Ordinal)&&
          fragment.Contains("SampleLocalTerrainMaterial(samplingDirection)",StringComparison.Ordinal)&&
          !fragment.Contains("anchored?LocalTerrainElevationResidual",StringComparison.Ordinal)&&
          !fragment.Contains("if(anchored&&localSample.resident)",StringComparison.Ordinal),
          "global and dynamic owners resolve addressing, local physical height, and local material from the same camera-ray/body-fixed authority");
    Check(synthesis.Contains("result.albedo=mix(geographicAlbedo,detailedGeographic,landDetail)",StringComparison.Ordinal)&&
          !synthesis.Contains("mix(geographicAlbedo,synthesized,.62)",StringComparison.Ordinal),
          "procedural near-field response preserves terrain-v5 geographic albedo instead of replacing it with an altitude-dependent constant palette");
    Check(synthesis.Contains("TerrainBiplanarWeights",StringComparison.Ordinal)&&synthesis.Contains("selectionConfidence",StringComparison.Ordinal)&&
          synthesis.Contains("if(weights.x>1e-4)",StringComparison.Ordinal)&&!synthesis.Contains("Triplanar",StringComparison.Ordinal),
          "adaptive biplanar path retains two-axis sampling away from a narrow smooth axis-change bridge");
    Check(fragment.Contains("dvec3 bodyMetres=ProductionRaySpherePosition(samplingDirection,representedHeight,bodyRadiusMetres)",StringComparison.Ordinal)&&
          fragment.Contains("vec3 differentialMetres=vec3(bodyMetres-cameraBodyMetres)",StringComparison.Ordinal)&&
          fragment.Contains("double bodyRadiusMetres=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w)",StringComparison.Ordinal)&&
          synthesis.Contains("96.0",StringComparison.Ordinal)&&
          synthesis.Contains("5.5",StringComparison.Ordinal)&&synthesis.Contains("410.0",StringComparison.Ordinal)&&
          synthesis.Contains("TerrainBiplanarNoiseRaw",StringComparison.Ordinal)&&synthesis.Contains("dFdx(differentialMetres)",StringComparison.Ordinal)&&
          !synthesis.Contains("dFdx(bodyMetres)",StringComparison.Ordinal)&&!synthesis.Contains("dFdx(point)",StringComparison.Ordinal)&&
          synthesis.IndexOf("TerrainWorldFootprintMetres(differentialMetres)",StringComparison.Ordinal)<synthesis.IndexOf("TerrainBiplanarNoiseRaw(bodyMetres",StringComparison.Ordinal),
          "macro/meso/micro identity uses one representation-independent FP64 body-fixed surface point while derivatives use its smooth precision-preserving camera-local metre domain before biplanar selection");
    Check(synthesis.Contains("TerrainFrequencyAttenuation",StringComparison.Ordinal)&&synthesis.Contains(".22,.62",StringComparison.Ordinal)&&
          synthesis.Contains("TerrainNormalFrequencyAttenuation",StringComparison.Ordinal)&&synthesis.Contains(".12,.38",StringComparison.Ordinal)&&
          synthesis.Contains("isnan(footprint)||isinf(footprint)",StringComparison.Ordinal),"procedural normal bands fade earlier than color before Nyquist and reject grazing derivative collapse/explosion");

    // A final pixel's view ray is independent of which valid surface owner
    // supplied its depth.  Owner-interpolated chord positions are not: a
    // coarse global triangle and a fine billboard triangle can lie hundreds
    // of metres apart on the same ray.  Material identity must therefore use
    // the represented radial-shell intersection shared by both owners.
    const double materialRadius=6_371_008.8d,representedHeight=120d;
    var materialCamera=new Double3(0d,0d,materialRadius+1_000d);
    var materialRay=new Double3(.08d,.03d,-1d).Normalized();
    var materialShell=materialRadius+representedHeight;
    var rayB=Double3.Dot(materialCamera,materialRay);
    var rayC=materialCamera.LengthSquared-materialShell*materialShell;
    var rayDistance=-rayB-Math.Sqrt(rayB*rayB-rayC);
    var canonicalMaterialPoint=materialCamera+materialRay*rayDistance;
    var coarseChordPoint=materialCamera+materialRay*(rayDistance+240d);
    var finePatchPoint=materialCamera+materialRay*(rayDistance+3d);
    var legacyOwnerDelta=Math.Sqrt((coarseChordPoint-finePatchPoint).LengthSquared);
    var canonicalOwnerDelta=Math.Sqrt((canonicalMaterialPoint-canonicalMaterialPoint).LengthSquared);
    Check(legacyOwnerDelta>200d&&canonicalOwnerDelta==0d&&
          Math.Abs(Math.Sqrt(canonicalMaterialPoint.LengthSquared)-materialShell)<1e-6d,
          "global and dynamic raster owners resolve one canonical FP64 body-fixed material point on the represented physical shell");
    Check(synthesis.Contains("ApplyTerrainHeightNormal",StringComparison.Ordinal)&&synthesis.Contains("TerrainMaterialMaximumVisualDisplacement=.45",StringComparison.Ordinal)&&
          synthesis.Contains("TerrainMaterialMaximumNormalAngleRadians=.1396263402",StringComparison.Ordinal),
          "height-derived visual normals retain bounded material detail with an 8-degree shading-normal limit");

    foreach(var edgePixels in new[]{0f,12f,48f,96f,384f})
    {
        var a=PlanetaryTerrainTessellationStudy.EdgeFactor(edgePixels,48f,1f);
        var b=PlanetaryTerrainTessellationStudy.EdgeFactor(edgePixels,48f,1f);
        Check(a==b&&a is >=1 and <=PlanetaryTerrainTessellationStudy.MaximumFactor&&(a&(a-1))==0,"candidate tessellation edge factors are deterministic, power-of-two, symmetric, and bounded");
        Check(PlanetaryTerrainTessellationStudy.AmplifiedTriangleCount(a)<=PlanetaryTerrainTessellationStudy.MaximumAmplifiedTriangleCount,"candidate triangle amplification is strictly capped");
    }
    Check(!PlanetaryTerrainTessellationStudy.AcceptedForProduction&&PlanetaryTerrainTessellationStudy.T3TriangleCount==261_632&&
          PlanetaryTerrainTessellationStudy.MaximumAmplifiedTriangleCount==16_744_448,
          "the bounded tessellation study remains rejected after finding up to 64x amplification without physical-height benefit");
    Check(native.Contains("anchoredTerrainPipeline",StringComparison.Ordinal)&&
          native.Contains("VK_PRIMITIVE_TOPOLOGY_PATCH_LIST",StringComparison.Ordinal)&&
          native.Contains("patchControlPoints=3",StringComparison.Ordinal)&&
          native.Contains("VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT",StringComparison.Ordinal)&&
          native.Contains("VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT",StringComparison.Ordinal)&&
          !native.Contains("NcAnchoredTerrainVertex",StringComparison.Ordinal),
          "the rejected full-density CPU T3 study remains absent while bounded late GPU refinement consumes reusable coarse patch topology");

    _=PlanetaryTerrainMaterialSynthesis.Classify(1f,200f,.2f,.3f,.6f,.7f);var allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
    for(var iteration=0;iteration<100_000;iteration++)_ = PlanetaryTerrainMaterialSynthesis.Blend(
        PlanetaryTerrainMaterialSynthesis.Classify(1f,(iteration&4095)-100f,(iteration&255)/255f,.31f,.62f,.74f));
    Check(GC.GetAllocatedBytesForCurrentThread()==allocatedBefore,"material classification/oracle evaluation is allocation-free");
    Console.WriteLine($"Terrain material synthesis: library={library.Length}; maxBoundaryDelta={maximumBoundaryDelta:R}; biplanarWeightDelta={maximumWeightTransition:E3}; normalAngles={string.Join(',',altitudeNormalStatistics.Select(value=>$"{value.Altitude:R}m/max{value.Maximum:F3}/p95{value.P95:F3}"))}; visualDisplacement<=0.45m; adaptiveBiplanar=true; legacyFullCpuT3={PlanetaryTerrainTessellationStudy.T3TriangleCount}; legacyTessellationStudy=rejected/{PlanetaryTerrainTessellationStudy.MaximumAmplifiedTriangleCount}; productionLateGpuRefinement=true");

    static PlanetaryTerrainMaterialKind Top(in PlanetaryTerrainMaterialWeights weights)
    {
        var result=PlanetaryTerrainMaterialKind.VegetatedSoil;var value=weights[result];
        for(var index=1;index<7;index++){var kind=(PlanetaryTerrainMaterialKind)index;if(weights[kind]>value){result=kind;value=weights[kind];}}
        return result;
    }
}

static void OpaqueDistantDetailedHandoffTest()
{
    var detailedWeights=new[]{0f,.15625f,.5f,.84375f,1f};
    foreach(var detailedWeight in detailedWeights)
    {
        var distantWeight=1f-detailedWeight;
        var distantDraw=distantWeight>0f;
        var detailedDraw=detailedWeight>0f;
        var destinationCoefficient=1f;
        if(distantDraw)destinationCoefficient*=0f;
        if(detailedDraw)destinationCoefficient*=1f-detailedWeight;
        Check(float.IsFinite(destinationCoefficient)&&destinationCoefficient==0f,$"distant-detailed handoff excludes destination color at detailed weight {detailedWeight:R}");
        Check(distantDraw||detailedDraw,"distant-detailed handoff always retains a geometry owner");
        Check(detailedWeight!=0f||distantDraw&&!detailedDraw,"zero detailed weight preserves Distant-only ownership");
        Check(detailedWeight!=1f||!distantDraw&&detailedDraw,"full detailed weight preserves Detailed-only ownership");
        var reverseWeight=1f-(1f-detailedWeight);
        Check(reverseWeight==detailedWeight,"distant-detailed opacity is symmetric under traversal reversal");

        var coveredOverBackground=distantDraw?.25f:0f;
        var coveredOverOrbit=distantDraw?.25f:1f;
        if(detailedDraw)
        {
            coveredOverBackground=detailedWeight*.75f+(1f-detailedWeight)*coveredOverBackground;
            coveredOverOrbit=detailedWeight*.75f+(1f-detailedWeight)*coveredOverOrbit;
        }
        Check(coveredOverOrbit==coveredOverBackground,"opaque planetary coverage fully rejects the behind-Earth orbit-line color");
    }
    var oldMidpointBackground=(1f-.5f)*(1f-.5f);
    Check(oldMidpointBackground==.25f,"regression exercises the former midpoint destination leak");

    var shaderDirectory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","shaders"));
    var distantVertex=File.ReadAllText(Path.Combine(shaderDirectory,"distant_planet.vert"));
    var distantFragment=File.ReadAllText(Path.Combine(shaderDirectory,"distant_planet.frag"));
    var selectionCompute=File.ReadAllText(Path.Combine(shaderDirectory,"planetary_select.comp"));
    var detailedFragment=File.ReadAllText(Path.Combine(shaderDirectory,"planetary.frag"));
    var nativeSource=File.ReadAllText(Path.Combine(shaderDirectory,"..","NovaCoreNative.cpp"));
    Check(distantVertex.Contains("color=vec4(presentation.colorDistant.rgb,1.0)",StringComparison.Ordinal)&&!distantVertex.Contains("color=vec4(presentation.colorDistant.rgb,presentation.colorDistant.a)",StringComparison.Ordinal),"Distant presentation is an opaque color base while it is owned");
    Check(selectionCompute.Contains("patchData.patches[index].color=vec4(presentation.colorDistant.rgb,presentation.blendMetricState.x)",StringComparison.Ordinal)&&detailedFragment.Contains("outColor=vec4(lit,color.a)",StringComparison.Ordinal),"DetailedAlpha remains the sole refinement blend weight");
    Check(!distantVertex.Contains("requestedEarthLevel",StringComparison.Ordinal)&&
        distantFragment.Contains("ProductionPatchOrdinal(face,0u,0u,0u)",StringComparison.Ordinal)&&
        distantFragment.Contains("productionLayers.values",StringComparison.Ordinal),
        "opaque handoff uses the resident terrain-v5 cube root transaction");
    Check(nativeSource.Contains("handoffDepth.depthWriteEnable=VK_FALSE",StringComparison.Ordinal)&&nativeSource.Contains("depth.depthWriteEnable=VK_TRUE",StringComparison.Ordinal)&&nativeSource.Contains("depth.depthCompareOp=VK_COMPARE_OP_GREATER",StringComparison.Ordinal),"Distant handoff and Detailed reversed-Z depth ownership remain unchanged");
    var orbitDraw=nativeSource.IndexOf("if(solarOverlay&&a.submission->orbitVertexCount>=2",StringComparison.Ordinal);
    var distantDrawIndex=nativeSource.IndexOf("if(distantCount){VkDeviceSize",StringComparison.Ordinal);
    var candidateOrAnchoredDrawIndex=nativeSource.IndexOf("if(a.anchoredPipelineStatisticsFrameSubmitted){vkCmdBeginQuery",StringComparison.Ordinal);
    var detailedDrawIndex=nativeSource.IndexOf("if(!candidate&&diagnosticGlobal&&regional&&(a.submission->planetaryPatchCount||gpuPlanetary)",StringComparison.Ordinal);
    var focusedOrbitDraw=nativeSource.IndexOf("if (!solarOverlay && a.submission->orbitVertexCount",StringComparison.Ordinal);
    Check(nativeSource.Contains("solarOrbitCreate=orbitPipeline",StringComparison.Ordinal)&&nativeSource.Contains("orbitDepth.depthWriteEnable=VK_FALSE",StringComparison.Ordinal)&&nativeSource.Contains("orbitDepth.depthCompareOp=VK_COMPARE_OP_GREATER_OR_EQUAL",StringComparison.Ordinal)&&nativeSource.Contains("orbitPipeline.pDepthStencilState=&orbitDepth",StringComparison.Ordinal)&&orbitDraw>=0&&distantDrawIndex>orbitDraw&&candidateOrAnchoredDrawIndex>distantDrawIndex&&detailedDrawIndex>candidateOrAnchoredDrawIndex&&focusedOrbitDraw>detailedDrawIndex,"scene-space orbit lines use read-only reversed-Z occlusion: Solar overview remains pre-surface while focused far-side segments cannot draw through terrain");
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
    var productionTerrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
    var elevation = productionTerrain.SampleHeight(aimedDirection, 24);
    var aimedRoot = earth.Position.Value + earth.BodyFixedToRoot.Rotate(aimedDirection * (earth.RadiusMetres + elevation));
    var cameraRoot = aimedRoot + earth.BodyFixedToRoot.Rotate(aimedDirection) * 3_000_000d;
    var cameraForward = (aimedRoot - cameraRoot).Normalized();
    Check(!SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root),
        Double3.Cross(cameraForward, Double3.UnitY).Normalized(), PlanetaryTerrainDefinition.EarthProductionCubeV5, out _),
        "a view ray that misses Earth does not fabricate a SurfaceAnchor");
    Check(SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root), cameraForward,
        productionTerrain, out var acquisition), "Earth camera ray acquires authoritative surface");
    var acquisitionPositionError = Math.Sqrt((acquisition.RootPositionAtAcquisition.Value - aimedRoot).LengthSquared);
    Check(acquisition.Anchor.IsValid && acquisition.SurfaceRefinementCount == SurfaceAnchorAcquisition.TerrainRefinementCount &&
        Math.Abs(acquisition.Anchor.AuthoritativeElevationMetres - productionTerrain.SampleHeight(acquisition.Anchor.BodyFixedDirection, 24)) < 1e-9d &&
        acquisitionPositionError < .01d, "Earth acquisition refines against the canonical physical surface");
    var retainedOnMiss = FocusTarget.AtSurface(acquisition.Anchor);
    if (SurfaceAnchorAcquisition.TryAcquire(earth, new UniversePosition(cameraRoot, root),
        Double3.Cross(cameraForward, Double3.UnitY).Normalized(), PlanetaryTerrainDefinition.EarthProductionCubeV5, out var replacement))
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
    var maximumAcquisitionInvariantError = 0d;
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
            maximumAcquisitionInvariantError = Math.Sqrt((camera.Position.Value -
                (scene.CurrentFocusRoot + scene.CurrentInertialCameraOffset)).LengthSquared);
            Check(scene.SurfaceAnchorBlend == 0d && scene.CurrentFocusRoot == scene.FocusedBody.Position.Value,
                "SurfaceAnchor identity begins at zero positional weight");
        }
    }
    Check(acquired && maximumAcquisitionCameraError < .01d && maximumAcquisitionCameraPositionError < .01d &&
        maximumAcquisitionInvariantError < 2e-6d,
        $"BodyCenter acquisition has no camera or focus-invariant snap (line={maximumAcquisitionCameraError:R}; position={maximumAcquisitionCameraPositionError:R}; invariant={maximumAcquisitionInvariantError:R})");
    var acquiredAnchor = scene.CurrentFocusTarget.SurfaceAnchor;
    var previousBlend = scene.SurfaceAnchorBlend;
    var maximumOrientationStep = 0d;
    var maximumSurfaceAltitudeZoomRatioError = 0d;
    for (var step = 0; step < 64 && scene.SurfaceAnchorBlend < 1d; step++)
    {
        var beforeOrientation = camera.Orientation;Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out _),"active anchor evaluates before zoom");var beforeAltitude=scene.SurfaceAltitudeMetres;
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out _),"active anchor evaluates after zoom");var afterAltitude=scene.SurfaceAltitudeMetres;
        maximumSurfaceAltitudeZoomRatioError=Math.Max(maximumSurfaceAltitudeZoomRatioError,Math.Abs(beforeAltitude/afterAltitude-SolarCameraZoomPolicy.DistanceRatioPerDetent));
        maximumOrientationStep = Math.Max(maximumOrientationStep, QuaternionAngle(beforeOrientation, camera.Orientation));
        Check(scene.SurfaceAnchorBlend >= previousBlend && scene.CurrentFocusTarget.SurfaceAnchor == acquiredAnchor,
            "handoff blend is monotonic and does not hop anchors");
        previousBlend = scene.SurfaceAnchorBlend;
    }
    Check(scene.SurfaceAnchorBlend == 1d && scene.SurfaceCameraMode == PlanetaryCameraPresentationMode.SurfaceLocal,
        "descent reaches full SurfaceAnchor focus");
    Check(maximumSurfaceAltitudeZoomRatioError<1e-9d,"post-acquisition wheel cadence logarithmically scales physical surface altitude");
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
        var offsetError = Math.Sqrt((warpCamera.Position.Value -
            (afterTarget + warpScene.CurrentInertialCameraOffset)).LengthSquared);
        var focusError = Math.Sqrt((afterTarget - expectedFocusRoot).LengthSquared);
        Check(warpScene.CurrentFocusTarget.SurfaceAnchor == stableAnchor && focusError < 1e-6d && afterTarget != beforeTarget &&
            warpCamera.Orientation == beforeView && offsetError < 2e-6d && Double3.Dot(beforeOffset, afterOffset) > 0d &&
            warpCamera.Position.Value.IsFinite && double.IsFinite(warpScene.SurfaceAltitudeMetres),
            $"SurfaceAnchor remains geographic and camera orientation remains inertial at {rate.Numerator}x (focus={focusError:R}; offset={offsetError:R}; orientation={QuaternionAngle(beforeView, warpCamera.Orientation):R})");
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
    Console.WriteLine($"SurfaceAnchor: acquisitionError={acquisitionPositionError:E3} m; ENU maxError={maximumRoundTripError:E3} m; cameraRelativePackError={maximumCameraRelativePackingError:E3} m; acquisitionCameraPositionError={maximumAcquisitionCameraPositionError:E3} m; acquisitionInvariantError={maximumAcquisitionInvariantError:E3} m; zoomRatioError={maximumSurfaceAltitudeZoomRatioError:E3}; maxOrientationStep={maximumOrientationStep:E3} rad; handoffTarget={firstReplay.AcquisitionTargetError:E3}/{firstReplay.ReleaseTargetError:E3} m; handoffCamera={firstReplay.CameraPositionError:E3} m; handoffDistance={firstReplay.TargetDistanceError:E3} m; handoffOrientation={firstReplay.AcquisitionOrientationError:E3}/{firstReplay.ReleaseOrientationError:E3} rad; anchor={anchorElapsed.TotalNanoseconds / 100_000d:F2} ns; ENU={enuElapsed.TotalNanoseconds / 100_000d:F2} ns; handoff={handoffElapsed.TotalNanoseconds / 100_000d:F2} ns; zoom={zoomElapsed.TotalNanoseconds / 100_000d:F2} ns; allocations={allocated}");

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
                targetDistanceError = Math.Sqrt((replayCamera.Position.Value -
                    (replay.CurrentFocusRoot + replay.CurrentInertialCameraOffset)).LengthSquared);
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
                targetDistanceError = Math.Max(targetDistanceError, Math.Sqrt((replayCamera.Position.Value -
                    (replay.CurrentFocusRoot + replay.CurrentInertialCameraOffset)).LengthSquared));
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
    maximumOffsetError = Math.Max(maximumOffsetError, Math.Sqrt((camera.Position.Value -
        (scene.CurrentFocusRoot + scene.CurrentInertialCameraOffset)).LengthSquared));
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
        var offsetDot=Double3.Dot(previousOffset,inertialOffset);
        var cameraBodyDirection=(cameraRoot-scene.FocusedBody.Position.Value).Normalized();
        var anchorAlignment=scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out var debugAnchor)
            ?Double3.Dot(cameraBodyDirection,(debugAnchor.Value-scene.FocusedBody.Position.Value).Normalized()):double.NaN;
        Check(previousOffset.LengthSquared == 0d || offsetDot > 0d,
            $"{sample}: inertial camera offset never inverts (dot={offsetDot:R}; previous={previousOffset}; current={inertialOffset}; altitude={scene.SurfaceAltitudeMetres:R}; blend={scene.SurfaceAnchorBlend:R}; orbit={scene.OrbitDistance:R}; anchorAlignment={anchorAlignment:R})");
        previousFocus = focus;
        previousCamera = cameraRoot;
        previousOffset = inertialOffset;
        previousKind = scene.CurrentFocusTarget.Kind;
    }
}

static void SurfaceVisualAimContinuityTest()
{
    var root=new ReferenceFrameId(1);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var candidate,out var error)&&candidate is not null,
        $"surface visual-aim scene: {error}");
    var scene=candidate!;
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(scene.Focus(camera,NativePresentationFocus.Earth),"surface visual-aim Earth focus");
    scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
    for(var step=0;step<160&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||scene.SurfaceAnchorBlend<1d);step++)
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
    Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d,
        $"surface visual aim begins with full SurfaceAnchor ownership: kind={scene.CurrentFocusTarget.Kind}; blend={scene.SurfaceAnchorBlend:R}; altitude={scene.SurfaceAltitudeMetres:R}; distance={scene.OrbitDistance:R}");

    scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=180f,MouseDeltaY=-90f},out _,out _);
    var retainedAnchor=scene.CurrentFocusTarget.SurfaceAnchor;
    var referenceYaw=scene.OrbitYawRadians;
    var referencePitch=scene.OrbitPitchRadians;
    var referenceOrientation=camera.Orientation;
    Check(scene.HasRetainedVisualAim&&scene.RetainedVisualAimWeight==1d&&
        ViewRayAngle(camera,scene.CurrentVisualAimRoot)<5e-8d,
        "oblique surface free-look retains its inertial view ray without moving the active SurfaceAnchor");

    Check(scene.CurrentFocusTarget.TryEvaluate(scene.FocusedBody,out var outwardAnchorRoot),"visual-aim anchor evaluates before outward traversal");

    var maximumAngularDiscontinuity=0d;
    var maximumVisualAimError=0d;
    var maximumOrbitLineError=0d;
    var maximumPivotReleaseError=0d;
    var maximumInvariantError=0d;
    var maximumSymmetryError=0d;
    var previousAnchorAngle=ViewRayAngle(camera,outwardAnchorRoot.Value);
    var previousVisualAimRoot=scene.CurrentVisualAimRoot;
    var previousOffset=scene.CurrentInertialCameraOffset;
    var previousKind=scene.CurrentFocusTarget.Kind;
    var previousAimOwned=scene.HasRetainedVisualAim;
    var ownershipReleases=0;
    var sawPartialPosition=false;
    var sawZeroPosition=false;
    var sawFocusRelease=false;
    var sawAimTransition=false;
    var sawAimRelease=false;
    var symmetryMeasured=false;

    for(var step=0;step<160&&!sawAimRelease;step++)
    {
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
        Measure($"outward {step}");
        if(!symmetryMeasured&&scene.RetainedVisualAimWeight is >.15d and <.85d)
        {
            var baselineAltitude=scene.SurfaceAltitudeMetres;
            var baselinePosition=camera.Position.Value;
            var baselineWeight=scene.RetainedVisualAimWeight;
            Check(FocusTarget.AtSurface(retainedAnchor).TryEvaluate(scene.FocusedBody,out var symmetryAnchor),"symmetry anchor evaluates");
            var baselineAngle=ViewRayAngle(camera,symmetryAnchor.Value);
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
            Check(FocusTarget.AtSurface(retainedAnchor).TryEvaluate(scene.FocusedBody,out symmetryAnchor),"symmetry anchor re-evaluates");
            maximumSymmetryError=Math.Max(maximumSymmetryError,Math.Abs(scene.SurfaceAltitudeMetres-baselineAltitude)/Math.Max(1d,baselineAltitude));
            maximumSymmetryError=Math.Max(maximumSymmetryError,Math.Sqrt((camera.Position.Value-baselinePosition).LengthSquared)/Math.Max(1d,baselineAltitude));
            maximumSymmetryError=Math.Max(maximumSymmetryError,Math.Abs(scene.RetainedVisualAimWeight-baselineWeight));
            maximumSymmetryError=Math.Max(maximumSymmetryError,Math.Abs(ViewRayAngle(camera,symmetryAnchor.Value)-baselineAngle));
            symmetryMeasured=true;
            previousAnchorAngle=ViewRayAngle(camera,symmetryAnchor.Value);
            previousVisualAimRoot=scene.CurrentVisualAimRoot;
            previousOffset=scene.CurrentInertialCameraOffset;
            previousKind=scene.CurrentFocusTarget.Kind;
            previousAimOwned=scene.HasRetainedVisualAim;
        }
    }

    Check(sawPartialPosition&&sawZeroPosition&&sawFocusRelease&&sawAimTransition&&sawAimRelease,
        $"outward traversal samples partial position, zero position, focus release, aim transition, and final aim release: {sawPartialPosition}/{sawZeroPosition}/{sawFocusRelease}/{sawAimTransition}/{sawAimRelease}; altitude={scene.SurfaceAltitudeMetres:R}; retained={scene.HasRetainedVisualAim}/{scene.RetainedVisualAimWeight:R}");
    Check(ownershipReleases==1,"retained visual-aim ownership releases exactly once");
    for(var crossing=0;crossing<8;crossing++)
    {
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=(crossing&1)==0?1:-1},out _,out _);
        Check(!scene.HasRetainedVisualAim,"released visual aim does not oscillate near its completed threshold");
    }
    var bodyOrientation=scene.FocusedBody.BodyFixedToRoot;
    var maximumCenteredRayError=ViewRayAngle(camera,scene.FocusedBody.Position.Value);
    var centeredDistance=Math.Sqrt((camera.Position.Value-scene.FocusedBody.Position.Value).LengthSquared);
    for(var drag=0;drag<12;drag++)
    {
        scene.ApplyPresentationInput(camera,new NativeInputState{
            LookActive=1,MouseDeltaX=(drag%5)-2,MouseDeltaY=(drag%3)-1},out _,out _);
        maximumCenteredRayError=Math.Max(maximumCenteredRayError,ViewRayAngle(camera,scene.FocusedBody.Position.Value));
        var distance=Math.Sqrt((camera.Position.Value-scene.FocusedBody.Position.Value).LengthSquared);
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&!scene.HasRetainedVisualAim&&
            scene.SurfaceCameraMode==PlanetaryCameraPresentationMode.Orbital&&
            Math.Abs(distance-centeredDistance)<1e-4d&&scene.FocusedBody.BodyFixedToRoot==bodyOrientation,
            $"far centered drag {drag} uses ordinary planet-centered orbit authority");
    }
    Check(maximumCenteredRayError<5e-8d,
        $"far click-drag keeps Earth centered after surface detach: {maximumCenteredRayError:R} rad");

    var repeatedCycleAnchor=default(SurfaceAnchorFocus);
    for(var cycle=0;cycle<2;cycle++)
    {
        for(var step=0;step<160&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||scene.SurfaceAnchorBlend<1d);step++)
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d&&
            scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial&&
            scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
            $"repeat cycle {cycle} reaches terrain-safe SurfaceAnchor ownership");
        repeatedCycleAnchor=scene.CurrentFocusTarget.SurfaceAnchor;
        scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=75f,MouseDeltaY=-45f},out _,out _);
        Check(scene.CurrentFocusTarget.SurfaceAnchor==repeatedCycleAnchor&&
            ViewRayAngle(camera,scene.CurrentVisualAimRoot)<5e-8d,
            $"repeat cycle {cycle} free-look preserves SurfaceAnchor geography and seeds the outward visual ray");
        for(var step=0;step<160&&scene.HasRetainedVisualAim;step++)
        {
            if(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor)
                Check(scene.CurrentFocusTarget.SurfaceAnchor==repeatedCycleAnchor,
                    $"repeat cycle {cycle} keeps SurfaceAnchor authority unchanged while attached");
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
        }
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&!scene.HasRetainedVisualAim&&
            scene.SurfaceCameraMode==PlanetaryCameraPresentationMode.Orbital&&
            ViewRayAngle(camera,scene.FocusedBody.Position.Value)<5e-8d&&
            scene.FocusedBody.BodyFixedToRoot==bodyOrientation,
            $"repeat cycle {cycle} cleanly restores centered inertial orbit authority");
    }
    Check(symmetryMeasured&&maximumSymmetryError<1e-8d,"inward/outward projected-anchor motion is symmetric");
    Check(maximumInvariantError<.001d,"3D-1 positional camera invariant remains exact through visual-aim handoff");
    Check(maximumAngularDiscontinuity<.05d,$"retained geographic anchor remains bounded on screen while the free-look aim transfers: {maximumAngularDiscontinuity:R} rad/detent");
    Check(maximumVisualAimError<5e-8d&&ViewRayAngle(camera,scene.FocusedBody.Position.Value)<5e-8d,
        "outward handoff finishes with the ordinary BodyCenter target on the inertial forward ray");
    Console.WriteLine($"Surface visual aim: angular={maximumAngularDiscontinuity:E3} rad; visualAim={maximumVisualAimError:E3} rad; centered={maximumCenteredRayError:E3} rad; pivotRelease={maximumPivotReleaseError:E3} m; orbitLine={maximumOrbitLineError:E3} m; invariant={maximumInvariantError:E3} m; symmetry={maximumSymmetryError:E3}; releases={ownershipReleases}; cycles=2");

    void Measure(string sample)
    {
        Check(FocusTarget.AtSurface(retainedAnchor).TryEvaluate(scene.FocusedBody,out var anchorRoot),$"{sample}: retained anchor evaluates");
        var anchorAngle=ViewRayAngle(camera,anchorRoot.Value);
        var visualAimAngle=ViewRayAngle(camera,scene.CurrentVisualAimRoot);
        var forward=camera.Orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();
        var expectedCamera=scene.CurrentVisualAimRoot-forward*scene.OrbitDistance;
        maximumAngularDiscontinuity=Math.Max(maximumAngularDiscontinuity,Math.Abs(anchorAngle-previousAnchorAngle));
        maximumVisualAimError=Math.Max(maximumVisualAimError,visualAimAngle);
        maximumOrbitLineError=Math.Max(maximumOrbitLineError,Math.Sqrt((camera.Position.Value-expectedCamera).LengthSquared));
        maximumInvariantError=Math.Max(maximumInvariantError,Math.Sqrt((camera.Position.Value-
            (scene.CurrentFocusRoot+scene.CurrentInertialCameraOffset)).LengthSquared));
        sawPartialPosition|=scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend is >0d and <1d;
        sawZeroPosition|=scene.SurfaceAnchorBlend==0d;
        sawFocusRelease|=previousKind==FocusTargetKind.SurfaceAnchor&&scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter;
        sawAimTransition|=scene.HasRetainedVisualAim&&scene.RetainedVisualAimWeight is >0d and <1d;
        sawAimRelease|=previousAimOwned&&!scene.HasRetainedVisualAim;
        if(previousAimOwned&&!scene.HasRetainedVisualAim)ownershipReleases++;
        Check(scene.OrbitYawRadians==referenceYaw&&scene.OrbitPitchRadians==referencePitch&&camera.Orientation==referenceOrientation,
            $"{sample}: yaw, pitch, and inertial orientation are unchanged");
        Check(scene.CurrentFocusRoot.IsFinite&&scene.CurrentVisualAimRoot.IsFinite&&scene.CurrentInertialCameraOffset.IsFinite&&
            camera.Position.Value.IsFinite&&double.IsFinite(anchorAngle)&&scene.RetainedVisualAimWeight is >=0d and <=1d,
            $"{sample}: visual-aim state remains finite and bounded");
        Check(Double3.Dot(previousOffset,scene.CurrentInertialCameraOffset)>0d,$"{sample}: camera offset never inverts");
        if(scene.HasRetainedVisualAim)
            Check(visualAimAngle<5e-8d,$"{sample}: retained visual aim remains on the camera forward ray");
        if(previousKind==FocusTargetKind.SurfaceAnchor&&scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter)
        {
            maximumPivotReleaseError=Math.Max(maximumPivotReleaseError,
                Math.Sqrt((scene.CurrentVisualAimRoot-previousVisualAimRoot).LengthSquared));
            Check(maximumPivotReleaseError<1e-4d&&maximumOrbitLineError<1e-3d,
                $"{sample}: positional release preserves the retained visual pivot");
        }
        previousAnchorAngle=anchorAngle;
        previousVisualAimRoot=scene.CurrentVisualAimRoot;
        previousOffset=scene.CurrentInertialCameraOffset;
        previousKind=scene.CurrentFocusTarget.Kind;
        previousAimOwned=scene.HasRetainedVisualAim;
    }

    static double ViewRayAngle(CameraState camera,in Double3 targetRoot)
    {
        var forward=camera.Orientation.Rotate(new Double3(0d,0d,-1d));
        var toTarget=(targetRoot-camera.Position.Value).Normalized();
        return Math.Acos(Math.Clamp(Double3.Dot(forward,toTarget),-1d,1d));
    }
}

static void InertialVisualAimAuthorityTest()
{
    var root=new ReferenceFrameId(1);
    var rates=new[]{new SimulationRate(1,1),new SimulationRate(30,1),new SimulationRate(600,1),
        new SimulationRate(14_400,1),new SimulationRate(7_776_000,1)};
    var maximumAnchorMotion=0d;
    var maximumCameraTranslation=0d;
    var maximumOrientationDiscontinuity=0d;
    var maximumVisualRayError=0d;
    var maximumInvariantError=0d;

    foreach(var rate in rates)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var candidate,out var error)&&candidate is not null,
            $"inertial visual-aim {rate.Numerator}x scene: {error}");
        var scene=candidate!;
        var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"inertial visual-aim Earth focus at {rate.Numerator}x");
        for(var step=0;step<160&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||scene.SurfaceAnchorBlend<1d);step++)
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d,
            $"full SurfaceAnchor ownership at {rate.Numerator}x");
        scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=170f,MouseDeltaY=-80f},out _,out _);
        var anchor=scene.CurrentFocusTarget.SurfaceAnchor;
        Check(anchor.IsValid&&anchor.LocalTangentBasis.IsValid,$"body-fixed anchor and ENU valid at {rate.Numerator}x");
        var rateIndex=SimulationSpeedPresets.IndexOf(rate);
        while(scene.SpeedPresetIndex<rateIndex)scene.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out _,out _);
        Check(scene.Rate==rate,$"selected {rate.Numerator}x rate");

        MeasureRotation("SurfaceAnchor",scene,camera,anchor,rate,ref error);

        for(var step=0;step<64&&scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor;step++)
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&scene.HasRetainedVisualAim,
            $"outward handoff reaches BodyCenter while retaining inertial aim at {rate.Numerator}x");
        MeasureRotation("BodyCenter retained aim",scene,camera,anchor,rate,ref error);
    }

    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var traversalCandidate,out var traversalError)&&traversalCandidate is not null,
        $"inertial aim round-trip scene: {traversalError}");
    var traversal=traversalCandidate!;
    var traversalCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,traversal.Projection,CameraMode.Free);
    Check(traversal.Focus(traversalCamera,NativePresentationFocus.Earth),"inertial aim round-trip Earth focus");
    traversal.ApplyPresentationInput(traversalCamera,new NativeInputState{PauseToggle=1},out _,out _);
    for(var step=0;step<160&&(traversal.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||traversal.SurfaceAnchorBlend<1d);step++)
        traversal.ApplyPresentationInput(traversalCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);
    traversal.ApplyPresentationInput(traversalCamera,new NativeInputState{LookActive=1,MouseDeltaX=150f,MouseDeltaY=-70f},out _,out _);
    var roundTripOrientation=traversalCamera.Orientation;
    var roundTripYaw=traversal.OrbitYawRadians;
    var roundTripPitch=traversal.OrbitPitchRadians;
    var maximumRoundTripRayError=0d;
    for(var step=0;step<64&&traversal.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor;step++)
    {
        traversal.ApplyPresentationInput(traversalCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
        MeasureRoundTrip("outward");
    }
    Check(traversal.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&traversal.HasRetainedVisualAim,
        "round trip reaches BodyCenter without releasing inertial visual aim");
    for(var step=0;step<160&&(traversal.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||traversal.SurfaceAnchorBlend<1d);step++)
    {
        traversal.ApplyPresentationInput(traversalCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);
        MeasureRoundTrip("inward");
    }
    Check(traversal.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&traversal.SurfaceAnchorBlend==1d,
        "round trip reacquires full body-fixed SurfaceAnchor ownership");
    Check(maximumRoundTripRayError<5e-8d,"outward and inward handoffs retain the inertial visual ray without recentering");
    Check(maximumCameraTranslation<.001d&&maximumOrientationDiscontinuity<1e-12d&&maximumVisualRayError<5e-8d&&maximumInvariantError<.001d,
        "body rotation moves the physical anchor without translating or rotating the inertial camera authority");
    Console.WriteLine($"Inertial visual aim: anchorMotion={maximumAnchorMotion:E3} m; cameraTranslation={maximumCameraTranslation:E3} m; orientation={maximumOrientationDiscontinuity:E3} rad; ray={maximumVisualRayError:E3} rad; invariant={maximumInvariantError:E3} m; roundTripRay={maximumRoundTripRayError:E3} rad");

    void MeasureRotation(string state,SolarSystemScene scene,CameraState camera,SurfaceAnchorFocus anchor,SimulationRate rate,ref string error)
    {
        Check(FocusTarget.AtSurface(anchor).TryEvaluate(scene.FocusedBody,out var anchorBefore),$"{state} anchor evaluates before {rate.Numerator}x");
        var centerBefore=scene.FocusedBody.Position.Value;
        var anchorOffsetBefore=anchorBefore.Value-centerBefore;
        var cameraOffsetBefore=camera.Position.Value-centerBefore;
        var visualOffsetBefore=scene.CurrentVisualAimRoot-centerBefore;
        var orientationBefore=camera.Orientation;
        var yawBefore=scene.OrbitYawRadians;
        var pitchBefore=scene.OrbitPitchRadians;
        Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out error),$"{state} {rate.Numerator}x advance: {error}");
        Check(FocusTarget.AtSurface(anchor).TryEvaluate(scene.FocusedBody,out var anchorAfter),$"{state} anchor evaluates after {rate.Numerator}x");
        var centerAfter=scene.FocusedBody.Position.Value;
        var anchorOffsetAfter=anchorAfter.Value-centerAfter;
        var cameraOffsetAfter=camera.Position.Value-centerAfter;
        var visualOffsetAfter=scene.CurrentVisualAimRoot-centerAfter;
        var anchorMotion=Math.Sqrt((anchorOffsetAfter-anchorOffsetBefore).LengthSquared);
        var cameraTranslation=Math.Sqrt((cameraOffsetAfter-cameraOffsetBefore).LengthSquared);
        maximumAnchorMotion=Math.Max(maximumAnchorMotion,anchorMotion);
        maximumCameraTranslation=Math.Max(maximumCameraTranslation,cameraTranslation);
        maximumOrientationDiscontinuity=Math.Max(maximumOrientationDiscontinuity,QuaternionAngle(orientationBefore,camera.Orientation));
        maximumVisualRayError=Math.Max(maximumVisualRayError,ViewRayAngle(camera,scene.CurrentVisualAimRoot));
        maximumInvariantError=Math.Max(maximumInvariantError,Math.Sqrt((camera.Position.Value-
            (scene.CurrentFocusRoot+scene.CurrentInertialCameraOffset)).LengthSquared));
        Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter||scene.CurrentFocusTarget.SurfaceAnchor==anchor,
            $"{state} body-fixed anchor identity remains unchanged at {rate.Numerator}x");
        Check(anchor.LocalTangentBasis.IsValid&&scene.FocusedBody.BodyFixedToRoot.IsFinite&&anchorMotion>0d,
            $"{state} Earth rotation moves the geographic anchor with valid body-fixed ENU at {rate.Numerator}x");
        Check(scene.OrbitYawRadians==yawBefore&&scene.OrbitPitchRadians==pitchBefore&&camera.Orientation==orientationBefore,
            $"{state} yaw, pitch, and camera quaternion remain inertial at {rate.Numerator}x");
        Check(Math.Sqrt((visualOffsetAfter-visualOffsetBefore).LengthSquared)<.001d&&cameraTranslation<.001d,
            $"{state} retained visual ray and camera position do not chase anchor rotation at {rate.Numerator}x");
        Check(anchorMotion>cameraTranslation*1000d&&anchorOffsetAfter.IsFinite&&cameraOffsetAfter.IsFinite&&visualOffsetAfter.IsFinite,
            $"{state} physical anchor motion is decoupled from finite camera translation at {rate.Numerator}x");
    }

    void MeasureRoundTrip(string state)
    {
        maximumRoundTripRayError=Math.Max(maximumRoundTripRayError,ViewRayAngle(traversalCamera,traversal.CurrentVisualAimRoot));
        maximumInvariantError=Math.Max(maximumInvariantError,Math.Sqrt((traversalCamera.Position.Value-
            (traversal.CurrentFocusRoot+traversal.CurrentInertialCameraOffset)).LengthSquared));
        Check(traversal.OrbitYawRadians==roundTripYaw&&traversal.OrbitPitchRadians==roundTripPitch&&traversalCamera.Orientation==roundTripOrientation,
            $"{state} round trip preserves inertial yaw, pitch, and quaternion");
        Check(traversal.CurrentFocusRoot.IsFinite&&traversal.CurrentVisualAimRoot.IsFinite&&traversal.CurrentInertialCameraOffset.IsFinite&&
            traversalCamera.Position.Value.IsFinite,$"{state} round-trip camera state remains finite");
    }

    static double ViewRayAngle(CameraState camera,in Double3 targetRoot)
    {
        var forward=camera.Orientation.Rotate(new Double3(0d,0d,-1d));
        var toTarget=(targetRoot-camera.Position.Value).Normalized();
        return Math.Acos(Math.Clamp(Double3.Dot(forward,toTarget),-1d,1d));
    }
}

static void CameraSurfaceAnchorHandoffMonotonicityTest()
{
    var root=new ReferenceFrameId(1);
    var slow=CreateScene("slow single-detent");
    var slowBodyOrientation=slow.Scene.FocusedBody.BodyFixedToRoot;
    var slowInward=Traverse(slow.Scene,slow.Camera,1,true,700_000d,"slow inward");
    Check(slowInward.StartAltitude>3_000_000d&&slowInward.EndAltitude<=700_000d&&slowInward.SawAnchor&&slowInward.SawPartialBlend,
        $"slow single-detent descent traverses 3,000 to 700 km through partial SurfaceAnchor ownership: start={slowInward.StartAltitude:R}; end={slowInward.EndAltitude:R}; anchor={slowInward.SawAnchor}; partial={slowInward.SawPartialBlend}");

    var rapid=CreateScene("rapid multi-detent");
    var rapidInward=Traverse(rapid.Scene,rapid.Camera,4,true,700_000d,"rapid inward");
    Check(rapidInward.StartAltitude>3_000_000d&&rapidInward.EndAltitude<=700_000d&&rapidInward.SawAnchor,
        $"rapid multi-detent descent traverses the handoff monotonically: start={rapidInward.StartAltitude:R}; end={rapidInward.EndAltitude:R}; anchor={rapidInward.SawAnchor}");

    var slowOutward=Traverse(slow.Scene,slow.Camera,-1,false,3_100_000d,"slow outward");
    Check(slowOutward.EndAltitude>=3_000_000d&&slow.Scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter,
        $"outward handoff releases stably to BodyCenter: end={slowOutward.EndAltitude:R}; kind={slow.Scene.CurrentFocusTarget.Kind}");
    Check(slow.Scene.FocusedBody.BodyFixedToRoot==slowBodyOrientation,
        "camera handoff does not change Earth/body orientation or body-fixed geography");

    Console.WriteLine(
        $"Camera handoff monotonicity: slowFrames={slowInward.Frames}; slowAltitudeReverse={slowInward.MaximumAltitudeReversal:E3}m; slowBlendReverse={slowInward.MaximumBlendReversal:E3}; slowPivotReverse={slowInward.MaximumPivotReversal:E3}m; slowScreenReverse={slowInward.MaximumScreenReversal:E3}rad; rapidFrames={rapidInward.Frames}; rapidAltitudeReverse={rapidInward.MaximumAltitudeReversal:E3}m; rapidBlendReverse={rapidInward.MaximumBlendReversal:E3}; rapidPivotReverse={rapidInward.MaximumPivotReversal:E3}m; outwardFrames={slowOutward.Frames}; outwardAltitudeReverse={slowOutward.MaximumAltitudeReversal:E3}m; outwardBlendReverse={slowOutward.MaximumBlendReversal:E3}; outwardPivotReverse={slowOutward.MaximumPivotReversal:E3}m; orientation={Math.Max(Math.Max(slowInward.MaximumOrientationStep,rapidInward.MaximumOrientationStep),slowOutward.MaximumOrientationStep):E3}rad");

    (SolarSystemScene Scene,CameraState Camera) CreateScene(string sample)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var candidate,out var error)&&candidate is not null,
            $"{sample} scene: {error}");
        var scene=candidate!;
        var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"{sample} Earth focus");
        scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
        return(scene,camera);
    }

    (double StartAltitude,double EndAltitude,int Frames,bool SawAnchor,bool SawPartialBlend,double MaximumAltitudeReversal,
        double MaximumBlendReversal,double MaximumPivotReversal,double MaximumScreenReversal,double MaximumOrientationStep)
        Traverse(SolarSystemScene scene,CameraState camera,int detents,bool inward,double targetAltitude,string sample)
    {
        var startAltitude=scene.SurfaceAltitudeMetres;
        var frames=0;
        var sawAnchor=scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor;
        var sawPartial=scene.SurfaceAnchorBlend is >0d and <1d;
        var maximumAltitudeReversal=0d;
        var maximumBlendReversal=0d;
        var maximumPivotReversal=0d;
        var maximumScreenReversal=0d;
        var maximumOrientationStep=0d;
        while(frames<96&&(inward?scene.SurfaceAltitudeMetres>targetAltitude:scene.SurfaceAltitudeMetres<targetAltitude||scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor))
        {
            var beforeAltitude=scene.SurfaceAltitudeMetres;
            var beforeBlend=scene.SurfaceAnchorBlend;
            var beforePivot=PivotDistance(scene);
            var beforeScreen=AngularRadius(scene,camera);
            var beforeOrientation=camera.Orientation;
            scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=detents},out _,out _);
            var altitudeDelta=scene.SurfaceAltitudeMetres-beforeAltitude;
            var blendDelta=scene.SurfaceAnchorBlend-beforeBlend;
            var pivotDelta=PivotDistance(scene)-beforePivot;
            var screenDelta=AngularRadius(scene,camera)-beforeScreen;
            maximumAltitudeReversal=Math.Max(maximumAltitudeReversal,inward?Math.Max(0d,altitudeDelta):Math.Max(0d,-altitudeDelta));
            maximumBlendReversal=Math.Max(maximumBlendReversal,inward?Math.Max(0d,-blendDelta):Math.Max(0d,blendDelta));
            maximumPivotReversal=Math.Max(maximumPivotReversal,inward?Math.Max(0d,-pivotDelta):Math.Max(0d,pivotDelta));
            maximumScreenReversal=Math.Max(maximumScreenReversal,inward?Math.Max(0d,-screenDelta):Math.Max(0d,screenDelta));
            maximumOrientationStep=Math.Max(maximumOrientationStep,QuaternionAngle(beforeOrientation,camera.Orientation));
            sawAnchor|=scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor;
            sawPartial|=scene.SurfaceAnchorBlend is >0d and <1d;
            frames++;
        }
        Check(maximumAltitudeReversal<=.001d&&maximumBlendReversal<=1e-12d&&maximumPivotReversal<=.001d&&
            maximumScreenReversal<=1e-12d&&maximumOrientationStep<=1e-12d,
            $"{sample} has no altitude/blend/pivot/screen close-far reversal and preserves orientation (altitude={maximumAltitudeReversal:E3}; blend={maximumBlendReversal:E3}; pivot={maximumPivotReversal:E3}; screen={maximumScreenReversal:E3}; orientation={maximumOrientationStep:E3})");
        return(startAltitude,scene.SurfaceAltitudeMetres,frames,sawAnchor,sawPartial,maximumAltitudeReversal,
            maximumBlendReversal,maximumPivotReversal,maximumScreenReversal,maximumOrientationStep);
    }

    static double PivotDistance(SolarSystemScene scene) =>
        Math.Sqrt((scene.CurrentFocusRoot-scene.FocusedBody.Position.Value).LengthSquared);

    static double AngularRadius(SolarSystemScene scene,CameraState camera)
    {
        var centerDistance=Math.Sqrt((camera.Position.Value-scene.FocusedBody.Position.Value).LengthSquared);
        return Math.Asin(Math.Clamp(scene.FocusedBody.RadiusMetres/centerDistance,0d,1d));
    }
}

static void SolarPresetCameraPathConvergenceTest()
{
    var root=new ReferenceFrameId(1);
    var overview=Create("Solar Overview");
    var fullscreen=Create("Earth Fullscreen");
    Check(overview.Scene.Focus(overview.Camera,NativePresentationFocus.Earth),"Solar Overview focuses Earth through normal focus routing");
    Check(overview.Scene.TryStartAtEarthValidationAltitude(overview.Camera,700_000d,"land")&&
        fullscreen.Scene.TryStartAtEarthValidationAltitude(fullscreen.Camera,700_000d,"land"),
        "both presets can establish the same initial Earth pose through SolarSystemScene setup");
    var expectedDirection=EarthPlanetaryScene.ValidationSurfaceDirection("land");
    Check(fullscreen.Scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&
        Math.Sqrt((fullscreen.Scene.CurrentFocusTarget.SurfaceAnchor.BodyFixedDirection-expectedDirection).LengthSquared)<1e-12d,
        "Earth fullscreen preserves its intended body-fixed land starting location");
    AssertEquivalent("initial aligned pose");

    var maximumWheelOrientationStep=0d;
    var outwardFrames=0;
    while(outwardFrames++<64&&(overview.Scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor||overview.Scene.SurfaceAltitudeMetres<3_100_000d))
        ApplyBoth(new NativeInputState{MouseWheelDetents=-1},$"outward {outwardFrames}",true);
    Check(overview.Scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&fullscreen.Scene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter,
        "both presets release SurfaceAnchor to BodyCenter on the same outward frame");

    ApplyBoth(new NativeInputState{LookActive=1,MouseDeltaX=61f,MouseDeltaY=-23f},"far orbit drag",false);
    var inwardFrames=0;
    while(inwardFrames++<64&&overview.Scene.SurfaceAltitudeMetres>700_000d)
        ApplyBoth(new NativeInputState{MouseWheelDetents=1},$"inward {inwardFrames}",true);
    Check(overview.Scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&fullscreen.Scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&
        overview.Scene.SurfaceAnchorBlend==1d&&fullscreen.Scene.SurfaceAnchorBlend==1d,
        "both presets reacquire the same full SurfaceAnchor state");
    ApplyBoth(new NativeInputState{LookActive=1,MouseDeltaX=-37f,MouseDeltaY=19f},"near-surface look",false);

    Check(overview.Scene.Focus(overview.Camera,NativePresentationFocus.Mars)&&fullscreen.Scene.Focus(fullscreen.Camera,NativePresentationFocus.Mars),
        "both presets share Solar focus routing away from Earth");
    AssertEquivalent("Mars focus");
    Check(overview.Scene.Focus(overview.Camera,NativePresentationFocus.Earth)&&fullscreen.Scene.Focus(fullscreen.Camera,NativePresentationFocus.Earth),
        "both presets share Solar focus routing back to Earth");
    AssertEquivalent("Earth refocus");
    Console.WriteLine($"Solar preset camera convergence: outwardFrames={outwardFrames-1}; inwardFrames={inwardFrames-1}; wheelOrientationMax={maximumWheelOrientationStep:E3}rad; finalAltitude={overview.Scene.SurfaceAltitudeMetres:R}m; path=SolarSystemScene");
    Check(maximumWheelOrientationStep<=1e-12d,"shared wheel path preserves continuous orientation");

    (SolarSystemScene Scene,CameraState Camera) Create(string label)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var sceneValue,out var error)&&sceneValue is not null,
            $"{label} scene: {error}");
        var scene=sceneValue!;
        var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        scene.ResetPresentationCamera(camera);
        scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
        return(scene,camera);
    }

    void ApplyBoth(in NativeInputState input,string label,bool wheel)
    {
        var overviewOrientation=overview.Camera.Orientation;
        var fullscreenOrientation=fullscreen.Camera.Orientation;
        overview.Scene.ApplyPresentationInput(overview.Camera,input,out _,out _);
        fullscreen.Scene.ApplyPresentationInput(fullscreen.Camera,input,out _,out _);
        if(wheel)
        {
            maximumWheelOrientationStep=Math.Max(maximumWheelOrientationStep,QuaternionAngle(overviewOrientation,overview.Camera.Orientation));
            maximumWheelOrientationStep=Math.Max(maximumWheelOrientationStep,QuaternionAngle(fullscreenOrientation,fullscreen.Camera.Orientation));
        }
        AssertEquivalent(label);
    }

    void AssertEquivalent(string label)
    {
        var overviewPivot=Math.Sqrt((overview.Scene.CurrentFocusRoot-overview.Scene.FocusedBody.Position.Value).LengthSquared);
        var fullscreenPivot=Math.Sqrt((fullscreen.Scene.CurrentFocusRoot-fullscreen.Scene.FocusedBody.Position.Value).LengthSquared);
        Check(overview.Scene.CurrentFocusTarget==fullscreen.Scene.CurrentFocusTarget&&
            overview.Scene.CurrentCameraReferenceAuthority==fullscreen.Scene.CurrentCameraReferenceAuthority&&
            overview.Scene.SurfaceCameraMode==fullscreen.Scene.SurfaceCameraMode&&
            overview.Scene.SurfaceAnchorBlend==fullscreen.Scene.SurfaceAnchorBlend&&
            overview.Scene.SurfaceAltitudeMetres==fullscreen.Scene.SurfaceAltitudeMetres&&
            overview.Scene.OrbitDistance==fullscreen.Scene.OrbitDistance&&
            overview.Scene.OrbitYawRadians==fullscreen.Scene.OrbitYawRadians&&
            overview.Scene.OrbitPitchRadians==fullscreen.Scene.OrbitPitchRadians&&
            overviewPivot==fullscreenPivot&&overview.Scene.CurrentVisualAimRoot==fullscreen.Scene.CurrentVisualAimRoot&&
            overview.Camera.Position.Value==fullscreen.Camera.Position.Value&&overview.Camera.Orientation==fullscreen.Camera.Orientation,
            $"{label}: Solar Overview and Earth Fullscreen camera state remains identical");
    }
}

static void ZoomMotionProfileContinuityTest()
{
    var root = new ReferenceFrameId(1);
    const double minimumAltitude = SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres;
    var maximumDistance = SolAnalyticalDefinition.AstronomicalUnitMetres * SolarSystemScene.MaximumOverviewDistanceAu;
    var logarithmicStep = Math.Log(SolarCameraZoomPolicy.DistanceRatioPerDetent);
    var maximumDistanceDiscontinuity = 0d;
    var maximumNormalizedVelocityDiscontinuity = 0d;
    var maximumNormalizedAccelerationDiscontinuity = 0d;
    var maximumSymmetryError = 0d;
    var sawAcquisition = false;
    var sawMidpoint = false;
    var sawCompletion = false;
    var sawRelease = false;

    Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var candidate, out var error) && candidate is not null,
        $"zoom continuity scene: {error}");
    var scene = candidate!;
    var camera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, scene.Projection, CameraMode.Free);
    Check(scene.Focus(camera, NativePresentationFocus.Earth), "zoom continuity Earth focus");
    scene.ApplyPresentationInput(camera, new NativeInputState { PauseToggle = 1 }, out _, out _);
    var previousVelocity = double.NaN;
    var previousKind = scene.CurrentFocusTarget.Kind;

    for (var step = 0; step < 160 && scene.SurfaceAltitudeMetres > minimumAltitude * 1.01d; step++)
    {
        MeasureDetent(1, -1d, ref previousVelocity, $"inward {step}");
        sawAcquisition |= previousKind == FocusTargetKind.BodyCenter && scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor;
        sawMidpoint |= scene.SurfaceAnchorBlend > 0d && scene.SurfaceAnchorBlend < 1d;
        sawCompletion |= scene.SurfaceAnchorBlend == 1d;
        previousKind = scene.CurrentFocusTarget.Kind;
    }
    Check(scene.SurfaceAltitudeMetres >= minimumAltitude - 1e-5d && scene.SurfaceAltitudeMetres <= minimumAltitude * 1.01d,
        "inward zoom reaches but does not penetrate minimum terrain clearance");

    previousVelocity = double.NaN;
    for (var step = 0; step < 160 && scene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor; step++)
    {
        var kindBefore = scene.CurrentFocusTarget.Kind;
        MeasureDetent(-1, 1d, ref previousVelocity, $"outward {step}");
        sawRelease |= kindBefore == FocusTargetKind.SurfaceAnchor && scene.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter;
    }
    Check(sawAcquisition && sawMidpoint && sawCompletion && sawRelease,
        "zoom samples acquisition start, partial ownership, full ownership, and release");

    foreach (var target in new[] { "body", "partial", "full" })
    {
        Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var symmetryCandidate, out error) && symmetryCandidate is not null,
            $"zoom symmetry scene {target}: {error}");
        var symmetryScene = symmetryCandidate!;
        var symmetryCamera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, symmetryScene.Projection, CameraMode.Free);
        Check(symmetryScene.Focus(symmetryCamera, NativePresentationFocus.Earth), $"zoom symmetry Earth focus {target}");
        for (var step = 0; step < 160 && !AtTarget(); step++)
            symmetryScene.ApplyPresentationInput(symmetryCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        Check(AtTarget(), $"zoom symmetry reached {target} state");
        var beforeAltitude = symmetryScene.SurfaceAltitudeMetres;
        var beforePosition = symmetryCamera.Position.Value;
        var beforeOrientation = symmetryCamera.Orientation;
        symmetryScene.ApplyPresentationInput(symmetryCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
        symmetryScene.ApplyPresentationInput(symmetryCamera, new NativeInputState { MouseWheelDetents = -1 }, out _, out _);
        maximumSymmetryError = Math.Max(maximumSymmetryError, Math.Abs(symmetryScene.SurfaceAltitudeMetres - beforeAltitude) / Math.Max(1d, beforeAltitude));
        maximumSymmetryError = Math.Max(maximumSymmetryError, Math.Sqrt((symmetryCamera.Position.Value - beforePosition).LengthSquared) / Math.Max(1d, beforeAltitude));
        Check(symmetryCamera.Orientation == beforeOrientation && Double3.Dot(symmetryScene.CurrentInertialCameraOffset,
            beforePosition - symmetryScene.CurrentFocusRoot) > 0d, $"zoom reversal preserves inertial orientation and offset sign at {target}");

        bool AtTarget() => target switch
        {
            "body" => symmetryScene.CurrentFocusTarget.Kind == FocusTargetKind.BodyCenter && symmetryScene.SurfaceAltitudeMetres < 2_500_000d,
            "partial" => symmetryScene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && symmetryScene.SurfaceAnchorBlend > .15d && symmetryScene.SurfaceAnchorBlend < .85d,
            _ => symmetryScene.CurrentFocusTarget.Kind == FocusTargetKind.SurfaceAnchor && symmetryScene.SurfaceAnchorBlend == 1d && symmetryScene.SurfaceAltitudeMetres > 100_000d,
        };
    }

    var frameRateStates = new (float DeltaSeconds, SolarSystemScene Scene, CameraState Camera)[3];
    var frameDurations = new[] { 1f / 30f, 1f / 60f, 1f / 240f };
    for (var index = 0; index < frameRateStates.Length; index++)
    {
        Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var frameCandidate, out error) && frameCandidate is not null,
            $"zoom frame-rate scene {index}: {error}");
        var frameScene = frameCandidate!;
        var frameCamera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, frameScene.Projection, CameraMode.Free);
        Check(frameScene.Focus(frameCamera, NativePresentationFocus.Earth), $"zoom frame-rate Earth focus {index}");
        frameScene.ApplyPresentationInput(frameCamera, new NativeInputState { MouseWheelDetents = 1, DeltaSeconds = frameDurations[index] }, out _, out _);
        frameRateStates[index] = (frameDurations[index], frameScene, frameCamera);
    }
    Check(frameRateStates.All(state => state.Scene.OrbitDistance == frameRateStates[0].Scene.OrbitDistance &&
        state.Camera.Position.Value == frameRateStates[0].Camera.Position.Value), "wheel response is deterministic across host frame durations");

    Check(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var warpCandidate, out error) && warpCandidate is not null,
        $"zoom maximum-warp scene: {error}");
    var warpScene = warpCandidate!;
    var warpCamera = new CameraState(new FramePosition(root, Double3.Zero), DoubleQuaternion.Identity, warpScene.Projection, CameraMode.Free);
    Check(warpScene.Focus(warpCamera, NativePresentationFocus.Earth), "zoom maximum-warp Earth focus");
    while (warpScene.SpeedPresetIndex < SimulationSpeedPresets.Count - 1)
        warpScene.ApplyPresentationInput(warpCamera, new NativeInputState { RateIncrease = 1 }, out _, out _);
    warpScene.ApplyPresentationInput(warpCamera, new NativeInputState { MouseWheelDetents = 1 }, out _, out _);
    Check(warpScene.OrbitDistance == frameRateStates[0].Scene.OrbitDistance && warpCamera.Position.Value == frameRateStates[0].Camera.Position.Value,
        "maximum warp does not change zoom response for identical user input");

    var boundedAltitude = SolarCameraZoomPolicy.ApplyAltitude(1d, minimumAltitude, maximumDistance, 1000);
    var boundedMaximum = SolarCameraZoomPolicy.ApplyAltitude(maximumDistance, minimumAltitude, maximumDistance, -1000);
    Check(boundedAltitude == minimumAltitude && boundedMaximum == maximumDistance && double.IsFinite(boundedAltitude) && double.IsFinite(boundedMaximum),
        "continuous distance domain remains positive, finite, and bounded at ground and astronomical scales");
    Console.WriteLine($"Zoom motion continuity: distance={maximumDistanceDiscontinuity:E3} m; velocity={maximumNormalizedVelocityDiscontinuity:E3}; acceleration={maximumNormalizedAccelerationDiscontinuity:E3}; symmetry={maximumSymmetryError:E3}");
    Check(maximumDistanceDiscontinuity < .001d && maximumNormalizedVelocityDiscontinuity < 5e-5d &&
        maximumNormalizedAccelerationDiscontinuity < 5e-5d && maximumSymmetryError < 1e-9d,
        "zoom motion profile is continuous and symmetric through focus handoff");

    void MeasureDetent(int detents, double expectedNormalizedVelocity, ref double priorVelocity, string sample)
    {
        var beforeAltitude = scene.SurfaceAltitudeMetres;
        var beforeOrientation = camera.Orientation;
        var beforeOffset = scene.CurrentInertialCameraOffset;
        var radial = beforeOffset.Normalized();
        var maximumAltitude = SurfaceAnchorAcquisition.SurfaceAltitude(scene.FocusedBody,
            scene.FocusedBody.Position.Value + radial * maximumDistance, EarthPlanetaryScene.Terrain);
        var expectedAltitude = SolarCameraZoomPolicy.ApplyAltitude(beforeAltitude, minimumAltitude, maximumAltitude, detents);
        scene.ApplyPresentationInput(camera, new NativeInputState { MouseWheelDetents = detents }, out _, out _);
        var afterAltitude = scene.SurfaceAltitudeMetres;
        var expectedPublishedAltitude=expectedAltitude==minimumAltitude
            ?SurfaceFocusHandoffPolicy.TerrainClearanceCorrectionTargetMetres:expectedAltitude;
        maximumDistanceDiscontinuity = Math.Max(maximumDistanceDiscontinuity, Math.Abs(afterAltitude - expectedPublishedAltitude));
        if (expectedAltitude > minimumAltitude && expectedAltitude < maximumAltitude)
        {
            var normalizedVelocity = Math.Log(afterAltitude / beforeAltitude) / logarithmicStep;
            maximumNormalizedVelocityDiscontinuity = Math.Max(maximumNormalizedVelocityDiscontinuity,
                Math.Abs(normalizedVelocity - expectedNormalizedVelocity));
            if (double.IsFinite(priorVelocity))
                maximumNormalizedAccelerationDiscontinuity = Math.Max(maximumNormalizedAccelerationDiscontinuity,
                    Math.Abs(normalizedVelocity - priorVelocity));
            priorVelocity = normalizedVelocity;
        }
        Check(afterAltitude > 0d && double.IsFinite(afterAltitude) && scene.OrbitDistance > 0d && double.IsFinite(scene.OrbitDistance) &&
            camera.Position.Value.IsFinite && camera.Orientation == beforeOrientation && Double3.Dot(beforeOffset, scene.CurrentInertialCameraOffset) > 0d,
            $"{sample}: zoom is finite, positive, inertial, and does not invert (beforeAltitude={beforeAltitude:R}; afterAltitude={afterAltitude:R}; beforeOffset={beforeOffset}; afterOffset={scene.CurrentInertialCameraOffset}; blend={scene.SurfaceAnchorBlend:R}; demand={scene.BodyLocalCameraAltitudeDemandMetres:R})");
    }
}

static void SolarCameraBoundedDomainCrashRegressionTest()
{
    var root=new ReferenceFrameId(1);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var candidate,out var error)&&candidate is not null,
        $"bounded camera-domain scene: {error}");
    var scene=candidate!;var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(scene.Focus(camera,NativePresentationFocus.Earth),"bounded camera-domain Earth focus");
    scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
    for(var step=0;step<160&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||scene.SurfaceAnchorBlend<1d||scene.SurfaceAltitudeMetres>3_000d);step++)
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);
    Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d&&scene.SurfaceAltitudeMetres is >=10d and <=3_000d,
        $"bounded camera-domain reaches near-Earth surface-relative state: kind={scene.CurrentFocusTarget.Kind}; blend={scene.SurfaceAnchorBlend:R}; altitude={scene.SurfaceAltitudeMetres:R}");
    scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=(float)(Math.PI/.002d),MouseDeltaY=0f},out _,out _);

    var body=scene.FocusedBody;var terrain=EarthPlanetaryScene.Terrain;var maximumDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu;
    var orientation=(DoubleQuaternion.FromAxisAngle(Double3.UnitY,scene.OrbitYawRadians)*DoubleQuaternion.FromAxisAngle(Double3.UnitX,scene.OrbitPitchRadians)).Normalized();
    var radial=-orientation.Rotate(new Double3(0d,0d,-1d));
    var currentAltitude=SurfaceAnchorAcquisition.SurfaceAltitude(body,camera.Position.Value,terrain);
    var bodyLineMaximum=SurfaceAnchorAcquisition.SurfaceAltitude(body,body.Position.Value+radial*maximumDistance,terrain);
    var detents=-1000;
    var formerlyRequestedAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
        bodyLineMaximum,detents);
    Check(scene.CurrentFocusTarget.TryEvaluate(body,out var anchorRoot),"bounded camera-domain evaluates active SurfaceAnchor");
    var activeLineMaximum=SurfaceAnchorAcquisition.SurfaceAltitude(body,scene.CurrentVisualAimRoot+radial*maximumDistance,terrain);
    var requestedAltitude=SolarCameraZoomPolicy.ApplyAltitude(Math.Max(0d,currentAltitude),SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
        SolarCameraZoomPolicy.MaximumSurfaceAltitude(body,scene.CurrentVisualAimRoot,radial,terrain,maximumDistance),detents);
    Console.WriteLine($"Bounded camera-domain reproduction: current={currentAltitude:R} m; detents={detents}; formerRequested={formerlyRequestedAltitude:R} m; activeLineMaximum={activeLineMaximum:R} m; formerExcess={formerlyRequestedAltitude-activeLineMaximum:R} m; correctedRequested={requestedAltitude:R} m; maximumOffset={maximumDistance:R} m");

    Exception? failure=null;
    try{scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=detents},out _,out _);}catch(Exception caught){failure=caught;}
    Check(failure is null,$"batched outward zoom after near-Earth drag remains inside the legitimate bounded camera domain: {failure}");
    var expectedAltitude=SolarCameraZoomPolicy.MaximumSurfaceAltitude(body,scene.CurrentVisualAimRoot,radial,terrain,maximumDistance);
    Check(requestedAltitude<=activeLineMaximum&&Math.Abs(scene.SurfaceAltitudeMetres-expectedAltitude)<.001d&&scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres&&
        camera.Position.Value.IsFinite&&scene.CurrentInertialCameraOffset.IsFinite&&Double3.Dot(scene.CurrentInertialCameraOffset,radial)>0d,
        "corrected request stays inside its exact ray domain without terrain penetration or camera-offset inversion");

    var minimumObservedAltitude=scene.SurfaceAltitudeMetres;var maximumDomainExcess=0d;var maximumInvariantError=0d;
    for(var cycle=0;cycle<24;cycle++)
    {
        foreach(var input in new[]{
            new NativeInputState{MouseWheelDetents=1000},
            new NativeInputState{LookActive=1,MouseDeltaX=cycle%2==0?237f:-311f,MouseDeltaY=cycle%3==0?71f:-53f},
            new NativeInputState{MouseWheelDetents=-1000}})
        {
            scene.ApplyPresentationInput(camera,input,out _,out _);
            var stateRadial=scene.CurrentInertialCameraOffset.Normalized();
            var stateMaximum=SolarCameraZoomPolicy.MaximumSurfaceAltitude(scene.FocusedBody,scene.CurrentVisualAimRoot,stateRadial,terrain,maximumDistance);
            minimumObservedAltitude=Math.Min(minimumObservedAltitude,scene.SurfaceAltitudeMetres);
            maximumDomainExcess=Math.Max(maximumDomainExcess,scene.SurfaceAltitudeMetres-stateMaximum);
            maximumInvariantError=Math.Max(maximumInvariantError,Math.Sqrt((camera.Position.Value-(scene.CurrentFocusRoot+scene.CurrentInertialCameraOffset)).LengthSquared));
            Check(scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres-.001d&&scene.SurfaceAltitudeMetres<=stateMaximum+.001d&&
                double.IsFinite(scene.SurfaceAltitudeMetres)&&double.IsFinite(scene.OrbitDistance)&&camera.Position.Value.IsFinite&&scene.CurrentInertialCameraOffset.IsFinite,
                $"drag/zoom stress cycle {cycle} remains finite, clear, and inside its ray-specific camera domain");
        }
    }
    scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1000},out _,out _);
    Check(scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.HasRetainedVisualAim,"stress returns to near-surface anchor ownership");
    scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=(float)(Math.PI/.002d),MouseDeltaY=0f},out _,out _);
    for(var outward=0;outward<180;outward++)
    {
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-2},out _,out _);
        var stateRadial=scene.CurrentInertialCameraOffset.Normalized();
        var stateMaximum=SolarCameraZoomPolicy.MaximumSurfaceAltitude(scene.FocusedBody,scene.CurrentVisualAimRoot,stateRadial,terrain,maximumDistance);
        maximumDomainExcess=Math.Max(maximumDomainExcess,scene.SurfaceAltitudeMetres-stateMaximum);
        Check(scene.SurfaceAltitudeMetres<=stateMaximum+.001d&&double.IsFinite(scene.SurfaceAltitudeMetres),
            $"paced outward release frame {outward} remains inside the updated visual-aim ray domain");
    }
    Console.WriteLine($"Bounded camera-domain stress: cycles=24; minimumAltitude={minimumObservedAltitude:R} m; maximumDomainExcess={maximumDomainExcess:E3} m; invariant={maximumInvariantError:E3} m");
    Check(minimumObservedAltitude>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres-.001d&&maximumDomainExcess<=.003d&&maximumInvariantError<.003d,
        "repeated near-Earth drag/zoom cycles preserve clearance and the 3D-1 positional invariant");
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
    Check(Marshal.SizeOf<NativePlanetaryPresentation>()==192&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.BodyIdLow)).ToInt32()==48&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.Roughness)).ToInt32()==64&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.ProjectionKind)).ToInt32()==80&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingInnerRadiusRatio)).ToInt32()==96&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingOrientationX)).ToInt32()==112&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.RingColorR)).ToInt32()==128&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.BodyOrientationX)).ToInt32()==144&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.LocalDetailScaleMeters)).ToInt32()==160&&Marshal.OffsetOf<NativePlanetaryPresentation>(nameof(NativePlanetaryPresentation.CenterLowX)).ToInt32()==176,"material, ring, body-orientation, local-detail, and compensated-center ABI layout");
}

static void PlanetaryPresentationSpirvStrideTest()
{
    var shaderSourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "native", "NovaCore.Native", "shaders"));
    var shaderBinaryDirectory = Path.GetFullPath(Path.Combine(shaderSourceDirectory, "..", "..", "..", "build", "native-ninja", "shaders"));
    string[] expectedConsumers =
    [
        "anchored_terrain.vert",
        "anchored_terrain.tese",
        "distant_planet.vert",
        "planetary.vert",
        "planetary_ring.vert",
        "production_nested_scale_mesh_cull.comp",
        "production_nested_scale_mesh_incoming_cull.comp",
        "production_spherical_billboard.tese",
        "production_spherical_billboard.vert",
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
        Check(File.ReadAllText(sourcePath).Contains("vec4 localDetail; vec4 centerLow;", StringComparison.Ordinal), $"{shader} includes localDetail and compensated center in Presentation ABI");
        var binaryPath = Path.Combine(shaderBinaryDirectory, shader + ".spv");
        Check(File.Exists(binaryPath), $"compiled SPIR-V exists for {shader}");
        var presentation = ReadSpirvStructLayout(binaryPath, "Presentation");
        var expectedOffsets = new Dictionary<string, uint>
        {
            ["centerRadius"] = 0, ["colorDistant"] = 16, ["blendMetricState"] = 32, ["identity"] = 48,
            ["surface"] = 64, ["hooks"] = 80, ["ringGeometry"] = 96, ["ringOrientation"] = 112,
            ["ringColor"] = 128, ["bodyOrientation"] = 144, ["localDetail"] = 160, ["centerLow"] = 176
        };
        Check(presentation.ArrayStride == 192u, $"{shader} Presentation ArrayStride is 192, actual {presentation.ArrayStride}");
        Check(presentation.MemberOffsets.Count == expectedOffsets.Count && expectedOffsets.All(expected =>
            presentation.MemberOffsets.TryGetValue(expected.Key, out var actual) && actual == expected.Value),
            $"{shader} complete Presentation member offsets");
        Console.WriteLine($"Presentation ABI {shader}.spv: stride={presentation.ArrayStride}; members={presentation.MemberOffsets.Count}");
    }

    foreach(var shader in new[]{"distant_planet.vert","planetary.vert","planetary_select.comp","planetary_terrain_generate.comp"})
    {
        var binaryPath=Path.Combine(shaderBinaryDirectory,shader+".spv");
        var input=ReadSpirvStructLayout(binaryPath,shader=="distant_planet.vert"?"PlanetaryInput":"Input");
        Check(input.Bindings.SequenceEqual(new uint[]{2}),$"{shader} projected-demand input is binding 2");
        Check(input.MemberOffsets.TryGetValue("textureDemand",out var demandOffset)&&demandOffset==80u,$"{shader} projected-demand member offset is 80");
        Console.WriteLine($"Projected-demand ABI {shader}.spv: textureDemand={demandOffset}; size=96");
    }
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
    var terrain = new PlanetaryTerrainDefinition(1, 1, 7_600d);
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

static void PlanetarySurfaceCameraPresentationTest()
{
    Check(PlanetarySurfaceCameraPolicy.Mode(1_000_000)==PlanetaryCameraPresentationMode.Orbital&&PlanetarySurfaceCameraPolicy.Mode(500_000)==PlanetaryCameraPresentationMode.Transition&&PlanetarySurfaceCameraPolicy.Mode(100_000)==PlanetaryCameraPresentationMode.SurfaceLocal,"camera mode altitude boundaries");
    Check(PlanetarySurfaceCameraPolicy.SurfaceBlend(1_000_000)==0&&PlanetarySurfaceCameraPolicy.SurfaceBlend(100_000)==1&&PlanetarySurfaceCameraPolicy.ZoomFactor(1_000)<PlanetarySurfaceCameraPolicy.ZoomFactor(1_000_000),"camera transition and fine zoom are deterministic");
    Check(Math.Abs(PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(2)-60.05d)<1e-12d&&
        PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(100_000)==1_000d&&
        PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(2,true)==300.25d&&
        Math.Abs(PlanetarySurfaceCameraPolicy.TranslationSpeedMetresPerSecond(2,false,true)-12.01d)<1e-12d,
        "SurfaceLocal translation speed is useful, bounded, altitude-aware, and modifier-scaled");
    var frame=PlanetarySurfaceFrame.AtDirection(Double3.UnitZ);var a=frame.LookOrientation(.25,-.2);var b=frame.LookOrientation(.25,-.2);Check(a==b&&Math.Abs(a.LengthSquared-1)<1e-12,"local tangent camera orientation deterministic");
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
    Console.WriteLine($"Planetary patch topology hash: 0x{topology.DeterministicHash:X16}");
    Check(topology.Vertices.Length==289&&topology.Indices.Length==1536,"patch grid counts");Check(topology.DeterministicHash==0x98792D7EBC45FF6DUL&&topology.DeterministicHash==repeated.DeterministicHash,"patch topology regression hash");
    Check(topology.Indices.All(index=>index<topology.Vertices.Length),"patch indices in range");Check(topology.Vertices.All(vertex=>vertex.U is >=0 and <=1&&vertex.V is >=0 and <=1),"patch coordinates bounded");Check(topology.Vertices[0]==new PlanetaryPatchTopology.Vertex(0,0)&&topology.Vertices[^1]==new PlanetaryPatchTopology.Vertex(1,1),"patch corners");
    Check(Marshal.SizeOf<NativePlanetaryPatch>()==64,"planetary patch ABI size");Check(Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.Face)).ToInt32()==0&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.CenterX)).ToInt32()==16&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.ColorR)).ToInt32()==32&&Marshal.OffsetOf<NativePlanetaryPatch>(nameof(NativePlanetaryPatch.StitchMask)).ToInt32()==48,"planetary patch ABI offsets");
    var patch=new NativePlanetaryPatch{Face=5,Level=3,X=7,Y=6,CenterX=BitConverter.Int32BitsToSingle(unchecked((int)0x3F800001)),CenterY=2,CenterZ=3,Radius=BitConverter.Int32BitsToSingle(unchecked((int)0x41200001)),ColorA=1,StitchMask=15};Check(patch.Face==5&&patch.Level==3&&patch.X==7&&patch.Y==6&&patch.StitchMask==15&&BitConverter.SingleToInt32Bits(patch.CenterX)==unchecked((int)0x3F800001)&&BitConverter.SingleToInt32Bits(patch.Radius)==unchecked((int)0x41200001),"planetary patch ABI bit preservation");
    Check(NativeRuntime.ValidatePlanetaryPatches(null,0)==NativeResult.Success,"native zero patch batch");Check(NativeRuntime.ValidatePlanetaryPatches(null,1)==NativeResult.InvalidArgument,"native null nonzero rejected");var pointer=&patch;Check(NativeRuntime.ValidatePlanetaryPatches(pointer,1)==NativeResult.Success,"native valid patch");var batch=stackalloc NativePlanetaryPatch[6];for(uint face=0;face<6;face++){batch[face]=patch;batch[face].Face=face;}Check(NativeRuntime.ValidatePlanetaryPatches(batch,6)==NativeResult.Success,"native six face batch");batch[0].Face=6;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native face rejected");batch[0]=patch;batch[0].Radius=0;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native zero radius rejected");batch[0]=patch;batch[0].Radius=float.NaN;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native nan radius rejected");batch[0]=patch;batch[0].CenterX=float.PositiveInfinity;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native infinite center rejected");batch[0]=patch;batch[0].Level=2;batch[0].X=4;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native level coordinates rejected");batch[0]=patch;batch[0].StitchMask=16;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native stitch mask rejected");batch[0]=patch;batch[0].Reserved0=1;Check(NativeRuntime.ValidatePlanetaryPatches(batch,1)==NativeResult.InvalidArgument,"native reserved metadata rejected");
}

static void TerrainV5PayloadSeamAndFloridaClassificationTest()
{
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    Check(TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthAssetId,null,out var manifest,out var packPath,out var resolveError),$"terrain-v5 continuity asset: {resolveError}");
    Check(manifest.Sha256=="38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae","terrain-v5 canonical-gutter content identity");
    var payloads=new Dictionary<PlanetarySurfacePatchId,(byte[] Albedo,ushort[] Elevation,byte[] Land)>();
    using(var stream=File.OpenRead(packPath))
    {
        Span<byte> headerBytes=stackalloc byte[PlanetaryCubeSurfacePackContract.HeaderBytes];stream.ReadExactly(headerBytes);
        Check(PlanetaryCubeSurfacePackContract.TryReadHeader(headerBytes,out var header)&&header.IsProductionLayout&&header.MaximumLevel==2&&header.RecordCount==126,"terrain-v5 bounded L0-L2 header");
        var recordBytes=new byte[PlanetaryCubeSurfacePackContract.RecordHeaderBytes];
        var shippedOrdinals=new HashSet<ulong>();
        for(var index=0;index<header.RecordCount;index++)
        {
            stream.ReadExactly(recordBytes);Check(PlanetaryCubeSurfacePackContract.TryReadRecordHeader(recordBytes,out var record)&&shippedOrdinals.Add(record.PatchOrdinal),$"terrain-v5 unique record {index}");
            var albedo=new byte[checked((int)record.AlbedoBytes)];var encodedElevation=new byte[checked((int)record.ElevationBytes)];var land=new byte[checked((int)record.LandMaskBytes)];
            stream.ReadExactly(albedo);stream.ReadExactly(encodedElevation);stream.ReadExactly(land);stream.Seek(record.CloudBytes,SeekOrigin.Current);
            var elevation=new ushort[encodedElevation.Length/2];Buffer.BlockCopy(encodedElevation,0,elevation,0,encodedElevation.Length);
            payloads.Add(record.Patch,(albedo,elevation,land));
        }
        Check(stream.Position==stream.Length&&payloads.Count==126&&shippedOrdinals.Count==126,"terrain-v5 continuity proof reads every coherent transaction exactly once");
    }

    // Quantify the geometric authority discontinuity that would result if the
    // complete L2 visual fallback and the retained near-field oracle were
    // treated as interchangeable radial surfaces.  This is diagnostic evidence
    // for ownership convergence; it deliberately does not alter either source.
    var validationDirection=BodyFixedGeography.DirectionFromLatitudeLongitude(-45d*Math.PI/180d,-70d*Math.PI/180d);
    var validationFrame=PlanetarySurfaceFrame.AtDirection(validationDirection);
    var globalMinimum=double.PositiveInfinity;var globalMaximum=double.NegativeInfinity;
    var oracleMinimum=double.PositiveInfinity;var oracleMaximum=double.NegativeInfinity;
    var maximumGlobalOracleDelta=0d;var meanGlobalOracleDelta=0d;var authoritySamples=0;
    for(var northMetres=-32_000d;northMetres<=32_000d;northMetres+=2_000d)
    for(var eastMetres=-32_000d;eastMetres<=32_000d;eastMetres+=2_000d)
    {
        var direction=(validationDirection+validationFrame.East*(eastMetres/6_378_137d)+
            validationFrame.North*(northMetres/6_378_137d)).Normalized();
        Check(RelaxedCubeSphereProjection.TryAddress(direction,out var face,out var u,out var v),"authority comparison address");
        var global=Math.Max(0d,ElevationMetres(SampleFace(face,2,u,v).Elevation));
        var oracle=EarthElevationDataset.SampleHeight(direction);
        globalMinimum=Math.Min(globalMinimum,global);globalMaximum=Math.Max(globalMaximum,global);
        oracleMinimum=Math.Min(oracleMinimum,oracle);oracleMaximum=Math.Max(oracleMaximum,oracle);
        var delta=Math.Abs(global-oracle);maximumGlobalOracleDelta=Math.Max(maximumGlobalOracleDelta,delta);
        meanGlobalOracleDelta+=delta;authoritySamples++;
    }
    meanGlobalOracleDelta/=authoritySamples;
    Check(authoritySamples==1089&&double.IsFinite(maximumGlobalOracleDelta),"bounded global/oracle authority comparison");
    Console.WriteLine($"Near-field height authority comparison: samples={authoritySamples}; footprint=64km; terrainV5L2=[{globalMinimum:F3},{globalMaximum:F3}]m; oracle8192=[{oracleMinimum:F3},{oracleMaximum:F3}]m; deltaMean={meanGlobalOracleDelta:F3}m; deltaMax={maximumGlobalOracleDelta:F3}m");

    var maximumInternalElevation=0d;var maximumInternalAlbedo=0d;var maximumInternalLand=0d;
    var maximumFaceElevation=0d;var maximumFaceAlbedo=0d;var maximumFaceLand=0d;
    var maximumCornerElevation=0d;var maximumCornerAlbedo=0d;var maximumCornerLand=0d;
    var edges=new[]{PlanetaryPatchEdge.NegativeU,PlanetaryPatchEdge.PositiveU,PlanetaryPatchEdge.NegativeV,PlanetaryPatchEdge.PositiveV};
    foreach(var level in new[]{0,1,2})
    {
        var side=1<<level;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        {
            for(var boundary=1;boundary<side;boundary++)for(var along=0;along<side;along++)foreach(var t in new[]{0d,.25d,.5d,.75d,1d})
            {
                Compare(Sample(new(6,5,face,level,boundary-1,along),1d,t),Sample(new(6,5,face,level,boundary,along),0d,t),ref maximumInternalElevation,ref maximumInternalAlbedo,ref maximumInternalLand);
                Compare(Sample(new(6,5,face,level,along,boundary-1),t,1d),Sample(new(6,5,face,level,along,boundary),t,0d),ref maximumInternalElevation,ref maximumInternalAlbedo,ref maximumInternalLand);
            }
            foreach(var edge in edges)
            {
                var transition=CubeSphereAdjacency.GetTransition(face,edge);
                for(var sample=0;sample<=64;sample++)
                {
                    var t=sample/64d;var neighborT=transition.Reversed?1d-t:t;
                    Compare(SampleEdge(face,level,edge,t),SampleEdge(transition.NeighborFace,level,transition.NeighborEdge,neighborT),ref maximumFaceElevation,ref maximumFaceAlbedo,ref maximumFaceLand);
                }
            }
        }
        var corners=new Dictionary<(int X,int Y,int Z),List<(double R,double G,double B,double Elevation,double Land)>>();
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())foreach(var u in new[]{0d,1d})foreach(var v in new[]{0d,1d})
        {
            var direction=RelaxedCubeSphereProjection.UnitDirection(face,u,v);var key=(Math.Sign(direction.X),Math.Sign(direction.Y),Math.Sign(direction.Z));
            if(!corners.TryGetValue(key,out var values))corners[key]=values=[];values.Add(SampleFace(face,level,u,v));
        }
        Check(corners.Count==8&&corners.Values.All(values=>values.Count==3),$"level {level} has eight canonical triple-face corners");
        foreach(var values in corners.Values)for(var index=1;index<values.Count;index++)Compare(values[0],values[index],ref maximumCornerElevation,ref maximumCornerAlbedo,ref maximumCornerLand);
    }

    var minimumPayloadElevationMetres=double.PositiveInfinity;var maximumPayloadElevationMetres=double.NegativeInfinity;
    foreach(var payload in payloads.Values)foreach(var encoded in payload.Elevation){var metres=-11_000d+encoded*(20_000d/65_535d);minimumPayloadElevationMetres=Math.Min(minimumPayloadElevationMetres,metres);maximumPayloadElevationMetres=Math.Max(maximumPayloadElevationMetres,metres);}
    var maximumRawParentChildElevationMetres=0d;var maximumRawParentChildAlbedo=0d;var maximumRawParentChildLand=0d;
    var maximumConstrainedEdgePositionGapMetres=0d;var maximumConstrainedEdgeMaterialGap=0d;var maximumConstrainedEdgeLandGap=0d;var maximumConstrainedEdgeNormalAngleRadians=0d;var maximumQuarterMorphStepMetres=0d;
    var maximumUncoordinatedMorphGapMetres=0d;var maximumCoherentMorphGapMetres=0d;var maximumEdgeAgeDiscrepancy=0d;var maximumParentAddressDirectionError=0d;var constrainedVertexCount=0;
    var maximumActualEdgePositionGapMetres=0d;var maximumOneSidedNormalAngleRadians=0d;var maximumGutterNormalAngleRadians=0d;
    foreach(var level in new[]{1,2})
    {
        var side=1<<level;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())for(var y=0;y<side;y++)for(var x=0;x<side;x++)
        {
            var childId=new PlanetarySurfacePatchId(6,5,face,level,x,y);var parentId=new PlanetarySurfacePatchId(6,5,face,level-1,x>>1,y>>1);
            for(var gridY=0;gridY<=16;gridY++)for(var gridX=0;gridX<=16;gridX++)
            {
                var childU=(x+gridX/16d)/(1<<level);var childV=(y+gridY/16d)/(1<<level);
                var parentU=((x>>1)+((x&1)*.5d+gridX/32d))/(1<<(level-1));var parentV=((y>>1)+((y&1)*.5d+gridY/32d))/(1<<(level-1));
                maximumParentAddressDirectionError=Math.Max(maximumParentAddressDirectionError,Math.Sqrt((RelaxedCubeSphereProjection.UnitDirection(face,childU,childV)-RelaxedCubeSphereProjection.UnitDirection(face,parentU,parentV)).LengthSquared));
            }
            for(var edge=0;edge<4;edge++)for(var index=0;index<=16;index++)
            {
                var along=index/16d;var u=edge switch{0=>0d,1=>1d,_=>along};var v=edge switch{2=>0d,3=>1d,_=>along};
                var parentU=((x&1)+u)*.5d;var parentV=((y&1)+v)*.5d;var parent=Sample(parentId,parentU,parentV);var child=Sample(childId,u,v);
                var elevationDeltaMetres=Math.Abs(parent.Elevation-child.Elevation)*(20_000d/65_535d);var albedoDelta=Math.Max(Math.Abs(parent.R-child.R),Math.Max(Math.Abs(parent.G-child.G),Math.Abs(parent.B-child.B)));var landDelta=Math.Abs(parent.Land-child.Land);
                maximumRawParentChildElevationMetres=Math.Max(maximumRawParentChildElevationMetres,elevationDeltaMetres);maximumRawParentChildAlbedo=Math.Max(maximumRawParentChildAlbedo,albedoDelta);maximumRawParentChildLand=Math.Max(maximumRawParentChildLand,landDelta);
                var previous=parent.Elevation;for(var step=1;step<=4;step++){var visible=Lerp(parent.Elevation,child.Elevation,step*.25d);maximumQuarterMorphStepMetres=Math.Max(maximumQuarterMorphStepMetres,Math.Abs(visible-previous)*(20_000d/65_535d));previous=visible;}
                // A stitched fine edge deliberately evaluates the parent endpoint for
                // geometry, macro material, classification, and the edge normal.  The
                // coarse neighbor is the same parent-level canonical surface, so every
                // rendered-surface quantity has an exact common value at the boundary.
                maximumConstrainedEdgePositionGapMetres=Math.Max(maximumConstrainedEdgePositionGapMetres,Math.Abs(parent.Elevation-parent.Elevation));
                maximumConstrainedEdgeMaterialGap=Math.Max(maximumConstrainedEdgeMaterialGap,Math.Max(Math.Abs(parent.R-parent.R),Math.Max(Math.Abs(parent.G-parent.G),Math.Abs(parent.B-parent.B))));
                maximumConstrainedEdgeLandGap=Math.Max(maximumConstrainedEdgeLandGap,Math.Abs(parent.Land-parent.Land));maximumConstrainedEdgeNormalAngleRadians=Math.Max(maximumConstrainedEdgeNormalAngleRadians,0d);
            }
        }
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        {
            for(var boundary=1;boundary<side;boundary++)for(var alongPatch=0;alongPatch<side;alongPatch++)for(var sample=0;sample<=16;sample++)
            {
                var t=sample/16d;
                CompareMorphBoundary(
                    new(6,5,face,level,boundary-1,alongPatch),1d,t,
                    new(6,5,face,level,boundary,alongPatch),0d,t);
                CompareMorphBoundary(
                    new(6,5,face,level,alongPatch,boundary-1),t,1d,
                    new(6,5,face,level,alongPatch,boundary),t,0d);
            }
        }
    }
    for(var edge=0;edge<4;edge++)for(var y=0;y<=16;y++)for(var x=0;x<=16;x++)
    {
        var stitchedX=x;var stitchedY=y;
        if(edge<2&&x==(edge==0?0:16))stitchedY=(y/2)*2;
        if(edge>=2&&y==(edge==2?0:16))stitchedX=(x/2)*2;
        var changed=stitchedX!=x||stitchedY!=y;
        var intended=edge<2?x==(edge==0?0:16)&&y%2==1:y==(edge==2?0:16)&&x%2==1;
        Check(changed==intended,$"stitch edge {edge} constrains only the intended odd shared-edge vertices");
        if(changed){constrainedVertexCount++;Check((edge<2?stitchedY:stitchedX)%2==0,"stitched fine vertex collapses to an exact coarse-grid vertex");}
    }
    foreach(var level in new[]{1,2})
    {
        var side=1<<level;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())for(var boundary=1;boundary<side;boundary++)for(var along=0;along<side;along++)for(var sample=0;sample<=16;sample++)
        {
            var aU=new PlanetarySurfacePatchId(6,5,face,level,boundary-1,along);var bU=new PlanetarySurfacePatchId(6,5,face,level,boundary,along);
            var aV=new PlanetarySurfacePatchId(6,5,face,level,along,boundary-1);var bV=new PlanetarySurfacePatchId(6,5,face,level,along,boundary);
            Measure(aU,16,sample,bU,0,sample);Measure(aV,sample,16,bV,sample,0);
        }
    }
    Check(constrainedVertexCount==32,"each of the four 17-vertex edges constrains exactly eight odd vertices and no interior/opposite-edge vertex");
    Check(maximumRawParentChildElevationMetres>100d&&maximumRawParentChildAlbedo>.1d,"terrain-v5 fixture exercises a materially different parent/child surface rather than an accidentally identical hierarchy");
    Check(maximumUncoordinatedMorphGapMetres>1d,"the shipped terrain proves that independent per-patch demand morph weights open a visible same-level edge");
    for(var ownAge=0;ownAge<=31;ownAge++)for(var neighborAge=0;neighborAge<=31;neighborAge++){var sharedAge=Math.Min(ownAge,neighborAge);maximumEdgeAgeDiscrepancy=Math.Max(maximumEdgeAgeDiscrepancy,Math.Abs(sharedAge-sharedAge));}
    Check(maximumCoherentMorphGapMetres<1e-9d&&maximumEdgeAgeDiscrepancy==0d,"one packed shared edge age preserves exact same-level edge correspondence for every transition age pair");
    Check(maximumConstrainedEdgePositionGapMetres==0d&&maximumConstrainedEdgeMaterialGap==0d&&maximumConstrainedEdgeLandGap==0d&&maximumConstrainedEdgeNormalAngleRadians==0d,"mixed-LOD fine edges reproduce the exact coarse rendered-surface authority");
    Check(maximumQuarterMorphStepMetres<=maximumRawParentChildElevationMetres*.2500000001d,"bounded parent-child geomorph changes elevation continuously without an instantaneous child displacement switch");
    Check(maximumParentAddressDirectionError<1e-15d,"native child-to-parent half-domain addressing represents the exact same relaxed-cube geographic direction");
    Check(maximumActualEdgePositionGapMetres<1e-8d&&maximumOneSidedNormalAngleRadians>1e-5d&&maximumGutterNormalAngleRadians<1e-5d,"real terrain-v5 shared positions are closed while one-sided edge normals are measurably discontinuous and canonical gutter normals agree");
    Check(double.IsFinite(minimumPayloadElevationMetres)&&double.IsFinite(maximumPayloadElevationMetres)&&minimumPayloadElevationMetres>=-11_000d&&maximumPayloadElevationMetres<=9_000d,"all real terrain-v5 payload elevations remain inside the signed production geometry envelope");

    var shaderRoot=Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders");var selectorSource=File.ReadAllText(Path.Combine(shaderRoot,"planetary_select.comp"));var vertexSource=File.ReadAllText(Path.Combine(shaderRoot,"planetary.vert"));var fragmentSource=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production.frag"));var authoritySource=File.ReadAllText(Path.Combine(shaderRoot,"planetary_physical_authority.glsl"));var localSource=File.ReadAllText(Path.Combine(shaderRoot,"local_terrain.glsl"));var nativeSource=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    Check(vertexSource.Contains("surfaceMorph=temporalMorph",StringComparison.Ordinal)&&!vertexSource.Contains("DemandMorph",StringComparison.Ordinal)&&vertexSource.Contains("ConstrainedMorph(mask,p.transitions.w,p.transitions.z",StringComparison.Ordinal)&&vertexSource.Contains("return 0.0",StringComparison.Ordinal)&&vertexSource.Contains("return 1.0",StringComparison.Ordinal),"temporal morph ownership uses retained-parent fine edges and current-coarse reverse edges instead of spatially divergent per-patch demand morphing");
    Check(selectorSource.Contains("TransitionAge",StringComparison.Ordinal)&&selectorSource.Contains("packedAges",StringComparison.Ordinal)&&selectorSource.Contains("FinerOutputNeighbor",StringComparison.Ordinal)&&selectorSource.Contains("transitionsSettled",StringComparison.Ordinal)&&selectorSource.Contains("ProductionPatchBindingCurrent",StringComparison.Ordinal)&&selectorSource.Contains("payloadBindingsCurrent",StringComparison.Ordinal)&&selectorSource.Contains("patchData.patches[index].transitions.w=finerNeighborMask",StringComparison.Ordinal),"selector publishes exact shared edge ages and reverse coarse/fine ownership and cannot fast-reuse an in-progress transition or a stale zero payload-layer binding");
    Check(vertexSource.Contains("struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; uvec4 transitions; };",StringComparison.Ordinal)&&selectorSource.Contains("struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; uvec4 transitions; };",StringComparison.Ordinal)&&vertexSource.Contains("p.transitions.z",StringComparison.Ordinal)&&vertexSource.Contains("p.transitions.w",StringComparison.Ordinal),"native/shader patch ABI remains four 16-byte records (64 bytes), with packed edge ages and reverse-edge mask contained in the existing transition record");
    Check(fragmentSource.Contains("surfaceWeight=productionTransition.x*MixedLodEdgeWeight",StringComparison.Ordinal)&&fragmentSource.Contains("visible.albedo=mix(parent.albedo,visible.albedo,surfaceWeight)",StringComparison.Ordinal)&&fragmentSource.Contains("visible.elevation=mix(parent.elevation,visible.elevation,surfaceWeight)",StringComparison.Ordinal)&&fragmentSource.Contains("visible.land=mix(parent.land,visible.land,surfaceWeight)",StringComparison.Ordinal),"global macro material, classification, and elevation share the geometry promotion state rather than independently selecting resident LOD");
    Check(authoritySource.Contains("CanonicalPhysicalHeight",StringComparison.Ordinal)&&vertexSource.Contains("CanonicalPhysicalHeight(direction)",StringComparison.Ordinal)&&fragmentSource.Contains("vec3 physical=normalize(normal)",StringComparison.Ordinal)&&!fragmentSource.Contains("materialBenchmark",StringComparison.Ordinal)&&!fragmentSource.Contains("ProductionFixedPhysicalNormal",StringComparison.Ordinal)&&fragmentSource.Contains("mix(analyticSphere,physical,landWeight)",StringComparison.Ordinal),"every production geometry owner uses one body-fixed physical height authority and fragment lighting consumes its prepared normal with a continuous analytic sea-level blend and no profiling bypass");
    Check(nativeSource.Contains("terrain[sample]=ProductionSampleElevation(a.productionElevationCpu[parentLayer]",StringComparison.Ordinal)&&nativeSource.Contains("terrain[sample+1]=ProductionSampleElevation(payload.elevation",StringComparison.Ordinal)&&nativeSource.Contains("ProductionHierarchyPayloadsReady",StringComparison.Ordinal)&&vertexSource.Contains("binding=9) readonly buffer TerrainSamples { vec2 heights[]; }",StringComparison.Ordinal)&&vertexSource.Contains("binding=10) readonly buffer PatchTerrainSlots { uvec2 values[]; }",StringComparison.Ordinal),"native two-float parent/current endpoint transport remains byte-compatible for non-immutable terrain users");
    Check(selectorSource.Contains("uint slot=ProductionPatchOrdinal(keyB.x,keyB.y,keyB.z,keyB.w)",StringComparison.Ordinal)&&selectorSource.Contains("patchTerrain.values[index]=uvec2(slot,slot+1u)",StringComparison.Ordinal)&&selectorSource.Contains("production?cacheHighWater:0u",StringComparison.Ordinal)&&!vertexSource.Contains("productionElevation",StringComparison.Ordinal)&&vertexSource.Contains("planetary_physical_authority.glsl",StringComparison.Ordinal)&&fragmentSource.Contains("layout(set=0,binding=25) uniform sampler2DArray productionElevation",StringComparison.Ordinal)&&nativeSource.Contains("terrainBindings=%u",StringComparison.Ordinal)&&nativeSource.Contains("NOVACORE_PRODUCTION_BOOTSTRAP_DELAY_MS",StringComparison.Ordinal),"the complete global owner publishes synchronously before rendering, uses the canonical oracle for geometry, and retains terrain-v5 only as its presentation payload");
    Check(fragmentSource.Contains("SampleLocalTerrainMaterial(samplingDirection)",StringComparison.Ordinal)&&
          fragmentSource.Contains("if(localSample.resident)",StringComparison.Ordinal)&&
          !fragmentSource.Contains("if(anchored&&localSample.resident)",StringComparison.Ordinal)&&
          localSource.Contains("LocalTerrainCoverage",StringComparison.Ordinal),
          "local-v2 material follows the same canonical body-fixed lookup for global and dynamic pixels and fades at incomplete sparse-footprint boundaries");

    var floridaDirection=BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,FloridaLaunchSite.Longitude*Math.PI/180d);
    Check(RelaxedCubeSphereProjection.TryAddress(floridaDirection,out var floridaFace,out var floridaU,out var floridaV)&&floridaFace==CubeSphereFace.PositiveZ&&Math.Sqrt((RelaxedCubeSphereProjection.UnitDirection(floridaFace,floridaU,floridaV)-floridaDirection).LengthSquared)<2e-12,"Florida launch site maps to one exact canonical +Z relaxed-cube address");
    var floridaLand=new double[3];for(var level=0;level<=2;level++)floridaLand[level]=SampleFace(floridaFace,level,floridaU,floridaV).Land;
    Check(floridaLand.All(value=>value>.5d),"Florida launch-site footprint remains land-classified at every global payload level");
    Check(maximumInternalElevation==0d&&maximumInternalAlbedo==0d&&maximumInternalLand==0d,"same-face patch filters are exactly continuous");
    Check(maximumFaceElevation==0d&&maximumFaceAlbedo==0d&&maximumFaceLand==0d,"all directed cube-face filters are exactly continuous");
    Check(maximumCornerElevation==0d&&maximumCornerAlbedo==0d&&maximumCornerLand==0d,"all triple-face corner filters share one exact sample");
    Console.WriteLine($"Terrain-v5 payload continuity: internal=E{maximumInternalElevation:R}/A{maximumInternalAlbedo:R}/L{maximumInternalLand:R}; face=E{maximumFaceElevation:R}/A{maximumFaceAlbedo:R}/L{maximumFaceLand:R}; corner=E{maximumCornerElevation:R}/A{maximumCornerAlbedo:R}/L{maximumCornerLand:R}; rawParentChild=E{maximumRawParentChildElevationMetres:F6}m/A{maximumRawParentChildAlbedo:F6}/L{maximumRawParentChildLand:F6}; uncoordinatedMorphGap={maximumUncoordinatedMorphGapMetres:F6}m; coherentMorphGap={maximumCoherentMorphGapMetres:R}m; edgeAgeGap={maximumEdgeAgeDiscrepancy:R}; renderedMixedEdge=position{maximumConstrainedEdgePositionGapMetres:R}m/material{maximumConstrainedEdgeMaterialGap:R}/land{maximumConstrainedEdgeLandGap:R}/normal{maximumConstrainedEdgeNormalAngleRadians:R}rad; actualEdgeGap={maximumActualEdgePositionGapMetres:E3}m; oneSidedNormal={maximumOneSidedNormalAngleRadians:E3}rad; gutterNormal={maximumGutterNormalAngleRadians:E3}rad; constrainedVertices={constrainedVertexCount}; stitch=[-U/+U oddY->evenY,-V/+V oddX->evenX]; parentAddressDirection={maximumParentAddressDirectionError:E3}; elevationEnvelope=[{minimumPayloadElevationMetres:F6},{maximumPayloadElevationMetres:F6}]m; quarterMorphStep={maximumQuarterMorphStepMetres:F6}m; Florida={floridaFace}/{floridaU:R}/{floridaV:R}; land=[{string.Join(',',floridaLand.Select(value=>value.ToString("F6")))}]");

    void Measure(in PlanetarySurfacePatchId a,int ax,int ay,in PlanetarySurfacePatchId b,int bx,int by)
    {
        var aPosition=Position(a,ax/16d,ay/16d);var bPosition=Position(b,bx/16d,by/16d);
        maximumActualEdgePositionGapMetres=Math.Max(maximumActualEdgePositionGapMetres,Math.Sqrt((aPosition-bPosition).LengthSquared));
        maximumOneSidedNormalAngleRadians=Math.Max(maximumOneSidedNormalAngleRadians,Angle(OneSidedNormal(a,ax,ay),OneSidedNormal(b,bx,by)));
        maximumGutterNormalAngleRadians=Math.Max(maximumGutterNormalAngleRadians,Angle(GutterNormal(a,ax/16d,ay/16d),GutterNormal(b,bx/16d,by/16d)));
    }
    Double3 OneSidedNormal(in PlanetarySurfacePatchId id,int x,int y)
    {
        var left=Position(id,Math.Max(0,x-1)/16d,y/16d);var right=Position(id,Math.Min(16,x+1)/16d,y/16d);
        var down=Position(id,x/16d,Math.Max(0,y-1)/16d);var up=Position(id,x/16d,Math.Min(16,y+1)/16d);
        var result=Double3.Cross(right-left,up-down).Normalized();var radial=Position(id,x/16d,y/16d).Normalized();return Double3.Dot(result,radial)<0d?-result:result;
    }
    Double3 GutterNormal(in PlanetarySurfacePatchId id,double u,double v)
    {
        const double step=1d/256d;var left=ExtendedPosition(id,u-step,v);var right=ExtendedPosition(id,u+step,v);var down=ExtendedPosition(id,u,v-step);var up=ExtendedPosition(id,u,v+step);
        var result=Double3.Cross(right-left,up-down).Normalized();var radial=Position(id,u,v).Normalized();return Double3.Dot(result,radial)<0d?-result:result;
    }
    Double3 Position(in PlanetarySurfacePatchId id,double u,double v)
    {
        var side=1<<id.Level;var direction=RelaxedCubeSphereProjection.UnitDirection(id.Face,(id.X+u)/side,(id.Y+v)/side);return direction*(6_378_137d+ElevationMetres(Sample(id,u,v).Elevation));
    }
    Double3 ExtendedPosition(in PlanetarySurfacePatchId id,double u,double v)
    {
        var side=1<<id.Level;var direction=RelaxedCubeSphereProjection.ProjectExtended(id.Face,(id.X+u)/side,(id.Y+v)/side,1d);return direction*(6_378_137d+ElevationMetres(SampleExtended(id,u,v).Elevation));
    }
    static double ElevationMetres(double encoded)=>-11_000d+encoded*(20_000d/65_535d);
    static double Angle(in Double3 a,in Double3 b)=>Math.Acos(Math.Clamp(Double3.Dot(a,b),-1d,1d));

    void CompareMorphBoundary(in PlanetarySurfacePatchId aId,double aU,double aV,in PlanetarySurfacePatchId bId,double bU,double bV)
    {
        var aParent=aId.Parent!.Value;var bParent=bId.Parent!.Value;
        var aParentU=((aId.X&1)+aU)*.5d;var aParentV=((aId.Y&1)+aV)*.5d;
        var bParentU=((bId.X&1)+bU)*.5d;var bParentV=((bId.Y&1)+bV)*.5d;
        var a0=Sample(aParent,aParentU,aParentV).Elevation;var b0=Sample(bParent,bParentU,bParentV).Elevation;
        var a1=Sample(aId,aU,aV).Elevation;var b1=Sample(bId,bU,bV).Elevation;
        var uncoordinated=Math.Abs(Lerp(a0,a1,.25d)-Lerp(b0,b1,.75d))*(20_000d/65_535d);
        var coherent=Math.Abs(Lerp(a0,a1,.5d)-Lerp(b0,b1,.5d))*(20_000d/65_535d);
        maximumUncoordinatedMorphGapMetres=Math.Max(maximumUncoordinatedMorphGapMetres,uncoordinated);
        maximumCoherentMorphGapMetres=Math.Max(maximumCoherentMorphGapMetres,coherent);
    }

    (double R,double G,double B,double Elevation,double Land) SampleFace(CubeSphereFace face,int level,double u,double v)
    {
        var side=1<<level;var scaledU=Math.Clamp(u,0d,1d)*side;var scaledV=Math.Clamp(v,0d,1d)*side;var x=Math.Min((int)Math.Floor(scaledU),side-1);var y=Math.Min((int)Math.Floor(scaledV),side-1);
        return Sample(new(6,5,face,level,x,y),scaledU-x,scaledV-y);
    }
    (double R,double G,double B,double Elevation,double Land) SampleEdge(CubeSphereFace face,int level,PlanetaryPatchEdge edge,double t)
    {
        var side=1<<level;var scaled=Math.Clamp(t,0d,1d)*side;var along=Math.Min((int)Math.Floor(scaled),side-1);var local=scaled-along;
        return edge switch
        {
            PlanetaryPatchEdge.NegativeU=>Sample(new(6,5,face,level,0,along),0d,local),
            PlanetaryPatchEdge.PositiveU=>Sample(new(6,5,face,level,side-1,along),1d,local),
            PlanetaryPatchEdge.NegativeV=>Sample(new(6,5,face,level,along,0),local,0d),
            PlanetaryPatchEdge.PositiveV=>Sample(new(6,5,face,level,along,side-1),local,1d),
            _=>throw new ArgumentOutOfRangeException(nameof(edge))
        };
    }
    (double R,double G,double B,double Elevation,double Land) Sample(in PlanetarySurfacePatchId id,double u,double v)
    {
        var value=payloads[id];const int extent=PlanetaryCubeSurfacePackContract.StoredExtent;const int interior=PlanetaryCubeSurfacePackContract.InteriorTexels;const int gutter=PlanetaryCubeSurfacePackContract.SeamGutterTexels;
        var px=gutter-.5d+Math.Clamp(u,0d,1d)*interior;var py=gutter-.5d+Math.Clamp(v,0d,1d)*interior;var x0=(int)Math.Floor(px);var y0=(int)Math.Floor(py);var x1=x0+1;var y1=y0+1;var tx=px-x0;var ty=py-y0;
        const int channelCount=3;
        double Byte(byte[] source,int channel)=>Lerp(Lerp(source[(y0*extent+x0)*channelCount+channel],source[(y0*extent+x1)*channelCount+channel],tx),Lerp(source[(y1*extent+x0)*channelCount+channel],source[(y1*extent+x1)*channelCount+channel],tx),ty);
        double Scalar(byte[] source)=>Lerp(Lerp(source[y0*extent+x0],source[y0*extent+x1],tx),Lerp(source[y1*extent+x0],source[y1*extent+x1],tx),ty);
        double Elevation()=>Lerp(Lerp(value.Elevation[y0*extent+x0],value.Elevation[y0*extent+x1],tx),Lerp(value.Elevation[y1*extent+x0],value.Elevation[y1*extent+x1],tx),ty);
        return(Byte(value.Albedo,0)/255d,Byte(value.Albedo,1)/255d,Byte(value.Albedo,2)/255d,Elevation(),Scalar(value.Land)/255d);
    }
    (double R,double G,double B,double Elevation,double Land) SampleExtended(in PlanetarySurfacePatchId id,double u,double v)
    {
        var value=payloads[id];const int extent=PlanetaryCubeSurfacePackContract.StoredExtent;const int interior=PlanetaryCubeSurfacePackContract.InteriorTexels;const int gutter=PlanetaryCubeSurfacePackContract.SeamGutterTexels;
        var px=gutter-.5d+u*interior;var py=gutter-.5d+v*interior;var x0=(int)Math.Floor(px);var y0=(int)Math.Floor(py);var x1=x0+1;var y1=y0+1;var tx=px-x0;var ty=py-y0;
        Check(x0>=0&&y0>=0&&x1<extent&&y1<extent,"normal proof samples remain inside canonical payload gutters");
        const int channelCount=3;
        double Byte(byte[] source,int channel)=>Lerp(Lerp(source[(y0*extent+x0)*channelCount+channel],source[(y0*extent+x1)*channelCount+channel],tx),Lerp(source[(y1*extent+x0)*channelCount+channel],source[(y1*extent+x1)*channelCount+channel],tx),ty);
        double Scalar(byte[] source)=>Lerp(Lerp(source[y0*extent+x0],source[y0*extent+x1],tx),Lerp(source[y1*extent+x0],source[y1*extent+x1],tx),ty);
        double Elevation()=>Lerp(Lerp(value.Elevation[y0*extent+x0],value.Elevation[y0*extent+x1],tx),Lerp(value.Elevation[y1*extent+x0],value.Elevation[y1*extent+x1],tx),ty);
        return(Byte(value.Albedo,0)/255d,Byte(value.Albedo,1)/255d,Byte(value.Albedo,2)/255d,Elevation(),Scalar(value.Land)/255d);
    }
    static double Lerp(double a,double b,double t)=>a+(b-a)*t;
    static void Compare((double R,double G,double B,double Elevation,double Land) a,(double R,double G,double B,double Elevation,double Land) b,ref double elevation,ref double albedo,ref double land)
    {
        elevation=Math.Max(elevation,Math.Abs(a.Elevation-b.Elevation));albedo=Math.Max(albedo,Math.Max(Math.Abs(a.R-b.R),Math.Max(Math.Abs(a.G-b.G),Math.Abs(a.B-b.B))));land=Math.Max(land,Math.Abs(a.Land-b.Land));
    }
}

static void ProductionRelaxedCubeSpherePatchHierarchyTest()
{
    const ulong bodyId=6;const uint terrainVersion=5;const int level=4;const int size=1<<level;
    var faces=Enum.GetValues<CubeSphereFace>();var edges=Enum.GetValues<PlanetaryPatchEdge>().Where(edge=>edge!=PlanetaryPatchEdge.None).ToArray();
    var maximumRadiusError=0d;var maximumEdgeError=0d;var maximumCornerError=0d;var maximumParentChildError=0d;
    foreach(var face in faces)
        for(var y=0;y<=16;y++)for(var x=0;x<=16;x++)
        {
            var point=RelaxedCubeSphereProjection.UnitDirection(face,x/16d,y/16d);
            maximumRadiusError=Math.Max(maximumRadiusError,Math.Abs(Math.Sqrt(point.LengthSquared)-1d));
        }
    foreach(var face in faces)foreach(var edge in edges)for(var along=0;along<size;along++)
    {
        var patch=edge is PlanetaryPatchEdge.NegativeU or PlanetaryPatchEdge.PositiveU
            ?new PlanetarySurfacePatchId(bodyId,terrainVersion,face,level,edge==PlanetaryPatchEdge.NegativeU?0:size-1,along)
            :new PlanetarySurfacePatchId(bodyId,terrainVersion,face,level,along,edge==PlanetaryPatchEdge.NegativeV?0:size-1);
        var transition=CubeSphereAdjacency.GetTransition(face,edge);var neighborPatch=CubeSphereAdjacency.NeighborAtSameLevel(patch.Patch,edge);
        var neighbor=new PlanetarySurfacePatchId(bodyId,terrainVersion,neighborPatch.Face,neighborPatch.Level,neighborPatch.X,neighborPatch.Y);
        for(var sample=0;sample<=PlanetaryPatchTopology.QuadsPerSide;sample++)
        {
            var targetSample=transition.Reversed?PlanetaryPatchTopology.QuadsPerSide-sample:sample;
            var source=EdgePoint(patch,edge,sample);var target=EdgePoint(neighbor,transition.NeighborEdge,targetSample);
            maximumEdgeError=Math.Max(maximumEdgeError,Math.Sqrt((source-target).LengthSquared));
        }
    }
    var cubeCorners=new Dictionary<(int X,int Y,int Z),List<Double3>>();
    foreach(var face in faces)foreach(var u in new[]{0d,1d})foreach(var v in new[]{0d,1d})
    {
        var point=RelaxedCubeSphereProjection.UnitDirection(face,u,v);var key=(Math.Sign(point.X),Math.Sign(point.Y),Math.Sign(point.Z));
        if(!cubeCorners.TryGetValue(key,out var values))cubeCorners[key]=values=[];values.Add(point);
    }
    Check(cubeCorners.Count==8&&cubeCorners.Values.All(values=>values.Count==3),"eight relaxed cube-sphere corners each have three canonical face representations");
    foreach(var values in cubeCorners.Values)for(var index=1;index<values.Count;index++)maximumCornerError=Math.Max(maximumCornerError,Math.Sqrt((values[index]-values[0]).LengthSquared));

    var parent=new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.PositiveZ,3,3,5);
    for(var childIndex=0;childIndex<4;childIndex++)
    {
        var child=parent.Child(childIndex);Check(child.Parent==parent&&child.IsValid,"production patch child preserves complete stable identity");
        for(var py=0;py<=PlanetaryPatchTopology.QuadsPerSide;py++)for(var px=0;px<=PlanetaryPatchTopology.QuadsPerSide;px++)
            if(PlanetaryPatch.TryMapGridVertexToChild(childIndex,px,py,out var cx,out var cy,PlanetaryPatchTopology.QuadsPerSide))
            {
                var a=RelaxedCubeSphereProjection.PatchPoint(parent,px,py);var b=RelaxedCubeSphereProjection.PatchPoint(child,cx,cy);
                maximumParentChildError=Math.Max(maximumParentChildError,Math.Sqrt((a-b).LengthSquared));
            }
    }
    var ordinals=new HashSet<ulong>();
    for(var currentLevel=0;currentLevel<=4;currentLevel++)
    {
        var side=1<<currentLevel;
        foreach(var face in faces)for(var y=0;y<side;y++)for(var x=0;x<side;x++)
            Check(ordinals.Add(PlanetaryCubeSurfacePackContract.PatchOrdinal(face,currentLevel,x,y)),"patch-aligned cube-surface ordinals are unique");
    }
    Check((ulong)ordinals.Count==PlanetaryCubeSurfacePackContract.PatchCountThroughLevel(4),"patch-aligned pack count exactly covers the hierarchy");
    Span<byte> headerBytes=stackalloc byte[256];BinaryPrimitives.WriteUInt64LittleEndian(headerBytes,PlanetaryCubeSurfacePackContract.Magic);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[8..],PlanetaryCubeSurfacePackContract.Version);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[12..],256);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[16..],256);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[20..],4);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[24..],264);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[28..],4);BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[32..],(uint)PlanetaryCubeSurfacePackContract.PatchCountThroughLevel(4));BinaryPrimitives.WriteUInt32LittleEndian(headerBytes[36..],terrainVersion);
    Check(PlanetaryCubeSurfacePackContract.TryReadHeader(headerBytes,out var header)&&header.IsValid&&header.RecordCount==(uint)ordinals.Count,"cube-surface pack header binds one record to every patch identity");
    Span<byte> recordBytes=stackalloc byte[PlanetaryCubeSurfacePackContract.RecordHeaderBytes];var recordPatch=new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.NegativeZ,4,7,11);BinaryPrimitives.WriteUInt64LittleEndian(recordBytes,bodyId);BinaryPrimitives.WriteUInt32LittleEndian(recordBytes[8..],terrainVersion);recordBytes[12]=(byte)recordPatch.Face;recordBytes[13]=(byte)recordPatch.Level;BinaryPrimitives.WriteUInt32LittleEndian(recordBytes[16..],(uint)recordPatch.X);BinaryPrimitives.WriteUInt32LittleEndian(recordBytes[20..],(uint)recordPatch.Y);BinaryPrimitives.WriteUInt64LittleEndian(recordBytes[24..],PlanetaryCubeSurfacePackContract.PatchOrdinal(recordPatch.Face,recordPatch.Level,recordPatch.X,recordPatch.Y));for(var offset=32;offset<=44;offset+=4)BinaryPrimitives.WriteUInt32LittleEndian(recordBytes[offset..],123u+(uint)offset);BinaryPrimitives.WriteUInt64LittleEndian(recordBytes[48..],1ul);
    Check(PlanetaryCubeSurfacePackContract.TryReadRecordHeader(recordBytes,out var recordHeader)&&recordHeader.Patch==recordPatch&&recordHeader.PatchOrdinal==PlanetaryCubeSurfacePackContract.PatchOrdinal(recordPatch.Face,recordPatch.Level,recordPatch.X,recordPatch.Y),"one record atomically addresses all patch-aligned material and elevation channels");
    var productionTerrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;var proofDirection=new Double3(.37,-.21,.91).Normalized();
    var expectedBaseHeight=Math.Max(0d,EarthElevationDataset.SampleHeight(proofDirection)+EarthLocalTerrainElevationDataset.SampleResidual(proofDirection));
    Check(productionTerrain.Version==terrainVersion&&productionTerrain.SampleBaseHeight(proofDirection)==expectedBaseHeight,"production cube terrain version preserves the lawful topology-neutral elevation oracle");
    Check(productionTerrain.SampleHeight(proofDirection,0)==productionTerrain.SamplePhysicalSurface(proofDirection).FinalHeightMetres,"production cube terrain height routes through the canonical physical-surface evaluator");

    var root=new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.PositiveX,0,0,0);var cache=new PlanetarySurfacePatchCache(16,2);
    cache.RegisterPayload(root,PlanetarySurfacePatchPayload.ProductionRequired);cache.SetInitialAuthoritative(root);
    for(var child=0;child<4;child++)cache.RegisterPayload(root.Child(child),child==3?PlanetarySurfacePatchPayload.Geometry|PlanetarySurfacePatchPayload.Elevation:PlanetarySurfacePatchPayload.ProductionRequired);
    Span<PlanetarySurfacePatchOwnership> ownership=stackalloc PlanetarySurfacePatchOwnership[8];
    Check(!cache.TryBeginPromotion(root)&&cache.SnapshotOwnership(ownership)==1&&ownership[0].Patch==root&&ownership[0].OpaqueBase,"incomplete child quartet never becomes a visible owner");
    cache.RegisterPayload(root.Child(3),PlanetarySurfacePatchPayload.Material|PlanetarySurfacePatchPayload.Classification);Check(cache.TryBeginPromotion(root),"complete child quartet begins one coherent promotion");
    cache.AdvancePromotions(.5f);var midpointCount=cache.SnapshotOwnership(ownership);var midpoint=ownership[..midpointCount].ToArray();
    Check(midpointCount==5&&midpoint.Count(value=>value.OpaqueBase)==1&&midpoint.Single(value=>value.OpaqueBase).Patch==root&&midpoint.Where(value=>!value.OpaqueBase).All(value=>value.RefinementWeight==.5f),"parent remains opaque while all four children morph with one transaction weight");
    cache.AdvancePromotions(.5f);var finalCount=cache.SnapshotOwnership(ownership);var final=ownership[..finalCount].ToArray();
    Check(finalCount==4&&final.All(value=>value.OpaqueBase&&value.Patch.Parent==root)&&cache.ResidencyOf(root)==PlanetarySurfacePatchResidency.Resident,"completed transaction atomically transfers authority to the complete child quartet");

    var balancedCache=new PlanetarySurfacePatchCache(96,4);foreach(var face in faces){var faceRoot=new PlanetarySurfacePatchId(bodyId,terrainVersion,face,0,0,0);balancedCache.RegisterPayload(faceRoot,PlanetarySurfacePatchPayload.ProductionRequired);balancedCache.SetInitialAuthoritative(faceRoot);}
    var boundaryRoot=new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.PositiveX,0,0,0);for(var child=0;child<4;child++)balancedCache.RegisterPayload(boundaryRoot.Child(child),PlanetarySurfacePatchPayload.ProductionRequired);Check(balancedCache.TryBeginPromotion(boundaryRoot),"balanced root quartet may promote");balancedCache.AdvancePromotions(1f);
    var boundaryChild=boundaryRoot.Child(0);for(var child=0;child<4;child++)balancedCache.RegisterPayload(boundaryChild.Child(child),PlanetarySurfacePatchPayload.ProductionRequired);Check(!balancedCache.TryBeginPromotion(boundaryChild),"promotion cannot create a two-level discontinuity across a coarse cube face");
    foreach(var neighborFace in faces.Where(face=>face!=CubeSphereFace.PositiveX)){var neighborRoot=new PlanetarySurfacePatchId(bodyId,terrainVersion,neighborFace,0,0,0);for(var child=0;child<4;child++)balancedCache.RegisterPayload(neighborRoot.Child(child),PlanetarySurfacePatchPayload.ProductionRequired);Check(balancedCache.TryBeginPromotion(neighborRoot),"neighbor face quartet prepares as one transaction");balancedCache.AdvancePromotions(1f);}Check(balancedCache.TryBeginPromotion(boundaryChild),"boundary refinement proceeds after canonical neighbor balance is restored");

    var visible=new[]{new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.PositiveX,2,0,1),new PlanetarySurfacePatchId(bodyId,terrainVersion,CubeSphereFace.PositiveX,2,1,1)};Span<PlanetarySurfacePatchDemand> demandsA=stackalloc PlanetarySurfacePatchDemand[64];Span<PlanetarySurfacePatchDemand> demandsB=stackalloc PlanetarySurfacePatchDemand[64];
    var demandCountA=PlanetarySurfaceResidencyPlanner.Build(visible,new Double3(1,.25,0),demandsA);var demandCountB=PlanetarySurfaceResidencyPlanner.Build(visible,new Double3(1,.25,0),demandsB);var demandArray=demandsA[..demandCountA].ToArray();
    Check(demandCountA==demandCountB&&demandsA[..demandCountA].SequenceEqual(demandsB[..demandCountB]),"visible-footprint and camera-motion residency demand is deterministic");
    Check(visible.All(id=>demandArray.Any(demand=>demand.Patch==id&&demand.Priority==0&&demand.Payload==PlanetarySurfacePatchPayload.ProductionRequired)),"every actually visible patch has highest coherent payload priority");
    Check(demandArray.Any(demand=>demand.Patch.Face!=visible[0].Face),"residency demand crosses canonical cube-face edges rather than using an anchor-centered UV rectangle");

    var productionSource=File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","src","NovaCore.Graphics","PlanetaryProductionSurface.cs")));
    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    if(TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthAssetId,null,out var productionManifest,out var packPath,out _))
    {
        var verification=TerrainAssetCache.Verify(productionManifest,packPath);
        Check(verification.IsValid&&verification.ActualSha256=="38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae","resolved production cube pack identity is deterministic");
        using(var pack=File.OpenRead(packPath))
        {
            Span<byte> packHeader=stackalloc byte[PlanetaryCubeSurfacePackContract.HeaderBytes];pack.ReadExactly(packHeader);
            Check(PlanetaryCubeSurfacePackContract.TryReadHeader(packHeader,out var parsedPack)&&parsedPack.IsProductionLayout&&parsedPack.MaximumLevel==2&&parsedPack.RecordCount==126&&parsedPack.TerrainVersion==terrainVersion,"cached cube pack header is the bounded terrain-v5 production hierarchy");
            var payload=new byte[PlanetaryCubeSurfacePackContract.StoredExtent*PlanetaryCubeSurfacePackContract.StoredExtent*7];
            var recordBuffer=new byte[PlanetaryCubeSurfacePackContract.RecordHeaderBytes];
            var shippedOrdinals=new HashSet<ulong>();var digestFailures=0;
            for(var index=0;index<parsedPack.RecordCount;index++)
            {
                Span<byte> record=recordBuffer;pack.ReadExactly(record);
                Check(PlanetaryCubeSurfacePackContract.TryReadRecordHeader(record,out var parsedRecord)&&parsedRecord.Patch.BodyId==bodyId&&parsedRecord.Patch.TerrainVersion==terrainVersion,"cached record has valid stable patch identity");
                Check(parsedRecord.AlbedoBytes==209_088&&parsedRecord.ElevationBytes==139_392&&parsedRecord.LandMaskBytes==69_696&&parsedRecord.CloudBytes==69_696,"one record owns the complete fixed-width patch transaction");
                var payloadBytes=checked((int)(parsedRecord.AlbedoBytes+parsedRecord.ElevationBytes+parsedRecord.LandMaskBytes+parsedRecord.CloudBytes));pack.ReadExactly(payload.AsSpan(0,payloadBytes));
                using var digest=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);digest.AppendData(record[..24]);digest.AppendData(payload,0,payloadBytes);var actual=digest.GetHashAndReset();
                Check(actual.AsSpan().SequenceEqual(record[48..80]),"cached patch transaction digest validates before residency");
                if(index==0){payload[0]^=1;using var corrupt=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);corrupt.AppendData(record[..24]);corrupt.AppendData(payload,0,payloadBytes);if(corrupt.GetHashAndReset().AsSpan().SequenceEqual(record[48..80]))digestFailures++;payload[0]^=1;}
                Check(shippedOrdinals.Add(parsedRecord.PatchOrdinal),"cached cube patch ordinal appears exactly once");
            }
            Check(pack.Position==pack.Length&&shippedOrdinals.Count==parsedPack.RecordCount&&digestFailures==0,"cached hierarchy is complete and a corrupted payload is rejected");
        }
    }
    Console.WriteLine($"Production relaxed cube-sphere topology hash: 0x{PlanetaryProductionPatchTopology.Shared.DeterministicHash:X16}");
    Check(PlanetaryProductionPatchTopology.Shared.DeterministicHash==0x61C28B0A3B4F21FFul,"production topology regression hash");
    Check(maximumRadiusError<5e-16&&maximumEdgeError<5e-15&&maximumCornerError<5e-15&&maximumParentChildError==0d,"relaxed cube-sphere is radius-stable, edge/corner continuous, and parent-child exact");
    Console.WriteLine($"Production relaxed cube-sphere: topologyHash=0x{PlanetaryProductionPatchTopology.Shared.DeterministicHash:X16}; radiusError={maximumRadiusError:E3}; edgeError={maximumEdgeError:E3}; cornerError={maximumCornerError:E3}; parentChildError={maximumParentChildError:E3}; cachePromotions={cache.Statistics.CompletedPromotions}; demands={demandCountA}");

    static Double3 EdgePoint(in PlanetarySurfacePatchId patch,PlanetaryPatchEdge edge,int sample)
    {
        return edge switch
        {
            PlanetaryPatchEdge.NegativeU=>RelaxedCubeSphereProjection.PatchPoint(patch,0,sample),
            PlanetaryPatchEdge.PositiveU=>RelaxedCubeSphereProjection.PatchPoint(patch,PlanetaryPatchTopology.QuadsPerSide,sample),
            PlanetaryPatchEdge.NegativeV=>RelaxedCubeSphereProjection.PatchPoint(patch,sample,0),
            PlanetaryPatchEdge.PositiveV=>RelaxedCubeSphereProjection.PatchPoint(patch,sample,PlanetaryPatchTopology.QuadsPerSide),
            _=>throw new ArgumentOutOfRangeException(nameof(edge))
        };
    }
}

static void EarthElevationOracleTest()
{
    var runtime=Path.Combine(Directory.GetCurrentDirectory(),"assets","earth","runtime");
    Check(EarthElevationDataset.TryLoad(runtime,out var error),$"Earth elevation oracle load: {error}");
    Check(EarthElevationDataset.IsLoaded,"Earth elevation oracle is resident");
    var everest=Direction(27.9881,86.925);var pacific=Direction(0,-140);var sahara=Direction(23,13);
    var everestElevation=EarthElevationDataset.SampleElevation(everest);var pacificElevation=EarthElevationDataset.SampleElevation(pacific);var saharaElevation=EarthElevationDataset.SampleElevation(sahara);
    Check(everestElevation is >5_000 and <8_000&&pacificElevation is <-3_000 and >-7_000&&saharaElevation is >200 and <2_000,"known ETOPO land/ocean elevation probes");
    Check(EarthElevationDataset.SampleHeight(pacific)==0d&&EarthElevationDataset.SampleHeight(everest)==everestElevation,"sea-level floor remains separate from signed elevation authority");
    Check(EarthElevationDataset.SampleElevation(everest)==everestElevation&&EarthElevationDataset.SampleElevation(pacific)==pacificElevation,"body-fixed CPU elevation samples are deterministic");
    using var stream=File.OpenRead(Path.Combine(runtime,"earth_elevation_8192x4096.r16"));
    Check(stream.Length==(long)EarthElevationDataset.Width*EarthElevationDataset.Height*sizeof(ushort)&&Convert.ToHexStringLower(SHA256.HashData(stream))==EarthElevationDataset.Sha256,"checked topology-neutral elevation oracle identity");
    static Double3 Direction(double latitudeDegrees,double longitudeDegrees)=>BodyFixedGeography.DirectionFromLatitudeLongitude(latitudeDegrees*Math.PI/180d,longitudeDegrees*Math.PI/180d);
}

static void CanonicalBodyFixedGeographicHandednessTest()
{
    const double tolerance=2e-12d;
    var equatorPrime=BodyFixedGeography.DirectionFromLatitudeLongitude(0d,0d);
    var equatorEast=BodyFixedGeography.DirectionFromLatitudeLongitude(0d,Math.PI*.5d);
    var equatorWest=BodyFixedGeography.DirectionFromLatitudeLongitude(0d,-Math.PI*.5d);
    var northPole=BodyFixedGeography.DirectionFromLatitudeLongitude(Math.PI*.5d,0d);
    var southPole=BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI*.5d,0d);
    Check(Math.Sqrt((equatorPrime-Double3.UnitX).LengthSquared)<=tolerance&&
        Math.Sqrt((equatorEast+Double3.UnitZ).LengthSquared)<=tolerance&&
        Math.Sqrt((equatorWest-Double3.UnitZ).LengthSquared)<=tolerance&&
        Math.Sqrt((northPole-Double3.UnitY).LengthSquared)<=tolerance&&
        Math.Sqrt((southPole+Double3.UnitY).LengthSquared)<=tolerance,
        "canonical cardinal geography is +Y north, +X prime meridian, and positive/east longitude toward -Z");

    var probes=new[]{(0d,0d),(28.6084d,-80.6042d),(-33.8688d,151.2093d),(51.5074d,-.1278d),(35.6762d,139.6503d)};
    var terrainVersion=new TerrainAuthorityVersion(2,5);double maximumEastError=0d,maximumIauError=0d;
    double maximumOrthogonalityError=0d,maximumCrossAlignmentError=0d;
    foreach(var (latitudeDegrees,longitudeDegrees) in probes)
    {
        var latitude=latitudeDegrees*Math.PI/180d;var longitude=longitudeDegrees*Math.PI/180d;
        var up=BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,longitude);
        Check(SurfaceAnchor.TryCreate(6,terrainVersion,up,0d,out var anchor)==SurfaceAnchorCreationStatus.Success,
            "canonical geographic probe creates a valid surface anchor");
        Check(SurfaceEnuFrame.TryCreate(anchor,out var enu),"canonical geographic probe creates a valid ENU frame");
        const double delta=1e-7d;
        var eastByLongitude=(BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,longitude+delta)-
            BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,longitude-delta)).Normalized();
        maximumEastError=Math.Max(maximumEastError,Math.Sqrt((enu.East-eastByLongitude).LengthSquared));
        var eastNorth=Double3.Dot(Double3.Cross(enu.East,enu.North),enu.Up);
        var northUp=Double3.Dot(Double3.Cross(enu.North,enu.Up),enu.East);
        var upEast=Double3.Dot(Double3.Cross(enu.Up,enu.East),enu.North);
        maximumOrthogonalityError=Math.Max(maximumOrthogonalityError,Math.Max(Math.Abs(Double3.Dot(enu.East,enu.North)),
            Math.Max(Math.Abs(Double3.Dot(enu.North,enu.Up)),Math.Abs(Double3.Dot(enu.Up,enu.East)))));
        maximumCrossAlignmentError=Math.Max(maximumCrossAlignmentError,Math.Max(Math.Abs(1d-eastNorth),
            Math.Max(Math.Abs(1d-northUp),Math.Abs(1d-upEast))));
        Check(maximumOrthogonalityError<=tolerance&&eastNorth>=1d-tolerance&&northUp>=1d-tolerance&&upEast>=1d-tolerance&&
            Double3.Dot(enu.East,eastByLongitude)>=1d-tolerance,
            "ENU is right-handed and East follows increasing authored longitude");

        // Existing Nova surface-to-IAU rotation is +90 degrees about local X:
        // (x,y,z) -> (x,-z,y). The result must be conventional IAU east-positive geography.
        var iau=new Double3(up.X,-up.Z,up.Y);
        var expectedIau=new Double3(Math.Cos(latitude)*Math.Cos(longitude),
            Math.Cos(latitude)*Math.Sin(longitude),Math.Sin(latitude));
        maximumIauError=Math.Max(maximumIauError,Math.Sqrt((iau-expectedIau).LengthSquared));
        Check(Math.Abs(BodyFixedGeography.LatitudeRadians(up)-latitude)<=tolerance&&
            Math.Abs(BodyFixedGeography.LongitudeRadians(up)-longitude)<=tolerance,
            "authored latitude/longitude round-trips through canonical body-fixed direction");
    }
    Check(maximumEastError<2e-9d&&maximumIauError<=tolerance&&maximumOrthogonalityError<=tolerance&&maximumCrossAlignmentError<=tolerance,
        "canonical geographic derivative, orthogonality, cross products, and IAU basis agree");
    Console.WriteLine($"Geographic handedness: East x North = Up; +longitude=-Z; maxOrthogonalityError={maximumOrthogonalityError:E3}; maxCrossAlignmentError={maximumCrossAlignmentError:E3}; maxEastError={maximumEastError:E3}; maxIauError={maximumIauError:E3}");
}

static void CanonicalSurfaceAnchorPhysicalTerrainAuthorityTest()
{
    var runtime=Path.Combine(Directory.GetCurrentDirectory(),"assets","earth","runtime");
    Check(EarthElevationDataset.TryLoad(runtime,out var error),$"canonical SurfaceAnchor Earth terrain load: {error}");
    var terrainDefinition=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    var terrain=new PlanetaryPhysicalTerrainAuthority(SolarSystemBodyIds.Earth.Value,terrainDefinition);
    var version=new TerrainAuthorityVersion(terrainDefinition.SourceId,terrainDefinition.Version);
    var body=new SurfaceBodyReference(SolarSystemBodyIds.Earth.Value,6_371_008.8d,new ReferenceFrameId(700));
    var directions=new[]{
        new Double3(1,0,0),new Double3(.37,.51,.776).Normalized(),new Double3(-.7,.713,.03).Normalized(),
        new Double3(1e-10,1,2e-10).Normalized(),new Double3(-1e-10,-1,3e-10).Normalized(),
        new Double3(1,1,0).Normalized(),new Double3(1,1,1).Normalized(),new Double3(-1,1,-1).Normalized()};
    var offsets=new[]{0d,25d,-10d,250_000d};
    var cache=new PlanetaryTerrainResidencyCache(1);double maximumAuthorityError=0d;ulong identityHash=14695981039346656037UL;
    foreach(var direction in directions)foreach(var offset in offsets)
    {
        Check(SurfaceAnchor.TryCreate(body.BodyId,version,direction,offset,out var anchor)==SurfaceAnchorCreationStatus.Success,"production SurfaceAnchor creation");
        Check(SurfaceAnchorEvaluator.TryEvaluateBodyFixed(anchor,body,terrain,out var first,out var physicalHeight)==SurfaceAnchorEvaluationStatus.Success,"production SurfaceAnchor physical evaluation");
        var expectedHeight=terrainDefinition.SampleHeight(direction,24);var expected=direction*(body.ReferenceRadiusMetres+expectedHeight+offset);
        maximumAuthorityError=Math.Max(maximumAuthorityError,Math.Sqrt((first-expected).LengthSquared));
        _=cache.Acquire(new(body.BodyId,CubeSphereFace.PositiveX,0,0,0,terrainDefinition.Version,terrainDefinition.SourceId),terrainDefinition);
        _=cache.Acquire(new(body.BodyId,CubeSphereFace.NegativeZ,1,1,1,terrainDefinition.Version,terrainDefinition.SourceId),terrainDefinition);
        Check(SurfaceAnchorEvaluator.TryEvaluateBodyFixed(anchor,body,terrain,out var afterCacheChurn,out var repeatedHeight)==SurfaceAnchorEvaluationStatus.Success&&
            afterCacheChurn==first&&repeatedHeight==physicalHeight&&anchor.NormalizedBodyFixedDirection==direction,
            "physical SurfaceAnchor is independent of render cache, face, pupil, tier, and residency state");
        identityHash=(identityHash^anchor.DeterministicHash)*1099511628211UL;
    }
    Check(SurfaceAnchor.TryCreate(body.BodyId,version,Double3.UnitX,0d,out var mismatchAnchor)==SurfaceAnchorCreationStatus.Success&&
        SurfaceAnchorEvaluator.TryEvaluateBodyFixed(mismatchAnchor,body,new PlanetaryPhysicalTerrainAuthority(body.BodyId,new(terrainDefinition.SourceId,terrainDefinition.Version+1,terrainDefinition.MaximumHeightMetres)),out _,out _)==SurfaceAnchorEvaluationStatus.TerrainVersionMismatch,
        "production terrain-version mismatch fails explicitly without reinterpretation");

    Check(SurfaceAnchor.TryCreate(body.BodyId,version,directions[1],125d,out var timeAnchor)==SurfaceAnchorCreationStatus.Success,"time proof anchor");
    var timeIdentity=(timeAnchor.BodyId,timeAnchor.TerrainAuthorityVersion,timeAnchor.NormalizedBodyFixedDirection,BitConverter.DoubleToInt64Bits(timeAnchor.TerrainRelativeOffsetMetres),timeAnchor.DeterministicHash);
    var rootFrame=new ReferenceFrameId(701);var bodyFixedFrame=new ReferenceFrameId(702);Double3? firstRoot=null;var distinctRootPositions=0;
    foreach(var instant in new[]{SimulationInstant.FromWholeSeconds(-31_536_000),SimulationInstant.Zero,SimulationInstant.FromWholeSeconds(86_400),new SimulationInstant(757_339_269_183_906L),SimulationInstant.FromWholeSeconds(315_360_000)})
    {
        Check(SolarSystemScene.TryCreateAt(rootFrame,instant,out var sceneValue,out var sceneError)&&sceneValue is not null,$"SurfaceAnchor instant scene {instant}: {sceneError}");
        var earth=sceneValue!.Presentation.Bodies[3];var snapshot=new ReferenceFrameSnapshot([
            (new ReferenceFrameDefinition(rootFrame,null,ReferenceFrameKind.Ecl,"ECL"),CelestialFrameFactory.RootEcl()),
            (new ReferenceFrameDefinition(bodyFixedFrame,rootFrame,ReferenceFrameKind.Ccf,"Earth CCF"),new EvaluatedReferenceFrame(new FrameTransform(earth.Position.Value,earth.BodyFixedToRoot),Double3.Zero,Double3.Zero,false))]);
        var instantFrames=new ReferenceFrameResolver(snapshot);var instantBody=new SurfaceBodyReference(body.BodyId,earth.RadiusMetres,bodyFixedFrame);
        Check(SurfaceAnchorEvaluator.TryEvaluateRoot(timeAnchor,instantBody,terrain,instantFrames,out var rootPosition,out _)==SurfaceAnchorEvaluationStatus.Success,"SurfaceAnchor evaluates at direct SimulationInstant");
        Check(SurfaceAnchorEvaluator.TryEvaluateBodyFixed(timeAnchor,instantBody,terrain,out var bodyFixedPosition,out _)==SurfaceAnchorEvaluationStatus.Success&&
            rootPosition.Value==earth.Position.Value+earth.BodyFixedToRoot.Rotate(bodyFixedPosition),"SurfaceAnchor composes current celestial translation and body orientation exactly");
        Check(timeIdentity==(timeAnchor.BodyId,timeAnchor.TerrainAuthorityVersion,timeAnchor.NormalizedBodyFixedDirection,BitConverter.DoubleToInt64Bits(timeAnchor.TerrainRelativeOffsetMetres),timeAnchor.DeterministicHash),"SimulationInstant leaves canonical SurfaceAnchor bit-identical");
        if(firstRoot.HasValue&&rootPosition.Value!=firstRoot.Value)distinctRootPositions++;else firstRoot??=rootPosition.Value;
    }
    Check(distinctRootPositions==4,"past, epoch, current-epoch, and future SimulationInstants move root position without integrating anchor identity");

    Check(SurfaceAnchor.TryCreate(body.BodyId,version,directions[1],15d,out var benchmarkAnchor)==SurfaceAnchorCreationStatus.Success,"production benchmark anchor");
    _=SurfaceAnchorEvaluator.TryEvaluateBodyFixed(benchmarkAnchor,body,terrain,out _,out _);_ = SurfaceEnuFrame.TryCreate(benchmarkAnchor,out _);
    var watch=new Stopwatch();var before=GC.GetAllocatedBytesForCurrentThread();watch.Start();double terrainChecksum=0d;
    for(var index=0;index<10_000;index++){Check(terrain.TrySampleHeight(body.BodyId,directions[index%directions.Length],out var height),"production terrain benchmark");terrainChecksum+=height;}
    watch.Stop();var terrainNanoseconds=watch.Elapsed.TotalNanoseconds/10_000d;watch.Restart();SurfaceEnuFrame enu=default;
    for(var index=0;index<100_000;index++)Check(SurfaceEnuFrame.TryCreate(benchmarkAnchor,out enu),"production ENU benchmark");
    watch.Stop();var enuNanoseconds=watch.Elapsed.TotalNanoseconds/100_000d;var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
    Check(maximumAuthorityError==0d&&terrainChecksum>=0d&&enu.IsValid&&allocated==0,"production SurfaceAnchor authority precision and allocation");
    Console.WriteLine($"Canonical SurfaceAnchor terrain: maxAuthorityError={maximumAuthorityError:E17} m; terrainQuery={terrainNanoseconds:F1} ns; ENU={enuNanoseconds:F1} ns; allocations={allocated}; hash=0x{identityHash:X16}");
}

static void AnchoredFloridaLaunchSiteTest()
{
    var root=new ReferenceFrameId(1);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var scene,out var error)&&scene is not null,$"Florida site scene: {error}");
    var site=scene!.FloridaLaunchSite;
    Check(site.IsValid&&site.LatitudeDegrees==28.6084d&&site.LongitudeDegrees==-80.6042d,"Florida coordinates author one immutable site");
    Check(site.Object.Id==FloridaLaunchSite.ObjectId&&site.Object.Geometry==FloridaLaunchSite.GeometryId&&
        site.Object.Anchor.BodyId==SolarSystemBodyIds.Earth.Value&&site.Object.Anchor.TerrainAuthorityVersion==
        new TerrainAuthorityVersion(PlanetaryTerrainDefinition.EarthProductionCubeV5.SourceId,PlanetaryTerrainDefinition.EarthProductionCubeV5.Version),
        "site identity binds Earth and terrain-v5 without render ownership");
    var direction=site.Object.Anchor.NormalizedBodyFixedDirection;
    var expectedLatitude=Math.Asin(direction.Y)*180d/Math.PI;
    var expectedLongitude=BodyFixedGeography.LongitudeRadians(direction)*180d/Math.PI;
    Check(Math.Abs(expectedLatitude-site.LatitudeDegrees)<1e-12d&&Math.Abs(expectedLongitude-site.LongitudeDegrees)<1e-12d,
        "canonical direction round-trips authored latitude/longitude");
    Check(scene.TryEvaluateFloridaLaunchSite(out var pose)&&pose.IsValid,"site evaluates at J2000");
    var earth=scene.Presentation.Bodies.ToArray().Single(body=>body.BodyId==SolarSystemBodyIds.Earth.Value);
    var radial=Math.Sqrt(pose.BodyFixedPosition.LengthSquared);
    Check(Math.Abs(radial-(earth.RadiusMetres+site.AnchorTerrainHeightMetres+site.FoundationOffsetMetres))<1e-9d&&
        Math.Abs(site.LocalPhysicalSurfaceRadiusMetres-radial)<1e-9d,"foundation is seated by the physical terrain oracle");
    var eastError=Math.Acos(Math.Clamp(Double3.Dot(pose.BodyFixedOrientation.Rotate(Double3.UnitX),pose.Enu.East),-1d,1d));
    var northError=Math.Acos(Math.Clamp(Double3.Dot(pose.BodyFixedOrientation.Rotate(Double3.UnitY),pose.Enu.North),-1d,1d));
    var upError=Math.Acos(Math.Clamp(Double3.Dot(pose.BodyFixedOrientation.Rotate(Double3.UnitZ),pose.Enu.Up),-1d,1d));
    Check(eastError<1e-12d&&northError<1e-12d&&upError<1e-12d,"launchpad local axes are exact canonical ENU");

    var anchor=site.Object.Anchor;var objectHash=site.Object.DeterministicHash;var bodyFixed=pose.BodyFixedPosition;var bodyOrientation=pose.BodyFixedOrientation;var rootAtZero=pose.RootPosition.Value;
    foreach(var seconds in new[]{1L,10L,600L,86_400L,31_536_000L})
    {
        AnchoredSurfaceObjectPose advancedPose=default;
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.FromWholeSeconds(seconds),out var advanced,out error)&&advanced is not null,$"Florida site time {seconds}: {error}");
        Check(advanced!.FloridaLaunchSite.Object.Anchor==anchor&&advanced.FloridaLaunchSite.Object.DeterministicHash==objectHash&&
            advanced.TryEvaluateFloridaLaunchSite(out advancedPose),$"Florida site identity/evaluation {seconds}");
        Check(advancedPose.BodyFixedPosition==bodyFixed&&advancedPose.BodyFixedOrientation==bodyOrientation,
            $"body-fixed launchpad has zero integrated drift at {seconds} s");
        Check(advancedPose.RootPosition.Value!=rootAtZero,$"Earth motion alone changes launchpad root pose at {seconds} s");
    }

    foreach(var (rate,paused) in new[]{
        (new SimulationRate(1,10),false),(SimulationRate.One,true),(SimulationRate.One,false),
        (new SimulationRate(2,1),false),(new SimulationRate(10,1),false),(new SimulationRate(30,1),false),
        (new SimulationRate(600,1),false),(new SimulationRate(7_776_000,1),false)})
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var warpScene,out error)&&warpScene is not null,$"Florida warp scene {rate}: {error}");
        var warpCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,warpScene!.Projection,CameraMode.Free);
        Check(warpScene.TryStartAtFloridaLaunchSite(warpCamera),$"Florida warp startup {rate}");
        var targetIndex=SimulationSpeedPresets.IndexOf(rate);
        while(warpScene.SpeedPresetIndex>targetIndex)warpScene.ApplyPresentationInput(warpCamera,new NativeInputState{RateDecrease=1},out _,out _);
        while(warpScene.SpeedPresetIndex<targetIndex)warpScene.ApplyPresentationInput(warpCamera,new NativeInputState{RateIncrease=1},out _,out _);
        if(paused)warpScene.ApplyPresentationInput(warpCamera,new NativeInputState{PauseToggle=1},out _,out _);
        AnchoredSurfaceObjectPose beforeWarp=default,afterWarp=default;
        Check(warpScene.TryEvaluateFloridaLaunchSite(out beforeWarp)&&
            warpScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),warpCamera,out error)&&
            warpScene.TryEvaluateFloridaLaunchSite(out afterWarp),$"Florida warp evaluation {rate}, paused={paused}: {error}");
        Check(warpScene.FloridaLaunchSite.Object.Anchor==anchor&&afterWarp.BodyFixedPosition==beforeWarp.BodyFixedPosition&&
            afterWarp.BodyFixedOrientation==beforeWarp.BodyFixedOrientation&&afterWarp.PhysicalTerrainHeightMetres==beforeWarp.PhysicalTerrainHeightMetres,
            $"Florida body-fixed/terrain-relative drift is zero at {rate}, paused={paused}");
        Check(paused?afterWarp.RootPosition==beforeWarp.RootPosition:afterWarp.RootPosition!=beforeWarp.RootPosition,
            $"Florida root motion follows only authoritative Earth motion at {rate}, paused={paused}");
    }

    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(scene.TryStartAtFloridaLaunchSite(camera)&&scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative&&
        scene.CurrentSurfaceCameraState.Anchor==anchor,"direct startup enters the canonical site in SurfaceRelative mode");
    Check(scene.TryGetFloridaLaunchSitePresentation(camera,out var presentedPosition,out var presentedOrientation)&&
        presentedPosition==pose.RootPosition&&presentedOrientation==pose.RootOrientation,"near site produces precise root presentation");
    var cameraBodyBefore=scene.CurrentSurfaceCameraState.Anchor;
    scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=25,MouseDeltaY=-12,DeltaSeconds=1f/60f},out _,out _);
    Check(scene.FloridaLaunchSite.Object.Anchor==anchor&&scene.CurrentSurfaceCameraState.Anchor==cameraBodyBefore,
        "camera look has no authority over pad geography");
    Check(scene.DetachSurfaceCamera(camera)&&scene.TryAttachSurfaceCamera(camera)&&scene.FloridaLaunchSite.Object.Anchor==anchor,
        "SurfaceRelative detach/attach leaves pad authority unchanged");
    foreach(var focus in new[]{NativePresentationFocus.Sun,NativePresentationFocus.Venus,NativePresentationFocus.Mars,NativePresentationFocus.Moon})
    {
        Check(scene.Focus(camera,focus)&&scene.FloridaLaunchSite.Object.Anchor==anchor&&scene.TryStartAtFloridaLaunchSite(camera)&&
            scene.TryEvaluateFloridaLaunchSite(out var returnedPose)&&returnedPose.BodyFixedPosition==bodyFixed&&
            returnedPose.BodyFixedOrientation==bodyOrientation,$"{focus} focus and Florida return preserve the same geographic pad");
    }
    var farCamera=new CameraState(new FramePosition(root,pose.RootPosition.Value+pose.Enu.Up*FloridaLaunchSite.MaximumRenderDistanceMetres*2d),
        DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(!scene.TryGetFloridaLaunchSitePresentation(farCamera,out _,out _),"human-scale geometry is culled at distance");
    Check(MeshHandle.FloridaLaunchPad.Value==3,"stable native launchpad mesh identity");
    Check(site.Object.DeterministicHash==objectHash&&objectHash==0x5B21D11ADAC71C6FUL,
        $"M12 site fingerprint binds the same geographic reservation to the regional physical height: 0x{objectHash:X16}");

    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var sliceCameraScene,out error)&&sliceCameraScene is not null,
        $"Florida vertical-slice production camera scene: {error}");
    var sliceCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,sliceCameraScene!.Projection,CameraMode.Free);
    Check(sliceCameraScene.TryStartAtFloridaValidationAltitude(sliceCamera,3_000_000d),
        "Florida launch site starts through normal Solar camera authority");
    var startupAim=(sliceCameraScene.CurrentVisualAimRoot-sliceCamera.Position.Value).Normalized();
    var startupForward=sliceCamera.Orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();
    Check(Double3.Dot(startupAim,startupForward)>.999999999999d,
        "Florida validation startup looks through the authoritative Solar visual aim");
    var immutableEarthOrientation=sliceCameraScene.FocusedBody.BodyFixedToRoot;
    var immutableAnchor=site.Object.Anchor;
    for(var step=0;step<192&&(sliceCameraScene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||
        sliceCameraScene.SurfaceAnchorBlend<1d||sliceCameraScene.SurfaceCameraMode!=PlanetaryCameraPresentationMode.SurfaceLocal);step++)
        sliceCameraScene.ApplyPresentationInput(sliceCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);
    Check(sliceCameraScene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial&&
        sliceCameraScene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&sliceCameraScene.SurfaceAnchorBlend==1d,
        "Florida approach uses the production inertial SurfaceAnchor handoff");
    var nearAnchor=sliceCameraScene.CurrentFocusTarget.SurfaceAnchor;
    var nearPosition=sliceCamera.Position.Value;
    for(var look=0;look<8;look++)
        sliceCameraScene.ApplyPresentationInput(sliceCamera,new NativeInputState{LookActive=1,MouseDeltaY=-100f},out _,out _);
    Check(sliceCamera.Position.Value==nearPosition&&sliceCameraScene.CurrentFocusTarget.SurfaceAnchor==nearAnchor&&
        sliceCameraScene.FocusedBody.BodyFixedToRoot==immutableEarthOrientation,
        "Florida near-surface horizon look is orientation-only and geography-stable");

    var retreatOrientation=sliceCamera.Orientation;var retreatMaximumOrientationStep=0d;var retreatMinimumClearance=double.PositiveInfinity;
    var retreatMaximumDistanceRatio=1d;var previousDistance=Math.Sqrt((sliceCamera.Position.Value-sliceCameraScene.FocusedBody.Position.Value).LengthSquared);
    for(var step=0;step<192&&(sliceCameraScene.CurrentFocusTarget.Kind!=FocusTargetKind.BodyCenter||sliceCameraScene.HasRetainedVisualAim);step++)
    {
        var beforeOrientation=sliceCamera.Orientation;
        sliceCameraScene.ApplyPresentationInput(sliceCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);
        retreatMaximumOrientationStep=Math.Max(retreatMaximumOrientationStep,
            SurfaceCameraAuthority.QuaternionAngularError(sliceCamera.Orientation,beforeOrientation));
        retreatMinimumClearance=Math.Min(retreatMinimumClearance,sliceCameraScene.SurfaceAltitudeMetres);
        var distance=Math.Sqrt((sliceCamera.Position.Value-sliceCameraScene.FocusedBody.Position.Value).LengthSquared);
        retreatMaximumDistanceRatio=Math.Max(retreatMaximumDistanceRatio,Math.Max(distance/previousDistance,previousDistance/distance));
        previousDistance=distance;
        Check(sliceCamera.Position.Value.IsFinite&&sliceCamera.Orientation.IsFinite&&
            sliceCameraScene.CurrentInertialCameraOffset.IsFinite&&
            sliceCameraScene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
            $"Florida retreat frame {step} is finite and exterior");
    }
    Check(sliceCameraScene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&!sliceCameraScene.HasRetainedVisualAim&&
        retreatMaximumOrientationStep<1e-7d&&sliceCamera.Orientation==retreatOrientation&&retreatMaximumDistanceRatio<=1.251d,
        "Florida retreat releases visual aim continuously into ordinary BodyCenter orbit without orientation or zoom reinterpretation");
    var beforeOrbitDistance=Math.Sqrt((sliceCamera.Position.Value-sliceCameraScene.FocusedBody.Position.Value).LengthSquared);
    sliceCameraScene.ApplyPresentationInput(sliceCamera,new NativeInputState{LookActive=1,MouseDeltaX=40f,MouseDeltaY=-18f},out _,out _);
    var afterOrbitDistance=Math.Sqrt((sliceCamera.Position.Value-sliceCameraScene.FocusedBody.Position.Value).LengthSquared);
    var bodyForward=(sliceCameraScene.FocusedBody.Position.Value-sliceCamera.Position.Value).Normalized();
    var cameraForward=sliceCamera.Orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();
    Check(Math.Abs(afterOrbitDistance-beforeOrbitDistance)<=beforeOrbitDistance*1e-12d&&
        Double3.Dot(bodyForward,cameraForward)>.999999999999d&&sliceCameraScene.CurrentFocusTarget.Kind==FocusTargetKind.BodyCenter&&
        sliceCameraScene.FocusedBody.BodyFixedToRoot==immutableEarthOrientation&&sliceCameraScene.FloridaLaunchSite.Object.Anchor==immutableAnchor,
        "Florida far click-drag is restored to centered planet orbit with unchanged body and anchored geography");
    for(var step=0;step<192&&sliceCameraScene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor;step++)
        sliceCameraScene.ApplyPresentationInput(sliceCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);
    Check(sliceCameraScene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&sliceCameraScene.FloridaLaunchSite.Object.Anchor==immutableAnchor,
        "repeated Florida far-to-near traversal deterministically reacquires the same surface authority");
    Console.WriteLine($"Florida production camera cycle: orientationStep={retreatMaximumOrientationStep:E9} rad; distanceRatio={retreatMaximumDistanceRatio:R}; minimumClearance={retreatMinimumClearance:R} m; centeredDot={Double3.Dot(bodyForward,cameraForward):R}; authority={sliceCameraScene.CurrentCameraReferenceAuthority}; projection=infinite-far Solar");

    Check(scene.TryEvaluateFloridaLaunchSite(out _)&&scene.TryGetFloridaLaunchSitePresentation(camera,out _,out _),"warm Florida evaluation/submission");
    var evaluationAllocationStart=GC.GetAllocatedBytesForCurrentThread();var evaluationStart=Stopwatch.GetTimestamp();double evaluationChecksum=0d;
    for(var sample=0;sample<100_000;sample++){Check(scene.TryEvaluateFloridaLaunchSite(out var evaluated),"Florida evaluation benchmark");evaluationChecksum+=evaluated.BodyFixedPosition.X;}
    var evaluationTicks=Stopwatch.GetTimestamp()-evaluationStart;var evaluationAllocated=GC.GetAllocatedBytesForCurrentThread()-evaluationAllocationStart;
    var presentationAllocationStart=GC.GetAllocatedBytesForCurrentThread();var presentationStart=Stopwatch.GetTimestamp();double presentationChecksum=0d;
    for(var sample=0;sample<100_000;sample++){Check(scene.TryGetFloridaLaunchSitePresentation(camera,out var evaluatedPosition,out _),"Florida presentation benchmark");presentationChecksum+=evaluatedPosition.Value.X;}
    var presentationTicks=Stopwatch.GetTimestamp()-presentationStart;var presentationAllocated=GC.GetAllocatedBytesForCurrentThread()-presentationAllocationStart;
    var evaluationNanoseconds=evaluationTicks*1_000_000_000d/Stopwatch.Frequency/100_000d;
    var presentationNanoseconds=presentationTicks*1_000_000_000d/Stopwatch.Frequency/100_000d;
    Check(evaluationAllocated==0&&presentationAllocated==0&&double.IsFinite(evaluationChecksum)&&double.IsFinite(presentationChecksum),
        "anchored-object evaluation and bounded presentation submission allocate zero managed bytes after warmup");
    Console.WriteLine($"Florida launch site: terrain={site.AnchorTerrainHeightMetres:R} m; foundation={site.FoundationOffsetMetres:R} m; radius={site.LocalPhysicalSurfaceRadiusMetres:R} m; eastError={eastError:R}; northError={northError:R}; evaluation={evaluationNanoseconds:F1} ns; presentation={presentationNanoseconds:F1} ns; allocations={evaluationAllocated+presentationAllocated}; hash=0x{objectHash:X16}");
}

static void SurfaceRelativeCameraAuthorityTest()
{
    var root=new ReferenceFrameId(706);
    var terrainDefinition=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    var terrain=new PlanetaryPhysicalTerrainAuthority(SolarSystemBodyIds.Earth.Value,terrainDefinition);
    var latitude=66.5607d*Math.PI/180d;var longitude=-32d*Math.PI/180d;
    var site=BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,longitude);
    double maximumAttachPosition=0d,maximumAttachPivot=0d,maximumAttachOrientation=0d,maximumDetachPosition=0d,maximumDetachPivot=0d,maximumDetachOrientation=0d;
    double maximumCanonicalEyeDrift=0d,maximumPivotDrift=0d,maximumRootRoundTripDrift=0d,maximumTerrainRelativeDrift=0d,maximumRootMotion=0d;
    double maximumOrientationDrift=0d,minimumClearance=double.PositiveInfinity;long evaluationAllocations=0;double evaluationNanoseconds=0d;

    SolarSystemScene CreateAtSite(out CameraState camera,Double3? requestedSite=null,bool horizonView=false)
    {
        Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error)&&value is not null,$"surface camera scene: {error}");
        var scene=value!;camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
        Check(scene.Focus(camera,NativePresentationFocus.Earth),"surface camera Earth focus");
        var selectedSite=(requestedSite??site).Normalized();var earth=scene.FocusedBody;var height=terrainDefinition.SampleHeight(selectedSite,24);var bodyEye=selectedSite*(earth.RadiusMetres+height+25d);
        camera.Position=camera.Position with{Value=earth.Position.Value+earth.BodyFixedToRoot.Rotate(bodyEye)};
        if(horizonView)
        {
            Check(SurfaceAnchor.TryCreate(earth.BodyId,terrain.AuthorityVersion,selectedSite,0d,out var cameraAnchor)==SurfaceAnchorCreationStatus.Success,
                "surface camera horizon fixture anchor");
            var cameraEnu=default(SurfaceEnuFrame);Check(SurfaceEnuFrame.TryCreate(cameraAnchor,out cameraEnu),"surface camera horizon fixture ENU");
            camera.Orientation=(earth.BodyFixedToRoot*SurfaceCameraState.LookOrientation(cameraEnu,0d,0d)).Normalized();
        }
        else
        {
            var radial=earth.BodyFixedToRoot.Rotate(selectedSite);var yaw=Math.Atan2(radial.X,radial.Z);var pitch=-Math.Asin(Math.Clamp(radial.Y,-1d,1d));
            camera.Orientation=(DoubleQuaternion.FromAxisAngle(Double3.UnitY,yaw)*DoubleQuaternion.FromAxisAngle(Double3.UnitX,pitch)).Normalized();
        }
        scene.EnforceFinalCameraInvariant(camera);
        return scene;
    }

    static Double3 GeographicDirection(double latitudeDegrees,double longitudeDegrees)
    {
        var latitudeRadians=latitudeDegrees*Math.PI/180d;var longitudeRadians=longitudeDegrees*Math.PI/180d;
        return BodyFixedGeography.DirectionFromLatitudeLongitude(latitudeRadians,longitudeRadians);
    }

    var horizonSites=new[]{
        GeographicDirection(0d,0d),GeographicDirection(42d,-71d),GeographicDirection(82d,34d),
        GeographicDirection(89.999999d,45d),GeographicDirection(-89.999999d,-30d),
        new Double3(1d,1d,0d).Normalized(),new Double3(1d,0d,1d).Normalized(),
        new Double3(1d,1d,1d).Normalized()};
    double maximumHorizonError=0d,maximumLocalRollError=0d;
    foreach(var horizonSite in horizonSites)
    {
        Check(SurfaceAnchor.TryCreate(SolarSystemBodyIds.Earth.Value,terrain.AuthorityVersion,horizonSite,0d,out var anchor)==SurfaceAnchorCreationStatus.Success,
            "surface free-look horizon anchor");
        var enu=default(SurfaceEnuFrame);Check(SurfaceEnuFrame.TryCreate(anchor,out enu),"surface free-look horizon site");
        foreach(var pitch in new[]{PlanetarySurfaceCameraPolicy.MinimumPitchRadians,0d,PlanetarySurfaceCameraPolicy.MaximumPitchRadians})
        {
            var orientation=SurfaceCameraState.LookOrientation(enu,.731d,pitch);
            var forward=orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();
            var actualRight=orientation.Rotate(Double3.UnitX).Normalized();
            var expectedRight=Double3.Cross(forward,enu.Up).Normalized();
            maximumLocalRollError=Math.Max(maximumLocalRollError,
                Math.Acos(Math.Clamp(Double3.Dot(actualRight,expectedRight),-1d,1d)));
            if(pitch==0d)maximumHorizonError=Math.Max(maximumHorizonError,
                Math.Abs(Math.Asin(Math.Clamp(Double3.Dot(forward,enu.Up),-1d,1d))));
        }
    }
    Check(maximumHorizonError<1e-14d&&maximumLocalRollError<1e-7d,
        "surface free-look is horizon-exact, pole-safe, cube-boundary-safe, and roll-free");
    Check(PlanetarySurfaceCameraPolicy.ApplyPitchDelta(0d,100d)==PlanetarySurfaceCameraPolicy.MaximumPitchRadians&&
        PlanetarySurfaceCameraPolicy.ApplyPitchDelta(0d,-100d)==PlanetarySurfaceCameraPolicy.MinimumPitchRadians&&
        PlanetarySurfaceCameraPolicy.MinimumPitchRadians==-PlanetarySurfaceCameraPolicy.MaximumPitchRadians,
        "surface free-look uses symmetric non-inverting pitch limits");

    foreach(var (label,rate,paused) in new[]{
        ("Pause",SimulationRate.One,true),("0.1x",new SimulationRate(1,10),false),("1x",SimulationRate.One,false),
        ("2x",SimulationRate.Two,false),("10x",SimulationRate.Ten,false),("30x",new SimulationRate(30,1),false),
        ("600x",new SimulationRate(600,1),false)})
    {
        var scene=CreateAtSite(out var camera);
        Check(scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial,
            $"{label} low altitude remains inertial until explicit attach");
        var beforeAttachPosition=camera.Position.Value;var beforeAttachOrientation=camera.Orientation;
        Check(scene.TryAttachSurfaceCamera(camera),$"{label} explicit surface camera attach");
        var attach=scene.SurfaceCameraLastTransitionMetrics;
        maximumAttachPosition=Math.Max(maximumAttachPosition,attach.PositionErrorMetres);
        maximumAttachPivot=Math.Max(maximumAttachPivot,attach.PivotErrorMetres);
        maximumAttachOrientation=Math.Max(maximumAttachOrientation,attach.OrientationErrorRadians);
        Check(scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative&&scene.CurrentSurfaceCameraState.IsValid&&
            Math.Sqrt((camera.Position.Value-beforeAttachPosition).LengthSquared)<1e-4d&&SurfaceCameraAuthority.QuaternionAngularError(camera.Orientation,beforeAttachOrientation)<1e-7d,
            $"{label} attach is atomic and pose-continuous");
        var canonical=scene.CurrentSurfaceCameraState;var anchorIdentity=canonical.Anchor;var beforeBody=scene.FocusedBody;
        Check(SurfaceCameraAuthority.TryEvaluate(beforeBody,canonical,terrain,out var beforePose),$"{label} evaluate retained surface pose");
        var beforeRoot=camera.Position.Value;var beforeCameraToTerrain=beforePose.BodyFixedEye-
            anchorIdentity.NormalizedBodyFixedDirection*(beforeBody.RadiusMetres+beforePose.PhysicalTerrainHeightMetres);
        var requested=SimulationSpeedPresets.IndexOf(rate);while(scene.SpeedPresetIndex<requested)scene.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out _,out _);while(scene.SpeedPresetIndex>requested)scene.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out _,out _);
        if(paused)scene.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);
        for(var frame=0;frame<1_000;frame++)Check(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(.01d),camera,out var error),$"{label} stationary frame {frame}: {error}");
        var afterBody=scene.FocusedBody;var afterState=scene.CurrentSurfaceCameraState;
        Check(SurfaceCameraAuthority.TryEvaluate(afterBody,afterState,terrain,out var afterPose),$"{label} evaluate final surface pose");
        var afterCameraBody=afterBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(camera.Position.Value-afterBody.Position.Value);
        var afterCameraToTerrain=afterPose.BodyFixedEye-anchorIdentity.NormalizedBodyFixedDirection*(afterBody.RadiusMetres+afterPose.PhysicalTerrainHeightMetres);
        var canonicalEyeDrift=Math.Sqrt((afterPose.BodyFixedEye-beforePose.BodyFixedEye).LengthSquared);
        var pivotDrift=Math.Sqrt((afterPose.BodyFixedPivot-beforePose.BodyFixedPivot).LengthSquared);
        var rootRoundTripDrift=Math.Sqrt((afterCameraBody-afterPose.BodyFixedEye).LengthSquared);
        var terrainDrift=Math.Sqrt((afterCameraToTerrain-beforeCameraToTerrain).LengthSquared);
        var rootMotion=Math.Sqrt((camera.Position.Value-beforeRoot).LengthSquared);
        var rootOrientationExpected=(afterBody.BodyFixedToRoot*afterState.BodyFixedOrientation).Normalized();
        var orientationDrift=SurfaceCameraAuthority.QuaternionAngularError(camera.Orientation,rootOrientationExpected);
        maximumCanonicalEyeDrift=Math.Max(maximumCanonicalEyeDrift,canonicalEyeDrift);
        maximumPivotDrift=Math.Max(maximumPivotDrift,pivotDrift);
        maximumRootRoundTripDrift=Math.Max(maximumRootRoundTripDrift,rootRoundTripDrift);
        maximumTerrainRelativeDrift=Math.Max(maximumTerrainRelativeDrift,terrainDrift);
        maximumRootMotion=Math.Max(maximumRootMotion,rootMotion);
        maximumOrientationDrift=Math.Max(maximumOrientationDrift,orientationDrift);
        minimumClearance=Math.Min(minimumClearance,scene.SurfaceAltitudeMetres);
        Console.WriteLine($"Surface camera {label}: stateEqual={afterState==canonical}; anchorEqual={afterState.Anchor==anchorIdentity}; eye={canonicalEyeDrift:E9}; pivot={pivotDrift:E9}; terrain={terrainDrift:E9}; rootRoundTrip={rootRoundTripDrift:E9}; orientation={orientationDrift:E9}; altitude={scene.SurfaceAltitudeMetres:R}; rootMotion={rootMotion:R}");
        Check(afterState.Anchor==anchorIdentity&&afterState==canonical&&canonicalEyeDrift==0d&&pivotDrift==0d&&terrainDrift==0d&&
            rootRoundTripDrift<1e-3d&&orientationDrift<1e-7d&&
            scene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
            $"{label} canonical body-fixed camera and terrain remain stationary");
        if(paused)Check(rootMotion==0d,"paused surface camera root is stationary");else Check(rootMotion>0d,"active warp re-evaluates root pose through rotating Earth");
        var beforeDetach=camera.Position.Value;var beforeDetachOrientation=camera.Orientation;
        var beforeDetachPivot=scene.CurrentVisualAimRoot;
        Check(scene.DetachSurfaceCamera(camera),$"{label} explicit surface camera detach");
        maximumDetachPosition=Math.Max(maximumDetachPosition,Math.Sqrt((camera.Position.Value-beforeDetach).LengthSquared));
        maximumDetachPivot=Math.Max(maximumDetachPivot,Math.Sqrt((scene.CurrentVisualAimRoot-beforeDetachPivot).LengthSquared));
        maximumDetachOrientation=Math.Max(maximumDetachOrientation,SurfaceCameraAuthority.QuaternionAngularError(camera.Orientation,beforeDetachOrientation));
        Check(scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial&&
            Math.Sqrt((camera.Position.Value-beforeDetach).LengthSquared)<1e-3d&&SurfaceCameraAuthority.QuaternionAngularError(camera.Orientation,beforeDetachOrientation)<1e-7d,
            $"{label} detach resolves the exact current pose into inertial authority");
    }

    var inertial=CreateAtSite(out var inertialCamera);
    Check(inertial.TryAttachSurfaceCamera(inertialCamera)&&inertial.DetachSurfaceCamera(inertialCamera),
        "surface authority round trip seeds the exact retained inertial pose");
    var inertialBeforeBody=inertial.FocusedBody;
    var inertialBeforeBodyEye=inertialBeforeBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(inertialCamera.Position.Value-inertialBeforeBody.Position.Value);
    Check(CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,inertial.CurrentTime,out var inertialOrientation),
        "surface authority inertial control orientation");
    var inertialExpectedSpeed=Math.Sqrt(Double3.Cross(inertialOrientation.AngularVelocityInInertial,
        inertialBeforeBody.BodyFixedToRoot.Rotate(inertialBeforeBodyEye)).LengthSquared);
    Check(inertial.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(.1d),inertialCamera,out var inertialError),
        $"surface authority inertial control advance: {inertialError}");
    var inertialAfterBody=inertial.FocusedBody;
    var inertialAfterBodyEye=inertialAfterBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(inertialCamera.Position.Value-inertialAfterBody.Position.Value);
    var inertialMeasuredSpeed=Math.Sqrt((inertialAfterBodyEye-inertialBeforeBodyEye).LengthSquared)/.1d;
    Check(Math.Abs(inertialMeasuredSpeed-inertialExpectedSpeed)/inertialExpectedSpeed<.002d,
        "explicit detach restores Earth-relative inertial drift instead of retaining surface authority");
    Console.WriteLine($"Surface camera inertial control: expected={inertialExpectedSpeed:R} m/s; measured={inertialMeasuredSpeed:R} m/s; latitude={latitude*180d/Math.PI:R} deg");

    var explicitToggle=CreateAtSite(out var explicitToggleCamera);
    explicitToggle.ApplyPresentationInput(explicitToggleCamera,new NativeInputState{MoveUp=1},out _,out _);
    Check(explicitToggle.CurrentCameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative,
        "explicit E/MoveUp edge attaches surface-relative authority");
    explicitToggle.ApplyPresentationInput(explicitToggleCamera,new NativeInputState{MoveUp=1},out _,out _);
    Check(explicitToggle.CurrentCameraReferenceAuthority==CameraReferenceAuthority.SurfaceRelative,
        "held surface-authority control does not oscillate ownership");
    explicitToggle.ApplyPresentationInput(explicitToggleCamera,default,out _,out _);
    explicitToggle.ApplyPresentationInput(explicitToggleCamera,new NativeInputState{MoveUp=1},out _,out _);
    Check(explicitToggle.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial,
        "second explicit E/MoveUp edge detaches surface-relative authority");
    Check(explicitToggle.Focus(explicitToggleCamera,NativePresentationFocus.Mars)&&!explicitToggle.TryAttachSurfaceCamera(explicitToggleCamera)&&
        explicitToggle.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial,
        "unsupported body remains in bounded inertial authority without altitude-inferred activation");

    var stress=CreateAtSite(out var stressCamera,horizonView:true);Check(stress.TryAttachSurfaceCamera(stressCamera),"surface input stress attach");
    var fixedAnchor=stress.CurrentSurfaceCameraState.Anchor;
    var fixedEye=stress.CurrentSurfaceCameraState.EyeOffsetEnuMetres;var fixedPivot=stress.CurrentSurfaceCameraState.PivotOffsetEnuMetres;
    stress.ApplyPresentationInput(stressCamera,new NativeInputState{LookActive=1,MouseDeltaX=90,MouseDeltaY=-45},out _,out _);
    Check(stress.CurrentSurfaceCameraState.Anchor==fixedAnchor&&
        stress.CurrentSurfaceCameraState.EyeOffsetEnuMetres==fixedEye&&stress.CurrentSurfaceCameraState.PivotOffsetEnuMetres==fixedPivot,
        "rotation-only surface free-look preserves canonical anchor, eye, and pivot geography");
    stress.ApplyPresentationInput(stressCamera,new NativeInputState{MoveForward=1,MoveRight=1,DeltaSeconds=.1f},out _,out _);
    Check(stress.CurrentSurfaceCameraState.Anchor!=fixedAnchor,
        "surface translation explicitly moves canonical geography");
    for(var cycle=0;cycle<100;cycle++)
    {
        stress.ApplyPresentationInput(stressCamera,new NativeInputState{LookActive=1,MouseDeltaX=(cycle%9)-4,MouseDeltaY=(cycle%7)-3,MouseWheelDetents=(cycle&1)==0?1:-1},out _,out _);
        stress.EnforceFinalCameraInvariant(stressCamera);minimumClearance=Math.Min(minimumClearance,stress.SurfaceAltitudeMetres);
        var position=stressCamera.Position.Value;var orientation=stressCamera.Orientation;
        Check(stress.DetachSurfaceCamera(stressCamera)&&stress.TryAttachSurfaceCamera(stressCamera),$"surface transition stress {cycle}");
        maximumDetachPosition=Math.Max(maximumDetachPosition,Math.Sqrt((stressCamera.Position.Value-position).LengthSquared));
        maximumDetachOrientation=Math.Max(maximumDetachOrientation,SurfaceCameraAuthority.QuaternionAngularError(stressCamera.Orientation,orientation));
        maximumAttachPosition=Math.Max(maximumAttachPosition,stress.SurfaceCameraLastTransitionMetrics.PositionErrorMetres);
        maximumAttachOrientation=Math.Max(maximumAttachOrientation,stress.SurfaceCameraLastTransitionMetrics.OrientationErrorRadians);
        Check(stress.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres&&stress.CurrentSurfaceCameraState.IsValid,
            $"surface transition stress {cycle} remains finite and exterior");
    }

    var transitionSites=new[]{
        site,GeographicDirection(0d,0d),GeographicDirection(-34.6d,18.5d),
        GeographicDirection(89.9d,45d),GeographicDirection(27.9881d,86.925d)};
    var transitionIndex=0;
    foreach(var transitionSite in transitionSites)
    {
        var transitionScene=CreateAtSite(out var transitionCamera,transitionSite);
        Check(transitionScene.TryAttachSurfaceCamera(transitionCamera),$"geographic transition site {transitionIndex} attach");
        for(var cycle=0;cycle<20;cycle++)
        {
            transitionScene.ApplyPresentationInput(transitionCamera,new NativeInputState{
                LookActive=1,MouseDeltaX=(cycle%5)-2,MouseDeltaY=(cycle%3)-1,
                MouseWheelDetents=(cycle&1)==0?1:-1},out _,out _);
            var beforePosition=transitionCamera.Position.Value;var beforePivot=transitionScene.CurrentVisualAimRoot;
            var beforeOrientation=transitionCamera.Orientation;
            Check(transitionScene.DetachSurfaceCamera(transitionCamera)&&transitionScene.TryAttachSurfaceCamera(transitionCamera),
                $"geographic transition site {transitionIndex} cycle {cycle}");
            var transitionPositionError=Math.Sqrt((transitionCamera.Position.Value-beforePosition).LengthSquared);
            var transitionPivotError=Math.Sqrt((transitionScene.CurrentVisualAimRoot-beforePivot).LengthSquared);
            var transitionOrientationError=SurfaceCameraAuthority.QuaternionAngularError(transitionCamera.Orientation,beforeOrientation);
            maximumDetachPosition=Math.Max(maximumDetachPosition,transitionPositionError);
            maximumDetachPivot=Math.Max(maximumDetachPivot,transitionPivotError);
            maximumDetachOrientation=Math.Max(maximumDetachOrientation,transitionOrientationError);
            maximumAttachPosition=Math.Max(maximumAttachPosition,transitionScene.SurfaceCameraLastTransitionMetrics.PositionErrorMetres);
            maximumAttachPivot=Math.Max(maximumAttachPivot,transitionScene.SurfaceCameraLastTransitionMetrics.PivotErrorMetres);
            maximumAttachOrientation=Math.Max(maximumAttachOrientation,transitionScene.SurfaceCameraLastTransitionMetrics.OrientationErrorRadians);
            minimumClearance=Math.Min(minimumClearance,transitionScene.SurfaceAltitudeMetres);
            Check(transitionPositionError<1e-3d&&transitionPivotError<1e-3d&&transitionOrientationError<1e-7d&&
                transitionScene.SurfaceAltitudeMetres>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
                $"geographic transition site {transitionIndex} cycle {cycle} is atomic and exterior");
        }
        transitionIndex++;
    }

    double MoveAtPitch(double pitchRadians,uint fastModifier,uint slowModifier,out double clearance)
    {
        var movementScene=CreateAtSite(out var movementCamera,GeographicDirection(37d,-122d),horizonView:true);
        Check(movementScene.TryAttachSurfaceCamera(movementCamera),"pitch-independent movement attach");
        movementScene.ApplyPresentationInput(movementCamera,new NativeInputState{
            LookActive=1,MouseDeltaY=(float)(-pitchRadians/.002d)},out _,out _);
        var before=movementScene.CurrentSurfaceCameraState.Anchor.NormalizedBodyFixedDirection;
        movementScene.ApplyPresentationInput(movementCamera,new NativeInputState{
            MoveForward=1,DeltaSeconds=.1f,FastModifier=fastModifier,SlowModifier=slowModifier},out _,out _);
        var after=movementScene.CurrentSurfaceCameraState.Anchor.NormalizedBodyFixedDirection;
        clearance=movementScene.SurfaceAltitudeMetres;
        return Math.Sqrt((after-before).LengthSquared)*movementScene.FocusedBody.RadiusMetres/.1d;
    }
    var levelSpeed=MoveAtPitch(0d,0,0,out var levelClearance);
    var upSpeed=MoveAtPitch(80d*Math.PI/180d,0,0,out var upClearance);
    var downSpeed=MoveAtPitch(-80d*Math.PI/180d,0,0,out var downClearance);
    var fastSpeed=MoveAtPitch(0d,1,0,out var fastClearance);
    var slowSpeed=MoveAtPitch(0d,0,1,out var slowClearance);
    var pitchSpeedError=Math.Max(Math.Abs(upSpeed-levelSpeed),Math.Abs(downSpeed-levelSpeed))/levelSpeed;
    Console.WriteLine($"Surface free-look controls: horizonError={maximumHorizonError:E9} rad; rollError={maximumLocalRollError:E9} rad; pitch=[{PlanetarySurfaceCameraPolicy.MinimumPitchRadians*180d/Math.PI:R},{PlanetarySurfaceCameraPolicy.MaximumPitchRadians*180d/Math.PI:R}] deg; speeds={slowSpeed:R}/{levelSpeed:R}/{fastSpeed:R} m/s; pitchSpeedError={pitchSpeedError:E9}; ratios={slowSpeed/levelSpeed:R}/{fastSpeed/levelSpeed:R}; clearances={levelClearance:R}/{upClearance:R}/{downClearance:R}/{fastClearance:R}/{slowClearance:R}");
    Check(pitchSpeedError<1e-8d&&Math.Abs(fastSpeed/levelSpeed-PlanetarySurfaceCameraPolicy.FastSurfaceSpeedMultiplier)<1e-8d&&
        Math.Abs(slowSpeed/levelSpeed-PlanetarySurfaceCameraPolicy.SlowSurfaceSpeedMultiplier)<1e-8d&&
        new[]{levelClearance,upClearance,downClearance,fastClearance,slowClearance}.All(value=>value>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres),
        "surface WASD is yaw-only, pitch-independent, modifier-scaled, and terrain-safe");

    var benchmarkBody=stress.FocusedBody;var benchmarkState=stress.CurrentSurfaceCameraState;
    Check(SurfaceCameraAuthority.TryEvaluate(benchmarkBody,benchmarkState,terrain,out _),"warm surface-camera evaluator");
    var timer=new Stopwatch();var beforeAllocation=GC.GetAllocatedBytesForCurrentThread();timer.Start();double checksum=0d;
    for(var index=0;index<100_000;index++){Check(SurfaceCameraAuthority.TryEvaluate(benchmarkBody,benchmarkState,terrain,out var pose),"surface camera benchmark evaluation");checksum+=pose.BodyFixedEye.X;}
    timer.Stop();evaluationAllocations=GC.GetAllocatedBytesForCurrentThread()-beforeAllocation;evaluationNanoseconds=timer.Elapsed.TotalNanoseconds/100_000d;
    Check(evaluationAllocations==0&&checksum!=0d,"surface-camera hot evaluation allocates zero bytes");

    Console.WriteLine($"Surface camera authority: attach={maximumAttachPosition:E9} m/{maximumAttachPivot:E9} m/{maximumAttachOrientation:E9} rad; detach={maximumDetachPosition:E9} m/{maximumDetachPivot:E9} m/{maximumDetachOrientation:E9} rad; canonicalEyeDrift={maximumCanonicalEyeDrift:E9} m; pivotDrift={maximumPivotDrift:E9} m; rootRoundTrip={maximumRootRoundTripDrift:E9} m; terrainDrift={maximumTerrainRelativeDrift:E9} m; rootMotion={maximumRootMotion:R} m; orientation={maximumOrientationDrift:E9} rad; minClearance={minimumClearance:R} m; evaluation={evaluationNanoseconds:F1} ns; allocations={evaluationAllocations}");
}

static void NearSurfaceInertialFreeLookRegressionTest()
{
    var root=new ReferenceFrameId(0x77D1);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var sceneValue,out var error)&&sceneValue is not null,
        $"near-surface free-look scene: {error}");
    var scene=sceneValue!;
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    Check(scene.Focus(camera,NativePresentationFocus.Earth),"near-surface free-look focuses Earth");
    for(var step=0;step<192&&(scene.CurrentFocusTarget.Kind!=FocusTargetKind.SurfaceAnchor||
        scene.SurfaceAnchorBlend<1d||scene.SurfaceCameraMode!=PlanetaryCameraPresentationMode.SurfaceLocal);step++)
        scene.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=10_000},out _,out _);

    Check(scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial&&
        scene.CurrentFocusTarget.Kind==FocusTargetKind.SurfaceAnchor&&scene.SurfaceAnchorBlend==1d&&
        scene.SurfaceCameraMode==PlanetaryCameraPresentationMode.SurfaceLocal,
        $"normal Solar descent reaches inertial SurfaceLocal authority: authority={scene.CurrentCameraReferenceAuthority}; target={scene.CurrentFocusTarget.Kind}; blend={scene.SurfaceAnchorBlend:R}; altitude={scene.SurfaceAltitudeMetres:R}");

    var body=scene.FocusedBody;
    var anchor=scene.CurrentFocusTarget.SurfaceAnchor;
    var tangent=anchor.LocalTangentBasis;
    var truth=scene.Presentation.Bodies.ToArray();
    var initialPosition=camera.Position.Value;
    var initialOffset=scene.CurrentInertialCameraOffset;
    var enu=new SurfaceEnuFrame(tangent.East,tangent.North,tangent.Up);
    var initialBodyOrientation=(body.BodyFixedToRoot.Conjugate().Normalized()*camera.Orientation).Normalized();
    Check(SurfaceCameraState.TryExtractLocalLook(initialBodyOrientation,enu,out _,out var initialPitch),
        "initial inertial SurfaceLocal view resolves in the retained tangent frame");

    var previousPitch=initialPitch;
    var crossedHorizon=false;
    var continuedAboveHorizon=false;
    var maximumPositionDrift=0d;
    var minimumClearance=double.PositiveInfinity;
    for(var step=0;step<12;step++)
    {
        var beforePosition=camera.Position.Value;
        scene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaY=-120f},out _,out _);
        var positionDrift=Math.Sqrt((camera.Position.Value-beforePosition).LengthSquared);
        maximumPositionDrift=Math.Max(maximumPositionDrift,positionDrift);
        var currentBody=scene.FocusedBody;
        var bodyFixedOrientation=(currentBody.BodyFixedToRoot.Conjugate().Normalized()*camera.Orientation).Normalized();
        Check(SurfaceCameraState.TryExtractLocalLook(bodyFixedOrientation,enu,out _,out var pitch),
            $"upward free-look sample {step} resolves in local ENU");
        if(pitch>0d)
        {
            if(crossedHorizon&&pitch>previousPitch+.05d)continuedAboveHorizon=true;
            crossedHorizon=true;
        }
        previousPitch=pitch;
        var clearance=SurfaceAnchorAcquisition.SurfaceAltitude(currentBody,camera.Position.Value,PlanetaryTerrainDefinition.EarthProductionCubeV5);
        minimumClearance=Math.Min(minimumClearance,clearance);
        Check(positionDrift==0d&&scene.CurrentFocusTarget.SurfaceAnchor==anchor&&
            scene.CurrentFocusTarget.SurfaceAnchor.LocalTangentBasis==tangent&&
            currentBody.Position==body.Position&&currentBody.BodyFixedToRoot==body.BodyFixedToRoot&&
            scene.Presentation.Bodies.SequenceEqual(truth)&&clearance>=SurfaceFocusHandoffPolicy.MinimumTerrainClearanceMetres,
            $"upward free-look sample {step} changes orientation only and remains terrain-safe");
    }

    Check(crossedHorizon&&continuedAboveHorizon&&previousPitch>.5d&&
        camera.Position.Value==initialPosition&&scene.CurrentInertialCameraOffset==initialOffset&&
        scene.CurrentCameraReferenceAuthority==CameraReferenceAuthority.Inertial,
        $"repeated upward input crosses and continues above the geometric horizon: initial={initialPitch:R}; final={previousPitch:R}");
    Console.WriteLine($"Near-surface inertial free-look: pitch={initialPitch:R}->{previousPitch:R} rad; horizonCrossed={crossedHorizon}; aboveHorizonContinued={continuedAboveHorizon}; positionDrift={maximumPositionDrift:E9} m; minimumClearance={minimumClearance:R} m; anchorStable={scene.CurrentFocusTarget.SurfaceAnchor==anchor}");
}

static void ProductionCubeSphereGpuResidencyIntegrationTest()
{
    var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var shaderRoot=Path.Combine(root,"native","NovaCore.Native","shaders");
    var selector=File.ReadAllText(Path.Combine(shaderRoot,"planetary_select.comp"));
    var terrain=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production_terrain.comp"));
    var fragment=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production.frag"));
    var projection=File.ReadAllText(Path.Combine(shaderRoot,"production_cube_surface.glsl"));
    var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","NovaCoreNative.cpp"));
    var parser=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","ProductionCubeSurface.cpp"));
    var scene=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle","EarthPlanetaryScene.cs"));
    var solar=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle","SolarSystemScene.cs"));
    var program=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle","Program.cs"));

    Check((uint)PlanetarySurfaceRendererMode.ProductionCubeSphere==2u&&(uint)NativePlanetarySurfaceMode.ProductionCubeSphere==2u,"managed production surface mode is the explicit ABI value 2");
    Check(scene.Contains("EarthProductionCubeV5.Version",StringComparison.Ordinal)&&scene.Contains("ProductionSurfaceRequested",StringComparison.Ordinal),"production scene submission explicitly selects terrain version 5");
    Check(program.Contains("\"production\"=>PlanetarySurfaceRendererMode.ProductionCubeSphere",StringComparison.Ordinal)&&program.Contains("NativePlanetarySurfaceMode.ProductionCubeSphere",StringComparison.Ordinal),"dedicated production proof selection cannot silently fall through to mode 1");
    Check(projection.Contains("ProductionSpherifyD",StringComparison.Ordinal)&&terrain.Contains("void main()",StringComparison.Ordinal),"GPU geometry uses the accepted relaxed cube-sphere projection while production elevation arrives through native payload residency");
    Check(selector.Contains("ProductionQuartetResident",StringComparison.Ordinal)&&selector.Contains("ProductionRootsResident",StringComparison.Ordinal)&&selector.Contains("!productionRootsReady",StringComparison.Ordinal)&&selector.Contains("face<6u",StringComparison.Ordinal)&&selector.Contains("pendingChildren",StringComparison.Ordinal)&&selector.Contains("payloadCount",StringComparison.Ordinal),"GPU residency bootstraps one complete six-face root transaction and retains parents while complete child quartets prepare invisibly");
    Check(selector.Contains("production?uvec4(presentation.identity.xy,inputData.controls.zz)",StringComparison.Ordinal),"production cache identity uses stable body ID and terrain version rather than material/albedo identity");
    Check(selector.Contains("PreviousContains(Parent(keyB),false)",StringComparison.Ordinal)&&selector.Contains("patchData.patches[index].transitions.y",StringComparison.Ordinal),"a complete promoted quartet receives one persistent GPU morph epoch");
    Check(selector.Contains("TransitionAge",StringComparison.Ordinal)&&selector.Contains("FinerOutputNeighbor",StringComparison.Ordinal)&&selector.Contains("transitionsSettled",StringComparison.Ordinal)&&selector.Contains("transitions.z=packedAges",StringComparison.Ordinal)&&selector.Contains("transitions.w=finerNeighborMask",StringComparison.Ordinal),"production selector coordinates same-level edge age, coarse/fine reverse ownership, and in-progress fast-path invalidation without expanding the patch ABI");
    Check(native.Contains("ProductionSampleElevation",StringComparison.Ordinal)&&native.Contains("terrain[sample]=ProductionSampleElevation",StringComparison.Ordinal)&&native.Contains("terrain[sample+1]=ProductionSampleElevation",StringComparison.Ordinal),"native payload preparation publishes parent and child elevation endpoints atomically");
    var vertex=File.ReadAllText(Path.Combine(shaderRoot,"planetary.vert"));
    Check(vertex.Contains("float temporalMorph=p.transitions.y==0u?1.0:clamp",StringComparison.Ordinal)&&vertex.Contains("surfaceMorph=temporalMorph",StringComparison.Ordinal)&&!vertex.Contains("DemandMorph",StringComparison.Ordinal)&&vertex.Contains("ConstrainedMorph(mask,p.transitions.w,p.transitions.z",StringComparison.Ordinal)&&vertex.Contains("/30.0",StringComparison.Ordinal),"production child geometry uses retained-parent/current endpoints with edge-compatible temporal morphing rather than spatially divergent per-patch demand morphing");
    Check(native.Contains("if(!productionSurface&&a.submission->planetaryPresentation.enabled&&a.submission->planetaryPresentation.regime==NC_PLANETARY_DISTANT_ONLY)",StringComparison.Ordinal),"production Earth roots remain available in the distant presentation regime");
    Check(native.Contains("production?a.productionPlanetaryTerrainPipeline:a.planetaryTerrainPipeline",StringComparison.Ordinal)&&native.Contains("a.productionPlanetaryFillPipeline:a.productionPlanetaryPipeline",StringComparison.Ordinal),"mode 2 selects dedicated production compute and exact-raster global-fill graphics pipelines");
    Check(native.Contains("sizeof(GpuPlanetaryControl) == 204",StringComparison.Ordinal)&&native.Contains("offsetof(GpuPlanetaryControl, productionDemandSignature) == 124",StringComparison.Ordinal),"production demand signature ABI is fixed and bounded");
    Check(selector.Contains("outputData.values[25]==0u",StringComparison.Ordinal)&&selector.Contains("index<20u",StringComparison.Ordinal)&&selector.Contains("presentation.identity[index-15u]",StringComparison.Ordinal),"steady-state selector reuse requires complete residency and includes stable body identity");
    Check(parser.Contains("production cube payload digest mismatch",StringComparison.Ordinal)&&parser.Contains("production cube hierarchy is incomplete",StringComparison.Ordinal)&&parser.Contains("record.ordinal >= records_.size()",StringComparison.Ordinal),"native pack parser rejects corrupt, incomplete, duplicate, and out-of-range patch transactions");
    Check(native.Contains("ProductionUploadBudget=2",StringComparison.Ordinal)&&native.Contains("ProductionMaximumPendingUploads=2",StringComparison.Ordinal)&&native.Contains("ProductionIoWorker",StringComparison.Ordinal)&&native.Contains("std::any_of(state->ready.begin()",StringComparison.Ordinal),"production reads and uploads use bounded asynchronous backpressure and upload budgets");
    Check(native.Contains("binding.binding=24+index",StringComparison.Ordinal)&&
        fragment.Contains("binding=24",StringComparison.Ordinal)&&fragment.Contains("binding=26",StringComparison.Ordinal),
        "production material owns dedicated patch-aligned albedo, elevation, and classification descriptors");
    Check(fragment.Contains("textureGrad(productionAlbedo",StringComparison.Ordinal)&&fragment.Contains("textureGrad(productionLand",StringComparison.Ordinal),"production fragment shading consumes resident NCCUBE payloads through seam-safe explicit gradients rather than procedural placeholders");
    Check(solar.Contains("PlanetaryProductionSurfaceEligibility",StringComparison.Ordinal)&&solar.Contains("ProductionSurfaceEligible",StringComparison.Ordinal)&&solar.Contains("EarthProductionCubeV5",StringComparison.Ordinal)&&program.Contains("sol?.ProductionSurfaceEligible==true",StringComparison.Ordinal),"normal Solar Earth focus requires the explicit terrain-v5 production eligibility contract");
    Check(!solar.Contains("PlanetaryEnvironment",StringComparison.Ordinal)&&!native.Contains("planetaryEnvironment",StringComparison.Ordinal)&&!File.Exists(Path.Combine(shaderRoot,"planetary_environment.frag")),"provisional environment presentation has no managed, native, or shader owner");

    Check(File.Exists(TerrainAssetRepository.ManifestPath(root,TerrainAssetCache.ProductionEarthAssetId))&&File.Exists(Path.Combine(root,"assets","earth","runtime","earth_elevation_8192x4096.r16")),"tracked terrain-v5 identity manifest and topology-neutral elevation oracle are retained while heavy runtime bytes resolve externally");
    Check(fragment.Contains("bool anchored=(productionLayer&0x40000000u)!=0u",StringComparison.Ordinal)&&
          !fragment.Contains("if(!anchored&&ProductionAnchoredOwnsDirection(unitDirection))discard",StringComparison.Ordinal)&&
          native.Contains("VK_FORMAT_D32_SFLOAT_S8_UINT",StringComparison.Ordinal)&&
          native.Contains("fillDepth.front.compareOp=VK_COMPARE_OP_EQUAL",StringComparison.Ordinal)&&
          native.Contains("anchoredDepth.front.passOp=VK_STENCIL_OP_REPLACE",StringComparison.Ordinal)&&
          native.IndexOf("vkCmdDrawIndexedIndirect(c,a.anchoredSurfaceIndirectBuffer",StringComparison.Ordinal)<native.IndexOf("a.productionPlanetaryFillPipeline:a.productionPlanetaryPipeline",StringComparison.Ordinal)&&
          !native.Contains("dynamicNeedsGlobal",StringComparison.Ordinal),
          "terrain-v5 remains the complete global fill while actual anchored raster samples transfer pixel ownership without analytic-boundary holes or visible overlap");
    Check(native.Contains("ValidateAnchoredStitchTemplates",StringComparison.Ordinal)&&
          native.Contains("doubleArea!=expectedDoubleArea",StringComparison.Ordinal)&&
          native.Contains("draws[index]={AnchoredSurfaceBaseIndicesPerPatch,1u,firstIndex,0,index}",StringComparison.Ordinal)&&
          native.Contains("command.vertexOffset!=0",StringComparison.Ordinal),
          "all sixteen stitch templates prove bounded winding and exact patch area while every indirect command carries validated index, vertex, and patch-instance correspondence");

    var binaryRoot=Path.Combine(root,"build","native-ninja","shaders");
    foreach(var shader in new[]{"planetary_production_terrain.comp.spv","anchored_terrain.vert.spv","planetary_production.frag.spv"})
    {
        var path=Path.Combine(binaryRoot,shader);Check(File.Exists(path),$"compiled production SPIR-V exists: {shader}");
        if(shader.EndsWith("comp.spv",StringComparison.Ordinal))
        {
            Check(new FileInfo(path).Length>=20&&!terrain.Contains("binding=",StringComparison.Ordinal),$"compiled production upload stage has no descriptor dependency: {shader}");
        }
    }
    Console.WriteLine("Production GPU residency: mode=2; terrainVersion=5; realPayload=true; maxPackLevel=2; parent-retained quartet preparation=true");
}

static void ProductionPhysicalNormalTangentContinuityTest()
{
    const double longitude=-42d*Math.PI/180d;
    var boundaryLatitude=Math.Asin(.95d);const double epsilon=1e-8d;
    var south=BodyFixedGeography.DirectionFromLatitudeLongitude(boundaryLatitude-epsilon,longitude);
    var north=BodyFixedGeography.DirectionFromLatitudeLongitude(boundaryLatitude+epsilon,longitude);

    static Double3 DiscontinuousReferenceEast(in Double3 radial)
    {
        var reference=Math.Abs(radial.Y)<.95d?Double3.UnitY:Double3.UnitX;
        return Double3.Cross(reference,radial).Normalized();
    }
    static Double3 CanonicalEast(in Double3 radial)
    {
        var horizontalSquared=radial.X*radial.X+radial.Z*radial.Z;
        return horizontalSquared>1e-24d
            ?new Double3(radial.Z,0d,-radial.X)/Math.Sqrt(horizontalSquared)
            :Double3.UnitX;
    }
    static Double3 CanonicalNorth(in Double3 radial)=>Double3.Cross(radial,CanonicalEast(radial)).Normalized();
    static double Angle(in Double3 a,in Double3 b)=>Math.Acos(Math.Clamp(Double3.Dot(a,b),-1d,1d));
    static string Address(in Double3 direction)
    {
        Check(RelaxedCubeSphereProjection.TryAddress(direction,out var face,out var u,out var v),"polar normal sample has a canonical relaxed-cube address");
        const int side=4;var x=Math.Min(side-1,(int)Math.Floor(u*side));var y=Math.Min(side-1,(int)Math.Floor(v*side));
        return $"{face}/L2/{x}/{y}@{u:R},{v:R}";
    }

    var discontinuousJump=Angle(DiscontinuousReferenceEast(south),DiscontinuousReferenceEast(north));
    var canonicalJump=Angle(CanonicalEast(south),CanonicalEast(north));
    var southNorth=Double3.Cross(south,CanonicalEast(south)).Normalized();
    var northNorth=Double3.Cross(north,CanonicalEast(north)).Normalized();
    var canonicalNorthJump=Angle(southNorth,northNorth);
    Check(discontinuousJump>.5d,"an abs(Y)=0.95 reference-axis switch creates a material finite-difference frame rotation");
    Check(canonicalJump<1e-7d&&canonicalNorthJump<1e-7d,"canonical longitude/latitude tangent frame remains continuous across the former latitude ring");

    var wrapMaximum=0d;foreach(var latitudeDegrees in new[]{-89d,-80d,-45d,0d,45d,80d,89d})
    {
        var latitude=latitudeDegrees*Math.PI/180d;
        var west=BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,-Math.PI+epsilon);
        var east=BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,Math.PI-epsilon);
        wrapMaximum=Math.Max(wrapMaximum,Math.Max(Angle(CanonicalEast(west),CanonicalEast(east)),Angle(CanonicalNorth(west),CanonicalNorth(east))));
    }
    var polarMaximum=0d;var minimumHandedness=1d;var previousPolar=BodyFixedGeography.DirectionFromLatitudeLongitude(-Math.PI*.5d+1e-3d,longitude);
    for(var step=1;step<=1000;step++)
    {
        var latitude=-Math.PI*.5d+1e-3d*(1d-step/1001d);
        var radial=BodyFixedGeography.DirectionFromLatitudeLongitude(latitude,longitude);var east=CanonicalEast(radial);var tangentNorth=CanonicalNorth(radial);
        polarMaximum=Math.Max(polarMaximum,Math.Max(Angle(CanonicalEast(previousPolar),east),Angle(CanonicalNorth(previousPolar),tangentNorth)));
        minimumHandedness=Math.Min(minimumHandedness,Double3.Dot(Double3.Cross(east,tangentNorth),radial));previousPolar=radial;
    }
    var exactSouthPole=-Double3.UnitY;var poleEast=CanonicalEast(exactSouthPole);var poleNorth=CanonicalNorth(exactSouthPole);
    minimumHandedness=Math.Min(minimumHandedness,Double3.Dot(Double3.Cross(poleEast,poleNorth),exactSouthPole));
    Check(wrapMaximum<1e-7d,"canonical tangent frame is continuous across longitude plus/minus pi");
    Check(polarMaximum<2e-6d&&minimumHandedness>1d-1e-12d&&poleEast.IsFinite&&poleNorth.IsFinite,"South-pole neighborhood stays finite, right-handed, and continuous until the mathematical coordinate singularity");

    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var shaderRoot=Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders");
    var physicalShader=File.ReadAllText(Path.Combine(shaderRoot,"physical_surface.glsl"));
    var authorityShader=File.ReadAllText(Path.Combine(shaderRoot,"planetary_physical_authority.glsl"));
    var fragmentShader=File.ReadAllText(Path.Combine(shaderRoot,"planetary_production.frag"));
    Check(physicalShader.Contains("horizontalSquared=direction.x*direction.x+direction.z*direction.z",StringComparison.Ordinal)&&
          physicalShader.Contains("dvec3(direction.z,0.0,-direction.x)/sqrt(horizontalSquared)",StringComparison.Ordinal)&&
          authorityShader.Contains("PhysicalEastD(direction)",StringComparison.Ordinal)&&
          !physicalShader.Contains("abs(direction.y)<.95",StringComparison.Ordinal)&&
          !fragmentShader.Contains("ProductionFixedPhysicalNormal",StringComparison.Ordinal),
        "the one prepared production normal authority uses the continuous canonical body-fixed tangent frame with no fragment-stage or mid-latitude alternate");
    Console.WriteLine($"Physical-normal frame: boundaryLatitude={boundaryLatitude*180d/Math.PI:R}deg; longitude=-42deg; discontinuousReferenceJump={discontinuousJump:R}rad; canonicalEastJump={canonicalJump:R}rad; canonicalNorthJump={canonicalNorthJump:R}rad; longitudeWrap={wrapMaximum:R}rad; polarStep={polarMaximum:R}rad; minimumHandedness={minimumHandedness:R}; south={Address(south)}; north={Address(north)}");
}

static void ProductionSurfaceBodyEligibilityAndTransitionOwnershipTest()
{
    var root=new ReferenceFrameId(995);
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var value,out var error)&&value is not null,$"production eligibility scene: {error}");
    var scene=value!;
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
    scene.ResetPresentationCamera(camera);scene.Update(camera);

    var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    var earthPolicy=new PlanetaryProductionSurfaceEligibility(SolarSystemBodyIds.Earth.Value,6_371_008.8d,terrain.SourceId,terrain.Version,"earth_surface_v5.nccube");
    Check(earthPolicy.IsValid&&earthPolicy.Supports(SolarSystemBodyIds.Earth.Value,6_371_008.8d,terrain),"Earth terrain-v5 identity, radius, and checked dataset form one explicit production eligibility contract");
    Check(!earthPolicy.Supports(SolarSystemBodyIds.Mars.Value,6_371_008.8d,terrain)&&!earthPolicy.Supports(SolarSystemBodyIds.Earth.Value,6_371_009d,terrain)&&!earthPolicy.Supports(SolarSystemBodyIds.Earth.Value,6_371_008.8d,new PlanetaryTerrainDefinition(terrain.SourceId,3,terrain.MaximumHeightMetres)),"body, radius, and terrain-version mismatches reject production eligibility");

    var unsupported=new[]{NativePresentationFocus.Moon,NativePresentationFocus.Venus,NativePresentationFocus.Mars,NativePresentationFocus.Jupiter,NativePresentationFocus.Saturn,NativePresentationFocus.Uranus,NativePresentationFocus.Neptune,NativePresentationFocus.Mercury,NativePresentationFocus.Sun};
    foreach(var focus in unsupported)
    {
        Check(scene.Focus(camera,focus),$"focus unsupported production body {focus}");
        var body=scene.FocusedBody;
        foreach(var radii in new[]{32d,18d,4d,1.01d,1.000001d})
        {
            camera.Position=camera.Position with{Value=body.Position.Value+body.BodyFixedToRoot.Rotate(Double3.UnitZ*(body.RadiusMetres*radii))};
            camera.Orientation=body.BodyFixedToRoot;scene.Update(camera);
            var gpu=scene.GpuConstants(camera);var presentation=scene.FocusedPresentation(camera);
            Check(!scene.ProductionSurfaceEligible&&!scene.DetailedComputeRequested&&scene.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly,$"{focus} at {radii:R} radii remains on the bounded non-production sphere");
            Check(gpu.TerrainVersion==0&&gpu.MaximumLevel==0&&gpu.OutputCapacity==6&&gpu.MaximumTerrainHeightMetres==0,$"{focus} at {radii:R} radii cannot traverse or allocate Earth production terrain");
            Check(presentation.Regime==NativePlanetaryRenderRegime.DistantOnly&&presentation.DistantAlpha==1f&&presentation.DetailedAlpha==0f&&scene.DistantBodies[0].DistantAlpha==1f,$"{focus} at {radii:R} radii always retains one opaque visible owner");
        }
    }

    for(var iteration=0;iteration<20;iteration++)
    {
        scene.ResetPresentationCamera(camera);scene.Update(camera);
        Check(!scene.ProductionSurfaceEligible&&scene.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly&&scene.DistantBodies[0].DistantAlpha==1f,$"Solar owner iteration {iteration}");
        Check(scene.Focus(camera,NativePresentationFocus.Earth),$"Earth focus iteration {iteration}");scene.Update(camera);
        var production=scene.FocusedPresentation(camera);var gpu=scene.GpuConstants(camera);
        Check(scene.ProductionSurfaceEligible&&scene.DetailedComputeRequested&&gpu.TerrainVersion==5&&production.Regime==NativePlanetaryRenderRegime.DetailedOnly&&production.DistantAlpha==0f&&production.DetailedAlpha==1f,$"Earth iteration {iteration} selects production mode 2 only");
        Check(scene.DistantBodies[0].DistantAlpha==0f,"Earth never publishes a generic root-bootstrap sphere after synchronous terrain-v5 startup");
    }

    var repositoryRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
    var native=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","NovaCoreNative.cpp"));
    var selector=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","planetary_select.comp"));
    Check(native.Contains("surfaceTransitionEpoch",StringComparison.Ordinal)&&native.Contains("surfaceContextBodyId!=contextBody",StringComparison.Ordinal)&&native.Contains("owner=%s",StringComparison.Ordinal),"body/mode/dataset transitions invalidate stale selection and identify the current production owner");
    Check(!native.Contains("productionFallbackOwner",StringComparison.Ordinal)&&
          !native.Contains("productionFallback",StringComparison.Ordinal)&&
          native.Contains("ProductionBillboardPresentationEnabled(a.productionBillboardAuthoritative,a.submission->productionBillboardFlags)",StringComparison.Ordinal)&&
          native.Contains("const bool candidateRequested=(a.submission->productionBillboardFlags&1u)!=0u",StringComparison.Ordinal)&&
          native.Contains("distantPresentation=!candidate&&handoff&&!production",StringComparison.Ordinal)&&
          native.Contains("if(!candidate&&diagnosticGlobal&&regional",StringComparison.Ordinal),
        "resident production-billboard resources become the Earth presentation owner only through the explicit per-frame presentation authority bit");
    var sample=File.ReadAllText(Path.Combine(repositoryRoot,"samples","NovaCore.Triangle","Program.cs"));
    Check(sample.Contains("ProductionBillboardPresentationEligible(state)",StringComparison.Ordinal)&&
          sample.Contains("submission->ProductionBillboard=null",StringComparison.Ordinal)&&
          sample.Contains("submission->ProductionBillboardFlags=0",StringComparison.Ordinal)&&
          sample.Contains("submission->ProductionBillboardFrame=null",StringComparison.Ordinal)&&
          native.Contains("static_assert(!ProductionBillboardPresentationEnabled(true,0u))",StringComparison.Ordinal),
        "switching away from Earth disables candidate submission without evicting its reusable topology or physical resources");
    var productionProjection=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","production_cube_surface.glsl"));
    var productionVertex=File.ReadAllText(Path.Combine(repositoryRoot,"native","NovaCore.Native","shaders","planetary.vert"));
    Check(native.Contains("BootstrapProductionHierarchy(a)",StringComparison.Ordinal)&&native.Contains("ProductionHierarchyPayloadsReady(a)",StringComparison.Ordinal)&&native.Contains("complete L0-L2 hierarchy synchronously resident before first submitted presentation frame",StringComparison.Ordinal)&&productionProjection.Contains("NOVACORE_PRODUCTION_TERRAIN_VERSION=5u",StringComparison.Ordinal)&&selector.Contains("bool production=terrain&&inputData.controls.z==NOVACORE_PRODUCTION_TERRAIN_VERSION",StringComparison.Ordinal)&&productionVertex.Contains("inputData.controls.z==NOVACORE_PRODUCTION_TERRAIN_VERSION",StringComparison.Ordinal)&&!selector.Contains("inputData.controls.z==4u",StringComparison.Ordinal)&&!productionVertex.Contains("inputData.controls.z==4u",StringComparison.Ordinal),"the complete immutable Earth L0-L2 hierarchy preloads before presentation and terrain-v5 consistently selects the production projection, payload keys, and transactional ownership path");
    Check(native.Contains("distantPresentation||detailedPresentation||gpuPlanetary",StringComparison.Ordinal),"production DistantOnly selection receives an explicit HOST_WRITE to COMPUTE_SHADER visibility barrier before consuming immutable terrain-v5 cache publication");
    Check(selector.Contains("presentation.identity[index-15u]",StringComparison.Ordinal)&&selector.Contains("cameraHighRadiusHigh.w",StringComparison.Ordinal)&&selector.Contains("cameraLowRadiusLow.w",StringComparison.Ordinal),"GPU demand identity includes selected body and exact transported radius configuration");
}

static void ParentChildLodGeographicCorrespondenceTest()
{
    const double radius=6_378_137d;const int grid=PlanetaryTerrainDefinition.GridResolution;
    var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;var topologyHash=PlanetaryPatchTopology.Shared.DeterministicHash;
    var representatives=new[]{new PlanetaryPatch(CubeSphereFace.PositiveX,0,0,0),new PlanetaryPatch(CubeSphereFace.PositiveZ,2,1,2),new PlanetaryPatch(CubeSphereFace.PositiveY,4,7,7),new PlanetaryPatch(CubeSphereFace.NegativeY,5,15,0),new PlanetaryPatch(CubeSphereFace.NegativeZ,6,0,63)};
    var rotations=new[]{DoubleQuaternion.Identity,new DoubleQuaternion(.17,-.31,.11,.9273618495495703).Normalized()};
    var cameras=new[]{Double3.Zero,new Double3(1.2e11,-3.4e10,8.7e10)};var maximumDrift=0d;var maximumElevationMismatch=0d;var maximumEdgeError=0d;var maximumRoundTrip=0d;
    foreach(var parent in representatives)
    {
        var parentSamples=new (Double3 Direction,double Height)[grid+1,grid+1];
        for(var y=0;y<=grid;y++)for(var x=0;x<=grid;x++){var (u,v)=parent.GridCoordinate(x,y);var direction=CubeSphereProjection.Project(parent.Face,u,v,1d);parentSamples[x,y]=(direction,terrain.SampleHeight(direction,24));}
        for(var childIndex=0;childIndex<4;childIndex++)
        {
            var child=parent.Child(childIndex);var bounds=child.Bounds;var expectedMin=parent.GridCoordinate((childIndex&1)*grid/2,(childIndex>>1)*grid/2);var expectedMax=parent.GridCoordinate(((childIndex&1)+1)*grid/2,((childIndex>>1)+1)*grid/2);
            Check(bounds==(expectedMin.U,expectedMin.V,expectedMax.U,expectedMax.V),"child exactly partitions the parent's dyadic geographic footprint");
            for(var parentY=0;parentY<=grid;parentY++)for(var parentX=0;parentX<=grid;parentX++)if(PlanetaryPatch.TryMapGridVertexToChild(childIndex,parentX,parentY,out var childX,out var childY))
            {
                var parentUv=parent.GridCoordinate(parentX,parentY);var childUv=child.GridCoordinate(childX,childY);Check(BitConverter.DoubleToInt64Bits(parentUv.U)==BitConverter.DoubleToInt64Bits(childUv.U)&&BitConverter.DoubleToInt64Bits(parentUv.V)==BitConverter.DoubleToInt64Bits(childUv.V),"shared parent/child grid coordinates are bit-identical");
                var childDirection=CubeSphereProjection.Project(child.Face,childUv.U,childUv.V,1d);var bodyDrift=Math.Sqrt((parentSamples[parentX,parentY].Direction-childDirection).LengthSquared)*radius;maximumDrift=Math.Max(maximumDrift,bodyDrift);
                var childHeight=terrain.SampleHeight(childDirection,24);maximumElevationMismatch=Math.Max(maximumElevationMismatch,Math.Abs(parentSamples[parentX,parentY].Height-childHeight));
                foreach(var rotation in rotations)foreach(var camera in cameras){var parentRoot=rotation.Rotate(parentSamples[parentX,parentY].Direction*(radius+parentSamples[parentX,parentY].Height))-camera;var childRoot=rotation.Rotate(childDirection*(radius+childHeight))-camera;maximumRoundTrip=Math.Max(maximumRoundTrip,Math.Sqrt((parentRoot-childRoot).LengthSquared));}
            }
        }
        var cache=new PlanetaryTerrainResidencyCache(5);var parentTile=cache.Acquire(new(SolarSystemBodyIds.Earth.Value,parent.Face,parent.Level,parent.X,parent.Y,terrain.Version,terrain.SourceId),terrain);for(var childIndex=0;childIndex<4;childIndex++){var child=parent.Child(childIndex);var childTile=cache.Acquire(new(SolarSystemBodyIds.Earth.Value,child.Face,child.Level,child.X,child.Y,terrain.Version,terrain.SourceId),terrain);for(var py=0;py<=grid;py++)for(var px=0;px<=grid;px++)if(PlanetaryPatch.TryMapGridVertexToChild(childIndex,px,py,out var cx,out var cy))maximumElevationMismatch=Math.Max(maximumElevationMismatch,Math.Abs(parentTile.Heights[py*(grid+1)+px]-childTile.Heights[cy*(grid+1)+cx]));}
    }
    foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())foreach(PlanetaryPatchEdge edge in Enum.GetValues<PlanetaryPatchEdge>().Where(value=>value!=PlanetaryPatchEdge.None))
    {
        var patch=new PlanetaryPatch(face,3,edge==PlanetaryPatchEdge.PositiveU?7:0,edge==PlanetaryPatchEdge.PositiveV?7:0);var neighbor=CubeSphereAdjacency.NeighborAtSameLevel(patch,edge);var transition=CubeSphereAdjacency.GetTransition(face,edge);
        for(var step=0;step<=grid;step++){var source=EdgeCoordinate(patch,edge,step);var targetStep=transition.Reversed?grid-step:step;var target=EdgeCoordinate(neighbor,transition.NeighborEdge,targetStep);var a=CubeSphereProjection.Project(face,source.U,source.V,radius);var b=CubeSphereProjection.Project(neighbor.Face,target.U,target.V,radius);maximumEdgeError=Math.Max(maximumEdgeError,Math.Sqrt((a-b).LengthSquared));}
    }
    for(var cycle=0;cycle<64;cycle++)foreach(var parent in representatives){var merged=Enumerable.Range(0,4).Select(parent.Child).Select(child=>child.Parent!.Value).Distinct().Single();Check(merged==parent,"repeated deterministic split/merge restores the exact parent identity");}
    var shaderDirectory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","shaders"));var productionProjection=File.ReadAllText(Path.Combine(shaderDirectory,"production_cube_surface.glsl"));var vertex=File.ReadAllText(Path.Combine(shaderDirectory,"planetary.vert"));var productionUpload=File.ReadAllText(Path.Combine(shaderDirectory,"planetary_production_terrain.comp"));
    Check(productionProjection.Contains("ProductionProjectGridD",StringComparison.Ordinal)&&vertex.Contains("ProductionProjectGridD(address,grid)",StringComparison.Ordinal),"production GPU geometry derives shared vertices from one exact patch-aligned relaxed-cube lattice");
    Check(vertex.Contains("productionLayer=patchTerrain.values[gl_InstanceIndex].y",StringComparison.Ordinal)&&!productionUpload.Contains("textureDemand",StringComparison.Ordinal),"production elevation authority follows the resident spherical patch transaction rather than an independent texture-demand hierarchy");
    Check(maximumDrift==0d&&maximumElevationMismatch==0d&&maximumEdgeError<1e-8d&&maximumRoundTrip==0d&&topologyHash==PlanetaryPatchTopology.Shared.DeterministicHash,"parent/child refinement adds samples without moving the represented geographic surface");
    Console.WriteLine($"Parent/child LOD correspondence: sharedDrift={maximumDrift:E3} m; elevationMismatch={maximumElevationMismatch:E3} m; edgeError={maximumEdgeError:E3} m; splitMerge={maximumRoundTrip:E3} m; patchHash=0x{topologyHash:X16}");

    static (double U,double V) EdgeCoordinate(in PlanetaryPatch patch,PlanetaryPatchEdge edge,int step)
    {
        return edge switch{PlanetaryPatchEdge.NegativeU=>patch.GridCoordinate(0,step),PlanetaryPatchEdge.PositiveU=>patch.GridCoordinate(grid,step),PlanetaryPatchEdge.NegativeV=>patch.GridCoordinate(step,0),PlanetaryPatchEdge.PositiveV=>patch.GridCoordinate(step,grid),_=>throw new ArgumentOutOfRangeException(nameof(edge))};
    }
}

static void PlanetaryTerrainResidencyAndSurfaceFrameTest()
{
    var terrain=new PlanetaryTerrainDefinition(1,1,7_600d);Check(terrain.IsValid&&terrain.MaximumHeightMetres==7_600d&&PlanetaryTerrainDefinition.GridVertexCount==289,"generic versioned bounded terrain definition");
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
    static double SignedWinding(in Double3 a,in Double3 b,in Double3 c)
    {
        var direction=((a+b+c)/3d).Normalized();
        return Double3.Dot(Double3.Cross(b-a,c-a),direction);
    }
    static double ProjectedSignedArea(in Double3 a,in Double3 b,in Double3 c)
    {
        var direction=((a+b+c)/3d).Normalized();var camera=direction*3d;var forward=-direction;
        var reference=Math.Abs(Double3.Dot(direction,Double3.UnitY))<.9d?Double3.UnitY:Double3.UnitZ;
        var right=Double3.Cross(forward,reference).Normalized();var up=Double3.Cross(right,forward).Normalized();
        (double X,double Y) Project(in Double3 point)
        {
            var relative=point-camera;var depth=Double3.Dot(relative,forward);
            Check(depth>0d,"representative production triangle remains in front of its diagnostic camera");
            // CameraRenderSnapshotBuilder negates projection Y for Vulkan's positive-height viewport.
            return (Double3.Dot(relative,right)/depth,-Double3.Dot(relative,up)/depth);
        }
        var pa=Project(a);var pb=Project(b);var pc=Project(c);
        return (pb.X-pa.X)*(pc.Y-pa.Y)-(pb.Y-pa.Y)*(pc.X-pa.X);
    }
    static Double3 SphereVertex(int latitude,int longitude)
    {
        const int latitudeSegments=12,longitudeSegments=24;
        if(latitude==0)return Double3.UnitY;
        var phi=Math.PI*latitude/latitudeSegments;var theta=Math.Tau*longitude/longitudeSegments;
        return new(Math.Sin(phi)*Math.Cos(theta),Math.Cos(phi),Math.Sin(phi)*Math.Sin(theta));
    }

    var distantWinding=SignedWinding(SphereVertex(0,0),SphereVertex(1,1),SphereVertex(1,0));
    var topology=PlanetaryProductionPatchTopology.Shared;var indices=topology.Indices.ToArray();
    var levelMinimum=new double[3];var levelMaximum=new double[3];var levelProjectedMinimum=new double[3];var levelProjectedMaximum=new double[3];
    Array.Fill(levelMinimum,double.PositiveInfinity);Array.Fill(levelProjectedMinimum,double.PositiveInfinity);
    Array.Fill(levelMaximum,double.NegativeInfinity);Array.Fill(levelProjectedMaximum,double.NegativeInfinity);
    var outward=0;var inward=0;var degenerate=0;var triangleCount=0;var firstMismatch=string.Empty;
    for(var level=0;level<=2;level++)
    {
        var side=1<<level;
        foreach(CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            for(var patchY=0;patchY<side;patchY++)for(var patchX=0;patchX<side;patchX++)
            {
                var patch=new PlanetarySurfacePatchId(6,PlanetaryTerrainDefinition.EarthProductionCubeV5.Version,face,level,patchX,patchY);
                for(var triangle=0;triangle<indices.Length;triangle+=3)
                {
                    Double3 Vertex(uint index)
                    {
                        var gridX=(int)(index%(PlanetaryPatchTopology.QuadsPerSide+1));var gridY=(int)(index/(PlanetaryPatchTopology.QuadsPerSide+1));
                        return RelaxedCubeSphereProjection.PatchPoint(patch,gridX,gridY);
                    }
                    var p0=Vertex(indices[triangle]);var p1=Vertex(indices[triangle+1]);var p2=Vertex(indices[triangle+2]);
                    var signed=SignedWinding(p0,p1,p2);var projected=ProjectedSignedArea(p0,p1,p2);triangleCount++;
                    levelMinimum[level]=Math.Min(levelMinimum[level],signed);levelMaximum[level]=Math.Max(levelMaximum[level],signed);
                    levelProjectedMinimum[level]=Math.Min(levelProjectedMinimum[level],projected);levelProjectedMaximum[level]=Math.Max(levelProjectedMaximum[level],projected);
                    if(signed>0d)outward++;else if(signed<0d){inward++;if(firstMismatch.Length==0)firstMismatch=$"face={face}; level={level}; patch=({patchX},{patchY}); triangle={triangle/3}; signed={signed:R}; projected={projected:R}";}else degenerate++;
                }
            }
    }

    var nativePath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","NovaCoreNative.cpp"));
    var nativeSource=File.ReadAllText(nativePath);
    var productionCounterClockwise=nativeSource.Contains("planetaryRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE",StringComparison.Ordinal);
    var nativeOutwardGrid=nativeSource.Contains("pi[index++]=q;pi[index++]=q+1;pi[index++]=q+side;pi[index++]=q+1;pi[index++]=q+side+1;pi[index++]=q+side;",StringComparison.Ordinal);
    Console.WriteLine($"Production model winding: triangles={triangleCount}; outward={outward}; inward={inward}; degenerate={degenerate}; firstMismatch={firstMismatch}");
    for(var level=0;level<=2;level++)Console.WriteLine($"Production winding L{level}: model=[{levelMinimum[level]:R},{levelMaximum[level]:R}]; projectedFramebuffer=[{levelProjectedMinimum[level]:R},{levelProjectedMaximum[level]:R}]");
    Check(outward==triangleCount&&inward==0&&degenerate==0,"all production cube-face L0/L1/L2 triangles have literal outward body-fixed winding");
    Check(nativeOutwardGrid,"native uploaded production grid uses the same outward triangle order as the managed topology contract");
    // The signed-area values above are expressed in positive-height Vulkan
    // framebuffer coordinates. A negative shoelace area is the authored
    // counter-clockwise front face under Vulkan's winding convention; calling
    // it clockwise previously made the static test bless the exact live L1/L2
    // visibility failure this regression is intended to prevent.
    Check(levelProjectedMaximum.All(value=>value<0d)&&productionCounterClockwise,"outward model winding plus the actual Vulkan camera/viewport parity produces counter-clockwise front-facing production triangles");
    Check(distantWinding>0d,"native distant sphere retains outward-authored model winding");
    Check(nativeSource.Contains("distantRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE",StringComparison.Ordinal),"distant sphere retains its independently validated front-face convention");
    Check(!nativeSource.Contains("distantRaster.frontFace=VK_FRONT_FACE_CLOCKWISE",StringComparison.Ordinal),"distant sphere cannot regress to rendering its back hemisphere");
    Console.WriteLine($"Visible-hemisphere winding: productionModel=outward; productionProjected=counter-clockwise; distantModel={distantWinding:R}; productionRaster=VK_FRONT_FACE_COUNTER_CLOCKWISE; distantRaster=VK_FRONT_FACE_COUNTER_CLOCKWISE");
}

static void ContinuousEarthDistanceVisibilityTest()
{
    var root=new ReferenceFrameId(1);
    Check(EarthPlanetaryScene.TryCreate(root,out var scene,out var error)&&scene is not null,$"continuous Earth visibility fixture: {error}");
    var earth=scene!.Earth with{Position=new UniversePosition(Double3.Zero,root),BodyFixedToRoot=DoubleQuaternion.Identity};
    var configuration=PlanetaryLodConfiguration.ForViewport(64d,EarthPlanetaryScene.RegionalMaximumLod,
        EarthPlanetaryScene.TargetPatchPixels,EarthPlanetaryScene.ProofViewportHeightPixels,Math.PI/3d,
        EarthPlanetaryScene.Terrain.MaximumHeightMetres);
    PlanetaryPatch[] previous=[];
    var minimumPatches=int.MaxValue;var maximumPatches=0;var minimumVisible=int.MaxValue;var samples=0;

    void Sample(double altitudeMetres)
    {
        var cameraBody=Double3.UnitZ*(earth.RadiusMetres+altitudeMetres);
        var selection=PlanetaryRepresentationSelector.SelectPatches(earth,cameraBody,configuration,-Double3.UnitZ,
            Math.PI/3d,16d/9d,altitudeMetres,previous);
        Check(selection.Representation==PlanetaryRepresentation.NearFieldSurface&&selection.Patches.Length>0,
            $"production Earth retains a geometry owner at altitude {altitudeMetres:R}");
        var visible=0;
        foreach(var patch in selection.Patches)
        {
            var bounds=patch.Bounds;
            var center=CubeSphereProjection.Project(patch.Face,(bounds.MinX+bounds.MaxX)*.5d,(bounds.MinY+bounds.MaxY)*.5d,earth.RadiusMetres);
            if(Double3.Dot(center,(cameraBody-center))>0d)visible++;
        }
        Check(visible>0,$"production Earth contributes front-hemisphere coverage at altitude {altitudeMetres:R}");
        minimumPatches=Math.Min(minimumPatches,selection.Patches.Length);maximumPatches=Math.Max(maximumPatches,selection.Patches.Length);minimumVisible=Math.Min(minimumVisible,visible);samples++;
        previous=selection.Patches;
    }

    const int steps=512;const double farAltitude=100_000_000d,nearAltitude=10d;
    for(var step=0;step<=steps;step++)Sample(Math.Exp(Math.Log(farAltitude)+(Math.Log(nearAltitude)-Math.Log(farAltitude))*step/steps));
    for(var step=1;step<=steps;step++)Sample(Math.Exp(Math.Log(nearAltitude)+(Math.Log(farAltitude)-Math.Log(nearAltitude))*step/steps));
    var nativePath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","NovaCoreNative.cpp"));
    var nativeSource=File.ReadAllText(nativePath);
    Check(nativeSource.Contains("planetaryRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE",StringComparison.Ordinal)&&
          !nativeSource.Contains("planetaryRaster.frontFace=VK_FRONT_FACE_CLOCKWISE",StringComparison.Ordinal),
        "continuous production coverage uses the outward L0/L1/L2 raster convention");
    Console.WriteLine($"Continuous Earth visibility: samples={samples}; altitude=[{nearAltitude:R},{farAltitude:R}]m; patches=[{minimumPatches},{maximumPatches}]; minimumFrontCoverage={minimumVisible}");
}

static void CameraDragIsolationTest()
{
    static void CheckOrientation(in NativePlanetaryPresentation before,in NativePlanetaryPresentation after,string label)
    {
        Check(before.BodyOrientationX==after.BodyOrientationX&&before.BodyOrientationY==after.BodyOrientationY&&
              before.BodyOrientationZ==after.BodyOrientationZ&&before.BodyOrientationW==after.BodyOrientationW,
            $"{label} camera drag cannot enter the production Earth body transform");
    }

    var root=new ReferenceFrameId(1);
    Check(EarthPlanetaryScene.TryCreate(root,NativePlanetaryMode.GpuProduction,128,out var scene,out var error)&&scene is not null,$"camera-drag Earth fixture: {error}");
    var earthScene=scene!;var earth=earthScene.Earth;
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,earthScene.Projection,CameraMode.Free);
    Check(earthScene.TryFocus(camera),"camera-drag Earth focus");
    var authoritativeEarth=earthScene.Presentation.Bodies.ToArray();
    var landmarkBody=BodyFixedGeography.DirectionFromLatitudeLongitude(.4d,-1.2d)*(earth.RadiusMetres+123d);
    var landmarkRoot=earth.Position.Value+earth.BodyFixedToRoot.Rotate(landmarkBody);
    foreach(var (label,altitude) in new[]{("far",50_000_000d),("700km",700_000d),("near",10d)})
    {
        earthScene.SetValidationAltitude(camera,altitude,"land");
        var cameraPositionBefore=camera.Position.Value;var cameraOrientationBefore=camera.Orientation;
        var presentationBefore=earthScene.NativePresentation(camera);
        earthScene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=83f,MouseDeltaY=-37f});
        var presentationAfter=earthScene.NativePresentation(camera);
        Check(camera.Position.Value!=cameraPositionBefore||camera.Orientation!=cameraOrientationBefore,$"{label} drag changes camera pose");
        Check(authoritativeEarth.SequenceEqual(earthScene.Presentation.Bodies.ToArray()),$"{label} drag leaves celestial Earth immutable");
        CheckOrientation(presentationBefore,presentationAfter,label);
        Check(landmarkRoot==earth.Position.Value+earth.BodyFixedToRoot.Rotate(landmarkBody),$"{label} body-fixed landmark remains bit-identical in root space");
    }

    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var solar,out error)&&solar is not null,$"camera-drag Solar fixture: {error}");
    var solarScene=solar!;var solarCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,solarScene.Projection,CameraMode.Free);
    Check(solarScene.Focus(solarCamera,NativePresentationFocus.Earth),"Solar camera-drag Earth focus");var earthBefore=solarScene.FocusedBody;
    Check(solarScene.Focus(solarCamera,NativePresentationFocus.Mars)&&solarScene.Focus(solarCamera,NativePresentationFocus.Earth),"Solar focus away/back");
    Check(solarScene.FocusedBody==earthBefore,"focus away/back at paused simulation instant preserves Earth position, radius, and orientation");
    var focusedBefore=solarScene.FocusedPresentation(solarCamera);var solarCameraBefore=solarCamera.Position.Value;
    solarScene.ApplyPresentationInput(solarCamera,new NativeInputState{LookActive=1,MouseDeltaX=61f,MouseDeltaY=23f},out _,out _);
    var focusedAfter=solarScene.FocusedPresentation(solarCamera);
    Check(solarCamera.Position.Value!=solarCameraBefore,"Solar drag orbits camera after focus away/back");CheckOrientation(focusedBefore,focusedAfter,"Solar focus away/back");
    Console.WriteLine("Camera drag isolation: far/700km/near and Solar focus-away/back preserve body quaternion, root landmark, and dynamic hierarchy geographic anchor");
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
    Check(bodies.Select(body=>body.Label).Distinct().Count()==10,"presentation labels map uniquely to body identities");
    Check(sol.OrbitRootSamples.Length==SolarSystemScene.OrbitPathCount*SolarSystemScene.OrbitSampleCount&&sol.OrbitVertices.Length==SolarSystemScene.OrbitVertexCount,"bounded nine-path solar trajectory transport");
    Check(sol.OrbitCacheKeys.Length==SolarSystemScene.OrbitPathCount&&sol.OrbitCacheKeys.ToArray().Select(key=>key.OrbiterBodyId).SequenceEqual(SolarSystemScene.BodyOrder.Skip(1)),"orbit cache identities bind system definition, orbiter, parent, payload, and segment topology");
    var frozenOrbit=sol.OrbitRootSamples.ToArray();var frozenLocalOrbit=sol.OrbitParentLocalSamples.ToArray();var frozenOrbitVertices=sol.OrbitVertices.ToArray();
    for(var path=0;path<SolarSystemScene.OrbitPathCount;path++){var parentCenter=path==SolarSystemScene.MoonOrbitPathIndex?bodies[3].Position.Value:bodies[0].Position.Value;for(var sample=0;sample<SolarSystemScene.OrbitSampleCount;sample++)Check(Math.Sqrt(((sol.OrbitRootSamples[path*SolarSystemScene.OrbitSampleCount+sample]-parentCenter)-sol.OrbitPresentationLocalSamples[path*SolarSystemScene.OrbitSampleCount+sample]).LengthSquared)<1e-4,$"orbit path {path} composes one-instant presentation-local geometry with its current authoritative parent");}
    var moonPath=SolarSystemScene.MoonOrbitPathIndex*SolarSystemScene.OrbitSampleCount;
    var moonTraversal=Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),index=>system.GetNodeInTraversalOrder(index).Id==SolarSystemBodyIds.Moon);var moonBinding=system.GetNodeInTraversalOrder(moonTraversal).Ephemeris;Check(system.TryGetAnalyticalKepler(moonBinding.PayloadIndex,out var moonTrajectory),"corrected Moon orbit trajectory available");Check(system.TryGetAnalyticalCorrection(moonBinding.PayloadIndex,out var moonCorrection)&&moonCorrection.IsValid,"corrected Moon orbit timing available");Check(system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth,out var earthProperties),"corrected Moon parent constants available");var moonRadius=Math.Sqrt(moonTrajectory.StateAtEpoch.Position.LengthSquared);var moonAlpha=2d/moonRadius-moonTrajectory.StateAtEpoch.Velocity.LengthSquared/earthProperties.GravitationalParameter;var moonOsculatingPeriod=2d*Math.PI/(Math.Sqrt(earthProperties.GravitationalParameter)*moonAlpha*Math.Sqrt(moonAlpha));var moonPeriod=moonOsculatingPeriod/moonCorrection.TimeScale;Check(sol.MoonOrbitPeriodSeconds==moonPeriod,"Moon presentation period follows corrected runtime mean motion");var earthTraversal=Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),index=>system.GetNodeInTraversalOrder(index).Id==SolarSystemBodyIds.Earth);var expectedMoonLocalPoint=roots[moonTraversal].Translation-roots[earthTraversal].Translation;var moonAuthoritySample=sol.OrbitCurrentSampleIndices[SolarSystemScene.MoonOrbitPathIndex];Check(sol.OrbitAuthorityTime==SimulationInstant.Zero&&sol.OrbitPresentationLocalSamples[moonPath+moonAuthoritySample]==expectedMoonLocalPoint,"Moon curve and evaluated Moon share one authority instant and one exact Earth-local sample");var maximumMoonPresentationRadius=Enumerable.Range(0,SolarSystemScene.OrbitSegmentCount).Max(sample=>Math.Sqrt(sol.OrbitPresentationLocalSamples[moonPath+sample].LengthSquared));Check(maximumMoonPresentationRadius<500_000_000d,"Moon presentation orbit remains Earth-local rather than heliocentric");Check(sol.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount]==sol.OrbitRootSamples[moonPath],"Moon orbit stores an exact closure sentinel rather than relying on future-state coincidence");var moonFirstRelative=SolarSystemScene.ParentLocalToCamera(bodies[3].Position.Value,sol.OrbitPresentationLocalSamples[moonPath],camera.Position.Value);var moonClosingVertex=sol.OrbitVertices[(SolarSystemScene.MoonOrbitPathIndex*SolarSystemScene.OrbitSegmentCount+SolarSystemScene.OrbitSegmentCount-1)*2+1];Check(moonClosingVertex.X==(float)moonFirstRelative.X&&moonClosingVertex.Y==(float)moonFirstRelative.Y&&moonClosingVertex.Z==(float)moonFirstRelative.Z,"Moon orbit renderer explicitly connects the last unique point to sample zero through the Earth-local common reference");Console.WriteLine($"Moon presentation orbit: base={moonOsculatingPeriod:R}s ({moonOsculatingPeriod/86_400d:R}d); corrected={moonPeriod:R}s ({moonPeriod/86_400d:R}d); currentSample={moonAuthoritySample}; preSnapError={sol.OrbitCurrentSampleErrors[SolarSystemScene.MoonOrbitPathIndex]:R}m; closed=true");Check(CelestialSystemEvaluator.TryEvaluateSystem(system,SimulationInstant.Zero,evaluations,roots,staging,stagingRoots).Succeeded,"restore independent Sol epoch evaluation");for(var index=0;index<10;index++){var body=bodies[index];var record=sol.DistantBodies[index];var expected=index==0?body.Position.Value-camera.Position.Value:SolarSystemScene.ParentLocalToCamera(index==4?bodies[3].Position.Value:bodies[0].Position.Value,body.Position.Value-(index==4?bodies[3].Position.Value:bodies[0].Position.Value),camera.Position.Value);Check((record.Enabled&255u)==(uint)(index+1)&&record.CenterX==(float)expected.X&&record.CenterY==(float)expected.Y&&record.CenterZ==(float)expected.Z,$"label and marker anchor {body.Label} uses the same most-local common reference as its orbit");}Check((sol.DistantBodies[0].Enabled&0x80000000u)!=0,"focused label/marker metadata");

    var moonControls=sol.MoonOrbitControlSamples;Check(moonControls.SequenceEqual(sol.OrbitParentLocalSamples.Slice(moonPath,SolarSystemScene.OrbitSegmentCount).ToArray()),"Moon immutable anomaly controls remain the cached parent-local two-body shape");
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
    Check(!sol.Focus(camera,NativePresentationFocus.None),"none focus does not select a body");sol.ResetPresentationCamera(camera);var preservedFocusOrientation=camera.Orientation;for(var index=0;index<10;index++){var focus=(NativePresentationFocus)(index+1);Check(sol.Focus(camera,focus),$"focus target {focus}");var firstPosition=camera.Position.Value;var expectedDistance=sol.FocusFramingDistance(sol.FocusedBody);Check(sol.Focus(camera,focus)&&camera.Position.Value==firstPosition,"deterministic extent-aware focus distance");sol.Update(camera);var earthProduction=index==3;var expectedRegime=earthProduction?PlanetaryRenderRegime.DetailedOnly:PlanetaryRenderRegime.DistantOnly;var expectedCameraOrientation=preservedFocusOrientation;Check(sol.CameraPresentationMode==SolarCameraPresentationMode.Free3D&&camera.Orientation==expectedCameraOrientation&&sol.FocusIndex==index&&sol.FocusedBody.BodyId==SolarSystemScene.BodyOrder[index]&&sol.FocusedBlend.Regime==expectedRegime&&sol.DetailedComputeRequested==earthProduction&&sol.DistantBodyCount==10,$"focus mapping, inertial camera orientation, explicit Earth-only production eligibility, and bounded non-Earth framing {focus}");var actualDistance=Math.Sqrt((camera.Position.Value-sol.FocusedBody.Position.Value).LengthSquared);Check(Math.Abs(actualDistance-expectedDistance)<=Math.Max(1e-4d,expectedDistance*1e-8d)&&actualDistance>sol.FocusedBody.RadiusMetres*4d,$"positive extent-aware focus distance {focus}");Check(SolarOverlayLayout.TryProjectBody(sol.FocusedBody,camera,out _,out _,out var focusedRadius,out _)&&focusedRadius is >=.06d and <=.151d,"focused body has useful deterministic apparent size");var focusedMaterial=sol.FocusedPresentation(camera);Check(focusedMaterial.BodyIdLow==(uint)sol.FocusedBody.BodyId&&focusedMaterial.AlbedoSource==sol.DistantBodies[0].AlbedoSource&&focusedMaterial.MaterialKind==sol.DistantBodies[0].MaterialKind,$"distant/detail material identity agrees for {focus}");}

    Check(sol.Focus(camera,NativePresentationFocus.Earth),"Earth focus for overlay hierarchy");var earthOrbitOpacity=sol.OrbitOpacityBytes.ToArray();Check(sol.VisibleOrbitCount==2&&earthOrbitOpacity[2]>0&&earthOrbitOpacity[3]>earthOrbitOpacity[2]&&earthOrbitOpacity.Where((_,index)=>index is not 2 and not 3).All(value=>value==0),"Earth-local view retains only focused and child hierarchy orbits");Check((sol.DistantBodies[0].Enabled&SolarSystemScene.MarkerVisibleBit)==0,"rendered focused Earth suppresses redundant marker");Check(sol.Focus(camera,NativePresentationFocus.Jupiter),"Jupiter focus for overlay hierarchy");Check(sol.VisibleOrbitCount==1&&sol.OrbitOpacityBytes[5]>0,"Jupiter-local view retains only the focused hierarchy orbit");var beforeMapReset=sol.Presentation.Bodies.ToArray();var timeBeforeMapReset=sol.CurrentTime;sol.ResetPresentationCamera(camera);sol.Update(camera);Check(sol.CameraPresentationMode==SolarCameraPresentationMode.SolarMap&&sol.FocusIndex==0&&sol.VisibleOrbitCount==SolarSystemScene.OrbitPathCount&&sol.CurrentTime==timeBeforeMapReset&&beforeMapReset.SequenceEqual(sol.Presentation.Bodies),"Solar Map reset restores overview without changing time or celestial evaluation");

    Check(sol.Focus(camera,3),"Earth focus for promotion");var earth=sol.FocusedBody;camera.Position=camera.Position with{Value=earth.Position.Value+Double3.UnitZ*earth.RadiusMetres*15d};sol.Update(camera);Check(sol.FocusedBlend.Regime==PlanetaryRenderRegime.Transition&&sol.FocusedBlend.DrawDetailed&&sol.FocusedBlend.DrawDistant,"focused transition retains the representation-selector state");Check(sol.DistantBodies[0].DetailedAlpha>0&&sol.DistantBodies[0].DistantAlpha==0&&sol.DistantBodies.Skip(1).All(body=>body.Regime==NativePlanetaryRenderRegime.DistantOnly&&body.DetailedAlpha==0&&body.DistantAlpha==1),"production Earth transport suppresses the generic distant owner while other bodies remain distant-only");
    Check(sol.Focus(camera,9),"move detail eligibility to Neptune");Check(sol.FocusedBody.BodyId==SolarSystemBodyIds.Neptune.Value&&sol.DistantBodies.Skip(1).All(body=>body.Regime==NativePlanetaryRenderRegime.DistantOnly&&body.DetailedAlpha==0),"old focus returns to distant batch");Check(sol.Focus(camera,3)&&sol.FocusedBody.Position==earth.Position,"Earth Neptune Earth focus identity");

    var moon=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Moon.Value);var neptune=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Neptune.Value);var sun=bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Sun.Value);double Distance(PlanetRenderProxy left,PlanetRenderProxy right)=>Math.Sqrt((left.Position.Value-right.Position.Value).LengthSquared);var earthMoon=Distance(earth,moon);var evaluatedEarthMoon=Math.Sqrt((EvaluatedCenter(earth.BodyId)-EvaluatedCenter(moon.BodyId)).LengthSquared);var tolerance=Math.Max(1e-6,evaluatedEarthMoon*1e-15);Check(Math.Abs(earthMoon-evaluatedEarthMoon)<=tolerance,"Earth-Moon snapshot/evaluation separation agreement");var sunEarth=Distance(sun,earth);var earthNeptune=Distance(earth,neptune);Check(sunEarth==Math.Sqrt((EvaluatedCenter(sun.BodyId)-EvaluatedCenter(earth.BodyId)).LengthSquared)&&earthNeptune==Math.Sqrt((EvaluatedCenter(earth.BodyId)-EvaluatedCenter(neptune.BodyId)).LengthSquared),"Sun-Earth and Earth-Neptune evaluated relationships");
    Check(sol.Focus(camera,0),"Sun focus for physical-radius check");sol.Update(camera);var mercury=bodies[1];Check(sol.DistantBodies[1].Radius==(float)mercury.RadiusMetres&&sol.Presentation.Bodies[1].RadiusMetres==mercury.RadiusMetres,"screen-space marker does not inflate physical sphere radius");Check(frozen.SequenceEqual(sol.Presentation.Bodies.ToArray())&&frozenOrbit.SequenceEqual(sol.OrbitRootSamples.ToArray()),"focus and presentation aids do not mutate snapshot or trajectories");Check(sol.DistantBodies.Take(sol.DistantBodyCount).All(body=>body.Radius>0&&float.IsFinite(body.CenterX)&&float.IsFinite(body.CenterY)&&float.IsFinite(body.CenterZ)),"physical body records finite");

    Check(SolarOverlayLayout.LabelGlyphWidthNdc>0d&&SolarOverlayLayout.LabelGlyphHeightNdc>SolarOverlayLayout.LabelGlyphWidthNdc&&SolarOverlayLayout.CharacterStrideNdc>=SolarOverlayLayout.LabelGlyphWidthNdc,"professional label metrics use restrained proportional sans-serif cells");var labelDistanceA=earth.RadiusMetres*12d;var labelDistanceB=SolAnalyticalDefinition.AstronomicalUnitMetres*12d;var labelCameraA=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*labelDistanceA),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var labelCameraB=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*labelDistanceB),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);Check(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out var labelBoundsA,out _)&&SolarOverlayLayout.TryProjectLabel(earth,labelCameraB,true,out var labelBoundsB,out _)&&Math.Abs((labelBoundsA.MaxX-labelBoundsA.MinX)-(labelBoundsB.MaxX-labelBoundsB.MinX))<1e-15d&&Math.Abs((labelBoundsA.MaxY-labelBoundsA.MinY)-(labelBoundsB.MaxY-labelBoundsB.MinY))<1e-15d,"celestial labels retain stable apparent screen size across camera distance");
    var solarFrames=new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root,null,ReferenceFrameKind.Ecl,"solar-test-root"),CelestialFrameFactory.RootEcl())]);var solarResolver=new ReferenceFrameResolver(solarFrames);var zoomCases=new[]{("near Earth",earth,earth.RadiusMetres*10d),("Earth-Moon",earth,690_280_069.1073977d),("inner system",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*5d),("full system",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*45d),("beyond overview",sun,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu)};foreach(var zoomCase in zoomCases){var zoomCamera=new CameraState(new FramePosition(root,zoomCase.Item2.Position.Value+Double3.UnitZ*zoomCase.Item3),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);Check(CameraRenderSnapshotBuilder.TryBuildInfiniteFar(zoomCamera,solarResolver,root,out var gpuCamera,out _,out _),$"{zoomCase.Item1} infinite-far camera build");Check(MatrixFinite(gpuCamera.ViewProjection),$"{zoomCase.Item1} view/projection finite");Check(SolarOverlayLayout.TryProjectLabel(zoomCase.Item2,zoomCamera,true,out var projectedBounds,out var projectedDepth)&&projectedBounds.IsFinite&&projectedDepth is >=0d and <1d,$"{zoomCase.Item1} focused marker/label projectable");var relative=zoomCase.Item2.Position.Value-zoomCamera.Position.Value;Check(((float)relative.X is var rx&&float.IsFinite(rx))&&((float)relative.Y is var ry&&float.IsFinite(ry))&&((float)relative.Z is var rz&&float.IsFinite(rz)),$"{zoomCase.Item1} camera-relative center finite");}
    var lastValidDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*45d*Math.Pow(1.1d,7d);var firstInvalidDistance=SolAnalyticalDefinition.AstronomicalUnitMetres*45d*Math.Pow(1.1d,8d);var finiteLast=new CameraState(new FramePosition(root,sun.Position.Value+Double3.UnitZ*lastValidDistance),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var finiteFirst=new CameraState(new FramePosition(root,sun.Position.Value+Double3.UnitZ*firstInvalidDistance),DoubleQuaternion.Identity,sol.Projection,CameraMode.Free);var finiteLastBuilt=CameraRenderSnapshotBuilder.TryBuild(finiteLast,solarResolver,root,out var finiteLastGpu,out _,out _);var finiteFirstBuilt=CameraRenderSnapshotBuilder.TryBuild(finiteFirst,solarResolver,root,out var finiteFirstGpu,out _,out _);Check(finiteLastBuilt&&finiteFirstBuilt,"finite-far boundary camera builds");var finiteLastDepth=ProjectedDepth(finiteLastGpu.ViewProjection,-lastValidDistance);var finiteFirstDepth=ProjectedDepth(finiteFirstGpu.ViewProjection,-firstInvalidDistance);Check(finiteLastDepth<=1d&&finiteFirstDepth>1d,"finite FP32 projection reproduces last-visible/first-clipped boundary");var infiniteFirstBuilt=CameraRenderSnapshotBuilder.TryBuildInfiniteFar(finiteFirst,solarResolver,root,out var infiniteFirstGpu,out _,out _);Check(infiniteFirstBuilt&&ProjectedDepth(infiniteFirstGpu.ViewProjection,-firstInvalidDistance)<1d,"infinite-far projection keeps former failing distance inside Vulkan depth");var reversedFiniteBuilt=CameraRenderSnapshotBuilder.TryBuildReversedZ(finiteLast,solarResolver,root,out var reversedFiniteGpu,out _,out _);var reversedInfiniteBuilt=CameraRenderSnapshotBuilder.TryBuildReversedInfiniteFar(finiteFirst,solarResolver,root,out var reversedInfiniteGpu,out _,out _);var reversedNearDepth=ProjectedDepth(reversedFiniteGpu.ViewProjection,-sol.Projection.NearClip);var reversedFarDepth=ProjectedDepth(reversedFiniteGpu.ViewProjection,-sol.Projection.FarClip);var reversedDistantDepth=ProjectedDepth(reversedInfiniteGpu.ViewProjection,-firstInvalidDistance);Check(reversedFiniteBuilt&&Math.Abs(reversedNearDepth-1d)<1e-6d&&Math.Abs(reversedFarDepth)<1e-6d,"finite reversed-Z projection maps near to one and far to zero without clip-space subtraction");Check(reversedInfiniteBuilt&&reversedDistantDepth>0d&&reversedDistantDepth<1d,"infinite reversed-Z projection retains positive depth at Solar scale");
    Check(sol.Focus(camera,0),"Sun focus for zoom round trip");sol.ResetPresentationCamera(camera);var roundTripDistance=sol.OrbitDistance;sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=5},out _,out _);sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-5},out _,out _);Check(Math.Abs(sol.OrbitDistance-roundTripDistance)<=roundTripDistance*1e-15d,"Solar zoom in/out round trip stable");sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=-100},out _,out _);Check(sol.OrbitDistance==SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu&&sol.VisibleLabelCount>0&&sol.VisibleLabelIds[0]==SolarSystemBodyIds.Sun.Value,"maximum Solar overview retains focused presentation label");Console.WriteLine($"Solar finite-depth boundary: last={lastValidDistance/SolAnalyticalDefinition.AstronomicalUnitMetres:R} AU depth={finiteLastDepth:R}; first={firstInvalidDistance/SolAnalyticalDefinition.AstronomicalUnitMetres:R} AU depth={finiteFirstDepth:R}; infiniteDepth={ProjectedDepth(infiniteFirstGpu.ViewProjection,-firstInvalidDistance):R}");

    static bool MatrixFinite(in Float4x4 matrix)=>new[]{matrix.C0R0,matrix.C0R1,matrix.C0R2,matrix.C0R3,matrix.C1R0,matrix.C1R1,matrix.C1R2,matrix.C1R3,matrix.C2R0,matrix.C2R1,matrix.C2R2,matrix.C2R3,matrix.C3R0,matrix.C3R1,matrix.C3R2,matrix.C3R3}.All(float.IsFinite);
    static double ProjectedDepth(in Float4x4 matrix,double cameraZ){var z=(float)cameraZ;var clipZ=matrix.C2R2*z+matrix.C3R2;var clipW=matrix.C2R3*z+matrix.C3R3;return clipZ/clipW;}
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var separation,out var separationError)&&separation is not null,$"camera/body separation scene: {separationError}");var separationScene=separation!;var separationCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,separationScene.Projection,CameraMode.Free);Check(separationScene.Focus(separationCamera,3),"camera/body separation Earth focus");separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{PauseToggle=1},out _,out _);var fixedTime=separationScene.CurrentTime;var fixedSnapshot=separationScene.Presentation.Bodies.ToArray();var fixedEarth=separationScene.FocusedBody;var anchorDirection=new Double3(.31,.42,.851).Normalized();var anchorIdentity=anchorDirection;var orientationProbeLatitude=.25d;var orientationProbeLongitude=-1.1d;var orientationProbeCosine=Math.Cos(orientationProbeLatitude);var orientationProbeDirection=new Double3(orientationProbeCosine*Math.Cos(orientationProbeLongitude),Math.Sin(orientationProbeLatitude),orientationProbeCosine*Math.Sin(orientationProbeLongitude));var orientationProbeBodyFixed=orientationProbeDirection*(fixedEarth.RadiusMetres+125d);Check(CelestialBodyFixedFrameEvaluator.TryTransformBodyFixedPosition(SolarSystemBodyIds.Earth,fixedTime,orientationProbeBodyFixed,fixedEarth.Position.Value,out var anchorRoot),"Earth body-fixed position transform");for(var step=0;step<12;step++){separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{LookActive=1,MouseDeltaX=31-step,MouseDeltaY=step-7,MouseWheelDetents=step%2==0?1:-1},out _,out _);Check(separationScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),separationCamera,out separationError),$"paused camera manipulation {step}: {separationError}");}var afterEarth=separationScene.FocusedBody;Check(separationScene.IsPaused&&separationScene.CurrentTime==fixedTime&&fixedEarth.BodyFixedToRoot==afterEarth.BodyFixedToRoot&&fixedSnapshot.SequenceEqual(separationScene.Presentation.Bodies.ToArray())&&anchorIdentity==anchorDirection&&CelestialBodyFixedFrameEvaluator.TryTransformBodyFixedPosition(SolarSystemBodyIds.Earth,fixedTime,orientationProbeBodyFixed,fixedEarth.Position.Value,out var anchorRootAfter)&&anchorRootAfter==anchorRoot,"paused drag/zoom preserves Earth orientation, body-fixed point, production surface identity, time, and celestial state");Check(separationScene.Focus(separationCamera,5),"camera/body separation Mars focus");var fixedMars=separationScene.FocusedBody;for(var step=0;step<8;step++)separationScene.ApplyPresentationInput(separationCamera,new NativeInputState{LookActive=1,MouseDeltaX=-22,MouseDeltaY=9,MouseWheelDetents=step%2==0?1:-1},out _,out _);Check(separationScene.CurrentTime==fixedTime&&separationScene.FocusedBody.BodyFixedToRoot==fixedMars.BodyFixedToRoot&&fixedSnapshot.SequenceEqual(separationScene.Presentation.Bodies.ToArray()),"paused Mars drag/zoom preserves physical orientation and celestial state");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var orientationProof,out var orientationError)&&orientationProof is not null,$"Earth handoff orientation proof scene: {orientationError}");var orientationScene=orientationProof!;var orientationCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,orientationScene.Projection,CameraMode.Free);Check(orientationScene.Focus(orientationCamera,NativePresentationFocus.Earth),"Earth handoff orientation focus");orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{PauseToggle=1},out _,out _);var orientationTime=orientationScene.CurrentTime;var orientationSnapshot=orientationScene.Presentation.Bodies.ToArray();var orientationEarth=orientationScene.FocusedBody;var geographicDirection=new Double3(.371d,.482d,.793d).Normalized();var geographicIdentity=geographicDirection;var geographicRootDirection=orientationEarth.BodyFixedToRoot.Rotate(geographicDirection);var distantRecoveredDirection=orientationEarth.BodyFixedToRoot.Conjugate().Normalized().Rotate(geographicRootDirection);Check(Math.Sqrt((distantRecoveredDirection-geographicDirection).LengthSquared)<1e-12d&&orientationEarth.BodyFixedToRoot.Rotate(geographicDirection)==geographicRootDirection,"distant, global, and dynamic anchored paths share body-local direction semantics");
    AssertPausedEarthPath("near-field",PlanetaryRenderRegime.DetailedOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);AssertPausedEarthPath("handoff",PlanetaryRenderRegime.Transition);var outwardNotches=0;while(orientationScene.FocusedBlend.Regime!=PlanetaryRenderRegime.DistantOnly&&outwardNotches++<16)orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-1},out _,out _);Check(orientationScene.FocusedBlend.Regime==PlanetaryRenderRegime.DistantOnly,"one-notch stepping reaches first distant-only state");AssertPausedEarthPath("just-outside-handoff",PlanetaryRenderRegime.DistantOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-4},out _,out _);AssertPausedEarthPath("far-distant",PlanetaryRenderRegime.DistantOnly);for(var crossing=0;crossing<4;crossing++){Check(orientationScene.Focus(orientationCamera,NativePresentationFocus.Earth),$"handoff return to near {crossing}");AssertPausedEarthPath($"repeat-near-{crossing}",PlanetaryRenderRegime.DetailedOnly);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{MouseWheelDetents=-8},out _,out _);Check(orientationScene.FocusedBlend.Regime is PlanetaryRenderRegime.Transition or PlanetaryRenderRegime.DistantOnly,$"handoff outward crossing {crossing}");AssertPausedEarthPath($"repeat-far-{crossing}",orientationScene.FocusedBlend.Regime);}
    void AssertPausedEarthPath(string path,PlanetaryRenderRegime expectedRegime){orientationScene.Update(orientationCamera);var beforeBody=orientationScene.FocusedBody;var beforeNative=orientationScene.FocusedPresentation(orientationCamera);var beforeDistant=orientationScene.DistantBodies[0];var beforePosition=orientationCamera.Position.Value;var beforeView=orientationCamera.Orientation;var beforeDistance=Math.Sqrt((beforePosition-beforeBody.Position.Value).LengthSquared);var beforeRootPoint=RootSurfacePoint(beforeBody,geographicDirection);var beforeDetailed=DetailedCameraRelativePoint(beforeBody,beforePosition,geographicDirection);var beforeFar=DistantCameraRelativePoint(beforeBody,beforePosition,geographicDirection);orientationScene.ApplyPresentationInput(orientationCamera,new NativeInputState{LookActive=1,MouseDeltaX=37,MouseDeltaY=-19},out _,out _);Check(orientationScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),orientationCamera,out orientationError),$"{path} paused host advance: {orientationError}");var afterBody=orientationScene.FocusedBody;var afterNative=orientationScene.FocusedPresentation(orientationCamera);var afterDistant=orientationScene.DistantBodies[0];var afterDistance=Math.Sqrt((orientationCamera.Position.Value-afterBody.Position.Value).LengthSquared);var afterRootPoint=RootSurfacePoint(afterBody,geographicDirection);var afterDetailed=DetailedCameraRelativePoint(afterBody,orientationCamera.Position.Value,geographicDirection);var afterFar=DistantCameraRelativePoint(afterBody,orientationCamera.Position.Value,geographicDirection);var lookDirection=(afterBody.Position.Value-orientationCamera.Position.Value).Normalized();var cameraForward=orientationCamera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();Check(orientationScene.FocusedBlend.Regime==expectedRegime&&orientationCamera.Position.Value!=beforePosition&&orientationCamera.Orientation!=beforeView&&Math.Abs(afterDistance-beforeDistance)<=Math.Max(1e-6d,beforeDistance*1e-12d)&&Double3.Dot(lookDirection,cameraForward)>.999999999999d,$"{path} drag orbits camera at fixed distance and retains body focus");Check(orientationScene.IsPaused&&orientationScene.CurrentTime==orientationTime&&beforeBody.Position==afterBody.Position&&beforeBody.BodyFixedToRoot==afterBody.BodyFixedToRoot&&orientationSnapshot.SequenceEqual(orientationScene.Presentation.Bodies.ToArray()),$"{path} body position, quaternion, time, and celestial snapshot invariant");Check(beforeRootPoint==afterRootPoint&&RootPointBits(beforeRootPoint)==RootPointBits(afterRootPoint),$"{path} representative physical root-space surface point is bit-identical across paused drag");Check(Math.Sqrt((beforeDetailed-beforeFar).LengthSquared)<1e-5d&&Math.Sqrt((afterDetailed-afterFar).LengthSquared)<1e-5d,$"{path} detailed and distant transform chains resolve one physical root-space orientation");Check(NativeOrientation(beforeNative)==NativeOrientation(afterNative)&&NativeOrientation(beforeDistant)==NativeOrientation(afterDistant)&&NativeOrientation(afterNative)==NativeOrientation(afterDistant),$"{path} focused and distant native paths consume one immutable quaternion");Check(geographicIdentity==geographicDirection&&orientationEarth.BodyFixedToRoot.Rotate(geographicDirection)==geographicRootDirection,$"{path} geographic anchor and production surface identity invariant");}
    static (int X,int Y,int Z,int W) NativeOrientation(in NativePlanetaryPresentation value)=>(BitConverter.SingleToInt32Bits(value.BodyOrientationX),BitConverter.SingleToInt32Bits(value.BodyOrientationY),BitConverter.SingleToInt32Bits(value.BodyOrientationZ),BitConverter.SingleToInt32Bits(value.BodyOrientationW));
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var allBodyProof,out var allBodyError)&&allBodyProof is not null,$"all-body live-path orientation scene: {allBodyError}");var allBodyScene=allBodyProof!;var allBodyCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,allBodyScene.Projection,CameraMode.Free);allBodyScene.ApplyPresentationInput(allBodyCamera,new NativeInputState{PauseToggle=1},out _,out _);var allBodySnapshot=allBodyScene.Presentation.Bodies.ToArray();var allBodyTime=allBodyScene.CurrentTime;var regimeProofs=new[]{PlanetaryRenderRegime.DetailedOnly,PlanetaryRenderRegime.Transition,PlanetaryRenderRegime.DistantOnly};
    for(var focusValue=2;focusValue<=10;focusValue++)
    {
        var focus=(NativePresentationFocus)focusValue;
        var bodyRegimeProofs=focus==NativePresentationFocus.Earth?regimeProofs:[PlanetaryRenderRegime.DistantOnly];
        foreach(var expectedRegime in bodyRegimeProofs)
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
    var planetaryVertexPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","native","NovaCore.Native","shaders","planetary.vert"));var planetaryVertexSource=File.ReadAllText(planetaryVertexPath);var distantVertexSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"distant_planet.vert"));var nativeRendererSource=File.ReadAllText(Path.Combine(Path.GetDirectoryName(planetaryVertexPath)!,"..","NovaCoreNative.cpp"));Check(planetaryVertexSource.Contains("lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz",StringComparison.Ordinal)&&!planetaryVertexSource.Contains("lighting.sourceCenterExposure.xyz-p.centerRadius.xyz",StringComparison.Ordinal),"detailed shader derives body-local Sun direction from root-camera-relative presentation center only");Check(planetaryVertexSource.Contains("RotateQuaternion(localPosition,presentation.bodyOrientation)",StringComparison.Ordinal)&&distantVertexSource.Contains("vec3 local=RotateQuaternion(bodyLocalPosition,presentation.bodyOrientation)",StringComparison.Ordinal)&&distantVertexSource.Contains("presentation.centerLow.xyz+local",StringComparison.Ordinal),"detailed and distant vertices apply only the immutable body quaternion before compensated shared view/projection");Check(!planetaryVertexSource.Contains("rootOrbit",StringComparison.OrdinalIgnoreCase)&&!distantVertexSource.Contains("rootOrbit",StringComparison.OrdinalIgnoreCase)&&!planetaryVertexSource.Contains("cameraOrientation",StringComparison.OrdinalIgnoreCase)&&!distantVertexSource.Contains("cameraOrientation",StringComparison.OrdinalIgnoreCase),"planet model shaders do not consume camera-orbit orientation state");Check(nativeRendererSource.Contains("handoffDepth.depthWriteEnable=VK_FALSE",StringComparison.Ordinal)&&nativeRendererSource.Contains("const uint32_t firstUnfocused=handoff?1u:0u",StringComparison.Ordinal),"focused handoff sphere cannot invisibly write depth or fight detailed geometry");
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
        var hierarchyEarthBefore=hierarchyScene.Presentation.Bodies[3].Position.Value;
        var hierarchyMoonSampleBefore=hierarchyScene.OrbitRootSamples[moonPath];
        var hierarchyBuildsBefore=hierarchyScene.OrbitCurveBuildCount;
        var hierarchyLocalBefore=hierarchyScene.OrbitParentLocalSamples.ToArray();
        Check(hierarchyScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),hierarchyCamera,out orbitHierarchyError),$"Moon hierarchy {hierarchyRate.Numerator}x advance: {orbitHierarchyError}");
        var hierarchyEarth=hierarchyScene.Presentation.Bodies[3].Position.Value;
        var hierarchyMaximumRadius=Enumerable.Range(0,SolarSystemScene.OrbitSegmentCount).Max(sample=>Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+sample]-hierarchyEarth).LengthSquared));
        var hierarchyMoon=hierarchyScene.Presentation.Bodies[4].Position.Value;var hierarchyMoonLocal=hierarchyMoon-hierarchyEarth;var hierarchyCurrentSample=hierarchyScene.OrbitCurrentSampleIndices[SolarSystemScene.MoonOrbitPathIndex];var hierarchyAttachmentError=Math.Sqrt((hierarchyScene.OrbitPresentationLocalSamples[moonPath+hierarchyCurrentSample]-hierarchyMoonLocal).LengthSquared);
        Check(hierarchyScene.OrbitCurveBuildCount==hierarchyBuildsBefore&&hierarchyLocalBefore.SequenceEqual(hierarchyScene.OrbitParentLocalSamples.ToArray())&&hierarchyScene.OrbitAuthorityTime==hierarchyScene.CurrentTime&&hierarchyAttachmentError==0d&&hierarchyMaximumRadius<500_000_000d,$"Moon body and Earth-local orbit share one authority instant and exact local sample at {hierarchyRate.Numerator}x");
        Check(hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount]==hierarchyScene.OrbitRootSamples[moonPath],$"Moon orbit remains exactly closed at {hierarchyRate.Numerator}x");
        var hierarchyIncoming=(hierarchyScene.OrbitRootSamples[moonPath]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).Normalized();var hierarchyOutgoing=(hierarchyScene.OrbitRootSamples[moonPath+1]-hierarchyScene.OrbitRootSamples[moonPath]).Normalized();var hierarchySeamTurn=Math.Acos(Math.Clamp(Double3.Dot(hierarchyIncoming,hierarchyOutgoing),-1d,1d))*180d/Math.PI;var hierarchyPreviousLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-2]).LengthSquared);var hierarchySeamLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath]-hierarchyScene.OrbitRootSamples[moonPath+SolarSystemScene.OrbitSegmentCount-1]).LengthSquared);var hierarchyNextLength=Math.Sqrt((hierarchyScene.OrbitRootSamples[moonPath+1]-hierarchyScene.OrbitRootSamples[moonPath]).LengthSquared);var hierarchyMeanLength=(hierarchyPreviousLength+hierarchySeamLength+hierarchyNextLength)/3d;var hierarchySegmentDiscontinuity=Math.Max(Math.Abs(hierarchySeamLength-hierarchyPreviousLength),Math.Abs(hierarchyNextLength-hierarchySeamLength))/hierarchyMeanLength;Check(hierarchySeamTurn<4d&&hierarchySegmentDiscontinuity<.06d,$"Moon same-instant osculating seam remains visually continuous at {hierarchyRate.Numerator}x");Console.WriteLine($"Moon same-instant orbit {hierarchyRate.Numerator}x: turn={hierarchySeamTurn:R}deg; relativeSegmentDiscontinuity={hierarchySegmentDiscontinuity:R}; authority={hierarchyScene.OrbitAuthorityTime.Ticks}; sample={hierarchyCurrentSample}; attachment={hierarchyAttachmentError:E3}m");
    }
    hierarchyScene.Update(hierarchyCamera);
    var moonOrbitUpdateAllocationBefore=GC.GetAllocatedBytesForCurrentThread();
    for(var update=0;update<10_000;update++)hierarchyScene.Update(hierarchyCamera);
    var moonOrbitUpdateAllocated=GC.GetAllocatedBytesForCurrentThread()-moonOrbitUpdateAllocationBefore;
    Check(moonOrbitUpdateAllocated==0,"warmed closed Moon orbit camera-relative updates allocate zero managed bytes");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var cadenceValue,out var cadenceError)&&cadenceValue is not null,$"Solar bounded orbit cadence scene: {cadenceError}");
    var cadenceScene=cadenceValue!;var cadenceCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,cadenceScene.Projection,CameraMode.Free);var cadenceBuilds=cadenceScene.OrbitCurveBuildCount;var cadenceLocal=cadenceScene.OrbitParentLocalSamples.ToArray();var cadenceStarted=System.Diagnostics.Stopwatch.GetTimestamp();for(var frame=0;frame<256;frame++)Check(cadenceScene.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),cadenceCamera,out cadenceError),$"Solar immutable orbit cadence frame {frame}: {cadenceError}");var cadenceMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-cadenceStarted)*1000d/System.Diagnostics.Stopwatch.Frequency;Check(cadenceScene.OrbitCurveBuildCount==cadenceBuilds&&cadenceLocal.SequenceEqual(cadenceScene.OrbitParentLocalSamples.ToArray())&&cadenceScene.OrbitCurveReuseCount>=256,"ordinary Solar frames reuse immutable closed parent-local orbit curves");Console.WriteLine($"Solar orbit cache: entries={cadenceScene.OrbitCacheKeys.Length}; frames=256; builds={cadenceScene.OrbitCurveBuildCount}; reuses={cadenceScene.OrbitCurveReuseCount}; averageMs={cadenceMilliseconds/256d:R}");
    var zoomMinimum=earth.RadiusMetres*1.05d;var zoomMaximum=SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.MaximumOverviewDistanceAu;var farZoom=SolarCameraZoomPolicy.Apply(SolAnalyticalDefinition.AstronomicalUnitMetres,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var nearStart=earth.RadiusMetres*2d;var nearZoom=SolarCameraZoomPolicy.Apply(nearStart,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var reverseNearZoom=SolarCameraZoomPolicy.Apply(nearZoom,earth.RadiusMetres,zoomMinimum,zoomMaximum,-1);Check(Math.Abs((SolAnalyticalDefinition.AstronomicalUnitMetres-earth.RadiusMetres)/(farZoom-earth.RadiusMetres)-SolarCameraZoomPolicy.DistanceRatioPerDetent)<1e-12d&&Math.Abs((nearStart-earth.RadiusMetres)/(nearZoom-earth.RadiusMetres)-SolarCameraZoomPolicy.DistanceRatioPerDetent)<1e-12d,"wheel zoom applies one continuous logarithmic altitude ratio at astronomical and local distance");Check(SolAnalyticalDefinition.AstronomicalUnitMetres-farZoom>nearStart-nearZoom&&Math.Abs(reverseNearZoom-nearStart)<1e-7d,"wheel zoom supplies large astronomical travel, fine near-body control, and reversible detents");Check(sol.Focus(camera,3),"Earth focus for interaction");var interactionSnapshot=sol.Presentation;var interactionOrbit=sol.OrbitRootSamples.ToArray();var initialOrbitDistance=sol.OrbitDistance;var initialCamera=camera.Position.Value;var initialView=camera.Orientation;sol.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-30},out var rateChanged,out var pauseChanged);var orbitDistanceTolerance=Math.Max(1e-6d,initialOrbitDistance*1e-12d);var interactionLook=(sol.FocusedBody.Position.Value-camera.Position.Value).Normalized();var interactionForward=camera.Orientation.Rotate(new Double3(0,0,-1)).Normalized();Check(!rateChanged&&!pauseChanged&&camera.Position.Value!=initialCamera&&camera.Orientation!=initialView&&Math.Abs(Math.Sqrt((camera.Position.Value-sol.FocusedBody.Position.Value).LengthSquared)-initialOrbitDistance)<orbitDistanceTolerance&&Double3.Dot(interactionLook,interactionForward)>.999999999999d,"Solar mouse drag orbits current focus without changing distance");var draggedPosition=camera.Position.Value;var draggedView=camera.Orientation;sol.ApplyPresentationInput(camera,new NativeInputState{MouseWheelDetents=1},out _,out _);Check(sol.OrbitDistance<initialOrbitDistance&&camera.Position.Value!=draggedPosition&&camera.Orientation==draggedView,"Solar wheel changes distance only and preserves root-inertial orbital direction");
    sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(1,10)&&sol.SpeedHudVisible&&sol.SpeedHudLabel=="Simulation Speed: 0.1x (Slow Motion)"&&sol.SolarLighting(camera).SpeedHud!=0,"Solar reaches 0.1x and immediately publishes its HUD");sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out rateChanged,out _);Check(!rateChanged&&sol.Rate==new SimulationRate(1,10),"Solar 0.1x lower clamp");for(var step=0;step<4;step++)sol.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(10,1)&&sol.SpeedHudLabel=="Simulation Speed: 10x","Solar ordered rate steps reach 10x");for(var step=0;step<15;step++)sol.ApplyPresentationInput(camera,new NativeInputState{DeltaSeconds=.1f},out _,out _);Check(sol.SpeedHudVisible&&sol.SpeedHudAlpha is >0f and <1f,"Solar speed HUD uses a readable wall-time hold and bounded fade");for(var step=0;step<5;step++)sol.ApplyPresentationInput(camera,new NativeInputState{DeltaSeconds=.1f},out _,out _);Check(!sol.SpeedHudVisible&&sol.SpeedHudAlpha==0f&&sol.SolarLighting(camera).SpeedHud==0,"Solar speed HUD disappears after two wall-time seconds");sol.ApplyPresentationInput(camera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(rateChanged&&sol.Rate==new SimulationRate(30,1)&&sol.SpeedHudVisible,"changing speed resets the HUD timer");sol.ApplyPresentationInput(camera,new NativeInputState{RateDecrease=1},out _,out _);Check(sol.Rate==new SimulationRate(10,1),"Solar restores 10x for deterministic replay");sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out pauseChanged);var pausedTime=sol.CurrentTime;var pausedAdvance=sol.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out var pausedError);Check(pauseChanged&&sol.IsPaused&&pausedAdvance&&sol.CurrentTime==pausedTime&&ReferenceEquals(interactionSnapshot,sol.Presentation),$"Solar pause freezes authoritative evaluation: {pausedError}");sol.ApplyPresentationInput(camera,new NativeInputState{PauseToggle=1},out _,out _);Check(!sol.IsPaused,"Solar resume");
    var oldFocusedCenter=sol.FocusedBody.Position.Value;var oldOrientation=sol.FocusedBody.BodyFixedToRoot;var oldCameraOffset=camera.Position.Value-oldFocusedCenter;var oldCameraOrientation=camera.Orientation;var oldBodyFixedCameraDirection=oldOrientation.Conjugate().Normalized().Rotate(oldCameraOffset.Normalized());var orbitBuildsBefore=sol.OrbitCurveBuildCount;var orbitLocalBefore=sol.OrbitParentLocalSamples.ToArray();Check(sol.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),camera,out var advanceError),$"Solar dynamic publication: {advanceError}");Check(sol.CurrentTime==SimulationInstant.FromWholeSeconds(10)&&!ReferenceEquals(interactionSnapshot,sol.Presentation)&&sol.FocusedBody.Position.Value!=oldFocusedCenter&&sol.FocusedBody.BodyFixedToRoot!=oldOrientation,"Solar time advancement republishes evaluated positions and exact-epoch body orientations");var newCameraOffset=camera.Position.Value-sol.FocusedBody.Position.Value;var newBodyFixedCameraDirection=sol.FocusedBody.BodyFixedToRoot.Conjugate().Normalized().Rotate(newCameraOffset.Normalized());Check(Math.Abs(Math.Sqrt(newCameraOffset.LengthSquared)-sol.OrbitDistance)<1e-4&&Math.Sqrt((newCameraOffset-oldCameraOffset).LengthSquared)<1e-4&&camera.Orientation==oldCameraOrientation&&newBodyFixedCameraDirection!=oldBodyFixedCameraDirection,"focused camera follows translation with inertially stable offset while body longitude rotates underneath");Check(interactionSnapshot.Bodies[3].Position.Value==oldFocusedCenter,"prior immutable presentation remains unchanged");Check(!interactionOrbit.SequenceEqual(sol.OrbitRootSamples.ToArray())&&sol.OrbitCurveBuildCount==orbitBuildsBefore&&orbitLocalBefore.SequenceEqual(sol.OrbitParentLocalSamples.ToArray())&&sol.OrbitCurveReuseCount>0,"ordinary time advancement reuses immutable local orbit geometry while translating parent-relative presentation");
    var dynamicResult=CelestialSystemEvaluator.TryEvaluateSystem(system,sol.CurrentTime,evaluations,roots,staging,stagingRoots);Check(dynamicResult.Succeeded,"independent dynamic Sol evaluation");for(var index=0;index<sol.Presentation.Count;index++)Check(sol.Presentation.Bodies[index].Position.Value==roots[Array.FindIndex(Enumerable.Range(0,system.Count).ToArray(),candidate=>system.GetNodeInTraversalOrder(candidate).Id.Value==sol.Presentation.Bodies[index].BodyId)].Translation,$"dynamic body {index} matches authoritative evaluator");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var replay,out var replayError)&&replay is not null,$"Solar replay creation: {replayError}");var replayCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,camera.Projection,CameraMode.Free);Check(replay!.Focus(replayCamera,3),"Solar replay Earth focus");replay.ApplyPresentationInput(replayCamera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-30},out _,out _);replay.ApplyPresentationInput(replayCamera,new NativeInputState{MouseWheelDetents=1},out _,out _);for(var step=0;step<3;step++)replay.ApplyPresentationInput(replayCamera,new NativeInputState{RateIncrease=1},out _,out _);Check(replay.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),replayCamera,out replayError),$"Solar replay advance: {replayError}");Check(replay.CurrentTime==sol.CurrentTime&&replay.Presentation.Bodies.SequenceEqual(sol.Presentation.Bodies)&&replayCamera.Position.Value==camera.Position.Value&&replay.DistantBodies.SequenceEqual(sol.DistantBodies)&&replay.OrbitRootSamples.SequenceEqual(sol.OrbitRootSamples),"identical Solar controls produce identical time, snapshot, camera, orbit paths, and presentation batch");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var warp,out var warpError)&&warp is not null,$"Solar high-warp creation: {warpError}");var warpCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,warp!.Projection,CameraMode.Free);Check(warp.Focus(warpCamera,4)&&warp.FocusedBody.BodyId==SolarSystemBodyIds.Moon.Value,"Solar high-warp Moon focus");var warpOrbit=warp.OrbitRootSamples.ToArray();var warpLocalOrbit=warp.OrbitParentLocalSamples.ToArray();var warpOrbitBuilds=warp.OrbitCurveBuildCount;for(var step=warp.SpeedPresetIndex;step<SimulationSpeedPresets.Count-1;step++)warp.ApplyPresentationInput(warpCamera,new NativeInputState{RateIncrease=1},out _,out _);Check(warp.Rate==new SimulationRate(7_776_000,1),"Solar reaches 7,776,000x maximum preset");for(var step=0;step<64;step++)Check(warp.TryAdvanceByHostDuration(SimulationDuration.FromWholeSeconds(1),warpCamera,out warpError),$"Solar repeated 7,776,000x advancement {step}: {warpError}");Check(warp.CurrentTime==SimulationInstant.FromWholeSeconds(497_664_000)&&warp.Presentation.Bodies.ToArray().All(body=>body.Position.Value.IsFinite)&&warp.DistantBodies.Take(warp.DistantBodyCount).All(body=>float.IsFinite(body.CenterX)&&float.IsFinite(body.CenterY)&&float.IsFinite(body.CenterZ)),"Solar sustained 7,776,000x states and transport finite");var epochStaticMoonMismatch=Math.Sqrt((warpOrbit[moonPath]-warp.FocusedBody.Position.Value).LengthSquared);Check(Math.Abs(Math.Sqrt((warpCamera.Position.Value-warp.FocusedBody.Position.Value).LengthSquared)-warp.OrbitDistance)<1e-3&&!warpOrbit.SequenceEqual(warp.OrbitRootSamples.ToArray())&&warpLocalOrbit.SequenceEqual(warp.OrbitParentLocalSamples.ToArray())&&warp.OrbitCurveBuildCount==warpOrbitBuilds&&epochStaticMoonMismatch>1e9d&&warp.OrbitVertices.All(vertex=>float.IsFinite(vertex.X)&&float.IsFinite(vertex.Y)&&float.IsFinite(vertex.Z)),"Solar maximum-warp Moon focus follows corrected body authority while immutable local orbit geometry remains cached");
    Check(SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out var presetScene,out var presetError)&&presetScene is not null,$"Solar preset-input scene: {presetError}");var presetCamera=new CameraState(new FramePosition(root,new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu)),DoubleQuaternion.Identity,presetScene!.Projection,CameraMode.Free);presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateDecrease=1},out _,out _);for(var index=0;index<SimulationSpeedPresets.Count;index++){var expected=SimulationSpeedPresets.Get(index);Check(presetScene.SpeedPresetIndex==index&&presetScene.Rate==expected.Rate&&presetScene.SpeedHudLabel==expected.Label,$"Solar exact input preset {index}");if(index<SimulationSpeedPresets.Count-1)presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1},out _,out _);}presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1},out rateChanged,out _);Check(!rateChanged&&presetScene.SpeedPresetIndex==SimulationSpeedPresets.Count-1,"Solar maximum preset upper clamp");presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{RateIncrease=1,RateDecrease=1},out rateChanged,out _);Check(!rateChanged&&presetScene.SpeedPresetIndex==SimulationSpeedPresets.Count-1,"simultaneous rate inputs do not skip presets");presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{DeltaSeconds=.001f},out _,out _);var hudAllocationBefore=GC.GetAllocatedBytesForCurrentThread();for(var frame=0;frame<10_000;frame++)presetScene.ApplyPresentationInput(presetCamera,new NativeInputState{DeltaSeconds=.001f},out _,out _);var hudAllocated=GC.GetAllocatedBytesForCurrentThread()-hudAllocationBefore;Check(hudAllocated==0,"warmed simulation-speed HUD timer allocates zero bytes");
    Check(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out _,out _),"warm professional label layout");var labelAllocationBefore=GC.GetAllocatedBytesForCurrentThread();var labelStart=System.Diagnostics.Stopwatch.GetTimestamp();var labelProjectionCount=0;for(var frame=0;frame<100_000;frame++)if(SolarOverlayLayout.TryProjectLabel(earth,labelCameraA,true,out _,out _))labelProjectionCount++;var labelTicks=System.Diagnostics.Stopwatch.GetTimestamp()-labelStart;var labelAllocated=GC.GetAllocatedBytesForCurrentThread()-labelAllocationBefore;var labelNanoseconds=labelTicks*1_000_000_000d/System.Diagnostics.Stopwatch.Frequency/100_000d;var zoomAccumulator=nearStart;SolarCameraZoomPolicy.Apply(zoomAccumulator,earth.RadiusMetres,zoomMinimum,zoomMaximum,1);var zoomAllocationBefore=GC.GetAllocatedBytesForCurrentThread();var zoomStart=System.Diagnostics.Stopwatch.GetTimestamp();for(var frame=0;frame<100_000;frame++)zoomAccumulator=SolarCameraZoomPolicy.Apply(zoomAccumulator,earth.RadiusMetres,zoomMinimum,zoomMaximum,(frame&1)==0?1:-1);var zoomTicks=System.Diagnostics.Stopwatch.GetTimestamp()-zoomStart;var zoomAllocated=GC.GetAllocatedBytesForCurrentThread()-zoomAllocationBefore;var zoomNanoseconds=zoomTicks*1_000_000_000d/System.Diagnostics.Stopwatch.Frequency/100_000d;Check(labelProjectionCount==100_000&&labelAllocated==0&&zoomAllocated==0&&double.IsFinite(zoomAccumulator),"professional label layout and logarithmic camera policy allocate zero managed bytes after warmup");
    var warpEarth=warp.Presentation.Bodies[3];var warpAnchorLatitude=.25d;var warpAnchorLongitude=-1.1d;var warpAnchorDirection=BodyFixedGeography.DirectionFromLatitudeLongitude(warpAnchorLatitude,warpAnchorLongitude);var warpBodyLocal=warpAnchorDirection*(warpEarth.RadiusMetres+125d);var warpIdentityBefore=warpBodyLocal.Normalized();Check(CelestialBodyFixedFrameEvaluator.TryTransformBodyFixedPosition(SolarSystemBodyIds.Earth,warp.CurrentTime,warpBodyLocal,warpEarth.Position.Value,out var warpAnchorRoot)&&warpAnchorRoot==warpEarth.Position.Value+warpEarth.BodyFixedToRoot.Rotate(warpBodyLocal)&&warpIdentityBefore==warpBodyLocal.Normalized(),"maximum warp preserves Earth body-fixed point and terrain-v5 geographic identity");
    Console.WriteLine($"Earth-Moon evaluated separation: {earthMoon:R} m");Console.WriteLine($"Sun-Earth evaluated separation: {sunEarth:R} m; Earth-Neptune: {earthNeptune:R} m");Console.WriteLine($"Solar interaction proof: time={sol.CurrentTime.Ticks}; rate={sol.Rate.Numerator}:{sol.Rate.Denominator}; focus={sol.FocusedBody.Label}; distance={sol.OrbitDistance:R} m; corrected_epoch_static_mismatch={epochStaticMoonMismatch:R} m; HUD allocation={hudAllocated} bytes");Console.WriteLine($"11B label/camera cost: label={labelNanoseconds:F2} ns/update, zoom={zoomNanoseconds:F2} ns/update, allocations={labelAllocated+zoomAllocated} bytes");
}

static void EarthPlanetarySceneTest()
{
    var root=new ReferenceFrameId(1);Check(EarthPlanetaryScene.TryCreate(root,out var scene,out var error)&&scene is not null,$"Earth planetary scene: {error}");var earthScene=scene!;
    PlanetRenderProxy publishedEarth=default;var hasEarth=earthScene.Presentation.TryGetBody(SolarSystemBodyIds.Earth.Value,out publishedEarth);Check(earthScene.Presentation.Count==1&&hasEarth,"Earth snapshot publication");var earth=publishedEarth;Check(earth==earthScene.Earth,"Earth proxy identity");
    Check(SolAnalyticalDefinition.Instance.TryGetBody(SolarSystemBodyIds.Earth,out var catalogEarth)&&earth.RadiusMetres==catalogEarth.PhysicalProperties.MeanRadius&&earth.RadiusMetres==6_371_008.8d,"Earth catalog radius");
    var distanceFromSun=Math.Sqrt(earth.Position.Value.LengthSquared);Check(distanceFromSun>.9d*SolAnalyticalDefinition.AstronomicalUnitMetres&&distanceFromSun<1.1d*SolAnalyticalDefinition.AstronomicalUnitMetres,"evaluated SolAnalytical Earth position");
    Check(earth.Color==EarthPlanetaryScene.EarthColor&&earth.Label=="Earth"&&earth.Visible,"Earth presentation properties");var materialCamera=new CameraState(new FramePosition(root,earth.Position.Value+Double3.UnitZ*earth.RadiusMetres*20d),DoubleQuaternion.Identity,earthScene.Projection,CameraMode.Free);var earthMaterial=earthScene.NativePresentation(materialCamera);Check(earthMaterial.BodyIdLow==6&&earthMaterial.AlbedoSource==(uint)PlanetAlbedoSource.EarthAuthoritative&&earthMaterial.RingAssociation==0,"Earth scene uses the shared generic material transport");var earthLighting=earthScene.SolarLighting(materialCamera);Check(earthLighting.Enabled==1&&earthLighting.SourceCenterZ<0&&earthLighting.AmbientFloor is >0 and <.1f,"Earth lighting derives from evaluated Sun and camera-relative transport");
    Check(earthScene.Patches.Length==EarthPlanetaryScene.MaximumPatchCapacity&&EarthPlanetaryScene.RegionalMaximumLod==12,"Earth retains bounded shallow-global terrain-v5 storage");
    var camera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,earthScene.Projection,CameraMode.Free);var before=earthScene.Presentation.Bodies.ToArray();Check(earthScene.TryFocus(camera),"Earth focus");var focused=camera.Position.Value;var focusedDistance=Math.Sqrt((focused-earth.Position.Value).LengthSquared);Check(Math.Abs(focusedDistance-earthScene.OrbitDistance)<=earth.RadiusMetres*1e-8d,"Earth focus distance");
    Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DistantOnly&&earthScene.ActivePatchCount==0&&earthScene.Representation==PlanetaryRepresentation.FarFieldBody&&earthScene.DistantDrawCount==1&&earthScene.RepresentationBlend is{DistantAlpha:1,DetailedAlpha:0},"far Earth uses only the distant body renderer");
    SetDistance(15d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.Transition&&earthScene.ActivePatchCount>0&&earthScene.DistantDrawCount==1&&earthScene.RepresentationBlend.DistantAlpha>0&&earthScene.RepresentationBlend.DetailedAlpha>0,"transition renders distant and detailed representations");
    SetDistance(10d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DetailedOnly&&earthScene.ActivePatchCount>6&&earthScene.MaximumActiveLod>0&&earthScene.DistantDrawCount==0,"closer Earth uses detailed patches only");
    SetDistance(2d);Console.WriteLine($"Earth adaptive LOD: patches={earthScene.ActivePatchCount}; min={earthScene.MinimumActiveLod}; max={earthScene.MaximumActiveLod}; refined={earthScene.RefinementCount}; balanced={earthScene.BalancedRefinementCount}; culled={earthScene.CulledPatchCount}");Check(earthScene.ActivePatchCount>6&&earthScene.MaximumActiveLod>0,"near Earth refines the visible frustum");var closeLeaves=earthScene.ActiveLeaves.ToArray();var closePatches=earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount).ToArray();var closeHash=Hash(closePatches);Check(closeLeaves.Distinct().Count()==closeLeaves.Length,"active patch IDs unique");Check(!HasAncestorOverlap(closeLeaves),"no parent child overlap");Check(IsTraversalOrdered(closeLeaves),"deterministic child traversal order");var axialIndices=closeLeaves.Select((patch,index)=>(patch,index)).Where(item=>item.patch.Face==CubeSphereFace.PositiveZ&&Contains(item.patch,.5d,.5d)).ToArray();Check(axialIndices.Length>0&&axialIndices.Any(item=>closePatches[item.index].StitchMask==0),"near selection retains unstitched camera-axis surface coverage");
    var closest=closeLeaves.MinBy(patch=>PatchDistance(patch));var farthest=closeLeaves.MaxBy(patch=>PatchDistance(patch));Check(closest.Level>=farthest.Level,"nearby patch is never coarser than the farthest visible patch");var active=closeLeaves.ToHashSet();var edges=new[]{PlanetaryPatchEdge.NegativeU,PlanetaryPatchEdge.PositiveU,PlanetaryPatchEdge.NegativeV,PlanetaryPatchEdge.PositiveV};Check(closeLeaves.All(patch=>edges.All(edge=>PlanetaryRepresentationSelector.FindCoveringNeighbor(patch,edge,active) is not { } neighbor||patch.Level-neighbor.Level<=1)),"edge neighbor level difference at most one");Check(earthScene.MinimumActiveLod<earthScene.MaximumActiveLod?closePatches.Any(patch=>patch.StitchMask!=0):closePatches.All(patch=>patch.StitchMask==0),"stitch metadata matches mixed or uniform visible LOD");for(var index=0;index<closeLeaves.Length;index++){uint expectedMask=0;foreach(var edge in edges)if(PlanetaryRepresentationSelector.FindCoveringNeighbor(closeLeaves[index],edge,active) is { } neighbor&&neighbor.Level+1==closeLeaves[index].Level)expectedMask|=(uint)edge;Check(closePatches[index].StitchMask==expectedMask,"deterministic stitch metadata");}
    var closeRefined=earthScene.RefinementCount;var closeBalanced=earthScene.BalancedRefinementCount;var closeCulled=earthScene.CulledPatchCount;earthScene.UpdatePatches(camera);Check(closeLeaves.SequenceEqual(earthScene.ActiveLeaves.ToArray())&&closeHash==Hash(earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount))&&closeRefined==earthScene.RefinementCount&&closeBalanced==earthScene.BalancedRefinementCount&&closeCulled==earthScene.CulledPatchCount,"repeated camera state has deterministic leaves, balancing, metadata, and batch");
    SetAltitude(3_000_000d);var regionalLevel=earthScene.MaximumActiveLod;var regionalCount=earthScene.ActivePatchCount;Check(regionalCount>0&&regionalLevel<=EarthPlanetaryScene.RegionalMaximumLod,"3000 km retains complete terrain-v5 cube coverage");
    SetAltitude(1_500_000d);Check(earthScene.ActivePatchCount>0,"orbital descent retains the complete global parent");
    var proofAltitudes=new[]{1_000_000d,100_000d,10_000d,1_000d,100d,10d,EarthPlanetaryScene.MinimumTerrainClearanceMetres};for(var index=0;index<proofAltitudes.Length;index++){SetAltitude(proofAltitudes[index]);Check(earthScene.ActivePatchCount>0,"surface descent retains complete terrain-v5 coverage");}
    Console.WriteLine($"Earth production descent proof: global 3000km={regionalCount}/L{regionalLevel}; nearAltitudes={proofAltitudes.Length}; fallback=terrain-v5");
    SetDistance(20d);Check(earthScene.RepresentationBlend.Regime==PlanetaryRenderRegime.DistantOnly&&earthScene.ActivePatchCount>0&&earthScene.DistantDrawCount==1,"receding restores distant framing while preserving the persistent production cube cache");
    earthScene.ApplyPresentationInput(camera,new NativeInputState{LookActive=1,MouseDeltaX=100,MouseDeltaY=-50,MouseWheelDetents=1});Check(camera.Position.Value!=focused&&Math.Abs(Math.Sqrt((camera.Position.Value-earth.Position.Value).LengthSquared)-earthScene.OrbitDistance)<=earth.RadiusMetres*1e-8d,"Earth camera orbit");
    Check(before.SequenceEqual(earthScene.Presentation.Bodies.ToArray()),"Earth focus and orbit do not mutate presentation snapshot");var relative=CubeSphereProjection.CameraRelativeCenter(earth,new UniversePosition(camera.Position.Value,root));var nativePresentation=earthScene.NativePresentation(camera);Check(nativePresentation.CenterX==(float)relative.X&&nativePresentation.CenterY==(float)relative.Y&&nativePresentation.CenterZ==(float)relative.Z&&nativePresentation.Radius==(float)earth.RadiusMetres,"distant and detailed paths share camera-relative center and authoritative radius");Check(earthScene.Patches.AsSpan(0,earthScene.ActivePatchCount).ToArray().All(patch=>patch.CenterX==(float)relative.X&&patch.CenterY==(float)relative.Y&&patch.CenterZ==(float)relative.Z&&patch.Radius==(float)earth.RadiusMetres),"Earth camera-relative patch batch");
    earthScene.ResetPresentationCamera(camera);var resetRadial=(camera.Position.Value-earth.Position.Value).Normalized();var resetBodyRadial=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(resetRadial);var resetLighting=earthScene.SolarLighting(camera);var evaluatedSunDirection=new Double3(resetLighting.SourceCenterX,resetLighting.SourceCenterY,resetLighting.SourceCenterZ).Normalized();Check(Double3.Dot(resetRadial,evaluatedSunDirection)>.70d&&Math.Abs(resetBodyRadial.Y)<.75d&&Math.Abs(Math.Sqrt((camera.Position.Value-earth.Position.Value).LengthSquared)-earth.RadiusMetres*EarthPlanetaryScene.InitialOrbitDistanceRadii)<=earth.RadiusMetres*1e-8d,"Earth focus reset uses evaluated-Sun temperate body-fixed day-side presentation without changing celestial truth");
    Check(EarthPlanetaryScene.TryCreate(root,NativePlanetaryMode.GpuProduction,128,out var gpuScene,out var gpuError)&&gpuScene is not null,$"GPU Earth scene: {gpuError}");
    var gpuEarth=gpuScene!;var gpuCamera=new CameraState(new FramePosition(root,Double3.Zero),DoubleQuaternion.Identity,gpuEarth.Projection,CameraMode.Free);Check(gpuEarth.TryFocus(gpuCamera),"GPU Earth focus");gpuEarth.UpdatePatches(gpuCamera);
    var gpuConstants=gpuEarth.GpuConstants(gpuCamera);var gpuCameraBody=new Double3((double)gpuConstants.CameraBodyHighX+gpuConstants.CameraBodyLowX,(double)gpuConstants.CameraBodyHighY+gpuConstants.CameraBodyLowY,(double)gpuConstants.CameraBodyHighZ+gpuConstants.CameraBodyLowZ);var gpuRadius=(double)gpuConstants.RadiusHigh+gpuConstants.RadiusLow;
    Check(gpuEarth.Mode==NativePlanetaryMode.GpuProduction&&gpuEarth.ActivePatchCount==0&&gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==1,"distant GPU production delegates persistent cube-cache selection to native compute and emits one whole-body draw");
    var gpuLight=gpuEarth.SolarLighting(gpuCamera);var gpuSunDirection=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(new Double3(gpuLight.SourceCenterX,gpuLight.SourceCenterY,gpuLight.SourceCenterZ)).Normalized();var productionThreshold=(float)(EarthPlanetaryScene.RegionalLodConfiguration.MaximumProjectedPatchSpan*EarthPlanetaryScene.ProductionTargetPatchPixels/EarthPlanetaryScene.TargetPatchPixels);Check(Math.Abs(Math.Sqrt(gpuCameraBody.LengthSquared)-earth.RadiusMetres*EarthPlanetaryScene.InitialOrbitDistanceRadii)<1e-5&&Double3.Dot(gpuCameraBody.Normalized(),gpuSunDirection)>.70d&&Math.Abs(gpuCameraBody.Normalized().Y)<.75d&&Math.Abs(gpuRadius-earth.RadiusMetres)<1e-6&&gpuConstants.RefinementThreshold==productionThreshold&&gpuConstants.MaximumLevel==EarthPlanetaryScene.RegionalMaximumLod&&gpuConstants.OutputCapacity==128&&gpuConstants.TerrainVersion==PlanetaryTerrainDefinition.EarthProductionCubeV5.Version,"GPU production high/low precision, evaluated-Sun temperate focus, and production cube constants");
    SetGpuDistance(15d);Check(gpuEarth.RepresentationBlend.Regime==PlanetaryRenderRegime.Transition&&gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==1,"GPU transition enables compute and both renderers");
    var gpuPresentation=gpuEarth.NativePresentation(gpuCamera);gpuConstants=gpuEarth.GpuConstants(gpuCamera);gpuCameraBody=new((double)gpuConstants.CameraBodyHighX+gpuConstants.CameraBodyLowX,(double)gpuConstants.CameraBodyHighY+gpuConstants.CameraBodyLowY,(double)gpuConstants.CameraBodyHighZ+gpuConstants.CameraBodyLowZ);gpuRadius=(double)gpuConstants.RadiusHigh+gpuConstants.RadiusLow;var expectedRootCenter=earth.BodyFixedToRoot.Rotate(-gpuCameraBody);var transportedCenter=new Double3(gpuPresentation.CenterX,gpuPresentation.CenterY,gpuPresentation.CenterZ);var transportedOrientation=new DoubleQuaternion(gpuPresentation.BodyOrientationX,gpuPresentation.BodyOrientationY,gpuPresentation.BodyOrientationZ,gpuPresentation.BodyOrientationW);var orientationDot=transportedOrientation.X*earth.BodyFixedToRoot.X+transportedOrientation.Y*earth.BodyFixedToRoot.Y+transportedOrientation.Z*earth.BodyFixedToRoot.Z+transportedOrientation.W*earth.BodyFixedToRoot.W;Check(Math.Sqrt((transportedCenter-expectedRootCenter).LengthSquared)<=32d&&gpuPresentation.Radius==(float)gpuRadius&&gpuPresentation.DetailedAlpha==1f&&gpuPresentation.Regime==NativePlanetaryRenderRegime.DetailedOnly&&Math.Abs(orientationDot)>.999999d,"production cube input preserves body-fixed orientation, root-relative center, radius, and opaque sole-terrain ownership through the framing handoff");
    SetGpuDistance(10d);Check(gpuEarth.RepresentationBlend.Regime==PlanetaryRenderRegime.DetailedOnly&&gpuEarth.DetailedComputeRequested&&gpuEarth.DistantDrawCount==0,"GPU detailed-only suppresses distant draw");var roundTripTruth=gpuEarth.Presentation.Bodies.ToArray();for(var step=0;step<128&&gpuEarth.CameraPresentationMode!=PlanetaryCameraPresentationMode.SurfaceLocal;step++)gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MouseWheelDetents=1});Check(gpuEarth.CameraPresentationMode==PlanetaryCameraPresentationMode.SurfaceLocal&&gpuEarth.SurfaceFocus is { IsValid:true },"orbital descent enters reusable SurfaceLocal camera mode");var surfaceAnchor=gpuEarth.SurfaceFocus!.Value.TangentFrame.Direction;var surfaceOrientation=gpuCamera.Orientation;gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{LookActive=1,MouseDeltaX=80,MouseDeltaY=-40});var cameraBodyFixed=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(gpuCamera.Position.Value-earth.Position.Value).Normalized();Check(Double3.Dot(cameraBodyFixed,surfaceAnchor)>.999999d&&gpuCamera.Orientation!=surfaceOrientation,"SurfaceLocal look changes orientation while preserving body-fixed anchor");var truthBeforeTranslation=gpuEarth.Presentation.Bodies.ToArray();var translatedFrom=gpuEarth.SurfaceFocus!.Value;gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MoveForward=1,MoveRight=1,DeltaSeconds=.1f});var translatedTo=gpuEarth.SurfaceFocus!.Value;Check(translatedTo.TangentFrame.Direction!=translatedFrom.TangentFrame.Direction&&translatedTo.BodyId==translatedFrom.BodyId&&Math.Abs(Math.Sqrt((gpuCamera.Position.Value-earth.Position.Value).LengthSquared)-gpuEarth.OrbitDistance)<1e-5&&truthBeforeTranslation.SequenceEqual(gpuEarth.Presentation.Bodies.ToArray()),"SurfaceLocal tangent translation moves the body-fixed anchor without moving celestial Earth");gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MoveBackward=1,MoveLeft=1,DeltaSeconds=.1f});Check(Double3.Dot(gpuEarth.SurfaceFocus!.Value.TangentFrame.Direction,surfaceAnchor)>.999999999999d,"opposed SurfaceLocal translation is stable and reversible");
    var retainedRetreatRadial=gpuEarth.SurfaceFocus!.Value.TangentFrame.Direction;var previousRetreatOrientation=gpuCamera.Orientation;var maximumRetreatRadialError=0d;var maximumRetreatOrientationStep=0d;var detachSamples=0;
    for(var step=0;step<128&&gpuEarth.CameraPresentationMode!=PlanetaryCameraPresentationMode.Orbital;step++)
    {
        gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{MouseWheelDetents=-1});
        var retreatRadial=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(gpuCamera.Position.Value-earth.Position.Value).Normalized();
        maximumRetreatRadialError=Math.Max(maximumRetreatRadialError,Math.Acos(Math.Clamp(Double3.Dot(retreatRadial,retainedRetreatRadial),-1d,1d)));
        maximumRetreatOrientationStep=Math.Max(maximumRetreatOrientationStep,SurfaceCameraAuthority.QuaternionAngularError(previousRetreatOrientation,gpuCamera.Orientation));
        previousRetreatOrientation=gpuCamera.Orientation;detachSamples++;
    }
    var detachedRadial=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(gpuCamera.Position.Value-earth.Position.Value).Normalized();var detachedForward=gpuCamera.Orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();var detachedToCenter=(earth.Position.Value-gpuCamera.Position.Value).Normalized();
    Console.WriteLine($"Earth diagnostic detach raw: mode={gpuEarth.CameraPresentationMode}; focus={gpuEarth.SurfaceFocus.HasValue}; radialError={maximumRetreatRadialError:R}; centeredDot={Double3.Dot(detachedForward,detachedToCenter):R}; truth={roundTripTruth.SequenceEqual(gpuEarth.Presentation.Bodies.ToArray())}; yaw={gpuEarth.OrbitYawRadians:R}; pitch={gpuEarth.OrbitPitchRadians:R}");
    Check(gpuEarth.CameraPresentationMode==PlanetaryCameraPresentationMode.Orbital&&gpuEarth.SurfaceFocus is null&&maximumRetreatRadialError<2e-8d&&Double3.Dot(detachedForward,detachedToCenter)>1d-1e-12d&&roundTripTruth.SequenceEqual(gpuEarth.Presentation.Bodies.ToArray()),"SurfaceLocal-to-orbital retreat preserves one physical radial and finishes centered without changing celestial truth");
    var beforeOrbitalDrag=detachedRadial;gpuEarth.ApplyPresentationInput(gpuCamera,new NativeInputState{LookActive=1,MouseDeltaX=8,MouseDeltaY=-4});var afterOrbitalDrag=earth.BodyFixedToRoot.Conjugate().Normalized().Rotate(gpuCamera.Position.Value-earth.Position.Value).Normalized();var afterDragForward=gpuCamera.Orientation.Rotate(new Double3(0d,0d,-1d)).Normalized();var afterDragToCenter=(earth.Position.Value-gpuCamera.Position.Value).Normalized();var intendedDragAngle=Math.Acos(Math.Clamp(Double3.Dot(beforeOrbitalDrag,afterOrbitalDrag),-1d,1d));
    Check(intendedDragAngle is >0d and <.05d&&Double3.Dot(afterDragForward,afterDragToCenter)>1d-1e-12d&&Math.Abs(Math.Sqrt((gpuCamera.Position.Value-earth.Position.Value).LengthSquared)-gpuEarth.OrbitDistance)<1e-5,"first post-detach drag uses the transferred planet-centered orbit rather than a stale surface pivot");
    Console.WriteLine($"Earth diagnostic camera handoff: samples={detachSamples}; radialError={maximumRetreatRadialError:E3} rad; orientationStep={maximumRetreatOrientationStep:E3} rad; postDetachDrag={intendedDragAngle:E3} rad; centered={Math.Acos(Math.Clamp(Double3.Dot(afterDragForward,afterDragToCenter),-1d,1d)):E3} rad");
    Check(EarthPlanetaryScene.TryCreate(root,NativePlanetaryMode.CpuGpuValidation,EarthPlanetaryScene.MaximumPatchCapacity,out var validationScene,out var validationError)&&validationScene is not null&&validationScene.ActivePatchCount==0&&validationScene.Mode==NativePlanetaryMode.CpuGpuValidation&&validationScene.DetailedComputeRequested,$"CPU/GPU oracle delegates production cube selection to the native validation path: {validationError}");

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
    Check(Marshal.SizeOf<NativeEncodedPosition>()==32,"encoded size");
    Check(Marshal.SizeOf<NativeCameraData>()==96&&Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.Position)).ToInt32()==0&&Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.ViewProjection)).ToInt32()==32,"native camera layout");
    Check(Marshal.SizeOf<GpuCameraData>()==96,"GPU camera size");
    Check(Marshal.SizeOf<NativeRenderTransform>()==32&&Marshal.SizeOf<NativeRenderObject>()==80&&Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Mesh)).ToInt32()==64,"render object layout");
    Check(Marshal.SizeOf<NativeDrawBatch>()==16,"draw batch stride");
    Check(Marshal.SizeOf<NativePlanetaryGpuConstants>()==96&&Marshal.SizeOf<NativePlanetaryPresentation>()==192&&Marshal.SizeOf<NativeSolarLighting>()==48,"planetary presentation layout");
    Check(Marshal.SizeOf<NativeAnchoredSurfacePatch>()==80&&
        Marshal.SizeOf<NativeAnchoredSurfacePresentation>()==144,"dynamic hierarchy descriptor and shared-frame ABI");
    Check(Marshal.SizeOf<NativeProductionBillboardPupilFrame>()==160&&
        Marshal.SizeOf<NativeProductionBillboardFrame>()==480&&
        Marshal.SizeOf<NativeFrameSubmission>()==800&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryGpu)).ToInt32()==208&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryMode)).ToInt32()==304&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.PlanetaryPresentation)).ToInt32()==320&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.DistantBodies)).ToInt32()==512&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.SolarLighting)).ToInt32()==528&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.AnchoredSurfacePatches)).ToInt32()==576&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.AnchoredSurfacePatchCount)).ToInt32()==584&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.AnchoredSurfacePresentation)).ToInt32()==624&&
        Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.ProductionBillboard)).ToInt32()==768,
        "native frame ABI and dynamic hierarchy offsets");
    Check(Marshal.SizeOf<NativeInputState>()==84&&Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.PresentationFocus)).ToInt32()==72&&
        Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.ViewportWidthPixels)).ToInt32()==76&&
        Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.ViewportHeightPixels)).ToInt32()==80,"input layout");
    Check(NativeRuntime.GetAbiLayout(out var abi)==NativeResult.Success&&abi.InputStateSize==84&&abi.FrameSubmissionSize==800&&
        abi.FramePlanetaryGpuOffset==208&&abi.FramePlanetaryModeOffset==304&&abi.FramePlanetaryPresentationOffset==320&&abi.FrameSolarLightingOffset==528&&
        abi.InputViewportWidthOffset==76&&abi.InputViewportHeightOffset==80,
        "native frame ABI layout");
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
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var first, out var resolvedRoot, out _), "camera snapshot setup");Check(resolvedRoot.Value==state.Position.Value&&first.Position==default,"camera root remains managed FP64 authority while the reserved GPU translation field stays zero"); var hash = CameraHash(first); Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var second, out _, out _) && CameraHash(second) == hash, "camera snapshot deterministic result");
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
    return ComposeDecodedMicroNormal(macroNormal, PlanetDecodeBc5Normal(encodedMicroXY), localContribution, detailStrength);
}
static Vector3 ComposeDecodedMicroNormal(Vector3 macroNormal, Vector3 micro, float localContribution, float detailStrength)
{
    var up = Vector3.Normalize(macroNormal);
    micro = Vector3.Normalize(micro);
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
