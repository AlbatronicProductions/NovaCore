using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Pure bounded elliptic universal-variable f/g propagation for canonical Cartesian epoch state.</summary>
internal static class UniversalVariableTwoBodyPropagator
{
    internal const long MaximumEvaluationTicks = 2_147_483_648_000_000L;
    internal const double NearParabolicThreshold = 1e-10d;
    internal const int MaximumBracketExpansions = 64;
    internal const int MaximumSolveIterations = 48;
    private const double SeriesThreshold = 1e-4d;
    private const double RelativeResidualTolerance = 3.552713678800501e-15d; // 2^-48
    private const double DegenerateAngularMomentumRelativeThreshold = 1e-24d;

    internal static TwoBodyPropagationResult TryEvaluate(in CartesianState stateAtEpoch, SimulationInstant epoch, SimulationInstant requestedTime, double gravitationalParameter) =>
        TryEvaluateCore(stateAtEpoch, epoch, requestedTime, gravitationalParameter, MaximumSolveIterations);

    // Narrow deterministic test seam; it does not alter production limits or global configuration.
    internal static TwoBodyPropagationResult TryEvaluateWithIterationLimitForTest(in CartesianState stateAtEpoch, SimulationInstant epoch, SimulationInstant requestedTime, double gravitationalParameter, int maximumIterations) =>
        maximumIterations is > 0 and <= MaximumSolveIterations
            ? TryEvaluateCore(stateAtEpoch, epoch, requestedTime, gravitationalParameter, maximumIterations)
            : Failure(TwoBodyPropagationStatus.NonConvergent, requestedTime, 0);

    internal static bool TryEvaluateStumpffForTest(double z, out double c, out double s) => TryEvaluateStumpff(z, out c, out s);

