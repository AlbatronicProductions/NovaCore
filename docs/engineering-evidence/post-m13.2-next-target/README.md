# Post-M13.2 next-target investigation

Decision: **NO WINNER YET**. No M13.3 is assigned and no production optimization
is left in the working tree. Earlier rejection has a substantial measured
whole-patch opportunity, but not yet a proven safe, valuable implementation.
The exact TES scheduling probe has no meaningful measured benefit.

## Baseline and measurement boundaries

Started on clean `main`; HEAD, origin/main and annotated
`m13.2-ordinary-terrain-shading` resolve to
`4accf92fd080c16cc8656080aa69def3fa65db53`.
Annotated `m13.1-ncsm1-tes-hotpath` resolves to
`fade1384c1c7df93d954e7223b1cc8f17db17f98`.
Investigation branch: `codex/m13-next-performance-target`. Neither tag moved.
RX 6800 XT, Release, native 3440 x 1440, fixed J2000, canonical strict Vulkan.
`baseline.json` records the initial Debug/Release dependencies and 49 shader hashes.

`clean-runtime.json` is the **uninstrumented banked executable** check performed
before applying any probes. Orbital and factor-1 100-sample terrain medians were
0.83100 and 6.55016 ms. Normal Florida startup and the existing horizon diagnostic
were also run; their aggregate records are retained, not misrepresented as the
identical later fixed-pose series. The banked grazing-directional control was at
136 m. Temporary host instrumentation supplies exact 10.004 m and Florida fixed
poses for the complete matrix below. Ordinary production shader selection is
unchanged in these timing runs. The factor-1 comparison is 6.55016 versus 6.54028
ms; orbital is 0.83100 versus 0.83140 ms.

An initial control omitted the established starting altitude and retained an
orbital near plane. Its empty near-surface draw was excluded. The input/log hash
record `initial-projection-control.json` preserves that exclusion. The corrected
recipe explicitly passes `--altitude=10.004`. No invalid timing is used below.

Each matrix row uses the last 100 aligned warmed frames. GPU timestamps are
terrain draw/total frame intervals, not an isolated TES duration. CPU submission
and waits are separate; fence wait is not CPU terrain computation.

| Ticket pose | Terrain median / P95 ms | Total GPU median ms | Selected/TCS patches | TES invocations | Hardware clipping input / output | Fragment invocations |
|---|---:|---:|---:|---:|---:|---:|
| A orbital, level 0 | 0.83140 / 0.84156 | 0.98880 | 15,376 | 46,128 | 12,893 / 12,893 | 35,913 |
| B factor-1, level 17, approximately 136 m | 6.54028 / 6.73788 | 9.40848 | 360,950 | 1,082,850 | 142,653 / 142,653 | 2,591,685 |
| C Florida fixed authored-site view | 8.38312 / 8.73156 | 11.24696 | 294,748 | 884,244 | 119,658 / 119,654 | 3,984,347 |
| D active, 10.004 m, pitch -1 | 23.92364 / 24.03032 | 26.75952 | 2,060 | 2,779,256 | 953,917 / 946,137 | 4,953,599 |
| E grazing, 10.004 m, pitch -0.001 | 13.93588 / 14.11132 | 16.94323 | 378,919 | 3,470,006 | 245,740 / 235,575 | 2,472,104 |

| Pose | Cull/compact ms | CPU record ms | CPU submit ms | Fence wait ms | Present call ms |
|---|---:|---:|---:|---:|---:|
| A | 0.09748 | 0.1453 | 0.0390 | 0.9076 | 0.1852 |
| B | 2.68768 | 0.1793 | 0.0455 | 9.3109 | 0.1939 |
| C | 2.65488 | 0.1871 | 0.0457 | 11.0589 | 0.3029 |
| D | 2.49304 | 0.2690 | 0.0619 | 26.5082 | 0.3900 |
| E | 2.70028 | 0.2298 | 0.0548 | 16.7266 | 0.3460 |

All source-native sample labels are explicitly mapped in `capture.py`: inherited
runner A/B/C/D/E means ticket B/D/E/C/A. File suffixes and this report use ticket
labels. Detailed factors, input counts, frame identities, arguments, environment,
P95/min/max and shader/compiler identities are retained in `results.json`.

