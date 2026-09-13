# Persistent BEPU contact publication candidate

Project Control subsequently accepted this accomplishment as **M14.19 — Persistent
BEPU Contact Publication**. Banking remains pending. The qualification-stage report
below, including its then-current acceptance status, is retained unchanged as evidence.

## Baseline / branch

Baseline: `30ccb218859811cb5185aee7a55669d23b1ddf26`, **Docs: refresh NovaCore project presentation**.
Its parent is banked M14.18 `e66d3a5d30dab14a60f28e9cc77a0c854ad53127`, tag
`m14.18-staged-finite-body-bepu-contact`. HEAD/main/origin/main and remote main still
resolve to the baseline. Candidate branch: `codex/bepu-persistent-contact-publication`.
No candidate commit, staging, tag, merge, push or milestone assignment was performed.

## WHAT HAPPENED

Implemented the sibling engine publisher, an opt-in retained-world publication binding,
genuine sealed receipts, dedicated fixed-capacity history, copied observations and a
distinct committed-but-private-invalidated terminal outcome. Qualification passed on
2026-09-13. This is an unbanked candidate awaiting Project Control acceptance.

## WHY IT MATTERS

One retained qualification world now repeatedly steps, exports, publishes and acknowledges
an endpoint without reconstructing its body, manifold or warm-start state. NovaCore owns
the canonical result; BEPU remains private solver state. Support is proven at each exact
published endpoint, not between endpoints or through future constant-force extrapolation.

## Architecture judgment

**CREATE SIBLING PERSISTENT-CONTACT PUBLICATION BOUNDARY**.
The implementation follows the approved assessment, **KSA-CONVERGENT — ADAPT**.

## M14.17 separation proof

`SimulationTransactionEngine.Continuation.cs`, `CertifiedContinuationPublication.cs` and
`ContinuationPublicationPhase.cs` are unchanged from the baseline. M14.17 still reads genuine
endpoint and `EventFreeThroughTarget` evidence and rejects absent/stale/foreign clearance.
Its request type cannot accept a BEPU receipt. Its history and result types remain separate.
Three existing M14.17 focused groups passed in each configuration, including exact paired
publication, admission gaps, replay, complete event snapshots and allocation paths.

## Shared atomic-publication primitives

Both publishers use the existing `ContinuationPublicationPhase`,
`SimulationState.TryPrepareContinuationSlot`, `SimulationState.InstallCertifiedContinuation`,
`SimulationClock.InstallCertifiedContinuation` and `ContinuationClockState`.
The existing fixed-install method names are retained to avoid semantic churn; the prepared
paired slot and clock writes have the same responsibility in both callers.

The sibling acquires the same owner phase **before** preparation. Receipt/authority/history/
arithmetic preparation, final applicability recheck, canonical fixed writes and private
acknowledgement all remain inside it. No second lock or callback exists. Other supported
canonical mutation APIs reject reentrancy; live canonical reads remain owner-thread only.
The sole production call site that prepares/commits the private acknowledgement is the
sibling engine publisher. Its prepared acknowledgement is never returned to callers/history.

## Persistent episode immutable state

Original `LocalContactSource` is never rewritten. It retains the source pair, coherent initial
motion, initial T0, admitted end, configuration, frozen force/inertia, and source engine.
The world retains the same solver, body/static shape, pool, generation and receipt seal.
The configuration retains original origin, constant origin velocity and fixed orientation.
The cold binding captures a deterministic engine-local episode sequence and copied source/
configuration value provenance. Process-global world generation is excluded from history.

## Expected canonical authority / acknowledgement state

The separate binding contains expected raw translation/rotation, physical properties,
spacecraft definition, StateRevision, TimelineRevision and the entire clock/debt/rate/
remainder/pause snapshot. Acknowledgement updates only raw paired state, StateRevision,
clock/debt, acknowledged frontier and pending status. Sequence equals acknowledged frontier
because every private interval is published exactly once in this mode.

Debt must be provisioned **before** binding. External debt/rate/pause mutation is an authority
conflict, not an implicit rebind. This is the issued strict episode contract, not a live host
debt-admission integration. Original immutable T0/frame state never advances during ack.

## One-outstanding-endpoint contract

Only a fresh, genuine frontier-zero world can opt in through `BeginPersistentContact`.
After Step succeeds, Pending blocks another solver step. Read can copy that stored endpoint;
publication consumes it once. Success enables the next interval with the same receipt's
acknowledged frontier. Refusal neither steps again nor silently drops the pending endpoint.
Private-only mode continues using the original immutable source validation.

