# Authored point-feature contact generation — candidate evidence

2026-09-09. **PASS — CONTACT GENERATION CANDIDATE — READY FOR PROJECT CONTROL
ACCEPTANCE.** Unbanked. Proposed accomplishment: **Generate deterministic
spacecraft-to-terrain contact observations**. No milestone number assigned.

## Baseline and retained responsibility

Clean `main` before implementation; HEAD/main/origin/main and annotated M14.2 tag
`m14.2-spacecraft-translational-authority` resolve to
`caa6d93ccd7c4e5924381e077a62585f99e5a32a`. M14.1 tag
`m14.1-canonical-surface-point-queries` remains at
`5b9b02ab1d2ba0e903809510908e4819984d4de2`.
Development branch: `codex/contact-generation`; nothing staged or banked.

The [current contract](../../contact-generation.md) defines immutable authored
COM-relative point features, exact-time spacecraft/body transport, canonical
terrain readiness, radial gap and witness velocities. Three new Simulation files
implement the admitted definition, fresh body adapter and pure generator. Existing
M14.1/M14.2 production files, Graphics/native/shaders/launcher are unchanged.
Only test registration, permanent fixtures and affected documentation accompany
the new production files. [Fingerprints](source-fingerprints.json) identify the
complete production/test candidate. [Results](results.json) retain compact numeric
measurements and validation outcomes, not raw profiler traces.

## Cheap proof and KSA relationship

The approved architecture remains valid: Simulation owns the point definition and
observations; Core supplies the query interface; application composition supplies
current immutable celestial/terrain authority and reusable buffers. M14.1 remains
the sole natural-terrain source. Coherent M14.2 motion supplies both physical
states. No response is stored or applied. An authored support hardpoint remains a
meaningful location when a future hull or leg collider is added, without claiming
that points cover finite geometry.

**ADAPT.** Current installed KSA source separates vehicle authority, collision
detection and solver stepping. NovaCore follows that responsibility separation
while retaining pure immutable observations and explicit canonical readiness.
KSA detection updates caches; it is not an equivalent pure API to copy verbatim.

Current source provenance: installed KSA `2026.9.7.5402`, revision
`487c3f340de24c6a81037120b6d1129c045c5400`, under `E:\Kitten Space Agency`.
Inspected production assembly decompilation is cached under
`build/ksa-residency-reference/assembly-source/KSA`; this is an implementation
reference, not a new runtime dependency or an original authored-source checkout.

| Source | Current responsibility observed |
|---|---|
| `ColliderModule` | Authored finite box/capsule/cylinder/sphere/hull definitions; not anonymous COM probes. |
| `Vehicle.cs:2007–2023`, `BepuHandles.cs:68–113` | COM-relative children map to structural parts; rebuild maintains that mapping. |
| `ConstraintSim.cs:261–277` | Sleeping bounds require valid persistent ownership. |
| `ConstraintSim.cs:834–866` | DetectCollisions precedes/separates from Simulate; detection remains stateful. |
| `PhysicsBubble`, `NarrowPhaseCallbacks` | Frame-consistent grouped physics/contact and subordinate solver mechanisms. |

SHA-256 identities confirmed in the narrow source review:

```text
KSA.dll             a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f
BepuPhysics.dll     77185e513bd530deb322e41dd4acda9ad8e32e626cfa19492fb7481f25ed1fa7
ConstraintSim.cs    2a34a8b269a74c0555db344e8bfa3b307ebf4ee1515dd41ee29e6679b8e39410
NarrowPhaseCallbacks.cs 2e702aa76ea23529751c757c9c203399265129591f527293e5eac0c3a0a93934
PhysicsBubble.cs    e2fe83c15d846e840ec382e6823e8e3550a04e80e510fe4fcc29cab9dbdb11d9
```

Official history already verified in the immediately preceding architecture
gauntlet was reused narrowly; no broad Discord research or source copying:

| Official revision | Engineering lesson retained |
|---|---|
| [4646](https://discord.com/channels/1260011486735241329/1260112103134724146/1516883816554168393) | Rough terrain classification is distinct from actual collision detection. |
| [4659](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885) | Representation/bubble changes must not spuriously wake or overwrite precise authoritative resting state. |
| [4663](https://discord.com/channels/1260011486735241329/1260112103134724146/1517049362700828722) | Mixed vehicle frames are a causal correctness error; grouping must be frame-consistent. |
| [5173](https://discord.com/channels/1260011486735241329/1260112103134724146/1534447088291811359) | Sleeping cannot leave stale uncollidable bounds. |
| [5382](https://discord.com/channels/1260011486735241329/1260112103134724146/1544200205769900113) | Resolve reusable pad pose at the correct lifecycle boundary rather than each vehicle/microstep. |

NovaCore applies the last lesson to once-per-body/time evaluation and once-per-set
coherent craft resolution. It implements neither pad collision nor sleeping here.

## Correctness and numerical bars

Bars were declared before evaluation, using FP64 unit roundoff u=2^-53. At root
origin scale O, analytical position bar is `8 ulp(max(1,O)) + 128 u × 32 m`.
The bounded plane-direction amplification permits four times that bar for witness
and radial gap. Velocity bar is `128 u × 40000 m/s + |omega_body| × positionBar`;
normal-vector bar is `128 u + positionBar / 8 m`. Fixtures use 0 and 150 billion
metre root origins, sphere/flat plane/sloped plane, independent craft tilts, body
rotation/translation and four off-center support hardpoints. These are fixture
bars, not universal surface-query error bounds.

| Maximum measured error, Debug and Release | Result |
|---|---:|
| Feature root position | 3.0517578125e-5 m |
| Terrain witness position | 3.0768239642021255e-5 m |
| Relative velocity | 4.741560129417225e-10 m/s |
| Transported physical-normal vector | 3.3678454736423286e-7 |
| Radial gap | 1.9352994833354842e-5 m |
| Dedicated exact-zero-gap fixture | 3.552713678800501e-15 m |
| Replay difference | 0 bits |
| Warmed generation allocation | 0 bytes / 10,000 four-feature batches |

The normal-vector figure includes root subtraction/direction error at astronomical
origins; it is not the physical normal oracle's intrinsic angular error. Existing
body omega is derived from orientation samples at t±0.5 s; tests prove transport
against that authority, not analytically exact planetary rotation.

Permanent checks cover all eighteen requested categories, including above/inside/
zero radial gap, co-moving zero relative velocity, independent craft angular
motion, ancestor center velocity, multiple features, deterministic bit replay and
no state/revision mutation. Admission tests cover duplicate IDs, caller mutation,
signed zero, changed geometry under reused version, body/root/time/system identity,
unavailable query, mismatched returned authority, capacity before queries and every
canonical non-ready status on a late feature. No partial ready prefix survives.

Freshness review found and corrected a candidate admission gap: a retained view
references live spacecraft storage but contains an old revision. The generator
now compares it with the independently supplied current revision. A permanent
test retains a view, commits a real canonical torque transaction, rejects that old
view before any query, then accepts the fresh view. The generator itself never
commits or changes torque. Caller composition remains responsible for supplying
the real current revision/system/root convention in the single-writer phase.

Production integration uses actual global/regional physical datasets at Florida,
28.5°N/80.5°W and 39.1°N/106.8°W. All 1/4/8-feature sets are ready. Repeated direct
canonical queries have zero measured witness/normal/gap disagreement. Those checks
establish integration consistency, not an independent terrain-normal oracle.
M14.1's permanent oracle remains unchanged and passes with output fingerprint
`3e08f86757588127099cbb100d3829b73d5e5f29b1de8d0f22ec83de28534912` in both builds.

## Query-inclusive performance and payoff

AMD Ryzen 9 9900X, .NET SDK 10.0.303. Three final fresh Release processes were
explicitly awaited in sequence. Each site/count uses 64 warmup batches and 201
individual samples per path, alternating complete/query-only order. Complete
includes fresh Sol/body evaluation, coherent spacecraft evaluation, every canonical
query and output. Setup/acquisition are excluded; no GPU work is timed. Query-only
uses the generator's same pre-query direction calculation. Percentiles are nearest
rank, not averages of loop batches. The paired mean difference is an approximate
overhead estimate; negative sub-microsecond differences illustrate measurement noise.

Ranges below span those three runs. All values are **CPU milliseconds**.

| Site / features | Complete median | Complete P95 | Complete P99 | Query-only median |
|---|---:|---:|---:|---:|
| Florida / 1 | .314–.330 | .320–.403 | .333–.438 | .292–.309 |
| Florida / 4 | .153–.210 | 1.194–1.343 | 1.205–1.452 | .131–.189 |
| Florida / 8 | .252–.257 | .256–.260 | .269–.276 | .250–.251 |
| Regional / 1 | .092–.095 | .094–.097 | .096–.104 | .090–.091 |
| Regional / 4 | .311–.316 | .315–.386 | .323–.482 | .310–.313 |
| Regional / 8 | .613–.620 | .620–.658 | .626–.773 | .611–.618 |
| Inland / 1 | .060–.060 | .060–.061 | .063–.063 | .058–.059 |
| Inland / 4 | .235–.237 | .241–.249 | .253–.335 | .234–.236 |
| Inland / 8 | .473–.494 | .480–.581 | .510–.652 | .471–.494 |

Every complete allocation window measured **zero bytes**. Allocation is measured
separately in the banked test-only no-GC accounting region; admission/exit must
succeed and the threshold remains exactly zero. Tiering and ordinary production
GC policy are unchanged. Timings do not run in that region.

Early Florida process-state tails remain visible, including a one-point median
larger than the later four-point median. The fixed warmup does not establish a
uniform compilation state; no trace independently attributes those tails to JIT.
They are not discarded, and no universal worst-case claim is made. Four-feature
regional mean added body/frame/generator work is approximately 1.2–5.0 µs; terrain
queries dominate. At the qualification workload of one craft/120 observations per
second, regional median is about 3.7–3.8% of one 8.33 ms CPU interval, with measured
P99 about 3.9–5.8%. Florida's early P99 can consume about 17.4%. This remains useful
headroom for this bounded observation responsibility, not a guarantee for response,
all geography, many craft, or an adopted simulation scheduling policy.

An initial repeat batch inadvertently overlapped the tail of a yielded Debug run;
its timings are excluded from qualification. Exactly three new sequential Release
runs replaced it. Correctness/allocation checks still passed. The compact results
record that exclusion and the initial exploratory run, rather than silently
selecting favorable measurements. No profiler campaign or performance correction
was opened.

## Validation and adversarial review

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS, zero warnings/errors | PASS, zero warnings/errors |
| Simulation | 34 groups PASS | 34 groups PASS |
| ReferenceFrames | 11 groups PASS | 11 groups PASS |
| Precision | PASS | PASS |
| Selected M14.1 headless regressions | 2 PASS, 0 fail/skip | 2 PASS, 0 fail/skip |
| New production-terrain integration | PASS | 3 final runs PASS |
| `git diff --check` | PASS | Same tree |

VERIFY A: attacked radial-versus-finite separation, fake normals, stale authority,
identity and omitted features. No production blocker. It found the missing flat
plane fixture, which was added alongside sphere/slope tests before final builds.
The normal comparison bar was made explicit as repeated-query transport roundoff.

VERIFY B: attacked double-counted ancestor motion, angular lever arms, time/root
and live-backed revision. The revision admission correction and real transaction
test close the only identified production-candidate gap. Final read-only review PASS.

VERIFY C: authored lower hardpoints persist as physical locations, but do not
substitute for finite geometry. Detection-only observations are useful inputs to
future response. Cost is dominated by the already-qualified canonical query;
scope did not expand into solver, rendering, contact response or scheduling work.
Early-process timing limits are disclosed. No remaining bounded blocker.

Manual visual acceptance: **NOT REQUIRED**. Production routes do not invoke a new
response or change motion/rendering. Response, coupled force/torque transactions,
solver/persistence, grounding, CCD/warp policy and pad collision return to Project
Control. No milestone assigned and no banking authorized here.

## Reproduction and evidence lifecycle

From repository root, build Debug/Release with `dotnet build NovaCore.sln -c Debug`
and `-c Release`. Run each matching build, sequentially:

```powershell
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Precision.Tests -c Release --no-build
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- '--test=Physical surface-point'
dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- '--case=Contact generation production terrain'
```

Repeat with Debug for correctness. `--contact-only` on Simulation is the narrow
analytical/admission/allocation path. For comparable Release cost, run the final
Graphics command in exactly three fresh processes, waiting for each to exit and
avoiding concurrent builds/tests. It writes only compact stdout; no trace capture.
Production assets must already resolve through the existing asset policy.

Retained evidence is this report, compact results and source fingerprints. The
fixture and tests are permanent reproduction tooling; no source copies, GPU
readbacks, video or bulk build logs were created. No diagnostic scratch remains;
ordinary ignored build products remain usable. **DISPOSABLE REMAINING: 0 bytes.**
No evidence deletion was necessary. Stop for Project Control acceptance.
