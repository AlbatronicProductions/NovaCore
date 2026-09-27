using System.Diagnostics;
using System.Text.Json;

namespace NovaCore.Diagnostics;

public sealed partial class RuntimeRetention
{
    public static bool IsRuntimeSession(Guid id,string path)=>string.Equals(Path.GetFullPath(path),Path.Combine(RuntimeRoot,id.ToString("N")),StringComparison.OrdinalIgnoreCase);
    // Runs after persistence disposal. Scheduling failure never fails recording.
    public static void Schedule()
    {
        try{using var self=Process.GetCurrentProcess();using var p=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"NovaCore.Recorder.exe")){ArgumentList={"retain-runtime-after",self.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),self.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)},UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}
        catch(Exception e){Console.Error.WriteLine("Recorder retention not started; all evidence preserved: "+e.Message);}
    }
    public static RetentionReport RunAndReport()
    {
        var owner=new RuntimeRetention();using var publication=new Mutex(false,owner.MutexName);bool acquired;
        try{acquired=publication.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
        if(!acquired)return owner.RollingExecute();
        var report=owner.RollingExecute();var anchors=new List<Microsoft.Win32.SafeHandles.SafeFileHandle>();
        try{
            anchors=owner.Anchor();owner.AtomicMetadata(Path.Combine(owner.root,"retention-status.json"),JsonSerializer.SerializeToUtf8Bytes(report));
            owner.MarkBelowThreshold(report);
        }catch(Exception e){var message="Retention report could not persist; unretired evidence preserved: "+e.Message;Console.Error.WriteLine(message);report=report with{Status="PRESERVE_WITH_WARNING",Warning=message,Failures=report.Failures.Append(message).ToArray()};}
        finally{DisposeAll(anchors);publication.ReleaseMutex();}
        return report;
    }
    public static string? StorageWarning()
    {
        var owner=new RuntimeRetention();var anchors=new List<Microsoft.Win32.SafeHandles.SafeFileHandle>();
        try{
            if(!Directory.Exists(owner.root))return null;
            anchors=owner.Anchor();using var file=Open(Path.Combine(owner.root,"retention-status.json"),false,false);using var report=Json(Path.Combine(owner.root,"retention-status.json"));
            var r=report.RootElement;var retired=r.GetProperty("Deleted").EnumerateArray().Select(x=>x.GetGuid()).ToHashSet();
            var described=r.GetProperty("Decisions").EnumerateArray().Select(x=>x.GetProperty("Session").GetGuid()).Where(id=>!retired.Contains(id)).ToHashSet();
            var current=Directory.GetDirectories(owner.root).Select(Path.GetFileName).Where(n=>Guid.TryParseExact(n,"N",out _)).Select(n=>Guid.ParseExact(n!,"N")).ToHashSet();
            if(!described.SetEquals(current))return "Recorder storage has changed since its last retention report. Evidence is preserved; storage review is pending.";
            return r.GetProperty("Warning").GetString();
        }
        catch{return "Recorder storage status is unavailable or ambiguous. Evidence is preserved; inspect MinimumRecorder storage before discarding any recording.";}
        finally{DisposeAll(anchors);}
    }
}
