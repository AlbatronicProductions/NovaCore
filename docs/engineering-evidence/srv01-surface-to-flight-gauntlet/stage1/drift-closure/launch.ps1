param()
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$exe=Join-Path $repo 'build/srv01-drift-closure/qualification/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Manual build absent. Run reproduce.ps1 -Mode Build first.'}
$env:NOVACORE_WINDOW_CLIENT_WIDTH='1280'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='720'
Push-Location $repo
try { & $exe --scene=srv01-supported-contact --log=renderer,vulkan; if($LASTEXITCODE -ne 0){throw "Scene exit $LASTEXITCODE"} }
finally { Pop-Location }
