using NovaCore.ConstructionEditor;
using NovaCore.Launcher;
using System.Text.Json;

var checks=0;
void Check(bool value,string name){checks++;if(!value)throw new InvalidOperationException(name);}
void Refuses(PlayerConfiguration value,string name){try{value.Validate();throw new Exception("Accepted invalid "+name);}catch(InvalidDataException){checks++;}}
var defaults=new PlayerConfiguration();defaults.Validate();
Check(PlayerTimeText.Format(true,false,15)=="Flight · 1×","physical HUD never substitutes exploration warp");
Check(PlayerTimeText.Format(true,true,15)=="Flight · Paused","physical effective pause");
Check(PlayerTimeText.Format(false,true,1)=="Exploration · Paused","menu and user pause use one display");
for(var preset=0;preset<NovaCore.Simulation.Time.SimulationSpeedPresets.Count;preset++)
{
    var label=PlayerTimeText.Format(false,false,preset);
    Check(label=="Exploration · "+NovaCore.Simulation.Time.SimulationSpeedPresets.Get(preset).Label.Replace("Simulation Speed: ",""),"HUD uses exact authoritative preset");
    Check(!label.Contains("Epoch",StringComparison.OrdinalIgnoreCase),"default HUD has no epoch");
}
Check(defaults.WindowMode==NovaCoreWindowMode.BorderlessFullscreen,"fullscreen default");
Refuses(defaults with{Version=2},"future version");
Refuses(defaults with{GameType="Flight"},"flight without a selected vehicle");
Refuses(defaults with{Vehicle="Saved craft"},"exploration craft substitution");
Refuses(defaults with{Location="Florida launch slab"},"exploration location substitution");
Refuses(defaults with{Resolution=(NovaCoreResolutionPreset)999},"unknown resolution");
Refuses(defaults with{WindowMode=(NovaCoreWindowMode)999},"unknown display mode");
Refuses(defaults with{TelemetryAnchor="outside"},"unbounded HUD anchor");
var construction=defaults with{GameType="Construction",Situation="Vehicle editor",Vehicle="New vehicle",Location="Florida launch slab"};construction.Validate();
var flight=construction with{GameType="Flight",Situation="Landed vehicle",Vehicle="Known craft"};flight.Validate();
Refuses(flight with{Situation="Solar overview"},"wrong physical starting situation");
Refuses(flight with{Vehicle="New vehicle"},"empty physical craft");
var root=Path.Combine(Path.GetTempPath(),"NovaCore-player-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var path=Path.Combine(root,"settings.json");
try{
    var saved=flight with{Telemetry=false,TimeAnchor="Bottom right"};saved.Save(path);
    Check(JsonSerializer.Deserialize<PlayerConfiguration>(File.ReadAllText(path))==saved,"settings exact round trip");
    var before=File.ReadAllBytes(path);try{(saved with{Version=99}).Save(path);}catch(InvalidDataException){}
    Check(File.ReadAllBytes(path).SequenceEqual(before),"invalid save preserves previous settings");
    defaults.Save(path);Check(JsonSerializer.Deserialize<PlayerConfiguration>(File.ReadAllText(path))==defaults,"atomic replacement");
    Check(Directory.EnumerateFiles(root).Count()==1,"no orphan transaction files");
    var layoutsPath=Path.Combine(root,"layouts.json");var layouts=PlayerHudLayouts.Load(layoutsPath);layouts.Layouts.Add("Flight",PlayerHudLayout.Capture(saved));layouts.Save(layoutsPath);
    var loaded=PlayerHudLayouts.Load(layoutsPath).Layouts["Flight"].Apply(defaults);
    Check(loaded.GameType==defaults.GameType&&loaded.Vehicle==defaults.Vehicle,"loading HUD cannot change spawn");
    Check(loaded.Telemetry==saved.Telemetry&&loaded.TimeAnchor==saved.TimeAnchor,"named HUD layout round trip");
}finally{foreach(var file in Directory.EnumerateFiles(root))File.Delete(file);Directory.Delete(root);}
Console.WriteLine($"PASS player configuration: {checks} checks");
var t=System.Diagnostics.Stopwatch.GetTimestamp();var frequency=System.Diagnostics.Stopwatch.Frequency;
var memory=new NovaCore.Interop.PlayerGpuMemoryReading(NovaCore.Interop.PlayerGpuMemoryStatus.Ready,1,t,frequency,DateTime.UtcNow,10000,
    1073741824,12369505812,17163091968,"RX test","id","luid",true,0x1002,0x73bf,0,2,"test");
Check(memory.IsLive&&memory.DisplayText.Contains("1.00 GiB")&&memory.DisplayText.Contains("11.52 GiB")&&memory.DisplayText.Contains("capacity 16.0 GiB"),"separate bytes to GiB and display rounding");
Check(!memory.DisplayText.Contains("UTC")&&!memory.DisplayText.Contains("Vulkan"),"implementation detail stays out of player text");
var stale=memory with{Timestamp=t-frequency*4};Check(!stale.IsLive&&stale.DisplayText.Contains("stale")&&!stale.DisplayText.Contains("11.52"),"stale values cannot appear live");
foreach(var state in new[]{NovaCore.Interop.PlayerGpuMemoryStatus.Unsupported,NovaCore.Interop.PlayerGpuMemoryStatus.Failed,NovaCore.Interop.PlayerGpuMemoryStatus.Stopped})
    Check(!(memory with{Status=state}).DisplayText.Contains("11.52"),"failure hides previous valid numbers");
Console.WriteLine($"PASS player configuration and GPU memory presentation: {checks} checks");
