using System.Diagnostics;
using System.Runtime.InteropServices;
internal static class PayoffDriver
{
    private static void Main(string[] args)
    {
        Qualification.Repo=Path.GetFullPath(args[0]);Qualification.Output=Path.GetFullPath(args[1]);int processOrder=int.Parse(args[2]);
        StudyDriver.Require(!Directory.Exists(Qualification.Output),"fresh payoff output");Directory.CreateDirectory(Qualification.Output);
        var input=StudyDriver.Actual(Qualification.Repo);StudyControl.Trace=false;StudyControl.TangentOverride=null;StudyControl.AllowCold=false;
        int[] counts=[8,12,12];bool[] useD=[true,true,false];string[] labels=["D+8","D+12","no-D+12"];
        var owners=new[]{new RetainedOperator(input.Seed),new RetainedOperator(input.Seed),new RetainedOperator(input.Seed)};
        var timings=new[]{new double[1024],new double[1024],new double[1024]};var last=new RetainedOperator.Proposal[3];
        var seed=input.Seed;
        for(int i=0;i<128+1024;i++)for(int order=0;order<3;order++){
            int arm=(order+i+processOrder)%3;StudyControl.Sweeps=counts[arm];StudyControl.UseD=useD[arm];
            long start=Stopwatch.GetTimestamp();
            var status=owners[arm].Prepare(seed.Identity,seed.Generation,seed.Piece+1,PieceKind.Coast,input.Geometry,input.Duration,input.Body,input.Load,seed.Endpoint,input.Native,out last[arm],out _);
            long end=Stopwatch.GetTimestamp();
            if(status!=OperatorStatus.Ready)throw new InvalidOperationException("Payoff preparation refused: "+status);
            if(i>=128)timings[arm][i-128]=(end-start)*1e6/Stopwatch.Frequency;
        }
        var rows=new List<object>();
        for(int arm=0;arm<3;arm++){
            var t=timings[arm];var original=t.ToArray();Array.Sort(t);double Q(double p)=>t[(int)Math.Ceiling(p*t.Length)-1];
            var correctness=StudyDriver.Run(input,labels[arm],counts[arm],useD[arm]);
            StudyDriver.Require(correctness.Impulses.SequenceEqual(StudyDriver.Values(last[arm].Target.Cache))&&correctness.Endpoint==last[arm].Target.Endpoint,"Timed result differs from diagnostic comparison");
            StudyDriver.Require(owners[arm].Snapshot==seed,"Timing mutated accepted input");
            StudyDriver.Require(correctness.Pass==(arm!=0),"Unexpected Release accuracy classification");
            rows.Add(new{arm=labels[arm],samples=t.Length,warmup=128,median=(t[511]+t[512])/2,p95=Q(.95),p99=Q(.99),max=t[^1],units="microseconds",correctness,
                worst=original.Select((value,index)=>new{value,index}).OrderByDescending(x=>x.value).Take(5).ToArray()});
        }
        Qualification.Save("payoff.json",new{runtime=RuntimeInformation.FrameworkDescription,process=Environment.ProcessId,processOrder,
            workload="Unchanged actual historical tuple: complete basis/duration/load/D-or-no-D preparation, current feasibility, N sweeps, endpoint/proposal construction. No install/native/canonical world.",
            observations="Per-row observation calls removed from timing build; result checks outside timestamp interval; no GC/tiering settings changed",rows});
    }
}
