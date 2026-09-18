# Stage 2 — player main-engine control

Judgment: **PASS — PROMOTE**. Responsibility: actual Z/X requests through the Stage 1 owner; READY waits for first admitted ignition. No attitude allocation or Florida departure.

## Current source and KSA convergence

Stage 1 current KSA binary/IL/live-history findings carry forward unchanged. Adopt requested state separate from realized actuation, independent controlled vehicle and viewport, and transient focus/reset lifecycle. Adapt explicit press-only Z=ON/X=OFF with same-boundary OFF dominance and deterministic bounded admission. Keybindings and tuning are NovaCore decisions, not copied KSA values.

## Implementation

Explicit `--scene=sol --player-flight-controls` and a separate launcher proving preset. Reject Florida, automatic benchmark/traversal, and conflicting startup contexts. The existing stock neutral Plan remains 128 intervals at 15,625 ticks: two physical seconds, 1x, zero gravity/contact, neutral gimbal, RCS OFF. A fixed inertial presentation offset places the finite demonstration in the Solar viewport without asserting an orbit.

Native foreground/focus checks collect only nonrepeat Z/X keydown edges; focus/capture loss clears pending edges. One callback consumes them. The application adapter deduplicates held intent, makes READY-only X a no-op, and uses the existing capability/identity/sequence ingress. Focus loss preserves already-admitted engine latch. Refusal retires service; terminal cannot restart. Space, auto-start and elapsed wall time cannot consume READY. First admitted ignition resets the wall baseline.

Meshes, exhaust and active-vessel focus use the same display placement. Earth/Moon/Sun/Mars focus and F never change the control capability. FREE remains deferred and HOME unbound.

## Validation and actual keyboard witness

Debug/Release native and managed builds PASS. Each configuration: **9,890 focused checks**, plus **50/50 campaign regression gates** in aggregate. Exact 128-interval live/recorded state/history comparison covers stores, powered duration, realized state, motion, mass, COM, inertia, revisions and clock; depletion, no feed, cutoff momentum, supported save/replay, READY, repeat/release, inactive focus and terminal are protected.

Computer Use (@oai/sky) exercised the visible isolated Release executable. The first cold run demonstrated indefinite READY, Z ignition, visible exhaust, resource decrement and terminal X/Z refusal. Tool roundtrip placed X after the two-second end, so it was correctly rejected rather than claimed as cutoff.

The second cold run executed a predetermined Z→X sequence, with a refreshed screenshot after each key. ON admitted at frontier 0; OFF admitted at frontier 35; OFF realized at frontier 36 with velocity X 0.4654607927885563 m/s. Fuel 29.957275390625 kg and oxidizer 44.9359130859375 kg remained unchanged through interval 128. Screenshots showed exhaust ON then absent while RUNNING, and terminal OFF. This is engineering keyboard proof, **not Project Control manual acceptance**. Concise exact traces and hashes: `keyboard.json`.

Warmed input/no-work/held path: **0 B over 20,000 calls**. Existing full live path and camera allocation gates pass. Journal remains 256×48 B; no additional unbounded input storage. Actual cutoff run display interval median/P95/P99/max: 5.5556/5.7244/6.031/7.794 ms. Service: 0.0035/0.1101/0.2327/6.0184 ms. These finite episode measurements include startup; Stage 6 owns full integrated distributions and repeating-tail qualification. No invented threshold.

## Red team and bounded correction

Independent source audit found one production compatibility defect: inserting the new preset before StockAssembly changed its persisted numeric enum identity. **One bounded correction** appended PlayerFlightControls=12, preserving StockAssembly=11. JSON numeric legacy identity and exact route tests protect it. Corrected independent source review PASS, subsequently discharged by all final regression gates and keyboard traces.

Test setup repairs did not change production: an inexact low-propellant fixture was replaced by exact matched binary fractions; Solar tests acquired the existing Florida survey fixture; the launcher runner was directed to its actual project, its catalog cardinality updated for the authorized row, and its existing standard-output path assumption respected.

That standard launcher test build refreshed three files in the existing Release launcher output. The initial restoration used the earlier free-camera correction candidate. Final campaign closure found that its DLL/PDB hashes differed from the later accepted warp-correction deployment; the EXE was identical. `launcher-build-provenance.json` preserves that initial action without relabeling it. `../launcher-provenance-reconciliation.json` records the two-file restoration from the later candidate, verified before and after against the accepted warp manifest. Final `../deployment-check.json` verifies all 88 deployed files. Triangle deployment was untouched; no controls candidate was deployed. This is build/deployment provenance repair, not a second production source correction.

## Promotion

All twelve promotion requirements are met. Stage 3 may proceed autonomously. No milestone, Git stage/commit/tag/push or banking action.
