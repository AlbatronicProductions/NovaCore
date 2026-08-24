using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NovaCore.Graphics;

public enum TerrainAssetVerificationStatus
{
    Valid,
    Missing,
    InvalidManifest,
    SizeMismatch,
    HashMismatch,
    PackContractMismatch
}

public readonly record struct TerrainAssetVerification(
    TerrainAssetVerificationStatus Status,
    string Path,
    long ExpectedBytes,
    long ActualBytes,
    string ExpectedSha256,
    string ActualSha256,
    int MaximumBufferBytes,
    string Message)
{
    public bool IsValid => Status == TerrainAssetVerificationStatus.Valid;
}

public sealed record TerrainAssetHierarchyManifest
{
    public string Coverage { get; init; } = string.Empty;
    public int MinimumPayloadLevel { get; init; }
    public int MaximumPayloadLevel { get; init; }
    public int FaceCount { get; init; }
    public int RecordCount { get; init; }
}

public sealed record TerrainAssetGeneratorManifest
{
    public string Tool { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Executable { get; init; } = string.Empty;
    public string[] Arguments { get; init; } = [];
}

public sealed record TerrainAssetArtifactManifest
{
    public string? Url { get; init; }
}

public sealed record TerrainAssetManifest
{
    public const string CurrentSchema = "NovaCore.TerrainAsset/1";

    public string Schema { get; init; } = string.Empty;
    public string AssetId { get; init; } = string.Empty;
    public ulong BodyId { get; init; }
    public uint TerrainVersion { get; init; }
    public string Format { get; init; } = string.Empty;
    public uint FormatVersion { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long ByteSize { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public TerrainAssetHierarchyManifest Hierarchy { get; init; } = new();
    public string Provenance { get; init; } = string.Empty;
    public string ContentManifest { get; init; } = string.Empty;
    public TerrainAssetGeneratorManifest? Generator { get; init; }
    public TerrainAssetArtifactManifest? Artifact { get; init; }

    public bool IsValid =>
        Schema == CurrentSchema &&
        IsSafeIdentity(AssetId) &&
        BodyId != 0 && TerrainVersion != 0 &&
        Format.Equals("nccube", StringComparison.Ordinal) && FormatVersion == PlanetaryCubeSurfacePackContract.Version &&
        IsSafeFileName(FileName) && ByteSize > PlanetaryCubeSurfacePackContract.HeaderBytes &&
        Sha256.Length == 64 && Sha256.All(IsLowerHex) &&
        IsSafeIdentity(Hierarchy.Coverage) &&
        Hierarchy.MinimumPayloadLevel >= 0 && Hierarchy.MaximumPayloadLevel >= Hierarchy.MinimumPayloadLevel &&
        Hierarchy.MaximumPayloadLevel <= PlanetaryCubeSurfacePackContract.MaximumLevel &&
        Hierarchy.FaceCount is >= 1 and <= 6 && Hierarchy.RecordCount > 0 &&
        !string.IsNullOrWhiteSpace(Provenance) && !string.IsNullOrWhiteSpace(ContentManifest);

    private static bool IsSafeIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith('/') || value.EndsWith('/')) return false;
        foreach (var segment in value.Split('/'))
            if (segment.Length == 0 || segment is "." or ".." ||
                segment.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
                return false;
        return true;
    }

    private static bool IsSafeFileName(string value) =>
        !string.IsNullOrWhiteSpace(value) && value == Path.GetFileName(value) &&
        value.EndsWith(".nccube", StringComparison.OrdinalIgnoreCase);

    private static bool IsLowerHex(char value) => value is >= '0' and <= '9' or >= 'a' and <= 'f';
}

public static class TerrainAssetManifestFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static bool TryLoad(string path, out TerrainAssetManifest manifest, out string error)
    {
        manifest = new();
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
            manifest = JsonSerializer.Deserialize<TerrainAssetManifest>(stream, Options) ?? new();
            if (!manifest.IsValid)
            {
                error = $"Terrain asset manifest is invalid: {path}";
                return false;
            }
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            error = $"Terrain asset manifest could not be read: {path}; {exception.Message}";
            return false;
        }
    }
}

public static class TerrainAssetRepository
{
    public const string CacheEnvironmentVariable = "NOVACORE_ASSET_CACHE";
    public const string ManifestRelativeDirectory = "assets/terrain/manifests";

    public static bool TryFindRoot(string startPath, out string root)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        if (!directory.Exists && directory.Parent is not null) directory = directory.Parent;
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                Directory.Exists(Path.Combine(directory.FullName, "assets", "terrain", "manifests")))
            {
                root = directory.FullName;
                return true;
            }
            directory = directory.Parent;
        }
        root = string.Empty;
        return false;
    }

    public static bool TryFindRoot(out string root) =>
        TryFindRoot(Environment.CurrentDirectory, out root) || TryFindRoot(AppContext.BaseDirectory, out root);

    public static string ManifestPath(string repositoryRoot, string assetId) =>
        Path.Combine(repositoryRoot, "assets", "terrain", "manifests", assetId + ".json");

    public static string CacheRoot(string repositoryRoot, string? explicitCacheRoot = null)
    {
        var configured = explicitCacheRoot;
        if (string.IsNullOrWhiteSpace(configured)) configured = Environment.GetEnvironmentVariable(CacheEnvironmentVariable);
        return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(repositoryRoot, ".novacore", "cache", "terrain", "v1")
            : configured);
    }
}

