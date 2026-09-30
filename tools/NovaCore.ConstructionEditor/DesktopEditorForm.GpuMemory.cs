using NovaCore.Interop;
using System.Diagnostics;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private bool gpuMemoryStarted;
    private readonly System.Windows.Forms.Timer gpuMemoryUiTimer=new(){Interval=250};
    private Label? loadingMemory;
    private PlayerGpuMemoryReading? gpuMemoryReading;
    private ulong gpuMemoryLoggedSequence;
    private void StartPlayerGpuMemory(){
        if(gpuMemoryStarted)return;
        // Product RecorderLaunchPolicy has admitted the session before Shown.
        // This does not mark Loading or initialize world/renderer resources.
        DiagnosticStartup.Mark(DiagnosticStartup.UiReady);
        PlayerGpuMemory.Begin(viewport.Handle);gpuMemoryStarted=true;
        gpuMemoryUiTimer.Tick+=(_,_)=>RefreshPlayerGpuMemory();gpuMemoryUiTimer.Start();
        RefreshPlayerGpuMemory();
    }
    private void RefreshPlayerGpuMemory(){
        if(!gpuMemoryStarted)return;
        var begin=Stopwatch.GetTimestamp();
        PlayerGpuMemory.Poll();
        gpuMemoryReading=PlayerGpuMemory.Read();
        var text=gpuMemoryReading.DisplayText;
        if(loadingMemory is {IsDisposed:false})loadingMemory.Text=text;
        if(gpuMemoryReading.Sequence!=gpuMemoryLoggedSequence){
            gpuMemoryLoggedSequence=gpuMemoryReading.Sequence;
            Console.WriteLine($"GPU_MEMORY_UI phase={(loading?"loading":running?"gameplay":"configuration")} sequence={gpuMemoryReading.Sequence} sampleQpc={gpuMemoryReading.Timestamp} uiQpc={Stopwatch.GetTimestamp()} utc={gpuMemoryReading.SampleUtc:O} status={gpuMemoryReading.Status} usage={gpuMemoryReading.UsageBytes} budget={gpuMemoryReading.BudgetBytes} capacity={gpuMemoryReading.CapacityBytes} queryNs={gpuMemoryReading.QueryNanoseconds} adapter={gpuMemoryReading.Adapter} uuid={gpuMemoryReading.Uuid} luid={gpuMemoryReading.Luid} luidValid={gpuMemoryReading.LuidValid} vendor={gpuMemoryReading.Vendor:X4} device={gpuMemoryReading.Device:X4} heapMask={gpuMemoryReading.HeapMask} copyAndUiUs={Stopwatch.GetElapsedTime(begin).TotalMicroseconds:F1}");
        }
        if(gpuMemoryReading.Status==PlayerGpuMemoryStatus.AdmissionDenied)CloseApproved();
    }
    internal void StopPlayerGpuMemory(){
        gpuMemoryUiTimer.Stop();gpuMemoryUiTimer.Dispose();
        if(!gpuMemoryStarted)return;
        PlayerGpuMemory.End();gpuMemoryStarted=false;
        Console.WriteLine("GPU_MEMORY_CLOSED timer=stopped renderer=retired sharedInstance=destroyed");
    }
}
