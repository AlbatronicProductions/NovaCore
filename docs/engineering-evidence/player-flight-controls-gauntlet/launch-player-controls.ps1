$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$exe=Join-Path $repo 'build/player-flight-controls-gauntlet/candidate/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Build the isolated Release candidate first.'}
$start=[Diagnostics.ProcessStartInfo]::new()
$start.FileName=$exe
$start.WorkingDirectory=$repo
$start.UseShellExecute=$false
$start.WindowStyle=[Diagnostics.ProcessWindowStyle]::Normal
$start.Environment.Remove('NOVACORE_CONTROL_TRACE') | Out-Null
foreach($arg in @('--scene=sol','--player-flight-controls','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate')){$start.ArgumentList.Add($arg)}
[Diagnostics.Process]::Start($start) | Out-Null
