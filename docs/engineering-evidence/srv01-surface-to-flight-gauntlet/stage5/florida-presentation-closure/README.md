# Florida presentation integration and pad-placement boundary

## Current disposition

**STOP FOR PROJECT CONTROL.** Manual acceptance is failed/pending. The requested middle-cube-top placement is not implemented. Stage 6 is closed, no milestone is assigned, and all work remains unbanked.

The original Stage-5 graded-ground engineering result remains valid for its original support domain. It is not qualification of the elevated facility top. Project Control observed the newly connected Florida facility and spacecraft, then correctly rejected the spacecraft's ground placement near the orange arm. No visual-only relocation was made.

## Proven placement chain

1. `StockAssemblyDevelopmentScene.PrepareFlorida` acquires the actual body-6 terrain query and passes **48 m east** to `AssemblyFloridaSite.Create`.
2. `AssemblyFloridaSite` requires `IPhysicalGradingProofSource`: the canonical terrain composition must reduce to the existing full-weight grading plane. Its corner queries certify that ground plane, not a facility surface.
3. `FloridaFacilitySupport.Region` defines ground-plane height **15.134892258793116 m** above the reference radius, with East/Up/North supplied by the existing authenticated anchor.
4. The site material-origin embedding is `Up*(R + Hground + 1.7) + East*48 + [East,Up,-North]*pLocal`. The support plane at local Y=-1.7 therefore maps exactly to that ground.
5. `LocalContactConfiguration.TryCreateAssembly` prepares the existing support with half extent 8 m: **16 x 16 m**. The qualified upright material origin is local zero; the COM conversion is separate and is not a missing placement offset.
6. Existing `FloridaLaunchSite` supplies the unchanged facility root at height **21.970461536198854 m**. `AuthoredFacilityGeometry.h` defines the middle box at ENU minimum `(-7,-7,1.5)`, maximum `(7,7,8.5)`. Its top is **14 x 14 m**, centered at East=North=0 and height **30.470461536198854 m**.
7. The new display uses the actual existing terrain-v5/NCSM1 and facility owners. Both spacecraft and terrain use a fixed Earth-relative display embedding at the episode's ready epoch. It faithfully shows the admitted ground location, which does not satisfy the requested pad-top placement.

| Quantity | Currently qualified | Requested middle-cube top |
|---|---:|---:|
| East / North from site anchor | 48 / 0 m | 0 / 0 m |
| Support-plane height above reference radius | 15.134892258793116 m | 30.470461536198854 m |
| Material-origin height at initial local zero | 16.834892258793116 m | 32.170461536198854 m |
| Surface extent | 16 x 16 m certified ground patch | 14 x 14 m authored top |
| Physical admission | terrain grading capability | none currently found |

Required displacement of the material origin would be:

`delta = -48*East + 15.335569277405738*Up metres`.

The axes agree; there is no demonstrated axis swap, doubled site transform, omitted COM conversion, or incorrect facility transform. The wrong responsibility was admitted for the intended visual placement: terrain ground rather than the authored middle-box top.

## Why this stops at a physical admission boundary

`IPhysicalGradingProofSource` explicitly attests terrain composition, not a replacement collider. `SurfaceGeometryId` is explicitly renderer-owned. Searches of Core and Simulation found no qualified Florida middle-pad-top spacecraft contact owner; `FloridaContactProvider` also uses the grading-plane contract. The existing facility geometry is consumed by raster geometry and light occlusion. Authenticated anchoring does not qualify its top for spacecraft support.

Moving only the drawing would falsify physical placement. Changing east to zero alone would put the craft under the cube, still on ground. Raising the canonical origin while retaining the ground capability would make the support witness false. Reusing the 16 m private slab on a 14 m top also requires a finite coverage proof; blindly translating it would supply nonexistent support outside the top. Finally, the actual origin enters gravity and rotating-frame terms, so exact old trajectory bits cannot be asserted for a relocated physical episode.

The smallest proposed next scope is an explicit, separately identified admission of the **existing** middle-box top, consuming unchanged authored geometry and anchor with a finite participating-contact coverage proof. Then place both canonical and visible spacecraft from that owner and requalify with unchanged bars. No solver tuning, terrain/facility redesign, spacecraft geometry change, or Stage-6 behavior is proposed. This scope is **not implemented** by this record.

Two independent read-only reviewers separately reached this boundary conclusion. No new KSA responsibility opened.

## Presentation work retained

