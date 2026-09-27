using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft;

internal sealed partial class SpacecraftStateStore
{
    private ConstructionRuntimeBinding?[]? _constructionBindings;
    private ConstructionRuntimeState?[]? _constructionStates;
    private bool IsConstruction(int index)=>_constructionBindings is not null&&_constructionBindings[index] is not null;
    internal static SpacecraftStateStore CreateConstruction(ConstructionRuntimeBinding binding,ReferenceFrameGraph graph)
    {
        var valid=binding.Physical is {} physical?physical.Site.AdmitsGraph(graph,binding.Spacecraft):
            graph.RootCount==1&&graph.TryGetNode(binding.Spacecraft.CarrierFrame,out var root)&&root.ParentId is null&&root.Kind==ReferenceFrameKind.Ecl&&
            graph.TryGetNode(binding.Spacecraft.BodyFrame,out var body)&&body.ParentId==root.Id;
        if(!valid)throw new InvalidDataException("Invalid construction frames.");
        // Construction is endpoint-only: all legacy attitude access and
        // propagation is refused by IsInstantaneous. Register its inert slot
        // with identity; never re-normalize the authoritative saved physical
        // orientation through an unrelated propagation-segment constructor.
        if(SpacecraftAttitudeState.TryCreate(binding.Spacecraft.Id,binding.Initial.Epoch,DoubleQuaternion.Identity,Double3.Zero,
            SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1,out var attitude)!=SpacecraftAttitudeEvaluationStatus.Success||
            !TryCreate([binding.Spacecraft],[attitude],out var store,out _)||store is null)throw new InvalidDataException("Invalid construction registration.");
        store._constructionBindings=[binding];store._constructionStates=[binding.Initial];
        // All fallible frame/slot preparation precedes the permanent, atomic lifetime claim.
        if(!binding.TryClaimRegistration())throw new InvalidDataException("Construction binding already registered.");
        return store;
    }
    internal bool TryGetConstruction(SpacecraftId id,out ConstructionRuntimeBinding? binding,out ConstructionRuntimeState? state)
    {
        if(TryGetIndex(id,out var index)&&IsConstruction(index)){binding=_constructionBindings![index];state=_constructionStates![index];return true;}
        binding=null;state=null;return false;
    }
    internal bool TryPrepareConstructionSlot(ConstructionRuntimeBinding binding,ConstructionRuntimeState expected,out int index)=>
        TryGetIndex(binding.Spacecraft.Id,out index)&&IsConstruction(index)&&ReferenceEquals(_constructionBindings![index],binding)&&ReferenceEquals(_constructionStates![index],expected);
    internal void InstallConstruction(int index,ConstructionRuntimeState successor)=>_constructionStates![index]=successor;
}
