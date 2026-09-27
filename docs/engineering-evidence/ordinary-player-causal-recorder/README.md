# Ordinary-player causal recorder — durability decision

**ESCALATE — one durability-contract decision is required before production integration.** Blackout cause UNRESOLVED; Player PASS, banking and NovaCore relaunch HOLD. No recorder-qualified candidate is claimed or frozen.

## What happened

The ordinary-route coverage review confirmed that the old recorder cannot simply be enabled: its activation changes Vulkan API/features, buffer usage, diagnostic queries and capture behavior. Two independent read-only reviewers examined durability and native integration. Before changing production, a standalone CPU-only persistence proof tested whether a nonblocking recorder can always preserve the actual terminal CPU/GPU/present boundary through arbitrary whole-machine reset.

The proof finds a real limit: different terminal histories can have **identical durable bytes**. Debug and Release each pass **139 proof checks**, with zero final build warnings/errors. This is a validated counterexample and parser model, **not production recorder qualification**. NovaCore was neither built nor launched. No production/test source or canonical package changed.

The current authorization includes both “as far as technically achievable” and a stronger red-team requirement that another blackout must not leave the first unreturned boundary unidentified. These have different meanings when an asynchronous terminal suffix can disappear. The requested clarification asks whether explicit terminal uncertainty is acceptable, or exact retention remains mandatory. Neither choice is assumed from silence.

## Old coverage gap

- Ordinary App startup creates `SessionLog`, which has a bounded character buffer and approximately one-second background flush. The fresh incident lost 448,536 characters and retained no terminal call journal.
- Native causal recording requires `NOVACORE_CAUSAL_MAPPING`. Ordinary bootstrap does not automatically establish it.
- Existing `causal.Active()` affects Vulkan API version (`NovaCoreNative.cpp:1011`), transfer-source buffer usage (`:1768`), optional extensions/features (`CausalNative.inl:37–49`), and heavy diagnostic work. Reusing it would alter the route under diagnosis.
- Existing observer code publishes its shared “durable” sequence before `journal.Flush()` returns (`tools/NovaCore.Causal.Observer/Program.cs:219–220`). A new recorder must acknowledge persistence after successful flush, not copy this ordering. This is a design hazard identified here, not a newly proven cause of the blackout or authority to patch historical machinery.

## Durability / abnormal-termination result

All three histories begin with persisted session identity and positive completion of submission 40. The background persistence owner has not yet flushed the subsequent events:

| History | Actual later execution | Bytes surviving the modeled reset |
|---|---|---|
| A | Submit 41 entered, no return | Session + completed 40 |
| B | Submit 41 returned; present 41 entered, no return | Session + completed 40 |
| C | Submit/present/frame 41 returned; next Update proves completion 41 | Session + completed 40 |

The retained-prefix SHA-256 is identical in all three cases:
`1bd4ce96bfa695d62a49fd6ad97373ebab995d0b0f50e8b27a748b71224ce649`.

No parser can distinguish histories that supply identical retained input. A shorter flush interval, separate observer, file mapping or asynchronous write-through improves survival but does not eliminate the pre-persistence interval. A configured cadence is not a hard maximum lag under scheduler/storage stalls.

This proof **does not** invalidate a report such as “submission N returned, completion N−1 proven, completion N absent from retained evidence” when that prefix has persisted. It invalidates an unconditional claim that the actual terminal API is always recoverable. “Return not observed” must not become “the API definitely never returned.”

The model also verifies a conditional positive case: if an observer survives and successfully drains the volatile suffix, present entry can be recovered. Process-termination survival is therefore different from machine-reset survival. No real process-kill, machine reset or storage failure was performed in this feasibility proof.

Mapped memory alone does not close the durability gap. Windows documents that `FlushViewOfFile` does not wait for underlying disk-cache persistence; mapped-file persistence requires the subsequent file-buffer flush. [Microsoft API contract](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-flushviewoffile). Even successful synchronous entry-marker persistence leaves a tiny marker-to-call instruction gap, so reports must retain the distinction between a call-entry marker and proof of execution inside the API.

## Proposed minimum contract — pending the decision

