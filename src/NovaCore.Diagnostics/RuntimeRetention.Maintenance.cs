using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NovaCore.Diagnostics;

public sealed record RecorderStorageNotice(RetentionReport Report,bool Warn,bool ManualReviewRequired,string Summary);

public sealed partial class RuntimeRetention
{
    public const long TotalWarningBytes=PreventiveBytes;
    // A retained event-window is a meaningful protected-storage increase.
    internal const long ProtectedGrowthBytes=OrdinaryJournal.EventBytes;
    string Acknowledgement=>Path.Combine(root,"storage-acknowledgement.json");
    string ReviewFile(Guid id)=>Path.Combine(root,"review-"+id.ToString("N")+".json");
    string ClassificationFile(Guid id)=>Path.Combine(root,"classification-"+id.ToString("N")+".json");
    void Locked(Action action)
    {
        using var mutex=new Mutex(false,MutexName);bool held;
        try{held=mutex.WaitOne(0);}catch(AbandonedMutexException){held=true;}
        if(!held)throw new IOException("Recorder maintenance owner is active; preserve");
        var anchors=new List<Microsoft.Win32.SafeHandles.SafeFileHandle>();
        try{anchors=Anchor();action();}finally{DisposeAll(anchors);mutex.ReleaseMutex();}
    }
    void AtomicMetadata(string path,byte[] bytes)
    {
        if(bytes.Length>ControlPublicationBytes)throw new InvalidDataException("Control metadata exceeds bounded publication");
        if(qualification is null)BoundControlWrite(bytes.Length);
        string temp=Path.Combine(root,"maintenance-"+Guid.NewGuid().ToString("N")+".tmp");
        try{using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes);f.Flush(true);}File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    RetentionReport Snapshot()
    {
        RetentionReport? result=null;
        Locked(()=>{
            var inventory=Inventory();var decisions=Scan().Select(d=>d with{Bytes=inventory.Sessions.GetValueOrDefault(d.Session,d.Bytes)}).ToArray();
            var totals=Totals(decisions,inventory.Other,CapsuleBytes());
            result=new("INSPECTED",totals,totals,decisions,[],[],null,inventory.Other,inventory.Unknown.Count==0,inventory.Unknown.ToArray());
        });
        return result!;
    }
    internal RecorderStorageNotice Notice()
    {
        var report=Snapshot();bool warn=false;
        Locked(()=>{
            if(report.After.TotalBytes<=TotalWarningBytes){
                if(File.Exists(Acknowledgement))AtomicMetadata(Acknowledgement,AckBytes(report));
                return;
            }
            warn=true;
            try{if(File.Exists(Acknowledgement))warn=ShouldWarn(report.After.TotalBytes,report.After.ExemptRawBytes,Bytes(Acknowledgement));}catch{warn=true;}
        });
        return Describe(report,warn);
    }
    internal static RecorderStorageNotice Describe(RetentionReport r,bool warn)
    {
        var a=r.After;bool manual=a.TotalBytes>TotalWarningBytes&&(a.ExemptRawBytes>0||!r.AccountingComplete||r.Failures.Length>0||r.OtherRetainedBytes>a.ForensicCapsuleBytes);
        string text=$"MinimumRecorder storage: {a.TotalBytes:N0} total bytes (warning threshold {TotalWarningBytes:N0}).\n"+
            $"Clean-session bytes: {a.CleanBytes:N0}\nOrdinary-abnormal RAW bytes: {a.OrdinaryAbnormalBytes:N0}\n"+
            $"Critical/pinned/unresolved RAW bytes: {a.ExemptRawBytes-a.ActiveBytes:N0}\nForensic-capsule bytes: {a.ForensicCapsuleBytes:N0}\nActive-session bytes: {a.ActiveBytes:N0}\n"+
            $"Other retained control/evidence bytes: {r.OtherRetainedBytes-a.ForensicCapsuleBytes:N0}\n"+
            (manual?"MANUAL REVIEW REQUIRED\n":"")+
            "Bounded rolling storage: preventive maintenance at 400 MiB; HARD TOTAL CAP 512 MiB. Before each session, maintenance makes room and reserves a complete recorder session. Whole eligible clean sessions retire first, then validated lossless ordinary/older critical capsules. PIN preserves evidence, not unlimited raw storage. Unrecorded play is exceptional when protected evidence prevents reservation; recorder-required qualification refuses.";
        return new(r,warn,manual,text);
    }
    public static RecorderStorageNotice? GetStorageNotice()
    {
        if(!Directory.Exists(RuntimeRoot))return null;
        return new RuntimeRetention().Notice();
    }
    public static RecorderStorageNotice SafeMaintenance()=>Describe(RunAndReport(),true);
    public static void AcknowledgeStorage(RecorderStorageNotice displayed)=>new RuntimeRetention().Acknowledge(displayed.Report);
    static string ProtectedIdentity(RetentionReport report)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
        report.Decisions.Where(d=>!report.Deleted.Contains(d.Session)&&(d.Pinned||d.Active||d.Classification!="ORDINARY"))
            .OrderBy(d=>d.Session).Select(d=>new{d.Session,d.Bytes,d.Pinned,d.Active,d.Classification,d.BaseClassification}))));
    static byte[] AckBytes(RetentionReport report)
    {
        byte[] payload=JsonSerializer.SerializeToUtf8Bytes(new{schema=1,threshold=TotalWarningBytes,armed=report.After.TotalBytes<=TotalWarningBytes,total=report.After.TotalBytes,protectedBytes=report.After.ExemptRawBytes,
            inventorySha256=ProtectedIdentity(report)});
        return JsonSerializer.SerializeToUtf8Bytes(new{payload,sha256=Convert.ToHexString(SHA256.HashData(payload))});
    }
    internal void Acknowledge(RetentionReport displayed)=>Locked(()=>{
        var current=Snapshot();
        if(ProtectedIdentity(current)!=ProtectedIdentity(displayed))throw new InvalidDataException("Protected evidence changed while the warning was open; refresh before acknowledgement");
        AtomicMetadata(Acknowledgement,AckBytes(displayed));
    });
    internal static bool ShouldWarn(long total,long protectedBytes,byte[]? acknowledgement)
    {
        if(total<=TotalWarningBytes)return false;
        try{
            if(acknowledgement is null||acknowledgement.Length>4096)return true;
            using var outer=JsonDocument.Parse(acknowledgement);Unique(outer.RootElement);var o=outer.RootElement;
            byte[] payload=o.GetProperty("payload").GetBytesFromBase64();if(o.GetProperty("sha256").GetString()!=Convert.ToHexString(SHA256.HashData(payload)))return true;
            using var inner=JsonDocument.Parse(payload);Unique(inner.RootElement);var v=inner.RootElement;
            if(v.GetProperty("schema").GetInt32()!=1||v.GetProperty("threshold").GetInt64()!=TotalWarningBytes||v.GetProperty("armed").GetBoolean()||
                v.GetProperty("total").GetInt64()<=TotalWarningBytes||v.GetProperty("protectedBytes").GetInt64()<0||
                v.GetProperty("protectedBytes").GetInt64()>v.GetProperty("total").GetInt64()||v.GetProperty("inventorySha256").GetString()?.Length!=64)return true;
            return protectedBytes-v.GetProperty("protectedBytes").GetInt64()>=ProtectedGrowthBytes;
        }catch{return true;}
    }
    void MarkBelowThreshold(RetentionReport report)
    {
        if(report.After.TotalBytes<=TotalWarningBytes&&File.Exists(Acknowledgement))
            AtomicMetadata(Acknowledgement,AckBytes(report));
    }
    string RawFingerprint(Guid id)
    {
        string path=SessionPath(id);var names=Directory.GetFiles(path).Select(Path.GetFileName).Where(n=>n!="preserve.pin").ToArray();
        if(!RawNames(names,false)||Directory.GetDirectories(path).Length!=0)throw new InvalidDataException("Unresolved raw inventory");
        var text=new StringBuilder();
        foreach(var name in names.Order(StringComparer.Ordinal)){
            using var f=new FileStream(Path.Combine(path,name!),FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
            if(f.Length>RawMaximum(name!))throw new InvalidDataException("Raw bound");
            text.Append(name).Append('\0').Append(f.Length).Append('\0').Append(Convert.ToHexString(SHA256.HashData(f))).Append('\n');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
    static string EvidenceFingerprint(JsonElement files)
    {
        var text=new StringBuilder();foreach(var p in files.EnumerateObject().OrderBy(p=>p.Name,StringComparer.Ordinal))
            text.Append(p.Name).Append('\0').Append(p.Value.GetProperty("bytes").GetInt64()).Append('\0').Append(p.Value.GetProperty("sha256").GetString()).Append('\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
    static bool ValidManualReview(JsonElement v,string raw)
    {
        Unique(v);
        if(v.GetProperty("schema").GetInt32()!=1||!v.GetProperty("positiveNonCritical").GetBoolean()||v.GetProperty("rawSha256").GetString()!=raw||
            string.IsNullOrWhiteSpace(v.GetProperty("rationale").GetString())||string.IsNullOrWhiteSpace(v.GetProperty("reviewer").GetString()))return false;
        byte[] data=v.GetProperty("sealedEvidence").GetBytesFromBase64();
        return data.Length is >0 and <=65536&&v.GetProperty("evidenceSha256").GetString()==Convert.ToHexString(SHA256.HashData(data));
    }
    JsonElement? ManualReview(Guid id)
    {
        string path=ClassificationFile(id);if(!File.Exists(path))return null;
        using var lease=Open(path,false,false);using var j=Json(path);var v=j.RootElement;
        return v.GetProperty("session").GetGuid()==id&&ValidManualReview(v,RawFingerprint(id))?v.Clone():null;
    }
    bool ManualOrdinary(Guid id)=>ManualReview(id) is not null;
    public static void SealReview(Guid id,string rationale,byte[] evidence,bool positiveNonCritical)
        =>new RuntimeRetention().ReviewSession(id,rationale,evidence,positiveNonCritical);
    internal void ReviewSession(Guid id,string rationale,byte[] evidence,bool positive)
    {
        if(string.IsNullOrWhiteSpace(rationale)||rationale.Length>4096||evidence.Length is 0 or >65536)throw new ArgumentException("A bounded report and causal rationale are required");
        Locked(()=>{
            using var directory=Open(SessionPath(id),true,false);var locks=new List<Microsoft.Win32.SafeHandles.SafeFileHandle>();
            try{
                foreach(string file in Directory.GetFiles(SessionPath(id)))locks.Add(Open(file,false,true));
                if(Active(id))throw new InvalidDataException("Active evidence cannot be reviewed for retirement");
                var bytes=JsonSerializer.SerializeToUtf8Bytes(new{schema=1,session=id,reviewer=Environment.UserName,rationale,positiveNonCritical=positive,
                    rawSha256=RawFingerprint(id),evidenceSha256=Convert.ToHexString(SHA256.HashData(evidence)),sealedEvidence=evidence});
                AtomicMetadata(ReviewFile(id),bytes);
            }finally{DisposeAll(locks);}
        });
    }
    public static void ReclassifyAsOrdinary(Guid id)=>new RuntimeRetention().Reclassify(id);
    internal void Reclassify(Guid id)
    {
        Locked(()=>{
            using var directory=Open(SessionPath(id),true,false);var locks=new List<Microsoft.Win32.SafeHandles.SafeFileHandle>();
            try{
                foreach(string file in Directory.GetFiles(SessionPath(id)))locks.Add(Open(file,false,true));
                if(Active(id))throw new InvalidDataException("Active evidence remains protected");
                using var review=Open(ReviewFile(id),false,false);using var j=Json(ReviewFile(id));
                if(j.RootElement.GetProperty("session").GetGuid()!=id||!ValidManualReview(j.RootElement,RawFingerprint(id)))throw new InvalidDataException("Positive noncritical review matching current raw evidence is required");
                AtomicMetadata(ClassificationFile(id),Bytes(ReviewFile(id)));
            }finally{DisposeAll(locks);}
        });
    }
    public static void Unpin(Guid id)=>new RuntimeRetention().UnpinSession(id);
    internal void UnpinSession(Guid id)
    {
        Locked(()=>{using var directory=Open(SessionPath(id),true,false);using var pin=Open(Path.Combine(SessionPath(id),"preserve.pin"),false,true);
            int remove=1;Need(SetFileInformationByHandle(pin,4,ref remove,4),"Remove explicit pin only");});
    }
    // Explicit request does not bypass budget, newest-useful, ownership or capsule.
    public static RetentionReport RetireRaw(Guid id)
    {
        // A selected request never retires another session. It still requires
        // pressure, non-newest eligibility and independently valid preservation.
        var owner=new RuntimeRetention();var snapshot=owner.Snapshot();
        if(!snapshot.Decisions.Any(d=>d.Session==id))throw new InvalidDataException("Unknown session");
        return owner.RollingExecute(requested:id);
    }
}

