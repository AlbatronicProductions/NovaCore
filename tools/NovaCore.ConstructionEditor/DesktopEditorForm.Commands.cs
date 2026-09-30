using NovaCore.Interop;
using NovaCore.Simulation.Time;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private sealed record PlayerCommand(string Id,string Label,Action Execute,Func<string?> Refusal,Keys Shortcut=Keys.None,Func<bool>? Checked=null);
    private readonly Dictionary<string,PlayerCommand> commands=new(StringComparer.Ordinal);
    private readonly PlayerOverlayLayer topBar=new(){BackColor=PanelColor,Alpha=0,Visible=false};
    private readonly MenuStrip menus=new(){Dock=DockStyle.Fill,AutoSize=false,BackColor=PanelColor,ForeColor=Color.Gainsboro,ShowItemToolTips=true};
    private readonly Label pauseCaption=new(){Text="Simulation Paused",AutoSize=true,ForeColor=AccentColor,BackColor=PanelColor,Visible=false};
    private readonly Label timeHud=new(){AutoSize=true,BackColor=PanelColor,ForeColor=Color.Gainsboro,Padding=new(6),Visible=false};
    private readonly Label flightHud=new(){AutoSize=true,MaximumSize=new(740,0),BackColor=PanelColor,ForeColor=Color.Gainsboro,Padding=new(6),Visible=false};
    private readonly Label frameHud=new(){AutoSize=true,BackColor=PanelColor,ForeColor=Color.Gainsboro,Padding=new(6),Visible=false};
    private readonly HashSet<Keys> blockedShortcuts=new();
    private bool menuOpen,physicalUserPaused,frameStatistics;
    private long topLastHover,hudNext;
    private bool menuInputActive=>menuOpen;
    private void BuildPlayerCommands()
    {
        void Add(string id,string label,Action action,Func<string?>? refusal=null,Keys shortcut=Keys.None,Func<bool>? check=null)=>commands.Add(id,new(id,label,action,refusal??(()=>null),shortcut,check));
        void Deferred(string label,string reason,Keys shortcut=Keys.None)=>Add(label,label,()=>{},()=>reason,shortcut);
        string? Ready()=>!running||loading?"Session is loading":null;
        string? Scene()=>Ready()??(editing?"Unavailable in construction":null);
        Add("save","Save/Load",()=>{if(editing)ShowSaveBrowser(false);else ShowFlightBrowser();},Ready,Keys.F7);
        Add("new","Build New Vehicle",()=>{if(editing)NewCraft();else EnterEditor();},Ready);
        Add("launch","Launch Existing Vehicle",ShowLaunchBrowser,Ready);
        Add("settings","Settings",ShowPlayerConfiguration,Ready,Keys.Escape);
        Add("update","Check for Update",()=>ShowInformation("NOVACORE UPDATES","Automatic update checking is not available.\nCurrent bank: M16.2\nLocal development changes may be present."),Ready);
        Add("history","Version History",()=>ShowInformation("VERSION HISTORY","M16.2 · banked engineering baseline\ncd8fdcf977dee633f57da790e931a388b922048f\n\nPlayer Entry, Fullscreen Viewport && UI Architecture Convergence\nSee docs/milestones/M16.2.md for the bank record."),Ready);
        Add("exit","Exit Game",()=>RequestLeave(CloseApproved));
        Add("pause","Paused",()=>SetUserPause(!UserPaused),Scene,check:()=>UserPaused);
        for(var i=0;i<SimulationSpeedPresets.Count;i++){var index=i;Add("speed"+i,SimulationSpeedPresets.Get(i).Label.Replace("Simulation Speed: ",""),()=>SelectSpeed(index),()=>Scene()??(flight is not null&&index!=1?"Physical flight is qualified at 1× only":null),check:()=>flight is null?solar?.SpeedPresetIndex==index: index==1);}
        Add("slower","Decrease speed",()=>StepSpeed(-1),()=>Scene()??(flight is not null?"Physical flight rate changes unavailable":null),Keys.Oemcomma);
        Add("faster","Increase speed",()=>StepSpeed(1),()=>Scene()??(flight is not null?"Physical flight rate changes unavailable":null),Keys.OemPeriod);
        Deferred("Auto Warp","Not implemented");Deferred("Manifest","Not implemented",Keys.F11);Deferred("Roster","Crew backend unavailable",Keys.Shift|Keys.F3);
        Add("vessel","Active vehicle",()=>solar!.RefocusActiveVessel(flightCamera!),()=>Scene()??(flight is null?"No active vehicle":null));
        foreach(var focus in Enum.GetValues<NativePresentationFocus>().Where(v=>v!=NativePresentationFocus.None)){var selected=focus;Add("body"+focus,focus.ToString(),()=>solar!.Focus(flightCamera!,selected),Scene);}
        Add("stats","Frame Statistics",()=>frameStatistics=!frameStatistics,Ready,Keys.F1,()=>frameStatistics);
        foreach(var pair in new[]{("Transfer Planner",Keys.F4),("Flight Plan",Keys.F5),("Ground Track",Keys.F3),("Target Track",Keys.F12),("Resources",Keys.F6),("Staging",Keys.F8),("Threads",Keys.F10),("Profiler",Keys.None)})Deferred(pair.Item1,"Panel not implemented",pair.Item2);
        Add("orbit","Orbit Camera",()=>{if(flight is not null)solar!.RefocusActiveVessel(flightCamera!);else solar!.Focus(flightCamera!,solar.FocusIndex);},Scene,check:()=>solar?.CameraPresentationMode!=SolarCameraPresentationMode.SolarMap);
        Deferred("Free Camera","Qualified free camera unavailable");
        Add("map","Map Camera",()=>solar!.ResetPresentationCamera(flightCamera!),Scene,check:()=>solar?.CameraPresentationMode==SolarCameraPresentationMode.SolarMap);
        Add("cameraCycle","Cycle available camera",()=>ExecuteCommand(solar?.CameraPresentationMode==SolarCameraPresentationMode.SolarMap?"orbit":"map"),Scene,Keys.Shift|Keys.C);
        Deferred("Add Camera","Multiple cameras unavailable");Deferred("Follow Terrain","Contextual surface camera remains on its existing controller");
        foreach(var label in new[]{"Orbit Lines","Celestial Info","Show Orbit Markers","Flight Plans","Celestial Names"})Deferred(label,"Automatic presentation; player toggle unavailable");
        Add("debug","Debug",()=>ShowInformation("DIAGNOSTICS",diagnostic??"No current application error. Frame Statistics is available from View."),Ready);
        Add("defaultHud","Default",()=>{playerSettings=playerSettings with{Telemetry=true,GameTime=true,TelemetryAnchor="Bottom left",TimeAnchor="Top right"};RefreshHud(true);},Ready);
        Add("saveHud","Save Current Layout",ShowSaveHudLayout,Ready);
        Add("defaultLayout","Set Current as Default",()=>SaveHudLayout(true),Ready);
        Add("layouts","Manage Layouts",ShowHudLayouts,Ready);
        foreach(var label in new[]{"Rendezvous Control","Burn Control","Engine Control","Flight Control"})Deferred(label,"Control panel not implemented");
        Add("telemetry","Telemetry",()=>{playerSettings=playerSettings with{Telemetry=!playerSettings.Telemetry};RefreshHud(true);},Ready,check:()=>playerSettings.Telemetry);
        Deferred("Autopilot Settings","Autopilot unavailable");Deferred("Altitude","Qualified altitude observation unavailable");
        Add("time","Game Time",()=>{playerSettings=playerSettings with{GameTime=!playerSettings.GameTime};RefreshHud(true);},Ready,check:()=>playerSettings.GameTime);
        foreach(var label in new[]{"Attitude Indicators","Sequences","Crew Portraits","Navball"})Deferred(label,"Instrument backend unavailable");
        Deferred("Context Assignments","Custom contexts unavailable; telemetry is flight-only");
        menus.Renderer=new ToolStripProfessionalRenderer(new PlayerMenuColors());menus.Font=Font;
        ToolStripMenuItem Command(string id){var c=commands[id];var item=new ToolStripMenuItem(c.Label){Tag=id,ShortcutKeyDisplayString=c.Shortcut==Keys.None?"":new KeysConverter().ConvertToString(c.Shortcut),ShowShortcutKeys=true};item.Click+=(_,_)=>ExecuteCommand(id);return item;}
        ToolStripMenuItem Group(ToolStripItemCollection parent,string title,IEnumerable<string> ids){var group=new ToolStripMenuItem(title);foreach(var id in ids){if(id=="-")group.DropDownItems.Add(new ToolStripSeparator());else group.DropDownItems.Add(Command(id));}parent.Add(group);return group;}
        Group(menus.Items,"File",["save","-","new","launch","-","settings","update","history","-","exit"]);
        var universe=Group(menus.Items,"Universe",[]);Group(universe.DropDownItems,"Speed",new[]{"pause","-"}.Concat(Enumerable.Range(0,SimulationSpeedPresets.Count).Select(i=>"speed"+i)));universe.DropDownItems.Add(Command("Auto Warp"));Group(universe.DropDownItems,"Vessels",["vessel"]);Group(universe.DropDownItems,"Bodies",Enum.GetValues<NativePresentationFocus>().Where(v=>v!=NativePresentationFocus.None).Select(v=>"body"+v));universe.DropDownItems.Add(Command("Manifest"));universe.DropDownItems.Add(Command("Roster"));
        Group(menus.Items,"View",["stats","Transfer Planner","Flight Plan","Ground Track","Target Track","Resources","Staging","-","Threads","Profiler","-","orbit","Free Camera","map","cameraCycle","Add Camera","Follow Terrain","-","Orbit Lines","Celestial Info","Show Orbit Markers","Flight Plans","Celestial Names","debug"]);
        Group(menus.Items,"HUD",["defaultHud","saveHud","defaultLayout","layouts","-","Rendezvous Control","Burn Control","Engine Control","Flight Control","telemetry","Autopilot Settings","Altitude","Resources","time","Attitude Indicators","Sequences","Crew Portraits","Navball","Context Assignments"]);
        foreach(ToolStripMenuItem item in menus.Items){item.DropDownOpening+=(_,_)=>{menuOpen=true;InvalidatePlayerInput();RefreshCommandItems(menus.Items);UpdateInputMode();};item.DropDownClosed+=(_,_)=>{menuOpen=false;InvalidatePlayerInput();UpdateInputMode();};}
        topBar.Controls.Add(menus);Controls.Add(topBar);Controls.Add(timeHud);Controls.Add(flightHud);Controls.Add(frameHud);Controls.Add(pauseCaption);
    }
    private sealed class PlayerMenuColors:ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground=>PanelColor;
        public override Color ImageMarginGradientBegin=>PanelColor;
        public override Color ImageMarginGradientMiddle=>PanelColor;
        public override Color ImageMarginGradientEnd=>PanelColor;
        public override Color MenuItemSelected=>Color.FromArgb(77,78,81);
        public override Color MenuItemSelectedGradientBegin=>MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd=>MenuItemSelected;
        public override Color MenuItemPressedGradientBegin=>MenuItemSelected;
        public override Color MenuItemPressedGradientEnd=>MenuItemSelected;
    }
    private void RefreshCommandItems(ToolStripItemCollection items)
    {
        foreach(ToolStripItem item in items)if(item is ToolStripMenuItem menu){menu.ForeColor=ForeColor;menu.BackColor=PanelColor;if(menu.Tag is string id){var c=commands[id];var why=c.Refusal();menu.Enabled=why is null;menu.Checked=c.Checked?.Invoke()==true;menu.ToolTipText=why??c.Label;menu.Text=c.Label+(why is null?"":"  · "+why);}RefreshCommandItems(menu.DropDownItems);}
    }
    private void ExecuteCommand(string id)
    {
        var c=commands[id];if(c.Refusal() is {} why){message=why;return;}InvalidatePlayerInput();Attempt(c.Execute);UpdateInputMode();
    }
    private bool UserPaused=>flight is not null?physicalUserPaused:solar?.IsPaused==true;
    private void SetUserPause(bool pause)
    {
        if(flight is not null){physicalUserPaused=pause;flight.SuspendLive();}
        else if(solar is not null&&flightCamera is not null&&solar.IsPaused!=pause)solar.ApplyPresentationInput(flightCamera,new NativeInputState{PauseToggle=1},out _,out _);
    }
    private void SelectSpeed(int index)
    {
        if(flight is not null){if(index!=1)throw new InvalidDataException("Physical flight supports 1× only.");SetUserPause(false);return;}
        while(solar is not null&&solar.SpeedPresetIndex!=index)StepSpeedPreset(index>solar.SpeedPresetIndex?1:-1);
    }
    private void StepSpeed(int direction)
    {
        if(flight is not null||solar is null||flightCamera is null)return;
        // Pause is separate from the retained minimum preset. A further decrease
        // is idempotent; the first increase releases only this user pause.
        if(solar.SpeedPresetIndex==0){
            if(direction<0){SetUserPause(true);return;}
            if(direction>0&&solar.IsPaused){SetUserPause(false);return;}
        }
        StepSpeedPreset(direction);
    }
    // Explicit menu selection changes the retained preset without changing pause.
    private void StepSpeedPreset(int direction){if(flight is null&&solar is not null&&flightCamera is not null)solar.ApplyPresentationInput(flightCamera,new NativeInputState{RateIncrease=(byte)(direction>0?1:0),RateDecrease=(byte)(direction<0?1:0)},out _,out _);}
    private bool DispatchPlayerKey(IntPtr window,ulong key)
    {
        if(GetForegroundWindow()!=Handle)return false;
        if(loading){if(key==27)CloseApproved();return true;}
        if(key==27){if(menuOpen)return false;if(overlay is not null){CloseOverlay();return true;}ShowPausePanel();return true;}
        if(menuOpen||GetFocus()!=window||overlay is not null||Control.FromChildHandle(window) is TextBoxBase or UpDownBase or ComboBox)return false;
        if(blockedShortcuts.Contains((Keys)key))return true;
        var chord=(Keys)key|ModifierKeys;
        if((chord&Keys.KeyCode) is Keys.Oemcomma or Keys.OemPeriod && (ModifierKeys&~Keys.Shift)==Keys.None)chord&=~Keys.Shift;var command=commands.Values.FirstOrDefault(c=>c.Shortcut==chord&&c.Shortcut!=Keys.None);
        if(command is null)return false;ExecuteCommand(command.Id);return true;
    }
    private void UpdateInputMode(){if(applicationViewport is not null)applicationViewport->Mode=loading||overlay is not null||menuInputActive?2u:editing?0u:1u;}
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern IntPtr GetCapture();
    private void InvalidatePlayerInput(){foreach(var c in commands.Values)if(c.Shortcut!=Keys.None)blockedShortcuts.Add(c.Shortcut&Keys.KeyCode);ReleaseCapture();previousButtons=0;var child=GetWindow(viewport.Handle,5);if(child!=IntPtr.Zero)SendMessage(child,0x001F,IntPtr.Zero,IntPtr.Zero);}
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,uint message,IntPtr w,IntPtr l);
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern short GetAsyncKeyState(int key);
    private void RefreshPlayerChrome(float delta)
    {
        blockedShortcuts.RemoveWhere(key=>(GetAsyncKeyState((int)key)&0x8000)==0);
        var now=Environment.TickCount64;var point=PointToClient(Cursor.Position);
        var intentional=GetCapture()==IntPtr.Zero&&Control.MouseButtons==MouseButtons.None&&point.Y>=0&&point.Y<Ui(28)&&ClientRectangle.Contains(point)&&Form.ActiveForm==this;
        if(intentional||menuOpen)topLastHover=now;
        var show=running&&!loading&&overlay is null&&(menuOpen||now-topLastHover<700);
        var target=show?255:0;topBar.Alpha=(byte)Math.Clamp(topBar.Alpha+Math.Sign(target-topBar.Alpha)*Math.Max(1,(int)(delta*1500)),0,255);topBar.Visible=topBar.Alpha>0;
        if(topBar.Visible)topBar.BringToFront();
        pauseCaption.Visible=running&&!loading&&!editing&&(UserPaused||overlay is not null||menuOpen);if(pauseCaption.Visible)pauseCaption.BringToFront();pauseCaption.Location=new((ClientSize.Width-pauseCaption.Width)/2,Ui(55));
        if(overlay is not null)overlay.BringToFront();RefreshHud(false);
    }
    private void RefreshHud(bool force)
    {
        if(!force&&Environment.TickCount64<hudNext)return;hudNext=Environment.TickCount64+200;
        timeHud.Visible=running&&!loading&&!editing&&playerSettings.GameTime;flightHud.Visible=running&&!loading&&!editing&&flight is not null&&playerSettings.Telemetry;frameHud.Visible=running&&!loading&&frameStatistics;
        timeHud.Text=PlayerTimeText.Format(flight is not null,UserPaused||overlay is not null||menuOpen,solar?.SpeedPresetIndex??1);
        flightHud.Text=flight?.PlayerStatus??"";
        frameHud.Text=$"Frame interval: {lastApplicationDelta*1000:0.00} ms\nTarget: {native->Width} × {native->Height}\nGPU timing: unavailable";
        if(overlay is null){if(timeHud.Visible)timeHud.BringToFront();if(flightHud.Visible)flightHud.BringToFront();if(frameHud.Visible)frameHud.BringToFront();}
        AnchorHud(timeHud,playerSettings.TimeAnchor);AnchorHud(flightHud,playerSettings.TelemetryAnchor);AnchorHud(frameHud,editing?"Top right":"Top left");
        if(timeHud.Visible&&flightHud.Visible&&playerSettings.TimeAnchor==playerSettings.TelemetryAnchor)flightHud.Top+=playerSettings.TelemetryAnchor.StartsWith("Bottom",StringComparison.Ordinal)?-timeHud.Height-Ui(8):timeHud.Height+Ui(8);
    }
    private void AnchorHud(Control control,string anchor){var margin=Ui(16);control.Location=new(anchor.EndsWith("right",StringComparison.Ordinal)?Math.Max(margin,ClientSize.Width-control.Width-margin):margin,anchor.StartsWith("Bottom",StringComparison.Ordinal)?Math.Max(Ui(38),ClientSize.Height-control.Height-margin):Ui(38));}
    private void ShowInformation(string title,string text){var p=OpenOverlay(title,Ui(600),Ui(320));p.Controls.Add(new Label{Text=text,Width=Ui(535),Height=Ui(170)});ActionButton(p,"BACK",CloseOverlay,Ui(535));}
    private void SaveHudLayout(bool asDefault){if(qualificationPath is null)playerSettings.Save();message=asDefault?"Current HUD is the startup default.":"HUD layout saved.";}
    private void ShowSaveHudLayout()
    {
        var panel=OpenOverlay("SAVE HUD LAYOUT",Ui(440),Ui(220));var name=new TextBox{Width=Ui(390),Text="My layout"};panel.Controls.Add(name);
        ActionButton(panel,"SAVE NEW LAYOUT",()=>{var title=name.Text.Trim();PlayerHudLayouts.ValidateName(title);var library=PlayerHudLayouts.Load();if(library.Layouts.ContainsKey(title))throw new InvalidDataException("That layout name exists. Choose a new name.");library.Layouts.Add(title,PlayerHudLayout.Capture(playerSettings));if(qualificationPath is null)library.Save();CloseOverlay();},Ui(390));
        ActionButton(panel,"CANCEL",CloseOverlay,Ui(390));
    }
    private void ShowHudLayouts()
    {
        var library=PlayerHudLayouts.Load();var panel=OpenOverlay("HUD LAYOUTS",Ui(440),Ui(540));
        var names=new ListBox{Width=Ui(370),Height=Ui(130),BackColor=RowColor,ForeColor=ForeColor};foreach(var name in library.Layouts.Keys.Order(StringComparer.Ordinal))names.Items.Add(name);panel.Controls.Add(names);
        ActionButton(panel,"LOAD SELECTED",()=>{if(names.SelectedItem is not string name)throw new InvalidDataException("Select a saved HUD layout.");playerSettings=library.Layouts[name].Apply(playerSettings);RefreshHud(true);CloseOverlay();},Ui(370));
        foreach(var id in new[]{"Telemetry","Game time"}){panel.Controls.Add(new Label{Text=id,AutoSize=true});var combo=new ComboBox{Width=Ui(370),DropDownStyle=ComboBoxStyle.DropDownList};combo.Items.AddRange(PlayerConfiguration.Anchors);combo.SelectedItem=id=="Telemetry"?playerSettings.TelemetryAnchor:playerSettings.TimeAnchor;combo.SelectedIndexChanged+=(_,_)=>{playerSettings=id=="Telemetry"?playerSettings with{TelemetryAnchor=(string)combo.SelectedItem!}:playerSettings with{TimeAnchor=(string)combo.SelectedItem!};RefreshHud(true);};panel.Controls.Add(combo);}
        ActionButton(panel,"SAVE LAYOUT",()=>{SaveHudLayout(true);CloseOverlay();},Ui(370));ActionButton(panel,"BACK",CloseOverlay,Ui(370));
    }
}
