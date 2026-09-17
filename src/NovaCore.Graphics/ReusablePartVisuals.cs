using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public readonly record struct VisualSocket(string Name,Double3 Point,Double3 Outward);
public sealed class PartVisualMesh
{
    public string Name {get;}
    public bool Gimballed {get;}
    public MeshHandle Handle {get;internal set;}
    internal NativeVisualVertex[] Vertices {get;}
    internal uint[] Indices {get;}
    internal PartVisualMesh(string name,bool gimballed,NativeVisualVertex[] vertices,uint[] indices)
    {Name=name;Gimballed=gimballed;Vertices=vertices;Indices=indices;}
    public int VertexCount=>Vertices.Length;
    public int TriangleCount=>Indices.Length/3;
}
public sealed record PartVisualAsset(string Identity,string Sha256,ImmutableArray<PartVisualMesh> Meshes,ImmutableArray<VisualSocket> Sockets,Double3 GimbalPivot);

/// <summary>Cold, hash-qualified GLB intake. Only rigid, opaque constant-PBR parts
/// are admitted. Sockets are presentation references, never physical authority.</summary>
public static class PartVisualLoader
{
    private static void Require(bool value,string why){if(!value)throw new InvalidDataException("Part visual: "+why);}
    private readonly record struct Pose(Double3 P,DoubleQuaternion Q)
    {
        internal Double3 Point(Double3 p)=>P+Q.Rotate(p);
        internal Pose Then(Pose b)=>new(Point(b.P),(Q*b.Q).Normalized());
    }
    // glTF (x,y,z) -> NovaCore body (-z,x,-y), determinant +1.
    public static Double3 FromGltf(Double3 v)=>new(-v.Z,v.X,-v.Y);
    private static Double3 Vector(JsonElement e)=>new(e[0].GetDouble(),e[1].GetDouble(),e[2].GetDouble());
    private static Pose NodePose(JsonElement n)
    {
        Require(!n.TryGetProperty("matrix",out _),"matrix nodes outside rigid TRS profile");
        if(n.TryGetProperty("scale",out var scale))Require(Vector(scale)==new Double3(1,1,1),"nonunit scale");
        var p=n.TryGetProperty("translation",out var t)?FromGltf(Vector(t)):default;
        var q=DoubleQuaternion.Identity;
        if(n.TryGetProperty("rotation",out var r)){var v=FromGltf(Vector(r));q=new(v.X,v.Y,v.Z,r[3].GetDouble());}
        Require(p.IsFinite&&q.IsFinite&&Math.Abs(q.LengthSquared-1)<1e-6,"nonrigid transform");
        return new(p,q.Normalized());
    }
    public static PartVisualAsset Load(string path,string identity,string expectedSha256)
    {
        var length=new FileInfo(path).Length;Require(length is >=28 and <=8_388_608,"file capacity");
        var bytes=File.ReadAllBytes(path);var hash=Convert.ToHexStringLower(SHA256.HashData(bytes));Require(hash==expectedSha256,"asset identity/hash mismatch");
        uint U(int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
        Require(U(0)==0x46546c67&&U(4)==2&&U(8)==bytes.Length&&U(16)==0x4e4f534a,"GLB header");
        var jsonLength=checked((int)U(12));Require(jsonLength>0&&jsonLength%4==0&&jsonLength<=bytes.Length-28,"JSON chunk bounds");
        var binHeader=20+jsonLength;var binLength=checked((int)U(binHeader));
        Require(U(binHeader+4)==0x004e4942&&binLength%4==0&&binHeader+8+binLength==bytes.Length,"single BIN chunk");
        var bin=bytes.AsMemory(binHeader+8,binLength);
        using var document=JsonDocument.Parse(bytes.AsMemory(20,jsonLength),new JsonDocumentOptions{MaxDepth=32});var d=document.RootElement;
        Require(d.GetProperty("asset").GetProperty("version").GetString()=="2.0","glTF version");
        foreach(var unsupported in new[]{"extensionsUsed","extensionsRequired","animations","skins","textures","images","cameras"})
            Require(!d.TryGetProperty(unsupported,out var values)||values.GetArrayLength()==0,"unsupported "+unsupported);
        var buffers=d.GetProperty("buffers");Require(buffers.GetArrayLength()==1&&!buffers[0].TryGetProperty("uri",out _),"embedded buffer only");
        Require(buffers[0].GetProperty("byteLength").GetInt32()>0&&buffers[0].GetProperty("byteLength").GetInt32()<=binLength,"buffer length");
        var nodes=d.GetProperty("nodes");Require(nodes.GetArrayLength() is >0 and <=128,"node capacity");
        var meshes=d.GetProperty("meshes");Require(meshes.GetArrayLength() is >0 and <=32,"mesh capacity");
        var scenes=d.GetProperty("scenes");Require(scenes.GetArrayLength()==1&&(!d.TryGetProperty("scene",out var si)||si.GetInt32()==0),"one scene");
        var roots=scenes[0].GetProperty("nodes");Require(roots.GetArrayLength()==1,"one part root");
        Require(nodes[roots[0].GetInt32()].GetProperty("name").GetString()=="ROOT_"+identity.Split('/')[0],"part root identity");
        var seen=new bool[nodes.GetArrayLength()];var seenMeshes=new bool[meshes.GetArrayLength()];var names=new HashSet<string>(StringComparer.Ordinal);
        var result=ImmutableArray.CreateBuilder<PartVisualMesh>();var sockets=ImmutableArray.CreateBuilder<VisualSocket>();Double3 pivot=default;
        void Visit(int index,Pose parent,bool moving,int depth)
        {
            Require(index>=0&&index<seen.Length&&!seen[index]&&depth<=16,"node ownership/cycle");seen[index]=true;
            var node=nodes[index];var name=node.GetProperty("name").GetString()??"";Require(name.Length is >0 and <=128&&names.Add(name),"node identity");
            var pose=parent.Then(NodePose(node));
            if(name=="MODULE_gimbal"){Require(!moving&&pose.Q==DoubleQuaternion.Identity,"one unrotated gimbal module");moving=true;pivot=pose.P;}
            if(name.StartsWith("SOCKET_",StringComparison.Ordinal))sockets.Add(new(name,pose.P,pose.Q.Rotate(FromGltf(new(0,0,-1)))));
            if(node.TryGetProperty("mesh",out var mi))
            {
                var meshIndex=mi.GetInt32();Require(meshIndex>=0&&meshIndex<seenMeshes.Length&&!seenMeshes[meshIndex],"mesh ownership");seenMeshes[meshIndex]=true;
                var primitives=meshes[meshIndex].GetProperty("primitives");Require(primitives.GetArrayLength()==1,"one material per mesh");
                var prim=primitives[0];Require(!prim.TryGetProperty("mode",out var mode)||mode.GetInt32()==4,"triangles only");
                var attributes=prim.GetProperty("attributes");Require(attributes.EnumerateObject().Count()==2,"position/normal profile");
                var positions=Vectors(attributes.GetProperty("POSITION").GetInt32());var normals=Vectors(attributes.GetProperty("NORMAL").GetInt32());
                Require(positions.Length==normals.Length,"normal count");
                var material=d.GetProperty("materials")[prim.GetProperty("material").GetInt32()];
                Require(!material.TryGetProperty("alphaMode",out var am)||am.GetString()=="OPAQUE","opaque material");
                Require(!material.TryGetProperty("doubleSided",out var ds)||!ds.GetBoolean(),"backface culling");
                Require(!material.TryGetProperty("normalTexture",out _)&&!material.TryGetProperty("emissiveTexture",out _),"constant material");
                var pbr=material.GetProperty("pbrMetallicRoughness");Require(!pbr.TryGetProperty("baseColorTexture",out _)&&!pbr.TryGetProperty("metallicRoughnessTexture",out _),"constant PBR");
                var baseColor=pbr.GetProperty("baseColorFactor");var rgb=Vector(baseColor);Require(baseColor[3].GetDouble()==1&&rgb.IsFinite&&rgb.X>=0&&rgb.X<=1&&rgb.Y>=0&&rgb.Y<=1&&rgb.Z>=0&&rgb.Z<=1,"base color");
                var metal=pbr.GetProperty("metallicFactor").GetSingle();var rough=pbr.GetProperty("roughnessFactor").GetSingle();
                Require(float.IsFinite(metal)&&metal is >=0 and <=1&&float.IsFinite(rough)&&rough is >0 and <=1,"PBR coefficients");
                var vertices=new NativeVisualVertex[positions.Length];
                for(var v=0;v<vertices.Length;v++)
                {
                    var position=pose.Point(FromGltf(positions[v]))-(moving?pivot:default);var normal=pose.Q.Rotate(FromGltf(normals[v]));
                    Require(position.IsFinite&&position.LengthSquared<=100&&normal.IsFinite&&Math.Abs(normal.LengthSquared-1)<1e-4,"vertex/normal bounds");
                    vertices[v]=new(){X=(float)position.X,Y=(float)position.Y,Z=(float)position.Z,R=(float)rgb.X,G=(float)rgb.Y,B=(float)rgb.Z,
                        Nx=(float)normal.X,Ny=(float)normal.Y,Nz=(float)normal.Z,Metallic=metal,Roughness=rough};
                }
                var indices=Indices(prim.GetProperty("indices").GetInt32());Require(indices.Length%3==0&&indices.All(i=>i<vertices.Length),"triangle indices");
                result.Add(new(name,moving,vertices,indices));
            }
            if(node.TryGetProperty("children",out var children))foreach(var child in children.EnumerateArray())Visit(child.GetInt32(),pose,moving,depth+1);
        }
        (int Offset,int Stride,int Count,int Component) Accessor(int index,string type,int components)
        {
            var a=d.GetProperty("accessors")[index];Require(!a.TryGetProperty("sparse",out _)&&(!a.TryGetProperty("normalized",out var norm)||!norm.GetBoolean())&&a.GetProperty("type").GetString()==type,"accessor profile");
            var component=a.GetProperty("componentType").GetInt32();var size=component switch{5121=>1,5123=>2,5125 or 5126=>4,_=>throw new InvalidDataException("Component type")};
            var count=a.GetProperty("count").GetInt32();Require(count is >0 and <=262144,"accessor capacity");
            var view=d.GetProperty("bufferViews")[a.GetProperty("bufferView").GetInt32()];Require(view.GetProperty("buffer").GetInt32()==0,"buffer identity");
            var start=view.TryGetProperty("byteOffset",out var vo)?vo.GetInt32():0;var length=view.GetProperty("byteLength").GetInt32();
            var offset=a.TryGetProperty("byteOffset",out var ao)?ao.GetInt32():0;var width=components*size;
            var stride=view.TryGetProperty("byteStride",out var bs)?bs.GetInt32():width;
            Require(start>=0&&length>0&&(long)start+length<=bin.Length&&offset>=0&&stride>=width&&stride<=252&&stride%size==0&&((long)count-1)*stride+offset+width<=length,"accessor bounds");
            return(checked(start+offset),stride,count,component);
        }
        Double3[] Vectors(int index)
        {
            var a=Accessor(index,"VEC3",3);Require(a.Component==5126,"float32 vectors");var values=new Double3[a.Count];
            for(var i=0;i<values.Length;i++){var p=bin.Span.Slice(a.Offset+i*a.Stride,12);values[i]=new(BinaryPrimitives.ReadSingleLittleEndian(p),BinaryPrimitives.ReadSingleLittleEndian(p[4..]),BinaryPrimitives.ReadSingleLittleEndian(p[8..]));}
            return values;
        }
        uint[] Indices(int index)
        {
            var a=Accessor(index,"SCALAR",1);Require(a.Component!=5126,"unsigned indices");var values=new uint[a.Count];
            for(var i=0;i<values.Length;i++){var p=bin.Span[(a.Offset+i*a.Stride)..];values[i]=a.Component switch{5121=>p[0],5123=>BinaryPrimitives.ReadUInt16LittleEndian(p),5125=>BinaryPrimitives.ReadUInt32LittleEndian(p),_=>throw new InvalidDataException()};}
            return values;
        }
        Visit(roots[0].GetInt32(),new(default,DoubleQuaternion.Identity),false,0);
        Require(seen.All(x=>x)&&seenMeshes.All(x=>x),"orphan node/mesh");
        return new(identity,hash,result.ToImmutable(),sockets.ToImmutable(),pivot);
    }
}

/// <summary>One scene-owned cold upload lease. Shared part instances reuse these
/// immutable mesh buffers; native renderer owns and destroys the GPU copies.</summary>
public sealed unsafe class ReusablePartVisuals : IDisposable
{
    public ImmutableArray<PartVisualAsset> Assets {get;}
    public MeshHandle ExhaustMesh {get;}
    private readonly NativeVisualMesh[] uploads;
    private readonly GCHandle[] pins;
    private bool disposed;
    public int UniqueMeshCount=>uploads.Length;
    public long BufferBytes {get;}
    public ReusablePartVisuals(string directory)
    {
        var entries=new[]{("NC_SRV_Capsule_A","6b045a63186ed63fa1c6d48ef7e7ab89299f83b4f533897b8679b22068ceff6a"),
            ("NC_SRV_Tank_A","22cd027892f20596238d7a7580a4e73032071f6303a73cb211915bab4c4de378"),
            ("NC_SRV_Main_A","3b02f9e3788a2ba85ea8c0238e2f61f8c916d7757e2c535b8348cb8e6c1ad46f"),
            ("NC_SRV_Rcs_A","6118cb37bd8b7678d0eacc032391b1143143e2360e351cdcbd67508a3124ca5b")};
        Assets=entries.Select(e=>PartVisualLoader.Load(Path.Combine(directory,e.Item1+".glb"),e.Item1+"/1",e.Item2)).ToImmutableArray();
        var meshes=Assets.SelectMany(a=>a.Meshes).Append(Exhaust()).ToArray();uploads=new NativeVisualMesh[meshes.Length];pins=new GCHandle[meshes.Length*2];
        try
        {
            for(var i=0;i<meshes.Length;i++)
            {
                var m=meshes[i];m.Handle=new((uint)(1024+i));pins[2*i]=GCHandle.Alloc(m.Vertices,GCHandleType.Pinned);pins[2*i+1]=GCHandle.Alloc(m.Indices,GCHandleType.Pinned);
                uploads[i]=new(){Vertices=(NativeVisualVertex*)pins[2*i].AddrOfPinnedObject(),Indices=(uint*)pins[2*i+1].AddrOfPinnedObject(),VertexCount=(uint)m.VertexCount,IndexCount=(uint)m.Indices.Length,PresentationKind=i==meshes.Length-1?1u:0u};
                BufferBytes+=m.VertexCount*48L+m.Indices.Length*4L;
            }
            ExhaustMesh=meshes[^1].Handle;
        }
        catch{Dispose();throw;}
    }
    private static PartVisualMesh Exhaust()
    {
        // Closed unit box only bounds ray integration. Its surfaces are never shaded.
        // Each nozzle has an independent transform; all instances share this immutable proxy.
        var v=new NativeVisualVertex[8];
        for(var i=0;i<8;i++)v[i]=new(){X=(i&1),Y=(i&2)==0?-1:1,Z=(i&4)==0?-1:1,Nx=1,R=1,G=1,B=1,Roughness=1};
        uint[] ix=[0,4,6,0,6,2,1,3,7,1,7,5,0,1,5,0,5,4,2,6,7,2,7,3,0,2,3,0,3,1,4,5,7,4,7,6];
        return new("realized-exhaust-volume",false,v,ix);
    }
    public PartVisualAsset Resolve(string identity)
    {ObjectDisposedException.ThrowIf(disposed,this);return Assets.Single(a=>a.Identity==identity);}
    public NativeResult Run(NativeFrameSubmission* submission,NativeRuntime.HostCallback callback,IntPtr userData,uint preparedObjectCapacity)
    {ObjectDisposedException.ThrowIf(disposed,this);fixed(NativeVisualMesh* p=uploads)return NativeRuntime.RunRendererWithVisualMeshes(submission,callback,userData,p,(uint)uploads.Length,preparedObjectCapacity);}
    public void Dispose(){if(disposed)return;disposed=true;foreach(var p in pins)if(p.IsAllocated)p.Free();}
}