- Route-specific NCSM1 selection with existing generation/quality guards.
- Combined assets-plus-reusable-mesh native forwarding entry; old entries unchanged.
- Fixed Earth-relative display embedding consuming copied site-local canonical poses; no Solar host-time advancement, focus/time controls, or second physical clock.
- Original pad and foundation geometry retained unchanged; qualification mesh 6 absent from Florida, retained in other support scenes.
- Cold render/site authority comparison and permanent site/frame/rotation/nonmutation checks.
- Lighting remains at the ready epoch; the display is not presented as current inertial state.

No physical source, facility geometry, terrain assets, spacecraft assets, solver settings, resource values, or contact bars were changed during this correction. Native edits only add the combined entry declaration/forwarder. Source comparison verified **232/232 Core and Simulation files unchanged**, with **475/482** captured C# inputs unchanged; the seven differences are authorized sample/graphics/interop/test inputs. Native header and implementation are additional presentation changes. The original scene/native predecessor inputs are retained for reproduction.

## Validation completed before the pad-placement stop

Final Debug and Release full-solution builds: zero warnings/errors. Final Debug and Release focused Florida presentation checks: PASS, including first/final orientation and translation checks, exact site seal, and canonical nonmutation. All 1,200 motion-history entries retain hash **F8D69B7E49FFFD20**, with exact 1,200 revisions/history entries, 20,000,000 ticks, 705 kg, 30 kg fuel, 45 kg oxidizer, engine/RCS OFF. Warm craft/facility submission and servicing: **0 B**; shared positive control: **152 B**.

Earlier in this same correction, Debug/Release Florida qualification, presentation storage, original assembly presentation and powered-support regressions passed. Retained scene/native/mesh storage was **6,469,144 B** in the Release measurement (managed 4,634,912; native pool 475,136; mesh GPU payload 1,359,096), below 8 MiB; shared terrain/device resources excluded and reported separately in live context. That storage run preceded the later cold identity guard and explicit test generation selection; do not call it a final pad-top storage qualification.

An initial new test failed because its independent oracle omitted the existing gimbal mesh pivot. The oracle was corrected, with no runtime change or tolerance relaxation. A later cold identity guard initially referenced a nonexistent public dataset property and failed compilation; it was replaced with the existing public physical-query acquisition/authority comparison. Final builds pass. Historical first outputs are retained in concise validation results, not rewritten as passes.

Broader pending presentation/launcher validation and manual-route preparation were stopped when Project Control rejected pad placement. No full qualification or manual PASS is claimed for this candidate.

## Bounded live characterization (ground placement only)

Two fresh Release processes, 4,800 frames each, 960 x 540, RX 6800 XT/Vulkan 1.4, original terrain quality. Both reached 1,200 publications and the exact final stores. These runs are observations of the ground-placement candidate, not pad acceptance.

| Process | Live display median | P95 | P99 | Maximum | Max contact service |
|---|---:|---:|---:|---:|---:|
| 1 | 5.5540 ms | 5.7757 ms | 6.1931 ms | 685.2025 ms | 60.7150 ms |
| 2 | 5.5588 ms | 5.7992 ms | 6.2224 ms | 117.6839 ms | 69.5613 ms |

Native whole-run frame p50/P95/P99/max: process 1 **5.504/5.853/6.160/83.311 ms**; process 2 **5.477/5.887/6.321/95.253 ms**. GPU average totals **2.840/2.534 ms**; average CPU host callback **0.167/0.173 ms**, fence wait **4.893/4.832 ms**. Regional physical residency allocated **119,737,728 B**, ceiling **124,895,232 B**. Cold regional dependency-delay maxima **645.493/666.242 ms** are existing instrumentation context, not attribution of either display maximum. Process 2 included one read-only window capture; no claim that this was measurement-neutral. Process 1 preceded the cold identity guard/header declaration, with the same hot path.

The large display maxima remain **UNATTRIBUTED** by these aggregate captures. No contact forensics or optimization campaign was opened, and no completed warmed-tail causality judgment is claimed after the pad-placement stop. The captured image corroborates ground placement near the orange arm; it is not manual acceptance.

## Git and cleanup

HEAD/main/origin/main remain `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`; branch `codex/srv01-supported-contact-admission`. Nothing staged, committed, tagged, pushed or banked. All 67 historical tag refs match the entry digest `4EABB8A142C30BA4FB1F969B4FBDCB9D55892383B8D226F64A34076783DCE54A`. `git diff --check` and cached check pass. Pre-existing unrelated dirty/untracked work is retained.

The one new disposable root is `E:\NovaCore\build\srv01-stage5-florida-presentation`: **1,708 files / 367,055,344 bytes** at closeout. It contains isolated native/managed builds, logs and read-only capture helpers. Nothing deleted; retained for reproduction pending Project Control. Existing earlier-ticket disposable paths were not touched. Exact reviewed cleanup and non-destructive verification are in [reproduction.md](reproduction.md) and must also be printed in the final report.
