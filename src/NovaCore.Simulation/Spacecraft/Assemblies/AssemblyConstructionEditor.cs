using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold design validation/inspection. These are immutable facts, never live simulation state.</summary>
internal sealed class ConstructionEditorDocument
{
    internal CompiledConstructionDesign Design {get;}
    private readonly ConstructionFuelNetwork? fuel;
    private readonly ConstructionPowerNetwork? power;
    internal ConstructionFuelNetwork Fuel=>fuel??throw new InvalidDataException("CraftDocument services require CompiledCraft.");
    internal ConstructionPowerNetwork Power=>power??throw new InvalidDataException("CraftDocument services require CompiledCraft.");
    private ConstructionEditorDocument(CompiledConstructionDesign design,ConstructionFuelNetwork? fuel,ConstructionPowerNetwork? power)
    {Design=design;this.fuel=fuel;this.power=power;}
    internal static ConstructionEditorDocument Compile(AssemblyDefinitionCatalog catalog,ConstructionDesignData data)
    {
        var design=CompiledConstructionDesign.Compile(catalog,data);
        if(data.Schema==CompiledConstructionDesign.CraftSchema)return new(design,null,null);
        var fuel=ConstructionFuelNetwork.Compile(design);
        return new(design,fuel,ConstructionPowerNetwork.Compile(fuel));
    }
}

