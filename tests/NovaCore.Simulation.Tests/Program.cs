using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Celestial.Transactions;
using NovaCore.Simulation.Celestial.ReferenceFrames;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Transactions;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Guidance;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using System.Diagnostics;

var tests = new (string Name, Action Test)[]
{
    ("SimulationInstant", InstantTests),
    ("SimulationDuration", DurationTests),
    ("SimulationRate", RateTests),
    ("Event ordering", EventOrderingTests),
    ("Timeline topology", TimelineTopologyTests),
    ("Simulation clock", ClockTests),
    ("Host-duration conversion", HostDurationTests),
    ("Host-duration debt servicing", HostDurationDebtServiceTests),
    ("Transaction contracts", TransactionTests),
    ("Canonical transaction groups", CanonicalGroupTests),
    ("Clock execution orchestration", ClockExecutionTests),
    ("Celestial contracts", CelestialContractTests),
    ("Celestial system definitions", CelestialSystemDefinitionTests),
    ("Celestial body catalog", CelestialBodyCatalogTests),
    ("Celestial system time and provenance", CelestialSystemTimeAndProvenanceTests),
    ("Celestial ephemeris catalogs", CelestialEphemerisCatalogTests),
    ("Celestial system evaluation", CelestialSystemEvaluationTests),
    ("Two-body propagation", TwoBodyPropagationTests),
    ("Spacecraft attitude", SpacecraftAttitudeTests),
    ("Spacecraft attitude integration", SpacecraftAttitudeIntegrationTests),
    ("Rigid-body rotation", RigidBodyRotationTests),
    ("Rigid-body torque transaction", RigidBodyTorqueTransactionTests),
    ("Flight reference and SAS guidance", FlightReferenceAndSasTests),
    ("SAS sign/frame continuity proof", SasSignFrameContinuityProofTests),
    ("Analytical orbit sampling", AnalyticalOrbitSamplingTests),
    ("Celestial frame extraction", CelestialFrameExtractionTests),
    ("Celestial trajectory replacement", CelestialTrajectoryReplacementTests),
    ("Celestial impulse events", CelestialImpulseEventTests),
    ("Allocation", AllocationTests),
};
foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }

static void InstantTests()
{
    Check(SimulationInstant.Zero.Ticks == 0 && new SimulationInstant(-1) < SimulationInstant.Zero && new SimulationInstant(long.MinValue) < new SimulationInstant(long.MaxValue), "zero/negative/extreme instant");
    var instant = SimulationInstant.FromWholeSeconds(2) + new SimulationDuration(1);
    Check(instant.Ticks == 2_000_001 && instant - SimulationInstant.FromWholeSeconds(2) == new SimulationDuration(1), "instant arithmetic");
    Check(SimulationInstant.FromSecondsRounded(1.25).Ticks == 1_250_000 && SimulationInstant.FromWholeSeconds(-1).SecondsSinceEpoch == -1d, "seconds conversion");
    Check(new SimulationInstant(1).Ticks - new SimulationInstant(0).Ticks == 1, "microtick resolution");
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MaxValue) + new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MinValue) - new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationInstant(long.MaxValue) - new SimulationInstant(-1));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.NaN));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.PositiveInfinity));
    Throws<ArgumentOutOfRangeException>(() => SimulationInstant.FromSecondsRounded(double.NegativeInfinity));
    Throws<OverflowException>(() => SimulationInstant.FromSecondsRounded(double.MaxValue));
}

static void SpacecraftAttitudeTests()
{
    var id = new SpacecraftId(1);
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var identity) == SpacecraftAttitudeEvaluationStatus.Success, "identity state");
    var zero = SpacecraftAttitudeEvaluator.TryEvaluate(identity, SimulationInstant.Zero); Check(zero.Succeeded && zero.OrientationLocalToParent == DoubleQuaternion.Identity, "identity/zero duration");
    Check(SpacecraftAttitudeEvaluator.TryEvaluate(identity, SimulationInstant.FromWholeSeconds(123)).Succeeded && SpacecraftAttitudeEvaluator.TryEvaluate(identity, SimulationInstant.FromWholeSeconds(123)).OrientationLocalToParent == DoubleQuaternion.Identity, "zero angular velocity");
    Check(SpacecraftAttitudeEvaluator.TryCanonicalize(new DoubleQuaternion(0, 0, 0, -1), out var negativeIdentity) == SpacecraftAttitudeEvaluationStatus.Success && negativeIdentity == DoubleQuaternion.Identity, "q/-q canonicalization");
    Check(SpacecraftAttitudeEvaluator.TryCanonicalize(new DoubleQuaternion(-1, 0, 0, 0), out var tie) == SpacecraftAttitudeEvaluationStatus.Success && tie.X > 0d, "zero-W tie break");
    Check(SpacecraftAttitudeEvaluator.TryCanonicalize(default, out _) == SpacecraftAttitudeEvaluationStatus.NearZeroOrientation, "near-zero rejection");
    Check(SpacecraftAttitudeEvaluator.TryCanonicalize(new DoubleQuaternion(double.NaN, 0, 0, 1), out _) == SpacecraftAttitudeEvaluationStatus.NonFiniteOrientation, "non-finite orientation rejection");
    Check(SpacecraftAttitudeState.TryCreate(SpacecraftId.Invalid, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out _) == SpacecraftAttitudeEvaluationStatus.InvalidSpacecraftId, "invalid spacecraft rejection");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(double.NaN, 0, 0), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out _) == SpacecraftAttitudeEvaluationStatus.NonFiniteAngularVelocity, "non-finite omega rejection");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, (SpacecraftAttitudeModel)99, out _) == SpacecraftAttitudeEvaluationStatus.UnsupportedModel, "model rejection");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(0, 0, Math.PI), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var zSpin) == SpacecraftAttitudeEvaluationStatus.Success, "z spin state");
    var half = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, SimulationInstant.FromSecondsRounded(.5d)); Check(half.Succeeded && Math.Abs(SpacecraftAttitudeEvaluator.Forward(half.OrientationLocalToParent).Y - 1d) < 1e-12d, "body Z rotation");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(Math.PI, 0, 0), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var xSpin) == SpacecraftAttitudeEvaluationStatus.Success, "x spin state");
    var xHalf = SpacecraftAttitudeEvaluator.TryEvaluate(xSpin, SimulationInstant.FromSecondsRounded(.5d)); Check(Math.Abs(SpacecraftAttitudeEvaluator.Right(xHalf.OrientationLocalToParent).Z - 1d) < 1e-12d, "body X rotation");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(0, Math.PI, 0), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var ySpin) == SpacecraftAttitudeEvaluationStatus.Success, "y spin state");
    var yHalf = SpacecraftAttitudeEvaluator.TryEvaluate(ySpin, SimulationInstant.FromSecondsRounded(.5d)); Check(Math.Abs(SpacecraftAttitudeEvaluator.Forward(yHalf.OrientationLocalToParent).Z + 1d) < 1e-12d, "body Y rotation");
    var forward = SpacecraftAttitudeEvaluator.Forward(DoubleQuaternion.Identity); var right = SpacecraftAttitudeEvaluator.Right(DoubleQuaternion.Identity); var down = SpacecraftAttitudeEvaluator.Down(DoubleQuaternion.Identity); Check(forward == Double3.UnitX && right == Double3.UnitY && down == Double3.UnitZ && SpacecraftAttitudeEvaluator.Up(DoubleQuaternion.Identity) == -Double3.UnitZ, "body axes");
    var xQuarter = DoubleQuaternion.FromAxisAngle(Double3.UnitX, Math.PI / 2d); var yQuarter = DoubleQuaternion.FromAxisAngle(Double3.UnitY, Math.PI / 2d); var composed = yQuarter * xQuarter; var local = new Double3(.25d, -.5d, .75d); CheckVectorNear(composed.Rotate(local), yQuarter.Rotate(xQuarter.Rotate(local)), 1e-12d, "Hamilton composition applies RHS first");
    var future = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, SimulationInstant.FromWholeSeconds(10)); var backward = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, SimulationInstant.Zero); Check(future.Succeeded && backward.Succeeded && backward.OrientationLocalToParent == DoubleQuaternion.Identity, "exact-time repeatability");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.FromWholeSeconds(10), future.OrientationLocalToParent, new Double3(0, 0, -Math.PI), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var reverse) == SpacecraftAttitudeEvaluationStatus.Success && Math.Abs(SpacecraftAttitudeEvaluator.TryEvaluate(reverse, SimulationInstant.Zero).OrientationLocalToParent.W - 1d) < 1e-12d, "forward/backward round trip");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(1e-12d, 0d, 0d), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var smallSpin) == SpacecraftAttitudeEvaluationStatus.Success && SpacecraftAttitudeEvaluator.TryEvaluate(smallSpin, SimulationInstant.FromWholeSeconds(1)).Succeeded, "small-angle evaluation");
    var longDuration = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, new SimulationInstant(SpacecraftAttitudeEvaluator.MaximumEvaluationTicks)); Check(longDuration.Succeeded && Math.Abs(longDuration.OrientationLocalToParent.LengthSquared - 1d) < 1e-12d, "long-duration normalization stability");
    var overflowingEpoch = new SpacecraftAttitudeState(id, new SimulationInstant(long.MinValue), DoubleQuaternion.Identity, Double3.Zero, SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1); Check(SpacecraftAttitudeEvaluator.TryEvaluate(overflowingEpoch, new SimulationInstant(long.MaxValue)).Status == SpacecraftAttitudeEvaluationStatus.DurationOverflow, "duration subtraction overflow");
    Check(SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, new SimulationInstant(SpacecraftAttitudeEvaluator.MaximumEvaluationTicks + 1)).Status == SpacecraftAttitudeEvaluationStatus.EvaluationSpanExceeded, "duration bound");
    _ = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, SimulationInstant.FromWholeSeconds(1)); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++)
    {
        var evaluated = SpacecraftAttitudeEvaluator.TryEvaluate(zSpin, new SimulationInstant(index)); Check(evaluated.Succeeded, "warm attitude");
        var orientation = evaluated.OrientationLocalToParent; var basis = SpacecraftAttitudeEvaluator.Forward(orientation);
        hash = Mix(hash, (ulong)evaluated.RequestedTime.Ticks); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(orientation.W)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(evaluated.AngularVelocityBody.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(basis.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(basis.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(basis.Z));
    }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm attitude allocation"); Console.WriteLine($"Deterministic spacecraft-attitude hash: 0x{hash:X16}; allocation=0 bytes");
}

static void SpacecraftAttitudeIntegrationTests()
{
    var id = new SpacecraftId(44); var carrier = new ReferenceFrameId(2); var body = new ReferenceFrameId(3);
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(0d, 0d, Math.PI), SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var initial) == SpacecraftAttitudeEvaluationStatus.Success, "spacecraft initial attitude");
    var definitions = new[] { new SpacecraftDefinition(id, carrier, body, "test-spacecraft") };
    Check(SpacecraftStateStore.TryCreate(definitions, new[] { initial }, out var store, out var storeStatus) && store is not null && storeStatus == SpacecraftStateStoreStatus.Success, "spacecraft store construction");
    var view = store!.CreateView(); Check(view.Count == 1 && view.TryGetDefinition(id, out var definition) && definition.BodyFrame == body && view.TryGetAttitude(id, out var stored) && stored == initial, "spacecraft lookup/declaration order");
    Check(!SpacecraftStateStore.TryCreate([definitions[0], definitions[0]], [initial, initial], out _, out var duplicateStatus) && duplicateStatus == SpacecraftStateStoreStatus.DuplicateSpacecraftId, "duplicate spacecraft rejection");
    var graphBuilder = new ReferenceFrameGraphBuilder(); graphBuilder.Add(new ReferenceFrameNode(new ReferenceFrameId(1), null, ReferenceFrameKind.Ecl, "root")); graphBuilder.Add(new ReferenceFrameNode(carrier, new ReferenceFrameId(1), ReferenceFrameKind.Cce, "carrier")); graphBuilder.Add(new ReferenceFrameNode(body, carrier, ReferenceFrameKind.Ccf, "body")); var graph = graphBuilder.Build();
    var evaluations = new ReferenceFrameEvaluation[3]; var frameStatus = SpacecraftReferenceFrameEvaluator.TryEvaluate(view, graph, SimulationInstant.FromSecondsRounded(.5d), evaluations); Check(frameStatus == SpacecraftReferenceFrameEvaluationStatus.Success, "spacecraft body-frame extraction");
    var bodyEvaluation = evaluations[2].Value; Check(bodyEvaluation.LocalToParent.Translation == Double3.Zero && bodyEvaluation.OriginVelocityInParent == Double3.Zero && Math.Abs(bodyEvaluation.AngularVelocityInParent.Z - Math.PI) < 1e-12d && Math.Abs(SpacecraftAttitudeEvaluator.Forward(bodyEvaluation.LocalToParent.Rotation).Y - 1d) < 1e-12d, "body frame attitude and parent angular velocity");
    var clock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.One); var engine = new SimulationTransactionEngine(clock, new SimulationState(null, store), initialHistoryCapacity: 1);
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.FromAxisAngle(Double3.UnitX, .25d), initial.AngularVelocityBody, initial.Model, out var replacement) == SpacecraftAttitudeEvaluationStatus.Success, "replacement state");
    var created = SpacecraftAttitudeTransactionEvaluator.TryCreateReplacement(engine.State, clock.CurrentTime, id, replacement); Check(created.Succeeded && created.Transaction is not null, "pure attitude replacement candidate"); var transaction = created.Transaction!.Value;
    var committed = engine.ValidateAndCommit(transaction); Check(committed.Committed && engine.ProcessedSpacecraftAttitudeCount == 1 && engine.State.Revision == new StateRevision(1) && engine.State.Spacecraft.TryGetAttitude(id, out var current) && current == replacement, "direct attitude commit and history");
    Check(engine.ValidateAndCommit(transaction).Status == SpacecraftAttitudeTransactionStatus.StateRevisionMismatch, "stale attitude candidate rejection");
    var noOp = SpacecraftAttitudeTransactionEvaluator.TryCreateReplacement(engine.State, clock.CurrentTime, id, replacement); Check(noOp.Status == SpacecraftAttitudeTransactionStatus.ReplacementNoOp, "attitude no-op rejection");
    var mismatch = new SpacecraftAttitudeReplacementTransaction(SimulationInstant.FromWholeSeconds(1), engine.State.Revision, id, replacement, initial); Check(engine.ValidateAndCommit(mismatch).Status == SpacecraftAttitudeTransactionStatus.TimeMismatch, "attitude time mismatch rejection");
    _ = view.TryGetAttitude(id, out _); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { Check(view.TryGetAttitude(id, out var warm), "warm spacecraft lookup"); var evaluated = SpacecraftAttitudeEvaluator.TryEvaluate(warm, new SimulationInstant(index)); Check(evaluated.Succeeded && SpacecraftReferenceFrameEvaluator.TryEvaluate(view, graph, new SimulationInstant(index), evaluations) == SpacecraftReferenceFrameEvaluationStatus.Success, "warm spacecraft evaluation/extraction"); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(evaluated.OrientationLocalToParent.W)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm spacecraft store/evaluation/frame extraction allocation"); Console.WriteLine($"Deterministic spacecraft-attitude integration hash: 0x{hash:X16}; allocation=0 bytes");
}

static void RigidBodyRotationTests()
{
    var id = new SpacecraftId(81); var spherical = new PrincipalMomentsOfInertia(2d, 2d, 2d);
    Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(0d, 0d, Math.PI), spherical, Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1, out var zeroTorque) == SpacecraftRigidBodyRotationEvaluationStatus.Success, "rigid-body state");
    var zero = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(zeroTorque, SimulationInstant.Zero); Check(zero.Succeeded && zero.SubstepCount == 0 && zero.OrientationLocalToParent == DoubleQuaternion.Identity, "rigid zero duration");
    Check(SpacecraftAttitudeState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, zeroTorque.AngularVelocityBody, SpacecraftAttitudeModel.ConstantBodyAngularVelocityV1, out var kinematic) == SpacecraftAttitudeEvaluationStatus.Success, "kinematic comparison state");
    var oneSecond = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(zeroTorque, SimulationInstant.FromWholeSeconds(1)); var kinematicOneSecond = SpacecraftAttitudeEvaluator.TryEvaluate(kinematic, SimulationInstant.FromWholeSeconds(1)); var sphericalOrientationError = Math.Abs(oneSecond.OrientationLocalToParent.W - kinematicOneSecond.OrientationLocalToParent.W); Check(oneSecond.Succeeded && sphericalOrientationError < 1e-8d && oneSecond.AngularVelocityBody == zeroTorque.AngularVelocityBody, "spherical zero torque matches 8A");
    var xTorque = CreateRigid(id, spherical, new Double3(4d, 0d, 0d), Double3.Zero); var yTorque = CreateRigid(id, spherical, new Double3(0d, 4d, 0d), Double3.Zero); var zTorque = CreateRigid(id, spherical, new Double3(0d, 0d, 4d), Double3.Zero);
    Check(Math.Abs(SpacecraftRigidBodyRotationEvaluator.TryEvaluate(xTorque, SimulationInstant.FromWholeSeconds(1)).AngularVelocityBody.X - 2d) < 1e-12d, "principal X torque"); Check(Math.Abs(SpacecraftRigidBodyRotationEvaluator.TryEvaluate(yTorque, SimulationInstant.FromWholeSeconds(1)).AngularVelocityBody.Y - 2d) < 1e-12d, "principal Y torque"); Check(Math.Abs(SpacecraftRigidBodyRotationEvaluator.TryEvaluate(zTorque, SimulationInstant.FromWholeSeconds(1)).AngularVelocityBody.Z - 2d) < 1e-12d, "principal Z torque");
    var asymmetric = new PrincipalMomentsOfInertia(2d, 3d, 5d); var coupled = CreateRigid(id, asymmetric, new Double3(.5d, -.75d, 1.25d), new Double3(.2d, -.3d, .4d)); var coupledResult = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(coupled, SimulationInstant.FromWholeSeconds(1)); Check(coupledResult.Succeeded && coupledResult.AngularVelocityBody != coupled.AngularVelocityBody, "asymmetric gyroscopic coupling");
    var free = CreateRigid(id, asymmetric, Double3.Zero, new Double3(.5d, -.75d, 1.25d)); var h0 = SpacecraftRigidBodyRotationEvaluator.AngularMomentum(asymmetric, free.AngularVelocityBody); var e0 = SpacecraftRigidBodyRotationEvaluator.RotationalEnergy(asymmetric, free.AngularVelocityBody); var freeResult = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(free, SimulationInstant.FromWholeSeconds(10)); var h1 = SpacecraftRigidBodyRotationEvaluator.AngularMomentum(asymmetric, freeResult.AngularVelocityBody); var e1 = SpacecraftRigidBodyRotationEvaluator.RotationalEnergy(asymmetric, freeResult.AngularVelocityBody); var momentumError = Math.Abs(Math.Sqrt(h1.LengthSquared) - Math.Sqrt(h0.LengthSquared)); var energyError = Math.Abs(e1-e0); Check(momentumError < 1e-9d && energyError < 1e-9d && Math.Abs(freeResult.OrientationLocalToParent.LengthSquared - 1d) < 1e-12d, "torque-free invariants and quaternion norm");
    var forward = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(coupled, SimulationInstant.FromWholeSeconds(1)); Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.FromWholeSeconds(1), forward.OrientationLocalToParent, forward.AngularVelocityBody, asymmetric, coupled.ConstantBodyTorque, coupled.Model, out var reverseState) == SpacecraftRigidBodyRotationEvaluationStatus.Success, "reverse state"); var backward = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(reverseState, SimulationInstant.Zero); Check(backward.Succeeded && Math.Abs(backward.AngularVelocityBody.X-coupled.AngularVelocityBody.X) < 1e-8d, "forward/backward bounded evaluation");
    var remainder = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(xTorque, new SimulationInstant(SpacecraftRigidBodyRotationEvaluator.FullSubstepTicks + 1)); Check(remainder.Succeeded && remainder.SubstepCount == 2, "integer full and remainder steps"); Check(SpacecraftRigidBodyRotationEvaluator.TryEvaluate(xTorque, new SimulationInstant((long)SpacecraftRigidBodyRotationEvaluator.MaximumSubstepCount * SpacecraftRigidBodyRotationEvaluator.FullSubstepTicks + 1)).Status == SpacecraftRigidBodyRotationEvaluationStatus.ExcessiveStepCount, "maximum step boundary");
    Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, new PrincipalMomentsOfInertia(0d, 1d, 1d), Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1, out _) == SpacecraftRigidBodyRotationEvaluationStatus.NonPositiveInertia, "invalid inertia"); Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, spherical, new Double3(double.NaN, 0d, 0d), RigidBodyRotationModel.ConstantBodyTorqueV1, out _) == SpacecraftRigidBodyRotationEvaluationStatus.NonFiniteTorque, "invalid torque"); Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, Double3.Zero, spherical, Double3.Zero, (RigidBodyRotationModel)99, out _) == SpacecraftRigidBodyRotationEvaluationStatus.UnsupportedModel, "unsupported rigid model");
    _ = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(coupled, new SimulationInstant(1)); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { var result = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(coupled, new SimulationInstant(index % 10)); Check(result.Succeeded, "warm rigid evaluation"); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.OrientationLocalToParent.W)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.AngularVelocityBody.X)); hash = Mix(hash, (ulong)result.RequestedTime.Ticks); hash = Mix(hash, (uint)result.SubstepCount); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm rigid evaluation allocation"); Console.WriteLine($"Rigid-body rotation: spherical orientation error={sphericalOrientationError:E3}; momentum error={momentumError:E3}; energy error={energyError:E3}; hash=0x{hash:X16}; allocation=0 bytes");
}

static SpacecraftRigidBodyRotationState CreateRigid(SpacecraftId id, PrincipalMomentsOfInertia inertia, Double3 torque, Double3 angularVelocity)
{
    Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, angularVelocity, inertia, torque, RigidBodyRotationModel.ConstantBodyTorqueV1, out var state) == SpacecraftRigidBodyRotationEvaluationStatus.Success, "create rigid test state"); return state;
}

