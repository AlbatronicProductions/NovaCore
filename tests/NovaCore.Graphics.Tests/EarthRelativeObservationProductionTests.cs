using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Time;
using static ContactGenerationFixture;

/// <summary>Headless same-E integration with the acquired production CPU terrain owner. No renderer.</summary>
internal static class EarthRelativeObservationProductionTests
{
    private static void Check(bool value,string message)=>ContactGenerationFixture.Check(value,message);
    internal static void Run()
    {
        var root=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var acquired)==PhysicalSurfaceQueryStatus.Ready,"acquired canonical physical Earth");
        var query=acquired!;var system=SolAnalyticalDefinition.Instance;var graph=Graph();var geometry=Geometry(1);
        var evaluations=new ReferenceFrameEvaluation[system.Count];var staging=new ReferenceFrameEvaluation[system.Count];
        var roots=new FrameTransform[system.Count];var stagingRoots=new FrameTransform[system.Count];
        Check(PhysicalEventEpoch.TryCreate(0,15625,32768,out var epoch)==PhysicalEventEpochStatus.Success,"exact epoch");
        Check(EarthPhysicalEventEvaluator.TryEvaluate(system,graph,epoch,evaluations,roots,staging,stagingRoots,out var earth)==EarthPhysicalEventStatus.Ready,"Earth at fractional E");
        foreach(var site in new[]{("Florida",FloridaFacilitySupport.Region.Up),("regional",Geo(28.5,-80.5)),("inland",Geo(39.1,-106.8))})
        {
            var center=query.Query(6,site.Item2);Check(center.IsReady,"physical center readiness");
            var p=earth.BodyFixedToRoot.LocalToParent(center.BodyFixedPositionMetres+site.Item2*10);
            var velocity=earth.VelocityRoot+Double3.Cross(earth.AngularVelocityRoot,earth.BodyFixedToRoot.LocalDirectionToParent(center.BodyFixedPositionMetres));
            var state=State(p,velocity,earth.BodyFixedToRoot.Rotation,Double3.Zero);var view=state.CreateView();
            bool Observe(out SpacecraftPhysicalEventContactObservation result)=>SpacecraftPhysicalEventObservationEvaluator.Evaluate(
                view,state.CreateView().Revision,epoch,geometry,geometry.GetFeature(0).Id,system,graph,query,query.Authority,
                evaluations,roots,staging,stagingRoots,out result).Succeeded;
            Check(Observe(out var value),"real terrain observation");
            var physical=query.Query(6,value.TerrainDirectionBodyFixed);Check(physical.IsReady,"independent repeat canonical query");
            Check(value.TerrainAuthority==query.Authority && physical.Authority==query.Authority && value.Epoch==epoch,"immutable provenance and time");
            var witness=earth.BodyFixedToRoot.LocalToParent(physical.BodyFixedPositionMetres);
            var normal=earth.BodyFixedToRoot.LocalDirectionToParent(physical.PhysicalNormal);
            Check(value.TerrainPositionRoot==witness && value.PhysicalNormalRoot==normal,"physical height/witness/qualified normal owner unchanged");
            var witnessVelocity=earth.VelocityRoot+Double3.Cross(earth.AngularVelocityRoot,earth.BodyFixedToRoot.LocalDirectionToParent(physical.BodyFixedPositionMetres));
            Check(value.TerrainVelocityRoot==witnessVelocity,"transported witness velocity");
            // Bounded query-inclusive timing; ordinary runtime, separately from checked allocation.
            for(var i=0;i<64;i++)Check(Observe(out _),"warm acquired query");
            var ticks=new long[101];var complete=0;
            for(var i=0;i<ticks.Length;i++){var start=Stopwatch.GetTimestamp();for(var j=0;j<16;j++)if(Observe(out _))complete++;ticks[i]=Stopwatch.GetTimestamp()-start;}
            Check(complete==1616,"real-query timing complete");Array.Sort(ticks);
            Check(GC.TryStartNoGCRegion(1L<<20,disallowFullBlockingGC:true),"checked real-query allocation entry");
            long bytes;var successes=0;
            try {var before=GC.GetAllocatedBytesForCurrentThread();for(var i=0;i<32;i++)if(Observe(out _))successes++;bytes=GC.GetAllocatedBytesForCurrentThread()-before;}
            finally {GC.EndNoGCRegion();}
            Check(bytes==0,$"real query zero bytes, actual={bytes}");Check(successes==32,"real query allocation completion");
            Check(state.CreateView().Revision==view.Revision,"no mutation");
            double Ns(int i)=>ticks[i]*1e9/Stopwatch.Frequency/16;
            Console.WriteLine(JsonSerializer.Serialize(new{kind="earth-real-terrain",site=site.Item1,epochFloor=epoch.FloorTicks,epoch.Numerator,epoch.Denominator,
                physical.HeightMetres,physical.NormalSampleDistanceMetres,value.RadialSignedGapMetres,bytes,medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99)}));
        }
        Console.WriteLine("EARTH_REAL_AUTHORITY "+JsonSerializer.Serialize(query.Authority));
    }
    private static Double3 Geo(double latitude,double longitude)=>BodyFixedGeography.DirectionFromLatitudeLongitude(latitude*Math.PI/180,longitude*Math.PI/180);
}
