namespace NovaCore.Graphics;

/// <summary>Shared 16x16 patch-local indexed grid. This topology has no body or world coordinates.</summary>
public sealed class PlanetaryPatchTopology
{
    public const int QuadsPerSide=16;
    public readonly record struct Vertex(float U,float V);
    private PlanetaryPatchTopology(Vertex[] vertices,uint[] indices,ulong hash){Vertices=vertices;Indices=indices;DeterministicHash=hash;}
    public Vertex[] Vertices{get;} public uint[] Indices{get;} public ulong DeterministicHash{get;}
    public static PlanetaryPatchTopology Shared{get;}=Create();
    static PlanetaryPatchTopology Create(){var vertices=new Vertex[(QuadsPerSide+1)*(QuadsPerSide+1)];var indices=new uint[QuadsPerSide*QuadsPerSide*6];var vertex=0;for(var y=0;y<=QuadsPerSide;y++)for(var x=0;x<=QuadsPerSide;x++)vertices[vertex++]=new((float)x/QuadsPerSide,(float)y/QuadsPerSide);var index=0;for(uint y=0;y<QuadsPerSide;y++)for(uint x=0;x<QuadsPerSide;x++){var a=y*(QuadsPerSide+1)+x;var b=a+1;var c=a+QuadsPerSide+1;indices[index++]=a;indices[index++]=b;indices[index++]=c;indices[index++]=b;indices[index++]=c+1;indices[index++]=c;}ulong hash=14695981039346656037;foreach(var value in vertices){hash=(hash^(uint)BitConverter.SingleToInt32Bits(value.U))*1099511628211;hash=(hash^(uint)BitConverter.SingleToInt32Bits(value.V))*1099511628211;}foreach(var value in indices)hash=(hash^value)*1099511628211;return new(vertices,indices,hash);}
}
