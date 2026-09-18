$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output='build/active-vessel-camera-free-correction'
function Json([string]$name,$value){$value|ConvertTo-Json -Depth 30 -Compress|Set-Content -LiteralPath (Join-Path $PSScriptRoot $name)}
function Parsed($lines,[string]$prefix){@($lines|Where-Object {$_.StartsWith($prefix)}|ForEach-Object {$_.Substring($prefix.Length)|ConvertFrom-Json})}
$runs=@(foreach($file in Get-ChildItem "$output/performance/*.txt"|Where-Object {$_.Name -notlike '*.err.txt'}){
 $lines=Get-Content -LiteralPath $file.FullName
 $metrics=Parsed $lines 'CAMERA_PROBE_COST '
 if($metrics.Count -ne 8){throw "Incomplete capture $($file.Name)"}
 if((Get-Item ($file.FullName.Replace('.txt','.err.txt'))).Length -ne 0){throw 'Renderer stderr is not empty'}
 [ordered]@{name=$file.BaseName;sha256=(Get-FileHash $file.FullName).Hash;bytes=$file.Length;
 identity=@($lines|Where-Object {$_.StartsWith('CAMERA_PROBE_ID ')});startup=@($lines|Where-Object {$_.StartsWith('CAMERA_PROBE_FIRST ')});
 metrics=$metrics;transitions=(Parsed $lines 'CAMERA_PROBE_TRANSITION ');switches=(Parsed $lines 'CAMERA_PROBE_SWITCH ');
 native=@($lines|Where-Object {$_ -match '^\[native\] (Frame pacing:|Fence wait pacing:|CPU timings:|GPU timing averages:|Frame pacing tail|Frame pacing slow|First present|.*first-present)'})}
})
if($runs.Count -ne 6){throw 'Expected six sequential renderer populations'}
Json 'performance.json' ([ordered]@{units='milliseconds';processes=6;resolution='960x540';framesPerProcess=4800;warmFramesPerRoute=180;measuredFramesPerRoute=1020;repetitions=2;
 baseline='45b1bbcdd8d0e1d42f79f3bb5124e80753ce0e8f';comparison='Matched existing Florida/Earth/Moon/switch routes with identical fixed physical credit. New core vessel/refocus routes are characterized separately; FREE is excluded.';
 limits=@('Fixed physical credit is diagnostic, not manual acceptance.','Native CPU/GPU metrics are whole-run aggregates.','Startup, transition tails and indexed switches are retained.','Single resolution and machine, two repetitions.');runs=$runs})
$validation=Get-Content "$output/validation-results.json" -Raw|ConvertFrom-Json
$gates=@(foreach($gate in $validation){$lines=Get-Content -LiteralPath $gate.log;[ordered]@{name=$gate.name;exit=$gate.exit;arguments=$gate.arguments;sha256=(Get-FileHash $gate.log).Hash;summary=@($lines|Where-Object {$_ -match 'ACTIVE_VESSEL_STATIC|ACTIVE_VESSEL_MOVING|^ORDINARY_ALLOCATION|^ACTIVE_VESSEL_STORAGE|passed|failed|PASS|FAIL|^Graphics|^Total'}|Select-Object -Last 2)}})
if($gates.Count -ne 94 -or @($gates|Where-Object {$_.exit -ne 0}).Count -ne 0){throw 'Regression matrix incomplete'}
Json 'regression.json' ([ordered]@{gateCount=94;failed=0;finalTestRefinement='Combined F/reserved action now starts from Moon; final Debug/Release builds and static reruns passed. Other runtime/source behavior unchanged after full matrix.';gates=$gates})
$camera=@(foreach($configuration in @('debug','release')){$lines=Get-Content "$output/florida-slab-camera-costs-$configuration.txt";[ordered]@{configuration=$configuration;allocation=@($lines|Where-Object {$_.StartsWith('ORDINARY_')});costs=(Parsed $lines 'ACTIVE_VESSEL_COST ');storage=(Parsed $lines 'ACTIVE_VESSEL_STORAGE ')}})
Json 'allocation.json' ([ordered]@{applicableZeroPaths=@('follow/orbit/zoom/display','F-refocus');positiveControlExpectedBytes=152;inheritedCelestialSwitchBytesPerAction=48;inheritedCause='Existing discrete Solar.Focus PlanetaryRepresentationHandoff';configurations=$camera})
Write-Output 'Consolidated six renderer processes and 94 gates.'