static void FlightReferenceAndSasTests()
{
    var r = new Double3(3d, 0d, 0d); var v = new Double3(0d, 4d, 0d);
    Check(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.Prograde).DirectionCarrierParent == Double3.UnitY, "prograde");
    Check(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.Retrograde).DirectionCarrierParent == -Double3.UnitY, "retrograde");
    Check(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.Normal).DirectionCarrierParent == Double3.UnitZ, "normal");
    Check(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.AntiNormal).DirectionCarrierParent == -Double3.UnitZ, "anti-normal");
    Check(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.RadialOut).DirectionCarrierParent == Double3.UnitX && FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.RadialIn).DirectionCarrierParent == -Double3.UnitX, "radial directions");
    Check(FlightReferenceEvaluator.TryEvaluate(Double3.Zero, v, DoubleQuaternion.Identity, FlightReferenceMode.RadialOut).Status == FlightReferenceEvaluationStatus.NearZeroRadius && FlightReferenceEvaluator.TryEvaluate(r, Double3.Zero, DoubleQuaternion.Identity, FlightReferenceMode.Prograde).Status == FlightReferenceEvaluationStatus.NearZeroVelocity, "degenerate vectors");
    Check(SpacecraftSasTargetOrientation.TryCreate(Double3.UnitY, -Double3.UnitZ, out var target) == SpacecraftSasControlStatus.Success && Double3.Dot(target.Rotate(Double3.UnitX), Double3.UnitY) > 1d - 1e-12d, "target forward basis");
    var config = new SpacecraftSasControllerConfiguration(new Double3(10, 10, 10), new Double3(2, 2, 2), new Double3(1, 1, 1), 1e-6, 1e-6, 1e-5, 1e-5); var inertia = new PrincipalMomentsOfInertia(1, 1, 1);
    var zero = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, DoubleQuaternion.Identity, inertia, config); Check(zero.Status == SpacecraftSasControlStatus.Settled && zero.RequestedBodyTorque == Double3.Zero, "zero settled");
    var damping = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, new Double3(.5, 0, 0), DoubleQuaternion.Identity, inertia, config); Check(damping.RequestedBodyTorque.X < 0d, "rate damping");
    var correction = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, DoubleQuaternion.FromAxisAngle(Double3.UnitX, Math.PI), inertia, config); Check(correction.RequestedBodyTorque.X == 1d, "shortest path clamp");
    Check(SpacecraftSasTargetOrientation.CaptureHold(new DoubleQuaternion(0, 0, 0, -1)) == DoubleQuaternion.Identity, "canonical hold capture");
    _ = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, target, inertia, config); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { var reference = FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, FlightReferenceMode.Prograde); var t = SpacecraftSasTargetOrientation.TryCreate(reference.DirectionCarrierParent, -Double3.UnitZ, out var q); var result = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, q, inertia, config); Check(t == SpacecraftSasControlStatus.Success && result.Succeeded, "warm guidance"); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.RequestedBodyTorque.X)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm guidance allocation"); Console.WriteLine($"Flight-reference/SAS guidance hash: 0x{hash:X16}; allocation=0 bytes");
}

static void SasSignFrameContinuityProofTests()
{
    var inertia = new PrincipalMomentsOfInertia(1d, 1d, 1d);
    var correction = new SpacecraftSasControllerConfiguration(new Double3(10d, 10d, 10d), Double3.Zero, new Double3(100d, 100d, 100d), 0d, 0d, .001d, .001d);
    var damping = new SpacecraftSasControllerConfiguration(new Double3(10d, 10d, 10d), new Double3(5d, 5d, 5d), new Double3(100d, 100d, 100d), 0d, 0d, .001d, .001d);
    ulong hash = 14695981039346656037UL;
    foreach (var axis in new[] { Double3.UnitX, Double3.UnitY, Double3.UnitZ })
    foreach (var angle in new[] { -.15d, .15d, -Math.PI / 2d, Math.PI / 2d })
    {
        var target = DoubleQuaternion.FromAxisAngle(axis, angle);
        var result = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, target, inertia, correction);
        var after = ApplyShortStep(DoubleQuaternion.Identity, Double3.Zero, result.RequestedBodyTorque, inertia);
        var beforeError = QuaternionAngle(DoubleQuaternion.Identity, target); var afterError = QuaternionAngle(after.OrientationLocalToParent, target);
        Check(result.Succeeded && afterError < beforeError, $"{axis} {angle:R} controller torque reduces orientation error");
        hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(afterError)); hash = MixDouble3(hash, result.RequestedBodyTorque);
    }

    var r = new Double3(3d, 0d, 0d); var v = new Double3(0d, 4d, 0d);
    VerifyFullPipeline(FlightReferenceMode.Prograde, Double3.UnitY, DoubleQuaternion.Identity);
    VerifyFullPipeline(FlightReferenceMode.Normal, Double3.UnitZ, DoubleQuaternion.Identity);
    VerifyFullPipeline(FlightReferenceMode.RadialOut, Double3.UnitX, DoubleQuaternion.FromAxisAngle(Double3.UnitZ, Math.PI / 2d));

    var equivalent = DoubleQuaternion.FromAxisAngle(Double3.UnitY, Math.PI / 2d); var negativeEquivalent = new DoubleQuaternion(-equivalent.X, -equivalent.Y, -equivalent.Z, -equivalent.W);
    var positiveResult = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, equivalent, inertia, correction); var negativeResult = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, negativeEquivalent, inertia, correction);
    Check(positiveResult.AttitudeErrorBody == negativeResult.AttitudeErrorBody && positiveResult.RequestedBodyTorque == negativeResult.RequestedBodyTorque, "q/-q shortest-path error and torque equivalence");
    Check(QuaternionAngle(ApplyShortStep(DoubleQuaternion.Identity, Double3.Zero, positiveResult.RequestedBodyTorque, inertia).OrientationLocalToParent, equivalent) < Math.PI / 2d, "q/-q short step follows the short path");
    var near180 = DoubleQuaternion.FromAxisAngle(Double3.UnitY, Math.PI - 1e-9d); var near180Result = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, near180, inertia, correction); Check(near180Result.RequestedBodyTorque.Y > 0d && near180Result.AttitudeErrorBody.Y > 0d, "near-180 tie has deterministic positive-axis correction");

    var hold = SpacecraftSasTargetOrientation.CaptureHold(DoubleQuaternion.Identity); var holdResult = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, new Double3(.2d, 0d, 0d), hold, inertia, damping); var damped = ApplyShortStep(DoubleQuaternion.Identity, new Double3(.2d, 0d, 0d), holdResult.RequestedBodyTorque, inertia);
    Check(hold == SpacecraftSasTargetOrientation.CaptureHold(hold) && holdResult.RequestedBodyTorque.X < 0d && Math.Abs(damped.AngularVelocityBody.X) < .2d, "hold is stable and damps angular velocity");
    Check(SpacecraftSasController.TryEvaluate(hold, Double3.Zero, hold, inertia, damping).RequestedBodyTorque == Double3.Zero, "hold zero error and rate requests zero torque");

    var progradeJump = MeasurePlanarPath(false, out var progradeFallbacks); var radialJump = MeasurePlanarPath(true, out var radialFallbacks); var normal = CreateTarget(Double3.UnitZ); var antiNormal = CreateTarget(-Double3.UnitZ);
    Check(progradeJump < .03d && radialJump < .03d && progradeFallbacks == 0 && radialFallbacks == 0, "planar prograde/radial target bases remain continuous without fallback");
    Check(Double3.Dot(normal.Rotate(Double3.UnitX), Double3.UnitZ) > 1d - 1e-12d && Double3.Dot(antiNormal.Rotate(Double3.UnitX), -Double3.UnitZ) > 1d - 1e-12d, "normal and anti-normal target bases are correct");
    var parallelJump = MeasurePreferredUpParallelPath(out var fallbackTransitions);
    Check(parallelJump <= Math.PI / 2d + 1e-6d && fallbackTransitions == 2, "preferred-up singularity has deterministic bounded fallback transitions");

    VerifyModeSwitch(FlightReferenceMode.Prograde, FlightReferenceMode.Normal); VerifyModeSwitch(FlightReferenceMode.Normal, FlightReferenceMode.RadialOut); VerifyModeSwitch(FlightReferenceMode.RadialOut, FlightReferenceMode.Retrograde);
    _ = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, equivalent, inertia, correction); var before = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) { var result = SpacecraftSasController.TryEvaluate(DoubleQuaternion.Identity, Double3.Zero, equivalent, inertia, correction); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(result.RequestedBodyTorque.Y)); } Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm SAS proof evaluation allocation");
    Console.WriteLine($"SAS sign/frame proof: one-step 90-degree error={QuaternionAngle(ApplyShortStep(DoubleQuaternion.Identity, Double3.Zero, positiveResult.RequestedBodyTorque, inertia).OrientationLocalToParent, equivalent):E6} rad; continuity prograde={progradeJump:E6}, radial={radialJump:E6}, preferred-up={parallelJump:E6} rad; fallback transitions={fallbackTransitions}; hash=0x{hash:X16}; allocation=0 bytes");

    void VerifyFullPipeline(FlightReferenceMode mode, in Double3 expected, in DoubleQuaternion current)
    {
        var reference = FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, mode); var target = CreateTarget(reference.DirectionCarrierParent); var result = SpacecraftSasController.TryEvaluate(current, Double3.Zero, target, inertia, correction); var after = ApplyShortStep(current, Double3.Zero, result.RequestedBodyTorque, inertia);
        Check(reference.Succeeded && Double3.Dot(target.Rotate(Double3.UnitX), expected) > 1d - 1e-12d, $"{mode} target maps body +X into carrier target");
        Check(QuaternionAngle(after.OrientationLocalToParent, target) < QuaternionAngle(current, target), $"{mode} full pipeline correction reduces error"); hash = MixDouble3(hash, result.RequestedBodyTorque);
    }

    void VerifyModeSwitch(FlightReferenceMode from, FlightReferenceMode to)
    {
        var first = CreateTarget(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, from).DirectionCarrierParent); var second = CreateTarget(FlightReferenceEvaluator.TryEvaluate(r, v, DoubleQuaternion.Identity, to).DirectionCarrierParent); var result = SpacecraftSasController.TryEvaluate(first, Double3.Zero, second, inertia, correction); var after = ApplyShortStep(first, Double3.Zero, result.RequestedBodyTorque, inertia);
        Check(QuaternionAngle(after.OrientationLocalToParent, second) < QuaternionAngle(first, second), $"{from}->{to} begins shortest corrective turn without unrelated roll flip"); hash = MixDouble3(hash, result.RequestedBodyTorque);
    }
}

static DoubleQuaternion CreateTarget(in Double3 direction) { Check(SpacecraftSasTargetOrientation.TryCreate(direction, Double3.UnitZ, out var target) == SpacecraftSasControlStatus.Success, "target orientation construction"); return target; }
static SpacecraftRigidBodyRotationEvaluationResult ApplyShortStep(in DoubleQuaternion orientation, in Double3 angularVelocity, in Double3 torque, in PrincipalMomentsOfInertia inertia)
{
    var id = new SpacecraftId(777); Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, orientation, angularVelocity, inertia, torque, RigidBodyRotationModel.ConstantBodyTorqueV1, out var state) == SpacecraftRigidBodyRotationEvaluationStatus.Success, "short correction state"); var result = SpacecraftRigidBodyRotationEvaluator.TryEvaluate(state, new SimulationInstant(50_000)); Check(result.Succeeded, "short correction evaluation"); return result;
}
static double QuaternionAngle(in DoubleQuaternion current, in DoubleQuaternion target)
{
    var error = current.Conjugate() * target; if (error.W < 0d) error = new(-error.X, -error.Y, -error.Z, -error.W); return 2d * Math.Atan2(Math.Sqrt(error.X * error.X + error.Y * error.Y + error.Z * error.Z), error.W);
}
static double MeasurePlanarPath(bool radial, out int fallbackTransitions)
{
    fallbackTransitions = 0; var previous = default(DoubleQuaternion); var maximum = 0d;
    for (var index = 0; index <= 256; index++) { var angle = Math.Tau * index / 256d; var direction = radial ? new Double3(Math.Cos(angle), Math.Sin(angle), 0d) : new Double3(-Math.Sin(angle), Math.Cos(angle), 0d); var current = CreateTarget(direction); if (index != 0) maximum = Math.Max(maximum, QuaternionAngle(previous, current)); previous = current; }
    return maximum;
}
static double MeasurePreferredUpParallelPath(out int fallbackTransitions)
{
    fallbackTransitions = 0; var previous = default(DoubleQuaternion); var maximum = 0d; var priorUsedFallback = false;
    for (var index = -16; index <= 16; index++) { var theta = index * 1e-7d; var direction = new Double3(Math.Sin(theta), 0d, Math.Cos(theta)); var projection = Double3.UnitZ - direction * Double3.Dot(Double3.UnitZ, direction); var usesFallback = projection.LengthSquared <= SpacecraftSasTargetOrientation.BasisMinimumSquared; var current = CreateTarget(direction); if (index != -16) { maximum = Math.Max(maximum, QuaternionAngle(previous, current)); if (usesFallback != priorUsedFallback) fallbackTransitions++; } previous = current; priorUsedFallback = usesFallback; }
    return maximum;
}

static void RigidBodyTorqueTransactionTests()
{
    var id = new SpacecraftId(91); var carrier = new ReferenceFrameId(2); var body = new ReferenceFrameId(3);
    var inertia = new PrincipalMomentsOfInertia(10d, 15d, 20d);
    Check(SpacecraftRigidBodyRotationState.TryCreate(id, SimulationInstant.Zero, DoubleQuaternion.Identity, new Double3(.1d, .2d, .3d), inertia, new Double3(2d, -1d, .5d), RigidBodyRotationModel.ConstantBodyTorqueV1, out var initial) == SpacecraftRigidBodyRotationEvaluationStatus.Success, "torque transaction source");
    var definitions = new[] { new SpacecraftDefinition(id, carrier, body, "rigid-transaction") };
    Check(SpacecraftStateStore.TryCreateRigidBody(definitions, new[] { initial }, out var store, out var storeStatus) && store is not null && storeStatus == SpacecraftStateStoreStatus.Success, "rigid store setup");
    var eventTime = SimulationInstant.FromWholeSeconds(1);
    var timeline = new SimulationTimeline(1); Check(SimulationEventRequest.TryCreateRigidBodyTorque(new SimulationEventId(901), eventTime, 0, id, out var request) && timeline.Schedule(SimulationInstant.Zero, request).Succeeded, "torque event schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline); var engine = new SimulationTransactionEngine(clock, new SimulationState(null, store), 1);
    var candidate = RigidBodyTorqueTransactionEvaluator.TryCreateReplacement(engine.State, eventTime, id); Check(candidate.Succeeded && candidate.Transaction is not null, "pure exact torque candidate"); var proposed = candidate.Transaction!.Value; Check(proposed.ReplacementRotation.Epoch == eventTime && proposed.ReplacementRotation.ConstantBodyTorque == Double3.Zero, "replacement epoch and torque-free continuation");
    Check(clock.AdvanceTo(eventTime).ReachedBoundary, "torque boundary reached"); var result = engine.ExecuteCanonicalPendingEvent();
    Check(result.Committed && engine.ProcessedRigidBodyTorqueCount == 1 && engine.State.Revision == new StateRevision(1) && engine.State.Spacecraft.TryGetRigidBody(id, out _), "atomic rigid commit"); var committed = engine.State.Spacecraft.TryGetRigidBody(id, out var currentRotation) ? currentRotation : default; Check(committed == proposed.ReplacementRotation, "committed rigid replacement");
    Check(committed.AngularVelocityBody.X > initial.AngularVelocityBody.X && !engine.EvaluateNext().IsInternallyConsistent, "torque direction and event consumed");
    var graphBuilder = new ReferenceFrameGraphBuilder(); graphBuilder.Add(new ReferenceFrameNode(new ReferenceFrameId(1), null, ReferenceFrameKind.Ecl, "root")); graphBuilder.Add(new ReferenceFrameNode(carrier, new ReferenceFrameId(1), ReferenceFrameKind.Cce, "carrier")); graphBuilder.Add(new ReferenceFrameNode(body, carrier, ReferenceFrameKind.Ccf, "body")); var graph = graphBuilder.Build(); var evaluations = new ReferenceFrameEvaluation[3];
    Check(SpacecraftReferenceFrameEvaluator.TryEvaluate(engine.State.Spacecraft, graph, eventTime, evaluations) == SpacecraftReferenceFrameEvaluationStatus.Success && evaluations[2].Value.LocalToParent.Rotation == committed.OrientationLocalToParent, "rigid source frame extraction");
    var stale = new RigidBodyTorqueReplacementTransaction(eventTime, StateRevision.Zero, id, initial, proposed.ReplacementRotation); Check(RigidBodyTorqueTransactionEvaluator.Validate(engine.State, eventTime, id, stale.ExpectedRotation, stale.ReplacementRotation, true) == RigidBodyTorqueTransactionStatus.RotationBasisMismatch, "stale rigid candidate rejection");
    _ = RigidBodyTorqueTransactionEvaluator.TryCreateReplacement(engine.State, eventTime, id); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { Check(engine.State.Spacecraft.TryGetRigidBody(id, out var warm), "warm rigid lookup"); hash = Mix(hash, RigidBodyTorqueTransactionEvaluator.ComputeHash(warm)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm rigid transaction lookup allocation"); Console.WriteLine($"Rigid-body torque transaction hash: 0x{hash:X16}; allocation=0 bytes");
}

static void AnalyticalOrbitSamplingTests()
{
    const double mu = 1_000d;
    var state = new CartesianState(new Double3(100d, 0d, 0d), new Double3(0d, Math.Sqrt(mu / 100d), 0d));
    var view = CreatePropagationView(mu, state);
    var trajectory = new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, state, TwoBodyPropagationModel.CartesianTwoBodyV1);
    Span<Double3> samples = stackalloc Double3[AnalyticalOrbitSampler.VertexCount];
    var result = AnalyticalOrbitSampler.TrySample(trajectory, view, samples);
    Check(result.Status == AnalyticalOrbitSamplingStatus.Success && result.VertexCount == 257 && samples[0] == samples[^1], "fixed exact closed circular sampling");
    var repeated = AnalyticalOrbitSampler.TrySample(trajectory, view, samples); Check(repeated == result, "deterministic sampler result");
    Check(AnalyticalOrbitSampler.TrySample(trajectory, view, samples[..256]).Status == AnalyticalOrbitSamplingStatus.DestinationTooSmall, "sampler capacity rejection");
    Check(AnalyticalOrbitSampler.TrySample(trajectory with { Model = 0 }, view, samples).Status == AnalyticalOrbitSamplingStatus.UnsupportedModel, "unsupported sampler model");
    _ = AnalyticalOrbitSampler.TrySample(trajectory, view, samples); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 10_000; index++) { var warm = AnalyticalOrbitSampler.TrySample(trajectory, view, samples); Check(warm.Status == AnalyticalOrbitSamplingStatus.Success, "warm sampling"); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(samples[64].X)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm orbit sampling allocation"); Console.WriteLine($"Analytical orbit sampler: vertices=257; allocation=0 bytes; hash=0x{hash:X16}");
}

static void DurationTests()
{
    Check(SimulationDuration.Zero.IsZero && new SimulationDuration(-1).IsNegative, "duration signs");
    Check(SimulationDuration.FromWholeSeconds(-2).TotalSeconds == -2d, "duration seconds");
    Check(new SimulationDuration(-5).Abs() == new SimulationDuration(5), "duration absolute value");
    Check(new SimulationDuration(-2) < new SimulationDuration(0) && new SimulationDuration(2) > new SimulationDuration(0), "duration comparison");
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MaxValue) + new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MinValue) - new SimulationDuration(1));
    Throws<OverflowException>(() => _ = new SimulationDuration(long.MinValue).Abs());
}

static void RateTests()
{
    Check(new SimulationRate(2, 4) == SimulationRate.Half, "rate normalization");
    Check(SimulationRate.Quarter == new SimulationRate(1, 4) && SimulationRate.Half == new SimulationRate(1, 2) && SimulationRate.One == new SimulationRate(1, 1) && SimulationRate.Two == new SimulationRate(2, 1) && SimulationRate.Five == new SimulationRate(5, 1) && SimulationRate.Ten == new SimulationRate(10, 1) && SimulationRate.Hundred == new SimulationRate(100, 1), "rate presets");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationRate(0, 1));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationRate(1, 0));
    Check(new SimulationRate(9_000_000_000_000_000_000, 3).Numerator == 3_000_000_000_000_000_000, "GCD edge case");
    var quarter = SimulationRate.Quarter; long remainder = 0; var repeated = 0L;
    for (var i = 0; i < 4; i++) { Check(quarter.TryScale(1, ref remainder, out var result), "tiny scale"); repeated += result; }
    Check(repeated == 1 && remainder == 0, "remainder after repeated tiny host durations");
    remainder = 0; Check(quarter.TryScale(4, ref remainder, out var combined) && combined == 1 && remainder == 0, "combined duration");
    var half = SimulationRate.Half; remainder = 0; Check(half.TryScale(2, ref remainder, out var halfResult) && halfResult == 1 && remainder == 0, "half rate exact");
    var twice = SimulationRate.Two; remainder = 0; Check(twice.TryScale(3, ref remainder, out var twiceResult) && twiceResult == 6 && remainder == 0, "double rate exact");
    remainder = 0; Check(half.TryScale(1, ref remainder, out var zero) && zero == 0 && remainder == 1, "retained fractional remainder");
    twice.ResetRemainder(ref remainder); Check(remainder == 0, "rate change resets remainder");
    remainder = 0; Check(!twice.TryScale(long.MaxValue, ref remainder, out _), "Int128 scaling overflow reports false");
    Check(!twice.TryScale(-1, ref remainder, out _), "negative host duration rejected");
    remainder = twice.Denominator; Check(!twice.TryScale(1, ref remainder, out _), "invalid remainder rejected");
    remainder = 0; var scripted = 0L; for (var i = 0; i < 10_000; i++) { Check(new SimulationRate(5, 7).TryScale(13, ref remainder, out var result), "scripted scale"); scripted = checked(scripted + result); Check(remainder >= 0 && remainder < 7, "remainder invariant"); } Check(scripted == 92_857 && remainder == 1, "deterministic scripted conversion");
}

