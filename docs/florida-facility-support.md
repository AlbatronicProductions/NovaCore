# Florida facility-support physical contract — unbanked candidate

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Current presentation follow-up: [authored facility light occlusion](facility-light-occlusion.md).
The physical support contract below remains unchanged and numerically valid.
Continuous final-pixel evidence identified missing Sun visibility (P4), and the
new lighting candidate remains unbanked with Florida manual acceptance still FAIL.

## Historical support-slice closeout

The following readiness statement records the earlier support implementation gate;
it is not a subsequent physical acceptance PASS.

**READY FOR FLORIDA MANUAL ACCEPTANCE**. Automated gates passed; user physical
acceptance remains required. This implements the requested support/contact vertical slice;
it does not bank Earth-route convergence. The previous south-midpoint clearance
is removed in the live near-surface geometry reconstruction without moving the
slab, increasing footing depth, or changing the 50 m TES contract.

## Architecture and KSA decision

**ADAPT KSA Model-E responsibility.** The local production assembly hash was
reverified against the inspected source (`KSA.dll`, SHA-256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`). The CPU
physical-height, authored decal, facility transform and collider responsibilities
remain as established in `build/structure-contact-review/REPORT.md`; the physical
height/decal source was re-inspected for this ticket. KSA implementation, assets,
constants and runtime dependencies were not copied.

NovaCore adopts authored physical grading and a fixed structure/support
relationship. A reusable immutable `FacilitySupportRegion` lives in Core.Surface;
canonical generation-4 terrain queries apply the authored Florida definition.
The existing GPU canonical evaluator has its equivalent physical operation.
This mirror is necessary because accepted NCSM1 physical preparation executes
on the GPU. It is not a Florida draw-path deformation, GPU-only decal, or TES
triangle-following structure placement. CPU collision/clearance queries consume
the same final H through `IPhysicalTerrainAuthority`. Gameplay colliders, landing
gear, wheels and walking are not implemented here.

## Authored footprint and support plane

Body 6, latitude 28.6084°, longitude −80.6042°, exact existing body-fixed direction
and ENU orientation. The original 64×48 m foundation remains centered on that
site. The fully supported rectangle is 128×112 m (half-extents 64 m east and
56 m north): the foundation plus a 32 m apron protects contact triangles during
near-surface pupil/adjacent-LOD changes. A 128 m transition on each side gives a
finite outer rectangle of 384×368 m (half-extents 192 and 184 m). This footprint
is explicit facility grading, not a body-wide flatten or a camera-selected area.

The plane is horizontal in the site's fixed ENU frame. Its elevation is
**15.134892258793116 m** above the reference sphere along the site up axis.
It is the existing foundation bottom: the original generation-4 natural center
and 896 perimeter intersections at 0.25 m spacing established the minimum, then
the already-authored 0.25 m embed established the footing bottom. A runtime
survey-provenance check rejects a changed natural dataset/definition instead of
silently moving the facility to a new plane.

The slab radius remains **6,371,030.770461536 m** and foundation depth remains
**6.835569277405739 m**. The center terrain height changes from 18.886950220789107
to 15.134892258793116 m. Consequently the terrain-relative anchor offset is
re-expressed as the same fixed foundation height, 6.835569277405739 m, preserving
the exact slab radius. This is not an added visual offset. Body ID, direction,
object/mesh identity, geographic coordinates, heading and body-fixed transform
remain unchanged. The full anchor hash may change because its terrain-relative
offset now refers to the supported H. The old natural survey remains recorded
separately from current supported contact.

## Deterministic physical composition

For unit body direction d, site up U, east E, north N, and reference radius R:

```
c = dot(d,U)
x = R * dot(d,E) / c
y = R * dot(d,N) / c
P(d) = (R + planeAltitude) / c - R
tX = clamp((abs(x) - 64) / 128, 0, 1)
tY = clamp((abs(y) - 56) / 128, 0, 1)
Q(t) = 6*t^5 - 15*t^4 + 10*t^3
w = (1-Q(tX)) * (1-Q(tY))
G = max(0, signedGlobalElevation + regionalResidual)
B = max(0, G + naturalMacro + naturalMeso)
Bsupported = B + w * (P-B)
Nsupported = (1-w) * naturalNear
H = max(0, Bsupported + Nsupported)
```

The exact rectangle tests return w=0 outside the outer footprint. The support
composition follows natural base/near evaluation and precedes the existing final
nonnegative clamp. On this positive-height site it is the convex blend of final
natural H and the plane. Crucially, the prepared base carries the plane while
canonical near relief is attenuated by the same physical weight. TES still adds
only that canonical near field inside its unchanged 40–50 m falloff. The inner
region has zero near relief, so camera-dependent TES attenuation cannot reopen
the foundation gap. Outside the footprint both physical height and near-field
semantics are unchanged, exactly.

The quintic transition has zero first and second derivatives at both ends.
Physical normals incorporate the plane slope and the analytic weight-gradient
term `(P-B)*grad(w)`; near gradients become `(1-w)*grad(N)-N*grad(w)`. Inner normals
are the exact site up axis; outer normals retain the accepted natural base-normal
estimator. NCSM1 preparation and CPU/native physical queries use the same rule.
No tessellation factor, TES range, P2S5G interface, topology or route changes.

## Identity, residency and lifecycle

The support definition is immutable for a renderer lifetime, with no live-edit
or hot-reload API. Its deterministic hash includes body, revision, frame, radius,
plane and footprint parameters and participates in canonical physical-sample
cache identity. A changed authored definition requires a fresh renderer lifetime
and complete preparation/publication; cached physical vertices are not repaired
in place. Shader build dependencies include the facility definition. The runtime
survey check detects a stale support plane relative to its original physical
source. Regional catalog/content identity remains a separate concern.

All 670 contributing regional records, asynchronous I/O, bounded uploads, fixed
pupil snapshots, 65,536-vertex preparation slices and final-fence readiness gates
are retained. Support adds no residency requests, textures, shader descriptors,
per-frame allocations, full-buffer rebuild or queue/device idle. The existing
normal support footprint remains sufficient: support-normal composition adds
only the central natural base sample already present in geographic demand.

## Numerical contact acceptance

Required contact tolerance: **1 cm at near-surface L16/L17**, not exact floating
point zero. The table is the completed L17 Florida return. Heights are metres
above the reference sphere. Support height outside the foundation is the
extrapolated plane for comparison; no structure contact is asserted there.

| ENU sample (m) | Natural H | Support modifier | Final H | Live draw height | Support plane | Draw − H | Foundation − draw |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| (0, 0) | 18.886950 | -3.752058 | 15.134892 | 15.134890 | 15.134892 | -0.000002 | 0.000002 |
| (-32, -24) | 21.720462 | -6.585444 | 15.135018 | 15.135019 | 15.135018 | 0.000001 | -0.000001 |
| (32, -24) | 19.915678 | -4.780660 | 15.135018 | 15.135019 | 15.135018 | 0.000001 | -0.000001 |
| (-32, 24) | 21.285737 | -6.150719 | 15.135018 | 15.135018 | 15.135018 | 0.000001 | -0.000001 |
| (32, 24) | 19.544928 | -4.409910 | 15.135018 | 15.135017 | 15.135018 | -0.000001 | 0.000001 |
| (-32, 0) | 21.588543 | -6.453570 | 15.134973 | 15.134975 | 15.134973 | 0.000003 | -0.000003 |
| (32, 0) | 20.286709 | -5.151736 | 15.134973 | 15.134970 | 15.134973 | -0.000003 | 0.000003 |
| (0, -24) | 16.884125 | -1.749188 | 15.134937 | 15.134935 | 15.134937 | -0.000003 | 0.000003 |
| (0, 24) | 17.402873 | -2.267936 | 15.134937 | 15.134940 | 15.134937 | 0.000002 | -0.000002 |
| (64, 0) | 12.219175 | 2.916039 | 15.135214 | 15.135214 | 15.135214 | -0.000000 | — |
| (96, 0) | 7.552201 | 6.798413 | 14.350614 | 14.258007 | 15.135616 | -0.092607 | — |
| (128, 0) | 7.138558 | 3.998810 | 11.137368 | 10.947355 | 15.136178 | -0.190013 | — |
| (160, 0) | 6.552870 | 0.888581 | 7.441451 | 7.520248 | 15.136901 | 0.078797 | — |
| (0, 120) | 7.514671 | 3.810676 | 11.325347 | 10.779465 | 15.136022 | -0.545882 | — |
| (0, 216) | 7.222070 | 0.000000 | 7.222070 | 6.461536 | 15.138554 | -0.760534 | — |

The previous south-midpoint `(0,-24)` clearance was approximately +0.692 m.
It now passes the 1 cm contract. Across the nine return contact points the maximum
absolute clearance is **0.000002811 m**; across 81 sampled near-surface L16/L17
contact points during movement and returns it is **0.000003967 m**.

These are reconstructions from live, fence-complete physical buffers and actual
compacted draw triangles, including VS FP32 camera-relative conversion. They are
not screenshot measurements or direct post-TES buffer capture. At these contact
points all triangle vertices lie inside the fully supported plane and canonical
near relief is zero, so the unchanged factor-1 TES leaves that triangle in place.
Rigid body/projection rounding and physical visual acceptance remain separate.
At the 700 km retreat, FP32 camera-relative reconstruction gives a maximum
0.020597 m plane difference, far below a pixel there; that far-distance transport
measurement is not presented as near-surface contact precision.

The transition/outside draw-minus-H differences in the table are reported rather
than hidden. They include the existing omission of canonical near relief outside
50 m and triangle interpolation. GPU **prepared-base** parity is checked against
CPU supported base, not falsely equated with the full natural near-field H at
every rendering distance. The facility's inner support removes that distinction
where foundation contact is required.

## Transition and physical parity

Dense 0.25 m cross-sections along both axes have maximum absolute slope
**0.130586 m/m**. Values stay between the natural and support endpoints, without
blend overshoot. Across ±0.1 mm around the four boundary joins the maximum height
change is **0.000002718 m**, consistent with a continuous slope rather than a step.
Outside-footprint canonical modifier samples are exactly zero. The analytic C2
blend also closes the rectangular corners continuously. No numerical cutoff cliff
or blend-generated overshoot was found; visual moat/ridge assessment is manual.

Live actual prepared vertices in the contact/transition/outside return pass the
existing 3 mm physical-base tolerance, maximum **0.000997195 m**. Inner live
FP32 normal squared error is **1.403e-16** against site up. The separate 94-query
canonical CPU/GPU regression passes height (max 9.32e-10 m), gradient (3.018e-5)
and normal (3.018e-5 rad) tolerances with zero validation errors. That query proof
supplements, and does not replace, the live NCSM1 preparation readback.

## Runtime, camera and performance

Cold Florida, retained movement, pupil snaps, adjacent LOD, 700 km retreat,
re-approach, leaving Florida, Mars/Saturn focus and Earth return are exercised.
The site/footing reconstruction and body-fixed transform are deterministic across
time, LOD and pupil changes. Camera SurfaceAnchor/clearance queries use supported
canonical H, while the startup slab-relative pose remains fixed. Smooth physical
grading avoids a discrete clearance step; perceived camera motion remains part
of manual acceptance.

Native 3440×1440 Debug, same 800-frame regional driver, L17 712,106 physical
vertices and 670 resident records. Timing builds omit only expensive diagnostic
readback. Two warm returns:

| Metric | P3 corrected baseline | Facility support |
| --- | ---: | ---: |
| Total physical GPU preparation median | 44.567 ms | 42.445 ms |
| Publication-frame median / max | 37.026 / 38.229 ms | 35.105 / 35.410 ms |
| Publication fence-wait median | 32.499 ms | 30.691 ms |
| Readiness median, 11 slices | 405.931 ms | 386.467 ms |
| Retained frames 760–799 median / max | 30.851 / 32.997 ms | 29.240 / 29.662 ms |

CPU Release center no-gradient canonical H query: 9,085 ns before / 9,213 ns after in the same
10,000-query harness (about 1.4% in these samples). Debug support regression is
about 9,599 ns/query. These are small run-specific measurements: no speedup or
universal frame budget is claimed. There is no measured P3 regression. The same
one-current/at-most-one-incoming owner remains drawable between slices; no missing,
overlapping or stale-generation owner failures and no regional reload churn.

## Files and verification

- `src/NovaCore.Core/Surface/FacilitySupportRegion.cs`: reusable physical grading
  primitive and immutable authored Florida support definition.
- `src/NovaCore.Graphics/PlanetaryPhysicalSurface.cs`: supported canonical base,
  final height, normals and separately reported support delta; natural survey oracle.
- `src/NovaCore.Graphics/FloridaLaunchSite.cs`: preserve original natural survey,
  slab/root and footing while anchoring to supported H.
- `src/NovaCore.Graphics/PlanetaryNaturalTerrainPreparation.cs`: support-definition
  identity in physical sample reuse keys.
- `native/NovaCore.Native/shaders/facility_support.glsl`,
  `planetary_natural_terrain_surface.glsl`,
  `production_spherical_billboard_physical.glsl`, `planetary_height_query.comp`:
  equivalent canonical physical composition and normal/gradient outputs.
- `native/NovaCore.Native/CMakeLists.txt`: physical shader include dependencies.
- `native/NovaCore.Native/RegionalPhysicalProbe.inl`: opt-in live compacted-triangle
  readback for facility/blend/outside points, normals and FP32-relative reconstruction.
- Graphics tests: new facility test; regional live contact/parity checks; foundation
  survey versus supported-contact distinction; route fixture loads authoritative data;
  registration; canonical GPU query modifier-reporting regression. Existing P2S5C/P2S5G and generation-3 checks are preserved.
- This report and current follow-up links in the two active convergence/residency
  documents. Historical investigation reports and measurements remain unchanged.

Debug/Release native, Debug sample and Graphics builds pass. Focused facility,
foundation, route-convergence, anchored Florida generation-3, P2S5C, compiled
P2S5G, GPU physical height/gradient/normal, P2S4, canonical SurfaceAnchor and body
eligibility tests pass. Live test evidence and final status are recorded below.
The full live application retains only the previously documented external Win32
import `VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645`; no new VUID or device
loss was observed. This is not a completely validation-clean application run.

Evidence resides under ignored `build/facility-support/`, including the initial
status/patch/hash snapshot, original natural survey, CPU support table, live
contact analysis, native and test logs, and per-frame timing trace. The prior P3
timing evidence remains intact under `build/regional-preparation-deblocking/`.

## Required stop

**READY FOR FLORIDA MANUAL ACCEPTANCE.** The user must check no floating or
excessive burial, convincing foundation contact, no unintended shelf/moat/ridge,
no visible LOD/pupil pop, fixed geography, smooth performance, and stable return.
Gameplay collision/support primitives remain future consumers of this canonical
H, not implemented gameplay. No renderer retirement, material/atmosphere work,
generation-3 removal, staging, commit, push, tag or convergence banking.

## Final acceptance evidence and Git boundary

The final live regression passed with **43 captures, 1,596 checked prepared
vertices, L8–L17 coverage and 40 pupil identities**. It includes nine contact,
four transition and one outside sample on the completed L17 return, and 81
near-surface L16/L17 contact samples through movement/returns. The separate
outside-Florida Earth scenario still requests five regional records; fresh
Mars/Saturn focus requests none. Full runtime evidence is at
`build/regional-live-tests/fa4d389866914dcdb0bd6c9d9ccd6a23/`; the test log is
`build/facility-support/live-acceptance.log`. The final canonical GPU query and
modifier-output regression also passes (`physical-query-final.log`).

`git diff --check`: **PASS (exit 0)**. Changed/new files also pass a trailing
whitespace scan. HEAD remains `7c41bb5d0317dc48290b359727031988145abb09` and the
index is empty. This ticket changes 15 existing files and adds four; the other
422 of 437 initially hashed source/document/configuration files remain
byte-identical, with none missing. The combined working tree includes the earlier
unbanked candidate: 26 modified and 24 untracked files. Nothing was staged,
committed, pushed, tagged or banked.

Final `git status --short`:

```text
 M README.md
 M docs/CODEX_HANDOFF.md
 M docs/M12D-P2S5G-workload-investigation.md
 M docs/NOVACORE_CURRENT_STATE.md
 M docs/architecture.md
 M docs/planetary-rendering.md
 M native/NovaCore.Native/CMakeLists.txt
 M native/NovaCore.Native/NovaCoreNative.cpp
 M native/NovaCore.Native/shaders/planetary_height_query.comp
 M native/NovaCore.Native/shaders/planetary_natural_terrain_surface.glsl
 M native/NovaCore.Native/shaders/planetary_physical_authority.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_incoming_prepare.comp
 M native/NovaCore.Native/shaders/production_spherical_billboard_physical.glsl
 M native/NovaCore.Native/shaders/production_spherical_billboard_prepare.comp
 M samples/NovaCore.Triangle/NovaCore.Triangle.csproj
 M samples/NovaCore.Triangle/Program.cs
 M src/NovaCore.Graphics/FloridaLaunchSite.cs
 M src/NovaCore.Graphics/PlanetaryNaturalTerrainPreparation.cs
 M src/NovaCore.Graphics/PlanetaryPhysicalSurface.cs
 M src/NovaCore.Graphics/RenderObject.cs
 M tests/NovaCore.Graphics.Tests/GpuPhysicalHeightPreparationTests.cs
 M tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
 M tests/NovaCore.Graphics.Tests/PlanetaryProductionSphericalBillboardRuntimeTests.cs
 M tests/NovaCore.Graphics.Tests/Program.cs
 M tests/NovaCore.Launcher.Tests/Program.cs
 M tools/NovaCore.Launcher/ScenarioCatalog.cs
?? docs/earth-route-convergence.md
?? docs/florida-facility-support.md
?? docs/florida-generation4-seating.md
?? docs/florida-physical-rendered-agreement.md
?? docs/ncsm1-regional-physical-residency.md
?? docs/ncsm1-regional-preparation-deblocking.md
?? docs/ncsm1-regional-residency-ksa-reference.md
?? native/NovaCore.Native/RegionalPhysicalPreparation.inl
?? native/NovaCore.Native/RegionalPhysicalProbe.inl
?? native/NovaCore.Native/RegionalPhysicalResidency.h
?? native/NovaCore.Native/RegionalPhysicalResidency.inl
?? native/NovaCore.Native/RegionalPhysicalResidencyTests.cpp
?? native/NovaCore.Native/shaders/facility_support.glsl
?? native/NovaCore.Native/shaders/regional_physical.glsl
?? native/NovaCore.Native/shaders/regional_physical_demand.comp
?? native/NovaCore.Native/shaders/regional_physical_incoming_demand.comp
?? samples/NovaCore.Triangle/EarthRoutePolicy.cs
?? samples/NovaCore.Triangle/EarthRouteValidation.cs
?? samples/NovaCore.Triangle/SampleOptions.cs
?? src/NovaCore.Core/Surface/FacilitySupportRegion.cs
?? tests/NovaCore.Graphics.Tests/EarthRouteConvergenceTests.cs
?? tests/NovaCore.Graphics.Tests/FacilitySupportTests.cs
?? tests/NovaCore.Graphics.Tests/FloridaFoundationSeatingTests.cs
?? tests/NovaCore.Graphics.Tests/RegionalPhysicalResidencyTests.cs
```