    private static TwoBodyPropagationResult TryEvaluateCore(in CartesianState stateAtEpoch, SimulationInstant epoch, SimulationInstant requestedTime, double mu, int maximumIterations)
    {
        if (!stateAtEpoch.IsFinite) return Failure(TwoBodyPropagationStatus.NonFiniteState, requestedTime, 0);
        if (!double.IsFinite(mu) || mu <= 0d) return Failure(TwoBodyPropagationStatus.InvalidGravitationalParameter, requestedTime, 0);
        long deltaTicks;
        try { deltaTicks = (requestedTime - epoch).Ticks; }
        catch (OverflowException) { return Failure(TwoBodyPropagationStatus.EvaluationSpanExceeded, requestedTime, 0); }
        if (deltaTicks < -MaximumEvaluationTicks || deltaTicks > MaximumEvaluationTicks) return Failure(TwoBodyPropagationStatus.EvaluationSpanExceeded, requestedTime, 0);

        var r0 = stateAtEpoch.Position; var v0 = stateAtEpoch.Velocity;
        var r0Squared = r0.LengthSquared; var v0Squared = v0.LengthSquared;
        if (!double.IsFinite(r0Squared) || !double.IsFinite(v0Squared)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        var radius0 = Math.Sqrt(r0Squared);
        if (!double.IsFinite(radius0) || radius0 <= 0d) return Failure(TwoBodyPropagationStatus.DegenerateRadius, requestedTime, 0);
        var angularMomentumSquared = Double3.Cross(r0, v0).LengthSquared;
        var angularMomentumScale = r0Squared * v0Squared;
        if (!double.IsFinite(angularMomentumSquared) || !double.IsFinite(angularMomentumScale)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        if (angularMomentumSquared <= angularMomentumScale * DegenerateAngularMomentumRelativeThreshold) return Failure(TwoBodyPropagationStatus.DegenerateAngularMomentum, requestedTime, 0);

        var alpha = 2d / radius0 - v0Squared / mu;
        var classification = alpha * radius0;
        if (!double.IsFinite(alpha) || !double.IsFinite(classification)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        if (classification < -NearParabolicThreshold) return Failure(TwoBodyPropagationStatus.HyperbolicUnsupported, requestedTime, 0);
        if (Math.Abs(classification) <= NearParabolicThreshold) return Failure(TwoBodyPropagationStatus.NearParabolicUnsupported, requestedTime, 0);
        if (deltaTicks == 0) return new(TwoBodyPropagationStatus.Success, requestedTime, stateAtEpoch, 0);

        var deltaSeconds = deltaTicks / (double)SimulationInstant.TicksPerSecond;
        var sqrtMu = Math.Sqrt(mu);
        if (!double.IsFinite(deltaSeconds) || !double.IsFinite(sqrtMu) || sqrtMu <= 0d) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        // Elliptic state repeats after its derived period. Reducing only the solver interval keeps large exact-time
        // requests numerically conditioned without changing the requested authoritative timestamp or stored epoch.
        var meanMotion = sqrtMu * alpha * Math.Sqrt(alpha);
        var period = 2d * Math.PI / meanMotion;
        if (!double.IsFinite(meanMotion) || !double.IsFinite(period) || period <= 0d) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        if (Math.Abs(deltaSeconds) > period) deltaSeconds = Math.IEEERemainder(deltaSeconds, period);
        if (!double.IsFinite(deltaSeconds)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        var radialVelocityDot = Double3.Dot(r0, v0);
        var initialChi = sqrtMu * alpha * deltaSeconds;
        if (!double.IsFinite(initialChi)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);
        if (initialChi == 0d) initialChi = sqrtMu * deltaSeconds / radius0;
        if (!double.IsFinite(initialChi) || initialChi == 0d) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, 0);

        if (!TryBracket(initialChi, alpha, radius0, radialVelocityDot, sqrtMu, deltaSeconds, out var lower, out var upper)) return Failure(TwoBodyPropagationStatus.NonConvergent, requestedTime, 0);
        var chi = initialChi;
        if (chi <= lower || chi >= upper) chi = lower + (upper - lower) * .5d;
        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            if (!TryEvaluateKepler(chi, alpha, radius0, radialVelocityDot, sqrtMu, deltaSeconds, out var residual, out var derivative, out var c, out var s)) return Failure(TwoBodyPropagationStatus.NonFiniteIntermediate, requestedTime, iteration);
            if (IsConverged(residual, chi, radius0, sqrtMu, deltaSeconds)) return TryBuildOutput(stateAtEpoch, requestedTime, mu, alpha, sqrtMu, deltaSeconds, chi, c, s, iteration);
            if (residual > 0d) upper = chi; else lower = chi;
            var midpoint = lower + (upper - lower) * .5d;
            var candidate = derivative > 0d && double.IsFinite(derivative) ? chi - residual / derivative : double.NaN;
            if (!double.IsFinite(candidate) || candidate < lower || candidate > upper) candidate = midpoint;
            if (candidate == chi && midpoint == chi) return Failure(TwoBodyPropagationStatus.NonConvergent, requestedTime, iteration);
            chi = candidate;
        }
        return Failure(TwoBodyPropagationStatus.NonConvergent, requestedTime, maximumIterations);
    }

    private static bool TryBracket(double initialChi, double alpha, double radius0, double radialVelocityDot, double sqrtMu, double deltaSeconds, out double lower, out double upper)
    {
        lower = 0d; upper = 0d;
        if (deltaSeconds > 0d)
        {
            upper = Math.Abs(initialChi);
            for (var attempt = 0; attempt < MaximumBracketExpansions; attempt++)
            {
                if (!TryEvaluateKepler(upper, alpha, radius0, radialVelocityDot, sqrtMu, deltaSeconds, out var residual, out _, out _, out _)) return false;
                if (residual >= 0d) return true;
                upper *= 2d;
                if (!double.IsFinite(upper)) return false;
            }
            return false;
        }
        lower = -Math.Abs(initialChi);
        for (var attempt = 0; attempt < MaximumBracketExpansions; attempt++)
        {
            if (!TryEvaluateKepler(lower, alpha, radius0, radialVelocityDot, sqrtMu, deltaSeconds, out var residual, out _, out _, out _)) return false;
            if (residual <= 0d) return true;
            lower *= 2d;
            if (!double.IsFinite(lower)) return false;
        }
        return false;
    }

    private static bool TryEvaluateKepler(double chi, double alpha, double radius0, double radialVelocityDot, double sqrtMu, double deltaSeconds, out double residual, out double derivative, out double c, out double s)
    {
        residual = derivative = c = s = double.NaN;
        var chiSquared = chi * chi; var z = alpha * chiSquared;
        if (!double.IsFinite(chiSquared) || !double.IsFinite(z) || !TryEvaluateStumpff(z, out c, out s)) return false;
        var chiCubed = chiSquared * chi; var radialTerm = radialVelocityDot / sqrtMu;
        residual = radialTerm * chiSquared * c + (1d - alpha * radius0) * chiCubed * s + radius0 * chi - sqrtMu * deltaSeconds;
        derivative = radialTerm * chi * (1d - z * s) + (1d - alpha * radius0) * chiSquared * c + radius0;
        return double.IsFinite(residual) && double.IsFinite(derivative);
    }

    private static TwoBodyPropagationResult TryBuildOutput(in CartesianState epochState, SimulationInstant requestedTime, double mu, double alpha, double sqrtMu, double deltaSeconds, double chi, double c, double s, int iteration)
    {
        var radius0 = Math.Sqrt(epochState.Position.LengthSquared); var chiSquared = chi * chi; var chiCubed = chiSquared * chi;
        var f = 1d - chiSquared / radius0 * c; var g = deltaSeconds - chiCubed / sqrtMu * s;
        var position = epochState.Position * f + epochState.Velocity * g;
        var radius = Math.Sqrt(position.LengthSquared);
        if (!position.IsFinite || !double.IsFinite(radius) || radius <= 0d) return Failure(TwoBodyPropagationStatus.NonFiniteOutput, requestedTime, iteration);
        var fDot = sqrtMu / (radius * radius0) * (alpha * chiCubed * s - chi);
        var gDot = 1d - chiSquared / radius * c;
        var velocity = epochState.Position * fDot + epochState.Velocity * gDot;
        var determinant = f * gDot - fDot * g;
        if (!velocity.IsFinite || !double.IsFinite(fDot) || !double.IsFinite(gDot) || !double.IsFinite(determinant) || Math.Abs(determinant - 1d) > 1e-10d) return Failure(TwoBodyPropagationStatus.NonFiniteOutput, requestedTime, iteration);
        return new(TwoBodyPropagationStatus.Success, requestedTime, new CartesianState(position, velocity), iteration);
    }

    private static bool IsConverged(double residual, double chi, double radius0, double sqrtMu, double deltaSeconds)
    {
        var scale = Math.Max(Math.Max(Math.Abs(radius0 * chi), Math.Abs(sqrtMu * deltaSeconds)), double.Epsilon);
        return Math.Abs(residual) <= RelativeResidualTolerance * scale;
    }

    private static bool TryEvaluateStumpff(double z, out double c, out double s)
    {
        c = s = double.NaN;
        if (!double.IsFinite(z) || z < 0d) return false;
        if (z <= SeriesThreshold)
        {
            var z2 = z * z; var z3 = z2 * z; var z4 = z3 * z; var z5 = z4 * z;
            c = .5d - z / 24d + z2 / 720d - z3 / 40320d + z4 / 3628800d - z5 / 479001600d;
            s = 1d / 6d - z / 120d + z2 / 5040d - z3 / 362880d + z4 / 39916800d - z5 / 6227020800d;
            return double.IsFinite(c) && double.IsFinite(s);
        }
        var root = Math.Sqrt(z); var rootCubed = root * root * root;
        if (!double.IsFinite(root) || !double.IsFinite(rootCubed) || rootCubed == 0d) return false;
        c = (1d - Math.Cos(root)) / z;
        s = (root - Math.Sin(root)) / rootCubed;
        return double.IsFinite(c) && double.IsFinite(s);
    }

    private static TwoBodyPropagationResult Failure(TwoBodyPropagationStatus status, SimulationInstant requestedTime, int iterations) => new(status, requestedTime, default, iterations);
}
