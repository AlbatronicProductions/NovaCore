# Verify B: whole-frame placement performance red team

## Actual implementation verification — current result

**Actual production implementation cheap performance signal: PASS.** The default implemented buffer-role policy, with no placement environment flag and all49 production shader hashes unchanged, retains meaningful net benefit. Correctness, portable fallback and full-suite qualification are owned by the lead; this performance result is not banking permission.

Candidate native SHA-256: d0d53126156f3ebb5598d031545928bc714e517de69fd167a2586237fadfe1b5. Candidate fixed Florida total median is9.19036 ms (mean9.181676, P959.38676, P999.47840, peak9.59528). All100 fixed samples remain above8.33, none above11.11. Its median gain is1.87820–2.19040 ms against the three bracketing baseline runs. Against the private local repeat it is0.05196 ms slower by median (0.069943 ms mean), within the measured ancillary/run-order variability.

All1,000 candidate route inputs, full published/incoming physical-frame bytes, topology/generations, cursors and publication cadence match the private local control. All839 aligned TCS/TES observations also match that private control. Raw clipping/fragment fields still differ; no output parity is inferred here.

Against the original baseline, the same29 input-state mismatches and four current-generation changes remain, so810 exact-input observations and the full839 descriptive route remain separate. All64 target replacement slices are exact input/hardware matches.

| Actual candidate population | Mean | Median | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|---:|
| all839 | 8.603927 | 8.95292 | 11.66481 | 12.74396 | 13.28179 | 659 | 62 |
| all810ExactInputs | 8.651295 | 8.96368 | 11.68728 | 12.74396 | 13.28179 | 654 | 62 |
| target64 | 12.011168 | 11.95792 | 13.01519 | 13.28179 | 13.28179 | 64 | 61 |
| settled | 8.414291 | 8.95716 | 9.09212 | 9.15084 | 9.46100 | 422 | 0 |
| FloridaStart | 8.947332 | 8.97704 | 9.11136 | 9.15052 | 9.15084 | 119 | 0 |
| firstReturnSettled | 9.003761 | 8.99600 | 9.37280 | 9.46100 | 9.46100 | 30 | 0 |
| secondReturnSettled | 8.943129 | 8.97080 | 9.09972 | 9.14692 | 9.15596 | 250 | 0 |

The actual target64 mean recovery is2.423740 ms, with median paired recovery2.40280 ms. Its minimum per-frame recovery is1.37978 ms: the >=1.5 ms claim applies to the complete regime, not every sample. The local candidate still has64/64>8.33 and61/64>11.11. The earlier private control had54>11.11; do not present that older count as the actual implementation result.

Actual versus private local-control mean total differs by+0.008900 ms over839 frames and+0.027967 ms over target64. Their similar mean does not erase the seven additional near-threshold11.11 misses.

Actual target64 completion CPU mean is14.857475 ms (P9515.8781, peak16.3689), against baseline17.278778 ms. Target64 submission non-fence mean is2.914339 ms, against2.906838 (+0.007502 ms). Normal1000 CPU mean/P99/peak are10.498668/15.6843/23.4730 ms, with no>40 ms sample. This remains slower than120 Hz wall-clock operation and does not claim the baseline mixed-scope transition stall is permanently eliminated.

The actual candidate preserves the six target first/last preparation and first-published logical frames. No calibrated per-job wall-clock latency is invented. The JSON adds actual candidate publication schedules, complete target timing/CPU rows and all fixed comparisons while preserving the earlier private-control evidence.

**Performance continuation gate remains satisfied in expensive Florida/regional workloads.** Root-owned exact-output, allocator failure/fallback, ownership/lifetime and full regression gates must pass before final candidate acceptance. No material-noise approximation, tessellation change, reuse map or incoming-validator revision is part of this recommendation.

---

## Retained private-control verification

The following section records the preceding private control. Its numeric values are historical within this ticket; the actual implementation values above are current.

**The private bulk-buffer placement control has a meaningful performance signal; production correctness and portability remain separate gates.** All 64 representative replacement slices are matched by full logged input/physical frame and TCS/TES identity. Their mean total GPU drops 14.434908 to 11.983201 ms, a 2.451707 ms recovery. Matching settled Florida groups recover about 2.36–2.42 ms. These gains exceed the new 1.5 ms continuation bar in the expensive Florida/regional regime without relying on output-changing noise controls or old reuse/validator changes.

