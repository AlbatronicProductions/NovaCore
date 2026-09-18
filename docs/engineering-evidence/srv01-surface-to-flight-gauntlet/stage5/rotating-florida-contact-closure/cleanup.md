# Exact disposable inventory and deferred manual cleanup

No deletion was attempted. The six predecessor output roots remain explicitly retained by Project Control; they include executable/checkpoint dependencies. The new causal-probe root contains rebuildable diagnostic output. Its exact source and results are preserved in this evidence package. The later mission-feasibility gate created no additional build/bin/obj tree.

All targets below were resolved and verified to be immediate children of E:\NovaCore\build. No production source, asset, legal review or evidence directory is a cleanup target.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106016279 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103968588 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103439190 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-stage3` | 1867 | 388109071 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-stage4` | 1536 | 328974343 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-stage5` | 193 | 33692955 | RETAIN — preceding checkpoint/build dependency; no deletion in this ticket |
| `E:\NovaCore\build\srv01-stage5-closure` | 53 | 9556239 | DISPOSABLE — exact probe sources, seals, results and reproduction retained; not deleted |

Total: 4806 files, 1073756665 bytes. Removed: 0 files / 0 bytes.

## Reviewed manual command — retained for later release, NOT for execution now

Execute only after Project Control releases the six preserved roots and their reproducibility dependencies have been retained/rebuilt elsewhere. Do not use this command to override their current retention requirement. It is printed now to preserve exact cleanup reporting; no deletion workaround or retry was performed.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-display-gap-population','E:\NovaCore\build\srv01-startup-first-present','E:\NovaCore\build\srv01-stage2-manual','E:\NovaCore\build\srv01-stage3','E:\NovaCore\build\srv01-stage4','E:\NovaCore\build\srv01-stage5','E:\NovaCore\build\srv01-stage5-closure' -Recurse -Force -ErrorAction Stop
```

Non-destructive existence verification (safe now):

```powershell
'E:\NovaCore\build\srv01-display-gap-population','E:\NovaCore\build\srv01-startup-first-present','E:\NovaCore\build\srv01-stage2-manual','E:\NovaCore\build\srv01-stage3','E:\NovaCore\build\srv01-stage4','E:\NovaCore\build\srv01-stage5','E:\NovaCore\build\srv01-stage5-closure' | ForEach-Object { [pscustomobject]@{ Path=$_; Exists=Test-Path -LiteralPath $_ } }
```

Expected now: all True. After a later authorized complete cleanup: all False. Re-inventory before any later deletion if these paths have changed. Do not retry deletion of absent paths.