1. Mandatory ordinary bootstrap before GPU initialization; no manual environment flag. Initialization failure visibly refuses an unrecorded session.
2. Independent minimum sink, with legacy heavy-capture activation remaining unchanged. No added Vulkan feature, extension, query, buffer-usage flag, GPU wait or queue submission merely for minimum recording.
3. Fixed-size events with session identity, event sequence, CPU-loop ordinal, operation identity, command-recording incarnation, submission identity, fence association, result and resource/generation context.
4. Preallocated producer storage; no routine filesystem work, managed allocations, or waiting for the persistence owner on the renderer. Capacity exhaustion and recorder failure must be explicit, not silently discarded.
5. Separate persistence ownership, preferably able to survive renderer-process termination; checksummed records and recoverable checkpoints, durable acknowledgement only after successful persistence.
6. Recovery distinguishes the last durable prefix, operations whose returns are absent **within that prefix**, and the possibly lost terminal suffix. Physical display completion remains unproven by present return.
7. Resource checkpoints remain self-contained after history rotation. Handle reuse uses birth/incarnation identities; a live resource cannot lose its sole provenance to rollover.
8. An unclean session never claims “no later operations occurred.” I/O failure, corruption, overflow and observer loss produce explicit incomplete-coverage status. Loss must not be relabelled successful recording.
9. Measure the complete implemented path before choosing a final cadence/capacity or claiming bounded overhead. A low nominal flush period is not proof of a universal durability deadline.

This is the cheapest defensible direction if explicitly reported terminal uncertainty is accepted. It has not been implemented or qualified. Exact terminal retention without uncertainty would need a different durability mechanism and a new perturbation assessment; synchronous per-operation gating conflicts with the present nonblocking/frame-pacing bar, and is not silently authorized.

## CPU / GPU / present and lifetime correlation

The [integration map](integration-map.md) identifies the existing owners and traps. In particular: CPU-loop identity must not reuse the later-incremented terrain frame; an initially signalled fence proves no submission; successful submissions bind to fence/command incarnations; acquire-out-of-date can skip work; exception unwinding is not successful return; recycled Vulkan handles need independent birth identities. Device/queue idle during recreate and cleanup must also be covered as possible unreturned operations.

## Overflow / failure and performance

The proof checks every partial final-record cut and every single-byte corruption position for its 64-byte model record. It does not test a production ring, actual I/O failure, storage exhaustion, complete resource ledger or live process termination. Those remain required after contract selection.

No production recorder overhead numbers are available because no production recorder was selected/installed. Median/P95/P99, recurring worst-case CPU cost, allocations, retained memory and I/O volume are **not measured**; no zero-cost or performance PASS is claimed. The model's two-record durable prefix is 128 bytes and is not a proposed production capacity.

## Offline validation and red team

- Standalone Debug and Release builds: PASS, zero warnings/errors after repairing one initial proof-source syntax error.
- Deterministic feasibility checks: **139 per configuration**. Three incompatible terminal histories produce identical durable input; 64 truncation cuts and 64 corruption cases refuse a fabricated final event.
- Native reviewer independently confirmed legacy activation perturbations and frame/submission/resource correlation hazards.
- Durability reviewer independently confirmed the counterexample, and caught an early model sequence that placed completion before present. Corrected v2 follows current NovaCore's submit→present→next-Update completion order and reproduces the same result.
- These checks are not the requested full offline recorder gauntlet. Always-on bootstrap, real abrupt process termination, storage faults, sustained overflow, resource correlation and performance cannot receive PASS from this model.

## Decision / next cheapest proof

**Recommended:** accept the durable-prefix contract with explicit uncertainty about any unpersisted terminal suffix. Then implement the independent minimum recorder, execute the full Debug/Release CPU/mock/fault/performance gauntlet, and stop before GPU exposure. Do not advertise a fixed worst-case uncertainty duration without proving scheduler/storage progress assumptions.

If exact actual-terminal-boundary retention remains mandatory for every machine reset, retain **ESCALATE** and decide the durability/perturbation architecture first. No local buffer-size or flush-frequency patch can meet that unconditional guarantee.

Both protected evidence sets verified; elevated LiveKernelReports evidence contains zero incident-date files. Its manifest is `d76fc6728dbc410d8e5f829e80cf2f3072ff2fdabd00dfce3184a19cb0062bc6`. This closes the prior access gap without establishing GPU/driver innocence. Primary recurrence manifest remains `ab37a3a6bff1173f224871f4978caec19402648eea897e1f62376d74407ef04e`.

**STOP FOR PROJECT CONTROL at the durability decision.** No ordinary application run, GPU proof, bank or milestone promotion is authorized by this evidence.
