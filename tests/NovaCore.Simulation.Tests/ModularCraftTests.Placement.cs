using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void PlayerPlacementGate()
    {
        checks=0;var catalog=StarterCatalog();var fit=new PartCompatibilityEvaluator(catalog);
        PartDefinitionData D(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        ConstructionEditorSession Stack(bool longer)
        {
            var session=new ConstructionEditorSession(catalog,fit);
            session.PreviewRoot(session.Revision,catalog.Reference(D("nc.core.command-2")),"core","player-placement",new(Double3.Zero,Matrix3.Identity));
            session.AcceptPreview(session.Revision);
            Place(session,longer?"nc.tank.long-2":"nc.tank.short-2","core","aft","fore",1,"tank");
            Place(session,"nc.mount.single-2to1","tank-0","aft","fore",1,"adapter");
            Place(session,"nc.engine.main-1","adapter-0","engine","fore",1,"engine");return session;
        }
        void Place(ConstructionEditorSession s,string definition,string parent,string target,string mount,int count,string prefix)
        {s.PreviewPlacement(s.Revision,new(catalog.Reference(D(definition)),parent,target,mount,0,count,prefix));s.AcceptPreview(s.Revision);}
        foreach(var longer in new[]{false,true})foreach(var count in new[]{1,2,4,8})
        {
            using var s=Stack(longer);var before=s.Current!.Design.Save();var history=s.UndoCount;
            var request=new CraftPlacementRequest(catalog.Reference(D("nc.rcs.block-r1")),"tank-0","radial-1","mount",0,count,"rcs");
            var candidate=s.PreparePlacement(s.Revision,request);
            Check(s.Current.Design.Save().SequenceEqual(before)&&s.Preview is null&&s.UndoCount==history,"preparation has no mutation/history");
            s.PreviewEdit(s.Revision,candidate);var preview=s.Preview!.Design;var selected=preview.Data.Symmetry.Single();
            Check(preview.Parts.Length==4+count&&selected.Members.Length==count&&selected.Placement!.Count==count,"all authored ghosts together");
            Check(s.Current.Design.Save().SequenceEqual(before),"ghosts are not committed");
            var oldRevision=s.Revision-1;Reject(()=>s.AcceptPreview(oldRevision),"stale commit");
            Check(s.Preview.Design.Digest==preview.Digest,"stale refusal preserves complete preview");
            s.AcceptPreview(s.Revision);var placed=s.Current.Design.Save();
            Check(s.Current.Design.Digest==preview.Digest&&s.UndoCount==history+1,"preview commit exact and one undo");
            Check(s.SelectionMembers("rcs-0").Length==count,"member selects whole group");
            s.Undo(s.Revision);Check(s.Current!.Design.Save().SequenceEqual(before),"undo removes complete group");
            s.Redo(s.Revision);Check(s.Current!.Design.Save().SequenceEqual(placed),"redo restores all identities");
            var bindings=s.Current.Design.Data.Connections;var poses=s.Current.Design.Data.Instances;
            s.PreviewClock(s.Revision,"rcs-"+(count-1),0);
            Check(s.Preview!.Design.Data.Connections.SequenceEqual(bindings)&&s.Preview.Design.Data.Instances.SequenceEqual(poses),"zero clock on non-base preserves all ID/socket/pose bindings");
            s.CancelPreview(s.Revision);
            s.FillForLaunch(s.Revision);var full=s.Current.Design.Data;
            Check(full.Configuration.Single(c=>c.Part=="tank-0").Stores.Sum(q=>q.QuantityKg)==(longer?1600:800),"typed fill finite tank");
            Check(full.Configuration.Single(c=>c.Part=="core").Electrical.Single(e=>e.Module=="battery").ChargeJ==90000,"typed finite charge");
            s.ConfigureSelection(s.Revision,"rcs-0",1,1,true,false);
            Check(s.Current.Design.Data.Configuration.Where(c=>c.Part.StartsWith("rcs-",StringComparison.Ordinal)).All(c=>c.Electrical.All(e=>!e.Enabled)),"typed group configuration atomic");
            var unchanged=s.Current.Design.Save();var rev=s.Revision;
            Reject(()=>s.ConfigureSelection(s.Revision,"rcs-0",double.NaN,1,true,true),"malformed typed configuration");
            Check(s.Revision==rev&&s.Current.Design.Save().SequenceEqual(unchanged),"failed configuration preserves full document");
            // Move the entire group onto another authored anchor without rewriting IDs/count.
            s.PreviewCraftReconnect(s.Revision,"rcs-0","tank-0","radial-2",0);s.AcceptPreview(s.Revision);
            Check(s.Current.Design.Data.Symmetry.Single().Placement!.Anchor=="radial-2"&&s.SelectionMembers("rcs-0").Length==count,"atomic whole-group reconnection");
            var groupBefore=s.Current.Design.Data.Symmetry.Single().Members.Select(m=>m.Part).ToArray();
            s.PreviewClock(s.Revision,"tank-0",90);s.AcceptPreview(s.Revision);
            Check(s.Current.Design.Data.Symmetry.Single().Members.Select(m=>m.Part).SequenceEqual(groupBefore),"stack clock moves entire dependent group");
            var fitted=s.Current.Design.Save();Reject(()=>s.PreviewClock(s.Revision,"engine-0",45),"undeclared indexed clock");
            Check(s.Current.Design.Save().SequenceEqual(fitted),"bad clock leaves state");
            s.CancelPreview(s.Revision);s.Remove(s.Revision,"rcs-0");
            Check(s.Current.Design.Parts.Length==4&&s.Current.Design.Data.Symmetry.Length==0,"member removal removes complete group");
            s.Undo(s.Revision);Check(s.Current!.Design.Save().SequenceEqual(fitted),"undo grouped deletion");
        }
        using(var blocked=Stack(false))
        {
            Place(blocked,"nc.rcs.block-r1","tank-0","radial-4","mount",1,"blocker");
            var before=blocked.Current!.Design.Save();var count=blocked.UndoCount;var revision=blocked.Revision;
            Reject(()=>blocked.PreviewPlacement(revision,new(catalog.Reference(D("nc.rcs.block-r1")),"tank-0","radial-0","mount",0,8,"eight")),"one of eight occupied refuses all");
            Check(blocked.Current.Design.Save().SequenceEqual(before)&&blocked.UndoCount==count&&blocked.Revision==revision&&blocked.Preview is null,"no 7/8 commit or history");
            Reject(()=>blocked.PreviewPlacement(revision,new(catalog.Reference(D("nc.rcs.block-r1")),"tank-0","radial-0","mount",0,3,"three")),"unsupported group count");
            Reject(()=>blocked.PreviewCraftReconnect(revision,"tank-0","engine-0","fore",0),"own subtree reconnection");
            Check(blocked.Current.Design.Save().SequenceEqual(before),"failed preparation immutable");
        }
        // Physical refusal cannot be bypassed by directly submitting structurally valid draft data.
        var baseCraft=StarterCraft(catalog,false);fit.RequireFit(baseCraft);
        var tank=D("nc.tank.short-2");var badTank=tank with {Standard=tank.Standard! with {Collision=tank.Standard.Collision.Add(new("blocker",[
            new(.6,.59,-.11),new(.9,.59,-.11),new(.6,.91,-.11),new(.9,.91,-.11),new(.6,.59,.11),new(.9,.59,.11),new(.6,.91,.11),new(.9,.91,.11)],"Adversarial authored obstacle at exactly one socket"))}};
        var changed=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(p=>p.Id==tank.Id?badTank:p).ToImmutableArray()});
        var obstacle=StarterCraft(changed,false);var evaluator=new PartCompatibilityEvaluator(changed);
        Reject(()=>evaluator.RequireFit(obstacle),"positive cross-part collision refusal");
        using(var s=new ConstructionEditorSession(changed,evaluator))
        {
            Reject(()=>s.Load(s.Revision,obstacle.Save()),"load shares full fit refusal");
            Check(s.Current is null&&s.Revision==0&&s.UndoCount==0,"fit-refused load leaves source empty");
        }
        using(var s=new ConstructionEditorSession(changed))
            RefuseEdit(s,()=>s.Load(s.Revision,obstacle.Save()),"default player session cannot bypass fit");
        var thin=badTank with {Standard=badTank.Standard! with {Collision=tank.Standard!.Collision.Add(new("thin-blocker",[
            new(.75-.000001,.70,-.05),new(.75+.000001,.70,-.05),new(.75-.000001,.80,-.05),new(.75+.000001,.80,-.05),
            new(.75-.000001,.70,.05),new(.75+.000001,.70,.05),new(.75-.000001,.80,.05),new(.75+.000001,.80,.05)],"Two-micrometre obstacle contained deeply inside a radial block"))}};
        var thinCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(p=>p.Id==tank.Id?thin:p).ToImmutableArray()});
        var thinCraft=StarterCraft(thinCatalog,false);var thinFit=new PartCompatibilityEvaluator(thinCatalog);
        Reject(()=>thinFit.RequireFit(thinCraft),"thin contained body is not near contact merely because it is thin");
        var far=CompiledConstructionDesign.Compile(thinCatalog,thinCraft.Data with {Instances=thinCraft.Data.Instances.Select(p=>p with {Pose=p.Pose with {Position=p.Pose.Position+new Double3(1e20,1e20,1e20)}}).ToImmutableArray()});
        Reject(()=>thinFit.RequireFit(far),"large absolute translation cannot erase overlapping local geometry");
        using(var s=new ConstructionEditorSession(thinCatalog,thinFit))
        {
            Reject(()=>s.Load(s.Revision,thinCraft.Save()),"contained thin obstacle refused on load");
            var dir=Path.Combine("build/modular-craft-first-playable/gate5-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            var recovery=Path.Combine(dir,"blocked-recovery.json");CraftDocumentStore.SaveRecovery(recovery,thinCatalog,thinCraft.Save(),null);
            Reject(()=>s.RestoreRecovery(s.Revision,recovery),"contained thin obstacle refused on recovery");
            Check(s.Current is null&&s.Preview is null&&s.Revision==0&&s.UndoCount==0&&s.RedoCount==0&&!s.Dirty,"load/recovery geometric refusal preserves complete empty state");
        }
        ReconnectServices(catalog);
        var remoteBox=new PartConvexVolume("remote",[new(1e20,-1,-1),new(1e20,1,-1),new(1e20,-1,1),new(1e20,1,1),
            new(1e20+16384,-1,-1),new(1e20+16384,1,-1),new(1e20+16384,-1,1),new(1e20+16384,1,1)],"Full-rank far-offset precision witness");
        var remoteCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d.Id is "nc.core.command-2" or "nc.tank.short-2"?d with {Standard=d.Standard! with {Collision=[remoteBox]}}:d).ToImmutableArray()});
        var remoteCraft=StarterCraft(remoteCatalog,false);var c=Math.Sqrt(.5);var rotation=new Matrix3(c,-c,0,c,c,0,0,0,1);
        var rotated=CompiledConstructionDesign.Compile(remoteCatalog,remoteCraft.Data with {Instances=remoteCraft.Data.Instances.Select(p=>p with {Pose=new(rotation.Apply(p.Pose.Position),rotation*p.Pose.Rotation)}).ToImmutableArray()});
        RefuseAt(()=>new PartCompatibilityEvaluator(remoteCatalog).RequireFit(rotated),"FIT_COLLISION");
        ContactMetricBoundary();
        Console.WriteLine($"Modular Gate 5 operations PASS: {checks} checks; direct viewport and integration qualification still required");
    }
    private static void ContactMetricBoundary()
    {
        // Independent binary-rational witness: an admitted almost-rigid transform
        // cannot enlarge the physical contact band by shortening an SAT axis.
        const double scale=.9999999999998,h=1e-5,offset=9.999999999995003e-6;
        var rotation=new Matrix3(scale,0,0,0,scale,0,0,0,scale);
        Check(rotation.Rigid,"boundary transform is admitted by rigid contract");
        var quantum=System.Numerics.BigInteger.One<<PartStandardExact.QuantumExponent;
        var depth=2*PartStandardExact.Encode(scale)*PartStandardExact.Encode(h)-PartStandardExact.Encode(offset)*quantum;
        Check(depth>PartStandardExact.Encode(PartCompatibilityEvaluator.ContactToleranceMetres)*quantum,"exact independent physical depth exceeds contact band");
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
        var type=typeof(PartCompatibilityEvaluator);var prepare=type.GetMethod("Prepare",flags)!;var overlap=type.GetMethod("Overlap",flags)!;
        var placed=type.GetNestedType("PlacedHull",System.Reflection.BindingFlags.NonPublic)!;
        var vertices=(from x in new[]{-h,h} from y in new[]{-h,h} from z in new[]{-h,h} select new Double3(x,y,z)).ToImmutableArray();
        var hull=prepare.Invoke(null,[vertices])!;
        object At(Double3 p)=>Activator.CreateInstance(placed,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,[hull,new AssemblyPose(p,rotation)],null)!;
        foreach(var axis in new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ}){
            var a=At(Double3.Zero);var b=At(axis*offset);
            Check((bool)overlap.Invoke(null,[a,b])!&&(bool)overlap.Invoke(null,[b,a])!,"scaled normal cannot falsely separate above-band penetration");
            Check(!(bool)overlap.Invoke(null,[a,At(axis*(2*h))])!,"certified separated boundary remains accepted");
        }
    }
    private static void ReconnectServices(AssemblyDefinitionCatalog source)
    {
        var core=source.Data.Definitions.Single(d=>d.Id=="nc.core.command-2");
        var aft=core.Attachments.Single(a=>a.Id=="aft");var mechanical=core.Standard!.Mechanical.Single(m=>m.Interface=="aft");
        var services=ConstructionService.Electricity|ConstructionService.Data;
        core=core with {Attachments=core.Attachments.Add(aft with {Id="test-socket",Frame=aft.Frame with {Position=new(0,10,0)}}),
            Construction=core.Construction! with {Interfaces=core.Construction.Interfaces.Add(new("test-socket",services,false))},
            Standard=core.Standard with {Mechanical=core.Standard.Mechanical.Add(mechanical with {Interface="test-socket"}),
                Ports=core.Standard.Ports.AddRange(core.Standard.Ports.Where(p=>p.Interface=="aft").Select(p=>p with {Id="test-"+p.Id,Interface="test-socket"}))}};
        var catalog=AssemblyDefinitionCatalog.Compile(source.Data with {Definitions=source.Data.Definitions.Select(d=>d.Id==core.Id?core:d).ToImmutableArray()});
        var craft=StarterCraft(catalog,false);using var s=new ConstructionEditorSession(catalog);s.Load(0,craft.Save());
        var before=s.Save(s.Revision);var history=s.UndoCount;
        s.PreviewCraftReconnect(s.Revision,"adapter","core","test-socket",0);
        var edge=s.Preview!.Design.Data.Connections.Single(e=>e.Child=="adapter");
        Check(edge.Construction!.Services==services,"reconnect derives explicit new endpoint services");
        Check(s.Save(s.Revision).SequenceEqual(before)&&s.UndoCount==history,"reconnect service preview preserves committed source");
        s.AcceptPreview(s.Revision);s.Undo(s.Revision);
        Check(s.Save(s.Revision).SequenceEqual(before),"reconnect services undo restores exact prior edge");
    }
}
