using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

internal static class FirstAlarmRegression
{
    static byte[] ReadShared(string path){using var file=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);var bytes=new byte[checked((int)file.Length)];file.ReadExactly(bytes);return bytes;}
    static byte[] Record(long serial,long frame,long phase,long kind,long qpc,params long[] data)
    {
        var bytes=new byte[512];void W(int i,long value)=>BitConverter.GetBytes(value).CopyTo(bytes,i*8);
        W(0,serial);W(1,qpc);W(2,frame);W(3,frame);W(4,frame-1);W(5,phase);W(6,kind);W(63,serial);
        for(int i=0;i<data.Length;i++)W(9+i,data[i]);
        ulong hash=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){hash^=bytes[i];hash=unchecked(hash*1099511628211);}
        W(61,unchecked((long)hash));return bytes;
    }
    internal static int Replay(string directory)
    {
        var header=File.ReadAllBytes(Path.Combine(directory,"breadcrumbs.bin"))[..128];
        var watch=new CaptureCostWatch(BitConverter.ToInt64(header,40),retainedReplay:true);
        var records=CausalJournal.Records(directory).Where(r=>BitConverter.ToInt64(r,0)>0).OrderBy(r=>BitConverter.ToInt64(r,0));
        long previous=0;foreach(var record in records){
            long serial=BitConverter.ToInt64(record,0);ulong hash=14695981039346656037;
            for(int i=0;i<504;i++)if(i<488||i>=496){hash^=record[i];hash=unchecked(hash*1099511628211);}
            if(hash!=BitConverter.ToUInt64(record,488)||serial!=BitConverter.ToInt64(record,504)||(previous!=0&&serial!=previous+1))throw new InvalidDataException("Replay checksum/continuity");previous=serial;
            var fault=watch.Observe(record);if(fault!=null){Console.WriteLine(JsonSerializer.Serialize(new{fault,serial,watch.ExcludedLeadingRecords,watch.Trigger,watch.TriggerSamples}));return 0;}}
        Console.WriteLine(JsonSerializer.Serialize(new{fault=(string?)null,watch.ExcludedLeadingRecords}));return 0;
    }
    internal static int Run(string output)
    {
        Directory.CreateDirectory(output);int checks=0;
        void Check(bool value,string why){if(!value)throw new InvalidOperationException(why);checks++;}
        const long frequency=1000000;
        var watch=new CaptureCostWatch(frequency);long serial=0,clock=0;
        void Authority(long frame,long transport=2){
            var h=new byte[4096];void W(int i,long v)=>BitConverter.GetBytes(v).CopyTo(h,i*8);
            W(3,frame);W(4,frame);W(11,15);W(15,77);W(16,77);W(17,77);W(45,transport);W(48,4096);W(52,1234);W(54,frame);W(35,91);W(33,92);
            if(transport==4){W(18,30);W(19,28);W(52,0);}
            for(int part=0;part<11;part++){
                int bytes=Math.Min(376,4096-part*376);var r=Record(++serial,frame,26,2,clock,frame,part,11,4096,bytes);
                h.AsSpan(part*376,bytes).CopyTo(r.AsSpan(112));watch.Observe(r);
            }
        }
        string? Fence(long frame,double ms,bool capture=true,long transport=2)
        {
            if(capture){watch.Observe(Record(++serial,frame,27,2,clock,2,frame,frame,4096,15,77));Authority(frame,transport);}
            watch.Observe(Record(++serial,frame,10,1,clock,1,2,frame+63));
            watch.Observe(Record(++serial,frame,24,0,clock,1,99,1,-1));clock+=(long)(ms*1000);
            return watch.Observe(Record(++serial,frame,24,1,clock));
        }
        Check(Fence(1,6.070,false)==null,"Isolated accepted terrain-transition wait rejected");
        Check(Fence(2,3.597)==null,"Closer baseline rejected");
        Check(Fence(3,6.244)==null&&Fence(4,6.394)==null,"Single/pair of slow captures promoted prematurely");
        Check(Fence(5,6.175)!=null,"Repeated ground cost not detected");
        Check(watch.Trigger is {Frame:5,Submission:68,Capture:5,Generation:15,Pupil:77},"Trigger identity wrong");
        Check(Fence(6,20)==null&&watch.Trigger!.Frame==5,"Repeated alarm replaced first trigger");
        watch=new CaptureCostWatch(frequency);
        for(int i=0;i<12;i++)Check(Fence(i+1,i%3==0?6.1:3.5)==null,"Non-repeating cost history rejected");

        byte[] Done(long frame,params long[] data){var r=Record(++serial,frame+1,27,2,clock,data);BitConverter.GetBytes(frame).CopyTo(r,32);return r;}
        string? Completion(long frame,double ms,int broken=-1){
            Fence(frame,.1);
            if(broken!=0)watch.Observe(Done(frame,8,frame,frame,1234,0,frame,broken==1?99:91,92,broken==2?2:0));
            if(broken!=3)watch.Observe(Done(frame,3,frame,frame,broken==4?999:frame+63,broken==5?16:15,77,1,3));
            return watch.Observe(Done(frame,6,broken==6?999:frame,frame,broken==7?4097:4096,BitConverter.DoubleToInt64Bits(broken==8?double.NaN:0.2),BitConverter.DoubleToInt64Bits(ms)));
        }
        watch=new CaptureCostWatch(frequency);
        Check(Completion(1,.015)==null&&Completion(2,.02)==null&&Completion(3,1)==null,"Completion baseline/exact threshold rejected");
        Check(Completion(4,2)==null&&Completion(5,7)==null,"Completion triggered before recurrence");
        Check(Fence(6,.2,false)==null&&Completion(7,2)!=null,"Interleaved completion recurrence absent");
        Check(watch.Trigger is {Frame:7,Submission:70,Completed:7,Capture:7,Generation:15,Pupil:77,Scope:"completion"}&&watch.Trigger.Copy!=null&&watch.Trigger.Authority?.Length==4096,"Completion joined wrong envelope/owner");
        Check(Fence(8,20,false)==null&&Fence(9,20,false)==null&&Fence(10,20,false)!=null&&watch.Trigger!.Frame==7&&watch.OrdinaryTrigger?.Frame==10,"Diagnostic latch masked later ordinary fence alarm");
        for(int broken=0;broken<=11;broken++){
            watch=new CaptureCostWatch(frequency);bool rejected=false;
            try{Completion(1,broken==9?double.NaN:broken==10?double.PositiveInfinity:broken==11?-1:.02,broken);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"Malformed completion evidence accepted: "+broken);
        }
        watch=new CaptureCostWatch(frequency);Completion(1,.02);bool duplicate=false;
        try{watch.Observe(Done(1,6,1,1,4096,0,0));}catch(InvalidDataException){duplicate=true;}Check(duplicate,"Duplicate completion accepted");
        var replayWatch=new CaptureCostWatch(frequency,true);replayWatch.Observe(Done(1,6,1,1,4096,0,0));Check(replayWatch.ExcludedLeadingRecords==1,"Missing replay prefix invented");
        for(int i=1;i<=4;i++){
            replayWatch.Observe(Record(++serial,i,24,0,clock));clock+=7000;
            Check(replayWatch.Observe(Record(++serial,i,24,1,clock))==null,"Unowned leading replay fence classified ordinary");
        }
        Check(replayWatch.ExcludedLeadingRecords==5,"Replay exclusions unreported");
        watch=new CaptureCostWatch(frequency);
        watch.Observe(Record(++serial,1,27,2,clock,2,1,1,4096,15,77));Authority(1,0);
        watch.Observe(Record(++serial,1,10,1,clock,1,2,64));watch.Observe(Done(1,3,1,1,64,15,77,1,3));
        Check(watch.Observe(Done(1,6,1,1,4096,BitConverter.DoubleToInt64Bits(.2),BitConverter.DoubleToInt64Bits(.02)))==null,"Legacy GPU transport without copy telemetry rejected");
        // Actual bytes do not exist at command-record time. The V4 GPU result
        // must join before copied-byte/completion telemetry can become valid.
        for(int broken=-1;broken<22;broken++){
            watch=new CaptureCostWatch(frequency);bool rejected=false;
            try{
                Fence(1,.1,true,4);
                var pack=Done(1,10,1,1,0,123,0x344b5046,0,30,28,30,1,0,1,0,1988,2340,104,0,0,0,0);
                if(broken>=0&&broken<19){int index=broken<16?14+broken:broken==16?10:broken==17?12:13;BitConverter.GetBytes(BitConverter.ToInt64(pack,index*8)^1).CopyTo(pack,index*8);}
                if(broken!=19)watch.Observe(pack);if(broken==20)watch.Observe(pack);
                watch.Observe(Done(1,8,1,1,broken==21?1989:1988,0,1,91,92,0));
                watch.Observe(Done(1,3,1,1,64,15,77,1,3));watch.Observe(Done(1,6,1,1,4096,BitConverter.DoubleToInt64Bits(.2),BitConverter.DoubleToInt64Bits(.02)));
            }catch(InvalidDataException){rejected=true;}
            // A different nonzero metadata buffer handle is legal; its exact
            // lifetime is checked by the retained resource-ledger join.
            Check(rejected==(broken>=0&&broken!=18),"packing completion accepted wrong authority "+broken);
        }

        string sealedDirectory=Path.Combine(output,"delayed-supervisor");Directory.CreateDirectory(sealedDirectory);
        var header=new byte[128];void H(int i,long value)=>BitConverter.GetBytes(value).CopyTo(header,i*8);
        H(0,0x314c41535541434e);H(1,2);H(2,512);H(5,frequency);H(15,8);
        string failing=Path.Combine(output,"trigger-write-failure");Directory.CreateDirectory(failing);
        var failedJournal=new AlarmJournal(failing,8,header);failedJournal.Dispose();bool signalled=false,storageFailed=false;
        try{failedJournal.WriteTrigger(Record(1,1,24,1,1),()=>signalled=true);}catch(ObjectDisposedException){storageFailed=true;}
        Check(signalled&&storageFailed,"Triggering storage failure preceded the stop signal");
        using(var journal=new AlarmJournal(sealedDirectory,8,header)){
            for(int i=1;i<=24;i++)journal.Write(Record(i,i,14,2,i));
            // Stand-in for the production stop callback has already completed.
            bool stopSignalled=true;
            stopSignalled=false;journal.WriteTrigger(Record(24,24,14,2,24),()=>stopSignalled=true);
            Check(stopSignalled,"Production trigger write did not signal stop");
            journal.Seal(24,"first qualified alarm",Record(24,24,14,2,24),header,new{stopSignalled,capture=0,submission=87,resource=91},0);
            var first=SHA256.HashData(ReadShared(Path.Combine(sealedDirectory,"breadcrumbs.bin")));
            var alarm=File.ReadAllBytes(Path.Combine(sealedDirectory,"first-alarm","alarm.json"));
            // Advance far beyond both the old ring and 24-second human delay.
            // No sleeping and no larger rolling retention window is involved.
            for(int i=25;i<=17000;i++){
                journal.Write(Record(i,i,14,2,i*frequency));
                if(i%97==0)journal.Seal(i,"later repeated alarm",Record(i,i,14,2,i),header,new{stopSignalled=false},999);
            }
            journal.Flush(header);journal.FinishPins();
            Check(journal.SealSerial==24&&journal.TailDropped==592,"Seal/tail ownership bound wrong");
            Check(first.AsSpan().SequenceEqual(SHA256.HashData(ReadShared(Path.Combine(sealedDirectory,"breadcrumbs.bin")))) ,"Delayed supervisor aged out first raw evidence");
            Check(alarm.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(sealedDirectory,"first-alarm","alarm.json"))),"Repeated alarms replaced first metadata");
            var retained=CausalJournal.Records(sealedDirectory).Where(r=>BitConverter.ToInt64(r,0)>0).Select(r=>BitConverter.ToInt64(r,0)).Order().ToArray();
            Check(retained[0]==17&&retained[^1]==16408&&retained.Zip(retained.Skip(1)).All(p=>p.Second==p.First+1),"Sealed window/termination tail not joined exactly");
        }
        string pinDirectory=Path.Combine(output,"late-publication");Directory.CreateDirectory(Path.Combine(pinDirectory,"frozen"));
        void Publish(long id){var h=new byte[4096];BitConverter.GetBytes(0x314e455a4f52464eul).CopyTo(h,0);BitConverter.GetBytes(id).CopyTo(h,24);BitConverter.GetBytes(id).CopyTo(h,504);
            string path=Path.Combine(pinDirectory,"frozen",$"snapshot-{id%2}.bin");File.WriteAllBytes(path+".tmp",h);File.Move(path+".tmp",path,true);}
        Publish(1);Publish(2);
        using(var journal=new AlarmJournal(pinDirectory,8,header)){
            journal.Write(Record(1,3,24,1,1));journal.Seal(1,"capture fence cost",Record(1,3,24,1,1),header,new{capture=3},3);
            Publish(3);journal.FinishPins();
            // Even publication path replacement after sealing cannot change pins.
            Publish(5);Publish(6);
            using var pin=JsonDocument.Parse(File.ReadAllText(Path.Combine(pinDirectory,"first-alarm","capture-pins.json")));
            Check(pin.RootElement.GetProperty("failure").ValueKind==JsonValueKind.Null,"Pending completion publication not pinned");
            foreach(long id in new long[]{1,2,3})Check(BitConverter.ToInt64(File.ReadAllBytes(Path.Combine(pinDirectory,"first-alarm",$"snapshot-{id}.bin")),24)==id,"Capture pin overwritten");
        }
        File.WriteAllText(Path.Combine(output,"first-alarm-regression.json"),JsonSerializer.Serialize(new{checks,passed=true,gpuExecution=false,ringEnlarged=false,
            cases=new[]{"isolated transition","baseline costs","3 of 4 recurrence","exact submission/generation identity","repeated alarms","delayed supervisor beyond ring capacity","bounded termination overflow","atomic snapshot replacement","pending capture publication"}}));
        Console.WriteLine($"PASS first-alarm checks={checks}; CPU-only; first boundary immutable; stop integration separately exercised by mock-frozen-repeated-cost");return 0;
    }
}
