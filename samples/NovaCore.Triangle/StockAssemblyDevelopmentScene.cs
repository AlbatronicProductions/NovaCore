using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

/// <summary>Ordinary application adapter for a registered stock assembly. Reusable asset presentation consumes copied canonical state only.</summary>
internal sealed class StockAssemblyDevelopmentScene : IDisposable
{
    private readonly AssemblyApplicationSession session;
    private AssemblyFlightObservation observation;
    private readonly CompiledAssemblyDesign design;
    private readonly bool supportedContact;
    private readonly bool poweredSupport;
    private readonly bool floridaSupport;
    private readonly DoubleQuaternion[] partRotations;
    private readonly (int Part,PartVisualMesh Mesh)[] presentedMeshes;
    internal ReusablePartVisuals Visuals {get;}
    internal int RenderCapacity=>presentedMeshes.Length+design.Jets.Length+1;
    internal FloridaContactPresentation? FloridaView {get;}
    private readonly double[] frameMilliseconds=new double[40000];
    private readonly double[] serviceMilliseconds=new double[40000];
    private readonly char[] title=new char[384];
    private int titledFrontier=-1;
    private bool titledStarted,titledCompleted,titledFailed;
    private long sequence,timestamp,remainder;
    private int frameCount;
    private bool started,reported;
    internal bool Completed {get;private set;}
    internal bool Failed {get;private set;}
    internal AssemblyFlightStatus Failure {get;private set;}
    internal AssemblyFlightObservation Observation=>observation;
    // A terminal observation is historical. It is not an instruction to keep firing FX.
    internal bool ActiveExhaust=>started&&!Completed&&!Failed;
    internal ResolvedRenderSnapshot InitialSnapshot {get;}
    internal StockAssemblyDevelopmentScene(string stockId="novacore.stock.SRV01.FourHorn",bool supportedContact=false,bool poweredSupport=false,bool floridaSupport=false,SolarSystemScene? solarWorld=null)
    {
        if(poweredSupport&&!supportedContact)throw new ArgumentException("Powered support requires the contact route.");
        if(floridaSupport&&(!supportedContact||poweredSupport))throw new ArgumentException("Florida qualification requires stock engine-OFF contact.");
        this.supportedContact=supportedContact;
        this.poweredSupport=poweredSupport;
        this.floridaSupport=floridaSupport;
        var preparationStart=Stopwatch.GetTimestamp();
        var d=AssemblyStockCatalog.LoadDefault().Resolve(stockId);
        if(!d.HasIndependentBlockJets)throw new InvalidDataException("The four-horn visual requires the separately qualified sixteen-jet definition.");
        design=d;
        // Fully recorded input schedule, selected before instantiation. No display-frame commands.
        var plan=new AssemblyCommand[128];
        for(var i=0;i<plan.Length;i++)plan[i]=i<32?new(true,null,i<16?0:.025,0,15625):new(false,d.Data.Design.Pairs[(i-32)/16].Name,0,0,15625);
        var definition=new SpacecraftDefinition(new(201),new(1),new(2),"Registered reference assembly");
        var launch=floridaSupport?AssemblyLaunch.CreateFloridaSupported(d,definition,"florida-slab",PrepareFlorida(solarWorld?.CurrentTime??SimulationInstant.Zero)):
            poweredSupport?AssemblyLaunch.CreatePoweredSupported(AssemblyContactProfile.Create(d),definition,"powered-supported-01"):
            supportedContact?AssemblyLaunch.CreateSupported(AssemblyContactProfile.Create(d),definition,"reference-launch-01"):
            new AssemblyLaunch(d,definition,"reference-launch-01",new(default,default,DoubleQuaternion.Identity,default),default,plan);
        session=supportedContact?AssemblyApplicationSession.CreateSupported(launch):AssemblyApplicationSession.Create(launch);Observe();
        try
        {
            if(floridaSupport)FloridaView=new(session.Launch.Site!,session.Launch.Spacecraft.CarrierFrame,solarWorld);
            partRotations=d.Parts.Select(p=>Rotation(p.Instance.Pose.Rotation)).ToArray();
            Visuals=new(Path.Combine(AppContext.BaseDirectory,"assets","visual","SRV01"));
            presentedMeshes=d.Parts.SelectMany((p,i)=>Visuals.Resolve(p.Definition.VisualReference).Meshes.Select(m=>(i,m))).ToArray();
            ValidateBindings();
            var objects=new ResolvedRenderObject[presentedMeshes.Length+(supportedContact?1:0)];for(var i=0;i<presentedMeshes.Length;i++)objects[i]=PresentedPart(i);
            if(FloridaView is { } view){objects[^1]=view.Slab();}
            else if(supportedContact)objects[^1]=SupportSlab();
            if(!ResolvedRenderSnapshot.TryCreate(objects,out var snapshot,out _))throw new InvalidDataException("Assembly presentation refused.");
            InitialSnapshot=snapshot!;
        }
        catch{Visuals?.Dispose();session.Dispose();throw;}
        Console.WriteLine("STOCK_ASSEMBLY_READY design="+d.Data.Design.Id+" digest="+d.Digest+(supportedContact?" intervals=1200 cadence_hz=60 supported_contact=true":" intervals=128 cadence_hz=64")+" max_service=4 parts=7 physical_jets=16 visual_mesh_instances="+presentedMeshes.Length+" shared_gpu_meshes="+Visuals.UniqueMeshCount+" mesh_buffer_bytes="+Visuals.BufferBytes);
        Console.WriteLine(floridaSupport?(FloridaView!.SolarNavigation?
            "Florida Solar world: authenticated site, one finite authored support slab, stock SRV-01 centred on top. Engine/RCS OFF. The bounded contact owner starts after preparation and holds its final site-relative endpoint after 20 seconds; Solar exploration, celestial time and camera controls remain independent presentation owners. No launch or departure. Project Control manual support/presentation acceptance PASS; production shadows deferred.":
            "Stock SRV-01 Florida support slab: authenticated terrain-v5 site, one finite authored base/footing slab, centred physical support on its top. Earth-fixed display and lighting held at ready epoch; Space starts 20 seconds; engine/RCS OFF; Earth rotates in canonical inertial publication. Camera WASD/QE, mouse look, R reset. No launch, departure or launch-stack qualification. Project Control manual support/presentation acceptance PASS; production shadows deferred."):
            poweredSupport?"SRV-01 powered support. Space starts 20 seconds: stock 600 N main, RCS OFF, finite fuel/oxidizer decrease. Sub-weight thrust: supported, no liftoff or departure. Camera WASD/QE, mouse look, R reset. Relaunch for a cold restart.":
            supportedContact?"SRV-01 supported contact. Space starts 20 seconds on the bounded slab. Main/RCS OFF; unchanged fuel and oxidizer. No departure. Camera WASD/QE, mouse look, R reset. Relaunch for a cold restart.":"SRV-01 reusable parts. Space starts the recorded 2-second episode: main, gimbal, six RCS commands. Exhaust shows realized jets only; no gravity or contact.");
        Console.WriteLine($"SRV01_COLD_PREPARATION ms={(Stopwatch.GetTimestamp()-preparationStart)*1000d/Stopwatch.Frequency:R} includes=catalog_owner_history_assets_bindings excludes=native_gpu_startup host_credit=0");
    }
    private static AssemblyFloridaSite PrepareFlorida(SimulationInstant epoch)
    {
        if(!TerrainAssetRepository.TryFindRoot(out var root))throw new InvalidDataException("Florida repository data root unavailable.");
        if(!EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error))throw new InvalidDataException(error);
        if(!TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error)||
            !EarthLocalTerrainElevationDataset.TryLoad(path,out error))throw new InvalidDataException(error);
        if(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)!=PhysicalSurfaceQueryStatus.Ready)throw new InvalidDataException("Florida physical terrain not ready.");
        if(!FloridaLaunchSite.TryCreate(6,query!.Authority.ReferenceRadiusMetres,PlanetaryTerrainDefinition.EarthProductionCubeV5,out var authored))throw new InvalidDataException("Florida authored base unavailable.");
        var site=AssemblyFloridaSite.CreateSlab(query,epoch,new(3),authored.CreateSupportSlab(query));
        Console.WriteLine($"SRV01_FLORIDA_SITE identity={site.Digest} authority={site.Authority} support=authored-base-footing-union presentation=single-finite-slab");
        return site;
    }
    internal void Start(){if(started||Failed)return;started=true;timestamp=Stopwatch.GetTimestamp();}
    internal void AdvanceLive(bool startRequested=false)
    {
        UpdateTitle();
        if(!started){if(startRequested)Start();return;}
        if(Completed||Failed){if(!reported){Report();reported=true;}return;}
        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;
        if(elapsed<0||frameCount==frameMilliseconds.Length){Fail(AssemblyFlightStatus.InvalidInput);return;}
        frameMilliseconds[frameCount++]=elapsed*1000d/Stopwatch.Frequency;
        var numerator=(Int128)elapsed*1_000_000+remainder;var ticks=numerator/Stopwatch.Frequency;remainder=(long)(numerator%Stopwatch.Frequency);
        if(ticks>long.MaxValue){Fail(AssemblyFlightStatus.Overflow);return;}
        var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));
        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;
    }
    internal void Advance(SimulationDuration elapsed)
    {
        if(!started||Completed||Failed)return;
        if(elapsed.Ticks!=0)
        {
            var credit=session.Engine.AdmitAssemblyHostTime(session.Authority,sequence+1,elapsed);
            if(credit.Status!=AssemblyFlightStatus.AcceptedCredit){Fail(credit.Status);return;}
            sequence++;
        }
        var service=supportedContact?session.Engine.ServiceAssemblyContactDebt(session.Authority):session.Engine.ServiceAssemblyFlightDebt(session.Authority);
        if(service.Status is not(AssemblyFlightStatus.Completed or AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.BudgetExhausted))
            {Fail(service.Status);return;}
        Observe();Completed=service.Status==AssemblyFlightStatus.Completed;
    }
    private void Fail(AssemblyFlightStatus status)
    {
        Failed=true;Failure=status;
        // Copy any fully committed endpoint even if private acknowledgement failed.
        var observed=session.Engine.ObserveAssemblyFlight(session.Authority,out var current);
        if(observed is AssemblyFlightStatus.Ready or AssemblyFlightStatus.Invalidated)observation=current;
    }
    private void Observe()
    {
        if(session.Engine.ObserveAssemblyFlight(session.Authority,out observation)!=AssemblyFlightStatus.Ready)throw new InvalidOperationException("Assembly publication unavailable.");
    }
    internal ResolvedRenderObject PresentedPart(int index)
    {
        var motion=observation.State.Motion;var binding=presentedMeshes[index];var p=design.Parts[binding.Part];
        var position=p.Instance.Pose.Position;var rotation=partRotations[binding.Part];
        if(binding.Mesh.Gimballed){position=p.Instance.Pose.Point(p.Definition.Gimbal!.Pivot);rotation*=GimbalRotation();}
        var value=new ResolvedRenderObject(new((uint)index+1),new(motion.PositionO+motion.BodyToWorld.Rotate(position),session.Launch.Spacecraft.CarrierFrame),
            motion.BodyToWorld*rotation,new(1,1,1),binding.Mesh.Handle);
        return FloridaView?.Embed(value)??value;
    }
    internal void BuildSubmission(in GpuCameraData camera,in UniversePosition cameraRoot,RenderFrameSubmission submission)
    {
        FloridaView?.RefreshDisplayFrame();
        submission.Begin(camera,cameraRoot);
        for(var i=0;i<presentedMeshes.Length;i++){var p=PresentedPart(i);submission.Add(p.RootPosition,p.RootOrientation,p.Scale,p.Mesh);}
        if(FloridaView is { } view)
        {
            var pad=view.Slab();
            submission.Add(pad.RootPosition,pad.RootOrientation,pad.Scale,pad.Mesh);
        }
        else if(supportedContact){var slab=SupportSlab();submission.Add(slab.RootPosition,slab.RootOrientation,slab.Scale,slab.Mesh);}
        var actual=observation.State.Actual;
        if(ActiveExhaust&&actual.MainOn)
        {
            var main=design.Main;var g=main.Definition.Gimbal!;var rotation=GimbalRotation();
            AddExhaust(submission,main.Instance.Pose.Point(g.Pivot+rotation.Rotate(g.NozzleOffset)),main.Instance.Pose.Rotation.Apply(rotation.Rotate(-Double3.UnitX)),1.35,.28);
        }
        for(var i=0;i<design.Jets.Length;i++)if(ActiveExhaust&&(actual.Jets&(1<<i))!=0)
        {
            var j=design.Jets[i];AddExhaust(submission,j.Instance.Pose.Point(j.Propulsion.Point),j.Instance.Pose.Rotation.Apply(-j.Propulsion.Axis),.36,.095);
        }
        submission.Complete();
    }
    // Specialized visual transport, using the existing 12 bytes reserved after
    // the mesh handle. No broad frame ABI or canonical observation is changed.
    internal unsafe void WriteExhaustParameters(NativeRenderObject* objects,int count)
    {
        var cursor=presentedMeshes.Length+(supportedContact?1:0);
        var time=BitConverter.SingleToUInt32Bits((float)(observation.Clock.Time.Ticks/1_000_000d));
        void Stamp(double exhaustSpeed,uint nozzle)
        {
            if(cursor>=count||objects[cursor].Mesh.Value!=Visuals.ExhaustMesh.Value)throw new InvalidOperationException("Exhaust transport binding");
            objects[cursor].Padding0=time;objects[cursor].Padding1=BitConverter.SingleToUInt32Bits((float)exhaustSpeed);objects[cursor].Padding2=nozzle;cursor++;
        }
        if(ActiveExhaust&&observation.State.Actual.MainOn)Stamp(design.Main.Definition.Propulsion!.ExhaustSpeed,0);
        for(var i=0;i<design.Jets.Length;i++)if(ActiveExhaust&&(observation.State.Actual.Jets&(1<<i))!=0)Stamp(design.Jets[i].Propulsion.ExhaustSpeed,(uint)i+1);
        if(cursor!=count)throw new InvalidOperationException("Exhaust transport count");
    }
    private DoubleQuaternion GimbalRotation()=>DoubleQuaternion.FromAxisAngle(Double3.UnitZ,observation.State.Gimbal.ActualZ)*DoubleQuaternion.FromAxisAngle(Double3.UnitY,observation.State.Gimbal.ActualY);
    private ResolvedRenderObject SupportSlab()=>new(new((uint)presentedMeshes.Length+1),new(new(0,-2.7,0),session.Launch.Spacecraft.CarrierFrame),DoubleQuaternion.Identity,new(16,2,16),MeshHandle.ContactQualificationSupport);
    private void AddExhaust(RenderFrameSubmission submission,Double3 local,Double3 outward,double length,double radius)
    {
        var axis=Double3.Cross(Double3.UnitX,outward);var q=axis.LengthSquared<1e-20?(outward.X>0?DoubleQuaternion.Identity:DoubleQuaternion.FromAxisAngle(Double3.UnitY,Math.PI)):DoubleQuaternion.FromAxisAngle(axis,Math.Acos(Math.Clamp(outward.X,-1,1)));
        var m=observation.State.Motion;submission.Add(new(m.PositionO+m.BodyToWorld.Rotate(local),session.Launch.Spacecraft.CarrierFrame),m.BodyToWorld*q,new(length,radius,radius),Visuals.ExhaustMesh);
    }
    private void ValidateBindings()
    {
        foreach(var part in design.Parts)
        {
            var asset=Visuals.Resolve(part.Definition.VisualReference);
            if(part.Definition.Gimbal is {} g&&((asset.GimbalPivot-g.Pivot).LengthSquared>1e-12||asset.Meshes.Count(m=>m.Gimballed)!=4))throw new InvalidDataException("Gimbal visual/physical binding mismatch.");
            if(part.Definition.Gimbal is {} mainGimbal)
            {
                var exit=asset.Sockets.Single(s=>s.Name=="SOCKET_exit");
                if((exit.Point-mainGimbal.Pivot-mainGimbal.NozzleOffset).LengthSquared>1e-12||(exit.Outward+part.Definition.Propulsion!.Axis).LengthSquared>1e-12)throw new InvalidDataException("Main visual/physical endpoint mismatch.");
            }
            if(part.Definition.JetActuators is {} jets)foreach(var jet in jets)
            {
                var socket=asset.Sockets.Single(s=>s.Name=="SOCKET_exit_"+jet.Id);
                if((socket.Point-jet.Point).LengthSquared>1e-12||(socket.Outward+jet.Axis).LengthSquared>1e-12)throw new InvalidDataException("Jet visual/physical endpoint mismatch.");
            }
        }
    }
    private static DoubleQuaternion Rotation(Matrix3 m)
    {
        double x,y,z,w;var trace=m.A+m.E+m.I;
        if(trace>0){var s=Math.Sqrt(trace+1)*2;w=s/4;x=(m.H-m.F)/s;y=(m.C-m.G)/s;z=(m.D-m.B)/s;}
        else if(m.A>m.E&&m.A>m.I){var s=Math.Sqrt(1+m.A-m.E-m.I)*2;w=(m.H-m.F)/s;x=s/4;y=(m.B+m.D)/s;z=(m.C+m.G)/s;}
        else if(m.E>m.I){var s=Math.Sqrt(1+m.E-m.A-m.I)*2;w=(m.C-m.G)/s;x=(m.B+m.D)/s;y=s/4;z=(m.F+m.H)/s;}
        else{var s=Math.Sqrt(1+m.I-m.A-m.E)*2;w=(m.D-m.B)/s;x=(m.C+m.G)/s;y=(m.F+m.H)/s;z=s/4;}
        return new DoubleQuaternion(x,y,z,w).Normalized();
    }
    public void Dispose(){session.Dispose();Visuals.Dispose();}
    private unsafe void UpdateTitle()
    {
        if(titledFrontier==observation.State.Frontier&&titledStarted==started&&titledCompleted==Completed&&titledFailed==Failed)return;
        var window=GetActiveWindow();if(window==IntPtr.Zero)return;
        var status=Failed?"FAILED":Completed?"COMPLETED / HELD ENDPOINT":started?"RUNNING":FloridaView?.SolarNavigation==true?"READY / AUTO START":"READY / SPACE TO START";
        observation.State.Stores.Fuel.TryToKilograms(out var fuel);observation.State.Stores.Oxidizer.TryToKilograms(out var oxidizer);
        var span=title.AsSpan();
        if(!span.TryWrite(CultureInfo.InvariantCulture,$"NovaCore - SRV-01 {(floridaSupport?"Florida support slab":poweredSupport?"powered support":supportedContact?"supported contact":"reusable parts")} - {status} | interval {observation.State.Frontier}/{session.Launch.Plan.Length} | main {(ActiveExhaust&&observation.State.Actual.MainOn?"ON":"OFF")} | RCS {(ActiveExhaust?observation.State.Actual.Jets:0):X4} | fuel {fuel:F6} kg oxide {oxidizer:F6} kg",out var written)||written>=title.Length-1)throw new InvalidOperationException("SRV01 status capacity");
        title[written]='\0';fixed(char* text=title)if(!SetWindowText(window,text))return;
        titledFrontier=observation.State.Frontier;titledStarted=started;titledCompleted=Completed;titledFailed=Failed;
    }
    [DllImport("user32.dll")]private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)]private static unsafe extern bool SetWindowText(IntPtr hwnd,char* text);
    internal byte[] Save()=>session.Save();
    private void Report()
    {
        observation.State.Stores.Fuel.TryToKilograms(out var fuel);observation.State.Stores.Oxidizer.TryToKilograms(out var oxide);
        Console.WriteLine($"STOCK_ASSEMBLY_END completed={Completed} failed={Failed} failure={Failure} frontier={observation.State.Frontier} revision={observation.StateRevision.Value} time={observation.Clock.Time.Ticks} fuel_kg={fuel:R} oxidizer_kg={oxide:R} mass_kg={observation.State.Mass.Mass:R} com_x={observation.State.Mass.Com.X:R} debt={observation.Clock.Debt.Ticks}");
        if(frameCount==0)return;Array.Sort(frameMilliseconds,0,frameCount);
        double P(double q)=>frameMilliseconds[Math.Clamp((int)Math.Ceiling(q*frameCount)-1,0,frameCount-1)];
        Console.WriteLine($"STOCK_ASSEMBLY_FRAME samples={frameCount} median_ms={P(.5):R} p95_ms={P(.95):R} p99_ms={P(.99):R} max_ms={P(1):R} measurement=display_callback_interval_including_present_host_scheduling");
        Array.Sort(serviceMilliseconds,0,frameCount);
        double W(double q)=>serviceMilliseconds[Math.Clamp((int)Math.Ceiling(q*frameCount)-1,0,frameCount-1)];
        Console.WriteLine($"SRV01_LIVE_SERVICE samples={frameCount} median_ms={W(.5):R} p95_ms={W(.95):R} p99_ms={W(.99):R} max_ms={W(1):R} includes=host_admission_bounded_service_copied_observation");
    }
}

