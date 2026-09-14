# Candidate selection and one bounded next ticket

## Serious architecture candidates

| Responsibility | A: resource-first segmenter | B: physical integrator detects depletion | C: fold resource into M14.23 preparation |
|---|---|---|---|
| Owner | One canonical resource owner consumes a genuine engine proposal | Dynamics solver owns both solve and resource decisions | Engine preparation owns feed state |
| Exactness | Can prove availability/time/successor before any physical work | Must couple resource root finding to each integration regime | Can calculate it, but obscures available-feed demand versus realized feed |
| Atomicity/replay | One sealed dependent proposal joins future physical commit | Possible with a proper joint commit, but refusal can occur after costly/private work | Risks private edge consumption being mistaken for canonical resource consumption |
| Shared feed | Natural central aggregate admission | Requires coordinating multiple integrators/consumers anyway | Per-engine subtraction invites double spending unless a resource owner is recreated inside it |
| Contact/free-flight reuse | Same two-segment result feeds either | Duplicated regime-specific resource policy or a second shared component | Hardware API acquires physical/resource-specific applicability |
| Event identity | Specialized exact source-bound resource event, integral public endpoint | Event identity tends to inherit solver precision/step representation | Source/proposal/event dependencies become conflated |
| Bounded cost | Fixed amount limbs and at most two segments | Larger solve/restart/error surface for an elementary constant-flow model | Can be bounded, but unnecessarily widens banked M14.23 |
| Decision | **SELECT** | REJECT for this first responsibility | REJECT |

B remains a valid future consumer of A's resource schedule, not the owner of feed accounting. It could be appropriate for a later physically coupled pressure/flow model, but that is outside constant M14.23 flow. C only becomes sound by preserving a separately owned resource sub-boundary, which is A with unnecessary banked API conflation. The source gaps and numeric counterexamples justify A independently of the prompt's suggested shape.

## Time/amount alternatives

1. **FP64 amount subtraction and duration:** reject as authority. Tiny debits stall, positive durations round to zero, and rounded full-cost equality is false. Keep only labelled approximate observations.
2. **Existing UInt64-fraction PhysicalEventEpoch:** keep within its banked contract; reject as the universal fuel representation. Valid depletion denominators exceed its capacity. No fractional public-clock migration.
3. **Fixed exact amount plus source-bound rational offset:** select. Capacity follows from binary64 and integer ticks; no runtime-growing BigInteger/event list. Existing exact identity/provider ownership principles are adapted without copying contact proofs or widening their format.

## Exactly one recommended first implementation ticket

**Prepare finite-propellant segmentation from one genuine prepared engine proposal.**

One spacecraft, one cold-initialized canonical resource source, one ideal available-feed M14.23 engine, one exact canonical interval. Introduce a validated resource/model identity, exact bounded usable amount, one issued dependent resource proposal and copied conditional observation. At most one powered segment and one unpowered remainder.

The initial cold resource source must be consistent with independently authored dry-body mass/inertia and the selected CentralPointReservoirV1 law. It may not alter a previously created physical body's mass to make attachment pass. Test resource state can be built alongside a mass-consistent synthetic physical fixture; do not reinterpret the M14.21 article or change its tests/geometry.

Inputs: owner-issued source resource, source ResourceRevision, genuine active engine token, same exact interval/physical/command/clock/configuration authority. Outputs: exact source/required/consumed/successor amounts, exact powered/unpowered durations, exhaustion classification/witness, conditional body wrench history, source/endpoint mass-law observation and copied provenance. Exact flags remain meaningful when optional FP64 projections round to zero.

New resource preparation may factor M14.23's source validation into an identical in-owned-phase read helper, preserving its externally guarded API, semantics, edge-capture behavior and tests. This narrow future implementation authorization must be explicit. No other engine/command change is justified by resource ownership.

On refusal: no resource reservation/consumption, no lost engine proposal, no changed canonical state. On success: private resource-proposal seal only. No live canonical ResourceRevision increment, ActuatorRevision publication, fuel commit, physical step, mass mutation, force/torque application, BEPU call or timeline event. The source component may exist canonically from cold construction; this is not permission for a standalone burn/refuel writer.

First-slice duplicate semantics: at most one active resource lease; repeated prepare refuses without replacement. Explicitly discarding that resource lease leaves the engine token active; re-preparation from the same still-valid source yields equal conditional values with a fresh private lease. Retiring the engine token invalidates dependent resource output. No private lease/generation enters deterministic canonical history.

The proposal should state `IfApplied`/`Proposed` throughout. Engine Enabled/Off, hardware Unavailable, feed Empty and proposed powered history remain separate. No actual firing, consumed fuel, completed physical interval or executed event claim.

## Qualification bar for that future ticket

Use cheap exact arithmetic and authority tests before broader regressions. Independent test-only BigInteger rationals may serve as an oracle; production uses the fixed bounded representation.

