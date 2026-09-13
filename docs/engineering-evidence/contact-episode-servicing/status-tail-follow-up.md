# Visible status and whole-frame tail follow-up

Baseline: `3e1028339e9e80648ed15c510644d871259c26bd`.
Branch: `codex/contact-episode-servicing`. UNBANKED; no milestone assigned.

**Outcome: status correction PASS; centered/tilted manual follow-up PASS; tail classification UNRESOLVED. Lead judgment: ESCALATE TO PROJECT CONTROL. Final classification: HOST-PACED CANONICAL CONTACT SERVICING — FOLLOW-UP REQUIRED.**

## Proven status cause

The prepared scene owns a managed status string, composes its full window label, and synchronously calls `SetWindowTextW`. The native window class and creation use `RegisterClassW` and `CreateWindowExW`. Its window procedure previously delegated unhandled messages to the unsuffixed `DefWindowProc`. This build does not define `UNICODE`: the original Release DLL import table proves **DefWindowProcA**, alongside **RegisterClassW/CreateWindowExW**. UTF-16 text delivered to that ANSI default handler is interpreted as narrow text; the zero byte following the initial ASCII `N` terminates it. This explains the first-character title for both native creation and managed status writes. It is not clipping, draw count, a transient managed pointer or BEPU status ownership.

Correction: explicitly call `DefWindowProcW` to match the registered Unicode window. The rebuilt Release DLL import table proves all three `W` entry points. No compiler-wide charset switch or UI framework was introduced. Other scenes retain their behavior; their generic native window titles also now retain full Unicode text instead of the unintended truncation.

