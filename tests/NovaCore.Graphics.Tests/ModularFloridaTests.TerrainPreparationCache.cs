using System.Security.Cryptography;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ModularFloridaTests
{
    internal static void TerrainPreparationCache()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        var source=new CacheProofSource(terrain.Query);
        using var session=ConstructionApplicationSession.CreateSupported(craft,source,terrain.Slab);
        var site=session.Binding.Physical!.Site;var config=LocalContactConfiguration.CreateCraft(session.Binding);
        var cache=site.TerrainPreparation;var saved=session.Save();
        string Hash(CraftTerrainGeometry.Prepared value)
        {
            using var bytes=new MemoryStream();using(var writer=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true))
            {
                writer.Write(value.MaximumError);writer.Write(value.MaximumConversionError);
                foreach(var t in value.Triangles)foreach(var p in new[]{t.A,t.B,t.C}){writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);}
            }
            return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
        }
        foreach(var key in new[]{new CraftTerrainGeometry.Tile(0,0),new(19,-1),new(20,0),new(-1,-1)})
        {
            var fresh=CraftTerrainGeometry.Prepare(site,config.OriginRoot,key,config.ContactTolerance);
            var first=cache.Get(config.OriginRoot,key,config.ContactTolerance);var calls=source.Preparations;
            var again=cache.Get(config.OriginRoot,key,config.ContactTolerance);
            Need(Hash(fresh)==Hash(first)&&Hash(first)==Hash(again),"fresh/cached all triangle bits and both certificate bounds identical");
            Need(ReferenceEquals(first,again)&&source.Preparations==calls,"hit reuses completed geometry without re-evaluating H");
        }
        var tile=new CraftTerrainGeometry.Tile(0,0);var priorMisses=cache.Misses;
        foreach(var origin in new[]{new Double3(-0d,config.OriginRoot.Y,0),config.OriginRoot+new Double3(.25,0,0),config.OriginRoot+new Double3(0,0,.25)})
        {
            var value=cache.Get(origin,tile,config.ContactTolerance);
            Need(Hash(value)==Hash(CraftTerrainGeometry.Prepare(site,origin,tile,config.ContactTolerance)),"exact origin preserves original FP64 conversion");
        }
        Need(cache.Misses==priorMisses+3,"bitwise signed zero and shifted origin cannot alias");
        var finer=cache.Get(config.OriginRoot,tile,config.ContactTolerance/2);
        Need(Hash(finer)==Hash(CraftTerrainGeometry.Prepare(site,config.OriginRoot,tile,config.ContactTolerance/2)),"different tolerance gets its own full certificate");
        priorMisses=cache.Misses;
        cache.Get(new(Math.BitIncrement(config.OriginRoot.X),config.OriginRoot.Y,config.OriginRoot.Z),tile,config.ContactTolerance);
        cache.Get(config.OriginRoot,tile,Math.BitIncrement(config.ContactTolerance));
        Need(cache.Misses==priorMisses+2,"one-bit origin/tolerance changes cannot alias");

        var bounded=new CraftTerrainPreparationCache(site,2);var kept=bounded.Get(config.OriginRoot,tile,config.ContactTolerance);var keptHash=Hash(kept);
        bounded.Get(config.OriginRoot,new(1,0),config.ContactTolerance);
        bounded.Get(config.OriginRoot,tile,config.ContactTolerance); // Touch retained active value.
        bounded.Get(config.OriginRoot,new(2,0),config.ContactTolerance);
        Need(bounded.Count==2&&ReferenceEquals(kept,bounded.Get(config.OriginRoot,tile,config.ContactTolerance)),"bounded LRU retains the touched entry");
        var misses=bounded.Misses;bounded.Get(config.OriginRoot,new(1,0),config.ContactTolerance);
        Need(bounded.Misses==misses+1&&bounded.Count==2,"evicted tile rebuilds on demand");
        bounded.Get(config.OriginRoot,new(3,0),config.ContactTolerance);
        Need(Hash(kept)==keptHash,"eviction does not mutate geometry leased by an active world");
        var bytesLimited=new CraftTerrainPreparationCache(site,64,kept.TriangleBytes);
        bytesLimited.Get(config.OriginRoot,tile,config.ContactTolerance);bytesLimited.Get(config.OriginRoot,new(1,0),config.ContactTolerance);
        Need(bytesLimited.Count==1&&bytesLimited.RetainedTriangleBytes<=kept.TriangleBytes,"triangle payload budget independently bounds residency");
        var oversized=new CraftTerrainPreparationCache(site,64,1);
        Need(Hash(oversized.Get(config.OriginRoot,tile,config.ContactTolerance))==keptHash&&oversized.Count==0&&oversized.RetainedTriangleBytes==0,"oversized successful result remains usable without retention or quality reduction");

        source.FailPreparation=true;var count=cache.Count;var retained=cache.RetainedTriangleBytes;
        Refuse(()=>cache.Get(config.OriginRoot,new(5,0),config.ContactTolerance),"failed preparation does not publish");
        Need(cache.Count==count&&cache.RetainedTriangleBytes==retained,"failure leaves prior complete entries intact");source.FailPreparation=false;
        var sourceCalls=source.Preparations;cache.Get(config.OriginRoot,new(5,0),config.ContactTolerance);
        Need(source.Preparations>sourceCalls,"a failed miss must really prepare on retry");
        source.Changed=true;
        Refuse(()=>cache.Get(config.OriginRoot,tile,config.ContactTolerance),"stale physical generation refuses even an existing hit");source.Changed=false;
        Refuse(()=>cache.Get(new(double.NaN,0,0),tile,config.ContactTolerance),"nonfinite origin refuses");
        Refuse(()=>cache.Get(config.OriginRoot,tile,0),"invalid tolerance refuses");
        using var foreign=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
        Need(!ReferenceEquals(foreign.Binding.Physical!.Site.TerrainPreparation,cache)&&foreign.Binding.Physical.Site.TerrainPreparation.Count==0,"same public authority in a new site does not inherit a cache");
        Need(session.Save().SequenceEqual(saved),"cache activity cannot change canonical physical state/history");
        Console.WriteLine($"TERRAIN_PREPARATION_CACHE_PASS checks={checks} hits={cache.Hits} misses={cache.Misses} retainedTiles={cache.Count} retainedTriangleBytes={cache.RetainedTriangleBytes}");
    }

    private sealed class CacheProofSource(IPhysicalSurfacePointQuery query):IPhysicalGradingProofSource,IPhysicalSurfaceCollisionSource
    {
        internal bool Changed,FailPreparation;
        internal int Preparations;
        public PhysicalSurfaceAuthorityIdentity Authority=>Changed?query.Authority with {QueryPolicyVersion=query.Authority.QueryPolicyVersion+1}:query.Authority;
        public FacilitySupportRegion GradingRegion=>((IPhysicalGradingProofSource)query).GradingRegion;
        public bool IsNumericalFullWeight(in Double3 direction)=>((IPhysicalGradingProofSource)query).IsNumericalFullWeight(direction);
        public PhysicalSurfacePointResult Query(ulong body,in Double3 direction)=>query.Query(body,direction);
        public Double3 CollisionPoint(PhysicalCollisionFrame frame,PhysicalPatchCoordinate point)=>((IPhysicalSurfaceCollisionSource)query).CollisionPoint(frame,point);
        public bool TryTriangleError(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double error)=>((IPhysicalSurfaceCollisionSource)query).TryTriangleError(frame,a,b,c,out error);
        public bool TryPreparePatch(PhysicalCollisionFrame frame,PhysicalPatchCoordinate minimum,PhysicalPatchCoordinate maximum,out IPhysicalCollisionPatch? patch)
        {
            Preparations++;if(FailPreparation)throw new InvalidDataException("Injected preparation refusal.");
            return ((IPhysicalSurfaceCollisionSource)query).TryPreparePatch(frame,minimum,maximum,out patch);
        }
    }
}
