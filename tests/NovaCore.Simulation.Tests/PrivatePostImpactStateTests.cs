using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using R = CertifiedResponseOracle.R;

internal static class PrivatePostImpactStateTests
{
    private static void Check(bool condition, string contract)
    { if (!condition) throw new InvalidOperationException("Private post-impact pose: " + contract); }

    internal static void Run()
    {
        AnalyticalRanges(); NumericalLimits(); Refusals();
        Console.WriteLine("PRIVATE_POSTIMPACT_POSE_ANALYTICAL exact-extrema/rational/irrational/force/epoch/cancellation/subnormal/refusal PASS");
    }

    private static SpacecraftTranslationState Source(Double3 position, Double3 velocity, Double3 force, long epochTicks = 0) =>
        new(new(1), new(1), new(epochTicks), position, velocity, force);

    private static FloridaVector Qualified(in SpacecraftTranslationState source, in SpacecraftPhysicalProperties properties,
        FloridaBound root, double width, string contract)
    {
        var original = source; var originalProperties = properties;
        var request = new PrivatePostImpactPoseRequest(width);
        var status = PrivatePostImpactPose.Evaluate(source, properties, root, request, out var pose);
        Check(status == PrivatePostImpactStateFailure.None, contract + " qualification: " + status);
        Check(pose.IsFinite, contract + " finite pose");
        AssertContains(source, properties, root, pose);
        Check(WidthFits(pose.X, width) && WidthFits(pose.Y, width) && WidthFits(pose.Z, width), contract + " declared full widths");
        Check(PrivatePostImpactPose.Evaluate(source, properties, root, request, out var repeated) == PrivatePostImpactStateFailure.None &&
            repeated == pose, contract + " deterministic repeated enclosure");
        Check(source == original && properties == originalProperties, contract + " immutable inputs");
        return pose;
    }

    private static bool WidthFits(FloridaBound interval, double requested) =>
        R.From(interval.Upper) - R.From(interval.Lower) <= R.From(requested);

    private static void AnalyticalRanges()
    {
        var linear = Source(new(2, -3, 5), new(4, -6, 8), Double3.Zero);
        var rational = Qualified(linear, new(8), new(.5, .5), 1, "rational constant velocity");
        Check(CertifiedResponseOracle.Contains(rational, new CertifiedResponseOracle.V(4, -6, 9)), "exact rational pose");

        // This is a mathematical kernel control, not a fabricated provider proof. Exact endpoint
        // inequalities establish inclusion of the genuinely irrational alpha = 1/sqrt(2).
        var irrational = new FloridaBound(.7071067811865475, .7071067811865476);
        var lo = R.From(irrational.Lower); var hi = R.From(irrational.Upper);
        Check(lo > 0 && 2 * lo * lo < 1 && 2 * hi * hi > 1, "irrational alpha lies strictly inside bracket");
        var forced = Source(new(7, -11, .25), new(.5, -2, 3), new(1, -2, 5));
        Qualified(forced, new(3), irrational, 1, "irrational constant force with exact F/m");

        // The epoch is not an integral second. The independent oracle never converts these ticks
        // to binary64 seconds; all components retain the same exact elapsed-time variable.
        var shifted = forced with { Epoch = new(-2_000_001) };
        Qualified(shifted, new(3), new(.5, .75), 16, "nonzero fractional-second source epoch");

        var stationary = Source(new(7, -7, 0), new(-2, 2, 0), new(2, -2, 0));
        var interval = new FloridaBound(.5, 1.5);
        var extremum = Qualified(stationary, new(1), interval, 16, "interior minimum and maximum");
        var xRange = ExactRange(stationary.PositionRoot.X, stationary.VelocityRoot.X, stationary.ConstantForceRoot.X,
            1, 0, interval);
        var yRange = ExactRange(stationary.PositionRoot.Y, stationary.VelocityRoot.Y, stationary.ConstantForceRoot.Y,
            1, 0, interval);
        Check(xRange.Lower == (R)6 && xRange.Upper == new R(25, 4) &&
            yRange.Lower == new R(-25, 4) && yRange.Upper == (R)(-6), "oracle includes interior stationary points");
        Check(CertifiedResponseOracle.Contains(extremum.X, (R)6) && CertifiedResponseOracle.Contains(extremum.Y, (R)(-6)),
            "production contains extrema missed by endpoint-only sampling");
    }

