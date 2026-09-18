# Resume only the missing population; retain any incomplete attempts separately.
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output=Join-Path $repo 'build/active-vessel-camera-free-correction'
$env:NOVACORE_WINDOW_CLIENT_WIDTH='960'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='540'
$env:NOVACORE_WINDOW_BORDERLESS='0'
$env:NOVACORE_CAMERA_PROBE_CADENCE='fixed'
$env:NOVACORE_CAMERA_PROBE='new'
$arguments=@('--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=4800','--log=renderer,vulkan')
$process=Start-Process -FilePath "$output/observer-candidate/runtime/NovaCore.Triangle.exe" -ArgumentList $arguments -WorkingDirectory $repo -WindowStyle Hidden -RedirectStandardOutput "$output/performance/candidate-new-fixed-2.txt" -RedirectStandardError "$output/performance/candidate-new-fixed-2.err.txt" -PassThru
$process.WaitForExit()
[pscustomobject]@{name='candidate-new-fixed-2';exit=$process.ExitCode;retryReason='Earlier attempts ended without final populations or parent completion record; cause unproven';partialLogs=@(Get-ChildItem "$output/incomplete/*.txt"|ForEach-Object {[ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash $_.FullName).Hash}})}|ConvertTo-Json -Depth 5|Set-Content "$output/capture-retry.json"
Write-Output "candidate-new-fixed-2 exit=$($process.ExitCode)"
if($process.ExitCode -ne 0){throw 'Renderer failed'}
