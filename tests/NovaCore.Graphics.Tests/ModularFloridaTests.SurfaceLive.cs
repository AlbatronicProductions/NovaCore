using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    // Regeneratable normal flight-save inputs for the direct native/UI witness.
    // No game-only fixture loader or alternate physical owner is introduced.
    internal static void SurfaceLiveFixtures()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var directory=Path.Combine(GraphicsTestHarness.RepositoryPath(),"build","surface-recontact","live-inputs");Directory.CreateDirectory(directory);
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab,new(18_000_000_000L));
            var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
            var start=basis with{Physical=new(new(new(0,.25,0),new(0,-.2,0),AssemblyContactProfile.Upright,default),default,AssemblyPhysicalConsumer.FreeFlight)};
            var name=longer?"long":"short";var bytes=AssemblyJson.Write(start);
            using var falling=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
            File.WriteAllBytes(Path.Combine(directory,name+"-descending.ncflight.json"),bytes);
            for(var i=1;i<=192;i++)
            {
                falling.Engine.AdmitConstructionHostTime(falling.Authority,i,new(15625));
                Need(falling.Engine.ServiceConstructionDebt(falling.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"native-input descending fixture continues physically");
            }
            Need(Observe(falling).Physical!.Consumer==AssemblyPhysicalConsumer.SurfaceContact&&Norm(Observe(falling).Physical!.Motion.VelocityO)<.002,"native-input fixture reaches physical rest");
            var grounded=falling.Save();using var restored=ConstructionApplicationSession.RestoreFlight(catalog,grounded,Assets,terrain.Query,terrain.Slab);
            Need(restored.Save().SequenceEqual(grounded),"native-input grounded fixture exact roundtrip");
            File.WriteAllBytes(Path.Combine(directory,name+"-grounded.ncflight.json"),grounded);
            Console.WriteLine($"SURFACE_LIVE_INPUT kind={name} source={craft.Design.Digest} compiled={craft.Digest} descendingSha={CraftLaunchAdmission.SourceHash(bytes)} groundedSha={CraftLaunchAdmission.SourceHash(grounded)}");
        }
        Console.WriteLine($"SURFACE_LIVE_INPUTS_PASS checks={checks}");
    }
}
