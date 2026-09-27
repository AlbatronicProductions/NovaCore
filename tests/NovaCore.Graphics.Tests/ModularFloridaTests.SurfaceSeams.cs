using System.Collections.Immutable;
using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceSeams()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var source=(IPhysicalSurfaceCollisionSource)terrain.Query;var region=FloridaFacilitySupport.Region;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        foreach(var kind in new[]{"short","long","asymmetric"})foreach(var sign in new[]{1,-1})
        {
            var data=Craft(catalog,kind=="long").Data;
            if(kind=="asymmetric")
            {
                var retained=data.Instances.Take(4).Concat(new[]{0,1,2,4,5,6}.Select(i=>data.Instances[4+i])).Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);
                var parts=data.Instances.Where(p=>retained.Contains(p.Id)).Select((p,i)=>p with{Order=i}).ToImmutableArray();
                data=data with{Instances=parts,Connections=data.Connections.Where(e=>retained.Contains(e.Child)).ToImmutableArray(),Configuration=data.Configuration.Where(c=>retained.Contains(c.Part)).ToImmutableArray(),Symmetry=[],DependencyDigest=catalog.DependencyDigest(parts)};
            }
            var craft=CraftCompiler.Compile(catalog,data,Assets);
            using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);var site=cold.Binding.Physical!.Site;
            var q=DoubleQuaternion.FromAxisAngle(Double3.UnitY,kind=="asymmetric"?.37:0)*AssemblyContactProfile.Upright;
            var vertices=craft.Collision.SelectMany(h=>h.Vertices).Select(v=>q.Rotate(v)).ToArray();
            var edge=sign>0?vertices.Max(v=>v.X):vertices.Min(v=>v.X);
            var origin=new Double3(sign*84-edge-sign*.15,0,kind=="asymmetric"?82:0);
            double Clearance(Double3 local)
            {
                var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(point,region.Up);
                var p=new PhysicalPatchCoordinate(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up);
                return Norm(point)-Norm(source.CollisionPoint(frame,p));
            }
            origin+=new Double3(0,-vertices.Min(v=>Clearance(origin+v))+.01,0);
            var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
            var saved=basis with{Physical=new(new(origin,new(sign*3,-.1,kind=="asymmetric"?sign*3:0),q,default),default,AssemblyPhysicalConsumer.FreeFlight)};
            var start=Stopwatch.GetTimestamp();using var s=ConstructionApplicationSession.RestoreFlight(catalog,AssemblyJson.Write(saved),Assets,terrain.Query,terrain.Slab);
            var preparation=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var energy=new SurfaceEnergy(s);
            var costs=new double[256];var changes=0;var contacts=0;var coveredRows=0;var deepest=0d;var lastGeneration=0;var generationCosts=new List<double>();
            for(var step=0;step<costs.Length;step++)
            {
                var priorWorld=s.Engine.ConstructionContactWorldForTest(s.Authority);
                if(priorWorld is not null)
                {
                    var rows=Field<LocalContactMetrics>(priorWorld,"metrics").Terrain!;
                    if(rows.RawForTest is null)rows.CaptureForTest();
                }
                start=Stopwatch.GetTimestamp();Need(s.Engine.AdmitConstructionHostTime(s.Authority,step+1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"seam credit");
                var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);costs[step]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if(status!=ConstructionServiceStatus.Published)
                {
                    var failed=s.Engine.ConstructionContactWorldForTest(s.Authority)!;var native=Field<BepuPhysics.Simulation>(failed,"simulation");
                    var body=native.Bodies[Field<BepuPhysics.BodyHandle>(failed,"body")];var config=Field<LocalContactConfiguration>(failed,"configuration");
                    var metric=Field<LocalContactMetrics>(failed,"metrics").Terrain!;
                    var nq=body.Pose.Orientation;var actualQ=new DoubleQuaternion(nq.X,nq.Y,nq.Z,nq.W)*AssemblyContactProfile.Upright;
                    var center=config.OriginRoot+new Double3(body.Pose.Position.X,body.Pose.Position.Y,body.Pose.Position.Z);
                    Need(s.Engine.State.Spacecraft.TryGetConstruction(s.Binding.Spacecraft.Id,out _,out var canonical),"failed seam canonical witness remains accessible");
                    var mass=canonical!.ReferenceMass!.Value;
                    var gap=craft.Collision.SelectMany(h=>h.Vertices).Min(v=>Clearance(center+actualQ.Rotate(v-mass.Com)));
                    Console.WriteLine($"SEAM_FAILURE kind={kind} sign={sign} step={step} exactHullGap={gap:R} deepest={metric.Deepest} velocity={body.Velocity.Linear} angular={body.Velocity.Angular}");
                    foreach(var row in Field<BepuPhysics.CollisionDetection.Contact[]>(metric,"rows").Take(Field<int>(metric,"count")))Console.WriteLine($"SEAM_PARENT depth={row.Depth:R} normal={row.Normal} offset={row.Offset} feature={row.FeatureId}");
                    var owner=Field<CraftTerrainColliders>(failed,"craftTerrain");ref var mesh=ref native.Shapes.GetShape<BepuPhysics.Collidables.Mesh>(Field<BepuPhysics.Collidables.TypedIndex>(owner,"shape").Index);
                    var triangle=mesh.Triangles[metric.Deepest.Triangle];Console.WriteLine($"SEAM_TRIANGLE a={triangle.A} b={triangle.B} c={triangle.C}");
                    ref var compound=ref native.Shapes.GetShape<BepuPhysics.Collidables.Compound>(Field<BepuPhysics.Collidables.TypedIndex>(failed,"bodyShape").Index);
                    var minFace=double.PositiveInfinity;var faceWorst=Double3.Zero;
                    var nativeQ=new DoubleQuaternion(nq.X,nq.Y,nq.Z,nq.W).Normalized();
                    for(var childIndex=0;childIndex<compound.Children.Length;childIndex++)
                    {
                        ref var child=ref compound.Children[childIndex];ref var hull=ref native.Shapes.GetShape<BepuPhysics.Collidables.ConvexHull>(child.ShapeIndex.Index);
                        for(var faceIndex=0;faceIndex<hull.FaceToVertexIndicesStart.Length;faceIndex++)
                        {
                            hull.GetVertexIndicesForFace(faceIndex,out var indices);var face=new Double3[indices.Length];
                            for(var j=0;j<indices.Length;j++)
                            {
                                hull.GetPoint(indices[j],out var v);var cq=child.LocalOrientation;
                                face[j]=center+nativeQ.Rotate(new Double3(child.LocalPosition.X,child.LocalPosition.Y,child.LocalPosition.Z)+new DoubleQuaternion(cq.X,cq.Y,cq.Z,cq.W).Rotate(new(v.X,v.Y,v.Z)));
                            }
                            for(var fan=1;fan<face.Length-1;fan++)for(var u=0;u<=16;u++)for(var v=0;v<=16-u;v++)
                            {
                                var local=face[0]+(face[fan]-face[0])*(u/16d)+(face[fan+1]-face[0])*(v/16d);
                                var clearance=Clearance(local);if(clearance<minFace){minFace=clearance;faceWorst=local;}
                            }
                        }
                    }
                    Console.WriteLine($"SEAM_FACE_SAMPLE clearance={minFace:R} worst={faceWorst}");
                    if(metric.RawForTest is {} raw)foreach(var row in raw.OrderByDescending(x=>x.Depth).Take(8))Console.WriteLine($"SEAM_RAW {row}");
                }
                Need(status==ConstructionServiceStatus.Published&&count==1,$"{kind}/{sign} seam successor {step}: {status}; {s.Engine.ConstructionSupportFailure(s.Authority)}");
                var world=s.Engine.ConstructionContactWorldForTest(s.Authority);
                if(world is not null)
                {
                    var mesh=Field<CraftTerrainColliders>(world,"craftTerrain");
                    if(lastGeneration!=0&&mesh.Generation!=lastGeneration){changes++;generationCosts.Add(costs[step]);}
                    lastGeneration=mesh.Generation;
                    coveredRows+=VerifyTerrainFootprint(world,site);
                }
                contacts+=s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records[^1].Contacts;
                var state=Observe(s);energy.Sample(state,step);var motion=state.Physical!.Motion;
                if(step%8==0)foreach(var v in craft.Collision.SelectMany(h=>h.Vertices))deepest=Math.Max(deepest,-Clearance(motion.PositionO+motion.BodyToWorld.Rotate(v)));
            }
            Need(changes>0&&contacts>0,"moving real contact crosses a native mesh generation boundary");
            Need(coveredRows>0,"raw contact smoothing coverage is positively exercised");
            Need(deepest<CraftSurfaceImpact.MinimumRecoverySaturationDepth,"independent full-H hull penetration stays below the earliest saturation transition");
            var sorted=costs.Order().ToArray();
            Console.WriteLine($"SURFACE_SEAM kind={kind} sign={sign} generations={changes} contacts={contacts} depth={deepest:R} prepareMs={preparation:R} medianMs={sorted[127]:R} p95Ms={sorted[243]:R} p99Ms={sorted[253]:R} maxMs={sorted[^1]:R} generationMs={string.Join(',',generationCosts)}");
            Console.WriteLine($"SEAM_ENERGY kind={kind} sign={sign} maximumGainJ={energy.MaximumGain:R} at={energy.MaximumAt} allowanceJ={energy.Allowance:R}");
            Console.WriteLine($"SEAM_COVERAGE rawRows={coveredRows}");
        }
        Console.WriteLine($"SURFACE_SEAMS_PASS checks={checks}");
    }
    private static int VerifyTerrainFootprint(LocalContactWorld world,AssemblyFloridaSite site)
    {
        var raw=Field<LocalContactMetrics>(world,"metrics").Terrain!.RawForTest;if(raw is null)return 0;
        var sim=Field<BepuPhysics.Simulation>(world,"simulation");var config=Field<LocalContactConfiguration>(world,"configuration");
        var native=sim.Bodies[Field<BepuPhysics.BodyHandle>(world,"body")];
        ref var compound=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Compound>(Field<BepuPhysics.Collidables.TypedIndex>(world,"bodyShape").Index);
        var tiles=Field<Dictionary<CraftTerrainGeometry.Tile,CraftTerrainGeometry.Prepared>>(Field<CraftTerrainColliders>(world,"craftTerrain"),"tiles");
        var region=FloridaFacilitySupport.Region;
        foreach(var row in raw)
        {
            ref var child=ref compound.Children[row.Child];BepuPhysics.Collidables.Compound.GetRotatedChildPose(child.LocalPosition,child.LocalOrientation,row.Pose.Orientation,out var pose);
            sim.Shapes[child.ShapeIndex.Type].ComputeBounds(child.ShapeIndex.Index,pose.Orientation,out var low,out var high);
            // Euclidean span conservatively encloses native max-axis span;
            // retain the existing conversion allowance around this oracle.
            var halo=((double)(high-low).Length()+2*Math.Sqrt(3)*native.Collidable.MaximumSpeculativeMargin)*1e-4+config.ContactTolerance/16;
            var point=row.Pose.Position+pose.Position+row.Offset;
            foreach(var dx in new[]{-halo,halo})foreach(var dy in new[]{-halo,halo})foreach(var dz in new[]{-halo,halo})
            {
                var local=config.OriginRoot+new Double3(point.X+dx,point.Y+dy,point.Z+dz);
                var body=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);var up=Double3.Dot(body,region.Up);
                var x=region.RadiusMetres*Double3.Dot(body,region.East)/up;var y=region.RadiusMetres*Double3.Dot(body,region.North)/up;
                Need(tiles.ContainsKey(new((int)Math.Floor(x/CraftTerrainGeometry.TileSize),(int)Math.Floor(y/CraftTerrainGeometry.TileSize))),"each accepted raw contact and conservative smoothing neighborhood remain in prepared terrain");
            }
        }
        return raw.Count;
    }
}
