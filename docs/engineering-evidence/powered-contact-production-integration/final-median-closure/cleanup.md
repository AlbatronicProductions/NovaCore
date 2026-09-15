# Reviewed disposable cleanup

Cleanup classification: DISPOSE both complete build roots below. They contain only generated
build/bin/obj output, diagnostic preimages and raw build/test/attribution output. Useful
results are preserved in the two concise evidence folders, candidate source/tests, first
correction delta and exact identity/restoration records. Reproduction instructions remain.

| Exact reviewed root | Files | Bytes | Current disposition |
|---|---:|---:|---|
| E:\NovaCore\build\powered-contact-lifecycle-correction | 253 | 44,749,838 | RETAINED, manual cleanup required |
| E:\NovaCore\build\powered-contact-final-median-closure | 125 | 22,941,027 | RETAINED, manual cleanup required |
| **Total remaining** | **378** | **67,690,865** | No automatic deletion executed |

The earlier E:\NovaCore\build\powered-contact-performance-revision is independently
verified ABSENT (0 files/bytes). Its absence is not attributed to this run. No deletion of
that absent path was attempted. No other build or unrelated diagnostic root is included.

Automatic approval review rejected the two-root removal before execution as "blocked by
policy". No retry or alternative deletion mechanism was used. The read-only inventory was
then refreshed; cleanup.json records current truth. This is evidence hygiene, not a reason
to continue engineering after the insufficient-payoff stop.

## Exact copy/paste-ready manual command

```powershell
Remove-Item -LiteralPath `
    'E:\NovaCore\build\powered-contact-lifecycle-correction', `
    'E:\NovaCore\build\powered-contact-final-median-closure' `
    -Recurse -Force
```

## Non-destructive verification

```powershell
$targets = @(
    'E:\NovaCore\build\powered-contact-lifecycle-correction'
    'E:\NovaCore\build\powered-contact-final-median-closure'
)
foreach ($target in $targets) {
    $exists = Test-Path -LiteralPath $target
    $files = if ($exists) { @(Get-ChildItem -LiteralPath $target -Recurse -File -Force) } else { @() }
    [pscustomobject]@{
        Path = $target
        Exists = $exists
        Files = $files.Count
        Bytes = if ($files.Count) { ($files | Measure-Object -Property Length -Sum).Sum } else { 0 }
    }
}
```

Expected after successful manual cleanup: both Exists=False, Files=0, Bytes=0.
Do not delete docs/engineering-evidence, candidate source/tests or other build roots.