using System.Diagnostics;
using System.Text.Json;

namespace NovaCore.Diagnostics;

public static class OrdinaryObserver
{
    public static int Run(string name,Guid session,string directory)
    {
        using var mapping=new OrdinaryMapping(name,session,false);
        using var owner=Process.GetProcessById(checked((int)mapping.OwnerPid));
        _=owner.SafeHandle; // Retain this exact incarnation through termination observation.
        if(owner.StartTime.ToUniversalTime().Ticks!=mapping.OwnerStartTicks)throw new InvalidDataException("Stale producer process incarnation.");
        // CPU fixtures predating host ownership use a separate explicit fixture
        // path; only sessions with both ownership records can become eligible.
        using var ownership=File.Exists(Path.Combine(directory,"ownership.lock"))?OrdinaryOwnership.Join(Path.Combine(directory,"ownership.lock")):null;
        // No GPU library is referenced or loaded by this executable.
        try{
            OrdinaryRecovery recovered;
            {
            using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var wall=Stopwatch.StartNew();long allocated=GC.GetTotalAllocatedBytes(true);var commits=new double[4096];long commitCount=0;double longestCommitMs=0;var ring=new OrdinaryProgressBounds();var lag=new OrdinaryProgressBounds();
            using var journal=new OrdinaryJournal(directory,session);
            OrdinaryOwnership.Write(Path.Combine(directory,"observer.json"),OrdinaryOwnership.ProcessIdentity(session));
            mapping.Write(OrdinaryProtocol.WorkerHeartbeat,Stopwatch.GetTimestamp());mapping.Write(OrdinaryProtocol.Ready,1);
            var records=new List<OrdinaryEvent>(OrdinaryJournal.MaximumBatchRecords);var bytes=new byte[256];long consumed=0;
            while(true){
                records.Clear();long produced=mapping.Read(OrdinaryProtocol.Produced);
                if(produced<consumed||produced-consumed>OrdinaryProtocol.Capacity)throw new InvalidDataException("Shared producer watermark corrupted.");
                for(int i=0;i<OrdinaryJournal.MaximumBatchRecords&&consumed<produced;i++){
                    if(!mapping.TryRead(consumed+1,bytes)){mapping.Fault(OrdinaryFault.Corruption,true);throw new InvalidDataException("Shared record failed sequence/checksum.");}
                    records.Add(OrdinaryEvent.Read(bytes));consumed++;
                }
                // Bounded multi-page batch; release only after durable bank/head.
                // Never wait to fill a batch: low-volume and close latency stay bounded.
                long stamp=Stopwatch.GetTimestamp(),writes=journal.BytesWritten;
                journal.Commit(records,(ulong)produced,mapping.Read(12)|mapping.Read(13),mapping.Read(16));
                if(writes!=journal.BytesWritten){var ms=(Stopwatch.GetTimestamp()-stamp)*1000.0/Stopwatch.Frequency;commits[commitCount++%commits.Length]=ms;longestCommitMs=Math.Max(longestCommitMs,ms);}
                long before=mapping.Read(OrdinaryProtocol.Produced),durable=checked((long)journal.DurableSequence);
                mapping.Write(OrdinaryProtocol.Durable,durable);lag.Published(before,durable,mapping.Read(OrdinaryProtocol.Produced));
                before=mapping.Read(OrdinaryProtocol.Produced);mapping.Write(OrdinaryProtocol.Consumed,consumed);ring.Published(before,consumed,mapping.Read(OrdinaryProtocol.Produced));
                if(ring.Lower>OrdinaryProtocol.Capacity)throw new InvalidDataException("Ring high water exceeds admitted capacity.");
                mapping.Write(OrdinaryProtocol.WorkerHeartbeat,Stopwatch.GetTimestamp());
                if(consumed==mapping.Read(8)&&(mapping.Read(14)!=0||owner.HasExited))break;
                if(records.Count==0)Thread.Sleep(4);
            }
            var ordered=commits.Take((int)Math.Min(commitCount,commits.Length)).Order().ToArray();double P(double p)=>ordered.Length==0?0:ordered[Math.Min(ordered.Length-1,(int)Math.Ceiling(p*ordered.Length)-1)];
            OrdinaryStorage.Json(Path.Combine(directory,"performance.json"),new{wallMs=wall.Elapsed.TotalMilliseconds,cpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds,allocatedBytes=GC.GetTotalAllocatedBytes(true)-allocated,managedRetainedBytes=GC.GetTotalMemory(true),peakWorkingSetBytes=process.PeakWorkingSet64,journal.BytesWritten,journal.Flushes,journal.Rotations,commitCount,sampledCommits=ordered.Length,p50ms=P(.5),p95ms=P(.95),p99ms=P(.99),maxMs=P(1),longestCommitMs,ringPeakLower=ring.Lower,ringPeakUpper=Math.Min(OrdinaryProtocol.Capacity,ring.Upper),volatileLagPeakLower=lag.Lower,volatileLagPeakUpper=lag.Upper,peakSemantics="Observer publication brackets; exact peak lies within inclusive lower/upper bounds. Ring upper is additionally bounded by capacity.",storageBoundBytes=OrdinaryStorage.MaximumSessionBytes});
            recovered=OrdinaryJournal.Recover(directory,session);
            }
            // All journal handles are disposed before durable finalization.
            WriteReport(directory,recovered);
            OrdinaryOwnership.Finalize(directory,session,recovered);
            OrdinaryTermination.Observe(directory,session,owner);
            return 0;
        }catch(Exception e){
            mapping.Fault(e is InvalidDataException?OrdinaryFault.Corruption:OrdinaryFault.Io,true);mapping.Write(OrdinaryProtocol.Ready,-1);
            // A failed volume may reject this too. Recovery still explicitly reports the uncertain suffix.
            try{var text=e.ToString();if(text.Length>16000)text=text[..16000]+" [bounded failure text; raw journal retained]";OrdinaryStorage.Write(Path.Combine(directory,"observer-failure.txt"),System.Text.Encoding.UTF8.GetBytes(text),FileMode.Create);}catch(IOException){}
            return 2;
        }
    }
    public static void WriteReport(string path,OrdinaryRecovery r)=>OrdinaryStorage.Write(Path.Combine(path,"recovery.json"),ReportBytes(r),FileMode.Create);
    internal static byte[] ReportBytes(OrdinaryRecovery r)=>JsonSerializer.SerializeToUtf8Bytes(new{
        r.Session,r.Revision,r.CommittedTarget,r.DurableSequence,r.CheckpointSequence,r.Corrupt,r.Detail,r.Complete,r.Terminal,
        r.State.Clean,r.State.Faults,r.State.Dropped,r.State.LastProducedObserved,r.State.Open,r.State.LastPhase,
        r.State.LastSubmit,r.State.LastCompletion,r.State.LastPresent,r.State.LastPublication,r.State.Pending,
        liveResourceCount=r.State.Resources.Count,retainedResourceCount=r.State.SubmittedResources.Count,
        provenance="Full bounded incarnation, binding, retirement and per-submission role ledger is retained in the binary checkpoint/journal; recovery summary is regeneratable."
    },new JsonSerializerOptions{WriteIndented=true});
}
