# AD4 editor restoration and time display consolidation

Candidate work on 2026-09-29, based on `40314c0f72396ea5f4ff39121f0b6821f1f96146`. Unbanked. Project Control retains visual/player acceptance. This supplements the preceding AD4 evidence; its earlier seals do not qualify these later edits.

## Proven cause and ownership

The ordinary exploration → Esc → New Vehicle route activated construction and its session, but displayed only the construction scene and origin marker. Native capture: `build/ad4-editor-restoration/before-editor.jpg`. AD4 had expanded the viewport to the full client while leaving construction controls underneath it. The banked predecessor's docked panels had avoided that composition conflict by subtracting their area from the renderer. Merely retaining control objects and invoking `PerformClick`/posting native messages never tested whether Windows would actually deliver a player's click to them. That is why the previous 588-check construction result missed the visible failure.

`DesktopEditorForm.EditorPresentation.cs` now owns one styled, overlaid catalogue and construction panel layout. The existing domain session, authored assets, command operations, preview renderer, persistence, compilation and flight handoff remain authoritative. `SetEditorPanels`, layout and modal return raise the active panels over the unchanged full-client native viewport. The palette provides actual categories, root-only discovery for an empty draft, thumbnails, selection state, symmetry/rotation and input help. Inspector controls remain scrollable at small supported extents. Optional Frame Statistics moves to the upper right in construction so it cannot cover the catalogue header.

The unused legacy navigation panel, list catalogue, intent ComboBox and clock ComboBox, with their subscriptions and independent visibility responsibility, are retired. Plain construction intent/rotation state feeds the existing commands. There is no hidden alternative presentation. The actual gesture remains click catalogue card → hover a compatible socket/see its preview → click to attach. Right drag orbits; middle drag pans; wheel zooms. UI capture is invalidated on a new held part, and the input consumer refuses scene/camera input inside visible editor panels. This does not implement drag-and-drop from the catalogue or change construction compatibility.

The old time notification had two owners: `SolarSystemScene`'s temporary packed speed/timer publication, and a native `solarSpeedHudPipeline` with separate shaders/draw/destruction. Both presentation paths and shader build dependencies are removed. The ABI field remains reserved for layout compatibility and the separate upper-bit surface diagnostic. The existing upper-right managed HUD calls `PlayerTimeText` using the active authoritative state: physical flight 1× or paused; exploration's existing preset or effective pause. Raw Epoch is absent. Simulation stepping, epoch, rate ladder and physical admission are unchanged; Space remains unbound for gameplay pause/staging implementation.

## KSA responsibility comparison

Installed read-only KSA: `2026.9.22.5482+40faabcc2a54baeb11b7b9f21beade6f7e6b524e`, DLL SHA-256 `CE43D022E7DAC9BEB2352B6106164BC1F521DDA1AE9D0B898B8DBA0C59A69723`.

- **ADOPT:** editor-owned persistent palette visibility, one presentation registration, input capture before camera handling, scene rendered before UI composition. The installed `PartWindow.IsShown` follows editor existence; grid visibility is separate from the remaining tools.
- **ADAPT:** KSA palette activation spawns a grabbed part, with release/focus handoff and authored connector feedback. NovaCore uses its existing click-selection/hover/click-commit commands and native preview, preserving transactions and supported gestures.
- **INTENTIONALLY DIFFER:** NovaCore separately validates structural fit, function and launch admission; showing a compatible connector does not promise a physically admitted craft. No KSA assets/source copied.

The supplied 81.578-second, 3440×1440 video (`E:\Videos\2026-09-28 21-45-06.mp4`, SHA-256 `8B4B9E2C21683AE231C8EF9F48527AE21368BDCCF4B62CFCA383459E7F4D6638`) supplies visual evidence at 68/71 seconds: charcoal catalogue, category controls and separate camera/launch panels. Those frames alone do not prove the interaction sequence; the activation/release ownership above is installed-code evidence.