## A: where late discard occurs

Active-frame causal flow:

| Stage | Input | Reject | Survive | Information / cost already paid |
|---|---:|---:|---:|---|
| Persistent topology / prepared physical owner | 1,424,208 triangles | 0 invalid at publication | 1,424,208 | Residency, canonical base, normals, publication already ready |
| GPU horizon | 1,424,208 | 240,353 | 1,183,855 | Prepared corner positions, support sphere, TES envelope; preparation paid |
| Broad cone/frustum | 1,183,855 | 1,181,709 | 2,146 | Enclosing sphere with displacement allowance |
| Narrow plane support | 2,146 | 86 | 2,060 | Prepared triangle, plane support; all upstream work paid |
| TCS | 2,060 | 0 | 2,060 patches | Accepted fractional-odd shared factors, up to 64 |
| Final triangle capture | 3,824,770 generated | 2,876,036 clipped | 948,734 clip polygons | Actual final positions; all TES physical work already paid |
| Early-depth fragment entry | 948,734 polygons | 11,228 produce no fragments | 937,506 primitives enter | Clipping, raster coverage and early-depth state available |

GPU query clipping counts differ from explicit generated-triangle counts: this
driver can eliminate geometry before the counted hardware clipping boundary.
The geometry-shader capture changes invocation reuse and timing. Its 64.05 ms
active draw is **not** a production timing or an optimization saving.
No attempt is made to sum these unlike counters as a single hardware pipeline.

Capture-control qualification: B/D/E combined depth/HDR/image bytes match the
TES-only capture. Repeated A/C controls preserve exact physical record sets and
depth, but A has 2 differing HDR bytes (image exact); C has 1,052 differing HDR
bytes and 1 differing final-image byte. This remains an unresolved diagnostic
pipeline interpolation/shading difference; its exact initiating instruction was
not established. The GS oracle is therefore not universal image-parity evidence.
One additional A GS control exited `0xC0000005` after writing its frame-175 capture
and shutdown statistics, without a VUID. Its repeat completed successfully.
The access-violation cause is unclassified; it is not silently attributed to OBS
or called a production failure. Both limitations are recorded for bounded
diagnostic follow-up. No production candidate is promoted through either gap.

Active inner-factor bins (<=1,2,4,8,16,32,>32):
`97,120,157,255,344,372,715`; maximum inner 63.993599, outer 64.
Selected compacted indices: 6,180. Maximum projected edge: 8,335.617188 pixels.
These factors are observed, never reduced.

| Pose | Generated triangles | Fully clipped | Wholly clipped parents | Clipped triangles in wholly clipped parents | Clipped triangles in partially visible parents |
|---|---:|---:|---:|---:|---:|
| A | 15,376 | 0 | 0 | 0 | 0 |
| B | 360,950 | 0 | 0 | 0 | 0 |
| C | 294,748 | 4 | 4 | 4 | 0 |
| D | 3,824,770 | 2,876,036 | 1,659 | 2,763,495 | 112,541 |
| E | 3,597,931 | 3,039,353 | 797 | 2,999,661 | 39,692 |

Thus 96.09% of active clipped triangles come from wholly clipped parents, not
partially visible patch amplification. Wholly clipped parents account for 72.25%
of all generated active triangles. The measured ~75.20% is reproduced exactly.

Active exclusive clip classification (first separating plane, avoiding double
counting): left 1,187,677; right 1,018,993; bottom 136,682; top 532,683; near 0;
far 0; multi-plane intersection with no single separating plane 1. Overlapping
per-plane counts are separately retained. No surviving active polygon is
back-facing. Grazing has 196,045 opposite-winding clip survivors and a much larger
set of no-fragment parents; zero fragments alone does not identify horizon versus
occlusion versus subpixel raster coverage. Those are not silently called safe
early rejects. Already horizon-rejected parents are counted upstream.

### Conservative bound and diagnostic oracle

The existing culler already tests **prepared physical** triangles, including
global/regional terrain and facility grading. It is not culling nominal sphere
topology. A 50 m point-to-triangle test activates TES screen support. The broad
cone/sphere and narrow plane tests use a scalar displacement envelope; near a
roughly 2 m base mesh, an 11.8126 m isotropic envelope retains many triangles whose
actual final surface is outside the view. At the measured frame these are bound
false positives, not a proof the allowance may be deleted for all frames.

