namespace NovaCore.ConstructionEditor;

internal sealed unsafe partial class DesktopEditorForm
{
    private string? launchedDigest;
    private long launchedRevision;
    private bool launchedDirty;
    private void RunLaunchRefusalChecks()
    {
        var saved=session.Save(session.Revision);var owner=flight;var epoch=flight!.State.Epoch.Ticks;
        foreach(var supportCase in new[]{false,true}){
            // Old two-/three-tank witnesses are now physically supported.
            // These full stock stacks exceed the unchanged authored total rating.
            var depth=supportCase?8:6;
            session.Load(session.Revision,NovaCore.Simulation.Spacecraft.Assemblies.AssemblyJson.Write(StabilizationCraftFixture.Create(session.Catalog,depth,shortOnly:supportCase)),true);
            if(supportCase)foreach(var host in new[]{depth/2,depth-1})session.Remove(session.Revision,$"r{host}-0");
            ClickButton("Fill consumables");var before=EditorFingerprint();
            ClickButton("Launch vehicle");
            var expected="Authored support total load capacity exceeded.";
            RequireQualification(overlay is not null&&overlay.Controls.OfType<TextBox>().Single().Text.Contains(expected,StringComparison.Ordinal),"native launch details explain "+expected);
            RequireQualification(EditorFingerprint()==before&&ReferenceEquals(flight,owner)&&flight!.State.Epoch.Ticks==epoch,"invalid launch preserves draft, history, flight owner and epoch");
            RequireQualification(owner!.Session.Engine.ObserveConstructionServices(owner.Session.Authority,out var retained)==NovaCore.Simulation.Spacecraft.Assemblies.ConstructionServiceStatus.Ready&&retained!.Epoch.Ticks==epoch,"refused replacement leaves retained flight authority usable");
            Console.WriteLine($"APPLICATION_REFUSAL_RECOVERY_PASS case={(supportCase?"short-stack-overload":"mixed-stack-overload")} reason={diagnostic}");
            ClickButton("Back to construction");
        }
        session.Load(session.Revision,saved,true);RefreshInspector();
        RequireQualification(session.Save(session.Revision).SequenceEqual(saved)&&ReferenceEquals(flight,owner),"valid retained craft restored after refusal witnesses");
    }
}
