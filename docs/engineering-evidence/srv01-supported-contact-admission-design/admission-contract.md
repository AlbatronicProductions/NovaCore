# Proposed bounded admission and retained ownership

**Design, not implementation or qualification.** This is one cold opt-in
supported-contact consumer of the canonical assembly transaction owner.
Do not create a generic vehicle framework or weaken the existing box route.

## Admitted domain and readiness

- Exactly the current registered SRV01.FourHorn design and canonical seven-part
  identity, original catalog digests, exact initial 30 kg fuel/45 kg oxidizer,
  derived 705 kg wet physical state. One independently prepared episode, one body
  and one retained local world; no transition from an already evolving flight.
- One declared resting orientation/placement on a finite planar slab under the
  existing constant -9.81 m/s2 local gravity fixture. Choose/support-check placement
  from the approved physical child geometry, not from visual AABBs. Record the
  placement in the cold episode input. No teleport or pre-settling of an already
  canonical craft behind its publication chain.
- Main OFF, all sixteen jets OFF, zero-demand resource calculation, fixed gimbal
  actual=target=0; no runtime configuration, topology, force, rate or pause edits.
  No engine-on option in this implementation slice.
- Fixed original frame orientation/origin epoch; one constant moving-frame
  regression. No rotating/rebased world. Prepare world, shape, owner, history and
  visuals before READY; host sampling begins afterward; zero initial debt.
- Bounded contact schedule T(n)=T0+floor(n*1000000/60), n=0..1200, 20 seconds.
  Cheap proof uses first 3 then 120 intervals; long qualification uses full1200.
  Keep T0 and moving-origin epoch across every acknowledgement. Max four complete
  intervals per service call, exact debt retained, no interval skipping.
- Contact-only credit capacity is 4801 records: 4800 positive samples for the
  declared 240 Hz/20-second exact-partition test plus one live completion
  overshoot sample. More inputs refuse before credit; service-existing-debt
  does not consume input slots. Preserve the current free-flight capacity4096.
  This bounded credit storage is prepared before READY and included in <=8MiB.

The banked AssemblyLaunch currently admits <=128 commands, <=15625 ticks each,
<=2 seconds and initial O at zero. These are **free-flight profile restrictions**,
not a compatible contact schedule. Introduce an explicitly selected contact
episode schedule/constructor profile; preserve existing constructor/defaults,
  stock plan and free-flight validation unchanged. Contact supplies the fixed
  1200-entry OFF plan or equivalent immutable fixed schedule, never a general
  larger arbitrary command envelope. The longer contact-only horizon reuses the
  existing retained-contact 1200-step qualification and permits its unchanged
  128-warm/1024-measured campaign; it does not widen RK4/resource evaluation.
  Slab/frame placement can put O at zero while
providing the recorded support height; a different initial O requires a named
contact-only admission, not silent relaxation of free-flight validation.

## Collision authority: missing prerequisite, explicit bounded addition

Current PartDefinitionData has no collision description. Existing GLB/manifest
bytes are presentation assets only; dry inertia is not a collision shape.
Therefore the future slice must first author a small immutable physical collider
profile keyed by existing design digest, definition revision/digest and canonical
part/instance keys. It is additional physical metadata, not new spacecraft identity
or modified stock mass data. Explicit child primitive dimensions and part-local
rigid poses, child->part mapping and a profile digest are required.

Use the existing BEPU convex-box/Compound construction where suitable, with
explicit authored part coverage and bounded child count. Do not call three old
bus/pod boxes SRV-01. Do not extract collision from GLB at runtime or derive box
dimensions from a tensor. Exact authored dimensions/count/placement must be
reviewable and qualified in the first cheap geometry gate before solving; this
design does **not** pretend those missing numbers already exist. If the real
parts cannot be represented faithfully enough by bounded existing primitives,
stop for Project Control rather than inventing hull/decomposition or omitting
unsupported geometry. No new rendering assets or Blender work is required.

Cold preparation compiles each child pose into assembly material coordinates,
then subtracts canonical source COM exactly once for native body coordinates.
Do not ask a compound builder to recompute mass or recenter canonical data.
Validate child count, finite dimensions/rotations, stable feature ownership,
FP64->FP32 error, smallest feature and support envelope before allocation/READY.
Reuse the qualified CompoundContactCoverage/Selector with scratch sized from
the admitted child count. Do not copy the three-child diagnostic mask limitation
or change the four-row selection policy. One dynamic compound/static convex
pair remains the narrow-phase domain; unexpected pairs/features fail closed.

## Same-world lifetime

Reuse LocalContactWorld's native storage/step/export/disposal ownership with a
typed assembly binding. Share numerical primitives rather than duplicate a BEPU
world implementation; legacy source validators keep their old meaning.
Immutable binding: engine/launch/design/collision profile, part mapping, shape
topology, initial T0/end/frame, native body/static handles, world generation.
Mutable expected binding: complete assembly value, state/timeline revisions,
clock/debt and input sequence, acknowledged frontier and pending receipt.

Native body is created once from the source. Ordinary acknowledgement changes
only expected authority, never imports canonical doubles back into the pose,
clears constraints, reconstructs shapes or resets T0. BEPU owns transient
manifolds/warm starts; individual rows may legitimately change. Same-body
continuation and trajectory witnesses establish retention, not frozen arrays.

One successful native step creates one outstanding sealed endpoint. No second
step until it is published/acknowledged. Precommit refusal retains a valid
pending result for retry where source remains sound; explicit cancellation after
native mutation terminates that continuation, since a solver step cannot be
rolled back. Post-step export/envelope/coverage failure poisons it. Dispose is
owner-thread-only, idempotently refused after disposal, and releases owned native
buffers/children; construction failure unwinds only partially created resources.

## Consumer selection and departure

Select FreeFlight or SupportedContact at cold episode admission in the existing
assembly authority/storage. That immutable selection guards prepare/service/
publish/abort/save entry points, not merely the display scene. Only one active
assembly owner may bind the canonical slot. Free-flight preparation cannot run
while the contact consumer owns it; contact cannot bind an active free-flight
episode. No extra clock, lock or canonical store.

Scene supplies sequenced host input through that owner and never calls the
ordinary clock orchestrator for this craft. A direct outside clock/event/state
mutation between calls remains possible under banked APIs but causes the next
contact authority check to refuse; it must never be acknowledged as self-change.
The common publication phase prevents interleaving during each operation.

Support evidence, active consumer and world membership are separate, matching
KSA. This slice stops/holds on unsupported outcome instead of switching consumer.
Departure eligibility/handoff is deferred by source-backed separability, not
thrust-to-weight. See equivalence-matrix.md for future handoff invariants.
No powered support, free-flight acquisition, liftoff, landing, terrain or Florida.
