using NovaCore.Interop;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    bool IApplicationPresentation.LoadingCancelled=>approvedClose;
    private bool loading,loadingNativeReady,spawnApplied;
    private Label? loadingText;
    private Label? loadingTask,loadingCompleted;
    private readonly List<string> completedLoadingStages=new();
    private void ShowLoading(string stage,string task)
    {
        loading=true;var panel=OpenOverlay("",Ui(740),Ui(280));panel.Controls.Clear();panel.Padding=new(0);panel.BackColor=Color.FromArgb(25,25,25);BackColor=panel.BackColor;
        modalBackdrop.BackColor=panel.BackColor;modalBackdrop.Alpha=255;modalBackdrop.Show();modalBackdrop.BringToFront();panel.BringToFront();
        loadingText=new Label{Text=stage,Width=Ui(734),Height=Ui(23),TextAlign=ContentAlignment.MiddleCenter};panel.Controls.Add(loadingText);
        loadingMemory=new Label{Text=gpuMemoryReading!.DisplayText,Width=Ui(734),Height=Ui(62),TextAlign=ContentAlignment.MiddleCenter};panel.Controls.Add(loadingMemory);
        loadingTask=new Label{Text=task,Width=Ui(734),Height=Ui(38),ForeColor=Color.FromArgb(164,164,164),TextAlign=ContentAlignment.MiddleCenter};panel.Controls.Add(loadingTask);
        loadingCompleted=new Label{Width=Ui(734),Height=Ui(62),ForeColor=Color.FromArgb(112,112,112),TextAlign=ContentAlignment.MiddleCenter};panel.Controls.Add(loadingCompleted);
        SetLoadingEntries();RefreshPlayerGpuMemory();var cancel=ActionButton(panel,"CANCEL",CloseApproved,Ui(120));cancel.Margin=new(Ui(307),Ui(12),0,0);UpdateInputMode();Refresh();
    }
    void IApplicationPresentation.LoadingProgress(string stage,bool ready)
    {
        try{
            if(ready){loadingNativeReady=true;return;}
            Console.WriteLine($"PLAYER_LOADING stage={stage} monotonic={System.Diagnostics.Stopwatch.GetTimestamp()}");
            completedLoadingStages.Add(stage);RefreshPlayerGpuMemory();
            if(loadingText is not null){
                var split=stage.Split([" · "," | "," - "],2,StringSplitOptions.None);
                loadingText.Text=split[0];loadingTask!.Text=split.Length>1?split[1]:"";SetLoadingEntries();
                loadingText.Refresh();loadingTask.Refresh();loadingCompleted?.Refresh();loadingMemory?.Refresh();
            }
            // Only loading/cancel UI is enabled. Start is latched and renderer
            // disposal is deferred until the synchronous native call returns.
            Application.DoEvents();
        }catch(Exception ex){diagnostic=ex.ToString();CloseApproved();}
    }
    private void SetLoadingEntries(){
        if(loadingCompleted is not null)loadingCompleted.Text=string.Join("\n",completedLoadingStages.Take(Math.Max(0,completedLoadingStages.Count-1)).TakeLast(3));
    }
    private void AdvanceLoading()
    {
        if(!loading||!loadingNativeReady||approvedClose)return;
        if(!spawnApplied){
            spawnApplied=true;
            if(startupCraftPath is not null){session.Load(session.Revision,startupCraftBytes!,true);RememberPath(startupCraftPath);craftName.Text=session.Current!.Design.Data.Craft!.Name;}
            if(playerSettings.GameType=="Construction")EnterEditor();
            else if(playerSettings.GameType=="Flight"){LaunchCraft();if(flight is null){loading=false;throw new InvalidDataException("Selected flight was refused; loading cannot complete.");}}
            loadingNativeReady=false;
            if(NativeRuntime.AwaitApplicationPresentation()!=NativeResult.Success)throw new InvalidOperationException("Selected-session readiness request refused.");
            ShowLoading("Selected session", "Waiting for required terrain publication and completed presentation");
            return;
        }
        loading=false;modalBackdrop.BackColor=Color.Gray;modalBackdrop.Alpha=96;CloseOverlay();SetEditorPanels(editing);FocusViewport();
        Console.WriteLine($"PLAYER_LOADING_COMPLETE readiness=native-first-fence-and-selected-session-presentation memory={gpuMemoryReading?.Status} sample={gpuMemoryReading?.Sequence}");
    }
}
