using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    internal static void AuthorDesign(string source,string target)
    {
        var catalog=Development();var input=AssemblyJson.Read<ConstructionDesignData>(File.ReadAllBytes(source),4_000_000);
        Check(input.Schema=="novacore.construction-authoring-template/1","explicit authoring template only");
        var candidate=input with {Schema=CompiledConstructionDesign.Schema,
            Instances=input.Instances.Select(p=>p with {Definition=catalog.Reference(catalog.Data.Definitions.Single(d=>d.Id==p.Definition.Id&&d.Revision==p.Definition.Revision))}).ToImmutableArray()};
        candidate=candidate with {DependencyDigest=catalog.DependencyDigest(candidate.Instances)};
        var design=CompiledConstructionDesign.Compile(catalog,candidate);
        File.WriteAllBytes(target,design.Save());Console.WriteLine($"Authored design {design.Data.Id}: {design.Digest}");
    }
    internal static CompiledConstructionDesign StockDevelopment()=>CompiledConstructionDesign.Load(Development(),File.ReadAllBytes(Path.Combine(Root,"assets/vehicles/development/development-design.json")));
    internal static void Graph()
    {
        checks=0;var design=StockDevelopment();var catalog=design.Catalog;
        Check(design.Parts.Length==9&&design.Connections.Length==8,"development topology");
        Check(design.Connections.Count(x=>x.Detachable)==2,"explicit detachable boundaries");
        foreach(var edge in design.Connections.Where(x=>x.Detachable))Check(edge.Services==ConstructionService.Data,"data does not imply power or fuel");
        var split=design.Partition("DLV.Connection.BoosterUpper.Lower.Release");
        Check(split.Length==4&&split.Contains(design.Index("Capsule"))&&!split.Contains(design.Index("Booster")),"future successor membership");
        Check(design.Partition("DLV.Connection.UpperCapsule.Lower.Release").SequenceEqual(new[]{design.Index("Capsule")}),"capsule partition");
        Check(design.Save().SequenceEqual(CompiledConstructionDesign.Load(catalog,design.Save()).Save()),"design roundtrip");
        var reversed=design.Data with {Connections=design.Data.Connections.Reverse().ToImmutableArray(),Instances=design.Data.Instances.Reverse().ToImmutableArray(),Configuration=design.Data.Configuration.Reverse().ToImmutableArray()};
        Check(CompiledConstructionDesign.Compile(catalog,reversed).Digest==design.Digest,"explicit order independent of input enumeration");
        var connection=design.Data.Connections[0];
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {Connections=design.Data.Connections.SetItem(0,connection with {ParentEndpoint="missing"})}),"missing endpoint");
        var releaseIndex=design.Data.Connections.FindIndex(c=>c.Construction!.Detachable);var release=design.Data.Connections[releaseIndex];
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {Connections=design.Data.Connections.SetItem(releaseIndex,release with {Construction=release.Construction! with {Services=ConstructionService.Electricity}})}),"capability escalation");
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {Connections=design.Data.Connections.RemoveAt(0)}),"disconnected design");
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {Root="Capsule"}),"root with parent");
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {ControlPart="Booster"}),"mesh body is not command authority");
        var moved=design.Data.Instances[3] with {Pose=new(new Double3(99,0,0),Matrix3.Identity)};
        Reject(()=>CompiledConstructionDesign.Compile(catalog,design.Data with {Instances=design.Data.Instances.SetItem(3,moved)}),"nearby geometry cannot repair explicit frame mismatch");
        var capsule=design.Parts.Single(p=>p.Instance.Id=="Capsule");
        var nonDlv=design.Data with {Id="nc.proof.capsule",Root="single",ControlPart="single",Instances=[capsule.Instance with {Id="single",Order=0,Pose=new(Double3.Zero,Matrix3.Identity)}],Connections=[],ServiceLinks=[],
            Configuration=[design.Data.Configuration.Single(c=>c.Part=="Capsule") with {Part="single"}],Symmetry=[],Actions=[]};
        nonDlv=nonDlv with {DependencyDigest=catalog.DependencyDigest(nonDlv.Instances)};
        var proof=CompiledConstructionDesign.Compile(catalog,nonDlv);Check(proof.Parts.Length==1&&proof.Connections.Length==0,"different non-DLV construction");
        var growing=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Add(capsule.Definition with {Id="nc.unrelated",Revision=2}),Resources=catalog.Data.Resources.Add(new("new.species",1,"Unrelated"))});
        Check(CompiledConstructionDesign.Load(growing,proof.Save()).Digest==proof.Digest,"unrelated definitions/resources preserve save identity");
        var used=capsule.Definition.Stores[0].ResourceIdentity;
        var modified=AssemblyDefinitionCatalog.Compile(catalog.Data with {Resources=catalog.Data.Resources.Select(r=>r.Id==used?r with {Revision=r.Revision+1}:r).ToImmutableArray()});
        Reject(()=>CompiledConstructionDesign.Load(modified,proof.Save()),"used resource revision changes closure");
        modified=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d==capsule.Definition?d with {Construction=d.Construction! with {Name="Changed"}}:d).ToImmutableArray()});
        Reject(()=>CompiledConstructionDesign.Load(modified,proof.Save()),"used exact part content changes closure");
        var anisotropic=capsule.Definition with {DryMassKg=1,LocalCom=Double3.Zero,LocalInertia=Matrix3.Diagonal(new(1.1,2.3,2.8)),Construction=capsule.Definition.Construction! with {MassRegions=[new("mass",1,Double3.Zero,Matrix3.Diagonal(new(1.1,2.3,2.8)))]}};
        var rotatedCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=[anisotropic]});
        var cosine=Math.Cos(.37);var sine=Math.Sin(.37);var rotated=nonDlv.Instances[0] with {Definition=rotatedCatalog.Reference(rotatedCatalog.Data.Definitions[0]),Pose=new(Double3.Zero,new(cosine,-sine,0,sine,cosine,0,0,0,1))};
        var rotatedDesign=CompiledConstructionDesign.Compile(rotatedCatalog,nonDlv with {Instances=[rotated],DependencyDigest=rotatedCatalog.DependencyDigest([rotated])});
        Check(rotatedDesign.DryMass.Inertia.PhysicalInertia&&Math.Abs(rotatedDesign.DryMass.Inertia.B+1.2*sine*cosine)<1e-14,"arbitrary anisotropic rotation preserves physical symmetric tensor");
        var engine=design.Parts.Single(p=>p.Instance.Id=="Booster.Engine.0");var body=design.Parts.Single(p=>p.Instance.Id=="Booster");
        var snapped=CompiledConstructionDesign.Snap(body.Instance.Pose,body.Definition.Attachments.Single(a=>a.Id=="ENGINE_0"),engine.Definition.Attachments.Single());
        Check((snapped.Position-engine.Instance.Pose.Position).LengthSquared<1e-10,"explicit frame snap");
        Reject(()=>CompiledConstructionDesign.Snap(body.Instance.Pose,body.Definition.Attachments[0],capsule.Definition.Attachments[0]),"incompatible family");
        Reject(()=>CompiledConstructionDesign.ValidateRootedParents([-1,2,1],0),"structural cycle");
        var selectable=new[]{body.Definition,engine.Definition}.Select(d=>d with {Construction=d.Construction! with {
            Interfaces=d.Construction.Interfaces.Select(a=>a with {Services=ConstructionService.Propellant|ConstructionService.Electricity|ConstructionService.Data}).ToImmutableArray()}}).ToImmutableArray();
        var fixtureCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=selectable});
        var chosen=new[]{"Booster","Booster.Engine.0","Booster.Engine.1"};
        var chosenParts=design.Data.Instances.Where(p=>chosen.Contains(p.Id)).Select((p,i)=>p with {Order=i,Definition=fixtureCatalog.Reference(fixtureCatalog.Data.Definitions.Single(d=>d.Id==p.Definition.Id))}).ToImmutableArray();
        var fixture=design.Data with {Id="nc.proof.service-triangle",DependencyDigest=fixtureCatalog.DependencyDigest(chosenParts),ControlPart=null,Instances=chosenParts,
            Connections=design.Data.Connections.Where(c=>chosen.Contains(c.Parent)&&chosen.Contains(c.Child)).Select(c=>c with {Construction=c.Construction! with {Services=ConstructionService.Propellant|ConstructionService.Electricity|ConstructionService.Data}}).ToImmutableArray(),
            Configuration=design.Data.Configuration.Where(c=>chosen.Contains(c.Part)).ToImmutableArray(),
            ServiceLinks=[new("crossfeed",chosen[1],"ENGINE_MOUNT",chosen[2],"ENGINE_MOUNT",ConstructionService.Propellant,true)],
            Actions=[new("main",0,0,chosen[1],"Main",null)]};
        var triangular=CompiledConstructionDesign.Compile(fixtureCatalog,fixture);
        Check(triangular.Connections.All(c=>c.Services.HasFlag(ConstructionService.Electricity))&&triangular.ServiceEdges.Length==3,"generic power/fuel opt-in and service cycle");
        var directed=CompiledConstructionDesign.Compile(fixtureCatalog,fixture with {ServiceLinks=[fixture.ServiceLinks[0] with {Bidirectional=false}]});
        Check(!CompiledConstructionDesign.Load(fixtureCatalog,directed.Save()).ServiceEdges.Single(e=>e.Id=="crossfeed").Bidirectional,"directed propellant service survives save/load");
        Reject(()=>CompiledConstructionDesign.Compile(fixtureCatalog,fixture with {ServiceLinks=[fixture.ServiceLinks[0] with {Bidirectional=false,Services=ConstructionService.Electricity}]}),"one-way electrical link");
        Reject(()=>CompiledConstructionDesign.Compile(fixtureCatalog,fixture with {Actions=[fixture.Actions[0] with {Consumer="missing"}]}),"invalid action target");
        var basePose=chosenParts[1].Pose;var other=chosenParts[2].Pose;
        var relative=new AssemblyPose(basePose.Rotation.Transpose().Apply(other.Position-basePose.Position),basePose.Rotation.Transpose()*other.Rotation);
        var group=new ConstructionSymmetry("pair",chosen[1],[new(chosen[1],new(Double3.Zero,Matrix3.Identity)),new(chosen[2],relative)]);
        Check(CompiledConstructionDesign.Compile(fixtureCatalog,fixture with {Symmetry=[group]}).Data.Symmetry.Length==1,"saved symmetry transforms");
        Reject(()=>CompiledConstructionDesign.Compile(fixtureCatalog,fixture with {Symmetry=[group with {Members=group.Members.SetItem(1,group.Members[1] with {FromBase=new(Double3.Zero,Matrix3.Identity)})}]}),"invalid symmetry transform");
        Console.WriteLine($"Construction Stage 2 PASS: {checks} checks; design {design.Digest}");
    }
}
