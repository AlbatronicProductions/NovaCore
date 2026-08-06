using NovaCore.EphemerisFormat;

if (args.Length == 1 && args[0] == "--self-test") return RunSelfTest();
if (args.Length is < 2 or > 3 || args[0] != "--synthetic") return Fail("Usage: --synthetic <artifact-path> [manifest-path]");
var result = NcpeV2Codec.TryWrite(SyntheticEphemerisDemo.CreateV2(), out var bytes);
if (result != NcpeV2Status.Success || bytes is null) return Fail($"Build failed: {result}");
try
{
    AtomicWrite(args[1], bytes);
    if (args.Length == 3) AtomicWrite(args[2], System.Text.Encoding.UTF8.GetBytes($"formatVersion=2\nartifactHash={EphemerisHash.Compute(bytes.AsSpan(0, bytes.Length - 16))}\ndefinitionHash={NcpeV2SemanticHash.Compute(SyntheticEphemerisDemo.CreateV2()):X16}\n"));
    Console.WriteLine($"SyntheticEphemerisDemo v2 artifactHash={EphemerisHash.Compute(bytes.AsSpan(0, bytes.Length - 16))}"); return 0;
}
catch (IOException ex) { return Fail($"OutputPathFailure: {ex.Message}"); }

static int RunSelfTest()
{
    var input = SyntheticEphemerisDemo.CreateV2();
    if (NcpeV2Codec.TryWrite(input, out var a) != NcpeV2Status.Success || a is null) return Fail("synthetic build");
    if (NcpeV2Codec.TryWrite(input, out var b) != NcpeV2Status.Success || b is null || !a.AsSpan().SequenceEqual(b)) return Fail("reproducibility");
    if (NcpeV2Codec.TryRead(a, out var parsed, out _, out _) != NcpeV2Status.Success || parsed is null || NcpeV2SemanticHash.Compute(parsed) != NcpeV2SemanticHash.Compute(input)) return Fail("round trip");
    Console.WriteLine($"PASS artifactHash={EphemerisHash.Compute(a.AsSpan(0,a.Length-16))} bytes={a.Length}"); return 0;
}
static void AtomicWrite(string path, byte[] bytes) { var full=Path.GetFullPath(path); var directory=Path.GetDirectoryName(full); if (directory is null) throw new IOException("output has no directory"); Directory.CreateDirectory(directory); var temporary=full+".tmp"; try { File.WriteAllBytes(temporary,bytes); File.Move(temporary,full,true); } finally { if(File.Exists(temporary))File.Delete(temporary); } }
static int Fail(string text) { Console.Error.WriteLine(text); return 2; }

internal static class SyntheticEphemerisDemo
{
    internal static NcpeV2Definition CreateV2()
    {
        var samples = new List<NormalizedEphemerisSample>(); samples.AddRange(Samples(0)); samples.AddRange(Samples(10));
        var system=new NcpeV2System(9001,0,0,1,1,1,71,2,17,-20,20,23,29,1,2,3,4,9002,2,0x5A5A);
        var sources=new[]{new NcpeV2Source(71,0,2,17,-20,20,23,29,1,2,3,4),new NcpeV2Source(72,1,2,17,-20,20,23,29,1,2,3,4)};
        var bodies=new[]{Body(1,"Synthetic Root",0,0),Body(2,"Synthetic Alpha",7,1),Body(3,"Synthetic Beta",7,1)};
        var bindings=new[]{new NcpeV2Binding(1,0,71,0,0,0,0,0,0,0,0,0,0,1,0,0,0),new NcpeV2Binding(2,1,72,0,0,0,0,0,0,0,0,0,0,1,0,0,0),new NcpeV2Binding(3,1,72,1,0,0,0,0,0,0,0,0,0,1,0,0,0)};
        var payloads=new[]{new NcpeV2Payload(17,0,4,1,-20,20),new NcpeV2Payload(17,4,4,1,-20,20)};return new(system,sources,bodies,bindings,payloads,samples);
    }
    private static NcpeV2Body Body(ulong id,string name,byte classification,ulong parent)=>new(id,name,classification,parent,0,0,0,Array.Empty<string>(),1d,1d,1d,1d,0d,0,0,0,0,0);
    internal static NormalizedEphemerisInput Create() => new(9001, 71, 2, 9002, 1, 17, 23, 29, -20, 20, 0xA5A5, 0x5A5A,
    [new(1,0,101,EphemerisInterpolationModel.CubicHermitePositionVelocityV1, Samples(0),0,0), new(2,1,102,EphemerisInterpolationModel.CubicHermitePositionVelocityV1,Samples(10),.01,.001),new(3,1,103,EphemerisInterpolationModel.CubicHermitePositionVelocityV1,Samples(-10),.02,.002)]);
    private static NormalizedEphemerisSample[] Samples(double offset) => [new(-20,offset-20,1,2,1,0,0),new(-5,offset-5,2,3,1,0,0),new(5,offset+5,3,4,1,0,0),new(20,offset+20,4,5,1,0,0)];
}
