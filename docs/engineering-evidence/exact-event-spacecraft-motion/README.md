# Exact-event spacecraft motion candidate evidence

10 September 2026. **UNBANKED — FOLLOW-UP REQUIRED.**
Lead judgment: **ESCALATE TO PROJECT CONTROL**.
Proposed accomplishment: **Evaluate coherent spacecraft motion at exact physical-event epochs**.
No milestone number, stage, commit, merge, tag, push or banking.

## Baseline and scope

Initial main was clean. HEAD/main/origin/main remain
`00f226096b61a117184464b5619e464d39789574`.
M14.6 tag `m14.6-physical-event-epochs` remains at
`d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
M14.1 through M14.6 tags are unchanged. Local branch:
`codex/exact-event-spacecraft-motion`.

The [contract](../../spacecraft-physical-event-motion.md) defines the one read-only
responsibility. Five production files extend the existing evaluators and add a
bounded shared local-duration helper. No timeline, store, transaction, contact,
public time, epoch arithmetic/order or renderer implementation changed. The
canonical rigid-rotation body remains unchanged; translation and constant-rate
attitude preserve their numerical expressions in shared kernels. Existing callers
remain canonical. Program.cs changes only selectors and one group appended after
all 37 existing groups; no existing test body, warmup or allocation boundary changed.

## Numerical and authority evidence

- 1,000 seeded exact-duration capacity/sign controls against BigInteger, including
  full floor range, negative intervals, UInt64 denominator limit and late short
  intervals. BigInteger is test-only.
- 64 canonical success cases compare every result field bitwise; three canonical
  refusal cases compare status/default output. Canonical success includes zero,
  one tick, fractional-step boundaries embedded canonically, negative epochs,
  `long.MinValue` and late `long.MaxValue` origins.
- 120 translation cases (360 axes): exact rational inputs and decoded binary64
  initial values, gamma32 scale-derived error bounds. Largest observed position
  difference: 0.0001220703125 m at a 1e12 m coordinate (one ULP); maximum velocity
  difference: 0 m/s. A binary-exact 15625/32768-tick witness produces 2^-18 m.
- Twelve analytical principal-axis torque cases: maximum quaternion distance
  1.1103267813128674e-16. Mixed torque/rates compared against an independently
  written explicit-midpoint solver at 1024/2048/4096 subdivisions: final quaternion
  difference 2.784314975363703e-15; angular-velocity norm difference
  2.7722596276462393e-15 rad/s; final reference refinement 1.3557157818527375e-14.
  The reference has its own derivative equations and does not call production RK4.
- Constant-rate analytical, stationary, signed near-substep-boundary and
  denominator-limit cases pass; finite unit quaternion and exact step-count
  assertions pass. Overflow, capacity, invalid state/mass and complete/default
  failures pass.
- 128 seeded epochs, reverse evaluation and 512 reordered requests preserve value
  bits. Differing segment epochs are established through an existing canonical
  torque transaction. Before/after physical segments, properties, revision,
  clock/debt, pending event, timeline revision and history counts remain equal.
  A later canonical force commit changes fresh evaluation while retained result
  values/revision remain unchanged. Result contains no managed references.
- Debug warmed measurement: 1024 warmups, 8192 measured coherent rational RK4
  evaluations, zero bytes, checked entry/exit PASS. Full Debug shared deliberate
  byte[128] control detects 152 bytes. No allocation tolerance or measurement
  architecture change was introduced.

## Validation and explicit stop

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS; 0 warnings/errors | PASS; 0 warnings/errors |
| Focused exact-event correctness | PASS | NOT RUN after stop |
| Full Simulation | PASS 38/38 | FAIL: 31 passed, 1 failed, 6 unreached |
| M14.2 translation, rigid rotation | PASS within full suite | PASS before stop |
| M14.3 generation, M14.4 response, M14.5 isolated response | PASS within full suite | PASS before stop |
| M14.6 epochs | PASS within full suite | PASS before stop |
| Standalone existing focused reruns | NOT RUN after stop | NOT RUN after stop |
| ReferenceFrames / Precision | NOT RUN after stop | NOT RUN after stop |
| Release cost measurement | n/a | NOT RUN after stop |

The full-suite processes were each run once. The Release process failed at
`SasSignFrameContinuityProofTests`, Program.cs:295:

```text
ORDINARY_ALLOCATION gate=sas-proof bytes=12336 entry=PASS exit=PASS
Unhandled exception. System.InvalidOperationException:
sas-proof: expected zero managed allocation, actual=12336
at OrdinaryAllocationMeasurement.RequireZero(...):line 44
at Program...SasSignFrameContinuityProofTests...:line 295
```

The failing assertion independently requires exact zero. Its preceding workload
is the unchanged 100,000-iteration SAS evaluation/hash loop following its original
single-call warmup. Entry and exit passed. The final report/checksum after that
assertion was not reached. Actual managed-object allocation, runtime accounting,
intermittency and candidate causality are **UNCLASSIFIED**. No observer, repeat,
baseline A/B or allocation forensic work was performed. Familiar previous byte
counts do not explain this 12,336-byte observation.

Six groups were unreached: Analytical orbit sampling, Celestial frame extraction,
Celestial trajectory replacement, Celestial impulse events, Allocation, and the
new Exact-event spacecraft motion group. Thus the new group's runtime workload
had not executed in the failing process; this does not establish that the overall
candidate is causally unrelated. Existing canonical numerical kernels did change
placement through extraction, and no causal claim is made from test order alone.

The required stop was honored. No remaining correctness/performance execution
occurred after this failure. Median/P95/P99 are **UNAVAILABLE**, not inferred.
The opt-in performance method is implemented but unexecuted: 41 samples of 256
evaluations after 4096 warmups, canonical/rational and 32 distinct craft/event
batch cases, analytical and RK4 models. Timing is outside no-GC measurement;
allocation is checked separately. No payoff conclusion is accepted yet.

## Review findings

**VERIFY A — physical/numerical:** checked signed exact arithmetic before local
conversion; no production BigInteger or canonical snap; fractional RK4 partition
retains the existing solver. Independent oracles pass in Debug. An initial test
oracle represented zero with an unnecessary 2^1074 denominator, causing NaN in
its floating output; exact zero is now represented as 0/1. An initial coherence
fixture violated the existing common initial-epoch contract; it now obtains
differing segment epochs through a canonical torque transaction. These were test
construction corrections. Release exact-event validation remains missing.

**VERIFY B — authority:** strongest objection is treating a captured revision/live
view as a concurrency snapshot or mutation receipt. The API retains the existing
fresh-view/single-writer precondition; returned values retain no live reference.
No owner token, mutation protocol, contact proof or scheduling path was invented.
Source/lifetime review and Debug immutability tests support the scoped boundary.

**VERIFY C — compatibility:** an initially proposed same-name coherent overload
made existing target-typed `new(...)` callers ambiguous. The distinct
`TryEvaluateAtPhysicalEpoch` name preserves those calls. All existing test bodies
remain unchanged; Debug full suite passes, Release canonical translation/rotation
and contact groups pass before the SAS stop. Complete Release compatibility is
not cleared. Reviews were performed by the implementation lead, not independent
external reviewers.

## KSA implementation check

The accepted post-M14.6 assessment is retained at
`E:/NovaCore-Research/post-m14.6-responsibility-20260910/assessment.md`.
Implementation rechecked current local `VehicleUpdateState` and
`PhysicsBubble.ApplyResultsToVehicles` / `ApplySynchronizedResultsToVehicles` in
`build/ksa-residency-reference/assembly-source/KSA/`. They distinguish vehicle/game
state, transient update/solver work and explicit application of results. This
candidate stops before application. **ADAPT responsibility:** simulation-owned
read-only derivation; no Bepu/solver persistent authority or BRUTAL publication
boundary is added. No KSA implementation/data was copied and no broader history
research was reopened. Prior official-history provenance remains in the accepted
assessment. Installed KSA.dll SHA-256 reverified:
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.

## Reproduction and continuation boundary

Commands run from `E:/NovaCore`, SDK 10.0.303. Runtime inventory includes
Microsoft.NETCore.App 10.0.12; no new profiler/tiering/runtime settings were used.
Source and built assembly hashes are in [identity.json](identity.json).
[validation.json](validation.json) retains compact gate results and decisive output.

```powershell
dotnet build NovaCore.sln -c Debug --no-restore -v:q
dotnet build NovaCore.sln -c Release --no-restore -v:q
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --exact-event-motion-only
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
```

These are reproduction instructions, not authorization to retry the failure.
Project Control must choose the next bounded step. Unexecuted opt-in selectors:
`--exact-event-motion-only`, `--exact-event-motion-performance`,
`--rigid-rotation-only`, plus existing `--translation-only`, `--contact-only`,
`--contact-response-only`, `--isolated-contact-only`, `--physical-event-epoch-only`.
ReferenceFrames and Precision remain separate test executables. Manual visual
acceptance is NOT REQUIRED: no visible route consumes this internal evaluator.

Only this concise report, structured results and hashes are retained. No new
comparison tree, observer, profiler capture, dump or raw stdout file was created.
Normal reusable solution outputs remain in their existing ignored build/bin/obj
locations; no duplicate runtime tree is retained. Diagnostic scratch: zero.

**Exact PhysicalEventEpoch != exact FP64 physical state. This result is not contact proof.**

**EXACT-EVENT SPACECRAFT MOTION CANDIDATE — FOLLOW-UP REQUIRED**
