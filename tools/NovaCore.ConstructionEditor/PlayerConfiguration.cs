using System.Text.Json;
using NovaCore.Launcher;

namespace NovaCore.ConstructionEditor;

// Player preferences are separate from diagnostic launcher policy. No terrain
// heights, solver settings or renderer quality substitutions belong here.
internal sealed record PlayerConfiguration
{
    public int Version { get; init; } = 1;
    public string System { get; init; } = "Solar System";
    public string GameType { get; init; } = "Exploration";
    public string Situation { get; init; } = "Solar overview";
    public string Vehicle { get; init; } = "None";
    public string Location { get; init; } = "Solar System";
    public NovaCoreWindowMode WindowMode { get; init; } = NovaCoreWindowMode.BorderlessFullscreen;
    public NovaCoreResolutionPreset Resolution { get; init; } = NovaCoreResolutionPreset.NativeDesktop;
    public bool Telemetry { get; init; } = true;
    public bool GameTime { get; init; } = true;
    public string TelemetryAnchor { get; init; } = "Bottom left";
    public string TimeAnchor { get; init; } = "Top right";
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaCore","Player","settings-v1.json");
    public void Validate()
    {
        if(Version!=1)throw new InvalidDataException("Unsupported player settings version. Restore a version 1 configuration or choose defaults.");
        if(System!="Solar System" || !Enum.IsDefined(WindowMode)||!Enum.IsDefined(Resolution))throw new InvalidDataException("Unsupported system or display setting.");
        if(GameType=="Exploration") { if(Situation!="Solar overview"||Vehicle!="None"||Location!="Solar System")throw new InvalidDataException("Exploration starts at Solar overview with no vessel."); }
        else if(GameType=="Construction") { if(Situation!="Vehicle editor"||Location!="Florida launch slab")throw new InvalidDataException("Construction uses the vehicle editor and the canonical Florida launch slab."); }
        else if(GameType=="Flight") { if(Situation!="Landed vehicle"||Location!="Florida launch slab"||Vehicle is "None" or "New vehicle")throw new InvalidDataException("Flight requires a saved, launch-admissible vehicle at Florida."); }
        else throw new InvalidDataException("Unsupported game type.");
        if(!Anchors.Contains(TelemetryAnchor)||!Anchors.Contains(TimeAnchor))throw new InvalidDataException("Unsupported HUD anchor.");
    }
    public static readonly string[] Anchors=["Top left","Top right","Bottom left","Bottom right"];
    public static PlayerConfiguration Load()
    {
        if(!File.Exists(FilePath))return new();
        if(new FileInfo(FilePath).Length>65536)throw new InvalidDataException("Player settings exceed their size limit.");
        var value=JsonSerializer.Deserialize<PlayerConfiguration>(File.ReadAllText(FilePath))??throw new InvalidDataException("Player settings are empty.");value.Validate();return value;
    }
    public void Save(string? path=null)
    {
        Validate();path??=FilePath;Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
