# Residual regional transition frame gauntlet

**Lead judgment: ESCALATE TO PROJECT CONTROL**

**Technical classification: NO SAFE M13.5 TARGET YET**

**M13 closure: M13 STILL NOT READY — ONE BOUNDED BLOCKER REMAINS**

The six recurring failures have a common measured cause: the final incoming
physical-preparation slice additionally builds a camera-specific visibility and
compacted-index list. The next frame publishes the physical generation and
rebuilds that list through the current-owner path before its first draw. The
incoming walk also performs required geometry validation, so deleting it would
delete correctness checks.

A private control moves immutable index-range validation to topology acquisition
and checks every prepared vertex once on the GPU. It preserves the former
finite-position/normal predicates while removing the unused incoming camera
list. Its six frames improve to **10.17–10.67 ms**. This is a useful causal
result, not a production-qualified implementation.

Project Control's updated hard current-framework target is **8.33 ms**; preferred
headroom is 6.94 ms and stretch headroom 6.67 ms. The future-game 11.11 ms target
and 16.67 ms Ultra floor are not current M13 acceptance gates. The revised
control still has **37/44** original transition samples above 8.33 ms. Even a
zero-cost final incoming block cannot bring this regime under the new target.
No production correction, new milestone, or wider optimization was undertaken.
The valid validation-ownership direction is retained for Project Control rather
than discarded because the earlier ticket used a different threshold.

## Baseline and scope

- HEAD, main, origin/main and dereferenced
  `m13.4-zero-contribution-terrain-material-noise`:
  `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
- Branch: `codex/m13-regional-preparation-blocker`.
- M13.3: `m13.3-prepared-physical-terrain` remains
  `180eaf150ba5db6364e17dd48336690778f058f9`.
- GPU: AMD Radeon RX 6800 XT; native 3440×1440; Vulkan 1.4.
- Starting tracked and staged diffs: empty. Three preceding untracked evidence
  packages were preserved. This ticket adds only this fourth evidence directory.
- Normal Release runtime:
  `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0`.
- Normal launcher:
  `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.

`baseline.json` and `closeout.json` contain complete configuration-specific
deployment and asset SHA-256 values. All 49 shaders in each of Debug and Release
remain unchanged. Release native SHA-256 is
`74859dbb7854e92d3069e4081276eef98980945c2c9c4d4adf3a926b18cbaeca`;
Release managed SHA-256 is
`86298b31bc05f7ab061f96567b75e798d7b6443f8437493f1254a49418502894`.
The R16 elevation asset, global NCCUBE cache and Florida NCCUBE cache are intact.

Temporary source instrumentation was rebuilt into a private host. Each build
restored all three touched tracked files and the original normal native DLL in
a `finally` block. Normal native and managed intermediate objects were then
rebuilt from restored production source; see `restoration.json`. There are no
persistent changes to terrain, TES, physical authority, residency, facilities,
materials, tessellation density, launcher behavior or production validation.

## Six failures, their neighbors, and exact responsibility

GPU times below are milliseconds. Original rows are the retained matched
D-mapped run; new rows are this ticket's timestamp control and vertex revision.
The current camera, physical basis, pupil history, topology and preparation
cursor are matched, not inferred from similar screenshots.

| Logical / GPU frame | Current→incoming generation | Pupil; LOD transition | Final slice first/count | Reuse / recompute | Original neighbor → failure | Fresh timing → vertex revision |
|---|---|---|---|---|---|---|
| 343 / 446 | 16→17 | 25→26; L14→15 | 589824 / 57178 | 22782 / 34396 | 9.27008 → 11.19128 | 11.12524 → 10.19840 |
| 355 / 458 | 17→18 | 26→27; L15→16 | 655360 / 41050 | 18791 / 22259 | 10.85488 → 11.30080 | 11.33156 → 10.29268 |
| 367 / 470 | 18→19 | 27→28; L16→17 | 655360 / 56746 | 41250 / 15496 | 9.60728 → 11.65300 | 11.70416 → 10.66576 |
| 723 / 826 | 34→35 | 45→46; L14→15 | 589824 / 57178 | 22782 / 34396 | 9.22764 → 11.14040 | 11.16372 → 10.16788 |
| 735 / 838 | 35→36 | 46→47; L15→16 | 655360 / 41050 | 18791 / 22259 | 10.85732 → 11.30452 | 11.30036 → 10.28452 |
| 747 / 850 | 36→37 | 47→48; L16→17 | 655360 / 56746 | 41250 / 15496 | 9.83860 → 11.62920 | 11.67396 → 10.59928 |

