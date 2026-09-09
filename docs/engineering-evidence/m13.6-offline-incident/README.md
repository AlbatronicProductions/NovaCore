# M13.6 LiveKernelEvent 141 offline incident gauntlet

**Failure classification: INSUFFICIENT EVIDENCE**

**Confidence: LOW for causal attribution**

**Pacing: INSUFFICIENT EVIDENCE**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

**M13.6 CANDIDATE — FOLLOW-UP REQUIRED**

The accessible existing incident evidence is exhausted within the authorized bounded scope. Windows confirms a display-engine timeout, but no available record links its initiating cause to the candidate, an AMD module, a particular command, resource, generation, presentation operation or hardware fault. Ordinary elevated access was attempted once through Windows consent and canceled before the helper started. The dump was not read. No stronger causal classification or production correction is justified.

One instrumented observation protocol is prepared in `retest-protocol.md`. It is a **design**, not implemented instrumentation, an executed test or permission to launch another sustained run. No runtime, GPU probe, build, frame cap, registry/security change or production edit occurred in this ticket. M13.6 remains unbanked and manual acceptance is not cleared.

## WHAT → HOW → BAR → WORK → VERIFY → JUDGE

- **WHAT:** distinguish the recorded timeout from its initiating responsibility without another freeze reproduction.
- **HOW:** refresh a 20-minute incident window, inspect equivalent WER metadata, establish protected-file access limits, audit the exact allocation/consumer/lifetime delta and consult only equivalent verified Vulkan/KSA boundaries.
- **BAR:** a stronger classification requires concrete responsibility evidence; a small source diff, nearby event, shared GPU model or earlier automated pass is insufficient.
- **WORK:** Work A owns Windows metadata; Work B owns exact source contracts; Work C owns narrowly useful reference/tooling facts. Lead alone owns access attempts, final evidence and any production decision. No production writing was needed.
- **VERIFY:** an independent review attacked alternate explanations, access claims, memory semantics and the proposed observation's safety/measurement limits. It required a distinction between emitted, received and durably flushed breadcrumbs; that refinement was incorporated.
- **JUDGE:** unknown causality, no fix, no acceptance clearance; return the bounded evidence and one prepared protocol to Project Control.

## Baseline

| Identity | Verified value |
|---|---|
| HEAD / main / local origin/main | `d4baab6940a57a46e478b98e36f5e45d1c4b558f` |
| M13.5 tag | `m13.5-local-gpu-terrain-working-data` |
| Annotated tag object / target | `75712e4bc990e11c3072a37299c55187cc30e0a1` / commit above |
| Candidate branch | `codex/m13-final-exit` |
| GPU / current driver | RX 6800 XT / `32.0.21045.5002` |
| Primary desktop now | `\\.\DISPLAY7`, ASRock PG34QRT2B, 3440×1440 at 180 Hz, RX 6800 XT |
| Secondary desktop now | `\\.\DISPLAY6`, 2560×1440 at 144 Hz, RX 6800 XT |
| Historical incident output | Unknown; current refresh values are not retroactive incident measurements |

Starting staged diff was empty. The existing three tracked files still contain the known 36-insertion / 6-deletion candidate. `baseline.json` captures exact refs, diff/source hashes, current display enumeration, deployment/assets and all prior evidence fingerprints. Both previous packages remain unchanged: `m13-final-exit` (108 files) and `m13.6-manual-failure` (14 files).

Both Debug and Release binaries and all 49 shaders per configuration match the previous deployment. Protected elevation and content-addressed terrain assets also match. Release native SHA-256 remains `932267253ea6e0221ba4ef5093515874116b26ede8c565ff8b8316e02e21eabf`; these current disk identities do not substitute for the absent failed-process module fingerprint.

## Incident and narrow timeline

Manual route: **Florida Launch Site / Fullscreen Native / Normal Diagnostics**. Approximate runtime: **five minutes**. Loud fans, freeze during/after zoom-out, black NovaCore display and restart are the reported observations. There is no trustworthy incident FPS, temperature, power, clock, frame trace, Vulkan result or displayed-cadence measurement.

