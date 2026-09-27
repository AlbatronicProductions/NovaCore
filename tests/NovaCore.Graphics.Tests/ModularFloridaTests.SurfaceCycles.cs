using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceCycles()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            long host=0,control=0;
            void Step(){Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625))==ConstructionServiceStatus.AcceptedCredit,"cycle host");Need(s.Engine.ServiceConstructionDebt(s.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,$"cycle physics: {s.Engine.ConstructionSupportFailure(s.Authority)}");}
            void Command(bool on)=>Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,++control,new(on)).Status==AssemblyControlStatus.Admitted,"cycle physical command");
            double Gap(){var p=Observe(s).Physical!.Motion;return craft.Collision.SelectMany(c=>c.Vertices).Min(v=>(p.PositionO+p.BodyToWorld.Rotate(v)).Y)-s.Binding.Physical!.Contact.SupportPlane;}
            for(var episode=0;episode<3;episode++)
            {
                var before=Observe(s);Command(true);var burn=0;
                while(Gap()<.06&&burn++<256)Step();
                Need(burn<=256&&Gap()>=.06,"whole compound physically clears support after finite burn");
                Command(false);var touched=false;var peak=Gap();
                for(var i=0;i<192;i++){Step();var gap=Gap();peak=Math.Max(peak,gap);touched|=gap<=.002;}
                var landed=Observe(s);
                Need(touched&&landed.Physical!.Consumer==AssemblyPhysicalConsumer.SurfaceContact&&Norm(landed.Physical.Motion.VelocityO)<.002,"whole compound separates, coasts, recontacts and settles");
                Need(!before.Fuel.Save().SequenceEqual(landed.Fuel.Save()),"each hop uses finite physical propellant");
                Console.WriteLine($"SURFACE_CYCLE long={longer} episode={episode} burnTicks={burn} peakClearance={peak:R}");
            }
            var checkpoint=s.Save();
            using var same=ConstructionApplicationSession.RestoreFlight(catalog,checkpoint,Assets,terrain.Query,terrain.Slab);
            using var split=ConstructionApplicationSession.RestoreFlight(catalog,checkpoint,Assets,terrain.Query,terrain.Slab);
            // Same admitted simulation time, different host-frame partition.
            Need(same.Engine.AdmitConstructionHostTime(same.Authority,host+1,new(31250))==ConstructionServiceStatus.AcceptedCredit,"whole host credit");
            Need(same.Engine.ServiceConstructionDebt(same.Authority,out var count)==ConstructionServiceStatus.Published&&count==2,"whole debt");
            for(var i=1;i<=2;i++){Need(split.Engine.AdmitConstructionHostTime(split.Authority,host+i,new(15625))==ConstructionServiceStatus.AcceptedCredit,"split host credit");Need(split.Engine.ServiceConstructionDebt(split.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"split debt");}
            var a=Observe(same);var b=Observe(split);
            Need(a.Physical==b.Physical&&a.Fuel.Save().SequenceEqual(b.Fuel.Save())&&a.Power.Save().SequenceEqual(b.Power.Save())&&a.Epoch==b.Epoch,"host partition preserves deterministic physical/resource endpoint");
        }
        Console.WriteLine($"SURFACE_CYCLES_PASS checks={checks}");
    }
}
