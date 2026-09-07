using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using System.Diagnostics;
using System.Text.Json;

internal static class FacilitySupportTests
{
    internal static void LoadPhysicalData()
    {
        var root=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        Require(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
    }
    internal static void Run()
    {
        var root=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        Require(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        var previous=PlanetaryPhysicalSurface.RuntimeGeneration;
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        try
        {
            var region=FloridaFacilitySupport.Region;var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
            Require(region.DeterministicHash==FloridaFacilitySupport.DefinitionIdentity&&
                (region with {PlaneAltitudeMetres=region.PlaneAltitudeMetres+.01}).DeterministicHash!=region.DeterministicHash,"support edits must invalidate physical sample identity");
            Require(FloridaLaunchSite.TryCreate(6,region.RadiusMetres,terrain,out var site),"Florida site");
            Require(site.LocalPhysicalSurfaceRadiusMetres==6371030.770461536d&&site.FoundationDepthMetres==6.835569277405739d,"slab or foundation moved");
            Require(site.Object.Anchor.NormalizedBodyFixedDirection==region.Up&&site.Object.Anchor.BodyId==6,"geographic identity");
            Require(Math.Abs(site.FoundationOffsetMetres-site.FoundationDepthMetres)<1e-8,"anchor height is the fixed foundation-to-support distance");
            var data=new List<object>();double maxSlope=0,maxBoundaryJump=0,maxNormalAngle=0;
            foreach(var (e,n) in Points())
            {
                var d=Direction(e,n);var sample=region.Sample(d);
                var original=PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(terrain,d);var h=Height(d);
                if(Math.Abs(e)>=192d+1e-6||Math.Abs(n)>=184d+1e-6)Require(sample.Weight==0d&&h==original,"outside physical terrain changed");
                if(Math.Abs(e)<=32&&Math.Abs(n)<=24)Require(Math.Abs(h-sample.PlaneHeight)<1e-8,"inner canonical terrain does not meet the support plane");
                Require(sample.Weight is >=0 and <=1,"non-convex support blend");
                data.Add(new {east=e,north=n,natural=original,modifier=h-original,canonical=h,weight=sample.Weight,plane=sample.PlaneHeight});
            }
            // Dense cross-sections through both axes, including both boundary joins.
            foreach(bool north in new[]{false,true})for(double x=-230;x<230;x+=.25)
            {
                var d=Direction(north?0:x,north?x:0);var h0=Height(d);var h1=Height(Direction(north?0:x+.25,north?x+.25:0));
                var support=region.Sample(d);var natural=PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(terrain,d);
                if(support.Weight>0)Require(h0>=Math.Min(natural,support.PlaneHeight)-1e-8&&h0<=Math.Max(natural,support.PlaneHeight)+1e-8,"blend overshoots its natural/support endpoints");
                maxSlope=Math.Max(maxSlope,Math.Abs(h1-h0)/.25);
            }
            foreach(var (e,n) in new[]{(64d,0d),(192d,0d),(0d,56d),(0d,184d)})
            {
                var alongEast=e!=0;var a=Height(Direction(e-(alongEast?1e-4:0),n-(alongEast?0:1e-4)));
                var b=Height(Direction(e+(alongEast?1e-4:0),n+(alongEast?0:1e-4)));maxBoundaryJump=Math.Max(maxBoundaryJump,Math.Abs(a-b));
            }
            Require(maxBoundaryJump<.001,"support transition has a height discontinuity");
            Require(maxSlope<1,"unexpected support cross-section inversion or cliff");
            foreach(var (e,n) in new[]{(0d,0d),(32d,24d),(-32d,-24d)})
            {
                var normal=terrain.SamplePhysicalSurface(Direction(e,n)).PhysicalNormal;
                maxNormalAngle=Math.Max(maxNormalAngle,(normal-region.Up).LengthSquared);
            }
            Require(maxNormalAngle<1e-18,"inner support normal is not the authored plane normal");
            var sw=Stopwatch.StartNew();double sum=0;for(int i=0;i<10000;i++)sum+=Height(region.Up);sw.Stop();
            Directory.CreateDirectory(Path.Combine(root,"build","facility-support"));
            File.WriteAllText(Path.Combine(root,"build","facility-support","cpu-support.json"),JsonSerializer.Serialize(new {data,maxSlope,maxBoundaryJump,maxNormalAngle,queryNs=sw.Elapsed.TotalNanoseconds/10000,sum},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"Facility support CPU PASS: maxSlope={maxSlope:R}; boundaryJump={maxBoundaryJump:R}; queryNs={sw.Elapsed.TotalNanoseconds/10000:R}; root/depth/geography=unchanged");
        }
        finally{PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous);}
    }
    internal static IEnumerable<(double East,double North)> Points()=>new[]{(0d,0d),(-32d,-24d),(32d,-24d),(-32d,24d),(32d,24d),(-32d,0d),(32d,0d),(0d,-24d),(0d,24d),(64d,0d),(96d,0d),(128d,0d),(160d,0d),(192.001d,0d),(224d,0d),(0d,120d),(0d,216d)};
    internal static Double3 Direction(double east,double north)
    {
        var r=FloridaFacilitySupport.Region;return (r.Up*r.RadiusMetres+r.East*east+r.North*north).Normalized();
    }
    internal static double Height(Double3 d)=>PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,d);
    internal static void AnalyzeLive(string output)
    {
        var region=FloridaFacilitySupport.Region;var rows=new List<object>();int contact=0,transition=0,outside=0;
        double maxContactGap=0,maxBaseError=0,maxNormalError=0;
        var reentry=Directory.GetFiles(output,"frame-*.json").Select(path=>
        {
            using var frame=JsonDocument.Parse(File.ReadAllText(path));
            return (Path:path,Frame:frame.RootElement.GetProperty("frame").GetInt32(),Level:frame.RootElement.GetProperty("level").GetInt32());
        }).Where(value=>value.Frame>=740&&value.Level==17).OrderBy(value=>value.Frame).First();
        using var json=JsonDocument.Parse(File.ReadAllText(reentry.Path));
        var f=json.RootElement;Require(f.GetProperty("level").GetInt32()==17&&f.GetProperty("maxOuter").GetDouble()==1,"supported reentry must exercise actual factor-1 L17 geometry");
        foreach(var sample in f.GetProperty("samples").EnumerateArray())
        {
            if(!sample.GetProperty("found").GetBoolean())continue;
            var e=sample.GetProperty("east").GetDouble();var n=sample.GetProperty("north").GetDouble();var d=Direction(e,n);
            var s=region.Sample(d);var original=PlanetaryPhysicalSurface.EvaluateNaturalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,d);
            var h=Height(d);var rendered=sample.GetProperty("height").GetDouble();
            var supportHeight=(region.RadiusMetres+region.PlaneAltitudeMetres)/Double3.Dot(d,region.Up)-region.RadiusMetres;
            var isContact=Math.Abs(e)<=32&&Math.Abs(n)<=24;
            if(isContact){contact++;maxContactGap=Math.Max(maxContactGap,Math.Abs(supportHeight-rendered));}
            if(s.Weight>0&&s.Weight<1)transition++;if(s.Weight==0)outside++;
            foreach(var vertex in sample.GetProperty("vertices").EnumerateArray())
            {
                var p=vertex.GetProperty("position");var direction=new Double3(p[0].GetDouble(),p[1].GetDouble(),p[2].GetDouble()).Normalized();
                var expected=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,direction);
                maxBaseError=Math.Max(maxBaseError,Math.Abs(expected-vertex.GetProperty("preparedHeight").GetDouble()));
                if(isContact){var norm=vertex.GetProperty("normal");maxNormalError=Math.Max(maxNormalError,(new Double3(norm[0].GetDouble(),norm[1].GetDouble(),norm[2].GetDouble())-region.Up).LengthSquared);}
            }
            rows.Add(new {east=e,north=n,natural=original,modifier=h-original,canonical=h,rendered,supportHeight,renderedMinusCanonical=rendered-h,foundationMinusRendered=isContact?supportHeight-rendered:(double?)null,weight=s.Weight});
        }
        Require(contact==9&&transition>0&&outside>0,"live contact/blend/outside coverage missing");
        Require(maxContactGap<.01&&maxBaseError<.003&&maxNormalError<1e-12,"live facility physical/contact/normal tolerance");
        int stableContactSamples=0;double maxMovingContactGap=0,maxFarTransportGap=0;
        foreach(var file in Directory.GetFiles(output,"frame-*.json"))
        {
            using var capture=JsonDocument.Parse(File.ReadAllText(file));var frame=capture.RootElement;
            if(frame.GetProperty("level").GetInt32()<16)continue;
            foreach(var sample in frame.GetProperty("samples").EnumerateArray())
            {
                var e=sample.GetProperty("east").GetDouble();var n=sample.GetProperty("north").GetDouble();
                if(!sample.GetProperty("found").GetBoolean()||Math.Abs(e)>32||Math.Abs(n)>24)continue;
                var d=Direction(e,n);var plane=(region.RadiusMetres+region.PlaneAltitudeMetres)/Double3.Dot(d,region.Up)-region.RadiusMetres;
                var camera=frame.GetProperty("camera");var distanceSquared=0d;
                for(int axis=0;axis<3;axis++){var delta=camera[axis].GetDouble()-frame.GetProperty("radius").GetDouble()*(axis==0?region.Up.X:axis==1?region.Up.Y:region.Up.Z);distanceSquared+=delta*delta;}
                var gap=Math.Abs(plane-sample.GetProperty("height").GetDouble());
                if(distanceSquared>1000d*1000d){maxFarTransportGap=Math.Max(maxFarTransportGap,gap);continue;}
                maxMovingContactGap=Math.Max(maxMovingContactGap,gap);stableContactSamples++;
            }
        }
        Require(stableContactSamples>18&&maxMovingContactGap<.01,"L16/L17 pupil/return support contact exceeded 1 cm");
        File.WriteAllText(Path.Combine(output,"facility-contact.json"),JsonSerializer.Serialize(new {rows,maxContactGap,maxBaseError,maxNormalError,contact,transition,outside,stableContactSamples,maxMovingContactGap,maxFarTransportGap},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Live facility support PASS: contact={contact}; transition={transition}; outside={outside}; maxContactGap={maxContactGap:R}; maxPreparedBaseError={maxBaseError:R}");
    }
    private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
}
