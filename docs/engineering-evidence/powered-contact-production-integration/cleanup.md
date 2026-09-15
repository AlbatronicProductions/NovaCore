# Exact disposable inventory

This ticket created one disposable root, absent before its first build:

| Exact path | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\build\powered-contact-production-integration\artifacts\bin | 80 | 30,256,004 |
| E:\NovaCore\build\powered-contact-production-integration\artifacts\obj | 144 | 14,069,432 |
| E:\NovaCore\build\powered-contact-production-integration\ContactConvexTypes.upstream.cs.txt | 1 | 104,525 |
| **Root total: E:\NovaCore\build\powered-contact-production-integration** | **225** | **44,429,961** |

No other new disposable tree, raw trace, stdout log, probe project or profiler output was
created. The compiled tests, PDBs, incremental compiler state and downloaded upstream source
are reproducible from the retained candidate, pinned dependencies and generation script.
Candidate source/test and accepted prerequisite evidence are KEEP. The whole reviewed root
above is DISPOSE. Earlier build trees and five pre-existing probe projects are outside this cleanup.

## Reviewed manual cleanup command

Exact copy/paste-ready PowerShell form, safe when the target is already absent:

```powershell
if (Test-Path -LiteralPath 'E:\NovaCore\build\powered-contact-production-integration') {
    Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-production-integration' -Recurse -Force
}
```

## Non-destructive verification

```powershell
$target = 'E:\NovaCore\build\powered-contact-production-integration'
if (Test-Path -LiteralPath $target) {
    $files = @(Get-ChildItem -LiteralPath $target -Recurse -File -Force)
    [pscustomobject]@{ Path = $target; Exists = $true; Files = $files.Count; Bytes = ($files | Measure-Object Length -Sum).Sum }
} else {
    [pscustomobject]@{ Path = $target; Exists = $false; Files = 0; Bytes = 0 }
}
```

## Actual disposition

One automatic deletion attempt was rejected before execution by automatic approval review:
`blocked by policy`. No retry or alternate deletion mechanism was attempted. **Removed:0
files/0bytes. Retained:225files/44,429,961bytes.** Manual Project Control cleanup is required
using the exact reviewed command above. This is separate from the engineering performance blocker.
