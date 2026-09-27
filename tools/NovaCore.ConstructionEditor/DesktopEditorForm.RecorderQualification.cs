using NovaCore.Diagnostics;
using NovaCore.Interop;
using System.Diagnostics;
using System.Text.Json;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private bool RecorderQualification=>qualificationPath is not null&&qualificationTankDefinition=="minimum-recorder";
    private readonly BenignRecorderRoute recorderRoute=new();
    private readonly List<object> recorderCoverage=new(64);
    private long recorderStart;
    private bool recorderCovered;
    private void RecorderBeginFrame()
    {
        try{
            if(recorderStart==0)recorderStart=Stopwatch.GetTimestamp();
            if(Stopwatch.GetElapsedTime(recorderStart).TotalSeconds>40)throw new InvalidDataException("Benign recorder route deadline.");
            if(recorderRoute.RequestFocus()&&(solar is null||flightCamera is null||!solar.Focus(flightCamera,NativePresentationFocus.Earth)))throw new InvalidDataException("Production Earth-focus controller refused.");
        }catch(Exception e){FailQualification(e);CloseApproved();}
    }
    private void RecorderObserve(NativeFrameSubmission* frame)
    {
        try{
            var scene=solar!;var body=scene.FocusedBody;var gpu=frame->PlanetaryGpu;
            double x=(double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,y=(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,z=(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ;
            double distance=Math.Sqrt((flightCamera!.Position.Value-body.Position.Value).LengthSquared);
            var value=new BenignRecorderObservation(body.BodyId,((ulong)frame->PlanetaryPresentation.BodyIdHigh<<32)|frame->PlanetaryPresentation.BodyIdLow,body.RadiusMetres,distance,Math.Sqrt(x*x+y*y+z*z),scene.FocusFramingDistance(body),scene.IsPaused,editing,flight is not null,scene.Rate.Numerator==scene.Rate.Denominator,frame->PlanetaryPresentation.Enabled!=0);
            var step=recorderRoute.Observe(Stopwatch.GetTimestamp(),Stopwatch.Frequency,value);
            if(step==BenignRecorderStep.StartMeasurement)MinimumRecorder.StartMeasurement();
            if(step==BenignRecorderStep.StartMeasurement||step==BenignRecorderStep.Complete||recorderRoute.HeldFrames>0&&recorderRoute.HeldFrames%120==0){
                MinimumRecorder.RouteMarker(body.BodyId,distance,body.RadiusMetres,(ulong)scene.CurrentTime.Ticks);
                if(recorderCoverage.Count<64)recorderCoverage.Add(new{step=step.ToString(),value,epoch=scene.CurrentTime.Ticks,heldFrames=recorderRoute.HeldFrames});
            }
            if(step==BenignRecorderStep.Complete){recorderCovered=true;MinimumRecorder.StopMeasurement();CloseApproved();}
        }catch(Exception e){FailQualification(e);CloseApproved();}
    }
    private void RecorderReport()
    {
        if(!RecorderQualification||!recorderCovered||Environment.ExitCode!=0)return;
        var (header,samples)=MinimumRecorder.ReadMeasurement();
        if(samples.Any(s=>s.AllocationCalls!=0||s.AllocationBytes!=0))throw new InvalidDataException("Timed recorder-owned allocation.");
        var us=samples.Select(s=>s.Ticks*1e6/header.Frequency).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(qualificationPath!))!);
        File.WriteAllText(qualificationPath!,JsonSerializer.Serialize(new{judgment="ROUTE_AND_MEASUREMENT_COMPLETE_PENDING_DURABLE_RECONCILIATION",playerPass=false,coverage=recorderCoverage,heldFrames=recorderRoute.HeldFrames,header,producerMicroseconds=Distribution(us),producerCycles=Distribution(samples.Select(s=>(double)s.Cycles)),allocationScope="Actual recorder-owned allocations; independent global-new offline probe covers hot-path bypasses.",samples},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
    }
}
