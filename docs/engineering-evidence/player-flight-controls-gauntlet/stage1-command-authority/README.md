# Stage 1 — canonical live command authority

## What happened / why it matters

Added simulation-owned live admission inside the existing assembly transaction engine. An application request cannot write physical state, resources, actuator realization or clock. The immutable recorded Plan remains unchanged; in live mode its already-qualified interval schedule and finite horizon remain authoritative. One resolved compiled command reaches the same physical evaluator and canonical publisher used by recorded mode.

## Current KSA convergence

Fresh installed binary: `E:\Kitten Space Agency\KSA.dll`, version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. Read-only specialist freshly confirmed method IL witnesses unchanged from the preceding [command review](../../post-camera-command-review/ksa.md).

Authenticated live history was freshly re-read: [buffered vehicle input](https://discord.com/channels/1260011486735241329/1260112103134724146/1494460516981014589), [computer update cadence](https://discord.com/channels/1260011486735241329/1260112103134724146/1500991990433583304), [held-input resets](https://discord.com/channels/1260011486735241329/1260112103134724146/1533672827256049807), [independent viewport/control ownership](https://discord.com/channels/1260011486735241329/1260112103134724146/1544008206777323561). No superseding installed mechanism was found. ADOPT the requested-state/worker/actuator/publication separation; ADAPT stronger deterministic admission, bounded journal and stale-capability refusal to NovaCore. KSA's transient direct-reference event buffers do not supply those latter guarantees.

## Implementation

- Logical control identity is the launch's canonical spacecraft ID plus persisted generation 1 for this single, nonrenewable session. A separately minted, nonserialized owner-bound capability prevents old/fresh/restored owners with equal numeric identities from exchanging input.
- Fresh request latch is OFF. Pilot demand is not implemented in this stage. Only main ON/OFF synthetic payloads are admitted.
- Owner selects the current unprepared frontier; callers cannot select or backdate it. Outstanding proposals refuse admission. Accepted records hold identity, owner sequence, frontier, accepted host-credit sequence, requested payload and effective latch.
- Retry sequence must equal the next owner sequence or an earlier identical admitted request. Historical duplicate receipts do not reset current state. Changed duplicates/gaps/foreign IDs/generations/capabilities refuse atomically.
- Records append immutably to at most 256 entries. Same-frontier OFF dominates ON; that arbitration expires when the physical frontier advances. READY-only cutoff behavior belongs to Stage 2 and must not create a veto that prevents first ignition.
- Physical owner retirement revokes admission, host credit, preparation and publication even if control was attached directly through the engine or never attached at all. Copied terminal observations remain readable. Session reactivation after disposal is refused.
- Save v2 remains byte-compatible for recorded mode. Live v3 adds the bounded admission record and digest. Restore merges admission/host-credit frontiers through the real owner, checks exact reconstructed receipts plus physical history/state/debt, and issues a fresh capability. Private proposals/aborts and unadmitted UI state are not serialized.
- Save/live domain remains ordinary zero-gravity nondevelopment free flight, neutral initial gimbal and neutral recorded command payloads, live physical rate 1x. No contact, development propulsion, pause, pending timeline, or flight-warp save expansion.

## Validation

Debug and Release builds pass. Both configurations pass 1,557 focused live checks, including exact live/recorded history and resource/motion/mass/COM/inertia equality, fragmented host credits, supported save continuation, admitted-but-unpublished save, prepare/abort ordering, malformed journals, duplicate/idempotence, same-frontier arbitration, stale target, disposal, capacity and terminal refusal.

The 42-gate stage matrix passes: live tests/measurement, recorded assembly production, FourHorn integration, contact/powered-contact/departure baselines, exact assembly allocation, M15.3 Florida qualification and M15.4 static/warp/moving/cost camera protections, plus Simulation, ReferenceFrames, Precision, Camera and BEPU dependency suites in both configurations. These are engineering checks, not Project Control player acceptance.

Warm live admission/compilation/physical preparation/publication: **0 B**, no GC over 2,048 operations. Capacity refusal, duplicate receipt and no-work: **0 B**. Journal record 48 B; 256-entry payload 12,288 B; measured cold control attachment 12,392 B; capacity-populated save with all 4,096 host credits 162,428 B (inside existing 1 MiB decode limit).

Normal-runtime Release sample: median 0.0911 ms, P95 0.0943 ms, P99 0.0961 ms, max 0.1335 ms. Sequential recorded population median 0.0551 ms is affected by runtime tier promotion across the measurement populations and is not treated as a pure admission overhead ratio. A separate explicitly non-tiered diagnostic produces live/recorded medians 0.0060/0.0059 ms, P95 0.0069/0.0069 ms and P99 0.0078/0.0082 ms; both remain zero allocation. This isolates tiering as the large aggregate comparison confound. No invented timing threshold or optimization was introduced. Raw populations and runtime conditions are retained under `build/`.

## Red team / correction

The independent verifier accepted the single-owner design, then found one production lifecycle defect: retirement was initially conditional on the application's optional control property. **REVISE — ONE BOUNDED CORRECTION** moved retirement to the physical authority, covering dispose-before-enable and direct engine grants. Tests explicitly protect both paths. Final corrected-source independent review **PASS**; no second production defect or remaining blocker. The eight-file reviewed readset exactly matches `source.json`.

During test authoring, an incorrect clock method name was corrected before compilation succeeded, and the deliberate stale-clock probe was changed to call admission directly rather than a helper that required a Ready observation. Those harness issues did not change production architecture or gates.

## Judgment / next stage

**PASS — PROMOTE** to Stage 2 automatically under Project Control's campaign authorization. All 12 promotion conditions are met: one proven responsibility, current source/KSA evidence, one accepted architecture, bounded implementation, permanent tests, deterministic/replay/resource contracts, bounded allocation/storage/performance, independent red team, no product decision, no intermediate manual requirement, and protected M15.3/M15.4 gates. No player proof is required for Stage 1.
