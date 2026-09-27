using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

// These extend the existing assembly definition/instance owners. The old schema
// remains the closed SRV admission projection; generic capabilities do not relax it.
internal sealed record ConstructionAsset(string Id, uint Revision, string Sha256, string RelativePath,
    string Units, string Basis, Double3 MaterialOrigin, ImmutableArray<string> RequiredNodes, string Provenance);
internal sealed record PhysicalSource(string Id, uint Revision, string Sha256, string Provenance);
internal sealed record MassRegionData(string Id, double MassKg, Double3 Com, Matrix3 InertiaAtCom);
internal sealed record StoreGeometryData(string Store, double UsableVolumeM3, Matrix3 InertiaPerKg);
internal enum ConstructionSubpartKind { Visual, Gimbal, Deployable, Physical }
internal sealed record ConstructionSubpart(string Id, string? Parent, AssemblyPose Pose, string VisualNode,
    ConstructionSubpartKind Kind, ImmutableArray<string> MassRegions, ImmutableArray<string> Actuators);
internal sealed record ResourceTypeData(string Id, uint Revision, string Name);
internal sealed record MixtureTerm(string Resource, uint Weight);
internal enum ConstructionFlowRule { FarthestFirst, NearestFirst }
internal sealed record ConsumerDefinition(string Id, string? Subpart, string Model, double TotalFlowKgS,
    ImmutableArray<MixtureTerm> Mixture, ImmutableArray<string> FeedStores, ImmutableArray<string> FeedInterfaces,
    ConstructionFlowRule FlowRule, Double3 ForcePoint, Double3 Axis, double ThrustN, GimbalData? Gimbal);
[Flags]
internal enum ConstructionService { None=0, Propellant=1, Electricity=2, Data=4 }
internal sealed record InterfaceCapability(string Interface, ConstructionService Services, bool Detachable);
internal enum ElectricalRole { Battery, EngineGenerator, SolarGenerator, Load }
internal sealed record ElectricalDefinition(string Id, ElectricalRole Role, double CapacityJ, double Watts, string? Driver);
internal sealed record PartConstructionData(string Name, bool Development, ConstructionAsset Asset, PhysicalSource PhysicalSource,
    ImmutableArray<MassRegionData> MassRegions, ImmutableArray<StoreGeometryData> StoreGeometry,
    ImmutableArray<ConstructionSubpart> Subparts, ImmutableArray<ConsumerDefinition> Consumers,
    ImmutableArray<InterfaceCapability> Interfaces, ImmutableArray<ElectricalDefinition> Electrical,
    bool Command, ImmutableArray<string> UnqualifiedHardware);
internal sealed record ConstructionCatalogData(string Schema, ImmutableArray<ResourceTypeData> Resources,
    ImmutableArray<PartDefinitionData> Definitions);
// Observation identities only; publication capabilities are never reconstructed from these values.
internal readonly record struct ConstructionVehicleIdentity(SpacecraftId Vessel, long Generation);
internal readonly record struct ConstructionPartIdentity(ConstructionVehicleIdentity Vehicle, string DesignInstance);
internal readonly record struct ConstructionSubpartIdentity(ConstructionPartIdentity Part, string LocalSubpart);

