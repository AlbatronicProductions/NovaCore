# Florida generation-4 foundation seating correction

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Current status: **UNBANKED — MANUAL RETEST FAILED** (2026-09-04).
The following is the historical seating-correction record. Its automated
measurements remain valid, but the subsequent manual retest still showed a gap.
The [live physical/rendered investigation](florida-physical-rendered-agreement.md)
proves missing regional data in NCSM1 preparation. The isolated parity verifier
used below did not establish live-renderer regional participation; its earlier
exclusion of a deeper authority issue is superseded by that direct evidence.
No further foundation adjustment is authorized by this finding.

Historical closeout status: **READY FOR FLORIDA MANUAL ACCEPTANCE**.
User-reported convergence acceptance is Solar/Earth **PASS so far**, Florida
**FAIL** for a floating pad. This correction has automated/runtime acceptance;
the corrected Florida visual result still requires physical Desktop acceptance.

## Starting state and cause

Started at `7c41bb5d0317dc48290b359727031988145abb09` (`origin/main`) with the
existing unbanked route-convergence work preserved. The banked renderer remains
P2S5G, commit `7bb03b0e635fa3444a4243bbad82758a1770ea60`, annotated tag
`m12d-p2s5g-surface-workload-efficiency`. No commit, staging, push or tag was made.

**Classification F: foundation geometry / anchor semantics.** The existing
anchor is the slab origin, raised to the maximum of the center and eight
footprint terrain samples plus a 0.25 m clearance margin. The native model's
lowest vertices were at local Z=0; its slab occupied Z=0..1.5 m. There was no
foundation below that raised plane. The local pivot offset is zero; model axes
are East X, North Y, Up Z, with no extra native vertical translation.

Measured with the production Solar Earth radius 6,371,008.8 m and installed
global oracle plus regional pack, before correction:

| Quantity | Metres above the reference sphere unless stated otherwise |
| -- | --: |
| Canonical generation-4 H at the anchor | 18.886950220789107 |
| Existing terrain-relative slab-origin offset | 3.083511314970373 |
| Slab root / bottom plane at center, H + offset | 21.97046153575948 |
| Body-fixed radial measurement of root | 21.970461536198854 |
| Slab top at center | 23.47046153575948 |
| Slab thickness / mount height above slab | 1.5 / 7 |
| Lowest original model-local vertex Z / pivot offset | 0 / 0 |
| Center gap | 3.083511314970373 |
| Smallest original eight-edge-sample radial gap | 0.2501247805807978 |
| Largest original eight-edge-sample radial gap | 5.086373495110152 |
| Global oracle elevation at anchor, before composition/clamping | -3.18103971223142 |
| Regional residual at anchor, before composition/clamping | 15.001533002687239 |

The sub-nanometre difference between the algebraic root and vector-length root
is FP64 rounding. The eight-edge-sample gaps above include spherical curvature
and query H at each actual bottom-vertex/edge-midpoint direction. The later
quarter-metre survey finds ground as low as local Z=-6.585569277405739; the
original nine samples were not a complete minimum-height survey.

A and B are excluded by the runtime generation-4 selection and live canonical
query; there is no cached generation-3 absolute altitude. C is excluded because
neither the anchor nor footing queries rendered geometry. D has no supporting
evidence: both CPU and GPU consume the installed regional residual in the same
physical composition. E has no supporting evidence: the pad uses the evaluated
canonical ENU/root pose, unit slab scale, and the existing FP64 camera-relative
transport. Focused pose/rotation tests pass. No deeper physical-authority issue
was found; the measured gap must not be relabeled as a terrain-height error.

## Bounded correction

The existing slab, extensions, mount, horizontal identity, heading, slab origin,
surface anchor, camera eye/pivot semantics and terrain remain unchanged. A
separate persistent unit foundation mesh occupies X/Y=-0.5..0.5 and Z=-1..0.
The existing object transport scales it to 64 x 48 metres and the surveyed depth,
at exactly the slab's position and orientation. This fills the missing support
below the deck without moving the site or stretching its authored structures.

At initialization, intersect vertical ENU lines around the perimeter with
canonical H at 0.25 m intervals (896 points), including the center in the minimum.
The intersection accounts for Earth curvature and the change of body direction
along each vertical line. The bounded fixed-point solve requires a 0.1 micrometre
step tolerance. The footing depth is:

`depth = -minimumTerrainLocalUp + FoundationEmbedMetres`

`FoundationEmbedMetres = 0.25 m` is structural penetration beneath the lowest
surveyed ground contact. It closes foundation-edge cracks; it is not a downward
pad nudge or a tuned renderer offset. A preliminary 1 m survey missed a 3.2 cm
dip between samples, so the final survey uses 0.25 m spacing. Independent tests
use 0.0625 m spacing to verify contact between placement samples.

| Corrected physical/model relationship | Metres |
| -- | --: |
| Unchanged slab root height | 21.97046153575948 |
| Lowest surveyed contact in slab-local ENU | -6.585569277405739 |
| Foundation depth / lowest actual model-local Z after scaling | 6.835569277405739 / -6.835569277405739 |
| Foundation bottom elevation at its center | 15.13489225835374 |
| Dense maximum bottom-to-ground radial gap | -0.24999968219162483 |
| Dense minimum deck-top clearance | 1.709151273457259 |

