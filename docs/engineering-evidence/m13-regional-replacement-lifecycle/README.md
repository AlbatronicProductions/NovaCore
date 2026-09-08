# Regional replacement scheduling and resource-lifecycle gauntlet

**Lead judgment: ESCALATE TO PROJECT CONTROL**

**Technical classification: NO SAFE M13.5 TARGET YET**

**M13 closure: M13 STILL NOT READY — ONE BOUNDED BLOCKER REMAINS**

**Dominant residual classification: NO SINGLE AVOIDABLE RESPONSIBILITY PROVEN.**

The remaining 42/64 over-budget observations are not all incremental replacement
cost. At the two current-L16 events, the same current terrain already costs
8.93–8.98 ms in the retained no-preparation observations. A fresh paired control
confirms the distinction: omitting incoming GPU work still leaves 26/64 frames
above 8.33 ms and a 9.02848 ms P95. Incoming work adds approximately 0.422 ms on
average, predominantly the necessary physical preparation measured in that
private control. It does not produce a measured systematic inflation of current
draw or current visibility work.

The expensive rational-map construction and mapped-index scans are absent from
banked M13.4. They belong to the rejected private prototype. Production already
retains topology, mappings and sufficiently large work buffers across these
events; the fresh 64-frame production trace contains **zero allocations/maps**.
Current cull/compaction runs every displayed frame, including frames with no
incoming replacement. No evidence shows it is rebuilt solely because incoming
work exists.

The cheap autonomous production gate therefore **FAILS**. No production candidate
is implemented, no final-block optimization is reopened, and no M13.5 name or tag
is assigned. The new data makes the accepted causal result more precise without
promoting the rejected composition.

## Baseline and preservation

- HEAD, main, origin/main and dereferenced
  `m13.4-zero-contribution-terrain-material-noise` remain
  `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
- Fresh branch: `codex/m13-regional-replacement-lifecycle`.
- `m13.3-prepared-physical-terrain` remains
  `180eaf150ba5db6364e17dd48336690778f058f9`.
- RX 6800 XT, native 3440×1440, Vulkan 1.4.
- Initial tracked/staged diffs were empty. All four preceding untracked evidence
  packages are preserved. Only this fifth evidence package is new.
- Normal deployment:
  `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0`.
- Launcher:
  `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.

`baseline.json` and `closeout.json` contain the complete identities. All 49
deployed shaders in each configuration (98 total), the R16 elevation asset and
both required global/Florida NCCUBE assets match the preceding baseline.
Release native SHA-256 remains
`74859dbb7854e92d3069e4081276eef98980945c2c9c4d4adf3a926b18cbaeca`;
Release managed SHA-256 remains
`86298b31bc05f7ab061f96567b75e798d7b6443f8437493f1254a49418502894`.

The normal deployed executable completed the existing regional Florida
departure/return driver for **1,000 frames**, exit 0, no reported Vulkan errors.
This is `deployed-route.json`: normal production binaries/shaders, canonical
child-process layer isolation, no private copy/map/validation flags. Its CPU
average frame time of 12.219 ms is not a per-event GPU measurement.

Detailed timing uses freshly built private instrumentation with unchanged
production shader semantics, plus a separately identified rejected-reference
control. Every build restores all temporarily instrumented source files and
normal deployment bytes in a `finally` block. `restoration.json` records the
final native/managed intermediate rebuild from restored banked source. No
renderer, terrain, TES, material, regional authority, facility, launcher or
production scheduling implementation changed.

## Measurements and their boundaries

Three complementary evidence sets answer different questions:

1. **Retained vertex revision:** the exact accepted causal evidence containing
   42/64 failures. `work-a.md/json` decomposes every frame and identifies a
   matching current-owner no-preparation observation for each event.
2. **Fresh bank-event:** deterministic departure/return using banked production
   preparation semantics with additional timing/identity instrumentation. It
   reproduces all 1,000 logical physical/input events and all 839 valid hardware
   TCS/TES observations against retained A. It supplies real production CPU,
   allocation, mapping, publication and GPU costs.
