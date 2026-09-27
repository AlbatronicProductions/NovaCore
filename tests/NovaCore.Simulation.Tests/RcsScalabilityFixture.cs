using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

// Qualification data only. The stock catalog and its support/load laws are never
// modified. A distinct smaller finite store exercises generic >64-jet flight.
internal static class RcsScalabilityFixture
{
    internal const string Assets="assets/vehicles/modular-starter";
    internal const string HalfTank="nc.test.tank.short-half";
    internal static AssemblyDefinitionCatalog Catalog(bool half=false)
    {
        var source=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        if(!half)return source;
        var original=source.Data.Definitions.Single(d=>d.Id=="nc.tank.short-2");
        var laws=original.Standard!.StoreLaws.Select(l=>l with {LengthM=l.LengthM/2,Provenance="RcsScalabilityFixture: half-length concentric finite stores inside unchanged conservative shell."}).ToImmutableArray();
        var stores=original.Stores.Select(s=>s with {CapacityKg=s.CapacityKg/2}).ToImmutableArray();
        var geometry=original.Construction!.StoreGeometry.Select(g=>{
            var law=laws.Single(l=>l.Store==g.Store);var r2=law.InnerRadiusM*law.InnerRadiusM+law.OuterRadiusM*law.OuterRadiusM;
            return g with {UsableVolumeM3=g.UsableVolumeM3/2,InertiaPerKg=Matrix3.Diagonal(new(r2/2,r2/4+law.LengthM*law.LengthM/12,r2/4+law.LengthM*law.LengthM/12))};
        }).ToImmutableArray();
        var definition=original with {Id=HalfTank,Stores=stores,Standard=original.Standard with {StoreLaws=laws},Construction=original.Construction with {
            Name="Qualification half-capacity short tank",StoreGeometry=geometry,
            PhysicalSource=new(HalfTank+".physics",1,AssemblyJson.Digest(new {original.Construction.PhysicalSource,stores,laws,geometry}),"RcsScalabilityFixture.cs analytic definition; qualification only, not a production part.")}};
        return AssemblyDefinitionCatalog.Compile(source.Data with {Definitions=source.Data.Definitions.Add(definition)});
    }
    internal static CompiledConstructionDesign Document(AssemblyDefinitionCatalog catalog,int tanks,int members=8,string tank="nc.tank.short-2")
    {
        if(tanks>16)return BulkDocument(catalog,tanks,tank);
        using var s=new ConstructionEditorSession(catalog);
        DefinitionReference D(string id)=>catalog.Reference(catalog.Data.Definitions.Single(d=>d.Id==id));
        s.PreviewRoot(0,D("nc.core.command-2"),"core","rcs-qualification",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
        void Add(string id,string parent,string target,string child,string name,int count=1){s.PreviewPlacement(s.Revision,new(D(id),parent,target,child,0,count,name));s.AcceptPreview(s.Revision);}
        for(var i=0;i<tanks;i++)Add(tank,i==0?"core":$"tank{i-1}-0","aft","fore",$"tank{i}");
        Add("nc.mount.single-2to1",$"tank{tanks-1}-0","aft","fore","adapter");Add("nc.engine.main-1","adapter-0","engine","fore","engine");
        for(var i=0;i<tanks;i++)Add("nc.rcs.block-r1",$"tank{i}-0","radial-1","mount",$"ring{i}",members);
        s.FillForLaunch(s.Revision);return s.Current!.Design;
    }
    private static CompiledConstructionDesign BulkDocument(AssemblyDefinitionCatalog catalog,int tanks,string tank)
    {
        // Stress compilation directly, not hundreds of repeated editor rebuilds.
        // Each physical pose still derives from authored connectors and the final
        // graph passes the same fit/identity/compiler checks as an editor save.
        var parts=ImmutableArray.CreateBuilder<PartInstanceData>();var edges=ImmutableArray.CreateBuilder<StructuralEdgeData>();
        PartDefinitionData D(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        parts.Add(new("core",0,catalog.Reference(D("nc.core.command-2")),new(Double3.Zero,Matrix3.Identity)));
        int Add(string id,string definition,int parent,string target,string child){var p=parts[parent];var pd=catalog.Resolve(p.Definition);var d=D(definition);
            var pose=CompiledConstructionDesign.Snap(p.Pose,pd.Attachments.Single(a=>a.Id==target),d.Attachments.Single(a=>a.Id==child));
            var services=pd.Construction!.Interfaces.Single(a=>a.Interface==target).Services&d.Construction!.Interfaces.Single(a=>a.Interface==child).Services;
            var index=parts.Count;parts.Add(new(id,index,catalog.Reference(d),pose));edges.Add(new(p.Id,target,id,child,new("joint-"+id,false,services,0)));return index;}
        var hosts=new int[tanks];for(var i=0;i<tanks;i++)hosts[i]=Add("tank"+i,tank,i==0?0:hosts[i-1],"aft","fore");
        var adapter=Add("adapter","nc.mount.single-2to1",hosts[^1],"aft","fore");Add("engine","nc.engine.main-1",adapter,"engine","fore");
        for(var i=0;i<tanks;i++)Add("block"+i,"nc.rcs.block-r1",hosts[i],"radial-1","mount");
        var instances=parts.ToImmutable();var config=instances.Select(p=>{var d=catalog.Resolve(p.Definition);return new ConstructionConfiguration(p.Id,
            d.Stores.Select(s=>new ConstructionStoreInitial(s.Id,p.Id=="tank0"?s.CapacityKg:0,true)).ToImmutableArray(),
            d.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,e.CapacityJ,true)).ToImmutableArray());}).ToImmutableArray();
        return CompiledConstructionDesign.Compile(catalog,new(CompiledConstructionDesign.CraftSchema,"rcs-bulk",1,catalog.DependencyDigest(instances),"core","core",instances,edges.ToImmutable(),[],config,[],[],new("Generic compilation stress only",1)));
    }
    internal static CompiledCraft Compile(AssemblyDefinitionCatalog catalog,CompiledConstructionDesign d)=>CraftCompiler.Compile(catalog,d.Data,Assets);
}
