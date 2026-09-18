param([switch]$Characterize,[int]$Run=1)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-stage2-closure'
$exe=Join-Path $scratch 'artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Run reproduce.ps1 -Mode Build first.'}
$env:NOVACORE_WINDOW_CLIENT_WIDTH='1280'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='720'
$arguments=@('--scene=srv01-powered-support','--log=renderer,vulkan')
if($Characterize){$arguments+='--benchmark-frames=4000'}
$name=if($Characterize){"live-$Run.log"}else{'manual.log'}
Push-Location $repo
try { & $exe @arguments *> (Join-Path $scratch $name); if($LASTEXITCODE -ne 0){throw "Scene exit $LASTEXITCODE"} }
finally {Pop-Location}
