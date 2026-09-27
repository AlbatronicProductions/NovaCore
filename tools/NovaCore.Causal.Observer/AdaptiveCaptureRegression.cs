using System.Security.Cryptography;
using System.Text.Json;
internal static class AdaptiveCaptureRegression
{
    internal static int Run(string directory){
        Directory.CreateDirectory(directory);int checks=0;void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);checks++;}
        byte[] Event(long serial,long operation,long request=100,long admitted=3,long durable=3,long state0=0,long state1=0){var b=new byte[512];void W(int i,long value)=>BitConverter.GetBytes(value).CopyTo(b,i*8);
            W(0,serial);W(1,serial*100);W(2,serial);W(3,serial);W(4,serial);W(5,30);W(6,2);W(9,operation);W(10,request);W(11,admitted);W(12,durable);W(13,state0);W(14,state1);W(63,serial);
            ulong hash=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){hash^=b[i];hash=unchecked(hash*1099511628211);}W(61,unchecked((long)hash));return b;}
        var p=new AdaptiveCapturePolicy(true,1000);Check(p.Request("repeated capture fence cost exceeds 6ms (3 of last 4)",100,1000),"cost not accepted");Check(!p.Request("repeated topology witness fence cost",101,1001)&&p.RequestSerial==100,"first cost request replaced");
        Check(p.Check(2000,0,0)==null&&p.Check(2001,0,0)!=null,"ack deadline changed");p.Observe(Event(11,1,durable:2,state1:4));Check(p.Check(4000,2,2)==null&&p.Check(4001,2,2)!=null,"publication bound changed");p.Observe(Event(12,2));Check(p.Check(5000,3,2)!=null&&p.Check(5000,3,3)==null,"drain ignored audit cutoff");
        foreach(var fault in new[]{"GPU frame exceeded 500ms","native failure -4","repeated noncapture fence cost exceeds 6ms","capture corruption","missing progress"})Check(!new AdaptiveCapturePolicy(true,1000).Request(fault,1,1),"production fault treated as diagnostic cost");
        Check(!new AdaptiveCapturePolicy(false,1000).Request("repeated capture fence cost",1,1),"policy enabled without authority");
        for(int bad=0;bad<8;bad++){
            var q=new AdaptiveCapturePolicy(true,1000);q.Request("repeated capture fence cost",100,1000);bool rejected=false;
            try{
                if(bad!=0)q.Observe(Event(11,1));
                if(bad==0)q.Observe(Event(12,2));else if(bad==1)q.Observe(Event(12,1));else if(bad==2)q.Observe(Event(12,2,request:101));else if(bad==3)q.Observe(Event(12,2,admitted:4));else if(bad==4)q.Observe(Event(12,2,durable:2));else if(bad==5)q.Observe(Event(12,2,state1:4));
                else {var e=Event(12,2);BitConverter.GetBytes(bad==6?27L:29L).CopyTo(e,40);BitConverter.GetBytes(2L).CopyTo(e,72);q.Observe(e);}
            }catch(InvalidDataException){rejected=true;}Check(rejected,"bad control transition accepted "+bad);
        }
        var ending=new AdaptiveCapturePolicy(true,1000);
        Check(ending.CompletionFailure(0,0)==null,"unused fallback required drain");
        ending.Request("repeated capture fence cost",100,1000);
        Check(ending.CompletionFailure(0,0)!=null,"early exit hid missing acknowledgement");
        ending.Observe(Event(11,1));Check(ending.CompletionFailure(3,3)!=null,"early exit hid missing drain");
        ending.Observe(Event(12,2));
        Check(ending.CompletionFailure(3,2)!=null&&ending.CompletionFailure(2,3)!=null&&ending.CompletionFailure(4,4)!=null,"final cutoff identity mismatch accepted");
        Check(ending.CompletionFailure(3,3)==null,"complete cutoff rejected");
        File.WriteAllText(Path.Combine(directory,"identity.json"),"{\"gpuExecution\":false}");Directory.CreateDirectory(Path.Combine(directory,"frozen"));
        var header=new byte[128];BitConverter.GetBytes(0x314c41535541434eL).CopyTo(header,0);BitConverter.GetBytes(2L).CopyTo(header,8);BitConverter.GetBytes(512L).CopyTo(header,16);BitConverter.GetBytes(1000L).CopyTo(header,40);BitConverter.GetBytes(8L).CopyTo(header,120);
        var cost=new DiagnosticCostCheckpoint(directory,8,8);using var main=new AlarmJournal(directory,8,header);
        for(int i=1;i<=16;i++){var e=Event(i,0);cost.Write(e);main.Write(e);}
        var trigger=Event(16,0);cost.Freeze(trigger,header,"capture cost",new{frame=16},0);cost.Freeze(Event(17,0),header,"later",new{},0);
        for(int i=17;i<=19;i++){var e=Event(i,0);cost.Write(e);main.Write(e);}cost.Finish(header,0);cost.Wait();
        var before=SHA256.HashData(File.ReadAllBytes(Path.Combine(cost.DirectoryPath,"breadcrumbs.bin")));
        for(int i=20;i<=200;i++){var e=Event(i,0);cost.Write(e);main.Write(e);}
        main.WriteTrigger(Event(201,0),()=>{});main.Seal(201,"later genuine production fault",Event(201,0),header,new{},0);main.FinishPins();
        Check(main.Sealed&&main.SealSerial==201,"cost consumed real first-alarm ownership");Check(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(cost.DirectoryPath,"breadcrumbs.bin")))) ,"diagnostic cost window aged out");
        var ids=CausalJournal.Records(cost.DirectoryPath).Where(r=>BitConverter.ToInt64(r,0)>0).Select(r=>BitConverter.ToInt64(r,0)).Order().ToArray();Check(ids.SequenceEqual(Enumerable.Range(9,11).Select(i=>(long)i)),"cost causal pre/post window changed");
        File.WriteAllText(Path.Combine(directory,"adaptive-regression.json"),JsonSerializer.Serialize(new{passed=true,checks,gpuExecution=false}));Console.WriteLine($"PASS adaptive policy/checkpoint checks={checks}; CPU-only");return 0;
    }
}
