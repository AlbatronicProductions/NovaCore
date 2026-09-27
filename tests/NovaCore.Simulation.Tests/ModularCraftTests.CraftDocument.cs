using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static ConstructionConfiguration Configuration(string id,PartDefinitionData part)=>new(id,
        part.Stores.Select(s=>new ConstructionStoreInitial(s.Id,.125,true)).ToImmutableArray(),
        part.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,e.CapacityJ/2,true)).ToImmutableArray());
    private static (AssemblyDefinitionCatalog Catalog,ConstructionDesignData Data) CraftFixture(int count=2)
    {
        var host=CompactCollision(SocketFixture());var block=CompactCollision(DefinitionFixture());var plug=new PartMechanicalInterface("mount",MechanicalKind.Radial,"nc.radial-equipment",1,"R-1",MateRole.Plug,[0]);
        block=block with {Id="block",Attachments=[new("mount",PartStandard.FamilyKey(plug),new(Double3.Zero,Matrix3.Identity))],
            Standard=block.Standard! with {RootEligible=false,Mechanical=[plug]}};
        var catalog=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.PartStandardSchema,[new("test.fluid",1,"Test fluid")],[host,block]));
        host=catalog.Data.Definitions.Single(d=>d.Id=="fixture");block=catalog.Data.Definitions.Single(d=>d.Id=="block");
        var hostPose=new AssemblyPose(Double3.Zero,Matrix3.Identity);var parts=ImmutableArray.CreateBuilder<PartInstanceData>();
        parts.Add(new("host",0,catalog.Reference(host),hostPose));
        var set=host.Standard!.SocketGroups[0].Placements.Single(p=>p.Anchor=="radial.0"&&p.Count==count);
        var edges=ImmutableArray.CreateBuilder<StructuralEdgeData>();var members=ImmutableArray.CreateBuilder<ConstructionSymmetryMember>();
        for(var i=0;i<count;i++){
            var id=i==0?"z-base":"a-member-"+i;
            var pose=CompiledConstructionDesign.Snap(hostPose,host.Attachments.Single(a=>a.Id==set.Sockets[i]),block.Attachments[0]);
            parts.Add(new(id,i+1,catalog.Reference(block),pose));
            edges.Add(new("host",set.Sockets[i],id,"mount",new("joint-"+i,false,ConstructionService.None,0)));
            var fromBase=i==0?new AssemblyPose(Double3.Zero,Matrix3.Identity):PartStandard.Inverse(parts[1].Pose).Then(pose);
            members.Add(new(id,fromBase));
        }
        var instances=parts.ToImmutable();var group=new ConstructionSymmetry("group","z-base",members.ToImmutable(),
            new("host","ring",set.Anchor,count,set.Sockets,host.Standard.SocketGroups[0].Axis,catalog.Reference(block)));
        var config=instances.Select(p=>Configuration(p.Id,catalog.Resolve(p.Definition))).ToImmutableArray();
        return(catalog,new(CompiledConstructionDesign.CraftSchema,"craft.test",7,catalog.DependencyDigest(instances),"host",null,
            instances,edges.ToImmutable(),[],config,[group],[],new("Unpowered draft",1)));
    }
    // These schema/history fixtures use widely separated small solids. The
    // production editor now enforces physical FIT as well as graph validity.
    private static PartDefinitionData CompactCollision(PartDefinitionData d)=>d with {Standard=d.Standard! with {
        Collision=d.Standard.Collision.Select(v=>v with {Vertices=v.Vertices.Select(p=>p*.1).ToImmutableArray()}).ToImmutableArray()}};
    internal static void CraftDocumentGate()
    {
        checks=0;
        foreach(var count in new[]{1,2,4,8}){
            var f=CraftFixture(count);var craft=CompiledConstructionDesign.Compile(f.Catalog,f.Data);var bytes=craft.Save();
            Check(CompiledConstructionDesign.LoadCraft(f.Catalog,bytes).Save().SequenceEqual(bytes),"authored-count deterministic roundtrip");
            Check(craft.Data.Symmetry[0].Members[0].Part=="z-base","ordered members not sorted away from sockets");
            Check(CompiledConstructionDesign.Compile(f.Catalog,f.Data with {Instances=f.Data.Instances.Reverse().ToImmutableArray(),Connections=f.Data.Connections.Reverse().ToImmutableArray(),Configuration=f.Data.Configuration.Reverse().ToImmutableArray()}).Digest==craft.Digest,"nonsemantic input enumeration canonical");
            Reject(()=>CompiledConstructionDesign.Load(f.Catalog,bytes),"legacy reader refuses CraftDocument");
            Reject(()=>ConstructionFuelNetwork.Compile(craft),"unqualified legacy service boundary");
            Reject(()=>{_=craft.DryMass;},"document graph is not CompiledCraft physics");
        }
        var fixture=CraftFixture();var cat=fixture.Catalog;var d=fixture.Data;var accepted=CompiledConstructionDesign.Compile(cat,d);var saved=accepted.Save();
        var rootOnly=OnePartDocument(Catalog(DefinitionFixture())) with {ControlPart=null};
        var incomplete=CompiledConstructionDesign.Compile(Catalog(DefinitionFixture()),rootOnly);
        Check(incomplete.Data.Instances.Length==1&&incomplete.Data.ControlPart is null&&incomplete.Data.Configuration[0].Stores.All(s=>s.QuantityKg==0),"empty unfuelled uncontrolled root remains a saveable draft");
        Check(CompiledConstructionDesign.LoadCraft(incomplete.Catalog,incomplete.Save()).Digest==incomplete.Digest,"incomplete draft roundtrip is separate from flight admission");
        void Bad(ConstructionDesignData candidate,string reason)=>Reject(()=>CompiledConstructionDesign.Compile(cat,candidate),reason);
        Bad(d with {Schema="novacore.craft-document/2"},"unknown version");Bad(d with {Craft=null},"required craft metadata");
        Bad(d with {Craft=new("",1)},"required player name");Bad(d with {Craft=new("test",2)},"reserved action version");
        Bad(d with {DependencyDigest=new string('0',64)},"changed resource closure");
        Bad(d with {Root="z-base"},"ineligible root");
        Bad(d with {Instances=d.Instances.SetItem(1,d.Instances[1] with {Definition=d.Instances[1].Definition with {Digest=new string('0',64)}})},"changed part content");
        var edge=d.Connections[0];
        Bad(d with {Connections=d.Connections.SetItem(0,edge with {Construction=edge.Construction! with {ClockDegrees=null}})},"missing indexed clock");
        Bad(d with {Connections=d.Connections.SetItem(0,edge with {Construction=edge.Construction! with {ClockDegrees=90}})},"radial keyed clock");
        Bad(d with {Connections=d.Connections.SetItem(0,edge with {Construction=edge.Construction! with {Detachable=true}})},"staging excluded");
        var group=d.Symmetry[0];var placement=group.Placement!;
        foreach(var bad in new[]{group with {Placement=null},group with {Members=group.Members.Reverse().ToImmutableArray()},
            group with {Placement=placement with {Count=4}},group with {Placement=placement with {Anchor="radial.1"}},
            group with {Placement=placement with {Sockets=placement.Sockets.Reverse().ToImmutableArray()}},
            group with {Placement=placement with {Axis=new(Double3.UnitX,Matrix3.Identity)}},
            group with {Placement=placement with {Host="z-base"}},
            group with {Placement=placement with {Definition=cat.Reference(cat.Data.Definitions.Single(p=>p.Id=="fixture"))}}})
            Bad(d with {Symmetry=[bad]},"authored symmetry provenance");
        Bad(d with {Actions=[new("fire",0,0,"z-base","engine",null)]},"reserved actions not executable");
        Bad(d with {ServiceLinks=[new("wire","host","mount","z-base","mount",ConstructionService.Electricity,true)]},"external service-line authoring excluded");
        foreach(var field in new[]{"runtimePosition","activeActuators","remainingFlightFuel","contactHandle"}){
            var node=JsonNode.Parse(saved)!;node[field]=0;
            Reject(()=>CompiledConstructionDesign.LoadCraft(cat,Encoding.UTF8.GetBytes(node.ToJsonString())),"runtime state excluded from document");
        }
        foreach(var field in new[]{"craft","instances","connections","configuration","symmetry"}){
            var node=JsonNode.Parse(saved)!;node[field]=null;
            Reject(()=>CompiledConstructionDesign.LoadCraft(cat,Encoding.UTF8.GetBytes(node.ToJsonString())),"null document collection/metadata");
        }
        Check(accepted.Save().SequenceEqual(saved),"failed loads leave prior immutable draft unchanged");
        foreach(var delta in new[]{-1,0,1}){
            var padded=new byte[CompiledConstructionDesign.MaximumDocumentBytes+delta];Array.Fill(padded,(byte)' ');saved.CopyTo(padded,0);
            if(delta<=0)Check(CompiledConstructionDesign.LoadCraft(cat,padded).Save().SequenceEqual(saved),"shared document byte boundary and canonical padding removal");
            else RefuseAt(()=>CompiledConstructionDesign.LoadCraft(cat,padded),"Bounded document size");
        }
        ClockConvention();MigrationCases();
        Console.WriteLine($"Modular Gate 2 CraftDocument PASS: {checks} strict-version/roundtrip/migration checks");
    }
    private static void ClockConvention()
    {
        var parent=DefinitionFixture();var child=DefinitionFixture() with {Id="stack-child"};
        var plug=child.Standard!.Mechanical[0] with {Role=MateRole.Plug};
        child=child with {Standard=child.Standard with {RootEligible=false,Mechanical=[plug]}};
        var cat=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.PartStandardSchema,[new("test.fluid",1,"Test fluid")],[parent,child]));
        parent=cat.Data.Definitions.Single(p=>p.Id=="fixture");child=cat.Data.Definitions.Single(p=>p.Id=="stack-child");
        foreach(var degrees in new[]{0,90,180,270}){
            var p=new PartInstanceData("root",0,cat.Reference(parent),new(Double3.Zero,Matrix3.Identity));
            var c=new PartInstanceData("child",1,cat.Reference(child),new(Double3.Zero,PartStandard.Roll(degrees)*Matrix3.Mate));
            var data=new ConstructionDesignData(CompiledConstructionDesign.CraftSchema,"clock",1,cat.DependencyDigest([p,c]),"root",null,[p,c],
                [new("root","mount","child","mount",new("joint",false,ConstructionService.None,degrees))],[],[Configuration("root",parent),Configuration("child",child)],[],[],new("Clock test",1));
            Check(CompiledConstructionDesign.Compile(cat,data).Parts.Length==2,"explicit parent-interface clock");
            if(degrees is 90 or 270)Reject(()=>CompiledConstructionDesign.Compile(cat,data with {Instances=[p,c with {Pose=new(Double3.Zero,Matrix3.Mate*PartStandard.Roll(degrees))}]}),"noncommuting reversed clock convention");
        }
    }
    private static void MigrationCases()
    {
        var f=CraftFixture();var target=f.Catalog;var targetData=f.Data;
        var legacy=AssemblyDefinitionCatalog.Compile(target.Data with {Schema=AssemblyDefinitionCatalog.Schema,Definitions=target.Data.Definitions.Select(d=>d with {Standard=null}).ToImmutableArray()});
        var instances=targetData.Instances.Select(p=>p with {Definition=legacy.Reference(legacy.Data.Definitions.Single(d=>d.Id==p.Definition.Id))}).ToImmutableArray();
        var data=targetData with {Schema=CompiledConstructionDesign.Schema,Craft=null,Instances=instances,DependencyDigest=legacy.DependencyDigest(instances),
            Connections=targetData.Connections.Select(e=>e with {Construction=e.Construction! with {ClockDegrees=null}}).ToImmutableArray(),
            Symmetry=targetData.Symmetry.Select(g=>g with {Placement=null}).ToImmutableArray()};
        var old=CompiledConstructionDesign.Compile(legacy,data);var bytes=old.Save();
        var recipe=new CraftMigrationRecipe("test.explicit-upgrade",old.Digest,old.Data.DependencyDigest,
            legacy.Data.Definitions.Select(d=>new CraftDefinitionMigration(legacy.Reference(d),target.Reference(target.Data.Definitions.Single(t=>t.Id==d.Id)))).ToImmutableArray(),
            targetData.Connections.Select(e=>new CraftClockMigration(e.Construction!.Id,0)).ToImmutableArray(),
            [new("group",targetData.Symmetry[0].Placement!,targetData.Symmetry[0].Members.Select(m=>m.Part).ToImmutableArray())]);
        var migrated=CraftDocumentMigration.Migrate(legacy,bytes,target,recipe,targetData.Craft!.Name);
        Check(migrated.Digest==CompiledConstructionDesign.Compile(target,targetData).Digest,"explicit migration equals independently authored target");
        Check(bytes.SequenceEqual(old.Save()),"migration preserves source bytes");
        Reject(()=>CompiledConstructionDesign.LoadCraft(legacy,bytes),"legacy bytes need explicit migration");
        Reject(()=>CompiledConstructionDesign.Compile(target,data),"legacy cannot bypass Part Standard via catalog2");
        foreach(var bad in new[]{recipe with {SourceDocumentDigest=new string('0',64)},recipe with {SourceDependencyDigest=new string('0',64)},
            recipe with {Definitions=[]},recipe with {Definitions=recipe.Definitions.Add(recipe.Definitions[0])},
            recipe with {Definitions=recipe.Definitions.SetItem(0,recipe.Definitions[0] with {Target=recipe.Definitions[0].Target with {Digest=new string('0',64)}})},
            recipe with {Clocks=[]},recipe with {Symmetry=[]},recipe with {Symmetry=[recipe.Symmetry[0] with {MemberOrder=["missing","z-base"]}]}})
            Reject(()=>CraftDocumentMigration.Migrate(legacy,bytes,target,bad,"Migrated"),"incomplete or changed migration mapping");
        var changed=AssemblyDefinitionCatalog.Compile(target.Data with {Resources=target.Data.Resources.SetItem(0,target.Data.Resources[0] with {Revision=2})});
        Reject(()=>CraftDocumentMigration.Migrate(legacy,bytes,changed,recipe,"Changed resource"),"no resource substitution during migration");
        // Both catalogs use the same two-resource union. Swapping which part holds
        // each species would nevertheless change its total (one host, two blocks).
        var resources=legacy.Data.Resources.Add(new("second.fluid",1,"Second fluid"));
        var swapOld=AssemblyDefinitionCatalog.Compile(legacy.Data with {Resources=resources,Definitions=legacy.Data.Definitions.Select(p=>p.Id=="block"?
            p with {Stores=p.Stores.Select(s=>s with {ResourceIdentity="second.fluid"}).ToImmutableArray()}:p).ToImmutableArray()});
        var swapTarget=AssemblyDefinitionCatalog.Compile(target.Data with {Resources=resources,Definitions=target.Data.Definitions.Select(p=>p.Id=="fixture"?
            p with {Stores=p.Stores.Select(s=>s with {ResourceIdentity="second.fluid"}).ToImmutableArray(),Standard=p.Standard! with {
                Ports=p.Standard!.Ports.Select(port=>port.Resource is not null?port with {Resource="second.fluid"}:port).ToImmutableArray()}}:p).ToImmutableArray()});
        var swapInstances=data.Instances.Select(p=>p with {Definition=swapOld.Reference(swapOld.Data.Definitions.Single(d=>d.Id==p.Definition.Id))}).ToImmutableArray();
        var swapDocument=CompiledConstructionDesign.Compile(swapOld,data with {Instances=swapInstances,DependencyDigest=swapOld.DependencyDigest(swapInstances)});
        var swapRecipe=recipe with {SourceDocumentDigest=swapDocument.Digest,SourceDependencyDigest=swapDocument.Data.DependencyDigest,
            Definitions=swapOld.Data.Definitions.Select(p=>new CraftDefinitionMigration(swapOld.Reference(p),swapTarget.Reference(swapTarget.Data.Definitions.Single(d=>d.Id==p.Id)))).ToImmutableArray()};
        RefuseAt(()=>CraftDocumentMigration.Migrate(swapOld,swapDocument.Save(),swapTarget,swapRecipe,"Swap"),"each store resource identity");
        Reject(()=>CraftDocumentMigration.Migrate(legacy,bytes,target,recipe with {Definitions=default},"Default map"),"default definition map");
        Reject(()=>CraftDocumentMigration.Migrate(legacy,bytes,target,recipe with {Clocks=recipe.Clocks.Add(recipe.Clocks[0])},"Duplicate clock"),"duplicate migration clock");
        Reject(()=>CraftDocumentMigration.Migrate(legacy,bytes,target,recipe with {Symmetry=[recipe.Symmetry[0] with {MemberOrder=default}]},"Default order"),"default member order");
        foreach(var where in new[]{"craft","clockDegrees","placement"}){
            var node=JsonNode.Parse(bytes)!;
            if(where=="craft")node[where]=null;
            else if(where=="clockDegrees")node["connections"]![0]!["construction"]![where]=null;
            else node["symmetry"]![0]![where]=null;
            Reject(()=>CompiledConstructionDesign.Load(legacy,Encoding.UTF8.GetBytes(node.ToJsonString())),"legacy unknown extension even when null");
        }
        Check(old.Save().SequenceEqual(bytes),"migration refusal preserves old design");
    }
}