| # | Required witness |
|---:|---|
| 1 | Zero propellant with positive demand: full unpowered, zero consumption, no repeated exhaustion event |
| 2 | Exact U=Vn>0: full powered interval, empty exactly at end |
| 3 | Slightly more than required, including one exact ledger unit: positive remainder |
| 4 | Ordinary non-integral interior exhaustion and exact complement duration |
| 5 | Tiny positive powered duration below binary64 seconds range; exact segment remains present |
| 6 | Interior exhaustion extremely near end, including exact one-unit deficit |
| 7 | Zero throttle: no demand; requested/proposed latch unchanged |
| 8 | Off engine with stored throttle: no demand, no fuel change |
| 9 | Unavailable hardware: no demand, accepted ignition history retained |
| 10 | Legitimate zero flow; distinguish and refuse positive-thrust/zero-flow underflow |
| 11 | Negative/nonfinite resource/flow, wrong amount encoding, signed-zero treatment |
| 12 | Duplicate, old, canceled, stale, default and fabricated proposal refusal |
| 13 | Foreign resource/engine/spacecraft/model/version/interval mismatch |
| 14 | Exact successor/debit oracle and same total debits under equivalent partitions |
| 15 | No negative amount or overdraw; no post-hoc clamp |
| 16 | Same accepted source bits yield identical segment/event/value provenance |
| 17 | No physical/resource/command/clock/debt/revision/history/BEPU mutation during preparation |
| 18 | Forced final pre-seal refusal preserves source and active engine dependency |
| 19 | Event value copy is no execution right; canceled/consumed lease cannot replay |
| 20 | Second independent consumer/outstanding proposal cannot claim the same source resource |

Additional necessary guards:

- all finite exponents/subnormals, largest mass/flow and maximum admitted integer duration;
- fixed-limb capacity proof and independent decode/multiply/compare/subtract boundary checks;
- repeated tiny positive debits that FP64 subtraction would lose, with exact conservation;
- U=V=C=0 handled before equality, producing no fake powered/exhaustion result;
- source mass correctly rounded from exact dry + exact usable amount, not double rounded or double counted;
- engine remains usable after resource refusal; engine retirement/source/clock mutation invalidates resource preview;
- owner thread and reentrancy; future admission/preview uses the genuine same-owner capability;
- future general rational cross products are not silently passed to UInt128 arithmetic;
- no root-space frozen thrust or averaging, no frame/camera/host-clock influence;
- existing M14.21/22/23 tests and fingerprints remain protected except the explicitly equivalent owned-phase reader factoring.

Target exact zero warmed allocation for source validation, exact arithmetic, segmentation, preview, refusal and complete preparation, with the existing checked helper and deliberate positive control. Retain bounded storage and report its composition. At most two fixed segment values; no heap event, collection or unbounded history. After correctness, use one bounded ordinary-runtime preparation-cost measurement separately from allocation; do not infer a dynamics performance result or invent an unrequested threshold.

The investigation has not implemented or run these future gates. No broad allocation, performance or renderer campaign belongs here.

## Future physical qualification prerequisites

Before any powered application, separately qualify: exact-segment consumption including tiny events; continuous changing-mass/body-wrench integration and exhaust momentum assumptions; valid paired/mass successor realization; retained-contact dynamic force/mass/inertia admission without world reconstruction or stale-check weakening; and a joint canonical publisher/acknowledgement for all successors. The existing constant-mass integrators and M14.19 fixed-property binding do not satisfy this automatically.

A future owner cannot start an admitted finite-fuel interval and then stop because its legitimate depletion time is not an integer tick or positive FP64 duration. The consumer must support its exact model before powered admission. The exact nonphysical segmenter does not refuse such depletion.

## Florida launch dependency order

`M14.22 commands → M14.23 prepared engine demand → finite-resource exact segmentation → dynamic body-wrench/changing-mass numerical qualification + physical-regime admission → joint resource/actual-actuator/mass/physical publication with private acknowledgement → powered retained-contact fixture → departure/free-flight ownership transition → qualified pad/terrain and world entry/continuity → Florida player ignition/liftoff → controllable ascent`.

Gimbal/RCS/attitude control realization is a separate parallel prerequisite for controlled ascent; it need not wait for Florida scenery or be mixed into fuel work. Vehicle/atmosphere/feed applicability and finite-frame lifetime must also be qualified before the production Florida path. Neither gimbal nor final artwork is needed to prove this first scalar-resource segmenter. No task after the recommended nonphysical preparation slice is started by this assessment.

## Evidence closure for later banking

Accepted fingerprints → exact intended stage set → all evidence dependencies/links present **inside that staged tree** → staged-tree whitespace check → exact staged-tree build → authorized commit/tag → atomic push → remote verification. Preserve historic tags and accepted provenance. This future process is recorded here; no staging or banking occurs in this investigation.
