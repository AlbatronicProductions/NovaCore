using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

internal static partial class AssemblyFloridaSiteTests
{
    private static AssemblyFloridaSite Site(bool slab=false)
    {
        var root=GraphicsTestHarness.RepositoryPath();
        Check(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Check(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Check(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Check(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)==PhysicalSurfaceQueryStatus.Ready,"real physical terrain");
        if(!slab)return AssemblyFloridaSite.Create(query!,SimulationInstant.Zero,new(3),48);
        Check(FloridaLaunchSite.TryCreate(6,query!.Authority.ReferenceRadiusMetres,PlanetaryTerrainDefinition.EarthProductionCubeV5,out var authored),"authored base");
        return AssemblyFloridaSite.CreateSlab(query,SimulationInstant.Zero,new(3),authored.CreateSupportSlab(query));
    }
    private static AssemblyLaunch Launch(AssemblyFloridaSite site) => AssemblyLaunch.CreateFloridaSupported(
        AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn"),new(new(305),new(1),new(2),"Stock SRV-01 Florida ground"),"florida-ground",site);
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {
        Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var v)==AssemblyFlightStatus.Ready,"copied observation");return v;
    }
    private static bool Operation(AssemblyApplicationSession s,ref AssemblyFlightObservation v)
    {
        var credit=s.Engine.AdmitAssemblyHostTime(s.Authority,v.HostSequence+1,new(s.Launch.Plan[v.State.Frontier].Request.Ticks));
        var service=s.Engine.ServiceAssemblyContactDebt(s.Authority);
        var observed=s.Engine.ObserveAssemblyFlight(s.Authority,out v);
        var mapped=SpacecraftMotionEvaluator.TryEvaluateAssembly(s.Engine.State,s.Launch.Spacecraft.Id,v.State.Epoch,out var inertial);
        return credit.Status==AssemblyFlightStatus.AcceptedCredit&&service.PublishedCount==1&&
            service.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed&&observed==AssemblyFlightStatus.Ready&&
            mapped==SpacecraftTranslationStatus.Success&&inertial.Time==v.State.Epoch&&inertial.Revision==v.StateRevision;
    }
    private static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    private static Matrix3 Rz(double a)=>new(Math.Cos(a),-Math.Sin(a),0,Math.Sin(a),Math.Cos(a),0,0,0,1);
    private static Matrix3 Rx(double a)=>new(1,0,0,0,Math.Cos(a),-Math.Sin(a),0,Math.Sin(a),Math.Cos(a));
    private static Matrix3 Q(DoubleQuaternion q)=>new(1-2*(q.Y*q.Y+q.Z*q.Z),2*(q.X*q.Y-q.Z*q.W),2*(q.X*q.Z+q.Y*q.W),
        2*(q.X*q.Y+q.Z*q.W),1-2*(q.X*q.X+q.Z*q.Z),2*(q.Y*q.Z-q.X*q.W),
        2*(q.X*q.Z-q.Y*q.W),2*(q.Y*q.Z+q.X*q.W),1-2*(q.X*q.X+q.Y*q.Y));
    private static double Norm(Double3 v)=>Math.Sqrt(v.LengthSquared);
    private static (double Position,double Velocity,double Orientation,double Angular) InertialOracle(AssemblyApplicationSession s,AssemblyFlightObservation v)
    {
        // Independent matrix product and analytic product derivative, no site At/ToEarth helper.
        var t=v.State.Epoch.Ticks/1e6;var rad=Math.PI/180;const double century=86400d*36525;
        var z=Rz((90-.641*t/century)*rad);var x=Rx(.557*t/century*rad);
        var w=Rz((190.147+360.9856235*t/86400)*rad);var h=Rx(Math.PI/2);
        var ja=new Matrix3(0,.641*rad/century,0,-.641*rad/century,0,0,0,0,0);
        var jb=new Matrix3(0,0,0,0,0,-.557*rad/century,0,.557*rad/century,0);
        var jc=new Matrix3(0,-360.9856235*rad/86400,0,360.9856235*rad/86400,0,0,0,0,0);
        var earth=z*x*w*h;var ed=ja*earth+z*jb*x*w*h+z*x*jc*w*h;
        var up=new Double3(.1433224599406355,.4788205718227514,.8661348234979923);
        var east=new Double3(.9865841313746494,0,-.163253642286255);
        var north=new Double3(-.07816920235165153,.8779127861008367,-.47239677793606216);
        var basis=new Matrix3(east.X,up.X,-north.X,east.Y,up.Y,-north.Y,east.Z,up.Z,-north.Z);
        var origin=s.Launch.Site!.Slab is null?up*(6371008.8+15.134892258793116+1.7)+east*48:
            up*(6371008.8+21.970461536198854+1.5+1.7); // Independent retained authored-base survey oracle.
        var c=earth*basis;var cd=ed*basis;var skew=ed*earth.Transpose();var omega=new Double3(skew.H,skew.C,skew.D);
        var local=v.State.Motion;var qi=c*Q(local.BodyToWorld);
        var p=earth.Apply(origin)+c.Apply(local.PositionO);
        var velocity=ed.Apply(origin)+c.Apply(local.VelocityO)+cd.Apply(local.PositionO);
        var angular=local.AngularVelocityBody+qi.Transpose().Apply(omega);
        Check(SpacecraftMotionEvaluator.TryEvaluateAssembly(s.Engine.State,s.Launch.Spacecraft.Id,v.State.Epoch,out var actual)==SpacecraftTranslationStatus.Success,"actual canonical typed view");
        Check(actual.RootFrame==s.Launch.Site!.EarthFrame&&actual.Time==v.State.Epoch&&actual.Revision==v.StateRevision&&actual.CurrentMass==v.State.Mass,"inertial publication identity");
        var a=actual.MaterialOriginMotion;
        var errors=(Norm(a.PositionO-p),Norm(a.VelocityO-velocity),(Q(a.BodyToWorld)-qi).Maximum,Norm(a.AngularVelocityBody-angular));
        Check(errors.Item1<1e-8&&errors.Item2<1e-10&&errors.Item3<1e-13&&errors.Item4<1e-15,"independent evolving canonical inertial oracle");
        return errors;
    }
    internal static void Qualification(bool slab=false)
    {
        Cheap(slab);var site=Site(slab);var launch=Launch(site);var reference=new AssemblyFlightRecord[1200];
        using(var s=AssemblyApplicationSession.CreateSupported(launch))
        {
            var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
            var sim=Field<BepuPhysics.Simulation>(world,"simulation");var body=sim.Bodies[Field<BepuPhysics.BodyHandle>(world,"body")];
            Check(body.Velocity.Linear==default&&body.Velocity.Angular==default,"initial native body stationary relative to fixed slab");
            var inertia=body.LocalInertia;
            Check(inertia.InverseMass==(float)(1d/705)&&Math.Abs(inertia.InverseInertiaTensor.XX-1d/731.6006491134752)<1e-9&&Math.Abs(inertia.InverseInertiaTensor.YY-1d/190.47255)<1e-9&&Math.Abs(inertia.InverseInertiaTensor.ZZ-1d/731.6006491134752)<1e-9,"stock mass and permuted tensor reach native body");
            Check(launch.Design.Parts.Length==7&&launch.ContactProfile!.Children.Length==8,"seven parts eight authored boxes");
            Check(launch.Initial.Mass.Inertia.B==0&&launch.Initial.Mass.Inertia.C==0&&launch.Initial.Mass.Inertia.D==0&&launch.Initial.Mass.Inertia.F==0&&launch.Initial.Mass.Inertia.G==0&&launch.Initial.Mass.Inertia.H==0,"full stock inertia off-diagonals");
            var v=Observe(s);InertialOracle(s,v);var original=v;
            Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority&&s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.InvalidAuthority&&Observe(s)==original&&world.AssemblyIdentityForTest==identity,"active supported owner refuses flight/departure without mutation");
            var p=0d;var speed=0d;var q=0d;var angular=0d;var shortTicks=0;var longTicks=0;
            for(var i=0;i<1200;i++)
            {
                Check(Operation(s,ref v),"ordinary stock Florida publication");
                var errors=InertialOracle(s,v);p=Math.Max(p,errors.Position);speed=Math.Max(speed,errors.Velocity);q=Math.Max(q,errors.Orientation);angular=Math.Max(angular,errors.Angular);
                Check(v.State.Epoch.Ticks==(i+1)*1_000_000L/60&&v.StateRevision.Value==(ulong)i+1&&v.TimelineRevision.Value==0&&v.HistoryCount==i+1&&v.Clock.Debt.Ticks==0,"tick/revision/history/debt");
                Check(v.State.Stores==launch.Initial.Stores&&v.State.Mass==launch.Initial.Mass&&v.State.ResourceRevision==0&&!v.State.Actual.MainOn&&v.State.Actual.Jets==0,"unchanged exact stores mass OFF realization");
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out reference[i]),"history retained");
                if(launch.Plan[i].Request.Ticks==16666)shortTicks++;else if(launch.Plan[i].Request.Ticks==16667)longTicks++;
                var id=world.AssemblyIdentityForTest;Check(id.Generation==identity.Generation&&id.Body==identity.Body&&id.Shape==identity.Shape&&!id.Pending&&!id.Invalidated,"same retained native authority");
            }
            Check(shortTicks==400&&longTicks==800,"original tick lattice");
            var before=v;var idBefore=world.AssemblyIdentityForTest;
            Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.InvalidAuthority&&s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status==AssemblyFlightStatus.InvalidAuthority,"no implicit rotating-site departure/free-flight owner");
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,v.HostSequence+1,new(1)).Status==AssemblyFlightStatus.Completed&&s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.Completed&&Observe(s)==before&&world.AssemblyIdentityForTest==idBefore,"completed hold nonmutation");
            Console.WriteLine($"FLORIDA_INERTIAL_ORACLE PASS intervals=1200 shortTicks={shortTicks} longTicks={longTicks} maxPosition={p:R} maxVelocity={speed:R} maxOrientationMatrix={q:R} maxAngular={angular:R} departure=REFUSED_NOT_QUALIFIED");
        }
        foreach(var fps in new[]{30,60,150,240,0})
        {
            using var s=AssemblyApplicationSession.CreateSupported(launch);var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
            var frames=fps==0?40:20*fps;long elapsed=0;var published=0;var budget=0;
            for(var n=1;n<=frames;n++)
            {
                var target=(long)n*20_000_000/frames;
                Check(s.Engine.AdmitAssemblyHostTime(s.Authority,n,new(target-elapsed)).Status==AssemblyFlightStatus.AcceptedCredit,"partition credit");elapsed=target;
                var result=s.Engine.ServiceAssemblyContactDebt(s.Authority);published+=result.PublishedCount;if(result.Status==AssemblyFlightStatus.BudgetExhausted)budget++;
                Check(result.PublishedCount<=4&&result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed or AssemblyFlightStatus.BudgetExhausted,"bounded partition service");
                var v=Observe(s);Check(v.Clock.Debt.Ticks==elapsed-v.State.Epoch.Ticks,"partition debt conservation");
            }
            while(published<1200){var v=s.Engine.ServiceAssemblyContactDebt(s.Authority);Check(v.PublishedCount is >0 and <=4&&v.Status is AssemblyFlightStatus.BudgetExhausted or AssemblyFlightStatus.Completed,"backlog progress");published+=v.PublishedCount;}
            for(var n=0;n<1200;n++)
            {
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,n,out var actual)&&actual==reference[n],"all frontier/history values equal");
                // Equality above is supplemented with binary64 bits, including signed zero.
                var a=actual.Successor.Motion;var b=reference[n].Successor.Motion;
                Check(Bits(a.PositionO,b.PositionO)&&Bits(a.VelocityO,b.VelocityO)&&Bits(a.AngularVelocityBody,b.AngularVelocityBody)&&Bits(a.BodyToWorld.X,b.BodyToWorld.X)&&Bits(a.BodyToWorld.Y,b.BodyToWorld.Y)&&Bits(a.BodyToWorld.Z,b.BodyToWorld.Z)&&Bits(a.BodyToWorld.W,b.BodyToWorld.W),"all frontier motion bits equal");
            }
            var final=Observe(s);var last=world.AssemblyIdentityForTest;
            Check(final.State.Frontier==1200&&final.State.Epoch.Ticks==20_000_000&&final.Clock.Debt.Ticks==0&&final.StateRevision.Value==1200&&final.HistoryCount==1200&&!final.PrivateInvalidated&&final.Consumer==AssemblyPhysicalConsumer.SupportedContact,"partition terminal authority");
            Check(last.Generation==identity.Generation&&last.Body==identity.Body&&last.Shape==identity.Shape&&!last.Pending&&!last.Invalidated,"partition native continuity");
            Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.Completed&&Observe(s)==final&&world.AssemblyIdentityForTest==last,"partition completed hold");
            Console.WriteLine($"FLORIDA_SCHEDULE PASS fps={fps} intervals={published} budgetExhausted={budget} allFrontiers=EXACT debt={final.Clock.Debt.Ticks}");
        }
    }
    private static bool Bits(double a,double b)=>BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a,Double3 b)=>Bits(a.X,b.X)&&Bits(a.Y,b.Y)&&Bits(a.Z,b.Z);
    internal static void Allocation(bool slab=false)
    {
        var launch=Launch(Site(slab));
        using(var s=AssemblyApplicationSession.CreateSupported(launch))
        {
            var v=Observe(s);for(var i=0;i<128;i++)Check(Operation(s,ref v),"allocation warmup");
            using var m=new OrdinaryAllocationMeasurement("florida-stock-complete-inertial");var ok=true;
            for(var i=0;i<1024;i++)ok&=Operation(s,ref v);
            var bytes=m.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"florida-stock-complete-inertial");Check(ok&&v.State.Frontier==1152,"measured full work");
        }
        using(var s=AssemblyApplicationSession.CreateSupported(launch))
        {
            Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(20_000_000)).Status==AssemblyFlightStatus.AcceptedCredit,"backlog credit");
            for(var i=0;i<32;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4,"backlog warmup");
            using var m=new OrdinaryAllocationMeasurement("florida-stock-backlog");var ok=true;
            for(var i=0;i<256;i++)ok&=s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4;
            var bytes=m.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"florida-stock-backlog");Check(ok,"backlog workload");
        }
        using(var s=AssemblyApplicationSession.CreateSupported(launch))
        {
            for(var i=0;i<128;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.AwaitingDebt,"no-work warmup");
            using var m=new OrdinaryAllocationMeasurement("florida-stock-no-work");var ok=true;
            for(var i=0;i<1024;i++)ok&=s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.AwaitingDebt;
            var bytes=m.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"florida-stock-no-work");Check(ok,"no-work result");
        }
        OrdinaryAllocationMeasurement.PositiveControl();
    }
    internal static void Costs(bool slab=false)
    {
        var site=Site(slab); // Shared terrain residency is measured separately from this bounded world.
        using(var warm=AssemblyApplicationSession.CreateSupported(Launch(site))){var v=Observe(warm);Check(Operation(warm,ref v),"cold code warmup");}
        var before=GC.GetAllocatedBytesForCurrentThread();var cold=Stopwatch.GetTimestamp();
        var launch=Launch(Site(slab));using var s=AssemblyApplicationSession.CreateSupported(launch);var coldMs=Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
        var v2=Observe(s);for(var i=0;i<128;i++)Check(Operation(s,ref v2),"cost warmup");
        var managed=GC.GetAllocatedBytesForCurrentThread()-before;var native=s.Engine.AssemblyContactWorldForTest(s.Authority)!.PoolBytes;
        Check(managed+(long)native<=8388608,"combined retained allocation upper bound");
        Console.WriteLine("FLORIDA_STORAGE "+JsonSerializer.Serialize(new{managedConstructionWarmUpperBound=managed,native,combinedUpperBound=managed+(long)native,historyPayloadIncluded=1200L*Unsafe.SizeOf<AssemblyFlightRecord>(),creditPayloadIncluded=4801L*Unsafe.SizeOf<AssemblyHostCredit>(),coldMs,limit=8388608}));
        var order=new double[1024];var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
        for(var i=0;i<1024;i++){var t=Stopwatch.GetTimestamp();var ok=Operation(s,ref v2);order[i]=Stopwatch.GetElapsedTime(t).TotalMilliseconds;Check(ok,"timed complete operation");}
        for(var i=0;i<3;i++)gc[i]=GC.CollectionCount(i)-gc[i];var sorted=(double[])order.Clone();Array.Sort(sorted);
        Console.WriteLine("FLORIDA_PERFORMANCE "+JsonSerializer.Serialize(new{median=(sorted[511]+sorted[512])/2,p95=sorted[972],p99=sorted[1013],maximum=sorted[^1],gc,warm=128,measured=1024,tails=order.Select((ms,index)=>new{ms,index}).OrderByDescending(x=>x.ms).Take(8)}));
        Console.WriteLine("Stage5 integrated headroom/tail review applies; no newly invented micro-budget.");
        while(v2.State.Frontier<1200)Check(Operation(s,ref v2),"storage final continuation");
        var nativeFinal=s.Engine.AssemblyContactWorldForTest(s.Authority)!.PoolBytes;
        Check(managed+(long)Math.Max(native,nativeFinal)<=8388608,"final retained upper bound including owned site/query");
        Console.WriteLine($"FLORIDA_STORAGE_FINAL native={nativeFinal} combinedUpperBound={managed+(long)Math.Max(native,nativeFinal)} ownedSiteAndQueryIncluded=true sharedPhysicalDatasetExcluded=true");
    }

    internal static void Presentation()
    {
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        Check(SampleOptions.TryParse(["--scene=srv01-florida-support"],out var option,out _)&&option.Scene=="srv01-florida-support"&&option.UseProductionEarth&&option.PhysicalSurfaceGeneration==PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate,"explicit Florida NCSM1 launch route");
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true);
        var initial=scene.Observation;var render=new RenderFrameSubmission(scene.RenderCapacity);
        Check(initial.State.Mass.Mass==705&&initial.Clock.Debt.Ticks==0&&scene.InitialSnapshot.Count==38,"stock ready with physical patch");
        scene.Advance(new(20_000_000));Check(scene.Observation==initial,"no startup credit");scene.Start();
        scene.Advance(new(16666));var first=scene.Observation;
        scene.BuildSubmission(default,new(new(5,3,8),new(1)),render);
        Check(render.ObjectCount==38&&render.Objects[37].Mesh==MeshHandle.FloridaSupportSlab,"one authored slab, no obsolete pad/foundation/qualification proxy");
        VerifyFloridaDisplay(scene,render);
        scene.BuildSubmission(default,new(new(-4,6,7),new(1)),render);Check(scene.Observation==first,"camera has no canonical authority");
        scene.Advance(new(20_000_000-16666));Check(scene.Observation.State.Frontier==5,"delayed display bounded four intervals");
        while(!scene.Completed&&!scene.Failed)scene.Advance(default);
        Check(scene.Completed&&!scene.Failed&&scene.Observation.State.Stores==initial.State.Stores&&scene.Observation.State.Frontier==1200,"Florida completion with exact stores");
        var session=Field<AssemblyApplicationSession>(scene,"session");
        ulong hash=14695981039346656037;
        void Mix(double value){unchecked{hash^=(ulong)BitConverter.DoubleToInt64Bits(value);hash*=1099511628211;}}
        for(var n=0;n<1200;n++)
        {
            Check(session.Engine.TryGetAssemblyHistory(session.Authority,n,out var h),"history at every published frontier");
            var m=h.Successor.Motion;
            Mix(m.PositionO.X);Mix(m.PositionO.Y);Mix(m.PositionO.Z);Mix(m.VelocityO.X);Mix(m.VelocityO.Y);Mix(m.VelocityO.Z);
            Mix(m.BodyToWorld.X);Mix(m.BodyToWorld.Y);Mix(m.BodyToWorld.Z);Mix(m.BodyToWorld.W);Mix(m.AngularVelocityBody.X);Mix(m.AngularVelocityBody.Y);Mix(m.AngularVelocityBody.Z);
        }
        Check(scene.Observation.Clock.Time.Ticks==20_000_000&&scene.Observation.StateRevision.Value==1200&&scene.Observation.TimelineRevision.Value==0&&scene.Observation.HistoryCount==1200,"slab trajectory, time, revisions and history");
        VerifyFloridaDisplay(scene,render);
        Console.WriteLine($"FLORIDA_SLAB_TRAJECTORY motionHash={hash:X16} intervals=1200 ticks=20000000 mass=705 fuel=30 oxidizer=45");
        var final=scene.Observation;scene.Advance(new(999));scene.BuildSubmission(default,new(default,new(1)),render);
        Check(scene.Observation==final&&!scene.ActiveExhaust&&render.ObjectCount==38,"final hold and no exhaust");
        using var measured=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true);measured.Start();
        for(var n=0;n<128;n++){measured.Advance(new(n%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
        using(var m=new OrdinaryAllocationMeasurement("florida-stock-application-presentation"))
        {
            for(var n=128;n<1152;n++){measured.Advance(new(n%3==0?16666:16667));measured.BuildSubmission(default,new(default,new(1)),render);}
            OrdinaryAllocationMeasurement.RequireZero(m.Complete(),"florida-stock-application-presentation");
        }
        Check(!measured.Failed&&measured.Observation.State.Frontier==1152,"measured presentation continued");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"FLORIDA_PRESENTATION PASS copiedCanonicalEarthFixedDisplay=true terrainV5=true singleAuthoredSlab=true visualMeshBytes={scene.Visuals.BufferBytes} displaySampleArraysBytes=640000 engine=OFF manualAcceptance=NOT_GRANTED");
    }
    private static void VerifyFloridaDisplay(StockAssemblyDevelopmentScene scene,RenderFrameSubmission render)
    {
        var view=scene.FloridaView!;var solar=view.Solar;var site=view.Site;var before=scene.Observation;
        var slab=site.Slab!;
        Check(slab is not null&&slab.Authority==site.Authority&&site.EastMetres==0&&site.Applicable,"explicit slab/site authority, no old east offset");
        Check(slab!.Dimensions.X==64&&slab.Dimensions.Z==48&&Math.Abs(slab.Dimensions.Y-8.335569277405739)<1e-9,"authored base-footing union");
        Check(Math.Abs(slab.TopRadius-6371008.8-23.470461536198854)<1e-8,"unchanged authored top oracle");
        Check(solar.Presentation.TryGetBody(6,out var earth),"Solar Earth present");
        var region=FloridaFacilitySupport.Region;
        var session=Field<AssemblyApplicationSession>(scene,"session");
        var world=session.Engine.AssemblyContactWorldForTest(session.Authority)!;
        var sim=Field<BepuPhysics.Simulation>(world,"simulation");
        var stat=sim.Statics[Field<BepuPhysics.StaticHandle>(world,"plane")];
        var shape=sim.Shapes.GetShape<BepuPhysics.Collidables.Box>(stat.Shape.Index);
        Check(sim.Statics.Count==1&&shape.Width==64&&shape.Length==48&&shape.Height==(float)slab.Dimensions.Y&&stat.Pose.Position.Y==-shape.Height*.5f,"one retained finite physical slab, top native y=0");
        var bindings=Field<(int Part,PartVisualMesh Mesh)[]>(scene,"presentedMeshes");
        foreach(var eye in new[]{new Double3(12,7,16),new Double3(-5,9,-12)})
        {
            var camera=new UniversePosition(view.Position(eye).Value,view.Root);scene.BuildSubmission(default,camera,render);
            for(var i=0;i<bindings.Length;i++)
            {
                var binding=bindings[i];var part=session.Launch.Design.Parts[binding.Part];var motion=before.State.Motion;
                var meshOrigin=binding.Mesh.Gimballed?part.Instance.Pose.Point(part.Definition.Gimbal!.Pivot):part.Instance.Pose.Position;
                var local=motion.PositionO+motion.BodyToWorld.Rotate(meshOrigin);
                var bf=region.Up*(6371008.8+23.470461536198854+1.7+local.Y)+region.East*local.X-region.North*local.Z;
                var expected=earth.Position.Value+earth.BodyFixedToRoot.Rotate(bf)-camera.Value;
                Check((render.Objects[i].Position.Reconstruct()-expected).LengthSquared<2.5e-9,"independent canonical material-origin placement");
            }
            var rendered=render.Objects[37];var centre=view.Position(new(0,-1.7-slab.Dimensions.Y*.5,0)).Value-camera.Value;
            Check((rendered.Position.Reconstruct()-centre).LengthSquared<2.5e-9,"visible and physical slab centre agreement");
            Check(rendered.Transform.Scale==Float3.FromDouble3(slab.Dimensions),"visible slab scale matches physical authored dimensions");
            var rq=rendered.Transform.Rotation;var rm=Q(new(rq.X,rq.Y,rq.Z,rq.W));
            // Independent ENU basis and transformed unit-box corners test rotation, handedness and scale together.
            for(var cornerIndex=0;cornerIndex<8;cornerIndex++)
            {
                var corner=new Double3(((cornerIndex&1)==0?-.5:.5)*64,((cornerIndex&2)==0?-.5:.5)*slab.Dimensions.Y,((cornerIndex&4)==0?-.5:.5)*48);
                var expectedOffset=earth.BodyFixedToRoot.Rotate(region.East*corner.X+region.Up*corner.Y-region.North*corner.Z);
                Check(Norm(rm.Apply(corner)-expectedOffset)<1e-5,"independent slab basis/corner orientation agrees within FP32 render transport");
            }
            Check(render.ObjectCount==38&&rendered.Mesh==MeshHandle.FloridaSupportSlab,"exactly one slab, no cube/arms/duplicate qualification slab");
        }
        Check(scene.Observation==before,"presentation nonmutation");
        Console.WriteLine($"FLORIDA_SLAB_DISPLAY PASS site={site.Digest} source={FloridaSlabSupport.Identity} dimensions={slab.Dimensions} nativeTop=0 materialOriginAboveTop=1.7 terrainIsNotContact=true");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmScene()
    {using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true);scene.Start();scene.Advance(new(16666));}
    internal static void PresentationStorage()
    {
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        WarmScene();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();var before=GC.GetTotalMemory(true);
        using var scene=new StockAssemblyDevelopmentScene(supportedContact:true,floridaSupport:true);
        var render=new RenderFrameSubmission(scene.RenderCapacity);var objects=new NovaCore.Interop.NativeRenderObject[scene.RenderCapacity];
        scene.Start();for(var i=0;i<1200;i++){scene.Advance(new(i%3==0?16666:16667));scene.BuildSubmission(default,new(default,new(1)),render);}
        Check(scene.Completed&&!scene.Failed,"storage whole episode completed");
        var session=Field<AssemblyApplicationSession>(scene,"session");var pool=(long)session.Engine.AssemblyContactWorldForTest(session.Authority)!.PoolBytes;
        var managed=GC.GetTotalMemory(true)-before;var visual=scene.Visuals.BufferBytes;
        // Native CreateMeshes builds the extra slab as 6 faces x 4 vertices / 6 indices.
        var slabMeshPayload=24L*Unsafe.SizeOf<NovaCore.Interop.NativeVisualVertex>()+36L*sizeof(uint);
        GC.KeepAlive(scene);GC.KeepAlive(render);GC.KeepAlive(objects);
        Check(managed>=640000+visual&&managed+pool+visual+slabMeshPayload<=8388608,"owned scene/native/mesh payload within retained budget");
        Console.WriteLine($"FLORIDA_PRESENTATION_STORAGE managedRetainedDelta={managed} nativePool={pool} gpuMeshPayload={visual} slabMeshPayload={slabMeshPayload} combined={managed+pool+visual+slabMeshPayload} limit=8388608 sharedTerrainDatasetAndDeviceExcluded=true");
    }
}
