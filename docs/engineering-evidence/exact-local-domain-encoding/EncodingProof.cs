// CPU feasibility only. Calls banked CPU authority, never simulates GPU parity.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

const double R=6371008.8;
var root=Path.GetFullPath(args[0]);
if(!EarthElevationDataset.TryLoad(Path.Combine(root,"assets/earth/runtime"),out var error))throw new Exception(error);
if(!TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var local,out error))throw new Exception(error);
if(!EarthLocalTerrainElevationDataset.TryLoad(local,out error))throw new Exception(error);
var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
var identity=new PlanetaryNaturalTerrainFamilyIdentity(6,2,0x4D12D2B1u);
var cases=new List<Probe>();
// Explicit aliases on different cube faces, using one canonical point and two
// actual face-interior anchors approaching that same edge/corner at L17 scale.
var faceAliases=new List<(CubeSphereFace Face,double U,double V,Double3 Point)>();
foreach(var face in Enum.GetValues<CubeSphereFace>())foreach(double u in new[]{0d,.5d,1d})foreach(double v in new[]{0d,.5d,1d})
    if(u!=.5||v!=.5)faceAliases.Add((face,u,v,RelaxedCubeSphereProjection.UnitDirection(face,u,v)*R));
for(int i=0;i<faceAliases.Count;i++)for(int j=i+1;j<faceAliases.Count;j++)
{
    var x=faceAliases[i];var y=faceAliases[j];
    if(x.Face==y.Face || (x.Point-y.Point).LengthSquared!=0)continue;
    double scale=Math.ScaleB(1d,-17);
    var a=RelaxedCubeSphereProjection.UnitDirection(x.Face,x.U+(.5-x.U)*scale,x.V+(.5-x.V)*scale)*R;
    var b=RelaxedCubeSphereProjection.UnitDirection(y.Face,y.U+(.5-y.U)*scale,y.V+(.5-y.V)*scale)*R;
    AddNeighbors("explicit-cross-face-edge-corner",x.Point,a,b);
}
// Every level has adjacent-patch, parent/child, edge and corner contexts.
foreach(var face in Enum.GetValues<CubeSphereFace>())for(int level=0;level<=17;level++)
foreach(var uv in new[]{(0d,.5d), (1d,.5d), (0d,0d),(.5d,.5d),(.5d+Math.ScaleB(1d,-level-1),.5d)})
{
    var p=RelaxedCubeSphereProjection.UnitDirection(face,uv.Item1,uv.Item2)*R;
    var step=Math.ScaleB(1d,-level);
    var a=RelaxedCubeSphereProjection.UnitDirection(face,Math.Clamp(uv.Item1-step*.25,0,1),Math.Clamp(uv.Item2-step*.25,0,1))*R;
    var b=RelaxedCubeSphereProjection.UnitDirection(face,Math.Clamp(uv.Item1+step*.25,0,1),Math.Clamp(uv.Item2+step*.25,0,1))*R;
    AddNeighbors($"L{level}/{face}/{uv}",p,a,b);
}
var support=FloridaFacilitySupport.Region;
foreach(double e in new[]{-192d,-128d,-64d,0,64d,128d,192d})foreach(double n in new[]{-184d,-56d,0,56d,184d})
{
    var p=(support.Up*R+support.East*e+support.North*n).Normalized()*(R+16);
    AddNeighbors("facility-support-boundary",p,support.Up*R,support.Up*R+support.East*64);
}
// Read one real regional sector identity, then straddle its geographic boundary.
using(var f=File.OpenRead(local))
{
    f.Position=PlanetaryLocalTerrainPackContract.HeaderBytes;
    var header=new byte[PlanetaryLocalTerrainPackContract.RecordHeaderBytes];f.ReadExactly(header);
    if(!PlanetaryLocalTerrainPackContract.TryReadRecordHeader(header,out var record))throw new Exception("regional header");
    var s=record.Sector;double count=1<<s.Level;
    foreach(double offset in new[]{-1e-8,0,1e-8})
    {
        var p=RelaxedCubeSphereProjection.UnitDirection(s.Face,(s.X+1+offset)/count,(s.Y+.5)/count)*R;
        AddNeighbors("regional-boundary",p,p.Normalized()*R,p.Normalized()*R+Double3.UnitX*64);
    }
    if(!RelaxedCubeSphereProjection.TryAddress(support.Up,out var ff,out var fu,out var fv))throw new Exception("Florida address");
    f.Position=PlanetaryLocalTerrainPackContract.HeaderBytes;
    while(f.Position<f.Length)
    {
        f.ReadExactly(header);
        if(!PlanetaryLocalTerrainPackContract.TryReadRecordHeader(header,out var entry))throw new Exception("regional entry");
        var sector=entry.Sector;double cells=1<<sector.Level;
        if(sector.Face==ff && sector.X==(int)(fu*cells) && sector.Y==(int)(fv*cells))
        foreach(double offset in new[]{-1e-8,0,1e-8})
        {
            var p=RelaxedCubeSphereProjection.UnitDirection(ff,(sector.X+1+offset)/cells,(sector.Y+.5)/cells)*R;
            AddNeighbors($"Florida-regional-sector-boundary-L{sector.Level}",p,p.Normalized()*R,p.Normalized()*R+Double3.UnitX*64);
        }
        f.Position=checked((long)(entry.PayloadOffset+entry.StoredAlbedoBytes+entry.StoredElevationBytes+entry.StoredNormalBytes+entry.StoredControlBytes));
    }
}
// The split camera may itself have a small transverse component carrying both
// high and low words; subtracting zero view preserves those bits at near range.
{
    float hi=1e-4f,lo=1e-12f;
    for(int i=0;i<128;i++)
    {
        var yz=Split(new Double3(0,R*.6+9.6,R*.8+12.8));
        var camera=new Double3((double)hi+lo,yz.Y,yz.Z);
        var p=camera-F(new Double3(0,9.6,12.8));
        cases.Add(new("reachable-near-axis-camera",p,new[]{new Double3(32,p.Y,p.Z),new Double3(-32,p.Y,p.Z)}));
        lo=MathF.BitIncrement(lo);
    }
}
// Camera split minus FP32 view: reachable input lattice at all five named regimes.
foreach(double distance in new[]{10d,40d,49.999,50d,1000d,700000d})for(int i=0;i<12;i++)
{
    var up=new Double3(Math.Sin(i+.4),Math.Cos(i*.7+.2),Math.Sin(i*.31+1)).Normalized();
    var a=up*R;var c=Split(a+up*distance);var v=F(c-up*(R+16));
    var p=c-v;var p2=c-new Double3(MathF.BitIncrement((float)v.X),v.Y,v.Z);
    cases.Add(new($"camera-distance-{distance}",p,new[]{a,a+Double3.UnitX*64}));
    cases.Add(new($"camera-distance-{distance}",p2,new[]{a,a+Double3.UnitX*64}));
}
// Near-zero transverse coordinate is a valid surface direction; anchor displacement
// must not erase it. These are deliberately wider than the reachable camera subset.
foreach(int exponent in new[]{-4,-20,-40,-80,-120})
    AddNeighbors("near-zero-axis",new Double3(Math.ScaleB(1.234567891,exponent),R*.6,R*.8),
        new Double3(64,R*.6,R*.8),new Double3(-64,R*.6,R*.8));
