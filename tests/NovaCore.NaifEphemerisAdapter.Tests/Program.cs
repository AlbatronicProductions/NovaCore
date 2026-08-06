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
Console.WriteLine("CSPICE diagnostic/reset proof: PASS");
static void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
