# Immutable facts, mutable authority and minimum safe boundaries

Pre-correction source/lifetime audit, completed before conditional implementation. The
whole-family payoff closure and correction are in correction-contract.md. Source locators use the key in
[operation-validation.md](operation-validation.md).

## Mutability timeline

Observation and host admission precede interval T0. They each release their owner phase.
Ordinary external source/clock/timeline mutation can occur between them and servicing.
Host credit commits debt and its expected-clock acknowledgement separately from physics.

| Fact | T0 source observation / service entry | T1 engine preparation after command closure | T2 resource preparation | T3 consumer / T4 contact preparation | T5 solve / T6 seal | T7 final recheck | T8 canonical commit / T9 ack | T10 next interval |
|---|---|---|---|---|---|---|---|---|
| Command revision/sequence | Live validation; due commands may change at closure | Post-closure identity captured, cursor/latch consumed once | Parent identity stable | Stable during owned path | Stable; do not replay in callbacks | Recheck current parent/command applicability | Engine lease consumed | New boundary/commands allowed |
| Engine identity | Immutable definition/version and owner capability | Active preview/seal/boundary issued | Parent of resource lease | Consumer cannot substitute definition/latch | Remains source preview | Must remain active/genuine | Preview retired; actual actuator revision increments | New preview, no rewind |
| Resource identity/revision | Exact canonical units and definition checked | No canonical resource write | Exact successor prepared; lease generation changes | Canonical source still old | Native work spends no canonical fuel | Exact source/lease still current | Exact successor units/revision installed, expected resource acked | New acknowledged source |
| Body/StateRevision | Live source bits and global revision checked | Source physical copied | Source mass consistency proved | Canonical source remains old | Native body evolves; canonical source does not | Full physical applicability/slot check | Endpoint+mass bits installed; StateRevision +1; expected physical acked | New source, old input expires |
| TimelineRevision/event | Checked live | Event boundary checked | Source timeline retained | Event<=target refuses before native step | No event consumed/synthesized | Still check boundary/current identity | Timeline unchanged | Recheck next target |
| World/gen/body handle | Issuer, original world/generation/body | Same | Same | Native property-only write; PreparedFrontier set | Same world/body; frontier advances and pending set | Live receipt/pending/gen/source match | No reconstruction; pending clears only at ack | Same world/manifold/cache |
| Contact source/config/frame | Original T0/end/local/moving-frame epoch | Immutable | Immutable | Used for original schedule and transforms | Original epoch used for root export | Must bind same source/config | No epoch reset | Original episode persists |
| Physical properties/mass/inertia | Exact-resource-to-source-bit invariant | Source-held | Proposed successor derived separately | Native source mass/inertia installed | Held for ordinary solve | Source and proposed successor remain distinct | Canonical successor mass, then expected properties | Source mass may differ next interval |
| Powered duration/duty | Not yet interval proof | Demand prepared | Exact rational duration fixed in resource proposal | Duty projected once from that proposal | Input fixed during solve | Sealed segmentation must match | Lease consumed | New duration, even if value happens equal |
| Ordinary duration | Original exact schedule | Next integer target | Same interval | 16666/16667 admitted; projection dependent only on schedule | Actual step uses exact target-current conversion | Exact target retained | Clock advances by exact integer interval | Original T0 determines next pattern |
| Clock/debt | Full expected state after accepted host credit | Stable during owner phase | Stable | Sufficient debt/horizon checked | Still unspent canonically | Recheck live clock and exact subtraction | Target clock/debt commit, both expectations ack | Remainder retained; fresh host API may add credit |
| Proposal generations | No outstanding interval for service start | Engine boundary active | Resource generation increments | Consumed only by selected consumer | Joint generation increments after valid endpoint | Seal/gen/active/pending recheck | All execution leases consumed | Old copies invalid |

There is no separate mutable engine-definition revision or body revision on this path.
Engine version is immutable; command revision/sequence, preview boundary/seal/active status
and ActualEngineState.ActuatorRevision serve different responsibilities. Physical freshness
uses global StateRevision **plus content equality**. Do not invent missing revision fields.

## What the owner phase actually proves

