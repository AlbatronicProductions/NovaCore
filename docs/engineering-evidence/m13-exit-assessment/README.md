# Post-M13.4 M13 exit assessment

**M13 NOT READY — ONE BOUNDED BLOCKER REMAINS**

Assessment date: 2026-09-07. No optimization implemented; M13.5 is not assigned or recommended. Stop for Project Control.

## Decision

The single blocker is **the frame-budget cost of incoming regional physical preparation during Earth/Florida replacement and publication transitions**. The complete currently published terrain remains correct while its replacement is prepared, but repeated supported Florida return transitions exceed 11.11 ms GPU for consecutive frames. This is stronger evidence against closure than Florida's fixed-pose miss of the preferred 8.33 ms target.

Preparation is required work. Its measured duration is not proof that it can be eliminated. The avoidable portion and output-preserving recoverable milliseconds are **unproven**, so this assessment does not meet the >=1.5 ms evidence gate for recommending M13.5. No proposed milestone title is assigned. Project Control should decide the next bounded proof concerning preparation scheduling/reuse before approving another implementation milestone.

## Baseline and isolation

HEAD, main, origin/main and the peeled annotated tag `m13.4-zero-contribution-terrain-material-noise` all resolve to **047ae479b33831eae1c0dfa3f37c657a7f70148f**. Its annotated tag object is `668efe2b06d2fcc907a0f46674c493b7670a2751`. M13.3 remains `180eaf150ba5db6364e17dd48336690778f058f9` under `m13.3-prepared-physical-terrain` (tag object `bb08cba20947f863391649e68e2e4fe7035520db`). M13.1/M13.2 identities are retained in baseline.json.

Started on clean main, staged and tracked diffs empty, only main local branch present. Assessment branch: `codex/m13-exit-assessment`. A historical detached M12B worktree registration is prunable; left untouched. Main and all tags remained unchanged.

Reference device logged by every measured run: **AMD Radeon RX 6800 XT, Vulkan 1.4**. Requested and screenshot-verified client size: **3440 x 1440 native**, borderless. Child-process canonical Khronos validation uses its own explicit layer manifest with an absolute SDK DLL path and an empty implicit-layer directory. No global registry changes, VUID suppression or reduced-quality modes. All processes completed with exit 0 and no captured Vulkan validation errors/crashes.

The following deployed identities were verified before measurements; all 49 SPIR-V hashes per configuration, source shader identities, three physical assets, and the final comparison are retained in baseline.json / closeout.json:

| Configuration | Native DLL SHA-256 | Managed sample DLL SHA-256 |
|---|---|---|
| Debug | eabc5e1919b91879b32f5e756f836de55aa4ca0c08788b6ad32e4764f9d874c8 | 206dfa919338a09091b9567874613235a4ff89239d421d1d44256fa3a3a8d279 |
| Release | 74859dbb7854e92d3069e4081276eef98980945c2c9c4d4adf3a926b18cbaeca | 86298b31bc05f7ab061f96567b75e798d7b6443f8437493f1254a49418502894 |

