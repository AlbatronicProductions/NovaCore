# Verify B: CPU-read key placement performance red team

This is a fresh banked-M13.5 comparison, not a repeat of the M13.5 GPU working-data win. The proposed role is the CPU-scanned terrain residency key table. Production correctness/fallback remain independent gates. This verifier performs no builds, GPU runs or production edits.

## Complete fixed100 populations

| Run | GPU median / P95 / P99 | Submission CPU median / P95 / P99 | Non-fence median | Queue median |
|---|---|---|---:|---:|
| poll-florida | 8.87696 / 8.92284 / 8.95232 | 11.44840 / 11.71410 / 11.79130 | 2.64790 | 1.91390 |
| poll-orbital | 0.82976 / 0.84072 / 0.87444 | 3.28470 / 3.46220 / 3.52970 | 2.52630 | 1.88620 |
| poll-return-florida | 9.10520 / 9.20364 / 9.23564 | 11.77030 / 12.10220 / 12.27390 | 2.72820 | 1.86590 |
| poll-return-orbital | 1.13668 / 1.26416 / 1.29784 | 5.53410 / 5.84800 / 5.96070 | 2.58590 | 1.84850 |
| cached-florida | 9.12292 / 9.20600 / 9.22828 | 9.91850 / 10.07910 / 10.29020 | 0.84950 | 0.01440 |
| cached-orbital | 1.11960 / 1.25600 / 1.27196 | 5.53710 / 5.79430 / 5.85210 | 0.78060 | 0.01410 |
| cached-repeat-florida | 9.13960 / 9.22784 / 9.45332 | 10.02710 / 10.29530 / 10.40500 | 0.93360 | 0.01570 |
| cached-repeat-orbital | 1.14120 / 1.24700 / 1.26716 | 5.52910 / 5.82920 / 5.90580 | 0.74670 | 0.01400 |

## Net benefit and input checks

Positive values below are baseline minus cached control. CPU benefit must be judged from complete host frames and non-fence work, not just the narrowed queue timer.

| Baseline -> cached | GPU median change | CPU median recovery | Non-fence median recovery | Queue median recovery | Input/hardware differences |
|---|---:|---:|---:|---:|---|
| poll-florida -> cached-florida | -0.24596 | 1.52990 | 1.79840 | 1.89950 | none in logged inputs/TCS/TES |
| poll-return-florida -> cached-florida | -0.01772 | 1.85180 | 1.87870 | 1.85150 | none in logged inputs/TCS/TES |
| poll-orbital -> cached-orbital | -0.28984 | -2.25240 | 1.74570 | 1.87210 | none in logged inputs/TCS/TES |
| poll-return-orbital -> cached-orbital | 0.01708 | -0.00300 | 1.80530 | 1.83440 | none in logged inputs/TCS/TES |
| poll-florida -> cached-repeat-florida | -0.26264 | 1.42130 | 1.71430 | 1.89820 | none in logged inputs/TCS/TES |
| poll-return-florida -> cached-repeat-florida | -0.03440 | 1.74320 | 1.79460 | 1.85020 | none in logged inputs/TCS/TES |
| poll-orbital -> cached-repeat-orbital | -0.31144 | -2.24440 | 1.77960 | 1.87220 | none in logged inputs/TCS/TES |
| poll-return-orbital -> cached-repeat-orbital | -0.00452 | 0.00500 | 1.83920 | 1.83450 | none in logged inputs/TCS/TES |

## Same-host orbital return/repeat and pacing limit

The later uncached orbital return reproduces the roughly5.53 ms whole frame seen with cached keys. Same-binary uncached/cached-repeat CPU medians are5.5341/5.5291 ms; P99 values5.9607/5.9058 and peaks6.3339/5.9252. GPU medians1.13668/1.14120 are also comparable. Non-fence work falls2.5859 to.7467 ms while fence wait rises2.9195 to4.7406 ms. The saved CPU work is absorbed by the existing paced interval in this cheap regime.