static void EventOrderingTests()
{
    var early = Header(1, -1, 0, 1); var late = Header(2, 1, 0, 1);
    var highPriority = Header(3, 0, -1, 1); var lowPriority = Header(4, 0, 1, 1);
    var firstSequence = Header(5, 0, 0, 1); var laterSequence = Header(6, 0, 0, 2);
    var firstId = Header(7, 0, 0, 3); var laterId = Header(8, 0, 0, 3);
    Check(SimulationEventHeaderComparer.Compare(early, late) < 0 && SimulationEventHeaderComparer.Compare(highPriority, lowPriority) < 0 && SimulationEventHeaderComparer.Compare(firstSequence, laterSequence) < 0 && SimulationEventHeaderComparer.Compare(firstId, laterId) < 0, "ordering tuple");
    Check(SimulationEventHeaderComparer.Compare(early, early) == 0 && Math.Sign(SimulationEventHeaderComparer.Compare(early, late)) == -Math.Sign(SimulationEventHeaderComparer.Compare(late, early)), "equality and antisymmetry");
    Check(SimulationEventHeaderComparer.Compare(early, highPriority) < 0 && SimulationEventHeaderComparer.Compare(highPriority, lowPriority) < 0 && SimulationEventHeaderComparer.Compare(early, lowPriority) < 0, "transitivity");
    var duplicate = new[] { early, early }; Throws<ArgumentException>(() => SimulationEventHeaderComparer.ValidateStrictlyOrdered(duplicate));
    var unordered = new[] { late, early }; Throws<ArgumentException>(() => SimulationEventHeaderComparer.ValidateStrictlyOrdered(unordered));
    var minimum = Header(9, long.MinValue, int.MinValue, ulong.MaxValue); var maximum = Header(10, long.MaxValue, int.MaxValue, ulong.MaxValue - 1); Check(SimulationEventHeaderComparer.Compare(minimum, maximum) < 0, "extreme timestamp and priority ordering");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(SimulationEventId.Invalid, SimulationInstant.Zero, 0, new SimulationEventSequence(1), SimulationEventKind.Marker));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(new SimulationEventId(1), SimulationInstant.Zero, 0, SimulationEventSequence.Unassigned, SimulationEventKind.Marker));
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationEventHeader(new SimulationEventId(1), SimulationInstant.Zero, 0, new SimulationEventSequence(1), (SimulationEventKind)255));

    var canonical = CreateStressHeaders(); Array.Sort(canonical, SimulationEventHeaderComparer.Compare); SimulationEventHeaderComparer.ValidateStrictlyOrdered(canonical); var hash = Hash(canonical);
    for (var pass = 0; pass < 8; pass++) { var shuffled = (SimulationEventHeader[])canonical.Clone(); Shuffle(shuffled, (ulong)(pass + 11)); Array.Sort(shuffled, SimulationEventHeaderComparer.Compare); Check(Hash(shuffled) == hash, "permutation canonical ordering"); }
    Console.WriteLine($"Deterministic event-order stress hash: 0x{hash:X16}");
}

static void AllocationTests()
{
    var instant = SimulationInstant.Zero; var duration = new SimulationDuration(1); var rate = new SimulationRate(5, 7); long remainder = 0; var left = Header(1, 0, 0, 1); var right = Header(2, 1, 0, 2);
    _ = instant + duration; rate.TryScale(1, ref remainder, out _); _ = SimulationEventHeaderComparer.Compare(left, right);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { instant += duration; _ = instant - duration; rate.TryScale(13, ref remainder, out _); _ = SimulationEventHeaderComparer.Compare(left, right); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "steady-state arithmetic, scaling, and comparison allocations");
}

static void CelestialContractTests()
{
    Check(CelestialBodyId.Invalid.Value == 0 && !CelestialBodyId.Invalid.IsValid && new CelestialBodyId(2) > new CelestialBodyId(1), "celestial ID value behavior");
    var definitions = new[]
    {
        new CelestialBodyDefinition(new CelestialBodyId(10), null, new ReferenceFrameId(1), 1000d),
        new CelestialBodyDefinition(new CelestialBodyId(20), new CelestialBodyId(10), new ReferenceFrameId(2), 100d),
        new CelestialBodyDefinition(new CelestialBodyId(30), new CelestialBodyId(20), new ReferenceFrameId(3), 10d),
    };
    var states = new[]
    {
        CelestialBodyState.Root(new CelestialBodyId(10)),
        CelestialBodyState.Orbiting(new CelestialBodyId(20), new TwoBodyTrajectory(new CelestialBodyId(10), SimulationInstant.Zero, new CartesianState(new Double3(100, 0, 0), new Double3(0, 10, 0)), TwoBodyPropagationModel.CartesianTwoBodyV1)),
        CelestialBodyState.Orbiting(new CelestialBodyId(30), new TwoBodyTrajectory(new CelestialBodyId(20), SimulationInstant.FromWholeSeconds(7), new CartesianState(new Double3(20, 0, 0), new Double3(0, 3, 0)), TwoBodyPropagationModel.CartesianTwoBodyV1)),
    };
    Check(CelestialStateStore.TryCreate(definitions, states, out var store, out var status) && store is not null && status == CelestialStateStoreStatus.Success, "valid authoritative celestial catalog");
    var view = store!.CreateView();
    Check(view.Count == 3 && view.GetDefinition(0).Id.Value == 10 && view.GetDefinition(1).Id.Value == 20 && view.GetState(0).Trajectory is null, "root representation and caller declaration order");
    Check(view.TryGetIndex(new CelestialBodyId(30), out var moonIndex) && moonIndex == 2 && view.TryGetDefinition(new CelestialBodyId(10), out var star) && star.GravitationalParameter == 1000d && view.TryGetState(new CelestialBodyId(20), out var planet) && planet.Trajectory!.Value.CentralBody == new CelestialBodyId(10), "allocation-free ID lookup and immutable records");
    var state = new SimulationState(store); var stateView = state.CreateView(); Check(stateView.Revision == StateRevision.Zero && stateView.Celestial.Count == 3, "SimulationState owns celestial store without revision mutation");
    var hash = CelestialContractHash.Compute(view); Check(hash == CelestialContractHash.Compute(view), "celestial raw-value hash repeatability"); Console.WriteLine($"Deterministic celestial-contract hash: 0x{hash:X16}");
    Check(CelestialStateStore.TryCreate(definitions, states, out var repeatedStore, out var repeatedStatus) && repeatedStore is not null && repeatedStatus == CelestialStateStoreStatus.Success && CelestialContractHash.Compute(repeatedStore.CreateView()) == hash, "repeated construction produces identical celestial hash");
    definitions[1] = definitions[1] with { GravitationalParameter = 999d }; states[1] = CelestialBodyState.Root(new CelestialBodyId(20));
    Check(view.GetDefinition(1).GravitationalParameter == 100d && view.GetState(1).Trajectory is not null, "source arrays cannot mutate authoritative store");

    var zeroDefinitions = new[] { new CelestialBodyDefinition(CelestialBodyId.Invalid, null, new ReferenceFrameId(1), 1d) }; var zeroStates = new[] { CelestialBodyState.Root(CelestialBodyId.Invalid) };
    CheckStore(zeroDefinitions, zeroStates, CelestialStateStoreStatus.InvalidBodyId, "zero ID rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(0), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)) }, CelestialStateStoreStatus.InvalidInertialFrame, "invalid frame rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), double.NaN) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)) }, CelestialStateStoreStatus.InvalidGravitationalParameter, "non-finite mu rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), new CelestialBodyId(1), new ReferenceFrameId(1), 1d) }, new[] { CelestialBodyState.Orbiting(new CelestialBodyId(1), new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.SelfPrimaryBody, "self-primary rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), new CelestialBodyId(2), new ReferenceFrameId(1), 1d) }, new[] { CelestialBodyState.Orbiting(new CelestialBodyId(1), new TwoBodyTrajectory(new CelestialBodyId(2), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.MissingPrimaryBody, "missing-primary rejection");
    var cycleDefinitions = new[] { new CelestialBodyDefinition(new CelestialBodyId(1), new CelestialBodyId(2), new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }; var cycleStates = new[] { CelestialBodyState.Orbiting(new CelestialBodyId(1), new TwoBodyTrajectory(new CelestialBodyId(2), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) };
    CheckStore(cycleDefinitions, cycleStates, CelestialStateStoreStatus.PrimaryBodyCycle, "cycle rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Root(new CelestialBodyId(1)) }, CelestialStateStoreStatus.DuplicateBodyId, "duplicate ID rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), null, new ReferenceFrameId(1), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Root(new CelestialBodyId(2)) }, CelestialStateStoreStatus.DuplicateInertialFrame, "duplicate frame rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d) }, new[] { CelestialBodyState.Orbiting(new CelestialBodyId(1), new TwoBodyTrajectory(new CelestialBodyId(2), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.RootTrajectoryNotAllowed, "root trajectory rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Root(new CelestialBodyId(2)) }, CelestialStateStoreStatus.ChildTrajectoryRequired, "child trajectory requirement");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(CelestialBodyId.Invalid, SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.InvalidTrajectoryCentralBody, "invalid central-body rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(new CelestialBodyId(2), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.TrajectoryPrimaryMismatch, "central/primary mismatch rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, new CartesianState(new Double3(double.NaN, 0, 0), Double3.Zero), TwoBodyPropagationModel.CartesianTwoBodyV1)) }, CelestialStateStoreStatus.NonFiniteCartesianState, "non-finite Cartesian rejection");
    CheckStore(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }, new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, new CartesianState(Double3.Zero, Double3.Zero), (TwoBodyPropagationModel)99)) }, CelestialStateStoreStatus.InvalidTrajectoryModel, "trajectory model rejection");
    Check(!CelestialStateStore.TryCreate(new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), 1d) }, Array.Empty<CelestialBodyState>(), out var failedStore, out var failedStatus) && failedStore is null && failedStatus == CelestialStateStoreStatus.StateCountMismatch, "failed construction publishes no store");

    _ = CelestialContractHash.Compute(view); _ = view.TryGetIndex(new CelestialBodyId(20), out _); var before = GC.GetAllocatedBytesForCurrentThread(); ulong traversal = 0;
    for (var iteration = 0; iteration < 100_000; iteration++) { for (var index = 0; index < view.Count; index++) traversal = Mix(traversal, view.GetDefinition(index).Id.Value); Check(view.TryGetIndex(new CelestialBodyId(30), out var lookup) && lookup == 2 && view.GetState(1).Trajectory!.Value.StateAtEpoch.IsFinite, "warm lookup and validation"); traversal = Mix(traversal, CelestialContractHash.Compute(view)); }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(allocated == 0 && traversal != 0, "warmed celestial traversal, lookup, validation, and hashing allocate zero bytes");
    Console.WriteLine($"Warm celestial view traversal and lookup allocations: {allocated} bytes");
}

static void CheckStore(CelestialBodyDefinition[] definitions, CelestialBodyState[] states, CelestialStateStoreStatus expected, string message) => Check(!CelestialStateStore.TryCreate(definitions, states, out var store, out var status) && store is null && status == expected, message);

static void CelestialSystemDefinitionTests()
{
    Check(CelestialSystemId.Invalid.Value == 0 && !CelestialSystemId.Invalid.IsValid, "celestial system ID value behavior");
    var sol = CelestialSystemFixtures.SolMini; var geocentric = CelestialSystemFixtures.GeocentricDemo; var binary = CelestialSystemFixtures.BinaryDemo;
    Check(sol.Count == 3 && sol.RootBody == new CelestialBodyId(1) && sol.GetNodeInTraversalOrder(0).TrajectoryModel == CelestialTrajectoryModel.FixedBody && sol.GetNodeInTraversalOrder(2).Id == new CelestialBodyId(3), "valid Sol hierarchy has root-first deterministic traversal");
    Check(geocentric.Count == 2 && geocentric.RootBody == new CelestialBodyId(10) && geocentric.GetNodeInTraversalOrder(1).TrajectoryModel == CelestialTrajectoryModel.CircularOrbit, "valid geocentric hierarchy");
    Check(binary.Count == 2 && binary.GetNodeInTraversalOrder(1).TrajectoryModel == CelestialTrajectoryModel.CircularOrbit, "valid binary hierarchy supports authored circular evaluation");
    Check(sol.TryGetBody(new CelestialBodyId(2), out var earth) && earth.Identity.ParentBody == new CelestialBodyId(1) && !sol.TryGetBody(new CelestialBodyId(99), out _), "deterministic body lookup");

    var nodes = new[]
    {
        Node(30, 10, 3, CelestialTrajectoryModel.AnalyticalKepler),
        Node(20, 10, 2, CelestialTrajectoryModel.CircularOrbit),
        Node(10, null, 1, CelestialTrajectoryModel.FixedBody),
    };
    Check(CelestialSystemDefinition.TryCreate(new CelestialSystemId(77), nodes, Mapping(), Metadata(), out var ordered, out var validation) && ordered is not null && validation.Succeeded && ordered.GetNodeInTraversalOrder(0).Id.Value == 10 && ordered.GetNodeInTraversalOrder(1).Id.Value == 20 && ordered.GetNodeInTraversalOrder(2).Id.Value == 30, "traversal order is root-first then stable body ID");
    var hash = CelestialSystemDefinitionHash.Compute(ordered!); Check(hash == CelestialSystemDefinitionHash.Compute(ordered!), "celestial-system hash repeatability");
    Check(CelestialSystemDefinition.TryCreate(new CelestialSystemId(77), nodes, Mapping(), Metadata(), out var repeated, out _) && repeated is not null && CelestialSystemDefinitionHash.Compute(repeated) == hash, "repeated system construction hash");
    nodes[0] = Node(30, null, 3, CelestialTrajectoryModel.FixedBody); Check(ordered!.GetNodeInTraversalOrder(2).ParentId == new CelestialBodyId(10), "system definition copies caller data");

    CheckSystem([Node(1, null, 1, CelestialTrajectoryModel.FixedBody), Node(1, 2, 2, CelestialTrajectoryModel.AnalyticalKepler)], CelestialSystemValidationStatus.DuplicateBodyId, "duplicate ID rejection");
    CheckSystem([Node(1, null, 1, CelestialTrajectoryModel.FixedBody), Node(2, 3, 2, CelestialTrajectoryModel.AnalyticalKepler), Node(3, 2, 3, CelestialTrajectoryModel.AnalyticalKepler)], CelestialSystemValidationStatus.ParentCycle, "cycle rejection");
    CheckSystem([Node(1, null, 1, CelestialTrajectoryModel.FixedBody), Node(2, 99, 2, CelestialTrajectoryModel.AnalyticalKepler)], CelestialSystemValidationStatus.MissingCatalogParent, "missing parent rejection");
    CheckSystem([Node(1, null, 1, CelestialTrajectoryModel.FixedBody), Node(2, null, 2, CelestialTrajectoryModel.FixedBody)], CelestialSystemValidationStatus.MultipleRoots, "multiple roots rejection");
    CheckSystem([Node(1, null, 1, (CelestialTrajectoryModel)99)], CelestialSystemValidationStatus.InvalidTrajectoryModel, "unsupported trajectory model rejection");
    CheckSystem([Node(1, null, 1, CelestialTrajectoryModel.AnalyticalKepler)], CelestialSystemValidationStatus.RootModelInvalid, "root model rejection");

    _ = CelestialSystemDefinitionHash.Compute(sol); _ = sol.TryGetNode(new CelestialBodyId(3), out _); var before = GC.GetAllocatedBytesForCurrentThread(); ulong traversal = 14695981039346656037UL;
    for (var iteration = 0; iteration < 100_000; iteration++) { for (var index = 0; index < sol.Count; index++) traversal = Mix(traversal, sol.GetNodeInTraversalOrder(index).Id.Value); Check(sol.TryGetNode(new CelestialBodyId(3), out var moon) && moon.TrajectoryModel == CelestialTrajectoryModel.AnalyticalKepler, "warm system lookup"); traversal = Mix(traversal, CelestialSystemDefinitionHash.Compute(sol)); }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0 && traversal != 0, "warmed celestial-system traversal, lookup, and hashing allocate zero bytes");
    Console.WriteLine($"Deterministic celestial-system validation hash: 0x{hash:X16}; warm allocations={allocated} bytes");

    static CelestialHierarchyNode Node(ulong id, ulong? parent, long frame, CelestialTrajectoryModel model) => new(new CelestialBodyDefinition(new(id), parent is { } value ? new CelestialBodyId(value) : null, new ReferenceFrameId(frame), 1d), model);
    static CelestialSystemTimeMapping Mapping() => CelestialSystemTimeMapping.Identity(new(1));
    static CelestialEphemerisMetadata Metadata() => new(new(1), new(1), new(1), long.MinValue, long.MaxValue, new(1), new(1), new(0, 0), new(0, 0));
    static void CheckSystem(CelestialHierarchyNode[] candidates, CelestialSystemValidationStatus expected, string message) => Check(!CelestialSystemDefinition.TryCreate(new CelestialSystemId(99), candidates, Mapping(), Metadata(), out var definition, out var result) && definition is null && result.Status == expected, message);
}

static void CelestialBodyCatalogTests()
{
    var root = new CelestialBodyCatalogEntry(new(new(700), "Barycenter", CelestialBodyClassification.Barycenter, null, default, default, default), new(0d, 0d, 0d, 0d, 0d, default, default, default));
    var child = new CelestialBodyCatalogEntry(new(new(701), "World", CelestialBodyClassification.Planet, new(700), default, default, default), new(3.986004418e14d, 6_371_000d, 6_378_137d, 6_356_752d, 1d / 298.257223563d, default, default, default));
    var entries = new[] { root, child };
    Check(CelestialBodyCatalog.TryCreate(entries, out var catalog, out var validation) && catalog is not null && validation.Succeeded, "valid immutable body catalog");
    Check(catalog!.TryGet(new(701), out var lookedUp) && lookedUp.Identity.DisplayName == "World" && catalog.TryGetPhysicalProperties(new(701), out var physical) && physical.GravitationalParameter == child.PhysicalProperties.GravitationalParameter, "stable ID and physical lookup");
    Check(!CelestialBodyCatalog.TryCreate([root, root], out _, out validation) && validation.Status == CelestialSystemValidationStatus.DuplicateBodyId, "duplicate body ID rejection");
    Check(!CelestialBodyCatalog.TryCreate([root, child with { Identity = child.Identity with { DisplayName = "Barycenter" } }], out _, out validation) && validation.Status == CelestialSystemValidationStatus.DuplicateBodyDisplayName, "duplicate display name rejection");
    Check(!CelestialBodyCatalog.TryCreate([root, child with { Identity = child.Identity with { ParentBody = new(999) } }], out _, out validation) && validation.Status == CelestialSystemValidationStatus.MissingCatalogParent, "missing catalog parent rejection");
    Check(!CelestialBodyCatalog.TryCreate([root, child with { PhysicalProperties = child.PhysicalProperties with { GravitationalParameter = -1d } }], out _, out validation) && validation.Status == CelestialSystemValidationStatus.InvalidPhysicalProperties, "negative physical constants rejection");
    Check(!CelestialBodyCatalog.TryCreate([root, child with { Identity = child.Identity with { Aliases = new CelestialBodyAliases(["World", "World"]) } }], out _, out validation) && validation.Status == CelestialSystemValidationStatus.InvalidBodyAlias, "invalid aliases rejection");
    Check(SolarSystemBodyIds.SolarSystemBarycenter.IsValid && SolarSystemBodyIds.Sun.Value < SolarSystemBodyIds.Neptune.Value, "reserved solar IDs stable");
    var firstHash = CatalogHash(catalog); Check(CelestialBodyCatalog.TryCreate(entries, out var copy, out _) && CatalogHash(copy!) == firstHash, "catalog hash determinism");
    Check(CelestialBodyCatalog.TryCreate([root, child with { PhysicalProperties = child.PhysicalProperties with { MeanRadius = 6_372_000d } }], out var changed, out _) && CatalogHash(changed!) != firstHash, "catalog hash sensitivity");
    _ = catalog.TryGet(new(701), out _); _ = catalog.TryGetPhysicalProperties(new(701), out _); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 0;
    for (var index = 0; index < 100_000; index++) { Check(catalog.TryGet(new(701), out var body), "warm catalog lookup"); Check(catalog.TryGetPhysicalProperties(new(701), out var props), "warm property lookup"); checksum = Mix(checksum, body.Id.Value ^ (ulong)BitConverter.DoubleToInt64Bits(props.MeanRadius)); }
    Check(GC.GetAllocatedBytesForCurrentThread() - before == 0 && checksum != 0, "warmed catalog lookup allocation");
}

