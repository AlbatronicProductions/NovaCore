using System.Diagnostics;
using System.Text.Json;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private bool SurfaceRetryQualification=>qualificationPath is not null&&qualificationTankDefinition=="surface-retry";
    private SurfaceRetryRoute surfaceRoute=new();
    private long surfaceGroundedHold=500_000;
    private long surfaceDisplayFrame;
    private readonly record struct SurfaceRow(SurfaceRetryObservation Physical,int Stage,double FrameMs,double ServiceMs,long ServiceAllocated,double ObserverMs,long ObserverAllocated,long DisplayFrame,long DebtBefore,long DebtAfter,long Admitted,ConstructionWorkProbe.Sample? Work,long InstrumentationAllocated,double InstrumentationMs,QualificationExecutionProbe.Sample? Execution=null);
    private readonly List<SurfaceRow> surfaceRows=new(10000);
    private long surfaceStart;
    private bool surfaceLoaded,surfaceComplete;
    private SurfaceRetryAction surfaceAction;
    private string? surfaceFailure;
    private NovaCore.Diagnostics.OrdinaryMapping? surfaceMapping;
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetFocus();
    private void SurfaceQualificationFocus()
    {
        // This input-driven engineering route must activate its own window,
        // just as WindowClick does. Child focus alone does not admit WM_KEYDOWN.
        var foreground=GetWindowThreadProcessId(GetForegroundWindow(),out _);var own=GetCurrentThreadId();
        var attached=foreground!=own&&AttachThreadInput(own,foreground,true);
        try{RequireQualification(SetForegroundWindow(Handle),"surface driver activates its own window");FocusViewport();}
        finally{if(attached)AttachThreadInput(own,foreground,false);}
        SurfaceQualificationCheckFocus();
    }
    private void SurfaceQualificationCheckFocus()
    {
        var child=GetWindow(viewport.Handle,5);
        var foreground=GetForegroundWindow()==Handle;var focused=child!=IntPtr.Zero&&GetFocus()==child;
        Console.WriteLine($"SURFACE_RETRY_INPUT foreground={foreground} viewportFocused={focused} action={surfaceAction}");
        RequireQualification(foreground&&focused,"surface driver requires real foreground viewport input");
    }
    private void SurfaceRetrySetup()
    {
        if(!SurfaceRetryQualification)return;
        // Qualification input only: lengthen settled 1x play without changing
        // service cadence, solver input, readiness, or normal player behavior.
        if(Environment.GetEnvironmentVariable("NOVACORE_QUALIFICATION_GROUNDED_HOLD_US") is {} hold){
            surfaceGroundedHold=long.Parse(hold,System.Globalization.CultureInfo.InvariantCulture);
            surfaceRoute=new SurfaceRetryRoute(surfaceGroundedHold);
        }
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaCore","MinimumRecorder");
        foreach(var path in Directory.GetFiles(root,"session.json",SearchOption.AllDirectories)){
            using var d=JsonDocument.Parse(File.ReadAllBytes(path));
            if(d.RootElement.GetProperty("producer").GetInt32()!=Environment.ProcessId||d.RootElement.GetProperty("startUtcTicks").GetInt64()!=Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks)continue;
            var id=d.RootElement.GetProperty("session").GetGuid();surfaceMapping=new("Local\\NovaCoreMinimum-"+id.ToString("N"),id,false);break;
        }
        if(surfaceMapping is not null&&(surfaceMapping.OwnerPid!=Environment.ProcessId||surfaceMapping.OwnerStartTicks!=Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks))throw new InvalidDataException("Recorder process incarnation differs.");
        if(surfaceMapping is null)throw new InvalidDataException("Native retry requires its canonical recorder session.");
        FormClosed+=(_,_)=>surfaceMapping.Dispose();
    }
    private void SurfaceRetryBegin()
    {
        surfaceDisplayFrame++;
        try{
            if(surfaceMapping!.Read(12)!=0||surfaceMapping.Read(13)!=0||surfaceMapping.Read(16)!=0)throw new InvalidDataException("Recorder fault/drop: stop native retry.");
            if(surfaceStart==0)surfaceStart=Stopwatch.GetTimestamp();
            if(Stopwatch.GetElapsedTime(surfaceStart).TotalSeconds>90)throw new InvalidDataException("Native surface retry wall deadline.");
            if(!surfaceLoaded){
                if(Stopwatch.GetElapsedTime(surfaceStart).TotalSeconds<2)return;
                var input=qualificationPath+".input.ncflight.json";
                // Same validated saved-flight loader and successor publication as Load flight.
                var next=ConstructionFlightScene.RestoreFlight(session.Catalog,File.ReadAllBytes(input),assetRoot,solar!,visuals);
                try{next.FloridaView.Solar.RetainRendererBuffers(solar!);}catch{next.Dispose();throw;}
                var previous=flight;solar=next.FloridaView.Solar;flight=next;compiled=next.Craft;previous?.Dispose();
                flight.MeasureQualificationService=true;LeaveEditor();surfaceLoaded=true;SurfaceQualificationFocus();MinimumRecorder.StartMeasurement();
                Console.WriteLine($"SURFACE_RETRY_BEGIN vessel={flight.Session.Binding.Identity} source={flight.Craft.Design.Digest} compiled={flight.Craft.Digest}");
            }
            if(surfaceAction is SurfaceRetryAction.Roll or SurfaceRetryAction.Release or SurfaceRetryAction.Ignite)SurfaceQualificationCheckFocus();
            switch(surfaceAction){
                case SurfaceRetryAction.Roll:PostFlightKey('Q',false);break;
                case SurfaceRetryAction.Release:ViewportMessage(0x101,'Q');break;
                case SurfaceRetryAction.Ignite:PostFlightKey('Z');break;
                case SurfaceRetryAction.Complete:surfaceComplete=true;MinimumRecorder.StopMeasurement();CloseApproved();break;
            }
            surfaceAction=SurfaceRetryAction.None;
        }catch(Exception e){surfaceFailure=e.ToString();FailQualification(e);CloseApproved();}
    }
    private void SurfaceRetryObserve()
    {
        if(!surfaceLoaded||surfaceComplete)return;
        try{
            if(flight!.Failed)throw new InvalidDataException("Frozen surface physics refused continuation.");
            if(surfaceRows.Count>=10000)throw new InvalidDataException("Bounded surface observation capacity.");
            var start=Stopwatch.GetTimestamp();var allocation=GC.GetAllocatedBytesForCurrentThread();
            var value=SurfaceRetryObservation.Read(flight.Session);surfaceAction=surfaceRoute.Observe(value);
            var row=new SurfaceRow(value,surfaceRoute.Stage,lastApplicationDelta*1000,flight.QualificationServiceMs,flight.QualificationServiceAllocated,Stopwatch.GetElapsedTime(start).TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-allocation,surfaceDisplayFrame,flight.QualificationDebtBefore,flight.QualificationDebtAfter,flight.QualificationAdmitted,flight.QualificationWork,flight.QualificationInstrumentationAllocated,flight.QualificationInstrumentationMs);
            surfaceRows.Add(row with {Execution=flight.QualificationExecution});
        }catch(Exception e){surfaceFailure=e.ToString();FailQualification(e);MinimumRecorder.StopMeasurement();CloseApproved();}
    }
    private void SurfaceRetryReport()
    {
        if(!SurfaceRetryQualification||!surfaceLoaded)return;
        MinimumRecorder.StopMeasurement();var (header,samples)=MinimumRecorder.ReadMeasurement();
        File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment=surfaceComplete&&Environment.ExitCode==0?"ROUTE_COMPLETE_PENDING_RECOVERY_AND_PERFORMANCE_REVIEW":"REVISE",playerPass=false,error=surfaceFailure,
            surfaceRoute.TerrainSeen,surfaceRoute.GroundedRcs,surfaceRoute.Powered,groundedHoldMicroseconds=surfaceGroundedHold,header,
            producerMicroseconds=Distribution(samples.Select(s=>s.Ticks*1e6/header.Frequency)),
            timedRecorderAllocations=samples.Sum(s=>(double)s.AllocationCalls),timedRecorderBytes=samples.Sum(s=>(double)s.AllocationBytes),
            serviceMs=Distribution(surfaceRows.Select(r=>r.ServiceMs)),observerMs=Distribution(surfaceRows.Select(r=>r.ObserverMs)),frameMs=Distribution(surfaceRows.Select(r=>r.FrameMs)),
            source=flight!.Craft.Design.Digest,vessel=flight.Session.Binding.Identity.ToString(),rows=surfaceRows},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
    }
}
