# Source properties, exact resources and typed publication

## One authority, explicit material-origin conversion

Current AssemblyRuntimeState is canonical: stores, AssemblyMass, material-origin
AssemblyMotion, gimbal, command, realization, frontier and resource/actuator
revisions. AssemblyLaunch/design supply immutable part identity and physical
authoring. Native BEPU properties are bounded numerical inputs only.

Let c be the canonical body-frame COM offset, R the canonical body-to-root
orientation and w the body angular velocity. Reuse the typed physical observation
semantics in SpacecraftMotionEvaluator.cs:54-60:

- Import: xC = xO + R*c; vC = vO + R*(w cross c).
- Child pose: instancePose(childLocalPose), then subtract c once from position.
- Export: xO = xC - R*c; vO = vC - R*(w cross c).

Apply existing fixed root/local-frame transport before/after these formulas.
Root COM position is origin0 + frameVelocity*(target-T0) + localToRoot*pLocal.
Angular input is body -> root -> local; output is local -> root -> staged body.
The original moving-frame epoch never changes on publication. Check the
FP64/FP32 import/export error and root spacing relative to the qualified physical
feature tolerance. A COM export installed as O would move all seven parts wrongly.

The first slice is unpowered: c and the full tensor remain constant, so the
rigid material velocity **at the COM location** equals the COM trajectory
velocity. The velocity at O generally differs by R*(w cross c), even when c
is constant. No c-dot term is needed for this slice. During future depletion,
the moving mass centroid needs separate analysis; no c-dot mechanism is invented
in this ticket.

Cold derive source mass/COM/tensor from the exact canonical stores using the
existing AssemblyLaunch.ObserveMass / CompiledAssemblyDesign.ObserveMass. Verify
the current diagonal-qualified tensor and positive representable inverse values;
never silently drop cross terms or compute a replacement box inertia. Install
the corresponding BEPU inverse tensor and mass. Keep source properties through
the ordinary solve. The first slice has no changed successor mass to install.

The KSA-equivalent future property boundary remains: canonical exact store
successor -> derived successor mass/COM/inertia -> same-body numerical refresh
before its next solve. If COM changes, collision children must be repositioned
relative to COM without moving the material geometry, broadphase bounds updated,
and compatible velocity semantics proven. This is an ownership constraint, not
authorization to implement powered COM relocation, change the source-mass policy,
or claim its numerical qualification now. Topology change is refused.

## Two-store preservation, no fake resource pipeline

Do not bind PropellantDefinition's one central reservoir or copy stores into
contact-owned mutable authority. For the admitted OFF/OFF interval, zero demand
is established by the immutable command profile. Carry exact fuel/oxidizer bits,
ResourceRevision and derived AssemblyMass through unchanged. Publish a truthful
OFF/zero-jet/NoDemand realization and existing per-interval ActuatorRevision+1;
there is one command/resource/physical lineage, not an actuator callback debit.

AssemblyResources.Calculate currently rejects ticks>15625 **before** its
zero-demand branch. Do not call it with a 16666/16667 contact interval, loosen
its banked free-flight validation globally, or run AssemblyDynamics merely to
obtain resource results. Shared zero-demand value preparation may be factored
without changing that evaluator; the contact consumer already proves no demand.
Realized main/RCS demand is therefore exactly zero in this first slice. Any
nonzero demand refuses before mutation. Later powered admission requires exact
two-store preparation and a coupled physical/property successor under this same
owner, not an automatic fallback to single-reservoir M15.1.

## One coherent ready/apply correction for the future implementation

Extend the existing assembly authority/storage with a cold immutable physical
consumer discriminator and optional retained-contact binding. Keep one canonical
assembly slot, one phase guard and common typed commit responsibility. Contact
preparation stages AssemblyRuntimeState and a dedicated value provenance record;
it does not masquerade as AssemblyDynamics/free-flight evidence or M14.17 clearance.

Readiness binds engine/launch/design/profile, source epoch/end/frame, exact state,
state/timeline/resource/actuator revisions, clock/debt/input sequence, world/body/
shape generation and acknowledged frontier. Prepare immutable geometry/mass
facts once; final checks cover still-mutable canonical authority and pending receipt.
No second generic transaction engine or lock.

Sequence within the existing exclusive owner phase:

1. Admit a sequenced host duration with existing PrepareHostAdvance conversion.
   Canonical debt and credit identity commit before fixed expected-clock update.
   This accepted credit remains canonical even if later service blocks.
2. Service existing debt separately, up to four intervals. Before a native step,
   check exact target/end, consumer/source, events at or before target, funded
   debt, history/proposal capacity and all revision arithmetic. No solver work
   to discover an already knowable refusal.
3. Install checked source numerical properties if needed, ordinary BEPU step,
   export, verify supported-domain/precision outcome, prepare typed O successor,
   exact copied resources and one deterministic contact history record.
4. Retain exactly one sealed pending receipt/proposal; prepare fixed private
   acknowledgement targets. Final source/slot/event/clock/debt/revision/receipt
   recheck protects the commit. Ordinary precommit retry reuses this endpoint,
   never repeats the solve or consumes stores again.
5. Fixed canonical writes: complete assembly successor in the existing slot,
   StateRevision+1, exact target clock, old debt minus exact integer interval,
   one history record. TimelineRevision and pending events remain unchanged.
6. Fixed private acknowledgement advances expected authority/frontier and clears
   pending; no solve, allocation, normalization, pose reimport, shape recreation
   or callback. Return copied canonical observation. Unexpected acknowledgement
   failure returns CANONICAL_COMMITTED_PRIVATE_INVALIDATED, never "not published".

The history entry identifies consumer/responsibility, source/target, spacecraft/
launch/part-profile digest, prior/new revision and exact successor lineage.
No live world, handle, pointer or process-global generation order belongs in
deterministic canonical history. Private seals/generations protect execution;
stable profile/value provenance protects history. Raw staged O/orientation/velocity
bits install once; no commit-time recomputation. Observations are endpoint-only,
not a contact extrapolation certificate.

## Current numerical schedule, not a remembered approximation

LocalContactWorld.Step currently passes float((targetTicks-currentTicks)/1000000d)
to BEPU. Its canonical durations alternate 16666/16667 as derived from T0.
The 1f/60 loop in PoweredContactPreparation is **cold fixture settling**, not
the production Step implementation. Preserve the current retained step contract,
8 iterations/1 substep, callbacks and selector; do not introduce a new exact
float dt, event-sized solve, force scaling or solver-quality change in this front.

## Persistence and display limits

Current runtime/2 SaveAssemblyFlight serializes a free-flight plan and replays it
to reconstruct exact history. Add an explicit consumer refusal for contact in
that API before serialization; keep all free-flight save/restore bytes/semantics.
Contact resume/manifold serialization is deferred, not falsely advertised.
Contact restart is an explicit new cold episode, never restoration hidden inside
continuation. Completed canonical endpoint remains observable after disposal.

Reuse material-O copied presentation: same seven part poses, reusable mesh binding,
no new plume, no live body reads. The contact scene supplies one copied observation
and slab presentation; suppress exhaust because Actual is OFF and on completion.
Render once per display frame after bounded service. Existing stock scene/launcher
remains free-flight; a separate clearly named supported route avoids false claims.
