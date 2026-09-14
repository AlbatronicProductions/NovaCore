using System.Diagnostics;
using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

/// <summary>Zero-environment propulsion witness. All displayed motion comes from copied canonical endpoints.</summary>
internal sealed class PoweredFlightDevelopmentScene
{
    private static readonly ReferenceFrameId Root=new(1);
    private static readonly double BodySide=Math.Sqrt(1.5); // Uniform dry cube: m*s^2/6 = 2 kg m^2.
    private readonly SimulationTransactionEngine engine;
    private readonly PoweredFlightAuthority authority;
    private readonly SpacecraftCommandAuthority commands;
    private readonly ResolvedRenderObject[] markers;
    private PoweredFlightObservation observation;
    private long sequence,timestamp,remainder,nextStatusTick;
    private bool started,terminal,reported;
    private string? displayedStatus;
    private IntPtr window;
    private readonly double[] frameMilliseconds=new double[20000];
    private int frameCount;
    internal ResolvedRenderSnapshot InitialSnapshot { get; }
    internal string Status { get; private set; }="READY - Space starts 8s free flight; no gravity/contact";
    internal PoweredFlightObservation Observation=>observation;

    internal PoweredFlightDevelopmentScene()
    {
        var cold=Stopwatch.GetTimestamp();var craft=new SpacecraftId(101);var body=new ReferenceFrameId(2);
        var graph=new ReferenceFrameGraphBuilder();graph.Add(new ReferenceFrameNode(Root,null,ReferenceFrameKind.Ecl,"free-flight inertial root"));
        graph.Add(new ReferenceFrameNode(body,Root,ReferenceFrameKind.Ccf,"authored dry cube COM"));
        Require(PropellantDefinition.TryCreate(new(1,1,1,1,1,1,1,PropellantMassLaw.CentralPointReservoirV1,8,new(2,2,2)),1d/128,out var resourceDefinition)==PropellantPreparationStatus.Ready,"resource");
        Require(SpacecraftStateStore.TryCreateTranslating([new(craft,Root,body,"8 kg dry uniform cube + ideal central point fuel")],
            [new(craft,default,DoubleQuaternion.Identity,default,new(2,2,2),default,RigidBodyRotationModel.ConstantBodyTorqueV1)],
            [new(resourceDefinition!.InitialTotalMassKilograms)],[new(craft,Root,default,default,default,default)],graph.Build(),out var store,out _),"state");
        var clock=new SimulationClock(default,new SimulationTimeline(4));engine=new(clock,new SimulationState(spacecraft:store),4);
        Require(engine.PrepareSpacecraftCommands(craft,480,out var boundCommands)==SpacecraftCommandStatus.Accepted,"commands");commands=boundCommands!;
        Require(IdealEngineDefinition.TryCreate(1,1,default,Double3.UnitX,8,5120,true,out var engineDefinition)==EnginePreparationStatus.Ready,"engine definition");
        Require(engine.BindSingleEnginePreparation(commands,engineDefinition,out var actuation)==EnginePreparationStatus.Ready,"engine bind");
        Require(engine.BindFinitePropellant(actuation!,resourceDefinition,out var resource)==PropellantPreparationStatus.Ready,"resource bind");
        Require(engine.BeginPoweredFreeFlight(resource!,480,out var powered)==PoweredFlightStatus.Ready,"powered bind");authority=powered!;
        observation=engine.ObservePoweredFreeFlight(authority).Observation;
        // Presentation-only reference lines, z=-3m. They are neither a support surface nor collision objects.
        markers=new ResolvedRenderObject[18];
        for(var i=0;i<15;i++)markers[i]=new(new((uint)i+2),new(new(i*2,-0,-3),Root),DoubleQuaternion.Identity,new(.025,8,.025),MeshHandle.ContactQualificationSupport);
        for(var i=0;i<3;i++)markers[15+i]=new(new((uint)i+17),new(new(14,(i-1)*3,-3),Root),DoubleQuaternion.Identity,new(28,.025,.025),MeshHandle.ContactQualificationSupport);
        var initial=new ResolvedRenderObject[markers.Length+1];initial[0]=PresentedBody();markers.CopyTo(initial,1);
        Require(ResolvedRenderSnapshot.TryCreate(initial,out var snapshot,out _),"presentation");InitialSnapshot=snapshot!;
        Console.WriteLine($"POWERED_LIVE_READY cold_ms={Stopwatch.GetElapsedTime(cold).TotalMilliseconds:R} dry_kg=8 fuel_kg=0.0078125 inertia=2,2,2 cube_side_m={BodySide:R} thrust_N=8 exhaust_mps=5120 steps=480 debt=0 contact=false gravity=false");
        Console.WriteLine("Powered free flight: Space starts. Blue authored cube accelerates in +X for approximately 5s, then coasts; final endpoint holds at 8s. Grid is presentation only. WASD/QE, mouse look, R camera reset. Relaunch for a cold restart.");
    }
    internal void Start()
    {
        if(started||terminal)return;
        Require(engine.AdmitSpacecraftCommand(commands,1,1,SpacecraftCommandIntent.Ignite()).Status==SpacecraftCommandStatus.Accepted,"ignite command");
        Require(engine.AdmitSpacecraftCommand(commands,1,2,SpacecraftCommandIntent.ThrottlePosition(1)).Status==SpacecraftCommandStatus.Accepted,"throttle command");
        started=true;Status="RUNNING - command accepted; awaiting first exact interval";
    }
    internal void AdvanceLive(bool startRequested=false)
    {
        if(terminal){UpdateTitle();if(!reported){Report();reported=true;}return;}
        if(!started)
        {
            if(startRequested){Start();timestamp=Stopwatch.GetTimestamp();}
            UpdateTitle();return;
        }
        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;
        if(elapsed<0){Fail("host clock moved backward");return;}
        if(frameCount==frameMilliseconds.Length){Fail("frame observation capacity");return;}
        frameMilliseconds[frameCount++]=elapsed*1000d/Stopwatch.Frequency;
        var numerator=(Int128)elapsed*1_000_000+remainder;var ticks=numerator/Stopwatch.Frequency;remainder=(long)(numerator%Stopwatch.Frequency);
        if(ticks>long.MaxValue){Fail("host duration overflow");return;}
        Advance(new((long)ticks));UpdateTitle();
    }
    internal void Advance(SimulationDuration elapsed)
    {
        if(!started||terminal)return;
        if(elapsed.Ticks!=0)
        {
            var credit=engine.AdmitPoweredHostTime(authority,sequence+1,elapsed);
            if(credit.Status!=PoweredFlightStatus.AcceptedCredit){observation=credit.Observation.HistoryCount>0?credit.Observation:observation;Fail("host credit "+credit.Status);return;}
            sequence++;
        }
        var service=engine.ServicePoweredFlightDebt(authority);
        if(service.PublishedCount>0)observation=service.Observation;
        if(service.Status==PoweredFlightStatus.Completed){terminal=true;RefreshStatus("COMPLETED - final canonical endpoint held");}
        else if(service.Status is not(PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.BudgetExhausted))Fail("physical service "+service.Status);
        else if(observation.Clock.Time.Ticks>=nextStatusTick){nextStatusTick=observation.Clock.Time.Ticks+200000;RefreshStatus("RUNNING");}
    }
    private void RefreshStatus(string state)
    {
        observation.Resource.RemainingUnits.TryToKilograms(out var fuel);
        Status=$"{state} | actual={observation.Actuator.Activity} throttle={observation.Actuator.EndpointThrottle:P0} fuel={fuel*1000:F3} g t={observation.Clock.Time.Ticks/1e6:F2}s v={observation.Endpoint.VelocityRoot.X:F3}m/s";
    }
    internal ResolvedRenderObject PresentedBody()=>new(new(1),new(observation.Endpoint.PositionRoot,Root),observation.Endpoint.BodyToRoot,
        new(BodySide,BodySide,BodySide),MeshHandle.ContactQualificationBody);
    internal void BuildSubmission(in GpuCameraData camera,in UniversePosition cameraRoot,RenderFrameSubmission submission)
    {
        submission.Begin(camera,cameraRoot);var body=PresentedBody();submission.Add(body.RootPosition,body.RootOrientation,body.Scale,body.Mesh);
        foreach(var marker in markers)submission.Add(marker.RootPosition,marker.RootOrientation,marker.Scale,marker.Mesh);
        submission.Complete();
    }
    private void Fail(string reason){terminal=true;Status="FAILED - "+reason;Console.Error.WriteLine("POWERED_LIVE_FAILED "+reason);}
    private void UpdateTitle()
    {
        if(displayedStatus==Status)return;if(window==IntPtr.Zero)window=GetActiveWindow();if(window==IntPtr.Zero)return;
        if(!SetWindowText(window,"NovaCore - Powered free flight - "+Status))throw new InvalidOperationException("Powered status write failed.");
        displayedStatus=Status;Console.WriteLine("POWERED_VISIBLE_STATUS "+Status);
    }
    [DllImport("user32.dll")]private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWindowText(IntPtr window,string text);
    private void Report()
    {
        Console.WriteLine($"POWERED_LIVE_END status={Status} frontier={observation.Actuator.Frontier} clock={observation.Clock.Time.Ticks} debt={engine.CaptureContinuationClock().Debt.Ticks} revision={observation.StateRevision.Value} history={observation.HistoryCount}");
        if(frameCount==0)return;Array.Sort(frameMilliseconds,0,frameCount);
        double P(double q)=>frameMilliseconds[Math.Clamp((int)Math.Ceiling(q*frameCount)-1,0,frameCount-1)];
        Console.WriteLine($"POWERED_LIVE_FRAME samples={frameCount} median_ms={P(.5):R} p95_ms={P(.95):R} p99_ms={P(.99):R} max_ms={P(1):R} measurement=whole_callback_interval_including_present_host_scheduling objective_ms=6.67 presentation_sample_bytes={frameMilliseconds.Length*8}");
    }
    private static void Require(bool condition,string stage){if(!condition)throw new InvalidOperationException("Powered scene preparation: "+stage);}
}
