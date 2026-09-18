$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
$e='docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/simplified-slab'
$o='build/srv01-stage5-simplified-slab'
$gates=Get-Content "$o/validation-results.json" -Raw|ConvertFrom-Json
$validation=@(foreach($g in $gates){
    $lines=Get-Content -LiteralPath $g.log
    [pscustomobject]@{name=$g.name;exit=$g.exit;sha256=(Get-FileHash -LiteralPath $g.log).Hash;witness=@($lines|Where-Object {$_ -match '^(FLORIDA_|ORDINARY_ALLOCATION|ORDINARY_CONTROL|NovaCore launcher tests passed|Precision|ReferenceFrames)'})}
})
$source=[IO.File]::ReadAllText((Join-Path $PWD 'tests/NovaCore.Simulation.Tests/Program.cs'))
$array=$source.Substring($source.IndexOf('var tests ='))
$array=$array.Substring(0,$array.IndexOf('};'))
$names=@([regex]::Matches($array,'\("([^"]+)",')|ForEach-Object {$_.Groups[1].Value})
$simulation=@(foreach($config in @('debug','release')){
    $lines=Get-Content "$o/Simulation-$config.txt"
    $missing=@($names|Where-Object {("PASS "+$_) -notin $lines})
    if($missing.Count){throw 'Simulation group missing'}
    [pscustomobject]@{configuration=$config;groups=$names.Count;passed=$names.Count;missing=$missing}
})
$performance=@(1..3|ForEach-Object {
    $i=$_;$lines=Get-Content "$o/performance-$i.txt"
    [pscustomobject]@{run=$i;result=($lines|Where-Object {$_ -like 'FLORIDA_PERFORMANCE *'}).Substring(20)|ConvertFrom-Json;storage=@($lines|Where-Object {$_ -like '*STORAGE*'});sha256=(Get-FileHash "$o/performance-$i.txt").Hash}
})
$live=@(foreach($name in @('manual','solar')){
    $log=Get-Content "$o/live-$name.txt"
    [pscustomobject]@{route=$name;identity=(Get-Content "$o/live-$name-result.json" -Raw|ConvertFrom-Json);sha256=(Get-FileHash "$o/live-$name.txt").Hash;stderr=(Get-Content "$o/live-$name.err.txt" -Raw);witness=@($log|Where-Object {$_ -match '^(STOCK_ASSEMBLY_END|STOCK_ASSEMBLY_FRAME|SRV01_LIVE_SERVICE|SLAB_LIVE_|SRV01_COLD_|FLORIDA_PRESENTATION_READY|Scene: sol|Solar workload:|\[native\] (Frame pacing:|CPU timings:|GPU timing averages:|NCSM1 regional physical totals:))'})}
})
$newSource=@('src/NovaCore.Core/Surface/FloridaSlabSupport.cs','tests/NovaCore.Graphics.Tests/AssemblyFloridaSolarTests.cs')
$entry=Get-Content "$e/entry-seals.json" -Raw|ConvertFrom-Json
$changes=@(foreach($s in $entry){if(!(Test-Path -LiteralPath $s.path)){throw "Entry file disappeared: $($s.path)"};if((Get-FileHash -LiteralPath $s.path).Hash-ne$s.sha256){[pscustomobject]@{path=$s.path;sha256=(Get-FileHash -LiteralPath $s.path).Hash}}})
$changes|ConvertTo-Json -Depth 4|Set-Content "$e/entry-changes.json"
$sourceSeals=@($changes|Where-Object path -Match '\.(cs|cpp|csproj)$')+@(foreach($p in $newSource){[pscustomobject]@{path=$p;sha256=(Get-FileHash -LiteralPath $p).Hash}})
$sourceSeals|ConvertTo-Json -Depth 4|Set-Content "$e/source-seals.json"
$bins=@(foreach($p in @('samples/NovaCore.Triangle/bin/Release/net10.0','tools/NovaCore.Launcher/bin/Release/net10.0-windows')){
    foreach($file in Get-ChildItem -LiteralPath $p -File -Recurse){[pscustomobject]@{path=[IO.Path]::GetRelativePath($PWD,$file.FullName);sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}}
})
$bins|ConvertTo-Json -Depth 4|Set-Content "$e/manual-binary-seals.json"
$tags=git show-ref --tags
$before=Get-Content "$e/tag-refs-before.txt"
$summary=[ordered]@{
    engineering='PASS';manual='PENDING';stage6='CLOSED';banked=$false
    head=(git rev-parse HEAD);main=(git rev-parse main);originMain=(git rev-parse origin/main);branch=(git branch --show-current)
    historicalTagCount=$tags.Count;historicalTagsUnchanged=(!(Compare-Object $before $tags));staged=@(git diff --cached --name-only)
    entryFiles=$entry.Count;entryChanged=$changes.Count;historicalEvidenceChanged=@($changes|Where-Object path -Match '^docs/')
    simulation=$simulation
    builds=@(foreach($config in @('debug','release')){[pscustomobject]@{configuration=$config;full=Get-Content "$o/full-build-$config.txt";nativeSha256=(Get-FileHash "$o/native-$config/NovaCore.Native.dll").Hash}})
    validation=$validation;performance=$performance;live=$live
    setupFailures=@('Focused test initially omitted required generation-4 selection; fixed before physics.', 'Test assumed optional compound-reducer callback always had four rows; use solver-facing export and independent geometry instead.', 'Test source nullable annotation fixed at compile.', 'Full solution exposed missing internal Graphics access for Triangle; narrow friend assembly added.', 'Current-epoch added oracle initially used a different full-epoch rounding contract; replaced by independent split-angle matrix, no production arithmetic/tolerance changed.', 'Observer deployment initially omitted managed closure: failed before Main/scene; copied qualified closure; no physical/performance sample from failed launch.')
}
$summary|ConvertTo-Json -Depth 15|Set-Content "$e/results.json"
Write-Output "Consolidated $($gates.Count) gates; Simulation groups=$($names.Count); source seals=$($sourceSeals.Count); tags unchanged=$($summary.historicalTagsUnchanged)"
