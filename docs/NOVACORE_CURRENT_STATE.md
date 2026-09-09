# NovaCore current engineering state

This is the primary current-state and architecture-handoff document. Durable
engineering rules live in [ENGINEERING_RULES.md](../ENGINEERING_RULES.md), and
the detailed renderer contract lives in
[planetary-rendering.md](planetary-rendering.md). Historical milestone records
remain useful provenance but do not override this file.

NovaCore is a deterministic C# space-simulation core with a native Vulkan
renderer. Simulation owns celestial identity, exact time, physical state, and
reference-frame transforms. Graphics consumes immutable root-resolved snapshots
and performs FP64 camera-relative subtraction before GPU transport.

## Banked production checkpoint

M14.2 — Establish independent spacecraft translational authority is **PRODUCTION
ACCEPTED but UNBANKED** on `codex/spacecraft-translation`. The transient allocation
gate is resolved as CLR background-GC allocation-context accounting. A test-only
measurement boundary preserves the exact-zero contract; repeated Debug/Release
validation passed. Manual disposal of classified diagnostic copies remains before
banking; see [attribution and closeout](engineering-evidence/spacecraft-translation/allocation-attribution.md).
No M14.2 tag was created. M14 — Surface Interaction / Launch Foundation remains
**OPEN**. Manual visual acceptance is **NOT REQUIRED**; there is no intended visible
production-route change.

Latest banked accomplishment: **M14.1 — Provide immutable canonical surface-point queries
for spacecraft contact**, commit
`5b9b02ab1d2ba0e903809510908e4819984d4de2`, annotated tag
`m14.1-canonical-surface-point-queries`. M14.1 remains closed.
See the [query contract](surface-point-queries.md).

M14.2's existing spacecraft store supports immutable
mass, FP64 inertial-root COM position/velocity, analytic constant-net-force
advancement and canonical exact-time force transactions. Existing rotation and
derived frame extraction share the same craft identity. See the
[translation contract](spacecraft-translation.md) and
[accepted evidence](engineering-evidence/spacecraft-translation/README.md).
Contact response, grounded state, pad collision and launch/landing are not
implemented. No visible route changes. The next production responsibility returns
to Project Control; no subsequent milestone work is authorized here.

### Closed M13 renderer baseline

Latest banked M13 production milestone: **M13.6 — Prefer CPU-cached memory for
terrain residency keys**.

- Commit: `90fef759243dd67918cd556e19027159e5a5eada`.
- Annotated tag: `m13.6-cpu-cached-terrain-residency-keys`.
- Project Control decision: **PASS — M13 CLOSED**.
- Front: **M13 — NCSM1 Terrain Performance**.
- **There is no M13.7.** M14 work retains this renderer baseline.

M13 contains six banked production accomplishments:

| Step | Responsibility | Engineering evidence |
|---|---|---|
| M13.1 | Optimize NCSM1 TES hot path | [TES geographic-address specialization](engineering-evidence/tes-address-removal/README.md) |
| M13.2 | Specialize ordinary terrain shading | [Ordinary shading](engineering-evidence/m13.2-terrain-shading/README.md) |
| M13.3 | Prepare physical terrain before tessellation | [Prepared terrain responsibility](engineering-evidence/ksa-terrain-convergence/README.md) |
| M13.4 | Skip zero-contribution terrain material noise | [Material contribution evaluation](engineering-evidence/post-m13.3-next-target/README.md) |
| M13.5 | Prefer local GPU memory for terrain working data | [Working-data placement](engineering-evidence/m13-final-whole-frame-causality/README.md) |
| M13.6 | Prefer CPU-cached memory for terrain residency keys | [Final M13 performance evidence](engineering-evidence/m13-final-exit/README.md) |

M13 closed because no remaining terrain responsibility demonstrated one clear
causal owner with a bounded, quality-preserving recovery of at least 1.5 ms,
sufficient confidence and an acceptable architecture. Remaining cost is required,
mixed, below that bar, insufficiently isolated, or belongs to rejected
non-production mechanisms. Closure does not claim universal 8.33 ms performance.

### Accepted route ownership

