using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

var root = args.Length == 1 ? args[0] : throw new ArgumentException("repository root required");
if (!EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error)) throw new Exception(error);
if (!TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error)) throw new Exception(error);
if (!EarthLocalTerrainElevationDataset.TryLoad(path,out error)) throw new Exception(error);
if (PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)!=PhysicalSurfaceQueryStatus.Ready) throw new Exception("not ready");
if (!query!.Query(6,FloridaFacilitySupport.Region.Up).IsReady) throw new Exception("normal unqualified");
var modules=Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(m=>m.ModuleName).ToArray();
var forbidden=modules.Where(n=>n.Contains("NovaCore.Native",StringComparison.OrdinalIgnoreCase)||n.Contains("vulkan",StringComparison.OrdinalIgnoreCase)).ToArray();
if(forbidden.Length!=0)throw new Exception(string.Join(",",forbidden));
Console.WriteLine(JsonSerializer.Serialize(new{result="PASS",nativeVulkanModules=forbidden,authority=query.Authority}));
