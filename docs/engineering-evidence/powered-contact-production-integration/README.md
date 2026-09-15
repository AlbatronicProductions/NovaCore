# Powered-contact production integration — stopped at performance gate

> Historical first-ticket report below, preserved unchanged. Project Control subsequently
> accepted the ready/apply correction and resolved the proposed local performance targets.
> Current qualification: [final-qualification](final-qualification/README.md). Historical
> failures remain authoritative measurements, not the current acceptance contract.

**Lead judgment: REVISE. UNBANKED. STOP FOR PROJECT CONTROL.**

The first fresh Release performance process failed the complete-path ceiling. No further
performance process, implementation correction, broad qualification, or visible route was
started after this failure. The implementation is a preserved **unqualified draft**.

## Baseline and scope

HEAD/main/origin/main/queried remote main remain
`49057fecceb0f725d5f551ec40e2780971b0d81d`; branch
`codex/powered-contact-production-integration`. All 65 historical tags are unchanged.
M15.0 remains `4607d8c802006d5e1a01c595ab608cf53a4dab6b`. Nothing staged, committed,
pushed, merged, tagged or banked. No milestone assigned. Existing prerequisite evidence and
five pre-existing diagnostic project roots remain untouched.

Accepted design: [production integration](../powered-contact-production-integration-design/architecture.md),
[source-mass decision A](../powered-contact-production-integration-design/mass-stop.md), and
[closed numerical handoff](../powered-contact-ordinary-step-event-closure/PRODUCTION-DIRECTION-HANDOFF.md).
This ticket does not reopen those decisions or Candidate A.

## What the draft implements

One shared transaction-owned resource/actuator/physical episode, with an explicit free-flight
or retained-contact consumer. The M15.0 changing-mass free-flight evaluator is unchanged.
The contact path holds acknowledged source mass through one ordinary retained BEPU step;
central-point fuel changes total mass while dry inertia remains (2,2,2).

`PoweredContactPreparation` owns cold resources privately: fixed qualification box/slab,
120 gravity preparation steps, then the retained fixture's explicit initial-rate assignment.
Its copied prestate constructs initial canonical records. A single successful binding transfers
the existing world/body/pool and clears preparation's native references. This is initial
preparation, never live free-flight acquisition or pose reimport. Cold moving-frame round-trip
errors are measured and admitted against the existing contact precision envelope.

Genuine command/engine/resource leases prepare the exact powered fraction. The new mapper
uses banked fixed-workspace ratio primitives and complete weighted products before FP32
transport. Exact ledger values and tiny-event identity remain exact. The mapped acceleration
contains gravity once; the callback applies that value and does not add gravity again.

The source epoch and moving-frame epoch remain original. Public time remains 1,000,000 ticks/s;
the accepted 60 Hz lattice supplies 16,666/16,667-tick ordinary intervals. No event-sized dt,
extra substep, cache scaling, load correction, Candidate-A solver, or increased iterations.

Only the named finite fixtures are admitted, with fixed initial throttle/latch sequences;
there is no continuous envelope inferred from extrema. The derived centered control has
fuel 33333/2^30 kg and throttle 78125/2^17: full power to16666, exact exhaustion at33333,
then no-feed continuation. This supplements, rather than relabels, the ten retained cases.

## Atomicity and continuity

Preparation checks owner/source/clock/event/debt/history/revisions before native mutation.
The staged native endpoint is sealed once. The engine prepares successor mass/resource,
actual actuator, endpoint slot, clock/debt, one revision and one history record before fixed
canonical writes in the existing owner phase. Final admission compares endpoint bits,
including native identity/time and separately validated resource-successor properties.

Fixed acknowledgement updates expected physical/resource/clock/frontier state only after
canonical commit. No native body reconstruction, pose/velocity write or solver call occurs
there. Before the next native solve, supported `SetLocalInertia` updates the same dynamic body;
property-only tests preserve pose, velocity, handle, constraint and cached impulse bits.
No manual warm-start reset/rescale. The earlier unpowered persistent-contact publisher refuses
powered-world binding, preventing a physical-only publication that bypasses fuel authority.

