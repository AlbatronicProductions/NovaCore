using System.Reflection;
using NovaCore.Core;
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
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static object Field(object x, string name) => x.GetType().GetField(name, Fields)!.GetValue(x)!;
    private static void Set(object x, string name, object value) => x.GetType().GetField(name, Fields)!.SetValue(x, value);
    private readonly record struct Bundle(SpacecraftPhysicalSource Physical, PropellantSourceObservation Resource,
        PoweredFlightObservation Observation, ContinuationClockState Clock, StateRevision Revision);
    private static Bundle Canonical(Contact c)
    {
        Check(SpacecraftPhysicalSource.TryCapture(c.Engine.State.Spacecraft, Contact.Craft, out var physical), "canonical source");
        return new(physical, c.Resource.Copy(), (PoweredFlightObservation)Field(Field(c.Engine, "_poweredFlight"), "Observation"),
            c.Engine.CaptureContinuationClock(), c.Engine.State.Revision);
    }
    private static void Unchanged(Contact c, Bundle before, int[] native, string why)
        => Check(Canonical(c) == before && NativeBits(c).SequenceEqual(native), "canonical/private nonmutation " + why);

    internal static void Authority()
    {
        using (var c = new Contact())
        {
            var before = Canonical(c); var bits = NativeBits(c);
            Check(c.Engine.AdmitPoweredContactHostTime(c.Power, 1, new(-1)).Status == PoweredFlightStatus.InvalidInput, "negative host");
            Check(c.Engine.AdmitPoweredContactHostTime(c.Power, 1, default).Status == PoweredFlightStatus.NoWork, "zero host");
            Unchanged(c, before, bits, "invalid/zero credit");
            c.Credit(16665); before = Canonical(c);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.AwaitingDebt, "one tick short");
            Check(c.Engine.AdmitPoweredContactHostTime(c.Power, 1, new(1)).Status == PoweredFlightStatus.InvalidSequence, "duplicate credit");
            Check(c.Engine.AdmitPoweredContactHostTime(c.Power, 2, new(long.MaxValue)).Status == PoweredFlightStatus.ArithmeticOverflow, "credit overflow");
            Unchanged(c, before, bits, "short/duplicate/overflow");
            c.Credit(1);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).PublishedCount == 1 && c.Clock.CurrentTime.Ticks == 16666, "exact interval boundary");
        }
        using (var c = new Contact())
        using (var foreign = new Contact())
        {
            c.Seal(); var before = Canonical(c); var bits = NativeBits(c);
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _, true) == PoweredFlightStatus.PreparationRefused, "early refusal");
            Check(c.Engine.PreparePoweredContact(c.Power, default, out _) == PoweredFlightStatus.InvalidProposal, "default resource lease");
            Check(c.Engine.PreparePoweredContact(foreign.Power, c.FuelLease, out _) == PoweredFlightStatus.InvalidAuthority, "foreign engine authority");
            Check(Task.Run(() => c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _)).GetAwaiter().GetResult() == PoweredFlightStatus.WrongOwnerThread, "wrong owner thread");
            Unchanged(c, before, bits, "early/foreign/thread");
            Check(c.Clock.PublicationPhase.TryEnter(c), "test owned phase");
            try
            {
                Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _) == PoweredFlightStatus.Reentrant &&
                    c.Engine.AdmitPoweredContactHostTime(c.Power, 2, new(1)).Status == PoweredFlightStatus.Reentrant &&
                    c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Reentrant, "reentrant paths");
            }
            finally { c.Clock.PublicationPhase.Exit(); }
            Unchanged(c, before, bits, "reentrant");
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out c.PhysicalLease) == PoweredFlightStatus.Prepared, "staged pending");
            before = Canonical(c); bits = NativeBits(c);
            Check(c.Engine.PublishPoweredContact(c.Power, default).Status == PoweredFlightStatus.InvalidProposal &&
                c.Engine.PublishPoweredContact(c.Power, new(c.PhysicalLease.Generation, new object())).Status == PoweredFlightStatus.InvalidProposal, "default/fabricated joint lease");
            Check(c.Engine.AdmitPoweredContactHostTime(c.Power, 2, new(1)).Status == PoweredFlightStatus.OutstandingProposal &&
                c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _) == PoweredFlightStatus.OutstandingProposal, "one outstanding world endpoint");
            var storage = Field(c.Engine, "_poweredFlight"); var record = (PoweredFlightRecord)Field(storage, "Prepared");
            Set(storage, "Prepared", record with { Endpoint = record.Endpoint with { PositionRoot = record.Endpoint.PositionRoot with { X = Math.BitIncrement(record.Endpoint.PositionRoot.X) } } });
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.InvalidProposal, "exact native endpoint mismatch");
            Set(storage, "Prepared", record); Unchanged(c, before, bits, "pending refusal");
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.Published, "pending retry publishes existing step");
            Check(NativeBits(c).SequenceEqual(bits), "retry cannot solve twice");
            before = Canonical(c);
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.InvalidProposal, "consumed/duplicate joint lease");
            Unchanged(c, before, bits, "duplicate");
            Check(c.Engine.PreviewSingleEngineActuation(c.Actuation, c.EngineLease, out _) == EnginePreparationStatus.InvalidProposal &&
                c.Engine.PreviewFinitePropellant(c.Resource, c.FuelLease, out _) == PropellantPreparationStatus.InvalidProposal, "all source leases consumed");
        }
        foreach (var mutation in new[] { "debt", "pause", "rate", "state", "timeline", "physical", "mass", "force", "torque", "configuration", "resource", "world", "generation", "frontier" })
        {
            using var c = new Contact(); using var foreign = new Contact();
            c.Seal(); var owner = Field(c.Engine, "_poweredFlight");
            switch (mutation)
            {
                case "debt": c.Clock.AdvanceByHostDuration(new(1)); break;
                case "pause": c.Clock.Pause(); break;
                case "rate": c.Clock.TrySetRate(SimulationRate.Two); break;
                case "state": Set(Field(c.Engine, "_state"), "_revision", new StateRevision(1)); break;
                case "timeline": c.Clock.Timeline.Schedule(default, new(new(1), new(100000), 0, NovaCore.Simulation.Timeline.SimulationEventKind.Marker)); break;
                case "physical":
                case "force":
                    var t = (SpacecraftTranslationState[])Field(c.Store, "_translations");
                    t[0] = mutation == "physical" ? t[0] with { PositionRoot = new(1, .5, 0) } : t[0] with { ConstantForceRoot = Double3.UnitX }; break;
                case "mass": ((SpacecraftPhysicalProperties[])Field(c.Store, "_properties"))[0] = new(9); break;
                case "torque":
                    var r = (SpacecraftRigidBodyRotationState[])Field(c.Store, "_rigidBodies"); r[0] = r[0] with { ConstantBodyTorque = Double3.UnitY }; break;
                case "configuration":
                    var d = (SpacecraftDefinition[])Field(c.Store, "_definitions"); d[0] = d[0] with { DiagnosticName = "changed" }; break;
                case "resource":
                    var fuel = Field(c.Engine, "_propellantPreparation"); Set(fuel, "Canonical", c.Resource.Copy() with { ResourceRevision = 1 }); break;
                case "world": Set(owner, "Contact", foreign.World); break;
                case "generation": Set(c.World, "<Generation>k__BackingField", c.World.Generation + 1); break;
                case "frontier": Set(c.World, "frontier", 1L); break;
            }
            var before = Canonical(c); var bits = NativeBits(c);
            var result = c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _);
            Check(result is PoweredFlightStatus.StaleSource or PoweredFlightStatus.InvalidAuthority or PoweredFlightStatus.InvalidProposal, "stale authority " + mutation + " " + result);
            Unchanged(c, before, bits, mutation);
        }
        foreach (var at in new[] { 16665L, 16666L })
        {
            using var c = new Contact(eventAt: at); c.Credit(16666); var before = Canonical(c); var bits = NativeBits(c);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.PendingEvent, "event at/before endpoint"); Unchanged(c, before, bits, "event");
        }
        using (var c = new Contact(revision: ulong.MaxValue))
        { c.Seal(); var before = Canonical(c); var bits = NativeBits(c); Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _) == PoweredFlightStatus.RevisionOverflow, "revision overflow"); Unchanged(c, before, bits, "revision overflow"); }
        using (var c = new Contact(capacity: 1))
        { c.Seal(); c.Apply(); c.Credit(16667); var before = Canonical(c); var bits = NativeBits(c); Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.HistoryCapacity, "capacity"); Unchanged(c, before, bits, "capacity"); }
        foreach (var parent in new[] { false, true })
        {
            using var c = new Contact(); c.Seal();
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out c.PhysicalLease) == PoweredFlightStatus.Prepared, "retirement pending");
            if (parent) c.Engine.DiscardSingleEngineProposal(c.Actuation, c.EngineLease); else c.Engine.RetireFinitePropellant(c.Resource, c.FuelLease);
            var before = Canonical(c); var bits = NativeBits(c);
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status is PoweredFlightStatus.StaleSource or PoweredFlightStatus.InvalidProposal, "retired dependency blocks publication");
            Check(c.Engine.RetirePoweredContactProposal(c.Power, c.PhysicalLease) == PoweredFlightStatus.Retired, "native endpoint cannot be rolled back for replay");
            Check(Canonical(c) == before && NativeBits(c).SequenceEqual(bits) && c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated, "retirement no debit/re-step");
        }
        using (var c = new Contact())
        {
            c.Send(SpacecraftCommandIntent.Shutdown()); c.Seal(); var before = Canonical(c); var bits = NativeBits(c);
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _) == PoweredFlightStatus.OutsideModel, "finite command sequence admission"); Unchanged(c, before, bits, "unqualified command sequence");
        }
        using (var c = new Contact())
        {
            c.Seal(); Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out c.PhysicalLease) == PoweredFlightStatus.Prepared, "terminal stage");
            var result = c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease, true);
            Check(result.Status == PoweredFlightStatus.CanonicalCommittedPrivateInvalidated && result.CanonicalCommitted && result.Observation.HistoryCount == 1 &&
                c.Resource.ResourceRevision == 1 && c.Engine.State.Revision.Value == 1 && c.Clock.CurrentTime.Ticks == 16666 && c.Clock.PendingSimulationDebt.Ticks == 0,
                "physical terminal preserves exact committed bundle");
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated, "terminal cannot retry");
        }
        using (var c = new Contact())
        {
            var result = c.Engine.AdmitPoweredContactHostTime(c.Power, 1, new(16666), true);
            Check(result.Status == PoweredFlightStatus.CanonicalCommittedPrivateInvalidated && c.Clock.PendingSimulationDebt.Ticks == 16666 &&
                c.Clock.CurrentTime.Ticks == 0 && c.Resource.ResourceRevision == 0 && c.Engine.State.Revision.Value == 0 &&
                c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated, "host terminal retains accounting only");
        }
        using (var c = new Contact())
        {
            c.Seal(); var before = Canonical(c); var native = Native(c);
            // Dedicated failure injection: finite but deliberately outside private export envelope.
            native.Simulation.Bodies[native.Body].Velocity.Linear.X = (float)(2 * c.Prepared.Configuration.MaximumSpeed);
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out _) == PoweredFlightStatus.NumericalFailure && Canonical(c) == before &&
                c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated, "post-step export failure spends no fuel and poisons continuation");
        }
        using (var c = new Contact())
        { var before = Canonical(c); c.World.Dispose(); Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated && Canonical(c) == before, "disposed world"); }
        PropertyOnly();
        FinalRefusalCloseout();
        Console.WriteLine("POWERED_CONTACT_AUTHORITY PASS precommit_nonmutation pending_retry joint_lease_once source_resource_world_guards terminal_physical terminal_credit export_failure property_only");
    }
    private static void FinalRefusalCloseout()
    {
        using (var c = new Contact())
        {
            c.Seal(); Check(c.Engine.PreparePoweredContact(c.Power,c.FuelLease,out c.PhysicalLease)==PoweredFlightStatus.Prepared,"command-stale pending endpoint");
            c.Engine.SaturateCommandRevisionForTest(c.Commands);
            var before=Canonical(c);var bits=NativeBits(c);
            // Publication maps an unreadable resource/engine parent to InvalidProposal before any writes.
            var refusal=c.Engine.PublishPoweredContact(c.Power,c.PhysicalLease).Status;
            Check(refusal==PoweredFlightStatus.InvalidProposal,"command revision changed after native step: "+refusal);
            Unchanged(c,before,bits,"command-stale publication refuses without second step or debit");
        }
        using (var c = new Contact())
        {
            var preview=c.Seal();var before=Canonical(c);var bits=NativeBits(c);
            Check(c.Clock.PublicationPhase.TryEnter(c.Engine),"wrong interval admission phase");
            try
            {
                var wrong=preview with { Engine=preview.Engine with { End=new(16667) } };
                Check(c.World.PreparePoweredInput(c.Engine,c.Power,c.World.CurrentPoweredReceipt,wrong,out _)==PoweredFlightStatus.OutsideModel,
                    "wrong exact interval cannot reach native input installation");
            }
            finally { c.Clock.PublicationPhase.Exit(); }
            Unchanged(c,before,bits,"wrong interval");
        }
        Console.WriteLine("POWERED_CONTACT_REFUSAL_CLOSEOUT PASS stale_command_after_step wrong_interval_before_input no_extra_debit_or_step");
    }
    private static void PropertyOnly()
    {
        using var c = new Contact(); c.Seal(); c.Apply(); var second = c.Seal();
        var n = Native(c); var bits = NativeBits(c); var before = Canonical(c);
        var oldMass = n.Simulation.Bodies[n.Body].LocalInertia.InverseMass;
        var constraint = n.Simulation.Bodies[n.Body].Constraints[0].ConnectingConstraintHandle;
        Check(c.Clock.PublicationPhase.TryEnter(c.Engine), "property update owner phase");
        try
        {
            Check(c.World.PreparePoweredInput(c.Engine, c.Power, c.World.CurrentPoweredReceipt, second, out var input) == PoweredFlightStatus.Ready, "genuine next property input");
            c.World.InstallPoweredInput(input);
        }
        finally { c.Clock.PublicationPhase.Exit(); }
        var current = n.Simulation.Bodies[n.Body].LocalInertia;
        Check(current.InverseMass != oldMass && current.InverseMass == (float)(1 / before.Physical.Properties.MassKilograms) &&
            NativeBits(c).SequenceEqual(bits) && Canonical(c) == before && !c.World.HasPoweredPendingEndpoint &&
            constraint == n.Simulation.Bodies[n.Body].Constraints[0].ConnectingConstraintHandle, "same body property-only update preserves pose/velocity/constraint/cache and canonical state");
    }
}