**NCSM1 / New Earth Renderer** and generation-4 physical terrain serve all six
supported Earth/Solar/Florida launcher routes. Scene, focus, altitude, camera and
Florida initialization remain route-specific. The accepted Florida presentation
and surface/orbit continuity are preserved. Terrain-v5 global bootstrap remains
only until complete NCSM1 publication; non-Earth presentation stays independent.

[Production consolidation](production-consolidation.md) records the banked
P2S5H route convergence. The [P2S5G investigation](M12D-P2S5G-workload-investigation.md)
records the earlier tessellation-interface compaction and accepted +89 invocation
accounting difference. These are historical engineering coordinates; M13.6 is
the current production checkpoint.

### Final accepted performance envelope

Reference device: RX 6800 XT, native 3440×1440. The latest retained
implemented-default M13.6 GPU measurements are milliseconds. Fixed Florida uses
100 warm samples; traversals retain aligned Earth GPU frames. These populations
are distinct from capture runs and from older prototype measurements.

| Regime | Median | P95 | P99 | Peak | Frames above 8.33 / 11.11 ms |
|---|---:|---:|---:|---:|---|
| Fixed Florida | 8.95040 | 9.07908 | 9.12464 | 9.16432 | 100 / 0 of 100 |
| Florida regional departure/return | 8.77008 | 11.59208 | 12.49588 | 12.84380 | 541 / 44 of 737 |
| Full surface–orbit–return | 4.75400 | 6.89472 | 7.93868 | 8.46020 | 2 / 0 of 1,698 |
| Warp | 3.46220 | 3.66908 | 5.01572 | 5.14892 | 0 / 0 of 745 |

Regional replacement overlapping the current detailed draw remains the principal
measured residual limitation. Its peak exceeds 8.33 ms by 4.51380 ms and 11.11 ms
by 1.73380 ms, leaving 3.82620 ms to 16.67 ms. That margin does not establish a
future full-game budget. The standing targets remain 8.33 ms for the current
terrain foundation, preferred 6.67–6.94 ms headroom, 11.11 ms for the preferred
fully featured game and a 16.67 ms Ultra/max sustained floor.

M13.6 recovered approximately 1.69–1.81 ms median CPU-frame time across measured
workloads, not universal GPU time. Regional CPU median/P95/P99/peak after first
authority are 9.4326/12.3144/13.4287/21.0747 ms. Mixed upload/inspection tails
remain; full traversal retains a 50.4425 ms early post-authority frame dominated
by 48.4895 ms fence waiting while its completed GPU frame is about 0.982 ms.
Its initiating cause is unclassified. CPU intervals include waits and must not
be added to GPU timings. See the [final measurements and limitations](engineering-evidence/m13-final-exit/README.md)
and [independent verification](engineering-evidence/m13-final-exit/verify-b.md).

### Historical LiveKernelEvent 141 disposition

**UNRESOLVED / NOT REPRODUCED / RESIDUAL UNCERTAINTY ACCEPTED.** One historical
manual run produced a whole-system/display failure associated with Windows
LiveKernelEvent 141. No root cause was proven. A later representative M13.6
manual run did not reproduce it; Project Control accepted the residual
uncertainty. This is not a claim that it was fixed or caused by a driver,
hardware, FPS, or M13.6. Detailed evidence remains in the
[manual-failure](engineering-evidence/m13.6-manual-failure/README.md) and
[offline-incident](engineering-evidence/m13.6-offline-incident/README.md) records.
Their original pending classifications describe the investigation stage.

## Production planetary responsibility chain

```text
canonical body-fixed physical terrain H(bodyDirection)
→ immutable persistent NCSM1 scale resource
→ retained and snapped pupil representation
→ full physical relief and normal preparation before TES
→ conservative curved-patch planet occlusion
→ conservative screen/frustum visibility
→ compacted GPU workload
→ KSA-parity per-edge TCS factors and TES interpolation of prepared terrain
→ indexed indirect raster
→ fence-complete atomic publication
```

The accepted NCSM1 topology library contains 18 immutable levels, L0 through L17.
Projected geometric error selects a level, and hysteresis prevents unstable
scale toggling. The camera-facing pupil retains an exact signed cube-lattice
identity, snaps on deterministic thresholds, and reuses matching samples when
the pupil moves or changes level.

