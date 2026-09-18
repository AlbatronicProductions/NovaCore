using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Launcher;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;

internal static class PlayerEngineControlTests
{
    private static int checks;
    private static readonly AssemblyStockCatalog Catalog=AssemblyStockCatalog.LoadDefault();
    private static readonly CompiledAssemblyDesign Design=Catalog.Resolve("novacore.stock.SRV01.FourHorn");
    private static readonly NativeInputState On=new(){ControlInputActive=1,EngineActions=NativeEngineActions.On};
    private static readonly NativeInputState Off=new(){ControlInputActive=1,EngineActions=NativeEngineActions.Off};
    private static void Check(bool pass,string message)
    {if(!pass)throw new InvalidOperationException("PLAYER ENGINE: "+message);checks++;}
    private static AssemblyApplicationSession Session(CompiledAssemblyDesign? design=null,bool live=true)
    {
        var commands=Enumerable.Repeat(new AssemblyCommand(false,null,0,0,15625),128).ToArray();
        if(!live)for(var i=0;i<commands.Length;i++)commands[i]=commands[i] with {MainOn=i<32};
        var s=AssemblyApplicationSession.Create(new(design??Design,new(new(201),new(1),new(2),"SRV"),"player-proof",new(default,default,DoubleQuaternion.Identity,default),default,commands));
        if(live)s.EnableLiveControl();return s;
    }
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"observe");return o;}
    private static void Next(AssemblyApplicationSession s)
    {
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,Observe(s).HostSequence+1,new(15625)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"single interval service");
    }
    private static void Same(AssemblyApplicationSession a,AssemblyApplicationSession b)
    {
        var x=Observe(a);var y=Observe(b);
        Check(x.State==y.State&&x.StateRevision==y.StateRevision&&x.TimelineRevision==y.TimelineRevision&&x.Clock==y.Clock&&x.HistoryCount==y.HistoryCount,"all physical state, mass, COM, inertia, stores, revisions, clock");
        for(var i=0;i<x.HistoryCount;i++)Check(a.Engine.TryGetAssemblyHistory(a.Authority,i,out var ar)&&b.Engine.TryGetAssemblyHistory(b.Authority,i,out var br)&&ar==br,"exact history including powered duration");
    }
    internal static void Run()
    {
        Check(System.Text.Json.JsonSerializer.Deserialize<NovaCoreScenarioPreset>("11")==NovaCoreScenarioPreset.StockAssembly&&System.Text.Json.JsonSerializer.Serialize(NovaCoreScenarioPreset.StockAssembly)=="11"&&(int)NovaCoreScenarioPreset.PlayerFlightControls==12,"legacy numeric launcher preset identity");
        Check(SampleOptions.TryParse(["--scene=sol","--player-flight-controls"],out var opt,out _)&&opt.PlayerFlightControls&&!opt.UsesFloridaSlab,"explicit proving context");
        foreach(var arg in new[]{"--surface-site=florida-launch","--benchmark-frames=20","--solar-warp-traversal","--focus=earth","--scene=stock-assembly"})
            Check(!SampleOptions.TryParse(["--scene=sol","--player-flight-controls",arg],out _,out _),"reject conflicting route "+arg);
        Check(ScenarioCatalog.TryCreateConfiguration(NovaCoreScenarioPreset.PlayerFlightControls,null,NovaCoreWindowMode.Windowed,NovaCoreResolutionPreset.Resolution1280x720,NovaCoreDiagnosticsMode.Normal,1920,1080,out var config,out _)&&LaunchCommandBuilder.BuildArguments(config!).Contains("--player-flight-controls"),"launcher reaches proving context");
        using(var s=Session())using(var recorded=Session(live:false))
        {
            var input=new PlayerFlightControlInput(s);var before=Observe(s);
            for(var i=0;i<1024;i++){input.Apply(default);input.Apply(Off);input.Apply(On with {ControlInputActive=0});input.Apply(On with {EngineActions=NativeEngineActions.On|NativeEngineActions.Off});}
            Check(!input.Started&&input.Observation.AdmissionCount==0&&Observe(s)==before,"READY no input/OFF/conflict/inactive cannot consume time, journal or resources");
            Check(input.Apply(On).Status==AssemblyControlStatus.Admitted&&input.Started&&Observe(s)==before,"ignition admits only request");
            for(var i=0;i<128;i++)
            {
                if(i==32)Check(input.Apply(Off).Status==AssemblyControlStatus.Admitted,"cutoff");
                var count=input.Observation.AdmissionCount;
                input.Apply(i<32?On:Off);input.Apply(default);input.Apply((i<32?Off:On) with {ControlInputActive=0});
                Check(input.Observation.AdmissionCount==count,"repeat/release/inactive do not alter latch");
                Next(s);Next(recorded);Same(s,recorded);
                if(i==31){Check(Observe(s).State.Stores!=before.State.Stores&&Observe(s).State.Actual.MainOn,"real consumption and thrust");}
                if(i==32){Check(!Observe(s).State.Actual.MainOn&&Observe(s).State.Motion.VelocityO.X>0,"cutoff retains momentum");}
                if(i==63)
                {
                    using var restored=AssemblyApplicationSession.Restore(Catalog,s.Save());
                    var restoredInput=new PlayerFlightControlInput(restored);
                    Same(s,restored);Check(restoredInput.Started&&!restoredInput.Observation.Requested.MainOn,"restore only admitted latch, no transient edge");
                }
            }
            var end=Observe(s);Check(input.Apply(On).Status==AssemblyControlStatus.Terminal&&Observe(s)==end,"terminal cannot restart");
            using var replay=AssemblyApplicationSession.Restore(Catalog,s.Save());Same(s,replay);
            s.Dispose();Check(input.Apply(On).Status==AssemblyControlStatus.Retired,"disposed owner refuses UI");
        }
        foreach(var amount in new[]{0d,25d/1024,25d/2048})
        {
            var design=CompiledAssemblyDesign.Compile(AssemblyJson.Write(Design.Data with {Design=Design.Data.Design with {InitialFuelKg=amount*2/5,InitialOxidizerKg=amount*3/5}}));
            using var s=Session(design);var input=new PlayerFlightControlInput(s);input.Apply(On);
            for(var i=0;i<10;i++)Next(s);
            var exhausted=Observe(s);Check(input.Observation.Requested.MainOn&&!exhausted.State.Actual.MainOn&&exhausted.State.Stores==default,"requested ON cannot bypass empty/depleted feed");
            Next(s);Check(Observe(s).State.Stores==exhausted.State.Stores&&Observe(s).State.ResourceRevision==exhausted.State.ResourceRevision,"no-feed no debit");
        }
        using(var s=Session())
        {
            var demand=new AssemblyPilotDemand(1,-1,1);
            Check(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(false,demand,true)).Status==AssemblyControlStatus.Admitted,"READY pilot admitted separately from engine intent");
            using var restored=AssemblyApplicationSession.Restore(Catalog,s.Save());
            var input=new PlayerFlightControlInput(restored);
            Check(!input.Started&&input.Observation.Requested.Pilot==demand,"READY demand restore does not start episode");
            Check(input.Apply(On with {PilotKeys=NativePilotKeys.W|NativePilotKeys.D|NativePilotKeys.Q}).Status==AssemblyControlStatus.Admitted&&input.Started&&input.Observation.Requested.Pilot==demand,"Z after pilot-only preserves demand and ignites");
            Next(restored);input.Apply(Off with {PilotKeys=NativePilotKeys.W|NativePilotKeys.D|NativePilotKeys.Q});
            Check(!input.Observation.Requested.MainOn&&input.Observation.Requested.Pilot==demand,"X preserves pilot demand");
            using var continuation=AssemblyApplicationSession.Restore(Catalog,restored.Save());
            Check(new PlayerFlightControlInput(continuation).Started,"restore finds ignition after READY pilot records");
        }
        Presentation();Measure();Console.WriteLine($"PLAYER_ENGINE PASS checks={checks}");
    }
    private static void Presentation()
    {
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        AssemblyFloridaSiteTests.PrepareCameraFixture();
        Check(SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var solar,out _),"Solar");
        using var scene=new StockAssemblyDevelopmentScene(solarWorld:solar,playerControls:true);
        var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);
        var initial=scene.Observation;var focus=scene.PrepareFocusObservation();var identity=scene.PlayerInput!.Identity;
        Check(solar!.BindActiveVessel(focus)&&solar.RefocusActiveVessel(camera),"bind free-flight active vessel");
        scene.Start();scene.Advance(new(2_000_000));scene.AdvanceLive(true);
        Check(scene.Observation==initial&&!scene.Completed,"all alternate starts leave READY unconsumed");
        foreach(var target in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Mars})
        {
            Check(solar.Focus(camera,target)&&scene.PlayerInput.Identity==identity,"view never changes control");
            Check(solar.RefocusActiveVessel(camera)&&scene.PlayerInput.Identity==identity,"F only view");
        }
        scene.ApplyPlayerInput(On);scene.Advance(new(15625));
        Check(scene.Observation.State.Frontier==1&&scene.Observation.State.Actual.MainOn&&scene.ActiveExhaust,"first admitted ignition starts fresh physical episode");
        var render=new RenderFrameSubmission(scene.RenderCapacity);scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);
        Check(render.ObjectCount==scene.InitialSnapshot.Count+1,"realized main emits one exhaust");
        var powered=scene.Observation;scene.ApplyPlayerInput(Off);scene.Advance(new(15625));
        Check(!scene.Observation.State.Actual.MainOn&&scene.Observation.State.Stores==powered.State.Stores&&scene.Observation.State.Motion.VelocityO==powered.State.Motion.VelocityO&&scene.Observation.State.Motion.PositionO.X>powered.State.Motion.PositionO.X,"cutoff stops debit, preserves moving endpoint");
        scene.BuildSubmission(default,new(camera.Position.Value,new(1)),render);Check(render.ObjectCount==scene.InitialSnapshot.Count,"cutoff suppresses exhaust");
        for(var i=2;i<128;i++)scene.Advance(new(15625));
        var terminal=scene.Observation;scene.ApplyPlayerInput(On);scene.Start();scene.Advance(new(1_000_000));
        Check(scene.Completed&&!scene.Failed&&!scene.ActiveExhaust&&scene.Observation==terminal,"finite terminal held, no restart");
    }
    private static void Measure()
    {
        using var s=Session();var input=new PlayerFlightControlInput(s);input.Apply(On);
        for(var i=0;i<10000;i++){input.Apply(On);input.Apply(default);}
        var allocated=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        for(var i=0;i<10000;i++){input.Apply(On);input.Apply(default);}
        var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Check(bytes==0,"warmed held/no-input path zero allocation");
        Console.WriteLine($"PLAYER_INPUT_COST calls=20000 elapsed_ms={elapsed:R} bytes={bytes}");
    }
}
