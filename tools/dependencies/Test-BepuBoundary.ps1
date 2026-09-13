# Isolated dependency qualification. Never changes canonical artifacts or global NuGet state.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$scratch = Join-Path $repo ('build/bepu-binary-validation/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
if (Test-Path -LiteralPath $scratch) { throw 'Refusing to overwrite qualification output' }
New-Item -ItemType Directory -Path $scratch | Out-Null
$tree = Join-Path $scratch 'checkout'
New-Item -ItemType Directory -Path $tree | Out-Null
$records = [Collections.Generic.List[object]]::new()
$oldCache = $env:NUGET_PACKAGES
$oldLocation = Get-Location
function Run-Dotnet([string]$label, [string[]]$arguments, [bool]$failureExpected = $false) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $output = (& dotnet @arguments 2>&1 | Out-String)
    $code = $LASTEXITCODE
    $watch.Stop()
    Set-Content -LiteralPath (Join-Path $scratch ($label + '.txt')) -Value $output
    $witness = @($output -split '\r?\n' | Where-Object { $_ -match 'error MSB395[1234]' } | Select-Object -Unique)
    $records.Add([ordered]@{ name=$label; exit=$code; expectedFailure=$failureExpected; seconds=$watch.Elapsed.TotalSeconds; witness=$witness })
    Write-Output "$label : exit=$code seconds=$($watch.Elapsed.TotalSeconds)"
    if ($failureExpected) {
        if ($code -eq 0 -or $witness.Count -eq 0) { throw "$label did not fail at the artifact identity gate" }
    } elseif ($code -ne 0) { throw "$label failed: $output" }
}
try {
    # Export only this dependency's managed closure; no Git index mutation or bulk terrain/native copy.
    $paths = @('Directory.Build.props', 'src/NovaCore.Core', 'src/NovaCore.EphemerisFormat', 'src/NovaCore.Simulation')
    foreach ($relative in (& git -C $repo ls-files -- @paths)) {
        if ($LASTEXITCODE -ne 0) { throw 'Tracked source inventory failed' }
        $destination = Join-Path $tree $relative
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repo $relative) -Destination $destination
    }
    Copy-Item -LiteralPath (Join-Path $repo 'Directory.Build.targets') -Destination $tree
    New-Item -ItemType Directory -Path (Join-Path $tree 'external') | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo 'external/bepu') -Destination (Join-Path $tree 'external') -Recurse
    $test = 'tests/NovaCore.BepuDependency.Tests'
    New-Item -ItemType Directory -Path (Join-Path $tree $test) -Force | Out-Null
    foreach ($name in @('Program.cs', 'NovaCore.BepuDependency.Tests.csproj')) {
        Copy-Item -LiteralPath (Join-Path $repo "$test/$name") -Destination (Join-Path $tree $test)
    }
    # Empty source + empty isolated cache. Framework packs come from the installed SDK.
    $feed = Join-Path $scratch 'competing-feed'
    New-Item -ItemType Directory -Path $feed | Out-Null
    $env:NUGET_PACKAGES = Join-Path $scratch 'isolated-cache'
    $config = Join-Path $tree 'NuGet.Config'
    Set-Content -LiteralPath $config -Value '<configuration><packageSources><clear /></packageSources></configuration>'
    Set-Location -LiteralPath $tree
    $project = "$test/NovaCore.BepuDependency.Tests.csproj"
    Run-Dotnet 'clean-empty-debug' @('build',$project,'-c','Debug','-p:ContinuousIntegrationBuild=true','--nologo','-v:minimal')
    Run-Dotnet 'empty-debug-load' @('run','--project',$project,'-c','Debug','--no-build')
    $versionRoot = Join-Path $tree 'external/bepu/2.5.0-beta.29'
    # Competing archives and extracted cache DLLs use the same IDs/versions, but different bytes.
    foreach ($id in @('BepuPhysics','BepuUtilities')) {
        $name = "$id.2.5.0-beta.29.nupkg"
        $bytes = [IO.File]::ReadAllBytes((Join-Path $versionRoot "packages/$name"))
        $bytes[100] = $bytes[100] -bxor 1
        [IO.File]::WriteAllBytes((Join-Path $feed $name),$bytes)
        $cache = Join-Path $env:NUGET_PACKAGES ($id.ToLowerInvariant() + '/2.5.0-beta.29')
        New-Item -ItemType Directory -Path (Join-Path $cache 'lib/net8.0') -Force | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $cache $name.ToLowerInvariant()),$bytes)
        $bytes = [IO.File]::ReadAllBytes((Join-Path $versionRoot "lib/$id.dll"))
        $bytes[100] = $bytes[100] -bxor 1
        [IO.File]::WriteAllBytes((Join-Path $cache "lib/net8.0/$id.dll"),$bytes)
        Set-Content -LiteralPath (Join-Path $cache '.nupkg.metadata') -Value '{"version":2,"contentHash":"unapproved"}'
    }
    $escapedFeed = [Security.SecurityElement]::Escape($feed)
    Set-Content -LiteralPath $config -Value "<configuration><packageSources><clear /><add key='competing' value='$escapedFeed' /></packageSources></configuration>"
    Run-Dotnet 'hostile-clean-release' @('build',$project,'-c','Release','-p:ContinuousIntegrationBuild=true','--nologo','-v:minimal','--force')
    Run-Dotnet 'hostile-release-load' @('run','--project',$project,'-c','Release','--no-build')
    Run-Dotnet 'publish' @('publish',$project,'-c','Release','--no-build','--nologo','-o',(Join-Path $scratch 'published'))
    Run-Dotnet 'published-load' @((Join-Path $scratch 'published/NovaCore.BepuDependency.Tests.dll'))
    # Prove an indirect consumer also gets both references/deps entries and licensing.
    $bridge = Join-Path $tree 'tests/Bridge'
    New-Item -ItemType Directory -Path $bridge | Out-Null
    Set-Content -LiteralPath (Join-Path $bridge 'Bridge.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><ProjectReference Include="../../src/NovaCore.Simulation/NovaCore.Simulation.csproj" /></ItemGroup></Project>'
    $projectText = [IO.File]::ReadAllText((Join-Path $tree $project))
    try {
        [IO.File]::WriteAllText((Join-Path $tree $project),$projectText.Replace('../../src/NovaCore.Simulation/NovaCore.Simulation.csproj','../Bridge/Bridge.csproj'))
        Run-Dotnet 'indirect-build' @('build',$project,'-c','Debug','--nologo','-v:minimal')
        Run-Dotnet 'indirect-load' @('run','--project',$project,'-c','Debug','--no-build')
    } finally { [IO.File]::WriteAllText((Join-Path $tree $project),$projectText) }
    $build = @('build',$project,'-c','Release','--no-restore','--nologo','-v:minimal')
    foreach ($relative in @('lib/BepuPhysics.dll','lib/BepuUtilities.dll','manifest/bepu-2.5.0-beta.29.json','packages/BepuPhysics.2.5.0-beta.29.nupkg','packages/BepuUtilities.2.5.0-beta.29.nupkg')) {
        $path = Join-Path $versionRoot $relative
        $original = [IO.File]::ReadAllBytes($path)
        try {
            $modified = [byte[]]$original.Clone(); $modified[100] = $modified[100] -bxor 1
            [IO.File]::WriteAllBytes($path,$modified)
            $arguments = if ($relative.StartsWith('packages/')) { @('msbuild',$project,'-t:VerifyBepuPackages','-nologo','-v:minimal') } else { $build }
            Run-Dotnet ('tamper-' + [IO.Path]::GetFileName($relative)) $arguments $true
            if ($relative -eq 'packages/BepuPhysics.2.5.0-beta.29.nupkg') {
                Run-Dotnet 'ci-archive-tamper' ($build + @('-p:ContinuousIntegrationBuild=true')) $true
            }
        } finally { [IO.File]::WriteAllBytes($path,$original) }
    }
    foreach ($relative in @('lib/BepuPhysics.dll','lib/BepuUtilities.dll','manifest/bepu-2.5.0-beta.29.json','packages/BepuPhysics.2.5.0-beta.29.nupkg')) {
        $path = Join-Path $versionRoot $relative
        # Rename instead of deletion: also tests wrong filename; restore in guaranteed cleanup.
        Move-Item -LiteralPath $path -Destination ($path + '.wrong-name')
        try {
            $arguments = if ($relative.StartsWith('packages/')) { @('msbuild',$project,'-t:VerifyBepuPackages','-nologo','-v:minimal') } else { $build }
            Run-Dotnet ('missing-' + [IO.Path]::GetFileName($relative)) $arguments $true
        } finally { Move-Item -LiteralPath ($path + '.wrong-name') -Destination $path }
    }
    # A valid managed assembly with a wrong assembly version/name is rejected by bytes.
    $path = Join-Path $versionRoot 'lib/BepuPhysics.dll'
    $original = [IO.File]::ReadAllBytes($path)
    try {
        Copy-Item -LiteralPath (Join-Path $tree 'src/NovaCore.Core/bin/Release/net10.0/NovaCore.Core.dll') -Destination $path -Force
        Run-Dotnet 'wrong-managed-assembly-version' $build $true
    } finally { [IO.File]::WriteAllBytes($path,$original) }
    # Publishing resolves pinned references directly; a stale build output must not be consumed.
    $path = Join-Path $tree "$test/bin/Release/net10.0/BepuPhysics.dll"
    $original = [IO.File]::ReadAllBytes($path)
    try {
        $modified = [byte[]]$original.Clone(); $modified[100] = $modified[100] -bxor 1
        [IO.File]::WriteAllBytes($path,$modified)
        Run-Dotnet 'stale-output-publish' @('publish',$project,'-c','Release','--no-build','--nologo','-o',(Join-Path $scratch 'tampered-publish'))
        Run-Dotnet 'stale-output-published-load' @((Join-Path $scratch 'tampered-publish/NovaCore.BepuDependency.Tests.dll'))
    } finally { [IO.File]::WriteAllBytes($path,$original) }
    Run-Dotnet 'restored-provenance' @('msbuild',$project,'-t:VerifyBepuPackages','-nologo','-v:minimal')
    Run-Dotnet 'restored-build' $build
    Run-Dotnet 'restored-load' @('run','--project',$project,'-c','Release','--no-build')
} finally {
    Set-Location -LiteralPath $oldLocation
    $env:NUGET_PACKAGES = $oldCache
    $records | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $scratch 'results.json')
    Write-Output "Evidence: $scratch"
}