3. **Corrected reference/work pairs:** the prior rejected exact-copy/device-local
   vertex-validation control is left unchanged, and one diagnostic reference
   tick omits incoming GPU work before each normal work tick. Camera, current
   generation, physical buffers, incoming state and hardware counts are proven
   equal within the 64 relevant pairs. This isolates the incremental incoming
   responsibility; it is not an alternative production scheduler.

All percentiles are nearest rank. No tails are removed. Strict GPU correlation
requires `gpuFrame == geometryFrame == hostFrame - 1`. Stage scopes must not be
summed with their parents. CPU update contains fence/inspection/callback/upload;
GPU preparation sits inside the older broad cull/preparation scope. The terrain
candidate timestamp is a pipeline span, not an isolated fragment-shader time.
The numerical reviews preserve separate adjacent cull/compact/barrier scopes
and an explicitly arithmetic remainder rather than inventing missing timers.

Production material uploads are recorded before the historical total GPU
timestamp. The new transfer ledger records actual pending uploads, command
count and bytes. **All 1,000 logical production frames have zero pending image
uploads/transfer commands/bytes**, so that omission cannot explain this warm
replacement window. Regional residual publication uses host uploads before GPU
work, not an unmeasured device-local transfer command on these 64 frames.

The first reference experiment advanced the managed LOD coordinator on both
ticks and therefore changed replacement history. Its journal is retained as a
failed diagnostic design, not a matched 64-frame performance result. The
corrected harness invokes that coordinator once per logical frame, freezing its
prepared submission on the repeat tick. All six original event ranges return.
The corrected experiment has known differences on some movement/focus frames
outside the 64 target pairs; global two-tick equivalence is not claimed.

Reference-first ordering can warm clocks/caches before the work tick, especially
after private synchronous map stalls. No GPU-clock cause is asserted. This
limitation bounds the interaction claim and prevents treating the paired
control's longer publication duration as production latency.

## Replacement timeline and 64-frame decomposition

Event identity joins generation, source/destination topology, pupil/basis,
camera, physical buffers and the preparation range. R-0 is each incoming job's
first recorded preparation slice, not an absolute runtime frame number.

| Incoming generation | Current→incoming LOD | Logical range | Event-relative slices | Current→incoming pupil | First draw of newly published generation |
|---:|---|---|---|---|---:|
| 17 | L14→L15 | 334–343 | R-0…R-9 | 25→26 | 344 |
| 18 | L15→L16 | 345–355 | R-0…R-10 | 26→27 | 356 |
| 19 | L16→L17 | 357–367 | R-0…R-10 | 27→28 | 368 |
| 35 | L14→L15 | 714–723 | R-0…R-9 | 45→46 | 724 |
| 36 | L15→L16 | 725–735 | R-0…R-10 | 46→47 | 736 |
| 37 | L16→L17 | 737–747 | R-0…R-10 | 47→48 | 748 |

The event runs through: retained-topology acquisition → incoming work-capacity
acquisition → descriptor binding → complete geographic demand/readiness → one
contiguous 65,536-vertex-or-smaller slice each work frame → final incoming
readiness → submitted final fence → next-frame host inspection/atomic swap →
current cull/compact before the first draw. Source current remains physically
unchanged during the incoming job. The two returns have distinct snap/basis
identities 11 and 13; those histories are not collapsed.

| Retained 42 failures, grouped by equivalent current render state | Frames | Above 8.33 | Exact no-preparation current total | Explanation |
|---|---:|---:|---|---|
| Current L14, incoming L15 | 20 | 5 | 7.38188 / 7.41544 ms | Later physical recomputation and an isolated first-slice outlier add cost to an otherwise sub-budget current draw |
| Current L15, incoming L16 | 22 | 15 | 8.09944 / 8.34104 ms | Current rendering is already at the limit; small copy slices cross it and recompute-heavy slices create larger tails |
| Current L16, incoming L17 | 22 | 22 | 8.93132 / 8.98280 ms | The matched current-only frame is already over budget before incoming preparation |

