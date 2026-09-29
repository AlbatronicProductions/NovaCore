using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>One coherent finite mesh per native world. Replacements preserve
/// exact terrain samples; the private solver cache is never canonical state.</summary>
internal sealed class CraftTerrainColliders(BepuPhysics.Simulation simulation,BufferPool pool,BodyHandle body,TypedIndex bodyShape,CompiledCraft craft,
    AssemblyFloridaSite site,Double3 origin,double tolerance,CraftTerrainContacts contacts):IDisposable
{
    private Dictionary<CraftTerrainGeometry.Tile,CraftTerrainGeometry.Prepared> tiles=new();
    private bool exists;
    private StaticHandle surface;
    private TypedIndex shape;
    private int tileMinX,tileMaxX,tileMinY,tileMaxY;
    internal int ReuseProofs {get;private set;}
    internal int FullFootprints {get;private set;}
    internal int TriangleCount {get;private set;}
    internal int Generation {get;private set;}
    internal int TileCount=>tiles.Count;
    internal void VerifyImportedSurface(Double3 center,DoubleQuaternion orientation,AssemblyMass mass)
    {
        // A wholly buried body need not intersect any one-sided mesh triangle.
        // Empty native manifolds therefore cannot admit a physical checkpoint.
        // Every admitted convex child must retain a resolved vertex on/outside
        // the one-sided ground skin. Partial compression uses ordinary capped
        // BEPU recovery; raw triangle depth is not a burial/strength metric.
        // This conservative checkpoint-coverage test never seats the endpoint.
        var region=FloridaFacilitySupport.Region;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        foreach(var hull in craft.Collision)
        {
          var highest=double.NegativeInfinity;var insideSlab=site.Slab is not null;
          foreach(var vertex in hull.Vertices)
          {
            var local=center+orientation.Rotate(vertex-mass.Com);
            var point=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(local);
            var up=Double3.Dot(point,region.Up);
            if(!double.IsFinite(up)||up<=region.RadiusMetres*.5)throw new InvalidDataException("Restored hull leaves the physical terrain patch.");
            var ground=site.CollisionSource.CollisionPoint(frame,new(region.RadiusMetres*Double3.Dot(point,region.East)/up,region.RadiusMetres*Double3.Dot(point,region.North)/up));
            var clearance=Math.Sqrt(point.LengthSquared)-Math.Sqrt(ground.LengthSquared);
            if(!double.IsFinite(clearance))throw new InvalidDataException("Restored hull has nonfinite terrain clearance.");
            highest=Math.Max(highest,clearance);
            if(site.Slab is {} slab)insideSlab&=Math.Abs(local.X)<slab.Dimensions.X*.5-tolerance&&Math.Abs(local.Z)<slab.Dimensions.Z*.5-tolerance&&
                local.Y<site.SupportPlane-tolerance&&local.Y>site.SupportPlane-slab.Dimensions.Y+tolerance;
          }
          if(highest< -tolerance||insideSlab)throw new InvalidDataException("Restored convex child has no resolved exterior surface coverage.");
        }
    }
    internal void Ensure(Double3 center,DoubleQuaternion orientation,AssemblyMass mass,double sweep)
    {
        if(!site.Applicable||!double.IsFinite(sweep)||sweep<0)throw new InvalidDataException("Stale terrain or swept hull.");
        try
        {
            var union=CraftTerrainFootprint.Union(simulation,body,bodyShape,site,origin,sweep);
            if(union.AboveGrade||exists&&Covered(union,tileMinX,tileMaxX,tileMinY,tileMaxY)){ReuseProofs++;return;}
        }
        catch(InvalidDataException){ } // A failed cheap proof is only a miss.
        FullFootprints++;
        var footprint=CraftTerrainFootprint.Compute(simulation,body,bodyShape,site,origin,sweep);
        var x0=footprint.MinX;var x1=footprint.MaxX;var y0=footprint.MinY;var y1=footprint.MaxY;
        if(footprint.AboveGrade)return;
        if(!double.IsFinite(x0+x1+y0+y1)||Math.Max(Math.Max(Math.Abs(x0),Math.Abs(x1)),Math.Max(Math.Abs(y0),Math.Abs(y1)))>PhysicalCollisionFrame.MaximumCoordinate)
            throw new InvalidDataException("Terrain footprint exceeds the local FP32 patch domain.");
        var minX=(int)Math.Floor(x0/CraftTerrainGeometry.TileSize);var maxX=(int)Math.Floor(x1/CraftTerrainGeometry.TileSize);
        var minY=(int)Math.Floor(y0/CraftTerrainGeometry.TileSize);var maxY=(int)Math.Floor(y1/CraftTerrainGeometry.TileSize);
        if((long)(maxX-minX+1)*(maxY-minY+1)>1024)throw new InvalidDataException("Finite terrain preparation capacity exceeded.");
        var covered=true;
        for(var x=minX;x<=maxX;x++)for(var y=minY;y<=maxY;y++)covered&=tiles.ContainsKey(new(x,y));
        if(covered)return;
        var prepared=new Dictionary<CraftTerrainGeometry.Tile,CraftTerrainGeometry.Prepared>();var total=0;
        for(var x=minX;x<=maxX;x++)for(var y=minY;y<=maxY;y++)
        {
            var key=new CraftTerrainGeometry.Tile(x,y);
            if(!tiles.TryGetValue(key,out var tile))tile=site.TerrainPreparation.Get(origin,key,tolerance);
            total=checked(total+tile.Triangles.Length);if(total>4_194_304)throw new InvalidDataException("Finite terrain triangle capacity exceeded.");
            prepared.Add(key,tile);
        }
        pool.Take<Triangle>(total,out var buffer);var index=0;
        foreach(var item in prepared.OrderBy(x=>x.Key))foreach(var triangle in item.Value.Triangles)buffer[index++]=triangle;
        var mesh=new Mesh(buffer,Vector3.One,pool);
        var newShape=simulation.Shapes.Add(mesh);StaticHandle next;
        try{next=simulation.Statics.Add(new StaticDescription(Vector3.Zero,newShape));}
        catch{simulation.Shapes.RemoveAndDispose(newShape,pool);throw;}
        // Allocate the successor before removing the prior surface so its
        // native handle cannot reuse the still-published pair identity.
        var patchUp=site.LocalToBodyFixed.Conjugate().Rotate(FloridaFacilitySupport.Region.Up).Normalized();
        contacts.Register(next,total,new((float)patchUp.X,(float)patchUp.Y,(float)patchUp.Z));
        if(exists){simulation.Statics.Remove(surface);simulation.Shapes.RemoveAndDispose(shape,pool);}
        if(ConstructionWorkProbe.Current is {} probe)probe.Meshes++;
        shape=newShape;surface=next;exists=true;tiles=prepared;TriangleCount=total;Generation++;
        tileMinX=minX;tileMaxX=maxX;tileMinY=minY;tileMaxY=maxY;
    }
    internal static bool Covered(CraftTerrainFootprint.Bounds b,int minX,int maxX,int minY,int maxY)
    {
        if(!double.IsFinite(b.MinX+b.MinY+b.MaxX+b.MaxY)||b.MinX>b.MaxX||b.MinY>b.MaxY||minX>maxX||minY>maxY)return false;
        if(Math.Max(Math.Max(Math.Abs(b.MinX),Math.Abs(b.MaxX)),Math.Max(Math.Abs(b.MinY),Math.Abs(b.MaxY)))>PhysicalCollisionFrame.MaximumCoordinate)return false;
        // Closed query bounds: equality at the high tile edge needs its successor.
        return Math.Floor(b.MinX/CraftTerrainGeometry.TileSize)>=minX&&Math.Floor(b.MaxX/CraftTerrainGeometry.TileSize)<=maxX&&
            Math.Floor(b.MinY/CraftTerrainGeometry.TileSize)>=minY&&Math.Floor(b.MaxY/CraftTerrainGeometry.TileSize)<=maxY;
    }
    public void Dispose()
    {
        if(exists){simulation.Statics.Remove(surface);simulation.Shapes.RemoveAndDispose(shape,pool);exists=false;}
        tiles.Clear();TriangleCount=0;
    }
}
