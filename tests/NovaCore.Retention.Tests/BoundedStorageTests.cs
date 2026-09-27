using NovaCore.Diagnostics;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class BoundedStorageTests
{
    internal static void Run(Action<bool,string> check,int cleanRuns=32,bool soakOnly=false)
    {
        var owner=RuntimeRetention.ForQualification(Guid.NewGuid());string root=owner.QualificationPath;Directory.CreateDirectory(root);
        string Raw(Guid id)=>Path.Combine(root,id.ToString("N"));
        string Hash(Guid id)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",Directory.GetFiles(Raw(id)).Order(StringComparer.Ordinal).Select(p=>Path.GetFileName(p)+Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))))))));
        Guid Create(int minutes,bool pin=true,bool fault=false,bool intentional=false)
        {
            Guid id=Guid.NewGuid();string path=Raw(id);Directory.CreateDirectory(path);long q=Stopwatch.GetTimestamp();
            OrdinaryOwnership.Write(Path.Combine(path,"session.json"),new{schema=2,session=id,producer=int.MaxValue,startUtcTicks=1,openedUtcTicks=DateTime.UtcNow.AddMinutes(minutes).Ticks,openedQpc=q,qpcFrequency=Stopwatch.Frequency});
            OrdinaryOwnership.Write(Path.Combine(path,"observer.json"),new{schema=1,session=id,pid=int.MaxValue,startUtcTicks=1});using(OrdinaryOwnership.Create(Path.Combine(path,"ownership.lock"))){}
            using var mapping=new OrdinaryMapping("Local\\BoundedTest-"+id,id,true);mapping.Emit(OrdinaryPhase.Session);mapping.Close(!intentional);
            var events=new List<OrdinaryEvent>();var b=new byte[256];for(int i=1;i<=3;i++){if(!mapping.TryRead(i,b))throw new Exception("fixture");events.Add(OrdinaryEvent.Read(b));}
            using(var journal=new OrdinaryJournal(path,id))journal.Commit(events,3,fault?1:0,fault?7:0);
            OrdinaryStorage.Json(Path.Combine(path,"performance.json"),new{fixture=true});var r=OrdinaryJournal.Recover(path,id);OrdinaryObserver.WriteReport(path,r);OrdinaryOwnership.Finalize(path,id,r);
            OrdinaryTermination.Write(path,id,int.MaxValue,1,true,0,DateTime.UtcNow.Ticks);if(pin)owner.PinSession(id);return id;
        }
        try{
            check(RuntimeRetention.RuntimeCapBytes==536870912&&RuntimeRetention.PreventiveBytes==419430400,"exact hard cap and preventive threshold");
            check(OrdinaryStorage.MaximumSessionBytes==87138304,"derived complete session includes every bounded metadata file");
            check(!root.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaCore","MinimumRecorder"),StringComparison.OrdinalIgnoreCase),"over-cap adversarial fixtures isolated from real runtime accounting");
            string padding=Path.Combine(root,"padding.bin");long threshold=RuntimeRetention.RuntimeCapBytes-RuntimeRetention.NewSessionReservationBytes-RuntimeRetention.ControlPublicationBytes;
            foreach(long size in new[]{threshold-1,threshold,threshold+1,RuntimeRetention.RuntimeCapBytes-1,RuntimeRetention.RuntimeCapBytes,RuntimeRetention.RuntimeCapBytes+1}){
                using(var f=new FileStream(padding,FileMode.Create,FileAccess.Write))f.SetLength(size);
                var a=owner.InspectAdmission();check(a.TotalBytes==size&&a.Available==(size<=threshold),"all-byte admission boundary "+size);
            }
            var before=new FileInfo(padding).Length;int configure=0,launched=0,warnings=0;
            RecorderLaunchPolicy.Start(()=>{using var lease=owner.AcquireAdmission();return new object();},_=>configure++,false,()=>launched++,_=>warnings++);
            check(launched==1&&configure==0&&warnings==1&&new FileInfo(padding).Length==before&&Directory.GetDirectories(root).Length==0,"cap exhaustion launches ordinary player action with no recorder configuration or writes");
            bool refused=false;try{RecorderLaunchPolicy.Start(()=>{using var lease=owner.AcquireAdmission();return new object();},_=>configure++,true,()=>launched++,_=>warnings++);}catch(RecorderStorageExhaustedException){refused=true;}
            check(refused&&launched==1&&configure==0,"recorder-required qualification refuses before launch without capacity");File.Delete(padding);
            check(RecorderLaunchPolicy.RequiresCoverage(["--qualify-editor","route"])&&RecorderLaunchPolicy.RequiresCoverage(["--qualification-tank","short"])&&!RecorderLaunchPolicy.RequiresCoverage([]),"production qualification route recognition");
            using(var entered=new ManualResetEventSlim())using(var release=new ManualResetEventSlim()){
                var busy=Task.Run(()=>{using var lease=owner.AcquireAdmission();entered.Set();release.Wait();});entered.Wait();
                var second=Task.Run(()=>{RecorderLaunchPolicy.Start(()=>{using var lease=owner.AcquireAdmission();return new object();},_=>configure++,false,()=>launched++,_=>warnings++);});
                Thread.Sleep(100);check(!second.IsCompleted,"concurrent startup awaits maintenance instead of launching unrecorded");
                release.Set();Task.WaitAll(busy,second);check(configure==1&&launched==2&&warnings==2,"maintenance contention resolves into recorded startup without another warning");
            }
            var active=Create(-1);var session=Path.Combine(Raw(active),"session.json");var data=JsonNode.Parse(File.ReadAllBytes(session))!;using var self=Process.GetCurrentProcess();
            data["reservedBytes"]=OrdinaryStorage.MaximumSessionBytes;data["producer"]=self.Id;data["startUtcTicks"]=self.StartTime.ToUniversalTime().Ticks;File.WriteAllText(session,data.ToJsonString());
            var reserved=owner.InspectAdmission();long bytes=Directory.GetFiles(Raw(active)).Sum(p=>new FileInfo(p).Length);
            check(reserved.OutstandingBytes==OrdinaryStorage.MaximumSessionBytes-bytes,"active future metadata growth remains reserved without double counting raw");
            data["producer"]=int.MaxValue;data["startUtcTicks"]=1;File.WriteAllText(session,data.ToJsonString());var observer=Path.Combine(Raw(active),"observer.json");
            check(owner.InspectAdmission().OutstandingBytes>0,"finalization or process exit never releases retained raw reservation");
            File.WriteAllText(observer,JsonSerializer.Serialize(new{schema=1,session=active,pid=self.Id,startUtcTicks=self.StartTime.ToUniversalTime().Ticks}));
            check(owner.InspectAdmission().OutstandingBytes>0,"producer exit does not release active persistence reservation");RetentionAdversaries.DeleteFixtureTree(root,Raw(active));

            var ids=new List<Guid>();for(int i=0;i<7;i++)ids.Add(Create(-70+i,true,i==0));
            // Preserve a contradictory original summary; the capsule must retain
            // it alongside independently reconstructed real durable authority.
            File.WriteAllText(Path.Combine(Raw(ids[0]),"recovery.json"),"{\"contradictoryHistoricalReport\":true}");
            var hashes=ids.ToDictionary(id=>id,Hash);
            var wrong=owner.RollingExecute(requested:ids[^1]);check(wrong.Deleted.Length==0&&ids.All(id=>Hash(id)==hashes[id]),"selected newest RETIRE never substitutes unrelated sessions");
            foreach(string cut in new[]{"capsule-memory-validated","capsule-written","capsule-flushed","capsule-recovered","validated","file-staged","before-commit"}){
                bool reached=false;var report=owner.RollingExecute(s=>{if(s==cut){reached=true;throw new IOException("injected "+cut);}});
                check(reached&&report.Deleted.Length==0&&ids.All(id=>Hash(id)==hashes[id])&&Directory.GetFiles(root,"capsule-*").Length==0,cut+" rolls back whole raw and uncommitted capsule");
            }
            var info=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};info.ArgumentList.Add("kill-rolling");info.ArgumentList.Add(Path.GetFileName(root));
            using(var child=Process.Start(info)!){var timer=Stopwatch.StartNew();while(!File.Exists(Path.Combine(root,"kill-ready"))&&!child.HasExited&&timer.ElapsedMilliseconds<30000)Thread.Sleep(20);
                check(File.Exists(Path.Combine(root,"kill-ready")),"critical capsule process-kill cut reached");child.Kill();child.WaitForExit();}
            File.Delete(Path.Combine(root,"kill-ready"));check(ids.All(id=>Hash(id)==hashes[id])&&Directory.GetFiles(root,"capsule-*").Length==0,"killed critical replacement retains complete original directory");
            var done=owner.RollingExecute();Console.WriteLine(JsonSerializer.Serialize(done));check(done.Deleted.Length==3&&done.After.TotalBytes<=RuntimeRetention.PreventiveBytes&&done.Failures.Length==0,"inherited over-cap store reduces through three atomic complete-session replacements");
            check(ids.Skip(3).All(id=>Hash(id)==hashes[id])&&owner.InspectAdmission().Available,"newest useful raw preserved and next complete session fits");
            var recovery=owner.ReadCapsule(ids[0]);check(recovery.State.Dropped==7&&recovery.State.Faults==1&&!recovery.Complete,"strong critical capsule retains loss and uncertainty despite contradictory historical report");
            string capsule=Path.Combine(root,"capsule-"+ids[0].ToString("N")+".bin");byte[] original=File.ReadAllBytes(capsule),bad=original.ToArray();bad[20]^=1;
            bool corrupt=false;try{RuntimeRetention.ValidateStrongCapsule(ids[0],bad);}catch(InvalidDataException){corrupt=true;}check(corrupt,"strong capsule corruption never validates");
            using(var buffer=new MemoryStream()){
                using(var zip=new System.IO.Compression.ZipArchive(buffer,System.IO.Compression.ZipArchiveMode.Create,true)){using var stream=zip.CreateEntry("evidence.json").Open();stream.Write(new byte[OrdinaryStorage.RecoveryMetadataBytes+65537]);}
                byte[] payload=buffer.ToArray();buffer.Write(SHA256.HashData(payload));File.WriteAllBytes(capsule,buffer.ToArray());bool bomb=false;try{owner.ReadCapsule(ids[0]);}catch(InvalidDataException){bomb=true;}
                check(bomb,"schema dispatch refuses checksummed oversized decompressed metadata before parse");File.WriteAllBytes(capsule,original);
            }
            check(!Directory.GetDirectories(root,".capsule-check-*").Any(),"normal-parser capsule recovery writes zero expanded files");
            foreach(var id in ids.Skip(3))RetentionAdversaries.DeleteFixtureTree(root,Raw(id));
            foreach(string p in Directory.GetFiles(root,"capsule-*"))File.Delete(p);

            var protectedIds=new List<Guid>();for(int i=0;i<6;i++){var id=Create(-20+i);protectedIds.Add(id);OrdinaryStorage.Json(Path.Combine(Raw(id),"raw-preserve.json"),new{session=id,reason="Unique physical raw recovery needed"});}
            var keep=protectedIds.ToDictionary(id=>id,Hash);done=owner.RollingExecute();
            check(done.Deleted.Length==0&&protectedIds.All(id=>Hash(id)==keep[id])&&!owner.InspectAdmission().Available,"unique RAW-PRESERVE never sacrificed; recording denied without root growth");
            foreach(var id in protectedIds)RetentionAdversaries.DeleteFixtureTree(root,Raw(id));

            var transient=new List<Guid>();for(int i=0;i<6;i++)transient.Add(Create(-20+i));var a0=owner.InspectAdmission();
            using(var f=new FileStream(padding,FileMode.Create,FileAccess.Write))f.SetLength(RuntimeRetention.RuntimeCapBytes-a0.TotalBytes-a0.OutstandingBytes);
            var t0=owner.InspectAdmission();done=owner.RollingExecute();check(done.Deleted.Length==0&&done.Failures.Any(x=>x.Contains("transient"))&&owner.InspectAdmission().TotalBytes==t0.TotalBytes,"capsule physical transient cannot overrun an in-cap root");File.Delete(padding);
            // Unknown unique files and incompressible raw cannot be silently lost.
            File.WriteAllText(Path.Combine(Raw(transient[0]),"unique.unknown"),"keep");
            using(var f=new FileStream(Path.Combine(Raw(transient[1]),"bank1.bin"),FileMode.Open,FileAccess.Write)){f.Position=OrdinaryJournal.PageBytes;f.Write(RandomNumberGenerator.GetBytes(RuntimeRetention.MaximumCapsuleBytes+1024*1024));}
            var unknownHash=Hash(transient[0]);var largeHash=Hash(transient[1]);done=owner.RollingExecute();
            check(Hash(transient[0])==unknownHash&&Hash(transient[1])==largeHash&&!File.Exists(Path.Combine(root,"capsule-"+transient[1].ToString("N")+".bin")),"unknown and incompressible unique raw survive bounded maintenance");
            foreach(var id in transient)if(Directory.Exists(Raw(id)))RetentionAdversaries.DeleteFixtureTree(root,Raw(id));foreach(string p in Directory.GetFiles(root,"capsule-*"))File.Delete(p);

            // Real complete-session owners/transactions, no GPU or runtime root.
            // Ordinary development repeatedly admits, records and closes; clean
            // retirement leaves neither raw accumulation nor per-run capsules.
            int starts=0,configured=0,unexpectedWarnings=0;long peak=0;var watch=Stopwatch.StartNew();
            for(int run=0;run<cleanRuns;run++){
                RecorderLaunchPolicy.Start(()=>{using var lease=owner.AcquireAdmission();Create(-20000+run,false);return new object();},_=>configured++,false,()=>starts++,_=>unexpectedWarnings++);
                var capacity=owner.InspectAdmission();peak=Math.Max(peak,capacity.TotalBytes+capacity.OutstandingBytes);
                if(peak>RuntimeRetention.RuntimeCapBytes)throw new Exception("Repeated normal startup exceeded cap");
            }
            done=owner.RollingExecute();check(starts==cleanRuns&&configured==cleanRuns&&unexpectedWarnings==0&&done.Failures.Length==0&&owner.InspectAdmission().Available,"expected automatic maintenance/reservation/recording path for "+cleanRuns+" consecutive clean runs");
            check(Directory.GetDirectories(root).Length<=4&&Directory.GetFiles(root,"capsule-*").Length==0&&peak<=RuntimeRetention.RuntimeCapBytes,"normal repeated runs bound complete raw directories and create no accumulating capsules");
            Console.WriteLine(JsonSerializer.Serialize(new{normalRollingRuns=cleanRuns,recordedStarts=configured,unrecordedStarts=unexpectedWarnings,peakCommittedBytes=peak,retainedRaw=Directory.GetDirectories(root).Length,seconds=watch.Elapsed.TotalSeconds}));
            if(soakOnly)return; // The complete suites cover later independent index adversaries.
            foreach(string path in Directory.GetDirectories(root))RetentionAdversaries.DeleteFixtureTree(root,path);

            // Exhaustion must first consume eligible ordinary retention, even
            // when those sessions would normally be kept as the newest useful.
            for(int i=0;i<3;i++)Create(-30+i,false);
            using(var f=new FileStream(padding,FileMode.Create,FileAccess.Write))f.SetLength(200L*1024*1024);
            done=owner.RollingExecute();check(done.Deleted.Length==1&&owner.InspectAdmission().Available,"ordinary newest preference cannot force exceptional fallback while safe clean retirement remains");
            File.Delete(padding);foreach(string path in Directory.GetDirectories(root))RetentionAdversaries.DeleteFixtureTree(root,path);

            foreach(bool pinned in new[]{true,false}){
                var older=Enumerable.Range(0,4).Select(i=>Create(-50+i,pinned,false,!pinned)).ToArray();var cleanLast=new[]{Create(-10,false),Create(-9,false)};
                var protectedHashes=older.ToDictionary(id=>id,Hash);done=owner.RollingExecute();
                check(done.Deleted.Order().SequenceEqual(cleanLast.Order())&&older.All(id=>Hash(id)==protectedHashes[id])&&Directory.GetFiles(root,"capsule-*").Length==0,"clean retirement precedes older "+(pinned?"critical":"ordinary-abnormal")+" compaction even when clean sessions are newest");
                foreach(string path in Directory.GetDirectories(root))RetentionAdversaries.DeleteFixtureTree(root,path);
            }

            string Capsule(Guid id)=>Path.Combine(root,"capsule-"+id.ToString("N")+".bin");
            Guid CreateCapsule(int minute,bool pin=false,bool fault=false){var id=Create(minute,pin,fault,true);var raw=Directory.GetFiles(Raw(id)).ToDictionary(p=>Path.GetFileName(p),File.ReadAllBytes);
                using var s=JsonDocument.Parse(raw["session.json"]);byte[] bundle=RuntimeRetention.StrongCapsule(id,s.RootElement.GetProperty("openedUtcTicks").GetInt64(),raw,OrdinaryJournal.Recover(Raw(id),id));
                File.WriteAllBytes(Capsule(id),bundle);RetentionAdversaries.DeleteFixtureTree(root,Raw(id));return id;}
            var pinnedCapsule=CreateCapsule(-101,true);var faultCapsule=CreateCapsule(-100,false,true);var ordinaryCapsules=new List<Guid>();
            ordinaryCapsules.Add(CreateCapsule(-99));
            var capsuleHashes=Directory.GetFiles(root,"capsule-*").ToDictionary(p=>p,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));string indexPath=Path.Combine(root,"ordinary-forensic-index.bin");
            foreach(string cut in new[]{"index-written","index-flushed","index-recovered","index-before-commit"}){
                bool reached=false;var failures=new List<string>();owner.ConsolidateOrdinaryCapsule(s=>{if(s==cut){reached=true;throw new IOException("injected "+cut);}},failures);
                check(reached&&!File.Exists(indexPath)&&capsuleHashes.All(p=>File.Exists(p.Key)&&Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p.Key)))==p.Value),cut+" rolls back index and complete capsules");
            }
            var killInfo=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};killInfo.ArgumentList.Add("kill-index");killInfo.ArgumentList.Add(Path.GetFileName(root));
            using(var child=Process.Start(killInfo)!){var timer=Stopwatch.StartNew();while(!File.Exists(Path.Combine(root,"kill-ready"))&&!child.HasExited&&timer.ElapsedMilliseconds<30000)Thread.Sleep(20);
                check(File.Exists(Path.Combine(root,"kill-ready")),"index process-kill cut reached");child.Kill();child.WaitForExit();}
            File.Delete(Path.Combine(root,"kill-ready"));check(!File.Exists(indexPath)&&capsuleHashes.All(p=>File.Exists(p.Key)&&Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p.Key)))==p.Value),"killed index consolidation restores complete original capsules");
            long capsuleTotal=Directory.GetFiles(root).Sum(p=>new FileInfo(p).Length);using(var f=new FileStream(padding,FileMode.Create,FileAccess.Write))f.SetLength(RuntimeRetention.RuntimeCapBytes-capsuleTotal);
            var transientErrors=new List<string>();check(!owner.ConsolidateOrdinaryCapsule(null,transientErrors)&&transientErrors.Any(e=>e.Contains("transient"))&&!File.Exists(indexPath)&&capsuleHashes.All(p=>File.Exists(p.Key)),"index publication includes all bytes and refuses transient cap overrun");File.Delete(padding);
            for(int i=1;i<70;i++)ordinaryCapsules.Add(CreateCapsule(-99+i));
            var errors=new List<string>();for(int i=0;i<70;i++)if(!owner.ConsolidateOrdinaryCapsule(null,errors))throw new Exception("Expected routine index consolidation: "+string.Join(";",errors));
            var index=owner.ReadIndex();check(index.Records.Length==64&&index.RolledSessions==6&&index.Records.Select(r=>r.Session).SequenceEqual(ordinaryCapsules.Skip(6))&&errors.Count==0,"bounded index retains newest64 exact routine summaries and receipt for six aged-out routine summaries");
            bool Reject(RuntimeRetention.IndexRow row){try{RuntimeRetention.IndexBytes(index with{Records=[row]});return false;}catch(InvalidDataException){return true;}}
            var witness=index.Records[0];var altered=witness.TerminalEvents.ToArray();var last=OrdinaryEvent.Read(altered.AsSpan(altered.Length-256,256));last.Words[20]^=1;last.Encode().CopyTo(altered,altered.Length-256);
            check(Reject(witness with{TerminalEvents=altered}),"valid-checksummed terminal substitution refuses checkpoint disagreement");
            JsonElement Element(JsonNode node){using var j=JsonDocument.Parse(node.ToJsonString());return j.RootElement.Clone();}
            var tampered=JsonNode.Parse(witness.Evidence.GetRawText())!;tampered["files"]!["preserve.pin"]=new JsonObject{["bytes"]=0,["sha256"]=new string('0',64)};
            check(Reject(witness with{Evidence=Element(tampered)}),"index pin inventory cannot be hidden by false pin summary");
            var modifiedAuthority=witness.Authority.ToDictionary(p=>p.Key,p=>p.Value.ToArray());modifiedAuthority["recovery.json"]=System.Text.Encoding.UTF8.GetBytes("{}");
            var term=JsonNode.Parse(modifiedAuthority["termination.json"])!;term["recoverySha256"]=Convert.ToHexString(SHA256.HashData(modifiedAuthority["recovery.json"]));modifiedAuthority["termination.json"]=System.Text.Encoding.UTF8.GetBytes(term.ToJsonString());
            tampered=JsonNode.Parse(witness.Evidence.GetRawText())!;foreach(string name in new[]{"termination.json","recovery.json"}){tampered["files"]![name]!["bytes"]=modifiedAuthority[name].Length;tampered["files"]![name]!["sha256"]=Convert.ToHexString(SHA256.HashData(modifiedAuthority[name]));}
            check(Reject(witness with{Evidence=Element(tampered),Authority=modifiedAuthority}),"attested but contradictory original report never enters ordinary rolling index");
            check(new FileInfo(indexPath).Length<=RuntimeRetention.IndexMaximumBytes&&Directory.GetFiles(root,"capsule-*").Length==2&&new[]{pinnedCapsule,faultCapsule}.All(id=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Capsule(id))))==capsuleHashes[Capsule(id)]),"pin and fault-bearing evidence remain lossless outside bounded routine index");
            byte[] indexBytes=File.ReadAllBytes(indexPath);indexBytes[10]^=1;bool invalidIndex=false;try{RuntimeRetention.ValidateIndex(indexBytes);}catch(InvalidDataException){invalidIndex=true;}check(invalidIndex,"corrupt rolling index cannot validate");
            int withIndex=0,indexWarnings=0;for(int run=0;run<24;run++)RecorderLaunchPolicy.Start(()=>{using var lease=owner.AcquireAdmission();Create(-200+run,false);return new object();},_=>withIndex++,false,()=>{},_=>indexWarnings++);
            check(withIndex==24&&indexWarnings==0&&Directory.GetFiles(root,"capsule-*").Length==2&&owner.ReadIndex().RolledSessions==6,"healthy recorded startups continue with fixed historical critical capsules/index and no new capsule accumulation");
            foreach(string path in Directory.GetDirectories(root))RetentionAdversaries.DeleteFixtureTree(root,path);foreach(string p in Directory.GetFiles(root,"capsule-*"))File.Delete(p);File.Delete(indexPath);

            var state=new OrdinaryState();ulong sequence=1;
            OrdinaryEvent Event(){var e=new OrdinaryEvent{Words=Enumerable.Repeat(ulong.MaxValue,30).ToArray()};e.Words[0]=sequence++;return e;}
            for(int i=0;i<1024;i++)state.Open.Add("18446744073709551615:"+i,Event());
            for(ulong i=0;i<64;i++)state.Pending.Add(ulong.MaxValue-i,Event());for(ulong i=0;i<21;i++)state.LastPhase.Add(ulong.MaxValue-i,Event());
            state.LastSubmit=Event();state.LastCompletion=Event();state.LastPresent=Event();state.LastPublication=Event();
            var maximum=new OrdinaryRecovery(Guid.NewGuid(),ulong.MaxValue,ulong.MaxValue,ulong.MaxValue,ulong.MaxValue,state,true,new string('X',1024));
            byte[] serialized=OrdinaryObserver.ReportBytes(maximum);check(serialized.Length<=OrdinaryStorage.RecoveryMetadataBytes,"maximum 1113-event recovery JSON fits enforced bound: "+serialized.Length);
            foreach(string name in new[]{"session.json","observer.json","performance.json","finalized.json","termination.json","observer-failure.txt","preserve.pin","raw-preserve.json","recovery.json"}){
                string p=Path.Combine(root,name);bool bounded=false;try{OrdinaryStorage.Write(p,new byte[OrdinaryStorage.Maximum(name)+1]);}catch(InvalidDataException){bounded=true;}
                check(bounded&&!File.Exists(p),"oversize "+name+" refuses before file creation");
            }
        }finally{RetentionAdversaries.DeleteFixtureTree(root,root);}
    }
}
