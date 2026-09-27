using System.Diagnostics;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Diagnostics;

// All subprocesses are CPU-only. This assembly never references the renderer.
if(args.Length>0&&args[0]=="native-load"){NativeLoadQualification.Run(args);return;}
if(args.Length==2&&args[0]=="batch-cuts"){JournalBatchQualification.Run(args[1]);return;}
if(args.Length==3&&args[0]=="inspect-journal"){
    var r=OrdinaryJournal.Recover(args[1],Guid.Parse(args[2]));
    Console.WriteLine(JsonSerializer.Serialize(new{r.DurableSequence,r.CommittedTarget,r.Complete,r.Corrupt,r.State.Faults,r.State.Dropped,open=r.State.Open.Count,pending=r.State.Pending.Count,r.Terminal}));return;
}
if(args.Length>0&&args[0]=="producer"){
    using var s=new OrdinarySession(args[1],args[2]);
    File.WriteAllText(Path.Combine(args[1],"producer-ready.txt"),s.DirectoryPath);
    ulong op=1;while(true){s.Mapping.Emit(OrdinaryPhase.Update,OrdinaryEdge.Enter,operation:op);s.Mapping.Emit(OrdinaryPhase.Update,OrdinaryEdge.Return,operation:op++);Thread.Sleep(1);}
}
if(args.Length!=2)throw new ArgumentException("output directory, recorder executable required");
string output=Path.GetFullPath(args[0]),worker=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
var checks=new List<string>();var measures=new List<object>();
void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
void Refuses(Action action,string name){bool threw=false;try{action();}catch(Exception e)when(e is IOException or InvalidDataException or InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception){threw=true;}Check(threw,name);}
OrdinaryEvent E(ulong seq,OrdinaryPhase phase=OrdinaryPhase.Heartbeat,OrdinaryEdge edge=OrdinaryEdge.Info,ulong op=0,long result=0,ulong handle=0,ulong birth=0,ulong action=0,ulong kind=0){var e=new OrdinaryEvent();e.Words[0]=seq;e.Words[4]=(ulong)phase;e.Words[5]=(ulong)edge;e.Words[6]=unchecked((ulong)result);e.Words[7]=op;e.Words[13]=handle;e.Words[14]=birth;e.Words[17]=action;e.Words[18]=kind;return e;}
string NewCase(string name)=>Path.Combine(output,name+"-"+Guid.NewGuid().ToString("N"));
void RetireFixture(string path){
 string full=Path.GetFullPath(path);if(!full.StartsWith(output+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Test cleanup escaped output");
 // Recovery publication precedes observer exit. Await that exact process incarnation
 // before deleting this test-owned fixture; never weaken the production ownership lease.
 foreach(string metadata in Directory.GetFiles(full,"observer.json",SearchOption.AllDirectories)){
  using var json=JsonDocument.Parse(File.ReadAllBytes(metadata));var owner=json.RootElement;
  Process? process=null;try{process=Process.GetProcessById(owner.GetProperty("pid").GetInt32());}catch(ArgumentException){}
  using(process){if(process is not null&&process.StartTime.ToUniversalTime().Ticks==owner.GetProperty("startUtcTicks").GetInt64()&&!process.WaitForExit(10000))throw new TimeoutException("Fixture observer did not exit");}
 }
 Directory.Delete(full,true);
}
void Commit(OrdinaryJournal j,params OrdinaryEvent[] e)=>j.Commit(e,e.Length==0?j.State.Sequence:e[^1].Sequence,0,0);
void Flip(string path,long offset){using var f=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.Read);f.Position=offset;int b=f.ReadByte();f.Position=offset;f.WriteByte((byte)(b^0x40));f.Flush(true);}
byte[] LatestHead(string path)=>Directory.GetFiles(path,"head*.bin").Select(File.ReadAllBytes).OrderByDescending(b=>BitConverter.ToUInt64(b,24)).First();
string Bank(string path,byte[] h)=>Path.Combine(path,$"bank{BitConverter.ToUInt64(h,32)}.bin");

// Exhaust every placement of producer increments around one credit publication.
for(long old=0;old<4;old++)for(long before=old;before<12;before++)for(long next=old;next<=before;next++)for(long at=before;at<16;at++)for(long after=at;after<18;after++){
 var b=new OrdinaryProgressBounds();b.Published(old,old,old);b.Published(before,next,after);
 long exact=Math.Max(old,Math.Max(at-old,after-next));
 Check(b.Lower<=exact&&b.Upper>=exact,"publication bracket includes independent interleaving peak");
}
foreach(var v in new[]{(-1L,0L,0L),(2L,3L,4L),(2L,1L,1L)})Refuses(()=>new OrdinaryProgressBounds().Published(v.Item1,v.Item2,v.Item3),"invalid progress bracket refuses");
{var b=new OrdinaryProgressBounds();b.Published(8192,1020,8192);Check(b.Lower==8192&&b.Upper==8192,"full ring boundary exact");b.Published(9212,9212,9212);Check(b.Lower==8192&&b.Upper==8192,"wrap and final drain retain peak");Refuses(()=>b.Published(9211,9212,9212),"produced regression refuses");}
// Pure camera coverage: requests are not observations; ordinary state is untouched.
{
 var good=new BenignRecorderObservation(6,6,6371008.8,73565972.9,73565972.9,73565972.9,false,false,false,true,true);
 var route=new BenignRecorderRoute();Refuses(()=>route.Observe(0,1000,good),"camera request alone cannot establish coverage");
 Check(route.RequestFocus()&&!route.RequestFocus(),"qualification requests production focus once");
 Check(route.Observe(0,1000,good)==BenignRecorderStep.Warmup,"actual camera begins warmup");
 Check(route.Observe(4999,1000,good)==BenignRecorderStep.Warmup&&route.Observe(5000,1000,good)==BenignRecorderStep.StartMeasurement,"timing waits for verified warmup");
 for(int i=1;i<200;i++)Check(route.Observe(5000+i*100,1000,good)==BenignRecorderStep.Hold,"verified held camera "+i);
 Check(route.Observe(25000,1000,good)==BenignRecorderStep.Complete,"bounded covered hold completes");
 foreach(var bad in new[]{good with{FocusBody=1},good with{SubmittedBody=1},good with{Distance=double.NaN},good with{Radius=double.PositiveInfinity},good with{SubmittedDistance=1},good with{ExpectedDistance=double.NaN},good with{Distance=good.Radius},good with{Paused=true},good with{Editing=true},good with{Flight=true},good with{RateOne=false},good with{SurfaceEnabled=false}})Check(!BenignRecorderRoute.Valid(bad),"bad camera state refused "+bad);
 var premature=new BenignRecorderRoute();premature.RequestFocus();premature.Observe(0,1000,good);premature.Observe(5000,1000,good);Refuses(()=>premature.Observe(25000,1000,good),"elapsed time without held frames not coverage");
 var drift=new BenignRecorderRoute();drift.RequestFocus();drift.Observe(0,1000,good);Refuses(()=>drift.Observe(1,1000,good with{Distance=1}),"held camera drift fails without silently refocusing");
}
// Sealed-close proof and independent disk authority; no RAM acknowledgement required.
{
 string p=NewCase("sealed-close");Guid id=Guid.NewGuid();using var m=new OrdinaryMapping("Local\\close-"+id,id,true);
 m.Emit(OrdinaryPhase.Session);Check(m.Close(true),"atomic close admitted");long final=m.Read(8);Check(final==3&&m.Read(14)==1&&!m.Emit(OrdinaryPhase.Frame)&&m.Read(8)==final,"post-close emit cannot reopen producer prefix");
 var bytes=new byte[256];var records=new List<OrdinaryEvent>();for(long i=1;i<=final;i++){Check(m.TryRead(i,bytes),"close record valid "+i);records.Add(OrdinaryEvent.Read(bytes));}
 using(var j=new OrdinaryJournal(p,id)){j.Commit(records,(ulong)final,0,0);j.Rotate();}
 var r=OrdinaryJournal.Recover(p,id);Check(r.Complete&&r.Terminal.StartsWith("COMPLETE")&&m.Read(10)==0,"durable sealed close COMPLETE without volatile ACK");
 Check(!(r with{Corrupt=true}).Complete&&!(r with{CommittedTarget=4}).Complete,"corruption and target mismatch remain uncertain");
 r.State.LastProducedObserved++;Check(!r.Complete,"produced ahead remains uncertain");r.State.LastProducedObserved--;
 r.State.Dropped=1;Check(!r.Complete,"loss remains uncertain");r.State.Dropped=0;
 r.State.Faults=1;Check(!r.Complete,"fault remains uncertain");r.State.Faults=0;
 r.State.Open["x"]=E(1);Check(!r.Complete,"open operation prevents COMPLETE");r.State.Open.Clear();
 r.State.Pending[1]=E(1);Check(!r.Complete,"pending submission prevents COMPLETE");r.State.Pending.Clear();
 r.State.LastPhase[20].Words[17]=0;Check(!r.Complete,"unsealed legacy close remains uncertain");r.State.LastPhase[20].Words[17]=1;
 r.State.LastPhase[20].Words[18]=2;Check(!r.Complete,"wrong final produced proof refused");RetireFixture(p);
 using var full=new OrdinaryMapping("Local\\close-full-"+id,id,true);full.Write(8,8191);Check(!full.Close(true)&&full.Read(14)==0&&(full.Read(12)&1)!=0,"close capacity refusal remains unsealed");
 using var busy=new OrdinaryMapping("Local\\close-busy-"+id,id,true);busy.Write(18,1);Check(!busy.Close(true)&&busy.Read(14)==0,"close never waits for busy producer");
}
foreach(string mode in new[]{"missing","failed","unmatched","truncated","after-close","pending-context"}){
 string p=NewCase("close-"+mode);Guid id=Guid.NewGuid();var records=new List<OrdinaryEvent>{E(1,OrdinaryPhase.Session)};
 if(mode!="missing"){
  if(mode!="unmatched")records.Add(E((ulong)records.Count+1,OrdinaryPhase.Shutdown,OrdinaryEdge.Enter,ulong.MaxValue));
  var close=E((ulong)records.Count+1,OrdinaryPhase.Shutdown,OrdinaryEdge.Return,ulong.MaxValue,mode=="failed"?-1:0);close.Words[17]=1;close.Words[18]=close.Sequence;records.Add(close);
 }
 if(mode=="after-close")records.Add(E((ulong)records.Count+1));
 using(var j=new OrdinaryJournal(p,id)){j.Commit(records,(ulong)records.Count,0,0);}
 if(mode=="truncated"){var h=LatestHead(p);using var f=new FileStream(Bank(p,h),FileMode.Open,FileAccess.Write,FileShare.Read);f.SetLength(OrdinaryJournal.EventOffset+256+2*256+100);f.Flush(true);}
 var r=OrdinaryJournal.Recover(p,id);if(mode=="pending-context")r.State.PendingContexts[7]=[];
 Check(!r.Complete&&r.Terminal.Contains("UNCERTAIN"),"no false COMPLETE for "+mode);RetireFixture(p);
}
// ABI and volatile ownership: delayed/absent persistence never advances Durable.
{
 Guid id=Guid.NewGuid();using var m=new OrdinaryMapping("Local\\test-"+id,id,true);
 Refuses(()=>{using var wrong=new OrdinaryMapping(m.Name,Guid.NewGuid(),false);},"stale mapping session refused");
 long alloc=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
 for(int i=0;i<OrdinaryProtocol.Capacity;i++)Check(m.Emit(OrdinaryPhase.Heartbeat),"preallocated accepted "+i);
 Check(m.Read(8)==8192&&m.Read(10)==0,"produced 8192 durable zero without persistence");
 Probe.Overflow(m,100000);m.Write(16,0);
 long delta=Probe.Overflow(m,100000);Check(m.Read(16)==100000&&(m.Read(12)&1)!=0,"sustained overflow explicit and bounded");Check(delta==0,"overflow producer zero allocations delta="+delta);Check(timer.Elapsed<TimeSpan.FromSeconds(5),"producer does not wait for absent consumer");
 var bytes=new byte[256];Check(m.TryRead(1,bytes)&&OrdinaryEvent.Read(bytes).Sequence==1,"unconsumed prefix not overwritten");
 m.Write(18,1);Check(!m.Emit(OrdinaryPhase.Frame)&&(m.Read(12)&2)!=0,"producer contention refuses without waiting");m.Write(18,0);
 m.Write(9,8192);m.Write(15,1);m.Write(11,1);Check(m.Emit(OrdinaryPhase.Frame)&&(m.Read(12)&32)!=0,"observer heartbeat loss explicit");
 Check(!m.EmitBytes(new byte[1])&&(m.Read(12)&64)!=0,"malformed record size refused");
 m.Write(8,-1);Check(!m.Emit(OrdinaryPhase.Frame)&&(m.Read(12)&16)!=0,"negative shared sequence refuses without addressing ring");
 m.Write(8,8193);m.Write(9,8194);Check(!m.Emit(OrdinaryPhase.Frame),"future consumed watermark refuses before slot overwrite");
 measures.Add(new{caseName="blocked-consumer",milliseconds=timer.Elapsed.TotalMilliseconds,attempts=108192,allocatedTestHarness=GC.GetAllocatedBytesForCurrentThread()-alloc});
}
// Full semantic sequence, checkpoint rollover, resource reuse and pending tombstone.
{
 string p=NewCase("blocked-flush");Guid id=Guid.NewGuid();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
 using var mapping=new OrdinaryMapping("Local\\blocked-"+id,id,true);bool armed=false;
 using(var journal=new OrdinaryJournal(p,id,2,s=>{if(armed&&s=="data-flush"){entered.Set();release.Wait();}})){
  armed=true;var persisting=Task.Run(()=>Commit(journal,E(1)));Check(entered.Wait(5000),"persistence reached held flush");
  var t=Stopwatch.StartNew();for(int i=0;i<1024;i++)mapping.Emit(OrdinaryPhase.Heartbeat);
  Check(mapping.Read(8)==1024&&journal.DurableSequence==0&&t.ElapsedMilliseconds<1000,"renderer progresses while real persistence owner is held before flush");release.Set();persisting.GetAwaiter().GetResult();Check(journal.DurableSequence==1,"ACK follows released storage flush");
 }RetireFixture(p);
}
{
 string p=NewCase("semantics");Guid id=Guid.NewGuid();
 using(var j=new OrdinaryJournal(p,id,2)){
  Commit(j,E(1,OrdinaryPhase.Update,OrdinaryEdge.Enter,1),E(2,OrdinaryPhase.Update,OrdinaryEdge.Return,1));
  var birth=E(3,OrdinaryPhase.Resource,handle:10,birth:1,action:1,kind:2);birth.Words[19]=1048576;
  var memory=E(4,OrdinaryPhase.Resource,handle:20,birth:2,action:1,kind:1);
  var bind=E(5,OrdinaryPhase.Resource,handle:10,birth:1,action:3,kind:2);bind.Words[15]=20;bind.Words[16]=2;
  Commit(j,birth,memory,bind);
  var role=E(6,OrdinaryPhase.Context,handle:10,birth:1,action:38);
  var enter=E(7,OrdinaryPhase.Submit,OrdinaryEdge.Enter,2);enter.Words[8]=1;var returned=E(8,OrdinaryPhase.Submit,OrdinaryEdge.Return,2);returned.Words[8]=1;
  Commit(j,role,enter,returned,E(9,OrdinaryPhase.Resource,handle:10,birth:1,action:2,kind:2));
  Commit(j,E(10,OrdinaryPhase.Resource,handle:10,birth:3,action:1,kind:2),E(11,OrdinaryPhase.Present,OrdinaryEdge.Enter,3),E(12,OrdinaryPhase.Present,OrdinaryEdge.Return,3));j.Rotate();
  var r=OrdinaryJournal.Recover(p,id);Check(r.DurableSequence==12&&r.CheckpointSequence==12,"monotonic checkpoint head");
  Check(r.State.Resources[3].Birth.Words[13]==10&&r.State.SubmittedResources[1].Birth.Words[19]==1048576,"recycled handle retains distinct original allocation");
  Check(r.State.SubmittedResources[1].Binding?.Words[16]==2&&r.State.SubmittedResources[1].Retirement?.Sequence==9,"binding and pending retirement provenance survive rotation");
  Check(r.State.PendingContexts[1][38].Words[14]==1&&r.State.Pending.Count==1,"pending submission role owns old incarnation");
  Check(r.State.LastPresent?.Edge==OrdinaryEdge.Return&&r.Terminal.Contains("not physical display"),"present return never scanout");
  var complete=E(13,OrdinaryPhase.Completed);complete.Words[8]=1;Commit(j,complete,E(14,OrdinaryPhase.Acquire,OrdinaryEdge.Enter,4),E(15,OrdinaryPhase.Acquire,OrdinaryEdge.Return,4,-1000001004));
  Commit(j,E(16,OrdinaryPhase.Draw,OrdinaryEdge.Enter,5),E(17,OrdinaryPhase.Draw,OrdinaryEdge.Exception,5,long.MinValue));
  Check(j.State.Pending.Count==0&&j.State.LastCompletion?.Words[8]==1&&j.State.LastSubmit?.Words[8]==1,"completion tied to submit; out-of-date does not fabricate new submit");
  Check(j.State.Open.Count==0&&j.State.LastPhase[(ulong)OrdinaryPhase.Draw].Edge==OrdinaryEdge.Exception,"exception distinct from successful return");
  Commit(j,E(18,OrdinaryPhase.Draw,OrdinaryEdge.Enter,6));j.Rotate();r=OrdinaryJournal.Recover(p,id);
  Check(r.State.Open.ContainsKey("4:6")&&r.Terminal.Contains("return not retained"),"open durable operation has explicitly uncertain return");
  Check(r.State.Faults==0,"valid semantic trace no protocol faults");
 }
 RetireFixture(p);
}
// Failure cuts: persistence ACK can never precede the completed head flush.
foreach(string stage in new[]{"data-write","data-flush","data-flushed","head-write","head-flush","head-flushed","rotation-start","checkpoint-write","checkpoint-flush","checkpoint-flushed"}){
 string p=NewCase("cut");Guid id=Guid.NewGuid();bool armed=false;ulong previous;
 using(var j=new OrdinaryJournal(p,id,1,s=>{if(armed&&s==stage)throw new IOException("injected "+s);})){
  Commit(j,E(1));previous=j.DurableSequence;armed=true;
  Refuses(()=>{if(stage.StartsWith("data")||stage.StartsWith("head")){armed=false;j.Rotate();armed=true;Commit(j,E(2));}else j.Rotate();},"cut at "+stage);
  Check(j.DurableSequence==previous,"no false RAM ACK at "+stage);
  Refuses(()=>Commit(j,E(2)),"failed owner cannot continue at "+stage);
 }
 var r=OrdinaryJournal.Recover(p,id);Check(r.DurableSequence>=previous&&r.DurableSequence<=2,"recover preceding-or-persisted prefix at "+stage);RetireFixture(p);
}
// Torn/corrupt final record: salvage stronger-than-previous-head prefix.
foreach(string mode in new[]{"record","truncate","meta","head","checkpoint"}){
 string p=NewCase("damage");Guid id=Guid.NewGuid();
 using(var j=new OrdinaryJournal(p,id,4)){Commit(j,E(1));j.Rotate();Commit(j,E(2),E(3),E(4),E(5));}
 byte[] h=LatestHead(p);string bank=Bank(p,h);
 if(mode=="record")Flip(bank,OrdinaryJournal.EventOffset+256+2*256+9);
 if(mode=="truncate"){using var f=new FileStream(bank,FileMode.Open,FileAccess.Write,FileShare.Read);f.SetLength(OrdinaryJournal.EventOffset+256+2*256+100);f.Flush(true);}
 if(mode=="meta")Flip(bank,24);
 if(mode=="head")Flip(Path.Combine(p,$"head{BitConverter.ToUInt64(h,24)%2}.bin"),24);
 if(mode=="checkpoint")Flip(bank,4096+10);
 if(mode is "meta" or "checkpoint")Refuses(()=>OrdinaryJournal.Recover(p,id),"unrecoverable sole checkpoint damage refused "+mode);
 else {var r=OrdinaryJournal.Recover(p,id);Check(r.Corrupt,"corruption exposed "+mode);Check(r.DurableSequence==(mode=="head"?1UL:3UL),"strongest valid prefix "+mode);}
 RetireFixture(p);
}
// Same revision with different valid metadata is an explicit ambiguous-head refusal.
{
 string p=NewCase("fallback-corruption");Guid id=Guid.NewGuid();using(var j=new OrdinaryJournal(p,id)){Commit(j,E(1));j.Rotate();}
 Flip(Bank(p,LatestHead(p)),24);var r=OrdinaryJournal.Recover(p,id);Check(r.DurableSequence==1&&r.Corrupt,"damaged newest incarnation cannot silently fall back cleanly");RetireFixture(p);
}
{
 string p=NewCase("ambiguous");Guid id=Guid.NewGuid();using(var j=new OrdinaryJournal(p,id)){Commit(j,E(1));}
 var h=LatestHead(p);var other=(byte[])h.Clone();BinaryPrimitives.WriteUInt64LittleEndian(other.AsSpan(56),2);SHA256.HashData(other.AsSpan(0,4064)).CopyTo(other,4064);
 File.WriteAllBytes(Path.Combine(p,$"head{1-BitConverter.ToUInt64(h,24)%2}.bin"),other);
 Refuses(()=>OrdinaryJournal.Recover(p,id),"conflicting same-revision head refused");Refuses(()=>OrdinaryJournal.Recover(p,Guid.NewGuid()),"stale disk session refused");RetireFixture(p);
}
// Allocation/capacity and wrong identity adversaries are permanent reducer tests.
{
 var s=new OrdinaryState();s.Apply(E(1,OrdinaryPhase.Resource,handle:1,birth:1,action:1,kind:2));s.Apply(E(2,OrdinaryPhase.Resource,handle:99,birth:1,action:2,kind:2));Check(s.Resources.ContainsKey(1)&&s.Faults!=0,"wrong-handle retirement cannot delete original");
 s=new();for(ulong n=1;n<=8192;n++)s.Resources[n]=new(E(n,OrdinaryPhase.Resource,handle:n,birth:n,action:1),null,null);
 for(ulong n=1;n<8192;n++)s.SubmittedResources[n]=s.Resources[n];s.Pending[99]=E(1,OrdinaryPhase.Submit);
 var b=s.Resources[8192].Birth;var binding=E(1,OrdinaryPhase.Resource,handle:8192,birth:8192,action:3);binding.Words[16]=9000;s.Resources[8192]=new(b,binding,null);s.Resources[9000]=new(E(1,OrdinaryPhase.Resource,handle:9000,birth:9000,action:1),null,null);
 s.Apply(E(1,OrdinaryPhase.Submit,OrdinaryEdge.Enter,1));Check(s.SubmittedResources.Count==8192&&s.Faults!=0,"each retained identity capacity-checked including bound memory");s.Resources.Remove(9000);
 var decoded=OrdinaryCheckpoint.Decode(OrdinaryCheckpoint.Encode(s));Check(decoded.SubmittedResources.Count==8192,"boundary checkpoint reconstructs");
 s.Resources[9000]=new(E(1,OrdinaryPhase.Resource,handle:9000,birth:9000,action:1),null,null);Refuses(()=>OrdinaryCheckpoint.Encode(s),"oversize state refused before ACK");
}
// Pre-GPU startup failures and atomic retention limit.
{
 string p=NewCase("init");Directory.CreateDirectory(p);for(int i=0;i<16;i++)Directory.CreateDirectory(Path.Combine(p,i.ToString()));
 using(var s=new OrdinarySession(p,worker)){s.Finish(true);Check(true,"startup remains independent of retained-session count");}
 var drain=Stopwatch.StartNew();while(Directory.GetDirectories(p).Any(d=>File.Exists(Path.Combine(d,"session.json"))&&!File.Exists(Path.Combine(d,"finalized.json")))&&drain.ElapsedMilliseconds<10000)Thread.Sleep(10);RetireFixture(p);
 p=NewCase("missing-helper");Refuses(()=>{using var s=new OrdinarySession(p,Path.Combine(p,"missing.exe"));},"missing helper refuses startup");RetireFixture(p);
 p=NewCase("existing-file");Directory.CreateDirectory(p);File.WriteAllText(Path.Combine(p,"bank0.bin"),"protected");Refuses(()=>{using var j=new OrdinaryJournal(p,Guid.NewGuid());},"existing session storage never overwritten");Check(File.ReadAllText(Path.Combine(p,"bank0.bin"))=="protected","init refusal preserves source storage");RetireFixture(p);
}
// Complete producer + independent worker, normal drain and real process kill.
{
 string p=NewCase("complete");Guid id;string sessionPath;
 using(var s=new OrdinarySession(p,worker)){
  id=s.Mapping.Session;sessionPath=s.DirectoryPath;var timing=new double[12000];long allocated=GC.GetAllocatedBytesForCurrentThread();var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var wall=Stopwatch.StartNew();
  for(int i=0;i<timing.Length;i++){long t=Stopwatch.GetTimestamp();s.Mapping.Emit(OrdinaryPhase.Update,OrdinaryEdge.Enter,operation:(ulong)i+1);s.Mapping.Emit(OrdinaryPhase.Update,OrdinaryEdge.Return,operation:(ulong)i+1);timing[i]=(Stopwatch.GetTimestamp()-t)*1e6/Stopwatch.Frequency;if(i%10==0)Thread.Sleep(8);}
  allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;Array.Sort(timing);measures.Add(new{caseName="managed-producer-independent-persistence",events=24000,p50us=timing[6000],p95us=timing[11400],p99us=timing[11880],maxus=timing[^1],producerAllocatedBytes=allocated,cpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds,wallMs=wall.Elapsed.TotalMilliseconds,produced=s.Mapping.Read(8),durable=s.Mapping.Read(10),faults=s.Mapping.Read(12)|s.Mapping.Read(13),dropped=s.Mapping.Read(16)});
  Check(s.Mapping.Read(12)==0&&s.Mapping.Read(13)==0,"normal independent worker no faults");s.Finish(true);
 }
 var timeout=Stopwatch.StartNew();while(!File.Exists(Path.Combine(sessionPath,"recovery.json"))&&timeout.ElapsedMilliseconds<10000)Thread.Sleep(10);
 var r=OrdinaryJournal.Recover(sessionPath,id);Check(r.Complete&&r.State.Clean&&r.DurableSequence==24003,"complete independent worker final durable clean close");Check(Directory.GetFiles(sessionPath,"*.bin").Sum(f=>new FileInfo(f).Length)==2*OrdinaryJournal.BankBytes+8192,"fixed disk growth bound");
 measures.Add(JsonDocument.Parse(File.ReadAllText(Path.Combine(sessionPath,"performance.json"))).RootElement.Clone());RetireFixture(p);
}
{
 string p=NewCase("observer-kill");Guid id;string sessionPath;
 using(var s=new OrdinarySession(p,worker)){
  id=s.Mapping.Session;sessionPath=s.DirectoryPath;var timeout=Stopwatch.StartNew();while(s.Mapping.Read(10)==0&&timeout.ElapsedMilliseconds<5000)Thread.Sleep(10);
  using var process=Process.GetProcessById(s.PersistenceProcessId);process.Kill();process.WaitForExit();long durable=s.Mapping.Read(10);s.Mapping.Write(11,Stopwatch.GetTimestamp()-Stopwatch.Frequency*6);
  Check(s.Mapping.Emit(OrdinaryPhase.Heartbeat)&&(s.Mapping.Read(12)&32)!=0,"real persistence owner death exposes observer loss without blocking producer");Check(s.Mapping.Read(10)==durable,"dead observer cannot advance durability");
 }var r=OrdinaryJournal.Recover(sessionPath,id);Check(!r.State.Clean&&r.Terminal.Contains("UNCERTAIN"),"observer death recovery does not fabricate volatile loss marker persistence");RetireFixture(p);
}
{
 string p=NewCase("rotation-cost");Guid id=Guid.NewGuid();var costs=new List<double>();
 using(var j=new OrdinaryJournal(p,id,4)){
  for(int i=0;i<64;i++){var t=Stopwatch.StartNew();var events=Enumerable.Range(0,15).Select(k=>E(j.State.Sequence+(ulong)k+1)).ToArray();Commit(j,events);costs.Add(t.Elapsed.TotalMilliseconds);}
  var r=OrdinaryJournal.Recover(p,id);Check(r.DurableSequence==960&&j.Rotations==15,"repeated rotation monotonic identity and strongest head");costs.Sort();measures.Add(new{caseName="rotation-cost",p50ms=costs[32],p95ms=costs[60],p99ms=costs[63],maxMs=costs[^1],j.Rotations,j.BytesWritten,j.Flushes});
 }RetireFixture(p);
}
{
 string p=NewCase("kill");Directory.CreateDirectory(p);var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add("producer");start.ArgumentList.Add(p);start.ArgumentList.Add(worker);
 using var child=Process.Start(start)!;var t=Stopwatch.StartNew();string ready=Path.Combine(p,"producer-ready.txt");while(!File.Exists(ready)&&!child.HasExited&&t.ElapsedMilliseconds<10000)Thread.Sleep(10);Check(File.Exists(ready),"CPU producer started independent observer");string sessionPath=File.ReadAllText(ready);Thread.Sleep(200);child.Kill();child.WaitForExit();
 t.Restart();while(!File.Exists(Path.Combine(sessionPath,"recovery.json"))&&t.ElapsedMilliseconds<10000)Thread.Sleep(10);var r=OrdinaryJournal.Recover(sessionPath,Guid.ParseExact(Path.GetFileName(sessionPath),"N"));
 Check(r.DurableSequence>0&&!r.State.Clean,"real process kill retained unclean prefix");Check(r.Terminal.Contains("UNCERTAIN"),"process kill never claims exact terminal");RetireFixture(p);
}
File.WriteAllText(Path.Combine(output,"qualification.json"),JsonSerializer.Serialize(new{judgment="PASS",checks=checks.Count,results=checks.Where(s=>!s.StartsWith("preallocated accepted")).ToArray(),measurements=measures},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS {checks.Count} checks; CPU-only.");

static class Probe {
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 public static long Overflow(OrdinaryMapping m,int count){long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<count;i++)m.Emit(OrdinaryPhase.Heartbeat);return GC.GetAllocatedBytesForCurrentThread()-before;}
}
