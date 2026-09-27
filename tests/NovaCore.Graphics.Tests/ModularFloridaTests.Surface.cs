using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceRecontact()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var collision=(IPhysicalSurfaceCollisionSource)terrain.Query;var region=FloridaFacilitySupport.Region;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        Need(collision.TryPreparePatch(frame,new(76,-4),new(80,0),out var parent)&&parent is not null,"immutable root preparation");
        Need(parent!.TrySubpatch(new(77,-3),new(79,-1),out var child)&&child is not null&&child.TrySubpatch(new(77.5,-2.5),new(78.5,-1.5),out _),"contained nested preparation retains numerical authority");
        foreach(var bad in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})for(var field=0;field<4;field++)
        {
            var values=new[]{77d,-3,79,-1};values[field]=bad;
            Need(!parent.TrySubpatch(new(values[0],values[1]),new(values[2],values[3]),out _),"each nonfinite subpatch bound refuses locally");
        }
        Need(!parent.TrySubpatch(new(79,-1),new(77,-3),out _)&&!parent.TrySubpatch(new(Math.BitDecrement(76),-4),new(80,0),out _),"reversed and one-ULP-outside subpatch refuse");
        Need(parent.TrySubpatch(new(76,-4),new(80,0),out var exact),"exact parent boundary admitted");
        var outcomes=new bool[32];
        Parallel.For(0,outcomes.Length,i=>
        {
            var a=new PhysicalPatchCoordinate(77+(i%4)*.1,-3+(i/4)*.1);var b=new PhysicalPatchCoordinate(a.X+.05,a.Y);var c=new PhysicalPatchCoordinate(a.X,a.Y+.05);
            outcomes[i]=child!.TryTriangleError(a,b,c,out var reused)&&collision.TryTriangleError(frame,a,b,c,out var fresh)&&double.IsFinite(reused+fresh)&&
                child.CollisionPoint(a)==collision.CollisionPoint(frame,a)&&exact!.CollisionPoint(a)==collision.CollisionPoint(frame,a);
        });
        Need(outcomes.All(x=>x),"concurrent reordered shared samples equal fresh authoritative H exactly");
        // Capacity is retention only. More unique points than the scratch cap
        // must still evaluate the exact authority and remain deterministic.
        for(var i=0;i<4200;i++)
        {
            var p=new PhysicalPatchCoordinate(76+(i%70)/70d,-4+(i/70)/60d);
            Need(parent.CollisionPoint(p)==collision.CollisionPoint(frame,p),"full sample cache never substitutes, truncates or drops H");
        }
        Need(collision.TryPreparePatch(frame,new(-1,-1),new(1,1),out var zeroPatch),"signed zero patch");
        foreach(var zero in new[]{0d,BitConverter.Int64BitsToDouble(long.MinValue)})
            Need(zeroPatch!.CollisionPoint(new(zero,zero))==collision.CollisionPoint(frame,new(zero,zero)),"signed-zero source ordering retained");
        Need(!parent.TryTriangleError(new(76,-4),new(76,-4),new(76,-4),out _),"degenerate triangle cannot acquire an error certificate");
        Need(!parent.TryTriangleError(new(76,-4),new(80,-4),new(76,0),out _,0),"cheap error refusal does not become geometry admission");
        var polarRefused=false;try{_=NovaCore.Graphics.EarthElevationDataset.CollisionBounds(100,new(new(-1e-12,-.5e-12),0,0,0,0,0),2e-12);}catch(InvalidDataException){polarRefused=true;}
        Need(polarRefused,"actual loaded geographic lookup refuses finite polar discontinuity collar");
        foreach(var x in new[]{0d,63.5,64,70,80,100,150,200,300})foreach(var size in new[]{1d,.25,.0625})
        {
            var a=new PhysicalPatchCoordinate(x,0);var b=new PhysicalPatchCoordinate(x+size,0);var c=new PhysicalPatchCoordinate(x,size);
            var ok=collision.TryTriangleError(frame,a,b,c,out var bound);
            Console.WriteLine($"TERRAIN_BOUND x={x} size={size} ready={ok} bound={bound:R}");
            if(!ok){
                ((NovaCore.Graphics.PlanetaryPhysicalSurfacePointQuery)terrain.Query).TryCollisionTriangle(frame,a,b,c,out _,out var detail);Console.WriteLine("TRIANGLE="+detail);
                var xx=NovaCore.Graphics.CollisionJet.Variable(new(x,x+size),true);var yy=NovaCore.Graphics.CollisionJet.Variable(new(0,size),false);
                var ray=new NovaCore.Graphics.CollisionVector(region.Up.X*region.RadiusMetres+region.East.X*xx+region.North.X*yy,
                    region.Up.Y*region.RadiusMetres+region.East.Y*xx+region.North.Y*yy,region.Up.Z*region.RadiusMetres+region.East.Z*xx+region.North.Z*yy);
                try{var addressed=NovaCore.Graphics.PhysicalCollisionAddressBounds.TryRegional(ray,out var face,out var u,out var v,out var e,out var reason);
                    Console.WriteLine($"ADDRESS={addressed} reason={reason} face={face} u={u.V} v={v.V} error={e:R}");
                    if(addressed)Console.WriteLine($"REGIONAL={NovaCore.Graphics.EarthLocalTerrainElevationDataset.TryCollisionBounds(face,u,v,e,out _,out var re)} error={re:R}");
                }catch(Exception ex){Console.WriteLine(ex);}
            }
            Need(ok,"finite Florida vicinity terrain bound");
            var pa=collision.CollisionPoint(frame,a);var pb=collision.CollisionPoint(frame,b);var pc=collision.CollisionPoint(frame,c);
            var normal=Double3.Cross(pb-pa,pc-pa).Normalized();var planeConstant=Double3.Dot(normal,pa);
            for(var i=0;i<7;i++)for(var j=0;j<7-i;j++)
            {
                var u=(i+.13)/8;var v=(j+.27)/8;var sample=collision.CollisionPoint(frame,new(x+u*size,v*size));
                var radial=sample.Normalized();var triangleRadius=planeConstant/Double3.Dot(normal,radial);
                Need(Math.Abs(Norm(sample)-triangleRadius)<=bound,"independent radial intersection challenges certified full H remainder");
            }
        }
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
        var checkpoint=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
        using(var rowFixture=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab))
        {
            var world=rowFixture.Engine.ConstructionContactWorldForTest(rowFixture.Authority)!;
            var metrics=Field<NovaCore.Simulation.Spacecraft.Contact.Staging.LocalContactMetrics>(world,"metrics").Craft!;
            var pair=new BepuPhysics.CollisionDetection.CollidablePair(new BepuPhysics.Collidables.CollidableReference(BepuPhysics.Collidables.CollidableMobility.Dynamic,Field<BepuPhysics.BodyHandle>(world,"body")),new BepuPhysics.Collidables.CollidableReference(Field<BepuPhysics.StaticHandle>(world,"plane")));
            var before=rowFixture.Save();
            foreach(var bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})for(var field=0;field<3;field++)
            {
                metrics.Begin(Observe(rowFixture).ReferenceMass!.Value);
                var manifold=new BepuPhysics.CollisionDetection.ConvexContactManifold{Count=1,Normal=field==1?new(bad,1,0):System.Numerics.Vector3.UnitY};
                manifold[0]=new(){Depth=field==0?bad:0,Offset=field==2?new(bad,0,0):default};
                Need(!metrics.Child(0,pair,0,0,ref manifold)&&metrics.Failed,"nonfinite pad row refuses before private reconciliation");
                Need(before.SequenceEqual(rowFixture.Save()),"malformed native row cannot mutate canonical source");
            }
        }
        var site=cold.Binding.Physical!.Site;var tolerance=cold.Binding.Physical.Contact.ContactTolerance;
        foreach(var x in new[]{0d,80d})
        {
            double Clearance(Double3 local)
            {
                if(x==0)return local.Y-site.SupportPlane;
                var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(point,region.Up);
                return Norm(point)-Norm(collision.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up)));
            }
            var hulls=craft.Collision.Select(h=>h.Vertices.Select(v=>new Double3(x,0,0)+AssemblyContactProfile.Upright.Rotate(v)).ToArray()).ToArray();
            var shift=-hulls.Min(h=>h.Max(Clearance))-2*tolerance;
            Need(hulls.Any(h=>h.All(v=>Clearance(v+new Double3(0,shift,0))< -tolerance))&&hulls.Any(h=>h.Any(v=>Clearance(v+new Double3(0,shift,0))>tolerance)),"one submerged child cannot hide behind exterior vertices of another child");
            var buried=checkpoint with{Physical=new(new(new(x,shift,0),default,AssemblyContactProfile.Upright,default),default,AssemblyPhysicalConsumer.SurfaceContact)};
            var before=cold.Save();Refuse(()=>{using var rejected=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(buried),Assets,terrain.Query,terrain.Slab);},"per-child buried checkpoint");
            Need(before.SequenceEqual(cold.Save()),"per-child burial refusal preserves accepted craft and resources");
        }
        var entry=checkpoint with {Physical=new(new(new(0,.03,0),new(0,-.2,0),AssemblyContactProfile.Upright,default),default,AssemblyPhysicalConsumer.FreeFlight)};
        using var s=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(entry),Assets,terrain.Query,terrain.Slab);
        Need(Observe(s).Physical==entry.Physical,"restore imports the exact airborne endpoint without seating");
        var contact=0;var maximumDepth=0d;
        for(var i=1;i<=192;i++)
        {
            Need(s.Engine.AdmitConstructionHostTime(s.Authority,i,new(15625))==ConstructionServiceStatus.AcceptedCredit,"recontact host credit");
            var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);
            Need(status==ConstructionServiceStatus.Published&&count==1,$"recontact step {i}: {status}, {s.Engine.ConstructionSupportFailure(s.Authority)}");
            var history=s.Engine.ObserveConstructionPhysicalHistory(s.Authority);contact+=history.Records[^1].Contacts;
            var m=Observe(s).Physical!.Motion;maximumDepth=Math.Max(maximumDepth,-m.PositionO.Y);
        }
        Need(contact>0&&Observe(s).Physical!.Consumer==AssemblyPhysicalConsumer.SurfaceContact,"real retained post-flight contact");
        Need(maximumDepth<.002&&Norm(Observe(s).Physical!.Motion.VelocityO)<.01,"gentle landing depth and rest");
        var saved=s.Save();using var restored=ConstructionApplicationSession.RestoreFlight(catalog,saved,Assets,terrain.Query,terrain.Slab);
        Need(Observe(restored).Physical==Observe(s).Physical&&Observe(restored).ReferenceMass==Observe(s).ReferenceMass&&restored.Binding.FlightId==s.Binding.FlightId,"grounded exact endpoint, mass and flight identity reload");
        Need(restored.Engine.CaptureContinuationClock()==s.Engine.CaptureContinuationClock()&&restored.Save().SequenceEqual(saved),"physical checkpoint canonical roundtrip");
        Console.WriteLine($"SURFACE_RECONTACT_PASS checks={checks} contacts={contact} depth={maximumDepth:R}");
    }
}
