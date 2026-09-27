using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Interop;

// Opt-in, bounded qualification replay through the ordinary camera-input handler.
// It never writes camera poses, renderer quality, simulation rate, or craft state.
internal sealed class BlackoutCameraQualification(int stage)
{
    private double elapsed,nextAction=1;
    private int actions;
    private bool approachComplete;
    internal bool TargetObserved {get;private set;}
    internal static BlackoutCameraQualification? FromEnvironment()
    {
        var value=Environment.GetEnvironmentVariable("NOVACORE_CAUSAL_ROUTE");
        if(value is null)return null;
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOVACORE_CAUSAL_MAPPING")))throw new InvalidOperationException("Camera qualification requires the independent observer.");
        return new(value switch {"distant-earth"=>1,"closer-earth"=>2,"ground"=>3,"horizon"=>4,"orbit"=>5,"matching"=>6,_=>throw new InvalidOperationException("Unknown camera qualification stage.")});
    }
    internal NativeInputState PrepareInput(in NativeInputState original,SolarSystemScene scene,CameraState camera)
    {
        if(original.DeltaSeconds<=0)return original; // Respect the application's pause overlay.
        elapsed+=Math.Min(.1,original.DeltaSeconds);
        if(elapsed>180)throw new InvalidOperationException("Camera qualification exceeded its bounded exposure.");
        if(elapsed<nextAction)return original;
        nextAction=elapsed+.4;
        var input=original;
        if(scene.FocusedBody.BodyId!=6){input.PresentationFocus=NativePresentationFocus.Earth;return input;}
        if(stage==1){TargetObserved=true;return input;}
        // Finish approach once. Earth continues moving between inputs, so a
        // near-exact clearance threshold must not restart zoom while turning.
        // This is a replay waypoint, not a renderer quality or camera limit.
        var target=stage==2?1e6:stage==3?1000d:100d;
        if(!approachComplete&&scene.SurfaceAltitudeMetres>target){input.MouseWheelDetents=1;return input;}
        approachComplete=true;
        if(stage<=3){TargetObserved=true;return input;}
        var radial=(camera.Position.Value-scene.FocusedBody.Position.Value).Normalized();
        double dot=Double3.Dot(radial,camera.Orientation.Rotate(-Double3.UnitZ));
        if(!TargetObserved&&dot<-.15){input.LookActive=1;input.MouseDeltaX=0;input.MouseDeltaY=-45;return input;}
        TargetObserved=true;
        if(stage==4)return input;
        input.LookActive=1;input.MouseDeltaX=stage==6&&actions/12%2!=0?-35:35;
        input.MouseDeltaY=dot<-.2?-10:dot>.1?10:0;
        if(stage==6&&actions%10==0)input.MouseWheelDetents=scene.SurfaceAltitudeMetres<30?-1:1;
        actions++;return input;
    }
}
