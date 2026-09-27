using System.Collections.Immutable;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed partial class ConstructionEditorSession
{
    internal const int MaximumHistoryCount=64,MaximumHistoryPayloadBytes=16*1024*1024;
    private ImmutableArray<byte[]?> undo=[],redo=[];
    private string? savedDigest;
    internal bool PlayerDocument=>Catalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema;
    internal bool Dirty=>Current?.Design.Digest!=savedDigest;
    internal bool HasUnsavedWork=>Dirty||Preview is not null;
    internal int UndoCount=>undo.Length;
    internal int RedoCount=>redo.Length;
    internal long HistoryEvictions {get;private set;}
    internal int HistoryPayloadBytes=>undo.Concat(redo).Sum(b=>b?.Length??0);
    private void ProtectUnsaved(bool discard)
    {Require(!PlayerDocument||!HasUnsavedWork||discard,"Unsaved work requires Save, Discard or Cancel.");}
    private void ResetHistory(){undo=[];redo=[];}
    private byte[]? Snapshot()=>Current?.Design.Save();
    private void RecordEdit()
    {
        // Capture before mutation. History retains bounded bytes, not a second graph owner.
        var plan=BoundHistory(undo.Add(Snapshot()),[]);InstallHistory(plan);
    }
    private (ImmutableArray<byte[]?> Undo,ImmutableArray<byte[]?> Redo,long Evictions) BoundHistory(ImmutableArray<byte[]?> nextUndo,ImmutableArray<byte[]?> nextRedo)
    {
        var evictions=HistoryEvictions;
        while(nextUndo.Length+nextRedo.Length>MaximumHistoryCount||nextUndo.Concat(nextRedo).Sum(b=>b?.Length??0)>MaximumHistoryPayloadBytes){
            // Index zero is farthest in each direction. Prefer the greater command
            // distance; a tie evicts undo deterministically. Preserve nearby navigation.
            if(nextUndo.Length>=nextRedo.Length&&nextUndo.Length>0)nextUndo=nextUndo.RemoveAt(0);else nextRedo=nextRedo.RemoveAt(0);
            evictions=checked(evictions+1);
        }
        return(nextUndo,nextRedo,evictions);
    }
    private void InstallHistory((ImmutableArray<byte[]?> Undo,ImmutableArray<byte[]?> Redo,long Evictions) plan)
    {undo=plan.Undo;redo=plan.Redo;HistoryEvictions=plan.Evictions;}
    private ConstructionEditorDocument? ReadSnapshot(byte[]? bytes)
    {
        if(bytes is null)return null;
        var design=PlayerDocument?CompiledConstructionDesign.LoadCraft(Catalog,bytes):CompiledConstructionDesign.Load(Catalog,bytes);
        compatibility?.RequireFit(design);
        return ConstructionEditorDocument.Compile(Catalog,design.Data);
    }
    internal void Undo(long expected)=>NavigateHistory(expected,true);
    internal void Redo(long expected)=>NavigateHistory(expected,false);
    private void NavigateHistory(long expected,bool backwards)
    {
        Verify(expected);var from=backwards?undo:redo;var to=backwards?redo:undo;
        Require(from.Length>0,"No history entry.");
        var next=checked(Revision+1);var destination=ReadSnapshot(from[^1]);var previous=Snapshot();
        // Destination validation and previous-state serialization both precede mutation.
        from=from.RemoveAt(from.Length-1);to=to.Add(previous);
        var plan=backwards?BoundHistory(from,to):BoundHistory(to,from);
        InstallHistory(plan);Current=destination;Preview=null;Revision=next;
    }
    internal void PreviewEdit(long expected,ConstructionDesignData candidate)
    {
        Verify(expected);Require(candidate is not null&&Current is not null&&candidate.Id==Current.Design.Data.Id,
            "Edit requires the current document identity.");
        Install(Compile(candidate with {Revision=Current.Design.Data.Revision}),true);
    }
    internal void SetName(long expected,string name)
    {
        Verify(expected);Require(PlayerDocument,"Player name belongs to CraftDocument.");
        var data=Document().Design.Data;Install(Compile(data with {Craft=data.Craft! with {Name=name}}));
    }
    internal void SetConfigurations(long expected,ImmutableArray<ConstructionConfiguration> configurations)
    {
        Verify(expected);Unique(configurations,c=>c.Part,"configuration edit owners");var data=Document().Design.Data;
        if(PlayerDocument)foreach(var group in data.Symmetry)
            if(group.Members.Any(m=>configurations.Any(c=>c.Part==m.Part)))
                Require(group.Members.All(m=>configurations.Any(c=>c.Part==m.Part)),"Group configuration requires every member in the operation.");
        var next=data.Configuration;
        foreach(var configuration in configurations){var i=next.FindIndex(c=>c.Part==configuration.Part);
            Require(i>=0,"Unknown configuration owner.");next=next.SetItem(i,configuration);}
        Install(Compile(data with {Configuration=next}));
    }
    private void ValidateEditPolicies(ConstructionDesignData next)
    {
        var prior=Document().Design.Data;var changed=new HashSet<string>(StringComparer.Ordinal);
        foreach(var instance in next.Instances){var old=prior.Instances.SingleOrDefault(i=>i.Id==instance.Id);
            Require(old is null||old.Definition==instance.Definition,"Existing instance definition is pinned; remove/place or explicitly migrate.");}
        foreach(var config in next.Configuration){
            var before=prior.Configuration.SingleOrDefault(c=>c.Part==config.Part);if(before is null)continue;
            if(AssemblyJson.Digest(before)==AssemblyJson.Digest(config))continue;
            changed.Add(config.Part);var instance=next.Instances.Single(i=>i.Id==config.Part);
            var policy=Catalog.Resolve(instance.Definition).Standard!.Configuration;
            foreach(var s in config.Stores){var old=before.Stores.SingleOrDefault(t=>t.Store==s.Store);
                Require(old is not null&&(policy.FillStores||s.QuantityKg==old.QuantityKg)&&(policy.EnableStores||s.Enabled==old.Enabled),"Store configuration policy refuses edit.");}
            foreach(var e in config.Electrical){var old=before.Electrical.SingleOrDefault(t=>t.Module==e.Module);
                Require(old is not null&&(policy.ChargeBattery||e.ChargeJ==old.ChargeJ)&&(policy.EnableElectrical||e.Enabled==old.Enabled),"Electrical configuration policy refuses edit.");}
        }
        foreach(var group in prior.Symmetry){
            var target=next.Symmetry.SingleOrDefault(g=>g.Id==group.Id);
            if(target is null){Require(group.Members.All(m=>next.Instances.All(p=>p.Id!=m.Part)),"Symmetry unlink is excluded; remove the complete group.");continue;}
            Require(group.Members.Select(m=>m.Part).SequenceEqual(target.Members.Select(m=>m.Part))&&
                group.Placement!.Definition==target.Placement!.Definition,"Existing symmetry membership/definition cannot be rewritten.");
            if(group.Members.Any(m=>changed.Contains(m.Part))){
                var configurations=target.Members.Select(m=>next.Configuration.Single(c=>c.Part==m.Part) with {Part="group"}).ToArray();
                var hash=AssemblyJson.Digest(configurations[0]);
                Require(configurations.All(c=>AssemblyJson.Digest(c)==hash),"Group configuration must apply the same settings atomically.");
            }
        }
        foreach(var group in next.Symmetry.Where(g=>prior.Symmetry.All(p=>p.Id!=g.Id)))
            Require(group.Members.All(m=>prior.Instances.All(p=>p.Id!=m.Part)),"Symmetry is created with placement, not retrofitted onto existing members.");
    }
    internal void SaveTo(long expected,string path)
    {
        Verify(expected);Require(PlayerDocument&&Preview is null,"Commit or cancel the preview before saving.");
        var document=Document();CraftDocumentStore.Save(path,Catalog,document.Design.Save());
        savedDigest=document.Design.Digest;
    }
    internal void LoadFrom(long expected,string path,bool discardUnsaved=false)
    {Verify(expected);ProtectUnsaved(discardUnsaved);Load(expected,CraftDocumentStore.Read(path),discardUnsaved);}
    internal bool TryClose(long expected,bool discardUnsaved=false)
    {
        Verify(expected);if(PlayerDocument&&HasUnsavedWork&&!discardUnsaved)return false;
        Dispose();return true;
    }
    internal void SaveRecovery(long expected,string path)
    {
        Verify(expected);Require(PlayerDocument,"Recovery requires CraftDocument.");
        CraftDocumentStore.SaveRecovery(path,Catalog,Snapshot(),savedDigest);
    }
    internal void RestoreRecovery(long expected,string path,bool discardUnsaved=false)
    {
        Verify(expected);ProtectUnsaved(discardUnsaved);
        var recovered=CraftDocumentStore.ReadRecovery(path,Catalog);
        if(recovered.Document is not null)compatibility?.RequireFit(recovered.Document.Design);
        Install(recovered.Document);savedDigest=recovered.SavedDigest;
    }
}
