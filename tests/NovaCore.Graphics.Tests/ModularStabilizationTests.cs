using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.ConstructionEditor;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class ModularStabilizationTests
{
    internal static void Run()
    {
        var checks=0;void Need(bool v,string why){checks++;if(!v)throw new InvalidDataException(why);}
        const string root="assets/vehicles/modular-starter";
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(root,"catalog.json")));
        PartDefinitionData D(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        var visuals=catalog.Data.Definitions.ToDictionary(d=>d.Id,d=>{var a=d.Construction!.Asset;return PartVisualLoader.Load(Path.Combine(root,a.RelativePath),a.Id+"/"+a.Revision,a.Sha256);});
        var meshes=visuals.ToDictionary(p=>p.Key,p=>PartVisualPicking.NeutralComposite(p.Value));
        foreach(var depth in new[]{1,2,4,16,64,96}){
            var data=StabilizationCraftFixture.Create(catalog,depth);using var s=new ConstructionEditorSession(catalog);
            s.Load(0,AssemblyJson.Write(data));var saved=s.Save(s.Revision);
            var distance=0d;
            for(var i=0;i<depth;i++){distance+=i%2==1?3:1.5;Need(Math.Abs(s.Current!.Design.Parts.Single(p=>p.Instance.Id==$"t{i}-0").Instance.Pose.Position.X+distance)<1e-9,"independent mixed-depth transform");}
            s.Remove(s.Revision,"adapter-0");s.Undo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(saved),"nested delete/undo restores exact data");
            s.Redo(s.Revision);s.Undo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(saved),"nested redo/undo exact");
            using var loaded=new ConstructionEditorSession(catalog);loaded.Load(0,saved);Need(loaded.Save(loaded.Revision).SequenceEqual(saved),"mixed-depth deterministic persistence");
            if(depth>=4){s.FillForLaunch(s.Revision);var before=s.Save(s.Revision);var craft=CraftCompiler.Compile(catalog,s.Current!.Design.Data,root);
                Need(craft.Function,"all authored attitude jets retain FUNCTION independently of contact admission");
                try{CraftLaunchAdmission.Prepare(catalog,before,root,CraftLaunchAdmission.SourceHash(before),s.Current.Design.Digest,craft.Digest,catalog.Digest);var profile=new CompiledCraftContact(craft);_=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftSupportPreparation.Prepare(profile,craft.InitialMass,new(0,-9.81,0));if(depth>=16)throw new Exception("Unsupported flight accepted");}catch(InvalidDataException e){Need((e.Message.Contains("capacity",StringComparison.Ordinal)||e.Message.Contains("load exceeded",StringComparison.Ordinal)),"physical reason for unsupported flight: "+e.Message);Need(s.Save(s.Revision).SequenceEqual(before),"physical launch refusal preserves document");}}
            Console.WriteLine($"STABILIZATION depth={depth} parts={data.Instances.Length} saveBytes={saved.Length} PASS");
        }
        foreach(var members in new[]{1,8}){
            using var s=new ConstructionEditorSession(catalog);s.PreviewRoot(0,catalog.Reference(D("nc.core.command-2")),"core","move",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
            void Add(string d,string p,string t,string m,string id,int n=1){s.PreviewPlacement(s.Revision,new(catalog.Reference(D(d)),p,t,m,0,n,id));s.AcceptPreview(s.Revision);}
            Add("nc.tank.short-2","core","aft","fore","a");Add("nc.tank.short-2","a-0","aft","fore","b");
            Add("nc.rcs.block-r1","a-0","radial-0","mount","moving",members);Add("nc.rcs.block-r1","b-0","radial-4","mount","block");
            var camera=new EditorCamera{Aspect=16d/9,Pitch=.2,Target=new(-2.25,0,0)};var targets=new List<EditorSocketTarget>();
            EditorSocketTargets.Rebuild(targets,s.Current!.Design,D("nc.rcs.block-r1"),members==1?8:1,0,"moving-0",s.SelectionMembers("moving-0"),camera,1600,900,meshes);
            var target=targets.Single(t=>t.Parent=="b-0"&&t.Target=="radial-0");Need(target.Count==members&&target.Available==(members==1),"reconnect candidates use existing membership");
            Need(targets.Single(t=>t.Parent=="a-0"&&t.Target=="radial-0").Available,"same-host reconnect ignores own occupancy");
            var before=s.Save(s.Revision);
            if(members==1){s.PreviewCraftReconnect(s.Revision,"moving-0","b-0","radial-0",0);s.AcceptPreview(s.Revision);s.Undo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(before),"one member reconnect is exactly undoable");}
            else {try{s.PreviewCraftReconnect(s.Revision,"moving-0","b-0","radial-0",0);throw new Exception("Occupied target accepted");}catch(InvalidDataException){Need(s.Save(s.Revision).SequenceEqual(before),"occupied sibling refusal is immutable");}}
        }
        var dense=StabilizationCraftFixture.Create(catalog,100,true);using var large=new ConstructionEditorSession(catalog);large.Load(0,AssemblyJson.Write(dense));
        var entries=dense.Instances.Sum(p=>visuals[p.Definition.Id].Meshes.Length);
        var maximumMeshes=visuals.Values.Max(v=>v.Meshes.Length);var placement=catalog.Data.Definitions.SelectMany(d=>d.Standard!.SocketGroups).SelectMany(g=>g.Placements).Max(p=>p.Count);
        var maximumActuators=catalog.Data.Definitions.Max(d=>d.Construction!.Consumers.Length);
        var capacity=EditorRenderCapacity.Required(maximumMeshes,placement,maximumActuators);
        Need(dense.Instances.Length==901&&entries==4101&&entries>4096&&entries<capacity,"admitted dense witness crosses old renderer bound");
        // Independent upper-bound accounting includes a refused eight-member
        // draft beyond a full document, every marker and a free held mesh set.
        Need(capacity>=1024*maximumMeshes+placement*maximumMeshes+1024+maximumMeshes,"all reachable drawing classes fit");
        Need(capacity>=1024*(maximumMeshes+maximumActuators)+1,"unified authored actuator plume/slab headroom");
        using(var full=new ConstructionEditorSession(catalog)){
            full.Load(0,AssemblyJson.Write(StabilizationCraftFixture.Create(catalog,113,true)));var parent="t112-0";
            for(var i=0;i<6;i++){full.PreviewPlacement(full.Revision,new(catalog.Reference(D("nc.tank.short-2")),parent,"aft","fore",0,1,$"extra{i}"));full.AcceptPreview(full.Revision);parent=$"extra{i}-0";}
            Need(full.Current!.Design.Parts.Length==1024,"exact admitted document capacity");var before=full.Save(full.Revision);
            var refused=full.PreparePlacement(full.Revision,new(catalog.Reference(D("nc.rcs.block-r1")),parent,"radial-1","mount",0,8,"over"));
            Need(refused.Instances.Length==1032&&refused.Instances.Sum(p=>visuals[p.Definition.Id].Meshes.Length)+1024+maximumMeshes<=capacity,"reachable refused full-group draft fits presentation");
            try{full.PreviewEdit(full.Revision,refused);throw new Exception("Overcapacity accepted");}catch(InvalidDataException){Need(full.Save(full.Revision).SequenceEqual(before),"exact-boundary refusal is immutable");}
        }
        var camera2=new EditorCamera{Aspect=16d/9,Target=new(-75,0,0),Distance=210};var markers=new List<EditorSocketTarget>();
        var overflow=EditorSocketTargets.Rebuild(markers,large.Current!.Design,D("nc.rcs.block-r1"),1,0,"t99-0",[],camera2,1600,900,meshes);
        Need(!overflow&&markers.Count==800&&markers.Count(t=>t.Parent=="t99-0")==8,"dense deep selected host stays reachable");
        var markerData=StabilizationCraftFixture.Create(catalog,140);large.Load(large.Revision,AssemblyJson.Write(markerData),true);
        camera2.Target=new(-150,0,0);camera2.Distance=450;
        overflow=EditorSocketTargets.Rebuild(markers,large.Current!.Design,D("nc.rcs.block-r1"),1,0,"t139-0",[],camera2,1600,900,meshes);
        Need(overflow&&markers.Count==EditorSocketTargets.DisplayCapacity&&markers.Count(t=>t.Parent=="t139-0")==8,"selected later host survives marker truncation");
        Console.WriteLine($"Modular stabilization PASS: {checks} checks; denseParts=901 renderEntries={entries} preparedCapacity={capacity}; no Player PASS.");
    }
}