Work A refreshed **September 8 00:03:10–00:23:10 EDT**, preserving exact event IDs, levels, payloads and query limits in `work-a.json`. Selected points:

| EDT | Provider / event | Existing evidence | Causal limit |
|---|---|---|---|
| 00:10:13 retrospective | EventLog 6008 | Recorded previous unexpected shutdown time | Not exact freeze onset; later preboot events exist |
| 00:10:40.016214 | Tcpip 4266 | UDP ephemeral-port allocation exhaustion | No process association or demonstrated graphics link |
| **00:13:10.080839** | **Windows Error Reporting 1001, record 15585** | **Fresh LiveKernelEvent 141**, dump `WATCHDOG-20260908-0013.dmp` | No process/module/engine/command attribution |
| 00:14:01.500 | OS boot time | Current boot began | Recovery sequence |
| 00:14:02.235850 | Kernel-Boot 20 | Prior shutdown unsuccessful | Does not identify cause |
| 00:14:05.543589 | Kernel-Power 41 | Unclean restart; BugcheckCode 0 | Does not diagnose power supply, thermal failure or driver implementation defect |
| 00:14:24.590900 / 00:17:21.331874 | WER 1001 | Same fresh report submitted again | Not two additional proven hangs |

Fresh report identity is `5880d346-5ecd-4c93-a8eb-576e32823930`. Parameters: P1 `141`, P2 `ffffe603859e4010`, P3 `fffff8079758b6a0`, P4 `0`, P5 `ffffe6035aae30c0`. P3 remains an unresolved address, not an identified AMD module.

The 40 WER records in the bounded window include repeats of old September 2/6 incidents. Neither their workloads nor their root causes are established. They do not prove the current failure is baseline-preexisting.

Additional postboot metadata was classified rather than treated as a cause:

- Kernel-PnP 219 `ROOT\DISPLAY\0000` maps in the current device inventory to **Meta Virtual Monitor**, not AMD physical display hardware. `ROOT\SYSTEM\0004` maps to **FakerInput**. Current OK status is not historical incident health.
- An AMD `atieclxx.exe` power-policy event is after boot; it does not identify the timed-out module or establish an incident power transition. Work A preserves its exact record.
- No matching successful Display 4101 recovery, WHEA, Windows bugcheck or NovaCore Application Error/Hang attribution was recovered. Disabled/unavailable diagnostic-channel coverage is not counted as a clean pass.
- Reliability records add shutdown/GameInput reconfiguration but no initiating process or engine. The old Downloads runtime log predates the incident and cannot provide a final Vulkan/zoom operation. That does not invalidate the user's runtime observation.