Those retained references are one observation per event. The fresh 64 paired
references strengthen the separation without pretending that a single old
observation was a steady-state distribution.

`work-a.json` contains all 64 original rows: event/R index, total, exact distance
above 8.33, terrain span, preparation, current/incoming cull, compact, barriers,
copy/transfer semantics, publication state and matched control. `verify-b.json`
adds all fresh production and paired measurements with full identities. Reuse
is compute-shader read/write traffic inside physical preparation; no separate
copy-engine timestamp is fabricated. Prior exact oracle counts remain linked
to their matched source events rather than regenerated through raw capture.

## Production CPU, resources and publication duration

The fresh production 64-frame window shows:

- **0** buffer allocation/mapping calls.
- Six retained-topology acquisitions, peak **0.0003 ms**.
- Six incoming-work acquisitions, peak **0.0002 ms**.
- Descriptor updates, peak **0.0308 ms**.
- Regional update mean **0.01695 ms**.
- Shared `PrepareProductionUploads` mean **2.01650 ms**, P95 **2.2624 ms**,
  peak **2.3970 ms**, despite zero warm image transfers.
- Publication inspection for the six jobs: **0.0584–0.0788 ms**.
- Rational copy-map builds and mapped-index validator scans: **zero**.

`PrepareProductionUploads` includes existing request/completion orchestration;
its measured CPU time is not GPU transfer time. It is current-frame shared work,
not a discovered allocation loop induced by incoming replacement. Removing it
would not by itself clear the measured GPU floor. No material/resource cleanup
or optimization of it was opened.

| Generation | Preparation frames | Native candidate creation→publication inspection end |
|---:|---:|---:|
| 17 | 10 | 162.006 ms |
| 18 | 11 | 191.583 ms |
| 19 | 11 | 198.274 ms |
| 35 | 10 | 163.682 ms |
| 36 | 11 | 189.997 ms |
| 37 | 11 | 200.194 ms |

These are fresh banked-semantic durations, not the earlier private control's
77.41 ms average. For example, generation 17 spends 38.07228 ms in GPU physical
preparation over ten frames and 2.43576 ms in final incoming work; generation 18
spends 42.72468 ms over eleven frames and 2.63184 ms in final incoming work.
Current rendering continues throughout. Host frame/fence totals overlap that GPU
work and cannot be added to it as separate elapsed time.

The geographic footprint is already resident for all six jobs. There is no
missing slice between first and last preparation, no per-event remapping, and
no measured large publication-ready host queue. Approximately two milliseconds
between candidate creation and regional update belongs mainly to existing
shared host orchestration. After the completed-fence inspection checkpoint,
publication takes substantially less than a millisecond. Actual GPU completion
to host wake-up delay has no calibrated cross-clock timestamp here and remains
explicitly unmeasured.

The earlier 77.41–134.97 ms private job clock starts after map construction and
startup. It must not be labeled end-to-end demand latency. In the paired
diagnostic, deliberately inserted reference ticks lengthen job wall time and
map work occurs before reference rendering. Those times diagnose ownership,
not a production latency improvement or regression.

`verify-a.json/md` supplies every event's candidate creation, first regional
update, first/final slice, publication marker, fence checkpoint, CPU components,
GPU-active preparation, final readiness cost and stated timing gaps.

## Native lifetime model and current-owner independence

`work-b.md/json` is the complete inspected production lifecycle model, expanded
by fresh resource observations in `verify-a.json`. It covers creation, memory
type, mapped state, ownership, generation, read/write permissions, fence,
invalidation, reuse, publication and destruction for each relevant resource.

