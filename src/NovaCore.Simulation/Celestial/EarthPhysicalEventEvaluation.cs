using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

internal enum EarthPhysicalEventStatus : byte
{
    Ready, UnsupportedModel, UnsupportedTimeMapping, OutsideCoverage, DurationCapacity,
    EvaluationFailed, RootMismatch, InvalidBuffers, UnsupportedOrientation,
}

/// <summary>Derived Earth-only numerical state. Exact time is not exact state or a contact certificate.</summary>
internal readonly record struct EarthPhysicalEventMotion(
    PhysicalEventEpoch Epoch, CelestialSystemId System, CelestialBodyId Body, ReferenceFrameId Root,
    FrameTransform BodyFixedToRoot, Double3 VelocityRoot, Double3 AngularVelocityRoot,
    ContactBodyMotion Canonical);

internal static class EarthPhysicalEventEvaluator
{
    internal static EarthPhysicalEventStatus TryEvaluate(CelestialSystemDefinition system, ReferenceFrameGraph graph,
        PhysicalEventEpoch epoch, Span<ReferenceFrameEvaluation> evaluations, Span<FrameTransform> roots,
        Span<ReferenceFrameEvaluation> staging, Span<FrameTransform> stagingRoots, out EarthPhysicalEventMotion result)
    {
        result = default;
        // Identity restriction intentionally excludes look-alike authoring, alternate corrections and future models.
        if (!ReferenceEquals(system, SolAnalyticalDefinition.Instance)) return EarthPhysicalEventStatus.UnsupportedModel;
        if (system.TimeMapping != CelestialSystemTimeMapping.Identity(system.TimeMapping.DomainAnchor.Domain))
            return EarthPhysicalEventStatus.UnsupportedTimeMapping;
        if (graph.RootCount != 1 || graph.GetNodeAt(0).Kind != ReferenceFrameKind.Ecl || graph.GetNodeAt(0).ParentId is not null)
            return EarthPhysicalEventStatus.RootMismatch;
        if (evaluations.Length < system.Count || roots.Length < system.Count || staging.Length < system.Count ||
            stagingRoots.Length < system.Count || evaluations.Overlaps(staging) || roots.Overlaps(stagingRoots))
            return EarthPhysicalEventStatus.InvalidBuffers;
        var coverage = system.EphemerisMetadata;
        if (epoch.CompareTo(PhysicalEventEpoch.FromCanonical(new(coverage.SupportedStartDomainTicks))) < 0 ||
            epoch.CompareTo(PhysicalEventEpoch.FromCanonical(new(coverage.SupportedEndDomainTicks))) > 0)
            return EarthPhysicalEventStatus.OutsideCoverage;
        if (epoch.TryGetCanonicalInstant(out var canonical))
        {
            var status = ContactBodyMotion.TryEvaluate(system, graph, SolarSystemBodyIds.Earth, canonical,
                evaluations, roots, staging, stagingRoots, out var body);
            if (status != ContactGenerationStatus.Ready) return EarthPhysicalEventStatus.EvaluationFailed;
            result = new(epoch, system.Id, body.Body, body.Root, body.BodyFixedToRoot,
                body.VelocityRoot, body.AngularVelocityRoot, body);
            return EarthPhysicalEventStatus.Ready;
        }
        // A whole-second anchor is exactly representable across the admitted coverage. In contrast,
        // arbitrary canonical microticks can round during the banked solver's time conversion.
        var seconds = epoch.FloorTicks / SimulationInstant.TicksPerSecond;
        if (epoch.FloorTicks % SimulationInstant.TicksPerSecond < 0) seconds--;
        var anchor = new SimulationInstant(seconds * SimulationInstant.TicksPerSecond);
        if (!PhysicalEventDuration.TryDifference(epoch, anchor, out var duration) || duration.IsNegative ||
            duration.ExceedsMagnitude(SimulationInstant.TicksPerSecond)) return EarthPhysicalEventStatus.DurationCapacity;
        if (!CelestialSystemEvaluator.TryEvaluateCurrentEarthOrbit(system, anchor, duration.Seconds, out var orbit))
            return EarthPhysicalEventStatus.EvaluationFailed;
        if (!CelestialBodyOrientationEvaluator.TryEvaluateEarthLocal(anchor, duration.Seconds, out var q, out var omega))
            return EarthPhysicalEventStatus.UnsupportedOrientation;
        result = new(epoch, system.Id, SolarSystemBodyIds.Earth, graph.GetNodeAt(0).Id,
            new(orbit.Position, q), orbit.Velocity, omega, default);
        return EarthPhysicalEventStatus.Ready;
    }
}

