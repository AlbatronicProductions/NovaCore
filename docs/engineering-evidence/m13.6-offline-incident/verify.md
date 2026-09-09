# Independent verification — offline incident disposition and proposed observation

Reviewed the current three-file M13.6 production diff, `work-a.md/json`, `work-b.md`, `access.md`, the fixed-path export helper, and `retest-protocol.md`. This verification performed no runtime launch, GPU work, build, debugger execution, protected-file access, source edit or security change.

**Disposition supported: INSUFFICIENT EVIDENCE; escalate to Project Control; M13.6 candidate follow-up required.** Confidence is high that the current package lacks a discriminating causal link. Confidence is low for assigning any particular initiator. That is confidence about the available evidence boundary, not a probability that the candidate is innocent or that a driver defect is most likely.

## Attempts to disprove the disposition

1. **Positive 141 versus attribution.** Windows did record a fresh display-engine timeout. The three fresh WER entries share report identity, parameters and dump path; later publication does not establish repeated new hangs. Their event-writer PIDs are not faulting-process identities. The driver-pointer parameter has not been resolved, and there is no engine/context, stack, failing GPU command, process image or incident-time Vulkan result. Calling this a proven AMD-driver implementation defect, a NovaCore memory bug or a pacing-induced fault would exceed the evidence.

2. **Nearby Windows events.** The postboot display PnP warning identifies a Meta Virtual Monitor instance in the current snapshot; it is not an AMD hardware-display failure record. FakerInput load warning, AMD policy reset to the same scheme, and RyzenMaster signature failures are postboot events without a demonstrated link to the timeout. Kernel-Power 41 describes the unclean restart. Zero WHEA/Application Error/Display-recovery records and current device OK status cannot exonerate any candidate cause. The disabled DriverFrameworks operational channel correctly remains unavailable coverage.

3. **Actual M13.6 delta.** The current diff still introduces the cached request-key role, selection tests, admission into the existing preferred mapped allocator, and one key-buffer callsite. Logical descriptor/field ownership and required host coherence remain unchanged. The map-before-bind startup order, potentially different mapping/backing and changed process cadence are real differences and are explicitly preserved by Work B. They are not themselves a demonstrated Vulkan violation. Successful shorter controls and memory-selection tests cannot establish correctness at the failed process's final operation. Conversely, no concrete newly missing field-visibility, key-backed wait, or zoom-dependent key-allocation lifetime violation was found that justifies a production correction now.

4. **Protected access.** The medium-integrity token and deny-only Administrators membership explain a demonstrated access boundary without resolving the incident child ACL. A readable SYSTEM-owned parent DACL does not prove the dump/Report.wer child owner or effective ACE. The Windows consent operation returned cancellation; it supplies no evidence about crash cause, operator intent, or future elevated readability. The helper did not run or copy bytes. No stack/hash/header/module result may be inferred. The bounded installed-tool search also does not prove that no debugger exists anywhere on disk.

5. **Absence of a proven fix.** Preserving the unbanked candidate is compatible with this disposition. No correction, rollback-as-causal-proof, performance cap, barrier patch, driver change or renewed acceptance is justified merely to produce an actionable-looking result. Further readable exact incident data could change this conclusion; the current package does not contain it.

## Proposed observation review

The protocol is a design requiring explicit Project Control authorization and implemented preflight. It is not a runnable instruction or clearance in its current state. It correctly prioritizes newly readable dump evidence, keeps the normal Florida route/configuration, preserves the deployed candidate, and limits any later observation to one process with an independent observer.

- The **180-second total lifetime** and at most 60 seconds to first healthy terrain are operational limits. The remaining 45/45/30-second phases fit only within the unused global budget; incomplete phases must stay incomplete. A clean shorter run cannot clear the reported roughly five-minute failure.
- The 500 ms / three 250 ms completed-frame limits, one-second lost progress and subsequent two-second termination attempt are observation stop rules, not Windows TDR values or causal classifications. They may intentionally stop benign slow progress and must report that fact. Termination and log durability remain conditional on OS scheduling; no observer can guarantee recovery from a kernel/display hang.
- Present API return, submission completion and displayed-frame cadence are distinct. Dropped frames, QPC joins, schema availability and current versus incident display identities are correctly kept separate. Missing sensors and lack of the user's FPS reading cannot be replaced with inferred thermal or pacing measurements.
- The **64 MiB is prospective temporary output**, not output already generated: 8 MiB breadcrumb slots + 32 MiB PresentMon + 24 MiB scalar/sensor/system data. Preflight still must prove bounded writers and quota-triggered stopping. The 4 MiB permanent target cannot authorize loss of unresolved irreplaceable fault evidence. Original Windows dumps remain in place.
- The protocol must not imply that an isolated telemetry build is already prepared or has qualified the current failure. Mock-child observer/deadline tests are prerequisites and do not exercise GPU stability.

### Breadcrumb clarification incorporated into the protocol

Track **last emitted, observer-received and durably flushed serials separately**, including loss and durability lag. A last durable begin marker is valuable, but a kernel hang can interrupt delivery or flushing after a later operation began. It therefore bounds the last confirmed activity; it does not automatically identify the exact final GPU operation or imply that no later call ran.

The render thread must not wait for observer acknowledgments or durable storage. Bounded logging must not introduce GPU waits, submit pacing or blocking backpressure that obscures the timing under investigation. Preflight must quantify logging perturbation and verify partial-record recovery, ring overflow, observer lag/loss and mandatory-stream failure handling with the mock child. If durability coverage cannot be maintained, end the observation and report its last confirmed serial rather than asserting complete final-operation provenance.

The parent incorporated this clarification, and the updated protocol was reread: it now explicitly uses a nonblocking producer, separates the three serial boundaries, tests lag/loss/partial records and overhead, and ends observation on ring overrun or more than one second of durable-journal lag. Those are future implementation/preflight gates, not a request to implement telemetry or run NovaCore in this offline ticket. The one-second limit constrains useful evidence loss and is not a promise of persistence during a kernel hang.

## Final independent judgment

No concrete evidence defeats **INSUFFICIENT EVIDENCE** or justifies a production fix. The candidate remains unbanked and the manual failure remains unresolved. The only proposed runtime follow-up is conditional, bounded, and unable to guarantee kernel recovery or acceptance. With the breadcrumb clarification incorporated, the protocol is a reasonable Project Control proposal with no remaining material report correction identified; it is **not ready to execute until its implementation and preflight requirements are fulfilled**.

This verification adds only this compact report. It does not alter either sealed prior evidence package or production behavior.
