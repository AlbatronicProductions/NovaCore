using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace NovaCore.Diagnostics;

public sealed record RecorderAdmission(bool Available,long TotalBytes,long OutstandingBytes,long SessionReservationBytes,long HeadroomBytes,string Status);

public sealed partial class RuntimeRetention
{
    public const long RuntimeCapBytes=512L*1024*1024, PreventiveBytes=400L*1024*1024;
    public const int ControlPublicationBytes=1024*1024;
    public const long NewSessionReservationBytes=OrdinaryStorage.MaximumSessionBytes;
    static bool RuntimePath(string path)=>Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).Equals(RuntimeRoot,StringComparison.OrdinalIgnoreCase);
    // Charge the whole qualified maximum until raw retirement, even after exit.
    // A sampled size followed by an exit observation must not release a promise:
    // the observer may have written its final metadata between those samples.
    long Outstanding((Dictionary<Guid,long> Sessions,long Other,List<string> Unknown) inventory,Guid? alreadyLocked=null)
    {
        long remaining=0;
        foreach(var pair in inventory.Sessions)
        {
            using var s=Json(Path.Combine(SessionPath(pair.Key),"session.json"));var v=s.RootElement;
            if(v.GetProperty("session").GetGuid()!=pair.Key)throw new InvalidDataException("Invalid session reservation");
            long observed=pair.Value;
            if(v.TryGetProperty("reservedBytes",out var bound)){
                if(bound.GetInt64()!=NewSessionReservationBytes)throw new InvalidDataException("Invalid session reservation");
            }else if(pair.Key!=alreadyLocked){
                using var dir=Open(SessionPath(pair.Key),true,false);var leases=new List<SafeFileHandle>();
                try{
                    foreach(string file in Directory.GetFiles(SessionPath(pair.Key)))leases.Add(Open(file,false,false));
                    if(!Exited(v.GetProperty("producer").GetInt32(),v.GetProperty("startUtcTicks").GetInt64()))throw new InvalidDataException("Active legacy producer has no bounded reservation");
                    string observer=Path.Combine(SessionPath(pair.Key),"observer.json");
                    if(File.Exists(observer)){using var o=Json(observer);if(!Exited(o.RootElement.GetProperty("pid").GetInt32(),o.RootElement.GetProperty("startUtcTicks").GetInt64()))throw new InvalidDataException("Active legacy observer has no bounded reservation");}
                    else if(!LegacyObserverAbsent())throw new InvalidDataException("Unresolved legacy persistence owner");
                    observed=Directory.GetFiles(SessionPath(pair.Key)).Sum(p=>new FileInfo(p).Length);
                }finally{DisposeAll(leases);}
            }
            remaining=checked(remaining+Math.Max(0,Math.Max(NewSessionReservationBytes,observed)-pair.Value));
        }
        return remaining;
    }
    static bool LegacyObserverAbsent()
    {
        // Legacy minimum sessions predate observer.json. Their known helper is
        // NovaCore.Recorder; conservatively refuse while any other incarnation
        // exists, in addition to exited producer + exclusive complete-file leases.
        foreach(var p in System.Diagnostics.Process.GetProcessesByName("NovaCore.Recorder"))
            using(p){if(p.Id!=Environment.ProcessId&&!p.HasExited)return false;}
        return true;
    }
    RecorderAdmission AdmissionState()
    {
        var i=Inventory();long total=checked(i.Other+i.Sessions.Values.Sum());
        if(i.Unknown.Count!=0)return new(false,total,0,NewSessionReservationBytes,0,RecorderLaunchPolicy.Exhausted+"; accounting incomplete");
        long outstanding=Outstanding(i),free=RuntimeCapBytes-total-outstanding;
        bool ok=free>=NewSessionReservationBytes+ControlPublicationBytes;
        return new(ok,total,outstanding,NewSessionReservationBytes,free,ok?"RECORDER CAPACITY RESERVED":RecorderLaunchPolicy.Exhausted);
    }
    internal RecorderAdmission InspectAdmission(){RecorderAdmission? result=null;Locked(()=>result=AdmissionState());return result!;}
    public static RecorderAdmission PrepareCapacity()
    {
        var owner=new RuntimeRetention();Directory.CreateDirectory(owner.root);
        owner.RollingExecute();return owner.InspectAdmission();
    }
    sealed class AdmissionLease(Mutex mutex,List<SafeFileHandle> anchors):IDisposable
    {public void Dispose(){DisposeAll(anchors);mutex.ReleaseMutex();mutex.Dispose();}}
    internal static IDisposable? AdmitSession(string path)
    {
        if(!RuntimePath(path))return null; // existing explicit CPU fixtures, never runtime cleanup authority
        return new RuntimeRetention().AcquireAdmission();
    }
    internal IDisposable AcquireAdmission()
    {
        var owner=this;Directory.CreateDirectory(owner.root);
        var mutex=new Mutex(false,owner.MutexName);bool held=false;var anchors=new List<SafeFileHandle>();
        try{
            // Startup is allowed to await the existing maintenance owner. A busy
            // owner is not evidence of exhausted storage or permission to omit
            // recording. This runs before the renderer/simulation exists.
            try{held=mutex.WaitOne(30000);}catch(AbandonedMutexException){held=true;}
            if(!held)throw new IOException("Recorder maintenance/admission owner did not finish; capacity not classified as exhausted");
            anchors=owner.Anchor();owner.RollingExecute();var state=owner.AdmissionState();
            if(!state.Available)throw new RecorderStorageExhaustedException(state.Status);
            return new AdmissionLease(mutex,anchors);
        }catch{DisposeAll(anchors);if(held)mutex.ReleaseMutex();mutex.Dispose();throw;}
    }
    void BoundControlWrite(long extra)
    {
        var i=Inventory();if(i.Unknown.Count!=0||extra<0||checked(i.Other+i.Sessions.Values.Sum()+Outstanding(i)+extra)>RuntimeCapBytes)
            throw new RecorderStorageExhaustedException(RecorderLaunchPolicy.Exhausted+"; control write refused");
    }
    // Only complete, positively quiescent directory inventories are replaceable.
    // Pin is copied into the lossless capsule; it never changes criticality.
    (long Order,Dictionary<string,byte[]> Raw,OrdinaryRecovery Recovery) RollingEvidence(Guid id)
    {
        string path=SessionPath(id);
        if(Directory.GetDirectories(path).Length!=0)throw new InvalidDataException("Nested session content; preserve");
        var names=Directory.GetFiles(path).Select(Path.GetFileName).Cast<string>().ToArray();
        if(names.Contains("raw-preserve.json"))throw new InvalidDataException("Exceptional RAW-PRESERVE evidence requires review");
        foreach(string n in new[]{"session.json","bank0.bin","bank1.bin","head0.bin","head1.bin","recovery.json","performance.json"})
            if(!names.Contains(n))throw new InvalidDataException("Incomplete session directory; preserve");
        var raw=new Dictionary<string,byte[]>(StringComparer.Ordinal);
        foreach(string name in names){long max=OrdinaryStorage.Maximum(name);using var f=new FileStream(Path.Combine(path,name),FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
            if(f.Length>max)throw new InvalidDataException("Unknown/oversized raw evidence; preserve");byte[] b=new byte[(int)f.Length];f.ReadExactly(b);raw.Add(name,b);}
        using var s=JsonDocument.Parse(raw["session.json"]);Unique(s.RootElement);var v=s.RootElement;
        if(v.GetProperty("session").GetGuid()!=id||!Exited(v.GetProperty("producer").GetInt32(),v.GetProperty("startUtcTicks").GetInt64()))throw new InvalidDataException("Active/unresolved producer");
        if(v.TryGetProperty("schema",out var schema)&&schema.GetInt32()!=2)throw new InvalidDataException("Unknown session schema");
        long utc=v.TryGetProperty("openedUtcTicks",out var epoch)?epoch.GetInt64():v.GetProperty("utc").GetDateTime().ToUniversalTime().Ticks;
        if(utc<=0||utc>DateTime.MaxValue.Ticks||utc<v.GetProperty("startUtcTicks").GetInt64()||v.GetProperty("qpcFrequency").GetInt64()<=0)throw new InvalidDataException("Invalid authoritative ordering");
        if(raw.TryGetValue("observer.json",out var observer)){
            using var o=JsonDocument.Parse(observer);Unique(o.RootElement);var ov=o.RootElement;
            if(ov.GetProperty("session").GetGuid()!=id||!Exited(ov.GetProperty("pid").GetInt32(),ov.GetProperty("startUtcTicks").GetInt64()))throw new InvalidDataException("Active/unresolved observer");
        }else if(v.TryGetProperty("schema",out _)||!LegacyObserverAbsent())throw new InvalidDataException("Observer identity unresolved; preserve");
        // Legacy ownership has no observer metadata; exclusive locks on both
        // banks/heads and the exited producer are required by the transaction.
        var recovered=OrdinaryJournal.RecoverStreams(id,n=>new MemoryStream(raw[n],false));
        return(utc,raw,recovered);
    }
    static string Digest(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    internal static byte[] StrongCapsule(Guid id,long utc,Dictionary<string,byte[]> raw,OrdinaryRecovery recovered)
    {
        using var output=new CapsuleBuffer();
        var identities=raw.OrderBy(p=>p.Key,StringComparer.Ordinal).ToDictionary(p=>p.Key,p=>new{bytes=p.Value.Length,sha256=Digest(p.Value)});
        byte[] evidence=JsonSerializer.SerializeToUtf8Bytes(new{schema=2,session=id,openedUtcTicks=utc,classification="PRESERVED_WITHOUT_RECLASSIFICATION",
            baseClassification=OrdinaryTermination.ClassifyBytes(id,recovered,n=>raw[n]),
            pinPreserved=raw.ContainsKey("preserve.pin"),rawBytes=raw.Values.Sum(x=>(long)x.Length),files=identities,
            recovery=JsonDocument.Parse(OrdinaryObserver.ReportBytes(recovered)).RootElement,
            stateSha256=Digest(OrdinaryCheckpoint.Encode(recovered.State)),
            reason="Bounded runtime window: complete older session replaced atomically by lossless forensic capsule",
            reproduction="Recover original bytes and run OrdinaryJournal recovery. Original contradictory reports, uncertainty and uncommitted tails remain byte-identical."});
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            void Add(string n,byte[] bytes){var e=zip.CreateEntry(n,CompressionLevel.Fastest);e.LastWriteTime=new DateTimeOffset(1980,1,1,0,0,0,TimeSpan.Zero);using var f=e.Open();f.Write(bytes);}
            Add("evidence.json",evidence);foreach(var p in raw.OrderBy(p=>p.Key,StringComparer.Ordinal))Add(p.Key,p.Value);
        }
        byte[] payload=output.ToArray();byte[] result=new byte[payload.Length+32];payload.CopyTo(result,0);SHA256.HashData(payload).CopyTo(result,payload.Length);return result;
    }
    internal static OrdinaryRecovery ValidateStrongCapsule(Guid id,byte[] bytes,Dictionary<string,byte[]>? expected=null)
    {
        if(bytes.Length<33||bytes.Length>MaximumCapsuleBytes||!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))throw new InvalidDataException("Capsule checksum/size");
        using var zip=new ZipArchive(new MemoryStream(bytes,0,bytes.Length-32,false),ZipArchiveMode.Read);
        if(zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=zip.Entries.Count)throw new InvalidDataException("Duplicate capsule entry");
        var raw=new Dictionary<string,byte[]>(StringComparer.Ordinal);JsonDocument? evidence=null;
        try{
            foreach(var e in zip.Entries){long bound=e.FullName=="evidence.json"?OrdinaryStorage.RecoveryMetadataBytes+65536:OrdinaryStorage.Maximum(e.FullName);
                if(e.Length<0||e.Length>bound)throw new InvalidDataException("Capsule expansion bound");byte[] b=new byte[(int)e.Length];using var f=e.Open();f.ReadExactly(b);if(f.ReadByte()!=-1)throw new InvalidDataException("Entry trailing data");
                if(e.FullName=="evidence.json")evidence=JsonDocument.Parse(b);else raw.Add(e.FullName,b);}
            if(evidence is null)throw new InvalidDataException("Missing capsule evidence");Unique(evidence.RootElement);var m=evidence.RootElement;
            if(m.GetProperty("schema").GetInt32()!=2||m.GetProperty("session").GetGuid()!=id||m.GetProperty("classification").GetString()!="PRESERVED_WITHOUT_RECLASSIFICATION")throw new InvalidDataException("Capsule identity");
            foreach(string required in new[]{"session.json","bank0.bin","bank1.bin","head0.bin","head1.bin","recovery.json","performance.json"})if(!raw.ContainsKey(required))throw new InvalidDataException("Incomplete capsule inventory");
            using var session=JsonDocument.Parse(raw["session.json"]);Unique(session.RootElement);var s=session.RootElement;
            long opened=s.TryGetProperty("openedUtcTicks",out var ticks)?ticks.GetInt64():s.GetProperty("utc").GetDateTime().ToUniversalTime().Ticks;
            if(s.GetProperty("session").GetGuid()!=id||m.GetProperty("openedUtcTicks").GetInt64()!=opened||opened<=0||opened>DateTime.MaxValue.Ticks)throw new InvalidDataException("Capsule session/epoch authority");
            var identities=m.GetProperty("files");if(!identities.EnumerateObject().Select(p=>p.Name).Order(StringComparer.Ordinal).SequenceEqual(raw.Keys.Order(StringComparer.Ordinal)))throw new InvalidDataException("Capsule inventory");
            foreach(var p in raw){var v=identities.GetProperty(p.Key);if(v.GetProperty("bytes").GetInt64()!=p.Value.Length||v.GetProperty("sha256").GetString()!=Digest(p.Value))throw new InvalidDataException("Capsule raw digest");}
            if(expected is not null&&(expected.Count!=raw.Count||expected.Any(p=>!raw.TryGetValue(p.Key,out var v)||!v.AsSpan().SequenceEqual(p.Value))))throw new InvalidDataException("Capsule changed original raw bytes");
            if(m.GetProperty("rawBytes").GetInt64()!=raw.Values.Sum(v=>(long)v.Length)||m.GetProperty("pinPreserved").GetBoolean()!=raw.ContainsKey("preserve.pin"))throw new InvalidDataException("Capsule accounting/pin");
            var r=OrdinaryJournal.RecoverStreams(id,n=>new MemoryStream(raw[n],false));using var report=JsonDocument.Parse(OrdinaryObserver.ReportBytes(r));
            if(!JsonElement.DeepEquals(report.RootElement,m.GetProperty("recovery"))||m.GetProperty("stateSha256").GetString()!=Digest(OrdinaryCheckpoint.Encode(r.State))||m.GetProperty("baseClassification").GetString()!=OrdinaryTermination.ClassifyBytes(id,r,n=>raw[n]))throw new InvalidDataException("Capsule normal-parser reconstruction mismatch");
            return r;
        }finally{evidence?.Dispose();}
    }
    void ReplaceWithCapsule(Guid id,Action<string>? cut)
    {
        using var tx=CreateTransaction(IntPtr.Zero,IntPtr.Zero,0,0,0,30000,"NovaCore complete raw to validated forensic capsule");Need(!tx.IsInvalid,"Transactional retirement unavailable; preserve");
        var locks=new List<SafeFileHandle>();bool committed=false;
        try{
            var directory=Open(SessionPath(id),true,true,tx);locks.Add(directory);
            foreach(string path in Directory.GetFileSystemEntries(SessionPath(id)))locks.Add(Open(path,false,true,tx));
            var e=RollingEvidence(id);byte[] capsule=StrongCapsule(id,e.Order,e.Raw,e.Recovery);ValidateStrongCapsule(id,capsule,e.Raw);cut?.Invoke("capsule-memory-validated");
            var i=Inventory(id,e.Raw.Values.Sum(v=>(long)v.Length));long before=checked(i.Other+i.Sessions.Values.Sum()),after=checked(before-i.Sessions[id]+capsule.Length);
            // In normal operation the transacted capsule's physical bytes also
            // fit before retirement. An inherited over-cap root is remediated
            // only by a strictly reducing atomic namespace replacement.
            if(before<=RuntimeCapBytes&&before+Outstanding(i,id)+capsule.Length>RuntimeCapBytes)throw new InvalidDataException("Capsule transient would exceed runtime cap; preserve raw");
            if(i.Unknown.Count!=0||after+Outstanding(i,id)>RuntimeCapBytes&&after>=before)throw new InvalidDataException("Replacement cannot increase inherited over-cap storage");
            if(before<=RuntimeCapBytes&&after+Outstanding(i,id)>RuntimeCapBytes)throw new InvalidDataException("Replacement exceeds runtime cap");
            using(var h=CreateFileTransactedW(CapsulePath(id),Read|0x40000000,1,IntPtr.Zero,1,NoFollow,IntPtr.Zero,tx,IntPtr.Zero,IntPtr.Zero)){
                Need(!h.IsInvalid,"Create transacted capsule");using var file=new FileStream(h,FileAccess.ReadWrite);file.Write(capsule);cut?.Invoke("capsule-written");file.Flush(true);cut?.Invoke("capsule-flushed");
                file.Position=0;byte[] reread=new byte[capsule.Length];file.ReadExactly(reread);if(!reread.AsSpan().SequenceEqual(capsule))throw new InvalidDataException("Persisted capsule changed");ValidateStrongCapsule(id,reread,e.Raw);cut?.Invoke("capsule-recovered");
            }
            cut?.Invoke("validated");for(int n=1;n<locks.Count;n++){int remove=1;Need(SetFileInformationByHandle(locks[n],4,ref remove,4),"Stage complete-session retirement");cut?.Invoke("file-staged");}
            for(int n=1;n<locks.Count;n++)locks[n].Dispose();int yes=1;Need(SetFileInformationByHandle(directory,4,ref yes,4),"Stage session directory");directory.Dispose();
            cut?.Invoke("before-commit");Need(CommitTransaction(tx),"Commit lossless complete-session replacement");committed=true;
        }finally{DisposeAll(locks);if(!committed)RollbackTransaction(tx);}
    }
    internal RetentionReport RollingExecute(Action<string>? cut=null,Guid? requested=null)
    {
        var retired=new List<Guid>();var failed=new List<string>();RetentionReport? result=null;
        try{Locked(()=>{
            var initial=Snapshot();var attempted=new HashSet<Guid>();
            while(true){
                var i=Inventory();long total=i.Other+i.Sessions.Values.Sum();if(i.Unknown.Count!=0)break;
                long outstanding=Outstanding(i);long target=Math.Min(PreventiveBytes,RuntimeCapBytes-NewSessionReservationBytes-ControlPublicationBytes-outstanding);
                if(total<=target)break;
                var candidates=new List<(Guid Id,long Order,int Rank)>();
                foreach(var id in i.Sessions.Keys){
                    try{using var dir=Open(SessionPath(id),true,false);var handles=new List<SafeFileHandle>();try{
                        foreach(string f in Directory.GetFiles(SessionPath(id)))handles.Add(Open(f,false,false));
                        var e=RollingEvidence(id);var d=Inspect(id,false);candidates.Add((id,e.Order,d.Clean&&d.Eligible?0:d.Classification=="ORDINARY"&&!d.Pinned?1:2));
                    }finally{DisposeAll(handles);}}catch{ /* Incomplete, active, raw-preserve or unknown evidence stays. */ }
                }
                // Newest raw is never a budget victim. Older pin/critical evidence
                // may change representation only through lossless validation.
                var newest=candidates.OrderByDescending(c=>c.Order).ThenBy(c=>c.Id).Take(3).Select(c=>c.Id).ToHashSet();
                var selected=candidates.Where(c=>!newest.Contains(c.Id)&&!attempted.Contains(c.Id)&&(!requested.HasValue||c.Id==requested.Value)).OrderBy(c=>c.Rank).ThenBy(c=>c.Order).ThenBy(c=>c.Id).ToArray();
                // Retirement class has priority over the ordinary newest-raw
                // preference: never compact critical evidence while a safely
                // retireable ordinary session can satisfy the same pressure.
                if(!requested.HasValue&&selected.Length>0&&selected[0].Rank>0){
                    var ordinary=candidates.Where(c=>c.Rank<selected[0].Rank&&!attempted.Contains(c.Id)).OrderBy(c=>c.Rank).ThenBy(c=>c.Order).ThenBy(c=>c.Id).ToArray();
                    if(ordinary.Length>0)selected=ordinary;
                }
                // Newest ordinary raw is a preference, not a reason to omit
                // recording when safe ordinary retirement can still make room.
                // Newest critical/pinned and exceptional RAW-PRESERVE stay.
                if(selected.Length==0&&!requested.HasValue&&!AdmissionState().Available)
                    selected=candidates.Where(c=>c.Rank<2&&!attempted.Contains(c.Id)).OrderBy(c=>c.Rank).ThenBy(c=>c.Order).ThenBy(c=>c.Id).ToArray();
                if(selected.Length==0){if(!requested.HasValue&&ConsolidateOrdinaryCapsule(cut,failed))continue;break;}var next=selected[0];attempted.Add(next.Id);
                try{if(next.Rank==0)Retire(next.Id,cut);else ReplaceWithCapsule(next.Id,cut);retired.Add(next.Id);}
                catch(Exception e){failed.Add(next.Id+": FULL RAW preserved: "+e.Message);}
                if(requested.HasValue)break;
            }
            var last=Snapshot();var state=AdmissionState();result=new(state.Available?"CAPACITY_AVAILABLE":"UNRECORDED_PLAYER_ONLY",initial.Before,last.After,
                initial.Decisions.Select(d=>retired.Contains(d.Session)?d with{Reason="RETIRED WHOLE SESSION: clean COMPLETE or lossless validated capsule; no criticality reclassification"}:d).ToArray(),retired.ToArray(),failed.ToArray(),state.Available?null:state.Status,last.OtherRetainedBytes,last.AccountingComplete,last.UnmeasuredEntries);
        });}catch(Exception e){var zero=new RetentionTotals(0,0,0,0,0,0,0,0);result=new("UNRECORDED_PLAYER_ONLY",zero,zero,[],[],[e.Message],RecorderLaunchPolicy.Exhausted,0,false,[e.Message]);}
        return result!;
    }
}