public static class TerrainAssetCache
{
    public const int VerificationBufferBytes = 1024 * 1024;
    public const string ProductionEarthAssetId = "earth-surface-v4";

    public static string ContentPath(string cacheRoot, in TerrainAssetManifest manifest) =>
        Path.Combine(cacheRoot, "sha256", manifest.Sha256[..2], manifest.Sha256 + ".nccube");

    public static TerrainAssetVerification Verify(in TerrainAssetManifest manifest, string path)
    {
        if (!manifest.IsValid)
            return new(TerrainAssetVerificationStatus.InvalidManifest, path, manifest.ByteSize, 0, manifest.Sha256, string.Empty, 0, "Terrain asset manifest is invalid.");
        if (!File.Exists(path))
            return new(TerrainAssetVerificationStatus.Missing, path, manifest.ByteSize, 0, manifest.Sha256, string.Empty, 0, "Terrain asset is missing.");

        var actualBytes = new FileInfo(path).Length;
        if (actualBytes != manifest.ByteSize)
            return new(TerrainAssetVerificationStatus.SizeMismatch, path, manifest.ByteSize, actualBytes, manifest.Sha256, string.Empty, 0,
                $"Terrain asset byte-size mismatch: expected {manifest.ByteSize}, found {actualBytes}.");

        if (!TryValidatePackStructure(manifest, path, out var packError))
            return new(TerrainAssetVerificationStatus.PackContractMismatch, path, manifest.ByteSize, actualBytes, manifest.Sha256, string.Empty, PlanetaryCubeSurfacePackContract.RecordHeaderBytes, packError);

        var buffer = ArrayPool<byte>.Shared.Rent(VerificationBufferBytes);
        try
        {
            using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, VerificationBufferBytes, FileOptions.SequentialScan);
            int read;
            while ((read = stream.Read(buffer, 0, VerificationBufferBytes)) != 0) digest.AppendData(buffer, 0, read);
            var actualSha = Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant();
            if (!actualSha.Equals(manifest.Sha256, StringComparison.Ordinal))
                return new(TerrainAssetVerificationStatus.HashMismatch, path, manifest.ByteSize, actualBytes, manifest.Sha256, actualSha, VerificationBufferBytes,
                    $"Terrain asset SHA-256 mismatch: expected {manifest.Sha256}, found {actualSha}.");
            return new(TerrainAssetVerificationStatus.Valid, path, manifest.ByteSize, actualBytes, manifest.Sha256, actualSha, VerificationBufferBytes, "Terrain asset is valid.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static bool TryResolveRequired(string repositoryRoot, string assetId, string? cacheRoot, out TerrainAssetManifest manifest, out string path, out string error)
    {
        var manifestPath = TerrainAssetRepository.ManifestPath(repositoryRoot, assetId);
        if (!TerrainAssetManifestFile.TryLoad(manifestPath, out manifest, out error))
        {
            path = string.Empty;
            return false;
        }
        var resolvedCache = TerrainAssetRepository.CacheRoot(repositoryRoot, cacheRoot);
        path = ContentPath(resolvedCache, manifest);
        var verification = Verify(manifest, path);
        if (verification.IsValid)
        {
            error = string.Empty;
            return true;
        }
        error = ActionableError(manifest, resolvedCache, verification);
        return false;
    }

    public static string ActionableError(in TerrainAssetManifest manifest, string cacheRoot, in TerrainAssetVerification verification) =>
        $"Required terrain asset '{manifest.AssetId}' is {verification.Status.ToString().ToLowerInvariant()}. " +
        $"Expected {manifest.ByteSize} bytes with SHA-256 {manifest.Sha256} at '{ContentPath(cacheRoot, manifest)}'. " +
        $"Run 'dotnet run --project tools/NovaCore.AssetTool -- fetch {manifest.AssetId}' or " +
        $"'dotnet run --project tools/NovaCore.AssetTool -- build {manifest.AssetId}'.";

    public static TerrainAssetVerification PublishFromFile(in TerrainAssetManifest manifest, string sourcePath, string cacheRoot)
    {
        var finalPath = ContentPath(cacheRoot, manifest);
        var current = Verify(manifest, finalPath);
        if (current.IsValid) return current;
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var temporaryPath = finalPath + $".incomplete-{Environment.ProcessId}-{Guid.NewGuid():N}";
        var buffer = ArrayPool<byte>.Shared.Rent(VerificationBufferBytes);
        try
        {
            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, VerificationBufferBytes, FileOptions.SequentialScan))
            using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, VerificationBufferBytes, FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                int read;
                while ((read = source.Read(buffer, 0, VerificationBufferBytes)) != 0) destination.Write(buffer, 0, read);
                destination.Flush(true);
            }
            var staged = Verify(manifest, temporaryPath);
            if (!staged.IsValid) return staged;
            // An existing valid object is returned above. Replacing an invalid
            // occupant is safe because staged bytes already match this content
            // address; the same-volume move publishes atomically.
            File.Move(temporaryPath, finalPath, true);
            return Verify(manifest, finalPath);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public static int RemoveStaleIncompleteFiles(string cacheRoot, TimeSpan minimumAge)
    {
        if (!Directory.Exists(cacheRoot)) return 0;
        var threshold = DateTime.UtcNow - minimumAge;
        var removed = 0;
        foreach (var path in Directory.EnumerateFiles(cacheRoot, "*.incomplete*", SearchOption.AllDirectories))
        {
            if (File.GetLastWriteTimeUtc(path) > threshold) continue;
            File.Delete(path);
            removed++;
        }
        return removed;
    }

    private static bool TryValidatePackStructure(in TerrainAssetManifest manifest, string path, out string error)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.RandomAccess);
        Span<byte> headerBytes = stackalloc byte[PlanetaryCubeSurfacePackContract.HeaderBytes];
        if (!TryReadExactly(stream, headerBytes) || !PlanetaryCubeSurfacePackContract.TryReadHeader(headerBytes, out var header) ||
            header.Version != manifest.FormatVersion || header.TerrainVersion != manifest.TerrainVersion ||
            header.MaximumLevel != manifest.Hierarchy.MaximumPayloadLevel || header.RecordCount != manifest.Hierarchy.RecordCount)
        {
            error = "Terrain asset .nccube header does not match the tracked manifest.";
            return false;
        }
        var ordinals = new HashSet<ulong>();
        Span<byte> recordBytes = stackalloc byte[PlanetaryCubeSurfacePackContract.RecordHeaderBytes];
        for (var index = 0; index < header.RecordCount; index++)
        {
            if (!TryReadExactly(stream, recordBytes) || !PlanetaryCubeSurfacePackContract.TryReadRecordHeader(recordBytes, out var record) ||
                record.Patch.BodyId != manifest.BodyId || record.Patch.TerrainVersion != manifest.TerrainVersion || !ordinals.Add(record.PatchOrdinal))
            {
                error = $"Terrain asset .nccube record {index} is invalid or duplicated.";
                return false;
            }
            var payloadBytes = checked((long)record.AlbedoBytes + record.ElevationBytes + record.LandMaskBytes + record.CloudBytes);
            if (payloadBytes <= 0 || stream.Position > stream.Length - payloadBytes)
            {
                error = $"Terrain asset .nccube record {index} payload is truncated.";
                return false;
            }
            stream.Seek(payloadBytes, SeekOrigin.Current);
        }
        if (stream.Position != stream.Length || ordinals.Count != header.RecordCount)
        {
            error = "Terrain asset .nccube record table does not exactly cover the file.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static bool TryReadExactly(Stream stream, Span<byte> destination)
    {
        var total = 0;
        while (total < destination.Length)
        {
            var read = stream.Read(destination[total..]);
            if (read == 0) return false;
            total += read;
        }
        return true;
    }
}

public static class TerrainAssetGenerator
{
    public const string PythonEnvironmentVariable = "NOVACORE_PYTHON";

