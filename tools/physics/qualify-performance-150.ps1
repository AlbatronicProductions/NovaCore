param([string[]]$Configurations=@('Debug','Release'))
$ErrorActionPreference='Stop'
$rootPath=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $rootPath
$outputPath=Join-Path $rootPath 'build/performance-150fps/qualification'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$results=[System.Collections.Generic.List[object]]::new()
function Invoke-Check([string]$Name,[string[]]$CommandArguments) {
    $logPath=Join-Path $outputPath ($Name+'.log')
    $watch=[Diagnostics.Stopwatch]::StartNew()
    if($CommandArguments[0].EndsWith('.exe')) { & $CommandArguments[0] @($CommandArguments[1..($CommandArguments.Length-1)]) *> $logPath }
    else { & dotnet @CommandArguments *> $logPath }
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{name=$Name;exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;log=$logPath})
    $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outputPath 'results.json') -Encoding utf8
    Write-Output "$Name exit=$code seconds=$($watch.Elapsed.TotalSeconds)"
    if($code -ne 0){Get-Content -LiteralPath $logPath -Tail 30;throw "Qualification failed: $Name"}
}
foreach($configurationName in $Configurations) {
    Invoke-Check "build-$configurationName" @('build','NovaCore.sln','-c',$configurationName,'--no-restore')
    Invoke-Check "simulation-$configurationName" @("tests/NovaCore.Simulation.Tests/bin/$configurationName/net10.0/NovaCore.Simulation.Tests.dll")
    Invoke-Check "response-$configurationName" @("tests/NovaCore.Simulation.Tests/bin/$configurationName/net10.0/NovaCore.Simulation.Tests.dll",'--surface-response')
    Invoke-Check "recorder-build-$configurationName" @('build','tests/NovaCore.MinimumRecorder.Tests/NovaCore.MinimumRecorder.Tests.csproj','-c',$configurationName,'--no-restore')
    Invoke-Check "retention-build-$configurationName" @('build','tests/NovaCore.Retention.Tests/NovaCore.Retention.Tests.csproj','-c',$configurationName,'--no-restore')
    Invoke-Check "worker-build-$configurationName" @('build','tools/NovaCore.Recorder/NovaCore.Recorder.csproj','-c',$configurationName,'--no-restore')
    Invoke-Check "recorder-$configurationName" @("tests/NovaCore.MinimumRecorder.Tests/bin/$configurationName/net10.0-windows/NovaCore.MinimumRecorder.Tests.exe","build/performance-150fps/recorder-$configurationName","tools/NovaCore.Recorder/bin/$configurationName/net10.0-windows/NovaCore.Recorder.exe")
    # Retention's child process harness requires its apphost, not dotnet.exe.
    $retention="tests/NovaCore.Retention.Tests/bin/$configurationName/net10.0-windows/NovaCore.Retention.Tests.exe"
    & $retention "build/performance-150fps/retention-$configurationName" "tools/NovaCore.Recorder/bin/$configurationName/net10.0-windows/NovaCore.Recorder.exe" *> (Join-Path $outputPath "retention-$configurationName.log")
    if($LASTEXITCODE -ne 0){throw "Retention qualification failed: $configurationName"}
    $graphics="tests/NovaCore.Graphics.Tests/bin/$configurationName/net10.0/NovaCore.Graphics.Tests.dll"
    foreach($case in @('Physical surface-point query','Physical surface-point stale snapshot','Earth CPU elevation oracle','Canonical body-fixed geographic handedness','Canonical SurfaceAnchor physical terrain authority','Multiscale physical terrain modifier foundation','Single canonical physical surface authority','Global/anchored physical frequency continuity','Local terrain format and GPU compression','Local terrain payload-2 authoring contract','M12 Florida regional physical surface')) {
        Invoke-Check ("physical-reference-"+($case -replace '[^a-zA-Z0-9]','-')+"-$configurationName") @($graphics,"--case=$case")
    }
    foreach($route in @('terrain-reuse','swept-clearance','surface-numerics','surface-recontact','surface-refinement','surface-cases','surface-cycles','surface-persistence','surface-terrain','surface-seams','surface-plane',
        'modular-connectors','modular-stabilization','modular-viewport','modular-editor-regressions','modular-gate9','modular-gate10-dynamics','modular-gate10-flight','modular-gate11','modular-gate12-application','florida-pad-authority','scalable-support-flight')) {
        Invoke-Check "$route-$configurationName" @($graphics,"--$route")
    }
}
Invoke-Check 'surface-performance-Release' @('tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll','--surface-performance')

Write-Output 'PERFORMANCE_150_OFFLINE_QUALIFICATION_PASS'
