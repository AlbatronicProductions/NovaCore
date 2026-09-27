using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace NovaCore.Diagnostics;

public sealed partial class RuntimeRetention
{
    public const long AbnormalSoftCeilingBytes=325L*1024*1024;
    // One event-window of compact storage; oversize preserves raw, never truncates.
    internal const int MaximumCapsuleBytes=OrdinaryJournal.EventBytes;
    sealed class CapsuleBuffer:MemoryStream
    {
        void Bound(long end){if(end>MaximumCapsuleBytes-32)throw new InvalidDataException("Lossless capsule exceeds compact bound; preserve full raw");}
        public override void Write(byte[] buffer,int offset,int count){Bound(checked(Position+count));base.Write(buffer,offset,count);}
        public override void Write(ReadOnlySpan<byte> buffer){Bound(checked(Position+buffer.Length));base.Write(buffer);}
        public override void WriteByte(byte value){Bound(checked(Position+1));base.WriteByte(value);}
        public override void SetLength(long value){Bound(value);base.SetLength(value);}
    }
    string CapsulePath(Guid id)=>Path.Combine(root,"capsule-"+id.ToString("N")+".bin");
    static bool RawNames(string?[] names,bool clean)
    {
        var a=names.Where(n=>n!="termination.json").Order(StringComparer.Ordinal);
        return a.SequenceEqual(Required.Order(StringComparer.Ordinal))||
            !clean&&a.SequenceEqual(Required.Where(n=>n!="finalized.json").Order(StringComparer.Ordinal));
    }
    static long RawMaximum(string name)=>name is "bank0.bin" or "bank1.bin"?OrdinaryJournal.BankBytes:
        name is "head0.bin" or "head1.bin"?OrdinaryJournal.PageBytes:name=="recovery.json"?OrdinaryStorage.RecoveryMetadataBytes:1_048_576;
    (long Order,byte[] Evidence,OrdinaryRecovery Recovery) AbnormalEvidence(Guid id)
    {
        string path=SessionPath(id);var names=Directory.GetFiles(path).Select(Path.GetFileName).ToArray();
        if(!RawNames(names,false)||Directory.GetDirectories(path).Length!=0)throw new InvalidDataException("Unknown raw content; preserve");
        using var session=Json(Path.Combine(path,"session.json"));using var observer=Json(Path.Combine(path,"observer.json"));
        using var termination=File.Exists(Path.Combine(path,"termination.json"))?Json(Path.Combine(path,"termination.json")):null;
        var s=session.RootElement;var o=observer.RootElement;
        if(s.GetProperty("schema").GetInt32()!=2||o.GetProperty("schema").GetInt32()!=1||s.GetProperty("session").GetGuid()!=id||o.GetProperty("session").GetGuid()!=id)
            throw new InvalidDataException("Unresolved ownership identity");
        if(!Exited(s.GetProperty("producer").GetInt32(),s.GetProperty("startUtcTicks").GetInt64())||!Exited(o.GetProperty("pid").GetInt32(),o.GetProperty("startUtcTicks").GetInt64()))
            throw new InvalidDataException("Producer/persistence incarnation remains active");
        long utc=s.GetProperty("openedUtcTicks").GetInt64();
        if(utc<=0||utc>DateTime.MaxValue.Ticks||s.GetProperty("openedQpc").GetInt64()<=0||s.GetProperty("qpcFrequency").GetInt64()<=0)
            throw new InvalidDataException("Invalid ordering provenance");
        var recovered=OrdinaryJournal.RecoverForRetirement(path,id);
        using var report=Json(Path.Combine(path,"recovery.json"));using var expected=JsonDocument.Parse(OrdinaryObserver.ReportBytes(recovered));
        if(!JsonElement.DeepEquals(report.RootElement,expected.RootElement))throw new InvalidDataException("Report contradicts durable authority");
        var manualReview=ManualReview(id);
        if(OrdinaryTermination.Classify(path,id,recovered)!="ORDINARY"&&manualReview is null)throw new InvalidDataException("Criticality unresolved; preserve full raw");
        var files=new SortedDictionary<string,object>(StringComparer.Ordinal);long bytes=0;
        foreach(var name in names.Order(StringComparer.Ordinal))
        {
            using var stream=new FileStream(Path.Combine(path,name!),FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
            if(stream.Length>RawMaximum(name!))throw new InvalidDataException("Raw file exceeds known bound");
            bytes=checked(bytes+stream.Length);files.Add(name!,new{bytes=stream.Length,sha256=Convert.ToHexString(SHA256.HashData(stream))});
        }
        return(utc,JsonSerializer.SerializeToUtf8Bytes(new{schema=1,session=id,classification="ORDINARY",openedUtcTicks=utc,
            producer=s,observer=o,termination=termination?.RootElement,recovery=report.RootElement,rawBytes=bytes,files,manualReview,baseClassification=OrdinaryTermination.Classify(path,id,recovered),
            stateSha256=Convert.ToHexString(SHA256.HashData(OrdinaryCheckpoint.Encode(recovered.State))),
            reason="Ordinary abnormal raw exceeds 325 MiB; oldest eligible outside newest three",
            reproduction="Lossless raw bundle. Recover both original heads/banks with OrdinaryJournal.Recover; compare full report and checkpoint-state digest. No synthetic close or missing terminal window.",
            intent="Raw retirement; transaction completion is reported separately"}),recovered);
    }
    RetentionDecision InspectAbnormal(Guid id,long size)
    {
        var e=AbnormalEvidence(id);
        string basis=OrdinaryTermination.Classify(SessionPath(id),id,e.Recovery);
        return new(id,size,false,false,true,e.Order,basis=="ORDINARY"?"Positively ordinary; sealed close and observed producer exit; metadata order":"Explicit positive noncritical review; original critical/uncertain facts retained; capsule required"){Classification="ORDINARY",BaseClassification=basis};
    }
    static FileStream PersistEvidence(string path,byte[] bytes,Action<string>? cut,string stage)
    {
        if(!File.Exists(path))
        {
            using var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
            f.Write(bytes);cut?.Invoke(stage+"-written");f.Flush(true);cut?.Invoke(stage+"-flushed");
        }
        var handle=Open(path,false,false,write:true);FileStream? file=null;
        try
        {
            file=new FileStream(handle,FileAccess.ReadWrite);
            if(file.Length!=bytes.Length)throw new IOException("Conflicting durable "+stage);
            byte[] read=new byte[bytes.Length];file.ReadExactly(read);
            if(!read.AsSpan().SequenceEqual(bytes))throw new IOException("Conflicting durable "+stage);
            file.Flush(true);cut?.Invoke(stage+"-verified");return file;
        }
        catch{file?.Dispose();handle.Dispose();throw;}
    }
    FileStream Capsule(Guid id,Action<string>? cut)
    {
        var e=AbnormalEvidence(id);using var output=new CapsuleBuffer();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            void Add(string name,Stream data){var entry=zip.CreateEntry(name,CompressionLevel.Fastest);entry.LastWriteTime=new DateTimeOffset(1980,1,1,0,0,0,TimeSpan.Zero);using var target=entry.Open();data.CopyTo(target);}
            using(var metadata=new MemoryStream(e.Evidence,false))Add("evidence.json",metadata);
            foreach(var path in Directory.GetFiles(SessionPath(id)).Order(StringComparer.Ordinal))
            {using var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);Add(Path.GetFileName(path),f);}
        }
        if(output.Length+32>MaximumCapsuleBytes)throw new InvalidDataException("Lossless capsule exceeds compact bound; preserve full raw");
        byte[] payload=output.ToArray();byte[] bytes=new byte[payload.Length+32];payload.CopyTo(bytes,0);SHA256.HashData(payload).CopyTo(bytes,payload.Length);
        var lease=PersistEvidence(CapsulePath(id),bytes,cut,"capsule");
        try{ValidateCapsule(id,bytes,e.Evidence);cut?.Invoke("capsule-recovered");return lease;}
        catch{lease.Dispose();throw;}
    }
    // Fixed-root evidence recovery; never accepts an extraction/cleanup path.
    public static OrdinaryRecovery RecoverCapsule(Guid id)=>new RuntimeRetention().ReadCapsule(id);
    internal OrdinaryRecovery ReadCapsule(Guid id)
    {
        var anchors=Anchor();try{using var file=Open(CapsulePath(id),false,false);byte[] bytes=Bytes(CapsulePath(id),MaximumCapsuleBytes);
            if(bytes.Length<33||!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))throw new InvalidDataException("Capsule checksum/size");
            using var zip=new ZipArchive(new MemoryStream(bytes,0,bytes.Length-32,false),ZipArchiveMode.Read);var entry=zip.GetEntry("evidence.json")??throw new InvalidDataException("Missing capsule evidence");
            if(entry.Length<1||entry.Length>OrdinaryStorage.RecoveryMetadataBytes+65536)throw new InvalidDataException("Capsule evidence expansion bound");
            byte[] header=new byte[(int)entry.Length];using(var stream=entry.Open()){stream.ReadExactly(header);if(stream.ReadByte()!=-1)throw new InvalidDataException("Capsule trailing evidence");}using var metadata=JsonDocument.Parse(header);
            return metadata.RootElement.GetProperty("schema").GetInt32()==2?ValidateStrongCapsule(id,bytes):ValidateCapsule(id,bytes,null);}
        finally{DisposeAll(anchors);}
    }
    OrdinaryRecovery ValidateCapsule(Guid id,byte[] bytes,byte[]? expected)
    {
        if(bytes.Length<33||bytes.Length>MaximumCapsuleBytes||!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))
            throw new InvalidDataException("Capsule checksum/size");
        using var zip=new ZipArchive(new MemoryStream(bytes,0,bytes.Length-32,false),ZipArchiveMode.Read);
        var names=zip.Entries.Select(x=>x.FullName).ToArray();
        if(names.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=names.Length||names.Count(n=>n=="evidence.json")!=1||
            !RawNames(names.Where(n=>n!="evidence.json").ToArray(),false))throw new InvalidDataException("Capsule entry inventory");
        byte[] ReadEntry(ZipArchiveEntry entry,long maximum)
        {
            if(entry.Length<0||entry.Length>maximum)throw new InvalidDataException("Capsule expansion bound");
            byte[] result=new byte[checked((int)entry.Length)];using var stream=entry.Open();stream.ReadExactly(result);
            if(stream.ReadByte()!=-1)throw new InvalidDataException("Capsule trailing entry data");return result;
        }
        byte[] evidence=ReadEntry(zip.GetEntry("evidence.json")!,1_048_576);
        if(expected is not null&&!expected.AsSpan().SequenceEqual(evidence))throw new InvalidDataException("Capsule source identity");
        using var manifest=JsonDocument.Parse(evidence);Unique(manifest.RootElement);var m=manifest.RootElement;
        if(m.GetProperty("schema").GetInt32()!=1||m.GetProperty("session").GetGuid()!=id||m.GetProperty("classification").GetString()!="ORDINARY")throw new InvalidDataException("Capsule classification");
        if(!m.GetProperty("files").EnumerateObject().Select(p=>p.Name).Order(StringComparer.Ordinal).SequenceEqual(names.Where(n=>n!="evidence.json").Order(StringComparer.Ordinal)))throw new InvalidDataException("Capsule manifest inventory");
        var raw=new Dictionary<string,byte[]>(StringComparer.Ordinal);long total=0;
        foreach(var entry in zip.Entries.Where(e=>e.FullName!="evidence.json"))
        {
            byte[] data=ReadEntry(entry,RawMaximum(entry.FullName));var identity=m.GetProperty("files").GetProperty(entry.FullName);
            if(identity.GetProperty("bytes").GetInt64()!=data.Length||identity.GetProperty("sha256").GetString()!=Convert.ToHexString(SHA256.HashData(data)))throw new InvalidDataException("Capsule raw identity");
            raw.Add(entry.FullName,data);total+=data.Length;
        }
        if(total!=m.GetProperty("rawBytes").GetInt64())throw new InvalidDataException("Capsule raw accounting");
        foreach(var pair in raw.Where(p=>p.Key.EndsWith(".json",StringComparison.Ordinal))){using var j=JsonDocument.Parse(pair.Value);Unique(j.RootElement);}
        var recovered=OrdinaryJournal.RecoverStreams(id,name=>new MemoryStream(raw[name],false));
        using var report=JsonDocument.Parse(OrdinaryObserver.ReportBytes(recovered));
        bool manual=m.GetProperty("manualReview").ValueKind==JsonValueKind.Object&&m.GetProperty("manualReview").GetProperty("session").GetGuid()==id&&ValidManualReview(m.GetProperty("manualReview"),EvidenceFingerprint(m.GetProperty("files")));
        if(!JsonElement.DeepEquals(report.RootElement,m.GetProperty("recovery"))||m.GetProperty("stateSha256").GetString()!=Convert.ToHexString(SHA256.HashData(OrdinaryCheckpoint.Encode(recovered.State)))||OrdinaryTermination.ClassifyBytes(id,recovered,name=>raw[name])!="ORDINARY"&&!manual)
            throw new InvalidDataException("Capsule normal-parser reconstruction mismatch");
        return recovered;
    }
}
