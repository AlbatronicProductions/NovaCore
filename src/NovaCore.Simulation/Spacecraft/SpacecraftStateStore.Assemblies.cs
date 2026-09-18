using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft;

internal sealed partial class SpacecraftStateStore
{
    // Optional cold arrays: old profiles allocate no assembly storage. These are
    // the only canonical assembly values; engine preparations are private copies.
    private AssemblyLaunch?[]? _assemblyLaunches;
    private AssemblyRuntimeState[]? _assemblyStates;
    private bool IsAssembly(int index)=>_assemblyLaunches is not null&&_assemblyLaunches[index] is not null;
    internal static SpacecraftStateStore CreateAssemblies(ReadOnlySpan<AssemblyLaunch> launches,ReferenceFrameGraph graph)
    {
        if(launches.Length==0)throw new InvalidDataException("A launch is required.");
        var definitions=new SpacecraftDefinition[launches.Length];var attitudes=new SpacecraftAttitudeState[launches.Length];
        var keys=new HashSet<string>(StringComparer.Ordinal);
        for(var i=0;i<launches.Length;i++)
        {
            var l=launches[i];definitions[i]=l.Spacecraft;
            var validFrame=l.Site is {} site ? site.AdmitsGraph(graph,l.Spacecraft) :
                graph.RootCount==1&&graph.TryGetNode(l.Spacecraft.CarrierFrame,out var root)&&root.ParentId is null&&root.Kind==ReferenceFrameKind.Ecl&&
                graph.TryGetNode(l.Spacecraft.BodyFrame,out var body)&&body.ParentId==root.Id;
            if(!keys.Add(l.LaunchId)||!validFrame)
                throw new InvalidDataException("Invalid launch namespace or assembly material frame.");
            if(SpacecraftAttitudeState.TryCreate(l.Spacecraft.Id,l.Initial.Epoch,l.Initial.Motion.BodyToWorld,l.Initial.Motion.AngularVelocityBody,
                SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1,out attitudes[i])!=SpacecraftAttitudeEvaluationStatus.Success)
                throw new InvalidDataException("Invalid initial orientation.");
        }
        if(!TryCreate(definitions,attitudes,out var store,out _)||store is null)throw new InvalidDataException("Invalid canonical spacecraft registration.");
        store._assemblyLaunches=new AssemblyLaunch?[launches.Length];store._assemblyStates=new AssemblyRuntimeState[launches.Length];
        for(var i=0;i<launches.Length;i++){store._assemblyLaunches[i]=launches[i];store._assemblyStates[i]=launches[i].Initial;}
        return store;
    }
    internal bool TryGetAssembly(SpacecraftId id,out AssemblyLaunch? launch,out AssemblyRuntimeState state)
    {
        if(TryGetIndex(id,out var index)&&IsAssembly(index)){launch=_assemblyLaunches![index];state=_assemblyStates![index];return true;}
        launch=null;state=default;return false;
    }
    internal bool TryPrepareAssemblySlot(AssemblyLaunch launch,in AssemblyRuntimeState expected,out int index)=>
        TryGetIndex(launch.Spacecraft.Id,out index)&&IsAssembly(index)&&ReferenceEquals(_assemblyLaunches![index],launch)&&_assemblyStates![index]==expected;
    internal void InstallAssembly(int index,in AssemblyRuntimeState state)=>_assemblyStates![index]=state;
}