Each neighbor is the preceding logical/GPU frame in the same replacement.
Snap/basis identities are 11 on the first return and 13 on the second. They are
constant within each pair, not collapsed across both returns. Camera body-space
position is approximately (913246.1316078771, 3050496.553466797,
5518278.972975897) metres, altitude 85.05555725097656 metres. Exact matrices,
160-byte current/incoming frame records and actual buffer/memory handles are in
the losslessly retained journals. Recycled Vulkan handles are observations,
not sufficient production lifetime identities.

| Logical frame | Incoming physical preparation | Incoming cull | Incoming compaction | Whole incoming final block | Replacement vertex-validation block |
|---:|---:|---:|---:|---:|---:|
| 343 | 2.63060 | 0.72168 | 0.31860 | 1.09052 | 0.10484 |
| 355 | 1.94272 | 0.78232 | 0.34520 | 1.17760 | 0.10784 |
| 367 | 1.57692 | 0.79676 | 0.35348 | 1.19844 | 0.12152 |
| 723 | 2.63780 | 0.72112 | 0.31800 | 1.08888 | 0.10568 |
| 735 | 1.95136 | 0.78300 | 0.34520 | 1.17932 | 0.11572 |
| 747 | 1.57756 | 0.79696 | 0.35404 | 1.19908 | 0.12364 |

Adjacent compute-stage timestamp boundaries measure reset, reset barrier, cull,
cull barrier, compaction and final barrier. The final block includes those
subscopes; do not sum it with its parts. Incoming physical preparation is nested
inside the older broad cull/preparation scope. The terrain candidate scope is a
pipeline span, not an isolated fragment-shader timer. Detailed draw, current
cull, host submission and completion measurements are in
`frame-pacing-review.json` and `frame-pacing-review.md`.

Incoming reset is approximately 0.0008 ms; the original three barriers sum to
0.04744–0.05036 ms. The residual is primarily executed cull/compact work, not a
one-millisecond barrier or host atomic-swap stall. The final slice arms the
incoming fence; the next `Update` waits and publishes; the next `Record` reculls
the current owner before drawing. Publication first affects logical frames
344/356/368/724/736/748. Current terrain stays authoritative during preparation.

There is no transfer-engine copy command in this path. `transferCommands=0` and
`transferEngineBytes=0` are source-verified diagnostic facts, not hardware bus
counters. Exact reuse is a compute-shader read/write of 64-byte physical records.
The three final-slice copy reads are 1,458,048 / 1,202,624 / 2,640,000 bytes;
destination writes are 3,659,392 / 2,627,200 / 3,631,744 bytes. Device-local,
host-visible coherent physical placement is inherited only inside this private
control; no new staging migration occurs on the failed frames.

The largest non-reusable remainder alone is disproven: the final L16 slice has
22,259 recomputes versus its neighbor's 26,977, and lower physical preparation
time, yet its total fails the old threshold. Its additional final camera-list
work explains that difference. `slice-identity-review.md` contains all 64 slice
frames, range/counts, source/destination placement, copy/work counts and exact
dependency identities. Regional/support negative-oracle counts were retained
per job, not per slice; that missing granularity is explicitly marked rather
than invented or obtained through a new geometry capture.

## Private correction and validation ownership

The first control retained per-triangle index/finite-vertex checks while dropping
camera classification and compaction. It was slower: its final validation blocks
cost 1.35688–1.48056 ms. It is rejected as a performance correction.

The one narrow revision instead:

1. Checks every immutable resident topology index against vertex count once at
   acquisition, and records a resident validation flag.
2. Validates every completed prepared vertex once on the GPU, including copied
   vertices and otherwise unreferenced vertices.
3. Requires the actual completed-element count, zero invalid elements, complete
   physical/dependency state, coherent generation and the final fence.
4. Publishes physical readiness truthfully with camera readiness still false.
5. Runs the unchanged current cull/compact before the first visible draw.

