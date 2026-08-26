using System.Buffers.Binary;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Stable identity for one geographically local terrain-v5 refinement payload.
/// Camera, pupil, mesh tier, residency slot and GPU address are intentionally
/// absent: none of them changes the represented physical region.
/// </summary>
public readonly record struct PlanetaryLocalTerrainSectorId(
    ulong BodyId,
    uint TerrainVersion,
    CubeSphereFace Face,
    int Level,
    int X,
    int Y,
    byte DetailFrequency,
    byte PayloadVersion) : IComparable<PlanetaryLocalTerrainSectorId>
{
    public bool IsValid
    {
        get
        {
            if (BodyId == 0 || TerrainVersion == 0 || Level is < PlanetaryLocalTerrainPackContract.MinimumSectorLevel or > PlanetaryLocalTerrainPackContract.MaximumSectorLevel ||
                DetailFrequency == 0 || PayloadVersion == 0) return false;
            var size = 1 << Level;
            return X >= 0 && Y >= 0 && X < size && Y < size;
        }
    }

    public PlanetarySurfacePatchId GeographicPatch => IsValid
        ? new(BodyId, TerrainVersion, Face, Level, X, Y)
        : throw new InvalidOperationException("Invalid local terrain sector identity.");

    public int CompareTo(PlanetaryLocalTerrainSectorId other)
    {
        var body = BodyId.CompareTo(other.BodyId); if (body != 0) return body;
        var terrain = TerrainVersion.CompareTo(other.TerrainVersion); if (terrain != 0) return terrain;
        var patch = GeographicPatch.CompareTo(other.GeographicPatch); if (patch != 0) return patch;
        var frequency = DetailFrequency.CompareTo(other.DetailFrequency); if (frequency != 0) return frequency;
        return PayloadVersion.CompareTo(other.PayloadVersion);
    }
}

public enum PlanetaryLocalTerrainGpuFormat : byte
{
    Bc7Srgb = 1,
    Bc4Unorm = 2,
    Bc5Unorm = 3
}

public enum PlanetaryLocalTerrainStorageCodec : byte
{
    RawGpuBlocks = 0,
    PackBits = 1
}

/// <summary>NCCUBE2 sparse local-payload contract. It complements, but never deepens, the global NCCUBE1 L0-L2 hierarchy.</summary>
public static class PlanetaryLocalTerrainPackContract
{
    public const ulong Magic = 0x003245425543434Eul; // "NCCUBE2\0", little endian
    public const uint Version = 2;
    public const int HeaderBytes = 256;
    public const int RecordHeaderBytes = 128;
    public const int InteriorTexels = 256;
    public const int SeamGutterTexels = 4;
    public const int StoredExtent = InteriorTexels + 2 * SeamGutterTexels;
    public const int MinimumSectorLevel = 3;
    public const int MaximumSectorLevel = 20;
    public const float DefaultResidualMinimumMetres = -512f;
    public const float DefaultResidualMaximumMetres = 512f;

    public static int BlockCount(int extent) => checked(((extent + 3) / 4) * ((extent + 3) / 4));
    public static int GpuBytes(PlanetaryLocalTerrainGpuFormat format, int extent) => checked(BlockCount(extent) * (format == PlanetaryLocalTerrainGpuFormat.Bc4Unorm ? 8 : 16));

    public static bool TryReadHeader(ReadOnlySpan<byte> bytes, out PlanetaryLocalTerrainPackHeader header)
    {
        header = default;
        if (bytes.Length < HeaderBytes || BinaryPrimitives.ReadUInt64LittleEndian(bytes) != Magic) return false;
        header = new(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..]),
            bytes[52], bytes[53],
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[56..])),
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[60..])));
        return header.IsValid;
    }

    public static bool TryReadRecordHeader(ReadOnlySpan<byte> bytes, out PlanetaryLocalTerrainRecordHeader header)
    {
        header = default;
        if (bytes.Length < RecordHeaderBytes) return false;
        var id = new PlanetaryLocalTerrainSectorId(
            BinaryPrimitives.ReadUInt64LittleEndian(bytes),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]),
            (CubeSphereFace)bytes[12], bytes[13],
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..])),
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..])),
            bytes[14], bytes[15]);
        header = new(
            id,
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[36..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[40..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[44..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]),
            (PlanetaryLocalTerrainStorageCodec)bytes[56],
            (PlanetaryLocalTerrainStorageCodec)bytes[57],
            (PlanetaryLocalTerrainStorageCodec)bytes[58],
            bytes[59],
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[72..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[80..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[88..]));
        return header.IsValid;
    }
}

