$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output='build/active-vessel-camera-warp-correction'
function Json([string]$name,$value){$value|ConvertTo-Json -Depth 24 -Compress|Set-Content -LiteralPath (Join-Path $PSScriptRoot $name)}
function Parsed($lines,[string]$prefix){@($lines|Where-Object {$_.StartsWith($prefix)}|ForEach-Object {$_.Substring($prefix.Length)|ConvertFrom-Json})}
$runs=@(foreach($file in Get-ChildItem "$output/performance/*.txt"|Where-Object {$_.Name -notlike '*.err.txt' -and $_.Name -notlike 'incomplete-*'}){
 $lines=Get-Content -LiteralPath $file.FullName
 $metrics=Parsed $lines 'WARP_RENDER_COST '
 $matched=$file.Name.StartsWith('matched-')
 $expected=if($matched){3}else{12}
 if($metrics.Count -ne $expected){throw "Incomplete capture $($file.Name)"}
 if((Get-Item ($file.FullName.Replace('.txt','.err.txt'))).Length -ne 0){throw 'Renderer stderr is not empty'}
 [ordered]@{name=$file.BaseName;sha256=(Get-FileHash $file.FullName).Hash;bytes=$file.Length;matchedCelestial=$matched;
 identity=@($lines|Where-Object {$_.StartsWith('WARP_RENDER_ID ')});metrics=$metrics;
 native=@($lines|Where-Object {$_ -match '^\[native\] (Frame pacing:|Fence wait pacing:|CPU timings:|GPU timing averages:|Frame pacing tail|Frame pacing slow|First present|.*first-present)'})}
})
if($runs.Count -ne 8){throw 'Expected four full routes plus four matched-celestial captures'}
$incomplete=@(Get-ChildItem "$output/performance/incomplete-*"|ForEach-Object {[ordered]@{path=$_.FullName;bytes=$_.Length;sha256=(Get-FileHash $_.FullName).Hash;qualified=$false}})
Json 'performance.json' ([ordered]@{units='milliseconds';resolution='960x540';repetitions=2;processes=8;fullRouteFrames=4800;matchedCelestialFrames=1200;warmFramesPerRoute=180;measuredFramesPerRoute=1020;
 comparison='Full routes characterize each behavior; entry/candidate vessel views are not equivalent. Only matched-* celestial runs reset to the same initial pose and qualify as equivalent performance comparison.';
 limits=@('One machine, one resolution, bounded repeats.','Native GPU and CPU summaries are whole-run aggregates.','Focus refresh timer excludes input and includes copied endpoint preparation; full callback and permanent input cost populations are separately reported.','Full-route captures precede the optional matched-only observer mode; timer and workload body are unchanged. Original full-route observer binary was replaced by this diagnostic-only rebuild; accepted production/deployment binaries were not replaced.');runs=$runs;incomplete=$incomplete;incompleteDisposition='First sequence lost its parent tool process during entry repeat 2. Partial log excluded; exact cause unproven. Continuously awaited second pair completed.'})
$validation=Get-Content "$output/validation-results.json" -Raw|ConvertFrom-Json
$gates=@(foreach($gate in $validation){[ordered]@{name=$gate.name;exit=$gate.exit;arguments=$gate.arguments;log=$gate.log;sha256=(Get-FileHash $gate.log).Hash}})
if($gates.Count -ne 96 -or @($gates|Where-Object {$_.exit -ne 0}).Count -ne 0){throw 'Regression matrix incomplete'}
Json 'regression.json' ([ordered]@{gateCount=96;failed=0;builds=@(foreach($config in @('debug','release')){[ordered]@{configuration=$config;sha256=(Get-FileHash "$output/build-$config.txt").Hash;summary=(Get-Content "$output/build-$config.txt" -Raw).Trim()}});gates=$gates})
$camera=@(foreach($config in @('debug','release')){
 $normal=Get-Content "$output/florida-slab-camera-costs-$config.txt"
 $warp=Get-Content "$output/florida-slab-camera-warp-$config.txt"
 [ordered]@{configuration=$config;normalAllocation=@($normal|Where-Object {$_.StartsWith('ORDINARY_')});warpAllocation=@($warp|Where-Object {$_.StartsWith('ORDINARY_')});normalCosts=(Parsed $normal 'ACTIVE_VESSEL_COST ');warpCosts=(Parsed $warp 'ACTIVE_VESSEL_COST ');invariants=(Parsed $warp 'ACTIVE_VESSEL_WARP ');transitions=@($warp|Where-Object {$_.StartsWith('ACTIVE_VESSEL_FRAME_TRANSITIONS ')});storage=(Parsed $normal 'ACTIVE_VESSEL_STORAGE ')}
})
$failures=@(foreach($name in @('warp-test-release.txt','warp-allocation-diagnostic.txt','warp-test-corrected-release.txt','warp-transition-diagnostic.txt')){[ordered]@{log="$output/$name";sha256=(Get-FileHash "$output/$name").Hash}})
Json 'allocation-and-invariants.json' ([ordered]@{exactZeroScope='Warmed input, copied focus, frame reconstruction, final clearance, presentation update and mesh submission against a coherent published epoch; F-refocus. Zero tolerance.';dynamicSolarPublicationBytesPerDisplay=2320;inheritedDiscreteCelestialSwitchBytesPerAction=48;positiveControlBytes=152;configurations=$camera;diagnosticFailures=$failures;failureDisposition='Initial new whole-display NoGC measurement exited its 1MiB reservation because existing Solar publication allocates 2320 B/display. Whole-display allocation remains reported under normal runtime; existing camera exact-zero scope preserved, not relabeled full-display zero. Generic rebase test first combined 4e11 m coordinate subtraction with a 0.1mm tolerance; diagnostic eye delta was 0.357mm and up-vector error 2.06e-6. Transition test now isolates frame math at 4e8 m; existing separate 7e12 m precision test remains unchanged. No production workaround or tolerance broadening.'})
Write-Output 'Consolidated 8 complete renderer captures, 96 gates and exact-zero camera/invariant results.'
