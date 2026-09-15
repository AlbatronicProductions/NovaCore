using BepuPhysics;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    private sealed class PoweredBinding(SimulationTransactionEngine engine, PoweredFlightAuthority authority,
        SpacecraftPhysicalSource physical, SpacecraftDefinition definition, PoweredContactPreparation preparation,
        PoweredFlightObservation observation, LocalContactStepInput input)
    {
        internal readonly SimulationTransactionEngine Engine = engine;
        internal readonly PoweredFlightAuthority Authority = authority;
        internal readonly SpacecraftDefinition Definition = definition;
        internal readonly PoweredContactFixture Fixture = preparation.Fixture;
        internal readonly double Throttle = preparation.Throttle;
        internal readonly bool Ignite = preparation.Ignite;
        internal readonly LocalContactStepInput Input = input;
        internal readonly OrdinaryContactProjectionSchedule Projection = preparation.Projection;
        internal SpacecraftPhysicalSource Physical = physical;
        internal PoweredFlightObservation Expected = observation;
        internal bool Pending;
        internal long PreparedFrontier;
        internal long HostSequence;
    }
    private PoweredBinding? powered;
    internal bool IsPoweredContact => powered is not null;

    // Transfer constructor: never creates/reimports a second native body or advances simulation.
    internal LocalContactWorld(LocalContactSource source, PoweredContactPreparation prepared,
        SimulationTransactionEngine engine, PoweredFlightAuthority authority, SpacecraftPhysicalSource physical,
        SpacecraftDefinition definition, PoweredFlightObservation observation, BepuUtilities.Memory.BufferPool nativePool,
        BepuPhysics.Simulation nativeSimulation, BodyHandle nativeBody, StaticHandle nativePlane,
        BepuPhysics.Collidables.TypedIndex nativeBodyShape, BepuPhysics.Collidables.TypedIndex nativeSurfaceShape,
        LocalContactMetrics nativeMetrics, LocalContactStepInput nativeInput)
    {
        this.source = source; configuration = prepared.Configuration;
        pool = nativePool; simulation = nativeSimulation; body = nativeBody; plane = nativePlane;
        bodyShape = nativeBodyShape; surfaceShape = nativeSurfaceShape; metrics = nativeMetrics;
        powered = new(engine, authority, physical, definition, prepared, observation, nativeInput);
        ImportPositionError = prepared.PositionImportError; ImportVelocityError = prepared.VelocityImportError;
        export = new(source.Motion, source.Motion.Time, source.TimelineRevision, Generation, 0,
            metrics.Contacts, metrics.MaximumDepth, simulation.Bodies[body].Pose.Position.Length(),
            simulation.Bodies[body].Velocity.Linear.Length(), ImportPositionError, ImportVelocityError, simulation.Bodies[body].Constraints.Count);
    }

    internal static LocalContactWorld BindPoweredPreparation(LocalContactSource source, PoweredContactPreparation prepared,
        SimulationTransactionEngine engine, PoweredFlightAuthority authority, SpacecraftPhysicalSource physical,
        SpacecraftDefinition definition, PoweredFlightObservation observation, out Receipt receipt)
    {
        if (!engine.OwnsPersistentPublicationPhase || !prepared.Available)
            throw new InvalidOperationException("Prepared ownership transfer requires the admitted owner phase.");
        var world = prepared.TransferTo(source, engine, authority, physical, definition, observation);
        receipt = new(world, 0, world.receiptIdentity); return world;
    }

    internal PoweredFlightStatus CheckPoweredAuthority(SimulationTransactionEngine engine, PoweredFlightAuthority authority,
        Receipt receipt, bool allowPending)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) return PoweredFlightStatus.WrongOwnerThread;
        if (disposed || invalidated) return PoweredFlightStatus.Invalidated;
        return ComparePoweredAuthority(engine, authority, receipt, allowPending);
    }

    // Pure token comparison, also used after native mutation while the fail-closed poison is still set.
    private PoweredFlightStatus ComparePoweredAuthority(SimulationTransactionEngine engine, PoweredFlightAuthority authority,
        Receipt receipt, bool allowPending)
    {
        if (powered is not { } p || !ReferenceEquals(p.Engine, engine) || !ReferenceEquals(p.Authority, authority))
            return PoweredFlightStatus.InvalidAuthority;
        if (!ReferenceEquals(receipt.Owner, this) || receipt.Generation != Generation || !receipt.IsIssuedBy(receiptIdentity) ||
            receipt.Step != frontier) return PoweredFlightStatus.InvalidProposal;
        var view = engine.State;
        if (view.Revision != p.Expected.StateRevision || engine.ContactProofTimelineRevision != p.Expected.TimelineRevision ||
            engine.CaptureContinuationClock() != p.Expected.Clock || authority.Resource.Copy() != p.Expected.Resource ||
            !SpacecraftPhysicalSource.TryCapture(view.Spacecraft, source.Motion.Spacecraft, out var current) || !current.Same(p.Physical) ||
            !view.Spacecraft.TryGetDefinition(source.Motion.Spacecraft, out var definition) || definition != p.Definition)
            return PoweredFlightStatus.StaleSource;
        if (p.Pending && !allowPending) return PoweredFlightStatus.OutstandingProposal;
        if (frontier != p.Expected.Actuator.Frontier + (p.Pending ? 1 : 0)) return PoweredFlightStatus.InvalidProposal;
        return PoweredFlightStatus.Ready;
    }

    // Only input/step consumers may reuse the engine's continuously-owned source proof.
    // Endpoint reads deliberately continue through the full canonical comparison above.
    private PoweredFlightStatus CompareReadyPoweredAuthority(SimulationTransactionEngine engine,
        PoweredFlightAuthority authority, Receipt receipt, bool allowPending)
    {
        if (powered is not { } p || !ReferenceEquals(p.Engine, engine) || !ReferenceEquals(p.Authority, authority))
            return PoweredFlightStatus.InvalidAuthority;
        if (!ReferenceEquals(receipt.Owner, this) || receipt.Generation != Generation || !receipt.IsIssuedBy(receiptIdentity) ||
            receipt.Step != frontier) return PoweredFlightStatus.InvalidProposal;
        if (p.Pending && !allowPending) return PoweredFlightStatus.OutstandingProposal;
        return frontier == p.Expected.Actuator.Frontier + (p.Pending ? 1 : 0)
            ? PoweredFlightStatus.Ready : PoweredFlightStatus.InvalidProposal;
    }

    internal PoweredFlightStatus PreparePoweredInput(SimulationTransactionEngine engine, PoweredFlightAuthority authority,
        Receipt receipt, in PropellantSegmentationPreview segmentation, out OrdinaryContactInput input)
    {
        input = default;
        if (Environment.CurrentManagedThreadId != ownerThread) return PoweredFlightStatus.WrongOwnerThread;
        if (disposed || invalidated) return PoweredFlightStatus.Invalidated;
        var status = engine.OwnsReadyContactSource(this, authority)
            ? CompareReadyPoweredAuthority(engine, authority, receipt, false)
            : CheckPoweredAuthority(engine, authority, receipt, false);
        if (status != PoweredFlightStatus.Ready) return status;
        var p = powered!;
        if (!engine.OwnsPersistentPublicationPhase || segmentation.Engine.Start != p.Expected.Clock.Time ||
            !source.TryEndpoint(frontier + 1, out var target) || segmentation.Engine.End != target ||
            segmentation.SourceMass.TotalMassKilograms != p.Physical.Properties.MassKilograms ||
            segmentation.SourceMass.Inertia != p.Physical.Inertia || segmentation.SourceMass.Inertia.X != 2 ||
            segmentation.Engine.RequestedThrottle != p.Throttle ||
            segmentation.Engine.ProposedLatch != (p.Ignite ? ProposedEngineLatch.Enabled : ProposedEngineLatch.Off))
            return PoweredFlightStatus.OutsideModel;
        if (engine.HasContactProofBoundaryThrough(target)) return PoweredFlightStatus.PendingEvent;
        var q = p.Physical.IsEndpoint ? p.Physical.Endpoint.BodyToRoot : p.Physical.Angular.OrientationLocalToParent;
        var force = configuration.LocalToRoot.Conjugate().Rotate(q.Rotate(segmentation.Engine.ProposedForceBodyNewtons));
        var moment = configuration.LocalToRoot.Conjugate().Rotate(q.Rotate(segmentation.Engine.ProposedMomentBodyNewtonMetres));
        var ticks = target.Ticks - p.Expected.Clock.Time.Ticks;
        if (ticks is not (16666 or 16667) || !p.Projection.TryMap(segmentation.PoweredDuration,
            (uint)ticks, force, moment, p.Physical.Properties.MassKilograms, 2,
            PoweredContactPreparation.GravityLocal, out input)) return PoweredFlightStatus.NumericalFailure;
        return PoweredFlightStatus.Ready;
    }

    // Called by the selected consumer after all canonical preflight checks. No pose/velocity/cache writes.
    internal void InstallPoweredInput(in OrdinaryContactInput input)
    {
        if (powered is not { } p || !p.Engine.OwnsPersistentPublicationPhase || p.Pending || disposed || invalidated)
            throw new InvalidOperationException("Invalid powered numerical input ownership.");
        invalidated = true;
        var inertia = new BodyInertia { InverseMass = input.InverseMass };
        inertia.InverseInertiaTensor.XX = inertia.InverseInertiaTensor.YY = inertia.InverseInertiaTensor.ZZ = .5f;
        simulation.Bodies.GetBodyReference(body).SetLocalInertia(inertia);
        p.Input.Linear = input.LinearAcceleration; p.Input.Angular = input.AngularAcceleration;
        p.PreparedFrontier = frontier + 1;
        invalidated = false;
    }

    internal void InvalidatePoweredContinuation() => invalidated = true;
    internal Receipt CurrentPoweredReceipt => new(this, frontier, receiptIdentity);
    internal bool HasPoweredPendingEndpoint => powered?.Pending == true;

    internal double PoweredPenetration()
    {
        var state = simulation.Bodies.GetBodyReference(body);
        var q = state.Pose.Orientation;
        var rotation = new NovaCore.Core.DoubleQuaternion(q.X, q.Y, q.Z, q.W);
        var lowest = double.PositiveInfinity;
        for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                    lowest = Math.Min(lowest, state.Pose.Position.Y + rotation.Rotate(new(x, .5 * y, .5 * z)).Y);
        return Math.Max(0, -lowest);
    }

    internal PoweredFlightStatus ReadPoweredEndpoint(SimulationTransactionEngine engine, PoweredFlightAuthority authority,
        Receipt receipt, out Export result)
    {
        result = default;
        var status = CheckPoweredAuthority(engine, authority, receipt, true);
        if (status != PoweredFlightStatus.Ready) return status;
        if (!powered!.Pending) return PoweredFlightStatus.InvalidProposal;
        result = export; return PoweredFlightStatus.Prepared;
    }

    internal readonly struct PoweredAcknowledgement
    {
        private readonly PoweredBinding binding;
        private readonly PoweredFlightObservation observation;
        private readonly SpacecraftPhysicalSource physical;
        private readonly long sequence;
        private readonly bool host;
        private PoweredAcknowledgement(PoweredBinding binding, PoweredFlightObservation observation,
            SpacecraftPhysicalSource physical, long sequence, bool host)
        { this.binding = binding; this.observation = observation; this.physical = physical; this.sequence = sequence; this.host = host; }
        internal static bool TryPrepare(LocalContactWorld world, SimulationTransactionEngine engine,
            PoweredFlightObservation observation, long sequence, bool host, out PoweredAcknowledgement acknowledgement)
        {
            acknowledgement = default;
            if (!engine.OwnsPersistentPublicationPhase || world.powered is not { } p || world.disposed || world.invalidated ||
                !ReferenceEquals(engine, p.Engine) || p.Pending == host ||
                (host ? p.HostSequence == long.MaxValue || sequence != p.HostSequence + 1 :
                    observation.Actuator.Frontier != world.frontier)) return false;
            var physical = host ? p.Physical : new SpacecraftPhysicalSource(true, observation.Endpoint, default, default, observation.Endpoint.Properties);
            acknowledgement = new(p, observation, physical, sequence, host); return true;
        }
        internal void Commit()
        {
            binding.Expected = observation; binding.Physical = physical;
            if (host) binding.HostSequence = sequence;
            else binding.Pending = false;
        }
    }
}
