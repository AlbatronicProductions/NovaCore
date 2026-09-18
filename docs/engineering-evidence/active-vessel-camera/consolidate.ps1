$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output='build/active-vessel-camera'
function Json([string]$name,$value){$value|ConvertTo-Json -Depth 30 -Compress|Set-Content -LiteralPath (Join-Path $PSScriptRoot $name)}
function Parsed($lines,[string]$prefix){@($lines|Where-Object {$_.StartsWith($prefix)}|ForEach-Object {$_.Substring($prefix.Length)|ConvertFrom-Json})}
$runs=@(foreach($file in Get-ChildItem "$output/performance/*.txt" | Where-Object {$_.Name -notlike '*.err.txt'}){
    $lines=Get-Content -LiteralPath $file.FullName
    $metrics=Parsed $lines 'CAMERA_PROBE_COST '
    if($metrics.Count -ne 8){throw "Incomplete capture $($file.Name)"}
    [ordered]@{name=$file.BaseName;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash;bytes=$file.Length;
        identity=@($lines|Where-Object {$_.StartsWith('CAMERA_PROBE_ID ')});
        startup=@($lines|Where-Object {$_.StartsWith('CAMERA_PROBE_FIRST ')});
        metrics=$metrics;transitions=(Parsed $lines 'CAMERA_PROBE_TRANSITION ');switches=(Parsed $lines 'CAMERA_PROBE_SWITCH ');
        native=@($lines|Where-Object {$_ -match '^\[native\] (Frame pacing:|Fence wait pacing:|CPU timings:|GPU timing averages:|Frame pacing tail|Frame pacing slow|First present|.*first-present)'})}
})
Json 'performance.json' ([ordered]@{units='milliseconds';processes=$runs.Count;resolution='960x540';framesPerProcess=4800;warmFramesPerRoute=180;measuredFramesPerRoute=1020;repetitions=2;
    baseline='45b1bbcdd8d0e1d42f79f3bb5124e80753ce0e8f';
    comparison='Matched scripted existing presentation routes; fixed variants also feed identical physical credit. New vessel/free/refocus semantics have no baseline equivalent and are characterized separately.';
    limits=@('Live runs use wall-clock physical servicing.','Original live captures omit final callback GC and per-route transition detail.','Sparse integrated switch actions are separately retained and complemented by dedicated action distributions.','Native CPU/GPU values are aggregate observations, not per-route GPU percentiles.','Startup and first-present residual remain.');runs=$runs})
$validation=Get-Content "$output/validation-results.json" -Raw|ConvertFrom-Json
$gates=@(foreach($gate in $validation){$lines=Get-Content -LiteralPath $gate.log;[ordered]@{name=$gate.name;exit=$gate.exit;executable=$gate.executable;arguments=$gate.arguments;sha256=(Get-FileHash -LiteralPath $gate.log).Hash;summary=@($lines|Where-Object {$_ -match 'ACTIVE_VESSEL_STATIC|ACTIVE_VESSEL_MOVING|^ORDINARY_ALLOCATION|^ACTIVE_VESSEL_STORAGE|passed|failed|PASS|FAIL|^Graphics|^Total' }|Select-Object -Last 5)}})
Json 'regression.json' ([ordered]@{gateCount=$gates.Count;failed=@($gates|Where-Object {$_.exit -ne 0}).Count;gates=$gates})
$camera=@(foreach($configuration in @('debug','release')){
    $lines=Get-Content "$output/florida-slab-camera-costs-$configuration.txt"
    [ordered]@{configuration=$configuration;allocation=@($lines|Where-Object {$_.StartsWith('ORDINARY_')});costs=(Parsed $lines 'ACTIVE_VESSEL_COST ');storage=(Parsed $lines 'ACTIVE_VESSEL_STORAGE ')}
})
Json 'allocation.json' ([ordered]@{applicableZeroPaths=@('follow/orbit/zoom/display','free/refocus','free/display');positiveControlExpectedBytes=152;
    inheritedCelestialSwitchBytesPerAction=48;inheritedCause='Existing Solar.Focus creates PlanetaryRepresentationHandoff; discrete celestial action is separately measured and excluded from zero-allocation claims.';configurations=$camera})
$builds=@(foreach($build in @(@('Debug','solution-debug.txt'),@('Release','release-build.txt'))){$path="$output/$($build[1])";[ordered]@{configuration=$build[0];log=$path;sha256=(Get-FileHash -LiteralPath $path).Hash;summary=@(Get-Content -LiteralPath $path|Where-Object {$_ -match 'Build succeeded|Warning\(s\)|Error\(s\)'})}})
$smoke=[ordered]@{frames=480;exit=0;stderrBytes=(Get-Item "$output/deployment-smoke.err.txt").Length;sha256=(Get-FileHash "$output/deployment-smoke.txt").Hash;runtimeAssemblySha256=(Get-FileHash 'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.dll').Hash;instrumented=$false}
Json 'validation.json' ([ordered]@{engineeringGates=if(@($gates|Where-Object {$_.exit -ne 0}).Count -eq 0){'PASS'}else{'FAIL'};manualRequired=$true;manualAcceptance='PENDING';judgment='STOP FOR PROJECT CONTROL';milestone='NOT ASSIGNED';bankingAuthorized=$false;coverage='test-contract.md';independentRedTeam='verification.md';regression='regression.json';allocation='allocation.json';performance='performance.json';builds=$builds;deployedSmoke=$smoke})
Write-Output "Consolidated $($runs.Count) renderer runs and $($gates.Count) validation gates."
