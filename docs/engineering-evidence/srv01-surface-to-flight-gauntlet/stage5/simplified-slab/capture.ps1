$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
$output=Join-Path $PWD 'build/srv01-stage5-simplified-slab'
$exe=Join-Path $output 'observer/NovaCore.Triangle.exe'
$env:NOVACORE_WINDOW_CLIENT_WIDTH='960'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='540'
$env:NOVACORE_WINDOW_BORDERLESS='0'
Remove-Item Env:VK_INSTANCE_LAYERS -ErrorAction SilentlyContinue
$runs=@(
    @{name='manual';arguments=@('--scene=srv01-florida-support','--benchmark-frames=4800','--log=renderer,vulkan')},
    @{name='solar';arguments=@('--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--benchmark-frames=4800','--log=renderer,vulkan')}
)
foreach($run in $runs){
    $name=$run.name
    Write-Output "START $name"
    $process=Start-Process -FilePath $exe -WorkingDirectory $PWD -ArgumentList $run.arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput "$output/live-$name.txt" -RedirectStandardError "$output/live-$name.err.txt"
    $process.WaitForExit()
    [pscustomobject]@{name=$name;exit=$process.ExitCode;arguments=$run.arguments;observer=$true} | ConvertTo-Json -Depth 4 | Set-Content "$output/live-$name-result.json"
    Write-Output "END $name EXIT=$($process.ExitCode)"
    if($process.ExitCode -ne 0){throw "Live route failed: $name"}
    $log=Get-Content "$output/live-$name.txt" -Raw
    if(!$log.Contains('STOCK_ASSEMBLY_END completed=True failed=False') -or !$log.Contains('frontier=1200')){throw "Episode incomplete: $name"}
    Get-Content "$output/live-$name.txt" | Select-String 'STOCK_ASSEMBLY_END|STOCK_ASSEMBLY_FRAME|SRV01_LIVE_SERVICE|SLAB_LIVE_WARM|SLAB_LIVE_GC'
}
