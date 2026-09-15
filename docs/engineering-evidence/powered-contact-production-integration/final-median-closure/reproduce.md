# Reproduction, not permission for another run in this ticket

Use the corrected unbanked candidate hashes in identity.json, SDK10.0.303/runtime10.0.12,
the recorded machine/runtime environment and qualified BEPU dependency bytes. This ticket
has stopped. Commands below retain reproducibility for a separately authorized review.

## Corrected candidate validation/control

Build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj with `-c Debug` and
`-c Release`, using an isolated `--artifacts-path`. The exact executed focused routes are in
../lifecycle-cost-convergence/validation.md. The dedicated control route is
`--powered-contact-control-cost`; it contains the same prime128, warm128 and1024 complete
operations and unchanged .050/.100/.250/.500 ms ceilings. Do not replace it with the broader
cost route or call its OFF still-fuelled population dry/exhausted.

No source change or second control run is required to reproduce the reported first failure.
No frequency, tiering, PGO, no-GC timing or profiler override was introduced.

## Final diagnostic

probe.py minimally adapts the retained ../performance-revision/instrument.py. It uses the
current live schedule mapper; J/K correctly have zero warm calls in the OFF control. Its
three added labels distinguish engine reads, resource preparation and resource reads.
Per-operation unions and edge arrays are reduced to summary JSON, not retained raw dumps.
The script refuses to overwrite an existing originals.json. Nine original files are stored
as exact bytes; two temporary source files are removed during restore.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
$probe = 'docs\engineering-evidence\powered-contact-production-integration\final-median-closure\probe.py'
$out = 'E:\NovaCore\build\powered-contact-final-median-closure'
python $probe apply
if ($LASTEXITCODE -ne 0) { throw 'Probe preparation failed; do not run' }
try {
    dotnet build 'tests\NovaCore.Simulation.Tests\NovaCore.Simulation.Tests.csproj' -c Release --artifacts-path "$out\artifacts" --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostic build failed; stop' }
    dotnet "$out\artifacts\bin\NovaCore.Simulation.Tests\release\NovaCore.Simulation.Tests.dll" --powered-contact-final-median-probe > "$out\attribution.json"
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostic failed; no retry' }
} finally {
    python $probe restore
    if ($LASTEXITCODE -ne 0) { throw 'Exact restoration failed' }
}
```

Exactly one fresh diagnostic process. Output must identify the correct population, thread,
zero allocation/collections,1152 exact history/endpoint/native matches and exact partition
reconciliation. Restore and rehash before interpretation. Raw stdout/builds are disposable
once decisive JSON and identity are retained.

## First correction provenance

../lifecycle-cost-convergence/correction.patch is the actual production/test correction
relative to eight stopped preimages plus one new test file. It is a zero-context,
byte-line-ending-preserving reproduction delta, not a temporary instrumentation patch.
preimages.json records those hashes, sizes and text encoding. A non-destructive reverse
applicability check on the corrected candidate passed:

```powershell
git -c core.autocrlf=false apply --reverse --check --unidiff-zero 'docs/engineering-evidence/powered-contact-production-integration/lifecycle-cost-convergence/correction.patch'
```

Use a separate disposable checkout and explicit authorization before applying it in either
direction. Do not reverse the accepted ready correction in the canonical worktree.
The older performance-revision report and instrument.py preserve the original comparison.

Current KSA exact installation/member identities, method-only decompiler invocation and
actual live-changelog URLs are in ../lifecycle-cost-convergence/ksa-lifecycle.md.
Cleanup commands and exact path inventory are in cleanup.md.