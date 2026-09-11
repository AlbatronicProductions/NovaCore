# Reproduction recipe only. The authorized 14 processes are complete; do not execute without a new Project Control decision.
# PowerShell 7; no canonical writes, cleanup, runtime-setting changes, acceptance, or retries.
# Uses the retained global observer plan/source, not a new tracing mechanism.
param(
 [Parameter(Mandatory)][ValidateSet('Prepare','Build','Run','PrepareMarkers','BuildMarkers','Restore')][string]$Phase,
 [ValidateSet('A','B','C','CM')][string]$State='A',
 [ValidateRange(1,5)][int]$Run=1,
 [switch]$QualifiedCFailureReviewed
)
$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$banked='f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe'
function Assert-Canonical {
 if((git -C $repo branch --show-current) -ne 'codex/root-linked-private-propagation'){throw 'Branch changed'}
 foreach($ref in @('HEAD','main','origin/main','m14.14-private-root-postimpact-state^{}')){
  if((git -C $repo rev-parse $ref) -ne $banked){throw "Baseline changed: $ref"}
 }
 $paths=@(git -C $repo ls-files --cached --others --exclude-standard -- src tests|Where-Object{$_ -match '\.(cs|csproj)$'}|Sort-Object -Unique)
 if($paths.Count -ne 315){throw 'Source/project count changed'}
 $manifest=($paths|ForEach-Object{$_+' '+(Get-FileHash -LiteralPath (Join-Path $repo $_)).Hash}) -join [char]10
 if([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($manifest))) -ne '599CCDF9B4ADA20B964100B4DD43C5230C83000E54EDC389BF07A92108A77B92'){throw 'Canonical fingerprint changed'}
 if((dotnet --version) -ne '10.0.303'){throw 'SDK mismatch'}
 if(!((dotnet --list-runtimes) -match '^Microsoft.NETCore.App 10.0.12 ')){throw 'Runtime unavailable'}
}

