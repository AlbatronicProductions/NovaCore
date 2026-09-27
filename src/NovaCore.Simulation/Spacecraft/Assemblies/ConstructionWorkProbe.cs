using System.Diagnostics;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

// Opt-in qualification observer. No callbacks, allocation or clock access when
// disabled; never participates in a physical decision or canonical state.
internal sealed class ConstructionWorkProbe : IDisposable
{
    internal enum Stage { Resources, Clearance, FreeFlight, World, SlicePreparation, Terrain, Solver, Publication }
    [ThreadStatic] private static ConstructionWorkProbe? current;
    private readonly ConstructionWorkProbe? prior;
    private readonly long[] elapsed=new long[8];
    internal int Intervals, Slices, Creates, Disposes, Tiles, Meshes;
    internal ConstructionWorkProbe(){prior=current;current=this;}
    internal static ConstructionWorkProbe? Current=>current;
    internal static Stamp Time(Stage stage)=>new(current,stage);
    internal readonly struct Stamp : IDisposable
    {
        private readonly ConstructionWorkProbe? owner;
        private readonly Stage stage;
        private readonly long start;
        internal Stamp(ConstructionWorkProbe? owner,Stage stage){this.owner=owner;this.stage=stage;start=owner is null?0:Stopwatch.GetTimestamp();}
        public void Dispose(){if(owner is not null)owner.elapsed[(int)stage]+=Stopwatch.GetTimestamp()-start;}
    }
    internal sealed record Sample(int Intervals,int Slices,int Creates,int Disposes,int Tiles,int Meshes,
        double ResourcesMs,double ClearanceMs,double FreeFlightMs,double WorldMs,double SlicePreparationMs,double TerrainMs,double SolverMs,double PublicationMs);
    internal Sample Read()=>new(Intervals,Slices,Creates,Disposes,Tiles,Meshes,
        Ms(0),Ms(1),Ms(2),Ms(3),Ms(4),Ms(5),Ms(6),Ms(7));
    private double Ms(int index)=>elapsed[index]*1000d/Stopwatch.Frequency;
    public void Dispose(){current=prior;}
}