This return control does not support attributing the earlier3.2847-to5.5371 ms direct comparison to the cache change. Exact driver/presentation/clock ownership of the later pacing remains unclassified; no production accommodation is justified. Do not claim a1.8 ms orbital display-frame gain. Same-host Florida return against the two cached runs shows1.8518/1.7432 ms CPU median recovery. The least favorable earlier-host Florida comparison against the cached repeat is only1.4213 ms, so there is no universal per-run >=1.5 claim.

## Complete production-managed dynamic comparisons

These pairs use the same cached-capable native binary and byte-identical deployed managed route, with only the cached-key environment selection changed. Every GPU statistic uses positive gpuFrame == geometryFrame == host.frame-1 and equal frame/total for child scopes. CPU after initial authority includes all bodies/route frames from the first published host frame onward; no favorable-frame subset replaces the full populations.

| Route / mode | Host after first publication | CPU mean / median / P95 / P99 / peak | GPU aligned / median / P95 / P99 / peak | GPU >8.33 / >11.11 |
|---|---:|---|---|---|
| regional / baseline | 898 | 10.37533 / 11.51220 / 14.33030 / 15.51240 / 24.45980 | 737 / 8.84548 / 11.75540 / 12.64832 / 13.04008 | 557 / 48 |
| regional / cached | 898 | 8.81420 / 9.66700 / 12.69910 / 13.91210 / 23.18010 | 737 / 8.94876 / 11.87425 / 12.82576 / 13.15552 | 554 / 58 |
| full / baseline | 1698 | 7.82273 / 7.76040 / 10.10750 / 12.14510 / 24.67080 | 1697 / 4.89380 / 7.11404 / 8.09588 / 8.87760 | 8 / 0 |
| full / cached | 1699 | 6.26256 / 5.87390 / 8.18100 / 9.82440 / 22.51010 | 1698 / 4.89156 / 7.06960 / 7.98880 / 8.62480 | 9 / 0 |
| regional return / baseline | 898 | 10.26046 / 11.40710 / 14.54830 / 15.68650 / 24.53690 | 737 / 8.90560 / 11.91204 / 12.71283 / 13.35426 | 551 / 58 |
| regional return / cached | 898 | 8.81420 / 9.66700 / 12.69910 / 13.91210 / 23.18010 | 737 / 8.94876 / 11.87425 / 12.82576 / 13.15552 | 554 / 58 |

Regional after-authority CPU mean recovery is1.561125 ms and median recovery1.8452 ms; full route mean recovery is1.560169 ms and median recovery1.8865 ms. Non-fence mean recovery is approximately2.0025/1.9661 ms. Normal sub40 ms topology/upload tails remain: full-route peaks24.6708/22.5101 and regional24.4598/23.1801. Neither pair has a CPU sample above40 ms after initial authority. This improves the display-frame critical path without eliminating all publication work.

Regional contains exactly737 aligned Earth frames on each side with identical37 generation/level/hash/publication-frame signatures. TCS/TES counts nevertheless differ on541 frames and fragment counts on531; no full camera/physical-byte stream is present. Full traversal has1697/1698 aligned frames and58 matching publication generation/level/hash signatures; generation25 onward shifts one native frame. At common frame numbers,34 generation,810 TCS,1040 TES and1373 fragment observations differ. Thus full traversal is a complete route comparison, not an exact cross-frame geometry join.

Regional GPU mean changes8.479253 to8.544920 ms; P99 changes12.64832 to12.82576 and >11.11 counts48 to58. Full GPU mean changes4.860449 to4.867943 ms; peaks8.87760/8.62480 and >8.33 counts8/9. These small mixed differences remain visible. There is no measured GPU-time recovery claim and no120 FPS GPU closure. Fixed geometry/output, publication, fallback and ordinary-driver tests remain required.

## Return-baseline and logging-disabled checks

The uncached regional return reproduces58 GPU frames >11.11, matching cached58 rather than the first baseline48. Return P95/peak are11.91204/13.35426 ms against cached11.87425/13.15552. Return and cached publication identities/cadence still match. Thus the initial additional10 threshold misses are not uniquely attributable to cached-key placement. No GPU saving is claimed.

Against the return, regional CPU median recovery remains1.7401 ms and P99 improves15.6865 to13.9121 ms. Mean recovery is1.446264 ms, below1.5, versus1.561125 for the first baseline. The meaningful typical-frame benefit is repeated; it is not >=1.5 for every population mean or every frame.

