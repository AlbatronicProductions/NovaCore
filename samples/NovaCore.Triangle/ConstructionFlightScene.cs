using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Time;

/// <summary>Ordinary live presentation over the canonical construction session.</summary>
internal sealed class ConstructionFlightScene : IApplicationVesselScene
{
    internal ConstructionApplicationSession Session {get;}
    internal ConstructionRuntimeState State {get;private set;}
    internal CompiledCraft Craft {get;}
    public ReusablePartVisuals Visuals {get;}
    public FloridaContactPresentation FloridaView {get;}
    public PlayerFlightControlInput PlayerInput {get;}
    public bool UsesSolarCamera=>true;
    public long? PhysicalEpochTicks=>State.Epoch.Ticks;
    public int RenderCapacity=>meshes.Length+1+Craft.Actuators.Length;
    public ResolvedRenderSnapshot InitialSnapshot {get;}
    internal ConstructionActuationObservation Actuation {get;private set;}
    public bool Failed {get;private set;}
    private string? failureReason;
    private bool disposed;
    private readonly bool ownsVisuals;
    private readonly (int Part,PartVisualMesh Mesh)[] meshes;
    private readonly DoubleQuaternion[] rotations;
    private readonly double[] exhaustSpeeds;
    private readonly int mainActuator;
    private readonly bool trace=Environment.GetEnvironmentVariable("NOVACORE_CONTROL_TRACE")=="1";
    internal ConstructionFlightMeasurements Measurements {get;}=new();
    private long timestamp,hostSequence,remainder;
    private readonly char[] title=new char[512];
    private long titledSecond=-1,titledAdmission=-1;
    private string? titledStatus;
    private readonly double[] frameTimes=new double[4096],serviceTimes=new double[4096];
    private int measured;
    internal bool MeasureQualificationService {get;set;}
    internal double QualificationServiceMs;
    internal long QualificationServiceAllocated;
    internal long QualificationInstrumentationAllocated;
    internal double QualificationInstrumentationMs;
    internal ConstructionWorkProbe.Sample? QualificationWork;
    internal QualificationExecutionProbe.Sample? QualificationExecution;
    internal long QualificationDebtBefore,QualificationDebtAfter,QualificationAdmitted;
    private CameraState? restoredCamera;

