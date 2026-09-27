using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

// Opt-in qualification fixture only. Every resulting document still passes the
// production graph, identity, compatibility and physical collision validators.
internal static class StabilizationCraftFixture
{
    internal static ConstructionDesignData Create(AssemblyDefinitionCatalog catalog,int depth,bool dense=false,bool shortOnly=false,string shortTankDefinition="nc.tank.short-2")
    {
        PartDefinitionData D(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        using var s=new ConstructionEditorSession(catalog);
        s.PreviewRoot(0,catalog.Reference(D("nc.core.command-2")),"core","stabilization",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
        for(var i=0;i<depth;i++){
            s.PreviewPlacement(s.Revision,new(catalog.Reference(D(!dense&&!shortOnly&&i%2==1?"nc.tank.long-2":shortTankDefinition)),i==0?"core":$"t{i-1}-0","aft","fore",0,1,$"t{i}"));s.AcceptPreview(s.Revision);
        }
        var original=s.Current!.Design.Data;var all=original;
        foreach(var i in dense?Enumerable.Range(0,depth):new[]{0,depth/2,depth-1}.Distinct()){
            var next=s.PreparePlacement(s.Revision,new(catalog.Reference(D("nc.rcs.block-r1")),$"t{i}-0","radial-1","mount",0,8,$"r{i}"));
            all=all with {Instances=all.Instances.AddRange(next.Instances.Skip(original.Instances.Length)),Connections=all.Connections.AddRange(next.Connections.Skip(original.Connections.Length)),Configuration=all.Configuration.AddRange(next.Configuration.Skip(original.Configuration.Length)),Symmetry=all.Symmetry.AddRange(next.Symmetry)};
        }
        all=all with {Instances=all.Instances.Select((p,i)=>p with {Order=i}).ToImmutableArray()};
        s.PreviewEdit(s.Revision,all);s.AcceptPreview(s.Revision);
        if(!dense){
            s.PreviewPlacement(s.Revision,new(catalog.Reference(D("nc.mount.single-2to1")),$"t{depth-1}-0","aft","fore",0,1,"adapter"));s.AcceptPreview(s.Revision);
            s.PreviewPlacement(s.Revision,new(catalog.Reference(D("nc.engine.main-1")),"adapter-0","engine","fore",0,1,"engine"));s.AcceptPreview(s.Revision);
        }
        return s.Current!.Design.Data;
    }
}
