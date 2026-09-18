# Reviewed disposable output

All concise results, source/binary hashes, decisive allocation ledger, predecessor source inputs and reproduction instructions are retained outside these roots. No candidate source, assets, KSA installation or legal review material is targeted. None of these four paths is tracked. Temporary production instrumentation has already been restored; these directories contain rebuildable binaries/objects, logs and diagnostic scratch.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106,016,279 | Prior blocked cleanup; not retried |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103,968,588 | Prior blocked cleanup; not retried |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103,439,190 | Prior blocked cleanup; not retried |
| `E:\NovaCore\build\srv01-stage3` | 1,867 | 388,109,071 | One new cleanup attempt rejected before execution; no retry |
| **Remaining** | **3,024** | **701,533,128** | **Manual cleanup available** |

Disposed this ticket: **0 files / 0 bytes**. Automatic approval review rejected the Stage3 removal as **blocked by policy**. This is output hygiene, not a failed physics/ownership gate. Original cleanup-inventory.json retains measured counts.

Exact copy/paste-ready PowerShell command; literal allowlist only, absent targets skipped:

```powershell
$reviewedPaths = @(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
    'E:\NovaCore\build\srv01-stage3'
)
foreach ($reviewedPath in $reviewedPaths) {
    if (Test-Path -LiteralPath $reviewedPath) {
        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop
    }
}
```

Non-destructive verification (independent of the deletion block):

```powershell
'E:\NovaCore\build\srv01-display-gap-population',
'E:\NovaCore\build\srv01-startup-first-present',
'E:\NovaCore\build\srv01-stage2-manual',
'E:\NovaCore\build\srv01-stage3' | ForEach-Object {
    $files = @(if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -File -Recurse -Force })
    [pscustomobject]@{
        Path = $_
        Exists = Test-Path -LiteralPath $_
        Files = $files.Count
        Bytes = ($files | Measure-Object -Property Length -Sum).Sum
    }
}
```

After manual cleanup, all four should report `Exists=False`, `Files=0`. Do not remove retained evidence merely to obtain an empty worktree.
