using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal enum LocalContactStatus : byte
{
    Success, InvalidConfiguration, InvalidSource, UnsupportedForceTorqueState,
    ForeignEngine, ChangedAuthority, TimelineConflict, PendingEvent, InvalidInterval,
    ConfigurationMismatch, GenerationMismatch, FrontierMismatch, WrongThread,
    Disposed, Invalidated, PrecisionEnvelopeExceeded, SolverFailure,
}

/// <summary>Immutable qualification geometry and fixed inertial transport; not gameplay collision authority.</summary>
internal sealed class LocalContactConfiguration
{
    internal long Revision { get; }
    internal ReferenceFrameId RootFrame { get; }
    internal Double3 OriginRoot { get; }
    internal Double3 OriginVelocityRoot { get; }
    internal DoubleQuaternion LocalToRoot { get; }
    internal Double3 BoxDimensions { get; }
    internal double PlaneHalfExtent { get; }
    internal double ContactTolerance { get; }
    internal double MaximumCoordinate { get; }
    internal double MaximumSpeed { get; }
    internal double MaximumAngularSpeed { get; }
    internal double Margin => 20 * ContactTolerance;
    // Speculative reach must cover the admitted surface speed over a whole exact step.
    // Margin alone is a geometric/depth cushion, not the motion horizon.
    internal double MaximumSpeculativeMargin => MaximumSpeed * (16667d / 1_000_000) + Margin;
    internal double BoundingRadius => Math.Sqrt(BoxDimensions.LengthSquared) * .5;

    private LocalContactConfiguration(long revision, ReferenceFrameId root, Double3 origin, Double3 velocity,
        DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent, double tolerance, double coordinate)
    {
        Revision = revision; RootFrame = root; OriginRoot = origin; OriginVelocityRoot = velocity;
        LocalToRoot = rotation; BoxDimensions = dimensions; PlaneHalfExtent = planeHalfExtent;
        ContactTolerance = tolerance; MaximumCoordinate = coordinate;
        // At most half the smallest extent per largest exact step, including rotation at the box radius.
        MaximumSpeed = Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z)) * .5 / (16667d / 1_000_000);
        MaximumAngularSpeed = MaximumSpeed / (Math.Sqrt(dimensions.LengthSquared) * .5);
    }

    internal static LocalContactStatus TryCreate(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent,
        out LocalContactConfiguration? configuration)
    {
        configuration = null;
        if (revision <= 0 || root.Value == 0 || !origin.IsFinite || !velocity.IsFinite || !rotation.IsFinite ||
            Math.Abs(rotation.LengthSquared - 1) > 1e-12 || !dimensions.IsFinite ||
            dimensions.X <= 0 || dimensions.Y <= 0 || dimensions.Z <= 0 || !double.IsFinite(planeHalfExtent))
            return LocalContactStatus.InvalidConfiguration;
        var minimum = Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z));
        var tolerance = minimum / 1000; // Qualification requires one-thousandth of the smallest body dimension.
        // Eight float spacings fit inside the contact tolerance. Power-of-two bound is exclusive.
        var bound = Math.Pow(2, Math.Floor(Math.Log2(tolerance / 8)) + 23);
        if (!double.IsFinite(bound) || bound <= 0 || planeHalfExtent <= Math.Sqrt(dimensions.LengthSquared) ||
            planeHalfExtent >= bound || (float)minimum <= 0 || !float.IsFinite((float)bound) ||
            !double.IsFinite(dimensions.LengthSquared) || tolerance <= 0 ||
            !double.IsFinite(minimum / (16667d / 1_000_000)) ||
            !FloatGeometryFits(dimensions, planeHalfExtent, tolerance) ||
            !RootSpacingFits(origin, tolerance))
            return LocalContactStatus.InvalidConfiguration;
        configuration = new(revision, root, origin, velocity, rotation, dimensions, planeHalfExtent, tolerance, bound);
        return LocalContactStatus.Success;
    }

    // Canonical FP64 addition itself must resolve the qualified contact tolerance.
    internal static bool RootSpacingFits(Double3 p, double tolerance) => p.IsFinite &&
        Math.Abs(Math.BitIncrement(p.X) - p.X) <= tolerance / 8 &&
        Math.Abs(Math.BitIncrement(p.Y) - p.Y) <= tolerance / 8 &&
        Math.Abs(Math.BitIncrement(p.Z) - p.Z) <= tolerance / 8;

    private static bool FloatGeometryFits(Double3 dimensions, double halfExtent, double tolerance)
    {
        var d = new System.Numerics.Vector3((float)dimensions.X, (float)dimensions.Y, (float)dimensions.Z);
        var edge = (float)(2 * halfExtent);
        var area = edge * edge;
        // BEPU broadphase/radius and triangle normal arithmetic use squared float lengths.
        // Refuse finite inputs whose derived geometry overflows or underflows before allocating a world.
        return float.IsNormal(d.LengthSquared()) && float.IsNormal(area * area) &&
            float.IsNormal((float)tolerance) && float.IsNormal((float)(20 * tolerance));
    }

    internal PrincipalMomentsOfInertia BoxInertia(double mass) => new(
        mass * (BoxDimensions.Y * BoxDimensions.Y + BoxDimensions.Z * BoxDimensions.Z) / 12,
        mass * (BoxDimensions.X * BoxDimensions.X + BoxDimensions.Z * BoxDimensions.Z) / 12,
        mass * (BoxDimensions.X * BoxDimensions.X + BoxDimensions.Y * BoxDimensions.Y) / 12);
}

