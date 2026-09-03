using System.Diagnostics;using System.Reflection;using System.Runtime.InteropServices;using System.Security.Cryptography;using System.Text;
using NovaCore.Core;using NovaCore.Core.Camera;using NovaCore.Core.ReferenceFrames;using NovaCore.Core.Surface;using NovaCore.Graphics;using NovaCore.Interop;using NovaCore.Platform;using NovaCore.Simulation.Time;using NovaCore.Simulation.Celestial;
if(args.Contains("--scene=m12d-spherical-billboard-gpu-proof",StringComparer.OrdinalIgnoreCase))return RunSphericalBillboardGpuProof();
var productionBillboardCandidate=args.Contains("--scene=m12d-production-spherical-billboard",StringComparer.OrdinalIgnoreCase);
if(productionBillboardCandidate)
{
    var mapped=args.Where(value=>!value.StartsWith("--scene=",StringComparison.OrdinalIgnoreCase)).ToList();
    mapped.Add("--scene=sol");
    if(!mapped.Any(value=>value.StartsWith("--focus=",StringComparison.OrdinalIgnoreCase)))mapped.Add("--focus=earth");
    if(!mapped.Any(value=>value.StartsWith("--altitude=",StringComparison.OrdinalIgnoreCase)))mapped.Add("--altitude=700000");
    if(!mapped.Any(value=>value.StartsWith("--physical-surface=",StringComparison.OrdinalIgnoreCase)))mapped.Add("--physical-surface=m12d-natural-candidate");
    args=mapped.ToArray();
}
if(!SampleOptions.TryParse(args,out var options,out var error)){Console.Error.WriteLine(error);return 2;}if(!LogOptions.TryParse(options.LogArguments,out var log,out var logError)){Console.Error.WriteLine(logError);return 2;}return Run(options,log,productionBillboardCandidate);
static int RunSphericalBillboardGpuProof()
{
    try
    {
        var root=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(Environment.CurrentDirectory);
        Console.WriteLine("Active planetary representation: M12D Spherical Billboard GPU Proof (isolated, opt-in, sphere-only physical route, production patch renderer inactive).");
        var report=PlanetarySphericalBillboardGpuProof.Run(root,includeScaling:true);
        Console.WriteLine($"P2S3 topology library: artifacts=3; loadValidateSelectMs={report.TopologyLoadMilliseconds:F6}");
        foreach(var result in report.Levels)
        {
            var m=result.Frame;var upload=result.Upload;
            Console.WriteLine($"P2S3 proof: level={result.Level}; hash=0x{m.TopologyHash:X16}; vertices={m.BaseVertexCount}; triangles={m.BaseTriangleCount}; visible={m.VisibleTriangles}; backface={m.BackfaceRejected}; frustum={m.FrustumRejected}; invalid={m.InvalidRejected}; topologyUploads={m.TopologyUploadCount}; uploadBytes={upload.ActiveTopologyBytes}; uploadMs={upload.TopologyUploadMilliseconds:F6}; prepared={m.PreparedVertices}; cpuMs={m.CpuFrameMilliseconds:F6}; prepareMs={m.PreparationMilliseconds:F6}; normalMs={m.NormalMilliseconds:F6}; cullMs={m.CullingMilliseconds:F6}; compactMs={m.CompactionMilliseconds:F6}; compactedIndices={m.IndirectIndexCount}; indirectDraws={m.IndirectDrawCount}; drawMs={m.DrawMilliseconds:F6}; readiness={m.Readiness}; frameSlot={m.FrameSlot}; validation={m.ValidationErrors}; pixelChecksum=0x{m.PixelChecksum:X16}");
        }
        foreach(var result in report.Scaling)
        {
            var m=result.Metrics;
            Console.WriteLine($"P2S3 scaling: vertices={result.Vertices}; triangles={result.Triangles}; cpuMs={m.CpuFrameMilliseconds:F6}; preparationMs={m.PreparationMilliseconds:F6}; normalMs={m.NormalMilliseconds:F6}; cullingMs={m.CullingMilliseconds:F6}; compactionMs={m.CompactionMilliseconds:F6}; gpuTotalMs={m.GpuTotalMilliseconds:F6}; measured=true");
        }
        var final=report.FinalMetrics;
        Console.WriteLine($"P2S3 allocation: immutableVertices={final.ImmutableVertexBytes}; immutableIndices={final.ImmutableIndexBytes}; immutableAdjacency={final.ImmutableAdjacencyBytes}; framePositions={final.FramePositionBytes}; frameNormals={final.FrameNormalBytes}; frameVisibility={final.FrameVisibilityBytes}; frameCompactedIndices={final.FrameCompactedIndexBytes}; frameIndirect={final.FrameIndirectBytes}; frameCounters={final.FrameCounterBytes}; scratch={final.TemporaryScratchBytes}; total={final.TotalAllocatedBytes}");
        Console.WriteLine($"P2S3 lifecycle: topologyUploads={final.TopologyUploadCount}; topologyBytesUploaded={final.TopologyBytesUploaded}; frameOutputWrites={final.FrameOutputWriteCount}; cullingDispatches={final.CullingDispatchCount}; indirectSubmissions={final.IndirectSubmissionCount}; allocatedBytes={final.TotalAllocatedBytes}; runtimeTopologyGenerations={final.RuntimeTopologyGenerationCount}; validationErrors={final.ValidationErrors}.");
        Console.WriteLine("Active planetary representation: M12D Spherical Billboard Natural Terrain Proof (isolated, opt-in, canonical M12D physical generation 4, production patch renderer inactive).");
        var natural=PlanetarySphericalBillboardNaturalTerrainProof.Run(root);
        foreach(var level in natural.Levels)
        {
            var m=level.Frame;
            Console.WriteLine($"P2S4 proof: level={level.Level}; representation=spherical-billboard; physicalGeneration={m.PhysicalGeneration}; terrainDataGeneration={m.TerrainDataGeneration}; canonicalAuthority=PlanetaryPhysicalSurface.M12DNaturalTerrainCandidate; vertices={m.BaseVertexCount}; triangles={m.BaseTriangleCount}; physicalSamples={m.PreparedPhysicalSamples}; dispatchedCanonicalSamples={level.PreparedCanonicalSamples}; canonicalReuse={level.ReusedCanonicalSamples}; heightMax={level.MaximumCpuHeightErrorMetres:E17}m; normalMax={level.MaximumCpuNormalErrorRadians:E17}rad; visible={m.VisibleTriangles}; compactedIndices={m.IndirectIndexCount}; physicalPrepareMs={level.PhysicalPreparation.GpuMilliseconds:F6}; normalPublishMs={m.NormalMilliseconds:F6}; finalizeMs={m.PreparationMilliseconds:F6}; cullMs={m.CullingMilliseconds:F6}; compactMs={m.CompactionMilliseconds:F6}; drawMs={m.DrawMilliseconds:F6}; readiness={m.Readiness}; validation={m.ValidationErrors}");
        }
        Console.WriteLine($"P2S4 identity: uniqueCanonicalSamples={natural.UniqueCanonicalSamples}; crossLevelReusedSamples={natural.ReusedCanonicalSamples}; sharedHeightDelta={natural.MaximumSharedLevelHeightDeltaMetres:E17}m; sharedNormalDelta={natural.MaximumSharedLevelNormalDeltaRadians:E17}rad; patchIdentity=false; cameraIdentity=false; topologyOwnsPhysicalTruth=false.");
        return 0;
    }
    catch(Exception exception){Console.Error.WriteLine($"P2S3 spherical billboard GPU proof failed: {exception.Message}");return 1;}
}
static unsafe int Run(SampleOptions options,LogOptions log,bool productionBillboardCandidate)
{
    PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(options.PhysicalSurfaceGeneration);
    Console.WriteLine($"Physical surface authority: generation={(uint)options.PhysicalSurfaceGeneration}; mode={options.PhysicalSurfaceGeneration}.");
    string? terrainAssetPath=null,localTerrainAssetPath=null,elevationOraclePath=null,terrainAssetSha=null,localTerrainAssetSha=null;if(options.Scene is "earth" or "sol")
    {
        if(!TerrainAssetRepository.TryFindRoot(out var repositoryRoot)){Console.Error.WriteLine("NovaCore repository root could not be located for terrain asset resolution.");return 1;}
        if(!TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthAssetId,null,out var terrainManifest,out terrainAssetPath,out var assetError)){Console.Error.WriteLine(assetError);return 1;}terrainAssetSha=terrainManifest.Sha256;
        if(!TerrainAssetCache.TryResolveRequired(repositoryRoot,TerrainAssetCache.ProductionEarthLocalAssetId,null,out var localTerrainManifest,out localTerrainAssetPath,out var localAssetError)){localTerrainAssetPath=null;Console.WriteLine($"Local terrain refinement unavailable; coherent terrain-v5 base retained. {localAssetError}");}
        else if(!EarthLocalTerrainElevationDataset.TryLoad(localTerrainAssetPath,out var localElevationError)){localTerrainAssetPath=null;Console.WriteLine($"Local terrain refinement unavailable; its body-fixed clearance oracle could not be loaded. {localElevationError}");}
        else localTerrainAssetSha=localTerrainManifest.Sha256;
        elevationOraclePath=Path.Combine(AppContext.BaseDirectory,"earth-data","earth_elevation_8192x4096.r16");
        if(!File.Exists(elevationOraclePath))elevationOraclePath=Path.Combine(repositoryRoot,"assets","earth","runtime","earth_elevation_8192x4096.r16");
        if(!File.Exists(elevationOraclePath)){Console.Error.WriteLine($"Earth physical elevation oracle is unavailable: {elevationOraclePath}");return 1;}
    }
    var root=new ReferenceFrameId(1); var defaultStart=new UniversePosition(new Double3(4e12,-3e12,7e12),root);
    ResolvedRenderSnapshot? snapshot=null; FixtureSceneDiagnostics fixture=default; DynamicReferenceFrameFixtureScene? dynamic=null; CelestialAnalyticalScene? celestial=null; EarthPlanetaryScene? earth=null; SolarSystemScene? sol=null; PlanetaryDynamicAnchoredSurface? dynamicAnchored=null; var cameraPosition=defaultStart.Value;var cameraOrientation=DoubleQuaternion.Identity; var projection=new CameraProjection(Math.PI/3,16d/9,.01,1000); var movementSpeed=.1d;
    if(options.Scene=="earth")
    {
        if(!EarthPlanetaryScene.TryCreate(root,options.PlanetaryMode,options.GpuCapacity,options.PlanetSurfaceMode,out earth,out var earthError)||earth is null){Console.Error.WriteLine(earthError);return 1;}
        var source=new[]{new SampleRenderableState(new UniversePosition(Double3.Zero,root),DoubleQuaternion.Identity,Double3.Zero,MeshHandle.Triangle)};
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Earth renderer snapshot failed: {status}");return 1;}
        projection=earth.Projection;movementSpeed=earth.Earth.RadiusMetres*.01d;PrintEarthDiagnostics(earth);
        dynamicAnchored=new(earth.Earth.BodyId,earth.Earth.RadiusMetres,EarthPlanetaryScene.Terrain,(uint)options.PhysicalSurfaceGeneration);
    }
    else if(options.Scene=="sol")
    {
        var solarCreated=options.SolarJ2000?SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out sol,out var solError):SolarSystemScene.TryCreate(root,out sol,out solError);
        if(!solarCreated||sol is null){Console.Error.WriteLine(solError);return 1;}
        var source=new[]{new SampleRenderableState(new UniversePosition(Double3.Zero,root),DoubleQuaternion.Identity,Double3.Zero,MeshHandle.Triangle)};if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Solar renderer snapshot failed: {status}");return 1;}
        cameraPosition=new Double3(0,0,SolAnalyticalDefinition.AstronomicalUnitMetres*SolarSystemScene.InitialOverviewDistanceAu);projection=sol.Projection;
        var earthBody=default(PlanetRenderProxy);var earthFound=false;foreach(var body in sol.Presentation.Bodies)if(body.BodyId==SolarSystemBodyIds.Earth.Value){earthBody=body;earthFound=true;break;}if(!earthFound){Console.Error.WriteLine("Solar Earth body is unavailable.");return 1;}
        dynamicAnchored=new(earthBody.BodyId,earthBody.RadiusMetres,EarthPlanetaryScene.Terrain,(uint)options.PhysicalSurfaceGeneration);
        Console.WriteLine($"Scene: sol; bodies={sol.Presentation.Count}; instant={sol.CurrentTime.Ticks}; rate={sol.Rate.Numerator}:{sol.Rate.Denominator}; physical body radii plus screen-space markers; trajectory paths={SolarSystemScene.OrbitPathCount} x {SolarSystemScene.OrbitSegmentCount} segments.");Console.WriteLine(sol.StartupUtc is { } startupUtc?$"Solar startup: UTC={startupUtc:O}; SimulationInstant={sol.CurrentTime.Ticks} ticks ({sol.CurrentTime.SecondsSinceEpoch:R} ET seconds from J2000).":"Solar startup: deterministic J2000 ET0.");
        Console.WriteLine("Controls: LMB/RMB drag orbit or surface yaw/pitch, wheel zoom, E attach/detach surface-relative camera, surface WASD with Shift fast/Ctrl slow, 1-0 focus, ,/. rate, Space pause, R Solar Map/home.");
    }
    else if(options.Scene=="planetary-subdivision-diagnostic")
    {
        var source=new[]{new SampleRenderableState(new UniversePosition(Double3.Zero,root),DoubleQuaternion.Identity,Double3.Zero,MeshHandle.Triangle)};
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Planetary diagnostic snapshot failed: {status}");return 1;}
        cameraPosition=new Double3(0,0,4);movementSpeed=.5d;Console.WriteLine("Scene: planetary-subdivision-diagnostic; factor-4 canonical relaxed-cube density; normal terrain ownership disabled.");
    }
    else if(options.Scene=="fixture")
    {
        if(!StaticReferenceFrameFixtureSceneFactory.TryCreate(out var fixtureScene,out var fixtureError)){Console.Error.WriteLine(fixtureError);return 1;}
        snapshot=fixtureScene.Snapshot; fixture=fixtureScene.Diagnostics; var fixtureCamera=StaticReferenceFrameFixtureSceneFactory.Camera; cameraPosition=fixtureCamera.Position; projection=fixtureCamera.Projection; movementSpeed=fixtureCamera.MovementSpeed;
        PrintFixtureDiagnostics(fixture,log);
    }
    else if(options.Scene=="fixture-dynamic")
    {
        if(!DynamicReferenceFrameFixtureScene.TryCreate(out dynamic,out var dynamicDiagnostics,out var dynamicError)||dynamic is null){Console.Error.WriteLine(dynamicError);return 1;}
        snapshot=dynamic.CurrentSnapshot; fixture=dynamicDiagnostics.InitialFixture; var fixtureCamera=DynamicReferenceFrameFixtureScene.Camera; cameraPosition=fixtureCamera.Position; projection=fixtureCamera.Projection; movementSpeed=fixtureCamera.MovementSpeed;
        PrintDynamicFixtureDiagnostics(dynamicDiagnostics);
    }
    else if(options.Scene=="celestial")
    {
        if(!CelestialAnalyticalScene.TryCreate(out celestial,out var celestialError)||celestial is null){Console.Error.WriteLine(celestialError);return 1;}
        snapshot=celestial.CurrentSnapshot; var celestialCamera=CelestialAnalyticalScene.Camera; cameraPosition=celestialCamera.Position; projection=celestialCamera.Projection; movementSpeed=celestialCamera.MovementSpeed;
        PrintCelestialDiagnostics();
    }
    else
    {
        var source=options.Scene=="frames"?ReferenceFrameDemoSceneFactory.Create(defaultStart):SampleSceneFactory.Create(options.ObjectCount,defaultStart);
        if(!TryCreateSnapshot(source,out snapshot,out var status)){Console.Error.WriteLine($"Scene snapshot failed: {status}");return 1;}
    }
    var frames=new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root,null,ReferenceFrameKind.Ecl,"ECL"),CelestialFrameFactory.RootEcl())]);
    var planetaryPatches=earth?.Patches??(options.Scene=="planetary-subdivision-diagnostic"?CreateDiagnosticPatches():[]);var distantBodies=sol?.DistantBodies??[];
    var camera=new CameraState(new FramePosition(root,cameraPosition),cameraOrientation,projection,CameraMode.Free);
    sol?.ResetPresentationCamera(camera);
    if(sol is not null&&options.SurfaceSite=="florida-launch")
    {
        if(!sol.TryStartAtFloridaLaunchSite(camera)){Console.Error.WriteLine("Florida launch-site camera startup failed.");return 1;}
    }
    else if(sol is not null&&options.InitialFocus!=NativePresentationFocus.None)
    {
        var started=options.AltitudeMetres is { } solarAltitude
            ?options.InitialFocus==NativePresentationFocus.Earth&&sol.TryStartAtEarthValidationAltitude(camera,solarAltitude,options.SurfaceSite)
            :sol.Focus(camera,options.InitialFocus);
        if(!started){Console.Error.WriteLine("Solar focused camera startup failed.");return 1;}
    }
    if(earth is not null&&!earth.TryFocus(camera)){Console.Error.WriteLine("Earth camera focus failed.");return 1;}
    if(earth is not null&&options.AltitudeMetres is { } altitude)earth.SetValidationAltitude(camera,altitude,options.SurfaceSite);
    if(options.Scene is "earth" or "sol")PrintRuntimeFingerprint(terrainAssetPath,terrainAssetSha,localTerrainAssetPath,localTerrainAssetSha,elevationOraclePath);
    var objectCapacity=snapshot!.Count+(sol is null?0:1);var state=new HostState(camera,frames,new ReferenceFrameResolver(frames),snapshot!,new RenderFrameSubmission(objectCapacity,Math.Max(snapshot.OrbitCurve?.Count??0,sol?.OrbitVertices.Length??0)),log,movementSpeed,dynamic,celestial,earth,planetaryPatches,sol,dynamicAnchored,options.BenchmarkFrames,options.DynamicTraversal,options.SolarWarpTraversal,options.FixedNearSurfacePose,options.FixedNearSurfaceMotion,options.AltitudeMetres);
    state.DynamicTraversal?.ApplyPose(camera,earth!.Earth);
    state.FixedNearSurface?.ApplyPose(camera,earth!.Earth);
    sol?.EnforceFinalCameraInvariant(camera);
    UpdatePlanetaryPatches(state);
    if(!TryBuild(state,out var buildError)){Console.Error.WriteLine(buildError);return 1;}
    if(productionBillboardCandidate)
    {
        var repositoryRoot=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(Environment.CurrentDirectory);
        var levels=PlanetaryProductionSphericalBillboardTopologyLibrary.Load(Path.Combine(repositoryRoot,"assets","planetary-production-topology"));
        state.ProductionBillboardRuntime=new PlanetaryProductionSphericalBillboardMovingRuntime(repositoryRoot,levels);
        var gpu=sol?.GpuConstants(camera)??throw new InvalidOperationException("Production billboard requires the focused Earth Solar camera.");
        var cameraBody=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
        var candidateAltitude=Math.Max(10d,Math.Sqrt(cameraBody.LengthSquared)-PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres);
        var view=new PlanetaryProductionBillboardView(candidateAltitude,cameraBody.Normalized(),(int)state.ViewportWidthPixels,(int)state.ViewportHeightPixels,camera.Projection.VerticalFieldOfViewRadians,0);
        state.ProductionBillboardRuntime.Update(view,0);
        PlanetaryProductionBillboardPreparedGeneration initial;
        while(!state.ProductionBillboardRuntime.TrySubmitPrepared(out initial))Thread.Sleep(1);
        state.ProductionBillboardLease=new ProductionBillboardNativeLease(initial);
        Console.WriteLine($"P2S5C2 candidate prepared: interactive=true; level={initial.Topology.Level}; hash=0x{initial.Topology.TopologyHash:X16}; vertices={initial.Topology.Vertices.Count}; triangles={initial.Topology.TriangleCount}; prepared={initial.Physical.PreparedSamples}; reused={initial.Physical.ReusedSamples}; physicalGpuMs={initial.Physical.Metrics.GpuMilliseconds:F6}; heightParity={initial.Physical.MaximumCpuHeightErrorMetres:E9}m; normalParity={initial.Physical.MaximumCpuNormalErrorRadians:E9}rad; runtimeTopologyGeneration=0");
    }
    var objects=new NativeRenderObject[objectCapacity];var batches=new NativeDrawBatch[objectCapacity];var orbit=sol?.OrbitVertices??new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var previousOrbit=new NativeOrbitLineVertex[snapshot.OrbitCurve?.Count??0];var bodyForward=new NativeOrbitLineVertex[2];var targetDirection=new NativeOrbitLineVertex[2];var anchoredSurfacePatches=dynamicAnchored?.SubmissionPatches??[];var h=GCHandle.Alloc(state);
    CopyObjects(state.Submission,objects,batches,orbit,previousOrbit,bodyForward,targetDirection,out var batchCount);
    var terrainPathUtf8=terrainAssetPath is null?null:Encoding.UTF8.GetBytes(terrainAssetPath+'\0');var localTerrainPathUtf8=localTerrainAssetPath is null?null:Encoding.UTF8.GetBytes(localTerrainAssetPath+'\0');var elevationOraclePathUtf8=elevationOraclePath is null?null:Encoding.UTF8.GetBytes(elevationOraclePath+'\0');
    fixed(NativeRenderObject* po=objects)fixed(NativeDrawBatch* pb=batches)fixed(NativeOrbitLineVertex* pl=orbit)fixed(NativeOrbitLineVertex* pp=previousOrbit)fixed(NativeOrbitLineVertex* pf=bodyForward)fixed(NativeOrbitLineVertex* pt=targetDirection)fixed(NativePlanetaryPatch* px=planetaryPatches)fixed(NativePlanetaryPresentation* pd=distantBodies)fixed(NativeAnchoredSurfacePatch* pas=anchoredSurfacePatches)fixed(byte* terrainPath=terrainPathUtf8)fixed(byte* localTerrainPath=localTerrainPathUtf8)fixed(byte* oraclePath=elevationOraclePathUtf8){NativeFrameSubmission n=new(){Camera=NativeCamera(state.Submission.Camera),Objects=po,ObjectCount=(uint)state.Submission.ObjectCount,Batches=pb,BatchCount=(uint)batchCount,OrbitVertices=pl,OrbitVertexCount=(uint)(sol?.OrbitVertices.Length??state.Submission.OrbitVertexCount),PreviousOrbitVertices=pp,PreviousOrbitVertexCount=(uint)state.Submission.PreviousOrbitVertexCount,BodyForwardVertices=pf,BodyForwardVertexCount=(uint)state.Submission.BodyForwardVertexCount,TargetDirectionVertices=pt,TargetDirectionVertexCount=(uint)state.Submission.TargetDirectionVertexCount,PlanetaryPatches=px,PlanetaryPatchCount=ActivePlanetaryPatchCount(state),PlanetaryGpu=earth?.GpuConstants(camera)??sol?.GpuConstants(camera)??default,PlanetaryMode=earth?.Mode??(sol is null?NativePlanetaryMode.CpuReference:NativePlanetaryMode.GpuProduction),PlanetarySurfaceMode=NativeSurfaceMode(earth,sol),PhysicalSurfaceGeneration=(uint)options.PhysicalSurfaceGeneration,PlanetaryPresentation=earth?.NativePresentation(camera)??sol?.FocusedPresentation(camera)??default,DistantBodies=pd,DistantBodyCount=(uint)(sol?.DistantBodyCount??0),SolarLighting=earth?.SolarLighting(camera)??sol?.SolarLighting(camera)??default,AnchoredSurfacePatches=pas,AnchoredSurfacePatchCount=(uint)(dynamicAnchored?.ActivePatchCount??0),AnchoredSurfaceCacheSlotCount=(uint)(dynamicAnchored?.CacheCapacity??0),AnchoredSurfaceActiveGeneration=dynamicAnchored?.ActiveGeneration??0u,AnchoredSurfaceFlags=dynamicAnchored?.Visible==true?1u|(dynamicAnchored.RequiresGlobalFallback?2u:0u):0u,AnchoredSurfacePresentation=dynamicAnchored?.NativePresentation??default,ProductionBillboard=state.ProductionBillboardLease?.Pointer,ProductionBillboardFlags=productionBillboardCandidate?1u:0u};var runtimeAssets=new NativeRuntimeAssets{Size=(uint)sizeof(NativeRuntimeAssets),Version=3,ProductionTerrainPathUtf8=terrainPath,LocalTerrainPathUtf8=localTerrainPath,ElevationOraclePathUtf8=oraclePath};try{return terrainPath is null?NativeRuntime.RunRenderer(&n,Callback,GCHandle.ToIntPtr(h))==NativeResult.Success?0:1:NativeRuntime.RunRendererWithAssets(&n,Callback,GCHandle.ToIntPtr(h),&runtimeAssets)==NativeResult.Success?0:1;}finally{state.DisposeProductionBillboardLeases();h.Free();}}
}
static void PrintRuntimeFingerprint(string? terrainPath,string? terrainSha,string? localPath,string? localSha,string? oraclePath)
{
    var managed=Assembly.GetExecutingAssembly().Location;var native=Path.Combine(AppContext.BaseDirectory,"NovaCore.Native.dll");
    var productionShader=Path.Combine(AppContext.BaseDirectory,"shaders","planetary_production.frag.spv");var distantShader=Path.Combine(AppContext.BaseDirectory,"shaders","distant_planet.frag.spv");
    Console.WriteLine($"Runtime fingerprint: cwd={Environment.CurrentDirectory}; process={Environment.ProcessPath}; base={AppContext.BaseDirectory}");
    Print("managed",managed);Print("native-resolved",native);Print("production-fragment",productionShader);Print("distant-fragment",distantShader);
    Console.WriteLine($"Runtime terrain-v5: global={Path.GetFullPath(terrainPath!)}; sha256={terrainSha}; local={(localPath is null?"unavailable":Path.GetFullPath(localPath))}; localSha256={localSha??"unavailable"}; elevationOracle={(oraclePath is null?"unavailable":Path.GetFullPath(oraclePath))}; elevationOracleLoaded={EarthElevationDataset.IsLoaded}; elevationSha256={EarthElevationDataset.Sha256}");
    static void Print(string identity,string path){using var stream=File.OpenRead(path);Console.WriteLine($"Runtime {identity}: path={Path.GetFullPath(path)}; bytes={stream.Length}; sha256={Convert.ToHexStringLower(SHA256.HashData(stream))}");}
}
static bool TryCreateSnapshot(SampleRenderableState[] source,out ResolvedRenderSnapshot? snapshot,out ResolvedRenderSnapshotStatus status)
{
    var objects=new ResolvedRenderObject[source.Length]; for(var i=0;i<source.Length;i++){var value=source[i];objects[i]=new(new RenderObjectId((uint)(i+1)),value.Position,value.Rotation,value.Scale,value.Mesh);} return ResolvedRenderSnapshot.TryCreate(objects,out snapshot,out status);
}
static void UpdateDynamicAnchoredSurface(HostState state)
{
    var hierarchy=state.DynamicAnchored;if(hierarchy is null)return;
    var production=state.Earth is not null||state.Sol?.ProductionSurfaceEligible==true;
    if(!production){hierarchy.Deactivate();return;}
    var gpu=state.Earth?.GpuConstants(state.Camera,state.ViewportHeightPixels)??state.Sol!.GpuConstants(state.Camera,state.ViewportHeightPixels);
    var cameraBody=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,
        (double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
    var forward=new Double3(gpu.ViewForwardX,gpu.ViewForwardY,gpu.ViewForwardZ);
    if(!state.DynamicAnchoredProbeLogged&&RelaxedCubeSphereProjection.TryAddress(cameraBody,out var probeFace,out var probeU,out var probeV))
    {
        const int probeLevel=12;var cells=1<<probeLevel;var probeX=Math.Min((int)Math.Floor(probeU*cells),cells-1);var probeY=Math.Min((int)Math.Floor(probeV*cells),cells-1);
        Console.WriteLine($"Dynamic anchored local probe: face={probeFace}; level={probeLevel}; x={probeX}; y={probeY}; forwardToCenter={Double3.Dot(forward.Normalized(),-cameraBody.Normalized()):R}; residual={EarthLocalTerrainElevationDataset.SampleResidual(cameraBody):R}m; localSector={EarthLocalTerrainElevationDataset.Intersects(probeFace,probeLevel,probeX,probeY)}");
        state.DynamicAnchoredProbeLogged=true;
    }
    hierarchy.Update(cameraBody,forward,state.Camera.Projection.VerticalFieldOfViewRadians,
        state.Camera.Projection.AspectRatio,state.ViewportHeightPixels,ActiveSurfaceAnchor(state),
        state.Camera.Projection.NearClip);
}
static SurfaceAnchor? ActiveSurfaceAnchor(HostState state)
{
    // The continuous Vulkan traversal directly drives the physical camera across snap/cube/LOD
    // boundaries; its presentation reference is therefore the camera radial, not the scene's
    // retained interactive focus anchor.
    if(state.DynamicTraversal is not null)return null;
    Double3 direction;
    if(state.Sol?.SurfaceFocus is { } solarFocus)direction=solarFocus.BodyFixedDirection;
    else if(state.Earth?.SurfaceFocus is { } earthFocus)direction=earthFocus.BodyLocalAnchor.Normalized();
    else return null;
    var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    return SurfaceAnchor.TryCreate(6,new TerrainAuthorityVersion(terrain.SourceId,terrain.Version),
        direction.Normalized(),0d,out var anchor)==SurfaceAnchorCreationStatus.Success?anchor:null;
}
static void LogDynamicAnchoredSurface(HostState state)
{
    var hierarchy=state.DynamicAnchored;if(hierarchy is null)return;var telemetry=hierarchy.Telemetry;
    if(state.LastDynamicAnchoredGeneration==telemetry.ActiveGeneration&&
        state.LastDynamicAnchoredDemand==telemetry.DemandedPatchCount&&
        state.LastDynamicAnchoredVisible==hierarchy.Visible&&telemetry.FramePreparations==0&&
        telemetry.FrameCacheMisses==0&&telemetry.FrameEvictions==0&&telemetry.FrameUploadBytes==0)return;
    state.LastDynamicAnchoredGeneration=telemetry.ActiveGeneration;
    state.LastDynamicAnchoredDemand=telemetry.DemandedPatchCount;
    state.LastDynamicAnchoredVisible=hierarchy.Visible;
    var localDependencies=hierarchy.SubmissionPatches.Take(hierarchy.ActivePatchCount).Count(value=>(value.Flags&PlanetaryDynamicAnchoredSurface.SubmissionLocalPayloadRequired)!=0);
    var gpu=state.Earth?.GpuConstants(state.Camera,state.ViewportHeightPixels)??state.Sol!.GpuConstants(state.Camera,state.ViewportHeightPixels);var cameraBody=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
    Console.WriteLine($"Dynamic anchored surface: demanded={telemetry.DemandedPatchCount}; authoritative={telemetry.AuthoritativePatchCount}; pending={telemetry.PreparedPendingPatchCount}/{telemetry.PendingPatchCount}; resident={telemetry.ResidentPatchCount}/{telemetry.CacheCapacity}; levels={telemetry.MinimumLevel}-{telemetry.MaximumLevel}; nadirCovered={telemetry.NadirCovered}; localDependencies={localDependencies}; generation={telemetry.ActiveGeneration}; visible={hierarchy.Visible}; complete={telemetry.CompleteCoverage}; globalFallback={telemetry.GlobalFallbackActive}; projectedError={telemetry.MaximumProjectedErrorPixels:R}px; viewport={state.ViewportWidthPixels}x{state.ViewportHeightPixels}; retainedRadius={hierarchy.RetainedNeighborhoodRadiusMetres:R}m; residencyCenterDistance={hierarchy.CameraToResidencyCenterMetres:R}m; recenterThreshold={hierarchy.ResidencyRecenterDistanceMetres:R}m; rotationReuses={hierarchy.RotationReuseCount}; overlap={telemetry.DemandOverlapPercent:R}%; retained={telemetry.RetainedDemandedPatchCount}; new={telemetry.NewDemandedPatchCount}; released={telemetry.ReleasedDemandedPatchCount}; selectionMs={telemetry.LastSelectionMilliseconds:R}; prepareMs={telemetry.LastPreparationMilliseconds:R}; backgroundPrepareMs={telemetry.LastBackgroundPreparationMilliseconds:R}; frameHits={telemetry.FrameCacheHits}; frameMisses={telemetry.FrameCacheMisses}; frameEvictions={telemetry.FrameEvictions}; framePreparations={telemetry.FramePreparations}; frameUploadBytes={telemetry.FrameUploadBytes}; hits={telemetry.CacheHits}; misses={telemetry.CacheMisses}; evictions={telemetry.Evictions}; uploadBytes={telemetry.UploadBytes}; rejected={telemetry.RejectedGenerationReason}");
}
static void ApplyViewport(HostState state,in NativeInputState input)
{
    if(input.ViewportWidthPixels==0||input.ViewportHeightPixels==0)return;
    state.ViewportWidthPixels=input.ViewportWidthPixels;state.ViewportHeightPixels=input.ViewportHeightPixels;
    var aspect=input.ViewportWidthPixels/(double)input.ViewportHeightPixels;
    if(Math.Abs(state.Camera.Projection.AspectRatio-aspect)>1e-12d)
        state.Camera.Projection=state.Camera.Projection.WithAspect(aspect);
}
static bool TryBuild(HostState s,out string error)
{
    var cameraBuilt=s.Sol is null?CameraRenderSnapshotBuilder.TryBuildReversedZ(s.Camera,s.Resolver,s.Frames.RootId,out var camera,out var rootPosition,out var cameraStatus):CameraRenderSnapshotBuilder.TryBuildReversedInfiniteFar(s.Camera,s.Resolver,s.Frames.RootId,out camera,out rootPosition,out cameraStatus);if(!cameraBuilt){error=$"Camera snapshot failed: {cameraStatus}";return false;}
    var status=ResolvedRenderSubmissionBuilder.TryBuild(s.Snapshot,camera,rootPosition,s.Submission); if(status!=ResolvedRenderSubmissionBuildStatus.Success){error=$"Resolved submission failed: {status}";return false;}
    if(s.Sol?.TryGetFloridaLaunchSitePresentation(s.Camera,out var sitePosition,out var siteOrientation)==true)
    {
        s.Submission.Add(sitePosition,siteOrientation,new Double3(1d,1d,1d),MeshHandle.FloridaLaunchPad);
        s.Submission.Complete();
    }
    error=string.Empty;return true;
}
static unsafe void Callback(NativeHostEvent* e,IntPtr data)
{
    var s=(HostState)GCHandle.FromIntPtr(data).Target!;if(e->Type==NativeHostEventType.Diagnostic){var c=(LogCategory)e->LogCategory;if(c is LogCategory.None or LogCategory.Validation||s.Log.IsEnabled(c))Console.WriteLine($"[native] {Marshal.PtrToStringUTF8((IntPtr)e->Utf8Message)}");return;}s.DynamicAnchored?.AcknowledgeGpuGeneration(e->Submission->AnchoredSurfaceGpuReadyGeneration);
    var celestialScene=s.Celestial;var earthScene=s.Earth;var orbitCamera=celestialScene is not null||earthScene is not null||s.Sol is not null;Span<CameraCommand> commands=stackalloc CameraCommand[4];var count=DebugCameraInput.Map(e->Input,commands,!orbitCamera,!orbitCamera);var previousSpeed=s.Controller.MovementSpeed;
    if(s.Dynamic is not null){var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!s.Dynamic.TryAdvanceByHostDuration(duration,out var publicationError)&&!s.DynamicFailureLogged){Console.Error.WriteLine(publicationError);s.DynamicFailureLogged=true;}else s.Snapshot=s.Dynamic.CurrentSnapshot;}
    if(celestialScene is not null){celestialScene.ApplyPresentationInput(s.Camera,e->Input,out var rateChanged,out var pauseChanged);var previousSasMode=celestialScene.SasMode;var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)e->Input.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!celestialScene.TryAdvanceByHostDuration(duration,e->Input,out var publicationError)&&!s.CelestialFailureLogged){Console.Error.WriteLine(publicationError);s.CelestialFailureLogged=true;}else s.Snapshot=celestialScene.CurrentSnapshot;if(s.Log.IsEnabled(LogCategory.Camera)&&rateChanged)Console.WriteLine($"Celestial rate={celestialScene.Rate.Numerator}:{celestialScene.Rate.Denominator}");if(s.Log.IsEnabled(LogCategory.Camera)&&pauseChanged)Console.WriteLine($"Celestial paused={celestialScene.IsPaused}");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Celestial zoom distance={celestialScene.OrbitDistance:R}");if(s.Log.IsEnabled(LogCategory.Camera)&&previousSasMode!=celestialScene.SasMode)Console.WriteLine($"Celestial SAS mode={celestialScene.SasMode}");LogSasIndicatorDiagnostics(s,celestialScene);}
    if(s.FixedNearSurface is null)earthScene?.ApplyPresentationInput(s.Camera,e->Input);
    if(s.Sol is not null){var solarScene=s.Sol;var solarInput=s.SolarWarpTraversal?.PrepareInput(e->Input,solarScene)??e->Input;solarScene.ApplyPresentationInput(s.Camera,solarInput,out var rateChanged,out var pauseChanged);var duration=SimulationDuration.FromSecondsRounded(Math.Clamp((double)solarInput.DeltaSeconds,0d,FreeCameraController.MaximumDeltaSeconds));if(!solarScene.TryAdvanceByHostDuration(duration,s.Camera,out var publicationError)&&!s.SolarFailureLogged){Console.Error.WriteLine(publicationError);s.SolarFailureLogged=true;}if(s.Log.IsEnabled(LogCategory.Camera)&&rateChanged)Console.WriteLine($"Solar rate={solarScene.Rate.Numerator}:{solarScene.Rate.Denominator}");if(s.Log.IsEnabled(LogCategory.Camera)&&pauseChanged)Console.WriteLine($"Solar paused={solarScene.IsPaused}");if(s.Log.IsEnabled(LogCategory.Camera)&&solarInput.MouseWheelDetents!=0)Console.WriteLine($"Solar orbit distance={solarScene.OrbitDistance:R} m");if(solarScene.Focus(s.Camera,solarInput.PresentationFocus))Console.WriteLine($"Solar focus: {solarScene.FocusedBody.Label}; id={solarScene.FocusedBody.BodyId}; radius={solarScene.FocusedBody.RadiusMetres:R} m; distance/radius={solarScene.FocusedBlend.DistanceRadii:R}; state={solarScene.FocusedBlend.Regime}");LogSolarTelemetry(s,solarScene);}
    if(e->Input.Reset!=0){s.Camera.Position=s.Default.Position;s.Camera.Orientation=s.Default.Orientation;s.Camera.Projection=s.Default.Projection;s.Camera.Mode=s.Default.Mode;s.Controller.Reset();celestialScene?.ResetPresentationCamera(s.Camera);earthScene?.ResetPresentationCamera(s.Camera);s.Sol?.ResetPresentationCamera(s.Camera);}else if(!orbitCamera)s.Controller.Update(s.Camera,commands[..count],e->Input.DeltaSeconds);
    ApplyViewport(s,e->Input);
    s.DynamicTraversal?.ApplyPose(s.Camera,earthScene!.Earth);
    s.FixedNearSurface?.ApplyPose(s.Camera,earthScene!.Earth);
    s.Sol?.EnforceFinalCameraInvariant(s.Camera);
    UpdateDynamicAnchoredSurface(s);
    LogDynamicAnchoredSurface(s);
    if(s.DynamicTraversal is { } traversal&&traversal.Observe(s.DynamicAnchored!))
    {
        Console.WriteLine(traversal.FinalReport);
        PostQuitMessage(traversal.Failed?1:0);
    }
    UpdatePlanetaryPatches(s);LogEarthLodDiagnostics(s,earthScene);
    if(s.Sol is { } observedSolar&&s.SolarWarpTraversal?.Observe(observedSolar)==true){Console.WriteLine(s.SolarWarpTraversal.FinalReport);PostQuitMessage(s.SolarWarpTraversal.Failed?1:0);}
    if(!TryBuild(s,out var buildError)){Console.Error.WriteLine(buildError);PostQuitMessage(1);return;} CopyObjectsToNative(s.Submission,e->Submission->Objects,e->Submission->Batches,e->Submission->OrbitVertices,e->Submission->PreviousOrbitVertices,e->Submission->BodyForwardVertices,e->Submission->TargetDirectionVertices,out var batchCount);e->Submission->ObjectCount=(uint)s.Submission.ObjectCount;e->Submission->BatchCount=(uint)batchCount;e->Submission->OrbitVertexCount=(uint)(s.Sol?.OrbitVertices.Length??s.Submission.OrbitVertexCount);e->Submission->PreviousOrbitVertexCount=(uint)s.Submission.PreviousOrbitVertexCount;e->Submission->BodyForwardVertexCount=(uint)s.Submission.BodyForwardVertexCount;e->Submission->TargetDirectionVertexCount=(uint)s.Submission.TargetDirectionVertexCount;e->Submission->PlanetaryPatchCount=ActivePlanetaryPatchCount(s);e->Submission->PlanetaryGpu=earthScene?.GpuConstants(s.Camera,s.ViewportHeightPixels)??s.Sol?.GpuConstants(s.Camera,s.ViewportHeightPixels)??default;e->Submission->PlanetaryMode=earthScene?.Mode??(s.Sol is null?NativePlanetaryMode.CpuReference:NativePlanetaryMode.GpuProduction);e->Submission->PlanetarySurfaceMode=NativeSurfaceMode(earthScene,s.Sol);e->Submission->PhysicalSurfaceGeneration=(uint)PlanetaryPhysicalSurface.RuntimeGeneration;e->Submission->PlanetaryPresentation=earthScene?.NativePresentation(s.Camera)??s.Sol?.FocusedPresentation(s.Camera)??default;e->Submission->DistantBodyCount=(uint)(s.Sol?.DistantBodyCount??0);e->Submission->SolarLighting=earthScene?.SolarLighting(s.Camera)??s.Sol?.SolarLighting(s.Camera)??default;e->Submission->AnchoredSurfacePatchCount=(uint)(s.DynamicAnchored?.ActivePatchCount??0);e->Submission->AnchoredSurfaceActiveGeneration=s.DynamicAnchored?.ActiveGeneration??0u;e->Submission->AnchoredSurfaceFlags=s.DynamicAnchored?.ActivePatchCount>0?1u|(s.DynamicAnchored.RequiresGlobalFallback?2u:0u):0u;e->Submission->AnchoredSurfacePresentation=s.DynamicAnchored?.NativePresentation??default;e->Submission->Camera=NativeCamera(s.Submission.Camera);UpdateProductionBillboard(s,e->Submission);
    if(!orbitCamera&&s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Camera wheelDetents={e->Input.MouseWheelDetents} speed={previousSpeed:R}->{s.Controller.MovementSpeed:R}");if(earthScene is not null&&s.Log.IsEnabled(LogCategory.Camera)&&e->Input.MouseWheelDetents!=0)Console.WriteLine($"Earth orbit distance={earthScene.OrbitDistance:R} m");if(s.Log.IsEnabled(LogCategory.Camera)&&e->Input.Reset!=0)Console.WriteLine($"Camera reset speed={s.Controller.MovementSpeed:R}");if(s.BenchmarkFramesRemaining>0&&--s.BenchmarkFramesRemaining==0){if(s.FixedNearSurface is { } fixedPose)Console.WriteLine(fixedPose.FinalReport);if(s.Sol is { } benchmarkSolar)Console.WriteLine($"Solar workload: bodies={benchmarkSolar.DistantBodyCount}; representation={(benchmarkSolar.ProductionSurfaceEligible?"production":"bounded-sphere")}; dynamicTerrain={benchmarkSolar.DetailedComputeRequested}; orbitCurveBuilds={benchmarkSolar.OrbitCurveBuildCount}; orbitCurveReuses={benchmarkSolar.OrbitCurveReuseCount}; orbitCacheEntries={benchmarkSolar.OrbitCacheKeys.Length}");PostQuitMessage(0);}
}
static unsafe void UpdateProductionBillboard(HostState state,NativeFrameSubmission* submission)
{
    var runtime=state.ProductionBillboardRuntime;if(runtime is null)return;
    var gpu=submission->PlanetaryGpu;var cameraBody=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
    if(!cameraBody.IsFinite||cameraBody.LengthSquared<=0d)return;
    var altitude=Math.Max(10d,Math.Sqrt(cameraBody.LengthSquared)-PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres);
    var telemetry=runtime.Update(new(altitude,cameraBody.Normalized(),(int)state.ViewportWidthPixels,(int)state.ViewportHeightPixels,state.Camera.Projection.VerticalFieldOfViewRadians,state.ProductionBillboardFrame++),submission->ProductionBillboardPadding);
    if(state.ProductionBillboardLease is { } acknowledged&&submission->ProductionBillboardPadding==unchecked((uint)acknowledged.Generation)){acknowledged.Dispose();state.ProductionBillboardLease=null;}
    if(state.ProductionBillboardLease is null&&runtime.TrySubmitPrepared(out var prepared))
    {
        state.ProductionBillboardLease=new(prepared);
        Console.WriteLine($"P2S5C2 replacement: generation={prepared.PublicationGeneration}; level={prepared.Topology.Level}; pupil={prepared.Pupil.Generation}; active={prepared.Reuse.ActiveSamples}; reused={prepared.Reuse.ReusedSamples}; new={prepared.Reuse.NewSamples}; reuse={prepared.Reuse.ReusePercent:F3}%; prepareMs={prepared.PreparationMilliseconds:F6}; topologyUpload={(prepared.TopologyUploadRequired?1:0)}");
    }
    submission->ProductionBillboard=state.ProductionBillboardLease?.Pointer;
    submission->ProductionBillboardFlags=1u;
    if(state.LastProductionBillboardPublication!=telemetry.Publications){state.LastProductionBillboardPublication=telemetry.Publications;Console.WriteLine($"P2S5C2 publication: count={telemetry.Publications}; current=L{telemetry.CurrentLevel}; pupilError={telemetry.PupilAngularErrorRadians:E9}rad; topologyUploads={telemetry.TopologyUploads}; owners=1; zeroOwner={telemetry.ZeroOwnerFrames}; overlap={telemetry.OverlapOwnerFrames}; stale={telemetry.StaleGenerationDraws}");}
}
static void PrintFixtureDiagnostics(in FixtureSceneDiagnostics fixture,LogOptions log)
{
    Console.WriteLine("Scene: fixture");Console.WriteLine("Static hierarchy: ECL Star -> CCE Planet -> CCI Moon -> CCF TestVessel");Console.WriteLine($"Resolved roots: Star={fixture.Star.Value}; Planet={fixture.Planet.Value}; Moon={fixture.Moon.Value}; TestVessel={fixture.Vessel.Value}");Console.WriteLine("Objects: 4; batches: 1");Console.WriteLine($"Fixture render setup hash: 0x{fixture.SetupHash:X16}");Console.WriteLine("Static reference-frame render fixture; no propagation or orbital simulation.");
    if(log.IsEnabled(LogCategory.Precision))Console.WriteLine($"Fixture root frame: {fixture.RootFrame.Value}");
}
static void PrintDynamicFixtureDiagnostics(in DynamicFixtureDiagnostics diagnostics)
{
    Console.WriteLine("Scene: fixture-dynamic");Console.WriteLine("Prescribed deterministic transform motion; no gravity or orbital simulation.");Console.WriteLine($"Moon: radius={DynamicReferenceFrameFixtureScene.MoonRadius:R}, rate={DynamicReferenceFrameFixtureScene.MoonAngularRate:R} rad/s; Vessel: radius={DynamicReferenceFrameFixtureScene.VesselRadius:R}, rate={DynamicReferenceFrameFixtureScene.VesselAngularRate:R} rad/s");Console.WriteLine("Initial controlled time: 0 ticks");Console.WriteLine($"Initial resolved roots: Star={diagnostics.InitialFixture.Star.Value}; Planet={diagnostics.InitialFixture.Planet.Value}; Moon={diagnostics.InitialFixture.Moon.Value}; TestVessel={diagnostics.InitialFixture.Vessel.Value}");Console.WriteLine($"Dynamic scripted-sequence hash: 0x{diagnostics.ScriptedSequenceHash:X16}");Console.WriteLine("Dynamic publication allocations are measured by NovaCore.Graphics.Tests; warmed frame assembly remains allocation-free.");
}
static void PrintCelestialDiagnostics()
{
    Console.WriteLine("Scene: celestial");Console.WriteLine("Clock-driven analytical two-body marker fixture; no prescribed orbital phase.");Console.WriteLine($"Presentation: 1 display unit = {CelestialAnalyticalScene.MetresPerDisplayUnit:R} m; clock rate={CelestialAnalyticalScene.SampleRate}:1.");Console.WriteLine("Scheduled inertial impulse: +200 m/s tangential at 100000 simulated seconds.");Console.WriteLine("Controls: LMB/RMB drag orbit, wheel zoom, WASD/QE manual torque, 1-7 SAS select, 0 SAS off, ,/. rate, Space pause, R camera reset.");Console.WriteLine("SAS torque control is suspended above 10:1; use , to reduce the rate before automatic pointing.");Console.WriteLine("Objects: central marker + satellite marker; triangle geometry is presentation-only.");
}
static void PrintEarthDiagnostics(EarthPlanetaryScene scene)
{
    Console.WriteLine("Scene: earth; SolAnalytical Earth through PlanetaryPresentationSnapshot.");
    Console.WriteLine($"Earth: body={scene.Earth.BodyId}; radius={scene.Earth.RadiusMetres:R} m; root position=({scene.Earth.Position.Value.X:R}, {scene.Earth.Position.Value.Y:R}, {scene.Earth.Position.Value.Z:R}) m.");
    Console.WriteLine($"Production renderer: body-fixed hierarchical relaxed cube-sphere; shared patch grid={PlanetaryPatchTopology.QuadsPerSide}x{PlanetaryPatchTopology.QuadsPerSide}; maximum level={EarthPlanetaryScene.RegionalMaximumLod}.");
    Console.WriteLine($"Representation handoff: detailed <= {EarthPlanetaryScene.HandoffConfiguration.DetailedOnlyMaximumDistanceRadii:R} radii; distant >= {EarthPlanetaryScene.HandoffConfiguration.DistantOnlyMinimumDistanceRadii:R} radii; hysteresis={EarthPlanetaryScene.HandoffConfiguration.HysteresisRadii:R} radii.");
    Console.WriteLine($"Planetary selection mode: {scene.Mode}.");
    Console.WriteLine($"Production terrain mode: 2; terrain version: {PlanetaryTerrainDefinition.EarthProductionCubeV5.Version}; topology=0x{PlanetaryProductionPatchTopology.Shared.DeterministicHash:X16}.");
    Console.WriteLine("Controls: LMB/RMB look/orbit, wheel zoom, W/S forward/back and A/D strafe in SurfaceLocal, R Earth focus reset.");
}
static void LogSasIndicatorDiagnostics(HostState state,CelestialAnalyticalScene scene)
{
    if(!state.Log.IsEnabled(LogCategory.Camera))return;
    var target=state.Snapshot.TargetDirectionIndicator;var valid=target.HasValue;var now=Stopwatch.GetTimestamp();
    var transitioned=!state.HasSasIndicatorDiagnostic||valid!=state.LastSasTargetValid;var due=valid&&now>=state.NextSasIndicatorDiagnosticTimestamp;
    if(!transitioned&&!due)return;
    state.HasSasIndicatorDiagnostic=true;state.LastSasTargetValid=valid;state.NextSasIndicatorDiagnosticTimestamp=checked(now+Stopwatch.Frequency);
    if(!valid){Console.WriteLine($"Celestial SAS target invalid or hidden ({scene.SasMode}).");return;}
    var body=state.Snapshot.BodyForwardIndicator!.Value;var bodyDirection=(body.End.Value-body.Start.Value).Normalized();var targetDirection=(target!.Value.End.Value-target.Value.Start.Value).Normalized();var angle=Math.Acos(Math.Clamp(Double3.Dot(bodyDirection,targetDirection),-1d,1d));Console.WriteLine($"Celestial SAS target valid; angular error={angle:R} rad.");
}
static void LogSolarTelemetry(HostState state,SolarSystemScene scene)
{
    if(!state.Log.IsEnabled(LogCategory.Camera))return;var now=Stopwatch.GetTimestamp();if(now<state.NextSolarDiagnosticTimestamp)return;state.NextSolarDiagnosticTimestamp=checked(now+Stopwatch.Frequency);var effectiveRate=scene.IsPaused?"0 (paused)":$"{scene.Rate.Numerator}:{scene.Rate.Denominator}";Console.WriteLine($"Solar instant={scene.CurrentTime.Ticks}; rate={effectiveRate}; mode={scene.CameraPresentationMode}; focus={scene.FocusedBody.Label}; distance={scene.OrbitDistance:R} m; distance/radius={scene.FocusedBlend.DistanceRadii:R}; overlays=orbits:{scene.VisibleOrbitCount},labels:{scene.VisibleLabelCount},markers:{scene.VisibleMarkerCount}");
}
[DllImport("user32.dll")] static extern void PostQuitMessage(int exitCode);
static unsafe void CopyObjects(RenderFrameSubmission s,NativeRenderObject[] o,NativeDrawBatch[] b,NativeOrbitLineVertex[] l,NativeOrbitLineVertex[] p,NativeOrbitLineVertex[] f,NativeOrbitLineVertex[] t,out int count){fixed(NativeRenderObject* po=o)fixed(NativeDrawBatch* pb=b)fixed(NativeOrbitLineVertex* pl=l)fixed(NativeOrbitLineVertex* pp=p)fixed(NativeOrbitLineVertex* pf=f)fixed(NativeOrbitLineVertex* pt=t)CopyObjectsToNative(s,po,pb,pl,pp,pf,pt,out count);}
static unsafe void CopyObjectsToNative(RenderFrameSubmission s,NativeRenderObject* o,NativeDrawBatch* b,NativeOrbitLineVertex* l,NativeOrbitLineVertex* p,NativeOrbitLineVertex* f,NativeOrbitLineVertex* t,out int count){for(var i=0;i<s.ObjectCount;i++){var x=s.Objects[i];o[i]=new(){Position=NativePosition(x.Position),Transform=new(){RotationX=x.Transform.Rotation.X,RotationY=x.Transform.Rotation.Y,RotationZ=x.Transform.Rotation.Z,RotationW=x.Transform.Rotation.W,ScaleX=x.Transform.Scale.X,ScaleY=x.Transform.Scale.Y,ScaleZ=x.Transform.Scale.Z},Mesh=new(){Value=x.Mesh.Value}};}count=s.BatchCount;for(var i=0;i<count;i++){var x=s.Batches[i];b[i]=new(){Mesh=new(){Value=x.Mesh.Value},FirstObject=x.FirstObject,ObjectCount=x.ObjectCount};}for(var i=0;i<s.OrbitVertexCount;i++){var x=s.OrbitVertices[i];l[i]=new(){X=x.X,Y=x.Y,Z=x.Z,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ};}for(var i=0;i<s.PreviousOrbitVertexCount;i++){var x=s.PreviousOrbitVertices[i];p[i]=new(){X=x.X,Y=x.Y,Z=x.Z,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ};}for(var i=0;i<s.BodyForwardVertexCount;i++){var x=s.BodyForwardVertices[i];f[i]=new(){X=x.X,Y=x.Y,Z=x.Z,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ};}for(var i=0;i<s.TargetDirectionVertexCount;i++){var x=s.TargetDirectionVertices[i];t[i]=new(){X=x.X,Y=x.Y,Z=x.Z,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ};}}
static NativeEncodedPosition NativePosition(EncodedPosition x,float highPadding=0,float lowPadding=0)=>new(){HighX=x.HighX,HighY=x.HighY,HighZ=x.HighZ,HighPadding=highPadding,LowX=x.LowX,LowY=x.LowY,LowZ=x.LowZ,LowPadding=lowPadding};static NativeCameraData NativeCamera(GpuCameraData c)=>new(){Position=NativePosition(c.Position,c.PositionHighPadding,c.PositionLowPadding),ViewProjection=new(){C0R0=c.ViewProjection.C0R0,C0R1=c.ViewProjection.C0R1,C0R2=c.ViewProjection.C0R2,C0R3=c.ViewProjection.C0R3,C1R0=c.ViewProjection.C1R0,C1R1=c.ViewProjection.C1R1,C1R2=c.ViewProjection.C1R2,C1R3=c.ViewProjection.C1R3,C2R0=c.ViewProjection.C2R0,C2R1=c.ViewProjection.C2R1,C2R2=c.ViewProjection.C2R2,C2R3=c.ViewProjection.C2R3,C3R0=c.ViewProjection.C3R0,C3R1=c.ViewProjection.C3R1,C3R2=c.ViewProjection.C3R2,C3R3=c.ViewProjection.C3R3}};
static NativePlanetaryPatch[] CreateDiagnosticPatches(){var colors=new[]{(1f,.2f,.2f),(.2f,1f,.2f),(.2f,.4f,1f),(1f,1f,.2f),(1f,.2f,1f),(.2f,1f,1f)};var result=new NativePlanetaryPatch[6];for(uint face=0;face<6;face++){var color=colors[face];result[face]=new(){Face=face,Level=0,X=0,Y=0,Radius=1,ColorR=color.Item1,ColorG=color.Item2,ColorB=color.Item3,ColorA=-1};}return result;}
static void UpdatePlanetaryPatches(HostState state){if(state.Earth is not null){state.Earth.UpdatePatches(state.Camera);return;}if(state.Sol is not null){state.Sol.Update(state.Camera);return;}if(state.PlanetaryPatches.Length==0)return;var relative=Double3.Zero-state.Camera.Position.Value;foreach(ref var patch in state.PlanetaryPatches.AsSpan()){patch.CenterX=(float)relative.X;patch.CenterY=(float)relative.Y;patch.CenterZ=(float)relative.Z;}}
static uint ActivePlanetaryPatchCount(HostState state)=>(uint)(state.Earth?.ActivePatchCount??state.PlanetaryPatches.Length);
static NativePlanetarySurfaceMode NativeSurfaceMode(EarthPlanetaryScene? earth,SolarSystemScene? sol)=>
    earth is not null||sol?.ProductionSurfaceEligible==true
        ?NativePlanetarySurfaceMode.ProductionCubeSphere
        :NativePlanetarySurfaceMode.Bounded;