The footing is intentionally partly underground and has exposed sidewalls on
the lower ground. The deck and mount are not lowered or buried. Terrain contact
occurs along the foundation sidewalls, not at an invented universal flat ground
plane. At the center, the sidewall/contact height still derives from H, and the
original slab origin remains 3.083511314970373 m above H.

## Regional authority and invariance

The site is inside the installed regional footprint: the nonzero residual is
15.001533002687239 m at its direction. The regional identity remains SHA-256
`c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.
The global CPU oracle identity remains
`4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317`.
These are inputs to the existing clamped physical composition, not simply
additive values to be appended again to final H.

Program initialization selects generation 4 and resolves/decodes regional
physical data before creating the Solar scene and Florida site. The CPU data
snapshot is immutable. GPU physical preparation uses the same oracle and local
pack, independently of material residency. Later material streaming or NCSM1
publication cannot change the site survey. The existing optional-pack fallback
policy remains unchanged; the measurements here use the installed pack.

Unchanged identity: body 6; object `0x4E435F464C5F5044`; latitude 28.6084 degrees;
longitude -80.6042 degrees; normalized direction
`(0.1433224599406355, 0.4788205718227514, 0.8661348234979923)`;
terrain-v5; original geometry identity and exact canonical ENU heading.
Slab/footing share a root pose; Earth rotation changes only that derived root
pose. The fixed body pose and footing depth do not depend on time, camera, LOD,
pupil generation, publication timing, or ascent/return.

## Verification and evidence

Files changed by this seating correction (the other dirty convergence files
were preserved):

- `src/NovaCore.Graphics/FloridaLaunchSite.cs`
- `src/NovaCore.Graphics/RenderObject.cs`
- `native/NovaCore.Native/NovaCoreNative.cpp`
- `samples/NovaCore.Triangle/Program.cs`
- `samples/NovaCore.Triangle/EarthRouteValidation.cs`
- `tests/NovaCore.Graphics.Tests/Program.cs`
- `tests/NovaCore.Graphics.Tests/FloridaFoundationSeatingTests.cs`
- `docs/earth-route-convergence.md`
- `docs/florida-generation4-seating.md`
- `docs/NOVACORE_CURRENT_STATE.md`
- `docs/CODEX_HANDOFF.md`

Native and sample Debug/Release builds passed without warnings/errors.
Focused Graphics checks passed: Florida foundation seating, Anchored Florida
launch site (including its unchanged historical object hash and zero-allocation
evaluation), Earth route convergence, GPU physical-height preparation, and P2S5G
compiled surface interface. All 15 launcher tests passed.

The seating regression loads production data before constructing the site,
independently solves contact by bisection to within 1 micrometre, checks all
perimeter edges at 6.25 cm spacing, preserves the deck above ground, exercises
real pupil snaps at levels 8/12/17, and checks body-fixed invariance through
camera detach/attach, ascent/return, and Earth rotation through one year.
CPU/GPU generation-4 parity over 94 samples (including Florida and local
offsets) has maximum height error `2.00138572381547419E-10 m`, validation errors 0.

Native 3440x1440 borderless validation:

- Debug: 460 frames, detach/attach, 700-km ascent, return; PASS. All 19 NCSM1
  publications (L17 through L8 and back to L17) report one owner, zero missing,
  overlapping or stale owners. Body-fixed seating and depth remain exact.
- Release: 160 ordinary Florida frames, without the scenario driver; exit 0.
  Final frame: `surfaceMode=2`, `terrainVersion=5`, `candidateOwner=1`,
  `globalOwner=0`, `dynamicOwner=0`, `candidateIndirectDraw=1`,
  `visibleEarthOwners=1`. Physical generation is 4; topology is NCSM1.
- Only the previously known KMT external-memory
  `VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645` appeared. No new VUID or
  device loss was observed. This is not a new performance acceptance claim.

Local evidence is in `build/florida-seat-probe/`: `before.log`,
`seating-test.log`, `check-0.log` (CPU/GPU parity), `check-1.log` (old anchor
regression), `check-2.log` (route convergence), `check-3.log` (P2S5G),
`launcher-tests.log`, `florida-runtime-debug.log`, and
`florida-runtime-release.log`. The pre-correction tracked diff is retained there
as `pre-seating.patch`. No terrain assets, shaders, culling, TES, 50 m refinement,
physical generation implementation, route selection, or compatibility paths
were changed by this correction.

## Manual gate

**READY FOR FLORIDA MANUAL ACCEPTANCE.** Launch the normal Florida preset and
check visible terrain contact without hover or excessive burial, no lateral
shift, coherent surrounding ground, camera movement around all sides, surface
navigation, ascent/return, and LOD/pupil transitions. The correction's visible
result remains unaccepted until the user performs this check. Stop here for
website-side review; do not bank or begin renderer retirement.
