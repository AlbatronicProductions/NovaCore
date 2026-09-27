using System.Diagnostics;
using System.Text.Json;

namespace NovaCore.Diagnostics;

// Post-drain evidence only. Never consulted by a renderer/producer and never
// delays an ACK. Absence or incomplete observation grants no retirement right.
internal static class OrdinaryTermination
{
    static byte[] Read(string path){using var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);if(f.Length>(Path.GetFileName(path)=="recovery.json"?OrdinaryStorage.RecoveryMetadataBytes:1_048_576))throw new InvalidDataException("Termination metadata size");byte[] b=new byte[(int)f.Length];f.ReadExactly(b);return b;}
    static string Hash(string path)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Read(path)));
    internal static void Observe(string path,Guid id,Process producer)
    {
        try
        {
            bool exited=producer.WaitForExit(1000);
            Write(path,id,producer.Id,producer.StartTime.ToUniversalTime().Ticks,
                exited,exited?producer.ExitCode:null,DateTime.UtcNow.Ticks);
        }
        catch(Exception e){Console.Error.WriteLine("Producer termination unclassified; preserve raw: "+e.Message);}
    }
    internal static void Write(string path,Guid id,int pid,long start,bool exited,int? code,long observedUtc)
    {
        OrdinaryOwnership.Write(Path.Combine(path,"termination.json"),new{schema=1,session=id,producer=pid,startUtcTicks=start,
            exited,exitCode=code,observedUtcTicks=observedUtc,
            sessionSha256=OrdinaryOwnership.Hash(Path.Combine(path,"session.json")),
            ownerSha256=OrdinaryOwnership.Hash(Path.Combine(path,"observer.json")),
            head0=OrdinaryOwnership.Hash(Path.Combine(path,"head0.bin")),head1=OrdinaryOwnership.Hash(Path.Combine(path,"head1.bin")),
            recoverySha256=OrdinaryOwnership.Hash(Path.Combine(path,"recovery.json"))});
    }
    internal static string Classify(string path,Guid id,OrdinaryRecovery r)
    {try{return ClassifyCore(id,r,name=>Read(Path.Combine(path,name)));}catch{return "CRITICAL_OR_UNRESOLVED";}}
    internal static string ClassifyBytes(Guid id,OrdinaryRecovery r,Func<string,byte[]> read)
    {try{return ClassifyCore(id,r,read);}catch{return "CRITICAL_OR_UNRESOLVED";}}
    static string ClassifyCore(Guid id,OrdinaryRecovery r,Func<string,byte[]> read)
    {
        if(r.Corrupt||(r.State.Faults&~(long)OrdinaryFault.Protocol)!=0||r.State.Dropped!=0)
            return "CRITICAL_OR_UNRESOLVED";
        if(r.State.LastPhase.Values.Any(e=>e.Phase==OrdinaryPhase.Error||e.Edge==OrdinaryEdge.Exception||
            e.Phase is OrdinaryPhase.Acquire or OrdinaryPhase.Submit or OrdinaryPhase.Fence or OrdinaryPhase.Present or OrdinaryPhase.DeviceIdle or OrdinaryPhase.QueueIdle&&e.Result<0))
            return "CRITICAL_OR_UNRESOLVED";
        // A fully sealed but intentionally unsuccessful close can be routine.
        // Missing close, incomplete durability or outstanding GPU work cannot.
        if(r.DurableSequence==0||r.DurableSequence!=r.CommittedTarget||r.DurableSequence!=r.State.LastProducedObserved||
            !r.State.LastPhase.TryGetValue(20,out var close)||close.Sequence!=r.DurableSequence||
            close.Edge!=OrdinaryEdge.Return||close.Result is not (0 or -1)||close.Words[17]!=1||close.Words[18]!=r.DurableSequence||
            r.State.Open.Count!=0||r.State.Pending.Count!=0||r.State.PendingContexts.Count!=0||r.State.Resources.Count!=0)
            return "CRITICAL_OR_UNRESOLVED";
        using var s=JsonDocument.Parse(read("session.json"));
        using var t=JsonDocument.Parse(read("termination.json"));
        RuntimeRetention.Unique(s.RootElement);RuntimeRetention.Unique(t.RootElement);
        var a=s.RootElement;var b=t.RootElement;
        if(b.GetProperty("schema").GetInt32()!=1||b.GetProperty("session").GetGuid()!=id||
            b.GetProperty("producer").GetInt32()!=a.GetProperty("producer").GetInt32()||
            b.GetProperty("startUtcTicks").GetInt64()!=a.GetProperty("startUtcTicks").GetInt64()||
            !b.GetProperty("exited").GetBoolean()||b.GetProperty("exitCode").GetInt32()!=0||
            b.GetProperty("observedUtcTicks").GetInt64()<a.GetProperty("openedUtcTicks").GetInt64()||
            b.GetProperty("observedUtcTicks").GetInt64()>DateTime.MaxValue.Ticks)
            return "CRITICAL_OR_UNRESOLVED";
        foreach(var pair in new[]{("sessionSha256","session.json"),("ownerSha256","observer.json"),("head0","head0.bin"),("head1","head1.bin"),("recoverySha256","recovery.json")})
            if(b.GetProperty(pair.Item1).GetString()!=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(read(pair.Item2))))return "CRITICAL_OR_UNRESOLVED";
        return "ORDINARY";
    }
}