/// <summary>One owner thread, isolated draft, revision-bound ghost and atomic validation before install.</summary>
internal sealed partial class ConstructionEditorSession : IDisposable
{
    private readonly int thread=Environment.CurrentManagedThreadId;
    private bool disposed;
    internal AssemblyDefinitionCatalog Catalog {get;}
    internal long Revision {get;private set;}
    internal ConstructionEditorDocument? Current {get;private set;}
    internal ConstructionEditorDocument? Preview {get;private set;}
    private readonly PartCompatibilityEvaluator? compatibility;
    internal ConstructionEditorSession(AssemblyDefinitionCatalog catalog,PartCompatibilityEvaluator? compatibility=null)
    {
        Catalog=catalog;
        // Every player session enforces the same physical admission boundary,
        // including load, history and recovery. An omitted cache is not a bypass.
        this.compatibility=compatibility??(catalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema?new PartCompatibilityEvaluator(catalog):null);
    }
    private void Verify(long expected)
    {
        Require(Environment.CurrentManagedThreadId==thread&&!disposed,"Retired or foreign-thread editor session.");
        Require(expected==Revision,"Stale editor revision.");
    }
    private void Install(ConstructionEditorDocument? document,bool preview=false,bool recordHistory=true)
    {
        var next=checked(Revision+1);
        if(preview)Preview=document;else{
            if(recordHistory&&Current?.Design.Digest!=document?.Design.Digest)RecordEdit();
            Current=document;Preview=null;
        }
        Revision=next;
    }
    private ConstructionEditorDocument Document()=>Current??throw new InvalidDataException("Place a root first.");
    private ConstructionEditorDocument Compile(ConstructionDesignData data,bool edit=true)
    {
        if(edit)data=data with {Revision=checked(data.Revision+1)};
        var result=ConstructionEditorDocument.Compile(Catalog,data with {DependencyDigest=Catalog.DependencyDigest(data.Instances)});
        if(edit&&PlayerDocument&&Current is not null)ValidateEditPolicies(result.Design.Data);
        compatibility?.RequireFit(result.Design);
        return result;
    }
    private static ConstructionConfiguration EmptyConfiguration(string id,PartDefinitionData definition)=>new(id,
        definition.Stores.Select(s=>new ConstructionStoreInitial(s.Id,0,true)).ToImmutableArray(),
        definition.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,0,true)).ToImmutableArray());
    internal void Clear(long expected,bool discardUnsaved=false){Verify(expected);ProtectUnsaved(discardUnsaved);Install(null);savedDigest=null;}
    internal void PreviewRoot(long expected,DefinitionReference definition,string instance,string designId,AssemblyPose pose,string? playerName=null)
    {
        Verify(expected);Require(Current is null,"Clear the existing design before placing a root.");
        var part=Catalog.Resolve(definition);var placed=new PartInstanceData(instance,0,definition,pose);
        var data=new ConstructionDesignData(PlayerDocument?CompiledConstructionDesign.CraftSchema:CompiledConstructionDesign.Schema,designId,1,Catalog.DependencyDigest([placed]),instance,
            part.Construction!.Command?instance:null,[placed],[],[],[EmptyConfiguration(instance,part)],[],[],PlayerDocument?new(playerName??designId,1):null);
        Install(Compile(data,false),true);
    }
    internal void PreviewAttach(long expected,DefinitionReference definition,string instance,string parent,string parentInterface,string childInterface,ConstructionConnection connection)
    {
        Verify(expected);var document=Document();var data=document.Design.Data;var owner=document.Design.Parts[document.Design.Index(parent)];
        var part=Catalog.Resolve(definition);
        var pose=CompiledConstructionDesign.Snap(owner.Instance.Pose,Endpoint(owner.Definition,parentInterface),Endpoint(part,childInterface));
        var placed=new PartInstanceData(instance,data.Instances.Length,definition,pose);
        Install(Compile(data with {Instances=data.Instances.Add(placed),
            Connections=data.Connections.Add(new(parent,parentInterface,instance,childInterface,connection)),
            Configuration=data.Configuration.Add(EmptyConfiguration(instance,part))}),true);
    }
    private static AttachmentData Endpoint(PartDefinitionData part,string id)=>part.Attachments.SingleOrDefault(a=>a.Id==id)??throw new InvalidDataException("Unknown attachment interface.");
    internal void AcceptPreview(long expected)
    {Verify(expected);Require(Preview is not null,"No validated placement preview.");Install(Preview);}
    internal void CancelPreview(long expected){Verify(expected);var next=checked(Revision+1);Preview=null;Revision=next;}
    internal void Load(long expected,ReadOnlySpan<byte> bytes,bool discardUnsaved=false)
    {
        Verify(expected);ProtectUnsaved(discardUnsaved);
        var design=PlayerDocument?CompiledConstructionDesign.LoadCraft(Catalog,bytes):CompiledConstructionDesign.Load(Catalog,bytes);
        compatibility?.RequireFit(design);
        Install(ConstructionEditorDocument.Compile(Catalog,design.Data));
        savedDigest=design.Digest;
    }
    internal byte[] Save(long expected){Verify(expected);return Document().Design.Save();}
    internal void SetConfiguration(long expected,ConstructionConfiguration configuration)
        =>SetConfigurations(expected,[configuration]);
    internal void SetControl(long expected,string? part)
    {Verify(expected);Install(Compile(Document().Design.Data with {ControlPart=part}));}
    internal void SetMetadata(long expected,ImmutableArray<ConstructionSymmetry> symmetry,ImmutableArray<ConstructionAction> actions,ImmutableArray<ConstructionServiceLink> links)
    {Verify(expected);Install(Compile(Document().Design.Data with {Symmetry=symmetry,Actions=actions,ServiceLinks=links}));}
    internal void SetConnection(long expected,ConstructionConnection connection)
    {
        Verify(expected);Require(connection is not null,"Missing connection edit.");var data=Document().Design.Data;var index=data.Connections.FindIndex(c=>c.Construction!.Id==connection.Id);
        Require(index>=0,"Unknown connection.");Install(Compile(data with {Connections=data.Connections.SetItem(index,data.Connections[index] with {Construction=connection})}));
    }
    private static HashSet<string> Descendants(CompiledConstructionDesign design,string root)
    {
        _=design.Index(root);var result=new HashSet<string>(StringComparer.Ordinal){root};
        for(var pass=0;pass<design.Parts.Length;pass++)foreach(var edge in design.Data.Connections)if(result.Contains(edge.Parent))result.Add(edge.Child);
        return result;
    }
    internal void Remove(long expected,string part)
    {
        Verify(expected);var design=Document().Design;if(part==design.Data.Root){Install(null);return;}
        var remove=Descendants(design,part);var data=design.Data;
        if(PlayerDocument){bool expanded;do{expanded=false;foreach(var group in data.Symmetry)
            if(group.Members.Any(m=>remove.Contains(m.Part)))foreach(var m in group.Members)
                foreach(var id in Descendants(design,m.Part))expanded|=remove.Add(id);
        }while(expanded);}
        var edges=data.Connections.Where(c=>!remove.Contains(c.Parent)&&!remove.Contains(c.Child)).ToImmutableArray();
        Install(Compile(data with {
            Instances=data.Instances.Where(p=>!remove.Contains(p.Id)).Select((p,i)=>p with {Order=i}).ToImmutableArray(),Connections=edges,
            Configuration=data.Configuration.Where(c=>!remove.Contains(c.Part)).ToImmutableArray(),
            ControlPart=data.ControlPart is {} control&&remove.Contains(control)?null:data.ControlPart,
            ServiceLinks=data.ServiceLinks.Where(l=>!remove.Contains(l.FromPart)&&!remove.Contains(l.ToPart)).ToImmutableArray(),
            Symmetry=data.Symmetry.Where(g=>g.Members.All(m=>!remove.Contains(m.Part))).ToImmutableArray(),
            Actions=data.Actions.Where(a=>!remove.Contains(a.Part)&&(a.Connection is null||edges.Any(e=>e.Construction!.Id==a.Connection))).ToImmutableArray()}));
    }
    internal void PreviewReconnect(long expected,string child,string parent,string parentInterface,string childInterface,ConstructionConnection connection)
    {
        Verify(expected);var design=Document().Design;Require(child!=design.Data.Root,"The root has no incoming connection.");
        var subtree=Descendants(design,child);Require(!subtree.Contains(parent),"Cannot reconnect into own subtree.");
        var moving=design.Parts[design.Index(child)];var target=design.Parts[design.Index(parent)];
        var snapped=CompiledConstructionDesign.Snap(target.Instance.Pose,Endpoint(target.Definition,parentInterface),Endpoint(moving.Definition,childInterface));
        var rotation=snapped.Rotation*moving.Instance.Pose.Rotation.Transpose();
        var transform=new AssemblyPose(snapped.Position-rotation.Apply(moving.Instance.Pose.Position),rotation);
        var data=design.Data;
        Install(Compile(data with {Instances=data.Instances.Select(p=>subtree.Contains(p.Id)?p with {Pose=transform.Then(p.Pose)}:p).ToImmutableArray(),
            Connections=data.Connections.Where(c=>c.Child!=child).Append(new(parent,parentInterface,child,childInterface,connection)).ToImmutableArray()}),true);
    }
    internal void Rotate(long expected,Matrix3 rotation)
    {
        Verify(expected);Require(rotation.Rigid,"Invalid editor rotation.");var data=Document().Design.Data;
        var origin=data.Instances.Single(p=>p.Id==data.Root).Pose.Position;
        var transform=new AssemblyPose(origin-rotation.Apply(origin),rotation);
        Install(Compile(data with {Instances=data.Instances.Select(p=>p with {Pose=transform.Then(p.Pose)}).ToImmutableArray()}));
    }
    public void Dispose()
    {
        Require(Environment.CurrentManagedThreadId==thread,"Foreign-thread editor disposal.");
        if(disposed)return;disposed=true;Current=null;Preview=null;ResetHistory();
    }
}
