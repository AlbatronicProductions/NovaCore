# Evidence harness only. Never overwrites retained measurements.
# Run from PowerShell 7; repository dependency targets retain all integrity checks.
$ErrorActionPreference = 'Stop'
$repo = 'E:\NovaCore'
$evidence = Join-Path $repo 'docs\engineering-evidence\powered-contact-ordinary-step-event-closure'
$scratch = Join-Path $repo 'build\powered-contact-ordinary-step-event-closure'
$results = Join-Path $scratch 'reproduction'
if (Test-Path -LiteralPath $results) { throw 'Prior reproduction exists; review before another run' }
Set-Location -LiteralPath $repo
$inputData = Get-Content -Raw -LiteralPath (Join-Path $evidence 'inputs.json') | ConvertFrom-Json -AsHashtable
if ($inputData.plan_sha256 -ne (Get-FileHash -LiteralPath (Join-Path $evidence 'plan.md')).Hash) { throw 'Plan mismatch' }
$prior = Join-Path $repo 'docs\engineering-evidence\powered-contact-chronological-event-feasibility\chronology.py'
if ($inputData.prior_source_sha256 -ne (Get-FileHash -LiteralPath $prior).Hash) { throw 'Reference source mismatch' }
dotnet build (Join-Path $evidence 'OrdinaryClosure.csproj') -c Release "-p:BaseIntermediateOutputPath=$scratch/obj/" "-p:OutputPath=$scratch/bin/" -p:ContinuousIntegrationBuild=true --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
New-Item -ItemType Directory -Path $results | Out-Null
$cases = @('centered-baseline','low-thrust','sliding-baseline','near-unloading','yaw-baseline','off-com','near-end','mid-event','tiny-event','zero-event')
$dll = Join-Path $scratch 'bin\OrdinaryClosure.dll'
foreach ($case in $cases) {
    $output = Join-Path $results "$case-8.json"
    dotnet $dll (Join-Path $evidence 'inputs.json') $case $output 8
    if ($LASTEXITCODE -ne 0) { throw "STOP: $case failed; no automatic convergence or retry" }
    python -B (Join-Path $evidence 'check-support.py') $output
    if ($LASTEXITCODE -ne 0) { throw "STOP: $case source/support failure" }
}
foreach ($repeat in 1..2) {
    $output = Join-Path $results "off-com-repeat$repeat.json"
    dotnet $dll (Join-Path $evidence 'inputs.json') off-com $output 8
    if ($LASTEXITCODE -ne 0) { throw 'STOP: repeat failed' }
    python -B (Join-Path $evidence 'check-support.py') $output
    if ($LASTEXITCODE -ne 0) { throw 'STOP: repeat support failed' }
    if ((Get-FileHash -LiteralPath $output).Hash -ne (Get-FileHash -LiteralPath (Join-Path $results 'off-com-8.json')).Hash) { throw 'Repeat differs' }
}
Write-Output 'Observed physical runs complete. Work UNQUALIFIED; no timing/full PASS.'
Write-Output "New disposable results: $results. Inventory before reviewed cleanup."
