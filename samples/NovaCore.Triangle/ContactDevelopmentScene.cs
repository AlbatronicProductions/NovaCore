using System.Diagnostics;
using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

/// <summary>Bounded live witness. Host time is input; copied canonical endpoints are presentation authority.</summary>
internal sealed class ContactDevelopmentScene : IDisposable
{
    private static readonly ReferenceFrameId Root = new(1);
    private readonly SimulationTransactionEngine engine;
    private readonly LocalContactWorld world;
    private readonly LocalContactConfiguration configuration;
    private LocalContactWorld.Receipt receipt;
    private SpacecraftTranslationState linear;
    private SpacecraftRigidBodyRotationState angular;
    private long sequence, timestamp, hostRemainder;
    private bool started, terminal, reported;
    private readonly string windowLabel;
    private string? displayedStatus;
    private IntPtr window;
    private readonly double[] frameMilliseconds = new double[100_000];
    private readonly FrameWork[]? frameWork = Environment.GetEnvironmentVariable("NOVACORE_CONTACT_TAIL_PROBE")=="1" ? new FrameWork[100_000] : null;
    private readonly record struct FrameWork(long Before, long After, double CreditMs, double ServiceMs);
    private FrameWork lastWork;
    private int frameCount;
    internal ResolvedRenderSnapshot InitialSnapshot { get; }
    internal string Status { get; private set; } = "READY - prepared; Space starts 20-second episode";
    internal string WindowStatus => windowLabel+Status;
    internal long Frontier => receipt.Step;
    internal SpacecraftTranslationState PresentedTranslation => linear;
    internal SpacecraftRigidBodyRotationState PresentedRotation => angular;

