using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

// Test assembly only; gameplay must retain the player-authored document.
internal static partial class ModularFloridaTests
{
    private static CompiledConstructionDesign Craft(AssemblyDefinitionCatalog catalog,bool longer)
    {
        var parts=ImmutableArray.CreateBuilder<PartInstanceData>();var edges=ImmutableArray.CreateBuilder<StructuralEdgeData>();
        PartDefinitionData Def(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        parts.Add(new("core",0,catalog.Reference(Def("nc.core.command-2")),new(Double3.Zero,Matrix3.Identity)));
        void Attach(string id,string definition,int parent,string target,string child)
        {
            var p=parts[parent];var pd=catalog.Resolve(p.Definition);var d=Def(definition);
            var frame=CompiledConstructionDesign.Snap(p.Pose,pd.Attachments.Single(a=>a.Id==target),d.Attachments.Single(a=>a.Id==child));
            parts.Add(new(id,parts.Count,catalog.Reference(d),frame));
            var services=pd.Construction!.Interfaces.Single(a=>a.Interface==target).Services&d.Construction!.Interfaces.Single(a=>a.Interface==child).Services;
            edges.Add(new(p.Id,target,id,child,new("joint-"+id,false,services,0)));
        }
        Attach("tank",longer?"nc.tank.long-2":"nc.tank.short-2",0,"aft","fore");
        Attach("adapter","nc.mount.single-2to1",1,"aft","fore");Attach("engine","nc.engine.main-1",2,"engine","fore");
        var tank=catalog.Resolve(parts[1].Definition);var group=tank.Standard!.SocketGroups.Single();
        var set=group.Placements.Single(s=>s.Anchor=="radial-0"&&s.Count==8);
        foreach(var socket in set.Sockets)Attach("block-"+socket,"nc.rcs.block-r1",1,socket,"mount");
        var basis=parts[4];var members=parts.Skip(4).Select(p=>new ConstructionSymmetryMember(p.Id,
            p==basis?new(Double3.Zero,Matrix3.Identity):PartStandard.Inverse(basis.Pose).Then(p.Pose))).ToImmutableArray();
        var instances=parts.ToImmutable();
        var config=instances.Select(p=>{var d=catalog.Resolve(p.Definition);return new ConstructionConfiguration(p.Id,
            d.Stores.Select(s=>new ConstructionStoreInitial(s.Id,s.CapacityKg,true)).ToImmutableArray(),
            d.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,e.CapacityJ,true)).ToImmutableArray());}).ToImmutableArray();
        return CompiledConstructionDesign.Compile(catalog,new(CompiledConstructionDesign.CraftSchema,"starter-test",1,catalog.DependencyDigest(instances),"core","core",instances,edges.ToImmutable(),[],config,
            [new("attitude",basis.Id,members,new("tank",group.Id,set.Anchor,8,set.Sockets,group.Axis,basis.Definition))],[],new("Test assembly only",1)));
    }
}
