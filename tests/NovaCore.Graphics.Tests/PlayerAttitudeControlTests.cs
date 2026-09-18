using NovaCore.Core;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class PlayerAttitudeControlTests
{
    private static int checks;
    private static readonly AssemblyStockCatalog Catalog=AssemblyStockCatalog.LoadDefault();
    private static void Check(bool pass,string message)
    {if(!pass)throw new InvalidOperationException("PLAYER ATTITUDE: "+message);checks++;}
    private static AssemblyApplicationSession Session()
    {
        var plan=Enumerable.Repeat(new AssemblyCommand(false,null,0,0,15625),128).ToArray();
        var s=AssemblyApplicationSession.Create(new(Catalog.Resolve("novacore.stock.SRV01.FourHorn"),new(new(201),new(1),new(2),"SRV"),"player-attitude",new(default,default,DoubleQuaternion.Identity,default),default,plan));
        s.EnableLiveControl(execution:AssemblyControlExecution.PhysicalActuators);return s;
    }
    private static NativeInputState Input(uint keys,NativeEngineActions actions=0,uint active=1)=>new(){ControlInputActive=active,PilotKeys=(NativePilotKeys)keys,EngineActions=actions};
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"observe");return o;}
    private static void Next(AssemblyApplicationSession s)
    {
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,Observe(s).HostSequence+1,new(15625)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"service");
    }
    internal static void Run()
    {
        // Literal keyboard oracle, independent of FromOpposingRequests.
        for(uint bits=0;bits<64;bits++)using(var s=Session())
        {
            var input=new PlayerFlightControlInput(s);var initial=Observe(s);
            var expected=new AssemblyPilotDemand((sbyte)(((bits&1)!=0?1:0)-((bits&2)!=0?1:0)),(sbyte)(((bits&4)!=0?1:0)-((bits&8)!=0?1:0)),(sbyte)(((bits&16)!=0?1:0)-((bits&32)!=0?1:0)));
            input.Apply(Input(bits));
            Check(input.Observation.Requested.Pilot==expected&&!input.Started&&Observe(s)==initial,"all 64 chords request only; READY remains untouched");
            var count=input.Observation.AdmissionCount;
            for(var held=0;held<100;held++)input.Apply(Input(bits));
            Check(input.Observation.AdmissionCount==count,"held/repeat cannot grow journal");
            input.Apply(Input(bits,NativeEngineActions.On));
            Check(input.Started&&input.Observation.Requested.MainOn&&input.Observation.Requested.Pilot==expected,"combined ignition and attitude");
            for(var i=0;i<34;i++)Next(s);
            var powered=Observe(s);
            Check(powered.State.Stores!=initial.State.Stores,"exact owner realizes overlapping consumption");
            input.Apply(Input(bits,NativeEngineActions.Off));Next(s);
            var off=Observe(s);Check(!off.State.Actual.MainOn&&input.Observation.Requested.Pilot==expected,"cutoff preserves held axes");
            if(expected!=default)Check(off.State.Actual.Jets!=0&&off.State.Stores!=powered.State.Stores,"engine-off RCS remains physical");
            input.Apply(Input(0));Next(s);var released=Observe(s);
            Check(input.Observation.Requested.Pilot==default&&released.State.Actual.Jets==0&&released.State.Stores==off.State.Stores,"release clears demand and fresh RCS debit");
            var inertia=off.State.Mass.Inertia;
            var beforeMomentum=off.State.Motion.BodyToWorld.Rotate(inertia.Apply(off.State.Motion.AngularVelocityBody));
            var afterMomentum=released.State.Motion.BodyToWorld.Rotate(inertia.Apply(released.State.Motion.AngularVelocityBody));
            Check((afterMomentum-beforeMomentum).LengthSquared<1e-18,"release preserves world angular momentum within integrator precision");
            using var replay=AssemblyApplicationSession.Restore(Catalog,s.Save());
            Check(Observe(replay)==released,"keyboard canonical save replay");
        }
        using(var s=Session())
        {
            var input=new PlayerFlightControlInput(s);
            input.Apply(Input(1,NativeEngineActions.Off));
            Check(!input.Started&&input.LastResult.Admission.Requested.PilotOnly,"READY X plus axis is pilot-only, no OFF veto");
            input.Apply(Input(1,NativeEngineActions.On));Check(input.Started,"same-frontier Z still ignites");Next(s);
            input.Apply(Input(3));Check(input.Observation.Requested.Pilot.Pitch==0,"opposing pitch cancels");
            input.Apply(Input(2));Check(input.Observation.Requested.Pilot.Pitch==-1,"release one opposing key reveals remaining hold");
            input.Apply(Input(63,NativeEngineActions.Off,0));
            Check(input.Observation.Requested.Pilot==default&&input.Observation.Requested.MainOn,"focus/capture loss neutralizes axes, ignores engine edge");
            var count=input.Observation.AdmissionCount;input.Apply(Input(0));Check(input.Observation.AdmissionCount==count,"fresh focus without fresh native keydown stays neutral");
            input.Apply(Input(64));Check(input.LastResult.Status==AssemblyControlStatus.InvalidInput&&input.Observation.AdmissionCount==count,"unknown input refused");
            input.Apply(Input(21));using var restored=AssemblyApplicationSession.Restore(Catalog,s.Save());
            var fresh=new PlayerFlightControlInput(restored);fresh.Apply(Input(0));
            Check(fresh.Observation.Requested.Pilot==default&&fresh.Observation.Requested.MainOn,"restore has admitted state, no restored UI hold");
            for(var i=1;i<128;i++)Next(s);
            var terminal=Observe(s);Check(input.Apply(Input(42,NativeEngineActions.On)).Status==AssemblyControlStatus.Terminal&&Observe(s)==terminal,"terminal no restart or attitude mutation");
            s.Dispose();Check(input.Apply(Input(1)).Status==AssemblyControlStatus.Retired,"retired owner rejects input");
        }
        var raw=new NativeInputState{MoveLeft=1,MoveRight=1,MoveForward=1,MoveBackward=1,MoveDown=1,MoveUp=1,LookActive=1,MouseDeltaX=2,MouseDeltaY=3,MouseWheelDetents=1,CameraActions=NativeCameraActions.FocusActiveVessel,PresentationFocus=NativePresentationFocus.Moon,RateIncrease=1};
        var camera=PlayerFlightControlInput.CameraInput(raw);
        Check(camera.Equals(raw with {MoveLeft=0,MoveRight=0,MoveForward=0,MoveBackward=0,MoveDown=0,MoveUp=0}),"context strips only movement; focus/F/orbit/zoom preserved");
        Measure();Console.WriteLine($"PLAYER_ATTITUDE PASS checks={checks}");
    }
    private static void Measure()
    {
        var times=new double[1024];
        static AssemblyApplicationSession[] Fixtures()=>Enumerable.Range(0,8).Select(_=>Session()).ToArray();
        static PlayerFlightControlInput[] Adapters(AssemblyApplicationSession[] s)=>s.Select(x=>new PlayerFlightControlInput(x)).ToArray();
        static void Work(PlayerFlightControlInput[] inputs,double[]? timing)
        {
            var index=0;
            foreach(var input in inputs)for(var i=0;i<128;i++)
            {
                var request=Input((uint)((i&1)==0?21:42));var start=System.Diagnostics.Stopwatch.GetTimestamp();
                var result=input.Apply(request);
                if(timing is not null)timing[index++]=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if(result.Status!=AssemblyControlStatus.Admitted)throw new InvalidOperationException("measured pilot admission failed");
            }
        }
        var warm=Fixtures();var normal=Fixtures();var isolated=Fixtures();
        var a=Adapters(warm);var b=Adapters(normal);var c=Adapters(isolated);
        Work(a,null);
        var gc0=GC.CollectionCount(0);var bytes=GC.GetAllocatedBytesForCurrentThread();Work(b,times);
        var raw=GC.GetAllocatedBytesForCurrentThread()-bytes;var collections=GC.CollectionCount(0)-gc0;
        Check(GC.TryStartNoGCRegion(32*1024*1024),"input isolated measurement entry");
        long measured;
        try{bytes=GC.GetAllocatedBytesForCurrentThread();Work(c,null);measured=GC.GetAllocatedBytesForCurrentThread()-bytes;}
        finally{GC.EndNoGCRegion();}
        Check(measured==0,"changing held snapshot plus canonical admission exact zero allocation");
        bytes=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[128]);var positive=GC.GetAllocatedBytesForCurrentThread()-bytes;
        Check(positive>0,"allocation positive control");Array.Sort(times);
        Console.WriteLine($"PLAYER_ATTITUDE_COST samples={times.Length} median_ms={times[512]:R} p95_ms={times[972]:R} p99_ms={times[1013]:R} max_ms={times[^1]:R} normal_bytes={raw} gc0={collections} isolated_bytes={measured} positive_bytes={positive}");
        foreach(var s in warm.Concat(normal).Concat(isolated))s.Dispose();
    }
}