    private static void NumericalLimits()
    {
        var huge = Math.ScaleB(1, 54);
        var cancellation = Source(new(huge, -huge, 0), new(-huge, huge, 0), new(6, -6, 0));
        var atOne = new FloridaBound(1, 1);
        // A coarse request of 64 source-coordinate ULPs is declared in advance; the deliberately
        // tighter request is a separate refusal control, not a fallback or a widened retry.
        var wide = Qualified(cancellation, new(3), atOne, 256, "large-coordinate cancellation");
        Check(CertifiedResponseOracle.Contains(wide.X, (R)1) && CertifiedResponseOracle.Contains(wide.Y, (R)(-1)),
            "large cancellation retains exact small physical result");
        Refused(cancellation, new(3), atOne, new(.25), PrivatePostImpactStateFailure.PoseResolution,
            "unmet cancellation width");

        var tiny = Source(Double3.Zero, Double3.Zero, new(double.Epsilon, 0, 0));
        var subnormal = Qualified(tiny, new(2), atOne, Math.ScaleB(1, -1000), "subnormal displacement enclosure");
        var displacement = R.From(double.Epsilon) / 4;
        Check(displacement > 0 && CertifiedResponseOracle.Contains(subnormal.X, displacement),
            "real epsilon/4 displacement is not silently treated as zero");

        var overflow = Source(new(double.MaxValue, 0, 0), new(double.MaxValue, 0, 0), Double3.Zero);
        Refused(overflow, new(1), atOne, new(double.MaxValue), PrivatePostImpactStateFailure.NonFinitePose,
            "finite inputs whose pose overflows");
    }

    private static void Refusals()
    {
        var source = Source(new(1, 2, 3), new(4, 5, 6), new(1, 2, 3));
        var root = new FloridaBound(.5, .75);
        foreach (var width in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            Refused(source, new(3), root, new(width), PrivatePostImpactStateFailure.InvalidRequest, "invalid width");
        foreach (var mass in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            Refused(source, new(mass), root, new(16), PrivatePostImpactStateFailure.InvalidSource, "invalid physical mass");
        Refused(default, new(3), root, new(16), PrivatePostImpactStateFailure.InvalidSource, "default source");
        foreach (var invalid in new[]
        {
            source with { PositionRoot = new(double.NaN, 2, 3) },
            source with { VelocityRoot = new(4, double.PositiveInfinity, 6) },
            source with { ConstantForceRoot = new(1, 2, double.NegativeInfinity) },
        }) Refused(invalid, new(3), root, new(16), PrivatePostImpactStateFailure.InvalidSource, "nonfinite source component");
        foreach (var invalidRoot in new[] { new FloridaBound(1, .5), new FloridaBound(double.NaN, 1), new FloridaBound(0, double.PositiveInfinity) })
            Refused(source, new(3), invalidRoot, new(16), PrivatePostImpactStateFailure.InvalidSource, "invalid enclosure");
    }

    private static void Refused(in SpacecraftTranslationState source, in SpacecraftPhysicalProperties properties,
        FloridaBound root, in PrivatePostImpactPoseRequest request, PrivatePostImpactStateFailure expected, string contract)
    {
        var status = PrivatePostImpactPose.Evaluate(source, properties, root, request, out var pose);
        Check(status == expected, contract + " status: " + status);
        Check(pose == default, contract + " has no partially qualified pose");
    }

    /// <summary>
    /// Independent exact-rational TRUE range of the source quadratic over the WHOLE supplied root
    /// interval. Endpoint and stationary-point extrema avoid reproducing production interval arithmetic.
    /// This proves enclosure, not selection of any coordinate or creation of a provider root identity.
    /// </summary>
    internal static void AssertContains(in SpacecraftTranslationState source, in SpacecraftPhysicalProperties properties,
        FloridaBound root, in FloridaVector pose)
    {
        Check(root.IsFinite && pose.IsFinite && properties.IsValid, "oracle finite qualified inputs");
        var mass = properties.MassKilograms; var epochTicks = source.Epoch.Ticks;
        Component(source.PositionRoot.X, source.VelocityRoot.X, source.ConstantForceRoot.X, pose.X, "X");
        Component(source.PositionRoot.Y, source.VelocityRoot.Y, source.ConstantForceRoot.Y, pose.Y, "Y");
        Component(source.PositionRoot.Z, source.VelocityRoot.Z, source.ConstantForceRoot.Z, pose.Z, "Z");
        void Component(double p, double v, double f, FloridaBound actual, string axis)
        {
            var range = ExactRange(p, v, f, mass, epochTicks, root);
            Check(CertifiedResponseOracle.Contains(actual, range.Lower) && CertifiedResponseOracle.Contains(actual, range.Upper),
                "whole-root exact quadratic " + axis + " containment");
        }
    }

    private static (R Lower, R Upper) ExactRange(double position, double velocity, double force,
        double mass, long epochTicks, FloridaBound root)
    {
        var epoch = new R(new BigInteger(epochTicks), new BigInteger(SimulationInstant.TicksPerSecond));
        var lo = R.From(root.Lower) - epoch; var hi = R.From(root.Upper) - epoch;
        var p = R.From(position); var v = R.From(velocity); var c = R.From(force) / (2 * R.From(mass));
        R Value(R d) => p + v * d + c * d * d;
        var first = Value(lo); var last = Value(hi);
        var lower = first < last ? first : last; var upper = first > last ? first : last;
        if (c != (R)0)
        {
            var vertex = -v / (2 * c);
            if (vertex >= lo && vertex <= hi)
            {
                var value = Value(vertex);
                if (value < lower) lower = value;
                if (value > upper) upper = value;
            }
        }
        return (lower, upper);
    }
}
