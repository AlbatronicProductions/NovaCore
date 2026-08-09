namespace NovaCore.Simulation.Time;

/// <summary>One user-facing simulation-speed choice with a stable exact label.</summary>
public readonly record struct SimulationSpeedPreset(SimulationRate Rate, string Label);

/// <summary>Authoritative ordered normal-play simulation-speed table.</summary>
public static class SimulationSpeedPresets
{
    private static readonly SimulationSpeedPreset[] Values =
    [
        new(new(1,10),"Simulation Speed: 0.1x (Slow Motion)"),
        new(SimulationRate.One,"Simulation Speed: 1x (Realtime)"),
        new(new(2,1),"Simulation Speed: 2x"),
        new(new(4,1),"Simulation Speed: 4x"),
        new(new(10,1),"Simulation Speed: 10x"),
        new(new(30,1),"Simulation Speed: 30x"),
        new(new(120,1),"Simulation Speed: 120x"),
        new(new(600,1),"Simulation Speed: 600x"),
        new(new(1_200,1),"Simulation Speed: 1,200x"),
        new(new(3_600,1),"Simulation Speed: 3,600x"),
        new(new(14_400,1),"Simulation Speed: 14,400x"),
        new(new(86_400,1),"Simulation Speed: 86,400x"),
        new(new(604_800,1),"Simulation Speed: 604,800x"),
        new(new(2_592_000,1),"Simulation Speed: 2,592,000x"),
        new(new(7_776_000,1),"Simulation Speed: 7,776,000x")
    ];

    public static int Count => Values.Length;
    public static SimulationSpeedPreset Get(int index) => (uint)index < (uint)Values.Length ? Values[index] : throw new ArgumentOutOfRangeException(nameof(index));
    public static int IndexOf(SimulationRate rate)
    {
        for (var index=0;index<Values.Length;index++) if (Values[index].Rate==rate) return index;
        return -1;
    }
}