This is not a claim of 8.33 ms closure: all64 local transition frames still exceed 8.33 ms and 54 exceed 11.11 ms. Whole-frame CPU also remains above 8.33 in these events. The new payoff rule permits a useful candidate with explicit residual limitations; it does not permit reporting those limits as passed.

## Matching and contamination checks

- Both production-semantics journals finish 1,000 logical route frames after readiness, 1,103 native frames and 37 publications, with no recorded Vulkan errors. Shader hashes match. The private native code, managed driver and all runtime inputs are retained in JSON.
- Strict GPU alignment: positive gpuFrame == geometryFrame == host.frame−1, plus extended GPU record frame/total equality. There are 839 aligned Earth pairs. Non-Earth/stale identity samples are not silently promoted to new terrain frames.
- All64 target frames have identical logged camera/GPU/presentation/lighting/current and incoming pupil bytes, topology/generation, cursor/activity/dependency fields, and hardware TCS/TES counts. Physical output parity itself is not supplied by this timing journal.
- The full route is not wholly equivalent. Incoming cadence differs on29 aligned logical states between239 and270; current generation/topology/published pupil differ at246/255/263/270. TCS/TES differ only at those four current-generation transitions. The exact full-input/hardware subset contains810 observations and is reported separately.
- Clipping-output and fragment counts differ on804/672 aligned observations. They are retained as an unresolved limitation; no pixel parity or fixed fragment workload is inferred.
- No copy-map reuse, alternative incoming validator or two-tick reference scheduler is enabled. The moved allocation roles are prepared physical and staging physical, immutable lattice/index storage, visibility output and compacted index output. HOST_VISIBLE/HOST_COHERENT remain required.

## Complete population timings

Milliseconds; nearest-rank percentiles. All tails remain in each named population. The all839 route is descriptive because of the29 differing event states; the exact810 and complete64 rows supply the stricter joins.

| Population / mode | Mean | Median | P95 | P99 | Peak | >8.33 | >11.11 |
|---|---:|---:|---:|---:|---:|---:|---:|
| all839 A | 10.904499 | 11.30835 | 14.03941 | 15.38228 | 17.49109 | 739 | 555 |
| all839 B | 8.595027 | 8.96712 | 11.71648 | 12.92016 | 13.26652 | 654 | 54 |
| all810ExactInputs A | 10.973594 | 11.32151 | 14.07558 | 15.38228 | 17.49109 | 714 | 554 |
| all810ExactInputs B | 8.645353 | 8.97668 | 11.74140 | 12.92016 | 13.26652 | 651 | 54 |
| target64 A | 14.434908 | 14.25262 | 15.60328 | 17.49109 | 17.49109 | 64 | 64 |
| target64 B | 11.983201 | 11.96124 | 13.00449 | 13.26652 | 13.26652 | 64 | 54 |
| settled A | 10.724737 | 11.30587 | 11.75313 | 11.88544 | 12.14301 | 426 | 347 |
| settled B | 8.428257 | 8.96832 | 9.15904 | 9.35284 | 9.45368 | 421 | 0 |
| FloridaStart A | 11.380280 | 11.36792 | 11.81357 | 11.92117 | 12.05074 | 119 | 102 |
| FloridaStart B | 8.958507 | 8.98224 | 9.10540 | 9.22276 | 9.34432 | 119 | 0 |
| firstReturnSettled A | 11.366819 | 11.37810 | 11.73244 | 12.14301 | 12.14301 | 30 | 27 |
| firstReturnSettled B | 8.954857 | 8.94608 | 9.16584 | 9.28432 | 9.28432 | 30 | 0 |
| secondReturnSettled A | 11.342512 | 11.33116 | 11.75514 | 11.87693 | 12.09229 | 250 | 201 |
| secondReturnSettled B | 8.985007 | 8.99812 | 9.19044 | 9.37192 | 9.45368 | 250 | 0 |

A is banked memory placement; B is the private bulk-local placement control. Settled requires no active incoming job and zero current/incoming preparation or demand in both samples. The full-input exact subset does not replace or conceal the descriptive all839 aggregate.

