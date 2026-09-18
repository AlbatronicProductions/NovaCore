# Stage 3 — canonical pilot demand

Historical checkpoint: Stage 5 recovery and Stage 6 qualification subsequently completed. Project Control has now reported final manual PASS; current authority is root campaign-verification.md and manual-acceptance.json. Original checkpoint wording below preserves its timing.

Judgment: **PASS — PROMOTE**, no production correction required. One responsibility: durable signed pilot intent, without physical allocation or keyboard attitude bindings.

## Responsibility and current KSA

Fresh installed KSA 5438 identity remains SHA256 A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8; relevant IL witnesses unchanged. The authenticated history reviewed in Stage 1 carries forward without new archaeology. Current manual control supplies independent signed intent, cancellation of opposing flags, and actuator allocation downstream. Adopt those boundaries; adapt NovaCore's exact body axes and bounded canonical snapshots. KSA assisted angular-rate overrides do not enter this raw-demand stage.

Body axes are right handed: roll X, pitch Y, yaw Z, established by admitted FourHorn pair geometry and inertia response. Each demand is a signed byte restricted to -1/0/+1. Future W/S, A/D, Q/E intent maps to ±Y, ±Z, ±X respectively; this is a demand-sign convention, not a claim about screen-relative up/down. Actual bindings remain Stage 5.

## Implementation

The existing request/journal now includes three independent axes and a PilotOnly bit. Pilot-only payloads canonically carry MainOn=false, preserve the current engine latch, and do not create an OFF arbitration veto. Engine updates carry the retained pilot snapshot. Effective journal records contain the full resolved engine/pilot state. Same-frontier engine OFF dominance remains unchanged; latest admitted pilot snapshot wins in sequence order.

Held intent persists until another admitted snapshot. Releasing an intent recomputes the axis from surviving opposing intents: both held cancel; releasing one restores the remaining sign. Simultaneous demands are not normalized or dropped. Zero pilot demand does not reset angular velocity. No pose, momentum, force, torque, gimbal or store direct writes were added.

READY can contain pilot-only admissions without consuming host time. The player adapter reconstructs Started from the first effective ignition, allowing preceding READY pilot records. Z/X preserve demand. Malformed axes and PilotOnly+MainOn=true refuse atomically.

Engine-only zero-demand v3 journal bytes and digests remain unchanged, proven against the old complete record shape. Demand-bearing or pilot-only journals use explicit **demand-only v4**. This version deliberately has neutral gimbal/RCS physical execution; future Stage 4 must not silently reinterpret it under an active allocator. Recorded v2 remains unchanged. Unadmitted input is absent from saves.

## Validation, cost and storage

Debug/Release builds PASS, **52/52** regression gates PASS. Both configurations pass **2,939** pilot checks and **9,898** adapter checks. Coverage includes all 64 opposing-input combinations/all 27 ternary demand states, invalid domains, duplicate/changed retry, engine/pilot ordering, explicit OFF arbitration, READY and midflight/terminal save replay, exact journal-byte compatibility, tamper rejection, host fragmentation, common physical history and absent unqualified actuation. Existing camera, Florida, recorded Plan, resource, launcher, ReferenceFrames, Precision and BEPU gates pass.

Normal Release admission median/P95/P99/max: **0.0018/0.0019/0.0023/0.0503 ms**, 2,048 samples, no collections; final raw counter 0 B. The exact acceptance gate independently measures equivalent fresh 8×256 admissions with the repository's checked no-GC helper: **0 B**, entry/exit PASS, positive-control 152 B. No timing threshold was invented.

Initial ordinary counter diagnostics reported a single 272–336 B event per population; an equivalent engine-only population also showed it, a no-op population did not, and non-tiered execution did not eliminate it. The underlying runtime cause is **unproven**. No bytes were subtracted or tolerated. The approved measurement split follows ENGINEERING_RULES.md; diagnostic source and raw failures remain reproducible under build/player-flight-controls-gauntlet. One exploratory helper result included reporting allocations after the inner measured counter; the final helper closes before all formatting and is the sole authoritative result. See measurement.json.

Admission record now 56 B, fixed 256-entry journal payload **14,336 B**; full demand journal save **76,578 B**, within the existing 1 MiB decode cap. Full live-path allocation remains zero. No growing queue or parallel journal.

## Independent review and promotion

Read-only reviewer accepted PilotOnly arbitration, unchanged v3 bytes, v4 demand-only policy, READY restoration, and final exact allocation measurement. No production defect or correction. Final 52 clean regression gates discharge its conditional source PASS. All twelve promotion conditions are met; proceed to Stage 4's allocation/geometry responsibility. Manual Project Control acceptance remains pending after integrated qualification. No milestone or banking action.
