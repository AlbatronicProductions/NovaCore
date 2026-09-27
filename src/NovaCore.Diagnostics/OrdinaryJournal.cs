using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NovaCore.Diagnostics;

public sealed record OrdinaryRecovery(Guid Session,ulong Revision,ulong CommittedTarget,ulong DurableSequence,ulong CheckpointSequence,
    OrdinaryState State,bool Corrupt,string Detail)
{
    public bool Complete => !Corrupt && DurableSequence>0 && DurableSequence==CommittedTarget &&
        DurableSequence==State.Sequence && DurableSequence==State.LastProducedObserved && State.Clean &&
        State.Faults==0 && State.Dropped==0 && State.Open.Count==0 && State.Pending.Count==0 && State.PendingContexts.Count==0 &&
        State.LastPhase.TryGetValue((ulong)OrdinaryPhase.Shutdown,out var close) && close.Sequence==DurableSequence &&
        close.Edge==OrdinaryEdge.Return && close.Result==0 && close.Words[17]==1 && close.Words[18]==DurableSequence;
    public string Terminal => Complete
        ? $"COMPLETE recorder session through DurableSequence {DurableSequence}; sealed final produced sequence fully persisted. Present return is not physical display completion."
        : "TERMINAL SUFFIX UNCERTAIN after DurableSequence; return not retained does not prove a call never returned. Present return is not physical display completion.";
}