internal static class AssemblyConstructionFacts
{
    internal static void Require([DoesNotReturnIf(false)] bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
    internal static bool Identifier(string? value) => value is { Length: >0 and <=128 } &&
        value.All(c=>char.IsAsciiLetterOrDigit(c)||c is '_' or '-' or '.' or '/' or '+');
    internal static bool Hash(string? value) => value is { Length:64 } && value.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f');
    internal static void Unique<T>(ImmutableArray<T> values, Func<T,string> key, string label)
    {
        Require(!values.IsDefault && values.Length<=4096, "Missing or oversized " + label);
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var value in values) Require(value is not null && Identifier(key(value)) && seen.Add(key(value)), "Invalid/duplicate " + label);
    }
    internal static CompiledPart Place(PartInstanceData instance, PartDefinitionData definition, bool genericTensor=false)
    {
        Require(Identifier(instance.Id)&&instance.Order>=0&&instance.Pose.Rigid,"Invalid instance identity/rigid pose.");
        Require(instance.Definition is not null&&instance.Definition.Id==definition.Id&&instance.Definition.Revision==definition.Revision&&
            instance.Definition.Digest==AssemblyJson.Digest(definition),"Unresolved or modified definition.");
        var tensor=instance.Pose.Rotation*definition.LocalInertia*instance.Pose.Rotation.Transpose();
        // Only derived generic tensors use one rounded upper triangle. Authored tensors
        // still require exact symmetry; the banked SRV projection keeps its arithmetic.
        if(genericTensor)tensor=new(tensor.A,tensor.B,tensor.C,tensor.B,tensor.E,tensor.F,tensor.C,tensor.F,tensor.I);
        return new(instance,definition,instance.Pose.Point(definition.LocalCom),tensor);
    }
    internal static AssemblyMass Aggregate(IEnumerable<MassRegionData> regions)
    {
        var mass=0d;var first=Double3.Zero;var origin=default(Matrix3);
        foreach(var r in regions){mass+=r.MassKg;first+=r.Com*r.MassKg;origin+=r.InertiaAtCom+Matrix3.Parallel(r.Com)*r.MassKg;}
        Require(double.IsFinite(mass)&&mass>0,"Invalid aggregate mass.");
        var com=first/mass;var inertia=origin-Matrix3.Parallel(com)*mass;
        Require(com.IsFinite&&inertia.PhysicalInertia,"Invalid aggregate tensor.");
        return new(mass,com,inertia);
    }
}

