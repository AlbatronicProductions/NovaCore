# Reproduction only. This ten-process differential is complete. New execution needs Project Control authorization.
# Reuses retained exact-object observer and hook plan; never modifies canonical source/binaries.
param([ValidateSet('Prepare','Build','Run','Restore')][string]$Phase,[ValidateSet('banked','candidate')][string]$State,[ValidateRange(1,5)][int]$Run=1)
function Prepare-Comparison {
$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$scratch='E:\NovaCore\.codex\private-propagation-banked-differential'
$banked='f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe'
if((git -C $repo branch --show-current) -ne 'codex/root-linked-private-propagation'){throw 'Candidate branch mismatch'}
if((git -C $repo rev-parse HEAD) -ne $banked){throw 'Candidate HEAD mismatch'}
$manifest=@(git -C $repo ls-files --cached --others --exclude-standard -- src tests|Where-Object{$_ -match '\.(cs|csproj)$'}|Sort-Object -Unique|ForEach-Object{$_+' '+(Get-FileHash -LiteralPath (Join-Path $repo $_)).Hash}) -join [char]10
if([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($manifest))) -ne '599CCDF9B4ADA20B964100B4DD43C5230C83000E54EDC389BF07A92108A77B92'){throw 'Canonical source/project identity mismatch'}
if(Test-Path -LiteralPath $scratch){throw 'Scratch exists; no overwrite'}
New-Item -ItemType Directory -Path $scratch|Out-Null
$closure=@('src/NovaCore.Core','src/NovaCore.EphemerisFormat','src/NovaCore.Simulation','tests/NovaCore.Simulation.Tests')
foreach($state in @('banked','candidate')){
    $root=Join-Path $scratch $state
    git -C $repo worktree add --detach --no-checkout $root $banked
    if($LASTEXITCODE -ne 0){throw 'Worktree add failed'}
    git -C $root sparse-checkout set --cone @closure
    if($LASTEXITCODE -ne 0){throw 'Sparse checkout failed'}
    git -C $root checkout --detach $banked
    if($LASTEXITCODE -ne 0){throw 'Checkout failed'}
    if(@(git -C $root status --porcelain).Count){throw 'Baseline not clean'}
}
foreach($path in @(git -C $repo ls-files --cached --others --exclude-standard -- @closure|Where-Object{$_ -match '\.(cs|csproj)$'}|Sort-Object -Unique)){
    Copy-Item -LiteralPath (Join-Path $repo $path) -Destination (Join-Path "$scratch\candidate" $path)
}
# Reuse the already-retained observer implementation, without executing its older campaign.
$recipe=[IO.File]::ReadAllText("$repo\docs\engineering-evidence\private-canonical-propagation\global-gate-observer-reproduce.ps1")
$match=[regex]::Match($recipe,'(?s)\$instrumentation = @''\r?\n(.*?)\r?\n''@')
if(!$match.Success){throw 'Retained observer plan not found'}
$plan=$match.Groups[1].Value|ConvertFrom-Json
$plan.edits[0].old='using System.Diagnostics;'
$plan.edits[0].newText='using System.Diagnostics;'+[char]10+[char]10+'GlobalGateAllocationObserver.Prepare();'
$plan|ConvertTo-Json -Depth 6 -Compress|Set-Content -LiteralPath "$scratch\instrumentation.json" -Encoding utf8

