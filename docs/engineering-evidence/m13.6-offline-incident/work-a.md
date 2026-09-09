# Work A — bounded offline incident metadata refresh

The readable metadata still proves a Windows `LiveKernelEvent 0x141` display-engine timeout. It does **not** identify the initiating application, resolved driver module, GPU engine, failing allocation, command, Vulkan result, or synchronization violation. The new device identities below narrow what two **postboot** warnings describe; they do not narrow the initiating cause of the timeout. M13.6 remains neither attributed nor exonerated.

This work refreshed only 2026-09-08 **00:03:10–00:23:10 EDT (UTC−04:00)**, covering the known 00:13:10 report and subsequent restart. It did not widen the window. The companion `work-a.json` preserves exact timestamps, event record IDs, event types, named event data, query counts, device properties, and SHA-256 identities of the previous evidence package. The earlier `m13.6-manual-failure` package was read and left unchanged. No NovaCore execution, GPU workload, build, production edit, registry/ACL change, protected-file access, or dump parsing occurred.

## Correlated timeline

All times below are September 8 EDT. The JSON retains subsecond precision and UTC timestamps.

| Local time | Provider / event / type | Exact responsibility and attribution limit |
| --- | --- | --- |
| 00:10:13 | Retrospective field in EventLog 6008 | Reported previous unexpected shutdown time; not a measured freeze-onset timestamp. Later records exist before reboot. |
| 00:10:40.0162139 | Tcpip 4266, Warning; record 12744 | Global UDP ephemeral-port allocation failed because ports were in use. No responsible PID is supplied. This does not establish a graphics cause. |
| **00:13:10.0808385** | **Windows Error Reporting 1001, Information; record 15585** | **Fresh LiveKernelEvent 141**, ReportId `5880d346-5ecd-4c93-a8eb-576e32823930`, report status 4. |
| 00:14:01.500 | CIM LastBootUpTime | Restart boundary; postboot observations cannot be substituted for incident-frame state. |
| 00:14:02.2358503 | Microsoft-Windows-Kernel-Boot 20, Information; record 12750 | Last shutdown unsuccessful; last boot successful. |
| 00:14:05.5421013 | Microsoft-Windows-Kernel-PnP 219, Warning; record 12778 | `\Driver\WUDFRd` failed to load for `ROOT\DISPLAY\0000`, status `0xC0000365`. Current device identity is Meta Virtual Monitor, detailed below. |
| 00:14:05.5435891 | Microsoft-Windows-Kernel-Power 41, Critical; record 12779 | Reboot without clean shutdown. BugcheckCode 0; this is a consequence record, not proof of a power, thermal, driver, or application cause. |
| 00:14:06.0322663 | Microsoft-Windows-Kernel-PnP 219, Warning; record 12783 | Same load status for `ROOT\SYSTEM\0004`; current device identity is FakerInput Device. |
| 00:14:20.7873331 | EventLog 6008, Error; record 12759 | Unexpected-shutdown notification containing the retrospective 00:10:13 time above. |
| 00:14:22.0483275 | Microsoft-Windows-UserModePowerService 12, Information; record 12862 | `atieclxx.exe`, PID 3892, reset the power policy from `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` to the **same** GUID. This names an AMD postboot process; it does not identify the 141 initiating module. |
| 00:14:23.1358628 / .1383729 | Service Control Manager 7000, Error; records 12869/12871 | RyzenMasterDriver V28/V31 start failures because Windows could not verify digital signatures. Postboot CPU utility service failures; no causal link to the prior GPU timeout. |
| 00:14:23.1760965 / .1855203 | Kernel-PnP/Configuration 400/410, Information; records 1122/1123 | Xbox `SWD\XvddEnum\XvddRootDevice_Instance` configured/started (`oem16.inf`, Xvdd). No GPU fault attribution. |
| 00:14:24.5908999 | Windows Error Reporting 1001, Information; record 15623 | Re-report of the **same** fresh 141 ReportId, parameters, and dump; report status 2049. |
| 00:14:30.044 | Reliability / MsiInstaller 1035 | Microsoft GameInput 3.3.221.0 reconfiguration, status 0. No application crash attribution. |
| 00:14:52.7346871 | Service Control Manager 7000, Error; record 12883 | Repeated RyzenMasterDriver V31 signature failure. |
| 00:17:21.3318743 | Windows Error Reporting 1001, Information; record 15689 | Another re-report of the **same** fresh 141, report status 2049. |

The AMD process path in the policy event is:
`C:\Windows\System32\DriverStore\FileRepository\u0203303.inf_amd64_e5876a26758f154c\B026363\atieclxx.exe`.
The event does not say that this executable initiated the timeout, performed the failing GPU operation, or was on a fault stack.

