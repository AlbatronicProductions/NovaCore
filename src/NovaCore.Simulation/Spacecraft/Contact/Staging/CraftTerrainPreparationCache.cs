using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Bounded immutable geometry owned by one acquired physical site,
/// independent of any private solver world. No native handles or receipts.</summary>
internal sealed class CraftTerrainPreparationCache
{
    internal const int MaximumTiles=64;
    internal const long MaximumTriangleBytes=8*1024*1024;
    private readonly AssemblyFloridaSite site;
    private readonly int maximumTiles;
    private readonly long maximumTriangleBytes;
    private readonly record struct Key(CraftTerrainGeometry.Tile Tile,long X,long Y,long Z,long Tolerance);
    private sealed record Entry(Key Key,CraftTerrainGeometry.Prepared Value);
    private readonly Dictionary<Key,LinkedListNode<Entry>> entries=new();
    private readonly LinkedList<Entry> recent=new();
    private readonly object gate=new();
    private IPhysicalSurfaceCollisionSource? source;
    internal long RetainedTriangleBytes {get;private set;}
    internal int Count=>entries.Count;
    internal long Hits {get;private set;}
    internal long Misses {get;private set;}

    internal CraftTerrainPreparationCache(AssemblyFloridaSite site,int maximumTiles=MaximumTiles,long maximumTriangleBytes=MaximumTriangleBytes)
    {
        if(maximumTiles is <1 or >MaximumTiles||maximumTriangleBytes is <1 or >MaximumTriangleBytes)throw new ArgumentOutOfRangeException(nameof(maximumTiles));
        this.site=site;this.maximumTiles=maximumTiles;this.maximumTriangleBytes=maximumTriangleBytes;
    }

    internal CraftTerrainGeometry.Prepared Get(Double3 origin,CraftTerrainGeometry.Tile tile,double tolerance)
    {
        lock(gate)
        {
            // Authority checks also run on hits. The acquired production H data
            // publish once and do not mutate/unload. A new site/source cannot
            // borrow this cache, even if it presents the same public digest.
            var current=site.CollisionSource;
            if(source is not null&&!ReferenceEquals(source,current))throw new InvalidDataException("Physical terrain cache source changed.");
            source=current;
            if(!origin.IsFinite||!double.IsFinite(tolerance)||tolerance<=0)throw new InvalidDataException("Invalid terrain cache precision domain.");
            var key=new Key(tile,BitConverter.DoubleToInt64Bits(origin.X),BitConverter.DoubleToInt64Bits(origin.Y),
                BitConverter.DoubleToInt64Bits(origin.Z),BitConverter.DoubleToInt64Bits(tolerance));
            if(entries.TryGetValue(key,out var node))
            {
                recent.Remove(node);recent.AddLast(node);Hits++;return node.Value.Value;
            }
            Misses++;
            // A failure publishes nothing; the original full-H certification,
            // triangle ordering and FP64 -> FP32 conversion run unchanged.
            var prepared=CraftTerrainGeometry.Prepare(site,origin,tile,tolerance);
            var bytes=prepared.TriangleBytes;
            if(bytes>maximumTriangleBytes)return prepared; // Capacity affects reuse only.
            while(entries.Count>=maximumTiles||RetainedTriangleBytes+bytes>maximumTriangleBytes)
            {
                var oldest=recent.First!;recent.RemoveFirst();entries.Remove(oldest.Value.Key);
                RetainedTriangleBytes-=oldest.Value.Value.TriangleBytes;
            }
            entries.Add(key,recent.AddLast(new Entry(key,prepared)));RetainedTriangleBytes+=bytes;
            return prepared;
        }
    }
}
