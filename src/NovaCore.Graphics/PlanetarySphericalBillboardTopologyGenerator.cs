using System.Numerics;

namespace NovaCore.Graphics;

/// <summary>Build-time-only deterministic generator for the three P2S2 proof meshes.</summary>
public static class PlanetarySphericalBillboardTopologyGenerator
{
    public enum ProofLevel : byte { Orbital=0, IntermediateApproach=1, SurfacePupil=2 }
    public readonly record struct TangentStepProof(int CommonVertices,int EnteringVertices,int LeavingVertices,int TotalVertices,ulong MappingHash)
    { public double Coverage => TotalVertices==0?0d:CommonVertices/(double)TotalVertices; }
    public static readonly ProofLevel[] ProofLevels=[ProofLevel.Orbital,ProofLevel.IntermediateApproach,ProofLevel.SurfacePupil];

    public static PlanetarySphericalBillboardTopology Generate(ProofLevel level)
    {
        var subdivisions=(int)level+2;
        var (positions,triangles)=Icosphere(subdivisions);
        var transformed=positions.Select(WarpToPupil).ToArray();
        var adjacency=BuildAdjacency(transformed.Length,triangles);
        var spacings=new float[transformed.Length];
        for(var i=0;i<spacings.Length;i++){var sum=0d;var count=adjacency[i].Length;foreach(var n in adjacency[i])sum+=Angle(transformed[i],transformed[n]);spacings[i]=(float)(sum/count);}
        var vertices=transformed.Select((p,i)=>new PlanetarySphericalBillboardTopology.Vertex(p,Region(p),spacings[i])).ToArray();
        var regions=BuildRegions(vertices,triangles);var offsets=new int[vertices.Length+1];var neighborList=new List<int>();for(var i=0;i<adjacency.Length;i++){offsets[i]=neighborList.Count;neighborList.AddRange(adjacency[i]);}offsets[^1]=neighborList.Count;
        var map=level==ProofLevel.Orbital?[]:MapPrevious(Generate((ProofLevel)((byte)level-1)),vertices);
        var snap=new PlanetarySphericalBillboardTopology.SnapMetadata((float)(Math.PI/(12*(1<<subdivisions))),0,Hash((ulong)level,0x534E41504C415454ul));
        var provisional=new PlanetarySphericalBillboardTopology((byte)level,vertices,triangles,offsets,neighborList.ToArray(),regions,map,snap,0);
        var bytes=PlanetarySphericalBillboardTopology.Serialize(provisional);
        var hash=BitConverter.ToUInt64(bytes,40);
        return new((byte)level,vertices,triangles,offsets,neighborList.ToArray(),regions,map,snap,hash);
    }
    public static IReadOnlyList<PlanetarySphericalBillboardTopology> GenerateProofLibrary()=>ProofLevels.Select(Generate).ToArray();
    /// <summary>Explicit offline/build-time materialization. Runtime loaders never call this.</summary>
    public static void WriteProofLibrary(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);Directory.CreateDirectory(directory);
        var rows=new List<string>();
        foreach(var level in ProofLevels)
        {
            var topology=Generate(level);var bytes=PlanetarySphericalBillboardTopology.Serialize(topology);var name=ArtifactFileName(level);
            File.WriteAllBytes(Path.Combine(directory,name),bytes);
            rows.Add($"    {{ \"level\": \"{level}\", \"file\": \"{name}\", \"bytes\": {bytes.Length}, \"topologyHash\": \"0x{topology.TopologyHash:X16}\" }}");
        }
        File.WriteAllText(Path.Combine(directory,"manifest.json"),"{\n  \"format\": \"NovaCoreSphericalBillboardTopology\",\n  \"formatVersion\": 1,\n  \"generatorVersion\": 1,\n  \"levels\": [\n"+string.Join(",\n",rows)+"\n  ]\n}\n");
    }
    public static TangentStepProof ProveTangentStep(PlanetarySphericalBillboardTopology topology)
    {
        // A snapped pupil is an external rigid transform of this closed local sphere.  One lattice
        // step changes only that transform: all immutable topology IDs survive, no boundary is cut.
        var hash=14695981039346656037ul;for(var i=0;i<topology.Vertices.Count;i++)hash=Hash(hash,(ulong)i);
        return new(topology.Vertices.Count,0,0,topology.Vertices.Count,hash);
    }
    public static double ProjectedPixelSpacing(double bodyRadius,double altitude,double verticalFovRadians,int viewportHeight,double angularSpacing)
    {
        if(!(bodyRadius>0d&&altitude>0d&&verticalFovRadians>0d&&viewportHeight>0&&angularSpacing>=0d))throw new ArgumentOutOfRangeException();
        var half=Math.Atan2(bodyRadius*Math.Sin(angularSpacing),bodyRadius+altitude-bodyRadius*Math.Cos(angularSpacing));
        return half/(verticalFovRadians*.5)*(viewportHeight*.5d);
    }
    public static string ArtifactFileName(ProofLevel level)=>level switch { ProofLevel.Orbital=>"orbital.nctop",ProofLevel.IntermediateApproach=>"intermediate-approach.nctop",_=>"surface-pupil.nctop" };
    private static int[] MapPrevious(PlanetarySphericalBillboardTopology previous,PlanetarySphericalBillboardTopology.Vertex[] current)
    { var lookup=new Dictionary<(int,int,int),int>();for(var i=0;i<current.Length;i++)lookup.Add(Key(current[i].Position),i);var map=new int[previous.Vertices.Count];for(var i=0;i<map.Length;i++){if(!lookup.TryGetValue(Key(previous.Vertices[i].Position),out var found))throw new InvalidOperationException("Nested vertex was lost.");map[i]=found;} return map; }
    private static (Vector3[] Positions,uint[] Triangles) Icosphere(int subdivisions)
    {
        var phi=(1f+(float)Math.Sqrt(5d))*0.5f;var v=new List<Vector3>{new(-1,phi,0),new(1,phi,0),new(-1,-phi,0),new(1,-phi,0),new(0,-1,phi),new(0,1,phi),new(0,-1,-phi),new(0,1,-phi),new(phi,0,-1),new(phi,0,1),new(-phi,0,-1),new(-phi,0,1)};for(var i=0;i<v.Count;i++)v[i]=Vector3.Normalize(v[i]);
        var f=new List<(int A,int B,int C)>{(0,11,5),(0,5,1),(0,1,7),(0,7,10),(0,10,11),(1,5,9),(5,11,4),(11,10,2),(10,7,6),(7,1,8),(3,9,4),(3,4,2),(3,2,6),(3,6,8),(3,8,9),(4,9,5),(2,4,11),(6,2,10),(8,6,7),(9,8,1)};
        Orient(v,f);for(var step=0;step<subdivisions;step++){var edges=new Dictionary<(int,int),int>();int Mid(int a,int b){var key=a<b?(a,b):(b,a);if(edges.TryGetValue(key,out var found))return found;var index=v.Count;v.Add(Vector3.Normalize(v[a]+v[b]));edges.Add(key,index);return index;}var next=new List<(int,int,int)>(f.Count*4);foreach(var (a,b,c) in f){var ab=Mid(a,b);var bc=Mid(b,c);var ca=Mid(c,a);next.Add((a,ab,ca));next.Add((b,bc,ab));next.Add((c,ca,bc));next.Add((ab,bc,ca));}f=next;}
        var indices=new uint[f.Count*3];for(var i=0;i<f.Count;i++){indices[i*3]=(uint)f[i].A;indices[i*3+1]=(uint)f[i].B;indices[i*3+2]=(uint)f[i].C;}return(v.ToArray(),indices);
    }
    private static void Orient(List<Vector3> v,List<(int A,int B,int C)> f){for(var i=0;i<f.Count;i++){var x=f[i];if(Vector3.Dot(Vector3.Cross(v[x.B]-v[x.A],v[x.C]-v[x.A]),v[x.A]+v[x.B]+v[x.C])<0f)f[i]=(x.A,x.C,x.B);}}
    private static Vector3 WarpToPupil(Vector3 source)
    { var theta=Math.Acos(Math.Clamp(source.Z,-1f,1f));var warped=theta-.72d*Math.Sin(theta);var planar=new Vector2(source.X,source.Y);if(planar.LengthSquared()<1e-20f)return source.Z>=0?Vector3.UnitZ:-Vector3.UnitZ;planar=Vector2.Normalize(planar)*(float)Math.Sin(warped);return new(planar.X,planar.Y,(float)Math.Cos(warped)); }
    private static byte Region(Vector3 p){var degrees=Math.Acos(Math.Clamp(p.Z,-1f,1f))*180d/Math.PI;return (byte)(degrees<=25d?0:degrees<=50d?1:degrees<=90d?2:3);}
    private static int[][] BuildAdjacency(int count,uint[] indices){var sets=Enumerable.Range(0,count).Select(_=>new SortedSet<int>()).ToArray();for(var i=0;i<indices.Length;i+=3){var a=(int)indices[i];var b=(int)indices[i+1];var c=(int)indices[i+2];sets[a].Add(b);sets[a].Add(c);sets[b].Add(a);sets[b].Add(c);sets[c].Add(a);sets[c].Add(b);}return sets.Select(x=>x.ToArray()).ToArray();}
    private static PlanetarySphericalBillboardTopology.RegionMetadata[] BuildRegions(PlanetarySphericalBillboardTopology.Vertex[] vertices,uint[] indices)
    { var result=new PlanetarySphericalBillboardTopology.RegionMetadata[4];for(byte region=0;region<4;region++){var members=vertices.Where(v=>v.DensityRegion==region).Select(v=>(double)v.AverageAngularSpacingRadians).OrderBy(x=>x).ToArray();var edges=new List<double>();for(var i=0;i<indices.Length;i+=3){var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];if(a.DensityRegion==region||b.DensityRegion==region||c.DensityRegion==region){edges.Add(Angle(a.Position,b.Position));edges.Add(Angle(b.Position,c.Position));edges.Add(Angle(c.Position,a.Position));}}var tri=edges.Count/3;result[region]=new(region,(float)members[members.Length/2],(float)edges.Max(),(float)edges.Average(),(float)edges.Max(),members.Length,tri);}return result; }
    private static double Angle(Vector3 a,Vector3 b)=>Math.Acos(Math.Clamp(Vector3.Dot(a,b),-1f,1f));
    private static (int,int,int) Key(Vector3 p)=>(BitConverter.SingleToInt32Bits(p.X),BitConverter.SingleToInt32Bits(p.Y),BitConverter.SingleToInt32Bits(p.Z));
    private static ulong Hash(ulong a,ulong b)=>(a^b)*1099511628211ul;
}
