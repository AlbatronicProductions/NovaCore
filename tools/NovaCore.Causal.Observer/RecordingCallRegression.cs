using System.Buffers.Binary;
using System.Text.Json;

internal static class RecordingCallRegression
{
    static ulong W(byte[] bytes,int word)=>BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(word*8));
    public static int Run(string journal)
    {
        var bytes=File.ReadAllBytes(journal);var records=new List<byte[]>();int checks=0;
        void Check(bool value,string reason){if(!value)throw new InvalidDataException(reason);checks++;}
        for(int offset=128;offset+512<=bytes.Length;offset+=512)records.Add(bytes.AsSpan(offset,512).ToArray());
        var progress=new RecordingCallProgress();ulong returned=0;int entries=0,scalarEntries=0;
        for(int i=0;i<records.Count;i++){
            progress.Observe(records[i]);Check(progress.Failure==null,"normal protocol");
            if(W(records[i],5)!=28)continue;
            var edge=W(records[i],10);var op=W(records[i],11);
            if(edge==1){
                // Replay the actual producer prefix ending at each call entry.
                // It models non-return at that precise boundary, not a real GPU.
                var stopped=new RecordingCallProgress();for(int n=0;n<=i;n++)stopped.Observe(records[n]);
                Check(stopped.Failure==null&&stopped.Pending==op&&stopped.LastReturned==returned,"wrong non-return/last-return classification");entries++;
            }
            if(edge==2)returned=op;
            if(edge==5){
                var stopped=new RecordingCallProgress();for(int n=0;n<=i;n++)stopped.Observe(records[n]);
                var scalar=W(records[i],15);
                Check(stopped.Failure==null&&stopped.Pending==8&&stopped.LastReturned==7&&stopped.PendingScalar==scalar&&stopped.LastReturnedScalar==scalar-1,"wrong scalar interruption classification");scalarEntries++;
            }
        }
        Check(entries==50&&progress.Pending==0&&progress.LastReturned==48,"coverage/finish");
        bool scalarProtocol=records.Any(r=>W(r,5)==28&&W(r,9)==2);
        Check(!scalarProtocol||(scalarEntries==23&&progress.LastReturnedScalar==23),"scalar coverage/finish");
        var marked=records.Where(r=>W(r,5)==28).ToList();
        void Reject(Action<List<byte[]>> mutate){var copy=marked.Select(b=>b.ToArray()).ToList();mutate(copy);var bad=new RecordingCallProgress();foreach(var r in copy)bad.Observe(r);Check(bad.Failure!=null,"corrupt marker protocol accepted");}
        void Set(byte[] row,int word,ulong value)=>BinaryPrimitives.WriteUInt64LittleEndian(row.AsSpan(word*8),value);
        Reject(r=>r.RemoveAt(0));
        Reject(r=>Set(r[1],11,999));
        Reject(r=>Set(r[2],11,3));
        Reject(r=>Set(r[1],13,42));
        Reject(r=>Set(r[1],2,169));
        Reject(r=>Set(r[1],19,42));
        Reject(r=>Set(r[1],20,42));
        Reject(r=>Set(r[1],37,4));
        Reject(r=>r.Add(r[0]));
        if(scalarProtocol){
            int scalarEntry=marked.FindIndex(r=>W(r,10)==5);
            Reject(r=>Set(r[scalarEntry],16,960));
            Reject(r=>Set(r[scalarEntry],15,24));
            Reject(r=>Set(r[scalarEntry],11,7));
            Reject(r=>Set(r[scalarEntry],9,1));
            Reject(r=>r.RemoveAt(scalarEntry+1));
        }
        var report=new{passed=true,checks,interruptedCallPrefixes=entries,interruptedScalarPrefixes=scalarEntries,gpuExecution=false,normal=progress.Evidence};
        File.WriteAllText(journal+".qualification.json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(new{checks,interruptedCallPrefixes=entries,interruptedScalarPrefixes=scalarEntries,passed=true}));return 0;
    }
}
