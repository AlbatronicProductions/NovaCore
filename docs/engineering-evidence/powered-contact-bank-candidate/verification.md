# Attribution-only successor verification

This document records the completed attribution-only qualification stage. The
later authorized whitespace-only successor is documented in whitespace-closure.md
and whitespace-result.json. Its normalized bytes require reinsertion of recorded
removed whitespace before the original reverse-attribution hash check. Earlier
build/test results below remain historical; no full suite or performance rerun is
implied by the second migration.

Correction authorized by Project Control ticket `838eaf45-90ad-4359-8834-dc9bf7fccde8`.

| Gate | Result |
| --- | --- |
| Before correction | 26/26 qualified source/test/sample hashes matched |
| Exact edit | Three unique MIT-to-Apache-2.0 attribution replacements; reversing each replacement recovered the complete original file string |
| Observer generation | Pinned upstream hash PASS; native arithmetic source recovery PASS; 12 row sites; regenerated successor byte hash identical |
| Debug test-project build | PASS, 0 warnings/errors |
| Release test-project build | PASS, 0 warnings/errors |
| Debug focused groups | cheap, physics, work, authority, lifecycle, sequences, allocation: 7/7 PASS |
| Release focused groups | Same seven: 7/7 PASS |
| Native expectations | Six mapper bits, exact work telescoping/native bits and lifetime projection equivalence PASS |
| Allocation | Five windows exactly 0 B, entry/exit PASS; positive control 152 B in both configurations |
| Physics/determinism | Existing bounds, 1,200-endpoint partitions and 600/600 support unchanged/PASS |
| Identity after correction | 25 historical hashes unchanged; observer successor explicitly migrated; production unchanged |
| Current attribution scan | No incorrect current BEPU MIT label; original stop text retained as historical |

No numerical expressions, using directives, test assertions, acceptance thresholds,
generator transformations, upstream code or production behavior were changed. The
only observer difference is one comment. Exact replacements/hashes are in
`attribution-identity-migration.json`; retained source identities are in
`qualified-source-identity.json`. The accepted full 74/74, performance and manual
qualification were not repeated for a comment-only change.

## Reproduce the bounded correction checks

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
& .\docs\engineering-evidence\powered-contact-production-integration\generate-work-observer.ps1
$out = 'E:\NovaCore\build\powered-contact-attribution-correction\artifacts'
foreach ($configuration in @('Debug','Release')) {
    dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c $configuration --artifacts-path $out --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $configuration" }
    $dll = Join-Path $out ('bin/NovaCore.Simulation.Tests/' + $configuration.ToLowerInvariant() + '/NovaCore.Simulation.Tests.dll')
    foreach ($gate in @('cheap','physics','work','authority','lifecycle','sequences','allocation')) {
        dotnet $dll "--powered-contact-$gate"
        if ($LASTEXITCODE -ne 0) { throw "Failed: $configuration / $gate" }
    }
}
git diff --check
git diff --cached --check
git diff --cached --name-only
```

Run from the exact successor source; generator writes its permanent generated
observer, so confirm its expected hash before/after. No retry-to-green or benchmark
route is part of this correction. Fresh qualification commands may create only
the two scratch paths classified in cleanup.md.

Independent read-only review verified the corrected attribution, reproduction
dependency closure and draft rights/public-claim boundaries. Draft consistency
review is not professional legal approval.

## Final bank-preflight distinction

Working-tree tracked `git diff --check` and the empty-index cached check pass.
They do not inspect untracked candidate additions. A prospective-addition check
against NUL finds inherited whitespace defects; final-review.md records the full
bounded inventory. No claim that the future staged check passes is made. Fixing
those source/evidence bytes is outside this exact attribution-only authorization.
The correction itself remains PASS; complete bank preparation is REVISE.
