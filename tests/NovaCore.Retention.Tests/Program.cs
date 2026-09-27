using NovaCore.Diagnostics;
using System.Diagnostics;
using System.Text.Json;

if(args.Length>0&&args[0]=="exit-attestation"){
 var child=RuntimeRetention.ForQualification(Guid.Parse(args[1]));using var s=new OrdinarySession(child.QualificationPath,args[2]);
 File.WriteAllText(Path.Combine(child.QualificationPath,"exit-ready"),s.Mapping.Session.ToString());
 if(args[3]=="gpu-error")s.Mapping.Emit(OrdinaryPhase.Error,result:-4);
 s.Finish(args[3]!="ordinary");Environment.ExitCode=args[3]=="crash-after-close"?27:0;return;
}
if(args.Length>0&&args[0]=="kill-retention"){
 var child=RuntimeRetention.ForQualification(Guid.Parse(args[1]));child.Execute(stage=>{if(stage=="file-staged"){File.WriteAllText(Path.Combine(child.QualificationPath,"kill-ready"),"ready");Thread.Sleep(Timeout.Infinite);}});return;
}
if(args.Length>0&&args[0]=="live-owner"){
 var child=RuntimeRetention.ForQualification(Guid.Parse(args[1]));using var s=new OrdinarySession(child.QualificationPath,args[2]);File.WriteAllText(Path.Combine(child.QualificationPath,"live-ready"),s.Mapping.Session.ToString());
 var deadline=Stopwatch.StartNew();while(!File.Exists(Path.Combine(child.QualificationPath,"finish"))&&deadline.ElapsedMilliseconds<15000)Thread.Sleep(10);s.Finish(true);using var worker=Process.GetProcessById(s.PersistenceProcessId);worker.WaitForExit(10000);return;
}
if(args.Length!=2)throw new ArgumentException("output directory, CPU recorder helper required");

if(args[0] is "--bounded-only" or "--rolling-soak"){
    int count=0;BoundedStorageTests.Run((ok,message)=>{if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS "+message);},args[0]=="--rolling-soak"?int.Parse(args[1]):32,args[0]=="--rolling-soak");Console.WriteLine($"PASS {count} bounded checks");return;
}
if(args.Length>0&&args[0]=="kill-rolling"){
 var child=RuntimeRetention.ForQualification(Guid.Parse(args[1]));child.RollingExecute(stage=>{if(stage=="before-commit"){File.WriteAllText(Path.Combine(child.QualificationPath,"kill-ready"),"ready");Thread.Sleep(Timeout.Infinite);}});return;
}
if(args[0]=="kill-index"){
 var child=RuntimeRetention.ForQualification(Guid.Parse(args[1]));child.ConsolidateOrdinaryCapsule(stage=>{if(stage=="index-before-commit"){File.WriteAllText(Path.Combine(child.QualificationPath,"kill-ready"),"ready");Thread.Sleep(Timeout.Infinite);}},[]);return;
}

var owner=RuntimeRetention.ForQualification(Guid.NewGuid());string root=owner.QualificationPath;Directory.CreateDirectory(root);
int checks=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
// Fixture directories are structurally fixed beneath the runtime qualification
// namespace. No cleanup or test overload can redirect retention elsewhere.
Guid Create(long utc)
{
    Guid id=Guid.NewGuid();string path=Path.Combine(root,id.ToString("N"));Directory.CreateDirectory(path);
    using var mapping=new OrdinaryMapping("Local\\RetentionTest-"+id,id,true);
    long q=Stopwatch.GetTimestamp();
    OrdinaryOwnership.Write(Path.Combine(path,"session.json"),new{schema=2,session=id,producer=int.MaxValue,startUtcTicks=1,openedUtcTicks=utc,openedQpc=q,qpcFrequency=Stopwatch.Frequency});
    OrdinaryOwnership.Write(Path.Combine(path,"observer.json"),new{schema=1,session=id,pid=int.MaxValue,startUtcTicks=1});
    using(OrdinaryOwnership.Create(Path.Combine(path,"ownership.lock"))){}
    mapping.Emit(OrdinaryPhase.Session);mapping.Close(true);var events=new List<OrdinaryEvent>();byte[] bytes=new byte[256];for(int n=1;n<=3;n++){Check(mapping.TryRead(n,bytes),"fixture valid record");events.Add(OrdinaryEvent.Read(bytes));}
    using(var journal=new OrdinaryJournal(path,id)){journal.Commit(events,3,0,0);}
    File.WriteAllText(Path.Combine(path,"performance.json"),"{}");var r=OrdinaryJournal.Recover(path,id);OrdinaryObserver.WriteReport(path,r);OrdinaryOwnership.Finalize(path,id,r);
    OrdinaryTermination.Write(path,id,int.MaxValue,1,true,0,DateTime.UtcNow.Ticks);return id;
}
try{
    var a=Create(DateTime.UtcNow.AddHours(-3).Ticks);var one=owner.Execute();Check(one.Deleted.Length==0&&one.After.CleanSessions==1,"one clean survives");
    var b=Create(DateTime.UtcNow.AddHours(-2).Ticks);var two=owner.Execute();Check(two.Deleted.Length==0&&two.After.CleanSessions==2,"newest two survive");
    var c=Create(DateTime.UtcNow.AddHours(-1).Ticks);var fail=owner.Execute(s=>{if(s=="file-staged")throw new IOException("injected");});
    Check(fail.Deleted.Length==0&&Directory.GetFiles(Path.Combine(root,a.ToString("N"))).Length==11,"staged failure rolls back entire directory");
    var three=owner.Execute();Console.WriteLine(JsonSerializer.Serialize(three));Check(three.Deleted.SequenceEqual(new[]{a})&&three.Failures.Length==0,"older eligible directory transaction retires");
    Check(Directory.Exists(Path.Combine(root,b.ToString("N")))&&Directory.Exists(Path.Combine(root,c.ToString("N"))),"newest survivors intact");
    AbnormalRetentionTests.Run(owner,Create,Check,Path.GetFullPath(args[1]));
    RecorderStorageTests.Run(owner,Create,Check);
    RetentionAdversaries.Run(owner,Create,Check,Path.GetFullPath(args[1]));
    BoundedStorageTests.Run(Check);
    Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"qualification.json"),JsonSerializer.Serialize(new{judgment="PASS",checks,gpuExposure=0,runtimeRootOverride=false}));
    Console.WriteLine($"PASS {checks} retention checks");
}finally{
    // Test-owned directory only, constructed above from a fresh GUID. Never a
    // runtime session; stop rather than follow any reparse test fixture.
    RetentionAdversaries.DeleteFixtureTree(root,root);
}
