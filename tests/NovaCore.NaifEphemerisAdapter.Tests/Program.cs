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
static void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
static CspiceSourceState ToSi(CspiceSourceState s) => new(s.X * 1000, s.Y * 1000, s.Z * 1000, s.Vx * 1000, s.Vy * 1000, s.Vz * 1000);
static CspiceSourceState Relative(CspiceSourceState child, CspiceSourceState parent) => new(child.X-parent.X,child.Y-parent.Y,child.Z-parent.Z,child.Vx-parent.Vx,child.Vy-parent.Vy,child.Vz-parent.Vz);
static CspiceSourceState Reconstruct(CspiceSourceState parent, CspiceSourceState relative) => new(parent.X+relative.X,parent.Y+relative.Y,parent.Z+relative.Z,parent.Vx+relative.Vx,parent.Vy+relative.Vy,parent.Vz+relative.Vz);
