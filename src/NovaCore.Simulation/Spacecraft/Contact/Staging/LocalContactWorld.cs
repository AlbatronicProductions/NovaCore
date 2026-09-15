using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Persistent, thread-owned solver continuation. Canonical publication belongs exclusively to the engine.</summary>
internal sealed partial class LocalContactWorld : IDisposable
{
    internal readonly struct Receipt
    {
        internal readonly LocalContactWorld? Owner;
        internal readonly long Step;
        private readonly object? seal;
        internal long Generation { get; }
        internal Receipt(LocalContactWorld owner, long step, object? seal = null)
        { Owner = owner; Step = step; this.seal = seal; Generation = owner.Generation; }
        internal bool IsIssuedBy(object identity) => ReferenceEquals(seal, identity);
    }

    internal readonly record struct Export(SpacecraftMotion Motion, SimulationInstant Source,
        TimelineRevision TimelineRevision, long Generation, long Frontier, int ContactPoints, float MaximumDepth,
        double LocalPositionMagnitude, double LocalSpeed, double ImportPositionError, double ImportVelocityError,
        int ConstraintCount = 0, int ArticleContactChildMask = 0);

    private static long nextGeneration;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly LocalContactSource source;
    private readonly LocalContactConfiguration configuration;
    private readonly BufferPool pool;
    private readonly BepuPhysics.Simulation simulation;
    private readonly BodyHandle body;
    private readonly TypedIndex surfaceShape;
    private readonly TypedIndex bodyShape;
    private readonly StaticHandle plane;
    private readonly LocalContactMetrics metrics;
    private long frontier;
    private Export export;
    private bool disposed, invalidated;
    private readonly object receiptIdentity = new();
    internal long Generation { get; } = Interlocked.Increment(ref nextGeneration);
    internal ulong PoolBytes => pool.GetTotalAllocatedByteCount();
    internal double ImportPositionError { get; }
    internal double ImportVelocityError { get; }

