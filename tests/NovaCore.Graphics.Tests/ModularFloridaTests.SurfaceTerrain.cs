using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceTerrain()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var compiled=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using var cold=ConstructionApplicationSession.CreateSupported(compiled,terrain.Query,terrain.Slab);
        var site=cold.Binding.Physical!.Site;var region=FloridaFacilitySupport.Region;
        var collision=(IPhysicalSurfaceCollisionSource)terrain.Query;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        var orientation=AssemblyContactProfile.Upright;var origin=new Double3(80,0,0);var height=double.NegativeInfinity;
        foreach(var hull in compiled.Collision)foreach(var vertex in hull.Vertices)
        {
            var local=origin+orientation.Rotate(vertex);var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);
            var up=Double3.Dot(point,region.Up);
            var coordinate=new PhysicalPatchCoordinate(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up);
            var ground=site.LocalToBodyFixed.Conjugate().Rotate(collision.CollisionPoint(frame,coordinate)-site.OriginBodyFixed);
            height=Math.Max(height,ground.Y-local.Y);
        }
        origin+=new Double3(0,height+.01,0);
        var saved=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save()) with
            {Physical=new(new(origin,new(.1,-.1,0),orientation,default),default,AssemblyPhysicalConsumer.FreeFlight)};
        var timer=Stopwatch.StartNew();
        using var session=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(saved),Assets,terrain.Query,terrain.Slab);
        Field<NovaCore.Simulation.Spacecraft.Contact.Staging.LocalContactMetrics>(session.Engine.ConstructionContactWorldForTest(session.Authority)!,"metrics").Terrain!.CaptureForTest();
        Console.WriteLine($"TERRAIN_RESTORE ms={timer.Elapsed.TotalMilliseconds:R} origin={origin}");var contacts=0;
        for(var i=1;i<=192;i++)
        {
            var before=Observe(session);
            Need(session.Engine.AdmitConstructionHostTime(session.Authority,i,new(15625))==ConstructionServiceStatus.AcceptedCredit,"terrain host credit");
            var status=session.Engine.ServiceConstructionDebt(session.Authority,out var count);
            if(status!=ConstructionServiceStatus.Published)
            {
                var world=session.Engine.ConstructionContactWorldForTest(session.Authority)!;
                var sim=Field<BepuPhysics.Simulation>(world,"simulation");var body=sim.Bodies[Field<BepuPhysics.BodyHandle>(world,"body")];
                var config=Field<NovaCore.Simulation.Spacecraft.Contact.Staging.LocalContactConfiguration>(world,"configuration");
                var metric=Field<NovaCore.Simulation.Spacecraft.Contact.Staging.LocalContactMetrics>(world,"metrics");
                var witness=metric.Terrain!.Deepest;Console.WriteLine($"CONTACT_WITNESS {witness}");
                foreach(var row in Field<BepuPhysics.CollisionDetection.Contact[]>(metric.Terrain,"rows").Take(Field<int>(metric.Terrain,"count")))Console.WriteLine($"PARENT_ROW depth={row.Depth:R} normal={row.Normal} offset={row.Offset} feature={row.FeatureId}");
                foreach(var raw in metric.Terrain.RawForTest!.Where(r=>r.Triangle==12728))Console.WriteLine($"MATCHED_RAW {raw}");
                var terrainOwner=Field<NovaCore.Simulation.Spacecraft.Contact.Staging.CraftTerrainColliders>(world,"craftTerrain");
                var meshShape=Field<BepuPhysics.Collidables.TypedIndex>(terrainOwner,"shape");
                ref var mesh=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Mesh>(meshShape.Index);
                var triangle=mesh.Triangles[witness.Triangle];
                Console.WriteLine($"CONTACT_TRIANGLE a={triangle.A} b={triangle.B} c={triangle.C}");
                var q=body.Pose.Orientation;var actualQ=(new DoubleQuaternion(q.X,q.Y,q.Z,q.W)*AssemblyContactProfile.Upright).Normalized();
                var center=config.OriginRoot+new Double3(body.Pose.Position.X,body.Pose.Position.Y,body.Pose.Position.Z);
                var minimum=double.PositiveInfinity;var worst=Double3.Zero;
                foreach(var hull in compiled.Collision)foreach(var vertex in hull.Vertices)
                {
                    var local=center+actualQ.Rotate(vertex-before.ReferenceMass!.Value.Com);
                    var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var radial=point.Normalized();
                    var ground=collision.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(radial,region.East)/Double3.Dot(radial,region.Up),region.RadiusMetres*Double3.Dot(radial,region.North)/Double3.Dot(radial,region.Up)));
                    var clearance=Norm(point)-Norm(ground);if(clearance<minimum){minimum=clearance;worst=local;}
                }
                Console.WriteLine($"GEOMETRIC_VERTEX clearance={minimum:R} worst={worst} native={body.Pose.Position} angular={body.Velocity.Angular}");
                ref var compound=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Compound>(Field<BepuPhysics.Collidables.TypedIndex>(world,"bodyShape").Index);
                var minFace=double.PositiveInfinity;var faceWorst=Double3.Zero;
                var nativeQ=new DoubleQuaternion(q.X,q.Y,q.Z,q.W).Normalized();
                for(var childIndex=0;childIndex<compound.Children.Length;childIndex++)
                {
                    ref var child=ref compound.Children[childIndex];ref var hull=ref sim.Shapes.GetShape<BepuPhysics.Collidables.ConvexHull>(child.ShapeIndex.Index);
                    for(var faceIndex=0;faceIndex<hull.FaceToVertexIndicesStart.Length;faceIndex++)
                    {
                        hull.GetVertexIndicesForFace(faceIndex,out var indices);var vertices=new Double3[indices.Length];
                        for(var j=0;j<indices.Length;j++)
                        {
                            hull.GetPoint(indices[j],out var v);var cq=child.LocalOrientation;
                            vertices[j]=center+nativeQ.Rotate(new Double3(child.LocalPosition.X,child.LocalPosition.Y,child.LocalPosition.Z)+new DoubleQuaternion(cq.X,cq.Y,cq.Z,cq.W).Rotate(new(v.X,v.Y,v.Z)));
                        }
                        for(var fan=1;fan<vertices.Length-1;fan++)for(var u=0;u<=32;u++)for(var v=0;v<=32-u;v++)
                        {
                            var local=vertices[0]+(vertices[fan]-vertices[0])*(u/32d)+(vertices[fan+1]-vertices[0])*(v/32d);
                            var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var radial=point.Normalized();
                            var ground=collision.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(radial,region.East)/Double3.Dot(radial,region.Up),region.RadiusMetres*Double3.Dot(radial,region.North)/Double3.Dot(radial,region.Up)));
                            var clearance=Norm(point)-Norm(ground);if(clearance<minFace){minFace=clearance;faceWorst=local;}
                        }
                    }
                }
                Console.WriteLine($"GEOMETRIC_FACE_SAMPLE clearance={minFace:R} worst={faceWorst}");
            }
            Need(status==ConstructionServiceStatus.Published&&count==1,$"terrain step {i}: {status}; {session.Engine.ConstructionSupportFailure(session.Authority)}");
            contacts+=session.Engine.ObserveConstructionPhysicalHistory(session.Authority).Records[^1].Contacts;
            if(i==8)
            {
                var transition=session.Save();using var transitionCopy=ConstructionApplicationSession.RestoreFlight(catalog,transition,Assets,terrain.Query,terrain.Slab);
                Need(transitionCopy.Save().SequenceEqual(transition),"early terrain checkpoint exact endpoint/frontier roundtrip");
            }
        }
        Need(contacts>0,"native full-H terrain contact");
        var bytes=session.Save();using var copy=ConstructionApplicationSession.RestoreFlight(catalog,bytes,Assets,terrain.Query,terrain.Slab);
        Need(copy.Save().SequenceEqual(bytes),"terrain-grounded checkpoint roundtrip");
        long restoredHost=192;
        void ContinueRestored()
        {
            Need(copy.Engine.AdmitConstructionHostTime(copy.Authority,++restoredHost,new(15625))==ConstructionServiceStatus.AcceptedCredit,"restored terrain host credit");
            Need(copy.Engine.ServiceConstructionDebt(copy.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"restored terrain physical continuation");
        }
        for(var i=0;i<256;i++)ContinueRestored();
        var resting=Observe(copy);
        var restMotion=resting.Physical!.Motion;
        var restComVelocity=restMotion.VelocityO+restMotion.BodyToWorld.Rotate(Double3.Cross(restMotion.AngularVelocityBody,resting.ReferenceMass!.Value.Com));
        Need(resting.Physical.Consumer==AssemblyPhysicalConsumer.SurfaceContact&&Norm(restComVelocity)<.002&&Norm(restMotion.AngularVelocityBody)<.002,"cold-reconstructed terrain contacts retain physical rest");
        var minimumClearance=double.PositiveInfinity;
        foreach(var hull in compiled.Collision)foreach(var vertex in hull.Vertices)
        {
            var local=resting.Physical.Motion.PositionO+resting.Physical.Motion.BodyToWorld.Rotate(vertex);
            var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(point,region.Up);
            var coordinate=new PhysicalPatchCoordinate(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up);
            minimumClearance=Math.Min(minimumClearance,Norm(point)-Norm(collision.CollisionPoint(frame,coordinate)));
        }
        Need(minimumClearance>=-.002&&minimumClearance<=.002&&copy.Engine.ObserveConstructionPhysicalHistory(copy.Authority).Records[^1].Contacts>0,"restored full-H vertex clearance and actual contacts exclude hover or new gross penetration");
        var resourceBefore=resting.Fuel.Save();
        Need(copy.Engine.AdmitAssemblyControl(copy.Control!,copy.Control!.Identity,1,new(false,new(0,0,1))).Status==AssemblyControlStatus.Admitted,"restored terrain RCS command");
        for(var i=0;i<4;i++)ContinueRestored();
        Need(!Observe(copy).Fuel.Save().SequenceEqual(resourceBefore),"restored terrain RCS consumes exact fuel");
        Need(copy.Engine.AdmitAssemblyControl(copy.Control!,copy.Control!.Identity,2,new(false)).Status==AssemblyControlStatus.Admitted,"restored terrain RCS release");
        for(var i=0;i<64;i++)ContinueRestored();
        Need(copy.Engine.AdmitAssemblyControl(copy.Control!,copy.Control!.Identity,3,new(true)).Status==AssemblyControlStatus.Admitted,"restored terrain ignition");
        for(var i=0;i<128;i++)ContinueRestored();
        var airborne=Observe(copy);
        var nativeContacts=copy.Engine.ObserveConstructionPhysicalHistory(copy.Authority).Records[^1].Contacts;
        var observationBefore=copy.Save();
        Need(copy.Engine.ObserveConstructionContactPoints(copy.Authority,out var observedContacts)==ConstructionServiceStatus.Ready&&observedContacts==nativeContacts&&observedContacts==0,"airborne native integration does not report physical surface contact");
        Need(copy.Engine.ObserveConstructionContactPoints(session.Authority,out var foreignContacts)==ConstructionServiceStatus.InvalidAuthority&&foreignContacts==0,"foreign contact observer refuses");
        Need(copy.Save().SequenceEqual(observationBefore),"contact observation and refusal preserve canonical source");
        Need(airborne.Physical!.Motion.PositionO.Y>resting.Physical.Motion.PositionO.Y+.1&&airborne.Physical.Motion.VelocityO.Y>0&&nativeContacts==0,"restored terrain thrust physically lifts the craft clear of contact");
        copy.Binding.Physical!.Clearance.Clears(airborne.Physical.Motion,airborne.ReferenceMass!.Value,airborne.Epoch,15625,out var departureClearance);
        var poweredTicks=128;
        // Outside the inner graded rectangle, the unchanged conservative
        // free-flight certificate also encloses the whole slab's radial maximum.
        // Separation is physical before that certificate can retire native work.
        while(Observe(copy).Physical!.Consumer!=AssemblyPhysicalConsumer.FreeFlight&&poweredTicks<512){ContinueRestored();poweredTicks++;}
        Need(Observe(copy).Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight,"restored terrain eventually proves the existing free-flight handoff");
        Console.WriteLine($"TERRAIN_RELAUNCH clearanceAt128={departureClearance:R} contactsAt128={nativeContacts} riseAt128={airborne.Physical.Motion.PositionO.Y-resting.Physical.Motion.PositionO.Y:R} handoffTick={poweredTicks}");
        var directory=Path.Combine(GraphicsTestHarness.RepositoryPath(),"build","surface-recontact","live-inputs");Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory,"terrain-descending.ncflight.json"),AssemblyJson.Write(saved));
        File.WriteAllBytes(Path.Combine(directory,"terrain-grounded.ncflight.json"),bytes);
        Console.WriteLine($"SURFACE_TERRAIN_PASS checks={checks} contacts={contacts} elapsedMs={timer.Elapsed.TotalMilliseconds:R} endpoint={Observe(session).Physical}");
    }
}