The same-host regional check with host/queue/GPU per-frame journaling disabled reports whole-run CPU average10.480 to8.963 ms (1.517 recovery), median11.439 to9.622 (1.817), P9514.387 to12.506 and P9915.693 to13.936. The mixed validation/upload mean drops2.011 to.112 ms. Both complete1000 frames and37 publications with zero recorded errors. This confirms the benefit is not dependent on those per-frame journal calls. Residual timestamps/log hooks remain: these are not fully uninstrumented runs. Their aggregate maxima184.415/194.019 include an unseparated startup-inclusive population, so no normal-frame tail attribution is manufactured from them.

## Continuation-bar interpretation

The ticket explicitly includes CPU update/submission as a closure gate and asks for net CPU/GPU improvement. A safe >=1.5 ms reduction in ordinary serialized display-frame wall time can qualify as meaningful CPU payoff. It must be labeled CPU/display-frame recovery; it does not reduce the GPU query, grant 8.33 ms GPU closure, or make M13.5 obsolete.

All stages of qualification remain separate: preserved inputs and request/upload/pending counters; actual net whole-CPU and non-fence benefit; unchanged or bounded GPU/tail behavior; normal production-managed traversal; exact key ownership/coherence/polling/publication; portable fallback and failure handling. A function timer alone cannot pass this gate.

**Performance judgment: PASS for the bounded prototype continuation gate.** Complete same-host Florida and normal production-managed full/regional routes show meaningful CPU/display-frame benefit. The orbital return/repeat resolves the apparent placement-specific wall-time regression; that regime remains paced with no claimed end-to-end gain. This is not final production readiness or a general GPU performance PASS.

## Cheap output gate and implemented-candidate boundary

The completed keys-florida-parity.json and keys-inland-parity.json reports both PASS at frame175,3440x1440: no logged input differences, all64 bytes of every prepared current vertex exact, exact oriented submitted triangle multiset and zero changed D32/HDR/final-image pixels. Compacted raw index order is not required to match; winding and multiplicity are preserved. Capture-enabled timings are excluded from both this review and Work A. These are prototype-selector captures, not yet the final default implementation identity.

The lead reports the bounded implementation as one TerrainRequestKeys cached/coherent preference and one allocation call site, with the original compatible fallback, all30 policy cases passing Debug/Release and native/managed builds passing. Ordinary queue polling, writes, fences, publications and the M13.5 GPU-local roles remain the required contract. The separately labeled implemented-candidate section below records available default timings and parity without replacing these prototype populations. Normal route/suite completion, deployment verification and lifecycle negatives are finalized by the lead. No further candidate exploration is recommended.

## Reproduction

Run `python -B docs/engineering-evidence/m13-final-exit/verify-b.py` after the bounded journals are complete. Both JSON and lossless gzip are supported. The complete journals remain the authority for every stored frame/identity; this file writes only verify-b.md/json.


## Actual default implementation measurements

These candidate-* journals are the implemented unbanked default candidate, never the banked M13.5 baseline. Quiet fixed runs use the normal deployed candidate native binary; separate CPU/dynamic profiles use the rebuilt private timing host without the cached-key selector or capture hooks. Their bank field records production ancestry. Earlier controls and return/repeat variance remain above.

| Candidate run | GPU median / P95 / P99 / peak | GPU >8.33 / >11.11 | Submission CPU median / P95 / P99 / peak |
|---|---|---|---|
| candidate-fixed-orbital | 0.82540 / 0.90936 / 0.99560 / 0.99940 | 0 / 0 | unavailable |
| candidate-fixed-factor1 | 3.52580 / 3.68496 / 3.72064 / 3.74624 | 0 / 0 | unavailable |
| candidate-fixed-florida | 8.95040 / 9.07908 / 9.12464 / 9.16432 | 100 / 0 | unavailable |
| candidate-fixed-grazing | 3.86328 / 3.97364 / 4.07700 / 4.08496 | 0 / 0 | unavailable |
| candidate-fixed-active-refinement | 4.70000 / 4.85920 / 4.87168 / 4.89700 | 0 / 0 | unavailable |
| candidate-fixed-inland | 8.05332 / 8.23944 / 8.30720 / 8.31748 | 0 / 0 | unavailable |
| candidate-cpu-florida | 8.93788 / 9.11884 / 9.13880 / 9.16920 | 100 / 0 | 9.76590 / 10.14640 / 10.30340 / 10.31290 |
| candidate-cpu-inland | 8.03832 / 8.21532 / 8.22704 / 8.32068 | 0 / 0 | 8.77230 / 9.07260 / 9.12690 / 13.01040 |