## All six replacement events

| Incoming generation | Logical range | A mean / P95 / peak | B mean / P95 / peak | Mean recovery | B >8.33 / >11.11 |
|---:|---|---|---|---:|---|
| 17 | 334–343 | 13.660479 / 15.38228 / 15.38228 | 11.167727 / 11.71648 / 11.71648 | 2.492752 | 10 / 5 |
| 18 | 345–355 | 14.315888 / 15.25676 / 15.25676 | 11.924482 / 12.60786 / 12.60786 | 2.391406 | 11 / 11 |
| 19 | 357–367 | 15.356719 / 17.49109 / 17.49109 | 12.804259 / 13.08454 / 13.08454 | 2.552460 | 11 / 11 |
| 35 | 714–723 | 13.536897 / 15.37616 / 15.37616 | 11.124141 / 11.43465 / 11.43465 | 2.412756 | 10 / 5 |
| 36 | 725–735 | 14.243208 / 15.60328 / 15.60328 | 11.990209 / 12.43428 / 12.43428 | 2.252999 | 11 / 11 |
| 37 | 737–747 | 15.344215 / 17.36884 / 17.36884 | 12.736157 / 13.26652 / 13.26652 | 2.608057 | 11 / 11 |

The smallest per-frame recovery across the complete64 is1.72944 ms, with no local GPU regression inside that matched set. The all839 route does contain negative paired differences where event history changes; those are retained rather than omitted.

## Where the time changes

| Population | Total mean recovery | Candidate span recovery | Broad compute recovery | Current cull recovery | Current compact recovery | Tone-map recovery |
|---|---:|---:|---:|---:|---:|---:|
| all839 | 2.309472 | 0.306806 | 1.902969 | 1.401753 | 0.343121 | -0.021197 |
| all810ExactInputs | 2.328241 | 0.310668 | 1.915080 | 1.419225 | 0.342760 | -0.021229 |
| target64 | 2.451707 | 0.262728 | 2.165159 | 1.513176 | 0.302270 | 0.071221 |
| settled | 2.296479 | 0.293238 | 1.853171 | 1.480478 | 0.336276 | -0.026463 |
| FloridaStart | 2.421773 | 0.303761 | 1.918467 | 1.547468 | 0.336125 | -0.020228 |
| firstReturnSettled | 2.411962 | 0.316048 | 1.916823 | 1.545805 | 0.335637 | 0.012124 |
| secondReturnSettled | 2.357505 | 0.292782 | 1.916257 | 1.545487 | 0.335744 | -0.032184 |

For target64, current cull improves1.513176 ms and current compact0.302270 ms on average; the whole compute span improves2.165159 ms. This is a GPU memory-placement signal affecting ongoing current work and some incoming final/readiness work, not exclusively faster H evaluation.

Preparation and current/final blocks are nested in broad compute. Candidate spans VS/TCS/TES/raster/fragment. Background/materials/detailed/scene intervals overlap and are not summed. Stage differences above identify where timing moves; adding the child columns to the broad-compute or candidate columns would double-count.

Fixed-run ancillary variance remains material: tone-map and background spans change with run order. The placement claim relies on total GPU recovery repeating in Florida and in the full regional traversal, alongside the directly measured current cull/compact reduction; it does not attribute every fluctuating ancillary microsecond to placement.

## CPU critical path and stalls

| Population / mode | Completion CPU mean / P95 / peak | Submission non-fence mean / P95 |
|---|---|---|
| all839 A | 13.916109 / 17.09080 / 51.94750 | 3.061150 / 3.30630 |
| all839 B | 11.483044 / 14.54590 / 23.64740 | 2.978367 / 3.52300 |
| target64 A | 17.278778 / 18.92620 / 20.40750 | 2.906837 / 3.25740 |
| target64 B | 14.902853 / 16.41350 / 16.54930 | 2.953670 / 3.60720 |
| settled A | 13.744186 / 14.69480 / 26.65390 | 2.818120 / 3.13450 |
| settled B | 11.317296 / 12.42820 / 19.12980 | 2.939971 / 3.49970 |