| Resource | Lifetime/owner | Read/write and publication boundary |
|---|---|---|
| Native topology/lattice/index | Retained immutable topology resource, reused for matching topology/count/family | Native owns copied storage after managed lease upload; current/incoming may share this immutable resource; teardown releases it |
| Mapped native index representation | Same lifetime as its host-visible coherent index allocation | It is not mapped/scanned afresh for each production replacement; private validator readback costs are a separate experiment |
| Current prepared physical/work buffers | Published native generation | GPU-read authority remains current until a completed replacement atomically takes over |
| Incoming prepared/work buffers | Fresh or sufficiently large spare capacity | GPU writes contiguous slices only after complete dependencies; never aliases current physical authority during those writes |
| Visibility/compaction/indirect/counters | Work-set lifetime, separate current/incoming sets | Current contents are view-dependent and rebuilt for drawing; incoming contents/readiness stay private until publication |
| Regional scratch | Retained staged-current refresh capacity | Writes are separate from published current; final fence swaps complete storage; incoming topology replacement cancels unpublished current refresh |
| Spare work set | Former current retained after safe publication | Can be reassigned/overwritten as destination; capacity identity is not a proof that old physical contents remain reusable |
| Material staging/resident textures | Retained bounded resources | Host prepares actual pending uploads; frame records transfers only if pending; completion controls exposure |
| Regional catalog/payload | Verified immutable dataset and resident record storage | All contributing records must be ready before physical work; residency growth does not change physical dataset identity |
| Descriptor/frame/control resources | Renderer/submission lifetime, rebuilt on safe resize | Resource pointers updated at binding/publication transitions; command binds do not allocate or remap the resources |

Banked generic buffers use host-visible coherent memory selected from compatible
memory types and remain mapped. The prior private physical-placement control
uses compatible device-local + host-visible coherent storage. Actual allocation
types are in `Prep heap` evidence. There is no new transfer engine or staging
copy on each incoming slice.

Physical ownership is independent; scheduling is not fully independent. Current
and incoming work use the same graphics command buffer and the same submitted
frame fence. Global compute barriers can order later work; the next host frame
waits for the whole submission. This is a real dependency, not proof of a
recoverable stall. Current does not change buffers halfway through incoming
preparation, and its cull/compact is not conditioned on the presence of incoming
work. The paired control measures the interaction instead of assigning cost from
queue topology alone.

The preceding 91-check capability model remains useful for a future exact-copy
implementation. It does not qualify actual native token integration, full
64-bit generation handling, source-content epochs, invalid mapped addresses or
same-direction regional/facility dependency revisions. No new production reuse
path is implemented, so those missing qualifications are not hidden behind the
existing correct production ownership model. Invalid optional reuse must lead
to complete canonical recomputation; invalid actual destination storage must
cancel and retain current. It must not receive a fallback write.

## Repeated-work frequency and ownership table

Costs below distinguish fresh production measurements from retained private
controls. Unknown subcosts remain unknown. A proposed invariant boundary is not
an implementation authorization.