static void CelestialSystemTimeAndProvenanceTests()
{
    var domain = new CelestialTimeDomainId(7); var identity = CelestialSystemTimeMapping.Identity(domain);
    Check(identity.TryMap(SimulationInstant.FromWholeSeconds(12), out var mapped) == CelestialSystemTimeMappingStatus.Success && mapped.Domain == domain && mapped.WholeDomainTicks == 12_000_000 && mapped.IsExact, "identity time mapping");
    var offset = new CelestialSystemTimeMapping(SimulationInstant.FromWholeSeconds(10), new(domain, 100, 10), 1, 1);
    Check(offset.TryMap(SimulationInstant.FromWholeSeconds(12), out mapped) == CelestialSystemTimeMappingStatus.Success && mapped.WholeDomainTicks == 120 && mapped.IsExact, "positive epoch offset");
    var scaled = new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, 10), 3, 2);
    Check(scaled.TryMap(SimulationInstant.FromWholeSeconds(2), out mapped) == CelestialSystemTimeMappingStatus.Success && mapped.WholeDomainTicks == 30 && mapped.IsExact, "rational time scaling");
    var inexact = new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, 1), 1, 3);
    Check(inexact.TryMap(SimulationInstant.FromWholeSeconds(1), out mapped) == CelestialSystemTimeMappingStatus.Success && mapped.WholeDomainTicks == 0 && mapped.RemainderNumerator == 1_000_000 && mapped.RemainderDenominator == 3_000_000, "inexact mapping retains exact remainder");
    Check(inexact.TryMap(SimulationInstant.FromWholeSeconds(-1), out mapped) == CelestialSystemTimeMappingStatus.Success && mapped.WholeDomainTicks == -1 && mapped.RemainderNumerator == 2_000_000 && mapped.RemainderDenominator == 3_000_000, "negative mapping uses Euclidean remainder");
    Check(offset.TryMap(SimulationInstant.FromWholeSeconds(8), out mapped) == CelestialSystemTimeMappingStatus.Success && mapped.WholeDomainTicks == 80, "requested time before anchor");
    Check(offset.TryMap(SimulationInstant.FromWholeSeconds(9), out var before) == CelestialSystemTimeMappingStatus.Success && offset.TryMap(SimulationInstant.FromWholeSeconds(11), out var after) == CelestialSystemTimeMappingStatus.Success && before.WholeDomainTicks == 90 && after.WholeDomainTicks == 110, "forward and backward anchor queries");
    Check(new CelestialSystemTimeMapping(new(long.MaxValue), new(domain, 0, 1), 1, 1).TryMap(new(long.MinValue), out _) == CelestialSystemTimeMappingStatus.ArithmeticOverflow, "checked subtraction overflow");
    Check(new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, long.MaxValue), long.MaxValue, 1).TryMap(new(long.MaxValue), out _) == CelestialSystemTimeMappingStatus.ArithmeticOverflow, "Int128 multiplication overflow");
    Check(new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, long.MaxValue, 1), 1, 1).TryMap(new(1_000_000), out _) == CelestialSystemTimeMappingStatus.MappedTimeOverflow, "mapped Int64 overflow");
    Check(new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, 1), 0, 1).TryMap(SimulationInstant.Zero, out _) == CelestialSystemTimeMappingStatus.InvalidScaleNumerator, "invalid numerator");
    Check(new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, 1), 1, 0).TryMap(SimulationInstant.Zero, out _) == CelestialSystemTimeMappingStatus.InvalidScaleDenominator, "invalid denominator");
    Check(new CelestialSystemTimeMapping(SimulationInstant.Zero, new(domain, 0, 0), 1, 1).TryMap(SimulationInstant.Zero, out _) == CelestialSystemTimeMappingStatus.InvalidDomainTickRate, "invalid tick rate");
    Check(CelestialSystemTimeMapping.Identity(CelestialTimeDomainId.Invalid).TryMap(SimulationInstant.Zero, out _) == CelestialSystemTimeMappingStatus.InvalidTimeDomain, "invalid time domain");
    Check(CelestialSystemFixtures.SolMini.TimeMapping == CelestialSystemTimeMapping.Identity(new(1)), "fixtures declare explicit identity mapping");

    var nodes = new[] { new CelestialHierarchyNode(new(new(1), null, new ReferenceFrameId(1), 1d), CelestialTrajectoryModel.FixedBody) };
    var metadata = new CelestialEphemerisMetadata(new(1), new(2), domain, -5, 5, new(3), new(4), new(5, 6), new(7, 8));
    Check(CelestialSystemDefinition.TryCreate(new(400), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata, out var system, out var validation) && system is not null && validation.Succeeded, "one system-wide mapping and compatible metadata");
    Check(system!.TryMapTime(new(-5_000_000), out _) == CelestialSystemTimeMappingStatus.Success && system.TryMapTime(new(5_000_000), out _) == CelestialSystemTimeMappingStatus.Success && system.TryMapTime(new(-6_000_000), out _) == CelestialSystemTimeMappingStatus.OutsideSupportedInterval && system.TryMapTime(new(6_000_000), out _) == CelestialSystemTimeMappingStatus.OutsideSupportedInterval, "inclusive supported interval boundaries");
    Check(!CelestialSystemDefinition.TryCreate(new(401), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata with { Domain = new(8) }, out _, out validation) && validation.Status == CelestialSystemValidationStatus.MappingMetadataDomainMismatch, "metadata domain compatibility");
    Check(!CelestialSystemDefinition.TryCreate(new(401), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata with { SupportedStartDomainTicks = 6, SupportedEndDomainTicks = 5 }, out _, out validation) && validation.Status == CelestialSystemValidationStatus.InvalidSupportedInterval, "invalid interval rejection");
    var hash = CelestialSystemDefinitionHash.Compute(system); Check(CelestialSystemDefinition.TryCreate(new(400), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata with { Version = new(9) }, out var changed, out _) && CelestialSystemDefinitionHash.Compute(changed!) != hash, "dataset version changes definition hash");
    Check(CelestialSystemDefinition.TryCreate(new(400), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata with { ContentHash = new(9, 9) }, out changed, out _) && CelestialSystemDefinitionHash.Compute(changed!) != hash, "content hash changes definition hash");
    Check(CelestialSystemDefinition.TryCreate(new(400), nodes, new(SimulationInstant.Zero, new(domain, 0, 1), 1, 1), metadata with { AuthoredModificationHash = new(9, 9), CoordinateFrame = new(9), ConstantsVersion = new(9) }, out changed, out _) && CelestialSystemDefinitionHash.Compute(changed!) != hash, "authored metadata changes definition hash");
    _ = system.TryMapTime(SimulationInstant.Zero, out _); var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { Check(system.TryMapTime(new SimulationInstant(index - 50_000), out var value) == CelestialSystemTimeMappingStatus.Success, "warm system mapping"); checksum = Mix(checksum, (ulong)value.WholeDomainTicks); }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore; Check(allocated == 0 && checksum != 0, "warmed system mapping allocates zero bytes");
    Console.WriteLine($"Deterministic celestial-system time hash: 0x{hash:X16}; warm mappings={allocated} bytes");
}

static void CelestialEphemerisCatalogTests()
{
    var mapping = CelestialSystemTimeMapping.Identity(new(1)); var metadata = new CelestialEphemerisMetadata(new(99), new(1), new(1), long.MinValue, long.MaxValue, new(1), new(1), new(0, 0), new(0, 0));
    var fixedSource = new CelestialEphemerisSource(new(1), CelestialTrajectoryModel.FixedBody, metadata with { Source = new(1) });
    var circularSource = new CelestialEphemerisSource(new(2), CelestialTrajectoryModel.CircularOrbit, metadata with { Source = new(2) });
    var fixedPayloads = new[] { FixedBodyEphemerisPayload.Identity }; var circularPayloads = new[] { new CircularOrbitEphemerisPayload(0, 2d, 0d, DoubleQuaternion.Identity, 1d) };
    var nodes = new[] { new CelestialHierarchyNode(new CelestialBodyDefinition(new(1), null, new ReferenceFrameId(1), 1d), new CelestialEphemerisBinding(CelestialTrajectoryModel.FixedBody, new(1), 0)), new CelestialHierarchyNode(new CelestialBodyDefinition(new(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d), new CelestialEphemerisBinding(CelestialTrajectoryModel.CircularOrbit, new(2), 0)) };
    Check(CelestialSystemDefinition.TryCreate(new(901), nodes, mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out var system, out var validation) && system is not null && validation.Succeeded, "typed FixedBody and CircularOrbit bindings validate");
    var constructionBefore = GC.GetAllocatedBytesForCurrentThread(); Check(CelestialSystemDefinition.TryCreate(new(901), nodes, mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out var constructionCopy, out _) && constructionCopy is not null, "catalog construction copy"); var constructionAllocated = GC.GetAllocatedBytesForCurrentThread() - constructionBefore;
    Check(system!.TryGetSource(new(2), out var lookedUp) && lookedUp == circularSource && system.TryGetFixedBody(0, out _) && system.TryGetCircularOrbit(0, out _), "deterministic source and typed payload lookup");
    var hash = CelestialSystemDefinitionHash.Compute(system);
    Check(CelestialSystemDefinition.TryCreate(new(901), nodes, mapping, metadata, [fixedSource, circularSource], fixedPayloads, [circularPayloads[0] with { InitialPhaseRadians = .25d }], [], out var changed, out _) && CelestialSystemDefinitionHash.Compute(changed!) != hash, "payload value changes definition hash");
    Check(CelestialSystemDefinition.TryCreate(new(901), nodes, mapping, metadata, [circularSource, fixedSource], fixedPayloads, circularPayloads, [], out changed, out _) && CelestialSystemDefinitionHash.Compute(changed!) != hash, "source declaration order changes definition hash");
    Check(!CelestialSystemDefinition.TryCreate(new(902), nodes, mapping, metadata, [fixedSource, fixedSource], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.DuplicateEphemerisSourceId, "duplicate source rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), [nodes[0], nodes[1] with { Ephemeris = new(CelestialTrajectoryModel.CircularOrbit, new(9), 0) }], mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.MissingEphemerisSource, "missing source rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), [nodes[0], nodes[1] with { Ephemeris = new(CelestialTrajectoryModel.CircularOrbit, new(2), -1) }], mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.NegativePayloadIndex, "negative payload index rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), [nodes[0], nodes[1] with { Ephemeris = new(CelestialTrajectoryModel.CircularOrbit, new(2), 1) }], mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.PayloadIndexOutOfRange, "payload bounds rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), [nodes[0] with { Ephemeris = new(CelestialTrajectoryModel.CircularOrbit, new(2), 0) }, nodes[1]], mapping, metadata, [fixedSource, circularSource], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.RootModelInvalid, "root FixedBody requirement");
    Check(!CelestialSystemDefinition.TryCreate(new(902), nodes, mapping, metadata, [fixedSource, circularSource], [FixedBodyEphemerisPayload.Identity with { Position = new Double3(double.NaN, 0, 0) }], circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.InvalidFixedBodyPayload, "invalid FixedBody rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), nodes, mapping, metadata, [fixedSource, circularSource], fixedPayloads, [circularPayloads[0] with { Radius = 0d }], [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.InvalidCircularOrbitPayload, "invalid CircularOrbit rejection");
    Check(!CelestialSystemDefinition.TryCreate(new(902), nodes, mapping, metadata, [fixedSource, circularSource with { Metadata = circularSource.Metadata with { Domain = new(2) } }], fixedPayloads, circularPayloads, [], out _, out validation) && validation.Status == CelestialSystemValidationStatus.SourceSystemTimeDomainMismatch, "source/system time-domain mismatch rejection");
    _ = system.TryGetSource(new(2), out _); _ = CelestialSystemDefinitionHash.Compute(system); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 0;
    for (var i = 0; i < 100_000; i++) { Check(system.TryGetSource(new(2), out var source), "warm source lookup"); Check(system.TryGetCircularOrbit(0, out var payload), "warm payload lookup"); checksum = Mix(checksum, source.Id.Value ^ (ulong)BitConverter.DoubleToInt64Bits(payload.Radius) ^ CelestialSystemDefinitionHash.Compute(system)); }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0 && checksum != 0, "warmed catalog lookup and hashing allocate zero bytes"); Console.WriteLine($"Deterministic celestial ephemeris-catalog hash: 0x{hash:X16}; construction={constructionAllocated} bytes; warm allocations={allocated} bytes");
}

static void CelestialSystemEvaluationTests()
{
    var instant = SimulationInstant.FromWholeSeconds(12_345);
    var solEvaluations = new ReferenceFrameEvaluation[CelestialSystemFixtures.SolMini.Count]; var solRoots = new FrameTransform[solEvaluations.Length]; var solStaging = new ReferenceFrameEvaluation[solEvaluations.Length]; var solRootStaging = new FrameTransform[solEvaluations.Length];
    var geocentricEvaluations = new ReferenceFrameEvaluation[CelestialSystemFixtures.GeocentricDemo.Count]; var geocentricRoots = new FrameTransform[geocentricEvaluations.Length]; var geocentricStaging = new ReferenceFrameEvaluation[geocentricEvaluations.Length]; var geocentricRootStaging = new FrameTransform[geocentricEvaluations.Length];
    var binaryEvaluations = new ReferenceFrameEvaluation[CelestialSystemFixtures.BinaryDemo.Count]; var binaryRoots = new FrameTransform[binaryEvaluations.Length]; var binaryStaging = new ReferenceFrameEvaluation[binaryEvaluations.Length]; var binaryRootStaging = new FrameTransform[binaryEvaluations.Length];
    var sol = CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SolMini, instant, solEvaluations, solRoots, solStaging, solRootStaging);
    var geocentric = CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.GeocentricDemo, instant, geocentricEvaluations, geocentricRoots, geocentricStaging, geocentricRootStaging);
    var binary = CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.BinaryDemo, instant, binaryEvaluations, binaryRoots, binaryStaging, binaryRootStaging);
    var sampledEvaluations = new ReferenceFrameEvaluation[CelestialSystemFixtures.SampledDemo.Count]; var sampledRoots = new FrameTransform[sampledEvaluations.Length]; var sampledStaging = new ReferenceFrameEvaluation[sampledEvaluations.Length]; var sampledRootStaging = new FrameTransform[sampledEvaluations.Length];
    var sampled = CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SampledDemo, SimulationInstant.Zero, sampledEvaluations, sampledRoots, sampledStaging, sampledRootStaging);
    Check(sol.Succeeded && geocentric.Succeeded && binary.Succeeded && sampled.Succeeded && sampledEvaluations[1].Value.LocalToParent.Translation == Double3.Zero, "binding-routed Kepler, circular, fixed, and sampled dispatch succeeds");
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SampledDemo, SimulationInstant.FromSecondsRounded(.5d), sampledEvaluations, sampledRoots, sampledStaging, sampledRootStaging).Succeeded && sampledEvaluations[1].Value.LocalToParent.Translation.IsFinite && CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SampledDemo, new SimulationInstant(-1_000_001), sampledEvaluations, sampledRoots, sampledStaging, sampledRootStaging).Status == CelestialSystemEvaluationStatus.TimeMappingFailure, "sampled exact rational interior and strict coverage");
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SampledDemo, SimulationInstant.Zero, sampledEvaluations, sampledRoots, sampledStaging, sampledRootStaging).Succeeded, "sampled exact interior identity"); var sampledHash = CelestialSystemEvaluationHash.Compute(sampledEvaluations); var sampledBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var sampleIteration = 0; sampleIteration < 100_000; sampleIteration++) Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SampledDemo, new SimulationInstant(sampleIteration % 1_000_000), sampledEvaluations, sampledRoots, sampledStaging, sampledRootStaging).Succeeded, "warm sampled evaluation");
    var sampledAllocated = GC.GetAllocatedBytesForCurrentThread() - sampledBefore; Check(sampledAllocated == 0, "warm sampled evaluation allocates zero bytes"); Console.WriteLine($"Deterministic SampledDemo hash: 0x{sampledHash:X16}; warm allocation={sampledAllocated} bytes");
    var unsupportedNodes = new[] { new CelestialHierarchyNode(new(new(900), null, new ReferenceFrameId(900), 1d), CelestialTrajectoryModel.FixedBody), new CelestialHierarchyNode(new(new(901), new CelestialBodyId(900), new ReferenceFrameId(901), 1d), CelestialTrajectoryModel.ReservedNumericalNBody) };
    Check(!CelestialSystemDefinition.TryCreate(new(900), unsupportedNodes, CelestialSystemTimeMapping.Identity(new(1)), new(new(1), new(1), new(1), long.MinValue, long.MaxValue, new(1), new(1), new(0, 0), new(0, 0)), out _, out var unsupportedValidation) && unsupportedValidation.Status == CelestialSystemValidationStatus.UnsupportedReservedTrajectoryModel, "reserved numerical model rejects before publication");
    Check(solEvaluations[0].Frame == new ReferenceFrameId(1) && solEvaluations[0].Value.LocalToParent == FrameTransform.Identity && solEvaluations[1].Frame == new ReferenceFrameId(2) && solRoots[2].Translation != solEvaluations[2].Value.LocalToParent.Translation, "root-first parent-before-child transform composition");
    Check(geocentricEvaluations[0].Value.LocalToParent == FrameTransform.Identity && geocentricEvaluations[1].Value.LocalToParent.Translation.IsFinite && geocentricEvaluations[1].Value.OriginVelocityInParent.IsFinite, "fixed body and circular orbit evaluation");
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SolMini, instant, solEvaluations.AsSpan(0, 2), solRoots, solStaging, solRootStaging).Status == CelestialSystemEvaluationStatus.DestinationTooSmall, "undersized evaluated-frame destination rejection");
    var solHash = CelestialSystemEvaluationHash.Compute(solEvaluations); var geocentricHash = CelestialSystemEvaluationHash.Compute(geocentricEvaluations); var binaryHash = CelestialSystemEvaluationHash.Compute(binaryEvaluations);
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SolMini, instant, solEvaluations, solRoots, solStaging, solRootStaging).Succeeded && CelestialSystemEvaluationHash.Compute(solEvaluations) == solHash, "SolMini repeatability");
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.GeocentricDemo, instant, geocentricEvaluations, geocentricRoots, geocentricStaging, geocentricRootStaging).Succeeded && CelestialSystemEvaluationHash.Compute(geocentricEvaluations) == geocentricHash, "GeocentricDemo repeatability");
    Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.BinaryDemo, instant, binaryEvaluations, binaryRoots, binaryStaging, binaryRootStaging).Succeeded && CelestialSystemEvaluationHash.Compute(binaryEvaluations) == binaryHash, "BinaryDemo repeatability");
    _ = CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SolMini, instant, solEvaluations, solRoots, solStaging, solRootStaging); var before = GC.GetAllocatedBytesForCurrentThread(); ulong composition = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { Check(CelestialSystemEvaluator.TryEvaluateSystem(CelestialSystemFixtures.SolMini, instant, solEvaluations, solRoots, solStaging, solRootStaging).Succeeded, "warm SolMini evaluation"); composition = Mix(composition, (ulong)BitConverter.DoubleToInt64Bits(solRoots[2].Translation.X)); }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0 && composition != 0, "warmed system evaluation and transform composition allocate zero bytes");
    Console.WriteLine($"Deterministic celestial-system evaluation hashes: sol=0x{solHash:X16}; geocentric=0x{geocentricHash:X16}; binary=0x{binaryHash:X16}; warm allocations={allocated} bytes");
}