    public static bool TryBuildAndPublish(string repositoryRoot, in TerrainAssetManifest manifest, string cacheRoot, out TerrainAssetVerification verification, out string error)
    {
        verification = default;
        if (manifest.Generator is not { } generator || string.IsNullOrWhiteSpace(generator.Tool) || string.IsNullOrWhiteSpace(generator.Executable))
        {
            error = $"Terrain asset '{manifest.AssetId}' has no deterministic generator configured.";
            return false;
        }
        var generationDirectory = Path.Combine(cacheRoot, ".generation");
        Directory.CreateDirectory(generationDirectory);
        var output = Path.Combine(generationDirectory, $"{manifest.AssetId}-{Guid.NewGuid():N}.nccube.incomplete");
        try
        {
            var executable = generator.Executable;
            if (executable.Equals("python", StringComparison.OrdinalIgnoreCase) &&
                Environment.GetEnvironmentVariable(PythonEnvironmentVariable) is { Length: > 0 } configuredPython)
                executable = configuredPython;
            var start = new ProcessStartInfo(executable)
            {
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add(Path.GetFullPath(Path.Combine(repositoryRoot, generator.Tool)));
            foreach (var argument in generator.Arguments)
                start.ArgumentList.Add(argument.Replace("{repository}", repositoryRoot, StringComparison.Ordinal).Replace("{output}", output, StringComparison.Ordinal));
            using var process = Process.Start(start);
            if (process is null)
            {
                error = "Terrain asset generator could not be started.";
                return false;
            }
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                error = $"Terrain asset generator failed with exit code {process.ExitCode}: {stderr}{stdout}";
                return false;
            }
            verification = TerrainAssetCache.PublishFromFile(manifest, output, cacheRoot);
            if (!verification.IsValid)
            {
                error = verification.Message;
                return false;
            }
            error = string.Empty;
            return true;
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
            var generatedManifest = output + ".manifest.json";
            if (File.Exists(generatedManifest)) File.Delete(generatedManifest);
        }
    }
}
