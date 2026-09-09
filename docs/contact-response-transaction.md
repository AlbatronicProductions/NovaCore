# Unified linear/angular contact transaction

Banked **M14.4**, commit `28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c`, tag
`m14.4-atomic-contact-response`. [Investigation evidence](engineering-evidence/contact-response-transaction/README.md)
retains the dated candidate and allocation closeout chronology.
The current unbanked [isolated analytical policy](isolated-contact-response.md)
produces this existing intent; it does not change the banked transaction.

## Responsibility and admission

Simulation accepts one already-qualified instantaneous impulse, not a force over
an interval. `SpacecraftContactImpulseIntent` contains spacecraft identity,
expected current global state revision, exact event instant, inertial-root frame,
root impulse in kg m/s, root COM-relative application offset in metres, response
version 1, authored feature/geometry identity, canonical physical support identity
and a nonzero producer observation ID. No renderer or solver cache identity enters.

The timeline's bounded typed store retains the immutable admitted intent. The
candidate is recomputed from the canonical resolved intent before commit; modifying a candidate's
impulse, provenance or either replacement cannot authorize different physics.
Structural provenance validation and payload equality are not independent proof
of physical contact. The trusted producer owns qualification, support readiness,
current geometry and identity admission at the event boundary. There is no
automatic conversion inside the transaction engine. The separate unbanked isolated
policy qualifies a restricted M14.3 supplied-instant observation. A future finite-shape producer
can use the same qualified-intent contract without invoking the point generator.

Zero impulse is rejected at admission without scheduling/consumption or mutation.
Invalid frame, nonfinite vectors, unsupported version and incomplete provenance
are rejected. The event's expected revision remains fixed: intervening changes
require explicit requalification, not silent refresh of old contact intent.

## Payload ownership and lifetime

Contact-capable composition explicitly supplies timeline contact capacity, separate
from generic pending/history preallocation. `ScheduleContactImpulse` and
`ReplaceContactImpulse` admit immutable intents into typed slots; generic requests
cannot install contact handles. Zero-capacity marker timelines allocate no contact
arena. The 112-byte generic event remains free of managed references. Its compact
slot index occupies existing alignment padding. Each configured slot occupies
248 bytes, including the unchanged 200-byte intent and full event identity.

Resolution verifies canonical pending identity, slot bounds and the full header.
The existing monotonically assigned, non-reused sequence is the occupancy generation;
overflow rejects before publication. Cancelled/consumed snapshots cannot resolve a
recycled slot. Reuse order does not participate in event ordering or replay.

Arena exhaustion rejects without mutation; it never grows or allocates an event
object. Typed replacement requires a free destination slot and leaves the original
intact if unavailable. Generic heap/ID-history capacity policy remains unchanged;
callers still preallocate those existing structures for allocation-free admission.

Rejected proposals retain their event and payload for retry or explicit cancellation.
Cancellation, replacement and successful consumption clear transient provenance
references and recycle the slot. Retirement readiness is checked before mutation.
The existing exclusive single-writer phase owns publication; no callbacks or
concurrent readers may observe intermediate slot/event updates. Committed coupled
history keeps its independent value provenance, not a transient slot reference.

## Exact event and physics

The authoritative clock already equals event time t. Both current segments are
evaluated at t, which must not precede either active segment epoch. Negative
historical absolute epochs are supported when their active segments cover t.
Time remains integer microticks (1,000,000 per second), with no new 60 Hz lattice.

For event body-to-root orientation Q and principal body inertia I:

```
deltaV_root = J_root / mass
deltaL_root = r_root cross J_root
deltaL_body = Q^-1 rotate deltaL_root
deltaOmega_body = componentwise(deltaL_body / I)
```

Both replacements use epoch t and retain the evaluated position/orientation.
Stored event-pose continuity is bit-identical. A subsequent derived rotational
evaluation can renormalize Q under the existing rotation evaluator; that numerical
roundoff is separate from the transaction and does not authorize altering it.
Only linear/angular velocity changes. Mass, inertia, model, root identity,
constant root force and constant body torque remain unchanged. No extra planetary
surface velocity is injected. No contact interval is integrated and no pose clamp
or penetration correction occurs.

## Transaction and publication

1. Match pending canonical event, clock, timeline revision and current state revision.
2. Re-evaluate its intent and compare both complete replacement candidates.
3. Validate numerical results, both state slots, history capacities, state revision
   overflow and timeline consumption/revision availability.
4. Construct successor revisions, coupled history and event receipt before writes.
5. Check both expected store slots before assigning either; write both and increment
   global state revision once.
6. Consume the prevalidated event and append into reserved history storage.

There is one coupled physical before/after record, not separate force/torque
histories. The existing general processed-event receipt records canonical scheduling.
Both capacities are preallocated and independently checked. A normal rejection
leaves both states, controls, revisions, histories, clock and pending event unchanged.

This is the existing exclusive single-writer authority model. No callbacks,
concurrent writers, user code, provider queries or allocation run between preflight
and publication. Post-preflight event-consumption failure would indicate internal
invariant corruption, not a recoverable rejection. Process termination or memory
corruption is not a transactional rollback guarantee.

## Verification and deferred responsibilities

The permanent test-only oracle uses one approaching point, qualified normal,
prescribed moving support, declared restitution and zero friction, without competing
contacts or penetration repair. It tests coupled momentum; it is not a production
solver or touchdown-event detector. Numerical bars are declared from FP64 unit
roundoff, operation envelopes and fixture scales before measuring errors.

No friction, rest, grounded state, sleeping, finite collision shapes, pads,
propulsion, gravity change, launch/landing policy or visible route integration is
included. Existing force and torque events retain their independent semantics.

KSA's game-owned state/publication and subordinate solver responsibility is adapted.
Bepu remains an eventual candidate, not a dependency. A later adapter must qualify
contact-response extraction, local FP32 conversion and integration ownership;
neither arbitrary solver endpoint replacement nor net substep impulse automatically
satisfies this instantaneous-event contract. Dynamic two-body collisions require
atomic publication across both participants and remain outside this seam.
