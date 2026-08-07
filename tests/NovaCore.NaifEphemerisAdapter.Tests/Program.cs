using NovaCore.NaifEphemerisAdapter;

var root = Environment.CurrentDirectory;
var shim = Path.Combine(root, "external", "naif", "build", "cspice-shim", "NovaCore.CSpiceShim.dll");
var kernels = new[] { "de440.bsp", "gm_de440.tpc", "pck00010.tpc", "naif0012.tls" }
    .Select(x => Path.Combine(root, "external", "naif", "kernels", x)).ToArray();
Check(CspiceSession.TryCreate(shim, out var session, out _), "explicit shim load");
var active = session ?? throw new InvalidOperationException("session missing after success");
using (active)
{
    Check(active.TryLoadKernels(kernels), "canonical kernel load");
    Check(!active.TryQuery(999999, 0, out var failed, out var diagnostic), "invalid target rejected");
    Check(failed == default, "failed query default state");
    Check(diagnostic.Status == CspiceSessionStatus.QueryFailure && diagnostic.Operation == "query", "query diagnostic status");
    Check(diagnostic.ShortMessage.Length > 0 && diagnostic.LongMessage.Length > 0, "short and long diagnostics");
    Check(active.TryQuery(10, 0, out var sun, out _), "valid query after reset");
    Check(double.IsFinite(sun.X) && Math.Abs(sun.X + 1067706.8053809535) < 1e-6, "Sun ET=0 km state");
    Check(active.Clear(), "kernel clear");
}
Check(CspiceSession.TryCreate(shim, out session, out _), "extraction session");
active = session ?? throw new InvalidOperationException("extraction session missing");
using (active)
{
    Check(active.TryLoadKernels(kernels), "extraction kernel load");
    foreach (var et in new[] { -86400d, 0d, 86400d })
    {
        var states = new Dictionary<int, CspiceSourceState>();
        foreach (var id in new[] { 0, 10, 3, 399, 301 })
        {
            Check(active.TryQuery(id, et, out var state, out _), $"state {id} {et}");
            Check(double.IsFinite(state.X) && double.IsFinite(state.Vz), $"finite {id} {et}");
            states.Add(id, ToSi(state));
        }
        Check(states[0] == default, $"SSB zero {et}");
        Check(Reconstruct(states[0], Relative(states[10], states[0])) == states[10], $"Sun reconstruction {et}");
        Check(Reconstruct(states[0], Relative(states[3], states[0])) == states[3], $"EMB reconstruction {et}");
        Check(Reconstruct(states[3], Relative(states[399], states[3])) == states[399], $"Earth reconstruction {et}");
        Check(Reconstruct(states[3], Relative(states[301], states[3])) == states[301], $"Moon reconstruction {et}");
        Console.WriteLine($"ET={et:F0} Sun SI=({states[10].X:R},{states[10].Y:R},{states[10].Z:R})");
    }
    Check(active.Clear(), "extraction clear");
}
Console.WriteLine("CSPICE diagnostic/reset and DE440 state validation: PASS");
Check(CspiceSession.TryCreate(shim, out session, out _), "sampler session");
active = session ?? throw new InvalidOperationException("sampler session missing");
using (active)
{
    Check(active.TryLoadKernels(kernels), "sampler kernel load");
    var config = new AdaptiveSamplingConfig(10, .001, 1, 20);
    Check(AdaptiveHermiteSampler.TrySample(active, 301, 3, 0, 86400, 21600, config, out var first), "Moon sample");
    Check(AdaptiveHermiteSampler.TrySample(active, 301, 3, 0, 86400, 21600, config, out var second), "Moon repeat");
    Check(first.Knots.Length == 5, "Moon accepted knot count");
    Check(first.Knots[0].Et == 0 && first.Knots[^1].Et == 86400, "exact coverage");
    Check(first.Knots.Zip(first.Knots.Skip(1)).All(p => p.First.Et < p.Second.Et), "strict knots");
    Check(double.IsFinite(first.RmsPositionError) && first.RmsPositionError >= 0, "finite Moon RMS position");
    Check(double.IsFinite(first.RmsVelocityError) && first.RmsVelocityError >= 0, "finite Moon RMS velocity");
    Check(first.WorstPositionErrorEt is >= 0 and <= 86400, "Moon worst position ET coverage");
    Check(first.WorstVelocityErrorEt is >= 0 and <= 86400, "Moon worst velocity ET coverage");
    Check(first.RmsPositionError == second.RmsPositionError && first.RmsVelocityError == second.RmsVelocityError && first.WorstPositionErrorEt == second.WorstPositionErrorEt && first.WorstVelocityErrorEt == second.WorstVelocityErrorEt, "deterministic Moon RMS and worst ETs");
    var hash = Hash(first); Check(hash == Hash(second) && first.Knots.SequenceEqual(second.Knots), "deterministic Moon result");
    Console.WriteLine($"Moon adaptive: knots={first.Knots.Length} rmsPosition={first.RmsPositionError:R} rmsVelocity={first.RmsVelocityError:R} maxPosition={first.MaxPositionError:R} worstPositionEt={first.WorstPositionErrorEt} maxVelocity={first.MaxVelocityError:R} worstVelocityEt={first.WorstVelocityErrorEt} depth={first.MaximumDepth} hash=0x{hash:X16}");
    var search = new AdaptiveSeedCadenceSearchConfig(3600, 86400, 60);
    Check(AdaptiveHermiteSampler.TryFindLargestUniformSeed(active, 301, 3, 0, 86400, config, search, out var firstCadence), "Moon seed-cadence search");
    Check(AdaptiveHermiteSampler.TryFindLargestUniformSeed(active, 301, 3, 0, 86400, config, search, out var secondCadence), "Moon seed-cadence repeat");
    var cadenceHash = Hash(firstCadence.Sample);
    Check(firstCadence.LargestPassingSeedSeconds == secondCadence.LargestPassingSeedSeconds && firstCadence.NextFailingSeedSeconds == secondCadence.NextFailingSeedSeconds && cadenceHash == Hash(secondCadence.Sample) && Same(firstCadence.Sample, secondCadence.Sample), "deterministic Moon seed-cadence result");
    Check(firstCadence.NextFailingSeedSeconds == firstCadence.LargestPassingSeedSeconds + 60, "Moon next failing cadence");
    Console.WriteLine($"Moon cadence: largestPassing={firstCadence.LargestPassingSeedSeconds} nextFailing={firstCadence.NextFailingSeedSeconds} samples={firstCadence.Sample.Knots.Length} intervals={firstCadence.Sample.Knots.Length-1} maxPosition={firstCadence.Sample.MaxPositionError:R} rmsPosition={firstCadence.Sample.RmsPositionError:R} maxVelocity={firstCadence.Sample.MaxVelocityError:R} rmsVelocity={firstCadence.Sample.RmsVelocityError:R} hash=0x{cadenceHash:X16}");
    Check(active.Clear(), "sampler clear");
}
static void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
static CspiceSourceState ToSi(CspiceSourceState s) => new(s.X * 1000, s.Y * 1000, s.Z * 1000, s.Vx * 1000, s.Vy * 1000, s.Vz * 1000);
static CspiceSourceState Relative(CspiceSourceState child, CspiceSourceState parent) => new(child.X-parent.X,child.Y-parent.Y,child.Z-parent.Z,child.Vx-parent.Vx,child.Vy-parent.Vy,child.Vz-parent.Vz);
static CspiceSourceState Reconstruct(CspiceSourceState parent, CspiceSourceState relative) => new(parent.X+relative.X,parent.Y+relative.Y,parent.Z+relative.Z,parent.Vx+relative.Vx,parent.Vy+relative.Vy,parent.Vz+relative.Vz);
static bool Same(AdaptiveSamplingResult a,AdaptiveSamplingResult b)=>a.BodyId==b.BodyId&&a.RmsPositionError==b.RmsPositionError&&a.RmsVelocityError==b.RmsVelocityError&&a.MaxPositionError==b.MaxPositionError&&a.WorstPositionErrorEt==b.WorstPositionErrorEt&&a.MaxVelocityError==b.MaxVelocityError&&a.WorstVelocityErrorEt==b.WorstVelocityErrorEt&&a.MaximumDepth==b.MaximumDepth&&a.Knots.SequenceEqual(b.Knots);
static ulong Hash(AdaptiveSamplingResult r){ulong h=14695981039346656037;void Add(long x){unchecked{h^=(ulong)x;h*=1099511628211;}}Add(r.BodyId);Add(BitConverter.DoubleToInt64Bits(r.RmsPositionError));Add(BitConverter.DoubleToInt64Bits(r.RmsVelocityError));Add(BitConverter.DoubleToInt64Bits(r.MaxPositionError));Add(r.WorstPositionErrorEt);Add(BitConverter.DoubleToInt64Bits(r.MaxVelocityError));Add(r.WorstVelocityErrorEt);Add(r.MaximumDepth);foreach(var k in r.Knots){Add(k.Et);Add(BitConverter.DoubleToInt64Bits(k.State.X));Add(BitConverter.DoubleToInt64Bits(k.State.Y));Add(BitConverter.DoubleToInt64Bits(k.State.Z));}return h;}
