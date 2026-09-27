using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NovaCore.Diagnostics;

internal static class AbnormalRetentionTests
{
    internal static void Run(RuntimeRetention owner,Func<long,Guid> create,Action<bool,string> check,string worker)
    {
        string root=owner.QualificationPath;var ids=new List<Guid>();
        string Raw(Guid id)=>Path.Combine(root,id.ToString("N"));
        string Capsule(Guid id)=>Path.Combine(root,"capsule-"+id.ToString("N")+".bin");
        string Hash(Guid id)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",Directory.GetFiles(Raw(id)).Order(StringComparer.Ordinal).Select(p=>Path.GetFileName(p)+Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))))))));
        void Attest(Guid id,int code=0){string p=Path.Combine(Raw(id),"termination.json");if(File.Exists(p))File.Delete(p);OrdinaryTermination.Write(Raw(id),id,int.MaxValue,1,true,code,DateTime.UtcNow.Ticks);}
        void ClearCapsules(){foreach(var id in ids)if(File.Exists(Capsule(id)))File.Delete(Capsule(id));}
        try
        {
            check(RuntimeRetention.AbnormalSoftCeilingBytes==340787200,"325 MiB exact ordinary raw contract");
            for(int i=0;i<5;i++){
                var id=create(DateTime.UtcNow.AddDays(-2).AddMinutes(i).Ticks);ids.Add(id);
                RetentionAdversaries.AbnormalJournal(Raw(id),id,64,0);File.Delete(Path.Combine(Raw(id),"termination.json"));
            }
            var before=ids.ToDictionary(id=>id,Hash);var r=owner.Execute();
            check(ids.All(id=>Directory.Exists(Raw(id))&&Hash(id)==before[id])&&r.After.OrdinaryAbnormalBytes==0&&r.After.ExemptRawBytes>RuntimeRetention.AbnormalSoftCeilingBytes,"missing positive criticality preserves full raw and exempts budget");
            check(r.Warning?.Contains("STORAGE PRESSURE",StringComparison.Ordinal)==true,"exempt evidence pressure surfaces without retirement");
            foreach(var id in ids)Attest(id);
            before=ids.ToDictionary(id=>id,Hash);
            var saved=File.ReadAllBytes(Path.Combine(Raw(ids[0]),"termination.json"));
            var stale=JsonNode.Parse(saved)!;stale["head0"]="stale";File.WriteAllText(Path.Combine(Raw(ids[0]),"termination.json"),stale.ToJsonString());
            var badHash=Hash(ids[0]);r=owner.Execute();
            check(!r.Deleted.Contains(ids[0])&&Hash(ids[0])==badHash&&r.Decisions.Single(d=>d.Session==ids[0]).Classification!="ORDINARY","stale attestation grants no routine classification");
            File.WriteAllBytes(Path.Combine(Raw(ids[0]),"termination.json"),saved);
            // Explicit reviewed false-positive incident: keep original exit/fault
            // facts, and require the independent capsule path after reclassification.
            File.Delete(Path.Combine(Raw(ids[0]),"termination.json"));owner.ReviewSession(ids[0],"Synthetic fixture has no exit attestation; independently known noncritical",Encoding.UTF8.GetBytes("Permanent CPU fixture; no GPU; full causal state known; absence of automatic exit evidence retained."),true);owner.Reclassify(ids[0]);
            before=ids.ToDictionary(id=>id,Hash);
            foreach(string stage in new[]{"capsule-written","capsule-flushed","capsule-verified","capsule-recovered","validated","file-staged","before-commit"}){
                r=owner.Execute(at=>{if(at==stage)throw new IOException("injected "+stage);});
                check(r.Deleted.Length==0&&ids.All(id=>Hash(id)==before[id]),stage+" preserves original raw");
                ClearCapsules();
            }
            foreach(var id in ids)Directory.SetLastWriteTimeUtc(Raw(id),DateTime.UtcNow.AddYears(4));
            r=owner.Execute(stage=>{if(stage=="capsule-written")throw new IOException("before first flush");});
            check(r.Deleted.Length==0&&File.Exists(Capsule(ids[0])),"interrupted first capsule write preserves raw");
            bool writeBlocked=false,replaceBlocked=false;
            r=owner.Execute(stage=>{if(stage!="capsule-verified")return;
                try{File.WriteAllText(Capsule(ids[0]),"tampered");}catch(IOException){writeBlocked=true;}
                try{File.Move(Capsule(ids[0]),Capsule(ids[0])+"-moved");}catch(IOException){replaceBlocked=true;}
            });
            check(writeBlocked&&replaceBlocked,"same verified capsule handle excludes mutation/replacement through transaction");
            check(r.Deleted.SequenceEqual(new[]{ids[0]})&&File.Exists(Capsule(ids[0])),"oldest ordinary raw retires only after durable normal-parser capsule validation");
            check(ids.Skip(1).All(id=>Directory.Exists(Raw(id)))&&r.After.OrdinaryAbnormalBytes<=RuntimeRetention.AbnormalSoftCeilingBytes,"newest three and useful ordinary survivor remain");
            var recovered=owner.ReadCapsule(ids[0]);
            check(!recovered.Complete&&recovered.State.Faults==64&&recovered.DurableSequence==3&&recovered.Terminal.Contains("UNCERTAIN"),"capsule independently reconstructs actual abnormal uncertainty");
            byte[] original=File.ReadAllBytes(Capsule(ids[0]));
            check(original.Length<RuntimeRetention.MaximumCapsuleBytes&&original.Length<OrdinaryJournal.BankBytes,"capsule is compact without dropping raw evidence");
            void Refuse(byte[] changed,string reason){
                File.WriteAllBytes(Capsule(ids[0]),changed);bool refused=false;try{owner.ReadCapsule(ids[0]);}catch(Exception e) when(e is IOException or InvalidDataException or ArgumentException){refused=true;}
                check(refused,reason);File.WriteAllBytes(Capsule(ids[0]),original);
            }
            var corrupt=original.ToArray();corrupt[20]^=1;Refuse(corrupt,"capsule checksum corruption refuses");
            byte[] Alter(Action<ZipArchive> action){
                using var m=new MemoryStream();m.Write(original,0,original.Length-32);m.Position=0;
                using(var zip=new ZipArchive(m,ZipArchiveMode.Update,true))action(zip);
                byte[] data=m.ToArray();m.Write(SHA256.HashData(data));return m.ToArray();
            }
            Refuse(Alter(z=>{using var f=z.CreateEntry("../escape").Open();f.WriteByte(1);}),"capsule traversal entry refuses");
            Refuse(Alter(z=>{using var f=z.CreateEntry("HEAD0.BIN").Open();f.WriteByte(1);}),"capsule case-colliding entry refuses");
            Refuse(Alter(z=>z.GetEntry("head1.bin")!.Delete()),"capsule missing head refuses");
            Refuse(Alter(z=>{var e=z.GetEntry("recovery.json")!;e.Delete();using var f=z.CreateEntry("recovery.json").Open();f.Write(Encoding.UTF8.GetBytes("{}"));}),"capsule altered raw hash refuses");
            check(!Directory.GetDirectories(root,".capsule-check-*").Any(),"normal-parser staging leaves no raw duplicate");
            var large=create(DateTime.UtcNow.AddDays(-4).Ticks);ids.Add(large);RetentionAdversaries.AbnormalJournal(Raw(large),large,64,0);Attest(large);
            using(var bank=new FileStream(Path.Combine(Raw(large),"bank1.bin"),FileMode.Open,FileAccess.Write)){
                bank.Position=OrdinaryJournal.PageBytes;bank.Write(RandomNumberGenerator.GetBytes(RuntimeRetention.MaximumCapsuleBytes+1024*1024));
            }
            var largeHash=Hash(large);r=owner.Execute();
            check(Directory.Exists(Raw(large))&&Hash(large)==largeHash&&!File.Exists(Capsule(large))&&r.Failures.Any(f=>f.Contains("compact bound",StringComparison.Ordinal)),"incompressible capsule hits streaming bound; raw preserved without truncation");
            // Pinned/critical/external-failure evidence cannot be reclassified by budget.
            foreach(string cause in new[]{"exit-crash","no-exit","missing-termination","gpu-error","overflow","pin"}){
                var id=create(DateTime.UtcNow.AddDays(-3).Ticks);ids.Add(id);
                if(cause=="overflow"){RetentionAdversaries.AbnormalJournal(Raw(id),id,1,7);Attest(id);}
                else if(cause=="pin")owner.PinSession(id);
                else if(cause=="missing-termination")File.Delete(Path.Combine(Raw(id),"termination.json"));
                else if(cause=="exit-crash")Attest(id,27);
                else if(cause=="no-exit"){var p=Path.Combine(Raw(id),"termination.json");var j=JsonNode.Parse(File.ReadAllBytes(p))!;j["exited"]=false;File.WriteAllText(p,j.ToJsonString());}
                else {
                    using var mapping=new OrdinaryMapping("Local\\CriticalTest-"+id,id,true);mapping.Emit(OrdinaryPhase.Session);mapping.Emit(OrdinaryPhase.Error,result:-4);mapping.Close(true);
                    foreach(string name in new[]{"bank0.bin","bank1.bin","head0.bin","head1.bin","finalized.json"})File.Delete(Path.Combine(Raw(id),name));
                    var events=new List<OrdinaryEvent>();byte[] b=new byte[256];for(int n=1;n<=4;n++){check(mapping.TryRead(n,b),"critical fixture event");events.Add(OrdinaryEvent.Read(b));}
                    using(var journal=new OrdinaryJournal(Raw(id),id))journal.Commit(events,4,0,0);
                    var rr=OrdinaryJournal.Recover(Raw(id),id);check(rr.Complete,"GPU-error witness otherwise satisfies legacy COMPLETE");
                    OrdinaryObserver.WriteReport(Raw(id),rr);OrdinaryOwnership.Finalize(Raw(id),id,rr);Attest(id);
                }
                var hash=Hash(id);r=owner.Execute();
                check(Directory.Exists(Raw(id))&&Hash(id)==hash&&!r.Decisions.Single(d=>d.Session==id).Eligible,cause+" critical/unresolved raw survives including clean retirement path");
            }
            var capsuleHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Capsule(ids[0]))));owner.Execute();
            check(capsuleHash==Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Capsule(ids[0])))),"raw cleanup never deletes compact capsule");
            foreach(string mode in new[]{"ordinary","crash-after-close","gpu-error"}){
                var info=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
                foreach(var value in new[]{"exit-attestation",Path.GetFileName(root),worker,mode})info.ArgumentList.Add(value);
                using var child=System.Diagnostics.Process.Start(info)!;check(child.WaitForExit(10000),"real producer exits: "+mode);
                var id=Guid.Parse(File.ReadAllText(Path.Combine(root,"exit-ready")));ids.Add(id);File.Delete(Path.Combine(root,"exit-ready"));
                using var metadata=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Raw(id),"observer.json")));
                try{using var observer=System.Diagnostics.Process.GetProcessById(metadata.RootElement.GetProperty("pid").GetInt32());check(observer.WaitForExit(10000),"real observer drained: "+mode);}catch(ArgumentException){}
                using var termination=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Raw(id),"termination.json")));
                check(termination.RootElement.GetProperty("exited").GetBoolean()&&termination.RootElement.GetProperty("exitCode").GetInt32()==child.ExitCode,"exact producer exit attested: "+mode);
                var decision=owner.Execute().Decisions.Single(d=>d.Session==id);
                check((decision.Classification=="ORDINARY")== (mode=="ordinary"),"positive ordinary versus critical actual lifecycle: "+mode);
            }
        }
        finally{
            foreach(var id in ids){if(Directory.Exists(Raw(id)))RetentionAdversaries.DeleteFixtureTree(root,Raw(id));if(File.Exists(Capsule(id)))File.Delete(Capsule(id));foreach(string prefix in new[]{"review-","classification-"}){var p=Path.Combine(root,prefix+id.ToString("N")+".json");if(File.Exists(p))File.Delete(p);}}
        }
    }
}