`ContinuationPublicationPhase` is a same-thread/reentrancy guard, not a lock or scheduler.
`SimulationState.VerifyStoreMutation` and ordinary clock/timeline mutation reject an active
publication phase. Engine command/resource APIs acquire that phase; they cannot interleave
as nested mutations. The current constrained callbacks only read prepared acceleration and
write native velocities/contact metrics; there is no user callback into canonical authority.
No callback or phase release exists between PF's final recheck and fixed writes.

This permits immutable fact consumption **within the current continuous owned call graph**.
It is not a global seal:

- Host and separately callable preview/prepare/publish methods release/reacquire ownership.
- The phase has no generation token proving continuity across returns.
- Private powered prepare/publish rely on guarded callers rather than their own explicit
  IsOwnedBy check; engine/resource helpers have that check.
- World/source/config/body fields are private readonly, but TryDispose checks the owner
  thread rather than active publication phase. Current callbacks cannot call it; a future
  generic callback would invalidate this call-graph proof.
- OrdinaryContactInput is a copied DTO. InstallPoweredInput's lifecycle guard does not
  turn arbitrary input values into a sealed resource/interval capability.
- Existing authority tests deliberately alter backing physical/config/mass/force values
  without StateRevision changes. Revision-only external/retry validation would weaken the
  accepted contract. Preserve source-content checks at these boundaries.

## Minimum safe boundary responsibilities

Counts below identify authoritative responsibilities. The implemented ready lifetime is
restricted by correction-contract.md; gross work credit is distinct from replacement cost.

| Fact class | Minimum safe placement if immutable proof is consumed internally | Why another boundary remains |
|---|---|---|
| Owner/issuer/current source | At every independently entered host/service/preview/prepare/publish/retry operation | Phase release permits unrelated mutation |
| Host sequence and full clock | Host entry plus final accounting applicability; service entry after credit; final physical commit | Accepted host credit is a separate commit; input cannot be consumed twice |
| Command source/demand | After actual command drain/closure, once before engine issuance; current parent/command recheck before physical commit | Command closure legitimately changes revision/cursor; older source proof is insufficient |
| Exact source mass derivation | Cold source establishment plus exact joint-successor induction; otherwise one derivation for a new unproved source | Only matching exact units/definition/physical authority inherit proof; never generic double equality |
| Resource proposal | Exact calculation/interval validation once per issued generation; live lease/source/parent applicability on exposed use and final commit | Retirement/reissue/substitution can happen between APIs |
| World/body/interval permission | Before native mutation and on exposed receipt/endpoint use | Disposed/poisoned/foreign/pending/wrong-frontier state cannot be covered by old canonical proof |
| Native result | After every actual solve before seal | Source validity does not imply solver/export/support success |
| Endpoint/publication | Genuine pending endpoint and bit/source match at publication admission/final preparation | Duplicate/stale/fabricated endpoint must not publish |
| Mutable canonical successor | Final state/resource/command/clock/event/revision/history/slot/ack check immediately before fixed writes | Required failure-atomic bundle cannot rely on arbitrary DTOs |
| Next interval | Acknowledged successor source plus fresh interval/command/resource eligibility | Publication changed physical/resource/clock/frontier; old interval input is invalid |

Six mass derivations and both ratio projection lifetimes are separately traced in the
operation audit. No minimum numerical reduction in D/L checks is claimed from this table.
Removing a repeated deep reader would still require a suitable owner/lease proof check.

## Existing prepared responsibility, authority and forgery

**Preference A: reuse existing prepared/sealed owners.** EnginePreparationStorage already
owns the genuine demand and its captured source. PropellantPreparationStorage owns the
exact segmentation, parent, private seal and generation. PoweredFlightStorage already
owns expected canonical authority and the one joint proposal/record. PoweredBinding owns
native permission and acknowledged world state. These are sufficient responsibility homes
for the established facts; no new public PreparedPoweredContactPacket is justified.

The implemented internal proof consumption binds issuing engine/resource owner,
private seal, active generation/boundary, exact source identities/content, selected consumer,
original episode, world/body, source/target and frontier. An immutable preview is evidence,
not authority by itself. Same numeric values or a copied generation cannot forge the private
issuer/seal relation. Default/foreign/stale/retired capabilities still refuse.