internal static partial class CelestialSystemEvaluator
{
    // Same authoritative trajectory and same banked anchor propagator; no Moon/all-body evaluation here.
    internal static bool TryEvaluateCurrentEarthOrbit(CelestialSystemDefinition system, SimulationInstant anchor,
        double localSeconds, out CartesianState state)
    {
        state = default;
        if (!ReferenceEquals(system, SolAnalyticalDefinition.Instance) ||
            system.RootBody != SolarSystemBodyIds.Sun ||
            !system.TryGetNode(SolarSystemBodyIds.Earth, out var earth) || earth.TrajectoryModel != CelestialTrajectoryModel.AnalyticalKepler ||
            !system.TryGetBody(SolarSystemBodyIds.Earth, out var definition) || definition.Identity.ParentBody != SolarSystemBodyIds.Sun ||
            !system.TryGetNode(SolarSystemBodyIds.Sun, out var sun) || sun.TrajectoryModel != CelestialTrajectoryModel.FixedBody ||
            !system.TryGetFixedBody(sun.Ephemeris.PayloadIndex, out var fixedSun) || fixedSun != FixedBodyEphemerisPayload.Identity ||
            !system.TryGetAnalyticalKepler(earth.Ephemeris.PayloadIndex, out var trajectory) ||
            !system.TryGetAnalyticalCorrection(earth.Ephemeris.PayloadIndex, out var correction) || !correction.IsIdentity ||
            !system.TryGetAnalyticalPeriodicCorrection(earth.Ephemeris.PayloadIndex, out var periodic) || !periodic.IsIdentity ||
            !system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun, out var properties)) return false;
        var rate = system.TimeMapping.DomainAnchor.DomainTicksPerSecond;
        if (system.TryMapTime(anchor, out var argument) != CelestialSystemTimeMappingStatus.Success ||
            !argument.TryToSimulationInstant(rate, out var solverAnchor) || solverAnchor != anchor ||
            system.TryMapTime(trajectory.Epoch, out var seedArgument) != CelestialSystemTimeMappingStatus.Success ||
            !seedArgument.TryToSimulationInstant(rate, out var seed)) return false;
        var evaluated = TryEvaluateAnalyticalKepler(trajectory.StateAtEpoch, seed, solverAnchor, properties.GravitationalParameter);
        return evaluated.Succeeded && TryAdvanceEarthLocal(evaluated.State, properties.GravitationalParameter, localSeconds, out state);
    }

    // Cubic/quadratic local continuation of the SAME central-force ODE, bounded to one second.
    // The documented snap remainder is below FP64 resolution at the admitted Earth scales.
    internal static bool TryAdvanceEarthLocal(in CartesianState anchor, double mu, double h, out CartesianState state)
    {
        state = default;
        if (!anchor.IsFinite || !double.IsFinite(mu) || mu <= 0 || !double.IsFinite(h) || h < 0 || h > 1) return false;
        var r = anchor.Position; var v = anchor.Velocity; var radius = Math.Sqrt(r.LengthSquared);
        if (!double.IsFinite(radius) || radius <= 0) return false;
        var halfRadius = radius / 2;
        var accelerationBound = mu / (halfRadius * halfRadius);
        if (Math.Sqrt(v.LengthSquared) * h + accelerationBound * h * h / 2 >= halfRadius) return false;
        var inverseCube = 1 / (radius * radius * radius);
        var a = r * (-mu * inverseCube);
        var j = (v - r * (3 * Double3.Dot(r, v) / (radius * radius))) * (-mu * inverseCube);
        var p = r + (v + (a / 2 + j * (h / 6)) * h) * h;
        var velocity = v + (a + j * (h / 2)) * h;
        if (!p.IsFinite || !velocity.IsFinite) return false;
        state = new(p, velocity);
        return true;
    }
}