Accepted production fragment SPIR-V SHA-256: `8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`. Normal Release host: `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0`. The ordinary launcher remains `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.

## Method and important measurement limits

Fixed results use the **unmodified banked native renderer and all banked shaders**, with only a private managed camera driver. CPU and aligned dynamic results use a private native timing build: existing timestamp/counter results plus host clocks, acquire/recreate outcomes and frame identities. No capture buffers, new shader experiments, counter-off control, forced tessellation or AMD disassembly mode were enabled. Native control traversals and a directly deployed Florida run reject a logger-only explanation for the transition overrun.

The temporary two-file source patch is retained as timing-host.patch and was restored byte-for-byte before measurements. The normal deployed runtime never received the diagnostic DLL. Native and managed intermediate objects were rebuilt from restored production source at the end, while preserving the verified normal deployment.

All fixed tables use the last **100 warm, frame-aligned samples**, after preparation ceased. Full directional observations remain in lossless columnar records; early preparation samples are not erased. CPU values come from the separately run paired profile, not a claim that quiet GPU and CPU values were sampled simultaneously. Update includes fence wait. The mixed `validationUpload` scope is real production work (`CreateProductionBillboard`, validation, upload, production uploads and regional residency); it is **not subtracted** as presumed instrumentation overhead. The complete host frame also contains render/present and initialization costs.

"Terrain" below is the existing `detailedDraw` GPU timestamp interval, **not fragment-only time or an isolated complete terrain subsystem cost**. Cull/preparation has its own interval in the workload table. Some other native stage timestamps overlap; do not sum background, material/overlay, candidate draw and detailed draw. Total GPU is the authoritative budget measurement.

The requested 50 m refinement responsibility needs an explicit distinction: at **exactly 50 m physical clearance**, the production edge-distance fade can already reach factor 1. Files named fixed-active/cpu-active retain that boundary control; they are not active-refinement proof. Actual active-refinement measurements use the established **10.004 m clearance, pitch -1 rad** pose inside the 50 m refinement region, with measured maximum factor 64. The added active sweep and full L0-L17 traversal cover actual refinement work. No factors or visibility rules were changed to achieve a result.

## Fresh fixed production envelope

All values ms. Budget distances are median minus target: positive means over budget.

| Pose | Terrain draw median | Total GPU median | GPU P95 | GPU P99 | CPU frame / update median | Delta 8.33 | Delta 11.11 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Orbital | 0.781 | 1.188 | 1.337 | 1.373 | 3.486 / 3.064 | -7.142 | -9.922 |
| Factor 1 near surface | 2.420 | 5.167 | 5.819 | 5.824 | 7.821 / 7.413 | -3.163 | -5.943 |
| Florida / regional | 7.869 | 10.581 | 11.002 | 11.038 | 13.382 / 12.903 | +2.251 | -0.529 |
| Grazing, 10.004 m | 2.794 | 5.600 | 6.487 | 6.501 | 8.044 / 7.661 | -2.730 | -5.510 |
| Active 50 m refinement region, 10.004 m clearance | 3.416 | 5.964 | 5.977 | 5.986 | 8.383 / 7.987 | -2.366 | -5.146 |
| Exact 50 m clearance boundary control | 1.862 | 4.715 | 4.863 | 4.878 | 6.834 / 6.443 | -3.615 | -6.395 |
| Inland 40 N, 105 W, 50 m above H | 6.788 | 9.735 | 9.818 | 9.858 | 11.776 / 11.373 | +1.405 | -1.375 |

Inland camera radius is body radius plus canonical physical height plus clearance; this is not an inside-ground benchmark. Florida uses the authored launcher eye with a fixed diagnostic orientation; the ordinary route orientation is also tested separately. Camera body positions/orientations and generation identity are retained in every pose record.

| Pose | CPU record | CPU submit | Fence wait | Selected/TCS patches | TES invocations | Fragment invocations | Clipping-output primitives | Cull/prep median |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Orbital | .146 | .040 | .839 | 15,376 | 46,128 | 35,913 | 12,892 | .105 |
| Factor 1 | .139 | .036 | 5.127 | 360,946 | 1,082,838 | 2,595,734 | 145,268 | 2.687 |
| Florida | .169 | .041 | 10.682 | 294,750 | 884,250 | 3,993,782 | 121,838 | 2.654 |
| Grazing | .138 | .036 | 5.453 | 378,919 | 3,470,003 | 2,488,853 | 242,630 | 2.754 |
| Active refinement | .139 | .036 | 5.892 | 2,060 | 2,779,255 | 4,953,599 | 946,137 | 2.493 |
| Exact 50 m boundary | .138 | .036 | 4.344 | 8,953 | 26,859 | 4,953,600 | 8,949 | 2.503 |
| Inland | .146 | .037 | 9.262 | 9,252 | 27,756 | 4,953,600 | 9,244 | 2.529 |

Clipping-output is the available generated/raster-input primitive statistic; it is not every pre-clipping generated primitive. No new query was added to imply that otherwise unavailable quantity. Maximum outer TES factor is 64 at active/grazing and 1 at the other listed fixed poses. All seven warm windows have publication generation 1, no incoming/current physical-preparation or geographic demand dispatch in the warm sample window. Startup incoming preparation is 1 dispatch/.887 ms at orbital; near poses use 11 slices, median approximately 3.75-3.80 ms each (Florida maximum3.899 ms). Demand and preparation raw numerical summaries remain separate in the JSON files.

Florida is the slowest fixed regime. Its preferred-target miss alone could be an explicit Project Control economic decision; its 0.529 ms median / 0.072 ms P99 margin below 11.11 ms is **not substantial future-system headroom**. The dynamic repeated overrun below prevents closure independently.

## Dynamic envelope

Each GPU stream is preserved. For a positively frame-identified Earth sample, the completing host must satisfy `gpuFrame == geometryFrame == host.frame - 1`, with positive identity. Bootstrap uses identity0; during regional focus-away the terrain identity499 repeats across non-Earth frames. These samples remain in full-stream timing but must not be misrepresented as new aligned Earth geometry. summarize.py matches the GPU/host sequence and values and asserts complete correspondence. Filtering only `frame > 0` is insufficient.

Median / P95 / P99 in ms; CPU includes initialization. Full-stream GPU includes bootstrap and non-Earth timing; aligned Earth excludes their ambiguous geometry identities.

| Run | Rendered frames | Aligned Earth samples | Aligned Earth GPU | Full-stream GPU | Whole host CPU |
|---|---:|---:|---|---|---|
| Full surface-space-return | 1,701 | 1,698 | 6.490 / 8.927 / 10.207 | 6.490 / 8.927 / 10.207 | 9.184 / 11.824 / 13.884 |
| Active near-camera 90x9 view sweep | 843 | 809 | 5.545 / 7.086 / 7.468 | 5.524 / 7.116 / 7.469 | 8.107 / 9.844 / 10.494 |
| Exact50 m boundary view sweep | 843 | 809 | 4.419 / 5.877 / 6.108 | 4.460 / 5.883 / 7.328 | 6.985 / 8.575 / 10.411 |
| Inland50 m view sweep | 843 | 809 | 8.642 / 10.655 / 11.153 | 8.486 / 10.651 / 11.153 | 11.086 / 13.396 / 14.034 |
| Florida regional/focus return | 1,000 | 737 | 10.841 / 13.731 / 14.674 | 10.739 / 12.996 / 14.604 | 13.230 / 16.215 / 17.585 |
| Florida detach/orbit/return | 500 | 397 | 10.860 / 13.645 / 14.700 | 10.825 / 13.454 / 14.643 | 13.384 / 16.542 / 22.018 |
| Bounded warp | 779 | 745 | 5.492 / 6.417 / 7.332 | 5.491 / 6.433 / 7.373 | 8.119 / 9.769 / 11.758 |

Full traversal visits **L0-L17, ten finest snaps, three scale reversals and 58 publications**, with surface movement, horizon rotation, retreat/reapproach and warp phases. It contains five observed crossings of the strong-refinement workload proxy (TES >4xTCS, only positive matched workloads); this is not a claim of an exact factor threshold. The dedicated active view sweep reaches factor64. Full traversal reports zero owner/overlap/stale failures, height/normal parity telemetry zero and maximum pupil error2.998e-6 rad. These are the driver's existing contracts, not a new universal geometric-error bound.

Full traversal host snap P95/P99=11.242/11.242 ms; publication P95/P99=9.659/10.163 ms. Scale-transition P95/P99=12.061/15.248 ms, including initialization in its native aggregate. Normal-native full control repeats the supported traversal; control records retain its own independent distributions. Normal-native inland and active view controls also pass ownership with the same banked shaders. The inland view sweep has a small P99 crossing and maximum11.728 ms (normal control maximum11.928); it does not establish a separate sustained blocking responsibility.

Regional driver moves laterally during frames120-149, retreats at180, returns at280, leaves Florida at400, focuses Mars500/Saturn580, and returns to Florida660. The ordinary Florida driver detaches/reattaches80, ascends160, returns280. These are supported production camera operations; no missing data, stale owner, overlapping owner, Vulkan error or crash was found. Regional/Florida publish37/19 generations respectively.

## One blocker: repeated regional replacement-preparation frame cost

The limiting normal condition is the regional return, **not active tessellation**. Four late-stage bands recur in one supported route and repeat in the separate Florida run:

| Regional GPU frames | Consecutive frames above11.11 | Mean GPU ms |
|---|---:|---:|
| 345-355 | 11 | 13.775 |
| 357-367 | 11 | 14.653 |
| 725-735 | 11 | 13.841 |
| 737-747 | 11 | 14.643 |

Earlier return bands are also retained in summary.json, not discarded. During the long bands, incoming preparation consumes approximately **3.5-3.9 ms per slice**. At the final slice, incoming cull/compact also runs before publication, increasing the combined scope. The regional incoming preparation median/P95/max over315 observed slices is **3.639 / 3.844 / 3.957 ms**. Final incoming publication follows at host356/368, then736/748.

The measured responsibility boundary is visible in current production source:

- `native/NovaCore.Native/RegionalPhysicalPreparation.inl`, `RecordRegionalPreparation`: a bounded vertex slice into unpublished storage; only the final slice may proceed to incoming cull/compact.
- `native/NovaCore.Native/NovaCoreNative.cpp`, `Record`: geographic demand, current production work and incoming production work all execute before the existing cull/preparation timestamp ends and the current-owner draw.
- `UpdateRegionalPreparation`/`InspectRegionalPreparation`: keep the published pupil while the replacement is incomplete; publish after the fence and complete identity checks.

GPU frame747: total **16.65820 ms**, candidate draw7.80520 ms, combined cull/preparation8.79572 ms. Submission host747: generation36, pupil62, flags1, update16.7001, acquire.0472, record.1922, submit.0473, present.2223, recreate0, total17.2187, residual.0096 ms. It inspects the preceding GPU746, so its update must not be incorrectly paired with GPU747.

Completing host748: generation37, pupil63, flags1; GPU747/geometry747/gpuGeneration36; update18.8877, **fence16.5265**, acquire.0608, record.2253, submit.0601, present.2326, recreate0/count0, total**19.4824**, residual.0159 ms; acquire/present resultsVK_SUCCESS. The associated previous/current identity distinction is retained, not collapsed into one misleading generation.

The normal-native regional control, with no frame instrumentation, records periodic GPU totals **14.712** and12.655 ms in the transition sequence, and whole-host P95/P99 **16.454/17.886 ms**. Its mixed validation/upload average remains2.050 ms. It lacks full GPU per-frame percentiles, so it is a reproduction of the slower condition and preparation schedule, not a second exact distribution. Directly deployed current-time Florida also reports transition GPU14.468 ms. The overrun is therefore not explained solely by the extra host logger.

**Why it blocks:** repeated consecutive GPU frames above11.11 and perceptible transition pacing fail the dynamic budget bar even though prepared data, ownership and fixed medians pass. **Avoidable portion:** unproven. **Expected recoverable milliseconds:** unproven; the full8.8 ms or required3.8 ms cannot be called removable. **Target for the next proof:** whether scheduling or demonstrably exact reuse can fit replacement preparation into the display-frame budget while retaining complete canonical H, deterministic atomic publication, current-owner continuity, bounded residency and unchanged visual outputs. This is a direction for a bounded investigation, not an approved implementation architecture.

## Large host events and pacing attribution

Across seven CPU profiles and seven dynamic profiles there are25 host frames >40 ms. All are **initial frames1 or7-9**. The maximum217.234 ms is initial frame1, generation/pupil0, with196.6212 ms explicitly measured in initial swapchain recreation; acquire/present succeeded. Other initial frame1 costs have the same measured scope. Repeated startup fence waits during frames7-9 reach74.4662 ms; their initiating driver/host cause is **unclassified**. They occur before the first published owner, with geometry/timing identity0, and cannot be attributed to a particular terrain GPU frame. Every full row, including residual, is retained.

There are **no >40 ms host events after frame30** in these fresh profiles. This does not claim startup is hitch-free, nor infer a cause for the historical181 ms event. No replay of that old event was undertaken. The actual closure blocker is the smaller but repeated post-start regional GPU overrun documented above. Overall perceptually smooth dynamic performance does **not** pass the stated exit bar. CPU waits on that GPU work; it has not independently become the new dominant compute/submission limiter. Full host times are nevertheless above11.11 at Florida and are not presented as90 FPS performance.

## Florida contact and regional correctness

The fresh production route preserves site identity, canonical generation4, body-fixed seating, foundation depth **6.835569277405739 m** and anchor H **15.134892258793116 m**. Final Florida physical camera altitude is85.05556330084801 m. Foundation remains submitted with the pad through supported return. Regional catalog loads670 records, uploads93,392,640 bytes, and requires no anchored demand. Complete readiness precedes the atomic sole-owner publication. No shader, terrain, facility, lighting or physical behavior changed in this assessment.

The actual normal deployed Release application was captured at the supported return pose, at native3440x1440, using process-owned foreground client bounds. `florida-current.png` was visually inspected: terrain, foundation, support and facility shadow are present; no new visible separation is apparent at this bounded pose. Exact startup UTC/runtime identity is in florida-current-visual.json. `florida-return.png` records the deterministic J2000 control; its terrain is too dark to adjudicate contact and **is not used as contact proof**. Neither screenshot is an exact GPU-frame geometry/depth capture or a new exhaustive low-angle manual acceptance. Earlier production acceptance is preserved; this assessment found no regression in its bounded view and existing scenario contracts.

## Warp and future fidelity

Warp control **PASS**: 1x,600x and7,776,000x each have240-frame holds. Total779 render frames and777 GPU timing samples; pupil/frame identity1 remains stable, one initial topology upload/publication, **zero further publications during each hold**. GPU costs remain single-digit milliseconds rather than scaling with simulated tick count; the supported display-frame render loop remains the owner of preparation/render work. This is bounded render-cadence evidence, not a test of unimplemented simulation workloads.

Focused architecture verification found no capacity regression:

- Hardware tessellation remains enabled, patch-list topology and three control points remain, fractional-odd TES remains, and screen/skew-aware factors still reach64. Active measurement confirms this capacity is exercised.
- M13.4's exact `landDetail > 0` branch removes only mathematically zero-contribution noise. Positive/partial material contributions, derivatives and downstream roughness/specular responsibilities remain. No epsilon quality reduction was introduced.
- M13.3 still prepares full canonical physical relief upstream for current/incoming published geometry. Continuous H remains gameplay/physical authority; the rendered prepared surface remains its accepted interpolated representation. No universal centimeter bound is invented from local contact proofs.
- TES transport and displacement extension points remain. **KSA-class authored material height-map TES displacement is not already implemented**: current prepared mode has zero additional local TES displacement and the current material visual-displacement signal contributes to fragment normals. Future geometric displacement must preserve physical/contact coherence and conservative publication bounds.
- Current active headroom is about5.15 ms below11.11, but Florida fixed median headroom is only.53 ms and transition headroom is negative. This does not plausibly demonstrate room across all Earth regimes for clouds, water, atmosphere, richer materials, shadows and spacecraft/effects. Those systems were neither implemented nor budgeted here.

## KSA responsibility comparison and independent red team

The independent read-only verifier challenged closure using current local source and fresh measurements. Its strongest concrete challenge is the repeated preparation bands, not historical inland rankings or a hypothetical future-fidelity defect. The lead additionally corrected two evidence traps: exactly50 m is a factor1 boundary, and nonzero stale terrain IDs during focus-away cannot establish aligned Earth samples. No other implementation blocker is asserted.

KSA production shader sources remain under `E:\Kitten Space Agency\Content\Core\Shaders\Planet`: PlanetTessEvaluation.tese interpolates prepared data and blends/fades authored displacement; TessellationFuncs.glsl retains screen/skew refinement up to64; Planet.frag evaluates contributing materials. The retained KSA reconstruction supplies exact local source pointers and hashes: `../ksa-terrain-architecture/pipeline-maps.md` and `README.md`.

Relevant local reconstructed source under `E:\NovaCore\build\ksa-residency-reference\source`:

- `KSA.Program.cs:4541`, `KSA.PlanetRenderer.cs:1770,1885-1947`: display-frame mesh preparation, resource-frame slots, ordered modifier/finalization/normal passes and barriers before consumers.
- `KSA.Rendering.StreamingUploader.cs:114-145`: completed transfer polling before completion callbacks. `KSA.Rendering.CelestialTextureStreamer.cs:819-849`: publish promoted view and content version after completion. Its319-330 supplies the published height view or coarse render view.
- `CelestialTextureStreamer.cs:872-881` and `KSA.Rendering.RetiredResourceQueue.cs:116-132`: preserve in-flight resource lifetime until retirement age/fence allows reclamation.

The compatible boundary is complete resource publication and safe retention of the prior owner. KSA's coarse available render-data fallback does **not** authorize incomplete NovaCore authoritative residual publication. Reviewed KSA source does not demonstrate an equivalent exact whole-physical-generation cache, cross-pupil reuse proof or preparation millisecond budget. Copying its per-display-frame mesh regeneration would not prove a solution to NovaCore's required preparation slices and could discard existing reuse. No KSA-derived recovery number is claimed.

## Reproduction and retention

Keep this report, compact measurement rows/identities, two bounded visual controls and the driver/timing patch. No raw GPU/frame archives were generated. Budget:5 MiB permanent evidence. Source/assets/generators/regression fixtures and KSA reference storage are untouched.

On the same banked commit with its verified Debug/Release deployments, choose a **new** evidence directory; never overwrite these accepted observations. Example PowerShell:

```powershell
$env:NOVACORE_EXIT_EVIDENCE_DIR='E:\NovaCore\build\m13-exit-reproduction-evidence'
python -B docs/engineering-evidence/m13-exit-assessment/assess.py baseline
python -B docs/engineering-evidence/m13-exit-assessment/assess.py build
python -B docs/engineering-evidence/m13-exit-assessment/assess.py fixed
python -B docs/engineering-evidence/m13-exit-assessment/assess.py cpu
python -B docs/engineering-evidence/m13-exit-assessment/assess.py dynamic
python -B docs/engineering-evidence/m13-exit-assessment/assess.py control
python -B docs/engineering-evidence/m13-exit-assessment/assess.py active
python -B docs/engineering-evidence/m13-exit-assessment/summarize.py
```

The normal regional control is `from assess import dynamic; dynamic('regional',False)` with the evidence source directory on Python's module path. visual.py uses a Python environment containing Pillow; `--current` selects the ordinary current-time startup, otherwise J2000. Visual capture is client-only and rejects an obscured window. An initial missing-Pillow invocation exited before launching the application; the bundled Python runtime then completed both visual runs. No packages were installed.

Run the captures serially. Rebuild native and managed intermediate objects from restored source afterward; retain the original verified deployment if compilation metadata changes output hashes. The builder restores source and normal native DLL in its finally block. All production shaders remain untouched. Refer to closeout.json for final identity/whitespace/status, disposable-manifest.json for the exact bounded generated-file accounting, and the storage section below for the completed cleanup result.

## Storage and final repository state

- Created: **78,777,127 logical bytes** (75.128 MiB), including the temporary67,108,864-byte production-asset hard-link alias. Newly exclusive logical output: **11,668,263 bytes**; this is not an NTFS allocation measurement.
- Retained: **4,736,442 bytes** (4.517 MiB), under the5 MiB permanent budget. Reports, bounded columnar numerical evidence, provenance, two compact screenshots and reproduction/cleanup tools.
- Disposed: **74,040,685 logical bytes**, comprising75 exactly classified private-host files plus the post-cleanup process-local loader manifest. Exclusive logical data retired: **6,931,821 bytes**. Unlinking the elevation alias did not delete or free the protected production asset. No raw GPU captures were made or moved elsewhere.
- Remaining disposable output: **0 bytes**; private scratch and temporary smoke loader directory absent. Log text was streamed into compact records and discarded, not retained as a bulk archive. Accounting counts file inventory, not cumulative compiler rewrite I/O.

Native/managed intermediate rebuilds from restored source passed. After the guarded cleanup, the **normal deployed240-frame Florida smoke PASS** verified L17 terrain publication, required production assets, sole ownership, zero Vulkan errors and zero crashes. Debug/Release native and all49 deployed shader identities per configuration remained exactly equal to baseline.

`git diff --check`: **PASS** (exit0). Staged and tracked production diffs are empty. Main/origin/main/M13.4/M13.3 identities are unchanged; no stage, commit, push or tag operation occurred. Only intended assessment evidence is untracked:

```text
?? docs/engineering-evidence/m13-exit-assessment/
```

**M13 NOT READY — ONE BOUNDED BLOCKER REMAINS.** Stop for Project Control.
