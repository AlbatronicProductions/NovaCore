using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfacePersistence()
    {
        checks=0;_=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        Need(SolarSystemScene.TryCreateAt(new(1),new(999000),out var initialSolar,out _),"persistence initial Solar");
        using var initial=new ConstructionFlightScene(craft,Assets,initialSolar!);
        var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(initial.SaveFlight());
        var motion=basis.Physical.Motion;
        var airborne=basis with{Physical=basis.Physical with{Consumer=AssemblyPhysicalConsumer.FreeFlight,Motion=motion with{PositionO=motion.PositionO+new Double3(0,.01,0),VelocityO=new(0,-.2,0)}}};
        using var landed=ConstructionFlightScene.RestoreFlight(catalog,AssemblyJson.Write(airborne),Assets,initialSolar!,initial.Visuals);
        var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,initialSolar!.Projection,CameraMode.Free);
        Need(landed.ActivateRestoredPresentation(camera),"prepared camera activation");
        var solar=landed.FloridaView.Solar;
        for(var i=0;i<128;i++)landed.Advance(new(15625));
        Need(!landed.Failed&&landed.State.Physical!.Consumer==AssemblyPhysicalConsumer.SurfaceContact,"physical grounded checkpoint");
        Need(landed.ContactPoints>0&&landed.FlightStatus=="SURFACE CONTACT","player contact status observes published contacts");
        Need(solar.TryPresentPhysicalEpoch(landed.State.Epoch,camera,out _)&&solar.RefreshActiveVessel(camera,landed.PrepareFocusObservation()),"grounded presentation");
        var saved=landed.SaveFlight();var savedEpoch=landed.State.Epoch;
        for(var i=0;i<16;i++)landed.Advance(new(15625));
        Need(solar.TryPresentPhysicalEpoch(landed.State.Epoch,camera,out _),"advance beyond saved epoch");
        Need(!solar.TryPresentPhysicalEpoch(savedEpoch,camera,out _),"ordinary presentation remains monotonic");
        solar.ApplyPresentationInput(camera,new(){RateIncrease=1,PauseToggle=1},out _,out _);
        var oldEpoch=solar.CurrentTime;var oldPresentation=solar.Presentation;var oldView=camera.Position;
        var oldBinding=solar.ActiveVesselObservation;var original=landed.SaveFlight();
        try{using var rejected=ConstructionFlightScene.RestoreFlight(catalog,"{}"u8.ToArray(),Assets,solar,initial.Visuals);throw new InvalidOperationException("Malformed JSON accepted.");}
        catch(System.Text.Json.JsonException){Need(true,"malformed scene restore");}
        Need(original.SequenceEqual(landed.SaveFlight())&&ReferenceEquals(solar.Presentation,oldPresentation)&&solar.CurrentTime==oldEpoch&&solar.ActiveVesselObservation==oldBinding&&camera.Position==oldView,"refusal preserves physical, camera, presentation and binding");
        for(var reload=0;reload<2;reload++)
        {
            using var loaded=ConstructionFlightScene.RestoreFlight(catalog,saved,Assets,solar,initial.Visuals);
            var successor=loaded.FloridaView.Solar;
            Need(successor.CurrentTime==savedEpoch&&successor.PresentationTicks==savedEpoch.Ticks&&successor.Rate==NovaCore.Simulation.Time.SimulationRate.One&&!successor.IsPaused,"load replaces presentation lifetime at saved epoch and 1x");
            Need(loaded.SaveFlight().SequenceEqual(saved)&&successor.ActiveVesselId==loaded.PrepareFocusObservation().CanonicalId&&successor.ActiveVesselGeneration==loaded.PrepareFocusObservation().Generation,"exact checkpoint and fresh canonical binding");
            Need(loaded.ContactPoints==0&&loaded.FlightStatus=="COAST","restored endpoint does not invent an unsolved contact count");
            Need(ReferenceEquals(solar.Presentation,oldPresentation)&&solar.CurrentTime==oldEpoch&&camera.Position==oldView,"preparation leaves prior scene untouched");
            successor.RetainRendererBuffers(solar);
            Need(ReferenceEquals(successor.DistantBodies,solar.DistantBodies)&&ReferenceEquals(successor.OrbitVertices,solar.OrbitVertices),"same pinned native publication buffers");
            Need(loaded.ActivateRestoredPresentation(camera)&&!loaded.ActivateRestoredPresentation(camera),"one-way prepared camera handoff");
            loaded.ApplyPlayerInput(default);loaded.Advance(new(15625));
            Need(!loaded.Failed&&successor.TryPresentPhysicalEpoch(loaded.State.Epoch,camera,out _)&&successor.RefreshActiveVessel(camera,loaded.PrepareFocusObservation()),"load advances real physics and presentation");
            Need(loaded.ContactPoints>0&&loaded.FlightStatus=="SURFACE CONTACT","restored physical contact publication reaches presentation");
            successor.Update(camera);
            Need(successor.DistantBodyCount>0&&successor.DistantBodies.Any(b=>!b.Equals(default(NativePlanetaryPresentation))),"renderer publication continues after replacement");
            // The next reload deliberately replaces this active binding too.
            solar=successor;oldEpoch=solar.CurrentTime;oldPresentation=solar.Presentation;oldView=camera.Position;
        }
        using var relaunched=ConstructionFlightScene.LaunchReplacement(craft,Assets,solar,initial.Visuals);
        Need(solar.ActiveVesselId!=0&&relaunched.FloridaView.Solar.ActiveVesselId==relaunched.PrepareFocusObservation().CanonicalId,"new construction launch replaces prior presentation lifetime explicitly");
        relaunched.FloridaView.Solar.RetainRendererBuffers(solar);
        Need(relaunched.ActivateRestoredPresentation(camera),"launch replacement camera prepared before commit");
        relaunched.Advance(new(15625));
        Need(!relaunched.Failed&&relaunched.FloridaView.Solar.TryPresentPhysicalEpoch(relaunched.State.Epoch,camera,out _),"construction relaunch continues new physical owner");
        Console.WriteLine($"SURFACE_PERSISTENCE_PASS checks={checks}");
    }
}
