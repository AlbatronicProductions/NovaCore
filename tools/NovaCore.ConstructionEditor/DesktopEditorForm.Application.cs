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
    private readonly PlayerOverlayLayer modalBackdrop=new(){BackColor=Color.Gray,Alpha=96,Visible=false};
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
    bool IApplicationPresentation.Paused=>editing||loading||overlay is not null||menuOpen||physicalUserPaused;
    void IApplicationPresentation.Attach(SolarSystemScene scene,CameraState camera){solar=scene;flightCamera=camera;}
    void IApplicationPresentation.BeginFrame(in NativeInputState input)
    {
        if(applicationViewport is null)return;playerFrameCounter++;
        if(firstApplicationFrame){firstApplicationFrame=false;message="Solar system · choose New vehicle to build, or Menu for navigation.";UpdateStatus();FocusViewport();}
        try{AdvanceLoading();}catch(Exception ex){ReportPresentationFailure(ex);}
        lastApplicationDelta=input.DeltaSeconds;
        UpdateInputMode();
        RefreshPlayerChrome(input.DeltaSeconds);
        if(!editing&&overlay is null&&flight is not null&&displayedFlightStatus!=flight.PlayerStatus){displayedFlightStatus=flight.PlayerStatus;status.Text=displayedFlightStatus+"\nMouse: orbit / zoom · 1–0: celestial focus · F: craft · Menu: return to construction · Physical flight: 1×";}
        try{if(editing&&overlay is null&&!menuOpen&&!loading)Input(*native);}
        catch(Exception ex){ReportPresentationFailure(ex);}
        if(!loading)QualifyApplicationFrame(input);
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
        Text="NovaCore";BackColor=Color.FromArgb(55,57,60);ForeColor=Color.FromArgb(224,231,237);
        // The editor has one presentation tree. Its panels overlay the full renderer.
        viewport.Dock=sidebar.Dock=inspector.Dock=status.Dock=DockStyle.None;
        sidebar.BackColor=inspector.BackColor=contextInspector.BackColor=status.BackColor=PanelColor;
        sidebar.Padding=new(Ui(10));inspector.Padding=new(Ui(12));status.Padding=new(Ui(8));
        Controls.Add(viewport);Controls.Add(inspector);Controls.Add(sidebar);Controls.Add(status);Controls.Add(modalBackdrop);
        BuildEditorCatalogue();
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
        ActionButton(inspector,"Focus craft",FocusCraft,225);
        ActionButton(inspector,"Return to flight",LeaveEditor,225);
        StyleEditorControls(inspector);StyleEditorControls(contextInspector);
        Controls.Add(contextInspector);BuildPlayerCommands();RefreshCatalog();SetEditorPanels(false);Resize+=(_,_)=>LayoutPlayerShell();LayoutPlayerShell();
    }
    private void LayoutPlayerShell()
    {
        viewport.Bounds=ClientRectangle;modalBackdrop.Bounds=ClientRectangle;
        topBar.Bounds=new(0,0,ClientSize.Width,Ui(27));
        LayoutEditorPanels();
        CenterOverlay();
    }
    private Button ActionButton(Control parent,string label,Action action,int width=240,int height=32)
    {
        var button=new Button(){Text=label,Width=width,Height=height,FlatStyle=FlatStyle.Flat,BackColor=PanelColor,ForeColor=ForeColor};
        button.Click+=(_,_)=>Attempt(action);parent.Controls.Add(button);return button;
    }
    private void RefreshCatalog()
    {
        if(session.Current is null&&category!="All"&&!catalogParts.Any(d=>d.Standard!.RootEligible&&d.Standard.Category==category)){category="All";categoryPicker.SelectedItem=category;}
        cards.SuspendLayout();cards.Controls.Clear();
        foreach(var d in catalogParts){if(category!="All"&&d.Standard!.Category!=category)continue;if(session.Current is null&&!d.Standard!.RootEligible)continue;cards.Controls.Add(partCards[d.Id]);}
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
        var panel=new FlowLayoutPanel(){Width=width,Height=height,Padding=new(Ui(12)),FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=PanelColor,AutoScroll=true};
        panel.Controls.Add(new Label(){Text=title,Width=width-52,Height=42,Font=new(Font.FontFamily,10,FontStyle.Bold)});
        if(applicationViewport is not null)applicationViewport->Mode=2;
        InvalidatePlayerInput();
        overlay=panel;Controls.Add(panel);CenterOverlay();topBar.Hide();modalBackdrop.Visible=running;modalBackdrop.BringToFront();panel.BringToFront();
        contextInspector.Hide();
        sidebar.Enabled=inspector.Enabled=false;
        return panel;
    }
    private void CenterOverlay(){if(overlay is not null)overlay.Location=new(Math.Max(0,(ClientSize.Width-overlay.Width)/2),Math.Max(0,(ClientSize.Height-overlay.Height)/2));}
    private void CloseOverlay(){InvalidatePlayerInput();if(overlay is not null){Controls.Remove(overlay);overlay.Dispose();overlay=null;}modalBackdrop.Hide();sidebar.Enabled=inspector.Enabled=true;UpdateInputMode();RaiseEditorPanels();FocusViewport();}
    private void ShowStartup()=>ShowPlayerConfiguration();
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr window);
    private void FocusViewport(){if(running){var child=GetWindow(viewport.Handle,5);if(child!=IntPtr.Zero)SetFocus(child);}}
    private void ShowPartContext(NativeEditorViewport input){if(selection is null){contextInspector.Hide();return;}RefreshInspector();contextInspector.Location=new(Math.Clamp(viewport.Left+input.PointerX+12,0,Math.Max(0,ClientSize.Width-contextInspector.Width)),Math.Clamp(viewport.Top+input.PointerY,46,Math.Max(46,ClientSize.Height-status.Height-contextInspector.Height)));contextInspector.Show();contextInspector.BringToFront();}
    private void ShowPause(){if(!running||loading)return;if(overlay is not null)CloseOverlay();else ShowPausePanel();}
    private void EnterEditor(){CloseOverlay();editing=true;flight?.SuspendLive();SetEditorPanels(true);RefreshInspector();if(applicationViewport is not null)applicationViewport->Mode=0;message="Choose a category and a part card.";UpdateStatus();}
    private void LeaveEditor(){CloseOverlay();editing=false;displayedFlightStatus=null;SetEditorPanels(false);flight?.SuspendLive();if(applicationViewport is not null)applicationViewport->Mode=1;FocusViewport();message=flight is null?"Solar system": "Z ignite · X cutoff · WASD/QE attitude · mouse orbit · F craft · Menu returns to construction";UpdateStatus();}
    private void SetEditorPanels(bool value){contextInspector.Hide();sidebar.Visible=inspector.Visible=status.Visible=value;RaiseEditorPanels();}
    private void RunViewport()
    {
        if(running)return;var lease=new NativeApplicationViewport(){Input=new(){Size=64,Version=1,ParentWindow=(ulong)viewport.Handle.ToInt64()},Mode=2};
        applicationViewport=&lease;native=&lease.Input;running=true;
        preprocess=(window,msg,w,l,_)=>{
            try{
                if(QualificationMessage(msg)!=0)return 1;
                if(msg==0x101)blockedShortcuts.Remove((Keys)w);
                if(msg==0x100&&(l&(1L<<30))==0){
                    if(DispatchPlayerKey((IntPtr)window,w))return 1;
                    if(editing&&overlay is null&&!menuOpen&&!loading&&GetForegroundWindow()==Handle&&GetFocus()==(IntPtr)window&&Control.FromChildHandle((IntPtr)window) is not TextBoxBase and not UpDownBase and not ComboBox){
                        if(w==46){CancelGhost();freeGhost=null;editorIntent=0;return 1;}
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
        }catch(OperationCanceledException)when(approvedClose){Console.WriteLine("PLAYER_LOADING_CANCELLED requested=true cleanup=complete");}
        catch(Exception ex){Console.Error.WriteLine(ex);message=ex.Message;UpdateStatus();FailQualification(ex);}
        finally{native=null;applicationViewport=null;preprocess=null;running=false;loading=false;}
        if(!approvedClose){startCommitted=false;ShowInformation("SESSION STOPPED",message+"\nYour construction draft is retained. Close the application and retry after correcting the reported cause.");}
        try{RecorderReport();SurfaceRetryReport();}catch(Exception e){FailQualification(e);}
        if(approvedClose)BeginInvoke(Close);
    }
    private void CloseApproved(){DiagnosticStartup.Mark(DiagnosticStartup.Shutdown);approvedClose=true;if(native is not null)native->Stop=1;else BeginInvoke(Close);}
}
