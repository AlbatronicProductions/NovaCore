# Earth-route ownership and convergence

The **unbanked consolidated candidate** uses NCSM1 and generation 4 for all six
supported Earth/Solar/Florida launcher routes, including **New Earth Renderer**.
Scene, focus, altitude, camera and Florida initialization remain route-specific.
Florida launcher-based manual acceptance **PASSED** before consolidation.
The superseded dynamic anchored owner, stitch/coverage draw, local texture-demand
transport and its investigation drivers have been retired. Generation-3 numerical
oracles and independent development scenes remain outside production Earth routing.
Terrain-v5 global bootstrap remains only until complete NCSM1 publication;
non-Earth presentation remains independent. No banking is authorized by this work.

Current architecture and validation: [production consolidation](production-consolidation.md).

## Historical convergence investigations

The status statements below record their original investigation/slice boundaries.
Everything below records the original pre-acceptance chronology, measurements and scope decisions.
It does not describe current routing or override the accepted consolidation boundary above.

Current contact follow-up: [Florida facility-support physical contract](florida-facility-support.md)
adapts KSA Model E with bounded canonical grading and unchanged slab/footing
transforms. The former south-midpoint clearance now passes the numerical contact
gate. **READY FOR FLORIDA MANUAL ACCEPTANCE**; convergence remains unbanked.
Earlier failure and P3 reports below retain their historical context.

P3 scheduling follow-up: [regional preparation de-blocking](ncsm1-regional-preparation-deblocking.md)
reduces the measured L17 publication-frame median from 71.699 ms to 37.026 ms
while retaining complete physical inputs and atomic ownership. Automated gates
pass; website-side review and manual acceptance remain pending. This does not
resolve the separate structure-contact discrepancy or bank route convergence.

Current follow-up: the [regional physical-residency migration](ncsm1-regional-physical-residency.md)
corrects live regional demand and parity. It remains **UNBANKED — BLOCKED by
remaining near-field physical/rendered seating disagreement** (0.692 m sampled
foundation clearance). The earlier observations below are historical evidence;
manual acceptance has not subsequently passed.

Status: **UNBANKED — FLORIDA MANUAL ACCEPTANCE FAIL**. Solar/Earth manual
acceptance is **PASS so far**. Florida's retest after the foundation correction
still failed. The [physical/rendered investigation](florida-physical-rendered-agreement.md)
proves missing regional data in live NCSM1 preparation (Category D); correction
requires website-side review. Rendering baseline remains
P2S5G at `7bb03b0e635fa3444a4243bbad82758a1770ea60`, annotated tag
`m12d-p2s5g-surface-workload-efficiency`. This work started clean at documentation
commit `7c41bb5d0317dc48290b359727031988145abb09` (also `origin/main`).

## Decision

All five ordinary target routes converge to the accepted NCSM1 owner and
physical generation 4. Generation 3 was historical compatibility for these
presets; no required scenario behavior was found to depend on it. Physical
heights and terrain-seated foundation offsets may change with the accepted
surface; authored geography, site identity and navigation remain intact.

| Route / preset | Preserved initialization | Previous Earth owner / generation | Candidate |
| -- | -- | -- | -- |
| Solar System Overview / SolarSystemOverview | `sol`; Sun-focused Solar Map, 58 AU overview; all ten bodies, orbits, focus/navigation | Anchored/global when Earth focused; generation 3 | NCSM1 / 4 on Earth focus; preparation deferred until needed |
| Earth Far / Orbital View / EarthFarOrbital | `earth`; Earth focus; 3,000,000 m above physical surface; land; existing Earth-only orbit/surface camera | Anchored/global / 3 | NCSM1 / 4 |
| Earth 700 km / Earth700Km | `earth`; Earth focus; 700,000 m; land; same Earth-only camera | Anchored/global / 3 | NCSM1 / 4 |
| Earth — Fullscreen Native / EarthFullscreenNative | `sol --focus=earth`; 700,000 m; land; native borderless; validation/performance configuration | Anchored/global / 3 | NCSM1 / 4 |
| Florida Launch Site / FloridaLaunchSite | `sol --focus=earth --surface-site=florida-launch`; authored 28.6084° N, 80.6042° W; surface-relative ENU camera and pad | Anchored/global / 3 | NCSM1 / 4; canonical terrain seating |

The three windowed target presets plus Florida retain 960×540 defaults; the
fullscreen preset retains native extent. All use surface mode 2 and terrain-v5
when Earth is the detailed target. Terrain and material assets are unchanged.

## Routing and compatibility boundary

`SampleOptions` and `EarthRoutePolicy` select production ownership from the
Earth/Solar scenario plus its explicit diagnostic policy, not solely from the
historical New Earth scene token. Ordinary routes default to generation 4.
The launcher encodes that generation explicitly for all five presets.
`earth` remains the Earth-only camera scene; `sol` remains the Solar scene.
No scene implementation was duplicated or replaced.

The existing `m12d-production-spherical-billboard` token remains a supported
Solar/Earth exploration alias with its existing Earth-focus/700-km defaults.
The **New Earth Renderer** preset remains useful as native-resolution Earth
exploration with performance telemetry; it is no longer unique in ownership.
It is retained without renaming. Fullscreen Native uses validation plus telemetry.

