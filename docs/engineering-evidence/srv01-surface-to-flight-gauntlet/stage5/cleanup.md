# Reviewed disposable output and manual cleanup

All useful results are retained in the Stage-2/3 evidence and Stage-4/5 reports, source identities, concise measurements and reproduction instructions. The predecessor source archives are **KEEP** reproducibility inputs, not disposable build output. Source, profiles, physical datasets, KSA installation, legal-review files and Git state are excluded.

No deletion was attempted during this Stage-4/5 closeout. Earlier policy-blocked directories were not retried. The new outputs remain with the exact same manual review boundary. No cleanup success is claimed.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106016279 | Disposable; prior blocked output |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103968588 | Disposable; prior blocked output |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103439190 | Disposable; prior blocked output |
| `E:\NovaCore\build\srv01-stage3` | 1867 | 388109071 | Disposable; prior blocked output, native build dependency reproducible |
| `E:\NovaCore\build\srv01-stage4` | 1536 | 328974343 | Disposable; this turn's builds, logs and supported-layout test copy |
| `E:\NovaCore\build\srv01-stage5` | 193 | 33692955 | Disposable; this turn's Debug build and two cheap-attempt logs |
| **Total** | **4753** | **1064200426** | **Remaining** |

Machine-readable inventory: `disposable-inventory.json`. Native outputs are shared only by the disposable validation builds; fresh rebuild instructions replace this dependency. Retained candidate code/evidence does not depend on retaining their binary bytes.

## Exact reviewed manual PowerShell cleanup

These are literal resolved directories under `E:\NovaCore\build`. The existence guard skips already-absent paths. Close any test/sample processes before removing their rebuildable outputs.

```powershell
$reviewedPaths = @(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
    'E:\NovaCore\build\srv01-stage3'
    'E:\NovaCore\build\srv01-stage4'
    'E:\NovaCore\build\srv01-stage5'
)
foreach ($reviewedPath in $reviewedPaths) {
    if (Test-Path -LiteralPath $reviewedPath) {
        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop
    }
}
```

## Standalone non-destructive verification

```powershell
@(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
    'E:\NovaCore\build\srv01-stage3'
    'E:\NovaCore\build\srv01-stage4'
    'E:\NovaCore\build\srv01-stage5'
) | ForEach-Object {
    [pscustomobject]@{ Path = $_; Exists = Test-Path -LiteralPath $_ }
}
```

After manual cleanup, all six should report `Exists=False`. This command does not delete, build, run qualification or alter Git. Cleanup does not resolve Stage-5's physical failure or authorize Stage 6.
