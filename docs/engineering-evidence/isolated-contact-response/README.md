# Isolated analytical contact response — candidate evidence

2026-09-09. **PASS — MINIMAL CONTACT RESPONSE CANDIDATE — READY FOR PROJECT
CONTROL ACCEPTANCE.** UNBANKED. Proposed accomplishment: **Qualify isolated
analytical terrain-contact response**. No milestone number assigned.

## Baseline and scope

Clean main; HEAD/main/origin/main and peeled `m14.4-atomic-contact-response` all
`28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c`. Predecessors verified:

| Tag | Commit |
|---|---|
| m14.1-canonical-surface-point-queries | 5b9b02ab1d2ba0e903809510908e4819984d4de2 |
| m14.2-spacecraft-translational-authority | caa6d93ccd7c4e5924381e077a62585f99e5a32a |
| m14.3-spacecraft-terrain-contact-observations | 79978cf8b0fd783d43a661df6df53a2c0202af4a |

Created `codex/minimal-contact-response`; nothing staged, committed or banked.
M14 remains OPEN. The [current policy contract](../../isolated-contact-response.md)
defines admission, lifetime, equations, numerical bounds and limitations.

The two production paths are the new `IsolatedContactResponsePolicy.cs` and an
additive generator forwarding overload that exposes the SAME evaluated motion.
The existing observation arithmetic, M14.4 transaction, typed arena, generic event,
existing analytical oracle, Graphics/native/rendering and visible routes are unchanged.
Tests add one Simulation group and two focused execution switches. Current docs
also correct demonstrably stale M14.4-unbanked/SAS-blocked descriptions against the
verified tag; dated investigation records are retained unchanged.

## WHAT / HOW / BAR / PAYOFF

Missing responsibility: admission plus production of physically justified intent,
not another mutation path. A private-construction receipt binds successful M14.3
generation to immutable query/geometry/system, exact time/root/revision and complete
physical state. Requalification needs no terrain query or repeat motion evaluation.

Complete authored geometry must contain one feature. This specialization is only
for one natural-terrain support, not other collision systems. **Only represented
canonical radial gap exactly zero is admitted.** Positive, negative and uncertain
cases do not get approximate impulses. This conservative limitation was selected
before implementation after verifying M14.3 fixture error bars are not universal
terrain-slope bounds. It may refuse numerically near-contact Earth states; no
automatic touchdown, contact skin or continuous nonpenetration is claimed.

The outward unit normal comes from successful canonical query provenance. Approach
is qualified against subtraction-plus-dot rounding of canonical velocities, using
outward-rounded IEEE binary64 bounds. Unknown physical-model/normal-derivative
errors are not certified. Off-center effective mass uses body principal inertia
and authored COM lever arm. Frictionless restitution-zero impulse becomes an
ordinary M14.4 intent. No producer mutation, zero event, recovery, cache or solver.

## KSA source and official history

Installed KSA `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400` remains
the source reference. Live installed hashes rechecked during this implementation:

```text
KSA.dll         A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F
BepuPhysics.dll 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7
```

Relevant installed-production decompilation under
`build/ksa-residency-reference/assembly-source/KSA` was inspected narrowly:
`NarrowPhaseCallbacks.cs:153–204` owns game contact filtering/material policy;
`ConstraintSim.cs:834–866` distinguishes stateful collision detection and interval
simulation, with poisoned-state failure handling. It is not a pure equivalent of
this receipt API. BRUTAL supplies mechanisms/numerics; KSA owns vehicle policy.

Official history verified read-only through the authenticated browser in the
immediately preceding decision gauntlet is reused here rather than repeating research:

