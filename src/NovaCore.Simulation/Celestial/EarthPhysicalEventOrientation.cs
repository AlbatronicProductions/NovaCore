using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

internal static partial class CelestialBodyOrientationEvaluator
{
    // Reuses the existing model table. Only the current linear Earth model is supported.
    // Canonical requests never enter this sidecar.
    internal static bool TryEvaluateEarthLocal(SimulationInstant wholeSecond, double h,
        out DoubleQuaternion orientation, out Double3 angularVelocity)
    {
        orientation = default; angularVelocity = default;
        if (!TryFind(SolarSystemBodyIds.Earth, out var model) || model.Kind != ModelKind.Linear || model.Wt2 != 0 ||
            wholeSecond.Ticks % SimulationInstant.TicksPerSecond != 0 || !double.IsFinite(h) || h < 0 || h > 1) return false;
        var seconds = wholeSecond.Ticks / SimulationInstant.TicksPerSecond;
        var q0 = EvaluateCore(model, seconds);
        var centuries = (seconds / SecondsPerDay) / DaysPerCentury;
        var ra = (model.Ra0 + model.RaT * centuries + 90) * DegreesToRadians;
        var u0 = DoubleQuaternion.FromAxisAngle(Double3.UnitZ, ra).Rotate(Double3.UnitX);
        var a = model.RaT * DegreesToRadians / (DaysPerCentury * SecondsPerDay);
        var b = -model.DecT * DegreesToRadians / (DaysPerCentury * SecondsPerDay);
        var c = model.Wd * DegreesToRadians / SecondsPerDay;
        var za = DoubleQuaternion.FromAxisAngle(Double3.UnitZ, a * h);
        var qb = DoubleQuaternion.FromAxisAngle(u0, b * h);
        var qc = DoubleQuaternion.FromAxisAngle(Double3.UnitY, c * h);
        var q = (za * qb * q0 * qc).Normalized();
        if (q.W < 0) q = new(-q.X, -q.Y, -q.Z, -q.W);

        // Algebraic Q(E+.5) Q(E-.5)^-1, with large absolute spin phase cancelled.
        // Preserve the existing finite-stencil meaning, not an instantaneous/certified derivative.
        var z = DoubleQuaternion.FromAxisAngle(Double3.UnitZ, a * .5);
        var u = DoubleQuaternion.FromAxisAngle(za.Rotate(u0), b * .5);
        var n = DoubleQuaternion.FromAxisAngle(q.Rotate(Double3.UnitY), c);
        var delta = (z * u * n * u * z).Normalized();
        if (delta.W < 0) delta = new(-delta.X, -delta.Y, -delta.Z, -delta.W);
        var vector = new Double3(delta.X, delta.Y, delta.Z);
        var length = Math.Sqrt(vector.LengthSquared);
        var omega = length <= 1e-18 ? Double3.Zero : vector * (2 * Math.Atan2(length, delta.W) / length);
        if (!q.IsFinite || !omega.IsFinite) return false;
        orientation = q; angularVelocity = omega;
        return true;
    }
}
