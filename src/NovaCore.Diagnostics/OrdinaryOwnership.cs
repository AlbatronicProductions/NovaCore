using System.Diagnostics;
using System.Text.Json;

namespace NovaCore.Diagnostics;

// Leases describe session ownership, never an age-based stale-lock heuristic.
internal static class OrdinaryOwnership
{
    internal static FileStream Create(string path)=>new(path,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.ReadWrite);
    internal static FileStream Join(string path)=>new(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
    internal static void Write(string path,object value)
    {
        OrdinaryStorage.Json(path,value);
    }
    internal static object ProcessIdentity(Guid session)
    {
        using var p=Process.GetCurrentProcess();
        return new {schema=1,session,pid=p.Id,startUtcTicks=p.StartTime.ToUniversalTime().Ticks};
    }
    internal static void Finalize(string path,Guid id,OrdinaryRecovery r)
    {
        if(!r.Complete)return;
        using var session=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(path,"session.json")));
        var s=session.RootElement;
        // Legacy recordings do not acquire synthetic closure provenance.
        if(!s.TryGetProperty("openedQpc",out var q)||!s.TryGetProperty("openedUtcTicks",out var utc))return;
        long close=checked((long)r.State.LastPhase[20].Words[1]);
        long elapsed=checked(close-q.GetInt64()),frequency=s.GetProperty("qpcFrequency").GetInt64();
        if(elapsed<0||frequency<=0)throw new InvalidDataException("Closure clock provenance.");
        long ticks=checked(utc.GetInt64()+(long)((decimal)elapsed*TimeSpan.TicksPerSecond/frequency));
        if(ticks<=0||ticks>DateTime.MaxValue.Ticks)throw new InvalidDataException("Closure UTC range.");
        Write(Path.Combine(path,"finalized.json"),new{schema=1,session=id,disposition="COMPLETE",durable=r.DurableSequence,revision=r.Revision,checkpoint=r.CheckpointSequence,closeQpc=close,closeUtcTicks=ticks,
            sessionSha256=Hash(Path.Combine(path,"session.json")),ownerSha256=Hash(Path.Combine(path,"observer.json")),
            head0=Hash(Path.Combine(path,"head0.bin")),head1=Hash(Path.Combine(path,"head1.bin")),recoverySha256=Hash(Path.Combine(path,"recovery.json"))});
    }
    internal static string Hash(string path)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
}