| Reference -> implemented candidate | GPU median change | CPU mean / median recovery | Input/hardware fields differing |
|---|---:|---|---|
| fixed-orbital -> candidate-fixed-orbital | 0.00324 | unavailable (separate quiet run) | none in logged sets |
| fixed-factor1 -> candidate-fixed-factor1 | -0.02352 | unavailable (separate quiet run) | none in logged sets |
| fixed-florida -> candidate-fixed-florida | -0.06628 | unavailable (separate quiet run) | none in logged sets |
| fixed-grazing -> candidate-fixed-grazing | -0.06764 | unavailable (separate quiet run) | refinedVertices |
| fixed-active-refinement -> candidate-fixed-active-refinement | -0.11284 | unavailable (separate quiet run) | refinedVertices |
| fixed-inland -> candidate-fixed-inland | -0.10452 | unavailable (separate quiet run) | none in logged sets |
| cpu-florida -> candidate-cpu-florida | -0.05900 | 1.75885 / 1.69360 | none in logged sets |
| poll-florida -> candidate-cpu-florida | -0.06092 | 1.70275 / 1.68250 | none in logged sets |
| poll-return-florida -> candidate-cpu-florida | 0.16732 | 2.00656 / 2.00440 | none in logged sets |
| cached-florida -> candidate-cpu-florida | 0.18504 | 0.12518 / 0.15260 | none in logged sets |
| cached-repeat-florida -> candidate-cpu-florida | 0.20172 | 0.27417 / 0.26120 | none in logged sets |
| cpu-inland -> candidate-cpu-inland | -0.09060 | 1.69864 / 1.69750 | none in logged sets |

| Dynamic reference -> implemented candidate | After-authority CPU mean / median recovery | Candidate CPU P95 / P99 / peak | Candidate GPU P95 / P99 / peak | >8.33 / >11.11 | Publication signature / cadence differences |
|---|---|---|---|---|---|
| dynamic-regional -> candidate-dynamic-regional | 2.26476 / 1.80890 | 12.31440 / 13.42870 / 21.07470 | 11.59208 / 12.49588 / 12.84380 | 541 / 44 | True / 0 |
| poll-regional -> candidate-dynamic-regional | 2.54833 / 2.07960 | 12.31440 / 13.42870 / 21.07470 | 11.59208 / 12.49588 / 12.84380 | 541 / 44 | True / 0 |
| cached-regional -> candidate-dynamic-regional | 0.98720 / 0.23440 | 12.31440 / 13.42870 / 21.07470 | 11.59208 / 12.49588 / 12.84380 | 541 / 44 | True / 0 |
| poll-return-regional -> candidate-dynamic-regional | 2.43346 / 1.97450 | 12.31440 / 13.42870 / 21.07470 | 11.59208 / 12.49588 / 12.84380 | 541 / 44 | True / 0 |
| dynamic-full -> candidate-dynamic-full | 1.52525 / 1.68600 | 7.99350 / 11.03990 / 50.44250 | 6.89472 / 7.93868 / 8.46020 | 2 / 0 | True / 0 |
| poll-full -> candidate-dynamic-full | 1.83334 / 2.01060 | 7.99350 / 11.03990 / 50.44250 | 6.89472 / 7.93868 / 8.46020 | 2 / 0 | True / 34 |
| cached-full -> candidate-dynamic-full | 0.27317 / 0.12410 | 7.99350 / 11.03990 / 50.44250 | 6.89472 / 7.93868 / 8.46020 | 2 / 0 | True / 0 |
| dynamic-warp -> candidate-dynamic-warp | 1.74304 / 1.75430 | 5.04740 / 6.58200 / 11.33190 | 3.66908 / 5.01572 / 5.14892 | 0 / 0 | True / 0 |