The established authenticated official-history inspection from 2026-09-29 through revision 5523 is reused, as permitted by the ticket. [The preceding report](README.md) records direct message links: 4996 contextual Esc/save, 5007 modal dismissal, 5047 modal Settings, 5202 persisted HUD contexts, 5334 editor game-save refusal, and 5517 later Speed availability. These support responsibility boundaries, not NovaCore behavior. During the final refresh the automation-visible Discord tab still displayed its login form after the user's sign-in reply; no new authenticated-history read is claimed.

## Qualification distinctions

Permanent integration checks now require the actual `WindowFromPoint` target for a visible/enabled catalogue or button and for scene gestures. Negative tests deliberately occlude/disable a card, and inject stale scene input under the UI, to verify refusal. Frame Statistics is exercised with the catalogue active. Synthetic `PerformClick` omits the physical release after capture cleanup; the engineering driver supplies that release before a distinct scene gesture. Earlier camera-test coordinates fell under the newly visible palette and are migrated to verified exposed scene positions, without relaxing production input isolation.

## Ordinary player walkthrough

The final Release app was launched without qualification switches (PID 21304, HWND 8850602) at 3440×1440 borderless fullscreen. Native Windows input operated the controls; no internal construction commands supplied this walkthrough. Captures below live under `build/ad4-editor-restoration/`.

| Player operation | Observed result / capture |
| --- | --- |
| Configuration → Construction / New vehicle → START | Root-only catalogue and construction tools visible; `startup-editor.jpg` |
| Save / Load → select own saved four-part draft → Load selected | Command core, short tank, adapter and engine restored; `restored-editor.jpg` |
| Launch four-part craft | Correctly refused for missing balanced attitude authority; domain validation preserved |
| Attitude palette → block → symmetry 8 → highlighted tank socket | Eight blocks attached in one transaction; 12 parts; Undo returned to four, Redo restored 12 |
| Fill consumables → Save / Load → overwrite own named draft → confirm | 800 kg propellant; saved craft `AD4 Editor Route 20260929`; `built-and-saved.jpg` |
| Save / Load → Load selected | Exact 12-part design visibly reloaded; `reloaded-editor.jpg` |
| Launch vehicle | Supported physical spawn; editor panels disappear; upper-right `Flight · 1×`; `ordinary-flight.jpg` |
| Esc → New Vehicle | Retained 12-part design and palette restored; `retained-editor.jpg` |
| Editor Esc → Exit Editor; File → Build New Vehicle; Return to flight | Flight, retained editor, then flight restored through all offered routes |
| Period in physical flight; Esc menu | Rate remained 1×; menu showed `Flight · Paused`; `flight-paused.jpg` |
| End only the task's test flight → exploration; period then comma | Single upper-right `Exploration · 2×`, then `Exploration · 1× (Realtime)`; no raw Epoch or temporary speed popup; `exploration-speed.jpg`, `exploration-realtime.jpg` |

The saved design is `C:\Users\Tyler\Documents\NovaCore\Craft\AD4 Editor Route 20260929.craft.json` (revision 8, 12 instances, 11 connections, one eight-member symmetry group). The earlier four-part version was also built by ordinary catalogue/socket interaction. This task updated only its own named witness save. The final ordinary process exited normally: `Session ended; exitCode=0`; telemetry reported timer stopped, renderer retired and shared instance destroyed. No test app remains running.

## Final-source regression results

| Check | Result / evidence |
| --- | --- |
| Native Release and managed App Release builds | PASS; managed 0 warnings / 0 errors; `build.log` |
| Player configuration, time text and GPU memory presentation | PASS 55 checks; `player-tests.log` |
| Filtered Graphics: Sol system presentation and focus | PASS, exit 0; exact rate/epoch and zero HUD allocation evidence; `solar-test.log` |
| Application construction integration | PASS 675 checks, `construction-final.json`; final native SHA-256 `82f13c237fda69089156acbc375ffbdc546b7763a46232b5163e8e63dbcb16ed` |
| Modular editor regressions | PASS, exit 0; engine 9898, attitude 8264, integrated 3007 checks, native lifecycle/input and active-vessel regressions; `editor-regressions.log` |

