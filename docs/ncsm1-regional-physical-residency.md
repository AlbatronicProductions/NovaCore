# NCSM1 regional physical-data residency migration candidate

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Current support/contact follow-up: [Florida facility-support physical contract](florida-facility-support.md)
preserves this residency architecture and P3 scheduling correction while making
the fixed facility contact plane part of canonical H. The numerical contact
gate passes; **Florida manual acceptance remains required**. The prior residual
and recommendations below describe the earlier investigation, not a new banking decision.

Current P3 follow-up: [bounded preparation and publication scheduling](ncsm1-regional-preparation-deblocking.md)
preserves complete regional authority and reduces the measured warm publication
frame median from 71.699 ms to 37.026 ms. **P3 RESOLVED — READY FOR
CONTACT-ARCHITECTURE WORK**, pending review/manual acceptance. The migration
results below are historical; the separate contact residual still blocks
convergence acceptance and this work remains unbanked.

2026-09-04. **UNBANKED. BLOCKED — remaining physical/render mismatch.**

The regional physical-data migration passes live numerical and ownership checks.
The former Classification D missing-data defect is corrected. The Florida seating
gate is not satisfied: at the south-edge midpoint the unchanged foundation lower
plane remains **0.692042 m above** the rendered factor-1 triangle. This is the
existing near-field/TES representation policy, with small base interpolation
error, rather than missing regional residency. Stop for review before changing
any pad, physical formula, tessellation range, or quality architecture. No manual
PASS, route banking, or renderer retirement is claimed.

## Baseline and preserved authority

Started from HEAD `7c41bb5d0317dc48290b359727031988145abb09` and the existing dirty
convergence, generation-4 foundation, and investigation candidate. Initial status,
patch and file hashes are saved under `build/regional-residency-migration/initial-*`.
P2S5G remains banked at `7bb03b0e635fa3444a4243bbad82758a1770ea60`, annotated tag
`m12d-p2s5g-surface-workload-efficiency`. This migration is an unbanked candidate.

Canonical generation-4 `H(bodyDirection)`, physical constants, site/object identity,
pad dimensions/origin/depth, ordinary launcher/CLI policy, NCSM1 topology, pupil
morphing algorithm, P2S5G interface compaction, culling, tessellation factors and
50 m TES range are unchanged by this migration. The compatibility renderer,
material cache, shaders/assets/tests, `ln -c Debug`, and root helpers remain.
The earlier investigation and KSA audit retain their historical observations.

## Old chain and causal failure

`PrepareLocalUploads -> QueueLocalRequests -> QueueAnchoredLocalRequests ->
LocalSectorRequiredForAnchoredPatch -> localIo requests/worker -> staged texture
uploads -> CompleteLocalUploads -> TryPromoteLocalVisibleTransaction ->
RebuildLocalLookup -> LocalTerrainElevationResidual`.

`QueueAnchoredLocalRequests` exits without submitted anchored patches. Ordinary
converged NCSM1 submits zero such patches, leaving requests and published layers
at zero. The shader then returned zero regional residual. The old 256-layer cache
also combines material and elevation residency; its resident-neighbor coverage
cannot represent canonical catalog coverage independently of loading state.
That compatibility path remains present, but NCSM1 physical preparation no longer
uses it as its regional physical-data owner.

## KSA reference decision

The completed [KSA architectural inspection](ncsm1-regional-residency-ksa-reference.md)
found body-owned physical inputs, separate height streaming, bounded background
acquisition/upload, completion before shader exposure, and content-version
invalidation. Those responsibility boundaries are adopted/adapted. KSA's
whole-body mip heuristics, eager local decals, coarse-height fallback and later
resnap do not establish NovaCore's strict regional completeness contract.
The implementation is independently written; no KSA code/assets/runtime dependency
was copied. The measured NovaCore footprint determines this service's capacity
and demand, rather than the old anchored material capacity.

## New demand, residency and publication contract

A native renderer-lifetime `regionalphysical::Residency` owns the immutable
regional catalog and asynchronous physical-record acquisition. Two small demand
compute entry points enumerate the actual current/incoming NCSM1 lattice in its
exact desired body-fixed pupil frame. They use the same canonical direction
reconstruction as preparation and include each vertex plus all four unchanged
9,774 m base-normal sampling directions. Finest present catalog record selection
uses the canonical FP64 relaxed-cube inverse; a bounded bitset deduplicates records.
No camera-screen heuristic, anchored submission, or material readiness is needed.

