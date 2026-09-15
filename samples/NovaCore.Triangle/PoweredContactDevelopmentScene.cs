using System.Diagnostics;
using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

/// <summary>Inspect two exact finite-fixture endpoints, then service retained dry contact live.</summary>
internal sealed class PoweredContactDevelopmentScene : IDisposable
{
    private static readonly ReferenceFrameId Root=new(1);
    private readonly SimulationTransactionEngine engine;
    private readonly PoweredFlightAuthority authority;
    private readonly PoweredContactPreparation preparation;
    private readonly LocalContactWorld world;
    private PoweredFlightObservation observation;
    private readonly ResolvedRenderObject slab=new(new(2),new(new(0,-1,0),Root),DoubleQuaternion.Identity,new(16,2,16),MeshHandle.ContactQualificationSupport);
    private readonly double[] frames=new double[20000];
    private readonly double[] work=new double[20000];
    private int stage,frameCount,budgetHits;
    private long sequence,timestamp,remainder;
    private bool terminal,reported;
    private IntPtr window;
    private string? displayed;
    internal string Status {get;private set;}="READY - Space: inspect powered endpoint 1 (exact 16,666 ticks)";
    internal PoweredFlightObservation Observation=>observation;
    internal ResolvedRenderSnapshot InitialSnapshot {get;}

