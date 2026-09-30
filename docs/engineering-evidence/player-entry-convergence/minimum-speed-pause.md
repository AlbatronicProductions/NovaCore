# AD4 minimum-speed / pause boundary

Recorded 2026-09-29 America/New_York (qualification timestamps are UTC, 2026-09-30). **PASS for this bounded correction. No banking.**

The current Project Control ticket accepts the interface and player workflow the user actually exercised manually in the preceding AD4 candidate. This records that scoped acceptance and supersedes the preceding report's pending disposition only for those exercised items. It does not claim unperformed physical DPI, held-key or focus-loss cases, or user acceptance of this new correction. Project Control confirmation and a separate bank-preparation decision remain outstanding.

## Cause and correction

Ordinary comma input reproduced the defect: 1× reached 0.1×, but a second comma remained at 0.1×. `build/ad4-speed-boundary/before-second-comma.jpg` records that observation. The new native-message regression was first built against the unchanged command implementation and failed at `native comma at 0.1x requests authoritative pause` (`before.json`, five checks reached). Its generic failure step counter was then still zero; the assertion identifies the failing boundary.

The route is `DispatchPlayerKey` → `ExecuteCommand(slower/faster)` → `StepSpeed` → `SolarSystemScene.ApplyPresentationInput`. Previously the command always forwarded a rate decrement. The scene clamps its preset index at zero and therefore never requested pause at the minimum. The existing `SetUserPause` already routes exploration pause to the scene's authoritative `SimulationClock`; physical flight separately owns `physicalUserPaused` and `SuspendLive`.

Only `DesktopEditorForm.Commands.cs` changes normal production behavior in this correction. At exploration preset zero, decreasing now calls `SetUserPause(true)`; repeated decrease is idempotent. Increasing from paused preset zero calls `SetUserPause(false)` and retains 0.1×. A subsequent increase uses the existing ladder. Explicit Speed-menu preset selection uses the raw `StepSpeedPreset` helper so it retains existing pause semantics and cannot loop on a pause-only transition. Higher-preset increase/decrease while paused retains its previous behavior. Shifted comma/period remain aliases; Space remains unbound.

There is no second clock, zero-rate preset, UI-owned rate, new ladder, save-format change, physical slowdown, integration change or resource/debt-policy change. `PlayerTimeText`, the Speed-menu checkmarks and the existing pause caption consume the existing authority. The single upper-right speed indicator remains; the separate pre-existing centered `Simulation Paused` status caption is preserved. Raw Epoch and the retired transient speed notification are absent.

## Ordinary keyboard evidence

The final Release app was launched without qualification switches, through configuration and Start, in exploration at 3440×1440. The Computer Use plugin supplied normal OS key presses and clicks. These are observed ordinary UI interactions using automated input, not human hardware-key checks. Captures are unedited; all paths below are under `build/ad4-speed-boundary/`.

| Input / observation | Capture |
| --- | --- |
| Comma from 1× reaches 0.1× | `ordinary-minimum.jpg` |
| Next comma enters Paused | `ordinary-paused.jpg` |
| Repeated comma remains Paused | `ordinary-repeat-paused.jpg` |
| Period resumes retained 0.1× | `ordinary-resumed-minimum.jpg` |
| Next period reaches existing 1× | `ordinary-realtime.jpg` |
| Shift+comma twice reaches minimum then Paused; Esc opens menu; period is suppressed; clicking Resume retains Paused | `ordinary-esc-resume.jpg` |
| Shift+period resumes 0.1×; Space leaves it running at 0.1× | `ordinary-space-unbound.jpg` |

The ordinary run closed normally with `Session ended; exitCode=0` and `GPU_MEMORY_CLOSED timer=stopped renderer=retired sharedInstance=destroyed`. Its copied log and session metadata are included in the new receipt. No task test app remains running. Captures establish visible behavior; authoritative epoch proof comes from the instrumented regression below.

## Authority, pause owners and lifetime

`DesktopEditorForm.SpeedBoundaryQualification.cs` is an opt-in, 20-phase regression across real rendered frames. It posts native keyboard messages, measures the actual scene/flight epochs, and also invokes selected commands directly to isolate ownership. `final.json` reports **46 checks passed**, `hardwareKeys=false`, `realOsDpiTransition=false`.