It preserves the old finite body XYZ, finite normal XYZ and normal.w=1 checks.
Neither old nor new validator checks all 64 bytes, unit normal length or finite
stale values. Correct count validation is not an exact physical-word oracle.
The private flag must still be joined explicitly to the publication target's
actual resident topology identity before a production implementation can ship.

The first attempt at this revision exited on startup because the diagnostic
script inserted its old triangle-count expectation after replacing the validator
with vertex counting. The completeness check correctly rejected generation 1.
`startup-control-correction.json` retains the failure, hash and bounded repair.
The expectation was corrected and the private host rebuilt; this was a harness
repair within the same revision, not removal of the failed check.

Temporary counter 23 holds validated elements in incoming storage but also has
a current-render TES telemetry meaning. The current reset clears it before
draw; publication-boundary telemetry can nevertheless interpret the temporary
value incorrectly. Hardware TCS/TES counts remain the workload comparison.
A production implementation requires dedicated typed validation evidence.

Diagnostic sources are `residual.py`, `timing.inl`, both `incoming_validate*.comp`
files and the recorded private-host patches. Production source is restored.
The earlier exact-copy/device-local composition remains rejected; it has not
been promoted by this successful causal isolation.

## Performance, publication and CPU cost

Nearest-rank percentiles; no tails omitted. Original target44 covers two L15→16
and two L16→17 jobs. It contains four of the six old failures; logical343/723
belong to the preceding L14→15 jobs. The expanded64 covers all six jobs. This
corrects the earlier shorthand that associated all six failures with target44.

| Target44 run | Mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|
| Original production A, retained | 14.25709 | 14.94804 | 16.68492 | 16.68492 | 44 | 44 |
| Rejected D-mapped, retained | 9.14063 | 11.30452 | 11.65300 | 11.65300 | 35 | 4 |
| Fresh timing control | 9.15018 | 11.33156 | 11.70416 | 11.70416 | 39 | 4 |
| Triangle validation | 9.15752 | 11.62280 | 11.95288 | 11.95288 | 35 | 4 |
| Vertex validation | 9.05196 | 10.69556 | 10.98624 | 10.98624 | 37 | 0 |

| Vertex revision sample set | Count | Mean | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Expanded six-job transition | 64 | 8.73007 | 10.66576 | 10.98624 | 10.98624 | 42 | 0 |
| All strictly aligned Earth observations | 839 | 7.89341 | 9.28412 | 10.63988 | 10.98624 | 531 | 0 |

The 1,000 logical-frame route does not have 1,000 valid Earth GPU observations.
Use `gpuFrame == geometryFrame == hostFrame - 1`; an older correlation helper
could overwrite one Earth observation with a later stale non-Earth alias.
Complete exact input comparisons and corrected statistics are retained in the
independent frame-pacing review.

The observed six-frame savings are approximately 0.93–1.07 ms. The affected
incoming block itself saves 0.98320–1.07692 ms. This does not imply a one-ms
improvement on every transition frame. The fresh timing control's **40 nonfinal
target frames** already average 8.914945 ms, peak at 10.93544 ms and contain
35 frames above 8.33 ms. Even subtracting its entire final incoming block
hypothetically leaves target44 mean 9.042123 ms and peak 10.93544 ms. The
zero-cost subtraction is only a bound, not a measured correction.

All three successful private runs complete 37 generation publications and the
same four staged current-pupil publications, with no reported validation errors.
The existing publication duration starts at physical job creation; it is not
end-to-end geographic demand latency and excludes diagnostic map construction.
Full per-publication durations and CPU submission/completion distributions are
in the frame-pacing report. They must not be collapsed into one frame time.

| Existing job-clock publication duration, excluding startup | Mean | P95 | Peak |
|---|---:|---:|---:|
| Fresh timing control, 36 publications | 78.15536 | 130.94490 | 131.18660 |
| Vertex revision, 36 publications | 77.40525 | 129.92700 | 134.96830 |

The vertex revision's 1,000 logical submission CPU frames average 22.47898 ms,
P95 13.29930 ms, P99 406.48160 ms and peak 898.46720 ms. The targeted 44
submission rows average 45.28071 ms, versus 11.81287 ms at GPU completion. These
are instrumented private-host costs, not a production baseline measurement.
The distinction exposes work before submission rather than hiding it behind
the later fence-completion row.