static void TwoBodyPropagationTests()
{
    const double mu = 3.986004418e14d; const double radius = 7_000_000d;
    var circular = new CartesianState(new Double3(radius, 0, 0), new Double3(0, Math.Sqrt(mu / radius), 0));
    var epoch = SimulationInstant.Zero; var periodSeconds = 2d * Math.PI * Math.Sqrt(radius * radius * radius / mu);
    var quarter = SimulationInstant.FromSecondsRounded(periodSeconds * .25d); var half = SimulationInstant.FromSecondsRounded(periodSeconds * .5d); var full = SimulationInstant.FromSecondsRounded(periodSeconds);
    var zero = UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, epoch, mu);
    Check(zero.Succeeded && zero.Iterations == 0 && RawCartesianEqual(zero.State, circular), "zero-duration raw identity");
    var q = RequirePropagation(circular, epoch, quarter, mu, "circular quarter"); var h = RequirePropagation(circular, epoch, half, mu, "circular half"); var f = RequirePropagation(circular, epoch, full, mu, "circular full");
    var circularPositionCheckpointTolerance = Math.Max(PositionTolerance(radius), .01d); var circularVelocityCheckpointTolerance = Math.Max(VelocityTolerance(circular.Velocity.Y), 1e-5d);
    CheckVectorNear(q.State.Position, new Double3(0, radius, 0), circularPositionCheckpointTolerance, "quarter position"); CheckVectorNear(q.State.Velocity, new Double3(-circular.Velocity.Y, 0, 0), circularVelocityCheckpointTolerance, "quarter velocity");
    CheckVectorNear(h.State.Position, new Double3(-radius, 0, 0), circularPositionCheckpointTolerance, "half position"); CheckVectorNear(h.State.Velocity, new Double3(0, -circular.Velocity.Y, 0), circularVelocityCheckpointTolerance, "half velocity");
    CheckVectorNear(f.State.Position, circular.Position, circularPositionCheckpointTolerance * 2d, "full position"); CheckVectorNear(f.State.Velocity, circular.Velocity, circularVelocityCheckpointTolerance * 2d, "full velocity");
    var circularCheckpointPositionError = Math.Max(Math.Max(VectorError(q.State.Position, new Double3(0, radius, 0)), VectorError(h.State.Position, new Double3(-radius, 0, 0))), VectorError(f.State.Position, circular.Position));
    var circularCheckpointVelocityError = Math.Max(Math.Max(VectorError(q.State.Velocity, new Double3(-circular.Velocity.Y, 0, 0)), VectorError(h.State.Velocity, new Double3(0, -circular.Velocity.Y, 0))), VectorError(f.State.Velocity, circular.Velocity));
    var circularEnergyError = RelativeError(SpecificEnergy(q.State, mu), SpecificEnergy(circular, mu)); var circularMomentumError = RelativeError(AngularMomentumMagnitude(q.State), AngularMomentumMagnitude(circular));
    Check(circularEnergyError <= 1e-11d && circularMomentumError <= 1e-11d, "circular invariants");

    var moderate = EllipticState(mu, 12_000_000d, .35d); var high = EllipticState(mu, 20_000_000d, .90d); var moderateTime = SimulationInstant.FromWholeSeconds(4_321); var highTime = SimulationInstant.FromWholeSeconds(7_654);
    var moderateResult = RequirePropagation(moderate, epoch, moderateTime, mu, "moderate ellipse"); var highResult = RequirePropagation(high, epoch, highTime, mu, "high ellipse");
    var moderateEnergyError = RelativeError(SpecificEnergy(moderateResult.State, mu), SpecificEnergy(moderate, mu)); var moderateMomentumError = RelativeError(AngularMomentumMagnitude(moderateResult.State), AngularMomentumMagnitude(moderate));
    var highEnergyError = RelativeError(SpecificEnergy(highResult.State, mu), SpecificEnergy(high, mu)); var highMomentumError = RelativeError(AngularMomentumMagnitude(highResult.State), AngularMomentumMagnitude(high));
    Check(moderateEnergyError <= 1e-11d && moderateMomentumError <= 1e-11d && highEnergyError <= 1e-10d && highMomentumError <= 1e-10d, "elliptic invariants");
    var backward = RequirePropagation(moderateResult.State, moderateTime, epoch, mu, "backward ellipse"); CheckVectorNear(backward.State.Position, moderate.Position, PositionTolerance(Math.Sqrt(moderate.Position.LengthSquared)) * 2d, "backward position"); CheckVectorNear(backward.State.Velocity, moderate.Velocity, VelocityTolerance(Math.Sqrt(moderate.Velocity.LengthSquared)) * 2d, "backward velocity");
    var t2 = SimulationInstant.FromWholeSeconds(8_000); var direct = RequirePropagation(moderate, epoch, t2, mu, "epoch direct"); var replacement = RequirePropagation(moderateResult.State, moderateTime, t2, mu, "epoch replacement"); CheckVectorNear(replacement.State.Position, direct.State.Position, PositionTolerance(Math.Sqrt(direct.State.Position.LengthSquared)) * 2d, "epoch replacement position"); CheckVectorNear(replacement.State.Velocity, direct.State.Velocity, VelocityTolerance(Math.Sqrt(direct.State.Velocity.LengthSquared)) * 2d, "epoch replacement velocity");
    var nearCircular = EllipticState(mu, 9_000_000d, 1e-6d); Check(UniversalVariableTwoBodyPropagator.TryEvaluate(nearCircular, epoch, SimulationInstant.FromWholeSeconds(500), mu).Succeeded, "near-circular supported");

    var hyperbolic = new CartesianState(new Double3(radius, 0, 0), new Double3(0, Math.Sqrt(2d * mu / radius) * 1.01d, 0)); Check(UniversalVariableTwoBodyPropagator.TryEvaluate(hyperbolic, epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.HyperbolicUnsupported, "hyperbolic controlled rejection");
    var parabolic = new CartesianState(new Double3(radius, 0, 0), new Double3(0, Math.Sqrt(2d * mu / radius), 0)); Check(UniversalVariableTwoBodyPropagator.TryEvaluate(parabolic, epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.NearParabolicUnsupported, "near-parabolic controlled rejection");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(new CartesianState(new Double3(radius, 0, 0), new Double3(circular.Velocity.Y, 0, 0)), epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.DegenerateAngularMomentum, "radial rejection");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(new CartesianState(Double3.Zero, circular.Velocity), epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.DegenerateRadius, "zero-radius rejection");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(new CartesianState(new Double3(double.NaN, 0, 0), circular.Velocity), epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.NonFiniteState, "non-finite state rejection");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, SimulationInstant.FromWholeSeconds(1), 0d).Status == TwoBodyPropagationStatus.InvalidGravitationalParameter, "invalid mu rejection");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, new SimulationInstant(UniversalVariableTwoBodyPropagator.MaximumEvaluationTicks), mu).Succeeded, "time-span boundary accepted");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, new SimulationInstant(UniversalVariableTwoBodyPropagator.MaximumEvaluationTicks + 1), mu).Status == TwoBodyPropagationStatus.EvaluationSpanExceeded, "time-span overflow rejected");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluateWithIterationLimitForTest(high, epoch, highTime, mu, 1).Status == TwoBodyPropagationStatus.NonConvergent, "bounded non-convergence seam");

    Check(UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(0d, out var c0, out var s0) && NearlyEqual(c0, .5d, 0d) && NearlyEqual(s0, 1d / 6d, 0d), "Stumpff zero");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(1e-6d, out var cSmall, out var sSmall) && NearlyEqual(cSmall, .5d - 1e-6d / 24d + 1e-12d / 720d, 1e-16d) && NearlyEqual(sSmall, 1d / 6d - 1e-6d / 120d + 1e-12d / 5040d, 1e-16d), "Stumpff series");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(1e-4d, out _, out _) && UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(1e-3d, out _, out _) && UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(100d, out _, out _) && !UniversalVariableTwoBodyPropagator.TryEvaluateStumpffForTest(-1d, out _, out _), "Stumpff branches");

    var adapterView = CreatePropagationView(mu, moderate); var adapter = CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(2), adapterView, moderateTime); Check(adapter.Succeeded && RawCartesianEqual(adapter.State, moderateResult.State), "celestial adapter resolves central mu"); Check(CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(1), adapterView, moderateTime).Status == TwoBodyPropagationStatus.NoTrajectory && CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(99), adapterView, moderateTime).Status == TwoBodyPropagationStatus.BodyNotFound, "adapter root and missing body statuses");

    var circularHash = PropagationHash(circular, epoch, mu, [epoch, quarter, half, full]); var ellipticHash = PropagationHash(moderate, epoch, mu, [epoch, moderateTime, highTime]); var backwardHash = PropagationHash(moderateResult.State, moderateTime, mu, [epoch, moderateTime]); var validationHash = ValidationHash(circular, epoch, mu); var combined = Mix(Mix(Mix(circularHash, ellipticHash), backwardHash), validationHash);
    Check(circularHash == PropagationHash(circular, epoch, mu, [epoch, quarter, half, full]) && combined != 0, "propagator raw-hash repeatability");

    _ = UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, quarter, mu); _ = CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(2), adapterView, quarter);
    var stopwatch = Stopwatch.StartNew(); var before = GC.GetAllocatedBytesForCurrentThread(); var maximumIterations = 0;
    for (var index = 0; index < 100_000; index++) { var result = UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, quarter, mu); Check(result.Succeeded, "warm circular"); maximumIterations = Math.Max(maximumIterations, result.Iterations); }
    var circularMilliseconds = stopwatch.Elapsed.TotalMilliseconds; var circularAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
    stopwatch.Restart(); before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { var result = UniversalVariableTwoBodyPropagator.TryEvaluate(moderate, epoch, moderateTime, mu); Check(result.Succeeded, "warm elliptic"); maximumIterations = Math.Max(maximumIterations, result.Iterations); }
    var ellipticMilliseconds = stopwatch.Elapsed.TotalMilliseconds; var ellipticAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
    before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { Check(CelestialTrajectoryEvaluator.TryEvaluate(new CelestialBodyId(2), adapterView, moderateTime).Succeeded, "warm adapter"); Check(UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, SimulationInstant.FromWholeSeconds(1), 0d).Status == TwoBodyPropagationStatus.InvalidGravitationalParameter, "warm invalid"); Check(UniversalVariableTwoBodyPropagator.TryEvaluate(hyperbolic, epoch, SimulationInstant.FromWholeSeconds(1), mu).Status == TwoBodyPropagationStatus.HyperbolicUnsupported, "warm unsupported"); Check(UniversalVariableTwoBodyPropagator.TryEvaluateWithIterationLimitForTest(high, epoch, highTime, mu, 1).Status == TwoBodyPropagationStatus.NonConvergent, "warm nonconvergent"); }
    var adapterAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(circularAllocated == 0 && ellipticAllocated == 0 && adapterAllocated == 0, "propagation paths allocate zero bytes");
    Console.WriteLine("Two-Body Propagation"); Console.WriteLine($"Circular orbit: t=0 PASS; t=0.25P PASS; t=0.50P PASS; t=1.00P PASS; max checkpoint position error={circularCheckpointPositionError:E3} m; velocity error={circularCheckpointVelocityError:E3} m/s"); Console.WriteLine($"Elliptic orbit: energy relative error={Math.Max(moderateEnergyError, highEnergyError):E3}; angular momentum relative error={Math.Max(moderateMomentumError, highMomentumError):E3}; maximum iterations={maximumIterations}"); Console.WriteLine("Backward propagation: PASS; Epoch replacement equivalence: PASS; Unsupported regimes: PASS"); Console.WriteLine($"Allocation: circular={circularAllocated} bytes, elliptic={ellipticAllocated} bytes, adapter/failure={adapterAllocated} bytes"); Console.WriteLine($"Benchmark: circular={circularMilliseconds:F3} ms, elliptic={ellipticMilliseconds:F3} ms"); Console.WriteLine($"Deterministic propagation hashes: circular=0x{circularHash:X16}, elliptic=0x{ellipticHash:X16}, backward=0x{backwardHash:X16}, validation=0x{validationHash:X16}, combined=0x{combined:X16}");
}

static TwoBodyPropagationResult RequirePropagation(in CartesianState state, SimulationInstant epoch, SimulationInstant time, double mu, string message) { var result = UniversalVariableTwoBodyPropagator.TryEvaluate(state, epoch, time, mu); Check(result.Succeeded, $"{message}: {result.Status}"); return result; }
static CelestialStateView CreatePropagationView(double mu, CartesianState trajectoryState)
{
    var definitions = new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), mu), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) };
    var states = new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, trajectoryState, TwoBodyPropagationModel.CartesianTwoBodyV1)) };
    Check(CelestialStateStore.TryCreate(definitions, states, out var store, out var status) && store is not null && status == CelestialStateStoreStatus.Success, "propagation adapter catalog"); return store!.CreateView();
}
static CartesianState EllipticState(double mu, double semiMajorAxis, double eccentricity) { var periapsis = semiMajorAxis * (1d - eccentricity); return new(new Double3(periapsis, 0, 0), new Double3(0, Math.Sqrt(mu * (2d / periapsis - 1d / semiMajorAxis)), 0)); }
static double SpecificEnergy(in CartesianState state, double mu) => state.Velocity.LengthSquared * .5d - mu / Math.Sqrt(state.Position.LengthSquared);
static double AngularMomentumMagnitude(in CartesianState state) => Math.Sqrt(Double3.Cross(state.Position, state.Velocity).LengthSquared);
static double RelativeError(double actual, double expected) => Math.Abs(actual - expected) / Math.Max(Math.Abs(expected), double.Epsilon);
static double PositionTolerance(double magnitude) => Math.Max(1d, Math.Abs(magnitude)) * 1e-11d;
static double VelocityTolerance(double magnitude) => Math.Max(1d, Math.Abs(magnitude)) * 1e-11d;
static bool NearlyEqual(double actual, double expected, double tolerance) => Math.Abs(actual - expected) <= tolerance;
static double VectorError(in Double3 actual, in Double3 expected) => Math.Sqrt((actual - expected).LengthSquared);
static void CheckVectorNear(in Double3 actual, in Double3 expected, double tolerance, string message) => Check((actual - expected).LengthSquared <= tolerance * tolerance, $"{message}; actual={actual}; expected={expected}; tolerance={tolerance:R}");
static bool RawCartesianEqual(in CartesianState left, in CartesianState right) => BitConverter.DoubleToInt64Bits(left.Position.X) == BitConverter.DoubleToInt64Bits(right.Position.X) && BitConverter.DoubleToInt64Bits(left.Position.Y) == BitConverter.DoubleToInt64Bits(right.Position.Y) && BitConverter.DoubleToInt64Bits(left.Position.Z) == BitConverter.DoubleToInt64Bits(right.Position.Z) && BitConverter.DoubleToInt64Bits(left.Velocity.X) == BitConverter.DoubleToInt64Bits(right.Velocity.X) && BitConverter.DoubleToInt64Bits(left.Velocity.Y) == BitConverter.DoubleToInt64Bits(right.Velocity.Y) && BitConverter.DoubleToInt64Bits(left.Velocity.Z) == BitConverter.DoubleToInt64Bits(right.Velocity.Z);
static ulong PropagationHash(in CartesianState state, SimulationInstant epoch, double mu, ReadOnlySpan<SimulationInstant> times) { ulong hash = 14695981039346656037UL; foreach (ref readonly var time in times) { var result = UniversalVariableTwoBodyPropagator.TryEvaluate(state, epoch, time, mu); hash = Mix(hash, (ulong)result.Status); hash = Mix(hash, (ulong)time.Ticks); if (result.Succeeded) { hash = MixCartesian(hash, result.State); } } return hash; }
static ulong ValidationHash(in CartesianState circular, SimulationInstant epoch, double mu) { ulong hash = 14695981039346656037UL; var hyper = new CartesianState(circular.Position, new Double3(0, Math.Sqrt(2d * mu / circular.Position.X) * 1.01d, 0)); foreach (var result in new[] { UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, SimulationInstant.FromWholeSeconds(1), 0d), UniversalVariableTwoBodyPropagator.TryEvaluate(hyper, epoch, SimulationInstant.FromWholeSeconds(1), mu), UniversalVariableTwoBodyPropagator.TryEvaluate(circular, epoch, new SimulationInstant(UniversalVariableTwoBodyPropagator.MaximumEvaluationTicks + 1), mu) }) { hash = Mix(hash, (ulong)result.Status); hash = Mix(hash, (ulong)result.RequestedTime.Ticks); } return hash; }
static ulong MixCartesian(ulong hash, in CartesianState state) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Position.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Position.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Position.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Velocity.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Velocity.Y)); return Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(state.Velocity.Z)); }

static void CelestialFrameExtractionTests()
{
    var rootBody = new CelestialBodyId(1); var planetBody = new CelestialBodyId(2); var moonBody = new CelestialBodyId(3);
    var rootFrame = new ReferenceFrameId(1); var planetFrame = new ReferenceFrameId(2); var moonFrame = new ReferenceFrameId(3);
    var definitions = new[]
    {
        new CelestialBodyDefinition(rootBody, null, rootFrame, 1_000d),
        new CelestialBodyDefinition(planetBody, rootBody, planetFrame, 100d),
        new CelestialBodyDefinition(moonBody, planetBody, moonFrame, 1d),
    };
    var states = new[]
    {
        CelestialBodyState.Root(rootBody),
        CelestialBodyState.Orbiting(planetBody, new TwoBodyTrajectory(rootBody, SimulationInstant.Zero, new CartesianState(new Double3(100d, 0d, 0d), new Double3(0d, Math.Sqrt(10d), 0d)), TwoBodyPropagationModel.CartesianTwoBodyV1)),
        CelestialBodyState.Orbiting(moonBody, new TwoBodyTrajectory(planetBody, SimulationInstant.Zero, new CartesianState(new Double3(10d, 0d, 0d), new Double3(0d, Math.Sqrt(10d), 0d)), TwoBodyPropagationModel.CartesianTwoBodyV1)),
    };
    Check(CelestialStateStore.TryCreate(definitions, states, out var store, out var storeStatus) && store is not null && storeStatus == CelestialStateStoreStatus.Success, "extraction authoritative store");
    var builder = new ReferenceFrameGraphBuilder(); builder.Add(new ReferenceFrameNode(rootFrame, null, ReferenceFrameKind.Ecl, "root")); builder.Add(new ReferenceFrameNode(planetFrame, rootFrame, ReferenceFrameKind.Cce, "planet")); builder.Add(new ReferenceFrameNode(moonFrame, planetFrame, ReferenceFrameKind.Cci, "moon")); var graph = builder.Build();
    Span<ReferenceFrameEvaluation> values = stackalloc ReferenceFrameEvaluation[3];
    Check(CelestialReferenceFrameEvaluator.TryEvaluate(store!.CreateView(), graph, SimulationInstant.Zero, values) == CelestialReferenceFrameEvaluationStatus.Success, "epoch extraction");
    Check(values[0].Value.LocalToParent == FrameTransform.Identity && values[0].Value.OriginVelocityInParent == Double3.Zero && values[0].Value.AngularVelocityInParent == Double3.Zero, "root identity and velocity");
    CheckVectorNear(values[1].Value.LocalToParent.Translation, new Double3(100d, 0d, 0d), 1e-12d, "planet local epoch"); CheckVectorNear(values[1].Value.OriginVelocityInParent, new Double3(0d, Math.Sqrt(10d), 0d), 1e-12d, "planet local velocity");
    var transforms = new ReferenceFrameTransformSet(graph, values); Span<ReferenceFrameId> source = stackalloc ReferenceFrameId[3]; Span<ReferenceFrameId> target = stackalloc ReferenceFrameId[3]; Span<ReferenceFrameId> traversal = stackalloc ReferenceFrameId[5];
    Check(ReferenceFrameTransformResolver.TryResolveTransform(transforms, moonFrame, rootFrame, source, target, traversal, out var resolved) == ReferenceFrameTransformResolutionStatus.Success, "nested extraction resolution");
    CheckVectorNear(resolved.ConvertPosition(Double3.Zero), new Double3(110d, 0d, 0d), 1e-12d, "nested root position"); CheckVectorNear(resolved.SourceOriginVelocityInTarget, new Double3(0d, 2d * Math.Sqrt(10d), 0d), 1e-12d, "nested root velocity");
    var quarter = SimulationInstant.FromSecondsRounded((2d * Math.PI * Math.Sqrt(100d * 100d * 100d / 1_000d)) / 4d); Check(CelestialReferenceFrameEvaluator.TryEvaluate(store.CreateView(), graph, quarter, values) == CelestialReferenceFrameEvaluationStatus.Success && Math.Abs(values[1].Value.LocalToParent.Translation.X) < .01d && values[1].Value.LocalToParent.Translation.Y > 99.99d, "quarter-period local position");
    Check(CelestialReferenceFrameEvaluator.TryEvaluate(store.CreateView(), graph, SimulationInstant.Zero, values[..2]) == CelestialReferenceFrameEvaluationStatus.DestinationTooSmall, "capacity rejection");
    var mismatchBuilder = new ReferenceFrameGraphBuilder(); mismatchBuilder.Add(new ReferenceFrameNode(rootFrame, null, ReferenceFrameKind.Ecl, "root")); mismatchBuilder.Add(new ReferenceFrameNode(moonFrame, rootFrame, ReferenceFrameKind.Cce, "mismatch")); mismatchBuilder.Add(new ReferenceFrameNode(planetFrame, moonFrame, ReferenceFrameKind.Cci, "mismatch-child"));
    Check(CelestialReferenceFrameEvaluator.TryEvaluate(store.CreateView(), mismatchBuilder.Build(), SimulationInstant.Zero, values) == CelestialReferenceFrameEvaluationStatus.FrameMappingMismatch, "mapping mismatch rejection");
    _ = CelestialReferenceFrameEvaluator.TryEvaluate(store.CreateView(), graph, SimulationInstant.Zero, values); var before = GC.GetAllocatedBytesForCurrentThread(); ulong hash = 14695981039346656037UL;
    for (var index = 0; index < 100_000; index++) { Check(CelestialReferenceFrameEvaluator.TryEvaluate(store.CreateView(), graph, SimulationInstant.Zero, values) == CelestialReferenceFrameEvaluationStatus.Success, "warm extraction"); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(values[2].Value.LocalToParent.Translation.X)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm celestial extraction allocation"); Console.WriteLine($"Celestial frame extraction: allocation=0 bytes; hash=0x{hash:X16}");
}

static void CelestialTrajectoryReplacementTests()
{
    const double mu = 3.986004418e14d; const double radius = 7_000_000d;
    var initialState = new CartesianState(new Double3(radius, 0d, 0d), new Double3(0d, Math.Sqrt(mu / radius), 0d));
    var eventTime = SimulationInstant.FromWholeSeconds(1_234);
    var initial = new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, initialState, TwoBodyPropagationModel.CartesianTwoBodyV1);
    var eventState = RequirePropagation(initialState, SimulationInstant.Zero, eventTime, mu, "replacement event state").State;
    var replacement = new TwoBodyTrajectory(new CelestialBodyId(1), eventTime, new CartesianState(eventState.Position, eventState.Velocity * .99d), TwoBodyPropagationModel.CartesianTwoBodyV1);

    var (timeline, state, engine, scheduled) = CreateReplacementScenario(initial, eventTime, 8, 10, 0);
    var creation = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(scheduled, engine.State, timeline.Revision, new CelestialBodyId(2), replacement);
    Check(creation.Succeeded && creation.Transaction is not null, "candidate creation");
    var candidate = creation.Transaction!.Value;
    var beforeHash = CelestialContractHash.Compute(engine.State.Celestial); var beforeRevision = engine.State.Revision;
    var result = engine.ValidateAndCommit(candidate);
    Check(result.Committed && result.ProcessedEvent is not null && result.ProcessedEvent.Value.CelestialTransition is not null, "atomic replacement commit");
    var processed = result.ProcessedEvent!.Value; var transition = processed.CelestialTransition!.Value;
    Check(engine.State.Revision.Value == beforeRevision.Value + 1 && timeline.Revision.Value == candidate.ExpectedTimelineRevision.Value + 1 && timeline.PendingCount == 0 && engine.ProcessedCount == 1, "replacement revisions and consumption");
    Check(engine.State.Celestial.TryGetState(new CelestialBodyId(2), out var replacedState) && replacedState.Trajectory is not null, "store exposes replacement");
    var actual = replacedState.Trajectory!.Value; Check(TwoBodyTrajectoryIdentity.EqualsRaw(actual, replacement), "replacement value matches");
    Check(beforeHash != CelestialContractHash.Compute(engine.State.Celestial) && transition.EventTime == eventTime && transition.Subject == new CelestialBodyId(2) && transition.StateRevisionAfter == engine.State.Revision, "history transition metadata");
    Check(UniversalVariableTwoBodyPropagator.TryEvaluate(actual.StateAtEpoch, actual.Epoch, eventTime, mu).Succeeded, "replacement remains propagatable");
    Check(TwoBodyTrajectoryIdentity.EqualsRaw(initial, new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, initialState, TwoBodyPropagationModel.CartesianTwoBodyV1)), "original trajectory value unchanged");

    var unsupported = new TwoBodyTrajectory(new CelestialBodyId(1), eventTime, new CartesianState(eventState.Position, eventState.Position * .001d), TwoBodyPropagationModel.CartesianTwoBodyV1);
    var unsupportedScenario = CreateReplacementScenario(initial, eventTime, 8, 11, 0);
    var unsupportedBefore = ReplacementSnapshot(unsupportedScenario.engine, unsupportedScenario.timeline);
    var unsupportedCreation = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(unsupportedScenario.scheduled, unsupportedScenario.engine.State, unsupportedScenario.timeline.Revision, new CelestialBodyId(2), unsupported);
    Check(unsupportedCreation.Status == CelestialTrajectoryTransactionStatus.UnsupportedReplacementOrbit, "unsupported replacement rejected during pure creation");
    Check(unsupportedBefore == ReplacementSnapshot(unsupportedScenario.engine, unsupportedScenario.timeline), "creation rejection is atomic");

    var validationScenario = CreateReplacementScenario(initial, eventTime, 2, 111, 0);
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(validationScenario.scheduled, validationScenario.engine.State, validationScenario.timeline.Revision, new CelestialBodyId(99), replacement).Status == CelestialTrajectoryTransactionStatus.SubjectNotFound, "missing subject rejection");
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(validationScenario.scheduled, validationScenario.engine.State, validationScenario.timeline.Revision, new CelestialBodyId(1), replacement).Status == CelestialTrajectoryTransactionStatus.RootBody, "root subject rejection");
    var noOpScenario = CreateReplacementScenario(replacement, eventTime, 2, 113, 0);
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(noOpScenario.scheduled, noOpScenario.engine.State, noOpScenario.timeline.Revision, new CelestialBodyId(2), replacement).Status == CelestialTrajectoryTransactionStatus.ReplacementNoOp, "no-op replacement rejection");
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(validationScenario.scheduled, validationScenario.engine.State, validationScenario.timeline.Revision, new CelestialBodyId(2), replacement with { CentralBody = new CelestialBodyId(2) }).Status == CelestialTrajectoryTransactionStatus.ReplacementCentralMismatch, "central mismatch rejection");
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(validationScenario.scheduled, validationScenario.engine.State, validationScenario.timeline.Revision, new CelestialBodyId(2), replacement with { StateAtEpoch = new CartesianState(new Double3(double.NaN, 0d, 0d), replacement.StateAtEpoch.Velocity) }).Status == CelestialTrajectoryTransactionStatus.InvalidReplacementState, "non-finite replacement rejection");
    Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(validationScenario.scheduled, validationScenario.engine.State, validationScenario.timeline.Revision, new CelestialBodyId(2), replacement with { Epoch = eventTime + new SimulationDuration(1) }).Status == CelestialTrajectoryTransactionStatus.EventTimeMismatch, "event epoch mismatch rejection");

    var capacityScenario = CreateReplacementScenario(initial, eventTime, 0, 112, 0);
    var capacityCandidate = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(capacityScenario.scheduled, capacityScenario.engine.State, capacityScenario.timeline.Revision, new CelestialBodyId(2), replacement).Transaction!.Value;
    var capacityBefore = ReplacementSnapshot(capacityScenario.engine, capacityScenario.timeline);
    Check(capacityScenario.engine.ValidateAndCommit(capacityCandidate).Status == CelestialTrajectoryTransactionStatus.HistoryCapacityFailure, "history capacity rejection");
    Check(capacityBefore == ReplacementSnapshot(capacityScenario.engine, capacityScenario.timeline), "history capacity rejection is atomic");

    var staleScenario = CreateReplacementScenario(initial, eventTime, 8, 12, 0);
    var staleCreation = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(staleScenario.scheduled, staleScenario.engine.State, staleScenario.timeline.Revision, new CelestialBodyId(2), replacement);
    Check(staleCreation.Transaction is not null, "stale candidate creation");
    var staleCandidate = staleCreation.Transaction!.Value;
    var forged = staleCandidate with { ExpectedStateRevision = new StateRevision(staleCandidate.ExpectedStateRevision.Value + 1) };
    var staleBefore = ReplacementSnapshot(staleScenario.engine, staleScenario.timeline);
    Check(staleScenario.engine.ValidateAndCommit(forged).Status == CelestialTrajectoryTransactionStatus.StateRevisionMismatch, "stale revision rejection");
    Check(staleBefore == ReplacementSnapshot(staleScenario.engine, staleScenario.timeline), "stale rejection is atomic");

    var sameTime = CreateSameTimeReplacementScenario(initial, eventTime, replacement);
    Check(sameTime.first.Transaction is not null && sameTime.secondStale.Transaction is not null, "same-time candidate creation");
    var first = sameTime.first.Transaction!.Value; var staleSecond = sameTime.secondStale.Transaction!.Value;
    Check(sameTime.engine.ValidateAndCommit(first).Committed, "first same-time replacement commits");
    var afterFirst = ReplacementSnapshot(sameTime.engine, sameTime.timeline);
    var staleAtCurrentTimeline = staleSecond with { ExpectedTimelineRevision = sameTime.timeline.Revision };
    Check(sameTime.engine.ValidateAndCommit(staleAtCurrentTimeline).Status == CelestialTrajectoryTransactionStatus.StateRevisionMismatch, "stale same-time candidate rejected");
    Check(afterFirst == ReplacementSnapshot(sameTime.engine, sameTime.timeline), "later rejection preserves earlier commit");
    Check(sameTime.timeline.TryPeekPending(out var secondPending), "second remains canonical pending");
    var secondFresh = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(secondPending, sameTime.engine.State, sameTime.timeline.Revision, new CelestialBodyId(2), sameTime.secondReplacement);
    Check(secondFresh.Transaction is { } second && sameTime.engine.ValidateAndCommit(second).Committed, "second same-time candidate evaluates against first replacement");
    Check(sameTime.engine.ProcessedCount == 2 && sameTime.engine.TryGetProcessed(0, out var firstHistory) && sameTime.engine.TryGetProcessed(1, out var secondHistory) && firstHistory.Event.Priority < secondHistory.Event.Priority && sameTime.engine.State.Revision.Value == 2, "same-time canonical history ordering");

    var replayA = ReplacementReplayHash(initial, eventTime, replacement); var replayB = ReplacementReplayHash(initial, eventTime, replacement);
    Check(replayA == replayB, "replacement replay hash");

    var allocationScenario = CreateReplacementScenario(initial, eventTime, 2, 13, 0);
    _ = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(allocationScenario.scheduled, allocationScenario.engine.State, allocationScenario.timeline.Revision, new CelestialBodyId(2), replacement);
    var allocationCandidate = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(allocationScenario.scheduled, allocationScenario.engine.State, allocationScenario.timeline.Revision, new CelestialBodyId(2), replacement).Transaction!.Value;
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) Check(CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(allocationScenario.scheduled, allocationScenario.engine.State, allocationScenario.timeline.Revision, new CelestialBodyId(2), replacement).Succeeded, "warm replacement creation");
    var creationAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
    allocationBefore = GC.GetAllocatedBytesForCurrentThread(); var allocationCommit = allocationScenario.engine.ValidateAndCommit(allocationCandidate); var commitAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
    Check(allocationCommit.Committed && creationAllocated == 0 && commitAllocated == 0, "replacement paths allocate zero bytes after warmup");

    Console.WriteLine("Celestial Trajectory Replacement");
    Console.WriteLine("Initial authoritative trajectory: PASS; Candidate evaluation: PASS; Atomic replacement commit: PASS");
    Console.WriteLine("Replacement epoch: PASS; Post-replacement propagation: PASS; Stale candidate rejection: PASS");
    Console.WriteLine("Same-time ordering: PASS; Failure atomicity: PASS; Replay: PASS");
    Console.WriteLine($"Allocation: {creationAllocated + commitAllocated} bytes; Replacement hash: 0x{replayA:X16}");
}