    internal ContactDevelopmentScene(bool tilted)
    {
        windowLabel = "NovaCore - Contact development / " + (tilted ? "tilted" : "centered") + " - ";
        var cold = Stopwatch.GetTimestamp();
        var craft = new SpacecraftId(901); var body = new ReferenceFrameId(902);
        var graph = new ReferenceFrameGraphBuilder();
        graph.Add(new ReferenceFrameNode(Root,null,ReferenceFrameKind.Ecl,"Contact qualification root"));
        graph.Add(new ReferenceFrameNode(body,Root,ReferenceFrameKind.Ccf,"Qualification-only box"));
        Require(LocalContactConfiguration.TryCreate(1,Root,Double3.Zero,Double3.Zero,DoubleQuaternion.Identity,new(2,1,1),64,out var config)==LocalContactStatus.Success,"configuration");
        configuration=config!;
        linear=new(craft,Root,SimulationInstant.Zero,new(0,2,0),Double3.Zero,new(0,-9810,0));
        angular=new(craft,SimulationInstant.Zero,tilted?DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.25):DoubleQuaternion.Identity,
            Double3.Zero,configuration.BoxInertia(1000),Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1);
        Require(SpacecraftStateStore.TryCreateTranslating([new(craft,Root,body,"Qualification-only box")],[angular],[new(1000)],[linear],graph.Build(),out var store,out _),"state");
        var clock=new SimulationClock(SimulationInstant.Zero,new SimulationTimeline(4));
        engine=new(clock,new SimulationState(spacecraft:store),4,persistentContactHistoryCapacity:1200);
        Require(LocalContactSource.Capture(engine,craft,configuration,new(20_000_000),out var source)==LocalContactStatus.Success,"source");
        var setupMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds; cold=Stopwatch.GetTimestamp();
        Require(LocalContactWorld.TryCreate(engine,source!,configuration,out var created,out receipt)==LocalContactStatus.Success,"world");
        world=created!;
        var worldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds; cold=Stopwatch.GetTimestamp();
        try
        {
            Require(engine.BeginPersistentContact(world,configuration,receipt)==LocalContactStatus.Success,"binding");
            Require(ResolvedRenderSnapshot.TryCreate([
                new(new(1),new(linear.PositionRoot,Root),angular.OrientationLocalToParent,new(2,1,1),MeshHandle.ContactQualificationBody),
                new(new(2),new(new(0,-1,0),Root),DoubleQuaternion.Identity,new(128,2,128),MeshHandle.ContactQualificationSupport)
            ],out var snapshot,out _),"presentation");
            InitialSnapshot=snapshot!;
            Console.WriteLine($"CONTACT_LIVE_READY tilted={tilted} setup_ms={setupMs:F4} world_ms={worldMs:F4} binding_presentation_ms={Stopwatch.GetElapsedTime(cold).TotalMilliseconds:F4} debt=0 steps=1200 rate=1:1 qualification_only=true");
            Console.WriteLine("Contact development: blue 2 x 1 x 1 m box / gray slab. Camera WASD/QE, mouse look, R camera reset. Fixed 20-second episode; no free-flight entry. Relaunch for an explicit cold restart.");
        }
        catch { world.Dispose(); throw; }
    }

    // Called only after native/presentation preparation. The first callback establishes the epoch of host sampling.
    internal void AdvanceLive(bool startRequested=false)
    {
        if(terminal)
        {
            UpdateWindowStatus();
            if(!reported)
            {
                // Include the final completion/title/presentation interval without admitting more host credit.
                if(started && frameCount<frameMilliseconds.Length)
                {frameMilliseconds[frameCount]=Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;if(frameWork is not null)frameWork[frameCount]=lastWork;frameCount++;}
                Report();reported=true;
            }
            return;
        }
        if(!started && !startRequested){UpdateWindowStatus();return;}
        var now=Stopwatch.GetTimestamp();
        if(!started){timestamp=now;started=true;Status="RUNNING (20-second episode)";UpdateWindowStatus();return;}
        var elapsed=now-timestamp;timestamp=now;
        if(elapsed<0){Fail("Host clock moved backward");return;}
        if(frameCount<frameMilliseconds.Length){frameMilliseconds[frameCount]=elapsed*1000d/Stopwatch.Frequency;if(frameWork is not null)frameWork[frameCount]=lastWork;frameCount++;}
        else {Fail("Frame observation capacity exhausted");return;}
        var numerator=(Int128)elapsed*SimulationInstant.TicksPerSecond+hostRemainder;
        var ticks=numerator/Stopwatch.Frequency;hostRemainder=(long)(numerator%Stopwatch.Frequency);
        if(ticks>long.MaxValue){Fail("Host duration overflow");return;}
        Advance(new((long)ticks));
        UpdateWindowStatus();
    }

    // Presentation-only, called on the native window's owner thread and only writes when status changes.
    private void UpdateWindowStatus()
    {
        if(displayedStatus==Status)return;
        if(window==IntPtr.Zero)window=GetActiveWindow();
        if(window==IntPtr.Zero)return;
        var expected=WindowStatus;
        if(!SetWindowText(window,expected))throw new InvalidOperationException("Contact window status write failed");
        var copied=new System.Text.StringBuilder(expected.Length+1);
        GetWindowText(window,copied,copied.Capacity);
        if(copied.ToString()!=expected)throw new InvalidOperationException("Contact window status roundtrip mismatch");
        displayedStatus=Status;
        Console.WriteLine("CONTACT_VISIBLE_STATUS roundtrip=PASS text="+copied);
    }
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowText(IntPtr window,string text);
    [DllImport("user32.dll",EntryPoint="GetWindowTextW",CharSet=CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window,System.Text.StringBuilder text,int capacity);

    // Same owner route used by deterministic presentation tests; no camera/render timing enters the solver.
    internal void Advance(SimulationDuration elapsed)
    {
        if(terminal)return;
        var beforeFrontier=receipt.Step;
        var creditStart=frameWork is null?0:Stopwatch.GetTimestamp();
        if(elapsed.Ticks!=0)
        {
            var credit=engine.AdmitContactHostTime(world,configuration,receipt,new(sequence+1,elapsed));
            if(credit.CanonicalCommitted)sequence++;
            if(credit.Status!=ContactHostCreditStatus.Accepted){Fail($"Host credit {credit.Status}/{credit.AuthorityStatus}");return;}
        }
        var creditMs=frameWork is null?0:Stopwatch.GetElapsedTime(creditStart).TotalMilliseconds;
        var serviceStart=frameWork is null?0:Stopwatch.GetTimestamp();
        var serviced=engine.ServiceContactDebt(world,configuration,receipt);receipt=serviced.Receipt;
        if(frameWork is not null)lastWork=new(beforeFrontier,receipt.Step,creditMs,Stopwatch.GetElapsedTime(serviceStart).TotalMilliseconds);
        if(serviced.Published>0){linear=serviced.Observation.Translation;angular=serviced.Observation.Rotation;}
        switch(serviced.Status)
        {
            case ContactServiceStatus.AwaitingDebt: case ContactServiceStatus.BudgetExhausted: break;
            case ContactServiceStatus.Completed:
                terminal=true;Status="COMPLETED - final canonical endpoint held";break;
            default: Fail($"Service {serviced.Status}/{serviced.AuthorityStatus}/{serviced.PublicationStatus}");break;
        }
    }

    internal void BuildSubmission(in GpuCameraData camera,in UniversePosition cameraRoot,RenderFrameSubmission submission)
    {
        submission.Begin(camera,cameraRoot);
        submission.Add(new UniversePosition(linear.PositionRoot,Root),angular.OrientationLocalToParent,new(2,1,1),MeshHandle.ContactQualificationBody);
        submission.Add(new UniversePosition(new(0,-1,0),Root),DoubleQuaternion.Identity,new(128,2,128),MeshHandle.ContactQualificationSupport);
        submission.Complete();
    }
    private void Fail(string reason){terminal=true;Status="FAILED - "+reason;Console.Error.WriteLine("CONTACT_LIVE_FAILED "+reason);}
    private void Report()
    {
        Console.WriteLine($"CONTACT_LIVE_END status={Status} frontier={receipt.Step} clock={engine.CaptureContinuationClock().Time.Ticks} debt={engine.CaptureContinuationClock().Debt.Ticks} revision={engine.State.Revision.Value} history={engine.ProcessedPersistentContactCount}");
        if(frameCount==0)return;
        if(frameWork is not null)
        {
            var tails=0;
            for(var i=0;i<frameCount;i++)if(frameMilliseconds[i]>6.67)
            {
                tails++;var w=frameWork[i];
                Console.WriteLine(FormattableString.Invariant($"CONTACT_FRAME_TAIL index={i} ms={frameMilliseconds[i]:R} preceding_frontier={w.Before}->{w.After} preceding_credit_ms={w.CreditMs:R} preceding_service_ms={w.ServiceMs:R} outside_credit_service_ms={frameMilliseconds[i]-w.CreditMs-w.ServiceMs:R}"));
            }
            Console.WriteLine($"CONTACT_FRAME_TAIL_COUNT above_6_67={tails} samples={frameCount} zero_based=true window=previous_callback_to_current_callback waits=UNAVAILABLE");
        }
        Array.Sort(frameMilliseconds,0,frameCount);
        double P(double q)=>frameMilliseconds[Math.Clamp((int)Math.Ceiling(q*frameCount)-1,0,frameCount-1)];
        Console.WriteLine($"CONTACT_LIVE_FRAME samples={frameCount} median_ms={P(.5):F6} p95_ms={P(.95):F6} p99_ms={P(.99):F6} max_ms={P(1):F6} measurement=callback_to_callback_including_present_and_host_scheduling objective_ms=6.666667 storage_bytes={frameMilliseconds.Length*sizeof(double)}");
    }
    private static void Require(bool result,string stage){if(!result)throw new InvalidOperationException("Contact development preparation: "+stage);}
    public void Dispose()=>world.Dispose();
}