| Responsibility | Current NovaCore frequency | Required/authoritative boundary | KSA frequency | Measured cost and candidate migration |
|---|---|---|---|---|
| Topology validation | Managed immutable topology checks; native triangle checks during current/final incoming cull | Structural proof can belong to immutable topology; changed physical values need their own proof | Static topology/adjacency generated and retained at library load | Subcost not isolated from cull. Prior final-block ownership direction retained; not reopened |
| Exact source-map construction | Absent banked; private rational hash map once per replacement | Topology-pair address relation may persist; per-transaction physical authorization cannot | No equivalent exact physical-copy map found | Hundreds of ms privately, zero banked event cost. Not a current production saving |
| Mapped-index scan | Absent banked; private validator once per distinct resident topology | Immutable CPU input/native copy qualification, bound to the correct resident resource | Static topology persists; no recurring equivalent CPU scan found | Prior private ten scans 183.86–451.43 ms each; zero banked event cost |
| Visibility construction | Current each rendered frame; incoming final slice | Current camera plus physical geometry/validation inputs; topology alone is insufficient | Each applicable frame, directly emits visible indices | Private current whole visibility block roughly 0.99–1.16 ms; removing it entirely still fails tails |
| Compaction | Current every frame; incoming final slice | Same view-list output responsibility | Fused into culling by directly appending survivor indices | Roughly 0.32–0.35 ms privately. KSA fusion is a precedent, not proof of enough saving |
| Full physical preparation | Changed incoming/staged pupil, bounded contiguous slices | Changed canonical physical domain, complete dependencies and publication job | Full prepare/modifiers/finalize/normals each applicable rendered frame | Fresh production selected jobs about 38–43 ms GPU active across 10–11 frames; not excessive frequency proved by KSA |
| Reusable physical copy | Private exact-copy control per incoming slice; no banked copy-map path | Only exact authorized values from a live immutable source | No equivalent exact 64-byte prepared copy found | Inside preparation scope, not a separate transfer; lifetime qualification incomplete |
| Buffer allocation | Capacity creation/growth; topology and work sets retained | Resource/capacity lifetime | Persistent frame-slot maximum-capacity buffers | Zero allocations over the fresh production 64 frames; no event allocator winner |
| Buffer mapping | Persistent host-visible resource lifetime | Resource lifetime; separate host controls from GPU-only work where appropriate | Persistent control/staging maps; device-local generated outputs | Zero mappings over the fresh production 64 frames |
| Descriptor/resource binding | Resource updates on creation/publication/resize; command binds at use | Actual resource/view/range change and consumer command | Persistent sets but per-frame mesh/cull descriptor updates | Fresh descriptor updates peak 0.0308 ms; not enough GPU payoff |
| Publication readiness | Each complete replacement, final fence then atomic swap | Complete domain, valid geometry/resource lifetime and final consumer fence | Frame geometry after same-command-buffer barriers; texture completion before view exposure | Fresh selected publication inspection 0.0584–0.0788 ms; no large avoidable late queue shown |

## KSA architectural reference

Live local KSA remains version **2026.9.7.5402**, revision
`487c3f340de24c6a81037120b6d1129c045c5400`, DLL SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
Thirteen reference fingerprints are recorded in `work-c.json`, with current vs
cached-decompilation provenance distinguished.

- **ADOPT:** persistent immutable topology and resource capacity, distinct from
  camera-dependent list contents. NovaCore already implements substantial parts
  of this boundary; mere architectural similarity proves no new saving.
- **ADAPT:** KSA's cull shader directly appends surviving triangle indices rather
  than running a separate compact pass. A valid production change still needs
  NovaCore validation/ordering guarantees and sufficient measured payoff.
- **ADAPT:** completion-before-exposure and safe retirement. KSA texture
  streaming uses worker/upload completion polling and delayed disposal; that is
  not proof that terrain geometry preparation runs on an asynchronous queue.
- **INTENTIONALLY DIFFER:** NovaCore requires complete canonical regional H,
  exact prepared-value reuse and atomic physical-generation publication. KSA's
  valid coarse texture fallback is not an equivalent permission to publish
  incomplete authoritative terrain.

KSA's `GenerateMeshData` runs on applicable rendered frames, followed by culling
and rendering in the acquired frame's graphics command buffer. Its asynchronous
texture streamer's 32 MB batch/96 MB in-flight targets and frame-slot resources
do not establish a terrain GPU-time budget, bounded physical replacement delay
or independent terrain-preparation queue. No such performance claim is inferred.
The source proves ownership/frequency, not spike-free behavior on this GPU.

## Paired performance result and payoff gate

| Evidence window | Mean GPU | P95 | P99 / peak | >8.33 ms | >11.11 ms |
|---|---:|---:|---:|---:|---:|
| Retained private vertex revision, 64 | 8.73007 | 10.66576 | 10.98624 | 42 | 0 |
| Fresh banked-semantics event, 64 | 14.27040 | 15.62185 | 16.95828 | 64 | 64 |
| Corrected paired reference, incoming omitted, 64 | 8.25487 | 9.02848 | 9.79276 | 26 | 0 |
| Corrected paired work, incoming enabled, 64 | 8.67662 | 10.62408 | 10.92796 | 39 | 0 |