## Exact tick schedule preservation

All three long sequences verify every ordered target against
`T(n) = T0 + floor(n * 1_000_000 / 60)` and every interval against
`16,666 → 16,667 → 16,667`. Each 1,200-step sequence contains 400 short and 800 long
intervals. Total advancement is exactly 20,000,000 ticks. Publication never resets T0.
Additional tests cover near-minimum/maximum canonical ticks, maximum retained debt and
preservation of nontrivial rate/remainder/pause fields.

## Moving-frame epoch preservation

The third 1,200-step sequence uses T0=1,234,567 ticks, origin `(1e9,-2e9,3e9)`, constant
origin velocity `(11,-7,3)` m/s and a nonidentity fixed orientation. All eight corners are
reconstructed independently against `Origin + Velocity * (target-T0)` and the original plane.
Its private-only comparator has identical raw physical bits at every endpoint. No canonical
double is imported back into BEPU during publication or acknowledgement.

## Publication admission contract

Requires genuine owner/seal/generation/frontier receipt, one pending endpoint, live valid
world, matching engine/configuration/raw pair/properties/model/revisions/clock, exact next
scheduled target, no pending event through target, sufficient debt, checked successor
arithmetic, fixed paired slot and history capacity. Prepared acknowledgement resolves its
binding and copied successor values before writes. Final live checks occur under the same phase.

## Event-at/before-target rejection

Both an event before target and one exactly at target return PendingEvent. Pending event
values and counts remain unchanged. Publication schedules/consumes no events and does not
increment TimelineRevision. Unrelated timeline mutation also refuses, even beyond the interval.

## Exact staged endpoint publication

Publisher reads the world's stored issued export, not arbitrary caller endpoint values.
It installs exact FP64 position/velocity, staged quaternion and body angular-velocity bits;
both segment epochs become exact target. No normalization, force evaluation, solver call,
repropagation, reconstruction or history construction occurs in the fixed-write portion.
Downstream observation/evaluator normalization remains a separate existing boundary.

## Revision behavior

N successful intervals produce exactly N StateRevision increments. Even supported resting
endpoints get one successor revision. Duplicate, old and consumed receipts add no revision.
TimelineRevision remains unchanged. External numerically identical marker mutation still
increments revision and is rejected as stale authority.

## Clock/debt behavior

Time becomes exact target and debt decreases by the exact admitted integer interval. Int128
checks reject invalid/nonpositive/oversized deltas; negative/insufficient debt and revision
overflow refuse before writes. Admitted M14.18 intervals cannot themselves exceed 16,667
ticks; extreme-source tests confirm no overflow at representable clock/debt limits.
Rate, remainder and pause state survive publication unchanged.

## History behavior

One `ProcessedPersistentContact` record per interval, in a constructor-preallocated array.
Version, index, deterministic episode, frontier, before/after raw pairs, properties,
clock snapshots, revisions and timeline are copied values. Record size: 744 bytes; 1,200
slots contain 892,800 payload bytes. There are no managed references, pointers, world
handles, receipt capabilities or global generation identifiers. No growth, dropping or
wrapping is implemented. Exhaustion refuses before writes. An 80-interval independent
publishing replay produces identical history despite different global world generations.

## Same-phase acknowledgement

Fixed acknowledgement writes only prequalified binding fields after canonical fixed commit,
before releasing the owner phase. It never rebuilds Simulation/shapes, resets generation or
schedule/frame epoch, clears contact caches, imports state, changes configuration or steps.
The returned observation is a copied value with target/revision/episode/frontier/history index.

## External mutation rejection

Permanent tests exercise changed revision, raw position, mass, inertia, force, torque,
rotation model, spacecraft definition, configuration reference, clock, debt and timeline.
No automatic rebind occurs. Any changed expected authority remains a conflict for that
episode. A malformed request does not poison an otherwise sound pending world.

## Duplicate/old/fabricated receipt rejection

World identity, cold private issuance seal and captured generation supplement exact frontier.
An internally constructed `Receipt(world, currentStep)` without the seal is invalid.
Default, foreign-world/body/generation, modified generation/frontier, initial/older and
already-consumed copies are refused. One receipt copy is usable once, not a second authority.

## Precommit refusal nonmutation