Precommit refusal spends no fuel. A pending sound endpoint can retry publication without
stepping again. Retiring a native proposal invalidates continuation; it cannot be recomputed
like a pure free-flight proposal. Postcommit acknowledgement failure preserves the committed
bundle/accounting, consumes leases, and explicitly returns committed/private-invalidated.

## Completed focused evidence

Debug and Release Simulation **project** builds pass, zero warnings/errors on the final
build of each configuration. Implementation-time type/import errors were corrected before
the affected tests ran; they were not qualification retries. These are not full-solution builds.

Both configurations pass the six focused routes: cheap, physics, work, authority, sequences,
allocation. [validation.json](validation.json) retains measured values and exact scope.

| Witness | Result |
|---|---|
| Literal 1/60 fixed-workspace projection replay | All six retained native FP32 bits match |
| Genuine resource/physical sequence | FullPowered16666; EndpointExhaustion33333; NoFeed50000 |
| Eleven first-interval physical cases | All frozen physical/support/impulse limits pass |
| Maximum velocity error | .005552701937765838 m/s <= .06 |
| Maximum angular-rate error | .004255174893994246 rad/s <= .0489897948557 |
| Maximum displacement error | .00017038024756332783 m <= .001 |
| Maximum orientation error | .0007618612707889043 rad <= .000816496581 |
| Maximum independent penetration | .00042975076742157725 m <= .001 |
| OffCOM tangent/twist work error | .004177657978519753 / .0006884086400016885 J, each < .07848 |
| Yaw baseline tangent/twist work error | .001671110138672799 / .00000032028185372595663 J, each < .07848 |

The friction witness preserves **ordered discrete impulse-work approximation**, not continuous
friction history. The test-only processor observes the original 54 applications/108 edges;
native row arithmetic is recovered exactly by the retained generator. It calls the pinned
[BEPU row implementation](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs).
Complete represented world inertia is inverted in the exact test oracle. Energy and signed
impulse sums telescope exactly; observed/unobserved committed/native bits match. This observer
is absent from allocation/performance work and does not change the dependency DLL.

CenteredEndpoint and OffCOM each pass 1,200-step comparisons under30/60/150/240Hz and delayed
backlog partitions, plus the constant-moving-frame control. Each has400 short/800 long
intervals, exactly20,000,000 ticks,1,200 revisions/records, unchanged TimelineRevision,
zero final debt, exact resource depletion, original frame epoch, and600/600 final independent
supported endpoint checks. Backlog reaches the four-step budget200 times without lost work.
This establishes deterministic same-build continuity, not arbitrary long powered accuracy.

Authority tests cover invalid/zero/duplicate/overflow host inputs, insufficient debt, wrong
thread/reentrancy, foreign and stale canonical/resource/world identity, generation/frontier,
event-at/before-target, revision/history capacity, pending/default/fabricated/duplicate leases,
endpoint mismatch, retired parent/resource, unqualified command sequence, both terminal
acknowledgement paths, disposed world, and an injected post-step export failure. Snapshot
checks distinguish precommit nonmutation from already-committed canonical effects. The full
required matrix and normal suite integration remain subject to final review, not accepted here.

Five allocation windows each measure **0 bytes**, checked no-GC entry/exit PASS: complete dry
continuation, interior exhaustion plus dry successor, no-work, backlog, and pure mapper.
The independent byte[128] control measures **152 bytes** in Debug and Release.

## First material failure and stop

[The plan](performance-plan.md) declared three fresh Release processes. Only process1 ran.
The cold measured managed-allocation upper bound was4,942,232 bytes; exclusive native pool
458,752 bytes; combined5,400,984 bytes versus8,388,608. History accounts for4,857,624 bytes,
including1,200 records of4,048 bytes. Cold construction took2.0944ms. This is a conservative
construction/ready-storage bound, not total process working set or a completed peak-lifetime campaign.

