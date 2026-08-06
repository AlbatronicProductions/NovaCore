using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace NovaCore.EphemerisFormat;

/// <summary>Version-one, little-endian offline interchange contract. It is not a runtime loader.</summary>
public static class EphemerisArtifactFormat
{
    public const uint Magic = 0x4550434E; // "NCPE", little endian
    public const ushort Version = 1;
    public const uint EndiannessMarker = 0x01020304;
    public const int HeaderSize = 32;
}

public enum EphemerisArtifactStatus : byte
{
    Success, InvalidMagic, InvalidVersion, UnsupportedEndianness, Truncated, SectionLengthOverflow,
    DuplicateSection, MissingRequiredSection, UnknownRequiredSection, InvalidMetadata, DuplicateBody,
    MissingParent, HierarchyCycle, InvalidBodyOrder, UnsupportedInterpolation, InvalidSampleCount,
    NonMonotonicSampleTimes, NonFiniteSample, CoverageMismatch, DomainFrameMismatch, HashMismatch,
    ArtifactLengthMismatch, ManifestMismatch, OutputPathFailure
}

public enum EphemerisInterpolationModel : byte { CubicHermitePositionVelocityV1 = 1 }

public readonly record struct EphemerisHash(ulong Value)
{
    public static EphemerisHash Compute(ReadOnlySpan<byte> bytes) { ulong h = 14695981039346656037UL; foreach (var b in bytes) { h ^= b; h *= 1099511628211UL; } return new(h); }
    public override string ToString() => Value.ToString("X16", CultureInfo.InvariantCulture);
}

public readonly record struct NormalizedEphemerisSample(long DomainTick, double PositionX, double PositionY, double PositionZ, double VelocityX, double VelocityY, double VelocityZ)
{
    public bool IsFinite => double.IsFinite(PositionX) && double.IsFinite(PositionY) && double.IsFinite(PositionZ) && double.IsFinite(VelocityX) && double.IsFinite(VelocityY) && double.IsFinite(VelocityZ);
}

public sealed record NormalizedEphemerisBody(ulong BodyId, ulong ParentId, ulong SourceBodyId, EphemerisInterpolationModel Interpolation, IReadOnlyList<NormalizedEphemerisSample> Samples, double PositionErrorBound, double VelocityErrorBound);
public sealed record NormalizedEphemerisInput(ulong DatasetId, ulong SourceId, ulong SourceVersion, ulong ConverterId, ulong ConverterVersion, ulong TimeDomainId, ulong CoordinateFrameId, ulong ConstantsVersionId, long CoverageStart, long CoverageEnd, ulong AuthoredModificationHash, ulong ConversionPolicyHash, IReadOnlyList<NormalizedEphemerisBody> Bodies);
public sealed record EphemerisArtifactManifest(ulong DatasetId, ushort FormatVersion, ulong ConverterId, ulong ConverterVersion, ulong SourceId, ulong SourceVersion, ulong TimeDomainId, ulong CoordinateFrameId, ulong ConstantsVersionId, long CoverageStart, long CoverageEnd, int BodyCount, int SampleCount, EphemerisHash SourceHash, EphemerisHash PolicyHash, EphemerisHash CatalogHash, EphemerisHash HierarchyHash, EphemerisHash PayloadHash, EphemerisHash ArtifactHash, ulong AuthoredModificationHash);
public sealed record EphemerisArtifact(EphemerisArtifactManifest Manifest, IReadOnlyList<NormalizedEphemerisBody> Bodies, byte[] Bytes);

public static class EphemerisArtifactCodec
{
    private const uint MetadataSection = 1, BodiesSection = 2, PayloadSection = 3, HashSection = 4;

