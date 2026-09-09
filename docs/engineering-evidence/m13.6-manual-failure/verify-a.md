# VERIFY A — stability red team

**Disposition supported: ESCALATE TO PROJECT CONTROL; M13.6 CANDIDATE — FOLLOW-UP REQUIRED.** Preserve the candidate, make no speculative production correction, and keep banking/manual acceptance on hold. This is approval of the investigation's disposition, not a stability PASS or permission to repeat a potentially destructive run.

The clarified manual observation is Florida Launch Site, Fullscreen Native, Normal Diagnostics, approximately five minutes; fans became loud, followed by PC freeze/black screen around zoom-out. **No FPS reading was observed.** The ticket's earlier 200–270+ FPS estimate must not be treated as measured incident data. Likewise, the active physical display/refresh relationship is still to be established; a source-requested FIFO mode or adapter-level refresh listing does not supply the failed process's actual displayed cadence.

## Attack on candidate exoneration

The exact current production diff still adds only `TerrainRequestKeys`, its selection tests, the helper dispatch condition and one allocation opt-in. The key table is the same 393,216-byte, renderer-lifetime, persistently mapped buffer. `MappedBufferMemory.h:11–36` requires compatible host-visible/coherent memory and prefers caching for this role, retaining the original fallback and fatal-error policy. `NovaCoreNative.cpp:1588–1602` creates it once and destroys it with terrain residency. It is not recreated by ordinary camera zoom. The six M13.5 GPU working-data roles remain unchanged.

This narrows the candidate's direct responsibility but **cannot exonerate it**. A different legal memory type exercises a different driver/cache path, and reduced CPU polling latency changes the timing and load of otherwise unchanged submissions and transitions. The small allocation size does not rule out a timing-mediated interaction. Automated parity, policy tests and shorter route traversals do not substitute for the failed interactive run's resource/command evidence.

The polling-elision alternative is irrelevant to this actual candidate: all polling, key updates, I/O worker activity and lifetime are retained. Do not argue that an empty queue proves correctness; that is not the implementation's guard. Nor does host coherence alone prove GPU-to-host visibility. The existing synchronization remains required.

The field-level producer boundary was checked against `work-b.md` and source. The production selector writes key words 8–9 for use/transition state; the production request poller consumes words 0–7 and 10–11, seeded/published on the host (`planetary_select.comp:129`; `NovaCoreNative.cpp:1124–1136`). Active authoritative NCSM1 bypasses that alternate selector. No newly introduced missing visibility dependency on the failing production route was demonstrated. The generic shader can rewrite other words, so this conclusion must not be generalized to every imaginable future mixed context or used to remove barriers.

## Zoom, ownership and lifetime challenge

Inspected source supports the following ordering; it is not a trace of what completed in the failed process:

* `Update` waits the prior frame fence before regional/publication inspection, managed callback, candidate creation and uploads (`NovaCoreNative.cpp:2340–2407`). Thus source-level buffer mutation and descriptor rebinding are ordered after that fence.
* One incoming topology transaction is admitted at a time (`:1646–1657`). Readiness checks include native/submitted generation and topology identity, physical/normal validity, compaction/indirect accounting and completion (`:2301–2338`). Publication transfers incoming ownership, keeps outgoing work as one spare, clears incoming aliases and rewrites current descriptors after the fence.
* The spare is moved into incoming when capacity suffices, otherwise destroyed before replacement allocation (`:1610–1623`). Immutable topology residency is capped at 18 identities; these are retained first-use resources, not an unbounded list of every generation (`:262,1630–1636`). A same-level pupil uses one separate scratch physical buffer with high-water capacity and a completed-fence swap (`RegionalPhysicalPreparation.inl:4–61`).
* Regional preparation has at most 65,536 vertices per slice. It retains published physical data until completion; only the final incoming slice enables final cull/compact and publication (`RegionalPhysicalPreparation.inl:64–86`). The regional payload allocation is bounded and not retired on every zoom-out (`RegionalPhysicalResidency.inl:67–96`).
* Graphics submit arms the incoming fence-pending state only after successful submission (`NovaCoreNative.cpp:2247`). Swapchain recreation waits for device idle before destroying old frame resources and preserves terrain resources (`:2035–2074`); teardown also waits before releasing resources (`:2263–2282`). Ordinary zoom does not directly request swapchain recreation.

These static bounds and ownership transfers reveal no concrete new M13.6 leak, premature free, descriptor mutation or generation swap. They do **not** prove stable live allocation/heap counts over the approximately five-minute failed session. No incident resource trace exists. First visits during zoom can change topology, work/scratch capacity and preparation workload; the specific failing generation, allocation or GPU engine remains unknown.

The strongest synchronization attack remains observability: fence/acquire use unbounded waits and some reset/device-idle returns are not separately checked. A driver/device hang can leave the log without an operation completion marker. The existing minimized-window recreation loop can also spin while client dimensions are zero. These are pre-existing source sites, not measured explanations of this wheel-zoom GPU timeout. Changing waits, forcing a cap or redesigning recreation would currently patch a hypothesis.

## Attack on the failure classification

`work-a.md/json` supplies positive Windows LiveKernelEvent 141 evidence before reboot. That identifies a display-engine timeout class; it is stronger than Kernel-Power 41 alone. It still does not resolve the driver-address parameter, timed-out process, Vulkan command, hardware condition or candidate contribution. The absence of Application Error, Display 4101 or readable current Vulkan error output is not proof that those responsibilities were healthy.

Earlier watchdog incidents predate M13.6, but neither their workloads nor causal stacks are established here. They cannot prove BASELINE-PREEXISTING for this event or satisfy the ticket's requirement to establish that M13.6 is not causal. Kernel-Power is a restart consequence. Loud fans are not measured temperature, power, utilization, FPS or useful display cadence.

The prior sealed `m13-final-exit/verify-a.md` PASS remains historical automated candidate qualification. The later manual FAIL changes the current acceptance state; it does not require rewriting the sealed evidence and must not be overridden by it.

## Missing proof and bounded next action

No concrete causal correction was missed by this review. The highest-value next step is readable access or a user export of the existing fresh watchdog dump and its WER report, with the approximate manual route/timing clarification preserved. Resolving that evidence can identify the driver/engine/process context without launching another workload. Do not bypass the access-denied ACL.

If this evidence remains insufficient, Project Control must decide the next bounded observation. Any later runtime comparison needs a stop mechanism and durable bounded breadcrumbs for operation serial/results, camera/LOD/generation, heap/live-resource state and actual presentation behavior before it starts. A short successful run or a paced run that merely avoids the symptom would not clear the cause. No repeat hard-hang stress is justified now.

Read-only review complete: no production/source/registry changes, builds or GPU runs. Only this report was written by this verifier; the sealed earlier package is unchanged. The unresolved physical failure remains the blocker, and any separately proven presentation improvement must retain its independent responsibility and acceptance boundary.