/// <summary>Immutable versioned library. No runtime identity, quantities or renderer objects.</summary>
internal sealed class AssemblyDefinitionCatalog
{
    internal const string Schema="novacore.construction-catalog/1";
    internal const string PartStandardSchema="novacore.construction-catalog/2";
    internal const int MaximumCatalogBytes=16_000_000;
    internal ConstructionCatalogData Data {get;}
    internal string Digest {get;}
    private readonly ImmutableDictionary<(string,uint),PartDefinitionData> definitions;
    private AssemblyDefinitionCatalog(ConstructionCatalogData data)
    {
        var bytes=AssemblyJson.Write(data);
        AssemblyConstructionFacts.Require(bytes.Length<=MaximumCatalogBytes,"Bounded catalog size exceeded.");
        Data=data;Digest=Convert.ToHexStringLower(SHA256.HashData(bytes));
        definitions=data.Definitions.ToImmutableDictionary(x=>(x.Id,x.Revision));
    }
    internal PartDefinitionData Resolve(DefinitionReference reference)
    {
        if(reference is null||!definitions.TryGetValue((reference.Id,reference.Revision),out var value)||AssemblyJson.Digest(value)!=reference.Digest)
            throw new InvalidDataException("Missing revision or changed part definition.");
        return value;
    }
    internal DefinitionReference Reference(PartDefinitionData definition)=>new(definition.Id,definition.Revision,AssemblyJson.Digest(definition));
    internal string DependencyDigest(IEnumerable<PartInstanceData> instances)
    {
        var used=instances.Select(i=>Resolve(i.Definition)).DistinctBy(d=>(d.Id,d.Revision))
            .OrderBy(d=>d.Id,StringComparer.Ordinal).ThenBy(d=>d.Revision).ToArray();
        var species=used.SelectMany(d=>d.Stores.Select(s=>s.ResourceIdentity)
            .Concat(d.Construction!.Consumers.SelectMany(c=>c.Mixture.Select(m=>m.Resource)))
            .Concat(d.Standard is {} standard ? standard.Ports.Where(p=>p.Resource is not null).Select(p=>p.Resource!) : []))
            .ToHashSet(StringComparer.Ordinal);
        return AssemblyJson.Digest(new {Parts=used.Select(Reference).ToArray(),
            Resources=Data.Resources.Where(r=>species.Contains(r.Id)).OrderBy(r=>r.Id,StringComparer.Ordinal).ToArray()});
    }
    internal static AssemblyDefinitionCatalog Load(ReadOnlySpan<byte> bytes)=>Compile(AssemblyJson.Read<ConstructionCatalogData>(bytes,MaximumCatalogBytes));
    internal byte[] Save()=>AssemblyJson.Write(Data);
    internal static AssemblyDefinitionCatalog Compile(ConstructionCatalogData data)
    {
        static void Need([DoesNotReturnIf(false)] bool v,string m)=>AssemblyConstructionFacts.Require(v,m);
        Need(data is not null&&(data.Schema==Schema||data.Schema==PartStandardSchema),"Unknown construction catalog.");
        bool Physical(Matrix3 value)=>data.Schema==PartStandardSchema?PartStandardExact.PhysicalInertia(value):value.PhysicalInertia;
        AssemblyConstructionFacts.Unique(data.Resources,r=>r.Id,"resources");
        Need(!data.Definitions.IsDefault&&data.Definitions.Length is >0 and <=1024,"Invalid definition count.");
        var keys=new HashSet<(string,uint)>();
        foreach(var resource in data.Resources)Need(resource.Revision>0&&!string.IsNullOrWhiteSpace(resource.Name),
            $"RESOURCE_METADATA_INVALID resources[{resource.Id}]: positive revision and resource name required.");
        var definitions=ImmutableArray.CreateBuilder<PartDefinitionData>(data.Definitions.Length);
        foreach(var input in data.Definitions)
        {
            Need(input is not null&&AssemblyConstructionFacts.Identifier(input.Id)&&input.Revision>0&&keys.Add((input.Id,input.Revision)),"Invalid/duplicate definition revision.");
            var c=input.Construction??throw new InvalidDataException("Generic part requires construction capabilities.");
            Need(data.Schema==PartStandardSchema ? input.Standard is not null : input.Standard is null,
                "Part Standard requires catalog/2; legacy catalog semantics cannot be reinterpreted.");
            Need(input.Role==AssemblyRole.Component&&input.Propulsion is null&&input.JetActuators is null&&input.Gimbal is null,"Generic behavior must use composable capabilities.");
            Need(!string.IsNullOrWhiteSpace(c.Name)&&c.Name.Length<=160,"Invalid display name.");
            var asset=c.Asset??throw new InvalidDataException("Missing immutable asset identity.");
            Need(AssemblyConstructionFacts.Identifier(asset.Id)&&asset.Revision>0&&AssemblyConstructionFacts.Hash(asset.Sha256)&&input.VisualReference==asset.Id,"Invalid asset identity.");
            Need(asset.Units=="m"&&asset.Basis=="gltf=(E.Y,-E.Z,-E.X)"&&asset.MaterialOrigin.IsFinite,"Unsupported asset units/basis/origin.");
            Need(!string.IsNullOrWhiteSpace(asset.Provenance)&&asset.Provenance.Length<=1024&&ValidRelativePath(asset.RelativePath),"Invalid asset path/provenance.");
            AssemblyConstructionFacts.Unique(asset.RequiredNodes,x=>x,"required visual nodes");
            var source=c.PhysicalSource??throw new InvalidDataException("Missing physical definition identity.");
            Need(AssemblyConstructionFacts.Identifier(source.Id)&&source.Revision>0&&AssemblyConstructionFacts.Hash(source.Sha256)&&!string.IsNullOrWhiteSpace(source.Provenance),"Invalid physical reference.");
            AssemblyConstructionFacts.Unique(c.MassRegions,x=>x.Id,"mass regions");
            Need(c.MassRegions.Length>0,"Physical mass regions required.");
            foreach(var r in c.MassRegions)Need(double.IsFinite(r.MassKg)&&r.MassKg>0&&r.Com.IsFinite&&Physical(r.InertiaAtCom),$"Invalid mass region definition[{input.Id}].massRegions[{r.Id}].mass/com/inertia");
            var orderedRegions=c.MassRegions.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray();
            Need(double.IsFinite(input.DryMassKg)&&input.DryMassKg>0&&input.LocalCom.IsFinite&&Physical(input.LocalInertia),
                "Invalid authored aggregate mass/COM/inertia.");
            if(data.Schema==PartStandardSchema)PartStandardAggregateConsistency.Validate(input,orderedRegions);
            else
            {
                // Preserve the existing v1 authoring/roundoff contract and serialized identities.
                var mass=AssemblyConstructionFacts.Aggregate(orderedRegions);
                Need(Math.Abs(mass.Mass-input.DryMassKg)<=1e-10*Math.Max(1,mass.Mass)&&
                    (mass.Com-input.LocalCom).LengthSquared<=1e-18&&(mass.Inertia-input.LocalInertia).Maximum<=1e-8*Math.Max(1,mass.Inertia.Maximum),
                    "Authored aggregate disagrees with constituent physical definitions.");
            }
            AssemblyConstructionFacts.Unique(input.Attachments,x=>x.Id,"attachment interfaces");
            foreach(var a in input.Attachments)Need(AssemblyConstructionFacts.Identifier(a.Family)&&a.Frame.Rigid,
                $"ATTACHMENT_FRAME_INVALID definition[{input.Id}].attachments[{a.Id}].frame/family");
            AssemblyConstructionFacts.Unique(c.Interfaces,x=>x.Interface,"interface services");
            Need(c.Interfaces.Length==input.Attachments.Length,"Every interface declares independent services.");
            foreach(var a in c.Interfaces)Need(input.Attachments.Any(x=>x.Id==a.Interface)&&((int)a.Services&~7)==0,"Undeclared interface or service.");
            AssemblyConstructionFacts.Unique(input.Stores,x=>x.Id,"stores");
            AssemblyConstructionFacts.Unique(c.StoreGeometry,x=>x.Store,"store geometry");
            Need(input.Stores.Length==c.StoreGeometry.Length,"Each store needs physical geometry.");
            foreach(var s in input.Stores)
            {
                Need(data.Resources.Any(r=>r.Id==s.ResourceIdentity)&&s.Datum.IsFinite&&double.IsFinite(s.CapacityKg)&&s.CapacityKg>0,"Invalid store/resource binding.");
                var g=c.StoreGeometry.SingleOrDefault(x=>x.Store==s.Id);
                Need(g is not null&&double.IsFinite(g.UsableVolumeM3)&&g.UsableVolumeM3>0&&Physical(g.InertiaPerKg),"Invalid finite store volume/inertia.");
            }
            AssemblyConstructionFacts.Unique(c.Consumers,x=>x.Id,"consumer capabilities");
            foreach(var consumer in c.Consumers)
            {
                Need(AssemblyConstructionFacts.Identifier(consumer.Model)&&double.IsFinite(consumer.TotalFlowKgS)&&consumer.TotalFlowKgS>0&&
                    double.IsFinite(consumer.ThrustN)&&consumer.ThrustN>0&&consumer.ForcePoint.IsFinite&&consumer.Axis.IsFinite&&
                    Math.Abs(consumer.Axis.LengthSquared-1)<=1e-10&&Enum.IsDefined(consumer.FlowRule),"Invalid consumer physical definition.");
                AssemblyConstructionFacts.Unique(consumer.Mixture,x=>x.Resource,"mixture");
                Need(consumer.Mixture.Length>0&&consumer.Mixture.All(x=>x.Weight>0&&data.Resources.Any(r=>r.Id==x.Resource)),"Invalid mixture species/weights.");
                AssemblyConstructionFacts.Unique(consumer.FeedStores,x=>x,"declared feed stores");
                AssemblyConstructionFacts.Unique(consumer.FeedInterfaces,x=>x,"declared feed interfaces");
                Need(consumer.FeedStores.Length+consumer.FeedInterfaces.Length>0&&consumer.FeedStores.All(id=>input.Stores.Any(s=>s.Id==id))&&
                    consumer.FeedInterfaces.All(id=>c.Interfaces.Any(a=>a.Interface==id&&a.Services.HasFlag(ConstructionService.Propellant))),"Unresolved engine inlets.");
                if(consumer.Gimbal is {} g)Need(AssemblyConstructionFacts.Identifier(g.Id)&&g.Pivot.IsFinite&&g.NozzleOffset.IsFinite&&
                    double.IsFinite(g.LimitY)&&g.LimitY>=0&&double.IsFinite(g.LimitZ)&&g.LimitZ>=0&&double.IsFinite(g.SlewRate)&&g.SlewRate>=0,"Invalid authored gimbal.");
            }
            AssemblyConstructionFacts.Unique(c.Subparts,x=>x.Id,"subparts");
            var assignedRegions=new HashSet<string>(StringComparer.Ordinal);var assignedActuators=new HashSet<string>(StringComparer.Ordinal);
            foreach(var sub in c.Subparts)
            {
                Need(sub.Pose.Rigid&&asset.RequiredNodes.Contains(sub.VisualNode)&&Enum.IsDefined(sub.Kind),"Invalid owned subpart transform/node.");
                AssemblyConstructionFacts.Unique(sub.MassRegions,x=>x,"subpart mass bindings");
                AssemblyConstructionFacts.Unique(sub.Actuators,x=>x,"subpart actuator bindings");
                foreach(var id in sub.MassRegions)Need(c.MassRegions.Any(x=>x.Id==id)&&assignedRegions.Add(id),"Duplicate/missing subpart region ownership.");
                foreach(var id in sub.Actuators)Need(c.Consumers.Any(x=>x.Id==id&&x.Subpart==sub.Id)&&assignedActuators.Add(id),"Duplicate/missing subpart actuator ownership.");
                var visited=new HashSet<string>(StringComparer.Ordinal){sub.Id};var parent=sub.Parent;
                while(parent is not null){Need(visited.Add(parent),"Subpart cycle.");parent=c.Subparts.SingleOrDefault(x=>x.Id==parent)?.Parent??
                    (c.Subparts.Any(x=>x.Id==parent)?null:throw new InvalidDataException("Missing subpart parent."));}
            }
            foreach(var consumer in c.Consumers)Need(consumer.Subpart is null||assignedActuators.Contains(consumer.Id),"Unresolved actuator subpart.");
            AssemblyConstructionFacts.Unique(c.Electrical,x=>x.Id,"electrical modules");
            foreach(var e in c.Electrical)
            {
                Need(Enum.IsDefined(e.Role)&&double.IsFinite(e.CapacityJ)&&double.IsFinite(e.Watts)&&e.CapacityJ>=0&&e.Watts>=0,"Invalid electrical quantities.");
                Need(e.Role==ElectricalRole.Battery?e.CapacityJ>0&&e.Watts==0&&e.Driver is null:e.CapacityJ==0&&e.Watts>0,"Invalid electrical role fields.");
                Need(e.Role==ElectricalRole.EngineGenerator?c.Consumers.Any(x=>x.Id==e.Driver):e.Driver is null,"Invalid generator driver.");
            }
            AssemblyConstructionFacts.Unique(c.UnqualifiedHardware,x=>x,"unqualified hardware references");
            if(input.Standard is not null)
            {
                PartStandard.Validate(input);
                // ResourceTypeData is authoritative inside this strict catalog schema. Its entire
                // canonical record (including revision) enters the used-resource dependency seal.
                // Interface-only resources must resolve even without a local store or consumer.
                foreach(var port in input.Standard.Ports)
                    if(port.Resource is {} resource)
                        Need(data.Resources.Any(r=>r.Id==resource),
                            $"RESOURCE_UNRESOLVED definition[{input.Id}].standard.ports[{port.Id}].resource: '{resource}'");
            }
            // Caller input order is not identity. Arrays are immutable; all nested collections are immutable too.
            definitions.Add(input with {Standard=input.Standard is null?null:PartStandard.Canonical(input.Standard),
                Attachments=input.Attachments.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),
                Stores=input.Stores.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),Construction=c with {
                    Asset=asset with {RequiredNodes=asset.RequiredNodes.Order(StringComparer.Ordinal).ToImmutableArray()},MassRegions=orderedRegions,
                    StoreGeometry=c.StoreGeometry.OrderBy(x=>x.Store,StringComparer.Ordinal).ToImmutableArray(),
                    Subparts=c.Subparts.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(s=>s with {
                        MassRegions=s.MassRegions.Order(StringComparer.Ordinal).ToImmutableArray(),Actuators=s.Actuators.Order(StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray(),
                    Consumers=c.Consumers.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(v=>v with {
                        Mixture=v.Mixture.OrderBy(x=>x.Resource,StringComparer.Ordinal).ToImmutableArray(),FeedStores=v.FeedStores.Order(StringComparer.Ordinal).ToImmutableArray(),
                        FeedInterfaces=v.FeedInterfaces.Order(StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray(),Interfaces=c.Interfaces.OrderBy(x=>x.Interface,StringComparer.Ordinal).ToImmutableArray(),
                    Electrical=c.Electrical.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),UnqualifiedHardware=c.UnqualifiedHardware.Order(StringComparer.Ordinal).ToImmutableArray()}});
        }
        return new(data with {Resources=data.Resources.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),
            Definitions=definitions.OrderBy(x=>x.Id,StringComparer.Ordinal).ThenBy(x=>x.Revision).ToImmutableArray()});
    }
    private static bool ValidRelativePath(string? path)=>path is {Length:>0 and <=512}&&!Path.IsPathRooted(path)&&
        !path.Contains('\\')&&!path.Contains(':')&&path.Split('/').All(s=>s.Length>0&&s!="."&&s!="..");
}

/// <summary>Cold session content verification. No .blend/runtime dependency and no mutable path identity.</summary>
internal sealed record VerifiedConstructionAsset(string SourcePath, ImmutableArray<byte> Bytes);
internal sealed class ConstructionAssetLibrary
{
    internal const int MaximumAssetBytes=134_217_728;
    internal const long MaximumSessionBytes=536_870_912;
    private readonly Dictionary<(string,uint,string),(VerifiedConstructionAsset Snapshot,ImmutableDictionary<string,int> Nodes)> verified=new();
    private long retainedBytes;
    internal int VerifiedCount=>verified.Count;
    internal VerifiedConstructionAsset Resolve(string root,ConstructionAsset asset)
    {
        var key=(asset.Id,asset.Revision,asset.Sha256);
        static void ValidateNodes(ConstructionAsset reference,ImmutableDictionary<string,int> nodes)
        {if(reference.RequiredNodes.IsDefault||reference.RequiredNodes.Any(n=>n is null||!nodes.TryGetValue(n,out var count)||count!=1))throw new InvalidDataException("Required node is missing or ambiguous.");}
        if(verified.TryGetValue(key,out var cached)){ValidateNodes(asset,cached.Nodes);return cached.Snapshot;}
        var directory=Path.GetFullPath(root);
        if(!Path.EndsInDirectorySeparator(directory))directory+=Path.DirectorySeparatorChar;
        var path=Path.GetFullPath(Path.Combine(directory,asset.RelativePath));
        if(!path.StartsWith(directory,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Asset escaped content root.");
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length is <28 or >MaximumAssetBytes||retainedBytes>MaximumSessionBytes-stream.Length||verified.Count>=1024)
            throw new InvalidDataException("Construction asset/session content capacity exceeded.");
        var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);
        if(Convert.ToHexStringLower(SHA256.HashData(bytes))!=asset.Sha256)throw new InvalidDataException("Changed visual content.");
        if(bytes.Length<20||BitConverter.ToUInt32(bytes,0)!=0x46546c67||BitConverter.ToUInt32(bytes,4)!=2||BitConverter.ToUInt32(bytes,8)!=bytes.Length||
            BitConverter.ToUInt32(bytes,16)!=0x4e4f534a)throw new InvalidDataException("Expected glTF2 binary asset.");
        var length=checked((int)BitConverter.ToUInt32(bytes,12));
        if(length<2||length%4!=0||length>bytes.Length-28)throw new InvalidDataException("Invalid GLB JSON length.");
        var binHeader=20+length;var binLength=BitConverter.ToUInt32(bytes,binHeader);
        if(BitConverter.ToUInt32(bytes,binHeader+4)!=0x004e4942||binLength%4!=0||binHeader+8L+binLength!=bytes.Length)
            throw new InvalidDataException("One bounded embedded BIN chunk required.");
        using var json=JsonDocument.Parse(bytes.AsMemory(20,length),new JsonDocumentOptions{MaxDepth=32});
        var d=json.RootElement;
        if(d.GetProperty("asset").GetProperty("version").GetString()!="2.0")throw new InvalidDataException("Invalid glTF version.");
        var buffers=d.GetProperty("buffers");
        if(buffers.GetArrayLength()!=1||buffers[0].GetProperty("byteLength").GetInt64() is <=0||buffers[0].GetProperty("byteLength").GetInt64()>binLength)
            throw new InvalidDataException("Invalid embedded buffer.");
        static void RejectExternal(JsonElement e)
        {
            if(e.ValueKind==JsonValueKind.Object)foreach(var p in e.EnumerateObject())
            {if(p.Name=="uri")throw new InvalidDataException("External/data URI assets require independent admission.");RejectExternal(p.Value);}
            else if(e.ValueKind==JsonValueKind.Array)foreach(var item in e.EnumerateArray())RejectExternal(item);
        }
        RejectExternal(d);
        var nodes=d.GetProperty("nodes").EnumerateArray().Where(x=>x.TryGetProperty("name",out _)).Select(x=>x.GetProperty("name").GetString()??"")
            .GroupBy(x=>x,StringComparer.Ordinal).ToImmutableDictionary(x=>x.Key,x=>x.Count(),StringComparer.Ordinal);
        ValidateNodes(asset,nodes);
        var snapshot=new VerifiedConstructionAsset(path,ImmutableArray.CreateRange(bytes));
        verified.Add(key,(snapshot,nodes));retainedBytes+=bytes.Length;return snapshot;
    }
}
