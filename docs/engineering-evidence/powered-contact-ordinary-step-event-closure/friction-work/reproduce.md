# Reproduction, bounded and read-only first

PowerShell 7, repository `E:\NovaCore`, SDK 10.0.303, runtime 10.0.12 X64.
No production/test edits. Existing original files and qualified dependencies
are required. Do not run the preceding ten-row reproduction campaign for this
revision. Do not run performance.

## Verify the retained proof without a world

The old native JSON is sufficient because every value matched the observed
run except the DLL directory. The compact traces contain exact native bits.
These commands print recalculated work and checks without writing output:

```powershell
$ev = 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work'
$prior = Split-Path $ev
python -B (Join-Path $ev 'check-work.py') (Join-Path $prior 'off-com-8.json') (Join-Path $ev 'off-com-trace.json')
if ($LASTEXITCODE -ne 0) { throw 'Off-COM evidence check failed' }
python -B (Join-Path $ev 'check-work.py') (Join-Path $prior 'yaw-baseline-8.json') (Join-Path $ev 'yaw-baseline-trace.json')
if ($LASTEXITCODE -ne 0) { throw 'Yaw evidence check failed' }
```

`sourceRawSha256` printed by this offline route identifies the ORIGINAL JSON
used, so it differs from the diagnostic-path hash in measurements.json.
Work, trace identity and proof checks are identical. This does not rewrite
old results, execute chronology.py/execute(), or run BEPU.

## Reproduce the diagnostic only when expressly needed later

`prepare-observer.ps1` fetches one exact pinned upstream file, verifies its
hash, and generates an evidence-only processor and a copy of the original
harness in the unique scratch directory. It asserts that removing recorder
calls and reversing names recovers the original arithmetic source exactly.
It does not change either BEPU DLL or any production/permanent-test file.
The generated source fingerprints are in identity.json. It refuses to
overwrite an existing scratch directory.

```powershell
$ev = 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work'
$prior = Split-Path $ev
$scratch = 'E:\NovaCore\build\powered-contact-friction-work'
& (Join-Path $ev 'prepare-observer.ps1')
dotnet build (Join-Path $scratch 'WorkProbe.csproj') -c Release -p:ContinuousIntegrationBuild=true --nologo
if ($LASTEXITCODE -ne 0) { throw 'STOP: build failure' }
$dll = Join-Path $scratch 'bin\Release\net10.0\WorkProbe.dll'
foreach ($name in @('off-com','yaw-baseline')) {
    $raw = Join-Path $scratch "$name.json"
    dotnet $dll (Join-Path $prior 'inputs.json') $name $raw 8
    if ($LASTEXITCODE -ne 0) { throw 'STOP: diagnostic/native failure' }
    python -B (Join-Path $ev 'check-work.py') $raw "$raw.trace.json"
    if ($LASTEXITCODE -ne 0) { throw 'STOP: identity or work failure; no next witness' }
}
```

No retry or new witness is implicit. Record paths/counts/bytes before any
cleanup; use [cleanup](cleanup.md), after retaining useful results.

## Current-source reproduction

Rehash actual `E:\Kitten Space Agency` files before repeating KSA claims.
Existing reader used:
`C:\Users\Tyler\AppData\Local\Temp\ilspycmd-11.0.0.9375\tools\net10.0\any\ilspycmd.dll`.
Use bounded single-member `-m 0xTOKEN` decoding with the current assembly,
and PEReader/MetadataReader to resolve current IL bytes/operands. Token/IL
anchors and hashes are in [KSA ownership](ksa-current-work-ownership.md).
In this tool version, combining `-il -m` unexpectedly ignored member scope;
that attempted output was stopped and no dump was retained. Do not repeat
the broad form. No KSA file modification is needed.

Inspect the actual live Discord messages linked there. The stalled focused
search was not a completed archive search. Relevant available history and
current installed implementation must stay separate.
