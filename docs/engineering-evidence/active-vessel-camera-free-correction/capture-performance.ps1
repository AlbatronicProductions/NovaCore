param([int]$Repetitions=2,[ValidateSet('live','fixed')][string]$Cadence='fixed')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo 'build/active-vessel-camera-free-correction/performance'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$env:NOVACORE_WINDOW_CLIENT_WIDTH='960'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='540'
$env:NOVACORE_WINDOW_BORDERLESS='0'
$env:NOVACORE_CAMERA_PROBE_CADENCE=$Cadence
$arguments=@('--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=4800','--log=renderer,vulkan')
for($run=1;$run -le $Repetitions;$run++) {
    foreach($route in @(@('baseline','matched'),@('candidate','matched'),@('candidate','new'))) {
        $version=$route[0];$mode=$route[1];$env:NOVACORE_CAMERA_PROBE=$mode
        $name=if($Cadence -eq 'live'){"$version-$mode-$run"}else{"$version-$mode-$Cadence-$run"}
        $observerRoot=if($version -eq 'baseline'){'build/active-vessel-camera'}else{'build/active-vessel-camera-free-correction'}
        $exe=Join-Path $repo "$observerRoot/observer-$version/runtime/NovaCore.Triangle.exe"
        $process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $repo -WindowStyle Hidden -RedirectStandardOutput "$output/$name.txt" -RedirectStandardError "$output/$name.err.txt" -PassThru
        $process.WaitForExit()
        Write-Output "$name exit=$($process.ExitCode)"
        if($process.ExitCode -ne 0){Get-Content "$output/$name.err.txt" -Tail 15;throw "Performance process failed: $name"}
        if((Select-String -LiteralPath "$output/$name.txt" -Pattern '^CAMERA_PROBE_COST ').Count -ne 8){throw "Missing camera populations: $name"}
    }
}
