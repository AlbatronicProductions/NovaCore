# Launch refusal diagnosis and recovery — 2026-09-26

**Bounded engineering correction; feature freeze; unbanked.** Final qualification
is recorded below. No Player PASS, commit, tag, push or milestone is assigned.

## What happened and causal owner

The supplied `2026-09-26 12-23-44.mp4` shows two different invalid configurations
behind the same generic `Craft launch requires functional services and valid
physical support.` message. Its SHA-256 is
`a41ddc616da545a30f504a7c79fa217701aec0ccc9f39c00a3ca69a622ab4e70`.

| Witness | Actual failing predicate | Consequence |
|---|---|---|
| Two long tanks, two eight-block RCS rings, 21 parts; filled | `ATTITUDE_COUNT` (64 jets; existing profile admits 1–32), with consequential `ATTITUDE_AUTHORITY` | Compiled readiness refuses before Florida/spawn/support admission. Filling cannot repair the hardware profile. |
| Later one-long-tank, one-ring craft, saved as `Test 1`, 12 parts | `POWER_PATH` and `FUEL_PATH`; zero battery charge and propellant | An isolated filled copy passes exact-source compiled launch admission. Original save bytes are unchanged. |
| Video's later load attempt; `UX-Review-20260922` | Retired part-definition revision | Correct identity refusal, independent of the launch/readiness complaint. No silent save migration. |

The failure was in **launch feedback and preparation recovery**. The admission
owner discarded available compiler reasons in favor of a generic exception;
the desktop presenter placed that exception in a small status area. Successful
explicit filling did not clear the stale refusal. Freshly placed stores/batteries
are intentionally empty in the existing configuration contract.

Before edits, every file in the final Florida source seal and canonical package
matched its recorded hash. Direct ordinary UI reproduction in that unchanged
canonical Release built a 12-part short craft, reproduced the generic refusal,
filled it, observed the stale error, then successfully reached the single-layer
Florida pad with `SUPPORTED` feedback. This disproves the hypothesis that the
Florida revision made every valid craft unlaunchable. The baseline binary probe
also passes filled short, long and two-short compiled readiness, while rejecting
the 64-jet witness. Prior successful stabilization routes explicitly filled their
craft and used one RCS ring; they did not qualify fresh-empty refusal feedback.

There is no evidence of corrupted package/source identity, broken connected
service routes, stale lifecycle ownership, or changed pad geometry in this
incident. The observed generic refusal occurs before physical support is tried.
Existing support admission separately rejects overloaded but service-valid
articles; that refusal is now exposed as its own reason.

## Implementation

- `CraftLaunchAdmission` preserves the exact same predicate and source/catalog/
  document/compiled identity checks. It formats the actual failed diagnostics,
  consolidates repeated resource messages and reports the actual attitude-jet
  count. The redundant allocation-authority message is omitted only when the
  count already violates the profile. No diagnostics or digests are modified.
- `DesktopEditorForm.Launch` presents a readable, scrollable launch-details
  dialog. Preparation and physical scene construction complete before replacing
  the live flight owner. Refusal preserves the draft, history and existing flight.
- Explicit **Fill consumables** clears the old error and confirms filling and
  charging, while saying Launch will recheck services and physical support.
  Launch never fills or enables resources implicitly.
- Permanent readiness checks and opt-in canonical application checks cover
  empty/refill recovery, partial and disabled resources, broken explicit service
  connections, save/reload/undo/redo, excess jets, and overloaded support.
  Retained authority is observed as `Ready` after refused replacement.

Native renderer, catalog/assets, physical support laws, max-capacity load
accounting, contact/spawn placement, Florida authority, compiler service routing,
resource defaults and profile limits are unchanged. The exact bounded patch is
retained in `change.patch`; `identity.json` records before/after source hashes.

## Qualification

| Final canonical Release route | Checks | Frames | Judgment |
|---|---:|---:|---|
| short | 557 | 10856 | PASS |
| long | 557 | 10872 | PASS |
| two-short | 561 | 10920 | PASS |
| construction | 1026 | 3040 | PASS |

Debug and Release solution builds pass; final application/dependency rebuilds
pass with zero warnings/errors after the presentation adjustment. Native builds
are unchanged and current. All 34 regression commands pass: 1,070 connector
checks per configuration, 43 new readiness checks each, 223 capacity checks each,
explicit service/compiler/persistence gates, physical gates 9–12, Florida support/
rotating-Earth, reference frames and camera. Each configuration passes 126
CPU/GPU facility visibility comparisons with explicit validation. Package
verification passes: 127 files and 66 production shaders.

