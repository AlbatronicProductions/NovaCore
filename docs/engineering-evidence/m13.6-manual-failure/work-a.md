# Work A — Windows and runtime failure evidence

## Finding and acceptance boundary

**Observed failure class: GPU-DEVICE-OR-DRIVER-FAILURE. Initiating cause: INSUFFICIENT EVIDENCE.** Windows recorded a fresh `LiveKernelEvent` code `141` before the restart, referencing `WATCHDOG-20260908-0013.dmp`. This establishes a display/GPU engine timeout in the incident window. It does not establish a driver implementation defect, a NovaCore command/resource defect, hardware instability, or excessive frame rate as its cause. M13.6 is neither causally blamed nor exonerated by the currently readable evidence. Manual acceptance remains failed and banking must remain on hold.

Microsoft defines [0x141 as VIDEO_ENGINE_TIMEOUT_DETECTED](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x141---video-engine-timeout-detected), meaning a display engine did not respond in time. It identifies a live dump, not a Windows blue-screen bugcheck. The WER event's driver-address parameter is not a resolved module identity; resolving it requires the dump or equivalent diagnostic metadata.

No runtime was launched, no stress/reproduction was attempted, no registry/ACL was changed, no dump was copied, and no production source or sealed `m13-final-exit` file was modified. This worker wrote only this report and `work-a.json` in the new evidence directory.

## Recorded timeline

All local times below are September 8, 2026, EDT (UTC−04:00). `work-a.json` preserves UTC timestamps and the fresh WER/Kernel-Power XML. The bounded final query covers September 7 22:00 through September 8 00:32:34.816 EDT.

| Local time | UTC | Evidence | Interpretation |
|---|---|---|---|
| 00:10:13 | 04:10:13 | Retrospective previous-shutdown time in EventLog 6008, record 12759 | Not a precise proven freeze onset: later preboot events were still written. |
| 00:10:40.016214 | 04:10:40.016214 | System/Tcpip 4266, record 12744: UDP ephemeral port allocation exhausted | Preboot resource observation, with no process attribution. It does not establish a graphics cause. |
| **00:13:10.080839** | **04:13:10.080839** | **Application/Windows Error Reporting 1001, record 15585: fresh LiveKernelEvent 141** | First recorded report for the new watchdog dump; it precedes reboot. |
| 00:14:01.500 | 04:14:01.500 | Win32_OperatingSystem LastBootUpTime | Current boot start. |
| 00:14:02.235850 | 04:14:02.235850 | Kernel-Boot 20, record 12750 | Last shutdown unsuccessful; last boot successful. |
| 00:14:05.543589 | 04:14:05.543589 | Kernel-Power 41, record 12779 | Unclean restart consequence, not a causal power-supply diagnosis. |
| 00:14:20.787333 | 04:14:20.787333 | EventLog 6008, record 12759 | Reports the earlier unexpected shutdown. |
| 00:14:24.590900 | 04:14:24.590900 | WER 1001, record 15623 | Same new watchdog report resubmitted after boot. |
| 00:17:21.331874 | 04:17:21.331874 | WER 1001, record 15689 | Same report submitted again; not evidence of another timeout. |

Fresh report identity: `5880d346-5ecd-4c93-a8eb-576e32823930`. Its WER parameters are `P1=141`, `P2=ffffe603859e4010`, `P3=fffff8079758b6a0`, `P4=0`, `P5=ffffe6035aae30c0`. WER supplies no resolved module, process, Vulkan call, command buffer, frame, camera pose, generation, resource address or driver fault bucket. The three WER records share report identity and dump path.

