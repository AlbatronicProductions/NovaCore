# Unified contact transaction candidate evidence

2026-09-09. **UNBANKED — FOLLOW-UP REQUIRED.** No milestone number assigned.
Accomplishment proposal: **Atomically commit linear and angular contact response**.

Latest [bounded payload ownership revision](payload-ownership/README.md) restores
112-byte generic events and banked-like matched counters using explicit typed
timeline slots. Full Debug now passes; full Release passes timeline/contact gates
then fails a separate SAS allocation assertion. Earlier results below are retained
as the original inline candidate's investigation history, not current validation.

Subsequent [matched banked/candidate discriminator](matched-allocation/README.md)
found a repeatable candidate-associated 8,104-byte difference. No permanent
measurement-boundary or production change was made; its cause remains unattributed.

The subsequent [disposable payload-layout control](payload-layout-control/README.md)
reproduced the exact candidate counter results with banked behavior and inert
payload structure. Generic layout is sufficient under the tested conditions;
object/runtime attribution remains unknown. The representation decision returns
to Project Control without a production or permanent-test change.

## Baseline and scope

Clean starting `main`, HEAD and origin/main:
`35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7` (README-only refresh after M14.3).
Candidate branch: `codex/contact-response-transaction`.

| Banked tag | Target |
|---|---|
| m14.1-canonical-surface-point-queries | 5b9b02ab1d2ba0e903809510908e4819984d4de2 |
| m14.2-spacecraft-translational-authority | caa6d93ccd7c4e5924381e077a62585f99e5a32a |
| m14.3-spacecraft-terrain-contact-observations | 79978cf8b0fd783d43a661df6df53a2c0202af4a |

Simulation-only implementation. No contact generator, renderer, terrain, shader,
launcher, physics package, solver, friction/rest or route changes. Current docs
also distinguish banked M14.3 from this new unbanked candidate; old investigation
records are preserved. No staging, commit, banking, tag, merge or push occurred.

## Authority and implementation proof

[Current contract](../../contact-response-transaction.md) specifies inputs and
equations. `SpacecraftContactImpulse.cs` owns immutable intent/provenance, pure
candidate evaluation and coupled history. Closed event payload/request/header
routing admits the new kind. The engine's contact partial owns canonical
re-evaluation, preflight, mutation and history. Existing stores receive one paired
operation; no pair of independent force/torque commits is composed.

An already-qualified producer supplies J_root, r_root, spacecraft/root/time,
expected revision, response version and feature/support/observation provenance.
Physical qualification is the producer's responsibility. The engine does not
query terrain or certify that an external observation happened. It binds the
candidate exactly to the canonical pending payload and fresh current state.

Both authorities evaluate at event time. Delta v = J/m; delta L_body is the
inverse event orientation applied to r cross J; delta omega divides by principal
body inertia. Evaluated pose is copied exactly. Mass, inertia, root, model,
continuous root force and body torque are preserved. No support velocity is added.
Zero impulse is rejected before scheduling, and no integration interval is created.

Preflight covers event/clock/timeline/state revisions, both complete recomputed
replacements, both history capacities and both revision overflows. All records and
successor revisions are constructed before writes. The store checks both expected
slots before its first assignment. One state revision, one coupled physical record
and the ordinary canonical-event receipt result. There are no separate force or
torque history entries. No callback, query or normal allocation/rejection path
exists after preflight under the existing exclusive single-writer contract.

## KSA provenance and adaptation

Current installed KSA: `2026.9.7.5402`, source revision
`487c3f340de24c6a81037120b6d1129c045c5400`.

