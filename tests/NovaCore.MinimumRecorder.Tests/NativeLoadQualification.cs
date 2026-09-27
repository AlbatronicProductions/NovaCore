using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using NovaCore.Diagnostics;

// Real shared producer + real journal; no renderer, Vulkan loader or GPU.
static class NativeLoadQualification
{
    public static void Run(string[] args)
    {
        string root=Path.GetFullPath(args[1]);Directory.CreateDirectory(root);
        double delayMs=double.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture);
        int frames=int.Parse(args[3]),fps=int.Parse(args[4]);bool expectLoss=args[5]=="loss";
        bool bursts=args.Length>6&&args[6]=="bursts";int burstCount=0;
        long managedRetainedBefore=GC.GetTotalMemory(true);
        // Reflection lets the identical harness falsify the pre-correction binary.
        int batch=(int?)typeof(OrdinaryJournal).GetField("MaximumBatchRecords")?.GetRawConstantValue()??15;
        if(args.Contains("legacy-pages"))batch=15; // test-only reproduction of former observer scheduling
        Guid id=Guid.NewGuid();string path=Path.Combine(root,id.ToString("N"));
        using var mapping=new OrdinaryMapping("Local\\NativeLoad-"+id,id,true);
        using var ready=new ManualResetEventSlim();Exception? failure=null;
        long high=0;long commits=0;var commitTimes=new List<double>();
        long allocatedWorker=0,bytesWritten=0,flushes=0,rotations=0;double workerCpuMs=0;
        // A minimum latency floor is an explicit falsifier, not a reconstruction
        // of the lost historical disk stalls. Delay is outside the producer.
        var worker=new Thread(()=>{
            try{
                long before=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();
                using var journal=new OrdinaryJournal(path,id,faultCut:s=>{if(s=="data-flush"&&delayMs>0){long start=Stopwatch.GetTimestamp();while(Stopwatch.GetElapsedTime(start).TotalMilliseconds<delayMs)Thread.SpinWait(64);}});
                var records=new List<OrdinaryEvent>(batch);var bytes=new byte[256];long consumed=0;
                mapping.Write(11,Stopwatch.GetTimestamp());mapping.Write(15,1);ready.Set();
                while(true){
                    records.Clear();long produced=mapping.Read(8);high=Math.Max(high,produced-consumed);
                    for(int i=0;i<batch&&consumed<produced;i++){
                        if(!mapping.TryRead(consumed+1,bytes))throw new Exception("torn shared record");
                        records.Add(OrdinaryEvent.Read(bytes));consumed++;
                    }
                    long t=Stopwatch.GetTimestamp(),writes=journal.BytesWritten;
                    journal.Commit(records,(ulong)produced,mapping.Read(12)|mapping.Read(13),mapping.Read(16));
                    if(writes!=journal.BytesWritten){commitTimes.Add(Stopwatch.GetElapsedTime(t).TotalMilliseconds);commits++;}
                    mapping.Write(10,(long)journal.DurableSequence);mapping.Write(9,consumed);mapping.Write(11,Stopwatch.GetTimestamp());
                    if(mapping.Read(14)!=0&&consumed==mapping.Read(8))break;
                    if(records.Count==0)Thread.Sleep(4);
                }
                allocatedWorker=GC.GetAllocatedBytesForCurrentThread()-before;workerCpuMs=watch.Elapsed.TotalMilliseconds;
                bytesWritten=journal.BytesWritten;flushes=journal.Flushes;rotations=journal.Rotations;
            }catch(Exception e){failure=e;ready.Set();}
        });worker.IsBackground=true;worker.Start();ready.Wait();if(failure!=null)throw failure;
        mapping.Emit(OrdinaryPhase.Session);
        var times=new double[frames];long allocated=0,producerHigh=0;ulong op=0;
        // Warm the exact stack-only producer before measuring allocation.
        for(int i=0;i<128;i++)Frame(mapping,ref op,(ulong)i+1);
        while(mapping.Read(9)<mapping.Read(8))Thread.Sleep(1);
        var clock=Stopwatch.StartNew();
        for(int i=0;i<frames;i++){
            // Paced 19-event renderer envelopes, emitted as a burst each frame.
            double due=i*1000.0/fps;
            while(clock.Elapsed.TotalMilliseconds<due){if(due-clock.Elapsed.TotalMilliseconds>2)Thread.Sleep(1);else Thread.SpinWait(64);}
            long a=GC.GetAllocatedBytesForCurrentThread(),t=Stopwatch.GetTimestamp();
            Frame(mapping,ref op,(ulong)i+129);
            if(bursts&&i%(fps*2)==0){Burst(mapping);burstCount++;}
            times[i]=Stopwatch.GetElapsedTime(t).TotalMicroseconds;allocated+=GC.GetAllocatedBytesForCurrentThread()-a;
            producerHigh=Math.Max(producerHigh,mapping.Read(8)-mapping.Read(9));
            if(failure!=null)throw failure;
        }
        double elapsed=clock.Elapsed.TotalSeconds;long dropped=mapping.Read(16),faults=mapping.Read(12)|mapping.Read(13);
        // Harness termination, outside timed producer. Do not lose the close
        // proof merely because an intentional over-envelope test filled storage.
        var deadline=Stopwatch.StartNew();while(mapping.Read(9)<mapping.Read(8)&&deadline.ElapsedMilliseconds<30000)Thread.Sleep(1);
        if(!mapping.Close(true))throw new Exception("close refused after drain");
        if(!worker.Join(30000))throw new Exception("worker failed to drain");if(failure!=null)throw failure;
        long managedRetainedAfter=GC.GetTotalMemory(true);
        var recovered=OrdinaryJournal.Recover(path,id);Array.Sort(times);commitTimes.Sort();
        double P(double[] a,double q)=>a[Math.Min(a.Length-1,(int)Math.Ceiling(a.Length*q)-1)];
        var ct=commitTimes.ToArray();
        var report=new{batch,frames,fps,eventsPerFrame=19,bursts,burstCount,eventsPerBurst=802,delayMs,elapsed,attemptedRate=(frames*19+burstCount*802)/elapsed,highWater=Math.Max(high,producerHigh),capacity=OrdinaryProtocol.Capacity,dropped,faults,recovered.Complete,recoveredFaults=recovered.State.Faults,recoveredDropped=recovered.State.Dropped,open=recovered.State.Open.Count,pending=recovered.State.Pending.Count,produced=mapping.Read(8),durable=mapping.Read(10),allocatedProducerBytes=allocated,producerUs=new{median=P(times,.5),p95=P(times,.95),p99=P(times,.99),max=times[^1]},commits,commitMs=new{median=P(ct,.5),p95=P(ct,.95),p99=P(ct,.99),max=ct[^1]},bytesWritten,flushes,rotations,allocatedWorker,workerWallMs=workerCpuMs,session=path};
        File.WriteAllText(Path.Combine(root,"load.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(report));
        File.WriteAllText(Path.Combine(root,"memory.json"),JsonSerializer.Serialize(new{managedRetainedBefore,managedRetainedAfter,delta=managedRetainedAfter-managedRetainedBefore,peakWorkingSetBytes=Process.GetCurrentProcess().PeakWorkingSet64,mappingBytes=OrdinaryProtocol.MappingBytes,maximumCopiedRecords=batch,maximumBatchFileBytes=batch/15*4096,limits="Process-level managed delta includes test timing/results; producer allocation is measured separately. Mapping and copied-record bounds are exact."}));
        if(allocated!=0)throw new Exception("producer allocated");
        if(expectLoss){if(dropped==0||recovered.Complete||(faults&1)==0)throw new Exception("overflow falsifier did not overflow explicitly");}
        else if(dropped!=0||faults!=0||!recovered.Complete||recovered.State.Open.Count!=0||recovered.State.Pending.Count!=0||mapping.Read(8)!=mapping.Read(10))throw new Exception("load qualification failed");
    }
    static void Frame(OrdinaryMapping m,ref ulong operation,ulong frame)
    {
        Span<byte> bytes=stackalloc byte[256];bytes.Clear();var w=MemoryMarshal.Cast<byte,ulong>(bytes);w[2]=frame;
        ulong f=++operation;Emit(m,bytes,OrdinaryPhase.Frame,OrdinaryEdge.Enter,f);
        Pair(m,bytes,OrdinaryPhase.Update,ref operation);Pair(m,bytes,OrdinaryPhase.Fence,ref operation);
        ulong d=++operation;Emit(m,bytes,OrdinaryPhase.Draw,OrdinaryEdge.Enter,d);
        Pair(m,bytes,OrdinaryPhase.Acquire,ref operation);Pair(m,bytes,OrdinaryPhase.Record,ref operation);Pair(m,bytes,OrdinaryPhase.Record,ref operation);
        w[8]=frame;Pair(m,bytes,OrdinaryPhase.Submit,ref operation);Emit(m,bytes,OrdinaryPhase.Completed,OrdinaryEdge.Info,0);
        Pair(m,bytes,OrdinaryPhase.Present,ref operation);Emit(m,bytes,OrdinaryPhase.Draw,OrdinaryEdge.Return,d);Emit(m,bytes,OrdinaryPhase.Frame,OrdinaryEdge.Return,f);
    }
    static void Pair(OrdinaryMapping m,Span<byte> b,OrdinaryPhase p,ref ulong op){ulong o=++op;Emit(m,b,p,OrdinaryEdge.Enter,o);Emit(m,b,p,OrdinaryEdge.Return,o);}
    static void Burst(OrdinaryMapping m)
    {
        Span<byte> bytes=stackalloc byte[256];bytes.Clear();var w=MemoryMarshal.Cast<byte,ulong>(bytes);w[4]=(ulong)OrdinaryPhase.Resource;w[18]=2;
        for(ulong i=1;i<=401;i++){w[13]=i;w[14]=(ulong)m.Read(8)+1;w[17]=1;m.EmitBytes(bytes);w[17]=2;m.EmitBytes(bytes);}
    }
    static void Emit(OrdinaryMapping m,Span<byte> b,OrdinaryPhase p,OrdinaryEdge e,ulong op){var w=MemoryMarshal.Cast<byte,ulong>(b);w[4]=(ulong)p;w[5]=(ulong)e;w[7]=op;m.EmitBytes(b);}
}
