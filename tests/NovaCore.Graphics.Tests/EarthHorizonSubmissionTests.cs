using System.Numerics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

// Offline boundary witness. No native renderer/device entry point is called.
// GPU arithmetic below is a scalar FP32 source-level model, not GPU execution.
// Canonical preparation uses the production CPU physical authority. The report
// explicitly retains that limitation instead of asserting bitwise GPU parity.
internal static class EarthHorizonSubmissionTests
{
    static readonly JsonSerializerOptions Json = new() { IncludeFields=true, NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals };
    record Edge(float WA,float WB,float MidW,float Distance,float Length,float Pixels,float Alignment,float Compensation,float Fade,float Raw,float Factor,float Corrected);
    record Context(string Name,Double3 Camera,Double3 Forward,DoubleQuaternion Orientation,Vector4 BodyRotation,
        Float4x4 Matrix,Vector4[] Planes,float HalfAngle,float Height,float TanY,double Altitude);
    static void Require(bool b,string m){if(!b)throw new InvalidOperationException(m);}
    static Vector3 F(Double3 p)=>new((float)p.X,(float)p.Y,(float)p.Z);
    static float Dot(Vector3 a,Vector3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    static float Dot(Vector4 a,Vector4 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z+a.W*b.W;
    static float Length(Vector3 p)=>MathF.Sqrt(Dot(p,p));
    static Vector3 Rotate(Vector3 p,Vector4 q)=>p+2*Vector3.Cross(new(q.X,q.Y,q.Z),Vector3.Cross(new(q.X,q.Y,q.Z),p)+q.W*p);
    static Vector4 Transform(Float4x4 m,Vector3 p)=>new(
        m.C0R0*p.X+m.C1R0*p.Y+m.C2R0*p.Z+m.C3R0,
        m.C0R1*p.X+m.C1R1*p.Y+m.C2R1*p.Z+m.C3R1,
        m.C0R2*p.X+m.C1R2*p.Y+m.C2R2*p.Z+m.C3R2,
        m.C0R3*p.X+m.C1R3*p.Y+m.C2R3*p.Z+m.C3R3);
    static Vector4[] Planes(Float4x4 m)
    {
        Vector4 r0=new(m.C0R0,m.C1R0,m.C2R0,m.C3R0),r1=new(m.C0R1,m.C1R1,m.C2R1,m.C3R1),
            r2=new(m.C0R2,m.C1R2,m.C2R2,m.C3R2),r3=new(m.C0R3,m.C1R3,m.C2R3,m.C3R3);
        return [r3+r0,r3-r0,r3+r1,r3-r1,r2,r3-r2];
    }
    static Edge Factor(Vector3 p0,Vector3 p1,Vector4 a,Vector4 b,Context c)
    {
        var mid=(p0+p1)*.5f;var edge=p1-p0;float d=Length(mid),len=Length(edge);
        float px=Length(new(c.Matrix.C0R0,c.Matrix.C1R0,c.Matrix.C2R0)),py=Length(new(c.Matrix.C0R1,c.Matrix.C1R1,c.Matrix.C2R1));
        float sx=(a.X/a.W-b.X/b.W)*(c.Height*py/px),sy=(a.Y/a.W-b.Y/b.W)*c.Height;
        float pixels=.5f*MathF.Sqrt(sx*sx+sy*sy),alignment=MathF.Abs(Dot(mid/d,edge/len)),skew=(alignment-.8f)/.2f;
        float mw=MathF.Abs((a.W+b.W)*.5f),compensation=0;
        if(skew>0){compensation=.5f*1.41421356237f*c.Height*(.6f*len)/(mw*c.TanY);pixels=pixels*(1-skew)+compensation*skew;}
        float fade=1-Math.Clamp(d/50,0,1),raw=pixels/3*fade;
        return new(a.W,b.W,mw,d,len,pixels,alignment,compensation,fade,raw,Math.Clamp(raw,1,64),Corrected(a,b,d,len,alignment,c));
    }
    static float Corrected(Vector4 a,Vector4 b,float d,float len,float alignment,Context c)
    {
        float fade=1-Math.Clamp(d/50,0,1);if(fade==0||len==0)return 1;
        if(a.W==0||b.W==0)return 64;
        float px=Length(new(c.Matrix.C0R0,c.Matrix.C1R0,c.Matrix.C2R0)),py=Length(new(c.Matrix.C0R1,c.Matrix.C1R1,c.Matrix.C2R1));
        float dx=(a.X/a.W-b.X/b.W)*(c.Height*py/px),dy=(a.Y/a.W-b.Y/b.W)*c.Height;
        float pixels=.5f*MathF.Sqrt(dx*dx+dy*dy),skew=(alignment-.8f)/.2f;
        if(skew>0){float mw=MathF.Abs((a.W+b.W)*.5f);if(mw==0)return 64;
            float compensation=.5f*1.41421356237f*c.Height*(.6f*len)/(mw*c.TanY);
            pixels=skew>=1?compensation:pixels*(1-skew)+compensation*skew;}
        return !float.IsFinite(pixels)?64:Math.Clamp(pixels/3*fade,1,64);
    }
    static void WriteProbe(StreamWriter writer,Vector4 a,Vector4 b,Edge edge,Context c)
    {
        float px=Length(new(c.Matrix.C0R0,c.Matrix.C1R0,c.Matrix.C2R0)),py=Length(new(c.Matrix.C0R1,c.Matrix.C1R1,c.Matrix.C2R1));
        float[] row=[a.X,a.Y,a.W,b.X,b.Y,b.W,edge.Distance,edge.Length,float.IsFinite(edge.Alignment)?edge.Alignment:0,c.Height*py/px,c.Height,c.TanY,edge.Corrected];
        writer.WriteLine(string.Join(' ',row.Select(x=>x.ToString("R",CultureInfo.InvariantCulture))));
    }
    static double Segment(Double3 p,Double3 a,Double3 b)
    {var e=b-a;double l=e.LengthSquared,t=l>1e-24?Math.Clamp(Double3.Dot(p-a,e)/l,0,1):0;return(p-(a+e*t)).LengthSquared;}
    static double TriangleDistance(Double3 p,Double3 a,Double3 b,Double3 c)
    {
        var ab=b-a;var ac=c-a;var n=Double3.Cross(ab,ac);double ns=n.LengthSquared;
        if(ns>1e-24){var projected=p-n*(Double3.Dot(p-a,n)/ns);double e0=Double3.Dot(Double3.Cross(ab,projected-a),n),e1=Double3.Dot(Double3.Cross(c-b,projected-b),n),e2=Double3.Dot(Double3.Cross(a-c,projected-c),n);
            if((e0>=0&&e1>=0&&e2>=0)||(e0<=0&&e1<=0&&e2<=0))return(p-projected).LengthSquared;}
        return Math.Min(Segment(p,a,b),Math.Min(Segment(p,b,c),Segment(p,c,a)));
    }
    static string Cull(Context c,Double3 a,Double3 b,Double3 d,PlanetaryProductionCullContract contract,out double margin)
    {
        // Same order and precision boundaries as production_nested_scale_mesh_cull.comp.
        var center=(a+b+d)/3;double br=Math.Sqrt(Math.Max((a-center).LengthSquared,Math.Max((b-center).LengthSquared,(d-center).LengthSquared)));
        double envelope=(float)contract.MaximumTesDisplacementMetres,support=(float)contract.PlanetOcclusionSupportRadiusMetres;
        margin=0;
        if(c.Camera.LengthSquared>support*support&&PlanetaryProductionSphericalBillboardCulling.IsOccludedByPlanet(c.Camera,new(center,br+envelope+.02),support))return "GPU-horizon-reject";
        double tes=TriangleDistance(c.Camera,a,b,d)<=2500.000001?envelope:0,radius=br+tes+.02;
        var delta=center-c.Camera;double distance=Math.Sqrt(delta.LengthSquared);
        if(distance>radius&&distance>1e-9){float angle=c.HalfAngle+MathF.Asin(Math.Clamp((float)(radius/distance),0,1))+1e-4f;
            if(angle<3.1415927f&&Double3.Dot(delta/distance,new((float)c.Forward.X,(float)c.Forward.Y,(float)c.Forward.Z))<MathF.Cos(angle))return "GPU-cone-reject";}
        var q0=new Vector4(Rotate(F(a-c.Camera),c.BodyRotation),1);var q1=new Vector4(Rotate(F(b-c.Camera),c.BodyRotation),1);var q2=new Vector4(Rotate(F(d-c.Camera),c.BodyRotation),1);
        var qc=new Vector4(Rotate(F(center-c.Camera),c.BodyRotation),1);bool intersects=false;
        for(int i=0;i<6;i++){var plane=c.Planes[i];float n=Length(new(plane.X,plane.Y,plane.Z)),cd=Dot(plane,qc),s=(float)radius*n;
            if(cd < -s){margin=-s-cd;return "GPU-frustum-sphere-reject-"+i;}intersects|=cd<s;}
        if(intersects)for(int i=0;i<6;i++){var plane=c.Planes[i];float s=(float)(tes+.02)*Length(new(plane.X,plane.Y,plane.Z));float maximum=Math.Max(Dot(plane,q0),Math.Max(Dot(plane,q1),Dot(plane,q2)));
            if(maximum < -s){margin=-s-maximum;return "GPU-frustum-triangle-reject-"+i;}}
        return "indirect-draw-included";
    }
    static DoubleQuaternion Look(Double3 forward,Double3 up)
    {
        // Construct a valid root camera pose; the production builder makes the GPU matrix.
        var z=-forward.Normalized();var x=Double3.Cross(up,z).Normalized();var y=Double3.Cross(z,x);
        double tr=x.X+y.Y+z.Z,s;
        if(tr>0){s=Math.Sqrt(tr+1)*2;return new DoubleQuaternion((y.Z-z.Y)/s,(z.X-x.Z)/s,(x.Y-y.X)/s,.25*s).Normalized();}
        if(x.X>y.Y&&x.X>z.Z){s=Math.Sqrt(1+x.X-y.Y-z.Z)*2;return new DoubleQuaternion(.25*s,(y.X+x.Y)/s,(z.X+x.Z)/s,(y.Z-z.Y)/s).Normalized();}
        if(y.Y>z.Z){s=Math.Sqrt(1+y.Y-x.X-z.Z)*2;return new DoubleQuaternion((y.X+x.Y)/s,.25*s,(z.Y+y.Z)/s,(z.X-x.Z)/s).Normalized();}
        s=Math.Sqrt(1+z.Z-x.X-y.Y)*2;return new DoubleQuaternion((z.X+x.Z)/s,(z.Y+y.Z)/s,.25*s,(x.Y-y.X)/s).Normalized();
    }
    public static void Run()
    {
        string root=GraphicsTestHarness.RepositoryPath(),output=Environment.GetEnvironmentVariable("NOVACORE_HORIZON_REPORT")??Path.Combine(root,"build","earth-blackout-closure","horizon-boundary");Directory.CreateDirectory(output);
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var local,out var error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(local,out error),error);
        var frame=new ReferenceFrameId(1);Require(SolarSystemScene.TryCreateAt(frame,SimulationInstant.Zero,out var created,out error),error);var scene=created!;
        var camera=new CameraState(new FramePosition(frame,Double3.Zero),DoubleQuaternion.Identity,scene.Projection.WithAspect(3440d/1440),CameraMode.Free);Require(scene.Focus(camera,NativePresentationFocus.Earth),"focus");
        var resolver=new ReferenceFrameResolver(new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(frame,null,ReferenceFrameKind.Ecl,"root"),CelestialFrameFactory.RootEcl())]));
        var body=scene.FocusedBody;var inverse=body.BodyFixedToRoot.Conjugate().Normalized();
        var (levels,contracts)=PlanetaryNestedScaleMeshRuntimeAdapter.Adapt(PlanetaryNestedScaleMeshTopologyLibrary.Load(Path.Combine(root,"assets","planetary-nested-scale-mesh")));
        var sourceNames=new[]{"production_spherical_billboard.vert","production_spherical_billboard.tesc","production_tessellation_factor.glsl","production_nested_scale_mesh_cull.comp","production_nested_scale_mesh_incoming_cull.comp","production_spherical_billboard_prepare.comp","production_spherical_billboard_incoming_prepare.comp","production_spherical_billboard_compact.comp"};
        var hashes=sourceNames.Select(name=>new{name,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"native","NovaCore.Native","shaders",name))))}).ToArray();
        // A source-level culling/VS model must fail closed when its source changes.
        // Re-audit these mirrors rather than silently accepting a stale proof.
        // 2026-09-30 audit: both direct-prepared and TES vertex variants share
        // FP64 body-camera subtraction, FP32 rotation and the same clip transform.
        // New direct varyings do not change that model. Factor coverage below
        // exercises the retained TES route, not ordinary direct raster execution.
        // See docs/engineering-evidence/prebank-convergence/contracts.md.
        var audited=new Dictionary<string,string>{
            ["production_spherical_billboard.vert"]="7F590AC482564ABA0A5706F894BFBECB12DBFDBE148D09DC14DB7530B7483307",
            ["production_spherical_billboard.tesc"]="CC3412546B6815CA264B539AE97D38CAA7146CB39BA04220EDFC0E23BE85C9AA",
            ["production_tessellation_factor.glsl"]="93202E80EC0DB1FD5F20DB6FD970A195AB055174F659E844DB2173D8E2A6AAC7",
            ["production_nested_scale_mesh_cull.comp"]="611C3670B5800CC3AC352BED5D326EB59EF1ABBA6BA2CC5519337967703B5550",
            ["production_nested_scale_mesh_incoming_cull.comp"]="7DC18DFB540A3BE29BC90671F49675FCC5A6D6D13B6BA05282A6BA900B3F1BD4"};
        foreach(var hash in hashes)if(audited.TryGetValue(hash.name,out var expected))Require(hash.sha256==expected,"Re-audit offline mirror for "+hash.name);
        File.WriteAllText(Path.Combine(output,"sources.json"),JsonSerializer.Serialize(hashes,Json));
        using var anomalies=new StreamWriter(Path.Combine(output,"anomalies.jsonl"));using var cases=new StreamWriter(Path.Combine(output,"cases.jsonl"));using var probes=new StreamWriter(Path.Combine(output,"shader-probes.txt"));
        var pinned=new Double3(2494046.3896166086,5845295.425018996,-449593.7759662047);
        bool full=Environment.GetEnvironmentVariable("NOVACORE_HORIZON_FULL")=="1";
        var sites=full?new[]{("pinned",pinned.Normalized()),("equator-X",Double3.UnitX),("equator-Z",Double3.UnitZ),("north-pole",Double3.UnitY)}:new[]{("pinned",pinned.Normalized()),("equator-Z",Double3.UnitZ)};
        int[] requestedLevels=full?Enumerable.Range(0,18).ToArray():[0,13,14,17];
        double[] altitudes=full?[10.001,49.999,50,50.001,80,1000,1e6]:[10.001];
        double[] angles=full?[-1e-3,-1e-5,-1e-7,0,1e-7,1e-5,1e-3]:[-1e-7,0,1e-7];
        long invalidIncluded=0,invalidRejected=0,total=0,oldInvalid=0,legacyIncluded=0;int caseCount=0;
        foreach(var (site,direction) in sites)foreach(int level in requestedLevels)
        {
            var topology=levels[level];var contract=contracts[topology.TopologyHash];var pupil=PlanetaryProductionBillboardPupil.Resolve(default,direction,topology);
            var points=new Double3[topology.Vertices.Count];
            for(int i=0;i<points.Length;i++){var dir=pupil.ResolveCanonicalDirection(topology.Vertices[i],topology);points[i]=dir*(body.RadiusMetres+PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,dir));Require(points[i].IsFinite,"prepared point finite");}
            foreach(double altitude in altitudes)
            {
                var selected=new PlanetaryProductionSphericalBillboardSelector(levels).Evaluate(new(altitude,direction,3440,1440,Math.PI/3,0),false).Level;
                if(altitude!=10.001&&level!=selected&&level!=0)continue;
                double h=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,direction);
                var requestedEye=direction*(body.RadiusMetres+h+altitude);
                var orientations=angles.Select(a=>(pitch:a,azimuth:0d)).ToList();
                if(full&&altitude==10.001&&level==selected)
                    foreach(double azimuth in new[]{Math.PI/4,Math.PI/2,Math.PI})foreach(double pitch in new[]{-1e-7,0,1e-7})orientations.Add((pitch,azimuth));
                foreach(var (angle,azimuth) in orientations)
                {
                    var tangent=Double3.Cross(direction,Math.Abs(direction.X)<.9?Double3.UnitX:Double3.UnitY).Normalized();
                    tangent=DoubleQuaternion.FromAxisAngle(direction,azimuth).Rotate(tangent);
                    var forward=(tangent*Math.Cos(angle)+direction*Math.Sin(angle)).Normalized();
                    camera.Position=new(frame,body.Position.Value+body.BodyFixedToRoot.Rotate(requestedEye));camera.Orientation=Look(body.BodyFixedToRoot.Rotate(forward),body.BodyFixedToRoot.Rotate(direction));
                    scene.EnforceFinalCameraInvariant(camera);
                    camera.Projection=scene.Projection.WithAspect(3440d/1440);
                    var gpu=scene.GpuConstants(camera,1440);var presentation=scene.FocusedPresentation(camera);
                    var eye=new Double3((double)gpu.CameraBodyHighX+gpu.CameraBodyLowX,(double)gpu.CameraBodyHighY+gpu.CameraBodyLowY,(double)gpu.CameraBodyHighZ+gpu.CameraBodyLowZ);
                    Require(CameraRenderSnapshotBuilder.TryBuildReversedInfiniteFar(camera,resolver,frame,out var built,out _,out _),"production camera builder");
                    var ctx=new Context($"{site}/L{level}/alt{altitude:R}/pitch{angle:R}/azimuth{azimuth:R}",eye,new(gpu.ViewForwardX,gpu.ViewForwardY,gpu.ViewForwardZ),camera.Orientation,
                        new(presentation.BodyOrientationX,presentation.BodyOrientationY,presentation.BodyOrientationZ,presentation.BodyOrientationW),built.ViewProjection,Planes(built.ViewProjection),gpu.ViewHalfAngleRadians,1440,gpu.VerticalTanHalfFov,gpu.SurfaceAltitudeMetres);
                    var projected=new Vector4[points.Length];var relative=new Vector3[points.Length];
                    for(int i=0;i<points.Length;i++){relative[i]=Rotate(F(points[i]-eye),ctx.BodyRotation);projected[i]=Transform(ctx.Matrix,relative[i]);}
                    var right=Double3.Cross(forward,direction).Normalized();var up=Double3.Cross(right,forward).Normalized();
                    Double3 View(Double3 p){var r=p-eye;return new(Double3.Dot(r,right),Double3.Dot(r,up),-Double3.Dot(r,forward));}
                    int bad=0,rejected=0,old=0,nearPlane=0,legacyDraw=0;float max=1;
                    for(int t=0;t<topology.Indices.Count;t+=3)
                    {
                        int ia=(int)topology.Indices[t],ib=(int)topology.Indices[t+1],ic=(int)topology.Indices[t+2];
                        var a=points[ia];var b=points[ib];var c=points[ic];
                        var e0=Factor(relative[ia],relative[ib],projected[ia],projected[ib],ctx);var e1=Factor(relative[ib],relative[ic],projected[ib],projected[ic],ctx);var e2=Factor(relative[ic],relative[ia],projected[ic],projected[ia],ctx);
                        bool finite=float.IsFinite(e0.Corrected)&&float.IsFinite(e1.Corrected)&&float.IsFinite(e2.Corrected);
                        bool legacyFinite=float.IsFinite(e0.Factor)&&float.IsFinite(e1.Factor)&&float.IsFinite(e2.Factor);
                        double old0=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(View(a),View(b),1440,Math.PI/3,50),old1=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(View(b),View(c),1440,Math.PI/3,50),old2=PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(View(c),View(a),1440,Math.PI/3,50);
                        bool oldFinite=double.IsFinite(old0)&&double.IsFinite(old1)&&double.IsFinite(old2);
                        // Near-plane numeric probes are retained even when the resulting factor is finite.
                        bool pathological=Math.Min(Math.Abs(projected[ia].W),Math.Min(Math.Abs(projected[ib].W),Math.Abs(projected[ic].W)))<=1e-5f;
                        total++;if(finite)max=Math.Max(max,Math.Max(e0.Corrected,Math.Max(e1.Corrected,e2.Corrected)));
                        if(finite&&legacyFinite&&oldFinite&&!pathological)continue;
                        var disposition=Cull(ctx,a,b,c,contract,out var margin);bool included=disposition=="indirect-draw-included";
                        if(!oldFinite){old++;oldInvalid++;}if(pathological)nearPlane++;
                        if(!finite){if(included){bad++;invalidIncluded++;}else{rejected++;invalidRejected++;}}
                        if(!legacyFinite&&included){
                            foreach(var point in new[]{a,b,c}){
                                var physical=PlanetaryTerrainDefinition.EarthProductionCubeV5.SamplePhysicalSurface(point.Normalized(),PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                                Require(physical.IsFinite&&physical.PhysicalNormal.LengthSquared>0,"reachable witness has valid canonical prepared normal");
                            }
                            legacyDraw++;legacyIncluded++;
                        }
                        WriteProbe(probes,projected[ia],projected[ib],e0,ctx);WriteProbe(probes,projected[ib],projected[ic],e1,ctx);WriteProbe(probes,projected[ic],projected[ia],e2,ctx);
                        anomalies.WriteLine(JsonSerializer.Serialize(new{ctx.Name,level,topology.TopologyHash,triangle=t/3,indices=new[]{ia,ib,ic},physical=new[]{a,b,c},rootRelative=new[]{relative[ia],relative[ib],relative[ic]},clip=new[]{projected[ia],projected[ib],projected[ic]},edges=new[]{e0,e1,e2},finite,legacyFinite,cpuReferenceFinite=oldFinite,cpuReferenceFactors=new[]{old0,old1,old2},pathological,
                            cpuCulling="none-per-triangle; selected generation admitted",preparationIncluded=true,gpuComputeSubmissionIncluded=true,gpuCullDisposition=disposition,margin,indirectDrawIncluded=included,
                            canonicalNormalsChecked=!legacyFinite&&included,
                            preCorrectionClassification=legacyFinite?"2-valid-after-production-transformation":included?"3-production-reachable-invalid-in-source-model":"4-unreachable-at-TCS-in-source-model; GPU-compute-submitted",
                            postCorrectionClassification=finite?"2-valid-after-production-transformation":included?"3-production-reachable-invalid-in-source-model":"4-unreachable-at-TCS-in-source-model; GPU-compute-submitted",
                            limitation="source-level CPU execution, not a captured GPU physical buffer or GPU execution"},Json));
                    }
                    var record=new{ctx,level,topology.TopologyHash,pupil,triangles=topology.TriangleCount,bad,rejected,legacyDraw,old,nearPlane,max,selectionRole="settled or retained generation during adjacent transition; not all camera/LOD pairs selected initially"};
                    cases.WriteLine(JsonSerializer.Serialize(record,Json));cases.Flush();anomalies.Flush();probes.Flush();caseCount++;Console.WriteLine($"Horizon {ctx.Name}: invalid draw={bad}, legacy draw={legacyDraw}, culled={rejected}, old={old}, nearPlane={nearPlane}");
                }
            }
        }
        PublicationWitness(root,levels,contracts,pinned.Normalized(),output);
        AuditOriginalPinned(scene,camera,resolver,levels[17],contracts[levels[17].TopologyHash],pinned,output);
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{gpuExposure=false,livePreflightClean=false,caseCount,total,invalidIncluded,invalidRejected,legacyIncluded,oldInvalid,scope="common direct/TES vertex transform and culling; retained TES factor route; production topology/pupil/CPU height authority; scalar FP32 source model, not ordinary direct-raster TCS execution"},Json));
        Require(legacyIncluded>0,"boundary regression must retain a pre-correction production-reachable witness");
        Require(invalidIncluded==0,"Source-model production draw includes invalid tessellation; preserve witness and investigate owner before GPU exposure");
    }
    static double LegacyDouble(Double3 a,Double3 b)
    {
        var mid=(a+b)*.5;double distance=Math.Sqrt(mid.LengthSquared),focal=1440/(2*Math.Tan(Math.PI/6));
        double ax=focal*a.X/-a.Z,ay=focal*a.Y/-a.Z,bx=focal*b.X/-b.Z,by=focal*b.Y/-b.Z;
        double screen=Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay));var edge=b-a;double len=Math.Sqrt(edge.LengthSquared);
        var direction=mid.LengthSquared>0?mid.Normalized():Double3.UnitZ;
        double alignment=len>0?Math.Abs(Double3.Dot(direction,edge/len)):0;
        if(alignment>.8){double skew=(alignment-.8)/.2,comp=Math.Sqrt(2)*focal*(.6*len)/Math.Abs(-(a.Z+b.Z)*.5);screen=screen+(comp-screen)*skew;}
        return Math.Clamp(screen*(1-Math.Clamp(distance/50,0,1))/3,1,64);
    }
    static void AuditOriginalPinned(SolarSystemScene scene,CameraState camera,ReferenceFrameResolver resolver,
        PlanetaryProductionSphericalBillboardTopology topology,PlanetaryProductionCullContract contract,Double3 eye,string output)
    {
        // This is the exact retained prior CPU input, not a second root/body
        // position round trip. That extra round trip changes its zero by microns.
        var encoded=EncodedPosition.Encode(eye);
        Require(new Double3((double)encoded.HighX+encoded.LowX,(double)encoded.HighY+encoded.LowY,(double)encoded.HighZ+encoded.LowZ)==eye,"pinned eye is a representable production high/low transport");
        var forward=new Double3(0,-.07668898120047421,-.9970550637564775);
        var right=new Double3(.9201919621364972,-.3903145885585041,.030021238778387863);
        var up=new Double3(.3914674351965733,.9174820554762033,-.07056858408511331);
        var body=scene.FocusedBody;
        camera.Orientation=Look(body.BodyFixedToRoot.Rotate(forward),body.BodyFixedToRoot.Rotate(up));
        camera.Projection=new(Math.PI/3,3440d/1440,PlanetarySurfaceCameraPolicy.NearClipMetres(10.00100040435791),camera.Projection.FarClip);
        Require(CameraRenderSnapshotBuilder.TryBuildReversedInfiniteFar(camera,resolver,camera.Position.Frame,out var gpu,out _,out _),"pinned production matrix");
        var presentation=scene.FocusedPresentation(camera);
        var ctx=new Context("original-pinned-exact",eye,forward,camera.Orientation,new(presentation.BodyOrientationX,presentation.BodyOrientationY,presentation.BodyOrientationZ,presentation.BodyOrientationW),gpu.ViewProjection,Planes(gpu.ViewProjection),
            (float)Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(Math.PI/6),2)*(1+Math.Pow(3440d/1440,2)))),1440,(float)Math.Tan(Math.PI/6),10.00100040435791);
        var pupil=PlanetaryProductionBillboardPupil.Resolve(default,eye.Normalized(),topology);var points=new Double3[topology.Vertices.Count];
        for(int i=0;i<points.Length;i++){var dir=pupil.ResolveCanonicalDirection(topology.Vertices[i],topology);points[i]=dir*(PlanetaryProductionSphericalBillboardTopologyGenerator.EarthRadiusMetres+PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,dir));}
        Double3 View(Double3 p){var r=p-eye;return new(Double3.Dot(r,right),Double3.Dot(r,up),-Double3.Dot(r,forward));}
        int count=0,validTransform=0,rejected=0,reachable=0;var records=new List<object>();
        for(int t=0;t<topology.Indices.Count;t+=3)
        {
            int ia=(int)topology.Indices[t],ib=(int)topology.Indices[t+1],ic=(int)topology.Indices[t+2];var a=points[ia];var b=points[ib];var c=points[ic];
            double fa=LegacyDouble(View(a),View(b)),fb=LegacyDouble(View(b),View(c)),fc=LegacyDouble(View(c),View(a));
            if(double.IsFinite(fa)&&double.IsFinite(fb)&&double.IsFinite(fc))continue;
            count++;var qa=Rotate(F(a-eye),ctx.BodyRotation);var qb=Rotate(F(b-eye),ctx.BodyRotation);var qc=Rotate(F(c-eye),ctx.BodyRotation);
            var ca=Transform(ctx.Matrix,qa);var cb=Transform(ctx.Matrix,qb);var cc=Transform(ctx.Matrix,qc);
            var e0=Factor(qa,qb,ca,cb,ctx);var e1=Factor(qb,qc,cb,cc,ctx);var e2=Factor(qc,qa,cc,ca,ctx);
            bool finite=float.IsFinite(e0.Factor)&&float.IsFinite(e1.Factor)&&float.IsFinite(e2.Factor);
            string cull=Cull(ctx,a,b,c,contract,out var margin);bool included=cull=="indirect-draw-included";
            int category=finite?2:included?3:4;if(category==2)validTransform++;else if(category==3)reachable++;else rejected++;
            Require(float.IsFinite(e0.Corrected)&&float.IsFinite(e1.Corrected)&&float.IsFinite(e2.Corrected),"pinned corrected factors finite");
            records.Add(new{triangle=t/3,indices=new[]{ia,ib,ic},physical=new[]{a,b,c},legacyCpuFactors=new[]{fa,fb,fc},clip=new[]{ca,cb,cc},edges=new[]{e0,e1,e2},finite,cpuCulling="none",preparationIncluded=true,gpuComputeSubmissionIncluded=true,cull,margin,indirectDrawIncluded=included,preCorrectionCategory=category,postCorrectionCategory=2});
        }
        File.WriteAllText(Path.Combine(output,"original-pinned-audit.json"),JsonSerializer.Serialize(new{ctx,topology.Level,topology.TopologyHash,pupil,count,validTransform,rejected,reachable,records,scope="exact prior CPU input; production native transport and shader source model; not hardware execution"},Json));
        Require(count==477,"original pinned 477-case singularity fixture changed; re-audit rather than silently replacing it");
        Console.WriteLine($"Original pinned: {count}; pre-fix transformed finite={validTransform}, GPU rejected={rejected}, source-model draw invalid={reachable}; corrected all finite");
    }
    static void PublicationWitness(string root,IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels,IReadOnlyDictionary<ulong,PlanetaryProductionCullContract> contracts,Double3 direction,string output)
    {
        var runtime=new PlanetaryProductionSphericalBillboardMovingRuntime(root,levels,contracts);uint ready=0;ulong frame=0;var seen=new HashSet<int>();var records=new List<object>();
        for(int i=0;i<3000;i++)
        {
            double altitude=runtime.Current is null&&seen.Count==0?1e8:10.001;
            var telemetry=runtime.Update(new(altitude,direction,3440,1440,Math.PI/3,frame++),ready);
            if(runtime.Current is {} current){seen.Add(current.Topology.Level);if(current.Topology.Level==17)break;}
            if(runtime.TrySubmitPrepared(out var generation)){
                records.Add(new{frame,altitude,currentLevel=runtime.Current?.Topology.Level,incomingLevel=generation.Topology.Level,generation.PublicationGeneration,generation.PupilFrameIdentity,generation.NativeGpuPhysicalPreparation,
                    indices=generation.Indices.Length,generation.Pupil,acknowledgment="synthetic CPU acknowledgement only"});
                ready=(uint)generation.PublicationGeneration;
            }
            else if(runtime.ReplacementInFlight)Thread.Sleep(1);
        }
        File.WriteAllText(Path.Combine(output,"publication-witness.json"),JsonSerializer.Serialize(records,Json));
        Require(Enumerable.Range(0,18).All(seen.Contains),"actual coordinator must retain and publish every adjacent level during near-ground approach");
    }
}
