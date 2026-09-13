// Disposable 0-40 causal comparison, not article acceptance.
using System.Text.Json;
using System.Numerics;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class EngineeringContactArticleTests
{
    internal static void Coverage(string arm)
    {
        using var f=new Fixture(tilted:true);
        var probe=f.World.EnableCoverageProbe(arm);var rows=new List<object>();var peak=0d;var peakStep=0;
        for(var step=0;step<=40;step++)
        {
            var ticks=step==0?0:step%3==1?16666:16667;var dt=(float)(ticks/1_000_000d);
            probe.Begin(step,dt);var before=f.World.CoveragePose();
            if(step>0)
            {
                Check(f.Credit(ticks).Status==ContactHostCreditStatus.Accepted,"coverage credit");
                Check(f.Service().Published==1,"coverage one publication");
            }
            var after=f.World.CoveragePose();
            Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var endpoint)==LocalContactStatus.Success,"coverage endpoint");
            Check(f.Clock.CurrentTime.Ticks==1234567+(long)step*1_000_000/60 && f.Clock.PendingSimulationDebt.Ticks==0 &&
                f.Engine.State.Revision.Value==(ulong)step && f.Engine.ProcessedPersistentContactCount==step,"coverage accounting");
            var minimum=Geometry(endpoint.Motion.PositionRoot,endpoint.Motion.BodyToRoot).MinY;
            if(-minimum>peak){peak=-minimum;peakStep=step;}
            var distances=new double[3];
            for(var i=0;i<3;i++)
            {
                var child=f.World.ReadArticleChildForTest(i);var q=after.Q;var h=child.Dimensions*.5;var c=child.CentreAssembly;
                double a=2*((double)q.X*q.Y+(double)q.Z*q.W),b=1-2*((double)q.X*q.X+(double)q.Z*q.Z),r=2*((double)q.Y*q.Z-(double)q.X*q.W);
                distances[i]=after.P.Y+a*c.X+b*c.Y+r*c.Z-Math.Abs(a)*h.X-Math.Abs(b)*h.Y-Math.Abs(r)*h.Z;
            }
            foreach(var snapshot in probe.BeforeSolve)
            {
                Check(snapshot.Contacts.Length==probe.Presented.Length,"solver count");
                for(var i=0;i<snapshot.Contacts.Length;i++)
                {var a=snapshot.Contacts[i];var b=probe.Presented[i];Check(a.Offset==b.Offset && a.Normal==b.Normal && a.Depth==b.Depth,"solver actual copied contact");}
            }
            rows.Add(new{step,ticks,before,after,minimum,distances,credit=f.Clock.PendingSimulationDebt.Ticks,
                raw=probe.Candidates.Take(probe.CandidateCount).ToArray(),reduced=probe.Reduced,presented=probe.Presented,
                solverBefore=probe.BeforeSolve,solverAfter=probe.AfterSolve});
        }
        Check(probe.Replacements==(arm=="normal"?0:1),"exact intervention count");
        Console.WriteLine("COVERAGE_RESULT "+JsonSerializer.Serialize(new{arm,peak,peakStep,probe.Replacements,probe.SelectedChild,
            probe.SelectedRawFeature,probe.SelectedPrediction,probe.MaximumTransportDifference,rows},new JsonSerializerOptions{IncludeFields=true}));
    }
}