Every ordinary refusal compares copied canonical pair/properties/revision, complete clock,
timeline, pending event value/count, domain history counts and last persistent history record.
Tests include an existing published prefix. An erroneous caller clock expectation can be
corrected and the same sound pending receipt retried. Capacity/debt refusal retains its
readable staged endpoint, without automatic debt repair, reconstruction or re-step.

### Required failure-matrix mapping

| # | Responsibility | Permanent witness |
|---|---|---|
| 1 | Stale StateRevision | Identical marker write still refuses |
| 2 | Stale TimelineRevision | Later unrelated event refuses |
| 3 | External canonical mutation | Raw translation and model-definition changes |
| 4 | Configuration change | Foreign immutable configuration reference |
| 5 | Mass/inertia | Separate altered-property and inertia fixtures |
| 6 | Force/torque model | Separate force, torque and rotation-model changes |
| 7 | Foreign engine | Both engines' canonical snapshots unchanged |
| 8 | Foreign world | Genuine receipt from other world rejected |
| 9 | Wrong generation | Test-only corrupted captured generation rejected |
| 10 | Wrong frontier | Test-only modified frontier rejected |
| 11 | Default/fabricated receipt | No seal cannot manufacture authority |
| 12 | Duplicate | No second commit/revision/debt/history |
| 13 | Older | Initial receipt after later committed interval |
| 14 | Consumed | Current receipt after successful publication |
| 15 | Event before target | PendingEvent, complete event snapshot unchanged |
| 16 | Event exactly at target | PendingEvent, including after a published prefix |
| 17 | Clock mismatch | External clock and incorrect request expectation |
| 18 | Insufficient debt | Zero debt refuses; staged endpoint remains readable |
| 19 | Debt arithmetic | Negative debt refuses; long.MaxValue debt/extreme ticks safely subtract exact intervals |
| 20 | Revision overflow | ulong.MaxValue source revision refuses |
| 21 | History capacity | Zero-capacity fixed array refuses before writes |
| 22 | Wrong owner | Foreign-thread call refuses |
| 23 | Reentrant call | Existing owner phase prevents nested publication |
| 24 | Disposed world | Receipt cannot publish after disposal |
| 25 | Invalidated world | Unsafe advanced world cannot publish/retry |
| 26 | Existing solver/export failure path | Excess force produces post-step precision/export failure and poisons private continuation; existing solver catch retained |
| 27 | Sound refused endpoint | Incorrect request corrected; same pending endpoint successfully publishes |
| 28 | Commit then ack failure | Internal fixed failure seam; full committed tuple/history remains; continuation terminally invalid |

No production failure-injection framework was added. Reflection is confined to permanent
test setup for impossible-to-author normally immutable/corrupted fields. Expected refusal
remains typed status, not exception.

## Committed-but-private-invalidated terminal path

The narrow internal test entry forces failure immediately after canonical writes. Result is
`CanonicalCommittedPrivateInvalidated`, with `CanonicalCommitted=true` and copied committed
observation. Both raw velocity tuples/poses/epochs, revision, debt and actual history entry
remain committed. Private continuation becomes invalid and both publication/step retries
refuse. No rollback, blind acknowledgement retry or reconstruction occurs. The production
path has only fixed prepared assignments, with the same terminal handling for an unexpected
acknowledgement exception. Canonical history records publication, not a false claim of ack.

## Private-only vs publishing trajectory comparison

Separate equivalent engines/worlds compare all physical fields and explicit FP64 bit
identities. Revision and global generation are deliberately different provenance. All
3,600 paired endpoints match exactly on this build/machine. Cross-platform bit identity
or a general BEPU serialization/replay guarantee is not asserted.

## Centered 1,200-step result

PASS. Geometric peak penetration 0.005455926960645563 m; manifold peak
0.005455954000353813 m; final support 600/600; support height drift
5.960464477539063e-8 m.

## Tilted 1,200-step result

PASS. Geometric peak 0.004164378500802757 m; manifold peak 0.004164367914199829 m;
support 600/600; drift 5.960464477539063e-8 m. Angular response is explicitly exercised.
Moving/tilted variant: peak 0.004164354113830093 m; support 600/600;
drift 4.0696466752088156e-8 m.

## Final 600/600 support evidence

Every final interval checks independent lowest-corner separation and center height within
2 mm, relative speed <=0.06 m/s, plus persistent manifold/constraint evidence. Whole-run
geometric and manifold penetration <=20 mm; final ten-second center-height drift <=1 mm.
These remain the qualification box/slab bounds, not production spacecraft or terrain policy.

