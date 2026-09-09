# Surface-point query — bounded performance qualification

2026-09-09. Unbanked on `codex/surface-contact-query`.

## Baseline and original blocker

HEAD, local main and origin/main are
`aaf3e7a032a806e7e547c2830d1f95d97aaf620b`. M13 is closed; M13.6 remains
banked at `90fef759243dd67918cd556e19027159e5a5eada`. The initial candidate
working tree was preserved; the index was and remains empty. No milestone assigned.

The original **4.0634 ms** was the mean of 64 batches of **16 identical regional
queries**, after 1,024 warmed single-query samples with ordinary Release .NET
tiering. It was not a realistic footprint percentile or GPU frame time. It was
enough to justify measuring the cost before adding future contact consumers.

The exact production revision in this follow-up is confined to staged normal
qualification in `PlanetaryPhysicalSurfacePointQuery.cs`. Its SHA-256 changed from
`1e1726c2eb4f377951455696e4b3858bde76699bee070091eef0e76a8d5789a0` to
`f8e4dd8f97cbdc4d7134c3fe90ba613fca31977cfe3de02e002d4bc93aef5e71`.
Core contract, regional loader and manifest-validation source hashes are unchanged
from the initial candidate. A permanent full-output fingerprint assertion was
added to the existing physical-query test; affected contract/evidence text updated.
There are no native, shader, asset, launcher, simulation or contact-response edits.

## WHAT → HOW → BAR → PAYOFF

**What:** rejected normal refinements eagerly evaluated expensive stencils that
could no longer affect acceptance. **How:** count full-H evaluations, attribute
nonoverlapping work, test a source-equivalent external prototype, then independently
compare original/final production DLLs. **Bar:** exact output identity, unchanged
qualification/readiness/lifetime, zero warmed query allocation, useful recovery
on distinct physical contact points. **Payoff:** a real 16-point landing footprint
recovers 1.532–1.581 ms, leaving 1.377–1.395 ms median CPU cost.

## Cost decomposition

Instrumented regional query buckets (microseconds) are mutually exclusive. They
are attribution evidence, not ordinary runtime timings: timers and diagnostic
decomposition perturb execution. Do not add inclusive normal cost atop these rows.

| Responsibility | Original us | Revised us | Frequency / necessity |
|---|---:|---:|---|
| Global physical sampling | 17.21 | 7.31 | Once per full H; required for samples that still participate |
| Regional address/record/interpolation/coverage | 151.53 | 64.43 | Once per H; direction-dependent lookup remains necessary |
| Natural physical composition | 110.32 | 46.04 | Once per H; same canonical arithmetic |
| Support/recomposition | 4.85 | 2.14 | Once per H; unchanged facility grading |
| Input/frame/normal/result arithmetic, loops and timer overhead | 22.65 | 10.45 | Remainder; not further separated reliably |
| Total instrumented query | 306.56 | 130.36 | 273 → 113 full H evaluations |
| Admission/body/manifest/version/readiness | Outside Query | Unchanged | Once when acquiring authority after data publication |
| Query file access / locks / GPU synchronization | None | None | Absent from the warmed call graph |

The center height costs one H; initial cardinal stencil eight; each original
refinement adds 24. Representative counts: regional **273→113**, inland **129→65**,
Florida **33→33**, unresolved south pole **489→169**. Each stencil still samples
the full global+regional+natural+support H. The theoretical bound remains 489.

Normal qualification dominates through repeated neighboring H evaluations.
Regional addressing versus record lookup versus coverage was not individually
timed; four immutable neighbor-membership checks are not proven dominant.
Inland regional lookup has similar per-H cost despite missing coverage. No second
neighbor-cache optimization is justified by these measurements. No existing
analytic full-H gradient with identical qualification was established.

## Lifecycle, query context and the one correction

| Fact / operation | Correct lifetime / responsibility |
|---|---|
| Decoded global/regional bytes, actual digest/header/length | Per dataset; published together once, never mutated/unloaded |
| Trusted manifest, terrain version, composition/support/query-policy identity | Per acquisition / terrain version; immutable provider authority |
| Readiness | Before provider publication; complete data required, no partial fallback |
| Body ID, input validity, direction/frame, center H | Per query |
| Sector / deepest record / geographic coordinates | Per sample direction; nearby stencil offsets can cross faces, records and coverage |
| Coverage neighbor membership | Per immutable dataset/record in principle; left unchanged because no measured payoff proof |
| Cardinal/cross/fine qualification | Per query and refinement; later work needed only if preceding required predicates pass |
| Batch state | None required or introduced; consumers can reuse the existing immutable provider |