// An additional actual split-camera lattice at a transverse-axis crossing, with
// |view|=16 m. This prevents broad-patch stress alone deciding a near-only lead.
foreach(float offset in new[]{1e-4f,1e-6f,1e-8f})
{
    float low=offset;
    for(int i=0;i<128;i++)
    {
        var camera=new Double3(16d+low,Split(new Double3(0,R*.6,R*.8)).Y,Split(new Double3(0,R*.6,R*.8)).Z);
        var p=camera-new Double3(16,0,0);
        cases.Add(new("reachable-near-axis-camera",p,new[]{new Double3(32,p.Y,p.Z),new Double3(-32,p.Y,p.Z)}));
        low=MathF.BitIncrement(low);
    }
}

var names=new[]{"global-fp64-control","single-fp32-control","split-fp32-local","exact-two-diff-local","fixed64-q2^-40","ksa-pattern"};
var results=new Dictionary<string,Summary>();
foreach(var name in names)
{
    var stats=new Summary();var seen=new Dictionary<string,Double3>();
    using var digest=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach(var c in cases)
    {
        var canonical=Evaluate(c.Point);Physical? first=null;
        foreach(var anchor in c.Anchors)
        {
            var groupName=c.Label.StartsWith("L")?c.Label.Split('/')[0]:c.Label;
            if(!stats.Subsets.TryGetValue(groupName,out var subset))stats.Subsets[groupName]=subset=new();
            var enc=Encode(name,c.Point,anchor);
            stats.Attempts++;
            if(enc is null){stats.RangeRejects++;continue;}
            var val=Evaluate(enc.Value.Point,enc.Value.Direction);
            var key=Key(anchor)+"/"+enc.Value.Payload;
            if(seen.TryGetValue(key,out var other)&&!Same(other,c.Point))
            {
                stats.Collisions++;
                subset.Collisions++;
                var otherValue=Evaluate(other);
                if(!Same(otherValue.Input,canonical.Input))stats.CollisionsChangingInput++;
                if(Bits(otherValue.Height)!=Bits(canonical.Height)||Bits(otherValue.Near)!=Bits(canonical.Near))stats.CollisionsChangingHeight++;
                if(c.Label=="reachable-near-axis-camera" && stats.ReachableNearCollisionWitness is null &&
                    (!Same(otherValue.Input,canonical.Input)))
                    stats.ReachableNearCollisionWitness=new{anchor=Vec(anchor),a=Vec(other),b=Vec(c.Point),payload=enc.Value.Payload,inputA=Vec(otherValue.Input),inputB=Vec(canonical.Input),heightA=otherValue.Height,heightB=canonical.Height,nearA=otherValue.Near,nearB=canonical.Near,finalA=Vec(otherValue.Final),finalB=Vec(canonical.Final)};
                if(stats.CollisionWitness is null || (stats.CollisionWitnessHeightEqual && Bits(otherValue.Height)!=Bits(canonical.Height)))
                {stats.CollisionWitness=new{c.Label,anchor=Vec(anchor),a=Vec(other),b=Vec(c.Point),payload=enc.Value.Payload,heightA=otherValue.Height,heightB=canonical.Height,nearA=otherValue.Near,nearB=canonical.Near};stats.CollisionWitnessHeightEqual=Bits(otherValue.Height)==Bits(canonical.Height);}
            }
            else seen.TryAdd(key,c.Point);
            stats.Observe(canonical,val);
            subset.Samples++;if(!Same(canonical.Input,val.Input))subset.InputMismatches++;
            if(!Same(canonical.Final,val.Final))subset.FinalMismatches++;
            if(Bits(canonical.Height)!=Bits(val.Height))subset.HeightMismatches++;
            subset.HeightMax=Math.Max(subset.HeightMax,Math.Abs(canonical.Height-val.Height));
            subset.NearMax=Math.Max(subset.NearMax,Math.Abs(canonical.Near-val.Near));
            subset.RegionalNonzero+=canonical.Regional!=0?1:0;
            subset.SupportNonzero+=canonical.Support!=0?1:0;
            if(Math.Abs(canonical.Height-val.Height)>stats.WitnessHeightDelta)
            {stats.WitnessHeightDelta=Math.Abs(canonical.Height-val.Height);stats.MaximumHeightWitness=new{c.Label,anchor=Vec(anchor),point=Vec(c.Point),decoded=Vec(enc.Value.Point),directionA=Vec(canonical.Direction),directionB=Vec(val.Direction),heightA=canonical.Height,heightB=val.Height,globalA=canonical.Global,globalB=val.Global,regionalA=canonical.Regional,regionalB=val.Regional,supportA=canonical.Support,supportB=val.Support};}
            if(first.HasValue && !SamePhysical(first.Value,val))stats.AnchorMismatches++;
            first??=val;
            if(stats.MismatchWitness is null&&!SamePhysical(canonical,val))stats.MismatchWitness=new{c.Label,anchor=Vec(anchor),point=Vec(c.Point),decoded=Vec(enc.Value.Point),inputA=Vec(canonical.Input),inputB=Vec(val.Input),canonical.Height,candidateHeight=val.Height,nearA=canonical.Near,nearB=val.Near};
            digest.AppendData(Encoding.UTF8.GetBytes(c.Label+"/"+Key(anchor)+"/"+enc.Value.Payload+"/"+PhysicalKey(val)+"\n"));
        }
    }
    stats.StreamSha256=Convert.ToHexStringLower(digest.GetHashAndReset());results[name]=stats;
}
Console.WriteLine(JsonSerializer.Serialize(new{kind="CPU representation and production CPU H evaluation; no GPU or raster inference",cases=cases.Count,
    groups=cases.GroupBy(x=>x.Label.StartsWith("L")?x.Label.Split('/')[0]:x.Label).ToDictionary(x=>x.Key,x=>x.Count()),
    globalLoaded=EarthElevationDataset.IsLoaded,regionalLoaded=EarthLocalTerrainElevationDataset.IsLoaded,localPath=local,
    localSha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(local))),globalSha256=EarthElevationDataset.Sha256,
    facilityIdentity=FloridaFacilitySupport.DefinitionIdentity,results},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));

