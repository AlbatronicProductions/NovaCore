# Reviewed cleanup and current dependencies

No deletion was attempted. Previous policy-blocked deletions were not retried. This restart created only `E:\NovaCore\build\srv01-stage5-stock`: **1,510 files / 327,237,644 bytes**. Counts below are current after evidence consolidation. Exact machine-readable inventory: `cleanup-inventory.json`.

| Exact path | Files | Bytes | Current disposition |
|---|---:|---:|---|
| `E:\NovaCore\build\srv01-display-gap-population` | 400 | 106,016,279 | DISPOSE: prior results/reproducer retained; backup source preservation independently checked by hash |
| `E:\NovaCore\build\srv01-startup-first-present` | 386 | 103,968,588 | DISPOSE: retained first-present evidence and reproduction |
| `E:\NovaCore\build\srv01-stage2-manual` | 371 | 103,439,190 | DISPOSE: manual decision/results and exact predecessor inputs retained |
| `E:\NovaCore\build\srv01-stage3` | 1,867 | 388,109,071 | KEEP NOW: current build uses native-debug/native-release DLLs and shaders |
| `E:\NovaCore\build\srv01-stage4` | 1,536 | 328,974,343 | DISPOSE: 35/35 qualified source inputs resolve to current files or retained archives; measurements/reproducer retained |
| `E:\NovaCore\build\srv01-stage5` | 193 | 33,692,955 | KEEP NOW: previous failed population's exact binaries are still direct inputs of retained causal probe |
| `E:\NovaCore\build\srv01-stage5-closure` | 53 | 9,556,239 | DISPOSE: exact probe source/project match retained reproduction copies; decisive original observations/results retained |
| `E:\NovaCore\build\srv01-stage5-stock` | 1,510 | 327,237,644 | KEEP NOW: current stopped executable, logs and prepared manual route for Project Control |
| **Total remaining** | **6,316** | **1,400,994,309** | **Removed: 0 files / 0 bytes** |

The five released roots total **2,746 files / 651,954,639 bytes**. This is not authority to delete source, evidence, assets, KSA, Blender, legal review or any other root. The three KEEP NOW roots are excluded from the command. Source checkpoint preservation and duplicate diagnostic source witnesses are recorded in `final-verification.json`.

One exact reviewed manual PowerShell cleanup command for only the five released roots; absent paths are skipped:

```powershell
$reviewedPaths = @(
    'E:\NovaCore\build\srv01-display-gap-population'
    'E:\NovaCore\build\srv01-startup-first-present'
    'E:\NovaCore\build\srv01-stage2-manual'
    'E:\NovaCore\build\srv01-stage4'
    'E:\NovaCore\build\srv01-stage5-closure'
)
foreach ($reviewedPath in $reviewedPaths) {
    if (Test-Path -LiteralPath $reviewedPath) {
        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop
    }
}
```

Non-destructive verification of all reviewed roots, before or after manual cleanup:

```powershell
'E:\NovaCore\build\srv01-display-gap-population',
'E:\NovaCore\build\srv01-startup-first-present',
'E:\NovaCore\build\srv01-stage2-manual',
'E:\NovaCore\build\srv01-stage3',
'E:\NovaCore\build\srv01-stage4',
'E:\NovaCore\build\srv01-stage5',
'E:\NovaCore\build\srv01-stage5-closure',
'E:\NovaCore\build\srv01-stage5-stock' | ForEach-Object {
    $files = @(if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -File -Recurse -Force })
    [pscustomobject]@{ Path=$_; Exists=Test-Path -LiteralPath $_; Files=$files.Count; Bytes=($files | Measure-Object Length -Sum).Sum }
}
```

After that cleanup the five released roots should be absent; Stage3, old Stage5 and current stock output should still exist. A future decision can release those remaining rebuildable dependencies after the stopped qualification/reproduction need is consumed. Do not silently broaden this command.
