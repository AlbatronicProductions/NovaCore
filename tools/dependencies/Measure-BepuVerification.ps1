# Bounded build-only comparison. The no-verification control exists only in scratch.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$QualifiedCheckout)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $QualifiedCheckout).Path
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$source.StartsWith((Join-Path $repo 'build/bepu-binary-validation/'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Use a disposable qualification checkout' }
$parent = Split-Path $source
$control = Join-Path $parent 'timing-control'
if (Test-Path -LiteralPath $control) { throw 'Refusing to overwrite timing control' }
New-Item -ItemType Directory -Path $control | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
    $relative = [IO.Path]::GetRelativePath($source,$file.FullName)
    if ($relative -match '(^|[\\/])(bin|obj)([\\/]|$)') { continue }
    $destination = Join-Path $control $relative
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
$targets = Join-Path $control 'external/bepu/Bepu.targets'
[xml]$xml = Get-Content -Raw -LiteralPath $targets
foreach ($node in @($xml.Project.Target | Where-Object { $_.Name.StartsWith('Verify') })) { [void]$xml.Project.RemoveChild($node) }
$xml.Save($targets)
$project = 'tests/NovaCore.BepuDependency.Tests/NovaCore.BepuDependency.Tests.csproj'
$rows = [Collections.Generic.List[object]]::new()
$oldLocation = Get-Location
function Measure-Build([string]$arm,[string]$mode,[int]$iteration,[string]$root) {
    Set-Location -LiteralPath $root
    $arguments = @('build',$project,'-c','Release','--nologo','-v:quiet')
    if ($mode -eq 'rebuild') { $arguments += '-t:Rebuild' }
    if ($mode -eq 'verify-dll') { $arguments = @('msbuild',$project,'-t:VerifyBepuBinaries','-nologo','-clp:PerformanceSummary') }
    if ($mode -eq 'verify-package') { $arguments = @('msbuild',$project,'-t:VerifyBepuPackages','-nologo','-clp:PerformanceSummary') }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $output = (& dotnet @arguments 2>&1 | Out-String)
    $code = $LASTEXITCODE; $watch.Stop()
    if ($code -ne 0) { throw $output }
    $taskTime = @($output -split '\r?\n' | Where-Object { $_ -match 'VerifyFileHash' })
    $rows.Add([ordered]@{arm=$arm;mode=$mode;iteration=$iteration;seconds=$watch.Elapsed.TotalSeconds;hashTask=$taskTime})
}
try {
    Measure-Build 'control' 'warm' 0 $control
    Measure-Build 'verified' 'warm' 0 $source
    foreach ($mode in @('incremental','rebuild')) {
        foreach ($iteration in 1..3) {
            # Reverse the order on the middle pair to reduce systematic ordering bias.
            if ($iteration -eq 2) { Measure-Build 'verified' $mode $iteration $source; Measure-Build 'control' $mode $iteration $control }
            else { Measure-Build 'control' $mode $iteration $control; Measure-Build 'verified' $mode $iteration $source }
        }
    }
    foreach ($iteration in 1..3) { Measure-Build 'verified' 'verify-dll' $iteration $source; Measure-Build 'verified' 'verify-package' $iteration $source }
} finally {
    Set-Location -LiteralPath $oldLocation
    $rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $parent 'build-overhead.json')
}
$rows | ForEach-Object { [pscustomobject]$_ } | Format-Table arm,mode,iteration,seconds
