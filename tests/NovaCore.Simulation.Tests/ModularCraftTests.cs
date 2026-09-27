using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static int checks;
    private static void Check(bool value,string label)
    {if(!value)throw new InvalidOperationException("MODULAR: "+label);checks++;}
    private static void Reject(Action action,string label)
    {try{action();}catch(Exception e) when(e is InvalidDataException or JsonException){checks++;return;}
        throw new InvalidOperationException("MODULAR accepted: "+label);}
    private static PartDefinitionData DefinitionFixture()
    {
        var mech=new PartMechanicalInterface("mount",MechanicalKind.Stack,"nc.stack",1,"NC-2",MateRole.Socket,[0,90,180,270]);
        var volume=new PartConvexVolume("body",[new(-1,-1,-1),new(-1,1,-1),new(-1,-1,1),new(1,1,1)],"Independent analytic test tetrahedron");
        var standard=new PartStandardData(PartStandard.Schema,true,"Qualification","Schema-only fixture",[mech],[],[volume],[],
            [new("foot",new(new(-1,0,0),Matrix3.Identity),.1,.1,1000,"body","Independent support fixture")],
            [new("store",StoreDepletionLaw.ProportionalSpatial,1000,0,.5,1,"Analytic cylinder")],
            [new("fuel",ConstructionService.Propellant,"test.fluid",null,"store",null,null,false),
             new("wire",ConstructionService.Electricity,null,"mount",null,null,null,false),
             new("battery",ConstructionService.Electricity,null,null,null,null,"battery",false),
             new("load",ConstructionService.Electricity,null,null,null,null,"load",false),
             new("command",ConstructionService.Data,null,null,null,null,null,true)],
            [new("battery-load","battery","load",true),new("battery-wire","battery","wire",true)],
            ["load"],new(true,true,true,true));
        var construction=new PartConstructionData("Fixture",true,
            new("fixture",1,new string('a',64),"fixture.glb","m","gltf=(E.Y,-E.Z,-E.X)",Double3.Zero,["ROOT"],"Test source"),
            new("fixture.physics",1,new string('b',64),"Analytic test mass"),[new("body",1,Double3.Zero,Matrix3.Identity)],
            [new("store",Math.PI/4,Matrix3.Diagonal(new(.125,.0625+1d/12,.0625+1d/12)))],[],[],
            [new("mount",ConstructionService.Electricity,false)],
            [new("battery",ElectricalRole.Battery,100,0,null),new("load",ElectricalRole.Load,0,1,null)],true,[]);
        return new("fixture",1,"fixture",AssemblyRole.Component,1,Double3.Zero,Matrix3.Identity,
            [new("mount",PartStandard.FamilyKey(mech),new(Double3.Zero,Matrix3.Identity))],
            [new("store","test.fluid","test.fluid",Double3.Zero,1000*Math.PI/4)],null,null,null,construction,standard);
    }
    private static AssemblyDefinitionCatalog Catalog(PartDefinitionData d)=>AssemblyDefinitionCatalog.Compile(
        new(AssemblyDefinitionCatalog.PartStandardSchema,[new("test.fluid",1,"Test fluid")],[d]));
    internal static void PartStandardGate()
    {
        checks=0;var d=DefinitionFixture();var c=Catalog(d);var s=d.Standard!;
        Check(c.Save().SequenceEqual(AssemblyDefinitionCatalog.Load(c.Save()).Save()),"strict deterministic roundtrip");
        Check(Catalog(d with {Standard=s with {Ports=s.Ports.Reverse().ToImmutableArray(),Routes=s.Routes.Reverse().ToImmutableArray(),
            Mechanical=s.Mechanical.Select(x=>x with {ClockDegrees=x.ClockDegrees.Reverse().ToImmutableArray()}).ToImmutableArray(),
            Collision=s.Collision.Select(x=>x with {Vertices=x.Vertices.Reverse().ToImmutableArray()}).ToImmutableArray()}}).Digest==c.Digest,
            "nonsemantic authoring order independent");
        Reject(()=>Catalog(d with {Standard=null}),"catalog2 requires standard");
        Reject(()=>AssemblyDefinitionCatalog.Compile(c.Data with {Schema=AssemblyDefinitionCatalog.Schema}),"legacy not reinterpreted");
        Reject(()=>Catalog(d with {Standard=s with {Schema="novacore.part-standard/2"}}),"unknown part schema");
        Reject(()=>Catalog(d with {Standard=s with {Mechanical=[]}}),"missing interface class");
        Reject(()=>Catalog(d with {Standard=s with {Mechanical=[s.Mechanical[0] with {Revision=2}]}}),"unsupported interface family revision");
        Reject(()=>Catalog(d with {Standard=s with {Mechanical=[s.Mechanical[0] with {Size="NC-3"}]}}),"reserved class not admitted");
        Reject(()=>Catalog(d with {Standard=s with {Mechanical=[s.Mechanical[0] with {ClockDegrees=[0,45]}]}}),"undeclared clock policy");
        Reject(()=>Catalog(d with {Standard=s with {Mechanical=[s.Mechanical[0] with {ClockDegrees=[0,0]}]}}),"duplicate clocks");
        Reject(()=>Catalog(d with {Standard=s with {Collision=[]}}),"mesh cannot supply physical volume");
        Reject(()=>Catalog(d with {Standard=s with {Collision=[s.Collision[0] with {Vertices=[new(0,0,0),new(1,0,0),new(0,1,0),new(1,1,0)]}]}}),"degenerate collision");
        Reject(()=>Catalog(d with {Standard=s with {Collision=[s.Collision[0] with {Provenance=""}]}}),"collision provenance");
        Reject(()=>Catalog(d with {Standard=s with {Support=[s.Support[0] with {MassRegion="nozzle.mesh"}]}}),"decorative support forbidden");
        Reject(()=>Catalog(d with {Standard=s with {Support=[s.Support[0] with {MaximumLoadN=double.PositiveInfinity}]}}),"finite support bound");
        Reject(()=>Catalog(d with {Standard=s with {StoreLaws=[]}}),"missing depletion law");
        Reject(()=>Catalog(d with {Standard=s with {StoreLaws=[s.StoreLaws[0] with {DensityKgM3=2000}]}}),"hidden tank inventory");
        Reject(()=>Catalog(d with {Standard=s with {StoreLaws=[s.StoreLaws[0] with {InnerRadiusM=.6}]}}),"inverted store region");
        Reject(()=>Catalog(d with {Construction=d.Construction! with {StoreGeometry=[d.Construction.StoreGeometry[0] with {InertiaPerKg=Matrix3.Identity}]}}),"independent distribution tensor");
        Reject(()=>Catalog(d with {Standard=s with {Routes=[new("bad","battery","fuel",true)]}}),"cross-service internal route");
        Reject(()=>Catalog(d with {Standard=s with {Routes=[new("bad","battery","absent",true)]}}),"unresolved route");
        Reject(()=>Catalog(d with {Standard=s with {Routes=[new("bad","battery","load",false)]}}),"wire direction semantics");
        Reject(()=>Catalog(d with {Standard=s with {Ports=s.Ports.SetItem(0,s.Ports[0] with {Electrical="battery"})}}),"multiple port owners");
        Reject(()=>Catalog(d with {Standard=s with {RequiredCommandLoads=[]}}),"command needs power");
        Reject(()=>Catalog(d with {Standard=s with {RequiredCommandLoads=["battery"]}}),"battery is not command load");
        var m=s.Mechanical[0];var plug=m with {Role=MateRole.Plug};
        Check(PartStandard.CanMate(m,plug,90),"indexed mate");
        Check(!PartStandard.CanMate(m,m,0),"opposed roles");
        Check(!PartStandard.CanMate(m,plug with {Size="NC-1"},0),"equal size");
        Check(!PartStandard.CanMate(m,plug with {ClockDegrees=[0]},90),"clock intersection");
        Check(!PartStandard.CanMate(m,plug,360),"canonical clock identity");
        var before=c.Save();var json=Encoding.UTF8.GetString(before);
        Reject(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(json.Replace("\"rootEligible\":true","\"rootEligible\":true,\"inferredMeshMass\":true"))),"unknown fields");
        Check(before.SequenceEqual(c.Save()),"refusals preserve source");
        var stock=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn").Data;
        var extended=stock.Definitions[0] with {Standard=s};
        var changedStock=stock with {Definitions=stock.Definitions.SetItem(0,extended),Design=stock.Design with {
            Instances=stock.Design.Instances.Select(i=>i.Definition.Id==extended.Id?
                i with {Definition=i.Definition with {Digest=AssemblyJson.Digest(extended)}}:i).ToImmutableArray()}};
        Reject(()=>CompiledAssemblyDesign.Compile(AssemblyJson.Write(changedStock)),"closed legacy assembly rejects Part Standard extension");
        Console.WriteLine($"Modular Part Standard baseline PASS: {checks} checks; catalog {c.Digest}; full Gate 1 also requires red-team probes");
    }
    internal static void PartStandardRedTeam()
    {
        var d=DefinitionFixture();var s=d.Standard!;var c=d.Construction!;
        var passthrough=new PartServicePort("passthrough",ConstructionService.Propellant,"missing.species","mount",null,null,null,false);
        var unresolved=d with {Construction=c with {Interfaces=[c.Interfaces[0] with {Services=ConstructionService.Electricity|ConstructionService.Propellant}]},
            Standard=s with {Ports=s.Ports.Add(passthrough)}};
        var unresolvedAccepted=false;
        try{_=Catalog(unresolved);unresolvedAccepted=true;}catch(InvalidDataException){}
        var registered=unresolved with {Standard=unresolved.Standard! with {Ports=s.Ports.Add(passthrough with {Resource="passthrough.fluid"})}};
        var first=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.PartStandardSchema,
            [new("test.fluid",1,"Test fluid"),new("passthrough.fluid",1,"Passthrough")],[registered]));
        var second=AssemblyDefinitionCatalog.Compile(first.Data with {Resources=first.Data.Resources.Select(r=>r.Id=="passthrough.fluid"?r with {Revision=2}:r).ToImmutableArray()});
        var placed=new PartInstanceData("root",0,first.Reference(first.Data.Definitions[0]),new(Double3.Zero,Matrix3.Identity));
        var omittedDependency=first.DependencyDigest([placed])==second.DependencyDigest([placed]);
        var radial=new PartMechanicalInterface("radial",MechanicalKind.Radial,"nc.radial-equipment",1,"R-1",MateRole.Socket,[0]);
        var malformed=d with {Attachments=d.Attachments.Add(new("radial",PartStandard.FamilyKey(radial),new(Double3.Zero,Matrix3.Identity))),
            Construction=c with {Interfaces=c.Interfaces.Add(new("radial",ConstructionService.None,false))},
            Standard=s with {Mechanical=s.Mechanical.Add(radial),SocketGroups=[new("group",new(Double3.Zero,Matrix3.Identity),["radial"],[null!])]}};
        string? refusal=null;
        try{_=AssemblyDefinitionCatalog.Load(AssemblyJson.Write(new ConstructionCatalogData(AssemblyDefinitionCatalog.PartStandardSchema,[new("test.fluid",1,"Test fluid")],[malformed])));}
        catch(Exception e){refusal=e.GetType().Name;}
        Console.WriteLine(JsonSerializer.Serialize(new {unresolvedPortResourceAccepted=unresolvedAccepted,
            passthroughResourceRevisionMissingFromDependencyDigest=omittedDependency,nullPlacementRefusal=refusal}));
        if(unresolvedAccepted||omittedDependency||refusal!=nameof(InvalidDataException))
            throw new InvalidOperationException("Gate 1 RED TEAM FAIL: resource-identity or malformed-placement defect.");
        Console.WriteLine("Modular Part Standard red team PASS");
    }
}