| Boundary | Final result |
| --- | --- |
| Minimum → pause → repeated comma | Scene epoch remains exactly `844002170864909`; preset remains zero; authoritative user pause is true while frames continue. |
| No paused-time catch-up | A direct 3600-second host sample during pause advances zero ticks. After resume at 0.1×, a supplied 10-second host sample adds exactly 1,000,000 simulation ticks (one second). |
| Esc and Resume | Menu hold coexists with user pause. Closing the overlay preserves user pause and exact epoch; period under the overlay queues no later rate command. |
| Unrelated menu owner | Increasing through the command clears only user pause. The open menu's hold remains true and epoch remains exactly `844002173966036`. |
| Speed menu agreement | Paused and the retained 0.1× preset are checked together. Selecting another preset preserves pause; higher-preset faster/slower preserve existing semantics. |
| Text/menu input isolation | Period sent to the open menu is suppressed. A visible modal TextBox receives punctuation without changing gameplay state; closing it queues no command. A visible, enabled craft-name control with verified native focus rejects gameplay dispatch. |
| Editor and craft save/reload | Same exploration scene owner, exact epoch, pause and preset survive editor entry, byte-exact draft save/reload and editor return. |
| Exploration persistence | There is no exploration-session save contract; SAVE NEW FLIGHT is disabled without a physical flight. No cross-session pause persistence was invented. |
| Exploration → physical launch | Existing launch creates a fresh physical presentation at 1×, unpaused. This intentional reset is asserted; exploration pause/rates are not carried into flight. |
| Physical pause and editor return | Comma, period and Space leave physical pause and epoch unchanged; unsupported rate commands remain refused. Editor return retains physical user pause. |
| Physical save/reload | Existing restore intentionally clears UI pause and starts at 1×, with the exact saved physical epoch; subsequent physical advancement is healthy. |

The clock stores pause separately from rate. `SimulationClock.PrepareHostAdvance` returns for pause before adding host credit or mutating rate remainder/debt. The presentation's independent host hold covers editing, loading, overlays, menus and physical user pause. This correction does not change those owners. Focus loss currently suppresses/clears native input; it does not own a separate automatic simulation pause. That existing contract is preserved, not redefined. Physical held-key/focus-loss permutations were not exercised here.

The qualification copies the prior task's 12-part witness into its isolated `craft-library`, then writes only test roundtrip files there and a flight roundtrip beside the receipt. The player witness remains unchanged (SHA-256 `eeda6331dc928e4ea38f4e5e18e3dd2d17ea928763a3e917adf75c0e1e057b94`).

## Validation and preservation

| Check | Result |
| --- | --- |
| New regression against pre-fix command | Expected FAIL at authoritative pause boundary; `before.json` |
| Final managed App Release build | PASS, zero warnings/errors; `build.log` |
| Final speed-boundary native integration | PASS 46, normal exit 0, empty stderr; `final.json`, `final.stdout.log`, `final.stderr.log` |
| Player configuration/time/GPU-memory presentation | PASS 55; `player-tests.log` |
| Filtered Graphics: Sol system presentation and focus | PASS, exit 0, exact time/rate and zero HUD allocation; `solar-test.log` |
| Independent verifier | PASS, no blocker in command, pause or lifecycle ownership; reviewed final source and 46-check evidence read-only. |

The independent verifier specifically checked separation of raw preset selection, minimum-boundary pause, higher-preset pause preservation, actual visible-control focus, menu holds and intentional physical reload reset. Its final caveat was to bind the current source and managed DLL to the receipt and retain hardware/DPI and Project Control limits. Those limits remain explicit here.

Reproduction: `dotnet build tools/NovaCore.App -c Release --no-restore`; `dotnet run --project tests/NovaCore.Player.Tests -c Release`; `dotnet run --project tests/NovaCore.Graphics.Tests -c Release --no-build -- --case="Sol system presentation and focus"`. For native integration, create a fresh evidence directory with `craft-library/fixture.craft.json` containing the cited witness and run the Release `NovaCore.exe --qualify-editor <evidence-directory>/final.json --qualification-tank speed-boundary`. Run only one interactive test app at a time. Ordinary reproduction uses no switches and follows the keyboard table.

`minimum-speed-pause-receipt.json` records the current source snapshot, every file in the Release app package, logs, captures, test outputs, deleted-source state and this report. Comparing all 268 sources from `editor-restoration-receipt.json` finds only the command file and qualification dispatcher changed; the new speed-boundary qualification file is added. The remaining 266 sources match. The native DLL remains `82f13c237fda69089156acbc375ffbdc546b7763a46232b5163e8e63dbcb16ed`; the final construction-editor DLL is `e7461c802a689ccdb636672ca4edd972b344f748d6a56c53f60791af7d8724bd`.

Prior broader construction/flight results remain evidence for the preceding candidate, not reruns claimed by this ticket. Configuration/loading, memory reporting, viewport, restored editor and accepted physical behavior are preserved by the source comparison and targeted checks. The current receipt is local evidence, not a release seal. HEAD remains `40314c0f72396ea5f4ff39121f0b6821f1f96146` on `main`, with an empty index. No commit, tag, push, bank or next production front was performed.
