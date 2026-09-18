$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
$allowed=@('native/NovaCore.Native/NovaCoreNative.cpp','native/NovaCore.Native/NovaCoreNative.h','samples/NovaCore.Triangle/Program.cs','samples/NovaCore.Triangle/SolarSystemScene.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs','src/NovaCore.Graphics/FocusTarget.cs','src/NovaCore.Graphics/SceneObjectFocusObservation.cs','src/NovaCore.Interop/NativeRuntime.cs','tests/NovaCore.Graphics.Tests/AssemblyFloridaSolarTests.cs','tests/NovaCore.Graphics.Tests/Program.cs','tests/NovaCore.Graphics.Tests/ActiveVesselCameraTests.cs')
$changed=@(& git -c core.quotepath=false diff --name-only)
foreach($path in $changed){if($path -notin $allowed){throw "Unexpected tracked change: $path"}}
foreach($item in $entry.preExistingUntracked){if((Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){throw "Preexisting untracked content changed: $($item.path)"}}
foreach($item in $entry.sourceInputs){if($item.path -notin $allowed -and (Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){throw "Unrelated source changed: $($item.path)"}}
if((& git rev-parse HEAD) -ne $entry.head){throw 'HEAD moved'}
$tag='m15.3-canonical-srv01-florida-supported-flight-foundation'
if((& git rev-parse $tag) -ne $entry.tagObject -or (& git rev-parse "$tag^{}") -ne $entry.bank){throw 'Bank/tag moved'}
$remote=@(& git ls-remote origin refs/heads/main "refs/tags/$tag" "refs/tags/$tag^{}")
if($LASTEXITCODE -ne 0 -or !($remote -match "^$($entry.head)\s+refs/heads/main$") -or !($remote -match "^$($entry.tagObject)\s+refs/tags/$tag$")){throw 'Remote identity differs'}
& git diff --cached --quiet
if($LASTEXITCODE -ne 0){throw 'Index is not empty'}
& git diff --check
if($LASTEXITCODE -ne 0){throw 'Whitespace check failed'}
$ksa=(Get-FileHash -LiteralPath 'E:/Kitten Space Agency/KSA.dll').Hash
if($ksa -ne $entry.ksaSha256){throw 'KSA identity moved'}
$source=@(foreach($path in $allowed){[ordered]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash}})
$binaries=@(foreach($version in @('baseline','candidate')){foreach($name in @('NovaCore.Triangle.dll','NovaCore.Interop.dll','NovaCore.Graphics.dll','NovaCore.Simulation.dll','NovaCore.Native.dll')){$path="build/active-vessel-camera/$version/bin/NovaCore.Triangle/release/$name";[ordered]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash}}})
$deployment=Get-Content 'build/active-vessel-camera/deployment.json' -Raw|ConvertFrom-Json
foreach($item in $deployment){if((Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){throw "Deployment changed: $($item.path)"}}
$package=@(Get-ChildItem -LiteralPath $PSScriptRoot -File|Where-Object {$_.Name -ne 'identity.json'}|ForEach-Object {[ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$temporary=Get-ChildItem -LiteralPath 'build/active-vessel-camera' -Recurse -File|Measure-Object Length -Sum
$identity=[ordered]@{capturedUtc=[DateTime]::UtcNow.ToString('O');head=$entry.head;branch=(& git branch --show-current);bank=$entry.bank;tagObject=$entry.tagObject;remote=$remote;indexEmpty=$true;diffCheck='PASS';productionTestPaths=$source;runtimeBinaries=$binaries;originalUntrackedCount=$entry.preExistingUntracked.Count;originalUntrackedPreserved=$true;unrelatedSourceInputsPreserved=$true;ksaSha256=$ksa;manualAcceptance='PENDING';milestone='NOT ASSIGNED';bankingAuthorized=$false;deployedFiles=$deployment.Count;deploymentManifestSha256=(Get-FileHash 'build/active-vessel-camera/deployment.json').Hash;temporaryFiles=$temporary.Count;temporaryBytes=$temporary.Sum;temporaryDisposition='Retained pending manual acceptance; no automatic deletion';packageBudgetBytes=$entry.packageBudgetBytes;packageFilesExcludingThisIdentity=$package}
$identity|ConvertTo-Json -Depth 10|Set-Content (Join-Path $PSScriptRoot 'identity.json')
$total=(Get-ChildItem -LiteralPath $PSScriptRoot -File|Measure-Object Length -Sum).Sum
if($total -gt $entry.packageBudgetBytes){throw "Evidence budget exceeded: $total"}
Write-Output "Identity PASS; source/test paths=$($allowed.Count); unchanged prior untracked=$($entry.preExistingUntracked.Count); package bytes=$total; temporary bytes=$($temporary.Sum)."
