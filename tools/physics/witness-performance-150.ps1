param([Parameter(Mandatory)][ValidateSet('stage-a','stage-b')][string]$Stage)
$ErrorActionPreference='Stop'
$repoPath=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $repoPath
$outPath=Join-Path $repoPath "build/performance-150fps/$Stage"
if(!(Test-Path -LiteralPath (Join-Path $outPath 'offline-admission.json'))){throw 'Missing explicit offline admission for this qualified correction'}
$admission=Get-Content -LiteralPath (Join-Path $outPath 'offline-admission.json') -Raw | ConvertFrom-Json
if($admission.judgment -ne 'PASS'){throw 'Offline admission did not pass'}
if(Get-Process NovaCore -ErrorAction SilentlyContinue){throw 'A player process is already running'}
$marker=Join-Path $outPath 'native-launch.json'
if(Test-Path -LiteralPath $marker){throw 'This correction already consumed its native exposure'}
python tools/physics/seal-performance-150.py --output (Join-Path $outPath 'prelaunch-check.json') --compare (Join-Path $outPath 'source-freeze.json')
if($LASTEXITCODE -ne 0){throw 'Source/package/preservation failed'}
$freeze=Get-Content -LiteralPath (Join-Path $outPath 'source-freeze.json') -Raw | ConvertFrom-Json
$started=[DateTime]::UtcNow
@{authorization='one measured native proof per qualified correction';stage=$Stage;utc=$started.ToString('o');source=$freeze.sourceSha256;package=$freeze.packageSha256} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding utf8
Copy-Item -LiteralPath 'build/surface-recontact/retry/native.json.input.ncflight.json' -Destination (Join-Path $outPath 'native.json.input.ncflight.json')
$info=[Diagnostics.ProcessStartInfo]::new((Join-Path $repoPath 'tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe'))
$info.UseShellExecute=$false
$info.WorkingDirectory=$repoPath
$info.ArgumentList.Add('--qualify-editor');$info.ArgumentList.Add((Join-Path $outPath 'native.json'))
$info.ArgumentList.Add('--qualification-tank');$info.ArgumentList.Add('surface-retry')
$info.Environment['NOVACORE_POST_CONTACT_TIMINGS']=Join-Path $outPath 'native-timing.csv'
$player=[Diagnostics.Process]::Start($info)
$watch=[Diagnostics.Stopwatch]::StartNew();$samples=[Collections.Generic.List[object]]::new();$forced=$false
while(!$player.HasExited){
    $player.Refresh()
    $samples.Add(@{seconds=$watch.Elapsed.TotalSeconds;cpuMs=$player.TotalProcessorTime.TotalMilliseconds;workingSet=$player.WorkingSet64;peakWorkingSet=$player.PeakWorkingSet64;privateBytes=$player.PrivateMemorySize64})
    if($watch.Elapsed.TotalSeconds -gt 120){$forced=$true;$player.Kill();break}
    Start-Sleep -Milliseconds 500
}
$player.WaitForExit()
@{startedUtc=$started.ToString('o');pid=$player.Id;wallSeconds=$watch.Elapsed.TotalSeconds;exitCode=$player.ExitCode;forcedStop=$forced;samples=$samples} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outPath 'process.json') -Encoding utf8
$osEvents=@(Get-WinEvent -FilterHashtable @{LogName='System';StartTime=$started.ToLocalTime();Level=1,2,3} -ErrorAction SilentlyContinue | Select-Object TimeCreated,Id,ProviderName,LevelDisplayName,Message)
@{events=$osEvents;queriedFromUtc=$started.ToString('o')} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outPath 'os-after.json') -Encoding utf8
Write-Output "NATIVE_WITNESS_ENDED pid=$($player.Id) exit=$($player.ExitCode) forced=$forced"
if($forced -or $player.ExitCode -ne 0){throw 'STOP: native abnormality; preserve evidence, no relaunch/rebuild'}