static (SimulationTimeline timeline, SimulationState state, SimulationTransactionEngine engine, ScheduledSimulationEvent scheduled) CreateReplacementScenario(TwoBodyTrajectory initial, SimulationInstant eventTime, int historyCapacity, ulong eventId, int priority)
{
    const double mu = 3.986004418e14d;
    var definitions = new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), mu), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) };
    var states = new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), initial) };
    Check(CelestialStateStore.TryCreate(definitions, states, out var store, out var status) && store is not null && status == CelestialStateStoreStatus.Success, "replacement store creation");
    var timeline = new SimulationTimeline(4); Check(timeline.Schedule(SimulationInstant.Zero, new SimulationEventRequest(new SimulationEventId(eventId), eventTime, priority, SimulationEventKind.ReplaceTrajectory)).Succeeded, "replacement event schedule");
    Check(timeline.TryPeekPending(out var scheduled), "replacement pending event");
    var state = new SimulationState(store); var engine = new SimulationTransactionEngine(new SimulationClock(eventTime, timeline), state, historyCapacity);
    return (timeline, state, engine, scheduled);
}

static (SimulationTimeline timeline, SimulationTransactionEngine engine, CelestialTrajectoryTransactionCreationResult first, CelestialTrajectoryTransactionCreationResult secondStale, TwoBodyTrajectory secondReplacement) CreateSameTimeReplacementScenario(TwoBodyTrajectory initial, SimulationInstant eventTime, TwoBodyTrajectory firstReplacement)
{
    var scenario = CreateReplacementScenario(initial, eventTime, 4, 14, -1);
    Check(scenario.timeline.Schedule(SimulationInstant.Zero, new SimulationEventRequest(new SimulationEventId(15), eventTime, 1, SimulationEventKind.ReplaceTrajectory)).Succeeded, "second same-time schedule");
    Check(scenario.timeline.TryPeekPending(out var firstPending), "first same-time pending");
    var first = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(firstPending, scenario.engine.State, scenario.timeline.Revision, new CelestialBodyId(2), firstReplacement);
    var secondReplacement = firstReplacement with { StateAtEpoch = new CartesianState(firstReplacement.StateAtEpoch.Position, firstReplacement.StateAtEpoch.Velocity * .98d) };
    Check(scenario.timeline.TryGetPending(new SimulationEventId(15), out var secondPending), "second same-time locate");
    var secondStale = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(secondPending, scenario.engine.State, scenario.timeline.Revision, new CelestialBodyId(2), secondReplacement);
    return (scenario.timeline, scenario.engine, first, secondStale, secondReplacement);
}

static ulong ReplacementReplayHash(TwoBodyTrajectory initial, SimulationInstant eventTime, TwoBodyTrajectory replacement)
{
    var scenario = CreateReplacementScenario(initial, eventTime, 2, 20, 0);
    var candidate = CelestialTrajectoryTransactionEvaluator.TryCreateReplacement(scenario.scheduled, scenario.engine.State, scenario.timeline.Revision, new CelestialBodyId(2), replacement).Transaction!.Value;
    var result = scenario.engine.ValidateAndCommit(candidate); Check(result.Committed && result.ProcessedEvent is not null && result.ProcessedEvent.Value.CelestialTransition is not null, "replacement replay commit");
    var history = result.ProcessedEvent!.Value; var transition = history.CelestialTransition!.Value;
    ulong hash = 14695981039346656037UL;
    hash = Mix(hash, CelestialContractHash.Compute(scenario.engine.State.Celestial)); hash = Mix(hash, scenario.engine.State.Revision.Value); hash = Mix(hash, scenario.timeline.Revision.Value); hash = Mix(hash, (ulong)scenario.engine.ProcessedCount);
    hash = Mix(hash, transition.PriorTrajectoryHash); hash = Mix(hash, transition.ReplacementTrajectoryHash); return Mix(hash, (ulong)history.Event.Id.Value);
}

static ReplacementStateSnapshot ReplacementSnapshot(SimulationTransactionEngine engine, SimulationTimeline timeline) => new(engine.State.MarkerValue, engine.State.Revision, CelestialContractHash.Compute(engine.State.Celestial), timeline.Revision, timeline.PendingCount, engine.ProcessedCount, engine.State.Celestial.GetState(1).Trajectory);

static void CelestialImpulseEventTests()
{
    const double mu = 3.986004418e14d; const double radius = 7_000_000d;
    var initialState = new CartesianState(new Double3(radius, 0d, 0d), new Double3(0d, Math.Sqrt(mu / radius), 0d));
    var initial = new TwoBodyTrajectory(new CelestialBodyId(1), SimulationInstant.Zero, initialState, TwoBodyPropagationModel.CartesianTwoBodyV1);
    var eventTime = SimulationInstant.FromWholeSeconds(1_234); var delta = new Double3(0d, 25d, 0d);
    var scenario = CreateImpulseScenario(initial, eventTime, (30UL, 0, delta));
    var expected = RequirePropagation(initialState, SimulationInstant.Zero, eventTime, mu, "impulse event state").State;
    var execution = scenario.engine.AdvanceAndExecuteOneCanonicalGroup(eventTime);
    Check(execution.Reason == SimulationExecutionStopReason.Completed && scenario.engine.ProcessedCount == 1 && scenario.engine.State.Revision.Value == 1, "canonical impulse execution");
    Check(scenario.engine.State.Celestial.TryGetState(new CelestialBodyId(2), out var updatedBody) && updatedBody.Trajectory is not null, "impulse replacement available");
    var updated = updatedBody.Trajectory!.Value;
    CheckVectorNear(updated.StateAtEpoch.Position, expected.Position, PositionTolerance(radius), "impulse position continuity");
    CheckVectorNear(updated.StateAtEpoch.Velocity, expected.Velocity + delta, VelocityTolerance(Math.Sqrt(expected.Velocity.LengthSquared)), "impulse velocity addition");
    Check(updated.Epoch == eventTime && UniversalVariableTwoBodyPropagator.TryEvaluate(updated.StateAtEpoch, updated.Epoch, eventTime + SimulationDuration.FromWholeSeconds(60), mu).Succeeded, "post-impulse propagation");
    Check(scenario.engine.TryGetProcessed(0, out var history) && history.CelestialTransition is { } transition && transition.ImpulseAudit is { } audit && RawVectorEqual(audit.DeltaVelocity, delta), "impulse history audit");

    var sameTime = CreateImpulseScenario(initial, eventTime, (40UL, -1, new Double3(0d, 10d, 0d)), (41UL, 1, new Double3(0d, 15d, 0d)));
    Check(sameTime.engine.AdvanceAndExecuteOneCanonicalGroup(eventTime).Reason == SimulationExecutionStopReason.Completed && sameTime.engine.ProcessedCount == 2 && sameTime.engine.State.Revision.Value == 2, "same-time impulses commit serially");
    Check(sameTime.engine.TryGetProcessed(0, out var first) && sameTime.engine.TryGetProcessed(1, out var second) && first.Event.Priority < second.Event.Priority && first.CelestialTransition!.Value.ImpulseAudit!.Value.DeltaVelocity.Y == 10d && second.CelestialTransition!.Value.ImpulseAudit!.Value.DeltaVelocity.Y == 15d, "same-time impulse audit order");

    var unsupported = CreateImpulseScenario(initial, eventTime, (50UL, 0, new Double3(0d, 20_000d, 0d)));
    Check(unsupported.engine.AdvanceAndExecuteOneCanonicalGroup(eventTime).Reason == SimulationExecutionStopReason.ValidationRejected, "unsupported impulse reaches canonical rejection");
    var before = ReplacementSnapshot(unsupported.engine, unsupported.timeline);
    var unsupportedResult = unsupported.engine.ExecuteCanonicalPendingEvent();
    Check(!unsupportedResult.Committed && unsupportedResult.CelestialImpulseStatus == CelestialImpulseEvaluationStatus.UnsupportedResultingOrbit && before == ReplacementSnapshot(unsupported.engine, unsupported.timeline), "unsupported impulse rejection is atomic");

    var invalidPayload = new SimulationTimeline(1);
    Check(invalidPayload.Schedule(SimulationInstant.Zero, new SimulationEventRequest(new SimulationEventId(51), eventTime, 0, SimulationEventKind.CelestialImpulse)).Status == SimulationScheduleStatus.InvalidPayload, "kind/payload mismatch rejected");
    Check(!SimulationEventRequest.TryCreateCelestialImpulse(new SimulationEventId(52), eventTime, 0, CelestialBodyId.Invalid, delta, out _) && !SimulationEventRequest.TryCreateCelestialImpulse(new SimulationEventId(53), eventTime, 0, new CelestialBodyId(2), Double3.Zero, out _), "invalid payload creation rejected");

    var replayA = ImpulseReplayHash(initial, eventTime); var replayB = ImpulseReplayHash(initial, eventTime); Check(replayA == replayB, "impulse replay hash");
    var allocation = CreateImpulseScenario(initial, eventTime, (60UL, 0, delta)); Check(allocation.timeline.TryPeekPending(out var pending), "allocation impulse pending"); _ = CelestialImpulseEvaluator.TryEvaluate(pending, allocation.engine.State, eventTime, allocation.timeline.Revision);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 100_000; index++) Check(CelestialImpulseEvaluator.TryEvaluate(pending, allocation.engine.State, eventTime, allocation.timeline.Revision).Succeeded, "warm impulse evaluation"); var evaluationAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
    allocationBefore = GC.GetAllocatedBytesForCurrentThread(); var allocationResult = allocation.engine.AdvanceAndExecuteOneCanonicalGroup(eventTime); var commitAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
    Check(allocationResult.Reason == SimulationExecutionStopReason.Completed && evaluationAllocated == 0 && commitAllocated == 0, "impulse paths allocate zero bytes after warmup");
    Console.WriteLine("Celestial Impulse Events"); Console.WriteLine("Impulse scheduling: PASS; Exact event-time propagation: PASS; Delta-v application: PASS"); Console.WriteLine("Authoritative replacement: PASS; Post-impulse propagation: PASS; Same-time impulse ordering: PASS"); Console.WriteLine("Unsupported resulting orbit rejection: PASS; Failure atomicity: PASS; Replay: PASS"); Console.WriteLine($"Allocation: {evaluationAllocated + commitAllocated} bytes; Impulse hash: 0x{replayA:X16}");
}

static (SimulationTimeline timeline, SimulationTransactionEngine engine, SimulationClock clock) CreateImpulseScenario(TwoBodyTrajectory initial, SimulationInstant eventTime, params (ulong Id, int Priority, Double3 DeltaVelocity)[] impulses)
{
    const double mu = 3.986004418e14d; var definitions = new[] { new CelestialBodyDefinition(new CelestialBodyId(1), null, new ReferenceFrameId(1), mu), new CelestialBodyDefinition(new CelestialBodyId(2), new CelestialBodyId(1), new ReferenceFrameId(2), 1d) }; var states = new[] { CelestialBodyState.Root(new CelestialBodyId(1)), CelestialBodyState.Orbiting(new CelestialBodyId(2), initial) };
    Check(CelestialStateStore.TryCreate(definitions, states, out var store, out var status) && store is not null && status == CelestialStateStoreStatus.Success, "impulse store creation"); var timeline = new SimulationTimeline(impulses.Length);
    foreach (var impulse in impulses) { if (!SimulationEventRequest.TryCreateCelestialImpulse(new SimulationEventId(impulse.Id), eventTime, impulse.Priority, new CelestialBodyId(2), impulse.DeltaVelocity, out var request)) continue; Check(timeline.Schedule(SimulationInstant.Zero, request).Succeeded, "impulse schedule"); }
    var clock = new SimulationClock(SimulationInstant.Zero, timeline); return (timeline, new SimulationTransactionEngine(clock, new SimulationState(store), impulses.Length), clock);
}
static ulong ImpulseReplayHash(TwoBodyTrajectory initial, SimulationInstant eventTime)
{
    var scenario = CreateImpulseScenario(initial, eventTime, (70UL, -1, new Double3(0d, 10d, 0d)), (71UL, 0, new Double3(0d, 15d, 0d)), (72UL, 1, new Double3(0d, 2d, 0d))); Check(scenario.engine.AdvanceAndExecuteOneCanonicalGroup(eventTime).Reason == SimulationExecutionStopReason.Completed && scenario.engine.ProcessedCount == 3 && scenario.timeline.PendingCount == 0, "impulse replay group"); ulong hash = Mix(14695981039346656037UL, CelestialContractHash.Compute(scenario.engine.State.Celestial)); for (var index = 0; index < scenario.engine.ProcessedCount; index++) { Check(scenario.engine.TryGetProcessed(index, out var entry) && entry.CelestialTransition is not null && entry.CelestialTransition.Value.ImpulseAudit is not null, "impulse replay history"); var transition = entry.CelestialTransition!.Value; var audit = transition.ImpulseAudit!.Value; hash = Mix(hash, entry.Event.Id.Value); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(audit.DeltaVelocity.Y)); hash = Mix(hash, transition.ReplacementTrajectoryHash); } return hash;
}
static bool RawVectorEqual(in Double3 left, in Double3 right) => BitConverter.DoubleToInt64Bits(left.X) == BitConverter.DoubleToInt64Bits(right.X) && BitConverter.DoubleToInt64Bits(left.Y) == BitConverter.DoubleToInt64Bits(right.Y) && BitConverter.DoubleToInt64Bits(left.Z) == BitConverter.DoubleToInt64Bits(right.Z);