The integration result deliberately remains `playerPass=false`: its 675 checks do not replace the ordinary walkthrough or Project Control acceptance. It covers actual topmost hit targets, deliberate occlusion/disable rejection, UI/scene isolation, compatible/refused previews, symmetry, rotation, delete/cancel, undo/redo, consumables, exact save/reload, launch refusal/admission, native input, modal/dropdown routing, retained draft/physical owner, window/child/renderer lifetime, full-client extent and repeated construction/flight transitions. Momentary input cleanup remains separate from persistent engine/throttle commands.

Earlier construction attempts remain local diagnostic evidence: the first needed an explicit synthetic release after capture cleanup; subsequent positive camera test coordinates incorrectly fell under the now-visible palette and were moved to verified exposed scene positions. The final run supersedes those failures and the preceding 675-check run before the Frame Statistics guard. The broad Graphics run was cancelled and is **not** claimed as a full-suite pass.

The final integration's three warmed 120-frame windows measured the following absolute costs. `cpuMs` and allocation scope are the existing managed editor-frame measurement, not total process CPU/GPU cost; frame intervals include native presentation.

| Window | Frame median / P95 / P99 / max (ms) | Managed CPU median / P95 / P99 / max (ms) | Allocations / GC |
| --- | --- | --- | --- |
| 1 | 5.5619 / 5.8725 / 5.9444 / 6.1216 | 0.0062 / 0.0305 / 0.0400 / 0.0919 | 0 bytes / 0 |
| 2 | 5.5523 / 5.8644 / 6.0130 / 10.8086 | 0.0055 / 0.0251 / 0.0347 / 0.0361 | 0 bytes / 0 |
| 3 | 5.5568 / 5.8760 / 5.9563 / 6.0410 | 0.0053 / 0.0217 / 0.0343 / 0.0454 | 0 bytes / 0 |

Active-vessel managed camera regression: median 0.0443 ms, P95 0.0446, P99 0.0487, maximum 0.0822, zero allocations/collections; F-refocus median 0.0143 ms, maximum 0.0270, zero allocations. These are preservation measurements, not a matched before/after speedup claim. No GPU-idle wait or new rendering backend was introduced.

Reproduce from the repository with Release builds, `dotnet run --project tests/NovaCore.Player.Tests -c Release`, filtered Graphics `--case="Sol system presentation and focus"`, and the Triangle `--modular-editor-regressions` route. Application integration uses `NovaCore.exe --qualify-editor <new-output.json> --qualification-tank nc.tank.short-2`; qualification storage should remain separate from player saves. For ordinary evidence, launch `NovaCore.exe` with no switches and follow the route table.

## Judgment and remaining acceptance

**PASS for the bounded engineering correction and observed ordinary player workflow.** Independent verifier review attacked visibility, actual Windows hit testing, ownership retirement, input routing, modes and construction preservation; its final source verdict was PASS with no production blocker. The KSA reference reviewer corroborated editor/palette registration and input lifecycle independently. Neither reviewer grants Project Control acceptance.

Current automated extent coverage includes 1100×740, 1280×720 and fullscreen transitions; the ordinary capture route used 3440×1440 at the current 96 DPI. Physical monitor-DPI changes and physically held-key/focus-loss permutations are not newly qualified by synthetic input checks. Final visual/player acceptance, those hardware checks, and any broader physical-flight acceptance remain with Project Control. The ordinary launch witness proves supported admission/return, not a new flight envelope or time-warp capability.

Configuration/loading, the authoritative adapter-matched memory provider, readiness, recorder admission, craft validation, full viewport and accepted physical/rendering owners remain in the AD4 candidate. The later loading-placement decision remains intact. No shadow, texture, staging, time-warp or unrelated gameplay backend was added. HEAD and the empty Git index remain unchanged; no commit, tag, push or banking occurred. `editor-restoration-receipt.json` records current source, binary and evidence hashes separately from the earlier AD4 seals.
