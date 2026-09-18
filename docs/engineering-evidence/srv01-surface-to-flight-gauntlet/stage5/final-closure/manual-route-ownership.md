# Stage 5 manual route / presentation ownership check

2026-09-17. **REVISE — ROUTE/PRESENTATION FIX REQUIRED.**

**Cause classification: A — intentional schematic representation of the admitted Florida physical plane, implemented with the generic qualification support mesh.** The route does not construct recognizable Florida terrain/site presentation. Real physical site admission and a generic visual cube are separate facts; proving the former did not satisfy the manual requirement for the latter.

Project Control corrected its earlier PASS: only supported-craft visual/physical behavior was accepted. Stage 5 engineering remains PASS; Florida manual acceptance is **PENDING / NOT YET TESTED CORRECTLY**; Stage 5 is **NOT FULLY QUALIFIED**. Promotion is on hold. Stage 6 CLOSED; no milestone or banking.

## Actual route and ownership chain

| Boundary | Direct current source | Proven behavior |
|---|---|---|
| Manual recipe | `final-closure/launch.ps1` | Uses the sealed stock Release executable with `--scene=srv01-florida-support --log=renderer,vulkan`. It does not select the Solar Florida route. |
| Argument selection | `samples/NovaCore.Triangle/SampleOptions.cs` | Accepts `srv01-florida-support` as a distinct scene. `florida-launch` surface-site selection belongs to `sol`. |
| Scene construction | `samples/NovaCore.Triangle/Program.cs:67–70` | Constructs `StockAssemblyDevelopmentScene` with supportedContact=true, poweredSupport=false, floridaSupport=true. No SolarSystemScene or EarthPlanetaryScene is constructed for this branch. |
| Physical data | `StockAssemblyDevelopmentScene.cs:79–88` `PrepareFlorida` | Loads required Earth/local physical data, acquires body-6 physical query, creates the Florida site at east=48 m and logs exact site/authority identities. Missing required data throws; there is no fallback-to-qualification-physics branch. |
| Admitted site | `AssemblyFloridaSite.cs:26–61` | Binds the current grading capability, generation/authority, Florida region, epoch and basis. Verifies the complete admitted patch lies in the full-weight plane branch and checks corner samples. The original qualified 16 m footprint is 48 m east of the anchor, outside the existing platform. |
| Canonical frame | `AssemblyApplicationSession.cs:21–25` | Creates the Earth-centred nonrotating parent and Florida CCF carrier. Physics/publication remain the already-qualified site-bound path. |
| Initial visible ground | `StockAssemblyDevelopmentScene.cs:65–66` | Every supported scene adds `SupportSlab()` to its snapshot, including floridaSupport. |
| Every visible frame | `StockAssemblyDevelopmentScene.cs:133–137` `BuildSubmission` | Submits copied canonical craft parts, then the same `SupportSlab()`. There is no Florida-specific ground rendering branch. |
| Exact ground object | `StockAssemblyDevelopmentScene.cs:169` | Constant centre `(0,-2.7,0)`, identity orientation, scale `(16,2,16)`, carrier frame, `MeshHandle.ContactQualificationSupport`. Its top at local Y=-1.7 geometrically matches the admitted plane. Its two-metre thickness is a visual cuboid, not geological data. |
| Mesh ownership | `src/NovaCore.Graphics/RenderObject.cs:8–10`; `native/NovaCore.Native/NovaCoreNative.cpp:735–742,822–824` | Handle 6 resolves to the generic support cube. Native code creates a centred unit cube with fixed RGB `(0.32,0.34,0.38)`, independent of facility geometry. This is the light/white-looking slab described by the user; no claim about exact perceived display colour is needed. |
| Native presentation submission | `src/NovaCore.Graphics/ReusablePartVisuals.cs:180–181`; `Program.cs:197` | The assembly uses `RunRendererWithVisualMeshes`. This branch does not supply the Earth/Solar terrain runtime assets/presentation owner. |
| Separate existing Florida presentation | `Program.cs:54–64,95–99,229–232`; `EarthRoutePolicy.cs` | Earth/Solar routes prepare terrain rendering. The Solar branch can add the Florida pad/foundation. Assembly `TryBuild` returns at line 224 before that branch. |

This is not an inference from the names `Florida` or `ContactQualificationSupport`: construction, arguments, submitted transforms, handle dispatch and vertex generation were read directly. The title and console explicitly describe the intended site-local schematic. That shows intentional reuse, rather than evidence of an accidentally selected physical fallback.

