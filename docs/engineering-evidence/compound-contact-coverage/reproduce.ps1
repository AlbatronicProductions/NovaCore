# Explicit diagnostic reproduction only. Does not edit the frozen candidate or its ordinary build output.
param([switch]$Run)
$ErrorActionPreference='Stop'
if(!$Run){throw 'Use -Run only for an authorized fresh diagnostic reproduction.'}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
if((git rev-parse HEAD) -ne '5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0'){throw 'Unexpected baseline'}
if((git branch --show-current) -ne 'codex/engineering-spacecraft-contact-article'){throw 'Unexpected branch'}
$identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../engineering-spacecraft-contact-article/current-draft-identity.json') -Raw | ConvertFrom-Json
foreach($f in $identity.files){if((Get-FileHash -LiteralPath $f.path).Hash -ne $f.sha256){throw ('Candidate mismatch: '+$f.path)}}
$output=[IO.Path]::GetFullPath((Join-Path $repo 'build/compound-coverage-reproduction'))
if(Test-Path -LiteralPath $output){throw 'Prior reproduction exists; review it rather than overwrite or retry'}
New-Item -ItemType Directory -Path $output | Out-Null
$frozen=@(git ls-files -co --exclude-standard src tests external | Sort-Object -Unique | ForEach-Object {[pscustomobject]@{path=$_;hash=(Get-FileHash -LiteralPath $_).Hash}})
try {
    foreach($name in @('CoverageRuntime.cs','CoverageTests.cs','inject.targets')){
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('reproduction/'+$name)) -Destination (Join-Path $output $name)
    }
    $callbacks=Get-Content -LiteralPath 'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs' -Raw
    $callbacks=$callbacks.Replace('internal int ArticleChildMask;','internal int ArticleChildMask;'+[Environment]::NewLine+'    internal CoverageProbe? Coverage;')
    $callbacks=$callbacks.Replace('material = new PairMaterialProperties(.5f, 2f, new SpringSettings(30, 1));','material = new PairMaterialProperties(.5f, 2f, new SpringSettings(30, 1));'+[Environment]::NewLine+'        metrics.Coverage?.Parent(ref manifold);')
    $callbacks=$callbacks.Replace('var child = pair.A.Mobility == CollidableMobility.Dynamic ? childA : childB;','metrics.Coverage?.Child(pair, childA, childB, ref manifold);'+[Environment]::NewLine+'        var child = pair.A.Mobility == CollidableMobility.Dynamic ? childA : childB;')
    [IO.File]::WriteAllText((Join-Path $output 'LocalContactCallbacks.cs'),$callbacks)
    $tests=(Get-Content -LiteralPath 'tests/NovaCore.Simulation.Tests/EngineeringContactArticleTests.cs' -Raw).Replace('internal static class EngineeringContactArticleTests','internal static partial class EngineeringContactArticleTests')
    [IO.File]::WriteAllText((Join-Path $output 'EngineeringContactArticleTests.cs'),$tests)
    $program=Get-Content -LiteralPath 'tests/NovaCore.Simulation.Tests/Program.cs' -Raw
    $program=$program.Replace('if (args.Contains("--engineering-article-cheap", StringComparer.Ordinal))','if (args.Length==2 && args[0]=="--coverage-probe") { EngineeringContactArticleTests.Coverage(args[1]); return; }'+[Environment]::NewLine+'if (args.Contains("--engineering-article-cheap", StringComparer.Ordinal))')
    [IO.File]::WriteAllText((Join-Path $output 'Program.cs'),$program)
    $artifacts=Join-Path $output 'artifacts';$injection=Join-Path $output 'inject.targets'
    dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --nologo --artifacts-path $artifacts "-p:CustomAfterMicrosoftCommonTargets=$injection"
    if($LASTEXITCODE -ne 0){throw 'Diagnostic build failed; no physics run'}
    $binary=Join-Path $artifacts 'bin/NovaCore.Simulation.Tests/debug/NovaCore.Simulation.Tests.dll'
    $binaryHash=(Get-FileHash -LiteralPath $binary).Hash
    $normal=$null
    foreach($arm in @('normal','right','bus')){
        if((Get-FileHash -LiteralPath $binary).Hash -ne $binaryHash){throw 'Binary changed during matrix'}
        $result=& dotnet $binary --coverage-probe $arm 2>&1
        if($LASTEXITCODE -ne 0){$result;throw ('Diagnostic failure: '+$arm)}
        $json=($result | Where-Object {$_ -like 'COVERAGE_RESULT *'}).Substring(16)
        [IO.File]::WriteAllText((Join-Path $output ($arm+'.json')),$json)
        $data=$json | ConvertFrom-Json
        if($arm -eq 'normal'){
            $normal=$data
            if($data.peak -ne 0.08786423715152591 -or $data.peakStep -ne 35){throw 'Original signal not reproduced'}
        } else {
            foreach($i in 0..34){if(($data.rows[$i].after|ConvertTo-Json -Compress) -cne ($normal.rows[$i].after|ConvertTo-Json -Compress)){throw 'Pre-intervention trajectory differs'}}
        }
        Write-Output ([pscustomobject]@{arm=$arm;peak=$data.peak;peakStep=$data.peakStep;replacements=$data.Replacements})
    }
} finally {
    foreach($f in $frozen){if((Get-FileHash -LiteralPath $f.path).Hash -ne $f.hash){throw ('Frozen input changed: '+$f.path)}}
}
# No automatic deletion; all generated artifacts stay inside the new reviewed output directory.
