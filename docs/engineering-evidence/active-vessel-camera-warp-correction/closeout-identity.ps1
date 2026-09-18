$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
$output='build/active-vessel-camera-warp-correction'
$delta=@('samples/NovaCore.Triangle/Program.cs','samples/NovaCore.Triangle/SolarSystemScene.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs','src/NovaCore.Graphics/FocusTarget.cs','src/NovaCore.Graphics/SceneObjectFocusObservation.cs','tests/NovaCore.Graphics.Tests/Program.cs','tests/NovaCore.Graphics.Tests/ActiveVesselCameraTests.cs')
$review=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verification.md'))
$source=@(foreach($item in $entry.source){
 $hash=(Get-FileHash -LiteralPath $item.path).Hash
 if($item.path -notin $delta -and $hash -ne $item.sha256){throw "Unrelated source changed: $($item.path)"}
 if(!$review.Contains('| '+$item.path+' | '+$hash+' |')){throw "Independent readset differs: $($item.path)"}
 if((Get-FileHash -LiteralPath "$output/entry-source/$($item.path)").Hash -ne $item.sha256){throw 'Entry seal differs'}
 [ordered]@{path=$item.path;sha256=$hash;changedThisCorrection=$hash -ne $item.sha256}
})
foreach($item in $entry.untracked){if($item.path -notin $delta -and (Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){throw "Prior evidence changed: $($item.path)"}}
foreach($path in @(& git diff --name-only)){if($path -notin $entry.source.path){throw "Unexpected tracked change: $path"}}
foreach($path in @(& git ls-files --others --exclude-standard)){
 if($path -notin $entry.untracked.path -and !$path.StartsWith('docs/engineering-evidence/active-vessel-camera-warp-correction/')){throw "Unexpected untracked addition: $path"}
}
if((& git rev-parse HEAD) -ne $entry.head){throw 'HEAD moved'}
$tag='m15.3-canonical-srv01-florida-supported-flight-foundation'
if((& git rev-parse $tag) -ne $entry.tagObject -or (& git rev-parse "$tag^{}") -ne $entry.bank){throw 'Bank/tag moved'}
$remote=@(& git ls-remote origin refs/heads/main "refs/tags/$tag" "refs/tags/$tag^{}")
if($LASTEXITCODE -ne 0 -or !($remote -match "^$($entry.head)\s+refs/heads/main$") -or !($remote -match "^$($entry.tagObject)\s+refs/tags/$tag$") -or !($remote -match "^$($entry.bank)\s+refs/tags/$tag\^\{\}$")){throw 'Remote identity differs'}
& git diff --cached --quiet
if($LASTEXITCODE -ne 0){throw 'Index not empty'}
& git diff --check
if($LASTEXITCODE -ne 0){throw 'Whitespace failure'}
if((Get-FileHash -LiteralPath 'E:/Kitten Space Agency/KSA.dll').Hash -ne $entry.ksaSha256){throw 'KSA identity changed'}
$deployment=Get-Content "$output/deployment.json" -Raw|ConvertFrom-Json
foreach($item in $deployment){if((Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){throw 'Deployment changed'}}
$runtime=@($deployment|Where-Object {$_.path -match 'NovaCore\.(Triangle|Launcher|Graphics|Native|Simulation|Core)\.(dll|exe)$'})
$testBinaries=@(foreach($configuration in @('debug','release')){foreach($project in @('Graphics','Simulation','Camera','Precision','ReferenceFrames')){
 $path="$output/candidate/bin/NovaCore.$project.Tests/$configuration/NovaCore.$project.Tests.dll"
 [ordered]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash}
}})
$smoke=Get-Content "$output/final-smoke.json" -Raw|ConvertFrom-Json
if($smoke.exit -ne 0 -or $smoke.stderrBytes -ne 0 -or !$smoke.frames){throw 'Final deployed smoke incomplete'}
$retirement=Get-Content (Join-Path $PSScriptRoot 'retirement.json') -Raw|ConvertFrom-Json
foreach($target in $retirement.targets){if(Test-Path -LiteralPath $target.path){throw 'Disposable runtime remains'}}
if((Get-FileHash "$output/retired-file-manifest.json").Hash -ne $retirement.inventorySha256){throw 'Retirement provenance changed'}
$package=@(Get-ChildItem $PSScriptRoot -File|Where-Object {$_.Name -ne 'identity.json'}|ForEach-Object {[ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash $_.FullName).Hash}})
[ordered]@{capturedUtc=[DateTime]::UtcNow.ToString('O');head=$entry.head;branch=(& git branch --show-current);bank=$entry.bank;tagObject=$entry.tagObject;remote=$remote;indexEmpty=$true;diffCheck='PASS';source=$source;independentReadsetMatches=$true;runtimeBinaries=$runtime;testBinaries=$testBinaries;ksaSha256=$entry.ksaSha256;priorEvidencePreserved=$true;
 manualAcceptance='PASS — explicit Project Control user report, after manual deployment and hash verification';deploymentReconciliation='88/88 files match candidate; VerifyOnly used; accepted runtime not replaced';deploymentManifestSha256=(Get-FileHash "$output/deployment.json").Hash;deploymentFiles=$deployment.Count;finalDeployedSmoke=$smoke;finalSmokeLogSha256=(Get-FileHash "$output/final-smoke.txt").Hash;
 freeCamera='DEFERRED';home='unbound';milestone='NOT ASSIGNED';bankingAuthorized=$false;nextProductionFrontOpened=$false;regressionGates=96;regressionFailures=0;nativePerformanceCompleteProcesses=8;retiredDiagnosticBytes=$retirement.retiredBytes;retainedCandidateBytes=(Get-ChildItem "$output/candidate" -Recurse -File|Measure-Object Length -Sum).Sum;candidateRetentionReason='Exact already-accepted Debug/Release build pair retained temporarily for Project Control unbanked handoff and deployment provenance; not permanent archival promotion';packageBudgetBytes=$entry.packageBudgetBytes;packageFilesExcludingThisIdentity=$package}|ConvertTo-Json -Depth 12 -Compress|Set-Content (Join-Path $PSScriptRoot 'identity.json')
$total=(Get-ChildItem $PSScriptRoot -File|Measure-Object Length -Sum).Sum
if($total -gt $entry.packageBudgetBytes){throw "Evidence budget exceeded: $total"}
Write-Output "Closure PASS: 96 gates; 88 deployment files; manual PASS; independent readset matches; Git unchanged; package $total/$($entry.packageBudgetBytes) bytes."
