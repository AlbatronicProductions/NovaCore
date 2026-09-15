using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class PoweredContactTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Powered contact: " + message); }

    private sealed class Contact : IDisposable
    {
        internal static readonly SpacecraftId Craft = new(101);
        internal readonly PoweredContactPreparation Prepared;
        internal readonly SimulationClock Clock;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftStateStore Store;
        internal readonly SpacecraftCommandAuthority Commands;
        internal readonly EnginePreparationAuthority Actuation;
        internal readonly PropellantResourceAuthority Resource;
        internal readonly PoweredFlightAuthority Power;
        internal readonly LocalContactWorld World;
        internal PropellantProposal FuelLease;
        internal EngineActuationProposal EngineLease;
        internal PoweredFlightProposal PhysicalLease;
        internal long Sequence;
        internal PoweredFlightObservation Observe => Engine.ObservePoweredContact(Power).Observation;

        internal Contact(PoweredContactFixture fixture = PoweredContactFixture.CenteredEndpoint, int capacity = 1200,
            Double3 frameVelocity = default, long? eventAt = null, ulong revision = 0)
        {
            var root = new ReferenceFrameId(1); var body = new ReferenceFrameId(2);
            Check(PoweredContactPreparation.TryPrepare(fixture, root, default, frameVelocity, out var preparation) == PoweredFlightStatus.Ready, "cold fixture");
            Prepared = preparation!;
            var graph = new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(root, null, ReferenceFrameKind.Ecl, "qualification inertial root"));
            graph.Add(new ReferenceFrameNode(body, root, ReferenceFrameKind.Ccf, "declared dry COM"));
            var p = Prepared.Initial;
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, root, body, "supported numerical dry-8 I2 body")],
                [new(Craft, default, p.Orientation, p.AngularVelocity, new(2, 2, 2), default, RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(Prepared.ResourceDefinition.InitialTotalMassKilograms)],
                [new(Craft, root, default, p.Position, p.Velocity, default)], graph.Build(), out var store, out _), "initial canonical copy");
            Store = store!;
            Clock = new(default, new SimulationTimeline(8));
            if (eventAt is { } tick) Clock.Timeline.Schedule(default, new(new(1), new(tick), 0, SimulationEventKind.Marker));
            Engine = new(Clock, new SimulationState(spacecraft: Store, initialRevision: new(revision)), 8);
            Check(Engine.PrepareSpacecraftCommands(Craft, 1200, out var commands) == SpacecraftCommandStatus.Accepted, "commands bind"); Commands = commands!;
            Check(Engine.BindSingleEnginePreparation(Commands, Prepared.EngineDefinition, out var actuation) == EnginePreparationStatus.Ready, "engine bind"); Actuation = actuation!;
            Check(Engine.BindFinitePropellant(Actuation, Prepared.ResourceDefinition, out var resource) == PropellantPreparationStatus.Ready, "resource bind"); Resource = resource!;
            Check(Engine.BeginPoweredContact(Resource, Prepared, capacity, out var authority, out var world) == PoweredFlightStatus.Ready, "contact bind");
            Power = authority!; World = world!;
            if (Prepared.Ignite) Send(SpacecraftCommandIntent.Ignite());
            Send(SpacecraftCommandIntent.ThrottlePosition(Prepared.Throttle));
        }

        internal void Send(SpacecraftCommandIntent intent)
        {
            Engine.ObserveSpacecraftCommands(Commands, out var state);
            Check(Engine.AdmitSpacecraftCommand(Commands, 1, state.LastAcceptedSequence + 1, intent).Status == SpacecraftCommandStatus.Accepted, "command admission");
        }
        internal void Credit(long ticks)
        {
            var r = Engine.AdmitPoweredContactHostTime(Power, Sequence + 1, new(ticks));
            Check(r.Status == PoweredFlightStatus.AcceptedCredit, "credit " + r.Status); Sequence++;
        }
        internal PropellantSegmentationPreview Seal(bool fund = true)
        {
            Check(Commands.TryBoundaryAtOrAfter(Clock.CurrentTime, Observe.Actuator.Frontier + 1, out _, out var target), "next boundary");
            if (fund) Credit(target.Ticks - Clock.CurrentTime.Ticks);
            for (var i = 0; i < 8; i++)
            {
                var command = Engine.CommitNextSpacecraftCommand(Commands).Status;
                if (command is SpacecraftCommandStatus.NoCommand or SpacecraftCommandStatus.Pending) break;
                Check(command is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange, "command commit");
            }
            Check(Engine.CloseSpacecraftCommandBoundary(Commands, out _) == SpacecraftCommandStatus.BoundaryReady, "close boundary");
            Check(Engine.PrepareSingleEngineActuation(Actuation, target, out EngineLease) == EnginePreparationStatus.Prepared, "engine proposal");
            Check(Engine.PrepareFinitePropellant(Resource, EngineLease, target, out FuelLease) == PropellantPreparationStatus.Prepared, "resource proposal");
            Check(Engine.PreviewFinitePropellant(Resource, FuelLease, out var preview) == PropellantPreparationStatus.Preview, "preview");
            return preview;
        }
        internal PoweredFlightResult Apply()
        {
            var p = Engine.PreparePoweredContact(Power, FuelLease, out PhysicalLease);
            Check(p == PoweredFlightStatus.Prepared, "contact preparation " + p);
            var r = Engine.PublishPoweredContact(Power, PhysicalLease);
            Check(r.Status == PoweredFlightStatus.Published, "joint publication " + r.Status);
            return r;
        }
        public void Dispose() { World.Dispose(); Prepared.Dispose(); }
    }

    internal static void Cheap()
    {
        Check(PropellantInteger.TryFromKilograms(.00013020833333333333, out var fuel) &&
            PropellantInteger.TryDecodeFlow(.015625, out var flow), "literal mapper operands");
        PropellantInteger.TryDecodeFlow(.015625, out flow);
        var hp = new PropellantDuration(fuel, flow);
        Check(OrdinaryContactInputProjection.TryMap(hp, 1_000_000, 60, new(48, 64, 0), new(-6.4, 4.8, 0),
            8.000130208333333, 2, new(0, -9.81, 0), out var input), "literal mapper");
        int[] observed = [BitConverter.SingleToInt32Bits(input.LinearAcceleration.X), BitConverter.SingleToInt32Bits(input.LinearAcceleration.Y),
            BitConverter.SingleToInt32Bits(input.AngularAcceleration.X), BitConverter.SingleToInt32Bits(input.AngularAcceleration.Y),
            BitConverter.SingleToInt32Bits(input.InverseMass), BitConverter.SingleToInt32Bits(input.Duration)];
        Check(observed.SequenceEqual(new[] { 1077935923, -1061557234, -1077097267, 1067030938, 1040187119, 1015580809 }), "six retained native bits");
        Console.WriteLine("POWERED_CONTACT_MAPPER PASS six_native_bits=PASS");
        using var c = new Contact();
        var start = c.Observe;
        Check(!c.Prepared.Available, "one-use preparation consumed");
        Check(c.Engine.BeginPersistentContact(c.World, c.Prepared.Configuration, c.World.CurrentPoweredReceipt) != LocalContactStatus.Success,
            "unpowered publisher cannot bind the powered world");
        Check(c.Engine.ObservePoweredContact(null!).Status == PoweredFlightStatus.InvalidAuthority &&
            c.Engine.ObservePoweredFreeFlight(null!).Status == PoweredFlightStatus.InvalidAuthority, "null authority typed refusal");
        Check(PoweredContactPreparation.TryPrepare(PoweredContactFixture.OffCom, new(1), default, new(1e20, 0, 0), out _) ==
            PoweredFlightStatus.OutsideModel, "unresolved moving-frame source refused");
        Check(c.Engine.BeginPoweredContact(c.Resource, c.Prepared, 1200, out _, out _) == PoweredFlightStatus.InvalidAuthority, "duplicate bind");
        Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.AwaitingDebt && c.Observe == start, "no pre-funded work");
        Check(c.Engine.ServicePoweredFlightDebt(c.Power).Status == PoweredFlightStatus.InvalidAuthority, "exclusive contact consumer");
        var first = c.Seal();
        Check(first.Classification == PropellantClassification.FullPowered, "first full powered");
        Check(c.Resource.Copy() == start.Resource, "preview never debits");
        var a = c.Apply().Observation;
        Check(a.Endpoint.Epoch.Ticks == 16666 && a.Resource.RemainingUnits == first.SuccessorUnits &&
            a.Endpoint.Properties.MassKilograms == first.ProposedSuccessorMass.TotalMassKilograms &&
            a.Actuator.Activity == ActualEngineActivity.ProducingOutput && a.StateRevision.Value == 1 && a.HistoryCount == 1 &&
            a.Clock.Debt.Ticks == 0 && a.TimelineRevision == start.TimelineRevision, "first joint successor");
        Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.InvalidProposal && c.Observe == a, "duplicate no debit");
        var second = c.Seal();
        Check(second.Classification == PropellantClassification.EndpointExhaustion, "exact endpoint exhaustion");
        var b = c.Apply().Observation;
        Check(b.Endpoint.Epoch.Ticks == 33333 && b.Resource.RemainingUnits.IsZero && b.Endpoint.Properties.MassKilograms == 8 &&
            b.Actuator.Activity == ActualEngineActivity.EnabledNoFeed && b.Actuator.EndpointThrottle == 0 && b.Resource.ResourceRevision == 2, "exhausted joint successor");
        var third = c.Seal();
        Check(third.Classification == PropellantClassification.NoFeed && third.ConsumedUnits.IsZero, "dry interval");
        var d = c.Apply().Observation;
        Check(d.Endpoint.Epoch.Ticks == 50000 && d.Resource == b.Resource && d.StateRevision.Value == 3 && d.HistoryCount == 3 &&
            d.Actuator.Activity == ActualEngineActivity.EnabledNoFeed && c.World.PoweredPenetration() <= .001, "same world dry continuation");
        Console.WriteLine("POWERED_CONTACT_CHEAP PASS full=16666 endpoint_exhaustion=33333 dry=50000 exact_resource=PASS joint_successor=PASS");
    }
}
