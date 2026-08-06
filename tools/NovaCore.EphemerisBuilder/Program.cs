using NovaCore.EphemerisFormat;

if (args.Length == 1 && args[0] == "--self-test") return RunSelfTest();
if (args.Length is < 2 or > 3 || args[0] != "--synthetic") return Fail("Usage: --synthetic <artifact-path> [manifest-path]");
var result = EphemerisArtifactCodec.TryBuild(SyntheticEphemerisDemo.Create(), out var artifact);
if (result != EphemerisArtifactStatus.Success || artifact is null) return Fail($"Build failed: {result}");
try
{
    AtomicWrite(args[1], artifact.Bytes);
    if (args.Length == 3) AtomicWrite(args[2], System.Text.Encoding.UTF8.GetBytes(EphemerisArtifactCodec.CreateManifestText(artifact.Manifest)));
    Console.WriteLine($"SyntheticEphemerisDemo artifactHash={artifact.Manifest.ArtifactHash}"); return 0;
}
catch (IOException ex) { return Fail($"OutputPathFailure: {ex.Message}"); }

static int RunSelfTest()
{
    var input = SyntheticEphemerisDemo.Create();
    if (EphemerisArtifactCodec.TryBuild(input, out var a) != EphemerisArtifactStatus.Success || a is null) return Fail("synthetic build");
    if (EphemerisArtifactCodec.TryBuild(input, out var b) != EphemerisArtifactStatus.Success || b is null || !a.Bytes.AsSpan().SequenceEqual(b.Bytes)) return Fail("reproducibility");
    if (EphemerisArtifactCodec.TryRead(a.Bytes, out var parsed) != EphemerisArtifactStatus.Success || parsed is null || !parsed.Bytes.AsSpan().SequenceEqual(a.Bytes)) return Fail("round trip");
    Console.WriteLine($"PASS artifactHash={a.Manifest.ArtifactHash} bytes={a.Bytes.Length}"); return 0;
}
static void AtomicWrite(string path, byte[] bytes) { var full=Path.GetFullPath(path); var directory=Path.GetDirectoryName(full); if (directory is null) throw new IOException("output has no directory"); Directory.CreateDirectory(directory); var temporary=full+".tmp"; try { File.WriteAllBytes(temporary,bytes); File.Move(temporary,full,true); } finally { if(File.Exists(temporary))File.Delete(temporary); } }
static int Fail(string text) { Console.Error.WriteLine(text); return 2; }

internal static class SyntheticEphemerisDemo
{
    internal static NormalizedEphemerisInput Create() => new(9001, 71, 2, 9002, 1, 17, 23, 29, -20, 20, 0xA5A5, 0x5A5A,
    [new(1,0,101,EphemerisInterpolationModel.CubicHermitePositionVelocityV1, Samples(0),0,0), new(2,1,102,EphemerisInterpolationModel.CubicHermitePositionVelocityV1,Samples(10),.01,.001),new(3,1,103,EphemerisInterpolationModel.CubicHermitePositionVelocityV1,Samples(-10),.02,.002)]);
    private static NormalizedEphemerisSample[] Samples(double offset) => [new(-20,offset-20,1,2,1,0,0),new(-5,offset-5,2,3,1,0,0),new(5,offset+5,3,4,1,0,0),new(20,offset+20,4,5,1,0,0)];
}