/// <summary>
/// Earth-relative display embedding shared by terrain, slab and copied spacecraft poses.
/// The manual harness holds celestial presentation at READY; the Solar consumer retains
/// its existing celestial/navigation owner. Neither embedding is another physical clock.
/// </summary>
internal sealed class FloridaContactPresentation
{
    internal AssemblyFloridaSite Site {get;}
    internal SolarSystemScene Solar {get;}
    internal Double3 Origin {get;private set;}
    internal DoubleQuaternion Rotation {get;private set;}
    internal ReferenceFrameId Root {get;}
    internal bool SolarNavigation {get;}
    internal FloridaContactPresentation(AssemblyFloridaSite site,ReferenceFrameId root,SolarSystemScene? existingSolar=null)
    {
        var start=Stopwatch.GetTimestamp();Site=site;Root=root;SolarNavigation=existingSolar is not null;
        if(!site.Applicable||site.Authority.PhysicalGeneration!=(uint)PlanetaryPhysicalSurface.RuntimeGeneration||
            !TerrainAssetRepository.TryFindRoot(out var repository)||
            PlanetaryPhysicalSurfacePointQuery.TryAcquire(site.Authority.BodyId,repository,out var current)!=PhysicalSurfaceQueryStatus.Ready||
            current!.Authority!=site.Authority)
            throw new InvalidDataException("Florida display preparation refused.");
        var solar=existingSolar;
        if(solar is null&&!SolarSystemScene.TryCreateAt(root,site.Start,out solar,out _))throw new InvalidDataException("Florida Solar presentation unavailable.");
        Solar=solar!;
        if(!Solar.Presentation.TryGetBody(site.Authority.BodyId,out var earth)||
            Solar.FloridaLaunchSite.Object.Anchor.TerrainAuthorityVersion!=site.Authority.Terrain||
            site.Authority.FacilitySupportIdentity!=FloridaFacilitySupport.DefinitionIdentity||
            site.Slab is not {} slab || slab.RootRadius!=Solar.FloridaLaunchSite.LocalPhysicalSurfaceRadiusMetres || slab.FoundationDepth!=Solar.FloridaLaunchSite.FoundationDepthMetres)throw new InvalidDataException("Florida display/site identity mismatch.");
        RefreshDisplayFrame();
        Console.WriteLine($"FLORIDA_PRESENTATION_READY site={site.Digest} body={earth.BodyId} terrain={site.Authority.Terrain} facility={Solar.FloridaLaunchSite.Object.Id} displayEpoch={site.Start.Ticks} east={site.EastMetres:R} support=authored-base-footing-union coldMs={(Stopwatch.GetTimestamp()-start)*1000d/Stopwatch.Frequency:R}");
    }
    // This is a display-frame embedding, never canonical evolution. Solar browsing owns
    // celestial presentation time; copied contact endpoints remain in their original site frame.
    internal void RefreshDisplayFrame()
    {
        if(!Solar.Presentation.TryGetBody(Site.Authority.BodyId,out var earth))throw new InvalidDataException("Florida Earth missing.");
        Origin=earth.Position.Value+earth.BodyFixedToRoot.Rotate(Site.OriginBodyFixed);
        Rotation=earth.BodyFixedToRoot*Site.LocalToBodyFixed;
    }
    internal UniversePosition Position(Double3 local)=>new(Origin+Rotation.Rotate(local),Root);
    internal ResolvedRenderObject Embed(in ResolvedRenderObject local)=>local with
    {RootPosition=Position(local.RootPosition.Value),RootOrientation=Rotation*local.RootOrientation};
    internal ResolvedRenderObject Slab()
    {
        var slab=Site.Slab??throw new InvalidOperationException("Explicit slab authority required.");
        var centre=new Double3(0,AssemblyContactProfile.SupportPlaneAtOrigin-slab.Dimensions.Y*.5,0);
        return new(new(0xFFFF0001u),Position(centre),Rotation,slab.Dimensions,MeshHandle.FloridaSupportSlab);
    }
    internal void PrepareCamera(CameraState camera)
    {
        if(SolarNavigation)
        {if(!Solar.TryStartAtFloridaSupportSlab(camera,FloridaSlabSupport.AuthoredTop-AssemblyContactProfile.SupportPlaneAtOrigin))throw new InvalidDataException("Florida Solar camera unavailable.");return;}
        if(!Solar.Focus(camera,NativePresentationFocus.Earth))throw new InvalidDataException("Florida Earth presentation unavailable.");
        // View the copied spacecraft on the single authored support slab. Camera only.
        camera.Position=camera.Position with {Value=Position(new(12,7,16)).Value};
        camera.Orientation=Rotation*DoubleQuaternion.FromAxisAngle(Double3.UnitY,Math.Atan2(12,16))*
            DoubleQuaternion.FromAxisAngle(Double3.UnitX,-Math.Atan2(7,20));
        camera.Mode=CameraMode.Free;
        Solar.EnforceFinalCameraInvariant(camera);camera.Projection=Solar.Projection;
    }
}