| Process1 complete control | Measured ms | Ceiling ms | Result |
|---|---:|---:|---|
| Median | .1464 | .05 | FAIL |
| P95 | .1854 | .10 | FAIL |
| P99 | .2448 | .25 | PASS |
| Maximum | 1.0729 | .50 | FAIL |

128 warm +1,024 measured operations, worst sample957, GC collections0/0/0. This is a broad
complete-operation budget failure plus a worst outlier, not proof of one particular tail.

**Population correction:** raw output says `dry-supported-retained`, but code used
`CenteredBaseline`: engine OFF with1.3020833333333334e-5kg remaining fuel. It is an unpowered,
still-fuelled control, not an exhausted/no-feed dry trajectory. The failed label is preserved
and corrected in evidence; stopped source was not edited or rerun. The complete-path ceilings
still fail. Genuine exhausted/dry timing, powered event timings and pure preparation timings
were **not reached**.

No measured subphase establishes cause. Do not assign the cost to BEPU, exact arithmetic,
copying/history size, tiering, GC or scheduling. Zero counted collections do not establish any
other mechanism. The historical Boundary-B preparation P95~.26ms/max~.30–.38ms remains
**UNATTRIBUTED**; this different complete-control population does not prove the same mechanism.

No optimization/correction, second process, retry-to-green or profiler was attempted.

Requested tail classification: **PARTIAL** (complete-operation slowdown observed; historical
preparation-tail population/cause not reached or established). Disposition: **REVISE**.

## Work not reached

- Processes2/3 and event/exhausted-dry/preparation performance populations.
- New visible development route; no presentation/manual-acceptance claim.
- New focused gates' registration in the ordinary full Simulation runner (focused CLI routes exist).
- Full Debug/Release solution builds and full Simulation suites.
- Focused protected M14.21–M15.0, M14.17–20, ReferenceFrames/Precision and dependency regressions.
- Final complete required refusal-matrix reconciliation and acceptance red team.

Banked numerical implementations/thresholds were not intentionally changed, but regression
preservation is **not dynamically qualified** by the focused candidate tests alone.

## KSA provenance

Current available installation `E:\Kitten Space Agency`, KSA2026.9.10.5438
(`+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`) rehashes identically to the accepted design,
including both BEPU DLLs and deps.json. Reused the accepted direct current-method review
and actual authenticated #live-changelog history (server1260011486735241329,
channel1260112103134724146, revisions4549/5177/5434). This ticket explicitly authorizes
reuse when identity is unchanged; no new KSA research was needed. Exact hashes and source
links are in [the accepted review](../powered-contact-production-integration-design/ksa-current-review.md)
and [candidate identity](identity.json). Source-mass policy remains **A**, only within the named domain.

## Handoff

Read-only independent review confirmed the stop, corrected the timing population description,
and identified the unregistered normal-runner/visible-route/regression work still outstanding.
Earlier draft admission gaps were closed before qualification: resource-capability leakage,
cross-consumer publication, command sequence widening, frame import precision and endpoint-bit
comparison. No further production edit followed the performance failure.

Project Control should decide one bounded complete-operation cost classification, while preserving
the failed measurement, before authorizing further implementation/qualification. No cost cause
or repair is asserted. No departure, Florida, terrain, renderer, asset or gameplay work occurred.

See [reproduction and file map](reproduce.md), [identity](identity.json), and
[exact disposable inventory/manual cleanup](cleanup.md). Preserve this draft **UNBANKED**.

Final integrity: all17 stopped source/test fingerprints still match; current refs and65 tags
are unchanged; nothing staged. Tracked `git diff --check` and cached diff check pass.
The separate untracked-file audit found10 upstream trailing-whitespace lines in generated
`PoweredContactTests.NativeRows.cs` (11,22,23,48,51,53,86,88,93,97). These are explicitly
unresolved hygiene items; no stopped source or recovery contract was silently rewritten.
Full candidate whitespace readiness is therefore not claimed. See [verification](verification.json).
