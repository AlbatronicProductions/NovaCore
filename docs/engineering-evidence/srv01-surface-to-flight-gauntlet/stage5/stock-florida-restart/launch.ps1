param([switch]$PrepareOnly,[switch]$ProjectControlResume)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-stage5-stock'
$exe=Join-Path $scratch 'artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
$seals=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'candidate-seals.json') -Raw | ConvertFrom-Json
foreach($s in $seals.changed) {
    if((Get-FileHash -LiteralPath (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw "Candidate changed: $($s.path)"}
}
$binaries=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manual-build.json') -Raw | ConvertFrom-Json
foreach($s in $binaries.files) {
    if((Get-FileHash -LiteralPath (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw "Prepared binary changed: $($s.path)"}
}
if($PrepareOnly){Write-Output 'Verified Release stock Florida route. No run or rebuild.';return}
if(!$ProjectControlResume){throw 'PERFORMANCE STOP: Project Control must authorize resumption before another live/manual run. The explicit resume switch is for that future authorization only.'}
Write-Output 'Known startup residual: a white/unpresented window may precede READY. Startup time is not admitted as simulation debt.'
Write-Output 'This is the site-local view of the real Florida terrain-v5 graded 16m patch, 48m east of the site anchor. Engine and RCS remain OFF; stock stores remain 30/45 kg. No liftoff is expected.'
$attempt=1
while(Test-Path -LiteralPath (Join-Path $scratch "manual-$attempt.log")){$attempt++}
Push-Location $repo
try {
    & $exe '--scene=srv01-florida-support' '--log=renderer,vulkan' *> (Join-Path $scratch "manual-$attempt.log")
    if($LASTEXITCODE -ne 0){throw "Florida window exited $LASTEXITCODE"}
    Write-Output "Window closed; log: $scratch\manual-$attempt.log"
}
finally{Pop-Location}
