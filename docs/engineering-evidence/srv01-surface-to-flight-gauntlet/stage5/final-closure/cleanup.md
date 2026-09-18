# Closure cleanup inventory

No deletion attempted. Previously blocked deletions were not retried. All source/evidence/assets/KSA/legal work is excluded. Three KEEP roots remain dependencies for native rebuilding, the original diagnostic and the uninstrumented manual route.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106,016,279 | DISPOSE: results and exact source/reproduction inputs retained outside build |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103,968,588 | DISPOSE: results and exact source/reproduction inputs retained outside build |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103,439,190 | DISPOSE: results and exact source/reproduction inputs retained outside build |
| `E:\NovaCore\build\srv01-stage3` | 1,867 | 388,109,071 | KEEP NOW: native DLLs/shaders used by the current build; retain pending next decision |
| `E:\NovaCore\build\srv01-stage4` | 1,536 | 328,974,343 | DISPOSE: results and exact source/reproduction inputs retained outside build |
| `E:\NovaCore\build\srv01-stage5` | 193 | 33,692,955 | KEEP NOW: exact previous failing-population binaries directly referenced by the retained causal harness |
| `E:\NovaCore\build\srv01-stage5-closure` | 53 | 9,556,239 | DISPOSE: results and exact source/reproduction inputs retained outside build |
| `E:\NovaCore\build\srv01-stage5-stock` | 1,510 | 327,237,644 | KEEP NOW: sealed uninstrumented final executable and logs required for pending Project Control manual acceptance |
| `E:\NovaCore\build\srv01-stage5-final-closure` | 488 | 138,728,068 | DISPOSE: closure witnesses, exact observer/reproducer and hashes retained; not the manual executable |

Total remaining: 6,804 files / 1,539,722,377 bytes. Six released roots: 3,234 files / 790,682,707 bytes. Removed: 0 / 0.

Exact reviewed manual PowerShell command (literal allowlist, absent paths skipped):

```powershell
$reviewedPaths = @(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
    'E:\NovaCore\build\srv01-stage4'
    'E:\NovaCore\build\srv01-stage5-closure'
    'E:\NovaCore\build\srv01-stage5-final-closure'
)
foreach ($reviewedPath in $reviewedPaths) {
    if (Test-Path -LiteralPath $reviewedPath) {
        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop
    }
}
```

Non-destructive verification:

```powershell
'E:\NovaCore\build\srv01-display-gap-population',
'E:\NovaCore\build\srv01-startup-first-present',
'E:\NovaCore\build\srv01-stage2-manual',
'E:\NovaCore\build\srv01-stage3',
'E:\NovaCore\build\srv01-stage4',
'E:\NovaCore\build\srv01-stage5',
'E:\NovaCore\build\srv01-stage5-closure',
'E:\NovaCore\build\srv01-stage5-stock',
'E:\NovaCore\build\srv01-stage5-final-closure' | ForEach-Object {
    $files = @(if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -File -Recurse -Force })
    [pscustomobject]@{ Path=$_; Exists=Test-Path -LiteralPath $_; Files=$files.Count; Bytes=($files | Measure-Object Length -Sum).Sum }
}
```