    public static EphemerisArtifactStatus TryBuild(NormalizedEphemerisInput input, out EphemerisArtifact? artifact)
    {
        artifact = null;
        if (!Validate(input, out var ordered)) return EphemerisArtifactStatus.InvalidMetadata;
        var metadata = WriteMetadata(input, ordered);
        var bodies = WriteBodies(ordered);
        var payload = WritePayload(ordered);
        var sourceHash = EphemerisHash.Compute(metadata);
        var catalogHash = EphemerisHash.Compute(bodies);
        var payloadHash = EphemerisHash.Compute(payload);
        var hierarchyHash = HashHierarchy(ordered);
        var hashSection = new byte[48];
        WriteUInt64(hashSection, 0, sourceHash.Value); WriteUInt64(hashSection, 8, input.ConversionPolicyHash); WriteUInt64(hashSection, 16, catalogHash.Value); WriteUInt64(hashSection, 24, hierarchyHash.Value); WriteUInt64(hashSection, 32, payloadHash.Value);
        var raw = Assemble(metadata, bodies, payload, hashSection);
        var artifactHash = EphemerisHash.Compute(raw.AsSpan(0, raw.Length - 8)); WriteUInt64(raw, raw.Length - 8, artifactHash.Value);
        var count = 0; foreach (var body in ordered) count += body.Samples.Count;
        var manifest = new EphemerisArtifactManifest(input.DatasetId, EphemerisArtifactFormat.Version, input.ConverterId, input.ConverterVersion, input.SourceId, input.SourceVersion, input.TimeDomainId, input.CoordinateFrameId, input.ConstantsVersionId, input.CoverageStart, input.CoverageEnd, ordered.Length, count, sourceHash, new(input.ConversionPolicyHash), catalogHash, hierarchyHash, payloadHash, artifactHash, input.AuthoredModificationHash);
        artifact = new(manifest, ordered, raw); return EphemerisArtifactStatus.Success;
    }

    public static EphemerisArtifactStatus TryRead(ReadOnlySpan<byte> bytes, out EphemerisArtifact? artifact)
    {
        artifact = null;
        if (bytes.Length < EphemerisArtifactFormat.HeaderSize + 8) return EphemerisArtifactStatus.Truncated;
        if (ReadUInt32(bytes, 0) != EphemerisArtifactFormat.Magic) return EphemerisArtifactStatus.InvalidMagic;
        if (ReadUInt16(bytes, 4) != EphemerisArtifactFormat.Version) return EphemerisArtifactStatus.InvalidVersion;
        if (ReadUInt32(bytes, 8) != EphemerisArtifactFormat.EndiannessMarker) return EphemerisArtifactStatus.UnsupportedEndianness;
        var total = ReadUInt32(bytes, 12); if (total != bytes.Length) return EphemerisArtifactStatus.ArtifactLengthMismatch;
        if (EphemerisHash.Compute(bytes[..^8]).Value != ReadUInt64(bytes, bytes.Length - 8)) return EphemerisArtifactStatus.HashMismatch;
        var sections = new Dictionary<uint, (int Offset, int Length)>(); var cursor = EphemerisArtifactFormat.HeaderSize; var sectionCount = ReadUInt32(bytes, 16);
        for (var i = 0u; i < sectionCount; i++) { if (cursor > bytes.Length - 8 - 8) return EphemerisArtifactStatus.Truncated; var kind = ReadUInt32(bytes, cursor); var length = ReadUInt32(bytes, cursor + 4); cursor += 8; if (length > bytes.Length - 8 - cursor) return EphemerisArtifactStatus.SectionLengthOverflow; if (!sections.TryAdd(kind, (cursor, (int)length))) return EphemerisArtifactStatus.DuplicateSection; cursor += (int)length; }
        if (cursor != bytes.Length - 8 || !sections.ContainsKey(MetadataSection) || !sections.ContainsKey(BodiesSection) || !sections.ContainsKey(PayloadSection) || !sections.ContainsKey(HashSection)) return EphemerisArtifactStatus.MissingRequiredSection;
        if (sections.Keys.Any(static x => x is < MetadataSection or > HashSection)) return EphemerisArtifactStatus.UnknownRequiredSection;
        var metadata = sections[MetadataSection]; var bodyData = sections[BodiesSection]; var payload = sections[PayloadSection];
        return TryDeserialize(bytes.Slice(metadata.Offset, metadata.Length), bytes.Slice(bodyData.Offset, bodyData.Length), bytes.Slice(payload.Offset, payload.Length), bytes.ToArray(), out artifact);
    }

    public static string CreateManifestText(EphemerisArtifactManifest m) => string.Create(CultureInfo.InvariantCulture, $"formatVersion={m.FormatVersion}\ndatasetId={m.DatasetId}\nconverter={m.ConverterId}:{m.ConverterVersion}\nsource={m.SourceId}:{m.SourceVersion}\ntimeDomain={m.TimeDomainId}\ncoordinateFrame={m.CoordinateFrameId}\nconstantsVersion={m.ConstantsVersionId}\ncoverage={m.CoverageStart}:{m.CoverageEnd}\nbodies={m.BodyCount}\nsamples={m.SampleCount}\nsourceHash={m.SourceHash}\npolicyHash={m.PolicyHash}\ncatalogHash={m.CatalogHash}\nhierarchyHash={m.HierarchyHash}\npayloadHash={m.PayloadHash}\nartifactHash={m.ArtifactHash}\nauthoredModificationHash={m.AuthoredModificationHash:X16}\n");

