using System.Text.Json;

namespace NovaCore.ConstructionEditor;
internal sealed record PlayerHudLayout(bool Telemetry,bool GameTime,string TelemetryAnchor,string TimeAnchor)
{
    public static PlayerHudLayout Capture(PlayerConfiguration value)=>new(value.Telemetry,value.GameTime,value.TelemetryAnchor,value.TimeAnchor);
    public PlayerConfiguration Apply(PlayerConfiguration value)
    {
        var next=value with{Telemetry=Telemetry,GameTime=GameTime,TelemetryAnchor=TelemetryAnchor,TimeAnchor=TimeAnchor};next.Validate();return next;
    }
}
internal sealed record PlayerHudLayouts(int Version,Dictionary<string,PlayerHudLayout> Layouts)
{
    public static string FilePath=>Path.Combine(Path.GetDirectoryName(PlayerConfiguration.FilePath)!,"hud-layouts-v1.json");
    public static PlayerHudLayouts Load(string? path=null)
    {
        path??=FilePath;if(!File.Exists(path))return new(1,new(StringComparer.Ordinal));
        if(new FileInfo(path).Length>65536)throw new InvalidDataException("HUD layout library exceeds its size limit.");
        var value=JsonSerializer.Deserialize<PlayerHudLayouts>(File.ReadAllText(path))??throw new InvalidDataException("HUD layout library is empty.");
        value.Validate();return value;
    }
    private void Validate()
    {
        if(Version!=1||Layouts is null||Layouts.Count>32)throw new InvalidDataException("Unsupported HUD layout library.");
        foreach(var entry in Layouts){ValidateName(entry.Key);if(entry.Value is null)throw new InvalidDataException("Missing HUD layout.");entry.Value.Apply(new());}
    }
    public static void ValidateName(string name){if(string.IsNullOrWhiteSpace(name)||name.Length>64||name!=name.Trim()||name.Any(char.IsControl))throw new InvalidDataException("Use a layout name of 1–64 visible characters.");}
    public void Save(string? path=null)
    {
        Validate();path??=FilePath;Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
