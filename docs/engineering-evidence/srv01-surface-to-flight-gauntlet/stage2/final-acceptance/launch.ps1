param([switch]$PrepareOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch = Join-Path $repo 'build/srv01-stage2-manual'
$exe = Join-Path $scratch 'artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
$seal = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../startup-first-present-correlation/identity.json') -Raw | ConvertFrom-Json
foreach ($item in $seal.sourceSeals) {
    if ((Get-FileHash -LiteralPath (Join-Path $repo $item.path)).Hash -ne $item.sha256) { throw "Candidate source mismatch: $($item.path)" }
}
if ((Get-FileHash -LiteralPath (Join-Path $repo 'native/NovaCore.Native/NovaCoreNative.cpp')).Hash -ne $seal.nativeSha256) { throw 'Native source mismatch' }
function Gate { if ($LASTEXITCODE -ne 0) { throw "Preparation failed: $LASTEXITCODE" } }
Push-Location $repo
try {
    if (!(Test-Path -LiteralPath $exe)) {
        New-Item -ItemType Directory -Force -Path $scratch | Out-Null
        & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation *> (Join-Path $scratch 'native-env.log')
        $env:VULKAN_SDK = 'C:/VulkanSDK/1.4.357.0'
        cmake -S native/NovaCore.Native -B (Join-Path $scratch 'native-release') -G Ninja -DCMAKE_BUILD_TYPE=Release *> (Join-Path $scratch 'native-configure.log'); Gate
        cmake --build (Join-Path $scratch 'native-release') *> (Join-Path $scratch 'native-build.log'); Gate
        dotnet build samples/NovaCore.Triangle -c Release --artifacts-path (Join-Path $scratch 'artifacts') '-p:NativeBuildDirectory=srv01-stage2-manual/native-release' -p:ContinuousIntegrationBuild=true --nologo -v:q *> (Join-Path $scratch 'managed-build.log'); Gate
        $binaries = @('NovaCore.Triangle.exe','NovaCore.Triangle.dll','NovaCore.Native.dll','NovaCore.Simulation.dll','NovaCore.Graphics.dll') | ForEach-Object {
            [ordered]@{ file = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path (Split-Path $exe) $_)).Hash }
        }
        [ordered]@{ prepared = (Get-Date).ToString('o'); sourceSeals = $seal.sourceSeals; nativeSha256 = $seal.nativeSha256; binaries = @($binaries); instrumentation = 'NONE'; build = 'Release native/sample PASS' } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manual-build.json') -Encoding utf8
    }
    $build = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manual-build.json') -Raw | ConvertFrom-Json
    foreach ($binary in $build.binaries) {
        if ((Get-FileHash -LiteralPath (Join-Path (Split-Path $exe) $binary.file)).Hash -ne $binary.sha256) { throw "Prepared binary mismatch: $($binary.file)" }
    }
    if ($PrepareOnly) { Write-Output 'Uninstrumented Release powered-supported route prepared; no qualification rerun.'; return }
    Write-Output 'Known residual: a white/unpresented startup window may last hundreds of milliseconds or longer (one targeted run: 16.67 seconds). This is not fixed. Wait for READY before Space.'
    $attempt = 1
    while (Test-Path -LiteralPath (Join-Path $scratch "manual-$attempt.log")) { $attempt++ }
    & $exe '--scene=srv01-powered-support' '--log=renderer,vulkan' *> (Join-Path $scratch "manual-$attempt.log")
    Gate
    Write-Output "Window closed. Manual log: $scratch\manual-$attempt.log"
}
finally { Pop-Location }
