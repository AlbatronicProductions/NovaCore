using System.Security.Cryptography;
using System.Text.Json;

// Observer-owned storage. The original ring changes ownership exactly once:
// rolling -> sealed. Shutdown has its own bounded append-only tail; it cannot
// overwrite the alarm window even if a human supervisor handles it much later.
internal sealed class AlarmJournal : IDisposable
{
    const int Header=128,Record=512,TailCapacity=16384;
    readonly FileStream primary;
    readonly string directory;
    readonly long capacity;
    FileStream? tail;
    long tailCount;
    internal bool Sealed {get;private set;}
    internal long SealSerial {get;private set;}
    internal long TailDropped {get;private set;}
    Task? pins;
    internal AlarmJournal(string directory,long capacity,ReadOnlySpan<byte> header)
    {
        this.directory=directory;this.capacity=capacity;
        primary=new(Path.Combine(directory,"breadcrumbs.bin"),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.Read,65536);
        primary.SetLength(Header+capacity*Record);primary.Write(header);primary.Flush(true);
    }
    internal void Write(byte[] record)
    {
        long serial=BitConverter.ToInt64(record,0);
        if(!Sealed){primary.Position=Header+((serial-1)%capacity)*Record;primary.Write(record);}
        else if(tailCount<TailCapacity){tail!.Position=Header+tailCount++*Record;tail.Write(record);}
        else TailDropped++;
    }
    // The triggering write can itself flush a FileStream buffer. Signal the
    // already-qualified stop before even that write, not only before Flush().
    internal void WriteTrigger(byte[] record,Action signalStop){signalStop();Write(record);}
    internal void Seal(long serial,string reason,byte[] trigger,ReadOnlySpan<byte> header,object state,long capture)
    {
        if(Sealed)return;
        // Caller has already signalled the existing stop channels. Ownership is
        // frozen before any potentially slow disk operation below.
        Sealed=true;SealSerial=serial;
        string alarm=Path.Combine(directory,"first-alarm");Directory.CreateDirectory(alarm);
        // Pin already published files by an open handle before copying them.
        // Atomic rename can replace the path but cannot replace these bytes.
        var held=OpenPublished(Path.Combine(directory,"frozen"));
        pins=Task.Run(()=>PinCaptures(directory,alarm,held,capture));
        var bytes=header.ToArray();BitConverter.GetBytes(serial).CopyTo(bytes,7*8);BitConverter.GetBytes(serial).CopyTo(bytes,8*8);
        primary.Flush(true);primary.Position=0;primary.Write(bytes);primary.Flush(true);
        tail=new(Path.Combine(directory,"termination-tail.bin"),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.Read,65536);
        tail.SetLength(Header+TailCapacity*Record);BitConverter.GetBytes((long)TailCapacity).CopyTo(bytes,15*8);tail.Write(bytes);tail.Flush(true);
        using var file=new FileStream(Path.Combine(alarm,"alarm.json"),FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        JsonSerializer.Serialize(file,new{schema=1,reason,sealSerial=serial,utc=DateTime.UtcNow,triggerRecord=trigger,
            triggerSha256=Convert.ToHexStringLower(SHA256.HashData(trigger)),capture,state});file.Flush(true);
    }
    internal void Flush(ReadOnlySpan<byte> header)
    {
        var destination=Sealed?tail!:primary;
        var bytes=header.ToArray();if(Sealed)BitConverter.GetBytes((long)TailCapacity).CopyTo(bytes,15*8);
        destination.Flush(true);destination.Position=0;destination.Write(bytes);destination.Flush(true);
    }
    internal static List<(long Identity,FileStream File)> OpenPublished(string directory)
    {
        var result=new List<(long,FileStream)>(2);
        foreach(int slot in new[]{0,1}){
            FileStream? file=null;
            try{
                file=new(Path.Combine(directory,$"snapshot-{slot}.bin"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                var header=new byte[4096];file.ReadExactly(header);long id=BitConverter.ToInt64(header,24);
                if(BitConverter.ToUInt64(header,0)!=0x314e455a4f52464e||id<=0||BitConverter.ToInt64(header,504)!=id)throw new InvalidDataException("Uncommitted alarm snapshot");
                file.Position=0;result.Add((id,file));file=null;
            }catch(IOException){}catch(UnauthorizedAccessException){}finally{file?.Dispose();}
        }
        return result;
    }
    internal static void PinCaptures(string directory,string alarm,List<(long Identity,FileStream File)> held,long required,int maximum=3)
    {
        var saved=new HashSet<long>();string? failure=null;long began=System.Diagnostics.Stopwatch.GetTimestamp();
        try{
            for(;;){
                foreach(var (id,file) in held)using(file){
                    // Two files at the boundary and, if still in flight, its
                    // exact publication. At most three self-contained captures.
                    if(saved.Contains(id))continue;
                    if(saved.Count>=maximum)throw new InvalidDataException("First alarm capture pin capacity");
                    using var destination=new FileStream(Path.Combine(alarm,$"snapshot-{id}.bin"),FileMode.CreateNew,FileAccess.Write,FileShare.Read);
                    file.CopyTo(destination);destination.Flush(true);saved.Add(id);
                }
                held.Clear();
                if(required==0||saved.Contains(required))break;
                if(System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalSeconds>=3){failure="Trigger capture was not published inside existing 3s evidence bound; retain its exact pending authority in the sealed journal";break;}
                Thread.Sleep(10);held=OpenPublished(Path.Combine(directory,"frozen"));
            }
        }catch(Exception error){failure=error.Message;}
        finally{foreach(var item in held)item.File.Dispose();}
        File.WriteAllText(Path.Combine(alarm,"capture-pins.json"),JsonSerializer.Serialize(new{required,saved,failure}));
    }
    internal void FinishPins(){if(pins!=null&&!pins.Wait(3500))throw new IOException("First alarm preservation worker did not finish");}
    public void Dispose(){primary.Dispose();tail?.Dispose();}
}