public readonly record struct PlanetaryLocalTerrainPackHeader(
    uint Version,
    uint HeaderSize,
    uint RecordHeaderSize,
    uint InteriorTexels,
    uint GutterTexels,
    uint StoredExtent,
    uint RecordCount,
    ulong BodyId,
    uint TerrainVersion,
    byte MinimumSectorLevel,
    byte MaximumSectorLevel,
    float ResidualMinimumMetres,
    float ResidualMaximumMetres)
{
    public bool IsValid => Version == PlanetaryLocalTerrainPackContract.Version && HeaderSize == PlanetaryLocalTerrainPackContract.HeaderBytes &&
        RecordHeaderSize == PlanetaryLocalTerrainPackContract.RecordHeaderBytes && InteriorTexels > 0 && GutterTexels > 0 &&
        StoredExtent == InteriorTexels + 2 * GutterTexels && RecordCount > 0 && BodyId != 0 && TerrainVersion != 0 &&
        MinimumSectorLevel >= PlanetaryLocalTerrainPackContract.MinimumSectorLevel && MaximumSectorLevel >= MinimumSectorLevel &&
        MaximumSectorLevel <= PlanetaryLocalTerrainPackContract.MaximumSectorLevel && float.IsFinite(ResidualMinimumMetres) &&
        float.IsFinite(ResidualMaximumMetres) && ResidualMaximumMetres > ResidualMinimumMetres;
}

public readonly record struct PlanetaryLocalTerrainRecordHeader(
    PlanetaryLocalTerrainSectorId Sector,
    ulong PayloadOffset,
    uint StoredAlbedoBytes,
    uint StoredElevationBytes,
    uint StoredNormalBytes,
    uint GpuAlbedoBytes,
    uint GpuElevationBytes,
    uint GpuNormalBytes,
    PlanetaryLocalTerrainStorageCodec AlbedoCodec,
    PlanetaryLocalTerrainStorageCodec ElevationCodec,
    PlanetaryLocalTerrainStorageCodec NormalCodec,
    byte Flags,
    ulong Digest0,
    ulong Digest1,
    ulong Digest2,
    ulong Digest3)
{
    public bool IsValid => Sector.IsValid && PayloadOffset >= PlanetaryLocalTerrainPackContract.HeaderBytes &&
        StoredAlbedoBytes > 0 && StoredElevationBytes > 0 && StoredNormalBytes > 0 &&
        GpuAlbedoBytes > 0 && GpuElevationBytes > 0 && GpuNormalBytes > 0 &&
        AlbedoCodec <= PlanetaryLocalTerrainStorageCodec.PackBits && ElevationCodec <= PlanetaryLocalTerrainStorageCodec.PackBits &&
        NormalCodec <= PlanetaryLocalTerrainStorageCodec.PackBits && (Digest0 | Digest1 | Digest2 | Digest3) != 0;
}

