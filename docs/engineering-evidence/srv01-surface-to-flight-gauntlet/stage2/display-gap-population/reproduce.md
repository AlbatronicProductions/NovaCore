# Reproduce this bounded comparison

Prerequisite: the exact 24 candidate source/test identities in preflight.json and the sealed native source; branch/HEAD as recorded. Run from PowerShell 7 on the recorded Windows host, .NET SDK 10.0.303, Visual Studio 18 native tools and Vulkan SDK 1.4.357.0. No claim of identical host scheduling on a later run.

The exact observer is retained as `managed-observer.cs.txt` and deterministic replacements in `build-observer.ps1`. Source backups/build/logs are disposable, under one bounded root. The builder restores the two touched files in `finally`, verifies byte hashes, and fails closed on unexpected input. No modified source remains in the candidate. Do not launch another build while those transient replacements are active.

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\display-gap-population\build-observer.ps1'
```

Then invoke `capture.ps1` once for each ID in exactly `A1 B1 C1 B2 C2 A2 C3 A3 B3`. The script rejects existing attempt records and verifies restored source before each launch. Do not run this over existing evidence; a newly authorized repetition needs its own declared root/count, not overwritten attempts. Each visible window starts automatically and exits after 4,000 callbacks. No camera/input/title-bar manipulation. No parallel benchmarks/builds. Review each capture for correctness/concrete-cause stop rules before the next. Current native default is **960 x 540**, corrected explicitly in protocol-note.md; never infer 1280 x 720 from the original plan typo.

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\display-gap-population\capture.ps1' A1
python 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\display-gap-population\analyze.py'
```

The analyzer requires the disposable raw reports, produces captures.json, preserves unexecuted/invalid attempts, uses nearest-rank quantiles and pools only technically valid captures. Full-capture intervals are separate from untouched legacy active-episode metrics. Native frame indices and after-title interval indices are not interchangeable. The overall native wall-time average is excluded because post-run output extends it. No hot logging, phase profiler, CPU sampling or allocation observer is added.

After rendering, a separate same-route one-interval-at-a-time reference compares copied endpoint motion bits, exact stores, all mass properties, revisions, history count and final state; it is outside timing and runtime-GC sampling. Geometry is independently evaluated from transformed child corners for sampled endpoints; retained qualified all-interval checks remain the full-support authority.

No manual acceptance or Stage3 work follows a nonreproducing result without Project Control resolving the retained blocker. Historical witnesses remain in ../closure and ../display-gap-closure.
