using System.Numerics;
using System.Reflection;
using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using static ContactKinematicsOracle;
using R = CertifiedResponseOracle.R;
using B = CertifiedResponseOracle.B;

internal static class CertifiedPreImpactVelocityTests
{
    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException("Certified pre-impact velocity: " + name); }

    internal static void Run()
    {
        AnalyticalMotion(); MotionRefusals(); AuthorityShape(); RepresentationAdversaries();
        Console.WriteLine("CERTIFIED_PREIMPACT_ANALYTICAL full-bracket/rational/irrational/force/epoch/material-motion/authority/representation PASS");
    }

    private static void AnalyticalMotion()
    {
        var system = SolAnalyticalDefinition.Instance;
        Check(system.TryGetNode(SolarSystemBodyIds.Earth, out var earth), "Earth source");
        Check(system.TryGetAnalyticalKepler(earth.Ephemeris.PayloadIndex, out var trajectory), "Earth seed");
        Check(CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model), "orientation source");
        var feature = new Double3(1, -2, 3);
        var attitude = new DoubleQuaternion(0, 0, .6, .8);
        var angular = new SpacecraftRigidBodyRotationState(new(1), SimulationInstant.Zero, attitude,
            Double3.Zero, new(2, 3, 4), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        var up = new Double3(.3, .4, .8);
        foreach (var force in new[] { Double3.Zero, new Double3(2, -1, 3) })
        foreach (var epochTicks in new long[] { 0, -2_000_000 })
        foreach (var rational in new[] { true, false })
        {
            var linear = new SpacecraftTranslationState(new(1), new(1), new(epochTicks),
                trajectory.StateAtEpoch.Position + new Double3(1000, 2000, -3000),
                trajectory.StateAtEpoch.Velocity + new Double3(2, -3, 5), force);
            var coherentAngular = angular with { Epoch = new(epochTicks) };
            Check(FloridaContactMotion.TryCreate(system, linear, coherentAngular, new(8), feature, out var motion), "real admitted motion");
            // Analytical roots establish test arithmetic only; no fabricated provider root or witness.
            // Both dyadic endpoints belong to the seed cell. The complete bracket is checked below.
            var center = rational ? .5 : 1 / Math.Sqrt(2);
            var bracket = new FloridaBound(center - Math.ScaleB(1, -14), center + Math.ScaleB(1, -14));
            var lo = R.From(bracket.Lower); var hi = R.From(bracket.Upper);
            Check(rational ? lo < new R(1, 2) && hi > new R(1, 2) : lo > 0 && 2 * lo * lo < 1 && 2 * hi * hi > 1,
                "exact rational/irrational alpha inclusion");
            Check(motion.PreImpactVelocities(bracket, out var com, out var relative), "vector evaluation");
            var elapsed = B.From(bracket) - B.Point(new R(epochTicks, SimulationInstant.TicksPerSecond));
            var v = linear.VelocityRoot; var f = linear.ConstantForceRoot;
            B Component(double velocity, double forceComponent) => B.Point(R.From(velocity)) +
                B.Point(R.From(forceComponent) / 8) * elapsed;
            Check(CertifiedResponseOracle.Contains(com, new[] { Component(v.X, f.X), Component(v.Y, f.Y), Component(v.Z, f.Z) }),
                "complete-bracket exact-rational affine COM velocity including segment epoch");

            var (point, derivative, centerTime, halfWidth) = RelativeReference(linear, coherentAngular, 8, feature, bracket);
            // W'' = -EarthJerk - Omega'' x r - 2 Omega' x v - Omega x a.
            // Guards in RelativeReference imply |W''| < 4.1e-8 + 1e-5 + 2e-5 + .001 < .002.
            // Center value/derivative errors include the independently bounded Earth Taylor tail and
            // decimal arithmetic. This box covers every time in the bracket, not only its midpoint.
            var tail = .001m * halfWidth * halfWidth + 1e-10m * (1 + halfWidth);
            B Whole(decimal value, decimal slope)
            {
                var radius = Math.Abs(slope) * halfWidth + tail;
                return new(R.From(value - radius), R.From(value + radius));
            }
            Check(CertifiedResponseOracle.Contains(relative, new[] { Whole(point.X, derivative.X), Whole(point.Y, derivative.Y), Whole(point.Z, derivative.Z) }),
                "complete-bracket independent moving-material relative velocity");
            Check(motion.ContactKinematics(bracket, up, out var normal, out var speed, out var lever), "matching scalar fields");
            var (n, _) = Orientation(model, centerTime, up);
            Check(Contains(normal, n) && Contains(lever, Lever(attitude, feature)), "normalized orientation and nonzero lever");
            Check(Contains(speed, V.Dot(n, point), 1e-10m), "independent vector/scalar normal-velocity coherence");
            Check(relative == EvaluateAgain(motion, bracket) && com.IsFinite, "deterministic repeated vector enclosure");
        }
    }

    private static FloridaVector EvaluateAgain(in FloridaContactMotion motion, FloridaBound time)
    {
        Check(motion.PreImpactVelocities(time, out _, out var relative), "repeat evaluation");
        return relative;
    }

    private static void MotionRefusals()
    {
        var system = SolAnalyticalDefinition.Instance;
        Check(system.TryGetNode(SolarSystemBodyIds.Earth, out var earth), "refusal Earth source");
        Check(system.TryGetAnalyticalKepler(earth.Ephemeris.PayloadIndex, out var trajectory), "refusal Earth seed");
        var linear = new SpacecraftTranslationState(new(1), new(1), SimulationInstant.Zero,
            trajectory.StateAtEpoch.Position, trajectory.StateAtEpoch.Velocity, Double3.Zero);
        var angular = new SpacecraftRigidBodyRotationState(new(1), SimulationInstant.Zero, DoubleQuaternion.Identity,
            Double3.Zero, new(2, 3, 4), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        Check(FloridaContactMotion.TryCreate(system, linear, angular, new(8), Double3.Zero, out var motion), "refusal valid model");
        foreach (var time in new[] { new FloridaBound(double.NaN, 1), new(0, double.PositiveInfinity), new(2, 1), new(3601, 3602) })
            Check(!motion.PreImpactVelocities(time, out var com, out var relative) && com == default && relative == default,
                "invalid/unsupported interval returns no values");
        var overflow = linear with { ConstantForceRoot = new(double.MaxValue, double.MaxValue, double.MaxValue) };
        Check(!FloridaContactMotion.TryCreate(system, overflow, angular, new(double.Epsilon), Double3.Zero, out _),
            "unrepresentable force/mass arithmetic refused");
        Check(!FloridaContactMotion.TryCreate(system, linear, angular with { AngularVelocityBody = Double3.UnitX }, new(8), Double3.Zero, out _),
            "nonzero spin remains outside admitted specialization");
    }

    private static (V Value, V Derivative, decimal Time, decimal HalfWidth) RelativeReference(
        in SpacecraftTranslationState linear, in SpacecraftRigidBodyRotationState angular,
        double mass, Double3 feature, FloridaBound bracket)
    {
        var lower = D(bracket.Lower); var upper = D(bracket.Upper);
        Check(lower >= 0 && upper <= 1, "independent oracle seed-cell coverage");
        var t = (lower + upper) / 2; var h = (upper - lower) / 2;
        var system = SolAnalyticalDefinition.Instance;
        Check(system.TryGetNode(SolarSystemBodyIds.Earth, out var node), "oracle source node");
        Check(system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var trajectory), "oracle source seed");
        Check(system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var sun), "oracle source mu");
        Check(CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model), "oracle source orientation");
        var p0 = V.From(trajectory.StateAtEpoch.Position); var v0 = V.From(trajectory.StateAtEpoch.Velocity);
        var radius = p0.Norm; var mu = D(sun.GravitationalParameter);
        // First-exit radius bound over one second, with |a| < .02 while r >= 1e11.
        // Thus |vEarth| < 50001, snap < 2e-13, position/velocity Taylor tails < 1e-14/4e-14.
        Check(radius - 50001 > 100_000_000_000m && v0.Norm < 50_000 && mu > 0 && mu < 200_000_000_000_000_000_000m,
            "Earth independent remainder guards");
        var earthA = p0 * (-mu / radius / radius / radius);
        var earthJ = (v0 - p0 * (3 * V.Dot(p0, v0) / radius / radius)) * (-mu / radius / radius / radius);
        var a = V.From(linear.ConstantForceRoot) / D(mass);
        var epoch = (decimal)linear.Epoch.Ticks / SimulationInstant.TicksPerSecond;
        var dt = t - epoch; var lever = Lever(angular.OrientationLocalToParent, feature);
        var displacementAtZero = V.From(linear.PositionRoot) - p0 + lever - V.From(linear.VelocityRoot) * epoch + a * (epoch * epoch / 2);
        var velocityAtZero = V.From(linear.VelocityRoot) - v0 - a * epoch;
        var rates = (Math.Abs(D(model.RaT)) + Math.Abs(D(model.DecT))) / D(model.DaysPerCentury) / D(model.SecondsPerDay) +
            Math.Abs(D(model.Wd)) / D(model.SecondsPerDay);
        Check(rates * Pi / 180 < .0001m && a.Norm + .02m < 10 && velocityAtZero.Norm + 10 < 1000 &&
            displacementAtZero.Norm + 10000 < 10_000_000, "whole-cell material derivative guards");

        // Subtract astronomical seed positions before evaluating the small relative polynomial.
        var r = V.From(linear.PositionRoot) - p0 + lever + V.From(linear.VelocityRoot) * dt + a * (dt * dt / 2) -
            v0 * t - earthA * (t * t / 2) - earthJ * (t * t * t / 6);
        var v = V.From(linear.VelocityRoot) + a * dt - v0 - earthA * t - earthJ * (t * t / 2);
        var acceleration = a - earthA - earthJ * t;
        var (_, omega) = Orientation(model, t, Double3.UnitX);
        var omegaPrime = OmegaDerivative(model, t);
        return (v - V.Cross(omega, r), acceleration - V.Cross(omegaPrime, r) - V.Cross(omega, v), t, h);
    }

    private static V OmegaDerivative(in EarthContactOrientation model, decimal t)
    {
        var rad = Pi / 180; var century = D(model.SecondsPerDay) * D(model.DaysPerCentury);
        var a = (D(model.Ra0) + 90 + D(model.RaT) * t / century) * rad;
        var b = (90 - D(model.Dec0) - D(model.DecT) * t / century) * rad;
        var ar = D(model.RaT) * rad / century; var br = -D(model.DecT) * rad / century;
        var cr = D(model.Wd) * rad / D(model.SecondsPerDay);
        var (sa, ca) = Trig(a); var (sb, cb) = Trig(b);
        var z = new V(0, 0, 1); var tiltedX = new V(ca, sa, 0); var tiltedZ = new V(sa * sb, -ca * sb, cb);
        // Differentiate the independent root-axis angular-velocity sum. |Omega'| <= S^2,
        // |Omega''| <= S^3 for the three constant-rate Euler factors with sum of rates S.
        return V.Cross(z, tiltedX) * (br * ar) + V.Cross(z * ar + tiltedX * br, tiltedZ) * cr;
    }

    private static (decimal Sin, decimal Cos) Trig(decimal angle)
    {
        angle -= decimal.Round(angle / (2 * Pi)) * 2 * Pi;
        decimal sin = angle, cos = 1, s = angle, c = 1;
        for (var k = 1; k <= 32; k++)
        {
            s *= -angle * angle / (2 * k * (2 * k + 1));
            c *= -angle * angle / ((2 * k - 1) * 2 * k); sin += s; cos += c;
        }
        return (sin, cos);
    }

    private static void AuthorityShape()
    {
        var request = new CertifiedPreImpactVelocityRequest(1e-5, 1e-5);
        Check(request.IsValid && new CertifiedPreImpactVelocityRequest(double.Epsilon, double.MaxValue).IsValid, "finite positive full widths");
        foreach (var invalid in new[] { 0d, -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Check(!new CertifiedPreImpactVelocityRequest(invalid, 1).IsValid && !new CertifiedPreImpactVelocityRequest(1, invalid).IsValid, "invalid width rejected");
        var root = default(FloridaContactProvider.Proof); var witness = default(FloridaContactProvider.Proof.Kinematics);
        var result = root.QualifyPreImpactVelocity(witness, request, default);
        Check(result.Status == CertifiedPreImpactVelocityStatus.Unsupported && result.Failure == CertifiedPreImpactVelocityFailure.InvalidWitness,
            "default issuance cannot fabricate alpha");
        Check(default(FloridaContactProvider.Proof.PreImpactVelocity).Read(root, witness, request, default, out var values) ==
            CertifiedPreImpactVelocityStatus.Unsupported && values == default, "default receipt cannot provide applicable values");
        Check(values.AngularVelocityBody == Double3.Zero, "data contains only admitted zero-spin fact, not authority");
        var type = typeof(FloridaContactProvider.Proof.PreImpactVelocity);
        Check(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(c => c.IsPrivate), "private receipt construction");
        Check(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(f => f.IsInitOnly), "immutable receipt fields");
        Check(!type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(m => m.GetParameters())
            .Select(p => p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)
            .Any(t => t == typeof(FloridaContactKinematics) || t == typeof(CertifiedPreImpactVelocityValues)), "copied data cannot issue root authority");
    }

    private static void RepresentationAdversaries()
    {
        // Analytical representational counterexamples, not provider-domain admission or response selection.
        // Unit mass, lever x, normal y, Iz=1/2: k=3 and the exact inelastic scalar is 1/3.
        var j = 1d / 3; var roundedLinear = -1d + j; var roundedAngular = j / .5d;
        Check(-1 + 3 * R.From(j) == new R(-1, BigInteger.One << 54), "impulse-only 1/3 residual");
        Check(R.From(roundedLinear) + R.From(roundedAngular) == new R(-1, BigInteger.One << 53), "final state addition has distinct normal residual");
        var upward = Math.BitIncrement(j);
        Check(-1 + 3 * R.From(upward) == new R(1, BigInteger.One << 53), "neighbor does not restore exact normal zero");
        var huge = Math.ScaleB(1, 54); var hugeAfter = huge + 1d;
        Check(hugeAfter == huge && R.From(hugeAfter) - R.From(huge) != 1, "command increment lost in large final velocity");
        var large = Math.ScaleB(1, 15); var tiny = Math.ScaleB(1, -40); var after = large + tiny;
        Check(after == large && R.From(after) - R.From(large) != R.From(tiny), "small increment lost at finite orbital velocity scale");
        var minimum = double.Epsilon; var half = minimum / 2;
        Check(half == 0 && R.From(minimum) / 2 > 0, "subnormal final increment need not be representable");
        var x = R.From(1 / Math.Sqrt(3)); var y = R.From(Math.Sqrt(2) / Math.Sqrt(3));
        Check(y * y != 2 * x * x, "rounded direction cannot assert exact generic frictionless collinearity");
    }
}
