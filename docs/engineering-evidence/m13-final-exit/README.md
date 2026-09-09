# Post-M13.5 final M13 exit gauntlet

Lead judgment: **PASS**.

**M13.6 CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE**

**KEEP M13 OPEN FOR ONE PROVEN M13.6 RESPONSIBILITY**

The surviving unbanked change is CPU-read terrain-key placement. M13.5 remains the banked authority. This is not GPU-budget closure, a newly assigned milestone, or manual acceptance.

## Baseline and measurement contract

HEAD, main and origin/main were `d4baab6940a57a46e478b98e36f5e45d1c4b558f`, the annotated `m13.5-local-gpu-terrain-working-data` target. M13.4 remained `047ae479b33831eae1c0dfa3f37c657a7f70148f`; M13.3 remained `180eaf150ba5db6364e17dd48336690778f058f9`. Initial tracked, staged and untracked state was clean. The investigation created `codex/m13-final-exit`. No milestone name, commit or tag was assigned.

Reference device: Radeon RX 6800 XT, native 3440×1440. Baseline records contain both configuration identities, all 49 runtime shader hashes per configuration, source shader hashes and required terrain/elevation/regional asset hashes. The normal deployed managed driver is distinguished from private fixed-pose and native timing instrumentation. Canonical process-local Vulkan validation remains strict, with unrelated implicit layers isolated; ambient compatibility is not reclassified by this ticket.

All numbers below are fresh banked-M13.5 measurements unless explicitly labeled prototype or implemented candidate. Fixed populations contain 100 warm samples. Percentiles use nearest rank. GPU rows join only when completed GPU frame equals geometry frame and host frame minus one; the child timestamps additionally match frame and total. Publication markers establish the first authoritative host frame. Later upload/publication tails are retained, not relabeled startup. All capture-enabled runs are excluded from performance analysis.

## Fresh fixed envelope

Quiet banked native GPU timing and separately profiled CPU timing are different runs with equivalent controlled inputs. CPU total includes fence waiting; update includes its nested inspection/callback/upload work. Marginal medians must not be added as if they were independent intervals.

| Pose | Terrain draw median | Total GPU median | P95 | P99 | CPU whole / update | Record / submit / fence | GPU minus 8.33 |
|---|---:|---:|---:|---:|---|---|---:|
| Orbital | 0.73636 | 0.82864 | 0.83716 | 0.83872 | 3.4057 / 2.9667 | 0.1608 / 0.0423 / 0.7546 | -7.50136 |
| Factor 1 | 2.31040 | 3.50228 | 3.51728 | 3.52040 | 5.9853 / 5.5605 | 0.1583 / 0.0411 / 3.4303 | -4.82772 |
| Florida | 7.70692 | 8.88412 | 8.92772 | 8.94396 | 11.4595 / 11.0057 | 0.1766 / 0.0442 / 8.7929 | +0.55412 |
| Grazing | 2.60444 | 3.79564 | 3.81368 | 3.82044 | 6.2424 / 5.8273 | 0.1535 / 0.0401 / 3.7253 | -4.53436 |
| Active factor 64 | 3.40768 | 4.58716 | 4.60588 | 4.60992 | 7.0627 / 6.6454 | 0.1526 / 0.0404 / 4.5095 | -3.74284 |
| Inland positive material | 6.76156 | 7.94880 | 7.95904 | 7.96092 | 10.4698 / 10.0274 | 0.1648 / 0.0435 / 7.8676 | -0.38120 |

The active case actually reaches factor 64 at 10.004 m and pitch -1 rad; it is not the older factor-1 pose mislabeled active. Full selected/TCS/TES/fragment counters, physical preparation and generation/pupil sets are retained in the fixed journals and Work A, together with all 100 samples.

| Pose | Selected triangle patches / TCS patches | TES invocations | Fragment invocations | Maximum outer factor |
|---|---:|---:|---:|---:|
| Orbital | 15376 | 46128 | 35913 | 1 |
| Factor 1 | 360946 | 1082838 | 2595760 | 1 |
| Florida | 294750 | 884250 | 3993943 | 1 |
| Grazing | 378919 | 3470003 | 2488953 | 64 |
| Active factor 64 | 2060 | 2779258 | 4953599 | 64 |
| Inland | 9252 | 27756 | 4953600 | 1 |