function Prepare-Variants {
$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$banked='f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe'
if(Test-Path -LiteralPath $scratch){throw 'Scratch exists'}
New-Item -ItemType Directory -Path $scratch|Out-Null
$closure=@('src/NovaCore.Core','src/NovaCore.EphemerisFormat','src/NovaCore.Simulation','tests/NovaCore.Simulation.Tests')
foreach($state in @('A','B','C')){
 $root=Join-Path $scratch $state
 git -C $repo worktree add --detach --no-checkout $root $banked
 if($LASTEXITCODE -ne 0){throw 'Worktree add failed'}
 git -C $root sparse-checkout set --cone @closure
 if($LASTEXITCODE -ne 0){throw 'Sparse checkout failed'}
 git -C $root checkout --detach $banked
 if($LASTEXITCODE -ne 0){throw 'Checkout failed'}
 if(@(git -C $root status --porcelain).Count){throw 'Baseline not clean'}
 if($state -ne 'A'){
  foreach($path in @(git -C $repo ls-files --cached --others --exclude-standard -- @closure|Where-Object{$_ -match '\.(cs|csproj)$'}|Sort-Object -Unique)){
   Copy-Item -LiteralPath (Join-Path $repo $path) -Destination (Join-Path $root $path)
  }
 }
}
$path="$scratch\C\tests\NovaCore.Simulation.Tests\Program.cs"
$text=[IO.File]::ReadAllText($path)
$line='    ("Paired private canonical propagation", PrivateCanonicalPropagationTests.Run),'
$pattern='(?m)^'+[regex]::Escape($line)+'\r?\n'
if([regex]::Matches($text,$pattern).Count -ne 1){throw 'Candidate row not unique'}
[IO.File]::WriteAllText($path,[regex]::Replace($text,$pattern,''))
# C retains the candidate selector and both complete candidate source files.
if(-not [IO.File]::ReadAllText($path).Contains('--private-propagation-only')){throw 'Selector changed'}

$recipe=[IO.File]::ReadAllText("$repo\docs\engineering-evidence\private-canonical-propagation\global-gate-observer-reproduce.ps1")
$match=[regex]::Match($recipe,'(?s)\$instrumentation = @''\r?\n(.*?)\r?\n''@')
if(!$match.Success){throw 'Retained observer plan missing'}
$plan=$match.Groups[1].Value|ConvertFrom-Json
$plan.edits[0].old='using System.Diagnostics;'
$plan.edits[0].newText='using System.Diagnostics;'+[char]10+[char]10+'GlobalGateAllocationObserver.Prepare();'
$plan|ConvertTo-Json -Depth 6 -Compress|Set-Content -LiteralPath "$scratch\instrumentation.json" -Encoding utf8

$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$plan=Get-Content -LiteralPath "$scratch\instrumentation.json" -Raw|ConvertFrom-Json
$states=@()
foreach($state in @('A','B','C')){
    $root=Join-Path $scratch $state
    $testRoot=Join-Path $root 'tests/NovaCore.Simulation.Tests'
    $before=@()
    foreach($path in @(Get-ChildItem -LiteralPath "$root\src\NovaCore.Core","$root\src\NovaCore.EphemerisFormat","$root\src\NovaCore.Simulation",$testRoot -Recurse -File|Where-Object{$_.Extension -in @('.cs','.csproj') })){
        $before+=[pscustomobject]@{path=[IO.Path]::GetRelativePath($root,$path.FullName).Replace('\','/');sha256=(Get-FileHash -LiteralPath $path.FullName).Hash;normalizedSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($path.FullName).Replace([string][char]13+[char]10,[string][char]10))))}
    }
    $before|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\$state-source-before.json" -Encoding utf8
    $programBefore=[IO.File]::ReadAllText("$testRoot\Program.cs").Replace([string][char]13+[char]10,[string][char]10)
    foreach($file in @($plan.edits.file|Select-Object -Unique)){
        $path=Join-Path $testRoot $file
        $backup=Join-Path "$scratch\backups\$state" $file
        New-Item -ItemType Directory -Force -Path (Split-Path $backup)|Out-Null
        Copy-Item -LiteralPath $path -Destination $backup
        $text=[IO.File]::ReadAllText($path).Replace([string][char]13+[char]10,[string][char]10)
        foreach($edit in @($plan.edits|Where-Object{$_.file -eq $file})){
            $pattern='(?m)^'+[regex]::Escape($edit.old)
            if([regex]::Matches($text,$pattern).Count -ne 1){throw "Non-unique hook: $state $file $($edit.old)"}
            $replacement=$edit.newText
            $text=[regex]::Replace($text,$pattern,[Text.RegularExpressions.MatchEvaluator]{param($match) $replacement})
        }
        [IO.File]::WriteAllText($path,$text)
    }
    [IO.File]::WriteAllText("$testRoot\GlobalGateAllocationObserver.cs",$plan.adapter)
    $programAfter=[IO.File]::ReadAllText("$testRoot\Program.cs")
    $registrationPattern='(?s)var tests = new .*?\[\].*?\n\};'
    $regBefore=[regex]::Match($programBefore,$registrationPattern).Value
    if(!$regBefore -or $regBefore -cne [regex]::Match($programAfter,$registrationPattern).Value){throw "Registration changed: $state"}
    $groups=@([regex]::Matches($regBefore,'\("([^"]+)",')|ForEach-Object{$_.Groups[1].Value})
    $states+=[pscustomobject]@{state=$state;sourceCount=$before.Count;groups=$groups;adapter=(Get-FileHash -LiteralPath "$testRoot\GlobalGateAllocationObserver.cs").Hash;instrumented=@($plan.edits.file|Select-Object -Unique|ForEach-Object{[pscustomobject]@{file=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $testRoot $_)).Hash}})}
}
$baseline=Get-Content -LiteralPath "$scratch\A-source-before.json" -Raw|ConvertFrom-Json
$candidate=Get-Content -LiteralPath "$scratch\B-source-before.json" -Raw|ConvertFrom-Json
$normalizedDiff=@($candidate|Where-Object{$entry=$_;$old=@($baseline|Where-Object{$_.path -eq $entry.path});$old.Count -ne 1 -or $old[0].normalizedSha256 -ne $entry.normalizedSha256}|Select-Object path,sha256)
[pscustomobject]@{states=$states;normalizedStateDifferences=$normalizedDiff;planSha256=(Get-FileHash -LiteralPath "$scratch\instrumentation.json").Hash}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath "$scratch\comparison-preflight.json" -Encoding utf8
Get-Content -LiteralPath "$scratch\comparison-preflight.json" -Raw

# The complete B/C difference before common instrumentation must be one registration row.
$b=Get-Content -LiteralPath "$scratch\B-source-before.json" -Raw|ConvertFrom-Json
$c=Get-Content -LiteralPath "$scratch\C-source-before.json" -Raw|ConvertFrom-Json
$delta=@($b|Where-Object{$entry=$_;@($c|Where-Object{$_.path -eq $entry.path -and $_.sha256 -eq $entry.sha256}).Count -ne 1})
if($delta.Count -ne 1 -or $delta[0].path -ne 'tests/NovaCore.Simulation.Tests/Program.cs'){throw 'B/C comparison invalid'}
}
function Build-Variants {
if(Test-Path -LiteralPath "$scratch\build-identities.json"){throw 'No repeat build'}
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$builds=@()
foreach($state in @('A','B','C')){
    $root=Join-Path $scratch $state
    Push-Location -LiteralPath $root
    try {
        $output=@(dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --nologo 2>&1)
        $code=$LASTEXITCODE
    } finally {Pop-Location}
    $output|Set-Content -LiteralPath "$scratch\$state-build.txt" -Encoding utf8
    if($code -ne 0 -or !($output -match '0 Warning\(s\)') -or !($output -match '0 Error\(s\)')){throw "Build failed or nonzero warnings: $state"}
    $closure=@(Get-ChildItem -LiteralPath "$root\tests\NovaCore.Simulation.Tests\bin\Release\net10.0" -File|Where-Object{$_.Extension -in @('.dll','.json')}|Sort-Object Name|ForEach-Object{[pscustomobject]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
    $builds+=[pscustomobject]@{state=$state;exit=$code;warnings=0;errors=0;closure=$closure}
}
$builds|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\build-identities.json" -Encoding utf8
$builds|ConvertTo-Json -Depth 5

}
function Run-Variant {
param([ValidateSet('A','B','C','CM')][string]$State,[ValidateRange(1,5)][int]$Run)
$ErrorActionPreference='Stop'
if($State -ne 'C' -and $Run -gt 3){throw 'Variant A/B limit is three'}
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$root=Join-Path $scratch $(if($State -eq 'CM'){'C'}else{$State})
$label="$State-$Run"
if(Test-Path -LiteralPath "$scratch\$label.started"){throw 'No retries'}
$buildFile=if($State -eq 'CM'){"$scratch\marker-build-identities.json"}else{"$scratch\build-identities.json"}
$build=Get-Content -LiteralPath $buildFile -Raw|ConvertFrom-Json|Where-Object{$_.state -eq $State}
foreach($file in $build.closure){if((Get-FileHash -LiteralPath "$root\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\$($file.file)").Hash -ne $file.sha256){throw 'Binary closure changed'}}
$psi=[Diagnostics.ProcessStartInfo]::new()
$psi.FileName='C:\Program Files\dotnet\dotnet.exe'
$psi.WorkingDirectory=$root
$psi.UseShellExecute=$false
$psi.CreateNoWindow=$true
$psi.RedirectStandardOutput=$true
$psi.RedirectStandardError=$true
$psi.ArgumentList.Add('tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll')
$unexpected=@($psi.Environment.Keys|Where-Object{$_ -match '^(CORECLR|COR_|COMPlus)' -or ($_ -match '^DOTNET' -and $_ -notin @('DOTNET_ROOT','DOTNET_ROOT(x86)'))})
if($unexpected.Count){throw "Unexpected runtime environment: $unexpected"}
$psi.Environment['CORECLR_ENABLE_PROFILING']='1'
$psi.Environment['CORECLR_PROFILER']='{61D15631-E78F-45D1-A00E-20FC6F1CED24}'
$psi.Environment['CORECLR_PROFILER_PATH']='E:\NovaCore\.codex\sas-allocation-classification\observer\profiler.dll'
if((Get-FileHash -LiteralPath $psi.Environment['CORECLR_PROFILER_PATH']).Hash -ne '8B86C6B14BFF5E35FCE4873C17A9B3A0620C9BB30BDD60A211CA60216A755E31'){throw 'Observer changed'}
$begin=[DateTimeOffset]::Now
[IO.File]::WriteAllText("$scratch\$label.started",$begin.ToString('O'))
$p=[Diagnostics.Process]::new();$p.StartInfo=$psi
if(!$p.Start()){throw 'Start failed'}
$pidValue=$p.Id
$outTask=$p.StandardOutput.ReadToEndAsync();$errTask=$p.StandardError.ReadToEndAsync()
$p.WaitForExit()
$stdout=$outTask.GetAwaiter().GetResult();$stderr=$errTask.GetAwaiter().GetResult()
[IO.File]::WriteAllText("$scratch\$label.stdout.txt",$stdout)
[IO.File]::WriteAllText("$scratch\$label.stderr.txt",$stderr)
$result=[pscustomobject]@{state=$State;run=$Run;label=$label;pid=$pidValue;start=$begin.ToString('O');end=[DateTimeOffset]::Now.ToString('O');exit=$p.ExitCode;cwd=$root;arguments=@($psi.ArgumentList);stdoutBytes=[Text.Encoding]::UTF8.GetByteCount($stdout);stderrBytes=[Text.Encoding]::UTF8.GetByteCount($stderr);stdoutSha256=(Get-FileHash -LiteralPath "$scratch\$label.stdout.txt").Hash;stderrSha256=(Get-FileHash -LiteralPath "$scratch\$label.stderr.txt").Hash}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\$label.json" -Encoding utf8
$result|ConvertTo-Json -Depth 5 -Compress

}
function Prepare-Markers {
if(Test-Path -LiteralPath "$scratch\marker-backup.json"){throw 'Markers already prepared'}
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$root="$scratch\C"
$program="$root\tests\NovaCore.Simulation.Tests\Program.cs"
$expected=(Get-Content -LiteralPath "$scratch\comparison-preflight.json" -Raw|ConvertFrom-Json).states|Where-Object{$_.state -eq 'C'}
if((Get-FileHash -LiteralPath $program).Hash -ne ($expected.instrumented|Where-Object{$_.file -eq 'Program.cs'}).sha256){throw 'C preimage mismatch'}
$paths=@('tests/NovaCore.Simulation.Tests/Program.cs')
$paths+=@(Get-ChildItem -LiteralPath "$root\tests\NovaCore.Simulation.Tests\bin\Release","$root\tests\NovaCore.Simulation.Tests\obj\Release" -Recurse -File|ForEach-Object{[IO.Path]::GetRelativePath($root,$_.FullName).Replace('\','/')})
$backup=@()
foreach($path in $paths){
 $from=Join-Path $root $path;$dest=Join-Path "$scratch\marker-backup" $path
 New-Item -ItemType Directory -Path (Split-Path $dest) -Force|Out-Null
 Copy-Item -LiteralPath $from -Destination $dest
 $backup+=[pscustomobject]@{path=$path;sha256=(Get-FileHash -LiteralPath $from).Hash}
}
$backup|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\marker-backup.json" -Encoding utf8
$text=[IO.File]::ReadAllText($program)
$old='GlobalGateAllocationObserver.Prepare();'
$new=$old+[char]10+'CandidateModuleSnapshot.Record("before-registration");'
if([regex]::Matches($text,[regex]::Escape($old)).Count -ne 1){throw 'Preparation anchor mismatch'}
$text=$text.Replace($old,$new)
$old='    foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }'
$new='    foreach (var (name, test) in tests) { if (name == "SAS sign/frame continuity proof") CandidateModuleSnapshot.Record("before-SAS"); test(); Console.WriteLine($"PASS {name}"); }'
if(!$text.Contains($old)){throw 'Runner anchor mismatch'}
[IO.File]::WriteAllText($program,$text.Replace($old,$new))

$marker=@'
// Temporary Step 3 passive module-presence observation. Called only outside allocation windows.
internal static class CandidateModuleSnapshot
{
    internal static void Record(string boundary)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is "NovaCore.Simulation" or "NovaCore.Simulation.Tests")
                Console.WriteLine($"CANDIDATE_MODULE boundary={boundary}; qpc={System.Diagnostics.Stopwatch.GetTimestamp()}; thread={Environment.CurrentManagedThreadId}; assembly={name}; location={assembly.Location}");
        }
        Console.WriteLine($"CANDIDATE_MODULE_END boundary={boundary}");
    }
}

'@
[IO.File]::WriteAllText("$scratch\C\tests\NovaCore.Simulation.Tests\CandidateModuleSnapshot.cs",$marker)
}
function Build-Markers {
if(Test-Path -LiteralPath "$scratch\marker-build-identities.json"){throw 'No repeat marker build'}
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$root="$scratch\C"
$before=(Get-Content -LiteralPath "$scratch\build-identities.json" -Raw|ConvertFrom-Json)|Where-Object{$_.state -eq 'C'}
Push-Location -LiteralPath $root
try {$output=@(dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --nologo --no-restore -p:BuildProjectReferences=false 2>&1);$exitCode=$LASTEXITCODE}finally{Pop-Location}
$output|Set-Content -LiteralPath "$scratch\CM-build.txt" -Encoding utf8
if($exitCode -ne 0 -or !($output -match '0 Warning\(s\)') -or !($output -match '0 Error\(s\)')){throw 'Marked build failed'}
foreach($file in @($before.closure|Where-Object{$_.file -in @('NovaCore.Core.dll','NovaCore.EphemerisFormat.dll','NovaCore.Simulation.dll')})){
 if((Get-FileHash -LiteralPath "$root\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\$($file.file)").Hash -ne $file.sha256){throw 'Production dependency changed'}
}
$result=[pscustomobject]@{state='CM';exit=0;warnings=0;errors=0;productionClosureUnchanged=$true;programSha256=(Get-FileHash -LiteralPath "$root\tests\NovaCore.Simulation.Tests\Program.cs").Hash;markerSha256=(Get-FileHash -LiteralPath "$root\tests\NovaCore.Simulation.Tests\CandidateModuleSnapshot.cs").Hash;closure=@(Get-ChildItem -LiteralPath "$root\tests\NovaCore.Simulation.Tests\bin\Release\net10.0" -File|Where-Object{$_.Extension -in @('.dll','.json')}|Sort-Object Name|ForEach-Object{[pscustomobject]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\marker-build-identities.json" -Encoding utf8
$result|ConvertTo-Json -Depth 5 -Compress

}
function Restore-Preimages {
# Non-destructive restoration only. Added diagnostic files remain classified disposable.
# Removing them or worktrees requires a separately permitted cleanup; no policy workaround.
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-registration-discriminator'
$markerBackup=@()
$markerChanged=@()
if(Test-Path -LiteralPath "$scratch\marker-backup.json"){
$markerBackup=Get-Content -LiteralPath "$scratch\marker-backup.json" -Raw|ConvertFrom-Json
foreach($file in $markerBackup){Copy-Item -LiteralPath (Join-Path "$scratch\marker-backup" $file.path) -Destination (Join-Path "$scratch\C" $file.path)}
$markerChanged=@($markerBackup|Where-Object{(Get-FileHash -LiteralPath (Join-Path "$scratch\C" $_.path)).Hash -ne $_.sha256})
if($markerChanged.Count){throw 'Marker preimage restore failed'}
}
$states=@()
foreach($state in @('A','B','C')){
 $root=Join-Path $scratch $state
 foreach($file in @('Program.cs','OrdinaryAllocationMeasurement.cs','PhysicalEventEpochTests.cs','ContactGenerationTests.cs','IsolatedContactResponseTests.cs')){
  Copy-Item -LiteralPath "$scratch\backups\$state\$file" -Destination "$root\tests\NovaCore.Simulation.Tests\$file"
 }
 $before=Get-Content -LiteralPath "$scratch\$state-source-before.json" -Raw|ConvertFrom-Json
 $changed=@($before|Where-Object{(Get-FileHash -LiteralPath (Join-Path $root $_.path)).Hash -ne $_.sha256})
 if($changed.Count){throw 'Variant source preimage mismatch'}
 $ordered=$before|Sort-Object path
 $manifest=($ordered|ForEach-Object{$_.path+' '+$_.sha256}) -join [char]10
 $normal=($ordered|ForEach-Object{$_.path+' '+$_.normalizedSha256}) -join [char]10
 $states+=[pscustomobject]@{state=$state;preimageCount=$before.Count;preimageMismatches=$changed.Count;manifestSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($manifest)));normalizedManifestSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normal)));extraDiagnosticFilesRetained=@(Get-ChildItem -LiteralPath "$root\tests\NovaCore.Simulation.Tests" -File|Where-Object{$_.Name -in @('GlobalGateAllocationObserver.cs','CandidateModuleSnapshot.cs')}|Select-Object -ExpandProperty Name)}
}
[pscustomobject]@{markerSourceBinaryPreimagesRestored=$markerBackup.Count;markerPreimageMismatches=$markerChanged.Count;states=$states;cleanupDisposition='DISPOSABLE SCRATCH RETAINED DUE TOOLING/POLICY RESTRICTION';deletions=0}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "$scratch\restoration.json" -Encoding utf8
Get-Content -LiteralPath "$scratch\restoration.json" -Raw

}
Assert-Canonical
switch($Phase) {
 'Prepare' {Prepare-Variants}
 'Build' {Build-Variants}
 'Run' {
  $sequence=@('A-1','B-1','C-1','A-2','B-2','C-2','A-3','B-3','C-3','C-4','C-5','CM-1','CM-2','CM-3')
  $label="$State-$Run";$index=[Array]::IndexOf($sequence,$label)
  if($index -lt 0){throw 'Outside bounded matrix'}
  for($i=0;$i -lt $index;$i++){if(!(Test-Path -LiteralPath "$scratch\$($sequence[$i]).json")){throw 'Preceding run incomplete'}}
  if($State -eq 'CM' -and !(Test-Path -LiteralPath "$scratch\marker-build-identities.json")){throw 'Markers not built'}
  Run-Variant -State $State -Run $Run
  Write-Output 'Review scope, controls, runtime identity and gate results before the next authorized process. No automatic retry.'
 }
 'PrepareMarkers' {
  if(!$QualifiedCFailureReviewed){throw 'Step 3 requires a reviewed valid C reproduction, not just a nonzero process exit'}
  foreach($id in 1..5){if(!(Test-Path -LiteralPath "$scratch\C-$id.json")){throw 'C matrix incomplete'}}
  Prepare-Markers
 }
 'BuildMarkers' {Build-Markers}
 'Restore' {Restore-Preimages}
}
Assert-Canonical
