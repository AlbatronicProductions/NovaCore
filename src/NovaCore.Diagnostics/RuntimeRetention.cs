using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace NovaCore.Diagnostics;

public sealed record RetentionDecision(Guid Session,long Bytes,bool Clean,bool Pinned,bool Eligible,long CloseUtcTicks,string Reason)
{public string Classification{get;init;}="CRITICAL_OR_UNRESOLVED";public string BaseClassification{get;init;}="CRITICAL_OR_UNRESOLVED";public bool Active{get;init;}}
public sealed record RetentionTotals(long TotalBytes,long CleanBytes,long AbnormalBytes,long PinnedBytes,int Sessions,int CleanSessions,int AbnormalSessions,int PinnedSessions)
{public long OrdinaryAbnormalBytes{get;init;}public long ExemptRawBytes{get;init;}public long ActiveBytes{get;init;}public long ForensicCapsuleBytes{get;init;}}
public sealed record RetentionReport(string Status,RetentionTotals Before,RetentionTotals After,RetentionDecision[] Decisions,Guid[] Deleted,string[] Failures,string? Warning,long OtherRetainedBytes,bool AccountingComplete,string[] UnmeasuredEntries);

/// <summary>Fixed runtime capability. No API accepts a cleanup root or a session path.</summary>
public sealed partial class RuntimeRetention
{
    static readonly string RuntimeRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaCore","MinimumRecorder");
    readonly string root;readonly Guid? qualification;
    static readonly string[] Required=["session.json","observer.json","ownership.lock","bank0.bin","bank1.bin","head0.bin","head1.bin","recovery.json","performance.json","finalized.json"];
    RuntimeRetention(Guid? test=null){qualification=test;root=test is null?RuntimeRoot:Path.Combine(Path.GetTempPath(),"NovaCore.Retention.Qualification",test.Value.ToString("N"));}
    internal static RuntimeRetention ForQualification(Guid id)=>new(id);
    internal string QualificationPath=>qualification.HasValue?root:throw new InvalidOperationException();
    string SessionPath(Guid id)=>Path.Combine(root,id.ToString("N"));
    string MutexName=>"Local\\NovaCore.Retention."+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(root.ToUpperInvariant())));
    static byte[] Bytes(string path,int maximum=1_048_576){if(Path.GetFileName(path)=="recovery.json")maximum=Math.Max(maximum,OrdinaryStorage.RecoveryMetadataBytes);using var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);if(f.Length>maximum)throw new InvalidDataException("Metadata size");byte[] b=new byte[(int)f.Length];f.ReadExactly(b);return b;}
    static JsonDocument Json(string path){var j=JsonDocument.Parse(Bytes(path));try{Unique(j.RootElement);return j;}catch{j.Dispose();throw;}}
    internal static void Unique(JsonElement e){if(e.ValueKind==JsonValueKind.Object){var names=new HashSet<string>(StringComparer.Ordinal);foreach(var p in e.EnumerateObject()){if(!names.Add(p.Name))throw new InvalidDataException("Duplicate metadata property");Unique(p.Value);}}else if(e.ValueKind==JsonValueKind.Array)foreach(var p in e.EnumerateArray())Unique(p);}
    static bool Exited(int pid,long started)
    {
        if(pid<=0||started<=0)throw new InvalidDataException("Owner identity");
        try{using var p=Process.GetProcessById(pid);return p.HasExited||p.StartTime.ToUniversalTime().Ticks!=started;}
        catch(ArgumentException){return true;} // PID positively absent; other failures preserve.
    }
    RetentionDecision Inspect(Guid id,bool acquire)
    {
        string path=SessionPath(id);long size=0;bool clean=false,pinned=false;var locks=new List<SafeFileHandle>();
        try{
            if(acquire)locks.Add(Open(path,true,false));
            var entries=Directory.GetFileSystemEntries(path);
            pinned=entries.Any(p=>Path.GetFileName(p).Equals("preserve.pin",StringComparison.OrdinalIgnoreCase));
            foreach(var p in entries){if((File.GetAttributes(p)&(FileAttributes.ReparsePoint|FileAttributes.Directory))!=0)throw new InvalidDataException("Nested/reparse content; preserve");size=checked(size+new FileInfo(p).Length);}
            // Legacy reports remain classified but never acquire invented ownership.
            if(acquire)foreach(var p in entries)locks.Add(Open(p,false,true));
            using var report=Json(Path.Combine(path,"recovery.json"));var rr=report.RootElement;
            var r=OrdinaryJournal.RecoverForRetirement(path,id);
            using var expectedReport=JsonDocument.Parse(OrdinaryObserver.ReportBytes(r));
            if(!JsonElement.DeepEquals(rr,expectedReport.RootElement))throw new InvalidDataException("Recovery metadata contradicts validated durable authority");
            clean=r.Complete&&rr.GetProperty("Complete").GetBoolean()&&rr.GetProperty("Clean").GetBoolean()&&!rr.GetProperty("Corrupt").GetBoolean()&&rr.GetProperty("Faults").GetInt64()==0&&rr.GetProperty("Dropped").GetInt64()==0&&rr.GetProperty("Session").GetGuid()==id&&rr.GetProperty("DurableSequence").GetUInt64()==r.DurableSequence&&rr.GetProperty("CommittedTarget").GetUInt64()==r.CommittedTarget&&rr.GetProperty("CheckpointSequence").GetUInt64()==r.CheckpointSequence&&rr.GetProperty("LastProducedObserved").GetUInt64()==r.DurableSequence&&rr.GetProperty("Revision").GetUInt64()==r.Revision&&rr.GetProperty("Terminal").GetString()==r.Terminal&&rr.GetProperty("Open").EnumerateObject().Count()==0&&rr.GetProperty("Pending").EnumerateObject().Count()==0;
            bool routine=OrdinaryTermination.Classify(path,id,r)=="ORDINARY";
            bool reviewed=!routine&&ManualOrdinary(id);
            if(pinned)return new(id,size,clean,true,false,0,"Explicit preserve.pin; independent criticality unchanged"){Classification=routine||reviewed?"ORDINARY":"CRITICAL_OR_UNRESOLVED",BaseClassification=routine?"ORDINARY":"CRITICAL_OR_UNRESOLVED"};
            if(!routine&&!reviewed)return new(id,size,clean,false,false,0,"Critical incident or unresolved criticality; preserve FULL RAW, exempt from ordinary budget");
            if(reviewed)clean=false; // Reviewed incidents take capsule path; raw facts remain in recovery.
            if(!clean)return InspectAbnormal(id,size);
            if(!RawNames(entries.Select(Path.GetFileName).ToArray(),true))return new(id,size,true,false,false,0,"Missing ownership/finalization or unknown content; preserve");
            using var session=Json(Path.Combine(path,"session.json"));using var owner=Json(Path.Combine(path,"observer.json"));using var final=Json(Path.Combine(path,"finalized.json"));
            var s=session.RootElement;var o=owner.RootElement;var f=final.RootElement;
            if(s.GetProperty("schema").GetInt32()!=2||o.GetProperty("schema").GetInt32()!=1||f.GetProperty("schema").GetInt32()!=1||new[]{s,o,f}.Any(e=>e.GetProperty("session").GetGuid()!=id))throw new InvalidDataException("Ownership schema/session mismatch");
            if(!Exited(s.GetProperty("producer").GetInt32(),s.GetProperty("startUtcTicks").GetInt64())||!Exited(o.GetProperty("pid").GetInt32(),o.GetProperty("startUtcTicks").GetInt64()))return new(id,size,true,false,false,0,"Producer or persistence process incarnation still active");
            string Hash(string n)=>Convert.ToHexString(SHA256.HashData(Bytes(Path.Combine(path,n))));
            if(f.GetProperty("disposition").GetString()!="COMPLETE"||f.GetProperty("durable").GetUInt64()!=r.DurableSequence||f.GetProperty("revision").GetUInt64()!=r.Revision||f.GetProperty("checkpoint").GetUInt64()!=r.CheckpointSequence||f.GetProperty("sessionSha256").GetString()!=Hash("session.json")||f.GetProperty("ownerSha256").GetString()!=Hash("observer.json")||f.GetProperty("head0").GetString()!=Hash("head0.bin")||f.GetProperty("head1").GetString()!=Hash("head1.bin")||f.GetProperty("recoverySha256").GetString()!=Hash("recovery.json"))throw new InvalidDataException("Finalization authority mismatch");
            long close=checked((long)r.State.LastPhase[20].Words[1]),origin=s.GetProperty("openedQpc").GetInt64(),frequency=s.GetProperty("qpcFrequency").GetInt64(),utc=s.GetProperty("openedUtcTicks").GetInt64();
            if(origin<=0||close<origin||frequency<=0||utc<=0||utc>DateTime.MaxValue.Ticks||f.GetProperty("closeQpc").GetInt64()!=close)throw new InvalidDataException("Invalid closure clock provenance");
            long closeUtc=checked(utc+(long)((decimal)(close-origin)*TimeSpan.TicksPerSecond/frequency));
            if(closeUtc<=0||closeUtc>DateTime.MaxValue.Ticks||closeUtc!=f.GetProperty("closeUtcTicks").GetInt64())throw new InvalidDataException("Closure ordering mismatch");
            return new(id,size,true,false,true,closeUtc,"Positively ordinary, clean, closed and unpinned; metadata-derived close order"){Classification="ORDINARY",BaseClassification="ORDINARY"};
        }catch(Exception e){return new(id,size,clean,pinned,false,0,"PRESERVE: "+e.GetType().Name+": "+e.Message);}
        finally{DisposeAll(locks);}
    }
    bool Active(Guid id){try{using var s=Json(Path.Combine(SessionPath(id),"session.json"));var a=s.RootElement;if(!Exited(a.GetProperty("producer").GetInt32(),a.GetProperty("startUtcTicks").GetInt64()))return true;using var o=Json(Path.Combine(SessionPath(id),"observer.json"));var b=o.RootElement;return !Exited(b.GetProperty("pid").GetInt32(),b.GetProperty("startUtcTicks").GetInt64());}catch{return false;}}
    RetentionDecision[] Scan()=>Directory.GetDirectories(root).Where(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _)).Select(p=>{var id=Guid.ParseExact(Path.GetFileName(p),"N");return Inspect(id,true) with{Active=Active(id)};}).OrderBy(d=>d.Session).ToArray();
    // Clean is the recovered disposition; abnormal also includes ownership
    // ambiguity. These safety dimensions overlap, rather than hiding a legacy
    // COMPLETE session whose process closure cannot be established.
    static bool Abnormal(RetentionDecision x)=>!x.Clean||!x.Eligible&&!x.Pinned;
    static RetentionTotals Totals(RetentionDecision[] d,long other,long capsules=0)=>new(d.Sum(x=>x.Bytes)+other,d.Where(x=>x.Clean&&x.Classification=="ORDINARY"&&!x.Pinned&&!x.Active).Sum(x=>x.Bytes),d.Where(Abnormal).Sum(x=>x.Bytes),d.Where(x=>x.Pinned).Sum(x=>x.Bytes),d.Length,d.Count(x=>x.Clean),d.Count(Abnormal),d.Count(x=>x.Pinned))
        {OrdinaryAbnormalBytes=d.Where(x=>!x.Clean&&x.Classification=="ORDINARY"&&!x.Pinned&&!x.Active).Sum(x=>x.Bytes),ExemptRawBytes=d.Where(x=>x.Pinned||x.Classification!="ORDINARY"||x.Active).Sum(x=>x.Bytes),ActiveBytes=d.Where(x=>x.Active).Sum(x=>x.Bytes),ForensicCapsuleBytes=capsules};
    long CapsuleBytes()=>Directory.GetFiles(root,"capsule-*.bin").Where(p=>Guid.TryParseExact(Path.GetFileNameWithoutExtension(p)[8..],"N",out _)).Sum(p=>{using var f=Open(p,false,false);return new FileInfo(p).Length;});
    (Dictionary<Guid,long> Sessions,long Other,List<string> Unknown) Inventory(Guid? lockedSession=null,long lockedBytes=0,IReadOnlyDictionary<string,long>? lockedFiles=null)
    {
        var sizes=new Dictionary<Guid,long>();var unknown=new List<string>();long other=0;
        long Measure(string path,int depth){try{if(depth>64)throw new IOException("Inventory depth");bool dir=(File.GetAttributes(path)&FileAttributes.Directory)!=0;using var handle=Open(path,dir,false);if(!dir)return new FileInfo(path).Length;return Directory.GetFileSystemEntries(path).Sum(p=>Measure(p,depth+1));}catch(Exception e){unknown.Add(Path.GetRelativePath(root,path)+": "+e.Message);return 0;}}
        foreach(var path in Directory.GetFileSystemEntries(root)){
            bool session=Directory.Exists(path)&&Guid.TryParseExact(Path.GetFileName(path),"N",out _);
            Guid id=session?Guid.ParseExact(Path.GetFileName(path),"N"):Guid.Empty;
            // Only the transaction holder supplies its fully locked, hashed raw
            // inventory. Reopening its DELETE leases would fail sharing checks.
            long bytes=session&&id==lockedSession?lockedBytes:lockedFiles is not null&&lockedFiles.TryGetValue(path,out long heldBytes)?heldBytes:Measure(path,0);
            if(session)sizes[id]=bytes;else other=checked(other+bytes);
        }
        return(sizes,other,unknown);
    }
    public static RetentionReport Run()=>new RuntimeRetention().RollingExecute();
    internal RetentionReport Execute(Action<string>? fault=null,Guid? requested=null)
    {
        var deleted=new List<Guid>();var attempted=new HashSet<Guid>();var failures=new List<string>();RetentionDecision[] before=[],after=[];var anchors=new List<SafeFileHandle>();long beforeOther=0,afterOther=0,beforeCapsules=0,afterCapsules=0;var unknown=new List<string>();
        using var mutex=new Mutex(false,MutexName);bool owned=false;
        try{
            try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}
            if(!owned)throw new IOException("Another retention/pin owner is active; preserve");
            anchors=Anchor();var inventory=Inventory();beforeOther=inventory.Other;beforeCapsules=CapsuleBytes();unknown.AddRange(inventory.Unknown);before=Scan().Select(d=>d with{Bytes=inventory.Sessions.GetValueOrDefault(d.Session,d.Bytes)}).ToArray();
            var eligible=before.Where(d=>d.Clean&&d.Eligible).OrderByDescending(d=>d.CloseUtcTicks).ThenBy(d=>d.Session).ToArray();
            foreach(var candidate in eligible.Skip(2).Where(_=>requested is null)){
                attempted.Add(candidate.Session);
                try{Retire(candidate.Session,fault);deleted.Add(candidate.Session);}
                catch(Exception e){failures.Add(candidate.Session+": preserved; retirement failed: "+e.Message);}
            }
            // Positive ordinary classification only. Critical/pinned/unresolved
            // raw is exempt. Preserve newest three useful ordinary raw sessions.
            while(true)
            {
                inventory=Inventory();var current=Scan().Select(d=>d with{Bytes=inventory.Sessions.GetValueOrDefault(d.Session,d.Bytes)}).ToArray();
                if(inventory.Unknown.Count!=0||Totals(current,inventory.Other).OrdinaryAbnormalBytes<=AbnormalSoftCeilingBytes)break;
                var ordered=current.Where(d=>!d.Clean&&d.Classification=="ORDINARY"&&!d.Pinned&&d.CloseUtcTicks>0).OrderByDescending(d=>d.CloseUtcTicks).ThenBy(d=>d.Session).ToArray();
                var candidate=ordered.Skip(3).Where(d=>!d.Clean&&d.Eligible&&!attempted.Contains(d.Session)).OrderBy(d=>d.CloseUtcTicks).ThenBy(d=>d.Session).FirstOrDefault();
                if(candidate is null)break;
                if(requested.HasValue&&candidate.Session!=requested.Value)throw new InvalidDataException("Selected session is not the oldest currently eligible ordinary raw session");
                attempted.Add(candidate.Session);
                try{Retire(candidate.Session,fault);deleted.Add(candidate.Session);}
                catch(Exception e){failures.Add(candidate.Session+": preserved; abnormal retirement failed: "+e.Message);}
                if(requested.HasValue)break;
            }
            inventory=Inventory();afterOther=inventory.Other;afterCapsules=CapsuleBytes();unknown.AddRange(inventory.Unknown);after=Scan().Select(d=>d with{Bytes=inventory.Sessions.GetValueOrDefault(d.Session,d.Bytes)}).ToArray();
        }catch(Exception e){failures.Add("Preserve all: "+e.Message);after=before;afterOther=beforeOther;unknown.Add("Inventory incomplete: "+e.Message);}
        finally{DisposeAll(anchors);if(owned)mutex.ReleaseMutex();}
        var a=Totals(after,afterOther,afterCapsules);string? warning=a.AbnormalSessions>0||a.ExemptRawBytes>0||failures.Count>0||unknown.Count>0?$"Recorder storage: ordinary abnormal raw {a.OrdinaryAbnormalBytes/1048576d:0.0} MiB / 325 MiB. Exempt critical/pinned/unresolved raw {a.ExemptRawBytes/1048576d:0.0} MiB; {a.PinnedSessions} pinned. {failures.Count} retirement failures. Critical, pinned, active, newest-three and unresolved evidence is preserved. "+(a.OrdinaryAbnormalBytes>AbnormalSoftCeilingBytes||a.ExemptRawBytes>AbnormalSoftCeilingBytes?"STORAGE PRESSURE: manual review required.":"Compact capsules are retained independently."):null;
        return new(failures.Count==0?"COMPLETE":"PRESERVE_WITH_WARNING",Totals(before,beforeOther,beforeCapsules),a,before.Select(d=>d with{Reason=deleted.Contains(d.Session)?d.Clean?"RETIRED: validated ordinary complete; older than newest two":"RETIRED: positively ordinary abnormal; durable parser-validated capsule; outside newest three":attempted.Contains(d.Session)?"PRESERVE: retirement failed, original evidence retained":d.Eligible?d.Clean?"PRESERVE: newest two eligible clean sessions":"PRESERVE: ordinary abnormal protected by newest three or below ceiling":d.Reason}).ToArray(),deleted.ToArray(),failures.ToArray(),warning,afterOther,unknown.Count==0,unknown.Distinct().ToArray());
    }
    public static void Pin(Guid id)
    {
        new RuntimeRetention().PinSession(id);
    }
    internal void PinSession(Guid id)
    {
        var owner=this;using var mutex=new Mutex(false,owner.MutexName);if(!mutex.WaitOne(0))throw new IOException("Retention active; pin not acknowledged, retry after completion.");
        var anchors=new List<SafeFileHandle>();try{anchors=owner.Anchor();using var dir=Open(owner.SessionPath(id),true,false);string file=Path.Combine(owner.SessionPath(id),"preserve.pin");byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(new{session=id,preserve=true});if(qualification is null)owner.BoundControlWrite(bytes.Length);OrdinaryStorage.Write(file,bytes);}finally{DisposeAll(anchors);mutex.ReleaseMutex();}
    }
}