The complete catalog supplies neighbor/fade coverage before any payload is
resident. The regional sampler retains R16 precision, CPU-equivalent range
subtraction, interpolation and coverage weighting. A missing chosen record is
never intentionally replaced with zero or a coarser resident record. The host
completeness gate, not a shader NaN sentinel, enforces readiness.

The transaction is:

1. Key desired work by pupil, topology and publication generation within the
   immutable body/terrain/dataset/physical-generation identity.
2. Dispatch dependency enumeration without preparing physical vertices.
3. Wait for the frame fence; read its dependency mask and queue deduplicated I/O.
4. Read and verify records asynchronously. CPU completion alone is not readiness.
5. Copy at most eight residuals per frame into host-coherent GPU storage while
   the previous frame fence is complete. Publish catalog residency only after
   those copies. The existing host-write/shader-read barrier precedes preparation.
6. Prepare incoming physical geometry only when every required record is resident;
   require the same readiness plus the existing completed GPU fence/compaction
   before atomic incoming/current publication.
7. Keep outgoing current geometry authoritative while waiting. For a same-level
   desired pupil with unresolved inputs, retain the exact last prepared pupil;
   resume the existing pupil update once complete. Never repair incomplete
   published vertices as new records arrive.

The existing global bootstrap remains the initial owner until the first complete
NCSM1 generation is ready. The dependency delay extends this startup interval to
about 1.28 seconds in the recorded run; it is no longer the historical one-frame
interval. Initial visual handoff/pop still needs manual review. No incomplete
NCSM1 generation is published during that interval.

Physical residuals alone gate geometry. The existing atomic record SHA covers all
channels, so integrity I/O still reads/verifies material bytes and discards them;
material GPU publication/readiness is independent. This is a format constraint,
not a material residency dependency.

## Identity, reuse, invalidation and bounds

Identity includes body, terrain version, ordered sector identities, per-record
SHA-256 and physical ranges, and physical generation 4. Paths and GPU allocation
addresses are not physical identity. Dataset metadata is immutable during the
renderer lifetime. A changed identity requires a fresh cache and full renderer
physical generation; mismatches are rejected before new preparation. Hot dataset
reload/partial geometry repair is intentionally unsupported. Matching complete
physical input identity permits existing pupil/sample reuse and focus re-entry.

The catalog has 859 records; 189 parents are completely covered by finer catalog
records and cannot contribute under canonical finest-record selection. Initial
Florida L17 demand is 670 records, matching the pre-implementation CPU sample-set
measurement, including normals. Once all contributing records are resident, any
later pupil/level dependency set is provably complete; further enumeration is
skipped. This is demand-triggered complete-set reuse, not unconditional pack load.

- Hard catalog/request bound: 896 records; bitsets: 28 words each.
- One I/O worker; ready queue at most 8 records, plus one in-flight read; at most
  8 residual copies per frame (1,115,136 bytes). No per-frame dataset reload.
- Actual payload allocation: 119,737,728 bytes, a stable catalog-indexed address
  space; cap 124,895,232 bytes, checked against GPU storage-buffer limits.
- Only demanded payloads are read/uploaded. Sparse Earth demand still reserves
  the same fixed address space at first upload; this explicit memory tradeoff
  avoids relocation and keeps a simple immutable GPU lookup.
- Payload allocation remains a 4-byte placeholder before demand; non-Earth startup
  requests/loads zero. Catalog/demand metadata and the idle service are bounded.
- Complete records are retained for this immutable renderer lifetime (no eviction
  or reload thrash). Cache destruction stops the worker before freeing the pack.
- A larger future pack or incompatible GPU limit fails explicitly; no degraded
  physically incomplete fallback is authorized by this capacity policy.

## Live production evidence

Primary 3440×1440 Debug run:
`build/regional-live-tests/0c9f41bfa97247eeab0f0bb9507fe71f/`.
`runtime.log`, actual mapped-buffer `frame-*.json`, `footprint.json`, and
`comparison.json` retain the evidence. The permanent regression launches the
ordinary Solar/Florida executable, with an opt-in traversal/readback driver.
It reads actual published NCSM1 buffers and samples the production descriptor set;
the eagerly loaded CPU dataset is only its oracle. It does not substitute a
standalone GPU parity dataset for production residency.

| Florida quantity | Before migration | Corrected run |
| --- | ---: | ---: |
| Anchored patches required/submitted | 0 | 0 |
| Regional requests / misses | 0 / 0 | 670 / 670 |
| Regional loaded/published records | 0 | 670 |
| Explicit per-record cache hits | 0 | 0 (complete-set reuse bypasses requests) |
| Bytes read for record integrity | 0 | 163,158,696 |
| Residual bytes uploaded | 0 | 93,392,640 |
| Resident high-water mark | 0 | 670 |
| Demand GPU dispatches | 0 | 1 |
| Initial physical dependency delay | no gate | 1,276.400 ms |
| Worker read/decode/verify total | 0 | 401.947 ms |

