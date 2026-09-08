# Final M13 blocker gauntlet: regional physical preparation

**Lead judgment: ESCALATE TO PROJECT CONTROL**
**Technical classification: NO SAFE M13.5 TARGET YET**
**M13 closure: M13 STILL NOT READY — ONE BOUNDED BLOCKER REMAINS**

There is real avoidable work, but no single qualified production winner. Exact reuse removes repeated height/normal evaluation for eligible values. Device-local placement of prepared physical buffers removes a separate downstream cost. Both private controls improve performance; neither satisfies the complete transition contract. Their combination was neither implemented nor claimed to be one correction. Production source, shaders, deployment, physical assets, main and milestone tags remain unchanged. No M13.5 is assigned.

## Baseline and method

- HEAD, main, origin/main and peeled `m13.4-zero-contribution-terrain-material-noise`: `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
- M13.3 `m13.3-prepared-physical-terrain`: `180eaf150ba5db6364e17dd48336690778f058f9`.
- Fresh branch: `codex/m13-regional-preparation-blocker`.
- Starting tracked and staged diffs: empty. Prior exit evidence was already untracked; this ticket added only its own evidence directory.
- RX 6800 XT, native 3440×1440, Release private host, process-local Khronos validation and empty implicit-layer discovery.
- Normal Debug/Release native, managed and executable identities; all 49 deployed shaders per configuration; R16/global/regional assets are recorded in `baseline.json` and rechecked at closeout.
- Normal Release native SHA-256: `74859dbb7854e92d3069e4081276eef98980945c2c9c4d4adf3a926b18cbaeca`.
- Normal Debug native SHA-256: `eabc5e1919b91879b32f5e756f836de55aa4ca0c08788b6ad32e4764f9d874c8`.

The common route was:

```text
--scene=sol --focus=earth --surface-site=florida-launch
--physical-surface=m12d-natural-candidate --solar-epoch=j2000
--benchmark-frames=1000 --log=startup,validation,vulkan
NOVACORE_EARTH_ROUTE_VALIDATION=regional
```

The private host adds aligned frame/submission timing and bounded aggregate buffer oracles. The CPU identity run is **not a performance run**: its temporary hash tables cost approximately 1–2 seconds per inspected publication. No raw geometry, depth, HDR or frame archive was generated. Only root edited, built and ran; three workers inspected distinct responsibilities, with two independent final challenges.

`profile.json` is the fresh no-oracle baseline. `profile-repeat.json` brackets the controls with restored original shaders and also logs the existing preparation-job timer. Each selected band has 11 samples; nearest-rank P95 and P99 therefore equal its maximum. The 44-frame combined table gives more useful tail statistics. These are bounded samples, not population estimates.

**Workload-equivalence limit:** identical route/job windows and publication counts do not imply identical physical trajectories. Pupil histories differed between runs. At host frame748, the oracle/copy/placement runs reported pupils65/60/57. The strongest exact-value proof is the **within-run GPU recomputation oracle**, not cross-run image or geometry parity. This limit independently prevents production qualification.

## WHAT / BAR

The remaining blocker is incoming regional physical preparation overlapping ordinary terrain rendering during replacement. The selected incoming generations18/19/36/37 occupy GPU frames345–355,357–367,725–735,737–747 and publish on host frames356/368/736/748. Each has ten 65,536-vertex slices plus a remainder.

The bar remains ≤11.11 ms total GPU sustained, preferably ≤8.33 ms where practical, with complete regional dependencies, exact generation-4 physical authority, facility support, retained current owner, final fence/readiness and atomic publication. No resolution, refinement, TES, material or physical-quality reduction is admissible.

Fresh baseline bands average14.026–15.429 ms, with a17.785 ms peak. The repeat averages13.978–15.286 ms, peaking18.687 ms. This is consistent with the previously established blocker, but the new numbers must not be substituted into old milestone measurements.

## WORK A — causal decomposition

`RecordRegionalPreparation` brackets exactly one compute dispatch with GPU timestamps. CPU acquisition, publication inspection and the subsequent barrier are outside that interval. Both incoming preparation and current rendering use the same graphics queue/command buffer; this is additional displayed-frame work, not asynchronous free capacity.

| Responsibility | Work / logical bytes | Measured cost | Required / avoidability |
|---|---|---|---|
| Incoming dispatch | 1 dispatch; 64×1×1 local size; 1,024 workgroups for a full slice | Selected baseline prep means3.747–3.858 ms/slice | Work for genuinely new values required; full recomputation for exact old values avoidable |
| Incoming lattice/frame transform | 65,536 vertices;1,048,576 B lattice footprint; shared160 B frame | Included in dispatch and reuse control | Destination direction required; source direction needed for exact eligibility |
| Exact check + full-record copy | Up to4 MiB source reads +4 MiB destination writes per full slice, plus source/destination lattice reads | Eligible24-slice subset mean0.237 ms | Real replacement overhead; not a pure bandwidth or isolated store measurement |
| Height recomputation + copied normal | Same admitted key; unchanged full height path, support and geography | Eligible first-eight-slice controls approximately0.83 ms | Height alone much smaller than normal bundle; these are diagnostic specializations |
| Normal recomputation + copied body/height | Same key; original full physical-normal path | Approximately3.03 ms | Dominant repeated bundle in this control |
| Geographic inputs | Center height plus four base-normal height samples; bilinear global/regional reads where applicable | Fused inside height/normal controls; no independent GPU ms | Catalog selection, geographic inversion and physical record values remain required for new samples |
| Procedural full relief | Macro/meso/base/near terms and normal gradients | Included above | Remains upstream of TES; no physical approximation permitted |
| Facility/support | Existing center/normal support evaluation;128×112 m core,384×368 m total apron | Not independently timed | Retained; no claim of zero cost or measured support-only saving |
| Output generation | 64 B per vertex;4,194,304 B/full slice | Included above | Every destination must become complete; no omission of new values |
| Dependency demand | Separate demand dispatches/readback;22 dispatches in these routes | Separately retained in each record | No copying-based bypass; complete footprint required before preparation/publication |
| Asset acquisition/upload | 670 immutable records;163,158,696 B read;93,392,640 B uploaded | Worker read/decode/verify totals and dependency latency retained | Same counts/bytes for baseline, copy and placement; not the recurring warm-band cost |
| Barrier | One write→compute/vertex/host-read barrier after each slice | Outside preparation timestamp; no standalone duration | No measured redundant-barrier opportunity |
| Final cull/compact/publication work | Final slice additionally arms incoming reset/cull/compact | Combined cull/prep minus incoming prep increases by2.574–3.129 ms versus preceding slices in original baseline | Associated residual, **not** an independently timed cull duration |
| Fence/publication | Existing final frame fence, full readiness, atomic swap | New scalar in placement/repeat records | Correctness boundary retained; CPU/GPU time cannot be removed by premature publication |

The height/normal controls preserve the existing physical functions and copy only under the same strict key. They reveal the cost distribution of repeated work. They are not additive timer partitions: compiler scheduling, shared calculations, copies and divergent paths differ. Geographic addressing, support and arithmetic are not assigned invented standalone timings.

### Compiler and memory evidence

Actual `VK_AMD_shader_info` results, not estimates:

| Field | Current prepare | Incoming prepare |
|---|---:|---:|
| VGPR |185|178|
| SGPR |75|48|
| Workgroup |64×1×1|64×1×1|
| Wave size from ISA |32|32|
| Scratch / LDS |0 /0 B|0 /0 B|
| Static instructions excluding end padding |76,763|76,506|
| FP64 non-conversion instructions |29,952|29,799|
| FP32 non-conversion instructions |984|984|
| Numeric conversions |1,698|1,697|

Incoming static memory instructions:34 descriptor loads,94 scalar buffer loads,137 vector buffer loads,6 vector buffer stores; no scratch/flat/LDS operations. The driver inlines the shader. Its ordinary-output prefix contains50,057 static instructions; another26,449 implement optional catalog probes and exit paths. **These are not executed instruction counts per vertex.** Actual occupancy and dynamic FP64/memory utilization were not measured. Zero scratch/LDS excludes a claimed spill/shared-memory mechanism; it does not prove FP64 throughput saturation.

Measured original allocation: type1, flags6 (`HOST_VISIBLE|HOST_COHERENT`), heap0 flags0,16,703,094,784 B system-memory heap. This includes prepared physical buffers, lattice, elevation oracle and regional residual payload. The placement-only control changed **only** incoming/current/spare physical storage through their normal swaps, plus regional pupil staging. It selected type2, flags7 (also `DEVICE_LOCAL`), heap1 flags3,17,163,091,968 B. All mappings and shader code remained present. No generic allocation policy, oracle, lattice, regional payload, counters or shader was changed.

Three physical allocations observed in that control total135,719,808 B; the conservative three-max-buffer bound is136,724,352 B. Their size did not grow; their heap changed. Region payload remained119,737,728 B with its existing124,895,232 B maximum. Portability, memory-budget admission and fallback behavior were not implemented.

**Classification:** repeated-data computation and placement-sensitive downstream GPU consumption are both measured. Arithmetic/memory cost is mixed; “FP64-bound” or “bandwidth-bound preparation” is not established. Placement barely changes the preparation dispatch itself while improving the whole band, so its gain must not be relabeled saved H evaluation.

## WORK B — identity, residency and publication

| Incoming generation | Total values | Exact full64 value exists in published set | Same-index exact | Different-index exact | No exact value in current set |
|---|---:|---:|---:|---:|---:|
|18|696,410|647,002|586,682|60,320|49,408|
|19|712,106|696,410|643,258|53,152|15,696|
|36|696,410|647,002|586,682|60,320|49,408|
|37|712,106|696,410|643,258|53,152|15,696|
|Total|2,817,032|2,686,824|2,459,880|226,944|130,208|

The CPU oracle's full64 lookup finds a bodyXYZ/H-key match anywhere in current storage and then compares all64 bytes. **Its rational-map comparison is separate and compares H+normal only.** Matching aggregate counts do not prove every rational map selects the corresponding full64 source. No complete mapped GPU reuse path was tested.

Independent same-index GPU oracle: **13,726,148 proposed copies across37 publications; zero mismatches across all16 words**. It computes the original incoming result, compares the proposed current source, and clears diagnostic markers after the final fence before publication. Original physical output is what gets published in that proof.

Basis-changing controls generations3/15/20 contain1,891,598 incoming values with zero exact current-set matches and zero admitted same-index reuse. Topology correspondence alone is therefore insufficient. These controls do not prove every individual height scalar changed.

Classification:

- Unchanged / same index: directly supported by exact direction and full-word oracle in tested states.
- Unchanged / different index:226,944 exact values exist elsewhere; production map routing still unproved.
- New relative to current set:130,208 values in the four slow jobs; they require preparation or another separately proven source.
- Regional-dependent, support-dependent, H-only and normal-only changes: **unknown**, not zero. No component counterfactual separated those causes.
- Per-query local UV and four normal-sample record identities were not captured. Ordered dataset identity and readiness were verified at the existing native authority boundary.

The safe key requires the actual published lattice/frame and buffer, exact direction bits, exact radius, valid count, active regional incoming job and immutable physical domain. Zero direction components are conservatively excluded because existing current preparation uses numeric equality, which equates signed zeros. Catalog probe mode disables copying to preserve reserved payloads. Reuse never bypasses incoming dependency discovery/readiness.

Current preparation is cancelled when an incoming topology transaction supersedes it. The published current buffer remains unchanged until the incoming final fence/readiness succeeds; the spare is only an allocation cache and is not a valid physical-data cache. Regional records are selected canonically independent of residency and are immutable for the renderer lifetime. Additional unrelated record loads cannot change an already complete normal footprint. Changed pack/generation identity requires a fresh renderer domain.

The11-frame band follows `ceil(vertexCount/65536)`, then next-frame publication. The slice budget predates M13.3/M13.4; it bounded a historically much larger dispatch. Smaller slices alone were not tried because they do not retire work and may extend latency. Final cull/compact remains required.

### Full-map option remains unproved

Existing managed parent maps total21,263,080 B. Lazy inverse maps could add up to24,056,200 B, plus one active GPU map ≤2,848,424 B. The background preparation task is the appropriate construction owner; never put the CPU oracle's hash maps on the display thread. A production map requires a versioned ABI/binding and source topology/generation validation; there is no existing native map pointer to repurpose.

An illustrative uniform3.5–3.9 ms/full-slice rate gives only0.26–0.33 ms **band-average** additional work equivalent from the53–60k remapped values, although the tail concentration could matter more. This is not measured performance or a strict bound. No ABI or topology format was changed to pursue it after the single-winner gate failed.

## WORK C — current KSA relationship

The worker revalidated all27 files in the existing narrow KSA provenance manifest against current local files. Installed version:2026.9.7.5402, revision `487c3f340de24c6a81037120b6d1129c045c5400`. KSA DLL SHA-256: `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`. No proprietary code/assets were copied into NovaCore.

Local reference coordinates under `E:\NovaCore\build\ksa-residency-reference\source\`:

- `KSA.PlanetRenderer.cs:1770,1886–1947`: nearby billboard preparation runs per displayed frame, with prepare/modifier/finalize then normal/cull barriers. No equivalent exact current→incoming physical cache was found.
- `KSA.PlanetRenderer.cs:1499–1540`: persistent per-flight position, normal, color, height/gradient and cull working buffers explicitly request device-local memory. This is requested policy, not a measured KSA heap on this machine.
- Mesh collection keeps topology/adjacency and maximum capacities; tangent snapping rebases transforms, not an evaluated physical cache.
- `KSA.CelestialTextureStreamer.cs:819–849` publishes completed views/versions; current views remain until promotion; retired resources use frame/fence lifetime. This is not proof that KSA retains a whole prior physical generation.
- Streaming uploader completion and bounded admission control publication; its32 MB batch/96 MB in-flight thresholds admit whole requests and are not strict byte caps.
- `KSA.TerrainPatch.cs:103–169,262–377` reuses rolling collision-patch rows and computes exposed rows. That supports geographic reuse as a principle, not NovaCore render-buffer identity.

The retained authenticated history references are reused from `ksa-terrain-architecture/history.md`: June2025 described per-frame prepass and snap-only work as a possible future direction; July/August2026 records concern completed-view promotion, invalidation and per-flight/snap fixes. No new broad history audit was needed.

**ADOPT:** persistent topology/working allocations and completion/lifetime discipline.
**ADAPT:** GPU-local working-data placement, exact-contribution awareness, and geographic reuse where NovaCore's stronger key is proved.
**INTENTIONALLY DIFFER:** NovaCore retains full generation-4 relief, complete residual dependencies, exact direction authority and atomic old-owner retention; KSA's per-frame reevaluation/coarse texture fallback does not authorize weakened physical publication.

## PAYOFF / candidate ranking

| Candidate | Measured avoidable responsibility | Recovery | Transition result | Risk / confidence |
|---|---|---|---|---|
| Same-index exact physical copy | Repeated H+normal on admitted values | Eligible24-slice mean prep3.885→0.237 ms;3.648 ms saved | Three bands improve strongly; one does not;16.690 ms peak remains | High feasibility confidence for tested key; incomplete path/cross-run proof |
| Device-local prepared working buffers | Downstream consumption of system-memory physical data | Selected band mean14.703→12.378 ms;2.325 ms difference | Every band improves;13.565 ms peak remains | Separate bounded KSA-aligned responsibility; portability/allocation budget unproved |
| Full cross-index map | Additional physically identical values elsewhere | Not measured; work-count estimate only | Tail benefit possible, no qualified closure claim | ABI/map/lifetime and GPU direction proof outstanding |
| Barrier/slicing changes | No redundant synchronization identified | Unproved | Could move work or increase latency | Reject as an unsupported implementation target |
| Copy + placement | Two distinct measured partial corrections | Not measured in combination | No combined result | Not disguised as the one authorized winner |

## Performance and scheduling

| Run | 44-frame GPU mean | P95 | P99 / peak |
|---|---:|---:|---:|
|Fresh baseline|14.703|15.652|17.785|
|Baseline repeat|14.712|16.616|18.687|
|Same-index copy|11.960|14.981|16.690|
|Placement only|12.378|12.822|13.565|

| Generation | Baseline band mean / peak | Copy mean / peak | Placement mean / peak |
|---|---|---|---|
|18|14.026 /14.999|13.757 /14.656|12.037 /12.190|
|19|15.203 /17.382|11.548 /16.690|12.799 /13.565|
|36|14.152 /15.645|10.970 /14.981|11.962 /12.132|
|37|15.429 /17.785|11.564 /16.588|12.714 /13.391|

| Generation | Baseline prep mean / total | Copy prep mean |
|---|---|---:|
|18|3.747 /41.212|3.633|
|19|3.858 /42.440|0.636|
|36|3.803 /41.833|0.813|
|37|3.832 /42.154|0.645|

Detailed per-slice mean/P95/P99/total, tail slices, whole valid-Earth traversal median/P95/P99, CPU scopes, demand costs, residency and source hashes are in `summary.json`; original compact rows remain in each run record. All selected jobs still occupy11 displayed GPU frames. A same-index optimization is sensitive to which published basis was actually retained; identity must never be forced to make a benchmark faster.

Direct job-start→publication wall-clock measurements in the later host:

| Generation | Baseline repeat | Placement |
|---|---:|---:|
|18|199.653 ms|158.355 ms|
|19|215.625 ms|165.774 ms|
|36|209.506 ms|157.710 ms|
|37|217.120 ms|166.971 ms|

These are observed run durations, not an identical-pupil latency proof. Earlier copy/baseline records lack that scalar; equal frame counts are not substituted for it. The `Prep publication` record's `pupil` field is the **source** published pupil.

CPU after startup remains separately reported. For the eligible24-slice copy subset, completion-host mean falls17.924→13.337 ms and fence-wait mean14.501→10.555 ms; removed GPU work was not replaced with the expensive CPU identity oracle. The baseline repeat also exhibits higher host validation/upload/record times, so absolute CPU deltas across runs are not a clean causal partition. None of the production-like controls has a post-frame30 host interval over40 ms.

No new regional bytes, maps or persistent physical copies were added by the same-index control. Its extra current64 B reads and source lattice16 B reads replace evaluation on admitted vertices; destination64 B writes remain. Placement moves the same bounded working allocation to the measured device-local heap and preserves mapped consumers; it adds no readback pass. Oracle-only markers, comparisons and CPU scans are excluded from performance records.

## Autonomous gate / implementation

**FAIL — stop for Project Control.** Measured avoidable cost and the cheap meaningful performance signal are proven. The KSA boundary is resolved. However, there is no unique complete winner; the architectural choice between two independent responsibilities remains; cross-run exact trajectories and full correctness/portability gates are incomplete; and neither isolated control meets the transition contract. Conditions1/3/11/12 of autonomous qualification are therefore unsatisfied or incomplete. The cheap performance signal passes; the full closure objective does not.

No production implementation phase was entered. No production file/shader/test migration occurred. Old responsibility remains authoritative. The ordinary fixed100-sample matrix, full L0–L17 traversal, warp1/600/7,776,000 and broad regressions were **not rerun**, because those are post-implementation gates. Their banked-M13.4 results remain in the prior exit assessment and are not presented as fresh gauntlet results.

The new bounded regional controls each completed1000 frames and37 reported publications with complete readiness and no observed owner gap/overlap/stale draw, Vulkan error or crash. This is observed probe coverage, not a universal publication or physical acceptance claim.

## VERIFY A — physical/publication challenge

The independent reviewer accepted the tested within-run exact-copy proof and immutable-domain boundary. Strongest attack: treating that proof plus37 publication messages as full cross-run geometry/raster/freshness parity. Pupil histories differ and such parity was not measured. Pole/seam/cancellation/count/identity negative controls, full mapped-key proof and production allocation fallback remain outstanding.

The first private GPU-oracle attempt failed `VUID-vkCmdDispatch-None-08114`: newly declared current bindings38/44 were not initialized during initial bootstrap. It was rejected, not counted as PASS. The private host then bound valid temporary aliases while current metadata was invalid, and normal publication supplied actual current descriptors. Initial reuse remained zero; the clean strict rerun passed. Exact error/hash/size are retained in `summary.json`; the rejected raw log is disposable. This was a diagnostic prototype defect, not a banked production defect.

## VERIFY B — performance/scheduling challenge

The independent reviewer verified frame alignment, changed pupil identities and remaining final-frame cost. First-eight-slice savings cannot establish closure. Original final whole-GPU minus incoming-dispatch timing leaves12.560–14.190 ms residuals; these are accounting residuals, not guaranteed counterfactual lower bounds. Copy P95/P99 and placement P95/P99 remain above the contract. Placement and reuse are distinct partial corrections, so combining them requires a new explicit bounded architecture decision.

## Project Control decision needed

Choose the next bounded responsibility after reviewing these partial results. A useful next proof would pin actual physical/pupil inputs, select GPU-local physical working storage or exact reuse as its explicit target, and test required portability/identity/cancellation and final-frame behavior. A combined treatment must be explicitly scoped; it is not silently implemented here. No further blocker or major rendering front is invented.

## Reproduction, evidence and closeout

Permanent budget:10 MiB. Retain this report, compact numeric records, exact source/runtime/asset identities, oracle/prototype recipe, latest private timing patch, compiler statistics/hashes, independent findings and cleanup manifest. Continuing value is the unresolved preparation decision. Production has no dependency on this evidence.

For reproduction, use `NOVACORE_PREP_EVIDENCE_DIR` pointing to a fresh directory under `E:\NovaCore\build\`; run `gauntlet.py baseline`, `build`, then `profile`, `oracle` and `placement-only` as needed. Run `prototype.py oracle`, `copy`, `height` or `normal` serially. Original shaders are restored in a finally block; the build restores production source and the normal native build DLL. Run `restore.py` afterward to recompile restored native/managed intermediate objects. Do not run measurements concurrently. Do not overwrite the retained results.

Normal launcher remains `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`; normal runtime remains `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`. Neither received private diagnostic shaders or binaries. `restoration.json` records successful restored-source native and managed Release rebuilds. `closeout.json` records the final normal-deployment, shader, asset, whitespace/status and post-cleanup240-frame Florida smoke checks.

`disposable-manifest.json` identifies only this ticket's generated scratch. `storage.json` contains final created/retained/disposed/remaining byte accounting. The67,108,864 B elevation scratch entry is a hard-link alias, not another production allocation; only that alias is retired. No raw captures were retained or relocated. Final remaining disposable output must be0.

No stage, commit, push, merge or tag. Stop for Project Control.
