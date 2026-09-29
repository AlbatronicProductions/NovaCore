using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void PostContactFixture(string output,int minimumIntervals=0)
    {
        if(minimumIntervals<0||minimumIntervals>1599)throw new ArgumentOutOfRangeException(nameof(minimumIntervals));
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        var bytes=File.ReadAllBytes(Path.Combine(GraphicsTestHarness.RepositoryPath(),"tests/fixtures/physical-history/surface-retry.ncflight.json"));
        using var s=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
        var rows=new List<object>();
        var route=new SurfaceRetryRoute();var input=new PlayerFlightControlInput(s);var keys=NativePilotKeys.None;var generations=new HashSet<int>();
        var costs=new List<double>();bool complete=false;long host=0;int contactRows=0;
        for(int i=0;i<1600;i++){
            if(complete&&i>=minimumIntervals)break;
            var before=i%64==0?s.Save():null;var value=SurfaceRetryObservation.Read(s);if(before is not null)Need(s.Save().SequenceEqual(before),"read observer preserves source");
            if(value.TerrainContacts>0){contactRows++;Need(value.Pair.StartsWith("body="),"real current terrain body/surface pair");}
            if(value.TerrainGeneration>0)generations.Add(value.TerrainGeneration);
            var action=route.Observe(value);var engine=NativeEngineActions.None;
            if(action==SurfaceRetryAction.Complete){complete=true;if(i>=minimumIntervals)break;}
            if(action==SurfaceRetryAction.Roll)keys=NativePilotKeys.Q;
            if(action==SurfaceRetryAction.Release)keys=NativePilotKeys.None;
            if(action==SurfaceRetryAction.Ignite)engine=NativeEngineActions.On;
            Need(input.Apply(new(){ControlInputActive=1,PilotKeys=keys,EngineActions=engine}).Status is AssemblyControlStatus.Admitted or AssemblyControlStatus.Ready,"production player input");
            using var probe=new ConstructionWorkProbe();
            var allocated=GC.GetAllocatedBytesForCurrentThread();var debtBefore=s.Clock.PendingSimulationDebt.Ticks;
            var stamp=System.Diagnostics.Stopwatch.GetTimestamp();s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625));
            var status=s.Engine.ServiceConstructionDebt(s.Authority,out var n);Need(status==ConstructionServiceStatus.Published&&n==1,$"retry fixture physical service step={i} stage={route.Stage} status={status} reason={s.Engine.ConstructionSupportFailure(s.Authority)} state={value}");var cost=System.Diagnostics.Stopwatch.GetElapsedTime(stamp).TotalMilliseconds;costs.Add(cost);
            rows.Add(new{Before=value,After=SurfaceRetryObservation.Read(s),ServiceMs=cost,Allocated=GC.GetAllocatedBytesForCurrentThread()-allocated,DebtBefore=debtBefore,DebtAfter=s.Clock.PendingSimulationDebt.Ticks,Admitted=15625,Retired=n*15625,Work=probe.Read(),StateSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(s.Save()))});
        }
        Need(complete&&route.TerrainSeen&&route.GroundedRcs&&route.Powered,"complete terrain/contact/RCS/powered/free flight route");
        var invalid=new SurfaceRetryRoute();bool refused=false;try{invalid.Observe(SurfaceRetryObservation.Read(s) with{RawCapture=true});}catch(InvalidDataException){refused=true;}Need(refused,"diagnostic capture cannot masquerade as production");
        File.WriteAllText(output,System.Text.Json.JsonSerializer.Serialize(rows,new System.Text.Json.JsonSerializerOptions{IncludeFields=true}));
        Console.WriteLine($"POST_CONTACT_FIXTURE_PASS checks={checks} ticks={host} contactRows={contactRows} generations={generations.Count} source={craft.Design.Digest} inputSha={CraftLaunchAdmission.SourceHash(bytes)} maxServiceMs={costs.Max():R} tails25={costs.Count(c=>c>25)}");
    }
}