The paired rows use the rejected reference mechanics, not banked M13.4. Comparing
their level to production is not a claimed improvement from the new timing
instrumentation. Cross-run clocks/cache/history vary even with exact geometric
inputs. Within all 64 target pairs, clipping-output and fragment counters differ
despite matching recorded inputs and TCS/TES counts. Their cause is unclassified;
neither fragment-work equality nor physical/depth/HDR/image parity is proved.
This further limits the control's ability to isolate incoming-work contention.

Within the 64 matched pairs:

- Work minus reference total: **+0.421754 ms mean**.
- Incoming physical preparation plus final block: **0.448556 ms mean**.
- Current terrain span change: **−0.022042 ms mean**.
- Current visibility block change: **−0.002556 ms mean**.

These measurements do not support a systematic one-millisecond contention tax
on current rendering. Small residual differences and reference-first bias do
not prove absence of every possible hardware interaction. They do reject the
claim that a common large interaction has been demonstrated and can safely fund
an asynchronous scheduling implementation.

| Candidate responsibility | Affected frames/cost | Optimistic payoff | CPU/latency consequence | Gate |
|---|---|---|---|---|
| Omit/fully overlap incoming GPU work | Up to all incoming slices; paired mean increment 0.422 ms | Measured no-incoming reference still P95 9.028 ms, 26/64 over budget | Omitting work is not a valid production correction; overlap needs independent proof | FAIL sufficient margin |
| Remove all current visibility work | Every visible frame, roughly 1 ms in the private control | Retained zero-cost subtraction: mean 7.6482 ms but P95 9.4423/peak 9.9138, 11 failures | Would remove required view/validation output unless correctly migrated | FAIL even optimistic tail bound |
| Fuse cull/index emission using KSA precedent | Separate compaction, about 0.32–0.35 ms | Less saving than the failed whole-visibility bound | Correct output ordering/validation must survive; no queue or CPU benefit proved | FAIL closure payoff |
| Persist diagnostic rational maps or change mapped-index scan boundary | Private host stalls only | Zero measured banked GPU saving | Could remove substantial prototype CPU stalls; not a production M13.5 payoff | FAIL current responsibility |
| Extend native allocation/mapping lifetimes | Capacity/resource acquisition | Zero calls in the relevant fresh window; existing persistence already works | No measured event CPU saving to recover | FAIL avoidable-cost proof |
| Move final host publication | Six final completed jobs; 0.0584–0.0788 ms inspection | No measured GPU reduction; complete-fence checkpoint is already promptly consumed | Earlier exposure would weaken readiness | FAIL measured cost/payoff |

The subtraction bounds are deliberately overoptimistic, not proposed zero-cost
implementations or predicted measured percentiles. No single proven wrong
ownership/frequency explains enough of the 42-frame excess to reach 8.33 ms
with margin. Combining current-visibility changes with preparation scheduling
or another independent renderer improvement would violate the one-winner gate.

**Autonomous implementation gate: FAIL.** Old/new production owner/frequency,
test migrations and resource-lifetime changes are therefore not applicable.
No production tests are retired or altered.

## Verification and remaining proof

**VERIFY A — resource/publication red team:** all 64 corrected pairs match
current/incoming generations, topology, full relevant frame inputs, physical
buffer/memory identities and hardware TCS/TES. Reference ticks do not advance
incoming preparation or record incoming work. Current and destination physical
allocations are distinct. A reference tick can legitimately publish the prior
work tick after its fence, so references are not falsely described as incapable
of publication. No existing owner gap, overlap, stale draw or Vulkan error is
reported. Strongest future-candidate blocker: actual reuse capability/lifetime
integration and negative controls remain unqualified.

