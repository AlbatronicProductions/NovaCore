using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    internal readonly record struct CraftNativeSample(double Seconds,AssemblyMass Mass,Double3 Position,Double3 Before,Double3 After,
        DoubleQuaternion Orientation,AssemblySiteFrame Frame,Double3 ContactImpulse);
    private CraftNativeSample[]? craftDiagnosticSamples;
    private int craftDiagnosticCount;
    internal void CaptureCraftDiagnosticsForTest()=>craftDiagnosticSamples=new CraftNativeSample[256];
    internal ReadOnlySpan<CraftNativeSample> CraftDiagnosticsForTest=>craftDiagnosticSamples.AsSpan(0,craftDiagnosticCount);
    // Optional bounded observation only. Production never enables this capture;
    // there are no callbacks, file writes or new authority inside a solve.
}

