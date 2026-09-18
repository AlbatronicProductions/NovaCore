# Reproduce the bounded check

Inputs: unchanged current candidate sealed by preflight.json, native source hash, .NET SDK10.0.303, Visual Studio18 Community native tools, Vulkan SDK1.4.357.0. Windows local console and RX6800XT. This is an instrumented startup check, not historical-harness equivalence.

From PowerShell7, repository `E:\NovaCore`:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\startup-first-present-correlation\build-probe.ps1'
```

The builder checks source seals, retains temporary backups only in its bounded scratch root, injects native-startup.h and managed-startup.cs.txt, adds the earliest managed-entry stamp, builds Release, then restores native/scene/Program source bytes in `finally` before any capture. Native existing frame and managed legacy sample expressions stay unchanged. Timestamp overhead is real and is not subtracted.

Original predeclared execution order: **A1 B1 B2 A2**,180frames each. Call capture.ps1 once per ID, inspecting correctness before the next. The script refuses an ID already attempted; no overwriting/replacement of evidence. No intentional input, no resolution override (actual960x540), no extra logger flags, no parallel build/profiler. Any future rerun needs authorization and a new bounded output identity; do not enlarge this completed four-run population.

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\startup-first-present-correlation\capture.ps1' A1
python 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\startup-first-present-correlation\analyze.py'
```

Analyzer input: raw `STARTUP_NATIVE`, `STARTUP_FRAMES` and `STARTUP_MANAGED` stdout records plus per-process metadata under the disposable root. Output: captures.json, including native code mapping, exact QPC ticks and offsets, first12callback/accounting rows, full lifecycle event trace for eight frames, complete-capture quantiles and canonical summary. QPC/Stopwatch frequencies must match. Post-render reference replay compares every sampled copied endpoint before printing the managed report.

Native code4 is **window-initialization entry**, not exact RunRenderer function entry. T0 is first managed entry, not OS creation. T2 follows bootstrap. T5/T6 are nested inside T3/T4. First strict `VK_SUCCESS` return is T6; all attempted results remain in the trace. T7 follows success and the graphics-submit fence, with38initialsceneobjects; it is not scanout/perception proof. T8 is entry to the first service invocation that advances frontier. T9 is copied publication observation after that service; where multiple publications occurred, first commit is bounded inside the call, not timed individually.

Do not merge window-to-present wall time with the legacy display-counter interval. Native overall wall-time average includes post-run reporting and is not used. The script intentionally stops a live episode after180frames; `completed=False, failed=False` is expected. Validate conservation, replay and authority instead of demanding1200publications from this startup-only run.

No production correction, KSA investigation or further captures follow from this check automatically. Exact cleanup paths/commands are retained in cleanup.md; prior population scratch is not an input dependency and was not modified.