static void LogEarthLodDiagnostics(HostState state,EarthPlanetaryScene? earth)
{
    if(earth is null)return;var blend=earth.RepresentationBlend;
    if(state.HasEarthLodDiagnostic&&state.LastEarthPatchCount==earth.ActivePatchCount&&state.LastEarthMinimumLod==earth.MinimumActiveLod&&state.LastEarthMaximumLod==earth.MaximumActiveLod&&state.LastEarthRepresentation==earth.Representation&&state.LastEarthRegime==blend.Regime)return;
    state.HasEarthLodDiagnostic=true;state.LastEarthPatchCount=earth.ActivePatchCount;state.LastEarthMinimumLod=earth.MinimumActiveLod;state.LastEarthMaximumLod=earth.MaximumActiveLod;state.LastEarthRepresentation=earth.Representation;state.LastEarthRegime=blend.Regime;
    Console.WriteLine($"Earth representation: state={blend.Regime}; metric={blend.DistanceRadii:R}; distantAlpha={blend.DistantAlpha:R}; detailedAlpha={blend.DetailedAlpha:R}; dynamicHierarchy=true; detailedPatches={earth.ActivePatchCount}; distantDraws={earth.DistantDrawCount}");
    Console.WriteLine($"Earth LOD: patches={earth.ActivePatchCount}; min={earth.MinimumActiveLod}; max={earth.MaximumActiveLod}; refined={earth.RefinementCount}; splits={earth.SplitPatchCount}; merges={earth.MergedPatchCount}; balanced={earth.BalancedRefinementCount}; culled={earth.CulledPatchCount}; frustum={earth.FrustumCulledPatchCount}; horizon={earth.HorizonCulledPatchCount}; parentFallbacks=0; pendingChildren=0; representation={earth.Representation}; altitude/radius={earth.AltitudeRadii:R}; surfaceAltitude={earth.AltitudeMetres:R} m; surfaceFrameBlend={earth.SurfaceFrameBlend:R}");
    var presentation=earth.NativePresentation(state.Camera);var lighting=earth.SolarLighting(state.Camera);var radial=(state.Camera.Position.Value-earth.Earth.Position.Value).Normalized();var light=new Double3(lighting.SourceCenterX-presentation.CenterX,lighting.SourceCenterY-presentation.CenterY,lighting.SourceCenterZ-presentation.CenterZ).Normalized();Console.WriteLine($"Earth presentation: radial/light={Double3.Dot(radial,light):R}; elevationLoaded={EarthElevationDataset.IsLoaded}; terrainSource={EarthPlanetaryScene.Terrain.SourceId}/{EarthPlanetaryScene.Terrain.Version}");
}
file sealed class HostState(CameraState camera,ReferenceFrameSnapshot frames,ReferenceFrameResolver resolver,ResolvedRenderSnapshot snapshot,RenderFrameSubmission submission,LogOptions log,double movementSpeed,DynamicReferenceFrameFixtureScene? dynamic,CelestialAnalyticalScene? celestial,EarthPlanetaryScene? earth,NativePlanetaryPatch[] planetaryPatches,SolarSystemScene? sol,PlanetaryDynamicAnchoredSurface? dynamicAnchored,int benchmarkFrames,bool dynamicTraversal,bool solarWarpTraversal,bool fixedNearSurfacePose,bool fixedNearSurfaceMotion,double? fixedNearSurfaceAltitude){public CameraState Camera=camera;public CameraState Default=new(new FramePosition(camera.Position.Frame,camera.Position.Value),camera.Orientation,camera.Projection,camera.Mode);public ReferenceFrameSnapshot Frames=frames;public ReferenceFrameResolver Resolver=resolver;public ResolvedRenderSnapshot Snapshot=snapshot;public RenderFrameSubmission Submission=submission;public LogOptions Log=log;public FreeCameraController Controller=new(movementSpeed,.002,Math.PI*89/180);public DynamicReferenceFrameFixtureScene? Dynamic=dynamic;public CelestialAnalyticalScene? Celestial=celestial;public EarthPlanetaryScene? Earth=earth;public SolarSystemScene? Sol=sol;public PlanetaryDynamicAnchoredSurface? DynamicAnchored=dynamicAnchored;public DynamicAnchoredVulkanTraversal? DynamicTraversal=dynamicTraversal?new():null;public FixedNearSurfaceVulkanBenchmark? FixedNearSurface=fixedNearSurfacePose?new(camera,earth!.Earth,fixedNearSurfaceAltitude!.Value,fixedNearSurfaceMotion):null;public SolarWarpVulkanTraversal? SolarWarpTraversal=solarWarpTraversal?new():null;public NativePlanetaryPatch[] PlanetaryPatches=planetaryPatches;public int BenchmarkFramesRemaining=benchmarkFrames;public bool DynamicFailureLogged;public bool CelestialFailureLogged;public bool SolarFailureLogged;public bool HasSasIndicatorDiagnostic;public bool LastSasTargetValid;public long NextSasIndicatorDiagnosticTimestamp;public long NextSolarDiagnosticTimestamp;public bool HasEarthLodDiagnostic;public int LastEarthPatchCount;public int LastEarthMinimumLod;public int LastEarthMaximumLod;public PlanetaryRepresentation LastEarthRepresentation;public PlanetaryRenderRegime LastEarthRegime;public uint LastDynamicAnchoredGeneration=uint.MaxValue;public int LastDynamicAnchoredDemand=-1;public bool LastDynamicAnchoredVisible;public bool DynamicAnchoredProbeLogged;public uint ViewportWidthPixels=(uint)Math.Round(camera.Projection.AspectRatio*EarthPlanetaryScene.ProofViewportHeightPixels);public uint ViewportHeightPixels=(uint)EarthPlanetaryScene.ProofViewportHeightPixels;public PlanetaryProductionSphericalBillboardMovingRuntime? ProductionBillboardRuntime;public ProductionBillboardNativeLease? ProductionBillboardLease;public ulong ProductionBillboardFrame;public ulong LastProductionBillboardPublication;public void DisposeProductionBillboardLeases(){ProductionBillboardLease?.Dispose();ProductionBillboardLease=null;}}
file sealed unsafe class ProductionBillboardNativeLease:IDisposable
{
    private GCHandle _lattice,_indices,_physical;private IntPtr _submission;public ulong Generation{get;}
    public NativeProductionSphericalBillboardSubmission* Pointer=>(NativeProductionSphericalBillboardSubmission*)_submission;
    public ProductionBillboardNativeLease(PlanetaryProductionBillboardPreparedGeneration generation)
    {
        Generation=generation.PublicationGeneration;_lattice=GCHandle.Alloc(generation.Lattice,GCHandleType.Pinned);_indices=GCHandle.Alloc(generation.Indices,GCHandleType.Pinned);_physical=GCHandle.Alloc(generation.Physical.Vertices,GCHandleType.Pinned);
        var value=new NativeProductionSphericalBillboardSubmission{Size=(uint)sizeof(NativeProductionSphericalBillboardSubmission),Version=1,Enabled=1,Level=(uint)generation.Topology.Level,VertexCount=(uint)generation.Lattice.Length,IndexCount=(uint)generation.Indices.Length,LatticeScale=(uint)generation.Topology.LatticeScale,PhysicalGeneration=PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration,TerrainDataGeneration=PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration,PupilGeneration=generation.Pupil.Generation,Reserved0=generation.TopologyUploadRequired?1u:0u,TopologyHash=generation.Topology.TopologyHash,PublicationGeneration=generation.PublicationGeneration,LatticeVertices=(NativeProductionBillboardLatticeVertex*)_lattice.AddrOfPinnedObject(),Indices=(uint*)_indices.AddrOfPinnedObject(),PhysicalVertices=(NativeSphericalBillboardPhysicalVertex*)_physical.AddrOfPinnedObject()};
        _submission=Marshal.AllocHGlobal(sizeof(NativeProductionSphericalBillboardSubmission));Marshal.StructureToPtr(value,_submission,false);
    }
    public void Dispose(){if(_submission!=IntPtr.Zero){Marshal.FreeHGlobal(_submission);_submission=IntPtr.Zero;}if(_lattice.IsAllocated)_lattice.Free();if(_indices.IsAllocated)_indices.Free();if(_physical.IsAllocated)_physical.Free();}
}
file readonly record struct SampleOptions(int ObjectCount,string Scene,NativePlanetaryMode PlanetaryMode,uint GpuCapacity,double? AltitudeMetres,int BenchmarkFrames,string SurfaceSite,NativePresentationFocus InitialFocus,bool SolarJ2000,PlanetarySurfaceRendererMode PlanetSurfaceMode,PlanetaryPhysicalSurfaceGeneration PhysicalSurfaceGeneration,bool DynamicTraversal,bool SolarWarpTraversal,bool FixedNearSurfacePose,bool FixedNearSurfaceMotion,string[] LogArguments){public static bool TryParse(string[] a,out SampleOptions v,out string? e){var n=100;var scene="grid";var mode=NativePlanetaryMode.GpuProduction;var surfaceMode=PlanetarySurfaceRendererMode.ProductionCubeSphere;var physicalGeneration=PlanetaryPhysicalSurfaceGeneration.Generation3;uint capacity=EarthPlanetaryScene.MaximumPatchCapacity;double? altitude=null;var benchmarkFrames=0;var surfaceSite="land";var initialFocus=NativePresentationFocus.None;var solarJ2000=false;var dynamicTraversal=false;var solarWarpTraversal=false;var fixedNearSurfacePose=false;var fixedNearSurfaceMotion=false;var logs=new List<string>();foreach(var x in a){if(x.StartsWith("--objects=")&&(!int.TryParse(x[10..],out n)||n is not(1 or 100 or 1000 or 10000))){v=default;e="Usage: --objects=1|100|1000|10000";return false;}if(x.StartsWith("--scene="))scene=x[8..];else if(x.StartsWith("--planetary-mode=")){mode=x[17..] switch{"cpu"=>NativePlanetaryMode.CpuReference,"gpu"=>NativePlanetaryMode.GpuProduction,"validate"=>NativePlanetaryMode.CpuGpuValidation,_=>(NativePlanetaryMode)uint.MaxValue};}else if(x.StartsWith("--planet-surface=")){surfaceMode=x[17..] switch{"production"=>PlanetarySurfaceRendererMode.ProductionCubeSphere,_=>(PlanetarySurfaceRendererMode)uint.MaxValue};}else if(x.StartsWith("--physical-surface=")){physicalGeneration=x[19..] switch{"generation-3"=>PlanetaryPhysicalSurfaceGeneration.Generation3,"m12d-natural-candidate"=>PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate,_=>(PlanetaryPhysicalSurfaceGeneration)uint.MaxValue};}else if(x.StartsWith("--gpu-capacity=")&&!uint.TryParse(x[15..],out capacity)){v=default;e="GPU capacity must be an integer.";return false;}else if(x.StartsWith("--altitude=")&&(!double.TryParse(x[11..],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var parsedAltitude)||parsedAltitude<EarthPlanetaryScene.MinimumTerrainClearanceMetres)){v=default;e="Altitude must be at least the terrain-safe floor.";return false;}else if(x.StartsWith("--altitude="))altitude=double.Parse(x[11..],System.Globalization.CultureInfo.InvariantCulture);else if(x.StartsWith("--surface-site="))surfaceSite=x[15..];else if(x=="--focus=earth")initialFocus=NativePresentationFocus.Earth;else if(x.StartsWith("--focus=")){v=default;e="Focus must be earth for the current Solar startup contract.";return false;}else if(x=="--solar-epoch=j2000")solarJ2000=true;else if(x.StartsWith("--solar-epoch=")){v=default;e="Solar epoch must be current or j2000.";return false;}else if(x=="--dynamic-traversal")dynamicTraversal=true;else if(x=="--solar-warp-traversal")solarWarpTraversal=true;else if(x=="--m12c-fixed-pose")fixedNearSurfacePose=true;else if(x=="--m12c-fixed-pose-motion")fixedNearSurfaceMotion=true;else if(x.StartsWith("--benchmark-frames=")&&(!int.TryParse(x[19..],out benchmarkFrames)||benchmarkFrames<2)){v=default;e="Benchmark frames must be at least 2.";return false;}else logs.Add(x);}var earthSite=surfaceSite is "land"or"ocean"or"slope"or"local-payload"or"arid"or"temperate"or"rock"or"snow"or"fallback"or"florida";var floridaSite=surfaceSite=="florida-launch"&&scene=="sol";if(scene is not("grid"or"frames"or"fixture"or"fixture-dynamic"or"celestial"or"planetary-subdivision-diagnostic"or"earth"or"sol")||mode>NativePlanetaryMode.CpuGpuValidation||surfaceMode!=PlanetarySurfaceRendererMode.ProductionCubeSphere||!Enum.IsDefined(physicalGeneration)||capacity is 0 or >EarthPlanetaryScene.MaximumPatchCapacity||altitude.HasValue&&scene!="earth"&&(scene!="sol"||initialFocus!=NativePresentationFocus.Earth)||initialFocus!=NativePresentationFocus.None&&scene!="sol"||dynamicTraversal&&scene!="earth"||solarWarpTraversal&&scene!="sol"||fixedNearSurfacePose&&(scene!="earth"||!altitude.HasValue||benchmarkFrames==0)||fixedNearSurfaceMotion&&!fixedNearSurfacePose||!earthSite&&!floridaSite){v=default;e="Usage: --scene=earth [--physical-surface=generation-3|m12d-natural-candidate] --altitude=metres [--benchmark-frames=count]";return false;}v=new(n,scene,mode,capacity,altitude,benchmarkFrames,surfaceSite,initialFocus,solarJ2000,surfaceMode,physicalGeneration,dynamicTraversal,solarWarpTraversal,fixedNearSurfacePose,fixedNearSurfaceMotion,logs.ToArray());e=null;return true;}}