/// <summary>One persistence owner. Flush(true) calls Windows FlushFileBuffers. A head is written only after bank flush, ACK only after head flush.</summary>
public sealed class OrdinaryJournal : IDisposable
{
    public const int PageBytes=4096,RecordsPerPage=15,EventBytes=8*1024*1024;
    // Copy at most one eighth of the existing ring: 1,020 records / 68 pages.
    // This contains the retained native 908-event/100-ms lifecycle burst while
    // keeping seven eighths of producer credit outside any observer batch.
    // Capacity/ABI and the durable page format remain unchanged.
    public const int MaximumBatchRecords=RecordsPerPage*(OrdinaryProtocol.Capacity/8/RecordsPerPage);
    public const long EventOffset=PageBytes+OrdinaryCheckpoint.MaximumBytes,BankBytes=EventOffset+EventBytes;
    readonly FileStream[] banks=new FileStream[2],heads=new FileStream[2];
    readonly Guid session;readonly string directory;readonly Action<string>? cut;
    int bank,pages;ulong revision,incarnation,checkpoint;
    byte[] checkpointHash=[];int checkpointLength;
    bool failed;
    public OrdinaryState State {get;private set;}=new();
    public ulong DurableSequence {get;private set;}
    public long BytesWritten {get;private set;}
    public long Flushes {get;private set;}
    public long Rotations {get;private set;}
    public int PageLimit {get;}
    public OrdinaryJournal(string path,Guid id,int pageLimit=EventBytes/PageBytes,Action<string>? faultCut=null)
    {
        if(pageLimit<1||pageLimit>EventBytes/PageBytes)throw new ArgumentOutOfRangeException(nameof(pageLimit));
        directory=path;session=id;PageLimit=pageLimit;cut=faultCut;
        Directory.CreateDirectory(path);
        try{
            for(int i=0;i<2;i++){banks[i]=NewFile($"bank{i}.bin",BankBytes);heads[i]=NewFile($"head{i}.bin",PageBytes);}
            incarnation=1;WriteCheckpoint();CommitHead();
        }catch{Dispose();throw;}
    }
    FileStream NewFile(string name,long size){var f=new FileStream(Path.Combine(directory,name),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.Read,4096,FileOptions.None);try{f.SetLength(size);f.Flush(true);return f;}catch{f.Dispose();throw;}}
    void Write(FileStream f,long at,byte[] bytes){f.Position=at;f.Write(bytes);BytesWritten+=bytes.Length;}
    void Flush(FileStream f){f.Flush(true);Flushes++;}
    void Cut(string stage)=>cut?.Invoke(stage);
    void WriteCheckpoint()
    {
        var bytes=OrdinaryCheckpoint.Encode(State);checkpoint=State.Sequence;checkpointLength=bytes.Length;checkpointHash=SHA256.HashData(bytes);
        var meta=new byte[PageBytes];Put(meta,0,0x314B4E4142434E);session.TryWriteBytes(meta.AsSpan(8));Put(meta,24,incarnation);Put(meta,32,checkpoint);Put(meta,40,(ulong)checkpointLength);checkpointHash.CopyTo(meta,48);
        SHA256.HashData(meta.AsSpan(0,PageBytes-32)).CopyTo(meta,PageBytes-32);
        Cut("checkpoint-write");Write(banks[bank],0,meta);Write(banks[bank],PageBytes,bytes);Cut("checkpoint-flush");Flush(banks[bank]);Cut("checkpoint-flushed");
    }
    void CommitHead()
    {
        var head=new byte[PageBytes];Put(head,0,0x3144414548434E);session.TryWriteBytes(head.AsSpan(8));Put(head,24,revision+1);Put(head,32,(ulong)bank);Put(head,40,incarnation);Put(head,48,checkpoint);Put(head,56,State.Sequence);Put(head,64,(ulong)pages);Put(head,72,(ulong)checkpointLength);checkpointHash.CopyTo(head,80);
        SHA256.HashData(head.AsSpan(0,PageBytes-32)).CopyTo(head,PageBytes-32);
        var f=heads[(revision+1)%2];Cut("head-write");Write(f,0,head);Cut("head-flush");Flush(f);Cut("head-flushed");revision++;DurableSequence=State.Sequence;
    }
    public void Commit(IReadOnlyList<OrdinaryEvent> events,ulong produced,long faults,long dropped)
    {
        if(failed)throw new InvalidOperationException("Persistence owner is failed; recovery required.");
        try{CommitCore(events,produced,faults,dropped);}catch{failed=true;throw;}
    }
    void CommitCore(IReadOnlyList<OrdinaryEvent> events,ulong produced,long faults,long dropped)
    {
        if(events.Count>MaximumBatchRecords)throw new ArgumentOutOfRangeException(nameof(events));
        if(events.Count==0){if(State.LastProducedObserved==produced&&State.Faults==(State.Faults|faults)&&State.Dropped==dropped)return;
            State.LastProducedObserved=produced;State.Faults|=faults;State.Dropped=dropped;Rotate();return;}
        int cursor=0;
        while(cursor<events.Count){
            // Rotate only at a durable state. A batch spanning a bank boundary
            // commits its prefix before checkpointing; speculative suffix state
            // can never leak into the new checkpoint.
            if(pages==PageLimit)Rotate();
            int pageCount=Math.Min(PageLimit-pages,(events.Count-cursor+RecordsPerPage-1)/RecordsPerPage);
            for(int n=0;n<pageCount;n++){
                int count=Math.Min(RecordsPerPage,events.Count-cursor);
                var page=new byte[PageBytes];session.TryWriteBytes(page);Put(page,16,incarnation);Put(page,24,events[cursor].Sequence);Put(page,32,(ulong)count);
                for(int i=0;i<count;i++){var e=events[cursor++];State.Apply(e);e.Encode().CopyTo(page,256+i*256);}
                State.LastProducedObserved=produced;State.Faults|=faults;State.Dropped=dropped;
                Put(page,40,produced);Put(page,48,unchecked((ulong)State.Faults));Put(page,56,(ulong)dropped);SHA256.HashData(page.AsSpan(0,224)).CopyTo(page,224);
                Cut("data-write");Write(banks[bank],EventOffset+(long)(pages+n)*PageBytes,page);Cut("data-page-written");
            }
            Cut("data-flush");Flush(banks[bank]);Cut("data-flushed");pages+=pageCount;CommitHead();
        }
    }
    public void Rotate()
    {
        if(failed)throw new InvalidOperationException("Persistence owner is failed; recovery required.");
        try{RotateCore();}catch{failed=true;throw;}
    }
    void RotateCore()
    {
        // Caller owns only state whose previous commit finished. Never re-use current authority.
        Cut("rotation-start");bank=1-bank;incarnation++;pages=0;WriteCheckpoint();CommitHead();Rotations++;
    }
    public void Dispose(){foreach(var f in banks)f?.Dispose();foreach(var f in heads)f?.Dispose();}
    static void Put(byte[] b,int i,ulong x)=>BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(i),x);
    static ulong Get(byte[] b,int i)=>BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(i));
    static bool HashValid(byte[] b)=>b.Length>=32&&SHA256.HashData(b.AsSpan(0,b.Length-32)).AsSpan().SequenceEqual(b.AsSpan(b.Length-32));
    public static OrdinaryRecovery Recover(string path,Guid expected)
        =>RecoverCore(path,expected,FileShare.ReadWrite);
    internal static OrdinaryRecovery RecoverForRetirement(string path,Guid expected)
        =>RecoverCore(path,expected,FileShare.Read|FileShare.Delete);
    static OrdinaryRecovery RecoverCore(string path,Guid expected,FileShare share)
        =>RecoverStreams(expected,name=>new FileStream(Path.Combine(path,name),FileMode.Open,FileAccess.Read,share));
    // Same recovery mechanism for RAM-validated capsules: no expanded raw copy
    // is written into the bounded runtime root.
    internal static OrdinaryRecovery RecoverStreams(Guid expected,Func<string,Stream> open)
    {
        var candidates=new List<(byte[] Head,OrdinaryRecovery Result)>();var superseded=new List<ulong>();var validHeads=new List<byte[]>();var errors=new List<string>();bool corrupt=false;
        for(int slot=0;slot<2;slot++){
            try{
                byte[] h=new byte[PageBytes];using(var head=open($"head{slot}.bin")){if(head.Length!=PageBytes)throw new InvalidDataException("Head size.");head.ReadExactly(h);}
                if(h.All(b=>b==0))continue;
                if(h.Length!=PageBytes||!HashValid(h)||Get(h,0)!=0x3144414548434E||new Guid(h.AsSpan(8,16))!=expected||Get(h,32)>1||Get(h,64)>EventBytes/PageBytes)throw new InvalidDataException("Invalid/stale head.");
                validHeads.Add(h);
                using var f=open($"bank{Get(h,32)}.bin");
                var meta=new byte[PageBytes];f.ReadExactly(meta);
                // An older head can legally name a bank reused by a later successful rotation.
                if(!HashValid(meta)||new Guid(meta.AsSpan(8,16))!=expected)throw new InvalidDataException("Damaged bank metadata.");
                if(Get(meta,24)>Get(h,40)){superseded.Add(Get(h,24));continue;}
                if(Get(meta,24)!=Get(h,40))throw new InvalidDataException("Bank incarnation regressed.");
                if(!HashValid(meta)||Get(meta,0)!=0x314B4E4142434E||new Guid(meta.AsSpan(8,16))!=expected||Get(meta,32)!=Get(h,48)||Get(meta,40)!=Get(h,72)||!meta.AsSpan(48,32).SequenceEqual(h.AsSpan(80,32)))throw new InvalidDataException("Invalid checkpoint header.");
                int length=checked((int)Get(h,72));if(length<1||length>OrdinaryCheckpoint.MaximumBytes)throw new InvalidDataException("Checkpoint length.");
                byte[] c=new byte[length];f.ReadExactly(c);if(!SHA256.HashData(c).AsSpan().SequenceEqual(h.AsSpan(80,32)))throw new InvalidDataException("Checkpoint digest.");
                var state=OrdinaryCheckpoint.Decode(c);if(state.Sequence!=Get(h,48)||Get(h,56)<state.Sequence)throw new InvalidDataException("Checkpoint sequence.");
                bool partial=false;
                for(ulong page=0;page<Get(h,64)&&!partial;page++){
                    var p=new byte[PageBytes];f.Position=EventOffset+(long)page*PageBytes;int available=f.ReadAtLeast(p,PageBytes,false);
                    if(available<256){partial=true;break;}
                    if(new Guid(p.AsSpan(0,16))!=expected||Get(p,16)!=Get(h,40)||Get(p,24)!=state.Sequence+1||Get(p,32)<1||Get(p,32)>RecordsPerPage||!SHA256.HashData(p.AsSpan(0,224)).AsSpan().SequenceEqual(p.AsSpan(224,32))){partial=true;break;}
                    for(int i=0;i<(int)Get(p,32);i++){
                        var r=p.AsSpan(256+i*256,256);
                        if(available<256+(i+1)*256||state.Sequence>=Get(h,56)||!OrdinaryProtocol.Valid(r,state.Sequence+1)){partial=true;break;}
                        state.Apply(OrdinaryEvent.Read(r));
                    }
                    state.LastProducedObserved=Get(p,40);state.Faults|=unchecked((long)Get(p,48));state.Dropped=(long)Get(p,56);
                }
                partial|=state.Sequence!=Get(h,56);
                candidates.Add((h,new(expected,Get(h,24),Get(h,56),state.Sequence,Get(h,48),state,partial,partial?"Committed data damaged; stopped at first invalid record.":"Validated committed authority; uncommitted tail ignored.")));
            }catch(Exception e) when(e is IOException or InvalidDataException or OverflowException or ArgumentException){corrupt=true;errors.Add($"head{slot}: {e.Message}");}
        }
        if(candidates.Count==0)throw new InvalidDataException("No valid durable checkpoint/head for expected session. "+string.Join("; ",errors));
        if(validHeads.Count==2&&Get(validHeads[0],24)==Get(validHeads[1],24)&&!validHeads[0].SequenceEqual(validHeads[1]))throw new InvalidDataException("Conflicting durable heads.");
        var best=candidates.OrderByDescending(x=>x.Result.DurableSequence).ThenByDescending(x=>x.Result.Revision).First().Result;
        return best with{Corrupt=best.Corrupt||corrupt||superseded.Any(r=>r>=best.Revision)};
    }
}
