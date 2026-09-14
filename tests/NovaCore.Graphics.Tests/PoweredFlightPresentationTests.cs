using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Actuation;

internal static class PoweredFlightPresentationTests
{
    private static void Check(bool value,string contract){if(!value)throw new InvalidOperationException("Powered presentation: "+contract);}
    internal static void Run()
    {
        var scene=new PoweredFlightDevelopmentScene();var initial=scene.Observation;
        Check(scene.Status.StartsWith("READY",StringComparison.Ordinal)&&initial.Clock.Debt.Ticks==0,"ready before host time");
        scene.Advance(new(1000000));Check(scene.Observation==initial,"preparation cannot admit startup time");scene.Start();
        var submission=new RenderFrameSubmission(scene.InitialSnapshot.Count);
        scene.Advance(new(16665));Check(scene.Observation.Actuator.Frontier==0,"hold until funded interval");scene.Advance(new(1));
        var first=scene.Observation;Check(first.Actuator.Frontier==1&&first.Endpoint.PositionRoot.X>0&&first.Actuator.Activity==ActualEngineActivity.ProducingOutput,"actual published acceleration");
        scene.BuildSubmission(default,new(new(20,8,30),new(1)),submission);var body=scene.PresentedBody();
        Check(body.RootPosition.Value==first.Endpoint.PositionRoot&&body.RootOrientation==first.Endpoint.BodyToRoot,"render authority copied canonical endpoint");
        scene.BuildSubmission(default,new(new(-10,20,80),new(1)),submission);Check(scene.Observation==first,"camera independent");
        scene.Advance(new(8_000_000-16666));Check(scene.Observation.Actuator.Frontier==5,"delayed host input services at most four intervals");
        for(var i=0;i<119;i++)scene.Advance(default);
        var final=scene.Observation;
        Check(final.Actuator.Frontier==480&&final.Resource.RemainingUnits.IsZero&&final.Actuator.Activity==ActualEngineActivity.EnabledNoFeed&&
            final.Clock.Time.Ticks==8_000_000&&final.Clock.Debt.Ticks==0&&scene.Status.StartsWith("COMPLETED",StringComparison.Ordinal),"depletion then exact finite completion");
        scene.Advance(new(1000000));Check(scene.Observation==final,"final endpoint held");
        var failure=new PoweredFlightDevelopmentScene();failure.Start();failure.Advance(new(-1));
        Check(failure.Status.StartsWith("FAILED",StringComparison.Ordinal)&&failure.Observation.Actuator.Frontier==0,"failed input holds authoritative source");
        Check(SampleOptions.TryParse(["--scene=powered-free-flight"],out var options,out _)&&!options.UseProductionEarth,"separate launch route");
        Console.WriteLine("POWERED_PRESENTATION PASS canonical_pose/ready/start/acceleration/depletion/camera/backlog/final_hold/failed_input");
    }
}
