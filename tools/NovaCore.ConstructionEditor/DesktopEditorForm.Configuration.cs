using NovaCore.Launcher;
using NovaCore.Interop;
using NovaCore.Platform;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private PlayerConfiguration playerSettings=new();
    private bool startCommitted;
    private string? startupCraftPath;
    private byte[]? startupCraftBytes;
    private string? settingsWarning;
    private Label? configurationError;
    private static readonly Color PanelColor=Color.FromArgb(32,34,37);
    private static readonly Color RowColor=Color.FromArgb(24,26,28);
    private static readonly Color AccentColor=Color.FromArgb(204,157,65);
    private float UiScale=>DeviceDpi/96f;
    private int Ui(int value)=>(int)Math.Round(value*UiScale);
    private void ShowPlayerConfiguration()
    {
        StartPlayerGpuMemory();
        if(!running&&!startCommitted){
            try{playerSettings=qualificationPath is null?PlayerConfiguration.Load():new();}
            catch(Exception ex)when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException){settingsWarning=ex.Message;playerSettings=new();}
        }
        if(!running)viewport.BackColor=Color.FromArgb(64,64,66);
        var panel=OpenOverlay(running?"NOVACORE · SETTINGS":"NOVACORE",Ui(600),Math.Min(ClientSize.Height-Ui(40),Ui(1100)));
        panel.Padding=new(Ui(12));panel.AutoScroll=true;
        var width=panel.Width-Ui(44);
        void Group(string name){panel.Controls.Add(new Label{Text=name,Width=width,Height=Ui(25),BackColor=Color.FromArgb(55,57,61),Padding=new(Ui(5),Ui(4),0,0)});}
        ComboBox Choice(string label,IEnumerable<object> values,object selected,bool enabled=true){
            var row=new FlowLayoutPanel{Width=width,Height=Ui(31),WrapContents=false,BackColor=RowColor,Margin=new(0,Ui(1),0,0)};
            row.Controls.Add(new Label{Text=label,Width=Ui(180),Height=Ui(27),TextAlign=ContentAlignment.MiddleLeft});
            var combo=new ComboBox{Width=width-Ui(190),DropDownStyle=ComboBoxStyle.DropDownList,Enabled=enabled,FlatStyle=FlatStyle.Flat,BackColor=RowColor,ForeColor=ForeColor};
            foreach(var value in values)combo.Items.Add(value);combo.SelectedItem=selected;if(combo.SelectedIndex<0&&combo.Items.Count>0)combo.SelectedIndex=0;row.Controls.Add(combo);panel.Controls.Add(row);return combo;
        }
        void Unavailable(string name,string reason){var label=new Label{Text=name+" — "+reason,AutoSize=true,MaximumSize=new(width,0),MinimumSize=new(width,Ui(33)),ForeColor=Color.FromArgb(153,155,158),BackColor=RowColor};panel.Controls.Add(label);tooltips.SetToolTip(label,reason);}
        Group("System");Choice("System",["Solar System"],"Solar System",false);
        Group("Game Type");var game=Choice("Game Type",["Exploration","Construction","Flight"],playerSettings.GameType,!running);
        Group("Starting Situation");var situation=Choice("Situation",[playerSettings.Situation],playerSettings.Situation,false);
        var vehicle=Choice("Vehicle",["None"],"None",!running);var location=Choice("Location",[playerSettings.Location],playerSettings.Location,false);
        var saved=Directory.Exists(SaveDirectory)?Directory.EnumerateFiles(SaveDirectory,"*.craft.json").Order(StringComparer.Ordinal).ToDictionary(p=>Path.GetFileName(p)[..^11],p=>p,StringComparer.Ordinal):new Dictionary<string,string>();
        void Dependencies(){var type=(string)game.SelectedItem!;situation.Items.Clear();situation.Items.Add(type=="Exploration"?"Solar overview":type=="Construction"?"Vehicle editor":"Landed vehicle");situation.SelectedIndex=0;location.Items.Clear();location.Items.Add(type=="Exploration"?"Solar System":"Florida launch slab");location.SelectedIndex=0;vehicle.Items.Clear();if(type!="Flight")vehicle.Items.Add(type=="Exploration"?"None":"New vehicle");if(type!="Exploration")foreach(var name in saved.Keys)vehicle.Items.Add(name);if(vehicle.Items.Count>0)vehicle.SelectedIndex=0;vehicle.Enabled=!running&&type!="Exploration";}
        game.SelectedIndexChanged+=(_,_)=>Dependencies();Dependencies();if(!vehicle.Items.Contains(playerSettings.Vehicle))vehicle.Items.Add(playerSettings.Vehicle);vehicle.SelectedItem=playerSettings.Vehicle;
        Group("Display");var display=Choice("Window mode",Enum.GetValues<NovaCoreWindowMode>().Cast<object>(),playerSettings.WindowMode);
        var resolution=Choice("Windowed resolution",Enum.GetValues<NovaCoreResolutionPreset>().Cast<object>(),playerSettings.Resolution);
        resolution.FormattingEnabled=true;resolution.Format+=(_,e)=>e.Value=e.ListItem?.ToString()?.Replace("Resolution","").Replace("NativeDesktop","Native desktop").Replace("x"," × ");
        void ResolutionState()=>resolution.Enabled=(NovaCoreWindowMode)display.SelectedItem! == NovaCoreWindowMode.Windowed;
        display.SelectedIndexChanged+=(_,_)=>ResolutionState();ResolutionState();
        Group("Vessel Textures");Unavailable("Textures","Constant PBR materials; texture backend unavailable");
        Group("Terrain Shadows and Textures");Unavailable("Terrain textures","Automatic authoritative residency; no quality selector");Unavailable("Terrain shadows","Configurable shadow maps unavailable");
        Group("Texture Streaming");Unavailable("Streaming","Automatic terrain streaming; no player budget control");
        Group("Cascaded Shadows and Filter");Unavailable("Cascades / filter","Backend not implemented");
        Group("Light System");Unavailable("Shadows / casters / atlas","Configurable light system not implemented");
        Group("Part Thumbnails");Choice("Size",["88 × 88"],"88 × 88",false);
        panel.Controls.Add(new Label{Text=running?"Display applies now through the renderer resize lifecycle. Spawn applies on next start.":"Spawn is validated before loading. Graphics above describe the active supported pipeline.",Width=width,Height=Ui(40)});
        configurationError=new Label{Text=settingsWarning??"",ForeColor=Color.Salmon,AutoSize=true,MaximumSize=new(width,0),MinimumSize=new(width,Ui(18))};panel.Controls.Add(configurationError);
        var start=ActionButton(panel,running?"APPLY SETTINGS":"START NOVACORE",()=>{
            if(startCommitted&&!running)return;
            try{
                var candidate=playerSettings with{GameType=(string)game.SelectedItem!,Situation=(string)situation.SelectedItem!,Vehicle=vehicle.SelectedItem as string??"",Location=(string)location.SelectedItem!,WindowMode=(NovaCoreWindowMode)display.SelectedItem!,Resolution=(NovaCoreResolutionPreset)resolution.SelectedItem!};candidate.Validate();
                string? craft=null;byte[]? validatedBytes=null;
                if(!running&&candidate.GameType!="Exploration"&&candidate.Vehicle!="New vehicle"){
                    if(!saved.TryGetValue(candidate.Vehicle,out craft)||!File.Exists(craft))throw new InvalidDataException("Select an available saved vehicle.");
                    var craftInfo=new FileInfo(craft);if(craftInfo.Length>4*1024*1024)throw new InvalidDataException("Craft file exceeds the spawn size limit.");
                    var bytes=File.ReadAllBytes(craft);validatedBytes=bytes;
                    // Validate in a disposable session; refusal does not mutate the retained draft.
                    using var probe=new ConstructionEditorSession(session.Catalog);probe.Load(probe.Revision,bytes,true);
                    if(candidate.GameType=="Flight"){
                        var design=probe.Current!.Design;var compiledCandidate=CraftCompiler.Compile(session.Catalog,design.Data,assetRoot);
                        CraftLaunchAdmission.Prepare(session.Catalog,bytes,assetRoot,CraftLaunchAdmission.SourceHash(bytes),design.Digest,compiledCandidate.Digest,session.Catalog.Digest);
                    }
                }
                var desktop=Screen.FromControl(this).Bounds;startupSettings=LauncherSettingsStore.LoadOrDefault();
                if(!ScenarioCatalog.TryCreateConfiguration(NovaCoreScenarioPreset.SolarSystemOverview,null,candidate.WindowMode,candidate.Resolution,startupSettings.Diagnostics,desktop.Width,desktop.Height,out configuration,out var error))throw new InvalidDataException(error);
                if(qualificationPath is null)candidate.Save();playerSettings=candidate;startupCraftPath=craft;startupCraftBytes=validatedBytes;
                if(!running&&!DiagnosticStartup.TryBeginLoading()){CloseApproved();return;}
                startCommitted=true;ApplyPlayerDisplay(desktop);CloseOverlay();
                if(running)return;
                ShowLoading("Core", "Preparing Solar system and renderer");BeginInvoke(RunViewport);
            }catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException){configurationError!.Text=ex.Message;}
        },Ui(180),Ui(34));start.Margin=new(width-Ui(180),Ui(4),0,0);start.BackColor=AccentColor;start.ForeColor=Color.Black;
        if(running)ActionButton(panel,"BACK",ShowPausePanel,width);
        if(!running){DiagnosticStartup.Mark(DiagnosticStartup.UiReady);if(qualificationPath is not null){
            display.SelectedItem=RcsScalabilityQualification||PlayerEntryQualification?NovaCoreWindowMode.BorderlessFullscreen:NovaCoreWindowMode.Windowed;
            resolution.SelectedItem=RcsScalabilityQualification||PlayerEntryQualification?NovaCoreResolutionPreset.Resolution3440x1440:NovaCoreResolutionPreset.Resolution1280x720;
            if(qualificationTankDefinition=="player-entry-flight"){game.SelectedItem="Flight";vehicle.SelectedItem=saved.Keys.Where(n=>n.StartsWith("Application-nc.tank.short-2-",StringComparison.Ordinal)).Order(StringComparer.Ordinal).LastOrDefault();}
            if(Environment.GetEnvironmentVariable("NOVACORE_QUALIFICATION_MANUAL_START")!="1")BeginInvoke(()=>start.PerformClick());
        }}
    }
    private void ApplyPlayerDisplay(Rectangle desktop)
    {
        var fullscreen=playerSettings.WindowMode==NovaCoreWindowMode.BorderlessFullscreen;
        FormBorderStyle=fullscreen?FormBorderStyle.None:FormBorderStyle.Sizable;
        if(fullscreen)Bounds=desktop;else ClientSize=new(configuration!.ClientResolution.Width,configuration.ClientResolution.Height);
        LayoutPlayerShell();
    }
}
