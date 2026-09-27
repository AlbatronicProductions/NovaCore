using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace NovaCore.Diagnostics;

public sealed partial class RuntimeRetention
{
    // This is explicitly a rolling index of routine, fault-free unsuccessful
    // closes. Critical, pinned, corrupt or fault-bearing capsules stay lossless.
    // Old ordinary summary rows roll into an aggregate, not an invented archive.
    internal const int IndexRecords=64, IndexMaximumBytes=8*1024*1024;
    internal const int IndexRecordBytes=(IndexMaximumBytes-4096)/IndexRecords;
    const string IndexName="ordinary-forensic-index.bin";
    internal sealed record IndexRow(Guid Session,long Epoch,string CapsuleSha256,long CapsuleBytes,
        JsonElement Evidence,Dictionary<string,byte[]> Authority,byte[] Checkpoint,byte[] TerminalEvents);
    internal sealed record ForensicIndex(int Schema,long RolledSessions,long FirstRolledEpoch,long LastRolledEpoch,
        string RolledDigest,IndexRow[] Records);
    static readonly string[] IndexAuthority=["session.json","observer.json","termination.json","recovery.json","head0.bin","head1.bin"];
    static bool RoutineIndexCandidate(OrdinaryRecovery r)=>!r.Corrupt&&r.State.Faults==0&&r.State.Dropped==0&&
        r.DurableSequence>0&&r.DurableSequence==r.CommittedTarget&&r.DurableSequence==r.State.LastProducedObserved&&
        r.State.Open.Count==0&&r.State.Pending.Count==0&&r.State.PendingContexts.Count==0&&r.State.Resources.Count==0;
    static OrdinaryRecovery IndexRecovery(IndexRow row)
    {
        if(row.Session==Guid.Empty||row.Epoch<=0||row.Epoch>DateTime.MaxValue.Ticks||row.CapsuleSha256.Length!=64||
            row.CapsuleBytes is <33 or >MaximumCapsuleBytes||JsonSerializer.SerializeToUtf8Bytes(row).Length>IndexRecordBytes)
            throw new InvalidDataException("Rolling index row bounds");
        var m=row.Evidence;Unique(m);
        if(m.GetProperty("schema").GetInt32()!=2||m.GetProperty("session").GetGuid()!=row.Session||
            m.GetProperty("openedUtcTicks").GetInt64()!=row.Epoch||m.GetProperty("baseClassification").GetString()!="ORDINARY"||
            m.GetProperty("pinPreserved").GetBoolean()||m.GetProperty("files").TryGetProperty("preserve.pin",out _)||m.GetProperty("files").TryGetProperty("raw-preserve.json",out _)||
            !row.Authority.Keys.Order(StringComparer.Ordinal).SequenceEqual(IndexAuthority.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Rolling index must positively preserve routine unpinned authority");
        foreach(var p in row.Authority){var v=m.GetProperty("files").GetProperty(p.Key);
            if(p.Value.Length>OrdinaryStorage.Maximum(p.Key)||v.GetProperty("bytes").GetInt64()!=p.Value.Length||v.GetProperty("sha256").GetString()!=Digest(p.Value))throw new InvalidDataException("Index original authority hash");}
        var j=m.GetProperty("recovery");var state=OrdinaryCheckpoint.Decode(row.Checkpoint);
        var r=new OrdinaryRecovery(row.Session,j.GetProperty("Revision").GetUInt64(),j.GetProperty("CommittedTarget").GetUInt64(),
            j.GetProperty("DurableSequence").GetUInt64(),j.GetProperty("CheckpointSequence").GetUInt64(),state,j.GetProperty("Corrupt").GetBoolean(),j.GetProperty("Detail").GetString()!);
        using var report=JsonDocument.Parse(OrdinaryObserver.ReportBytes(r));
        using var originalReport=JsonDocument.Parse(row.Authority["recovery.json"]);Unique(originalReport.RootElement);
        if(!JsonElement.DeepEquals(originalReport.RootElement,report.RootElement))throw new InvalidDataException("Contradictory original report stays lossless");
        if(!row.Checkpoint.AsSpan().SequenceEqual(OrdinaryCheckpoint.Encode(state))||!RoutineIndexCandidate(r)||OrdinaryTermination.ClassifyBytes(row.Session,r,n=>row.Authority[n])!="ORDINARY"||
            Digest(row.Checkpoint)!=m.GetProperty("stateSha256").GetString()||!JsonElement.DeepEquals(report.RootElement,j))throw new InvalidDataException("Index checkpoint/report/termination reconciliation");
        if(row.TerminalEvents.Length is 0 or >64*256||row.TerminalEvents.Length%256!=0)throw new InvalidDataException("Index terminal window bound");
        ulong prior=0;for(int at=0;at<row.TerminalEvents.Length;at+=256){var e=row.TerminalEvents.AsSpan(at,256);ulong sequence=BinaryPrimitives.ReadUInt64LittleEndian(e);
            if(!OrdinaryProtocol.Valid(e,sequence)||(prior!=0&&sequence!=prior+1)||sequence>r.DurableSequence)throw new InvalidDataException("Index terminal event authority");prior=sequence;}
        if(prior!=r.DurableSequence)throw new InvalidDataException("Index window must end at original durable close");
        ulong first=BinaryPrimitives.ReadUInt64LittleEndian(row.TerminalEvents);
        foreach(var e in state.LastPhase.Values.Cast<OrdinaryEvent?>().Concat([state.LastSubmit,state.LastCompletion,state.LastPresent,state.LastPublication]))
            if(e is not null&&e.Sequence>=first&&e.Sequence<=prior&&!row.TerminalEvents.AsSpan(checked((int)(e.Sequence-first)*256),256).SequenceEqual(e.Encode()))
                throw new InvalidDataException("Index terminal window contradicts retained checkpoint event");
        return r;
    }
    internal static ForensicIndex ValidateIndex(byte[] bytes)
    {
        if(bytes.Length is <33 or >IndexMaximumBytes||!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))throw new InvalidDataException("Index checksum/size");
        using(var json=JsonDocument.Parse(bytes.AsMemory(0,bytes.Length-32)))Unique(json.RootElement);
        var index=JsonSerializer.Deserialize<ForensicIndex>(bytes.AsSpan(0,bytes.Length-32))??throw new InvalidDataException("Index schema");
        if(index.Schema!=1||index.RolledSessions<0||index.RolledDigest.Length!=64||index.Records.Length>IndexRecords||
            index.Records.Select(r=>r.Session).Distinct().Count()!=index.Records.Length||
            (index.RolledSessions==0?(index.FirstRolledEpoch!=0||index.LastRolledEpoch!=0):(index.FirstRolledEpoch<=0||index.LastRolledEpoch<index.FirstRolledEpoch)))throw new InvalidDataException("Index aggregate authority");
        foreach(var row in index.Records)IndexRecovery(row);return index;
    }
    internal static byte[] IndexBytes(ForensicIndex index)
    {
        byte[] payload=JsonSerializer.SerializeToUtf8Bytes(index);if(payload.Length+32>IndexMaximumBytes)throw new InvalidDataException("Bounded forensic index full; preserve capsule");
        byte[] b=new byte[payload.Length+32];payload.CopyTo(b,0);SHA256.HashData(payload).CopyTo(b,payload.Length);ValidateIndex(b);return b;
    }
    internal static ForensicIndex AppendIndex(ForensicIndex index,IndexRow row)
    {
        IndexRecovery(row);if(index.Records.Any(r=>r.Session==row.Session))throw new InvalidDataException("Duplicate indexed session");
        var rows=index.Records.Append(row).OrderBy(r=>r.Epoch).ThenBy(r=>r.Session).ToList();long count=index.RolledSessions,first=index.FirstRolledEpoch,last=index.LastRolledEpoch;string digest=index.RolledDigest;
        while(rows.Count>IndexRecords){var old=rows[0];rows.RemoveAt(0);count=checked(count+1);first=first==0?old.Epoch:Math.Min(first,old.Epoch);last=Math.Max(last,old.Epoch);
            digest=Digest(System.Text.Encoding.UTF8.GetBytes(digest).Concat(JsonSerializer.SerializeToUtf8Bytes(old)).ToArray());}
        return new(1,count,first,last,digest,rows.ToArray());
    }
    static IndexRow CapsuleIndexRow(Guid id,byte[] capsule)
    {
        var r=ValidateStrongCapsule(id,capsule);if(!RoutineIndexCandidate(r))throw new InvalidDataException("Fault-bearing/unique capsule stays lossless");
        using var zip=new ZipArchive(new MemoryStream(capsule,0,capsule.Length-32,false),ZipArchiveMode.Read);
        byte[] ReadEntry(string name){var e=zip.GetEntry(name)??throw new InvalidDataException("Missing index authority");long bound=name=="evidence.json"?OrdinaryStorage.RecoveryMetadataBytes+65536:OrdinaryStorage.Maximum(name);
            if(e.Length>bound)throw new InvalidDataException("Index source expansion bound");byte[] b=new byte[(int)e.Length];using var s=e.Open();s.ReadExactly(b);return b;}
        using var m=JsonDocument.Parse(ReadEntry("evidence.json"));var authority=IndexAuthority.ToDictionary(n=>n,ReadEntry);var events=new List<byte[]>();
        // Keep original committed terminal records, never synthesize a close.
        foreach(var name in new[]{"head0.bin","head1.bin"}){byte[] head=authority[name];if(BinaryPrimitives.ReadUInt64LittleEndian(head.AsSpan(24))!=r.Revision)continue;
            ulong bank=BinaryPrimitives.ReadUInt64LittleEndian(head.AsSpan(32)),pages=BinaryPrimitives.ReadUInt64LittleEndian(head.AsSpan(64));byte[] raw=ReadEntry($"bank{bank}.bin");
            for(ulong page=pages>5?pages-5:0;page<pages;page++){int at=checked((int)(OrdinaryJournal.EventOffset+(long)page*OrdinaryJournal.PageBytes));int n=checked((int)BinaryPrimitives.ReadUInt64LittleEndian(raw.AsSpan(at+32)));
                for(int k=0;k<n;k++)events.Add(raw.AsSpan(at+256+k*256,256).ToArray());}break;}
        var row=new IndexRow(id,m.RootElement.GetProperty("openedUtcTicks").GetInt64(),Digest(capsule),capsule.Length,m.RootElement.Clone(),authority,
            OrdinaryCheckpoint.Encode(r.State),events.TakeLast(64).SelectMany(e=>e).ToArray());IndexRecovery(row);return row;
    }
    internal ForensicIndex ReadIndex()=>ValidateIndex(Bytes(Path.Combine(root,IndexName),IndexMaximumBytes));
    public static string InspectForensicIndex()=>JsonSerializer.Serialize(new RuntimeRetention().ReadIndex());
    internal bool ConsolidateOrdinaryCapsule(Action<string>? cut,List<string> failed)
    {
        // Metadata is only an inexpensive ordering hint. The selected capsule
        // is fully recovered and classified again under transaction ownership.
        var candidates=new List<(Guid Session,long Epoch)>();
        foreach(string path in Directory.GetFiles(root,"capsule-*.bin"))try{if(!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path)[8..],"N",out var id))continue;
            using var lease=Open(path,false,false);byte[] bytes=Bytes(path,MaximumCapsuleBytes);
            if(bytes.Length<33||!SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).AsSpan().SequenceEqual(bytes.AsSpan(bytes.Length-32)))continue;
            using var zip=new ZipArchive(new MemoryStream(bytes,0,bytes.Length-32,false),ZipArchiveMode.Read);var entry=zip.GetEntry("evidence.json");
            if(entry is null||entry.Length<1||entry.Length>OrdinaryStorage.RecoveryMetadataBytes+65536)continue;byte[] header=new byte[(int)entry.Length];using(var stream=entry.Open())stream.ReadExactly(header);
            using var metadata=JsonDocument.Parse(header);var m=metadata.RootElement;Unique(m);
            if(m.GetProperty("schema").GetInt32()==2&&m.GetProperty("baseClassification").GetString()=="ORDINARY"&&!m.GetProperty("pinPreserved").GetBoolean()&&
                !m.GetProperty("files").TryGetProperty("raw-preserve.json",out _)&&m.GetProperty("recovery").GetProperty("Faults").GetInt64()==0&&m.GetProperty("recovery").GetProperty("Dropped").GetInt64()==0)
                candidates.Add((id,m.GetProperty("openedUtcTicks").GetInt64()));}catch{ /* Critical, pinned, unknown, oversized or fault-bearing capsules retain all bytes. */ }
        foreach(var row in candidates.OrderBy(r=>r.Epoch).ThenBy(r=>r.Session))try{
            using var tx=CreateTransaction(IntPtr.Zero,IntPtr.Zero,0,0,0,30000,"NovaCore validated routine rolling forensic index");Need(!tx.IsInvalid,"Index transaction unavailable; preserve capsule");bool committed=false;
            try{
                using var source=Open(CapsulePath(row.Session),false,true,tx);var current=CapsuleIndexRow(row.Session,Bytes(CapsulePath(row.Session),MaximumCapsuleBytes));
                if(current.Epoch!=row.Epoch)throw new InvalidDataException("Capsule ordering changed before indexing");
                string target=Path.Combine(root,IndexName);using var old=File.Exists(target)?Open(target,false,true,tx,write:true):null;
                using var oldStream=old is null?null:new FileStream(old,FileAccess.ReadWrite);long oldBytes=oldStream?.Length??0;
                var index=new ForensicIndex(1,0,0,0,new string('0',64),[]);
                if(oldStream is not null){if(oldBytes>IndexMaximumBytes)throw new InvalidDataException("Existing index bound");byte[] bytes=new byte[(int)oldBytes];oldStream.ReadExactly(bytes);index=ValidateIndex(bytes);}
                byte[] output=IndexBytes(AppendIndex(index,current));
                // Only transaction-held exact file identities bypass reopening.
                var i=Inventory(lockedFiles:new Dictionary<string,long>{{CapsulePath(row.Session),current.CapsuleBytes},{target,oldBytes}});
                long total=i.Other+i.Sessions.Values.Sum();
                // Admission mutex+anchored tree owns publication; cap accounts for
                // old and new index bytes until commit. No extraction directories.
                if(i.Unknown.Count!=0||total+Outstanding(i)+output.Length>RuntimeCapBytes)throw new InvalidDataException("Index transient cannot fit; preserve capsule");
                if(output.Length>=current.CapsuleBytes+oldBytes)throw new InvalidDataException("Index would not reduce storage; preserve capsule");
                using var created=old is null?CreateFileTransactedW(target,Read|0x40000000,1,IntPtr.Zero,1,NoFollow,IntPtr.Zero,tx,IntPtr.Zero,IntPtr.Zero):null;
                if(created is not null)Need(!created.IsInvalid,"Create transacted forensic index");
                using var newStream=created is null?null:new FileStream(created,FileAccess.ReadWrite);var file=oldStream??newStream!;
                file.Position=0;file.SetLength(output.Length);file.Write(output);cut?.Invoke("index-written");file.Flush(true);cut?.Invoke("index-flushed");file.Position=0;byte[] persisted=new byte[output.Length];file.ReadExactly(persisted);ValidateIndex(persisted);
                if(!persisted.AsSpan().SequenceEqual(output))throw new InvalidDataException("Index persistence mismatch");cut?.Invoke("index-recovered");
                int remove=1;Need(SetFileInformationByHandle(source,4,ref remove,4),"Stage indexed ordinary capsule retirement");source.Dispose();file.Dispose();old?.Dispose();created?.Dispose();cut?.Invoke("index-before-commit");Need(CommitTransaction(tx),"Commit bounded forensic index");committed=true;return true;
            }finally{if(!committed)RollbackTransaction(tx);}
        }catch(Exception e){failed.Add(row.Session+": capsule/index preserved: "+e.Message);}
        return false;
    }
}