[Microsoft's 0x141 definition](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x141---video-engine-timeout-detected) establishes the **display-engine-timeout manifestation**. The primary classification remains insufficient because another initiating responsibility remains equally plausible and no attributable driver/device condition was recovered beyond that already-known signature.

## Dump / WER access and analysis

Exact originals:

```text
C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp
C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930\Report.wer
```

`access.md` records the supported access result. The current token has medium integrity and Administrators membership marked deny-only. Dump/child security descriptors remain denied. The readable WER ReportQueue parent is SYSTEM-owned; the exact child owner/ACE is unknown. No guessed ACL is presented as fact.

The parsed fixed-path `export-incident.ps1` can read metadata and copy at most 256 MiB into a guarded scratch directory, preserving originals/permissions and verifying hashes. One standard Windows `RunAs` consent request returned **“The operation was canceled by the user.”** This is the OS outcome, not inferred user intent. No helper/export/scratch was created, no source security was changed and no automatic retry occurred.

PATH, installed SDK Debuggers directory, Appx/WindowsApps aliases and retained recipes did not reveal a currently installed WinDbg/cdb/dumpchk path. No debugger was installed or downloaded to work around an unreadable input. Available WER/Event Viewer metadata was inspected instead.

**Offline dump analysis: not performed.** Original bytes, size/hash, header, kernel stack, failing engine, AMD module, TDR recovery context, process association, command/memory fault and device-removed/reset detail are unavailable. A permitted export remains the higher-value optional next evidence step before any runtime protocol; it is prepared concretely in `access.md`.

## M13.5 versus M13.6 causal audit

`work-b.md/json` answers all eight questions and fingerprints the source. The key buffer is one 393,216-byte logical storage buffer: 8,192 slots × 12 uint32 words, descriptor binding 8, not a raw GPU-address API.

| Responsibility | M13.5 | M13.6 |
|---|---|---|
| Memory selection | First compatible HOST_VISIBLE + HOST_COHERENT | Prefer compatible HOST_CACHED as well, retain coherent fallback |
| Prior measured selection | Type 1 / flags 6 / heap 0 | Type 3 / flags 14 / heap 0 |
| Allocation order | allocate → bind → map → initialize | allocate → map → bind → initialize through existing preference helper |
| Map lifetime | One persistent successful map for renderer lifetime | Same, with possible failed startup attempt/fallback cleanup |
| Explicit flush/invalidate requirement | None for required coherent type | Unchanged; cached-but-noncoherent type excluded |
| Successful key lifetime | Resize/zoom retain allocation; final teardown releases | Unchanged logical owner/order, possibly different wall-clock timing |
| Polling / GPU synchronization | Existing scans, producers, frame fence and host-write barrier | Unchanged implementation |

The prior memory observations are not a recovered allocation record from the failed process. Source does not prove identical buffer/memory handles, CPU address, GPU virtual mapping, physical backing or driver allocation size. The nominal buffer size and shared heap do not establish those identities.

The map-before-bind ordering is a real consequence of entering the existing allocator. Joint valid-usage review of [mapping](https://docs.vulkan.org/refpages/latest/refpages/source/vkMapMemory.html) and [binding](https://docs.vulkan.org/refpages/latest/refpages/source/vkBindBufferMemory.html) permits it; this is a legality inference, not a platform-stability result. [HOST_CACHED and HOST_COHERENT](https://docs.vulkan.org/refpages/latest/refpages/source/VkMemoryPropertyFlagBits.html) can coexist; caching does not introduce explicit flush/invalidate requirements for a coherent allocation. Required access ordering and host/device memory dependencies remain. Neither flag combination is AMD DEVICE_COHERENT.

Eight-question disposition:

1. **GPU-visible address:** may differ; the descriptor contract stays the same. No address equality is claimed.
2. **Data visibility:** implementation path can differ; required coherent visibility and barriers do not. No observed violation was recovered.
3. **Stale CPU values:** no new missing dependency is demonstrated under the intended production consumers. CPU scans host-owned words 0–7/10–11; the production selector touches words 8–9, which that poller does not read.
4. **Map lifetime:** successful mapping occurs earlier relative to binding; no per-frame/zoom remapping was added.
5. **Flush/invalidate:** requirements do not change for the admitted coherent types. No synchronization may be removed on that basis.
6. **Destruction/reuse:** successful ordering is unchanged; startup fallback and wall-clock scheduling can differ. Device-idle failure behavior is not newly proved safe.
7. **GPU waits:** no wait-on-key/spin/handshake protocol was added. Driver memory transaction/translation behavior may differ when accessed.
8. **Zoom transitions:** no direct key-based NCSM1 readiness/generation/retirement dependency was found; different backing and CPU cadence remain plausible indirect interactions.

## Zoom and sustained lifetime

Near-surface → wheel/camera → adjacent LOD/pupil selection → one incoming generation → independent regional physical demand/preparation → fence-qualified publication/spare retention → submit/present is the unchanged responsibility chain. The key table remains allocated and host-scanned during it.

Production-v5 readiness and canonical bindings bypass mutable key-table readiness. Once NCSM1 is authoritative, its alternate selector does not dispatch; physical preparation, regional readiness, cull, TES and fragment work do not dynamically consume this key table. Regional buffers/bindings 53–57 own their own geographic data. Descriptor presence is not proof of a GPU load. This is strong source separation, conditional on the intended owner/route; the incident's final frame is not captured.

Static bounds remain one key map/allocation, one frame fence/acquire semaphore, per-image presentation/command resources, one descriptor set, at most 18 topology identities, current plus incoming-or-spare terrain work, one pupil scratch and finite regional catalog/queues. Existing short traces show reuse/publication, not a five-minute live heap/resource trend. No unbounded normal accumulation or actual premature release was proved, and none is ruled out solely by container bounds.

## Narrow KSA / Vulkan / AMD relevance

Work C rechecks the current installed KSA/reference identity and retains only relevant device-result, rebuild and resource-retirement boundaries. The prior authenticated official history provides concrete frame/image-ownership and swapchain corrections; it is referenced without repeating broad presentation research. No equivalent history links excessive FPS, a cached NovaCore key table or this AMD timeout to one demonstrated cause.

**ADOPT, already aligned:** acquired-image presentation resources and explicit completion/retirement ownership. **ADAPT only with concrete proof:** diagnosable device-result boundaries. **INTENTIONALLY DIFFER:** preserve NovaCore's exact physical publication and single-fence resource contract rather than copying KSA's flight count, age-based retirement alternative or coarse fallback.

Specific Vulkan references answer allocation/mapping/coherence questions above. No new AMD-specific condition was identified, so no generic RX 6800 XT crash browsing, driver update, sensor hack or graphics-setting change was undertaken. Pacing remains on hold; current FIFO, an absent explicit limiter and loud fans do not establish incident cadence or wasted work.

## Verifier, correction and prepared protocol

`verify.md` records the adversarial review. Strongest alternatives remain an unobserved candidate memory/backing/timing interaction, baseline lifetime/synchronization/workload behavior, a driver issue or system/hardware interaction. The Windows timeout may be a downstream manifestation. None has enough concrete linkage to exclude the others.

**Correction: none.** There is no bounded proven defect for which CAUSE/RESPONSIBILITY/CORRECTION/BAR/PAYOFF can honestly be completed. No rollback, flush, ordering change or frame cap is presented as a causal fix.

The single [instrumented protocol](retest-protocol.md) requires prepared diagnostic identity, nonblocking native begin/end records, independent durable observer, exact display/swapchain information, PresentMon process/QPC capture, available GPU counters/sensors, transition/publication/resource records and validated stop behavior **before launch**. It distinguishes emitted/received/durably persisted serials and acknowledges that a kernel hang can prevent final evidence or termination.

The proposed observation is one **180-second maximum process lifetime**: healthy startup, near-surface Florida movement, gradual zoom toward orbit and return, stopping at the first anomaly or deadline. It is deliberately not another blind five-minute stress test and cannot clear the longer incident if it passes. Device/validation errors, severe stalls, lost progress, verified thermal/power abnormalities, visible freeze, observer loss or quota exhaustion end the observation. Operational timing thresholds do not change Windows TDR or rendering semantics.

PresentMon is installed and its help was inspected; no trace ran. Mandatory telemetry is not yet implemented, and AMD temperature/power/clock logging is not proven available. That availability boundary is explicit. Future temporary budget is 64 MiB of scalar/journal output, no raw captures; this ticket created none of it.

## Storage, checks and final status

Retained evidence is restricted to this new package, within a 4 MiB budget: baseline/provenance, timeline, access result/helper, exact memory audit, narrow reference/tooling facts, verifier, protocol and closeout. Created equals retained final new-file footprint. No temporary dump copy, GPU raw capture, trace or scratch file exists; disposed bytes and disposable remaining are zero. Exact logical/allocated bytes and file count are in `closeout.json`.

Validation: prior source/diff/refs and all 122 files across both prior evidence packages preserved; deployed binaries/shaders/protected assets preserved; JSON/UTF-8/whitespace checks; export helper parsed; `git diff --check` PASS with existing line-ending warnings. No build/GPU test is claimed or required for these offline documentation/helper changes. Staged diff remains empty.

```text
 M native/NovaCore.Native/MappedBufferMemory.h
 M native/NovaCore.Native/MappedBufferMemoryTests.cpp
 M native/NovaCore.Native/NovaCoreNative.cpp
?? docs/engineering-evidence/m13-final-exit/
?? docs/engineering-evidence/m13.6-manual-failure/
?? docs/engineering-evidence/m13.6-offline-incident/
```

Only the last evidence package was added by this ticket. No stage, commit, push, merge, tag, bank, main promotion or M13.7 work occurred. Stop for Project Control.
