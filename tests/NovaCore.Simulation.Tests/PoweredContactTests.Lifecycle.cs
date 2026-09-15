using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Translation;

internal static partial class PoweredContactTests
{
    private static void NoReadyPermission(Contact c)
    {
        Check(!c.Engine.OwnsReadyContactSource(c.World, c.Power), "released owner has no readiness");
        Check(c.Clock.PublicationPhase.TryEnter(c.Engine), "manual phase for readiness refusal");
        try { Check(!c.Engine.OwnsReadyContactSource(c.World, c.Power), "phase ownership alone cannot issue interval readiness"); }
        finally { c.Clock.PublicationPhase.Exit(); }
    }

    internal static void Lifecycle()
    {
        Check(OrdinaryContactProjectionSchedule.TryPrepare(out var schedule), "cold projection schedule");
        var one = PropellantInteger.FromUInt64(1);
        foreach (var ticks in new[] { 16666U, 16667U })
        {
            var durations = new[] { new PropellantDuration(default, one),
                new PropellantDuration(PropellantInteger.FromUInt64(ticks), one),
                new PropellantDuration(PropellantInteger.FromUInt64(ticks), PropellantInteger.FromUInt64(2)),
                new PropellantDuration(one, PropellantInteger.FromUInt64(ulong.MaxValue)) };
            foreach (var duration in durations)
            {
                Check(OrdinaryContactInputProjection.TryMap(duration, ticks, 1, new(48, 64, 0), new(-6.4, 4.8, 0),
                    8.000130208333333, 2, PoweredContactPreparation.GravityLocal, out var original) &&
                    schedule.TryMap(duration, ticks, new(48, 64, 0), new(-6.4, 4.8, 0), 8.000130208333333, 2,
                    PoweredContactPreparation.GravityLocal, out var prepared) && original == prepared,
                    "cold schedule and original mapper identical");
            }
        }
        Check(!default(OrdinaryContactProjectionSchedule).TryMap(new(default, one), 16666, default, default, 8, 2, default, out _) &&
            !schedule.TryMap(default, 16666, default, default, 8, 2, default, out _) &&
            !schedule.TryMap(new(default, one), 16665, default, default, 8, 2, default, out _) &&
            !schedule.TryMap(new(PropellantInteger.FromUInt64(16667), one), 16666, default, default, 8, 2, default, out _),
            "unprepared/invalid denominator/wrong H/out-of-interval projection refuses");

        foreach (var fixture in Enum.GetValues<PoweredContactFixture>())
        {
            using var direct = new Contact(fixture, capacity: 4);
            using var ready = new Contact(fixture, capacity: 4);
            for (var i = 0; i < 3; i++)
            {
                direct.Seal(); direct.Apply(); Complete(ready);
                Check(ready.Engine.TryGetPoweredFlightRecord(i, out var actual) && direct.Engine.TryGetPoweredFlightRecord(i, out var expected) &&
                    actual == expected && actual.Endpoint.SameBits(expected.Endpoint) &&
                    NativeBits(ready).SequenceEqual(NativeBits(direct)), "ready/direct full record and native bits " + fixture);
                NoReadyPermission(ready);
            }
        }
        using (var c = new Contact())
        {
            c.Seal(); c.Engine.RetireFinitePropellant(c.Resource, c.FuelLease); c.Engine.DiscardSingleEngineProposal(c.Actuation, c.EngineLease);
            var before = Canonical(c); var native = NativeBits(c);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.EngineBlocked, "refusal inside readiness");
            Unchanged(c, before, native, "ready refusal"); NoReadyPermission(c);
        }
        using (var c = new Contact())
        {
            c.Credit(16666); var before = Canonical(c);
            var native = Native(c); native.Simulation.Bodies[native.Body].Velocity.Linear.X = (float)(2 * c.Prepared.Configuration.MaximumSpeed);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.NumericalFailure && Canonical(c) == before &&
                c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.Invalidated, "ready native failure poisons without canonical debit");
            NoReadyPermission(c);
        }
        using (var c = new Contact())
        {
            c.Credit(16666);
            ((SpacecraftPhysicalProperties[])Field(c.Store, "_properties"))[0] = new(9);
            var before = Canonical(c); var native = NativeBits(c);
            Check(c.Engine.ServicePoweredContactDebt(c.Power).Status == PoweredFlightStatus.StaleSource, "unrevised mass mutation prevents readiness");
            Unchanged(c, before, native, "mass mutation"); NoReadyPermission(c);
        }
        using (var c = new Contact())
        {
            Complete(c); c.Seal();
            Check(c.Engine.PreparePoweredContact(c.Power, c.FuelLease, out c.PhysicalLease) == PoweredFlightStatus.Prepared, "separate prepare after ready service");
            var owner = Field(c.Engine, "_poweredFlight"); var record = (PoweredFlightRecord)Field(owner, "Prepared");
            Set(owner, "Prepared", record with { Endpoint = record.Endpoint with { PositionRoot = record.Endpoint.PositionRoot + Double3.UnitX } });
            var before = Canonical(c); var native = NativeBits(c);
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.InvalidProposal, "separate retry remains fully checked");
            Unchanged(c, before, native, "retry mismatch"); NoReadyPermission(c);
            Set(owner, "Prepared", record);
            Check(c.Engine.PublishPoweredContact(c.Power, c.PhysicalLease).Status == PoweredFlightStatus.Published &&
                NativeBits(c).SequenceEqual(native), "valid retry publishes without solver replay"); NoReadyPermission(c);
        }
        Console.WriteLine("POWERED_CONTACT_LIFECYCLE PASS fixtures=11 frontiers=3 exact_record_native_identity=PASS projection=PASS refusal_cleanup=PASS retry=PASS");
    }
}