Target64 completion CPU mean improves17.278778 to14.902853 ms. Submission non-fence mean rises2.906838 to2.953670 ms (+0.046833 ms), while fence/whole-frame duration improves. This is a GPU/fence-led net benefit, not zero CPU cost or proof of120 Hz CPU scheduling. All acquire/record/submit/present/inspection/update/unattributed components are retained in JSON.

The whole normal1000-frame host means are12.570173 ms A and10.524078 ms B; P99 values are18.9262/16.1532 ms. The baseline has a normal-route, non-startup51.9475 ms host event at native285/logical182. Its mixed validationUpload scope is41.7361 ms, fence8.5623 ms, recreation0. The same local frame is23.6474 ms, validationUpload17.1974 ms and fence5.1891 ms. The internal cause within the mixed upload/validation/creation scope is not isolated. Do not erase this event as startup or promise it has been permanently eliminated.

## Publication and memory lifetime limits

All37 publication records report physical/normals/cull/compact/TES/indirect readiness and completed fence. The target six jobs retain exactly the same slice counts and first published logical frames. No calibrated request-to-publication wall timestamp is emitted by this instrumented host.

| Generation | First/last preparation | First published | A / B summed observed host-frame ms through publication |
|---:|---|---:|---|
| 17 | 334–343 | 344 | 178.2695 / 149.5983 |
| 18 | 345–355 | 356 | 201.6666 / 172.3709 |
| 19 | 357–367 | 368 | 215.4227 / 184.7260 |
| 35 | 714–723 | 724 | 175.1842 / 150.5308 |
| 36 | 725–735 | 736 | 200.0526 / 172.8535 |
| 37 | 737–747 | 748 | 213.0274 / 188.3102 |

These are sums of measured host frame durations, explicitly including the first published frame. They omit unmeasured inter-frame gaps and do not define a new precise publication latency or GPU-to-CPU clock mapping. They show no hidden many-frame publication delay in the selected events.

Both heap ledgers contain27 named allocation events totaling388,836,032 logical bytes for the moved roles. This is an allocation-event sum, not peak simultaneous residency or extra memory. Correct production capacity/lifetime accounting and fallback under device-local allocation failure remain required. Nothing in these timings qualifies device-loss handling or a broad catch-and-retry allocation policy.

## Fixed-regime continuation nuance

Florida bulk-local total medians repeat8.88184/9.13840 ms against current bracketing baselines11.06856/11.19293/11.38076 ms. Inland reaches8.21552 ms versus9.38668 initially and9.72476 on return: its1.171–1.509 ms median gain is baseline-order dependent. Active and grazing benefit but begin below the target. The clear meaningful payoff belongs to Florida and complete regional replacement work; do not claim every regime independently saves>=1.5 ms.

Noise-live terrain sensitivity is large but deliberately changes the material field. It is not a competing proven safe optimization. Physical-only memory placement is narrower and has smaller fresh benefit. The proposed coherent responsibility must accurately name all moved GPU-dominated resident/work buffers; it cannot use physical-only evidence to disguise a broader change.

## Performance judgment

**Cheap performance signal: PASS for meaningful recovery in the worst Florida/regional regime.** No shipping or banking recommendation follows until the root-owned correctness/portability and full validation gates pass. Residual >8.33 and >11.11 transition counts, CPU wall time and the unrelated normal-route host outlier remain visible.

The source/input join does not replace prepared physical-word, oriented-geometry, depth/HDR/image parity, owner/lifetime negative tests or validation. Run-order bias and clipping/fragment variability remain explicit; no broader material/TES change is justified by this performance result.

## Reproduction and read-only boundary

Read the lossless regional-base/regional-local journals (plain JSON or gzip) and their source hashes. Decode the columnar host/GPU rows; correlate by native identity and exact GPU total; attach logical frames by ordered Composition route markers; compare all logged fields before selecting exact populations. Target64 is the six complete ranges334–343,345–355,357–367,714–723,725–735,737–747.

work-a.json contains fixed comparisons and this ticket’s split scopes. verify-b.json retains all64 full rows, all839 compact timings, every mismatch, allocation records, publication logs, per-job slice schedules and separate CPU components. No production edits, builds, GPU runs, captures or cleanup were performed by this verifier. Only the authorized evidence files were written.
