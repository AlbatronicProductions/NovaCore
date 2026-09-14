# Current source map

Inspected working tree and banked source:89a432ce3b96a5dcb0751cfdc26bfd6776c883a1. Paths below are relative to repository root. Anchors describe CURRENT implementation; proposed extraction or new writers are not present.

| Source | Current contract | Reuse / gap |
|---|---|---|
| `src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftTranslationState.cs:6-18` | Positive finite scalar mass; COM position; body origin COM; inertial ECL root; constant net root force | No dynamic mass/COM law or finite powered validity |
| `.../Spacecraft/Translation/SpacecraftTranslationEvaluator.cs:20-28` | a=F/m; analytical x,v at requested integral time | Constant-mass/root-force oracle only |
| `.../Spacecraft/Translation/SpacecraftMotionEvaluator.cs:25-30` | Independent translation plus angular evaluator | Coherent observation, not coupled body-thrust integration |
| `.../Spacecraft/Rotation/SpacecraftRigidBodyRotationEvaluator.cs:7-12,78-111` | Bounded10 ms RK4; constant diagonal inertia/BODY torque; shared quaternion/Euler algebra currently private | ADAPT pure derivative/canonicalizer for a sibling coupled kernel; preserve old operation order/tests |
| `src/NovaCore.Core/Precision.cs:21-30` | Scalar-last Hamilton multiplication; active vector rotation normalizes its input | Reuse frame math; raw RK stages remain raw, accepted endpoints canonicalize |
| `.../Spacecraft/ReferenceFrames/SpacecraftReferenceFrameEvaluator.cs:42-54` | Translating carrier must be ECL root; omega_root=R(Q)omega_body | No new rotational convention |
| `.../Spacecraft/Translation/SpacecraftPhysicalEventTranslationEvaluator.cs:14-33` | Fractional read-only specialization of same constant-force/mass model | Not an execution consumer |
| `.../Spacecraft/Rotation/SpacecraftPhysicalEventRotationEvaluator.cs:36-63` | Integral floor evaluation then event-only fractional RK4 | No full resource-ratio input or changing mass |
| `.../Time/PhysicalEventEpoch.cs:12-14,42-47`; `.../Time/PhysicalEventDuration.cs:8-45` | Bounded UInt64 fraction; local duration projected to double seconds | Preserve encoding; M14.24 ratios need their own numerical adapter |
| `.../Spacecraft/Contact/PrivateCanonicalPropagation.cs:61-141` | Certified degree-11 constant-force/mass and torque-free propagation | Contact proof responsibility; not a general propulsion solver |
| `.../Spacecraft/Contact/Staging/LocalContactCallbacks.cs:48-63` | Fixed local acceleration=source force/source mass; no hidden gravity; torque-free angular integration | Powered contact needs explicit new admission/cadence |
| `.../Celestial/CelestialSystemEvaluator.cs:10-18,58-73` | Body ephemerides and physical properties | Does not apply spacecraft gravity |
| `.../Transactions/SimulationTransactionEngine.EnginePreparation.cs:131-190,205-247` | One genuine parent; seal consumes PRIVATE edge/latch cursor; captures full clock and physical authority; discard does not rewind | Actual hardware state/publication is missing |
| `.../Spacecraft/Actuation/SingleEngineActuation.cs:77-91` | Conditional BODY wrench, required flow, not actual actuation | Keep prepared versus committed observations distinct |
| `.../Spacecraft/Resources/FinitePropellantSegmentation.cs:23-59,77-146` | Point reservoir; readonly cold source; bounded exact split; copied WOULD-event | Future joint writer required behind stable owner capability |
| `.../Transactions/SimulationTransactionEngine.Propellant.cs:29-39,90-166` | Exact mass-source check; genuine parent; private resource lease; cancel/reissue permitted with fresh generation | No fuel consumption, mass writer or resource revision increment |
| `.../Spacecraft/SpacecraftStateStore.cs:23,96,125-134` | Mass initialized cold; paired replacement installs linear/angular state only | Prepare paired-plus-mass destination for new sibling publisher |
| `.../Transactions/SimulationState.cs:59-68` | Fixed paired install plus one StateRevision | Reuse fixed-write responsibility, extend only under new joint preparation |
| `.../Transactions/ContinuationPublicationPhase.cs:3-35` | Existing exclusive owner thread/phase; rejects reentrancy | Reuse; no second lock or authority |
| `.../Transactions/SimulationTransactionEngine.PersistentContact.cs:61-125` | Receipt admission; exact event/debt/history checks; paired fixed commit then private ack | Pattern reusable; evidence/applicability contract remains separate |
| `.../Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs:18,61-78,129-133` | Mass/force/inertia source frozen; ack advances paired state/revisions/clock/frontier only | Do not widen for powered source by equality bypass |
| `.../Transactions/SimulationTransactionEngine.Continuation.cs:89-92` | M14.17 requires genuine EventFreeThroughTarget | Never fabricate propulsion clearance |
| `.../Transactions/SimulationTransactionEngine.ContactServicing.cs:47-59` | Host credit commits distinctly; private ack failure retains credited debt | Reuse failure linearization in future powered owner |

`...` in this table expands to `src/NovaCore.Simulation`.

## Command, preparation and backlog

`SimulationTransactionEngine.SpacecraftCommands.cs:114-132` admits monotone ingress at/after the funded horizon. Lines143-201 commit due requested transitions at current exact boundary, capturing engine edges. Lines209-221 close a drained boundary. `SpacecraftCommandAuthority.cs:25-36` derives the original 60 Hz integer lattice. A command committed before a refused physical interval remains committed; newly arrived commands cannot be applied retroactively into funded backlog.

M14.23's preparation-consumed cursor is not a canonical applied-actuator cursor. Its full-clock seal also means new host debt cannot be admitted while a retryable physical proposal remains pending without staling it. The future owner orders host credit before preparation and retains later unsubmitted elapsed input separately until that lease is resolved.

M14.24 current source is readonly, not a writable tank hidden elsewhere. Resource-only cancellation followed by segmentation of the same still-active engine parent is valid and nonphysical. Future execution must bind the exact current lease generation; numerical equality after reissue cannot revive an old endpoint.

## Regression anchors for a future implementation

Current tests were inspected, NOT rerun for this documentation investigation:

- `tests/NovaCore.Simulation.Tests/Program.cs:364-380`: principal-axis torque, asymmetric Euler coupling, momentum/energy, normalization, bounded work and allocation.
- `tests/NovaCore.Simulation.Tests/SpacecraftPhysicalEventMotionTests.cs:166-195`: analytical axis-rotation and independently converged midpoint references.
- `tests/NovaCore.Simulation.Tests/SingleEngineActuationTests.cs:158-162`: COM/mirrored mount moment signs.
- `tests/NovaCore.Simulation.Tests/FinitePropellantTests.cs:197-204,298`: dedicated dry/fuel fixture and conditional mass/inertia.
- Banked resource [qualification](../finite-propellant-segmentation/README.md) records 66/66 Debug/Release, zero allocation and article non-actuation. Its original UNBANKED wording is historical; current refs establish M14.24 BANKED. No historical report was rewritten.

No source evidence supports calling a present constant-mass endpoint a powered result. No new compile, renderer or allocation campaign is justified for this source-only assessment.