Two explicit CPU blockers prevent calling this a surviving production candidate:

- The inherited synchronous diagnostic map builder still costs hundreds of
  milliseconds per topology replacement. Completion-row CPU times hide this;
  submission rows and whole-route host tails reveal it. Production map ownership
  and construction are not qualified.
- The new prototype scans the already-mapped native index buffer. Ten immutable
  scans cost **183.8632–451.4311 ms**. This proves the scan is at the wrong CPU
  access boundary for production even though it runs only once per topology.
  A future implementation should validate the immutable CPU input at upload,
  bind that proof to the resident copy, and avoid CPU reads back through mapped
  graphics storage. That alternative has not been implemented or timed here.

The control therefore does not trade a hidden CPU regression for a claimed GPU
success. CPU cost and publication risk remain explicit. Only one traversal path
and its two matched returns were measured; no cross-seed robustness is claimed.

## Cheap gate and implementation decision

| Requirement | Finding |
|---|---|
| Common six-frame responsibility | PASS: final incoming camera-list work plus required validation |
| Cost measured | PASS: adjacent GPU scopes and matched frame identities |
| Avoidable portion demonstrated | PASS as a private causal control; roughly one ms on six frames |
| Bounded ownership direction | PASS in concept: immutable index proof + prepared-vertex proof |
| Actual map/lifetime qualification | NOT QUALIFIED |
| Sufficient 8.33 ms margin across the regime | FAIL, including the zero-cost final-block bound |

**Overall cheap production gate: FAIL.** No production implementation or full
production validation campaign follows this gate. The original 11.11 ms residual
is removed within the private control, but that is no longer sufficient to close
M13. No additional optimization was layered onto the rejected composition.

## Map/lifetime qualification and negative controls

`resource-qualification-review.md` records creation, owner, read/write permission,
fence, invalidation, reuse, publication and destruction for current, incoming,
regional scratch, spare, resident topology, mapped storage and descriptor/control
resources. The real lifecycle was inspected. It is not a claim that the private
prototype already enforces a new capability contract.

The retained C++ model (`capability.hpp` / `capability_test.cpp`) passes **91 checks,
zero failures**, including full 64-bit generation, basis/domain identity,
allocation/content/binding/map epochs, address ranges, source lifetime, incomplete
destination and withheld-fence cases. Optional stale reuse information means
full canonical recomputation. An invalid actual destination means cancellation
and current-owner retention, not writing fallback H into invalid storage.

Those are **model tests**. Actual native resource identities do not yet feed and
consume these predicates. Live invalid mapped-address, high-generation-bit,
same-direction regional/support dataset revision, source overwrite, resize,
cancellation and destination-lifetime injections have not been qualified. No
freed Vulkan pointer was deliberately bound. A model pass cannot stand in for
those GPU tests. This is a shipping blocker preserved from the preceding review.

Prior D-mapped-oracle evidence retains 15,750,208 proposed exact copies with zero
full-word mismatches, and invalid-source fallback coverage. That is unchanged
prior evidence, not a fresh oracle for this vertex-validation revision. The new
run reports complete finite-position/normal validation, retained current owner,
zero owner gaps/overlap/stale draws and atomic fence publication. Exact final
physical/depth/HDR/image parity and same-direction asset-revision negatives were
not rerun after this cheap gate failed. No production parity claim is made.

## Narrow KSA comparison

Reverified the current local KSA DLL, `KSA.PlanetRenderer.cs` and frustum-culling
shader against the preceding reference manifest: version 2026.9.7.5402, revision
`487c3f340de24c6a81037120b6d1129c045c5400`; see `ksa-narrow-check.json`.

`KSA.PlanetRenderer.cs:1886–1940` prepares vertices, finalizes modifiers, generates
normals, applies resource barriers and performs viewport/frame culling.
`:2027–2039` consumes that frame's culling index buffer and indirect arguments.
This supports **ADAPT**: keep physical preparation and view-dependent selection
as distinct owned responsibilities, with culling attached to the consuming view.

The inspected KSA code does not establish an equivalent NovaCore incoming
atomic-publication validation bank or an exact physical-word reuse contract.
**INTENTIONALLY DIFFER** there: retain NovaCore's complete H, dependency identity,
current-owner and final-fence guarantees. No KSA performance or spike-avoidance
claim was inferred from source, and no broad KSA research was repeated.

