using System.Diagnostics;
using System.Security.Cryptography;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceGeometry()
    {
        var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets);
        using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
        var site=cold.Binding.Physical!.Site;var config=LocalContactConfiguration.CreateCraft(cold.Binding);
        var region=NovaCore.Core.Surface.FloridaFacilitySupport.Region;
        var frame=new NovaCore.Core.Surface.PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        var okay=((NovaCore.Graphics.PlanetaryPhysicalSurfacePointQuery)terrain.Query).TryCollisionTriangle(frame,new(76,-4),new(76.015625,-4),new(76.015625,-3.984375),out var exactError,out var why);
        Console.WriteLine($"GEOMETRY_NUMERICAL_PROBE ready={okay} bound={exactError:R} reason={why} angleBudget={NovaCore.Graphics.PhysicalCollisionAngles.MaximumAdmittedError:R}");
        foreach(var p in new[]{new NovaCore.Core.Surface.PhysicalPatchCoordinate(84.0625,-8),new(80,-4),new(83.984375,-4)})
        {
            var ready=((NovaCore.Graphics.PlanetaryPhysicalSurfacePointQuery)terrain.Query).TryCollisionTriangle(frame,p,new(p.X+.015625,p.Y),new(p.X+.015625,p.Y+.015625),out var e,out var reason);
            Console.WriteLine($"GEOMETRY_EDGE_PROBE x={p.X:R} y={p.Y:R} ready={ready} bound={e:R} reason={reason}");
        }
        foreach(var tile in new[]{new CraftTerrainGeometry.Tile(0,0),new(19,-1),new(20,0),new(19,-1),new(20,0),new(19,-1),new(20,0),new(19,-1),new(20,0)})
        {
            var before=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.GetTimestamp();
            var result=CraftTerrainGeometry.Prepare(site,config.OriginRoot,tile,config.ContactTolerance,(phase,ms,n)=>Console.WriteLine($"GEOMETRY_PHASE name={phase} ms={ms:R} count={n}"));
            var milliseconds=Stopwatch.GetElapsedTime(timer).TotalMilliseconds;var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            using var bytes=new MemoryStream();using(var writer=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true))
            {
                writer.Write(result.MaximumError);writer.Write(result.MaximumConversionError);
                foreach(var t in result.Triangles)foreach(var v in new[]{t.A,t.B,t.C}){writer.Write(v.X);writer.Write(v.Y);writer.Write(v.Z);}
            }
            Console.WriteLine($"GEOMETRY_TILE x={tile.X} y={tile.Y} triangles={result.Triangles.Length} bound={result.MaximumError:R} conversion={result.MaximumConversionError:R} sha256={Convert.ToHexString(SHA256.HashData(bytes.ToArray()))} ms={milliseconds:R} allocated={allocated}");
        }
    }
}