Managed code owns canonical FP64 identity and queries, selection, bounded
preparation scheduling, regional demand and immutable submission. Native Vulkan
owns persistent topology, full-relief render preparation, reusable working
buffers, conservative visibility/compaction, bounded tessellation and
interpolation, synchronization and fence-confirmed publication, HDR, reversed-Z
depth, and presentation. Ordinary shading specializes unused diagnostics and
skips material noise only when its contribution is zero.

Prepared physical storage, regional physical staging, lattice/index topology,
visibility output and compacted indices prefer compatible device-local mapped
memory. Only terrain residency keys prefer CPU-cached mapped memory. Both
preferences require compatible HOST_VISIBLE|HOST_COHERENT memory and preserve
the original coherent-host fallback. Allocation/map capacity failures may retry
a different compatible fallback; device loss and unrelated failures remain
fatal. Resource lifetime, physical authority and synchronization are unchanged.
See the [renderer contract](planetary-rendering.md) for the ownership details.

## Physical authority and representation

Every representation of a geographic point derives from one body-fixed
physical-height function, `H(bodyDirection)`. The canonical composition includes
the checked global elevation oracle, regional residual wherever the authoritative catalog contributes, accepted
deterministic physical modifiers, and the final non-negative clamp. It owns:

- FP64 body-fixed geometry and deterministic displacement;
- physical gradients and normals where physical truth is required;
- collision, camera clearance, and physical queries;
- geographic identity independent of camera, topology, pupil, level, cache,
  generation, and render cadence.

Full physical relief is prepared at shared render vertices before tessellation.
Ordinary TES interpolates that published surface and adds no separate physical
height evaluation; gameplay and camera clearance retain full canonical queries.
Topology density controls the piecewise-linear presentation of that surface; it
does not define physical truth. Exact render/query equality at every interpolated
point is not implied. Hardware tessellation and future material-displacement
capacity remain, without a second physical authority. Material shading may consume
cheaper deterministic derived information, but no material, LOD, cache, or fallback path
may become a second height or ownership authority.

`earth-surface-v5` and optional `earth-florida-m12` remain manifest-resolved,
hash-verified distribution and sampling inputs. They improve macro material,
classification, and regional physical fidelity. They are not competing visible
Earth surfaces. The retired adaptive patch, stitch-template, and radial-Eyeball
architectures must not be restored as production ownership paths.

## Pupil and scale behavior

The pupil is a moving presentation of the same body-fixed surface. Its origin,
level, and generation may change while the canonical direction, physical
height, and FP64 body-fixed position of a tracked geographic sample remain
unchanged. Exact lattice identities are reused across retained movement; a new
generation prepares only the data required by the resolved pupil.

Scale selection is driven by projected error rather than camera-distance tables
alone. Hysteresis and a retained current generation prevent oscillation while a
replacement prepares. Level changes and same-level snaps do not change Earth
orientation or `SurfaceAnchor` authority.

The 50 m contract applies only to near-camera TCS/TES refinement. It is not a
terrain-coverage radius. Factor-1 production base geometry remains present and
depth-owning outside the refinement range, including across the local horizon.

## GPU generations and ownership

The production runtime maintains one current generation and at most one
incoming generation. Preparation is asynchronous and bounded. The incoming
generation cannot publish until topology, physical positions, normals,
visibility/compaction state, indirect draw payload, and required GPU work are
complete and fence-confirmed.

Publication occurs atomically at a frame boundary. A completely prepared
generation with zero visible triangles is valid and may publish as the sole
owner; per-frame culling regenerates visible work from that same generation when
Earth re-enters the frustum. The new current generation
then owns Earth and the superseded GPU resources retire only after their use is
safe. Production telemetry and regressions require:

- exactly one Earth owner;
- zero overlap owners and zero unowned frames;
- zero stale-generation draws;
- valid compacted indices and one indexed-indirect billboard draw;
- no partial incoming generation suppressing the current surface.

## Accepted coverage and deployment

NCSM1 retains the accepted 18-level surface/orbit coverage, conservative curved
bounds, valid zero-visible publication and body-specific ownership. Earlier
coverage and tessellation stabilization measurements remain in
[production consolidation](production-consolidation.md) and its historical links.

