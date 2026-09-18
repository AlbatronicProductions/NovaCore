# Reviewed cleanup and reproduction retention

**Deletion stopped.** One automatic attempt to remove two exact duplicate evidence
files was rejected before execution as `blocked by policy`. No retry or workaround.
Previously blocked campaign roots were not retried. No recursive cleanup was attempted
for the new final build. Cleanup does not block engineering acceptance under this ticket.

All build roots were reviewed as contained non-reparse repository outputs with no tracked
content; candidate source/assets/physical datasets are outside them. Historical source
archives and direct restoration-hash checks preserve the unique inputs, including the
failed Stage5 probe dependencies. The final regression is retained before cleanup.
Final source identity:702 inputs; no new physical source.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
| E:\NovaCore\build\srv01-display-gap-population | 400 | 106016279 | DISPOSE |
| E:\NovaCore\build\srv01-startup-first-present | 386 | 103968588 | DISPOSE |
| E:\NovaCore\build\srv01-stage2-manual | 371 | 103439190 | DISPOSE |
| E:\NovaCore\build\srv01-stage3 | 1867 | 388109071 | DISPOSE |
| E:\NovaCore\build\srv01-stage4 | 1536 | 328974343 | DISPOSE |
| E:\NovaCore\build\srv01-stage5 | 193 | 33692955 | DISPOSE |
| E:\NovaCore\build\srv01-stage5-closure | 53 | 9556239 | DISPOSE |
| E:\NovaCore\build\srv01-stage5-stock | 1511 | 327250732 | DISPOSE |
| E:\NovaCore\build\srv01-stage5-final-closure | 488 | 138728068 | DISPOSE |
| E:\NovaCore\build\srv01-stage5-florida-presentation | 1708 | 367055344 | DISPOSE |
| E:\NovaCore\build\srv01-foundation-final | 1781 | 366186221 | DISPOSE |
| E:\NovaCore\build\srv01-stage5-simplified-slab | 0 | 0 | MANUAL-ACCEPTANCE BINARY NO LONGER NEEDED / ALREADY ABSENT |
| E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0 | 0 | 0 | MANUAL-ACCEPTANCE BINARY NO LONGER NEEDED / ALREADY ABSENT |
| E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows | 0 | 0 | MANUAL-ACCEPTANCE BINARY NO LONGER NEEDED / ALREADY ABSENT |
| E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\display-gap-closure\cleanup.json | 1 | 2810 | DISPOSE |
| E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\florida-presentation-closure\current-tag-refs.txt | 1 | 5487 | DISPOSE |

Remaining **10,296 files / 2,272,985,327 bytes**.

Removed during closeout: one intermediate reconstructed text file, **569,437 bytes**, at `E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\recovered-program.cs.txt`. It was byte-verified against its retained historical-stock-test-inputs.zip entry before removal. This is the only actual deletion; no bulk cleanup success is claimed.

The three already-absent prior manual/build paths are not retried. All unrelated historical
build roots remain outside this review. The installed KSA tree, accepted regional terrain,
stock GLBs, current source/tests, legal reviews and Git metadata are untouched.

## Exact reviewed manual PowerShell cleanup

Run only after confirming these review targets still contain this campaign's disposable
outputs. The existence guard avoids retrying absent paths. No wildcard deletion.

~~~powershell
$reviewedPaths = @(
    'E:\NovaCore\build\srv01-display-gap-population',
    'E:\NovaCore\build\srv01-startup-first-present',
    'E:\NovaCore\build\srv01-stage2-manual',
    'E:\NovaCore\build\srv01-stage3',
    'E:\NovaCore\build\srv01-stage4',
    'E:\NovaCore\build\srv01-stage5',
    'E:\NovaCore\build\srv01-stage5-closure',
    'E:\NovaCore\build\srv01-stage5-stock',
    'E:\NovaCore\build\srv01-stage5-final-closure',
    'E:\NovaCore\build\srv01-stage5-florida-presentation',
    'E:\NovaCore\build\srv01-foundation-final',
    'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\display-gap-closure\cleanup.json',
    'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\florida-presentation-closure\current-tag-refs.txt'
)
foreach ($reviewedPath in $reviewedPaths) {
    if (Test-Path -LiteralPath $reviewedPath) {
        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop
    }
}
~~~

Non-destructive verification, using the same explicit $reviewedPaths array above:

~~~powershell
$reviewedPaths | ForEach-Object {
    $files = @(if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -File -Recurse -Force })
    [pscustomobject]@{Path=$_; Exists=Test-Path -LiteralPath $_; Files=$files.Count; Bytes=($files | Measure-Object Length -Sum).Sum}
}
~~~

Expected after cleanup: all Exists=False. Retain the final package and all KEEP entries in evidence-retention.json.

Previously removed intermediate text (no retry requested), exact completed command:

~~~powershell
Remove-Item -LiteralPath 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\recovered-program.cs.txt' -Force -ErrorAction Stop
~~~

Non-destructive verification (expected False):

~~~powershell
Test-Path -LiteralPath 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\recovered-program.cs.txt'
~~~