## M14.17 regressions

Debug/Release PASS: certified Florida continuation publication, certified continuation
acceptance gaps and certified continuation allocation matrix. Existing ownership group
also passes in both full Simulation suites. No clearance or result/history semantics changed.

## M14.18 regressions

Debug/Release focused and full-suite PASS: private-only admission, centered/tilted support,
exact intervals, source-authority isolation, moving-frame precision, post-step invalidation,
zero warmed allocation and bounded storage. Original staging tests/callbacks are unchanged.

## Debug build

Full solution PASS, zero warnings/errors. Full Simulation 50/50 groups PASS. New focused,
ReferenceFrames, Precision and verified BEPU dependency checks PASS.

## Release build

Full solution PASS, zero warnings/errors. Full Simulation 50/50 groups PASS. New focused,
ReferenceFrames, Precision and verified BEPU dependency checks PASS.
SDK 10.0.303; net10.0 default runtime selection on a host with latest installed .NET 10.0.12.
No profiling, tiering, PGO or GC override was introduced.

## Zero-allocation result

Exact **0 bytes**, Debug and Release, for 1,024 complete operations after 128 warm complete
operations. Measurement is the existing checked 1 MiB temporary no-GC test helper; entry
and exit pass. Timing uses separate fresh normal-runtime fixtures. Shared deliberate
`byte[128]` control detects **152 bytes**. No subtraction, tolerance or retry.

## Retained storage

Combined conservative upper bound **1,411,688 bytes <=8,388,608**:
458,752 native pool bytes + 952,936 managed setup/warm allocation upper bound.
Managed total includes 892,800 bytes of fixed history payload and at most 60,136 other
setup/world/binding bytes. Allocation accounting deliberately overcounts temporary setup;
this is a proven retained-storage upper bound, not a heap-dump claim of exact liveness.
Native pool size is unchanged through the measured complete-path loop.

## Performance

Three fresh Release processes, each 128 warm then 1,024 measured **complete** operations.
All measurements include private step, export, admission/preparation, canonical commit,
acknowledgement and copied result. No construction or no-GC region is in the timed window.

| Process | Median ms | P95 ms | P99 ms | Max ms | Result |
|---|---:|---:|---:|---:|---|
| 1 | 0.0161 | 0.0183 | 0.0245 | 0.1152 | PASS |
| 2 | 0.0163 | 0.0189 | 0.0329 | 0.1773 | PASS |
| 3 | 0.0163 | 0.0168 | 0.0276 | 0.1112 | PASS |
| Ceiling | 0.05 | 0.10 | 0.25 | 0.50 | All satisfied |

Aggregate summary is the range of process statistics, not pooled/invented percentiles:
median 0.0161–0.0163, P95 0.0168–0.0189, P99 0.0245–0.0329, overall maximum 0.1773 ms.
4/4/5 samples exceeded 0.05 ms, one per process exceeded 0.10 ms, none reached 0.25 ms.
Worst sample indices were 394/543/507; bounded top-eight samples are in results.json.
Tails occur at different indices and remain inside every declared gate. Their mechanism
was not attributed; no profiler or second campaign was justified. No retry occurred.

## Cold lifecycle costs

Fixture plus canonical/history setup: 12.7229 / 12.3897 / 12.4501 ms.
World creation: 30.8003 / 30.4980 / 30.6268 ms.
Publication binding: 0.6985 / 0.6927 / 0.7368 ms.
These are fresh-process lifecycle timings including JIT/setup where encountered; the
fixture/history total is not attributed solely to the new history allocation.

## Runtime/render impact

No renderer work; no display-frame multiplication; no production terrain integration;
no production spacecraft collider integration. No live gameplay caller was added.
The box remains qualification-only. No extrapolation beyond a published contact endpoint
is certified. No dependency version/hash/trust changes were made.

## KSA responsibility comparison

Preserves the approved source-backed assessment (no architecture reopening): retained solver/
manifold continuation **ADOPT**; game-owned solved-state application **ADAPT**; ready/apply and
failure poisoning **ADAPT**; exact NovaCore revision/debt/history **INTENTIONALLY DIFFER** for
NovaCore canonical determinism; same-phase private acknowledgement **ADAPT** to its owner phase.
No equivalent KSA revision/debt/history contract is claimed. Assessment/provenance remains at
`E:\NovaCore-Assessments\persistent-contact-publication\assessment.md`; installed current KSA
and official engineering history were assessed there. This implementation did not repeat
source extraction or Discord research and does not depend on those artifacts at runtime.

