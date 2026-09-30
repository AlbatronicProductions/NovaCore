using NovaCore.Simulation.Transactions;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private void ShowPausePanel()
    {
        if(!running||loading)return;
        var panel=OpenOverlay("",Ui(260),Ui(editing?204:276));panel.Controls.Clear();panel.Padding=new(Ui(12));
        void Button(string text,Action action,string? reason=null){var b=ActionButton(panel,text,action,Ui(230),Ui(30));b.Enabled=reason is null;if(reason is not null)tooltips.SetToolTip(b,reason);}
        Button("Resume",CloseOverlay);
        if(editing){Button("Save/Load",()=>ExecuteCommand("save"));Button("Exit Editor",LeaveEditor);}
        else{Button("New Vehicle",()=>ExecuteCommand("new"));Button("Launch Vehicle",()=>ExecuteCommand("launch"));Button("Abandon Vehicle",ConfirmAbandon,flight is null?"No active vehicle":null);Button("Save/Load",()=>ExecuteCommand("save"));}
        Button("Settings",()=>ExecuteCommand("settings"));Button("Quit",()=>ExecuteCommand("exit"));
    }
    private void ConfirmAbandon()
    {
        if(flight is null)return;
        var panel=OpenOverlay("ABANDON VEHICLE?",Ui(440),Ui(240));panel.Controls.Add(new Label{Text="The active flight will end. Save it first to keep its state. Your construction draft is retained.",Width=Ui(400),Height=Ui(80)});
        ActionButton(panel,"ABANDON",()=>{var previous=flight;flight=null;compiled=null;physicalUserPaused=false;solar!.RefreshActiveVessel(flightCamera!,default);solar.ResetPresentationCamera(flightCamera!);previous?.Dispose();CloseOverlay();},Ui(400));ActionButton(panel,"CANCEL",ShowPausePanel,Ui(400));
    }
    private void ShowLaunchBrowser()
    {
        var panel=OpenOverlay("LAUNCH EXISTING VEHICLE",Ui(600),Ui(480));var list=new ListBox{Width=Ui(555),Height=Ui(285),DisplayMember="Text",BackColor=RowColor,ForeColor=ForeColor};panel.Controls.Add(list);
        if(Directory.Exists(SaveDirectory))foreach(var path in Directory.EnumerateFiles(SaveDirectory,"*.craft.json").Order(StringComparer.Ordinal))list.Items.Add(new SavedEntry(path));
        panel.Controls.Add(new Label{Text="Location: canonical Florida launch slab. Full launch admission is required.",Width=Ui(555),Height=Ui(42)});
        ActionButton(panel,"LOAD INTO EDITOR",()=>{if(list.SelectedItem is not SavedEntry chosen)throw new InvalidDataException("Select a vehicle.");RequestLeave(()=>{session.LoadFrom(session.Revision,chosen.Path,true);RememberPath(chosen.Path);craftName.Text=session.Current!.Design.Data.Craft!.Name;selection=null;EnterEditor();FocusCraft();});},Ui(555));
        ActionButton(panel,"BACK",ShowPausePanel,Ui(555));
    }
    private void ShowFlightBrowser()
    {
        var panel=OpenOverlay("FLIGHTS · SAVE / LOAD",Ui(600),Ui(480));var list=new ListBox{Width=Ui(555),Height=Ui(240),BackColor=RowColor,ForeColor=ForeColor};panel.Controls.Add(list);
        if(Directory.Exists(FlightDirectory))foreach(var path in Directory.EnumerateFiles(FlightDirectory,"*.ncflight.json").Order(StringComparer.Ordinal))list.Items.Add(Path.GetFileName(path));
        var name=new TextBox{Width=Ui(555),Text="Flight"};panel.Controls.Add(name);
        var save=ActionButton(panel,"SAVE NEW FLIGHT",()=>{
            var value=name.Text.Trim();if(value.Length is 0 or >80||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0||value is "." or ".."||value.EndsWith('.')||value.EndsWith(' '))throw new InvalidDataException("Choose a name without path characters.");
            var path=Path.Combine(FlightDirectory,value+".ncflight.json");if(File.Exists(path))throw new IOException("That flight exists; choose a new name.");
            var current=flight??throw new InvalidDataException("No active flight.");current.SuspendLive();var bytes=current.SaveFlight();Directory.CreateDirectory(FlightDirectory);
            var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,bytes);File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}ShowFlightBrowser();
        },Ui(555));save.Enabled=flight is {Failed:false};tooltips.SetToolTip(save,save.Enabled?"Save physical flight state":"No healthy active flight to save; exploration saves are unavailable");
        ActionButton(panel,"LOAD SELECTED FLIGHT",()=>{
            if(list.SelectedItem is not string selected)throw new InvalidDataException("Select a saved flight.");
            var path=Path.Combine(FlightDirectory,selected);var confirm=OpenOverlay("REPLACE ACTIVE FLIGHT?",Ui(440),Ui(240));confirm.Controls.Add(new Label{Text="Your unsaved active flight state will be replaced. The construction draft is retained.",Width=Ui(400),Height=Ui(70)});
            ActionButton(confirm,"LOAD",()=>RestoreFlightPath(path),Ui(400));ActionButton(confirm,"CANCEL",ShowFlightBrowser,Ui(400));
        },Ui(555));ActionButton(panel,"BACK",ShowPausePanel,Ui(555));
    }
    private void RestoreFlightPath(string path)
    {
        if(solar is null)throw new InvalidDataException("The session is not ready.");
        var length=new FileInfo(path).Length;if(length is <=0 or >SimulationTransactionEngine.ConstructionSaveMaximumBytes)throw new InvalidDataException("Flight save exceeds its bounded capacity.");
        var next=ConstructionFlightScene.RestoreFlight(session.Catalog,File.ReadAllBytes(path),assetRoot,solar,visuals);
        try{next.FloridaView.Solar.RetainRendererBuffers(solar);}catch{next.Dispose();throw;}
        var previous=flight;solar=next.FloridaView.Solar;flight=next;compiled=next.Craft;physicalUserPaused=false;previous?.Dispose();LeaveEditor();
    }
}
