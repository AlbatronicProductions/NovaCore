using NovaCore.Diagnostics;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class RetentionAdversaries
{
    internal static void DeleteFixtureTree(string root,string path)
    {
        if(!Path.GetFullPath(path).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&path!=root)throw new Exception("Test cleanup scope");
        foreach(var p in Directory.GetFileSystemEntries(path)){
            var a=File.GetAttributes(p);if((a&FileAttributes.Directory)!=0){if((a&FileAttributes.ReparsePoint)!=0)Directory.Delete(p);else DeleteFixtureTree(root,p);}else File.Delete(p);
        }Directory.Delete(path);
    }
    static Dictionary<string,string> Hashes(string path)=>Directory.GetFiles(path,"*",SearchOption.AllDirectories).ToDictionary(p=>Path.GetRelativePath(path,p),p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
    static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var v)&&v==p.Value);
    static void Change(string path,string file,string key,JsonNode? value){var j=JsonNode.Parse(File.ReadAllText(Path.Combine(path,file)))!;j[key]=value;File.WriteAllText(Path.Combine(path,file),j.ToJsonString());}
    static void FinalizeAgain(string path,Guid id){File.Delete(Path.Combine(path,"finalized.json"));OrdinaryOwnership.Finalize(path,id,OrdinaryJournal.Recover(path,id));}
    internal static void AbnormalJournal(string path,Guid id,long faults,long drops,bool omitClose=false){
        byte[] bytes=new byte[768];using(var source=new FileStream(Path.Combine(path,"bank0.bin"),FileMode.Open,FileAccess.Read)){source.Position=OrdinaryJournal.EventOffset+256;source.ReadExactly(bytes);}
        var records=Enumerable.Range(0,omitClose?2:3).Select(i=>OrdinaryEvent.Read(bytes.AsSpan(i*256,256))).ToArray();
        foreach(string name in new[]{"bank0.bin","bank1.bin","head0.bin","head1.bin","finalized.json"})File.Delete(Path.Combine(path,name));
        using(var journal=new OrdinaryJournal(path,id)){journal.Commit(records,(ulong)records.Length,faults,drops);}
        OrdinaryObserver.WriteReport(path,OrdinaryJournal.Recover(path,id));
    }
    static Process Child(params string[] args){var info=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};foreach(var a in args)info.ArgumentList.Add(a);return Process.Start(info)!;}
    static void Await(string file,Process child){var t=Stopwatch.StartNew();while(!File.Exists(file)&&!child.HasExited&&t.ElapsedMilliseconds<15000)Thread.Sleep(10);if(!File.Exists(file))throw new Exception("CPU child did not establish fixture");}
    static void Junction(string path,string target){var code="New-Item -ItemType Junction -Path '"+path.Replace("'","''")+"' -Target '"+target.Replace("'","''")+"' | Out-Null";var i=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true};i.ArgumentList.Add("-NoProfile");i.ArgumentList.Add("-EncodedCommand");i.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(code)));using var p=Process.Start(i)!;p.WaitForExit();if(p.ExitCode!=0)throw new Exception("Junction fixture setup failed");}
    internal static void Run(RuntimeRetention owner,Func<long,Guid> create,Action<bool,string> check,string worker)
    {
        string root=owner.QualificationPath;Guid qualification=Guid.ParseExact(Path.GetFileName(root),"N");
        Guid Older()=>create(DateTime.UtcNow.AddHours(-4).Ticks);
        string PathOf(Guid id)=>Path.Combine(root,id.ToString("N"));
        void Preserve(string name,Action<Guid,string> mutate){Guid id=Older();string path=PathOf(id);mutate(id,path);var before=Hashes(path);var r=owner.Execute();check(!r.Deleted.Contains(id)&&Directory.Exists(path)&&Equal(before,Hashes(path)),name+" preserved byte-for-byte");check(r.Decisions.Single(d=>d.Session==id).Eligible==false,name+" is ineligible");DeleteFixtureTree(root,path);}
        var dated=Older();string dp=PathOf(dated);Directory.SetLastWriteTimeUtc(dp,DateTime.UtcNow.AddYears(3));foreach(var f in Directory.GetFiles(dp))File.SetLastWriteTimeUtc(f,DateTime.UtcNow.AddYears(3));check(owner.Execute().Deleted.Contains(dated),"filesystem timestamp manipulation cannot replace durable ordering");
        foreach(var pair in new[]{("uncertain","Complete",(JsonNode?)JsonValue.Create(false)),("Clean false","Clean",JsonValue.Create(false)),("corrupt report","Corrupt",JsonValue.Create(true)),("faulted","Faults",JsonValue.Create(1)),("dropped overflow","Dropped",JsonValue.Create(1)),("I/O failure","Faults",JsonValue.Create(8)),("protocol failure","Faults",JsonValue.Create(64))})Preserve(pair.Item1,(_,p)=>Change(p,"recovery.json",pair.Item2,pair.Item3!.DeepClone()));
        Preserve("missing recovery",(_,p)=>File.Delete(Path.Combine(p,"recovery.json")));
        Preserve("malformed recovery",(_,p)=>File.WriteAllText(Path.Combine(p,"recovery.json"),"{"));
        Preserve("duplicate contradictory property",(_,p)=>File.WriteAllText(Path.Combine(p,"recovery.json"),File.ReadAllText(Path.Combine(p,"recovery.json")).Insert(1,"\"Clean\":false,")));
        Preserve("incomplete close",(_,p)=>File.Delete(Path.Combine(p,"finalized.json")));
        Preserve("wrong final sequence",(_,p)=>Change(p,"finalized.json","durable",4));
        Preserve("invalid close ordering",(_,p)=>Change(p,"finalized.json","closeUtcTicks",0));
        Preserve("wrong owner schema",(_,p)=>Change(p,"observer.json","schema",99));
        Preserve("wrong owner identity",(_,p)=>Change(p,"observer.json","session",Guid.NewGuid().ToString()));
        Preserve("unknown I/O diagnostic",(_,p)=>File.WriteAllText(Path.Combine(p,"observer-failure.txt"),"failure"));
        Preserve("unexpected nested unique data",(_,p)=>{Directory.CreateDirectory(Path.Combine(p,"nested"));File.WriteAllText(Path.Combine(p,"nested","unique.txt"),"keep");});
        Preserve("journal corruption",(_,p)=>{using var f=new FileStream(Path.Combine(p,"head0.bin"),FileMode.Open,FileAccess.ReadWrite);f.WriteByte(255);f.Flush(true);});
        foreach(long fault in new[]{1L,8L,64L})Preserve("durable actual fault "+fault,(id,p)=>AbnormalJournal(p,id,fault,0));
        Preserve("durable actual dropped record",(id,p)=>AbnormalJournal(p,id,0,1));
        Preserve("durable unreturned shutdown",(id,p)=>AbnormalJournal(p,id,0,0,true));
        Preserve("clean explicit pin",(id,p)=>owner.PinSession(id));
        Preserve("abnormal malformed pin",(_,p)=>{File.WriteAllText(Path.Combine(p,"preserve.pin"),"malformed");File.WriteAllText(Path.Combine(p,"recovery.json"),"malformed");});
        Preserve("producer PID still alive",(id,p)=>{using var self=Process.GetCurrentProcess();Change(p,"session.json","producer",self.Id);Change(p,"session.json","startUtcTicks",self.StartTime.ToUniversalTime().Ticks);FinalizeAgain(p,id);});
        Preserve("persistence PID still alive",(id,p)=>{using var self=Process.GetCurrentProcess();Change(p,"observer.json","pid",self.Id);Change(p,"observer.json","startUtcTicks",self.StartTime.ToUniversalTime().Ticks);FinalizeAgain(p,id);});
        foreach(string role in new[]{"producer","persistence"}){
            var id=Older();string path=PathOf(id);var before=Hashes(path);using(OrdinaryOwnership.Join(Path.Combine(path,"ownership.lock"))){var r=owner.Execute();check(!r.Deleted.Contains(id)&&!r.Decisions.Single(d=>d.Session==id).Eligible,role+" active lease defeats stale ownership metadata");}check(Equal(before,Hashes(path)),role+" active bytes preserved");DeleteFixtureTree(root,path);
        }
        foreach(string stage in new[]{"validated","file-staged","before-commit"}){
            var id=Older();string path=PathOf(id);var before=Hashes(path);var r=owner.Execute(s=>{if(s==stage)throw new IOException("injected retirement interruption");});check(r.Failures.Length>0&&!r.Deleted.Contains(id)&&Equal(before,Hashes(path)),stage+" interruption rolls back all files");DeleteFixtureTree(root,path);
        }
        foreach(string stage in new[]{"validated","before-commit"}){
            var id=Older();string path=PathOf(id);var before=Hashes(path);var r=owner.Execute(s=>{if(s==stage)File.WriteAllText(Path.Combine(path,"preserve.pin"),"late pin");});
            check(!r.Deleted.Contains(id)&&Directory.Exists(path),stage+" late pin prevents retirement");if(File.Exists(Path.Combine(path,"preserve.pin")))File.Delete(Path.Combine(path,"preserve.pin"));check(Equal(before,Hashes(path)),stage+" late pin retains original evidence");DeleteFixtureTree(root,path);
        }
        {
            var id=Older();string path=PathOf(id);var before=Hashes(path);using var child=Child("kill-retention",qualification.ToString());Await(Path.Combine(root,"kill-ready"),child);child.Kill();child.WaitForExit();
            check(Equal(before,Hashes(path)),"process kill before transaction commit restores whole session");File.Delete(Path.Combine(root,"kill-ready"));DeleteFixtureTree(root,path);
        }
        {
            using var child=Child("live-owner",qualification.ToString(),worker);Await(Path.Combine(root,"live-ready"),child);Guid id=Guid.Parse(File.ReadAllText(Path.Combine(root,"live-ready")));var r=owner.Execute();check(!r.Deleted.Contains(id)&&Directory.Exists(PathOf(id)),"concurrent real startup/producer/persistence owner survives");
            File.WriteAllText(Path.Combine(root,"finish"),"finish");child.WaitForExit(15000);check(child.HasExited&&child.ExitCode==0&&OrdinaryJournal.Recover(PathOf(id),id).Complete,"concurrent real CPU recorder completes correctly");DeleteFixtureTree(root,PathOf(id));File.Delete(Path.Combine(root,"finish"));File.Delete(Path.Combine(root,"live-ready"));
        }
        {
            var id=Older();string path=PathOf(id);bool startup=false,renameBlocked=false;
            var r=owner.Execute(stage=>{if(stage!="validated")return;try{Directory.Move(root,root+"-moved");}catch(IOException){renameBlocked=true;}
                using(var s=new OrdinarySession(root,worker)){Guid sid=s.Mapping.Session;s.Finish(true);using var process=Process.GetProcessById(s.PersistenceProcessId);process.WaitForExit(10000);startup=OrdinaryJournal.Recover(s.DirectoryPath,sid).Complete;}
                throw new IOException("retirement unavailable");});
            check(startup&&renameBlocked&&r.Failures.Length>0&&Directory.Exists(path),"cleanup failure and locked ancestry do not prevent independent recorder startup");DeleteFixtureTree(root,path);
        }
        {
            string outside=Path.Combine(Path.GetTempPath(),"NovaCoreRetentionSentinel-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(outside);string sentinel=Path.Combine(outside,"unique.txt");File.WriteAllText(sentinel,"outside authority");
            var id=Guid.NewGuid();Junction(PathOf(id),outside);var r=owner.Execute();check(File.ReadAllText(sentinel)=="outside authority"&&!r.AccountingComplete&&!r.Deleted.Contains(id),"session junction never traverses outside authority");Directory.Delete(PathOf(id));
            string saved=root+"-saved";Directory.Move(root,saved);Junction(root,outside);r=owner.Execute();check(r.Failures.Length>0&&File.ReadAllText(sentinel)=="outside authority","ancestor junction prevents any retention authority");Directory.Delete(root);Directory.Move(saved,root);File.Delete(sentinel);Directory.Delete(outside);
        }
        var inventory=owner.Execute();check(inventory.AccountingComplete&&inventory.After.TotalBytes==Directory.GetFiles(root,"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length),"total storage includes control files and ambiguous session bytes");
    }
}
