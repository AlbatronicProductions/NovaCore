using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Diagnostics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class BenignRecorderRouteTests
{
    public static void Run()
    {
        int checks=0;
        void Require(bool ok,string message){checks++;if(!ok)throw new InvalidDataException(message);}
        foreach(bool current in new[]{false,true}){
            var frame=new ReferenceFrameId(1);
            SolarSystemScene? candidate;string error;
            Require(current?SolarSystemScene.TryCreate(frame,out candidate,out error):SolarSystemScene.TryCreateAt(frame,SimulationInstant.Zero,out candidate,out error),error);
            var scene=candidate!;
            var camera=new CameraState(new FramePosition(frame,Double3.Zero),DoubleQuaternion.Identity,scene.Projection,CameraMode.Free);
            var route=new BenignRecorderRoute();Require(route.RequestFocus()&&scene.Focus(camera,NativePresentationFocus.Earth),"Production Earth focus");
            BenignRecorderStep step=default;
            for(int tick=0;tick<=1500;tick++){
                var input=new NativeInputState{DeltaSeconds=1f/60,ViewportWidthPixels=3440,ViewportHeightPixels=1322};
                scene.ApplyPresentationInput(camera,input,out _,out _);
                Require(scene.TryAdvanceByHostDuration(SimulationDuration.FromSecondsRounded(input.DeltaSeconds),camera,out error),error);
                scene.Focus(camera,input.PresentationFocus);scene.EnforceFinalCameraInvariant(camera);
                var gpu=scene.GpuConstants(camera,1322);var presentation=scene.FocusedPresentation(camera);var body=scene.FocusedBody;
                double x=(double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,y=(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,z=(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ;
                var value=new BenignRecorderObservation(body.BodyId,((ulong)presentation.BodyIdHigh<<32)|presentation.BodyIdLow,body.RadiusMetres,
                    Math.Sqrt((camera.Position.Value-body.Position.Value).LengthSquared),Math.Sqrt(x*x+y*y+z*z),scene.FocusFramingDistance(body),scene.IsPaused,false,false,scene.Rate.Numerator==scene.Rate.Denominator,presentation.Enabled!=0);
                Require(BenignRecorderRoute.Valid(value),"Actual encoded production Earth observation: "+value);
                step=route.Observe(tick,60,value);
            }
            Require(step==BenignRecorderStep.Complete&&route.HeldFrames==1200,"Real controller held route complete");
        }
        Console.WriteLine($"PASS {checks} actual production camera/submission checks; CPU only, no Vulkan.");
    }
}
