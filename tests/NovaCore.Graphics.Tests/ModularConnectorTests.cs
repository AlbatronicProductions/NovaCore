using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.ConstructionEditor;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class ModularConnectorTests
{
    internal static void Run()
    {
        var checks=0;void Need(bool value,string why){checks++;if(!value)throw new InvalidDataException("Connector graph: "+why);}
        const string root="assets/vehicles/modular-starter";
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(root,"catalog.json")));
        var fit=new PartCompatibilityEvaluator(catalog);
        PartDefinitionData D(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        var meshes=catalog.Data.Definitions.ToDictionary(d=>d.Id,d=>{var a=d.Construction!.Asset;return PartVisualPicking.NeutralComposite(PartVisualLoader.Load(Path.Combine(root,a.RelativePath),a.Id+"/"+a.Revision,a.Sha256));});
        ConstructionEditorSession New(AssemblyPose pose){var s=new ConstructionEditorSession(catalog,fit);s.PreviewRoot(s.Revision,catalog.Reference(D("nc.core.command-2")),"root","connector-test",pose);s.AcceptPreview(s.Revision);return s;}
        void Place(ConstructionEditorSession s,string def,string host,string target,string mount,string prefix,int count=1){
            var before=s.Save(s.Revision);s.PreviewPlacement(s.Revision,new(catalog.Reference(D(def)),host,target,mount,0,count,prefix));
            var preview=s.Preview!.Design.Save();Need(s.Save(s.Revision).SequenceEqual(before),"preview source immutability");s.AcceptPreview(s.Revision);
            Need(s.Save(s.Revision).SequenceEqual(preview),"commit is exact preview");
        }
        List<EditorSocketTarget> Targets(ConstructionEditorSession s,string child,int count,EditorCamera camera,string? selected=null){
            var targets=new List<EditorSocketTarget>();EditorSocketTargets.Rebuild(targets,s.Current!.Design,D(child),count,0,selected,[],camera,1600,900,meshes);return targets;
        }
        var camera=new EditorCamera{Aspect=1600d/900};
        var blockDefinition=D("nc.rcs.block-r1");
        Need(blockDefinition.Revision==2&&blockDefinition.Standard!.Clearance.IsEmpty,"RCS revision separates placement from provisional plume volume");
        Need(blockDefinition.Standard!.Collision.Length==5&&blockDefinition.Construction!.Consumers.Length==4,"body and four physical nozzles/actuators retained");
        using(var ordinary=New(new(Double3.Zero,Matrix3.Identity))){
            Place(ordinary,"nc.tank.short-2","root","aft","fore","tank");camera.Target=new(-1.5,0,0);camera.Pitch=.2;
            var targets=Targets(ordinary,"nc.tank.short-2",8,camera);
            var end=targets.Single(t=>t.Parent=="tank-0"&&t.Target=="aft");
            Need(end.Count==1&&end.Available&&end.Visible,"stack count remains singular with radial setting eight");
            var host=ordinary.Current!.Design.Parts.Single(p=>p.Instance.Id=="tank-0");var delta=end.Point-camera.Eye;var inv=host.Instance.Pose.Rotation.Transpose();
            var oldHit=PartVisualPicking.Intersect(meshes[host.Definition.Id],inv.Apply(camera.Eye-host.Instance.Pose.Position),inv.Apply(delta.Normalized()));
            Need(oldHit is {} h&&h<Math.Sqrt(delta.LengthSquared)-Math.Sqrt(3)*.065/2,"pre-correction own-host occlusion witness");
            Need(!EditorSocketTargets.TryMarker(ordinary.Current.Design.Parts,meshes,"tank-0",end.Point,new(8,0,0),.1,out _,out _),"separate foreground core still occludes tank connector");
            foreach(var count in new[]{1,2,4,8}){
                camera.Target=new(-.75,0,0);var radial=Targets(ordinary,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent=="tank-0"&&t.Target=="radial-1");
                Need(radial.Count==count&&radial.Visible&&radial.Available,"authored radial count exposed");
                var before=ordinary.Save(ordinary.Revision);Place(ordinary,"nc.rcs.block-r1","tank-0","radial-1","mount","rcs",count);
                Need(!Targets(ordinary,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent=="tank-0"&&t.Target=="radial-1").Available,"whole symmetry set occupies only its host endpoints");
                var placed=ordinary.Save(ordinary.Revision);ordinary.Undo(ordinary.Revision);Need(ordinary.Save(ordinary.Revision).SequenceEqual(before),"atomic symmetry undo");ordinary.Redo(ordinary.Revision);Need(ordinary.Save(ordinary.Revision).SequenceEqual(placed),"atomic symmetry redo");
                ordinary.Remove(ordinary.Revision,"rcs-0");Need(Targets(ordinary,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent=="tank-0"&&t.Target=="radial-1").Available,"symmetry deletion frees host endpoints");
            }
        }
        foreach(var origin in new[]{new AssemblyPose(Double3.Zero,Matrix3.Identity),new AssemblyPose(new(53,-17,29),PartStandard.Roll(90))}){
            using var s=New(origin);var parent="root";
            for(var i=0;i<64;i++){
                var host=s.Current!.Design.Parts.Single(p=>p.Instance.Id==parent);
                var point=host.Instance.Pose.Then(host.Definition.Attachments.Single(a=>a.Id=="aft").Frame).Position;
                camera.Target=point;camera.Pitch=.2;camera.Distance=10;
                var target=Targets(s,"nc.tank.short-2",1,camera,parent).Single(t=>t.Parent==parent&&t.Target=="aft");
                Need(target.Available&&target.Visible,"free endpoint reachable at depth "+i);
                Need(target.Point==point,"authored endpoint is assembly pose, never marker pose");
                var drawn=camera.Project(target.Marker,1600,900);
                Need(Math.Abs(drawn.X-target.X)<1e-8&&Math.Abs(drawn.Y-target.Y)<1e-8,"marker lift preserves pick pixels");
                Need(EditorSocketTargets.Pick([target],target.X,target.Y)?.Parent==parent,"preview selects the instance endpoint");
                var id="tank"+i;Place(s,"nc.tank.short-2",parent,"aft","fore",id);parent=id+"-0";
                var placed=s.Current.Design.Parts.Single(p=>p.Instance.Id==parent);
                Need((placed.Instance.Pose.Position-origin.Point(new(-1.5*(i+1),0,0))).LengthSquared<1e-20,"independent nested translation");
            }
            foreach(var host in new[]{"tank31-0","tank63-0"}){
                camera.Target=s.Current!.Design.Parts.Single(p=>p.Instance.Id==host).Instance.Pose.Point(new(.75,0,0));
                var radial=Targets(s,"nc.rcs.block-r1",8,camera,host).Single(t=>t.Parent==host&&t.Target=="radial-1");
                Need(radial.Available&&radial.Visible,"radial placement reaches arbitrary depth host");
                Place(s,"nc.rcs.block-r1",host,"radial-1","mount","deep-"+host,8);
            }
            var saved=s.Save(s.Revision);s.Undo(s.Revision);s.Redo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(saved),"deep undo/redo exact");
            using var loaded=new ConstructionEditorSession(catalog,fit);loaded.Load(loaded.Revision,saved);Need(loaded.Save(loaded.Revision).SequenceEqual(saved),"deep save/reload exact");
            s.Remove(s.Revision,"tank1-0");Need(s.Current!.Design.Parts.Length==2,"intermediate deletion removes dependent subtree");
            camera.Target=origin.Point(new(-1.5,0,0));Need(Targets(s,"nc.tank.short-2",1,camera).Single(t=>t.Parent=="tank0-0"&&t.Target=="aft").Available,"deletion restores parent endpoint ownership");
            s.Undo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(saved),"undo intermediate deletion restores entire graph");
        }
        using(var s=New(new(Double3.Zero,Matrix3.Identity))){
            for(var i=0;i<3;i++)Place(s,"nc.tank.short-2",i==0?"root":$"tank{i-1}-0","aft","fore",$"tank{i}");
            camera.Target=new(-3,0,0);camera.Pitch=.2;
            foreach(var count in new[]{1,2,4,8}){
                var candidates=Targets(s,"nc.rcs.block-r1",count,camera);
                for(var host=0;host<3;host++){
                    var name=$"tank{host}-0";var t=candidates.Single(t=>t.Parent==name&&t.Target=="radial-1");
                    Need(t.Available&&t.Visible,"radial identity independent on every host/count");
                    var data=s.PreparePlacement(s.Revision,new(catalog.Reference(D("nc.rcs.block-r1")),name,"radial-1","mount",0,count,$"radials{host}"));
                    Need(data.Symmetry.Last().Placement!.Host==name&&data.Symmetry.Last().Members.Length==count,"symmetry stays on chosen instance");
                    var before=s.Save(s.Revision);s.PreviewEdit(s.Revision,data);
                    Need(s.Save(s.Revision).SequenceEqual(before),"radial preview is immutable");
                    var preview=s.Preview!.Design.Save();s.AcceptPreview(s.Revision);
                    Need(s.Save(s.Revision).SequenceEqual(preview),"nested host radial preview commits exactly");
                    Need(s.Current!.Design.Parts.Length==4+(host+1)*count,"prior host groups do not poison later hosts");
                    Need(!Targets(s,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent==name&&t.Target=="radial-1").Available,"only placed host becomes occupied");
                }
                var all=s.Save(s.Revision);using var reloaded=new ConstructionEditorSession(catalog,fit);reloaded.Load(reloaded.Revision,all);Need(reloaded.Save(reloaded.Revision).SequenceEqual(all),"three-host symmetry save/reload exact");
                var middle=s.Current!.Design.Data.Symmetry.Single(g=>g.Placement!.Host=="tank1-0").BasePart;
                s.Remove(s.Revision,middle);Need(Targets(s,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent=="tank1-0"&&t.Target=="radial-1").Available,"middle radial deletion frees only its host");
                Need(!Targets(s,"nc.rcs.block-r1",count,camera).Single(t=>t.Parent=="tank2-0"&&t.Target=="radial-1").Available,"other host remains occupied after deletion");
                s.Undo(s.Revision);Need(s.Save(s.Revision).SequenceEqual(all),"undo radial deletion restores exact groups");
                if(count!=8)for(var host=2;host>=0;host--)s.Remove(s.Revision,s.Current!.Design.Data.Symmetry.Single(g=>g.Placement!.Host==$"tank{host}-0").BasePart);
            }
            Place(s,"nc.mount.single-2to1","tank2-0","aft","fore","adapter");
            camera.Target=new(-4.9,0,0);var engine=Targets(s,"nc.engine.main-1",1,camera).Single(t=>t.Parent=="adapter-0"&&t.Target=="engine");
            Need(engine.Available&&engine.Visible,"engine connector exposed through its own support mesh");
            Place(s,"nc.engine.main-1","adapter-0","engine","fore","engine");
            Need(s.Current!.Design.Parts.Length==30,"three tanks plus all 24 RCS blocks, core adapter engine committed");
            var stale=s.Current.Design.Data with {Instances=s.Current.Design.Data.Instances.Select(p=>p.Definition.Id==blockDefinition.Id?p with {Definition=p.Definition with {Revision=1}}:p).ToImmutableArray()};
            var exactBefore=s.Save(s.Revision);
            try{s.PreviewEdit(s.Revision,stale);throw new Exception("Retired RCS revision silently substituted");}
            catch(InvalidDataException){Need(s.Save(s.Revision).SequenceEqual(exactBefore),"retired definition identity refuses without substitution or mutation");}
            var beforeInvalid=s.Save(s.Revision);
            try{s.PreviewPlacement(s.Revision,new(catalog.Reference(D("nc.engine.main-1")),"tank2-0","aft","fore",0,1,"bad"));throw new Exception("wrong connector accepted");}
            catch(InvalidDataException){Need(s.Save(s.Revision).SequenceEqual(beforeInvalid),"wrong interface refused unchanged");}
        }
        // Preserve the causal witness for the RETIRED assumption. Its expanding
        // plume intersected hardware, but it was never solid attachment geometry.
        var sample=new Double3(-4.4,.58,0);var local=sample-new Double3(-.75,.6,0);
        var tAlong=(-local.X-.145)/4;var halfWidth=.01+4*Math.Tan(Math.PI/60)*tAlong;
        Need(tAlong>0&&tAlong<1&&Math.Abs(local.Y-.15)<halfWidth&&sample.X>-4.5&&sample.X<-3&&Math.Abs(sample.Y)<.6,"independent retired provisional-volume witness");
        foreach(var center in new[]{new Double3(.615,.762,0),new Double3(.885,.762,0),new Double3(.75,.762,-.11),new Double3(.75,.762,.11)}){
            var tank=D("nc.tank.short-2");var obstacle=new PartConvexVolume("nozzle-obstacle",(from x in new[]{-.002,.002} from y in new[]{-.002,.002} from z in new[]{-.002,.002} select center+new Double3(x,y,z)).ToImmutableArray(),"Independent hardware obstruction at nozzle metal, outside block body");
            var obstructed=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d.Id==tank.Id?d with {Standard=d.Standard! with {Collision=d.Standard.Collision.Add(obstacle)}}:d).ToImmutableArray()});
            using var s=new ConstructionEditorSession(obstructed);s.PreviewRoot(s.Revision,obstructed.Reference(D("nc.core.command-2")),"root","obstructed",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
            s.PreviewPlacement(s.Revision,new(obstructed.Reference(obstructed.Data.Definitions.Single(d=>d.Id==tank.Id)),"root","aft","fore",0,1,"tank"));s.AcceptPreview(s.Revision);
            var before=s.Save(s.Revision);var rev=s.Revision;
            try{s.PreviewPlacement(s.Revision,new(obstructed.Reference(blockDefinition),"tank-0","radial-0","mount",0,1,"rcs"));throw new Exception("Nozzle hardware collision admitted");}
            catch(InvalidDataException e){Need(e.Message.StartsWith("FIT_COLLISION",StringComparison.Ordinal)&&e.Message.Contains("nozzle-",StringComparison.Ordinal),"each solid nozzle still refuses collision");}
            Need(s.Revision==rev&&s.Save(s.Revision).SequenceEqual(before)&&s.Preview is null,"hardware refusal leaves source unchanged");
        }
        {
            var keepout=new PartConvexVolume("required-clearance",(from x in new[]{-.05,.05} from y in new[]{-.25,-.15} from z in new[]{-.05,.05} select new Double3(x,y,z)).ToImmutableArray(),"Adversarial real clearance constraint inside host");
            var constrained=blockDefinition with {Standard=blockDefinition.Standard! with {Clearance=[keepout]}};
            var constrainedCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d.Id==constrained.Id?constrained:d).ToImmutableArray()});
            using var s=new ConstructionEditorSession(constrainedCatalog);s.PreviewRoot(s.Revision,constrainedCatalog.Reference(D("nc.core.command-2")),"root","clearance-witness",new(Double3.Zero,Matrix3.Identity));s.AcceptPreview(s.Revision);
            s.PreviewPlacement(s.Revision,new(constrainedCatalog.Reference(D("nc.tank.short-2")),"root","aft","fore",0,1,"tank"));s.AcceptPreview(s.Revision);var before=s.Save(s.Revision);
            try{s.PreviewPlacement(s.Revision,new(constrainedCatalog.Reference(constrained),"tank-0","radial-0","mount",0,1,"rcs"));throw new Exception("Required clearance silently ignored for RCS");}
            catch(InvalidDataException e){Need(e.Message.StartsWith("FIT_CLEARANCE",StringComparison.Ordinal),"generic clearance validator still applies to RCS definitions");}
            Need(s.Save(s.Revision).SequenceEqual(before),"clearance refusal remains immutable");
        }
        // Availability wins over an occupied target; deterministic ties do not
        // depend on definition/catalog/instance enumeration order.
        var a=new EditorSocketTarget("a","aft","fore",default,100,100,3,false,true,default,.1);
        var b=a with {Parent="b",Available=true,Depth=4};
        Need(EditorSocketTargets.Pick([a,b],100,100)==b&&EditorSocketTargets.Pick([b,a],100,100)==b,"occupied endpoint cannot steal another instance");
        a=a with {Available=true,Depth=4};Need(EditorSocketTargets.Pick([a,b],100,100)==a&&EditorSocketTargets.Pick([b,a],100,100)==a,"stable endpoint tie");
        var c=a with {Parent="c",X=100.6,Depth=2};b=b with {X=100.4,Depth=3};
        var picks=new[]{new[]{a,b,c},new[]{a,c,b},new[]{b,a,c},new[]{b,c,a},new[]{c,a,b},new[]{c,b,a}}.Select(order=>EditorSocketTargets.Pick(order,100,100)).ToArray();
        Need(picks.All(p=>p==picks[0]),"three near-distance candidates order independent");
        Need(EditorSocketTargets.Pick([a with {Visible=false}],100,100) is null,"another-part occlusion remains a refusal");
        Console.WriteLine($"Modular connector graph PASS: {checks} checks; depths 0..64, two root frames, all three tank hosts at 1/2/4/8 symmetry; physical nozzle collisions still refused; no Player PASS.");
    }
}
