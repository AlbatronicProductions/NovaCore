using System.Diagnostics;
using System.Text.Json;

// A single bounded reader task; hashing/sorting never blocks the observer heartbeat.
internal sealed class LiveFrozenAudit(string output,bool gpu)
{
    sealed record Checked(ulong Identity,ulong Frame,double WallMilliseconds,int Errors);
    readonly List<Checked> checks=new(256);
    Task<(FrozenSnapshot.Result Result,double Milliseconds)>? pending;
    long requested,started;
    internal long ValidatedIdentity {get;private set;}
    internal string? Failure {get;private set;}
    internal void Update(long durable)
    {
        if(Failure!=null)return;
        if(pending is {IsCompleted:true}) {
            try {
                var (result,ms)=pending.GetAwaiter().GetResult();
                if(result.Identity!=(ulong)requested||result.Synthetic==gpu||result.Errors.Count!=0)
                    throw new InvalidDataException("Live frozen identity/membership check failed: "+string.Join("; ",result.Errors));
                if(checks.Count==256)throw new InvalidDataException("Live frozen audit capacity");
                checks.Add(new(result.Identity,result.Frame,ms,result.Errors.Count));ValidatedIdentity=requested;
            }catch(Exception error){Failure=error.Message;}
            pending=null;
        }
        if(pending!=null) {
            if(durable>requested+1||Stopwatch.GetElapsedTime(started).TotalSeconds>3)Failure="Live frozen audit fell behind bounded capture window";
            return;
        }
        if(Failure!=null||durable<=ValidatedIdentity)return;
        if(ValidatedIdentity!=0&&durable!=ValidatedIdentity+1){Failure="Live frozen durable identity skipped validation";return;}
        requested=durable;started=Stopwatch.GetTimestamp();string path=Path.Combine(output,"frozen",$"snapshot-{durable%2}.bin");
        pending=Task.Run(()=>{long began=Stopwatch.GetTimestamp();var result=FrozenSnapshot.Read(path);return(result,Stopwatch.GetElapsedTime(began).TotalMilliseconds);});
    }
    internal void Finish(long durable)
    {
        // Child is already stopped. Still bound the final reader task.
        for(int i=0;i<2&&Failure==null;i++) {
            Update(durable);
            if(pending!=null)try{if(!pending.Wait(3000))Failure="Final frozen audit timeout";}catch(Exception error){Failure=error.GetBaseException().Message;}
        }
        Update(durable);
        File.WriteAllText(Path.Combine(output,"live-capture-validation.json"),JsonSerializer.Serialize(new{gpuExecution=gpu,ValidatedIdentity,Failure,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
}
