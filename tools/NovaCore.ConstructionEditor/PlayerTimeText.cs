using NovaCore.Simulation.Time;

namespace NovaCore.ConstructionEditor;

internal static class PlayerTimeText
{
    internal static string Format(bool physical,bool paused,int explorationPreset)
    {
        var mode=physical?"Flight":"Exploration";
        var rate=physical?"1×":SimulationSpeedPresets.Get(explorationPreset).Label.Replace("Simulation Speed: ","");
        return paused?$"{mode} · Paused":$"{mode} · {rate}";
    }
}
