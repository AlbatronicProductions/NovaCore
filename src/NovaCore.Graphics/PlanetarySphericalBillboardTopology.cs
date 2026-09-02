using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace NovaCore.Graphics;

/// <summary>Immutable, pre-generated spherical billboard topology.  It has no terrain or camera authority.</summary>
public sealed class PlanetarySphericalBillboardTopology
{
    public const uint FormatVersion=1, GeneratorVersion=1;
    public const uint Magic=0x42544F4Eu; // NOTB, little-endian "NCOT" on disk.
    public readonly record struct Vertex(Vector3 Position,byte DensityRegion,float AverageAngularSpacingRadians);
    public readonly record struct RegionMetadata(byte Region,float RepresentativeAngularSpacingRadians,float MaximumAngularEdgeRadians,float AverageAngularEdgeRadians,float ConservativeAngularSpacingRadians,int VertexCount,int TriangleCount);
    public readonly record struct SnapMetadata(float TangentStepRadians,int PupilRegion,ulong Identity);

    internal PlanetarySphericalBillboardTopology(byte level,Vertex[] vertices,uint[] indices,int[] neighborOffsets,int[] neighbors,
        RegionMetadata[] regions,int[] previousLevelVertexMap,SnapMetadata snap,ulong hash)
    {
        Level=level; Vertices=Array.AsReadOnly(vertices.ToArray()); Indices=Array.AsReadOnly(indices.ToArray()); NeighborOffsets=Array.AsReadOnly(neighborOffsets.ToArray()); Neighbors=Array.AsReadOnly(neighbors.ToArray());
        Regions=Array.AsReadOnly(regions.ToArray()); PreviousLevelVertexMap=Array.AsReadOnly(previousLevelVertexMap.ToArray()); Snap=snap; TopologyHash=hash;
    }
    public byte Level { get; }
    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<uint> Indices { get; }
    public IReadOnlyList<int> NeighborOffsets { get; }
    public IReadOnlyList<int> Neighbors { get; }
    public IReadOnlyList<RegionMetadata> Regions { get; }
    /// <summary>For level zero this is empty; otherwise one exact finer index per prior-level vertex.</summary>
    public IReadOnlyList<int> PreviousLevelVertexMap { get; }
    public SnapMetadata Snap { get; }
    public ulong TopologyHash { get; }