The final native identity remains
`61a1832895bdcff54d2ee727e63a6d942202502e2ab6bd3aea5539c77cdd7c99`.
Final package SHA-256: `8bad01bb3a569465c24fb4466cb0ce039e3ec295d2d151cc157f5b239a5958ed`.
Commands, concise measurements and provenance are retained in `regressions.json`,
`native.json`, `native-witnesses.json`, `builds.json` and `canonical-package.json`.
Native frame/CPU median, P95, P99, maximum and allocation windows are recorded;
this feedback correction makes no new renderer performance claim.

Native qualification exercises the normal canonical Release executable with
isolated save directories: fresh construction, refused empty launch, explicit
fill, exact save/reload, supported Florida spawn, ignition, physical attitude
demand, cutoff, return/resume, refused replacement, and display/input transitions.
It checks the same immutable slab/caster over game, editor, game-return, flight
and flight-return. These are integration checks, not manual Player acceptance.

The CPU capacity suite covers mixed depth through 96 tanks, 901-part dense
presentation, exact 1,024-part capacity and immutable over-capacity refusal.
The native construction route checks deep placement/capacity; it does not launch
arbitrarily deep articles. The authored adapter admits the two-short-tank witness
but refuses three or more full-capacity short tanks. One long tank is admitted;
two long tanks are not a newly qualified support configuration. Partial filling
does not bypass the existing conservative maximum-capacity load law.

Independent read-only review found no blocking production defect. Its specific
retained-owner observation gap was corrected with an actual `Ready` authority
observation. Native handler checks prove modal contents; the separate ordinary
UI observation below establishes readable on-screen presentation.

Direct final ordinary UI reproduction loaded the original **Test 1** at native
3440x1440 BorderlessFullscreen (3440x1322 render viewport inside the app).
The fuel/power reasons appeared as separate readable paragraphs. Filling set
1600 kg propellant and cleared the stale error; Launch reached the single slab
with `SUPPORTED` status. Return/resume retained the supported craft. The
temporary fill was undone before closing. Both original user saves retain their
previous hashes. `direct-ui.json` records actions and exact application log
provenance. The diagnostic short craft created before edits was moved out of the
user library to `build/launch-admission-regression/direct-before.craft.json`;
no user craft was saved over or migrated.

## Reproduce

Use the documented Windows toolchain, resolved LFS assets and verified terrain
packages. Build native Debug/Release and the managed solution in both modes with
`--no-incremental`, then run from the repository root:

```powershell
python docs/engineering-evidence/launch-admission-regression/qualify.py --output build/launch-readiness-rerun/regressions
python docs/engineering-evidence/launch-admission-regression/qualify.py --native --output build/launch-readiness-rerun/native
python tools/verify-player-package.py --output build/launch-readiness-rerun/package.json
```

The runner records commands, exit codes, log hashes and concise results. It sets
only a child-process `VK_IMPLICIT_LAYER_PATH` to an empty task directory because
this host has stale Epic implicit overlay manifests. Explicit Vulkan validation
remains enabled. This is the same existing qualification accommodation, not a
driver/registry/settings edit. Run native window routes sequentially.

For ordinary UI reproduction, open
`tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`, enter New vehicle,
place a command core, short or long tank, adapter, main engine and one eight-block
RCS ring. Launch while empty: read the actual fuel/power blockers. Return to
construction, Fill consumables, and Launch: reach supported Florida flight.
Use two short tanks with one ring for the deeper admitted witness. Use a second
ring for the excess-jet refusal; a three-short-tank stack with one ring exercises
the separate support-load refusal. No original user save needs overwriting.

## Disposition and next action

**PASS — bounded launch feedback/recovery correction.**
**REJECT — hypothesis that the Florida revision invalidated all valid launches.**
Unsupported and invalid craft continue to refuse. Engineering/integration PASS
does not assign Project Control acceptance, Player PASS or banking.

The next action is Project Control review of this bounded correction and the
documented limits. No banking or new capability front is authorized. Increasing
jet count or arbitrary-depth launch support would be separate architecture work,
not a legitimate fix for this feedback defect.
