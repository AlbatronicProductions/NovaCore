$ErrorActionPreference='Stop'
Set-Location (Resolve-Path "$PSScriptRoot/../../..")
$runRoot=Join-Path (Get-Location) 'build/scalable-launch-support'
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
& python "$PSScriptRoot/capture-candidate.py" before-build
if($LASTEXITCODE -ne 0){throw 'Build input capture failed'}
& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$runs=@()
function Invoke-Build([string]$name,[string]$program,[string[]]$arguments){
    $log=Join-Path $runRoot ($name+'.log')
    $timer=[Diagnostics.Stopwatch]::StartNew()
    & $program @arguments *> $log
    $code=$LASTEXITCODE
    $script:runs+=@{name=$name;program=$program;arguments=$arguments;exit=$code;seconds=$timer.Elapsed.TotalSeconds;logSha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant()}
    $script:runs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runRoot 'build-results.json')
    Write-Output "$name exit=$code"
    if($code -ne 0){Get-Content -LiteralPath $log -Tail 35;throw "$name failed"}
}
foreach($configuration in @('Debug','Release')){
    $suffix=if($configuration -eq 'Release'){'-release'}else{''}
    Invoke-Build "configure-$configuration" 'C:/Program Files/CMake/bin/cmake.exe' @('-S','native/NovaCore.Native','-B',"build/native-ninja$suffix",'-G','Ninja',"-DCMAKE_BUILD_TYPE=$configuration")
    Invoke-Build "native-$configuration" 'C:/Program Files/CMake/bin/cmake.exe' @('--build',"build/native-ninja$suffix",'--clean-first','-j8')
    Invoke-Build "clean-$configuration" 'dotnet' @('clean','NovaCore.sln','-c',$configuration,'-v','quiet')
    Invoke-Build "managed-$configuration" 'dotnet' @('build','NovaCore.sln','-c',$configuration,'--no-incremental','-v','quiet')
}
Invoke-Build 'package' 'python' @('tools/verify-player-package.py','--output','build/scalable-launch-support/canonical-package.json')
& python "$PSScriptRoot/capture-candidate.py" after-build
if($LASTEXITCODE -ne 0){throw 'Candidate continuity capture failed'}