These are warm medians. Each fixed pose has one published generation, a single authoritative owner and no replacement during its warm window. Physical preparation/readiness completes before the retained steady samples. Florida's regional startup resolves 670 records; this startup preparation is not conflated with a warm per-frame request-queue cost.

## Fresh dynamic envelope

Normal deployed full/regional/warp controls passed. The detailed profiles below use byte-identical deployed managed movement code with private native timing. The active/inland sweeps use the separately disclosed diagnostic movement driver. Sparse normal GPU logs do not support invented normal-run percentiles.

| Route | Aligned Earth frames | GPU median / P95 / P99 / peak | >8.33 / >11.11 | Publications / replacements |
|---|---:|---|---|---|
| Full L0–L17 / surface–orbit / return | 1698 | 4.68676 / 6.79024 / 7.74608 / 8.13880 | 0 / 0 | 58 / 57 |
| Florida regional departure / return | 737 | 8.70492 / 11.57328 / 12.33732 / 12.66220 | 533 / 42 | 37 / 36 |
| Regional repeat | 737 | 8.70584 / 11.58168 / 12.32072 / 12.67580 | 534 / 42 | 37 / 36 |
| Warp | 745 | 3.43968 / 3.46144 / 4.94864 / 4.96764 | 0 / 0 | 1 / 0 |
| Active-refinement entry / exit sweep | 809 | 3.71404 / 4.62448 / 6.05980 / 6.08536 | 0 / 0 | 1 / 0 |
| Inland altitude/geography sweep | 809 | 6.77240 / 8.89196 / 9.21376 / 9.31404 | 202 / 0 | 1 / 0 |

The 1000-frame regional route contains 737 aligned Earth observations; the other frames are retained in the host journal, including supported non-Earth/focus and initial-authority phases. Normal controls and profiles agree on publication counts. No ownership failure, Vulkan error or crash was recorded.

The worst repeatable regime is Florida regional departure/return with incoming preparation overlapping the current detailed draw. Regional CPU after initial authority has median 11.2415, P95 14.1766, P99 15.2296 and peak 23.1587 ms; the repeat peaks at 22.7922 ms. Full-route CPU peaks at 24.4020 ms, and inland sweep at 26.5416 ms. No profiled CPU frame exceeds 40 ms after first authority. These nontrivial normal tails remain part of the result.

At the banked regional repeat peak, the GPU is 4.3458 ms above 8.33, 1.5658 above 11.11 and 3.9942 below 16.67. At regional P99 the corresponding first-run margins are -4.00732, -1.22732 and +4.33268 ms. This is not universal 120-FPS-class GPU closure or a promise of future full-game headroom.

## Work A: whole-frame responsibility

Work A preserves non-overlapping timestamps and complete frame identities. Regional current cull median is 0.73168 ms, compaction 0.01812 ms; the entire current block median is 0.75720 ms. Incoming physical work has P95 3.82696 ms, while the detailed draw has P95 7.94244 ms. The worst GPU frames combine required draw and incoming work. Parent/child scopes, scene/background/material subscopes and barriers are not double-counted. The enclosing CPU validation/upload scope is almost constant near 1.9–2.0 ms across cheap and expensive poses.

Narrow timing proves that `QueueProductionRequests` itself spends about 1.9 ms scanning the 8192-slot, 393216-byte shared terrain-key allocation twice per displayed frame. Warm request and pending counts are zero, with 126 records/uploads. The compatible selected memory type is uncached coherent host type 1 (flags 6); compatible type 3 adds host caching (flags 14). The loops still own live GPU-key observation, key repair and requests. Empty work queues are not a valid reason to remove them.

## Work B: candidate ranking

