using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Actuation;

internal static class PoweredContactPresentationTests
{
    private static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Powered contact presentation: "+message);}
    internal static void Run()
    {
        using var scene=new PoweredContactDevelopmentScene();var initial=scene.Observation;
        Check(scene.Status.StartsWith("READY",StringComparison.Ordinal)&&initial.Clock.Debt.Ticks==0,"cold ready");
        scene.Advance(new(1_000_000));Check(scene.Observation==initial,"no startup debt");
        scene.PressSpace();var powered=scene.Observation;
        Check(powered.Actuator.Frontier==1&&powered.Actuator.Activity==ActualEngineActivity.ProducingOutput&&
            powered.Clock.Time.Ticks==16666&&powered.Resource.RemainingUnits!=initial.Resource.RemainingUnits&&!powered.Resource.RemainingUnits.IsZero,"genuine inspectable powered endpoint");
        var submission=new RenderFrameSubmission(2);scene.BuildSubmission(default,new(new(6,4,8),new(1)),submission);
        var body=scene.PresentedBody();Check(body.RootPosition.Value==powered.Endpoint.PositionRoot&&body.RootOrientation==powered.Endpoint.BodyToRoot&&body.Scale==new NovaCore.Core.Double3(2,1,1),"copied canonical geometry");
        scene.BuildSubmission(default,new(new(-10,20,30),new(1)),submission);Check(scene.Observation==powered,"camera cannot mutate physics");
        scene.PressSpace();Check(scene.Observation.Actuator.Frontier==2&&scene.Observation.Resource.RemainingUnits.IsZero&&
            scene.Observation.Actuator.Activity==ActualEngineActivity.EnabledNoFeed&&scene.Observation.Clock.Time.Ticks==33333,"genuine inspectable exact exhaustion");
        scene.PressSpace();scene.Advance(new(20_000_000-33333));Check(scene.Observation.Actuator.Frontier==6,"bounded four interval catchup");
        for(var i=0;i<299;i++)scene.Advance(default);
        var final=scene.Observation;Check(final.Actuator.Frontier==1200&&final.Clock.Time.Ticks==20_000_000&&final.Clock.Debt.Ticks==0&&final.HistoryCount==1200&&scene.Status.StartsWith("COMPLETED",StringComparison.Ordinal),"finite dry completion");
        scene.Advance(new(1_000_000));scene.PressSpace();Check(scene.Observation==final,"held endpoint");
        Check(SampleOptions.TryParse(["--scene=powered-contact"],out var options,out _)&&!options.UseProductionEarth,"separate existing renderer route");
        Console.WriteLine("POWERED_CONTACT_PRESENTATION PASS ready/inspection/powered/exhaustion/canonical_geometry/camera/bounded_backlog/completion");
    }
}
