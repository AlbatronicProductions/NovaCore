param([string[]]$Configurations=@('Debug','Release'))
$ErrorActionPreference='Stop'
$repoPath=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $repoPath
$outPath=Join-Path $repoPath 'build/recorder-bounded-storage'
New-Item -ItemType Directory -Path $outPath -Force | Out-Null
foreach($cfg in $Configurations){
    foreach($project in @('NovaCore.sln','tests/NovaCore.MinimumRecorder.Tests/NovaCore.MinimumRecorder.Tests.csproj','tests/NovaCore.Retention.Tests/NovaCore.Retention.Tests.csproj')){
        $name=[IO.Path]::GetFileNameWithoutExtension($project)
        & dotnet build $project -c $cfg --no-restore *> (Join-Path $outPath "build-$name-$cfg.log")
        if($LASTEXITCODE -ne 0){throw "Build failed: $project $cfg"}
    }
    $helper="tools/NovaCore.Recorder/bin/$cfg/net10.0-windows/NovaCore.Recorder.exe"
    & "tests/NovaCore.MinimumRecorder.Tests/bin/$cfg/net10.0-windows/NovaCore.MinimumRecorder.Tests.exe" (Join-Path $outPath "recorder-$cfg") $helper *> (Join-Path $outPath "recorder-$cfg.log")
    if($LASTEXITCODE -ne 0){throw "Recorder failed: $cfg"}
    & "tests/NovaCore.Retention.Tests/bin/$cfg/net10.0-windows/NovaCore.Retention.Tests.exe" (Join-Path $outPath "retention-$cfg") $helper *> (Join-Path $outPath "retention-$cfg.log")
    if($LASTEXITCODE -ne 0){throw "Bounded storage/retention failed: $cfg"}
    $uiOutput=Join-Path $outPath "ui-$cfg.txt"
    $ui=Start-Process -FilePath (Join-Path $repoPath "tools/NovaCore.App/bin/$cfg/net10.0-windows/NovaCore.exe") -ArgumentList @('--qualify-storage-ui',$uiOutput) -WindowStyle Hidden -Wait -PassThru
    if($ui.ExitCode -ne 0 -or !(Test-Path -LiteralPath $uiOutput)){throw "CPU-only storage UI failed: $cfg"}
    Get-Content -LiteralPath $uiOutput
    Write-Output "RECORDER_BOUNDED_STORAGE_$cfg PASS (GPU exposure=0)"
}
