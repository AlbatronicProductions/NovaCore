param([switch]$SkipFullSimulation)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
Set-Location -LiteralPath $taskRoot
if ((git rev-parse HEAD) -ne 'c70ccfacf3607d1342396c702682781b42bb525e') { throw 'Review baseline identity before reproducing this unbanked candidate.' }
$taskIdentity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
foreach ($taskFile in $taskIdentity.candidate) {
    if ((Get-FileHash -LiteralPath $taskFile.path -Algorithm SHA256).Hash -ne $taskFile.sha256) { throw "Candidate differs: $($taskFile.path)" }
}
function Invoke-Gate([string[]]$DotnetArguments) {
    & dotnet @DotnetArguments
    if ($LASTEXITCODE -ne 0) { throw "Gate failed; stop without retry: $DotnetArguments" }
}
# One declared attempt per gate, no profiler, runtime override or measurement changes.
foreach ($taskConfiguration in @('Debug','Release')) {
    Invoke-Gate @('build','NovaCore.sln','-c',$taskConfiguration,'--nologo')
}
foreach ($taskConfiguration in @('Debug','Release')) {
    foreach ($taskGate in @('--engine-preparation-cheap','--engine-preparation-allocation','--engine-preparation-article',
        '--spacecraft-command-cheap','--spacecraft-command-schedules','--spacecraft-command-allocation')) {
        Invoke-Gate @('run','--project','tests/NovaCore.Simulation.Tests','-c',$taskConfiguration,'--no-build','--',$taskGate)
    }
}
if (-not $SkipFullSimulation) {
    foreach ($taskConfiguration in @('Debug','Release')) {
        Invoke-Gate @('run','--project','tests/NovaCore.Simulation.Tests','-c',$taskConfiguration,'--no-build')
    }
}
foreach ($taskConfiguration in @('Debug','Release')) {
    foreach ($taskProject in @('ReferenceFrames','Precision','BepuDependency')) {
        Invoke-Gate @('run','--project',"tests/NovaCore.$taskProject.Tests",'-c',$taskConfiguration,'--no-build')
    }
}
Invoke-Gate @('run','--project','tests/NovaCore.Simulation.Tests','-c','Release','--no-build','--','--engine-preparation-cost')
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Diff whitespace check failed.' }
# Stop for Project Control. This script never stages, commits, tags, pushes or cleans files.
