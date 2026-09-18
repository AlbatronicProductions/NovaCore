param([int]$Repetitions=2,[int]$StartRun=1,[switch]$MatchedCelestial)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo 'build/active-vessel-camera-warp-correction/performance'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$env:NOVACORE_WINDOW_CLIENT_WIDTH='960';$env:NOVACORE_WINDOW_CLIENT_HEIGHT='540';$env:NOVACORE_WINDOW_BORDERLESS='0'
$env:NOVACORE_WARP_MATCHED_CELESTIAL=if($MatchedCelestial){'1'}else{'0'}
$frames=if($MatchedCelestial){1200}else{4800}
$expected=if($MatchedCelestial){3}else{12}
$arguments=@('--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000',"--benchmark-frames=$frames",'--log=renderer,vulkan')
for($run=$StartRun;$run -le $Repetitions;$run++) {
 foreach($version in @('entry','candidate')) {
  $name=if($MatchedCelestial){"matched-$version-$run"}else{"$version-$run"}
  $exe=Join-Path $repo "build/active-vessel-camera-warp-correction/observer-$version/runtime/NovaCore.Triangle.exe"
  $process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $repo -WindowStyle Hidden -RedirectStandardOutput "$output/$name.txt" -RedirectStandardError "$output/$name.err.txt" -PassThru
  $process.WaitForExit()
  if($process.ExitCode -ne 0){Get-Content "$output/$name.err.txt" -Tail 15;throw "Capture failed $name exit=$($process.ExitCode)"}
  if((Select-String -LiteralPath "$output/$name.txt" -Pattern '^WARP_RENDER_COST ').Count -ne $expected){throw "Incomplete capture $name"}
  Write-Output "$name exit=0 complete=$frames"
 }
}
