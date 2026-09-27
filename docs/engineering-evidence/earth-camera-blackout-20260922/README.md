# Earth-camera blackout investigation — 22 September 2026

**Blackout cause UNRESOLVED. Bounded error-handling and diagnostics correction built and tested without a GPU. Manual acceptance remains on hold. UNBANKED.**

The player reported both monitors turning black while viewing Earth and moving the camera, followed by a required PC restart. In the follow-up, the player confirmed the restart was around 10:49 AM and that they were **zooming close to Earth and moving the camera, with ground and horizon visible, in borderless fullscreen at native resolution, after a few minutes**. This investigation did not launch NovaCore or recreate the GPU workload. The existing normal manual-test build is preserved; the new build is a separate diagnostic candidate, not a replacement accepted release.

## Evidence and its limits

| Evidence | Observation | What it establishes |
| --- | --- | --- |
| Windows boot | Last boot 2026-09-22 10:49:36 EDT | Player confirms this approximate restart time belongs to the reported incident. Exact blackout onset and executable remain unverified. |
| Kernel-Power 41 | 10:49:40 EDT; BugcheckCode 0; no recorded power-button timestamp | An unclean restart, not a root-cause diagnosis. |
| EventLog 6008 | 10:49:55 EDT; message reports previous shutdown at 10:11:35 EDT | Windows recorded an unexpected shutdown. The embedded time must not be substituted for a confirmed player incident time. |
| Graphics watchdog reports | WER LiveKernelEvent 141 and AMD a1000001 entries were logged after reboot, but their named dumps date to September 2, 6 and 8; the same incidents were also reported earlier today | Historical graphics incidents exist. These entries do **not** prove a new September 22 GPU timeout. |
| NovaCore memory report | WER RADAR_PRE_LEAK_64 for NovaCore.exe at 01:27:15 EDT | A separate earlier memory-related diagnostic. No retained allocation trace or demonstrated connection to this blackout. It predates the current apphost's 02:15 write time. |
| Other Windows evidence | No Display 4101, WHEA, volmgr or resource-exhaustion event returned in the selected September 22 System-log query | No corroborating event in that query; this does not exclude hardware, driver, power or application-triggered failures. |
| Protected dumps | LiveKernelReports and queued kernel Report.wer access denied to this session | Their contents were not analyzed; no access permissions were changed. |
| Installed adapters | Radeon RX 6800 XT and integrated Radeon Graphics report driver 32.0.21045.5002 | Installed driver identity, not confirmation of which adapter rendered the incident. |
| Normal application diagnostics | App was WinExe; renderer wrote Console output without retaining a session file | No accessible renderer trace was available for the reported normal launch. |

Microsoft explains why Event 41 with zero bugcheck data cannot by itself identify the cause: [Event ID 41 troubleshooting](https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/event-id-41-restart). The source events and bounded machine inventory are retained locally in `build/earth-camera-blackout-20260922/windows-evidence.json`.

No diagnosis of a faulty GPU, faulty driver, memory leak, power supply fault or specific Earth shader is established. Source inspection found a tessellation limit of 64; a limit alone is not a dynamic GPU-workload qualification. No terrain resolution, visual quality, physical model, camera law, GPU selection, driver setting or Windows timeout was changed.

## Demonstrated defects corrected

1. **A resize could mask a fatal presentation result.** Previously, `vkQueuePresentKHR` returning device-lost while the resize/suboptimal flag was set entered swapchain reconstruction before checking the failure. Fatal results now win over resize recovery. Ordinary success, suboptimal and out-of-date behavior remains explicitly classified.
2. **Failed synchronization results were ignored.** Fence reset, patch-buffer growth idle wait, swapchain idle wait and normal shutdown idle wait now report failures through the existing renderer failure boundary. Failed waits cannot silently authorize rebuilding those resources. Cleanup retains the idle wait and records its result without throwing out of cleanup.
3. **Normal sessions discarded diagnostic output.** The product entry point now captures both console streams, managed unhandled exceptions, application exit status, and existing native diagnostics. Startup records Vulkan device/driver identifiers; a five-second renderer checkpoint records the most recently completed frame, viewport, application/surface mode, patch count, Earth detail ownership, camera coordinates and fence-wait duration.