- KSA.dll SHA256: `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
- BepuPhysics.dll SHA256: `77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`.
- Matching local `ConstraintSim.cs` SHA256: `2A34A8B269A74C0555DB344E8BFA3B307EBF4EE1515DD41EE29E6679B8E39410`.

Read-only source: `E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/`.
`KinematicStates.cs:25-60` establishes game state push/pull; `ConstraintSim.cs:261-277`
refreshes body/bounds state, `:373-421` preserves sleeping state from unnecessary
readback; `VehicleUpdateTask.cs:141-171` applies ready results before render events.
BRUTAL numerics support KSA's game-owned state; Bepu is subordinate mechanism.

**ADAPT:** retain game/Simulation-owned publication and add NovaCore's explicit
paired FP64 transaction. No proprietary code, float solver state, damping or
sleep/actuator policy is copied. Bepu remains unadopted.

Relevant official history was verified read-only during the directly preceding
architecture gauntlet and reused after checking current local identities:

- [4659](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885): higher-quality game/sleeping state must survive solver/bubble updates; frame consistency and warp collision checks are distinct responsibilities.
- [5173](https://discord.com/channels/1260011486735241329/1260112103134724146/1534447088291811359): stale sleeping bounds could make landed vehicles uncollidable; current source refreshes changed bounds.

These records support ownership, not proof of NovaCore atomicity. No broad Bepu
research or dependency installation was repeated.

## Numerical bars and results

Bars were declared before implementation comparison: binary64 u=2^-53 and
gamma_n=n*u/(1-n*u). Component errors use gamma32 for delta-v, gamma256 for
delta-omega, gamma64 for linear momentum and gamma512 for root angular momentum,
multiplied by the input velocity/momentum/inertia scales stated in permanent tests.
Independent decimal cross products and explicit rotation matrices form the oracle.
These are conservative fixture operation envelopes, not universal guarantees for
arbitrarily ill-conditioned input.

| Maximum error, identical Debug/Release | Value |
|---|---:|
| Delta-v, m/s | 1.6653345369377348e-16 |
| Delta-omega, rad/s | 2.220446049250313e-15 |
| Linear momentum, kg m/s | 1.3322676295501878e-15 |
| Root angular momentum, kg m^2/s | 7.105427357601002e-15 |

All pass the declared bars. Stored event position/orientation are bit-identical.
Derived evaluation may independently renormalize orientation under the existing
banked rotation contract. Replay hash `0x88CC7A6780F19270` matches Debug/Release
and 1/30/60/144/1000 observation cadences, including complete coupled history.

The test-only oracle has one approaching point, zero friction/gap, prescribed
support velocity and restitution fixtures 0, 0.5 and 1. It checks outgoing relative
normal velocity and off-center momentum. It is not automatic production response,
penetration repair, finite collision geometry, rest or contact-event bracketing.

## Focused validation and performance

Focused Debug/Release response tests pass: COM/off-center/lever-arm response,
rotated anisotropic inertia, nonzero continuous controls, negative historical
absolute epochs, exact non-epoch events, stale/forged rejection, either forged
half, wrong frame/spacecraft, mixed/stripped envelope, independent history-capacity
and revision-overflow failures, filled history preservation, event retention,
bitwise continuity, replay and independent force/torque compatibility.

Permanent warmed allocation gate: **0 bytes for 256 complete transactions**, with
separate allocation and completion assertions. No runtime-policy override or retry.

One bounded Release performance run: four warmup batches, 21 timed batches of 256
prepared events, then 21 independent allocation batches. Timing includes
EvaluateNext, canonical re-evaluation/validation, paired mutation, event removal
and both preallocated history writes. Setup/admission and analytical oracle excluded.

| Complete transaction | Result |
|---|---:|
| Median | 8,021.5 ns |
| P95 | 8,131.2 ns |
| P99 | 8,144.9 ns |
| Allocation median/max | 0 / 0 bytes per batch |

Percentiles are **batch-average latency**, not individual-event tail latency.
This is a bounded microbenchmark, not future contact/solver cost. No performance
target was tuned and no extra performance run was used to obtain a preferred number.

## Full-validation blocker and payoff stop

Both full solution builds passed with zero warnings/errors. Both full Simulation
runs then stopped in the pre-existing `TimelineTopologyTests` allocation assertion,
before the new response group. Each completed five groups, failed the sixth and
did not reach the remaining 29 groups. Do not report a 35-group PASS.

Cheap decomposition preserved the workload, threshold and warmup, separating the
counter and invariant assertions. One fixed sequential run per configuration gave:

| Field | Debug | Release |
|---|---:|---:|
| Counter delta | 8,104 bytes | 8,104 bytes |
| Timeline invariants | true | true |
| Pending | 100 | 100 |
| Cancelled | 9,900 | 9,900 |
| GC counts before/after | (2,2,2)/(2,2,2) | (2,2,2)/(2,2,2) |

The expected scheduling/cancellation operations succeeded. The allocation-counter
gate failed. No object type, allocating call stack or current-run accounting
mechanism was attributed. Candidate causality is **INSUFFICIENT EVIDENCE**.

Bounded source review found no expected capacity growth inside that loop: at most
101 pending entries, 10,000 reserved IDs and 9,900 cancelled records against
20,000 capacities. Marker events do not invoke contact provenance validation.
The new inline intent does enlarge timeline storage: measured intent/payload/event
sizes are 200/272/312 bytes. Two preallocated 20,000-event arrays contain 12,480,000
element bytes outside the counter window. This can affect GC timing, but does not
prove the 8,104 bytes are production allocations or accounting-only.

[Retained M14.2 attribution](../spacecraft-translation/allocation-attribution.md)
proves a background-GC context-retirement accounting mechanism in an older measured
incident. Unchanged GC counts do not exclude completion of an existing collection.
That evidence does not automatically attribute this new failure. No profiler,
arbitrary warmup, no-GC guard migration, tiering change or threshold relaxation was
introduced. No new Bepu or physics work was opened.

**PAYOFF RECHECK:** stop new forensic work and return this one validation blocker
to Project Control. The next decision is bounded attribution of this timeline
window and review of the new payload's storage footprint; do not rerun full suites
hoping for a pass or reopen translational/contact architecture.

Independent regressions completed after the stop, without rerunning the failed gate:

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS | PASS |
| Focused contact response | PASS | PASS |
| Full Simulation | FAIL at timeline gate; 5 pass, 1 fail, 29 not reached | Same |
| Focused M14.2 translation | PASS | PASS |
| Focused M14.3 contact generation | PASS | PASS |
| ReferenceFrames | 11 groups PASS | 11 groups PASS |
| Precision | PASS | PASS |
| M14.1 headless surface query/stale snapshot | 2 pass, 0 fail, 0 skip | Same |
| M14.3 production-terrain headless contact | 1 pass, 0 fail, 0 skip | Same |
| git diff --check | PASS (line-ending warnings only) | Shared working tree |

Independent tests do not replace the missing full-suite pass. Normal production
routes are unchanged; manual visual acceptance is **NOT REQUIRED** for this seam.

## Verifiers

- **A, atomicity:** no source blocker. Recompute canonical intent, preflight both
  slots/capacities/revisions, then pair-write. Normal consumption failure cannot
  arise after preflight in the exclusive single-writer phase. Fatal corruption and
  concurrent misuse are not rollback guarantees.
- **B, physics:** no mapping blocker. Inertia-frame conversion, preserved controls
  and stored event pose pass. Derived quaternion normalization is separately bounded.
- **C, scope/KSA:** no solver, friction/rest or proprietary implementation entered.
  The seam remains useful to future adapters, which still need separate temporal
  integration/readback qualification.
- **Validation red team:** the old accounting finding cannot excuse the present
  failed gate. Complete-candidate acceptance remains blocked pending attribution.

## Reproduction and retention

```
dotnet build NovaCore.sln -c Debug --no-restore
dotnet build NovaCore.sln -c Release --no-restore
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --contact-response-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --contact-response-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --contact-response-performance
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Precision.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --category=headless "--test=Physical surface-point"
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --category=headless "--test=Contact generation production terrain"
```

Use matching Debug commands for remaining configuration checks. These are recipes,
not authorization to repeat the failed gate without attribution instrumentation.

Retention budget: 24 KiB for this report plus candidate source/test fingerprints.
`candidate-fingerprints.json` records the 14 changed/new production and test files.
No raw diagnostic files or bulk captures were created. Standard build products
remain ordinary rebuildable outputs, not evidence archives. Disposable diagnostic
output created/disposed/remaining: **0 / 0 / 0 bytes**.

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

**CONTACT RESPONSE TRANSACTION CANDIDATE — FOLLOW-UP REQUIRED**
