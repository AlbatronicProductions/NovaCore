using System.Text.Json;

internal sealed class ResourceLedger
{
    internal sealed class Life
    {
        public ulong Kind {get;init;} public ulong Handle {get;init;} public long BirthSerial {get;init;}
        public long BirthFrame {get;init;} public long? DeathSerial {get;set;} public ulong[] Facts {get;init;}=[];
    }
    readonly List<Life> history=[];
    readonly Dictionary<(ulong Kind,ulong Handle),Life> active=[];
    internal bool Buffer(ulong handle,ulong birth,ulong bytes)=>active.TryGetValue((3,handle),out var life)&&life.BirthSerial==(long)birth&&life.Facts[0]>=bytes;
    internal void Observe(byte[] record)
    {
        ulong W(int i)=>BitConverter.ToUInt64(record,i*8);
        if(W(5)!=15||W(6)!=2||W(7)!=0)return;
        ulong op=W(9),handle=W(10);if(handle==0)return;
        if(op is 1 or 3 or 5){
            if(history.Count>=65536||active.ContainsKey((op,handle)))throw new InvalidDataException("Resource identity ledger capacity/duplicate birth");
            var life=new Life{Kind=op,Handle=handle,BirthSerial=(long)W(0),BirthFrame=(long)W(2),Facts=[W(11),W(12),W(13),W(14),W(15),W(16)]};
            history.Add(life);active.Add((op,handle),life);
        }
        else if(op is 2 or 4 or 6){if(!active.Remove((op-1,handle),out var life))throw new InvalidDataException("Resource destruction without retained birth");life.DeathSerial=(long)W(0);}
    }
    internal void Save(string directory)
    {
        string temporary=Path.Combine(directory,"resource-lifetimes.tmp"),final=Path.Combine(directory,"resource-lifetimes.json");
        using(var file=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.Read)){JsonSerializer.Serialize(file,new{capacity=65536,history});file.Flush(true);}
        File.Move(temporary,final,true);
    }
    internal static void SelfTest(){
        var ledger=new ResourceLedger();
        void Event(ulong serial,ulong op){var record=new byte[512];foreach(var (word,value) in new[]{(0,serial),(2,7ul),(5,15ul),(6,2ul),(9,op),(10,42ul),(11,4096ul)})BitConverter.GetBytes(value).CopyTo(record,word*8);ledger.Observe(record);}
        Event(1,3);Event(2,4);Event(3,3);
        if(ledger.history.Count!=2||ledger.history[0].DeathSerial!=2||ledger.history[1].BirthSerial!=3||ledger.history[1].DeathSerial!=null)throw new InvalidOperationException("Resource incarnation regression");
        bool duplicate=false;try{Event(4,3);}catch(InvalidDataException){duplicate=true;}if(!duplicate)throw new InvalidOperationException("Duplicate resource birth accepted");
    }
}