**VERIFY B — performance/scheduling red team:** the fresh production input join
passes; corrected target pairs pass; the first diagnostic's history and the
corrected global non-target differences are explicitly rejected as equivalence
proof. Reference-first order and CPU-map clock/cache effects limit causal claims.
The no-incoming P95 already fails, so no shipping/closure recommendation survives.

`verify-a.md/json` and `verify-b.md/json` retain the independent detailed checks.
There is no fresh full physical-word/depth/HDR/image parity proof for the rejected
reference mechanics; prior exact-copy evidence is not relabeled as production
qualification. No new renderer source correction requires manual Florida
acceptance in this ticket.

Fresh private Release native/managed builds and diagnostic shader compilation
pass. Normal source/intermediates/deployment were restored. Full Debug/Release,
headless/GPU/window/native GPU suites, launcher regressions, six-route campaign,
L0–L17, warp and fixed-regime recapture are **not run** because the cheap gate
fails. Historical orbital, factor-1, Florida, grazing, active and inland evidence
is retained; it is not asserted to satisfy the newer 8.33 ms contract.

The final normal deployed Florida smoke passes **240 frames**, exit 0, with
physical-ready L17, zero missing/overlapping/stale owners and no reported Vulkan
errors or crash. All normal shaders/assets remain unchanged. This validates
preservation after cleanup, not a private candidate's acceptance.

## Evidence lifecycle and reproduction

Permanent budget: **12 MiB**. Retain event timelines, scalar timing/resource
identities, ownership/frequency models, source/reproduction patches, independent
reviews, hashes and judgment. No raw geometry, depth, HDR, image sequence or
performance video was generated.

`lifecycle.py` is the private reproducer. From the repository root, use Python
with `-B`. `deployed-route` runs the normal deployment. `build-bank` then a fresh
`bank-...` label builds/runs production-semantics instrumentation. A
`build-reference-...` build followed by a fresh `reference-paired-...` label runs
the corrected two-tick causal reference. It reuses preceding sealed source
templates and the rejected reference shaders, restoring production bytes after
building and private shaders after each run. Use a new bounded lifecycle and
unique labels for any future reproduction; existing evidence is not overwritten.

The route is `--scene=sol --focus=earth --surface-site=florida-launch
--physical-surface=m12d-natural-candidate --solar-epoch=j2000`. The private
comparison starts from complete residency, pauses the epoch and excludes
external input. Its 1,000 logical route ticks include the same two Florida
returns; reference/work mode deliberately adds one display tick per logical
tick. The actual deployed route uses the normal existing regional driver.

Private output was hash-inventoried and individually removed: **77 files,
73,992,678 logical bytes**, including a **67,108,864-byte hard-link alias** of
protected elevation data. Exclusive private output was **6,883,814 bytes**.
Only the alias was removed; the production asset is preserved. No broad delete,
cache policy, Git/LFS action or unrelated cleanup occurred.

`journal-manifest.json` verifies lossless compression of the three timed
journals. `storage.json` gives created/accounted, retained, disposed and remaining
bytes. Those are distinct file-instance logical/exclusive sizes, not invented
NTFS allocated savings or cumulative I/O. Disposable scratch remaining is zero.
All preceding sealed evidence manifests are verified unchanged.

## Final repository status and decision

`git diff --check`: **PASS**. Tracked and staged diffs remain empty. Only the new
evidence package is added; no stage, commit, push, merge, tag or banking action
occurred.

```text
?? docs/engineering-evidence/m13-exit-assessment/
?? docs/engineering-evidence/m13-regional-preparation-blocker/
?? docs/engineering-evidence/m13-regional-preparation-convergence/
?? docs/engineering-evidence/m13-regional-replacement-lifecycle/
?? docs/engineering-evidence/m13-residual-regional-frames/
```

Return the causal result to Project Control. There is no proposed M13.5 title.
The scoped regional pacing blocker remains; this label is not a claim that all
other supported regimes have passed the revised 8.33 ms contract. The next
decision must account for the measured current-render floor as well as incoming
preparation, rather than presuming a hidden allocation or publication stall.