static void TimelineTopologyTests()
{
    var now = new SimulationInstant(100);
    var timeline = new SimulationTimeline(32);
    var first = timeline.Schedule(now, Request(1, 100, 0));
    Check(first.Succeeded && first.ScheduledEvent.Header.Sequence.Value == 1 && timeline.Revision.Value == 1, "first sequence and revision");
    var second = timeline.Schedule(now, Request(2, 101, 0));
    Check(second.Succeeded && second.ScheduledEvent.Header.Sequence.Value == 2, "monotonic sequence");
    var revision = timeline.Revision;
    Check(timeline.Schedule(now, Request(0, 101, 0)).Status == SimulationScheduleStatus.InvalidId, "zero ID rejected");
    Check(timeline.Schedule(now, new SimulationEventRequest(new SimulationEventId(3), now, 0, (SimulationEventKind)255)).Status == SimulationScheduleStatus.InvalidKind, "kind rejected");
    Check(timeline.Schedule(now, Request(3, 99, 0)).Status == SimulationScheduleStatus.PastTime, "past event rejected");
    Check(timeline.Schedule(now, Request(1, 102, 0)).Status == SimulationScheduleStatus.DuplicateId, "duplicate ID rejected");
    Check(timeline.Revision == revision && timeline.ValidateInvariants(), "failed schedule leaves topology unchanged");

    var middle = timeline.Schedule(now, Request(4, 102, 0));
    var leaf = timeline.Schedule(now, Request(5, 103, 0));
    Check(middle.Succeeded && leaf.Succeeded && timeline.ValidateInvariants(), "insert heap entries");
    Check(timeline.TryPeekPending(out var minimumPending) && minimumPending.Header.Id == new SimulationEventId(1), "heap root is canonical minimum");
    Check(timeline.Cancel(new SimulationEventId(1)).Succeeded && timeline.ValidateInvariants(), "cancel root");
    Check(timeline.Cancel(new SimulationEventId(4)).Succeeded && timeline.ValidateInvariants(), "cancel middle");
    Check(timeline.Cancel(new SimulationEventId(5)).Succeeded && timeline.ValidateInvariants(), "cancel leaf");
    Check(timeline.Cancel(new SimulationEventId(5)).Status == SimulationCancelStatus.NotPending && timeline.IsIdReserved(new SimulationEventId(5)), "cancelled ID remains reserved");

    var beforeReplacement = timeline.Revision;
    var replacement = timeline.Replace(now, new SimulationEventId(2), Request(6, 104, -1));
    Check(replacement.Succeeded && replacement.ScheduledEvent.Header.Sequence.Value == 5 && timeline.Revision.Value == beforeReplacement.Value + 1, "atomic replacement");
    Check(timeline.IsIdReserved(new SimulationEventId(2)) && timeline.IsIdReserved(new SimulationEventId(6)) && timeline.ValidateInvariants(), "replacement reserves both IDs");
    revision = timeline.Revision;
    Check(timeline.Replace(now, new SimulationEventId(2), Request(7, 104, 0)).Status == SimulationScheduleStatus.ReplacementTargetNotPending, "missing replacement target");
    Check(timeline.Replace(now, new SimulationEventId(6), Request(6, 104, 0)).Status == SimulationScheduleStatus.DuplicateId, "replacement duplicate new ID");
    Check(timeline.Revision == revision && timeline.ValidateInvariants(), "failed replacement unchanged");

    var overflow = new SimulationTimeline(1, ulong.MaxValue, TimelineRevision.Zero);
    Check(overflow.Schedule(SimulationInstant.Zero, Request(100, 0, 0)).Status == SimulationScheduleStatus.SequenceOverflow && overflow.PendingCount == 0 && overflow.Revision == TimelineRevision.Zero, "sequence overflow controlled");
    var revisionOverflow = new SimulationTimeline(1, 1, new TimelineRevision(ulong.MaxValue));
    Check(revisionOverflow.Schedule(SimulationInstant.Zero, Request(101, 0, 0)).Status == SimulationScheduleStatus.RevisionOverflow, "revision overflow controlled");

    var expected = CanonicalHeaders();
    for (var pass = 0; pass < 8; pass++)
    {
        var permuted = (SimulationEventRequest[])expected.Clone(); ShuffleRequests(permuted, (ulong)(pass + 41));
        var orderedTimeline = new SimulationTimeline(permuted.Length);
        foreach (var request in permuted) Check(orderedTimeline.Schedule(new SimulationInstant(long.MinValue), request).Succeeded, "permuted schedule");
        var actual = new ScheduledSimulationEvent[orderedTimeline.PendingCount]; orderedTimeline.CopyPending(actual);
        Array.Sort(actual, static (left, right) => SimulationEventHeaderComparer.Compare(left.Header, right.Header));
        for (var index = 0; index < actual.Length; index++) Check(actual[index].Header.Time == expected[index].Time && actual[index].Header.Priority == expected[index].Priority && actual[index].Header.Id == expected[index].Id, "canonical heap order");
        Check(orderedTimeline.ValidateInvariants(), "permuted heap invariants");
    }

    var allocatedTimeline = new SimulationTimeline(20_000);
    for (ulong id = 1; id <= 100; id++) Check(allocatedTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id, 0)).Succeeded, "allocation warmup");
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (ulong id = 101; id <= 10_000; id++) { Check(allocatedTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id, 0)).Succeeded, "allocation schedule"); Check(allocatedTimeline.Cancel(new SimulationEventId(id)).Succeeded, "allocation cancel"); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && allocatedTimeline.ValidateInvariants(), "preallocated timeline operations allocate zero bytes");

    var hash = TimelineHash(expected); Check(TimelineHash(CanonicalHeaders()) == hash, "deterministic timeline stress hash");
    Console.WriteLine($"Deterministic timeline stress hash: 0x{hash:X16}");
    var mixedHash = MixedTimelineHash(); Check(MixedTimelineHash() == mixedHash, "fixed-seed mixed timeline operations");
    Console.WriteLine($"Deterministic mixed-timeline stress hash: 0x{mixedHash:X16}");
}

static void ClockTests()
{
    var timeline = new SimulationTimeline(16);
    var clock = new SimulationClock(new SimulationInstant(-10), timeline);
    Check(clock.CurrentTime.Ticks == -10 && ReferenceEquals(clock.Timeline, timeline) && clock.Rate == SimulationRate.One && clock.Settings.MaximumEventsPerAdvance == 10_000 && !clock.IsPaused && clock.RateRemainder == 0, "clock construction");
    Throws<ArgumentOutOfRangeException>(() => _ = new SimulationClockSettings(0));

    var revision = timeline.Revision; clock.Pause(); clock.Pause(); Check(clock.IsPaused && clock.CurrentTime.Ticks == -10 && timeline.Revision == revision, "pause idempotent");
    var pausedAdvance = clock.AdvanceTo(new SimulationInstant(10)); Check(pausedAdvance.Reason == SimulationAdvanceStopReason.ReachedTarget && clock.CurrentTime.Ticks == 10, "explicit advance while paused");
    clock.Resume(); clock.Resume(); Check(!clock.IsPaused && timeline.Revision == revision, "resume idempotent");
    Check(clock.TrySetRate(SimulationRate.Two) && clock.Rate == SimulationRate.Two && clock.RateRemainder == 0 && clock.CurrentTime.Ticks == 10 && timeline.Revision == revision, "rate change");
    Check(!clock.TrySetRate(new SimulationRate(4, 2)), "equivalent normalized rate is no-op");

    var noEvents = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(noEvents.AdvanceTo(SimulationInstant.Zero).Reason == SimulationAdvanceStopReason.ReachedTarget, "equal empty target");
    Check(noEvents.AdvanceTo(new SimulationInstant(long.MaxValue)).Reason == SimulationAdvanceStopReason.ReachedTarget && noEvents.CurrentTime.Ticks == long.MaxValue, "huge empty jump");
    Check(noEvents.AdvanceTo(SimulationInstant.Zero).Reason == SimulationAdvanceStopReason.TargetBeforeCurrent && noEvents.CurrentTime.Ticks == long.MaxValue, "target before current");
    Check(noEvents.AdvanceUntilNextEvent().Reason == SimulationAdvanceStopReason.NoPendingEvent, "no pending event");

    var eventTimeline = new SimulationTimeline(8);
    Check(eventTimeline.Schedule(SimulationInstant.Zero, Request(1, 20, 0)).Succeeded, "schedule future boundary");
    Check(eventTimeline.Schedule(SimulationInstant.Zero, Request(2, 20, -1)).Succeeded, "schedule canonical priority boundary");
    var eventClock = new SimulationClock(SimulationInstant.Zero, eventTimeline);
    Check(eventClock.AdvanceTo(new SimulationInstant(10)).Reason == SimulationAdvanceStopReason.ReachedTarget && eventClock.CurrentTime.Ticks == 10, "event after target");
    var boundary = eventClock.AdvanceTo(new SimulationInstant(20));
    Check(boundary.Reason == SimulationAdvanceStopReason.ReachedEventBoundary && boundary.BoundaryEvent!.Value.Id == new SimulationEventId(2) && eventClock.CurrentTime.Ticks == 20, "canonical boundary at target");
    Check(eventTimeline.PendingCount == 2 && eventTimeline.Revision.Value == 2, "boundary remains pending");
    var repeated = eventClock.AdvanceTo(new SimulationInstant(100)); Check(repeated.ReachedBoundary && repeated.BoundaryEvent!.Value.Id == new SimulationEventId(2), "repeated boundary remains stable");
    eventClock.Pause(); var until = eventClock.AdvanceUntilNextEvent(); Check(until.ReachedBoundary && until.BoundaryEvent!.Value.Id == new SimulationEventId(2), "until next works while paused");
    var reentrant = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(reentrant.AdvanceToWhileGuardedForTest(new SimulationInstant(1)).Reason == SimulationAdvanceStopReason.ReentrantAdvance, "nested AdvanceTo rejected");
    Check(reentrant.AdvanceUntilNextEventWhileGuardedForTest().Reason == SimulationAdvanceStopReason.ReentrantAdvance, "nested AdvanceUntilNextEvent rejected");
    Check(reentrant.AdvanceTo(new SimulationInstant(1)).Reason == SimulationAdvanceStopReason.ReachedTarget, "advancement guard restored");

    var allocationTimeline = new SimulationTimeline(2); Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(50, 5, 0)).Succeeded, "allocation boundary setup");
    var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); _ = allocationClock.AdvanceTo(new SimulationInstant(1)); _ = allocationClock.AdvanceUntilNextEvent(); allocationClock.Pause(); allocationClock.Resume(); _ = allocationClock.TrySetRate(SimulationRate.One);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) { _ = allocationClock.AdvanceTo(new SimulationInstant(5)); _ = allocationClock.AdvanceUntilNextEvent(); allocationClock.Pause(); allocationClock.Resume(); _ = allocationClock.TrySetRate(SimulationRate.One); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "clock steady-state operations allocate zero bytes");

    var hash = ClockHash(); Check(ClockHash() == hash, "deterministic clock script");
    Console.WriteLine($"Deterministic clock stress hash: 0x{hash:X16}");
}

static void HostDurationTests()
{
    var one = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    var accepted = one.AdvanceByHostDuration(new SimulationDuration(10));
    Check(accepted.Reason == SimulationHostAdvanceStopReason.Accepted && accepted.DerivedSimulationDuration.Ticks == 10 && one.PendingSimulationDebt.Ticks == 10 && one.RateRemainder == 0 && one.CurrentTime == SimulationInstant.Zero, "one-to-one host conversion");

    var half = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Half);
    Check(half.AdvanceByHostDuration(new SimulationDuration(1)).DerivedSimulationDuration.Ticks == 0 && half.RateRemainder == 1, "half fractional remainder");
    Check(half.AdvanceByHostDuration(new SimulationDuration(1)).DerivedSimulationDuration.Ticks == 1 && half.PendingSimulationDebt.Ticks == 1 && half.RateRemainder == 0, "half retained remainder");
    var quarter = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Quarter);
    for (var index = 0; index < 4; index++) _ = quarter.AdvanceByHostDuration(new SimulationDuration(1));
    Check(quarter.PendingSimulationDebt.Ticks == 1 && quarter.RateRemainder == 0, "quarter rate");
    var twice = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Two);
    Check(twice.AdvanceByHostDuration(new SimulationDuration(3)).DerivedSimulationDuration.Ticks == 6 && twice.PendingSimulationDebt.Ticks == 6, "accelerated rate");

    var split = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    var combined = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    for (var index = 0; index < 10_000; index++) _ = split.AdvanceByHostDuration(new SimulationDuration(13));
    _ = combined.AdvanceByHostDuration(new SimulationDuration(130_000));
    Check(split.PendingSimulationDebt == combined.PendingSimulationDebt && split.RateRemainder == combined.RateRemainder, "split and combined exact composition");

    var rateChange = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Half);
    _ = rateChange.AdvanceByHostDuration(new SimulationDuration(1));
    Check(!rateChange.TrySetRate(new SimulationRate(2, 4)) && rateChange.RateRemainder == 1, "equivalent rate complete no-op");
    _ = rateChange.AdvanceByHostDuration(new SimulationDuration(1));
    var debtBeforeChange = rateChange.PendingSimulationDebt;
    Check(rateChange.TrySetRate(SimulationRate.Two) && rateChange.RateRemainder == 0 && rateChange.PendingSimulationDebt == debtBeforeChange, "changed rate resets only remainder");

    rateChange.Pause(); var pausedDebt = rateChange.PendingSimulationDebt; var pausedRemainder = rateChange.RateRemainder;
    Check(rateChange.AdvanceByHostDuration(new SimulationDuration(100)).Reason == SimulationHostAdvanceStopReason.Paused && rateChange.PendingSimulationDebt == pausedDebt && rateChange.RateRemainder == pausedRemainder, "pause preserves debt and remainder");
    rateChange.Resume();
    var zeroDebt = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(zeroDebt.AdvanceByHostDuration(SimulationDuration.Zero).Reason == SimulationHostAdvanceStopReason.NoWork && zeroDebt.PendingSimulationDebt.IsZero, "zero duration with no debt");
    Check(rateChange.AdvanceByHostDuration(SimulationDuration.Zero).Reason == SimulationHostAdvanceStopReason.NoWork && rateChange.PendingSimulationDebt == pausedDebt, "zero duration retains debt in 4A");

    var invalid = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    Check(invalid.AdvanceByHostDuration(new SimulationDuration(-1)).Reason == SimulationHostAdvanceStopReason.InvalidHostDuration && invalid.PendingSimulationDebt.IsZero && invalid.RateRemainder == 0, "negative host duration rejected");
    var scaleOverflow = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), SimulationRate.Two);
    Check(scaleOverflow.AdvanceByHostDuration(new SimulationDuration(long.MaxValue)).Reason == SimulationHostAdvanceStopReason.ArithmeticOverflow && scaleOverflow.PendingSimulationDebt.IsZero && scaleOverflow.RateRemainder == 0, "scaling overflow no partial state");
    var debtOverflow = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    _ = debtOverflow.AdvanceByHostDuration(new SimulationDuration(1)); Check(debtOverflow.TrySetRate(new SimulationRate(long.MaxValue, 1)), "debt overflow rate setup");
    Check(debtOverflow.AdvanceByHostDuration(new SimulationDuration(1)).Reason == SimulationHostAdvanceStopReason.ArithmeticOverflow && debtOverflow.PendingSimulationDebt.Ticks == 1 && debtOverflow.RateRemainder == 0, "debt overflow no partial state");

    var warm = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7)); _ = warm.AdvanceByHostDuration(new SimulationDuration(1));
    var allocation = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline(), new SimulationRate(5, 7));
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100_000; index++) _ = allocation.AdvanceByHostDuration(new SimulationDuration(13));
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore, "host conversion and debt accumulation allocate zero bytes");

    var hash = HostDurationHash(); Check(HostDurationHash() == hash, "deterministic host conversion replay hash");
    Console.WriteLine($"Deterministic host-duration conversion hash: 0x{hash:X16}");
}

static void HostDurationDebtServiceTests()
{
    var emptyClock = new SimulationClock(SimulationInstant.Zero, new SimulationTimeline());
    var emptyEngine = new SimulationTransactionEngine(emptyClock, new SimulationState());
    Check(emptyEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.NoDebt, "no debt service is controlled");
    _ = emptyClock.AdvanceByHostDuration(new SimulationDuration(20));
    var coast = emptyEngine.ServicePendingHostDurationDebt();
    Check(coast.Reason == SimulationDebtServiceStopReason.Completed && coast.TargetTime.Ticks == 20 && coast.ReachedTime.Ticks == 20 && coast.DebtAfter.IsZero && coast.ProcessedEventCount == 0 && coast.ExecutedGroupCount == 0 && coast.LastGroupStopReason is null, "debt coast diagnostics");

    var timeline = new SimulationTimeline(4);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 5, 1)).Succeeded, "debt schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 10, 0)).Succeeded, "debt schedule B");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 4);
    _ = clock.AdvanceByHostDuration(new SimulationDuration(15));
    var executed = engine.ServicePendingHostDurationDebt();
    Check(executed.Reason == SimulationDebtServiceStopReason.Completed && executed.TargetTime.Ticks == 15 && clock.CurrentTime.Ticks == 15 && clock.PendingSimulationDebt.IsZero && executed.ProcessedEventCount == 2 && executed.ExecutedGroupCount == 2 && executed.LastGroupStopReason == SimulationCanonicalGroupStopReason.Completed && engine.ProcessedCount == 2, "debt group diagnostics");

    var capTimeline = new SimulationTimeline(4);
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(10, 5, 0)).Succeeded, "budget schedule A");
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(11, 5, 1)).Succeeded, "budget schedule B");
    Check(capTimeline.Schedule(SimulationInstant.Zero, Request(12, 10, 0)).Succeeded, "budget schedule C");
    var capClock = new SimulationClock(SimulationInstant.Zero, capTimeline, settings: new SimulationClockSettings(2));
    var capEngine = new SimulationTransactionEngine(capClock, new SimulationState(), 4);
    _ = capClock.AdvanceByHostDuration(new SimulationDuration(20));
    var capped = capEngine.ServicePendingHostDurationDebt();
    Check(capped.Reason == SimulationDebtServiceStopReason.EventLimitReached && capped.TargetTime.Ticks == 20 && capped.ProcessedEventCount == 2 && capped.ExecutedGroupCount == 1 && capped.LastGroupStopReason == SimulationCanonicalGroupStopReason.Completed && capClock.CurrentTime.Ticks == 10 && capClock.PendingSimulationDebt.Ticks == 10 && capTimeline.PendingCount == 1, "one call-wide budget retains accurate debt");
    Check(capEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed && capClock.CurrentTime.Ticks == 20 && capClock.PendingSimulationDebt.IsZero && capEngine.ProcessedCount == 3, "next service resumes retained debt");

    var rejectionTimeline = new SimulationTimeline(2);
    Check(rejectionTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(20, 5, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "debt rejection schedule");
    var rejectionClock = new SimulationClock(SimulationInstant.Zero, rejectionTimeline);
    var rejectionEngine = new SimulationTransactionEngine(rejectionClock, new SimulationState(), 2);
    _ = rejectionClock.AdvanceByHostDuration(new SimulationDuration(20));
    var rejected = rejectionEngine.ServicePendingHostDurationDebt();
    Check(rejected.Reason == SimulationDebtServiceStopReason.ValidationRejected && rejected.TargetTime.Ticks == 20 && rejected.ProcessedEventCount == 0 && rejected.ExecutedGroupCount == 1 && rejected.LastGroupStopReason == SimulationCanonicalGroupStopReason.ValidationRejected && rejectionClock.CurrentTime.Ticks == 5 && rejectionClock.PendingSimulationDebt.Ticks == 15 && rejectionEngine.ProcessedCount == 0 && rejectionTimeline.PendingCount == 1, "validation rejection retains untraversed debt and authority");

    var laterTimeline = new SimulationTimeline(1); Check(laterTimeline.Schedule(SimulationInstant.Zero, Request(30, 25, 0)).Succeeded, "later boundary schedule");
    var laterClock = new SimulationClock(SimulationInstant.Zero, laterTimeline); var laterEngine = new SimulationTransactionEngine(laterClock, new SimulationState(), 1);
    _ = laterClock.AdvanceByHostDuration(new SimulationDuration(20));
    Check(laterEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed && laterClock.CurrentTime.Ticks == 20 && laterTimeline.PendingCount == 1, "debt never executes beyond requested duration");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(90, 1, 0)).Succeeded, "debt allocation warmup schedule");
    var warmClock = new SimulationClock(SimulationInstant.Zero, warmTimeline); _ = warmClock.AdvanceByHostDuration(new SimulationDuration(2)); _ = new SimulationTransactionEngine(warmClock, new SimulationState(), 1).ServicePendingHostDurationDebt();
    var allocationTimeline = new SimulationTimeline(1_000);
    for (ulong id = 1; id <= 1_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 1, (int)id)).Succeeded, "debt allocation schedule");
    var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); var allocationEngine = new SimulationTransactionEngine(allocationClock, new SimulationState(), 1_000); _ = allocationClock.AdvanceByHostDuration(new SimulationDuration(2));
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread(); var allocation = allocationEngine.ServicePendingHostDurationDebt();
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocation.Reason == SimulationDebtServiceStopReason.Completed && allocation.ProcessedEventCount == 1_000, "preallocated debt servicing allocates zero bytes");

    const int stressEventCount = 5_000;
    var stressTimeline = new SimulationTimeline(stressEventCount);
    for (ulong id = 1; id <= stressEventCount; id++) Check(stressTimeline.Schedule(SimulationInstant.Zero, Request(id, (long)id * 10, 0)).Succeeded, "long-run schedule");
    var stressClock = new SimulationClock(SimulationInstant.Zero, stressTimeline, settings: new SimulationClockSettings(16));
    var stressEngine = new SimulationTransactionEngine(stressClock, new SimulationState(), stressEventCount);
    _ = stressClock.AdvanceByHostDuration(new SimulationDuration(100)); _ = stressEngine.ServicePendingHostDurationDebt();
    var stressBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var cycle = 1; cycle < 500; cycle++) { _ = stressClock.AdvanceByHostDuration(new SimulationDuration(100)); Check(stressEngine.ServicePendingHostDurationDebt().Reason == SimulationDebtServiceStopReason.Completed, "long-run debt service"); }
    Check(GC.GetAllocatedBytesForCurrentThread() == stressBefore && stressClock.CurrentTime.Ticks == 50_000 && stressClock.PendingSimulationDebt.IsZero && stressEngine.ProcessedCount == stressEventCount && stressTimeline.PendingCount == 0, "repeated long-duration servicing is allocation-free");

    var hash = HostDurationDebtServiceHash(); Check(HostDurationDebtServiceHash() == hash, "deterministic host-duration orchestration hash");
    Console.WriteLine($"Deterministic host-duration orchestration hash: 0x{hash:X16}");
    var longRunHash = HostDurationLongRunHash(); Check(HostDurationLongRunHash() == longRunHash, "deterministic long-duration replay hash");
    Console.WriteLine($"Deterministic long-duration host advancement hash: 0x{longRunHash:X16}");
}

