using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceRetryFixture()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab,new(18_000_000_000L));
        var site=cold.Binding.Physical!.Site;var region=FloridaFacilitySupport.Region;var source=(IPhysicalSurfaceCollisionSource)terrain.Query;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);var q=AssemblyContactProfile.Upright;
        var vertices=craft.Collision.SelectMany(h=>h.Vertices).Select(v=>q.Rotate(v)).ToArray();
        var origin=new Double3(80,0,0);
        double Clearance(Double3 local){var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(point,region.Up);return Norm(point)-Norm(source.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up)));}
        origin+=new Double3(0,-vertices.Min(v=>Clearance(origin+v))+.01,0);
        var saved=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save()) with{Physical=new(new(origin,new(.1,-.1,0),q,default),default,AssemblyPhysicalConsumer.FreeFlight)};
        var bytes=AssemblyJson.Write(saved);using var s=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
        var route=new SurfaceRetryRoute();var input=new PlayerFlightControlInput(s);var keys=NativePilotKeys.None;var generations=new HashSet<int>();
        var costs=new List<double>();bool complete=false;long host=0;int contactRows=0;
        for(int i=0;i<1600;i++){
            var before=i%64==0?s.Save():null;var value=SurfaceRetryObservation.Read(s);if(before is not null)Need(s.Save().SequenceEqual(before),"read observer preserves source");
            if(value.TerrainContacts>0){contactRows++;Need(value.Pair.StartsWith("body="),"real current terrain body/surface pair");}
            if(value.TerrainGeneration>0)generations.Add(value.TerrainGeneration);
            var action=route.Observe(value);var engine=NativeEngineActions.None;
            if(action==SurfaceRetryAction.Complete){complete=true;break;}
            if(action==SurfaceRetryAction.Roll)keys=NativePilotKeys.Q;
            if(action==SurfaceRetryAction.Release)keys=NativePilotKeys.None;
            if(action==SurfaceRetryAction.Ignite)engine=NativeEngineActions.On;
            Need(input.Apply(new(){ControlInputActive=1,PilotKeys=keys,EngineActions=engine}).Status is AssemblyControlStatus.Admitted or AssemblyControlStatus.Ready,"production player input");
            var stamp=System.Diagnostics.Stopwatch.GetTimestamp();s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));
            var status=s.Engine.ServiceConstructionDebt(s.Authority,out var n);Need(status==ConstructionServiceStatus.Published&&n==1,$"retry fixture physical service step={i} stage={route.Stage} status={status} reason={s.Engine.ConstructionSupportFailure(s.Authority)} state={value}");costs.Add(System.Diagnostics.Stopwatch.GetElapsedTime(stamp).TotalMilliseconds);
        }
        Need(complete&&route.TerrainSeen&&route.GroundedRcs&&route.Powered,"complete terrain/contact/RCS/powered/free flight route");
        var invalid=new SurfaceRetryRoute();bool refused=false;try{invalid.Observe(SurfaceRetryObservation.Read(s) with{RawCapture=true});}catch(InvalidDataException){refused=true;}Need(refused,"diagnostic capture cannot masquerade as production");
        var path=Path.Combine(GraphicsTestHarness.RepositoryPath(),"build/surface-recontact/retry/native.json.input.ncflight.json");File.WriteAllBytes(path,bytes);
        Console.WriteLine($"SURFACE_RETRY_FIXTURE_PASS checks={checks} ticks={host} contactRows={contactRows} generations={generations.Count} source={craft.Design.Digest} inputSha={CraftLaunchAdmission.SourceHash(bytes)} maxServiceMs={costs.Max():R} tails25={costs.Count(c=>c>25)}");
    }
}