/// <summary>Copied immutable source. Never retains a borrowed state view or claims solver cache serialization.</summary>
internal sealed class LocalContactSource
{
    private readonly SimulationTransactionEngine engine;
    private readonly SpacecraftTranslationState linear;
    private readonly SpacecraftRigidBodyRotationState angular;
    internal SpacecraftMotion Motion { get; }
    internal LocalContactConfiguration Configuration { get; }
    internal SimulationInstant End { get; }
    internal TimelineRevision TimelineRevision { get; }
    internal Double3 ForceRoot => linear.ConstantForceRoot;

    private LocalContactSource(SimulationTransactionEngine engine, LocalContactConfiguration configuration,
        SpacecraftTranslationState linear, SpacecraftRigidBodyRotationState angular, SpacecraftMotion motion,
        TimelineRevision timelineRevision, SimulationInstant end)
    {
        this.engine = engine; Configuration = configuration; this.linear = linear; this.angular = angular;
        Motion = motion; TimelineRevision = timelineRevision; End = end;
    }

    internal static LocalContactStatus Capture(SimulationTransactionEngine engine, SpacecraftId subject,
        LocalContactConfiguration? configuration, SimulationInstant end, out LocalContactSource? source)
    {
        source = null;
        if (!engine.IsContactProofOwnerThread) return LocalContactStatus.WrongThread;
        if (configuration is null) return LocalContactStatus.InvalidConfiguration;
        var start = engine.ContactProofCurrentTime;
        if (end <= start || (Int128)end.Ticks - start.Ticks > long.MaxValue) return LocalContactStatus.InvalidInterval;
        if (engine.HasContactProofBoundaryThrough(end)) return LocalContactStatus.PendingEvent;
        var view = engine.State;
        if (!view.Spacecraft.TryGetTranslation(subject, out var linear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(subject, out var angular) || !mass.IsValid ||
            linear.RootFrame != configuration.RootFrame || angular.PrincipalInertia != configuration.BoxInertia(mass.MassKilograms))
            return LocalContactStatus.InvalidSource;
        if (angular.ConstantBodyTorque != Double3.Zero || angular.Model != RigidBodyRotationModel.ConstantBodyTorqueV1)
            return LocalContactStatus.UnsupportedForceTorqueState;
        if (SpacecraftMotionEvaluator.TryEvaluate(view, subject, start, out var motion) != SpacecraftTranslationStatus.Success)
            return LocalContactStatus.InvalidSource;
        source = new(engine, configuration, linear, angular, motion, engine.ContactProofTimelineRevision, end);
        return LocalContactStatus.Success;
    }

    internal LocalContactStatus Validate(SimulationTransactionEngine current, LocalContactConfiguration configuration,
        SimulationInstant target)
    {
        if (!ReferenceEquals(current, engine)) return LocalContactStatus.ForeignEngine;
        if (!current.IsContactProofOwnerThread) return LocalContactStatus.WrongThread;
        if (!ReferenceEquals(configuration, Configuration)) return LocalContactStatus.ConfigurationMismatch;
        if (current.ContactProofTimelineRevision != TimelineRevision) return LocalContactStatus.TimelineConflict;
        var view = current.State;
        if (view.Revision != Motion.Revision || current.ContactProofCurrentTime != Motion.Time ||
            !view.Spacecraft.TryGetTranslation(Motion.Spacecraft, out var nowLinear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(Motion.Spacecraft, out var nowAngular) ||
            nowLinear != linear || nowAngular != angular || mass != Motion.Properties)
            return LocalContactStatus.ChangedAuthority;
        if (target < Motion.Time || target > End) return LocalContactStatus.InvalidInterval;
        return current.HasContactProofBoundaryThrough(target) ? LocalContactStatus.PendingEvent : LocalContactStatus.Success;
    }

    internal bool TryEndpoint(long step, out SimulationInstant endpoint)
    {
        endpoint = default;
        if (step < 0) return false;
        var ticks = (Int128)Motion.Time.Ticks + (Int128)step * SimulationInstant.TicksPerSecond / 60;
        if (ticks < long.MinValue || ticks > long.MaxValue || ticks > End.Ticks) return false;
        endpoint = new((long)ticks); return true;
    }
}