| Responsibility | Measured cost / avoidable portion | Qualified recovery | Complexity / confidence |
|---|---|---|---|
| CPU-read terrain key memory placement | About 1.9 ms CPU scan; same scans on cached coherent backing cost about 0.014–0.016 ms | Contemporary Florida/regional/full typical whole-frame recovery about 1.74–1.89 ms; full-route mean 1.560 ms; not GPU recovery | One allocation role plus existing fallback; high causal confidence |
| Current visibility/compaction | Roughly 0.75 ms regional current block, roughly 1.1 ms fixed near-surface | No independently qualified >=1.5 ms saving even for the whole block | Conservative bound/quality risk; not a winner |
| Positive material draw | Required material response dominates parts of detailed draw | No exact-output >=1.5 ms alternative proven; older changed-noise sensitivity is not a valid saving | New material representation would broaden scope and alter quality |
| Incoming physical preparation/publication | Several ms while replacement is active | No new safe >=1.5 ms avoidable portion proven after banked M13.5 | Exact physical authority and atomic readiness remain required |
| Tessellation/facility visibility | Required present/future geometry and accepted Florida appearance | No admissible removal or global weakening | Retain future displacement/silhouette capacity |

Exactly one bounded responsibility survives. This does not reopen M13.5's six GPU working-set roles or the rejected broad poll-elision alternative. Work B also records stale current-facing milestone wording as separate documentation debt; this ticket does not normalize unrelated current-state files.

## Work C: current KSA comparison

The installed reference is version 2026.9.7.5402, revision `487c3f340de24c6a81037120b6d1129c045c5400`, DLL SHA-256 `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`. Applicable current source fingerprints were verified. KSA owns requests in CPU map/request objects and polls actual in-flight uploader batches at completion. No equivalent 8192-entry Vulkan shared-key scan or HOST_CACHED selector was established.

ADAPT role-specific resource placement and completion ownership. INTENTIONALLY DIFFER by preserving NovaCore's existing GPU-shared keys, every producer/scan/write and synchronization contract. No new mirror, invalidation authority, copied KSA source or KSA dependency is introduced. Hardware tessellation and future material-displacement responsibilities remain intact.

## Cheap gate and implementation

Continuation gate: PASS for meaningful typical CPU/display-frame recovery, with no GPU saving asserted. Normal full-route mean improves 7.822733 to 6.262564 ms and median 7.7604 to 5.8739; regional first-pair mean improves 10.375325 to 8.814200 and median 11.5122 to 9.6670. The regional return mean improvement is only 1.446264 ms, while its median improves 1.7401 ms. Logging-disabled regional aggregate average improves 10.480 to 8.963 ms, median 11.439 to 9.622 and P99 15.693 to 13.936. Residual timing hooks remain in those controls; they are not claimed to be completely uninstrumented.

Orbital's initial apparent regression disappears in the same-host uncached return/cached repeat: 5.5341 versus 5.5291 ms median. The recovered CPU work fills a longer fence wait under the later paced regime. No orbital end-to-end gain or exact ambient pacing cause is claimed. Regional cached and uncached-return GPU runs both have 58 frames above 11.11; earlier differences are not uniquely attributable to allocation. Full-route dynamic inputs and some native frame counts differ, so no exact cross-frame raster equality is inferred from traversal counters.

Florida and inland same-input prototype parity pass: all 64 bytes per prepared vertex, selected oriented triangle multiplicity/winding, full D32, HDR and final image. Raw compaction order differs, which is not a change in the rendered triangle set. All captured attachment values are finite. Capture timing and its file-write stalls are excluded from performance evidence.

The implemented default adds `MappedBufferUse::TerrainRequestKeys`, preferring compatible HOST_VISIBLE|HOST_COHERENT|HOST_CACHED memory for only the terrain key allocation. The first compatible coherent host type remains the fallback. Existing allocation/map capacity fallback and fatal non-capacity errors remain unchanged. Default Host allocation, all six banked GPU roles, loops, workers, fences, shaders, assets, geometry and publication remain unchanged. Fourteen original policy checks are preserved and sixteen focused cases are added. No production correction revision was necessary.

Retirement condition: the key callsite uses the qualified default preference; no prototype environment switch ships in production. The compatible uncached fallback has a current device-support responsibility, not a hidden alternate terrain owner. Proposed title only: **NovaCore M13.6: Cache CPU reads of terrain residency keys**.

