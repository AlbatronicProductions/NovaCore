using System.Collections.Immutable;
using System.Text.Json.Serialization;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record ConstructionConnection(string Id, bool Detachable, ConstructionService Services,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] int? ClockDegrees=null);
internal sealed record ConstructionServiceLink(string Id,string FromPart,string FromInterface,string ToPart,string ToInterface,
    ConstructionService Services,bool Bidirectional);
internal sealed record ConstructionStoreInitial(string Store,double QuantityKg,bool Enabled);
internal sealed record ConstructionElectricalInitial(string Module,double ChargeJ,bool Enabled);
internal sealed record ConstructionConfiguration(string Part,ImmutableArray<ConstructionStoreInitial> Stores,ImmutableArray<ConstructionElectricalInitial> Electrical);
internal sealed record ConstructionSymmetryMember(string Part,AssemblyPose FromBase);
internal sealed record ConstructionSymmetry(string Id,string BasePart,ImmutableArray<ConstructionSymmetryMember> Members,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] AuthoredSocketPlacement? Placement=null);
internal sealed record AuthoredSocketPlacement(string Host,string SocketGroup,string Anchor,int Count,
    ImmutableArray<string> Sockets,AssemblyPose Axis,DefinitionReference Definition);
internal sealed record CraftDocumentMetadata(string Name,uint ActionVersion);
internal sealed record ConstructionAction(string Id,int Stage,int Order,string Part,string? Consumer,string? Connection);
internal sealed record ConstructionDesignData(string Schema,string Id,uint Revision,string DependencyDigest,string Root,string? ControlPart,
    ImmutableArray<PartInstanceData> Instances,ImmutableArray<StructuralEdgeData> Connections,
    ImmutableArray<ConstructionServiceLink> ServiceLinks,ImmutableArray<ConstructionConfiguration> Configuration,
    ImmutableArray<ConstructionSymmetry> Symmetry,ImmutableArray<ConstructionAction> Actions,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] CraftDocumentMetadata? Craft=null);
internal readonly record struct CompiledConstructionConnection(string Id,int Parent,int Child,string ParentInterface,string ChildInterface,
    bool Detachable,ConstructionService Services);
internal readonly record struct CompiledServiceEdge(string Id,int Supply,int Receiver,string SupplyInterface,string ReceiverInterface,ConstructionService Services,bool Bidirectional);

