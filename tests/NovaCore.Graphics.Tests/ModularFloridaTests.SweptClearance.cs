using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SweptClearance()
    {
        checks=0;var terrain=Terrain();var source=(IPhysicalSurfaceCollisionSource)terrain.Query;
        var region=FloridaFacilitySupport.Region;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        // Independently evaluate full H, including samples inside triangles and
        // at tile edges. This corroborates the analytical interval enclosure.
        foreach(var tile in new[]{(-4d,-4d),(0d,0d),(60d,52d),(64d,56d),(76d,-4d),(80d,0d),(188d,180d),(200d,200d)})
        foreach(var angle in new[]{0d,.000001})
        {
            var rotation=DoubleQuaternion.FromAxisAngle(region.North,angle);
            var f=new PhysicalCollisionFrame(rotation.Rotate(frame.Radial),rotation.Rotate(frame.East),frame.North,frame.Radius);
            Need(source.TryPreparePatch(f,new(tile.Item1,tile.Item2),new(tile.Item1+4,tile.Item2+4),out var p)&&p is not null,"full-H height preparation");
            Need(p!.TryFloridaHeightRange(out var lo,out var hi),"finite full-H height envelope");
            for(int x=0;x<=16;x++)for(int y=0;y<=16;y++)
            {
                var point=source.CollisionPoint(f,new(tile.Item1+x*.25,tile.Item2+y*.25));
                var height=Double3.Dot(point,region.Up)-region.RadiusMetres;
                Need(height>=lo&&height<=hi,$"independent H projection in [{lo:R},{hi:R}]: {height:R}");
            }
        }
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using var supported=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab,new(18_000_000_000));
        var profile=supported.Binding.Physical!;var clearance=profile.Clearance;var s=clearance.Swept;
        CraftSweptTerrainClearance.Vector Box(double x,double y,double z,double r=.02)=>new(new(x-r,x+r),new(y-r,y+r),new(z-r,z+r));
        var half=terrain.Slab.Dimensions*.5;var top=profile.Site.SupportPlane;
        Need(!s.Clears(Box(0,top-.01,0),out _),"finite slab top collision refuses");
        Need(!s.Clears(Box(half.X,top-.1,half.Z),out _),"finite slab corner overlap refuses");
        Need(!s.Clears(Box(0,top-terrain.Slab.Dimensions.Y,0),out _),"finite slab underside overlap refuses");
        Need(s.Clears(Box(half.X+.1,top+.1,half.Z+.1),out _),"slab horizontal corners cannot veto disjoint swept box");
        Need(!s.Clears(new(new(double.NaN,1),new(0,1),new(0,1)),out _),"nonfinite sweep refused");
        Need(!s.Clears(Box(0,-2*region.RadiusMetres,0),out _),"nonpositive gnomonic denominator refused");
        Need(!s.Clears(Box(PhysicalCollisionFrame.MaximumCoordinate*2,1000,0),out _),"unsupported local domain refused");
        var before=supported.Save();
        foreach(var x in new[]{-4d,Math.BitDecrement(-4d),Math.BitIncrement(-4d),0d,Math.BitDecrement(4d),4d,Math.BitIncrement(4d),80d})
        {
            s.ClearCache();var box=Box(x,300,0);var cold=s.Clears(box,out var a);var hot=s.Clears(box,out var b);
            Need(cold&&hot&&a==b,"cold/warm exact certificate equality at signed tile boundary");
            for(int k=0;k<80;k++)s.Clears(Box(240+8*k,300,0),out _);
            Need(s.Cached<=64&&s.Clears(box,out var c)==cold&&c==a,"eviction history changes no certificate");
        }
        Need(before.SequenceEqual(supported.Save()),"certificate/cache observation cannot mutate source");
        var initial=Observe(supported);var mass=initial.ReferenceMass!.Value;
        var upright=AssemblyContactProfile.Upright;
        var safe=new AssemblyMotion(new(80,10,0),new(0,2,0),upright,default);
        Need(clearance.Clears(safe,mass,initial.Epoch,15625,out _),"positive post-contact local clearance");
        Need(!clearance.Clears(safe with{VelocityO=new(0,-2000,0)},mass,initial.Epoch,15625,out _),"high-speed downward crossing protected");
        Need(!clearance.Clears(safe with{AngularVelocityBody=new(1000,0,0)},mass,initial.Epoch,15625,out _),"rotating hull approach protected");
        Need(!clearance.Clears(safe,mass,initial.Epoch,15626,out _),"uncertified interval refused");
        // Full trajectory enclosure for convex hulls under independently
        // constructed constant accelerations and angular rates.
        var motion=safe with{VelocityO=new(-2,3,1),AngularVelocityBody=new(.4,.2,-.3)};
        const double h=1d/64;var omega=Norm(motion.AngularVelocityBody);var radius=craft.Collision.SelectMany(c=>c.Vertices).Max(v=>Norm(v));
        var acceleration=new Double3(2,-4,1);var expand=.5*Norm(acceleration)*h*h+radius*omega*h+.001;
        var swept=CraftSweptTerrainClearance.SweptBox(craft,motion,motion.VelocityO,h,expand);
        for(int i=0;i<=64;i++)foreach(var v in craft.Collision.SelectMany(c=>c.Vertices))
        {
            var t=h*i/64;var q=motion.BodyToWorld*DoubleQuaternion.FromAxisAngle(motion.AngularVelocityBody,omega*t);
            var point=motion.PositionO+motion.VelocityO*t+acceleration*(.5*t*t)+q.Rotate(v);
            Need(point.X>=swept.X.Low&&point.X<=swept.X.High&&point.Y>=swept.Y.Low&&point.Y<=swept.Y.High&&point.Z>=swept.Z.Low&&point.Z<=swept.Z.High,"continuous swept box encloses sampled accelerated rotating vertices");
        }
        OffPadReturn();Cadence();Console.WriteLine($"SWEPT_CLEARANCE_PASS checks={checks} cache={s.Cached}");
        void OffPadReturn()
        {
            var state=AssemblyJson.Read<ConstructionFlightCheckpoint>(supported.Save()) with{
                Physical=new(new(new(80,-3,0),default,upright,default),default,AssemblyPhysicalConsumer.FreeFlight)};
            using var falling=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(state),Assets,terrain.Query,terrain.Slab);
            var p=falling.Binding.Physical!;var start=Observe(falling);
            Need(p.Clearance.Clears(start.Physical!.Motion,start.ReferenceMass!.Value,start.Epoch,15625,out _),"off-pad return begins with positive new certificate");
            var sawClear=false;var importedAt=-1;var touchedAt=-1;
            for(int tick=0;tick<256;tick++)
            {
                var prior=Observe(falling);var clear=p.Clearance.Clears(prior.Physical!.Motion,prior.ReferenceMass!.Value,prior.Epoch,15625,out _);
                var hadWorld=falling.Engine.ConstructionContactWorldForTest(falling.Authority) is not null;
                Need(falling.Engine.AdmitConstructionHostTime(falling.Authority,tick+1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"off-pad falling credit");
                Need(falling.Engine.ServiceConstructionDebt(falling.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"off-pad recontact continuation");
                var value=SurfaceRetryObservation.Read(falling);
                if(clear&&prior.Physical.Consumer==AssemblyPhysicalConsumer.FreeFlight){Need(value.World==0,"certified free flight retires native world");sawClear=true;}
                if(!clear&&!hadWorld&&value.World>0&&importedAt<0)importedAt=tick;
                if(value.TerrainContacts>0&&touchedAt<0)touchedAt=tick;
            }
            var final=SurfaceRetryObservation.Read(falling);
            // A drop can still rock/tip at this finite endpoint. The contract is
            // ordinary physical continuation, not rest at an arbitrary time.
            Need(sawClear&&importedAt>=0&&touchedAt>=importedAt&&final.TerrainContacts>0&&final.Consumer==(int)AssemblyPhysicalConsumer.SurfaceContact,"new certificate releases to real off-pad terrain recontact and physical continuation");
            var finalMotion=Observe(falling).Physical!.Motion;
            foreach(var hull in craft.Collision)
            {
                var highest=double.NegativeInfinity;
                foreach(var v in hull.Vertices)
                {
                    var point=p.Site.OriginBodyFixed+p.Site.LocalToBodyFixed.Rotate(finalMotion.PositionO+finalMotion.BodyToWorld.Rotate(v));
                    var up=Double3.Dot(point,region.Up);var ground=source.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up));
                    highest=Math.Max(highest,Norm(point)-Norm(ground));
                }
                Need(highest>=-p.Contact.ContactTolerance,"no whole convex child buried beneath authoritative H");
            }
            Console.WriteLine($"POST_CONTACT_OFF_PAD_RETURN importedAt={importedAt} touchedAt={touchedAt} contacts={final.TerrainContacts} speed={Norm(final.Velocity):R}");
        }
        void Cadence()
        {
            var vertices=craft.Collision.SelectMany(h=>h.Vertices).Select(v=>upright.Rotate(v)).ToArray();
            var origin=new Double3(80,0,0);
            double Gap(Double3 local){var point=profile.Site.OriginBodyFixed+profile.Site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(point,region.Up);return Norm(point)-Norm(source.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up)));}
            origin+=new Double3(0,-vertices.Min(v=>Gap(origin+v))+.01,0);
            var saved=AssemblyJson.Read<ConstructionFlightCheckpoint>(supported.Save()) with{Physical=new(new(origin,new(.1,-.1,0),upright,default),default,AssemblyPhysicalConsumer.FreeFlight)};
            var bytes=AssemblyJson.Write(saved);
            ConstructionRuntimeState? expected=null;
            foreach(var fps in new[]{150,60,30,0})
            {
                using var session=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
                long host=0,command=0,total=0,maxDebt=0;var callbacks=0;var published=0;
                foreach(var phase in new[]{(Ticks:2_000_000L,Request:new AssemblyControlRequest(false)),(Ticks:62_500L,Request:new AssemblyControlRequest(false,new(0,0,1))),
                    (Ticks:1_000_000L,Request:new AssemblyControlRequest(false)),(Ticks:2_000_000L,Request:new AssemblyControlRequest(true))})
                {
                    Need(session.Engine.AdmitAssemblyControl(session.Control!,session.Control!.Identity,++command,phase.Request).Status==AssemblyControlStatus.Admitted,"same canonical command frontier at every display cadence");
                    long admitted=0;int frameIndex=0;
                    while(admitted<phase.Ticks)
                    {
                        var credit=fps==0?new long[]{1,90000,2222,150000,6667}[frameIndex%5]:((frameIndex+1L)*1_000_000/fps-frameIndex*1_000_000L/fps);
                        credit=Math.Min(credit,phase.Ticks-admitted);frameIndex++;admitted+=credit;total+=credit;
                        Need(session.Engine.AdmitConstructionHostTime(session.Authority,++host,new(credit))==ConstructionServiceStatus.AcceptedCredit,"cadence host credit");
                        maxDebt=Math.Max(maxDebt,session.Clock.PendingSimulationDebt.Ticks);Service();
                    }
                    while(session.Clock.PendingSimulationDebt.Ticks>=15625)Service();
                }
                var actual=Observe(session);
                Need(total==5_062_500&&session.Clock.PendingSimulationDebt.Ticks==0&&published==324&&actual.Sequence==324,"all admitted work retired; no hidden debt");
                if(expected is null)expected=actual;else Need(actual.Physical==expected.Physical&&actual.ReferenceMass==expected.ReferenceMass&&actual.Fuel.Save().SequenceEqual(expected.Fuel.Save())&&actual.Power.Save().SequenceEqual(expected.Power.Save())&&actual.Epoch==expected.Epoch,"physics/resources independent of150/60/30/irregular display cadence");
                var checkpoint=session.Save();using var restored=ConstructionApplicationSession.RestoreFlight(catalog,checkpoint,Assets,terrain.Query,terrain.Slab);
                Need(restored.Save().SequenceEqual(checkpoint),"handoff flight checkpoint identity");
                Console.WriteLine($"POST_CONTACT_CADENCE fps={fps} callbacks={callbacks} published={published} admitted={total} retired={published*15625L} maxDebt={maxDebt} finalDebt=0");
                void Service(){var status=session.Engine.ServiceConstructionDebt(session.Authority,out var n);Need(status is ConstructionServiceStatus.Published or ConstructionServiceStatus.AwaitingDebt,"cadence service");callbacks++;published+=n;}
            }
        }
    }
}
