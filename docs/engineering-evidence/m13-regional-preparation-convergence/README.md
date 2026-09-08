# Final M13 regional preparation convergence

## Decision

**REJECT**

**COMBINED CANDIDATE REJECTED**

**M13 STILL NOT READY — ONE BOUNDED BLOCKER REMAINS**

The tested composition materially improves equivalent replacement work but does
not qualify for production or M13 closure. The original combined control reaches
9.316 ms mean, 12.180 ms P95 and 13.519 ms peak. Its one bounded mapped-address
revision reaches 9.141 ms mean, 11.305 ms P95 and 11.653 ms peak. Six normal return
frames still exceed 11.11 ms across the two returns. Four belong to the selected
44-frame band; two belong to the immediately preceding upscale.

This rejects qualification of the tested candidate. It does not establish that
exact reuse or device-local placement is architecturally invalid, that no further
gain exists, or that M13 closure is impossible. No second revision, new target,
production ABI change, M13.5 title or banking action was undertaken.

## Baseline and authority

- HEAD, `main`, `origin/main`, and dereferenced
  `m13.4-zero-contribution-terrain-material-noise`:
  `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
- Branch: `codex/m13-regional-preparation-blocker`.
- M13.3 remains `180eaf150ba5db6364e17dd48336690778f058f9`;
  its annotated tag is unchanged.
- GPU: AMD Radeon RX 6800 XT, Vulkan 1.4; native 3440 x 1440.
- Entry tracked and staged diffs were empty. The existing exit-assessment and
  regional-preparation-blocker evidence directories were untracked. This package
  adds the third untracked directory. `baseline.json` was captured after creating
  this package, so its status already includes that third entry.

Project Control authorized **exact physical reuse + device-local preparation
placement** inside one regional replacement/preparation responsibility. The
first composition was tested privately before any permanent implementation. The
one permitted revision expanded source-address discovery beyond the same index.
Terrain authority, tessellation, materials, scheduling and publication semantics
were preserved. All temporary tracked-file patches were restored byte for byte
after each private build; normal deployed binaries and all 49 shaders per
configuration were preserved.

## Reuse authority contract

The following is the required production contract, distinct from the narrower
proof provided by the private prototypes:

| Responsibility | Required identity / lifetime |
|---|---|
| Source | Actual completed published generation, topology family/hash/count, lattice, physical buffer and published reference frame |
| Destination | Frozen incoming generation, topology, lattice/count and preparation frame; it owns a separate working buffer |
| Direction | Exact GPU FP64 canonical direction words; topology coordinates or spatial proximity alone never authorize reuse |
| Basis and radius | Exact radius and all nine basis-vector component words; changed basis takes full recomputation |
| Physical domain | Immutable generation-4 H, oracle, ordered regional data identity, natural-terrain configuration and facility-support identity for the renderer lifetime |
| Regional dependencies | Incoming complete footprint remains required, including physical-normal sample support, even if every destination could be copied |
| Facility | Same immutable support definition and complete inputs; no change to support grading, geometry or H |
| Lifetime | Published source remains live while incoming slices run; no recycled/spare buffer is a physical cache |
| Readiness | Existing regional/facility readiness, all slices, final GPU fence and atomic frame-boundary publication remain mandatory |
| Fallback | Any failed validity, basis, direction, domain or source-address test executes the complete existing H/normal path |

The same-index prototype checks an active incoming job, valid current frame,
source range, radius, nine basis components and exact canonical direction bits.
Directions with a zero component conservatively recompute because the existing
current-preparation numeric equality does not distinguish signed zero. Test-only
catalog probes disable reuse. Source and destination publication generations are
expected to differ; matching generation numbers would be the wrong contract.

The renderer already treats physical assets as immutable: changing the bound
physical dataset identity is an error, rather than permission to reuse stale data.
No mutable asset/support API was introduced merely to manufacture a negative test.

The mapped revision tries same-index first, then a different candidate address,
then full recomputation. Its private native rational-coordinate lookup demonstrates
address feasibility only. Each candidate still passes the same exact GPU direction,
basis and radius predicate. The intended production design would use the existing
adjacent-level `ParentVertexMap` in the managed background preparation task:
parent-to-child for downscale source lookup, inverted for upscale destination lookup.
The map would remain an address hint, with explicit source/destination topology and
transaction identity, bounded storage, pinned transport and native ownership.

That production integration was **not implemented** after the GPU gate failed.
The private map stores low-32-bit generation values, but its shader consumes only
counts, not those generations or topology hashes. It is therefore not a qualified
production invalidation/transport contract. Its validity in the measured runs
comes from synchronous construction against the actual current/incoming buffers,
the observed owner lifetime and the independent physical-word oracle.

## Placement contract and KSA reference

Only the prepared physical working allocations change placement in C/D:
incoming/current/spare physical storage as existing ownership swaps move it,
and the current-pupil staging physical allocation. Lattice, indices, regional
catalog/residual storage, CPU control/readback and other graphics resources retain
their existing responsibilities.

Measured physical memory types:

| Control | Type | Properties | Heap |
|---|---:|---:|---|
| A/B | 1 | 6: HOST_VISIBLE + HOST_COHERENT | 0, system memory |
| C/D | 2 | 7: DEVICE_LOCAL + HOST_VISIBLE + HOST_COHERENT | 1, device local |

The mapped/coherent requirement preserves CPU initialization, CPU-authored
physical uploads and existing mapped inspection. No readback mirror, extra
transfer queue, `vkCmdCopyBuffer`, publication step or new synchronization scheme
was introduced. Incoming compute reads the current physical buffer and writes
the separate incoming buffer under the existing barriers and fences.

Observed physical working allocations total 135,719,808 bytes across three
allocations; three maximum-sized buffers would be 136,724,352 bytes. These are
existing working roles with changed placement, not additional copies. Regional
payload allocation remains 119,737,728 bytes against the existing 124,895,232-byte
maximum. Each control loads 670 records, reads 163,158,696 bytes and uploads
93,392,640 bytes.

A production allocator would explicitly name this buffer role and prefer a
compatible DEVICE_LOCAL + HOST_VISIBLE + HOST_COHERENT type, with a deliberate
compatible host-visible fallback where needed. It must preserve allocation and
device-loss errors appropriately. The prototype selects placement by private
allocation-label instrumentation; portable memory-type and allocation-failure
fallback qualification was not reached.

`ksa-reference-check.json` re-verifies all 27 previously cited local files by
SHA-256, including KSA 2026.9.7.5402, revision
`487c3f340de24c6a81037120b6d1129c045c5400`. KSA's persistent device-local prepared
position/normal buffers support the placement responsibility boundary. Its render
preparation does not supply NovaCore's exact physical-copy eligibility contract.
Its rolling collision-sample reuse illustrates retained sample ownership, but
does not waive NovaCore's H, reference basis, complete dependencies or atomic
publication requirements. No KSA source was copied. Detailed source references
remain in the preceding blocker package.

## Equivalent A/B/C/D measurements

The private host sanitizes external input, pauses at J2000 before the first
simulation advance, and starts the logical route after contributing regional
records are resident, the current owner is authoritative, and no preparation job
is active. It does not force publication, substitute a pupil, or bypass readiness.
Each route completes 1,000 logical frames and 37 incoming publications. Native
frame count includes approximately 103 warmup frames.

Every frame retains exact camera, GPU, presentation, lighting and current/incoming
pupil bytes. GPU frame N is correlated with its completion observation, not just
with a nearby log line. `analysis.json` and `analyze.py` preserve the selection and
nearest-rank percentile method.

The 44 selected logical frames are 345–355, 357–367, 725–735 and 737–747: two
L15→16 and two L16→17 jobs, 11 slices each. All 44 match across A/B/C/D and the
revision in camera, physical pupil geometry, topology, readiness/cursor and
TCS/TES workload. This addresses the earlier unmatched-history limitation.

| Mode | Transition mean | P95 | P99 | Peak | Incoming slice mean | Slice P95 | Slice P99/peak |
|---|---:|---:|---:|---:|---:|---:|---:|
| A: M13.4 baseline | 14.25709 | 14.94804 | 16.68492 | 16.68492 | 3.66919 | 3.84292 | 3.88944 |
| B: exact same-index copy | 11.33387 | 15.06132 | 17.05192 | 17.05192 | 0.73568 | 3.39636 | 3.92368 |
| C: placement only | 12.35854 | 12.83176 | 13.66388 | 13.66388 | 3.66868 | 3.83976 | 3.84716 |
| D: combined | 9.31583 | 12.17952 | 13.51932 | 13.51932 | 0.60854 | 3.37508 | 3.86704 |
| D: one mapped-address revision | 9.14063 | 11.30452 | 11.65300 | 11.65300 | 0.41877 | 1.94696 | 2.66216 |

All times are milliseconds. These are measured compositions, not sums of isolated
gains. D materially improves on both isolated controls, but its tail result does
not close the gate. The revision recovers the missed same-physical/different-index
values and still fails the normal replacement-frame gate:

| Logical frame | Revision total GPU | Incoming preparation | Residual full preparation in final slice |
|---:|---:|---:|---:|
| 343 | 11.19128 | 2.68064 | 34,396 vertices |
| 355 | 11.30080 | 1.94696 | 22,259 vertices |
| 367 | 11.65300 | 1.58516 | 15,496 vertices |
| 723 | 11.14040 | 2.64096 | 34,396 vertices |
| 735 | 11.30452 | 1.93840 | 22,259 vertices |
| 747 | 11.62920 | 1.58696 | 15,496 vertices |

These are recurring normal return frames, not startup or physical-oracle timings.
No further scheduling, slicing, dispatch-order or shader revision was attempted.

Across all 839 aligned Earth observations, D has median/P95/P99/peak
9.11688 / 9.32676 / 10.73156 / 13.51932 ms; the revision has
9.11576 / 9.26404 / 10.78252 / 11.65300 ms. The diluted traversal P99 cannot replace
the selected replacement-tail gate.

### Remaining comparison limits

A/B match all 1,000 input and physical events. C publishes one frame earlier at
logical 255, 263 and 270; D differs at 263 and 270. Their nearby incoming/cursor
events also differ. Those differences are outside the 44 selected targets and
are retained explicitly, not erased as merely ordinal pupil differences.

The mapped revision matches A's physical/input history for all 1,000 events and
TCS/TES counts for all 839 aligned Earth observations. Nevertheless its clipping
output and fragment statistics differ on 801 and 670 observations respectively.
The exact cause of those counters is not established here. No exact HDR, depth,
image or final-raster parity claim follows from input/physical equality.

## Physical oracle and negative controls

The diagnostic oracle always computes the original full destination result,
compares all 16 words against each proposed source, and records markers in the
otherwise zero reserved words. Host checks occur after the final fence and before
the unchanged ownership swap; markers are cleared before publication. Timings
from these expensive oracles are excluded from performance claims.

| Oracle | Prepared records | Proposed exact copies | Full fallback | Mismatching physical records |
|---|---:|---:|---:|---:|
| D same-index | 19,490,034 | 14,314,873 | 5,175,161 | **0** |
| Invalid-source flag | 19,490,034 | 0 | 19,490,034 | **0** |
| D mapped revision | 19,490,034 | 15,750,208 | 3,739,826 | **0** |

Every oracle covers 315 incoming slices and 37 publications. The revision admits
14,314,873 same-index and 1,435,335 additional mapped proposals. Each proposed
copy matches FP64 position/height, normal and reserved words. Fallback retains the
complete original preparation expression and operation order.

Reuse ratios are 73.4471% for D and 80.8116% for the mapped revision over each
complete oracle; the selected target jobs improve from 87.3217% to 95.3778%.

| Target job | Destination vertices | Same-index | Additional mapped | Full fallback |
|---|---:|---:|---:|---:|
| L15→16, generation 18 | 696,410 | 586,682 | 60,320 | 49,408 |
| L16→17, generation 19 | 712,106 | 643,258 | 53,152 | 15,696 |
| L15→16, generation 36 | 696,410 | 586,682 | 60,320 | 49,408 |
| L16→17, generation 37 | 712,106 | 643,258 | 53,152 | 15,696 |

Both returns repeat the per-slice counts. The added lookup supplies the exact
counterparts identified by the preceding completed-buffer investigation, now
with the missing full-word GPU proof for each mapped address.

Observed rejection counters (overlapping categories, not distinct vertex totals):

| Control | Changed center regional residual | Changed support weight/plane/gradient | Changed basis | Invalid accepted |
|---|---:|---:|---:|---:|
| D oracle | 2,086,246 | 90,504 | 1,545,742 | 0 |
| Mapped oracle | 1,455,731 | 41,818 | 1,545,742 | 0 |

The separate invalid-source probe sets the local published-frame validity to zero;
all 19,490,034 records recompute and all 37 publications complete. In this controlled
history, changed-basis generations 16, 20 and 29 admit no copies. Older prototype
generation numbers 3/15 are not reused as current negative-control identities.

These negatives do not establish same-direction dataset/support revision
invalidation, all normal-offset dependency identities, corrupted map-header
generation/hash rejection, cancellation/source replacement, pole/seam coverage
or recycled-buffer lifetime protection. The private map has no production
versioned transport contract. Those are missing qualification proofs, not observed
physical failures; the failed cheap GPU gate stopped production integration.

## Publication, CPU and bandwidth accounting

All observed incoming publications preserve current ownership, complete
dependencies, physical/normal readiness, the final fence and atomic frame-boundary
swap. Logs report zero missing, overlapping or stale owner, invalid draw,
incomplete publication, regional/facility failure, Vulkan error or crash.
This is bounded observed evidence, not a substitute for the gated full traversal.
D and its mapped revision also each complete four current-pupil staged
publications through the existing fence and atomic-swap path.

Job-start-to-publication elapsed time, including existing dependency/fence work:

| Mode | Generation 18 | 19 | 36 | 37 | Slices per job |
|---|---:|---:|---:|---:|---:|
| A | 178.920 | 188.757 | 177.487 | 187.128 | 11 |
| B | 149.170 | 154.856 | 148.110 | 155.786 | 11 |
| C | 158.000 | 166.497 | 157.480 | 166.037 | 11 |
| D | 127.826 | 133.819 | 125.328 | 130.606 | 11 |
| Mapped private revision | 124.651 | 129.879 | 124.820 | 131.269 | 11 |

The mapped row starts **after** its synchronous diagnostic map construction; it
is not total demand-to-publication latency. Managed demand, eligibility and actual
publication were not separately timestamped, so their split is not claimed.

Mean CPU milliseconds on the 44 preparation-submission frames:

| Mode | Update incl. wait | Fence wait | Callback | Validation/upload | Record | Submit | Total host |
|---|---:|---:|---:|---:|---:|---:|---:|
| A | 15.8440 | 13.6487 | 0.1212 | 2.0126 | 0.1810 | 0.0524 | 16.3521 |
| B | 12.9851 | 10.7145 | 0.1474 | 2.0604 | 0.1994 | 0.0446 | 13.5045 |
| C | 14.0063 | 11.8488 | 0.1194 | 1.9803 | 0.1936 | 0.0502 | 14.5326 |
| D | 11.0503 | 8.8104 | 0.1518 | 2.0262 | 0.1940 | 0.0457 | 11.5765 |
| Mapped private revision | 43.7829 | 8.7402 | 0.1618 | 34.8211 | 0.2198 | 0.0496 | 44.3516 |

This profiler is enabled equally in the comparison controls. Update includes the
listed child costs; the columns must not be added together. `analysis.json` also
retains completion-frame means and each component's P95/P99/peak.

Private map construction/upload costs are deliberately visible: 36 constructions,
8,910.7655 ms total; 247.5213 ms mean, 363.0001 ms P95, 370.2112 ms peak. This
synchronous diagnostic hash lookup is unacceptable production orchestration and
was never promoted. Its map payload is bounded to 712,106 indices plus a 16-byte
header, 2,848,440 bytes; cumulative map writes are 75,112,288 bytes. Temporary CPU
lookup allocations are released after each construction; their allocator-level
peak was not measured. A future production managed-map cache/pinning footprint
was not implemented or qualified.

Logical GPU physical traffic from the within-run oracle counts:

- D target jobs: 2,459,880 source records copied, 157,432,320 source bytes;
  357,152 full recomputations, representing 22,857,728 destination bytes.
- Mapped target jobs: 2,686,824 source records copied, 171,956,736 source bytes;
  130,208 full recomputations, representing 8,333,312 destination bytes.
- Every target destination is still written: 2,817,032 records / 180,290,048 bytes.
- Entire D oracle: 916,151,872 copy-source bytes. Entire mapped oracle:
  1,008,013,312 copy-source bytes. Every complete oracle writes 1,247,362,176
  destination bytes.

These are cumulative logical record bytes, not measured bus traffic or allocation
growth. No separate device-local transfer dispatch was added; source reads and
destination writes are inside the measured preparation slices. Memory initialization
and host upload costs remain included in CPU telemetry. Device-local mapped writes
do not imply zero hardware transfer cost.

## Validation and independent review

Completed in this ticket:

- Fresh private native/managed Release builds, zero warnings/errors; recorded
  native/managed hashes, complete shader hashes and exact temporary patches.
- Equivalent A/B/C/D controls and one mapped-address revision.
- Three full-word/negative oracle traversals, each 315 slices / 37 publications.
- Repeated Florida departures/returns, pupil and basis changes, surface-to-700-km
  orbit and non-Earth detours; observed L8–L17. Current ownership remains NCSM1.
- Strict canonical Khronos Vulkan validation in all bounded runs: no errors,
  VUID suppression, skips or crashes.
- Restored-source native/managed Release rebuild to remove diagnostic intermediate
  objects; normal deployed artifacts remained byte-identical.
- All 49 deployed shaders in each configuration and required production terrain
  assets verified against the initial hashes.
- Scratch cleanup followed by normal deployed Florida smoke: **PASS, 240 frames**,
  visible terrain, L17 physical readiness and zero owner/Vulkan failures.
- `git diff --check`: PASS. Tracked/staged diffs empty.

**Not reached because the cheap closure gate failed:** production implementation,
Debug candidate build, complete headless/Graphics/GPU/window/native suites,
15 launcher regressions, six route probes, L0–L17 full traversal, 1x/600x/7,776,000x
warp matrix, six fixed poses with 100 samples each, and exact depth/HDR/image
parity. No old banked result is reported as a fresh candidate pass. Paused J2000
is a controlled comparison condition, not time-warp validation. The current
banked fixed-pose matrix remains in the preceding exit report and was not rerun.

**VERIFY A — physical/publication:** independent reviewer verified zero full-word
mismatches, exact repeated fallback counts, and marker clearing before the existing
swap. Strongest attack: the diagnostic map does not consume explicit generation/
topology identity, lacks stale-map/lifetime negative controls and has no production
ABI/background transport. It cannot qualify production despite the measured oracle.

**VERIFY B — performance/placement:** independent reviewer reproduced the 44-event
matching, all revision tails and the large diagnostic map CPU cost. Strongest
attack: six ordinary return frames still exceed the standing budget; neither mean
nor overall traversal P99 removes that failure. Shader/source freshness is recorded;
exact raster parity remains unproven. An independent architecture reviewer agrees
with rejection of this tested candidate's qualification and stopping after one
revision. The reviewers performed no writes, builds or GPU runs.

## Evidence lifecycle, recovery and final state

The retained package has a 16 MiB upper budget and contains only this report,
lossless scalar/input journals, analysis, hashes, temporary-patch provenance,
reproduction source and cleanup/closeout records. Its continuing responsibility is
to let Project Control review the failed final-blocker gate without repeating the
GPU campaign or mistaking a passing mean for closure. No raw physical buffers,
depth/HDR archives, images or proprietary KSA source are retained.

`journal-manifest.json` records original/compressed sizes and SHA-256 identities
for all eight journals. Their 19,494,228 bytes are losslessly represented by
988,516 compressed bytes; uncompressed duplicates were removed. `analyze.py`
reads either representation and reproduces `analysis.json` without GPU work.

`disposable-manifest.json` identifies the 81 private-host/compiler/loader files
removed by guarded, exact-path cleanup. They represented 74,016,558 logical bytes,
including a 67,108,864-byte hard-link alias of the protected production elevation
asset. Exclusive disposable file content was 6,907,694 bytes; the original asset
and production caches remain unchanged. `storage.json` records final accounted
created/retained/disposed/remaining byte totals. These are file-inventory logical
bytes, not NTFS allocated-size or cumulative write-I/O measurements.

The current temporary directory is absent. Restored production source and normal
deployment were verified before and after the 240-frame smoke. No production
asset, generator, permanent test/fixture, cache, Git object, reference or historical
evidence package was removed.

For read-only reanalysis:

```powershell
python -B docs/engineering-evidence/m13-regional-preparation-convergence/analyze.py
```

For an intentionally authorized recapture, restore an isolated checkout of the
recorded baseline and deliberately adapt the archived paths/toolchain/deployment
assertions. Do not run archived mutation scripts blindly against a later checkout.
The sequence was `composition.py baseline`, `build`, A/B/C/D, `build-v2`,
D-oracle/D-invalid-source, then `mapped.py build`, `run`, `oracle`. The same
Release Florida configuration was used throughout:

```text
--scene=sol --focus=earth --surface-site=florida-launch
--physical-surface=m12d-natural-candidate --solar-epoch=j2000
--benchmark-frames=3000 --log=startup,validation,vulkan
```

The private harness quits at logical frame 1,000; the 3,000-frame argument is only
a safety cap. Build patches, environment, runtime and shader hashes are retained.
After recapture, restore/rebuild ordinary sources and retire only the reviewed
private output before a normal smoke. The closeout smoke instead uses the normal
deployed Release executable with the Florida configuration and
`--benchmark-frames=240`, without composition/mapping/profiling environment flags.

Final normal runtime:
`E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`.
Normal launcher remains
`E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`.
The launcher itself was verified present/hashed; the gated 15-case route suite was
not rerun. Full normal deployment/asset hashes are in `closeout.json`.

```text
?? docs/engineering-evidence/m13-exit-assessment/
?? docs/engineering-evidence/m13-regional-preparation-blocker/
?? docs/engineering-evidence/m13-regional-preparation-convergence/
```

Nothing staged, committed, pushed, tagged or banked. Stop for Project Control.