Readiness is logged before the first NCSM1 publication. Logs show generation 4,
surface mode 2, terrain-v5, NCSM1, and zero anchored-patch dependency. Existing
current/incoming owner diagnostics report no missing/overlap/stale generation
counts; non-Earth focus removes Earth presentation authority.

## Exact Florida composition and rendered result

Body 6, object `0x4E435F464C5F5044`, latitude 28.6084°, longitude −80.6042°,
direction `(0.1433224599406355, 0.4788205718227514, 0.8661348234979923)`.

| Component (m) | Canonical CPU | Old live GPU | Corrected live GPU |
| --- | ---: | ---: | ---: |
| Signed global elevation | -3.181039712231420 | -3.181703128373114 | -3.181703128373114 |
| Regional residual | 15.001533002687239 | 0 | 15.001533002687239 |
| Composed geographic height | 11.820493290455819 | 0 | 11.819829874314125 |
| Prepared base height at exact direction | 16.598663016983820 | 4.778169726528001 | 16.597999600842126 |
| Unweighted near-field contribution | 2.288287203805286 | 2.288287203805288 | 2.288287203805288 |
| Full unweighted canonical composition | 18.886950220789107 | 7.066456930333289 | 18.886286804647412 |

The regional difference is zero at reported precision. The existing global oracle
FP32 UV path accounts for 0.000663416 m of global/full-height difference. This
migration does not change that global source/transport. The six component values
are diagnostic exact-direction queries on the live preparation descriptors;
actual vertex heights and the drawn triangle are checked separately.

At frame 740 the actual rendered factor-1 center height is 16.595315885 m,
versus old 4.778170083 m: the −14.108780 m failure becomes −2.291634 m relative
to full H. Actual prepared vertices across the captured footprint agree with
canonical base height within 0.001537 m (test bound 0.003 m). Max outer TES factor
is 1 and inner is at most 1. The unchanged foundation bottom is approximately
15.134892259 m, now 1.460424 m below the center rendered surface.

## 3×3 footprint and remaining classification

Frame 740, same 64×48 m footprint. Positive bottom-minus-render means clearance.

