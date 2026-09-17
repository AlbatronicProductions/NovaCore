using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using NovaCore.Core;
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
    private readonly DoubleQuaternion[] partRotations;
    private readonly (int Part,PartVisualMesh Mesh)[] presentedMeshes;
    internal ReusablePartVisuals Visuals {get;}
    internal int RenderCapacity=>presentedMeshes.Length+design.Jets.Length+1;
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
    internal StockAssemblyDevelopmentScene(string stockId="novacore.stock.SRV01.FourHorn")
    {
        var preparationStart=Stopwatch.GetTimestamp();
        var d=AssemblyStockCatalog.LoadDefault().Resolve(stockId);
        if(!d.HasIndependentBlockJets)throw new InvalidDataException("The four-horn visual requires the separately qualified sixteen-jet definition.");
        design=d;
        // Fully recorded input schedule, selected before instantiation. No display-frame commands.
        var plan=new AssemblyCommand[128];
        for(var i=0;i<plan.Length;i++)plan[i]=i<32?new(true,null,i<16?0:.025,0,15625):new(false,d.Data.Design.Pairs[(i-32)/16].Name,0,0,15625);
        var launch=new AssemblyLaunch(d,new(new(201),new(1),new(2),"Registered reference assembly"),"reference-launch-01",
            new(default,default,DoubleQuaternion.Identity,default),default,plan);
        session=AssemblyApplicationSession.Create(launch);Observe();
        partRotations=d.Parts.Select(p=>Rotation(p.Instance.Pose.Rotation)).ToArray();
        Visuals=new(Path.Combine(AppContext.BaseDirectory,"assets","visual","SRV01"));
        try
        {
            presentedMeshes=d.Parts.SelectMany((p,i)=>Visuals.Resolve(p.Definition.VisualReference).Meshes.Select(m=>(i,m))).ToArray();
            ValidateBindings();
            var objects=new ResolvedRenderObject[presentedMeshes.Length];for(var i=0;i<objects.Length;i++)objects[i]=PresentedPart(i);
            if(!ResolvedRenderSnapshot.TryCreate(objects,out var snapshot,out _))throw new InvalidDataException("Assembly presentation refused.");
            InitialSnapshot=snapshot!;
        }
        catch{Visuals.Dispose();throw;}
        Console.WriteLine("STOCK_ASSEMBLY_READY design="+d.Data.Design.Id+" digest="+d.Digest+" intervals=128 cadence_hz=64 max_service=4 parts=7 physical_jets=16 visual_mesh_instances="+presentedMeshes.Length+" shared_gpu_meshes="+Visuals.UniqueMeshCount+" mesh_buffer_bytes="+Visuals.BufferBytes);
        Console.WriteLine("SRV-01 reusable parts. Space starts the recorded 2-second episode: main, gimbal, six RCS commands. Exhaust shows realized jets only; no gravity or contact.");
        Console.WriteLine($"SRV01_COLD_PREPARATION ms={(Stopwatch.GetTimestamp()-preparationStart)*1000d/Stopwatch.Frequency:R} includes=catalog_owner_history_assets_bindings excludes=native_gpu_startup host_credit=0");
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
        var service=session.Engine.ServiceAssemblyFlightDebt(session.Authority);
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
        return new(new((uint)index+1),new(motion.PositionO+motion.BodyToWorld.Rotate(position),session.Launch.Spacecraft.CarrierFrame),
            motion.BodyToWorld*rotation,new(1,1,1),binding.Mesh.Handle);
    }
    internal void BuildSubmission(in GpuCameraData camera,in UniversePosition cameraRoot,RenderFrameSubmission submission)
    {
        submission.Begin(camera,cameraRoot);
        for(var i=0;i<presentedMeshes.Length;i++){var p=PresentedPart(i);submission.Add(p.RootPosition,p.RootOrientation,p.Scale,p.Mesh);}
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
        var cursor=presentedMeshes.Length;
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
    public void Dispose()=>Visuals.Dispose();
    private unsafe void UpdateTitle()
    {
        if(titledFrontier==observation.State.Frontier&&titledStarted==started&&titledCompleted==Completed&&titledFailed==Failed)return;
        var window=GetActiveWindow();if(window==IntPtr.Zero)return;
        var status=Failed?"FAILED":Completed?"COMPLETED / HELD ENDPOINT":started?"RUNNING":"READY / SPACE TO START";
        observation.State.Stores.Fuel.TryToKilograms(out var fuel);observation.State.Stores.Oxidizer.TryToKilograms(out var oxidizer);
        var span=title.AsSpan();
        if(!span.TryWrite(CultureInfo.InvariantCulture,$"NovaCore - SRV-01 reusable parts - {status} | interval {observation.State.Frontier}/128 | main {(ActiveExhaust&&observation.State.Actual.MainOn?"ON":"OFF")} | RCS {(ActiveExhaust?observation.State.Actual.Jets:0):X4} | fuel {fuel:F6} kg oxide {oxidizer:F6} kg",out var written)||written>=title.Length-1)throw new InvalidOperationException("SRV01 status capacity");
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