Kernel-Power 41 records `BugcheckCode=0`, all bugcheck parameters zero, `PowerButtonTimestamp=0`, `LongPowerButtonPressDetected=false`, and `WHEABootErrorCount=0`. These fields do not rule out a hard hang, forced restart, hardware fault or unwritten crash information. [Microsoft's Event 41 guidance](https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/event-id-41-restart) explains that it is written during the subsequent startup and may be insufficient to identify the cause.

## Old reports versus the fresh event

The query found 40 WER records describing 12 distinct dump filenames. Eleven filenames belong to September 2 or September 6; repeated report IDs and filenames show that old reports were being resubmitted, including before this incident. The new September 8 filename appears in exactly the three records above. Do not count every postboot `141`/`a1000001` WER event as a new GPU failure.

The earlier September 2/6 watchdog evidence shows this machine has previously recorded GPU watchdog incidents, including before the current M13.6 work. It does not identify the old workloads or prove this incident has the same root cause. It therefore cannot establish BASELINE-PREEXISTING as the cause of the current event or clear M13.6.

## Dump and WER access

The following exact references are retained for further read-only analysis. No ACL bypass or privilege change was attempted.

| Path | Result |
|---|---|
| `C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp` | WER-referenced; direct file metadata access denied. Size, hash, dump content and driver stack are unverified. |
| `C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930\Report.wer` | Access denied; parent report directory also access denied. |
| `C:\Windows\LiveKernelReports` | Directory enumeration access denied. |
| `C:\Windows\SystemTemp\WER-96193296-0.sysdata.xml` | Not found at inspection. |
| `C:\Windows\MEMORY.DMP` | Not found. |
| `C:\Windows\Minidump` | Enumeration succeeded with no files returned. |

The user crash-dump directory contains older NovaCore test dumps and `NovaCore.Triangle.exe.37048.dmp` (7,426,405 bytes, modified September 7 00:16:47.731983 UTC, i.e. September 6 evening local). None is timestamped to this September 8 event. Those old dumps were not copied or analyzed as incident evidence.

The smallest valuable next evidence is permitted read access or a user-provided copy/export of the fresh watchdog dump and its `Report.wer`, followed by bounded dump analysis. The driver-address parameter may then resolve to a module and the TDR context may expose the timed-out engine/process. Even a driver module on that stack is not automatically proof of whether application commands or the driver caused the timeout. Another deliberate hard-hang reproduction is not justified by these findings.

## Available NovaCore log

The only matching Downloads runtime log found is:

`C:\Users\Tyler\Downloads\ENovaCoresamplesNovaCore.TrianglebinReleasenet10.0NovaCore.Triangle.exe.txt`

It is 685,589 bytes / 5,357 lines, created and modified **September 6 13:42:54.131362 UTC**, SHA-256 `36957963928af39f077e3052c1e37ed422a1d9a15fef046169a1c36a310483b1`. It starts mid-run with old physical-slice/pupil entries and lacks a current incident start/end timestamp or runtime fingerprint. Searches found no device-lost, VUID, validation-error, fatal/crash or exception marker, but that negative result concerns this old log only.

This file cannot describe the current manual failure. Its age does **not** imply that the user's failing runtime was stale. The manual observation remains authoritative. Exact run duration, failing zoom state, current/incoming generation, last successful submit/present/fence, device-lost return code and runtime/module identity at the instant of failure are unavailable from this file. No matching fresh `.log`, `.txt` or `.dmp` was found directly in the normal Release launch directory.

## Negative evidence and other observations

The bounded System/Application query returned no Display 4101 successful recovery record, WHEA record, AMD/display provider error, bugcheck report, or Application Error/Hang 1000/1002 record around the incident. Thus a successful system-wide GPU reset, a Windows bugcheck and a NovaCore process crash are not independently established. Absence of those records does not erase the positive 141 timeout or exclude errors that could not be flushed before restart.

Enabled WerKernel, WER-Diag and WER-PayloadHealth operational channels have no matching entries in this window. Reliability records show the unexpected shutdown, a prior Defender intelligence update and postboot GameInput reconfiguration; no additional causal detail. Resource-Exhaustion-Detector only records postboot detector startup, and Resource-Exhaustion-Resolver has no matching event. No incident memory-exhaustion diagnosis was recorded. Postboot RyzenMaster signature/service-start failures and DistributedCOM warnings are not evidence of the earlier GPU timeout's cause.

The postboot ACPI thermal event describes `\_TZ.TZ10` at `_TMP=290K` during startup. It is not an RX 6800 XT junction-temperature trace and does not prove GPU overheating. No incident GPU temperature, power, clocks, fan telemetry, throttling or GPU memory-usage time series is available. The reported fan ramp alone remains causally inconclusive.

At 00:32 EDT after restart, CIM reports about 21 GiB free physical memory, C: about 91.0 GB free and E: about 467.2 GB free. These values show no current storage exhaustion, but cannot reconstruct incident memory pressure. No new diagnostic GPU/raw capture was generated by this worker.

Postrestart Win32_VideoController reports RX 6800 XT driver `32.0.21045.5002` and also two virtual monitor adapters. It lists the RX 6800 XT at 2560×1440/144 Hz and virtual adapters at 3440×1440/180 Hz (one virtual adapter reports Error). This is adapter-provider state, not proof of the actual display running NovaCore when it failed; the presentation worker must resolve active display ownership. No virtual adapter is attributed as the cause by this evidence.

## Conclusion for Project Control

There is positive Windows GPU-engine-timeout evidence, not merely a report of high fan speed or an unexplained Kernel-Power event. However, M13.6 causality, the timed-out command/resource/process, and any connection to uncapped rendering remain unresolved. Do not mask the failure with a frame cap or clear manual acceptance from prior automated tests. Preserve the current unbanked candidate while obtaining the fresh dump/incident runtime evidence; pacing can be assessed independently by its assigned owner.

Retained output consists only of this bounded report and the approximately 46 KB structured evidence record. No bulk data was created, copied or disposed; disposable output from this worker is zero. Existing incident and historical files remain in place.
