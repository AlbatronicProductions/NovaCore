param([string]$AssetCache = $env:NOVACORE_ASSET_CACHE)

$ErrorActionPreference = 'Stop'
$correctionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$previousLocation = Get-Location
$previousCache = $env:NOVACORE_ASSET_CACHE

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    Write-Host "$Executable $($Arguments -join ' ')"
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable exited with $LASTEXITCODE" }
}

function Ensure-Junction([string]$Link, [string]$Target) {
    if (Test-Path -LiteralPath $Link) {
        $item = Get-Item -LiteralPath $Link
        if ($item.LinkType -ne 'Junction' -or $item.Target -ne $Target) {
            throw "Existing test-root path is not the required junction: $Link"
        }
    } else {
        New-Item -ItemType Junction -Path $Link -Target $Target | Out-Null
    }
}

try {
    Set-Location -LiteralPath $correctionRoot
    if ($AssetCache) { $env:NOVACORE_ASSET_CACHE = $AssetCache }
    $identity = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'validation.json') | ConvertFrom-Json
    foreach ($source in $identity.changedSourceFiles) {
        $text = [IO.File]::ReadAllText((Join-Path $correctionRoot $source.path)).Replace("`r`n", "`n")
        $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
        if ($hash -ne $source.sha256LF) { throw "Corrected source identity mismatch: $($source.path)" }
    }
    $accepted = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'accepted-source.json') | ConvertFrom-Json
    $acceptedBytes = [Text.Encoding]::UTF8.GetBytes($accepted.sourceUtf8)
    if ($acceptedBytes.Length -ne $accepted.bytes -or
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($acceptedBytes)) -ne $accepted.sha256) {
        throw 'Recovered accepted source identity mismatch'
    }

    # Native prerequisites must first be built from this checkout; see docs/build-windows.md.
    Invoke-Checked dotnet @('build', 'NovaCore.sln', '-c', 'Debug', '-v:q')
    Invoke-Checked dotnet @('build', 'NovaCore.sln', '-c', 'Release', '--no-restore', '-v:q')

    $graphicsRoot = $correctionRoot
    if (-not (Test-Path -LiteralPath (Join-Path $correctionRoot '.git') -PathType Container)) {
        $graphicsRoot = "$correctionRoot-graphics-test-root"
        New-Item -ItemType Directory -Path $graphicsRoot -Force | Out-Null
        Invoke-Checked git @('init', '--quiet', $graphicsRoot)
        Ensure-Junction (Join-Path $graphicsRoot 'assets') (Join-Path $correctionRoot 'assets')
        Ensure-Junction (Join-Path $graphicsRoot 'build') (Join-Path $correctionRoot 'build')
    }
    foreach ($configuration in @('Debug', 'Release')) {
        Set-Location -LiteralPath $correctionRoot
        $simulation = Join-Path $correctionRoot "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
        foreach ($selector in @('--persistent-contact-only', '--contact-servicing-cheap', '--contact-servicing-only')) {
            Invoke-Checked dotnet @($simulation, $selector)
        }
        Invoke-Checked dotnet @($simulation)
        Set-Location -LiteralPath $graphicsRoot
        $graphics = Join-Path $correctionRoot "tests/NovaCore.Graphics.Tests/bin/$configuration/net10.0/NovaCore.Graphics.Tests.dll"
        foreach ($case in @('Contact development canonical presentation', 'Certified Florida continuation publication',
            'Certified continuation acceptance gaps', 'Certified continuation allocation matrix')) {
            Invoke-Checked dotnet @($graphics, "--case=$case")
        }
    }
    Set-Location -LiteralPath $correctionRoot
    $simulation = Join-Path $correctionRoot 'tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll'
    Invoke-Checked dotnet @($simulation, '--persistent-contact-performance')
    Invoke-Checked dotnet @($simulation, '--contact-servicing-performance')
    Invoke-Checked git @('diff', '--check')
} finally {
    Set-Location -LiteralPath $previousLocation
    $env:NOVACORE_ASSET_CACHE = $previousCache
}
