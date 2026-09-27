namespace NovaCore.Diagnostics;

// Bounded binary checkpoint. Events retain their original sequence, birth and payload.
public static class OrdinaryCheckpoint
{
    // Two 8192-entry resource ledgers * four optional 257-byte events, plus
    // 64*128 role records and operation/phase state fit below this power of two.
    public const int MaximumBytes=32*1024*1024;
    public static byte[] Encode(OrdinaryState s)
    {
        if(s.Open.Count>1024||s.Resources.Count>8192||s.SubmittedResources.Count>8192||s.Context.Count>128||s.SubmittedContext.Count>128||s.Pending.Count>64||s.Recordings.Count>1024)throw new InvalidDataException("Checkpoint state capacity.");
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        w.Write(1);w.Write(s.Sequence);w.Write(s.Faults);w.Write(s.LastProducedObserved);w.Write(s.Dropped);w.Write(s.Clean);
        Events(w,s.Open.Values);Resources(w,s.Resources);Events(w,s.Context.Values);Resources(w,s.SubmittedResources);Events(w,s.SubmittedContext.Values);
        if(s.LastPhase.Count>21||s.PendingContexts.Count>64||s.PendingContexts.Values.Any(v=>v.Count>128))throw new InvalidDataException("Checkpoint context capacity.");
        Events(w,s.LastPhase.Values);w.Write(s.PendingContexts.Count);foreach(var p in s.PendingContexts.OrderBy(p=>p.Key)){w.Write(p.Key);Events(w,p.Value.Values);}
        Events(w,s.Recordings.Values);Events(w,s.Pending.Values);Event(w,s.LastSubmit);Event(w,s.LastCompletion);Event(w,s.LastPresent);Event(w,s.LastPublication);
        if(stream.Length>MaximumBytes)throw new InvalidDataException("Checkpoint capacity exceeded.");return stream.ToArray();
    }
    public static OrdinaryState Decode(byte[] bytes)
    {
        if(bytes.Length>MaximumBytes)throw new InvalidDataException("Oversized checkpoint.");
        using var stream=new MemoryStream(bytes,false);using var r=new BinaryReader(stream);
        if(r.ReadInt32()!=1)throw new InvalidDataException("Checkpoint schema.");
        var s=new OrdinaryState{Sequence=r.ReadUInt64(),Faults=r.ReadInt64(),LastProducedObserved=r.ReadUInt64(),Dropped=r.ReadInt64(),Clean=r.ReadBoolean()};
        s.Open=ReadEvents(r,1024).ToDictionary(e=>$"{e.Words[4]}:{e.Words[7]}");s.Resources=ReadResources(r);
        s.Context=ReadEvents(r,128).ToDictionary(e=>e.Words[17]);s.SubmittedResources=ReadResources(r);s.SubmittedContext=ReadEvents(r,128).ToDictionary(e=>e.Words[17]);
        s.LastPhase=ReadEvents(r,21).ToDictionary(e=>e.Words[4]);int contexts=Count(r,64);for(int i=0;i<contexts;i++)s.PendingContexts.Add(r.ReadUInt64(),ReadEvents(r,128).ToDictionary(e=>e.Words[17]));
        s.Recordings=ReadEvents(r,1024).ToDictionary(e=>e.Words[14]);s.Pending=ReadEvents(r,64).ToDictionary(e=>e.Words[8]);s.LastSubmit=ReadEvent(r);s.LastCompletion=ReadEvent(r);s.LastPresent=ReadEvent(r);s.LastPublication=ReadEvent(r);
        if(stream.Position!=stream.Length)throw new InvalidDataException("Checkpoint trailing bytes.");return s;
    }
    static void Event(BinaryWriter w,OrdinaryEvent? e){w.Write(e is not null);if(e is not null)w.Write(e.Encode());}
    static OrdinaryEvent? ReadEvent(BinaryReader r){if(!r.ReadBoolean())return null;var b=r.ReadBytes(256);if(b.Length!=256||!OrdinaryProtocol.Valid(b,BitConverter.ToUInt64(b)))throw new InvalidDataException("Checkpoint event checksum.");return OrdinaryEvent.Read(b);}
    static void Events(BinaryWriter w,IEnumerable<OrdinaryEvent> values){var list=values.OrderBy(e=>e.Sequence).ToArray();w.Write(list.Length);foreach(var e in list)Event(w,e);}
    static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Checkpoint count.");return n;}
    static List<OrdinaryEvent> ReadEvents(BinaryReader r,int max){int n=Count(r,max);var list=new List<OrdinaryEvent>(n);for(int i=0;i<n;i++)list.Add(ReadEvent(r)??throw new InvalidDataException("Null checkpoint entry."));return list;}
    static void Resources(BinaryWriter w,Dictionary<ulong,ResourceHistory> values){w.Write(values.Count);foreach(var p in values.OrderBy(x=>x.Key)){if(p.Key==0||p.Key!=p.Value.Birth.Words[14])throw new InvalidDataException("Birth identity.");w.Write(p.Key);Event(w,p.Value.Birth);Event(w,p.Value.Binding);Event(w,p.Value.Mapping);Event(w,p.Value.Retirement);}}
    static Dictionary<ulong,ResourceHistory> ReadResources(BinaryReader r){int n=Count(r,8192);var d=new Dictionary<ulong,ResourceHistory>(n);for(int i=0;i<n;i++){ulong key=r.ReadUInt64();var birth=ReadEvent(r)??throw new InvalidDataException("Missing birth.");if(key==0||key!=birth.Words[14])throw new InvalidDataException("Birth identity.");d.Add(key,new(birth,ReadEvent(r),ReadEvent(r),ReadEvent(r)));}return d;}
}