Mass reuse has an inductive proof: cold PC:29 checked exact dry+fuel projection equals source
mass bits; resource preparation computes exact successor mass/units; the single canonical
commit installs both; successful ack advances both expectations. Canonical or resource
substitution breaks the premise; failed ack ends continuation. This supports eliminating
re-derivation as a *fact*, not removing current source comparisons or changing source mass policy.

Ordinary duration can have an episode-schedule lifetime. Powered duty, orientation-dependent
wrench and projected native acceleration cannot outlive their exact prepared interval.
The correction extends existing owner fields only; validation.md reports measured combined storage and the failed runtime ceiling.

## Final recheck obligations

Keep source state revision AND physical bits/definition; exact resource definition/units/
revision; command revision/sequence/closed boundary; engine/resource active seals and parent;
joint active seal/generation/consumer; original episode and source/target; clock/debt/rate/
pause/remainder; event at/before target; history/revision/arithmetic capacity; live native
world/gen/body/frontier/pending receipt and exact exported endpoint match; prepared target
slot and ack destination. Pure exact derivations can be consumed as proof only while these
dependencies remain linked. No numerical work belongs in the fixed canonical writes.

## Failure and publication retry

| Refusal class | Last safe detection / preserved result |
|---|---|
| Wrong thread/reentrancy/foreign authority | Entry before any request-specific mutation |
| Stale command/engine/closed boundary/parent | Engine/resource use and final publication; no replay of consumed command cursor |
| Stale exact resource/definition/mass/body/force/configuration | Fresh exposed entry plus final source content/lease checks; numeric equality/revision alone insufficient |
| Wrong world/body/generation/frontier/interval/consumer | Before native mutation, endpoint read and publication admission |
| Default/fabricated/old/duplicate/retired proposal | Private issuer+active seal/generation/parent/receipt checks; no second fuel debit or physical publication |
| Clock/rate/pause/debt mismatch or duplicate host sample | Host/service entry; final checked accounting; accepted earlier credit remains committed |
| Event<=target/history/debt/revision capacity | Cheap pre-native gate; final publication admission. No event processing here |
| Solver/property-install/export/support failure | Native poison and poststep validation; no canonical resource debit; invalidate unsafe continuation |
| Sound pending endpoint, ordinary precommit refusal | Preserve same pending endpoint/leases where still valid; revalidate dependencies on retry |
| Endpoint/resource/actuator mismatch | Before canonical writes; never manufacture a matching successor after the fact |
| Canonical physical commit followed by ack failure | Keep endpoint/resource/debt/revision/history, consume leases, invalidate world; distinct committed terminal result |
| Canonical host credit followed by ack failure | Keep admitted debt, invalidate continuation; do not report ordinary refusal or roll back credit |

A publication retry re-enters ownership and rechecks current dependencies, then consumes
the original pending endpoint. It does **not** run engine demand, segmentation mapping,
property installation or BEPU again. It may reuse source-derived facts only if the genuine
parents/generations and full expected authority remain current. Retirement consumes the
permission; a stepped contact proposal cannot be discarded and silently recalculated like
pure free flight. Success or terminal ack failure kills every execution lease.

Existing refusal tests and new ready-lifetime tests passed Debug and Release. This table
defines their detection boundaries; validation.md records execution and the performance stop.

## Storage and physical constraints

Existing private stores retain source snapshots/previews/seals. The correction adds one bool
and one long to PoweredFlightStorage, and three PoweredBinaryScale values in each of cold
preparation and retained powered binding. No per-operation object, array, pool or global cache.
The measured combined conservative upper bound is 5,401,088 bytes (history 4,857,624 included),
104 bytes above the retained prior bound. This includes the reachable cold preparation copy.
Five warmed contact allocation windows and the M15.0 allocation regressions report exact zero.

Source-mass policy, exact resource/event arithmetic, force/torque, FP32 projection, gravity,
ordinary timestep, 8 iterations/1 substep, warm starts, geometry/error bars and M15.0 free flight
remain unchanged. The first new Release control process fails median: 0.0683 > 0.05 ms.
No second correction or further qualification follows. UNBANKED; STOP FOR PROJECT CONTROL.