$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-banked-differential'
$plan=Get-Content -LiteralPath "$scratch\instrumentation.json" -Raw|ConvertFrom-Json
$states=@()
foreach($state in @('banked','candidate')){
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
$baseline=Get-Content -LiteralPath "$scratch\banked-source-before.json" -Raw|ConvertFrom-Json
$candidate=Get-Content -LiteralPath "$scratch\candidate-source-before.json" -Raw|ConvertFrom-Json
$normalizedDiff=@($candidate|Where-Object{$entry=$_;$old=@($baseline|Where-Object{$_.path -eq $entry.path});$old.Count -ne 1 -or $old[0].normalizedSha256 -ne $entry.normalizedSha256}|Select-Object path,sha256)
[pscustomobject]@{states=$states;normalizedStateDifferences=$normalizedDiff;planSha256=(Get-FileHash -LiteralPath "$scratch\instrumentation.json").Hash}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath "$scratch\comparison-preflight.json" -Encoding utf8
Get-Content -LiteralPath "$scratch\comparison-preflight.json" -Raw

}
function Build-Comparison {
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-banked-differential'
if(Test-Path -LiteralPath "$scratch\build-identities.json"){throw 'Already built; no rebuild during matrix'}
$builds=@()
foreach($state in @('banked','candidate')){
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
function Run-Comparison {
param([ValidateSet('banked','candidate')][string]$State,[ValidateRange(1,5)][int]$Run)
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-banked-differential'
$root=Join-Path $scratch $State
$label="$State-$Run"
if(Test-Path -LiteralPath "$scratch\$label.started"){throw 'No retries'}
$build=Get-Content -LiteralPath "$scratch\build-identities.json" -Raw|ConvertFrom-Json|Where-Object{$_.state -eq $State}
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
function Restore-Comparison {
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-banked-differential'
$pre=Get-Content -LiteralPath "$scratch\comparison-preflight.json" -Raw|ConvertFrom-Json
$restored=@()
foreach($state in @('banked','candidate')){
    $root=Join-Path $scratch $state
    $identity=$pre.states|Where-Object{$_.state -eq $state}
    foreach($file in $identity.instrumented){
        $path=Join-Path "$root\tests\NovaCore.Simulation.Tests" $file.file
        if((Get-FileHash -LiteralPath $path).Hash -ne $file.sha256){throw 'Unexpected instrumented content'}
        Copy-Item -LiteralPath "$scratch\backups\$state\$($file.file)" -Destination $path
    }
    $adapter=Join-Path "$root\tests\NovaCore.Simulation.Tests" 'GlobalGateAllocationObserver.cs'
    if((Get-FileHash -LiteralPath $adapter).Hash -ne $identity.adapter){throw 'Adapter mismatch'}
    Remove-Item -LiteralPath $adapter -Force -ErrorAction Stop
    $before=Get-Content -LiteralPath "$scratch\$state-source-before.json" -Raw|ConvertFrom-Json
    $changed=@($before|Where-Object{(Get-FileHash -LiteralPath (Join-Path $root $_.path)).Hash -ne $_.sha256})
    if($changed.Count){throw 'Restoration mismatch'}
    $ordered=$before|Sort-Object path
    $manifest=($ordered|ForEach-Object{$_.path+' '+$_.sha256}) -join [char]10
    $normal=($ordered|ForEach-Object{$_.path+' '+$_.normalizedSha256}) -join [char]10
    $restored+=[pscustomobject]@{state=$state;count=$before.Count;changed=$changed.Count;adapterAbsent=!(Test-Path -LiteralPath $adapter);manifestSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($manifest)));normalizedManifestSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normal)))}
}
$bankedProgram=[IO.File]::ReadAllText("$scratch\backups\banked\Program.cs").Replace([string][char]13+[char]10,[string][char]10)
$candidateProgram=[IO.File]::ReadAllText("$scratch\backups\candidate\Program.cs").Replace([string][char]13+[char]10,[string][char]10)
$withoutCandidate=($candidateProgram -split [char]10|Where-Object{$_ -notmatch 'PrivateCanonicalPropagationTests\.Run'}) -join [char]10
if($withoutCandidate -cne $bankedProgram){throw 'Other runner difference exists'}
[pscustomobject]@{restored=$restored;onlyTwoCandidateRegistrationLinesDiffer=$true;baselineRemainingNumstat=@(git -C "$scratch\banked" diff --numstat);candidateRemainingNumstat=@(git -C "$scratch\candidate" diff --numstat)}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$scratch\restoration.json" -Encoding utf8
Get-Content -LiteralPath "$scratch\restoration.json" -Raw

}
switch($Phase){'Prepare'{Prepare-Comparison};'Build'{Build-Comparison};'Run'{Run-Comparison -State $State -Run $Run};'Restore'{Restore-Comparison}}