## Production files changed

- `Spacecraft/Contact/Staging/LocalContactSource.cs`: appended pending-publication status.
- `Spacecraft/Contact/Staging/LocalContactWorld.cs`: opt-in validation, sealed receipt and pending endpoint hooks.
- `Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs`: immutable episode vs acknowledged binding and prepared ack.
- `Transactions/PersistentContactPublication.cs`: separate deterministic value types/statuses.
- `Transactions/SimulationTransactionEngine.PersistentContact.cs`: sibling admission/commit and dedicated journal.
- `Transactions/SimulationTransactionEngine.cs`: optional preallocated persistent history capacity.

All production paths above are under `src/NovaCore.Simulation/`. No other runtime subsystem changed.

## Permanent tests added/changed

`tests/NovaCore.Simulation.Tests/PersistentContactPublicationTests.cs` adds the complete
physical/refusal/terminal/bit/history/allocation/storage/performance witnesses.
`Program.cs` adds focused selectors and appends one full-suite group; previous group order
and all predecessor tests remain unchanged. Tests use the existing qualified fixture shape
and allocation helper. No test threshold was relaxed or obsolete gate removed.

## Evidence retained

This report, `results.json` and `identity.json`: contract, measurements, review findings,
candidate source/binary hashes, baseline provenance and reproducible commands. No duplicate
source trees, observer copies, raw benchmark arrays, build logs or bulk diagnostics are retained.

## Reproduction

Use the fingerprinted working candidate on the baseline, SDK 10.0.303 and the qualified
repository BEPU binary path. Ordinary project build output is rebuildable, not evidence.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet build NovaCore.sln -c Debug --no-restore -v:q
dotnet build NovaCore.sln -c Release --no-restore -v:q
foreach ($configuration in @('Debug','Release')) {
  dotnet "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll" --persistent-contact-only
  if ($LASTEXITCODE -ne 0) { throw 'Persistent qualification failed' }
  dotnet "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
  if ($LASTEXITCODE -ne 0) { throw 'Simulation failed' }
  dotnet "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll" --local-contact-staging-only
  if ($LASTEXITCODE -ne 0) { throw 'Private staging failed' }
  foreach ($case in @('Certified Florida continuation publication','Certified continuation acceptance gaps','Certified continuation allocation matrix')) {
    dotnet "tests/NovaCore.Graphics.Tests/bin/$configuration/net10.0/NovaCore.Graphics.Tests.dll" "--case=$case"
    if ($LASTEXITCODE -ne 0) { throw 'Certified continuation failed' }
  }
  foreach ($project in @('NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests')) {
    dotnet "tests/$project/bin/$configuration/net10.0/$project.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Regression failed' }
  }
}
foreach ($run in 1..3) {
  dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --persistent-contact-performance
  if ($LASTEXITCODE -ne 0) { throw 'Stop on first performance failure' }
}
git diff --check
```

## Independent reviews

Atomic reviewer: PASS. Checked common owner phase, prepared fixed targets, same-phase ack,
genuine receipt boundary, M14.17 separation and asymmetric failure semantics.
Episode/test reviewer: PASS. Checked original T0/frame, trajectory bits, exact sequences,
support, deterministic history, complete refusal snapshots and measurement boundaries.
Both were read-only. The lead alone wrote candidate files and executed validation.

## Disposable diagnostics removed / remaining

New disposable investigation trees/files: **0 created, 0 remaining**. Tool-captured output
was reduced directly to concise results; no diagnostic cleanup was needed or attempted.
The pre-existing untracked `bepu-local-contact-staging/staging-build-blocker.md` (5,366 bytes)
is untouched and excluded from this candidate. No unrelated cleanup occurred.

## Git/ref state

Candidate remains on the new bounded branch; nothing staged. Main, origin/main and remote
main remain baseline. Historical tags were not modified. `git diff --check` passes.
Source/binary fingerprints and unchanged responsibility witnesses are in identity.json.

## Lead judgment

**PASS**

## Final classification

**PERSISTENT BEPU CONTACT PUBLICATION — READY FOR PROJECT CONTROL ACCEPTANCE**

**UNBANKED. STOP FOR PROJECT CONTROL.**