## Final validation and judgment

The implemented default passed the same Florida/inland exact prepared/oriented-index/D32/HDR/image checks, without the prototype switch. Both traces select compatible memory type 3, flags 14, for exactly 393216 bytes. Source review confirms no shaders, managed source, physical authority, facility support, residency, tessellation, culling, publication or workload settings changed.

| Validation | Debug | Release |
|---|---|---|
| Native / managed builds | PASS | PASS |
| Mapped-memory policy | 30 checks PASS | 30 checks PASS |
| Graphics headless | 79 pass / 0 fail / 0 skip | 79 pass / 0 fail / 0 skip |
| GPU, no window | 8 / 0 / 0 | 8 / 0 / 0 |
| Visible window / lifecycle / regional dependencies | 4 / 0 / 0 | 4 / 0 / 0 |
| Native GPU | 2 / 0 / 0 | 2 / 0 / 0 |
| Native regional CPU | PASS | PASS |

Release also passed 15 launcher regressions, six supported preset probes, 500-frame Florida smoke, full L0–L17 transitions, warp, both production asset verifications and the six final fixed cases. All 49 deployed shaders in each configuration match the banked hashes. No validation errors or test skips are hidden by aggregation. Physical manual acceptance is not inferred from these automated results.

### Actual default timing

Final quiet GPU medians/P95/P99 are orbital 0.82540/0.90936/0.99560, factor 1 3.52580/3.68496/3.72064, Florida 8.95040/9.07908/9.12464, grazing 3.86328/3.97364/4.07700, active factor 64 4.70000/4.85920/4.87168 and inland 8.05332/8.23944/8.30720 ms. Small mixed GPU changes remain; no GPU gain is claimed. Final Florida/inland profiled CPU medians are 9.7659/8.7723 ms, recovering 1.6936/1.6975 ms from the fresh bank profiles.

| Default route | Aligned Earth frames | GPU median / P95 / P99 / peak | >8.33 / >11.11 | CPU after authority mean / median / P95 / P99 / peak | Publications / replacements |
|---|---:|---|---|---|---|
| Regional | 737 | 8.77008 / 11.59208 / 12.49588 / 12.84380 | 541 / 44 | 7.8270 / 9.4326 / 12.3144 / 13.4287 / 21.0747 | 37 / 36 |
| Full | 1698 | 4.75400 / 6.89472 / 7.93868 / 8.46020 | 2 / 0 | 5.9894 / 5.7498 / 7.9935 / 11.0399 / 50.4425 | 58 / 57 |
| Warp | 745 | 3.46220 / 3.66908 / 5.01572 / 5.14892 | 0 / 0 | 4.3360 / 4.2281 / 5.0474 / 6.5820 / 11.3319 | 1 / 0 |

The default full traversal has one early post-authority CPU outlier at frame 8: total 50.4425 ms, fence wait 48.4895 ms, completed GPU frame 7 about 0.982 ms. Its external scheduling/driver/presentation cause is unclassified. It remains in all distributions and is not silently relabeled startup, waived, or declared fixed. A later ordinary upload/inspection tail at frame 347 is 25.3435 ms. This one mixed wait does not prove a recurring catastrophic terrain-work owner or justify broadening the ticket. Final regional/full/warp typical CPU recovery is 1.8089/1.6860/1.7543 ms against the original fresh profiles, with run-order/environment limits retained in Verify B.

At the final regional GPU peak there is 3.8262 ms to 16.67, while 11.11 is exceeded by 1.7338 ms and 8.33 by 4.5138 ms. All 737 regional GPU frames remain below 16.67; this is an observed bounded sample, not a future-game guarantee. The CPU correction leaves those GPU limits explicit.

### Artifact identity and final smoke

Actual candidate managed source is unchanged from HEAD, but fresh builds produce managed DLL hashes different from the initially deployed banked artifacts. Exact binary pairing is asserted only for the earlier same-host A/B and default capture comparisons, not across different managed builds. Candidate dynamic managed hash matches its normal deployment manifest.