The diagnostic CPU zero-envelope test predicts rejects and compares them with
the complete accepted final triangle/fragment capture. It never changes selection:

| Pose | Proposed rejects | True fully clipped | False clip / fragment rejects | False-retained wholly clipped parents | Generated triangles in proposed rejects |
|---|---:|---:|---:|---:|---:|
| A | 0 | 0 | 0 / 0 | 0 | 0 |
| B | 0 | 0 | 0 / 0 | 0 | 0 |
| C | 4 | 4 | 0 / 0 | 0 | 4 |
| D | 1,656 | 1,656 | 0 / 0 | 3 | 2,762,184 |
| E | 797 | 797 | 0 / 0 | 0 | 2,999,661 |

**Rejected as a production algorithm.** This CPU reference uses logged matrix and
same-frame prepared/compacted inputs; it is not a bit-identical GPU predicate.
It omits nonzero physical displacement and supplies no universal containment or
roundoff bound. Zero observed false rejects does not make it conservative. No
unsafe culling A/B is used to manufacture recovered milliseconds. Reapplying the
already executed accepted bound proposes zero additional rejects. Final clipping
of whole parents is an upper ceiling requiring information obtained after TES,
not an earlier algorithm. Actual TES executions saved cannot be obtained by
multiplying GS triangle counts: driver reuse changes that accounting.

There is also a specific envelope derivation to reconcile before tightening it:
`PlanetaryNaturalTerrainFamilies.Bounds` includes `ShapeLinear` in NearHeight;
the manifest stores 11.812586507619743 m. The TES near-only path calls unshaped
`NaturalTerrainFamilyScale` for both families. Its raw conservative amplitude
bound is `11*sqrt(3) = 19.0525588833 m` before range/support attenuation. This is
a **proof gap**, not a measured terrain escape or authorization to change H.
Warp changes location, not the noise amplitude bound; family blending and support
weights must be bounded using their actual implementations. Prepared height's
zero clamp and final floating-point projection must also enter the proof.

A promising next diagnostic is a signed radial displacement interval projected
onto each clip-plane normal, with range/family/support intervals derived from
current authority. For a prepared patch sphere of center c and radius r<|c|,
the direction cone is bounded in real arithmetic by asin(r/|c|); this can bound
plane-normal radial support more tightly than a sphere in every direction.
That formula alone does not bound FP32 interpolation, quaternion rotation or
projection error. No arbitrary new padding or assumed constant family is adopted.

Remaining 112,541 active clipped triangles inside partially visible patches cannot
be recovered by rejecting the whole patch. Changing tessellator layout/factors
would change the accepted surface. No fine-grained pre-TES primitive scheduler is
established here. A stronger conservative oracle and exact A/B across 40/50 m,
pupil snaps, incoming/current publications, seams, Florida and low-angle motion
remain prerequisites. The rejected zero-envelope predicate is not stress-certified.

## B: what TES still executes

TES constructs final body direction from interpolated camera-relative prepared
positions, evaluates analytic near height/gradient only when the 40-to-50 m fade
is nonzero, applies facility support attenuation, composes the final normal,
rotates radial displacement, and emits required raster/material inputs.
It does **not** independently reload regional residual data or reevaluate the
entire global/macro/meso physical base per final sample. Those responsibilities
are already prepared. M13.1 geographic addressing is disabled for ordinary TES;
M13.2's ordinary consumer already lets the compiler drop unused outputs.

| Capture | TES executions | Exact unique directions | Repeated executions | Within-patch repeats | Additional patch/direction identities across patches | Near executions / outside |
|---|---:|---:|---:|---:|---:|---:|
| D | 2,779,238 | 1,913,891 | 865,347 | 781,756 | 83,591 | 2,779,083 / 155 |
| E | 3,470,023 | 1,801,715 | 1,668,308 | 658,859 | 1,009,449 | 2,338,440 / 1,131,583 |

