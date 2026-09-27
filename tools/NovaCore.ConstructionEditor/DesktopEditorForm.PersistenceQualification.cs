using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

// Adversarial application-handler checks. The native input checks run separately
// across real frames in ApplicationQualification; neither claims Player PASS.
internal sealed unsafe partial class DesktopEditorForm
{
    private string EditorFingerprint()=>AssemblyJson.Digest(new {
        document=session.Current?.Design.Digest,preview=session.Preview?.Design.Digest,
        session.Revision,session.Dirty,session.UndoCount,session.RedoCount,session.HistoryPayloadBytes,
        selection,savePath,craftName.Text,targetKey,refused=refusedGhost is null?null:AssemblyJson.Digest(refusedGhost)});
    private void TestSaveNew(string name)
    {
        ShowSaveBrowser(false);overlay!.Controls.OfType<TextBox>().Single().Text=name;ClickButton("Save new");
    }
    private void TestSelectSave(string path)
    {
        ShowSaveBrowser(false);var list=overlay!.Controls.OfType<ListBox>().Single();
        list.SelectedItem=list.Items.Cast<SavedEntry>().Single(e=>e.Path==path);
    }
    private void TestLoad(string path){TestSelectSave(path);ClickButton("Load selected");}
    private void TestRename(string name){craftName.Text=name;ValidateChildren();}
    private void TestInspect(string id)
    {
        selection=id;ShowPartContext(*native);
    }
    private void RunApplicationAdversarialChecks()
    {
        var baseline=session.Save(session.Revision);var baselinePath=savePath!;
        var originalFlight=flight;var physicalEpoch=flight!.State.Epoch.Ticks;
        var suffix=Guid.NewGuid().ToString("N")[..8];
        TestInspect(qualificationTank!);fill.Value=37.5m;stores.Checked=false;ClickButton("Apply to selected group");
        TestInspect("core");charge.Value=42.5m;electrical.Checked=false;ClickButton("Apply to selected group");
        foreach(var group in session.Current!.Design.Data.Symmetry){
            var symmetryMember=group.Members[Math.Min(3,group.Members.Length-1)].Part;
            TestInspect(symmetryMember);electrical.Checked=false;ClickButton("Apply to selected group");
            RequireQualification(group.Members.All(member=>session.Current!.Design.Data.Configuration.Single(c=>c.Part==member.Part).Electrical.All(e=>!e.Enabled)),"each symmetry group preserves its own disabled service configuration");
        }
        TestRename("Partial "+suffix);
        var partial=session.Save(session.Revision);var identity=session.Current!.Design.Data.Id;
        RequireQualification(session.Current.Design.Data.Configuration.Single(c=>c.Part==qualificationTank).Stores.All(s=>!s.Enabled),"disabled partial stores are authored state");
        RequireQualification(session.Current.Design.Data.Configuration.Single(c=>c.Part=="core").Electrical.Single(e=>e.Module=="battery").ChargeJ==38250,"partial charge is typed battery state");
        var beforeInspect=EditorFingerprint();RefreshInspector();RequireQualification(EditorFingerprint()==beforeInspect,"inspection does not change source or history");
        TestInspect(qualificationTank!);RequireQualification(fill.Value==37.5m&&!stores.Checked,"inspector reflects actual selected propellant state");
        TestInspect("core");RequireQualification(charge.Value==42.5m&&!electrical.Checked,"inspector reflects actual selected charge state");
        TestSaveNew("Partial-"+suffix);var partialPath=savePath!;
        RequireQualification(!session.Dirty&&File.ReadAllBytes(partialPath).SequenceEqual(partial),"visible Save new publishes exact partial bytes");
        ClickButton("New vehicle");RequireQualification(session.Current is null&&savePath is null,"new draft clears active file association");
        TestLoad(partialPath);RequireQualification(session.Save(session.Revision).SequenceEqual(partial)&&!session.Dirty&&session.Current!.Design.Data.Id==identity,"visible reload preserves identity, symmetry and partial resources");
        var clean=EditorFingerprint();ShowSaveBrowser(false);
        RequireQualification(!sidebar.Enabled&&!inspector.Enabled&&!navigation.Enabled,"modal save flow blocks background editing");
        ClickButton("Back");RequireQualification(EditorFingerprint()==clean,"cancelled browser leaves all source state intact");
        TestRename("Unsaved "+suffix);var dirty=EditorFingerprint();
        ClickButton("New vehicle");ClickButton("Cancel");RequireQualification(EditorFingerprint()==dirty,"Cancel protects a dirty draft from New");
        TestLoad(baselinePath);ClickButton("Cancel");RequireQualification(EditorFingerprint()==dirty,"Cancel protects a dirty draft from Load");
        ClickButton("Undo");RequireQualification(!session.Dirty&&session.Save(session.Revision).SequenceEqual(partial)&&craftName.Text==session.Current!.Design.Data.Craft!.Name,"undo returns exact saved partial identity and visible name");
        ClickButton("Redo");var renamed=session.Save(session.Revision);var beforeFailed=EditorFingerprint();
        TestSelectSave(partialPath);ClickButton("Overwrite selected");
        using(var locked=new FileStream(partialPath,FileMode.Open,FileAccess.Read,FileShare.Read))ClickButton("Overwrite");
        RequireQualification(EditorFingerprint()==beforeFailed&&File.ReadAllBytes(partialPath).SequenceEqual(partial),"failed atomic overwrite preserves source, history, path and prior file");
        CloseOverlay();TestSaveNew("Copy-"+suffix);var copyPath=savePath!;
        RequireQualification(!session.Dirty&&File.ReadAllBytes(copyPath).SequenceEqual(renamed),"Save new recovers from unavailable original file");
        var bad=Path.Combine(SaveDirectory,"Malformed-"+suffix+".craft.json");File.WriteAllText(bad,"{\"schema\":\"unsupported\"}");var beforeBad=EditorFingerprint();
        TestLoad(bad);RequireQualification(EditorFingerprint()==beforeBad,"malformed load preserves document, selection, visible name, history and file association");CloseOverlay();
        TestLoad(partialPath);ClickButton("Undo");RequireQualification(session.Save(session.Revision).SequenceEqual(renamed)&&savePath==copyPath,"same-ID copy undo restores exact snapshot association");
        ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(partial)&&savePath==partialPath,"redo restores corresponding original file association");
        ClickButton("Launch vehicle");RequireQualification(editing&&ReferenceEquals(flight,originalFlight)&&flight.State.Epoch.Ticks==physicalEpoch,"refused partial launch preserves existing physical owner and epoch");
        RequireQualification(overlay is not null&&diagnostic is not null,"refused partial launch explains its blockers");ClickButton("Back to construction");
        TestRename("Discarded "+suffix);TestLoad(baselinePath);ClickButton("Discard changes");RequireQualification(session.Save(session.Revision).SequenceEqual(baseline),"Discard loads exact chosen source without refilling");
        TestInspect(qualificationAdapter!);var beforeClock=session.Save(session.Revision);var oldAngle=session.Current!.Design.Data.Connections.Single(c=>c.Child==qualificationAdapter).Construction!.ClockDegrees;
        ClickButton("Rotate");RequireQualification(session.Current!.Design.Data.Connections.Single(c=>c.Child==qualificationAdapter).Construction!.ClockDegrees!=oldAngle,"Rotate advances the connected part's admitted clock");
        ClickButton("Undo");RequireQualification(session.Save(session.Revision).SequenceEqual(beforeClock),"clock undo restores exact assembly");
        foreach(var group in session.Current!.Design.Data.Symmetry){
            TestInspect(group.Members[0].Part);var keyed=EditorFingerprint();ClickButton("Rotate");RequireQualification(EditorFingerprint()==keyed&&session.Preview is null,"each fixed radial key refuses rotation without mutation");
        }
        TestInspect(qualificationTank!);fill.Value=50;ClickButton("Apply to selected group");var updated=session.Save(session.Revision);
        ClickButton("New vehicle");ClickButton("Save and continue");overlay!.Controls.OfType<TextBox>().Single().Text="Saved-before-new-"+suffix;ClickButton("Save new");
        RequireQualification(session.Current is null&&File.ReadAllBytes(Path.Combine(SaveDirectory,"Saved-before-new-"+suffix+".craft.json")).SequenceEqual(updated),"Save and continue publishes before clearing draft");
        TestLoad(baselinePath);TestInspect("core");ClickButton("Delete selected group");RequireQualification(session.Current is null&&savePath is null,"root deletion clears inherited file association");
        // Exercise the same handler as a native root click, including preview and
        // transaction admission. Native click latching is proven on the main route.
        HoldPart(session.Catalog.Data.Definitions.Single(d=>d.Id=="nc.core.command-2"));
        Input(new NativeEditorViewport(){Width=(uint)viewport.Width,Height=(uint)viewport.Height,PointerX=viewport.Width/2,PointerY=viewport.Height/2,Focused=1,Pressed=1});
        RequireQualification(session.Current is not null&&session.Current.Design.Data.Id!=identity&&savePath is null,"replacement root receives a fresh identity and no old destination");
        TestSaveNew("Second-"+suffix);var secondPath=savePath!;var secondIdentity=session.Current!.Design.Data.Id;
        TestLoad(baselinePath);ClickButton("Undo");RequireQualification(session.Current!.Design.Data.Id==secondIdentity&&savePath==secondPath,"cross-document undo recovers the exact saved association");
        ClickButton("Redo");RequireQualification(session.Save(session.Revision).SequenceEqual(baseline)&&savePath==baselinePath,"cross-document redo restores baseline");
        RequireQualification(!Directory.EnumerateFiles(SaveDirectory,"*.tmp").Any(),"no save temporary remains after refusals");
        RequireQualification(ReferenceEquals(flight,originalFlight)&&flight.State.Epoch.Ticks==physicalEpoch&&solar!.CurrentTime.Ticks==physicalEpoch,"all paused editor operations preserve physical and celestial epoch");
        contextInspector.Hide();CloseOverlay();selection=null;FocusCraft();RefreshInspector();
    }
}
