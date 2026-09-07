using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

// Diagnostic only: evaluate the CURRENT gameplay oracle at actual GPU TES
// positions, and independently sample every selected prepared triangle.
internal static class CoherenceProof
{
    const PlanetaryPhysicalSurfaceGeneration Generation=PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate;
    static readonly PlanetaryTerrainDefinition Terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
    static double Height(Double3 d)=>PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(Terrain,d,Generation);
    static double Len(Double3 v)=>Math.Sqrt(v.LengthSquared);
    static double[] A(Double3 v)=>[v.X,v.Y,v.Z];
    static Double3 V(BinaryReader b)=>new(b.ReadDouble(),b.ReadDouble(),b.ReadDouble());
    static Double3 F(Double3 v)=>new((float)v.X,(float)v.Y,(float)v.Z);
    static void Require(bool value,string error){if(!value)throw new Exception(error);}
    sealed class Stats
    {
        public int Count; public double SumSquares,Min=double.PositiveInfinity,Max=double.NegativeInfinity,MaxAbs;
        public object? Worst;
        public void Add(double e,object detail){Count++;SumSquares+=e*e;Min=Math.Min(Min,e);Max=Math.Max(Max,e);if(Math.Abs(e)>=MaxAbs){MaxAbs=Math.Abs(e);Worst=detail;}}
        public object Report()=>new {count=Count,min=Count>0?(double?)Min:null,max=Count>0?(double?)Max:null,maxAbs=MaxAbs,rms=Count>0?(double?)Math.Sqrt(SumSquares/Count):null,worst=Worst};
    }
    static void Main(string[] args)
    {
        string root=Path.GetFullPath(args[0]),runtime=Path.GetFullPath(args[1]);
        Require(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out var manifest,out var local,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(local,out error),error);
        Require(EarthElevationDataset.IsLoaded&&EarthLocalTerrainElevationDataset.IsLoaded,"Both production datasets must be loaded");
        var line=File.ReadLines(Path.Combine(runtime,"runtime.log")).Last(x=>x.Contains("P2S5F directional visibility: submittedFrame=175;"));
        var match=Regex.Match(line,@"cameraBody=\(([^)]+)\)");
        var xyz=match.Groups[1].Value.Split(',').Select(x=>double.Parse(x,CultureInfo.InvariantCulture)).ToArray();
        var camera=new Double3(xyz[0],xyz[1],xyz[2]);
        double radius=PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
        var stats=new Dictionary<string,Stats>();
        void Add(string kind,Double3 p,int primitive,double[] bary,bool visible)
        {
            double length=Len(p),h=Height(p/length),e=length-radius-h,dist=Len(p-camera);
            var detail=new {primitive,bary,body=A(p),distance=dist,renderHeight=length-radius,physicalHeight=h,error=e,visible};
            foreach(string key in new[]{kind+"/all",kind+"/"+(dist<=50?"0-50m":dist<=100?"50-100m":dist<=500?"100-500m":"beyond500m"),visible?kind+"/clip-visible":""})
            {if(key.Length==0)continue;if(!stats.TryGetValue(key,out var s))stats[key]=s=new();s.Add(e,detail);}
        }
        int tesCount; double maxNear=0;
        using(var reader=new BinaryReader(File.OpenRead(Path.Combine(runtime,"tes.bin"))))
        {
            tesCount=reader.ReadInt32();Require(tesCount<=65536,"capture overflow");reader.BaseStream.Position=32;
            for(int i=0;i<tesCount;i++)
            {
                var p=V(reader);reader.ReadDouble();V(reader);reader.ReadDouble();
                float x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle(),w=reader.ReadSingle();
                double[] bary=[reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()];reader.ReadSingle();
                maxNear=Math.Max(maxNear,Math.Abs(reader.ReadSingle()));for(int j=0;j<3;j++)reader.ReadSingle();
                int parent=reader.ReadInt32();for(int j=0;j<3;j++)reader.ReadInt32();
                Add("gpuTes",p,parent,bary,w>0&&Math.Abs(x)<=w&&Math.Abs(y)<=w&&z>=0&&z<=w);
            }
        }
        int count=(int)(new FileInfo(Path.Combine(runtime,"prepared.bin")).Length/64);
        var positions=new Double3[count];var normals=new Double3[count];var preparedHeight=new double[count];
        using(var r=new BinaryReader(File.OpenRead(Path.Combine(runtime,"prepared.bin"))))
        for(int i=0;i<count;i++){positions[i]=V(r);preparedHeight[i]=r.ReadDouble();normals[i]=new(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int j=0;j<5;j++)r.ReadSingle();}
        var indices=new List<int>();using(var r=new BinaryReader(File.OpenRead(Path.Combine(runtime,"selected.bin"))))while(r.BaseStream.Position<r.BaseStream.Length)indices.Add(r.ReadInt32());
        Require(indices.Count%3==0&&indices.All(x=>x>=0&&x<count),"selected index bounds");
        var vertexError=new Stats();
        foreach(int i in indices.Distinct())
        {
            double h=Height(positions[i]/Len(positions[i]));
            vertexError.Add(preparedHeight[i]-h,new{vertex=i,body=A(positions[i]),preparedHeight=preparedHeight[i],physicalHeight=h});
        }
        // Endpoint and interior probes of EVERY selected triangle. These are
        // candidate render interpolation probes, not invented full-H vertices.
        // FP32 camera-relative transport follows the unchanged VS/TES boundary.
        var barycentrics=new List<double[]>();
        for(int i=0;i<=4;i++)for(int j=0;j<=4-i;j++)barycentrics.Add([i*.25,j*.25,1-(i+j)*.25]);
        int triangleProbes=0;var transport=new Stats();
        for(int tri=0;tri<indices.Count/3;tri++)
        {
            var a=F(positions[indices[tri*3]]-camera);var b=F(positions[indices[tri*3+1]]-camera);var c=F(positions[indices[tri*3+2]]-camera);
            var near=Len(a)<=500||Len(b)<=500||Len(c)<=500;
            IEnumerable<double[]> weightsToTest=near?barycentrics:new[]{new[]{1d/3,1d/3,1d/3}};
            foreach(var weights in weightsToTest)
            {
                var relative=F(F(F(a*weights[0])+F(b*weights[1]))+F(c*weights[2]));var p=camera+relative;
                Add("preparedTriangle",p,tri,weights,false);triangleProbes++;
                if(near)
                {
                    var p64=positions[indices[tri*3]]*weights[0]+positions[indices[tri*3+1]]*weights[1]+positions[indices[tri*3+2]]*weights[2];
                    Add("publishedTriangle64",p64,tri,weights,false);
                    transport.Add(Len(p-p64),new{triangle=tri,distance=Len(relative),bary=weights});
                }
            }
        }
        var region=FloridaFacilitySupport.Region;var contacts=new List<object>();var contactErrors=new Stats();
        foreach(var (east,north) in new[]{(0d,0d),(-32d,-24d),(32d,-24d),(-32d,24d),(32d,24d),(-32d,0d),(32d,0d),(0d,-24d),(0d,24d),(96d,0d),(128d,0d),(160d,0d),(192.001d,0d),(224d,0d),(0d,120d),(0d,216d)})
        {
            var origin=region.Up*region.RadiusMetres+region.East*east+region.North*north;bool found=false;
            for(int tri=0;tri<indices.Count/3;tri++)
            {
                var a=camera+F(positions[indices[tri*3]]-camera);var b=camera+F(positions[indices[tri*3+1]]-camera);var c=camera+F(positions[indices[tri*3+2]]-camera);
                double ax=Double3.Dot(a-origin,region.East),ay=Double3.Dot(a-origin,region.North),bx=Double3.Dot(b-origin,region.East),by=Double3.Dot(b-origin,region.North),cx=Double3.Dot(c-origin,region.East),cy=Double3.Dot(c-origin,region.North);
                double det=(by-cy)*(ax-cx)+(cx-bx)*(ay-cy);if(Math.Abs(det)<1e-20)continue;
                double u=((by-cy)*(-cx)+(cx-bx)*(-cy))/det,v=((cy-ay)*(-cx)+(ax-cx)*(-cy))/det,t=1-u-v;
                if(u < -1e-8||v < -1e-8||t < -1e-8)continue;
                var p=a*u+b*v+c*t;double planeGap=region.RadiusMetres+region.PlaneAltitudeMetres-Double3.Dot(p,region.Up);
                var detail=new{east,north,found=true,triangle=tri,bary=new[]{u,v,t},body=A(p),planeGap,physicalGap=Len(p)-radius-Height(p/Len(p)),supportWeight=region.Sample(p/Len(p)).Weight};
                contacts.Add(detail);if(Math.Abs(east)<=32&&Math.Abs(north)<=24)contactErrors.Add(planeGap,detail);found=true;break;
            }
            if(!found)contacts.Add(new{east,north,found=false});
        }
        var result=new{camera=A(camera),radius,tesCount,maximumTesNearHeight=maxNear,preparedVertices=count,selectedTriangles=indices.Count/3,triangleProbes,productionLocalAsset=local,source="GPU final TES body positions; every selected triangle centroid plus 15 barycentric probes per triangle within 500 m; current production CPU full-H oracle",transportWithin500m=transport.Report(),preparedVertexError=vertexError.Report(),statistics=stats.ToDictionary(x=>x.Key,x=>x.Value.Report()),floridaContacts=contacts,floridaCore=contactErrors.Report()};
        File.WriteAllText(args[2],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(new{tesCount,preparedVertexError=vertexError.Report(),near=stats.Where(x=>x.Key.Contains("0-50m")).ToDictionary(x=>x.Key,x=>x.Value.Report()),floridaCore=contactErrors.Report()}));
    }
}
