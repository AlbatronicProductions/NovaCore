using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static class LocalTerrainPayload2Tests
{
    internal static void Run() => Run("local-payload2.nccube", 5, 2, CurrentHash);
    // Frozen output of the current real-input generator; see the Python regeneration test.
    private const string CurrentHash = "19366ff801f81204ca1be18efa466785126a5bd1601fb2b28cc89be1fe749524";

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void Run(string name, uint terrain, byte payload, string hash)
    {
        var path = GraphicsTestHarness.RepositoryPath("tests", "fixtures", "terrain", name);
        var bytes = File.ReadAllBytes(path);
        Check(bytes.Length < 192 * 1024 && Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.OrdinalIgnoreCase), "bounded current fixture identity");
        Check(PlanetaryLocalTerrainPackContract.TryReadHeader(bytes, out var header) && header.Version == 2 && header.TerrainVersion == terrain && header.RecordCount == 1, "schema-2 header");
        Check(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(bytes.AsSpan(256), out var record) && record.Sector.PayloadVersion == payload && record.Sector.TerrainVersion == terrain && record.Sector.DetailFrequency == 1 && !record.HasControl && !record.HasPerRecordResidualRange, "three-channel payload identity without regional control/ranges");
        var channels = new byte[3][];
        var cursor = 384;
        for (var channel = 0; channel < 3; channel++)
        {
            var stored = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(288 + channel * 4)));
            var gpu = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(300 + channel * 4)));
            Check(gpu == (channel == 1 ? 34848 : 69696), "BC7/BC4/BC5 layout");
            channels[channel] = new byte[gpu];
            if (bytes[312 + channel] == 0) bytes.AsSpan(cursor, stored).CopyTo(channels[channel]);
            else Check(PlanetaryLocalTerrainTranscode.TryDecodePackBits(bytes.AsSpan(cursor, stored), channels[channel], out var written) && written == gpu, "PackBits channel decode");
            cursor += stored;
        }
        Check(cursor == bytes.Length, "exact record coverage");
        using (var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            digest.AppendData(bytes.AsSpan(256, 24));
            foreach (var channel in channels) digest.AppendData(channel);
            Check(digest.GetHashAndReset().AsSpan().SequenceEqual(bytes.AsSpan(320, 32)), "identity and decoded channels digest");
        }
        var manifest = new TerrainAssetManifest
        {
            Schema = TerrainAssetManifest.CurrentSchema, AssetId = "payload2-test", BodyId = 6,
            TerrainVersion = terrain, Format = "nccube", FormatVersion = 2, FileName = name,
            ByteSize = bytes.Length, Sha256 = hash,
            Hierarchy = new() { Coverage = "sparse-local-relaxed-cube-sphere", MinimumPayloadLevel = header.MinimumSectorLevel, MaximumPayloadLevel = header.MaximumSectorLevel, FaceCount = 6, RecordCount = 1 },
            Provenance = "tests/fixtures/terrain/README.md", ContentManifest = "tests/fixtures/terrain/README.md"
        };
        Check(TerrainAssetCache.Verify(manifest, path).IsValid, "full manifest, structure and hash verification");
        Check(NativeRuntime.ValidateTerrainAsset(path, 6, terrain, 1) == NativeResult.Success, "native schema-2 read, transcode and digest");
        var temporary = Path.Combine(Path.GetTempPath(), $"novacore-payload2-{Guid.NewGuid():N}.nccube");
        try
        {
            // Corrupt each channel and the digest-bound identity. A single record ensures
            // the native first-payload diagnostic also covers the complete fixture.
            foreach (var offset in new[] { 271, 384, 384 + (int)record.StoredAlbedoBytes, 384 + (int)(record.StoredAlbedoBytes + record.StoredElevationBytes) })
            {
                var corrupt = (byte[])bytes.Clone(); corrupt[offset] ^= 0x5a;
                File.WriteAllBytes(temporary, corrupt);
                Check(NativeRuntime.ValidateTerrainAsset(temporary, 6, terrain, 1) == NativeResult.Failure, "native rejects identity/channel corruption");
                Check(!TerrainAssetCache.Verify(manifest, temporary).IsValid, "manifest rejects corrupt bytes");
            }
            foreach (var unsupportedPayload in new byte[] { 1, 255 })
            {
                var unsupported = (byte[])bytes.Clone(); unsupported[271] = unsupportedPayload;
                // Keep the record digest valid so rejection cannot be attributed to corruption.
                using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                digest.AppendData(unsupported.AsSpan(256, 24));
                foreach (var channel in channels) digest.AppendData(channel);
                digest.GetHashAndReset().CopyTo(unsupported, 320);
                File.WriteAllBytes(temporary, unsupported);
                Check(NativeRuntime.ValidateTerrainAsset(temporary, 6, terrain, 1) == NativeResult.Failure, "native rejects unsupported payload contract with valid digest");
                Check(!PlanetaryLocalTerrainPackContract.TryReadRecordHeader(unsupported.AsSpan(256), out _), "record API rejects unsupported payload");
                Check(!EarthLocalTerrainElevationDataset.TryLoad(temporary, out _), "oracle rejects unsupported payload before publication");
                var identity = Convert.ToHexString(SHA256.HashData(unsupported)).ToLowerInvariant();
                Check(TerrainAssetCache.Verify(manifest with { Sha256 = identity }, temporary).Status == TerrainAssetVerificationStatus.PackContractMismatch, "manifest with matching hash cannot admit unsupported payload");
            }
            File.WriteAllBytes(temporary, bytes[..^1]);
            Check(!EarthLocalTerrainElevationDataset.TryLoad(temporary, out _), "managed oracle rejects truncated payload before publication");
            Check(NativeRuntime.ValidateTerrainAsset(temporary, 6, terrain, 1) == NativeResult.Failure, "native rejects truncated first payload");
            Check(TerrainAssetCache.Verify(manifest, temporary).Status == TerrainAssetVerificationStatus.SizeMismatch, "manifest rejects truncation");
            Check(TerrainAssetCache.Verify(manifest with { TerrainVersion = terrain + 10 }, path).Status == TerrainAssetVerificationStatus.PackContractMismatch, "manifest cannot relabel terrain lineage");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }

        Check(!EarthLocalTerrainElevationDataset.IsLoaded && EarthLocalTerrainElevationDataset.TryLoad(path, out _), "fresh isolated process loads original schema-2 elevation");
        // Interior texel centers avoid interpolation and sparse-edge fading. This is
        // the current stored geographic address space.
        var extent = 264; var cells = 1 << record.Sector.Level;
        foreach (var (x, y) in new[] { (68, 68), (132, 132), (196, 196) })
        {
            var block = ((y / 4) * 66 + x / 4) * 8;
            var b = channels[1].AsSpan(block, 8);
            var palette = new byte[8]; palette[0] = b[0]; palette[1] = b[1];
            if (b[0] > b[1]) for (var n = 2; n < 8; n++) palette[n] = (byte)(((8 - n) * b[0] + (n - 1) * b[1] + 3) / 7);
            else { for (var n = 2; n < 6; n++) palette[n] = (byte)(((6 - n) * b[0] + (n - 1) * b[1] + 2) / 5); palette[7] = 255; }
            ulong indices = 0; for (var n = 0; n < 6; n++) indices |= (ulong)b[n + 2] << (n * 8);
            var encoded = palette[(indices >> (3 * ((y % 4) * 4 + x % 4))) & 7];
            var direction = RelaxedCubeSphereProjection.UnitDirection(record.Sector.Face,
                (record.Sector.X + (x - 3.5) / (extent - 8)) / cells,
                (record.Sector.Y + (y - 3.5) / (extent - 8)) / cells);
            var expected = header.ResidualMinimumMetres + encoded * (header.ResidualMaximumMetres - header.ResidualMinimumMetres) / 255d;
            Check(Math.Abs(EarthLocalTerrainElevationDataset.SampleResidual(direction) - expected) < 1e-5, "managed decoded residual preserves stored address semantics");
        }
        Console.WriteLine($"NCCUBE2 schema=2 payload={payload} terrain={terrain}; bytes={bytes.Length}; sha256={hash}; channels/digest/oracle/corruption/truncation PASS");
    }
}