    private LocalContactWorld(LocalContactSource source, Vector3 position, Vector3 velocity, Quaternion orientation,
        Vector3 angularVelocity, Vector3 acceleration, double positionError, double velocityError)
    {
        this.source = source; configuration = source.Configuration;
        ImportPositionError = positionError; ImportVelocityError = velocityError;
        pool = new BufferPool(16384); metrics = new();
        simulation = BepuPhysics.Simulation.Create(pool, new LocalContactCallbacks(metrics), new LocalContactIntegrator(acceleration),
            new SolveDescription(8, 1), initialAllocationSizes: new SimulationAllocationSizes(128, 32, 16, 128, 2048, 128, 8));
        try
        {
            var d = configuration.BoxDimensions;
            bodyShape = configuration.Article is { } article ? CreateArticleShape(simulation.Shapes, pool, article) :
                simulation.Shapes.Add(new Box((float)d.X, (float)d.Y, (float)d.Z));
            var inertia = source.Motion.Inertia;
            var bodyInertia = new BodyInertia { InverseMass = (float)(1 / source.Motion.Properties.MassKilograms) };
            bodyInertia.InverseInertiaTensor.XX = (float)(1 / inertia.X);
            bodyInertia.InverseInertiaTensor.YY = (float)(1 / inertia.Y);
            bodyInertia.InverseInertiaTensor.ZZ = (float)(1 / inertia.Z);
            body = simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(position, orientation),
                new BodyVelocity(velocity, angularVelocity), bodyInertia,
                new CollidableDescription(bodyShape, (float)configuration.MaximumSpeculativeMargin), new BodyActivityDescription(-1)));
            // Qualification-only planar surface: a finite slab with its top at local y=0.
            // The two-triangle version dropped the advancing corner manifold at its internal seam.
            // A single convex face preserves that planar coverage without changing solver quality settings.
            // Legacy box thickness is retained; engineering-article thickness is explicitly 2 m.
            var h = (float)configuration.PlaneHalfExtent;
            var halfThickness = (float)configuration.SlabHalfThickness;
            surfaceShape = simulation.Shapes.Add(new Box(2 * h, 2 * halfThickness, 2 * h));
            plane = simulation.Statics.Add(new StaticDescription(new Vector3(0, -halfThickness, 0), surfaceShape));
            if (bodyShape.Type == Compound.Id)
                metrics.Coverage = new(simulation, body, plane, bodyShape, acceleration, (float)configuration.ContactTolerance);
            // Initial receipt retains the original FP64 source, without a needless float round trip.
            export = new(source.Motion, source.Motion.Time, source.TimelineRevision, Generation, 0, 0, 0,
                position.Length(), velocity.Length(), positionError, velocityError);
        }
        catch
        {
            try
            {
                if (configuration.Article is not null && bodyShape.Exists)
                    simulation.Shapes.RecursivelyRemoveAndDispose(bodyShape, pool);
                simulation.Dispose();
            }
            finally { pool.Clear(); }
            throw;
        }
    }

    internal static LocalContactStatus TryCreate(SimulationTransactionEngine engine, LocalContactSource source,
        LocalContactConfiguration configuration, out LocalContactWorld? world, out Receipt initial)
    {
        world = null; initial = default;
        var status = source.Validate(engine, configuration, source.End);
        if (status != LocalContactStatus.Success) return status;
        var motion = source.Motion;
        var rootToLocal = configuration.LocalToRoot.Conjugate();
        var p = rootToLocal.Rotate(motion.PositionRoot - configuration.OriginRoot);
        var v = rootToLocal.Rotate(motion.VelocityRoot - configuration.OriginVelocityRoot);
        var w = rootToLocal.Rotate(motion.BodyToRoot.Rotate(motion.AngularVelocityBody));
        var a = rootToLocal.Rotate(source.ForceRoot / motion.Properties.MassKilograms);
        var q = rootToLocal * motion.BodyToRoot;
        if (!Within(configuration, p, v, w) || !a.IsFinite || !Finite(ToFloat(a)) ||
            !PositiveFloat(1 / motion.Properties.MassKilograms) ||
            !PositiveFloat(1 / motion.Inertia.X) || !PositiveFloat(1 / motion.Inertia.Y) || !PositiveFloat(1 / motion.Inertia.Z))
            return LocalContactStatus.PrecisionEnvelopeExceeded;
        var fp = ToFloat(p); var fv = ToFloat(v);
        world = new(source, fp, fv, Quaternion.Normalize(new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W)),
            ToFloat(w), ToFloat(a), Math.Sqrt((FromFloat(fp) - p).LengthSquared), Math.Sqrt((FromFloat(fv) - v).LengthSquared));
        initial = new(world, 0, world.receiptIdentity); return LocalContactStatus.Success;
    }

    internal LocalContactStatus Step(SimulationTransactionEngine engine, LocalContactConfiguration configuration,
        Receipt previous, SimulationInstant target, out Receipt next)
    {
        next = default;
        var status = Validate(engine, configuration, previous, target);
        if (status != LocalContactStatus.Success) return status;
        if ((publication is not null && publication.Pending) || powered?.Pending == true) return LocalContactStatus.PublicationPending;
        if (powered is { } activePower && (!engine.OwnsPersistentPublicationPhase || activePower.PreparedFrontier != frontier + 1))
            return LocalContactStatus.InvalidSource;
        if (frontier == long.MaxValue || !source.TryEndpoint(frontier + 1, out var expected) || target != expected ||
            !source.TryEndpoint(frontier, out var current) || target <= current) return LocalContactStatus.InvalidInterval;
        var dt = (float)((target.Ticks - current.Ticks) / (double)SimulationInstant.TicksPerSecond);
        metrics.Contacts = 0; metrics.MaximumDepth = 0; metrics.ArticleChildMask = 0;
        metrics.Coverage?.Begin(dt);
        // All admissions precede mutation. Once advanced, any failed export destroys continuation permission.
        invalidated = true;
        if (powered is not null) powered.PreparedFrontier = 0;
        try { simulation.Timestep(dt); }
        catch (Exception) { return LocalContactStatus.SolverFailure; }
        if (metrics.Coverage?.Failed == true) return LocalContactStatus.SolverFailure;
        status = ValidateAuthority(engine, configuration, target);
        if (status != LocalContactStatus.Success) return status;
        var state = simulation.Bodies.GetBodyReference(body);
        var p = FromFloat(state.Pose.Position); var v = FromFloat(state.Velocity.Linear); var w = FromFloat(state.Velocity.Angular);
        var fq = state.Pose.Orientation;
        if (!Within(configuration, p, v, w) ||
            SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(configuration.LocalToRoot * new DoubleQuaternion(fq.X, fq.Y, fq.Z, fq.W), out var q)
                != SpacecraftRigidBodyRotationEvaluationStatus.Success) return LocalContactStatus.PrecisionEnvelopeExceeded;
        var elapsed = (target.Ticks - source.Motion.Time.Ticks) / (double)SimulationInstant.TicksPerSecond;
        var rootPosition = configuration.OriginRoot + configuration.OriginVelocityRoot * elapsed + configuration.LocalToRoot.Rotate(p);
        var rootVelocity = configuration.OriginVelocityRoot + configuration.LocalToRoot.Rotate(v);
        var omega = q.Conjugate().Rotate(configuration.LocalToRoot.Rotate(w));
        if (!LocalContactConfiguration.RootSpacingFits(rootPosition, configuration.ContactTolerance) ||
            !rootVelocity.IsFinite || !omega.IsFinite) return LocalContactStatus.PrecisionEnvelopeExceeded;
        var paired = source.Motion with { Time = target, Revision = powered?.Expected.StateRevision ?? publication?.Revision ?? source.Motion.Revision,
            Properties = powered?.Physical.Properties ?? source.Motion.Properties,
            PositionRoot = rootPosition, VelocityRoot = rootVelocity, BodyToRoot = q, AngularVelocityBody = omega };
        frontier++;
        export = new(paired, source.Motion.Time, source.TimelineRevision, Generation, frontier, metrics.Contacts,
            metrics.MaximumDepth, Math.Sqrt(p.LengthSquared), Math.Sqrt(v.LengthSquared), ImportPositionError, ImportVelocityError,
            state.Constraints.Count, configuration.Article is null ? 0 : metrics.ArticleChildMask);
        if (publication is not null) publication.Pending = true;
        if (powered is not null) powered.Pending = true;
        invalidated = false; next = new(this, frontier, receiptIdentity); return LocalContactStatus.Success;
    }

    internal LocalContactStatus Read(SimulationTransactionEngine engine, LocalContactConfiguration configuration, Receipt receipt, out Export result)
    {
        result = default;
        var status = Validate(engine, configuration, receipt, export.Motion.Time);
        if (status == LocalContactStatus.Success) result = export;
        return status;
    }

    private LocalContactStatus Validate(SimulationTransactionEngine engine, LocalContactConfiguration configuration, Receipt receipt, SimulationInstant target)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) return LocalContactStatus.WrongThread;
        if (disposed) return LocalContactStatus.Disposed;
        if (invalidated) return LocalContactStatus.Invalidated;
        if (!ReferenceEquals(receipt.Owner, this)) return LocalContactStatus.GenerationMismatch;
        if (receipt.Step != frontier) return LocalContactStatus.FrontierMismatch;
        if ((publication is not null || powered is not null) && (!receipt.IsIssuedBy(receiptIdentity) || receipt.Generation != Generation))
            return LocalContactStatus.GenerationMismatch;
        return ValidateAuthority(engine, configuration, target);
    }

    internal LocalContactStatus TryDispose()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) return LocalContactStatus.WrongThread;
        if (disposed) return LocalContactStatus.Disposed;
        disposed = true;
        // Shape buffers belong exclusively to this pool/generation.
        try
        {
            simulation.Statics.Remove(plane);
            simulation.Shapes.RemoveAndDispose(surfaceShape, pool);
            if (configuration.Article is not null)
            {
                simulation.Bodies.Remove(body);
                simulation.Shapes.RecursivelyRemoveAndDispose(bodyShape, pool);
            }
            simulation.Dispose();
        }
        finally { pool.Clear(); }
        return LocalContactStatus.Success;
    }
    public void Dispose()
    {
        if (TryDispose() == LocalContactStatus.WrongThread) throw new InvalidOperationException("Private world disposal requires its owner thread.");
    }
    private static bool PositiveFloat(double value) => (float)value > 0 && float.IsFinite((float)value);
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    private static Vector3 ToFloat(Double3 v) => new((float)v.X, (float)v.Y, (float)v.Z);
    private static Double3 FromFloat(Vector3 v) => new(v.X, v.Y, v.Z);
    private static bool Within(LocalContactConfiguration c, Double3 p, Double3 v, Double3 w) =>
        p.IsFinite && v.IsFinite && w.IsFinite && Math.Abs(p.X) + c.BoundingRadius + c.MaximumSpeculativeMargin < c.MaximumCoordinate &&
        Math.Abs(p.Y) + c.BoundingRadius + c.MaximumSpeculativeMargin < c.MaximumCoordinate &&
        Math.Abs(p.Z) + c.BoundingRadius + c.MaximumSpeculativeMargin < c.MaximumCoordinate &&
        Math.Sqrt(v.LengthSquared) + Math.Sqrt(w.LengthSquared) * c.BoundingRadius <= c.MaximumSpeed;
}
