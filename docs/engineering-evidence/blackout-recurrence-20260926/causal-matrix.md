# Causal matrix and resource audit

These classifications concern this fresh recurrence. A missing event is not exoneration. All line references below are in the sealed incident session's `segment-0.log`, unless a source path is named.

| Proposed causal owner | Classification | Positive evidence and limit |
|---|---|---|
| CPU/render-thread stall | UNRESOLVED | Render telemetry ends; background CPU flushes continue. No thread stack or pending-call journal. Whole-process cessation at the last render sample is weakened by subsequent logger execution. |
| Synchronization/fence deadlock | UNRESOLVED | A fence/query completed for 33978; no later begin/end wait identity. Cannot assign a stalled fence. |
| Vulkan device-loss path | UNRESOLVED | No retained returned device-loss/error; no device-fault dump. A nonreturning call/reset could bypass logging. |
| Vulkan present/swapchain/display failure | UNRESOLVED | Both displays reportedly blacked out; physical display symptom observed, Vulkan ownership not established. No last display acknowledgment or composition trace. |
| GPU completion stall | UNRESOLVED | 33978 completed. Subsequent submission/completion unknown. Two black monitors do not prove GPU execution stopped. |
| GPU workload/timeout/TDR | UNRESOLVED | Healthy completed timing samples weaken sustained overload in sampled intervals; no terminal-frame duration or incident TDR record. A later pathological frame remains possible. |
| AMD driver/watchdog fault | UNRESOLVED | No accessible incident signature. LiveKernelReports inaccessible; diagnostic trace unavailable. |
| GPU memory/residency/lifetime | UNRESOLVED | Completed generation138 and stable earlier component totals. Missing handle history, final heap budget, and last dependencies prevent proof either way. |
| PCIe/WHEA/device hardware fault | UNRESOLVED | No fresh WHEA signature; repeated boot WHEA data and virtual-device warnings do not establish a Radeon/PCIe fault. Unrecorded fault remains possible. |
| PSU/power/system instability | UNRESOLVED | Abnormal restart observed. No power telemetry or causal signature. Zero Event41 fields do not identify a PSU. |
| Windows/system failure | UNRESOLVED | Abnormal restart observed; no incident dump/bugcheck identifying Windows as owner. |
| Other evidenced owner | UNRESOLVED for blackout | Clock refusal and diagnostic loss are OBSERVED separate problems; no causal chain from either to both displays failing. |

**OBSERVED:** user-reported simultaneous display loss and required restart; Windows abnormal restart; successful GPU completion; one clock-refused launch; diagnostic character loss.

**SUPPORTED:** current candidate association (incident native/managed/shader hashes and App MVID match); sampled CPU/GPU progress before the evidence ends; evidence category **F**, inability to distinguish A–E after the retained sequence. None is a proven blackout mechanism.

**WEAKENED:** monotonic NovaCore process-private/working-set growth across retained samples (private bytes fall from peak 4,783,886,336 to 4,031,561,728; working set peak 4,127,268,864 to 3,417,800,704); factor 64, near-ground camera, 4,008 kg craft, or resize alone as a sufficient trigger (successful ordinary-route counterexamples). System commit, other processes and GPU heap pressure remain unmeasured. These facts do not exclude transient GPU pressure or a particular combined sequence.

**REJECTED inferences:** Event 41 alone proves PSU failure; PnP 219 names the Radeon; logger heartbeat proves rendering; absence of a device-loss line proves a healthy driver; successful sampled timings prove no later GPU stall; frame 33979 is known submitted/stalled; an old route PASS clears this recurrence. These reject arguments, not whole causal domains.

## Resource authority / identity / lifetime

| Responsibility | Retained/static evidence | Ceiling |
|---|---|---|
| Authority | Physical generation 4, terrain-v5 owner; input content fingerprints match accepted package; simulation/presentation authorities remain distinct. | Final exact camera/physical input bundle not captured. |
| Identity | Last completed draw generation 138, LOD 17, family 1; 1,424,208 input triangles, counters/query match. | Raw per-frame buffer/memory identities unavailable. |
| Ownership | Static current/incoming/spare topology/work ownership; latest complete residency generation 128 / LOD 7 at line 5488. | Missing publication detail 129–138 cannot be reconstructed solely from logs. |
| Lifetime / fences | `NovaCoreNative.cpp:2611` waits existing frame fence before publication inspections 2635–2638. Last pupil 1108 publication at line 6704 reports completion; later frame identities progress 32845→33978. | Later retirement/destruction or dependency failures unobserved. |
| Mapping | `RegionalPhysicalPreparation.inl:4–19` swaps buffer, memory, mapped pointer and capacity together after generation/completeness validation. | No runtime mapping/handle birth/free journal. |
| Readiness / generation | Pupil 1108, generation 138, 712,106 vertices; readiness 76.757 ms; fenceComplete and atomicFrameBoundary. | Completion of this publication does not prove subsequent publication/reuse safe. |
| Invalidation / publication | Current source validates job generation/cursor before publication and updates descriptors after ownership swap; old work retained as spare at `NovaCoreNative.cpp:2596`. | No final exact invalidation/destruction sequence. |
| Residency | Last component report: 18 topologies / 241,114,176 bytes; 18 work allocations / 110 reuses; working 136,222,328 bytes, current/incoming 1+0, spare 1. | Logical component counts, not total VRAM, heap usage, budget or pressure. |

No fresh witness proves use-after-retirement, premature destruction, identity reuse before completion, stale draw, generation mismatch or an unsatisfied dependency. The missing trace also prevents certifying their absence.

## Recording limits

`SessionLog.cs` has a 32,768-character in-memory buffer and approximately 1 Hz background flush. Twelve retained overflow markers total **448,536 dropped characters**; last overflow line 5599, batch 23:19:42.6791521Z. This is proven diagnostic loss, not evidence that rendering failed. No overflow marker occurs in the final approximately 30 seconds, but sampling still omits per-call information and a reset may lose pending data.

The native causal recorder requires `NOVACORE_CAUSAL_MAPPING` (`CausalRecorder.h:55–56`) and emits nothing without its mapped header. The ordinary App does not automatically launch that observer. This capture contains only text session files for PID37752; there is no durable causal journal/frozen bundle/first-alarm payload for it. The incident process environment itself was not captured, so absence of a retained journal is the precise evidence claim.

Source falsifier: `NovaCoreNative.cpp:2854` deliberately suppresses rendering for hidden/minimized/zero-area viewports while background logging can continue. This defeats inference from empty batches alone; it does not explain simultaneous monitor blackout.