Microsoft documents that `RegisterClassW` delivers text-bearing messages as Unicode and that unsuffixed Win32 functions select A/W via `UNICODE`. See [window class registration](https://learn.microsoft.com/en-us/windows/win32/intl/registering-window-classes) and [working with strings](https://learn.microsoft.com/en-us/windows/win32/learnwin32/working-with-strings). Local compiled imports, current source and visible roundtrip are the candidate evidence.

## Status/lifecycle boundary

The scene now presents **READY** until Space explicitly starts the prepared episode. READY has zero admitted host debt and no private step. Space establishes the host-sampling timestamp and **RUNNING**. It does not alter the canonical simulation pause state or rate; subsequent Space input is not a runtime pause feature. Completion holds the copied endpoint and displays **COMPLETED**. Refusal/terminal failure holds the previous copied endpoint and displays **FAILED**. There is no direct BEPU presentation read.

Each changed status uses `SetWindowTextW` followed by a full `GetWindowTextW` roundtrip equality check. Failure is explicit. The owned managed string and Windows' copied title have no retained temporary native pointer. The check/log happens only on status changes, outside the accepted simulation operation's allocation/timing gates.

The permanent copied-presentation test asserts full READY text, completion without RUNNING, retained string value across collection, and negative-input terminal failure that cannot become COMPLETED on a later call. It also retains the original geometry/camera/held-endpoint checks. Release focused status/presentation test: PASS. Native Release and affected managed Release builds: PASS, zero warnings/errors. Broad qualification is not repeated before the tail decision.

## Bounded observer and measurement limits

Exactly one opt-in observer is used: `NOVACORE_CONTACT_TAIL_PROBE=1`, enabled by `launch.ps1 <case> -TailProbe`. The planned live observations are one fresh centered and one fresh tilted manual process. No profiler, runtime configuration change or physics tuning is used.

The existing callback-to-callback interval is preserved. For every sample above **6.67 ms**, the observer reports its zero-based index and previous callback's private frontier range, host-credit elapsed time, service elapsed time and the arithmetic remainder outside those calls. The preceding callback's work belongs to the measured interval, rather than the current callback's later work. The first live interval includes start/status presentation; the final completion/title/presentation interval is now captured on the following callback without admitting any further credit. Final diagnostic formatting occurs after these captured intervals.

The observer uses preallocated value storage and two optional timing spans around existing calls. It cannot attribute the remainder to a specific OS wait, present operation, JIT action or GC event. Such facts remain UNAVAILABLE unless independently demonstrated. A four-interval backlog call is not the same performance unit as the accepted single-complete-operation ceiling. No profiled/diagnostic total replaces the accepted three-process performance qualification.

The prior logs retained only sorted distributions, so their exact old frame indices cannot be reconstructed. New observations preserve every over-threshold sample and lifecycle frontier. Do not retroactively assign a cause to 36.0642 or 40.4358 ms merely because a new run has a similar duration.

## Reproduction

In PowerShell 7, from the prepared candidate:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\contact-episode-servicing\launch.ps1' centered -TailProbe
& 'E:\NovaCore\docs\engineering-evidence\contact-episode-servicing\launch.ps1' tilted -TailProbe
```

Observe READY, press Space, observe RUNNING, perform the camera/support checks and one brief title-bar hold, observe COMPLETED and held endpoint, then close. Record approximately when the hold occurred and its duration. The exact status roundtrips, tail rows and final accounting are saved under `build/contact-episode-servicing-live/*-status-tail.stdout.txt`. These are new filenames; prior manual evidence is preserved.

The simulation production fingerprints remain identical to `identity.json`. Source changes in this follow-up are limited to the native default Unicode handler, sample status/start/reporting boundary, focused presentation tests and reproduction/evidence. No contact owner, clock conversion, solver, publication, acknowledgement, dependency, lattice or allocation gate is changed.

## Exact tail observations

The final closed-window log snapshots below supersede the preliminary centered summary previously captured while manual logs were still being replaced. No additional application execution was initiated during evidence consolidation. Both retained source files currently match the hashes and exact lines in status-tail-results.json. Earlier sorted-only observations remain historical in the original qualification evidence; their maxima cannot be assigned retrospective frame indices.

The user confirmed centered and tilted READY/RUNNING/COMPLETED readability, stable physical behavior, camera independence, brief title-bar holds, and final held endpoints. Full native string roundtrips passed. FAILED semantics passed the focused permanent negative-input test; no new manual failure injection was introduced.

All indices are zero-based callback-to-callback samples. Credit/service work belongs to the preceding callback. Every sample above 6.67 ms is included, including delays and backlog recovery.

| Case | Frame | Whole ms | Frontier | Credit ms | Service ms | Outside calls ms |
|---|---:|---:|---|---:|---:|---:|
| centered | 4 | 28.0666 | 0->1 | 0.0403 | 27.394 | 0.6323 |
| centered | 96 | 19.3493 | 32->33 | 0.0032 | 18.599 | 0.7471 |
| centered | 537 | 3048.6131 | 180->180 | 0.0015 | 0.0014 | 3048.6102 |
| centered | 650 | 92.7292 | 399->400 | 0.0009 | 0.0483 | 92.6800 |
| centered | 651 | 44.63 | 400->404 | 0.003 | 0.0692 | 44.5578 |
| centered | 652 | 22.3925 | 404->408 | 0.0031 | 0.0669 | 22.3225 |
| centered | 697 | 16.6168 | 423->423 | 0.0015 | 0.0007 | 16.6146 |
| centered | 699 | 10.6183 | 424->424 | 0.0006 | 0.0005 | 10.6172 |
| centered | 815 | 719.4324 | 463->463 | 0.0008 | 0.0007 | 719.4309 |
| centered | 816 | 28.2832 | 463->467 | 0.003 | 0.0712 | 28.2090 |
| centered | 817 | 26.412 | 467->471 | 0.003 | 0.0693 | 26.3397 |
| centered | 820 | 18.7238 | 479->483 | 0.001 | 0.0308 | 18.6920 |
| centered | 822 | 10.2576 | 487->491 | 0.0016 | 0.0384 | 10.2176 |
| centered | 1615 | 7.2443 | 775->776 | 0.0008 | 0.0331 | 7.2104 |
| tilted | 4 | 28.5446 | 0->1 | 0.017 | 27.7216 | 0.8060 |
| tilted | 91 | 19.9873 | 30->31 | 0.0045 | 19.3235 | 0.6593 |
| tilted | 101 | 9.2088 | 34->35 | 0.0041 | 8.6818 | 0.5229 |
| tilted | 163 | 10.9019 | 55->55 | 0.0009 | 0.0007 | 10.9003 |
| tilted | 198 | 11.1242 | 67->67 | 0.0007 | 0.001 | 11.1225 |
| tilted | 305 | 11.1114 | 103->103 | 0.001 | 0.0008 | 11.1096 |
| tilted | 421 | 11.1154 | 142->142 | 0.0015 | 0.0011 | 11.1128 |
| tilted | 881 | 11.1901 | 296->296 | 0.0007 | 0.0008 | 11.1886 |
| tilted | 1006 | 11.0476 | 338->338 | 0.0014 | 0.0012 | 11.0450 |
| tilted | 1970 | 11.1926 | 659->660 | 0.0007 | 0.0315 | 11.1604 |

Centered: **14 / 2,888** samples above threshold; median/P95/P99/max **5.5553 / 5.6952 / 6.0040 / 3,048.6131 ms**. Final frontier/revision/history 1,200; clock 20,000,000; retained debt 304 ticks.

Tilted: **10 / 3,590** samples above threshold; median/P95/P99/max **5.5557 / 5.7271 / 6.0062 / 28.5446 ms**. Final frontier/revision/history 1,200; clock 20,000,000; retained debt 342 ticks.

Both runs have an early first-private-step spike: centered service 27.3940 ms; tilted 27.7216 ms. Centered step 33 costs 18.5990 ms. Tilted steps 31 and 35 cost 19.3235 and 8.6818 ms and align with the retained M14.18 first-contact/far-edge-arrival trace. This establishes lifecycle association, not which internal operation causes the cost. These occur before the accepted 128-operation warmup and do not establish failure of the previously qualified warmed ceiling. They remain included as real cold live costs.

The centered maximum at frame 537 spans frontier 180->180: service 0.0014 ms, credit 0.0015 ms, outside those calls 3,048.6102 ms. Another delay at frame 815 is 719.4324 ms. Subsequent rows show bounded four-interval backlog servicing (for example 400->404 and 463->467); the measured backlog service calls remain below 0.50 ms, but four-interval calls are not the same timing unit as the accepted single-operation benchmark. Later tilted tails predominantly occur outside credit/service, often with no private step.

The user confirms intentional title-bar holds but supplied no precise time/duration correspondence. Large no-work delays are consistent with those holds, but the observer cannot positively assign every sample or the original maxima to them. Specific present/fence/OS wait, JIT and GC attribution is **UNAVAILABLE**. First-step/first-contact costs also remain internally unexplained. Preparation precedes live sampling. The final completion/title interval is included and is not over threshold. No reconstruction or render-per-private-interval behavior is demonstrated by the observations.

## Judgment and stop

**UNRESOLVED.** The bounded observation identifies actual early service costs and later time outside those calls, but cannot explain the entire original/new maximum family with a qualified single lifecycle/wait mechanism. A/B would overclaim; C's recurring avoidable production cause is not proven. No performance correction, extra profiler, broad regression, allocation rerun or three-process timing rerun was performed. The previously accepted subsystem results remain retained rather than relabeled as fresh results.

Manual centered: PASS. Manual tilted: PASS. Status: PASS. Simulation production bytes changed: **NO**. Presentation/native runtime bytes changed: **YES**, for Unicode/status readiness and the optional observer only. Minimum Release build/focused status checks passed; further regression expansion was not undertaken because the ticket requires stopping at D. The opt-in diagnostic remains bounded reproduction machinery for this still-open question; it does not enable itself in ordinary launches.

Full exact lines, changed-file hashes and native identity are in [status-tail-results.json](status-tail-results.json). The original candidate identity/result files remain historical qualification evidence. The two nonempty new probe logs are retained while the tail question is active; old redundant logs and empty stderr files are disposable after their results/provenance are retained. No unrelated scratch is in the cleanup scope.

Cleanup disposition: KEEP the two final nonempty status-tail stdout logs (28,642 bytes). DISPOSE the eight old logs and two empty new stderr files (ten files, 82,824 bytes). Exact paths, hashes and useful contact result lines from every disposable log are preserved in [status-tail-cleanup.json](status-tail-cleanup.json), including the updated tilted manual result. After the user explicitly renewed authorization, automatic approval review again rejected the exact hash-checked deletion before execution with `blocked by policy`. Nothing was deleted; no alternate deletion method was attempted. Current remaining total: twelve files, 111,466 bytes, of which ten files are disposable. This tool-enforced cleanup limitation is separate from the technical UNRESOLVED classification.

**UNBANKED. STOP FOR PROJECT CONTROL.**
