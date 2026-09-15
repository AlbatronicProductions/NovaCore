# Reviewed disposable cleanup

New root: E:\NovaCore\build\powered-contact-performance-revision.
Resolved absolute root verified; no reparse points. Original source preimages are no longer
needed after exact restoration; reproducer and fingerprints remain. Raw attribution is copied
verbatim to concise retained evidence. Build artifacts are reproducible.

| Exact disposable path | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\build\powered-contact-performance-revision\probe-artifacts\bin |40|15,032,416|
| E:\NovaCore\build\powered-contact-performance-revision\probe-artifacts\obj |82|7,117,220|
| E:\NovaCore\build\powered-contact-performance-revision\originals.json |1|733,243|
| E:\NovaCore\build\powered-contact-performance-revision\attribution.json |1|10,929|
| New total |124|22,893,808|

The prior report counted225 files /44,429,961 bytes under
E:\NovaCore\build\powered-contact-production-integration. Current final filesystem inspection
finds that prior root ABSENT,0 files/0 bytes. No deletion of that root was executed or retried
in this turn; this report does not attribute its removal. Current remaining scratch is the
new124 files /22,893,808 bytes only.

Two temporary diagnostic source files were removed during exact source restoration:

| Exact removed diagnostic source | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\src\NovaCore.Simulation\PoweredContactCostProbe.cs |1|1,348|
| E:\NovaCore\tests\NovaCore.Simulation.Tests\PoweredContactTests.CostProbe.cs |1|3,605|
| Removed total |2|4,953|

Manual cleanup (new reviewed root only):

```powershell
$target = 'E:\NovaCore\build\powered-contact-performance-revision'
if (Test-Path -LiteralPath $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}
```

Non-destructive verification:

```powershell
$reviewed = @(
    'E:\NovaCore\build\powered-contact-performance-revision',
    'E:\NovaCore\build\powered-contact-production-integration'
)
foreach ($target in $reviewed) {
    $exists = Test-Path -LiteralPath $target
    $files = @()
    if ($exists) { $files = @(Get-ChildItem -LiteralPath $target -Recurse -File -Force) }
    [pscustomobject]@{
        Path = $target
        Exists = $exists
        Files = $files.Count
        Bytes = [long](($files | Measure-Object Length -Sum).Sum)
    }
}
```

Automatic approval review rejected one attempt to delete the NEW root before execution with
"blocked by policy". No retry/workaround.124 files remain. The old root was not targeted.
Actual cleanup outcome is recorded in verification.json and the final report.
