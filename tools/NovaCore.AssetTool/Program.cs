using System.Net.Http.Headers;
using NovaCore.Graphics;

if (!TerrainAssetRepository.TryFindRoot(out var repositoryRoot))
{
    Console.Error.WriteLine("NovaCore repository root could not be located.");
    return 2;
}

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";
var assetId = args.Skip(1).FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal)) ?? TerrainAssetCache.ProductionEarthAssetId;
var cacheOption = Option(args, "--cache");
var sourceOption = Option(args, "--source");
var cacheRoot = TerrainAssetRepository.CacheRoot(repositoryRoot, cacheOption);
var manifestPath = TerrainAssetRepository.ManifestPath(repositoryRoot, assetId);
if (!TerrainAssetManifestFile.TryLoad(manifestPath, out var manifest, out var manifestError))
{
    Console.Error.WriteLine(manifestError);
    return 2;
}

var contentPath = TerrainAssetCache.ContentPath(cacheRoot, manifest);
switch (command)
{
    case "status":
    case "verify":
    {
        var result = TerrainAssetCache.Verify(manifest, contentPath);
        Print(manifest, cacheRoot, result);
        return result.IsValid ? 0 : 1;
    }
    case "install":
    {
        if (string.IsNullOrWhiteSpace(sourceOption))
        {
            Console.Error.WriteLine("install requires --source <path>.");
            return 2;
        }
        var result = TerrainAssetCache.PublishFromFile(manifest, Path.GetFullPath(sourceOption), cacheRoot);
        Print(manifest, cacheRoot, result);
        return result.IsValid ? 0 : 1;
    }
    case "fetch":
    {
        if (string.IsNullOrWhiteSpace(manifest.Artifact?.Url))
        {
            Console.Error.WriteLine($"No remote artifact is configured for '{assetId}'. Run 'dotnet run --project tools/NovaCore.AssetTool -- build {assetId}'.");
            return 1;
        }
        var stagingDirectory = Path.Combine(cacheRoot, ".downloads");
        Directory.CreateDirectory(stagingDirectory);
        var staging = Path.Combine(stagingDirectory, $"{assetId}-{Guid.NewGuid():N}.nccube.incomplete");
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NovaCore.AssetTool", "1"));
            using var response = await client.GetAsync(manifest.Artifact.Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync())
            await using (var destination = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None, TerrainAssetCache.VerificationBufferBytes, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(destination, TerrainAssetCache.VerificationBufferBytes);
                await destination.FlushAsync();
            }
            var result = TerrainAssetCache.PublishFromFile(manifest, staging, cacheRoot);
            Print(manifest, cacheRoot, result);
            return result.IsValid ? 0 : 1;
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }
    case "build":
    case "regenerate":
    {
        if (!TerrainAssetGenerator.TryBuildAndPublish(repositoryRoot, manifest, cacheRoot, out var result, out var error))
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        Print(manifest, cacheRoot, result);
        return 0;
    }
    case "clean-incomplete":
    {
        Console.WriteLine($"Removed stale incomplete files: {TerrainAssetCache.RemoveStaleIncompleteFiles(cacheRoot, TimeSpan.FromHours(24))}");
        return 0;
    }
    default:
        Console.Error.WriteLine("Usage: NovaCore.AssetTool [status|verify|fetch|build|install|clean-incomplete] [asset-id] [--cache path] [--source path]");
        return 2;
}

static string? Option(string[] values, string name)
{
    for (var index = 0; index + 1 < values.Length; index++) if (values[index].Equals(name, StringComparison.OrdinalIgnoreCase)) return values[index + 1];
    return null;
}

static void Print(in TerrainAssetManifest manifest, string cacheRoot, in TerrainAssetVerification result)
{
    Console.WriteLine($"Asset: {manifest.AssetId}");
    Console.WriteLine($"Version: terrain-v{manifest.TerrainVersion}; NCCUBE{manifest.FormatVersion}");
    Console.WriteLine($"Cache: {cacheRoot}");
    Console.WriteLine($"Path: {result.Path}");
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Bytes: {result.ActualBytes}/{result.ExpectedBytes}");
    Console.WriteLine($"SHA-256: {(string.IsNullOrEmpty(result.ActualSha256) ? "unavailable" : result.ActualSha256)}");
    Console.WriteLine($"Verification buffer: {result.MaximumBufferBytes} bytes");
}