public static class PlanetaryLocalTerrainTranscode
{
    /// <summary>Deterministic byte-oriented PackBits used only on disk; output remains GPU-native BC blocks.</summary>
    public static bool TryDecodePackBits(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
    {
        var input = 0; written = 0;
        while (input < source.Length)
        {
            var control = source[input++];
            if ((control & 0x80) == 0)
            {
                var count = control + 1;
                if (input + count > source.Length || written + count > destination.Length) return false;
                source.Slice(input, count).CopyTo(destination[written..]); input += count; written += count;
            }
            else
            {
                var count = (control & 0x7f) + 3;
                if (input >= source.Length || written + count > destination.Length) return false;
                destination.Slice(written, count).Fill(source[input++]); written += count;
            }
        }
        return written == destination.Length;
    }
}

public readonly record struct PlanetaryLocalTerrainResidualError(
    double MaximumVerticalErrorMetres,
    double RmsVerticalErrorMetres,
    int WorstSampleIndex)
{
    public bool IsValid => double.IsFinite(MaximumVerticalErrorMetres) && double.IsFinite(RmsVerticalErrorMetres) &&
        MaximumVerticalErrorMetres >= 0d && RmsVerticalErrorMetres >= 0d && WorstSampleIndex >= 0;
}

public static class PlanetaryLocalTerrainResidualAnalysis
{
    public static PlanetaryLocalTerrainResidualError Measure(ReadOnlySpan<float> sourceResidualMetres, ReadOnlySpan<byte> reconstructedBc4Unorm,
        float minimumMetres = PlanetaryLocalTerrainPackContract.DefaultResidualMinimumMetres,
        float maximumMetres = PlanetaryLocalTerrainPackContract.DefaultResidualMaximumMetres)
    {
        if (sourceResidualMetres.IsEmpty || sourceResidualMetres.Length != reconstructedBc4Unorm.Length || !float.IsFinite(minimumMetres) ||
            !float.IsFinite(maximumMetres) || maximumMetres <= minimumMetres) throw new ArgumentOutOfRangeException();
        var maximum = -1d; var sumSquares = 0d; var worst = 0;
        for (var index = 0; index < sourceResidualMetres.Length; index++)
        {
            var source = sourceResidualMetres[index]; if (!float.IsFinite(source)) throw new ArgumentOutOfRangeException(nameof(sourceResidualMetres));
            var reconstructed = minimumMetres + reconstructedBc4Unorm[index] * ((maximumMetres - minimumMetres) / 255d);
            var error = Math.Abs(source - reconstructed); sumSquares += error * error;
            if (error > maximum) { maximum = error; worst = index; }
        }
        return new(maximum, Math.Sqrt(sumSquares / sourceResidualMetres.Length), worst);
    }
}

/// <summary>
/// CPU mirror of the resident NCCUBE2 physical elevation-residual channel.
/// Camera clearance queries use the same relaxed-cube sector identity and the
/// same decoded BC4 texels as the GPU, so a local refinement can never rise
/// through an otherwise terrain-safe camera.
/// </summary>
public static class EarthLocalTerrainElevationDataset
{
    private sealed class Snapshot
    {
        internal Snapshot(PlanetaryLocalTerrainPackHeader header, byte detailFrequency, byte payloadVersion,
            Dictionary<PlanetaryLocalTerrainSectorId, byte[]> residuals)
        { Header = header; DetailFrequency = detailFrequency; PayloadVersion = payloadVersion; Residuals = residuals; }
        internal PlanetaryLocalTerrainPackHeader Header { get; }
        internal byte DetailFrequency { get; }
        internal byte PayloadVersion { get; }
        internal Dictionary<PlanetaryLocalTerrainSectorId, byte[]> Residuals { get; }
    }

    private static readonly object Gate = new();
    private static Snapshot? _snapshot;
    public static bool IsLoaded => Volatile.Read(ref _snapshot) is not null;

