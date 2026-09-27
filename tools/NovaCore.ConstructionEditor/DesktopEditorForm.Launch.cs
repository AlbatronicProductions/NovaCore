using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private ConstructionFlightScene? flight;
    private void LaunchCraft()
    {
        if(solar is null)throw new InvalidDataException("The game session is not ready.");
        if(mode.SelectedIndex!=0||session.Preview is not null)throw new InvalidDataException("Place or cancel the held part before launch.");
        var source=session.Current?.Design??throw new InvalidDataException("Build a craft before launch.");
        var bytes=source.Save();var candidate=CraftCompiler.Compile(session.Catalog,source.Data,assetRoot);
        // Preserve exact-source admission after removing the process boundary.
        CompiledCraft admitted;ConstructionFlightScene next;
        try{
            admitted=CraftLaunchAdmission.Prepare(session.Catalog,bytes,assetRoot,CraftLaunchAdmission.SourceHash(bytes),source.Digest,candidate.Digest,session.Catalog.Digest);
            next=ConstructionFlightScene.LaunchReplacement(admitted,assetRoot,solar,visuals);
        }catch(InvalidDataException e){
            Console.WriteLine($"APPLICATION_LAUNCH_REFUSED source={source.Digest} codes={string.Join(',',candidate.Diagnostics.Concat(candidate.AdmissionDiagnostics).Select(d=>d.Code).Distinct(StringComparer.Ordinal))} reason={e.Message}");
            diagnostic=e.Message;message="Launch refused. Review the launch details and correct the craft.";
            var panel=OpenOverlay("LAUNCH DETAILS",570,480);
            panel.Controls.Add(new TextBox(){Text=e.Message.ReplaceLineEndings(Environment.NewLine),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Width=515,Height=245,BackColor=BackColor,ForeColor=ForeColor});
            if(candidate.Diagnostics.Any(d=>d.Code is "FUEL_PATH" or "POWER_PATH"))
                panel.Controls.Add(new Label(){Text="Fill consumables fills tanks and charges batteries. Launch checks all services and physical support again.",Width=515,Height=58});
            ActionButton(panel,"Back to construction",CloseOverlay,515);
            return;
        }
        try{next.FloridaView.Solar.RetainRendererBuffers(solar);}
        catch{next.Dispose();throw;}
        var previous=flight;solar=next.FloridaView.Solar;flight=next;compiled=admitted;previous?.Dispose();
        LeaveEditor();message="Z ignite · X cutoff · WASD/QE attitude · mouse orbit · F craft · Menu returns to construction";
        Console.WriteLine($"APPLICATION_LAUNCH process={Environment.ProcessId} source={source.Digest} compiled={admitted.Digest} epoch={next.PhysicalEpochTicks} sourceRevision={session.Revision} dirty={session.Dirty}");
    }
    private void FillConsumables()
    {
        CancelGhost();session.FillForLaunch(session.Revision);RefreshInspector();diagnostic=null;
        message="Consumables filled and batteries charged. Launch will recheck services and physical support.";
    }
}
