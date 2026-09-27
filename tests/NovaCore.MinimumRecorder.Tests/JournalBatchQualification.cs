using System.Text.Json;
using NovaCore.Diagnostics;

static class JournalBatchQualification
{
    public static void Run(string output)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);int checks=0;
        void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
        string New()=>Path.Combine(output,Guid.NewGuid().ToString("N"));
        void Retire(string path){if(!Path.GetFullPath(path).StartsWith(output+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("fixture cleanup escaped root");Directory.Delete(path,true);}
        OrdinaryEvent[] Events(ulong start,int count)=>Enumerable.Range(0,count).Select(i=>{var e=new OrdinaryEvent();e.Words[0]=start+(ulong)i;e.Words[4]=21;return e;}).ToArray();
        int maximum=OrdinaryJournal.MaximumBatchRecords,pages=maximum/15;
        foreach(int count in new[]{1,14,15,16,maximum-1,maximum}){
            string path=New();Guid id=Guid.NewGuid();
            using(var j=new OrdinaryJournal(path,id)){long flush=j.Flushes;j.Commit(Events(1,count),(ulong)count,0,0);Check(j.DurableSequence==(ulong)count,"batch durable prefix");Check(j.Flushes-flush==2,"one bank/head flush pair for all pages");}
            Check(OrdinaryJournal.Recover(path,id).DurableSequence==(ulong)count,"batch disk prefix");Retire(path);
        }
        foreach(int pageLimit in new[]{1,2,pages-1,pages,pages+1}){
            string path=New();Guid id=Guid.NewGuid();
            using(var j=new OrdinaryJournal(path,id,pageLimit)){j.Commit(Events(1,1),1,0,0);j.Commit(Events(2,maximum),(ulong)maximum+1,0,0);Check(j.State.Sequence==j.DurableSequence,"rotation only checkpoints durable batch prefix");}
            var r=OrdinaryJournal.Recover(path,id);Check(!r.Corrupt&&r.DurableSequence==(ulong)maximum+1,"spanning rotation reconstructs every event");Retire(path);
        }
        // Cut after EVERY individual page, before any grouped flush/head ACK.
        for(int target=1;target<=pages;target++){
            string path=New();Guid id=Guid.NewGuid();bool armed=false,threw=false;int seen=0;
            using(var j=new OrdinaryJournal(path,id,faultCut:s=>{if(armed&&s=="data-page-written"&&++seen==target)throw new IOException("cut page");})){
                j.Commit(Events(1,1),1,0,0);armed=true;
                try{j.Commit(Events(2,maximum),(ulong)maximum+1,0,0);}catch(IOException){threw=true;}
                Check(threw&&j.DurableSequence==1,"no speculative page ACK");
                try{j.Commit(Events(2,1),2,0,0);throw new Exception("failed owner reused");}catch(InvalidOperationException){checks++;}
            }
            var r=OrdinaryJournal.Recover(path,id);Check(!r.Corrupt&&r.DurableSequence==1,"uncommitted batch suffix ignored");Retire(path);
        }
        foreach(string stage in new[]{"data-flush","data-flushed","head-write","head-flush","head-flushed"}){
            string path=New();Guid id=Guid.NewGuid();bool armed=false;
            using(var j=new OrdinaryJournal(path,id,faultCut:s=>{if(armed&&s==stage)throw new IOException(stage);})){
                j.Commit(Events(1,1),1,0,0);armed=true;
                try{j.Commit(Events(2,maximum),(ulong)maximum+1,0,0);throw new Exception("cut missed");}catch(IOException){}
                Check(j.DurableSequence==1,"head ACK follows completed flush");
            }
            var r=OrdinaryJournal.Recover(path,id);Check(r.DurableSequence==1||r.DurableSequence==(ulong)maximum+1,"old or fully persisted group recovered");Retire(path);
        }
        // Cross-bank cuts: page limit two, one seed page, 46 new events.
        // Group1 publishes through16, rotation checkpoints16, group2 through46,
        // rotation checkpoints46, then final partial group through47.
        foreach(string stage in new[]{"data-page-written","data-flush","data-flushed","head-write","head-flush","head-flushed","rotation-start","checkpoint-write","checkpoint-flush","checkpoint-flushed"})
        for(int ordinal=1;ordinal<=(stage=="data-page-written"?4:stage.StartsWith("head")?5:stage.StartsWith("data")?3:2);ordinal++){
            string path=New();Guid id=Guid.NewGuid();bool armed=false,threw=false;int seen=0;ulong acknowledged=0;
            using(var j=new OrdinaryJournal(path,id,2,s=>{if(armed&&s==stage&&++seen==ordinal)throw new IOException("spanning cut");})){
                j.Commit(Events(1,1),1,0,0);armed=true;
                try{j.Commit(Events(2,46),47,0,0);}catch(IOException){threw=true;}
                acknowledged=j.DurableSequence;
                Check(threw&&acknowledged is 1 or 16 or 46,"cross-bank ACK limited to completed groups");
            }
            var r=OrdinaryJournal.Recover(path,id);
            Check(r.DurableSequence>=acknowledged&&r.DurableSequence<=47&&r.DurableSequence is 1 or 16 or 46 or 47,"cross-bank recovery contains only complete committed group");
            Check(!r.Complete,"interrupted spanning batch never complete");Retire(path);
        }
        {
            string path=New();Guid id=Guid.NewGuid();
            using(var j=new OrdinaryJournal(path,id)){j.Commit(Events(1,1),1,0,0);j.Commit(Events(2,60),61,0,0);}
            var head=Directory.GetFiles(path,"head*.bin").Select(File.ReadAllBytes).OrderByDescending(b=>BitConverter.ToUInt64(b,24)).First();
            string bank=Path.Combine(path,$"bank{BitConverter.ToUInt64(head,32)}.bin");
            using(var f=new FileStream(bank,FileMode.Open,FileAccess.ReadWrite)){f.Position=OrdinaryJournal.EventOffset+2*4096+256+2*256+9;int b=f.ReadByte();f.Position--;f.WriteByte((byte)(b^64));f.Flush(true);}
            var r=OrdinaryJournal.Recover(path,id);Check(r.Corrupt&&r.DurableSequence==18&&!r.Complete,"torn grouped page retains exact verified prefix and uncertainty");Retire(path);
        }
        File.WriteAllText(Path.Combine(output,"batch-cuts.json"),JsonSerializer.Serialize(new{checks,pages,maximum,judgment="PASS"}));Console.WriteLine($"PASS batch cuts {checks} checks / {pages} individual page cuts");
    }
}