static void TransactionTests()
{
    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 10, 0)).Succeeded, "transaction schedule");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 10, -1)).Succeeded, "canonical transaction schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    Check(clock.AdvanceTo(new SimulationInstant(10)).ReachedBoundary, "reach transaction boundary");
    var state = new SimulationState();
    var engine = new SimulationTransactionEngine(clock, state, 8);
    var transaction = engine.EvaluateNext();
    Check(engine.State.MarkerValue == 0 && transaction.ProposedMarkerValue == 1 && transaction.Event.Id == new SimulationEventId(2), "evaluation is immutable");
    var beforeTimeline = timeline.Revision; var result = engine.ExecuteCanonicalPendingEvent();
    Check(result.Committed && result.ProcessedEvent!.Value.Event.Id == new SimulationEventId(2), "one canonical event committed");
    Check(engine.State.MarkerValue == 1 && engine.State.Revision.Value == 1 && timeline.PendingCount == 1 && timeline.Revision.Value == beforeTimeline.Value + 1 && clock.CurrentTime.Ticks == 10, "atomic state and timeline commit");
    Check(engine.ProcessedCount == 1 && engine.TryGetProcessed(0, out var processed) && processed.ExecutionTime.Ticks == 10 && processed.TimelineRevisionBefore == beforeTimeline && processed.TimelineRevisionAfter == timeline.Revision && processed.StateRevisionBefore == StateRevision.Zero && processed.StateRevisionAfter == engine.State.Revision, "append-only processed history");

    Check(timeline.Schedule(clock.CurrentTime, Request(3, 10, 0)).Succeeded, "failure schedule");
    var invalid = engine.EvaluateNext() with { ExpectedTimelineRevision = TimelineRevision.Zero };
    var stateBefore = engine.State; beforeTimeline = timeline.Revision; var historyBefore = engine.ProcessedCount;
    var failed = engine.ValidateAndCommit(invalid);
    Check(!failed.Committed && failed.Validation.Status == SimulationTransactionValidationStatus.TimelineRevisionMismatch, "controlled validation failure");
    Check(engine.State == stateBefore && timeline.Revision == beforeTimeline && timeline.PendingCount == 2 && engine.ProcessedCount == historyBefore && clock.CurrentTime.Ticks == 10, "failed validation leaves all authority unchanged");

    var allocationTimeline = new SimulationTimeline(5_000); var allocationClock = new SimulationClock(SimulationInstant.Zero, allocationTimeline); var allocationEngine = new SimulationTransactionEngine(allocationClock, new SimulationState(), 5_000);
    for (ulong id = 1; id <= 5_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, 0)).Succeeded, "allocation transaction schedule");
    _ = allocationEngine.EvaluateNext();
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 5_000; index++) Check(allocationEngine.ExecuteCanonicalPendingEvent().Committed, "allocation transaction commit");
    Check(GC.GetAllocatedBytesForCurrentThread() == before && allocationEngine.ProcessedCount == 5_000, "preallocated transaction execution allocates zero bytes");

    var hash = TransactionHash(); Check(TransactionHash() == hash, "deterministic transaction replay hash");
    Console.WriteLine($"Deterministic transaction replay hash: 0x{hash:X16}");
}

static void CanonicalGroupTests()
{
    var emptyEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, new SimulationTimeline()), new SimulationState());
    Check(emptyEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.NoPendingEvent, "empty canonical group");

    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 0, 1)).Succeeded, "group schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(2, 0, -1)).Succeeded, "group schedule B");
    Check(timeline.Schedule(SimulationInstant.Zero, RequestWithKind(3, 0, 0, SimulationEventKind.NoOpMarker)).Succeeded, "group no-op schedule");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(4, 1, 0)).Succeeded, "later group schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 8);
    var group = engine.ExecuteCanonicalGroup();
    Check(group.Reason == SimulationCanonicalGroupStopReason.Completed && group.IsComplete && group.GroupTime == SimulationInstant.Zero && group.ProcessedEventCount == 3 && group.PendingEvent!.Value.Id == new SimulationEventId(4), "canonical group completion");
    Check(engine.State.MarkerValue == 2 && engine.State.Revision.Value == 2 && timeline.PendingCount == 1 && timeline.Revision.Value == 7 && clock.CurrentTime == SimulationInstant.Zero, "per-event revisions and later pending event");
    Check(engine.TryGetProcessed(0, out var first) && engine.TryGetProcessed(1, out var second) && engine.TryGetProcessed(2, out var third) && first.Event.Id == new SimulationEventId(2) && second.Event.Id == new SimulationEventId(3) && third.Event.Id == new SimulationEventId(1), "canonical history ordering");

    var capTimeline = new SimulationTimeline(4);
    for (ulong id = 10; id <= 12; id++) Check(capTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, (int)id)).Succeeded, "cap schedule");
    var capEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, capTimeline, settings: new SimulationClockSettings(2)), new SimulationState(), 4);
    var capped = capEngine.ExecuteCanonicalGroup();
    Check(capped.Reason == SimulationCanonicalGroupStopReason.EventLimitReached && capped.ProcessedEventCount == 2 && capTimeline.PendingCount == 1 && capped.PendingEvent!.Value.Id == new SimulationEventId(12), "event cap preserves pending order");
    Check(capEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.Completed && capEngine.ProcessedCount == 3, "group resumes at same boundary");

    var failureTimeline = new SimulationTimeline(4);
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(20, 0, -1)).Succeeded, "failure marker schedule");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(21, 0, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "failure invalid schedule");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(22, 0, 1)).Succeeded, "failure later schedule");
    var failureEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, failureTimeline), new SimulationState(), 4);
    var failed = failureEngine.ExecuteCanonicalGroup();
    Check(failed.Reason == SimulationCanonicalGroupStopReason.ValidationRejected && failed.ProcessedEventCount == 1 && failed.PendingEvent!.Value.Id == new SimulationEventId(21), "partial group rejection");
    Check(failureEngine.State.MarkerValue == 1 && failureEngine.ProcessedCount == 1 && failureTimeline.PendingCount == 2, "earlier commit retained and later events pending");
    Check(failureEngine.ExecuteCanonicalGroup().Reason == SimulationCanonicalGroupStopReason.ValidationRejected, "rejected group remains resumable without loss");
    Check(failureEngine.ExecuteCanonicalGroupWhileGuardedForTest().Reason == SimulationCanonicalGroupStopReason.ReentrantExecution, "reentrant group rejected");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(99, 0, 0)).Succeeded, "group allocation warmup schedule");
    _ = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, warmTimeline), new SimulationState(), 1).ExecuteCanonicalGroup();
    var allocationTimeline = new SimulationTimeline(5_000);
    for (ulong id = 1; id <= 5_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 0, (int)id)).Succeeded, "group allocation schedule");
    var allocationEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, allocationTimeline), new SimulationState(), 5_000);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var allocationResult = allocationEngine.ExecuteCanonicalGroup();
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocationResult.IsComplete && allocationEngine.ProcessedCount == 5_000, "preallocated canonical group execution allocates zero bytes");

    var hash = CanonicalGroupHash(); Check(CanonicalGroupHash() == hash, "canonical group permutation replay hash");
    Console.WriteLine($"Deterministic canonical-group replay hash: 0x{hash:X16}");
}

static void ClockExecutionTests()
{
    var timeline = new SimulationTimeline(8);
    Check(timeline.Schedule(SimulationInstant.Zero, Request(1, 10, -1)).Succeeded, "orchestration schedule A");
    Check(timeline.Schedule(SimulationInstant.Zero, RequestWithKind(2, 10, 0, SimulationEventKind.NoOpMarker)).Succeeded, "orchestration schedule no-op");
    Check(timeline.Schedule(SimulationInstant.Zero, Request(3, 30, 0)).Succeeded, "orchestration later schedule");
    var clock = new SimulationClock(SimulationInstant.Zero, timeline);
    var engine = new SimulationTransactionEngine(clock, new SimulationState(), 8);
    var first = engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(20));
    Check(first.Reason == SimulationExecutionStopReason.Completed && first.InitialAdvanceReason == SimulationAdvanceStopReason.ReachedEventBoundary && first.ContinuationAdvanceReason == SimulationAdvanceStopReason.ReachedTarget && first.Group!.Value.ProcessedEventCount == 2 && first.ReachedTime.Ticks == 20, "coast execute and resume");
    Check(engine.State.MarkerValue == 1 && engine.State.Revision.Value == 1 && engine.ProcessedCount == 2 && timeline.PendingCount == 1 && timeline.TryPeekPending(out var later) && later.Header.Id == new SimulationEventId(3), "later timestamp remains pending");
    var second = engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(100));
    Check(second.Reason == SimulationExecutionStopReason.Completed && second.Group!.Value.ProcessedEventCount == 1 && clock.CurrentTime.Ticks == 100 && timeline.PendingCount == 0, "subsequent boundary executes once");
    Check(engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(101)).Reason == SimulationExecutionStopReason.ReachedTarget, "empty coast diagnostics");

    var failureTimeline = new SimulationTimeline(4);
    Check(failureTimeline.Schedule(SimulationInstant.Zero, Request(10, 5, -1)).Succeeded, "orchestration failure marker");
    Check(failureTimeline.Schedule(SimulationInstant.Zero, RequestWithKind(11, 5, 0, SimulationEventKind.ReplaceTrajectory)).Succeeded, "orchestration failure invalid");
    var failureClock = new SimulationClock(SimulationInstant.Zero, failureTimeline);
    var failureEngine = new SimulationTransactionEngine(failureClock, new SimulationState(), 4);
    var failure = failureEngine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(50));
    Check(failure.Reason == SimulationExecutionStopReason.ValidationRejected && failure.Group!.Value.ProcessedEventCount == 1 && failure.ReachedTime.Ticks == 5, "validation rejection diagnostics");
    Check(failureEngine.State.MarkerValue == 1 && failureEngine.ProcessedCount == 1 && failureTimeline.PendingCount == 1 && failureTimeline.TryPeekPending(out var rejected) && rejected.Header.Id == new SimulationEventId(11), "rejection preserves failing authority");
    Check(failureEngine.AdvanceAndExecuteOneCanonicalGroupWhileGuardedForTest(new SimulationInstant(50)).Reason == SimulationExecutionStopReason.ReentrantExecution, "orchestration reentrancy rejected");

    var warmTimeline = new SimulationTimeline(1); Check(warmTimeline.Schedule(SimulationInstant.Zero, Request(90, 1, 0)).Succeeded, "orchestration allocation warmup schedule");
    _ = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, warmTimeline), new SimulationState(), 1).AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(2));
    var allocationTimeline = new SimulationTimeline(1_000);
    for (ulong id = 1; id <= 1_000; id++) Check(allocationTimeline.Schedule(SimulationInstant.Zero, Request(id, 1, (int)id)).Succeeded, "orchestration allocation schedule");
    var allocationEngine = new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero, allocationTimeline), new SimulationState(), 1_000);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var allocation = allocationEngine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(2));
    Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore && allocation.Reason == SimulationExecutionStopReason.Completed && allocationEngine.ProcessedCount == 1_000, "preallocated orchestration allocates zero bytes");

    var hash = ClockExecutionHash(); Check(ClockExecutionHash() == hash, "deterministic orchestration hash");
    Console.WriteLine($"Deterministic clock-orchestration hash: 0x{hash:X16}");
}

static SimulationEventHeader Header(ulong id, long time, int priority, ulong sequence) => new(new SimulationEventId(id), new SimulationInstant(time), priority, new SimulationEventSequence(sequence), SimulationEventKind.Marker);
static SimulationEventRequest Request(ulong id, long time, int priority) => new(new SimulationEventId(id), new SimulationInstant(time), priority, SimulationEventKind.Marker);
static SimulationEventRequest RequestWithKind(ulong id, long time, int priority, SimulationEventKind kind) => new(new SimulationEventId(id), new SimulationInstant(time), priority, kind);
static SimulationEventRequest[] CanonicalHeaders(){var random=new FixedRandom(0xD6E8FEB86659FD93);var values=new SimulationEventRequest[1024];for(var index=0;index<values.Length;index++){values[index]=Request((ulong)index+1,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%2001)-1000);}Array.Sort(values,static(left,right)=>{var time=left.Time.CompareTo(right.Time);if(time!=0)return time;var priority=left.Priority.CompareTo(right.Priority);return priority!=0?priority:left.Id.CompareTo(right.Id);});return values;}
static void ShuffleRequests(SimulationEventRequest[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong TimelineHash(ReadOnlySpan<SimulationEventRequest> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong ClockHash(){var timeline=new SimulationTimeline(4);Check(timeline.Schedule(SimulationInstant.Zero,Request(10,30,0)).Succeeded,"clock hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(11,10,0)).Succeeded,"clock hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var first=clock.AdvanceTo(new SimulationInstant(100));var second=clock.AdvanceUntilNextEvent();ulong hash=14695981039346656037;hash=Mix(hash,(ulong)first.ReachedTime.Ticks);hash=Mix(hash,first.BoundaryEvent!.Value.Id.Value);hash=Mix(hash,(ulong)second.ReachedTime.Ticks);return Mix(hash,second.BoundaryEvent!.Value.Id.Value);}
static ulong TransactionHash(){var timeline=new SimulationTimeline(32);for(ulong id=1;id<=32;id++)Check(timeline.Schedule(SimulationInstant.Zero,Request(id,0,(int)(id%3))).Succeeded,"hash transaction schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var engine=new SimulationTransactionEngine(clock,new SimulationState(),32);ulong hash=14695981039346656037;while(timeline.PendingCount!=0){var result=engine.ExecuteCanonicalPendingEvent();Check(result.Committed,"hash transaction commit");var entry=result.ProcessedEvent!.Value;hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);hash=Mix(hash,entry.TimelineRevisionAfter.Value);}return hash;}
static ulong CanonicalGroupHash(){var requests=new[]{Request(31,0,2),Request(32,0,-1),RequestWithKind(33,0,0,SimulationEventKind.NoOpMarker),Request(34,0,1)};ulong expected=0;for(var pass=0;pass<8;pass++){var shuffled=(SimulationEventRequest[])requests.Clone();ShuffleRequests(shuffled,(ulong)(pass+101));var timeline=new SimulationTimeline(4);foreach(var request in shuffled)Check(timeline.Schedule(SimulationInstant.Zero,request).Succeeded,"group permutation schedule");var engine=new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero,timeline),new SimulationState(),4);Check(engine.ExecuteCanonicalGroup().IsComplete,"group permutation execute");ulong hash=14695981039346656037;for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"group permutation history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);hash=Mix(hash,entry.TimelineRevisionAfter.Value);}if(pass==0)expected=hash;else Check(hash==expected,"group permutation canonical order");}return expected;}
static ulong ClockExecutionHash(){var timeline=new SimulationTimeline(3);Check(timeline.Schedule(SimulationInstant.Zero,Request(41,10,-1)).Succeeded,"orchestration hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(42,10,0,SimulationEventKind.NoOpMarker)).Succeeded,"orchestration hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(43,20,0)).Succeeded,"orchestration hash schedule");var engine=new SimulationTransactionEngine(new SimulationClock(SimulationInstant.Zero,timeline),new SimulationState(),3);var first=engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(15));var second=engine.AdvanceAndExecuteOneCanonicalGroup(new SimulationInstant(30));ulong hash=14695981039346656037;hash=Mix(hash,(ulong)first.Reason);hash=Mix(hash,(ulong)first.ReachedTime.Ticks);hash=Mix(hash,(ulong)second.Reason);hash=Mix(hash,(ulong)second.ReachedTime.Ticks);for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"orchestration hash history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);}return hash;}
static ulong HostDurationHash(){var clock=new SimulationClock(SimulationInstant.Zero,new SimulationTimeline(),new SimulationRate(5,7));ulong hash=14695981039346656037;for(var index=0;index<1024;index++){var result=clock.AdvanceByHostDuration(new SimulationDuration(13));hash=Mix(hash,(ulong)result.DerivedSimulationDuration.Ticks);hash=Mix(hash,(ulong)result.DebtAfter.Ticks);hash=Mix(hash,(ulong)result.RateRemainderAfter);}return hash;}
static ulong HostDurationDebtServiceHash(){var timeline=new SimulationTimeline(4);Check(timeline.Schedule(SimulationInstant.Zero,Request(70,3,1)).Succeeded,"debt hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,Request(71,7,-1)).Succeeded,"debt hash schedule");Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(72,7,0,SimulationEventKind.NoOpMarker)).Succeeded,"debt hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline);var engine=new SimulationTransactionEngine(clock,new SimulationState(),4);_=clock.AdvanceByHostDuration(new SimulationDuration(11));var result=engine.ServicePendingHostDurationDebt();ulong hash=14695981039346656037;hash=Mix(hash,(ulong)result.Reason);hash=Mix(hash,(ulong)result.ReachedTime.Ticks);hash=Mix(hash,(ulong)result.ProcessedEventCount);hash=Mix(hash,(ulong)result.DebtAfter.Ticks);for(var index=0;index<engine.ProcessedCount;index++){Check(engine.TryGetProcessed(index,out var entry),"debt hash history");hash=Mix(hash,entry.Event.Id.Value);hash=Mix(hash,entry.StateRevisionAfter.Value);}return hash;}
static ulong HostDurationLongRunHash(){const int count=128;var timeline=new SimulationTimeline(count);for(ulong id=1;id<=count;id++)Check(timeline.Schedule(SimulationInstant.Zero,RequestWithKind(id,(long)id*5,(int)(id%3),id%5==0?SimulationEventKind.NoOpMarker:SimulationEventKind.Marker)).Succeeded,"long hash schedule");var clock=new SimulationClock(SimulationInstant.Zero,timeline,new SimulationRate(5,7),new SimulationClockSettings(16));var engine=new SimulationTransactionEngine(clock,new SimulationState(),count);ulong hash=14695981039346656037;for(var cycle=0;cycle<count;cycle++){var converted=clock.AdvanceByHostDuration(new SimulationDuration(7));var serviced=engine.ServicePendingHostDurationDebt();Check(converted.Reason==SimulationHostAdvanceStopReason.Accepted&&serviced.Reason==SimulationDebtServiceStopReason.Completed,"long hash service");hash=Mix(hash,(ulong)converted.DerivedSimulationDuration.Ticks);hash=Mix(hash,(ulong)serviced.ReachedTime.Ticks);hash=Mix(hash,(ulong)serviced.ProcessedEventCount);hash=Mix(hash,(ulong)serviced.ExecutedGroupCount);hash=Mix(hash,(ulong)serviced.LastGroupStopReason!.Value);}Check(clock.CurrentTime.Ticks==count*5&&engine.ProcessedCount==count&&timeline.PendingCount==0,"long hash completion");return Mix(hash,engine.State.Revision.Value);}
static ulong MixedTimelineHash(){var timeline=new SimulationTimeline(4096);var random=new FixedRandom(0xA24BAED4963EE407);var active=new SimulationEventId[4096];var activeCount=0;ulong nextId=1;for(var operation=0;operation<4096;operation++){if(activeCount==0||(random.Next()%3)!=0){var request=Request(nextId++,(long)(random.Next()>>1)-long.MaxValue/2,(int)(random.Next()%101)-50);Check(timeline.Schedule(new SimulationInstant(long.MinValue),request).Succeeded,"mixed schedule");active[activeCount++]=request.Id;}else{var index=(int)(random.Next()%(ulong)activeCount);Check(timeline.Cancel(active[index]).Succeeded,"mixed cancel");active[index]=active[--activeCount];}Check(timeline.ValidateInvariants(),"mixed invariant");}var pending=new ScheduledSimulationEvent[timeline.PendingCount];timeline.CopyPending(pending);Array.Sort(pending,static(left,right)=>SimulationEventHeaderComparer.Compare(left.Header,right.Header));ulong hash=14695981039346656037;foreach(var value in pending){hash=Mix(hash,(ulong)value.Header.Time.Ticks);hash=Mix(hash,(uint)value.Header.Priority);hash=Mix(hash,value.Header.Sequence.Value);hash=Mix(hash,value.Header.Id.Value);}return Mix(hash,timeline.Revision.Value);}
static SimulationEventHeader[] CreateStressHeaders(){var random=new FixedRandom(0x6A09E667F3BCC909);var result=new SimulationEventHeader[2048];for(var index=0;index<result.Length;index++){var time=(long)(random.Next()>>1)-long.MaxValue/2;var priority=(int)(random.Next()%2001)-1000;result[index]=Header((ulong)index+1,time,priority,(ulong)index+1);}return result;}
static void Shuffle(SimulationEventHeader[] values,ulong seed){var random=new FixedRandom(seed);for(var index=values.Length-1;index>0;index--){var other=(int)(random.Next()%(ulong)(index+1));(values[index],values[other])=(values[other],values[index]);}}
static ulong Hash(ReadOnlySpan<SimulationEventHeader> values){ulong hash=14695981039346656037;foreach(ref readonly var value in values){hash=Mix(hash,(ulong)value.Time.Ticks);hash=Mix(hash,(uint)value.Priority);hash=Mix(hash,value.Sequence.Value);hash=Mix(hash,value.Id.Value);}return hash;}
static ulong CatalogHash(CelestialBodyCatalog catalog){ulong hash=14695981039346656037;for(var index=0;index<catalog.Count;index++){var entry=catalog.GetEntry(index);hash=Mix(hash,entry.Id.Value);hash=Mix(hash,(ulong)entry.Identity.Classification);hash=Mix(hash,entry.Identity.ParentBody?.Value??0UL);hash=Mix(hash,(ulong)BitConverter.DoubleToInt64Bits(entry.PhysicalProperties.GravitationalParameter));hash=Mix(hash,(ulong)BitConverter.DoubleToInt64Bits(entry.PhysicalProperties.MeanRadius));}return hash;}
static ulong Mix(ulong hash,ulong value){for(var index=0;index<8;index++){hash^=(byte)value;hash*=1099511628211;value>>=8;}return hash;}
static ulong MixDouble3(ulong hash,in Double3 value){hash=Mix(hash,(ulong)BitConverter.DoubleToInt64Bits(value.X));hash=Mix(hash,(ulong)BitConverter.DoubleToInt64Bits(value.Y));return Mix(hash,(ulong)BitConverter.DoubleToInt64Bits(value.Z));}
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
static void Throws<T>(Action action) where T:Exception {try{action();throw new Exception($"Expected {typeof(T).Name}.");}catch(T){}}
struct FixedRandom(ulong state){private ulong _state=state;public ulong Next(){_state^=_state>>12;_state^=_state<<25;_state^=_state>>27;return _state*2685821657736338717UL;}}
readonly record struct ReplacementStateSnapshot(long MarkerValue, StateRevision StateRevision, ulong CelestialHash, TimelineRevision TimelineRevision, int PendingCount, int HistoryCount, TwoBodyTrajectory? SubjectTrajectory);