    private static bool Validate(NormalizedEphemerisInput input, out NormalizedEphemerisBody[] ordered)
    {
        ordered = input.Bodies.OrderBy(static x => x.BodyId).ToArray();
        if (input.DatasetId == 0 || input.TimeDomainId == 0 || input.CoordinateFrameId == 0 || input.CoverageStart > input.CoverageEnd || ordered.Length == 0) return false;
        ulong previous = 0; var roots = 0;
        foreach (var body in ordered) { if (body.BodyId == 0 || body.BodyId == previous || body.Samples.Count < 2 || !double.IsFinite(body.PositionErrorBound) || !double.IsFinite(body.VelocityErrorBound)) return false; previous = body.BodyId; if (body.ParentId == 0) roots++; else if (Array.BinarySearch(ordered.Select(static b => b.BodyId).ToArray(), body.ParentId) < 0) return false; long last = long.MinValue; foreach (var s in body.Samples) { if (!s.IsFinite || s.DomainTick <= last || s.DomainTick < input.CoverageStart || s.DomainTick > input.CoverageEnd) return false; last = s.DomainTick; } }
        if (roots != 1) return false; foreach (var b in ordered) { var cursor = b; for (var i = 0; cursor.ParentId != 0 && i < ordered.Length; i++) { var parent = Array.Find(ordered, x => x.BodyId == cursor.ParentId); if (parent is null) return false; cursor = parent; } if (cursor.ParentId != 0) return false; } return true;
    }
    private static byte[] WriteMetadata(NormalizedEphemerisInput x, IReadOnlyList<NormalizedEphemerisBody> _) { var b = new byte[96]; var values = new ulong[] { x.DatasetId, x.SourceId, x.SourceVersion, x.ConverterId, x.ConverterVersion, x.TimeDomainId, x.CoordinateFrameId, x.ConstantsVersionId, unchecked((ulong)x.CoverageStart), unchecked((ulong)x.CoverageEnd), x.AuthoredModificationHash, x.ConversionPolicyHash }; for (var i = 0; i < values.Length; i++) WriteUInt64(b, i * 8, values[i]); return b; }
    private static byte[] WriteBodies(IReadOnlyList<NormalizedEphemerisBody> bodies) { var b = new byte[4 + bodies.Count * 64]; WriteUInt32(b, 0, (uint)bodies.Count); for (var i = 0; i < bodies.Count; i++) { var o = 4 + i * 64; var x = bodies[i]; WriteUInt64(b,o,x.BodyId); WriteUInt64(b,o+8,x.ParentId); WriteUInt64(b,o+16,x.SourceBodyId); b[o+24]=(byte)x.Interpolation; WriteUInt32(b,o+28,(uint)x.Samples.Count); WriteUInt64(b,o+32,unchecked((ulong)x.Samples[0].DomainTick)); WriteUInt64(b,o+40,unchecked((ulong)x.Samples[^1].DomainTick)); WriteDouble(b,o+48,x.PositionErrorBound); WriteDouble(b,o+56,x.VelocityErrorBound); } return b; }
    private static byte[] WritePayload(IReadOnlyList<NormalizedEphemerisBody> bodies) { var count = bodies.Sum(static x => x.Samples.Count); var b = new byte[4 + count * 64]; WriteUInt32(b,0,(uint)count); var o=4; foreach(var body in bodies) foreach(var s in body.Samples) { WriteUInt64(b,o,unchecked((ulong)s.DomainTick)); WriteDouble(b,o+8,s.PositionX);WriteDouble(b,o+16,s.PositionY);WriteDouble(b,o+24,s.PositionZ);WriteDouble(b,o+32,s.VelocityX);WriteDouble(b,o+40,s.VelocityY);WriteDouble(b,o+48,s.VelocityZ); WriteUInt64(b,o+56,body.BodyId);o+=64;} return b; }
    private static byte[] Assemble(params byte[][] sections) { var length=EphemerisArtifactFormat.HeaderSize+8; foreach(var s in sections) length+=8+s.Length; var b=new byte[length]; WriteUInt32(b,0,EphemerisArtifactFormat.Magic);WriteUInt16(b,4,EphemerisArtifactFormat.Version);WriteUInt32(b,8,EphemerisArtifactFormat.EndiannessMarker);WriteUInt32(b,12,(uint)length);WriteUInt32(b,16,(uint)sections.Length); var kinds=new[]{MetadataSection,BodiesSection,PayloadSection,HashSection};var o=EphemerisArtifactFormat.HeaderSize;for(var i=0;i<sections.Length;i++){WriteUInt32(b,o,kinds[i]);WriteUInt32(b,o+4,(uint)sections[i].Length);o+=8;sections[i].CopyTo(b,o);o+=sections[i].Length;} return b; }
    private static EphemerisArtifactStatus TryDeserialize(ReadOnlySpan<byte> metadata, ReadOnlySpan<byte> bodies, ReadOnlySpan<byte> payload, byte[] raw, out EphemerisArtifact? artifact) { artifact=null; if(metadata.Length!=96||bodies.Length<4||payload.Length<4) return EphemerisArtifactStatus.Truncated; var bodyCount=(int)ReadUInt32(bodies,0);if(bodies.Length!=4+bodyCount*64) return EphemerisArtifactStatus.SectionLengthOverflow;var ps=(int)ReadUInt32(payload,0);if(payload.Length!=4+ps*64)return EphemerisArtifactStatus.SectionLengthOverflow;var list=new List<NormalizedEphemerisBody>(bodyCount);for(var i=0;i<bodyCount;i++){var o=4+i*64;var id=ReadUInt64(bodies,o);var samples=new List<NormalizedEphemerisSample>();for(var j=0;j<ps;j++){var p=4+j*64;if(ReadUInt64(payload,p+56)!=id)continue;samples.Add(new(unchecked((long)ReadUInt64(payload,p)),ReadDouble(payload,p+8),ReadDouble(payload,p+16),ReadDouble(payload,p+24),ReadDouble(payload,p+32),ReadDouble(payload,p+40),ReadDouble(payload,p+48)));}list.Add(new(id,ReadUInt64(bodies,o+8),ReadUInt64(bodies,o+16),(EphemerisInterpolationModel)bodies[o+24],samples,ReadDouble(bodies,o+48),ReadDouble(bodies,o+56)));}var input=new NormalizedEphemerisInput(ReadUInt64(metadata,0),ReadUInt64(metadata,8),ReadUInt64(metadata,16),ReadUInt64(metadata,24),ReadUInt64(metadata,32),ReadUInt64(metadata,40),ReadUInt64(metadata,48),ReadUInt64(metadata,56),unchecked((long)ReadUInt64(metadata,64)),unchecked((long)ReadUInt64(metadata,72)),ReadUInt64(metadata,80),ReadUInt64(metadata,88),list);var status=TryBuild(input,out var rebuilt);if(status!=EphemerisArtifactStatus.Success||rebuilt is null)return status;if(!raw.AsSpan().SequenceEqual(rebuilt.Bytes))return EphemerisArtifactStatus.HashMismatch;artifact=rebuilt;return EphemerisArtifactStatus.Success; }
    private static EphemerisHash HashHierarchy(IEnumerable<NormalizedEphemerisBody> bodies){ulong hash=14695981039346656037UL;foreach(var x in bodies){hash=Mix(hash,x.BodyId);hash=Mix(hash,x.ParentId);}return new(hash);}
    private static ulong Mix(ulong hash, ulong value) { for (var i=0;i<8;i++) { hash ^= (byte)(value >> (i * 8)); hash *= 1099511628211UL; } return hash; }
    private static uint ReadUInt32(ReadOnlySpan<byte> b,int o)=>BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(o,4)); private static ushort ReadUInt16(ReadOnlySpan<byte>b,int o)=>BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(o,2));private static ulong ReadUInt64(ReadOnlySpan<byte>b,int o)=>BinaryPrimitives.ReadUInt64LittleEndian(b.Slice(o,8));private static double ReadDouble(ReadOnlySpan<byte>b,int o)=>BitConverter.Int64BitsToDouble(unchecked((long)ReadUInt64(b,o)));private static void WriteUInt32(byte[]b,int o,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(o,4),v);private static void WriteUInt16(byte[]b,int o,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(o,2),v);private static void WriteUInt64(byte[]b,int o,ulong v)=>BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(o,8),v);private static void WriteDouble(byte[]b,int o,double v)=>WriteUInt64(b,o,unchecked((ulong)BitConverter.DoubleToInt64Bits(v)));
}