The previous loop calculated cardinal, rotated and fine stencils before its
conjunction. Now cardinal failure advances exactly the same cardinal history and
continues; rotated failure skips the fine stencil. Every successful predicate,
step-halving, rotated axis, `h/3` sample, final normal, finite/outward guard and
failure result is preserved. Negated `<` checks preserve NaN rejection.

No value is shared between points, cached across calls or moved into acquisition.
H is pure over admitted immutable data, so skipping unused evaluations cannot
change state. There is no new invalidation seam. Future hot replacement would
require a new explicit authority contract; the current loader cannot hot-replace.
Rendered generation, GPU residency, pupil, LOD and camera never enter this API.
A batch interface is not needed for the measured recovery; hidden center-sector
reuse would be unsafe without proving every offset remains in the same domain.

## KSA current source and official history — ADAPT

Current installed `E:/Kitten Space Agency/KSA.dll`, version `2026.9.7.5402`, SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`, matches the
verified cached production decompilation at
`E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/`.

- `TerrainPatchState.cs:7–44` retains anchor, FP64 physical vertices, dirty
  rows/columns and content version. `TerrainPatch.cs:103–170,213–260` initializes
  patch context and reuses/shifts prepared state.
- `ConstraintSim.cs:622–700` avoids terrain geometry upload when dirty/version/
  mode/guard state is unchanged. Contact uses prepared physical geometry.
- `ConstraintSim.cs:19–32,510–598` resolves launchpad poses once per static-object
  pass and celestial identity; nearest-pad selection and origin conversion remain
  per vehicle. `PhysicsBubble.cs:1984,2664` resets that cache for its owning pass.
- `Celestial.cs:791–835,1584–1606` still performs direction-dependent terrain-height
  composition and bicubic lookup. No exact equivalent of NovaCore's regional mask
  and qualified canonical normal was established.

Official authenticated browser history was rechecked narrowly:
[revision 5382, September 1](https://discord.com/channels/1260011486735241329/1260112103134724146/1544200205769900113)
moved repeated launchpad-pose resolution from vehicle/microstep calls into a
pass-owned cache. Its reported roughly 1,985→162 us was **KSA's workload**, not a
NovaCore forecast. The current source retains pass/body invalidation rather than
an indefinite cache.

**ADOPT:** physical authority remains separate from presentation; resolve immutable
facts at their valid lifetime. **ADAPT:** NovaCore already acquires its immutable
dataset context outside Query; the measured remaining waste is query-local
qualification work, not missing KSA-style pose caching. **INTENTIONALLY DIFFER:**
retain full canonical H/FP64 point-normal qualification rather than substituting
KSA patch/manifold normals, because NovaCore's accepted query contract requires
that physical agreement. No Bepu or contact solver was added.

## Correctness

All **564** points remain bit-identical to the pre-revision implementation:
**527 Ready / 37 NormalUnqualified**, including 12 actual clamp creases explicitly
rejected. The fingerprint includes inputs, full authority, statuses and every
result FP64 component: SHA-256
`3e08f86757588127099cbb100d3829b73d5e5f29b1de8d0f22ec83de28534912`.
It was captured from the original implementation before editing, then asserted
by the rebuilt permanent tests in both configurations.

Independent reference error remains: height **9.094947017729282e-13 m**, position
**0 m**, normal **2.181364183438901e-7 rad** against **5e-5 rad**; support-plane
error **4.691547355542352e-8 rad**. Changed-output difference is **zero**.
Deterministic repeat/order, global/regional boundaries, stale/malformed/mismatched
authority, incomplete data, and camera/render-generation independence remain
covered. An unqualified normal remains a real failure state, not surface support.

## Final actual-production performance

The same native-free benchmark loads preserved original or final Release DLLs in
four fresh processes: A-forward, B-forward, B-reverse, A-reverse. Exclusive CPU
timing; 1,024 samples per pattern/process after 32 complete pattern warmups.
`DOTNET_TieredCompilation=0`, `DOTNET_ReadyToRun=0` control JIT tier/order effects.
These absolute costs must not be merged with the earlier default-PGO 4.06 ms.
The separate default-tiered prototype corroborates material recovery.

Four leg centers form an **8×8 m** square; 4/8/16 contacts use one/two/four points
per leg, with ±0.15 m subcontacts. Nearby16 uses distinct directions across a 6 m
strip; boundary16 crosses the regional coverage edge. Coordinates/arrays are
preallocated; timing includes every Query, index rotation and result store.
Full result hashes are consumed outside timing and match across all four runs.

All costs below are **microseconds**, ranges across two processes per variant.

| Pattern | Original median | Revised median | Revised P95 | Revised P99 |
|---|---:|---:|---:|---:|
| Single regional | 338.7–346.1 | 135.6–137.9 | 163.6–228.8 | 194.6–297.1 |
| Identical regional ×16 | 5596.9–5813.1 | 2253.5–2356.4 | 2422.5–2711.4 | 2652.4–2880.9 |
| Nearby distinct ×16 | 3506.5–3647.5 | 1608.2–1609.0 | 1959.3–2057.8 | 2111.2–2256.5 |
| Four legs / 4 contacts | 709.5–720.0 | 345.9–366.1 | 421.1–506.9 | 471.3–521.2 |
| Four legs / 8 contacts | 1431.7–1433.4 | 693.0–694.7 | 826.9–879.4 | 884.6–928.0 |
| Four legs / 16 contacts | 2926.4–2958.1 | 1377.4–1394.7 | 1588.0–1592.9 | 1679.4–1683.0 |
| Coverage boundary / 16 | 2047.4–2174.7 | 1094.8–1123.3 | 1288.8–1350.5 | 1373.9–1487.0 |
| Florida / 16 | 671.3–697.7 | 684.4–687.6 | 798.0–942.8 | 922.7–1047.2 |
| Inland / 16 | 2544.4–2636.0 | 1261.0–1305.2 | 1382.1–1673.8 | 1429.2–1792.5 |
| Unqualified south pole / 1 | 570.5–596.2 | 195.8–203.2 | 240.5–322.4 | 271.7–394.5 |

**Zero timed bytes/allocations and zero GC collections** in all patterns/runs.
Florida already qualifies on its first refinement; no gain claimed there.
Full original/revised P95/P99 and absolute recoveries are in
[actual-result.json](qualification/actual-result.json).

### Setup and amortized capacity

Nothing was moved into setup. Existing cold-process costs with populated OS cache:
global decode/verification 70.5–75.9 ms; regional resolution 300.8–326.3 ms;
regional read/decode/hash/publication 200.4–205.3 ms; first acquisition 7.2–8.1 ms;
first regional query including JIT 14.4–15.8 ms. This is not a cold-disk guarantee.
Harness coordinate construction with its own JIT costs 9.3–10.2 ms; per-pattern
warmup/preflight are preserved in the external raw summary JSONs.

Warmed `TryAcquire` is **35.8–41.8 us and 70,232 allocated bytes/call**, unchanged;
it must not be performed per contact. Provider acquisition follows publication
and is reused. No new context cost needs amortization.

Illustrative, not an assigned simulation cadence: 16 contacts at 60 reevaluations/s
consume about **83 ms of one CPU core per second**, or **167 ms/s at 120**; four
contacts at 120 consume **42–44 ms/s**. This leaves useful room for an initial
craft and additional simulation, without establishing rich high-rate multi-craft
capacity. Several 16-contact craft still consume substantial CPU. Actual future
contact scheduling, solver cost and simulation cadence must be measured together.

## VERIFY A — authority/lifecycle red team

**PASS.** Strongest objection: skipping stencils could change refinement history,
NaN behavior or authority through hidden state. Source audit confirms identical
history/acceptance/sample coordinates, negated-`<` rejection, pure full H, unchanged
readiness and no new state. Complete 564-output identity and boundary/failure
tests reinforce the audit. No new stale-cache or presentation dependency exists.
Numerical qualification still does not certify differentiability everywhere.

## VERIFY B — performance red team

**PASS.** Strongest objection: identical-point bias or cost shifted into setup.
Actual DLL ABBA, distinct four-leg/nearby/boundary patterns, accounted startup,
zero allocations and unchanged Florida cost defeat that explanation. Controlled
JIT and default-PGO measurements independently support the causal direction.
No unlimited-craft/headroom claim is made. See the
[independent review](qualification/actual-review.md).

## Validation, storage and judgment

Debug and Release Graphics test builds passed. Debug has zero warnings/errors;
Release completed with zero errors after eight transient MSB3026 copy retries
while the earlier diagnostic process held the test assembly. The lock released,
deployment completed, and the verified final assembly hashes were used in both
actual-DLL timing and parity. The warnings are retained, not described as a
warning-free build.

Both configurations passed **11 distinct relevant headless cases**, zero failures
or skips: nine physical cases plus terrain asset distribution and local payload
contract. Native regional identity/readiness/cache tests and mapped-memory policy
tests (30 checks) passed in both configurations using existing matching binaries;
native source did not change. No unrelated full suite or stability soak was run.

The starting and final source/assembly identities, original output fingerprint,
build/test logs, compact cost attribution and actual-DLL results are retained in
`qualification/`. The full native-free benchmark and pre-revision source snapshots
remain at `E:/NovaCore-Research/surface-query-qualification-20260909/verify-b/`;
the counted-H/prototype recipes remain in its sibling `cost-proof/`.
To reconstruct the old benchmark after manual binary cleanup, export banked HEAD
into private output, overlay the recorded `starting-source` files, build Core/
Graphics/Interop in Release, verify their recorded hashes, and repopulate the
benchmark's referenced `starting-assemblies`/private host paths before running
its preserved runner. The corrected side uses the fingerprinted final source.
Do not run that reconstruction over the current worktree or silently use the
corrected DLLs as the original baseline.

Fresh final-candidate **Florida and inland raster parity: PASS**, two 200-frame
runs at 3440×1440, capture frame 175. Both match the recorded source-exact banked
baseline in prepared FP64 transport bytes, oriented triangle multiset, complete
D32 depth, HDR attachment and final image, plus all composition/camera/publication
and asset inputs. Florida has 339,054 selected triangles, inland 4,086; each uses
712,106 prepared vertices. All 49 shaders and the private capture native binary
are baseline-identical. The previous initial-candidate host was preserved.

Khronos validation was **actually loaded** in both processes, with **zero Vulkan
errors**; the existing `OutputNotConsumed` non-error warning remains visible.
The normal launcher deployment was not replaced. Private capture timings are not
performance data or an indefinite-stability claim. See
[final parity result](qualification/final-qualified-result.json) and its adjacent
full dependency identities, execution records and output digests.
**Manual visual acceptance: NOT REQUIRED**, preserving the accepted API ticket's
gate because both numerical and raster outputs are unchanged.

`git diff --check`: PASS (line-ending conversion warnings only). The index is
empty; no staged work, commits, tags, pushes, merges or banking.

### Evidence lifecycle

**PROJECT CONTROL CLEANUP REQUIRED.** Exact literal commands and group-by-group
counts are in [manual-cleanup.md](manual-cleanup.md) and
[manual-disposable-inventory.json](qualification/manual-disposable-inventory.json).
The groups have no production runtime, permanent test or asset-regeneration consumer.

| Disposable group | Files | Logical bytes remaining |
|---|---:|---:|
| Original proof debris, reconfirmed | 683 | 45,650,081 |
| Cost-prototype build output + superseded summaries | 34 | 1,455,607 |
| Actual-DLL benchmark hosts/intermediates | 46 | 3,863,793 |
| Final parity private host | 67 | 6,830,363 |
| Final parity raw files | 6 | 253,782,448 |
| **Total** | **836** | **311,582,292** |

Prior raw disposal remains **507,564,896 bytes**; this follow-up disposed **0**.
New raw output is only two bounded frames, not a frame archive. Once exact hashes
were recorded it became disposable and is included in the literal manual commands.
No broad deletion or policy bypass was attempted. The cost worker's attempted
removal of two superseded JSON summaries was rejected by automatic review with
only `blocked by policy`; no retry occurred. Engineering qualification is not
blocked by that permission boundary. Before banking, Project Control should
remove the classified output or explicitly accept justified retention.

Compact report/hash/identity/test evidence and reproducible source are retained;
ordinary NovaCore build outputs, production assets and permanent fixtures are
preserved. [Storage checkpoint](qualification/storage-checkpoint.json) records
retained evidence and new diagnostic storage separately from the old debris.

## Lead judgment

**PASS** — one measured responsibility removed unnecessary work, realistic
footprints improved materially, and every physical/readiness/raster gate survived.
This qualifies the bounded query service; it does not authorize later contact
mechanics or promise arbitrary multi-craft capacity.

## Final classification

**SURFACE-POINT QUERY CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE**

Proposed accomplishment title: **Provide immutable canonical surface-point queries
for spacecraft contact**. Return unbanked and stop for Project Control.

## Final Git status

Tracked diff: seven files, 40 insertions / nine deletions, unchanged from the
starting tracked candidate. This follow-up's provider/test correction and evidence
edits are within files already untracked at the starting checkpoint; ordinary
`git diff --stat` excludes them. Their exact source identities are retained in
`qualification/final-source-identity.json` and the original identity snapshot.

```text
 M docs/CODEX_HANDOFF.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/engineering-evidence/README.md
 M docs/planetary-rendering.md
 M src/NovaCore.Graphics/PlanetaryLocalTerrain.cs
 M src/NovaCore.Graphics/TerrainAssetDistribution.cs
 M tests/NovaCore.Graphics.Tests/Program.cs
?? docs/engineering-evidence/surface-point-query/
?? docs/surface-point-queries.md
?? src/NovaCore.Core/Surface/PhysicalSurfacePointQuery.cs
?? src/NovaCore.Graphics/PlanetaryPhysicalSurfacePointQuery.cs
?? tests/NovaCore.Graphics.Tests/PhysicalSurfacePointQueryTests.cs
```