## Independent review and validation boundary

- **VERIFY A:** resource/physical red team rejects production qualification.
  Strongest attacks are finite stale values despite valid vertices, incomplete
  native capability integration, missing explicit topology-proof publication
  join and counter23 telemetry aliasing. See the full state/negative-control
  matrix in `resource-qualification-review.md`.
- **VERIFY B:** frame-pacing red team rejects M13 closure. Exact workload joins
  survive, but nonfinal frames already exceed 8.33 ms; host map/scan costs and
  single-path evidence also prevent a production-ready claim.
- Independent slice reconstruction validates all 64 joined slice identities
  and disproves the largest-remainder-only explanation.
- Private native/managed Release builds and validator shader compilation PASS.
  Capability C++ test build with `/W4 /WX` and execution PASS.
- All successful private traversals report strict Vulkan error count zero.
- Debug/Release full suites, headless/window/native GPU tests, 15 launcher
  regressions, six route probes, warp and fresh fixed-pose performance checks
  were **not run**, because the required cheap production gate failed.

Historical fixed-pose evidence remains in `m13-exit-assessment`: orbital,
factor-1, Florida, grazing, active refinement and inland. It is not refreshed or
reclassified as passing the new 8.33 ms target. In particular, previously
reported Florida/inland costs already warn against a global closure claim.
The requested closure label above describes this scoped blocker; it does not
assert that every other regime has now been qualified against the new contract.

After restoration and guarded deletion of the private host, the normal deployed
Florida route completed **240 frames**, exit 0, no reported Vulkan errors,
physical-ready L17 terrain, zero missing/overlapping/stale owners and no crash.
All shader and required asset hashes still match the baseline. The normal
launcher exists and is hashed; no launcher behavior was modified. This smoke is
production preservation evidence, not acceptance of the private control.

## Evidence lifecycle and reproduction

Only scalar timing, exact input/resource identities, compact proof models,
diagnostic source/patches, hashes and reviews are retained. No new raw geometry,
depth, HDR or frame archive was captured. `journal-manifest.json` proves the
three journals are losslessly compressed. The original failure log is reduced
to its causal summary/hash. Previous sealed evidence is unchanged, verified in
`prior-evidence-verification.json`.

The reproducible control is `residual.py`; it adapts retained preceding source
templates and builds an isolated Release host, restoring normal source and
deployment afterward. `build-<label>` builds it; `timing-<label>` uses original
incoming work; `validation-triangles-<label>` uses triangle checks;
`validation-vertices-<label>` uses the bounded vertex revision. Use unique labels
and a new bounded diagnostic lifecycle if Project Control requests reproduction.
These are private proof commands, not a normal player launch workflow.

The route is Florida with `--scene=sol --focus=earth
--surface-site=florida-launch --physical-surface=m12d-natural-candidate
--solar-epoch=j2000`. The deterministic regional traversal stops after 1,000
logical frames. Canonical child-process Vulkan layer isolation and strict
validation are inherited from the retained harness.

`disposable-manifest.json` classifies 81 private-host files: 75,421,449 logical
bytes, of which 67,108,864 bytes are a hard-link alias of protected elevation
data. Only that alias is removed; 8,312,585 bytes are exclusive private output.
The guarded cleanup checks exact root, per-file size/hash, ignored status and
absence of reparse points before deleting individually. Scratch remaining: zero.
`storage.json` gives final retained, created/accounted and disposed totals;
allocated disk savings and cumulative write traffic are not inferred from
logical sizes. No Git/LFS, production cache, asset or unrelated cleanup occurred.

## Final repository state

`git diff --check`: PASS. Tracked diff and staged diff remain empty. No stage,
commit, push, merge, tag or milestone assignment occurred. Final status:

```text
?? docs/engineering-evidence/m13-exit-assessment/
?? docs/engineering-evidence/m13-regional-preparation-blocker/
?? docs/engineering-evidence/m13-regional-preparation-convergence/
?? docs/engineering-evidence/m13-residual-regional-frames/
```

Project Control receives the causal finding and reproducible private correction,
with its performance and correctness boundaries stated. No M13.5 title is
proposed. Further production validation ownership/map qualification, or work on
the remaining nonfinal 8.33 ms gap, requires the next bounded decision.