Active cross-patch direction groups: 79,600; edge executions: 174,729. Grazing:
254,077 groups, 1,273,494 edge executions. Exact frequency distributions are in
`results.json`. Nearby but different bit patterns are not merged. These are
capture-shader execution counts and vary slightly with driver invocation reuse;
physical sets remain identical. Direction identity can key near-field authority,
but alone does not key the complete final record: fade, prepared base, frame and
publication also matter. No cross-TES synchronization or bounded cache hit-rate
architecture is proven by this census.

Camera, physical identity, radius and presentation uniforms already come from
immutable buffers; ISA includes scalar loads. Per-patch corners/heights/normals
already cross TCS. Final direction, family blend, warp and near gradient vary
within a patch. No expensive additional patch-constant physical term was proven.
Reusing an interpolated prepared normal as final analytic near normal is not exact.
Adding patch varyings or a hash cache would incur bandwidth, occupancy and
publication/synchronization work that has not been shown cheaper.

The sole exact probe defers base-height interpolation and base-normal construction
until after near-field evaluation. Its expressions and arithmetic are unchanged;
this tests scheduling/live-range pressure, not approximate physics.

| Active uninstrumented timing pair | Normal terrain median ms | Probe terrain median ms | Difference ms |
|---|---:|---:|---:|
| Normal then probe | 23.93880 | 23.93408 | 0.00472 |
| Probe then normal | 23.94856 | 23.93600 | 0.01256 |

Differences of 0.020% and 0.052% are below the observed run/sample variation;
**no meaningful saving is established**. Pair timing and total GPU statistics
are retained. Capture timings are reported separately and are not used to claim
performance. The full five-pose probe comparison has exact prepared bytes,
physical record sets, D32, HDR and final image. Dispatch primitive IDs and
redundant execution multiplicities are not physical identity; their raw record
hashes differ and are explicitly retained rather than hidden.

Driver ISA: 190 VGPR, 36 SGPR, wave32, 4,608 LDS bytes, zero scratch for both;
static instructions 7,658 -> 7,651; FP64 instructions 3,094 -> 3,094.
Ordinary FS remains 85 VGPR / 53 SGPR. `compiler.json` records instruction
families and source/ISA hashes. Static instruction counts are not dynamic
execution counts. Near-branch participation was measured above. AMD shader-info
is available; RGP/RGA executables were not found in PATH or the inspected standard
AMD locations. Dynamic occupancy, cache misses, cycle-weighted VALU/SALU pressure
and isolated TES duration were not obtained. They are not inferred from VGPR count.

## Comparison and decision

| Candidate | Total measured cost | Proven avoidable / tested saving | Expected recoverable ms | Risk and complexity | KSA evidence / confidence |
|---|---|---|---|---|---|
| A earlier rejection | Active terrain 23.92364 ms; 2.876 M clipped triangles | 2.763 M triangles in wholly clipped parents is a hindsight ceiling; no proven earlier removable work | Not established | High correctness risk until displaced/roundoff bound is proved; bounded culler ownership if proved | KSA uses prepared bounds and corrected missing displacement tolerance; high confidence in cause, insufficient safe-saving proof |
| B TES pressure | Same active draw; almost every active invocation enters near evaluation | Exact scheduling probe: no meaningful saving; duplicates are potential reuse, not an available cache | No measurable benefit from tested probe | Scheduling low risk but no value; cache substantially more complex | KSA prepares coarse data and retains final-sample work; high confidence in rejecting this probe |

**NO WINNER YET.** Required next proof is an authoritative bound reconciled with
the actual TES evaluator, a tighter conservative GPU predicate with zero false
rejects through the full dynamic matrix, and exact output-preserving timing.
Alternatively B needs a concrete exact reuse/hoist with measured total-work
savings, not static cost or duplicate count alone. Do not assign M13.3 from this
investigation or describe the triangle ceiling as saved milliseconds.

[KSA current source and live history](ksa-comparison.md) directly informed this
decision. No renderer, tessellation quality, physical authority, support, residency,
material, ownership or publication correction is proposed for production here.

## Reproduction and evidence lifecycle

Permanent package budget: 3 MB. Retain scripts, compact numerical results,
provenance, compiler summaries, validation and this report. No raw attachment or
full ISA archive is a permanent fixture. Existing banked helper modules remain
referenced rather than copied wholesale. Production has no dependency on this folder.

