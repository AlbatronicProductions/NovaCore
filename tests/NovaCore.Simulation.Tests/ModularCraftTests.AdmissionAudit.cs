using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    // Independent full-boundary counterexamples retained as permanent refusal tests.
    // This is a schema-only mutation, not a proposed seventh part or a balance change.
    internal static void PartStandardAdmissionAudit()
    {
        var d=DefinitionFixture();const double radius=1e-5,length=3e-5;
        var volume=Math.PI*radius*radius*length;
        var tensor=Matrix3.Diagonal(new(radius*radius/2,radius*radius/4+length*length/12,radius*radius/4+length*length/12));
        var failures=0;
        foreach(var pair in new[]{(Density:1000d,Capacity:volume*1000*1000),(Density:1e20,Capacity:1d)})
        {
            var candidate=d with {Stores=[d.Stores[0] with {CapacityKg=pair.Capacity}],
                Construction=d.Construction! with {StoreGeometry=[new("store",volume,tensor)]},
                Standard=d.Standard! with {StoreLaws=[new("store",StoreDepletionLaw.ProportionalSpatial,pair.Density,0,radius,length,"Independent inconsistency witness")]}};
            var before=AssemblyJson.Write(candidate);var admitted=false;string? digest=null;var reloaded=false;
            try{var catalog=Catalog(candidate);admitted=true;digest=catalog.Digest;reloaded=AssemblyDefinitionCatalog.Load(catalog.Save()).Digest==digest;}
            catch(InvalidDataException){}
            var expectedMass=pair.Density*volume;
            Console.WriteLine(JsonSerializer.Serialize(new {densityKgM3=pair.Density,capacityKg=pair.Capacity,
                geometricVolumeM3=volume,requiredVolumeM3=pair.Capacity/pair.Density,expectedMassKg=expectedMass,
                capacityToPhysicalMassRatio=pair.Capacity/expectedMass,admitted,reloaded,digest,
                sourceUnchanged=before.SequenceEqual(AssemblyJson.Write(candidate))}));
            if(admitted)failures++;
        }
        foreach(var field in new[]{"construction.asset.materialOrigin","standard.socketGroups[0].axis.position"})
        {
            var json=JsonNode.Parse(Catalog(SocketFixture()).Save())!;
            var definition=json["definitions"]![0]!;
            var vector=(field.StartsWith("construction",StringComparison.Ordinal)?definition["construction"]!["asset"]!["materialOrigin"]:
                definition["standard"]!["socketGroups"]![0]!["axis"]!["position"])!.AsObject();
            vector.Remove("z");vector["bogus"]=0;
            string? refusal=null;var structured=false;
            try{_=AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(json.ToJsonString()));}
            catch(Exception e){refusal=e.GetType().Name;structured=e is InvalidDataException or JsonException;}
            Console.WriteLine(JsonSerializer.Serialize(new {field,refusal,structured}));
            if(!structured)failures++;
        }
        if(failures!=0)throw new InvalidOperationException("Gate 1 admission defects: physical consistency or malformed vector JSON refusal.");
        Console.WriteLine("Modular full admission audit PASS");
    }
}
