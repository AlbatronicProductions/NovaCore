$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
$old=Get-Content 'docs/engineering-evidence/active-vessel-camera/entry-identity.json' -Raw|ConvertFrom-Json
$delta=@('native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/Program.cs','samples/NovaCore.Triangle/SolarSystemScene.cs','src/NovaCore.Interop/NativeRuntime.cs','tests/NovaCore.Graphics.Tests/ActiveVesselCameraTests.cs')
$historical=@('README.md','manual-route.md','input-bindings.md','implementation.md','test-contract.md','verification.md','ksa-current-camera.md','performance.md','reproduce.md','owner-map.md','target-contract.md')|ForEach-Object {'docs/engineering-evidence/active-vessel-camera/'+$_}
$notice='> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).'+[Environment]::NewLine+[Environment]::NewLine
foreach($item in $entry.source){if($item.path -notin $delta -and (Get-FileHash $item.path).Hash -ne $item.sha256){throw "Unrelated source changed: $($item.path)"}}
foreach($item in $entry.untracked){
 if($item.path -in $delta){continue}
 if($item.path -in $historical){
  $text=[IO.File]::ReadAllText((Join-Path $repo $item.path))
  if(!$text.StartsWith($notice)){throw 'Historical supersession missing'}
  $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text.Substring($notice.Length))))
  if($hash -ne $item.sha256){throw "Historical body changed: $($item.path)"}
 }elseif((Get-FileHash $item.path).Hash -ne $item.sha256){throw "Prior evidence changed: $($item.path)"}
}
foreach($path in @(& git diff --name-only)){if($path -notin $entry.source.path){throw "Unexpected tracked change: $path"}}
if((& git rev-parse HEAD) -ne $entry.head){throw 'HEAD moved'}
$tag='m15.3-canonical-srv01-florida-supported-flight-foundation'
if((& git rev-parse $tag) -ne $old.tagObject -or (& git rev-parse "$tag^{}") -ne $old.bank){throw 'Bank/tag moved'}
$remote=@(& git ls-remote origin refs/heads/main "refs/tags/$tag" "refs/tags/$tag^{}")
if($LASTEXITCODE -ne 0 -or !($remote -match "^$($entry.head)\s+refs/heads/main$") -or !($remote -match "^$($old.tagObject)\s+refs/tags/$tag$") -or !($remote -match "^$($old.bank)\s+refs/tags/$tag\^\{\}$")){throw 'Remote identity differs'}
& git diff --cached --quiet
if($LASTEXITCODE -ne 0){throw 'Index not empty'}
& git diff --check
if($LASTEXITCODE -ne 0){throw 'Whitespace failure'}
if((Get-FileHash 'E:/Kitten Space Agency/KSA.dll').Hash -ne $entry.ksaSha256){throw 'KSA moved'}
$output='build/active-vessel-camera-free-correction'
$deployment=Get-Content "$output/deployment.json" -Raw|ConvertFrom-Json
foreach($item in $deployment){if((Get-FileHash $item.path).Hash -ne $item.sha256){throw 'Deployment mismatch'}}
$source=@(foreach($item in $entry.source){[ordered]@{path=$item.path;sha256=(Get-FileHash $item.path).Hash;changedThisCorrection=$item.path -in $delta}})
$binaries=@(foreach($path in @('samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.dll','samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Native.dll','build/active-vessel-camera/observer-baseline/runtime/NovaCore.Triangle.dll','build/active-vessel-camera-free-correction/observer-candidate/runtime/NovaCore.Triangle.dll')){[ordered]@{path=$path;sha256=(Get-FileHash $path).Hash}})
$package=@(Get-ChildItem $PSScriptRoot -File|Where-Object {$_.Name -ne 'identity.json'}|ForEach-Object {[ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash $_.FullName).Hash}})
$identity=[ordered]@{capturedUtc=[DateTime]::UtcNow.ToString('O');head=$entry.head;branch=(& git branch --show-current);bank=$old.bank;tagObject=$old.tagObject;remote=$remote;indexEmpty=$true;diffCheck='PASS';source=$source;runtimeBinaries=$binaries;ksaSha256=$entry.ksaSha256;priorEvidencePreserved=$true;historicalDocsChangedBySupersessionOnly=$historical;manualAcceptance='RETEST PENDING; PREVIOUS STEP 5 FAILED';freeCamera='DEFERRED';milestone='NOT ASSIGNED';bankingAuthorized=$false;deploymentManifestSha256=(Get-FileHash "$output/deployment.json").Hash;temporaryDisposition='Retained pending Project Control; no deletion';packageBudgetBytes=$entry.packageBudgetBytes;packageFilesExcludingThisIdentity=$package}
$identity|ConvertTo-Json -Depth 10 -Compress|Set-Content (Join-Path $PSScriptRoot 'identity.json')
$total=(Get-ChildItem $PSScriptRoot -File|Measure-Object Length -Sum).Sum
if($total -gt $entry.packageBudgetBytes){throw "Evidence budget exceeded: $total"}
Write-Output "Identity PASS; five correction source/test paths; prior evidence preserved; package bytes=$total."
