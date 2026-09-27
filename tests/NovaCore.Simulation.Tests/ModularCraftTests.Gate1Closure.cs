using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static void RefuseAt(Action action,string field)
    {
        try{action();}
        catch(InvalidDataException e){Check(e.Message.Contains(field,StringComparison.Ordinal),"diagnostic field: "+field);return;}
        catch(JsonException e){Check((e.Path??"").Contains(field,StringComparison.Ordinal),"JSON diagnostic path: "+field);return;}
        throw new InvalidOperationException("MODULAR accepted malformed "+field);
    }
    private static PartDefinitionData PortFixture(string? resource="passthrough.fluid")
    {
        var d=DefinitionFixture();
        return d with {Construction=d.Construction! with {Interfaces=[new("mount",ConstructionService.Electricity|ConstructionService.Propellant,false)]},
            Standard=d.Standard! with {Ports=d.Standard.Ports.Add(new("passthrough",ConstructionService.Propellant,resource,"mount",null,null,null,false))}};
    }
    private static ConstructionCatalogData PortData()=>new(AssemblyDefinitionCatalog.PartStandardSchema,
        [new("test.fluid",1,"Test fluid"),new("passthrough.fluid",7,"Transit fluid")],[PortFixture()]);
    internal static void PartStandardCatalogBound()
    {
        checks=0;var catalog=Catalog(DefinitionFixture());var original=catalog.Save();var data=catalog.Data;
        var emptyNameBytes=AssemblyJson.Write(data with {Resources=[data.Resources[0] with {Name=""}]}).Length;
        var room=AssemblyDefinitionCatalog.MaximumCatalogBytes-emptyNameBytes;
        foreach(var delta in new[]{-1,0,1}){
            var candidate=data with {Resources=[data.Resources[0] with {Name=new string('x',room+delta)}]};
            var bytes=AssemblyJson.Write(candidate);
            Check(bytes.Length==AssemblyDefinitionCatalog.MaximumCatalogBytes+delta,"exact catalog byte boundary constructed");
            if(delta<=0){var compiled=AssemblyDefinitionCatalog.Compile(candidate);
                Check(compiled.Save().SequenceEqual(bytes),"bounded publication same canonical bytes");
                Check(AssemblyDefinitionCatalog.Load(bytes).Digest==compiled.Digest,"largest catalog roundtrips");}
            else{RefuseAt(()=>AssemblyDefinitionCatalog.Compile(candidate),"Bounded catalog size");
                RefuseAt(()=>AssemblyDefinitionCatalog.Load(bytes),"Bounded document size");}
        }
        // Escaping/UTF8 expansion counts bytes, never managed string length.
        var unicode=data with {Resources=[data.Resources[0] with {Name=new string('\u03c0',room/2)}]};
        Check(AssemblyJson.Write(unicode).Length>AssemblyDefinitionCatalog.MaximumCatalogBytes,"Unicode expansion exceeds bound");
        RefuseAt(()=>AssemblyDefinitionCatalog.Compile(unicode),"Bounded catalog size");
        Check(catalog.Save().SequenceEqual(original),"oversize refusal leaves accepted catalog unchanged");
        Console.WriteLine($"Modular Gate 1 catalog bound PASS: {checks} byte-boundary checks");
    }
    private static ConstructionDesignData OnePartDocument(AssemblyDefinitionCatalog catalog)
    {
        var definition=catalog.Data.Definitions[0];
        var p=new PartInstanceData("root",0,catalog.Reference(definition),new(Double3.Zero,Matrix3.Identity));
        return new(CompiledConstructionDesign.CraftSchema,"closure.fixture",1,catalog.DependencyDigest([p]),"root","root",[p],[],[],
            [new("root",definition.Stores.Select(s=>new ConstructionStoreInitial(s.Id,0,true)).ToImmutableArray(),
                definition.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,0,true)).ToImmutableArray())],[],[],new("Closure fixture",1));
    }
    private static PartDefinitionData SocketFixture()
    {
        var d=DefinitionFixture();var s=d.Standard!;var c=d.Construction!;
        var ids=Enumerable.Range(0,8).Select(i=>"radial."+i).ToImmutableArray();
        var mechanics=ids.Select(id=>new PartMechanicalInterface(id,MechanicalKind.Radial,"nc.radial-equipment",1,"R-1",MateRole.Socket,[0])).ToImmutableArray();
        var facing=new Matrix3(0,-1,0,1,0,0,0,0,1);
        var sockets=mechanics.Select((m,i)=>new AttachmentData(m.Interface,PartStandard.FamilyKey(m),
            new(PartStandard.Roll(45*i).Apply(Double3.UnitY),PartStandard.Roll(45*i)*facing))).ToImmutableArray();
        var sets=Enumerable.Range(0,8).SelectMany(anchor=>new[]{1,2,4,8}.Select(count=>new SocketPlacementSet(ids[anchor],count,
            Enumerable.Range(0,count).Select(i=>ids[(anchor+i*8/count)%8]).ToImmutableArray()))).ToImmutableArray();
        return d with {Attachments=d.Attachments.AddRange(sockets),Construction=c with {Interfaces=c.Interfaces.AddRange(ids.Select(id=>new InterfaceCapability(id,ConstructionService.None,false)))},
            Standard=s with {Mechanical=s.Mechanical.AddRange(mechanics),SocketGroups=[new("ring",new(Double3.Zero,Matrix3.Identity),ids,sets)]}};
    }
    internal static void PartStandardClosure()
    {
        checks=0;var data=PortData();var before=AssemblyJson.Write(data);var accepted=AssemblyDefinitionCatalog.Compile(data);var saved=accepted.Save();
        var document=OnePartDocument(accepted);var seal=document.DependencyDigest;
        Check(accepted.Data.Resources.Single(r=>r.Id=="passthrough.fluid").Revision==7,"authoritative exact revision retained");
        Check(AssemblyDefinitionCatalog.Load(saved).Digest==accepted.Digest,"resource schema/content roundtrip");
        foreach(var resource in new string?[]{null,"","bad resource","missing.species","Passthrough.fluid","Transit fluid","passthrough.fluid/8"})
            RefuseAt(()=>AssemblyDefinitionCatalog.Compile(data with {Definitions=[PortFixture(resource)]}),"definition[fixture].standard.ports[passthrough].resource");
        RefuseAt(()=>AssemblyDefinitionCatalog.Compile(data with {Resources=data.Resources.SetItem(1,data.Resources[1] with {Revision=0})}),"resources[passthrough.fluid]");
        Reject(()=>AssemblyDefinitionCatalog.Compile(data with {Resources=data.Resources.Add(data.Resources[1] with {Revision=8})}),"ambiguous resource revision refuses latest choice");
        foreach(var replacement in new[]{data.Resources[1] with {Revision=8},data.Resources[1] with {Name="Changed authoritative content"}})
        {
            var other=AssemblyDefinitionCatalog.Compile(data with {Resources=data.Resources.SetItem(1,replacement)});
            Check(other.DependencyDigest(document.Instances)!=seal,"resource revision/content changes used dependency seal");
            RefuseAt(()=>CompiledConstructionDesign.Compile(other,document),"dependency closure");
            RefuseAt(()=>CompiledConstructionDesign.LoadCraft(other,AssemblyJson.Write(document)),"dependency closure");
            RefuseAt(()=>ConstructionApplicationSession.Create(CompiledConstructionDesign.Compile(accepted,document)),"CraftDocument");
        }
        var substituted=data with {Resources=data.Resources.SetItem(1,data.Resources[1] with {Id="replacement.fluid"})};
        RefuseAt(()=>AssemblyDefinitionCatalog.Compile(substituted),"ports[passthrough].resource");
        var unused=data with {Resources=data.Resources.Add(new("unused.fluid",1,"Transit fluid")),Definitions=data.Definitions.Add(DefinitionFixture() with {Id="unused.part"})};
        var a=AssemblyDefinitionCatalog.Compile(unused);var b=AssemblyDefinitionCatalog.Compile(unused with {Resources=unused.Resources.Reverse().ToImmutableArray(),Definitions=unused.Definitions.Reverse().ToImmutableArray()});
        Check(a.Digest==b.Digest&&a.DependencyDigest(document.Instances)==b.DependencyDigest(document.Instances),"catalog and definition enumeration independent");
        Check(a.DependencyDigest(document.Instances)==seal,"unreferenced resource cannot substitute or pollute closure");
        var altered=AssemblyDefinitionCatalog.Compile(unused with {Resources=unused.Resources.Select(r=>r.Id=="unused.fluid"?r with {Revision=2}:r).ToImmutableArray()});
        Check(altered.DependencyDigest(document.Instances)==seal,"only actual dependencies sealed");
        var unknownSchema=JsonNode.Parse(saved)!.AsObject();unknownSchema["schema"]="novacore.construction-catalog/999";
        Reject(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(unknownSchema.ToJsonString())),"unknown resource schema owner");
        unknownSchema=JsonNode.Parse(saved)!.AsObject();unknownSchema["resources"]![0]!["unapprovedSchema"]="resource/2";
        Reject(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(unknownSchema.ToJsonString())),"unknown authoritative resource fields");
        Check(before.SequenceEqual(AssemblyJson.Write(data))&&saved.SequenceEqual(accepted.Save()),"resource refusals leave source/catalog unchanged");

        var d=SocketFixture();var s=d.Standard!;var g=s.SocketGroups[0];var initial=AssemblyJson.Write(d);var catalog=Catalog(d);var catalogBytes=catalog.Save();
        Check(g.Placements.Length==32&&g.Placements.Select(p=>p.Count).Distinct().Order().SequenceEqual([1,2,4,8]),"all anchors and authored counts admitted");
        Check(catalog.Save().SequenceEqual(AssemblyDefinitionCatalog.Load(catalog.Save()).Save()),"socket placement persistence roundtrip");
        Check(Catalog(d with {Standard=s with {SocketGroups=[g with {Placements=g.Placements.Reverse().ToImmutableArray()}]}}).Digest==catalog.Digest,"placement enumeration canonical");
        Check(Catalog(d with {Standard=s with {SocketGroups=[g with {Sockets=g.Sockets.Reverse().ToImmutableArray()}]}}).Digest==catalog.Digest,"socket membership enumeration canonical; placement order retained");
        void Bad(PartSocketGroup group,string field)=>RefuseAt(()=>Catalog(d with {Standard=s with {SocketGroups=[group]}}),field);
        Bad(g with {Placements=[null!]},"definition[fixture].standard.socketGroups[ring].placements[0]");
        Bad(g with {Placements=[]},"placements");Bad(g with {Placements=default},"placements");
        Bad(g with {Placements=[new(null!,1,["radial.0"])]},"anchor");
        Bad(g with {Placements=[new("missing",1,["missing"])]},"anchor");
        Bad(g with {Placements=[new("radial.0",0,[])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",3,["radial.0","radial.1","radial.2"])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",16,g.Sockets)]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",1,default)]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",1,[])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",2,["radial.0",null!])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",2,["radial.0","radial.0"])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",2,["radial.1","radial.4"])]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",2,["radial.0","unknown"])]},"count/sockets");
        Bad(g with {Placements=[g.Placements[0],g.Placements[0]]},"count/sockets");
        Bad(g with {Placements=[new("radial.0",2,["radial.0","radial.1"])]},"SOCKET_AUTHORED_ROTATIONAL_EQUIVALENCE");
        Bad(g with {Id="bad group"},"definition[fixture].standard.socketGroups");
        Bad(g with {Sockets=["missing"]},"sockets");Bad(g with {Sockets=[null!]},"sockets");
        Bad(g with {Axis=default},"axis");
        Bad(g with {Axis=new(new(double.NaN,0,0),Matrix3.Identity)},"axis");
        Bad(g with {Axis=new(Double3.Zero,Matrix3.Identity*-1)},"axis");
        RefuseAt(()=>Catalog(d with {Attachments=d.Attachments.SetItem(1,d.Attachments[1] with {Frame=default})}),"definition[fixture].attachments[radial.0].frame");
        foreach(var field in new[]{"anchor","count","sockets"})
        {
            var json=JsonNode.Parse(catalogBytes)!;json["definitions"]![0]!["standard"]!["socketGroups"]![0]!["placements"]![0]!.AsObject().Remove(field);
            RefuseAt(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(json.ToJsonString())),"definitions[0].standard.socketGroups[0].placements[0]");
        }
        var nullJson=JsonNode.Parse(catalogBytes)!;nullJson["definitions"]![0]!["standard"]!["socketGroups"]![0]!["placements"]![0]=null;
        RefuseAt(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(nullJson.ToJsonString())),"definition[fixture].standard.socketGroups[ring].placements[0]");
        Check(initial.SequenceEqual(AssemblyJson.Write(d))&&catalogBytes.SequenceEqual(catalog.Save()),"all socket refusals preserve source/catalog bytes");
        Console.WriteLine($"Modular Gate 1 bounded closure PASS: {checks} checks; resource catalog {accepted.Digest}; socket catalog {catalog.Digest}");
    }
}