/// <summary>Immutable rooted construction graph; no physical separation or dynamic state.</summary>
internal sealed partial class CompiledConstructionDesign
{
    internal const string Schema="novacore.vehicle-design/1";
    internal const string CraftSchema="novacore.craft-document/1";
    internal const int MaximumDocumentBytes=4_000_000;
    internal const int MaximumParts=1024;
    internal AssemblyDefinitionCatalog Catalog {get;}
    internal ConstructionDesignData Data {get;}
    internal string Digest {get;}
    internal ImmutableArray<CompiledPart> Parts {get;}
    internal ImmutableArray<CompiledConstructionConnection> Connections {get;}
    internal ImmutableArray<CompiledServiceEdge> ServiceEdges {get;}
    internal int RootIndex {get;}
    internal int ControlIndex {get;}
    private readonly AssemblyMass? legacyDryMass;
    internal AssemblyMass DryMass=>legacyDryMass??throw new InvalidDataException("CraftDocument physical preparation requires CompiledCraft.");
    private readonly ImmutableDictionary<string,int> indices;
    private CompiledConstructionDesign(AssemblyDefinitionCatalog catalog,ConstructionDesignData data,ImmutableArray<CompiledPart> parts,
        ImmutableArray<CompiledConstructionConnection> connections,ImmutableArray<CompiledServiceEdge> links)
    {
        // One shared ingress/egress contract, including escaped identifiers. A draft must be reloadable.
        var bytes=AssemblyJson.Write(data);Require(bytes.Length<=MaximumDocumentBytes,"Bounded document size exceeded.");
        Catalog=catalog;Data=data;Parts=parts;Connections=connections;ServiceEdges=links;
        Digest=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        indices=parts.Select((p,i)=>(p.Instance.Id,i)).ToImmutableDictionary(x=>x.Id,x=>x.i,StringComparer.Ordinal);
        RootIndex=indices[data.Root];ControlIndex=data.ControlPart is null?-1:indices[data.ControlPart];
        if(data.Schema==Schema)legacyDryMass=Aggregate(parts.Select(p=>new MassRegionData(p.Instance.Id,p.Definition.DryMassKg,p.Com,p.InertiaAtOwnCom)));
    }
    internal int Index(string id)=>Identifier(id)&&indices.TryGetValue(id,out var index)?index:throw new InvalidDataException("Unresolved part instance.");
    internal byte[] Save()=>AssemblyJson.Write(Data);
    internal static CompiledConstructionDesign Load(AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var data=AssemblyJson.Read<ConstructionDesignData>(bytes,MaximumDocumentBytes);
        Require(data.Schema==Schema,"Legacy vehicle-design schema required.");
        RejectLegacyCraftFields(bytes);
        return Compile(catalog,data);
    }
    internal static CompiledConstructionDesign Compile(AssemblyDefinitionCatalog catalog,ConstructionDesignData input)
    {
        Require(input is not null&&(input.Schema==Schema||input.Schema==CraftSchema)&&Identifier(input.Id)&&input.Revision>0,"Invalid design identity/schema.");
        var craft=input.Schema==CraftSchema;
        Require(craft?catalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema&&input.Craft is not null:
            catalog.Data.Schema==AssemblyDefinitionCatalog.Schema&&input.Craft is null,"CraftDocument metadata/catalog schema mismatch; explicit migration required.");
        if(craft)Require(!string.IsNullOrWhiteSpace(input.Craft!.Name)&&input.Craft.Name.Length<=160&&input.Craft.ActionVersion==1,
            "Invalid craft name/reserved action version.");
        Unique(input.Instances,x=>x.Id,"design instances");Require(input.Instances.Length is >0 and <=MaximumParts,"Construction part capacity.");
        Require(input.DependencyDigest==catalog.DependencyDigest(input.Instances),"Modified used definition/resource dependency closure.");
        var instances=input.Instances.OrderBy(x=>x.Order).ToImmutableArray();
        Require(instances.Select(x=>x.Order).SequenceEqual(Enumerable.Range(0,instances.Length)),"Instance order must be explicit and complete.");
        var parts=instances.Select(i=>Place(i,catalog.Resolve(i.Definition),genericTensor:true)).ToImmutableArray();
        int Find(string id)
        {var i=parts.FindIndex(p=>p.Instance.Id==id);Require(i>=0,"Unresolved part instance.");return i;}
        var root=Find(input.Root);
        if(craft)Require(parts[root].Definition.Standard!.RootEligible,"Craft root is not eligible.");
        if(input.ControlPart is {} control)Require(parts[Find(control)].Definition.Construction!.Command,"Selected control part lacks command capability.");
        Require(!input.Connections.IsDefault&&input.Connections.Length==parts.Length-1,"One connected rooted structural tree required.");
        var occupied=new HashSet<(string,string)>();var names=new HashSet<string>(StringComparer.Ordinal);var parents=Enumerable.Repeat(-1,parts.Length).ToArray();
        var connections=ImmutableArray.CreateBuilder<CompiledConstructionConnection>(input.Connections.Length);
        foreach(var edge in input.Connections)
        {
            Require(edge is not null&&edge.Construction is not null,"Explicit connection identity/capabilities required.");
            var c=edge.Construction;Require(Identifier(c.Id)&&names.Add(c.Id)&&((int)c.Services&~7)==0,"Duplicate/invalid connection identity/services.");
            Require(craft?c.ClockDegrees is not null&&!c.Detachable:c.ClockDegrees is null,"Connection clock/schema or excluded detachability.");
            var parent=Find(edge.Parent);var child=Find(edge.Child);
            Require(parent!=child&&child!=root&&parents[child]<0,"Cycle, root parent or multiple structural parents.");
            Require(occupied.Add((edge.Parent,edge.ParentEndpoint))&&occupied.Add((edge.Child,edge.ChildEndpoint)),"Occupied structural interface.");
            ValidateMate(parts[parent],edge.ParentEndpoint,parts[child],edge.ChildEndpoint,c);
            parents[child]=parent;
            connections.Add(new(c.Id,parent,child,edge.ParentEndpoint,edge.ChildEndpoint,c.Detachable,c.Services));
        }
        ValidateRootedParents(parents,root);
        Unique(input.ServiceLinks,x=>x.Id,"service links");
        if(craft)Require(input.ServiceLinks.Length==0,"Craft external service-line authoring is outside this slice.");
        var links=ImmutableArray.CreateBuilder<CompiledServiceEdge>(input.ServiceLinks.Length+connections.Count);
        foreach(var c in connections)links.Add(new(c.Id,c.Parent,c.Child,c.ParentInterface,c.ChildInterface,c.Services,true));
        foreach(var link in input.ServiceLinks)
        {
            Require(names.Add(link.Id)&&link.Services!=ConstructionService.None&&((int)link.Services&~7)==0,"Invalid duplicate/empty service link.");
            var from=Find(link.FromPart);var to=Find(link.ToPart);Require(from!=to,"Self service link.");
            var a=Capability(parts[from],link.FromInterface);var b=Capability(parts[to],link.ToInterface);
            Require((a.Services&b.Services&link.Services)==link.Services,"Service link exceeds endpoint capabilities.");
            Require(link.Bidirectional||(link.Services&(ConstructionService.Electricity|ConstructionService.Data))==0,"Power and data links are bidirectional.");
            links.Add(new(link.Id,from,to,link.FromInterface,link.ToInterface,link.Services,link.Bidirectional));
        }
        Unique(input.Configuration,x=>x.Part,"instance configuration");Require(input.Configuration.Length==parts.Length,"Every instance has explicit configuration.");
        foreach(var configuration in input.Configuration)
        {
            var part=parts[Find(configuration.Part)].Definition;
            Unique(configuration.Stores,x=>x.Store,"initial stores");Unique(configuration.Electrical,x=>x.Module,"initial electrical modules");
            Require(configuration.Stores.Length==part.Stores.Length&&configuration.Electrical.Length==part.Construction!.Electrical.Length,"Configuration/capability mismatch.");
            foreach(var s in configuration.Stores)
            {var def=part.Stores.SingleOrDefault(d=>d.Id==s.Store);Require(def is not null&&double.IsFinite(s.QuantityKg)&&s.QuantityKg>=0&&s.QuantityKg<=def.CapacityKg,"Invalid initial store quantity.");}
            foreach(var e in configuration.Electrical)
            {var def=part.Construction!.Electrical.SingleOrDefault(d=>d.Id==e.Module);Require(def is not null&&double.IsFinite(e.ChargeJ)&&e.ChargeJ>=0&&e.ChargeJ<=def.CapacityJ,"Invalid initial battery charge.");}
        }
        Unique(input.Symmetry,x=>x.Id,"symmetry groups");var groupMembers=new HashSet<string>(StringComparer.Ordinal);
        foreach(var group in input.Symmetry)
        {
            var basis=parts[Find(group.BasePart)];Unique(group.Members,x=>x.Part,"symmetry membership");
            Require(group.Members.Length>=(craft?1:2)&&group.Members.Any(m=>m.Part==group.BasePart),"Symmetry needs base and members.");
            Require(craft?group.Placement is not null:group.Placement is null,"Symmetry schema/provenance requires explicit migration.");
            foreach(var member in group.Members)
            {
                var placed=parts[Find(member.Part)];var expected=basis.Instance.Pose.Then(member.FromBase);
                Require(groupMembers.Add(member.Part)&&placed.Instance.Definition==basis.Instance.Definition&&member.FromBase.Rigid&&
                    (expected.Position-placed.Instance.Pose.Position).LengthSquared<=1e-18&&(expected.Rotation-placed.Instance.Pose.Rotation).Maximum<=1e-12,
                    "Symmetry transform/definition mismatch or overlapping ownership.");
            }
        }
        Unique(input.Actions,x=>x.Id,"design actions");var actionOrder=new HashSet<(int,int)>();
        if(craft)Require(input.Actions.Length==0,"Craft action metadata reserved; executable actions excluded.");
        foreach(var action in input.Actions)
        {
            var part=parts[Find(action.Part)];Require(action.Stage>=0&&action.Order>=0&&actionOrder.Add((action.Stage,action.Order)),"Invalid action ordering.");
            Require((action.Consumer is null)!=(action.Connection is null),"One action target required.");
            if(action.Consumer is {} consumer)Require(part.Definition.Construction!.Consumers.Any(c=>c.Id==consumer),"Unknown action consumer.");
            if(action.Connection is {} connection)Require(connections.Any(c=>c.Id==connection&&c.Detachable&&(parts[c.Parent].Instance.Id==action.Part||parts[c.Child].Instance.Id==action.Part)),"Action does not own a detachable interface.");
        }
        if(craft)ValidateCraftBindings(input,parts);
        var canonical=input with {Instances=instances,Connections=input.Connections.OrderBy(x=>x.Construction!.Id,StringComparer.Ordinal).ToImmutableArray(),
            ServiceLinks=input.ServiceLinks.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),
            Configuration=input.Configuration.OrderBy(x=>x.Part,StringComparer.Ordinal).Select(c=>c with {Stores=c.Stores.OrderBy(x=>x.Store,StringComparer.Ordinal).ToImmutableArray(),Electrical=c.Electrical.OrderBy(x=>x.Module,StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray(),
            Symmetry=input.Symmetry.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(g=>craft?g:g with {Members=g.Members.OrderBy(x=>x.Part,StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray(),
            Actions=input.Actions.OrderBy(x=>x.Stage).ThenBy(x=>x.Order).ToImmutableArray()};
        return new(catalog,canonical,parts,connections.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),links.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray());
    }
    internal static InterfaceCapability Capability(CompiledPart part,string id)=>part.Definition.Construction!.Interfaces.SingleOrDefault(x=>x.Interface==id)??throw new InvalidDataException("Undeclared interface.");
    internal static void ValidateMate(CompiledPart parent,string parentId,CompiledPart child,string childId,ConstructionConnection connection)
    {
        var p=parent.Definition.Attachments.SingleOrDefault(x=>x.Id==parentId)??throw new InvalidDataException("Missing parent endpoint.");
        var c=child.Definition.Attachments.SingleOrDefault(x=>x.Id==childId)??throw new InvalidDataException("Missing child endpoint.");
        var pc=Capability(parent,parentId);var cc=Capability(child,childId);
        Require(p.Family==c.Family,"Incompatible explicit interface family.");
        Require((pc.Services&cc.Services&connection.Services)==connection.Services&&(!connection.Detachable||(pc.Detachable&&cc.Detachable)),"Connection exceeds explicit endpoint capability.");
        var clock=Matrix3.Identity;
        if(connection.ClockDegrees is {} degrees){
            Require(parent.Definition.Standard is not null&&child.Definition.Standard is not null,"Clocking requires Part Standard.");
            var pm=parent.Definition.Standard.Mechanical.Single(m=>m.Interface==parentId);
            var cm=child.Definition.Standard.Mechanical.Single(m=>m.Interface==childId);
            Require(PartStandard.CanMate(pm,cm,degrees),"Incompatible explicit role/class/indexed clock.");
            clock=PartStandard.Roll(degrees);
        }
        var a=parent.Instance.Pose.Then(p.Frame);var b=child.Instance.Pose.Then(c.Frame);
        // Accepted FP32 export transport permits ten micrometres; no proximity creates a connection.
        Require((a.Position-b.Position).LengthSquared<=1e-10&&(a.Rotation*clock*Matrix3.Mate-b.Rotation).Maximum<=1e-6,"Explicit mating frames disagree.");
    }
    internal static AssemblyPose Snap(AssemblyPose parent,AttachmentData parentInterface,AttachmentData childInterface,double rollRadians=0)
    {
        Require(parent.Rigid&&parentInterface.Frame.Rigid&&childInterface.Frame.Rigid&&parentInterface.Family==childInterface.Family&&double.IsFinite(rollRadians),"Invalid snap contract.");
        // Interface family defines a keyed mate. Arbitrary roll is applied before choosing a compatible interface, not after mating.
        Require(rollRadians==0,"Keyed attachment does not permit undeclared roll.");
        var target=parent.Then(parentInterface.Frame);var rotation=target.Rotation*Matrix3.Mate*childInterface.Frame.Rotation.Transpose();
        return new(target.Position-rotation.Apply(childInterface.Frame.Position),rotation);
    }
    internal static void ValidateRootedParents(ReadOnlySpan<int> parents,int root)
    {
        Require((uint)root<(uint)parents.Length&&parents[root]==-1,"Invalid root.");
        for(var i=0;i<parents.Length;i++)
        {var current=i;var remaining=parents.Length;while(current!=root){Require(remaining-->0&&(uint)current<(uint)parents.Length,"Disconnected/cyclic graph.");current=parents[current];}}
    }
    internal ImmutableArray<int> Partition(string connectionId)
    {
        var c=Connections.SingleOrDefault(c=>c.Id==connectionId);
        Require(c.Id is not null&&c.Detachable,"Partition proof requires a detachable edge.");
        var members=new bool[Parts.Length];members[c.Child]=true;
        for(var pass=0;pass<Parts.Length;pass++)foreach(var edge in Connections)if(edge.Id!=connectionId&&members[edge.Parent])members[edge.Child]=true;
        return Enumerable.Range(0,members.Length).Where(i=>members[i]).ToImmutableArray();
    }
}

internal static class ConstructionArraySearch
{
    internal static int FindIndex<T>(this ImmutableArray<T> values,Func<T,bool> predicate)
    {for(var i=0;i<values.Length;i++)if(predicate(values[i]))return i;return -1;}
}