    public static bool TryLoad(string path, out string error)
    {
        if (string.IsNullOrWhiteSpace(path)) { error = "Local terrain path is empty."; return false; }
        if (IsLoaded) { error = string.Empty; return true; }
        try
        {
            var package = File.ReadAllBytes(path);
            if (!PlanetaryLocalTerrainPackContract.TryReadHeader(package, out var header))
            { error = "Local terrain elevation oracle header is invalid."; return false; }
            var residuals = new Dictionary<PlanetaryLocalTerrainSectorId, byte[]>(checked((int)header.RecordCount));
            var offset = PlanetaryLocalTerrainPackContract.HeaderBytes; byte detailFrequency = 0, payloadVersion = 0;
            for (var index = 0; index < header.RecordCount; index++)
            {
                if (offset + PlanetaryLocalTerrainPackContract.RecordHeaderBytes > package.Length ||
                    !PlanetaryLocalTerrainPackContract.TryReadRecordHeader(package.AsSpan(offset, PlanetaryLocalTerrainPackContract.RecordHeaderBytes), out var record))
                { error = $"Local terrain elevation record {index} is invalid."; return false; }
                var payload = checked((int)record.PayloadOffset); var elevationOffset = checked(payload + (int)record.StoredAlbedoBytes);
                var elevationEnd = checked(elevationOffset + (int)record.StoredElevationBytes);
                var recordEnd = checked(elevationEnd + (int)record.StoredNormalBytes);
                if (payload != offset + PlanetaryLocalTerrainPackContract.RecordHeaderBytes || recordEnd > package.Length ||
                    record.GpuElevationBytes != PlanetaryLocalTerrainPackContract.GpuBytes(PlanetaryLocalTerrainGpuFormat.Bc4Unorm, PlanetaryLocalTerrainPackContract.StoredExtent))
                { error = $"Local terrain elevation record {index} payload is invalid."; return false; }
                if (index == 0) { detailFrequency = record.Sector.DetailFrequency; payloadVersion = record.Sector.PayloadVersion; }
                else if (record.Sector.DetailFrequency != detailFrequency || record.Sector.PayloadVersion != payloadVersion)
                { error = $"Local terrain elevation record {index} uses an inconsistent payload identity."; return false; }
                var blocks = new byte[checked((int)record.GpuElevationBytes)]; var stored = package.AsSpan(elevationOffset, checked((int)record.StoredElevationBytes));
                var decoded = record.ElevationCodec == PlanetaryLocalTerrainStorageCodec.RawGpuBlocks
                    ? TryCopy(stored, blocks)
                    : PlanetaryLocalTerrainTranscode.TryDecodePackBits(stored, blocks, out var written) && written == blocks.Length;
                if (!decoded || !residuals.TryAdd(record.Sector, DecodeBc4(blocks)))
                { error = $"Local terrain elevation record {index} could not be decoded."; return false; }
                offset = recordEnd;
            }
            if (offset != package.Length) { error = "Local terrain elevation package has trailing bytes."; return false; }
            lock (Gate) _snapshot ??= new Snapshot(header, detailFrequency, payloadVersion, residuals);
            error = string.Empty; return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OverflowException or OutOfMemoryException)
        { error = $"Local terrain elevation oracle: {exception.Message}"; return false; }
    }

    public static double SampleResidual(in Double3 bodyDirection)
    {
        if (!bodyDirection.IsFinite || bodyDirection.LengthSquared <= 0d) throw new ArgumentOutOfRangeException(nameof(bodyDirection));
        var snapshot = Volatile.Read(ref _snapshot);
        if (snapshot is null || !RelaxedCubeSphereProjection.TryAddress(bodyDirection, out var face, out var faceU, out var faceV)) return 0d;
        for (var level = (int)snapshot.Header.MaximumSectorLevel; level >= snapshot.Header.MinimumSectorLevel; level--)
        {
            var cells = 1 << level; var x = Math.Min((int)Math.Floor(faceU * cells), cells - 1); var y = Math.Min((int)Math.Floor(faceV * cells), cells - 1);
            var id = new PlanetaryLocalTerrainSectorId(snapshot.Header.BodyId, snapshot.Header.TerrainVersion, face, level, x, y,
                snapshot.DetailFrequency, snapshot.PayloadVersion);
            if (!snapshot.Residuals.TryGetValue(id, out var texels)) continue;
            var localU = Math.Clamp(faceU * cells - x, 0d, 1d); var localV = Math.Clamp(faceV * cells - y, 0d, 1d);
            var sampleX = 3.5d + localU * PlanetaryLocalTerrainPackContract.InteriorTexels;
            var sampleY = 3.5d + localV * PlanetaryLocalTerrainPackContract.InteriorTexels;
            var x0 = Math.Clamp((int)Math.Floor(sampleX), 0, PlanetaryLocalTerrainPackContract.StoredExtent - 1);
            var y0 = Math.Clamp((int)Math.Floor(sampleY), 0, PlanetaryLocalTerrainPackContract.StoredExtent - 1);
            var x1 = Math.Min(x0 + 1, PlanetaryLocalTerrainPackContract.StoredExtent - 1); var y1 = Math.Min(y0 + 1, PlanetaryLocalTerrainPackContract.StoredExtent - 1);
            var tx = sampleX - Math.Floor(sampleX); var ty = sampleY - Math.Floor(sampleY); var extent = PlanetaryLocalTerrainPackContract.StoredExtent;
            var a = texels[y0 * extent + x0]; var b = texels[y0 * extent + x1]; var c = texels[y1 * extent + x0]; var d = texels[y1 * extent + x1];
            var encoded = Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty) / 255d;
            return snapshot.Header.ResidualMinimumMetres + encoded * (snapshot.Header.ResidualMaximumMetres - snapshot.Header.ResidualMinimumMetres);
        }
        return 0d;
    }

