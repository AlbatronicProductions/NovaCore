using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static class LaunchReadinessTests
{
    internal static void Run()
    {
        const string assets="assets/vehicles/modular-starter";
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(assets,"catalog.json")));
        var checks=0;void Need(bool value,string label){checks++;if(!value)throw new InvalidDataException(label);}
        using var editor=new ConstructionEditorSession(catalog);
        void Build(AssemblyDefinitionCatalog library,string[] tanks,int rings)
        {
            editor.Clear(editor.Revision,true);
            DefinitionReference D(string id)=>library.Reference(library.Data.Definitions.Single(d=>d.Id==id));
            editor.PreviewRoot(editor.Revision,D("nc.core.command-2"),"core","readiness",new(Double3.Zero,Matrix3.Identity));editor.AcceptPreview(editor.Revision);
            void Add(string id,string parent,string target,string child,string name,int count=1){editor.PreviewPlacement(editor.Revision,new(D(id),parent,target,child,0,count,name));editor.AcceptPreview(editor.Revision);}
            for(var i=0;i<tanks.Length;i++)Add(tanks[i],i==0?"core":$"tank{i-1}-0","aft","fore",$"tank{i}");
            Add("nc.mount.single-2to1",$"tank{tanks.Length-1}-0","aft","fore","adapter");Add("nc.engine.main-1","adapter-0","engine","fore","engine");
            for(var i=0;i<rings;i++)Add("nc.rcs.block-r1",$"tank{i}-0","radial-1","mount",$"rcs{i}",8);
        }
        CompiledCraft Compile()=>CraftCompiler.Compile(catalog,editor.Current!.Design.Data,assets);
        CompiledCraft Admit(CompiledCraft craft)=>CraftLaunchAdmission.Prepare(catalog,craft.Design.Save(),assets,CraftLaunchAdmission.SourceHash(craft.Design.Save()),craft.Design.Digest,craft.Digest,catalog.Digest);
        string Refuse(CompiledCraft craft,params string[] codes)
        {
            var before=editor.Save(editor.Revision);var revision=editor.Revision;var history=editor.UndoCount;
            Need(codes.All(code=>craft.Diagnostics.Concat(craft.AdmissionDiagnostics).Any(d=>d.Code==code)),"actual failed predicates");
            try{Admit(craft);throw new Exception("Invalid craft admitted");}
            catch(InvalidDataException e){
                Need(editor.Save(editor.Revision).SequenceEqual(before)&&editor.Revision==revision&&editor.UndoCount==history,"refusal is observational");return e.Message;
            }
        }
        foreach(var tanks in new[]{new[]{"nc.tank.short-2"},new[]{"nc.tank.long-2"},new[]{"nc.tank.short-2","nc.tank.short-2"}}){
            Build(catalog,tanks,1);var empty=editor.Save(editor.Revision);var positions=editor.Current!.Design.Data.Instances;
            var reason=Refuse(Compile(),"FUEL_PATH","POWER_PATH");
            Need(reason.Contains("Propellant is unavailable",StringComparison.Ordinal)&&reason.Contains("Electrical power is unavailable",StringComparison.Ordinal)&&!reason.Contains("support",StringComparison.OrdinalIgnoreCase),"empty craft explains fuel and charge without falsely blaming support");
            editor.FillForLaunch(editor.Revision);var full=editor.Save(editor.Revision);var craft=Admit(Compile());
            Need(craft.Function&&craft.AdmissionDiagnostics.IsEmpty&&editor.Current!.Design.Data.Instances.SequenceEqual(positions),"fill admits unchanged geometry and routes");
            editor.Undo(editor.Revision);Need(editor.Save(editor.Revision).SequenceEqual(empty),"fill undo restores exact empty draft");
            editor.Redo(editor.Revision);Need(editor.Save(editor.Revision).SequenceEqual(full),"fill redo restores exact prepared draft");
            editor.Load(editor.Revision,full,true);Need(Admit(Compile()).Digest==craft.Digest,"save/reload retains exact readiness and symmetry");
            editor.ConfigureSelection(editor.Revision,"tank0-0",.375,1,true,true);var partial=editor.Save(editor.Revision);Admit(Compile());
            Need(editor.Save(editor.Revision).SequenceEqual(partial),"launch never silently refills partial tanks");
            editor.ConfigureSelection(editor.Revision,"tank0-0",.375,1,false,true);
            if(tanks.Length==1)Refuse(Compile(),"FUEL_PATH");
            editor.ConfigureSelection(editor.Revision,"core",1,0,true,true);Refuse(Compile(),"POWER_PATH");
            editor.FillForLaunch(editor.Revision);Admit(Compile());
            Console.WriteLine($"LAUNCH_READINESS tanks={string.Join(',',tanks)} parts={craft.Render.Length} empty=REFUSED filled=ADMITTED partial=ADMITTED persistence=PASS");
        }
        Build(catalog,["nc.tank.long-2","nc.tank.long-2"],2);editor.FillForLaunch(editor.Revision);
        var sixtyFour=Admit(Compile());Need(sixtyFour.Function&&sixtyFour.Allocation.JetActuators.Length==64,"64 jets are functional without count refusal");
        var source=editor.Save(editor.Revision);var supported=new CompiledCraftContact(sixtyFour);
        Need(CraftSupportPreparation.Prepare(supported,sixtyFour.InitialMass,new(0,-9.81,0)).MaximumObservedLoad<15000&&editor.Save(editor.Revision).SequenceEqual(source),"vertical-gravity two-long support fixture is load-safe and observational");
        Build(catalog,["nc.tank.short-2"],1);editor.FillForLaunch(editor.Revision);
        var prepared=editor.Save(editor.Revision);var data=editor.Current!.Design.Data;
        foreach(var service in new[]{ConstructionService.Propellant,ConstructionService.Electricity,ConstructionService.Data}){
            var edge=data.Connections.Single(e=>e.Child=="engine-0");
            editor.SetConnection(editor.Revision,edge.Construction! with {Services=edge.Construction.Services&~service});
            Refuse(Compile(),service==ConstructionService.Propellant?"FUEL_PATH":service==ConstructionService.Electricity?"POWER_PATH":"DATA_PATH");
            editor.Load(editor.Revision,prepared,true);Admit(Compile());
        }
        Console.WriteLine($"LAUNCH_READINESS PASS checks={checks}; fresh construction, explicit preparation, disabled/empty services, broken routes, oversized RCS, observational refusal, deterministic history/save/reload");
    }
}
