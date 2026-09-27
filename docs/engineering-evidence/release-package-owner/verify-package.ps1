param(
    [string]$Repository = 'E:\NovaCore',
    [string]$Package = 'tools\NovaCore.App\bin\Release\net10.0-windows',
    [string]$ReportName = 'canonical',
    [switch]$NativeContentOnly
)
$ErrorActionPreference = 'Stop'
$Repository = (Resolve-Path -LiteralPath $Repository).Path
$packageRoot = (Resolve-Path -LiteralPath (Join-Path $Repository $Package)).Path
$nativeRoot = Join-Path $Repository 'build\native-ninja-release'
$evidence = Join-Path $Repository 'docs\engineering-evidence\release-package-owner'
function Manifest([string]$Root) {
    @(Get-ChildItem -LiteralPath $Root -Recurse -File | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{path=[IO.Path]::GetRelativePath($Root,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
    })
}
$manifest = Manifest $packageRoot
$native = Manifest (Join-Path $nativeRoot 'shaders')
$published = @($manifest | Where-Object path -like 'shaders\*.spv')
$expected = @{}; foreach ($file in $native) { $expected['shaders\'+$file.path] = $file.sha256 }
$actual = @{}; foreach ($file in $manifest) { $actual[$file.path] = $file.sha256 }
$missing = @($expected.Keys | Where-Object { !$actual.ContainsKey($_) } | Sort-Object)
$extra = @($published.path | Where-Object { !$expected.ContainsKey($_) } | Sort-Object)
$mismatch = @($expected.Keys | Where-Object { $actual.ContainsKey($_) -and $actual[$_] -ne $expected[$_] } | Sort-Object)
$nativeHash = (Get-FileHash -LiteralPath (Join-Path $nativeRoot 'NovaCore.Native.dll') -Algorithm SHA256).Hash
$candidate = Get-Content -LiteralPath (Join-Path $evidence 'qualified-candidate.json') -Raw | ConvertFrom-Json
$candidateMap = @{}; foreach ($file in $candidate) { $candidateMap[$file.path] = $file.sha256 }
$missingCandidate = @($candidateMap.Keys | Where-Object { !$actual.ContainsKey($_) } | Sort-Object)
$extraCandidate = @($actual.Keys | Where-Object { !$candidateMap.ContainsKey($_) } | Sort-Object)
$changedCandidate = @($candidateMap.Keys | Where-Object { $actual.ContainsKey($_) -and $candidateMap[$_] -ne $actual[$_] } | Sort-Object)
$contentPaths = @($candidateMap.Keys | Where-Object { $_ -like 'assets\*' -or $_ -like 'wwwroot\*' -or $_ -like 'third-party\*' })
$changedContent = @($contentPaths | Where-Object { !$actual.ContainsKey($_) -or $candidateMap[$_] -ne $actual[$_] } | Sort-Object)
$summary = [ordered]@{
    package=$packageRoot; scope=$(if($NativeContentOnly){'native runtime and shaders'}else{'canonical package parity'}); files=$manifest.Count; nativeShaders=$native.Count; packageShaders=$published.Count
    missingShaders=$missing; extraShaders=$extra; mismatchedShaderHashes=$mismatch
    nativeDllMatches=($actual['NovaCore.Native.dll'] -eq $nativeHash); nativeDllSha256=$nativeHash
    candidateFiles=$candidate.Count; missingCandidatePaths=$missingCandidate; extraCandidatePaths=$extraCandidate
    changedCandidateHashes=$changedCandidate; unchangedAssetWebLicenseFiles=($contentPaths.Count-$changedContent.Count); changedAssetWebLicenseFiles=$changedContent
    executableSha256=$actual['NovaCore.exe']
}
$manifest | ConvertTo-Json | Set-Content (Join-Path $evidence ($ReportName+'-manifest.json'))
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence ($ReportName+'-summary.json'))
$summary | ConvertTo-Json -Depth 5
if ($native.Count -eq 0 -or $missing.Count -or $extra.Count -or $mismatch.Count -or !$summary.nativeDllMatches -or !$actual.ContainsKey('NovaCore.exe')) { throw 'Native package verification failed.' }
if (!$NativeContentOnly -and ($missingCandidate.Count -or $extraCandidate.Count -or $changedContent.Count)) { throw 'Canonical package parity failed.' }
