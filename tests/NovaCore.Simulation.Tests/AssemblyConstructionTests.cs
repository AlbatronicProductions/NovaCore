using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private static int checks;
    private static void Check(bool value,string label)
    {if(!value)throw new InvalidOperationException("CONSTRUCTION: "+label);checks++;}
    private static void Reject(Action action,string label)
    {try{action();}catch(Exception e) when(e is InvalidDataException or JsonException){checks++;return;}throw new InvalidOperationException("CONSTRUCTION accepted: "+label);}
    internal static string Root
    {
        get {var path=new DirectoryInfo(Environment.CurrentDirectory);while(path is not null&&!File.Exists(Path.Combine(path.FullName,"NovaCore.sln")))path=path.Parent;
            return path?.FullName??throw new InvalidDataException("Repository root required for development fixture.");}
    }
    internal static AssemblyDefinitionCatalog Development()=>AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Root,"assets/vehicles/development/development-catalog.json")));
    private static byte[] Glb(string[] names,bool external=false)
    {
        var json=JsonSerializer.SerializeToUtf8Bytes(new {asset=new {version="2.0"},nodes=names.Select(name=>new {name}),buffers=new[]{new Dictionary<string,object>{{"byteLength",4}}}});
        if(external)json=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(json).Replace("\"byteLength\":4","\"byteLength\":4,\"uri\":\"unsealed.bin\""));
        var aligned=(json.Length+3)&~3;var data=new byte[20+aligned+12];
        void U(int offset,uint v)=>System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset),v);
        U(0,0x46546c67);U(4,2);U(8,(uint)data.Length);U(12,(uint)aligned);U(16,0x4e4f534a);
        json.CopyTo(data,20);data.AsSpan(20+json.Length,aligned-json.Length).Fill(32);U(20+aligned,4);U(24+aligned,0x004e4942);return data;
    }
    internal static void Definitions(string? assetRoot=null)
    {
        checks=0;var catalog=Development();
        Check(catalog.Data.Definitions.Length==7,"accepted development definitions");
        Check(catalog.Data.Definitions.All(x=>x.Construction!.Development),"development metadata");
        Check(catalog.Data.Definitions.Sum(x=>x.Stores.Length)==36,"explicit resource owners");
        Check(catalog.Data.Definitions.Sum(x=>x.Construction!.MassRegions.Length)==146,"reusable engine regions counted once");
        Check(catalog.Save().SequenceEqual(AssemblyDefinitionCatalog.Load(catalog.Save()).Save()),"catalog roundtrip");
        var reversed=catalog.Data with {Definitions=catalog.Data.Definitions.Reverse().Select(d=>d with {Construction=d.Construction! with {
            Consumers=d.Construction.Consumers.Reverse().Select(c=>c with {Mixture=c.Mixture.Reverse().ToImmutableArray(),FeedStores=c.FeedStores.Reverse().ToImmutableArray(),FeedInterfaces=c.FeedInterfaces.Reverse().ToImmutableArray()}).ToImmutableArray(),
            Subparts=d.Construction.Subparts.Reverse().Select(s=>s with {MassRegions=s.MassRegions.Reverse().ToImmutableArray(),Actuators=s.Actuators.Reverse().ToImmutableArray()}).ToImmutableArray()}}).ToImmutableArray()};
        Check(AssemblyDefinitionCatalog.Compile(reversed).Digest==catalog.Digest,"nested membership permutation identity");
        var engine=catalog.Data.Definitions.Single(d=>d.Id=="nc.booster-engine.liquid-a");var reference=catalog.Reference(engine);
        Check(ReferenceEquals(catalog.Resolve(reference),engine),"shared immutable definition");
        var placements=Enumerable.Range(0,3).Select(i=>AssemblyConstructionFacts.Place(new("engine."+i,i,reference,new(new Double3(0,i,0),Matrix3.Identity)),engine)).ToArray();
        Check(placements.Select(x=>x.Instance.Id).Distinct().Count()==3&&placements.All(x=>ReferenceEquals(x.Definition,engine)),"repeated instance identity");
        var vessel=new ConstructionVehicleIdentity(new(100),1);
        var runtimeParts=placements.Select(p=>new ConstructionPartIdentity(vessel,p.Instance.Id)).ToArray();
        Check(runtimeParts.Distinct().Count()==3&&runtimeParts[0]!=runtimeParts[0] with {Vehicle=vessel with {Generation=2}},"runtime lifetime separate from design identity");
        Check(new ConstructionSubpartIdentity(runtimeParts[0],"Engine")!=new ConstructionSubpartIdentity(runtimeParts[1],"Engine"),"parent-local subpart identity");
        Reject(()=>catalog.Resolve(reference with {Revision=2}),"missing revision");
        Reject(()=>catalog.Resolve(reference with {Digest=new string('0',64)}),"changed definition");
        var region=new MassRegionData("tiny",1e-12,Double3.Zero,Matrix3.Identity*1e-12);
        var tiny=engine with {DryMassKg=0,LocalCom=Double3.Zero,LocalInertia=default,Construction=engine.Construction! with {MassRegions=[region],Subparts=[],Consumers=[]}};
        Reject(()=>AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=[tiny]}),"invalid authored aggregate hidden by tolerance");
        var cycle=engine with {Construction=engine.Construction! with {Subparts=engine.Construction.Subparts.Select(s=>s with {Parent=s.Id}).ToImmutableArray()}};
        Reject(()=>AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=[cycle]}),"subpart cycle");
        Reject(()=>AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Add(engine)}),"duplicate revision");
        var revised=engine with {Revision=2};
        Check(AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Add(revised)}).Resolve(new(engine.Id,2,AssemblyJson.Digest(revised))).Revision==2,"coexisting revisions");
        Check(engine.Construction!.Subparts.Single().VisualNode.EndsWith(".Engine",StringComparison.Ordinal)&&engine.Construction.Asset.RequiredNodes.Any(n=>n.EndsWith(".Engine.Mount",StringComparison.Ordinal)),"moving engine and fixed mount");
        Check(AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn").Digest=="ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf","banked SRV identity unchanged");
        var temp=Path.Combine(Root,"build","construction-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try
        {
            var bytes=Glb(["ROOT","Engine"]);var file=Path.Combine(temp,"fixture.glb");File.WriteAllBytes(file,bytes);
            var asset=engine.Construction.Asset with {RelativePath="fixture.glb",Sha256=Convert.ToHexStringLower(SHA256.HashData(bytes)),RequiredNodes=["ROOT","Engine"]};
            var resolver=new ConstructionAssetLibrary();var snapshot=resolver.Resolve(temp+Path.DirectorySeparatorChar,asset);
            Check(snapshot.Bytes.AsSpan().SequenceEqual(bytes),"sealed immutable bytes");
            Reject(()=>resolver.Resolve(temp,asset with {RequiredNodes=["missing"]}),"cache contract validation");
            File.WriteAllBytes(file,Glb(["modified"]));Check(resolver.Resolve(temp,asset).Bytes==snapshot.Bytes,"admitted snapshot not mutable path");
            Reject(()=>new ConstructionAssetLibrary().Resolve(temp,asset),"changed disk content");
            bytes=Glb(["ROOT","ROOT"]);File.WriteAllBytes(file,bytes);asset=asset with {Sha256=Convert.ToHexStringLower(SHA256.HashData(bytes)),RequiredNodes=["ROOT"]};
            Reject(()=>new ConstructionAssetLibrary().Resolve(temp,asset),"ambiguous named node");
            bytes=Glb(["ROOT"],true);File.WriteAllBytes(file,bytes);asset=asset with {Sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))};
            Reject(()=>new ConstructionAssetLibrary().Resolve(temp,asset),"external buffer");
        }
        finally {Directory.Delete(temp,true);}
        if(assetRoot is not null){var resolver=new ConstructionAssetLibrary();foreach(var d in catalog.Data.Definitions)resolver.Resolve(assetRoot,d.Construction!.Asset);Check(resolver.VerifiedCount==7,"accepted exported assets");}
        Console.WriteLine($"Construction Stage 1 PASS: {checks} checks; catalog {catalog.Digest}");
    }
}
