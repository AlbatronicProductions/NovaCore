using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class ScalableLaunchSupportFixture
{
    internal const string Assets="assets/vehicles/modular-starter";
    internal static CompiledConstructionDesign Document(AssemblyDefinitionCatalog catalog,string[] tanks,int members=8,bool fill=true)
    {
        using var s=new ConstructionEditorSession(catalog);
        DefinitionReference D(string id)=>catalog.Reference(catalog.Data.Definitions.Single(d=>d.Id==id));
        s.PreviewRoot(0,D("nc.core.command-2"),"core","support-qualification",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
        void Add(string id,string parent,string target,string child,string name,int count=1){s.PreviewPlacement(s.Revision,new(D(id),parent,target,child,0,count,name));s.AcceptPreview(s.Revision);}
        for(var i=0;i<tanks.Length;i++)Add(tanks[i],i==0?"core":$"tank{i-1}-0","aft","fore",$"tank{i}");
        Add("nc.mount.single-2to1",$"tank{tanks.Length-1}-0","aft","fore","adapter");Add("nc.engine.main-1","adapter-0","engine","fore","engine");
        for(var i=0;i<tanks.Length;i++)Add("nc.rcs.block-r1",$"tank{i}-0","radial-1","mount",$"ring{i}",members);
        if(fill)s.FillForLaunch(s.Revision);return s.Current!.Design;
    }
}