- [4659](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885): impact-aware constraints must influence final advancement interval; this candidate does not discover that interval.
- [4663](https://discord.com/channels/1260011486735241329/1260112103134724146/1517049362700828722): mixed frames require explicit ownership; receipt uses one coherent root/time.
- [5174](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902): actuator/landed validity is a separate policy; no grounded/sleep state is inferred here.
- [5177](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058): repeated physics evaluation must not re-execute control commands; this policy is pure and M14.4 owns publication.

**ADOPT/ADAPT ownership:** simulation/game owns admission and response policy;
solver/render mechanisms are subordinate. **INTENTIONALLY DIFFER mechanism:**
the qualified isolated point and exact impulse boundary admits a closed-form
solution. Its independent oracle remains valuable after general solver qualification.
No claim of matching KSA manifold, friction, recovery or continuous collision behavior.

## Correctness and validation

Final Debug and Release full solution builds passed with zero warnings/errors.
Both full Simulation suites passed **36/36 groups**, including translation,
contact generation, M14.4, SAS and the new response policy. Two full-suite runs
per configuration passed during development/finalization; the final runs include
the additional nonradial and near-unit normal fixtures. No allocation failure
triggered another forensic campaign. ReferenceFrames **11/11** and Precision
passed in both configurations. M14.1 query/stale-snapshot **2/2** and M14.3 real
terrain integration **1/1** passed in both configurations, with no skips among
selected tests. These are headless CPU-terrain regressions, not a visible smoke.

New fixtures cover 144 combinations: centered/two off-center features; two craft
orientations; moving support; spinning craft; zero/150-billion-metre origins;
radial, tilted and near-unit outward normals. Body rotation comes from the same
canonical orientation authority as M14.3. Oracle compliance uses an independent
explicit rotation matrix and projected point velocity, not the policy function.

Predeclared comparison bars use gamma2048 times stated transport/momentum scales.
Normal-input squared-length error is accounted separately in impulse and
post-normal comparisons. No unexplained fixed physical epsilon is used.

| Final maximum, same Debug/Release | Value |
|---|---:|
| Absolute post-impact relative normal velocity | 3.2596422030904357e-12 m/s |
| Linear momentum error | 1.3356214468852346e-11 kg m/s |
| Angular momentum error | 1.8318679906315083e-15 kg m²/s |
| Event pose | Exact equality |
| Repeated/fresh replay intent | Exact equality |
| Pure policy evaluation | No state/revision/timeline mutation |
| Warm policy allocation | 0 bytes / 10,000 evaluations |
| Terrain queries during those evaluations | 0 additional; one acquisition observation |

Refusal tests cover separation, cancellation-dominated near-zero speed, zero
speed, positive/negative gaps including small nonzero values, unqualified/invalid
normal, invalid provenance ID, geometry/feature/system/root/body/time mismatch,
changed terrain authority/query instance and stale state. Invalid mass/inertia
cannot enter canonical physical state. Caller scratch mutation cannot forge the
receipt. A valid intent commits atomically; a duplicate becomes stale and leaves
both states/history unchanged, retains its rejected payload, then explicit cancel
retires that payload. Existing M14.4 forgery/capacity/replacement tests also pass.

The first focused run exposed a test setup assertion expecting success for a
deliberately NaN query normal; the generator correctly refused it. The fixture
expectation was corrected before the successful gates. No production defect was
hidden by this test correction.

## Bounded warmed Release cost

Plan declared in permanent test source: four warm batches, 21 timing samples of
128 independent prepared states per mode. Setup excluded. A separate 21-batch
allocation measurement uses the already banked test-only no-GC accounting
boundary; ordinary production GC/tiering remain unchanged. Timings use default
runtime behavior without that accounting override. Percentiles are **batch-average
latencies**, not individual-event tail guarantees or guaranteed final-tier code.

| Path | Median ns | P95 ns | P99 ns | Maximum allocation / 128 |
|---|---:|---:|---:|---:|
| Policy admission/evaluation | 1922.7 | 2028.9 | 2100.0 | 0 bytes |
| M14.4 scheduling + canonical transaction | 5178.9 | 5471.1 | 5772.7 | 0 bytes |
| Analytical observation + policy + transaction | 8559.4 | 8651.6 | 8657.8 | 0 bytes |

These are the final rebuilt candidate measurements. Before the additional normal
fixtures, the same unchanged performance paths reported median/P95/P99 respectively
1964.8/2093.8/2105.5, 5314.8/6088.3/6120.3 and 8743.8/9335.9/9685.9 ns, also with
zero allocation. The final repeat verifies the rebuilt test executable; it is not
a retry to clear a failed performance threshold.

The complete bounded path uses a cheap analytical CPU witness and already-current
body kinematics; it is not real-Earth total query cost. Actual M14.1 query cost
remains separate and can dominate. No GPU/frame-budget or many-craft scaling claim.
Policy work is constant in the admitted one-feature input and does not allocate,
requery terrain or introduce persistent state. No optimization was undertaken.

## VERIFY and judgment

**Physics/admission red team:** exact gap is a limited represented contract, not
continuous collision. Opaque successful provenance prevents unit length alone
from qualifying a physical normal. Multi-feature definitions refuse. No blocker.

**Identity/transaction red team:** independently current composition inputs remain
required; merely passing an old query as current cannot prove real freshness.
Complete state comparison and canonical M14.4 checks protect their respective
boundaries. Duplicate stale rejection and payload lifecycle tested. No blocker.

**Numerical evidence improvement:** independent reviewer requested tilted and
near-unit normals and explicit normal-input error in post-impact bars. Added and
passed without changing production equations or threshold.

**Scope/payoff red team:** this adds real production admission and intent ownership,
not only copied oracle arithmetic. It does not solve recovery, multi-contact or
advancement. No new causal question required scope expansion. No blocker.

Lead judgment **PASS**. **Manual acceptance NOT REQUIRED**: simulation-only,
no visible route uses the policy. Return UNBANKED for Project Control acceptance.

## Reproduction and evidence lifecycle

From `E:\NovaCore`, with normal repository dependencies available:

```powershell
dotnet build NovaCore.sln -c Debug --no-restore
dotnet build NovaCore.sln -c Release --no-restore
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --isolated-contact-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --isolated-contact-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --isolated-contact-performance
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Debug --no-build
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Precision.Tests -c Debug --no-build
dotnet run --project tests/NovaCore.Precision.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug --no-build -- --category=headless "--test=Physical surface-point"
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --category=headless "--test=Physical surface-point"
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug --no-build -- --category=headless "--test=Contact generation production terrain"
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --category=headless "--test=Contact generation production terrain"
git diff --check
```

Retain this report and source fingerprints, budget 16 KiB combined; permanent
fixtures/reproduction live in tests. No profiler traces, raw captures or disposable
comparison trees were created. Test-owned temporary malformed-input fixtures
clean themselves up. Normal ignored build outputs are retained as development
outputs, not copied into evidence. **Disposable diagnostic remaining: 0 bytes.**
No production runtime, assets or previous evidence was deleted. No staging or banking.
