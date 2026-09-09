using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum IsolatedContactResponseStatus : byte
{
    Uninitialized, Ready, ObservationUnavailable, UnsupportedGeometry, StaleState,
    IdentityMismatch, TimeMismatch, AuthorityMismatch, InvalidNormal, InvalidPhysicalState,
    NotContacting, RecoveryRequired, Separating, Indeterminate, NonFiniteResponse,
}

internal readonly record struct IsolatedContactResponseResult(IsolatedContactResponseStatus Status,
    SpacecraftContactImpulseIntent Intent = default, double NormalSpeed = 0,
    double NormalSpeedUncertainty = 0, double EffectiveInverseMass = 0)
{
    internal bool Succeeded => Status == IsolatedContactResponseStatus.Ready;
}

/// <summary>
/// Ephemeral generator-issued receipt, not a cache or contact manifold. Its private construction binds
/// the complete single-feature observation to the immutable query, celestial system, geometry and motion.
/// Default is invalid. Acquire/evaluate/submit in one single-writer phase with independently current inputs.
/// </summary>
internal readonly struct IsolatedContactObservation
{
    private readonly IPhysicalSurfacePointQuery? _query;
    private readonly SpacecraftContactGeometry? _geometry;
    private readonly ContactBodyMotion _body;
    private readonly SpacecraftMotion _motion;
    private readonly SpacecraftTranslationState _translation;
    private readonly SpacecraftRigidBodyRotationState _rotation;
    internal SpacecraftContactObservation Observation { get; }

    private IsolatedContactObservation(IPhysicalSurfacePointQuery query, SpacecraftContactGeometry geometry,
        ContactBodyMotion body, SpacecraftMotion motion, SpacecraftTranslationState translation,
        SpacecraftRigidBodyRotationState rotation, SpacecraftContactObservation observation)
    {
        _query = query; _geometry = geometry; _body = body; _motion = motion;
        _translation = translation; _rotation = rotation; Observation = observation;
    }

    /// <summary>
    /// Exactly one feature in the COMPLETE authored definition, not a selected subset. This natural-terrain-only
    /// policy cannot establish isolation from other collision systems. Their composition must not invoke it.
    /// Caller supplies one scratch element; no receipt is issued if any generation/qualification check fails.
    /// </summary>
    internal static ContactGenerationResult TryObserve(in SimulationStateView state, StateRevision currentRevision,
        SimulationInstant time, SpacecraftContactGeometry? geometry, in ContactBodyMotion body,
        CelestialSystemDefinition currentSystem, IPhysicalSurfacePointQuery? currentQuery,
        in PhysicalSurfaceAuthorityIdentity currentAuthority, Span<SpacecraftContactObservation> scratch,
        out IsolatedContactObservation receipt)
    {
        receipt = default;
        if (geometry is null || geometry.Count != 1)
        { scratch.Clear(); return new(ContactGenerationStatus.InvalidGeometry, 0); }
        var generated = SpacecraftContactGenerator.GenerateWithMotion(state, currentRevision, time, geometry, body,
            currentSystem, currentQuery, currentAuthority, scratch, out var motion);
        if (!generated.Succeeded) return generated;
        if (!state.Spacecraft.TryGetTranslation(geometry.Spacecraft, out var translation, out _) ||
            !state.Spacecraft.TryGetRigidBody(geometry.Spacecraft, out var rotation))
        { scratch.Clear(); return new(ContactGenerationStatus.MotionUnavailable, 0); }
        receipt = new(currentQuery!, geometry, body, motion, translation, rotation, scratch[0]);
        return generated;
    }

    /// <summary>
    /// Pure producer. No queries, scheduling or mutation. Current query/system/geometry are supplied by
    /// composition; old inputs must not be relabeled current. M14.4 still revalidates the resulting transaction.
    /// Observation ID is the producer's nonzero deterministic provenance identity, not a timeline event ID.
    /// </summary>
    internal IsolatedContactResponseResult Evaluate(in SimulationStateView state, StateRevision currentRevision,
        SimulationInstant time, SpacecraftContactGeometry? currentGeometry, CelestialSystemDefinition currentSystem,
        IPhysicalSurfacePointQuery? currentQuery, in PhysicalSurfaceAuthorityIdentity currentAuthority, ulong observationId)
    {
        if (_query is null || !Observation.IsReady) return new(IsolatedContactResponseStatus.ObservationUnavailable);
        if (currentGeometry is null || currentGeometry.Count != 1) return new(IsolatedContactResponseStatus.UnsupportedGeometry);
        if (state.Revision != currentRevision || Observation.SpacecraftRevision != currentRevision)
            return new(IsolatedContactResponseStatus.StaleState);
        if (time != Observation.Time || time != _body.Time) return new(IsolatedContactResponseStatus.TimeMismatch);
        if (!ReferenceEquals(_geometry, currentGeometry) || !ReferenceEquals(_body.System, currentSystem) ||
            Observation.Feature != new ContactFeatureIdentity(currentGeometry.Spacecraft, currentGeometry.Identity, currentGeometry.GetFeature(0).Id) ||
            Observation.Root != _body.Root || Observation.Root != _motion.RootFrame)
            return new(IsolatedContactResponseStatus.IdentityMismatch);
        if (!ReferenceEquals(_query, currentQuery) || currentQuery.Authority != currentAuthority ||
            Observation.TerrainAuthority != currentAuthority || currentAuthority.BodyId != _body.Body.Value)
            return new(IsolatedContactResponseStatus.AuthorityMismatch);
        if (!state.Spacecraft.TryGetTranslation(currentGeometry.Spacecraft, out var translation, out var mass) ||
            !state.Spacecraft.TryGetRigidBody(currentGeometry.Spacecraft, out var rotation) ||
            translation != _translation || rotation != _rotation || mass != _motion.Properties)
            return new(IsolatedContactResponseStatus.StaleState);
        if (!mass.IsValid || !_motion.Inertia.IsFinite || !_motion.Inertia.IsStrictlyPositive)
            return new(IsolatedContactResponseStatus.InvalidPhysicalState);
        var n = Observation.PhysicalNormalRoot;
        if (!n.IsFinite || Math.Abs(n.LengthSquared - 1) > SurfaceAnchor.DirectionUnitLengthSquaredTolerance ||
            Double3.Dot(n, _body.BodyFixedToRoot.LocalDirectionToParent(Observation.TerrainDirectionBodyFixed)) <= 0)
            return new(IsolatedContactResponseStatus.InvalidNormal);
        var provenance = new ContactResponseProvenance(Observation.Feature, currentAuthority, observationId);
        if (!provenance.IsValid) return new(IsolatedContactResponseStatus.IdentityMismatch);

        // Deliberately conservative: only represented canonical zero radial gap is eligible. No generic
        // terrain-slope bound exists in M14.3; fixture error bars cannot authorize a production contact skin.
        var gap = Observation.RadialSignedGapMetres;
        if (!double.IsFinite(gap)) return new(IsolatedContactResponseStatus.NonFiniteResponse);
        if (gap > 0) return new(IsolatedContactResponseStatus.NotContacting);
        if (gap < 0) return new(IsolatedContactResponseStatus.RecoveryRequired);
        var relative = Observation.RelativeVelocityRoot;
        var u = Double3.Dot(n, relative);
        var uncertainty = VelocityUncertainty(n, Observation.FeatureVelocityRoot, Observation.TerrainVelocityRoot, relative);
        if (!double.IsFinite(u) || !double.IsFinite(uncertainty)) return new(IsolatedContactResponseStatus.NonFiniteResponse);
        if (Math.Abs(u) <= uncertainty) return new(IsolatedContactResponseStatus.Indeterminate, NormalSpeed: u, NormalSpeedUncertainty: uncertainty);
        if (u > uncertainty) return new(IsolatedContactResponseStatus.Separating, NormalSpeed: u, NormalSpeedUncertainty: uncertainty);

        // Use the authored lever arm directly, never subtract two large inertial positions.
        var r = _motion.BodyToRoot.Rotate(currentGeometry.GetFeature(0).OffsetFromComMetres);
        var a = _motion.BodyToRoot.Conjugate().Rotate(Double3.Cross(r, n));
        var inertia = _motion.Inertia;
        var k = 1 / mass.MassKilograms + a.X * a.X / inertia.X + a.Y * a.Y / inertia.Y + a.Z * a.Z / inertia.Z;
        var j = -u / k;
        var impulse = n * j;
        if (!r.IsFinite || !a.IsFinite || !double.IsFinite(k) || k <= 0 || !double.IsFinite(j) || j <= 0 ||
            !impulse.IsFinite || impulse == Double3.Zero)
            return new(IsolatedContactResponseStatus.NonFiniteResponse);
        var intent = new SpacecraftContactImpulseIntent(currentGeometry.Spacecraft, currentRevision, time,
            Observation.Root, impulse, r, provenance);
        return new(IsolatedContactResponseStatus.Ready, intent, u, uncertainty, k);
    }

    // Bound only arithmetic versus the CANONICAL observation vectors, not celestial model or true-gradient error.
    // 3 subtraction errors: u/(1-u)*(|vf|+|vs|); dot: gamma5*sum|n*relative|.
    // Each positive bound operation rounds upward; epsilon terms cover gradual underflow (including products).
    private static double VelocityUncertainty(Double3 n, Double3 feature, Double3 surface, Double3 relative)
    {
        const double unitRoundoff = 1.1102230246251565e-16;
        var subtractionFactor = Math.BitIncrement(unitRoundoff / (1 - unitRoundoff));
        var dotFactor = Math.BitIncrement(5 * unitRoundoff / (1 - 5 * unitRoundoff));
        static double Add(double a, double b) => Math.BitIncrement(a + b);
        static double Mul(double a, double b) => Math.BitIncrement(a * b);
        double Component(double normal, double vf, double vs) => Mul(Math.Abs(normal),
            Add(Mul(subtractionFactor, Add(Math.Abs(vf), Math.Abs(vs))), double.Epsilon));
        var subtraction = Add(Add(Component(n.X, feature.X, surface.X), Component(n.Y, feature.Y, surface.Y)), Component(n.Z, feature.Z, surface.Z));
        var products = Add(Add(Mul(Math.Abs(n.X), Math.Abs(relative.X)), Mul(Math.Abs(n.Y), Math.Abs(relative.Y))), Mul(Math.Abs(n.Z), Math.Abs(relative.Z)));
        return Add(Add(subtraction, Mul(dotFactor, products)), 8 * double.Epsilon);
    }
}