    public static PlanetarySphericalBillboardTopology Load(ReadOnlySpan<byte> bytes)
    {
        const int headerBytes=80;
        if(bytes.Length<headerBytes) throw new InvalidDataException("Topology artifact is truncated.");
        var magic=BinaryPrimitives.ReadUInt32LittleEndian(bytes); var version=BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]);
        if(magic!=Magic||version!=FormatVersion) throw new InvalidDataException($"Topology artifact magic or version is invalid: magic=0x{magic:X8}; version={version}.");
        var generator=BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]); if(generator!=GeneratorVersion) throw new InvalidDataException("Unsupported topology generator version.");
        var level=bytes[12]; var vertexCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]));
        var indexCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..])); var neighborOffsetCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]));
        var neighborCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..])); var regionCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[32..]));
        var mapCount=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[36..])); var expectedHash=BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]);
        var vertexOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..])); var indexOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]));
        var adjacencyOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[56..])); var regionOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[60..]));
        var mapOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[64..])); var snapOffset=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[68..]));
        const int vertexBytes=20,regionBytes=24,snapBytes=16;
        var required=checked(snapOffset+snapBytes);
        if(vertexOffset!=headerBytes||indexOffset!=checked(vertexOffset+vertexCount*vertexBytes)||adjacencyOffset!=checked(indexOffset+indexCount*4)||
            regionOffset!=checked(adjacencyOffset+(neighborOffsetCount+neighborCount)*4)||mapOffset!=checked(regionOffset+regionCount*regionBytes)||
            snapOffset!=checked(mapOffset+mapCount*4)||required!=bytes.Length||indexCount%3!=0||neighborOffsetCount!=vertexCount+1)
            throw new InvalidDataException("Topology artifact lengths or offsets are invalid.");
        var vertices=new Vertex[vertexCount]; for(var i=0;i<vertexCount;i++){var p=vertexOffset+i*vertexBytes;vertices[i]=new(new(BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[p..])),BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+4)..])),BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+8)..]))),bytes[p+12],BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+16)..])));}
        var indices=new uint[indexCount]; for(var i=0;i<indexCount;i++)indices[i]=BinaryPrimitives.ReadUInt32LittleEndian(bytes[(indexOffset+i*4)..]);
        var offsets=new int[neighborOffsetCount];for(var i=0;i<offsets.Length;i++)offsets[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(adjacencyOffset+i*4)..]);
        var neighbors=new int[neighborCount];var neighborStart=adjacencyOffset+neighborOffsetCount*4;for(var i=0;i<neighbors.Length;i++)neighbors[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(neighborStart+i*4)..]);
        var regions=new RegionMetadata[regionCount];for(var i=0;i<regionCount;i++){var p=regionOffset+i*regionBytes;regions[i]=new(bytes[p],BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+4)..])),BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+8)..])),BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+12)..])),BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+16)..])),BinaryPrimitives.ReadInt32LittleEndian(bytes[(p+20)..]),0);}
        // Triangle counts are reconstructed from immutable indices, not trusted from external input.
        for(var i=0;i<regions.Length;i++)regions[i]=regions[i] with { TriangleCount=indices.Chunk(3).Count(t=>vertices[(int)t[0]].DensityRegion==i||vertices[(int)t[1]].DensityRegion==i||vertices[(int)t[2]].DensityRegion==i) };
        var map=new int[mapCount];for(var i=0;i<map.Length;i++)map[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(mapOffset+i*4)..]);
        var snap=new SnapMetadata(BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[snapOffset..])),BinaryPrimitives.ReadInt32LittleEndian(bytes[(snapOffset+4)..]),BinaryPrimitives.ReadUInt64LittleEndian(bytes[(snapOffset+8)..]));
        var topology=new PlanetarySphericalBillboardTopology(level,vertices,indices,offsets,neighbors,regions,map,snap,expectedHash);
        ValidateStructure(topology); if(ComputeHash(bytes)!=expectedHash) throw new InvalidDataException("Topology artifact hash is invalid.");
        return topology;
    }

    public static byte[] Serialize(PlanetarySphericalBillboardTopology value)
    {
        ValidateStructure(value);const int headerBytes=80,vertexBytes=20,regionBytes=24,snapBytes=16;
        var vertexOffset=headerBytes;var indexOffset=checked(vertexOffset+value.Vertices.Count*vertexBytes);var adjacencyOffset=checked(indexOffset+value.Indices.Count*4);
        var regionOffset=checked(adjacencyOffset+(value.NeighborOffsets.Count+value.Neighbors.Count)*4);var mapOffset=checked(regionOffset+value.Regions.Count*regionBytes);var snapOffset=checked(mapOffset+value.PreviousLevelVertexMap.Count*4);
        var bytes=new byte[checked(snapOffset+snapBytes)];var output=bytes.AsSpan();BinaryPrimitives.WriteUInt32LittleEndian(output,Magic);output[4]=(byte)FormatVersion;output[8]=(byte)GeneratorVersion;output[12]=value.Level;
        BinaryPrimitives.WriteUInt32LittleEndian(output[16..],(uint)value.Vertices.Count);BinaryPrimitives.WriteUInt32LittleEndian(output[20..],(uint)value.Indices.Count);BinaryPrimitives.WriteUInt32LittleEndian(output[24..],(uint)value.NeighborOffsets.Count);BinaryPrimitives.WriteUInt32LittleEndian(output[28..],(uint)value.Neighbors.Count);BinaryPrimitives.WriteUInt32LittleEndian(output[32..],(uint)value.Regions.Count);BinaryPrimitives.WriteUInt32LittleEndian(output[36..],(uint)value.PreviousLevelVertexMap.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(output[48..],(uint)vertexOffset);BinaryPrimitives.WriteUInt32LittleEndian(output[52..],(uint)indexOffset);BinaryPrimitives.WriteUInt32LittleEndian(output[56..],(uint)adjacencyOffset);BinaryPrimitives.WriteUInt32LittleEndian(output[60..],(uint)regionOffset);BinaryPrimitives.WriteUInt32LittleEndian(output[64..],(uint)mapOffset);BinaryPrimitives.WriteUInt32LittleEndian(output[68..],(uint)snapOffset);
        for(var i=0;i<value.Vertices.Count;i++){var p=vertexOffset+i*vertexBytes;var v=value.Vertices[i];BinaryPrimitives.WriteInt32LittleEndian(output[p..],BitConverter.SingleToInt32Bits(v.Position.X));BinaryPrimitives.WriteInt32LittleEndian(output[(p+4)..],BitConverter.SingleToInt32Bits(v.Position.Y));BinaryPrimitives.WriteInt32LittleEndian(output[(p+8)..],BitConverter.SingleToInt32Bits(v.Position.Z));output[p+12]=v.DensityRegion;BinaryPrimitives.WriteInt32LittleEndian(output[(p+16)..],BitConverter.SingleToInt32Bits(v.AverageAngularSpacingRadians));}
        for(var i=0;i<value.Indices.Count;i++)BinaryPrimitives.WriteUInt32LittleEndian(output[(indexOffset+i*4)..],value.Indices[i]);for(var i=0;i<value.NeighborOffsets.Count;i++)BinaryPrimitives.WriteInt32LittleEndian(output[(adjacencyOffset+i*4)..],value.NeighborOffsets[i]);var n=adjacencyOffset+value.NeighborOffsets.Count*4;for(var i=0;i<value.Neighbors.Count;i++)BinaryPrimitives.WriteInt32LittleEndian(output[(n+i*4)..],value.Neighbors[i]);
        for(var i=0;i<value.Regions.Count;i++){var p=regionOffset+i*regionBytes;var r=value.Regions[i];output[p]=r.Region;BinaryPrimitives.WriteInt32LittleEndian(output[(p+4)..],BitConverter.SingleToInt32Bits(r.RepresentativeAngularSpacingRadians));BinaryPrimitives.WriteInt32LittleEndian(output[(p+8)..],BitConverter.SingleToInt32Bits(r.MaximumAngularEdgeRadians));BinaryPrimitives.WriteInt32LittleEndian(output[(p+12)..],BitConverter.SingleToInt32Bits(r.AverageAngularEdgeRadians));BinaryPrimitives.WriteInt32LittleEndian(output[(p+16)..],BitConverter.SingleToInt32Bits(r.ConservativeAngularSpacingRadians));BinaryPrimitives.WriteInt32LittleEndian(output[(p+20)..],r.VertexCount);}
        for(var i=0;i<value.PreviousLevelVertexMap.Count;i++)BinaryPrimitives.WriteInt32LittleEndian(output[(mapOffset+i*4)..],value.PreviousLevelVertexMap[i]);BinaryPrimitives.WriteInt32LittleEndian(output[snapOffset..],BitConverter.SingleToInt32Bits(value.Snap.TangentStepRadians));BinaryPrimitives.WriteInt32LittleEndian(output[(snapOffset+4)..],value.Snap.PupilRegion);BinaryPrimitives.WriteUInt64LittleEndian(output[(snapOffset+8)..],value.Snap.Identity);
        BinaryPrimitives.WriteUInt64LittleEndian(output[40..],ComputeHash(bytes));return bytes;
    }
    private static ulong ComputeHash(ReadOnlySpan<byte> bytes){using var sha=SHA256.Create();var copy=bytes.ToArray();copy.AsSpan(40,8).Clear();return BinaryPrimitives.ReadUInt64LittleEndian(sha.ComputeHash(copy));}
    private static void ValidateStructure(PlanetarySphericalBillboardTopology value)
    { if(value.Vertices.Count==0||value.Indices.Count==0||value.Indices.Count%3!=0||value.NeighborOffsets.Count!=value.Vertices.Count+1||value.NeighborOffsets[0]!=0||value.NeighborOffsets[^1]!=value.Neighbors.Count||value.Regions.Count==0)throw new InvalidDataException("Invalid immutable topology structure."); for(var i=0;i<value.Indices.Count;i++)if(value.Indices[i]>=value.Vertices.Count)throw new InvalidDataException("Invalid topology index."); for(var i=0;i<value.NeighborOffsets.Count-1;i++)if(value.NeighborOffsets[i]>value.NeighborOffsets[i+1])throw new InvalidDataException("Invalid adjacency offsets."); }
}