| East, north (m) | Canonical H (m) | Regional (m) | Rendered factor-1 (m) | Render − H (m) | Bottom − render (m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| +0, +0 | 18.886950221 | 15.001533003 | 16.595315885 | -2.291634336 | -1.460423626 |
| -32, -24 | 21.720461536 | 17.227548588 | 18.852449127 | -2.868012409 | -3.717431299 |
| +32, -24 | 19.915677805 | 16.298537164 | 17.897386282 | -2.018291523 | -2.762368455 |
| -32, +24 | 21.285736873 | 17.173789353 | 18.770114104 | -2.515622769 | -3.635096276 |
| +32, +24 | 19.544928310 | 16.360576468 | 17.902791905 | -1.642136405 | -2.767774078 |
| -32, +0 | 21.588542821 | 17.256906282 | 18.867545396 | -2.720997425 | -3.732572773 |
| +32, +0 | 20.286708836 | 16.861598598 | 18.444879883 | -1.841828953 | -3.309907260 |
| +0, -24 | 16.884125413 | 12.822126323 | 14.442895549 | -2.441229863 | +0.692041915 |
| +0, +24 | 17.402873129 | 13.747867367 | 15.379860086 | -2.023013043 | -0.244922621 |

All nine samples have TES near-field weight zero under the unchanged 50 m rule.
The smallest relevant triangle edge-midpoint distance is over 156 m. Canonical
near-field contributions are 1.616766–2.867232 m. After accounting for that term,
base representation error is at most 0.050202 m here; camera-relative FP32
transport contributes under 0.000003 m, and triangle chord sag under 0.000001 m.
The large former regional omission is gone. Nevertheless the south midpoint is
still visibly relevant geometric clearance, rather than a demonstrated seated
foundation. Classify the remaining blocker as **near-field physical/rendered
representation agreement at the structure**, not renewed Classification D.
No pad offset/depth or 50 m TES adjustment was made to hide it.

## Continuity, isolation and performance

The live regression passed 43 captures, 891 actual vertex comparisons, levels
8–17, and 37 distinct captured pupil identities. Retained/same-level pupil state,
snap, adjacent L16/L17, incoming/current replacement, retreat, re-approach, focus
away and Florida return preserve the regional contribution. Frame 740 contains
all nine footprint points after re-entry. Full canonical H is invariant; visual
movement/seating acceptance is not inferred from numerical tests.

A fresh Earth-away run at land/3,000,000 m requests and loads only 5 records,
with 5 subsequent hits, 1,171,913 bytes read, 696,960 bytes uploaded and two demand
dispatches. Whole-planet NCSM1 preparation still has a small Florida dependency
at that view; screen position does not erase actual physical sample requirements.
Four live GPU catalog queries outside coverage, including +Z on the Florida cube
face, all return no record. Fresh Mars/Saturn focus requests and loads zero;
Florida→Mars→Saturn→Florida uses retained Earth data without making it another
body's geometry authority.

GPU timestamps separate demand and physical preparation from draw. The primary
run's one-time demand dispatch cost 38.446410 ms. A separate stationary 200-frame
pair under `build/regional-residency-migration/timing-{old,corrected}/` used the
same L17 topology hash `13611079496444506427`, all 712,106 vertices and exact pupil
frame. An isolated runtime copy used the previous incomplete preparation SPIR-V;
the candidate runtime was not overwritten. Full incoming preparation was
9.740800 ms old versus 36.610400 ms corrected: **+26.869600 ms** in this single
pair. The added canonical regional lookup/normal sampling cost is real. This is
not a steady-frame optimization claim or a statistically established percentile.
Both paths reused preparation for the remaining stationary frames. The old path
is invalid physical output and is evidence only, not a selectable production mode.

Mixed traversal timings (different levels/work amounts; not an A/B speed claim):

| GPU work | Samples | Min ms | Median ms | Max ms |
| --- | ---: | ---: | ---: | ---: |
| currentPhysicalPreparation | 21 | 0.775400 | 1.863200 | 35.990000 |
| incomingPhysicalPreparation | 37 | 8.295120 | 23.497650 | 36.964760 |

No per-frame physical preparation or demand is added when the accepted pupil is
retained. The service still performs bounded catalog identity/readiness checks
and metadata publication on the host. Startup I/O/dependency delay, movement
preparation, draw workload and manual frame pacing remain separate acceptance
questions. No unsupported per-category baseline percentile is inferred.

## Files and validation

Migration implementation:

- `native/NovaCore.Native/RegionalPhysicalResidency.{h,inl}`: independent catalog,
  identity, bounded worker/cache, GPU demand/readiness and upload lifecycle.
- `native/NovaCore.Native/shaders/regional_physical.glsl` and both
  `regional_physical*_demand.comp` files: canonical catalog sampling and exact
  current/incoming physical demand.
- `NovaCoreNative.cpp`, `CMakeLists.txt`, and the Triangle `.csproj`: native
  lifecycle/descriptors/publication gate, shader builds/copy and GPU timing.
- Both `production_spherical_billboard*_prepare.comp` and
  `planetary_physical_authority.glsl`: use complete regional physical inputs in
  NCSM1 preparation, preserving the shared canonical physical formulas.
- `RegionalPhysicalProbe.inl`, `RegionalPhysicalResidencyTests.cpp`,
  `RegionalPhysicalResidencyTests.cs`, Graphics test registration and the existing
  `EarthRouteValidation.cs`: opt-in actual-path readback, acquisition/identity
  tests and controlled traversal/isolation. Ordinary controls remain unchanged.
- This report and active convergence/current-state/handoff/rendering references:
  current correction status, preserving historical investigation evidence.

## Completed validation and review boundary

- Debug and Release native builds: PASS. Debug and Release Triangle builds: PASS,
  zero managed build warnings/errors. Debug Graphics test build: PASS.
- Native `NovaCoreRegionalPhysicalTests`: PASS for immutable dataset/physical
  identity, deduplicated asynchronous acquisition, empty dependency sets,
  unresolved-input rejection, CPU-completion versus GPU-publication distinction,
  material separation and matching-record reuse.
- Permanent live Graphics regression: PASS again on the final candidate under
  `build/regional-live-tests/88ecbf3191894d11b183678f9461ebec/`. It repeats the
  800-frame 3440×1440 traversal, exact outside-record query checks, five-record
  fresh Earth-away run and zero-demand non-Earth run. 43 captures, 891 vertices,
  levels 8–17 and 37 captured pupils. Its dependency delay was 1,233.397 ms and
  demand GPU time 28.196640 ms; request/byte counts match the primary table.
- Existing focused Graphics regressions: Earth route convergence; Florida
  foundation seating; anchored Florida launch site; P2S5C production runtime and
  pupil/coverage continuity; P2S5G bounded interface; body eligibility/ownership;
  single canonical physical authority; Florida regional physical surface: PASS.
  An initial source-text draw-gate assertion caught the new demand call placed
  inside its expected dispatch prefix; moving demand immediately before that
  unchanged guard preserved the existing fixture and behavior. The rerun passed.
- The first isolation fixture incorrectly requested unsupported `--focus=mars`;
  corrected to existing Solar startup plus the opt-in focus driver. CLI behavior
  was not expanded. Corrected live regressions pass.
- VS/TCS/TES and ordinary/NCSM1 current/incoming culling source are unchanged;
  five directly checked billboard shader SPIR-V outputs also match the prior
  investigation build byte-for-byte. No NCSM1 topology assets were regenerated.
- Pre-existing pad, physical H, routing policy/CLI, launcher, renderer transport
  and original foundation/route tests match starting file hashes. Only the listed
  native integration, validation driver/registration and active documentation
  changed among pre-existing dirty files. Full preservation results are in
  `build/regional-residency-migration/preservation-results.json`.
- Vulkan: no new VUID in the live regression; the existing external Win32 memory
  import `VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645` remains. This is not a
  completely validation-clean run. No device loss or observed ownership failure.
- `git diff --check`: PASS (exit 0); only Git's existing line-ending conversion
  notices. The index is empty of changes; HEAD is unchanged. Nothing was staged,
  committed, pushed, tagged or banked.

Reproduce the live test after building the Debug sample and Graphics tests:
`dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll
"--test=Live NCSM1 regional physical residency"`. It opens controlled native test
windows and writes evidence below ignored `build/regional-live-tests/`.
Build/run the native test target through the configured MSVC CMake build, passing
its executable the installed production regional NCCUBE path.

**Recommendation: BLOCKED — remaining physical/render mismatch.** Regional
residency migration is implemented and its automated gate passes. The measured
0.692 m foundation clearance remains; manual terrain stability, initial handoff,
no float/excessive burial and re-entry seating have not passed for this correction.
Review that representation-level conflict before selecting another bounded ticket.
Do not bank route convergence or begin renderer retirement.

## Final git status --short

The full combined working tree includes both preserved prior work and this
migration; the files above identify this migration's responsibilities.

```text
 M README.md
 M docs/CODEX_HANDOFF.md
 M docs/M12D-P2S5G-workload-investigation.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/architecture.md
 M docs/planetary-rendering.md
 M native/NovaCore.Native/CMakeLists.txt
 M native/NovaCore.Native/NovaCoreNative.cpp
 M native/NovaCore.Native/shaders/planetary_physical_authority.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_incoming_prepare.comp
 M native/NovaCore.Native/shaders/production_spherical_billboard_prepare.comp
 M samples/NovaCore.Triangle/NovaCore.Triangle.csproj
 M samples/NovaCore.Triangle/Program.cs
 M src/NovaCore.Graphics/FloridaLaunchSite.cs
 M src/NovaCore.Graphics/PlanetaryPhysicalSurface.cs
 M src/NovaCore.Graphics/RenderObject.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tests/NovaCore.Launcher.Tests/Program.cs
 M tools/NovaCore.Launcher/ScenarioCatalog.cs
?? docs/earth-route-convergence.md
?? docs/florida-generation4-seating.md
?? docs/florida-physical-rendered-agreement.md
?? docs/ncsm1-regional-physical-residency.md
?? docs/ncsm1-regional-residency-ksa-reference.md
?? native/NovaCore.Native/RegionalPhysicalProbe.inl
?? native/NovaCore.Native/RegionalPhysicalResidency.h
?? native/NovaCore.Native/RegionalPhysicalResidency.inl
?? native/NovaCore.Native/RegionalPhysicalResidencyTests.cpp
?? native/NovaCore.Native/shaders/regional_physical.glsl
?? native/NovaCore.Native/shaders/regional_physical_demand.comp
?? native/NovaCore.Native/shaders/regional_physical_incoming_demand.comp
?? samples/NovaCore.Triangle/EarthRoutePolicy.cs
?? samples/NovaCore.Triangle/EarthRouteValidation.cs
?? samples/NovaCore.Triangle/SampleOptions.cs
?? tests/NovaCore.Graphics.Tests/EarthRouteConvergenceTests.cs
?? tests/NovaCore.Graphics.Tests/FloridaFoundationSeatingTests.cs
?? tests/NovaCore.Graphics.Tests/RegionalPhysicalResidencyTests.cs
```