1. On the recorded clean baseline, run `clean_baseline.py` before instrumentation.
2. Apply this package's `instrumentation.patch`; build native and Triangle Release.
3. `run.py` runs one inherited pose label and probe in an isolated deployment.
   Run baseline E/A/D/B/C with labels baseline-A/B/C/D/E. For active scheduling,
   run B normal/defer-base then defer-base/normal. Do not overlap GPU tests.
4. With NumPy available, `capture.py parents` captures the five parent oracles;
   `capture.py tes` runs exact duplicate census and five-pose scheduling parity.
   Capture frame is 175; 100 warmed timing frames are used. Raw per-run temporary
   directories are disposed only after numerical analysis and hashing.
5. Restore only the two instrumented production files, rebuild Release, and run
   `validate.py build`, `tests`, `traversal`, `identity`.
6. Consolidate compact JSON and compiler summaries before retiring the bounded
   `build/post-m13.2-next-target` scratch files. Never target existing evidence,
   source assets, deployment, caches or repository history.

`closeout.json` and `final-validation.json` record the actual final validation,
capture-control limits, storage totals and status. No manual acceptance is needed
for this investigation-only candidate.

## Final validation and remaining responsibility

Restored Release native, Triangle and Graphics test builds: PASS. Focused Release
Graphics: GPU 8/8; six selected headless contracts 6/6; visible-window lifecycle
1/1, all with zero failures/skips. Launcher regression executable: PASS. Both
production Earth assets verify. Full restored orbital/surface traversal, bounded
Florida smoke and restored factor-1 control: PASS under strict validation with
no VUID. The restored factor-1 terrain median is 6.56272 ms (100 samples).
All 49 Debug and 49 Release shader modules validate and match banked deployment
hashes. Each native runtime matches its own configuration's build. Release native
and managed binaries were freshly rebuilt; their new hashes are recorded, not
misrepresented as byte-identical to the initial binaries. Production source is
restored exactly. `git diff --check`: PASS.

Diagnostic failures are not buried in that validation: the first GS setup used
the wrong private shader filename and reported missing shader plus
`VUID-vkDestroyDevice-device-05137` during failure teardown. Correcting the private
filename resolved that setup error. The later one-off `0xC0000005` and GS HDR/image
differences remain as described above. They are not a license to alter production
rendering or weaken validation.

Remaining bounded proof: reconcile near-displacement bounds with the actual
evaluator; prove the tighter predicate including floating-point error; validate
its oracle through the required dynamic matrix and then measure exact-parity
timing. Separately, establish a universal image-exact primitive capture control
and classify the diagnostic exit failure before using that tool as an acceptance
gate. No cache, renderer redesign or repository-debt cleanup is authorized by
these findings.

Storage: completed serialized raw captures total **9,467,741,400 bytes**, already
disposed by per-run temporary-directory lifecycle. Final scratch contains
**198 files / 41,454,002 bytes**. Known generated diagnostic output is therefore
at least **9,509,195,402 bytes**; this deliberately excludes transient isolated
runtime copies, compiler writes, and the failed capture whose full manifest was
not retained. It is not an exact cumulative disk-write counter. Permanent evidence
is approximately 1.22 MB within the 3 MB budget; exact final totals are in
`closeout.json`. No bulk raw capture remains. Source assets, caches, old evidence,
the launcher and runtime deployment were not targeted.

**Cleanup blocked by automatic approval review.** The exact 198-file set was
verified against `scratch-retirement-manifest.json`: all Git ignored, contained
under `E:/NovaCore/build/post-m13.2-next-target`, no reparse points, size/hash match.
The deletion command was rejected with “blocked by policy” before execution;
no more specific reason was supplied. No alternate deletion mechanism was tried.
The user may manually remove that verified disposable scratch folder after
checking it still matches the manifest. It contains only this ticket's logs,
compiler output and duplicated compact JSON, not production deployment or assets.
Disposable remaining is therefore **41,454,002 bytes**, not zero.

Final classification: **PARTIAL PERFORMANCE DIAGNOSIS — FOLLOW-UP REQUIRED**.
Winner remains **NO WINNER YET**. No M13.3 is assigned. No staging, commit, push,
tag change or production optimization occurred.

Complete final `git status --short`:

```text
?? docs/engineering-evidence/post-m13.2-next-target/
```
