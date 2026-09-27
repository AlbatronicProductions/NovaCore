using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

// Independent preallocated history. A diagnostic-cost result must neither seal
// the production journal nor consume its future first-failure tail.
internal sealed class DiagnosticCostCheckpoint(string output,int capacity=131072,int tailCapacity=16384)
{
    readonly byte[] ring=new byte[capacity*512],tail=new byte[tailCapacity*512];
    int tailCount;bool finished;byte[]? finishHeader;long serial,cutoff;Task? writer;
    internal bool Frozen{get;private set;}
    internal string DirectoryPath=>Path.Combine(output,"diagnostic-cost");
    internal bool Completed=>writer?.IsCompletedSuccessfully==true;
    internal string? Failure=>writer?.IsFaulted==true?writer.Exception!.GetBaseException().Message:null;
    internal void Write(byte[] record){
        if(finished)return;
        if(Frozen){if(tailCount==tailCapacity)throw new InvalidDataException("Diagnostic cost continuation overflow");record.CopyTo(tail,tailCount++*512);}
        else {long id=BitConverter.ToInt64(record,0);record.CopyTo(ring,(int)((id-1)%capacity)*512);}
    }
    internal void Freeze(byte[] trigger,byte[] header,string reason,object state,long capture){
        if(Frozen)return;Frozen=true;serial=BitConverter.ToInt64(trigger,0);
        var raw=(byte[])trigger.Clone();var h=(byte[])header.Clone();BitConverter.GetBytes(serial).CopyTo(h,56);BitConverter.GetBytes(serial).CopyTo(h,64);BitConverter.GetBytes((long)capacity).CopyTo(h,120);
        var frozenState=JsonSerializer.SerializeToElement(state);
        var held=AlarmJournal.OpenPublished(Path.Combine(output,"frozen"));
        writer=Task.Run(()=>{
            string dir=DirectoryPath,alarm=Path.Combine(dir,"first-alarm");Directory.CreateDirectory(alarm);
            File.Copy(Path.Combine(output,"identity.json"),Path.Combine(dir,"identity.json"));
            using(var file=new FileStream(Path.Combine(dir,"breadcrumbs.bin"),FileMode.CreateNew,FileAccess.Write,FileShare.Read)){file.Write(h);file.Write(ring);file.Flush(true);}
            File.WriteAllText(Path.Combine(alarm,"alarm.json"),JsonSerializer.Serialize(new{reason,sealSerial=serial,triggerRecord=raw,triggerSha256=Convert.ToHexStringLower(SHA256.HashData(raw)),capture,state=frozenState}));
            long began=Stopwatch.GetTimestamp();while(!Volatile.Read(ref finished)){if(Stopwatch.GetElapsedTime(began).TotalSeconds>3.5)throw new IOException("Diagnostic checkpoint drain not finalized");Thread.Sleep(1);}
            AlarmJournal.PinCaptures(output,alarm,held,Math.Max(capture,cutoff),4);
            var end=finishHeader!;BitConverter.GetBytes((long)tailCapacity).CopyTo(end,120);
            using var after=new FileStream(Path.Combine(dir,"termination-tail.bin"),FileMode.CreateNew,FileAccess.Write,FileShare.Read);after.Write(end);after.Write(tail);after.Flush(true);
        });
    }
    internal void Finish(byte[] header,long lastCapture){if(!Frozen||finished)return;finishHeader=(byte[])header.Clone();cutoff=lastCapture;Volatile.Write(ref finished,true);}
    internal void Wait(){if(writer!=null&&!writer.Wait(4000))throw new IOException("Diagnostic checkpoint writer exceeded existing bounded preservation window");}
}
