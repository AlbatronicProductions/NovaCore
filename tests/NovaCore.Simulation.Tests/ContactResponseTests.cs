using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class ContactResponseTests
{
    private static readonly SpacecraftId Craft = new(71);
    private static readonly ReferenceFrameId Root = new(1);
    private static readonly SpacecraftPhysicalProperties Mass = new(8);
    private static readonly ContactResponseProvenance Provenance = new(
        new(Craft, ContactGenerationFixture.Geometry(1).Identity, 1),
        new(6, new(2, 5), 4, 16, new string('A', 64), new string('B', 64), 1, 1, 1), 401);
    // Declared before comparison: IEEE binary64 u, gamma_n = n*u/(1-n*u).
    // Operation budgets include quaternion normalization, independent matrix conversion,
    // component arithmetic and cancellation against the pre-impulse momentum.
    private const double U = 1.1102230246251565e-16;
    private static double Gamma(int n) => n * U / (1 - n * U);
    private static double _maxVelocityError, _maxOmegaError, _maxLinearMomentumError, _maxAngularMomentumError;

    internal static void Run()
    {
        Contracts(); PayloadOwnership(); Numerical(); Rejections(); Preflight(); HistoricalTime(); ReplayAndCadence(); IndependentTransactions(); AnalyticalOracle(); Allocations();
        Console.WriteLine($"Contact response numerical: maxDeltaVError={_maxVelocityError:R}; maxDeltaOmegaError={_maxOmegaError:R}; " +
            $"maxLinearMomentumError={_maxLinearMomentumError:R}; maxAngularMomentumRootError={_maxAngularMomentumError:R}; " +
            "bars=gamma32/gamma256/gamma64/gamma512 times stated scales; poses=exact; replay=exact");
        Console.WriteLine("PASS Contact response: impulse contracts, coupled publication, analytical conservation, forgery/preflight rejection, exact-time replay and independent force/torque compatibility");
    }

    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("Contact response: " + message); }

    private sealed record Fixture(SimulationTransactionEngine Engine, SimulationClock Clock, SimulationState Authority);
    private static Fixture Create(int capacity = 16, int? contactCapacity = null, SimulationInstant epoch = default,
        DoubleQuaternion? orientation = null, Double3? omega = null, Double3? velocity = null,
        Double3? force = null, Double3? torque = null, ulong revision = 0, SimulationTimeline? timeline = null)
    {
        var rotation = new SpacecraftRigidBodyRotationState(Craft, epoch, orientation ?? DoubleQuaternion.Identity,
            omega ?? new Double3(.125, -.25, .5), new(2, 3, 4), torque ?? Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
        var translation = new SpacecraftTranslationState(Craft, Root, epoch, new(150_000_000_000, -32, 8),
            velocity ?? new Double3(2, -1, .5), force ?? Double3.Zero);
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, new(72), "Coupled impulse fixture")],
            [rotation], [Mass], [translation], ContactGenerationFixture.Graph(), out var store, out _), "fixture admission");
        var authority = new SimulationState(spacecraft: store, initialRevision: new(revision));
        var clock = new SimulationClock(epoch, timeline ?? new SimulationTimeline(capacity, Math.Max(1, capacity)));
        return new(new SimulationTransactionEngine(clock, authority, capacity, contactImpulseHistoryCapacity: contactCapacity ?? capacity), clock, authority);
    }

    private static SpacecraftTranslationState Translation(Fixture f)
    {
        Check(f.Engine.State.Spacecraft.TryGetTranslation(Craft, out var value, out var mass) && mass == Mass, "mass retained");
        return value;
    }
    private static SpacecraftRigidBodyRotationState Rotation(Fixture f)
    { Check(f.Engine.State.Spacecraft.TryGetRigidBody(Craft, out var value), "rotation available"); return value; }
    private static SpacecraftContactImpulseIntent Intent(Fixture f, Double3 impulse, Double3 offset, SimulationInstant? time = null,
        ulong? revision = null) => new(Craft, new(revision ?? f.Engine.State.Revision.Value), time ?? f.Clock.CurrentTime, Root, impulse, offset, Provenance);
    private static void Schedule(Fixture f, SpacecraftContactImpulseIntent intent, ulong id = 1, int priority = 0)
    {
        Check(f.Clock.Timeline.ScheduleContactImpulse(f.Clock.CurrentTime, new(id), priority, intent).Succeeded, "typed intent admitted and event scheduled");
    }
    private static SpacecraftContactImpulseTransaction Candidate(Fixture f)
    {
        var result = f.Engine.EvaluateNext();
        Check(result.ContactImpulseReplacement.HasValue, "coupled candidate produced");
        return result.ContactImpulseReplacement!.Value;
    }
    private sealed record Snapshot(SpacecraftTranslationState Translation, SpacecraftRigidBodyRotationState Rotation,
        StateRevision Revision, TimelineRevision TimelineRevision, SimulationInstant Time, int Pending,
        int History, int ContactHistory, int ForceHistory, int TorqueHistory, int AttitudeHistory, int Payloads,
        object? LastContact, object? LastEvent, object? PendingEvent);
    private static Snapshot Capture(Fixture f) => new(Translation(f), Rotation(f), f.Engine.State.Revision,
        f.Clock.Timeline.Revision, f.Clock.CurrentTime, f.Clock.Timeline.PendingCount, f.Engine.ProcessedCount,
        f.Engine.ProcessedContactImpulseCount, f.Engine.ProcessedSpacecraftForceCount, f.Engine.ProcessedRigidBodyTorqueCount,
        f.Engine.ProcessedSpacecraftAttitudeCount, f.Clock.Timeline.ContactPayloadCount,
        f.Engine.TryGetProcessedContactImpulse(f.Engine.ProcessedContactImpulseCount - 1, out var contact) ? contact : null,
        f.Engine.TryGetProcessed(f.Engine.ProcessedCount - 1, out var canonical) ? canonical : null,
        f.Clock.Timeline.TryPeekPending(out var pending) ? pending : null);
    private static void Unchanged(Fixture f, Snapshot before, string why) => Check(Capture(f) == before && f.Clock.Timeline.ValidateContactPayloadInvariants(), why + ": both states, all histories, revisions, clock, pending event and payload ownership unchanged");

    private static void Contracts()
    {
        var f = Create(); var intent = Intent(f, new(8, 0, 0), Double3.Zero);
        Check(intent.IsValid, "valid root impulse");
        foreach (var bad in new[] { intent with { Spacecraft = default }, intent with { RootFrame = default },
            intent with { ImpulseRoot = Double3.Zero }, intent with { ImpulseRoot = new(double.NaN, 0, 0) },
            intent with { ImpulseRoot = new(double.PositiveInfinity, 0, 0) }, intent with { OffsetFromComRoot = new(0, double.NaN, 0) },
            intent with { OffsetFromComRoot = new(0, 0, double.NegativeInfinity) }, intent with { Version = 0 }, intent with { Version = 2 },
            intent with { Provenance = default }, intent with { Provenance = Provenance with { ObservationId = 0 } },
            intent with { Provenance = Provenance with { Feature = Provenance.Feature with { Spacecraft = new(999) } } },
            intent with { Provenance = Provenance with { Feature = Provenance.Feature with { FeatureId = 0 } } },
            intent with { Provenance = Provenance with { Feature = Provenance.Feature with { Geometry = new(101, 1, "not-a-sha256") } } } })
        {
            var before = Capture(f);
            Check(!bad.IsValid && !f.Clock.Timeline.ScheduleContactImpulse(f.Clock.CurrentTime, new(1), 0, bad).Succeeded, "structurally invalid intent rejected before scheduling");
            Unchanged(f, before, "invalid intent/zero impulse policy");
        }
        Check(f.Engine.ValidateAndCommit(default(SpacecraftContactImpulseTransaction)) != ContactImpulseStatus.Success, "default candidate fails closed");
    }

    private static void Numerical()
    {
        foreach (var q in new[] { DoubleQuaternion.Identity, new DoubleQuaternion(.5, .5, .5, .5),
            DoubleQuaternion.FromAxisAngle(new Double3(1, 2, 3) / Math.Sqrt(14), .731) })
        foreach (var r in new[] { Double3.Zero, new Double3(2, -1, .5), new Double3(-4, 3, 2) })
        {
            var f = Create(orientation: q); var beforeT = Translation(f); var beforeR = Rotation(f);
            var intent = Intent(f, new(3.5, -2.25, 1.125), r); Schedule(f, intent);
            var snapshot = Capture(f); var c = Candidate(f); Unchanged(f, snapshot, "pure evaluation");
            Check(f.Engine.ValidateAndCommit(c) == ContactImpulseStatus.Success, "typed coupled commit");
            AssertPhysical(beforeT, beforeR, Translation(f), Rotation(f), intent);
            var derived = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(Rotation(f), intent.Time);
            Check(derived.Succeeded && QuaternionError(derived.OrientationLocalToParent, Rotation(f).OrientationLocalToParent) <= Gamma(32),
                "existing derived quaternion normalization stays within gamma32; stored pose continuity remains exact");
            Check(f.Engine.State.Revision.Value == 1 && f.Engine.ProcessedCount == 1 && f.Engine.ProcessedContactImpulseCount == 1 &&
                f.Clock.Timeline.PendingCount == 0 && f.Clock.Timeline.Revision.Value == snapshot.TimelineRevision.Value + 1, "one publication and atomic event consumption");
            Check(f.Engine.ProcessedSpacecraftForceCount == 0 && f.Engine.ProcessedRigidBodyTorqueCount == 0 &&
                f.Engine.ProcessedSpacecraftAttitudeCount == 0, "one coupled history without fictitious force/torque histories");
            Check(f.Engine.TryGetProcessedContactImpulse(0, out var history) && history.Intent == intent &&
                history.ExpectedTranslation == beforeT && history.ExpectedRotation == beforeR &&
                history.ReplacementTranslation == Translation(f) && history.ReplacementRotation == Rotation(f) &&
                history.Before == snapshot.Revision && history.After == f.Engine.State.Revision && history.Event == c.Event,
                "history retains full physical provenance, exact event, before/after revisions and both states");
            var committed = Capture(f);
            Check(f.Engine.ValidateAndCommit(c) != ContactImpulseStatus.Success, "consumed response cannot apply twice");
            Unchanged(f, committed, "duplicate commit");
        }
        var center = Create(); var offset = Create();
        Schedule(center, Intent(center, new(8, 0, 0), Double3.Zero)); Schedule(offset, Intent(offset, new(8, 0, 0), Double3.UnitY));
        Check(center.Engine.ExecuteCanonicalPendingEvent().Committed && offset.Engine.ExecuteCanonicalPendingEvent().Committed, "canonical routing accepts contacts");
        Check(Translation(center).VelocityRoot == Translation(offset).VelocityRoot && Rotation(center).AngularVelocityBody != Rotation(offset).AngularVelocityBody,
            "equal impulse different lever arm: same translation, different rotation");
    }

    private static void PayloadOwnership()
    {
        Check(System.Runtime.CompilerServices.Unsafe.SizeOf<ScheduledSimulationEvent>() == 112 &&
            !System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<ScheduledSimulationEvent>(),
            "generic records retain banked size without managed references");
        Check(System.Runtime.CompilerServices.Unsafe.SizeOf<SpacecraftContactImpulseIntent>() == 200,
            "typed intent retains physical/provenance representation");
        Console.WriteLine($"Contact payload structure: generic={System.Runtime.CompilerServices.Unsafe.SizeOf<ScheduledSimulationEvent>()}; " +
            $"intent={System.Runtime.CompilerServices.Unsafe.SizeOf<SpacecraftContactImpulseIntent>()}; " +
            $"slot={System.Runtime.CompilerServices.Unsafe.SizeOf<SimulationTimeline.ContactPayloadSlot>()}; genericReferences=false");
        var ordinary = new SimulationTimeline(16);
        var f = Create(timeline: new SimulationTimeline(32, 1));
        var t = f.Clock.Timeline;
        var intent = Intent(f, Double3.UnitX, Double3.UnitY);
        Check(ordinary.ContactPayloadCapacity == 0 && ordinary.ContactPayloadCount == 0 &&
            ordinary.ScheduleContactImpulse(default, new(1), 0, intent).Status == SimulationScheduleStatus.ContactPayloadCapacityExceeded,
            "ordinary timeline has no arena; contact capacity is explicit");
        Schedule(f, intent); Check(t.TryPeekPending(out var first), "first payload pending");
        var proposal = Candidate(f); var before = Capture(f);
        Check(t.TryResolveContactImpulse(first, out var resolved) && resolved == intent, "canonical slot resolves exact immutable intent");
        Check(t.Schedule(default, new(new(2), default, 0, SimulationEventKind.SpacecraftContactImpulse, first.Payload)).Status == SimulationScheduleStatus.InvalidPayload,
            "generic requests cannot inject typed slot handles");
        Check(t.ScheduleContactImpulse(default, new(2), 0, intent).Status == SimulationScheduleStatus.ContactPayloadCapacityExceeded &&
            t.ReplaceContactImpulse(default, new(1), new(2), 0, intent).Status == SimulationScheduleStatus.ContactPayloadCapacityExceeded,
            "full arena rejects admission/replacement without consuming old payload");
        Unchanged(f, before, "full arena");
        Check(t.ContactPayloadCount == 1 && t.ValidateContactPayloadInvariants(), "full arena intact");
        Check(t.Cancel(new(1)).Succeeded && t.ContactPayloadCount == 0 && !t.TryResolveContactImpulse(first, out _) &&
            t.ValidateContactPayloadInvariants(), "cancel clears and retires payload");
        Check(!t.Cancel(new(1)).Succeeded && t.ContactPayloadCount == 0, "double cancel cannot free twice");
        Schedule(f, intent with { ImpulseRoot = Double3.UnitY }, 2); Check(t.TryPeekPending(out var second), "reused slot pending");
        Check(t.TryResolveContactImpulse(second, out var secondIntent), "reused slot resolves");
        Check(first.Payload.ContactSlot == second.Payload.ContactSlot && first.Header.Sequence != second.Header.Sequence &&
            !t.TryResolveContactImpulse(first, out _) && secondIntent.ImpulseRoot == Double3.UnitY,
            "existing unique sequence prevents slot ABA");
        var reused = Capture(f);
        Check(f.Engine.ValidateAndCommit(proposal) != ContactImpulseStatus.Success, "proposal from prior slot occupancy rejected");
        Unchanged(f, reused, "retired proposal");
        Check(!t.TryResolveContactImpulse(new(second.Header, SimulationEventPayload.ContactHandle(2)), out _), "forged slot cannot resolve");
        Check(f.Engine.ExecuteCanonicalPendingEvent().Committed && t.ContactPayloadCount == 0 && !t.TryResolveContactImpulse(second, out _) &&
            t.ValidateContactPayloadInvariants(), "commit consumes and clears payload");
        Check(f.Engine.TryGetProcessedContactImpulse(0, out var history) && history.Intent == secondIntent, "history owns value provenance after retirement");
        Schedule(f, Intent(f, Double3.UnitZ, Double3.Zero), 3);
        Check(f.Engine.TryGetProcessedContactImpulse(0, out var retained) && retained == history, "slot reuse cannot mutate committed history");
        Check(t.Replace(default, new(3), new(new(4), default, 0, SimulationEventKind.Marker)).Succeeded && t.ContactPayloadCount == 0,
            "generic replacement retires old contact payload");

        var replacements = Create(timeline: new SimulationTimeline(32, 2)); var rt = replacements.Clock.Timeline;
        Schedule(replacements, Intent(replacements, Double3.UnitX, Double3.Zero)); rt.TryPeekPending(out var old);
        Check(rt.ReplaceContactImpulse(default, new(1), new(2), 0, Intent(replacements, Double3.UnitY, Double3.Zero)).Succeeded &&
            rt.ContactPayloadCount == 1 && !rt.TryResolveContactImpulse(old, out _) && rt.ValidateContactPayloadInvariants(), "typed replacement publishes new then retires old");
        rt.TryPeekPending(out var next); var revision = rt.Revision;
        Check(rt.ReplaceContactImpulse(default, new(2), new(2), 0, intent).Status == SimulationScheduleStatus.DuplicateId &&
            rt.Revision == revision && rt.ContactPayloadCount == 1 && rt.TryResolveContactImpulse(next, out _), "failed replacement retains original slot");

        var exhaustedSequence = new SimulationTimeline(1, ulong.MaxValue, TimelineRevision.Zero, 1);
        Check(exhaustedSequence.ScheduleContactImpulse(default, new(1), 0, intent).Status == SimulationScheduleStatus.SequenceOverflow &&
            exhaustedSequence.ContactPayloadCount == 0 && exhaustedSequence.ValidateContactPayloadInvariants(), "generation cannot wrap");
        var finalSequence = new SimulationTimeline(1, ulong.MaxValue - 1, TimelineRevision.Zero, 1);
        Check(finalSequence.ScheduleContactImpulse(default, new(1), 0, intent).Succeeded && finalSequence.Cancel(new(1)).Succeeded &&
            finalSequence.ScheduleContactImpulse(default, new(2), 0, intent).Status == SimulationScheduleStatus.SequenceOverflow &&
            finalSequence.ContactPayloadCount == 0, "retired final sequence is never reused");

        var stress = new SimulationTimeline(20_000, 1);
        for (ulong id = 1; id <= 100; id++) Check(stress.Schedule(default, new(new(id), default, 0, SimulationEventKind.Marker)).Succeeded, "marker population");
        for (ulong id = 101; id <= 10_000; id++)
        {
            Check(stress.ScheduleContactImpulse(default, new(id), 0, intent).Succeeded, "bounded contact slot admission");
            Check(stress.Cancel(new(id)).Succeeded && stress.ContactPayloadCount == 0, "bounded contact cancellation");
        }
        Check(stress.PendingCount == 100 && stress.CancelledCount == 9900 && stress.ValidateInvariants() && stress.ValidateContactPayloadInvariants(),
            "9900 contact slot reuses preserve markers and free-list ownership");
        Console.WriteLine("PASS Contact payload lifetime: explicit capacity, cancellation, retry, replacement, sequence ABA/overflow, history independence, 9900 slot reuses");
    }

    private static void AssertPhysical(SpacecraftTranslationState beforeT, SpacecraftRigidBodyRotationState beforeR,
        SpacecraftTranslationState afterT, SpacecraftRigidBodyRotationState afterR, SpacecraftContactImpulseIntent intent)
    {
        Check(SameBits(beforeT.PositionRoot, afterT.PositionRoot) && SameQuaternionBits(beforeR.OrientationLocalToParent, afterR.OrientationLocalToParent),
            "instantaneous position and orientation continuity is exact");
        Check(afterT.Epoch == intent.Time && afterR.Epoch == intent.Time && afterT.ConstantForceRoot == beforeT.ConstantForceRoot &&
            afterR.ConstantBodyTorque == beforeR.ConstantBodyTorque && afterR.PrincipalInertia == beforeR.PrincipalInertia &&
            afterT.RootFrame == beforeT.RootFrame && afterR.Model == beforeR.Model, "epoch and continuous-force/inertia/model authority retained");
        var dv = intent.ImpulseRoot / 8;
        var dl = CrossDecimal(intent.OffsetFromComRoot, intent.ImpulseRoot);
        var dlBody = MatrixRotate(beforeR.OrientationLocalToParent, dl, transpose: true);
        var i = beforeR.PrincipalInertia;
        var dw = new Double3(dlBody.X / i.X, dlBody.Y / i.Y, dlBody.Z / i.Z);
        var actualDv = afterT.VelocityRoot - beforeT.VelocityRoot;
        var actualDw = afterR.AngularVelocityBody - beforeR.AngularVelocityBody;
        _maxVelocityError = Math.Max(_maxVelocityError, Error(actualDv, dv));
        _maxOmegaError = Math.Max(_maxOmegaError, Error(actualDw, dw));
        Near(actualDv, dv, Gamma(32) * (Norm(beforeT.VelocityRoot) + Norm(dv)), "delta velocity gamma32");
        Near(actualDw, dw, Gamma(256) * (Norm(beforeR.AngularVelocityBody) + Norm(dlBody) / Math.Min(i.X, Math.Min(i.Y, i.Z))), "delta omega gamma256");
        var dp = actualDv * 8;
        var actualDl = MatrixRotate(beforeR.OrientationLocalToParent,
            new(i.X * actualDw.X, i.Y * actualDw.Y, i.Z * actualDw.Z));
        _maxLinearMomentumError = Math.Max(_maxLinearMomentumError, Error(dp, intent.ImpulseRoot));
        _maxAngularMomentumError = Math.Max(_maxAngularMomentumError, Error(actualDl, dl));
        Near(dp, intent.ImpulseRoot, Gamma(64) * (8 * Norm(beforeT.VelocityRoot) + Norm(intent.ImpulseRoot)), "linear momentum gamma64");
        Near(actualDl, dl, Gamma(512) * (Math.Max(i.X, Math.Max(i.Y, i.Z)) * Norm(beforeR.AngularVelocityBody) + Norm(intent.OffsetFromComRoot) * Norm(intent.ImpulseRoot)), "root angular momentum gamma512");
        if (intent.OffsetFromComRoot == Double3.Zero) Check(afterR.AngularVelocityBody == beforeR.AngularVelocityBody, "COM impulse exactly preserves omega");
    }
    private static Double3 CrossDecimal(Double3 a, Double3 b) => new(
        (double)((decimal)a.Y * (decimal)b.Z - (decimal)a.Z * (decimal)b.Y),
        (double)((decimal)a.Z * (decimal)b.X - (decimal)a.X * (decimal)b.Z),
        (double)((decimal)a.X * (decimal)b.Y - (decimal)a.Y * (decimal)b.X));
    // Independent matrix oracle; does not call production Quaternion.Rotate/Conjugate or inertia helper.
    private static Double3 MatrixRotate(DoubleQuaternion q, Double3 v, bool transpose = false)
    {
        var xx = q.X * q.X; var yy = q.Y * q.Y; var zz = q.Z * q.Z;
        var xy = q.X * q.Y; var xz = q.X * q.Z; var yz = q.Y * q.Z;
        var wx = q.W * q.X; var wy = q.W * q.Y; var wz = q.W * q.Z;
        var s = transpose ? -1d : 1d;
        return new((1 - 2 * (yy + zz)) * v.X + 2 * (xy - s * wz) * v.Y + 2 * (xz + s * wy) * v.Z,
            2 * (xy + s * wz) * v.X + (1 - 2 * (xx + zz)) * v.Y + 2 * (yz - s * wx) * v.Z,
            2 * (xz - s * wy) * v.X + 2 * (yz + s * wx) * v.Y + (1 - 2 * (xx + yy)) * v.Z);
    }
    private static double Norm(Double3 v) => Math.Abs(v.X) + Math.Abs(v.Y) + Math.Abs(v.Z);
    private static bool SameBits(Double3 a, Double3 b) => BitConverter.DoubleToInt64Bits(a.X) == BitConverter.DoubleToInt64Bits(b.X) &&
        BitConverter.DoubleToInt64Bits(a.Y) == BitConverter.DoubleToInt64Bits(b.Y) && BitConverter.DoubleToInt64Bits(a.Z) == BitConverter.DoubleToInt64Bits(b.Z);
    private static bool SameQuaternionBits(DoubleQuaternion a, DoubleQuaternion b) => SameBits(new(a.X, a.Y, a.Z), new(b.X, b.Y, b.Z)) &&
        BitConverter.DoubleToInt64Bits(a.W) == BitConverter.DoubleToInt64Bits(b.W);
    private static double QuaternionError(DoubleQuaternion a, DoubleQuaternion b) => Math.Max(Math.Max(Math.Abs(a.X-b.X), Math.Abs(a.Y-b.Y)), Math.Max(Math.Abs(a.Z-b.Z), Math.Abs(a.W-b.W)));
    private static double Error(Double3 a, Double3 b) => Math.Max(Math.Abs(a.X - b.X), Math.Max(Math.Abs(a.Y - b.Y), Math.Abs(a.Z - b.Z)));
    private static void Near(Double3 a, Double3 b, double bound, string name) => Check(Error(a, b) <= bound, $"{name}: error={Error(a, b):R}, bound={bound:R}");

    private static void Rejections()
    {
        var f = Create(); Schedule(f, Intent(f, new(8, 0, 0), Double3.UnitY));
        var outer = f.Engine.EvaluateNext(); var c = Candidate(f); var before = Capture(f);
        foreach (var forged in new[] {
            c with { Event = new(new(999), c.Event.Time, c.Event.Priority, c.Event.Sequence, c.Event.Kind) },
            c with { ReplacementTranslation = c.ReplacementTranslation with { PositionRoot = Double3.Zero } },
            c with { ReplacementTranslation = c.ReplacementTranslation with { VelocityRoot = Double3.Zero } },
            c with { ReplacementTranslation = c.ReplacementTranslation with { ConstantForceRoot = Double3.UnitX } },
            c with { ReplacementTranslation = c.ReplacementTranslation with { RootFrame = new(72) } },
            c with { ReplacementRotation = c.ReplacementRotation with { OrientationLocalToParent = new(.5, .5, .5, .5) } },
            c with { ReplacementRotation = c.ReplacementRotation with { AngularVelocityBody = Double3.Zero } },
            c with { ReplacementRotation = c.ReplacementRotation with { PrincipalInertia = new(2, 0, 4) } },
            c with { ReplacementRotation = c.ReplacementRotation with { ConstantBodyTorque = Double3.UnitX } },
            c with { ExpectedTranslation = c.ExpectedTranslation with { Epoch = new(-1) } },
            c with { ExpectedRotation = c.ExpectedRotation with { Epoch = new(-1) } },
            c with { Intent = c.Intent with { ExpectedStateRevision = new(99) } },
            c with { Intent = c.Intent with { Spacecraft = new(999) } },
            c with { Intent = c.Intent with { RootFrame = new(72) } },
            c with { Intent = c.Intent with { Time = new(1) } },
            c with { Intent = c.Intent with { ImpulseRoot = new(double.NaN, 0, 0) } },
            c with { Intent = c.Intent with { OffsetFromComRoot = new(double.PositiveInfinity, 0, 0) } },
            c with { Intent = c.Intent with { Provenance = Provenance with { ObservationId = 999 } } },
            c with { Intent = c.Intent with { Provenance = Provenance with { Support = Provenance.Support with { CompositionIdentity = 2 } } } },
            c with { Intent = c.Intent with { Provenance = Provenance with { Feature = Provenance.Feature with { FeatureId = 2 } } } }
        })
        { Check(f.Engine.ValidateAndCommit(forged) != ContactImpulseStatus.Success, "forged half/intent rejected"); Unchanged(f, before, "forged candidate"); }
        foreach (var forged in new[] { outer with { ContactImpulseReplacement = null, ChangesAuthoritativeState = false },
            outer with { EvaluationTime = new(1) }, outer with { ExpectedStateRevision = new(99) },
            outer with { ChangesAuthoritativeState = false }, outer with { IsInternallyConsistent = false },
            outer with { ContactImpulseReplacement = c with { ReplacementRotation = default } } })
        { Check(!f.Engine.ValidateAndCommit(forged).Committed, "canonical wrapper forgery rejected"); Unchanged(f, before, "forged wrapper"); }
        Check(f.Engine.ValidateAndCommit(outer).Committed, "rejected proposals leave original canonical transaction committable");

        var stale = Create(); Schedule(stale, Intent(stale, Double3.UnitX, Double3.UnitY)); var old = Candidate(stale);
        var torque = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(stale.Engine.State, new(Craft, Double3.UnitX, stale.Clock.CurrentTime));
        Check(torque.Succeeded && stale.Engine.ValidateAndCommit(torque.Transaction!.Value).Committed, "intervening authoritative torque change");
        var staleBefore = Capture(stale);
        Check(stale.Engine.ValidateAndCommit(old) != ContactImpulseStatus.Success && !stale.Engine.ExecuteCanonicalPendingEvent().Committed,
            "stale expected revision cannot be silently refreshed"); Unchanged(stale, staleBefore, "stale intent");
        var timelineStale = Create(); Schedule(timelineStale, Intent(timelineStale, Double3.UnitX, Double3.UnitY)); var oldTimeline = Candidate(timelineStale);
        Schedule(timelineStale, Intent(timelineStale, Double3.UnitY, Double3.Zero, new(1)), 2);
        var timelineBefore = Capture(timelineStale);
        Check(timelineStale.Engine.ValidateAndCommit(oldTimeline) != ContactImpulseStatus.Success, "timeline revision mismatch rejected");
        Unchanged(timelineStale, timelineBefore, "stale timeline");
        foreach (var invalid in new[] { Intent(f, Double3.UnitX, Double3.Zero) with { RootFrame = new(72) },
            Intent(f, Double3.UnitX, Double3.Zero) with { Spacecraft = new(999), Provenance = Provenance with { Feature = Provenance.Feature with { Spacecraft = new(999) } } } })
        {
            var unknown = Create(); Schedule(unknown, invalid with { ExpectedStateRevision = StateRevision.Zero }); var unknownBefore = Capture(unknown);
            Check(!unknown.Engine.ExecuteCanonicalPendingEvent().Committed, "canonical unknown craft/non-root frame rejected"); Unchanged(unknown, unknownBefore, "invalid canonical authority");
        }
    }

    private static void Preflight()
    {
        foreach (var (f, status) in new[] { (Create(capacity: 0, contactCapacity: 1), ContactImpulseStatus.HistoryCapacityFailure),
            (Create(capacity: 1, contactCapacity: 0), ContactImpulseStatus.HistoryCapacityFailure),
            (Create(revision: ulong.MaxValue), ContactImpulseStatus.StateRevisionOverflow),
            (Create(timeline: new SimulationTimeline(1, 1, new(ulong.MaxValue - 1), 1)), ContactImpulseStatus.TimelineRevisionOverflow) })
        {
            Schedule(f, Intent(f, Double3.UnitX, Double3.UnitY)); var before = Capture(f);
            var c = Candidate(f);
            Check(f.Engine.ValidateAndCommit(c) == status, "history/revision preflight reports the exact terminal resource");
            Unchanged(f, before, "preflight failure");
        }
        var overflow = Create(); Schedule(overflow, Intent(overflow, new(double.MaxValue, double.MaxValue, 0), new(double.MaxValue, 0, 0)));
        var overflowBefore = Capture(overflow);
        Check(!overflow.Engine.ExecuteCanonicalPendingEvent().Committed, "finite inputs with overflowing response rejected");
        Unchanged(overflow, overflowBefore, "nonfinite response");
        var full = Create(capacity: 1); Schedule(full, Intent(full, Double3.UnitX, Double3.Zero));
        Check(full.Engine.ExecuteCanonicalPendingEvent().Committed, "fill history with existing valid record");
        Schedule(full, Intent(full, Double3.UnitY, Double3.UnitX), 2); var fullBefore = Capture(full);
        Check(full.Engine.ValidateAndCommit(Candidate(full)) == ContactImpulseStatus.HistoryCapacityFailure, "filled history capacity rejected");
        Unchanged(full, fullBefore, "capacity failure preserves existing history contents");
    }

    private static void HistoricalTime()
    {
        // Negative exact epochs are legitimate history-domain instants, not requests to rewind current authority.
        var f = Create(epoch: new(-2_000_000), force: new(4, 2, -1), torque: new(.125, -.25, .5));
        var time = new SimulationInstant(-999_999); Schedule(f, Intent(f, new(8, 2, -1), new(2, -1, .5), time));
        var beforeClock = Capture(f);
        Check(!f.Engine.ExecuteCanonicalPendingEvent().Committed, "future intent cannot move authoritative clock");
        Unchanged(f, beforeClock, "future event");
        f.Clock.AdvanceTo(time);
        var oldT = Translation(f); var oldR = Rotation(f);
        var t = SpacecraftTranslationEvaluator.TryEvaluate(oldT, Mass, time);
        var r = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(oldR, time);
        Check(t.Succeeded && r.Succeeded, "both authorities evaluate to exact historical-domain event");
        var eventT = oldT with { Epoch = time, PositionRoot = t.PositionRoot, VelocityRoot = t.VelocityRoot };
        var eventR = oldR with { Epoch = time, OrientationLocalToParent = r.OrientationLocalToParent, AngularVelocityBody = r.AngularVelocityBody };
        var c = Candidate(f); Check(f.Engine.ValidateAndCommit(c) == ContactImpulseStatus.Success, "one-microtick off second exact event");
        AssertPhysical(eventT, eventR, Translation(f), Rotation(f), c.Intent);
        Check(!f.Clock.Timeline.ScheduleContactImpulse(f.Clock.CurrentTime, new(9), 0, c.Intent with { Time = new(-2_000_001) }).Succeeded, "past event cannot rewind committed state");
    }

    private static (Snapshot State, ulong Hash) Replay(int frames)
    {
        var f = Create(omega: Double3.Zero); var time = new SimulationInstant(1_000_001);
        Schedule(f, Intent(f, new(8, 0, 0), Double3.Zero, time, 0), 1, 0);
        Schedule(f, Intent(f, new(0, 4, 0), Double3.UnitX, time, 1), 2, 1);
        for (var frame = 0; frame <= frames; frame++)
        {
            var target = new SimulationInstant(2_000_000L * frame / frames);
            while (f.Clock.CurrentTime < target)
            {
                f.Clock.AdvanceTo(target);
                if (f.Clock.Timeline.TryPeekPending(out var pending) && pending.Header.Time == f.Clock.CurrentTime)
                    Check(f.Engine.ExecuteCanonicalGroup().IsComplete, "same-time impulse group");
            }
            Check(SpacecraftMotionEvaluator.TryEvaluate(f.Engine.State, Craft, target, out _) == SpacecraftTranslationStatus.Success, "read-only render observation");
        }
        return (Capture(f), BitHash(f));
    }
    private static void ReplayAndCadence()
    {
        var baseline = Replay(1); Check(Replay(1) == baseline, "identical replay includes full coupled history");
        foreach (var frames in new[] { 30, 60, 144, 1000 }) Check(Replay(frames) == baseline, "render FPS independent exact state and history");
        Check(baseline.State.Revision.Value == 2 && baseline.State.ContactHistory == 2 && baseline.State.History == 2, "same-time responses advance one revision each");
        Console.WriteLine($"Contact response deterministic state+complete history bit hash: 0x{baseline.Hash:X16}; cadences=1,30,60,144,1000; identical");
    }

    private static ulong BitHash(Fixture f)
    {
        // Explicit stable FNV-1a byte encoding, never runtime-randomized GetHashCode.
        ulong hash = 14695981039346656037UL;
        void Word(ulong value) { for (var k = 0; k < 8; k++) { hash = unchecked((hash ^ (byte)value) * 1099511628211UL); value >>= 8; } }
        void Scalar(double value) => Word(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        void Vector(Double3 value) { Scalar(value.X); Scalar(value.Y); Scalar(value.Z); }
        void Text(string value) { Word((ulong)value.Length); foreach (var c in value) Word(c); }
        void Linear(SpacecraftTranslationState value)
        { Word(value.Spacecraft.Value); Word(unchecked((ulong)value.RootFrame.Value)); Word(unchecked((ulong)value.Epoch.Ticks)); Vector(value.PositionRoot); Vector(value.VelocityRoot); Vector(value.ConstantForceRoot); }
        void Angular(SpacecraftRigidBodyRotationState value)
        { Word(value.Spacecraft.Value); Word(unchecked((ulong)value.Epoch.Ticks)); var q = value.OrientationLocalToParent; Scalar(q.X); Scalar(q.Y); Scalar(q.Z); Scalar(q.W); Vector(value.AngularVelocityBody); Scalar(value.PrincipalInertia.X); Scalar(value.PrincipalInertia.Y); Scalar(value.PrincipalInertia.Z); Vector(value.ConstantBodyTorque); Word((ulong)value.Model); }
        void Header(SimulationEventHeader value)
        { Word(value.Id.Value); Word(unchecked((ulong)value.Time.Ticks)); Word(unchecked((ulong)value.Priority)); Word(value.Sequence.Value); Word((ulong)value.Kind); }
        void Source(ContactResponseProvenance value)
        {
            Word(value.Feature.Spacecraft.Value); Word(value.Feature.FeatureId); Word(value.Feature.Geometry.DefinitionId); Word(value.Feature.Geometry.Version); Text(value.Feature.Geometry.ContentSha256);
            var s = value.Support; Word(s.BodyId); Word(s.Terrain.SourceId); Word(s.Terrain.Version); Word(s.PhysicalGeneration); Scalar(s.ReferenceRadiusMetres);
            Text(s.GlobalSha256); Text(s.RegionalSha256); Word(s.CompositionIdentity); Word(s.FacilitySupportIdentity); Word(s.QueryPolicyVersion); Word(value.ObservationId);
        }
        Linear(Translation(f)); Angular(Rotation(f)); Word(f.Engine.State.Revision.Value); Word(f.Clock.Timeline.Revision.Value); Word(unchecked((ulong)f.Clock.CurrentTime.Ticks));
        Word((ulong)f.Engine.ProcessedContactImpulseCount);
        for (var index = 0; index < f.Engine.ProcessedContactImpulseCount; index++)
        {
            Check(f.Engine.TryGetProcessedContactImpulse(index, out var record), "hash contact history");
            Header(record.Event); Word(record.Before.Value); Word(record.After.Value);
            var intent = record.Intent; Word(intent.Spacecraft.Value); Word(intent.ExpectedStateRevision.Value); Word(unchecked((ulong)intent.Time.Ticks)); Word(unchecked((ulong)intent.RootFrame.Value));
            Vector(intent.ImpulseRoot); Vector(intent.OffsetFromComRoot); Source(intent.Provenance); Word(intent.Version);
            Linear(record.ExpectedTranslation); Linear(record.ReplacementTranslation); Angular(record.ExpectedRotation); Angular(record.ReplacementRotation);
        }
        Word((ulong)f.Engine.ProcessedCount);
        for (var index = 0; index < f.Engine.ProcessedCount; index++)
        {
            Check(f.Engine.TryGetProcessed(index, out var record), "hash canonical history"); Header(record.Event);
            Word(unchecked((ulong)record.ExecutionTime.Ticks)); Word(record.TimelineRevisionBefore.Value); Word(record.TimelineRevisionAfter.Value);
            Word(record.StateRevisionBefore.Value); Word(record.StateRevisionAfter.Value);
        }
        return hash;
    }

    private static void IndependentTransactions()
    {
        var f = Create(); Schedule(f, Intent(f, Double3.UnitX, Double3.UnitY)); Check(f.Engine.ExecuteCanonicalPendingEvent().Committed, "contact before independent control");
        var afterContact = Capture(f);
        Check(SimulationEventRequest.TryCreateSpacecraftForce(new(2), 0, new(Craft, f.Clock.CurrentTime, new(8, 0, 0)), out var force) &&
            f.Clock.Timeline.Schedule(f.Clock.CurrentTime, force).Succeeded && f.Engine.ExecuteCanonicalPendingEvent().Committed, "independent force transaction still works");
        Check(Rotation(f) == afterContact.Rotation && f.Engine.ProcessedSpacecraftForceCount == 1 && f.Engine.ProcessedContactImpulseCount == 1, "force retains rotation and contact history");
        var afterForce = Translation(f);
        var torque = RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(f.Engine.State, new(Craft, new(.25, 0, 0), f.Clock.CurrentTime));
        Check(torque.Succeeded && f.Engine.ValidateAndCommit(torque.Transaction!.Value).Committed && Translation(f) == afterForce &&
            f.Engine.ProcessedRigidBodyTorqueCount == 1 && f.Engine.ProcessedContactImpulseCount == 1 && f.Engine.State.Revision.Value == 3,
            "independent torque retains translation and contact history");
    }

    private static void AnalyticalOracle()
    {
        // Test-only one point, zero friction/gap, prescribed moving support, no competing contacts.
        // Restitution is an explicit fixture parameter; this function is not a production response policy.
        foreach (var e in new[] { 0d, .5d, 1d })
        {
            var f = Create(omega: Double3.Zero, velocity: new(0, 0, -3));
            var r = new Double3(2, 0, 0); var n = Double3.UnitZ;
            var supportVelocity = new Double3(0, 0, .75); // Prescribed surface witness motion, already in root space.
            var beforeT = Translation(f); var beforeR = Rotation(f);
            var observation = new SpacecraftContactObservation(true, Provenance.Feature, Provenance.Support,
                f.Engine.State.Revision, f.Clock.CurrentTime, Root, beforeT.PositionRoot + r, beforeT.PositionRoot + r,
                Double3.UnitZ, n, 0, beforeT.VelocityRoot, supportVelocity, beforeT.VelocityRoot - supportVelocity);
            Check(observation.IsReady && observation.RadialSignedGapMetres == 0 && n.LengthSquared == 1 &&
                Double3.Dot(n, observation.RelativeVelocityRoot) < 0, "qualified approaching single-point fixture");
            // Exact decimal denominator for r=(2,0,0), n=Z, I_y=3, m=8: 1/8 + 4/3.
            var magnitude = (double)(-(1m + (decimal)e) * -3.75m / (1m / 8m + 4m / 3m));
            var intent = Intent(f, n * magnitude, r); Schedule(f, intent);
            Check(f.Engine.ExecuteCanonicalPendingEvent().Committed, "analytical supplied impulse accepted");
            AssertPhysical(beforeT, beforeR, Translation(f), Rotation(f), intent);
            var afterFeatureVelocity = Translation(f).VelocityRoot + Double3.Cross(MatrixRotate(Rotation(f).OrientationLocalToParent, Rotation(f).AngularVelocityBody), r);
            var afterNormal = Double3.Dot(n, afterFeatureVelocity - supportVelocity);
            Check(Math.Abs(afterNormal - e * 3.75) <= Gamma(512) * (3.75 + Math.Abs(afterNormal)), "single-point restitution oracle with prescribed moving support");
        }
    }

    internal static void Performance()
    {
        StoragePerformance();
        const int batchCount = 21, transactions = 256, warmupBatches = 4;
        for (var i = 0; i < warmupBatches; i++) ExecutePrepared(Prepare(transactions), transactions);
        var samples = new double[batchCount]; var allocationSamples = new long[batchCount];
        for (var batch = 0; batch < batchCount; batch++)
        {
            var f = Prepare(transactions); // Timeline, intent/provenance and both history allocations are outside timing.
            var started = Stopwatch.GetTimestamp(); ExecutePrepared(f, transactions); var elapsed = Stopwatch.GetTimestamp() - started;
            samples[batch] = elapsed * (1_000_000_000d / Stopwatch.Frequency) / transactions;
        }
        // Independent allocation experiment: no Stopwatch operations or reporting in the measured region.
        for (var batch = 0; batch < batchCount; batch++)
        {
            var f = Prepare(transactions); var before = GC.GetAllocatedBytesForCurrentThread();
            ExecutePrepared(f, transactions); allocationSamples[batch] = GC.GetAllocatedBytesForCurrentThread() - before;
        }
        Array.Sort(samples); Array.Sort(allocationSamples);
        Console.WriteLine($"Contact response complete EvaluateNext+ValidateAndCommit: warmed {batchCount}x{transactions}; " +
            $"median={samples[batchCount / 2]:F1} ns/transaction; P95={samples[(int)Math.Ceiling(.95 * batchCount) - 1]:F1}; " +
            $"P99={samples[(int)Math.Ceiling(.99 * batchCount) - 1]:F1}; percentiles=batch-average latency; " +
            $"allocation median={allocationSamples[batchCount / 2]} bytes/batch; max={allocationSamples[^1]} bytes/batch; " +
            "storage=one canonical event record+one coupled contact record per commit in preallocated histories; " +
            "fixture oracle excluded; no latency threshold or retry-until-pass");
    }

    private static void StoragePerformance()
    {
        const int count = 1000, samples = 21, warmup = 4;
        var intent = Intent(Create(), Double3.UnitX, Double3.UnitY);
        for (var mode = 0; mode < 3; mode++)
        {
            for (var i = 0; i < warmup; i++) StorageBatch(mode, StorageTimeline(mode, count, intent), intent, count);
            var times = new double[samples]; var allocations = new long[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = StorageTimeline(mode, count, intent);
                var start = Stopwatch.GetTimestamp(); StorageBatch(mode, t, intent, count);
                times[i] = (Stopwatch.GetTimestamp() - start) * (1_000_000_000d / Stopwatch.Frequency) / count;
            }
            for (var i = 0; i < samples; i++)
            {
                var t = StorageTimeline(mode, count, intent);
                var before = GC.GetAllocatedBytesForCurrentThread(); StorageBatch(mode, t, intent, count);
                allocations[i] = GC.GetAllocatedBytesForCurrentThread() - before;
            }
            Array.Sort(times); Array.Sort(allocations);
            Console.WriteLine($"Timeline storage performance: mode={new[] { "marker schedule/cancel", "contact schedule/cancel", "validated contact lookup" }[mode]}; " +
                $"{samples}x{count}; median={times[samples / 2]:F1} ns/op; P95={times[19]:F1}; P99={times[20]:F1}; " +
                $"allocation median={allocations[samples / 2]} max={allocations[^1]} bytes/batch; percentiles=batch averages; no GC/runtime override");
        }
    }

    private static SimulationTimeline StorageTimeline(int mode, int count, SpacecraftContactImpulseIntent intent)
    {
        var t = mode == 0 ? new SimulationTimeline(count) : new SimulationTimeline(count, 1);
        if (mode == 2 && !t.ScheduleContactImpulse(default, new(1), 0, intent).Succeeded) throw new InvalidOperationException("Lookup fixture failed.");
        return t;
    }

    private static void StorageBatch(int mode, SimulationTimeline t, SpacecraftContactImpulseIntent intent, int count)
    {
        t.TryPeekPending(out var pending);
        for (ulong id = 1; id <= (ulong)count; id++)
        {
            if (mode == 2)
            {
                if (!t.TryResolveContactImpulse(pending, out var resolved) || resolved != intent) throw new InvalidOperationException("Lookup failed.");
            }
            else
            {
                var result = mode == 0 ? t.Schedule(default, new(new(id), default, 0, SimulationEventKind.Marker)) :
                    t.ScheduleContactImpulse(default, new(id), 0, intent);
                if (!result.Succeeded || !t.Cancel(new(id)).Succeeded) throw new InvalidOperationException("Storage benchmark failed.");
            }
        }
    }
    private static void Allocations()
    {
        const int count = 256;
        // One bounded warmup and one independently measured batch. No retry or GC/JIT policy overrides.
        for (var warm = 0; warm < 4; warm++) ExecutePrepared(Prepare(count), count);
        var f = Prepare(count); var before = GC.GetAllocatedBytesForCurrentThread();
        ExecutePrepared(f, count);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"warmed complete coupled transaction allocation: expected=0 bytes, actual={allocated} bytes");
        Check(f.Engine.ProcessedCount == count && f.Engine.ProcessedContactImpulseCount == count &&
            f.Engine.State.Revision.Value == count && f.Clock.Timeline.PendingCount == 0,
            "warmed complete coupled transactions publish all records and consume all pending events");
        Console.WriteLine($"Contact response permanent allocation contract: {count} complete transactions; allocated={allocated} bytes; preallocated canonical+coupled histories");
    }
    private static Fixture Prepare(int count)
    {
        var f = Create(count, omega: Double3.Zero);
        for (var i = 0; i < count; i++) Schedule(f, Intent(f, new(.125, .25, -.125), new(1, -1, .5), revision: (ulong)i), (ulong)i + 1);
        return f;
    }
    private static void ExecutePrepared(Fixture f, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var candidate = f.Engine.EvaluateNext();
            if (!f.Engine.ValidateAndCommit(candidate).Committed) throw new InvalidOperationException("Prepared performance contact failed.");
        }
        if (f.Engine.ProcessedContactImpulseCount != count || f.Engine.State.Revision.Value != (ulong)count)
            throw new InvalidOperationException("Performance run did not commit every contact.");
    }
}
