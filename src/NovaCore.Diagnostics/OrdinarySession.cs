using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NovaCore.Diagnostics;

/// <summary>Pre-GPU startup gate. The helper is separate from the player process and heavy capture.</summary>
public sealed class OrdinarySession : IDisposable
{
    public OrdinaryMapping Mapping {get;}
    public string DirectoryPath {get;}
    public int PersistenceProcessId=>worker.Id;
    readonly Process worker;readonly FileStream ownership;bool finished;
    public OrdinarySession(string root,string executable)
    {
        Directory.CreateDirectory(root);
        using var reservation=RuntimeRetention.AdmitSession(root);
        using var admission=new FileStream(Path.Combine(root,"admission.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        Guid id=Guid.NewGuid();DirectoryPath=Path.Combine(root,id.ToString("N"));
        using var self=Process.GetCurrentProcess();
        Mapping=new OrdinaryMapping("Local\\NovaCoreMinimum-"+id.ToString("N"),id,true,self.Id,self.StartTime.ToUniversalTime().Ticks);
        try{
            Directory.CreateDirectory(DirectoryPath);
            ownership=OrdinaryOwnership.Create(Path.Combine(DirectoryPath,"ownership.lock"));
            var paths=new[]{Environment.ProcessPath!,Path.Combine(AppContext.BaseDirectory,"NovaCore.dll"),Path.Combine(AppContext.BaseDirectory,"NovaCore.Native.dll"),typeof(OrdinaryMapping).Assembly.Location,executable};
            var identity=paths.Where(File.Exists).Distinct().Select(p=>new{path=p,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))}).ToArray();
            OrdinaryStorage.Json(Path.Combine(DirectoryPath,"session.json"),new{schema=2,session=id,producer=self.Id,startUtcTicks=self.StartTime.ToUniversalTime().Ticks,utc=DateTime.UtcNow,openedUtcTicks=DateTime.UtcNow.Ticks,openedQpc=Stopwatch.GetTimestamp(),qpcFrequency=Stopwatch.Frequency,reservedBytes=OrdinaryStorage.MaximumSessionBytes,identity});
            var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
            start.ArgumentList.Add("observe");start.ArgumentList.Add(Mapping.Name);start.ArgumentList.Add(id.ToString());start.ArgumentList.Add(DirectoryPath);
            worker=Process.Start(start)??throw new IOException("Minimum persistence owner did not start.");
            var timer=Stopwatch.StartNew();while(Mapping.Read(15)==0&&!worker.HasExited&&timer.Elapsed<TimeSpan.FromSeconds(20))Thread.Sleep(10);
            if(Mapping.Read(15)!=1||worker.HasExited)throw new IOException("Mandatory minimum recorder could not establish durable storage. Unrecorded session refused before GPU initialization.");
            if(!Mapping.Emit(OrdinaryPhase.Session))throw new IOException("Minimum session entry failed.");
        }catch{Mapping.Write(14,1);Mapping.Dispose();ownership?.Dispose();throw;}
    }
    public void Finish(bool success)
    {
        if(finished)return;finished=true;
        Mapping.Close(success);
        // No renderer is alive now. Disposal does not wait for disk; observer holds mapping until drained.
    }
    public void Dispose(){Finish(false);Mapping.Dispose();worker.Dispose();ownership.Dispose();}
}
