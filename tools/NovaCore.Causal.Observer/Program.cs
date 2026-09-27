using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Interop;

// This program only launches one explicitly selected process. No retry/reboot loop.
internal static unsafe class Program
{
    const long Magic=0x314c41535541434e, Header=128, Record=512, Capacity=16384, JournalRecords=131072;
    [DllImport("user32.dll")] static extern bool PostMessage(nint window,uint message,nint w,nint l);
    static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--inspect")return CausalJournal.Inspect(args[1]);
        if(args.Length==2&&args[0]=="--inspect-frozen")return FrozenSnapshot.Inspect(args[1]);
        if(args.Length==3&&args[0]=="--test-frozen")return FrozenRegression.Run(args[1],args[2]);
        if(args.Length==2&&args[0]=="--verify-authorities")return FrozenAuthorities.Inspect(args[1]);
        if(args.Length==2&&args[0]=="--test-startup")return StartupRegression.Run(args[1]);
        if(args.Length==2&&args[0]=="--test-recording-calls")return RecordingCallRegression.Run(args[1]);
        if(args.Length==2&&args[0]=="--test-first-alarm")return FirstAlarmRegression.Run(args[1]);
        if(args.Length==3&&args[0]=="--test-topology")return TopologyRegression.Run(args[1],args[2]);
        if(args.Length==2&&args[0]=="--replay-capture-cost")return FirstAlarmRegression.Replay(args[1]);
        if(args.Length==2&&args[0]=="--test-adaptive")return AdaptiveCaptureRegression.Run(args[1]);
        if(args.Length==2&&args[0]=="--test-production-frame")return ProductionFrameRegression.Run(args[1]);
        if(args.Length==2&&args[0]=="--replay-production-frame")return ProductionFrameRegression.Replay(args[1]);
        var values=new Dictionary<string,string>(StringComparer.Ordinal);
        for(var i=0;i<args.Length;i+=2){if(i+1>=args.Length)throw new ArgumentException("Every option needs a value.");values.Add(args[i],args[i+1]);}
        var app=Path.GetFullPath(values["--app"]);var output=Path.GetFullPath(values["--output"]);
        var stage=values["--stage"];var seconds=int.Parse(values["--seconds"]);
        if(seconds is <1 or >180)throw new ArgumentException("Duration must be 1–180 seconds.");
        bool mock=stage.StartsWith("mock-",StringComparison.Ordinal);
        bool frozenRequired=!mock||stage.StartsWith("mock-frozen",StringComparison.Ordinal);
        if(mock&&Path.GetFileName(app)!=(frozenRequired?"NovaCoreFrozenCaptureMock.exe":"NovaCoreCausalRecorderMock.exe"))throw new InvalidOperationException("Mock mode is restricted to the CPU-only diagnostic executable.");
        if(!mock){using var gate=JsonDocument.Parse(File.ReadAllText(values["--qualification"]));var g=gate.RootElement;
            if(!g.GetProperty("liveGatePassed").GetBoolean()||!g.GetProperty("allowedStages").EnumerateArray().Any(s=>s.GetString()==stage)||!g.TryGetProperty("frozenEvidenceContract",out var contract)||contract.GetInt32()!=1||!g.GetProperty("projectControlExposureAuthorized").GetBoolean())throw new InvalidOperationException("This stage has not passed its frozen-evidence preflight and Project Control authorization gate.");
            if(!string.Equals(app,g.GetProperty("application").GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Gate belongs to a different application.");
            if(g.TryGetProperty("maximumSeconds",out var duration)&&seconds>duration.GetInt32())throw new InvalidOperationException("Duration exceeds the authorized exposure.");
            if(!g.TryGetProperty("startupLifecycleContract",out var lifecycle)||lifecycle.GetInt32()!=1)throw new InvalidOperationException("Missing qualified startup lifecycle contract.");
            foreach(var (role,path) in FrozenAuthorities.Verify(g))values["asset-"+role]=path;}
        if(!mock){using var gate=JsonDocument.Parse(File.ReadAllText(values["--qualification"]));if(gate.RootElement.TryGetProperty("adaptiveNativeEvidence",out var adaptive)&&adaptive.GetBoolean()){
            if(stage!="matching")throw new InvalidOperationException("Adaptive native policy only authorized for matching route");values["adaptive-native"]="1";}}
        if(Directory.Exists(output))throw new IOException("Use a new evidence directory for every observation.");
        Directory.CreateDirectory(output);
        if(frozenRequired)Directory.CreateDirectory(Path.Combine(output,"frozen"));
        if(!mock)File.Copy(values["--qualification"],Path.Combine(output,"authorization-and-authorities.json"));
        string mappingName="Local\\NovaCore.Causal."+Guid.NewGuid().ToString("N");
        using var mapping=MemoryMappedFile.CreateNew(mappingName,Header+Capacity*Record,MemoryMappedFileAccess.ReadWrite);
        using var view=mapping.CreateViewAccessor();byte* pointer=null;view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
        try{return Observe(values,app,output,stage,seconds,mock,mappingName,pointer);}
        finally{view.SafeMemoryMappedViewHandle.ReleasePointer();}
    }
    static int Observe(Dictionary<string,string> options,string app,string output,string stage,int seconds,bool mock,string name,byte* memory)
    {
        long* header=(long*)memory;
        header[0]=Magic;header[1]=2;header[2]=Record;header[3]=Capacity;header[5]=Stopwatch.Frequency;
        header[14]=Stopwatch.GetTimestamp();
        header[15]=JournalRecords;
        Volatile.Write(ref header[10],1);
        using var journal=new AlarmJournal(output,JournalRecords,new ReadOnlySpan<byte>(memory,(int)Header));
        var start=new ProcessStartInfo(app){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(app)!,RedirectStandardOutput=true,RedirectStandardError=true};
        start.Environment["NOVACORE_CAUSAL_MAPPING"]=name;
        using var lifecycleMapping=MemoryMappedFile.CreateNew(name+".Startup",DiagnosticStartup.Bytes);
        using var lifecycleView=lifecycleMapping.CreateViewAccessor();
        byte* lifecyclePointer=null;lifecycleView.SafeMemoryMappedViewHandle.AcquirePointer(ref lifecyclePointer);
        using var lifecyclePointerLease=new PointerLease(lifecycleView);
        long* signals=(long*)lifecyclePointer;
        signals[0]=DiagnosticStartup.Magic;signals[1]=DiagnosticStartup.Version;signals[2]=DiagnosticStartup.Bytes;
        signals[24]=Stopwatch.Frequency;signals[4]=Stopwatch.GetTimestamp();
        bool adaptiveNative=options.ContainsKey("adaptive-native")||stage.StartsWith("mock-frozen-adaptive",StringComparison.Ordinal);if(adaptiveNative)signals[25]=2026092301;
        start.Environment["NOVACORE_STARTUP_MAPPING"]=name+".Startup";
        bool frozenRequired=!mock||stage.StartsWith("mock-frozen",StringComparison.Ordinal);
        if(frozenRequired)start.Environment["NOVACORE_FROZEN_CAPTURE"]=Path.Combine(output,"frozen");
        if(!mock)foreach(string role in new[]{"production","regional","oracle"})start.Environment["NOVACORE_FROZEN_ASSET_"+role.ToUpperInvariant()]=options["asset-"+role];
        if(!mock)start.Environment["NOVACORE_CAUSAL_ROUTE"]=stage;
        if(mock)start.ArgumentList.Add(stage);
        long launchQpc=Stopwatch.GetTimestamp();
        using var process=Process.Start(start)??throw new InvalidOperationException("Child did not start.");
        var lifecycle=new StartupLifecycle(launchQpc,Stopwatch.Frequency);var lifecycleSnapshot=new long[32];
        // The app already has bounded background session logging. Drain inherited
        // console streams without another unbounded file or render-thread disk path.
        var stdout=process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
        var stderr=process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        bool killAttempted=false;
        try {
        var identity=new{stage,app,pid=process.Id,processStartUtc=process.StartTime.ToUniversalTime(),startedUtc=DateTime.UtcNow,
            executableSha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(app))),seconds,mappingCapacity=Capacity,journalRecords=JournalRecords,
            gpuExecution=!mock,startupLifecycleContract=1,stopPolicy="UI 60s; post-click startup 60s overall with loading 30s/init 30s/submission 5s/completion 1s/steady 1s phases; first fatal result, diagnostic loss, 500ms completed GPU operation, 1s unresolved GPU operation or missing steady progress; single close then terminate after 2s"};
        File.WriteAllText(Path.Combine(output,"identity.json"),JsonSerializer.Serialize(identity,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Observing PID {process.Id}, stage {stage}; evidence {output}");
        var buffer=new byte[Record];long read=0,durable=0,overwritten=0,torn=0,corrupt=0,firstQpc=0,lastQpc=0;
        long lastFlush=Stopwatch.GetTimestamp(),startQpc=lastFlush,stopQpc=0;string? stop=null;bool terminated=false;
        var phases=new long[32];long lastFrame=0,submitted=0,completed=0,readyQpc=0;int nativeResult=0;
        long targetFrames=0;double minimumEarthAltitude=double.MaxValue,maximumEarthAltitude=0;
        long rendererStoppedQpc=0,firstStopDecisionQpc=0,producerStopReason=0;
        long lastFrozenFrame=0,lastFrozenIdentity=0,lastFrozenQpc=0,firstAuthorityQpc=0;
        double maximumFlushMilliseconds=0,maximumLoopMilliseconds=0;long previousLoop=0;
        var allocations=new Dictionary<ulong,(ulong Bytes,long Birth)>();ulong liveAllocationBytes=0,peakAllocationBytes=0;long allocationCreates=0,allocationFrees=0;
        var resourceLedger=new ResourceLedger();long lastLedgerFlush=0;
        var recordingCalls=new RecordingCallProgress();
        var topologyWitnesses=new TopologyWitnessWatch(Stopwatch.Frequency,resourceLedger);
        var captureCosts=new CaptureCostWatch(Stopwatch.Frequency,topology:topologyWitnesses,qualifyOrdinaryFence:!adaptiveNative);
        var productionFrames=adaptiveNative?new ProductionFrameWatch(Stopwatch.Frequency):null;
        var liveFrozenAudit=frozenRequired?new LiveFrozenAudit(output,!mock):null;
        var adaptiveCapture=new AdaptiveCapturePolicy(adaptiveNative,Stopwatch.Frequency);
        var costCheckpoint=adaptiveNative?new DiagnosticCostCheckpoint(output):null;
        bool costCheckpointFinished=false;
        bool TimedPhase(long phase)=>readyQpc!=0||phase is 5 or 9 or 10 or 11 or 22 or 23 or 24;
        void SignalStop()
        {
            if(stop==null)return;
            if(stopQpc==0){firstStopDecisionQpc=stopQpc=Stopwatch.GetTimestamp();
                Volatile.Write(ref signals[5],1);Volatile.Write(ref header[9],1);
                if(process.MainWindowHandle!=0)PostMessage(process.MainWindowHandle,0x10,0,0);}
        }
        void StopAndSeal()
        {
            if(stop==null)return;
            SignalStop();
            if(stop=="stage duration reached"||journal.Sealed)return;
            journal.Seal(read,stop,(byte[])buffer.Clone(),new ReadOnlySpan<byte>(memory,(int)Header),
                new{lastFrame,submitted,completed,lastFrozenIdentity,lastFrozenFrame,liveAllocationBytes,peakAllocationBytes,
                    firstStopDecisionQpc,recordingCalls=recordingCalls.Evidence,costSamples=captureCosts.TriggerSamples,ordinarySamples=captureCosts.OrdinaryTriggerSamples,productionFrames=productionFrames?.Evidence,adaptiveCapture=adaptiveCapture.Evidence,latestScheduled=captureCosts.LatestScheduled,topologyWitnesses=topologyWitnesses.Evidence},
                captureCosts.Trigger?.Capture??captureCosts.LatestScheduled?.Identity??lastFrozenIdentity);
            resourceLedger.Save(Path.Combine(output,"first-alarm"));
            topologyWitnesses.Save(Path.Combine(output,"first-alarm"));
        }
        while(true)
        {
            long now=Stopwatch.GetTimestamp();Volatile.Write(ref header[14],now);
            Volatile.Write(ref signals[4],now);
            if(previousLoop!=0)maximumLoopMilliseconds=Math.Max(maximumLoopMilliseconds,(now-previousLoop)*1000d/Stopwatch.Frequency);
            previousLoop=now;
            long emitted=Volatile.Read(ref header[6]);
            if(emitted-read>Capacity){overwritten+=emitted-read-Capacity;read=emitted-Capacity;stop??="ring overwritten";StopAndSeal();}
            for(var limit=0;read<emitted&&limit<4096;limit++)
            {
                long wanted=read+1;byte* slot=memory+Header+((wanted-1)%Capacity)*Record;
                long commit=Volatile.Read(ref *(long*)(slot+504));
                if(commit!=wanted)break;
                new ReadOnlySpan<byte>(slot,(int)Record).CopyTo(buffer);
                if(Volatile.Read(ref *(long*)(slot+504))!=commit||BitConverter.ToInt64(buffer,0)!=wanted){torn++;break;}
                if(Checksum(buffer)!=BitConverter.ToUInt64(buffer,488)){corrupt++;stop??="record checksum mismatch";StopAndSeal();break;}
                long qpc=BitConverter.ToInt64(buffer,8),phase=BitConverter.ToInt64(buffer,40),kind=BitConverter.ToInt64(buffer,48),result=BitConverter.ToInt64(buffer,56);
                lastFrame=BitConverter.ToInt64(buffer,16);submitted=BitConverter.ToInt64(buffer,24);completed=BitConverter.ToInt64(buffer,32);
                string? diagnosticCost=null;
                try{costCheckpoint?.Write(buffer);adaptiveCapture.Observe(buffer);}catch(Exception error){stop??=error.Message;}
                try{stop??=productionFrames?.Observe(buffer);}catch(Exception error){stop??=error.Message;}
                try{resourceLedger.Observe(buffer);}catch(Exception error){stop??=error.Message;}
                try{var topologyFailure=topologyWitnesses.Observe(buffer);if(topologyFailure!=null&&adaptiveNative&&AdaptiveCapturePolicy.IsCost(topologyFailure))diagnosticCost=topologyFailure;else stop??=topologyFailure;}catch(Exception error){stop??=error.Message;}
                recordingCalls.Observe(buffer);stop??=recordingCalls.Failure;
                try{var cost=captureCosts.Observe(buffer);if(cost!=null&&adaptiveNative&&AdaptiveCapturePolicy.IsCost(cost))diagnosticCost??=cost;else stop??=cost;}catch(Exception error){stop??=error.Message;}
                firstQpc=firstQpc==0?qpc:firstQpc;lastQpc=qpc;
                if(phase==1&&kind==1&&result==0)readyQpc=qpc;
                if(phase==12&&kind==1&&result==0&&completed==submitted)rendererStoppedQpc=qpc;
                if(phase==25&&kind==2){producerStopReason=BitConverter.ToInt64(buffer,72);if(producerStopReason==2)stop??="observer heartbeat expired; renderer ended exposure";}
                if(phase==26&&kind==2&&firstAuthorityQpc==0)firstAuthorityQpc=qpc;
                if(phase==27&&kind==2&&BitConverter.ToUInt64(buffer,72) is 4 or 5){long id=BitConverter.ToInt64(buffer,80);if(id>lastFrozenIdentity){lastFrozenIdentity=id;lastFrozenFrame=BitConverter.ToInt64(buffer,88);lastFrozenQpc=qpc;}}
                if(phase is >1 and <32&&kind==0)phases[phase]=qpc;
                if(phase is >1 and <32&&kind==1&&phases[phase]!=0){if(TimedPhase(phase)&&(qpc-phases[phase])/(double)Stopwatch.Frequency>.5)stop??=$"operation {phase} exceeded 500ms";phases[phase]=0;}
                // Out-of-date/suboptimal are recognized presentation transitions, not fatal device errors.
                if(result<0&&result!=-1000001004){nativeResult=(int)result;stop??=$"native failure {result}, phase {phase}";}
                if(phase==21&&result!=0)stop??="Vulkan validation error";
                if(phase==20&&kind==2){ulong usage=BitConverter.ToUInt64(buffer,80),budget=BitConverter.ToUInt64(buffer,88);if(budget>0&&(usage>budget||budget-usage<176ul*1024*1024))stop??="GPU heap lacks one capture-storage allocation of budget headroom";}
                if(phase==14&&kind==2&&BitConverter.ToInt64(buffer,72)==3&&BitConverter.ToInt64(buffer,120)!=0)stop??="GPU terrain reports invalid prepared triangles";
                if(phase==14&&kind==2&&BitConverter.ToInt64(buffer,72)==3&&BitConverter.ToInt64(buffer,128)!=0)stop??="GPU terrain compaction overflow";
                if(phase==14&&kind==2&&BitConverter.ToInt64(buffer,72)==5){float factor=BitConverter.Int32BitsToSingle(BitConverter.ToInt32(buffer,88));if(!float.IsFinite(factor)||factor>64.001f)stop??="GPU terrain tessellation factor invalid";}
                if(phase==14&&kind==2&&BitConverter.ToInt64(buffer,72)==7&&BitConverter.ToDouble(buffer,88)>500)stop??="GPU frame exceeded 500ms";
                if(phase==15&&kind==2&&result==0){
                    ulong op=BitConverter.ToUInt64(buffer,72),handle=BitConverter.ToUInt64(buffer,80),bytes=BitConverter.ToUInt64(buffer,88);
                    if(op==1&&handle!=0){if(allocations.Count>=65536||allocations.ContainsKey(handle))stop??="allocation lifetime ledger overflow or duplicate live handle";
                        else {allocations.Add(handle,(bytes,wanted));liveAllocationBytes+=bytes;peakAllocationBytes=Math.Max(peakAllocationBytes,liveAllocationBytes);allocationCreates++;}}
                    if(op==2&&allocations.Remove(handle,out var freed)){liveAllocationBytes-=freed.Bytes;allocationFrees++;}
                }
                if(phase==14&&kind==2&&BitConverter.ToInt64(buffer,72)==0&&BitConverter.ToUInt64(buffer,(9+39)*8)==6){
                    // The accepted native route is a 3440x1440 borderless App window.
                    // Its existing navigation/status chrome leaves a 3440x1322 viewport.
                    if(adaptiveNative&&!mock&&(BitConverter.ToUInt64(buffer,80)!=3440||BitConverter.ToUInt64(buffer,88)!=1322))stop??="Native route viewport differs from authorized 3440x1322 within the 3440x1440 application";
                    double altitude=BitConverter.ToDouble(buffer,(9+7)*8),dot=0,length=0;
                    for(int axis=0;axis<3;axis++){double c=BitConverter.ToDouble(buffer,(9+4+axis)*8),f=BitConverter.ToDouble(buffer,(9+8+axis)*8);dot+=c*f;length+=c*c;}
                    dot/=Math.Sqrt(length);minimumEarthAltitude=Math.Min(minimumEarthAltitude,altitude);maximumEarthAltitude=Math.Max(maximumEarthAltitude,altitude);
                    bool reached=stage switch{"distant-earth"=>altitude>1e7,"closer-earth"=>altitude is >=1e5 and <=1e6,"ground"=>altitude is >100 and <=1000,_=>altitude<1000&&dot is >-.2 and <.2};
                    if(reached)targetFrames++;
                }
                if(stop==null&&diagnosticCost!=null&&adaptiveCapture.Request(diagnosticCost,wanted,Stopwatch.GetTimestamp())){
                    // Publish the one-way request before any preservation I/O.
                    Volatile.Write(ref signals[21],adaptiveCapture.RequestQpc);Volatile.Write(ref signals[20],wanted);
                    costCheckpoint!.Freeze(buffer,new ReadOnlySpan<byte>(memory,(int)Header).ToArray(),diagnosticCost,
                        new{lastFrame,submitted,completed,liveAllocationBytes,peakAllocationBytes,costSamples=captureCosts.TriggerSamples,topology=topologyWitnesses.Evidence},captureCosts.LatestScheduled?.Identity??lastFrozenIdentity);
                }
                if(stop!=null)journal.WriteTrigger(buffer,SignalStop);else journal.Write(buffer);
                read=wanted;Volatile.Write(ref header[7],read);
                StopAndSeal();
            }
            if(header[11]!=0||header[12]!=0)stop??="producer diagnostic loss";
            liveFrozenAudit?.Update(lastFrozenIdentity);
            if(liveFrozenAudit?.Failure is {} captureFailure)stop??=captureFailure;
            if(!adaptiveCapture.Requested&&frozenRequired&&firstAuthorityQpc!=0&&rendererStoppedQpc==0&&(now-(lastFrozenQpc!=0?lastFrozenQpc:firstAuthorityQpc))/(double)Stopwatch.Frequency>3)stop??="frozen evidence publication absent for 3s";
            stop??=adaptiveCapture.Check(now,lastFrozenIdentity,liveFrozenAudit?.ValidatedIdentity??0);stop??=costCheckpoint?.Failure;
            if(adaptiveCapture.Drained&&!costCheckpointFinished&&lastFrozenIdentity==adaptiveCapture.Cutoff&&liveFrozenAudit?.ValidatedIdentity==adaptiveCapture.Cutoff){
                Directory.CreateDirectory(costCheckpoint!.DirectoryPath);resourceLedger.Save(costCheckpoint.DirectoryPath);topologyWitnesses.Save(costCheckpoint.DirectoryPath);
                costCheckpoint.Finish(new ReadOnlySpan<byte>(memory,(int)Header).ToArray(),adaptiveCapture.Cutoff);costCheckpointFinished=true;
            }
            // Read later milestones first, then their causal predecessors; each timestamp is write-once.
            for(int i=31;i>=0;i--)lifecycleSnapshot[i]=Volatile.Read(ref signals[i]);
            if(lifecycleSnapshot[3]!=0&&lifecycleSnapshot[3]!=process.Id)stop??="startup channel owner mismatch";
            var lifecycleFailure=lifecycle.Update(lifecycleSnapshot,Stopwatch.GetTimestamp(),process.HasExited);stop??=lifecycleFailure;
            if(firstQpc!=0){
                // A backlogged reader must not mistake an unread end event for
                // a hung call. Completed durations still use every event's QPC.
                if(read>=Volatile.Read(ref header[6]))for(int phase=2;phase<phases.Length;phase++)if(TimedPhase(phase)&&phases[phase]!=0&&(now-phases[phase])/(double)Stopwatch.Frequency>1)stop??=$"renderer operation {phase} has not returned for 1s";
                if(lifecycle.Started&&lifecycleSnapshot[13]==0&&rendererStoppedQpc==0&&(now-Volatile.Read(ref header[13]))/(double)Stopwatch.Frequency>1&&!process.HasExited)stop??="steady renderer progress absent beyond deadline";
                if(rendererStoppedQpc!=0&&(now-rendererStoppedQpc)/(double)Stopwatch.Frequency>2&&!process.HasExited)stop??="application did not exit after completed renderer cleanup";
                if(lifecycle.Started&&(now-lifecycle.SteadyQpc)/(double)Stopwatch.Frequency>=seconds)stop??="stage duration reached";}
            StopAndSeal();
            if((now-lastFlush)/(double)Stopwatch.Frequency>=.1||stop!=null||process.HasExited)
            {
                if((now-lastLedgerFlush)/(double)Stopwatch.Frequency>=1||stop!=null||process.HasExited){resourceLedger.Save(output);topologyWitnesses.Save(output);lastLedgerFlush=now;}
                long flushBegan=Stopwatch.GetTimestamp();durable=read;Volatile.Write(ref header[8],durable);
                journal.Flush(new ReadOnlySpan<byte>(memory,(int)Header));lastFlush=Stopwatch.GetTimestamp();
                maximumFlushMilliseconds=Math.Max(maximumFlushMilliseconds,(Stopwatch.GetTimestamp()-flushBegan)*1000d/Stopwatch.Frequency);
                // Tiny replaceable state file gives the durable boundary independently of a clean process exit.
                File.WriteAllText(Path.Combine(output,"startup-lifecycle.json"),JsonSerializer.Serialize(lifecycle.Evidence));
                File.WriteAllText(Path.Combine(output,"progress.tmp"),JsonSerializer.Serialize(new{emitted,received=read,durable,lastFrame,submitted,completed,lastQpc,targetFrames,liveAllocationBytes,peakAllocationBytes,startupPhase=lifecycle.Phase,recordingCalls=recordingCalls.Evidence,utc=DateTime.UtcNow,stop}));
                File.Move(Path.Combine(output,"progress.tmp"),Path.Combine(output,"progress.json"),true);
            }
            if(stopQpc!=0&&!process.HasExited&&!killAttempted&&(now-stopQpc)/(double)Stopwatch.Frequency>2)
            {
                // The retained Process object identifies exactly our child, never a name-based process group.
                killAttempted=true;process.Kill();terminated=true;process.WaitForExit(3000);
            }
            if(process.HasExited&&read>=Volatile.Read(ref header[6]))break;
            if(process.HasExited&&read<Volatile.Read(ref header[6])&&Volatile.Read(ref header[6])-read<=Capacity&&
                (Volatile.Read(ref *(long*)(memory+Header+(read%Capacity)*Record+504))!=read+1||corrupt!=0))
            {stop??="child exited with incomplete or corrupt final record";break;}
            if(killAttempted&&!process.HasExited&&(now-stopQpc)/(double)Stopwatch.Frequency>7)
                throw new InvalidOperationException("Exact child did not exit after one termination attempt; no relaunch is permitted.");
            Thread.Sleep(10);
        }
        journal.Flush(new ReadOnlySpan<byte>(memory,(int)Header));
        for(int i=31;i>=0;i--)lifecycleSnapshot[i]=Volatile.Read(ref signals[i]);
        var finalLifecycleFailure=lifecycle.Update(lifecycleSnapshot,Stopwatch.GetTimestamp(),process.HasExited);stop??=finalLifecycleFailure;
        var finalNativeClosureFailure=mock?null:NativeClosurePolicy.Check(lifecycle.Started,lifecycleSnapshot[14],rendererStoppedQpc,submitted,completed,liveAllocationBytes);
        File.WriteAllText(Path.Combine(output,"startup-lifecycle.json"),JsonSerializer.Serialize(lifecycle.Evidence));
        liveFrozenAudit?.Finish(lastFrozenIdentity);
        if(costCheckpoint?.Frozen==true){
            Directory.CreateDirectory(costCheckpoint.DirectoryPath);resourceLedger.Save(costCheckpoint.DirectoryPath);topologyWitnesses.Save(costCheckpoint.DirectoryPath);
            costCheckpoint.Finish(new ReadOnlySpan<byte>(memory,(int)Header).ToArray(),adaptiveCapture.Cutoff);costCheckpoint.Wait();
        }
        if(liveFrozenAudit?.Failure is {} finalCaptureFailure)stop??=finalCaptureFailure;
        bool frozenClean=!frozenRequired;string? frozenFailure=null;
        string? frozenProofDirectory=costCheckpoint?.Frozen==true?costCheckpoint.DirectoryPath:null;
        if(frozenRequired){try{frozenClean=FrozenSnapshot.Inspect(output,frozenProofDirectory)==0;if(frozenProofDirectory!=null)frozenClean&=FrozenSnapshot.InspectPinned(frozenProofDirectory,maximum:4);}catch(Exception error){frozenFailure=error.Message;File.WriteAllText(Path.Combine(output,"frozen-inspection-failure.txt"),error.ToString());}}
        bool authoritiesUnchanged=true;
        if(!mock){try{using var seal=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"authorization-and-authorities.json")));FrozenAuthorities.Verify(seal.RootElement);}catch(Exception error){authoritiesUnchanged=false;stop??=error.Message;}}
        if(stop is null or "stage duration reached")stop=finalLifecycleFailure??finalNativeClosureFailure??liveFrozenAudit?.Failure??adaptiveCapture.CompletionFailure(lastFrozenIdentity,liveFrozenAudit?.ValidatedIdentity??0)??costCheckpoint?.Failure??(!frozenClean?"final frozen capture validation failed":!authoritiesUnchanged?"final authority validation failed":stop);
        StopAndSeal();journal.FinishPins();
        bool? alarmCapturesClean=journal.Sealed?FrozenSnapshot.InspectPinned(output,frozenProofDirectory):null;
        var summary=new{identity,stop=stop??"child exited",exitCode=process.ExitCode,terminated,emitted=header[6],received=read,durable,overwritten,torn,corrupt,
            producerOverruns=header[11],producerContentionDrops=header[12],lastFrame,submitted,completed,nativeResult,firstQpc,lastQpc,
            targetFrames,minimumEarthAltitude,maximumEarthAltitude,liveAllocationBytes,peakAllocationBytes,allocationCreates,allocationFrees,
            rendererStoppedQpc,firstStopDecisionQpc,producerStopReason,maximumLoopMilliseconds,maximumFlushMilliseconds,
            firstAlarmSealed=journal.Sealed,firstAlarmSerial=journal.SealSerial,terminationTailDropped=journal.TailDropped,
            alarmCapturesClean,
            startup=lifecycle.Evidence,recordingCalls=recordingCalls.Evidence,
            frozenRequired,frozenClean,frozenFailure,authoritiesUnchanged,lastFrozenFrame,lastFrozenIdentity,lastFrozenQpc,
            liveFrozenValidatedIdentity=liveFrozenAudit?.ValidatedIdentity,liveFrozenAuditFailure=liveFrozenAudit?.Failure,
            adaptiveCapture=adaptiveCapture.Evidence,productionFrames=productionFrames?.Evidence,finalNativeClosureFailure,frozenProofDirectory,costCheckpointFinished,costCheckpointComplete=costCheckpoint?.Completed};
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(summary));
        bool clean=(stop is null or "stage duration reached")&&process.ExitCode==0&&!terminated&&overwritten==0&&corrupt==0&&header[11]==0&&header[12]==0&&(mock||targetFrames>=120);
        clean&=frozenClean&&authoritiesUnchanged&&(mock||lifecycle.Started);
        return clean?0:3;
        } finally {
            // Any observer I/O/parse failure must also end its exact child.
            if(!process.HasExited){Volatile.Write(ref signals[5],1);Volatile.Write(ref header[9],1);if(process.MainWindowHandle!=0)PostMessage(process.MainWindowHandle,0x10,0,0);if(!process.WaitForExit(2000)&&!killAttempted){killAttempted=true;process.Kill();process.WaitForExit(3000);}}
        }
    }
    static ulong Checksum(ReadOnlySpan<byte> entry){ulong hash=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){hash^=entry[i];hash=unchecked(hash*1099511628211);}return hash;}
    sealed class PointerLease(MemoryMappedViewAccessor view):IDisposable {public void Dispose()=>view.SafeMemoryMappedViewHandle.ReleasePointer();}
}
