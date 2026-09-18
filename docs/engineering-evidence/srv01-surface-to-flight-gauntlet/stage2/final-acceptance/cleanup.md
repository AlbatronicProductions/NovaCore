# Reviewed Stage-2 disposable output

The final package consolidates qualification, both historical anomalies, direct diagnostics, the nine-capture population, the four-capture startup check and the Project Control manual result. Keep all concise source/evidence/reproduction files. No KSA source/assets, candidate source or unique canonical data is in the reviewed disposable roots.

| Exact disposable root | Files before cleanup | Bytes before cleanup | Responsibility |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106016279 | Previous population's rebuildable binaries/logs/backups. Earlier automatic cleanup blocked; no retry in this ticket. |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103968588 | This targeted check's isolated build, backups and bulk logs; exact useful timelines/results/identities retained. |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103439190 | Plain Release manual bundle and completed manual log; results/seals/reproduction retained. |

Total before cleanup and independently verified remaining: **1157 files / 313424057 bytes**. **Removed: 0 files / 0 bytes.** Automatic approval review rejected deletion of the two newly reviewed roots as `blocked by policy` before execution. The older population root was not retried. All three require the reviewed manual cleanup below; no workaround was attempted. The manual process is closed. All roots resolved within `E:\NovaCore\build`; no reparse points or Git-tracked files. `cleanup.json` records actual final disposition. Do not infer deletion from this eligibility classification.

Exact reviewed PowerShell command for remaining targets, skipping any already absent path:

```powershell
@(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
) | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        Remove-Item -LiteralPath $_ -Recurse -Force -ErrorAction Stop
    }
}
```

Non-destructive verification:

```powershell
@(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
) | ForEach-Object {
    [pscustomobject]@{ Path = $_; Exists = Test-Path -LiteralPath $_ }
}
```

Expected after manual cleanup: all `Exists=False`. No automatic retry/workaround of a rejected deletion. No deletion of historical retained evidence. Cleanup does not change qualification or grant Stage-3 promotion.
