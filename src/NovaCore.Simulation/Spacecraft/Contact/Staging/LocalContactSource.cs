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
    Disposed, Invalidated, PrecisionEnvelopeExceeded, SolverFailure, PublicationPending,
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
    internal EngineeringContactArticle? Article { get; }
    internal bool ResourceAwareNumericalFixture { get; private init; }
    // The new article uses the original 2 m thick slab. Legacy box fixtures retain their exact geometry.
    internal double SlabHalfThickness => Article is null && !ResourceAwareNumericalFixture ? BoxDimensions.Y : 1;
    internal double PlaneHalfExtent { get; }
    internal double ContactTolerance { get; }
    internal double MaximumCoordinate { get; }
    internal double MaximumSpeed { get; }
    internal double MaximumAngularSpeed { get; }
    internal double Margin => 20 * ContactTolerance;
    // Speculative reach must cover the admitted surface speed over a whole exact step.
    // Margin alone is a geometric/depth cushion, not the motion horizon.
    internal double MaximumSpeculativeMargin => ResourceAwareNumericalFixture ? .01 : MaximumSpeed * (16667d / 1_000_000) + Margin;
    internal double BoundingRadius => Article?.BoundingRadius ?? Math.Sqrt(BoxDimensions.LengthSquared) * .5;

    private LocalContactConfiguration(long revision, ReferenceFrameId root, Double3 origin, Double3 velocity,
        DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent, double tolerance, double coordinate,
        EngineeringContactArticle? article)
    {
        Revision = revision; RootFrame = root; OriginRoot = origin; OriginVelocityRoot = velocity;
        LocalToRoot = rotation; BoxDimensions = dimensions; PlaneHalfExtent = planeHalfExtent;
        Article = article;
        ContactTolerance = tolerance; MaximumCoordinate = coordinate;
        // At most half the smallest extent per largest exact step, including rotation at the box radius.
        MaximumSpeed = (article?.SmallestFeature ?? Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z))) * .5 / (16667d / 1_000_000);
        MaximumAngularSpeed = MaximumSpeed / BoundingRadius;
    }

    internal static LocalContactStatus TryCreate(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent,
        out LocalContactConfiguration? configuration)
        => TryCreateCore(revision, root, origin, velocity, rotation, dimensions, planeHalfExtent, null, out configuration);

    internal static LocalContactStatus TryCreateArticle(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, EngineeringContactArticle? article, double planeHalfExtent,
        out LocalContactConfiguration? configuration)
    {
        configuration = null;
        return article is null ? LocalContactStatus.InvalidConfiguration :
            TryCreateCore(revision, root, origin, velocity, rotation, article.Dimensions, planeHalfExtent, article, out configuration);
    }

    // Separate named numerical fixture, not a new interpretation of homogeneous-box/article inertia.
    internal static LocalContactStatus TryCreatePoweredFixture(ReferenceFrameId root, Double3 origin,
        Double3 velocity, out LocalContactConfiguration? configuration)
    {
        var status = TryCreateCore(1, root, origin, velocity, DoubleQuaternion.Identity,
            new(2, 1, 1), 8, null, out configuration);
        if (status == LocalContactStatus.Success)
            configuration = new(1, root, origin, velocity, DoubleQuaternion.Identity, new(2, 1, 1), 8,
                configuration!.ContactTolerance, configuration.MaximumCoordinate, null) { ResourceAwareNumericalFixture = true };
        return status;
    }

    private static LocalContactStatus TryCreateCore(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent,
        EngineeringContactArticle? article, out LocalContactConfiguration? configuration)
    {
        configuration = null;
        if (revision <= 0 || root.Value == 0 || !origin.IsFinite || !velocity.IsFinite || !rotation.IsFinite ||
            Math.Abs(rotation.LengthSquared - 1) > 1e-12 || !dimensions.IsFinite ||
            dimensions.X <= 0 || dimensions.Y <= 0 || dimensions.Z <= 0 || !double.IsFinite(planeHalfExtent))
            return LocalContactStatus.InvalidConfiguration;
        var minimum = article?.SmallestFeature ?? Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z));
        var tolerance = minimum / 1000; // Qualification requires one-thousandth of the smallest body dimension.
        // Eight float spacings fit inside the contact tolerance. Power-of-two bound is exclusive.
        var bound = Math.Pow(2, Math.Floor(Math.Log2(tolerance / 8)) + 23);
        if (!double.IsFinite(bound) || bound <= 0 || planeHalfExtent <= (article is null ? Math.Sqrt(dimensions.LengthSquared) : 2 * article.BoundingRadius) ||
            planeHalfExtent >= bound || (float)minimum <= 0 || !float.IsFinite((float)bound) ||
            !double.IsFinite(dimensions.LengthSquared) || tolerance <= 0 ||
            !double.IsFinite(minimum / (16667d / 1_000_000)) ||
            !FloatGeometryFits(dimensions, planeHalfExtent, tolerance) ||
            !RootSpacingFits(origin, tolerance))
            return LocalContactStatus.InvalidConfiguration;
        if (article is not null)
            for (var i = 0; i < EngineeringContactArticle.ChildCount; i++)
                if (!FloatGeometryFits(article.Child(i).Dimensions, planeHalfExtent, tolerance))
                    return LocalContactStatus.InvalidConfiguration;
        configuration = new(revision, root, origin, velocity, rotation, dimensions, planeHalfExtent, tolerance, bound, article);
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

    internal bool AdmitsMassInertia(double mass, PrincipalMomentsOfInertia inertia) => Article is { } article
        ? mass == article.MassKilograms && inertia == article.PrincipalInertia
        : !ResourceAwareNumericalFixture && inertia == BoxInertia(mass);
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
            linear.RootFrame != configuration.RootFrame || !configuration.AdmitsMassInertia(mass.MassKilograms, angular.PrincipalInertia))
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

    internal static LocalContactStatus CapturePoweredPreparation(SimulationTransactionEngine engine, SpacecraftId subject,
        PoweredContactPreparation prepared, SimulationInstant end, out LocalContactSource? source)
    {
        source = null;
        if (!engine.OwnsPersistentPublicationPhase) return LocalContactStatus.WrongThread;
        if (!prepared.Available || !prepared.Configuration.ResourceAwareNumericalFixture) return LocalContactStatus.InvalidSource;
        var view = engine.State;
        var start = engine.ContactProofCurrentTime;
        if (end <= start || (Int128)end.Ticks - start.Ticks > long.MaxValue) return LocalContactStatus.InvalidInterval;
        if (!view.Spacecraft.TryGetTranslation(subject, out var linear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(subject, out var angular) || linear.RootFrame != prepared.Configuration.RootFrame ||
            linear.Epoch != start || angular.Epoch != start || linear.ConstantForceRoot != Double3.Zero ||
            angular.ConstantBodyTorque != Double3.Zero || angular.Model != RigidBodyRotationModel.ConstantBodyTorqueV1 ||
            mass.MassKilograms != prepared.ResourceDefinition.InitialTotalMassKilograms || angular.PrincipalInertia != new PrincipalMomentsOfInertia(2, 2, 2))
            return LocalContactStatus.InvalidSource;
        var actual = new SpacecraftAppliedEndpoint(subject, linear.RootFrame, start, linear.PositionRoot, linear.VelocityRoot,
            angular.OrientationLocalToParent, angular.AngularVelocityBody, mass, angular.PrincipalInertia, AppliedEndpointValidity.EndpointOnly);
        var wanted = actual with { PositionRoot = prepared.Initial.Position, VelocityRoot = prepared.Initial.Velocity,
            BodyToRoot = prepared.Initial.Orientation, AngularVelocityBody = prepared.Initial.AngularVelocity };
        if (!actual.SameBits(wanted)) return LocalContactStatus.ChangedAuthority;
        var motion = new SpacecraftMotion(subject, start, view.Revision, linear.RootFrame, actual.PositionRoot,
            actual.VelocityRoot, actual.BodyToRoot, actual.AngularVelocityBody, mass, angular.PrincipalInertia);
        source = new(engine, prepared.Configuration, linear, angular, motion, engine.ContactProofTimelineRevision, end);
        return LocalContactStatus.Success;
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
