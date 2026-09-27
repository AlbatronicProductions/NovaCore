using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    internal static void Editor()
    {
        checks=0;var fixture=Plumbing().Design;
        var catalog=AssemblyDefinitionCatalog.Compile(fixture.Catalog.Data with {Definitions=fixture.Catalog.Data.Definitions.Select(d=>d with {
            Attachments=d.Attachments.Select(a=>a with {Frame=a.Frame with {Position=a.Id=="OUT"?new(-1,0,0):a.Id=="OTHER"?new(0,2,0):new(1,0,0)}}).ToImmutableArray()}).ToImmutableArray()});
        var engine=catalog.Data.Definitions.Single(d=>d.Id=="proof.engine");var tank=catalog.Data.Definitions.Single(d=>d.Id=="proof.tank");
        using var editor=new ConstructionEditorSession(catalog);
        editor.PreviewRoot(0,catalog.Reference(engine),"root","player.proof",new(Double3.Zero,Matrix3.Identity));
        Check(editor.Current is null&&editor.Preview!.Design.Data.Instances.Length==1,"ghost does not install root");
        var old=editor.Revision;editor.AcceptPreview(old);var first=editor.Current!;var firstBytes=editor.Save(editor.Revision);
        Reject(()=>editor.AcceptPreview(old),"consumed preview revision");
        Reject(()=>editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"tank1","root","IN","OUT",new("mount1",false,ConstructionService.Electricity)),"unsupported editor service");
        Check(ReferenceEquals(first,editor.Current)&&firstBytes.SequenceEqual(editor.Save(editor.Revision))&&editor.Preview is null,"invalid preview atomic refusal");
        editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"tank1","root","IN","OUT",new("mount1",false,ConstructionService.Propellant));
        var stalePreview=editor.Revision;
        var ghost=editor.Preview;
        Reject(()=>editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"bad","root","IN","missing",new("bad",false,ConstructionService.None)),"refused replacement ghost");
        Check(ReferenceEquals(ghost,editor.Preview)&&editor.Revision==stalePreview,"refusal retains existing ghost and revision");
        editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"tank1","root","IN","OUT",new("mount1",false,ConstructionService.Propellant));
        Reject(()=>editor.AcceptPreview(stalePreview),"superseded ghost");editor.AcceptPreview(editor.Revision);
        var two=editor.Current!;Check(two.Design.Parts[1].Instance.Pose.Position==new Double3(2,0,0),"explicit frame snap");
        editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"tank2","root","OTHER","OUT",new("mount2",false,ConstructionService.Propellant));editor.AcceptPreview(editor.Revision);
        Check(editor.Current!.Design.Parts[1].Instance.Definition==editor.Current.Design.Parts[2].Instance.Definition&&editor.Current.Design.Parts[1].Instance.Id!=editor.Current.Design.Parts[2].Instance.Id,"repeated shared definition distinct placements");
        var config=editor.Current.Design.Data.Configuration.Single(c=>c.Part=="tank1");
        editor.SetConfiguration(editor.Revision,config with {Stores=[new("tank",1,true)]});
        Check(editor.Current!.Fuel.InitiallyPositive==1&&two.Fuel.InitiallyPositive==0,"draft configuration immutable across documents");
        var beforeConfig=editor.Save(editor.Revision);var configRevision=editor.Revision;
        Reject(()=>editor.SetConfiguration(editor.Revision,config with {Stores=[new("tank",2,true)]}),"over-capacity draft configuration");
        Reject(()=>editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"occupied","root","IN","OUT",new("occupied",false,ConstructionService.None)),"occupied editor socket");
        Check(configRevision==editor.Revision&&beforeConfig.SequenceEqual(editor.Save(editor.Revision)),"configuration and occupied-socket refusal atomic");
        editor.PreviewReconnect(editor.Revision,"tank2","tank1","NEXT","OUT",new("mount2",false,ConstructionService.Propellant));editor.AcceptPreview(editor.Revision);
        Check(editor.Current!.Design.Parts[2].Instance.Pose.Position==new Double3(4,0,0),"ordinary non-detachable mount reconnects in editor");
        editor.PreviewAttach(editor.Revision,catalog.Reference(tank),"tank3","tank2","NEXT","OUT",new("mount3",false,ConstructionService.Propellant));editor.AcceptPreview(editor.Revision);
        var before=editor.Save(editor.Revision);var version=editor.Revision;
        Reject(()=>editor.PreviewReconnect(editor.Revision,"tank1","tank3","NEXT","OUT",new("mount1",false,ConstructionService.Propellant)),"editor cycle refusal");
        Check(version==editor.Revision&&before.SequenceEqual(editor.Save(editor.Revision)),"reconnect refusal preserves exact source");
        editor.PreviewReconnect(editor.Revision,"tank2","root","OTHER","OUT",new("mount2",false,ConstructionService.Propellant));editor.AcceptPreview(editor.Revision);
        Check(editor.Current!.Design.Parts[2].Instance.Pose.Position==new Double3(1,2,0)&&editor.Current.Design.Parts[3].Instance.Pose.Position==new Double3(3,2,0),"reparent transforms whole subtree");
        editor.SetConnection(editor.Revision,new("mount1",false,ConstructionService.None));
        Check(editor.Current!.Fuel.Consumers[0].Terms[0].Stores.Length==1,"service edits compile explicit new reachability");
        var p1=editor.Current.Design.Parts[1].Instance;var p2=editor.Current.Design.Parts[2].Instance;
        editor.SetMetadata(editor.Revision,[new("pair","tank1",[new("tank1",new(Double3.Zero,Matrix3.Identity)),new("tank2",new(p2.Pose.Position-p1.Pose.Position,Matrix3.Identity))])],
            [new("engine-start",0,0,"root","engine",null)],[]);
        editor.Rotate(editor.Revision,Matrix3.Mate);
        Check(editor.Current!.Design.Data.Symmetry.Length==1&&editor.Current.Design.Data.Actions.Length==1,"rotation preserves symmetry and action ownership");
        var saved=editor.Save(editor.Revision);var digest=editor.Current.Design.Digest;var identityRevision=editor.Current.Design.Data.Revision;
        editor.Load(editor.Revision,saved);
        Check(saved.SequenceEqual(editor.Save(editor.Revision))&&editor.Current!.Design.Digest==digest&&editor.Current.Design.Data.Revision==identityRevision,"save/reload exact shared design identity");
        using(var other=new ConstructionEditorSession(catalog))
        {other.Load(0,saved);other.SetControl(other.Revision,"tank2");other.Remove(other.Revision,"tank2");Check(editor.Save(editor.Revision).SequenceEqual(saved),"other editor cannot mutate original draft");
            Check(other.Current!.Design.Parts.Length==2&&other.Current.Design.Data.Symmetry.IsEmpty&&other.Current.Design.Data.Actions.Length==1&&other.Current.Design.Data.ControlPart is null,"subtree removal prunes symmetry/control but preserves independent action");}
        Reject(()=>editor.Load(editor.Revision,fixture.Save()),"changed used catalog dependency refused");
        Check(saved.SequenceEqual(editor.Save(editor.Revision)),"invalid load preserves exact draft");
        editor.PreviewReconnect(editor.Revision,"tank2","root","OTHER","OUT",new("mount2",false,ConstructionService.Propellant));
        var cancelled=editor.Revision;editor.CancelPreview(editor.Revision);
        Reject(()=>editor.AcceptPreview(cancelled),"cancel invalidates ghost");
        editor.PreviewReconnect(editor.Revision,"tank2","root","OTHER","OUT",new("mount2",false,ConstructionService.Propellant));
        var beforeLoad=editor.Revision;editor.Load(editor.Revision,saved);
        Reject(()=>editor.AcceptPreview(beforeLoad),"load invalidates ghost even for same design bytes");
        Task.Run(()=>Reject(()=>editor.Clear(editor.Revision),"foreign-thread editor")).GetAwaiter().GetResult();
        editor.Remove(editor.Revision,"root");Check(editor.Current is null&&editor.Preview is null,"root removal yields explicit empty editor");
        Reject(()=>editor.Save(editor.Revision),"empty editor cannot serialize a vehicle");
        editor.Dispose();Reject(()=>editor.Clear(editor.Revision),"retired editor");
        using(var services=new ConstructionEditorSession(PowerPair(true).Fuel.Design.Catalog))
        {
            var design=PowerPair(true).Fuel.Design;services.Load(0,design.Save());
            // Root owns command and a detachable connection action; removing its child prunes that action.
            services.SetMetadata(services.Revision,[],[new("disconnect",0,0,"source",null,"wire")],
                [new("data-hose","source","a","load","b",ConstructionService.Data,true)]);
            services.Remove(services.Revision,"load");
            Check(services.Current!.Design.Data.Actions.IsEmpty&&services.Current.Design.Data.ServiceLinks.IsEmpty,"removal prunes cross-subtree action and service references");
        }
        Console.WriteLine($"Construction Stage 5 core PASS: {checks} checks; isolated revision-bound editor; UI qualification separate");
    }
    internal static void MeasureEditor()
    {
        var d=Plumbing().Design;var bytes=d.Save();using var editor=new ConstructionEditorSession(d.Catalog);editor.Load(0,bytes);
        var rows=new List<Measurement>();
        for(var window=0;window<3;window++)rows.Add(Measure("editor-load-compile-window-"+window,()=>editor.Load(editor.Revision,bytes),64));
        rows.Add(Measure("editor-save",()=>editor.Save(editor.Revision)));
        rows.Add(Measure("editor-rotate-whole",()=>editor.Rotate(editor.Revision,Matrix3.Mate),64));
        rows.Add(Measure("editor-reconnect-preview-cancel",()=>{editor.PreviewReconnect(editor.Revision,"far","near","NEXT","OUT",new("branch",true,ConstructionService.Propellant));editor.CancelPreview(editor.Revision);},64));
        rows.Add(Measure("editor-cached-inspection",()=>{if(editor.Current!.Design.Parts.Length!=3||editor.Current.Fuel.Stores.Length!=4)throw new InvalidDataException("Lost editor facts.");},1024));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {scope="Stage5 cold editor operations; no runtime state",d.Digest,Results=rows},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
}