Explicit `--physical-surface=generation-3`, `--planetary-mode=cpu|validate`,
non-default `--gpu-capacity`, `--dynamic-traversal`, and `--m12c-fixed-pose`
retain their diagnostic/compatibility owner and physical-generation behavior.
Explicit generation-4 legacy diagnostics also remain possible. No CLI token,
proof route, shader, native pipeline, descriptor binding, generation-3
implementation, radial support, or old test was deleted.

The production moving runtime is shared. Eligible focused startup prepares its
first generation from that scene's own GPU constants. Solar overview creates
no prepared NCSM1 generation until Earth focus; subsequent focus switches use
the existing body-specific publication/presentation gate. Managed dynamic
anchored ownership is not instantiated for converged routes. Native shared
resources remain: retirement decomposition is a separate task.

## Automated evidence

Debug and Release sample builds and 15 launcher tests pass. Focused Graphics
coverage exercises five route contracts, explicit legacy diagnostics, body
eligibility, generation-4 Florida placement and identity, detach/attach and
return. Existing P2S5G interface, P2S5C runtime, body-eligibility, Florida and
Solar-scene regressions pass. No P2S5G shader or native implementation changed.

Ignored `build/route-before-{overview,far,700km,fullscreen,florida}.log` records
baseline startup. Candidate `build/route-after-*.log` records actual native
startup destinations: generation 4, NCSM1, `surfaceMode=2`, terrain-v5,
`material=production-billboard`, global/dynamic draws 0, candidate draw 1 and
one visible Earth owner after initial publication. The first Earth frame uses
one global bootstrap draw and no candidate/dynamic draw; the next frame uses
NCSM1. This matches the saved P2S5G physical-trajectory and closeout logs and
the unchanged New Earth alias. Solar first Earth focus has the same one-frame
bootstrap. No publication/lifecycle change was made to eliminate it.
Overview stays on the Sun without
an Earth draw until Earth focus. No performance improvement is claimed.

At native 3440×1440, `build/route-solar-focus-validation.log` records
Sun → Earth → Mars → Saturn → Earth over 460 frames. Non-Earth contexts have
surface mode 0, terrain version 0 and bounded-sphere presentation; returning
Earth restores NCSM1. Ten bodies remain present. Existing bounded shared
native preparation/residency is retained; no NCSM1 terrain draw runs for the
unrelated focused bodies.

`build/route-florida-return-validation.log` records 460 frames of site startup,
detach/attach, ascent to 700 km, and return. The pad is submitted at the site,
authored site identity is stable, and 19 publications report zero missing,
overlapping or stale owners. Installed Florida regional pack and elevation
oracle paths/hashes are retained in the runtime fingerprint. The standalone
identity test also distinguishes changed generation-4 terrain/foundation height
from unchanged authored body, direction, object and terrain-version identity.

Vulkan validation recorded only the known
`VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645` KMT external-memory warning;
no device loss or new VUID was observed. These are automated integration
results, not manual visual acceptance or a new performance gate.

The opt-in `NOVACORE_EARTH_ROUTE_VALIDATION=solar|florida` driver uses existing
scene operations for the two 460-frame checks. It is absent in ordinary launches.

## Required manual acceptance — Florida blocked by physical/rendered mismatch

The user reports Solar/Earth routes **PASS so far**. Florida's initial test and
post-foundation-correction retest both **FAILED** (floating pad). The regional
physical-data mismatch must be addressed before the next physical check below.
The original automated route evidence above did not establish visual foundation
contact or live-renderer regional parity.

- Solar System Overview: start normally, inspect Earth, switch to other bodies
  (including Saturn/rings), return to Earth; check no wrong-body Earth geometry,
  disappearance, missing orbits, or navigation regression.
- Earth Far / Orbital View: verify the 3,000-km start, approach Earth and inspect
  coherent detailed transition with the existing camera.
- Earth 700 km: verify the 700-km start, normal navigation, ownership and quality.
- Earth — Fullscreen Native: verify native borderless configuration, Earth route
  and Solar navigation.
- Florida Launch Site: verify the geographic site, pad/local-terrain relationship,
  near-surface movement, detach/reattach, ascent and return without geographic shift.

Use normal launcher presets with route-validation environment overrides absent.
No atmosphere/material polish is required. Do not bank before user acceptance.

## Retirement boundary

Before implementing the regional physical-data migration, the installed KSA
production terrain/loading path was inspected. The
[KSA responsibility audit](ncsm1-regional-residency-ksa-reference.md) records the
compatible body-owned data, upload completion and content-invalidation boundaries,
and the important difference between KSA's whole-body mip/coarse fallback policy
and NovaCore's mandatory regional physical dependencies. This reference inspection
does not close Florida's measured mismatch or any migration acceptance gate.

The old island remains live through explicit diagnostic/compatibility routes
and the accepted first-generation global bootstrap. It contains shared assets,
physical queries, preparation and presentation
responsibilities. Ordinary-route convergence makes portions candidates for a
later retirement audit; it does not prove that entire files or shader families
are removable. Do not begin retirement, root-helper consolidation, debris
removal, format/API cleanup, pupil continuity or material work in this ticket.
