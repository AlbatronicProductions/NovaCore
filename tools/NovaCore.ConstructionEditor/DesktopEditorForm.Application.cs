using NovaCore.Core.Camera;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Launcher;
using NovaCore.Platform;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

internal sealed unsafe partial class DesktopEditorForm
{
    private NativeApplicationViewport* applicationViewport;
    private NativeRuntime.EditorMessageCallback? preprocess;
    private SolarSystemScene? solar;
    private CameraState? flightCamera;
    private bool editing;
    private bool firstApplicationFrame=true;
    private string? displayedFlightStatus;
    private float lastApplicationDelta;
    private Control? overlay;
    private readonly FlowLayoutPanel navigation=new(){Dock=DockStyle.Top,Height=46,Padding=new(8,5,8,5),WrapContents=false};
    private readonly FlowLayoutPanel inspector=new(){Dock=DockStyle.Right,Width=260,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new(12),AutoScroll=true};
    private readonly FlowLayoutPanel cards=new(){Width=220,Height=480,AutoScroll=true,WrapContents=true};
    private readonly FlowLayoutPanel contextInspector=new(){Width=280,Height=420,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new(12),AutoScroll=true,Visible=false};
    private readonly Label selectedInfo=new(){Width=225,Height=110};
    private readonly Label launchInfo=new(){Width=225,Height=66,Text="EARTH · FLORIDA\nDevelopment launch slab\nPhysical flight · 1×"};
    private string category="All";
    private readonly Dictionary<string,Bitmap> thumbnails=new(StringComparer.Ordinal);
    private readonly Dictionary<string,Button> partCards=new(StringComparer.Ordinal);
    private readonly ToolTip tooltips=new();
    private LauncherSettings? startupSettings;
    private NovaCoreLaunchConfiguration? configuration;
    ReusablePartVisuals IApplicationPresentation.Visuals=>visuals;
    int IApplicationPresentation.RenderCapacity=>renderCapacity;
    NativeApplicationViewport* IApplicationPresentation.Viewport=>applicationViewport;
    NativeRuntime.EditorMessageCallback IApplicationPresentation.Preprocess=>preprocess!;
    ConstructionFlightScene? IApplicationPresentation.Flight=>flight;
    bool IApplicationPresentation.Editing=>editing;
    bool IApplicationPresentation.Paused=>editing||overlay is not null;
    void IApplicationPresentation.Attach(SolarSystemScene scene,CameraState camera){solar=scene;flightCamera=camera;}
    void IApplicationPresentation.BeginFrame(in NativeInputState input)
    {
        if(applicationViewport is null)return;
        if(firstApplicationFrame){firstApplicationFrame=false;message="Solar system · choose New vehicle to build, or Menu for navigation.";UpdateStatus();FocusViewport();}
        lastApplicationDelta=input.DeltaSeconds;
        applicationViewport->Mode=overlay is not null?2u:editing?0u:1u;
        if(!editing&&overlay is null&&flight is not null&&displayedFlightStatus!=flight.PlayerStatus){displayedFlightStatus=flight.PlayerStatus;status.Text=displayedFlightStatus+"\nMouse: orbit / zoom · 1–0: celestial focus · F: craft · Menu: return to construction · Physical flight: 1×";}
        try{if(editing&&overlay is null)Input(*native);}
        catch(Exception ex){ReportPresentationFailure(ex);}
        QualifyApplicationFrame(input);
    }
    void IApplicationPresentation.PresentEditor(NativeFrameSubmission* frame)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();var allocated=GC.GetAllocatedBytesForCurrentThread();
        // Keep borrowed buffers and publication acknowledgements intact while
        // suppressing flight presentation. No Solar or physical time advances.
        frame->OrbitVertexCount=frame->PreviousOrbitVertexCount=frame->BodyForwardVertexCount=frame->TargetDirectionVertexCount=0;
        frame->PlanetaryPatchCount=frame->DistantBodyCount=0;frame->PlanetaryGpu=default;
        frame->PlanetaryMode=NativePlanetaryMode.CpuReference;frame->PlanetarySurfaceMode=NativePlanetarySurfaceMode.Bounded;
        frame->PlanetaryPresentation=default;frame->SolarLighting=default;frame->FacilityCaster=null;
        frame->ProductionBillboard=null;frame->ProductionBillboardFrame=null;frame->ProductionBillboardFlags=0;
        try{Render(frame);}catch(Exception ex){ReportPresentationFailure(ex);}
        if(qualificationPath is not null)MeasureQualificationFrame(lastApplicationDelta,System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-allocated);
    }
    private void ReportPresentationFailure(Exception exception)
    {
        diagnostic=exception.ToString();message="Presentation stopped. Your construction draft is retained.";Console.Error.WriteLine(exception);UpdateStatus();
        if(native is not null)native->Stop=1;FailQualification(exception);
    }
    private void BuildPlayerShell()
    {
        Text="NovaCore";BackColor=Color.FromArgb(20,26,34);ForeColor=Color.FromArgb(224,231,237);
        sidebar.Width=360;sidebar.Padding=new(8);sidebar.BackColor=Color.FromArgb(27,34,44);
        inspector.BackColor=contextInspector.BackColor=sidebar.BackColor;navigation.BackColor=Color.FromArgb(14,20,28);status.BackColor=navigation.BackColor;status.Height=72;
        Controls.Add(viewport);Controls.Add(inspector);Controls.Add(sidebar);Controls.Add(status);Controls.Add(navigation);
        ActionButton(navigation,"Menu",ShowPause,90);ActionButton(navigation,"New vehicle",()=>{if(editing)NewCraft();else EnterEditor();},130);
        ActionButton(navigation,"Return to flight",LeaveEditor,140);ActionButton(navigation,"Focus craft",FocusCraft,110);
        var body=new FlowLayoutPanel(){Width=344,Height=400,WrapContents=false};
        var categories=new FlowLayoutPanel(){Width=112,Height=400,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
        foreach(var name in new[]{"All"}.Concat(session.Catalog.Data.Definitions.Select(d=>d.Standard!.Category).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
            ActionButton(categories,name,()=>{category=name;RefreshCatalog();},104,70);
        void FitCatalog(){body.Height=Math.Max(180,sidebar.ClientSize.Height-190);cards.Height=categories.Height=body.Height-6;}
        sidebar.Resize+=(_,_)=>FitCatalog();FitCatalog();
        body.Controls.Add(categories);body.Controls.Add(cards);sidebar.Controls.Add(body);
        foreach(CatalogItem item in catalogList.Items){var d=item.Definition;thumbnails[d.Id]=PartThumbnail.Draw(assets[d.Id]);
            var card=new Button(){Text=d.Construction!.Name,Image=thumbnails[d.Id],TextImageRelation=TextImageRelation.ImageAboveText,Width=103,Height=128,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(37,46,59),ForeColor=ForeColor,Tag=d.Id};
            card.Click+=(_,_)=>Attempt(()=>HoldPart(d));tooltips.SetToolTip(card,$"{d.Construction.Name}\n{d.Standard!.Purpose}\nDry mass {d.DryMassKg:0.##} kg · development content");partCards.Add(d.Id,card);
        }
        var modes=new FlowLayoutPanel(){Width=340,Height=40};ActionButton(modes,"Select",()=>{CancelGhost();freeGhost=null;mode.SelectedIndex=0;},80);ActionButton(modes,"Move",()=>{NeedSelection();CancelGhost();mode.SelectedIndex=2;},80);ActionButton(modes,"Bin",()=>{CancelGhost();freeGhost=null;mode.SelectedIndex=0;message="Held part cancelled.";},80);sidebar.Controls.Add(modes);
        var symmetry=new FlowLayoutPanel(){Width=340,Height=62};symmetry.Controls.Add(new Label(){Text="Symmetry",AutoSize=true,Margin=new(4,8,4,0)});count.Width=70;symmetry.Controls.Add(count);
        ActionButton(symmetry,"Rotate",RotateHeld,90);sidebar.Controls.Add(symmetry);
        sidebar.Controls.Add(new Label(){Width=330,Height=70,Text="Click a part card, then a connection.\nRight drag: orbit · middle drag: pan\nWheel: zoom · X: symmetry · Delete: cancel"});
        contextInspector.Controls.Add(selectedInfo);contextInspector.Controls.Add(new Label(){Text="Propellant / charge (%)",AutoSize=true});
        var quantities=new FlowLayoutPanel(){Width=232,Height=38};fill.Width=103;charge.Width=103;quantities.Controls.Add(fill);quantities.Controls.Add(charge);contextInspector.Controls.Add(quantities);contextInspector.Controls.Add(stores);contextInspector.Controls.Add(electrical);
        ActionButton(contextInspector,"Apply to selected group",()=>{NeedSelection();CancelGhost();session.ConfigureSelection(session.Revision,selection!,(double)fill.Value/100,(double)charge.Value/100,stores.Checked,electrical.Checked);RefreshInspector();},225);
        ActionButton(contextInspector,"Delete selected group",()=>{NeedSelection();CancelGhost();session.Remove(session.Revision,selection!);selection=null;if(session.Current is null){savePath=null;draftIdentity=Guid.NewGuid().ToString("N");}contextInspector.Hide();RefreshInspector();},225);
        var history=new FlowLayoutPanel(){Width=232,Height=42};ActionButton(history,"Undo",()=>NavigateHistory(true),108);ActionButton(history,"Redo",()=>NavigateHistory(false),108);inspector.Controls.Add(history);
        inspector.Controls.Add(launchInfo);craftName.Width=225;inspector.Controls.Add(craftName);
        craftName.Validated+=(_,_)=>Attempt(()=>{if(session.Current is not null&&session.Current.Design.Data.Craft!.Name!=craftName.Text)session.SetName(session.Revision,craftName.Text);});
        ActionButton(inspector,"Fill consumables",FillConsumables,225);
        ActionButton(inspector,"Launch vehicle",LaunchCraft,225,42);
        ActionButton(inspector,"Save / Load",()=>ShowSaveBrowser(false),225);
        Controls.Add(contextInspector);RefreshCatalog();SetEditorPanels(false);Resize+=(_,_)=>CenterOverlay();
    }
    private Button ActionButton(Control parent,string label,Action action,int width=240,int height=32)
    {
        var button=new Button(){Text=label,Width=width,Height=height,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(43,57,72),ForeColor=ForeColor};
        button.Click+=(_,_)=>Attempt(action);parent.Controls.Add(button);return button;
    }
    private void RefreshCatalog()
    {
        cards.SuspendLayout();cards.Controls.Clear();
        foreach(CatalogItem item in catalogList.Items){var d=item.Definition;if(category!="All"&&d.Standard!.Category!=category)continue;if(session.Current is null&&!d.Standard!.RootEligible)continue;cards.Controls.Add(partCards[d.Id]);}
        cards.ResumeLayout();
    }
    private void RefreshInspector()
    {
        var part=selection is null?null:session.Current?.Design.Parts.FirstOrDefault(p=>p.Instance.Id==selection).Definition;
        selectedInfo.Text=part is null?"CRAFT CONSTRUCTION\nChoose a part in the viewport to inspect its group.":$"{part.Construction!.Name}\n{part.Standard!.Purpose}\nDry mass: {part.DryMassKg:0.##} kg";
        var policy=part?.Standard?.Configuration;fill.Enabled=policy?.FillStores==true;charge.Enabled=policy?.ChargeBattery==true;stores.Enabled=policy?.EnableStores==true;electrical.Enabled=policy?.EnableElectrical==true;
        if(part is not null&&selection is not null){
            var configured=session.Current!.Design.Data.Configuration.Single(c=>c.Part==selection);
            var capacity=part.Stores.Sum(s=>s.CapacityKg);var chargeCapacity=part.Construction!.Electrical.Sum(e=>e.CapacityJ);
            fill.Value=capacity>0?(decimal)Math.Clamp(configured.Stores.Sum(s=>s.QuantityKg)/capacity*100,0,100):0;
            charge.Value=chargeCapacity>0?(decimal)Math.Clamp(configured.Electrical.Sum(e=>e.ChargeJ)/chargeCapacity*100,0,100):0;
            stores.Checked=configured.Stores.All(s=>s.Enabled);electrical.Checked=configured.Electrical.All(e=>e.Enabled);
        }
        RefreshCatalog();
    }
    private FlowLayoutPanel OpenOverlay(string title,int width=360,int height=360)
    {
        if(overlay is not null){Controls.Remove(overlay);overlay.Dispose();}
        var panel=new FlowLayoutPanel(){Width=width,Height=height,Padding=new(24),FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.FromArgb(24,33,45),AutoScroll=true};
        panel.Controls.Add(new Label(){Text=title,Width=width-52,Height=42,Font=new(Font.FontFamily,15,FontStyle.Bold)});
        overlay=panel;Controls.Add(panel);CenterOverlay();panel.BringToFront();if(applicationViewport is not null)applicationViewport->Mode=2;
        contextInspector.Hide();
        sidebar.Enabled=inspector.Enabled=navigation.Enabled=false;
        return panel;
    }
    private void CenterOverlay(){if(overlay is not null)overlay.Location=new(Math.Max(0,(ClientSize.Width-overlay.Width)/2),Math.Max(navigation.Height,(ClientSize.Height-overlay.Height)/2));}
    private void CloseOverlay(){if(overlay is not null){Controls.Remove(overlay);overlay.Dispose();overlay=null;}sidebar.Enabled=inspector.Enabled=true;navigation.Enabled=running;if(applicationViewport is not null)applicationViewport->Mode=editing?0u:1u;FocusViewport();}
    private void ShowStartup()
    {
        navigation.Enabled=running;
        startupSettings=LauncherSettingsStore.LoadOrDefault();var panel=OpenOverlay("NOVACORE",430,370);
        var resolution=new ComboBox(){Width=370,DropDownStyle=ComboBoxStyle.DropDownList};
        resolution.FormattingEnabled=true;resolution.Format+=(_,e)=>{if(e.ListItem is NovaCoreResolutionPreset preset)e.Value=preset==NovaCoreResolutionPreset.NativeDesktop?"Native display":preset.ToString().Replace("Resolution",string.Empty).Replace("x"," × ");};
        foreach(var value in Enum.GetValues<NovaCoreResolutionPreset>())resolution.Items.Add(value);resolution.SelectedItem=startupSettings.Resolution;
        var fullscreen=new CheckBox(){Text="Borderless fullscreen",Checked=startupSettings.WindowMode==NovaCoreWindowMode.BorderlessFullscreen,AutoSize=true};
        resolution.Enabled=!fullscreen.Checked;fullscreen.CheckedChanged+=(_,_)=>resolution.Enabled=!fullscreen.Checked;
        panel.Controls.Add(new Label(){Text="Windowed resolution",AutoSize=true});panel.Controls.Add(resolution);panel.Controls.Add(fullscreen);
        panel.Controls.Add(new Label(){Text="Explore the Solar system and build a vehicle.\nDevelopment content · Florida flight at 1×",Width=370,Height=64});
        ActionButton(panel,running?"Apply display settings":"Start NovaCore",()=>{
            if(!running&&!DiagnosticStartup.TryBeginLoading()){CloseApproved();return;}
            var desktop=Screen.FromControl(this).Bounds;
            if(!ScenarioCatalog.TryCreateConfiguration(NovaCoreScenarioPreset.SolarSystemOverview,null,fullscreen.Checked?NovaCoreWindowMode.BorderlessFullscreen:NovaCoreWindowMode.Windowed,(NovaCoreResolutionPreset)resolution.SelectedItem!,startupSettings.Diagnostics,desktop.Width,desktop.Height,out configuration,out var error))throw new InvalidDataException(error);
            if(qualificationPath is null&&!LauncherSettingsStore.TrySave(startupSettings with {WindowMode=configuration!.WindowMode,Resolution=configuration.ResolutionPreset,Diagnostics=configuration.Diagnostics},out error))throw new IOException(error);
            FormBorderStyle=fullscreen.Checked?FormBorderStyle.None:FormBorderStyle.Sizable;
            if(fullscreen.Checked){Bounds=desktop;}else ClientSize=new(Math.Max(960,configuration!.ClientResolution.Width),Math.Max(700,configuration.ClientResolution.Height));
            CloseOverlay();navigation.Enabled=true;message=running?"Display settings applied.":"Loading the Solar system…";UpdateStatus();Refresh();if(!running)BeginInvoke(RunViewport);
        },370,42);
        if(running)ActionButton(panel,"Back",CloseOverlay,370);
        if(!running)DiagnosticStartup.Mark(DiagnosticStartup.UiReady);
        if(qualificationPath is not null&&!running){if(!RecorderQualification&&!SurfaceRetryQualification){resolution.SelectedItem=RcsScalabilityQualification?NovaCoreResolutionPreset.Resolution3440x1440:NovaCoreResolutionPreset.Resolution1280x720;fullscreen.Checked=RcsScalabilityQualification;}BeginInvoke(()=>ClickButton("Start NovaCore"));}
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr window);
    private void FocusViewport(){if(running){var child=GetWindow(viewport.Handle,5);if(child!=IntPtr.Zero)SetFocus(child);}}
    private void ShowPartContext(NativeEditorViewport input){if(selection is null){contextInspector.Hide();return;}RefreshInspector();contextInspector.Location=new(Math.Clamp(viewport.Left+input.PointerX+12,0,Math.Max(0,ClientSize.Width-contextInspector.Width)),Math.Clamp(viewport.Top+input.PointerY,46,Math.Max(46,ClientSize.Height-status.Height-contextInspector.Height)));contextInspector.Show();contextInspector.BringToFront();}
    private void ShowPause()
    {
        if(!running)return;if(overlay is not null){CloseOverlay();return;}
        var panel=OpenOverlay(editing?"CONSTRUCTION · PAUSED":"PAUSED",360,450);
        ActionButton(panel,"Resume",CloseOverlay,300);
        if(editing){ActionButton(panel,"Save / Load",()=>ShowSaveBrowser(false),300);ActionButton(panel,"Exit editor",LeaveEditor,300);}
        else
        {
            ActionButton(panel,"New vehicle / retained design",EnterEditor,300);
            if(flight is {Failed:false})ActionButton(panel,"Save flight",SaveFlight,300);
            ActionButton(panel,"Load flight",LoadFlight,300);
        }
        ActionButton(panel,"Settings",ShowStartup,300);
        ActionButton(panel,"Quit",()=>RequestLeave(CloseApproved),300);
    }
    private void EnterEditor(){CloseOverlay();editing=true;flight?.SuspendLive();SetEditorPanels(true);RefreshInspector();if(applicationViewport is not null)applicationViewport->Mode=0;message="Choose a category and a part card.";UpdateStatus();}
    private void LeaveEditor(){CloseOverlay();editing=false;displayedFlightStatus=null;SetEditorPanels(false);flight?.SuspendLive();if(applicationViewport is not null)applicationViewport->Mode=1;FocusViewport();message=flight is null?"Solar system": "Z ignite · X cutoff · WASD/QE attitude · mouse orbit · F craft · Menu returns to construction";UpdateStatus();}
    private void SetEditorPanels(bool value){contextInspector.Hide();sidebar.Visible=inspector.Visible=value;navigation.Controls[2].Visible=value;navigation.Controls[3].Visible=value;}
    private void RunViewport()
    {
        if(running)return;var lease=new NativeApplicationViewport(){Input=new(){Size=64,Version=1,ParentWindow=(ulong)viewport.Handle.ToInt64()},Mode=1};
        applicationViewport=&lease;native=&lease.Input;running=true;
        preprocess=(window,msg,w,l,_)=>{
            try{
                if(QualificationMessage(msg)!=0)return 1;
                if(msg==0x100&&(l&(1L<<30))==0){
                    if(w==27){ShowPause();return 1;}
                    if(editing&&overlay is null&&Control.FromChildHandle((IntPtr)window) is not TextBoxBase and not UpDownBase and not ComboBox){
                        if(w==46){CancelGhost();freeGhost=null;mode.SelectedIndex=0;return 1;}
                        if(w==88){count.SelectedIndex=(count.SelectedIndex+((ModifierKeys&Keys.Shift)!=0?3:1))%4;return 1;}
                    }
                }
                var m=Message.Create((IntPtr)window,(int)msg,(IntPtr)w,(IntPtr)l);
                if(Application.FilterMessage(ref m))return 1;
                if(msg is >=0x100 and <=0x109&&Control.FromChildHandle(m.HWnd) is {} control&&control.PreProcessControlMessage(ref m)==PreProcessControlState.MessageProcessed)return 1;
                return 0;
            }catch(Exception ex){diagnostic=ex.ToString();message=ex.Message;UpdateStatus();FailQualification(ex);return 1;}
        };
        try{
            if(!SampleOptions.TryParse(["--scene=sol"],out var options,out var error)||!LogOptions.TryParse(options.LogArguments,out var log,out error))throw new InvalidDataException(error);
            if(ApplicationRenderer.Run(options,log,options.UseProductionEarth,this)!=0)throw new InvalidDataException("The application renderer stopped. See the retained diagnostic log.");
        }catch(Exception ex){Console.Error.WriteLine(ex);message=ex.Message;UpdateStatus();FailQualification(ex);}
        finally{native=null;applicationViewport=null;preprocess=null;running=false;}
        try{RecorderReport();SurfaceRetryReport();}catch(Exception e){FailQualification(e);}
        if(approvedClose)BeginInvoke(Close);
    }
    private void CloseApproved(){DiagnosticStartup.Mark(DiagnosticStartup.Shutdown);approvedClose=true;if(native is not null)native->Stop=1;else BeginInvoke(Close);}
}