Dynamic summaries describe all strictly aligned Earth GPU frames and every host frame after initial authority. Identity/cadence differences are retained in JSON; these are not exact camera/physical-byte joins. The absence of a queue-only timer in the final host does not authorize deriving exact function cost from a mixed CPU scope. Full validation/readiness remains the lead report responsibility.

Default-implementation exact-output gates: florida PASS, inland PASS. Complete prepared64-byte records, oriented submitted triangle multisets and whole D32/HDR/image hashes are checked against retained pre-change digests. Their capture timings are excluded; no dynamic or individual post-TES-stream proof is inferred.

## Final performance red-team judgment

**PASS - bounded implemented CPU/display-frame responsibility.** The actual default candidate confirms median whole-CPU recovery of1.6936/1.6975 ms in Florida/inland and1.8089/1.6860/1.7543 ms in regional/full/warp versus the original fresh bank profiles. Fixed mean recovery is1.75885/1.69864 ms; full/warp means recover1.52525/1.74304 ms. The regional mean recovery2.26476 ms exceeds its non-fence mean recovery1.90955 ms, so it must not all be assigned to the key scan; fence/run-order variation contributes. The same-binary selector controls and narrow queue timer establish causal memory-placement ownership; these final runs establish default-implementation delivery.

The final quiet GPU medians range from.82540 ms orbital to8.95040 ms Florida. Florida remains100/100 >8.33; inland peaks8.31748. Final regional737 aligned GPU frames have median8.77008, P9912.49588, peak12.84380 and541 >8.33 /44 >11.11. Full has2/1698 >8.33; warp has none. Against initial bank fixed results, non-orbital medians increase.02352-.11284 ms; these small mixed changes and earlier return/repeat variability remain visible. No GPU performance improvement or universal8.33/120 FPS closure is claimed.

All final fixed camera/orientation/projection, generation, level, topology family, selected/TCS counts and factor-bin sets match the corresponding bank sets. Grazing and active-refinement hardware TES invocation sets differ slightly (medians3470003 ->3470004 and2779258 ->2779254); no exact TES-count claim is made. Full/regional/warp publication signatures and native cadence match their original bank profiles, with58/37/1 publications and no logged invalid publication/readiness/ownership records. Dynamic common-frame generation matches, but TCS/TES/fragment counts differ; no exact dynamic camera, physical-byte or raster parity claim follows.

The default rebuilt dynamic managed binary differs in hash from the original bank artifact and matches the candidate deployment. Quiet fixed native also matches the candidate deployment. Arguments and shader hashes match the reference; changing native/managed build identity is recorded, not described as a byte-identical runtime comparison. The controlled prototype comparison remains the same-binary causal evidence. Final parity independently records selected memory type3/flags14 without the prototype selector.

Frame stability is improved at typical and percentile levels, with residuals retained. Regional CPU after initial authority is9.4326 median /12.3144 P95 /13.4287 P99 /21.0747 peak; its lone >16.67 event is frame182, including14.8288 ms validation/upload work. Full is5.7498 /7.9935 /11.0399 /50.4425, with8 >16.67 and one >40. Full frame8 already uses generation1 after first authority:48.4895 ms fence wait dominates50.4425 ms whole CPU while completed GPU7 is.982 ms. No window recreation or Vulkan failure is logged. It is an early post-authority fence/scheduling outlier with exact external cause unclassified, not proven startup-only and not a50 ms terrain GPU job. Later full topology/upload tails remain up to25.3435 ms at frame347. The outlier is not removed from any distribution.

This one nonrecurring, unclassified wait does not establish a second worthwhile avoidable terrain owner and does not negate the repeated narrow CPU gain. It prevents a claim that every post-authority frame is below40 ms or that all stability debt is solved. The ticket stop rule supports returning the one implemented responsibility with these residuals rather than exploring another optimization. No additional performance candidate or GPU recapture is recommended; full functional validation and final banking readiness are the lead responsibility.