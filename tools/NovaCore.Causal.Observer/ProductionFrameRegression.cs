using System.Text.Json;
internal static class ProductionFrameRegression
{
    static byte[] Event(long frame,long qpc,long phase=11,long kind=1,long op=0,long result=0){
        var b=new byte[512];void W(int n,long v)=>BitConverter.GetBytes(v).CopyTo(b,n*8);
        W(0,frame);W(1,qpc);W(2,frame);W(3,frame);W(4,frame-1);W(5,phase);W(6,kind);W(7,result);W(9,op);return b;
    }
    internal static int Run(string output){
        Directory.CreateDirectory(output);int checks=0;
        void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);checks++;}
        const long frequency=60000; // Exact boundary = 1000 ticks; independent of floating rounding.
        foreach(long step in new long[]{999,1000,1001}){
            var w=new ProductionFrameWatch(frequency);w.Observe(Event(1,1));
            for(int f=2;f<=4;f++)Check((w.Observe(Event(f,1+(f-1)*step))!=null)==(step==1001&&f==4),"exact 60 FPS boundary/recurrence");
        }
        var mixed=new ProductionFrameWatch(frequency);long q=1;mixed.Observe(Event(1,q));
        long[] sequence=[1001,999,1001,999,1001];
        for(int i=0;i<sequence.Length;i++)Check(mixed.Observe(Event(i+2,q+=sequence[i]))==null,"isolated/alternating overruns treated as sustained");
        Check(mixed.Observe(Event(7,q+=1001))!=null,"3 of 4 full-frame overruns missed");
        long first=mixed.Trigger!.Frame;Check(mixed.Observe(Event(8,q+=2000))==null&&mixed.Trigger.Frame==first,"first trigger replaced");
        foreach(long phase in new long[]{27,29}){
            var w=new ProductionFrameWatch(frequency);w.Observe(Event(1,1));w.Observe(Event(2,2,phase,2,2));
            Check(w.Observe(Event(2,2001))==null&&w.Observe(Event(3,4001))==null&&w.DiagnosticIntervalsExcluded==2,"diagnostic overlap misclassified");
            Check(w.Observe(Event(4,6001))==null&&w.Observe(Event(5,8001))==null&&w.Observe(Event(6,10001))!=null,"ordinary gate disappeared after diagnostic interval");
        }
        var reset=new ProductionFrameWatch(frequency);reset.Observe(Event(1,1));reset.Observe(Event(2,2001));reset.Observe(Event(3,4001));
        reset.Observe(Event(3,5000,4,0));Check(reset.Observe(Event(4,200000))==null&&reset.Observe(Event(5,201000))==null,"swapchain gap represented as ordinary frame");
        Check(reset.Observe(Event(7,202000))==null&&reset.DiscontinuitiesExcluded==3,"missing present paired across gap");
        bool rejected=false;try{reset.Observe(Event(8,202000));}catch(InvalidDataException){rejected=true;}Check(rejected,"nonadvancing clock accepted");
        var native=new CaptureCostWatch(frequency,qualifyOrdinaryFence:false);
        var legacy=new CaptureCostWatch(frequency);string? fault=null;
        for(int i=1;i<=3;i++){
            var begin=Event(i,i*10000,24,0);var end=Event(i,i*10000+540,24,1);
            native.Observe(begin);legacy.Observe(begin);Check(native.Observe(end)==null,"9ms fence still has obsolete native gate");fault=legacy.Observe(end);
        }
        Check(fault?.StartsWith("repeated noncapture fence cost")==true,"unrelated limited diagnostic gate changed");
        Check(NativeClosurePolicy.Check(true,0,0,18863,18863,1110714008)!=null,"observed callback unwind accepted as native closure");
        Check(NativeClosurePolicy.Check(true,1,0,3,3,0)!=null,"cleanup timestamp alone accepted");
        Check(NativeClosurePolicy.Check(true,1,2,3,2,0)!=null,"unfinished GPU frame accepted");
        Check(NativeClosurePolicy.Check(true,1,2,3,3,1)!=null,"live allocation accepted");
        Check(NativeClosurePolicy.Check(true,1,2,3,3,0)==null,"complete native closure rejected");
        Check(NativeClosurePolicy.Check(false,0,0,0,0,0)==null,"pre-render exit incorrectly requires native cleanup");
        File.WriteAllText(Path.Combine(output,"production-frame-regression.json"),JsonSerializer.Serialize(new{passed=true,checks,minimumFps=60,frameBudgetMs=ProductionFrameWatch.FrameBudgetMilliseconds,gpuExecution=false}));
        Console.WriteLine($"PASS production frame checks={checks}; exact 60 FPS boundary; CPU-only");return 0;
    }
    internal static int Replay(string directory){
        using var stream=File.OpenRead(Path.Combine(directory,"breadcrumbs.bin"));var header=new byte[128];stream.ReadExactly(header);
        var watch=new ProductionFrameWatch(BitConverter.ToInt64(header,40));long previous=0;string? first=null;
        foreach(var record in CausalJournal.Records(directory).Where(r=>BitConverter.ToInt64(r,0)>0).OrderBy(r=>BitConverter.ToInt64(r,0))){
            long serial=BitConverter.ToInt64(record,0);ulong hash=14695981039346656037;
            for(int i=0;i<504;i++)if(i<488||i>=496){hash^=record[i];hash=unchecked(hash*1099511628211);}
            if(hash!=BitConverter.ToUInt64(record,488)||serial!=BitConverter.ToInt64(record,504)||(previous!=0&&serial!=previous+1))throw new InvalidDataException("Replay checksum/continuity");
            previous=serial;first??=watch.Observe(record);
        }
        Console.WriteLine(JsonSerializer.Serialize(new{fault=first,watch=watch.Evidence}));return first==null?0:3;
    }
}
