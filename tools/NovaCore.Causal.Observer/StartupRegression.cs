using System.Text.Json;
using NovaCore.Interop;

internal static unsafe class StartupRegression
{
    public static int Run(string output)
    {
        var results=new List<object>();int failures=0;
        void Case(string name,Action test){try{test();results.Add(new{name,passed=true});}catch(Exception e){failures++;results.Add(new{name,passed=false,error=e.Message});}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static long[] State(params double[] milestones){var s=new long[32];for(int i=0;i<milestones.Length;i++)s[6+i]=(long)(milestones[i]*1000);if(s[10]!=0)s[16]=1;if(s[11]!=0)s[17]=1;if(s[12]!=0)s[18]=2;return s;}
        static string? Check(long[] s,double now,bool exited=false)=>new StartupLifecycle(1000,1000).Update(s,(long)(now*1000),exited);
        var normal=State(2,3,4,5,5.1,5.2,5.3);
        Case("normal startup and explicit phases",()=>{
            string[] phases=["ui-handoff","ui-handoff","loading-handoff","native-initialization","first-render-submission","first-gpu-completion","steady-rendering","steady-rendering"];
            var policy=new StartupLifecycle(1000,1000);var s=new long[32];
            Require(policy.Update(s,1100,false)==null&&policy.Phase==phases[0],"initial UI phase");
            for(int i=0;i<7;i++){s[6+i]=normal[6+i];if(i>=4)s[16]=1;if(i>=5)s[17]=1;if(i>=6)s[18]=2;
                Require(policy.Update(s,s[6+i]+1,false)==null,"valid milestone rejected");Require(policy.Phase==phases[i+1],policy.Phase);}
            Require(policy.Update(s,80000,false)==null,"steady rendering retains startup deadline");
        });
        Case("slow valid UI plus loading exceeds old process 60s",()=>Require(Check(State(2,56,65,66,66.1,66.2,66.3),66.4)==null,"valid supervised handoff rejected"));
        Case("missing native handoff",()=>Require(Check(State(2,3),33.001)=="loading-handoff exceeded deadline","wrong boundary"));
        Case("native initialization incomplete",()=>Require(Check(State(2,3,4),34.001)=="native-initialization exceeded deadline","wrong boundary"));
        Case("renderer initialized without first frame",()=>Require(Check(State(2,3,4,5),10.001)=="first-render-submission exceeded deadline","wrong boundary"));
        Case("first-frame GPU stall",()=>Require(Check(State(2,3,4,5,5.1),6.101)=="first-gpu-completion exceeded deadline","wrong boundary"));
        Case("first completion without steady progress",()=>Require(Check(State(2,3,4,5,5.1,5.2),6.201)=="steady-rendering exceeded deadline","wrong boundary"));
        Case("UI bound never reset by ready signal",()=>Require(Check(State(60),61.001)=="ui-handoff exceeded deadline","UI deadline reset"));
        Case("post-click overall bound",()=>Require(Check(State(2,3,32.9,62.8),63.001)=="overall post-click startup exceeded 60s","overall startup bound missing"));
        Case("process overall cap is finite",()=>Require(Check(State(),121.001)!=null,"unbounded process startup"));
        Case("exact phase endpoints inclusive",()=>Require(Check(State(2,61,91,91.1,96.1,97.1,98.1),98.1)==null,"inclusive endpoints rejected"));
        Case("out of order publication rejected",()=>{var s=State(2,3,4);s[7]=0;Require(Check(s,5)!=null,"hole accepted");});
        Case("future or regressing milestone rejected",()=>Require(Check(State(2,3,2.5),4)!=null&&Check(State(2,3,5),4)!=null,"bad clock accepted"));
        Case("milestone cannot restart phase",()=>{var p=new StartupLifecycle(1000,1000);var s=State(2,3);Require(p.Update(s,4000,false)==null,"valid prefix rejected");s[7]=4000;Require(p.Update(s,5000,false)!=null,"reset accepted");});
        Case("bootstrap frame zero never qualifies",()=>{var s=(long[])normal.Clone();s[16]=0;Require(Check(s,6)!=null,"frame zero accepted");});
        Case("steady requires second distinct completed frame",()=>{var s=(long[])normal.Clone();s[18]=1;Require(Check(s,6)!=null,"duplicate completion accepted");});
        for(int phase=0;phase<=7;phase++) {int count=phase;Case("orderly shutdown phase "+count,()=>{
            var s=State(normal.Skip(6).Take(count).Select(t=>t/1000d).ToArray());double close=count==0?1.1:s[5+count]/1000d+.01;
            s[13]=(long)(close*1000);Require(Check(s,close+.5)==null,"valid shutdown rejected");s[15]=(long)((close+.5)*1000);
            Require(Check(s,close+.6,true)==null,"valid exit rejected");Require(Check(s,close+2.01)=="orderly shutdown exceeded 2s","shutdown bound lost");
        });}
        Case("exit without shutdown is not orderly",()=>Require(Check(normal,6,true)!=null,"unannounced exit accepted"));
        Case("late shutdown cannot hide first GPU stall",()=>{var s=State(2,3,4,5,5.1);s[13]=7100;Require(Check(s,7.2,true)=="first-gpu-completion exceeded deadline","stall erased by close");});
        Case("managed producer ownership and revoked UI handoff",()=>{
            string name="Local\\NovaCore.Startup.Test."+Guid.NewGuid().ToString("N");
            using var map=System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(name,256);using var view=map.CreateViewAccessor();
            byte* pointer=null;view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
            try{long* w=(long*)pointer;w[0]=DiagnosticStartup.Magic;w[1]=1;w[2]=256;w[24]=System.Diagnostics.Stopwatch.Frequency;w[4]=System.Diagnostics.Stopwatch.GetTimestamp();
                Environment.SetEnvironmentVariable("NOVACORE_STARTUP_MAPPING",name);
                DiagnosticStartup.Mark(DiagnosticStartup.UiReady);Require(w[3]==Environment.ProcessId&&w[6]!=0,"managed owner or UI stamp missing");
                Require(DiagnosticStartup.TryBeginLoading()&&w[7]!=0,"valid loading denied");long first=w[7];
                w[5]=1;Require(!DiagnosticStartup.TryBeginLoading()&&w[7]==first&&w[19]!=0,"revocation allowed loading or reset clock");
                w[5]=0;w[4]=System.Diagnostics.Stopwatch.GetTimestamp()-2*w[24];Require(!DiagnosticStartup.TryBeginLoading(),"expired observer admitted loading");
            }finally{view.SafeMemoryMappedViewHandle.ReleasePointer();Environment.SetEnvironmentVariable("NOVACORE_STARTUP_MAPPING",null);}
        });
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"startup-regressions.json"),JsonSerializer.Serialize(new{passed=failures==0,checks=results.Count,failures,clock="deterministic QPC model; no Vulkan",results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Startup policy: {results.Count-failures}/{results.Count} passed; no GPU.");return failures==0?0:1;
    }
}