## Duplicate correlation and available fault identity

The narrow Application query contains 40 LiveKernelEvent report records. Three reference the fresh September 8 dump and identical ReportId above. The remaining records refer to previously retained September 2/6 watchdog reports. These are reporting/reprocessing timestamps, not 40 new incident-time GPU failures. Their exact report identities, codes, filenames, and timestamps are retained as a compact mapping in JSON; prior full evidence need not be duplicated.

Fresh parameters remain `P1=141`, `P2=ffffe603859e4010`, `P3=fffff8079758b6a0`, `P4=0`, `P5=ffffe6035aae30c0`, `P6=10_0_26200`, `P7=0_0`, `P8=256_1`. Analysis symbol, bucket and hashed bucket remain empty. The driver-pointer parameter has not been resolved to a loaded module by this metadata work. It is not a GPU-engine identifier or proof of driver culpability.

The event XML execution fields name writer PIDs/TIDs `36372/33152`, `9652/9656`, and `24468/24476`. They describe event emission contexts; **none is an established GPU-fault initiator PID**. There is no NovaCore process path, command line, failing primitive, Vulkan message/VUID, or last frame in these records.

The exact referenced fresh dump remains `C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp`. The exact report folder remains `C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930`. Protected dump/`Report.wer` access belongs to the parent investigation. This worker did not repeat the prior access-denied checks or infer fresh readability from an event reference.

## New equivalent device metadata

Read-only PnP queries now map the two postboot warning instances precisely:

| Instance | Current identity | Driver / service / INF | Current status |
| --- | --- | --- | --- |
| `ROOT\DISPLAY\0000` | Meta Virtual Monitor; Display; Meta Inc. | 5.3.57.114 / WUDFRd / oem189.inf | OK; problem 0 |
| `ROOT\SYSTEM\0004` | FakerInput Device; System; Ryochan7 | 0.1.0.0 / mshidumdf / oem81.inf | OK; problem 0 |

The first warning therefore must not be relabeled an AMD physical-display driver load failure. Neither mapping proves which monitor hosted Florida, which adapter executed the failed work, or whether either virtual device participated in the hang. Current OK status is also not evidence of correct incident-time operation.

## Narrow negative results and coverage limits

The refreshed System query contains 157 records; Application contains 139. Relevant selected System records and all watchdog identity mappings are retained; routine processor/boot enumeration is not duplicated.

- No WHEA-provider event, Display 4101 recovery event, or AMD/Dxg fault-provider event appears in this window. The AMD-named utility events above are preserved separately and are not excluded from the interpretation.
- No Application Error 1000 or Application Hang 1002 record appears. That does not establish that NovaCore exited cleanly or that error logging survived the hang.
- Reliability records in the same window still provide only shutdown 6008 and GameInput reconfiguration 1035. Reliability contributes no new live-kernel process/module/engine resolution.
- Enabled WerKernel, WER-Diag, WER-PayloadHealth, DeviceSetupManager/Admin, and Resource-Exhaustion-Resolver operational channels contain no matching event.
- Resource-Exhaustion-Detector contains two postboot initialization records, with no incident-time pressure attribution.
- DriverFrameworks-UserMode/Operational is **disabled**, so its zero records are unavailable instrumentation, not a negative fault result. It was not enabled for this task.
- Kernel-PnP/Configuration contains only the two Xbox Xvdd configuration/start events above.

No new incident-time temperatures, GPU power/clocks, engine status, utilization, allocation pressure, frame rate, or present trace became available. The user explicitly supplied **no FPS reading**; high fan noise and the roughly five-minute Florida/fullscreen/normal-diagnostics session do not supply a numerical rate or thermal cause.

## Disposition and remaining proof

**No new causal narrowing beyond the already established display-engine timeout. Initiating cause: INSUFFICIENT EVIDENCE.** This work supplies useful exclusions against misreading nearby events, not an exoneration of NovaCore, M13.6, the driver, overlays/virtual displays, hardware, or frame pacing. Kernel-Power 41 is not a cause classifier.

A stronger classification requires a concrete incident link from readable protected evidence or other exact incident data: resolved module/stack, affected GPU engine/context and owning process, timeout/recovery status, or a specific invalid resource/command/lifetime. The parent owns that bounded offline investigation. This worker recommends no production change or replay based on these metadata alone.

Retention: only this report and its compact JSON refresh. Prior failure-package content is preserved and hash-inventoried in JSON. No raw capture or duplicated dump was created.

