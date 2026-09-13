# Requires PowerShell 7 and the recorded source snapshot.
# Runs one explicitly selected correctness route; never allocation/performance qualification.
[CmdletBinding()]
param(
    [ValidateSet('selector','rest','tilted','regressions')]
    [string]$Case = 'selector',
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'precision-identity.json') -Raw | ConvertFrom-Json
Push-Location -LiteralPath $repo
try {
    $head = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $head -ne $identity.baseline) {
        throw 'Baseline changed; review snapshot before reproduction.'
    }
    foreach ($item in @($identity.changed) + @($identity.protected)) {
        if ((Get-FileHash -LiteralPath $item.path -Algorithm SHA256).Hash -ne $item.sha256) {
            throw "Input changed: $($item.path)"
        }
    }
    $listed = @(git ls-files --cached --others --exclude-standard -- src tests external/bepu)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate candidate inputs.' }
    $paths = @($listed | Sort-Object -Unique)
    if ($paths.Count -ne $identity.current_count) { throw 'Candidate input count changed.' }
    $rows = ($paths | ForEach-Object {
        $_ + ' ' + (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash
    }) -join [char]10
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($rows)))
    if ($hash -ne $identity.current_tree_sha256) {
        throw 'Candidate input tree changed; review before reproduction.'
    }
    $sdk = dotnet --version
    if ($LASTEXITCODE -ne 0 -or $sdk -ne $identity.sdk) { throw 'Recorded SDK is required.' }
    Write-Output "Snapshot identity PASS: $hash"
    Write-Output "Recorded qualification environment: $($identity.platform), SDK $($identity.sdk), runtime $($identity.runtime)."
    if ($VerifyOnly) { return }

    # Check the runtime selected by normal net10.0 patch roll-forward. Do not alter runtime policy.
    $runtimeList = @(dotnet --list-runtimes)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate runtimes.' }
    $versions = @($runtimeList | ForEach-Object {
        if ($_ -match '^Microsoft\.NETCore\.App (10\.0\.\d+) ') { [version]$Matches[1] }
    } | Sort-Object)
    if ($versions.Count -eq 0 -or $versions[-1].ToString() -ne $identity.runtime) {
        throw 'Recorded net10.0 runtime is required; do not silently compare another runtime.'
    }

    dotnet build tests/NovaCore.Simulation.Tests -c Debug --no-restore -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Focused Debug build failed.' }
    $route = @{
        selector = '--compound-selector-only'
        rest = '--compound-coverage-rest-witness'
        tilted = '--compound-coverage-tilted'
        regressions = '--compound-coverage-regressions'
    }[$Case]
    dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll $route
    if ($LASTEXITCODE -ne 0) { throw "Correctness gate failed ($route); stop without retries." }
} finally {
    Pop-Location
}