    internal PoweredContactDevelopmentScene()
    {
        var cold=Stopwatch.GetTimestamp();var craft=new SpacecraftId(101);var body=new ReferenceFrameId(2);
        Require(PoweredContactPreparation.TryPrepare(PoweredContactFixture.CenteredEndpoint,Root,default,default,out var prepared)==PoweredFlightStatus.Ready,"fixture");
        preparation=prepared!;
        try
        {
            var graph=new ReferenceFrameGraphBuilder();graph.Add(new ReferenceFrameNode(Root,null,ReferenceFrameKind.Ecl,"supported numerical fixture root"));
            graph.Add(new ReferenceFrameNode(body,Root,ReferenceFrameKind.Ccf,"qualification box COM"));
            var p=preparation.Initial;
            Require(SpacecraftStateStore.TryCreateTranslating([new(craft,Root,body,"qualified powered contact numerical box")],
                [new(craft,default,p.Orientation,p.AngularVelocity,new(2,2,2),default,RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(preparation.ResourceDefinition.InitialTotalMassKilograms)],
                [new(craft,Root,default,p.Position,p.Velocity,default)],graph.Build(),out var store,out _),"state");
            engine=new(new SimulationClock(default,new SimulationTimeline(8)),new SimulationState(spacecraft:store),8);
            Require(engine.PrepareSpacecraftCommands(craft,1200,out var commands)==SpacecraftCommandStatus.Accepted,"commands");
            Require(engine.BindSingleEnginePreparation(commands!,preparation.EngineDefinition,out var actuator)==EnginePreparationStatus.Ready,"actuator");
            Require(engine.BindFinitePropellant(actuator!,preparation.ResourceDefinition,out var resource)==PropellantPreparationStatus.Ready,"resource");
            Require(engine.BeginPoweredContact(resource!,preparation,1200,out var power,out var retained)==PoweredFlightStatus.Ready,"retained world");
            authority=power!;world=retained!;
            Require(engine.AdmitSpacecraftCommand(commands!,1,1,SpacecraftCommandIntent.Ignite()).Status==SpacecraftCommandStatus.Accepted,"ignite");
            Require(engine.AdmitSpacecraftCommand(commands!,1,2,SpacecraftCommandIntent.ThrottlePosition(preparation.Throttle)).Status==SpacecraftCommandStatus.Accepted,"throttle");
            observation=engine.ObservePoweredContact(authority).Observation;
            Require(ResolvedRenderSnapshot.TryCreate([PresentedBody(),slab],out var snapshot,out _),"presentation");InitialSnapshot=snapshot!;
            Console.WriteLine($"POWERED_CONTACT_LIVE_READY cold_ms={Stopwatch.GetElapsedTime(cold).TotalMilliseconds:R} geometry=2x1x1_slab16x2x16 fixture=CenteredEndpoint source_mass_policy=unchanged debt=0");
            Console.WriteLine("Space #1: one exact powered interval, held for inspection. Space #2: exact exhaustion endpoint, held. Space #3: remaining dry contact live at 1:1 to 20s. First two inputs are explicit inspection intervals, not elapsed wall time. WASD/QE/mouse/R camera only. No departure or interpolation.");
        }
        catch { preparation.Dispose();throw; }
    }
    internal void PressSpace()
    {
        if(terminal||stage>=3)return;
        stage++;
        if(stage<=2)
        {
            Advance(new(stage==1?16666:16667));
            if(!terminal) Refresh(stage==1?"POWERED HELD - Space: exhaust next exact interval":"EXHAUSTED HELD - Space: start live dry continuation");
        }
        else { timestamp=Stopwatch.GetTimestamp();Refresh("RUNNING - live dry contact"); }
    }
    internal void AdvanceLive(bool space=false)
    {
        if(terminal){UpdateTitle();if(!reported){Report();reported=true;}return;}
        if(stage<3){if(space)PressSpace();UpdateTitle();return;}
        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;
        if(elapsed<0||frameCount==frames.Length){Fail("host/frame bound");UpdateTitle();return;}
        frames[frameCount]=elapsed*1000d/Stopwatch.Frequency;
        var numerator=(Int128)elapsed*1_000_000+remainder;var ticks=numerator/Stopwatch.Frequency;remainder=(long)(numerator%Stopwatch.Frequency);
        if(ticks>long.MaxValue){Fail("host overflow");UpdateTitle();return;}
        var start=Stopwatch.GetTimestamp();Advance(new((long)ticks));work[frameCount++]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;UpdateTitle();
    }
    internal void Advance(SimulationDuration duration)
    {
        if(terminal||stage==0)return;
        if(duration.Ticks!=0)
        {
            var credit=engine.AdmitPoweredContactHostTime(authority,sequence+1,duration);
            if(credit.Status!=PoweredFlightStatus.AcceptedCredit){Fail("credit "+credit.Status);return;}sequence++;
        }
        var result=engine.ServicePoweredContactDebt(authority);
        if(result.PublishedCount>0)observation=result.Observation;
        if(result.Status==PoweredFlightStatus.Completed){terminal=true;Refresh("COMPLETED - final supported endpoint held");}
        else if(result.Status is not(PoweredFlightStatus.AwaitingDebt or PoweredFlightStatus.BudgetExhausted))Fail("service "+result.Status);
        else if(result.Status==PoweredFlightStatus.BudgetExhausted)budgetHits++;
        if(stage>=3&&!terminal&&result.PublishedCount>0&&observation.Actuator.Frontier%30==0)Refresh("RUNNING - live dry contact");
    }
    private void Refresh(string label)
    {
        observation.Resource.RemainingUnits.TryToKilograms(out var fuel);
        Status=$"{label} | actual={observation.Actuator.Activity} throttle={observation.Actuator.EndpointThrottle:P1} fuel={fuel*1e6:F6} mg tick={observation.Clock.Time.Ticks}";
        Console.WriteLine("POWERED_CONTACT_VISIBLE_STATUS "+Status);
    }
    internal ResolvedRenderObject PresentedBody()=>new(new(1),new(observation.Endpoint.PositionRoot,Root),observation.Endpoint.BodyToRoot,new(2,1,1),MeshHandle.ContactQualificationBody);
    internal void BuildSubmission(in GpuCameraData camera,in UniversePosition cameraRoot,RenderFrameSubmission submission)
    {
        submission.Begin(camera,cameraRoot);var b=PresentedBody();submission.Add(b.RootPosition,b.RootOrientation,b.Scale,b.Mesh);
        submission.Add(slab.RootPosition,slab.RootOrientation,slab.Scale,slab.Mesh);submission.Complete();
    }
    private void UpdateTitle()
    {
        if(displayed==Status)return;if(window==IntPtr.Zero)window=GetActiveWindow();if(window==IntPtr.Zero)return;
        Require(SetWindowText(window,"NovaCore - Powered supported contact - "+Status),"window status");displayed=Status;
    }
    private void Fail(string reason){terminal=true;Status="FAILED - "+reason;Console.Error.WriteLine(Status);}
    private void Report()
    {
        Console.WriteLine($"POWERED_CONTACT_LIVE_END frontier={observation.Actuator.Frontier} history={observation.HistoryCount} ticks={observation.Clock.Time.Ticks} budgetHits={budgetHits} debt={engine.CaptureContinuationClock().Debt.Ticks} status={Status}");
        if(frameCount==0)return;
        static string Distribution(double[] a,int count){Array.Sort(a,0,count);double P(double q)=>a[Math.Clamp((int)Math.Ceiling(q*count)-1,0,count-1)];return $"median_ms={P(.5):R} p95_ms={P(.95):R} p99_ms={P(.99):R} max_ms={P(1):R}";}
        Console.WriteLine($"POWERED_CONTACT_LIVE_FRAME samples={frameCount} {Distribution(frames,frameCount)} includes_present_and_host_scheduling=true");
        Console.WriteLine($"POWERED_CONTACT_LIVE_WORK samples={frameCount} {Distribution(work,frameCount)} includes_credit_and_bounded_service=true");
    }
    public void Dispose(){world.Dispose();preparation.Dispose();}
    private static void Require(bool ok,string phase){if(!ok)throw new InvalidOperationException("Powered contact presentation: "+phase);}
    [DllImport("user32.dll")]private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWindowText(IntPtr hwnd,string text);
}