A Release launcher starts the Release Triangle runtime; a Debug launcher starts
the Debug runtime. Runtime fallback builds retain the launcher's configuration.

## Known presentation continuity limit

Canonical physical terrain is invariant during camera translation, pupil
replacement, and adjacent-level selection. The current factor-1 triangulated
approximation is not perfectly invariant under a rare full pupil rebase because
the new lattice may triangulate the same smooth physical field differently.

Measured bounds at the accepted C3 baseline are:

- up to approximately 2.595 m change in the coarse factor-1 triangulated
  approximation during a rare/full rebase;
- approximately 2.8 mm difference between adjacent L14 and L15
  representations.

These values describe presentation/LOD morph work. They do not indicate moving
`H(bodyDirection)`, changing FP64 body-fixed points, translucent duplicate
terrain, or lost depth ownership. Future finer pupil/re-triangulation morphing
must preserve that distinction.

## Camera and SurfaceAnchor

The managed camera owns navigation. `SurfaceAnchor` stores immutable body-fixed
identity and reevaluates through authoritative body orientation. Earth —
Fullscreen Native uses the same production Solar navigation path after preset
initialization. Near-surface ENU free look, terrain-aware clearance, and the
body-center/surface handoff are presentation behavior and may not modify Earth
orientation or physical terrain.

## Accepted versus unfinished

Implemented and stable:

- the 18-level NCSM1 New Earth Renderer topology/runtime;
- canonical body-fixed terrain authority and physical preparation;
- moving/snapped pupil identity, reuse, scale selection, and hysteresis;
- asynchronous current/incoming GPU lifecycle and fence-complete publication;
- conservative curved-patch occlusion and screen/frustum rejection;
- persistent topology residency and reusable current/incoming GPU working resources;
- GPU cull/compact, KSA-parity bounded 50 m TES refinement, and indexed indirect raster;
- zero-visible publication/re-entry and body-specific presentation authority;
- one-owner Earth coverage through the accepted orbit-to-surface trajectory;
- FP64 world/physical authority with camera-relative GPU transport.

Future responsibilities, not an open production front:

- terrain presentation, filtering, and material quality;
- finer pupil/re-triangulation morph quality;
- atmosphere, clouds, water, weather, and environmental rebuilding;
- richer regional/global source fidelity;
- spacecraft flight and surface gameplay;
- measured regional replacement cost and orientation-sensitive workload variability;
- general immutable simulation snapshots and broader host concurrency.

The current renderer is production-owned and physically accepted, but it is not
visually finished.

## Known debt

- Regional replacement and viewing-dependent GPU cost remain as quantified above.
  No remaining terrain responsibility qualified for the M13 continuation bar.
- Ambient OBS Vulkan capture can report D3D11 KMT import
  `memoryTypeIndex-00645`; the measured caller is the OBS hook. Stale Epic
  registrations separately cause loader errors. Graphics Package 2, banked at
  `9409fec36e72f34f26cd4ab5fe47082e7dcafb20`,
  [isolates canonical validation](graphics-validation-package-2.md) while retaining
  strict ambient diagnostics.
- The opaque distant/detailed source assertion was corrected in banked Graphics
  Package 1. Package 2 validates generic grid/frames startup without Earth assets;
  other shared generic/planetary allocations remain separate resource-ownership debt.

## Next architectural decision and work boundary

Begin from banked M13.6. M13 is closed and there is no M13.7. Preserve the
accepted NCSM1 physical, regional/facility, visibility, tessellation, memory and
publication contracts. Historical exact-copy, residual-validation and lifecycle
experiments do not automatically become production candidates.

Next production-front authorization returns to Project Control. No surface/launch,
environment, material-fidelity or other next-front implementation is opened by
this closeout. Do not assign a next milestone or regenerate NCSM1 assets.
Future work must distinguish physical geometry, depth/ownership and
presentation/LOD behavior while keeping `H(bodyDirection)` authoritative.

Unrelated retirement findings are tracked in [the subsequent debt ledger](repository-debt-retirement.md). Deferral is not permanent acceptance.