    private static bool TryCopy(ReadOnlySpan<byte> source, Span<byte> destination)
    { if (source.Length != destination.Length) return false; source.CopyTo(destination); return true; }

    private static byte[] DecodeBc4(ReadOnlySpan<byte> blocks)
    {
        var extent = PlanetaryLocalTerrainPackContract.StoredExtent; var result = new byte[extent * extent]; var blocksPerRow = extent / 4;
        Span<byte> palette = stackalloc byte[8];
        for (var block = 0; block < blocks.Length / 8; block++)
        {
            var source = blocks.Slice(block * 8, 8); palette[0] = source[0]; palette[1] = source[1];
            if (palette[0] > palette[1]) for (var index = 1; index < 7; index++) palette[index + 1] = (byte)(((7 - index) * palette[0] + index * palette[1] + 3) / 7);
            else
            {
                for (var index = 1; index < 5; index++) palette[index + 1] = (byte)(((5 - index) * palette[0] + index * palette[1] + 2) / 5);
                palette[6] = 0; palette[7] = 255;
            }
            ulong indices = 0; for (var index = 0; index < 6; index++) indices |= (ulong)source[index + 2] << (8 * index);
            var blockX = block % blocksPerRow; var blockY = block / blocksPerRow;
            for (var pixel = 0; pixel < 16; pixel++) result[(blockY * 4 + pixel / 4) * extent + blockX * 4 + pixel % 4] = palette[(int)((indices >> (3 * pixel)) & 7)];
        }
        return result;
    }

    private static double Lerp(double first, double second, double amount) => first + (second - first) * amount;
}

public readonly record struct PlanetaryLocalTerrainDemandInput(
    ulong BodyId,
    uint TerrainVersion,
    Double3 PupilDirection,
    Double3 PreviousPupilDirection,
    Double3 ViewDirection,
    double SurfaceAltitudeMetres,
    double BodyRadiusMetres,
    double ViewportHeightPixels,
    double VerticalTanHalfFov)
{
    public bool IsValid => BodyId != 0 && TerrainVersion != 0 && PupilDirection.IsFinite && PupilDirection.LengthSquared > 0d &&
        PreviousPupilDirection.IsFinite && PreviousPupilDirection.LengthSquared > 0d && ViewDirection.IsFinite && ViewDirection.LengthSquared > 0d &&
        double.IsFinite(SurfaceAltitudeMetres) && SurfaceAltitudeMetres >= 0d && double.IsFinite(BodyRadiusMetres) && BodyRadiusMetres > 0d &&
        double.IsFinite(ViewportHeightPixels) && ViewportHeightPixels > 0d && double.IsFinite(VerticalTanHalfFov) && VerticalTanHalfFov > 0d;
}

public readonly record struct PlanetaryLocalTerrainDemand(PlanetaryLocalTerrainSectorId Sector, double Priority, bool Visible, bool Predicted);

/// <summary>Allocation-free, footprint-driven local demand. Available sectors are sparse; the planner never constructs a deep global tree.</summary>
public static class PlanetaryLocalTerrainDemandPlanner
{
    public const double MaximumStreamingAltitudeMetres = 100_000d;
    public const int MaximumDemandCount = 64;