void AddNeighbors(string label,Double3 p,Double3 a,Double3 b)
{
    for(int i=0;i<4;i++){cases.Add(new(label,p,new[]{a,b,p.Normalized()*R}));p=new(Math.BitIncrement(p.X),p.Y,p.Z);}
}
Physical Evaluate(Double3 p,Double3? overrideDirection=null)
{
    var d=overrideDirection??Norm(p);var input=Norm(d)*R;
    var family=PlanetaryNaturalTerrainFamilies.EvaluateComposed(input,identity).Near;
    var grade=FloridaFacilitySupport.Region.Sample(d);
    var h=terrain.SamplePhysicalSurface(d,PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
    var global=EarthElevationDataset.SampleElevation(d);var regional=EarthLocalTerrainElevationDataset.SampleResidual(d);
    return new(p,d,input,h.FinalHeightMetres,Norm(d)*(R+h.FinalHeightMetres),family.Height*(1-grade.Weight),grade.Weight,global,regional,h.PhysicalNormal);
}
Encoded? Encode(string name,Double3 p,Double3 a)
{
    if(name=="global-fp64-control")return new(p,Key(p),null);
    if(name=="ksa-pattern")
    {
        // Independent implementation of the observed responsibility pattern, with
        // exact upstream direction and no FP32 rotation error: favorable to KSA.
        var au=Norm(a);var hi=F(au);var lo=F(au-hi);var delta=F(Norm(p)-au);
        float m=2*(float)Double3.Dot(hi,delta)+(float)Double3.Dot(delta,delta);
        float k=-.5f*m+.375f*m*m;
        var corrected=F(delta+F(F(hi+delta)*k));
        var d=hi+(lo+corrected);
        return new(d*Len(p),Key(hi)+Key(lo)+Key(delta)+Bits(Len(p)),d);
    }
    var decoded=new double[3];var payload=new List<string>();
    var pv=new[]{p.X,p.Y,p.Z};var av=new[]{a.X,a.Y,a.Z};
    for(int i=0;i<3;i++)
    {
        double delta=pv[i]-av[i];
        if(name=="single-fp32-control"){float f=(float)delta;decoded[i]=av[i]+f;payload.Add(Bits(f));}
        else if(name=="split-fp32-local")
        {
            // Favorable two-float candidate: do not discard subtraction roundoff
            // before splitting, and compensate the anchor reconstruction too.
            var b=delta-pv[i];var err=(pv[i]-(delta-b))-(av[i]+b);
            float hi=(float)delta,lo=(float)((delta-hi)+err);
            var sum=av[i]+hi;var z=sum-av[i];var tail=(av[i]-(sum-z))+(hi-z);
            decoded[i]=sum+(tail+lo);payload.Add(Bits(hi)+Bits(lo));
        }
        else if(name=="exact-two-diff-local")
        {
            // Error-free subtraction, then error-free addition and residual recovery.
            var b=delta-pv[i];var err=(pv[i]-(delta-b))-(av[i]+b);
            var s=av[i]+delta;var z=s-av[i];var e=(av[i]-(s-z))+(delta-z);
            decoded[i]=s+(e+err);
            // Exact numeric zero has two encodings. Preserve the source sign explicitly.
            var zeroSign=pv[i]==0?Bits(pv[i]):"nonzero";
            if(pv[i]==0)decoded[i]=pv[i];
            payload.Add(Bits(delta)+Bits(err)+zeroSign);
        }
        else
        {
            var scaled=Math.ScaleB(delta,40);
            if(!double.IsFinite(scaled)||scaled>=Math.ScaleB(1d,63)||scaled < -Math.ScaleB(1d,63))return null;
            long q=checked((long)Math.Round(scaled,MidpointRounding.ToEven));
            decoded[i]=av[i]+Math.ScaleB((double)q,-40);payload.Add(q.ToString("x16"));
        }
    }
    return new(new Double3(decoded[0],decoded[1],decoded[2]),string.Join('/',payload),null);
}
static Double3 F(Double3 p)=>new((float)p.X,(float)p.Y,(float)p.Z);
static Double3 Split(Double3 p){var hi=F(p);return hi+F(p-hi);}
static Double3 Norm(Double3 p)=>p*(1d/Math.Sqrt(Double3.Dot(p,p)));
static double Len(Double3 p)=>Math.Sqrt(p.LengthSquared);
static string Bits(double v)=>BitConverter.DoubleToUInt64Bits(v).ToString("x16");
static string Key(Double3 p)=>Bits(p.X)+Bits(p.Y)+Bits(p.Z);
static object Vec(Double3 p)=>new{p.X,p.Y,p.Z,bits=Key(p)};
static bool Same(Double3 a,Double3 b)=>Key(a)==Key(b);
static string PhysicalKey(Physical p)=>Key(p.Point)+Key(p.Direction)+Key(p.Input)+Bits(p.Height)+Key(p.Final)+Bits(p.Near)+Bits(p.Support)+Bits(p.Global)+Bits(p.Regional)+Key(p.Normal);
static bool SamePhysical(Physical a,Physical b)=>PhysicalKey(a)==PhysicalKey(b);
record Probe(string Label,Double3 Point,Double3[] Anchors);
record struct Encoded(Double3 Point,string Payload,Double3? Direction);
record struct Physical(Double3 Point,Double3 Direction,Double3 Input,double Height,Double3 Final,double Near,double Support,double Global,double Regional,Double3 Normal);
class Summary
{
    public long Attempts,Samples,RangeRejects,Collisions,CollisionsChangingInput,CollisionsChangingHeight,AnchorMismatches;
    public string StreamSha256="";public object? CollisionWitness,MismatchWitness;public bool CollisionWitnessHeightEqual;
    public double WitnessHeightDelta;public object? MaximumHeightWitness;
    public object? ReachableNearCollisionWitness;
    public Dictionary<string,Metric> Metrics=new();
    public Dictionary<string,Subset> Subsets=new();
    public void Observe(Physical a,Physical b)
    {
        Samples++;
        V("point",a.Point,b.Point);V("direction",a.Direction,b.Direction);V("input",a.Input,b.Input);V("final",a.Final,b.Final);V("normal",a.Normal,b.Normal);
        S("height",a.Height,b.Height);S("near",a.Near,b.Near);S("support",a.Support,b.Support);S("global",a.Global,b.Global);S("regional",a.Regional,b.Regional);
        double angle=Math.Atan2(Math.Sqrt(Double3.Cross(a.Direction,b.Direction).LengthSquared),Double3.Dot(a.Direction,b.Direction));
        Get("angular").Add(angle,angle==0);
    }
    Metric Get(string name){if(!Metrics.TryGetValue(name,out var m))Metrics[name]=m=new();return m;}
    void S(string name,double a,double b)=>Get(name).Add(Math.Abs(a-b),BitConverter.DoubleToUInt64Bits(a)==BitConverter.DoubleToUInt64Bits(b));
    void V(string name,Double3 a,Double3 b)=>Get(name).Add(Math.Sqrt((a-b).LengthSquared),BitConverter.DoubleToUInt64Bits(a.X)==BitConverter.DoubleToUInt64Bits(b.X)&&BitConverter.DoubleToUInt64Bits(a.Y)==BitConverter.DoubleToUInt64Bits(b.Y)&&BitConverter.DoubleToUInt64Bits(a.Z)==BitConverter.DoubleToUInt64Bits(b.Z));
}
class Subset {public long Samples,Collisions,InputMismatches,HeightMismatches,FinalMismatches,RegionalNonzero,SupportNonzero;public double HeightMax,NearMax;}
class Metric
{
    public long N,Exact;public long Mismatch=>N-Exact;public double Max;private double squared;public double Rms=>Math.Sqrt(squared/N);
    public void Add(double delta,bool exact){N++;if(exact)Exact++;Max=Math.Max(Max,delta);squared+=delta*delta;}
}
