param([switch]$PrepareOnly)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$closure=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'closure-verification.json') -Raw | ConvertFrom-Json
if(!$closure.engineeringQualified){throw 'Stage5 engineering closure is not qualified.'}
$previous=Join-Path $PSScriptRoot '../stock-florida-restart'
$sources=Get-Content -LiteralPath (Join-Path $previous 'candidate-seals.json') -Raw | ConvertFrom-Json
foreach($s in $sources.changed){if((Get-FileHash -LiteralPath (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw "Source identity changed: $($s.path)"}}
$binaries=Get-Content -LiteralPath (Join-Path $previous 'manual-build.json') -Raw | ConvertFrom-Json
foreach($s in $binaries.files){if((Get-FileHash -LiteralPath (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw "Uninstrumented binary identity changed: $($s.path)"}}
if($PrepareOnly){Write-Output 'Qualified manual route and original uninstrumented Release binary seals verified; no launch.';return}
$exe=Join-Path $repo 'build/srv01-stage5-stock/artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
$n=1
while(Test-Path -LiteralPath (Join-Path $repo "build/srv01-stage5-stock/manual-final-$n.log")){$n++}
$log=Join-Path $repo "build/srv01-stage5-stock/manual-final-$n.log"
Write-Output 'Florida stock engine-OFF manual acceptance. Wait through any known white startup period for READY, then Space. First contact has a disclosed roughly 54-59 ms cold servicing cost; no optimization is claimed.'
Write-Output 'This is the authenticated terrain-v5 graded patch 48 m east of the Florida anchor, shown in site-local coordinates, not a full facility view. Engine/RCS OFF; no departure. WASD/QE, mouse, R camera reset.'
Push-Location $repo
try {
    & $exe '--scene=srv01-florida-support' '--log=renderer,vulkan' *> $log
    if($LASTEXITCODE -ne 0){throw "Manual window failed: $LASTEXITCODE; log=$log"}
    Write-Output "Manual window closed. Report observations to Project Control. Log: $log"
}
finally{Pop-Location}