    public static int Plan(in PlanetaryLocalTerrainDemandInput input, ReadOnlySpan<PlanetaryLocalTerrainSectorId> available,
        Span<PlanetaryLocalTerrainDemand> output)
    {
        if (!input.IsValid || output.Length == 0) throw new ArgumentOutOfRangeException();
        if (input.SurfaceAltitudeMetres > MaximumStreamingAltitudeMetres) return 0;
        var pupil = input.PupilDirection.Normalized(); var previous = input.PreviousPupilDirection.Normalized();
        var delta = pupil - previous; var predicted = (pupil + delta * 8d).Normalized();
        var horizon = Math.Acos(Math.Clamp(input.BodyRadiusMetres / (input.BodyRadiusMetres + Math.Max(1d, input.SurfaceAltitudeMetres)), 0d, 1d));
        var pixelAngle = 2d * input.VerticalTanHalfFov / input.ViewportHeightPixels;
        var margin = Math.Max(.002d, pixelAngle * 64d + Math.Acos(Math.Clamp(Double3.Dot(pupil, predicted), -1d, 1d)));
        var footprint = Math.Min(.45d, horizon + margin);
        var count = 0;
        for (var index = 0; index < available.Length && count < Math.Min(output.Length, MaximumDemandCount); index++)
        {
            var sector = available[index];
            if (!sector.IsValid || sector.BodyId != input.BodyId || sector.TerrainVersion != input.TerrainVersion) continue;
            var size = 1 << sector.Level;
            var center = RelaxedCubeSphereProjection.UnitDirection(sector.Face, (sector.X + .5d) / size, (sector.Y + .5d) / size);
            var currentAngle = Math.Acos(Math.Clamp(Double3.Dot(center, pupil), -1d, 1d));
            var futureAngle = Math.Acos(Math.Clamp(Double3.Dot(center, predicted), -1d, 1d));
            var sectorRadius = Math.PI / (Math.Sqrt(3d) * size);
            var visible = currentAngle <= footprint + sectorRadius;
            var future = futureAngle <= footprint + sectorRadius;
            if (!visible && !future) continue;
            var priority = Math.Min(currentAngle, futureAngle + .25d * footprint) + (visible ? 0d : footprint);
            var demand = new PlanetaryLocalTerrainDemand(sector, priority, visible, future);
            var insert = count;
            while (insert > 0 && (output[insert - 1].Priority > priority ||
                output[insert - 1].Priority == priority && output[insert - 1].Sector.CompareTo(sector) > 0))
            { if (insert < output.Length) output[insert] = output[insert - 1]; insert--; }
            if (insert < output.Length) output[insert] = demand;
            if (count < output.Length) count++;
        }
        return count;
    }
}

public enum PlanetaryLocalTerrainSlotState : byte { Empty, Requested, Reading, Ready, Resident }

public readonly record struct PlanetaryLocalTerrainSlot(PlanetaryLocalTerrainSectorId Sector, int Slot, uint Generation);

public readonly record struct PlanetaryLocalTerrainCacheStatistics(
    int Capacity, int Resident, int Pending, long Requests, long Hits, long Misses, long Evictions, long Canceled, ulong BytesRead, ulong BytesTranscoded, ulong BytesUploaded);

/// <summary>Bounded deterministic local-payload LRU with visible/in-flight and stale-generation protection.</summary>
public sealed class PlanetaryLocalTerrainCache
{
    private sealed class Entry
    {
        internal PlanetaryLocalTerrainSectorId Sector;
        internal PlanetaryLocalTerrainSlotState State;
        internal long LastUse;
        internal uint Generation;
        internal bool Visible;
        internal bool InFlight;
    }
    private readonly Entry[] _entries;
    private long _serial, _requests, _hits, _misses, _evictions, _canceled;
    private ulong _bytesRead, _bytesTranscoded, _bytesUploaded;

    public PlanetaryLocalTerrainCache(int capacity = 256)
    {
        if (capacity is not (128 or 256 or 512)) throw new ArgumentOutOfRangeException(nameof(capacity));
        _entries = Enumerable.Range(0, capacity).Select(_ => new Entry()).ToArray();
    }