The full Release suite and default quiet measurements used native SHA-256 `873aed18af5872bde044b473d5656462813a93086cb239111dc2a7fb2501ebc2`. Rebuilding normal intermediate objects after diagnostic work produces `932267253ea6e0221ba4ef5093515874116b26ede8c565ff8b8316e02e21eabf`. Complete binary comparison finds only four changed timestamp bytes in the COFF/debug metadata; executable code, data, unwind, resources and relocation sections are identical. The final native hash matches the normal build and deployment, and that exact final runtime passed the post-cleanup 500-frame Florida smoke. Required asset hashes and all deployed shader hashes are unchanged.

Normal launcher: `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`; preset **Florida Launch Site**. It resolves to the normal `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe` and `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`. No launcher behavior changed.

### Independent reviews and stop

VERIFY A: PASS. Strongest concern was proving that the actual default, not just an opt-in prototype, preserved output and fallback. Default parity, source review and full validation resolve that blocker. VERIFY B: PASS for the bounded CPU/display-frame responsibility. Strongest concern was cost migration and overclaiming the >=1.5 ms bar; matched controls, return runs, logging-disabled comparisons and actual default measurements support the typical-frame payoff while retaining the orbital nongain, below-bar return mean, GPU misses and frame-8 wait.

No production revision was needed. No staging, commit, push, merge or tag occurred. Keep the candidate unbanked for Project Control acceptance; do not begin another optimization or next production front. Future-fidelity tessellation/material-displacement capacity remains intact. The stale current-facing milestone wording and unclassified early fence event are recorded limitations, not authorization for unrelated work.

Final `git diff --check`: PASS (line-ending warnings only). Intended Git status:

```text
 M native/NovaCore.Native/MappedBufferMemory.h
 M native/NovaCore.Native/MappedBufferMemoryTests.cpp
 M native/NovaCore.Native/NovaCoreNative.cpp
?? docs/engineering-evidence/m13-final-exit/
```

## Reproduction and evidence lifecycle

Retain this report, Work A/B/C, Verify B, baseline/deployment identities, lossless scalar journals, exact parity hashes and bounded reproduction scripts. `exit.py`, `poll.py`, `poll_dynamic.py`, `build_capture.py`, `parity.py`, `candidate_parity.py`, `candidate_runs.py` and `validate.py` separate each stage. They reuse retained instrumentation source but do not execute old cleanup paths. Only the lead builds/runs GPU work; private builds restore source and normal native deployment. Normal intermediate objects are rebuilt before final cleanup.

Permanent package budget is 6 MiB after lossless compression. One reusable raw parity slot is capped at 512 MiB; no bulk capture archive is retained. Current-ticket scratch is classified by exact path/hash/ignore state and reparse/hardlink guards before removal. Earlier banked evidence, production assets, normal deployment, .git and cache policy are outside cleanup scope.

Created: 113 classified scratch file names occupied 132633566 exclusive logical bytes (132807760 allocated) at retirement. The additional 67108864-byte elevation hardlink was not duplicated storage and its production target remains unchanged. Six capture writes totaled 759938376 bytes through the same three-file slot; this cumulative-write quantity is not additive to the live scratch inventory. Peak raw-slot content was 128369384 bytes, within the 512 MiB cap.

Retained: approximately 5.45 MiB of reports, identities, source and lossless numerical journals, within the 6 MiB budget. Exact final logical/allocated bytes and file count are in `closeout.json`. Forty-three journals totaling 28049753 uncompressed bytes are preserved losslessly in 4451055 compressed bytes; SHA-256 and decompression identity were verified before removing the loose duplicates.

Disposed: all 113 classified scratch files and all 43 verified loose-journal duplicates, 160683319 exclusive logical bytes removed. After accounting for compressed replacement streams, the corresponding net logical recovery is 156232264 bytes. This is current-ticket storage, not a claim about the entire workspace or every temporary build write. No raw GPU archive remains. Disposable remaining: **zero**. The final normal 500-frame Florida smoke passed after scratch removal, and both scratch/temporary-layer directories are absent. No Git/LFS, historical evidence, production asset or cache-policy cleanup occurred.
