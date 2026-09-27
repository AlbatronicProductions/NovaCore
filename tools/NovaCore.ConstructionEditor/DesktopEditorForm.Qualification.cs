using System.Runtime.InteropServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.ConstructionEditor;

// Opt-in engineering driver. It sends ordinary window clicks to the actual child
// viewport and invokes the same visible buttons. It cannot provide Player PASS.
internal sealed unsafe partial class DesktopEditorForm
{
    private readonly string? qualificationPath;
    private readonly string qualificationTankDefinition;
    private int qualificationStep,qualificationChecks,editorWarmup;
    private bool editorMeasuring;
    private byte[]? qualificationSaved;
    private string? qualificationTank,qualificationAdapter;
    private double qualificationYaw,qualificationDistance;
    private Double3 qualificationTarget;
    private readonly List<(double Frame,double Cpu,long Allocated)> warmMeasurements=new(360);
    private readonly List<object> warmWindows=new(3);
    private int warmGc0,warmGc1,warmGc2;
    private static object Distribution(IEnumerable<double> source){var v=source.Order().ToArray();double P(double p)=>v[(int)Math.Ceiling(p*v.Length)-1];return new {median=P(.5),p95=P(.95),p99=P(.99),max=v[^1]};}
    private void MeasureQualificationFrame(float seconds,double cpu,long allocated)
    {
        if(!editorMeasuring||editorWarmup++<64||warmMeasurements.Count>=360)return;
        if(warmMeasurements.Count%120==0){warmGc0=GC.CollectionCount(0);warmGc1=GC.CollectionCount(1);warmGc2=GC.CollectionCount(2);}
        warmMeasurements.Add((seconds*1000,cpu,allocated));
        if(warmMeasurements.Count%120==0){var window=warmMeasurements.TakeLast(120).ToArray();warmWindows.Add(new {samples=120,frameMs=Distribution(window.Select(v=>v.Frame)),cpuMs=Distribution(window.Select(v=>v.Cpu)),allocatedBytes=Distribution(window.Select(v=>(double)v.Allocated)),gc=new[]{GC.CollectionCount(0)-warmGc0,GC.CollectionCount(1)-warmGc1,GC.CollectionCount(2)-warmGc2}});}
    }
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr parent,uint command);
    [DllImport("user32.dll")] private static extern bool PostMessageW(IntPtr window,uint message,nuint wParam,nint lParam);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint source,uint target,bool attach);
    [DllImport("user32.dll")] private static extern IntPtr SetCapture(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    private uint QualificationMessage(uint message)
    {
        if(qualificationPath is null||message!=0x8001)return 0;
        SetCapture(craftName.Handle);return 1;
    }
    private void ViewportMessage(uint message,nuint w=0,int x=200,int y=200)=>PostMessageW(GetWindow(viewport.Handle,5),message,w,(nint)((y<<16)|(x&65535)));
    private void RequireQualification(bool value,string label){qualificationChecks++;if(!value)throw new InvalidDataException("Editor integration: "+label+"; "+message);}
    private void ClickButton(string text)=>AllControls(this).OfType<Button>().First(b=>b.Text==text&&b.Visible&&b.Enabled).PerformClick();
    private static IEnumerable<Control> AllControls(Control parent){foreach(Control control in parent.Controls){yield return control;foreach(var nested in AllControls(control))yield return nested;}}
    private void WindowClick(Double3 position)
    {
        qualificationHover=null;
        var foreground=GetWindowThreadProcessId(GetForegroundWindow(),out _);var own=GetCurrentThreadId();
        var attached=foreground!=own&&AttachThreadInput(own,foreground,true);
        try{RequireQualification(SetForegroundWindow(Handle),"engineering driver activates its own window");}finally{if(attached)AttachThreadInput(own,foreground,false);}
        var point=camera.Project(position,viewport.ClientSize.Width,viewport.ClientSize.Height);
        RequireQualification(point.Depth>0&&point.X>=0&&point.Y>=0&&point.X<viewport.Width&&point.Y<viewport.Height,"target projects inside client");
        var packed=(nint)(((int)Math.Round(point.Y)<<16)|((int)Math.Round(point.X)&65535));var child=GetWindow(viewport.Handle,5);
        RequireQualification(child!=IntPtr.Zero,"native child owned by viewport");
        RequireQualification(PostMessageW(child,0x201,1,packed)&&PostMessageW(child,0x202,0,packed),"post native left edges");
        RequireQualification(PostMessageW(child,0x200,0,(nint)((10<<16)|10)),"later pointer motion cannot relocate the pending click");
    }
    private void SocketClick(string instance,string socket)
    {
        var p=session.Current!.Design.Parts.Single(p=>p.Instance.Id==instance);
        WindowClick(p.Instance.Pose.Then(p.Definition.Attachments.Single(a=>a.Id==socket).Frame).Position);
    }
    private void FailQualification(Exception e)
    {
        if(qualificationPath is null)return;Environment.ExitCode=1;approvedClose=true;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(qualificationPath))!);
        File.WriteAllText(qualificationPath,JsonSerializer.Serialize(new {judgment="FAIL",step=qualificationStep,checks=qualificationChecks,error=e.ToString()},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static string NativeRuntimeLibraryIdentity()=>Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"NovaCore.Native.dll"))));
}