The retained `manual-final-1.log` records the stock/Florida identities and completed 1,200-interval episode. Current source and eight manual-binary seals match the prepared route. Those facts explain the observation without requiring a wrong-executable hypothesis; the static review does not independently reconstruct which process the user opened.

## Why the Florida scene was not visible

The physical path authenticates Florida, but the presentation path discards almost all site context and supplies the same generic cuboid as the non-Florida support route. It has no Florida terrain material/mesh submission, visible site anchor/ground context or facility owner. Its local dimensions and alignment are consistent with the plane, but a viewer cannot distinguish it from the qualification slab by the displayed ground.

The source therefore supports **A** as the primary cause. **B** (accidental leftover) is not established because the schematic reuse was deliberate and documented. There is also a missing terrain-presentation integration boundary, but **C** as a runtime fallback is not what the code does: it never attempts that presentation and then falls back. No evidence supports a physics/data substitution or failed terrain load.

The earlier report accurately disclosed a site-local view, but incorrectly treated that disclosure plus geometric agreement as sufficient preparation for Florida visual acceptance. Project Control has explicitly rejected that sufficiency. No earlier failure, physical result or timing measurement is rewritten.

## Why automated tests did not catch this

`AssemblyFloridaQualificationTests.Presentation` checks route parsing, 38 submitted objects, ground centre, camera nonmutation, bounded servicing, exact stores, final hold and zero allocation. It does not require a distinct Florida-ground presentation owner or recognizable site context. Its line-191 assertion text mentions dimensions, but the expression checks object count and reconstructed centre, not a distinct site mesh/material. These tests support the engineering behaviors they actually inspect; they cannot grant manual scene identity acceptance.

The existing Launcher `FloridaLaunchSite` preset in `tools/NovaCore.Launcher/ScenarioCatalog.cs:52–54` selects Solar scenery. The separate Launcher test expects `--scene=sol --focus=earth --surface-site=florida-launch`. That route does not instantiate the supported SRV-01 session. Merely changing the command to that scenery route would omit the required craft/physics witness and is not a complete correction.

## Minimum correction responsibility — not implemented here

Provide a visibly identifiable Florida ground/site presentation bound to the same admitted site authority, anchor, basis and footprint, alongside the unchanged copied canonical SRV-01 endpoints. Reuse the suitable existing terrain/site presentation resources, or a bounded data-derived site representation if sufficient; do not merely rename or recolour the generic cube and claim terrain integration.

Preserve the qualified 48 m east placement, outside the existing platform. Do not move the craft onto the platform or change the ground/collider to make the scene resemble a pad. A new launchpad, tower, polished facility, or full launch complex is unnecessary. The correction must make its limited site-local scope explicit and visibly understandable without granting authority to decorative geometry.

The eventual affected checks must prove that render/site identity and transforms come from the admitted Florida owner, canonical pose remains copied, absent/stale physical data fails closed, unrelated support/Solar routes retain behavior, no simulation work is multiplied by rendering, and the changed presentation preserves its applicable allocation/storage/timing limits. Then prepare the exact corrected route for a new Project Control manual review. No manual PASS is inferred from tests.

This ticket's targeted ownership question is settled; a presentation correction is required. No production/presentation implementation or permanent-test edit was made during this check. No new KSA method or rendering architecture was selected; this report only traces current NovaCore ownership.

## Verification and retained scope

- Seven candidate plus 475 other source/test/sample seals remain unchanged; Stage-5 physics and bars were not reopened.
- Manual log hash and acceptance correction are retained in [manual-result.json](manual-result.json).
- Independent read-only review confirmed classification A, missing recognizable site presentation, the separate Solar route, and the required placement/authority boundary.
- No application, solver, build, test, benchmark or profiler ran. No disposable output created: **0 files / 0 bytes**. No deletion attempted.
- Only concise evidence/status documents changed. HEAD/main/origin/main/remote main remain `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`; branch `codex/srv01-supported-contact-admission`; 67 historical tags unchanged; nothing staged or banked.

**REVISE — ROUTE/PRESENTATION FIX REQUIRED. Stage 5 engineering PASS; Florida manual PENDING; Stage 5 NOT FULLY QUALIFIED. Promotion paused. Stage 6 CLOSED. UNBANKED. STOP FOR PROJECT CONTROL.**