    internal ConstructionFlightScene(CompiledCraft craft,string assetRoot,SolarSystemScene solar,ReusablePartVisuals? sharedVisuals=null)
        :this(craft,assetRoot,solar,sharedVisuals,null){}
    private ConstructionFlightScene(CompiledCraft craft,string assetRoot,SolarSystemScene solar,ReusablePartVisuals? sharedVisuals,ConstructionApplicationSession? restored)
    {
        Craft=craft;
        if(restored is null)
        {
            if(!TerrainAssetRepository.TryFindRoot(out var repository)||
                PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var query)!=PhysicalSurfaceQueryStatus.Ready)
                throw new InvalidDataException("Florida physical terrain is not ready.");
            var slab=solar.FloridaLaunchSite.CreateSupportSlab(query!);
            Session=ConstructionApplicationSession.CreateSupported(craft,query!,slab,solar.CurrentTime);
        }
        else Session=restored;
        try
        {
            State=Session.Binding.Initial;PlayerInput=new(Session);
            hostSequence=Session.Engine.ObserveConstructionHostSequence(Session.Authority);
            FloridaView=new(Session.Binding.Physical!.Site,solar.Presentation.RootFrame,solar);
            ownsVisuals=sharedVisuals is null;
            Visuals=sharedVisuals??new(craft.Render.Select(r=>r.Asset).DistinctBy(a=>a.Id+"/"+a.Revision).Select(a=>
                PartVisualLoader.Load(Path.Combine(assetRoot,a.RelativePath),a.Id+"/"+a.Revision,a.Sha256)).ToImmutableArray());
            foreach(var reference in craft.Render.Select(r=>r.Asset))if(Visuals.Resolve(reference.Id+"/"+reference.Revision).Sha256!=reference.Sha256)throw new InvalidDataException("Shared craft visual identity differs.");
            rotations=craft.Render.Select(r=>StockAssemblyDevelopmentScene.Rotation(r.MaterialPose.Rotation)).ToArray();
            exhaustSpeeds=craft.Actuators.Select(a=>{var c=craft.Design.Parts[a.Part].Definition.Construction!.Consumers.Single(c=>c.Id==a.Key.Consumer);return c.ThrustN/c.TotalFlowKgS;}).ToArray();
            mainActuator=craft.Actuators.IndexOf(Session.Binding.Physical!.Control.Main);
            meshes=craft.Render.SelectMany((r,i)=>Visuals.Resolve(r.Asset.Id+"/"+r.Asset.Revision).Meshes.Select(m=>(i,m))).ToArray();
            foreach(var pair in meshes)if(pair.Mesh.Gimballed)
            {
                var main=Session.Binding.Physical.Control.Main;
                var asset=Visuals.Resolve(craft.Render[pair.Part].Asset.Id+"/"+craft.Render[pair.Part].Asset.Revision);
                if(pair.Part!=main.Part||asset.GimbalPivot!=main.Gimbal!.Pivot)throw new InvalidDataException("Craft gimbal visual/physical binding differs.");
            }
            var objects=Enumerable.Range(0,meshes.Length).Select(PresentedPart).Append(FloridaView.Slab()).ToArray();
            if(!ResolvedRenderSnapshot.TryCreate(objects,out var snapshot,out _))throw new InvalidDataException("Craft visual publication refused.");
            InitialSnapshot=snapshot!;Observe();
        }
        catch{if(ownsVisuals)Visuals?.Dispose();Session.Dispose();throw;}
        Console.WriteLine($"MODULAR_FLIGHT_READY source={craft.Design.Digest} compiled={craft.Digest} vehicle={Session.Binding.Identity} parts={craft.Render.Length} epoch={State.Epoch.Ticks} mass={State.ReferenceMass!.Value.Mass:R} physical=continuous-64Hz restored={restored is not null} request={PlayerInput.Observation.Requested}");
        Console.WriteLine("Z: ignite · X: cutoff · W/S pitch · A/D yaw · Q/E roll · mouse orbit · wheel zoom · F: craft · 1–0: celestial focus. Physical flight runs at 1x. In NovaCore, Menu returns to retained construction.");
    }
    internal byte[] SaveFlight()
    {
        if(Failed||disposed)throw new InvalidDataException("Only an active qualified flight can be saved.");
        return Session.Save();
    }
    internal static ConstructionFlightScene RestoreFlight(AssemblyDefinitionCatalog catalog,byte[] checkpoint,string assetRoot,SolarSystemScene solar,ReusablePartVisuals? sharedVisuals=null)
    {
        if(!TerrainAssetRepository.TryFindRoot(out var repository)||
            PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository,out var query)!=PhysicalSurfaceQueryStatus.Ready)
            throw new InvalidDataException("Florida physical terrain is not ready.");
        var slab=solar.FloridaLaunchSite.CreateSupportSlab(query!);
        var restored=ConstructionApplicationSession.RestoreFlight(catalog,checkpoint,assetRoot,query!,slab);
        // Loading replaces a presentation lifetime, not the monotonic clock of
        // a running flight. Prepare privately at the saved physical epoch, at
        // 1x with no presentation debt or stale active-vessel binding.
        ConstructionFlightScene? next=null;
        try
        {
            if(!SolarSystemScene.TryCreateAt(solar.Presentation.RootFrame,restored.Binding.Initial.Epoch,out var successor,out var error))
                throw new InvalidDataException(error);
            next=new(restored.Binding.Physical!.Craft,assetRoot,successor!,sharedVisuals,restored);
            next.PrepareReplacementPresentation();return next;
        }
        catch{if(next is not null)next.Dispose();else restored.Dispose();throw;}
    }
    internal static ConstructionFlightScene LaunchReplacement(CompiledCraft craft,string assetRoot,SolarSystemScene solar,ReusablePartVisuals sharedVisuals)
    {
        if(!SolarSystemScene.TryCreateAt(solar.Presentation.RootFrame,solar.CurrentTime,out var successor,out var error))throw new InvalidDataException(error);
        var next=new ConstructionFlightScene(craft,assetRoot,successor!,sharedVisuals);
        try{next.PrepareReplacementPresentation();return next;}catch{next.Dispose();throw;}
    }
    private void PrepareReplacementPresentation()
    {
        var solar=FloridaView.Solar;
        var camera=new CameraState(new(solar.Presentation.RootFrame,Double3.Zero),DoubleQuaternion.Identity,solar.Projection,CameraMode.Free);
        FloridaView.PrepareCamera(camera);
        if(!solar.BindActiveVessel(PrepareFocusObservation())||!solar.RefocusActiveVessel(camera))throw new InvalidDataException("Replacement flight presentation refused.");
        camera.Validate();restoredCamera=camera;
    }
    internal bool ActivateRestoredPresentation(CameraState camera)
    {
        if(restoredCamera is not {} prepared)return false;
        camera.Position=prepared.Position;camera.Orientation=prepared.Orientation;
        camera.Projection=prepared.Projection;camera.Mode=prepared.Mode;restoredCamera=null;
        return true;
    }
    private void Observe()
    {
        var status=Session.Engine.ObserveConstructionServices(Session.Authority,out var state);
        if(status!=ConstructionServiceStatus.Ready||state is null)throw new InvalidDataException("Canonical craft observation unavailable.");
        State=state;
        if(Session.Engine.ObserveConstructionActuation(Session.Authority,out var actual)!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Craft actuation observation unavailable.");
        Actuation=actual;
        if(Session.Engine.ObserveConstructionContactPoints(Session.Authority,out var points)!=ConstructionServiceStatus.Ready)throw new InvalidDataException("Craft contact observation unavailable.");
        ContactPoints=points;
    }
    public SceneObjectFocusObservation PrepareFocusObservation()=>FloridaView.CraftFocus(Session.Binding,State,Session.Engine.State.Revision.Value,
        disposed?SceneObjectFocusStatus.Retired:Failed?SceneObjectFocusStatus.Failed:SceneObjectFocusStatus.Active);
    public void ApplyPlayerInput(in NativeInputState input)
    {
        if(Failed||disposed)return;
        var result=PlayerInput.Apply(input);
        if(result.Status is not(AssemblyControlStatus.Ready or AssemblyControlStatus.Admitted)){Fail(result.Status.ToString());return;}
        Observe();
        if(trace&&result.Status==AssemblyControlStatus.Admitted)Console.WriteLine($"MODULAR_INPUT keys={(uint)input.PilotKeys} actions={(uint)input.EngineActions} active={input.ControlInputActive} request={PlayerInput.Observation.Requested} actual={Actuation} sequence={State.Sequence}");
    }
    internal static NativeInputState CameraInput(in NativeInputState input)=>PlayerFlightControlInput.CameraInput(input) with {RateIncrease=0,RateDecrease=0,PauseToggle=0};
    internal void TraceCamera(in NativeInputState input,string stage)
    {
        if(trace&&(input.CameraActions!=0||input.PresentationFocus!=0))Console.WriteLine($"MODULAR_CAMERA stage={stage} actions={(uint)input.CameraActions} celestial={(uint)input.PresentationFocus} focus={FloridaView.Solar.CurrentFocusTarget.Kind} physical={State.Epoch.Ticks} solar={FloridaView.Solar.CurrentTime.Ticks}");
    }
    public void AdvanceLive(bool startRequested=false)
    {
        var now=Stopwatch.GetTimestamp();
        if(timestamp==0){timestamp=now;UpdateTitle();return;}
        var elapsed=now-timestamp;timestamp=now;
        if(Failed||disposed){UpdateTitle();return;}
        var numerator=(Int128)elapsed*1_000_000+remainder;
        if(elapsed<0||numerator/Stopwatch.Frequency>long.MaxValue){Fail("Host time overflow");return;}
        remainder=(long)(numerator%Stopwatch.Frequency);
        var instrumentationStart=MeasureQualificationService?Stopwatch.GetTimestamp():0;
        var instrumentationAllocation=MeasureQualificationService?GC.GetAllocatedBytesForCurrentThread():0;
        using var workProbe=MeasureQualificationService?new ConstructionWorkProbe():null;
        if(MeasureQualificationService){QualificationDebtBefore=Session.Clock.PendingSimulationDebt.Ticks;QualificationAdmitted=(long)(numerator/Stopwatch.Frequency);}
        var beforeAllocation=MeasureQualificationService?GC.GetAllocatedBytesForCurrentThread():0;
        var execution=MeasureQualificationService?QualificationExecutionProbe.Read():default;
        var begin=Stopwatch.GetTimestamp();Advance(new((long)(numerator/Stopwatch.Frequency)));
        if(MeasureQualificationService){QualificationServiceMs=Stopwatch.GetElapsedTime(begin).TotalMilliseconds;QualificationServiceAllocated=GC.GetAllocatedBytesForCurrentThread()-beforeAllocation;QualificationWork=workProbe!.Read();QualificationDebtAfter=Session.Clock.PendingSimulationDebt.Ticks;QualificationInstrumentationAllocated=GC.GetAllocatedBytesForCurrentThread()-instrumentationAllocation-QualificationServiceAllocated;QualificationInstrumentationMs=Stopwatch.GetElapsedTime(instrumentationStart).TotalMilliseconds-QualificationServiceMs;}
        if(MeasureQualificationService)QualificationExecution=QualificationExecutionProbe.Finish(execution);
        if(measured<frameTimes.Length){frameTimes[measured]=elapsed*1000d/Stopwatch.Frequency;serviceTimes[measured++]=(Stopwatch.GetTimestamp()-begin)*1000d/Stopwatch.Frequency;}
        UpdateTitle();
    }
    // Discard only wall-clock time spent outside active flight. Canonical epoch,
    // admitted time debt, resources, pose and actuator state remain retained.
    internal void SuspendLive()=>timestamp=0;
    internal void Advance(SimulationDuration duration)
    {
        if(Failed||disposed)return;
        if(duration.Ticks>0)
        {
            var credit=Session.Engine.AdmitConstructionHostTime(Session.Authority,checked(hostSequence+1),duration);
            if(credit!=ConstructionServiceStatus.AcceptedCredit){Fail(credit.ToString());return;}hostSequence++;
        }
        var status=Session.Engine.ServiceConstructionDebt(Session.Authority,out _);
        if(status is not(ConstructionServiceStatus.Published or ConstructionServiceStatus.AwaitingDebt)){Fail(Session.Engine.ConstructionSupportFailure(Session.Authority)??status.ToString());return;}
        Observe();
    }
    internal void Fail(string reason)
    {
        if(Failed)return;
        // Bounded service may have published earlier intervals before refusing
        // a later one. Always display the last canonical publication.
        if(Session.Engine.State.Spacecraft.TryGetConstruction(Session.Binding.Spacecraft.Id,out var binding,out var last)&&ReferenceEquals(binding,Session.Binding)&&last is not null)State=last;
        Failed=true;failureReason=reason;Actuation=default;
        Console.Error.WriteLine($"MODULAR_FLIGHT_REFUSED reason={reason} epoch={State.Epoch.Ticks} sequence={State.Sequence}");
    }
    private DoubleQuaternion Gimbal()=>DoubleQuaternion.FromAxisAngle(Double3.UnitZ,State.Physical!.Gimbal.ActualZ)*DoubleQuaternion.FromAxisAngle(Double3.UnitY,State.Physical.Gimbal.ActualY);
    internal ResolvedRenderObject PresentedPart(int index)
    {
        var binding=meshes[index];var pose=Craft.Render[binding.Part].MaterialPose;var position=pose.Position;var rotation=rotations[binding.Part];
        if(binding.Mesh.Gimballed){position=pose.Point(Session.Binding.Physical!.Control.Main.Gimbal!.Pivot);rotation*=Gimbal();}
        var motion=State.Physical!.Motion;
        return FloridaView.Embed(new(new((uint)index+1),new(motion.PositionO+motion.BodyToWorld.Rotate(position),Session.Binding.Spacecraft.CarrierFrame),motion.BodyToWorld*rotation,new(1,1,1),binding.Mesh.Handle));
    }
    public void BuildSubmission(in GpuCameraData camera,in UniversePosition root,RenderFrameSubmission submission)
    {
        FloridaView.RefreshDisplayFrame();submission.Begin(camera,root);
        for(var i=0;i<meshes.Length;i++){var p=PresentedPart(i);submission.Add(p.RootPosition,p.RootOrientation,p.Scale,p.Mesh);}
        var slab=FloridaView.Slab();submission.Add(slab.RootPosition,slab.RootOrientation,slab.Scale,slab.Mesh);
        if(Actuation.Main)
        {
            var main=Session.Binding.Physical!.Control.Main;var g=main.Gimbal!;var q=Gimbal();
            AddExhaust(submission,main.PartOrigin+main.PartRotation.Apply(g.Pivot+q.Rotate(g.NozzleOffset)),main.PartRotation.Apply(q.Rotate(-Double3.UnitX)),2.8,.35);
        }
        for(var i=0;i<Craft.Allocation.JetActuators.Length;i++)if(Actuation.Jets.Contains(i)){var a=Craft.Actuators[Craft.Allocation.JetActuators[i]];AddExhaust(submission,a.Point,-a.Axis,.36,.095);}
        submission.Complete();
    }
    private void AddExhaust(RenderFrameSubmission submission,Double3 point,Double3 axis,double length,double radius)
    {
        var cross=Double3.Cross(Double3.UnitX,axis);var q=cross.LengthSquared<1e-20?(axis.X>0?DoubleQuaternion.Identity:DoubleQuaternion.FromAxisAngle(Double3.UnitY,Math.PI)):DoubleQuaternion.FromAxisAngle(cross,Math.Acos(Math.Clamp(axis.X,-1,1)));
        var motion=State.Physical!.Motion;submission.Add(FloridaView.Position(motion.PositionO+motion.BodyToWorld.Rotate(point)),FloridaView.Rotation*motion.BodyToWorld*q,new(length,radius,radius),Visuals.ExhaustMesh);
    }
    public unsafe void WriteExhaustParameters(NativeRenderObject* objects,int count)
    {
        var cursor=meshes.Length+1;var time=BitConverter.SingleToUInt32Bits((float)((State.Epoch.Ticks-Session.Binding.Initial.Epoch.Ticks)/1_000_000d));
        void Stamp(int actuator,uint nozzle)
        {
            if(cursor>=count||objects[cursor].Mesh.Value!=Visuals.ExhaustMesh.Value)throw new InvalidOperationException("Craft exhaust binding differs.");
            objects[cursor].Padding0=time;objects[cursor].Padding1=BitConverter.SingleToUInt32Bits((float)exhaustSpeeds[actuator]);objects[cursor++].Padding2=nozzle;
        }
        if(Actuation.Main)Stamp(mainActuator,0);
        for(var i=0;i<Craft.Allocation.JetActuators.Length;i++)if(Actuation.Jets.Contains(i))Stamp(Craft.Allocation.JetActuators[i],(uint)i+1);
        if(cursor!=count)throw new InvalidOperationException("Craft exhaust count differs.");
    }
    private unsafe void UpdateTitle()
    {
        var second=(State.Epoch.Ticks-Session.Binding.Initial.Epoch.Ticks)/1_000_000;
        var status=FlightStatus;
        if(!Failed&&second==titledSecond&&titledAdmission==PlayerInput.Observation.AdmissionCount&&titledStatus==status)return;
        var window=GetActiveWindow();if(ownsVisuals&&window==IntPtr.Zero)return;
        if(!title.AsSpan().TryWrite(CultureInfo.InvariantCulture,$"NovaCore · {Craft.Design.Data.Craft!.Name} · {status} · {second}s · main {(Actuation.Main?"ON":"OFF")} · W/S pitch · A/D yaw · Q/E roll · X cutoff · F craft",out var written)||written>=title.Length-1)return;
        if(!ownsVisuals&&!PlayerStatus.AsSpan().SequenceEqual(title.AsSpan(0,written)))PlayerStatus=new string(title,0,written);
        title[written]='\0';if(window!=IntPtr.Zero){fixed(char* text=title)SetWindowText(window,text);}titledSecond=second;titledAdmission=PlayerInput.Observation.AdmissionCount;titledStatus=status;
    }
    internal string PlayerStatus {get;private set;}=string.Empty;
    internal int ContactPoints {get;private set;}
    internal string FlightStatus=>Failed?"FLIGHT STOPPED — outside qualified continuation":State.Physical!.Consumer==AssemblyPhysicalConsumer.SupportedContact?"SUPPORTED — Z to ignite":ContactPoints>0?"SURFACE CONTACT":Actuation.Main?"POWERED ASCENT":"COAST";
    [DllImport("user32.dll")]private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]private static extern unsafe bool SetWindowText(IntPtr window,char* text);
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        Console.WriteLine($"MODULAR_FLIGHT_END failed={Failed} reason={failureReason??"none"} source={Craft.Design.Digest} compiled={Craft.Digest} epoch={State.Epoch.Ticks} sequence={State.Sequence} mass={State.ReferenceMass!.Value.Mass:R} position={State.Physical!.Motion.PositionO} velocity={State.Physical.Motion.VelocityO}");
        if(measured>0){Array.Sort(frameTimes,0,measured);Array.Sort(serviceTimes,0,measured);double P(double[] a,double p)=>a[Math.Clamp((int)Math.Ceiling(p*measured)-1,0,measured-1)];Console.WriteLine($"MODULAR_FRAME samples={measured} median={P(frameTimes,.5):R} p95={P(frameTimes,.95):R} p99={P(frameTimes,.99):R} max={P(frameTimes,1):R} serviceMedian={P(serviceTimes,.5):R} serviceP95={P(serviceTimes,.95):R} serviceP99={P(serviceTimes,.99):R} serviceMax={P(serviceTimes,1):R}");}
        Session.Dispose();if(ownsVisuals)Visuals.Dispose();
        Measurements.Report();
    }
}
