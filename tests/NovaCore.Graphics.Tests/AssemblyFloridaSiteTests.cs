using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class AssemblyFloridaSiteTests
{
    private static void Check(bool value,string message)
    {if(!value)throw new InvalidOperationException("FLORIDA SITE: "+message);}
    internal static void Cheap(bool slab=false)
    {
        var root=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)==PhysicalSurfaceQueryStatus.Ready,"real terrain acquisition");
        // Midway between the existing platform edge and the full-weight grading edge.
        // The entire 16m patch is outside the authored 64m platform footprint.
        var east=(FloridaLaunchSite.PlatformEastWidthMetres*.5+FloridaFacilitySupport.Region.InnerEastMetres)*.5;
        var site=slab?Site(true):AssemblyFloridaSite.Create(query!,SimulationInstant.Zero,new(3),east);
        Check(east-8>FloridaLaunchSite.PlatformEastWidthMetres*.5,"ground patch clear of existing facility");
        var motion=new AssemblyMotion(new(.2,1.3,-.7),new(.05,-.03,.02),AssemblyContactProfile.Upright,new(.00002,.00001,-.00003));
        var maxPosition=0d;var maxVelocity=0d;
        foreach(var ticks in new[]{0L,16666,33333,10_000_000,20_000_000})
        {
            var time=new SimulationInstant(ticks);var earth=site.ToEarth(motion,time);var back=site.FromEarth(earth,time);
            maxPosition=Math.Max(maxPosition,Math.Sqrt((back.PositionO-motion.PositionO).LengthSquared));
            maxVelocity=Math.Max(maxVelocity,Math.Sqrt((back.VelocityO-motion.VelocityO).LengthSquared));
            Check(Math.Sqrt((back.AngularVelocityBody-motion.AngularVelocityBody).LengthSquared)<1e-15,"angular frame round trip");
            Check(CelestialBodyOrientationEvaluator.TryEvaluate(SolarSystemBodyIds.Earth,time,out var orientation),"banked Earth pose");
            var expected=orientation.BodyFixedToInertial.Rotate(site.OriginBodyFixed);
            Check(Math.Sqrt((expected-site.At(time).Position).LengthSquared)<1e-7,"current orientation owner agreement");
        }
        Check(maxPosition<1e-8&&maxVelocity<1e-10,"full physical frame round trip");
        var f=site.At(new(10_000_000));var before=site.At(new(9_999_000));var after=site.At(new(10_001_000));
        var derivative=(after.Position-before.Position)/.002;
        Check(Math.Sqrt((derivative-f.Velocity).LengthSquared)<2e-5,"independent centered position derivative");
        var testP=new Double3(.2,1,-.5);var testV=new Double3(.1,.05,.02);
        var local=site.LinearAcceleration(f,testP,testV);
        var r=f.Position+f.Orientation.Rotate(testP);var radius=Math.Sqrt(r.LengthSquared);
        var omega=f.Orientation.Rotate(f.Omega);var alpha=f.Orientation.Rotate(f.Alpha);
        var originA=Double3.Cross(alpha,f.Position)+Double3.Cross(omega,Double3.Cross(omega,f.Position));
        var reconstructed=originA+f.Orientation.Rotate(local+Double3.Cross(f.Omega,testV)*2+
            Double3.Cross(f.Omega,Double3.Cross(f.Omega,testP))+Double3.Cross(f.Alpha,testP));
        Check(Math.Sqrt((reconstructed-r*(-site.Mu/(radius*radius*radius))).LengthSquared)<2e-13,"inertial acceleration balance including tangential terms");
        Console.WriteLine($"FLORIDA_SITE_FRAME PASS positionRoundTrip={maxPosition:R} velocityRoundTrip={maxVelocity:R} derivativeError={Math.Sqrt((derivative-f.Velocity).LengthSquared):R} gravity={local} authority={site.Authority} east={site.EastMetres}");

        var d=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn");
        Check(d.Development is null && d.Digest=="ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf", "unchanged authored stock identity");
        Check(d.Data.Design.InitialFuelKg==30&&d.Data.Design.InitialOxidizerKg==45, "stock finite stores");
        var launch=AssemblyLaunch.CreateFloridaSupported(d,new SpacecraftDefinition(new(305),new(1),new(2),"Stock SRV-01 Florida ground"),"florida-ground",site);
        var mass=launch.Initial.Mass;
        if(slab)
        {
            var pose=launch.Initial.Motion;var rotation=Q(pose.BodyToWorld);
            var com=pose.PositionO+rotation.Apply(mass.Com);
            var supportMinX=double.MaxValue;var supportMaxX=double.MinValue;
            var supportMinZ=double.MaxValue;var supportMaxZ=double.MinValue;var initialMargin=double.MaxValue;
            var minimumHeight=double.MaxValue;var supportCorners=0;
            foreach(var child in launch.ContactProfile!.Children)for(var i=0;i<8;i++)
            {
                var p=child.AtOrigin.Position+child.AtOrigin.Rotation.Apply(new(
                    ((i&1)==0?-.5:.5)*child.Dimensions.X,((i&2)==0?-.5:.5)*child.Dimensions.Y,((i&4)==0?-.5:.5)*child.Dimensions.Z));
                var c=pose.PositionO+rotation.Apply(p);minimumHeight=Math.Min(minimumHeight,c.Y+1.7);
                initialMargin=Math.Min(initialMargin,Math.Min(32-Math.Abs(c.X),24-Math.Abs(c.Z)));
                if(Math.Abs(c.Y+1.7)<1e-12){supportCorners++;supportMinX=Math.Min(supportMinX,c.X);supportMaxX=Math.Max(supportMaxX,c.X);supportMinZ=Math.Min(supportMinZ,c.Z);supportMaxZ=Math.Max(supportMaxZ,c.Z);}
            }
            Check(Math.Abs(minimumHeight)<1e-12&&initialMargin>.03172&&supportCorners==4,"initial authored corners supported wholly inside finite slab");
            Check(Math.Abs(com.X)<1e-12&&Math.Abs(com.Z)<1e-12&&supportMinX<-.259999&&supportMaxX>.259999&&supportMinZ<-.259999&&supportMaxZ>.259999,"independent COM projection inside authored aft support rectangle");
            Console.WriteLine($"FLORIDA_SLAB_INITIAL PASS minHeight={minimumHeight:R} allCornerEdgeMargin={initialMargin:R} supportCorners={supportCorners} comProjection=({com.X:R},{com.Z:R}) supportRectangle=({supportMinX:R},{supportMaxX:R},{supportMinZ:R},{supportMaxZ:R})");
        }
        Check(mass.Mass==705 && Math.Abs(mass.Com.X-734.4/705)<1e-14 && Math.Abs(mass.Com.Y)<1e-14 && Math.Abs(mass.Com.Z)<1e-14,"authored stock total and first-moment COM");
        Check(Math.Abs(mass.Inertia.A-190.47255)<1e-10 && Math.Abs(mass.Inertia.E-731.6006491134752)<1e-9 && Math.Abs(mass.Inertia.I-731.6006491134752)<1e-9,"independent stock inertia oracle");
        var stationary=site.ToEarth(launch.Initial.Motion,site.Start);var firstFrame=site.At(site.Start);
        Check(Math.Sqrt(stationary.VelocityO.LengthSquared)>400 && Math.Sqrt(stationary.AngularVelocityBody.LengthSquared)>7e-5,"surface stationary includes Earth inertial rotation");
        Check(Math.Sqrt(site.FromEarth(stationary,site.Start).VelocityO.LengthSquared)<1e-10,"zero relative surface velocity");
        Console.WriteLine($"FLORIDA_STOCK_IDENTITY PASS digest={d.Digest} mass={mass.Mass:R} com={mass.Com} inertia={mass.Inertia} fuel=30 oxidizer=45 siteDigest={site.Digest} origin={firstFrame.Position} velocity={firstFrame.Velocity} omega={firstFrame.Omega}");
        using var session=AssemblyApplicationSession.CreateSupported(launch);
        var world=session.Engine.AssemblyContactWorldForTest(session.Authority)!;var identity=world.AssemblyIdentityForTest;
        var maxPenetration=0d;var finalSupported=0;
        var tolerance=launch.ContactProfile!.SmallestFeature/1000;
        var minClearance=double.MaxValue;var maxOrientationDrift=0d;var childMasks=0;var minContacts=int.MaxValue;var featureChanges=0;var previousFeatures=0UL;

        var settledOrigin=Double3.Zero;var settledHeight=0d;var drift=0d;var speed=0d;var angular=0d;
        for(var n=1;n<=1200;n++)
        {
            var ticks=(long)n*1_000_000/60-(long)(n-1)*1_000_000/60;
            Check(session.Engine.AdmitAssemblyHostTime(session.Authority,n,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"host credit");
            var result=session.Engine.ServiceAssemblyContactDebt(session.Authority);
            session.Engine.ObserveAssemblyFlight(session.Authority,out var observation);
            var minimum=double.MaxValue;
            if(slab){var exported=Field<LocalContactWorld.Export>(world,"export");minContacts=Math.Min(minContacts,exported.ContactPoints);}
            ulong features=0;var featureIndex=0;
            var pose=observation.State.Motion;var q=pose.BodyToWorld;
            var ry0=2*(q.X*q.Y+q.Z*q.W);var ry1=1-2*(q.X*q.X+q.Z*q.Z);var ry2=2*(q.Y*q.Z-q.X*q.W);
            foreach(var child in launch.ContactProfile!.Children)for(var i=0;i<8;i++)
            {
                // Independent matrix arithmetic, not the production contact/pose helper.
                var x=((i&1)==0?-.5:.5)*child.Dimensions.X;var y=((i&2)==0?-.5:.5)*child.Dimensions.Y;var z=((i&4)==0?-.5:.5)*child.Dimensions.Z;
                var m=child.AtOrigin.Rotation;var o=child.AtOrigin.Position;
                var ax=o.X+m.A*x+m.B*y+m.C*z;var ay=o.Y+m.D*x+m.E*y+m.F*z;var az=o.Z+m.G*x+m.H*y+m.I*z;
                var cornerHeight=pose.PositionO.Y+ry0*ax+ry1*ay+ry2*az+1.7;minimum=Math.Min(minimum,cornerHeight);
                if(Math.Abs(cornerHeight)<=2*tolerance){features|=1UL<<featureIndex;childMasks|=1<<(featureIndex/8);}featureIndex++;
                if(slab){var corner=pose.PositionO+Q(q).Apply(new(ax,ay,az));minClearance=Math.Min(minClearance,Math.Min(32-Math.Abs(corner.X),24-Math.Abs(corner.Z)));Check(minClearance>.03172,"finite slab coverage with speculative margin");}
            }
            if(n>601&&features!=previousFeatures)featureChanges++;previousFeatures=features;
            maxPenetration=Math.Max(maxPenetration,-minimum);
            if(n==600)settledOrigin=pose.PositionO;
            if(n>600)
            {
                settledHeight=Math.Max(settledHeight,Math.Abs(minimum));
                if(Math.Abs(minimum)<=2*tolerance)finalSupported++;
                var delta=pose.PositionO-settledOrigin;
                drift=Math.Max(drift,Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z));
                speed=Math.Max(speed,Math.Sqrt(pose.VelocityO.LengthSquared));
                angular=Math.Max(angular,Math.Sqrt(pose.AngularVelocityBody.LengthSquared));
                maxOrientationDrift=Math.Max(maxOrientationDrift,(Q(q)-Q(AssemblyContactProfile.Upright)).Maximum);
                Check(settledHeight<=2*tolerance,"settled height <= twice feature-derived tolerance");
                Check(drift<=tolerance,"settled drift <= feature-derived tolerance");
                Check(speed<=tolerance/.016667&&angular*launch.ContactProfile.BoundingRadius<=tolerance/.016667,"settled linear/angular surface speed");
            }
            if(n<=4||result.PublishedCount!=1||n==1200)
                Console.WriteLine($"FLORIDA_SITE_STEP n={n} status={result.Status} published={result.PublishedCount} tick={observation.State.Epoch.Ticks} penetration={-minimum:R} velocity={observation.State.Motion.VelocityO} omega={observation.State.Motion.AngularVelocityBody} private={world.AssemblyIdentityForTest}");
            Check(result.PublishedCount==1&&result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"first material contact result");
            Check(observation.State.Stores==launch.Initial.Stores&&observation.State.Mass==launch.Initial.Mass,"no resource/mass mutation");
            Check(maxPenetration<=.020,"20mm physical penetration ceiling");
            Check(observation.StateRevision.Value==(ulong)n&&observation.HistoryCount==n&&observation.Clock.Debt.Ticks==0&&observation.TimelineRevision.Value==0,"canonical accounting");
            Check(world.AssemblyIdentityForTest.Generation==identity.Generation&&world.AssemblyIdentityForTest.Body==identity.Body,"retained world/body");
        }
        Check(finalSupported==600,"final600 support");
        if(slab)Console.WriteLine($"FLORIDA_SLAB_METRICS peak={maxPenetration:R} settledHeight={settledHeight:R} drift={drift:R} speed={speed:R} angular={angular:R} orientationMatrixDrift={maxOrientationDrift:R} minEdgeClearance={minClearance:R} minContacts={minContacts} geometricContactChildMask={childMasks} settledGeometricFeatureChanges={featureChanges} support=600/600");
        Console.WriteLine($"FLORIDA_SITE_CHEAP PASS peakPenetration={maxPenetration:R} support={finalSupported}/600 intervals=1200");
    }
}