These are source-proven defects and bounded corrections. **None is proven to have caused this incident.** Error handling runs only if control returns to the application; it cannot guarantee recovery from a whole-system hang. Vulkan explicitly distinguishes logical device loss from failures that may crash the operating system: [Lost Device specification](https://docs.vulkan.org/spec/latest/chapters/devsandqueues.html#devsandqueues-lost-device).

## Session-log behavior

- The new normal application entry point creates a unique directory under `%LOCALAPPDATA%\NovaCore\Logs` for each run. `session.txt` records time, process path, runtime and app module identity. Existing renderer output records further runtime fingerprints.
- Four rotating segments retain recent output. Each segment has a 2 MiB threshold plus at most one bounded batch of overshoot. Only this session's own segment files are overwritten; older session directories are preserved.
- Two fixed 32 Ki-character buffers bound pending capture memory. Overload records an explicit dropped-character count. Individual lines can be truncated by overload; logs are not a lossless flight recorder.
- A background timer drains once per second and requests a disk flush. The render thread copies existing diagnostic text into the buffer; it performs no routine log-file I/O. Process memory is sampled every ten seconds on the background callback.
- Explicit exception/exit handling flushes synchronously. The last batch can still be lost during a power failure, driver/OS hang, delayed callback or storage failure. A missing end marker means an incomplete log, not a proven crash.
- Storage failures are retained in the logger's failure state and do not throw into rendering. The logger does not automatically remove old sessions. Whole-application performance and real GPU-error recovery remain unqualified for this correction.

## Validation

| Check | Debug | Release | Boundary |
| --- | --- | --- | --- |
| Native library and shaders build | PASS | PASS | Compilation; existing ABI static assertions remain enforced |
| Native presentation-result tests | 18 PASS | 18 PASS | CPU-only outcome classification, including device loss during resize; no real device loss injected |
| Managed application build | PASS, 0 warnings/errors | PASS, 0 warnings/errors | Separate artifact output; no application launch |
| Session diagnostics tests | 113 PASS | 113 PASS | Forwarding, live/background flush, partial writes, concurrent lines, bounded flood/rotation, newest evidence, final flush, storage failure and exact zero additional allocation for warmed capture |
| Source whitespace check | PASS | PASS | Affected tracked source |
| Actual Earth traversal / GPU validation / full application performance | NOT RUN | NOT RUN | Avoided re-exposing the desktop to the reported hang |

Development checks first exposed a test-reader Windows sharing issue and an ambiguous Timer type in the WinForms build. Both were corrected before the final successful runs. They are not incident-cause findings. This pass does not replace or re-claim the prior campaign's full regression/integration/manual acceptance.

Reproduction commands and complete outputs are retained in `build/earth-camera-blackout-20260922/`: `build-native-debug.cmd`, `build-native-release.cmd`, `native-*-build.log`, `app-*-build.log`, and `log-tests-*.txt`. The managed commands use `--artifacts-path build/earth-camera-blackout-20260922/managed` and the matching `NativeBuildDirectory=earth-camera-blackout-20260922/native-debug` or `native-release`. Run the new CPU diagnostics project explicitly; it is not added to the solution.

The separate Release diagnostic build is:

`E:\NovaCore\build\earth-camera-blackout-20260922\managed\bin\NovaCore.App\release\NovaCore.exe`

Its existence is **not** a request to repeat the failing route immediately. The earlier normal executable at `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe` remains byte-identical and does not contain this correction.

## Preservation

The entry inventory contains 3,501 existing files. Only these three existing files changed: `native/NovaCore.Native/NovaCoreNative.cpp`, its `CMakeLists.txt`, and `tools/NovaCore.App/Program.cs`. Five implementation/test files were added. The other 3,498 entry files remain byte-identical; none is missing. All 126 files in the previous normal Release application output remain byte-identical.

HEAD, Git index, refs and worktrees remain unchanged. Entry snapshots, file hashes, build identities and preservation results are under `build/earth-camera-blackout-20260922`; the compact final identity and preservation report is [validation.json](validation.json).

KSA writes: **0**. Driver/system setting changes: **0**. No commit, tag, push, deployment promotion, milestone assignment or cleanup. No Player PASS or system-crash-prevention claim.

## Remaining unknowns and next diagnostic boundary

1. Approximate restart time, ground/horizon view, zoom/movement, borderless fullscreen at native resolution and a few minutes of runtime are confirmed. Exact executable, altitude, monitor/pixel dimensions and continued audio/PC activity remain unknown. Do not substitute the previous 1280×720 qualification workload for the reported route.
2. Determine whether a current incident dump exists, as distinct from the older watchdog reports. Protected dump contents were unavailable in this session; acquiring access must not be confused with evidence that a new dump exists.
3. Establish a causal trace: last progressing renderer frame, actual selected GPU, memory progression, native result/validation errors and camera transition. The new logs are preparation for this step, not its result.
4. Decide on a controlled, supervised reproduction after the incident details are reconciled. Start with bounded exposure, preserve the log and stop on the first abnormality; do not run an unattended stress/reboot loop. A repeat on this PC may still hang it.
5. Qualify the demonstrated fix against the actual cause, then repeat affected live application and performance checks before Project Control manual acceptance resumes.

## Follow-up after player confirmation

**No further production changes or GPU execution in this follow-up.** Four existing CPU camera regressions passed in each of Debug and Release (eight case executions): bounded-domain extreme zoom with repeated near-Earth drags, surface-anchor handoff monotonicity, terrain exclusion, and camera-drag isolation. The extreme-zoom case includes 24 inward/drag/outward cycles and retained at least 10.000007 m clearance in its Release fixture. These tests exercise camera calculations and CPU terrain/transport checks; they do not reproduce GPU terrain refinement, display-driver behavior or the blackout. A test log's `gpuClearance` label describes transported-coordinate arithmetic, not GPU execution here.

Source inspection confirms one managed replacement-preparation task at a time, bounded current/incoming ownership, and a finite 18-level topology library. No new camera or lifecycle defect was demonstrated by this pass. The near-surface shader performs perspective divisions when estimating tessellation; numerical edge cases and actual refinement load remain investigation targets, not an attributed crash cause or justification for an unmeasured shader/quality change.

The saved launcher settings were last written at **10:46:54 EDT**, before the confirmed restart, and request borderless fullscreen, native desktop resolution and performance telemetry. The player independently confirms the display mode and native resolution. The preferences are not a capture of the selected GPU, effective telemetry, exact pixel dimensions or process start time. In particular, the saved Florida preset is retained by the unified application while it starts Solar overview, so it cannot prove a Florida startup route. The actual fullscreen workload is not covered merely by the previous 1280×720 application qualification.

An earlier [September 8 offline incident investigation](../m13.6-offline-incident/README.md) records Earth/Florida zoom-out, a system freeze, and a fresh 141 watchdog report on the same GPU/driver combination. Its cause remained unresolved. That history is relevant but does not prove today's zoom-in incident has the same cause, nor attribute either incident to one NovaCore change. It explains why re-reported September 8 watchdog records must not be treated as fresh September 22 failures.

The earlier [instrumented observation protocol](../m13.6-offline-incident/retest-protocol.md) also distinguishes ordinary session logs from an independent observer with durable operation breadcrumbs and verified stop behavior. The new session logger is useful evidence collection, but **does not implement that complete protocol or establish that a repeat is safe**. Protected incident evidence and this instrumentation gap remain to be resolved before a supervised reproduction is proposed.

Exact follow-up commands/results, log hashes, saved-setting observation and source/build preservation checks are in [camera-followup.json](camera-followup.json). Original validation identities remain historical records of the preceding correction.