    public int Capacity => _entries.Length;
    public PlanetaryLocalTerrainCacheStatistics Statistics => new(Capacity,
        _entries.Count(entry => entry.State == PlanetaryLocalTerrainSlotState.Resident),
        _entries.Count(entry => entry.State is PlanetaryLocalTerrainSlotState.Requested or PlanetaryLocalTerrainSlotState.Reading or PlanetaryLocalTerrainSlotState.Ready),
        _requests, _hits, _misses, _evictions, _canceled, _bytesRead, _bytesTranscoded, _bytesUploaded);

    public PlanetaryLocalTerrainSlot Request(in PlanetaryLocalTerrainSectorId sector, bool visible)
    {
        if (!sector.IsValid) throw new ArgumentOutOfRangeException(nameof(sector));
        _serial++; _requests++;
        for (var index = 0; index < _entries.Length; index++)
            if (_entries[index].Sector == sector)
            {
                var hit = _entries[index]; hit.LastUse = _serial; hit.Visible |= visible; _hits++;
                return new(sector, index, hit.Generation);
            }
        _misses++;
        var selected = Array.FindIndex(_entries, entry => entry.State == PlanetaryLocalTerrainSlotState.Empty);
        if (selected < 0)
        {
            long oldest = long.MaxValue;
            for (var index = 0; index < _entries.Length; index++)
            {
                var candidate = _entries[index];
                if (candidate.Visible || candidate.InFlight || candidate.State is PlanetaryLocalTerrainSlotState.Reading or PlanetaryLocalTerrainSlotState.Ready) continue;
                if (candidate.LastUse < oldest) { oldest = candidate.LastUse; selected = index; }
            }
        }
        if (selected < 0) throw new InvalidOperationException("The bounded local terrain cache has no GPU-safe eviction candidate.");
        var entry = _entries[selected]; if (entry.State != PlanetaryLocalTerrainSlotState.Empty) _evictions++;
        entry.Sector = sector; entry.State = PlanetaryLocalTerrainSlotState.Requested; entry.LastUse = _serial; entry.Generation++; entry.Visible = visible; entry.InFlight = false;
        return new(sector, selected, entry.Generation);
    }

    public bool TryBeginRead(in PlanetaryLocalTerrainSlot slot) => Transition(slot, PlanetaryLocalTerrainSlotState.Requested, PlanetaryLocalTerrainSlotState.Reading, true);
    public bool TryCompleteRead(in PlanetaryLocalTerrainSlot slot, uint bytesRead, uint bytesTranscoded)
    {
        if (!Transition(slot, PlanetaryLocalTerrainSlotState.Reading, PlanetaryLocalTerrainSlotState.Ready, false)) return false;
        _bytesRead += bytesRead; _bytesTranscoded += bytesTranscoded; return true;
    }
    public bool TryPublish(in PlanetaryLocalTerrainSlot slot, uint bytesUploaded)
    {
        if (!Transition(slot, PlanetaryLocalTerrainSlotState.Ready, PlanetaryLocalTerrainSlotState.Resident, false)) return false;
        _bytesUploaded += bytesUploaded; return true;
    }
    public bool Owns(in PlanetaryLocalTerrainSlot slot) => (uint)slot.Slot < _entries.Length && _entries[slot.Slot].Sector == slot.Sector && _entries[slot.Slot].Generation == slot.Generation;
    public void BeginFrame() { foreach (var entry in _entries) entry.Visible = false; }
    public bool Cancel(in PlanetaryLocalTerrainSlot slot)
    {
        if (!Owns(slot) || _entries[slot.Slot].State == PlanetaryLocalTerrainSlotState.Resident) return false;
        var entry = _entries[slot.Slot]; entry.State = PlanetaryLocalTerrainSlotState.Empty; entry.Sector = default; entry.InFlight = false; entry.Generation++; _canceled++; return true;
    }

    private bool Transition(in PlanetaryLocalTerrainSlot slot, PlanetaryLocalTerrainSlotState expected, PlanetaryLocalTerrainSlotState next, bool inFlight)
    {
        if (!Owns(slot)) return false;
        var entry = _entries[slot.Slot]; if (entry.State != expected) return false;
        entry.State = next; entry.InFlight = inFlight; return true;
    }
}
